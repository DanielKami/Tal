// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure file parsers: CifParser.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// EXTENDED: full CIF parsing (refinement, reflections, experiment, publication)
// FIXED: CCDC anisotropic parsing — handling variants of U_11/U11, U_22/U22, etc.
// FIXED: removed ref from Dictionary — pass object by reference

using System.Globalization;
using System.Text.RegularExpressions;
using Tal;

namespace Tal.Parsers
{
 /// <summary>
 /// Parses Crystallographic Information File (CIF) content into structured data.
 /// Handles unit cell parameters, atomic coordinates, symmetry operations,
 /// refinement statistics, reflection statistics, experiment details,
 /// anisotropic displacement parameters, and publication metadata.
 ///
 /// ⭐ FIX CCDC: obsługa obu wariantów kluczy anizotroafterwych:
 /// _atom_site_aniso_U_11 → "U11" (after normalisation)
 /// _atom_site_aniso_U11 → "U11"
 /// Normalizacja + checksnie obu wariantów w warunku wejścia.
 /// </summary>
    public static class CifParser
    {
        #region Constants

 /// <summary>Array of chemical element symbols indexed by atomic number (1-based).</summary>
        private static readonly string[] _elements = new string[]
        {
            "H","He","Li","Be","B","C","N","O","F","Ne","Na","Mg",
            "Al","Si","P","S","Cl","Ar","K","Ca","Sc","Ti","V","Cr",
            "Mn","Fe","Co","Ni","Cu","Zn","Ga","Ge","As","Se","Br",
            "Kr","Rb","Sr","Y","Zr","Nb","Mo","Tc","Ru","Rh","Pd",
            "Ag","Cd","In","Sn","Sb","Te","I","Xe","Cs","Ba","La",
            "Ce","Pr","Nd","Pm","Sm","Eu","Gd","Tb","Dy","Ho","Er",
            "Tm","Yb","Lu","Hf","Ta","W","Re","Os","Ir","Pt","Au",
            "Hg","Tl","Pb","Bi","Po","At","Rn","Fr","Ra","Ac","Th",
            "Pa","U","Np","Pu","Am","Cm","Bk","Cf"
        };

        #endregion

        #region Public Methods

 /// <summary>
 /// Parses CIF content and populates the CifData object.
 /// </summary>
        public static bool Parse(string content, CifData cif)
        {
            if (string.IsNullOrEmpty(content))
                return false;

            if (cif == null)
                return false;

            if (CountDataBlocks(content) > 1)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[CifParser] ⚠️ Multiple 'data_' blocks detected in a single Parse() call. " +
                    "Use CifDivider.Divide() and parse each block separately.");
                return false;
            }

            cif.Clean();

            var lines = Regex.Split(content, "\r\n|\r|\n");

 // ⭐ Słowniki tworzone RAZ — przekazywane without ref (są typami referencyjnymi)
            var atomFlags = new Dictionary<string, int>();
            var anisoFlags = new Dictionary<string, int>();

 // ---- Step 1: Simple values ----
            ParseSimpleValues(lines, cif);

 // ---- Step 2: Symmetry operations ----
            ParseSymmetryOperations(lines, cif);

 // ---- Step 3: Normalize space group ----
            NormalizeSpaceGroup(cif);

 // ---- Step 4: Find atom_site loop ----
            ParseAtomSiteLoop(lines, cif, atomFlags, out bool hasAtomFlags);

 // ---- Step 5: Find anisotropic displacement loop ----
            ParseAnisotropicLoop(lines, cif, anisoFlags, out bool hasAnisoFlags);

 // ---- Step 6: Parse single-line anisotropic data (legacy format) ----
            ParseSingleLineAnisotropic(lines, cif);

 // ---- Step 7: Set default isotropic values if missing ----
            SetDefaultIsotropicValues(cif);

 // ---- Step 8: Assign atomic numbers from names if not set ----
            AssignAtomicNumbersFromNames(cif);

 // ---- Step 9: Log summary ----
            LogParsingSummary(cif);

            cif.completed = true;
            System.Diagnostics.Debug.WriteLine($"[CifParser] Parsing completed. Atoms: {cif.atom.Count}");

            return true;
        }

 /// <summary>
 /// Parses a CIF file that may contain MULTIPLE data blocks.
 /// </summary>
        public static List<CifData> ParseMultiBlock(string content, string filePath = "")
        {
            var result = new List<CifData>();

            if (string.IsNullOrEmpty(content))
                return result;

            var blocks = CifDivider.Divide(content, filePath);

            System.Diagnostics.Debug.WriteLine(
                $"[CifParser] ParseMultiBlock: found {blocks.Count} blocks in file '{filePath}'");

            foreach (var block in blocks)
            {
                var cif = new CifData();
                cif.FileName = block.Name;

                if (Parse(block.RawData, cif))
                {
                    result.Add(cif);
                    System.Diagnostics.Debug.WriteLine(
                        $"[CifParser]   ✓ Block '{block.Name}': {cif.atom.Count} atoms, " +
                        $"grupa {cif.space_group_IT_number}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[CifParser]   ✗ Blok '{block.Name}': parsowanie nieudane");
                }
            }

            return result;
        }

        #endregion

        #region Private Helpers

 /// <summary>
 /// Counts occurrences of 'data_' directives.
 /// </summary>
        private static int CountDataBlocks(string content)
        {
            if (string.IsNullOrEmpty(content))
                return 0;

            int count = 0;
            var lines = Regex.Split(content, "\r\n|\r|\n");
            foreach (var line in lines)
            {
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("data_"))
                    count++;

                if (count > 1)
                    return count;
            }
            return count;
        }

        #endregion

        #region Private Parsing Methods

 /// <summary>
 /// Parses simple CIF values.
 /// </summary>
        private static void ParseSimpleValues(string[] lines, CifData cif)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#")) continue;
                if (!line.StartsWith("_")) continue;

 // UNIT CELL
                if (line.StartsWith("_cell_length_a")) { cif.a = GetValue(line); cif.a_e = GetError(line); }
                else if (line.StartsWith("_cell_length_b")) { cif.b = GetValue(line); cif.b_e = GetError(line); }
                else if (line.StartsWith("_cell_length_c")) { cif.c = GetValue(line); cif.c_e = GetError(line); }
                else if (line.StartsWith("_cell_angle_alpha")) { cif.alp = GetValue(line); cif.alp_e = GetError(line); }
                else if (line.StartsWith("_cell_angle_beta")) { cif.bet = GetValue(line); cif.bet_e = GetError(line); }
                else if (line.StartsWith("_cell_angle_gamma")) { cif.gam = GetValue(line); cif.gam_e = GetError(line); }
                else if (line.StartsWith("_cell_volume")) { cif.cell_volume = GetValue(line); cif.cell_volume_e = GetError(line); }
                else if (line.StartsWith("_cell_formula_units_Z")) { cif.formula_units_Z = GetValue(line); }
                else if (line.StartsWith("_cell_measurement_reflns_used")) { cif.cell_measurement_reflns_used = (int)GetValue(line); }
                else if (line.StartsWith("_cell_measurement_theta_min")) { cif.cell_theta_min = GetValue(line); }
                else if (line.StartsWith("_cell_measurement_theta_max")) { cif.cell_theta_max = GetValue(line); }
                else if (line.StartsWith("_cell_measurement_temperature")) { cif.measurement_temperature = GetValue(line); }
                else if (line.StartsWith("_cell_measurement_wavelength")) { if (cif.wavelength == 0) cif.wavelength = GetValue(line); }

 // SYMMETRY
                else if (line.StartsWith("_space_group_IT_number")) { cif.space_group_IT_number = (int)GetValue(line); }
                else if (line.StartsWith("_symmetry_Int_Tables_number")) { if (cif.space_group_IT_number == 0) cif.space_group_IT_number = (int)GetValue(line); }
                else if (line.StartsWith("_space_group_name_H-M_alt")) { cif.space_group_name_HM = GetStringValue(line); }
                else if (line.StartsWith("_space_group_name_H-M")) { if (string.IsNullOrEmpty(cif.space_group_name_HM)) cif.space_group_name_HM = GetStringValue(line); }
                else if (line.StartsWith("_symmetry_space_group_name_H-M")) { if (string.IsNullOrEmpty(cif.space_group_name_HM)) cif.space_group_name_HM = GetStringValue(line); }
                else if (line.StartsWith("_space_group_name_Hall")) { cif.space_group_name_hall = GetStringValue(line); }

 // CHEMICAL METADATA
                else if (line.StartsWith("_chemical_name_common")) { cif.chemical_name_common = GetStringValue(line); }
                else if (line.StartsWith("_chemical_name_systematic")) { cif.chemical_name_systematic = GetStringValue(line); }
                else if (line.StartsWith("_chemical_formula_sum")) { cif.formula_sum = GetStringValue(line); }
                else if (line.StartsWith("_chemical_formula_moiety")) { cif.formula_moiety = GetStringValue(line); }
                else if (line.StartsWith("_chemical_formula_weight")) { cif.formula_weight = GetValue(line); }
                else if (line.StartsWith("_chemical_oxdiff_usercomment")) { cif.chemical_oxdiff_usercomment = GetStringValue(line); }

 // TEMPERATURE
                else if (line.StartsWith("_diffrn_ambient_temperature")) { if (cif.measurement_temperature == 0) cif.measurement_temperature = GetValue(line); }

 // REFINEMENT STATISTICS
                else if (line.StartsWith("_refine_ls_R_factor_gt")) { cif.s_R_factor_gt = GetValue(line); }
                else if (line.StartsWith("_refine_ls_R_factor_obs")) { if (cif.s_R_factor_gt == 0) cif.s_R_factor_gt = GetValue(line); }
                else if (line.StartsWith("_refine_ls_R_factor_all")) { cif._refine_ls_R_factor_all = GetValue(line); }
                else if (line.StartsWith("_refine_ls_wR_factor_ref")) { cif.refine_wR2 = GetValue(line); }
                else if (line.StartsWith("_refine_ls_wR_factor_all")) { if (cif.refine_wR2 == 0) cif.refine_wR2 = GetValue(line); }
                else if (line.StartsWith("_refine_ls_wR_factor_gt")) { cif.refine_wR2_gt = GetValue(line); }
                else if (line.StartsWith("_refine_ls_goodness_of_fit_ref")) { cif.refine_goof = GetValue(line); }
                else if (line.StartsWith("_refine_ls_goodness_of_fit_all")) { if (cif.refine_goof == 0) cif.refine_goof = GetValue(line); }
                else if (line.StartsWith("_refine_ls_number_reflns")) { cif.refine_nreflns = (int)GetValue(line); }
                else if (line.StartsWith("_refine_ls_number_parameters")) { cif.refine_nparams = (int)GetValue(line); }
                else if (line.StartsWith("_refine_ls_number_restraints")) { cif.refine_nrestraints = (int)GetValue(line); }
                else if (line.StartsWith("_refine_ls_shift/su_max")) { cif.refine_shift_max = GetValue(line); }
                else if (line.StartsWith("_refine_ls_shift/su_mean")) { cif.refine_shift_mean = GetValue(line); }
                else if (line.StartsWith("_refine_ls_weighting_details")) { cif.refine_weighting = GetStringValue(line); }
                else if (line.StartsWith("_refine_ls_weighting_scheme")) { if (string.IsNullOrEmpty(cif.refine_weighting)) cif.refine_weighting = GetStringValue(line); }
                else if (line.StartsWith("_refine_diff_density_max")) { cif.refine_diff_max = GetValue(line); }
                else if (line.StartsWith("_refine_diff_density_min")) { cif.refine_diff_min = GetValue(line); }
                else if (line.StartsWith("_refine_diff_density_rms")) { cif.refine_diff_rms = GetValue(line); }

 // REFLECTION STATISTICS
                else if (line.StartsWith("_reflns_number_total")) { cif.reflns_total = (int)GetValue(line); }
                else if (line.StartsWith("_reflns_number_gt")) { cif.reflns_gt = (int)GetValue(line); }
                else if (line.StartsWith("_reflns_number_observed")) { if (cif.reflns_gt == 0) cif.reflns_gt = (int)GetValue(line); }
                else if (line.StartsWith("_reflns_threshold_expression")) { cif.reflns_threshold = GetStringValue(line); }
                else if (line.StartsWith("_reflns_d_resolution_high")) { cif.resolution_high = GetValue(line); }
                else if (line.StartsWith("_reflns_d_resolution_low")) { cif.resolution_low = GetValue(line); }
                else if (line.StartsWith("_diffrn_reflns_theta_min")) { cif.theta_min = GetValue(line); }
                else if (line.StartsWith("_diffrn_reflns_theta_max")) { cif.theta_max = GetValue(line); }

 // EXPERIMENT
                else if (line.StartsWith("_exptl_crystal_F_000")) { cif.F_000 = GetValue(line); }
                else if (line.StartsWith("_exptl_crystal_f_000")) { if (cif.F_000 == 0) cif.F_000 = GetValue(line); }
                else if (line.StartsWith("_diffrn_radiation_wavelength")) { cif.wavelength = GetValue(line); }
                else if (line.StartsWith("_diffrn_radiation_type")) { cif.radiation_type = GetStringValue(line); }
                else if (line.StartsWith("_diffrn_measurement_device_type")) { cif.measurement_device = GetStringValue(line); }
                else if (line.StartsWith("_diffrn_source")) { cif.diffrn_source = GetStringValue(line); }
                else if (line.StartsWith("_diffrn_detector")) { cif.diffrn_detector = GetStringValue(line); }
                else if (line.StartsWith("_exptl_crystal_colour")) { cif.crystal_colour = GetStringValue(line); }
                else if (line.StartsWith("_exptl_crystal_description")) { cif.crystal_description = GetStringValue(line); }
                else if (line.StartsWith("_exptl_crystal_size_max")) { cif.crystal_size_max = GetValue(line); }
                else if (line.StartsWith("_exptl_crystal_size_mid")) { cif.crystal_size_mid = GetValue(line); }
                else if (line.StartsWith("_exptl_crystal_size_min")) { cif.crystal_size_min = GetValue(line); }
                else if (line.StartsWith("_exptl_absorpt_coefficient_mu")) { cif.exptl_absorpt_coefficient_mu = GetValue(line); }
                else if (line.StartsWith("_exptl_absorpt_correction_type")) { cif.exptl_absorpt_correction_type = GetStringValue(line); }
                else if (line.StartsWith("_exptl_absorpt_correction_T_min")) { cif.abs_correction_t_min = GetValue(line); }
                else if (line.StartsWith("_exptl_absorpt_correction_T_max")) { cif.abs_correction_t_max = GetValue(line); }
                else if (line.StartsWith("_exptl_crystal_density_diffrn")) { cif.exptl_crystal_density_diffrn = GetValue(line); }
                else if (line.StartsWith("_exptl_crystal_density_method")) { cif.exptl_crystal_density_method = GetStringValue(line); }

 // AUDIT / PUBLICATION
                else if (line.StartsWith("_audit_creation_date")) { cif.audit_creation_date = GetStringValue(line); }
                else if (line.StartsWith("_audit_creation_method")) { cif.audit_creation_method = GetStringValue(line); }
                else if (line.StartsWith("_publ_section_title")) { cif.publ_title = GetStringValue(line); }
                else if (line.StartsWith("_journal_name_full")) { cif.journal_name = GetStringValue(line); }
                else if (line.StartsWith("_journal_coden_CAS")) { if (string.IsNullOrEmpty(cif.journal_name)) cif.journal_name = GetStringValue(line); }
                else if (line.StartsWith("_journal_volume")) { cif.journal_volume = GetStringValue(line); }
                else if (line.StartsWith("_journal_page_first")) { cif.journal_page = GetStringValue(line); }
                else if (line.StartsWith("_journal_year")) { cif.journal_year = GetStringValue(line); }
                else if (line.StartsWith("_journal_paper_doi")) { cif.doi = GetStringValue(line); }
            }
        }

 /// <summary>
 /// Normalizes the space group name and assigns the International Tables number.
 /// </summary>
        private static void NormalizeSpaceGroup(CifData cif)
        {
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"[CifParser][SG] START");
            System.Diagnostics.Debug.WriteLine($"[CifParser][SG]   name_HM = '{cif.space_group_name_HM}'");
            System.Diagnostics.Debug.WriteLine($"[CifParser][SG]   IT_number = {cif.space_group_IT_number}");
            System.Diagnostics.Debug.WriteLine($"[CifParser][SG]   SymmetryOperations.Count = {cif.SymmetryOperations?.Count ?? 0}");
            System.Diagnostics.Debug.WriteLine($"[CifParser][SG]   cell: a={cif.a} b={cif.b} c={cif.c} α={cif.alp} β={cif.bet} γ={cif.gam}");
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════");

 // 1. Próba z nazwy grupy
            if (cif.space_group_IT_number == 0 &&
                !string.IsNullOrEmpty(cif.space_group_name_HM) &&
                cif.space_group_name_HM != "?")
            {
                string normalized = SymmetryElements.NormalizeSpaceGroupName(cif.space_group_name_HM);

                if (SymmetryElements.GroupNameToNumber.ContainsKey(normalized))
                {
                    cif.space_group_IT_number = SymmetryElements.GroupNameToNumber[normalized];
                    System.Diagnostics.Debug.WriteLine(
                        $"[CifParser][SG] Assigned group {cif.space_group_IT_number} " +
                        $"from name '{cif.space_group_name_HM}' (normalized: '{normalized}')");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[CifParser][SG] ⚠️ Group name '{cif.space_group_name_HM}' " +
                        $"normalized to '{normalized}' NOT FOUND in dictionary");
                }
            }

 // 2. Detekcja przez ZBIÓR OPERACJI
            if (cif.space_group_IT_number == 0 &&
                cif.SymmetryOperations != null &&
                cif.SymmetryOperations.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[CifParser][SG] No group name — detecting from " +
                    $"{cif.SymmetryOperations.Count} symmetry operations");

                int detectedGroup = SymmetryElements.DetectSpaceGroupByOperations(
                    cif.a, cif.b, cif.c,
                    cif.alp, cif.bet, cif.gam,
                    cif.SymmetryOperations);

                if (detectedGroup > 0)
                {
                    cif.space_group_IT_number = detectedGroup;

                    string detectedName = GetSpaceGroupNameFromNumber(detectedGroup);
                    if (!string.IsNullOrEmpty(detectedName))
                        cif.space_group_name_HM = detectedName;

                    System.Diagnostics.Debug.WriteLine(
                        $"[CifParser][SG] ✅ Detected group {detectedGroup} " +
                        $"({cif.space_group_name_HM}) from operations");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[CifParser][SG] ❌ Could not detect group from operations");
                }
            }

 // 3. Fallback — P1
            if (cif.space_group_IT_number == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[CifParser][SG] ⚠️ Defaulting to P1 (group 1)");
                cif.space_group_IT_number = 1;
                cif.space_group_name_HM = "P1";
            }

            System.Diagnostics.Debug.WriteLine(
                $"[CifParser][SG] FINAL: group={cif.space_group_IT_number}, " +
                $"name='{cif.space_group_name_HM}'");
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════");
        }

 /// <summary>
 /// Returns name space group for numeru.
 /// </summary>
        private static string GetSpaceGroupNameFromNumber(int groupNumber)
        {
            foreach (var entry in SymmetryElements.SymmetryOperations)
            {
                if (entry.Length >= 3 &&
                    int.TryParse(entry[0], out int g) && g == groupNumber)
                {
                    return entry[2];
                }
            }
            return "";
        }

 /// <summary>
 /// Parses symmetry operations from a loop_ structure.
 /// </summary>
        private static void ParseSymmetryOperations(string[] lines, CifData cif)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line == "loop_")
                {
                    bool hasSymmetryFlags = false;
                    int start = i + 1;
                    int j = start;
                    while (j < lines.Length && lines[j].Trim().StartsWith("_"))
                    {
                        string flag = lines[j].Trim();
                        if (flag.Contains("_space_group_symop_operation_xyz") ||
                            flag.Contains("_symmetry_equiv_pos_as_xyz"))
                        {
                            hasSymmetryFlags = true;
                        }
                        j++;
                    }
                    if (hasSymmetryFlags)
                    {
                        for (int k = j; k < lines.Length; k++)
                        {
                            string raw = lines[k];
                            string op = raw.Trim();

                            if (string.IsNullOrEmpty(op)) continue;
                            if (op.StartsWith("loop_")) break;
                            if (op.StartsWith("_")) break;
                            if (op.StartsWith("#")) break;

                            string cleanedOp = StripLeadingIndex(op);

                            if (string.IsNullOrEmpty(cleanedOp))
                                continue;

                            cif.SymmetryOperations.Add(cleanedOp);
                            SymmetryElements.CreateTransformationFromFile(cleanedOp);

                            System.Diagnostics.Debug.WriteLine(
                                $"[CifParser][SymOp] Added: '{cleanedOp}'");
                        }
                        break;
                    }
                }
            }
        }

 /// <summary>
 /// Removes leading numeric index from a symmetry line.
 /// </summary>
        private static string StripLeadingIndex(string op)
        {
            if (string.IsNullOrEmpty(op))
                return op;

            int spaceIdx = op.IndexOf(' ');
            if (spaceIdx > 0)
            {
                string firstToken = op.Substring(0, spaceIdx);
                if (int.TryParse(firstToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                {
                    return op.Substring(spaceIdx + 1).Trim();
                }
            }

            return op;
        }

 /// <summary>
 /// Parses the atom_site loop containing atomic coordinates and properties.
 /// ⭐ Słownik przekazywany without ref (typ referencyjny) — modyfikowany in-place.
 /// ⭐ hasAtomFlags returnsne przez out.
 /// </summary>
        private static void ParseAtomSiteLoop(string[] lines, CifData cif,
            Dictionary<string, int> atomFlags, out bool hasAtomFlags)
        {
            hasAtomFlags = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line != "loop_") continue;

                atomFlags.Clear();
                bool localHasAtomFlags = false;

                int start = i + 1;
                int j = start;

                while (j < lines.Length && lines[j].Trim().StartsWith("_"))
                {
                    string flag = lines[j].Trim();

 // ⭐ Wyklucz flagi anizotroafterwe — one należą do innego loop_
                    if (flag.Contains("_atom_site_") && !flag.Contains("_atom_site_aniso_"))
                    {
                        string key = flag.Replace("_atom_site_", "").Trim();
                        atomFlags[key] = j - start;
                        localHasAtomFlags = true;

                        System.Diagnostics.Debug.WriteLine(
                            $"[CifParser] ATOM FLAG: {key} -> {j - start}");
                    }

                    j++;
                }

                if (localHasAtomFlags &&
                    atomFlags.ContainsKey("fract_x") &&
                    atomFlags.ContainsKey("fract_y") &&
                    atomFlags.ContainsKey("fract_z"))
                {
                    for (int k = j; k < lines.Length; k++)
                    {
                        string trimmed = lines[k].Trim();

                        if (string.IsNullOrEmpty(trimmed)) continue;
                        if (trimmed.StartsWith("loop_")) break;
                        if (trimmed.StartsWith("_")) break;
                        if (trimmed.StartsWith("#")) break;

                        var atom = ParseAtomLine(trimmed, atomFlags);
                        if (atom != null)
                        {
                            cif.atom.Add(atom);
                            if (cif.atom.Count <= 20)
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"[CifParser] Atom {cif.atom.Count}: {atom.name} " +
                                    $"type={atom.type} adp_type={atom.adp_type} " +
                                    $"iso={atom.iso:F6} occ={atom.occ:F3}");
                            }
                        }
                    }

                    hasAtomFlags = true;
                    return;   // ⭐ znaleziono i sparsowano — koniec
                }
            }

            System.Diagnostics.Debug.WriteLine(
                "[CifParser] ❌ Nie znaleziono bloku atom_site!");
        }

 /// <summary>
 /// Parses the anisotropic displacement parameter loop.
 ///
 /// ⭐ FIX CCDC: obsługa obu wariantów kluczy anizotroafterwych:
 /// _atom_site_aniso_U_11 → "U11" (after normalisation)
 /// _atom_site_aniso_U11 → "U11"
 /// Pełne logs diagnostyczne na każdym etapie.
 /// </summary>
        private static void ParseAnisotropicLoop(string[] lines, CifData cif,
            Dictionary<string, int> anisoFlags, out bool hasAnisoFlags)
        {
            hasAnisoFlags = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line != "loop_") continue;

                anisoFlags.Clear();
                bool localHasAnisoFlags = false;

                int start = i + 1;
                int j = start;

                System.Diagnostics.Debug.WriteLine(
                    $"[CifParser][ANISO] ── Found loop_ at line {i}, scanning flags...");

                while (j < lines.Length && lines[j].Trim().StartsWith("_"))
                {
                    string flag = lines[j].Trim();

                    if (flag.Contains("_atom_site_aniso_"))
                    {
 // ⭐ Zachowaj surowy klucz do logs
                        string rawKey = flag.Replace("_atom_site_aniso_", "").Trim();

 // ⭐ NORMALIZACJA: "U_11" → "U11", "U11" → "U11"
                        string key = NormalizeAnisoKey(rawKey);

                        anisoFlags[key] = j - start;
                        localHasAnisoFlags = true;

                        System.Diagnostics.Debug.WriteLine(
                            $"[CifParser][ANISO]   FLAG raw='{rawKey}' → key='{key}' → index={j - start}");
                    }

                    j++;
                }

 // ⭐ Check all warianty kluczy (after normalisation)
                bool hasU11 = anisoFlags.ContainsKey("U11");
                bool hasU22 = anisoFlags.ContainsKey("U22");
                bool hasU33 = anisoFlags.ContainsKey("U33");

                System.Diagnostics.Debug.WriteLine(
                    $"[CifParser][ANISO]   hasU11={hasU11} hasU22={hasU22} hasU33={hasU33} " +
                    $"hasLabel={anisoFlags.ContainsKey("label")}");

                if (localHasAnisoFlags && hasU11 && hasU22 && hasU33)
                {
                    var tempU = new Dictionary<string, AnisoData>();
                    int atomCount = 0;

                    for (int k = j; k < lines.Length; k++)
                    {
                        string trimmed = lines[k].Trim();

                        if (string.IsNullOrEmpty(trimmed)) continue;
                        if (trimmed.StartsWith("loop_")) break;
                        if (trimmed.StartsWith("_")) break;
                        if (trimmed.StartsWith("#")) break;

                        var parts = SplitLine(trimmed);

                        System.Diagnostics.Debug.WriteLine(
                            $"[CifParser][ANISO]   DATA line {k}: parts={parts.Length} " +
                            $"[{string.Join("|", parts)}]");

                        if (!anisoFlags.ContainsKey("label") || parts.Length <= anisoFlags["label"])
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[CifParser][ANISO]     ⚠️ skip: no label or too few parts");
                            continue;
                        }

                        string label = parts[anisoFlags["label"]]
                            .Replace("'", "").Replace("\"", "").Trim();

                        var data = new AnisoData { label = label };

 // ⭐ Lokalne funkcje — without ref, safe
                        float GetVal(string baseKey)
                        {
                            if (anisoFlags.TryGetValue(baseKey, out int idx) && parts.Length > idx)
                                return GetValue(parts[idx]);

 // fallback: "U11" → "U_11"
                            string altKey = baseKey.Insert(1, "_");
                            if (anisoFlags.TryGetValue(altKey, out int idx2) && parts.Length > idx2)
                                return GetValue(parts[idx2]);

                            return 0f;
                        }
                        float GetErr(string baseKey)
                        {
                            if (anisoFlags.TryGetValue(baseKey, out int idx) && parts.Length > idx)
                                return GetError(parts[idx]);

                            string altKey = baseKey.Insert(1, "_");
                            if (anisoFlags.TryGetValue(altKey, out int idx2) && parts.Length > idx2)
                                return GetError(parts[idx2]);

                            return 0f;
                        }

                        data.u11 = GetVal("U11"); data.u11_e = GetErr("U11");
                        data.u22 = GetVal("U22"); data.u22_e = GetErr("U22");
                        data.u33 = GetVal("U33"); data.u33_e = GetErr("U33");
                        data.u12 = GetVal("U12"); data.u12_e = GetErr("U12");
                        data.u13 = GetVal("U13"); data.u13_e = GetErr("U13");
                        data.u23 = GetVal("U23"); data.u23_e = GetErr("U23");

                        tempU[label] = data;
                        atomCount++;

                        System.Diagnostics.Debug.WriteLine(
                            $"[CifParser][ANISO]     ✓ '{label}': " +
                            $"U11={data.u11:F5} U22={data.u22:F5} U33={data.u33:F5} " +
                            $"U12={data.u12:F5} U13={data.u13:F5} U23={data.u23:F5}");
                    }

                    System.Diagnostics.Debug.WriteLine(
                        $"[CifParser][ANISO] ── Parsed {atomCount} aniso entries, " +
                        $"assigning to {cif.atom.Count} atoms...");

                    AssignAnisotropicDataToAtoms(cif, tempU);

                    hasAnisoFlags = true;
                    return;   // ⭐ sparsowano — koniec
                }
            }

            System.Diagnostics.Debug.WriteLine(
                "[CifParser][ANISO] ❌ Nie znaleziono bloku anizotropowego!");
        }

 /// <summary>
 /// ⭐ Normalisee klucz anizotroafterwy: usuwa afterdkreślnik between U a cyframi.
 /// Przykłady: "U_11" → "U11", "U11" → "U11", "U_12" → "U12"
 /// </summary>
        private static string NormalizeAnisoKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;

 // U_11 → U11
            if (key.Length >= 3 && key[0] == 'U' && key[1] == '_')
                return "U" + key.Substring(2);

 // U11 → U11 (without changes)
            return key;
        }

 /// <summary>
 /// Assigns anisotropic displacement data to the corresafternding atoms.
 /// ⭐ FIX: dopasowanie exact + case-insensitive fallback, full logs.
 /// </summary>
        private static void AssignAnisotropicDataToAtoms(CifData cif, Dictionary<string, AnisoData> tempU)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[CifParser][ANISO-ASSIGN] tempU keys ({tempU.Count}): " +
                $"[{string.Join(", ", tempU.Keys)}]");
            System.Diagnostics.Debug.WriteLine(
                $"[CifParser][ANISO-ASSIGN] atom names (first 10): " +
                $"[{string.Join(", ", cif.atom.Take(10).Select(a => a.name))}...]");

            int matched = 0;
            int unmatched = 0;

            foreach (var atom in cif.atom)
            {
 // ⭐ Próbuj najpierw dokładne dopasowanie, then case-insensitive
                AnisoData u = null;

                if (tempU.TryGetValue(atom.name, out u))
                {
 // exact match — ok
                }
                else
                {
 // fallback: dopasowanie without rozróżniania wielkości liter
                    var key = tempU.Keys.FirstOrDefault(k =>
                        string.Equals(k, atom.name, StringComparison.OrdinalIgnoreCase));

                    if (key != null)
                    {
                        u = tempU[key];
                        System.Diagnostics.Debug.WriteLine(
                            $"[CifParser][ANISO-ASSIGN]   case-insensitive match: " +
                            $"'{atom.name}' ↔ '{key}'");
                    }
                }

                if (u == null)
                {
                    unmatched++;
                    System.Diagnostics.Debug.WriteLine(
                        $"[CifParser][ANISO-ASSIGN]   ✗ NO MATCH for atom '{atom.name}'");
                    continue;
                }

                atom.adp_type = "Uani";
                atom.u11 = u.u11;
                atom.u22 = u.u22;
                atom.u33 = u.u33;
                atom.u12 = u.u12;
                atom.u13 = u.u13;
                atom.u23 = u.u23;
                atom.u11_e = u.u11_e;
                atom.u22_e = u.u22_e;
                atom.u33_e = u.u33_e;
                atom.u12_e = u.u12_e;
                atom.u13_e = u.u13_e;
                atom.u23_e = u.u23_e;

                if (atom.iso < 0.0001f)
                {
                    atom.iso = (u.u11 + u.u22 + u.u33) / 3.0f;
                }

                matched++;
                System.Diagnostics.Debug.WriteLine(
                    $"[CifParser][ANISO-ASSIGN]   ✓ '{atom.name}' ← Uani " +
                    $"(Ueq={atom.iso:F5})");
            }

            System.Diagnostics.Debug.WriteLine(
                $"[CifParser][ANISO-ASSIGN] ── matched={matched}, unmatched={unmatched}");
        }

 /// <summary>
 /// Parses single-line anisotropic data (legacy format without loop_).
 /// ⭐ FIX: aftermija, if Uani already setione z loop_ (ochrona before nadpisaniem).
 /// </summary>
        private static void ParseSingleLineAnisotropic(string[] lines, CifData cif)
        {
 // ⭐ NIE parsuj, if already mamy Uani z loop_
            if (cif.atom.Any(a => a.adp_type == "Uani"))
            {
                System.Diagnostics.Debug.WriteLine(
                    "[CifParser][ANISO-SINGLE] Skipping — Uani already set from loop_");
                return;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#")) continue;

                if (line.StartsWith("_atom_site_aniso_") && !line.StartsWith("_atom_site_aniso_loop"))
                {
                    var parts = SplitLine(line);
                    if (parts.Length >= 7)
                    {
                        string label = parts[1].Replace("'", "").Replace("\"", "");
                        float u11 = GetValue(parts[2]);
                        float u11_e = GetError(parts[2]);
                        float u22 = GetValue(parts[3]);
                        float u22_e = GetError(parts[3]);
                        float u33 = GetValue(parts[4]);
                        float u33_e = GetError(parts[4]);
                        float u12 = GetValue(parts[5]);
                        float u12_e = GetError(parts[5]);
                        float u13 = GetValue(parts[6]);
                        float u13_e = GetError(parts[6]);
                        float u23 = parts.Length > 7 ? GetValue(parts[7]) : 0;
                        float u23_e = parts.Length > 7 ? GetError(parts[7]) : 0;

                        foreach (var atom in cif.atom)
                        {
                            if (atom.name == label)
                            {
                                atom.adp_type = "Uani";
                                atom.u11 = u11;
                                atom.u22 = u22;
                                atom.u33 = u33;
                                atom.u12 = u12;
                                atom.u13 = u13;
                                atom.u23 = u23;
                                atom.u11_e = u11_e;
                                atom.u22_e = u22_e;
                                atom.u33_e = u33_e;
                                atom.u12_e = u12_e;
                                atom.u13_e = u13_e;
                                atom.u23_e = u23_e;

                                if (atom.iso < 0.0001f)
                                    atom.iso = (u11 + u22 + u33) / 3.0f;

                                break;
                            }
                        }
                    }
                }
            }
        }

 /// <summary>
 /// Sets default isotropic values for atoms that don't have them.
 /// </summary>
        private static void SetDefaultIsotropicValues(CifData cif)
        {
            if (cif.atom.Count > 0)
            {
                foreach (var atom in cif.atom)
                {
                    if (string.IsNullOrEmpty(atom.adp_type))
                        atom.adp_type = "Uiso";

                    if (atom.adp_type == "Uiso" && atom.iso < 0.0001f)
                    {
                        atom.iso = 0.05f;
                    }
                }
            }
        }

 /// <summary>
 /// Assigns atomic numbers from atom names if not already set.
 /// </summary>
        private static void AssignAtomicNumbersFromNames(CifData cif)
        {
            foreach (var atom in cif.atom)
            {
                if (atom.type == 0 && !string.IsNullOrEmpty(atom.name))
                    atom.type = GetAtomicNumberFromName(atom.name);
            }
        }

 /// <summary>
 /// Logs a summary of the parsing results.
 /// </summary>
        private static void LogParsingSummary(CifData cif)
        {
            System.Diagnostics.Debug.WriteLine($"[CifParser] ========== PARSING SUMMARY ==========");
            System.Diagnostics.Debug.WriteLine($"[CifParser] File: {cif.FileName}");
            System.Diagnostics.Debug.WriteLine($"[CifParser] Space group: {cif.space_group_name_HM} (#{cif.space_group_IT_number})");
            System.Diagnostics.Debug.WriteLine($"[CifParser] Cell: a={cif.a:F4} b={cif.b:F4} c={cif.c:F4} " +
                $"alp={cif.alp:F3} bet={cif.bet:F3} gam={cif.gam:F3}");
            System.Diagnostics.Debug.WriteLine($"[CifParser] Symmetry ops: {cif.SymmetryOperations.Count}");
            System.Diagnostics.Debug.WriteLine($"[CifParser] Atoms: {cif.atom.Count}");
            System.Diagnostics.Debug.WriteLine($"[CifParser] ANISOTROPIC: {cif.atom.Count(a => a.adp_type == "Uani")}");
            System.Diagnostics.Debug.WriteLine($"[CifParser] ISOTROPIC: {cif.atom.Count(a => a.adp_type == "Uiso")}");
            System.Diagnostics.Debug.WriteLine($"[CifParser] R1(F>4σ)={cif.s_R_factor_gt:F4} R1(all)={cif._refine_ls_R_factor_all:F4} " +
                                               $"wR2={cif.refine_wR2:F4} GooF={cif.refine_goof:F3}");
            System.Diagnostics.Debug.WriteLine($"[CifParser] Reflns: {cif.reflns_gt}/{cif.reflns_total} " +
                                               $"Params: {cif.refine_nparams} Restraints: {cif.refine_nrestraints}");
            System.Diagnostics.Debug.WriteLine($"[CifParser] F(000)={cif.F_000:F1} " +
                                               $"μ={cif.exptl_absorpt_coefficient_mu:F3} mm⁻¹ " +
                                               $"T={cif.measurement_temperature:F1} K");
            System.Diagnostics.Debug.WriteLine($"[CifParser] ======================================");
        }

        #endregion

        #region Atom Parsing

 /// <summary>
 /// Parses a single atom line from the atom_site loop.
 /// </summary>
        private static Atom ParseAtomLine(string line, Dictionary<string, int> flags)
        {
            var parts = SplitLine(line);
            if (parts.Length == 0) return null;

            var atom = new Atom();

            if (flags.ContainsKey("label") && parts.Length > flags["label"])
                atom.name = parts[flags["label"]].Replace("'", "").Replace("\"", "");

            if (flags.ContainsKey("type_symbol") && parts.Length > flags["type_symbol"])
                atom.type = GetAtomicNumber(parts[flags["type_symbol"]]);

            if (flags.ContainsKey("fract_x") && parts.Length > flags["fract_x"])
            { atom.x = GetValue(parts[flags["fract_x"]]); atom.x_e = GetError(parts[flags["fract_x"]]); }

            if (flags.ContainsKey("fract_y") && parts.Length > flags["fract_y"])
            { atom.y = GetValue(parts[flags["fract_y"]]); atom.y_e = GetError(parts[flags["fract_y"]]); }

            if (flags.ContainsKey("fract_z") && parts.Length > flags["fract_z"])
            { atom.z = GetValue(parts[flags["fract_z"]]); atom.z_e = GetError(parts[flags["fract_z"]]); }

 // U_iso_or_equiv
            if (flags.ContainsKey("U_iso_or_equiv") && parts.Length > flags["U_iso_or_equiv"])
            {
                string isoValue = parts[flags["U_iso_or_equiv"]];
                if (!string.IsNullOrEmpty(isoValue) && isoValue != "?" && isoValue != "0" && isoValue != ".")
                {
                    atom.iso = GetValue(isoValue);
                    atom.iso_e = GetError(isoValue);
                }
                else
                {
                    atom.iso = 0.0f;
                }
            }
            else
            {
                atom.iso = 0.05f;
            }

 // Occupancy
            if (flags.ContainsKey("occupancy") && parts.Length > flags["occupancy"])
            { atom.occ = GetValue(parts[flags["occupancy"]]); atom.occ_e = GetError(parts[flags["occupancy"]]); }
            else
                atom.occ = 1;

 // ADP type
            if (flags.ContainsKey("adp_type") && parts.Length > flags["adp_type"])
            {
                atom.adp_type = parts[flags["adp_type"]];
            }
            else
            {
                if (atom.iso > 0.0001f)
                    atom.adp_type = "Uiso";
                else
                    atom.adp_type = "Uani";
            }

 // Site symmetry order
            if (flags.ContainsKey("site_symmetry_order") && parts.Length > flags["site_symmetry_order"])
                atom.symmetry_order = (int)GetValue(parts[flags["site_symmetry_order"]]);

 // Calculation flag
            if (flags.ContainsKey("calc_flag") && parts.Length > flags["calc_flag"])
                atom.calc_flag = parts[flags["calc_flag"]];

 // Disorder group
            if (flags.ContainsKey("disorder_group") && parts.Length > flags["disorder_group"])
                atom.disorder_group = (int)GetValue(parts[flags["disorder_group"]]);

            return atom;
        }

        #endregion

        #region Utility Methods

 /// <summary>
 /// Splits a line by whitespace into an array of tokens.
 /// </summary>
        private static string[] SplitLine(string line)
        {
            return Regex.Split(line.Trim(), @"\s+").Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

 /// <summary>
 /// Extracts a numeric value from a CIF line.
 /// </summary>
        private static float GetValue(string line)
        {
            if (string.IsNullOrEmpty(line)) return 0;
            var match = Regex.Match(line, @"-?\d+[.]\d+");
            if (!match.Success)
                match = Regex.Match(line, @"-?\d+");
            if (match.Success)
            {
                float.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out float result);
                return result;
            }
            return 0;
        }

 /// <summary>
 /// Extracts the standard uncertainty from a CIF line (value in parentheses).
 /// </summary>
        private static float GetError(string line)
        {
            if (string.IsNullOrEmpty(line)) return 0;
            var match = Regex.Match(line, @"[(](\d+)[)]");
            if (match.Success)
            {
                int digits = match.Groups[1].Value.Length;
                float mul = (float)Math.Pow(10, digits);
                float val = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                return val / mul;
            }
            return 0;
        }

 /// <summary>
 /// Extracts a string value from a CIF line (removes quotes and semicolons).
 /// </summary>
        private static string GetStringValue(string line)
        {
            if (string.IsNullOrEmpty(line)) return "";

            var parts = line.Split(new char[] { ' ' }, 2);
            if (parts.Length < 2) return "";

            string result = parts[1].Trim().Replace("'", "").Replace("\"", "");

            if (result.StartsWith(";")) result = result.Substring(1).Trim();
            if (result.EndsWith(";")) result = result.Substring(0, result.Length - 1).Trim();

            return result;
        }

 /// <summary>
 /// Gets the atomic number from an element symbol.
 /// </summary>
        private static int GetAtomicNumber(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return 6;

            var clean = symbol.Trim().Replace("'", "").Replace("\"", "");

            for (int i = 0; i < _elements.Length; i++)
            {
                if (_elements[i] == clean)
                    return i + 1;
            }

            if (clean.Length >= 2)
            {
                string firstTwo = clean.Substring(0, 2);
                for (int i = 0; i < _elements.Length; i++)
                {
                    if (_elements[i] == firstTwo)
                        return i + 1;
                }
            }

            if (clean.Length >= 1)
            {
                string firstOne = clean.Substring(0, 1);
                for (int i = 0; i < _elements.Length; i++)
                {
                    if (_elements[i] == firstOne)
                        return i + 1;
                }
            }

            return 6;
        }

 /// <summary>
 /// Gets the atomic number from an atom label (e.g., "C1" → 6).
 /// </summary>
        private static int GetAtomicNumberFromName(string name)
        {
            if (string.IsNullOrEmpty(name)) return 6;

            string clean = Regex.Replace(name, @"[\d\-_]", "");
            if (string.IsNullOrEmpty(clean)) return 6;

            if (clean.Length >= 2)
            {
                string firstTwo = clean.Substring(0, 2);
                for (int i = 0; i < _elements.Length; i++)
                {
                    if (_elements[i] == firstTwo)
                        return i + 1;
                }
            }

            string firstOne = clean.Substring(0, 1);
            for (int i = 0; i < _elements.Length; i++)
            {
                if (_elements[i] == firstOne)
                    return i + 1;
            }

            return 6;
        }

        #endregion

        #region Helper Classes

 /// <summary>
 /// Helper class for storing anisotropic displacement data with uncertainties.
 /// </summary>
        private class AnisoData
        {
            public string label;
            public float u11, u22, u33, u12, u13, u23;
            public float u11_e, u22_e, u33_e, u12_e, u13_e, u23_e;
        }

        #endregion
    }
}