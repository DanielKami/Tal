// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure / image export: CrystalSpaceGroupValidator.

using Tal.Saver;

namespace Tal.Saver
{
    public static class CrystalSpaceGroupValidator
    {
 /// <summary>
 /// Checks czy space group structure jest w standardowej orientacji.
 /// </summary>
 /// <returns>
 /// null if OK, albo komunikat warning if grupa jest niestandardowa.
 /// </returns>
        public static string Validate(StructureData structure)
        {
            if (structure == null) return null;

            string name = structure.SpaceGroupName ?? "";
            int group = structure.SpaceGroup;

            if (string.IsNullOrEmpty(name) && group <= 0)
                return null;   // no info — we report nothing

            string normalized = SymmetryElements.NormalizeSpaceGroupName(name);

            if (group < 1 && !string.IsNullOrEmpty(normalized))
            {
                if (SymmetryElements.GroupNameToNumber.TryGetValue(normalized, out int num))
                    group = num;
            }

            if (group < 1) return null;   // nie znamy grupy

 // ⭐ Standard = orientation 0 → pierwszy wpis grupy w tabeli
            string standardName = SpaceGroupTransformer.GetStandardName(group);
            string standardNorm = SymmetryElements.NormalizeSpaceGroupName(standardName);

            if (normalized == standardNorm)
                return null;   // standard — OK

            string msg =
                $"⚠️ Non-standard space-group setting detected.\n\n" +
                $"Structure:  {name}  (group {group})\n" +
                $"Standard:   {standardName}\n\n" +
                $"CRYSTAL17 requires the standard setting of the space group.\n" +
                $"Non-standard orientations (e.g. P 2₁/n instead of P 2₁/c) are\n" +
                $"rejected or misinterpreted by CRYSTAL.\n\n" +
                $"Please transform the structure to the standard setting\n" +
                $"(e.g. using PLATON 'ADDSYM' / 'NEWSYM' or a similar tool)\n" +
                $"before running CRYSTAL17.";

            return msg;
        }
    }
}