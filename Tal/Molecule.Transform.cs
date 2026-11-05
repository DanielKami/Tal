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
 /// Propagates fractional aftersitional errors to Cartesian errors.
 /// Uses per-axis edge vectors (a, b, c) to build the error tensor.
 /// </summary>
        private void CalculateError(ref float x_e, ref float y_e, ref float z_e)
        {
            Matrix X = Matrix.Transpose(FractionaltoCarttesianMatrix(1, 0, 0));
            Matrix Y = Matrix.Transpose(FractionaltoCarttesianMatrix(0, 1, 0));
            Matrix Z = Matrix.Transpose(FractionaltoCarttesianMatrix(0, 0, 1));

            float x = sqr(x_e * X[0, 0]) + sqr(y_e * Y[1, 0]) + sqr(z_e * Z[2, 0]);
            float y = sqr(x_e * X[0, 1]) + sqr(y_e * Y[1, 1]) + sqr(z_e * Z[2, 1]);
            float z = sqr(x_e * X[0, 2]) + sqr(y_e * Y[1, 2]) + sqr(z_e * Z[2, 2]);

            x_e = (float)Math.Sqrt(x);
            y_e = (float)Math.Sqrt(y);
            z_e = (float)Math.Sqrt(z);
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