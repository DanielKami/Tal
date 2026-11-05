// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Symmetry element definitions and rendering data.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Tal
{
 /// <summary>
 /// Provides symmetry operations and space group data for crystallographic applications.
 /// Contains a comprehensive database of space group operations, transformation utilities,
 /// and parsing methods for symmetry strings.
 /// </summary>
    public static class SymmetryElements
    {
        #region Public Fields

 /// <summary>Current space group number.</summary>
        public static int group_number;

        #endregion

        #region Static Constructor

        static SymmetryElements()
        {
 // ⭐ Automatycznie wygenerate warianty A2, Am, Aa, A2/m z C2, Cm, Cc, C2/m
            GenerateACenteredVariants();
        }

 /// <summary>
 /// Generatee warianty A-centered (A2, Am, Aa, A2/m) przez transformation
 /// of C-centered operations (C2, Cm, Cc, C2/m) by matrix C → A (cell choice 1 → 2):
 ///
 /// Q = [[0, 0, -1],
 /// [0, 1, 0],
 /// [1, 0, -1]]
 ///
 /// For each grupy:
 /// 1. Find wpis C-centered (orientation = 0).
 /// 2. Sparsuj operacje do ReadSymm.
 /// 3. Conjugate each operation by Q: s' = Q · s · Q⁻¹.
 /// 4. Serialize to strings (x, y, z).
 /// 5. Add new wpis {group, system, "A2", "1", ops...} do tabeli.
 /// </summary>
        private static void GenerateACenteredVariants()
        {
 // ⭐ Maafterwanie: grupa → (nazwa C, nazwa A)
            var variantMap = new Dictionary<int, (string cName, string aName)>
    {
        { 5,  ("C2",    "A2") },
        { 8,  ("Cm",    "Am") },
        { 9,  ("Cc",    "Aa") },
        { 12, ("C2/m",  "A2/m") },
    };

 // ⭐ Macierz Q = P⁻¹ for C → A (cell choice 1 → 2, unique axis b)
 // P (na wektory): a' = -a - c, b' = b, c' = a
 // Q = P⁻¹ (na coordinates): x' = -z, y' = y, z' = x - z
            var Q = new float[,]
            {
        {  0, 0, -1 },
        {  0, 1,  0 },
        {  1, 0, -1 },
            };

 // ⭐ Macierz odwrotna Q⁻¹ = P (do koniugacji: s' = Q · s · Q⁻¹)
            var QInv = new float[,]
            {
        { -1, 0, 1 },
        {  0, 1, 0 },
        { -1, 0, 0 },
            };

            foreach (var kvp in variantMap)
            {
                int group = kvp.Key;
                string cName = kvp.Value.cName;
                string aName = kvp.Value.aName;

 // Find wpis C-centered
                string[] cEntry = null;
                foreach (var entry in SymmetryOperations)
                {
                    if (entry.Length < 4) continue;
                    if (!int.TryParse(entry[0], out int g) || g != group) continue;
                    if (entry[2] != cName) continue;

                    cEntry = entry;
                    break;
                }

                if (cEntry == null) continue;

 // Sparsuj operacje C-centered
                var cOps = new List<ReadSymm>();
                for (int i = 4; i < cEntry.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(cEntry[i])) continue;
                    cOps.Add(RetriveTransformation(cEntry[i]));
                }

 // Conjugate each operation by Q
                var aOpsStrings = new List<string>();
                foreach (var cOp in cOps)
                {
                    var aOp = ConjugateByMatrix(cOp, Q, QInv);
                    aOpsStrings.Add(ReadSymmToString(aOp));
                }

 // Zbuduj new wpis
                var newEntry = new string[4 + aOpsStrings.Count];
                newEntry[0] = group.ToString();
                newEntry[1] = cEntry[1];   // crystal system
                newEntry[2] = aName;
                newEntry[3] = "1";          // orientation 1 (A-centered)
                for (int i = 0; i < aOpsStrings.Count; i++)
                    newEntry[4 + i] = aOpsStrings[i];

                SymmetryOperations.Add(newEntry);

                Debug.WriteLine(
                    $"[Symmetry] Generated A-centered variant: {aName} (group {group}), " +
                    $"ops = {aOpsStrings.Count}");

 // Add to the GroupNameToNumber dictionary
                string normalizedA = NormalizeSpaceGroupName(aName);
                if (!GroupNameToNumber.ContainsKey(normalizedA))
                    GroupNameToNumber[normalizedA] = group;
            }
        }

 /// <summary>
 /// Conjugates a single symmetry operation by matrix Q:
 /// s' = Q · s · Q⁻¹
 /// gdzie Q to matrix 3×3 (without translation).
 /// </summary>
        private static ReadSymm ConjugateByMatrix(ReadSymm s, float[,] Q, float[,] QInv)
        {
 // Zbuduj matrix M_s z ReadSymm
            float[,] Ms = new float[3, 3];
            for (int axis = 0; axis < 3; axis++)
            {
                int p1 = s.pos1[axis];
                int s1 = s.sign1[axis];
                if (p1 >= 0 && p1 < 3 && s1 != 0) Ms[axis, p1] += s1;

                int p2 = s.pos2[axis];
                int s2 = s.sign2[axis];
                if (p2 >= 0 && p2 < 3 && s2 != 0) Ms[axis, p2] += s2;
            }

 // M_s' = Q · M_s · Q⁻¹
            float[,] QMs = MatMul3x3(Q, Ms);
            float[,] MsPrime = MatMul3x3(QMs, QInv);

 // Translation s' = Q · t_s (for simplicity — without conjugating translation,
 // because in this case translations cancel or are preserved)
            Vector3 ts = new Vector3(s.shift[0], s.shift[1], s.shift[2]);
            Vector3 tsPrime = MatVec3x3(Q, ts);

 // Zbuduj new ReadSymm z MsPrime i tsPrime
            var result = new ReadSymm
            {
                pos1 = new int[3],
                sign1 = new int[3],
                pos2 = new int[3],
                sign2 = new int[3],
                shift = new float[3],
                orientation = 1
            };

            for (int axis = 0; axis < 3; axis++)
            {
                ConvertMatrixRowToReadSymm(MsPrime, axis, ref result);

                float sh = axis == 0 ? tsPrime.X : axis == 1 ? tsPrime.Y : tsPrime.Z;
                sh = sh - (float)Math.Round(sh);
                result.shift[axis] = sh;
            }

            return result;
        }

        private static float[,] MatMul3x3(float[,] A, float[,] B)
        {
            float[,] C = new float[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    float sum = 0;
                    for (int k = 0; k < 3; k++)
                        sum += A[i, k] * B[k, j];
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

        private static void ConvertMatrixRowToReadSymm(float[,] M, int row, ref ReadSymm result)
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

        public static string ReadSymmToString(ReadSymm op)
        {
            var sb = new System.Text.StringBuilder();
            string[] vars = { "x", "y", "z" };

            for (int axis = 0; axis < 3; axis++)
            {
                if (axis > 0) sb.Append(",");

                bool wrote = false;

                if (op.sign1[axis] != 0 && op.pos1[axis] >= 0)
                {
                    if (op.sign1[axis] < 0) sb.Append("-");
                    sb.Append(vars[op.pos1[axis]]);
                    wrote = true;
                }

                if (op.sign2[axis] != 0 && op.pos2[axis] >= 0)
                {
                    if (wrote) sb.Append(op.sign2[axis] > 0 ? "+" : "-");
                    else if (op.sign2[axis] < 0) sb.Append("-");
                    sb.Append(vars[op.pos2[axis]]);
                    wrote = true;
                }

                float sh = op.shift[axis];
                if (Math.Abs(sh) > 1e-6f)
                {
                    if (wrote && sh > 0) sb.Append("+");
                    if (sh < 0) { sb.Append("-"); sh = Math.Abs(sh); }

                    if (Math.Abs(sh - 0.5f) < 1e-4f) sb.Append("1/2");
                    else if (Math.Abs(sh - 0.25f) < 1e-4f) sb.Append("1/4");
                    else if (Math.Abs(sh - 0.75f) < 1e-4f) sb.Append("3/4");
                    else sb.Append(sh.ToString("0.0###", CultureInfo.InvariantCulture));

                    wrote = true;
                }

                if (!wrote) sb.Append("0");
            }

            return sb.ToString();
        }

        #endregion

        #region Private Fields

        private static string _spaceGroupName = "";
        private static int _spaceGroupNumber = 0;

        #endregion

        #region Symmetry Operations Database

 /// <summary>
 /// Comprehensive database of space group symmetry operations.
 /// Each entry contains: group number, crystal system, group name, orientation, operations.
 /// </summary>
        public static List<string[]> SymmetryOperations = new List<string[]>
        {
            new string[] {"0","Empty","P0","0",""},
            new string[] {"1","Triclinic","P1","0","x,y,z"},
            new string[] {"2","Triclinic", "P-1","0","x,y,z","-x,-y,-z"},
            new string[] {"3","Monoclinic", "P2","0","x,y,z","-x,y,-z"},
            new string[] {"4", "Monoclinic", "P21", "0","x,y,z","-x,y+1/2,-z"},
            new string[] {"5", "Monoclinic", "C2", "0","x,y,z","-x,y,-z","x+1/2,y+1/2,z","-x+1/2,y+1/2,-z"},
            new string[] {"6", "Monoclinic", "Pm", "0","x,y,z","x,-y,z"},
            new string[] {"7", "Monoclinic", "Pc", "0","x,y,z","x,-y,z+1/2"},
            new string[] {"8", "Monoclinic", "Cm", "0","x,y,z","x,-y,z","x+1/2,y+1/2,z","x+1/2,-y+1/2,z"},
            new string[] {"9", "Monoclinic", "Cc", "0","x,y,z","x,-y,z+1/2","x+1/2,y+1/2,z","x+1/2,-y+1/2,z+1/2"},
            new string[] {"10", "Monoclinic", "P2/m", "0","x,y,z","-x,y,-z","-x,-y,-z","x,-y,z"},
            new string[] {"11", "Monoclinic", "P21/m", "0","x,y,z","-x,y+1/2,-z","-x,-y,-z","x,-y+1/2,z"},
            new string[] {"12", "Monoclinic", "C2/m", "0","x,y,z","-x,y,-z","-x,-y,-z","x,-y,z","x+1/2,y+1/2,z","-x+1/2,y+1/2,-z","-x+1/2,-y+1/2,-z","x+1/2,-y+1/2,z"},
            new string[] {"13", "Monoclinic", "P2/c", "0","x,y,z","-x,y,-z+1/2","-x,-y,-z","x,-y,z+1/2"},
            new string[] {"13", "Monoclinic", "P2/n", "1","x,y,z","-x+1/2,y+1/2,-z","-x,-y,-z","x+1/2,-y+1/2,z"},
            new string[] {"13", "Monoclinic", "P2/a", "2","x,y,z","-x,y,-z","-x,-y,-z","x,-y,z"},
            new string[] { "14", "Monoclinic", "P21/c", "0","x,y,z","-x,y+1/2,-z+1/2", "-x,-y,-z", "x,-y+1/2,z+1/2"},
            new string[] { "14", "Monoclinic", "P21/n", "1", "x,y,z", "-x+1/2,y+1/2,-z+1/2", "-x,-y,-z", "x+1/2,-y+1/2,z+1/2"},
            new string[] { "14", "Monoclinic", "P21/a", "2","x,y,z", "-x+1/2,y+1/2,-z","-x,-y,-z", "x+1/2,-y+1/2,z"},
            new string[] { "14", "Monoclinic", "P21/b", "3", "x,y,z","-x,y+1/2,-z+1/2","-x,-y,-z", "x,-y+1/2,z+1/2"},
            new string[] {"15", "Monoclinic", "C2/c", "0","x,y,z","-x,y,-z+1/2","-x,-y,-z","x,-y,z+1/2","x+1/2,y+1/2,z","-x+1/2,y+1/2,-z+1/2","-x+1/2,-y+1/2,-z","x+1/2,-y+1/2,z+1/2"},
            new string[] {"16","Orthorhombic", "P222","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z"},
            new string[] {"17", "Orthorhombic", "P2221","0","x,y,z","-x,-y,z+1/2","-x,y,-z+1/2","x,-y,-z"},
            new string[] {"18", "Orthorhombic", "P21212","0","x,y,z","-x,-y,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z"},
            new string[] {"18", "Orthorhombic", "P21212","1","x,y,z","-x,-y,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z"},
            new string[] {"19", "Orthorhombic", "P212121","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z"},
            new string[] {"19", "Orthorhombic", "P212121","1","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z"},
            new string[] {"20", "Orthorhombic", "C2221","0","x,y,z","-x,-y,z+1/2","-x,y,-z+1/2","x,-y,-z","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z"},
            new string[] {"21", "Orthorhombic", "C222","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z"},
            new string[] {"22", "Orthorhombic", "F222","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","-x,y+1/2,-z+1/2","x,-y+1/2,-z+1/2","x+1/2,y,z+1/2","-x+1/2,-y,z+1/2","-x+1/2,y,-z+1/2","x+1/2,-y,-z+1/2","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z"},
            new string[] {"23", "Orthorhombic", "I222","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2"},
            new string[] {"24", "Orthorhombic", "I212121","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","x+1/2,y+1/2,z+1/2","-x,-y+1/2,z","-x+1/2,y,-z","x,-y,-z+1/2"},
            new string[] {"25", "Orthorhombic", "Pmm2","0","x,y,z","-x,-y,z","x,-y,z","-x,y,z"},
            new string[] {"26", "Orthorhombic", "Pmc21","0","x,y,z","-x,-y,z+1/2","x,-y,z+1/2","-x,y,z"},
            new string[] {"27", "Orthorhombic", "Pcc2","0","x,y,z","-x,-y,z","x,-y,z+1/2","-x,y,z+1/2"},
            new string[] {"28", "Orthorhombic", "Pma2","0","x,y,z","-x,-y,z","x+1/2,-y,z","-x+1/2,y,z"},
            new string[] {"29", "Orthorhombic", "Pca21","0","x,y,z","-x,-y,z+1/2","x+1/2,-y,z","-x+1/2,y,z+1/2"},
            new string[] {"30", "Orthorhombic", "Pnc2","0","x,y,z","-x,-y,z","x,-y+1/2,z+1/2","-x,y+1/2,z+1/2"},
            new string[] {"31", "Orthorhombic", "Pmn21","0","x,y,z","-x+1/2,-y,z+1/2","x+1/2,-y,z+1/2","-x,y,z"},
            new string[] {"32", "Orthorhombic", "Pba2","0","x,y,z","-x,-y,z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z"},
            new string[] {"33", "Orthorhombic", "Pna21","0","x,y,z","-x,-y,z+1/2","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z+1/2"},
            new string[] {"34", "Orthorhombic", "Pnn2","0","x,y,z","-x,-y,z","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2"},
            new string[] {"35", "Orthorhombic", "Cmm2","0","x,y,z","-x,-y,z","x,-y,z","-x,y,z","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z"},
            new string[] {"36", "Orthorhombic", "Cmc21","0","x,y,z","-x,-y,z+1/2","x,-y,z+1/2","-x,y,z","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z+1/2","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z"},
            new string[] {"37", "Orthorhombic", "Ccc2","0","x,y,z","-x,-y,z","x,-y,z+1/2","-x,y,z+1/2","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2"},
            new string[] {"38", "Orthorhombic", "Amm2","0","x,y,z","-x,-y,z","x,-y,z","-x,y,z","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","x,-y+1/2,z+1/2","-x,y+1/2,z+1/2"},
            new string[] {"39", "Orthorhombic", "Abm2","0","x,y,z","-x,-y,z","x,-y+1/2,z","-x,y+1/2,z","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","x,-y,z+1/2","-x,y,z+1/2"},
            new string[] {"40", "Orthorhombic", "Ama2","0","x,y,z","-x,-y,z","x+1/2,-y,z","-x+1/2,y,z","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2"},
            new string[] {"41", "Orthorhombic", "Aba2","0","x,y,z","-x,-y,z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","x+1/2,-y,z+1/2","-x+1/2,y,z+1/2"},
            new string[] {"42", "Orthorhombic", "Fmm2","0","x,y,z","-x,-y,z","x,-y,z","-x,y,z","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","x,-y+1/2,z+1/2","-x,y+1/2,z+1/2","x+1/2,y,z+1/2","-x+1/2,-y,z+1/2","x+1/2,-y,z+1/2","-x+1/2,y,z+1/2","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z"},
            new string[] {"43", "Orthorhombic", "Fdd2","0","x,y,z","-x,-y,z","x+1/4,-y+1/4,z+1/4","-x+1/4,y+1/4,z+1/4","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","x+1/4,-y+3/4,z+3/4","-x+1/4,y+3/4,z+3/4","x+1/2,y,z+1/2","-x+1/2,-y,z+1/2","x+3/4,-y+1/4,z+3/4","-x+3/4,y+1/4,z+3/4","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","x+3/4,-y+3/4,z+1/4","-x+3/4,y+3/4,z+1/4"},
            new string[] {"44", "Orthorhombic", "Imm2","0","x,y,z","-x,-y,z","x,-y,z","-x,y,z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2"},
            new string[] {"45", "Orthorhombic", "Iba2","0","x,y,z","-x,-y,z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","x,-y,z+1/2","-x,y,z+1/2"},
            new string[] {"46", "Orthorhombic", "Ima2","0","x,y,z","-x,-y,z","x+1/2,-y,z","-x+1/2,y,z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","x,-y+1/2,z+1/2","-x,y+1/2,z+1/2"},
            new string[] {"47", "Orthorhombic", "Pmmm","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","-x,-y,-z","x,y,-z","x,-y,z","-x,y,z"},
            new string[] {"48", "Orthorhombic", "Pnnn","0","x,y,z","-x+1/2,-y+1/2,z","-x+1/2,y,-z+1/2","x,-y+1/2,-z+1/2","-x,-y,-z","x+1/2,y+1/2,-z","x+1/2,-y,z+1/2","-x,y+1/2,z+1/2"},
            new string[] {"49", "Orthorhombic", "Pccm","0","x,y,z","-x,-y,z","-x,y,-z+1/2","x,-y,-z+1/2","-x,-y,-z","x,y,-z","x,-y,z+1/2","-x,y,z+1/2"},
            new string[] {"50", "Orthorhombic", "Pban","0","x,y,z","-x+1/2,-y+1/2,z","-x+1/2,y,-z","x,-y+1/2,-z","-x,-y,-z","x+1/2,y+1/2,-z","x+1/2,-y,z","-x,y+1/2,z"},
            new string[] {"51", "Orthorhombic", "Pmma","0","x,y,z","-x+1/2,-y,z","-x,y,-z","x+1/2,-y,-z","-x,-y,-z","x+1/2,y,-z","x,-y,z","-x+1/2,y,z"},
            new string[] {"52", "Orthorhombic", "Pnna","0","x,y,z","-x+1/2,-y,z","-x+1/2,y+1/2,-z+1/2","x,-y+1/2,-z+1/2","-x,-y,-z","x+1/2,y,-z","x+1/2,-y+1/2,z+1/2","-x,y+1/2,z+1/2"},
            new string[] {"53", "Orthorhombic", "Pmna","0","x,y,z","-x+1/2,-y,z+1/2","-x+1/2,y,-z+1/2","x,-y,-z","-x,-y,-z","x+1/2,y,-z+1/2","x+1/2,-y,z+1/2","-x,y,z"},
            new string[] {"54", "Orthorhombic", "Pcca","0","x,y,z","-x+1/2,-y,z","-x,y,-z+1/2","x+1/2,-y,-z+1/2","-x,-y,-z","x+1/2,y,-z","x,-y,z+1/2","-x+1/2,y,z+1/2"},
            new string[] {"55", "Orthorhombic", "Pbam","0","x,y,z","-x,-y,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","-x,-y,-z","x,y,-z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z"},
            new string[] {"56", "Orthorhombic", "Pccn","0","x,y,z","-x+1/2,-y+1/2,z","-x,y+1/2,-z+1/2","x+1/2,-y,-z+1/2","-x,-y,-z","x+1/2,y+1/2,-z","x,-y+1/2,z+1/2","-x+1/2,y,z+1/2"},
            new string[] {"57", "Orthorhombic", "Pbcm","0","x,y,z","-x,-y,z+1/2","-x,y+1/2,-z+1/2","x,-y+1/2,-z","-x,-y,-z","x,y,-z+1/2","x,-y+1/2,z+1/2","-x,y+1/2,z"},
            new string[] {"58", "Orthorhombic", "Pnnm","0","x,y,z","-x,-y,z","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","-x,-y,-z","x,y,-z","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2"},
            new string[] {"59", "Orthorhombic", "Pmmn","0","x,y,z","-x+1/2,-y+1/2,z","-x,y+1/2,-z","x+1/2,-y,-z","-x,-y,-z","x+1/2,y+1/2,-z","x,-y+1/2,z","-x+1/2,y,z"},
            new string[] {"60", "Orthorhombic", "Pbcn","0","x,y,z","-x+1/2,-y+1/2,z+1/2","-x,y,-z+1/2","x+1/2,-y+1/2,-z","-x,-y,-z","x+1/2,y+1/2,-z+1/2","x,-y,z+1/2","-x+1/2,y+1/2,z"},
            new string[] {"61", "Orthorhombic", "Pbca","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","-x,-y,-z","x+1/2,y,-z+1/2","x,-y+1/2,z+1/2","-x+1/2,y+1/2,z"},
            new string[] {"62", "Orthorhombic", "Pnma","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z","x+1/2,-y+1/2,-z+1/2","-x,-y,-z","x+1/2,y,-z+1/2","x,-y+1/2,z","-x+1/2,y+1/2,z+1/2"},
            new string[] {"62", "Orthorhombic", "Pnam","1","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z","x+1/2,-y+1/2,-z+1/2"},
            new string[] {"63", "Orthorhombic", "Cmcm","0","x,y,z","-x,-y,z+1/2","-x,y,-z+1/2","x,-y,-z","-x,-y,-z","x,y,-z+1/2","x,-y,z+1/2","-x,y,z","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","-x+1/2,-y+1/2,-z","x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z"},
            new string[] {"64", "Orthorhombic", "Cmca","0","x,y,z","-x,-y+1/2,z+1/2","-x,y+1/2,-z+1/2","x,-y,-z","-x,-y,-z","x,y+1/2,-z+1/2","x,-y+1/2,z+1/2","-x,y,z","x+1/2,y+1/2,z","-x+1/2,-y,z+1/2","-x+1/2,y,-z+1/2","x+1/2,-y+1/2,-z","-x+1/2,-y+1/2,-z","x+1/2,y,-z+1/2","x+1/2,-y,z+1/2","-x+1/2,y+1/2,z"},
            new string[] {"65", "Orthorhombic", "Cmmm","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","-x,-y,-z","x,y,-z","x,-y,z","-x,y,z","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","-x+1/2,-y+1/2,-z","x+1/2,y+1/2,-z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z"},
            new string[] {"66", "Orthorhombic", "Cccm","0","x,y,z","-x,-y,z","-x,y,-z+1/2","x,-y,-z+1/2","-x,-y,-z","x,y,-z","x,-y,z+1/2","-x,y,z+1/2","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","-x+1/2,-y+1/2,-z","x+1/2,y+1/2,-z","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2"},
            new string[] {"67", "Orthorhombic", "Cmma","0","x,y,z","-x,-y+1/2,z","-x,y+1/2,-z","x,-y,-z","-x,-y,-z","x,y+1/2,-z","x,-y+1/2,z","-x,y,z","x+1/2,y+1/2,z","-x+1/2,-y,z","-x+1/2,y,-z","x+1/2,-y+1/2,-z","-x+1/2,-y+1/2,-z","x+1/2,y,-z","x+1/2,-y,z","-x+1/2,y+1/2,z"},
            new string[] {"68", "Orthorhombic", "Ccca","0","x,y,z","-x+1/2,-y,z","-x,y,-z+1/2","x+1/2,-y,-z+1/2","-x,-y,-z","x+1/2,y,-z","x,-y,z+1/2","-x+1/2,y,z+1/2","x+1/2,y+1/2,z","-x,-y+1/2,z","-x+1/2,y+1/2,-z+1/2","x,-y+1/2,-z+1/2","-x+1/2,-y+1/2,-z","x,y+1/2,-z","x+1/2,-y+1/2,z+1/2","-x,y+1/2,z+1/2"},
            new string[] {"69", "Orthorhombic", "Fmmm","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","-x,-y,-z","x,y,-z","x,-y,z","-x,y,z","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","-x,y+1/2,-z+1/2","x,-y+1/2,-z+1/2","-x,-y+1/2,-z+1/2","x,y+1/2,-z+1/2","x,-y+1/2,z+1/2","-x,y+1/2,z+1/2","x+1/2,y,z+1/2","-x+1/2,-y,z+1/2","-x+1/2,y,-z+1/2","x+1/2,-y,-z+1/2","-x+1/2,-y,-z+1/2","x+1/2,y,-z+1/2","x+1/2,-y,z+1/2","-x+1/2,y,z+1/2","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","-x+1/2,-y+1/2,-z","x+1/2,y+1/2,-z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z"},
            new string[] {"70", "Orthorhombic", "Fddd","0","x,y,z","-x+3/4,-y+3/4,z","-x+3/4,y,-z+3/4","x,-y+3/4,-z+3/4","-x,-y,-z","x+1/4,y+1/4,-z","x+1/4,-y,z+1/4","-x,y+1/4,z+1/4","x,y+1/2,z+1/2","-x+3/4,-y+1/4,z+1/2","-x+3/4,y+1/2,-z+1/4","x,-y+1/4,-z+1/4","-x,-y+1/2,-z+1/2","x+1/4,y+3/4,-z+1/2","x+1/4,-y+1/2,z+3/4","-x,y+3/4,z+3/4","x+1/2,y,z+1/2","-x+1/4,-y+3/4,z+1/2","-x+1/4,y,-z+1/4","x+1/2,-y+3/4,-z+1/4","-x+1/2,-y,-z+1/2","x+3/4,y+1/4,-z+1/2","x+3/4,-y,z+3/4","-x+1/2,y+1/4,z+3/4","x+1/2,y+1/2,z","-x+1/4,-y+1/4,z","-x+1/4,y+1/2,-z+3/4","x+1/2,-y+1/4,-z+3/4","-x+1/2,-y+1/2,-z","x+3/4,y+3/4,-z","x+3/4,-y+1/2,z+1/4","-x+1/2,y+3/4,z+1/4"},
            new string[] {"71", "Orthorhombic", "Immm","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","-x,-y,-z","x,y,-z","x,-y,z","-x,y,z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","-x+1/2,-y+1/2,-z+1/2","x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2"},
            new string[] {"72", "Orthorhombic", "Ibam","0","x,y,z","-x,-y,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","-x,-y,-z","x,y,-z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-x,y,-z+1/2","x,-y,-z+1/2","-x+1/2,-y+1/2,-z+1/2","x+1/2,y+1/2,-z+1/2","x,-y,z+1/2","-x,y,z+1/2"},
            new string[] {"73", "Orthorhombic", "Ibca","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","-x,-y,-z","x+1/2,y,-z+1/2","x,-y+1/2,z+1/2","-x+1/2,y+1/2,z","x+1/2,y+1/2,z+1/2","-x,-y+1/2,z","-x+1/2,y,-z","x,-y,-z+1/2","-x+1/2,-y+1/2,-z+1/2","x,y+1/2,-z","x+1/2,-y,z","-x,y,z+1/2"},
            new string[] {"74", "Orthorhombic", "Imma","0","x,y,z","-x,-y+1/2,z","-x,y+1/2,-z","x,-y,-z","-x,-y,-z","x,y+1/2,-z","x,-y+1/2,z","-x,y,z","x+1/2,y+1/2,z+1/2","-x+1/2,-y,z+1/2","-x+1/2,y,-z+1/2","x+1/2,-y+1/2,-z+1/2","-x+1/2,-y+1/2,-z+1/2","x+1/2,y,-z+1/2","x+1/2,-y,z+1/2","-x+1/2,y+1/2,z+1/2"},
            new string[] {"75", "Tetragonal", "P4","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z"},
            new string[] {"76", "Tetragonal", "P41","0","x,y,z","-x,-y,z+1/2","-y,x,z+1/4","y,-x,z+3/4"},
            new string[] {"77", "Tetragonal", "P42","0","x,y,z","-x,-y,z","-y,x,z+1/2","y,-x,z+1/2"},
            new string[] {"78", "Tetragonal", "P43","0","x,y,z","-x,-y,z+1/2","-y,x,z+3/4","y,-x,z+1/4"},
            new string[] {"79", "Tetragonal", "I4","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-y+1/2,x+1/2,z+1/2","y+1/2,-x+1/2,z+1/2"},
            new string[] {"80", "Tetragonal", "I41","0","x,y,z","-x+1/2,-y+1/2,z+1/2","-y,x+1/2,z+1/4","y+1/2,-x,z+3/4","x+1/2,y+1/2,z+1/2","-x,-y,z","-y+1/2,x,z+3/4","y,-x+1/2,z+1/4"},
            new string[] {"81", "Tetragonal", "P-4","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z"},
            new string[] {"82", "Tetragonal", "I-4","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","y+1/2,-x+1/2,-z+1/2","-y+1/2,x+1/2,-z+1/2"},
            new string[] {"83", "Tetragonal", "P4/m","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","-x,-y,-z","x,y,-z","y,-x,-z","-y,x,-z"},
            new string[] {"84", "Tetragonal", "P42/m","0","x,y,z","-x,-y,z","-y,x,z+1/2","y,-x,z+1/2","-x,-y,-z","x,y,-z","y,-x,-z+1/2","-y,x,-z+1/2"},
            new string[] {"85", "Tetragonal", "P4/n","0","x,y,z","-x+1/2,-y+1/2,z","-y+1/2,x,z","y,-x+1/2,z","-x,-y,-z","x+1/2,y+1/2,-z","y+1/2,-x,-z","-y,x+1/2,-z"},
            new string[] {"86", "Tetragonal", "P42/n","0","x,y,z","-x+1/2,-y+1/2,z","-y,x+1/2,z+1/2","y+1/2,-x,z+1/2","-x,-y,-z","x+1/2,y+1/2,-z","y,-x+1/2,-z+1/2","-y+1/2,x,-z+1/2"},
            new string[] {"87", "Tetragonal", "I4/m","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","-x,-y,-z","x,y,-z","y,-x,-z","-y,x,-z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-y+1/2,x+1/2,z+1/2","y+1/2,-x+1/2,z+1/2","-x+1/2,-y+1/2,-z+1/2","x+1/2,y+1/2,-z+1/2","y+1/2,-x+1/2,-z+1/2","-y+1/2,x+1/2,-z+1/2"},
            new string[] {"88", "Tetragonal", "I41/a","0","x,y,z","-x+1/2,-y,z+1/2","-y+3/4,x+1/4,z+1/4","y+3/4,-x+3/4,z+3/4","-x,-y,-z","x+1/2,y,-z+1/2","y+1/4,-x+3/4,-z+3/4","-y+1/4,x+1/4,-z+1/4","x+1/2,y+1/2,z+1/2","-x,-y+1/2,z","-y+1/4,x+3/4,z+3/4","y+1/4,-x+1/4,z+1/4","-x+1/2,-y+1/2,-z+1/2","x,y+1/2,-z","y+3/4,-x+1/4,-z+1/4","-y+3/4,x+3/4,-z+3/4"},
            new string[] {"89", "Tetragonal", "P422","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","-x,y,-z","x,-y,-z","y,x,-z","-y,-x,-z"},
            new string[] {"90", "Tetragonal", "P4212","0","x,y,z","-x,-y,z","-y+1/2,x+1/2,z","y+1/2,-x+1/2,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","y,x,-z","-y,-x,-z"},
            new string[] {"91", "Tetragonal", "P4122","0","x,y,z","-x,-y,z+1/2","-y,x,z+1/4","y,-x,z+3/4","-x,y,-z","x,-y,-z+1/2","y,x,-z+3/4","-y,-x,-z+1/4"},
            new string[] {"92", "Tetragonal", "P41212","0","x,y,z","-x,-y,z+1/2","-y+1/2,x+1/2,z+1/4","y+1/2,-x+1/2,z+3/4","-x+1/2,y+1/2,-z+1/4","x+1/2,-y+1/2,-z+3/4","y,x,-z","-y,-x,-z+1/2"},
            new string[] {"93", "Tetragonal", "P4222","0","x,y,z","-x,-y,z","-y,x,z+1/2","y,-x,z+1/2","-x,y,-z","x,-y,-z","y,x,-z+1/2","-y,-x,-z+1/2"},
            new string[] {"94", "Tetragonal", "P42212","0","x,y,z","-x,-y,z","-y+1/2,x+1/2,z+1/2","y+1/2,-x+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","y,x,-z","-y,-x,-z"},
            new string[] {"95", "Tetragonal", "P4322","0","x,y,z","-x,-y,z+1/2","-y,x,z+3/4","y,-x,z+1/4","-x,y,-z","x,-y,-z+1/2","y,x,-z+1/4","-y,-x,-z+3/4"},
            new string[] {"96", "Tetragonal", "P43212","0","x,y,z","-x,-y,z+1/2","-y+1/2,x+1/2,z+3/4","y+1/2,-x+1/2,z+1/4","-x+1/2,y+1/2,-z+3/4","x+1/2,-y+1/2,-z+1/4","y,x,-z","-y,-x,-z+1/2"},
            new string[] {"97", "Tetragonal", "I422","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","-x,y,-z","x,-y,-z","y,x,-z","-y,-x,-z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-y+1/2,x+1/2,z+1/2","y+1/2,-x+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","y+1/2,x+1/2,-z+1/2","-y+1/2,-x+1/2,-z+1/2"},
            new string[] {"98", "Tetragonal", "I4122","0","x,y,z","-x+1/2,-y+1/2,z+1/2","-y,x+1/2,z+1/4","y+1/2,-x,z+3/4","-x+1/2,y,-z+3/4","x,-y+1/2,-z+1/4","y+1/2,x+1/2,-z+1/2","-y,-x,-z","x+1/2,y+1/2,z+1/2","-x,-y,z","-y+1/2,x,z+3/4","y,-x+1/2,z+1/4","-x,y+1/2,-z+1/4","x+1/2,-y,-z+3/4","y,x,-z","-y+1/2,-x+1/2,-z+1/2"},
            new string[] {"99", "Tetragonal", "P4mm","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","x,-y,z","-x,y,z","-y,-x,z","y,x,z"},
            new string[] {"100", "Tetragonal", "P4bm","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","-y+1/2,-x+1/2,z","y+1/2,x+1/2,z"},
            new string[] {"101", "Tetragonal", "P42cm","0","x,y,z","-x,-y,z","-y,x,z+1/2","y,-x,z+1/2","x,-y,z+1/2","-x,y,z+1/2","-y,-x,z","y,x,z"},
            new string[] {"102", "Tetragonal", "P42nm","0","x,y,z","-x,-y,z","-y+1/2,x+1/2,z+1/2","y+1/2,-x+1/2,z+1/2","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2","-y,-x,z","y,x,z"},
            new string[] {"103", "Tetragonal", "P4cc","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","x,-y,z+1/2","-x,y,z+1/2","-y,-x,z+1/2","y,x,z+1/2"},
            new string[] {"104", "Tetragonal", "P4nc","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2","-y+1/2,-x+1/2,z+1/2","y+1/2,x+1/2,z+1/2"},
            new string[] {"105", "Tetragonal", "P42mc","0","x,y,z","-x,-y,z","-y,x,z+1/2","y,-x,z+1/2","x,-y,z","-x,y,z","-y,-x,z+1/2","y,x,z+1/2"},
            new string[] {"106", "Tetragonal", "P42bc","0","x,y,z","-x,-y,z","-y,x,z+1/2","y,-x,z+1/2","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","-y+1/2,-x+1/2,z+1/2","y+1/2,x+1/2,z+1/2"},
            new string[] {"107", "Tetragonal", "I4mm","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","x,-y,z","-x,y,z","-y,-x,z","y,x,z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-y+1/2,x+1/2,z+1/2","y+1/2,-x+1/2,z+1/2","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2","-y+1/2,-x+1/2,z+1/2","y+1/2,x+1/2,z+1/2"},
            new string[] {"108", "Tetragonal", "I4cm","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","x,-y,z+1/2","-x,y,z+1/2","-y,-x,z+1/2","y,x,z+1/2","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-y+1/2,x+1/2,z+1/2","y+1/2,-x+1/2,z+1/2","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","-y+1/2,-x+1/2,z","y+1/2,x+1/2,z"},
            new string[] {"109", "Tetragonal", "I41md","0","x,y,z","-x+1/2,-y+1/2,z+1/2","-y,x+1/2,z+1/4","y+1/2,-x,z+3/4","x,-y,z","-x+1/2,y+1/2,z+1/2","-y,-x+1/2,z+1/4","y+1/2,x,z+3/4","x+1/2,y+1/2,z+1/2","-x,-y,z","-y+1/2,x,z+3/4","y,-x+1/2,z+1/4","x+1/2,-y+1/2,z+1/2","-x,y,z","-y+1/2,-x,z+3/4","y,x+1/2,z+1/4"},
            new string[] {"110", "Tetragonal", "I41cd","0","x,y,z","-x+1/2,-y+1/2,z+1/2","-y,x+1/2,z+1/4","y+1/2,-x,z+3/4","x,-y,z+1/2","-x+1/2,y+1/2,z","-y,-x+1/2,z+3/4","y+1/2,x,z+1/4","x+1/2,y+1/2,z+1/2","-x,-y,z","-y+1/2,x,z+3/4","y,-x+1/2,z+1/4","x+1/2,-y+1/2,z","-x,y,z+1/2","-y+1/2,-x,z+1/4","y,x+1/2,z+3/4"},
            new string[] {"111", "Tetragonal", "P-42m","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","-x,y,-z","x,-y,-z","-y,-x,z","y,x,z"},
            new string[] {"112", "Tetragonal", "P-42c","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","-x,y,-z+1/2","x,-y,-z+1/2","-y,-x,z+1/2","y,x,z+1/2"},
            new string[] {"113", "Tetragonal", "P-421m","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","-y+1/2,-x+1/2,z","y+1/2,x+1/2,z"},
            new string[] {"114", "Tetragonal", "P-421c","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","-y+1/2,-x+1/2,z+1/2","y+1/2,x+1/2,z+1/2"},
            new string[] {"115", "Tetragonal", "P-4m2","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","x,-y,z","-x,y,z","y,x,-z","-y,-x,-z"},
            new string[] {"116", "Tetragonal", "P-4c2","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","x,-y,z+1/2","-x,y,z+1/2","y,x,-z+1/2","-y,-x,-z+1/2"},
            new string[] {"117", "Tetragonal", "P-4b2","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","y+1/2,x+1/2,-z","-y+1/2,-x+1/2,-z"},
            new string[] {"118", "Tetragonal", "P-4n2","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2","y+1/2,x+1/2,-z+1/2","-y+1/2,-x+1/2,-z+1/2"},
            new string[] {"119", "Tetragonal", "I-4m2","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","x,-y,z","-x,y,z","y,x,-z","-y,-x,-z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","y+1/2,-x+1/2,-z+1/2","-y+1/2,x+1/2,-z+1/2","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2","y+1/2,x+1/2,-z+1/2","-y+1/2,-x+1/2,-z+1/2"},
            new string[] {"120", "Tetragonal", "I-4c2","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","x,-y,z+1/2","-x,y,z+1/2","y,x,-z+1/2","-y,-x,-z+1/2","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","y+1/2,-x+1/2,-z+1/2","-y+1/2,x+1/2,-z+1/2","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","y+1/2,x+1/2,-z","-y+1/2,-x+1/2,-z"},
            new string[] {"121", "Tetragonal", "I-42m","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","-x,y,-z","x,-y,-z","-y,-x,z","y,x,z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","y+1/2,-x+1/2,-z+1/2","-y+1/2,x+1/2,-z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","-y+1/2,-x+1/2,z+1/2","y+1/2,x+1/2,z+1/2"},
            new string[] {"122", "Tetragonal", "I-42d","0","x,y,z","-x,-y,z","y,-x,-z","-y,x,-z","-x+1/2,y,-z+3/4","x+1/2,-y,-z+3/4","-y+1/2,-x,z+3/4","y+1/2,x,z+3/4","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","y+1/2,-x+1/2,-z+1/2","-y+1/2,x+1/2,-z+1/2","-x,y+1/2,-z+1/4","x,-y+1/2,-z+1/4","-y,-x+1/2,z+1/4","y,x+1/2,z+1/4"},
            new string[] {"123", "Tetragonal", "P4/mmm","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","-x,y,-z","x,-y,-z","y,x,-z","-y,-x,-z","-x,-y,-z","x,y,-z","y,-x,-z","-y,x,-z","x,-y,z","-x,y,z","-y,-x,z","y,x,z"},
            new string[] {"124", "Tetragonal", "P4/mcc","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","-x,y,-z+1/2","x,-y,-z+1/2","y,x,-z+1/2","-y,-x,-z+1/2","-x,-y,-z","x,y,-z","y,-x,-z","-y,x,-z","x,-y,z+1/2","-x,y,z+1/2","-y,-x,z+1/2","y,x,z+1/2"},
            new string[] {"125", "Tetragonal", "P4/nbm","0","x,y,z","-x+1/2,-y+1/2,z","-y+1/2,x,z","y,-x+1/2,z","-x+1/2,y,-z","x,-y+1/2,-z","y,x,-z","-y+1/2,-x+1/2,-z","-x,-y,-z","x+1/2,y+1/2,-z","y+1/2,-x,-z","-y,x+1/2,-z","x+1/2,-y,z","-x,y+1/2,z","-y,-x,z","y+1/2,x+1/2,z"},
            new string[] {"126", "Tetragonal", "P4/nnc","0","x,y,z","-x+1/2,-y+1/2,z","-y+1/2,x,z","y,-x+1/2,z","-x+1/2,y,-z+1/2","x,-y+1/2,-z+1/2","y,x,-z+1/2","-y+1/2,-x+1/2,-z+1/2","-x,-y,-z","x+1/2,y+1/2,-z","y+1/2,-x,-z","-y,x+1/2,-z","x+1/2,-y,z+1/2","-x,y+1/2,z+1/2","-y,-x,z+1/2","y+1/2,x+1/2,z+1/2"},
            new string[] {"127", "Tetragonal", "P4/mbm","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","y+1/2,x+1/2,-z","-y+1/2,-x+1/2,-z","-x,-y,-z","x,y,-z","y,-x,-z","-y,x,-z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","-y+1/2,-x+1/2,z","y+1/2,x+1/2,z"},
            new string[] {"128", "Tetragonal", "P4/mnc","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","y+1/2,x+1/2,-z+1/2","-y+1/2,-x+1/2,-z+1/2","-x,-y,-z","x,y,-z","y,-x,-z","-y,x,-z","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2","-y+1/2,-x+1/2,z+1/2","y+1/2,x+1/2,z+1/2"},
            new string[] {"129", "Tetragonal", "P4/nmm","0","x,y,z","-x+1/2,-y+1/2,z","-y+1/2,x,z","y,-x+1/2,z","-x,y+1/2,-z","x+1/2,-y,-z","y+1/2,x+1/2,-z","-y,-x,-z","-x,-y,-z","x+1/2,y+1/2,-z","y+1/2,-x,-z","-y,x+1/2,-z","x,-y+1/2,z","-x+1/2,y,z","-y+1/2,-x+1/2,z","y,x,z"},
            new string[] {"130", "Tetragonal", "P4/ncc","0","x,y,z","-x+1/2,-y+1/2,z","-y+1/2,x,z","y,-x+1/2,z","-x,y+1/2,-z+1/2","x+1/2,-y,-z+1/2","y+1/2,x+1/2,-z+1/2","-y,-x,-z+1/2","-x,-y,-z","x+1/2,y+1/2,-z","y+1/2,-x,-z","-y,x+1/2,-z","x,-y+1/2,z+1/2","-x+1/2,y,z+1/2","-y+1/2,-x+1/2,z+1/2","y,x,z+1/2"},
            new string[] {"131", "Tetragonal", "P42/mmc","0","x,y,z","-x,-y,z","-y,x,z+1/2","y,-x,z+1/2","-x,y,-z","x,-y,-z","y,x,-z+1/2","-y,-x,-z+1/2","-x,-y,-z","x,y,-z","y,-x,-z+1/2","-y,x,-z+1/2","x,-y,z","-x,y,z","-y,-x,z+1/2","y,x,z+1/2"},
            new string[] {"132", "Tetragonal", "P42/mcm","0","x,y,z","-x,-y,z","-y,x,z+1/2","y,-x,z+1/2","-x,y,-z+1/2","x,-y,-z+1/2","y,x,-z","-y,-x,-z","-x,-y,-z","x,y,-z","y,-x,-z+1/2","-y,x,-z+1/2","x,-y,z+1/2","-x,y,z+1/2","-y,-x,z","y,x,z"},
            new string[] {"133", "Tetragonal", "P42/nbc","0","x,y,z","-x+1/2,-y+1/2,z","-y+1/2,x,z+1/2","y,-x+1/2,z+1/2","-x+1/2,y,-z","x,-y+1/2,-z","y,x,-z+1/2","-y+1/2,-x+1/2,-z+1/2","-x,-y,-z","x+1/2,y+1/2,-z","y+1/2,-x,-z+1/2","-y,x+1/2,-z+1/2","x+1/2,-y,z","-x,y+1/2,z","-y,-x,z+1/2","y+1/2,x+1/2,z+1/2"},
            new string[] {"134", "Tetragonal", "P42/nnm","0","x,y,z","-x+1/2,-y+1/2,z","-y+1/2,x,z+1/2","y,-x+1/2,z+1/2","-x+1/2,y,-z+1/2","x,-y+1/2,-z+1/2","y,x,-z","-y+1/2,-x+1/2,-z","-x,-y,-z","x+1/2,y+1/2,-z","y+1/2,-x,-z+1/2","-y,x+1/2,-z+1/2","x+1/2,-y,z+1/2","-x,y+1/2,z+1/2","-y,-x,z","y+1/2,x+1/2,z"},
            new string[] {"135", "Tetragonal", "P42/mbc","0","x,y,z","-x,-y,z","-y,x,z+1/2","y,-x,z+1/2","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","y+1/2,x+1/2,-z+1/2","-y+1/2,-x+1/2,-z+1/2","-x,-y,-z","x,y,-z","y,-x,-z+1/2","-y,x,-z+1/2","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","-y+1/2,-x+1/2,z+1/2","y+1/2,x+1/2,z+1/2"},
            new string[] {"136", "Tetragonal", "P42/mnm","0","x,y,z","-x,-y,z","-y+1/2,x+1/2,z+1/2","y+1/2,-x+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","y,x,-z","-y,-x,-z","-x,-y,-z","x,y,-z","y+1/2,-x+1/2,-z+1/2","-y+1/2,x+1/2,-z+1/2","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2","-y,-x,z","y,x,z"},
            new string[] {"137", "Tetragonal", "P42/nmc","0","x,y,z","-x+1/2,-y+1/2,z","-y+1/2,x,z+1/2","y,-x+1/2,z+1/2","-x,y+1/2,-z","x+1/2,-y,-z","y+1/2,x+1/2,-z+1/2","-y,-x,-z+1/2","-x,-y,-z","x+1/2,y+1/2,-z","y+1/2,-x,-z+1/2","-y,x+1/2,-z+1/2","x,-y+1/2,z","-x+1/2,y,z","-y+1/2,-x+1/2,z+1/2","y,x,z+1/2"},
            new string[] {"138", "Tetragonal", "P42/ncm","0","x,y,z","-x+1/2,-y+1/2,z","-y+1/2,x,z+1/2","y,-x+1/2,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y,-z+1/2","y+1/2,x+1/2,-z","-y,-x,-z","-x,-y,-z","x+1/2,y+1/2,-z","y+1/2,-x,-z+1/2","-y,x+1/2,-z+1/2","x,-y+1/2,z+1/2","-x+1/2,y,z+1/2","-y+1/2,-x+1/2,z","y,x,z"},
            new string[] {"139", "Tetragonal", "I4/mmm","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","-x,y,-z","x,-y,-z","y,x,-z","-y,-x,-z","-x,-y,-z","x,y,-z","y,-x,-z","-y,x,-z","x,-y,z","-x,y,z","-y,-x,z","y,x,z","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-y+1/2,x+1/2,z+1/2","y+1/2,-x+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","y+1/2,x+1/2,-z+1/2","-y+1/2,-x+1/2,-z+1/2","-x+1/2,-y+1/2,-z+1/2","x+1/2,y+1/2,-z+1/2","y+1/2,-x+1/2,-z+1/2","-y+1/2,x+1/2,-z+1/2","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2","-y+1/2,-x+1/2,z+1/2","y+1/2,x+1/2,z+1/2"},
            new string[] {"140", "Tetragonal", "I4/mcm","0","x,y,z","-x,-y,z","-y,x,z","y,-x,z","-x,y,-z+1/2","x,-y,-z+1/2","y,x,-z+1/2","-y,-x,-z+1/2","-x,-y,-z","x,y,-z","y,-x,-z","-y,x,-z","x,-y,z+1/2","-x,y,z+1/2","-y,-x,z+1/2","y,x,z+1/2","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-y+1/2,x+1/2,z+1/2","y+1/2,-x+1/2,z+1/2","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","y+1/2,x+1/2,-z","-y+1/2,-x+1/2,-z","-x+1/2,-y+1/2,-z+1/2","x+1/2,y+1/2,-z+1/2","y+1/2,-x+1/2,-z+1/2","-y+1/2,x+1/2,-z+1/2","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","-y+1/2,-x+1/2,z","y+1/2,x+1/2,z"},
            new string[] {"141", "Tetragonal", "I41/amd","0","x,y,z","-x+1/2,-y,z+1/2","-y+1/4,x+3/4,z+1/4","y+1/4,-x+1/4,z+3/4","-x+1/2,y,-z+1/2","x,-y,-z","y+1/4,x+3/4,-z+1/4","-y+1/4,-x+1/4,-z+3/4","-x,-y,-z","x+1/2,y,-z+1/2","y+3/4,-x+1/4,-z+3/4","-y+3/4,x+3/4,-z+1/4","x+1/2,-y,z+1/2","-x,y,z","-y+3/4,-x+1/4,z+3/4","y+3/4,x+3/4,z+1/4","x+1/2,y+1/2,z+1/2","-x,-y+1/2,z","-y+3/4,x+1/4,z+3/4","y+3/4,-x+3/4,z+1/4","-x,y+1/2,-z","x+1/2,-y+1/2,-z+1/2","y+3/4,x+1/4,-z+3/4","-y+3/4,-x+3/4,-z+1/4","-x+1/2,-y+1/2,-z+1/2","x,y+1/2,-z","y+1/4,-x+3/4,-z+1/4","-y+1/4,x+1/4,-z+3/4","x,-y+1/2,z","-x+1/2,y+1/2,z+1/2","-y+1/4,-x+3/4,z+1/4","y+1/4,x+1/4,z+3/4"},
            new string[] {"142", "Tetragonal", "I41/acd","0","x,y,z","-x+1/2,-y,z+1/2","-y+1/4,x+3/4,z+1/4","y+1/4,-x+1/4,z+3/4","-x+1/2,y,-z","x,-y,-z+1/2","y+1/4,x+3/4,-z+3/4","-y+1/4,-x+1/4,-z+1/4","-x,-y,-z","x+1/2,y,-z+1/2","y+3/4,-x+1/4,-z+3/4","-y+3/4,x+3/4,-z+1/4","x+1/2,-y,z","-x,y,z+1/2","-y+3/4,-x+1/4,z+1/4","y+3/4,x+3/4,z+3/4","x+1/2,y+1/2,z+1/2","-x,-y+1/2,z","-y+3/4,x+1/4,z+3/4","y+3/4,-x+3/4,z+1/4","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","y+3/4,x+1/4,-z+1/4","-y+3/4,-x+3/4,-z+3/4","-x+1/2,-y+1/2,-z+1/2","x,y+1/2,-z","y+1/4,-x+3/4,-z+1/4","-y+1/4,x+1/4,-z+3/4","x,-y+1/2,z+1/2","-x+1/2,y+1/2,z","-y+1/4,-x+3/4,z+3/4","y+1/4,x+1/4,z+1/4"},
            new string[] {"143", "Trigonal", "P3","0","x,y,z","-y,x-y,z","-x+y,-x,z"},
            new string[] {"144", "Trigonal", "P31","0","x,y,z","-y,x-y,z+1/3","-x+y,-x,z+2/3"},
            new string[] {"145", "Trigonal", "P32","0","x,y,z","-y,x-y,z+2/3","-x+y,-x,z+1/3"},
            new string[] {"146", "Trigonal", "R3","0","x,y,z","-y,x-y,z","-x+y,-x,z","x+2/3,y+1/3,z+1/3","-y+2/3,x-y+1/3,z+1/3","-x+y+2/3,-x+1/3,z+1/3","x+1/3,y+2/3,z+2/3","-y+1/3,x-y+2/3,z+2/3","-x+y+1/3,-x+2/3,z+2/3"},
            new string[] {"147", "Trigonal", "P-3","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,-z","y,-x+y,-z","x-y,x,-z"},
            new string[] {"148", "Trigonal", "R-3","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,-z","y,-x+y,-z","x-y,x,-z","x+2/3,y+1/3,z+1/3","-y+2/3,x-y+1/3,z+1/3","-x+y+2/3,-x+1/3,z+1/3","-x+2/3,-y+1/3,-z+1/3","y+2/3,-x+y+1/3,-z+1/3","x-y+2/3,x+1/3,-z+1/3","x+1/3,y+2/3,z+2/3","-y+1/3,x-y+2/3,z+2/3","-x+y+1/3,-x+2/3,z+2/3","-x+1/3,-y+2/3,-z+2/3","y+1/3,-x+y+2/3,-z+2/3","x-y+1/3,x+2/3,-z+2/3"},
            new string[] {"149", "Trigonal", "P312","0","x,y,z","-y,x-y,z","-x+y,-x,z","-y,-x,-z","-x+y,y,-z","x,x-y,-z"},
            new string[] {"150", "Trigonal", "P321","0","x,y,z","-y,x-y,z","-x+y,-x,z","y,x,-z","x-y,-y,-z","-x,-x+y,-z"},
            new string[] {"151", "Trigonal", "P3112","0","x,y,z","-y,x-y,z+1/3","-x+y,-x,z+2/3","-y,-x,-z+2/3","-x+y,y,-z+1/3","x,x-y,-z"},
            new string[] {"152", "Trigonal", "P3121","0","x,y,z","-y,x-y,z+1/3","-x+y,-x,z+2/3","y,x,-z","x-y,-y,-z+2/3","-x,-x+y,-z+1/3"},
            new string[] {"153", "Trigonal", "P3212","0","x,y,z","-y,x-y,z+2/3","-x+y,-x,z+1/3","-y,-x,-z+1/3","-x+y,y,-z+2/3","x,x-y,-z"},
            new string[] {"154", "Trigonal", "P3221","0","x,y,z","-y,x-y,z+2/3","-x+y,-x,z+1/3","y,x,-z","x-y,-y,-z+1/3","-x,-x+y,-z+2/3"},
            new string[] {"155", "Trigonal", "R32","0","x,y,z","-y,x-y,z","-x+y,-x,z","y,x,-z","x-y,-y,-z","-x,-x+y,-z","x+2/3,y+1/3,z+1/3","-y+2/3,x-y+1/3,z+1/3","-x+y+2/3,-x+1/3,z+1/3","y+2/3,x+1/3,-z+1/3","x-y+2/3,-y+1/3,-z+1/3","-x+2/3,-x+y+1/3,-z+1/3","x+1/3,y+2/3,z+2/3","-y+1/3,x-y+2/3,z+2/3","-x+y+1/3,-x+2/3,z+2/3","y+1/3,x+2/3,-z+2/3","x-y+1/3,-y+2/3,-z+2/3","-x+1/3,-x+y+2/3,-z+2/3"},
            new string[] {"156", "Trigonal", "P3m1","0","x,y,z","-y,x-y,z","-x+y,-x,z","-y,-x,z","-x+y,y,z","x,x-y,z"},
            new string[] {"157", "Trigonal", "P31m","0","x,y,z","-y,x-y,z","-x+y,-x,z","y,x,z","x-y,-y,z","-x,-x+y,z"},
            new string[] {"158", "Trigonal", "P3c1","0","x,y,z","-y,x-y,z","-x+y,-x,z","-y,-x,z+1/2","-x+y,y,z+1/2","x,x-y,z+1/2"},
            new string[] {"159", "Trigonal", "P31c","0","x,y,z","-y,x-y,z","-x+y,-x,z","y,x,z+1/2","x-y,-y,z+1/2","-x,-x+y,z+1/2"},
            new string[] {"160", "Trigonal", "R3m","0","x,y,z","-y,x-y,z","-x+y,-x,z","-y,-x,z","-x+y,y,z","x,x-y,z","x+2/3,y+1/3,z+1/3","-y+2/3,x-y+1/3,z+1/3","-x+y+2/3,-x+1/3,z+1/3","-y+2/3,-x+1/3,z+1/3","-x+y+2/3,y+1/3,z+1/3","x+2/3,x-y+1/3,z+1/3","x+1/3,y+2/3,z+2/3","-y+1/3,x-y+2/3,z+2/3","-x+y+1/3,-x+2/3,z+2/3","-y+1/3,-x+2/3,z+2/3","-x+y+1/3,y+2/3,z+2/3","x+1/3,x-y+2/3,z+2/3"},
            new string[] {"161", "Trigonal", "R3c","0","x,y,z","-y,x-y,z","-x+y,-x,z","-y,-x,z+1/2","-x+y,y,z+1/2","x,x-y,z+1/2","x+2/3,y+1/3,z+1/3","-y+2/3,x-y+1/3,z+1/3","-x+y+2/3,-x+1/3,z+1/3","-y+2/3,-x+1/3,z+5/6","-x+y+2/3,y+1/3,z+5/6","x+2/3,x-y+1/3,z+5/6","x+1/3,y+2/3,z+2/3","-y+1/3,x-y+2/3,z+2/3","-x+y+1/3,-x+2/3,z+2/3","-y+1/3,-x+2/3,z+1/6","-x+y+1/3,y+2/3,z+1/6","x+1/3,x-y+2/3,z+1/6"},
            new string[] {"162", "Trigonal", "P-31m","0","x,y,z","-y,x-y,z","-x+y,-x,z","-y,-x,-z","-x+y,y,-z","x,x-y,-z","-x,-y,-z","y,-x+y,-z","x-y,x,-z","y,x,z","x-y,-y,z","-x,-x+y,z"},
            new string[] {"163", "Trigonal", "P-31c","0","x,y,z","-y,x-y,z","-x+y,-x,z","-y,-x,-z+1/2","-x+y,y,-z+1/2","x,x-y,-z+1/2","-x,-y,-z","y,-x+y,-z","x-y,x,-z","y,x,z+1/2","x-y,-y,z+1/2","-x,-x+y,z+1/2"},
            new string[] {"164", "Trigonal", "P-3m1","0","x,y,z","-y,x-y,z","-x+y,-x,z","y,x,-z","x-y,-y,-z","-x,-x+y,-z","-x,-y,-z","y,-x+y,-z","x-y,x,-z","-y,-x,z","-x+y,y,z","x,x-y,z"},
            new string[] {"165", "Trigonal", "P-3c1","0","x,y,z","-y,x-y,z","-x+y,-x,z","y,x,-z+1/2","x-y,-y,-z+1/2","-x,-x+y,-z+1/2","-x,-y,-z","y,-x+y,-z","x-y,x,-z","-y,-x,z+1/2","-x+y,y,z+1/2","x,x-y,z+1/2"},
            new string[] {"166", "Trigonal", "R-3m","0","x,y,z","-y,x-y,z","-x+y,-x,z","y,x,-z","x-y,-y,-z","-x,-x+y,-z","-x,-y,-z","y,-x+y,-z","x-y,x,-z","-y,-x,z","-x+y,y,z","x,x-y,z","x+2/3,y+1/3,z+1/3","-y+2/3,x-y+1/3,z+1/3","-x+y+2/3,-x+1/3,z+1/3","y+2/3,x+1/3,-z+1/3","x-y+2/3,-y+1/3,-z+1/3","-x+2/3,-x+y+1/3,-z+1/3","-x+2/3,-y+1/3,-z+1/3","y+2/3,-x+y+1/3,-z+1/3","x-y+2/3,x+1/3,-z+1/3","-y+2/3,-x+1/3,z+1/3","-x+y+2/3,y+1/3,z+1/3","x+2/3,x-y+1/3,z+1/3","x+1/3,y+2/3,z+2/3","-y+1/3,x-y+2/3,z+2/3","-x+y+1/3,-x+2/3,z+2/3","y+1/3,x+2/3,-z+2/3","x-y+1/3,-y+2/3,-z+2/3","-x+1/3,-x+y+2/3,-z+2/3","-x+1/3,-y+2/3,-z+2/3","y+1/3,-x+y+2/3,-z+2/3","x-y+1/3,x+2/3,-z+2/3","-y+1/3,-x+2/3,z+2/3","-x+y+1/3,y+2/3,z+2/3","x+1/3,x-y+2/3,z+2/3"},
            new string[] {"167", "Trigonal", "R-3c","0","x,y,z","-y,x-y,z","-x+y,-x,z","y,x,-z+1/2","x-y,-y,-z+1/2","-x,-x+y,-z+1/2","-x,-y,-z","y,-x+y,-z","x-y,x,-z","-y,-x,z+1/2","-x+y,y,z+1/2","x,x-y,z+1/2","x+2/3,y+1/3,z+1/3","-y+2/3,x-y+1/3,z+1/3","-x+y+2/3,-x+1/3,z+1/3","y+2/3,x+1/3,-z+5/6","x-y+2/3,-y+1/3,-z+5/6","-x+2/3,-x+y+1/3,-z+5/6","-x+2/3,-y+1/3,-z+1/3","y+2/3,-x+y+1/3,-z+1/3","x-y+2/3,x+1/3,-z+1/3","-y+2/3,-x+1/3,z+5/6","-x+y+2/3,y+1/3,z+5/6","x+2/3,x-y+1/3,z+5/6","x+1/3,y+2/3,z+2/3","-y+1/3,x-y+2/3,z+2/3","-x+y+1/3,-x+2/3,z+2/3","y+1/3,x+2/3,-z+1/6","x-y+1/3,-y+2/3,-z+1/6","-x+1/3,-x+y+2/3,-z+1/6","-x+1/3,-y+2/3,-z+2/3","y+1/3,-x+y+2/3,-z+2/3","x-y+1/3,x+2/3,-z+2/3","-y+1/3,-x+2/3,z+1/6","-x+y+1/3,y+2/3,z+1/6","x+1/3,x-y+2/3,z+1/6"},
            new string[] {"168", "Hexagonal", "P6","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z","y,-x+y,z","x-y,x,z"},
            new string[] {"169", "Hexagonal", "P61","0","x,y,z","-y,x-y,z+1/3","-x+y,-x,z+2/3","-x,-y,z+1/2","y,-x+y,z+5/6","x-y,x,z+1/6"},
            new string[] {"170", "Hexagonal", "P65","0","x,y,z","-y,x-y,z+2/3","-x+y,-x,z+1/3","-x,-y,z+1/2","y,-x+y,z+1/6","x-y,x,z+5/6"},
            new string[] {"171", "Hexagonal", "P62","0","x,y,z","-y,x-y,z+2/3","-x+y,-x,z+1/3","-x,-y,z","y,-x+y,z+2/3","x-y,x,z+1/3"},
            new string[] {"172", "Hexagonal", "P64","0","x,y,z","-y,x-y,z+1/3","-x+y,-x,z+2/3","-x,-y,z","y,-x+y,z+1/3","x-y,x,z+2/3"},
            new string[] {"173", "Hexagonal", "P63","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z+1/2","y,-x+y,z+1/2","x-y,x,z+1/2"},
            new string[] {"174", "Hexagonal", "P-6","0","x,y,z","-y,x-y,z","-x+y,-x,z","x,y,-z","-y,x-y,-z","-x+y,-x,-z"},
            new string[] {"175", "Hexagonal", "P6/m","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z","y,-x+y,z","x-y,x,z","-x,-y,-z","y,-x+y,-z","x-y,x,-z","x,y,-z","-y,x-y,-z","-x+y,-x,-z"},
            new string[] {"176", "Hexagonal", "P63/m","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z+1/2","y,-x+y,z+1/2","x-y,x,z+1/2","-x,-y,-z","y,-x+y,-z","x-y,x,-z","x,y,-z+1/2","-y,x-y,-z+1/2","-x+y,-x,-z+1/2"},
            new string[] {"177", "Hexagonal", "P622","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z","y,-x+y,z","x-y,x,z","y,x,-z","x-y,-y,-z","-x,-x+y,-z","-y,-x,-z","-x+y,y,-z","x,x-y,-z"},
            new string[] {"178", "Hexagonal", "P6122","0","x,y,z","-y,x-y,z+1/3","-x+y,-x,z+2/3","-x,-y,z+1/2","y,-x+y,z+5/6","x-y,x,z+1/6","y,x,-z+1/3","x-y,-y,-z","-x,-x+y,-z+2/3","-y,-x,-z+5/6","-x+y,y,-z+1/2","x,x-y,-z+1/6"},
            new string[] {"179", "Hexagonal", "P6522","0","x,y,z","-y,x-y,z+2/3","-x+y,-x,z+1/3","-x,-y,z+1/2","y,-x+y,z+1/6","x-y,x,z+5/6","y,x,-z+2/3","x-y,-y,-z","-x,-x+y,-z+1/3","-y,-x,-z+1/6","-x+y,y,-z+1/2","x,x-y,-z+5/6"},
            new string[] {"180", "Hexagonal", "P6222","0","x,y,z","-y,x-y,z+2/3","-x+y,-x,z+1/3","-x,-y,z","y,-x+y,z+2/3","x-y,x,z+1/3","y,x,-z+2/3","x-y,-y,-z","-x,-x+y,-z+1/3","-y,-x,-z+2/3","-x+y,y,-z","x,x-y,-z+1/3"},
            new string[] {"181", "Hexagonal", "P6422","0","x,y,z","-y,x-y,z+1/3","-x+y,-x,z+2/3","-x,-y,z","y,-x+y,z+1/3","x-y,x,z+2/3","y,x,-z+1/3","x-y,-y,-z","-x,-x+y,-z+2/3","-y,-x,-z+1/3","-x+y,y,-z","x,x-y,-z+2/3"},
            new string[] {"182", "Hexagonal", "P6322","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z+1/2","y,-x+y,z+1/2","x-y,x,z+1/2","y,x,-z","x-y,-y,-z","-x,-x+y,-z","-y,-x,-z+1/2","-x+y,y,-z+1/2","x,x-y,-z+1/2"},
            new string[] {"183", "Hexagonal", "P6mm","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z","y,-x+y,z","x-y,x,z","-y,-x,z","-x+y,y,z","x,x-y,z","y,x,z","x-y,-y,z","-x,-x+y,z"},
            new string[] {"184", "Hexagonal", "P6cc","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z","y,-x+y,z","x-y,x,z","-y,-x,z+1/2","-x+y,y,z+1/2","x,x-y,z+1/2","y,x,z+1/2","x-y,-y,z+1/2","-x,-x+y,z+1/2"},
            new string[] {"185", "Hexagonal", "P63cm","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z+1/2","y,-x+y,z+1/2","x-y,x,z+1/2","-y,-x,z+1/2","-x+y,y,z+1/2","x,x-y,z+1/2","y,x,z","x-y,-y,z","-x,-x+y,z"},
            new string[] {"186", "Hexagonal", "P63mc","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z+1/2","y,-x+y,z+1/2","x-y,x,z+1/2","-y,-x,z","-x+y,y,z","x,x-y,z","y,x,z+1/2","x-y,-y,z+1/2","-x,-x+y,z+1/2"},
            new string[] {"187", "Hexagonal", "P-6m2","0","x,y,z","-y,x-y,z","-x+y,-x,z","x,y,-z","-y,x-y,-z","-x+y,-x,-z","-y,-x,z","-x+y,y,z","x,x-y,z","-y,-x,-z","-x+y,y,-z","x,x-y,-z"},
            new string[] {"188", "Hexagonal", "P-6c2","0","x,y,z","-y,x-y,z","-x+y,-x,z","x,y,-z+1/2","-y,x-y,-z+1/2","-x+y,-x,-z+1/2","-y,-x,z+1/2","-x+y,y,z+1/2","x,x-y,z+1/2","-y,-x,-z","-x+y,y,-z","x,x-y,-z"},
            new string[] {"189", "Hexagonal", "P-62m","0","x,y,z","-y,x-y,z","-x+y,-x,z","x,y,-z","-y,x-y,-z","-x+y,-x,-z","y,x,-z","x-y,-y,-z","-x,-x+y,-z","y,x,z","x-y,-y,z","-x,-x+y,z"},
            new string[] {"190", "Hexagonal", "P-62c","0","x,y,z","-y,x-y,z","-x+y,-x,z","x,y,-z+1/2","-y,x-y,-z+1/2","-x+y,-x,-z+1/2","y,x,-z","x-y,-y,-z","-x,-x+y,-z","y,x,z+1/2","x-y,-y,z+1/2","-x,-x+y,z+1/2"},
            new string[] {"191", "Hexagonal", "P6/mmm","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z","y,-x+y,z","x-y,x,z","y,x,-z","x-y,-y,-z","-x,-x+y,-z","-y,-x,-z","-x+y,y,-z","x,x-y,-z","-x,-y,-z","y,-x+y,-z","x-y,x,-z","x,y,-z","-y,x-y,-z","-x+y,-x,-z","-y,-x,z","-x+y,y,z","x,x-y,z","y,x,z","x-y,-y,z","-x,-x+y,z"},
            new string[] {"192", "Hexagonal", "P6/mcc","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z","y,-x+y,z","x-y,x,z","y,x,-z+1/2","x-y,-y,-z+1/2","-x,-x+y,-z+1/2","-y,-x,-z+1/2","-x+y,y,-z+1/2","x,x-y,-z+1/2","-x,-y,-z","y,-x+y,-z","x-y,x,-z","x,y,-z","-y,x-y,-z","-x+y,-x,-z","-y,-x,z+1/2","-x+y,y,z+1/2","x,x-y,z+1/2","y,x,z+1/2","x-y,-y,z+1/2","-x,-x+y,z+1/2"},
            new string[] {"193", "Hexagonal", "P63/mcm","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z+1/2","y,-x+y,z+1/2","x-y,x,z+1/2","y,x,-z+1/2","x-y,-y,-z+1/2","-x,-x+y,-z+1/2","-y,-x,-z","-x+y,y,-z","x,x-y,-z","-x,-y,-z","y,-x+y,-z","x-y,x,-z","x,y,-z+1/2","-y,x-y,-z+1/2","-x+y,-x,-z+1/2","-y,-x,z+1/2","-x+y,y,z+1/2","x,x-y,z+1/2","y,x,z","x-y,-y,z","-x,-x+y,z"},
            new string[] {"194", "Hexagonal", "P63/mmc","0","x,y,z","-y,x-y,z","-x+y,-x,z","-x,-y,z+1/2","y,-x+y,z+1/2","x-y,x,z+1/2","y,x,-z","x-y,-y,-z","-x,-x+y,-z","-y,-x,-z+1/2","-x+y,y,-z+1/2","x,x-y,-z+1/2","-x,-y,-z","y,-x+y,-z","x-y,x,-z","x,y,-z+1/2","-y,x-y,-z+1/2","-x+y,-x,-z+1/2","-y,-x,z","-x+y,y,z","x,x-y,z","y,x,z+1/2","x-y,-y,z+1/2","-x,-x+y,z+1/2"},
            new string[] {"195", "Cubic", "P23","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x"},
            new string[] {"196", "Cubic", "F23","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","-x,y+1/2,-z+1/2","x,-y+1/2,-z+1/2","z,x+1/2,y+1/2","z,-x+1/2,-y+1/2","-z,-x+1/2,y+1/2","-z,x+1/2,-y+1/2","y,z+1/2,x+1/2","-y,z+1/2,-x+1/2","y,-z+1/2,-x+1/2","-y,-z+1/2,x+1/2","x+1/2,y,z+1/2","-x+1/2,-y,z+1/2","-x+1/2,y,-z+1/2","x+1/2,-y,-z+1/2","z+1/2,x,y+1/2","z+1/2,-x,-y+1/2","-z+1/2,-x,y+1/2","-z+1/2,x,-y+1/2","y+1/2,z,x+1/2","-y+1/2,z,-x+1/2","y+1/2,-z,-x+1/2","-y+1/2,-z,x+1/2","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","z+1/2,x+1/2,y","z+1/2,-x+1/2,-y","-z+1/2,-x+1/2,y","-z+1/2,x+1/2,-y","y+1/2,z+1/2,x","-y+1/2,z+1/2,-x","y+1/2,-z+1/2,-x","-y+1/2,-z+1/2,x"},
            new string[] {"197", "Cubic", "I23","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","z+1/2,x+1/2,y+1/2","z+1/2,-x+1/2,-y+1/2","-z+1/2,-x+1/2,y+1/2","-z+1/2,x+1/2,-y+1/2","y+1/2,z+1/2,x+1/2","-y+1/2,z+1/2,-x+1/2","y+1/2,-z+1/2,-x+1/2","-y+1/2,-z+1/2,x+1/2"},
            new string[] {"198", "Cubic", "P213","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","z,x,y","z+1/2,-x+1/2,-y","-z+1/2,-x,y+1/2","-z,x+1/2,-y+1/2","y,z,x","-y,z+1/2,-x+1/2","y+1/2,-z+1/2,-x","-y+1/2,-z,x+1/2"},
            new string[] {"199", "Cubic", "I213","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","z,x,y","z+1/2,-x+1/2,-y","-z+1/2,-x,y+1/2","-z,x+1/2,-y+1/2","y,z,x","-y,z+1/2,-x+1/2","y+1/2,-z+1/2,-x","-y+1/2,-z,x+1/2","x+1/2,y+1/2,z+1/2","-x,-y+1/2,z","-x+1/2,y,-z","x,-y,-z+1/2","z+1/2,x+1/2,y+1/2","z,-x,-y+1/2","-z,-x+1/2,y","-z+1/2,x,-y","y+1/2,z+1/2,x+1/2","-y+1/2,z,-x","y,-z,-x+1/2","-y,-z+1/2,x"},
            new string[] {"200", "Cubic", "Pm-3","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","-x,-y,-z","x,y,-z","x,-y,z","-x,y,z","-z,-x,-y","-z,x,y","z,x,-y","z,-x,y","-y,-z,-x","y,-z,x","-y,z,x","y,z,-x"},
            new string[] {"201", "Cubic", "Pn-3","0","x,y,z","-x+1/2,-y+1/2,z","-x+1/2,y,-z+1/2","x,-y+1/2,-z+1/2","z,x,y","z,-x+1/2,-y+1/2","-z+1/2,-x+1/2,y","-z+1/2,x,-y+1/2","y,z,x","-y+1/2,z,-x+1/2","y,-z+1/2,-x+1/2","-y+1/2,-z+1/2,x","-x,-y,-z","x+1/2,y+1/2,-z","x+1/2,-y,z+1/2","-x,y+1/2,z+1/2","-z,-x,-y","-z,x+1/2,y+1/2","z+1/2,x+1/2,-y","z+1/2,-x,y+1/2","-y,-z,-x","y+1/2,-z,x+1/2","-y,z+1/2,x+1/2","y+1/2,z+1/2,-x"},
            new string[] {"202", "Cubic", "Fm-3","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","-x,-y,-z","x,y,-z","x,-y,z","-x,y,z","-z,-x,-y","-z,x,y","z,x,-y","z,-x,y","-y,-z,-x","y,-z,x","-y,z,x","y,z,-x","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","-x,y+1/2,-z+1/2","x,-y+1/2,-z+1/2","z,x+1/2,y+1/2","z,-x+1/2,-y+1/2","-z,-x+1/2,y+1/2","-z,x+1/2,-y+1/2","y,z+1/2,x+1/2","-y,z+1/2,-x+1/2","y,-z+1/2,-x+1/2","-y,-z+1/2,x+1/2","-x,-y+1/2,-z+1/2","x,y+1/2,-z+1/2","x,-y+1/2,z+1/2","-x,y+1/2,z+1/2","-z,-x+1/2,-y+1/2","-z,x+1/2,y+1/2","z,x+1/2,-y+1/2","z,-x+1/2,y+1/2","-y,-z+1/2,-x+1/2","y,-z+1/2,x+1/2","-y,z+1/2,x+1/2","y,z+1/2,-x+1/2","x+1/2,y,z+1/2","-x+1/2,-y,z+1/2","-x+1/2,y,-z+1/2","x+1/2,-y,-z+1/2","z+1/2,x,y+1/2","z+1/2,-x,-y+1/2","-z+1/2,-x,y+1/2","-z+1/2,x,-y+1/2","y+1/2,z,x+1/2","-y+1/2,z,-x+1/2","y+1/2,-z,-x+1/2","-y+1/2,-z,x+1/2","-x+1/2,-y,-z+1/2","x+1/2,y,-z+1/2","x+1/2,-y,z+1/2","-x+1/2,y,z+1/2","-z+1/2,-x,-y+1/2","-z+1/2,x,y+1/2","z+1/2,x,-y+1/2","z+1/2,-x,y+1/2","-y+1/2,-z,-x+1/2","y+1/2,-z,x+1/2","-y+1/2,z,x+1/2","y+1/2,z,-x+1/2","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","z+1/2,x+1/2,y","z+1/2,-x+1/2,-y","-z+1/2,-x+1/2,y","-z+1/2,x+1/2,-y","y+1/2,z+1/2,x","-y+1/2,z+1/2,-x","y+1/2,-z+1/2,-x","-y+1/2,-z+1/2,x","-x+1/2,-y+1/2,-z","x+1/2,y+1/2,-z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","-z+1/2,-x+1/2,-y","-z+1/2,x+1/2,y","z+1/2,x+1/2,-y","z+1/2,-x+1/2,y","-y+1/2,-z+1/2,-x","y+1/2,-z+1/2,x","-y+1/2,z+1/2,x","y+1/2,z+1/2,-x"},
            new string[] {"203", "Cubic", "Fd-3","0","x,y,z","-x+1/4,-y+1/4,z","-x+1/4,y,-z+1/4","x,-y+1/4,-z+1/4","z,x,y","z,-x+1/4,-y+1/4","-z+1/4,-x+1/4,y","-z+1/4,x,-y+1/4","y,z,x","-y+1/4,z,-x+1/4","y,-z+1/4,-x+1/4","-y+1/4,-z+1/4,x","-x,-y,-z","x+3/4,y+3/4,-z","x+3/4,-y,z+3/4","-x,y+3/4,z+3/4","-z,-x,-y","-z,x+3/4,y+3/4","z+3/4,x+3/4,-y","z+3/4,-x,y+3/4","-y,-z,-x","y+3/4,-z,x+3/4","-y,z+3/4,x+3/4","y+3/4,z+3/4,-x","x,y+1/2,z+1/2","-x+1/4,-y+3/4,z+1/2","-x+1/4,y+1/2,-z+3/4","x,-y+3/4,-z+3/4","z,x+1/2,y+1/2","z,-x+3/4,-y+3/4","-z+1/4,-x+3/4,y+1/2","-z+1/4,x+1/2,-y+3/4","y,z+1/2,x+1/2","-y+1/4,z+1/2,-x+3/4","y,-z+3/4,-x+3/4","-y+1/4,-z+3/4,x+1/2","-x,-y+1/2,-z+1/2","x+3/4,y+1/4,-z+1/2","x+3/4,-y+1/2,z+1/4","-x,y+1/4,z+1/4","-z,-x+1/2,-y+1/2","-z,x+1/4,y+1/4","z+3/4,x+1/4,-y+1/2","z+3/4,-x+1/2,y+1/4","-y,-z+1/2,-x+1/2","y+3/4,-z+1/2,x+1/4","-y,z+1/4,x+1/4","y+3/4,z+1/4,-x+1/2","x+1/2,y,z+1/2","-x+3/4,-y+1/4,z+1/2","-x+3/4,y,-z+3/4","x+1/2,-y+1/4,-z+3/4","z+1/2,x,y+1/2","z+1/2,-x+1/4,-y+3/4","-z+3/4,-x+1/4,y+1/2","-z+3/4,x,-y+3/4","y+1/2,z,x+1/2","-y+3/4,z,-x+3/4","y+1/2,-z+1/4,-x+3/4","-y+3/4,-z+1/4,x+1/2","-x+1/2,-y,-z+1/2","x+1/4,y+3/4,-z+1/2","x+1/4,-y,z+1/4","-x+1/2,y+3/4,z+1/4","-z+1/2,-x,-y+1/2","-z+1/2,x+3/4,y+1/4","z+1/4,x+3/4,-y+1/2","z+1/4,-x,y+1/4","-y+1/2,-z,-x+1/2","y+1/4,-z,x+1/4","-y+1/2,z+3/4,x+1/4","y+1/4,z+3/4,-x+1/2","x+1/2,y+1/2,z","-x+3/4,-y+3/4,z","-x+3/4,y+1/2,-z+1/4","x+1/2,-y+3/4,-z+1/4","z+1/2,x+1/2,y","z+1/2,-x+3/4,-y+1/4","-z+3/4,-x+3/4,y","-z+3/4,x+1/2,-y+1/4","y+1/2,z+1/2,x","-y+3/4,z+1/2,-x+1/4","y+1/2,-z+3/4,-x+1/4","-y+3/4,-z+3/4,x","-x+1/2,-y+1/2,-z","x+1/4,y+1/4,-z","x+1/4,-y+1/2,z+3/4","-x+1/2,y+1/4,z+3/4","-z+1/2,-x+1/2,-y","-z+1/2,x+1/4,y+3/4","z+1/4,x+1/4,-y","z+1/4,-x+1/2,y+3/4","-y+1/2,-z+1/2,-x","y+1/4,-z+1/2,x+3/4","-y+1/2,z+1/4,x+3/4","y+1/4,z+1/4,-x"},
            new string[] {"204", "Cubic", "Im-3","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","-x,-y,-z","x,y,-z","x,-y,z","-x,y,z","-z,-x,-y","-z,x,y","z,x,-y","z,-x,y","-y,-z,-x","y,-z,x","-y,z,x","y,z,-x","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","z+1/2,x+1/2,y+1/2","z+1/2,-x+1/2,-y+1/2","-z+1/2,-x+1/2,y+1/2","-z+1/2,x+1/2,-y+1/2","y+1/2,z+1/2,x+1/2","-y+1/2,z+1/2,-x+1/2","y+1/2,-z+1/2,-x+1/2","-y+1/2,-z+1/2,x+1/2","-x+1/2,-y+1/2,-z+1/2","x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2","-z+1/2,-x+1/2,-y+1/2","-z+1/2,x+1/2,y+1/2","z+1/2,x+1/2,-y+1/2","z+1/2,-x+1/2,y+1/2","-y+1/2,-z+1/2,-x+1/2","y+1/2,-z+1/2,x+1/2","-y+1/2,z+1/2,x+1/2","y+1/2,z+1/2,-x+1/2"},
            new string[] {"205", "Cubic", "Pa-3","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","z,x,y","z+1/2,-x+1/2,-y","-z+1/2,-x,y+1/2","-z,x+1/2,-y+1/2","y,z,x","-y,z+1/2,-x+1/2","y+1/2,-z+1/2,-x","-y+1/2,-z,x+1/2","-x,-y,-z","x+1/2,y,-z+1/2","x,-y+1/2,z+1/2","-x+1/2,y+1/2,z","-z,-x,-y","-z+1/2,x+1/2,y","z+1/2,x,-y+1/2","z,-x+1/2,y+1/2","-y,-z,-x","y,-z+1/2,x+1/2","-y+1/2,z+1/2,x","y+1/2,z,-x+1/2"},
            new string[] {"206", "Cubic", "Ia-3","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","z,x,y","z+1/2,-x+1/2,-y","-z+1/2,-x,y+1/2","-z,x+1/2,-y+1/2","y,z,x","-y,z+1/2,-x+1/2","y+1/2,-z+1/2,-x","-y+1/2,-z,x+1/2","-x,-y,-z","x+1/2,y,-z+1/2","x,-y+1/2,z+1/2","-x+1/2,y+1/2,z","-z,-x,-y","-z+1/2,x+1/2,y","z+1/2,x,-y+1/2","z,-x+1/2,y+1/2","-y,-z,-x","y,-z+1/2,x+1/2","-y+1/2,z+1/2,x","y+1/2,z,-x+1/2","x+1/2,y+1/2,z+1/2","-x,-y+1/2,z","-x+1/2,y,-z","x,-y,-z+1/2","z+1/2,x+1/2,y+1/2","z,-x,-y+1/2","-z,-x+1/2,y","-z+1/2,x,-y","y+1/2,z+1/2,x+1/2","-y+1/2,z,-x","y,-z,-x+1/2","-y,-z+1/2,x","-x+1/2,-y+1/2,-z+1/2","x,y+1/2,-z","x+1/2,-y,z","-x,y,z+1/2","-z+1/2,-x+1/2,-y+1/2","-z,x,y+1/2","z,x+1/2,-y","z+1/2,-x,y","-y+1/2,-z+1/2,-x+1/2","y+1/2,-z,x","-y,z,x+1/2","y,z+1/2,-x"},
            new string[] {"207", "Cubic", "P432","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y,x,-z","-y,-x,-z","y,-x,z","-y,x,z","x,z,-y","-x,z,y","-x,-z,-y","x,-z,y","z,y,-x","z,-y,x","-z,y,x","-z,-y,-x"},
            new string[] {"208", "Cubic", "P4232","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y+1/2,x+1/2,-z+1/2","-y+1/2,-x+1/2,-z+1/2","y+1/2,-x+1/2,z+1/2","-y+1/2,x+1/2,z+1/2","x+1/2,z+1/2,-y+1/2","-x+1/2,z+1/2,y+1/2","-x+1/2,-z+1/2,-y+1/2","x+1/2,-z+1/2,y+1/2","z+1/2,y+1/2,-x+1/2","z+1/2,-y+1/2,x+1/2","-z+1/2,y+1/2,x+1/2","-z+1/2,-y+1/2,-x+1/2"},
            new string[] {"209", "Cubic", "F432","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y,x,-z","-y,-x,-z","y,-x,z","-y,x,z","x,z,-y","-x,z,y","-x,-z,-y","x,-z,y","z,y,-x","z,-y,x","-z,y,x","-z,-y,-x","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","-x,y+1/2,-z+1/2","x,-y+1/2,-z+1/2","z,x+1/2,y+1/2","z,-x+1/2,-y+1/2","-z,-x+1/2,y+1/2","-z,x+1/2,-y+1/2","y,z+1/2,x+1/2","-y,z+1/2,-x+1/2","y,-z+1/2,-x+1/2","-y,-z+1/2,x+1/2","y,x+1/2,-z+1/2","-y,-x+1/2,-z+1/2","y,-x+1/2,z+1/2","-y,x+1/2,z+1/2","x,z+1/2,-y+1/2","-x,z+1/2,y+1/2","-x,-z+1/2,-y+1/2","x,-z+1/2,y+1/2","z,y+1/2,-x+1/2","z,-y+1/2,x+1/2","-z,y+1/2,x+1/2","-z,-y+1/2,-x+1/2","x+1/2,y,z+1/2","-x+1/2,-y,z+1/2","-x+1/2,y,-z+1/2","x+1/2,-y,-z+1/2","z+1/2,x,y+1/2","z+1/2,-x,-y+1/2","-z+1/2,-x,y+1/2","-z+1/2,x,-y+1/2","y+1/2,z,x+1/2","-y+1/2,z,-x+1/2","y+1/2,-z,-x+1/2","-y+1/2,-z,x+1/2","y+1/2,x,-z+1/2","-y+1/2,-x,-z+1/2","y+1/2,-x,z+1/2","-y+1/2,x,z+1/2","x+1/2,z,-y+1/2","-x+1/2,z,y+1/2","-x+1/2,-z,-y+1/2","x+1/2,-z,y+1/2","z+1/2,y,-x+1/2","z+1/2,-y,x+1/2","-z+1/2,y,x+1/2","-z+1/2,-y,-x+1/2","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","z+1/2,x+1/2,y","z+1/2,-x+1/2,-y","-z+1/2,-x+1/2,y","-z+1/2,x+1/2,-y","y+1/2,z+1/2,x","-y+1/2,z+1/2,-x","y+1/2,-z+1/2,-x","-y+1/2,-z+1/2,x","y+1/2,x+1/2,-z","-y+1/2,-x+1/2,-z","y+1/2,-x+1/2,z","-y+1/2,x+1/2,z","x+1/2,z+1/2,-y","-x+1/2,z+1/2,y","-x+1/2,-z+1/2,-y","x+1/2,-z+1/2,y","z+1/2,y+1/2,-x","z+1/2,-y+1/2,x","-z+1/2,y+1/2,x","-z+1/2,-y+1/2,-x"},
            new string[] {"210", "Cubic", "F4132","0","x,y,z","-x,-y+1/2,z+1/2","-x+1/2,y+1/2,-z","x+1/2,-y,-z+1/2","z,x,y","z+1/2,-x,-y+1/2","-z,-x+1/2,y+1/2","-z+1/2,x+1/2,-y","y,z,x","-y+1/2,z+1/2,-x","y+1/2,-z,-x+1/2","-y,-z+1/2,x+1/2","y+3/4,x+1/4,-z+3/4","-y+1/4,-x+1/4,-z+1/4","y+1/4,-x+3/4,z+3/4","-y+3/4,x+3/4,z+1/4","x+3/4,z+1/4,-y+3/4","-x+3/4,z+3/4,y+1/4","-x+1/4,-z+1/4,-y+1/4","x+1/4,-z+3/4,y+3/4","z+3/4,y+1/4,-x+3/4","z+1/4,-y+3/4,x+3/4","-z+3/4,y+3/4,x+1/4","-z+1/4,-y+1/4,-x+1/4","x,y+1/2,z+1/2","-x,-y,z","-x+1/2,y,-z+1/2","x+1/2,-y+1/2,-z","z,x+1/2,y+1/2","z+1/2,-x+1/2,-y","-z,-x,y","-z+1/2,x,-y+1/2","y,z+1/2,x+1/2","-y+1/2,z,-x+1/2","y+1/2,-z+1/2,-x","-y,-z,x","y+3/4,x+3/4,-z+1/4","-y+1/4,-x+3/4,-z+3/4","y+1/4,-x+1/4,z+1/4","-y+3/4,x+1/4,z+3/4","x+3/4,z+3/4,-y+1/4","-x+3/4,z+1/4,y+3/4","-x+1/4,-z+3/4,-y+3/4","x+1/4,-z+1/4,y+1/4","z+3/4,y+3/4,-x+1/4","z+1/4,-y+1/4,x+1/4","-z+3/4,y+1/4,x+3/4","-z+1/4,-y+3/4,-x+3/4","x+1/2,y,z+1/2","-x+1/2,-y+1/2,z","-x,y+1/2,-z+1/2","x,-y,-z","z+1/2,x,y+1/2","z,-x,-y","-z+1/2,-x+1/2,y","-z,x+1/2,-y+1/2","y+1/2,z,x+1/2","-y,z+1/2,-x+1/2","y,-z,-x","-y+1/2,-z+1/2,x","y+1/4,x+1/4,-z+1/4","-y+3/4,-x+1/4,-z+3/4","y+3/4,-x+3/4,z+1/4","-y+1/4,x+3/4,z+3/4","x+1/4,z+1/4,-y+1/4","-x+1/4,z+3/4,y+3/4","-x+3/4,-z+1/4,-y+3/4","x+3/4,-z+3/4,y+1/4","z+1/4,y+1/4,-x+1/4","z+3/4,-y+3/4,x+1/4","-z+1/4,y+3/4,x+3/4","-z+3/4,-y+1/4,-x+3/4","x+1/2,y+1/2,z","-x+1/2,-y,z+1/2","-x,y,-z","x,-y+1/2,-z+1/2","z+1/2,x+1/2,y","z,-x+1/2,-y+1/2","-z+1/2,-x,y+1/2","-z,x,-y","y+1/2,z+1/2,x","-y,z,-x","y,-z+1/2,-x+1/2","-y+1/2,-z,x+1/2","y+1/4,x+3/4,-z+3/4","-y+3/4,-x+3/4,-z+1/4","y+3/4,-x+1/4,z+3/4","-y+1/4,x+1/4,z+1/4","x+1/4,z+3/4,-y+3/4","-x+1/4,z+1/4,y+1/4","-x+3/4,-z+3/4,-y+1/4","x+3/4,-z+1/4,y+3/4","z+1/4,y+3/4,-x+3/4","z+3/4,-y+1/4,x+3/4","-z+1/4,y+1/4,x+1/4","-z+3/4,-y+3/4,-x+1/4"},
            new string[] {"211", "Cubic", "I432","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y,x,-z","-y,-x,-z","y,-x,z","-y,x,z","x,z,-y","-x,z,y","-x,-z,-y","x,-z,y","z,y,-x","z,-y,x","-z,y,x","-z,-y,-x","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","z+1/2,x+1/2,y+1/2","z+1/2,-x+1/2,-y+1/2","-z+1/2,-x+1/2,y+1/2","-z+1/2,x+1/2,-y+1/2","y+1/2,z+1/2,x+1/2","-y+1/2,z+1/2,-x+1/2","y+1/2,-z+1/2,-x+1/2","-y+1/2,-z+1/2,x+1/2","y+1/2,x+1/2,-z+1/2","-y+1/2,-x+1/2,-z+1/2","y+1/2,-x+1/2,z+1/2","-y+1/2,x+1/2,z+1/2","x+1/2,z+1/2,-y+1/2","-x+1/2,z+1/2,y+1/2","-x+1/2,-z+1/2,-y+1/2","x+1/2,-z+1/2,y+1/2","z+1/2,y+1/2,-x+1/2","z+1/2,-y+1/2,x+1/2","-z+1/2,y+1/2,x+1/2","-z+1/2,-y+1/2,-x+1/2"},
            new string[] {"212", "Cubic", "P4332","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","z,x,y","z+1/2,-x+1/2,-y","-z+1/2,-x,y+1/2","-z,x+1/2,-y+1/2","y,z,x","-y,z+1/2,-x+1/2","y+1/2,-z+1/2,-x","-y+1/2,-z,x+1/2","y+1/4,x+3/4,-z+3/4","-y+1/4,-x+1/4,-z+1/4","y+3/4,-x+3/4,z+1/4","-y+3/4,x+1/4,z+3/4","x+1/4,z+3/4,-y+3/4","-x+3/4,z+1/4,y+3/4","-x+1/4,-z+1/4,-y+1/4","x+3/4,-z+3/4,y+1/4","z+1/4,y+3/4,-x+3/4","z+3/4,-y+3/4,x+1/4","-z+3/4,y+1/4,x+3/4","-z+1/4,-y+1/4,-x+1/4"},
            new string[] {"213", "Cubic", "P4132","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","z,x,y","z+1/2,-x+1/2,-y","-z+1/2,-x,y+1/2","-z,x+1/2,-y+1/2","y,z,x","-y,z+1/2,-x+1/2","y+1/2,-z+1/2,-x","-y+1/2,-z,x+1/2","y+3/4,x+1/4,-z+1/4","-y+3/4,-x+3/4,-z+3/4","y+1/4,-x+1/4,z+3/4","-y+1/4,x+3/4,z+1/4","x+3/4,z+1/4,-y+1/4","-x+1/4,z+3/4,y+1/4","-x+3/4,-z+3/4,-y+3/4","x+1/4,-z+1/4,y+3/4","z+3/4,y+1/4,-x+1/4","z+1/4,-y+1/4,x+3/4","-z+1/4,y+3/4,x+1/4","-z+3/4,-y+3/4,-x+3/4"},
            new string[] {"214", "Cubic", "I4132","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","z,x,y","z+1/2,-x+1/2,-y","-z+1/2,-x,y+1/2","-z,x+1/2,-y+1/2","y,z,x","-y,z+1/2,-x+1/2","y+1/2,-z+1/2,-x","-y+1/2,-z,x+1/2","y+3/4,x+1/4,-z+1/4","-y+3/4,-x+3/4,-z+3/4","y+1/4,-x+1/4,z+3/4","-y+1/4,x+3/4,z+1/4","x+3/4,z+1/4,-y+1/4","-x+1/4,z+3/4,y+1/4","-x+3/4,-z+3/4,-y+3/4","x+1/4,-z+1/4,y+3/4","z+3/4,y+1/4,-x+1/4","z+1/4,-y+1/4,x+3/4","-z+1/4,y+3/4,x+1/4","-z+3/4,-y+3/4,-x+3/4","x+1/2,y+1/2,z+1/2","-x,-y+1/2,z","-x+1/2,y,-z","x,-y,-z+1/2","z+1/2,x+1/2,y+1/2","z,-x,-y+1/2","-z,-x+1/2,y","-z+1/2,x,-y","y+1/2,z+1/2,x+1/2","-y+1/2,z,-x","y,-z,-x+1/2","-y,-z+1/2,x","y+1/4,x+3/4,-z+3/4","-y+1/4,-x+1/4,-z+1/4","y+3/4,-x+3/4,z+1/4","-y+3/4,x+1/4,z+3/4","x+1/4,z+3/4,-y+3/4","-x+3/4,z+1/4,y+3/4","-x+1/4,-z+1/4,-y+1/4","x+3/4,-z+3/4,y+1/4","z+1/4,y+3/4,-x+3/4","z+3/4,-y+3/4,x+1/4","-z+3/4,y+1/4,x+3/4","-z+1/4,-y+1/4,-x+1/4"},
            new string[] {"215", "Cubic", "P-43m","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y,x,z","-y,-x,z","y,-x,-z","-y,x,-z","x,z,y","-x,z,-y","-x,-z,y","x,-z,-y","z,y,x","z,-y,-x","-z,y,-x","-z,-y,x"},
            new string[] {"216", "Cubic", "F-43m","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y,x,z","-y,-x,z","y,-x,-z","-y,x,-z","x,z,y","-x,z,-y","-x,-z,y","x,-z,-y","z,y,x","z,-y,-x","-z,y,-x","-z,-y,x","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","-x,y+1/2,-z+1/2","x,-y+1/2,-z+1/2","z,x+1/2,y+1/2","z,-x+1/2,-y+1/2","-z,-x+1/2,y+1/2","-z,x+1/2,-y+1/2","y,z+1/2,x+1/2","-y,z+1/2,-x+1/2","y,-z+1/2,-x+1/2","-y,-z+1/2,x+1/2","y,x+1/2,z+1/2","-y,-x+1/2,z+1/2","y,-x+1/2,-z+1/2","-y,x+1/2,-z+1/2","x,z+1/2,y+1/2","-x,z+1/2,-y+1/2","-x,-z+1/2,y+1/2","x,-z+1/2,-y+1/2","z,y+1/2,x+1/2","z,-y+1/2,-x+1/2","-z,y+1/2,-x+1/2","-z,-y+1/2,x+1/2","x+1/2,y,z+1/2","-x+1/2,-y,z+1/2","-x+1/2,y,-z+1/2","x+1/2,-y,-z+1/2","z+1/2,x,y+1/2","z+1/2,-x,-y+1/2","-z+1/2,-x,y+1/2","-z+1/2,x,-y+1/2","y+1/2,z,x+1/2","-y+1/2,z,-x+1/2","y+1/2,-z,-x+1/2","-y+1/2,-z,x+1/2","y+1/2,x,z+1/2","-y+1/2,-x,z+1/2","y+1/2,-x,-z+1/2","-y+1/2,x,-z+1/2","x+1/2,z,y+1/2","-x+1/2,z,-y+1/2","-x+1/2,-z,y+1/2","x+1/2,-z,-y+1/2","z+1/2,y,x+1/2","z+1/2,-y,-x+1/2","-z+1/2,y,-x+1/2","-z+1/2,-y,x+1/2","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","z+1/2,x+1/2,y","z+1/2,-x+1/2,-y","-z+1/2,-x+1/2,y","-z+1/2,x+1/2,-y","y+1/2,z+1/2,x","-y+1/2,z+1/2,-x","y+1/2,-z+1/2,-x","-y+1/2,-z+1/2,x","y+1/2,x+1/2,z","-y+1/2,-x+1/2,z","y+1/2,-x+1/2,-z","-y+1/2,x+1/2,-z","x+1/2,z+1/2,y","-x+1/2,z+1/2,-y","-x+1/2,-z+1/2,y","x+1/2,-z+1/2,-y","z+1/2,y+1/2,x","z+1/2,-y+1/2,-x","-z+1/2,y+1/2,-x","-z+1/2,-y+1/2,x"},
            new string[] {"217", "Cubic", "I-43m","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y,x,z","-y,-x,z","y,-x,-z","-y,x,-z","x,z,y","-x,z,-y","-x,-z,y","x,-z,-y","z,y,x","z,-y,-x","-z,y,-x","-z,-y,x","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","z+1/2,x+1/2,y+1/2","z+1/2,-x+1/2,-y+1/2","-z+1/2,-x+1/2,y+1/2","-z+1/2,x+1/2,-y+1/2","y+1/2,z+1/2,x+1/2","-y+1/2,z+1/2,-x+1/2","y+1/2,-z+1/2,-x+1/2","-y+1/2,-z+1/2,x+1/2","y+1/2,x+1/2,z+1/2","-y+1/2,-x+1/2,z+1/2","y+1/2,-x+1/2,-z+1/2","-y+1/2,x+1/2,-z+1/2","x+1/2,z+1/2,y+1/2","-x+1/2,z+1/2,-y+1/2","-x+1/2,-z+1/2,y+1/2","x+1/2,-z+1/2,-y+1/2","z+1/2,y+1/2,x+1/2","z+1/2,-y+1/2,-x+1/2","-z+1/2,y+1/2,-x+1/2","-z+1/2,-y+1/2,x+1/2"},
            new string[] {"218", "Cubic", "P-43n","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y+1/2,x+1/2,z+1/2","-y+1/2,-x+1/2,z+1/2","y+1/2,-x+1/2,-z+1/2","-y+1/2,x+1/2,-z+1/2","x+1/2,z+1/2,y+1/2","-x+1/2,z+1/2,-y+1/2","-x+1/2,-z+1/2,y+1/2","x+1/2,-z+1/2,-y+1/2","z+1/2,y+1/2,x+1/2","z+1/2,-y+1/2,-x+1/2","-z+1/2,y+1/2,-x+1/2","-z+1/2,-y+1/2,x+1/2"},
            new string[] {"219", "Cubic", "F-43c","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y+1/2,x+1/2,z+1/2","-y+1/2,-x+1/2,z+1/2","y+1/2,-x+1/2,-z+1/2","-y+1/2,x+1/2,-z+1/2","x+1/2,z+1/2,y+1/2","-x+1/2,z+1/2,-y+1/2","-x+1/2,-z+1/2,y+1/2","x+1/2,-z+1/2,-y+1/2","z+1/2,y+1/2,x+1/2","z+1/2,-y+1/2,-x+1/2","-z+1/2,y+1/2,-x+1/2","-z+1/2,-y+1/2,x+1/2","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","-x,y+1/2,-z+1/2","x,-y+1/2,-z+1/2","z,x+1/2,y+1/2","z,-x+1/2,-y+1/2","-z,-x+1/2,y+1/2","-z,x+1/2,-y+1/2","y,z+1/2,x+1/2","-y,z+1/2,-x+1/2","y,-z+1/2,-x+1/2","-y,-z+1/2,x+1/2","y+1/2,x,z","-y+1/2,-x,z","y+1/2,-x,-z","-y+1/2,x,-z","x+1/2,z,y","-x+1/2,z,-y","-x+1/2,-z,y","x+1/2,-z,-y","z+1/2,y,x","z+1/2,-y,-x","-z+1/2,y,-x","-z+1/2,-y,x","x+1/2,y,z+1/2","-x+1/2,-y,z+1/2","-x+1/2,y,-z+1/2","x+1/2,-y,-z+1/2","z+1/2,x,y+1/2","z+1/2,-x,-y+1/2","-z+1/2,-x,y+1/2","-z+1/2,x,-y+1/2","y+1/2,z,x+1/2","-y+1/2,z,-x+1/2","y+1/2,-z,-x+1/2","-y+1/2,-z,x+1/2","y,x+1/2,z","-y,-x+1/2,z","y,-x+1/2,-z","-y,x+1/2,-z","x,z+1/2,y","-x,z+1/2,-y","-x,-z+1/2,y","x,-z+1/2,-y","z,y+1/2,x","z,-y+1/2,-x","-z,y+1/2,-x","-z,-y+1/2,x","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","z+1/2,x+1/2,y","z+1/2,-x+1/2,-y","-z+1/2,-x+1/2,y","-z+1/2,x+1/2,-y","y+1/2,z+1/2,x","-y+1/2,z+1/2,-x","y+1/2,-z+1/2,-x","-y+1/2,-z+1/2,x","y,x,z+1/2","-y,-x,z+1/2","y,-x,-z+1/2","-y,x,-z+1/2","x,z,y+1/2","-x,z,-y+1/2","-x,-z,y+1/2","x,-z,-y+1/2","z,y,x+1/2","z,-y,-x+1/2","-z,y,-x+1/2","-z,-y,x+1/2"},
            new string[] {"220", "Cubic", "I-43d","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","z,x,y","z+1/2,-x+1/2,-y","-z+1/2,-x,y+1/2","-z,x+1/2,-y+1/2","y,z,x","-y,z+1/2,-x+1/2","y+1/2,-z+1/2,-x","-y+1/2,-z,x+1/2","y+1/4,x+1/4,z+1/4","-y+1/4,-x+3/4,z+3/4","y+3/4,-x+1/4,-z+3/4","-y+3/4,x+3/4,-z+1/4","x+1/4,z+1/4,y+1/4","-x+3/4,z+3/4,-y+1/4","-x+1/4,-z+3/4,y+3/4","x+3/4,-z+1/4,-y+3/4","z+1/4,y+1/4,x+1/4","z+3/4,-y+1/4,-x+3/4","-z+3/4,y+3/4,-x+1/4","-z+1/4,-y+3/4,x+3/4","x+1/2,y+1/2,z+1/2","-x,-y+1/2,z","-x+1/2,y,-z","x,-y,-z+1/2","z+1/2,x+1/2,y+1/2","z,-x,-y+1/2","-z,-x+1/2,y","-z+1/2,x,-y","y+1/2,z+1/2,x+1/2","-y+1/2,z,-x","y,-z,-x+1/2","-y,-z+1/2,x","y+3/4,x+3/4,z+3/4","-y+3/4,-x+1/4,z+1/4","y+1/4,-x+3/4,-z+1/4","-y+1/4,x+1/4,-z+3/4","x+3/4,z+3/4,y+3/4","-x+1/4,z+1/4,-y+3/4","-x+3/4,-z+1/4,y+1/4","x+1/4,-z+3/4,-y+1/4","z+3/4,y+3/4,x+3/4","z+1/4,-y+3/4,-x+1/4","-z+1/4,y+1/4,-x+3/4","-z+3/4,-y+1/4,x+1/4"},
            new string[] {"221", "Cubic", "Pm-3m","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y,x,-z","-y,-x,-z","y,-x,z","-y,x,z","x,z,-y","-x,z,y","-x,-z,-y","x,-z,y","z,y,-x","z,-y,x","-z,y,x","-z,-y,-x","-x,-y,-z","x,y,-z","x,-y,z","-x,y,z","-z,-x,-y","-z,x,y","z,x,-y","z,-x,y","-y,-z,-x","y,-z,x","-y,z,x","y,z,-x","-y,-x,z","y,x,z","-y,x,-z","y,-x,-z","-x,-z,y","x,-z,-y","x,z,y","-x,z,-y","-z,-y,x","-z,y,-x","z,-y,-x","z,y,x"},
            new string[] {"222", "Cubic", "Pn-3n","0","x,y,z","-x+1/2,-y+1/2,z","-x+1/2,y,-z+1/2","x,-y+1/2,-z+1/2","z,x,y","z,-x+1/2,-y+1/2","-z+1/2,-x+1/2,y","-z+1/2,x,-y+1/2","y,z,x","-y+1/2,z,-x+1/2","y,-z+1/2,-x+1/2","-y+1/2,-z+1/2,x","y,x,-z+1/2","-y+1/2,-x+1/2,-z+1/2","y,-x+1/2,z","-y+1/2,x,z","x,z,-y+1/2","-x+1/2,z,y","-x+1/2,-z+1/2,-y+1/2","x,-z+1/2,y","z,y,-x+1/2","z,-y+1/2,x","-z+1/2,y,x","-z+1/2,-y+1/2,-x+1/2","-x,-y,-z","x+1/2,y+1/2,-z","x+1/2,-y,z+1/2","-x,y+1/2,z+1/2","-z,-x,-y","-z,x+1/2,y+1/2","z+1/2,x+1/2,-y","z+1/2,-x,y+1/2","-y,-z,-x","y+1/2,-z,x+1/2","-y,z+1/2,x+1/2","y+1/2,z+1/2,-x","-y,-x,z+1/2","y+1/2,x+1/2,z+1/2","-y,x+1/2,-z","y+1/2,-x,-z","-x,-z,y+1/2","x+1/2,-z,-y","x+1/2,z+1/2,y+1/2","-x,z+1/2,-y","-z,-y,x+1/2","-z,y+1/2,-x","z+1/2,-y,-x","z+1/2,y+1/2,x+1/2"},
            new string[] {"223", "Cubic", "Pm-3n","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y+1/2,x+1/2,-z+1/2","-y+1/2,-x+1/2,-z+1/2","y+1/2,-x+1/2,z+1/2","-y+1/2,x+1/2,z+1/2","x+1/2,z+1/2,-y+1/2","-x+1/2,z+1/2,y+1/2","-x+1/2,-z+1/2,-y+1/2","x+1/2,-z+1/2,y+1/2","z+1/2,y+1/2,-x+1/2","z+1/2,-y+1/2,x+1/2","-z+1/2,y+1/2,x+1/2","-z+1/2,-y+1/2,-x+1/2","-x,-y,-z","x,y,-z","x,-y,z","-x,y,z","-z,-x,-y","-z,x,y","z,x,-y","z,-x,y","-y,-z,-x","y,-z,x","-y,z,x","y,z,-x","-y+1/2,-x+1/2,z+1/2","y+1/2,x+1/2,z+1/2","-y+1/2,x+1/2,-z+1/2","y+1/2,-x+1/2,-z+1/2","-x+1/2,-z+1/2,y+1/2","x+1/2,-z+1/2,-y+1/2","x+1/2,z+1/2,y+1/2","-x+1/2,z+1/2,-y+1/2","-z+1/2,-y+1/2,x+1/2","-z+1/2,y+1/2,-x+1/2","z+1/2,-y+1/2,-x+1/2","z+1/2,y+1/2,x+1/2"},
            new string[] {"224", "Cubic", "Pn-3m","0","x,y,z","-x+1/2,-y+1/2,z","-x+1/2,y,-z+1/2","x,-y+1/2,-z+1/2","z,x,y","z,-x+1/2,-y+1/2","-z+1/2,-x+1/2,y","-z+1/2,x,-y+1/2","y,z,x","-y+1/2,z,-x+1/2","y,-z+1/2,-x+1/2","-y+1/2,-z+1/2,x","y+1/2,x+1/2,-z","-y,-x,-z","y+1/2,-x,z+1/2","-y,x+1/2,z+1/2","x+1/2,z+1/2,-y","-x,z+1/2,y+1/2","-x,-z,-y","x+1/2,-z,y+1/2","z+1/2,y+1/2,-x","z+1/2,-y,x+1/2","-z,y+1/2,x+1/2","-z,-y,-x","-x,-y,-z","x+1/2,y+1/2,-z","x+1/2,-y,z+1/2","-x,y+1/2,z+1/2","-z,-x,-y","-z,x+1/2,y+1/2","z+1/2,x+1/2,-y","z+1/2,-x,y+1/2","-y,-z,-x","y+1/2,-z,x+1/2","-y,z+1/2,x+1/2","y+1/2,z+1/2,-x","-y+1/2,-x+1/2,z","y,x,z","-y+1/2,x,-z+1/2","y,-x+1/2,-z+1/2","-x+1/2,-z+1/2,y","x,-z+1/2,-y+1/2","x,z,y","-x+1/2,z,-y+1/2","-z+1/2,-y+1/2,x","-z+1/2,y,-x+1/2","z,-y+1/2,-x+1/2","z,y,x"},
            new string[] {"225", "Cubic", "Fm-3m","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y,x,-z","-y,-x,-z","y,-x,z","-y,x,z","x,z,-y","-x,z,y","-x,-z,-y","x,-z,y","z,y,-x","z,-y,x","-z,y,x","-z,-y,-x","-x,-y,-z","x,y,-z","x,-y,z","-x,y,z","-z,-x,-y","-z,x,y","z,x,-y","z,-x,y","-y,-z,-x","y,-z,x","-y,z,x","y,z,-x","-y,-x,z","y,x,z","-y,x,-z","y,-x,-z","-x,-z,y","x,-z,-y","x,z,y","-x,z,-y","-z,-y,x","-z,y,-x","z,-y,-x","z,y,x","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","-x,y+1/2,-z+1/2","x,-y+1/2,-z+1/2","z,x+1/2,y+1/2","z,-x+1/2,-y+1/2","-z,-x+1/2,y+1/2","-z,x+1/2,-y+1/2","y,z+1/2,x+1/2","-y,z+1/2,-x+1/2","y,-z+1/2,-x+1/2","-y,-z+1/2,x+1/2","y,x+1/2,-z+1/2","-y,-x+1/2,-z+1/2","y,-x+1/2,z+1/2","-y,x+1/2,z+1/2","x,z+1/2,-y+1/2","-x,z+1/2,y+1/2","-x,-z+1/2,-y+1/2","x,-z+1/2,y+1/2","z,y+1/2,-x+1/2","z,-y+1/2,x+1/2","-z,y+1/2,x+1/2","-z,-y+1/2,-x+1/2","-x,-y+1/2,-z+1/2","x,y+1/2,-z+1/2","x,-y+1/2,z+1/2","-x,y+1/2,z+1/2","-z,-x+1/2,-y+1/2","-z,x+1/2,y+1/2","z,x+1/2,-y+1/2","z,-x+1/2,y+1/2","-y,-z+1/2,-x+1/2","y,-z+1/2,x+1/2","-y,z+1/2,x+1/2","y,z+1/2,-x+1/2","-y,-x+1/2,z+1/2","y,x+1/2,z+1/2","-y,x+1/2,-z+1/2","y,-x+1/2,-z+1/2","-x,-z+1/2,y+1/2","x,-z+1/2,-y+1/2","x,z+1/2,y+1/2","-x,z+1/2,-y+1/2","-z,-y+1/2,x+1/2","-z,y+1/2,-x+1/2","z,-y+1/2,-x+1/2","z,y+1/2,x+1/2","x+1/2,y,z+1/2","-x+1/2,-y,z+1/2","-x+1/2,y,-z+1/2","x+1/2,-y,-z+1/2","z+1/2,x,y+1/2","z+1/2,-x,-y+1/2","-z+1/2,-x,y+1/2","-z+1/2,x,-y+1/2","y+1/2,z,x+1/2","-y+1/2,z,-x+1/2","y+1/2,-z,-x+1/2","-y+1/2,-z,x+1/2","y+1/2,x,-z+1/2","-y+1/2,-x,-z+1/2","y+1/2,-x,z+1/2","-y+1/2,x,z+1/2","x+1/2,z,-y+1/2","-x+1/2,z,y+1/2","-x+1/2,-z,-y+1/2","x+1/2,-z,y+1/2","z+1/2,y,-x+1/2","z+1/2,-y,x+1/2","-z+1/2,y,x+1/2","-z+1/2,-y,-x+1/2","-x+1/2,-y,-z+1/2","x+1/2,y,-z+1/2","x+1/2,-y,z+1/2","-x+1/2,y,z+1/2","-z+1/2,-x,-y+1/2","-z+1/2,x,y+1/2","z+1/2,x,-y+1/2","z+1/2,-x,y+1/2","-y+1/2,-z,-x+1/2","y+1/2,-z,x+1/2","-y+1/2,z,x+1/2","y+1/2,z,-x+1/2","-y+1/2,-x,z+1/2","y+1/2,x,z+1/2","-y+1/2,x,-z+1/2","y+1/2,-x,-z+1/2","-x+1/2,-z,y+1/2","x+1/2,-z,-y+1/2","x+1/2,z,y+1/2","-x+1/2,z,-y+1/2","-z+1/2,-y,x+1/2","-z+1/2,y,-x+1/2","z+1/2,-y,-x+1/2","z+1/2,y,x+1/2","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","z+1/2,x+1/2,y","z+1/2,-x+1/2,-y","-z+1/2,-x+1/2,y","-z+1/2,x+1/2,-y","y+1/2,z+1/2,x","-y+1/2,z+1/2,-x","y+1/2,-z+1/2,-x","-y+1/2,-z+1/2,x","y+1/2,x+1/2,-z","-y+1/2,-x+1/2,-z","y+1/2,-x+1/2,z","-y+1/2,x+1/2,z","x+1/2,z+1/2,-y","-x+1/2,z+1/2,y","-x+1/2,-z+1/2,-y","x+1/2,-z+1/2,y","z+1/2,y+1/2,-x","z+1/2,-y+1/2,x","-z+1/2,y+1/2,x","-z+1/2,-y+1/2,-x","-x+1/2,-y+1/2,-z","x+1/2,y+1/2,-z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","-z+1/2,-x+1/2,-y","-z+1/2,x+1/2,y","z+1/2,x+1/2,-y","z+1/2,-x+1/2,y","-y+1/2,-z+1/2,-x","y+1/2,-z+1/2,x","-y+1/2,z+1/2,x","y+1/2,z+1/2,-x","-y+1/2,-x+1/2,z","y+1/2,x+1/2,z","-y+1/2,x+1/2,-z","y+1/2,-x+1/2,-z","-x+1/2,-z+1/2,y","x+1/2,-z+1/2,-y","x+1/2,z+1/2,y","-x+1/2,z+1/2,-y","-z+1/2,-y+1/2,x","-z+1/2,y+1/2,-x","z+1/2,-y+1/2,-x","z+1/2,y+1/2,x"},
            new string[] {"226", "Cubic", "Fm-3c","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y+1/2,x+1/2,-z+1/2","-y+1/2,-x+1/2,-z+1/2","y+1/2,-x+1/2,z+1/2","-y+1/2,x+1/2,z+1/2","x+1/2,z+1/2,-y+1/2","-x+1/2,z+1/2,y+1/2","-x+1/2,-z+1/2,-y+1/2","x+1/2,-z+1/2,y+1/2","z+1/2,y+1/2,-x+1/2","z+1/2,-y+1/2,x+1/2","-z+1/2,y+1/2,x+1/2","-z+1/2,-y+1/2,-x+1/2","-x,-y,-z","x,y,-z","x,-y,z","-x,y,z","-z,-x,-y","-z,x,y","z,x,-y","z,-x,y","-y,-z,-x","y,-z,x","-y,z,x","y,z,-x","-y+1/2,-x+1/2,z+1/2","y+1/2,x+1/2,z+1/2","-y+1/2,x+1/2,-z+1/2","y+1/2,-x+1/2,-z+1/2","-x+1/2,-z+1/2,y+1/2","x+1/2,-z+1/2,-y+1/2","x+1/2,z+1/2,y+1/2","-x+1/2,z+1/2,-y+1/2","-z+1/2,-y+1/2,x+1/2","-z+1/2,y+1/2,-x+1/2","z+1/2,-y+1/2,-x+1/2","z+1/2,y+1/2,x+1/2","x,y+1/2,z+1/2","-x,-y+1/2,z+1/2","-x,y+1/2,-z+1/2","x,-y+1/2,-z+1/2","z,x+1/2,y+1/2","z,-x+1/2,-y+1/2","-z,-x+1/2,y+1/2","-z,x+1/2,-y+1/2","y,z+1/2,x+1/2","-y,z+1/2,-x+1/2","y,-z+1/2,-x+1/2","-y,-z+1/2,x+1/2","y+1/2,x,-z","-y+1/2,-x,-z","y+1/2,-x,z","-y+1/2,x,z","x+1/2,z,-y","-x+1/2,z,y","-x+1/2,-z,-y","x+1/2,-z,y","z+1/2,y,-x","z+1/2,-y,x","-z+1/2,y,x","-z+1/2,-y,-x","-x,-y+1/2,-z+1/2","x,y+1/2,-z+1/2","x,-y+1/2,z+1/2","-x,y+1/2,z+1/2","-z,-x+1/2,-y+1/2","-z,x+1/2,y+1/2","z,x+1/2,-y+1/2","z,-x+1/2,y+1/2","-y,-z+1/2,-x+1/2","y,-z+1/2,x+1/2","-y,z+1/2,x+1/2","y,z+1/2,-x+1/2","-y+1/2,-x,z","y+1/2,x,z","-y+1/2,x,-z","y+1/2,-x,-z","-x+1/2,-z,y","x+1/2,-z,-y","x+1/2,z,y","-x+1/2,z,-y","-z+1/2,-y,x","-z+1/2,y,-x","z+1/2,-y,-x","z+1/2,y,x","x+1/2,y,z+1/2","-x+1/2,-y,z+1/2","-x+1/2,y,-z+1/2","x+1/2,-y,-z+1/2","z+1/2,x,y+1/2","z+1/2,-x,-y+1/2","-z+1/2,-x,y+1/2","-z+1/2,x,-y+1/2","y+1/2,z,x+1/2","-y+1/2,z,-x+1/2","y+1/2,-z,-x+1/2","-y+1/2,-z,x+1/2","y,x+1/2,-z","-y,-x+1/2,-z","y,-x+1/2,z","-y,x+1/2,z","x,z+1/2,-y","-x,z+1/2,y","-x,-z+1/2,-y","x,-z+1/2,y","z,y+1/2,-x","z,-y+1/2,x","-z,y+1/2,x","-z,-y+1/2,-x","-x+1/2,-y,-z+1/2","x+1/2,y,-z+1/2","x+1/2,-y,z+1/2","-x+1/2,y,z+1/2","-z+1/2,-x,-y+1/2","-z+1/2,x,y+1/2","z+1/2,x,-y+1/2","z+1/2,-x,y+1/2","-y+1/2,-z,-x+1/2","y+1/2,-z,x+1/2","-y+1/2,z,x+1/2","y+1/2,z,-x+1/2","-y,-x+1/2,z","y,x+1/2,z","-y,x+1/2,-z","y,-x+1/2,-z","-x,-z+1/2,y","x,-z+1/2,-y","x,z+1/2,y","-x,z+1/2,-y","-z,-y+1/2,x","-z,y+1/2,-x","z,-y+1/2,-x","z,y+1/2,x","x+1/2,y+1/2,z","-x+1/2,-y+1/2,z","-x+1/2,y+1/2,-z","x+1/2,-y+1/2,-z","z+1/2,x+1/2,y","z+1/2,-x+1/2,-y","-z+1/2,-x+1/2,y","-z+1/2,x+1/2,-y","y+1/2,z+1/2,x","-y+1/2,z+1/2,-x","y+1/2,-z+1/2,-x","-y+1/2,-z+1/2,x","y,x,-z+1/2","-y,-x,-z+1/2","y,-x,z+1/2","-y,x,z+1/2","x,z,-y+1/2","-x,z,y+1/2","-x,-z,-y+1/2","x,-z,y+1/2","z,y,-x+1/2","z,-y,x+1/2","-z,y,x+1/2","-z,-y,-x+1/2","-x+1/2,-y+1/2,-z","x+1/2,y+1/2,-z","x+1/2,-y+1/2,z","-x+1/2,y+1/2,z","-z+1/2,-x+1/2,-y","-z+1/2,x+1/2,y","z+1/2,x+1/2,-y","z+1/2,-x+1/2,y","-y+1/2,-z+1/2,-x","y+1/2,-z+1/2,x","-y+1/2,z+1/2,x","y+1/2,z+1/2,-x","-y,-x,z+1/2","y,x,z+1/2","-y,x,-z+1/2","y,-x,-z+1/2","-x,-z,y+1/2","x,-z,-y+1/2","x,z,y+1/2","-x,z,-y+1/2","-z,-y,x+1/2","-z,y,-x+1/2","z,-y,-x+1/2","z,y,x+1/2"},
            new string[] {"227", "Cubic", "Fd-3m","0","x,y,z","-x+3/4,-y+1/4,z+1/2","-x+1/4,y+1/2,-z+3/4","x+1/2,-y+3/4,-z+1/4","z,x,y","z+1/2,-x+3/4,-y+1/4","-z+3/4,-x+1/4,y+1/2","-z+1/4,x+1/2,-y+3/4","y,z,x","-y+1/4,z+1/2,-x+3/4","y+1/2,-z+3/4,-x+1/4","-y+3/4,-z+1/4,x+1/2","y+3/4,x+1/4,-z+1/2","-y,-x,-z","y+1/4,-x+1/2,z+3/4","-y+1/2,x+3/4,z+1/4","x+3/4,z+1/4,-y+1/2","-x+1/2,z+3/4,y+1/4","-x,-z,-y","x+1/4,-z+1/2,y+3/4","z+3/4,y+1/4,-x+1/2","z+1/4,-y+1/2,x+3/4","-z+1/2,y+3/4,x+1/4","-z,-y,-x","-x,-y,-z","x+1/4,y+3/4,-z+1/2","x+3/4,-y+1/2,z+1/4","-x+1/2,y+1/4,z+3/4","-z,-x,-y","-z+1/2,x+1/4,y+3/4","z+1/4,x+3/4,-y+1/2","z+3/4,-x+1/2,y+1/4","-y,-z,-x","y+3/4,-z+1/2,x+1/4","-y+1/2,z+1/4,x+3/4","y+1/4,z+3/4,-x+1/2","-y+1/4,-x+3/4,z+1/2","y,x,z","-y+3/4,x+1/2,-z+1/4","y+1/2,-x+1/4,-z+3/4","-x+1/4,-z+3/4,y+1/2","x+1/2,-z+1/4,-y+3/4","x,z,y","-x+3/4,z+1/2,-y+1/4","-z+1/4,-y+3/4,x+1/2","-z+3/4,y+1/2,-x+1/4","z+1/2,-y+1/4,-x+3/4","z,y,x","x,y+1/2,z+1/2","-x+3/4,-y+3/4,z","-x+1/4,y,-z+1/4","x+1/2,-y+1/4,-z+3/4","z,x+1/2,y+1/2","z+1/2,-x+1/4,-y+3/4","-z+3/4,-x+3/4,y","-z+1/4,x,-y+1/4","y,z+1/2,x+1/2","-y+1/4,z,-x+1/4","y+1/2,-z+1/4,-x+3/4","-y+3/4,-z+3/4,x","y+3/4,x+3/4,-z","-y,-x+1/2,-z+1/2","y+1/4,-x,z+1/4","-y+1/2,x+1/4,z+3/4","x+3/4,z+3/4,-y","-x+1/2,z+1/4,y+3/4","-x,-z+1/2,-y+1/2","x+1/4,-z,y+1/4","z+3/4,y+3/4,-x","z+1/4,-y,x+1/4","-z+1/2,y+1/4,x+3/4","-z,-y+1/2,-x+1/2","-x,-y+1/2,-z+1/2","x+1/4,y+1/4,-z","x+3/4,-y,z+3/4","-x+1/2,y+3/4,z+1/4","-z,-x+1/2,-y+1/2","-z+1/2,x+3/4,y+1/4","z+1/4,x+1/4,-y","z+3/4,-x,y+3/4","-y,-z+1/2,-x+1/2","y+3/4,-z,x+3/4","-y+1/2,z+3/4,x+1/4","y+1/4,z+1/4,-x","-y+1/4,-x+1/4,z","y,x+1/2,z+1/2","-y+3/4,x,-z+3/4","y+1/2,-x+3/4,-z+1/4","-x+1/4,-z+1/4,y","x+1/2,-z+3/4,-y+1/4","x,z+1/2,y+1/2","-x+3/4,z,-y+3/4","-z+1/4,-y+1/4,x","-z+3/4,y,-x+3/4","z+1/2,-y+3/4,-x+1/4","z,y+1/2,x+1/2","x+1/2,y,z+1/2","-x+1/4,-y+1/4,z","-x+3/4,y+1/2,-z+1/4","x,-y+3/4,-z+3/4","z+1/2,x,y+1/2","z,-x+3/4,-y+3/4","-z+1/4,-x+1/4,y","-z+3/4,x+1/2,-y+1/4","y+1/2,z,x+1/2","-y+3/4,z+1/2,-x+1/4","y,-z+3/4,-x+3/4","-y+1/4,-z+1/4,x","y+1/4,x+1/4,-z","-y+1/2,-x,-z+1/2","y+3/4,-x+1/2,z+1/4","-y,x+3/4,z+3/4","x+1/4,z+1/4,-y","-x,z+3/4,y+3/4","-x+1/2,-z,-y+1/2","x+3/4,-z+1/2,y+1/4","z+1/4,y+1/4,-x","z+3/4,-y+1/2,x+1/4","-z,y+3/4,x+3/4","-z+1/2,-y,-x+1/2","-x+1/2,-y,-z+1/2","x+3/4,y+3/4,-z","x+1/4,-y+1/2,z+3/4","-x,y+1/4,z+1/4","-z+1/2,-x,-y+1/2","-z,x+1/4,y+1/4","z+3/4,x+3/4,-y","z+1/4,-x+1/2,y+3/4","-y+1/2,-z,-x+1/2","y+1/4,-z+1/2,x+3/4","-y,z+1/4,x+1/4","y+3/4,z+3/4,-x","-y+3/4,-x+3/4,z","y+1/2,x,z+1/2","-y+1/4,x+1/2,-z+3/4","y,-x+1/4,-z+1/4","-x+3/4,-z+3/4,y","x,-z+1/4,-y+1/4","x+1/2,z,y+1/2","-x+1/4,z+1/2,-y+3/4","-z+3/4,-y+3/4,x","-z+1/4,y+1/2,-x+3/4","z,-y+1/4,-x+1/4","z+1/2,y,x+1/2","x+1/2,y+1/2,z","-x+1/4,-y+3/4,z+1/2","-x+3/4,y,-z+3/4","x,-y+1/4,-z+1/4","z+1/2,x+1/2,y","z,-x+1/4,-y+1/4","-z+1/4,-x+3/4,y+1/2","-z+3/4,x,-y+3/4","y+1/2,z+1/2,x","-y+3/4,z,-x+3/4","y,-z+1/4,-x+1/4","-y+1/4,-z+3/4,x+1/2","y+1/4,x+3/4,-z+1/2","-y+1/2,-x+1/2,-z","y+3/4,-x,z+3/4","-y,x+1/4,z+1/4","x+1/4,z+3/4,-y+1/2","-x,z+1/4,y+1/4","-x+1/2,-z+1/2,-y","x+3/4,-z,y+3/4","z+1/4,y+3/4,-x+1/2","z+3/4,-y,x+3/4","-z,y+1/4,x+1/4","-z+1/2,-y+1/2,-x","-x+1/2,-y+1/2,-z","x+3/4,y+1/4,-z+1/2","x+1/4,-y,z+1/4","-x,y+3/4,z+3/4","-z+1/2,-x+1/2,-y","-z,x+3/4,y+3/4","z+3/4,x+1/4,-y+1/2","z+1/4,-x,y+1/4","-y+1/2,-z+1/2,-x","y+1/4,-z,x+1/4","-y,z+3/4,x+3/4","y+3/4,z+1/4,-x+1/2","-y+3/4,-x+1/4,z+1/2","y+1/2,x+1/2,z","-y+1/4,x,-z+1/4","y,-x+3/4,-z+3/4","-x+3/4,-z+1/4,y+1/2","x,-z+3/4,-y+3/4","x+1/2,z+1/2,y","-x+1/4,z,-y+1/4","-z+3/4,-y+1/4,x+1/2","-z+1/4,y,-x+1/4","z,-y+3/4,-x+3/4","z+1/2,y+1/2,x"},
            new string[] {"228", "Cubic", "Fd-3c","0","x,y,z","-x+1/4,-y+3/4,z+1/2","-x+3/4,y+1/2,-z+1/4","x+1/2,-y+1/4,-z+3/4","z,x,y","z+1/2,-x+1/4,-y+3/4","-z+1/4,-x+3/4,y+1/2","-z+3/4,x+1/2,-y+1/4","y,z,x","-y+3/4,z+1/2,-x+1/4","y+1/2,-z+1/4,-x+3/4","-y+1/4,-z+3/4,x+1/2","y+3/4,x+1/4,-z","-y+1/2,-x+1/2,-z+1/2","y+1/4,-x,z+3/4","-y,x+3/4,z+1/4","x+3/4,z+1/4,-y","-x,z+3/4,y+1/4","-x+1/2,-z+1/2,-y+1/2","x+1/4,-z,y+3/4","z+3/4,y+1/4,-x","z+1/4,-y,x+3/4","-z,y+3/4,x+1/4","-z+1/2,-y+1/2,-x+1/2","-x,-y,-z","x+3/4,y+1/4,-z+1/2","x+1/4,-y+1/2,z+3/4","-x+1/2,y+3/4,z+1/4","-z,-x,-y","-z+1/2,x+3/4,y+1/4","z+3/4,x+1/4,-y+1/2","z+1/4,-x+1/2,y+3/4","-y,-z,-x","y+1/4,-z+1/2,x+3/4","-y+1/2,z+3/4,x+1/4","y+3/4,z+1/4,-x+1/2","-y+1/4,-x+3/4,z","y+1/2,x+1/2,z+1/2","-y+3/4,x,-z+1/4","y,-x+1/4,-z+3/4","-x+1/4,-z+3/4,y","x,-z+1/4,-y+3/4","x+1/2,z+1/2,y+1/2","-x+3/4,z,-y+1/4","-z+1/4,-y+3/4,x","-z+3/4,y,-x+1/4","z,-y+1/4,-x+3/4","z+1/2,y+1/2,x+1/2","x,y+1/2,z+1/2","-x+1/4,-y+1/4,z","-x+3/4,y,-z+3/4","x+1/2,-y+3/4,-z+1/4","z,x+1/2,y+1/2","z+1/2,-x+3/4,-y+1/4","-z+1/4,-x+1/4,y","-z+3/4,x,-y+3/4","y,z+1/2,x+1/2","-y+3/4,z,-x+3/4","y+1/2,-z+3/4,-x+1/4","-y+1/4,-z+1/4,x","y+3/4,x+3/4,-z+1/2","-y+1/2,-x,-z","y+1/4,-x+1/2,z+1/4","-y,x+1/4,z+3/4","x+3/4,z+3/4,-y+1/2","-x,z+1/4,y+3/4","-x+1/2,-z,-y","x+1/4,-z+1/2,y+1/4","z+3/4,y+3/4,-x+1/2","z+1/4,-y+1/2,x+1/4","-z,y+1/4,x+3/4","-z+1/2,-y,-x","-x,-y+1/2,-z+1/2","x+3/4,y+3/4,-z","x+1/4,-y,z+1/4","-x+1/2,y+1/4,z+3/4","-z,-x+1/2,-y+1/2","-z+1/2,x+1/4,y+3/4","z+3/4,x+3/4,-y","z+1/4,-x,y+1/4","-y,-z+1/2,-x+1/2","y+1/4,-z,x+1/4","-y+1/2,z+1/4,x+3/4","y+3/4,z+3/4,-x","-y+1/4,-x+1/4,z+1/2","y+1/2,x,z","-y+3/4,x+1/2,-z+3/4","y,-x+3/4,-z+1/4","-x+1/4,-z+1/4,y+1/2","x,-z+3/4,-y+1/4","x+1/2,z,y","-x+3/4,z+1/2,-y+3/4","-z+1/4,-y+1/4,x+1/2","-z+3/4,y+1/2,-x+3/4","z,-y+3/4,-x+1/4","z+1/2,y,x","x+1/2,y,z+1/2","-x+3/4,-y+3/4,z","-x+1/4,y+1/2,-z+3/4","x,-y+1/4,-z+1/4","z+1/2,x,y+1/2","z,-x+1/4,-y+1/4","-z+3/4,-x+3/4,y","-z+1/4,x+1/2,-y+3/4","y+1/2,z,x+1/2","-y+1/4,z+1/2,-x+3/4","y,-z+1/4,-x+1/4","-y+3/4,-z+3/4,x","y+1/4,x+1/4,-z+1/2","-y,-x+1/2,-z","y+3/4,-x,z+1/4","-y+1/2,x+3/4,z+3/4","x+1/4,z+1/4,-y+1/2","-x+1/2,z+3/4,y+3/4","-x,-z+1/2,-y","x+3/4,-z,y+1/4","z+1/4,y+1/4,-x+1/2","z+3/4,-y,x+1/4","-z+1/2,y+3/4,x+3/4","-z,-y+1/2,-x","-x+1/2,-y,-z+1/2","x+1/4,y+1/4,-z","x+3/4,-y+1/2,z+1/4","-x,y+3/4,z+3/4","-z+1/2,-x,-y+1/2","-z,x+3/4,y+3/4","z+1/4,x+1/4,-y","z+3/4,-x+1/2,y+1/4","-y+1/2,-z,-x+1/2","y+3/4,-z+1/2,x+1/4","-y,z+3/4,x+3/4","y+1/4,z+1/4,-x","-y+3/4,-x+3/4,z+1/2","y,x+1/2,z","-y+1/4,x,-z+3/4","y+1/2,-x+1/4,-z+1/4","-x+3/4,-z+3/4,y+1/2","x+1/2,-z+1/4,-y+1/4","x,z+1/2,y","-x+1/4,z,-y+3/4","-z+3/4,-y+3/4,x+1/2","-z+1/4,y,-x+3/4","z+1/2,-y+1/4,-x+1/4","z,y+1/2,x","x+1/2,y+1/2,z","-x+3/4,-y+1/4,z+1/2","-x+1/4,y,-z+1/4","x,-y+3/4,-z+3/4","z+1/2,x+1/2,y","z,-x+3/4,-y+3/4","-z+3/4,-x+1/4,y+1/2","-z+1/4,x,-y+1/4","y+1/2,z+1/2,x","-y+1/4,z,-x+1/4","y,-z+3/4,-x+3/4","-y+3/4,-z+1/4,x+1/2","y+1/4,x+3/4,-z","-y,-x,-z+1/2","y+3/4,-x+1/2,z+3/4","-y+1/2,x+1/4,z+1/4","x+1/4,z+3/4,-y","-x+1/2,z+1/4,y+1/4","-x,-z,-y+1/2","x+3/4,-z+1/2,y+3/4","z+1/4,y+3/4,-x","z+3/4,-y+1/2,x+3/4","-z+1/2,y+1/4,x+1/4","-z,-y,-x+1/2","-x+1/2,-y+1/2,-z","x+1/4,y+3/4,-z+1/2","x+3/4,-y,z+3/4","-x,y+1/4,z+1/4","-z+1/2,-x+1/2,-y","-z,x+1/4,y+1/4","z+1/4,x+3/4,-y+1/2","z+3/4,-x,y+3/4","-y+1/2,-z+1/2,-x","y+3/4,-z,x+3/4","-y,z+1/4,x+1/4","y+1/4,z+3/4,-x+1/2","-y+3/4,-x+1/4,z","y,x,z+1/2","-y+1/4,x+1/2,-z+1/4","y+1/2,-x+3/4,-z+3/4","-x+3/4,-z+1/4,y","x+1/2,-z+3/4,-y+3/4","x,z,y+1/2","-x+1/4,z+1/2,-y+1/4","-z+3/4,-y+1/4,x","-z+1/4,y+1/2,-x+1/4","z+1/2,-y+3/4,-x+3/4","z,y,x+1/2"},
            new string[] {"229", "Cubic", "Im-3m","0","x,y,z","-x,-y,z","-x,y,-z","x,-y,-z","z,x,y","z,-x,-y","-z,-x,y","-z,x,-y","y,z,x","-y,z,-x","y,-z,-x","-y,-z,x","y,x,-z","-y,-x,-z","y,-x,z","-y,x,z","x,z,-y","-x,z,y","-x,-z,-y","x,-z,y","z,y,-x","z,-y,x","-z,y,x","-z,-y,-x","-x,-y,-z","x,y,-z","x,-y,z","-x,y,z","-z,-x,-y","-z,x,y","z,x,-y","z,-x,y","-y,-z,-x","y,-z,x","-y,z,x","y,z,-x","-y,-x,z","y,x,z","-y,x,-z","y,-x,-z","-x,-z,y","x,-z,-y","x,z,y","-x,z,-y","-z,-y,x","-z,y,-x","z,-y,-x","z,y,x","x+1/2,y+1/2,z+1/2","-x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,-z+1/2","z+1/2,x+1/2,y+1/2","z+1/2,-x+1/2,-y+1/2","-z+1/2,-x+1/2,y+1/2","-z+1/2,x+1/2,-y+1/2","y+1/2,z+1/2,x+1/2","-y+1/2,z+1/2,-x+1/2","y+1/2,-z+1/2,-x+1/2","-y+1/2,-z+1/2,x+1/2","y+1/2,x+1/2,-z+1/2","-y+1/2,-x+1/2,-z+1/2","y+1/2,-x+1/2,z+1/2","-y+1/2,x+1/2,z+1/2","x+1/2,z+1/2,-y+1/2","-x+1/2,z+1/2,y+1/2","-x+1/2,-z+1/2,-y+1/2","x+1/2,-z+1/2,y+1/2","z+1/2,y+1/2,-x+1/2","z+1/2,-y+1/2,x+1/2","-z+1/2,y+1/2,x+1/2","-z+1/2,-y+1/2,-x+1/2","-x+1/2,-y+1/2,-z+1/2","x+1/2,y+1/2,-z+1/2","x+1/2,-y+1/2,z+1/2","-x+1/2,y+1/2,z+1/2","-z+1/2,-x+1/2,-y+1/2","-z+1/2,x+1/2,y+1/2","z+1/2,x+1/2,-y+1/2","z+1/2,-x+1/2,y+1/2","-y+1/2,-z+1/2,-x+1/2","y+1/2,-z+1/2,x+1/2","-y+1/2,z+1/2,x+1/2","y+1/2,z+1/2,-x+1/2","-y+1/2,-x+1/2,z+1/2","y+1/2,x+1/2,z+1/2","-y+1/2,x+1/2,-z+1/2","y+1/2,-x+1/2,-z+1/2","-x+1/2,-z+1/2,y+1/2","x+1/2,-z+1/2,-y+1/2","x+1/2,z+1/2,y+1/2","-x+1/2,z+1/2,-y+1/2","-z+1/2,-y+1/2,x+1/2","-z+1/2,y+1/2,-x+1/2","z+1/2,-y+1/2,-x+1/2","z+1/2,y+1/2,x+1/2"},
            new string[] {"230", "Cubic", "Ia-3d","0","x,y,z","-x+1/2,-y,z+1/2","-x,y+1/2,-z+1/2","x+1/2,-y+1/2,-z","z,x,y","z+1/2,-x+1/2,-y","-z+1/2,-x,y+1/2","-z,x+1/2,-y+1/2","y,z,x","-y,z+1/2,-x+1/2","y+1/2,-z+1/2,-x","-y+1/2,-z,x+1/2","y+3/4,x+1/4,-z+1/4","-y+3/4,-x+3/4,-z+3/4","y+1/4,-x+1/4,z+3/4","-y+1/4,x+3/4,z+1/4","x+3/4,z+1/4,-y+1/4","-x+1/4,z+3/4,y+1/4","-x+3/4,-z+3/4,-y+3/4","x+1/4,-z+1/4,y+3/4","z+3/4,y+1/4,-x+1/4","z+1/4,-y+1/4,x+3/4","-z+1/4,y+3/4,x+1/4","-z+3/4,-y+3/4,-x+3/4","-x,-y,-z","x+1/2,y,-z+1/2","x,-y+1/2,z+1/2","-x+1/2,y+1/2,z","-z,-x,-y","-z+1/2,x+1/2,y","z+1/2,x,-y+1/2","z,-x+1/2,y+1/2","-y,-z,-x","y,-z+1/2,x+1/2","-y+1/2,z+1/2,x","y+1/2,z,-x+1/2","-y+1/4,-x+3/4,z+3/4","y+1/4,x+1/4,z+1/4","-y+3/4,x+3/4,-z+1/4","y+3/4,-x+1/4,-z+3/4","-x+1/4,-z+3/4,y+3/4","x+3/4,-z+1/4,-y+3/4","x+1/4,z+1/4,y+1/4","-x+3/4,z+3/4,-y+1/4","-z+1/4,-y+3/4,x+3/4","-z+3/4,y+3/4,-x+1/4","z+3/4,-y+1/4,-x+3/4","z+1/4,y+1/4,x+1/4","x+1/2,y+1/2,z+1/2","-x,-y+1/2,z","-x+1/2,y,-z","x,-y,-z+1/2","z+1/2,x+1/2,y+1/2","z,-x,-y+1/2","-z,-x+1/2,y","-z+1/2,x,-y","y+1/2,z+1/2,x+1/2","-y+1/2,z,-x","y,-z,-x+1/2","-y,-z+1/2,x","y+1/4,x+3/4,-z+3/4","-y+1/4,-x+1/4,-z+1/4","y+3/4,-x+3/4,z+1/4","-y+3/4,x+1/4,z+3/4","x+1/4,z+3/4,-y+3/4","-x+3/4,z+1/4,y+3/4","-x+1/4,-z+1/4,-y+1/4","x+3/4,-z+3/4,y+1/4","z+1/4,y+3/4,-x+3/4","z+3/4,-y+3/4,x+1/4","-z+3/4,y+1/4,x+3/4","-z+1/4,-y+1/4,-x+1/4","-x+1/2,-y+1/2,-z+1/2","x,y+1/2,-z","x+1/2,-y,z","-x,y,z+1/2","-z+1/2,-x+1/2,-y+1/2","-z,x,y+1/2","z,x+1/2,-y","z+1/2,-x,y","-y+1/2,-z+1/2,-x+1/2","y+1/2,-z,x","-y,z,x+1/2","y,z+1/2,-x","-y+3/4,-x+1/4,z+1/4","y+3/4,x+3/4,z+3/4","-y+1/4,x+1/4,-z+3/4","y+1/4,-x+3/4,-z+1/4","-x+3/4,-z+1/4,y+1/4","x+1/4,-z+3/4,-y+1/4","x+3/4,z+3/4,y+3/4","-x+1/4,z+1/4,-y+3/4","-z+3/4,-y+1/4,x+1/4","-z+1/4,y+1/4,-x+3/4","z+1/4,-y+3/4,-x+1/4","z+3/4,y+3/4,x+3/4"}
        };

        #endregion

        #region Space Group Name Mapping

 /// <summary>
 /// Dictionary mapping normalized space group names to International Tables numbers.
 /// Key: nazwa normalised (lowercase, without spacji) — patrz NormalizeSpaceGroupName.
 /// Value: numer grupy w International Tables (1-230).
 ///
 /// NOTE: warianty orientacji (np. P21/c vs P21/n) have TEN SAM numer.
 /// The difference is recognized by GetOrientationForGroup().
 /// </summary>
        public static Dictionary<string, int> GroupNameToNumber =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
 // ═══════════════════════════════════════════════════════════
 // TRICLINIC (1-2)
 // ═══════════════════════════════════════════════════════════
    { "p1",  1 },
    { "p-1", 2 },

 // ═══════════════════════════════════════════════════════════
 // MONOCLINIC (3-15)
 // ═══════════════════════════════════════════════════════════
    { "p2",    3 },
    { "p21",   4 },

    { "c2",    5 },

    { "pm",    6 },
    { "pc",    7 },

    { "cm",    8 },
    { "cc",    9 },

    { "p2/m",  10 },
    { "p21/m", 11 },
    { "c2/m",  12 },

 // Grupa 13 — P2/c i warianty
    { "p2/c",  13 },
    { "p2/n",  13 },
    { "p2/a",  13 },

 // Grupa 14 — P21/c i warianty
    { "p21/c", 14 },
    { "p21/n", 14 },
    { "p21/a", 14 },
    { "p21/b", 14 },

    { "c2/c",  15 },
    { "c2/n",  15 },

 // ═══════════════════════════════════════════════════════════
 // ORTHORHOMBIC (16-74)
 // ═══════════════════════════════════════════════════════════
    { "p222",    16 },
    { "p2221",   17 },
    { "p21212",  18 },
    { "p212121", 19 },

    { "c2221",   20 },
    { "c222",    21 },
    { "f222",    22 },
    { "i222",    23 },
    { "i212121", 24 },

    { "pmm2",    25 },
    { "pmc21",   26 },
    { "pcc2",    27 },
    { "pma2",    28 },
    { "pca21",   29 },
    { "pnc2",    30 },
    { "pmn21",   31 },
    { "pba2",    32 },
    { "pna21",   33 },
    { "pnn2",    34 },

    { "cmm2",    35 },
    { "cmc21",   36 },
    { "ccc2",    37 },

    { "amm2",    38 },
    { "abm2",    39 },
    { "ama2",    40 },
    { "aba2",    41 },

    { "fmm2",    42 },
    { "fdd2",    43 },

    { "imm2",    44 },
    { "iba2",    45 },
    { "ima2",    46 },

    { "pmmm",    47 },
    { "pnnn",    48 },
    { "pccm",    49 },
    { "pban",    50 },
    { "pmma",    51 },
    { "pnna",    52 },
    { "pmna",    53 },
    { "pcca",    54 },
    { "pbam",    55 },
    { "pccn",    56 },
    { "pbcm",    57 },
    { "pnnm",    58 },
    { "pmmn",    59 },
    { "pbcn",    60 },
    { "pbca",    61 },

 // Grupa 62 — Pnma (standard) i warianty
    { "pnma",    62 },
    { "pbnm",    62 },
    { "pmcn",    62 },
    { "pnam",    62 },
    { "pmnb",    62 },

 // Grupa 63 — Cmcm (standard) — NIE dodawaj cmca (to grupa 64!)
    { "cmcm",    63 },

 // Grupa 64 — Cmca (standard) — NIE dodawaj cmcm
    { "cmca",    64 },

    { "cmmm",    65 },
    { "cccm",    66 },
    { "cmma",    67 },
    { "ccca",    68 },
    { "fmmm",    69 },
    { "fddd",    70 },
    { "immm",    71 },
    { "ibam",    72 },
    { "ibca",    73 },
    { "imma",    74 },

 // ═══════════════════════════════════════════════════════════
 // TETRAGONAL (75-142)
 // ═══════════════════════════════════════════════════════════
    { "p4",       75 },
    { "p41",      76 },
    { "p42",      77 },
    { "p43",      78 },
    { "i4",       79 },
    { "i41",      80 },
    { "p-4",      81 },
    { "i-4",      82 },
    { "p4/m",     83 },
    { "p42/m",    84 },
    { "p4/n",     85 },
    { "p42/n",    86 },
    { "i4/m",     87 },
    { "i41/a",    88 },
    { "p422",     89 },
    { "p4212",    90 },
    { "p4122",    91 },
    { "p41212",   92 },
    { "p4222",    93 },
    { "p42212",   94 },
    { "p4322",    95 },
    { "p43212",   96 },
    { "i422",     97 },
    { "i4122",    98 },
    { "p4mm",     99 },
    { "p4bm",     100 },
    { "p42cm",    101 },
    { "p42nm",    102 },
    { "p4cc",     103 },
    { "p4nc",     104 },
    { "p42mc",    105 },
    { "p42bc",    106 },
    { "i4mm",     107 },
    { "i4cm",     108 },
    { "i41md",    109 },
    { "i41cd",    110 },
    { "p-42m",    111 },
    { "p-42c",    112 },
    { "p-421m",   113 },
    { "p-421c",   114 },
    { "p-4m2",    115 },
    { "p-4c2",    116 },
    { "p-4b2",    117 },
    { "p-4n2",    118 },
    { "i-4m2",    119 },
    { "i-4c2",    120 },
    { "i-42m",    121 },
    { "i-42d",    122 },
    { "p4/mmm",   123 },
    { "p4/mcc",   124 },
    { "p4/nbm",   125 },
    { "p4/nnc",   126 },
    { "p4/mbm",   127 },
    { "p4/mnc",   128 },
    { "p4/nmm",   129 },
    { "p4/ncc",   130 },
    { "p42/mmc",  131 },
    { "p42/mcm",  132 },
    { "p42/nbc",  133 },
    { "p42/nnm",  134 },
    { "p42/mbc",  135 },
    { "p42/mnm",  136 },
    { "p42/nmc",  137 },
    { "p42/ncm",  138 },
    { "i4/mmm",   139 },
    { "i4/mcm",   140 },
    { "i41/amd",  141 },
    { "i41/acd",  142 },

 // ═══════════════════════════════════════════════════════════
 // TRIGONAL (143-167)
 // ═══════════════════════════════════════════════════════════
    { "p3",      143 },
    { "p31",     144 },
    { "p32",     145 },
    { "r3",      146 },
    { "p-3",     147 },
    { "r-3",     148 },
    { "p312",    149 },
    { "p321",    150 },
    { "p3112",   151 },
    { "p3121",   152 },
    { "p3212",   153 },
    { "p3221",   154 },
    { "r32",     155 },
    { "p3m1",    156 },
    { "p31m",    157 },
    { "p3c1",    158 },
    { "p31c",    159 },
    { "r3m",     160 },
    { "r3c",     161 },
    { "p-31m",   162 },
    { "p-31c",   163 },
    { "p-3m1",   164 },
    { "p-3c1",   165 },
    { "r-3m",    166 },
    { "r-3c",    167 },

 // ═══════════════════════════════════════════════════════════
 // HEXAGONAL (168-194)
 // ═══════════════════════════════════════════════════════════
    { "p6",      168 },
    { "p61",     169 },
    { "p65",     170 },
    { "p62",     171 },
    { "p64",     172 },
    { "p63",     173 },
    { "p-6",     174 },
    { "p6/m",    175 },
    { "p63/m",   176 },
    { "p622",    177 },
    { "p6122",   178 },
    { "p6522",   179 },
    { "p6222",   180 },
    { "p6422",   181 },
    { "p6322",   182 },
    { "p6mm",    183 },
    { "p6cc",    184 },
    { "p63cm",   185 },
    { "p63mc",   186 },
    { "p-6m2",   187 },
    { "p-6c2",   188 },
    { "p-62m",   189 },
    { "p-62c",   190 },
    { "p6/mmm",  191 },
    { "p6/mcc",  192 },
    { "p63/mcm", 193 },
    { "p63/mmc", 194 },

 // ═══════════════════════════════════════════════════════════
 // CUBIC (195-230)
 // ═══════════════════════════════════════════════════════════
    { "p23",     195 },
    { "f23",     196 },
    { "i23",     197 },
    { "p213",    198 },
    { "i213",    199 },
    { "pm-3",    200 },
    { "pn-3",    201 },
    { "fm-3",    202 },
    { "fd-3",    203 },
    { "im-3",    204 },
    { "pa-3",    205 },
    { "ia-3",    206 },
    { "p432",    207 },
    { "p4232",   208 },
    { "f432",    209 },
    { "f4132",   210 },
    { "i432",    211 },
    { "p4332",   212 },
    { "p4132",   213 },
    { "i4132",   214 },
    { "p-43m",   215 },
    { "f-43m",   216 },
    { "i-43m",   217 },
    { "p-43n",   218 },
    { "f-43c",   219 },
    { "i-43d",   220 },
    { "pm-3m",   221 },
    { "pn-3n",   222 },
    { "pm-3n",   223 },
    { "pn-3m",   224 },
    { "fm-3m",   225 },
    { "fm-3c",   226 },
    { "fd-3m",   227 },
    { "fd-3c",   228 },
    { "im-3m",   229 },
    { "ia-3d",   230 },
        };
        #endregion

        #region Public Structures

 /// <summary>
 /// Represents a parsed symmetry transformation.
 /// Contains coefficients for x, y, z transformations and shifts.
 /// </summary>
        public struct ReadSymm
        {
            public int[] sign1;
            public int[] sign2;
            public int[] pos1;
            public int[] pos2;
            public float[] shift;
            public int orientation;
        }

 /// <summary>
 /// Represents a symmetry entry from the database.
 /// </summary>
        public struct SymmetryEntry
        {
            public int groupNumber;
            public int orientation;
            public string crystalSystem;
            public string groupName;
            public List<string> operations;
        }

        #endregion

        #region Public Methods

 /// <summary>
 /// Sets the current space group information.
 /// </summary>
 /// <param name="name">The space group name.</param>
 /// <param name="number">The International Tables number.</param>
        public static void SetSpaceGroupInfo(string name, int number)
        {
            _spaceGroupName = name ?? "";
            _spaceGroupNumber = number;
        }

 /// <summary>
 /// Clears transformation data (legacy method, kept for compatibility).
 /// </summary>
        public static void TransformationsClean()
        {
 // Static database is not cleared
        }

 /// <summary>
 /// Creates symmetry transformations for a given space group.
 /// </summary>
 /// <param name="group">The space group International Tables number.</param>
 /// <returns>A list of parsed symmetry operations.</returns>
        public static List<ReadSymm> CreateTransformations(int group)
        {
            var result = new List<ReadSymm>();

            if (group < 1) return result;

            int targetGroup = group;
            int targetOrientation = 0;

            if (!string.IsNullOrEmpty(_spaceGroupName))
            {
                string normalized = NormalizeSpaceGroupName(_spaceGroupName);
                targetOrientation = GetOrientationForGroup(normalized, targetGroup);
            }

            string[] selectedEntry = null;
            foreach (var entry in SymmetryOperations)
            {
                if (entry.Length > 3 && int.Parse(entry[0]) == targetGroup && int.Parse(entry[3]) == targetOrientation)
                {
                    selectedEntry = entry;
                    break;
                }
            }

            if (selectedEntry == null)
            {
                foreach (var entry in SymmetryOperations)
                {
                    if (entry.Length > 3 && int.Parse(entry[0]) == targetGroup && int.Parse(entry[3]) == 0)
                    {
                        selectedEntry = entry;
                        targetOrientation = 0;
                        break;
                    }
                }
            }

            if (selectedEntry == null) return result;

            int startIndex = 3;
            if (selectedEntry.Length > 4 && (selectedEntry[3] == "0" || selectedEntry[3] == "1" ||
                                              selectedEntry[3] == "2" || selectedEntry[3] == "3"))
            {
                startIndex = 4;
            }

            for (int i = startIndex; i < selectedEntry.Length; i++)
            {
                result.Add(RetriveTransformation(selectedEntry[i]));
            }

            return result;
        }

 /// <summary>
 /// Creates a single symmetry transformation from a CIF operation string.
 /// </summary>
 /// <param name="value">The symmetry operation string (e.g., "x,y,z").</param>
 /// <returns>A parsed ReadSymm structure.</returns>
        public static ReadSymm CreateTransformationFromFile(string value)
        {
            string modifiedString = value.Replace(" ", "").Replace("'", "").Replace("`", "").Replace("\"", "");
            return RetriveTransformation(modifiedString);
        }

 /// <summary>
 /// Parses a symmetry operation string into a ReadSymm structure.
 /// </summary>
 /// <param name="s">The operation string to parse.</param>
 /// <returns>A parsed ReadSymm structure.</returns>
 /// <summary>
 /// Parsuje string operacji symmetry do structure ReadSymm.
 /// Handles:
 /// - small i WIELKIE litery: "x,y,z", "-X,Y,-Z"
 /// - fractions: "1/2", "1/3", "2/3"
 /// - decimals: "0.5", "0.25"
 /// - forms with spaces: "x, y, z", "- x + 1/2, ..."
 /// - SHELX-style forms with dot: "0.5-X,0.5+Y,0.5-Z"
 /// </summary>
        public static ReadSymm RetriveTransformation(string s)
        {
            ReadSymm rs = new ReadSymm();
            rs.pos1 = new int[3];
            rs.sign1 = new int[3];
            rs.pos2 = new int[3];
            rs.sign2 = new int[3];
            rs.shift = new float[3];
            rs.orientation = 0;

 // Inicjalizacja default
            for (int i = 0; i < 3; i++)
            {
                rs.sign1[i] = 0;
                rs.sign2[i] = 0;
                rs.pos1[i] = -1;
                rs.pos2[i] = -1;
                rs.shift[i] = 0;
            }

            if (string.IsNullOrEmpty(s))
                return rs;

 // ⭐ Krok 1: NORMALIZACJA
 // - remove spaces, apostrophes, quotes, backticks, underscores
 // - konversion na lowercase (so that X,Y,Z = x,y,z)
            s = s.Replace(" ", "")
                 .Replace("\t", "")
                 .Replace("'", "")
                 .Replace("\"", "")
                 .Replace("`", "")
                 .Replace("_", "")
                 .ToLowerInvariant();

 // ⭐ Krok 2: rozbij after przecinkach na 3 osie
            string[] axes = s.Split(',');

            if (axes.Length < 3)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[RetriveTransformation] ⚠️ Only {axes.Length} axes in '{s}', expected 3");
 // If fewer than 3 axes — pad with empty ones
                var padded = new string[3];
                for (int i = 0; i < 3; i++)
                    padded[i] = i < axes.Length ? axes[i] : "";
                axes = padded;
            }

 // Step 3: parse each axis separately
            for (int axis = 0; axis < 3; axis++)
            {
                ParseAxisExpression(axes[axis].Trim(), axis, ref rs);
            }

            return rs;
        }

 /// <summary>
 /// Helper method — parses a single expression for one axis.
 /// Handles:
 /// "x" → afters1=0, sign1=+1
 /// "-x" → afters1=0, sign1=-1
 /// "y+1/2" → afters1=1, sign1=+1, shift=+0.5
 /// "-z+1/4" → afters1=2, sign1=-1, shift=+0.25
 /// "0.5-x" → afters1=0, sign1=-1, shift=+0.5 (SHELX)
 /// "x-y" → afters1=0, sign1=+1, afters2=1, sign2=-1
 /// "1/3+z" → afters1=2, sign1=+1, shift=+1/3
 /// </summary>
        private static void ParseAxisExpression(string expr, int axis, ref ReadSymm rs)
        {
            if (string.IsNullOrEmpty(expr))
            {
 // Nic — zostaw zera
                rs.sign1[axis] = 0;
                rs.sign2[axis] = 0;
                rs.pos1[axis] = 0;
                rs.pos2[axis] = 0;
                rs.shift[axis] = 0;
                return;
            }

 // Normalise: remove spacje
            expr = expr.Replace(" ", "").ToLowerInvariant();

 // ⭐ Krok A: find termy rozdzielone przez + i -
 // Note: pierwszy znak may be +/- or no
            var terms = new List<(char sign, string body)>();

            int i = 0;
            while (i < expr.Length)
            {
                char sign = '+';
                if (expr[i] == '+' || expr[i] == '-')
                {
                    sign = expr[i];
                    i++;
                }

                int start = i;
                while (i < expr.Length && expr[i] != '+' && expr[i] != '-')
                    i++;

                if (i > start)
                    terms.Add((sign, expr.Substring(start, i - start)));
            }

 // ⭐ Krok B: klasyfikuj termy — zmienna (x/y/z) czy shift (liczba)
 // ZMIENNA: "x", "y", "z"
 // SHIFT: "1/2", "0.5", "1/4", "1/3", "2/3", "0.25"

            foreach (var (sign, body) in terms)
            {
 // Check czy to zmienna (x/y/z)
                if (body == "x" || body == "y" || body == "z")
                {
                    int varIndex = body[0] - 'x';   // 'x' → 0, 'y' → 1, 'z' → 2
                    int signValue = (sign == '-') ? -1 : 1;

                    if (rs.pos1[axis] == -1)
                    {
                        rs.pos1[axis] = varIndex;
                        rs.sign1[axis] = signValue;
                    }
                    else if (rs.pos2[axis] == -1)
                    {
                        rs.pos2[axis] = varIndex;
                        rs.sign2[axis] = signValue;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[RetriveTransformation] ⚠️ Too many variables in axis {axis}: '{expr}'");
                    }
                }
                else
                {
 // This is a shift — parse number or fraction
                    float shiftValue = ParseNumber(body);
                    if (sign == '-')
                        shiftValue = -shiftValue;

                    rs.shift[axis] += shiftValue;
                }
            }

 // Step C: if no variable found in this axis — set zero
            if (rs.pos1[axis] == -1)
            {
                rs.pos1[axis] = 0;
                rs.sign1[axis] = 0;
            }
            if (rs.pos2[axis] == -1)
            {
                rs.pos2[axis] = 0;
                rs.sign2[axis] = 0;
            }
        }

 /// <summary>
 /// Parses a fraction or decimal number.
 /// "1/2" → 0.5
 /// "1/3" → 0.333...
 /// "2/3" → 0.666...
 /// "0.5" → 0.5
 /// "0.25" → 0.25
 /// "1" → 1.0
 /// "0" → 0.0
 /// </summary>
        private static float ParseNumber(string body)
        {
            if (string.IsNullOrEmpty(body))
                return 0;

 // Fraction: "1/2", "1/3", "2/3"
            if (body.Contains('/'))
            {
                var parts = body.Split('/');
                if (parts.Length == 2 &&
                    float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float num) &&
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float den) &&
                    Math.Abs(den) > 1e-9f)
                {
                    return num / den;
                }
            }

 // Decimal number: "0.5", "0.25"
            if (float.TryParse(body, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                return value;

            return 0;
        }

 /// <summary>
 /// Returns numer orientacji for danej nazwy grupy.
 /// Orientacja 0 = standard (wg International Tables).
 /// Orientation >0 = non-standard (different axis).
 /// </summary>
        public static int GetOrientationForGroup(string name, int groupNumber)
        {
            if (string.IsNullOrEmpty(name)) return 0;

 // Normalise na wszelki wypadek
            string n = NormalizeSpaceGroupName(name);

            switch (groupNumber)
            {
 // ═══ Grupa 13 — P2/c ═══
                case 13:
                    return n switch
                    {
                        "p2/c" => 0,
                        "p2/n" => 1,
                        "p2/a" => 2,
                        _ => 0
                    };

 // ═══ Grupa 14 — P21/c ═══
                case 14:
                    return n switch
                    {
                        "p21/c" => 0,
                        "p21/n" => 1,
                        "p21/a" => 2,
                        "p21/b" => 3,
                        _ => 0
                    };

 // ═══ Grupa 15 — C2/c ═══
                case 15:
                    return n switch
                    {
                        "c2/c" => 0,
                        "c2/n" => 1,
                        _ => 0
                    };

 // ═══ Grupa 62 — Pnma ═══
                case 62:
                    return n switch
                    {
                        "pnma" => 0,
                        "pnam" => 1,
                        "pmcn" => 2,
                        "pbnm" => 3,
                        "pmnb" => 4,
                        _ => 0
                    };

 // ═══ Grupa 63 — Cmcm ═══
                case 63:
                    return n switch
                    {
                        "cmcm" => 0,
                        "cmca" => 1,   // ← orientation Cmcm jako Cmca
                        _ => 0
                    };

 // ═══ Grupa 64 — Cmca ═══
                case 64:
                    return n switch
                    {
                        "cmca" => 0,
                        "cmcm" => 1,   // ← orientation Cmca jako Cmcm
                        _ => 0
                    };

                default:
                    return 0;
            }
        }



 /// <summary>
 /// Retrieves the crystal system name for a symmetry operation index.
 /// </summary>
 /// <param name="index">The index in the SymmetryOperations list.</param>
 /// <returns>The crystal system name.</returns>
        public static string RetriveSystem(int index)
        {
            if (index >= 0 && index < SymmetryOperations.Count)
            {
                string[] value = SymmetryOperations[index];
                return value.Length > 1 ? value[1] : "Unknown";
            }
            return "Unknown";
        }

        #endregion

 /// <summary>
 /// Normalisee name space group do kanonicznej afterstaci lowercase without spacji.
 ///
 /// Examples:
 /// "P 21/c" -> "p21/c"
 /// "P 1 21/c 1" -> "p21/c"
 /// "P 1 1 21/c 1" -> "p21/c"
 /// "P21/n" -> "p21/n"
 /// "P 21/n" -> "p21/n"
 /// "P n m a" -> "pnma"
 /// "P-1" -> "p-1"
 /// "P 1" -> "p1"
 /// "C 2/c" -> "c2/c"
 /// "P 1 2/c 1" -> "p2/c"
 /// "R -3 c" -> "r-3c"
 /// "F d -3 m" -> "fd-3m"
 /// </summary>
 /// <summary>
 /// Normalisee name space group do kanonicznej afterstaci lowercase without spacji.
 ///
 /// Examples:
 /// "P 21/c" -> "p21/c"
 /// "P 1 21/c 1" -> "p21/c"
 /// "P 1 1 21/c 1" -> "p21/c"
 /// "P21/n" -> "p21/n"
 /// "P 21/n" -> "p21/n"
 /// "P2(1)/n" -> "p21/n"
 /// "P 2(1)/c" -> "p2/c"
 /// "P n m a" -> "pnma"
 /// "P-1" -> "p-1"
 /// "P 1" -> "p1"
 /// "C 2/c" -> "c2/c"
 /// "P 1 2/c 1" -> "p2/c"
 /// "R -3 c" -> "r-3c"
 /// "F d -3 m" -> "fd-3m"
 /// </summary>
        public static string NormalizeSpaceGroupName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

 // ─── Step 1: preliminary cleanup ───────────────────────────
 // Remove apostrophes, quotes, backticks, underscores, PARENTHESES
            string s = name
                .Replace("'", "")
                .Replace("\"", "")
                .Replace("`", "")
                .Replace("_", " ")
                .Replace("(", "")     // ⭐ KLUCZOWE — usuwa nawias z P2(1)/n
                .Replace(")", "")     // ⭐ KLUCZOWE
                .Trim();

 // ─── Step 2: remove ALL whitespace ────────────────────
 // "P 1 21/c 1" -> "P121/c1"
 // "P n m a" -> "Pnma"
 // "P2(1)/n" -> "P21/n" (after parenthesis removal above)
            s = Regex.Replace(s, @"\s+", "");

            if (s.Length == 0)
                return "";

 // ─── Step 3: handle special cases with hyphen ───────
 // "P-1", "P-3", "Fd-3m" — leave the hyphen alone

 // ─── Step 4: remove leading "1" (c/a axis marker) ─────────
 // Standard H-M: "P 1 21/c 1" — leading "1" means "along axis"
 // BUT: "P1" (group 1) must stay "p1"
 // "P-1" must stay "p-1"
            if (s.Length >= 3 && s[0] != '-')
            {
                char first = char.ToUpperInvariant(s[0]);
                if (first == 'P' || first == 'C' || first == 'I' ||
                    first == 'F' || first == 'R' || first == 'A' || first == 'B')
                {
                    if (s[1] == '1' && s.Length > 2)
                    {
 // "P121/c1" -> "P21/c1"
                        s = s[0] + s.Substring(2);
                    }
                }
            }

 // ─── Step 5: remove trailing "1" ──────────────────────────────
 // "P21/c1" -> "P21/c"
 // BUT do NOT touch: "P-1" (hyphen before "1")
 // "P1" (grupa 1)
            if (s.Length >= 2 && s.EndsWith("1"))
            {
                bool precededByMinus = s.Length >= 2 && s[s.Length - 2] == '-';

                bool isJustP1 = s.Length == 2 &&
                                (char.ToUpperInvariant(s[0]) == 'P' ||
                                 char.ToUpperInvariant(s[0]) == 'C' ||
                                 char.ToUpperInvariant(s[0]) == 'I' ||
                                 char.ToUpperInvariant(s[0]) == 'F' ||
                                 char.ToUpperInvariant(s[0]) == 'R' ||
                                 char.ToUpperInvariant(s[0]) == 'A' ||
                                 char.ToUpperInvariant(s[0]) == 'B');

                if (!precededByMinus && !isJustP1)
                {
                    s = s.Substring(0, s.Length - 1);
                }
            }

 // ─── Step 6: repeat leading "1" removal ────────────────
 // "P 1 1 21/c 1" -> "P1121/c1" -> "P121/c1" -> "P21/c1"
            if (s.Length >= 3)
            {
                char first = char.ToUpperInvariant(s[0]);
                if (first == 'P' || first == 'C' || first == 'I' ||
                    first == 'F' || first == 'R' || first == 'A' || first == 'B')
                {
                    while (s.Length >= 3 && s[1] == '1' && char.IsDigit(s[2]))
                    {
                        s = s[0] + s.Substring(2);
                    }
                }
            }

 // ─── Krok 7: konversion na lowercase ────────────────────────
            s = s.ToLowerInvariant();

            if (string.IsNullOrEmpty(s))
                return "";

            System.Diagnostics.Debug.WriteLine(
                $"[Symmetry] NormalizeSpaceGroupName: '{name}' -> '{s}'");

            return s;
        }

 /// <summary>
 /// Creates list transformation symmetry for danej grupy.
 /// 
 /// PRIORYTET:
 /// 1. explicitOperations (np. z CIF-a) — najpewniejsze
 /// 2. tabela + orientation z nazwy grupy (P21/c → orient 0, P21/n → orient 1)
 /// 3. tabela + orientation 0 (standard)
 /// </summary>
 /// <param name="group">Numer grupy w International Tables (1-230).</param>
 /// <param name="groupName">Group name (e.g. "p21/c", "P 21/n") — optional. Used to choose orientation.</param>
 /// <param name="explicitOperations">Explicit operations from file (e.g. from CIF "x,y,z") — optional. Highest priority.</param>
        public static List<ReadSymm> CreateTransformations(
            int group,
            string groupName,
            List<string> explicitOperations)
        {
 // ═══════════════════════════════════════════════════════════
 // PRIORYTET 1: jawne operacje z pliku
 // ═══════════════════════════════════════════════════════════
            if (explicitOperations != null && explicitOperations.Count > 0)
            {
                var fromFile = new List<ReadSymm>();
                foreach (var op in explicitOperations)
                {
                    if (string.IsNullOrWhiteSpace(op)) continue;
                    fromFile.Add(CreateTransformationFromFile(op));
                }

                if (fromFile.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[Symmetry] CreateTransformations: using {fromFile.Count} explicit operations");
                    return fromFile;
                }
            }

 // ═══════════════════════════════════════════════════════════
 // PRIORYTET 2: tabela + orientation z nazwy
 // ═══════════════════════════════════════════════════════════
            var result = new List<ReadSymm>();
            if (group < 1) return result;

            int targetGroup = group;
            int targetOrientation = 0;

 // Choose orientation from group name (if given)
            string effectiveName = groupName ?? _spaceGroupName;
            if (!string.IsNullOrEmpty(effectiveName))
            {
                string normalized = NormalizeSpaceGroupName(effectiveName);
                targetOrientation = GetOrientationForGroup(normalized, targetGroup);

                System.Diagnostics.Debug.WriteLine(
                    $"[Symmetry] CreateTransformations: group={targetGroup}, " +
                    $"name='{effectiveName}' → normalized='{normalized}', orientation={targetOrientation}");
            }

 // Find wpis w tabeli
            string[] selectedEntry = null;
            foreach (var entry in SymmetryOperations)
            {
                if (entry.Length > 3 &&
                    int.TryParse(entry[0], out int entryGroup) && entryGroup == targetGroup &&
                    int.TryParse(entry[3], out int entryOrient) && entryOrient == targetOrientation)
                {
                    selectedEntry = entry;
                    break;
                }
            }

 // Fallback na orientation=0 if nie znaleziono
            if (selectedEntry == null && targetOrientation != 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Symmetry] ⚠️ No entry for group={targetGroup} orient={targetOrientation}, " +
                    $"falling back to orient=0");

                foreach (var entry in SymmetryOperations)
                {
                    if (entry.Length > 3 &&
                        int.TryParse(entry[0], out int entryGroup) && entryGroup == targetGroup &&
                        int.TryParse(entry[3], out int entryOrient) && entryOrient == 0)
                    {
                        selectedEntry = entry;
                        targetOrientation = 0;
                        break;
                    }
                }
            }

            if (selectedEntry == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[Symmetry] ❌ No entry in table for group={targetGroup}");
                return result;
            }

 // Parsuj operacje
            int startIndex = 4;   // entry[0]=numer, entry[1]=system, entry[2]=nazwa, entry[3]=orient
            for (int i = startIndex; i < selectedEntry.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(selectedEntry[i])) continue;
                result.Add(RetriveTransformation(selectedEntry[i]));
            }

            System.Diagnostics.Debug.WriteLine(
                $"[Symmetry] ✅ CreateTransformations: group={targetGroup}, " +
                $"orientation={targetOrientation}, ops={result.Count}");

            return result;
        }

        #region Space Group Detection — Set Matching

 /// <summary>
 /// Recognizes the space group by comparing the SET of operations from the file
 /// ze zbiorem operacji each grupy w tabeli.
 ///
 /// Algorytm:
 /// 1. Wyznacz system krystalograficzny z cell.
 /// 2. Sparsuj operacje z pliku do structure ReadSymm.
 /// 3. For each group in the table with matching crystal system:
 /// a. Sparsuj operacje tabeli.
 /// b. Check czy liczba operacji zgadza (szybki filtr).
 /// c. Check whether EVERY file operation has a counterpart in the table
 /// (comparison via test point, mod 1).
 /// d. Check whether EVERY table operation has a counterpart in the file.
 /// e. If both conditions are met → this is the group.
 ///
 /// Comparing operations via a test point solves problems with:
 /// - translationmi +1/2 vs -1/2 (ta sama operacja krystalograficzna)
 /// - order of components (x-y vs -y+x)
 /// - different writing of the same operation
 /// </summary>
        public static int DetectSpaceGroupByOperations(
            float cellA, float cellB, float cellC,
            float alpha, float beta, float gamma,
            List<string> fileOperations)
        {
            if (fileOperations == null || fileOperations.Count == 0)
                return 0;

 // ═══════════════════════════════════════════════════════════
 // 1. Crystal system
 // ═══════════════════════════════════════════════════════════
            CrystalSystem system = DetermineCrystalSystem(
                cellA, cellB, cellC, alpha, beta, gamma);

            Debug.WriteLine($"[DetectSG] system={system}, " +
                            $"cell=({cellA:F3},{cellB:F3},{cellC:F3}," +
                            $"{alpha:F2},{beta:F2},{gamma:F2})");

 // ═══════════════════════════════════════════════════════════
 // 2. Sparsuj operacje z pliku
 // ═══════════════════════════════════════════════════════════
            var fileOps = new List<ReadSymm>();
            foreach (var s in fileOperations)
            {
                if (string.IsNullOrWhiteSpace(s)) continue;
                var op = CreateTransformationFromFile(s);
                fileOps.Add(op);
                Debug.WriteLine($"[DetectSG] fileOp: '{s}'");
            }

            if (fileOps.Count == 0)
                return 0;

            int fileOpCount = fileOps.Count;

 // ═══════════════════════════════════════════════════════════
 // 3. Grupuj wpisy tabeli after numerze grupy
 // ═══════════════════════════════════════════════════════════
            var groupEntries = new Dictionary<int, List<string[]>>();
            foreach (var entry in SymmetryOperations)
            {
                if (entry.Length < 5) continue;
                if (!int.TryParse(entry[0], out int g)) continue;
                if (g < 1) continue;

                if (!groupEntries.ContainsKey(g))
                    groupEntries[g] = new List<string[]>();
                groupEntries[g].Add(entry);
            }

            Debug.WriteLine($"[DetectSG] Table has {groupEntries.Count} unique groups");

 // ═══════════════════════════════════════════════════════════
 // 4. Iteruj grupy w order numerycznej
 // ═══════════════════════════════════════════════════════════
            foreach (var kvp in groupEntries.OrderBy(k => k.Key))
            {
                int groupNum = kvp.Key;

 // Filtr systemu — bierzemy pierwszy wpis grupy
                string systemName = kvp.Value[0][1];
                if (!SystemMatches(systemName, system))
                    continue;

 // For each orientacji
                foreach (var entry in kvp.Value)
                {
 // Afterlicz operacje w tabeli
                    var tableOpStrings = new List<string>();
                    for (int i = 4; i < entry.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(entry[i])) continue;
                        tableOpStrings.Add(entry[i]);
                    }

 // Quick filter — different number of operations
                    if (tableOpStrings.Count != fileOpCount)
                        continue;

 // Sparsuj operacje tabeli
                    var tableOps = new List<ReadSymm>();
                    foreach (var s in tableOpStrings)
                        tableOps.Add(RetriveTransformation(s));

 // ═══════════════════════════════════════════════════════
 // 5. Check whether the SETS of operations are identical
 // ═══════════════════════════════════════════════════════
                    bool allFileOpsInTable = AllOpsMatch(fileOps, tableOps);
                    bool allTableOpsInFile = AllOpsMatch(tableOps, fileOps);

                    if (allFileOpsInTable && allTableOpsInFile)
                    {
                        Debug.WriteLine($"[DetectSG] ✅ MATCH: group={groupNum}, " +
                                        $"orient={entry[3]} ({entry[2]})");
                        return groupNum;
                    }
                }
            }

            Debug.WriteLine($"[DetectSG] ❌ No matching group found");
            return 0;
        }

 /// <summary>
 /// Checks whether EVERY operation from list A has a counterpart (crystallographically
 /// equivalent) in list B.
 /// </summary>
        private static bool AllOpsMatch(List<ReadSymm> a, List<ReadSymm> b)
        {
            foreach (var opA in a)
            {
                bool found = false;
                foreach (var opB in b)
                {
                    if (OperationsEquivalent(opA, opB))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }
            return true;
        }

 /// <summary>
 /// Checks whether two symmetry operations give the same effect on the test point
 /// (mod 1). Odafterrne na:
 /// - translacje ±1/2, ±1/3, ±1/4 (mod 1)
 /// - different algebraic writing (x-y vs -y+x)
 /// - order of components in the expression
 /// </summary>
        private static bool OperationsEquivalent(ReadSymm opA, ReadSymm opB)
        {
            const float TOL = 1e-3f;

 // Use 2 different test points for certainty
            float[][] testPoints = new[]
            {
        new float[] { 0.13f, 0.27f, 0.41f },
        new float[] { 0.31f, 0.07f, 0.83f },
    };

            foreach (var p in testPoints)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    float va = ApplyAxis(opA, axis, p);
                    float vb = ApplyAxis(opB, axis, p);

 // Mod-1 reduction: -0.5 and +0.5 are equivalent
                    float diff = va - vb;
                    diff = diff - (float)Math.Round(diff);   // do [-0.5, 0.5]

                    if (Math.Abs(diff) > TOL)
                        return false;
                }
            }

            return true;
        }

 /// <summary>
 /// Applies a ReadSymm transformation axis to a point.
 /// </summary>
        private static float ApplyAxis(ReadSymm op, int axis, float[] point)
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

        #region Fingerprint Helpers

 /// <summary>
 /// Crystal systems (for filtering the table).
 /// </summary>
        public enum CrystalSystem
        {
            Unknown,
            Triclinic,
            Monoclinic,
            Orthorhombic,
            Tetragonal,
            Trigonal,
            Hexagonal,
            Cubic
        }

 /// <summary>
 /// Determines the crystal system from unit-cell parameters.
 /// Tolerances for length: 0.5 Å, for angles: 1°.
 /// </summary>
        public static CrystalSystem DetermineCrystalSystem(
            float a, float b, float c,
            float alpha, float beta, float gamma)
        {
            const float LEN_TOL = 0.5f;
            const float ANG_TOL = 1.0f;

            bool aEqB = Math.Abs(a - b) < LEN_TOL;
            bool bEqC = Math.Abs(b - c) < LEN_TOL;
            bool aEqC = Math.Abs(a - c) < LEN_TOL;

            bool ang90 = Math.Abs(alpha - 90f) < ANG_TOL &&
                         Math.Abs(beta - 90f) < ANG_TOL &&
                         Math.Abs(gamma - 90f) < ANG_TOL;

            bool ang120 = Math.Abs(alpha - 90f) < ANG_TOL &&
                          Math.Abs(beta - 90f) < ANG_TOL &&
                          Math.Abs(gamma - 120f) < ANG_TOL;

 // ─── Cubic ───
            if (aEqB && bEqC && ang90)
                return CrystalSystem.Cubic;

 // ─── Hexagonal ───
            if (aEqB && ang120)
                return CrystalSystem.Hexagonal;

 // ─── Trigonal (romboedryczny or heksagonalny) ───
 // Romboedryczny: a=b=c, α=β=γ ≠ 90
            if (aEqB && bEqC &&
                Math.Abs(alpha - beta) < ANG_TOL &&
                Math.Abs(beta - gamma) < ANG_TOL &&
                Math.Abs(alpha - 90f) > ANG_TOL)
            {
                return CrystalSystem.Trigonal;
            }
 // Trigonal-hexagonal: a=b, γ=120, α=β=90
            if (aEqB && ang120)
                return CrystalSystem.Trigonal;

 // ─── Tetragonal ───
            if (aEqB && !bEqC && ang90)
                return CrystalSystem.Tetragonal;

 // ─── Orthorhombic ───
            if (ang90 && !aEqB && !bEqC && !aEqC)
                return CrystalSystem.Orthorhombic;

 // ─── Monoclinic ───
 // One angle ≠ 90, the other two = 90
            int not90Count = 0;
            if (Math.Abs(alpha - 90f) > ANG_TOL) not90Count++;
            if (Math.Abs(beta - 90f) > ANG_TOL) not90Count++;
            if (Math.Abs(gamma - 90f) > ANG_TOL) not90Count++;

            if (not90Count == 1)
                return CrystalSystem.Monoclinic;

 // ─── Triclinic ───
            if (not90Count >= 2)
                return CrystalSystem.Triclinic;

 // Fallback
            return CrystalSystem.Triclinic;
        }

 /// <summary>
 /// Does the system from the table match the detected system from the cell?
 /// "Trigonal" in the table may mean either rhombohedral or hexagonal.
 /// </summary>
        private static bool SystemMatches(string tableSystemName, CrystalSystem detected)
        {
            if (string.IsNullOrEmpty(tableSystemName)) return false;

            string t = tableSystemName.ToLowerInvariant();

            switch (detected)
            {
                case CrystalSystem.Triclinic:
                    return t.Contains("triclinic");
                case CrystalSystem.Monoclinic:
                    return t.Contains("monoclinic");
                case CrystalSystem.Orthorhombic:
                    return t.Contains("orthorhombic");
                case CrystalSystem.Tetragonal:
                    return t.Contains("tetragonal");
                case CrystalSystem.Trigonal:
                    return t.Contains("trigonal") || t.Contains("hexagonal");
                case CrystalSystem.Hexagonal:
                    return t.Contains("hexagonal") || t.Contains("trigonal");
                case CrystalSystem.Cubic:
                    return t.Contains("cubic");
                default:
                    return true;   // Unknown → nie filtruj
            }
        }

 /// <summary>
 /// Test points used to build the fingerprint. Chosen so that they give
 /// different results for different symmetry operations and different groups.
 /// </summary>
        private static readonly float[][] TestPoints = new float[][]
        {
    new float[] { 0.13f, 0.27f, 0.41f },
    new float[] { 0.31f, 0.07f, 0.83f },
    new float[] { 0.57f, 0.63f, 0.19f },
    new float[] { 0.71f, 0.89f, 0.23f },
    new float[] { 0.42f, 0.11f, 0.55f },
        };

 /// <summary>
 /// Builds a fingerprint — set of points obtained by transformation
 /// points testowych przez list operacji symmetry.
 /// Each point is reduced to [0, 1)³ (mod 1), rounded to 4 places.
 /// </summary>
        private static List<(float x, float y, float z)> BuildFingerprint(List<ReadSymm> ops)
        {
            var points = new List<(float, float, float)>();

            foreach (var op in ops)
            {
                foreach (var tp in TestPoints)
                {
                    float x = ApplyAxis(op, 0, tp);
                    float y = ApplyAxis(op, 1, tp);
                    float z = ApplyAxis(op, 2, tp);

 // Mod 1 → [0, 1)
                    x = x - (float)Math.Floor(x);
                    y = y - (float)Math.Floor(y);
                    z = z - (float)Math.Floor(z);

 // Redukcja szumu (for 1/3, 2/3 → 0.3333)
                    x = (float)Math.Round(x, 4);
                    y = (float)Math.Round(y, 4);
                    z = (float)Math.Round(z, 4);

                    points.Add((x, y, z));
                }
            }

 // Sort for deterministic comparison
            points.Sort((p1, p2) =>
            {
                int c = p1.Item1.CompareTo(p2.Item1);
                if (c != 0) return c;
                c = p1.Item2.CompareTo(p2.Item2);
                if (c != 0) return c;
                return p1.Item3.CompareTo(p2.Item3);
            });

            return points;
        }


 /// <summary>
 /// Are two fingerprints identical (within tolerance)?
 /// Both must have the same number of points and each point must have
 /// odafterwiednik w drugim (mod 1, tolerancja).
 /// </summary>
        private static bool FingerprintsMatch(
            List<(float x, float y, float z)> fp1,
            List<(float x, float y, float z)> fp2,
            float tolerance)
        {
            if (fp1.Count != fp2.Count) return false;

 // Both are sorted — compare 1:1
            for (int i = 0; i < fp1.Count; i++)
            {
                if (!PointsEqual(fp1[i], fp2[i], tolerance))
                    return false;
            }

            return true;
        }

 /// <summary>
 /// How many points from fp1 have a counterpart in fp2? (for partial match score)
 /// </summary>
        private static int CountMatchingPoints(
            List<(float x, float y, float z)> fp1,
            List<(float x, float y, float z)> fp2,
            float tolerance)
        {
            int count = 0;

 // Simple O(n²) approach — n is small (~20-40)
            foreach (var p1 in fp1)
            {
                foreach (var p2 in fp2)
                {
                    if (PointsEqual(p1, p2, tolerance))
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

 /// <summary>
 /// Point comparison modulo 1 (handles +1/2 vs -1/2).
 /// </summary>
        private static bool PointsEqual(
            (float x, float y, float z) p1,
            (float x, float y, float z) p2,
            float tolerance)
        {
            return AxisEqual(p1.x, p2.x, tolerance) &&
                   AxisEqual(p1.y, p2.y, tolerance) &&
                   AxisEqual(p1.z, p2.z, tolerance);
        }

        private static bool AxisEqual(float a, float b, float tolerance)
        {
            float d = a - b;
 // Redukcja do [-0.5, 0.5] — mod 1
            d = d - (float)Math.Round(d);
            return Math.Abs(d) < tolerance;
        }

        #endregion
    }
}