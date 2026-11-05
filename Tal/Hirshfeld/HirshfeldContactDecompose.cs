// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldContactDecomafterse.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HirshfeldContactDecomafterse — full decomafterse fingerprintu (jak CrystalExplorer):
// - typ contactu per vertex (Element_inner ··· Element_outer)
// - % surface (area-weighted triangles)
// - tabela tekstowa do schowka
// - element-order normalisation (H···O = O···H)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Tal.Hirshfeld
{
    public static class HirshfeldContactDecompose
    {
 /// <summary>
 /// Extracts the element symbol from an atom name ("H12" → "H", "C12A" → "C", "CL1" → "CL").
 /// </summary>
        public static string ElementFromName(string name)
        {
            if (string.IsNullOrEmpty(name) || name == "?")
                return "?";

 // Skip leading digits / spaces
            int i = 0;
            while (i < name.Length && !char.IsLetter(name[i]))
                i++;

            if (i >= name.Length)
                return "?";

 // 1–2 litery (Cl, Br, Fe…)
            var sb = new StringBuilder(2);
            sb.Append(char.ToUpperInvariant(name[i]));
            i++;
            if (i < name.Length && char.IsLetter(name[i]) && char.IsLower(name[i]))
            {
                sb.Append(char.ToLowerInvariant(name[i]));
            }

            return sb.ToString();
        }

 /// <summary>
 /// Normalises contact type: always lighter/alphabetically first ··· second.
 /// Thanks to this H···O and O···H go into one bin.
 /// </summary>
        public static string NormalizeContactType(string elemA, string elemB)
        {
            if (string.IsNullOrEmpty(elemA)) elemA = "?";
            if (string.IsNullOrEmpty(elemB)) elemB = "?";

 // Alphabetical order (stable, as in many CE reports)
            if (string.CompareOrdinal(elemA, elemB) <= 0)
                return elemA + "···" + elemB;
            return elemB + "···" + elemA;
        }

        public static string MakeContactType(string innerName, string outerName)
        {
            return NormalizeContactType(
                ElementFromName(innerName),
                ElementFromName(outerName));
        }

 /// <summary>
 /// Fills ContactTypes[], areas, percentages, table.
 /// Area-weighting: each triangle dzieli area after ⅓ na 3 vertices.
 /// </summary>
        public static void Compute(HirshfeldAnalysisResults analysis, HirshfeldSurface surface)
        {
            if (analysis == null || surface == null || !surface.IsValid)
                return;

            int n = surface.VertexCount;
            analysis.ContactTypes = new string[n];

            var innerNames = surface.NearestInnerAtomName;
            var outerNames = surface.NearestOuterAtomName;

            for (int i = 0; i < n; i++)
            {
                string inner = (innerNames != null && i < innerNames.Length)
                    ? innerNames[i] : "?";
                string outer = (outerNames != null && i < outerNames.Length)
                    ? outerNames[i] : "?";

                analysis.ContactTypes[i] = MakeContactType(inner, outer);
            }

 // ── Area per vertex (⅓ areas each triangle) ──
            var areaPerVertex = new float[n];
            var inds = surface.Indices;
            var verts = surface.Vertices;

            float totalArea = 0f;

            if (inds != null && inds.Count >= 3)
            {
                for (int t = 0; t < inds.Count; t += 3)
                {
                    int ia = inds[t];
                    int ib = inds[t + 1];
                    int ic = inds[t + 2];
                    if (ia < 0 || ib < 0 || ic < 0 || ia >= n || ib >= n || ic >= n)
                        continue;

                    Vector3 ab = verts[ib] - verts[ia];
                    Vector3 ac = verts[ic] - verts[ia];
                    float area = 0.5f * Vector3.Cross(ab, ac).Length();
                    if (area < 1e-12f) continue;

                    float third = area / 3f;
                    areaPerVertex[ia] += third;
                    areaPerVertex[ib] += third;
                    areaPerVertex[ic] += third;
                    totalArea += area;
                }
            }

 // Fallback: no triangles → equal weight 1
            bool useArea = totalArea > 1e-6f;
            if (!useArea)
            {
                totalArea = n;
                for (int i = 0; i < n; i++)
                    areaPerVertex[i] = 1f;
            }

            analysis.TotalSurfaceArea = useArea ? totalArea : 0f;

 // ── Sumy per type ──
            analysis.ContactTypeCounts = new Dictionary<string, int>();
            analysis.ContactTypeAreas = new Dictionary<string, float>();
            analysis.ContactTypePercentages = new Dictionary<string, float>();

            for (int i = 0; i < n; i++)
            {
                string typ = analysis.ContactTypes[i];
                if (string.IsNullOrEmpty(typ)) continue;

                if (analysis.ContactTypeCounts.TryGetValue(typ, out int c))
                    analysis.ContactTypeCounts[typ] = c + 1;
                else
                    analysis.ContactTypeCounts[typ] = 1;

                if (analysis.ContactTypeAreas.TryGetValue(typ, out float a))
                    analysis.ContactTypeAreas[typ] = a + areaPerVertex[i];
                else
                    analysis.ContactTypeAreas[typ] = areaPerVertex[i];
            }

            float denom = useArea ? totalArea : n;
            if (denom < 1e-12f) denom = 1f;

            foreach (var kv in analysis.ContactTypeAreas)
            {
                analysis.ContactTypePercentages[kv.Key] =
                    100f * kv.Value / denom;
            }

            analysis.DecomposeTableText = BuildTable(analysis);
        }

        public static string BuildTable(HirshfeldAnalysisResults analysis)
        {
            if (analysis?.ContactTypePercentages == null
                || analysis.ContactTypePercentages.Count == 0)
            {
                return "(no contact decompose available)";
            }

            var sb = new StringBuilder();
            sb.AppendLine("Hirshfeld fingerprint decompose");
            if (analysis.TotalSurfaceArea > 0f)
                sb.AppendLine($"Surface area: {analysis.TotalSurfaceArea:F2} Å²");
            else
                sb.AppendLine("Surface area: (vertex-weighted)");
            sb.AppendLine();
            sb.AppendLine($"{"Contact",-12} {"Area (Å²)",12} {"%",8} {"Verts",8}");
            sb.AppendLine(new string('─', 44));

            var ordered = analysis.ContactTypePercentages
                .OrderByDescending(kv => kv.Value)
                .ToList();

            float sumPct = 0f;
            float sumArea = 0f;
            int sumVerts = 0;

            foreach (var kv in ordered)
            {
                string typ = kv.Key;
                float pct = kv.Value;
                float area = analysis.ContactTypeAreas.TryGetValue(typ, out float a) ? a : 0f;
                int verts = analysis.ContactTypeCounts.TryGetValue(typ, out int c) ? c : 0;

                sb.AppendLine($"{typ,-12} {area,12:F2} {pct,7:F1} {verts,8}");
                sumPct += pct;
                sumArea += area;
                sumVerts += verts;
            }

            sb.AppendLine(new string('─', 44));
            sb.AppendLine($"{"Total",-12} {sumArea,12:F2} {sumPct,7:F1} {sumVerts,8}");

            return sb.ToString();
        }
    }
}