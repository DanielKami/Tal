// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Powder diffraction module: PowderDiffractionEngine.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Tal.Core;

namespace Tal.Core.PowderDiffraction
{
    /// <summary>
    /// Kinematical powder pattern: one peak per symmetry-independent reflection (orbit of the Laue group),
    /// I = m · |F|² · LP, where m is the true multiplicity of the orbit.
    /// Distinct reflections that merely overlap in 2θ stay separate peaks; they are summed only when
    /// the profile is drawn.
    /// </summary>
    public class PowderDiffractionEngine
    {
        private readonly Molecule _molecule;
        private readonly StructureFactorCalculator _structureFactor;
        private double _wavelength = 1.5406;

        public double Wavelength
        {
            get => _wavelength;
            set => _wavelength = value;
        }

        public PowderDiffractionEngine(Molecule molecule)
        {
            _molecule = molecule ?? throw new ArgumentNullException(nameof(molecule));
            _structureFactor = new StructureFactorCalculator(molecule);
        }

        /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
        /// <exception cref="UnsupportedElementException">
        /// The structure contains an element without tabulated X-ray scattering factors.
        /// </exception>
        public List<DiffractionPeak> CalculatePeaks(
            double minTwoTheta,
            double maxTwoTheta,
            double wavelength = 1.5406,
            double debyeWallerB = 0.5,
            bool useLPCorrection = true,
            bool useAtomicDisplacement = false,
            double displacementScale = 1.0,
            CancellationToken cancellationToken = default)
        {
            _wavelength = wavelength;
            var peaks = new List<DiffractionPeak>();

            if (_molecule.AsymmetricPart.Count == 0 || wavelength <= 0)
                return peaks;

            // Throws UnsupportedElementException instead of silently dropping atoms from F(hkl).
            _structureFactor.PrepareCell();

            // Reciprocal metric tensor Q = G⁻¹:  d*² = hᵀ Q h
            if (!ReflectionGeometry.TryBuildReciprocalMetric(
                    _molecule.a, _molecule.b, _molecule.c,
                    _molecule.alpha, _molecule.beta, _molecule.gamma, out double[,] q))
                return peaks;

            double maxTT = Math.Min(maxTwoTheta, 180.0);
            double minTT = Math.Max(minTwoTheta, 0.0);
            if (maxTT <= 0.0 || minTT >= maxTT)
                return peaks;

            // d* = 1/d = 2 sinθ / λ
            double dStarMax = 2.0 * Math.Sin(maxTT / 2.0 * Math.PI / 180.0) / wavelength;
            double dStarMin = 2.0 * Math.Sin(minTT / 2.0 * Math.PI / 180.0) / wavelength;

            int[][] laue = BuildLaueOperations();
            char centering = GetCenteringSymbol();

            int visited = 0;

            // All lattice points of the 2θ shell, from the metric (no fixed index box or limit).
            foreach (var (h, k, l) in ReflectionGeometry.EnumerateLatticePoints(q, dStarMin, dStarMax))
            {
                if ((++visited & 0x3FF) == 0)
                    cancellationToken.ThrowIfCancellationRequested();

                double dStar2 = ReflectionGeometry.DStarSquared(q, h, k, l);

                double d = 1.0 / Math.Sqrt(dStar2);
                double sinTheta = 0.5 * wavelength / d;
                if (sinTheta > 1.0)
                {
                    if (sinTheta > 1.0 + 1e-9) continue;
                    sinTheta = 1.0;
                }

                double theta = Math.Asin(sinTheta) * 180.0 / Math.PI;
                double twoTheta = 2.0 * theta;
                if (twoTheta < minTwoTheta || twoTheta > maxTwoTheta)
                    continue;

                // Fast pre-filter from the real centring symbol; the true extinctions
                // (screw axes, glide planes) come out of F(hkl) over the full cell.
                if (!PassesCenteringPrefilter(centering, h, k, l))
                    continue;

                // One entry per symmetry orbit. Every other member of the orbit has the same
                // d and the same |F|, so it is represented by this one through the multiplicity.
                if (!ReflectionGeometry.TryGetOrbitRepresentative(laue, h, k, l, out int multiplicity))
                    continue;

                double F = _structureFactor.Calculate(h, k, l, theta, wavelength, debyeWallerB, useAtomicDisplacement, displacementScale);
                double lp = useLPCorrection ? LorentzPolarizationCorrection(theta) : 1.0;

                // I = m · |F|² · LP   (F already contains the Debye-Waller factor)
                double intensity = multiplicity * F * F * lp;
                if (intensity < 1e-10)
                    continue;

                peaks.Add(new DiffractionPeak
                {
                    H = h,
                    K = k,
                    L = l,
                    TwoTheta = twoTheta,
                    DSpacing = d,
                    Intensity = intensity,
                    Multiplicity = multiplicity,
                    StructureFactor = F,
                    Q = 4.0 * Math.PI * sinTheta / wavelength,
                    ReciprocalD = Math.Sqrt(dStar2)
                });
            }

            return peaks.OrderBy(p => p.TwoTheta).ThenByDescending(p => p.Intensity).ToList();
        }

        /// <summary>
        /// Rough size of a calculation, to warn before a huge cell freezes the program:
        /// LatticePoints = lattice points inside the 2θ shell, (4π/3)(d*max³ − d*min³)·V;
        /// StructureFactorTerms = symmetry-independent reflections × atoms in the cell (each term = one sin/cos pair).
        /// </summary>
        public (double LatticePoints, double StructureFactorTerms) EstimateWork(
            double minTwoTheta, double maxTwoTheta, double wavelength)
        {
            if (wavelength <= 0 || _molecule.AsymmetricPart.Count == 0 ||
                !ReflectionGeometry.TryBuildReciprocalMetric(
                    _molecule.a, _molecule.b, _molecule.c,
                    _molecule.alpha, _molecule.beta, _molecule.gamma, out double[,] q))
                return (0, 0);

            double maxTT = Math.Min(maxTwoTheta, 180.0);
            double minTT = Math.Max(minTwoTheta, 0.0);
            if (maxTT <= minTT) return (0, 0);

            double dMax = 2.0 * Math.Sin(maxTT / 2.0 * Math.PI / 180.0) / wavelength;
            double dMin = 2.0 * Math.Sin(minTT / 2.0 * Math.PI / 180.0) / wavelength;

            double detQ = q[0, 0] * (q[1, 1] * q[2, 2] - q[1, 2] * q[2, 1])
                        - q[0, 1] * (q[1, 0] * q[2, 2] - q[1, 2] * q[2, 0])
                        + q[0, 2] * (q[1, 0] * q[2, 1] - q[1, 1] * q[2, 0]);
            if (!(detQ > 0)) return (0, 0);

            double volume = 1.0 / Math.Sqrt(detQ);
            double latticePoints = 4.0 * Math.PI / 3.0 * (dMax * dMax * dMax - dMin * dMin * dMin) * volume;

            int laueOrder = Math.Max(1, BuildLaueOperations().Length);
            int nOps = Math.Max(1, _molecule.SymmetryOperations?.Count ?? 0);
            double atomsInCell = (double)_molecule.AsymmetricPart.Count * nOps;   // upper bound

            return (latticePoints, latticePoints / laueOrder * atomsInCell);
        }


        #region Symmetry equivalence of reflections

        /// <summary>
        /// Laue group of the pattern from the rotation parts of ALL space-group operations (plus
        /// inversion, Friedel). Independent of the symmetry operations switched on or off in the 3D view.
        /// </summary>
        private int[][] BuildLaueOperations()
        {
            var rotations = new List<int[]>();

            var ops = _molecule.SymmetryOperations;
            if (ops != null)
            {
                foreach (var op in ops)
                {
                    int[] r = new int[9];
                    for (int axis = 0; axis < 3; axis++)
                    {
                        int p1 = op.pos1[axis], s1 = op.sign1[axis];
                        if (s1 != 0 && p1 >= 0 && p1 < 3) r[axis * 3 + p1] += s1;

                        int p2 = op.pos2[axis], s2 = op.sign2[axis];
                        if (s2 != 0 && p2 >= 0 && p2 < 3) r[axis * 3 + p2] += s2;
                    }
                    rotations.Add(r);
                }
            }

            return ReflectionGeometry.BuildLaueGroup(rotations);
        }

        #endregion

        #region Centring, Lorentz-polarization

        /// <summary>
        /// Quick performance pre-filter from the centring letter of the real Hermann-Mauguin symbol
        /// (not from the space-group number). Genuine extinctions come from F(hkl) over the full cell.
        /// </summary>
        private static bool PassesCenteringPrefilter(char centering, int h, int k, int l)
        {
            switch (centering)
            {
                case 'A': return (k + l) % 2 == 0;
                case 'B': return (h + l) % 2 == 0;
                case 'C': return (h + k) % 2 == 0;
                case 'F':
                    bool hEven = h % 2 == 0;
                    bool kEven = k % 2 == 0;
                    bool lEven = l % 2 == 0;
                    return hEven == kEven && kEven == lEven;   // all even or all odd
                case 'I': return (h + k + l) % 2 == 0;
                // R in the hexagonal setting needs -h+k+l = 3n; the rhombohedral setting differs,
                // so nothing is filtered here and F(hkl) decides.
                case 'R': return true;
                default: return true;
            }
        }

        private char GetCenteringSymbol()
        {
            string name = _molecule.SpaceGroupName;
            if (string.IsNullOrWhiteSpace(name))
                return 'P';

            char c = char.ToUpperInvariant(name.Trim()[0]);
            return (c == 'P' || c == 'A' || c == 'B' || c == 'C' || c == 'F' || c == 'I' || c == 'R')
                ? c
                : 'P';
        }

        private double LorentzPolarizationCorrection(double theta)
        {
            double thetaRad = theta * Math.PI / 180;
            double cos2Theta = Math.Cos(2 * thetaRad);
            double sinTheta = Math.Sin(thetaRad);
            double cosTheta = Math.Cos(thetaRad);

            // Lorentz factor: 1 / (2 sin²θ cosθ)
            double lorentz = 1.0 / (2 * sinTheta * sinTheta * cosTheta);

            // Polarization factor for unpolarized radiation
            double polarization = (1 + cos2Theta * cos2Theta) / 2;

            return lorentz * polarization;
        }

        public void ApplyCorrections(List<DiffractionPeak> peaks)
        {
            // Placeholder for additional corrections (e.g. preferred orientation).
        }

        #endregion
    }
}
