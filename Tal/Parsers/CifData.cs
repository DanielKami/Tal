// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure file parsers: CifData.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// EXTENDED: full CIF metadata (refinement, reflections, experiment, publication)

namespace Tal.Parsers
{
 /// <summary>
 /// Represents all data parsed from a Crystallographic Information File (CIF).
 /// Contains unit cell parameters, atomic coordinates, symmetry operations,
 /// refinement statistics, reflection statistics, experiment details,
 /// and publication metadata.
 /// </summary>
    public class CifData
    {
        #region File Information

 /// <summary>Gets or sets the name of the CIF file (or data block name).</summary>
        public string FileName { get; set; } = "";

        #endregion

        #region Unit Cell Parameters

 /// <summary>Unit cell parameter a (in Ångströms).</summary>
        public float a;
 /// <summary>Unit cell parameter b (in Ångströms).</summary>
        public float b;
 /// <summary>Unit cell parameter c (in Ångströms).</summary>
        public float c;

 /// <summary>Unit cell angle alpha (in degrees).</summary>
        public float alp;
 /// <summary>Unit cell angle beta (in degrees).</summary>
        public float bet;
 /// <summary>Unit cell angle gamma (in degrees).</summary>
        public float gam;

 /// <summary>Standard uncertainty for parameter a.</summary>
        public float a_e;
 /// <summary>Standard uncertainty for parameter b.</summary>
        public float b_e;
 /// <summary>Standard uncertainty for parameter c.</summary>
        public float c_e;

 /// <summary>Standard uncertainty for angle alpha.</summary>
        public float alp_e;
 /// <summary>Standard uncertainty for angle beta.</summary>
        public float bet_e;
 /// <summary>Standard uncertainty for angle gamma.</summary>
        public float gam_e;

 /// <summary>Unit cell volume (in Ångströms³).</summary>
        public float cell_volume;
 /// <summary>Standard uncertainty for cell volume.</summary>
        public float cell_volume_e;

 /// <summary>Number of reflections used for cell measurement.</summary>
        public int cell_measurement_reflns_used;

 /// <summary>Theta minimum for cell measurement (degrees).</summary>
        public float cell_theta_min;

 /// <summary>Theta maximum for cell measurement (degrees).</summary>
        public float cell_theta_max;

        #endregion

        #region Symmetry Information

 /// <summary>International Tables for Crystallography space group number.</summary>
        public int space_group_IT_number;

 /// <summary>Number of formula units per unit cell (Z).</summary>
        public float formula_units_Z;

 /// <summary>Source of symmetry information.</summary>
        public string symmetry_source = "";

 /// <summary>Symmetry orientation description.</summary>
        public string symmetry_orientation = "";

 /// <summary>Symmetry orientation number.</summary>
        public int symmetry_orientation_number = 0;

 /// <summary>Hermann-Mauguin space group symbol (e.g., "P 21/c").</summary>
        public string space_group_name_HM = "";

 /// <summary>⭐ NEW: Hall space group symbol (e.g., "-P 2ybc").</summary>
        public string space_group_name_hall = "";

 /// <summary>List of symmetry operation strings (e.g., "x,y,z", "-x,-y,-z").</summary>
        public List<string> SymmetryOperations = new List<string>();

        #endregion

        #region Atomic Data

 /// <summary>List of atoms in the asymmetric unit.</summary>
        public List<Atom> atom = new List<Atom>();

        #endregion

        #region Chemical Metadata

 /// <summary>Common chemical name of the comafterund.</summary>
        public string chemical_name_common = "";

 /// <summary>⭐ NEW: Systematic (IUPAC) chemical name.</summary>
        public string chemical_name_systematic = "";

 /// <summary>Formula weight (molecular mass).</summary>
        public float formula_weight;

 /// <summary>Chemical formula sum (e.g., "C6H12O6").</summary>
        public string formula_sum;

 /// <summary>⭐ NEW: Chemical formula moiety (formula of asymmetric unit).</summary>
        public string formula_moiety = "";

 /// <summary>Measurement temperature (in Kelvin).</summary>
        public float measurement_temperature;

 /// <summary>User comments from Oxford Diffraction software.</summary>
        public string chemical_oxdiff_usercomment = "";

        #endregion

        #region ⭐ NEW: Refinement Statistics

 /// <summary>R1 factor for observed reflections (I &gt; 2σ(I)) / (F &gt; 4σ(F)).</summary>
        public float s_R_factor_gt;

 /// <summary>R1 factor for all reflections.</summary>
        public float _refine_ls_R_factor_all;

 /// <summary>wR2 factor for all reflections.</summary>
        public float refine_wR2;

 /// <summary>wR2 factor for observed reflections.</summary>
        public float refine_wR2_gt;

 /// <summary>Goodness of fit (GooF / S).</summary>
        public float refine_goof;

 /// <summary>Number of reflections used in refinement.</summary>
        public int refine_nreflns;

 /// <summary>Number of parameters refined.</summary>
        public int refine_nparams;

 /// <summary>Number of restraints applied.</summary>
        public int refine_nrestraints;

 /// <summary>Maximum shift/su ratio.</summary>
        public float refine_shift_max;

 /// <summary>Mean shift/su ratio.</summary>
        public float refine_shift_mean;

 /// <summary>Weighting scheme details (e.g., "w = 1/[σ²(Fo²) + (0.05P)² + 0.5P]").</summary>
        public string refine_weighting = "";

 /// <summary>Largest residual electron density peak (e/Å³).</summary>
        public float refine_diff_max;

 /// <summary>Deepest residual electron density hole (e/Å³).</summary>
        public float refine_diff_min;

 /// <summary>RMS residual electron density (e/Å³).</summary>
        public float refine_diff_rms;

        #endregion

        #region ⭐ NEW: Reflection Statistics

 /// <summary>Total number of reflections measured.</summary>
        public int reflns_total;

 /// <summary>Number of reflections with I &gt; 2σ(I).</summary>
        public int reflns_gt;

 /// <summary>Threshold expression (e.g., "I &gt; 2σ(I)").</summary>
        public string reflns_threshold = "";

 /// <summary>High resolution limit (d_min, Å).</summary>
        public float resolution_high;

 /// <summary>Low resolution limit (d_max, Å).</summary>
        public float resolution_low;

 /// <summary>Theta minimum for data collection (degrees).</summary>
        public float theta_min;

 /// <summary>Theta maximum for data collection (degrees).</summary>
        public float theta_max;

        #endregion

        #region ⭐ NEW: Experiment

 /// <summary>F(000) value (structure factor at zero angle).</summary>
        public float F_000;

 /// <summary>Radiation wavelength (Å).</summary>
        public float wavelength;

 /// <summary>Radiation type (e.g., "Mo Kα", "Cu Kα").</summary>
        public string radiation_type = "";

 /// <summary>Diffractometer / measurement device type.</summary>
        public string measurement_device = "";

 /// <summary>Diffraction source (e.g., "fine-focus sealed tube").</summary>
        public string diffrn_source = "";

 /// <summary>Diffraction detector type (e.g., "CCD area detector").</summary>
        public string diffrn_detector = "";

 /// <summary>Crystal colour.</summary>
        public string crystal_colour = "";

 /// <summary>Crystal description / shape (e.g., "block", "needle").</summary>
        public string crystal_description = "";

 /// <summary>Crystal size max dimension (mm).</summary>
        public float crystal_size_max;

 /// <summary>Crystal size mid dimension (mm).</summary>
        public float crystal_size_mid;

 /// <summary>Crystal size min dimension (mm).</summary>
        public float crystal_size_min;

 /// <summary>Absorption coefficient μ (in mm⁻¹).</summary>
        public float exptl_absorpt_coefficient_mu;

 /// <summary>Absorption correction type (e.g., "multi-scan", "numerical").</summary>
        public string exptl_absorpt_correction_type = "";

 /// <summary>Absorption correction T_min.</summary>
        public float abs_correction_t_min;

 /// <summary>Absorption correction T_max.</summary>
        public float abs_correction_t_max;

 /// <summary>Calculated crystal density (in g/cm³).</summary>
        public float exptl_crystal_density_diffrn;

 /// <summary>Method used to determine crystal density.</summary>
        public string exptl_crystal_density_method = "";

        #endregion

        #region ⭐ NEW: Audit / Publication

 /// <summary>Audit creation date (when CIF was written).</summary>
        public string audit_creation_date = "";

 /// <summary>Audit creation method (software used to write CIF).</summary>
        public string audit_creation_method = "";

 /// <summary>Publication title.</summary>
        public string publ_title = "";

 /// <summary>Journal name.</summary>
        public string journal_name = "";

 /// <summary>Journal volume.</summary>
        public string journal_volume = "";

 /// <summary>Journal first page.</summary>
        public string journal_page = "";

 /// <summary>Journal year.</summary>
        public string journal_year = "";

 /// <summary>Publication DOI.</summary>
        public string doi = "";

        #endregion

        #region State Flag

 /// <summary>Indicates whether the CIF data has been fully parsed.</summary>
        public bool completed;

        #endregion

        #region Methods

 /// <summary>
 /// Clears all CIF data, resetting all fields to their default values.
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
            cell_volume_e = 0;
            cell_measurement_reflns_used = 0;
            cell_theta_min = cell_theta_max = 0;

 // Symmetry
            space_group_IT_number = 0;
            formula_units_Z = 0;
            symmetry_source = "";
            symmetry_orientation = "";
            symmetry_orientation_number = 0;
            space_group_name_HM = "";
            space_group_name_hall = "";
            SymmetryOperations.Clear();

 // Atoms
            atom.Clear();

 // Chemical metadata
            chemical_name_common = "";
            chemical_name_systematic = "";
            formula_weight = 0;
            formula_sum = "";
            formula_moiety = "";
            measurement_temperature = 0;
            chemical_oxdiff_usercomment = "";

 // ⭐ NEW: Refinement
            s_R_factor_gt = 0;
            _refine_ls_R_factor_all = 0;
            refine_wR2 = 0;
            refine_wR2_gt = 0;
            refine_goof = 0;
            refine_nreflns = 0;
            refine_nparams = 0;
            refine_nrestraints = 0;
            refine_shift_max = 0;
            refine_shift_mean = 0;
            refine_weighting = "";
            refine_diff_max = 0;
            refine_diff_min = 0;
            refine_diff_rms = 0;

 // ⭐ NEW: Reflections
            reflns_total = 0;
            reflns_gt = 0;
            reflns_threshold = "";
            resolution_high = 0;
            resolution_low = 0;
            theta_min = 0;
            theta_max = 0;

 // ⭐ NEW: Experiment
            F_000 = 0;
            wavelength = 0;
            radiation_type = "";
            measurement_device = "";
            diffrn_source = "";
            diffrn_detector = "";
            crystal_colour = "";
            crystal_description = "";
            crystal_size_max = crystal_size_mid = crystal_size_min = 0;
            exptl_absorpt_coefficient_mu = 0;
            exptl_absorpt_correction_type = "";
            abs_correction_t_min = 0;
            abs_correction_t_max = 0;
            exptl_crystal_density_diffrn = 0;
            exptl_crystal_density_method = "";

 // ⭐ NEW: Audit / Publication
            audit_creation_date = "";
            audit_creation_method = "";
            publ_title = "";
            journal_name = "";
            journal_volume = "";
            journal_page = "";
            journal_year = "";
            doi = "";
        }

        #endregion
    }

 /// <summary>
 /// Represents a single atom parsed from a CIF file.
 /// Contains aftersitional coordinates, displacement parameters,
 /// and other crystallographic data for an atom in the asymmetric unit.
 /// </summary>
    public class Atom
    {
        #region Identification

 /// <summary>Atom label (e.g., "C1", "O2").</summary>
        public string name;

 /// <summary>Atom type identifier (element number).</summary>
        public int type;

        #endregion

        #region Positional Coordinates

 /// <summary>Fractional coordinate x.</summary>
        public float x;
 /// <summary>Fractional coordinate y.</summary>
        public float y;
 /// <summary>Fractional coordinate z.</summary>
        public float z;

 /// <summary>Standard uncertainty for coordinate x.</summary>
        public float x_e;
 /// <summary>Standard uncertainty for coordinate y.</summary>
        public float y_e;
 /// <summary>Standard uncertainty for coordinate z.</summary>
        public float z_e;

        #endregion

        #region Occupancy and Thermal Parameters

 /// <summary>Atom site occupancy factor.</summary>
        public float occ;
 /// <summary>Standard uncertainty for occupancy.</summary>
        public float occ_e;

 /// <summary>Isotropic atomic displacement parameter (U_iso or B_iso).</summary>
        public float iso;
 /// <summary>Standard uncertainty for isotropic displacement.</summary>
        public float iso_e;

 /// <summary>Type of ADP (Anisotropic Displacement Parameters).</summary>
        public string adp_type;

 /// <summary>Anisotropic displacement parameter U11.</summary>
        public float u11, u22, u33, u12, u13, u23;
 /// <summary>Standard uncertainties for anisotropic displacement parameters.</summary>
        public float u11_e, u22_e, u33_e, u12_e, u13_e, u23_e;

        #endregion

        #region Symmetry and Disorder

 /// <summary>Symmetry order (multiplicity of the atom site).</summary>
        public int symmetry_order;

 /// <summary>Calculation flag (e.g., "calc" for calculated aftersition).</summary>
        public string calc_flag;

 /// <summary>Disorder group number for disordered structures.</summary>
        public int disorder_group;

        #endregion
    }
}