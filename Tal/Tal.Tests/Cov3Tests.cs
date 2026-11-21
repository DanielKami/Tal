using Microsoft.Xna.Framework;
using Xunit;

namespace Tal.Tests
{
    public class Cov3Tests
    {
        // column convention: x_cart = A · x_frac
        private static Matrix Orthorhombic(float a, float b, float c) =>
            new Matrix(a, 0, 0, 0,
                       0, b, 0, 0,
                       0, 0, c, 0,
                       0, 0, 0, 1);

        private static Matrix Monoclinic(float a, float b, float c, float betaDeg)
        {
            float be = MathHelper.ToRadians(betaDeg);
            // columns = cell vectors a=(a,0,0) b=(0,b,0) c=(c cosβ, 0, c sinβ)
            return new Matrix(a, 0, c * MathF.Cos(be), 0,
                              0, b, 0, 0,
                              0, 0, c * MathF.Sin(be), 0,
                              0, 0, 0, 1);
        }

        private static void Close(double expected, double actual, double relative = 1e-4)
        {
            Assert.True(Math.Abs(actual - expected) <= relative * Math.Max(Math.Abs(expected), 1e-12),
                        $"expected {expected:G9}, got {actual:G9}");
        }

        [Fact]
        public void OrthogonalCellGivesADiagonalCovariance()
        {
            var cov = Cov3.FromFractionalSigma(new Vector3(0.001f, 0.002f, 0.003f), Orthorhombic(10, 12, 15));

            Close(Math.Pow(10 * 0.001, 2), cov.XX);
            Close(Math.Pow(12 * 0.002, 2), cov.YY);
            Close(Math.Pow(15 * 0.003, 2), cov.ZZ);
            Assert.Equal(0f, cov.XY);
            Assert.Equal(0f, cov.XZ);
            Assert.Equal(0f, cov.YZ);
        }

        [Fact]
        public void ObliqueCellGivesCorrelatedAxes()
        {
            var a = Monoclinic(9.1f, 11.3f, 13.7f, 100f);
            var s = new Vector3(0.0004f, 0.0003f, 0.0005f);
            var cov = Cov3.FromFractionalSigma(s, a);

            // C = A·diag(σ²)·Aᵀ
            double[,] am = { { a.M11, a.M12, a.M13 }, { a.M21, a.M22, a.M23 }, { a.M31, a.M32, a.M33 } };
            double[] s2 = { s.X * s.X, s.Y * s.Y, s.Z * s.Z };
            double Expected(int i, int j)
            {
                double sum = 0;
                for (int k = 0; k < 3; k++) sum += am[i, k] * am[j, k] * s2[k];
                return sum;
            }

            Close(Expected(0, 0), cov.XX);
            Close(Expected(2, 2), cov.ZZ);
            Close(Expected(0, 2), cov.XZ);
            Assert.NotEqual(0f, cov.XZ);        // the correlation that per-axis variances throw away
        }

        [Fact]
        public void TwoFoldRotationFlipsTheMixedTerms()
        {
            var cov = new Cov3 { XX = 4, YY = 5, ZZ = 6, XY = 0.5f, XZ = 1.5f, YZ = -0.7f };
            var rotated = cov.Rotated(Matrix.CreateRotationZ(MathF.PI));   // diag(-1,-1,1)

            Assert.Equal(4, rotated.XX, 4);
            Assert.Equal(5, rotated.YY, 4);
            Assert.Equal(6, rotated.ZZ, 4);
            Assert.Equal(0.5f, rotated.XY, 4);     // (-x)(-y)
            Assert.Equal(-1.5f, rotated.XZ, 4);
            Assert.Equal(0.7f, rotated.YZ, 4);
        }

        [Fact]
        public void InversionLeavesTheCovarianceUnchanged()
        {
            var cov = new Cov3 { XX = 4, YY = 5, ZZ = 6, XY = 0.5f, XZ = 1.5f, YZ = -0.7f };
            var inv = new Matrix(-1, 0, 0, 0, 0, -1, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1);
            var rotated = cov.Rotated(inv);

            Assert.Equal(cov.XX, rotated.XX, 5);
            Assert.Equal(cov.XZ, rotated.XZ, 5);
            Assert.Equal(cov.YZ, rotated.YZ, 5);
        }

        [Fact]
        public void QuadraticFormAndSigmaAreConsistent()
        {
            var cov = new Cov3 { XX = 4, YY = 9, ZZ = 16, XY = 1, XZ = 0, YZ = 0 };

            Assert.Equal(4.0, cov.Quadratic(new Vector3(1, 0, 0)), 9);
            Assert.Equal(4.0 + 9.0 + 2 * 1.0, cov.Quadratic(new Vector3(1, 1, 0)), 6);
            Assert.Equal(new Vector3(2, 3, 4), cov.Sigma);
        }
    }
}
