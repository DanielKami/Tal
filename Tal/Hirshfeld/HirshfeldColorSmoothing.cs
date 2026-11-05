// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldColorSmoothing.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HirshfeldColorSmoothing v3 — selective smoothing for DNorm.
//
// ⭐ v3 CHANGES:
//
// FIX #1 — Selective smoothing for DNorm.
// Old code smoothed DNorm globally, which slightly blurred the
// red/blue extremes. The real problem was in the MIDDLE zone
// (|dnorm| < 0.5), where pixel-level fluctuations around zero
// produced ugly non-physical blotches that global smoothing did
// not fix.
//
// New code smooths ONLY the middle zone (|dnorm| < 0.5) and
// leaves the red/blue extremes (|dnorm| >= 0.5) SHARP — preserving
// the strong contact features that were already good.
//
// FIX #2 — Increased BASE_RADIUS from 0.20 Å to 0.40 Å.
// Rationale: the middle-zone fluctuations have a spatial extent
// of ~0.3-0.5 Å. The old 0.20 Å radius was too small to average
// them out. The new 0.40 Å radius smooths the middle zone
// properly without touching the sharp extremes.
//
// FIX #3 — DE and DI are NOT smoothed (kept from v3).
// Their contrast is the whole afterint of those visualizations —
// smoothing them destroys the contact features.
//
// ⭐ v2 (kept):
// - Neighborhood radius in Å (not in grid units)
// - Distance weights 1/d² — closer neighbors dominate
// - Iterations 1-3 (not 5) — gentle smoothing
// - Always starts from the original values (DNormOriginal / DEOriginal
// / DIOriginal), so the slider is deterministic.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Per-vertex COLOR smoothing (does NOT change geometry).
 ///
 /// ⭐ v3:
 /// - DNorm is smoothed ONLY in the middle zone (|dnorm| < 0.5).
 /// The red/blue extremes stay sharp.
 /// - DE and DI are NOT smoothed — preserve their contact contrast.
 /// - BASE_RADIUS increased to 0.40 Å for better middle-zone averaging.
 /// </summary>
    public static class HirshfeldColorSmoothing
    {
 // ⭐ FIX #2: Base neighborhood radius in Å — for level 1.
 // Increased from 0.20 Å to 0.40 Å so the middle-zone
 // fluctuations (spatial extent ~0.3-0.5 Å) are averaged out.
        private const float BASE_RADIUS = 0.40f;

 // ⭐ FIX #2: Radius step per level (level 2 = 0.60, level 5 = 1.20 Å)
        private const float RADIUS_STEP = 0.20f;

 // ⭐ FIX #1: Middle-zone threshold.
 // DNorm values with |dnorm| < this threshold are smoothed.
 // Values with |dnorm| >= this threshold are kept sharp.
 // 0.5 is a good balance — matches the visual width of the
 // white transition zone in CrystalExplorer-style palettes.
        private const float MIDDLE_ZONE_THRESHOLD = 0.5f;

        public static void Apply(HirshfeldSurface surface, int level)
        {
            if (surface == null || !surface.IsValid) return;

            int n = surface.VertexCount;

 // ⭐ Always start from the originals (deterministic slider)
            if (surface.DNormOriginal != null && surface.DNormOriginal.Length == n)
                surface.DNorm = (float[])surface.DNormOriginal.Clone();

            if (surface.DEOriginal != null && surface.DEOriginal.Length == n)
                surface.DE = (float[])surface.DEOriginal.Clone();

            if (surface.DIOriginal != null && surface.DIOriginal.Length == n)
                surface.DI = (float[])surface.DIOriginal.Clone();

            if (level <= 0) return;
            if (n < 3) return;

 // Level → parameters mapping (with increased radius):
 // Level 1: radius 0.40 Å, 1 iter
 // Level 2: radius 0.60 Å, 1 iter
 // Level 3: radius 0.80 Å, 2 iter
 // Level 4: radius 1.00 Å, 2 iter
 // Level 5: radius 1.20 Å, 3 iter
            float radius = BASE_RADIUS + (level - 1) * RADIUS_STEP;

            int iterations = level switch
            {
                1 => 1,
                2 => 1,
                3 => 2,
                4 => 2,
                5 => 3,
                _ => 1
            };

            var neighbors = BuildNeighborList(surface, radius);

 // ═══════════════════════════════════════════════════════════
 // ⭐ FIX #1: SELECTIVE smoothing for DNorm.
 //
 // Smooth ONLY the middle zone (|dnorm| < MIDDLE_ZONE_THRESHOLD),
 // where pixel-level fluctuations around zero produce ugly
 // non-physical blotches.
 //
 // Leave the red/blue extremes (|dnorm| >= threshold) SHARP —
 // they were already good and smoothing them would destroy
 // the strong contact features.
 // ═══════════════════════════════════════════════════════════
            if (surface.DNorm != null && surface.DNorm.Length == n)
            {
                var smoothed = SmoothValues(
                    surface.DNorm, surface.Vertices, neighbors, iterations);

                for (int i = 0; i < n; i++)
                {
                    float d = surface.DNorm[i];
                    if (MathF.Abs(d) < MIDDLE_ZONE_THRESHOLD)
                        surface.DNorm[i] = smoothed[i];   // smooth middle
 // else: keep sharp (red/blue extremes)
                }
            }

 // ⭐ v3: DE and DI are NOT smoothed — preserve their contrast.
 // Smoothing DE/DI would flatten the very features that
 // make those visualizations useful.
 //
 // if (surface.DE != null && surface.DE.Length == n)
 // surface.DE = SmoothValues(surface.DE, ...);
 //
 // if (surface.DI != null && surface.DI.Length == n)
 // surface.DI = SmoothValues(surface.DI, ...);
        }

 /// <summary>
 /// Builds a neighbor list within radius R with distance-based weights.
 /// Returns an array of (index, weight) pairs for each vertex.
 ///
 /// Uses a spatial hash grid (bucket size = radius) for O(n) lookup.
 /// </summary>
        private static List<(int index, float weight)>[] BuildNeighborList(
            HirshfeldSurface surface, float radius)
        {
            int n = surface.VertexCount;
            var verts = surface.Vertices;

            float cellSize = Math.Max(radius, 0.01f);
            var buckets = new Dictionary<(int, int, int), List<int>>();

            (int, int, int) GetCell(Vector3 p)
            {
                return (
                    (int)MathF.Floor(p.X / cellSize),
                    (int)MathF.Floor(p.Y / cellSize),
                    (int)MathF.Floor(p.Z / cellSize)
                );
            }

            for (int i = 0; i < n; i++)
            {
                var key = GetCell(verts[i]);
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<int>(8);
                    buckets[key] = list;
                }
                list.Add(i);
            }

            float radiusSq = radius * radius;
            var neighbors = new List<(int, float)>[n];

            for (int i = 0; i < n; i++)
            {
                var p = verts[i];
                var cell = GetCell(p);
                var result = new List<(int, float)>(16);

                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            var key = (cell.Item1 + dx, cell.Item2 + dy, cell.Item3 + dz);
                            if (!buckets.TryGetValue(key, out var list)) continue;

                            foreach (int j in list)
                            {
                                if (j == i) continue;

                                float d2 = (verts[j] - p).LengthSquared();
                                if (d2 > radiusSq || d2 < 1e-9f) continue;

 // ⭐ Weight = 1/d² — closer neighbors dominate.
 // This preserves local structure while
 // still averaging out small fluctuations.
                                float w = 1.0f / d2;

                                result.Add((j, w));
                            }
                        }

                neighbors[i] = result;
            }

            return neighbors;
        }

 /// <summary>
 /// Smooths a value array with distance weights.
 /// new[i] = (own + Σ w_j · v_j) / (1 + Σ w_j)
 ///
 /// The own value has weight 1.0, so the result is a proper
 /// weighted average centered on the original value.
 /// </summary>
        private static float[] SmoothValues(
            float[] values,
            List<Vector3> vertices,
            List<(int index, float weight)>[] neighbors,
            int iterations)
        {
            int n = values.Length;
            var current = (float[])values.Clone();
            var next = new float[n];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int i = 0; i < n; i++)
                {
                    var nb = neighbors[i];
                    if (nb == null || nb.Count == 0)
                    {
                        next[i] = current[i];
                        continue;
                    }

 // Own value with weight 1.0
                    float sum = current[i];
                    float sumW = 1.0f;

                    foreach (var (j, w) in nb)
                    {
                        sum += current[j] * w;
                        sumW += w;
                    }

                    next[i] = sum / sumW;
                }

 // Swap buffers (no allocation per iteration)
                var tmp = current;
                current = next;
                next = tmp;
            }

            return current;
        }
    }
}