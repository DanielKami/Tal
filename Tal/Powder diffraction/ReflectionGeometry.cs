// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Powder diffraction module: pure reflection geometry (metric, hkl enumeration, Laue-group orbits).
// No dependency on Molecule, so it can be unit-tested on its own.

namespace Tal.Core.PowderDiffraction
{
    /// <summary>
    /// Reflection geometry of a lattice and of its Laue group, independent of the atomic structure.
    /// </summary>
    public static class ReflectionGeometry
    {
        /// <summary>
        /// Reciprocal metric tensor Q = G⁻¹ (1/Å²) of the cell (a, b, c in Å, angles in radians),
        /// so that d*² = 1/d² = hᵀ·Q·h. Returns false for a degenerate cell.
        /// </summary>
        public static bool TryBuildReciprocalMetric(double a, double b, double c,
                                                    double alpha, double beta, double gamma,
                                                    out double[,] q)
        {
            q = new double[3, 3];
            if (!(a > 0) || !(b > 0) || !(c > 0)) return false;

            double ca = Math.Cos(alpha), cb = Math.Cos(beta), cg = Math.Cos(gamma);

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

        /// <summary>
        /// All non-zero lattice points h,k,l with  dStarMin² ≤ hᵀQh ≤ dStarMax²  (an exact walk over the
        /// ellipsoid, no index box and no arbitrary limit; valid for oblique cells).
        /// The largest |h| is a / d_min (|k| ≤ b / d_min, |l| ≤ c / d_min).
        /// </summary>
        public static IEnumerable<(int H, int K, int L)> EnumerateLatticePoints(
            double[,] q, double dStarMin, double dStarMax)
        {
            double r2Max = dStarMax * dStarMax;
            double r2Min = dStarMin * dStarMin;

            double q11 = q[0, 0], q12 = q[0, 1], q13 = q[0, 2];
            double q22 = q[1, 1], q23 = q[1, 2], q33 = q[2, 2];

            double q11p = q11 - q13 * q13 / q33;
            double q12p = q12 - q13 * q23 / q33;
            double q22p = q22 - q23 * q23 / q33;
            double q11pp = q11p - q12p * q12p / q22p;
            if (!(q33 > 0) || !(q22p > 0) || !(q11pp > 0))
                yield break;

            const double eps = 1e-9;
            int hMax = (int)Math.Floor(Math.Sqrt(r2Max / q11pp) + eps);

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
                        if (h == 0 && k == 0 && l == 0) continue;

                        double dStar2 = DStarSquared(q, h, k, l);
                        if (dStar2 <= 0.0) continue;
                        if (dStar2 > r2Max * (1.0 + 1e-12) || dStar2 < r2Min * (1.0 - 1e-12)) continue;

                        yield return (h, k, l);
                    }
                }
            }
        }

        /// <summary>d*² = 1/d² = hᵀ·Q·h.</summary>
        public static double DStarSquared(double[,] q, int h, int k, int l)
        {
            return q[0, 0] * h * h + q[1, 1] * k * k + q[2, 2] * l * l +
                   2.0 * (q[0, 1] * h * k + q[0, 2] * h * l + q[1, 2] * k * l);
        }

        /// <summary>
        /// Laue group of the diffraction pattern: the given 3×3 integer rotation parts (row-major,
        /// x'_a = Σ_j R[a*3+j]·x_j) together with their inversion partners (Friedel's law), without duplicates.
        /// Identity and inversion are always included.
        /// </summary>
        public static int[][] BuildLaueGroup(IEnumerable<int[]> rotationParts)
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

            if (rotationParts != null)
            {
                foreach (var r in rotationParts)
                {
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
        /// Under the real-space operation x' = R x + t the structure factor obeys |F(h)| = |F(Rᵀh)|, i.e. the
        /// equivalent indices are the row vector h·R. Returns true only for the lexicographically largest
        /// member of the orbit (so each orbit is visited exactly once) and gives the orbit size = multiplicity.
        /// </summary>
        public static bool TryGetOrbitRepresentative(int[][] laue, int h, int k, int l, out int multiplicity)
        {
            multiplicity = 0;

            foreach (var r in laue)
            {
                int h2 = h * r[0] + k * r[3] + l * r[6];
                int k2 = h * r[1] + k * r[4] + l * r[7];
                int l2 = h * r[2] + k * r[5] + l * r[8];

                if (h2 > h || (h2 == h && (k2 > k || (k2 == k && l2 > l))))
                    return false;   // a larger equivalent reflection exists and will carry this orbit
            }

            var orbit = new HashSet<(int, int, int)>();
            foreach (var r in laue)
            {
                orbit.Add((h * r[0] + k * r[3] + l * r[6],
                           h * r[1] + k * r[4] + l * r[7],
                           h * r[2] + k * r[5] + l * r[8]));
            }

            multiplicity = orbit.Count;
            return true;
        }
    }
}
