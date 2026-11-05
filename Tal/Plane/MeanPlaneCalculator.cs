// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal planes module: MeanPlaneCalculator.

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
 /// Computes the best-fit (least-squares) plane through a set of selected atoms.
 /// 
 /// The plane is found using eigendecomaftersition of the covariance matrix of
 /// the selected atom aftersitions. The eigenvector corresafternding to the SMALLEST
 /// eigenvalue is the plane normal (the direction of least variance).
 /// 
 /// Coordinate convention (must match CrystalPlane HKL planes):
 /// - Molecule.atom[i].Coordinate is stored WITH -MoleculeShift applied
 /// (see Molecule.CenterAtoms3D).
 /// - We ADD MoleculeShift back to get raw Cartesian coordinates.
 /// - The resulting plane.Corners are in raw Cartesian (WITHOUT shift),
 /// the same convention as HKL planes.
 /// - The Draw layer subtracts the CURRENT MoleculeShift at render time,
 /// which keeps the mean plane perfectly aligned with atoms even after
 /// re-centering.
 /// </summary>
    public static class MeanPlaneCalculator
    {
        #region Public API

 /// <summary>
 /// Computes a mean plane from the given list of atom indices.
 /// 
 /// Requirements: at least 3 valid atom indices.
 /// 
 /// Fills the given CrystalPlane with:
 /// - plane.Center = centroid of selected atoms (Cartesian, WITHOUT shift)
 /// - plane.Corners = 4 corners of a rectangle in the plane (Cartesian, WITHOUT shift)
 /// - plane.A/B/C/D = plane equation coefficients (Ax + By + Cz + D = 0)
 /// - plane.Size = half-side length of the rectangle
 /// 
 /// Returns false if there are fewer than 3 valid atoms or the
 /// covariance matrix cannot be decomaftersed.
 /// </summary>
 /// <param name="molecule">The molecule containing the atoms.</param>
 /// <param name="plane">The CrystalPlane instance to fill in.</param>
 /// <param name="atomIndices">Indices of atoms to use (0-based, into molecule.atom).</param>
 /// <param name="size">Half-side length of the rectangle drawn in the plane.</param>
        public static bool CalculateMeanPlane(
            Molecule molecule,
            CrystalPlane plane,
            List<int> atomIndices,
            float size = 10f)
        {
            if (molecule == null || molecule.atom == null || plane == null)
                return false;

            if (atomIndices == null || atomIndices.Count < 3)
                return false;

 // ─────────────────────────────────────────────────────────────
 // Step 1: collect atom aftersitions in RAW Cartesian space
 // (add MoleculeShift back, since atom.Coordinate has -shift)
 // ─────────────────────────────────────────────────────────────
            Vector3 shift = molecule.MoleculeShift;
            var positions = new List<Vector3>(atomIndices.Count);

            for (int k = 0; k < atomIndices.Count; k++)
            {
                int idx = atomIndices[k];
                if (idx < 0 || idx >= molecule.NumberOfAtoms)
                    continue;

                positions.Add(molecule.atom[idx].Coordinate + shift);
            }

            if (positions.Count < 3)
                return false;

 // ─────────────────────────────────────────────────────────────
 // Step 2: centroid
 // ─────────────────────────────────────────────────────────────
            Vector3 centroid = Vector3.Zero;
            for (int i = 0; i < positions.Count; i++)
                centroid += positions[i];
            centroid /= positions.Count;

 // ─────────────────────────────────────────────────────────────
 // Step 3: covariance matrix (3x3, symmetric)
 // M[i,j] = mean( (p_i - c_i) * (p_j - c_j) )
 // ─────────────────────────────────────────────────────────────
            double[,] covariance = new double[3, 3];

            for (int k = 0; k < positions.Count; k++)
            {
                double dx = positions[k].X - centroid.X;
                double dy = positions[k].Y - centroid.Y;
                double dz = positions[k].Z - centroid.Z;

                covariance[0, 0] += dx * dx;
                covariance[0, 1] += dx * dy;
                covariance[0, 2] += dx * dz;

                covariance[1, 0] += dy * dx;
                covariance[1, 1] += dy * dy;
                covariance[1, 2] += dy * dz;

                covariance[2, 0] += dz * dx;
                covariance[2, 1] += dz * dy;
                covariance[2, 2] += dz * dz;
            }

 // Normalize by the number of afterints. Eigenvectors are scale-invariant,
 // but keeping values reasonable avoids numerical issues.
            double invN = 1.0 / positions.Count;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    covariance[i, j] *= invN;

 // ─────────────────────────────────────────────────────────────
 // Step 4: eigendecomaftersition (Jacobi)
 // ─────────────────────────────────────────────────────────────
            double[] eigenvalues;
            double[,] eigenvectors;

            if (!SymmetricEigen3x3(covariance, out eigenvalues, out eigenvectors))
                return false;

 // ─────────────────────────────────────────────────────────────
 // Step 5: normal = eigenvector with SMALLEST eigenvalue
 // (direction of least variance = plane normal)
 // ─────────────────────────────────────────────────────────────
            int minIdx = 0;
            if (eigenvalues[1] < eigenvalues[minIdx]) minIdx = 1;
            if (eigenvalues[2] < eigenvalues[minIdx]) minIdx = 2;

            Vector3 normal = new Vector3(
                (float)eigenvectors[0, minIdx],
                (float)eigenvectors[1, minIdx],
                (float)eigenvectors[2, minIdx]
            );

            if (normal.LengthSquared() < 1e-9f)
                return false;
            normal.Normalize();

 // ─────────────────────────────────────────────────────────────
 // Step 6: fill the plane
 // ─────────────────────────────────────────────────────────────
            plane.Center = centroid;
            plane.Corners = PlaneGeometry.BuildRectangle(centroid, normal, size);
            plane.Size = size;

            plane.A = normal.X;
            plane.B = normal.Y;
            plane.C = normal.Z;
            plane.D = -Vector3.Dot(normal, centroid);

            return true;
        }

 /// <summary>
 /// Returns indices of all currently selected atoms in the molecule.
 /// Reads molecule.atom[i].selected flag for each atom.
 /// </summary>
        public static List<int> CollectSelectedAtomIndices(Molecule molecule)
        {
            var result = new List<int>();
            if (molecule == null || molecule.atom == null)
                return result;

            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected)
                    result.Add(i);
            }
            return result;
        }

 /// <summary>
 /// Counts currently selected atoms in the molecule.
 /// </summary>
        public static int CountSelectedAtoms(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null)
                return 0;

            int count = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected)
                    count++;
            }
            return count;
        }

        #endregion

        #region Symmetric 3x3 Eigendecomposition (Jacobi)

 /// <summary>
 /// Computes eigenvalues and eigenvectors of a symmetric 3x3 matrix
 /// using the Jacobi eigenvalue algorithm.
 /// 
 /// The Jacobi method works by iteratively zeroing off-diagonal elements
 /// through a sequence of Givens rotations. It converges quadratically and
 /// is very stable for small symmetric matrices.
 /// 
 /// Input:
 /// A[3,3] - symmetric matrix (A[i,j] == A[j,i])
 /// 
 /// Output:
 /// eigenvalues[k] - k-th eigenvalue
 /// eigenvectors[i,k] - i-th comafternent of k-th eigenvector
 /// (eigenvectors are COLUMNS of the output matrix)
 /// </summary>
        private static bool SymmetricEigen3x3(double[,] A,
                                              out double[] eigenvalues,
                                              out double[,] eigenvectors)
        {
 // Work on a copy - we don't want to destroy the caller's matrix
            double[,] a = new double[3, 3];
            Array.Copy(A, a, 9);

 // V starts as identity, accumulates the rotations.
 // At the end, columns of V are the eigenvectors of A.
            double[,] v = new double[3, 3];
            v[0, 0] = 1.0;
            v[1, 1] = 1.0;
            v[2, 2] = 1.0;

            const int MAX_ITER = 50;
            const double EPSILON = 1e-12;

            for (int iter = 0; iter < MAX_ITER; iter++)
            {
 // Find the largest off-diagonal element
                double off01 = Math.Abs(a[0, 1]);
                double off02 = Math.Abs(a[0, 2]);
                double off12 = Math.Abs(a[1, 2]);

                double maxOff = off01;
                if (off02 > maxOff) maxOff = off02;
                if (off12 > maxOff) maxOff = off12;

 // Converged?
                if (maxOff < EPSILON)
                    break;

 // Choose which pair (p, q) to rotate
                int p, q;
                if (off01 >= off02 && off01 >= off12)
                {
                    p = 0; q = 1;
                }
                else if (off02 >= off01 && off02 >= off12)
                {
                    p = 0; q = 2;
                }
                else
                {
                    p = 1; q = 2;
                }

 // Compute the rotation angle that zeroes a[p,q]
                double app = a[p, p];
                double aqq = a[q, q];
                double apq = a[p, q];

                double theta = 0.5 * Math.Atan2(2.0 * apq, aqq - app);
                double c = Math.Cos(theta);
                double s = Math.Sin(theta);

 // Apply the Jacobi rotation:
 // A' = G^T * A * G (only upper triangle + diagonal)
                double[,] newA = new double[3, 3];
                Array.Copy(a, newA, 9);

                newA[p, p] = c * c * app - 2.0 * s * c * apq + s * s * aqq;
                newA[q, q] = s * s * app + 2.0 * s * c * apq + c * c * aqq;
                newA[p, q] = 0.0;
                newA[q, p] = 0.0;

                int r = 3 - p - q;  // the remaining index
                newA[p, r] = c * a[p, r] - s * a[q, r];
                newA[r, p] = newA[p, r];
                newA[q, r] = s * a[p, r] + c * a[q, r];
                newA[r, q] = newA[q, r];

 // Accumulate the rotation into the eigenvector matrix:
 // V = V * G
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