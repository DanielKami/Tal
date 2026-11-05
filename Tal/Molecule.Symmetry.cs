// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — symmetry expansion + ADP transformation (Un / Ue).
//
// ============================================================================
// Molecule.Symmetry.cs — symmetry operations + ADP transformation (Un / Ue)
// ============================================================================
//
// This file generates the list of SymmetryEquivalents from AsymmetricPart.
// Below is the full matrix chain from CIF to ORTEP and cavities (must agree
// with Molecule.Data.cs, Draw.*.cs, VoxelScanner, CavityDetector).
//
// ---------------------------------------------------------------------------
// 0. MATRIX NOTATION
// ---------------------------------------------------------------------------
//
// A  FractionaltoCartesianMatrix(0,0,0)
//    COLUMN convention: x_cart = A · x_frac
//    (same A as used for Uij → Ue conversion in Molecule.Data.cs).
//    Do NOT use Transpose(A) when building R_cart — that is the row form
//    for Vector3.Transform and breaks hexagonal / rhombohedral / trigonal.
//
// N  Axis-length / angle normalisation matrix (in Data.cs) for Uij→Ue.
//
// Ue After reading CIF: first the U tensor in the crystal frame, then
//    Ue_cart = A · N · U · Nᵀ · Aᵀ
//    After Eigen.main(ref Ue): Ue is OVERWRITTEN by the eigen form (+D·Eᵀ),
//    it is no longer the raw ADP tensor.
//
// Un Un = Eigen.main(ref Ue) = −D · Eᵀ
//    ROW i = √|λ_i| · e_iᵀ (1σ semi-axis × eigenvector direction in Cartesian).
//    This is the form required by CavityDetector (row length = semi-axis).
//
// R_frac  3×3 LINEAR part of the symmetry operation from ReadSymm
//         (axis1/sign1 + axis2/sign2). Without translations t — translations
//         do not rotate ADPs.  x_frac' = R_frac · x_frac + t
//
// R_cart  R_cart = A · R_frac · A⁻¹
//         Same operation in Cartesian: x_cart' = R_cart · x_cart + A·t
//
// T       atom.Transformation — left-multiply in Draw (see below).
//
// ---------------------------------------------------------------------------
// 1. DIAGRAM: CIF → ORTEP / CAVITY
// ---------------------------------------------------------------------------
//
// CIF
// │  Uij (crystal system), x_frac, symmetry strings
// ▼
// Molecule.Data.cs
// │  Ue ← Uij
// │  Ue ← A · N · Ue · Nᵀ · Aᵀ          // tensor in CARTESIAN
// │  Un ← Eigen.main(ref Ue)            // Un = −D·Eᵀ, Ue overwritten
// ▼
// AsymmetricPart
// │  Un in Cartesian, Transformation = I

// ▼
// CreateTransformations / ApplySymmetry
// │  R_frac from ReadSymm, R_cart = A · R_frac · A⁻¹
// │  Un' = Un · R_cartᵀ          // eigen form, semi-axes preserved
// │  T   = R_cart                // Draw: R · (Un·Rᵀ) = R·Un·Rᵀ
// │  position: x' = R_frac·x + t (fractional)
// ▼
// SymmetryEquivalents
// │  Un' (eigen), Transformation = R_cart
// ▼          ▼
// Draw       CavityDetector / VoxelScanner (SDF)
// │          │  Rx = r_vdw·mult + √λ_i
// │          │  q = Σ (proj_i/Rx)² ≤ 1 → occupied
// ▼          ▼
// ellipsoid on screen   void surface (ADP ON)
//
// ---------------------------------------------------------------------------
// 2. WHY SPLIT Un' vs Transformation
// ---------------------------------------------------------------------------
//
// Draw computes:  vertex' = vertex × Transformation × Un
// SDF  computes:  lengths and directions of ROWS of Un (without Transformation).
//
// So that Draw gets M = R·Un·Rᵀ (like old S·Un·S for diagonal S)
// and SDF gets Un in eigen form (|row| = √λ), we need:
//
//   Un' = Un · Rᵀ     // eigen form, semi-axes preserved
//   T   = R           // Draw: R · (Un·Rᵀ) = R·Un·Rᵀ
//
// WRONG: Un' = R·Un·Rᵀ and T = I
//   → Draw OK, but SDF: rows are NO LONGER axes → bad voids
//     (especially rhombohedral / hexagonal where R_cart is not diagonal).
//
// WRONG: Un' = Un·Rᵀ and T = I
//   → SDF OK, but Draw = Un·Rᵀ ≠ R·Un·Rᵀ → "inverted" ORTEP axes.
//
// Old code (signs only, diagonal operations):
//   Un'_ij = Un_ij * (sign1[j]*sig2[j])   // Un · S
//   T = diag(sign1)                       // ≈ S
//   Draw: T·Un' ≈ S·Un·S
// The new path generalises this to full R_cart (axis permutations etc.).
//
// ---------------------------------------------------------------------------
// 3. FALLBACK (no invertible A)
// ---------------------------------------------------------------------------
//
// Exact legacy: TransformUnSignOnly + Transformation = diag(sign1)
// (bitwise identical to historical CalculateSymmetryEquivalents).
//
// ---------------------------------------------------------------------------
// 4. SPHERICAL VdW (without UseThermalMotion)
// ---------------------------------------------------------------------------
//
// SDF uses only r_vdw·mult — Un and R_cart do not affect voids.
// ADP transformation matters only with anisotropy.
//
// ============================================================================

using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using static Tal.SymmetryElements;

namespace Tal
{
    public partial class Molecule
    {
        #region Space Group Configuration

        public void SetSpaceGroupInfo(string name, int number)
        {
            _spaceGroupName = name ?? "";
            _spaceGroupNumber = number;
        }

        #endregion

        #region Symmetry Operations Generation

        public void CreateTransformations(int group)
        {
            SymmetryOperations.Clear();
            SymmetryOperationStrings.Clear();
            if (group < 1) return;

            int targetGroup = group;
            int targetOrientation = 0;

            if (!string.IsNullOrEmpty(_spaceGroupName))
            {
                string normalized = NormalizeGroupName(_spaceGroupName);
                targetOrientation = GetOrientationForGroup(normalized, targetGroup);
            }

            string[] selectedEntry = null;
            foreach (var entry in SymmetryElements.SymmetryOperations)
            {
                if (entry.Length > 3 && int.Parse(entry[0]) == targetGroup && int.Parse(entry[3]) == targetOrientation)
                {
                    selectedEntry = entry;
                    break;
                }
            }

            if (selectedEntry == null)
            {
                foreach (var entry in SymmetryElements.SymmetryOperations)
                {
                    if (entry.Length > 3 && int.Parse(entry[0]) == targetGroup && int.Parse(entry[3]) == 0)
                    {
                        selectedEntry = entry;
                        targetOrientation = 0;
                        break;
                    }
                }
            }

            if (selectedEntry == null) return;

            int startIndex = 3;
            if (selectedEntry.Length > 4 && (selectedEntry[3] == "0" || selectedEntry[3] == "1" ||
                                              selectedEntry[3] == "2" || selectedEntry[3] == "3"))
            {
                startIndex = 4;
            }

            for (int i = startIndex; i < selectedEntry.Length; i++)
            {
                SymmetryOperations.Add(RetriveTransformation(selectedEntry[i]));
                SymmetryOperationStrings.Add(selectedEntry[i]);
            }

            Debug.WriteLine($"[CreateTransformations] Created {SymmetryOperations.Count} symmetry operations for group {group}");
        }

        public void AddSymmetryOperationFromFile(string value)
        {
            string modifiedString = value.Replace(" ", "").Replace("'", "").Replace("`", "").Replace("\"", "");
            SymmetryOperations.Add(RetriveTransformation(modifiedString));
            if (SymmetryOperationStrings == null)
                SymmetryOperationStrings = new System.Collections.Generic.List<string>();
            SymmetryOperationStrings.Add(value.Trim());
        }

        #endregion

        public string GetSymmetryOperationLabel(int index)
        {
            if (SymmetryOperations == null || index < 0 || index >= SymmetryOperations.Count)
                return $"#{index}";

            if (SymmetryOperationStrings != null
                && index < SymmetryOperationStrings.Count
                && !string.IsNullOrWhiteSpace(SymmetryOperationStrings[index]))
                return SymmetryOperationStrings[index];

            return SymmetryElements.ReadSymmToString(SymmetryOperations[index]);
        }

        /// <summary>
        /// Returns whether the operation at <paramref name="index"/> participates in AU expansion.
        /// When <see cref="Flags.EnabledSymmetryOps"/> is null or empty, all operations are enabled.
        /// </summary>
        public bool IsSymmetryOperationEnabled(int index)
        {
            var en = Flags.Instance.EnabledSymmetryOps;
            if (en == null || en.Length == 0) return true;
            if (index < 0 || index >= en.Length) return true;
            return en[index];
        }

        /// <summary>
        /// Ensures <see cref="Flags.EnabledSymmetryOps"/> matches <see cref="SymmetryOperations"/>.Count.
        /// New slots default to enabled; existing flags are preserved when the array grows or shrinks.
        /// </summary>
        public void EnsureSymmetryOpFlags()
        {
            int n = SymmetryOperations?.Count ?? 0;
            if (n == 0)
            {
                Flags.Instance.EnabledSymmetryOps = null;
                return;
            }

            var en = Flags.Instance.EnabledSymmetryOps;
            if (en != null && en.Length == n)
                return;

            var neu = new bool[n];
            for (int i = 0; i < n; i++)
                neu[i] = en != null && i < en.Length ? en[i] : true;
            Flags.Instance.EnabledSymmetryOps = neu;
        }

        #region Helpers

        private string NormalizeGroupName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            return name.Replace(" ", "").Replace("(", "").Replace(")", "");
        }

        private int GetOrientationForGroup(string normalizedName, int groupNumber) => 0;

        #endregion

        #region ADP / R_cart helpers

        private static void BuildRfrac(ReadSymm s, out Matrix rFrac)
        {
            rFrac = new Matrix(
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 1);

            for (int axis = 0; axis < 3; axis++)
            {
                int p1 = s.pos1[axis], s1 = s.sign1[axis];
                if (p1 >= 0 && p1 < 3 && s1 != 0)
                    SetM(ref rFrac, axis, p1, GetM(rFrac, axis, p1) + s1);

                int p2 = s.pos2[axis], s2 = s.sign2[axis];
                if (p2 >= 0 && p2 < 3 && s2 != 0)
                    SetM(ref rFrac, axis, p2, GetM(rFrac, axis, p2) + s2);
            }
        }

        private static bool TryBuildRcart(Matrix A, Matrix rFrac, out Matrix rCart)
        {
            if (Math.Abs(A.Determinant()) < 1e-12f)
            {
                rCart = Matrix.Identity;
                return false;
            }

            Matrix Ainv;
            Matrix.Invert(ref A, out Ainv);

            rCart = A * rFrac * Ainv;
            rCart.M14 = rCart.M24 = rCart.M34 = 0;
            rCart.M41 = rCart.M42 = rCart.M43 = 0;
            rCart.M44 = 1;
            return true;
        }

 /// <summary>
 /// Un' = Un · R_cartᵀ.
 /// Rows: √|λ|·eᵀ → √|λ|·(R e)ᵀ. Row length = semi-axis (SDF requirement).
 /// Together with Transformation = R_cart yields in Draw: R·Un·Rᵀ.
 /// </summary>
        private static Matrix TransformUnEigenRows(Matrix un, Matrix rCart)
        {
            Matrix m = un * Matrix.Transpose(rCart);
            m.M14 = m.M24 = m.M34 = 0;
            m.M41 = m.M42 = m.M43 = 0;
            m.M44 = un.M44;
            return m;
        }

 /// <summary>Legacy column sign scale (bit-compatible with old code).</summary>
        private static Matrix TransformUnSignOnly(Matrix un, ReadSymm matrix)
        {
            int sig1 = matrix.sign2[0]; if (sig1 == 0) sig1 = 1;
            int sig2 = matrix.sign2[1]; if (sig2 == 0) sig2 = 1;
            int sig3 = matrix.sign2[2]; if (sig3 == 0) sig3 = 1;

            float s0 = matrix.sign1[0] * sig1;
            float s1 = matrix.sign1[1] * sig2;
            float s2 = matrix.sign1[2] * sig3;

            Matrix u = un;
            u.M11 = un.M11 * s0; u.M12 = un.M12 * s1; u.M13 = un.M13 * s2; u.M14 = 0;
            u.M21 = un.M21 * s0; u.M22 = un.M22 * s1; u.M23 = un.M23 * s2; u.M24 = 0;
            u.M31 = un.M31 * s0; u.M32 = un.M32 * s1; u.M33 = un.M33 * s2; u.M34 = 0;
            u.M44 = un.M44;
            return u;
        }

        private static Matrix BuildLegacySignTransformation(ReadSymm matrix)
        {
            return new Matrix(
                matrix.sign1[0], 0, 0, 0,
                0, matrix.sign1[1], 0, 0,
                0, 0, matrix.sign1[2], 0,
                0, 0, 0, 1);
        }

        private static float GetM(Matrix m, int row, int col) => (row, col) switch
        {
            (0, 0) => m.M11,
            (0, 1) => m.M12,
            (0, 2) => m.M13,
            (1, 0) => m.M21,
            (1, 1) => m.M22,
            (1, 2) => m.M23,
            (2, 0) => m.M31,
            (2, 1) => m.M32,
            (2, 2) => m.M33,
            _ => 0
        };

        private static void SetM(ref Matrix m, int row, int col, float value)
        {
            switch (row, col)
            {
                case (0, 0): m.M11 = value; break;
                case (0, 1): m.M12 = value; break;
                case (0, 2): m.M13 = value; break;
                case (1, 0): m.M21 = value; break;
                case (1, 1): m.M22 = value; break;
                case (1, 2): m.M23 = value; break;
                case (2, 0): m.M31 = value; break;
                case (2, 1): m.M32 = value; break;
                case (2, 2): m.M33 = value; break;
            }
        }

        #endregion

        #region Symmetry Equivalents

 /// <summary>
 /// Builds SymetryEquivalents z AsymmetricPart.
 /// Positions: R_frac·x + t.
 /// ADP: Un' = Un·R_cartᵀ, Transformation = R_cart (see file header).
 /// </summary>
        public void CalculateSymmetryEquivalents()
        {
            SymetryEquivalents.Clear();

 // A: x_cart = A · x_frac (column). Same as Uij→Ue in Molecule.Data.
            Matrix A = FractionaltoCarttesianMatrix(0, 0, 0);
            bool haveCell = Math.Abs(A.Determinant()) >= 1e-12f;

            EnsureSymmetryOpFlags();

            for (int j = 0; j < AsymmetricPart.Count; j++)
            {
                for (int i = 0; i < SymmetryOperations.Count; i++)
                {
                    // Selective symmetry: skip operations turned off in the UI
                    if (!IsSymmetryOperationEnabled(i))
                        continue;

                    AtomSimple atom_local = new AtomSimple();
                    var matrix = SymmetryOperations[i];

                    atom_local.AtomicNumber = AsymmetricPart[j].AtomicNumber;
                    atom_local.name = AsymmetricPart[j].name;
                    atom_local.occupancy = AsymmetricPart[j].occupancy;
                    atom_local.disorder_group = AsymmetricPart[j].disorder_group;
                    atom_local.SymmetryEquivalentNumberInCell = i;
                    atom_local.charge = AsymmetricPart[j].charge;
                    atom_local.Hydrophobicity = AsymmetricPart[j].Hydrophobicity;
                    atom_local.IndividualMoleculeInCell = -1;
                    atom_local.isAnisotropic = AsymmetricPart[j].isAnisotropic;

                    float[] pos = new float[3];
                    pos[0] = AsymmetricPart[j].x;
                    pos[1] = AsymmetricPart[j].y;
                    pos[2] = AsymmetricPart[j].z;

                    atom_local.x = pos[matrix.pos1[0]] * matrix.sign1[0] + pos[matrix.pos2[0]] * matrix.sign2[0] + matrix.shift[0];
                    atom_local.y = pos[matrix.pos1[1]] * matrix.sign1[1] + pos[matrix.pos2[1]] * matrix.sign2[1] + matrix.shift[1];
                    atom_local.z = pos[matrix.pos1[2]] * matrix.sign1[2] + pos[matrix.pos2[2]] * matrix.sign2[2] + matrix.shift[2];

                    atom_local.x_e = AsymmetricPart[j].x_e;
                    atom_local.y_e = AsymmetricPart[j].y_e;
                    atom_local.z_e = AsymmetricPart[j].z_e;

                    if (haveCell)
                    {
                        BuildRfrac(matrix, out Matrix rFrac);
                        if (TryBuildRcart(A, rFrac, out Matrix rCart))
                        {
 // --- ADP split (see file header diagram) ---
 // Un' = Un · Rᵀ → SDF reads rows as axes; |row| = √|λ|
                            atom_local.Un = TransformUnEigenRows(AsymmetricPart[j].Un, rCart);
                            atom_local.Ue = TransformUnEigenRows(AsymmetricPart[j].Ue, rCart);
 // T = R → Draw: M = T·Un' = R·Un·Rᵀ (≃ old S·Un·S)
 // VoxelScanner / CavityDetector ignore Transformation.
                            atom_local.Transformation = rCart;
                        }
                        else
                        {
                            ApplyLegacyAdp(ref atom_local, AsymmetricPart[j], matrix);
                        }
                    }
                    else
                    {
                        ApplyLegacyAdp(ref atom_local, AsymmetricPart[j], matrix);
                    }

                    if (!IsTheSamePosition(atom_local))
                        SymetryEquivalents.Add(atom_local);
                }
            }
        }

        private static void ApplyLegacyAdp(ref AtomSimple atom_local, AtomSimple src, ReadSymm matrix)
        {
            atom_local.Un = TransformUnSignOnly(src.Un, matrix);
            atom_local.Ue = TransformUnSignOnly(src.Ue, matrix);
            atom_local.Transformation = BuildLegacySignTransformation(matrix);
        }

        private bool IsTheSamePosition(AtomSimple new_record)
        {
            float acceptance = 0.01f;
            foreach (AtomSimple a in SymetryEquivalents)
            {
                if (Math.Abs(a.x - new_record.x) < acceptance &&
                    Math.Abs(a.y - new_record.y) < acceptance &&
                    Math.Abs(a.z - new_record.z) < acceptance)
                    return true;
            }
            return false;
        }


        /// <summary>
        /// Rebuild <see cref="SymetryEquivalents"/> as the full crystallographic unit cell
        /// for energy / force calculations. Temporarily enables ALL symmetry operations
        /// (ignores Display Options asymmetric / partial-op selection), regenerates
        /// equivalents, folds fractional coordinates into [0,1), and removes duplicates.
        /// Restores the previous <see cref="Flags.EnabledSymmetryOps"/> array afterwards.
        /// </summary>
        /// <returns>Number of unique atoms in the unit cell.</returns>
        public int EnsureFullUnitCellForCalculations()
        {
            if (AsymmetricPart == null || AsymmetricPart.Count == 0)
                throw new InvalidOperationException("AsymmetricPart is empty — cannot build unit cell.");

            if (SymmetryOperations == null || SymmetryOperations.Count == 0)
            {
                SymetryEquivalents = new List<AtomSimple>(AsymmetricPart);
                Debug.WriteLine(
                    $"[UnitCell] no symmetry ops → SymetryEquivalents = AsymmetricPart N={SymetryEquivalents.Count}");
                return SymetryEquivalents.Count;
            }

            var saved = Flags.Instance.EnabledSymmetryOps;
            try
            {
                int nOps = SymmetryOperations.Count;
                var allOn = new bool[nOps];
                for (int i = 0; i < nOps; i++) allOn[i] = true;
                Flags.Instance.EnabledSymmetryOps = allOn;

                CalculateSymmetryEquivalents();
                FoldAndDeduplicateSymmetryEquivalents();

                Debug.WriteLine(
                    $"[UnitCell] AsymmetricPart={AsymmetricPart.Count}, " +
                    $"SymmetryOperations={nOps}, SymetryEquivalents={SymetryEquivalents.Count}");
            }
            finally
            {
                Flags.Instance.EnabledSymmetryOps = saved;
            }

            return SymetryEquivalents?.Count ?? 0;
        }

        /// <summary>
        /// Bring each SymetryEquivalents atom into the primary unit cell [0,1)³
        /// and drop near-duplicates (same element within 0.01 in fractional space).
        /// </summary>
        private void FoldAndDeduplicateSymmetryEquivalents()
        {
            if (SymetryEquivalents == null || SymetryEquivalents.Count == 0) return;

            for (int i = 0; i < SymetryEquivalents.Count; i++)
            {
                var a = SymetryEquivalents[i];
                a.x = Fold01(a.x);
                a.y = Fold01(a.y);
                a.z = Fold01(a.z);
                SymetryEquivalents[i] = a;
            }

            const float tol = 0.01f;
            var unique = new List<AtomSimple>(SymetryEquivalents.Count);
            foreach (var a in SymetryEquivalents)
            {
                bool dup = false;
                foreach (var u in unique)
                {
                    if (u.AtomicNumber != a.AtomicNumber) continue;
                    float dx = Math.Abs(u.x - a.x);
                    float dy = Math.Abs(u.y - a.y);
                    float dz = Math.Abs(u.z - a.z);
                    if (dx > 0.5f) dx = 1f - dx;
                    if (dy > 0.5f) dy = 1f - dy;
                    if (dz > 0.5f) dz = 1f - dz;
                    if (dx < tol && dy < tol && dz < tol)
                    {
                        dup = true;
                        break;
                    }
                }
                if (!dup) unique.Add(a);
            }

            if (unique.Count != SymetryEquivalents.Count)
            {
                Debug.WriteLine(
                    $"[UnitCell] fold/dedupe: {SymetryEquivalents.Count} → {unique.Count}");
                SymetryEquivalents = unique;
            }
        }

        private static float Fold01(float x)
        {
            x -= (float)Math.Floor(x);
            if (x < 0f) x += 1f;
            if (x >= 1f) x -= 1f;
            return x;
        }

        #endregion
    }
}