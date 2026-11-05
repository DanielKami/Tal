// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure / image exafterrt: SymmetryHelper.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Helpers to obtain a complete, reliable list of symmetry operation strings
// for writing RES / CIF / MOL2 etc.
//
// ⭐ Jedno source prawdy: SymmetryElements (tabela + NormalizeSpaceGroupName).

namespace Tal.Saver
{
    public static class SymmetryHelper
    {
 /// <summary>
 /// Returns a clean list of symmetry operation strings ("x,y,z", "-x,y+1/2,-z", …).
 /// Priority:
 /// 1. explicit ops already stored in StructureData
 /// 2. SymmetryElements database by IT number + name
 /// 3. fallback → identity only
 /// </summary>
        public static List<string> GetOperationStrings(StructureData s)
        {
 // 1) Already present in the structure
            if (s.SymmetryOperations != null && s.SymmetryOperations.Count > 0)
            {
                var cleaned = s.SymmetryOperations
                    .Where(op => !string.IsNullOrWhiteSpace(op))
                    .Select(op => op.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (cleaned.Count > 0)
                    return cleaned;
            }

 // 2) From the big database
            int group = s.SpaceGroup;
            string name = s.SpaceGroupName ?? "";

            if (group < 1 && !string.IsNullOrEmpty(name))
            {
                string normalized = SymmetryElements.NormalizeSpaceGroupName(name);
                if (SymmetryElements.GroupNameToNumber.TryGetValue(normalized, out int num))
                    group = num;
            }

            if (group >= 1 && group <= 230)
            {
                var strings = GetOperationStringsFromTable(group, name);
                if (strings.Count > 0)
                    return strings;
            }

 // 3) Fallback
            return new List<string> { "x,y,z" };
        }

 /// <summary>
 /// Reads the raw operation strings straight from SymmetryElements.SymmetryOperations.
 /// ⭐ Szuka by NORMALISED NAZWIE (nie after orientation).
 /// </summary>
        public static List<string> GetOperationStringsFromTable(int groupNumber, string groupName)
        {
            var result = new List<string>();
            if (groupNumber < 1) return result;

            string targetNorm = SymmetryElements.NormalizeSpaceGroupName(groupName ?? "");

 // Exact match by normalized name
            if (!string.IsNullOrEmpty(targetNorm))
            {
                foreach (var entry in SymmetryElements.SymmetryOperations)
                {
                    if (entry.Length < 5) continue;
                    if (!int.TryParse(entry[0], out int g) || g != groupNumber) continue;

                    string entryNorm = SymmetryElements.NormalizeSpaceGroupName(entry[2] ?? "");
                    if (!string.Equals(entryNorm, targetNorm, StringComparison.OrdinalIgnoreCase))
                        continue;

                    for (int i = 4; i < entry.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(entry[i])) continue;
                        result.Add(entry[i].Trim());
                    }
                    return result;
                }
            }

 // Fallback — pierwszy wpis grupy z orientation = 0
            foreach (var entry in SymmetryElements.SymmetryOperations)
            {
                if (entry.Length < 5) continue;
                if (!int.TryParse(entry[0], out int g) || g != groupNumber) continue;
                if (!int.TryParse(entry[3], out int o) || o != 0) continue;

                for (int i = 4; i < entry.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(entry[i])) continue;
                    result.Add(entry[i].Trim());
                }
                return result;
            }

            return result;
        }

 /// <summary>
 /// True when the list of operations contains an inversion centre
 /// (i.e. the operation "-x,-y,-z" or an equivalent).
 /// Used to decide LATT sign in SHELX.
 /// </summary>
        public static bool ContainsInversion(IList<string> ops)
        {
            foreach (var op in ops)
            {
                string n = NormalizeOp(op);
                if (n == "-x,-y,-z" || n == "-x -y -z")
                    return true;
            }
            return false;
        }

        private static string NormalizeOp(string op)
        {
            return op.Replace(" ", "").ToLowerInvariant();
        }

 /// <summary>
 /// Returns symmetry operations for the exact setting name (e.g. "P21/n").
 /// Szuka after nazwie — nie afterlega na orientation.
 /// </summary>
        public static List<string> GetOperationStringsByName(int group, string settingName)
        {
            var result = new List<string>();
            if (group < 1 || string.IsNullOrWhiteSpace(settingName)) return result;

            string target = SymmetryElements.NormalizeSpaceGroupName(settingName);

            foreach (var entry in SymmetryElements.SymmetryOperations)
            {
                if (entry.Length < 4) continue;
                if (!int.TryParse(entry[0], out int g) || g != group) continue;

                string entryName = SymmetryElements.NormalizeSpaceGroupName(entry[2] ?? "");
                if (!string.Equals(entryName, target, StringComparison.OrdinalIgnoreCase))
                    continue;

                for (int i = 4; i < entry.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(entry[i])) continue;
                    result.Add(entry[i].Trim());
                }
                return result;
            }

            return result;
        }
    }
}