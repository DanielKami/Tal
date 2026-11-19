// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Symmetric 3x3 covariance matrix of an atomic position in Cartesian axes (Å²).
// Keeps the correlations between Cartesian axes that appear whenever the cell
// is not orthogonal (fractional errors are independent, Cartesian ones are not).

using Microsoft.Xna.Framework;

namespace Tal
{
    /// <summary>
    /// Covariance of an atomic position in Cartesian axes (Å²).
    /// Column convention (x_cart = A · x_frac), the same as used for ADP transforms.
    /// </summary>
    public struct Cov3
    {
        public float XX, YY, ZZ, XY, XZ, YZ;

        public bool IsZero =>
            XX == 0f && YY == 0f && ZZ == 0f && XY == 0f && XZ == 0f && YZ == 0f;

        /// <summary>Per-axis standard deviations (sqrt of the diagonal), Å.</summary>
        public Vector3 Sigma => new Vector3(
            MathF.Sqrt(MathF.Max(XX, 0f)),
            MathF.Sqrt(MathF.Max(YY, 0f)),
            MathF.Sqrt(MathF.Max(ZZ, 0f)));

        public static Cov3 FromDiagonal(Vector3 sigma)
        {
            return new Cov3
            {
                XX = sigma.X * sigma.X,
                YY = sigma.Y * sigma.Y,
                ZZ = sigma.Z * sigma.Z
            };
        }

        /// <summary>
        /// C = A · diag(σ²) · Aᵀ for independent fractional errors σ.
        /// A = FractionaltoCarttesianMatrix (column convention).
        /// </summary>
        public static Cov3 FromFractionalSigma(Vector3 fracSigma, Matrix A)
        {
            double[] s2 =
            {
                (double)fracSigma.X * fracSigma.X,
                (double)fracSigma.Y * fracSigma.Y,
                (double)fracSigma.Z * fracSigma.Z
            };

            double Cij(int i, int j)
            {
                double sum = 0;
                for (int k = 0; k < 3; k++)
                    sum += (double)A[i, k] * A[j, k] * s2[k];
                return sum;
            }

            return new Cov3
            {
                XX = (float)Cij(0, 0),
                YY = (float)Cij(1, 1),
                ZZ = (float)Cij(2, 2),
                XY = (float)Cij(0, 1),
                XZ = (float)Cij(0, 2),
                YZ = (float)Cij(1, 2)
            };
        }

        private double[,] ToArray()
        {
            return new double[,]
            {
                { XX, XY, XZ },
                { XY, YY, YZ },
                { XZ, YZ, ZZ }
            };
        }

        /// <summary>
        /// Covariance after a rotation / symmetry operation: C' = R · C · Rᵀ
        /// (R = R_cart, column convention, only the upper-left 3x3 block is used).
        /// </summary>
        public Cov3 Rotated(Matrix R)
        {
            double[,] c = ToArray();
            double[,] rc = new double[3, 3];
            double[,] res = new double[3, 3];

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < 3; k++) sum += (double)R[i, k] * c[k, j];
                    rc[i, j] = sum;
                }

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < 3; k++) sum += rc[i, k] * R[j, k];
                    res[i, j] = sum;
                }

            return new Cov3
            {
                XX = (float)res[0, 0],
                YY = (float)res[1, 1],
                ZZ = (float)res[2, 2],
                XY = (float)(0.5 * (res[0, 1] + res[1, 0])),
                XZ = (float)(0.5 * (res[0, 2] + res[2, 0])),
                YZ = (float)(0.5 * (res[1, 2] + res[2, 1]))
            };
        }

        /// <summary>Variance of a linear functional: gᵀ · C · g.</summary>
        public double Quadratic(Vector3 g)
        {
            double v =
                (double)g.X * g.X * XX + (double)g.Y * g.Y * YY + (double)g.Z * g.Z * ZZ +
                2.0 * ((double)g.X * g.Y * XY + (double)g.X * g.Z * XZ + (double)g.Y * g.Z * YZ);
            return Math.Max(v, 0.0);
        }
    }
}
