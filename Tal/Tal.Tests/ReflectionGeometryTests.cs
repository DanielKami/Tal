using Tal.Core.PowderDiffraction;
using Xunit;

namespace Tal.Tests
{
    public class ReflectionGeometryTests
    {
        private static double Rad(double deg) => deg * Math.PI / 180.0;

        private static double[,] Metric(double a, double b, double c, double al, double be, double ga)
        {
            Assert.True(ReflectionGeometry.TryBuildReciprocalMetric(a, b, c, Rad(al), Rad(be), Rad(ga), out var q));
            return q;
        }

        // ----- rotation-group helpers (row-major 3x3 integer matrices) -----

        private static int[] Mul(int[] x, int[] y)
        {
            int[] r = new int[9];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                        r[i * 3 + j] += x[i * 3 + k] * y[k * 3 + j];
            return r;
        }

        private static bool Same(int[] a, int[] b) => a.SequenceEqual(b);

        private static List<int[]> Close(IEnumerable<int[]> generators)
        {
            var group = new List<int[]> { new[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 } };
            var gens = generators.ToList();
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (var g in group.ToList())
                    foreach (var h in gens)
                    {
                        var p = Mul(g, h);
                        if (!group.Any(m => Same(m, p))) { group.Add(p); grew = true; }
                    }
            }
            return group;
        }

        private static int CountOrbitSizes(double[,] q, double dMin, int[][] laue, out int sumMultiplicity,
                                           out Dictionary<(int, int, int), int> reps)
        {
            reps = new Dictionary<(int, int, int), int>();
            int points = 0;
            sumMultiplicity = 0;
            foreach (var (h, k, l) in ReflectionGeometry.EnumerateLatticePoints(q, 0.0, 1.0 / dMin))
            {
                points++;
                if (ReflectionGeometry.TryGetOrbitRepresentative(laue, h, k, l, out int m))
                {
                    reps[(h, k, l)] = m;
                    sumMultiplicity += m;
                }
            }
            return points;
        }

        // ----- enumeration -----

        [Fact]
        public void EnumerationMatchesBruteForceForTriclinicCell()
        {
            var q = Metric(7.3, 9.2, 11.1, 83, 97, 105);
            const double dMin = 1.4;
            double r2 = 1.0 / (dMin * dMin);

            var fast = ReflectionGeometry.EnumerateLatticePoints(q, 0.0, 1.0 / dMin).ToHashSet();

            var brute = new HashSet<(int, int, int)>();
            int box = (int)Math.Ceiling(11.1 / dMin) + 2;
            for (int h = -box; h <= box; h++)
                for (int k = -box; k <= box; k++)
                    for (int l = -box; l <= box; l++)
                        if ((h, k, l) != (0, 0, 0) && ReflectionGeometry.DStarSquared(q, h, k, l) <= r2 * (1 + 1e-12))
                            brute.Add((h, k, l));

            Assert.Equal(brute.Count, fast.Count);
            Assert.True(brute.SetEquals(fast));
        }

        [Fact]
        public void NoArbitraryIndexLimit()
        {
            // 120 Å cell, d_min = 3 Å  ->  |h| up to 40, the old code stopped at 30
            var q = Metric(120, 120, 120, 90, 90, 90);
            int maxH = ReflectionGeometry.EnumerateLatticePoints(q, 0.0, 1.0 / 3.0).Max(p => Math.Abs(p.H));
            Assert.Equal(40, maxH);
        }

        [Fact]
        public void ShellExcludesReflectionsBelowDStarMin()
        {
            var q = Metric(9.1, 11.3, 13.7, 90, 100, 90);
            double dStarMin = 0.2, dStarMax = 0.5;
            foreach (var (h, k, l) in ReflectionGeometry.EnumerateLatticePoints(q, dStarMin, dStarMax))
            {
                double d2 = ReflectionGeometry.DStarSquared(q, h, k, l);
                Assert.InRange(d2, dStarMin * dStarMin * (1 - 1e-9), dStarMax * dStarMax * (1 + 1e-9));
            }
        }

        [Fact]
        public void DegenerateCellIsRejected()
        {
            Assert.False(ReflectionGeometry.TryBuildReciprocalMetric(0, 5, 5, Rad(90), Rad(90), Rad(90), out _));
            Assert.False(ReflectionGeometry.TryBuildReciprocalMetric(5, 5, 5, Rad(0), Rad(0), Rad(0), out _));
        }

        // ----- Laue group orbits -----

        [Fact]
        public void LaueGroupAlwaysContainsIdentityAndInversion()
        {
            var laue = ReflectionGeometry.BuildLaueGroup(Array.Empty<int[]>());
            Assert.Equal(2, laue.Length);   // P1 -> -1 (Friedel)
        }

        [Fact]
        public void CubicOrbitsHaveTheTextbookMultiplicities()
        {
            // m-3m: all 48 signed permutation matrices
            var oh = new List<int[]>();
            int[][] perms = { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
                              new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
            foreach (var p in perms)
                for (int s = 0; s < 8; s++)
                {
                    int[] r = new int[9];
                    for (int row = 0; row < 3; row++)
                        r[row * 3 + p[row]] = ((s >> row) & 1) == 0 ? 1 : -1;
                    oh.Add(r);
                }
            var laue = ReflectionGeometry.BuildLaueGroup(oh);
            Assert.Equal(48, laue.Length);

            int Mult(int h, int k, int l)
            {
                // the representative is the largest member; query it directly
                var best = (h, k, l);
                foreach (var r in laue)
                {
                    var t = (h * r[0] + k * r[3] + l * r[6], h * r[1] + k * r[4] + l * r[7], h * r[2] + k * r[5] + l * r[8]);
                    if (t.CompareTo(best) > 0) best = t;
                }
                Assert.True(ReflectionGeometry.TryGetOrbitRepresentative(laue, best.Item1, best.Item2, best.Item3, out int m));
                return m;
            }

            Assert.Equal(6, Mult(1, 0, 0));
            Assert.Equal(12, Mult(1, 1, 0));
            Assert.Equal(8, Mult(1, 1, 1));
            Assert.Equal(24, Mult(2, 1, 0));
            Assert.Equal(48, Mult(3, 2, 1));
        }

        [Fact]
        public void EachOrbitIsVisitedExactlyOnce_Cubic()
        {
            var oh = new List<int[]>();
            int[][] perms = { new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
                              new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 } };
            foreach (var p in perms)
                for (int s = 0; s < 8; s++)
                {
                    int[] r = new int[9];
                    for (int row = 0; row < 3; row++) r[row * 3 + p[row]] = ((s >> row) & 1) == 0 ? 1 : -1;
                    oh.Add(r);
                }
            var laue = ReflectionGeometry.BuildLaueGroup(oh);
            var q = Metric(5.4, 5.4, 5.4, 90, 90, 90);

            int points = CountOrbitSizes(q, 1.2, laue, out int sum, out var reps);
            Assert.Equal(points, sum);          // the orbits partition the lattice points
            Assert.True(reps.Count < points / 10);
        }

        [Fact]
        public void EachOrbitIsVisitedExactlyOnce_Monoclinic_P21c()
        {
            // 2/m (unique axis b): identity, 2_b, inversion, m_b
            var laue = ReflectionGeometry.BuildLaueGroup(new[]
            {
                new[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 },
                new[] { -1, 0, 0, 0, 1, 0, 0, 0, -1 },
                new[] { -1, 0, 0, 0, -1, 0, 0, 0, -1 },
                new[] { 1, 0, 0, 0, -1, 0, 0, 0, 1 }
            });
            Assert.Equal(4, laue.Length);

            var q = Metric(9.1, 11.3, 13.7, 90, 100, 90);
            int points = CountOrbitSizes(q, 1.4, laue, out int sum, out var reps);
            Assert.Equal(points, sum);
            Assert.All(reps.Values, m => Assert.Contains(m, new[] { 2, 4 }));   // 2 on the b axis / (h0l) edge cases, 4 general
            Assert.Equal(2, reps[(0, 2, 0)]);
        }

        [Fact]
        public void EachOrbitIsVisitedExactlyOnce_Hexagonal_6overm()
        {
            // x' = x - y, y' = x, z' = z  (6-fold along c) and the mirror m_z
            int[] sixFold = { 1, -1, 0, 1, 0, 0, 0, 0, 1 };
            int[] mirrorZ = { 1, 0, 0, 0, 1, 0, 0, 0, -1 };
            var group = Close(new[] { sixFold, mirrorZ });
            Assert.Equal(12, group.Count);

            var laue = ReflectionGeometry.BuildLaueGroup(group);
            Assert.Equal(12, laue.Length);

            var q = Metric(8.2, 8.2, 12.5, 90, 90, 120);
            int points = CountOrbitSizes(q, 1.5, laue, out int sum, out var reps);
            Assert.Equal(points, sum);
            Assert.Equal(12, reps.Values.Max());
            Assert.Equal(2, reps[(0, 0, 1)]);
            Assert.Equal(6, reps[(1, 0, 0)]);
        }

        [Fact]
        public void EquivalentReflectionsHaveTheSameDSpacing()
        {
            var group = Close(new[] { new[] { 1, -1, 0, 1, 0, 0, 0, 0, 1 }, new[] { 1, 0, 0, 0, 1, 0, 0, 0, -1 } });
            var laue = ReflectionGeometry.BuildLaueGroup(group);
            var q = Metric(8.2, 8.2, 12.5, 90, 90, 120);

            int h = 3, k = 1, l = 2;
            double d0 = ReflectionGeometry.DStarSquared(q, h, k, l);
            foreach (var r in laue)
            {
                int h2 = h * r[0] + k * r[3] + l * r[6];
                int k2 = h * r[1] + k * r[4] + l * r[7];
                int l2 = h * r[2] + k * r[5] + l * r[8];
                Assert.Equal(d0, ReflectionGeometry.DStarSquared(q, h2, k2, l2), 9);
            }
        }
    }
}
