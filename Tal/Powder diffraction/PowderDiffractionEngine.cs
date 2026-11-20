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

        /// <exception cref="UnsupportedElementException">
        /// The structure contains an element without tabulated X-ray scattering factors.
        /// </exception>
        public List<DiffractionPeak> CalculatePeaks(
            double minTwoTheta,
            double maxTwoTheta,
            double wavelength = 1.5406,
            double debyeWallerB = 0.5,
            bool useLPCorrection = true)
        {
            _wavelength = wavelength;
            var peaks = new List<DiffractionPeak>();

            if (_molecule.AsymmetricPart.Count == 0 || wavelength <= 0)
                return peaks;

            // Throws UnsupportedElementException instead of silently dropping atoms from F(hkl).
            _structureFactor.PrepareCell();

            // Reciprocal metric tensor Q = G⁻¹:  d*² = hᵀ Q h
            if (!TryBuildReciprocalMetric(out double[,] q))
                return peaks;

            double maxTT = Math.Min(maxTwoTheta, 180.0);
            double minTT = Math.Max(minTwoTheta, 0.0);
            if (maxTT <= 0.0 || minTT >= maxTT)
                return peaks;

            // d* = 1/d = 2 sinθ / λ
            double dStarMax = 2.0 * Math.Sin(maxTT / 2.0 * Math.PI / 180.0) / wavelength;
            double dStarMin = 2.0 * Math.Sin(minTT / 2.0 * Math.PI / 180.0) / wavelength;
            double r2Max = dStarMax * dStarMax;
            double r2Min = dStarMin * dStarMin;

            int[][] laue = BuildLaueOperations();
            char centering = GetCenteringSymbol();

            // The hkl range follows from the metric, not from a fixed box: all lattice points inside
            // the ellipsoid  hᵀ Q h ≤ d*max²  are visited (no arbitrary index limit, correct for any cell).
            double q11 = q[0, 0], q12 = q[0, 1], q13 = q[0, 2];
            double q22 = q[1, 1], q23 = q[1, 2], q33 = q[2, 2];

            double q11p = q11 - q13 * q13 / q33;
            double q12p = q12 - q13 * q23 / q33;
            double q22p = q22 - q23 * q23 / q33;
            double q11pp = q11p - q12p * q12p / q22p;
            if (!(q33 > 0) || !(q22p > 0) || !(q11pp > 0))
                return peaks;

            const double eps = 1e-9;
            int hMax = (int)Math.Floor(Math.Sqrt(r2Max / q11pp) + eps);   // = a / d_min

            for (int h = -hMax; h <= hMax; h++)
            {
                double discK = (q12p * h) * (q12p * h) - q22p * (q11p * h * h - r2Max);
                if (discK < 0) continue;
                double kc = -q12p * h / q22p;
                double kd = Math.Sqrt(discK) / q22p;
                int kLo = (int)Math.Ceiling(kc - kd - eps);
                int kHi = (int)Math.Floor(kc + kd + eps);

                for (int k = kLo; k <= kHi; k++)
                {
                    double b = q13 * h + q23 * k;
                    double c0 = q11 * h * h + q22 * k * k + 2.0 * q12 * h * k - r2Max;
                    double discL = b * b - q33 * c0;
                    if (discL < 0) continue;
                    double lc = -b / q33;
                    double ld = Math.Sqrt(discL) / q33;
                    int lLo = (int)Math.Ceiling(lc - ld - eps);
                    int lHi = (int)Math.Floor(lc + ld + eps);

                    for (int l = lLo; l <= lHi; l++)
                    {
                        if (h == 0 && k == 0 && l == 0)
                            continue;

                        double dStar2 = q11 * h * h + q22 * k * k + q33 * l * l +
                                        2.0 * (q12 * h * k + q13 * h * l + q23 * k * l);

                        if (dStar2 <= 0.0) continue;
                        if (dStar2 > r2Max * (1.0 + 1e-12) || dStar2 < r2Min * (1.0 - 1e-12)) continue;

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
                        if (!TryGetOrbitRepresentative(laue, h, k, l, out int multiplicity))
                            continue;

                        double F = _structureFactor.Calculate(h, k, l, theta, wavelength, debyeWallerB);
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
                            ReciprocalD = dStar2 > 0 ? Math.Sqrt(dStar2) : 0.0
                        });
                    }
                }
            }

            return peaks.OrderBy(p => p.TwoTheta).ThenByDescending(p => p.Intensity).ToList();
        }

        #region Lattice metric

        /// <summary>Q = G⁻¹ where G is the direct metric tensor (cell angles in radians).</summary>
        private bool TryBuildReciprocalMetric(out double[,] q)
        {
            q = new double[3, 3];

            double a = _molecule.a, b = _molecule.b, c = _molecule.c;
            if (a <= 0 || b <= 0 || c <= 0) return false;

            double ca = Math.Cos(_molecule.alpha);
            double cb = Math.Cos(_molecule.beta);
            double cg = Math.Cos(_molecule.gamma);

            double g11 = a * a, g22 = b * b, g33 = c * c;
            double g12 = a * b * cg, g13 = a * c * cb, g23 = b * c * ca;

            double det = g11 * (g22 * g33 - g23 * g23)
                       - g12 * (g12 * g33 - g23 * g13)
                       + g13 * (g12 * g23 - g22 * g13);
            if (!(det > 1e-12)) return false;

            q[0, 0] = (g22 * g33 - g23 * g23) / det;
            q[0, 1] = q[1, 0] = (g13 * g23 - g12 * g33) / det;
            q[0, 2] = q[2, 0] = (g12 * g23 - g13 * g22) / det;
            q[1, 1] = (g11 * g33 - g13 * g13) / det;
            q[1, 2] = q[2, 1] = (g12 * g13 - g11 * g23) / det;
            q[2, 2] = (g11 * g22 - g12 * g12) / det;
            return true;
        }

        #endregion

        #region Symmetry equivalence of reflections

        /// <summary>
        /// Rotation parts (3×3 integer matrices, row-major) of ALL space-group operations plus their
        /// inversion partners (Friedel's law): the Laue group of the diffraction pattern.
        /// Independent of the symmetry operations that are switched on or off in the 3D view.
        /// </summary>
        private int[][] BuildLaueOperations()
        {
            var list = new List<int[]>();

            void Add(int[] r)
            {
                foreach (var m in list)
                {
                    bool same = true;
                    for (int i = 0; i < 9; i++)
                        if (m[i] != r[i]) { same = false; break; }
                    if (same) return;
                }
                list.Add(r);
            }

            int[] identity = { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
            Add(identity);
            Add(Negate(identity));

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
                    Add(r);
                    Add(Negate(r));
                }
            }

            return list.ToArray();
        }

        private static int[] Negate(int[] r)
        {
            int[] n = new int[9];
            for (int i = 0; i < 9; i++) n[i] = -r[i];
            return n;
        }

        /// <summary>
        /// Under the real-space operation x' = R x + t the structure factor obeys
        /// |F(h)| = |F(Rᵀh)|, i.e. the equivalent indices are the row vector h·R.
        /// Returns true only for the lexicographically largest member of the orbit (so every
        /// orbit is processed exactly once) and gives the orbit size = multiplicity.
        /// </summary>
        private static bool TryGetOrbitRepresentative(int[][] ops, int h, int k, int l, out int multiplicity)
        {
            multiplicity = 0;

            foreach (var r in ops)
            {
                int h2 = h * r[0] + k * r[3] + l * r[6];
                int k2 = h * r[1] + k * r[4] + l * r[7];
                int l2 = h * r[2] + k * r[5] + l * r[8];

                if (h2 > h || (h2 == h && (k2 > k || (k2 == k && l2 > l))))
                    return false;   // a larger equivalent reflection exists, it will carry this orbit
            }

            var orbit = new HashSet<(int, int, int)>();
            foreach (var r in ops)
            {
                orbit.Add((h * r[0] + k * r[3] + l * r[6],
                           h * r[1] + k * r[4] + l * r[7],
                           h * r[2] + k * r[5] + l * r[8]));
            }

            multiplicity = orbit.Count;
            return true;
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
