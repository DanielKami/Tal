// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldDiagnosticsFormatter.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HirshfeldDiagnosticsFormatter — zamienia HirshfeldAtomDistanceDiagnostics
// na list linii (string, Color) do display w DebugWindow.
//
// ⭐ ZASADA: DebugWindow NIE WIE nic o Hirshfeldzie.
// This class does ALL the formatting work.
// DebugWindow dostaje only gotowe linie do display.

using System.Collections.Generic;
using System.Globalization;
using GdiColor = System.Drawing.Color;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Converte HirshfeldAtomDistanceDiagnostics → list sformatowanych linii.
 /// Each line has its own colour — DebugWindow only displays.
 /// </summary>
    public static class HirshfeldDiagnosticsFormatter
    {
        public const int MAX_BAR_LENGTH = 60;

 /// <summary>
 /// Main method — returns lines for display.
 /// </summary>
        public static List<(string text, GdiColor color)> FormatAsLines(
            HirshfeldAtomDistanceDiagnostics diag)
        {
            var lines = new List<(string, GdiColor)>();

            if (diag == null)
            {
                lines.Add(("(no diagnostics available)", GdiColor.Gray));
                return lines;
            }

 // ═══ Header ═══
            lines.Add(("", GdiColor.White));
            lines.Add(("════════════════════════════════════════════", GdiColor.Cyan));
            lines.Add(("🔬 HIRSHFELD ATOM DISTANCE DISTRIBUTION", GdiColor.Cyan));
            lines.Add(("════════════════════════════════════════════", GdiColor.Cyan));
            lines.Add(("", GdiColor.White));

 // ═══ Grid Info ═══
            lines.Add(("📦 Grid Info:", GdiColor.Yellow));
            lines.Add(($"  TargetCenter = ({diag.TargetCenter.X:F2}, " +
                       $"{diag.TargetCenter.Y:F2}, {diag.TargetCenter.Z:F2}) Å",
                       GdiColor.White));
            lines.Add(($"  R_target = {diag.RTarget:F3} Å", GdiColor.White));
            lines.Add(($"  R_grid   = {diag.RGrid:F3} Å  (margin = {diag.Margin:F2} Å)",
                       GdiColor.White));
            lines.Add(($"  Voxel size = {diag.VoxelSize:F4} Å", GdiColor.White));
            lines.Add(($"  Grid = {diag.GridResX} × {diag.GridResY} × {diag.GridResZ}",
                       GdiColor.White));
            lines.Add(($"  Total atoms = {diag.TotalAtoms}  " +
                       $"(target: {diag.TargetCount}, outer: {diag.OuterCount})",
                       GdiColor.White));
            lines.Add(("", GdiColor.White));

 // ═══ Target ═══
            lines.Add(("🎯 Target Atoms:", GdiColor.Yellow));
            if (diag.TargetCount > 0)
            {
                lines.Add(($"  min={diag.TargetMin:F3}  max={diag.TargetMax:F3}  " +
                           $"median={diag.TargetMedian:F3}", GdiColor.LightBlue));
                lines.Add(($"  p95={diag.TargetP95:F3}  p99={diag.TargetP99:F3}",
                           GdiColor.LightBlue));
            }
            else
            {
                lines.Add(("  (no target atoms)", GdiColor.Gray));
            }
            lines.Add(("", GdiColor.White));

 // ═══ Outer ═══
            lines.Add(("🌐 Outer Atoms:", GdiColor.Yellow));
            if (diag.OuterCount > 0)
            {
                lines.Add(($"  min={diag.OuterMin:F3}  max={diag.OuterMax:F3}  " +
                           $"median={diag.OuterMedian:F3}", GdiColor.LightGreen));
                lines.Add(($"  p95={diag.OuterP95:F3}  p99={diag.OuterP99:F3}",
                           GdiColor.LightGreen));
            }
            else
            {
                lines.Add(("  (no outer atoms)", GdiColor.Gray));
            }
            lines.Add(("", GdiColor.White));

 // ═══ Histogram ═══
            if (diag.Histogram != null && diag.Histogram.Length > 0)
            {
                lines.Add(($"📊 Histogram (bin = {diag.HistogramBinSize:F1} Å):",
                           GdiColor.Yellow));

                for (int b = 0; b < diag.Histogram.Length; b++)
                {
                    int count = diag.Histogram[b];
                    if (count == 0) continue;

                    float lo = b * diag.HistogramBinSize;
                    float hi = lo + diag.HistogramBinSize;

                    int barLen = diag.HistogramMaxCount > 0
                        ? (int)(MAX_BAR_LENGTH * count / (float)diag.HistogramMaxCount)
                        : 0;
                    barLen = System.Math.Max(1, barLen);
                    string bar = new string('█', barLen);

                    GdiColor color = PickHistogramColor(lo, diag.RTarget);

                    lines.Add(($"    {lo,5:F1}–{hi,5:F1} Å : {count,5}  {bar}", color));
                }
                lines.Add(("", GdiColor.White));
            }

 // ═══ Cutoff Analysis ═══
            if (diag.CutoffAnalysis != null && diag.CutoffAnalysis.Count > 0)
            {
                lines.Add(("✂️ Cutoff Analysis (atomy z d > cutoff = odrzucone):",
                           GdiColor.Yellow));

                foreach (var (dCutoff, inside, outside) in diag.CutoffAnalysis)
                {
                    int total = inside + outside;
                    float pctOut = total > 0 ? 100f * outside / total : 0f;
                    float delta = dCutoff - diag.RTarget;

                    string text = $"    R_target + {delta:F0} Å = {dCutoff:F2} Å  →  " +
                                  $"inside {inside,5}  outside {outside,5}  " +
                                  $"({pctOut:F1}% odrzucone)";

                    GdiColor color = pctOut > 20f ? GdiColor.LightGreen
                                   : pctOut > 5f ? GdiColor.Yellow
                                   : GdiColor.Gray;

                    lines.Add((text, color));
                }
                lines.Add(("", GdiColor.White));
            }

 // ═══ Hint ═══
            lines.Add(("💡 Interpretation:", GdiColor.LightBlue));
            lines.Add(("  • Atoms outside R_grid + 5σ_max are candidates for rejection.",
                       GdiColor.LightGray));
            lines.Add(("  • Bezpieczne odrzucenie: d > R_target + 5 Å.",
                       GdiColor.LightGray));
            lines.Add(("  • Aggressive rejection: d > R_target + 4 Å (risk of artefacts).",
                       GdiColor.LightGray));
            lines.Add(("", GdiColor.White));
            lines.Add(("════════════════════════════════════════════", GdiColor.Cyan));

            return lines;
        }

 /// <summary>
 /// Histogram bar colour — green (near), yellow, orange (far).
 /// </summary>
        private static GdiColor PickHistogramColor(float lo, float rTarget)
        {
            if (lo < rTarget + 4) return GdiColor.LightGreen;
            if (lo < rTarget + 6) return GdiColor.Yellow;
            return GdiColor.Orange;
        }
    }
}