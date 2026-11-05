// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Loaded structure collection / storage.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// EXTENDED: full metadata mapping CifData/ResData -> StructureData

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Tal.Parsers;

namespace Tal
{
 /// <summary>
 /// Singleton storage manager for crystallographic structure data.
 /// Handles loading, storing, and managing multiple structures from various file formats
 /// (CIF, RES/INS, XYZ). Implements INotifyPropertyChanged for UI updates.
 /// </summary>
    public class StructureStorage : INotifyPropertyChanged
    {
        #region Private Fields

        private static StructureStorage _instance;
        private StructureData _selectedStructure;
        private string _currentFilePath = "";

        #endregion

        #region Events

 /// <summary>Event raised when a property value changes.</summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Singleton

 /// <summary>Gets the singleton instance of the StructureStorage.</summary>
        public static StructureStorage Instance => _instance ??= new StructureStorage();

        #endregion

        #region Constructor

        private StructureStorage() { }

        #endregion

        #region Public Properties

 /// <summary>Collection of all loaded structures.</summary>
        public ObservableCollection<StructureData> Structures { get; } = new ObservableCollection<StructureData>();

 /// <summary>Dictionary mapping file paths to lists of structures.</summary>
        public Dictionary<string, List<StructureData>> StructuresByFile { get; } = new Dictionary<string, List<StructureData>>();

 /// <summary>Currently selected structure.</summary>
        public StructureData SelectedStructure
        {
            get => _selectedStructure;
            set
            {
                if (_selectedStructure != value)
                {
                    _selectedStructure = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedStructure));
                }
            }
        }

 /// <summary>Indicates whether a structure is currently selected.</summary>
        public bool HasSelectedStructure => SelectedStructure != null;

 /// <summary>Current file path being processed.</summary>
        public string CurrentFilePath => _currentFilePath;

 /// <summary>Total number of structures.</summary>
        public int StructureCount => Structures.Count;

        #endregion

        #region Public Methods

 /// <summary>
 /// Loads a file and adds its structures to the storage.
 /// For CIF files with multiple 'data_' blocks, each block is parsed separately.
 /// </summary>
 /// <param name="filePath">Path to the file to load.</param>
 /// <returns>True if loading was successful.</returns>
        public bool LoadFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[StructureStorage] File does not exist: {filePath}");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] Loading file: {filePath}");

                string content = File.ReadAllText(filePath);
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                string fileName = Path.GetFileName(filePath);
                _currentFilePath = filePath;

                List<StructureData> structures;

                switch (ext)
                {
                    case ".cif":
                        structures = LoadCif(content, filePath);
                        break;

                    case ".res":
                    case ".ins":
                        structures = LoadRes(content, filePath);
                        break;

                    case ".xyz":
                        structures = LoadXyz(content, filePath);
                        break;
                    case ".pdb":
                    case ".ent":
                        structures = LoadPdb(content, filePath);
                        break;
                    case ".mol":
                    case ".sdf":
                    case ".sd":
                        structures = LoadMol(content, filePath);
                        break;
                    case ".mol2":
                    case ".ml2":
                        structures = LoadMol2(content, filePath);
                        break;
                    case ".gjf":
                    case ".com":
                    case ".gau":
                        structures = LoadGaussian(content, filePath);
                        break;

                    default:
                        System.Diagnostics.Debug.WriteLine(
                            $"[StructureStorage] Unsupported format: {ext}");
                        return false;
                }

                if (structures == null || structures.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[StructureStorage] No structures found in file: {fileName}");
                    return false;
                }

 // Add all structures to storage
                foreach (var structure in structures)
                {
                    structure.SourceFileName = fileName;
                    structure.FilePath = filePath;
                    structure.IsLoaded = true;

                    Structures.Add(structure);

                    if (!StructuresByFile.ContainsKey(filePath))
                        StructuresByFile[filePath] = new List<StructureData>();

                    StructuresByFile[filePath].Add(structure);
                }

                if (SelectedStructure == null && Structures.Count > 0)
                    SelectedStructure = Structures[0];

                OnPropertyChanged(nameof(Structures));

                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] ✅ Added {structures.Count} structures from {fileName}");

                for (int i = 0; i < structures.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[StructureStorage]   [{i}] {structures[i].Name} " +
                        $"({structures[i].Atoms.Count} atoms, " +
                        $"SG #{structures[i].SpaceGroup})");
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] STACK: {ex.StackTrace}");
                return false;
            }
        }

 /// <summary>
 /// Gets structures from a specific file by file path (key in StructuresByFile).
 /// </summary>
        public List<StructureData> GetStructuresFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return new List<StructureData>();

 // 1) Try by full path (dictionary key)
            if (StructuresByFile.TryGetValue(filePath, out var structures))
                return structures;

 // 2) Fallback — by file name (SourceFileName)
            return Structures
                .Where(s => string.Equals(s.SourceFileName, filePath,
                                           StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

 /// <summary>
 /// Gets structures from a file identified by its SourceFileName.
 /// </summary>
        public List<StructureData> GetStructuresBySourceFileName(string sourceFileName)
        {
            if (string.IsNullOrEmpty(sourceFileName))
                return new List<StructureData>();

            return Structures
                .Where(s => string.Equals(s.SourceFileName, sourceFileName,
                                           StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

 /// <summary>
 /// Removes all structures associated with a file.
 /// </summary>
        public bool RemoveFile(string filePath)
        {
            if (!StructuresByFile.TryGetValue(filePath, out var toRemove))
                return false;

            foreach (var structure in toRemove)
                Structures.Remove(structure);

            StructuresByFile.Remove(filePath);

            if (SelectedStructure != null && !Structures.Contains(SelectedStructure))
                SelectedStructure = Structures.Count > 0 ? Structures[0] : null;

            OnPropertyChanged(nameof(Structures));
            return true;
        }

 /// <summary>
 /// Clears all structures from storage.
 /// </summary>
        public void Clear()
        {
            Structures.Clear();
            StructuresByFile.Clear();
            SelectedStructure = null;
            _currentFilePath = "";
            OnPropertyChanged(nameof(Structures));
        }

        #endregion

        #region File Loading Methods

 /// <summary>
 /// Loads structures from a CIF file.
 /// Splits the file into 'data_...' blocks via CifDivider.Divide(),
 /// then parses each block separately with CifParser.Parse().
 /// </summary>
        private List<StructureData> LoadCif(string content, string filePath)
        {
            var result = new List<StructureData>();

            if (string.IsNullOrEmpty(content))
                return result;

 // Step 1: Split file into 'data_...' blocks
            var blocks = CifDivider.Divide(content, filePath);

            System.Diagnostics.Debug.WriteLine(
                $"[StructureStorage] CIF: found {blocks.Count} blocks in " +
                $"{Path.GetFileName(filePath)}");

            if (blocks.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[StructureStorage] CIF: no blocks to parse");
                return result;
            }

 // Step 2: Parse each block separately
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];

                try
                {
                    var cifData = new CifData();
                    cifData.FileName = block.Name;

                    if (!CifParser.Parse(block.RawData, cifData) || !cifData.completed)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[StructureStorage]   ✗ Block [{i}] '{block.Name}': " +
                            $"parsing failed");
                        continue;
                    }

                    var structure = ConvertCifToStructure(cifData, filePath);

                    if (structure == null)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[StructureStorage]   ✗ Block [{i}] '{block.Name}': " +
                            $"conversion failed");
                        continue;
                    }

                    if (structure.Atoms.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[StructureStorage]   ⚠ Block [{i}] '{block.Name}': " +
                            $"0 atoms — skipped");
                        continue;
                    }

 // Structure name — priority for 'data_...' block name
                    if (!string.IsNullOrEmpty(block.Name))
                        structure.Name = block.Name;
                    else
                        structure.Name = $"Structure_{result.Count + 1}";

                    result.Add(structure);

                    System.Diagnostics.Debug.WriteLine(
                        $"[StructureStorage]   ✓ Block [{i}] '{structure.Name}': " +
                        $"{structure.Atoms.Count} atoms, " +
                        $"group #{structure.SpaceGroup} ({structure.SpaceGroupName})");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[StructureStorage]   ✗ Error parsing block [{i}] " +
                        $"'{block.Name}': {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"[StructureStorage] CIF: parsed {result.Count}/{blocks.Count} blocks");

            return result;
        }

 /// <summary>
 /// Loads structures from a RES/INS file.
 /// </summary>
        private List<StructureData> LoadRes(string content, string filePath)
        {
            var result = new List<StructureData>();

            var resData = new ResData();
            resData.FileName = Path.GetFileName(filePath);

            if (!ResParser.Parse(content, resData) || !resData.completed)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] RES: parsing failed for " +
                    $"{Path.GetFileName(filePath)}");
                return result;
            }

            var structure = ConvertResToStructure(resData, filePath);
            if (structure != null && structure.Atoms.Count > 0)
            {
                result.Add(structure);
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] Added RES structure: {structure.Name} " +
                    $"({structure.Atoms.Count} atoms)");
            }

            return result;
        }

        private List<StructureData> LoadMol(string content, string filePath)
        {
            var result = new List<StructureData>();

            var molData = new MolData();
            molData.FileName = Path.GetFileName(filePath);

            if (!MolParser.Parse(content, molData) || !molData.completed)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] MOL: parsing failed for {Path.GetFileName(filePath)}");
                return result;
            }

            var structure = MolParser.ConvertToStructure(molData, filePath);
            if (structure != null && structure.Atoms.Count > 0)
            {
                result.Add(structure);
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] Added MOL structure: {structure.Name} ({structure.Atoms.Count} atoms, {molData.Bonds.Count} bonds)");
            }

            return result;
        }

        private List<StructureData> LoadMol2(string content, string filePath)
        {
            var result = new List<StructureData>();

            var mol2Data = new Mol2Data();
            mol2Data.FileName = Path.GetFileName(filePath);

            if (!Mol2Parser.Parse(content, mol2Data) || !mol2Data.completed)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] MOL2: parsing failed for {Path.GetFileName(filePath)}");
                return result;
            }

            var structure = Mol2Parser.ConvertToStructure(mol2Data, filePath);
            if (structure != null && structure.Atoms.Count > 0)
            {
                result.Add(structure);
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] Added MOL2: {structure.Name} " +
                    $"({structure.Atoms.Count} atoms, {mol2Data.Bonds.Count} bonds" +
                    (mol2Data.hasCrysin ? $", cell from CRYSIN SG#{mol2Data.space_group_IT_number}" : "") + ")");
            }

            return result;
        }

        private List<StructureData> LoadPdb(string content, string filePath)
        {
            var result = new List<StructureData>();

            var pdbData = new PdbData();
            pdbData.FileName = Path.GetFileName(filePath);

            if (!PdbParser.Parse(content, pdbData) || !pdbData.completed)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] PDB: parsing failed for {Path.GetFileName(filePath)}");
                return result;
            }

            var structure = PdbParser.ConvertToStructure(pdbData, filePath);
            if (structure != null && structure.Atoms.Count > 0)
            {
                result.Add(structure);
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] Added PDB structure: {structure.Name} ({structure.Atoms.Count} atoms)");
            }

            return result;
        }

        private List<StructureData> LoadXyz(string content, string filePath)
        {
            var result = new List<StructureData>();

            var xyzData = new XyzData();
            xyzData.FileName = Path.GetFileName(filePath);

            if (!XyzParser.Parse(content, xyzData) || !xyzData.completed)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] XYZ: parsing failed for {Path.GetFileName(filePath)}");
                return result;
            }

            var structure = XyzParser.ConvertToStructure(xyzData, filePath);
            if (structure != null && structure.Atoms.Count > 0)
            {
                result.Add(structure);
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] Added XYZ structure: {structure.Name} ({structure.Atoms.Count} atoms)");
            }

            return result;
        }

        private List<StructureData> LoadGaussian(string content, string filePath)
        {
            var result = new List<StructureData>();

            var gauData = new GaussianData();
            gauData.FileName = Path.GetFileName(filePath);

            if (!GaussianParser.Parse(content, gauData) || !gauData.completed)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] Gaussian: parsing failed for {Path.GetFileName(filePath)}");
                return result;
            }

            var structure = GaussianParser.ConvertToStructure(gauData, filePath);
            if (structure != null && structure.Atoms.Count > 0)
            {
                result.Add(structure);
                System.Diagnostics.Debug.WriteLine(
                    $"[StructureStorage] Added Gaussian: {structure.Name} " +
                    $"({structure.Atoms.Count} atoms, charge={gauData.Charge}, mult={gauData.Multiplicity})");
            }

            return result;
        }

        #endregion

        #region Conversion Methods

 /// <summary>
 /// Converts CifData to StructureData.
 /// Copies all metadata: unit cell, symmetry, atoms, refinement,
 /// reflections, experiment details, and publication metadata.
 /// </summary>
        private StructureData ConvertCifToStructure(CifData cifData, string filePath)
        {
            var structure = new StructureData
            {
                Name = cifData.FileName,
                FilePath = filePath,
                a = cifData.a,
                b = cifData.b,
                c = cifData.c,
                alpha = cifData.alp,
                beta = cifData.bet,
                gamma = cifData.gam,
                a_Error = cifData.a_e,
                b_Error = cifData.b_e,
                c_Error = cifData.c_e,
                alpha_Error = cifData.alp_e,
                beta_Error = cifData.bet_e,
                gamma_Error = cifData.gam_e,
                Volume = cifData.cell_volume,
                SpaceGroup = cifData.space_group_IT_number,
                SpaceGroupName = cifData.space_group_name_HM ?? "",
                ChemicalName = cifData.chemical_name_common ?? "",
                Formula = cifData.formula_sum ?? "",
                FormulaWeight = cifData.formula_weight,
                Temperature = cifData.measurement_temperature,
                RFactor = cifData.s_R_factor_gt
            };

 // ⭐ Copy symmetry operations
            if (cifData.SymmetryOperations != null)
                structure.SymmetryOperations.AddRange(cifData.SymmetryOperations);

 // ⭐ Copy atoms
            foreach (var cifAtom in cifData.atom)
            {
                var atom = new SimpleAtom
                {
                    Name = cifAtom.name ?? "Unknown",
                    Symbol = GetElementSymbol(cifAtom.type),
                    AtomicNumber = cifAtom.type,
                    X = cifAtom.x,
                    Y = cifAtom.y,
                    Z = cifAtom.z,
                    X_Error = cifAtom.x_e,
                    Y_Error = cifAtom.y_e,
                    Z_Error = cifAtom.z_e,
                    Occupancy = cifAtom.occ,
                    iso = cifAtom.iso,
                    AdpType = cifAtom.adp_type ?? "Uiso",
                    DisorderGroup = cifAtom.disorder_group,
                    U11 = cifAtom.u11,
                    U22 = cifAtom.u22,
                    U33 = cifAtom.u33,
                    U12 = cifAtom.u12,
                    U13 = cifAtom.u13,
                    U23 = cifAtom.u23
                };
                structure.Atoms.Add(atom);
            }

 // ═══════════════════════════════════════════════════════════
 // ⭐ NEW: Refinement statistics
 // ═══════════════════════════════════════════════════════════
            structure.R1_Observed = cifData.s_R_factor_gt;
            structure.R1_All = cifData._refine_ls_R_factor_all;
            structure.wR2_All = cifData.refine_wR2;
            structure.wR2_Observed = cifData.refine_wR2_gt;
            structure.GooF = cifData.refine_goof;
            structure.RefinementReflections = cifData.refine_nreflns;
            structure.RefinementParameters = cifData.refine_nparams;
            structure.RefinementRestraints = cifData.refine_nrestraints;
            structure.ShiftMax = cifData.refine_shift_max;
            structure.ShiftMean = cifData.refine_shift_mean;
            structure.DiffDensityMax = cifData.refine_diff_max;
            structure.DiffDensityMin = cifData.refine_diff_min;
            structure.DiffDensityRMS = cifData.refine_diff_rms;
            structure.WeightingScheme = cifData.refine_weighting ?? "";

 // ═══════════════════════════════════════════════════════════
 // ⭐ NEW: Reflection statistics
 // ═══════════════════════════════════════════════════════════
            structure.ReflnsTotal = cifData.reflns_total;
            structure.ReflnsObserved = cifData.reflns_gt;
            structure.ReflnsThreshold = cifData.reflns_threshold ?? "";
            structure.ResolutionHigh = cifData.resolution_high;
            structure.ResolutionLow = cifData.resolution_low;
            structure.ThetaMin = cifData.theta_min;
            structure.ThetaMax = cifData.theta_max;

 // ═══════════════════════════════════════════════════════════
 // ⭐ NEW: Experiment
 // ═══════════════════════════════════════════════════════════
            structure.F000 = cifData.F_000;
            structure.Wavelength = cifData.wavelength;
            structure.RadiationType = cifData.radiation_type ?? "";
            structure.MeasurementDevice = cifData.measurement_device ?? "";
            structure.CrystalColour = cifData.crystal_colour ?? "";
            structure.CrystalDescription = cifData.crystal_description ?? "";
            structure.CrystalSizeMax = cifData.crystal_size_max;
            structure.CrystalSizeMid = cifData.crystal_size_mid;
            structure.CrystalSizeMin = cifData.crystal_size_min;
            structure.AbsorptionCorrectionType = cifData.exptl_absorpt_correction_type ?? "";
            structure.AbsCorrectionTMin = cifData.abs_correction_t_min;
            structure.AbsCorrectionTMax = cifData.abs_correction_t_max;
            structure.AbsorptionCoefficient = cifData.exptl_absorpt_coefficient_mu;
            structure.DensityDiffrn = cifData.exptl_crystal_density_diffrn;

 // ═══════════════════════════════════════════════════════════
 // ⭐ NEW: Audit / Publication
 // ═══════════════════════════════════════════════════════════
            structure.AuditCreationDate = cifData.audit_creation_date ?? "";
            structure.AuditCreationMethod = cifData.audit_creation_method ?? "";
            structure.PublTitle = cifData.publ_title ?? "";
            structure.JournalName = cifData.journal_name ?? "";
            structure.JournalReference = $"{cifData.journal_volume} {cifData.journal_page} ({cifData.journal_year})".Trim();
            structure.DOI = cifData.doi ?? "";

            return structure;
        }

 /// <summary>
 /// Converts ResData to StructureData.
 /// Copies all metadata: unit cell, symmetry, atoms, and refinement
 /// statistics parsed from REM lines.
 ///
 /// KEY: Atomic number for each atom is determined from the SYMBOL
 /// elementu (resAtom.element, setionego przez ResParser
 /// na afterdstawie maafterwania SFAC), a NIE z sztywnej tabeli
 /// MapSfacTypeToAtomicNumber. Thanks to this, files with non-standard
 /// SFAC order (e.g. "C H O" instead of "C H N O") map 
 /// correctly.
 /// </summary>
        private StructureData ConvertResToStructure(ResData resData, string filePath)
        {
            var structure = new StructureData
            {
                FilePath = filePath,
                Name = !string.IsNullOrEmpty(resData.TITL)
                    ? resData.TITL
                    : Path.GetFileNameWithoutExtension(filePath),
                RawData = resData.FileName,
                a = resData.a,
                b = resData.b,
                c = resData.c,
                alpha = resData.alp,
                beta = resData.bet,
                gamma = resData.gam,
                a_Error = resData.a_e,
                b_Error = resData.b_e,
                c_Error = resData.c_e,
                alpha_Error = resData.alp_e,
                beta_Error = resData.bet_e,
                gamma_Error = resData.gam_e,
                Volume = resData.cell_volume,
                SpaceGroup = resData.space_group_IT_number,
                SpaceGroupName = resData.space_group_name_HM ?? "",
                Formula = resData.formula_sum ?? "",
                FormulaWeight = resData.formula_weight,
                Temperature = resData.measurement_temperature,
                RFactor = resData.s_R_factor_gt
            };

 // ⭐ Copy symmetry operations
            if (resData.SymmetryOperations != null)
                structure.SymmetryOperations.AddRange(resData.SymmetryOperations);

 // ═══════════════════════════════════════════════════════════════
 // ⭐ Copy atoms — atomic number z SYMBOLU ELEMENTU
 // ═══════════════════════════════════════════════════════════════
            foreach (var resAtom in resData.atom)
            {
 // ── Atomic number ───────────────────────────────────────────
 // Priorytet:
 // 1. resAtom.element (symbol z SFAC) → SymbolToAtomicNumber
 // 2. resAtom.type (numer SFAC) → MapSfacTypeToAtomicNumber (fallback)
                int atomicNumber = 6;   // C — ultimate fallback

                if (!string.IsNullOrEmpty(resAtom.element))
                {
                    atomicNumber = Molecule.SymbolToAtomicNumber(resAtom.element);

                    if (atomicNumber <= 0)
                    {
 // Unknown symbol — use SFAC number as fallback
                        atomicNumber = MapSfacTypeToAtomicNumber(resAtom.type);
                    }
                }
                else
                {
 // No element (old RES without SFAC) — use SFAC number
                    atomicNumber = MapSfacTypeToAtomicNumber(resAtom.type);
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[ConvertResToStructure] '{resAtom.name}' " +
                    $"sfacType={resAtom.type} element='{resAtom.element}' " +
                    $"→ atomicNumber={atomicNumber}");

 // ── Create SimpleAtom ───────────────────────────────────────
                var atom = new SimpleAtom
                {
                    Name = resAtom.name ?? "Unknown",
                    Symbol = GetElementSymbol(atomicNumber),
                    AtomicNumber = atomicNumber,
                    X = resAtom.x,
                    Y = resAtom.y,
                    Z = resAtom.z,
                    X_Error = resAtom.x_e,
                    Y_Error = resAtom.y_e,
                    Z_Error = resAtom.z_e,
                    Occupancy = resAtom.occ,
                    iso = resAtom.iso,
                    AdpType = resAtom.adp_type ?? "Uiso",
                    DisorderGroup = resAtom.part_number,
                    U11 = resAtom.u11,
                    U22 = resAtom.u22,
                    U33 = resAtom.u33,
                    U12 = resAtom.u12,
                    U13 = resAtom.u13,
                    U23 = resAtom.u23
                };

                structure.Atoms.Add(atom);
            }

 // ═══════════════════════════════════════════════════════════════
 // ⭐ Refinement statistics (from REM lines)
 // ═══════════════════════════════════════════════════════════════
            structure.R1_Observed = resData.s_R_factor_gt;
            structure.R1_All = resData._refine_ls_R_factor_all;
            structure.wR2_All = resData.wR2;
            structure.wR2_Observed = resData.wR2;   // RES does not distinguish — reuse wR2
            structure.GooF = resData.goof;
            structure.RefinementReflections = resData.refine_nreflns_all;
            structure.RefinementParameters = resData.refine_nparams;
            structure.RefinementRestraints = resData.refine_nrestraints;
            structure.ShiftMax = resData.refine_shift_max;
            structure.ShiftMean = resData.refine_shift_mean;
            structure.DiffDensityMax = resData.refine_diff_max;
            structure.DiffDensityMin = resData.refine_diff_min;
            structure.DiffDensityRMS = 0f;   // not available in RES

 // ═══════════════════════════════════════════════════════════════
 // ⭐ Experiment
 // ═══════════════════════════════════════════════════════════════
            structure.F000 = resData.F_000;
            structure.Wavelength = resData.wavelength;

            return structure;
        }

        #endregion

        #region Helper Methods

 /// <summary>
 /// Maps SFAC type to atomic number.
 /// </summary>
        private int MapSfacTypeToAtomicNumber(int sfacType)
        {
            switch (sfacType)
            {
                case 1: return 6;   // C
                case 2: return 1;   // H
                case 3: return 7;   // N
                case 4: return 8;   // O
                case 5: return 16;  // S
                case 6: return 15;  // P
                case 7: return 9;   // F
                case 8: return 17;  // Cl
                case 9: return 35;  // Br
                case 10: return 53; // I
                default:
                    if (sfacType > 0 && sfacType <= 118)
                        return sfacType;
                    return 6;
            }
        }

 /// <summary>
 /// Gets the element symbol for an atomic number.
 /// </summary>
        private string GetElementSymbol(int atomicNumber)
        {
            string[] elements = {
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
            if (atomicNumber > 0 && atomicNumber <= elements.Length)
                return elements[atomicNumber - 1];
            return "C";
        }

        #endregion

        #region INotifyPropertyChanged

 /// <summary>
 /// Raises the PropertyChanged event.
 /// </summary>
 /// <param name="name">The name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}