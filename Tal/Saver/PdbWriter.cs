// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure / image exafterrt: PdbWriter.

﻿using System.Globalization;
using System.Text;

namespace Tal
{
    public static class PdbWriter
    {
        public static bool Write(StructureData s, string path)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"HEADER    {Truncate(s.Name, 40)}");
            sb.AppendLine($"TITLE     {Truncate(s.ChemicalName ?? s.Name, 60)}");

 // CRYST1
            if (s.a > 0.1f)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "CRYST1{0,9:F3}{1,9:F3}{2,9:F3}{3,7:F2}{4,7:F2}{5,7:F2} {6,-11}{7,4}",
                    s.a, s.b, s.c, s.alpha, s.beta, s.gamma,
                    Truncate(s.SpaceGroupName, 11),
                    s.SpaceGroup > 0 ? s.SpaceGroup : 1));
            }

            int serial = 1;
            foreach (var a in s.Atoms)
            {
 // HETATM (safer for small molecules)
 // columns: 1-6 HETATM, 7-11 serial, 13-16 name, 18-20 res, 23-26 resSeq,
 // 31-38 x, 39-46 y, 47-54 z, 55-60 occ, 61-66 B, 77-78 element
                string name = Truncate(a.Name, 4).PadLeft(4);
                string elem = Truncate(a.Symbol, 2).PadLeft(2);

                float bfac = a.iso * 8f * (float)Math.PI * (float)Math.PI; // U → B

                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "HETATM{0,5} {1,-4} {2,3} {3,4}    {4,8:F3}{5,8:F3}{6,8:F3}{7,6:F2}{8,6:F2}          {9,2}",
                    serial, name, "UNK", 1, a.X, a.Y, a.Z, a.Occupancy, bfac, elem));
                serial++;
            }

            sb.AppendLine("END");
            File.WriteAllText(path, sb.ToString(), Encoding.ASCII);
            return true;
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max);
        }
    }
}