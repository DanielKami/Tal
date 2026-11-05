// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Calculation module: Eigen.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;

namespace Tal.Calculate
{
 /// <summary>
 /// Provides eigenvalue and eigenvector decomaftersition for matrices.
 /// Implements numerical algorithms for both symmetric and non-symmetric matrices,
 /// including balancing, Hessenberg reduction, and QR decomaftersition.
 /// </summary>
    public static class Eigen
    {
        #region Constants

        private const float RADIX = 2.0f;

        #endregion

        #region Public Methods

 /// <summary>
 /// Computes the eigenvalues and eigenvectors of a 3x3 symmetric matrix.
 /// </summary>
 /// <param name="A">The input matrix (modified in place to contain eigenvectors).</param>
 /// <returns>A transformed matrix containing the eigenvectors.</returns>
        public static Matrix main(ref Matrix A)
        {
            double[] u = new double[3];
            double[,] a = new double[5, 5];

 // Copy matrix data
            a[0, 0] = A.M11;
            a[0, 1] = A.M12;
            a[0, 2] = A.M13;

            a[1, 0] = A.M21;
            a[1, 1] = A.M22;
            a[1, 2] = A.M23;

            a[2, 0] = A.M31;
            a[2, 1] = A.M32;
            a[2, 2] = A.M33;

 // Compute eigenvalues and eigenvectors (symmetric case)
            n_eigen_symm(ref a, 3, ref u);

 // Copy eigenvectors back
            A.M11 = (float)a[0, 0];
            A.M12 = (float)a[0, 1];
            A.M13 = (float)a[0, 2];

            A.M21 = (float)a[1, 0];
            A.M22 = (float)a[1, 1];
            A.M23 = (float)a[1, 2];

            A.M31 = (float)a[2, 0];
            A.M32 = (float)a[2, 1];
            A.M33 = (float)a[2, 2];

 // Create diagonal matrix from eigenvalues
            Matrix B = Matrix.Identity;
            B.M11 = (float)Math.Sqrt(Math.Abs(u[0]));
            B.M22 = (float)Math.Sqrt(Math.Abs(u[1]));
            B.M33 = (float)Math.Sqrt(Math.Abs(u[2]));

            A = Matrix.Transpose(A * B);
            return -A;
        }

        #endregion

        #region Matrix Balancing

 /// <summary>
 /// Balances a matrix to improve numerical stability for eigenvalue computation.
 /// </summary>
 /// <param name="a">The matrix to balance (modified in place).</param>
 /// <param name="n">The dimension of the matrix.</param>
        private static void balanc(ref double[,] a, int n)
        {
            int i, j, last = 0;
            double s, r, g, f, c, sqrdx;
            sqrdx = RADIX * RADIX;

            while (last == 0)
            {
                last = 1;
                for (i = 0; i < n; i++)
                {
                    r = c = 0.0;
                    for (j = 0; j < n; j++)
                        if (j != i)
                        {
                            c += Math.Abs(a[j, i]);
                            r += Math.Abs(a[i, j]);
                        }

                    if (c != 0.0 && r != 0.0)
                    {
                        g = r / RADIX;
                        f = 1.0;
                        s = c + r;

                        while (c < g)
                        {
                            f *= RADIX;
                            c *= sqrdx;
                        }
                        g = r * RADIX;
                        while (c > g)
                        {
                            f /= RADIX;
                            c /= sqrdx;
                        }

                        if ((c + r) / f < 0.95 * s)
                        {
                            last = 0;
                            g = 1.0 / f;
                            for (j = 0; j < n; j++)
                                a[i, j] *= g;
                            for (j = 0; j < n; j++)
                                a[j, i] *= f;
                        }
                    }
                }
            }
        }

        #endregion

        #region Hessenberg Reduction

 /// <summary>
 /// Reduces a matrix to upper Hessenberg form using elementary similarity transformations.
 /// </summary>
 /// <param name="a">The matrix to reduce (modified in place).</param>
 /// <param name="n">The dimension of the matrix.</param>
        private static void elmhes(ref double[,] a, int n)
        {
            int i, j, m;
            double y, x;

            for (m = 1; m < n - 1; m++)
            {
                x = 0.0;
                i = m;

 // Find pivot
                for (j = m; j < n; j++)
                {
                    if (Math.Abs(a[j, m - 1]) > Math.Abs(x))
                    {
                        x = a[j, m - 1];
                        i = j;
                    }
                }

                if (i != m)
                {
 // Swap rows
                    for (j = m - 1; j < n; j++)
                        SWAP(ref a[i, j], a[m, j]);
 // Swap columns
                    for (j = 0; j < n; j++)
                        SWAP(ref a[j, i], a[j, m]);
                }

                if (x != 0.0)
                {
                    for (i = m + 1; i < n; i++)
                    {
                        if ((y = a[i, m - 1]) != 0.0)
                        {
                            y /= x;
                            a[i, m - 1] = y;
                            for (j = m; j < n; j++)
                                a[i, j] -= y * a[m, j];
                            for (j = 0; j < n; j++)
                                a[j, m] += y * a[j, i];
                        }
                    }
                }
            }
        }

        #endregion

        #region QR Algorithm

 /// <summary>
 /// Computes eigenvalues of a matrix in upper Hessenberg form using the QR algorithm.
 /// </summary>
 /// <param name="a">The matrix in Hessenberg form (modified in place).</param>
 /// <param name="n">The dimension of the matrix.</param>
 /// <param name="wr">Array to store real parts of eigenvalues.</param>
 /// <param name="wi">Array to store imaginary parts of eigenvalues.</param>
 /// <returns>A string indicating success or failure.</returns>
        private static string hqr(ref double[,] a, int n, ref double[] wr, ref double[] wi)
        {
            string str = "ok";
            int nn, m, l, k, j, its, i, mmin;
            double z, y, x, w, v, u, t, s, r, q, p, anorm;

            p = q = r = 0.0;
            anorm = 0.0;

 // Compute norm
            for (i = 0; i < n; i++)
                for (j = i - 1 > 0 ? i - 1 : 0; j < n; j++)
                    anorm += Math.Abs(a[i, j]);

            nn = n - 1;
            t = 0.0;

            while (nn >= 0)
            {
                its = 0;
                do
                {
 // Find subdiagonal element to split matrix
                    for (l = nn; l > 0; l--)
                    {
                        s = Math.Abs(a[l - 1, l - 1]) + Math.Abs(a[l, l]);
                        if (s == 0.0)
                            s = anorm;
                        if (Math.Abs(a[l, l - 1]) + s == s)
                        {
                            a[l, l - 1] = 0.0;
                            break;
                        }
                    }

                    x = a[nn, nn];

                    if (l == nn)
                    {
 // Single eigenvalue
                        wr[nn] = x + t;
                        wi[nn--] = 0.0;
                    }
                    else
                    {
                        y = a[nn - 1, nn - 1];
                        w = a[nn, nn - 1] * a[nn - 1, nn];

                        if (l == nn - 1)
                        {
 // Two eigenvalues (maybe complex conjugate)
                            p = 0.5 * (y - x);
                            q = p * p + w;
                            z = Math.Sqrt(Math.Abs(q));
                            x += t;

                            if (q >= 0.0)
                            {
 // Real eigenvalues
                                z = p + SIGN(z, p);
                                wr[nn - 1] = wr[nn] = x + z;
                                if (z != 0.0)
                                    wr[nn] = x - w / z;
                                wi[nn - 1] = wi[nn] = 0.0;
                            }
                            else
                            {
 // Complex eigenvalues
                                wr[nn - 1] = wr[nn] = x + p;
                                wi[nn - 1] = -(wi[nn] = z);
                            }
                            nn -= 2;
                        }
                        else
                        {
 // Perform QR iteration
                            if (its == 30)
                            {
                                str = "[hqr] too many iterations.\n";
                                break;
                            }

                            if (its == 10 || its == 20)
                            {
                                t += x;
                                for (i = 0; i < nn + 1; i++)
                                    a[i, i] -= x;
                                s = Math.Abs(a[nn, nn - 1]) + Math.Abs(a[nn - 1, nn - 2]);
                                y = x = 0.75 * s;
                                w = -0.4375 * s * s;
                            }

                            ++its;

 // Find lower subdiagonal element
                            for (m = nn - 2; m >= l; m--)
                            {
                                z = a[m, m];
                                r = x - z;
                                s = y - z;
                                p = (r * s - w) / a[m + 1, m] + a[m, m + 1];
                                q = a[m + 1, m + 1] - z - r - s;
                                r = a[m + 2, m + 1];
                                s = Math.Abs(p) + Math.Abs(q) + Math.Abs(r);
                                p /= s;
                                q /= s;
                                r /= s;

                                if (m == l)
                                    break;

                                u = Math.Abs(a[m, m - 1]) * (Math.Abs(q) + Math.Abs(r));
                                v = Math.Abs(p) * (Math.Abs(a[m - 1, m - 1]) + Math.Abs(z) + Math.Abs(a[m + 1, m + 1]));
                                if (u + v == v)
                                    break;
                            }

 // QR transformation
                            for (i = m; i < nn - 1; i++)
                            {
                                a[i + 2, i] = 0.0;
                                if (i != m)
                                    a[i + 2, i - 1] = 0.0;
                            }

                            for (k = m; k < nn; k++)
                            {
                                if (k != m)
                                {
                                    p = a[k, k - 1];
                                    q = a[k + 1, k - 1];
                                    r = 0.0;
                                    if (k + 1 != nn)
                                        r = a[k + 2, k - 1];
                                    if ((x = Math.Abs(p) + Math.Abs(q) + Math.Abs(r)) != 0.0)
                                    {
                                        p /= x;
                                        q /= x;
                                        r /= x;
                                    }
                                }

                                if ((s = SIGN(Math.Sqrt(p * p + q * q + r * r), p)) != 0.0)
                                {
                                    if (k == m)
                                    {
                                        if (l != m)
                                            a[k, k - 1] = -a[k, k - 1];
                                    }
                                    else
                                        a[k, k - 1] = -s * x;

                                    p += s;
                                    x = p / s;
                                    y = q / s;
                                    z = r / s;
                                    q /= p;
                                    r /= p;

 // Transform rows
                                    for (j = k; j < nn + 1; j++)
                                    {
                                        p = a[k, j] + q * a[k + 1, j];
                                        if (k + 1 != nn)
                                        {
                                            p += r * a[k + 2, j];
                                            a[k + 2, j] -= p * z;
                                        }
                                        a[k + 1, j] -= p * y;
                                        a[k, j] -= p * x;
                                    }

                                    mmin = nn < k + 3 ? nn : k + 3;

 // Transform columns
                                    for (i = l; i < mmin + 1; i++)
                                    {
                                        p = x * a[i, k] + y * a[i, k + 1];
                                        if (k != nn)
                                        {
                                            p += z * a[i, k + 2];
                                            a[i, k + 2] -= p * r;
                                        }
                                        a[i, k + 1] -= p * q;
                                        a[i, k] -= p;
                                    }
                                }
                            }
                        }
                    }
                } while (l + 1 < nn);
            }

            return str;
        }

        #endregion

        #region Symmetric Eigenvalue Algorithms

 /// <summary>
 /// Computes eigenvalues of a symmetric matrix using Householder reduction and QL algorithm.
 /// </summary>
 /// <param name="a">The symmetric matrix (modified in place).</param>
 /// <param name="n">The dimension of the matrix.</param>
 /// <param name="eval">Array to store eigenvalues.</param>
 /// <returns>0 on success.</returns>
        private static int n_eigen_symm(ref double[,] a, int n, ref double[] eval)
        {
            double[] e = new double[n];
            tred2(ref a, n, ref eval, ref e);
            tqli(ref eval, ref e, n, ref a);
            return 0;
        }

 /// <summary>
 /// Reduces a symmetric matrix to tridiagonal form using Householder transformations.
 /// </summary>
 /// <param name="a">The symmetric matrix (modified in place).</param>
 /// <param name="n">The dimension of the matrix.</param>
 /// <param name="d">Array to store diagonal elements.</param>
 /// <param name="e">Array to store off-diagonal elements.</param>
        private static void tred2(ref double[,] a, int n, ref double[] d, ref double[] e)
        {
            int l, k, j, i;
            double scale, hh, h, g, f;

            for (i = n - 1; i > 0; i--)
            {
                l = i - 1;
                h = scale = 0.0;

                if (l > 0)
                {
 // Compute scale factor
                    for (k = 0; k < l + 1; k++)
                        scale += Math.Abs(a[i, k]);

                    if (scale == 0.0)
                        e[i] = a[i, l];
                    else
                    {
                        for (k = 0; k < l + 1; k++)
                        {
                            a[i, k] /= scale;
                            h += a[i, k] * a[i, k];
                        }

                        f = a[i, l];
                        g = f >= 0.0 ? -Math.Sqrt(h) : Math.Sqrt(h);
                        e[i] = scale * g;
                        h -= f * g;
                        a[i, l] = f - g;
                        f = 0.0;

 // Apply Householder transformation
                        for (j = 0; j < l + 1; j++)
                        {
                            a[j, i] = a[i, j] / h;
                            g = 0.0;
                            for (k = 0; k < j + 1; k++)
                                g += a[j, k] * a[i, k];
                            for (k = j + 1; k < l + 1; k++)
                                g += a[k, j] * a[i, k];
                            e[j] = g / h;
                            f += e[j] * a[i, j];
                        }

                        hh = f / (h + h);
                        for (j = 0; j < l + 1; j++)
                        {
                            f = a[i, j];
                            e[j] = g = e[j] - hh * f;
                            for (k = 0; k < j + 1; k++)
                                a[j, k] -= f * e[k] + g * a[i, k];
                        }
                    }
                }
                else
                    e[i] = a[i, l];

                d[i] = h;
            }

            d[0] = 0.0;
            e[0] = 0.0;

 // Accumulate transformations
            for (i = 0; i < n; i++)
            {
                l = i;
                if (d[i] != 0.0)
                {
                    for (j = 0; j < l; j++)
                    {
                        g = 0.0;
                        for (k = 0; k < l; k++)
                            g += a[i, k] * a[k, j];
                        for (k = 0; k < l; k++)
                            a[k, j] -= g * a[k, i];
                    }
                }
                d[i] = a[i, i];
                a[i, i] = 1.0;
                for (j = 0; j < l; j++)
                    a[j, i] = a[i, j] = 0.0;
            }
        }

 /// <summary>
 /// Computes eigenvalues and eigenvectors of a tridiagonal matrix using the QL algorithm.
 /// </summary>
 /// <param name="d">Array containing diagonal elements (modified in place).</param>
 /// <param name="e">Array containing off-diagonal elements (modified in place).</param>
 /// <param name="n">The dimension of the matrix.</param>
 /// <param name="z">Matrix containing eigenvectors (modified in place).</param>
 /// <returns>A string indicating success or failure.</returns>
        private static string tqli(ref double[] d, ref double[] e, int n, ref double[,] z)
        {
            string str = "ok";
            int m, l, iter, i, k;
            double s, r, p, g, f, dd, c, b;

 // Shift e array
            for (i = 1; i < n; i++)
                e[i - 1] = e[i];
            e[n - 1] = 0.0;

            for (l = 0; l < n; l++)
            {
                iter = 0;
                do
                {
 // Find subdiagonal element to split
                    for (m = l; m < n - 1; m++)
                    {
                        dd = Math.Abs(d[m]) + Math.Abs(d[m + 1]);
                        if (Math.Abs(e[m]) + dd == dd)
                            break;
                    }

                    if (m != l)
                    {
                        if (iter++ == 30)
                        {
                            str = "[tqli] Too many iterations in tqli.\n";
                            break;
                        }

 // Form shift
                        g = (d[l + 1] - d[l]) / (2.0 * e[l]);
                        r = pythag(g, 1.0);
                        g = d[m] - d[l] + e[l] / (g + SIGN(r, g));
                        s = c = 1.0;
                        p = 0.0;

 // QL transformation
                        for (i = m - 1; i >= l; i--)
                        {
                            f = s * e[i];
                            b = c * e[i];
                            e[i + 1] = r = pythag(f, g);

                            if (r == 0.0)
                            {
                                d[i + 1] -= p;
                                e[m] = 0.0;
                                break;
                            }

                            s = f / r;
                            c = g / r;
                            g = d[i + 1] - p;
                            r = (d[i] - g) * s + 2.0 * c * b;
                            d[i + 1] = g + (p = s * r);
                            g = c * r - b;

 // Apply transformation to eigenvectors
                            for (k = 0; k < n; k++)
                            {
                                f = z[k, i + 1];
                                z[k, i + 1] = s * z[k, i] + c * f;
                                z[k, i] = c * z[k, i] - s * f;
                            }
                        }

                        if (r == 0.0 && i >= l)
                            continue;

                        d[l] -= p;
                        e[l] = g;
                        e[m] = 0.0;
                    }
                } while (m != l);
            }

            return str;
        }

        #endregion

        #region General Eigenvalue Algorithms

 /// <summary>
 /// Computes eigenvalues of a general matrix using balancing, Hessenberg reduction, and QR algorithm.
 /// </summary>
 /// <param name="a">The matrix (modified in place).</param>
 /// <param name="n">The dimension of the matrix.</param>
 /// <param name="wr">Array to store real parts of eigenvalues.</param>
 /// <param name="wi">Array to store imaginary parts of eigenvalues.</param>
        private static void n_eigen(ref double[,] a, int n, double[] wr, double[] wi)
        {
            balanc(ref a, n);
            elmhes(ref a, n);
            hqr(ref a, n, ref wr, ref wi);
        }

        #endregion

        #region Utility Methods

 /// <summary>
 /// Swaps two double values.
 /// </summary>
 /// <param name="a">First value (modified).</param>
 /// <param name="b">Second value (modified).</param>
        private static void SWAP(ref double a, double b)
        {
            double t = a;
            a = b;
            b = t;
        }

 /// <summary>
 /// Returns the sign of a with the sign of b.
 /// </summary>
 /// <param name="a">The value to sign.</param>
 /// <param name="b">The sign reference.</param>
 /// <returns>|a| if b >= 0, otherwise -|a|.</returns>
        private static double SIGN(double a, double b)
        {
            if (b >= 0.0)
                return Math.Abs(a);
            else
                return -Math.Abs(a);
        }

 /// <summary>
 /// Computes the square of a number.
 /// </summary>
        private static double SQR(double a) { return a * a; }

 /// <summary>
 /// Computes sqrt(a^2 + b^2) without destructive underflow/overflow.
 /// </summary>
        private static double pythag(double a, double b)
        {
            double absa = Math.Abs(a);
            double absb = Math.Abs(b);

            if (absa > absb)
                return absa * Math.Sqrt(1.0 + SQR(absb / absa));
            else
                return absb == 0.0 ? 0.0 : absb * Math.Sqrt(1.0 + SQR(absa / absb));
        }

        #endregion
    }
}