// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldAnalysisResults.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Tryb colourowania surface.
 /// </summary>
    public enum HirshfeldPropertyMode
    {
        BaseColor = 0,
        DNorm = 1,
        DE = 2,
        DI = 3,
        Charge = 4,          // ⭐ NEW — charge (green-yellow-red)
        Hydrophobicity = 5   // ⭐ NOWE — hydrofobowość (niebieski-white-brązowy)
    }

 /// <summary>
 /// Container for Hirshfeld analysis results (v1.3 — fingerprint, contacts, bins per vertex).
 ///
 /// ⭐ Used by HirshfeldFingerprintWindow do budowy heatmapy 2D (d_e × d_i).
 /// ⭐ Zawiera oryginalne values DNorm/DE/DI przez HirshfeldColorSmoothing.
 /// ⭐ v1.3: VertexBins — for each vertex jego bin (binDe, binDi).
 /// Used by HirshfeldDocument for highlighting mesh region
 /// after a click on the fingerprint map.
 /// </summary>
    public class HirshfeldAnalysisResults
    {


 /// <summary>Typ contactu per vertex, np. "C···H". Indeks = vertex.</summary>
        public string[] ContactTypes;

 /// <summary>Liczba vertices per type.</summary>
        public Dictionary<string, int> ContactTypeCounts =
            new Dictionary<string, int>();

 /// <summary>Sum of areas [Å²] per typee (area-weighted).</summary>
        public Dictionary<string, float> ContactTypeAreas =
            new Dictionary<string, float>();

 /// <summary>Surface percentage share per type.</summary>
        public Dictionary<string, float> ContactTypePercentages =
            new Dictionary<string, float>();

 /// <summary>Total surface area [Å²] (0 = vertex weights were used).</summary>
        public float TotalSurfaceArea;

 /// <summary>Gotowa tabela tekstowa do skopiowania.</summary>
        public string DecomposeTableText = "";

 /// <summary>
 /// Active type filter (null / empty = all).
 /// Used by fingerprint window i highlighting meshu.
 /// </summary>
        public HashSet<string> ActiveContactFilter;


 /// <summary>
 /// Surface for which the analysis was performed.
 /// </summary>
        public HirshfeldSurface Surface;

 /// <summary>
 /// ⭐ Fingerprint 2D: histogram (binDe, binDi) → count.
 /// Klucz = (bin d_e, bin d_i), value = liczba vertices w binie.
 /// </summary>
        public Dictionary<(int binDe, int binDi), int> FingerprintHistogram =
            new Dictionary<(int binDe, int binDi), int>();

 /// <summary>
 /// Bin resolution (Å). Typically 0.05 Å (20 bins per 1 Å).
 /// </summary>
        public float FingerprintBinSize = 0.05f;

 /// <summary>
 /// ⭐ Minimalna value osi d_e / d_i (Å).
 /// Allows starting the map from e.g. 0.5 Å instead of 0.0.
 /// </summary>
        public float FingerprintRangeMin { get; set; } = 0.0f;


 /// <summary>
 /// Maksymalny zakres d_e / d_i (Å). Typically 3.0 Å.
 /// Everything above is ignored when building the heatmap.
 /// </summary>
        public float FingerprintRangeMax = 3.0f;

 

 

 /// <summary>
 /// ⭐ v1.3: (binDe, binDi) per vertex meshu.
 /// Indeks = index vertex, value = jego bin.
 /// (-1, -1) = vertex outside the map (e.g. d_e = 0 or d_i = 0).
 /// Used by HirshfeldDocument for highlighting mesh region.
 /// </summary>
        public (int binDe, int binDi)[] VertexBins;

 /// <summary>
 /// Calculates number vertices per type contactu (na afterdstawie ContactTypes).
 /// Call after filling ContactTypes.
 /// </summary>
        public void ComputeContactTypeCounts()
        {
            ContactTypeCounts.Clear();

            if (ContactTypes == null) return;

            foreach (var t in ContactTypes)
            {
                if (string.IsNullOrEmpty(t)) continue;

                if (ContactTypeCounts.TryGetValue(t, out int c))
                    ContactTypeCounts[t] = c + 1;
                else
                    ContactTypeCounts[t] = 1;
            }
        }

 /// <summary>
 /// Info debug.
 /// </summary>
        public string GetInfo()
        {
            int totalBins = FingerprintHistogram?.Count ?? 0;
            int totalContacts = ContactTypes?.Length ?? 0;

            return $"Fingerprint: {totalBins} bins, " +
                   $"bin size = {FingerprintBinSize:F3} Å, " +
                   $"range = {FingerprintRangeMax:F1} Å, " +
                   $"contacts = {totalContacts}";
        }
    }
}