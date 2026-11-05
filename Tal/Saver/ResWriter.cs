// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure / image exafterrt: ResWriter.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Globalization;
using System.Text;
using Tal.Saver;

namespace Tal
{
    public static class ResWriter
    {
        public static bool Write(StructureData s, string path)
        {
            if (s == null || s.Atoms == null || s.Atoms.Count == 0)
                return false;

            var sb = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;

 // ── TITL ──────────────────────────────────────────────
            string title = string.IsNullOrWhiteSpace(s.Name) ? "TAL_structure" : s.Name;
            sb.AppendLine($"TITL {title}");

 // ── CELL / ZERR ────────────────────────────────────────
            float wavelength = 0.71073f; // Mo default – can be made a parameter later
            sb.AppendLine(string.Format(inv,
                "CELL {0:F5} {1:F4} {2:F4} {3:F4} {4:F3} {5:F3} {6:F3}",
                wavelength, s.a, s.b, s.c, s.alpha, s.beta, s.gamma));

            sb.AppendLine(string.Format(inv,
                "ZERR {0} {1:F4} {2:F4} {3:F4} {4:F3} {5:F3} {6:F3}",
                Math.Max(1, s.Atoms.Count),
                s.a_Error > 0 ? s.a_Error : 0.0001f,
                s.b_Error > 0 ? s.b_Error : 0.0001f,
                s.c_Error > 0 ? s.c_Error : 0.0001f,
                s.alpha_Error > 0 ? s.alpha_Error : 0.01f,
                s.beta_Error > 0 ? s.beta_Error : 0.01f,
                s.gamma_Error > 0 ? s.gamma_Error : 0.01f));

 // ── LATT + SYMM (z bazy, without zgadywania) ───────────────
            var ops = SymmetryHelper.GetOperationStrings(s);

 // SHELX: LATT > 0 → centrosymmetric (program will add inversion)
 // LATT < 0 → non-centrosymmetric (all operacje afterdajemy sami)
 // Safest: always LATT -1 i we will write the full list (without identity).
            bool hasInversion = SymmetryHelper.ContainsInversion(ops);
            int latt = hasInversion ? 1 : -1;   // P-lattice; sign depends on inversion
 // (for C/I/F/R lattice type can be extended later)

            sb.AppendLine($"LATT {latt}");

            foreach (var op in ops)
            {
                string normalized = op.Replace(" ", "").ToLowerInvariant();
                if (normalized == "x,y,z") continue;          // identity always implied
                sb.AppendLine($"SYMM {op}");
            }

 // ── SFAC / UNIT ────────────────────────────────────────
            var elements = s.Atoms
                .Select(a => string.IsNullOrEmpty(a.Symbol) ? "C" : a.Symbol)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            sb.Append("SFAC");
            foreach (var el in elements)
                sb.Append(" ").Append(el);
            sb.AppendLine();

            sb.Append("UNIT");
            foreach (var el in elements)
            {
                int count = s.Atoms.Count(a =>
                    string.Equals(a.Symbol, el, StringComparison.OrdinalIgnoreCase));
                sb.Append(" ").Append(count);
            }
            sb.AppendLine();

 // ── Atoms ──────────────────────────────────────────────
            var sfacMap = elements
                .Select((e, i) => (e, idx: i + 1))
                .ToDictionary(x => x.e, x => x.idx, StringComparer.OrdinalIgnoreCase);

            foreach (var a in s.Atoms)
            {
                string label = string.IsNullOrEmpty(a.Name) ? a.Symbol : a.Name;
                if (label.Length > 6) label = label.Substring(0, 6);

                int type = sfacMap.TryGetValue(a.Symbol ?? "C", out int t) ? t : 1;

 // occupancy: SHELX uses 11.000 for free, 10.xxx for fixed, etc.
 // We write the real occupancy + 10 (free variable)
                float occ = a.Occupancy + 10f;
                if (occ < 10.001f) occ = 11.000f;   // fully occupied free

                if (string.Equals(a.AdpType, "Uani", StringComparison.OrdinalIgnoreCase) &&
                    (Math.Abs(a.U11) > 1e-6 || Math.Abs(a.U22) > 1e-6))
                {
 // anisotropic
                    sb.AppendLine(string.Format(inv,
                        "{0,-6} {1} {2,9:F5} {3,9:F5} {4,9:F5} {5,8:F5} {6,7:F4} =\n" +
                        "            {7,7:F4} {8,7:F4} {9,7:F4} {10,7:F4} {11,7:F4}",
                        label, type, a.X, a.Y, a.Z, occ, a.U11,
                        a.U22, a.U33, a.U23, a.U13, a.U12));
                }
                else
                {
 // isotropic
                    float uiso = a.iso > 0 ? a.iso : 0.05f;
                    sb.AppendLine(string.Format(inv,
                        "{0,-6} {1} {2,9:F5} {3,9:F5} {4,9:F5} {5,8:F5} {6,7:F4}",
                        label, type, a.X, a.Y, a.Z, occ, uiso));
                }
            }

            sb.AppendLine("HKLF 4");
            sb.AppendLine("END");

            File.WriteAllText(path, sb.ToString(), Encoding.ASCII);
            return true;
        }
    }
}