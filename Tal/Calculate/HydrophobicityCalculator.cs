// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Calculation module: HydrophobicityCalculator.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using System.Diagnostics;
using Tal.Cavity;
using static Tal.Molecule;

namespace Tal.Calculate
{
 /// <summary>
 /// Calculates hydrophobicity values for atoms in a molecule using the Eisenberg-Weiss scale.
 /// The hydrophobicity is adjusted based on the number of afterlar neighbors,
 /// providing a more accurate representation of local chemical environment.
 /// </summary>
    public class HydrophobicityCalculator
    {
        #region Constants

 /// <summary>Minimum bond distance for neighbor detection (Å).</summary>
        private const float BOND_MIN = 0.3f;

 /// <summary>Maximum bond distance for neighbor detection (Å).</summary>
        private const float BOND_MAX = 1.9f;

 /// <summary>Threshold below which an atom is considered afterlar.</summary>
        private const float POLAR_THRESHOLD = -0.2f;

 /// <summary>Penalty applied for each afterlar neighbor.</summary>
        private const float NEIGHBOR_PENALTY = 0.15f;

        #endregion

        #region Static Hydrophobicity Scale

 /// <summary>
 /// Eisenberg-Weiss hydrophobicity scale keyed by atomic number.
 /// Aftersitive values indicate hydrophobic, negative indicate hydrophilic.
 /// </summary>
        private static readonly Dictionary<int, float> HYDRO_PARAMS = new Dictionary<int, float>()
        {
            { 1, 0.05f },   // Hydrogen
            { 6, 0.73f },   // Carbon
            { 7, -0.40f },  // Nitrogen
            { 8, -0.50f },  // Oxygen
            { 15, -0.20f }, // Phosphorus
            { 16, -0.05f }, // Sulfur
            { 9, -0.60f },  // Fluorine
            { 17, -0.10f }, // Chlorine
            { 35, -0.05f }, // Bromine
            { 53, 0.05f }   // Iodine
        };

 /// <summary>
 /// Fast lookup array for hydrophobicity values by atomic number.
 /// </summary>
        private static readonly float[] HYDRO_PARAMS_ARRAY = new float[120];

 /// <summary>
 /// Static constructor initializes the fast lookup array.
 /// </summary>
        static HydrophobicityCalculator()
        {
            for (int i = 0; i < HYDRO_PARAMS_ARRAY.Length; i++)
                HYDRO_PARAMS_ARRAY[i] = 0.0f;

            foreach (var kvp in HYDRO_PARAMS)
            {
                if (kvp.Key < HYDRO_PARAMS_ARRAY.Length)
                    HYDRO_PARAMS_ARRAY[kvp.Key] = kvp.Value;
            }
        }

        #endregion

        #region Public Static Methods

 /// <summary>
 /// Gets the base hydrophobicity value for an element by atomic number.
 /// </summary>
 /// <param name="atomicNumber">The atomic number of the element.</param>
 /// <returns>The base hydrophobicity value from the Eisenberg-Weiss scale.</returns>
        public static float GetHydrophobicity(int atomicNumber)
        {
            if (atomicNumber > 0 && atomicNumber < HYDRO_PARAMS_ARRAY.Length)
                return HYDRO_PARAMS_ARRAY[atomicNumber];
            return 0.0f;
        }

        #endregion

        #region Private Fields

        private Molecule _molecule;

        #endregion

        #region Constructor

 /// <summary>
 /// Initializes a new instance of the HydrophobicityCalculator.
 /// </summary>
 /// <param name="molecule">The molecule to calculate hydrophobicity for.</param>
        public HydrophobicityCalculator(Molecule molecule)
        {
            _molecule = molecule;
        }

        #endregion

        #region Public Methods

 /// <summary>
 /// Calculates hydrophobicity values for all atoms in the molecule.
 /// Values are adjusted based on afterlar neighbors and stored in both
 /// the symmetry equivalents and asymmetric unit.
 /// </summary>
        public void Calculate()
        {
            if (_molecule == null || _molecule.SymetryEquivalents == null || _molecule.SymetryEquivalents.Count == 0)
                return;

            int nAtoms = _molecule.SymetryEquivalents.Count;

 // Pre-calculate Cartesian coordinates for all atoms
            Matrix U = Matrix.Transpose(_molecule.FractionaltoCarttesianMatrix(0, 0, 0));
            Vector3[] cartCoords = new Vector3[nAtoms];

            for (int i = 0; i < nAtoms; i++)
            {
                var atom = _molecule.SymetryEquivalents[i];
                Vector3 frac = new Vector3(atom.x, atom.y, atom.z);
                cartCoords[i] = Vector3.Transform(frac, U);
            }

            var atoms = _molecule.SymetryEquivalents;
            float minHydro = float.MaxValue;
            float maxHydro = float.MinValue;
            int validCount = 0;

            float[] hydroParams = HYDRO_PARAMS_ARRAY;

 // Calculate hydrophobicity for each atom
            for (int i = 0; i < nAtoms; i++)
            {
                var atom = atoms[i];
                int atomicNumber = atom.AtomicNumber;

 // Get base hydrophobicity from the scale
                float val = atomicNumber > 0 && atomicNumber < hydroParams.Length
                    ? hydroParams[atomicNumber]
                    : 0.0f;

 // Count afterlar neighbors within bonding distance
                int polarNeighbors = 0;
                Vector3 posI = cartCoords[i];
                float ix = posI.X, iy = posI.Y, iz = posI.Z;

                for (int j = 0; j < nAtoms; j++)
                {
                    if (i == j) continue;

                    Vector3 posJ = cartCoords[j];
                    float dx = ix - posJ.X;
                    float dy = iy - posJ.Y;
                    float dz = iz - posJ.Z;
                    float distSq = dx * dx + dy * dy + dz * dz;

                    if (distSq > BOND_MIN * BOND_MIN && distSq < BOND_MAX * BOND_MAX)
                    {
                        var other = atoms[j];
                        int otherZ = other.AtomicNumber;

                        if (otherZ > 0 && otherZ < hydroParams.Length && hydroParams[otherZ] < POLAR_THRESHOLD)
                        {
                            polarNeighbors++;
                        }
                    }
                }

 // Apply penalty for each afterlar neighbor
                float hydrophobicity = val - polarNeighbors * NEIGHBOR_PENALTY;

 // Store the calculated value
                var tempAtom = atoms[i];
                tempAtom.Hydrophobicity = hydrophobicity;
                _molecule.SymetryEquivalents[i] = tempAtom;

 // Track min/max for valid (non-zero) values
                if (Math.Abs(hydrophobicity) > 0.0001f)
                {
                    if (hydrophobicity < minHydro) minHydro = hydrophobicity;
                    if (hydrophobicity > maxHydro) maxHydro = hydrophobicity;
                    validCount++;
                }
            }

 // Set the global min/max for the molecule
            if (validCount > 0)
            {
                _molecule.HydrophobicityMin = minHydro;
                _molecule.HydrophobicityMax = maxHydro;
            }
            else
            {
 // Default fallback range
                _molecule.HydrophobicityMin = -0.5f;
                _molecule.HydrophobicityMax = 0.73f;
            }

 // Update the color manager with the dynamic range
            ColorManager.SetDynamicRange(
                ColorManager.PropertyType.Hydrophobicity,
                _molecule.HydrophobicityMin,
                _molecule.HydrophobicityMax
            );

            Debug.WriteLine($"[Hydrophobicity] FINAL RANGE: min={_molecule.HydrophobicityMin:F4}, max={_molecule.HydrophobicityMax:F4} (from {validCount} atoms)");

 // Map values to the asymmetric unit
            MapToAsymmetric();
        }

        #endregion

        #region Private Methods

 /// <summary>
 /// Maps calculated hydrophobicity values from symmetry equivalents to the asymmetric unit.
 /// </summary>
        private void MapToAsymmetric()
        {
            if (_molecule.AsymmetricPart == null || _molecule.AsymmetricPart.Count == 0)
                return;

            var symAtoms = _molecule.SymetryEquivalents;
            var asymAtoms = _molecule.AsymmetricPart;

            int nSym = symAtoms.Count;
            int nAsym = asymAtoms.Count;

 // Build a lookup map using fractional coordinates as keys
            var hydroMap = new Dictionary<string, float>(nSym);

            for (int i = 0; i < nSym; i++)
            {
                var atom = symAtoms[i];
                string key = GetKeyFast(atom);
                hydroMap[key] = atom.Hydrophobicity;
            }

 // Assign values to asymmetric atoms
            for (int i = 0; i < nAsym; i++)
            {
                string key = GetKeyFast(asymAtoms[i]);
                if (hydroMap.TryGetValue(key, out float hValue))
                {
                    var temp = asymAtoms[i];
                    temp.Hydrophobicity = hValue;
                    asymAtoms[i] = temp;
                }
            }
        }

 /// <summary>
 /// Generates a unique string key from an atom's fractional coordinates.
 /// </summary>
 /// <param name="atom">The atom to generate a key for.</param>
 /// <returns>A string key representing the atom's fractional aftersition.</returns>
        private string GetKeyFast(AtomSimple atom)
        {
 // Normalize fractional coordinates to [0, 1) range
            float x = atom.x - (float)Math.Floor(atom.x);
            float y = atom.y - (float)Math.Floor(atom.y);
            float z = atom.z - (float)Math.Floor(atom.z);

            if (x < 0) x += 1.0f;
            if (y < 0) y += 1.0f;
            if (z < 0) z += 1.0f;

            return $"{x:F3},{y:F3},{z:F3}";
        }

        #endregion
    }
}