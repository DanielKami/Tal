// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Calculation module: SqeChargeCalculator (Split-charge equilibration).

using System;
using System.Collections.Generic;
using System.Text;

namespace Tal.Calculate
{
    /// <summary>
    /// SQE: Split-Charge Equilibration, in the form of Eq. (16) of the article
    /// (Nistor, Polihronov, Müser, Mosey, J. Chem. Phys. 125 (2006); Verstraelen, Van Speybroeck, Waroquier, J. Chem. Phys. 131 (2009);
    /// extends the QEq method of Rappé and Goddard, J. Phys. Chem. 95 (1991), 3358).
    ///
    /// The partial charge is q_i = q_i⁰ + Σ_j p_ij, with antisymmetric split charges p_ij = −p_ji transferred across the bonded
    /// pairs (i, j) ∈ B (q_i⁰ = formal reference charge; here 0). The energy is minimized with respect to p_ij:
    ///     E_SQE({p_ij}) = Σ_i ( χ_i⁰ q_i + ½ η_i⁰ q_i² ) + ½ Σ_{i≠j} J_ij q_i q_j + ½ Σ_{(i,j)∈B} κ_ij p_ij²
    /// with
    ///     χ_i⁰  electronegativity (eV),
    ///     η_i⁰  atomic hardness (eV/e²), equal to the on-site Coulomb integral J_ii,
    ///     J_ij  shielded Coulomb kernel (Ohno–Klopman)  J_ij = 14.3996 / sqrt(r² + a_ij²),  a_ij = 2·14.3996 / (η_i + η_j)  (eV/e²),
    ///     κ_ij  bond hardness (eV/e²); κ → 0 recovers full QEq equalization, κ → ∞ keeps the reference charges.
    /// Stationarity ∂E/∂p_ij = 0 gives  p_ij = (χ̃_j − χ̃_i) / κ_ij,  χ̃_i = ∂E/∂q_i, i.e. the linear system
    ///     [ Bᵀ (diag η + J) B + diag κ ] p = −Δχ ,   Δχ_b = χ_i − χ_j  (b = (i, j), i < j),
    /// solved with a preconditioned conjugate-gradient method.
    /// Parametrization of this implementation: the element tables give χ_i⁰, η_i⁰ and a bond-softness number β_i;
    /// the bond hardness is κ_ij = κ0 / sqrt(β_i β_j) with κ0 = 3 eV/e² (a soft, ionic pair has a small κ_ij).
    /// A non-bonded atom has no split charge and keeps q = q⁰ = 0. The Coulomb kernel is that of an isolated cluster
    /// (no Ewald summation); the atoms of the unit cell (symmetry equivalents) are used with the same bond topology as the Gasteiger calculator.
    /// </summary>
    public class SqeChargeCalculator
    {
        #region Constants

        /// <summary>e²/(4πε0) in eV·Å.</summary>
        private const double COULOMB_EV_A = 14.3996454784;

        /// <summary>Global scale κ0 of the bond hardness (eV/e²): κ_ij = κ0 / sqrt(β_i β_j).</summary>
        public const double KAPPA0 = 3.0;

        private const int POSITION_PRECISION = 1000;
        private const double CG_TOLERANCE = 1e-9;

        /// <summary>Above this atom count the Coulomb kernel is truncated at <see cref="LARGE_SYSTEM_CUTOFF"/> (Å).</summary>
        private const int LARGE_SYSTEM_ATOMS = 1500;
        private const double LARGE_SYSTEM_CUTOFF = 15.0;

        #endregion

        #region Private Fields

        private readonly Molecule _molecule;
        private readonly StringBuilder _debugLog = new StringBuilder();

        private float[] _finalCharges;
        private readonly Dictionary<int, float> _asymmetricCharges = new Dictionary<int, float>();

        #endregion

        #region Parameters

        private enum Hyb { Sp3 = 0, Sp2 = 1, Sp = 2 }

        /// <summary>Element parameters: electronegativity χ⁰ (eV), hardness η⁰ (eV/e²), bond softness β (sets the bond hardness κ_ij).</summary>
        private readonly struct SqeParams
        {
            public readonly double Chi, Eta, Beta;
            public SqeParams(double chi, double eta, double beta) { Chi = chi; Eta = eta; Beta = beta; }
        }

        private static readonly SqeParams?[] BY_Z = new SqeParams?[119];
        private static readonly Dictionary<int, SqeParams> BY_Z_HYB = new Dictionary<int, SqeParams>();

        private static int HybKey(int z, Hyb h) => z * 10 + (int)h;

        static SqeChargeCalculator()
        {
            void Set(int z, double chi, double eta, double beta) => BY_Z[z] = new SqeParams(chi, eta, beta);
            void SetH(int z, Hyb h, double chi, double eta, double beta) => BY_Z_HYB[HybKey(z, h)] = new SqeParams(chi, eta, beta);

            // Group 1
            Set(1, 4.41, 12.83, 0.50); Set(3, 2.50, 8.00, 0.30); Set(11, 2.20, 7.00, 0.25); Set(19, 2.00, 6.50, 0.20);
            Set(37, 1.80, 6.00, 0.20); Set(55, 1.60, 5.50, 0.18); Set(87, 1.50, 5.00, 0.15);
            // Group 2
            Set(4, 3.00, 9.00, 0.35); Set(12, 2.50, 8.00, 0.30); Set(20, 2.20, 7.00, 0.28); Set(38, 2.00, 6.50, 0.25);
            Set(56, 1.80, 6.00, 0.22); Set(88, 1.60, 5.50, 0.20);
            // Group 3
            Set(5, 4.00, 10.00, 0.40); Set(13, 3.50, 9.00, 0.35); Set(21, 3.00, 8.50, 0.32); Set(31, 3.50, 8.50, 0.35);
            Set(39, 3.00, 8.00, 0.30); Set(49, 3.50, 8.50, 0.35); Set(57, 3.00, 8.00, 0.30); Set(89, 3.00, 8.00, 0.30);
            // Group 4
            Set(6, 5.39, 10.15, 0.35); SetH(6, Hyb.Sp2, 6.21, 9.80, 0.38); SetH(6, Hyb.Sp, 7.45, 9.50, 0.42);
            Set(14, 4.80, 7.50, 0.30); Set(22, 3.50, 8.50, 0.35); Set(32, 4.00, 9.00, 0.40); Set(40, 3.50, 8.50, 0.35);
            Set(50, 3.50, 8.50, 0.35); Set(72, 3.50, 8.50, 0.35); Set(82, 3.50, 8.50, 0.35);
            // Group 5
            Set(7, 6.42, 11.45, 0.40); SetH(7, Hyb.Sp2, 7.89, 10.92, 0.45); SetH(7, Hyb.Sp, 9.84, 10.50, 0.50);
            Set(15, 5.03, 8.45, 0.30); Set(23, 3.50, 8.50, 0.35); Set(33, 4.50, 9.50, 0.45); Set(41, 3.50, 8.50, 0.35);
            Set(51, 4.00, 9.00, 0.40); Set(73, 3.50, 8.50, 0.35); Set(83, 3.50, 8.50, 0.35);
            // Group 6
            Set(8, 7.54, 13.01, 0.45); SetH(8, Hyb.Sp2, 9.24, 12.50, 0.50);
            Set(16, 5.69, 9.20, 0.35); SetH(16, Hyb.Sp2, 6.30, 8.90, 0.40);
            Set(24, 3.50, 8.50, 0.35); Set(34, 4.80, 10.00, 0.45); Set(42, 3.50, 8.50, 0.35); Set(52, 4.50, 9.50, 0.45);
            Set(74, 3.50, 8.50, 0.35); Set(84, 4.00, 9.00, 0.40);
            // Group 7
            Set(9, 8.52, 14.20, 0.60); Set(17, 6.94, 10.80, 0.50); Set(25, 3.50, 8.50, 0.35); Set(35, 6.64, 9.80, 0.55);
            Set(43, 3.50, 8.50, 0.35); Set(53, 6.24, 8.70, 0.60); Set(75, 3.50, 8.50, 0.35); Set(85, 4.50, 9.50, 0.45);
            // Group 8 / noble gases
            Set(26, 3.50, 8.50, 0.35); Set(44, 3.50, 8.50, 0.35); Set(76, 3.50, 8.50, 0.35);
            Set(2, 7.00, 14.00, 0.60); Set(10, 9.00, 15.00, 0.70); Set(18, 7.50, 12.00, 0.60);
            Set(36, 7.00, 11.00, 0.60); Set(54, 6.50, 10.00, 0.65); Set(86, 5.00, 10.00, 0.50);
            // Groups 9-12
            for (int z = 27; z <= 30; z++) Set(z, 3.50, 8.50, 0.35);
            for (int z = 45; z <= 48; z++) Set(z, 3.50, 8.50, 0.35);
            for (int z = 77; z <= 80; z++) Set(z, 3.50, 8.50, 0.35);
            // Lanthanides, actinides
            for (int z = 58; z <= 71; z++) Set(z, 3.00, 8.00, 0.30);
            for (int z = 90; z <= 103; z++) Set(z, 3.00, 8.00, 0.30);
        }

        private static Hyb HybridState(int z, int code)
        {
            // Only C, N, O, S carry a hybridization state. The PEOE code equals Z for sp3, which must not be confused
            // with the atomic number of another element (60 = Nd, 70 = Yb, 80 = Hg ...), so the code is read only for these four.
            if (z != 6 && z != 7 && z != 8 && z != 16) return Hyb.Sp3;
            switch (code)
            {
                case 60: case 70: case 80: case 160:
                case 666: case 777: case 166:
                    return Hyb.Sp2;
                case 600: case 700:
                    return Hyb.Sp;
                default:
                    return Hyb.Sp3;
            }
        }

        private static SqeParams GetParams(int z, int hybridizationCode)
        {
            var h = HybridState(z, hybridizationCode);
            if (h != Hyb.Sp3 && BY_Z_HYB.TryGetValue(HybKey(z, h), out var ph)) return ph;
            if (z >= 0 && z < BY_Z.Length && BY_Z[z].HasValue) return BY_Z[z].Value;
            return DefaultParams(z);
        }

        /// <summary>Periodic-trend estimate for elements without tabulated parameters.</summary>
        private static SqeParams DefaultParams(int z)
        {
            double chi, eta, beta;
            if (z <= 2) { chi = 7.0 + (z - 1) * 3.0; eta = 14.0 - (z - 1) * 2.0; beta = 0.6 - (z - 1) * 0.1; }
            else if (z <= 10) { chi = 3.0 + (z - 2) * 0.8; eta = 10.0 + (z - 2) * 0.5; beta = 0.3 + (z - 2) * 0.03; }
            else if (z <= 18) { chi = 2.0 + (z - 10) * 0.4; eta = 8.0 + (z - 10) * 0.3; beta = 0.25 + (z - 10) * 0.02; }
            else if (z <= 36) { chi = 2.0 + (z - 18) * 0.25; eta = 7.0 + (z - 18) * 0.2; beta = 0.2 + (z - 18) * 0.015; }
            else if (z <= 54) { chi = 2.0 + (z - 36) * 0.15; eta = 7.0 + (z - 36) * 0.15; beta = 0.2 + (z - 36) * 0.01; }
            else { chi = 2.0 + (z - 54) * 0.1; eta = 7.0 + (z - 54) * 0.1; beta = 0.2 + (z - 54) * 0.008; }
            chi = Math.Clamp(chi, 1.5, 10.0);
            eta = Math.Clamp(eta, 5.0, 15.0);
            beta = Math.Clamp(beta, 0.15, 0.7);
            return new SqeParams(chi, eta, beta);
        }

        #endregion

        #region Public Properties

        /// <summary>Occupancy-weighted total charge after calculation (e).</summary>
        public double TotalCharge { get; private set; }

        /// <summary>True when the conjugate-gradient solver reached its tolerance.</summary>
        public bool IsConverged { get; private set; }

        /// <summary>Conjugate-gradient iterations used.</summary>
        public int Iterations { get; private set; }

        /// <summary>Relative residual |A p + Δχ| / |Δχ| of the split-charge system.</summary>
        public double RelativeResidual { get; private set; }

        /// <summary>Number of bonds (split-charge variables).</summary>
        public int BondCount { get; private set; }

        /// <summary>Debug log string.</summary>
        public string DebugLog => _debugLog.ToString();

        /// <summary>Performance metrics for the calculation.</summary>
        public PerformanceMetrics Metrics { get; private set; } = new PerformanceMetrics();

        /// <summary>Performance metrics for the charge calculation.</summary>
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

        public SqeChargeCalculator(Molecule molecule)
        {
            _molecule = molecule;
            TotalCharge = 0;
            IsConverged = true;
        }

        #endregion

        #region Main Calculation

        private sealed class Bond
        {
            public int I, J;
            public double Kappa;      // bond hardness κ_ij (eV/e²)
            public double DeltaChi;   // χ_i − χ_j
            public double Jij;        // Coulomb kernel of the bonded pair (eV/e²)
        }

        /// <summary>
        /// Calculates split-charge equilibration charges for all symmetry equivalents and maps them to the asymmetric part.
        /// </summary>
        public void CalculateSqeChargesForSymmetryEquivalents()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Metrics = new PerformanceMetrics();
            _debugLog.Clear();

            try
            {
                var topo = PEOEChargeCalculator.BuildTopology(_molecule);
                if (topo == null) return;

                int n = topo.Count;
                var symAtoms = _molecule.SymetryEquivalents;

                var chi = new double[n];
                var eta = new double[n];
                var beta = new double[n];
                for (int i = 0; i < n; i++)
                {
                    var p = GetParams(topo.AtomicNumbers[i], topo.HybridizationCodes[i]);
                    chi[i] = p.Chi; eta[i] = p.Eta; beta[i] = p.Beta;
                }

                // Bonds i<j
                var bonds = new List<Bond>();
                for (int i = 0; i < n; i++)
                {
                    var nb = topo.Neighbors[i];
                    for (int k = 0; k < nb.Count; k++)
                    {
                        int j = nb[k];
                        if (j <= i) continue;
                        double kappa = KAPPA0 / Math.Sqrt(beta[i] * beta[j]);
                        double r = topo.NeighborDistances[i][k];
                        double a = 2.0 * COULOMB_EV_A / (eta[i] + eta[j]);
                        bonds.Add(new Bond
                        {
                            I = i, J = j,
                            Kappa = kappa,
                            DeltaChi = chi[i] - chi[j],
                            Jij = COULOMB_EV_A / Math.Sqrt(r * r + a * a)
                        });
                    }
                }
                BondCount = bonds.Count;

                // Coulomb kernel J_ij (Ohno–Klopman) for atom pairs; only atoms that take part in a bond carry charge
                var active = new bool[n];
                foreach (var b in bonds) { active[b.I] = true; active[b.J] = true; }
                var pairI = new List<int>();
                var pairJ = new List<int>();
                var pairK = new List<double>();
                double cutoff = n > LARGE_SYSTEM_ATOMS ? LARGE_SYSTEM_CUTOFF : double.MaxValue;
                double cutoffSq = cutoff * cutoff;
                for (int i = 0; i < n; i++)
                {
                    if (!active[i]) continue;
                    for (int j = i + 1; j < n; j++)
                    {
                        if (!active[j]) continue;
                        var d = topo.Cart[i] - topo.Cart[j];
                        double r2 = d.X * (double)d.X + d.Y * (double)d.Y + d.Z * (double)d.Z;
                        if (r2 > cutoffSq) continue;
                        double a = 2.0 * COULOMB_EV_A / (eta[i] + eta[j]);
                        pairI.Add(i); pairJ.Add(j);
                        pairK.Add(COULOMB_EV_A / Math.Sqrt(r2 + a * a));
                    }
                }

                var q = new double[n];
                SolveSplitCharges(bonds, eta, pairI, pairJ, pairK, q);

                _finalCharges = new float[n];
                for (int i = 0; i < n; i++) _finalCharges[i] = (float)q[i];

                for (int i = 0; i < n; i++)
                {
                    var atom = symAtoms[i];
                    atom.charge = _finalCharges[i];
                    symAtoms[i] = atom;
                }

                double total = 0;
                for (int i = 0; i < n; i++) total += _finalCharges[i] * topo.Occupancy[i];
                TotalCharge = total;

                var swMap = System.Diagnostics.Stopwatch.StartNew();
                MapChargesToAsymmetricPart(symAtoms, n);
                swMap.Stop();
                UpdateMoleculeChargeRange();

                Metrics.AtomsProcessed = n;
                Metrics.LoadedElements = 118;
                Metrics.MappingTimeMs = swMap.Elapsed.TotalMilliseconds;
                sw.Stop();
                Metrics.CalculationTimeMs = sw.Elapsed.TotalMilliseconds - Metrics.MappingTimeMs;

                _debugLog.AppendLine($"SQE: {n} atoms, {BondCount} bonds, {pairK.Count} Coulomb pairs, CG {Iterations} it., rel. residual {RelativeResidual:E2}, converged={IsConverged}");
                System.Diagnostics.Debug.WriteLine("[SQE] " + _debugLog.ToString().Trim());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SQE Error: {ex.Message}");
                IsConverged = false;
                SetDefaultCharges();
            }
        }

        /// <summary>
        /// Solves [Bᵀ (diag η + J) B + diag κ] p = −Δχ by Jacobi-preconditioned conjugate gradients and returns q = B p.
        /// </summary>
        private void SolveSplitCharges(List<Bond> bonds, double[] eta,
                                       List<int> pairI, List<int> pairJ, List<double> pairK, double[] q)
        {
            int nb = bonds.Count;
            int n = q.Length;
            Array.Clear(q, 0, n);
            Iterations = 0;
            IsConverged = true;
            RelativeResidual = 0;
            if (nb == 0) return;

            int np = pairK.Count;
            var qq = new double[n];
            var y = new double[n];

            void Apply(double[] x, double[] result)
            {
                Array.Clear(qq, 0, n);
                for (int b = 0; b < nb; b++) { qq[bonds[b].I] += x[b]; qq[bonds[b].J] -= x[b]; }
                for (int i = 0; i < n; i++) y[i] = eta[i] * qq[i];
                for (int k = 0; k < np; k++)
                {
                    int i = pairI[k], j = pairJ[k];
                    double jk = pairK[k];
                    y[i] += jk * qq[j];
                    y[j] += jk * qq[i];
                }
                for (int b = 0; b < nb; b++)
                    result[b] = y[bonds[b].I] - y[bonds[b].J] + bonds[b].Kappa * x[b];
            }

            // diagonal of A: M_ii + M_jj - 2 M_ij + kappa = eta_i + eta_j - 2 J_ij + kappa (Jacobi preconditioner)
            var diag = new double[nb];
            for (int b = 0; b < nb; b++)
                diag[b] = eta[bonds[b].I] + eta[bonds[b].J] - 2.0 * bonds[b].Jij + bonds[b].Kappa;

            var rhs = new double[nb];
            double rhsNorm = 0;
            for (int b = 0; b < nb; b++) { rhs[b] = -bonds[b].DeltaChi; rhsNorm += rhs[b] * rhs[b]; }
            rhsNorm = Math.Sqrt(rhsNorm);
            if (rhsNorm < 1e-14) return;

            var p = new double[nb];
            var r = (double[])rhs.Clone();
            var z = new double[nb];
            var d = new double[nb];
            var Ad = new double[nb];

            for (int b = 0; b < nb; b++) z[b] = r[b] / diag[b];
            Array.Copy(z, d, nb);
            double rz = 0;
            for (int b = 0; b < nb; b++) rz += r[b] * z[b];

            int maxIter = Math.Min(2 * nb + 50, 1000);
            int it = 0;
            double rNorm = rhsNorm;
            for (; it < maxIter; it++)
            {
                Apply(d, Ad);
                double dAd = 0;
                for (int b = 0; b < nb; b++) dAd += d[b] * Ad[b];
                if (dAd <= 0) break;
                double alpha = rz / dAd;

                double rr = 0;
                for (int b = 0; b < nb; b++)
                {
                    p[b] += alpha * d[b];
                    r[b] -= alpha * Ad[b];
                    rr += r[b] * r[b];
                }
                rNorm = Math.Sqrt(rr);
                if (rNorm / rhsNorm < CG_TOLERANCE) { it++; break; }

                double rzNew = 0;
                for (int b = 0; b < nb; b++) { z[b] = r[b] / diag[b]; rzNew += r[b] * z[b]; }
                double beta = rzNew / rz;
                rz = rzNew;
                for (int b = 0; b < nb; b++) d[b] = z[b] + beta * d[b];
            }

            Iterations = it;
            RelativeResidual = rNorm / rhsNorm;
            IsConverged = RelativeResidual < 1e-6;

            for (int b = 0; b < nb; b++) { q[bonds[b].I] += p[b]; q[bonds[b].J] -= p[b]; }
        }

        #endregion

        #region Mapping and Statistics

        private void MapChargesToAsymmetricPart(List<Molecule.AtomSimple> symAtoms, int nAtoms)
        {
            if (_molecule.AsymmetricPart == null || _molecule.AsymmetricPart.Count == 0) return;

            var asymAtoms = _molecule.AsymmetricPart;
            var symDict = new Dictionary<long, float>(nAtoms);
            for (int j = 0; j < nAtoms; j++)
            {
                long key = GetPositionKey(symAtoms[j].x, symAtoms[j].y, symAtoms[j].z);
                if (!symDict.ContainsKey(key)) symDict[key] = _finalCharges[j];
            }

            _asymmetricCharges.Clear();
            for (int i = 0; i < asymAtoms.Count; i++)
            {
                var a = asymAtoms[i];
                if (symDict.TryGetValue(GetPositionKey(a.x, a.y, a.z), out float charge))
                {
                    _asymmetricCharges[i] = charge;
                    a.charge = charge;
                    asymAtoms[i] = a;
                }
            }
        }

        private void UpdateMoleculeChargeRange()
        {
            if (_molecule == null || _finalCharges == null || _finalCharges.Length == 0) return;
            float min = float.MaxValue, max = float.MinValue;
            foreach (float c in _finalCharges)
            {
                if (c < min) min = c;
                if (c > max) max = c;
            }
            _molecule.ChargeMin = min;
            _molecule.ChargeMax = max;
        }

        private void SetDefaultCharges()
        {
            if (_molecule?.SymetryEquivalents != null)
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

        /// <summary>Key from fractional coordinates (wrapped into [0,1), 1/1000 resolution, periodic at 1.0).</summary>
        private static long GetPositionKey(float x, float y, float z)
        {
            static int Q(float v)
            {
                float f = v - (float)Math.Floor(v);
                return (int)(f * POSITION_PRECISION + 0.5f) % POSITION_PRECISION;
            }
            return (long)Q(x) << 40 | (long)Q(y) << 20 | (long)Q(z);
        }

        #endregion

        #region Public Getters

        public float GetChargeForSymmetryEquivalent(int index)
        {
            if (_finalCharges != null && index >= 0 && index < _finalCharges.Length) return _finalCharges[index];
            return 0.0f;
        }

        public float GetAsymmetricCharge(int asymIndex)
            => _asymmetricCharges.TryGetValue(asymIndex, out float c) ? c : 0.0f;

        public float[] GetFinalCharges() => _finalCharges;

        public Dictionary<int, float> GetAllAsymmetricCharges() => new Dictionary<int, float>(_asymmetricCharges);

        public void PrintChargeSummary()
        {
            System.Diagnostics.Debug.WriteLine($"SQE: {Metrics.AtomsProcessed} atoms, {BondCount} bonds, {Metrics.CalculationTimeMs:F1} ms");
            if (_molecule != null)
            {
                System.Diagnostics.Debug.WriteLine($"Charge range: min={_molecule.ChargeMin:F4}, max={_molecule.ChargeMax:F4}");
                System.Diagnostics.Debug.WriteLine($"Total charge: {TotalCharge:F4}");
            }
        }

        #endregion
    }
}
