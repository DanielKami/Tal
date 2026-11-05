// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hydrogen bond-length normalisation to standard values.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HydrogenNormalizer v10 — finalna version.
//
// ⭐ v10 (KLUCZOWA POPRAWKA):
// - NIE klampujemy do [0, 1) i NIE redukujemy.
// - CIF may have atoms afterza [0, 1) — to naturalne (geometria lokalna
// more important than packing in the cell).
// - We use DELTA: newHFrac = hFrac + fracShift
// gdzie fracShift = CartDeltaToFracDelta(unit * (target - len)).
// - Thanks to this H zostaje w TEJ SAMEJ kopii cell co jego heavy
// (np. hFrac.Z = 1.0090 → newHFrac.Z = 1.024, a nie 0.0054).
//
// ⭐ v9: Tylko AsymmetricPart, DetectMolecules(AsymPart) na starcie.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Tal.Calculate
{
 /// <summary>
 /// Statyczna klasa do normalisation wodoru do length neutronewch.
 /// Operates exclusively on AsymmetricPart.
 /// </summary>
    public static class HydrogenNormalizer
    {
        #region Static constructor

        static HydrogenNormalizer()
        {
            Debug.WriteLine(
                $"[HydrogenNormalizer] static init: " +
                $"{NeutronLengths.Count} neutron bond lengths");
        }

        #endregion

        #region Neutron Bond Lengths

        public static readonly Dictionary<int, float> NeutronLengths =
            new Dictionary<int, float>
        {
            {  5, 1.180f },  // B-H
            {  6, 1.083f },  // C-H
            {  7, 1.009f },  // N-H
            {  8, 0.983f },  // O-H
            {  9, 0.950f },  // F-H
            { 15, 1.420f },  // P-H
            { 16, 1.340f },  // S-H
            { 17, 1.280f },  // Cl-H
        };

        public static readonly Dictionary<int, string> NeutronLabels =
            new Dictionary<int, string>
        {
            {  5, "B-H"  },
            {  6, "C-H"  },
            {  7, "N-H"  },
            {  8, "O-H"  },
            {  9, "F-H"  },
            { 15, "P-H"  },
            { 16, "S-H"  },
            { 17, "Cl-H" },
        };

        #endregion

        #region Types

        public struct HydrogenFix
        {
            public int AsymHydrogenIndex;
            public int AsymHeavyIndex;

            public string HydrogenName;
            public string HeavyName;
            public int HeavyZ;

            public float OriginalLength;
            public float TargetLength;
            public float Delta;

            public bool Skipped;
            public string SkipReason;
        }

        #endregion

        #region Cache of originals

        private class OriginalSnapshot
        {
            public Vector3[] AsymFrac;
        }

        private static readonly Dictionary<Molecule, OriginalSnapshot> _originalCache
            = new Dictionary<Molecule, OriginalSnapshot>();

        private static readonly object _cacheLock = new object();

        public static void SaveOriginalPositions(Molecule molecule)
        {
            if (molecule == null || molecule.AsymmetricPart == null) return;

            lock (_cacheLock)
            {
                if (_originalCache.ContainsKey(molecule)) return;

                var snap = new OriginalSnapshot
                {
                    AsymFrac = new Vector3[molecule.AsymmetricPart.Count]
                };

                for (int i = 0; i < molecule.AsymmetricPart.Count; i++)
                {
                    var a = molecule.AsymmetricPart[i];
                    snap.AsymFrac[i] = new Vector3(a.x, a.y, a.z);
                }

                _originalCache[molecule] = snap;

                Debug.WriteLine(
                    $"[HydrogenNormalizer] Saved originals: " +
                    $"AsymPart={snap.AsymFrac.Length}");
            }
        }

        public static bool HasOriginalPositions(Molecule molecule)
        {
            if (molecule == null) return false;
            lock (_cacheLock) return _originalCache.ContainsKey(molecule);
        }

        public static void ClearCache(Molecule molecule)
        {
            if (molecule == null) return;
            lock (_cacheLock) _originalCache.Remove(molecule);
        }

        #endregion

        #region Public API

        public static List<HydrogenFix> Check(
            Molecule molecule,
            float cutoffHeavy = 1.3f,
            bool skipBridging = true)
        {
            var result = new List<HydrogenFix>();

            if (molecule == null || molecule.AsymmetricPart == null)
                return result;

            var asym = molecule.AsymmetricPart;
            if (asym.Count == 0) return result;

 // ⭐ DetectMolecules(AsymPart) — przypisz IndividualMoleculeInCell
            molecule.DetectMolecules(asym, resetExisting: true);

            int n = asym.Count;
            var frac = new Vector3[n];
            var zList = new int[n];
            var molList = new int[n];
            for (int i = 0; i < n; i++)
            {
                frac[i] = new Vector3(asym[i].x, asym[i].y, asym[i].z);
                zList[i] = asym[i].AtomicNumber;
                molList[i] = asym[i].IndividualMoleculeInCell;
            }

            for (int hIdx = 0; hIdx < n; hIdx++)
            {
                if (zList[hIdx] != 1) continue;

                var (heavyIdx, dist, candidateCount)
                    = FindHeavyNeighbor(molecule, asym, frac, zList, molList, hIdx, cutoffHeavy);

                var fix = new HydrogenFix
                {
                    AsymHydrogenIndex = hIdx,
                    AsymHeavyIndex = heavyIdx,
                    HydrogenName = asym[hIdx].name ?? $"H{hIdx}",
                    HeavyName = heavyIdx >= 0 ? asym[heavyIdx].name : "?",
                    HeavyZ = heavyIdx >= 0 ? zList[heavyIdx] : 0,
                    OriginalLength = dist,
                    Skipped = false,
                };

                if (heavyIdx < 0)
                {
                    fix.Skipped = true;
                    fix.SkipReason = "no heavy neighbor";
                    result.Add(fix);
                    continue;
                }

                if (candidateCount > 1 && skipBridging)
                {
                    fix.Skipped = true;
                    fix.SkipReason = $"bridging ({candidateCount} heavy)";
                    result.Add(fix);
                    continue;
                }

                if (!NeutronLengths.TryGetValue(fix.HeavyZ, out float target))
                {
                    fix.Skipped = true;
                    fix.SkipReason = $"no neutron data for Z={fix.HeavyZ}";
                    result.Add(fix);
                    continue;
                }

                fix.TargetLength = target;
                fix.Delta = target - fix.OriginalLength;

                if (MathF.Abs(fix.Delta) < 0.005f)
                {
                    fix.Skipped = true;
                    fix.SkipReason = "already normalized";
                    result.Add(fix);
                    continue;
                }

                result.Add(fix);
            }

            return result;
        }

        public static List<HydrogenFix> Normalize(
            Molecule molecule,
            float cutoffHeavy = 1.3f,
            bool skipBridging = true)
        {
            var fixes = Check(molecule, cutoffHeavy, skipBridging);

            if (molecule == null || molecule.AsymmetricPart == null)
                return fixes;

            SaveOriginalPositions(molecule);

            var asym = molecule.AsymmetricPart;

            int modified = 0;
            int skipped = 0;

            for (int k = 0; k < fixes.Count; k++)
            {
                var fix = fixes[k];

                if (fix.Skipped) { skipped++; continue; }

                int hIdx = fix.AsymHydrogenIndex;
                int heavyIdx = fix.AsymHeavyIndex;

 // ⭐ v10: Zachowaj oryginalne hFrac (may be afterza [0, 1))
                Vector3 hFrac = new Vector3(asym[hIdx].x, asym[hIdx].y, asym[hIdx].z);
                Vector3 heavyFrac = new Vector3(asym[heavyIdx].x, asym[heavyIdx].y, asym[heavyIdx].z);

                Vector3 hCart = molecule.FracToCart(hFrac);
                Vector3 heavyCart = molecule.FracToCart(heavyFrac);

                Vector3 dir = hCart - heavyCart;
                float len = dir.Length();

                if (len < 1e-6f) continue;

                Vector3 unit = dir / len;

 // v10: We compute Cartesian SHIFT, not a new absolute position
                Vector3 cartShift = unit * (fix.TargetLength - len);

 // ⭐ Konversion shifts Cartesiango na frakcjonalne (delta)
                Vector3 fracShift = molecule.CartDeltaToFracDelta(cartShift);

 // ⭐ Nowa position = old + shift
 // Preserves the copy: hFrac.Z = 1.0090 → newHFrac.Z = 1.024
                Vector3 newHFrac = hFrac + fracShift;

 // ⭐ Save BEZ klamafterwania, BEZ redukcji do [0, 1)
                var a = asym[hIdx];
                a.x = newHFrac.X;
                a.y = newHFrac.Y;
                a.z = newHFrac.Z;
                asym[hIdx] = a;

                modified++;
            }

            molecule.HydrogenNormalizationApplied = true;

            Debug.WriteLine(
                $"[HydrogenNormalizer] Normalize: modified={modified}, skipped={skipped}");

            return fixes;
        }

        public static bool ResetToOriginal(Molecule molecule)
        {
            if (molecule == null) return false;

            lock (_cacheLock)
            {
                if (!_originalCache.TryGetValue(molecule, out var snap))
                    return false;

                if (molecule.AsymmetricPart != null && snap.AsymFrac != null)
                {
                    int n = Math.Min(molecule.AsymmetricPart.Count, snap.AsymFrac.Length);
                    for (int i = 0; i < n; i++)
                    {
                        var a = molecule.AsymmetricPart[i];
                        a.x = snap.AsymFrac[i].X;
                        a.y = snap.AsymFrac[i].Y;
                        a.z = snap.AsymFrac[i].Z;
                        molecule.AsymmetricPart[i] = a;
                    }
                }

                molecule.HydrogenNormalizationApplied = false;
                molecule.HydrogenNormalizationDisabledByUser = true;

                Debug.WriteLine("[HydrogenNormalizer] Reset to original " +
                                "(DisabledByUser=true)");
                return true;
            }
        }

        #endregion

        #region Helpers — heavy neighbor

 /// <summary>
 /// Finds nearest heavy for H w AsymmetricPart.
 ///
 /// ⭐ Filtr: heavy must have TEN SAM IndividualMoleculeInCell co H.
 ///
 /// v10: Fractional difference WITHOUT mod-1 reduction.
 /// H and heavy are in the same copy (local chemistry) — if
 /// even both are outside [0, 1), the difference is small.
 /// Detector uses minimum-image only przy szukaniu przez
 /// unit-cell boundary, but here H and heavy are chemically bonded,
 /// so they are close in Cartesian space.
 /// </summary>
        private static (int idx, float dist, int candidateCount)
            FindHeavyNeighbor(
                Molecule molecule,
                List<Molecule.AtomSimple> asym,
                Vector3[] frac,
                int[] zList,
                int[] molList,
                int hIdx,
                float cutoff)
        {
            int n = asym.Count;
            Vector3 hFrac = frac[hIdx];
            int hMol = molList[hIdx];
            float cutoffSq = cutoff * cutoff;

            var bestByName = new Dictionary<string, (int idx, float d2)>(
                StringComparer.OrdinalIgnoreCase);

            for (int j = 0; j < n; j++)
            {
                if (j == hIdx) continue;
                if (zList[j] == 1) continue;

 // ⭐ Filtr molekularny
                if (molList[j] != hMol) continue;

                string name = asym[j].name;
                if (string.IsNullOrEmpty(name)) name = $"_{j}";

 // Fractional difference — without reduction
                float dx = hFrac.X - frac[j].X;
                float dy = hFrac.Y - frac[j].Y;
                float dz = hFrac.Z - frac[j].Z;

                Vector3 dCart = molecule.FracDeltaToCartDelta(new Vector3(dx, dy, dz));
                float d2 = dCart.LengthSquared();

                if (d2 > cutoffSq) continue;

                if (!bestByName.TryGetValue(name, out var cur) || d2 < cur.d2)
                {
                    bestByName[name] = (j, d2);
                }
            }

            int candidateCount = bestByName.Count;

            if (candidateCount == 0)
                return (-1, 0f, 0);

            int bestIdx = -1;
            float bestDistSq = float.MaxValue;

            foreach (var kv in bestByName)
            {
                if (kv.Value.d2 < bestDistSq)
                {
                    bestDistSq = kv.Value.d2;
                    bestIdx = kv.Value.idx;
                }
            }

            float dist = MathF.Sqrt(bestDistSq);
            return (bestIdx, dist, candidateCount);
        }

        #endregion

        #region Diagnostics

        public static IEnumerable<string> FormatFixes(
            Molecule molecule,
            List<HydrogenFix> fixes)
        {
            yield return $"═══════════ HYDROGEN NORMALIZATION ═══════════";
            yield return $"  Molecule: {molecule?.Name ?? "?"}, " +
                         $"atoms={molecule?.NumberOfAtoms ?? 0}, " +
                         $"AsymPart={molecule?.AsymmetricPart?.Count ?? 0}, " +
                         $"hydrogens={CountHydrogens(molecule)}";
            yield return $"  Applied={molecule?.HydrogenNormalizationApplied ?? false}, " +
                         $"DisabledByUser={molecule?.HydrogenNormalizationDisabledByUser ?? false}";

            int toFix = 0, skipped = 0;
            foreach (var f in fixes)
            {
                if (f.Skipped) skipped++;
                else toFix++;
            }

            yield return $"  To normalize: {toFix}, skipped: {skipped}";
            yield return "";

            if (toFix > 0)
            {
                yield return "  Fixes:";
                foreach (var f in fixes)
                {
                    if (f.Skipped) continue;
                    yield return $"    {f.HydrogenName,-6} " +
                                 $"{f.HeavyName,-6} ({f.HeavyZ,2})  " +
                                 $"{f.OriginalLength,6:F3} → {f.TargetLength,6:F3} Å  " +
                                 $"Δ = {f.Delta:+0.000;-0.000;0.000}";
                }
            }

            if (skipped > 0)
            {
                yield return "";
                yield return "  Skipped:";
                foreach (var f in fixes)
                {
                    if (!f.Skipped) continue;
                    yield return $"    {f.HydrogenName,-6} — {f.SkipReason}";
                }
            }

            yield return "═════════════════════════════════════════════";
        }

        private static int CountHydrogens(Molecule molecule)
        {
            if (molecule?.AsymmetricPart == null) return 0;
            int c = 0;
            for (int i = 0; i < molecule.AsymmetricPart.Count; i++)
                if (molecule.AsymmetricPart[i].AtomicNumber == 1) c++;
            return c;
        }

        #endregion
    }
}