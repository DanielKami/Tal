// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldAtomDistanceDiagnostics.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HirshfeldAtomDistanceDiagnostics — statystyki distance atoms od TargetCenter.
//
// Used for diagnostics: how many atoms are at what distance from the surface
// Hirshfelda i which can be discarded without changing the result.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Statystyki distance atoms target + outer od TargetCenter.
 /// Used by DebugWindow do diagnostyki.
 /// </summary>
    public class HirshfeldAtomDistanceDiagnostics
    {
 // ═══ Target ═══
        public int TargetCount;
        public float TargetMin, TargetMax, TargetMedian, TargetP95, TargetP99;

 // ═══ Outer ═══
        public int OuterCount;
        public float OuterMin, OuterMax, OuterMedian, OuterP95, OuterP99;

 // ═══ Grid ═══
        public Vector3 TargetCenter;
        public float RTarget;       // target radius (from centre to farthest atom)
        public float RGrid;         // promień grid (R_target + margin)
        public float Margin;        // margin użyty przy budowie grida
        public float VoxelSize;
        public int GridResX, GridResY, GridResZ;
        public int TotalAtoms;

 // ═══ Histogram ═══
        public float HistogramBinSize;
        public int[] Histogram;                 // counts per bin
        public int HistogramMaxCount;           // max(Histogram) — do skalowania

 // ═══ Balance for different thresholds ═══
 /// <summary>Progi d_cutoff [Å] i liczba atoms w each strefie.</summary>
        public List<(float dCutoff, int insideCount, int outsideCount)> CutoffAnalysis
            = new List<(float, int, int)>();

 // ═══ Metody ═══

 /// <summary>
 /// Liczy statystyki na afterdstawie Scene + Grid + Surface.
 /// </summary>
        public static HirshfeldAtomDistanceDiagnostics Compute(
            HirshfeldScene scene,
            HirshfeldGrid grid,
            HirshfeldSurface surface)
        {
            var diag = new HirshfeldAtomDistanceDiagnostics();

            if (scene == null)
                return diag;

            diag.TargetCenter = scene.TargetCenter;
            diag.RTarget = scene.TargetRadius;
            diag.TotalAtoms = scene.TargetAtoms.Count + scene.OuterAtoms.Count;

 // ═══ Grid info ═══
            if (grid != null)
            {
                diag.GridResX = grid.ResX;
                diag.GridResY = grid.ResY;
                diag.GridResZ = grid.ResZ;

 // Voxel size = (BoundsMax.X - BoundsMin.X) / (ResX - 1)
                float extent = grid.BoundsMax.X - grid.BoundsMin.X;
                diag.VoxelSize = grid.ResX > 1 ? extent / (grid.ResX - 1) : 0f;

 // R_grid = half diagonal / 2 (approximation)
 // albo: R_grid = R_target + margin
 // Wyznaczymy z surface.Metadata (if jest)
                if (surface != null)
                {
 // margin = R_grid - R_target
 // Ale nie mamy R_grid wprost — afterlicz z gridu:
 // grid is a cube, R_grid = (BoundsMax.X - BoundsMin.X) / 2
                    diag.RGrid = extent / 2f;
                    diag.Margin = diag.RGrid - diag.RTarget;
                }
            }

 // ═══ Target distances ═══
            var targetDist = new List<float>(scene.TargetAtoms.Count);
            foreach (var a in scene.TargetAtoms)
                targetDist.Add((a.CartPosition - scene.TargetCenter).Length());

            targetDist.Sort();
            diag.TargetCount = targetDist.Count;

            if (targetDist.Count > 0)
            {
                diag.TargetMin = targetDist[0];
                diag.TargetMax = targetDist[^1];
                diag.TargetMedian = targetDist[targetDist.Count / 2];
                diag.TargetP95 = targetDist[(int)(targetDist.Count * 0.95)];
                diag.TargetP99 = targetDist[Math.Min(targetDist.Count - 1,
                                                     (int)(targetDist.Count * 0.99))];
            }

 // ═══ Outer distances ═══
            var outerDist = new List<float>(scene.OuterAtoms.Count);
            foreach (var a in scene.OuterAtoms)
                outerDist.Add((a.CartPosition - scene.TargetCenter).Length());

            outerDist.Sort();
            diag.OuterCount = outerDist.Count;

            if (outerDist.Count > 0)
            {
                diag.OuterMin = outerDist[0];
                diag.OuterMax = outerDist[^1];
                diag.OuterMedian = outerDist[outerDist.Count / 2];
                diag.OuterP95 = outerDist[(int)(outerDist.Count * 0.95)];
                diag.OuterP99 = outerDist[Math.Min(outerDist.Count - 1,
                                                   (int)(outerDist.Count * 0.99))];
            }

 // ═══ Histogram ═══
            if (outerDist.Count > 0)
            {
                diag.HistogramBinSize = 0.5f;
                float maxD = outerDist[^1];
                int nBins = (int)MathF.Ceiling(maxD / diag.HistogramBinSize) + 1;
                if (nBins > 100) nBins = 100;   // sanity

                diag.Histogram = new int[nBins];

                foreach (float d in outerDist)
                {
                    int b = (int)(d / diag.HistogramBinSize);
                    if (b >= nBins) b = nBins - 1;
                    diag.Histogram[b]++;
                }

                diag.HistogramMaxCount = 0;
                for (int i = 0; i < nBins; i++)
                    if (diag.Histogram[i] > diag.HistogramMaxCount)
                        diag.HistogramMaxCount = diag.Histogram[i];
            }
            else
            {
                diag.Histogram = Array.Empty<int>();
            }

 // ═══ Cutoff analysis ═══
 // For different R_target + delta (2, 3, 4, 5, 6 Å)
            diag.CutoffAnalysis.Clear();

            float[] deltas = { 2f, 3f, 4f, 5f, 6f, 7f, 8f };
            foreach (float delta in deltas)
            {
                float cutoff = diag.RTarget + delta;
                int inside = 0, outside = 0;

                foreach (float d in outerDist)
                {
                    if (d <= cutoff) inside++;
                    else outside++;
                }

                diag.CutoffAnalysis.Add((cutoff, inside, outside));
            }

            return diag;
        }

 /// <summary>
 /// Returns the number of atoms that will be discarded at the given d_cutoff.
 /// </summary>
        public (int inside, int outside) GetCutoffCounts(float dCutoff)
        {
            foreach (var (d, inside, outside) in CutoffAnalysis)
            {
                if (MathF.Abs(d - dCutoff) < 0.01f)
                    return (inside, outside);
            }
            return (0, 0);
        }

 /// <summary>
 /// Returns a formatted balance for all thresholds.
 /// </summary>
        public IEnumerable<string> FormatCutoffAnalysis()
        {
            foreach (var (dCutoff, inside, outside) in CutoffAnalysis)
            {
                int total = inside + outside;
                float pctOut = total > 0 ? 100f * outside / total : 0f;
                float delta = dCutoff - RTarget;

                yield return
                    $"R_target + {delta:F0} Å = {dCutoff:F2} Å  →  " +
                    $"inside {inside,5}  outside {outside,5}  " +
                    $"({pctOut:F1}% odrzucone)";
            }
        }
    }
}