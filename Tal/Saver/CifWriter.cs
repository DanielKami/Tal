// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure / image exafterrt: CifWriter.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Globalization;
using System.Text;
using Tal.Saver;

namespace Tal
{
 /// <summary>
 /// Writes a StructureData object to a CIF (IUCr) file.
 /// Uses SymmetryHelper / SymmetryElements so symmetry operations
 /// are never guessed.
 /// </summary>
    public static class CifWriter
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static bool Write(StructureData s, string path)
        {
            if (s == null || s.Atoms == null || s.Atoms.Count == 0)
                return false;

            try
            {
                var sb = new StringBuilder(4096);

                string dataName = SanitizeDataName(s.Name);
                sb.AppendLine($"data_{dataName}");
                sb.AppendLine();

 // ─────────────────────────────────────────────
 // Audit / software
 // ─────────────────────────────────────────────
                sb.AppendLine($"_audit_creation_date               {DateTime.Now:yyyy-MM-dd}");
                sb.AppendLine($"_audit_creation_method             'TAL crystallographic application'");
                sb.AppendLine();

 // ─────────────────────────────────────────────
 // Chemical info
 // ─────────────────────────────────────────────
                WriteText(sb, "_chemical_name_common", s.ChemicalName ?? s.Name);
                if (!string.IsNullOrWhiteSpace(s.Formula))
                    WriteText(sb, "_chemical_formula_sum", s.Formula);
                if (s.FormulaWeight > 0.01f)
                    WriteFloat(sb, "_chemical_formula_weight", s.FormulaWeight, "F3");
                sb.AppendLine();

 // ─────────────────────────────────────────────
 // Unit cell
 // ─────────────────────────────────────────────
                WriteFloat(sb, "_cell_length_a", s.a, "F5", s.a_Error);
                WriteFloat(sb, "_cell_length_b", s.b, "F5", s.b_Error);
                WriteFloat(sb, "_cell_length_c", s.c, "F5", s.c_Error);
                WriteFloat(sb, "_cell_angle_alpha", s.alpha, "F3", s.alpha_Error);
                WriteFloat(sb, "_cell_angle_beta", s.beta, "F3", s.beta_Error);
                WriteFloat(sb, "_cell_angle_gamma", s.gamma, "F3", s.gamma_Error);

                if (s.Volume > 0.1f)
                    WriteFloat(sb, "_cell_volume", s.Volume, "F2");

                if (s.Temperature > 0.1f)
                    WriteFloat(sb, "_diffrn_ambient_temperature", s.Temperature, "F1");

                if (s.RFactor > 0.0001f && s.RFactor < 1.0f)
                    WriteFloat(sb, "_refine_ls_R_factor_gt", s.RFactor, "F4");

                sb.AppendLine();

 // ─────────────────────────────────────────────
 // Space group
 // ─────────────────────────────────────────────
                if (s.SpaceGroup > 0 && s.SpaceGroup <= 230)
                    sb.AppendLine($"_space_group_IT_number             {s.SpaceGroup}");

                if (!string.IsNullOrWhiteSpace(s.SpaceGroupName))
                    WriteText(sb, "_space_group_name_H-M_alt", s.SpaceGroupName);

                sb.AppendLine();

 // ─────────────────────────────────────────────
 // Symmetry operations (never guessed)
 // ─────────────────────────────────────────────
                var ops = SymmetryHelper.GetOperationStrings(s);

                sb.AppendLine("loop_");
                sb.AppendLine("_space_group_symop_id");
                sb.AppendLine("_space_group_symop_operation_xyz");
                for (int i = 0; i < ops.Count; i++)
                {
                    sb.AppendLine($"{i + 1,3} '{ops[i]}'");
                }
                sb.AppendLine();

 // ─────────────────────────────────────────────
 // Atom sites
 // ─────────────────────────────────────────────
                bool hasAniso = s.Atoms.Any(a =>
                    string.Equals(a.AdpType, "Uani", StringComparison.OrdinalIgnoreCase) &&
                    (Math.Abs(a.U11) > 1e-8 || Math.Abs(a.U22) > 1e-8 || Math.Abs(a.U33) > 1e-8));

                bool hasDisorder = s.Atoms.Any(a => a.DisorderGroup != 0);

                sb.AppendLine("loop_");
                sb.AppendLine("_atom_site_label");
                sb.AppendLine("_atom_site_type_symbol");
                sb.AppendLine("_atom_site_fract_x");
                sb.AppendLine("_atom_site_fract_y");
                sb.AppendLine("_atom_site_fract_z");
                sb.AppendLine("_atom_site_occupancy");
                sb.AppendLine("_atom_site_U_iso_or_equiv");
                sb.AppendLine("_atom_site_adp_type");
                if (hasDisorder)
                    sb.AppendLine("_atom_site_disorder_group");

                foreach (var a in s.Atoms)
                {
                    string label = string.IsNullOrWhiteSpace(a.Name) ? a.Symbol : a.Name;
                    string symbol = string.IsNullOrWhiteSpace(a.Symbol) ? "C" : a.Symbol;
                    string adp = string.IsNullOrWhiteSpace(a.AdpType) ? "Uiso" : a.AdpType;
                    float uiso = a.iso > 1e-8f ? a.iso : 0.05f;

                    var line = new StringBuilder();
                    line.Append(string.Format(Inv, "{0,-8} {1,-3} ", label, symbol));
                    line.Append(FormatCoord(a.X, a.X_Error));
                    line.Append(' ');
                    line.Append(FormatCoord(a.Y, a.Y_Error));
                    line.Append(' ');
                    line.Append(FormatCoord(a.Z, a.Z_Error));
                    line.Append(string.Format(Inv, " {0,6:F3} {1,8:F5} {2,-5}",
                        a.Occupancy > 0 ? a.Occupancy : 1.0f, uiso, adp));

                    if (hasDisorder)
                        line.Append(string.Format(Inv, " {0,3}", a.DisorderGroup));

                    sb.AppendLine(line.ToString());
                }
                sb.AppendLine();

 // ─────────────────────────────────────────────
 // Anisotropic displacement parameters
 // ─────────────────────────────────────────────
                if (hasAniso)
                {
                    sb.AppendLine("loop_");
                    sb.AppendLine("_atom_site_aniso_label");
                    sb.AppendLine("_atom_site_aniso_U_11");
                    sb.AppendLine("_atom_site_aniso_U_22");
                    sb.AppendLine("_atom_site_aniso_U_33");
                    sb.AppendLine("_atom_site_aniso_U_12");
                    sb.AppendLine("_atom_site_aniso_U_13");
                    sb.AppendLine("_atom_site_aniso_U_23");

                    foreach (var a in s.Atoms)
                    {
                        if (!string.Equals(a.AdpType, "Uani", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (Math.Abs(a.U11) < 1e-8 && Math.Abs(a.U22) < 1e-8 && Math.Abs(a.U33) < 1e-8)
                            continue;

                        string label = string.IsNullOrWhiteSpace(a.Name) ? a.Symbol : a.Name;
                        sb.AppendLine(string.Format(Inv,
                            "{0,-8} {1,9:F5} {2,9:F5} {3,9:F5} {4,9:F5} {5,9:F5} {6,9:F5}",
                            label, a.U11, a.U22, a.U33, a.U12, a.U13, a.U23));
                    }
                    sb.AppendLine();
                }

 // ─────────────────────────────────────────────
                File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CifWriter] Error: {ex.Message}");
                return false;
            }
        }

        #region Formatting helpers

        private static void WriteText(StringBuilder sb, string tag, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            string v = value.Trim();
 // CIF string quoting rules (simple but safe)
            if (v.Contains('\n') || v.Contains('\r'))
            {
                sb.AppendLine(tag);
                sb.AppendLine(";");
                sb.AppendLine(v);
                sb.AppendLine(";");
            }
            else if (v.Contains(' ') || v.Contains('\'') || v.Contains('"') || v.StartsWith("_"))
            {
 // prefer single quotes; escape internal single quotes by doubling
                string escaped = v.Replace("'", "''");
                sb.AppendLine($"{tag,-34} '{escaped}'");
            }
            else
            {
                sb.AppendLine($"{tag,-34} {v}");
            }
        }

        private static void WriteFloat(StringBuilder sb, string tag, float value, string format, float error = 0f)
        {
            if (error > 1e-8f)
            {
 // su value(esd) style – simplified
                string val = value.ToString(format, Inv);
                string esd = FormatEsd(error);
                sb.AppendLine($"{tag,-34} {val}({esd})");
            }
            else
            {
                sb.AppendLine($"{tag,-34} {value.ToString(format, Inv)}");
            }
        }

        private static string FormatCoord(float coord, float error)
        {
            if (error > 1e-8f)
            {
 // rough digit matching
                int digits = Math.Max(0, (int)Math.Ceiling(-Math.Log10(error)) + 1);
                digits = Math.Clamp(digits, 3, 6);
                string fmt = "F" + digits;
                string esd = FormatEsd(error);
                return string.Format(Inv, "{0}({1})", coord.ToString(fmt, Inv), esd);
            }
            return string.Format(Inv, "{0:F5}", coord);
        }

        private static string FormatEsd(float error)
        {
            if (error >= 1f) return ((int)Math.Round(error)).ToString(Inv);
            if (error >= 0.1f) return error.ToString("F1", Inv).Replace(".", "");
 // scientific-ish → just significant digits
            double log = Math.Floor(Math.Log10(error));
            double scaled = error / Math.Pow(10, log);
            int rounded = (int)Math.Round(scaled);
            if (rounded >= 10) { rounded /= 10; log += 1; }
 // return only the integer part of the esd as commonly done in CIF
            return rounded.ToString(Inv);
        }

        private static string SanitizeDataName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "tal_structure";

            var sb = new StringBuilder(name.Length);
            foreach (char c in name.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')
                    sb.Append(c);
                else if (char.IsWhiteSpace(c))
                    sb.Append('_');
            }
            string result = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(result) ? "tal_structure" : result;
        }

        #endregion
    }
}