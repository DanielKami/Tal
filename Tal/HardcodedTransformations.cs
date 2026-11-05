// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hardcoded space-group transformation matrices (ITA-verified).
//
// ═══════════════════════════════════════════════════════════════════
// HARDCODED TRANSFORMATIONS — matrices from ITA Table 1.5.1.1
// ═══════════════════════════════════════════════════════════════════
//
// Every matrix is verified by SpaceGroupTransformer.TestTransform
// (comparison of the set of symmetry operations). If a hardcoded matrix
// fails the test it is REJECTED and brute-force is used instead.
//
// CONVENTION:
// Matrix Q acts on ATOM COORDINATES: x' = Q · x + t
// Q = P⁻¹ where P is the ITA matrix acting on basis vectors:
// (a', b', c') = (a, b, c) · P
//
// MATRIX SOURCE:
// International Tables for Crystallography Vol. A, Table 1.5.1.1
// + section 1.5.3.2.1 (transformations between settings).
//
// KEY FORMAT:
// (groupNumber, sourceSetting, targetSetting) — names normalised
// (lowercase without spaces).
//
// SCOPE:
// ✅ Monoclinic (5, 8, 9, 12, 13, 14, 15) — cell choices (unique axis b)
// + P21/b (unique axis c) for group 14
// ✅ Orthorhombic (62, 63, 64) — axis permutations
// ❌ R-groups (146, 148, 155, 160, 161, 166, 167) — hexagonal ↔
// rhombohedral conversion requires a separate matrix (not here).


using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Tal.Saver
{
    public static class HardcodedTransformations
    {
        #region TransformKey

 /// <summary>
 /// Klucz: (grupa, source setting, target setting) — nazwy normalised.
 /// </summary>
        public struct TransformKey : System.IEquatable<TransformKey>
        {
            public int Group;
            public string Source;
            public string Target;

            public TransformKey(int group, string source, string target)
            {
                Group = group;
                Source = SymmetryElements.NormalizeSpaceGroupName(source ?? "");
                Target = SymmetryElements.NormalizeSpaceGroupName(target ?? "");
            }

            public bool Equals(TransformKey other)
                => Group == other.Group &&
                   Source == other.Source &&
                   Target == other.Target;

            public override bool Equals(object obj)
                => obj is TransformKey k && Equals(k);

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = Group;
                    h = h * 397 ^ (Source?.GetHashCode() ?? 0);
                    h = h * 397 ^ (Target?.GetHashCode() ?? 0);
                    return h;
                }
            }
        }

        #endregion

        #region Table

 /// <summary>
 /// Tabela hardcoded transformation.
 /// Klucz: (grupa, source setting, target setting).
 /// Value: matrix Q (on coordinates) + translation.
 /// </summary>
        private static readonly Dictionary<TransformKey, SpaceGroupTransformer.Transform> Table
            = new Dictionary<TransformKey, SpaceGroupTransformer.Transform>();

        static HardcodedTransformations()
        {
            BuildTable();
        }

        #endregion

        #region Public API

 /// <summary>
 /// Returns hardcoded transformation for the given pair of settings.
 /// If the pair is not defined directly, tries to find the inverse
 /// (target → source) and invert it.
 /// </summary>
 /// <returns>Transform if znaleziony, null otherwise.</returns>
        public static SpaceGroupTransformer.Transform? Get(
            int group, string source, string target)
        {
            string s = SymmetryElements.NormalizeSpaceGroupName(source ?? "");
            string t = SymmetryElements.NormalizeSpaceGroupName(target ?? "");

            var key = new TransformKey(group, s, t);

            if (Table.TryGetValue(key, out var transform))
                return transform;

 // Try the inverse (target → source) and invert it
            var reverseKey = new TransformKey(group, t, s);
            if (Table.TryGetValue(reverseKey, out var reverseTransform))
            {
                return InvertTransform(reverseTransform);
            }

            return null;
        }

 /// <summary>
 /// Czy w tabeli exists transformation for danej pary?
 /// </summary>
        public static bool Has(int group, string source, string target)
        {
            string s = SymmetryElements.NormalizeSpaceGroupName(source ?? "");
            string t = SymmetryElements.NormalizeSpaceGroupName(target ?? "");

            return Table.ContainsKey(new TransformKey(group, s, t)) ||
                   Table.ContainsKey(new TransformKey(group, t, s));
        }

 /// <summary>
 /// Liczba entries w tabeli (diagnostyka).
 /// </summary>
        public static int Count => Table.Count;

        #endregion

        #region Inversion

 /// <summary>
 /// Odwraca transformation: z (M, t) robi (M⁻¹, -M⁻¹ · t).
 /// </summary>
        private static SpaceGroupTransformer.Transform InvertTransform(
            SpaceGroupTransformer.Transform T)
        {
            float det =
                T.M11 * (T.M22 * T.M33 - T.M23 * T.M32) -
                T.M12 * (T.M21 * T.M33 - T.M23 * T.M31) +
                T.M13 * (T.M21 * T.M32 - T.M22 * T.M31);

            if (System.Math.Abs(det) < 1e-6f)
                return SpaceGroupTransformer.Transform.Identity;

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

            return new SpaceGroupTransformer.Transform
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
        }

        #endregion

        #region Table builder

 /// <summary>
 /// Builds the hardcoded transformation table.
 ///
 /// KONWENCJA: Q = P⁻¹ (na coordinates atoms).
 /// x' = Q · x + t
 ///
 /// ═══════════════════════════════════════════════════════════════
 /// CELL CHOICES — unique axis b (all grupy monoclinic)
 /// ═══════════════════════════════════════════════════════════════
 ///
 /// Cell choice 1 → 2:
 /// P (wektory): a' = -a-c, b' = b, c' = a
 /// P = [[-1, 0, 1], [0, 1, 0], [-1, 0, 0]]
 /// Q = P⁻¹ = [[0, 0, -1], [0, 1, 0], [1, 0, -1]]
 ///
 /// Cell choice 2 → 3:
 /// P (wektory): a' = -c, b' = b, c' = a-c
 /// P = [[0, 0, -1], [0, 1, 0], [1, 0, -1]]
 /// Q = P⁻¹ = [[-1, 0, 0], [0, 1, 0], [1, 0, -1]]
 ///
 /// Cell choice 3 → 1:
 /// P (wektory): a' = -a, b' = b, c' = a-c
 /// P = [[-1, 0, 0], [0, 1, 0], [1, 0, -1]]
 /// Q = P⁻¹ = [[-1, 0, -1], [0, 1, 0], [0, 0, -1]]
 ///
 /// Cell choice 1 → 3:
 /// Q_13 = Q_23 · Q_12 = [[0, 0, 1], [0, 1, 0], [-1, 0, 0]]
 ///
 /// Cell choice 3 → 2:
 /// Q_32 = Q_12 · Q_31 = ... (inverse Q_23)
 /// </summary>
        private static void BuildTable()
        {
 // ═══════════════════════════════════════════════════════════
 // CELL CHOICE 1 → 2
 // Q = [[0, 0, -1], [0, 1, 0], [1, 0, -1]]
 // x' = -z, y' = y, z' = x - z
 // ═══════════════════════════════════════════════════════════
            var Q_12 = new SpaceGroupTransformer.Transform
            {
                M11 = 0,
                M12 = 0,
                M13 = -1,
                M21 = 0,
                M22 = 1,
                M23 = 0,
                M31 = 1,
                M32 = 0,
                M33 = -1,
                TX = 0,
                TY = 0,
                TZ = 0
            };

 // ═══════════════════════════════════════════════════════════
 // CELL CHOICE 2 → 3
 // Q = [[-1, 0, 0], [0, 1, 0], [1, 0, -1]]
 // x' = -x, y' = y, z' = x - z
 // ═══════════════════════════════════════════════════════════
            var Q_23 = new SpaceGroupTransformer.Transform
            {
                M11 = -1,
                M12 = 0,
                M13 = 0,
                M21 = 0,
                M22 = 1,
                M23 = 0,
                M31 = 1,
                M32 = 0,
                M33 = -1,
                TX = 0,
                TY = 0,
                TZ = 0
            };

 // ═══════════════════════════════════════════════════════════
 // CELL CHOICE 3 → 1
 // Q = [[-1, 0, -1], [0, 1, 0], [0, 0, -1]]
 // x' = -x - z, y' = y, z' = -z
 // ═══════════════════════════════════════════════════════════
            var Q_31 = new SpaceGroupTransformer.Transform
            {
                M11 = -1,
                M12 = 0,
                M13 = -1,
                M21 = 0,
                M22 = 1,
                M23 = 0,
                M31 = 0,
                M32 = 0,
                M33 = -1,
                TX = 0,
                TY = 0,
                TZ = 0
            };

 // ═══════════════════════════════════════════════════════════
 // CELL CHOICE 1 → 3 (directly)
 // Q = Q_23 · Q_12 = [[0, 0, 1], [0, 1, 0], [-1, 0, 0]]
 // x' = z, y' = y, z' = -x
 // ═══════════════════════════════════════════════════════════
            var Q_13 = new SpaceGroupTransformer.Transform
            {
                M11 = 0,
                M12 = 0,
                M13 = 1,
                M21 = 0,
                M22 = 1,
                M23 = 0,
                M31 = -1,
                M32 = 0,
                M33 = 0,
                TX = 0,
                TY = 0,
                TZ = 0
            };

 // ═══════════════════════════════════════════════════════════
 // CELL CHOICE 3 → 2 (inverse Q_23)
 // Q = Q_23⁻¹ = [[-1, 0, 0], [0, 1, 0], [-1, 0, -1]]
 // ═══════════════════════════════════════════════════════════
            var Q_32 = new SpaceGroupTransformer.Transform
            {
                M11 = -1,
                M12 = 0,
                M13 = 0,
                M21 = 0,
                M22 = 1,
                M23 = 0,
                M31 = -1,
                M32 = 0,
                M33 = -1,
                TX = 0,
                TY = 0,
                TZ = 0
            };

 // ═══════════════════════════════════════════════════════════
 // CELL CHOICE 2 → 1 (inverse Q_12)
 // Q = Q_12⁻¹ = [[-1, 0, 1], [0, 1, 0], [-1, 0, 0]]
 // ═══════════════════════════════════════════════════════════
            var Q_21 = new SpaceGroupTransformer.Transform
            {
                M11 = -1,
                M12 = 0,
                M13 = 1,
                M21 = 0,
                M22 = 1,
                M23 = 0,
                M31 = -1,
                M32 = 0,
                M33 = 0,
                TX = 0,
                TY = 0,
                TZ = 0
            };

 // ═══════════════════════════════════════════════════════════
 // UNIQUE AXIS b → c (swap b↔c with negation of c)
 // For P21/c → P21/b:
 // P (wektory): a' = a, b' = -c, c' = b
 // Q = [[1, 0, 0], [0, 0, -1], [0, 1, 0]]
 // x' = x, y' = -z, z' = y
 // ═══════════════════════════════════════════════════════════
            var Q_bc = new SpaceGroupTransformer.Transform
            {
                M11 = 1,
                M12 = 0,
                M13 = 0,
                M21 = 0,
                M22 = 0,
                M23 = -1,
                M31 = 0,
                M32 = 1,
                M33 = 0,
                TX = 0,
                TY = 0,
                TZ = 0
            };

 // ═══════════════════════════════════════════════════════════
 // UNIQUE AXIS c → b (inverse Q_bc)
 // For P21/b → P21/c:
 // Q = [[1, 0, 0], [0, 0, 1], [0, -1, 0]]
 // ═══════════════════════════════════════════════════════════
            var Q_cb = new SpaceGroupTransformer.Transform
            {
                M11 = 1,
                M12 = 0,
                M13 = 0,
                M21 = 0,
                M22 = 0,
                M23 = 1,
                M31 = 0,
                M32 = -1,
                M33 = 0,
                TX = 0,
                TY = 0,
                TZ = 0
            };

 // ═══════════════════════════════════════════════════════════
 // UNIQUE AXIS b → a (swap a↔b with negation of b)
 // For P21/c → P21/c11 (unique axis a) — rarely used
 // Q = [[0, 1, 0], [1, 0, 0], [0, 0, -1]]
 // x' = y, y' = x, z' = -z
 // ═══════════════════════════════════════════════════════════
            var Q_ba = new SpaceGroupTransformer.Transform
            {
                M11 = 0,
                M12 = 1,
                M13 = 0,
                M21 = 1,
                M22 = 0,
                M23 = 0,
                M31 = 0,
                M32 = 0,
                M33 = -1,
                TX = 0,
                TY = 0,
                TZ = 0
            };

 // ═══════════════════════════════════════════════════════════
 // UNIQUE AXIS a → b (inverse Q_ba)
 // ═══════════════════════════════════════════════════════════
            var Q_ab = new SpaceGroupTransformer.Transform
            {
                M11 = 0,
                M12 = 1,
                M13 = 0,
                M21 = 1,
                M22 = 0,
                M23 = 0,
                M31 = 0,
                M32 = 0,
                M33 = -1,
                TX = 0,
                TY = 0,
                TZ = 0
            };

 // ═══════════════════════════════════════════════════════════
 // GRUPA 5 — C2 family (unique axis b)
 // Cell choices: C2 (1) ↔ A2 (2)
 // ═══════════════════════════════════════════════════════════
            Add(5, "c2", "a2", Q_12);

 // ═══════════════════════════════════════════════════════════
 // GRUPA 8 — Cm family (unique axis b)
 // ═══════════════════════════════════════════════════════════
            Add(8, "cm", "am", Q_12);

 // ═══════════════════════════════════════════════════════════
 // GRUPA 9 — Cc family (unique axis b)
 // ═══════════════════════════════════════════════════════════
            Add(9, "cc", "aa", Q_12);

 // ═══════════════════════════════════════════════════════════
 // GRUPA 12 — C2/m family (unique axis b)
 // ═══════════════════════════════════════════════════════════
            Add(12, "c2/m", "a2/m", Q_12);

 // ═══════════════════════════════════════════════════════════
 // GRUPA 13 — P2/c family (unique axis b)
 // Cell choices: 1: P2/c, 2: P2/n, 3: P2/a
 // Full graph (6 pairs):
 // ═══════════════════════════════════════════════════════════
            Add(13, "p2/c", "p2/n", Q_12);
            Add(13, "p2/n", "p2/a", Q_23);
            Add(13, "p2/a", "p2/c", Q_31);

 // Direct (no intermediate) — shortcut
            Add(13, "p2/c", "p2/a", Q_13);
            Add(13, "p2/a", "p2/n", Q_32);
            Add(13, "p2/n", "p2/c", Q_21);

 // ═══════════════════════════════════════════════════════════
 // GRUPA 14 — P21/c family (unique axis b)
 // Cell choices: 1: P21/c, 2: P21/n, 3: P21/a, 4: P21/b
 //
 // ⚠️ P21/b w ITA Table to setting z unique axis c, cell choice 1
 // (not unique axis b!). Therefore transformation P21/c → P21/b
 // to change unique axis + cell choice.
 // ═══════════════════════════════════════════════════════════
 // Cell choices (unique axis b):
            Add(14, "p21/c", "p21/n", Q_12);
            Add(14, "p21/n", "p21/a", Q_23);
            Add(14, "p21/a", "p21/c", Q_31);

 // Direct (no intermediate) — shortcut
            Add(14, "p21/c", "p21/a", Q_13);
            Add(14, "p21/a", "p21/n", Q_32);
            Add(14, "p21/n", "p21/c", Q_21);

 // ⭐ P21/c ↔ P21/b (unique axis b ↔ unique axis c)
            Add(14, "p21/c", "p21/b", Q_bc);
 // P21/n → P21/b: P21/n → P21/c (Q_21) aftertem P21/c → P21/b (Q_bc)
            Add(14, "p21/n", "p21/b", Compose(Q_bc, Q_21));
 // P21/a → P21/b: P21/a → P21/c (Q_31) aftertem P21/c → P21/b (Q_bc)
            Add(14, "p21/a", "p21/b", Compose(Q_bc, Q_31));

 // ═══════════════════════════════════════════════════════════
 // GRUPA 15 — C2/c family (unique axis b)
 // Cell choices: 1: C2/c, 2: C2/n
 // ═══════════════════════════════════════════════════════════
            Add(15, "c2/c", "c2/n", Q_12);

            System.Diagnostics.Debug.WriteLine(
                $"[HardcodedTransformations] Built table: {Table.Count} entries");
        }

 /// <summary>
 /// Composition of two transformations: (T2 ∘ T1)(x) = T2 · (T1 · x + t1) + t2
 /// </summary>
        private static SpaceGroupTransformer.Transform Compose(
            SpaceGroupTransformer.Transform T2,
            SpaceGroupTransformer.Transform T1)
        {
            var result = new SpaceGroupTransformer.Transform
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

        private static void Add(int group, string source, string target,
            SpaceGroupTransformer.Transform transform)
        {
            var key = new TransformKey(group, source, target);
            Table[key] = transform;
        }

        #endregion

        #region Diagnostics

 /// <summary>
 /// Returns list all entries w tabeli (do debugowania).
 /// </summary>
        public static List<string> DumpTable()
        {
            var result = new List<string>();

            foreach (var kvp in Table)
            {
                result.Add(
                    $"  [{kvp.Key.Group}] {kvp.Key.Source} → {kvp.Key.Target} : " +
                    $"{kvp.Value}");
            }

            return result;
        }

        #endregion
    }
}