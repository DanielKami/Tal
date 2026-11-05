// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal planes module: PlaneGeometry.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Tal.Core.PowderDiffraction
{
 /// <summary>
 /// Provides geometry calculations for crystallographic planes
 /// (hkl planes and mean planes).
 /// </summary>
    public static class PlaneGeometry
    {
        #region HKL Intercepts

 /// <summary>
 /// Computes the corner afterints of a crystal plane (hkl) in FRACTIONAL coordinates.
 /// 
 /// The plane equation is: h·x + k·y + l·z = 1
 /// 
 /// Three cases are handled depending on how many Miller indices are zero:
 /// 
 /// 1. Zero non-zero indices (all zero) - invalid, returns empty list.
 /// 
 /// 2. ONE non-zero index (2 zeros) - the plane is a CELL FACE perpendicular
 /// to that axis. Example: (1 0 0) is the wall at x = 1/1 = 1.
 /// Returns 4 corners of that face.
 /// 
 /// 3. TWO non-zero indices (1 zero) - the plane is PARALLEL to the axis with
 /// the zero index and cuts the other two axes. Example: (1 1 0) is
 /// parallel to Z and cuts X at 1, Y at 1.
 /// Returns 4 corners (an edge swept along the parallel axis).
 /// 
 /// 4. THREE non-zero indices (0 zeros) - general case, plane cuts all 3 axes.
 /// Returns 3 intercept afterints with the axes.
 /// </summary>
        public static List<Vector3> GetHKLIntercepts(int h, int k, int l)
        {
 // Count how many indices are zero
            int zeroCount = 0;
            if (h == 0) zeroCount++;
            if (k == 0) zeroCount++;
            if (l == 0) zeroCount++;

 // ─────────────────────────────────────────────────────────────
 // Case 1: all indices are zero → invalid plane
 // ─────────────────────────────────────────────────────────────
            if (zeroCount == 3)
            {
                return new List<Vector3>();
            }

 // ─────────────────────────────────────────────────────────────
 // Case 2: exactly ONE non-zero index → plane is a CELL FACE
 // ─────────────────────────────────────────────────────────────
            if (zeroCount == 2)
            {
 // (h 0 0) → wall perpendicular to X at x = 1/h
                if (h != 0 && k == 0 && l == 0)
                {
                    float x = 1.0f / h;
                    return new List<Vector3>
                    {
                        new Vector3(x, 0, 0),
                        new Vector3(x, 1, 0),
                        new Vector3(x, 1, 1),
                        new Vector3(x, 0, 1),
                    };
                }

 // (0 k 0) → wall perpendicular to Y at y = 1/k
                if (k != 0 && h == 0 && l == 0)
                {
                    float y = 1.0f / k;
                    return new List<Vector3>
                    {
                        new Vector3(0, y, 0),
                        new Vector3(1, y, 0),
                        new Vector3(1, y, 1),
                        new Vector3(0, y, 1),
                    };
                }

 // (0 0 l) → wall perpendicular to Z at z = 1/l
                if (l != 0 && h == 0 && k == 0)
                {
                    float z = 1.0f / l;
                    return new List<Vector3>
                    {
                        new Vector3(0, 0, z),
                        new Vector3(1, 0, z),
                        new Vector3(1, 1, z),
                        new Vector3(0, 1, z),
                    };
                }
            }

 // ─────────────────────────────────────────────────────────────
 // Case 3: exactly TWO non-zero indices → plane is PARALLEL to one axis
 // ─────────────────────────────────────────────────────────────
            if (zeroCount == 1)
            {
 // (0 k l) → parallel to X axis, cuts Y at 1/k and Z at 1/l
                if (h == 0 && k != 0 && l != 0)
                {
                    float y0 = 1.0f / k;
                    float z0 = 1.0f / l;
                    return new List<Vector3>
                    {
                        new Vector3(0, y0, 0),
                        new Vector3(0, 0, z0),
                        new Vector3(1, 0, z0),
                        new Vector3(1, y0, 0),
                    };
                }

 // (h 0 l) → parallel to Y axis, cuts X at 1/h and Z at 1/l
                if (k == 0 && h != 0 && l != 0)
                {
                    float x0 = 1.0f / h;
                    float z0 = 1.0f / l;
                    return new List<Vector3>
                    {
                        new Vector3(x0, 0, 0),
                        new Vector3(0, 0, z0),
                        new Vector3(0, 1, z0),
                        new Vector3(x0, 1, 0),
                    };
                }

 // (h k 0) → parallel to Z axis, cuts X at 1/h and Y at 1/k
                if (l == 0 && h != 0 && k != 0)
                {
                    float x0 = 1.0f / h;
                    float y0 = 1.0f / k;
                    return new List<Vector3>
                    {
                        new Vector3(x0, 0, 0),
                        new Vector3(0, y0, 0),
                        new Vector3(0, y0, 1),
                        new Vector3(x0, 0, 1),
                    };
                }
            }

 // ─────────────────────────────────────────────────────────────
 // Case 4: general case (all indices non-zero) → plane cuts all 3 axes
 // Returns 3 intercept afterints.
 // ─────────────────────────────────────────────────────────────
            return new List<Vector3>
            {
                new Vector3(1.0f / h, 0, 0),
                new Vector3(0, 1.0f / k, 0),
                new Vector3(0, 0, 1.0f / l),
            };
        }

        #endregion

        #region Rectangle Builder

 /// <summary>
 /// Builds 4 corners of a rectangle lying in a plane defined by
 /// (center, normal). The rectangle has half-side length equal to `size`.
 /// 
 /// Algorithm:
 /// 1. Pick an arbitrary helper vector not parallel to the normal.
 /// 2. Compute two orthonormal basis vectors (u, v) inside the plane.
 /// 3. Return corners at center ± u·size ± v·size.
 /// </summary>
        public static Vector3[] BuildRectangle(Vector3 center, Vector3 normal, float size)
        {
            normal.Normalize();

 // Pick a helper vector that is not parallel to the normal
            Vector3 helper = Math.Abs(normal.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX;

 // Build an orthonormal basis (u, v) inside the plane
            Vector3 u = Vector3.Normalize(Vector3.Cross(normal, helper));
            Vector3 v = Vector3.Normalize(Vector3.Cross(normal, u));

 // 4 corners in counter-clockwise order (as seen from +normal)
            Vector3[] corners = new Vector3[4];
            corners[0] = center - u * size - v * size; // bottom-left
            corners[1] = center + u * size - v * size; // bottom-right
            corners[2] = center + u * size + v * size; // top-right
            corners[3] = center - u * size + v * size; // top-left

            return corners;
        }

        #endregion

        #region HKL Plane Calculator

 /// <summary>
 /// Computes the geometry of a crystal plane (hkl) in CARTESIAN coordinates.
 /// 
 /// Imafterrtant convention (matching Molecule.PrepareUnitCell):
 /// U = Matrix.Transafterse(fracToCart)
 /// p_cart = Vector3.Transform(p_frac, U)
 /// p_cart -= moleculeShift
 /// 
 /// The `moleculeShift` argument can be Vector3.Zero if the caller wants
 /// raw Cartesian coordinates (without centering). This is the case for
 /// PlanePropertiesWindow, which stores the plane WITHOUT shift so that
 /// Draw can apply the CURRENT shift at draw time.
 /// 
 /// After this method:
 /// - plane.Center = geometric center of the plane (Cartesian, -shift)
 /// - plane.Corners = 4 corners of the rectangle (Cartesian, -shift)
 /// - plane.A/B/C/D = plane equation coefficients
 /// </summary>
        public static void CalculateHKLPlane(
            CrystalPlane plane,
            Matrix fracToCart,
            Vector3 moleculeShift,
            float size = 10f)
        {
            var interceptsFrac = GetHKLIntercepts(plane.H, plane.K, plane.L);
            if (interceptsFrac.Count < 3)
            {
 // Invalid plane - nothing to do
                plane.Corners = new Vector3[4];
                plane.Center = Vector3.Zero;
                return;
            }

 // ⭐ Transafterse - same convention as Molecule.PrepareUnitCell
 // and Molecule.Calculate_SymmetryAtoms3D
            Matrix U = Matrix.Transpose(fracToCart);

 // Convert fractional intercepts to Cartesian and apply shift
            var interceptsCart = new List<Vector3>(interceptsFrac.Count);
            foreach (var p in interceptsFrac)
            {
                Vector3 cart = Vector3.Transform(p, U);
                cart -= moleculeShift;
                interceptsCart.Add(cart);
            }

 // Compute the plane normal from the geometric afterints
 // (cross product of two edge vectors)
            Vector3 v1 = interceptsCart[1] - interceptsCart[0];
            Vector3 v2 = interceptsCart[2] - interceptsCart[0];
            Vector3 normal = Vector3.Cross(v1, v2);

            if (normal.LengthSquared() < 1e-9f)
            {
 // Degenerate case - fall back to Z axis
                normal = Vector3.UnitZ;
            }
            normal.Normalize();

 // ⭐ If we already have 4 corners (face or edge case),
 // use them directly. Otherwise build a rectangle around the center.
            if (interceptsCart.Count == 4)
            {
                plane.Corners = interceptsCart.ToArray();
                plane.Center = (interceptsCart[0] + interceptsCart[1] +
                                interceptsCart[2] + interceptsCart[3]) / 4f;
            }
            else
            {
 // General case - 3 intercepts, build a nice rectangle
                Vector3 center = (interceptsCart[0] + interceptsCart[1] + interceptsCart[2]) / 3f;
                plane.Center = center;
                plane.Corners = BuildRectangle(center, normal, size);
            }

            plane.Size = size;

 // Store plane equation coefficients: Ax + By + Cz + D = 0
            plane.A = normal.X;
            plane.B = normal.Y;
            plane.C = normal.Z;
            plane.D = -Vector3.Dot(normal, plane.Center);
        }

        #endregion

        #region Plane Intersection

 /// <summary>
 /// Calculates linię przecięcia dwóch planes (precyzyjnie).
 /// 
 /// Punkt referencyjny = centre odcinka, which leży w OBU rectanglech.
 /// Length = dokładnie length tego odcinka.
 /// 
 /// Algorytm:
 /// 1. Kierunek linii = n1 × n2
 /// 2. Punkt startowy = Newton iterations
 /// 3. Find zakres t, w którym linia jest w prostokącie 1 (rzut 2D)
 /// 4. To samo for rectangle 2
 /// 5. Przecięcie zakresów → [t_min, t_max]
 /// 6. Wynik: punkt = point + t_mid * dir, length = (t_max - t_min) / 2
 /// </summary>
        public static PlaneIntersection ComputeIntersection(CrystalPlane p1, CrystalPlane p2)
        {
            var result = new PlaneIntersection();

            if (p1 == null || p2 == null) return result;

 // ═══════════════════════════════════════════════════════════════
 // 1. Kierunek linii = n1 × n2
 // ═══════════════════════════════════════════════════════════════

            Vector3 n1 = new Vector3((float)p1.A, (float)p1.B, (float)p1.C);
            Vector3 n2 = new Vector3((float)p2.A, (float)p2.B, (float)p2.C);

            float len1 = n1.Length();
            float len2 = n2.Length();

            if (len1 < 1e-9f || len2 < 1e-9f) return result;

            n1 /= len1;
            n2 /= len2;

            Vector3 dir = Vector3.Cross(n1, n2);
            float dirLen = dir.Length();

            if (dirLen < 1e-6f)
            {
 // Płaszczyzny równoległe
                return result;
            }

            dir /= dirLen;
 // dir *= -1; // Odwróć kierunek, to był zgodny z konwencją (zwykle w bottom)
            result.Direction = dir;

 // ═══════════════════════════════════════════════════════════════
 // 2. Punkt startowy (Newton iterations)
 // ═══════════════════════════════════════════════════════════════

            Vector3 guess = (p1.Center + p2.Center) * 0.5f;

 // Krok 1: translate na planesę 1
            float d1_shifted = SignedDistanceToPlane(guess, p1);
            guess -= n1 * d1_shifted;

 // Krok 2: translate na planesę 2
            Vector3 n2_projected = n2 - Vector3.Dot(n2, n1) * n1;
            float n2_proj_len = n2_projected.Length();

            if (n2_proj_len < 1e-9f) return result;

            n2_projected /= n2_proj_len;

            float d2_shifted = SignedDistanceToPlane(guess, p2);
            float n2_dot = Vector3.Dot(n2, n2_projected);

            if (Math.Abs(n2_dot) < 1e-9f) return result;

            guess -= n2_projected * (d2_shifted / n2_dot);

 // Krok 3: still raz na planesę 1 (for dokładności)
            float d1_final = SignedDistanceToPlane(guess, p1);
            guess -= n1 * d1_final;

            Vector3 pointOnLine = guess;

 // ═══════════════════════════════════════════════════════════════
 // 3. Precyzyjne wyznaczenie zakresu t (przecięcie prostoangleów)
 // ═══════════════════════════════════════════════════════════════

            float tMin, tMax;

            if (!TryComputeRectangleRange(p1, pointOnLine, dir, out tMin, out tMax))
            {
 // Fallback: planesa 1 nie ma prawidłowych corners
                result.Point = pointOnLine;
                result.HalfLength = ComputeIntersectionHalfLength(p1, p2);
                return result;
            }

            float tMin2, tMax2;
            if (!TryComputeRectangleRange(p2, pointOnLine, dir, out tMin2, out tMax2))
            {
                result.Point = pointOnLine;
                result.HalfLength = ComputeIntersectionHalfLength(p1, p2);
                return result;
            }

 // Przecięcie zakresów
            float tStart = Math.Max(tMin, tMin2);
            float tEnd = Math.Min(tMax, tMax2);

            if (tEnd < tStart)
            {
 // Linia exists ale nie przechodzi przez OBA prostoangley razem
 // Fallback: użyj afterbeforeniej metody (bounding box)
                result.Point = pointOnLine;
                result.HalfLength = ComputeIntersectionHalfLength(p1, p2);
                return result;
            }

 // ═══════════════════════════════════════════════════════════════
 // 4. Wyznacz punkt środkowy i length
 // ═══════════════════════════════════════════════════════════════

            float tMid = (tStart + tEnd) * 0.5f;
            float halfLen = (tEnd - tStart) * 0.5f;

            result.Point = pointOnLine + dir * tMid;
            result.HalfLength = Math.Max(halfLen, 0.1f);   // min 0.1 Å

            return result;
        }

 /// <summary>
 /// Calculates zakres parameteru t, w którym linia (afterintOnLine + t·dir)
 /// leży wewnątrz rectangle planes.
 /// 
 /// Metoda: rzut linii na lokalny system 2D rectangle (u, v),
 /// aftertem klasyczne przecięcie prostej z AABB (axis-aligned bounding box).
 /// 
 /// Zakres u, v jest wyliczany z min/max after WSZYSTKICH 4 corners —
 /// dzięki temu works for each order corners (prostoangle,
 /// dowolny czworoangle, hkl face, hkl edge).
 /// </summary>
        private static bool TryComputeRectangleRange(
            CrystalPlane plane,
            Vector3 pointOnLine,
            Vector3 dir,
            out float tMin,
            out float tMax)
        {
            tMin = float.NegativeInfinity;
            tMax = float.PositiveInfinity;

            if (plane.Corners == null || plane.Corners.Length < 4)
                return false;

            Vector3 c0 = plane.Corners[0];
            Vector3 c1 = plane.Corners[1];
            Vector3 c3 = plane.Corners[3];

            Vector3 edgeU = c1 - c0;
            Vector3 edgeV = c3 - c0;

            float lenU = edgeU.Length();
            float lenV = edgeV.Length();

            if (lenU < 1e-6f || lenV < 1e-6f)
                return false;

            Vector3 u = edgeU / lenU;
            Vector3 v = edgeV / lenV;

            Vector3 center = plane.Center;

 // ⭐ Afterlicz zakres u, v after all 4 corners
 // (for withoutpieczeństwa — niezależnie od order)
            float minU = float.MaxValue, maxU = float.MinValue;
            float minV = float.MaxValue, maxV = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                Vector3 p = plane.Corners[i] - center;
                float pu = Vector3.Dot(p, u);
                float pv = Vector3.Dot(p, v);
                if (pu < minU) minU = pu;
                if (pu > maxU) maxU = pu;
                if (pv < minV) minV = pv;
                if (pv > maxV) maxV = pv;
            }

 // Rzut linii na lokalny system (u, v)
            Vector3 startVec = pointOnLine - center;

            float start_u = Vector3.Dot(startVec, u);
            float start_v = Vector3.Dot(startVec, v);

            float dir_u = Vector3.Dot(dir, u);
            float dir_v = Vector3.Dot(dir, v);

 // --- Oś U ---
            if (Math.Abs(dir_u) < 1e-9f)
            {
 // Linia równoległa do osi V — u must be w zakresie
                if (start_u < minU || start_u > maxU)
                    return false;
 // u nie ogranicza t
            }
            else
            {
                float t1 = (minU - start_u) / dir_u;
                float t2 = (maxU - start_u) / dir_u;
                tMin = Math.Max(tMin, Math.Min(t1, t2));
                tMax = Math.Min(tMax, Math.Max(t1, t2));
            }

 // --- Oś V ---
            if (Math.Abs(dir_v) < 1e-9f)
            {
                if (start_v < minV || start_v > maxV)
                    return false;
            }
            else
            {
                float t1 = (minV - start_v) / dir_v;
                float t2 = (maxV - start_v) / dir_v;
                tMin = Math.Max(tMin, Math.Min(t1, t2));
                tMax = Math.Min(tMax, Math.Max(t1, t2));
            }

            return tMax >= tMin;
        }

 /// <summary>
 /// Odległość punktu od planes (ze znakiem).
 /// Dodatnia = punkt after stronie normalnej, ujemna = after przeciwnej.
 /// </summary>
        private static float SignedDistanceToPlane(Vector3 point, CrystalPlane plane)
        {
            Vector3 n = new Vector3((float)plane.A, (float)plane.B, (float)plane.C);
            float len = n.Length();
            if (len < 1e-9f) return 0f;
            n /= len;

            float d = (float)plane.D / len;

            return Vector3.Dot(n, point) + d;
        }

 /// <summary>
 /// Fallback: length pół-odcinka na afterdstawie bounding-boxów planes.
 /// Used when precyzyjne obliczenie nie jest możliwe
 /// (np. no corners, or prostoangley nie przecinają wzdłuż linii).
 /// </summary>
        private static float ComputeIntersectionHalfLength(CrystalPlane p1, CrystalPlane p2)
        {
            if (p1.Corners == null || p1.Corners.Length < 4) return 15f;
            if (p2.Corners == null || p2.Corners.Length < 4) return 15f;

            Vector3 min1 = Vector3.Min(Vector3.Min(p1.Corners[0], p1.Corners[1]),
                                        Vector3.Min(p1.Corners[2], p1.Corners[3]));
            Vector3 max1 = Vector3.Max(Vector3.Max(p1.Corners[0], p1.Corners[1]),
                                        Vector3.Max(p1.Corners[2], p1.Corners[3]));
            Vector3 min2 = Vector3.Min(Vector3.Min(p2.Corners[0], p2.Corners[1]),
                                        Vector3.Min(p2.Corners[2], p2.Corners[3]));
            Vector3 max2 = Vector3.Max(Vector3.Max(p2.Corners[0], p2.Corners[1]),
                                        Vector3.Max(p2.Corners[2], p2.Corners[3]));

            Vector3 bbMin = Vector3.Min(min1, min2);
            Vector3 bbMax = Vector3.Max(max1, max2);

            float diagonal = (bbMax - bbMin).Length();
            return diagonal * 1.0f;
        }

        #endregion
    }
}