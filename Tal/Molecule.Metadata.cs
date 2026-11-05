// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — structure metadata.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — metadane krystalograficzne, eksperymentalne i publikacyjne.
// All fields are optional — populated from CIF/RES, empty when no data.

using System.Diagnostics;
using Tal.Parsers;

namespace Tal
{
    public partial class Molecule
    {
        #region Basic Metadata

 /// <summary>Measurement temperature (K).</summary>
        public float Temperature;

 /// <summary>R factor (legacy, from StructureData.RFactor).</summary>
        public float RFactor;

        #endregion

        #region Refinement Statistics

 /// <summary>R1 factor for observed reflections (F &gt; 4σ).</summary>
        public float R1_Observed;

 /// <summary>R1 factor for all data.</summary>
        public float R1_All;

 /// <summary>wR2 factor for all data.</summary>
        public float wR2_All;

 /// <summary>wR2 factor for observed data.</summary>
        public float wR2_Observed;

 /// <summary>Goodness of fit (GooF / S).</summary>
        public float GooF;

 /// <summary>Number of reflections used in refinement.</summary>
        public int RefinementReflections;

 /// <summary>Number of parameters refined.</summary>
        public int RefinementParameters;

 /// <summary>Number of restraints applied.</summary>
        public int RefinementRestraints;

 /// <summary>Maximum shift/su ratio.</summary>
        public float ShiftMax;

 /// <summary>Mean shift/su ratio.</summary>
        public float ShiftMean;

 /// <summary>Largest residual electron density peak (e/Å³).</summary>
        public float DiffDensityMax;

 /// <summary>Deepest residual electron density hole (e/Å³).</summary>
        public float DiffDensityMin;

 /// <summary>RMS residual electron density (e/Å³).</summary>
        public float DiffDensityRMS;

 /// <summary>Weighting scheme details.</summary>
        public string WeightingScheme = "";

        #endregion

        #region Reflection Statistics

 /// <summary>Total reflections measured.</summary>
        public int ReflnsTotal;

 /// <summary>Reflections with I &gt; 2σ(I).</summary>
        public int ReflnsObserved;

 /// <summary>Threshold expression.</summary>
        public string ReflnsThreshold = "";

 /// <summary>High resolution limit (d_min, Å).</summary>
        public float ResolutionHigh;

 /// <summary>Low resolution limit (d_max, Å).</summary>
        public float ResolutionLow;

 /// <summary>Theta minimum for data collection (degrees).</summary>
        public float ThetaMin;

 /// <summary>Theta maximum for data collection (degrees).</summary>
        public float ThetaMax;

        #endregion

        #region Experiment

 /// <summary>F(000) value.</summary>
        public float F000;

 /// <summary>Radiation wavelength (Å).</summary>
        public float Wavelength;

 /// <summary>Radiation type (e.g. "Mo Kα").</summary>
        public string RadiationType = "";

 /// <summary>Diffractometer / measurement device type.</summary>
        public string MeasurementDevice = "";

 /// <summary>Crystal colour.</summary>
        public string CrystalColour = "";

 /// <summary>Crystal description (shape).</summary>
        public string CrystalDescription = "";

 /// <summary>Crystal size max dimension (mm).</summary>
        public float CrystalSizeMax;

 /// <summary>Crystal size mid dimension (mm).</summary>
        public float CrystalSizeMid;

 /// <summary>Crystal size min dimension (mm).</summary>
        public float CrystalSizeMin;

 /// <summary>Absorption correction type.</summary>
        public string AbsorptionCorrectionType = "";

 /// <summary>Absorption correction T_min.</summary>
        public float AbsCorrectionTMin;

 /// <summary>Absorption correction T_max.</summary>
        public float AbsCorrectionTMax;

 /// <summary>Absorption coefficient μ (mm⁻¹).</summary>
        public float AbsorptionCoefficient;

 /// <summary>Calculated density (g/cm³).</summary>
        public float DensityDiffrn;

        #endregion

        #region Audit / Publication

 /// <summary>Audit creation date.</summary>
        public string AuditCreationDate = "";

 /// <summary>Audit creation method (software).</summary>
        public string AuditCreationMethod = "";

 /// <summary>Publication title.</summary>
        public string PublTitle = "";

 /// <summary>Journal name.</summary>
        public string JournalName = "";

 /// <summary>Journal reference (volume / page / year).</summary>
        public string JournalReference = "";

 /// <summary>Publication DOI.</summary>
        public string DOI = "";

        #endregion

        #region Copy from StructureData (CIF)

 /// <summary>
 /// Copies refinement, experiment, and publication metadata from a
 /// StructureData object into this Molecule.
 /// Called at the end of AsignXYZ so the data is available to the
 /// StructureInfoWindow and other UI comafternents.
 /// </summary>
        internal void CopyMetadataFromStructure(StructureData structure)
        {
            if (structure == null) return;

 // Refinement
            R1_Observed = structure.R1_Observed;
            R1_All = structure.R1_All;
            wR2_All = structure.wR2_All;
            wR2_Observed = structure.wR2_Observed;
            GooF = structure.GooF;
            RefinementReflections = structure.RefinementReflections;
            RefinementParameters = structure.RefinementParameters;
            RefinementRestraints = structure.RefinementRestraints;
            ShiftMax = structure.ShiftMax;
            ShiftMean = structure.ShiftMean;
            DiffDensityMax = structure.DiffDensityMax;
            DiffDensityMin = structure.DiffDensityMin;
            DiffDensityRMS = structure.DiffDensityRMS;
            WeightingScheme = structure.WeightingScheme ?? "";
            Temperature = structure.Temperature;
            RFactor = structure.RFactor;

 // Reflections
            ReflnsTotal = structure.ReflnsTotal;
            ReflnsObserved = structure.ReflnsObserved;
            ReflnsThreshold = structure.ReflnsThreshold ?? "";
            ResolutionHigh = structure.ResolutionHigh;
            ResolutionLow = structure.ResolutionLow;
            ThetaMin = structure.ThetaMin;
            ThetaMax = structure.ThetaMax;

 // Experiment
            F000 = structure.F000;
            Wavelength = structure.Wavelength;
            RadiationType = structure.RadiationType ?? "";
            MeasurementDevice = structure.MeasurementDevice ?? "";
            CrystalColour = structure.CrystalColour ?? "";
            CrystalDescription = structure.CrystalDescription ?? "";
            CrystalSizeMax = structure.CrystalSizeMax;
            CrystalSizeMid = structure.CrystalSizeMid;
            CrystalSizeMin = structure.CrystalSizeMin;
            AbsorptionCorrectionType = structure.AbsorptionCorrectionType ?? "";
            AbsCorrectionTMin = structure.AbsCorrectionTMin;
            AbsCorrectionTMax = structure.AbsCorrectionTMax;
            AbsorptionCoefficient = structure.AbsorptionCoefficient;
            DensityDiffrn = structure.DensityDiffrn;

 // Audit / Publication
            AuditCreationDate = structure.AuditCreationDate ?? "";
            AuditCreationMethod = structure.AuditCreationMethod ?? "";
            PublTitle = structure.PublTitle ?? "";
            JournalName = structure.JournalName ?? "";
            JournalReference = structure.JournalReference ?? "";
            DOI = structure.DOI ?? "";

            Debug.WriteLine($"[Molecule] Metadata copied from StructureData: " +
                            $"R1={R1_Observed:F4}, wR2={wR2_All:F4}, GooF={GooF:F3}, " +
                            $"F(000)={F000:F1}, Temp={structure.Temperature:F1}K");
        }

        #endregion

        #region Copy from ResData (RES/INS)

 /// <summary>
 /// Copies refinement metadata from a ResData object
 /// (parsed from a RES/INS file including REM lines) into this Molecule.
 /// </summary>
        internal void CopyMetadataFromRes(ResData resData)
        {
            if (resData == null) return;

 // Refinement
            R1_Observed = resData.s_R_factor_gt;
            R1_All = resData._refine_ls_R_factor_all;
            wR2_All = resData.wR2;
            wR2_Observed = resData.wR2; // RES does not distinguish — reuse wR2
            GooF = resData.goof;
            RefinementReflections = resData.refine_nreflns_all;
            RefinementParameters = resData.refine_nparams;
            RefinementRestraints = resData.refine_nrestraints;
            ShiftMax = resData.refine_shift_max;
            ShiftMean = resData.refine_shift_mean;
            DiffDensityMax = resData.refine_diff_max;
            DiffDensityMin = resData.refine_diff_min;
            DiffDensityRMS = 0f; // not available in RES
            Formula = resData.formula_sum ?? "";                  // ⭐ NOWE
            FormulaWeight = resData.formula_weight;
 // Experiment
            F000 = resData.F_000;
            Wavelength = resData.wavelength;
            Temperature = resData.measurement_temperature;
            RFactor = resData.s_R_factor_gt;

            Debug.WriteLine($"[Molecule] RES metadata copied: " +
                            $"R1={R1_Observed:F4}, wR2={wR2_All:F4}, GooF={GooF:F3}, " +
                            $"F(000)={F000:F1}");
        }

        #endregion

        #region Clear

 /// <summary>
 /// Resets all refinement, experiment, and publication metadata
 /// fields to their default values.
 /// </summary>
        private void ClearMetadata()
        {
            R1_Observed = R1_All = wR2_All = wR2_Observed = GooF = 0f;
            RefinementReflections = RefinementParameters = RefinementRestraints = 0;
            ShiftMax = ShiftMean = 0f;
            DiffDensityMax = DiffDensityMin = DiffDensityRMS = 0f;
            WeightingScheme = "";

            ReflnsTotal = ReflnsObserved = 0;
            ReflnsThreshold = "";
            ResolutionHigh = ResolutionLow = ThetaMin = ThetaMax = 0f;

            F000 = Wavelength = 0f;
            RadiationType = MeasurementDevice = CrystalColour = CrystalDescription = "";
            CrystalSizeMax = CrystalSizeMid = CrystalSizeMin = 0f;
            AbsorptionCorrectionType = "";
            AbsCorrectionTMin = AbsCorrectionTMax = AbsorptionCoefficient = 0f;
            DensityDiffrn = 0f;
            Temperature = 0f;
            RFactor = 0f;
            AuditCreationDate = AuditCreationMethod = "";
            PublTitle = JournalName = JournalReference = DOI = "";

            Formula = "";
            FormulaWeight = 0f;
            ChemicalName = "";


        }

        #endregion
    }
}