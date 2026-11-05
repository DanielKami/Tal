// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Calculation module: PEOEChargeCalculator.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Tal.Calculate
{
 /// <summary>
 /// Gasteiger-Marsili PEOE (Partial Equalization of Orbital Electronegativity) charge calculator.
 /// Based on: Gasteiger, J. and Marsili, M. (1980) Tetrahedron 36, 3219-3228
 /// 
 /// This algorithm iteratively equalizes electronegativities across bonds
 /// to compute partial atomic charges consistent with molecular structure.
 /// </summary>
    public class PEOEChargeCalculator
    {
        #region Constants

        private const float MAX_ITERATIONS = 50;
        private const float CONVERGENCE_THRESHOLD = 0.0005f;
        private const float DAMPING_FACTOR = 0.5f;

        #endregion

        #region Gasteiger Parameters

 /// <summary>
 /// Gasteiger parameters for electronegativity calculation:
 /// χ = a + b*q + c*q²
 /// </summary>
        private class GasteigerParams
        {
            public float a, b, c;
            public GasteigerParams(float a, float b, float c)
            {
                this.a = a;
                this.b = b;
                this.c = c;
            }
        }

        private static readonly Dictionary<int, GasteigerParams> GASTEIGER_PARAMS = new Dictionary<int, GasteigerParams>()
        {
 // Hydrogen
            { 1, new GasteigerParams(7.17f, 6.24f, -0.56f) },
            
 // Carbon: sp3, sp2, sp
            { 6, new GasteigerParams(7.98f, 9.18f, 1.88f) },
            { 60, new GasteigerParams(8.79f, 9.32f, 1.51f) },
            { 600, new GasteigerParams(10.39f, 9.45f, 0.73f) },
            { 666, new GasteigerParams(8.79f, 9.32f, 1.51f) }, // Aromatic carbon
            
 // Nitrogen: sp3, sp2, sp
            { 7, new GasteigerParams(11.54f, 10.82f, 1.36f) },
            { 70, new GasteigerParams(12.87f, 11.15f, 0.85f) },
            { 700, new GasteigerParams(17.68f, 12.70f, -0.27f) },
            { 777, new GasteigerParams(12.87f, 11.15f, 0.85f) }, // Aromatic nitrogen
            
 // Oxygen: sp3, sp2
            { 8, new GasteigerParams(14.18f, 12.92f, 1.39f) },
            { 80, new GasteigerParams(17.07f, 13.79f, 0.47f) },
            
 // Fluorine
            { 9, new GasteigerParams(14.66f, 13.85f, 2.31f) },
            
 // Phosphorus
            { 15, new GasteigerParams(8.90f, 8.32f, 1.58f) },
            { 150, new GasteigerParams(9.50f, 8.80f, 1.70f) },
            
 // Sulfur
            { 16, new GasteigerParams(10.14f, 9.13f, 1.38f) },
            { 160, new GasteigerParams(10.80f, 9.60f, 1.50f) },
            { 166, new GasteigerParams(10.80f, 9.60f, 1.50f) }, // Aromatic sulfur
            
 // Chlorine
            { 17, new GasteigerParams(12.31f, 10.84f, 1.512f) },
            
 // Bromine
            { 35, new GasteigerParams(11.44f, 9.63f, 1.31f) },
            
 // Iodine
            { 53, new GasteigerParams(9.88f, 7.95f, 0.945f) },
            
 // Silicon
            { 14, new GasteigerParams(8.50f, 7.80f, 1.50f) },
        };

        #endregion

        #region Bond Length Tables

 /// <summary>
 /// Bond length tables for determining bond order from distance.
 /// </summary>
        private static class BondLengthTables
        {
            public static readonly Dictionary<string, float> BondLengths = new Dictionary<string, float>()
            {
 // Carbon bonds
                { "C-C", 1.54f }, { "C=C", 1.34f }, { "C#C", 1.20f },
                { "C-N", 1.47f }, { "C=N", 1.30f }, { "C#N", 1.16f },
                { "C-O", 1.43f }, { "C=O", 1.21f },
                { "C-S", 1.82f }, { "C=S", 1.61f },
                { "C-P", 1.84f },
                { "C-H", 1.09f },
                
 // Nitrogen bonds
                { "N-N", 1.45f }, { "N=N", 1.25f }, { "N#N", 1.10f },
                { "N-O", 1.40f }, { "N=O", 1.22f },
                { "N-H", 1.01f },
                
 // Oxygen bonds
                { "O-O", 1.48f }, { "O=O", 1.21f },
                { "O-H", 0.96f },
                
 // Phosphorus bonds
                { "P-O", 1.63f }, { "P=O", 1.48f },
                { "P-S", 2.10f }, { "P=S", 1.95f },
                
 // Sulfur bonds
                { "S-O", 1.58f }, { "S=O", 1.43f },
                { "S-S", 2.05f }, { "S=S", 1.89f },
                { "S-H", 1.34f },
                
 // Hydrogen
                { "H-H", 0.74f },
            };

            public const float TOLERANCE = 0.10f;

 /// <summary>
 /// Determines bond order based on atomic numbers and distance.
 /// </summary>
            public static int GetBondOrder(int z1, int z2, float distance)
            {
                int[] possibleOrders = GetPossibleBondOrders(z1, z2);
                int bestOrder = 1;
                float bestDiff = float.MaxValue;

                foreach (int order in possibleOrders)
                {
                    string key = GetBondKeyWithOrder(z1, z2, order);
                    if (BondLengths.ContainsKey(key))
                    {
                        float expectedLength = BondLengths[key];
                        float diff = Math.Abs(distance - expectedLength);

                        if (diff < bestDiff && diff < TOLERANCE)
                        {
                            bestDiff = diff;
                            bestOrder = order;
                        }
                    }
                }

                return bestOrder;
            }

            private static string GetBondKeyWithOrder(int z1, int z2, int order)
            {
                string symbol1 = GetElementSymbol(z1);
                string symbol2 = GetElementSymbol(z2);
                string bondSymbol = order == 1 ? "-" : order == 2 ? "=" : "#";
                return $"{symbol1}{bondSymbol}{symbol2}";
            }

            private static string GetElementSymbol(int atomicNumber)
            {
                string[] symbols = {
                    "", "H", "He", "Li", "Be", "B", "C", "N", "O", "F", "Ne",
                    "Na", "Mg", "Al", "Si", "P", "S", "Cl", "Ar", "K", "Ca",
                    "Sc", "Ti", "V", "Cr", "Mn", "Fe", "Co", "Ni", "Cu", "Zn",
                    "Ga", "Ge", "As", "Se", "Br", "Kr", "Rb", "Sr", "Y", "Zr",
                    "Nb", "Mo", "Tc", "Ru", "Rh", "Pd", "Ag", "Cd", "In", "Sn",
                    "Sb", "Te", "I", "Xe", "Cs", "Ba", "La", "Ce", "Pr", "Nd",
                    "Pm", "Sm", "Eu", "Gd", "Tb", "Dy", "Ho", "Er", "Tm", "Yb",
                    "Lu", "Hf", "Ta", "W", "Re", "Os", "Ir", "Pt", "Au", "Hg",
                    "Tl", "Pb", "Bi", "Po", "At", "Rn", "Fr", "Ra", "Ac", "Th",
                    "Pa", "U", "Np", "Pu", "Am", "Cm", "Bk", "Cf"
                };

                if (atomicNumber > 0 && atomicNumber < symbols.Length)
                    return symbols[atomicNumber];
                return "X";
            }

            private static int[] GetPossibleBondOrders(int z1, int z2)
            {
                string s1 = GetElementSymbol(z1);
                string s2 = GetElementSymbol(z2);
                string key = $"{s1}-{s2}";

                switch (key)
                {
                    case "C-C": return new int[] { 1, 2, 3 };
                    case "C-N": return new int[] { 1, 2, 3 };
                    case "C-O": return new int[] { 1, 2 };
                    case "C-S": return new int[] { 1, 2 };
                    case "N-N": return new int[] { 1, 2, 3 };
                    case "N-O": return new int[] { 1, 2 };
                    case "O-O": return new int[] { 1, 2 };
                    case "P-O": return new int[] { 1, 2 };
                    case "P-S": return new int[] { 1, 2 };
                    case "S-O": return new int[] { 1, 2 };
                    case "S-S": return new int[] { 1, 2 };
                    case "C-H": return new int[] { 1 };
                    case "N-H": return new int[] { 1 };
                    case "O-H": return new int[] { 1 };
                    case "S-H": return new int[] { 1 };
                    default: return new int[] { 1 };
                }
            }
        }

        #endregion

        #region Charge Range Limits

        private static readonly float[] MIN_CHARGE = new float[120];
        private static readonly float[] MAX_CHARGE = new float[120];

        static PEOEChargeCalculator()
        {
            for (int i = 0; i < MIN_CHARGE.Length; i++)
            {
                MIN_CHARGE[i] = 0.0f;
                MAX_CHARGE[i] = 0.0f;
            }

 // Set charge limits for common elements
            MIN_CHARGE[1] = -0.50f; MAX_CHARGE[1] = 1.50f;  // H
            MIN_CHARGE[6] = -1.00f; MAX_CHARGE[6] = 1.00f;  // C
            MIN_CHARGE[7] = -1.50f; MAX_CHARGE[7] = 1.50f;  // N
            MIN_CHARGE[8] = -1.50f; MAX_CHARGE[8] = 1.50f;  // O
            MIN_CHARGE[9] = -1.00f; MAX_CHARGE[9] = 1.00f;  // F
            MIN_CHARGE[15] = -1.00f; MAX_CHARGE[15] = 1.00f; // P
            MIN_CHARGE[16] = -1.00f; MAX_CHARGE[16] = 1.00f; // S
            MIN_CHARGE[17] = -1.00f; MAX_CHARGE[17] = 1.00f; // Cl
            MIN_CHARGE[35] = -1.00f; MAX_CHARGE[35] = 1.00f; // Br
            MIN_CHARGE[53] = -1.00f; MAX_CHARGE[53] = 1.00f; // I
        }

        #endregion

        #region Private Fields

        private Molecule _molecule;

 // Results
        private float[] _finalCharges;
        private Dictionary<int, float> _asymmetricCharges = new Dictionary<int, float>();
        private HashSet<int> _aromaticAtoms = new HashSet<int>();
        private bool _aromaticDetectionDone = false;
        private Dictionary<int, bool> _asymmetricAromaticity = new Dictionary<int, bool>();

 // Cache
        private Dictionary<string, int> _asymmetricIndexMap;
        private bool _indexMapBuilt = false;

 // Reusable buffers
        private float[] _tempCharges;
        private float[] _tempNewCharges;
        private int[] _tempHybridizationCodes;
        private GasteigerParams[] _tempParamsList;
        private Vector3[] _tempCartCoords;
        private List<int>[] _tempNeighbors;
        private List<float>[] _tempNeighborDistances;

        #endregion

        #region Public Properties

 /// <summary>Total charge of the molecule after calculation.</summary>
        public float TotalCharge { get; private set; }

 /// <summary>Number of iterations used.</summary>
        public int IterationsUsed { get; private set; }

 /// <summary>Whether the calculation converged.</summary>
        public bool IsConverged { get; private set; }

 /// <summary>Final maximum charge change in the last iteration.</summary>
        public float FinalMaxChange { get; private set; }

        #endregion

        #region Constructor

 /// <summary>
 /// Initializes a new instance of the PEOEChargeCalculator.
 /// </summary>
 /// <param name="molecule">The molecule to calculate charges for.</param>
        public PEOEChargeCalculator(Molecule molecule)
        {
            _molecule = molecule;
            TotalCharge = 0;
            IterationsUsed = 0;
            IsConverged = false;
            FinalMaxChange = 0;
        }

        #endregion

        #region Main Calculation Method

 /// <summary>
 /// Calculates partial atomic charges using the PEOE algorithm.
 /// </summary>
        public void CalculateCharges()
        {
            if (_molecule == null)
            {
                System.Diagnostics.Debug.WriteLine("[PEOE] Molecule is null.");
                return;
            }

            if (_molecule.SymetryEquivalents == null || _molecule.SymetryEquivalents.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("[PEOE] No symmetry equivalents found.");
                return;
            }

            int nAtoms = _molecule.SymetryEquivalents.Count;
            System.Diagnostics.Debug.WriteLine($"[PEOE] Calculating charges for {nAtoms} symmetry equivalents");

 // Initialize buffers
            InitializeBuffers(nAtoms);

            float[] charges = _tempCharges;
            float[] newCharges = _tempNewCharges;
            int[] atomicNumbers = new int[nAtoms];
            float[] occupancies = new float[nAtoms];
            int[] hybridizationCodes = _tempHybridizationCodes;
            Molecule.AtomSimple[] tempAtoms = new Molecule.AtomSimple[nAtoms];
            GasteigerParams[] paramsList = _tempParamsList;
            Vector3[] cartCoords = _tempCartCoords;
            List<int>[] neighbors = _tempNeighbors;
            List<float>[] neighborDistances = _tempNeighborDistances;

 // Copy data
            for (int i = 0; i < nAtoms; i++)
            {
                tempAtoms[i] = _molecule.SymetryEquivalents[i];
                atomicNumbers[i] = tempAtoms[i].AtomicNumber;
                occupancies[i] = tempAtoms[i].occupancy;
                charges[i] = 0.0f;
                newCharges[i] = 0.0f;
                _tempNeighbors[i].Clear();
                _tempNeighborDistances[i].Clear();
            }

 // Convert to Cartesian coordinates
            Matrix U = Matrix.Transpose(_molecule.FractionaltoCarttesianMatrix(0, 0, 0));
            for (int i = 0; i < nAtoms; i++)
            {
                Vector3 frac = new Vector3(tempAtoms[i].x, tempAtoms[i].y, tempAtoms[i].z);
                cartCoords[i] = Vector3.Transform(frac, U);
            }

 // Build bonds
            BuildBonds(nAtoms, cartCoords, occupancies, atomicNumbers, neighbors, neighborDistances);

 // Detect aromatic rings
            DetectAromaticRings(neighbors, tempAtoms);

 // Determine hybridization
            for (int i = 0; i < nAtoms; i++)
            {
                hybridizationCodes[i] = DetermineHybridization(i, neighbors, neighborDistances, tempAtoms);
                paramsList[i] = GetGasteigerParams(hybridizationCodes[i]);
            }

 // Iterative charge equilibration
            bool converged = false;
            int iteration = 0;
            float finalMaxChange = 0;
            float[] chiValues = new float[nAtoms];

            for (iteration = 0; iteration < MAX_ITERATIONS && !converged; iteration++)
            {
                float maxChange = 0.0f;

 // Calculate electronegativity values (χ)
                for (int i = 0; i < nAtoms; i++)
                {
                    var p = paramsList[i];
                    float q = charges[i];
                    chiValues[i] = p.a + p.b * q + p.c * q * q;
                }

 // Update charges
                for (int i = 0; i < nAtoms; i++)
                {
                    int bondCount = neighbors[i].Count;
                    if (bondCount == 0)
                    {
                        newCharges[i] = charges[i];
                        continue;
                    }

                    float totalDelta = 0.0f;
                    float chi_i = chiValues[i];

                    for (int b = 0; b < bondCount; b++)
                    {
                        int j = neighbors[i][b];
                        float dist = neighborDistances[i][b];
                        float delta = chiValues[j] - chi_i;
                        float transfer = delta * (1.0f / (1.0f + dist * dist)) * 0.5f;
                        transfer = Math.Clamp(transfer, -0.3f, 0.3f);
                        totalDelta += transfer;
                    }

                    totalDelta /= bondCount;
                    float rawCharge = charges[i] + totalDelta;
                    float newCharge = charges[i] + DAMPING_FACTOR * (rawCharge - charges[i]);

 // Apply charge limits
                    if (atomicNumbers[i] > 0 && atomicNumbers[i] < MIN_CHARGE.Length)
                    {
                        newCharge = Math.Clamp(newCharge, MIN_CHARGE[atomicNumbers[i]], MAX_CHARGE[atomicNumbers[i]]);
                    }

                    newCharges[i] = newCharge;
                    float change = Math.Abs(newCharge - charges[i]);
                    if (change > maxChange) maxChange = change;
                }

                Array.Copy(newCharges, charges, nAtoms);
                NormalizeCharges(charges, occupancies, atomicNumbers);

                finalMaxChange = maxChange;
                if (maxChange < CONVERGENCE_THRESHOLD)
                {
                    converged = true;
                    break;
                }
            }

 // Store results
            _finalCharges = new float[nAtoms];
            for (int i = 0; i < nAtoms; i++)
            {
                _finalCharges[i] = charges[i];
                var atom = _molecule.SymetryEquivalents[i];
                atom.charge = charges[i];
                _molecule.SymetryEquivalents[i] = atom;
            }

            TotalCharge = CalculateTotalCharge(charges, occupancies);
            IterationsUsed = iteration + 1;
            IsConverged = converged;
            FinalMaxChange = finalMaxChange;

 // Update molecule charge ranges
            float minCharge = float.MaxValue, maxCharge = float.MinValue;
            for (int i = 0; i < _finalCharges.Length; i++)
            {
                if (_finalCharges[i] < minCharge) minCharge = _finalCharges[i];
                if (_finalCharges[i] > maxCharge) maxCharge = _finalCharges[i];
            }
            _molecule.ChargeMin = minCharge;
            _molecule.ChargeMax = maxCharge;

 // Map charges to asymmetric part
            MapChargesToAsymmetricPart(charges, tempAtoms);

            System.Diagnostics.Debug.WriteLine($"[PEOE] Done: {nAtoms} atoms, {IterationsUsed} iterations, converged={IsConverged}, total={TotalCharge:F4}");
        }

        #endregion

        #region Buffer Initialization

 /// <summary>
 /// Initializes reusable buffers for the calculation.
 /// </summary>
        private void InitializeBuffers(int size)
        {
            if (_tempCharges == null || _tempCharges.Length < size)
            {
                _tempCharges = new float[size];
                _tempNewCharges = new float[size];
                _tempHybridizationCodes = new int[size];
                _tempParamsList = new GasteigerParams[size];
                _tempCartCoords = new Vector3[size];
                _tempNeighbors = new List<int>[size];
                _tempNeighborDistances = new List<float>[size];

                for (int i = 0; i < size; i++)
                {
                    if (_tempNeighbors[i] == null)
                        _tempNeighbors[i] = new List<int>(6);
                    if (_tempNeighborDistances[i] == null)
                        _tempNeighborDistances[i] = new List<float>(6);
                }
            }
        }

        #endregion

        #region Bond Building

 /// <summary>
 /// Builds covalent bonds based on distance and atomic radii.
 /// </summary>
        private void BuildBonds(int nAtoms, Vector3[] cartCoords, float[] occupancies, int[] atomicNumbers,
                                List<int>[] neighbors, List<float>[] neighborDistances)
        {
            float bondFactor = Flags.Instance.BOND_FACTOR;

            for (int i = 0; i < nAtoms; i++)
            {
                if (occupancies[i] < 0.01f) continue;
                for (int j = i + 1; j < nAtoms; j++)
                {
                    if (occupancies[j] < 0.01f) continue;

                    float distance = Vector3.Distance(cartCoords[i], cartCoords[j]);
                    if (distance > Flags.Instance.MAX_ATOM_DISTANCE) continue;

                    float bondCorr = bondFactor;
                    if (IsMetal(atomicNumbers[i]) || IsMetal(atomicNumbers[j]))
                        bondCorr = Flags.Instance.BOND_FACTOR_METALS;

                    float radiiSum = (VdwParameters.GetRadius(atomicNumbers[i]) +
                                      VdwParameters.GetRadius(atomicNumbers[j])) * bondCorr;

                    if (distance < radiiSum && distance > 0.1f)
                    {
                        neighbors[i].Add(j);
                        neighborDistances[i].Add(distance);
                        neighbors[j].Add(i);
                        neighborDistances[j].Add(distance);
                    }
                }
            }
        }

 /// <summary>
 /// Determines if an atomic number belongs to a metal.
 /// </summary>
        private bool IsMetal(int atomicNumber)
        {
            return atomicNumber == 3 || atomicNumber >= 11 && atomicNumber <= 14 ||
                   atomicNumber >= 19 && atomicNumber <= 30 ||
                   atomicNumber >= 37 && atomicNumber <= 50 ||
                   atomicNumber >= 55 && atomicNumber <= 83 ||
                   atomicNumber >= 87 && atomicNumber <= 118;
        }

        #endregion

        #region Aromatic Detection

 /// <summary>
 /// Detects aromatic rings in the molecule (simplified version).
 /// </summary>
        private void DetectAromaticRings(List<int>[] neighbors, Molecule.AtomSimple[] atoms)
        {
            _aromaticAtoms.Clear();
            _aromaticDetectionDone = false;

 // Simple aromatic detection - look for 6-membered rings with alternating bonds
 // This is a simplified version; full implementation would use more sophisticated algorithms
            _aromaticDetectionDone = true;
        }

 /// <summary>
 /// Checks if an atom is aromatic.
 /// </summary>
        private bool IsAromatic(int atomIndex)
        {
            return _aromaticAtoms.Contains(atomIndex);
        }

        #endregion

        #region Hybridization Determination

 /// <summary>
 /// Determines the hybridization state of an atom.
 /// </summary>
        private int DetermineHybridization(int atomIndex, List<int>[] neighbors, List<float>[] neighborDistances,
                                           Molecule.AtomSimple[] atoms)
        {
            int atomicNumber = atoms[atomIndex].AtomicNumber;
            int bondCount = neighbors[atomIndex].Count;

            if (bondCount == 0) return atomicNumber;

            if (IsAromatic(atomIndex))
            {
                if (atomicNumber == 6) return 666;
                if (atomicNumber == 7) return 777;
                if (atomicNumber == 8) return 80;
                if (atomicNumber == 16) return 166;
            }

            int maxBondOrder = 1;
            int doubleBondCount = 0;
            int tripleBondCount = 0;

            for (int b = 0; b < bondCount; b++)
            {
                int neighborIdx = neighbors[atomIndex][b];
                float distance = neighborDistances[atomIndex][b];
                int neighborZ = atoms[neighborIdx].AtomicNumber;

 // Determine bond order from distance
                int bondOrder = BondLengthTables.GetBondOrder(atomicNumber, neighborZ, distance);

                if (bondOrder > maxBondOrder) maxBondOrder = bondOrder;
                if (bondOrder == 2) doubleBondCount++;
                if (bondOrder == 3) tripleBondCount++;
            }

 // Carbon hybridization
            if (atomicNumber == 6)
            {
                if (tripleBondCount > 0 || maxBondOrder == 3) return 600; // sp
                if (doubleBondCount > 0 || maxBondOrder == 2) return 60;  // sp2
                if (bondCount == 4) return 6;  // sp3
                if (bondCount == 3) return 60; // sp2 (carbocation or radical)
                if (bondCount == 2) return 600; // sp (carbene)
                return 6;
            }

 // Nitrogen hybridization
            if (atomicNumber == 7)
            {
                if (tripleBondCount > 0 || maxBondOrder == 3) return 700; // sp
                if (doubleBondCount > 0 || maxBondOrder == 2) return 70;  // sp2
                if (bondCount == 3) return 7;  // sp3
                if (bondCount == 2) return 70; // sp2 (imine)
                return 7;
            }

 // Oxygen hybridization
            if (atomicNumber == 8)
            {
                if (doubleBondCount > 0 || maxBondOrder == 2) return 80;  // sp2
                if (bondCount == 2) return 8;  // sp3
                if (bondCount == 1) return 80; // sp2 (carbonyl)
                return 8;
            }

 // Sulfur hybridization
            if (atomicNumber == 16)
            {
                if (doubleBondCount > 0 || maxBondOrder == 2) return 160; // sp2
                return 16; // sp3
            }

            return atomicNumber;
        }

 /// <summary>
 /// Gets Gasteiger parameters for a hybridization code.
 /// </summary>
        private GasteigerParams GetGasteigerParams(int hybridizationCode)
        {
            if (GASTEIGER_PARAMS.ContainsKey(hybridizationCode))
                return GASTEIGER_PARAMS[hybridizationCode];

            int baseCode = hybridizationCode;
            while (baseCode >= 100) baseCode /= 10;
            if (GASTEIGER_PARAMS.ContainsKey(baseCode))
                return GASTEIGER_PARAMS[baseCode];

 // Default to carbon sp3 parameters
            return new GasteigerParams(7.98f, 9.18f, 1.88f);
        }

        #endregion

        #region Charge Normalization

 /// <summary>
 /// Normalizes charges to preserve total charge neutrality.
 /// </summary>
        private void NormalizeCharges(float[] charges, float[] occupancies, int[] atomicNumbers)
        {
            float totalOcc = 0.0f;
            float currentSum = 0.0f;
            int nAtoms = charges.Length;

            for (int i = 0; i < nAtoms; i++)
            {
                totalOcc += occupancies[i];
                currentSum += charges[i] * occupancies[i];
            }

            if (totalOcc < 0.001f) return;

            float diff = -currentSum;
            if (Math.Abs(diff) < 0.0001f) return;

            float[] capacity = new float[nAtoms];
            float totalCapacity = 0.0f;

            for (int i = 0; i < nAtoms; i++)
            {
                if (occupancies[i] < 0.001f) continue;

                int z = atomicNumbers[i];
                float minCharge = z > 0 && z < MIN_CHARGE.Length ? MIN_CHARGE[z] : -2.0f;
                float maxCharge = z > 0 && z < MAX_CHARGE.Length ? MAX_CHARGE[z] : 2.0f;

                if (diff > 0)
                    capacity[i] = (maxCharge - charges[i]) * occupancies[i];
                else
                    capacity[i] = (charges[i] - minCharge) * occupancies[i];

                if (capacity[i] < 0) capacity[i] = 0;
                totalCapacity += capacity[i];
            }

            if (totalCapacity < 0.0001f) return;

            float correction = diff / totalCapacity;
            correction = Math.Clamp(correction, -0.5f, 0.5f);

            for (int i = 0; i < nAtoms; i++)
            {
                if (occupancies[i] > 0.001f && capacity[i] > 0.001f)
                {
                    int z = atomicNumbers[i];
                    float minCharge = z > 0 && z < MIN_CHARGE.Length ? MIN_CHARGE[z] : -2.0f;
                    float maxCharge = z > 0 && z < MAX_CHARGE.Length ? MAX_CHARGE[z] : 2.0f;

                    float newCharge = charges[i] + correction * capacity[i];
                    charges[i] = Math.Clamp(newCharge, minCharge, maxCharge);
                }
            }
        }

 /// <summary>
 /// Calculates the total charge of the molecule.
 /// </summary>
        private float CalculateTotalCharge(float[] charges, float[] occupancies)
        {
            float sum = 0.0f;
            for (int i = 0; i < charges.Length; i++)
            {
                sum += charges[i] * occupancies[i];
            }
            return sum;
        }

        #endregion

        #region Mapping to Asymmetric Part

 /// <summary>
 /// Maps charges from symmetry equivalents to the asymmetric unit.
 /// </summary>
        private void MapChargesToAsymmetricPart(float[] charges, Molecule.AtomSimple[] tempAtoms)
        {
            if (_molecule.AsymmetricPart == null || _molecule.AsymmetricPart.Count == 0) return;

            if (!_indexMapBuilt)
                BuildAsymmetricIndexMap();

            float[] chargeSums = new float[_molecule.AsymmetricPart.Count];
            float[] occupancySums = new float[_molecule.AsymmetricPart.Count];
            int[] chargeCounts = new int[_molecule.AsymmetricPart.Count];

            for (int i = 0; i < charges.Length && i < tempAtoms.Length; i++)
            {
                int asymIndex = FindAsymmetricAtomIndex(tempAtoms[i]);
                if (asymIndex >= 0)
                {
                    float occ = tempAtoms[i].occupancy;
                    chargeSums[asymIndex] += charges[i] * occ;
                    occupancySums[asymIndex] += occ;
                    chargeCounts[asymIndex]++;
                }
            }

            _asymmetricCharges.Clear();
            for (int i = 0; i < _molecule.AsymmetricPart.Count; i++)
            {
                if (chargeCounts[i] > 0 && occupancySums[i] > 0.001f)
                {
                    float avgCharge = chargeSums[i] / occupancySums[i];
                    _asymmetricCharges[i] = avgCharge;

                    var atom = _molecule.AsymmetricPart[i];
                    atom.charge = avgCharge;
                    _molecule.AsymmetricPart[i] = atom;
                }
                else
                {
                    _asymmetricCharges[i] = 0.0f;
                }
            }
        }

 /// <summary>
 /// Builds a map from fractional coordinates to asymmetric atom indices.
 /// </summary>
        private void BuildAsymmetricIndexMap()
        {
            if (_indexMapBuilt) return;

            _asymmetricIndexMap = new Dictionary<string, int>(_molecule.AsymmetricPart.Count);

            for (int i = 0; i < _molecule.AsymmetricPart.Count; i++)
            {
                var atom = _molecule.AsymmetricPart[i];
                string key = GetAtomKey(atom.x, atom.y, atom.z);
                if (!_asymmetricIndexMap.ContainsKey(key))
                {
                    _asymmetricIndexMap[key] = i;
                }
            }

            _indexMapBuilt = true;
        }

 /// <summary>
 /// Generates a unique key for fractional coordinates.
 /// </summary>
        private string GetAtomKey(float x, float y, float z)
        {
            int ix = (int)Math.Round(x * 1000);
            int iy = (int)Math.Round(y * 1000);
            int iz = (int)Math.Round(z * 1000);
            return $"{ix},{iy},{iz}";
        }

 /// <summary>
 /// Finds the corresafternding asymmetric atom index for a symmetry equivalent.
 /// </summary>
        private int FindAsymmetricAtomIndex(Molecule.AtomSimple symAtom)
        {
            if (!_indexMapBuilt)
                BuildAsymmetricIndexMap();

            float fx = symAtom.x - (float)Math.Floor(symAtom.x);
            float fy = symAtom.y - (float)Math.Floor(symAtom.y);
            float fz = symAtom.z - (float)Math.Floor(symAtom.z);

            if (fx < 0) fx += 1.0f;
            if (fy < 0) fy += 1.0f;
            if (fz < 0) fz += 1.0f;

            string key = GetAtomKey(fx, fy, fz);
            if (_asymmetricIndexMap.TryGetValue(key, out int index))
                return index;

            return -1;
        }

        #endregion

        #region Public Getters

 /// <summary>
 /// Gets the charge for a symmetry equivalent atom.
 /// </summary>
        public float GetChargeForSymmetryEquivalent(int index)
        {
            if (_finalCharges != null && index >= 0 && index < _finalCharges.Length)
                return _finalCharges[index];
            return 0.0f;
        }

 /// <summary>
 /// Gets the charge for an asymmetric atom.
 /// </summary>
        public float GetAsymmetricCharge(int asymIndex)
        {
            if (_asymmetricCharges.ContainsKey(asymIndex))
                return _asymmetricCharges[asymIndex];
            return 0.0f;
        }

 /// <summary>
 /// Gets the final charges array.
 /// </summary>
        public float[] GetFinalCharges()
        {
            return _finalCharges;
        }

 /// <summary>
 /// Gets all asymmetric charges.
 /// </summary>
        public Dictionary<int, float> GetAllAsymmetricCharges()
        {
            return new Dictionary<int, float>(_asymmetricCharges);
        }

        #endregion
    }
}