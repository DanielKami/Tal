// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: VdWAnalysisReafterrt.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Text;
using System.Text.RegularExpressions;

namespace Tal.Cavity
{
 /// <summary>
 /// Generates reafterrts from VdW analysis results.
 /// Supafterrts TXT and HTML formats.
 /// </summary>
    public static class VdWAnalysisReport
    {
        #region Public Methods

        public static string GenerateTxtReport(VdWAnalysisResults results)
        {
            if (results == null || results.CavityResults.Count == 0)
                return "No results to report.";

            var sb = new StringBuilder();
            AppendHeader(sb, results);
            AppendQSPR(sb, results);
            AppendScaleSummary(sb, results);
            AppendScaleDependence(sb, results);
            AppendSpherePacking(sb, results);
            AppendGasCompatibility(sb, results);
            AppendSolventCompatibility(sb, results);
            AppendElectrostaticSummary(sb, results);
            AppendCriticalEvents(sb, results);
            AppendCavityTracking(sb, results);
            AppendChannelAnalysis(sb, results);
            AppendFooter(sb);
            return sb.ToString();
        }

        public static string GenerateHtmlReport(VdWAnalysisResults results)
        {
            if (results == null || results.CavityResults.Count == 0)
                return GetEmptyHtmlReport();

            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='UTF-8'>");
            sb.AppendLine($"<title>VDW Analysis Report - {DateTime.Now:yyyy-MM-dd}</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(GetHtmlStyles());
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine($"<h1>VDW SCALING ANALYSIS</h1>");
            sb.AppendLine($"<p class='timestamp'>Generated: {DateTime.Now}</p>");
            sb.AppendLine("<hr>");

            AppendHtmlHeader(sb, results);
            AppendHtmlQSPR(sb, results);
            AppendHtmlScaleSummary(sb, results);
            AppendHtmlScaleDependence(sb, results);
            AppendHtmlSpherePacking(sb, results);
            AppendHtmlGasCompatibility(sb, results);
            AppendHtmlSolventCompatibility(sb, results);
            AppendHtmlElectrostatic(sb, results);
            AppendHtmlCriticalEvents(sb, results);
            AppendHtmlCavityTracking(sb, results);
            AppendHtmlChannels(sb, results);

            sb.AppendLine("<hr>");
            sb.AppendLine($"<p class='timestamp'>Generated: {DateTime.Now}</p>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        public static bool SaveReport(VdWAnalysisResults results, string filePath)
        {
            try
            {
                string extension = Path.GetExtension(filePath).ToLower();
                string content = extension == ".html" ? GenerateHtmlReport(results) : GenerateTxtReport(results);
                File.WriteAllText(filePath, content, Encoding.UTF8);
                return true;
            }
            catch { return false; }
        }

        #endregion

        #region TXT Sections

        private static void AppendHeader(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine($"VDW SCALING ANALYSIS");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();

 // ⭐ CELL PARAMETERS
            sb.AppendLine("CELL PARAMETERS:");
            sb.AppendLine($"  Volume:         {results.CellVolume:F4} Å³");
            sb.AppendLine($"  Voxel volume:   {CellParameters.GetVoxelVolume():F6} Å³");
            sb.AppendLine($"  Atoms in cell:  {results.AtomCount}");
            sb.AppendLine($"  Cell mass:      {results.CellMass:F2} u");
            sb.AppendLine();

 // ⭐ PARAMETRY ANALIZY
            sb.AppendLine("ANALYSIS PARAMETERS:");
            sb.AppendLine($"  Resolution:     {Flags.Instance.Resolution:F3} Å");
            sb.AppendLine($"  Thermal motion (ADP):    {(Flags.Instance.UseThermalMotion ? "YES" : "NO")}");
            sb.AppendLine("  SDF:            computed directly for each VdW scale");
            sb.AppendLine($"  Scales tested:  {string.Join(", ", results.VdWScales)}");
            sb.AppendLine($"  Scale range:    {results.VdWScales.Min():F2} - {results.VdWScales.Max():F2}");
            sb.AppendLine($"  Analysis time:  {results.TotalProcessingTimeMs / 1000:F2} s");
            sb.AppendLine();

 // ⭐ RESULTS SUMMARY
            int totalCavities = 0;
            int totalChannels = results.GetTotalChannelCount();
            int cavitiesWithChannels = results.GetCavitiesWithChannelsCount();
            float totalVolume = 0f;
            float totalSurface = 0f;

            foreach (var kvp in results.CavityResults)
            {
                var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - 1.0f) < 0.001f);
                if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                {
                    totalCavities++;
                    totalVolume += r.Volume;
                    totalSurface += r.SurfaceArea;
                }
            }

            sb.AppendLine("SUMMARY (VdW=1.0):");
            sb.AppendLine($"  Total cavities:             {totalCavities}");
            sb.AppendLine($"  Total volume:               {totalVolume:F2} Å³");
            sb.AppendLine($"  Total surface:              {totalSurface:F2} Å²");
            sb.AppendLine($"  Volume/Surface ratio:       {(totalSurface > 0 ? totalVolume / totalSurface : 0):F3}");
            sb.AppendLine($"  Cavities with channels:     {cavitiesWithChannels}");
            sb.AppendLine($"  Total channels:             {totalChannels}");
            sb.AppendLine();

 // Largest cavity
            var largest = results.CavityResults
                .SelectMany(kvp => kvp.Value)
                .Where(r => r != null && r.Volume > 0 && !r.IsPeriodicDuplicate && Math.Abs(r.VdWScale - 1.0f) < 0.001f)
                .OrderByDescending(r => r.Volume)
                .FirstOrDefault();

            if (largest != null)
            {
                sb.AppendLine("LARGEST CAVITY (VdW=1.0):");
                sb.AppendLine($"  ID:                         #{largest.CavityId}");
                sb.AppendLine($"  Volume:                     {largest.Volume:F2} Å³");
                sb.AppendLine($"  Surface:                    {largest.SurfaceArea:F2} Å²");
                sb.AppendLine($"  Sphericity:                 {largest.Sphericity:F3}");
                sb.AppendLine($"  Walls:                      {largest.WallConnections}");
                sb.AppendLine($"  Voxels:                     {largest.VoxelCount}");
                sb.AppendLine($"  Triangles:                  {largest.TriangleCount}");
                sb.AppendLine();
            }

 // Largest sphere
            if (results.SpherePacking.Count > 0)
            {
                var bestPacking = results.SpherePacking.Values
                    .OrderByDescending(p => p.MaxSphereRadius)
                    .FirstOrDefault();

                if (bestPacking != null && bestPacking.MaxSphereRadius > 0)
                {
                    sb.AppendLine("LARGEST SPHERE FIT:");
                    sb.AppendLine($"  Cavity ID:                  #{bestPacking.CavityId}");
                    sb.AppendLine($"  Radius:                     {bestPacking.MaxSphereRadius:F3} Å");
                    sb.AppendLine($"  Diameter:                   {bestPacking.EffectiveDiameter:F3} Å");
                    sb.AppendLine($"  Spheres:                    {bestPacking.SphereCount}");
                    sb.AppendLine($"  Packing efficiency:         {bestPacking.PackingEfficiency:F3}");
                    sb.AppendLine($"  Usable volume:              {bestPacking.UsableVolume:F2} Å³");
                    sb.AppendLine($"  Channels:                   {(bestPacking.HasChannels ? bestPacking.ChannelInfo : "none")}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();
        }

        private static void AppendQSPR(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("QSPR DESCRIPTORS SUMMARY");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

 // I_frag
            sb.AppendLine($"I_frag (Fragmentation Index):        {results.FragmentationIndex:F6}");
            sb.AppendLine($"  → Number of cavities per atom in the cell; measure of structural fragmentation");
            sb.AppendLine($"    I_frag = N_cavities / N_atoms(cell), counted at the reference scale (VdW≈1.0),");
            sb.AppendLine($"    only cavities with volume >= 1 Å³ (smaller pockets are numerical noise)");
            sb.AppendLine($"    Higher values → more fragmented structure, more cavities");
            sb.AppendLine($"    No validated threshold: compare only between structures analysed the same way");
            sb.AppendLine();

 // f_collapse
            sb.AppendLine($"f_collapse (Critical Collapse):      {results.CollapseScale:F2}");
            sb.AppendLine($"  → Smallest VdW scale at which total cavity volume falls below 0.1% of the cell volume");
            sb.AppendLine($"    (2.00 means the threshold was not reached); structural stability indicator");
            sb.AppendLine($"    Higher values → more stable structure, cavities persist at larger VdW radii");
            sb.AppendLine($"    f_collapse > 1.5 → stable framework");
            sb.AppendLine($"    f_collapse < 1.2 → fragile framework");
            sb.AppendLine();

 // Ω_Q
            sb.AppendLine($"Ω_Q (Electrostatic Frustration):     {results.ElectrostaticFrustration:F6}");
            sb.AppendLine($"  → Total charge distribution inhomogeneity on cavity surfaces");
            sb.AppendLine($"    Ω_Q = ∫ ⟨σ_Q²⟩ · V d(scale) over the tested VdW scales (trapezoidal rule)");
            sb.AppendLine($"    Higher values → more heterogeneous charge distribution");
            sb.AppendLine($"    Low values → uniform charge distribution, less selective");
            sb.AppendLine();

 // Ψ_H
            sb.AppendLine($"Ψ_H (Hydrophobic Frustration):       {results.HydrophobicFrustration:F6}");
            sb.AppendLine($"  → Total hydrophobicity inhomogeneity on cavity surfaces");
            sb.AppendLine($"    Ψ_H = ∫ ⟨σ_H²⟩ · S d(scale) over the tested VdW scales (trapezoidal rule)");
            sb.AppendLine($"    Higher values → more heterogeneous hydrophobicity");
            sb.AppendLine($"    Controls selectivity for polar vs non-polar guests");
            sb.AppendLine();

 // ⟨Z_cav⟩
            sb.AppendLine($"⟨Z_cav⟩ (Mean Polarizability):       {results.MeanPolarizability:F4}");
            sb.AppendLine($"  → Average polarizability of cavities; measure of polarizability capacity");
            sb.AppendLine($"    ⟨Z_cav⟩ = (1/N) Σ Z_effective over all cavities");
            sb.AppendLine($"    Higher values → stronger dispersion interactions with guests");
            sb.AppendLine($"    Correlates with adsorption enthalpy of non-polar guests");
            sb.AppendLine();

 // α_afterlar
            sb.AppendLine($"α_polar (Max Dipole Sensitivity):    {results.MaxDipoleSensitivity:F6}");
            sb.AppendLine($"  → Maximum dipole sensitivity; response to external electric field");
            sb.AppendLine($"    α_polar = max |d|Dipole|/d(VdW)| between consecutive VdW scales");
            sb.AppendLine($"    Higher values → structure more responsive to electric fields");
            sb.AppendLine($"    Important for polar guest interactions and separation");
            sb.AppendLine();
        }

        private static void AppendScaleSummary(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("SCALE SUMMARY (with normalizations)");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            sb.AppendLine($"{"Scale",6} {"Cav",5} {"Vol(Å³)",12} {"Surf(Å²)",12} {"V/S",10} {"Vol/Voxels",12} {"Vol/Atom",12} {"Channels",10}");
            sb.AppendLine(new string('-', 100));

            float voxelVolume = CellParameters.GetVoxelVolume();

            foreach (var scale in results.VdWScales)
            {
                int count = 0;
                float totalV = 0f;
                float totalS = 0f;
                int channels = 0;

                foreach (var kvp in results.CavityResults)
                {
                    var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - scale) < 0.001f);
                    if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                    {
                        count++;
                        totalV += r.Volume;
                        totalS += r.SurfaceArea;

                        if (results.CrystalChannelData.ContainsKey(r.CavityId) &&
                            results.CrystalChannelData[r.CavityId].Count > 0)
                            channels++;
                    }
                }

                float volPerVoxel = voxelVolume > 0 ? totalV / voxelVolume : 0f;
                float volPerAtom = results.AtomCount > 0 ? totalV / results.AtomCount : 0f;
                float vOverS = totalS > 0 ? totalV / totalS : 0f;

                sb.AppendLine($"{scale,6:F2} {count,5} {totalV,12:F4} {totalS,12:F4} {vOverS,10:F3} {volPerVoxel,12:F0} {volPerAtom,12:F6} {channels,10}");
            }
            sb.AppendLine();
        }

        private static void AppendScaleDependence(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("VDW SCALE DEPENDENCE - Volume & Surface Area Changes");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            sb.AppendLine($"{"Scale",6} {"Vol(Å³)",12} {"Surf(Å²)",12} {"dV/dscale",12} {"dS/dscale",12} {"ΔV(%)",10} {"ΔS(%)",10} {"V/S",10}");
            sb.AppendLine(new string('-', 92));

            float prevV = 0f, prevS = 0f;
            float prevScale = 0f;
            bool first = true;

            var scaleData = new List<(float scale, float vol, float surf)>();

            foreach (var scale in results.VdWScales.OrderBy(s => s))
            {
                float totalV = 0f, totalS = 0f;
                foreach (var kvp in results.CavityResults)
                {
                    var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - scale) < 0.001f);
                    if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                    {
                        totalV += r.Volume;
                        totalS += r.SurfaceArea;
                    }
                }
                scaleData.Add((scale, totalV, totalS));
            }

            foreach (var data in scaleData)
            {
                float dV = 0f, dS = 0f;
                float dVpercent = 0f, dSpercent = 0f;
                float dVdScale = 0f, dSdScale = 0f;
                float vOverS = data.surf > 0 ? data.vol / data.surf : 0f;

                if (!first && prevScale > 0)
                {
                    float deltaScale = data.scale - prevScale;
                    if (deltaScale > 0.0001f)
                    {
                        dV = data.vol - prevV;
                        dS = data.surf - prevS;
                        dVpercent = prevV > 0 ? (dV / prevV) * 100f : 0f;
                        dSpercent = prevS > 0 ? (dS / prevS) * 100f : 0f;
                        dVdScale = dV / deltaScale;
                        dSdScale = dS / deltaScale;
                    }
                }

                string dVStr = first ? "         -" : $"{dV,12:F4}";
                string dSStr = first ? "         -" : $"{dS,12:F4}";
                string dVpStr = first ? "        -" : $"{dVpercent,10:F2}";
                string dSpStr = first ? "        -" : $"{dSpercent,10:F2}";
                string dVdStr = first ? "          -" : $"{dVdScale,12:F4}";
                string dSdStr = first ? "          -" : $"{dSdScale,12:F4}";

                sb.AppendLine($"{data.scale,6:F2} {data.vol,12:F4} {data.surf,12:F4} {dVdStr} {dSdStr} {dVpStr} {dSpStr} {vOverS,10:F3}");

                prevV = data.vol;
                prevS = data.surf;
                prevScale = data.scale;
                first = false;
            }
            sb.AppendLine();
        }

        private static void AppendSpherePacking(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("SPHERE PACKING ANALYSIS");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            if (results.SpherePacking.Count == 0)
            {
                sb.AppendLine("No sphere packing data available.");
                sb.AppendLine();
                return;
            }

            sb.AppendLine($"{"Cav",4} {"Spheres",8} {"Max R(Å)",12} {"Diameter(Å)",12} {"Packing",12} {"Vol(Å³)",12} {"Channels",12}");
            sb.AppendLine(new string('-', 80));

            foreach (var kvp in results.SpherePacking.OrderBy(k => k.Key))
            {
                var packing = kvp.Value;
                sb.AppendLine($"{kvp.Key,4} {packing.SphereCount,8} {packing.MaxSphereRadius,12:F3} " +
                    $"{packing.EffectiveDiameter,12:F3} {packing.PackingEfficiency,12:F3} " +
                    $"{packing.UsableVolume,12:F2} {packing.ChannelInfo,12}");
            }
            sb.AppendLine();

            var largestPacking = results.SpherePacking.Values.OrderByDescending(p => p.MaxSphereRadius).FirstOrDefault();
            if (largestPacking != null && largestPacking.Spheres.Count > 0)
            {
                sb.AppendLine($"Detailed sphere positions for cavity #{largestPacking.CavityId}:");
                sb.AppendLine($"  Total spheres: {largestPacking.SphereCount}");
                sb.AppendLine($"  Max radius: {largestPacking.MaxSphereRadius:F3} Å");
                sb.AppendLine($"  Effective diameter: {largestPacking.EffectiveDiameter:F3} Å");
                sb.AppendLine($"  Packing efficiency: {largestPacking.PackingEfficiency:F3}");
                sb.AppendLine($"  Usable volume: {largestPacking.UsableVolume:F2} Å³");
                sb.AppendLine();

                sb.AppendLine($"  {"Idx",4} {"Radius(Å)",12} {"Frac X",10} {"Frac Y",10} {"Frac Z",10} {"On Wall",10} {"Dist to center",16}");
                sb.AppendLine(new string('-', 80));

                foreach (var sphere in largestPacking.Spheres.OrderBy(s => s.Index))
                {
                    string onWall = sphere.IsOnWall ? "YES" : "NO";
                    sb.AppendLine($"  {sphere.Index,4} {sphere.Radius,12:F3} {sphere.FracX,10:F3} " +
                        $"{sphere.FracY,10:F3} {sphere.FracZ,10:F3} {onWall,10} {sphere.DistanceToCenter,16:F3}");
                }
                sb.AppendLine();
            }
        }

        private static void AppendGasCompatibility(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("GAS COMPATIBILITY");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            if (results.SpherePacking.Count == 0)
            {
                sb.AppendLine("No gas compatibility data available.");
                sb.AppendLine();
                return;
            }

            var packing = results.SpherePacking.Values.OrderByDescending(p => p.MaxSphereRadius).FirstOrDefault();
            if (packing == null || packing.GasCompatibility.Count == 0)
            {
                sb.AppendLine("No gas compatibility data available.");
                sb.AppendLine();
                return;
            }

            var goodGases = packing.GasCompatibility.Where(g => g.CanFit && g.Score >= 0.5f).ToList();
            var bestGases = packing.GasCompatibility.Where(g => g.CanFit).OrderByDescending(g => g.Score).Take(5).ToList();

            sb.AppendLine($"Cavity #{packing.CavityId} - Gas compatibility summary:");
            sb.AppendLine($"  Compatible gases: {goodGases.Count}/{packing.GasCompatibility.Count}");
            sb.AppendLine();

            sb.AppendLine($"  {"Gas",14} {"Formula",8} {"Diameter(Å)",12} {"Score",10} {"Status",10} {"Count",10}");
            sb.AppendLine(new string('-', 70));

            foreach (var gas in bestGases)
            {
                sb.AppendLine($"  {gas.Name,14} {gas.Formula,8} {gas.KineticDiameter,12:F3} " +
                    $"{gas.Score,10:F3} {gas.Status,10} {gas.RecommendedCount,10:F0}");
            }
            sb.AppendLine();

            var bestGas = packing.GasCompatibility.Where(g => g.CanFit).OrderByDescending(g => g.Score).FirstOrDefault();
            if (bestGas != null)
            {
                sb.AppendLine($"Best gas: {bestGas.Name} ({bestGas.Formula})");
                sb.AppendLine($"  Kinetic diameter: {bestGas.KineticDiameter:F3} Å");
                sb.AppendLine($"  Score: {bestGas.Score:F3}");
                sb.AppendLine($"  Status: {bestGas.Status}");
                if (bestGas.BestSphereLocation != "none")
                    sb.AppendLine($"  Location: {bestGas.BestSphereLocation}");
                sb.AppendLine($"  Max molecules: {bestGas.RecommendedCount:F0}");
                sb.AppendLine($"  Diffusion limited: {(bestGas.IsDiffusionLimited ? "YES" : "NO")}");
                sb.AppendLine();
            }

            var hydrogen = packing.GasCompatibility.FirstOrDefault(g => g.Name == "Hydrogen");
            var helium = packing.GasCompatibility.FirstOrDefault(g => g.Name == "Helium");

            if (hydrogen != null && helium != null)
            {
                sb.AppendLine("Hydrogen/Helium storage potential:");
                sb.AppendLine($"  H₂: Can fit: {hydrogen.CanFit}, Max count: {hydrogen.RecommendedCount:F0}, Score: {hydrogen.Score:F3}");
                sb.AppendLine($"  He: Can fit: {helium.CanFit}, Max count: {helium.RecommendedCount:F0}, Score: {helium.Score:F3}");
                sb.AppendLine();
            }
        }

        private static void AppendSolventCompatibility(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("SOLVENT COMPATIBILITY (with voxel cluster analysis)");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            if (results.SpherePacking.Count == 0)
            {
                sb.AppendLine("No solvent compatibility data available.");
                sb.AppendLine();
                return;
            }

            var packing = results.SpherePacking.Values.OrderByDescending(p => p.MaxSphereRadius).FirstOrDefault();
            if (packing == null || packing.SolventCompatibility.Count == 0)
            {
                sb.AppendLine("No solvent compatibility data available.");
                sb.AppendLine();
                return;
            }

            var bestSolvents = packing.SolventCompatibility.Where(s => s.CanFit).OrderByDescending(s => s.Score).Take(8).ToList();
            var goodSolvents = packing.SolventCompatibility.Where(s => s.CanFit && s.Score >= 0.6f).ToList();

            sb.AppendLine($"Cavity #{packing.CavityId} - Solvent compatibility summary:");
            sb.AppendLine($"  Compatible solvents: {goodSolvents.Count}/{packing.SolventCompatibility.Count}");
            sb.AppendLine();

            sb.AppendLine($"  {"Solvent",14} {"Formula",10} {"Volume(Å³)",12} {"Score",10} {"Status",10} {"Voxel Match",14}");
            sb.AppendLine(new string('-', 80));

            foreach (var solvent in bestSolvents)
            {
                sb.AppendLine($"  {solvent.Name,14} {solvent.Formula,10} {solvent.Volume,12:F0} " +
                    $"{solvent.Score,10:F3} {solvent.Status,10} {solvent.VoxelClusterMatch,14:F3}");
            }
            sb.AppendLine();

            var bestSolvent = packing.SolventCompatibility.Where(s => s.CanFit).OrderByDescending(s => s.Score).FirstOrDefault();
            if (bestSolvent != null)
            {
                sb.AppendLine($"Best solvent: {bestSolvent.Name} ({bestSolvent.Formula})");
                sb.AppendLine($"  Volume: {bestSolvent.Volume:F0} Å³");
                sb.AppendLine($"  Diameter: {bestSolvent.Diameter:F2} Å");
                sb.AppendLine($"  Score: {bestSolvent.Score:F3}");
                sb.AppendLine($"  Status: {bestSolvent.Status}");
                sb.AppendLine($"  Polarizability match: {bestSolvent.PolarizabilityMatch:F3}");
                sb.AppendLine($"  Charge compatibility: {bestSolvent.ChargeCompatibility:F3}");
                sb.AppendLine($"  Hydrophobicity match: {bestSolvent.HydrophobicityMatch:F3}");
                sb.AppendLine($"  Voxel cluster match: {bestSolvent.VoxelClusterMatch:F3}");
                sb.AppendLine($"  Max molecules: {bestSolvent.RecommendedCount:F0}");
                if (bestSolvent.BestSphereLocation != "none")
                    sb.AppendLine($"  Location: {bestSolvent.BestSphereLocation}");
                sb.AppendLine();
            }
        }

        private static void AppendElectrostaticSummary(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("ELECTROSTATIC & HYDROPHOBIC SUMMARY");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            sb.AppendLine($"{"Scale",6} {"Mean_Q",10} {"Var_Q",10} {"Mean_H",10} {"Var_H",10} {"Mean_Z",10} {"|Dipole|",12} {"Surf(Å²)",12}");
            sb.AppendLine(new string('-', 90));

            foreach (var scale in results.VdWScales)
            {
                float meanQ = 0f, varQ = 0f, meanH = 0f, varH = 0f, meanZ = 0f, totalDip = 0f, totalS = 0f;
                int count = 0;

                foreach (var kvp in results.CavityResults)
                {
                    var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - scale) < 0.001f);
                    if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                    {
                        meanQ += r.MeanCharge;
                        varQ += r.ChargeVariance;
                        meanH += r.MeanHydrophobicity;
                        varH += r.HydrophobicityVariance;
                        meanZ += r.MeanZEffective;
                        totalDip += r.DipoleMomentMagnitude;
                        totalS += r.SurfaceArea;
                        count++;
                    }
                }

                if (count > 0)
                {
                    meanQ /= count; varQ /= count; meanH /= count;
                    varH /= count; meanZ /= count;
                }

                sb.AppendLine($"{scale,6:F2} {meanQ,10:F4} {varQ,10:F4} {meanH,10:F4} {varH,10:F4} {meanZ,10:F2} {totalDip,12:F4} {totalS,12:F4}");
            }
            sb.AppendLine();
        }

        private static void AppendCriticalEvents(StringBuilder sb, VdWAnalysisResults results)
        {
            if (results.CriticalEvents.Count == 0) return;
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("CRITICAL EVENTS");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();
            foreach (var evt in results.CriticalEvents.OrderBy(e => e.Scale))
                sb.AppendLine($"Scale {evt.Scale:F2}: {evt.Description}");
            sb.AppendLine();
        }

        private static void AppendCavityTracking(StringBuilder sb, VdWAnalysisResults results)
        {
            if (results.CavityTracks.Count == 0) return;
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("CAVITY SURVIVAL TRACKING (Volume & Surface Area)");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            sb.AppendLine($"{"ID",4} {"Vol@1.0",12} {"Surf@1.0",12} {"Vol@1.1",12} {"Surf@1.1",12} {"Vol@1.2",12} {"Surf@1.2",12} " +
                          $"{"Vol@1.3",12} {"Surf@1.3",12} {"Vol@1.4",12} {"Surf@1.4",12} {"Vol@1.5",12} {"Surf@1.5",12} {"Disappears",12}");
            sb.AppendLine(new string('-', 175));

            foreach (var track in results.CavityTracks.Values.OrderBy(t => t.Id))
            {
                float surf1_0 = GetSurfaceAtScale(results, track.Id, 1.0f);
                float surf1_1 = GetSurfaceAtScale(results, track.Id, 1.1f);
                float surf1_2 = GetSurfaceAtScale(results, track.Id, 1.2f);
                float surf1_3 = GetSurfaceAtScale(results, track.Id, 1.3f);
                float surf1_4 = GetSurfaceAtScale(results, track.Id, 1.4f);
                float surf1_5 = GetSurfaceAtScale(results, track.Id, 1.5f);

                sb.AppendLine($"{track.Id,4} " +
                    $"{track.VolumeAt1_0,12:F4} {surf1_0,12:F4} " +
                    $"{track.VolumeAt1_1,12:F4} {surf1_1,12:F4} " +
                    $"{track.VolumeAt1_2,12:F4} {surf1_2,12:F4} " +
                    $"{track.VolumeAt1_3,12:F4} {surf1_3,12:F4} " +
                    $"{track.VolumeAt1_4,12:F4} {surf1_4,12:F4} " +
                    $"{track.VolumeAt1_5,12:F4} {surf1_5,12:F4} " +
                    $"{(track.Disappears ? track.DisappearanceScale.ToString("F2") : ">2.0"),12}");
            }
            sb.AppendLine();
        }

        private static void AppendChannelAnalysis(StringBuilder sb, VdWAnalysisResults results)
        {
            if (results.CrystalChannelData == null || results.CrystalChannelData.Count == 0)
            {
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine("CRYSTAL CHANNELS (SEMI-PERIODIC)");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine();
                sb.AppendLine("No crystal channels detected.");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("CRYSTAL CHANNELS (SEMI-PERIODIC)");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            sb.AppendLine("CHANNEL SUMMARY");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            sb.AppendLine($"{"Cav",4} {"Dir",4} {"Length(Å)",8} {"AvgW(Å)",8} {"MinW(Å)",8} {"MaxW(Å)",8} {"EffW(Å)",8} {"Hel(°)",8} {"Cross",6}");
            sb.AppendLine(new string('-', 80));

            foreach (var kvp in results.CrystalChannelData.OrderBy(k => k.Key))
            {
                int cavityId = kvp.Key;
                var channels = kvp.Value;

                foreach (var ch in channels.OrderBy(k => k.Key))
                {
                    string channel = ch.Key;
                    var result = ch.Value;
                    if (!result.IsConnected) continue;

                    sb.AppendLine($"{cavityId,4} {channel,4} " +
                        $"{result.TotalLength,8:F2} " +
                        $"{result.AverageWidth,8:F2} " +
                        $"{result.MinWidth,8:F2} " +
                        $"{result.MaxWidth,8:F2} " +
                        $"{result.EffectiveDiameter,8:F2} " +
                        $"{result.Helicity,8:F2} " +
                        $"{result.CellCrossings,6}");
                }
            }
            sb.AppendLine();

            sb.AppendLine("DETAILED CHANNEL INFORMATION");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            var channelStats = new Dictionary<string, int>();
            var helicityStats = new Dictionary<string, List<float>>();
            var widthStats = new Dictionary<string, List<float>>();
            var diameterStats = new Dictionary<string, List<float>>();

            foreach (var kvp in results.CrystalChannelData.OrderBy(k => k.Key))
            {
                int cavityId = kvp.Key;
                var channels = kvp.Value;

                sb.AppendLine($"Cavity #{cavityId}:");

                foreach (var ch in channels.OrderBy(k => k.Key))
                {
                    string channel = ch.Key;
                    var result = ch.Value;

                    if (!result.IsConnected)
                    {
                        sb.AppendLine($"  {channel}: NOT CONNECTED");
                        continue;
                    }

                    if (!channelStats.ContainsKey(channel))
                    {
                        channelStats[channel] = 0;
                        helicityStats[channel] = new List<float>();
                        widthStats[channel] = new List<float>();
                        diameterStats[channel] = new List<float>();
                    }
                    channelStats[channel]++;
                    helicityStats[channel].Add(result.Helicity);
                    widthStats[channel].Add(result.AverageWidth);
                    diameterStats[channel].Add(result.EffectiveDiameter);

                    string helicityDesc = GetHelicityDescription(result.Helicity);
                    string shapeDesc = GetChannelShapeDescription(result);

                    sb.AppendLine($"  {channel}:");
                    sb.AppendLine($"    Length: {result.TotalLength:F2} Å");
                    sb.AppendLine($"    Width: avg={result.AverageWidth:F2} Å, min={result.MinWidth:F2} Å, max={result.MaxWidth:F2} Å");
                    sb.AppendLine($"    Effective diameter: {result.EffectiveDiameter:F2} Å");
                    sb.AppendLine($"    Helicity: {result.Helicity:F2}° ({helicityDesc})");
                    sb.AppendLine($"    Shape: {shapeDesc}");
                    sb.AppendLine($"    Path points: {result.PathPoints}");
                    sb.AppendLine($"    Cell crossings: {result.CellCrossings}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("CHANNEL STATISTICS");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            sb.AppendLine($"{"Direction",6} {"Count",6} {"Avg Hel(°)",10} {"Min Hel(°)",10} {"Max Hel(°)",10} {"Avg W(Å)",10} {"Avg EffD(Å)",10}");
            sb.AppendLine(new string('-', 80));

            foreach (var stat in channelStats.OrderBy(k => k.Key))
            {
                string channel = stat.Key;
                int count = stat.Value;

                string avgHel = "N/A", minHel = "N/A", maxHel = "N/A";
                string avgW = "N/A", avgEffD = "N/A";

                if (helicityStats.TryGetValue(channel, out var helicities) && helicities.Count > 0)
                {
                    avgHel = helicities.Average().ToString("F2");
                    minHel = helicities.Min().ToString("F2");
                    maxHel = helicities.Max().ToString("F2");
                }

                if (widthStats.TryGetValue(channel, out var widths) && widths.Count > 0)
                    avgW = widths.Average().ToString("F2");

                if (diameterStats.TryGetValue(channel, out var diameters) && diameters.Count > 0)
                    avgEffD = diameters.Average().ToString("F2");

                sb.AppendLine($"{channel,6} {count,6} {avgHel,10} {minHel,10} {maxHel,10} {avgW,10} {avgEffD,10}");
            }
            sb.AppendLine();

            sb.AppendLine("CHANNEL NETWORK SUMMARY");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            bool hasX = channelStats.ContainsKey("XX");
            bool hasY = channelStats.ContainsKey("YY");
            bool hasZ = channelStats.ContainsKey("ZZ");

            int totalChannels = channelStats.Values.Sum();
            sb.AppendLine($"Total crystal channels: {totalChannels}");
            sb.AppendLine($"  XX: {(hasX ? channelStats["XX"].ToString() : "0")}");
            sb.AppendLine($"  YY: {(hasY ? channelStats["YY"].ToString() : "0")}");
            sb.AppendLine($"  ZZ: {(hasZ ? channelStats["ZZ"].ToString() : "0")}");

            if (hasX && hasY && hasZ)
                sb.AppendLine("3D CHANNEL NETWORK: Channels in all three directions!");
            else if ((hasX && hasY) || (hasX && hasZ) || (hasY && hasZ))
            {
                string dirs = hasX && hasY ? "X and Y" : (hasX && hasZ ? "X and Z" : "Y and Z");
                sb.AppendLine($"2D CHANNEL NETWORK: Channels in {dirs} directions.");
            }
            else if (hasX || hasY || hasZ)
            {
                string dir = hasX ? "X" : (hasY ? "Y" : "Z");
                sb.AppendLine($"1D CHANNEL NETWORK: Only {dir}-direction channels.");
            }
            else
                sb.AppendLine("No channel network detected.");

            sb.AppendLine();
        }

        private static void AppendFooter(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine($"Generated: {DateTime.Now}");
            sb.AppendLine("=".PadRight(80, '='));
        }

        #endregion

        #region HTML Sections

        private static void AppendHtmlHeader(StringBuilder sb, VdWAnalysisResults results)
        {
 // ⭐ CELL PARAMETERS
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>📐 Cell Parameters</h2>");
            sb.AppendLine("<div class='info-grid'>");
            sb.AppendLine($"<div><span class='label'>Volume:</span> <span class='value'>{results.CellVolume:F4} Å³</span></div>");
            sb.AppendLine($"<div><span class='label'>Voxel volume:</span> <span class='value'>{CellParameters.GetVoxelVolume():F6} Å³</span></div>");
            sb.AppendLine($"<div><span class='label'>Atoms in cell:</span> <span class='value'>{results.AtomCount}</span></div>");
            sb.AppendLine($"<div><span class='label'>Cell mass:</span> <span class='value'>{results.CellMass:F2} u</span></div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

 // ⭐ ANALYSIS PARAMETERS
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>⚙️ Analysis Parameters</h2>");
            sb.AppendLine("<div class='info-grid'>");
            sb.AppendLine($"<div><span class='label'>Resolution:</span> <span class='value'>{Flags.Instance.Resolution:F3} Å</span></div>");
            sb.AppendLine($"<div><span class='label'>Anisotropic:</span> <span class='value'>{GetStatusBadge(Flags.Instance.UseThermalMotion)}</span></div>");
            sb.AppendLine("<div><span class='label'>SDF:</span> <span class='value'>computed directly for each VdW scale</span></div>");
            sb.AppendLine($"<div><span class='label'>Scales tested:</span> <span class='value'>{string.Join(", ", results.VdWScales)}</span></div>");
            sb.AppendLine($"<div><span class='label'>Scale range:</span> <span class='value'>{results.VdWScales.Min():F2} - {results.VdWScales.Max():F2}</span></div>");
            sb.AppendLine($"<div><span class='label'>Analysis time:</span> <span class='value'>{results.TotalProcessingTimeMs / 1000:F2} s</span></div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

 // ⭐ SUMMARY
            int totalCavities = 0;
            int totalChannels = results.GetTotalChannelCount();
            int cavitiesWithChannels = results.GetCavitiesWithChannelsCount();
            float totalVolume = 0f;
            float totalSurface = 0f;

            foreach (var kvp in results.CavityResults)
            {
                var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - 1.0f) < 0.001f);
                if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                {
                    totalCavities++;
                    totalVolume += r.Volume;
                    totalSurface += r.SurfaceArea;
                }
            }

            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>📈 Summary (VdW=1.0)</h2>");
            sb.AppendLine("<div class='info-grid'>");
            sb.AppendLine($"<div><span class='label'>Total cavities:</span> <span class='value'>{totalCavities}</span></div>");
            sb.AppendLine($"<div><span class='label'>Total volume:</span> <span class='value'>{totalVolume:F2} Å³</span></div>");
            sb.AppendLine($"<div><span class='label'>Total surface:</span> <span class='value'>{totalSurface:F2} Å²</span></div>");
            sb.AppendLine($"<div><span class='label'>Volume/Surface:</span> <span class='value'>{(totalSurface > 0 ? totalVolume / totalSurface : 0):F3}</span></div>");
            sb.AppendLine($"<div><span class='label'>Cavities with channels:</span> <span class='value'>{cavitiesWithChannels}</span></div>");
            sb.AppendLine($"<div><span class='label'>Total channels:</span> <span class='value'>{totalChannels}</span></div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

 // Largest cavity
            var largest = results.CavityResults
                .SelectMany(kvp => kvp.Value)
                .Where(r => r != null && r.Volume > 0 && !r.IsPeriodicDuplicate && Math.Abs(r.VdWScale - 1.0f) < 0.001f)
                .OrderByDescending(r => r.Volume)
                .FirstOrDefault();

            if (largest != null)
            {
                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<h2>🏆 Largest Cavity (VdW=1.0)</h2>");
                sb.AppendLine("<div class='info-grid'>");
                sb.AppendLine($"<div><span class='label'>ID:</span> <span class='value'>#{largest.CavityId}</span></div>");
                sb.AppendLine($"<div><span class='label'>Volume:</span> <span class='value'>{largest.Volume:F2} Å³</span></div>");
                sb.AppendLine($"<div><span class='label'>Surface:</span> <span class='value'>{largest.SurfaceArea:F2} Å²</span></div>");
                sb.AppendLine($"<div><span class='label'>Sphericity:</span> <span class='value'>{largest.Sphericity:F3}</span></div>");
                sb.AppendLine($"<div><span class='label'>Walls:</span> <span class='value'>{largest.WallConnections}</span></div>");
                sb.AppendLine($"<div><span class='label'>Voxels:</span> <span class='value'>{largest.VoxelCount}</span></div>");
                sb.AppendLine($"<div><span class='label'>Triangles:</span> <span class='value'>{largest.TriangleCount}</span></div>");
                sb.AppendLine("</div>");
                sb.AppendLine("</div>");
            }

 // Largest sphere
            if (results.SpherePacking.Count > 0)
            {
                var bestPacking = results.SpherePacking.Values
                    .OrderByDescending(p => p.MaxSphereRadius)
                    .FirstOrDefault();

                if (bestPacking != null && bestPacking.MaxSphereRadius > 0)
                {
                    sb.AppendLine("<div class='section'>");
                    sb.AppendLine("<h2>⚪ Largest Sphere Fit</h2>");
                    sb.AppendLine("<div class='info-grid'>");
                    sb.AppendLine($"<div><span class='label'>Cavity ID:</span> <span class='value'>#{bestPacking.CavityId}</span></div>");
                    sb.AppendLine($"<div><span class='label'>Radius:</span> <span class='value'>{bestPacking.MaxSphereRadius:F3} Å</span></div>");
                    sb.AppendLine($"<div><span class='label'>Diameter:</span> <span class='value'>{bestPacking.EffectiveDiameter:F3} Å</span></div>");
                    sb.AppendLine($"<div><span class='label'>Spheres:</span> <span class='value'>{bestPacking.SphereCount}</span></div>");
                    sb.AppendLine($"<div><span class='label'>Packing efficiency:</span> <span class='value'>{bestPacking.PackingEfficiency:F3}</span></div>");
                    sb.AppendLine($"<div><span class='label'>Usable volume:</span> <span class='value'>{bestPacking.UsableVolume:F2} Å³</span></div>");
                    sb.AppendLine($"<div><span class='label'>Channels:</span> <span class='value'>{(bestPacking.HasChannels ? bestPacking.ChannelInfo : "none")}</span></div>");
                    sb.AppendLine("</div>");
                    sb.AppendLine("</div>");
                }
            }
        }

        private static void AppendHtmlQSPR(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>🧮 QSPR DESCRIPTORS</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Descriptor</th><th>Value</th><th>Description</th><th>Interpretation</th></tr>");

 // I_frag
            sb.AppendLine($"<tr><td><strong>I_frag</strong></td>" +
                $"<td>{results.FragmentationIndex:F6}</td>" +
                $"<td>Fragmentation Index</td>" +
                $"<td style='font-size:11px;color:#9cdcfe;'>N_cavities (≥ 1 Å³, VdW≈1.0) / N_atoms in cell<br/>Higher → more fragmented</td></tr>");

 // f_collapse
            string collapseColor = results.CollapseScale >= 1.5 ? "#6a9955" : results.CollapseScale >= 1.2 ? "#dcdcaa" : "#f44747";
            sb.AppendLine($"<tr><td><strong>f_collapse</strong></td>" +
                $"<td style='color:{collapseColor};'>{results.CollapseScale:F2}</td>" +
                $"<td>Critical Collapse</td>" +
                $"<td style='font-size:11px;color:#9cdcfe;'>Smallest VdW scale at which total cavity volume &lt; 0.1% of cell<br/>Higher → more stable</td></tr>");

 // Ω_Q
            string qColor = results.ElectrostaticFrustration > 0.5 ? "#dcdcaa" : "#6a9955";
            sb.AppendLine($"<tr><td><strong>Ω_Q</strong></td>" +
                $"<td style='color:{qColor};'>{results.ElectrostaticFrustration:F6}</td>" +
                $"<td>Electrostatic Frustration</td>" +
                $"<td style='font-size:11px;color:#9cdcfe;'>Charge inhomogeneity on surfaces<br/>Higher → more heterogeneous</td></tr>");

 // Ψ_H
            string hColor = results.HydrophobicFrustration > 0.5 ? "#dcdcaa" : "#6a9955";
            sb.AppendLine($"<tr><td><strong>Ψ_H</strong></td>" +
                $"<td style='color:{hColor};'>{results.HydrophobicFrustration:F6}</td>" +
                $"<td>Hydrophobic Frustration</td>" +
                $"<td style='font-size:11px;color:#9cdcfe;'>Hydrophobicity inhomogeneity<br/>Higher → more selective</td></tr>");

 // ⟨Z_cav⟩
            sb.AppendLine($"<tr><td><strong>⟨Z_cav⟩</strong></td>" +
                $"<td>{results.MeanPolarizability:F4}</td>" +
                $"<td>Mean Polarizability</td>" +
                $"<td style='font-size:11px;color:#9cdcfe;'>Polarizability capacity<br/>Higher → stronger dispersion interactions</td></tr>");

 // α_afterlar
            sb.AppendLine($"<tr><td><strong>α_polar</strong></td>" +
                $"<td>{results.MaxDipoleSensitivity:F6}</td>" +
                $"<td>Max Dipole Sensitivity</td>" +
                $"<td style='font-size:11px;color:#9cdcfe;'>Response to electric field<br/>Important for polar guest separation</td></tr>");

            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
        }

        private static void AppendHtmlScaleSummary(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>📊 SCALE SUMMARY</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Scale</th><th>Cav</th><th>Vol(Å³)</th><th>Surf(Å²)</th><th>V/S</th><th>Vol/Voxels</th><th>Vol/Atom</th><th>Channels</th></tr>");

            float voxelVolume = CellParameters.GetVoxelVolume();

            foreach (var scale in results.VdWScales)
            {
                int count = 0;
                float totalV = 0f;
                float totalS = 0f;
                int channels = 0;

                foreach (var kvp in results.CavityResults)
                {
                    var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - scale) < 0.001f);
                    if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                    {
                        count++;
                        totalV += r.Volume;
                        totalS += r.SurfaceArea;
                        if (results.CrystalChannelData.ContainsKey(r.CavityId) &&
                            results.CrystalChannelData[r.CavityId].Count > 0)
                            channels++;
                    }
                }

                float volPerVoxel = voxelVolume > 0 ? totalV / voxelVolume : 0f;
                float volPerAtom = results.AtomCount > 0 ? totalV / results.AtomCount : 0f;
                float vOverS = totalS > 0 ? totalV / totalS : 0f;

                sb.AppendLine($"<tr><td><strong>{scale:F2}</strong></td>" +
                    $"<td>{count}</td>" +
                    $"<td>{totalV:F4}</td>" +
                    $"<td>{totalS:F4}</td>" +
                    $"<td>{vOverS:F3}</td>" +
                    $"<td>{volPerVoxel:F0}</td>" +
                    $"<td>{volPerAtom:F6}</td>" +
                    $"<td>{channels}</td></tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
        }

        private static void AppendHtmlScaleDependence(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>📈 VDW SCALE DEPENDENCE</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Scale</th><th>Vol(Å³)</th><th>Surf(Å²)</th><th>dV/dscale</th><th>dS/dscale</th><th>ΔV(%)</th><th>ΔS(%)</th><th>V/S</th></tr>");

            float prevV = 0f, prevS = 0f;
            float prevScale = 0f;
            bool first = true;

            var scaleData = new List<(float scale, float vol, float surf)>();
            foreach (var scale in results.VdWScales.OrderBy(s => s))
            {
                float totalV = 0f, totalS = 0f;
                foreach (var kvp in results.CavityResults)
                {
                    var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - scale) < 0.001f);
                    if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                    {
                        totalV += r.Volume;
                        totalS += r.SurfaceArea;
                    }
                }
                scaleData.Add((scale, totalV, totalS));
            }

            foreach (var data in scaleData)
            {
                float dV = 0f, dS = 0f;
                float dVpercent = 0f, dSpercent = 0f;
                float dVdScale = 0f, dSdScale = 0f;
                float vOverS = data.surf > 0 ? data.vol / data.surf : 0f;

                if (!first && prevScale > 0)
                {
                    float deltaScale = data.scale - prevScale;
                    if (deltaScale > 0.0001f)
                    {
                        dV = data.vol - prevV;
                        dS = data.surf - prevS;
                        dVpercent = prevV > 0 ? (dV / prevV) * 100f : 0f;
                        dSpercent = prevS > 0 ? (dS / prevS) * 100f : 0f;
                        dVdScale = dV / deltaScale;
                        dSdScale = dS / deltaScale;
                    }
                }

                string dVStr = first ? "-" : $"{dV:F4}";
                string dSStr = first ? "-" : $"{dS:F4}";
                string dVpStr = first ? "-" : $"{dVpercent:F2}";
                string dSpStr = first ? "-" : $"{dSpercent:F2}";
                string dVdStr = first ? "-" : $"{dVdScale:F4}";
                string dSdStr = first ? "-" : $"{dSdScale:F4}";

                sb.AppendLine($"<tr><td><strong>{data.scale:F2}</strong></td>" +
                    $"<td>{data.vol:F4}</td>" +
                    $"<td>{data.surf:F4}</td>" +
                    $"<td>{dVdStr}</td>" +
                    $"<td>{dSdStr}</td>" +
                    $"<td>{dVpStr}</td>" +
                    $"<td>{dSpStr}</td>" +
                    $"<td>{vOverS:F3}</td></tr>");

                prevV = data.vol;
                prevS = data.surf;
                prevScale = data.scale;
                first = false;
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
        }

        private static void AppendHtmlSpherePacking(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>⚪ SPHERE PACKING ANALYSIS</h2>");

            if (results.SpherePacking.Count == 0)
            {
                sb.AppendLine("<p>No sphere packing data available.</p>");
                sb.AppendLine("</div>");
                return;
            }

            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Cav</th><th>Spheres</th><th>Max R(Å)</th><th>Diameter(Å)</th><th>Packing</th><th>Vol(Å³)</th><th>Channels</th></tr>");

            foreach (var kvp in results.SpherePacking.OrderBy(k => k.Key))
            {
                var packing = kvp.Value;
                sb.AppendLine($"<tr><td>{kvp.Key}</td>" +
                    $"<td>{packing.SphereCount}</td>" +
                    $"<td>{packing.MaxSphereRadius:F3}</td>" +
                    $"<td>{packing.EffectiveDiameter:F3}</td>" +
                    $"<td>{packing.PackingEfficiency:F3}</td>" +
                    $"<td>{packing.UsableVolume:F2}</td>" +
                    $"<td>{packing.ChannelInfo}</td></tr>");
            }
            sb.AppendLine("</table>");

            var largestPacking = results.SpherePacking.Values.OrderByDescending(p => p.MaxSphereRadius).FirstOrDefault();
            if (largestPacking != null && largestPacking.Spheres.Count > 0)
            {
                sb.AppendLine($"<h3>Detailed sphere positions for cavity #{largestPacking.CavityId}</h3>");
                sb.AppendLine($"<p>Total spheres: {largestPacking.SphereCount}, Max radius: {largestPacking.MaxSphereRadius:F3} Å</p>");
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>Idx</th><th>Radius(Å)</th><th>Frac X</th><th>Frac Y</th><th>Frac Z</th><th>On Wall</th><th>Dist to center</th></tr>");

                foreach (var sphere in largestPacking.Spheres.OrderBy(s => s.Index))
                {
                    string onWall = sphere.IsOnWall ? "✅ YES" : "❌ NO";
                    sb.AppendLine($"<tr><td>{sphere.Index}</td>" +
                        $"<td>{sphere.Radius:F3}</td>" +
                        $"<td>{sphere.FracX:F3}</td>" +
                        $"<td>{sphere.FracY:F3}</td>" +
                        $"<td>{sphere.FracZ:F3}</td>" +
                        $"<td>{onWall}</td>" +
                        $"<td>{sphere.DistanceToCenter:F3}</td></tr>");
                }
                sb.AppendLine("</table>");
            }

            sb.AppendLine("</div>");
        }

        private static void AppendHtmlGasCompatibility(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>💨 GAS COMPATIBILITY</h2>");

            if (results.SpherePacking.Count == 0)
            {
                sb.AppendLine("<p>No gas compatibility data available.</p>");
                sb.AppendLine("</div>");
                return;
            }

            var packing = results.SpherePacking.Values.OrderByDescending(p => p.MaxSphereRadius).FirstOrDefault();
            if (packing == null || packing.GasCompatibility.Count == 0)
            {
                sb.AppendLine("<p>No gas compatibility data available.</p>");
                sb.AppendLine("</div>");
                return;
            }

            var goodGases = packing.GasCompatibility.Where(g => g.CanFit && g.Score >= 0.5f).ToList();
            var bestGases = packing.GasCompatibility.Where(g => g.CanFit).OrderByDescending(g => g.Score).Take(5).ToList();

            sb.AppendLine($"<p>Cavity #{packing.CavityId} - Compatible gases: {goodGases.Count}/{packing.GasCompatibility.Count}</p>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Gas</th><th>Formula</th><th>Diameter(Å)</th><th>Score</th><th>Status</th><th>Count</th></tr>");

            foreach (var gas in bestGases)
            {
                string statusClass = gas.Status == "GOOD" ? "status-good" : gas.Status == "FAIR" ? "status-fair" : "status-poor";
                sb.AppendLine($"<tr><td>{gas.Name}</td>" +
                    $"<td>{gas.Formula}</td>" +
                    $"<td>{gas.KineticDiameter:F3}</td>" +
                    $"<td>{gas.Score:F3}</td>" +
                    $"<td class='{statusClass}'>{gas.Status}</td>" +
                    $"<td>{gas.RecommendedCount:F0}</td></tr>");
            }
            sb.AppendLine("</table>");

            var bestGas = packing.GasCompatibility.Where(g => g.CanFit).OrderByDescending(g => g.Score).FirstOrDefault();
            if (bestGas != null)
            {
                sb.AppendLine($"<p><strong>Best gas:</strong> {bestGas.Name} ({bestGas.Formula}) - Score: {bestGas.Score:F3}, Status: {bestGas.Status}</p>");
                sb.AppendLine($"<p>Diffusion limited: {(bestGas.IsDiffusionLimited ? "⚠️ YES" : "✅ NO")}</p>");
            }

            var hydrogen = packing.GasCompatibility.FirstOrDefault(g => g.Name == "Hydrogen");
            var helium = packing.GasCompatibility.FirstOrDefault(g => g.Name == "Helium");

            if (hydrogen != null && helium != null)
            {
                sb.AppendLine("<h3>Hydrogen/Helium storage potential:</h3>");
                sb.AppendLine($"<p>H₂: Can fit: {(hydrogen.CanFit ? "✅ YES" : "❌ NO")}, Max count: {hydrogen.RecommendedCount:F0}, Score: {hydrogen.Score:F3}</p>");
                sb.AppendLine($"<p>He: Can fit: {(helium.CanFit ? "✅ YES" : "❌ NO")}, Max count: {helium.RecommendedCount:F0}, Score: {helium.Score:F3}</p>");
            }

            sb.AppendLine("</div>");
        }

        private static void AppendHtmlSolventCompatibility(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>🧪 SOLVENT COMPATIBILITY</h2>");

            if (results.SpherePacking.Count == 0)
            {
                sb.AppendLine("<p>No solvent compatibility data available.</p>");
                sb.AppendLine("</div>");
                return;
            }

            var packing = results.SpherePacking.Values.OrderByDescending(p => p.MaxSphereRadius).FirstOrDefault();
            if (packing == null || packing.SolventCompatibility.Count == 0)
            {
                sb.AppendLine("<p>No solvent compatibility data available.</p>");
                sb.AppendLine("</div>");
                return;
            }

            var bestSolvents = packing.SolventCompatibility.Where(s => s.CanFit).OrderByDescending(s => s.Score).Take(8).ToList();
            var goodSolvents = packing.SolventCompatibility.Where(s => s.CanFit && s.Score >= 0.6f).ToList();

            sb.AppendLine($"<p>Cavity #{packing.CavityId} - Compatible solvents: {goodSolvents.Count}/{packing.SolventCompatibility.Count}</p>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Solvent</th><th>Formula</th><th>Volume(Å³)</th><th>Score</th><th>Status</th><th>Voxel Match</th></tr>");

            foreach (var solvent in bestSolvents)
            {
                string statusClass = solvent.Status == "GOOD" ? "status-good" : solvent.Status == "FAIR" ? "status-fair" : "status-poor";
                sb.AppendLine($"<tr><td>{solvent.Name}</td>" +
                    $"<td>{solvent.Formula}</td>" +
                    $"<td>{solvent.Volume:F0}</td>" +
                    $"<td>{solvent.Score:F3}</td>" +
                    $"<td class='{statusClass}'>{solvent.Status}</td>" +
                    $"<td>{solvent.VoxelClusterMatch:F3}</td></tr>");
            }
            sb.AppendLine("</table>");

            var bestSolvent = packing.SolventCompatibility.Where(s => s.CanFit).OrderByDescending(s => s.Score).FirstOrDefault();
            if (bestSolvent != null)
            {
                sb.AppendLine($"<p><strong>Best solvent:</strong> {bestSolvent.Name} ({bestSolvent.Formula}) - Score: {bestSolvent.Score:F3}, Status: {bestSolvent.Status}</p>");
                sb.AppendLine($"<p>Max molecules: {bestSolvent.RecommendedCount:F0}</p>");
            }

            sb.AppendLine("</div>");
        }

        private static void AppendHtmlElectrostatic(StringBuilder sb, VdWAnalysisResults results)
        {
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>⚡ ELECTROSTATIC & HYDROPHOBIC SUMMARY</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Scale</th><th>Mean_Q</th><th>Var_Q</th><th>Mean_H</th><th>Var_H</th><th>Mean_Z</th><th>|Dipole|</th><th>Surf(Å²)</th></tr>");

            foreach (var scale in results.VdWScales)
            {
                float meanQ = 0f, varQ = 0f, meanH = 0f, varH = 0f, meanZ = 0f, totalDip = 0f, totalS = 0f;
                int count = 0;

                foreach (var kvp in results.CavityResults)
                {
                    var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - scale) < 0.001f);
                    if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                    {
                        meanQ += r.MeanCharge;
                        varQ += r.ChargeVariance;
                        meanH += r.MeanHydrophobicity;
                        varH += r.HydrophobicityVariance;
                        meanZ += r.MeanZEffective;
                        totalDip += r.DipoleMomentMagnitude;
                        totalS += r.SurfaceArea;
                        count++;
                    }
                }

                if (count > 0)
                {
                    meanQ /= count; varQ /= count; meanH /= count;
                    varH /= count; meanZ /= count;
                }

                sb.AppendLine($"<tr><td><strong>{scale:F2}</strong></td>" +
                    $"<td>{meanQ:F4}</td>" +
                    $"<td>{varQ:F4}</td>" +
                    $"<td>{meanH:F4}</td>" +
                    $"<td>{varH:F4}</td>" +
                    $"<td>{meanZ:F2}</td>" +
                    $"<td>{totalDip:F4}</td>" +
                    $"<td>{totalS:F4}</td></tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
        }

        private static void AppendHtmlCriticalEvents(StringBuilder sb, VdWAnalysisResults results)
        {
            if (results.CriticalEvents.Count == 0) return;

            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>⚠️ CRITICAL EVENTS</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Scale</th><th>Event</th></tr>");

            foreach (var evt in results.CriticalEvents.OrderBy(e => e.Scale))
            {
                string color = evt.Type == CriticalEvent.EventType.Disappearance ? "color: #f44747;" :
                               evt.Type == CriticalEvent.EventType.ChannelOpen ? "color: #6a9955;" :
                               evt.Type == CriticalEvent.EventType.ChannelClose ? "color: #dcdcaa;" : "";
                sb.AppendLine($"<tr><td><strong>{evt.Scale:F2}</strong></td><td style='{color}'>{evt.Description}</td></tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
        }

        private static void AppendHtmlCavityTracking(StringBuilder sb, VdWAnalysisResults results)
        {
            if (results.CavityTracks.Count == 0) return;

            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>📋 CAVITY SURVIVAL TRACKING</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>ID</th><th>Vol@1.0</th><th>Surf@1.0</th><th>Vol@1.1</th><th>Surf@1.1</th><th>Vol@1.2</th><th>Surf@1.2</th><th>Vol@1.3</th><th>Surf@1.3</th><th>Vol@1.4</th><th>Surf@1.4</th><th>Vol@1.5</th><th>Surf@1.5</th><th>Disappears</th></tr>");

            foreach (var track in results.CavityTracks.Values.OrderBy(t => t.Id))
            {
                float surf1_0 = GetSurfaceAtScale(results, track.Id, 1.0f);
                float surf1_1 = GetSurfaceAtScale(results, track.Id, 1.1f);
                float surf1_2 = GetSurfaceAtScale(results, track.Id, 1.2f);
                float surf1_3 = GetSurfaceAtScale(results, track.Id, 1.3f);
                float surf1_4 = GetSurfaceAtScale(results, track.Id, 1.4f);
                float surf1_5 = GetSurfaceAtScale(results, track.Id, 1.5f);

                string disappear = track.Disappears ? track.DisappearanceScale.ToString("F2") : ">2.0";
                string color = track.Disappears ? "color: #f44747;" : "color: #6a9955;";

                sb.AppendLine($"<tr><td><strong>{track.Id}</strong></td>" +
                    $"<td>{track.VolumeAt1_0:F4}</td><td>{surf1_0:F4}</td>" +
                    $"<td>{track.VolumeAt1_1:F4}</td><td>{surf1_1:F4}</td>" +
                    $"<td>{track.VolumeAt1_2:F4}</td><td>{surf1_2:F4}</td>" +
                    $"<td>{track.VolumeAt1_3:F4}</td><td>{surf1_3:F4}</td>" +
                    $"<td>{track.VolumeAt1_4:F4}</td><td>{surf1_4:F4}</td>" +
                    $"<td>{track.VolumeAt1_5:F4}</td><td>{surf1_5:F4}</td>" +
                    $"<td style='{color}'>{disappear}</td></tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");
        }

        private static void AppendHtmlChannels(StringBuilder sb, VdWAnalysisResults results)
        {
            if (results.CrystalChannelData == null || results.CrystalChannelData.Count == 0)
            {
                sb.AppendLine("<div class='section'>");
                sb.AppendLine("<h2>🔀 CRYSTAL CHANNELS</h2>");
                sb.AppendLine("<p>No crystal channels detected.</p>");
                sb.AppendLine("</div>");
                return;
            }

            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>🔀 CRYSTAL CHANNELS</h2>");

            sb.AppendLine("<h3>Channel Summary</h3>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Cav</th><th>Dir</th><th>Length(Å)</th><th>AvgW(Å)</th><th>MinW(Å)</th><th>MaxW(Å)</th><th>EffW(Å)</th><th>Hel(°)</th><th>Cross</th></tr>");

            foreach (var kvp in results.CrystalChannelData.OrderBy(k => k.Key))
            {
                int cavityId = kvp.Key;
                var channels = kvp.Value;

                foreach (var ch in channels.OrderBy(k => k.Key))
                {
                    string channel = ch.Key;
                    var result = ch.Value;
                    if (!result.IsConnected) continue;

                    sb.AppendLine($"<tr><td>{cavityId}</td>" +
                        $"<td><strong>{channel}</strong></td>" +
                        $"<td>{result.TotalLength:F2}</td>" +
                        $"<td>{result.AverageWidth:F2}</td>" +
                        $"<td>{result.MinWidth:F2}</td>" +
                        $"<td>{result.MaxWidth:F2}</td>" +
                        $"<td>{result.EffectiveDiameter:F2}</td>" +
                        $"<td>{result.Helicity:F2}</td>" +
                        $"<td>{result.CellCrossings}</td></tr>");
                }
            }
            sb.AppendLine("</table>");

            var channelStats = new Dictionary<string, int>();
            var helicityStats = new Dictionary<string, List<float>>();
            var widthStats = new Dictionary<string, List<float>>();
            var diameterStats = new Dictionary<string, List<float>>();

            foreach (var kvp in results.CrystalChannelData)
            {
                foreach (var ch in kvp.Value)
                {
                    string channel = ch.Key;
                    var result = ch.Value;
                    if (!result.IsConnected) continue;

                    if (!channelStats.ContainsKey(channel))
                    {
                        channelStats[channel] = 0;
                        helicityStats[channel] = new List<float>();
                        widthStats[channel] = new List<float>();
                        diameterStats[channel] = new List<float>();
                    }
                    channelStats[channel]++;
                    helicityStats[channel].Add(result.Helicity);
                    widthStats[channel].Add(result.AverageWidth);
                    diameterStats[channel].Add(result.EffectiveDiameter);
                }
            }

            sb.AppendLine("<h3>Channel Statistics</h3>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Direction</th><th>Count</th><th>Avg Hel(°)</th><th>Min Hel(°)</th><th>Max Hel(°)</th><th>Avg W(Å)</th><th>Avg EffD(Å)</th></tr>");

            foreach (var stat in channelStats.OrderBy(k => k.Key))
            {
                string channel = stat.Key;
                int count = stat.Value;

                string avgHel = "N/A", minHel = "N/A", maxHel = "N/A";
                string avgW = "N/A", avgEffD = "N/A";

                if (helicityStats.TryGetValue(channel, out var helicities) && helicities.Count > 0)
                {
                    avgHel = helicities.Average().ToString("F2");
                    minHel = helicities.Min().ToString("F2");
                    maxHel = helicities.Max().ToString("F2");
                }

                if (widthStats.TryGetValue(channel, out var widths) && widths.Count > 0)
                    avgW = widths.Average().ToString("F2");

                if (diameterStats.TryGetValue(channel, out var diameters) && diameters.Count > 0)
                    avgEffD = diameters.Average().ToString("F2");

                sb.AppendLine($"<tr><td><strong>{channel}</strong></td>" +
                    $"<td>{count}</td>" +
                    $"<td>{avgHel}</td>" +
                    $"<td>{minHel}</td>" +
                    $"<td>{maxHel}</td>" +
                    $"<td>{avgW}</td>" +
                    $"<td>{avgEffD}</td></tr>");
            }
            sb.AppendLine("</table>");

            bool hasX = channelStats.ContainsKey("XX");
            bool hasY = channelStats.ContainsKey("YY");
            bool hasZ = channelStats.ContainsKey("ZZ");

            sb.AppendLine("<h3>Channel Network</h3>");
            sb.AppendLine($"<p>Total channels: {channelStats.Values.Sum()}</p>");

            if (hasX && hasY && hasZ)
                sb.AppendLine("<p class='status-good'>✅ 3D CHANNEL NETWORK - Channels in all three directions!</p>");
            else if ((hasX && hasY) || (hasX && hasZ) || (hasY && hasZ))
            {
                string dirs = hasX && hasY ? "X and Y" : (hasX && hasZ ? "X and Z" : "Y and Z");
                sb.AppendLine($"<p class='status-fair'>⚠️ 2D CHANNEL NETWORK - Channels in {dirs} directions.</p>");
            }
            else if (hasX || hasY || hasZ)
            {
                string dir = hasX ? "X" : (hasY ? "Y" : "Z");
                sb.AppendLine($"<p class='status-poor'>⚠️ 1D CHANNEL NETWORK - Only {dir}-direction channels.</p>");
            }
            else
                sb.AppendLine("<p>No channel network detected.</p>");

            sb.AppendLine("</div>");
        }

        #endregion

        #region Helpers

        private static float GetSurfaceAtScale(VdWAnalysisResults results, int cavityId, float scale)
        {
            if (results.CavityResults.TryGetValue(cavityId, out var snapshots))
            {
                var r = snapshots.FirstOrDefault(x => Math.Abs(x.VdWScale - scale) < 0.001f);
                if (r != null) return r.SurfaceArea;
            }
            return 0f;
        }

        private static string GetHelicityDescription(float helicity)
        {
            if (helicity < 0.5f) return "STRAIGHT";
            if (helicity < 2.0f) return "SLIGHTLY TWISTED";
            if (helicity < 5.0f) return "MODERATELY HELICAL";
            if (helicity < 10.0f) return "STRONGLY HELICAL";
            return "HIGHLY HELICAL";
        }

        private static string GetChannelShapeDescription(CavityPathFinder.ChannelPathResult result)
        {
            if (result.AverageWidth <= 0) return "unknown shape";
            float aspectRatio = result.TotalLength / (result.AverageWidth * 2f);
            if (aspectRatio < 1.0f) return "short and wide";
            if (aspectRatio < 2.0f) return "slightly elongated";
            if (aspectRatio < 4.0f) return "moderately elongated";
            if (aspectRatio < 8.0f) return "highly elongated";
            return "extremely long and narrow (nanotube-like)";
        }

        private static string GetStatusBadge(bool value)
        {
            return value ? "<span class='status-good'>✅ YES</span>" : "<span class='status-poor'>❌ NO</span>";
        }

        private static string GetEmptyHtmlReport()
        {
            return @"<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'><title>VDW Analysis Report</title>
<style>body{font-family:'Consolas',monospace;background:#1e1e1e;color:#d4d4d4;padding:20px;}
h1{color:#4ec9b0;}</style>
</head>
<body>
<h1>No results to report</h1>
<p>Please run VdW analysis first.</p>
</body>
</html>";
        }

        #endregion

        #region CSS Styles

        private static string GetHtmlStyles()
        {
            return @"
body {
    font-family: 'Segoe UI', 'Consolas', monospace;
    background: #1e1e1e;
    color: #d4d4d4;
    padding: 20px;
    max-width: 1400px;
    margin: 0 auto;
}
h1 {
    color: #4ec9b0;
    font-size: 28px;
    border-bottom: 2px solid #4ec9b0;
    padding-bottom: 10px;
}
h2 {
    color: #569cd6;
    font-size: 22px;
    margin-top: 30px;
    border-bottom: 1px solid #333;
    padding-bottom: 5px;
}
h3 {
    color: #9cdcfe;
    font-size: 18px;
    margin-top: 20px;
}
.timestamp {
    color: #808080;
    font-size: 12px;
    margin-bottom: 20px;
}
.section {
    background: #252525;
    border-radius: 8px;
    padding: 15px 20px;
    margin-bottom: 20px;
    border: 1px solid #333;
}
.info-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 8px;
}
.info-grid .label {
    color: #9cdcfe;
    font-weight: bold;
}
.info-grid .value {
    color: #b5cea8;
}
table {
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;
    margin: 10px 0;
}
th {
    background: #2d2d2d;
    color: #4ec9b0;
    font-weight: bold;
    padding: 8px 10px;
    text-align: center;
    border: 1px solid #3d3d3d;
}
td {
    padding: 6px 10px;
    text-align: center;
    border: 1px solid #333;
}
tr:hover {
    background: #2a2a2a;
}
.status-good {
    color: #6a9955;
    font-weight: bold;
}
.status-fair {
    color: #dcdcaa;
    font-weight: bold;
}
.status-poor {
    color: #f44747;
    font-weight: bold;
}
hr {
    border: none;
    border-top: 1px solid #333;
    margin: 30px 0;
}
p {
    margin: 8px 0;
    line-height: 1.5;
}
";
        }

        #endregion
    }
}