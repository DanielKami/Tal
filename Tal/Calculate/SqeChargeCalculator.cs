// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Calculation module: SqeChargeCalculator.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace Tal.Calculate
{
 /// <summary>
 /// SQE (Simple Quantum Electronegativity) charge calculator.
 /// Based on electronegativity equalization principles with 
 /// element-specific parameters for rapid charge assignment.
 /// </summary>
    public class SqeChargeCalculator
    {
        #region Constants

        private const double DEFAULT_TOTAL_CHARGE = 0.0;
        private const int POSITION_PRECISION = 1000;
        private const double CHARGE_SCALE = 8.0;
        private const double MAX_CORRECTION = 0.2;

        #endregion

        #region Private Fields

        private Molecule _molecule;
        private StringBuilder _debugLog = new StringBuilder();

        private float[] _finalCharges;
        private Dictionary<int, float> _asymmetricCharges = new Dictionary<int, float>();

        #endregion

        #region Private Classes

 /// <summary>
 /// SQE parameters for an element: electronegativity (Chi),
 /// hardness (Eta), and bond ionicity (Beta).
 /// </summary>
        private class SqeParams
        {
            public double Chi;
            public double Eta;
            public double Beta;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SqeParams(double chi, double eta, double beta)
            {
                Chi = chi;
                Eta = eta;
                Beta = beta;
            }
        }

        #endregion

        #region Static Arrays

        private static readonly SqeParams[] PARAMS_ARRAY = new SqeParams[800];
        private static readonly string[] ELEMENT_NAMES_ARRAY = new string[110];
        private static readonly int[] HYBRIDIZATION_MAP = new int[110];

        #endregion

        #region Static Constructor

        static SqeChargeCalculator()
        {
            try
            {
 // Group 1 - Alkali Metals
                PARAMS_ARRAY[1] = new SqeParams(4.41, 12.83, 0.50);
                PARAMS_ARRAY[3] = new SqeParams(2.50, 8.00, 0.30);
                PARAMS_ARRAY[11] = new SqeParams(2.20, 7.00, 0.25);
                PARAMS_ARRAY[19] = new SqeParams(2.00, 6.50, 0.20);
                PARAMS_ARRAY[37] = new SqeParams(1.80, 6.00, 0.20);
                PARAMS_ARRAY[55] = new SqeParams(1.60, 5.50, 0.18);
                PARAMS_ARRAY[87] = new SqeParams(1.50, 5.00, 0.15);

 // Group 2 - Alkaline Earth Metals
                PARAMS_ARRAY[4] = new SqeParams(3.00, 9.00, 0.35);
                PARAMS_ARRAY[12] = new SqeParams(2.50, 8.00, 0.30);
                PARAMS_ARRAY[20] = new SqeParams(2.20, 7.00, 0.28);
                PARAMS_ARRAY[38] = new SqeParams(2.00, 6.50, 0.25);
                PARAMS_ARRAY[56] = new SqeParams(1.80, 6.00, 0.22);
                PARAMS_ARRAY[88] = new SqeParams(1.60, 5.50, 0.20);

 // Group 3
                PARAMS_ARRAY[5] = new SqeParams(4.00, 10.00, 0.40);
                PARAMS_ARRAY[13] = new SqeParams(3.50, 9.00, 0.35);
                PARAMS_ARRAY[21] = new SqeParams(3.00, 8.50, 0.32);
                PARAMS_ARRAY[31] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[39] = new SqeParams(3.00, 8.00, 0.30);
                PARAMS_ARRAY[49] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[57] = new SqeParams(3.00, 8.00, 0.30);
                PARAMS_ARRAY[89] = new SqeParams(3.00, 8.00, 0.30);

 // Group 4 - Carbon, Silicon, etc.
                PARAMS_ARRAY[6] = new SqeParams(5.39, 10.15, 0.35);
                PARAMS_ARRAY[60] = new SqeParams(6.21, 9.80, 0.38);   // sp2
                PARAMS_ARRAY[600] = new SqeParams(7.45, 9.50, 0.42);  // sp
                PARAMS_ARRAY[666] = new SqeParams(6.21, 9.80, 0.38);  // aromatic
                PARAMS_ARRAY[14] = new SqeParams(4.80, 7.50, 0.30);
                PARAMS_ARRAY[22] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[32] = new SqeParams(4.00, 9.00, 0.40);
                PARAMS_ARRAY[40] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[50] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[72] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[82] = new SqeParams(3.50, 8.50, 0.35);

 // Group 5 - Nitrogen, Phosphorus, etc.
                PARAMS_ARRAY[7] = new SqeParams(6.42, 11.45, 0.40);
                PARAMS_ARRAY[70] = new SqeParams(7.89, 10.92, 0.45);  // sp2
                PARAMS_ARRAY[700] = new SqeParams(9.84, 10.50, 0.50); // sp
                PARAMS_ARRAY[777] = new SqeParams(7.89, 10.92, 0.45); // aromatic
                PARAMS_ARRAY[15] = new SqeParams(5.03, 8.45, 0.30);
                PARAMS_ARRAY[150] = new SqeParams(5.60, 8.20, 0.35);
                PARAMS_ARRAY[23] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[33] = new SqeParams(4.50, 9.50, 0.45);
                PARAMS_ARRAY[41] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[51] = new SqeParams(4.00, 9.00, 0.40);
                PARAMS_ARRAY[73] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[83] = new SqeParams(3.50, 8.50, 0.35);

 // Group 6 - Oxygen, Sulfur, etc.
                PARAMS_ARRAY[8] = new SqeParams(7.54, 13.01, 0.45);
                PARAMS_ARRAY[80] = new SqeParams(9.24, 12.50, 0.50);  // sp2
                PARAMS_ARRAY[16] = new SqeParams(5.69, 9.20, 0.35);
                PARAMS_ARRAY[160] = new SqeParams(6.30, 8.90, 0.40);
                PARAMS_ARRAY[166] = new SqeParams(6.30, 8.90, 0.40); // aromatic
                PARAMS_ARRAY[24] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[34] = new SqeParams(4.80, 10.00, 0.45);
                PARAMS_ARRAY[42] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[52] = new SqeParams(4.50, 9.50, 0.45);
                PARAMS_ARRAY[74] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[84] = new SqeParams(4.00, 9.00, 0.40);

 // Group 7 - Halogens
                PARAMS_ARRAY[9] = new SqeParams(8.52, 14.20, 0.60);
                PARAMS_ARRAY[17] = new SqeParams(6.94, 10.80, 0.50);
                PARAMS_ARRAY[25] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[35] = new SqeParams(6.64, 9.80, 0.55);
                PARAMS_ARRAY[43] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[53] = new SqeParams(6.24, 8.70, 0.60);
                PARAMS_ARRAY[75] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[85] = new SqeParams(4.50, 9.50, 0.45);

 // Group 8 - Noble Gases and Transition Metals
                PARAMS_ARRAY[26] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[44] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[76] = new SqeParams(3.50, 8.50, 0.35);
                PARAMS_ARRAY[2] = new SqeParams(7.00, 14.00, 0.60);
                PARAMS_ARRAY[10] = new SqeParams(9.00, 15.00, 0.70);
                PARAMS_ARRAY[18] = new SqeParams(7.50, 12.00, 0.60);
                PARAMS_ARRAY[36] = new SqeParams(7.00, 11.00, 0.60);
                PARAMS_ARRAY[54] = new SqeParams(6.50, 10.00, 0.65);
                PARAMS_ARRAY[86] = new SqeParams(5.00, 10.00, 0.50);

 // Groups 9-12 - Transition Metals
                for (int z = 27; z <= 30; z++) PARAMS_ARRAY[z] = new SqeParams(3.50, 8.50, 0.35);
                for (int z = 45; z <= 48; z++) PARAMS_ARRAY[z] = new SqeParams(3.50, 8.50, 0.35);
                for (int z = 77; z <= 80; z++) PARAMS_ARRAY[z] = new SqeParams(3.50, 8.50, 0.35);

 // Lanthanides
                for (int z = 58; z <= 71; z++) PARAMS_ARRAY[z] = new SqeParams(3.00, 8.00, 0.30);

 // Actinides
                for (int z = 90; z <= 103; z++) PARAMS_ARRAY[z] = new SqeParams(3.00, 8.00, 0.30);

 // Element names
                string[] names = {
                    "", "H", "He", "Li", "Be", "B", "C", "N", "O", "F", "Ne",
                    "Na", "Mg", "Al", "Si", "P", "S", "Cl", "Ar", "K", "Ca",
                    "Sc", "Ti", "V", "Cr", "Mn", "Fe", "Co", "Ni", "Cu", "Zn",
                    "Ga", "Ge", "As", "Se", "Br", "Kr", "Rb", "Sr", "Y", "Zr",
                    "Nb", "Mo", "Tc", "Ru", "Rh", "Pd", "Ag", "Cd", "In", "Sn",
                    "Sb", "Te", "I", "Xe", "Cs", "Ba", "La", "Ce", "Pr", "Nd",
                    "Pm", "Sm", "Eu", "Gd", "Tb", "Dy", "Ho", "Er", "Tm", "Yb",
                    "Lu", "Hf", "Ta", "W", "Re", "Os", "Ir", "Pt", "Au", "Hg",
                    "Tl", "Pb", "Bi", "Po", "At", "Rn", "Fr", "Ra", "Ac", "Th",
                    "Pa", "U", "Np", "Pu", "Am", "Cm", "Bk", "Cf", "Es", "Fm",
                    "Md", "No", "Lr"
                };

                for (int i = 0; i < names.Length && i < ELEMENT_NAMES_ARRAY.Length; i++)
                {
                    ELEMENT_NAMES_ARRAY[i] = names[i];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SQE static init error: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Public Properties

 /// <summary>Total charge of the molecule after calculation.</summary>
        public double TotalCharge { get; private set; }

 /// <summary>Indicates whether the calculation converged.</summary>
        public bool IsConverged { get; private set; }

 /// <summary>Debug log string.</summary>
        public string DebugLog => _debugLog.ToString();

 /// <summary>Performance metrics for the calculation.</summary>
        public PerformanceMetrics Metrics { get; private set; } = new PerformanceMetrics();

        #endregion

        #region Public Classes

 /// <summary>
 /// Performance metrics for the charge calculation.
 /// </summary>
        public class PerformanceMetrics
        {
            public int AtomsProcessed { get; set; }
            public double CalculationTimeMs { get; set; }
            public double MappingTimeMs { get; set; }
            public double TotalTimeMs => CalculationTimeMs + MappingTimeMs;
            public int LoadedElements { get; set; }
        }

        #endregion

        #region Constructor

 /// <summary>
 /// Initializes a new instance of the SqeChargeCalculator.
 /// </summary>
 /// <param name="molecule">The molecule to calculate charges for.</param>
        public SqeChargeCalculator(Molecule molecule)
        {
            _molecule = molecule;
            TotalCharge = 0;
            IsConverged = true;
        }

        #endregion

        #region Main Calculation Method

 /// <summary>
 /// Calculates SQE charges for all symmetry equivalents in the molecule.
 /// </summary>
        public void CalculateSqeChargesForSymmetryEquivalents()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Metrics = new PerformanceMetrics();

            _debugLog.Clear();

            try
            {
                if (_molecule == null || _molecule.SymetryEquivalents == null || _molecule.SymetryEquivalents.Count == 0)
                {
                    return;
                }

                int nAtoms = _molecule.SymetryEquivalents.Count;
                var symAtoms = _molecule.SymetryEquivalents;

                _finalCharges = new float[nAtoms];
                int[] atomicNumbers = new int[nAtoms];
                float[] occupancies = new float[nAtoms];
                double[] chiValues = new double[nAtoms];

                double totalChi = 0;
                double totalOcc = 0;

 // Calculate electronegativity values for each atom
                for (int i = 0; i < nAtoms; i++)
                {
                    var atom = symAtoms[i];
                    int z = atom.AtomicNumber;
                    atomicNumbers[i] = z;
                    occupancies[i] = atom.occupancy;

                    SqeParams param = GetParametersForElement(z);

                    if (param != null && atom.occupancy > 0.001f)
                    {
                        chiValues[i] = param.Chi;
                        totalChi += param.Chi * atom.occupancy;
                        totalOcc += atom.occupancy;
                    }
                    else
                    {
                        var defaultParams = GetDefaultParamsFast(z);
                        chiValues[i] = defaultParams.Chi;
                        totalChi += defaultParams.Chi * atom.occupancy;
                        totalOcc += atom.occupancy;
                    }
                }

 // Calculate average electronegativity
                double avgChi = totalOcc > 0 ? totalChi / totalOcc : 5.0;

 // Calculate charges from electronegativity differences
                for (int i = 0; i < nAtoms; i++)
                {
                    if (occupancies[i] > 0.001f)
                    {
                        double diff = avgChi - chiValues[i];
                        double charge = diff / CHARGE_SCALE;
                        int z = atomicNumbers[i];
                        charge = ClampChargeFast(charge, z);
                        _finalCharges[i] = (float)charge;
                    }
                    else
                    {
                        _finalCharges[i] = 0;
                    }
                }

 // Normalize charges to maintain total charge neutrality
                NormalizeCharges(nAtoms, occupancies);

 // Store charges in molecule
                for (int i = 0; i < nAtoms; i++)
                {
                    var atom = symAtoms[i];
                    atom.charge = _finalCharges[i];
                    _molecule.SymetryEquivalents[i] = atom;
                }

 // Update total charge
                TotalCharge = 0;
                for (int i = 0; i < nAtoms; i++)
                {
                    TotalCharge += _finalCharges[i] * occupancies[i];
                }

 // Map charges to asymmetric part
                MapChargesToAsymmetricPart(symAtoms, nAtoms);

 // Update molecule charge range
                UpdateMoleculeChargeRange();

                Metrics.AtomsProcessed = nAtoms;
                Metrics.LoadedElements = 109;

                stopwatch.Stop();
                Metrics.CalculationTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SQE Error: {ex.Message}");
                IsConverged = false;
                SetDefaultCharges();
            }
        }

        #endregion

        #region Helper Methods

 /// <summary>
 /// Gets SQE parameters for an element, including hybridization handling.
 /// </summary>
        private SqeParams GetParametersForElement(int z)
        {
            SqeParams param = null;
            if (z >= 0 && z < PARAMS_ARRAY.Length)
            {
                param = PARAMS_ARRAY[z];
            }

            if (param == null)
            {
                int baseZ = z;
                while (baseZ >= 100) baseZ /= 10;
                if (baseZ >= 0 && baseZ < PARAMS_ARRAY.Length)
                {
                    param = PARAMS_ARRAY[baseZ];
                }
            }

            return param;
        }

 /// <summary>
 /// Normalizes charges to preserve total charge neutrality.
 /// </summary>
        private void NormalizeCharges(int nAtoms, float[] occupancies)
        {
            double total = 0;
            double totalOcc = 0;
            for (int i = 0; i < nAtoms; i++)
            {
                total += _finalCharges[i] * occupancies[i];
                totalOcc += occupancies[i];
            }

            if (totalOcc > 0.001)
            {
                double correction = -total / totalOcc;
                if (correction > MAX_CORRECTION) correction = MAX_CORRECTION;
                if (correction < -MAX_CORRECTION) correction = -MAX_CORRECTION;

                for (int i = 0; i < nAtoms; i++)
                {
                    if (occupancies[i] > 0.001f)
                    {
                        _finalCharges[i] = (float)(_finalCharges[i] + correction);
                    }
                }
            }
        }

 /// <summary>
 /// Maps charges from symmetry equivalents to the asymmetric unit.
 /// </summary>
 /// <param name="symAtoms">The list of symmetry equivalent atoms.</param>
 /// <param name="nAtoms">The number of symmetry equivalent atoms.</param>
        private void MapChargesToAsymmetricPart(List<Molecule.AtomSimple> symAtoms, int nAtoms)
        {
            if (_molecule.AsymmetricPart == null || _molecule.AsymmetricPart.Count == 0)
                return;

            var asymAtoms = _molecule.AsymmetricPart;
            int asymCount = asymAtoms.Count;

 // Build lookup dictionary from symmetry equivalents
            var symDict = new Dictionary<long, float>(nAtoms);
            for (int j = 0; j < nAtoms; j++)
            {
                var symAtom = symAtoms[j];
                long key = GetPositionKeyFast(symAtom.x, symAtom.y, symAtom.z);
                if (!symDict.ContainsKey(key))
                {
                    symDict[key] = _finalCharges[j];
                }
            }

 // Assign charges to asymmetric atoms
            _asymmetricCharges.Clear();
            for (int i = 0; i < asymCount; i++)
            {
                var asymAtom = asymAtoms[i];
                long key = GetPositionKeyFast(asymAtom.x, asymAtom.y, asymAtom.z);

                if (symDict.TryGetValue(key, out float charge))
                {
                    _asymmetricCharges[i] = charge;
                    var temp = asymAtoms[i];
                    temp.charge = charge;
                    _molecule.AsymmetricPart[i] = temp;
                }
            }
        }

 /// <summary>
 /// Updates the molecule's charge range.
 /// </summary>
        private void UpdateMoleculeChargeRange()
        {
            if (_molecule == null) return;

            float minCharge = float.MaxValue;
            float maxCharge = float.MinValue;

            for (int i = 0; i < _finalCharges.Length; i++)
            {
                float charge = _finalCharges[i];
                if (charge < minCharge) minCharge = charge;
                if (charge > maxCharge) maxCharge = charge;
            }

            _molecule.ChargeMin = minCharge;
            _molecule.ChargeMax = maxCharge;
        }

 /// <summary>
 /// Sets default (zero) charges for all atoms.
 /// </summary>
        private void SetDefaultCharges()
        {
            if (_molecule != null && _molecule.SymetryEquivalents != null)
            {
                for (int i = 0; i < _molecule.SymetryEquivalents.Count; i++)
                {
                    var atom = _molecule.SymetryEquivalents[i];
                    atom.charge = 0;
                    _molecule.SymetryEquivalents[i] = atom;
                }
            }
            TotalCharge = 0;
            IsConverged = false;
        }

        #endregion

        #region Fast Inline Methods

 /// <summary>
 /// Clamps a charge value to reasonable limits for an element.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double ClampChargeFast(double charge, int z)
        {
 // Transition metals have wider charge ranges
            if (z >= 21 && z <= 30 || z >= 39 && z <= 48 || z >= 57 && z <= 80)
            {
                return charge < -2.0 ? -2.0 : charge > 3.0 ? 3.0 : charge;
            }

 // Specific element limits
            switch (z)
            {
                case 1:  return charge < -0.5 ? -0.5 : charge > 1.5 ? 1.5 : charge;  // H
                case 6:  return charge < -1.0 ? -1.0 : charge > 1.0 ? 1.0 : charge;  // C
                case 7:  return charge < -1.5 ? -1.5 : charge > 1.5 ? 1.5 : charge;  // N
                case 8:  return charge < -1.5 ? -1.5 : charge > 1.5 ? 1.5 : charge;  // O
                case 9:  return charge < -1.0 ? -1.0 : charge > 1.0 ? 1.0 : charge;  // F
                case 15: return charge < -1.0 ? -1.0 : charge > 1.0 ? 1.0 : charge;  // P
                case 16: return charge < -1.0 ? -1.0 : charge > 1.0 ? 1.0 : charge;  // S
                case 17: return charge < -1.0 ? -1.0 : charge > 1.0 ? 1.0 : charge;  // Cl
                case 35: return charge < -1.0 ? -1.0 : charge > 1.0 ? 1.0 : charge;  // Br
                case 53: return charge < -1.0 ? -1.0 : charge > 1.0 ? 1.0 : charge;  // I
                default: return charge < -2.0 ? -2.0 : charge > 2.0 ? 2.0 : charge;
            }
        }

 /// <summary>
 /// Gets default SQE parameters for an element based on periodic trends.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SqeParams GetDefaultParamsFast(int atomicNumber)
        {
            double chi, eta, beta;

            if (atomicNumber <= 2)
            {
                chi = 7.0 + (atomicNumber - 1) * 3.0;
                eta = 14.0 - (atomicNumber - 1) * 2.0;
                beta = 0.6 - (atomicNumber - 1) * 0.1;
            }
            else if (atomicNumber <= 10)
            {
                chi = 3.0 + (atomicNumber - 2) * 0.8;
                eta = 10.0 + (atomicNumber - 2) * 0.5;
                beta = 0.3 + (atomicNumber - 2) * 0.03;
            }
            else if (atomicNumber <= 18)
            {
                chi = 2.0 + (atomicNumber - 10) * 0.4;
                eta = 8.0 + (atomicNumber - 10) * 0.3;
                beta = 0.25 + (atomicNumber - 10) * 0.02;
            }
            else if (atomicNumber <= 36)
            {
                chi = 2.0 + (atomicNumber - 18) * 0.25;
                eta = 7.0 + (atomicNumber - 18) * 0.2;
                beta = 0.2 + (atomicNumber - 18) * 0.015;
            }
            else if (atomicNumber <= 54)
            {
                chi = 2.0 + (atomicNumber - 36) * 0.15;
                eta = 7.0 + (atomicNumber - 36) * 0.15;
                beta = 0.2 + (atomicNumber - 36) * 0.01;
            }
            else
            {
                chi = 2.0 + (atomicNumber - 54) * 0.1;
                eta = 7.0 + (atomicNumber - 54) * 0.1;
                beta = 0.2 + (atomicNumber - 54) * 0.008;
            }

            chi = chi < 1.5 ? 1.5 : chi > 10.0 ? 10.0 : chi;
            eta = eta < 5.0 ? 5.0 : eta > 15.0 ? 15.0 : eta;
            beta = beta < 0.15 ? 0.15 : beta > 0.7 ? 0.7 : beta;

            return new SqeParams(chi, eta, beta);
        }

 /// <summary>
 /// Generates a unique key from fractional coordinates.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetPositionKeyFast(float x, float y, float z)
        {
            int ix = (int)((x - (float)Math.Floor(x)) * POSITION_PRECISION + 0.5f);
            int iy = (int)((y - (float)Math.Floor(y)) * POSITION_PRECISION + 0.5f);
            int iz = (int)((z - (float)Math.Floor(z)) * POSITION_PRECISION + 0.5f);
            return (long)ix << 40 | (long)iy << 20 | iz;
        }

        #endregion

        #region Public Getter Methods

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

 /// <summary>
 /// Prints a summary of the charge calculation.
 /// </summary>
        public void PrintChargeSummary()
        {
            System.Diagnostics.Debug.WriteLine($"SQE: {Metrics.AtomsProcessed} atoms, {Metrics.CalculationTimeMs:F1}ms");

            if (_molecule != null)
            {
                System.Diagnostics.Debug.WriteLine($"Charge range: min={_molecule.ChargeMin:F4}, max={_molecule.ChargeMax:F4}");
                System.Diagnostics.Debug.WriteLine($"Total charge: {TotalCharge:F4}");
            }
        }

        #endregion
    }
}