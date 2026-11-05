// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure file parsers: ResData.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// EXTENDED: full RES metadata (refinement from REM lines)

using System.Collections.Generic;

namespace Tal.Parsers
{
 /// <summary>
 /// Represents all data parsed from a SHELX RES/INS file.
 /// Contains unit cell parameters, atomic coordinates, symmetry operations,
 /// refinement statistics (parsed from REM lines), and chemical metadata.
 /// </summary>
    public class ResData
    {
        #region File Information

 /// <summary>Name of the source file.</summary>
        public string FileName { get; set; } = "";



        #endregion

        #region Unit Cell Parameters

 /// <summary>Unit cell parameter a (in Ångströms).</summary>
        public float a, b, c;

 /// <summary>Unit cell angles (in degrees).</summary>
        public float alp, bet, gam;

 /// <summary>Standard uncertainties for a, b, c.</summary>
        public float a_e, b_e, c_e;

 /// <summary>Standard uncertainties for α, β, γ.</summary>
        public float alp_e, bet_e, gam_e;

 /// <summary>Unit cell volume (in Ångströms³).</summary>
        public float cell_volume;

 /// <summary>Number of formula units per unit cell (Z).</summary>
        public float formula_units_Z;

        #endregion

        #region Symmetry

 /// <summary>International Tables for Crystallography space group number.</summary>
        public int space_group_IT_number;

 /// <summary>Hermann-Mauguin space group name (extracted from TITL).</summary>
        public string space_group_name_HM = "";

 /// <summary>
 /// Source of symmetry operations:
 /// "file" (from SYMM cards), "database" (from SymmetryElements),
 /// "fallback" (identity only).
 /// </summary>
        public string symmetry_source = "";

 /// <summary>List of symmetry operation strings (e.g., "x,y,z").</summary>
        public List<string> SymmetryOperations = new List<string>();

        #endregion

        #region Atoms / SFAC

 /// <summary>List of atoms parsed from the RES file.</summary>
        public List<ResAtom> atom = new List<ResAtom>();

 /// <summary>List of scattering factor symbols (from SFAC card).</summary>
        public List<string> sfacList = new List<string>();

        #endregion

        #region Chemical / Experiment
 
       
 /// <summary>Title from the TITL card.</summary>
        public string TITL = "";

 /// <summary>Radiation wavelength (from CELL card).</summary>
        public float wavelength;

 /// <summary>Formula weight (molecular mass).</summary>
        public float formula_weight;

 /// <summary>Chemical formula sum (e.g., "C6H12O6").</summary>
        public string formula_sum;

 /// <summary>Measurement temperature (in Kelvin, from TEMP card).</summary>
        public float measurement_temperature;

        #endregion

        #region Refinement Statistics (basic)

 /// <summary>R1 factor for observed reflections (F &gt; 4σ).</summary>
        public float s_R_factor_gt;

 /// <summary>R1 factor for all reflections.</summary>
        public float _refine_ls_R_factor_all;

 /// <summary>wR2 factor for all reflections.</summary>
        public float wR2;

 /// <summary>Goodness of fit (GooF / S).</summary>
        public float goof;

 /// <summary>Maximum shift/su ratio.</summary>
        public float shift_max;

 /// <summary>Mean shift/su ratio.</summary>
        public float shift_mean;

 /// <summary>Global scale factor (from FVAR card).</summary>
        public float global_scale_factor;

        #endregion

        #region ⭐ NEW: Refinement Statistics (extended, from REM lines)

 /// <summary>Number of reflections used (F &gt; 4σ).</summary>
        public int refine_nreflns_gt;

 /// <summary>Number of reflections used (all data).</summary>
        public int refine_nreflns_all;

 /// <summary>Number of parameters refined.</summary>
        public int refine_nparams;

 /// <summary>Number of restraints applied.</summary>
        public int refine_nrestraints;

 /// <summary>Maximum shift/su (from REM).</summary>
        public float refine_shift_max;

 /// <summary>Mean shift/su (from REM).</summary>
        public float refine_shift_mean;

 /// <summary>Largest residual electron density peak (e/Å³).</summary>
        public float refine_diff_max;

 /// <summary>Deepest residual electron density hole (e/Å³).</summary>
        public float refine_diff_min;

 /// <summary>F(000) value (from REM line).</summary>
        public float F_000;

        #endregion

        #region State Flag

 /// <summary>Indicates whether the RES data has been fully parsed.</summary>
        public bool completed;

        #endregion

        #region Methods

 /// <summary>
 /// Clears all RES data, resetting all fields to their default values.
 /// </summary>
        public void Clean()
        {
            completed = false;

 // Unit cell
            a = b = c = 0;
            alp = bet = gam = 0;
            a_e = b_e = c_e = 0;
            alp_e = bet_e = gam_e = 0;
            cell_volume = 0;
            space_group_IT_number = 0;
            formula_units_Z = 0;

 // Chemical / experiment
            TITL = "";
            wavelength = 0;
            formula_weight = 0;
            formula_sum = "";
            measurement_temperature = 0;

 // Refinement (basic)
            s_R_factor_gt = 0;
            _refine_ls_R_factor_all = 0;
            global_scale_factor = 1.0f;
            wR2 = 0;
            goof = 0;
            shift_max = 0;
            shift_mean = 0;

 // Refinement (extended)
            refine_nreflns_gt = 0;
            refine_nreflns_all = 0;
            refine_nparams = 0;
            refine_nrestraints = 0;
            refine_shift_max = 0;
            refine_shift_mean = 0;
            refine_diff_max = 0;
            refine_diff_min = 0;
            F_000 = 0;

 // Collections
            atom?.Clear();
            SymmetryOperations?.Clear();
            sfacList?.Clear();

            space_group_name_HM = "";
            symmetry_source = "";
        }

        #endregion
    }

 /// <summary>
 /// Represents a single atom parsed from a SHELX RES/INS file.
 /// Contains fractional coordinates, occupancy, and displacement parameters.
 /// </summary>
    public class ResAtom
    {
        #region Identification

 /// <summary>Atom label (e.g., "C1", "O2").</summary>
        public string name = "";

 /// <summary>SFAC type number (index into sfacList).</summary>
        public int type;

 /// <summary>Element symbol (resolved from SFAC).</summary>
        public string element = "";

        #endregion

        #region Positional Coordinates (Fractional)

 /// <summary>Fractional coordinate x.</summary>
        public float x, y, z;

 /// <summary>Standard uncertainties for x, y, z.</summary>
        public float x_e, y_e, z_e;

        #endregion

        #region Occupancy and Displacement

 /// <summary>Atom site occupancy factor.</summary>
        public float occ = 1.0f;

 /// <summary>Standard uncertainty for occupancy.</summary>
        public float occ_e;

 /// <summary>Isotropic atomic displacement parameter.</summary>
        public float iso = 0.05f;

 /// <summary>Standard uncertainty for isotropic displacement.</summary>
        public float iso_e;

 /// <summary>Type of ADP: "Uiso" or "Uani".</summary>
        public string adp_type = "Uiso";

        #endregion

        #region Anisotropic Displacement Parameters

 /// <summary>Anisotropic displacement parameters U11..U23.</summary>
        public float u11, u22, u33, u12, u13, u23;

 /// <summary>Standard uncertainties for anisotropic displacement parameters.</summary>
        public float u11_e, u22_e, u33_e, u12_e, u13_e, u23_e;

        #endregion

        #region Flags

 /// <summary>True if occupancy was fixed (value with +10 offset in RES).</summary>
        public bool is_fixed_occ;

 /// <summary>True if Uiso was fixed (negative or +10 offset value in RES).</summary>
        public bool is_fixed_iso;

 /// <summary>PART number (disorder group).</summary>
        public int part_number = 1;

 /// <summary>True if atom name suggests hydrogen.</summary>
        public bool is_hydrogen;

 /// <summary>True if atom has anisotropic displacement parameters.</summary>
        public bool has_aniso = false;

        #endregion

        #region Flags

 

 /// <summary>
 /// ⭐ NEW: AFIX multiplier for wodoru geometrycznego (np. 1.2, 1.5).
 /// 0 = no AFIX (U_iso afterdane directly, albo atom ciężki).
 /// Used w drugim przebiegu RecalculateHydrogenUiso do obliczenia
 /// U_iso(H) = afix_multiplier × U_eq(neighbor).
 /// </summary>
        public float afix_multiplier = 0f;

 

        #endregion
    }
}