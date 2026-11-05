// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldMeshRelaxation.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// ⭐ Mesh relaxation — translate mesh vertices along the tangent
 /// and normal, so as to "straighten" MC steps without losing shape.
 ///
 /// IDEA:
 /// MC generates triangles on voxel edges, which creates steps.
 /// Each vertex has a normal (already computed). Where
 /// the normal is "stepped" (two triangles have different normals),
 /// shift the vertex by a small value in the direction of:
 /// - tangent (perpendicular to normal) — smooths steps
 /// - normal (small boost) — reduces jagging
 ///
 /// Effect: straightens jagging WITHOUT losing shape — the surface remains
 /// sharp where the physics is sharp (contacts), but steps disappear.
 ///
 /// Robi PO Taubin, PRZED color smoothingiem.
 /// </summary>
    public static class HirshfeldMeshRelaxation
    {
 /// <summary>
 /// Aplikuje mesh relaxation do surface Hirshfelda.
 ///
 /// iterations: 0-5 (zwykle 2-3 wystarcza)
 /// strength: 0.0-1.0 (how strongly to shift, 0.4 = default)
 /// </summary>
        public static void Apply(HirshfeldSurface surface, int iterations, float strength)
        {
            if (surface == null || !surface.IsValid) return;
            if (iterations <= 0 || strength <= 0.01f) return;

            int n = surface.VertexCount;
            if (n < 3) return;

 // ═══ Cache neighbourhood + normalne per-vertex ═══
            var neighbors = BuildNeighbors(surface);
            var normals = ComputeVertexNormals(surface);

 // ═══ Copy of current positions (start from original) ═══
            var currentPos = (Vector3[])surface.Vertices.ToArray();

 // ═══ Iteracje relaksacji ═══
            for (int iter = 0; iter < iterations; iter++)
            {
                var newPos = new Vector3[n];

                for (int i = 0; i < n; i++)
                {
                    var nb = neighbors[i];

 // ⭐ Vertex without neighbours — leave unchanged
                    if (nb == null || nb.Length < 2)
                    {
                        newPos[i] = currentPos[i];
                        continue;
                    }

 // ═══ 1. Mean neighbour position (Laplacian) ═══
                    Vector3 avg = Vector3.Zero;
                    for (int k = 0; k < nb.Length; k++)
                        avg += currentPos[nb[k]];
                    avg /= nb.Length;

 // ═══ 2. Vector to the mean ═══
                    Vector3 diff = avg - currentPos[i];

 // ═══ 3. Current vertex normal ═══
                    Vector3 N = normals[i];

 // ═══ 4. Styczny component — prostuje schodki ═══
 // Project diff onto tangent plane (remove normal component)
                    Vector3 tangential = diff - Vector3.Dot(diff, N) * N;

 // ═══ 5. Normal component — smooths "jagging" ═══
 // Neighbour normal difference — at step locations
 // normals differ; in smooth regions they are similar.
                    Vector3 normalDiff = Vector3.Zero;
                    for (int k = 0; k < nb.Length; k++)
                        normalDiff += normals[nb[k]] - N;
                    normalDiff /= nb.Length;

 // ═══ 6. Finalne shift ═══
 // Tangent (flattens steps) + normal * 0.1 (reduces jagging)
                    Vector3 displacement = tangential + normalDiff * 0.1f;

 // ═══ 7. Clamp length — nie przesuwaj za daleko ═══
 // maxLen = 5% of mesh unit, so as not to "jump"
 // onto a neighbouring face or tear the mesh.
                    float len = displacement.Length();
                    const float maxLen = 0.05f;
                    if (len > maxLen)
                        displacement = displacement / len * maxLen;

                    newPos[i] = currentPos[i] + displacement * strength;
                }

 // ⭐ Swap — new positions become current
                currentPos = newPos;
            }

 // ═══ Save do surface ═══
            for (int i = 0; i < n; i++)
                surface.Vertices[i] = currentPos[i];

 // ═══ Przelicz normalne after relaksacji ═══
            surface.Normals = new List<Vector3>(ComputeVertexNormals(surface));
        }

 /// <summary>
 /// Builds list neighbours per-vertex (indexy neighbours).
 /// Triangle edges — without duplicates.
 /// </summary>
        private static int[][] BuildNeighbors(HirshfeldSurface surface)
        {
            int n = surface.VertexCount;
            if (n == 0) return Array.Empty<int[]>();

            var sets = new HashSet<int>[n];
            for (int i = 0; i < n; i++)
                sets[i] = new HashSet<int>();

            var inds = surface.Indices;
            for (int i = 0; i < inds.Count; i += 3)
            {
                int a = inds[i];
                int b = inds[i + 1];
                int c = inds[i + 2];

                if (a >= n || b >= n || c >= n) continue;

                sets[a].Add(b); sets[a].Add(c);
                sets[b].Add(a); sets[b].Add(c);
                sets[c].Add(a); sets[c].Add(b);
            }

            var result = new int[n][];
            for (int i = 0; i < n; i++)
            {
                result[i] = new int[sets[i].Count];
                sets[i].CopyTo(result[i]);
            }

            return result;
        }

 /// <summary>
 /// Liczy normalne per-vertex (area-weighted triangles).
 /// Without inversion — used for internal calculations.
 /// </summary>
        private static Vector3[] ComputeVertexNormals(HirshfeldSurface surface)
        {
            int n = surface.VertexCount;
            var normals = new Vector3[n];
            var verts = surface.Vertices;
            var inds = surface.Indices;

            for (int i = 0; i < inds.Count; i += 3)
            {
                int ia = inds[i];
                int ib = inds[i + 1];
                int ic = inds[i + 2];

                if (ia >= n || ib >= n || ic >= n) continue;

                Vector3 a = verts[ia];
                Vector3 b = verts[ib];
                Vector3 c = verts[ic];

 // Normalna z iloczynu wektorowego — length = 2 × area triangle
                Vector3 nrm = Vector3.Cross(b - a, c - a);
                float lenSq = nrm.LengthSquared();
                if (lenSq < 1e-9f) continue;

                normals[ia] += nrm;
                normals[ib] += nrm;
                normals[ic] += nrm;
            }

            for (int i = 0; i < n; i++)
            {
                float len = normals[i].Length();
                normals[i] = len > 1e-9f
                    ? normals[i] / len
                    : Vector3.UnitY;
            }

            return normals;
        }
    }
}