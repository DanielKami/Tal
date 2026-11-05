// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: CavitySingleRunReafterrt.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Globalization;
using System.Text;

namespace Tal.Cavity
{
 /// <summary>
 /// Generates a complete single-run cavity detection reafterrt.
 /// Includes crystal data, grid info, per-cavity table, channels,
 /// sphere fits, gas/solvent compatibility, single-run QSPR, and refinement metadata.
 /// </summary>
    public static class CavitySingleRunReport
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string Generate(
            Molecule molecule,
            CavityDetector detector,
            List<Cavity> cavities,
            CavityHelpers helpers)
        {
            if (detector == null) return "No detector available.";

            var sb = new StringBuilder(64 * 1024);

            AppendHeader(sb, molecule, detector);
            AppendCrystalData(sb, molecule);
            AppendGridData(sb, detector, molecule);
            AppendCavitySummary(sb, cavities, molecule, helpers);
            AppendPerCavityTable(sb, cavities);
            AppendChannelSection(sb, cavities);
            AppendSphereFitSection(sb, cavities);
            AppendSingleRunQSPR(sb, cavities, molecule);
            AppendRefinementMetadata(sb, molecule);
            AppendFooter(sb);

            return sb.ToString();
        }

 // ═══════════════════════════════════════════════════════════════
 // 1. HEADER
 // ═══════════════════════════════════════════════════════════════

        private static void AppendHeader(StringBuilder sb, Molecule molecule, CavityDetector detector)
        {
            var flags = Flags.Instance;

            sb.AppendLine(new string('=', 80));
            sb.AppendLine("CAVITY DETECTION REPORT — SINGLE RUN");
            sb.AppendLine(new string('=', 80));
            sb.AppendLine();
            sb.AppendLine($"Generated:                 {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Structure:                 {molecule?.Name ?? "(unnamed)"}");
            sb.AppendLine($"Space group:               {molecule?.SpaceGroupName ?? "?"} (#{molecule?.SpaceGroupNumber ?? 0})");
            sb.AppendLine();
            sb.AppendLine("DETECTION PARAMETERS:");
            sb.AppendLine($"  Resolution:              {flags.Resolution:F3} Å");
            sb.AppendLine($"  VdW multiplier:          {flags.VdWValue:F3}");
            sb.AppendLine($"  Probe radius:            {detector.ProbeRadius:F3} Å");
            sb.AppendLine($"  Thermal motion (ADP):    {(flags.UseThermalMotion ? "YES (ellipsoids)" : "NO (spheres)")}");
            sb.AppendLine($"  Min cavity volume:       {CavityDetector.MinCavityVolume:F3} Å³");
            sb.AppendLine($"  Scan margin:             {CavityDetector.ScanMargin:F2} Å");
            sb.AppendLine($"  SDF base radius:         {CavityDetector.BASE_SDF_RADIUS:F2}");
            sb.AppendLine($"  Smoothing level:         {flags.smuth}");
            sb.AppendLine();
        }

 // ═══════════════════════════════════════════════════════════════
 // 2. CRYSTAL DATA
 // ═══════════════════════════════════════════════════════════════

        private static void AppendCrystalData(StringBuilder sb, Molecule molecule)
        {
            if (molecule == null) return;

            sb.AppendLine(new string('-', 80));
            sb.AppendLine("CRYSTAL DATA");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine();

            sb.AppendLine("Unit cell:");
            sb.AppendLine($"  a = {molecule.a,12:F4} Å      α = {RadToDeg(molecule.alpha),10:F4}°");
            sb.AppendLine($"  b = {molecule.b,12:F4} Å      β = {RadToDeg(molecule.beta),10:F4}°");
            sb.AppendLine($"  c = {molecule.c,12:F4} Å      γ = {RadToDeg(molecule.gamma),10:F4}°");
            sb.AppendLine($"  Volume = {molecule.Volume:F4} Å³");
            sb.AppendLine($"  System = {DetermineSystem(molecule)}");
            sb.AppendLine();

            sb.AppendLine("Symmetry and content:");
            sb.AppendLine($"  Space group:             {molecule.SpaceGroupName} (#{molecule.SpaceGroupNumber})");
            sb.AppendLine($"  Symmetry operations:     {molecule.SymmetryOperations?.Count ?? 1}");
            sb.AppendLine($"  Atoms in asymmetric:     {molecule.NumberOfAtoms}");
            sb.AppendLine($"  Atoms in cell:           {molecule.SymetryEquivalents?.Count ?? 0}");
            sb.AppendLine($"  Molecule count in cell:  {CountMolecules(molecule)}");
            sb.AppendLine();

            if (molecule.DensityDiffrn > 0)
            {
                sb.AppendLine("Physical properties:");
                sb.AppendLine($"  Density (calculated):    {molecule.DensityDiffrn:F4} g/cm³");
                sb.AppendLine($"  Formula:                 {molecule.Formula}");
                if (molecule.FormulaWeight > 0)
                    sb.AppendLine($"  Formula weight:          {molecule.FormulaWeight:F3} u");
                if (molecule.F000 > 0)
                    sb.AppendLine($"  F(000):                  {molecule.F000:F1}");
                sb.AppendLine();
            }

            sb.AppendLine("Chemical property ranges (used for property mapping):");
            sb.AppendLine($"  Charge:                  [{molecule.ChargeMin:F4}, {molecule.ChargeMax:F4}]");
            sb.AppendLine($"  Hydrophobicity:          [{molecule.HydrophobicityMin:F4}, {molecule.HydrophobicityMax:F4}]");
            sb.AppendLine();
        }

        private static string RadToDeg(float rad) => (rad * 180f / MathF.PI).ToString("F4", Inv);

        private static string DetermineSystem(Molecule m)
        {
            float a = m.a, b = m.b, c = m.c;
            float alpha = m.alpha, beta = m.beta, gamma = m.gamma;

            bool aEqB = Math.Abs(a - b) < 0.01f;
            bool bEqC = Math.Abs(b - c) < 0.01f;
            bool aEqC = Math.Abs(a - c) < 0.01f;
            bool all90 = Math.Abs(alpha - MathF.PI / 2) < 0.01f
                      && Math.Abs(beta - MathF.PI / 2) < 0.01f
                      && Math.Abs(gamma - MathF.PI / 2) < 0.01f;

            if (aEqB && bEqC && all90) return "cubic";
            if (aEqB && all90 && !bEqC) return "tetragonal";
            if (all90 && !aEqB && !bEqC && !aEqC) return "orthorhombic";
            if (aEqB && Math.Abs(gamma - 2 * MathF.PI / 3) < 0.01f) return "hexagonal";
            if (all90) return "orthorhombic";
            return "triclinic / monoclinic";
        }

        private static int CountMolecules(Molecule m)
        {
            if (m?.SymetryEquivalents == null) return 0;
            var seen = new HashSet<int>();
            foreach (var a in m.SymetryEquivalents)
                if (a.IndividualMoleculeInCell >= 0) seen.Add(a.IndividualMoleculeInCell);
            return seen.Count;
        }

 // ═══════════════════════════════════════════════════════════════
 // 3. GRID / SDF DATA
 // ═══════════════════════════════════════════════════════════════

        private static void AppendGridData(StringBuilder sb, CavityDetector detector, Molecule molecule)
        {
            sb.AppendLine(new string('-', 80));
            sb.AppendLine("GRID / SDF DATA");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine();

 // Rozmiar mesh
            int resX = detector.GridResX;
            int resY = detector.GridResY;
            int resZ = detector.GridResZ;
            long totalVoxels = detector.GridTotalVoxels;
            int margX = detector.MarginX;
            int margY = detector.MarginY;
            int margZ = detector.MarginZ;

            if (resX > 0 && resY > 0 && resZ > 0)
            {
                sb.AppendLine("Periodic cell grid (used for detection):");
                sb.AppendLine($"  ResX × ResY × ResZ:      {resX} × {resY} × {resZ}");
                sb.AppendLine($"  Total voxels:            {totalVoxels:N0}");
                sb.AppendLine($"  Margins (voxels):        {margX} / {margY} / {margZ}");
                sb.AppendLine();

                int superX = resX + 2 * margX;
                int superY = resY + 2 * margY;
                int superZ = resZ + 2 * margZ;
                long superTotal = (long)superX * superY * superZ;

                sb.AppendLine("Supercell grid (with margin, used for SDF):");
                sb.AppendLine($"  ResX × ResY × ResZ:      {superX} × {superY} × {superZ}");
                sb.AppendLine($"  Total voxels:            {superTotal:N0}");
                sb.AppendLine();

                double memMB = superTotal * 4.0 / (1024 * 1024);
                sb.AppendLine("Estimated memory (density only):");
                sb.AppendLine($"  {memMB:F2} MB  ({superTotal * 4:N0} bytes)");
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine("  Grid not prepared — run detection first.");
                sb.AppendLine();
            }

            float voxelVolume = CellParameters.IsInitialized ? CellParameters.GetVoxelVolume() : 0f;
            float voxelSize = detector.Scanner?.VoxelSize ?? 0f;

            sb.AppendLine("Voxel geometry:");
            sb.AppendLine($"  Voxel size:              {voxelSize:F4} Å");
            sb.AppendLine($"  Voxel volume:            {voxelVolume:F6} Å³");
            sb.AppendLine($"  Max SDF distance:        {detector.Scanner?.MaxDistFine ?? 0:F3} Å");
            sb.AppendLine($"  Non-orthogonal cell:     {(CellParameters.IsNonOrthogonal() ? "YES" : "NO")}");
            sb.AppendLine();
        }

 // ═══════════════════════════════════════════════════════════════
 // 4. CAVITY SUMMARY
 // ═══════════════════════════════════════════════════════════════

        private static void AppendCavitySummary(
            StringBuilder sb, List<Cavity> cavities, Molecule molecule, CavityHelpers helpers)
        {
            sb.AppendLine(new string('-', 80));
            sb.AppendLine("CAVITY SUMMARY");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine();

            if (cavities == null || cavities.Count == 0)
            {
                sb.AppendLine("  No cavities detected.");
                sb.AppendLine();
                return;
            }

            int primary = cavities.Count(c => !c.IsPeriodicDuplicate);
            int duplicate = cavities.Count(c => c.IsPeriodicDuplicate);
            int periodic = cavities.Count(c => c.WallConnections > 0);
            int closed = cavities.Count(c => c.WallConnections == 0);
            int withChannels = cavities.Count(c => c.HasCrystalChannels);
            int withMesh = cavities.Count(c => c.TrianglesGenerated);
            int withSpheres = cavities.Count(c => c.SphereFitCalculated
                                                && c.SphereFits != null
                                                && c.SphereFits.Count > 0);

            float totalVol = helpers?.GetTotalVoidsVolumeFromCavities(cavities)
                             ?? cavities.Where(c => !c.IsPeriodicDuplicate).Sum(c => c.Volume);
            float totalVolErr = helpers?.GetTotalVoidsVolumeErrorFromCavities(cavities) ?? 0f;
            float totalSurf = helpers?.GetTotalSurfaceAreaFromCavities(cavities)
                              ?? cavities.Where(c => !c.IsPeriodicDuplicate).Sum(c => c.SurfaceArea);
            float totalSurfErr = helpers?.GetTotalSurfaceAreaErrorFromCavities(cavities) ?? 0f;
            float cellVol = molecule?.Volume ?? 0f;
            float vsRatio = totalSurf > 0 ? totalVol / totalSurf : 0f;
            float fillPercent = cellVol > 0 ? 100f * totalVol / cellVol : 0f;

            sb.AppendLine($"  Total cavities:          {cavities.Count}");
            sb.AppendLine($"    primary:               {primary}");
            sb.AppendLine($"    periodic duplicates:   {duplicate}");
            sb.AppendLine($"    touching walls:        {periodic}");
            sb.AppendLine($"    closed (0 walls):      {closed}");
            sb.AppendLine($"    with channels:         {withChannels}");
            sb.AppendLine($"    with mesh:             {withMesh}");
            sb.AppendLine($"    with sphere fits:      {withSpheres}");
            sb.AppendLine();

            sb.AppendLine($"  Total void volume:       {totalVol:F4} Å³  (±{totalVolErr:F4})");
            sb.AppendLine($"  Total void surface:      {totalSurf:F4} Å²  (±{totalSurfErr:F4})");
            sb.AppendLine($"  Volume / surface:        {vsRatio:F4} Å");
            sb.AppendLine($"  Cell volume:             {cellVol:F4} Å³");
            sb.AppendLine($"  Void fill fraction:      {fillPercent:F4} %");
            sb.AppendLine();

            if (primary > 0)
            {
                var prim = cavities.Where(c => !c.IsPeriodicDuplicate).ToList();
                float minVol = prim.Min(c => c.Volume);
                float maxVol = prim.Max(c => c.Volume);
                float avgVol = prim.Average(c => c.Volume);
                float avgSph = prim.Average(c => c.Sphericity);
                float maxSphR = prim.Where(c => c.SphereFitCalculated)
                                    .Select(c => c.MaxSphereRadius)
                                    .DefaultIfEmpty(0f).Max();

                sb.AppendLine("  Statistics (primary cavities only):");
                sb.AppendLine($"    Volume:                min={minVol:F4}  avg={avgVol:F4}  max={maxVol:F4}  Å³");
                sb.AppendLine($"    Avg sphericity:        {avgSph:F4}");
                sb.AppendLine($"    Largest sphere radius: {maxSphR:F4} Å  (D = {2 * maxSphR:F4} Å)");
                sb.AppendLine();
            }
        }

 // ═══════════════════════════════════════════════════════════════
 // 5. PER-CAVITY TABLE
 // ═══════════════════════════════════════════════════════════════

        private static void AppendPerCavityTable(StringBuilder sb, List<Cavity> cavities)
        {
            sb.AppendLine(new string('-', 80));
            sb.AppendLine("PER-CAVITY DETAILS");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine();

            if (cavities == null || cavities.Count == 0)
            {
                sb.AppendLine("  No cavities.");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("  " +
                $"{"ID",4} {"Vol(Å³)",10} {"Surf(Å²)",10} {"Sph",6} {"V/S",7} " +
                $"{"Walls",5} {"Vox",8} {"MaxR(Å)",8} {"EffD(Å)",8} {"Channels",12}");
            sb.AppendLine("  " + new string('-', 92));

            foreach (var c in cavities.OrderBy(c => c.Id))
            {
                string chan = c.HasCrystalChannels
                    ? string.Join("/", c.CrystalChannelResults.Keys)
                    : "-";
                float vs = c.SurfaceArea > 0 ? c.Volume / c.SurfaceArea : 0f;
                float effD = c.SphereFitCalculated ? 2 * c.MaxSphereRadius : 0f;

                sb.AppendLine("  " +
                    $"{c.Id,4} " +
                    $"{c.Volume,10:F4} " +
                    $"{c.SurfaceArea,10:F4} " +
                    $"{c.Sphericity,6:F3} " +
                    $"{vs,7:F3} " +
                    $"{c.WallConnections,5} " +
                    $"{c.Voxels?.Count ?? 0,8} " +
                    $"{c.MaxSphereRadius,8:F3} " +
                    $"{effD,8:F3} " +
                    $"{chan,12}");
            }
            sb.AppendLine();
        }

 // ═══════════════════════════════════════════════════════════════
 // 6. CHANNELS
 // ═══════════════════════════════════════════════════════════════

        private static void AppendChannelSection(StringBuilder sb, List<Cavity> cavities)
        {
            sb.AppendLine(new string('-', 80));
            sb.AppendLine("CRYSTAL CHANNELS (semi-periodic)");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine();

            var withCh = cavities?.Where(c => c.HasCrystalChannels).ToList();
            if (withCh == null || withCh.Count == 0)
            {
                sb.AppendLine("  No channels detected.");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("  " +
                $"{"Cav",4} {"Dir",4} {"Len(Å)",9} {"AvgW(Å)",9} {"MinW(Å)",9} " +
                $"{"MaxW(Å)",9} {"EffD(Å)",9} {"Hel(°)",8} {"Cross",6}");
            sb.AppendLine("  " + new string('-', 80));

            var dirStats = new Dictionary<string, int>();

            foreach (var c in withCh.OrderBy(c => c.Id))
            {
                foreach (var kvp in c.CrystalChannelResults.OrderBy(k => k.Key))
                {
                    var r = kvp.Value;
                    if (r == null || !r.IsConnected) continue;

                    if (!dirStats.ContainsKey(kvp.Key)) dirStats[kvp.Key] = 0;
                    dirStats[kvp.Key]++;

                    sb.AppendLine("  " +
                        $"{c.Id,4} " +
                        $"{kvp.Key,4} " +
                        $"{r.TotalLength,9:F3} " +
                        $"{r.AverageWidth,9:F3} " +
                        $"{r.MinWidth,9:F3} " +
                        $"{r.MaxWidth,9:F3} " +
                        $"{r.EffectiveDiameter,9:F3} " +
                        $"{r.Helicity,8:F2} " +
                        $"{r.CellCrossings,6}");
                }
            }
            sb.AppendLine();

            sb.AppendLine("  Direction summary:");
            foreach (var kvp in dirStats.OrderBy(k => k.Key))
                sb.AppendLine($"    {kvp.Key}:  {kvp.Value} channel(s)");
            sb.AppendLine();

            bool hasX = dirStats.ContainsKey("XX");
            bool hasY = dirStats.ContainsKey("YY");
            bool hasZ = dirStats.ContainsKey("ZZ");
            string net = (hasX && hasY && hasZ) ? "3D channel network"
                       : (hasX || hasY || hasZ) ? "1D/2D channel network"
                       : "isolated cavities only";
            sb.AppendLine($"  Network: {net}");
            sb.AppendLine();
        }

 // ═══════════════════════════════════════════════════════════════
 // 7. SPHERE FITS
 // ═══════════════════════════════════════════════════════════════

        private static void AppendSphereFitSection(StringBuilder sb, List<Cavity> cavities)
        {
            sb.AppendLine(new string('-', 80));
            sb.AppendLine("SPHERE FITS");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine();

            var withSph = cavities?.Where(c => c.SphereFitCalculated
                                            && c.SphereFits != null
                                            && c.SphereFits.Count > 0).ToList();
            if (withSph == null || withSph.Count == 0)
            {
                sb.AppendLine("  No sphere fits available.");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("  " +
                $"{"Cav",4} {"N",4} {"MaxR(Å)",9} {"EffD(Å)",9} " +
                $"{"Packing",9} {"UsableV(Å³)",12} {"OnWall",7}");
            sb.AppendLine("  " + new string('-', 60));

            foreach (var c in withSph.OrderBy(c => c.Id))
            {
                float usableVolume = 0f;
                int onWall = 0;

                foreach (var s in c.SphereFits)
                {
                    float r = s.MaxSphereRadius;
                    usableVolume += (4f / 3f) * MathF.PI * r * r * r;

                    bool isOnWall = s.FracX < 0.01f || s.FracX > 0.99f
                                 || s.FracY < 0.01f || s.FracY > 0.99f
                                 || s.FracZ < 0.01f || s.FracZ > 0.99f;
                    if (isOnWall) onWall++;
                }

                float packing = c.Volume > 0 ? usableVolume / c.Volume : 0f;
                float maxR = c.SphereFits.Max(s => s.MaxSphereRadius);
                float effD = 2 * maxR;

                sb.AppendLine("  " +
                    $"{c.Id,4} " +
                    $"{c.SphereFits.Count,4} " +
                    $"{maxR,9:F3} " +
                    $"{effD,9:F3} " +
                    $"{packing,9:F3} " +
                    $"{usableVolume,12:F3} " +
                    $"{onWall,7}");
            }
            sb.AppendLine();
        }

 // ═══════════════════════════════════════════════════════════════
 // 8. SINGLE-RUN QSPR
 // ═══════════════════════════════════════════════════════════════

        private static void AppendSingleRunQSPR(StringBuilder sb, List<Cavity> cavities, Molecule molecule)
        {
            sb.AppendLine(new string('-', 80));
            sb.AppendLine("SINGLE-RUN QSPR DESCRIPTORS");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine();

            var primary = cavities?.Where(c => !c.IsPeriodicDuplicate && c.Volume > 0).ToList();
            if (primary == null || primary.Count == 0)
            {
                sb.AppendLine("  No primary cavities — QSPR descriptors not available.");
                sb.AppendLine();
                return;
            }

            int atomCount = molecule?.NumberOfAtoms ?? 0;
            float cellVolume = molecule?.Volume ?? 0f;
            float totalVolume = primary.Sum(c => c.Volume);
            float totalSurface = primary.Sum(c => c.SurfaceArea);

            float I_frag = atomCount > 0 ? (float)primary.Count / atomCount : 0f;
            float porosity = cellVolume > 0 ? totalVolume / cellVolume : 0f;
            float vsRatio = totalSurface > 0 ? totalVolume / totalSurface : 0f;

            float meanQVar = 0f, meanHVar = 0f, meanZ = 0f;
            int counted = 0;
            foreach (var c in primary)
            {
                if (!c.HasVoxelPropertyData) continue;
                meanQVar += ComputeVariance(c.VoxelCharges);
                meanHVar += ComputeVariance(c.VoxelHydrophobicity);
                meanZ += c.VoxelZEffective?.Average() ?? 0f;
                counted++;
            }
            if (counted > 0)
            {
                meanQVar /= counted;
                meanHVar /= counted;
                meanZ /= counted;
            }

            sb.AppendLine($"  I_frag (cavities/atom):       {I_frag:F6}");
            sb.AppendLine($"    → {primary.Count} cavities / {atomCount} atoms in cell");
            sb.AppendLine();
            sb.AppendLine($"  Porosity V_voids/V_cell:      {porosity:F6}");
            sb.AppendLine($"    → {totalVolume:F4} Å³ voids / {cellVolume:F4} Å³ cell");
            sb.AppendLine();
            sb.AppendLine($"  Volume-to-surface ratio:      {vsRatio:F4} Å");
            sb.AppendLine($"  Mean charge variance:         {meanQVar:F6}");
            sb.AppendLine($"  Mean hydrophobicity variance: {meanHVar:F6}");
            sb.AppendLine($"  Mean Z-effective:             {meanZ:F4}");
            sb.AppendLine();
            sb.AppendLine("  Note: for a full QSPR profile across VdW scales, use");
            sb.AppendLine("        Calculate → VdW Scaling Analysis...");
            sb.AppendLine();
        }

        private static float ComputeVariance(float[] values)
        {
            if (values == null || values.Length < 2) return 0f;
            float sum = 0f, sumSq = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                sum += values[i];
                sumSq += values[i] * values[i];
            }
            float mean = sum / values.Length;
            return sumSq / values.Length - mean * mean;
        }

 // ═══════════════════════════════════════════════════════════════
 // 9. REFINEMENT METADATA (z Molecule.Metadata.cs)
 // ═══════════════════════════════════════════════════════════════

        private static void AppendRefinementMetadata(StringBuilder sb, Molecule m)
        {
            if (m == null) return;

            bool hasAny = m.R1_Observed > 0 || m.R1_All > 0 || m.wR2_All > 0
                       || m.GooF > 0 || m.F000 > 0 || m.Wavelength > 0
                       || m.Temperature > 0 || m.PublTitle.Length > 0
                       || m.DOI.Length > 0 || m.AuditCreationDate.Length > 0;

            if (!hasAny) return;

            sb.AppendLine(new string('-', 80));
            sb.AppendLine("REFINEMENT / EXPERIMENT / PUBLICATION METADATA");
            sb.AppendLine(new string('-', 80));
            sb.AppendLine();

            if (m.R1_Observed > 0 || m.R1_All > 0 || m.wR2_All > 0 || m.GooF > 0)
            {
                sb.AppendLine("Refinement statistics:");
                if (m.R1_Observed > 0) sb.AppendLine($"  R1 (observed):           {m.R1_Observed:F4}");
                if (m.R1_All > 0) sb.AppendLine($"  R1 (all):                {m.R1_All:F4}");
                if (m.wR2_All > 0) sb.AppendLine($"  wR2 (all):               {m.wR2_All:F4}");
                if (m.wR2_Observed > 0) sb.AppendLine($"  wR2 (observed):          {m.wR2_Observed:F4}");
                if (m.GooF > 0) sb.AppendLine($"  GooF:                    {m.GooF:F4}");
                if (m.RefinementReflections > 0) sb.AppendLine($"  Reflections:             {m.RefinementReflections:N0}");
                if (m.RefinementParameters > 0) sb.AppendLine($"  Parameters:              {m.RefinementParameters:N0}");
                if (m.RefinementRestraints > 0) sb.AppendLine($"  Restraints:              {m.RefinementRestraints:N0}");
                if (m.ShiftMax > 0) sb.AppendLine($"  Shift/su max:            {m.ShiftMax:F4}");
                if (m.ShiftMean > 0) sb.AppendLine($"  Shift/su mean:           {m.ShiftMean:F4}");
                if (m.DiffDensityMax > 0 || m.DiffDensityMin < 0)
                    sb.AppendLine($"  Δρ max / min:            {m.DiffDensityMax:F4} / {m.DiffDensityMin:F4} e/Å³");
                if (m.DiffDensityRMS > 0) sb.AppendLine($"  Δρ RMS:                  {m.DiffDensityRMS:F4} e/Å³");
                if (!string.IsNullOrEmpty(m.WeightingScheme))
                    sb.AppendLine($"  Weighting:               {m.WeightingScheme}");
                sb.AppendLine();
            }

            if (m.ReflnsTotal > 0 || m.ResolutionHigh > 0 || m.ThetaMax > 0)
            {
                sb.AppendLine("Reflection statistics:");
                if (m.ReflnsTotal > 0) sb.AppendLine($"  Total reflections:       {m.ReflnsTotal:N0}");
                if (m.ReflnsObserved > 0) sb.AppendLine($"  Observed:                {m.ReflnsObserved:N0}");
                if (!string.IsNullOrEmpty(m.ReflnsThreshold))
                    sb.AppendLine($"  Threshold:               {m.ReflnsThreshold}");
                if (m.ResolutionHigh > 0) sb.AppendLine($"  d_min:                   {m.ResolutionHigh:F4} Å");
                if (m.ResolutionLow > 0) sb.AppendLine($"  d_max:                   {m.ResolutionLow:F4} Å");
                if (m.ThetaMin > 0) sb.AppendLine($"  θ_min:                   {m.ThetaMin:F3}°");
                if (m.ThetaMax > 0) sb.AppendLine($"  θ_max:                   {m.ThetaMax:F3}°");
                sb.AppendLine();
            }

            if (m.Wavelength > 0 || m.Temperature > 0 || m.F000 > 0
                || !string.IsNullOrEmpty(m.MeasurementDevice))
            {
                sb.AppendLine("Experiment:");
                if (m.Temperature > 0) sb.AppendLine($"  Temperature:             {m.Temperature:F2} K");
                if (m.Wavelength > 0) sb.AppendLine($"  Wavelength:              {m.Wavelength:F4} Å");
                if (!string.IsNullOrEmpty(m.RadiationType))
                    sb.AppendLine($"  Radiation type:          {m.RadiationType}");
                if (!string.IsNullOrEmpty(m.MeasurementDevice))
                    sb.AppendLine($"  Device:                  {m.MeasurementDevice}");
                if (m.F000 > 0) sb.AppendLine($"  F(000):                  {m.F000:F1}");
                if (!string.IsNullOrEmpty(m.CrystalColour))
                    sb.AppendLine($"  Crystal colour:          {m.CrystalColour}");
                if (!string.IsNullOrEmpty(m.CrystalDescription))
                    sb.AppendLine($"  Crystal description:     {m.CrystalDescription}");
                if (m.CrystalSizeMax > 0)
                    sb.AppendLine($"  Crystal size:            {m.CrystalSizeMax:F3} × {m.CrystalSizeMid:F3} × {m.CrystalSizeMin:F3} mm");
                if (m.AbsorptionCoefficient > 0)
                    sb.AppendLine($"  Absorption μ:            {m.AbsorptionCoefficient:F4} mm⁻¹");
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(m.PublTitle) || !string.IsNullOrEmpty(m.JournalName)
                || !string.IsNullOrEmpty(m.DOI))
            {
                sb.AppendLine("Publication:");
                if (!string.IsNullOrEmpty(m.PublTitle))
                    sb.AppendLine($"  Title:                   {m.PublTitle}");
                if (!string.IsNullOrEmpty(m.JournalName))
                    sb.AppendLine($"  Journal:                 {m.JournalName}");
                if (!string.IsNullOrEmpty(m.JournalReference))
                    sb.AppendLine($"  Reference:               {m.JournalReference}");
                if (!string.IsNullOrEmpty(m.DOI))
                    sb.AppendLine($"  DOI:                     {m.DOI}");
                if (!string.IsNullOrEmpty(m.AuditCreationDate))
                    sb.AppendLine($"  Audit date:              {m.AuditCreationDate}");
                if (!string.IsNullOrEmpty(m.AuditCreationMethod))
                    sb.AppendLine($"  Audit method:            {m.AuditCreationMethod}");
                sb.AppendLine();
            }
        }

 // ═══════════════════════════════════════════════════════════════
 // 10. FOOTER
 // ═══════════════════════════════════════════════════════════════

        private static void AppendFooter(StringBuilder sb)
        {
            sb.AppendLine(new string('=', 80));
            sb.AppendLine($"Generated: {DateTime.Now}");
            sb.AppendLine(new string('=', 80));
        }
    }
}