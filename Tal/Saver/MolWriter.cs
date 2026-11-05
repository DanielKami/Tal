// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure / image exafterrt: MolWriter.

﻿using System.Globalization;
using System.Text;

namespace Tal
{
    public static class MolWriter
    {
        public static bool Write(StructureData s, string path)
        {
            var sb = new StringBuilder();

 // Header
            sb.AppendLine(Truncate(s.Name, 80));
            sb.AppendLine("  TAL    3D");
            sb.AppendLine();

 // Counts line
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "{0,3}{1,3}  0  0  0  0  0  0  0  0999 V2000",
                s.Atoms.Count, 0)); // bonds = 0 for now (can be extended)

 // Atoms
            foreach (var a in s.Atoms)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0,10:F4}{1,10:F4}{2,10:F4} {3,-3} 0  0  0  0  0  0  0  0  0  0  0  0",
                    a.X, a.Y, a.Z, a.Symbol));
            }

            sb.AppendLine("M  END");
            File.WriteAllText(path, sb.ToString(), Encoding.ASCII);
            return true;
        }

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max));
    }
}