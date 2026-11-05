// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Bond order + aromatic ring detection for Display → Bond Display.

using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;

namespace Tal.Calculate
{
    /// <summary>
    /// Assigns bond orders (1/2/3/aromatic) from distances and finds
    /// 5-/6-membered aromatic rings for the dashed-ring overlay.
    /// </summary>
    public static class BondOrderAnalyzer
    {
        /// <summary>
        /// Typical single-bond length (Å) for common organic pairs.
        /// Used only as a reference for order classification.
        /// </summary>
        private static float TypicalSingle(int z1, int z2)
        {
            if (z1 > z2) { int t = z1; z1 = z2; z2 = t; }
            // C=6, N=7, O=8, S=16
            if (z1 == 6 && z2 == 6) return 1.54f;
            if (z1 == 6 && z2 == 7) return 1.47f;
            if (z1 == 6 && z2 == 8) return 1.43f;
            if (z1 == 6 && z2 == 16) return 1.82f;
            if (z1 == 7 && z2 == 7) return 1.45f;
            if (z1 == 7 && z2 == 8) return 1.40f;
            if (z1 == 8 && z2 == 8) return 1.48f;
            // Fallback: sum of covalent radii approx
            return 1.50f;
        }

        /// <summary>
        /// Hybridization from heavy-atom coordination (H counted).
        /// sp3 = 4 bonds (no multiples), sp2 = 3 (one double), sp = 2 (triple or two doubles).
        /// </summary>
        private static int Hybridization(Molecule molecule, int atomIndex)
        {
            int z = molecule.atom[atomIndex].AtomicNumber;
            int nb = molecule.atom[atomIndex].NrBondedAtoms;
            // Only C, N, O get hybridization-based restriction
            if (z != 6 && z != 7 && z != 8) return 0; // unrestricted
            if (nb >= 4) return 3; // sp3
            if (nb == 3) return 2; // sp2
            if (nb == 2) return 1; // sp
            return 0;
        }

        /// <summary>
        /// Typical double-bond length for common pairs.
        /// </summary>
        private static float TypicalDouble(int z1, int z2)
        {
            if (z1 > z2) { int t = z1; z1 = z2; z2 = t; }
            if (z1 == 6 && z2 == 6) return 1.34f; // C=C
            if (z1 == 6 && z2 == 7) return 1.28f; // C=N
            if (z1 == 6 && z2 == 8) return 1.22f; // C=O
            if (z1 == 6 && z2 == 16) return 1.60f; // C=S
            if (z1 == 7 && z2 == 7) return 1.25f; // N=N
            if (z1 == 7 && z2 == 8) return 1.21f; // N=O
            return 1.30f;
        }

        /// <summary>
        /// Typical triple-bond length for common pairs.
        /// </summary>
        private static float TypicalTriple(int z1, int z2)
        {
            if (z1 > z2) { int t = z1; z1 = z2; z2 = t; }
            if (z1 == 6 && z2 == 6) return 1.20f; // C≡C
            if (z1 == 6 && z2 == 7) return 1.16f; // C≡N
            if (z1 == 7 && z2 == 7) return 1.10f; // N≡N
            return 1.15f;
        }

        /// <summary>
        /// Classify bond order using hybridization + distance.
        /// sp3 → always single. sp2 → at most double. sp → at most triple.
        /// </summary>
        private static int ClassifyOrder(Molecule molecule, int i, int j, float distance)
        {
            if (distance <= 0.1f) return 1;
            int z1 = molecule.atom[i].AtomicNumber;
            int z2 = molecule.atom[j].AtomicNumber;

            int hyb1 = Hybridization(molecule, i);
            int hyb2 = Hybridization(molecule, j);

            // sp3 on either side → single only
            if (hyb1 == 3 || hyb2 == 3) return 1;

            float triple = TypicalTriple(z1, z2);
            float dbl = TypicalDouble(z1, z2);

            // Triple only if both are sp (or unrestricted) and distance matches
            bool canTriple = (hyb1 <= 1 && hyb2 <= 1);
            if (canTriple && distance < triple + 0.06f && distance > triple - 0.08f)
                return 3;

            // Double if distance matches and hybridization allows
            bool canDouble = (hyb1 <= 2 && hyb2 <= 2);
            if (canDouble && distance < dbl + 0.08f && distance > dbl - 0.10f)
                return 2;

            return 1;
        }

        /// <summary>
        /// Run detection and write BondOrder[] + AromaticRings.
        /// </summary>
        public static void Analyze(Molecule molecule)
        {
            if (molecule?.atom == null || molecule.NumberOfAtoms == 0)
            {
                molecule?.AromaticRings?.Clear();
                return;
            }

            int n = molecule.NumberOfAtoms;

            // Allocate BondOrder
            for (int i = 0; i < n; i++)
            {
                int nb = molecule.atom[i].NrBondedAtoms;
                if (molecule.atom[i].BondOrder == null || molecule.atom[i].BondOrder.Length < nb)
                    molecule.atom[i].BondOrder = new int[System.Math.Max(nb, 4)];
                for (int k = 0; k < nb; k++)
                    molecule.atom[i].BondOrder[k] = 1;
            }

            // Distance-based order
            for (int i = 0; i < n; i++)
            {
                int nb = molecule.atom[i].NrBondedAtoms;
                int zi = molecule.atom[i].AtomicNumber;
                for (int k = 0; k < nb; k++)
                {
                    int j = molecule.atom[i].BondedAtom[k];
                    if (j < 0 || j >= n) continue;
                    float d = molecule.atom[i].Distance[k];
                    int order = ClassifyOrder(molecule, i, j, d);
                    molecule.atom[i].BondOrder[k] = order;
                }
            }

            // Aromatic rings
            var rings = FindAromaticRings(molecule);
            molecule.AromaticRings = rings;

            // Mark bonds that belong to an aromatic ring as order 4
            foreach (var ring in rings)
            {
                for (int a = 0; a < ring.AtomIndices.Count; a++)
                {
                    int i = ring.AtomIndices[a];
                    int j = ring.AtomIndices[(a + 1) % ring.AtomIndices.Count];
                    MarkBondAromatic(molecule, i, j);
                    MarkBondAromatic(molecule, j, i);
                }
            }

            Debug.WriteLine($"[BondOrder] {n} atoms, {rings.Count} aromatic rings");
        }

        private static void MarkBondAromatic(Molecule molecule, int from, int to)
        {
            int nb = molecule.atom[from].NrBondedAtoms;
            if (molecule.atom[from].BondedAtom == null) return;
            for (int k = 0; k < nb; k++)
            {
                if (molecule.atom[from].BondedAtom[k] == to)
                {
                    if (molecule.atom[from].BondOrder != null && k < molecule.atom[from].BondOrder.Length)
                        molecule.atom[from].BondOrder[k] = 4;
                    return;
                }
            }
        }

        /// <summary>
        /// Find 5- and 6-membered rings of C/N (optionally O/S) whose bonds
        /// are all in the aromatic distance window.
        /// </summary>
        private static List<Molecule.AromaticRingInfo> FindAromaticRings(Molecule molecule)
        {
            var result = new List<Molecule.AromaticRingInfo>();
            int n = molecule.NumberOfAtoms;
            var seen = new HashSet<string>();

            for (int start = 0; start < n; start++)
            {
                int z = molecule.atom[start].AtomicNumber;
                if (z != 6 && z != 7) continue; // start from C or N

                // DFS for cycles of length 5 or 6
                var path = new List<int> { start };
                var pathSet = new HashSet<int> { start };
                DfsCycles(molecule, start, start, 1, path, pathSet, seen, result);
            }

            return result;
        }

        private static void DfsCycles(
            Molecule molecule, int start, int current, int depth,
            List<int> path, HashSet<int> pathSet,
            HashSet<string> seen, List<Molecule.AromaticRingInfo> result)
        {
            if (depth > 6) return;

            int nb = molecule.atom[current].NrBondedAtoms;
            if (molecule.atom[current].BondedAtom == null) return;

            for (int k = 0; k < nb; k++)
            {
                int next = molecule.atom[current].BondedAtom[k];
                if (next < 0 || next >= molecule.NumberOfAtoms) continue;

                // Close cycle
                if (next == start && (depth == 5 || depth == 6))
                {
                    var ring = new List<int>(path);
                    if (IsAromaticRing(molecule, ring))
                    {
                        string key = CanonicalKey(ring);
                        if (seen.Add(key))
                        {
                            var info = BuildRingInfo(molecule, ring);
                            result.Add(info);
                        }
                    }
                    continue;
                }

                if (pathSet.Contains(next)) continue;
                int nz = molecule.atom[next].AtomicNumber;
                if (nz != 6 && nz != 7 && nz != 8 && nz != 16) continue;

                path.Add(next);
                pathSet.Add(next);
                DfsCycles(molecule, start, next, depth + 1, path, pathSet, seen, result);
                path.RemoveAt(path.Count - 1);
                pathSet.Remove(next);
            }
        }

        private static string CanonicalKey(List<int> ring)
        {
            // Normalize so each ring is counted once
            int min = ring[0];
            int minIdx = 0;
            for (int i = 1; i < ring.Count; i++)
                if (ring[i] < min) { min = ring[i]; minIdx = i; }

            var a = new List<int>();
            var b = new List<int>();
            for (int i = 0; i < ring.Count; i++)
            {
                a.Add(ring[(minIdx + i) % ring.Count]);
                b.Add(ring[(minIdx - i + ring.Count) % ring.Count]);
            }
            var use = a;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] < b[i]) { use = a; break; }
                if (b[i] < a[i]) { use = b; break; }
            }
            return string.Join(",", use);
        }

        private static bool IsAromaticRing(Molecule molecule, List<int> ring)
        {
            // All bonds in aromatic distance window
            for (int a = 0; a < ring.Count; a++)
            {
                int i = ring[a];
                int j = ring[(a + 1) % ring.Count];
                float d = DistanceBetween(molecule, i, j);
                if (d < 1.20f || d > 1.50f) return false;
            }
            return true;
        }

        private static float DistanceBetween(Molecule molecule, int i, int j)
        {
            int nb = molecule.atom[i].NrBondedAtoms;
            for (int k = 0; k < nb; k++)
                if (molecule.atom[i].BondedAtom[k] == j)
                    return molecule.atom[i].Distance[k];
            return Vector3.Distance(molecule.atom[i].Coordinate, molecule.atom[j].Coordinate);
        }

        private static Molecule.AromaticRingInfo BuildRingInfo(Molecule molecule, List<int> ring)
        {
            Vector3 center = Vector3.Zero;
            foreach (int idx in ring)
                center += molecule.atom[idx].Coordinate;
            center /= ring.Count;

            // Normal from two edges
            Vector3 p0 = molecule.atom[ring[0]].Coordinate;
            Vector3 p1 = molecule.atom[ring[1]].Coordinate;
            Vector3 p2 = molecule.atom[ring[2]].Coordinate;
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
            if (normal.LengthSquared() > 1e-8f)
                normal.Normalize();
            else
                normal = Vector3.UnitZ;

            float radius = 0f;
            foreach (int idx in ring)
                radius += Vector3.Distance(center, molecule.atom[idx].Coordinate);
            radius /= ring.Count;
            // Inner ring ~65% of atom radius
            radius *= 0.65f;

            return new Molecule.AromaticRingInfo
            {
                AtomIndices = ring,
                Center = center,
                Normal = normal,
                Radius = radius
            };
        }
    }
}
