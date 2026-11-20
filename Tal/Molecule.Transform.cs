// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — coordinate transforms.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — matrixe transformation krystalograficznych:
// - konversion frakcjonalne ↔ Cartesian
// - transformation matrixy U (Ustat → U)
// - propagation of atom position errors
// - access do per-atomowych matrixy Ue / Un

using Microsoft.Xna.Framework;

namespace Tal
{
    public partial class Molecule
    {
        #region Fractional ↔ Cartesian

 /// <summary>
 /// Builds the fractional-to-Cartesian conversion matrix.
 /// Uses cell parameters; the <paramref name="a_"/>, <paramref name="b_"/>,
 /// <paramref name="c_"/> parameters override the stored cell edges
 /// when non-zero (used to construct per-axis edge vectors).
 /// </summary>
        public Matrix FractionaltoCarttesianMatrix(float a_, float b_, float c_)
        {
            if (a_ == 0) a_ = a;
            if (b_ == 0) b_ = b;
            if (c_ == 0) c_ = c;

            double aStar, bStar, cStar, sinAlpStar, cosAlpStar, sinBetStar, cosBetStar, sinGamStar, cosGamStar;

            double sinAlp, sinBet, sinGam;
            double cosAlp, cosBet, cosGam;

            sinAlp = Math.Sin(alpha);
            cosAlp = Math.Cos(alpha);
            sinBet = Math.Sin(beta);
            cosBet = Math.Cos(beta);
            sinGam = Math.Sin(gamma);
            cosGam = Math.Cos(gamma);

            float abc = a_ * b_ * c_;

            float V = (float)(abc * Math.Sqrt(1.0f
                                                            - cosAlp * cosAlp
                                                            - cosBet * cosBet
                                                            - cosGam * cosGam
                                                            + 2.0f * cosAlp * cosBet * cosGam));

            aStar = (float)(b_ * c_ * sinAlp / V);
            bStar = (float)(a_ * c_ * sinBet / V);
            cStar = (float)(a_ * b_ * sinGam / V);

            sinAlpStar = V / (abc * sinBet * sinGam);
            cosAlpStar = (cosBet * cosGam - cosAlp) / (sinBet * sinGam);

            sinBetStar = V / (abc * sinAlp * sinGam);
            cosBetStar = (cosAlp * cosGam - cosBet) / (sinAlp * sinGam);

            sinGamStar = V / (abc * sinAlp * sinBet);
            cosGamStar = (cosAlp * cosBet - cosGam) / (sinAlp * sinBet);

            Matrix U = new Matrix();

            U.M11 = a_;
            U.M12 = (float)(b_ * cosGam);
            U.M13 = (float)(c_ * cosBet);
            U.M14 = 0;

            U.M21 = 0;
            U.M22 = (float)(b_ * sinGam);
            U.M23 = (float)(-c_ * sinBet * cosAlpStar);
            U.M24 = 0;

            U.M31 = 0;
            U.M32 = 0;
            U.M33 = (float)(c * Math.Sqrt(sqr((float)sinBet) - sqr((float)(-sinBet * cosAlpStar))));
            U.M34 = 0;

            U.M41 = 0; U.M42 = 0; U.M43 = 0; U.M44 = 1;
            return U;
        }


 // ⭐═══════════════════════════════════════════════════════════════
 // ⭐ Publiczne konwersje frak ↔ kart
 // Consistent with Calculate_SymmetryAtoms3D (uses matrix transpose).
 // ⭐═══════════════════════════════════════════════════════════════

 /// <summary>
 /// Konversion positions frakcjonalnej → Cartesianj (RAW, without MoleculeShift).
 /// Consistent with Calculate_SymmetryAtoms3D — uses matrix transpose.
 ///
 /// To obtain shifted (as in atom[].Coordinate), subtract MoleculeShift.
 /// </summary>
        public Vector3 FracToCart(Vector3 frac)
        {
            Matrix U = Matrix.Transpose(FractionaltoCarttesianMatrix(0, 0, 0));
            return Vector3.Transform(frac, U);
        }

 /// <summary>
 /// Konversion positions Cartesianj (RAW, without MoleculeShift) → frakcjonalnej.
 /// Inverse of FracToCart.
 ///
 /// If masz shifted (jak w atom[].Coordinate), najpierw add MoleculeShift.
 /// </summary>
        public Vector3 CartToFrac(Vector3 cart)
        {
            Matrix U = Matrix.Transpose(FractionaltoCarttesianMatrix(0, 0, 0));
            Matrix invU = Matrix.Invert(U);
            return Vector3.Transform(cart, invU);
        }

 /// <summary>
 /// Conversion of fractional difference (delta frac) → Cartesian difference (delta cart).
 /// NIE adde translation — uses only part liniowej matrixy.
 /// Wykorzystywane do liczenia distance w FindHeavyNeighbor.
 /// </summary>
        public Vector3 FracDeltaToCartDelta(Vector3 fracDelta)
        {
            Matrix U = Matrix.Transpose(FractionaltoCarttesianMatrix(0, 0, 0));
            return Vector3.TransformNormal(fracDelta, U);
        }

 /// <summary>
 /// Conversion of Cartesian difference (delta cart) → fractional difference (delta frac).
 /// NIE adde translation — uses only part liniowej matrixy.
 /// Wykorzystywane przez HydrogenNormalizer do shifts H without utraty kopii cell.
 /// </summary>
        public Vector3 CartDeltaToFracDelta(Vector3 cartDelta)
        {
            Matrix U = Matrix.Transpose(FractionaltoCarttesianMatrix(0, 0, 0));
            Matrix invU = Matrix.Invert(U);
            return Vector3.TransformNormal(cartDelta, invU);
        }
        #endregion

        #region Ustat → U Matrix

 /// <summary>
 /// Builds the transformation matrix from the U-stat (reciprocal-space)
 /// representation to real-space U.
 /// </summary>
        public Matrix TransformUStatToU()
        {
            double aStar, bStar, cStar, cosAlpStar;

            double sinAlp, sinBet, sinGam;
            double cosAlp, cosBet, cosGam;

            sinAlp = Math.Sin(alpha);
            cosAlp = Math.Cos(alpha);
            sinBet = Math.Sin(beta);
            cosBet = Math.Cos(beta);
            sinGam = Math.Sin(gamma);
            cosGam = Math.Cos(gamma);

            Volume = (float)(a * b * c * Math.Sqrt(1.0f
                                                           - cosAlp * cosAlp
                                                           - cosBet * cosBet
                                                           - cosGam * cosGam
                                                           + 2.0f * cosAlp * cosBet * cosGam));

            aStar = (float)(b * c * sinAlp / Volume);
            bStar = (float)(a * c * sinBet / Volume);
            cStar = (float)(a * b * sinGam / Volume);

            cosAlpStar = (cosBet * cosGam - cosAlp) / (sinBet * sinGam);

            Matrix U = new Matrix();

            U.M11 = (float)aStar;
            U.M12 = 0;
            U.M13 = 0;
            U.M14 = 0;

            U.M21 = 0;
            U.M22 = (float)bStar;
            U.M23 = 0;
            U.M24 = 0;

            U.M31 = 0;
            U.M32 = 0;
            U.M33 = (float)cStar;
            U.M34 = 0;

            U.M41 = 0; U.M42 = 0; U.M43 = 0; U.M44 = 1;
            return U;
        }

        #endregion

        #region Error Propagation

        /// <summary>
        /// Propagates independent fractional coordinate errors (sigma_x, sigma_y, sigma_z)
        /// to per-axis Cartesian errors in quadrature:
        ///   sigma_X^2 = sum_j (A[X,j] * sigma_j)^2
        /// where A is the fractional -> Cartesian matrix (columns = cell vectors a, b, c;
        /// x_cart = A * x_frac, i.e. FracToCart). Components are in Angstrom.
        /// NOTE: correlations between Cartesian axes (off-diagonal covariance) are not kept.
        /// </summary>
        public Vector3 FracErrorToCartError(Vector3 fracErr)
        {
            Matrix A = FractionaltoCarttesianMatrix(0, 0, 0);

            float ex = (float)Math.Sqrt(sqr(A.M11 * fracErr.X) + sqr(A.M12 * fracErr.Y) + sqr(A.M13 * fracErr.Z));
            float ey = (float)Math.Sqrt(sqr(A.M22 * fracErr.Y) + sqr(A.M23 * fracErr.Z));
            float ez = Math.Abs(A.M33 * fracErr.Z);

            return new Vector3(ex, ey, ez);
        }

        private void CalculateError(ref float x_e, ref float y_e, ref float z_e)
        {
            Vector3 cart = FracErrorToCartError(new Vector3(x_e, y_e, z_e));
            x_e = cart.X;
            y_e = cart.Y;
            z_e = cart.Z;
        }

        #endregion

        #region Per-Atom U Matrix Accessors

 /// <summary>
 /// Returns the Un matrix (eigenvectors of U) for the given atom.
 /// </summary>
        public Matrix GetAtomUn(int atomIndex)
        {
            if (atomIndex < 0 || atomIndex >= NumberOfAtoms)
                return Matrix.Identity;
            return atom[atomIndex].Un;
        }

 /// <summary>
 /// Returns the Ue matrix (Cartesian ADP tensor) for the given atom.
 /// </summary>
        public Matrix GetAtomUe(int atomIndex)
        {
            if (atomIndex < 0 || atomIndex >= NumberOfAtoms)
                return Matrix.Identity;
            return atom[atomIndex].Ue;
        }

        #endregion

        #region Small Helpers

        private float sqr(float x) { return x * x; }

        #endregion
    }
}