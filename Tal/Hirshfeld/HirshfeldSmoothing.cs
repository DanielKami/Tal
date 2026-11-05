// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldSmoothing.

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
 /// Hirshfeld surface mesh smoothing.
 ///
 /// ⭐ Uses the **Taubin** algorithm (λ|μ) instead of plain Laplacian:
 /// - λ > 0 (shrinking step)
 /// - μ < 0 (inflating step, |μ| > λ)
 /// Result: smooths but does NOT shrink the surface (unlike Laplacian).
 ///
 /// ⭐ Cache oryginalnych positions vertices — smoothing always startuje
 /// z tego samego punktu, so suwak jest deterministyczny:
 /// change 5 → 0 wraca do ORYGINALNEJ surface z MC.
 ///
 /// ⭐ Cache neighbourhood — toafterlogsa nie zmienia, so liczone raz.
 ///
 /// ⭐ Normals recomputed after every smoothing (needed for lighting).
 /// </summary>
    public static class HirshfeldSmoothing
    {
        #region Constants

 // ═══ Taubin parameters ═══
 // λ i μ are chosen so that λ + μ < 0 (no-shrinkage condition).
 // Standardowe values z literatury (Taubin 1995):
        private const float LAMBDA_BASE = 0.330f;
        private const float MU = -0.331f;   // |μ| > λ

        #endregion

        #region Cache

 /// <summary>
 /// Cache oryginalnych positions vertices — key = hashcode surface.
 /// Enables deterministic changes of the smoothing level.
 /// </summary>
        private static readonly Dictionary<int, Vector3[]> _originalPositionsCache
            = new Dictionary<int, Vector3[]>();

 /// <summary>
 /// Cache neighbourhood per-vertex — toafterlogsa nie zmienia .
 /// </summary>
        private static readonly Dictionary<int, int[][]> _neighborCache
            = new Dictionary<int, int[][]>();

        private static readonly object _cacheLock = new object();

        #endregion

        #region Public API

 /// <summary>
 /// Applies Taubin smoothing at the given strength.
 /// Level 0 = none (return to original), 5 = maximum.
 /// </summary>
        public static void Apply(HirshfeldSurface surface, int level)
        {
            if (surface == null || !surface.IsValid) return;
            if (level < 0) level = 0;
            if (level > 25) level = 25;

            int key = System.Runtime.CompilerServices.RuntimeHelpers
                .GetHashCode(surface);

            Vector3[] original;
            int[][] neighbors;

            lock (_cacheLock)
            {
 // ⭐ Originals cache (once per surface)
                if (!_originalPositionsCache.TryGetValue(key, out original)
                    || original.Length != surface.VertexCount)
                {
                    original = surface.Vertices.ToArray();
                    _originalPositionsCache[key] = original;
                }

 // ⭐ Cache neighbourhood (raz per surface)
                if (!_neighborCache.TryGetValue(key, out neighbors)
                    || neighbors.Length != surface.VertexCount)
                {
                    neighbors = BuildNeighbors(surface);
                    _neighborCache[key] = neighbors;
                }
            }

 // ⭐ Zawsze startuj z original — deterministyczne
            for (int i = 0; i < surface.VertexCount; i++)
                surface.Vertices[i] = original[i];

            if (level == 0)
            {
 // Level 0 — only recompute normals and exit
                RecomputeNormals(surface);
                return;
            }

 // ⭐ Number of Taubin iterations depending on level
 // (1 iteracja = krok λ + krok μ)
            int iterations = level switch
            {
                1 => 1,
                2 => 1,
                3 => 2,
                4 => 2,
                5 => 3,
                _ => 1
            };

 // ⭐ λ strength — smaller for lower levels
            float lambdaScale = level switch
            {
                1 => 0.50f,
                2 => 0.70f,
                3 => 0.85f,
                4 => 1.00f,
                5 => 1.00f,
                _ => 1.00f
            };

            float lambda = LAMBDA_BASE * lambdaScale;

            for (int iter = 0; iter < iterations; iter++)
            {
 // Step λ (smoothing)
                SmoothPass(surface, neighbors, lambda);

 // Step μ (inflating — compensates shrinkage)
                SmoothPass(surface, neighbors, MU);
            }

 // ⭐ Recompute normals after smoothing
            RecomputeNormals(surface);
        }

 /// <summary>
 /// Clears cache for danej surface — call when surface jest usuwana.
 /// </summary>
        public static void ClearCache(HirshfeldSurface surface)
        {
            if (surface == null) return;

            int key = System.Runtime.CompilerServices.RuntimeHelpers
                .GetHashCode(surface);

            lock (_cacheLock)
            {
                _originalPositionsCache.Remove(key);
                _neighborCache.Remove(key);
            }
        }

 /// <summary>
 /// Clears the entire cache (e.g. when closing the document).
 /// </summary>
        public static void ClearAllCache()
        {
            lock (_cacheLock)
            {
                _originalPositionsCache.Clear();
                _neighborCache.Clear();
            }
        }

        #endregion

        #region Private — Neighborhood

 /// <summary>
 /// Builds neighbour list for each vertex (via triangle edges).
 /// Returns jagged array: neighbors[i] = list of neighbour indices of vertex i.
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

        #endregion

        #region Private — Smoothing

 /// <summary>
 /// Single smoothing pass: for each vertex
 /// shift it towards the mean neighbour position by factor.
 /// </summary>
        private static void SmoothPass(HirshfeldSurface surface, int[][] neighbors, float factor)
        {
            int n = surface.VertexCount;
            if (n == 0) return;

            var verts = surface.Vertices;
            var newPos = new Vector3[n];

            for (int i = 0; i < n; i++)
            {
                var nb = neighbors[i];

                if (nb.Length == 0)
                {
                    newPos[i] = verts[i];
                    continue;
                }

 // ⭐ Mean neighbour position
                Vector3 sum = Vector3.Zero;
                for (int k = 0; k < nb.Length; k++)
                    sum += verts[nb[k]];

                Vector3 avg = sum / nb.Length;

 // ⭐ Shift towards the mean
 // factor > 0 → smoothing (shrinking)
 // factor < 0 → rozszerzanie (kompensuje kurczenie)
                newPos[i] = Vector3.Lerp(verts[i], avg, factor);
            }

 // ⭐ Scopy z afterwrotem (in-place)
            for (int i = 0; i < n; i++)
                verts[i] = newPos[i];
        }

        #endregion

        #region Private — Normals

 /// <summary>
 /// Recomputes per-vertex normals after smoothing.
 /// Identycznie jak w CavityRenderer.GetOrComputeNormalsXna:
 /// - sumowanie normalnych triangles (area-weighted)
 /// - normalisation
 /// - inversion (outside = -normal)
 /// </summary>
        private static void RecomputeNormals(HirshfeldSurface surface)
        {
            int n = surface.VertexCount;
            if (n == 0) return;

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

 // ⭐ Normalna z iloczynu wektorowego
 // (length wektora = 2 × area triangle, so area-weighted)
                Vector3 nrm = Vector3.Cross(b - a, c - a);
                float lenSq = nrm.LengthSquared();
                if (lenSq < 1e-9f) continue;

                normals[ia] += nrm;
                normals[ib] += nrm;
                normals[ic] += nrm;
            }

 // ⭐ Normalizacja + inversion (outside = -normal)
            for (int i = 0; i < n; i++)
            {
                float len = normals[i].Length();
                normals[i] = len > 1e-9f
                    ? normals[i] / len
                    : Vector3.UnitY;

                normals[i] = -normals[i];
            }

 // ⭐ Save do surface
            if (surface.Normals == null)
                surface.Normals = new List<Vector3>(n);

            surface.Normals.Clear();
            surface.Normals.AddRange(normals);
        }

        #endregion
    }
}