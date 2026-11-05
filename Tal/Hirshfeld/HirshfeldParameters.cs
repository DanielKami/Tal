// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldParameters.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HirshfeldParameters v4 — increased CUT_OFF for better neighbour collection.

using System;
using Microsoft.Xna.Framework;
using Tal.Hirshfeld.Sto;

namespace Tal.Hirshfeld
{
    #region AtomStoParams

 /// <summary>
 /// ⭐ Parametry density STO for atom (referencja do LUT).
 /// Used when we have the STO database (sto_atoms_neutral.json).
 /// </summary>
    public readonly struct AtomStoParams
    {
        public readonly StoDensityLut Lut;
        public readonly Vector3 Position;
        public readonly string Symbol;
        public readonly float CutoffSq;

        public AtomStoParams(StoDensityLut lut, Vector3 position, string symbol)
        {
            Lut = lut;
            Position = position;
            Symbol = symbol;

            float rMax = lut?.MaxRadius ?? 10f;
            CutoffSq = rMax * rMax;
        }

        public float Evaluate(Vector3 delta)
        {
            if (Lut == null) return 0f;
            float d2 = delta.LengthSquared();
            if (d2 > CutoffSq) return 0f;
            return Lut.SampleSq(d2);
        }

        public float Evaluate(float dx, float dy, float dz)
        {
            if (Lut == null) return 0f;
            float d2 = dx * dx + dy * dy + dz * dz;
            if (d2 > CutoffSq) return 0f;
            return Lut.SampleSq(d2);
        }

 /// <summary>Najszybsza version — when znamy already d².</summary>
        public float EvaluateD2(float d2)
        {
            if (Lut == null) return 0f;
            if (d2 > CutoffSq) return 0f;
            return Lut.SampleSq(d2);
        }
    }

    #endregion

    #region AtomGaussParams

 /// <summary>
 /// Parametry gaussowskie density elektronewj atom.
 /// </summary>
    public readonly struct AtomGaussParams
    {
        public readonly bool IsAnisotropic;
        public readonly int AtomicNumber;
        public readonly int OriginalAtomIndex;

        public readonly float NormFactor;
        public readonly float InvSigmaSq;

        public readonly Vector3 E1, E2, E3;
        public readonly float InvA1Sq, InvA2Sq, InvA3Sq;

        public readonly float CutoffRadius;
        public readonly Vector3 Position;

        public AtomGaussParams(
            bool isAnisotropic,
            int atomicNumber,
            int originalAtomIndex,
            float normFactor,
            float invSigmaSq,
            Vector3 e1,
            Vector3 e2,
            Vector3 e3,
            float invA1Sq,
            float invA2Sq,
            float invA3Sq,
            float cutoffRadius,
            Vector3 position)
        {
            IsAnisotropic = isAnisotropic;
            AtomicNumber = atomicNumber;
            OriginalAtomIndex = originalAtomIndex;
            NormFactor = normFactor;
            InvSigmaSq = invSigmaSq;
            E1 = e1;
            E2 = e2;
            E3 = e3;
            InvA1Sq = invA1Sq;
            InvA2Sq = invA2Sq;
            InvA3Sq = invA3Sq;
            CutoffRadius = cutoffRadius;
            Position = position;
        }

        public float Evaluate(Vector3 delta)
        {
            if (!IsAnisotropic)
            {
                float d2 = delta.LengthSquared();
                if (d2 > CutoffRadius * CutoffRadius) return 0f;
                return NormFactor * HirshfeldExpLut.FastExpNeg(0.5f * d2 * InvSigmaSq);
            }
            else
            {
                float d1 = Vector3.Dot(delta, E1);
                float d2 = Vector3.Dot(delta, E2);
                float d3 = Vector3.Dot(delta, E3);
                float quad = d1 * d1 * InvA1Sq + d2 * d2 * InvA2Sq + d3 * d3 * InvA3Sq;
                return NormFactor * HirshfeldExpLut.FastExpNeg(0.5f * quad);
            }
        }

        public float Evaluate(float dx, float dy, float dz)
        {
            if (!IsAnisotropic)
            {
                float d2 = dx * dx + dy * dy + dz * dz;
                if (d2 > CutoffRadius * CutoffRadius) return 0f;
                return NormFactor * HirshfeldExpLut.FastExpNeg(0.5f * d2 * InvSigmaSq);
            }
            else
            {
                float d1 = dx * E1.X + dy * E1.Y + dz * E1.Z;
                float d2 = dx * E2.X + dy * E2.Y + dz * E2.Z;
                float d3 = dx * E3.X + dy * E3.Y + dz * E3.Z;
                float quad = d1 * d1 * InvA1Sq + d2 * d2 * InvA2Sq + d3 * d3 * InvA3Sq;
                return NormFactor * HirshfeldExpLut.FastExpNeg(0.5f * quad);
            }
        }
    }

    #endregion

 /// <summary>
 /// Builder of Gaussian parameters from atoms.
 /// </summary>
    public static class HirshfeldParameters
    {
 // ═══════════════════════════════════════════════════════════════
 // KONFIGURACJA
 // ═══════════════════════════════════════════════════════════════

 /// <summary>
 /// ⭐ Cutoff for zbierania neighbours (outer atoms).
 ///
 /// v4: 10 → 15 Å.
 /// Increased so that more outer atoms enter rho_outer
 /// → better discrimination of w(r) at contact sites
 /// → ostrzejsza surface Hirshfelda.
 ///
 /// Also used as a cutoff for Gaussians (cutoff = CUT_OFF * sigma).
 /// </summary>
        public const float CUT_OFF = 15f;

 // ═══════════════════════════════════════════════════════════════
 // BUILD
 // ═══════════════════════════════════════════════════════════════

        #region BuildFromAtom

        public static AtomGaussParams BuildFromAtom(
            in Molecule.Atom atom,
            Vector3 cartesianPosition,
            int originalAtomIndex,
            bool useAnisotropic,
            float sigmaMultiplier)
        {
            int z = atom.AtomicNumber;
            if (z <= 0) z = 6;

            float zEff = HirshfeldAtomicRadii.ZEffective(z);
            float sigmaBase = HirshfeldAtomicRadii.GetSigma(z) * sigmaMultiplier;

            if (!useAnisotropic || !atom.isAnisotropic)
            {
                return BuildIsotropic(
                    zEff, sigmaBase, cartesianPosition, z, originalAtomIndex);
            }

            return BuildAnisotropicFromMatrix(
                atom.Un, zEff, sigmaBase, cartesianPosition, z, originalAtomIndex);
        }

        public static AtomGaussParams BuildFromAtomSimple(
            in Molecule.AtomSimple atom,
            Vector3 cartesianPosition,
            int originalAtomIndex,
            bool useAnisotropic,
            float sigmaMultiplier)
        {
            int z = atom.AtomicNumber;
            if (z <= 0) z = 6;

            float zEff = HirshfeldAtomicRadii.ZEffective(z);
            float sigmaBase = HirshfeldAtomicRadii.GetSigma(z) * sigmaMultiplier;

            if (!useAnisotropic || !atom.isAnisotropic)
            {
                return BuildIsotropic(
                    zEff, sigmaBase, cartesianPosition, z, originalAtomIndex);
            }

            return BuildAnisotropicFromMatrix(
                atom.Un, zEff, sigmaBase, cartesianPosition, z, originalAtomIndex);
        }

        #endregion

        #region Private Builders

        private static AtomGaussParams BuildIsotropic(
            float zEff, float sigma, Vector3 position,
            int atomicNumber, int originalAtomIndex)
        {
            if (sigma < 1e-4f) sigma = 1e-4f;

            float norm = (float)(zEff / Math.Pow(2.0 * Math.PI, 1.5) / (sigma * sigma * sigma));
            float invSigmaSq = 1.0f / (sigma * sigma);
            float cutoff = CUT_OFF * sigma;

            return new AtomGaussParams(
                isAnisotropic: false,
                atomicNumber: atomicNumber,
                originalAtomIndex: originalAtomIndex,
                normFactor: norm,
                invSigmaSq: invSigmaSq,
                e1: Vector3.UnitX,
                e2: Vector3.UnitY,
                e3: Vector3.UnitZ,
                invA1Sq: invSigmaSq,
                invA2Sq: invSigmaSq,
                invA3Sq: invSigmaSq,
                cutoffRadius: cutoff,
                position: position);
        }

        private static AtomGaussParams BuildAnisotropicFromMatrix(
            Matrix un, float zEff, float sigmaE, Vector3 position,
            int atomicNumber, int originalAtomIndex)
        {
            Vector3 r0 = new Vector3(un.M11, un.M12, un.M13);
            Vector3 r1 = new Vector3(un.M21, un.M22, un.M23);
            Vector3 r2 = new Vector3(un.M31, un.M32, un.M33);

            float a1Adp = r0.Length();
            float a2Adp = r1.Length();
            float a3Adp = r2.Length();

            if (r0.LengthSquared() < 1e-12f) r0 = Vector3.UnitX;
            else r0 = Vector3.Normalize(r0);

            if (r1.LengthSquared() < 1e-12f) r1 = Vector3.UnitY;
            else
            {
                r1 = r1 - Vector3.Dot(r1, r0) * r0;
                r1 = r1.LengthSquared() < 1e-12f ? Vector3.UnitY : Vector3.Normalize(r1);
            }

            if (r2.LengthSquared() < 1e-12f) r2 = Vector3.UnitZ;
            else
            {
                r2 = r2 - Vector3.Dot(r2, r0) * r0 - Vector3.Dot(r2, r1) * r1;
                r2 = r2.LengthSquared() < 1e-12f ? Vector3.UnitZ : Vector3.Normalize(r2);
            }

            float a1 = MathF.Sqrt(a1Adp * a1Adp + sigmaE * sigmaE);
            float a2 = MathF.Sqrt(a2Adp * a2Adp + sigmaE * sigmaE);
            float a3 = MathF.Sqrt(a3Adp * a3Adp + sigmaE * sigmaE);

            if (a1 < 1e-4f) a1 = 1e-4f;
            if (a2 < 1e-4f) a2 = 1e-4f;
            if (a3 < 1e-4f) a3 = 1e-4f;

            float norm = (float)(zEff / Math.Pow(2.0 * Math.PI, 1.5) / (a1 * a2 * a3));
            float cutoff = CUT_OFF * MathF.Max(a1, MathF.Max(a2, a3));

            return new AtomGaussParams(
                isAnisotropic: true,
                atomicNumber: atomicNumber,
                originalAtomIndex: originalAtomIndex,
                normFactor: norm,
                invSigmaSq: 0f,
                e1: r0, e2: r1, e3: r2,
                invA1Sq: 1.0f / (a1 * a1),
                invA2Sq: 1.0f / (a2 * a2),
                invA3Sq: 1.0f / (a3 * a3),
                cutoffRadius: cutoff,
                position: position);
        }

        #endregion
    }
}