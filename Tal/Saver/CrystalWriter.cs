// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure / image exafterrt: CrystalWriter.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Crystal17 .d12 input generator.
// Produces a CRYSTAL17 geometry + SCF input deck from StructureData.
//
// ⚠️ Crystal17 requires the STANDARD setting of the space group.
// Non-standard orientations (e.g. P 21/n instead of P 21/c) are
// reafterrted via CrystalSpaceGroupValidator and may be rejected by
// CRYSTAL. We emit a warning and still write the file.
//
// ⚠️ Transformation to standard setting is NOT implemented here —
// use PLATON or a similar tool to transform coordinates first.

using System.Globalization;
using System.Text;
using Tal;
namespace Tal.Saver
{
    public static class CrystalWriter
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

 // ═══════════════════════════════════════════════════════════
 // SCF / DFT defaults (one can extend przez Flags later)
 // ═══════════════════════════════════════════════════════════
        private const string DEFAULT_BASISSET = "POB-TZVP";
        private const string DEFAULT_HAMILTONIAN = "B3LYP-D3";
        private const string DEFAULT_DFTGRID = "XLGRID";
        private const int DEFAULT_SHRINK1 = 8;
        private const int DEFAULT_SHRINK2 = 8;
        private const int DEFAULT_LEVSHIFT1 = 6;
        private const double DEFAULT_LEVSHIFT2 = 1.0;
        private const string DEFAULT_TOLINTEG = "7 7 7 7 25";
        private const int DEFAULT_BIPOSIZE = 60000000;
        private const int DEFAULT_EXCHSIZE = 40000000;

 /// <summary>
 /// Zapisuje plik .d12 for Crystal17.
 ///
 /// ⭐ If space group jest w niestandardowej orientacji
 /// (np. P21/n instead of P21/c) — asks the user whether to perform
 /// transformation do standardowego settings.
 /// </summary>
 /// <param name="structure">Struktura do zapisania (StructureData).</param>
 /// <param name="filePath">Destination path.</param>
 /// <param name="warnings">List of warnings (np. o transformation).</param>
 /// <returns>true if zapis succeeded.</returns>
        public static bool Write(StructureData structure, string filePath,
                                  out List<string> warnings)
        {
            warnings = new List<string>();

            if (structure == null || structure.Atoms == null || structure.Atoms.Count == 0)
                return false;

            StructureData toWrite = structure;

 // ═══════════════════════════════════════════════════════════
 // ⭐ WYKRYCIE NIESTANDARDOWEJ GROUP I TRANSFORMATION
 // ═══════════════════════════════════════════════════════════
            var transform = SpaceGroupTransformer.GetTransformToStandard(structure);

            if (transform.HasValue)
            {
                int group = structure.SpaceGroup;
                if (group < 1)
                {
                    string normalized = SymmetryElements.NormalizeSpaceGroupName(
                        structure.SpaceGroupName ?? "");
                    if (SymmetryElements.GroupNameToNumber.TryGetValue(normalized, out int num))
                        group = num;
                }

                string sourceName = structure.SpaceGroupName ?? "?";
                string standardName = SpaceGroupTransformer.GetStandardName(group);

                var dialogResult = System.Windows.Forms.MessageBox.Show(
                    $"⚠️ Non-standard space-group setting detected.\n\n" +
                    $"Structure:  {sourceName}  (group {group})\n" +
                    $"Standard:   {standardName}\n\n" +
                    $"CRYSTAL17 requires the standard setting of the space group.\n\n" +
                    $"Transform automatically before saving?\n\n" +
                    $"YES    — transform and save\n" +
                    $"NO     — save without transformation (may fail in CRYSTAL)\n" +
                    $"CANCEL — abort export",
                    "CRYSTAL17 export — non-standard setting",
                    System.Windows.Forms.MessageBoxButtons.YesNoCancel,
                    System.Windows.Forms.MessageBoxIcon.Warning);

                if (dialogResult == System.Windows.Forms.DialogResult.Cancel)
                    return false;

                if (dialogResult == System.Windows.Forms.DialogResult.Yes)
                {
 // ⭐ Kopia — nie modyfikujemy original w StructureStorage
                    toWrite = CloneStructure(structure);
                    SpaceGroupTransformer.ApplyTransform(toWrite, transform.Value);
                    toWrite.SpaceGroupName = standardName;

                    warnings.Add(
                        $"✅ Structure transformed to standard setting: " +
                        $"{sourceName} → {standardName}");

                    System.Diagnostics.Debug.WriteLine(
                        $"[CrystalWriter] Applied transform: {transform.Value}");
                }
            }

 // ═══════════════════════════════════════════════════════════
 // BUDOWA PLIKU .d12
 // ═══════════════════════════════════════════════════════════
            var sb = new System.Text.StringBuilder();

 // ── Title ──
            sb.AppendLine(BuildTitle(toWrite));

 // ── Periodicity ──
            sb.AppendLine("CRYSTAL");

 // ── Geometry input flag ──
            sb.AppendLine("0 0 0");

 // ── Space group number ──
            int finalGroup = toWrite.SpaceGroup;
            if (finalGroup < 1)
            {
                string normalized = SymmetryElements.NormalizeSpaceGroupName(
                    toWrite.SpaceGroupName ?? "");
                if (SymmetryElements.GroupNameToNumber.TryGetValue(normalized, out int num))
                    finalGroup = num;
            }
            if (finalGroup < 1) finalGroup = 1;
            sb.AppendLine(finalGroup.ToString(System.Globalization.CultureInfo.InvariantCulture));

 // ── Cell parameters ──
            sb.AppendLine(BuildCellLine(toWrite));

 // ── Number of atoms ──
            sb.AppendLine(toWrite.Atoms.Count.ToString(
                System.Globalization.CultureInfo.InvariantCulture));

 // ── Atoms ──
            foreach (var atom in toWrite.Atoms)
            {
                sb.AppendLine(BuildAtomLine(atom));
            }

 // ── SCF / DFT settings ──
            sb.AppendLine(BuildSettingsBlock());

 // Save
            File.WriteAllText(filePath, sb.ToString(), System.Text.Encoding.UTF8);

            System.Diagnostics.Debug.WriteLine(
                $"[CrystalWriter] Saved .d12: {filePath} " +
                $"({toWrite.Atoms.Count} atoms, group {finalGroup})");

            return true;
        }

 /// <summary>
 /// Deep copy StructureData — so that the transformation does not modify
 /// oryginalnej structure w StructureStorage.
 /// </summary>
        private static StructureData CloneStructure(StructureData src)
        {
            var copy = new StructureData
            {
                Name = src.Name,
                FilePath = src.FilePath,
                SourceFileName = src.SourceFileName,
                IsLoaded = src.IsLoaded,
                IsVisible = src.IsVisible,
                Color = src.Color,

 // Cell
                a = src.a,
                b = src.b,
                c = src.c,
                alpha = src.alpha,
                beta = src.beta,
                gamma = src.gamma,
                Volume = src.Volume,
                a_Error = src.a_Error,
                b_Error = src.b_Error,
                c_Error = src.c_Error,
                alpha_Error = src.alpha_Error,
                beta_Error = src.beta_Error,
                gamma_Error = src.gamma_Error,

 // Symmetry
                SpaceGroup = src.SpaceGroup,
                SpaceGroupName = src.SpaceGroupName,

 // Metadata
                ChemicalName = src.ChemicalName,
                Formula = src.Formula,
                FormulaWeight = src.FormulaWeight,
                Temperature = src.Temperature,
                RFactor = src.RFactor,

 // Refinement (optional — if filled)
                R1_Observed = src.R1_Observed,
                R1_All = src.R1_All,
                wR2_All = src.wR2_All,
                wR2_Observed = src.wR2_Observed,
                GooF = src.GooF,

 // Experiment (optional)
                F000 = src.F000,
                Wavelength = src.Wavelength,
                RadiationType = src.RadiationType,
                MeasurementDevice = src.MeasurementDevice,
                CrystalColour = src.CrystalColour,
                CrystalDescription = src.CrystalDescription,
            };

 // ⭐ Symmetry operations
            copy.SymmetryOperations = src.SymmetryOperations != null
                ? new List<string>(src.SymmetryOperations)
                : new List<string>();

 // ⭐ Atomy (deep copy)
            foreach (var a in src.Atoms)
            {
                copy.Atoms.Add(new SimpleAtom
                {
                    Name = a.Name,
                    Symbol = a.Symbol,
                    AtomicNumber = a.AtomicNumber,

                    X = a.X,
                    Y = a.Y,
                    Z = a.Z,
                    X_Error = a.X_Error,
                    Y_Error = a.Y_Error,
                    Z_Error = a.Z_Error,

                    Occupancy = a.Occupancy,
                    iso = a.iso,
                    AdpType = a.AdpType,
                    DisorderGroup = a.DisorderGroup,

                    U11 = a.U11,
                    U22 = a.U22,
                    U33 = a.U33,
                    U12 = a.U12,
                    U13 = a.U13,
                    U23 = a.U23,
                });
            }

            return copy;
        }

 // ═══════════════════════════════════════════════════════════
 // Helpers
 // ═══════════════════════════════════════════════════════════

        private static string BuildTitle(StructureData structure)
        {
            string name = structure.Name ?? "structure";
            string chemical = structure.ChemicalName ?? "";
            return $"{name} {chemical}  Generated by TAL".Trim();
        }

        private static string BuildCellLine(StructureData structure)
        {
 // ⚠️ Crystal17 actually requires the FULL set:
 // a b c alpha beta gamma
 // (also tolerates the short form, ale the full one is safer).
            double a = structure.a;
            double b = structure.b;
            double c = structure.c;
            double al = structure.alpha;
            double be = structure.beta;
            double ga = structure.gamma;

            return string.Format(Inv,
                "{0:0.00000} {1:0.00000} {2:0.00000} {3:0.0000} {4:0.0000} {5:0.0000}",
                a, b, c, al, be, ga);
        }

        private static string BuildAtomLine(SimpleAtom atom)
        {
 // Crystal17 format: Z x y z [label]
            int z = atom.AtomicNumber;
            if (z < 1) z = 6;   // fallback C

            string label = string.IsNullOrEmpty(atom.Name) ? "" : $" {atom.Name}";

            return string.Format(Inv,
                "{0}   {1:0.000000}   {2:0.000000}   {3:0.000000}{4}",
                z, atom.X, atom.Y, atom.Z, label);
        }

        private static string BuildSettingsBlock()
        {
            var sb = new StringBuilder();

            sb.AppendLine("OPTGEOM");
            sb.AppendLine("FULLOPTG");
            sb.AppendLine("ENDOPT");
            sb.AppendLine("BASISSET");
            sb.AppendLine(DEFAULT_BASISSET);
            sb.AppendLine("DFT");
            sb.AppendLine(DEFAULT_HAMILTONIAN);
            sb.AppendLine(DEFAULT_DFTGRID);
            sb.AppendLine("END");
            sb.AppendLine("SCFDIR");
            sb.AppendLine("BIPOSIZE");
            sb.AppendLine(DEFAULT_BIPOSIZE.ToString(Inv));
            sb.AppendLine("EXCHSIZE");
            sb.AppendLine(DEFAULT_EXCHSIZE.ToString(Inv));
            sb.AppendLine("SHRINK");
            sb.AppendLine($"{DEFAULT_SHRINK1} {DEFAULT_SHRINK2}");
            sb.AppendLine("LEVSHIFT");
            sb.AppendLine($"{DEFAULT_LEVSHIFT1} {DEFAULT_LEVSHIFT2.ToString("0.0", Inv)}");
            sb.AppendLine("TOLINTEG");
            sb.AppendLine(DEFAULT_TOLINTEG);
            sb.AppendLine("END");

            return sb.ToString();
        }
    }
}