// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure / image exafterrt: SpaceGroupTransformer.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════
// TRANSFORMATION BETWEEN SETTINGS GROUP SPACE
// ═══════════════════════════════════════════════════════════════════
//
// JEDNO SOURCE TRUTH:
// SymmetryElements.SymmetryOperations (tabela operacji)
//
// MAIN METHOD PUBLIC:
// TransformTo(structure, targetSettingName)
//
// ALGORYTM:
// 1. ResolveTransform — find matrix (hardcoded → brute-force).
// 2. ApplyTransformToStructure — aplikuje:
// - atoms (coordinates frakcjonalne, BEZ wraafterwania)
// - ADP (U' = N'⁻¹·Q·N·U·Nᵀ·Qᵀ·N'⁻ᵀ; not plain Q·U·Qᵀ)
// - cell (M' = M · P)
// - SymmetryOperations (regeneracja)
// - SpaceGroupName (update)
// 3. RefineMoleculeAftersitionsAfterTransform (called z MainForm):
// - iterative attraction atoms to neighbours (covalent)
// - przez ±1 w each osi frakcjonalnej
// - attractor — small fragments pulled toward the largest
// - atoms may leave the cell (that is OK)
//
// KONWENCJA MACIERZY:
// Transform works na COORDINATES atoms: x' = Q · x + t
// Q = P⁻¹ gdzie P z ITA works na wektory bazowe:
// (a', b', c') = (a, b, c) · P
//
// GWARANCJE:
// ✅ Round-trip A→B→A = identity mod 1.
// ✅ Hardcoded weryfikowany przez TestTransform.
// ✅ Brute-force jako fallback.
// ✅ Cell volume preserved przy transformation (V' = V).
// ✅ Structure CONSISTENT after transformation (name + cell + ops + atoms).

using Microsoft.Xna.Framework;

namespace Tal.Saver
{
    public static class SpaceGroupTransformer
    {
        #region Public types

 /// <summary>
 /// Macierz transformation 3×3 + translation.
 /// Aplikacja: x' = M · x + t (frakcjonalne).
 /// </summary>
        public struct Transform
        {
            public float M11, M12, M13;
            public float M21, M22, M23;
            public float M31, M32, M33;
            public float TX, TY, TZ;

            public static Transform Identity => new Transform
            {
                M11 = 1,
                M12 = 0,
                M13 = 0,
                M21 = 0,
                M22 = 1,
                M23 = 0,
                M31 = 0,
                M32 = 0,
                M33 = 1,
                TX = 0,
                TY = 0,
                TZ = 0
            };

            public Vector3 Apply(Vector3 frac)
            {
                return new Vector3(
                    M11 * frac.X + M12 * frac.Y + M13 * frac.Z + TX,
                    M21 * frac.X + M22 * frac.Y + M23 * frac.Z + TY,
                    M31 * frac.X + M32 * frac.Y + M33 * frac.Z + TZ
                );
            }

            public Vector3 ApplyRotationOnly(Vector3 v)
            {
                return new Vector3(
                    M11 * v.X + M12 * v.Y + M13 * v.Z,
                    M21 * v.X + M22 * v.Y + M23 * v.Z,
                    M31 * v.X + M32 * v.Y + M33 * v.Z
                );
            }

            public float Determinant =>
                M11 * (M22 * M33 - M23 * M32) -
                M12 * (M21 * M33 - M23 * M31) +
                M13 * (M21 * M32 - M22 * M31);

            public Transform RotationOnly()
            {
                var r = this;
                r.TX = r.TY = r.TZ = 0;
                return r;
            }

            public override string ToString()
            {
                return $"[[{M11:F0} {M12:F0} {M13:F0}] " +
                       $"[{M21:F0} {M22:F0} {M23:F0}] " +
                       $"[{M31:F0} {M32:F0} {M33:F0}]] + " +
                       $"({TX:F3},{TY:F3},{TZ:F3})";
            }
        }

 /// <summary>
 /// Wynik operacji TransformTo.
 /// </summary>
        public struct TransformResult
        {
            public bool Success;
            public Transform? AppliedTransform;
            public string TargetSetting;
            public string Message;

            public override string ToString()
                => $"{Message} | Success={Success} | " +
                   $"Transform={(AppliedTransform.HasValue
                       ? AppliedTransform.Value.ToString()
                       : "none")}";
        }

 /// <summary>
 /// Simple 3D vector used internally (double, without XNA).
 /// </summary>
        private struct Vec3d
        {
            public double X, Y, Z;
            public Vec3d(double x, double y, double z) { X = x; Y = y; Z = z; }
        }

        #endregion

        #region ⭐ MAIN METHOD PUBLIC

 /// <summary>
 /// ⭐ MAIN METHOD PUBLIC — complete transformation:
 /// 1. Finds matrix transformation (ResolveTransform)
 /// 2. Applies it to StructureData (atoms, ADP, cell,
 /// SymmetryOperations, SpaceGroupName)
 /// 3. Returns result
 ///
 /// The caller should afterwards:
 /// - call RefineMoleculeAftersitionsAfterTransform(structure)
 /// - rebuild Molecule: molecule.AsignXYZ(structure)
 /// - refresh the view
 /// </summary>
        public static TransformResult TransformTo(
            StructureData structure,
            string targetSettingName)
        {
            var result = new TransformResult
            {
                Success = false,
                AppliedTransform = null,
                TargetSetting = targetSettingName,
                Message = ""
            };

            if (structure == null)
            {
                result.Message = "Structure is null";
                return result;
            }

 // ── 1. Find matrix ──
            var transform = ResolveTransform(structure, targetSettingName);

            if (!transform.HasValue)
            {
                string srcNorm = SymmetryElements.NormalizeSpaceGroupName(
                    structure.SpaceGroupName ?? "");
                string dstNorm = SymmetryElements.NormalizeSpaceGroupName(
                    targetSettingName ?? "");

                if (srcNorm == dstNorm)
                {
                    result.Success = true;
                    result.Message = "Structure is already in target setting.";
                }
                else
                {
                    result.Success = false;
                    result.Message =
                        $"No transformation found from " +
                        $"'{structure.SpaceGroupName}' to '{targetSettingName}'.";
                }
                return result;
            }

 // ── 2. Zastosuj (BEZ wraafterwania — Refine to naprawi) ──
            result.AppliedTransform = transform;
            bool ok = ApplyTransformToStructure(
                structure, transform.Value, targetSettingName,
                wrapAtoms: false);

            if (!ok)
            {
                result.Success = false;
                result.Message = "ApplyTransformToStructure failed.";
                return result;
            }

            result.Success = true;
            result.Message =
                $"Transformed to '{structure.SpaceGroupName}' " +
                $"({structure.Atoms.Count} atoms).";

            return result;
        }

        #endregion

        #region Apply — kompletny update struktury

 /// <summary>
 /// Aplikuje transformation do structure IN-PLACE — KOMPLETNY update:
 /// 1. Fractional coordinates atoms
 /// ⭐ wrapAtoms = false (by default) → do NOT wrap, allow
 /// atoms to land in arbitrary positions. Refine je
 /// will pull them into a common cell.
 /// ⭐ wrapAtoms = true → wrapuj do [0, 1) (stare zachowanie,
 /// but tears molecules apart, that crossed the boundary).
 /// 2. Macierze U (ADP): U' = N'⁻¹ · Q · N · U · Nᵀ · Qᵀ · N'⁻ᵀ
 /// 3. Parametry cell: M' = M · P
 /// 4. SymmetryOperations — regeneratee for newgo settingu
 /// 5. SpaceGroupName — updates name
 /// </summary>
        public static bool ApplyTransformToStructure(
            StructureData structure,
            Transform T,
            string targetSettingName,
            bool wrapAtoms = false)
        {
            if (structure == null) return false;
            if (structure.Atoms == null || structure.Atoms.Count == 0)
                return false;

 // ── 1. Fractional coordinates: x' = Q · x + t ──
            foreach (var atom in structure.Atoms)
            {
                Vector3 frac = new Vector3(atom.X, atom.Y, atom.Z);
                Vector3 newFrac = T.Apply(frac);

                if (wrapAtoms)
                    newFrac = WrapToUnitCell(newFrac);

                atom.X = newFrac.X;
                atom.Y = newFrac.Y;
                atom.Z = newFrac.Z;
            }

 // ── 2. Snapshot OLD reciprocal metric (N = diag(a*,b*,c*)) BEFORE cell change ──
            GetReciprocalMetricN(structure, out float nOld11, out float nOld22, out float nOld33);

 // ── 3. Cell parameters: (a',b',c') = (a,b,c) · P, P = Q⁻¹ ──
            UpdateCellParametersFromTransform(structure, T);

 // ── 4. ADP (CIF U^{ij}): must include N, not plain Q·U·Qᵀ ──
 //    With A' = A·P and Q = P⁻¹, Cartesian U is invariant (R_cart = I),
 //    so:  U' = N'⁻¹ · Q · N · U · Nᵀ · Qᵀ · N'⁻ᵀ
 //    Plain U' = Q·U·Qᵀ is wrong when a*,c* change (e.g. β=144° → 99°).
            GetReciprocalMetricN(structure, out float nNew11, out float nNew22, out float nNew33);
            Transform R = T.RotationOnly();
            foreach (var atom in structure.Atoms)
            {
                if (atom.AdpType != "Uani") continue;
                ApplyAdpRotation(atom, R, nOld11, nOld22, nOld33, nNew11, nNew22, nNew33);
            }

 // ── 4. Zaktualizuj name i operacje symmetry ──
            int group = ResolveGroupNumber(structure);
            if (group < 1)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[SGT] ApplyTransformToStructure: cannot resolve group " +
                    "— skipping symmetry update");
                return true;
            }

 // 4a. Nazwa grupy
            string newName = string.IsNullOrWhiteSpace(targetSettingName)
                ? GetStandardName(group)
                : targetSettingName;

            structure.SpaceGroupName = newName;

 // 4b. Nowe operacje symmetry
            var newOps = GetOperationStringsForSetting(group, newName);

            if (newOps.Count > 0)
            {
                structure.SymmetryOperations.Clear();
                structure.SymmetryOperations.AddRange(newOps);

                System.Diagnostics.Debug.WriteLine(
                    $"[SGT] Updated SymmetryOperations: {newOps.Count} ops " +
                    $"for group {group} ('{newName}')");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SGT] ⚠️ No operations found for group {group} " +
                    $"('{newName}') — SymmetryOperations unchanged");
            }

            System.Diagnostics.Debug.WriteLine(
                $"[SGT] Applied transform: {T} to {structure.Atoms.Count} atoms, " +
                $"wrapAtoms={wrapAtoms}, target='{newName}'");

            return true;
        }

 /// <summary>
 /// Niskolevelowa version — aplikuje only atoms + ADP + cell.
 /// Nie rusza SymmetryOperations ani SpaceGroupName.
 /// Nie wrapuje atoms.
 /// </summary>
        public static void ApplyTransform(StructureData structure, Transform T)
        {
            if (structure == null) return;
            if (structure.Atoms == null || structure.Atoms.Count == 0) return;

            foreach (var atom in structure.Atoms)
            {
                Vector3 frac = new Vector3(atom.X, atom.Y, atom.Z);
                Vector3 newFrac = T.Apply(frac);
 // ⭐ do NOT wrap

                atom.X = newFrac.X;
                atom.Y = newFrac.Y;
                atom.Z = newFrac.Z;
            }

            GetReciprocalMetricN(structure, out float nOld11, out float nOld22, out float nOld33);
            UpdateCellParametersFromTransform(structure, T);
            GetReciprocalMetricN(structure, out float nNew11, out float nNew22, out float nNew33);

            Transform R = T.RotationOnly();
            foreach (var atom in structure.Atoms)
            {
                if (atom.AdpType != "Uani") continue;
                ApplyAdpRotation(atom, R, nOld11, nOld22, nOld33, nNew11, nNew22, nNew33);
            }

            System.Diagnostics.Debug.WriteLine(
                $"[SGT] Applied (low-level): {T} to {structure.Atoms.Count} atoms");
        }

        #endregion

        #region ResolveTransform — find the matrix

 /// <summary>
 /// Finds matrix transformation z currentgo settingu structure
 /// do WYBRANEGO settingu. Hardcoded → weryfikacja → brute-force.
 /// </summary>
        public static Transform? ResolveTransform(
            StructureData structure,
            string targetSettingName)
        {
            if (structure == null) return null;

            int group = ResolveGroupNumber(structure);
            if (group < 1)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[SGT] Cannot resolve group number.");
                return null;
            }

            string sourceName = structure.SpaceGroupName ?? "";
            string targetName = targetSettingName ?? "";

            if (string.IsNullOrWhiteSpace(targetName))
                targetName = GetStandardName(group);

            string srcNorm = SymmetryElements.NormalizeSpaceGroupName(sourceName);
            string dstNorm = SymmetryElements.NormalizeSpaceGroupName(targetName);

            System.Diagnostics.Debug.WriteLine(
                $"[SGT] ResolveTransform: group={group}, " +
                $"src='{sourceName}' ({srcNorm}), " +
                $"dst='{targetName}' ({dstNorm})");

            if (srcNorm == dstNorm)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[SGT] Source == target — nothing to do.");
                return null;
            }

            var srcOps = GetSourceOperations(structure, group);
            if (srcOps == null || srcOps.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[SGT] No source operations available.");
                return null;
            }

            var dstOps = GetOperationsForSetting(group, targetName);
            if (dstOps == null || dstOps.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SGT] No target operations for '{targetName}'.");
                return null;
            }

            if (srcOps.Count == dstOps.Count &&
                OperationSetsEquivalent(srcOps, dstOps))
            {
                System.Diagnostics.Debug.WriteLine(
                    "[SGT] Operation sets already equivalent — identity.");
                return null;
            }

            if (srcOps.Count != dstOps.Count)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SGT] ❌ Op count mismatch: src={srcOps.Count}, dst={dstOps.Count}");
                return null;
            }

 // HARDCODED with verification
            var hardcoded = HardcodedTransformations.Get(group, sourceName, targetName);
            if (hardcoded.HasValue)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SGT] Trying hardcoded: {hardcoded.Value}");

                if (TestTransform(hardcoded.Value, srcOps, dstOps))
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[SGT] ✅ Hardcoded VERIFIED");
                    return hardcoded;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[SGT] ⚠️ Hardcoded failed verification — " +
                        "falling back to brute-force");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    "[SGT] No hardcoded entry — using brute-force.");
            }

 // BRUTE-FORCE
            var found = FindTransformationMatrix(srcOps, dstOps, group);
            if (found.HasValue)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SGT] ✅ Brute-force found: {found.Value}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    "[SGT] ❌ No transformation found.");
            }
            return found;
        }

 // ── Convenience ──

        public static Transform? GetTransformToStandard(StructureData s)
        {
            if (s == null) return null;
            int g = ResolveGroupNumber(s);
            if (g < 1) return null;
            return ResolveTransform(s, GetStandardName(g));
        }

        public static Transform? GetTransformToSetting(
            StructureData s, string targetSettingName)
        {
            return ResolveTransform(s, targetSettingName);
        }

        public static List<string> GetAvailableSettings(int group)
        {
            var result = new List<string>();
            if (group < 1) return result;

            foreach (var entry in SymmetryElements.SymmetryOperations)
            {
                if (entry.Length < 4) continue;
                if (!int.TryParse(entry[0], out int g) || g != group) continue;

                string name = entry[2];
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (!result.Contains(name)) result.Add(name);
            }
            return result;
        }

        public static string GetCurrentSetting(StructureData structure)
        {
            if (structure == null) return "";

            string name = structure.SpaceGroupName ?? "";
            if (!string.IsNullOrWhiteSpace(name))
            {
                string normalized = SymmetryElements.NormalizeSpaceGroupName(name);

                foreach (var entry in SymmetryElements.SymmetryOperations)
                {
                    if (entry.Length < 3) continue;
                    string entryName = entry[2] ?? "";
                    string entryNorm = SymmetryElements.NormalizeSpaceGroupName(entryName);

                    if (string.Equals(entryNorm, normalized,
                                      StringComparison.OrdinalIgnoreCase))
                        return entryName;
                }
            }
            return name;
        }

        public static string GetStandardName(int group)
        {
            foreach (var entry in SymmetryElements.SymmetryOperations)
            {
                if (entry.Length < 4) continue;
                if (!int.TryParse(entry[0], out int g) || g != group) continue;
                if (!int.TryParse(entry[3], out int o) || o != 0) continue;
                return entry[2] ?? $"#{group}";
            }
            return $"#{group}";
        }

 /// <summary>
 /// Returns the list of symmetry-operation strings for the given setting.
 /// </summary>
        public static List<string> GetOperationStringsForSetting(
            int group, string settingName)
        {
            var result = new List<string>();
            if (group < 1) return result;

            string targetNorm =
                SymmetryElements.NormalizeSpaceGroupName(settingName ?? "");

            if (string.IsNullOrEmpty(targetNorm))
                targetNorm = SymmetryElements.NormalizeSpaceGroupName(
                    GetStandardName(group));

            foreach (var entry in SymmetryElements.SymmetryOperations)
            {
                if (entry.Length < 5) continue;
                if (!int.TryParse(entry[0], out int g) || g != group) continue;

                string entryName = entry[2] ?? "";
                string entryNorm = SymmetryElements.NormalizeSpaceGroupName(entryName);

                if (!string.Equals(entryNorm, targetNorm,
                                   StringComparison.OrdinalIgnoreCase))
                    continue;

                for (int i = 4; i < entry.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(entry[i])) continue;
                    result.Add(entry[i].Trim());
                }
                return result;
            }

            System.Diagnostics.Debug.WriteLine(
                $"[SGT] ⚠️ No operations in table for group {group}, " +
                $"setting='{settingName}' (normalized: '{targetNorm}')");

            return result;
        }

        #endregion

        #region Source / target operations

        private static int ResolveGroupNumber(StructureData structure)
        {
            int group = structure.SpaceGroup;
            if (group >= 1) return group;

            string name = structure.SpaceGroupName ?? "";
            if (string.IsNullOrEmpty(name)) return 0;

            string normalized = SymmetryElements.NormalizeSpaceGroupName(name);
            if (SymmetryElements.GroupNameToNumber.TryGetValue(normalized, out int num))
                return num;

            return 0;
        }

        private static List<SymmetryElements.ReadSymm> GetSourceOperations(
            StructureData structure, int group)
        {
            if (structure.SymmetryOperations != null &&
                structure.SymmetryOperations.Count > 0)
            {
                var list = new List<SymmetryElements.ReadSymm>();
                foreach (var op in structure.SymmetryOperations)
                {
                    if (string.IsNullOrWhiteSpace(op)) continue;
                    list.Add(SymmetryElements.CreateTransformationFromFile(op));
                }
                if (list.Count > 0) return list;
            }

            string name = structure.SpaceGroupName ?? "";
            return SymmetryElements.CreateTransformations(group, name, null);
        }

        private static List<SymmetryElements.ReadSymm> GetOperationsForSetting(
            int group, string settingName)
        {
            string normalizedTarget =
                SymmetryElements.NormalizeSpaceGroupName(settingName);

            foreach (var entry in SymmetryElements.SymmetryOperations)
            {
                if (entry.Length < 4) continue;
                if (!int.TryParse(entry[0], out int g) || g != group) continue;

                string entryName = entry[2] ?? "";
                string entryNorm = SymmetryElements.NormalizeSpaceGroupName(entryName);

                if (!string.Equals(entryNorm, normalizedTarget,
                                   StringComparison.OrdinalIgnoreCase))
                    continue;

                var result = new List<SymmetryElements.ReadSymm>();
                for (int i = 4; i < entry.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(entry[i])) continue;
                    result.Add(SymmetryElements.CreateTransformationFromFile(entry[i]));
                }
                return result;
            }
            return null;
        }

        #endregion

        #region Brute-force search

        private static Transform? FindTransformationMatrix(
            List<SymmetryElements.ReadSymm> srcOps,
            List<SymmetryElements.ReadSymm> dstOps,
            int groupNumber)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[SGT.Find] START: group={groupNumber}, " +
                $"srcOps={srcOps.Count}, dstOps={dstOps.Count}");

            var rotations = GenerateCandidateRotations();
            var shifts = GenerateCandidateShifts();

            System.Diagnostics.Debug.WriteLine(
                $"[SGT.Find] Candidates: {rotations.Count} rotations × " +
                $"{shifts.Count} shifts = {rotations.Count * shifts.Count}");

            int tested = 0;

            foreach (var rot in rotations)
            {
                foreach (var shift in shifts)
                {
                    tested++;

                    var Q = rot;
                    Q.TX = shift.X;
                    Q.TY = shift.Y;
                    Q.TZ = shift.Z;

                    if (TestTransform(Q, srcOps, dstOps))
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[SGT.Find] ✅ OK: {Q} (tested={tested})");
                        return Q;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"[SGT.Find] ❌ No match: tested={tested}");
            return null;
        }

        internal static bool TestTransform(
            Transform Q,
            List<SymmetryElements.ReadSymm> srcOps,
            List<SymmetryElements.ReadSymm> dstOps)
        {
            if (!InvertTransform(Q, out Transform Qinv))
                return false;

            var transformed = new List<SymmetryElements.ReadSymm>();
            foreach (var s in srcOps)
                transformed.Add(ConjugateOperation(s, Q, Qinv));

            return OperationSetsEquivalent(transformed, dstOps);
        }

        private static SymmetryElements.ReadSymm ConjugateOperation(
            SymmetryElements.ReadSymm s,
            Transform Q,
            Transform Qinv)
        {
            float[,] Ms = BuildMatrixFromReadSymm(s);
            float[,] Mq = BuildMatrixFromTransform(Q);
            float[,] MqInv = BuildMatrixFromTransform(Qinv);

            float[,] MqMs = MatMul3x3(Mq, Ms);
            float[,] MsPrime = MatMul3x3(MqMs, MqInv);

            Vector3 ts = new Vector3(s.shift[0], s.shift[1], s.shift[2]);
            Vector3 tQ = new Vector3(Q.TX, Q.TY, Q.TZ);

            Vector3 Q_ts = MatVec3x3(Mq, ts);
            Vector3 MsPrime_tQ = MatVec3x3(MsPrime, tQ);
            Vector3 tsPrime = Q_ts + tQ - MsPrime_tQ;

            float[] tsPrimeArr = { tsPrime.X, tsPrime.Y, tsPrime.Z };

            var result = new SymmetryElements.ReadSymm
            {
                pos1 = new int[3],
                sign1 = new int[3],
                pos2 = new int[3],
                sign2 = new int[3],
                shift = new float[3],
                orientation = s.orientation
            };

            for (int axis = 0; axis < 3; axis++)
            {
                ConvertMatrixRowToReadSymm(MsPrime, axis, ref result);

                float sh = tsPrimeArr[axis];
                sh = sh - (float)Math.Round(sh);
                result.shift[axis] = sh;
            }

            return result;
        }

        private static readonly List<Transform> CachedRotations = BuildAllIntegerRotations();

        private static List<Transform> BuildAllIntegerRotations()
        {
            var result = new List<Transform>();
            int[] values = { -1, 0, 1 };

            foreach (int m11 in values)
                foreach (int m12 in values)
                    foreach (int m13 in values)
                        foreach (int m21 in values)
                            foreach (int m22 in values)
                                foreach (int m23 in values)
                                    foreach (int m31 in values)
                                        foreach (int m32 in values)
                                            foreach (int m33 in values)
                                            {
                                                var T = new Transform
                                                {
                                                    M11 = m11,
                                                    M12 = m12,
                                                    M13 = m13,
                                                    M21 = m21,
                                                    M22 = m22,
                                                    M23 = m23,
                                                    M31 = m31,
                                                    M32 = m32,
                                                    M33 = m33
                                                };

                                                float det = T.Determinant;
                                                if (Math.Abs(Math.Abs(det) - 1f) > 1e-4f) continue;

                                                result.Add(T);
                                            }

            System.Diagnostics.Debug.WriteLine(
                $"[SGT] Cached {result.Count} integer rotations (det = ±1)");

            return result;
        }

        private static List<Transform> GenerateCandidateRotations()
            => CachedRotations;

        private static List<Vector3> GenerateCandidateShifts()
        {
            var result = new List<Vector3>();
            float[] commonValues = { 0f, 0.25f, 0.5f };

            foreach (var tx in commonValues)
                foreach (var ty in commonValues)
                    foreach (var tz in commonValues)
                        result.Add(new Vector3(tx, ty, tz));

            return result;
        }

        #endregion

        #region Operation set comparison

        private static bool OperationSetsEquivalent(
            List<SymmetryElements.ReadSymm> setA,
            List<SymmetryElements.ReadSymm> setB)
        {
            if (setA.Count != setB.Count) return false;

            foreach (var opA in setA)
            {
                bool found = false;
                foreach (var opB in setB)
                {
                    if (OperationsEquivalent(opA, opB)) { found = true; break; }
                }
                if (!found) return false;
            }

            foreach (var opB in setB)
            {
                bool found = false;
                foreach (var opA in setA)
                {
                    if (OperationsEquivalent(opB, opA)) { found = true; break; }
                }
                if (!found) return false;
            }

            return true;
        }

        private static bool OperationsEquivalent(
            SymmetryElements.ReadSymm opA,
            SymmetryElements.ReadSymm opB)
        {
            const float TOL = 1e-3f;

            float[][] testPoints = new[]
            {
                new float[] { 0.13f, 0.27f, 0.41f },
                new float[] { 0.31f, 0.07f, 0.83f },
                new float[] { 0.57f, 0.63f, 0.19f },
            };

            foreach (var p in testPoints)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    float va = ApplyOpAxis(opA, axis, p);
                    float vb = ApplyOpAxis(opB, axis, p);

                    float diff = va - vb;
                    diff = diff - (float)Math.Round(diff);
                    if (Math.Abs(diff) > TOL) return false;
                }
            }
            return true;
        }

        private static float ApplyOpAxis(
            SymmetryElements.ReadSymm op, int axis, float[] point)
        {
            float result = op.shift[axis];

            int p1 = op.pos1[axis];
            int s1 = op.sign1[axis];
            if (p1 >= 0 && p1 < 3 && s1 != 0)
                result += s1 * point[p1];

            int p2 = op.pos2[axis];
            int s2 = op.sign2[axis];
            if (p2 >= 0 && p2 < 3 && s2 != 0)
                result += s2 * point[p2];

            return result;
        }

        #endregion

        #region Matrix helpers

        private static float[,] BuildMatrixFromReadSymm(SymmetryElements.ReadSymm s)
        {
            float[,] M = new float[3, 3];
            for (int axis = 0; axis < 3; axis++)
            {
                int p1 = s.pos1[axis], s1 = s.sign1[axis];
                if (p1 >= 0 && p1 < 3 && s1 != 0) M[axis, p1] += s1;

                int p2 = s.pos2[axis], s2 = s.sign2[axis];
                if (p2 >= 0 && p2 < 3 && s2 != 0) M[axis, p2] += s2;
            }
            return M;
        }

        private static float[,] BuildMatrixFromTransform(Transform T)
        {
            return new float[,]
            {
                { T.M11, T.M12, T.M13 },
                { T.M21, T.M22, T.M23 },
                { T.M31, T.M32, T.M33 }
            };
        }

        private static float[,] MatMul3x3(float[,] A, float[,] B)
        {
            float[,] C = new float[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    float sum = 0;
                    for (int k = 0; k < 3; k++) sum += A[i, k] * B[k, j];
                    C[i, j] = sum;
                }
            return C;
        }

        private static Vector3 MatVec3x3(float[,] M, Vector3 v)
        {
            return new Vector3(
                M[0, 0] * v.X + M[0, 1] * v.Y + M[0, 2] * v.Z,
                M[1, 0] * v.X + M[1, 1] * v.Y + M[1, 2] * v.Z,
                M[2, 0] * v.X + M[2, 1] * v.Y + M[2, 2] * v.Z
            );
        }

        private static void ConvertMatrixRowToReadSymm(
            float[,] M, int row, ref SymmetryElements.ReadSymm result)
        {
            result.pos1[row] = 0;
            result.sign1[row] = 0;
            result.pos2[row] = 0;
            result.sign2[row] = 0;

            int found = 0;
            for (int col = 0; col < 3; col++)
            {
                float v = M[row, col];
                if (Math.Abs(v) < 1e-6f) continue;

                int signVal = v > 0 ? 1 : -1;
                if (found == 0)
                {
                    result.pos1[row] = col;
                    result.sign1[row] = signVal;
                }
                else if (found == 1)
                {
                    result.pos2[row] = col;
                    result.sign2[row] = signVal;
                }
                found++;
            }
        }

        private static bool InvertTransform(Transform T, out Transform inv)
        {
            float det =
                T.M11 * (T.M22 * T.M33 - T.M23 * T.M32) -
                T.M12 * (T.M21 * T.M33 - T.M23 * T.M31) +
                T.M13 * (T.M21 * T.M32 - T.M22 * T.M31);

            if (Math.Abs(det) < 1e-6f)
            {
                inv = Transform.Identity;
                return false;
            }

            float invDet = 1f / det;

            float i11 = (T.M22 * T.M33 - T.M23 * T.M32) * invDet;
            float i12 = (T.M13 * T.M32 - T.M12 * T.M33) * invDet;
            float i13 = (T.M12 * T.M23 - T.M13 * T.M22) * invDet;

            float i21 = (T.M23 * T.M31 - T.M21 * T.M33) * invDet;
            float i22 = (T.M11 * T.M33 - T.M13 * T.M31) * invDet;
            float i23 = (T.M13 * T.M21 - T.M11 * T.M23) * invDet;

            float i31 = (T.M21 * T.M32 - T.M22 * T.M31) * invDet;
            float i32 = (T.M12 * T.M31 - T.M11 * T.M32) * invDet;
            float i33 = (T.M11 * T.M22 - T.M12 * T.M21) * invDet;

            float txInv = -(i11 * T.TX + i12 * T.TY + i13 * T.TZ);
            float tyInv = -(i21 * T.TX + i22 * T.TY + i23 * T.TZ);
            float tzInv = -(i31 * T.TX + i32 * T.TY + i33 * T.TZ);

            inv = new Transform
            {
                M11 = i11,
                M12 = i12,
                M13 = i13,
                M21 = i21,
                M22 = i22,
                M23 = i23,
                M31 = i31,
                M32 = i32,
                M33 = i33,
                TX = txInv,
                TY = tyInv,
                TZ = tzInv
            };
            return true;
        }

        #endregion

        #region ADP rotation

        /// <summary>
        /// Reciprocal-axis lengths a*, b*, c* used as N = diag(a*,b*,c*)
        /// in Molecule.TransformUStatToU / AsignXYZ (Ue = A·N·U·Nᵀ·Aᵀ).
        /// </summary>
        private static void GetReciprocalMetricN(
            StructureData s,
            out float n11, out float n22, out float n33)
        {
            double a = s.a, b = s.b, c = s.c;
            double cosA = Math.Cos(MathHelper.ToRadians(s.alpha));
            double cosB = Math.Cos(MathHelper.ToRadians(s.beta));
            double cosG = Math.Cos(MathHelper.ToRadians(s.gamma));
            double sinA = Math.Sin(MathHelper.ToRadians(s.alpha));
            double sinB = Math.Sin(MathHelper.ToRadians(s.beta));
            double sinG = Math.Sin(MathHelper.ToRadians(s.gamma));

            double vol = a * b * c * Math.Sqrt(Math.Max(0.0,
                1.0 - cosA * cosA - cosB * cosB - cosG * cosG
                + 2.0 * cosA * cosB * cosG));

            if (vol < 1e-12)
            {
                n11 = n22 = n33 = 1f;
                return;
            }

            n11 = (float)(b * c * sinA / vol); // a*
            n22 = (float)(a * c * sinB / vol); // b*
            n33 = (float)(a * b * sinG / vol); // c*
        }

        /// <summary>
        /// Transforms CIF U^{ij} under setting change x' = Q·x.
        ///
        /// Correct formula (Cartesian U invariant when A' = A·P, Q = P⁻¹):
        ///   U' = N'⁻¹ · Q · N · U · Nᵀ · Qᵀ · N'⁻ᵀ
        ///
        /// Plain U' = Q·U·Qᵀ is only valid when N' = N (cubic / unchanged metric).
        /// Monoclinic cell-choice changes (β 144° ↔ 99°) make a*,c* differ a lot —
        /// that was the ~9% void-volume bias with thermal motion ON.
        /// </summary>
        private static void ApplyAdpRotation(
            SimpleAtom atom, Transform R,
            float nOld11, float nOld22, float nOld33,
            float nNew11, float nNew22, float nNew33)
        {
            // Step 1: Ũ = N · U · Nᵀ  (N diagonal)
            float u11 = atom.U11 * nOld11 * nOld11;
            float u22 = atom.U22 * nOld22 * nOld22;
            float u33 = atom.U33 * nOld33 * nOld33;
            float u12 = atom.U12 * nOld11 * nOld22;
            float u13 = atom.U13 * nOld11 * nOld33;
            float u23 = atom.U23 * nOld22 * nOld33;

            // Step 2: Ũ' = Q · Ũ · Qᵀ
            float r11 = R.M11, r12 = R.M12, r13 = R.M13;
            float r21 = R.M21, r22 = R.M22, r23 = R.M23;
            float r31 = R.M31, r32 = R.M32, r33 = R.M33;

            float ru11 = r11 * u11 + r12 * u12 + r13 * u13;
            float ru12 = r11 * u12 + r12 * u22 + r13 * u23;
            float ru13 = r11 * u13 + r12 * u23 + r13 * u33;

            float ru21 = r21 * u11 + r22 * u12 + r23 * u13;
            float ru22 = r21 * u12 + r22 * u22 + r23 * u23;
            float ru23 = r21 * u13 + r22 * u23 + r23 * u33;

            float ru31 = r31 * u11 + r32 * u12 + r33 * u13;
            float ru32 = r31 * u12 + r32 * u22 + r33 * u23;
            float ru33 = r31 * u13 + r32 * u23 + r33 * u33;

            u11 = ru11 * r11 + ru12 * r12 + ru13 * r13;
            u12 = ru11 * r21 + ru12 * r22 + ru13 * r23;
            u13 = ru11 * r31 + ru12 * r32 + ru13 * r33;
            u22 = ru21 * r21 + ru22 * r22 + ru23 * r23;
            u23 = ru21 * r31 + ru22 * r32 + ru23 * r33;
            u33 = ru31 * r31 + ru32 * r32 + ru33 * r33;

            // Step 3: U' = N'⁻¹ · Ũ' · N'⁻ᵀ
            float i1 = (Math.Abs(nNew11) > 1e-12f) ? 1f / nNew11 : 1f;
            float i2 = (Math.Abs(nNew22) > 1e-12f) ? 1f / nNew22 : 1f;
            float i3 = (Math.Abs(nNew33) > 1e-12f) ? 1f / nNew33 : 1f;

            atom.U11 = u11 * i1 * i1;
            atom.U22 = u22 * i2 * i2;
            atom.U33 = u33 * i3 * i3;
            atom.U12 = u12 * i1 * i2;
            atom.U13 = u13 * i1 * i3;
            atom.U23 = u23 * i2 * i3;
        }

        #endregion

        #region Cell parameters update — M · P

        private static void UpdateCellParametersFromTransform(
            StructureData structure, Transform Q)
        {
            if (!InvertTransform(Q, out Transform P))
            {
                System.Diagnostics.Debug.WriteLine(
                    "[SGT] UpdateCell: cannot invert Q — cell unchanged");
                return;
            }

            double a = structure.a;
            double b = structure.b;
            double c = structure.c;
            double alpha = MathHelper.ToRadians(structure.alpha);
            double beta = MathHelper.ToRadians(structure.beta);
            double gamma = MathHelper.ToRadians(structure.gamma);

            double sinG = Math.Sin(gamma);
            if (Math.Abs(sinG) < 1e-9) sinG = 1e-9;

            double ax = a, ay = 0, az = 0;

            double bx = b * Math.Cos(gamma);
            double by = b * sinG;
            double bz = 0;

            double cx = c * Math.Cos(beta);
            double cy = c * (Math.Cos(alpha) - Math.Cos(beta) * Math.Cos(gamma)) / sinG;
            double cz2 = c * c - cx * cx - cy * cy;
            double cz = cz2 > 0 ? Math.Sqrt(cz2) : 0;

            double p11 = P.M11, p12 = P.M12, p13 = P.M13;
            double p21 = P.M21, p22 = P.M22, p23 = P.M23;
            double p31 = P.M31, p32 = P.M32, p33 = P.M33;

            double nax = ax * p11 + bx * p21 + cx * p31;
            double nay = ay * p11 + by * p21 + cy * p31;
            double naz = az * p11 + bz * p21 + cz * p31;

            double nbx = ax * p12 + bx * p22 + cx * p32;
            double nby = ay * p12 + by * p22 + cy * p32;
            double nbz = az * p12 + bz * p22 + cz * p32;

            double ncx = ax * p13 + bx * p23 + cx * p33;
            double ncy = ay * p13 + by * p23 + cy * p33;
            double ncz = az * p13 + bz * p23 + cz * p33;

            double newA = Math.Sqrt(nax * nax + nay * nay + naz * naz);
            double newB = Math.Sqrt(nbx * nbx + nby * nby + nbz * nbz);
            double newC = Math.Sqrt(ncx * ncx + ncy * ncy + ncz * ncz);

            double newAlpha = AngleBetween3D(nbx, nby, nbz, ncx, ncy, ncz);
            double newBeta = AngleBetween3D(nax, nay, naz, ncx, ncy, ncz);
            double newGamma = AngleBetween3D(nax, nay, naz, nbx, nby, nbz);

            double volOld = a * b * c *
                Math.Sqrt(1.0
                    - Math.Cos(alpha) * Math.Cos(alpha)
                    - Math.Cos(beta) * Math.Cos(beta)
                    - Math.Cos(gamma) * Math.Cos(gamma)
                    + 2.0 * Math.Cos(alpha) * Math.Cos(beta) * Math.Cos(gamma));

            double volNew = Math.Abs(
                nax * (nby * ncz - nbz * ncy) -
                nay * (nbx * ncz - nbz * ncx) +
                naz * (nbx * ncy - nby * ncx));

            structure.a = (float)newA;
            structure.b = (float)newB;
            structure.c = (float)newC;
            structure.alpha = (float)MathHelper.ToDegrees((float)newAlpha);
            structure.beta = (float)MathHelper.ToDegrees((float)newBeta);
            structure.gamma = (float)MathHelper.ToDegrees((float)newGamma);

            System.Diagnostics.Debug.WriteLine(
                $"[SGT] Cell after transform: " +
                $"a={newA:F4} b={newB:F4} c={newC:F4} " +
                $"α={structure.alpha:F3} β={structure.beta:F3} γ={structure.gamma:F3}");

            System.Diagnostics.Debug.WriteLine(
                $"[SGT] Volume: {volOld:F2} → {volNew:F2} " +
                $"(Δ={(volNew - volOld):F4})");
        }

        private static double AngleBetween3D(
            double ux, double uy, double uz,
            double vx, double vy, double vz)
        {
            double dot = ux * vx + uy * vy + uz * vz;
            double lenU = Math.Sqrt(ux * ux + uy * uy + uz * uz);
            double lenV = Math.Sqrt(vx * vx + vy * vy + vz * vz);
            if (lenU < 1e-9 || lenV < 1e-9) return Math.PI / 2;
            double cos = dot / (lenU * lenV);
            cos = Math.Max(-1, Math.Min(1, cos));
            return Math.Acos(cos);
        }

        #endregion

        #region Helpers

        private static Vector3 WrapToUnitCell(Vector3 frac)
        {
            return new Vector3(
                frac.X - (float)Math.Floor(frac.X),
                frac.Y - (float)Math.Floor(frac.Y),
                frac.Z - (float)Math.Floor(frac.Z)
            );
        }

        private static float Wrap01Static(float v)
        {
            float r = v - (float)Math.Floor(v);
            if (r >= 1f) r -= 1f;
            if (r < 0f) r += 1f;
            return r;
        }

        #endregion

        #region REFINE — iterative bond relaxation + attractor

 /// <summary>
 /// NEW METHOD — called AFTER TransformTo.
 ///
 /// Goal: pull atoms of the same molecule into a common cell
 /// przez iteracyjne translation afterjedynczych atoms o ±1 (or ±2)
 /// in fractional coordinates so that CARTESIAN distances
 /// between bonded atoms are close (covalent).
 ///
 /// ⭐ NIE WRAPUJE na starcie ani at the end.
 /// Atoms may leave the cell — that is OK.
 /// We want the molecule to be in one piece, not fragmented across cells.
 ///
 /// ⭐ ATRAKTOR:
 /// After each iteracji checksmy graf bonds w currentj geometrii
 /// Cartesian. If there are 2+ connected components, we pick the LARGEST
 /// as attractor and pull the remaining components toward it by
 /// a common translation of ±2.
 /// </summary>
        public static void RefineMoleculePositionsAfterTransform(
            StructureData structure,
            List<(int i, int j)> bonds = null,
            int maxIterations = 100)
        {
            if (structure == null || structure.Atoms == null || structure.Atoms.Count < 2)
                return;

            int n = structure.Atoms.Count;

            System.Diagnostics.Debug.WriteLine(
                $"[Refine] START — {n} atoms, maxIter={maxIterations}");

 // ⭐ do NOT wrap na starcie — allow atomom be w dowolnych positionch

 // ═══════════════════════════════════════════════════════════
 // Macierz frakcjonalne → Cartesian
 // ═══════════════════════════════════════════════════════════
            double ca = structure.a, cb = structure.b, cc = structure.c;
            double al = structure.alpha * Math.PI / 180.0;
            double be = structure.beta * Math.PI / 180.0;
            double ga = structure.gamma * Math.PI / 180.0;

            double sinG = Math.Sin(ga);
            if (Math.Abs(sinG) < 1e-9) sinG = 1e-9;

            double ax = ca, ay = 0, az = 0;
            double bx = cb * Math.Cos(ga);
            double by = cb * sinG;
            double bz = 0;
            double cx = cc * Math.Cos(be);
            double cy = cc * (Math.Cos(al) - Math.Cos(be) * Math.Cos(ga)) / sinG;
            double cz2 = cc * cc - cx * cx - cy * cy;
            double cz = cz2 > 0 ? Math.Sqrt(cz2) : 0;

            Vec3d FracToCart(double fx, double fy, double fz)
            {
                return new Vec3d(
                    fx * ax + fy * bx + fz * cx,
                    fx * ay + fy * by + fz * cy,
                    fx * az + fy * bz + fz * cz);
            }

 // ═══════════════════════════════════════════════════════════
 // Wykryj bonds (if nie afterdano)
 // ═══════════════════════════════════════════════════════════
            if (bonds == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Refine] No bonds provided — detecting (minimum image)");
                bonds = DetectBondsInStructure(structure);
            }

            System.Diagnostics.Debug.WriteLine(
                $"[Refine] Using {bonds.Count} bonds");

            if (bonds.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Refine] No bonds — nothing to refine");
                return;
            }

 // ═══════════════════════════════════════════════════════════
 // Iteracyjna relaksacja after bondsch + atraktor
 // ═══════════════════════════════════════════════════════════
            for (int iter = 0; iter < maxIterations; iter++)
            {
                int changedThisIter = 0;

                foreach (var (i, j) in bonds)
                {
                    var ai = structure.Atoms[i];
                    var aj = structure.Atoms[j];

                    var ci = FracToCart(ai.X, ai.Y, ai.Z);
                    var cj = FracToCart(aj.X, aj.Y, aj.Z);

                    double ddx = ci.X - cj.X;
                    double ddy = ci.Y - cj.Y;
                    double ddz = ci.Z - cj.Z;
                    double currentDist = Math.Sqrt(ddx * ddx + ddy * ddy + ddz * ddz);

                    double thresh = CovalentThreshold(ai.AtomicNumber, aj.AtomicNumber);
                    if (currentDist < thresh * 1.5)
                        continue;

 // Search shifts for atom j (±1)
                    double bestDist = currentDist;
                    int bestDx = 0, bestDy = 0, bestDz = 0;

                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                if (dx == 0 && dy == 0 && dz == 0) continue;

                                var cj2 = FracToCart(aj.X + dx, aj.Y + dy, aj.Z + dz);

                                double ndx = ci.X - cj2.X;
                                double ndy = ci.Y - cj2.Y;
                                double ndz = ci.Z - cj2.Z;
                                double newDist = Math.Sqrt(ndx * ndx + ndy * ndy + ndz * ndz);

                                if (newDist < bestDist - 1e-6)
                                {
                                    bestDist = newDist;
                                    bestDx = dx;
                                    bestDy = dy;
                                    bestDz = dz;
                                }
                            }

                    if (bestDx != 0 || bestDy != 0 || bestDz != 0)
                    {
                        var ajNew = structure.Atoms[j];
                        ajNew.X += bestDx;
                        ajNew.Y += bestDy;
                        ajNew.Z += bestDz;
                        structure.Atoms[j] = ajNew;
                        changedThisIter++;
                    }
                }

 // Atraktor
                int attractorAdjustments = ApplyAttractor(
                    structure, FracToCart, iter);

                System.Diagnostics.Debug.WriteLine(
                    $"[Refine] Iter {iter + 1}: {changedThisIter} bond adjustments, " +
                    $"{attractorAdjustments} attractor adjustments");

                if (changedThisIter == 0 && attractorAdjustments == 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Refine] Converged after {iter + 1} iterations");
                    break;
                }
            }

 // ═══════════════════════════════════════════════════════════
 // Finally — translate each molecule by an INTEGER translation
 // so that the anchor is near (0.5, 0.5, 0.5).
 // This does not wrap atoms — it only shifts the WHOLE molecule.
 // ═══════════════════════════════════════════════════════════
            int[] molIds = DetectMoleculesSimple(structure);
            var groups = new Dictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                int m = molIds[i];
                if (m < 0) m = 0;
                if (!groups.TryGetValue(m, out var lst))
                {
                    lst = new List<int>();
                    groups[m] = lst;
                }
                lst.Add(i);
            }

            int biggestGroupId = -1;
            int biggestSize = -1;
            foreach (var kvp in groups)
            {
                if (kvp.Value.Count > biggestSize)
                {
                    biggestSize = kvp.Value.Count;
                    biggestGroupId = kvp.Key;
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"[Refine] After refinement: {groups.Count} fragments, " +
                $"biggest={biggestSize} atoms (id={biggestGroupId})");

            foreach (var kvp in groups)
            {
                var indices = kvp.Value;
                if (indices.Count == 0) continue;

 // Anchor: heaviest non-hydrogen
                int anchorIdx = indices[0];
                int bestZ = -1;
                foreach (int idx in indices)
                {
                    int z = structure.Atoms[idx].AtomicNumber;
                    if (z == 1) continue;
                    if (z > bestZ) { bestZ = z; anchorIdx = idx; }
                }
                if (bestZ < 0) anchorIdx = indices[0];

                double axA = structure.Atoms[anchorIdx].X;
                double ayA = structure.Atoms[anchorIdx].Y;
                double azA = structure.Atoms[anchorIdx].Z;

                int shiftX = (int)Math.Round(0.5 - axA);
                int shiftY = (int)Math.Round(0.5 - ayA);
                int shiftZ = (int)Math.Round(0.5 - azA);

                if (shiftX != 0 || shiftY != 0 || shiftZ != 0)
                {
                    foreach (int idx in indices)
                    {
                        var a = structure.Atoms[idx];
                        a.X += shiftX;
                        a.Y += shiftY;
                        a.Z += shiftZ;
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[Refine] Mol {kvp.Key}: {indices.Count} atoms, " +
                    $"anchor={structure.Atoms[anchorIdx].Name}, " +
                    $"shift=({shiftX},{shiftY},{shiftZ})");
            }

            System.Diagnostics.Debug.WriteLine("[Refine] END");
        }

 /// <summary>
 /// ATTRACTOR — pulls small fragments (connected components of the bond graph)
 /// toward the LARGEST fragment. Trial range: ±2 per axis (125 candidates).
 /// </summary>
        private static int ApplyAttractor(
            StructureData structure,
            Func<double, double, double, Vec3d> fracToCart,
            int iteration)
        {
            int n = structure.Atoms.Count;
            if (n < 2) return 0;

 // Cartesian positions of all atoms
            var cart = new Vec3d[n];
            for (int i = 0; i < n; i++)
            {
                var a = structure.Atoms[i];
                cart[i] = fracToCart(a.X, a.Y, a.Z);
            }

 // Connected components via CARTESIAN bonds (< 3 Å)
            var adj = new List<int>[n];
            for (int i = 0; i < n; i++) adj[i] = new List<int>();

            const double CLOSE_THRESHOLD = 3.0;   // Å

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    double dx = cart[i].X - cart[j].X;
                    double dy = cart[i].Y - cart[j].Y;
                    double dz = cart[i].Z - cart[j].Z;
                    double d = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                    if (d < CLOSE_THRESHOLD)
                    {
                        adj[i].Add(j);
                        adj[j].Add(i);
                    }
                }
            }

            var componentId = new int[n];
            for (int i = 0; i < n; i++) componentId[i] = -1;

            var components = new List<List<int>>();
            for (int start = 0; start < n; start++)
            {
                if (componentId[start] >= 0) continue;

                int cid = components.Count;
                var comp = new List<int>();
                var queue = new Queue<int>();
                queue.Enqueue(start);
                componentId[start] = cid;

                while (queue.Count > 0)
                {
                    int cur = queue.Dequeue();
                    comp.Add(cur);

                    foreach (var nb in adj[cur])
                    {
                        if (componentId[nb] < 0)
                        {
                            componentId[nb] = cid;
                            queue.Enqueue(nb);
                        }
                    }
                }
                components.Add(comp);
            }

            if (components.Count <= 1)
                return 0;

 // Largest component = attractor
            int attractorId = 0;
            int attractorSize = components[0].Count;
            for (int c = 1; c < components.Count; c++)
            {
                if (components[c].Count > attractorSize)
                {
                    attractorSize = components[c].Count;
                    attractorId = c;
                }
            }

            var attractor = components[attractorId];

            System.Diagnostics.Debug.WriteLine(
                $"[Attractor] Iter {iteration + 1}: {components.Count} components, " +
                $"attractor={attractorId} ({attractorSize} atoms)");

            int totalMoved = 0;

 // Trial range: ±2 (125 candidates)
            const int RANGE = 2;

            for (int c = 0; c < components.Count; c++)
            {
                if (c == attractorId) continue;

                var comp = components[c];

 // Find the nearest pair (a ∈ comp, b ∈ attractor)
                int bestCompIdx = -1;
                int bestAttIdx = -1;
                double bestDist = double.MaxValue;

                foreach (int ci in comp)
                {
                    foreach (int ai in attractor)
                    {
                        double dx = cart[ci].X - cart[ai].X;
                        double dy = cart[ci].Y - cart[ai].Y;
                        double dz = cart[ci].Z - cart[ai].Z;
                        double d = Math.Sqrt(dx * dx + dy * dy + dz * dz);

                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestCompIdx = ci;
                            bestAttIdx = ai;
                        }
                    }
                }

                if (bestCompIdx < 0 || bestAttIdx < 0)
                    continue;

                var ac = structure.Atoms[bestCompIdx];
                double ax0 = ac.X, ay0 = ac.Y, az0 = ac.Z;

                var aa = structure.Atoms[bestAttIdx];
                double attX = aa.X, attY = aa.Y, attZ = aa.Z;

                double bestNewDist = bestDist;
                int bestDx = 0, bestDy = 0, bestDz = 0;

                for (int dx = -RANGE; dx <= RANGE; dx++)
                    for (int dy = -RANGE; dy <= RANGE; dy++)
                        for (int dz = -RANGE; dz <= RANGE; dz++)
                        {
                            if (dx == 0 && dy == 0 && dz == 0) continue;

                            var newC = fracToCart(ax0 + dx, ay0 + dy, az0 + dz);
                            var newA = fracToCart(attX, attY, attZ);

                            double ndx = newC.X - newA.X;
                            double ndy = newC.Y - newA.Y;
                            double ndz = newC.Z - newA.Z;
                            double nd = Math.Sqrt(ndx * ndx + ndy * ndy + ndz * ndz);

                            if (nd < bestNewDist - 1e-6)
                            {
                                bestNewDist = nd;
                                bestDx = dx;
                                bestDy = dy;
                                bestDz = dz;
                            }
                        }

                if (bestDx != 0 || bestDy != 0 || bestDz != 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Attractor] Comp {c} ({comp.Count} atoms): " +
                        $"closest {bestCompIdx}↔{bestAttIdx}, " +
                        $"dist {bestDist:F3} → {bestNewDist:F3} Å, " +
                        $"shift ({bestDx},{bestDy},{bestDz})");

                    foreach (int idx in comp)
                    {
                        var atom = structure.Atoms[idx];
                        atom.X += bestDx;
                        atom.Y += bestDy;
                        atom.Z += bestDz;
                        structure.Atoms[idx] = atom;
                    }

                    totalMoved += comp.Count;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Attractor] Comp {c} ({comp.Count} atoms): " +
                        $"closest {bestCompIdx}↔{bestAttIdx}, " +
                        $"dist {bestDist:F3} Å, NO IMPROVEMENT possible");
                }
            }

            return totalMoved;
        }

 /// <summary>
 /// Detects covalent bonds in the structure (list of index pairs).
 /// Uses minimum-image convention, so it catches bonds even between
 /// atomami w different cellch.
 /// </summary>
        public static List<(int i, int j)> DetectBondsInStructure(
            StructureData structure)
        {
            var result = new List<(int, int)>();
            if (structure == null || structure.Atoms == null) return result;

            int n = structure.Atoms.Count;

            double ca = structure.a, cb = structure.b, cc = structure.c;
            double al = structure.alpha * Math.PI / 180.0;
            double be = structure.beta * Math.PI / 180.0;
            double ga = structure.gamma * Math.PI / 180.0;
            double sinG = Math.Sin(ga);
            if (Math.Abs(sinG) < 1e-9) sinG = 1e-9;

            double ax = ca, ay = 0, az = 0;
            double bx = cb * Math.Cos(ga);
            double by = cb * sinG;
            double bz = 0;
            double cx = cc * Math.Cos(be);
            double cy = cc * (Math.Cos(al) - Math.Cos(be) * Math.Cos(ga)) / sinG;
            double cz2 = cc * cc - cx * cx - cy * cy;
            double cz = cz2 > 0 ? Math.Sqrt(cz2) : 0;

            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    var ai = structure.Atoms[i];
                    var aj = structure.Atoms[j];

                    double dx = aj.X - ai.X;
                    double dy = aj.Y - ai.Y;
                    double dz = aj.Z - ai.Z;
                    dx -= Math.Round(dx);
                    dy -= Math.Round(dy);
                    dz -= Math.Round(dz);

                    double px = dx * ax + dy * bx + dz * cx;
                    double py = dx * ay + dy * by + dz * cy;
                    double pz = dx * az + dy * bz + dz * cz;
                    double dist = Math.Sqrt(px * px + py * py + pz * pz);

                    double threshold = CovalentThreshold(
                        ai.AtomicNumber, aj.AtomicNumber);

                    if (dist < threshold)
                        result.Add((i, j));
                }

            return result;
        }

        #endregion

        #region Molecule detection (BFS over bonds)

 /// <summary>
 /// Wykrywa molecule w StructureData przez BFS after bondsch covalent.
 /// Returns an array: [i] = molecule id for each atom (0, 1, 2, ...).
 /// </summary>
        private static int[] DetectMoleculesSimple(StructureData structure)
        {
            int n = structure.Atoms.Count;
            int[] moleculeIds = new int[n];
            for (int i = 0; i < n; i++) moleculeIds[i] = -1;

            double ca = structure.a, cb = structure.b, cc = structure.c;
            double al = structure.alpha * Math.PI / 180.0;
            double be = structure.beta * Math.PI / 180.0;
            double ga = structure.gamma * Math.PI / 180.0;
            double sinG = Math.Sin(ga);
            if (Math.Abs(sinG) < 1e-9) sinG = 1e-9;

            double ax = ca, ay = 0, az = 0;
            double bx = cb * Math.Cos(ga);
            double by = cb * sinG;
            double bz = 0;
            double cx = cc * Math.Cos(be);
            double cy = cc * (Math.Cos(al) - Math.Cos(be) * Math.Cos(ga)) / sinG;
            double cz2 = cc * cc - cx * cx - cy * cy;
            double cz = cz2 > 0 ? Math.Sqrt(cz2) : 0;

            int molId = 0;
            var queue = new Queue<int>();

            for (int start = 0; start < n; start++)
            {
                if (moleculeIds[start] >= 0) continue;

                moleculeIds[start] = molId;
                queue.Clear();
                queue.Enqueue(start);

                while (queue.Count > 0)
                {
                    int cur = queue.Dequeue();
                    var ai = structure.Atoms[cur];

                    for (int j = 0; j < n; j++)
                    {
                        if (moleculeIds[j] >= 0) continue;

                        var aj = structure.Atoms[j];

                        double dx = aj.X - ai.X;
                        double dy = aj.Y - ai.Y;
                        double dz = aj.Z - ai.Z;
                        dx -= Math.Round(dx);
                        dy -= Math.Round(dy);
                        dz -= Math.Round(dz);

                        double px = dx * ax + dy * bx + dz * cx;
                        double py = dx * ay + dy * by + dz * cy;
                        double pz = dx * az + dy * bz + dz * cz;
                        double dist = Math.Sqrt(px * px + py * py + pz * pz);

                        double threshold = CovalentThreshold(
                            ai.AtomicNumber, aj.AtomicNumber);

                        if (dist < threshold)
                        {
                            moleculeIds[j] = molId;
                            queue.Enqueue(j);
                        }
                    }
                }

                molId++;
            }

            return moleculeIds;
        }

 /// <summary>
 /// Distance threshold for a covalent bond between two atoms (Å).
 /// </summary>
        private static double CovalentThreshold(int z1, int z2)
        {
            if ((z1 == 1 && (z2 == 7 || z2 == 8 || z2 == 9)) ||
                (z2 == 1 && (z1 == 7 || z1 == 8 || z1 == 9)))
                return 1.3;

            if (z1 == 1 || z2 == 1)
                return 1.3;

            if (z1 == 6 && z2 == 6)
                return 1.8;

            if ((z1 == 6 && (z2 == 7 || z2 == 8 || z2 == 9)) ||
                (z2 == 6 && (z1 == 7 || z1 == 8 || z1 == 9)))
                return 1.8;

            if (z1 == 16 || z2 == 16)
                return 2.2;

            double r1 = VdwParameters.GetRadius(z1);
            double r2 = VdwParameters.GetRadius(z2);
            double sum = r1 + r2;

            if (sum > 3.0)
                return sum * 0.7;

            return sum * 0.6;
        }

        #endregion

        #region Diagnostics

        public static bool VerifyRoundTrip(int group, string settingA, string settingB)
        {
            var opsA = GetOperationsForSetting(group, settingA);
            var opsB = GetOperationsForSetting(group, settingB);

            if (opsA == null || opsB == null) return false;
            if (opsA.Count != opsB.Count) return false;

            var Q_AB = FindTransformationMatrix(opsA, opsB, group);
            var Q_BA = FindTransformationMatrix(opsB, opsA, group);

            if (!Q_AB.HasValue || !Q_BA.HasValue) return false;

            var composed = ComposeTransforms(Q_BA.Value, Q_AB.Value);

            float[][] testPoints = new[]
            {
                new float[] { 0.13f, 0.27f, 0.41f },
                new float[] { 0.71f, 0.19f, 0.53f },
            };

            foreach (var p in testPoints)
            {
                Vector3 v = new Vector3(p[0], p[1], p[2]);
                Vector3 result = composed.Apply(v);
                for (int axis = 0; axis < 3; axis++)
                {
                    float orig = p[axis];
                    float res = axis == 0 ? result.X
                              : axis == 1 ? result.Y : result.Z;
                    float diff = orig - res;
                    diff = diff - (float)Math.Round(diff);
                    if (Math.Abs(diff) > 1e-3f) return false;
                }
            }
            return true;
        }

        private static Transform ComposeTransforms(Transform T2, Transform T1)
        {
            var result = new Transform
            {
                M11 = T2.M11 * T1.M11 + T2.M12 * T1.M21 + T2.M13 * T1.M31,
                M12 = T2.M11 * T1.M12 + T2.M12 * T1.M22 + T2.M13 * T1.M32,
                M13 = T2.M11 * T1.M13 + T2.M12 * T1.M23 + T2.M13 * T1.M33,

                M21 = T2.M21 * T1.M11 + T2.M22 * T1.M21 + T2.M23 * T1.M31,
                M22 = T2.M21 * T1.M12 + T2.M22 * T1.M22 + T2.M23 * T1.M32,
                M23 = T2.M21 * T1.M13 + T2.M22 * T1.M23 + T2.M23 * T1.M33,

                M31 = T2.M31 * T1.M11 + T2.M32 * T1.M21 + T2.M33 * T1.M31,
                M32 = T2.M31 * T1.M12 + T2.M32 * T1.M22 + T2.M33 * T1.M32,
                M33 = T2.M31 * T1.M13 + T2.M32 * T1.M23 + T2.M33 * T1.M33
            };

            Vector3 t1 = new Vector3(T1.TX, T1.TY, T1.TZ);
            Vector3 rotated = T2.ApplyRotationOnly(t1);
            result.TX = rotated.X + T2.TX;
            result.TY = rotated.Y + T2.TY;
            result.TZ = rotated.Z + T2.TZ;

            return result;
        }

        #endregion
    }
}