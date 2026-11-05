// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal morphology / Wulff module: ConvexAfterlyhedronClipper.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// Wersja: accelerated + correct RemoveDuplicates (27 buckets)

using Microsoft.Xna.Framework;
using Tal.Utils;

namespace Tal
{
    public class ConvexPolyhedronFrac
    {
        public struct Tri
        {
            public Vector3 A, B, C;
            public string FaceName;
        }

        public List<Tri> Triangles = new List<Tri>();

        #region Debug Helpers
        private static void LogError(string msg) => ProgressReporter.Log(msg, System.Drawing.Color.Red);
        #endregion

        #region Factory

        public static ConvexPolyhedronFrac FromBox(float r)
        {
            var poly = new ConvexPolyhedronFrac();

            Vector3[] v = new Vector3[8];
            v[0] = new Vector3(-r, -r, -r);
            v[1] = new Vector3(+r, -r, -r);
            v[2] = new Vector3(+r, +r, -r);
            v[3] = new Vector3(-r, +r, -r);
            v[4] = new Vector3(-r, -r, +r);
            v[5] = new Vector3(+r, -r, +r);
            v[6] = new Vector3(+r, +r, +r);
            v[7] = new Vector3(-r, +r, +r);

            AddQuad(poly, v[0], v[1], v[2], v[3], "cell-z-");
            AddQuad(poly, v[4], v[5], v[6], v[7], "cell-z+");
            AddQuad(poly, v[0], v[1], v[5], v[4], "cell-y-");
            AddQuad(poly, v[2], v[3], v[7], v[6], "cell-y+");
            AddQuad(poly, v[0], v[3], v[7], v[4], "cell-x-");
            AddQuad(poly, v[1], v[2], v[6], v[5], "cell-x+");

            return poly;
        }

        private static void AddQuad(ConvexPolyhedronFrac poly,
                                     Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                                     string name)
        {
            poly.Triangles.Add(new Tri { A = a, B = b, C = c, FaceName = name });
            poly.Triangles.Add(new Tri { A = a, B = c, C = d, FaceName = name });
        }

        #endregion

        #region Clipping

        public void ClipByPlane(int h, int k, int l, float d, string faceName)
        {
            if (Triangles.Count == 0) return;

            float nLenSq = h * h + k * k + l * l;
            if (nLenSq < 1e-9f) return;

            float F(Vector3 p) => h * p.X + k * p.Y + l * p.Z - d;
            const float EPS = 1e-5f;

            int beforeCount = Triangles.Count;

            var result = new List<Tri>(Triangles.Count + 8);
            var capCandidates = new List<Vector3>(12);

            foreach (var tri in Triangles)
            {
                float fa = F(tri.A);
                float fb = F(tri.B);
                float fc = F(tri.C);

                if (Math.Abs(fa) < EPS && Math.Abs(fb) < EPS && Math.Abs(fc) < EPS)
                    continue;

                if (fa <= EPS && fb <= EPS && fc <= EPS)
                {
                    result.Add(tri);
                    if (Math.Abs(fa) < EPS) capCandidates.Add(tri.A);
                    if (Math.Abs(fb) < EPS) capCandidates.Add(tri.B);
                    if (Math.Abs(fc) < EPS) capCandidates.Add(tri.C);
                    continue;
                }

                if (fa >= -EPS && fb >= -EPS && fc >= -EPS)
                    continue;

                var clippedPoly = ClipTriangleAgainstPlane(tri.A, tri.B, tri.C, fa, fb, fc, EPS);
                if (clippedPoly.Count < 3) continue;

                for (int i = 1; i < clippedPoly.Count - 1; i++)
                {
                    Vector3 a = clippedPoly[0];
                    Vector3 b = clippedPoly[i];
                    Vector3 c = clippedPoly[i + 1];

                    if (Vector3.Cross(b - a, c - a).LengthSquared() < 1e-12f)
                        continue;

                    result.Add(new Tri { A = a, B = b, C = c, FaceName = tri.FaceName });
                }

                foreach (var p in clippedPoly)
                    if (Math.Abs(F(p)) < EPS)
                        capCandidates.Add(p);
            }

            var uniqueCap = RemoveDuplicates(capCandidates, EPS);

            if (uniqueCap.Count >= 3)
            {
                Vector3 center = Vector3.Zero;
                foreach (var p in uniqueCap) center += p;
                center /= uniqueCap.Count;

                Vector3 n = new Vector3(h, k, l);
                Vector3 nNorm = Vector3.Normalize(n);

                Vector3 up = Math.Abs(Vector3.Dot(nNorm, Vector3.UnitY)) > 0.9f
                    ? Vector3.UnitX : Vector3.UnitY;
                Vector3 u = Vector3.Normalize(Vector3.Cross(nNorm, up));
                Vector3 w = Vector3.Normalize(Vector3.Cross(nNorm, u));

                uniqueCap.Sort((p1, p2) =>
                {
                    Vector3 d1 = p1 - center;
                    Vector3 d2 = p2 - center;
                    float a1 = (float)Math.Atan2(Vector3.Dot(d1, w), Vector3.Dot(d1, u));
                    float a2 = (float)Math.Atan2(Vector3.Dot(d2, w), Vector3.Dot(d2, u));
                    return a1.CompareTo(a2);
                });

                for (int i = 1; i < uniqueCap.Count - 1; i++)
                {
                    Vector3 a = uniqueCap[0];
                    Vector3 b = uniqueCap[i];
                    Vector3 c = uniqueCap[i + 1];

                    Vector3 cross = Vector3.Cross(b - a, c - a);

                    if (Vector3.Dot(cross, n) < 0)
                    {
                        (b, c) = (c, b);
                        cross = -cross;
                    }

                    if (cross.LengthSquared() < 1e-12f) continue;

                    result.Add(new Tri { A = a, B = b, C = c, FaceName = faceName });
                }
            }

            Triangles = result;

            if (Triangles.Count == 0)
                LogError($"[Clip] ❌ DESTROYED by {faceName}! (before={beforeCount})");
        }

        private static List<Vector3> ClipTriangleAgainstPlane(
            Vector3 a, Vector3 b, Vector3 c,
            float fa, float fb, float fc, float eps)
        {
            var output = new List<Vector3>(4);

            ProcessEdge((a, fa), (b, fb), output, eps);
            ProcessEdge((b, fb), (c, fc), output, eps);
            ProcessEdge((c, fc), (a, fa), output, eps);

            return RemoveDuplicates(output, eps);
        }

        private static void ProcessEdge((Vector3 p, float f) prev,
                                        (Vector3 p, float f) curr,
                                        List<Vector3> output, float eps)
        {
            bool prevIn = prev.f <= eps;
            bool currIn = curr.f <= eps;

            if (currIn)
            {
                if (!prevIn)
                {
                    float t = prev.f / (prev.f - curr.f);
                    output.Add(Vector3.Lerp(prev.p, curr.p, t));
                }
                output.Add(curr.p);
            }
            else if (prevIn)
            {
                float t = prev.f / (prev.f - curr.f);
                output.Add(Vector3.Lerp(prev.p, curr.p, t));
            }
        }

        #endregion

        #region Helpers

 /// <summary>
 /// ⭐ POPRAWIONE: duplicate removal with verification against 27 neighbouring buckets.
 /// Zaafterbiega gubieniu triangles przez kolizje hasha.
 /// </summary>
        private static List<Vector3> RemoveDuplicates(List<Vector3> points, float eps)
        {
            if (points.Count <= 1) return points;

            var result = new List<Vector3>(points.Count);
            var grid = new Dictionary<long, List<int>>(points.Count);
            float epsSq = eps * eps;
            float invEps = 1f / eps;

            long Hash(int ix, int iy, int iz)
                => ((long)ix * 73856093L) ^ ((long)iy * 19349663L) ^ ((long)iz * 83492791L);

            foreach (var p in points)
            {
                int ix = (int)MathF.Round(p.X * invEps);
                int iy = (int)MathF.Round(p.Y * invEps);
                int iz = (int)MathF.Round(p.Z * invEps);

                bool dup = false;

 // ⭐ Check 27 neighbouring buckets
                for (int dx = -1; dx <= 1 && !dup; dx++)
                    for (int dy = -1; dy <= 1 && !dup; dy++)
                        for (int dz = -1; dz <= 1 && !dup; dz++)
                        {
                            long h = Hash(ix + dx, iy + dy, iz + dz);
                            if (!grid.TryGetValue(h, out var list)) continue;

                            for (int k = 0; k < list.Count; k++)
                            {
                                if (Vector3.DistanceSquared(p, result[list[k]]) < epsSq)
                                {
                                    dup = true;
                                    break;
                                }
                            }
                        }

                if (!dup)
                {
                    int newIdx = result.Count;
                    result.Add(p);
                    long h = Hash(ix, iy, iz);
                    if (!grid.TryGetValue(h, out var list))
                    {
                        list = new List<int>(2);
                        grid[h] = list;
                    }
                    list.Add(newIdx);
                }
            }

            return result;
        }

        #endregion
    }
}