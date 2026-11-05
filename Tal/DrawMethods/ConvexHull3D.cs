// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: ConvexHull3D.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;

namespace Tal.DrawMethods
{
 /// <summary>
 /// Iteracyjny algorytm QuickHull 3D.
 /// Returns a list of triangles (indices into the input list) forming the surface
 /// convex hull. Triangles are oriented CCW when viewed from outside.
 /// </summary>
    public static class ConvexHull3D
    {
        private struct Face
        {
            public int A, B, C;       // indeksy wierzchołków
            public Vector3 Normal;    // normalna (outward)
            public float D;           // plane: n·x + D = 0
            public bool Alive;

            public void ComputePlane(List<Vector3> points)
            {
                Vector3 pa = points[A];
                Vector3 pb = points[B];
                Vector3 pc = points[C];

                Normal = Vector3.Cross(pb - pa, pc - pa);
                float len = Normal.Length();
                if (len > 1e-9f)
                {
                    Normal /= len;
                    D = -Vector3.Dot(Normal, pa);
                }
                else
                {
                    Normal = Vector3.UnitY;
                    D = 0;
                }
            }

            public float Distance(Vector3 p) => Vector3.Dot(Normal, p) + D;
        }

 /// <summary>
 /// Returns convex hull triangles or null when fewer than 4 points / all coplanar.
 /// </summary>
        public static List<(int, int, int)> Compute(List<Vector3> points)
        {
            int n = points.Count;
            if (n < 4) return null;

 // 1. Find skrajne punkty (min/max X, Y, Z) — 6 points
            int iMinX = 0, iMaxX = 0, iMinY = 0, iMaxY = 0, iMinZ = 0, iMaxZ = 0;
            for (int i = 1; i < n; i++)
            {
                if (points[i].X < points[iMinX].X) iMinX = i;
                if (points[i].X > points[iMaxX].X) iMaxX = i;
                if (points[i].Y < points[iMinY].Y) iMinY = i;
                if (points[i].Y > points[iMaxY].Y) iMaxY = i;
                if (points[i].Z < points[iMinZ].Z) iMinZ = i;
                if (points[i].Z > points[iMaxZ].Z) iMaxZ = i;
            }

 // 2. Find the pair of most distant points
            int[] candidates = { iMinX, iMaxX, iMinY, iMaxY, iMinZ, iMaxZ };
            int p0 = 0, p1 = 1;
            float bestDist = 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                for (int j = i + 1; j < candidates.Length; j++)
                {
                    float d = Vector3.DistanceSquared(
                        points[candidates[i]], points[candidates[j]]);
                    if (d > bestDist)
                    {
                        bestDist = d;
                        p0 = candidates[i];
                        p1 = candidates[j];
                    }
                }
            }

 // 3. Find the third point — farthest from line p0-p1
            int p2 = -1;
            float bestArea = 0;
            Vector3 lineDir = points[p1] - points[p0];
            for (int i = 0; i < n; i++)
            {
                if (i == p0 || i == p1) continue;
                Vector3 v = points[i] - points[p0];
                float area = Vector3.Cross(v, lineDir).Length();
                if (area > bestArea)
                {
                    bestArea = area;
                    p2 = i;
                }
            }

            if (p2 < 0 || bestArea < 1e-6f)
                return null;

 // 4. Find the fourth point — farthest from plane p0-p1-p2
            Vector3 planeN = Vector3.Normalize(Vector3.Cross(
                points[p1] - points[p0],
                points[p2] - points[p0]));

            int p3 = -1;
            float bestDist4 = 0;
            for (int i = 0; i < n; i++)
            {
                if (i == p0 || i == p1 || i == p2) continue;
                float d = Math.Abs(Vector3.Dot(planeN, points[i] - points[p0]));
                if (d > bestDist4)
                {
                    bestDist4 = d;
                    p3 = i;
                }
            }

            if (p3 < 0 || bestDist4 < 1e-6f)
                return null;   // wszystkie punkty współpłaszczyznowe

 // 5. Build the starting tetrahedron (4 faces)
            var faces = new List<Face>(64);

 // Check orientation and build 4 faces so normals are outward
            AddTetrahedron(faces, points, p0, p1, p2, p3);

 // 6. Iteracyjnie dodawaj punkty
            var assigned = new bool[n];
            assigned[p0] = assigned[p1] = assigned[p2] = assigned[p3] = true;

            var outsidePoints = new List<int>();
            for (int i = 0; i < n; i++)
                if (!assigned[i]) outsidePoints.Add(i);

            while (outsidePoints.Count > 0)
            {
 // Find punkt afterza hull
                int bestPt = -1;
                int bestFace = -1;
                float maxDist = 1e-6f;

                foreach (int pi in outsidePoints)
                {
                    for (int fi = 0; fi < faces.Count; fi++)
                    {
                        if (!faces[fi].Alive) continue;
                        float d = faces[fi].Distance(points[pi]);
                        if (d > maxDist)
                        {
                            maxDist = d;
                            bestPt = pi;
                            bestFace = fi;
                        }
                    }
                }

                if (bestPt < 0) break;   // wszystkie punkty wewnątrz

 // Zbierz all faces visible z bestPt
                var visibleFaces = new List<int>();
                for (int fi = 0; fi < faces.Count; fi++)
                {
                    if (!faces[fi].Alive) continue;
                    if (faces[fi].Distance(points[bestPt]) > 1e-6f)
                        visibleFaces.Add(fi);
                }

 // Find the "horizon" — edges between visible and invisible faces
                var horizon = new List<(int, int)>();
                foreach (int fi in visibleFaces)
                {
                    var f = faces[fi];
                    int[,] edges = { { f.A, f.B }, { f.B, f.C }, { f.C, f.A } };
                    for (int e = 0; e < 3; e++)
                    {
                        int e0 = edges[e, 0];
                        int e1 = edges[e, 1];

 // Check whether the neighbouring face is visible
                        bool neighborVisible = false;
                        foreach (int gi in visibleFaces)
                        {
                            if (gi == fi) continue;
                            var g = faces[gi];
 // Does g have edge e1->e0?
                            if (g.A == e1 && g.B == e0 ||
                                g.B == e1 && g.C == e0 ||
                                g.C == e1 && g.A == e0)
                            {
                                neighborVisible = true;
                                break;
                            }
                        }

                        if (!neighborVisible)
                        {
                            horizon.Add((e0, e1));
                        }
                    }
                }

 // Remove visible faces
                foreach (int fi in visibleFaces)
                {
                    var f = faces[fi];
                    f.Alive = false;
                    faces[fi] = f;
                }

 // Add new faces z bestPt do horyzontu
                foreach (var (e0, e1) in horizon)
                {
                    var newFace = new Face { A = e0, B = e1, C = bestPt, Alive = true };
                    newFace.ComputePlane(points);
                    faces.Add(newFace);
                }

 // Remove bestPt z list
                outsidePoints.Remove(bestPt);
            }

 // 7. Return live faces as triangles
            var result = new List<(int, int, int)>();
            foreach (var f in faces)
            {
                if (f.Alive)
                    result.Add((f.A, f.B, f.C));
            }

            return result;
        }

 /// <summary>
 /// Builds 4 tetrahedron faces z afterprawnymi normalnymi (outward).
 /// </summary>
        private static void AddTetrahedron(List<Face> faces, List<Vector3> pts,
                                            int i0, int i1, int i2, int i3)
        {
 // Check znak orientacji i0,i1,i2,i3
            Vector3 n = Vector3.Cross(pts[i1] - pts[i0], pts[i2] - pts[i0]);
            float side = Vector3.Dot(n, pts[i3] - pts[i0]);

 // If i3 is "above" the plane — reverse order so faces are outward
            if (side > 0)
            {
 // i3 nad → replace i1 z i2
                (i1, i2) = (i2, i1);
            }

 // 4 tetrahedron faces
            AddFace(faces, pts, i0, i1, i2);   // ABC
            AddFace(faces, pts, i0, i3, i1);   // A D B
            AddFace(faces, pts, i1, i3, i2);   // B D C
            AddFace(faces, pts, i2, i3, i0);   // C D A
        }

        private static void AddFace(List<Face> faces, List<Vector3> pts, int a, int b, int c)
        {
            var f = new Face { A = a, B = b, C = c, Alive = true };
            f.ComputePlane(pts);
            faces.Add(f);
        }
    }
}