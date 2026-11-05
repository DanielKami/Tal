// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure data container.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// EXTENDED: full crystallographic metadata (refinement, reflections, experiment, publication)

using Microsoft.Xna.Framework;

namespace Tal
{
 /// <summary>
 /// Represents a single atom from a CIF file.
 /// Contains aftersitional coordinates, displacement parameters,
 /// and occupancy data for crystallographic structure determination.
 /// </summary>
    public class SimpleAtom
    {
        #region Identification

 /// <summary>Atom label (e.g., "C1", "O2").</summary>
        public string Name { get; set; } = "";

 /// <summary>Element symbol (e.g., "C", "O").</summary>
        public string Symbol { get; set; } = "";

 /// <summary>Atomic number of the element.</summary>
        public int AtomicNumber { get; set; }

        #endregion

        #region Positional Coordinates (Fractional)

 /// <summary>Fractional coordinate X.</summary>
        public float X { get; set; }

 /// <summary>Fractional coordinate Y.</summary>
        public float Y { get; set; }

 /// <summary>Fractional coordinate Z.</summary>
        public float Z { get; set; }

 /// <summary>Standard uncertainty for coordinate X.</summary>
        public float X_Error { get; set; } = 0f;

 /// <summary>Standard uncertainty for coordinate Y.</summary>
        public float Y_Error { get; set; } = 0f;

 /// <summary>Standard uncertainty for coordinate Z.</summary>
        public float Z_Error { get; set; } = 0f;

        #endregion

        #region Occupancy and Displacement

 /// <summary>Atom site occupancy factor.</summary>
        public float Occupancy { get; set; } = 1.0f;

 /// <summary>Isotropic atomic displacement parameter.</summary>
        public float iso { get; set; } = 0.05f;

 /// <summary>Type of ADP: "Uiso" or "Uani".</summary>
        public string AdpType { get; set; } = "Uiso";

        #endregion

        #region Anisotropic Displacement Parameters

 /// <summary>Anisotropic displacement parameter U11.</summary>
        public float U11 { get; set; }

 /// <summary>Anisotropic displacement parameter U22.</summary>
        public float U22 { get; set; }

 /// <summary>Anisotropic displacement parameter U33.</summary>
        public float U33 { get; set; }

 /// <summary>Anisotropic displacement parameter U12.</summary>
        public float U12 { get; set; }

 /// <summary>Anisotropic displacement parameter U13.</summary>
        public float U13 { get; set; }

 /// <summary>Anisotropic displacement parameter U23.</summary>
        public float U23 { get; set; }

        #endregion

        #region Disorder

 /// <summary>Disorder group number for disordered structures.</summary>
        public int DisorderGroup { get; set; }

        #endregion
    }

 /// <summary>
 /// Represents a complete crystallographic structure from a CIF/RES file.
 /// Contains unit cell parameters, symmetry information, atoms,
 /// refinement statistics, experimental details, and publication metadata.
 /// </summary>
    public class StructureData
    {
        #region Basic Information

 /// <summary>Structure name.</summary>
        public string Name { get; set; } = "Unknown";

 /// <summary>Raw data from the source file.</summary>
        public string RawData { get; set; } = "";

 /// <summary>Indicates whether the structure data is loaded.</summary>
        public bool IsLoaded { get; set; } = false;

 /// <summary>File path to the source file.</summary>
        public string FilePath { get; set; } = "";

 /// <summary>Source file name.</summary>
        public string SourceFileName { get; set; } = "";

 /// <summary>Indicates whether the structure is visible in the 3D view.</summary>
        public bool IsVisible { get; set; } = true;

 /// <summary>Color for the structure (used in UI display).</summary>
        public Vector3 Color { get; set; } = new Vector3(0.6f, 0.6f, 0.8f);

        #endregion

        #region Unit Cell Parameters

 /// <summary>Unit cell parameter a (in Ångströms).</summary>
        public float a { get; set; }

 /// <summary>Unit cell parameter b (in Ångströms).</summary>
        public float b { get; set; }

 /// <summary>Unit cell parameter c (in Ångströms).</summary>
        public float c { get; set; }

 /// <summary>Unit cell angle alpha (in degrees).</summary>
        public float alpha { get; set; }

 /// <summary>Unit cell angle beta (in degrees).</summary>
        public float beta { get; set; }

 /// <summary>Unit cell angle gamma (in degrees).</summary>
        public float gamma { get; set; }

 /// <summary>Unit cell volume (in Ångströms³).</summary>
        public float Volume { get; set; }

 /// <summary>Standard uncertainty for parameter a.</summary>
        public float a_Error { get; set; }

 /// <summary>Standard uncertainty for parameter b.</summary>
        public float b_Error { get; set; }

 /// <summary>Standard uncertainty for parameter c.</summary>
        public float c_Error { get; set; }

 /// <summary>Standard uncertainty for angle alpha.</summary>
        public float alpha_Error { get; set; }

 /// <summary>Standard uncertainty for angle beta.</summary>
        public float beta_Error { get; set; }

 /// <summary>Standard uncertainty for angle gamma.</summary>
        public float gamma_Error { get; set; }

        #endregion

        #region Symmetry Information

 /// <summary>International Tables for Crystallography space group number.</summary>
        public int SpaceGroup { get; set; }

 /// <summary>Hermann-Mauguin space group name.</summary>
        public string SpaceGroupName { get; set; } = "";

 /// <summary>List of symmetry operation strings.</summary>
        public List<string> SymmetryOperations { get; set; } = new List<string>();

        #endregion

        #region Atoms

 /// <summary>List of atoms in the asymmetric unit.</summary>
        public List<SimpleAtom> Atoms { get; set; } = new List<SimpleAtom>();

        #endregion

        #region Metadata

 /// <summary>Chemical name of the comafterund.</summary>
        public string ChemicalName { get; set; } = "";

 /// <summary>Chemical formula sum.</summary>
        public string Formula { get; set; } = "";

 /// <summary>Formula weight (molecular mass).</summary>
        public float FormulaWeight { get; set; }

 /// <summary>Measurement temperature (in Kelvin).</summary>
        public float Temperature { get; set; }

 /// <summary>R factor for the structure.</summary>
        public float RFactor { get; set; }

        #endregion

        #region ⭐ NEW: Refinement / Statistics

 /// <summary>R1 factor for observed data (F > 4σ).</summary>
        public float R1_Observed { get; set; }

 /// <summary>R1 factor for all data.</summary>
        public float R1_All { get; set; }

 /// <summary>wR2 factor for all data.</summary>
        public float wR2_All { get; set; }

 /// <summary>wR2 factor for observed data.</summary>
        public float wR2_Observed { get; set; }

 /// <summary>Goodness of fit (S = GooF).</summary>
        public float GooF { get; set; }

 /// <summary>Number of reflections used in refinement.</summary>
        public int RefinementReflections { get; set; }

 /// <summary>Number of parameters refined.</summary>
        public int RefinementParameters { get; set; }

 /// <summary>Number of restraints applied.</summary>
        public int RefinementRestraints { get; set; }

 /// <summary>Maximum shift/su ratio.</summary>
        public float ShiftMax { get; set; }

 /// <summary>Mean shift/su ratio.</summary>
        public float ShiftMean { get; set; }

 /// <summary>Largest residual electron density peak (e/Å³).</summary>
        public float DiffDensityMax { get; set; }

 /// <summary>Deepest residual electron density hole (e/Å³).</summary>
        public float DiffDensityMin { get; set; }

 /// <summary>RMS residual electron density (e/Å³).</summary>
        public float DiffDensityRMS { get; set; }

 /// <summary>Weighting scheme details.</summary>
        public string WeightingScheme { get; set; } = "";

        #endregion

        #region ⭐ NEW: Reflection Statistics

 /// <summary>Total reflections measured.</summary>
        public int ReflnsTotal { get; set; }

 /// <summary>Reflections with I > 2σ(I).</summary>
        public int ReflnsObserved { get; set; }

 /// <summary>Resolution threshold expression (e.g. "I > 2σ(I)").</summary>
        public string ReflnsThreshold { get; set; } = "";

 /// <summary>Low resolution limit (d_min, Å).</summary>
        public float ResolutionHigh { get; set; }

 /// <summary>High resolution limit (d_max, Å).</summary>
        public float ResolutionLow { get; set; }

 /// <summary>Theta minimum for data collection (degrees).</summary>
        public float ThetaMin { get; set; }

 /// <summary>Theta maximum for data collection (degrees).</summary>
        public float ThetaMax { get; set; }

        #endregion

        #region ⭐ NEW: Experiment

 /// <summary>F(000) value.</summary>
        public float F000 { get; set; }

 /// <summary>Radiation wavelength (Å).</summary>
        public float Wavelength { get; set; }

 /// <summary>Radiation type (e.g. "Mo Kα").</summary>
        public string RadiationType { get; set; } = "";

 /// <summary>Diffractometer / measurement device type.</summary>
        public string MeasurementDevice { get; set; } = "";

 /// <summary>Crystal colour.</summary>
        public string CrystalColour { get; set; } = "";

 /// <summary>Crystal description (shape).</summary>
        public string CrystalDescription { get; set; } = "";

 /// <summary>Crystal size max dimension (mm).</summary>
        public float CrystalSizeMax { get; set; }

 /// <summary>Crystal size mid dimension (mm).</summary>
        public float CrystalSizeMid { get; set; }

 /// <summary>Crystal size min dimension (mm).</summary>
        public float CrystalSizeMin { get; set; }

 /// <summary>Absorption correction type.</summary>
        public string AbsorptionCorrectionType { get; set; } = "";

 /// <summary>Absorption correction T_min.</summary>
        public float AbsCorrectionTMin { get; set; }

 /// <summary>Absorption correction T_max.</summary>
        public float AbsCorrectionTMax { get; set; }

 /// <summary>Absorption coefficient μ (mm⁻¹).</summary>
        public float AbsorptionCoefficient { get; set; }

 /// <summary>Calculated density (g/cm³).</summary>
        public float DensityDiffrn { get; set; }

        #endregion

        #region ⭐ NEW: Audit / Publication

 /// <summary>Audit creation date.</summary>
        public string AuditCreationDate { get; set; } = "";

 /// <summary>Audit creation method (software used).</summary>
        public string AuditCreationMethod { get; set; } = "";

 /// <summary>Publication title.</summary>
        public string PublTitle { get; set; } = "";

 /// <summary>Journal name.</summary>
        public string JournalName { get; set; } = "";

 /// <summary>Journal volume / page reference.</summary>
        public string JournalReference { get; set; } = "";

 /// <summary>Publication DOI.</summary>
        public string DOI { get; set; } = "";

        #endregion

        #region Computed Properties

 /// <summary>Number of atoms in the structure.</summary>
        public int AtomCount => Atoms?.Count ?? 0;

 /// <summary>First atom in the structure (convenience property).</summary>
        public SimpleAtom FirstAtom => Atoms.Count > 0 ? Atoms[0] : null;

        #endregion

        #region Methods

 /// <summary>
 /// Returns a string representation of the structure.
 /// </summary>
        public override string ToString()
        {
            return $"{Name} ({AtomCount} atoms)";
        }

 /// <summary>
 /// Clears all structure data, resetting to default values.
 /// </summary>
        public void Clear()
        {
            Name = "Unknown";
            RawData = "";
            IsLoaded = false;
            FilePath = "";
            SourceFileName = "";
            a = b = c = 0;
            alpha = beta = gamma = 0;
            Volume = 0;
            SpaceGroup = 0;
            SpaceGroupName = "";
            SymmetryOperations.Clear();
            Atoms.Clear();
            ChemicalName = "";
            Formula = "";
            FormulaWeight = 0;
            Temperature = 0;
            RFactor = 0;

 // ⭐ NEW: clear extended metadata
            R1_Observed = R1_All = wR2_All = wR2_Observed = GooF = 0;
            RefinementReflections = RefinementParameters = RefinementRestraints = 0;
            ShiftMax = ShiftMean = 0;
            DiffDensityMax = DiffDensityMin = DiffDensityRMS = 0;
            WeightingScheme = "";

            ReflnsTotal = ReflnsObserved = 0;
            ReflnsThreshold = "";
            ResolutionHigh = ResolutionLow = ThetaMin = ThetaMax = 0;

            F000 = Wavelength = 0;
            RadiationType = MeasurementDevice = CrystalColour = CrystalDescription = "";
            CrystalSizeMax = CrystalSizeMid = CrystalSizeMin = 0;
            AbsorptionCorrectionType = "";
            AbsCorrectionTMin = AbsCorrectionTMax = AbsorptionCoefficient = 0;
            DensityDiffrn = 0;

            AuditCreationDate = AuditCreationMethod = "";
            PublTitle = JournalName = JournalReference = DOI = "";
        }

 /// <summary>
 /// Gets a formatted summary of the structure.
 /// </summary>
 /// <returns>A formatted string containing structure information.</returns>
        public string GetSummary()
        {
            return $"Structure: {Name}\n" +
                   $"Cell: a={a:F4}, b={b:F4}, c={c:F4} Å\n" +
                   $"Angles: α={alpha:F2}, β={beta:F2}, γ={gamma:F2}°\n" +
                   $"V={Volume:F2} Å³\n" +
                   $"Space group: {SpaceGroupName} (#{SpaceGroup})\n" +
                   $"Atoms: {AtomCount}\n" +
                   $"Formula: {Formula}";
        }

        #endregion
    }
}