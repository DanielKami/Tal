// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cache of MACE forces. Cart positions use the SAME convention as Presentation:
//   U = Transpose(FractionaltoCarttesianMatrix);  cart = Transform(frac, U)
// Apply matches display atoms with MoleculeShift undone + lattice minimum-image.

using Microsoft.Xna.Framework;

namespace Tal.Calculate
{
    public static class MaceForceCache
    {
        public struct Entry
        {
            public string Name;
            public int Z;
            public int SymId;
            public int MolId;
            public float FracX, FracY, FracZ;
            public Vector3 Cart;
            public Vector3 Force;
            public Vector3 ForceElectro;
            public Vector3 ForceDispersion;
            public Vector3 ForceRepulsion;
            public bool HasForce;
        }

        private static readonly List<Entry> _entries = new();
        private static float _energyPerCell;
        private static float _energySupercell;
        private static bool _hasEnergy;

        public static bool HasData => _entries.Count > 0;
        public static float EnergyPerCell => _energyPerCell;
        public static float EnergySupercell => _energySupercell;
        public static bool HasEnergy => _hasEnergy;

        public static void Clear()
        {
            _entries.Clear();
            _hasEnergy = false;
            _energyPerCell = 0;
            _energySupercell = 0;
        }

        /// <summary>
        /// Store forces in SymetryEquivalents order (= MACE CentralCellForces).
        /// Cartesian = Presentation convention (Transpose + Transform).
        /// </summary>
        public static void StoreFromSymmetryEquivalents(
            Molecule mol, float[,] forcesN3, float energyPerCell, float energySupercell)
        {
            _entries.Clear();
            if (mol?.SymetryEquivalents == null || forcesN3 == null) return;

            // Same matrix as Molecule.Presentation.Calculate_SymmetryAtoms3D
            Matrix U = Matrix.Transpose(mol.FractionaltoCarttesianMatrix(0, 0, 0));
            int n = Math.Min(mol.SymetryEquivalents.Count, forcesN3.GetLength(0));
            for (int i = 0; i < n; i++)
            {
                var s = mol.SymetryEquivalents[i];
                var frac = new Vector3(s.x, s.y, s.z);
                var cart = Vector3.Transform(frac, U);

                _entries.Add(new Entry
                {
                    Name = s.name ?? "",
                    Z = s.AtomicNumber,
                    SymId = s.SymmetryEquivalentNumberInCell,
                    MolId = s.IndividualMoleculeInCell,
                    FracX = s.x,
                    FracY = s.y,
                    FracZ = s.z,
                    Cart = cart,
                    Force = new Vector3(forcesN3[i, 0], forcesN3[i, 1], forcesN3[i, 2]),
                    HasForce = true
                });
            }

            _energyPerCell = energyPerCell;
            _energySupercell = energySupercell;
            _hasEnergy = true;
        }

        public static void StoreFromMolecule(Molecule mol, int[] indices,
            float energyPerCell, float energySupercell)
        {
            _entries.Clear();
            if (mol?.atom == null || indices == null) return;

            foreach (int i in indices)
            {
                if (i < 0 || i >= mol.NumberOfAtoms) continue;
                var a = mol.atom[i];
                _entries.Add(new Entry
                {
                    Name = a.name ?? "",
                    Z = a.AtomicNumber,
                    SymId = a.SymmetryEquivalentNumberInCell,
                    MolId = a.IndividualMoleculeInCell,
                    Cart = a.Coordinate + mol.MoleculeShift,
                    Force = a.Force,
                    ForceElectro = a.ForceElectro,
                    ForceDispersion = a.ForceDispersion,
                    ForceRepulsion = a.ForceRepulsion,
                    HasForce = a.HasForce
                });
            }

            _energyPerCell = energyPerCell;
            _energySupercell = energySupercell;
            _hasEnergy = true;
        }

        /// <summary>
        /// Map cached forces onto mol.atom[].
        /// Display coords are Presentation: Transform(frac, U^T) − MoleculeShift.
        /// Matching undoes MoleculeShift and uses lattice minimum-image (±1 cell).
        /// </summary>
        public static int ApplyToMolecule(Molecule mol)
        {
            if (mol?.atom == null || _entries.Count == 0) return 0;

            for (int i = 0; i < mol.NumberOfAtoms; i++)
            {
                var a = mol.atom[i];
                a.HasForce = false;
                a.Force = Vector3.Zero;
                a.ForceElectro = Vector3.Zero;
                a.ForceDispersion = Vector3.Zero;
                a.ForceRepulsion = Vector3.Zero;
                mol.atom[i] = a;
            }

            Matrix raw = mol.FractionaltoCarttesianMatrix(0, 0, 0);

            // ═══════════════════════════════════════════════════════════
            // ⭐ POPRAWKA: wektory a/b/c to KOLUMNY macierzy raw,
            //    nie wiersze. FractionaltoCarttesianMatrix ma wektory
            //    bazy w kolumnach (konwencja MACE/ASE "cell").
            //
            //    Kolumna 1 = a: (M11, M21, M31)
            //    Kolumna 2 = b: (M12, M22, M32)
            //    Kolumna 3 = c: (M13, M23, M33)
            // ═══════════════════════════════════════════════════════════
            Vector3 aVec = new Vector3(raw.M11, raw.M21, raw.M31);
            Vector3 bVec = new Vector3(raw.M12, raw.M22, raw.M32);
            Vector3 cVec = new Vector3(raw.M13, raw.M23, raw.M33);

            Vector3 shift = mol.MoleculeShift;

            // Tolerancja dopasowania — 0.1 Å (wcześniej 1 Å było za luźne
            // dla gęstych struktur, mogło dopasować zły atom tego samego Z).
            const float tol2 = 0.1f * 0.1f;

            int applied = 0;
            var claimed = new bool[_entries.Count];

            // Pass 1: unique nearest match (prefer unclaimed entries)
            for (int i = 0; i < mol.NumberOfAtoms; i++)
            {
                var a = mol.atom[i];
                if (a.AtomicNumber <= 0) continue;

                // Undo display centering
                Vector3 p = a.Coordinate + shift;

                int best = -1;
                float bestD2 = float.MaxValue;
                for (int e = 0; e < _entries.Count; e++)
                {
                    if (claimed[e]) continue;
                    if (_entries[e].Z != a.AtomicNumber) continue;
                    float d2 = MinImageD2(p, _entries[e].Cart, aVec, bVec, cVec);
                    if (d2 < bestD2)
                    {
                        bestD2 = d2;
                        best = e;
                    }
                }

                // If all claimed for this Z, allow reuse (packing images)
                if (best < 0 || bestD2 > tol2)
                {
                    best = -1;
                    bestD2 = float.MaxValue;
                    for (int e = 0; e < _entries.Count; e++)
                    {
                        if (_entries[e].Z != a.AtomicNumber) continue;
                        float d2 = MinImageD2(p, _entries[e].Cart, aVec, bVec, cVec);
                        if (d2 < bestD2)
                        {
                            bestD2 = d2;
                            best = e;
                        }
                    }
                }

                if (best < 0 || bestD2 > tol2) continue;

                claimed[best] = true;
                var be = _entries[best];
                a.Force = be.Force;
                a.ForceElectro = be.ForceElectro;
                a.ForceDispersion = be.ForceDispersion;
                a.ForceRepulsion = be.ForceRepulsion;
                a.HasForce = be.HasForce;
                if (be.MolId >= 0)
                    a.IndividualMoleculeInCell = be.MolId;
                mol.atom[i] = a;
                if (a.HasForce) applied++;
            }

            return applied;
        }

        /// <summary>
        /// Minimalna odległość kwadratowa z uwzględnieniem obrazów komórki (±1 w każdym kierunku).
        /// Wektory a/b/c muszą być wektorami bazy w KOLUMNACH macierzy frakcyjne→kartezjańskie.
        /// </summary>
        private static float MinImageD2(Vector3 p, Vector3 q, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 d0 = p - q;
            float best = d0.LengthSquared();
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    for (int k = -1; k <= 1; k++)
                    {
                        if (i == 0 && j == 0 && k == 0) continue;
                        Vector3 d = d0 + i * a + j * b + k * c;
                        float d2 = d.LengthSquared();
                        if (d2 < best) best = d2;
                    }
            return best;
        }
    }
}