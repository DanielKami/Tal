// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule core class — partial definition entry afterint.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// EXTENDED: refinement / experiment / publication metadata for crystallographic reafterrting

using Microsoft.Xna.Framework;
using Tal.Calculate;
using Tal.Core.PowderDiffraction;
using static Tal.SymmetryElements;


namespace Tal
{


 /// <summary>
 /// Represents a molecular structure with atoms, symmetry operations,
 /// and 3D rendering data. Handles atom loading, symmetry generation,
 /// bond detection, and property calculations.
 /// </summary>
    public partial class Molecule
    {
        #region Events

 /// <summary>Event raised when the molecule shift changes.</summary>
        public event Action<Vector3> MoleculeShiftChanged;

        #endregion

        #region Basic Metadata
 

 // ⭐ NOWE — aftertrzebne do reafterrtu CavitySingleRunReafterrt
 /// <summary>Chemical formula sum (e.g. "C13 H18 O2").</summary>
        public string Formula = "";

 /// <summary>Formula weight (molecular mass, u).</summary>
        public float FormulaWeight;

 /// <summary>Chemical name of the comafterund.</summary>
        public string ChemicalName = "";

        #endregion

 /// <summary>
 /// Crystallographic planes determined for this structure
 /// (mean planes and hkl planes). Each structure has its own list.
 /// </summary>
        public List<CrystalPlane> Planes = new List<CrystalPlane>();

 

 /// <summary>
 /// Have hydrogens been normalised to neutron lengths?
 /// Set by HydrogenNormalizer.Normalize() and by Presentation() with the global flag.
 /// Zerowane przez ResetToOriginal() i CleanupBeforeNewLoad().
 /// </summary>
        public bool HydrogenNormalizationApplied;

 /// <summary>
 /// Has the user manually disabled auto-normalisation for this structure (clicked Reset)?
 /// Blocks auto-normalisation from Presentation() even when the global flag is ON.
 /// Zerowane przez Reset() w GUI (after Apply) i CleanupBeforeNewLoad().
 /// </summary>
        public bool HydrogenNormalizationDisabledByUser;

        #region Enums

 /// <summary>Charge calculation method.</summary>


        #endregion

        #region Public Fields

 /// <summary>Asymmetric unit atoms (from CIF).</summary>
        public List<AtomSimple> AsymmetricPart = new List<AtomSimple>();

 /// <summary>All symmetry equivalent atoms.</summary>
        public List<AtomSimple> SymetryEquivalents = new List<AtomSimple>();

 /// <summary>Unit cell frame data for rendering.</summary>
        public List<object3d> UnitCellFrame = new List<object3d>();

 /// <summary>Symmetry operations as parsed transformations.</summary>
        public List<ReadSymm> SymmetryOperations = new List<ReadSymm>();

        /// <summary>Original CIF/RES strings parallel to SymmetryOperations.</summary>
        public List<string> SymmetryOperationStrings = new List<string>();

 /// <summary>3D atoms for rendering.</summary>
        public Atom[] atom;

        /// <summary>Detected aromatic rings (filled by BondOrderAnalyzer).</summary>
        public List<AromaticRingInfo> AromaticRings = new List<AromaticRingInfo>();

        /// <summary>Info for one aromatic ring (centroid, normal, radius, atom indices).</summary>
        public struct AromaticRingInfo
        {
            public List<int> AtomIndices;
            public Vector3 Center;
            public Vector3 Normal;
            public float Radius;
        }

 /// <summary>Label offsets for atom labels.</summary>
        public AtomLabelOffset[] LabelOffsets;
        /// <summary>Screen offsets for unit-cell axis labels a,b,c (NDC), independent of atom labels.</summary>
        public AtomLabelOffset[] AxisLabelOffsets;

 /// <summary>Error message string.</summary>
        public static string error_message = "";

        #region Short Contacts — public API

 /// <summary>
 /// Recalculates Short Contacts. Delegates to ShortContactsFinder.
 /// </summary>
        public void UpdateShortContacts()
            => ShortContactsFinder.Calculate(this);


 /// <summary>
 /// NEW: Atoms the user MANUALLY clicked in Expand Contacts mode
 /// (as expansion "seed"). Their descendants must also stay.
 /// Used by RemoveHangingContacts so as not to remove expanded branches.
 /// </summary>
        public List<int> ExpandedSeedAtomIndices = new List<int>();


 /// <summary>
 /// Clears UseForShortContacts flag on expanded atoms.
 /// Delegates to ShortContactsFinder.
 /// </summary>
        public void RemoveExpandedAtoms()
            => ShortContactsFinder.RemoveExpandedAtoms(this);

 /// <summary>
 /// Clears all Contact* data on all atoms. Delegates to ShortContactsFinder.
 /// </summary>
        public void ClearAllContacts()
            => ShortContactsFinder.ClearAllContacts(this);

 /// <summary>
 /// Finds symmetry-related atoms near a given aftersition.
 /// Delegates to ShortContactsFinder.
 /// </summary>
        public List<AtomSimple> FindNearbyAtomsFromSymmetry(
            Vector3 pos, int atomIndex,
            bool filterIntraInter = true,
            bool skipCellZero = false)
            => ShortContactsFinder.FindNearbyAtomsFromSymmetry(
                this, pos, atomIndex, filterIntraInter, skipCellZero);

        #endregion

        #endregion

        #region Properties

        public string Name { get; set; } = "Unnamed Molecule";

 /// <summary>Number of atoms in the 3D structure.</summary>
        public int NumberOfAtoms;

 /// <summary>Number of selected atoms.</summary>
        public int NumberSelected;

 /// <summary>Unit cell parameter a.</summary>
        public float a, b, c, alpha, beta, gamma;

 /// <summary>Unit cell parameter uncertainties.</summary>
        public float a_e, b_e, c_e;
        public float alpha_e, beta_e, gamma_e;

 /// <summary>Space group name.</summary>
        public string SpaceGroupName
        {
            get => _spaceGroupName;
            set => _spaceGroupName = value ?? "";
        }

 /// <summary>Space group IT number.</summary>
        public int SpaceGroupNumber
        {
            get => _spaceGroupNumber;
            set => _spaceGroupNumber = value;
        }

 /// <summary>Molecule shift vector after centering.</summary>
        public Vector3 MoleculeShift;

 /// <summary>
 /// Number of atoms before H-bond cell expansion. Donors are only searched
 /// within this range so H-bonds are not duplicated when atoms from neighboring
 /// cells are added.
 /// </summary>
        public int OriginalAtomCountBeforeExpansion = 0;

 /// <summary>Unit cell volume.</summary>
        public float Volume;

        #endregion

        #region Private Fields

        private string _spaceGroupName = "";
        private int _spaceGroupNumber = 0;
        private float totalCharge;

        #endregion

        #region Property Ranges

 /// <summary>Minimum charge value in the molecule.</summary>
        public float ChargeMin { get; set; } = -0.5f;

 /// <summary>Maximum charge value in the molecule.</summary>
        public float ChargeMax { get; set; } = 0.5f;

 /// <summary>Minimum hydrophobicity value.</summary>
        public float HydrophobicityMin { get; set; } = -0.5f;

 /// <summary>Maximum hydrophobicity value.</summary>
        public float HydrophobicityMax { get; set; } = 0.5f;

 /// <summary>Total charge of the molecule.</summary>
        public float TotalCharge
        {
            get => totalCharge;
            private set => totalCharge = value;
        }

        #endregion


 /// <summary>
 /// Atom indices added by Expand Contacts / Short Contacts.
 /// ⭐ These atoms are NOT removed — we only clear their UseForShortContacts flag.
 /// </summary>
        public List<int> ExpandedAtomIndices = new List<int>();

 /// <summary>
 /// Atom indices added by HBondFinder (acceptors from neighboring cells
 /// that form an H-bond).
 /// ⭐ These atoms are NOT removed — we only clear their UseForHBonds flag.
 /// </summary>
        public List<int> HBondNeighborAtomIndices = new List<int>();

        #region Nested Types

 /// <summary>
 /// Atom data for asymmetric unit and symmetry equivalents.
 /// </summary>
        public struct AtomSimple
        {
            public string name;
            public int AtomicNumber;
            public float x, y, z;
            public float x_e, y_e, z_e;
            public Matrix Ue;
            public Matrix Un;
            public float iso;
            public float occupancy;
            public Matrix Transformation;
            public int SymmetryEquivalentNumberInCell;
            public int disorder_group;
            public int IndividualMoleculeInCell;
            public float charge;
            public float Hydrophobicity;
            public bool isAnisotropic;
            public float U11, U22, U33, U12, U13, U23;

            public int CellShiftX;
            public int CellShiftY;
            public int CellShiftZ;
        }

 /// <summary>
 /// 3D atom data for rendering with bonds and properties.
 /// </summary>
        public struct Atom
        {
 // ═══════════════════════════════════════════════════════════
 // ORIGINAL FIELDS (DO NOT REMOVE)
 // ═══════════════════════════════════════════════════════════

            public string name;
            public int style;
            public int AtomicNumber;
            public bool selected;
            public int SelectionOrder;
            public bool show;
            public Vector3 Coordinate;
            public Vector3 Coordinate_error;
            public int Z_Depth;
            public int disorder_group;
            public float charge;
            public float Hydrophobicity;

            /// <summary>MACE force on this atom (eV/Å), central cell only.</summary>
            public Vector3 Force;
            public Vector3 ForceElectro;
            public Vector3 ForceDispersion;
            public Vector3 ForceRepulsion;
            /// <summary>True when Force has been computed for this atom.</summary>
            public bool HasForce;

 // ── Covalent bonds ──
            public int NrBondedAtoms;
            public float[] Distance;
            public float[] BondOccupancy;
            public int[] BondedAtom;
            public float[] AngleZ;
            public float[] AngleXY;
            /// <summary>Bond order per bonded partner: 1=single, 2=double, 3=triple, 4=aromatic.</summary>
            public int[] BondOrder;

 // ── Hydrogen bonds (original) ──
            public int H_NrBondedAtoms;
            public float[] H_Distance;
            public int[] H_BondedAtom;
            public float[] H_AngleZ;
            public float[] H_AngleXY;

 // ── ADP / symmetry / matrices ──
            public Matrix Ue;
            public Matrix Un;
            public float occupancy;
            public Matrix Transformation;
            public int SymmetryEquivalentNumberInCell;
            public int IndividualMoleculeInCell;
            public Matrix[] PrecomputedBondMatrix;
            public Matrix[] PrecomputedHBondMatrix;
            public bool isAnisotropic;

 // ── Custom color (overlay) ──
            public Vector3 CustomColor;

            public bool show_label;

 // ═══════════════════════════════════════════════════════════
 // ⭐ Participation flags:
 //
 // Atoms added by finders are NOT removed from atom[] —
 // only disabled via a flag. This keeps indices in arrays
 // (ContactAtom, BondedAtom, H_BondedAtom) stable.
 //
 // - Structure atoms (from Presentation): both = true
 // - Acceptors from HBondFinder: UseForHBonds = true,
 // UseForShortContacts = false
 // - Neighbors from ShortContactsFinder:
 // UseForShortContacts = true,
 // UseForHBonds = false
 // ═══════════════════════════════════════════════════════════

 /// <summary>
 /// Whether the atom participates in H-bond calculations.
 /// Atoms added by ShortContactsFinder have this = false.
 /// </summary>
            public bool UseForHBonds;

 /// <summary>
 /// Whether the atom participates in Short Contacts calculations.
 /// Atoms added by HBondFinder have this = false.
 /// </summary>
            public bool UseForShortContacts;

 // ── Contact data (Expand Contacts) ──
            public int ContactCount;
            public float[] ContactDistance;
            public int[] ContactAtom;
            public float[] ContactAngleZ;
            public float[] ContactAngleXY;
            public Matrix[] PrecomputedContactMatrix;
            public bool[] ContactIsNew;

 // ═══════════════════════════════════════════════════════════
 // Unit cell translation
 // ═══════════════════════════════════════════════════════════

 /// <summary>Cell translation X (0 = parent cell, ±1 = neighbors).</summary>
            public int CellShiftX;

 /// <summary>Cell translation Y.</summary>
            public int CellShiftY;

 /// <summary>Cell translation Z.</summary>
            public int CellShiftZ;

 // ═══════════════════════════════════════════════════════════
 // Extended H-bond fields
 // ═══════════════════════════════════════════════════════════

 /// <summary>Hydrogen index for each H-bond.</summary>
            public int[] H_HydrogenAtom;

 /// <summary>H⋯A distance in Å.</summary>
            public float[] H_HydrogenAcceptorDistance;

 /// <summary>D⋯A distance in Å.</summary>
            public float[] H_DonorAcceptorDistance;

 /// <summary>D-H⋯A angle in degrees.</summary>
            public float[] H_DHA_Angle;

 /// <summary>Acceptor cell translation X.</summary>
            public int[] H_AccCellX;

 /// <summary>Acceptor cell translation Y.</summary>
            public int[] H_AccCellY;

 /// <summary>Acceptor cell translation Z.</summary>
            public int[] H_AccCellZ;

 /// <summary>Acceptor symmetry operation index.</summary>
            public int[] H_AccSymmOpIndex;

 /// <summary>Acceptor name (e.g. "O1").</summary>
            public string[] H_AccName;

 /// <summary>Acceptor Cartesian aftersition (shifted).</summary>
            public Vector3[] H_AccCartPos;

 /// <summary>Acceptor atomic number.</summary>
            public int[] H_AccAtomicNumber;

 /// <summary>Acceptor molecule ID.</summary>
            public int[] H_AccMoleculeId;
        }

 /// <summary>
 /// Label offset data for atom labels.
 /// </summary>
        public struct AtomLabelOffset
        {
            public float OffsetX;
            public float OffsetY;
            public bool IsCustom;
        }

        #endregion


    }


}