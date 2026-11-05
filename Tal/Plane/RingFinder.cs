// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal planes module: RingFinder.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Tal.Core.PowderDiffraction
{
 /// <summary>
 /// Finds rings (cycles) in a molecule's bond graph.
 /// Used by PlanePropertiesWindow to select a ring of atoms.
 /// 
 /// Strategy:
 /// - Start from the clicked atom.
 /// - For each neighbor, find the shortest path back to the start
 /// without using the direct edge (which would give a trivial 2-ring).
 /// - The path + start atom forms a candidate ring.
 /// - Return the SMALLEST candidate ring (fewest atoms).
 /// - Optionally require the ring to be approximately planar.
 /// </summary>
    public static class RingFinder
    {
        #region Public API

 /// <summary>
 /// Finds the smallest ring containing the given atom.
 /// </summary>
 /// <param name="molecule">The molecule containing the atoms.</param>
 /// <param name="startAtomIndex">Index of the clicked atom.</param>
 /// <param name="maxRingSize">Maximum allowed ring size (number of atoms).</param>
 /// <param name="requirePlanar">If true, only planar rings are considered.</param>
 /// <param name="planarityTolerance">Max deviation (Å) from the best-fit plane.</param>
 /// <returns>List of atom indices forming the ring, or empty if none found.</returns>
        public static List<int> FindSmallestRing(
            Molecule molecule,
            int startAtomIndex,
            int maxRingSize = 12,
            bool requirePlanar = true,
            float planarityTolerance = 0.15f)
        {
            if (molecule == null || molecule.atom == null)
                return new List<int>();

            if (startAtomIndex < 0 || startAtomIndex >= molecule.NumberOfAtoms)
                return new List<int>();

            var best = new List<int>();
            var startAtom = molecule.atom[startAtomIndex];
            int neighborCount = startAtom.NrBondedAtoms;

            for (int n = 0; n < neighborCount; n++)
            {
                int neighbor = startAtom.BondedAtom[n];
                if (neighbor < 0 || neighbor >= molecule.NumberOfAtoms) continue;

 // BFS from neighbor back to startAtom, forbidding:
 // - passing through startAtom as an intermediate
 // - using the direct edge neighbor->startAtom (would give a 2-ring)
                var path = ShortestPathAvoiding(
                    molecule,
                    from: neighbor,
                    to: startAtomIndex,
                    avoid: startAtomIndex,
                    maxDepth: maxRingSize - 1);

                if (path == null || path.Count < 3) continue;

 // path already starts at neighbor and ends at startAtom;
 // that IS the ring (with startAtom counted once at the end).
                var ring = path;

                if (ring.Count < 3 || ring.Count > maxRingSize) continue;

                if (!IsValidRing(ring)) continue;

                if (requirePlanar && !IsPlanar(molecule, ring, planarityTolerance))
                    continue;

                if (best.Count == 0 || ring.Count < best.Count)
                {
                    best = ring;
                }
            }

            return best;
        }

        #endregion

        #region Graph Search

 /// <summary>
 /// BFS shortest path from 'from' to 'to'. Never:
 /// - passes through 'avoid' as an intermediate node
 /// (but 'avoid' may be the target 'to');
 /// - uses the direct edge 'from' -> 'to' as the first step
 /// (this would produce a trivial 2-element path that is not a ring).
 /// Returns the path from 'from' to 'to' inclusive, or null.
 /// </summary>
        private static List<int> ShortestPathAvoiding(
            Molecule molecule,
            int from,
            int to,
            int avoid,
            int maxDepth)
        {
            if (from == to) return new List<int> { from };

            var queue = new Queue<int>();
            var parent = new Dictionary<int, int>();
            var visited = new HashSet<int>();

            queue.Enqueue(from);
            visited.Add(from);
            parent[from] = -1;

            int depth = 0;
            int nodesAtCurrentDepth = 1;
            int nodesAtNextDepth = 0;

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                nodesAtCurrentDepth--;

                if (current == to)
                    return ReconstructPath(parent, from, to);

                var atom = molecule.atom[current];
                for (int i = 0; i < atom.NrBondedAtoms; i++)
                {
                    int next = atom.BondedAtom[i];

 // Skip the avoid node unless it is our target
                    if (next == avoid && next != to) continue;

 // ⭐ KEY FIX: forbid the direct edge from->to as the first step
 // Without this, BFS would immediately return [from, to] (2-ring)
 // and stop, never finding the actual ring.
                    if (current == from && next == to) continue;

                    if (visited.Contains(next)) continue;

                    visited.Add(next);
                    parent[next] = current;
                    queue.Enqueue(next);
                    nodesAtNextDepth++;
                }

                if (nodesAtCurrentDepth == 0)
                {
                    depth++;
                    if (depth > maxDepth) return null;
                    nodesAtCurrentDepth = nodesAtNextDepth;
                    nodesAtNextDepth = 0;
                }
            }

            return null;
        }

        private static List<int> ReconstructPath(Dictionary<int, int> parent, int from, int to)
        {
            var path = new List<int>();
            int node = to;

            while (node != -1)
            {
                path.Add(node);
                if (node == from) break;
                if (!parent.ContainsKey(node)) break;
                node = parent[node];
            }

            path.Reverse();
            return path;
        }

        private static bool IsValidRing(List<int> ring)
        {
            var seen = new HashSet<int>();
            foreach (var idx in ring)
            {
                if (!seen.Add(idx)) return false;
            }
            return true;
        }

        #endregion

        #region Planarity Check

 /// <summary>
 /// Returns true if all atoms of the ring lie close enough to the
 /// best-fit plane (PCA / smallest eigenvalue of the covariance matrix).
 /// </summary>
        private static bool IsPlanar(Molecule molecule, List<int> ring, float tolerance)
        {
            if (ring.Count < 3) return false;

 // Centroid
            Vector3 centroid = Vector3.Zero;
            for (int i = 0; i < ring.Count; i++)
                centroid += molecule.atom[ring[i]].Coordinate;
            centroid /= ring.Count;

 // Covariance matrix (symmetric 3x3)
            double[,] cov = new double[3, 3];
            for (int i = 0; i < ring.Count; i++)
            {
                Vector3 d = molecule.atom[ring[i]].Coordinate - centroid;

                cov[0, 0] += d.X * d.X;
                cov[0, 1] += d.X * d.Y;
                cov[0, 2] += d.X * d.Z;

                cov[1, 1] += d.Y * d.Y;
                cov[1, 2] += d.Y * d.Z;

                cov[2, 2] += d.Z * d.Z;
            }
            cov[1, 0] = cov[0, 1];
            cov[2, 0] = cov[0, 2];
            cov[2, 1] = cov[1, 2];

 // Eigendecomaftersition
            double[] eigenvalues;
            double[,] eigenvectors;
            if (!JacobiEigen(cov, out eigenvalues, out eigenvectors))
                return false;

 // Smallest eigenvalue -> plane normal
            int minIdx = 0;
            if (eigenvalues[1] < eigenvalues[minIdx]) minIdx = 1;
            if (eigenvalues[2] < eigenvalues[minIdx]) minIdx = 2;

            Vector3 normal = new Vector3(
                (float)eigenvectors[0, minIdx],
                (float)eigenvectors[1, minIdx],
                (float)eigenvectors[2, minIdx]
            );

            if (normal.LengthSquared() < 1e-9f) return false;
            normal.Normalize();

 // Max perpendicular deviation from the plane
            float maxDev = 0f;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector3 d = molecule.atom[ring[i]].Coordinate - centroid;
                float dev = Math.Abs(Vector3.Dot(d, normal));
                if (dev > maxDev) maxDev = dev;
            }

            return maxDev < tolerance;
        }

 /// <summary>
 /// Local copy of the Jacobi eigendecomaftersition for symmetric 3x3 matrices.
 /// </summary>
        private static bool JacobiEigen(double[,] A, out double[] eigenvalues, out double[,] eigenvectors)
        {
            double[,] a = new double[3, 3];
            Array.Copy(A, a, 9);

            double[,] v = new double[3, 3];
            v[0, 0] = 1; v[1, 1] = 1; v[2, 2] = 1;

            const int MAX_ITER = 50;
            const double EPSILON = 1e-12;

            for (int iter = 0; iter < MAX_ITER; iter++)
            {
                double off01 = Math.Abs(a[0, 1]);
                double off02 = Math.Abs(a[0, 2]);
                double off12 = Math.Abs(a[1, 2]);

                double maxOff = off01;
                if (off02 > maxOff) maxOff = off02;
                if (off12 > maxOff) maxOff = off12;

                if (maxOff < EPSILON) break;

                int p, q;
                if (off01 >= off02 && off01 >= off12) { p = 0; q = 1; }
                else if (off02 >= off01 && off02 >= off12) { p = 0; q = 2; }
                else { p = 1; q = 2; }

                double app = a[p, p];
                double aqq = a[q, q];
                double apq = a[p, q];

                double theta = 0.5 * Math.Atan2(2.0 * apq, aqq - app);
                double c = Math.Cos(theta);
                double s = Math.Sin(theta);

                double[,] newA = new double[3, 3];
                Array.Copy(a, newA, 9);

                newA[p, p] = c * c * app - 2.0 * s * c * apq + s * s * aqq;
                newA[q, q] = s * s * app + 2.0 * s * c * apq + c * c * aqq;
                newA[p, q] = 0.0;
                newA[q, p] = 0.0;

                int r = 3 - p - q;
                newA[p, r] = c * a[p, r] - s * a[q, r];
                newA[r, p] = newA[p, r];
                newA[q, r] = s * a[p, r] + c * a[q, r];
                newA[r, q] = newA[q, r];

                for (int i = 0; i < 3; i++)
                {
                    double vip = v[i, p];
                    double viq = v[i, q];
                    v[i, p] = c * vip - s * viq;
                    v[i, q] = s * vip + c * viq;
                }

                a = newA;
            }

            eigenvalues = new double[3];
            eigenvalues[0] = a[0, 0];
            eigenvalues[1] = a[1, 1];
            eigenvalues[2] = a[2, 2];

            eigenvectors = v;
            return true;
        }

        #endregion
    }
}