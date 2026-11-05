// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldProperties.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HirshfeldProperties v3 — fixes for "white patches" on the Hirshfeld surface.
//
// ⭐ v3 CHANGES (bug fixes):
//
// FIX #1 — MIN_DIST_SQ reduced from 1e-6f to 1e-10f.
// Rationale: the mesh vertex is NOT an atom, so "self-distance" does
// not exist. The old threshold 1e-3 Å was rejecting legitimate atoms
// that sit very close to the surface (heavy atoms with narrow Gaussians,
// or vertices near atomic nuclei). When ALL outer atoms were rejected,
// de == 0 and the d_norm term was silently skipped → d_norm ≈ 0 →
// white color on the surface.
//
// FIX #2 — d_norm now ALWAYS accumulates BOTH terms.
// Old code used "if (de > 0 && ...)" which silently skipped a term
// when no atom was found. That produced d_norm ≈ 0 (white) by accident.
// New code uses a fallback (r_vdW * 2) so d_norm is never 0 due to
// missing data.
//
// FIX #3 — d_norm clamp widened from [-1.5, 1.5] to [-3.0, 3.0].
// Old clamp created large flat-colored regions where d_norm exceeded
// 1.5 — visually appearing as "non-physical brightening".
//
// KEPT — SceneAtom-based computation (v2). CartPosition in SceneAtom is
// already in the same coordinate frame as surface.Vertices (no extra
// moleculeShift is added).

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Tal.Hirshfeld
{
    public static class HirshfeldProperties
    {
 /// <summary>
 /// ⭐ FIX #1: Minimum squared distance below which an atom is ignored.
 /// Reduced from 1e-6f (1e-3 Å) to 1e-10f (1e-5 Å).
 ///
 /// The mesh vertex is NOT an atom, so "self-distance" does not exist.
 /// The old 1e-3 Å threshold was rejecting legitimate nearby atoms.
 /// </summary>
        private const float MIN_DIST_SQ = 1e-10f;

 /// <summary>
 /// Computes d_e, d_i and d_norm for all mesh vertices.
 ///
 /// ⭐ v3: d_norm always accumulates both terms. Missing atoms use
 /// a safe fallback (r_vdW * 2) instead of being skipped.
 /// </summary>
 /// <summary>
 /// Computes d_e, d_i and d_norm for all mesh vertices.
 ///
 /// ⭐ v4: d_norm uses soft-min over K nearest atoms (no Voronoi jumps).
 /// d_e / d_i remain pure nearest-neighbor (for fingerprint).
 /// </summary>
 /// <summary>
 /// Computes d_e, d_i, d_norm, charge and hydrophobicity for all mesh vertices.
 ///
 /// ⭐ v4: d_norm uses soft-min over K nearest atoms (no Voronoi jumps).
 /// d_e / d_i remain pure nearest-neighbor (for fingerprint).
 ///
 /// ⭐ v5: Added charge + hydrophobicity via weighted gaussian average.
 /// Charge: from SceneAtom.Charge (from Molecule.Atom.charge).
 /// Hydrophobicity: from SceneAtom.Hydrophobicity.
 /// </summary>
        public static void Compute(
            HirshfeldSurface surface,
            IReadOnlyList<SceneAtom> targetAtoms,
            IReadOnlyList<SceneAtom> outerAtoms,
            Molecule molecule,
            Action<string, float> progress = null,
            System.Threading.CancellationToken ct = default)
        {
            int n = surface.VertexCount;
            if (n == 0) return;

            if (surface.Vertices == null || surface.Vertices.Count != n)
            {
                progress?.Invoke("HirshfeldProperties: invalid surface", 1.0f);
                return;
            }

            int targetCount = targetAtoms?.Count ?? 0;
            int outerCount = outerAtoms?.Count ?? 0;

            if (targetCount == 0 || outerCount == 0)
            {
                progress?.Invoke(
                    $"HirshfeldProperties: target={targetCount}, outer={outerCount} " +
                    "(need both > 0)", 1.0f);

                surface.DE = new float[n];
                surface.DI = new float[n];
                surface.DNorm = new float[n];
                surface.NearestOuterAtom = new int[n];
                surface.NearestInnerAtom = new int[n];
                surface.NearestOuterAtomName = new string[n];
                surface.NearestInnerAtomName = new string[n];
                surface.Charge = new float[n];
                surface.Hydrophobicity = new float[n];
                return;
            }

 // ═══════════════════════════════════════════════════════════════
 // Prepare flat arrays of positions + VdW radii + names
 // ═══════════════════════════════════════════════════════════════
            var targetPos = new (Vector3 pos, float rvdw, string name)[targetCount];
            var outerPos = new (Vector3 pos, float rvdw, string name)[outerCount];

            for (int i = 0; i < targetCount; i++)
            {
                var sa = targetAtoms[i];
                float rvdw = molecule.GetVdwRadius(sa.AtomicNumber);
                targetPos[i] = (sa.CartPosition, rvdw, sa.Name ?? "?");
            }

            for (int i = 0; i < outerCount; i++)
            {
                var sa = outerAtoms[i];
                float rvdw = molecule.GetVdwRadius(sa.AtomicNumber);
                outerPos[i] = (sa.CartPosition, rvdw, sa.Name ?? "?");
            }

 // ═══════════════════════════════════════════════════════════════
 // Alokuj tablice resultowe
 // ═══════════════════════════════════════════════════════════════
            surface.DE = new float[n];
            surface.DI = new float[n];
            surface.DNorm = new float[n];
            surface.NearestOuterAtom = new int[n];
            surface.NearestInnerAtom = new int[n];
            surface.NearestOuterAtomName = new string[n];
            surface.NearestInnerAtomName = new string[n];

            progress?.Invoke(
                $"Mapping d_e/d_i ({n} verts, {targetCount} target, {outerCount} outer)...",
                0.92f);

 // ═══════════════════════════════════════════════════════════════
 // Parametry soft-min for d_norm
 // ═══════════════════════════════════════════════════════════════
            const float MIN_DIST_SQ = 1e-10f;
            const float SOFTMIN_BETA = 8.0f;   // 6–12: higher = closer to hard nearest
            const int K_NEAREST = 8;

            int maxParallel = Math.Max(1, Environment.ProcessorCount - 1);
            var options = new System.Threading.Tasks.ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallel,
                CancellationToken = ct
            };

 // ═══════════════════════════════════════════════════════════════
 // MAIN LOOP — d_e / d_i / d_norm per-vertex
 // ═══════════════════════════════════════════════════════════════
            System.Threading.Tasks.Parallel.For(0, n, options, vi =>
            {
                ct.ThrowIfCancellationRequested();

                Vector3 p = surface.Vertices[vi];

 // ── Nearest OUTER (for d_e + names) + K nearest for soft-min ──
                float bestOuterSq = float.MaxValue;
                int bestOuterIdx = -1;
                float bestOuterR = 1.7f;
                string bestOuterName = "?";

                Span<(float d2, float r)> outerNear = stackalloc (float, float)[K_NEAREST];
                int outerNearCount = 0;

                for (int j = 0; j < outerCount; j++)
                {
                    var (apos, rvdw, aname) = outerPos[j];
                    float d2 = (p - apos).LengthSquared();
                    if (d2 < MIN_DIST_SQ) continue;

                    if (d2 < bestOuterSq)
                    {
                        bestOuterSq = d2;
                        bestOuterIdx = j;
                        bestOuterR = rvdw;
                        bestOuterName = aname;
                    }

                    if (outerNearCount < K_NEAREST)
                    {
                        outerNear[outerNearCount++] = (d2, rvdw);
                        for (int k = outerNearCount - 1; k > 0; k--)
                        {
                            if (outerNear[k].d2 >= outerNear[k - 1].d2) break;
                            var tmp = outerNear[k];
                            outerNear[k] = outerNear[k - 1];
                            outerNear[k - 1] = tmp;
                        }
                    }
                    else if (d2 < outerNear[K_NEAREST - 1].d2)
                    {
                        outerNear[K_NEAREST - 1] = (d2, rvdw);
                        for (int k = K_NEAREST - 1; k > 0; k--)
                        {
                            if (outerNear[k].d2 >= outerNear[k - 1].d2) break;
                            var tmp = outerNear[k];
                            outerNear[k] = outerNear[k - 1];
                            outerNear[k - 1] = tmp;
                        }
                    }
                }

 // ── Nearest INNER (for d_i + names) + K nearest for soft-min ──
                float bestInnerSq = float.MaxValue;
                int bestInnerIdx = -1;
                float bestInnerR = 1.7f;
                string bestInnerName = "?";

                Span<(float d2, float r)> innerNear = stackalloc (float, float)[K_NEAREST];
                int innerNearCount = 0;

                for (int j = 0; j < targetCount; j++)
                {
                    var (apos, rvdw, aname) = targetPos[j];
                    float d2 = (p - apos).LengthSquared();
                    if (d2 < MIN_DIST_SQ) continue;

                    if (d2 < bestInnerSq)
                    {
                        bestInnerSq = d2;
                        bestInnerIdx = j;
                        bestInnerR = rvdw;
                        bestInnerName = aname;
                    }

                    if (innerNearCount < K_NEAREST)
                    {
                        innerNear[innerNearCount++] = (d2, rvdw);
                        for (int k = innerNearCount - 1; k > 0; k--)
                        {
                            if (innerNear[k].d2 >= innerNear[k - 1].d2) break;
                            var tmp = innerNear[k];
                            innerNear[k] = innerNear[k - 1];
                            innerNear[k - 1] = tmp;
                        }
                    }
                    else if (d2 < innerNear[K_NEAREST - 1].d2)
                    {
                        innerNear[K_NEAREST - 1] = (d2, rvdw);
                        for (int k = K_NEAREST - 1; k > 0; k--)
                        {
                            if (innerNear[k].d2 >= innerNear[k - 1].d2) break;
                            var tmp = innerNear[k];
                            innerNear[k] = innerNear[k - 1];
                            innerNear[k - 1] = tmp;
                        }
                    }
                }

 // ── Pure nearest-neighbor distances (fingerprint) ──
                float de = bestOuterSq < float.MaxValue ? MathF.Sqrt(bestOuterSq) : 0f;
                float di = bestInnerSq < float.MaxValue ? MathF.Sqrt(bestInnerSq) : 0f;

                surface.DE[vi] = de;
                surface.DI[vi] = di;
                surface.NearestOuterAtom[vi] = bestOuterIdx;
                surface.NearestInnerAtom[vi] = bestInnerIdx;
                surface.NearestOuterAtomName[vi] = bestOuterName;
                surface.NearestInnerAtomName[vi] = bestInnerName;

 // ── Soft-min d_norm (no Voronoi discontinuities) ──
                float SoftNormTerm(Span<(float d2, float r)> near, int count)
                {
                    float sumW = 0f, sumTerm = 0f;
                    for (int k = 0; k < count; k++)
                    {
                        float d = MathF.Sqrt(near[k].d2);
                        float r = near[k].r;
                        if (r < 1e-4f) continue;
                        float w = MathF.Exp(-SOFTMIN_BETA * d);
                        sumW += w;
                        sumTerm += w * ((d - r) / r);
                    }
                    return sumW > 1e-12f ? sumTerm / sumW : 0f;
                }

                float dnorm = SoftNormTerm(outerNear, outerNearCount)
                            + SoftNormTerm(innerNear, innerNearCount);

                if (dnorm < -3f) dnorm = -3f;
                if (dnorm > 3f) dnorm = 3f;

                surface.DNorm[vi] = dnorm;
            });

 // ═══════════════════════════════════════════════════════════════
 // Originals before color smoothing (nie used, ale zostawione)
 // ═══════════════════════════════════════════════════════════════
            surface.DNormOriginal = (float[])surface.DNorm.Clone();
            surface.DEOriginal = (float[])surface.DE.Clone();
            surface.DIOriginal = (float[])surface.DI.Clone();

 // ═══════════════════════════════════════════════════════════════
 // ⭐ Charge + Hydrophobicity — weighted average per-vertex
 //
 // Waga = exp(-d² / 2σ²) — gaussowski kernel around vertex.
 // σ = 0.6 Å — covers nearest neighbours (~1-2 atoms).
 //
 // Outer atoms have weight × 0.7 (are farther, influence less).
 //
 // Charge: bierzemy z SceneAtom.Charge (z Molecule.Atom.charge).
 // Hydrophobicity: bierzemy z SceneAtom.Hydrophobicity.
 // ═══════════════════════════════════════════════════════════════
            surface.Charge = new float[n];
            surface.Hydrophobicity = new float[n];

            var targetCharge = new float[targetCount];
            var targetHydro = new float[targetCount];
            var outerCharge = new float[outerCount];
            var outerHydro = new float[outerCount];

            for (int i = 0; i < targetCount; i++)
            {
                var sa = targetAtoms[i];
                targetCharge[i] = sa.Charge;
                targetHydro[i] = sa.Hydrophobicity;
            }

            for (int i = 0; i < outerCount; i++)
            {
                var sa = outerAtoms[i];
                outerCharge[i] = sa.Charge;
                outerHydro[i] = sa.Hydrophobicity;
            }

            const float CHARGE_SIGMA = 0.6f;
            float invTwoSigmaSq = 1.0f / (2f * CHARGE_SIGMA * CHARGE_SIGMA);

            float globalChargeMin = float.MaxValue;
            float globalChargeMax = float.MinValue;
            float globalHydroMin = float.MaxValue;
            float globalHydroMax = float.MinValue;
            var statLock = new object();

            progress?.Invoke("Mapping charge / hydrophobicity...", 0.96f);

            System.Threading.Tasks.Parallel.For(0, n, options, vi =>
            {
                ct.ThrowIfCancellationRequested();

                Vector3 p = surface.Vertices[vi];

                float sumW = 0f;
                float sumQ = 0f;
                float sumH = 0f;

 // Atomy target — full waga
                for (int j = 0; j < targetCount; j++)
                {
                    var (apos, _, _) = targetPos[j];
                    float d2 = (p - apos).LengthSquared();
                    float w = MathF.Exp(-d2 * invTwoSigmaSq);
                    sumW += w;
                    sumQ += w * targetCharge[j];
                    sumH += w * targetHydro[j];
                }

 // Atomy outer — waga × 0.7
                for (int j = 0; j < outerCount; j++)
                {
                    var (apos, _, _) = outerPos[j];
                    float d2 = (p - apos).LengthSquared();
                    float w = MathF.Exp(-d2 * invTwoSigmaSq) * 0.7f;
                    sumW += w;
                    sumQ += w * outerCharge[j];
                    sumH += w * outerHydro[j];
                }

                float invSumW = sumW > 1e-9f ? 1f / sumW : 0f;
                float qv = sumQ * invSumW;
                float hv = sumH * invSumW;

                surface.Charge[vi] = qv;
                surface.Hydrophobicity[vi] = hv;

                lock (statLock)
                {
                    if (qv < globalChargeMin) globalChargeMin = qv;
                    if (qv > globalChargeMax) globalChargeMax = qv;
                    if (hv < globalHydroMin) globalHydroMin = hv;
                    if (hv > globalHydroMax) globalHydroMax = hv;
                }
            });

            surface.ChargeMin = globalChargeMin < float.MaxValue ? globalChargeMin : -0.5f;
            surface.ChargeMax = globalChargeMax > float.MinValue ? globalChargeMax : +0.5f;
            surface.HydrophobicityMin = globalHydroMin < float.MaxValue ? globalHydroMin : -0.5f;
            surface.HydrophobicityMax = globalHydroMax > float.MinValue ? globalHydroMax : +0.5f;

 // ═══════════════════════════════════════════════════════════════
 // Diagnostyka
 // ═══════════════════════════════════════════════════════════════
            int dnormNegCount = 0, dnormPosCount = 0;
            int zeroDE = 0, zeroDI = 0, zeroDNorm = 0;
            float dnormMin = 0f, dnormMax = 0f;

            for (int i = 0; i < n; i++)
            {
                float d = surface.DNorm[i];
                if (d < dnormMin) dnormMin = d;
                if (d > dnormMax) dnormMax = d;
                if (d < -0.1f) dnormNegCount++;
                else if (d > 0.1f) dnormPosCount++;
                if (surface.DE[i] < 1e-6f) zeroDE++;
                if (surface.DI[i] < 1e-6f) zeroDI++;
                if (MathF.Abs(d) < 1e-6f) zeroDNorm++;
            }

 // ⭐ Check whether atoms have non-zero charge / hydrophobicity at all
            bool allZeroCharge = true;
            bool allZeroHydro = true;

            for (int i = 0; i < targetCount; i++)
            {
                if (MathF.Abs(targetCharge[i]) > 1e-6f) { allZeroCharge = false; }
                if (MathF.Abs(targetHydro[i]) > 1e-6f) { allZeroHydro = false; }
                if (!allZeroCharge && !allZeroHydro) break;
            }

            progress?.Invoke(
                $"d_norm range: [{dnormMin:F2}, {dnormMax:F2}], " +
                $"charge range: [{surface.ChargeMin:F3}, {surface.ChargeMax:F3}], " +
                $"hydro range: [{surface.HydrophobicityMin:F3}, {surface.HydrophobicityMax:F3}]",
                0.99f);

            System.Diagnostics.Debug.WriteLine(
                $"[HirshfeldProperties] Diagnostic: " +
                $"zeroDE={zeroDE}/{n} ({100f * zeroDE / n:F2}%), " +
                $"zeroDI={zeroDI}/{n} ({100f * zeroDI / n:F2}%), " +
                $"zeroDNorm={zeroDNorm}/{n} ({100f * zeroDNorm / n:F2}%), " +
                $"chargeRange=[{surface.ChargeMin:F3}, {surface.ChargeMax:F3}], " +
                $"hydroRange=[{surface.HydrophobicityMin:F3}, {surface.HydrophobicityMax:F3}]");

            if (allZeroCharge)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[HirshfeldProperties] ⚠️ ALL ATOM CHARGES ARE ZERO — " +
                    "check Molecule.Atom.charge / AtomSimple.charge");
            }

            if (allZeroHydro)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[HirshfeldProperties] ⚠️ ALL ATOM HYDROPHOBICITIES ARE ZERO — " +
                    "check Molecule.Atom.Hydrophobicity / AtomSimple.Hydrophobicity");
            }
        }
    }
}