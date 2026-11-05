// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: VdWAnalysisResult.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Xna.Framework;
using Tal.GUI;
using SysVector3 = System.Numerics.Vector3;

namespace Tal.Cavity
{
 // ============================================================
 // BASE RESULT CLASSES
 // ============================================================

 /// <summary>
 /// VdW analysis result for a single cavity at a given scale.
 /// Contains all geometric and electrostatic descriptors.
 /// </summary>
    public class VdWAnalysisResult
    {
        public float VdWScale { get; set; }
        public int CavityId { get; set; }
        public float Volume { get; set; }
        public float VolumeError { get; set; }
        public float SurfaceArea { get; set; }
        public float SurfaceAreaError { get; set; }
        public float Sphericity { get; set; }
        public float VolumeToSurfaceRatio { get; set; }
        public float RatioError { get; set; }
        public int TriangleCount { get; set; }
        public int VoxelCount { get; set; }
        public int WallConnections { get; set; }
        public bool IsPeriodicDuplicate { get; set; }

        public float MeanCharge { get; set; }
        public float ChargeVariance { get; set; }
        public float MeanHydrophobicity { get; set; }
        public float HydrophobicityVariance { get; set; }
        public float MeanZEffective { get; set; }
        public float DipoleMomentX { get; set; }
        public float DipoleMomentY { get; set; }
        public float DipoleMomentZ { get; set; }
        public float DipoleMomentMagnitude { get; set; }

        public bool HasChannelsAtThisScale { get; set; } = false;
        public HashSet<string> ChannelDirections { get; set; } = new HashSet<string>();
    }

 // ============================================================
 // GUEST MOLECULE
 // ============================================================

 /// <summary>
 /// Guest molecule definition with extended properties
 /// </summary>
    public class GuestMolecule
    {
        public string Name { get; set; }
        public string Formula { get; set; }
        public float Volume { get; set; }
        public float Diameter { get; set; }
        public float Polarizability { get; set; }
        public bool IsPolar { get; set; }
        public float Hydrophobicity { get; set; }
        public float Density { get; set; } = 1.0f;
        public float MolecularWeight { get; set; } = 0f;
        public float DiffusionCoefficient { get; set; } = 0f;
        public float BoilingPoint { get; set; } = 0f;
        public float MeltingPoint { get; set; } = 0f;
        public float DipoleMoment { get; set; } = 0f;
        public bool IsGas { get; set; } = false;

        public GuestMolecule(string name, string formula, float volume, float diameter,
                             float polarizability, bool isPolar, float hydrophobicity,
                             float density = 1.0f, float molecularWeight = 0f,
                             float dipoleMoment = 0f, bool isGas = false)
        {
            Name = name;
            Formula = formula;
            Volume = volume;
            Diameter = diameter;
            Polarizability = polarizability;
            IsPolar = isPolar;
            Hydrophobicity = hydrophobicity;
            Density = density;
            MolecularWeight = molecularWeight;
            DipoleMoment = dipoleMoment;
            IsGas = isGas;
        }

        public override string ToString() => $"{Name} ({Formula}) - {Volume:F0} Å³";
    }

 // ============================================================
 // SPHERE PACKING RESULT
 // ============================================================

 /// <summary>
 /// Result of sphere packing analysis within a cavity
 /// </summary>
    public class SpherePackingResult
    {
        public int CavityId { get; set; }
        public float CavityVolume { get; set; }
        public float UsableVolume { get; set; }
        public float MaxSphereRadius { get; set; }
        public float EffectiveDiameter { get; set; }
        public int SphereCount { get; set; }
        public List<PackedSphere> Spheres { get; set; } = new List<PackedSphere>();
        public float PackingEfficiency { get; set; }
        public float UsableVolumeFraction { get; set; }
        public bool HasChannels { get; set; }
        public string ChannelInfo { get; set; }

        public List<GasGuestResult> GasCompatibility { get; set; } = new List<GasGuestResult>();
        public List<SolventGuestResult> SolventCompatibility { get; set; } = new List<SolventGuestResult>();

        public float GetMaxGuestDiameter() => EffectiveDiameter;
        public float GetMaxGuestRadius() => EffectiveDiameter / 2f;
    }

 /// <summary>
 /// Single packed sphere
 /// </summary>
    public class PackedSphere
    {
        public int Index { get; set; }
        public SysVector3 Center { get; set; }
        public float Radius { get; set; }
        public float Diameter => Radius * 2f;
        public float FracX { get; set; }
        public float FracY { get; set; }
        public float FracZ { get; set; }
        public bool IsOnWall { get; set; }
        public float DistanceToCenter { get; set; }
    }

 /// <summary>
 /// Gas compatibility result
 /// </summary>
    public class GasGuestResult
    {
        public string Name { get; set; }
        public string Formula { get; set; }
        public float KineticDiameter { get; set; }
        public float MolecularWeight { get; set; }
        public float CalculatedVolume { get; set; }
        public float VolumeRatio { get; set; }
        public bool CanFit { get; set; }
        public float Score { get; set; }
        public string Status { get; set; }
        public string BestSphereLocation { get; set; }
        public float RecommendedCount { get; set; }
        public bool IsDiffusionLimited { get; set; }
    }

 /// <summary>
 /// Solvent compatibility result with voxel cluster analysis
 /// </summary>
    public class SolventGuestResult
    {
        public string Name { get; set; }
        public string Formula { get; set; }
        public float Volume { get; set; }
        public float Diameter { get; set; }
        public float Score { get; set; }
        public string Status { get; set; }
        public float PolarizabilityMatch { get; set; }
        public float ChargeCompatibility { get; set; }
        public float HydrophobicityMatch { get; set; }
        public float ShapeMatch { get; set; }
        public string BestSphereLocation { get; set; }
        public bool CanFit { get; set; }
        public float RecommendedCount { get; set; }
        public float VoxelClusterMatch { get; set; }
    }

 // ============================================================
 // MAIN VDW ANALYSIS RESULTS CONTAINER
 // ============================================================

    public partial class VdWAnalysisResults
    {
        public List<float> VdWScales { get; set; } = new List<float>();
        public Dictionary<int, List<VdWAnalysisResult>> CavityResults { get; set; } = new Dictionary<int, List<VdWAnalysisResult>>();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public float TotalProcessingTimeMs { get; set; }
        public int TotalCavitiesAnalyzed { get; set; }

        public float FragmentationIndex { get; set; }
        public float CollapseScale { get; set; }
        public float ElectrostaticFrustration { get; set; }
        public float HydrophobicFrustration { get; set; }
        public float MeanPolarizability { get; set; }
        public float MaxDipoleSensitivity { get; set; }

        public float CellVolume { get; set; }

        public float VoxelVolume { get; set; }
        public int AtomCount { get; set; }
        public float CellMass { get; set; }

        public Dictionary<int, CavityTrackData> CavityTracks { get; set; } = new Dictionary<int, CavityTrackData>();
        public List<CriticalEvent> CriticalEvents { get; set; } = new List<CriticalEvent>();

        public Dictionary<int, Dictionary<string, CavityPathFinder.ChannelPathResult>> CrystalChannelData { get; set; }
            = new Dictionary<int, Dictionary<string, CavityPathFinder.ChannelPathResult>>();

        public Dictionary<int, SpherePackingResult> SpherePacking { get; set; } = new Dictionary<int, SpherePackingResult>();

        public Dictionary<int, CavityPathStats> CavityPathStats { get; set; }
            = new Dictionary<int, CavityPathStats>();

        public Dictionary<string, List<CavityPathFinder.ChannelPathResult>> AllChannelPaths { get; set; }
            = new Dictionary<string, List<CavityPathFinder.ChannelPathResult>>();

        public Dictionary<string, CavityPathFinder.ChannelPathResult> BestChannelPaths { get; set; }
            = new Dictionary<string, CavityPathFinder.ChannelPathResult>();

        public int TotalChannelPaths => AllChannelPaths.Values.Sum(list => list?.Count ?? 0);
        public int CavitiesWithPaths => CavityPathStats.Count(s => s.Value.HasPaths);

        public bool CavityHasChannels(int cavityId)
        {
            return CrystalChannelData.ContainsKey(cavityId) && CrystalChannelData[cavityId].Count > 0;
        }

        public HashSet<string> GetCavityChannelDirections(int cavityId)
        {
            if (CrystalChannelData.TryGetValue(cavityId, out var channels))
                return new HashSet<string>(channels.Keys);
            return new HashSet<string>();
        }

        public int GetCavityChannelCount(int cavityId)
        {
            if (CrystalChannelData.TryGetValue(cavityId, out var channels))
                return channels.Count;
            return 0;
        }

        public SpherePackingResult GetSpherePacking(int cavityId)
        {
            SpherePacking.TryGetValue(cavityId, out var result);
            return result;
        }
    }

 // ============================================================
 // CAVITY TRACK DATA
 // ============================================================

    public class CavityTrackData
    {
        public int Id { get; set; }
        public Dictionary<float, CavitySnapshot> Snapshots { get; set; } = new Dictionary<float, CavitySnapshot>();
        public float DisappearanceScale { get; set; } = 2.0f;
        public bool Disappears => DisappearanceScale < 2.0f;

        public float VolumeAt1_0 => Snapshots.ContainsKey(1.0f) ? Snapshots[1.0f].Volume : 0f;
        public float VolumeAt1_1 => Snapshots.ContainsKey(1.1f) ? Snapshots[1.1f].Volume : 0f;
        public float VolumeAt1_2 => Snapshots.ContainsKey(1.2f) ? Snapshots[1.2f].Volume : 0f;
        public float VolumeAt1_3 => Snapshots.ContainsKey(1.3f) ? Snapshots[1.3f].Volume : 0f;
        public float VolumeAt1_4 => Snapshots.ContainsKey(1.4f) ? Snapshots[1.4f].Volume : 0f;
        public float VolumeAt1_5 => Snapshots.ContainsKey(1.5f) ? Snapshots[1.5f].Volume : 0f;

        public bool HasChannels { get; set; } = false;
        public HashSet<string> ChannelDirections { get; set; } = new HashSet<string>();

        public bool HasChannel(string direction) => ChannelDirections.Contains(direction);
        public string GetChannelDescription()
        {
            if (!HasChannels || ChannelDirections.Count == 0)
                return "none";
            return string.Join(",", ChannelDirections);
        }
    }

 // ============================================================
 // CAVITY SNAPSHOT
 // ============================================================

    public class CavitySnapshot
    {
        public float Scale { get; set; }
        public int Id { get; set; }
        public float Volume { get; set; }
        public float SurfaceArea { get; set; }
        public float Sphericity { get; set; }
        public float VolumeToSurfaceRatio { get; set; }
        public int WallConnections { get; set; }
        public int TopologyType { get; set; }

        public float MeanCharge { get; set; }
        public float ChargeVariance { get; set; }
        public float MeanHydrophobicity { get; set; }
        public float HydrophobicityVariance { get; set; }
        public float MeanZEffective { get; set; }
        public float DipoleMomentX { get; set; }
        public float DipoleMomentY { get; set; }
        public float DipoleMomentZ { get; set; }
        public float DipoleMomentMagnitude { get; set; }

        public bool HasChannels { get; set; } = false;
        public HashSet<string> ChannelDirections { get; set; } = new HashSet<string>();

        public string GetChannelDescription()
        {
            if (!HasChannels || ChannelDirections.Count == 0)
                return "none";
            return string.Join(",", ChannelDirections);
        }
    }

 // ============================================================
 // CRITICAL EVENTS
 // ============================================================

    public class CriticalEvent
    {
        public float Scale { get; set; }
        public int CavityId { get; set; }
        public string Description { get; set; }
        public EventType Type { get; set; }

        public enum EventType
        {
            Disappearance,
            Merge,
            Split,
            ChannelOpen,
            ChannelClose,
            TopologyChange
        }
    }

 // ============================================================
 // HOST-GUEST REPORT
 // ============================================================

    public class HostGuestReport
    {
        public int LargestCavityId { get; set; }
        public float LargestVolume { get; set; }
        public float EquivalentDiameter { get; set; }
        public float Sphericity { get; set; }
        public float VSRatio { get; set; }

        public bool HasChannels { get; set; }
        public int ChannelCount { get; set; }
        public string BestChannel { get; set; }
        public float BestChannelWidth { get; set; }
        public float BestChannelLength { get; set; }
        public float BestChannelHelicity { get; set; }
        public float BestChannelEffectiveDiameter { get; set; }
        public float BestChannelMinDiameter { get; set; }
        public float BestChannelAvgDiameter { get; set; }
        public float BestChannelSmoothingError { get; set; }

        public Dictionary<string, float> ChannelWidths { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, float> ChannelLengths { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, float> ChannelHelicities { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, float> ChannelMinWidths { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, float> ChannelMaxWidths { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, float> ChannelEffectiveDiameters { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, float> ChannelSmoothingErrors { get; set; } = new Dictionary<string, float>();

        public SpherePackingResult SpherePacking { get; set; }

        public Dictionary<string, float> GuestScores { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, string> GuestRecommendations { get; set; } = new Dictionary<string, string>();
        public List<GuestMolecule> CompatibleGuests { get; set; } = new List<GuestMolecule>();
        public List<GuestMolecule> PartialGuests { get; set; } = new List<GuestMolecule>();
        public List<GuestMolecule> IncompatibleGuests { get; set; } = new List<GuestMolecule>();

        public List<GasGuestResult> GasResults { get; set; } = new List<GasGuestResult>();
        public List<SolventGuestResult> SolventResults { get; set; } = new List<SolventGuestResult>();

        public float ChannelMeanCharge { get; set; }
        public float ChannelChargeVariance { get; set; }
        public float ChannelMeanHydrophobicity { get; set; }
        public float ChannelHydrophobicityVariance { get; set; }

        public string OverallRating { get; set; }
        public string Summary { get; set; }
    }

 // ============================================================
 // CAVITY PATH STATS
 // ============================================================

 /// <summary>
 /// Cavity path statistics container.
 /// Uses ChannelPathStatistics from Tal.Cavity namespace.
 /// </summary>
    public class CavityPathStats
    {
        public int CavityId { get; set; }

        public Dictionary<string, ChannelPathStatistics> DirectionStats { get; set; }
            = new Dictionary<string, ChannelPathStatistics>();

        public Dictionary<string, CavityPathFinder.ChannelPathResult> BestPaths { get; set; }
            = new Dictionary<string, CavityPathFinder.ChannelPathResult>();

        public Dictionary<string, List<CavityPathFinder.ChannelPathResult>> AllPaths { get; set; }
            = new Dictionary<string, List<CavityPathFinder.ChannelPathResult>>();

        public int TotalPaths => AllPaths.Values.Sum(list => list?.Count ?? 0);
        public bool HasPaths => TotalPaths > 0;

        public CavityPathFinder.ChannelPathResult BestOverallPath
        {
            get
            {
                return BestPaths.Values
                    .OrderByDescending(p => p?.AverageWidth ?? 0)
                    .FirstOrDefault();
            }
        }

        public CavityPathFinder.ChannelPathResult ShortestOverallPath
        {
            get
            {
                return AllPaths.Values
                    .SelectMany(list => list ?? new List<CavityPathFinder.ChannelPathResult>())
                    .OrderBy(p => p?.TotalLength ?? float.MaxValue)
                    .FirstOrDefault();
            }
        }
    }

 // ============================================================
 // VDW ANALYSIS EXTENSIONS
 // ============================================================

    public static class VdWAnalysisExtensions
    {
        public static Dictionary<string, CavityPathFinder.ChannelPathResult> GetChannelsForCavity(
            this VdWAnalysisResults results, int cavityId)
        {
            if (results.CrystalChannelData.TryGetValue(cavityId, out var channels))
                return channels;
            return new Dictionary<string, CavityPathFinder.ChannelPathResult>();
        }

        public static bool HasChannel(this VdWAnalysisResults results, int cavityId, string direction)
        {
            if (results.CrystalChannelData.TryGetValue(cavityId, out var channels))
                return channels.ContainsKey(direction);
            return false;
        }

        public static CavityPathFinder.ChannelPathResult GetChannelResult(
            this VdWAnalysisResults results, int cavityId, string direction)
        {
            if (results.CrystalChannelData.TryGetValue(cavityId, out var channels))
            {
                if (channels.TryGetValue(direction, out var result))
                    return result;
            }
            return null;
        }

        public static Dictionary<string, int> GetChannelStatistics(this VdWAnalysisResults results)
        {
            var stats = new Dictionary<string, int>();
            foreach (var kvp in results.CrystalChannelData)
            {
                foreach (var dir in kvp.Value.Keys)
                {
                    if (!stats.ContainsKey(dir))
                        stats[dir] = 0;
                    stats[dir]++;
                }
            }
            return stats;
        }

        public static int GetTotalChannelCount(this VdWAnalysisResults results)
        {
            int count = 0;
            foreach (var kvp in results.CrystalChannelData)
                count += kvp.Value.Count;
            return count;
        }

        public static int GetCavitiesWithChannelsCount(this VdWAnalysisResults results)
        {
            int count = 0;
            foreach (var kvp in results.CrystalChannelData)
            {
                if (kvp.Value.Count > 0)
                    count++;
            }
            return count;
        }

        public static List<int> GetCavityIdsWithChannels(this VdWAnalysisResults results)
        {
            var list = new List<int>();
            foreach (var kvp in results.CrystalChannelData)
            {
                if (kvp.Value.Count > 0)
                    list.Add(kvp.Key);
            }
            return list;
        }

        public static string GetChannelNetworkDescription(this VdWAnalysisResults results)
        {
            var stats = results.GetChannelStatistics();
            bool hasX = stats.ContainsKey("XX");
            bool hasY = stats.ContainsKey("YY");
            bool hasZ = stats.ContainsKey("ZZ");

            if (hasX && hasY && hasZ)
                return "3D CHANNEL NETWORK - channels in all three directions";
            else if ((hasX && hasY) || (hasX && hasZ) || (hasY && hasZ))
            {
                string dirs = "";
                if (hasX && hasY) dirs = "X and Y";
                else if (hasX && hasZ) dirs = "X and Z";
                else if (hasY && hasZ) dirs = "Y and Z";
                return $"2D CHANNEL NETWORK - channels in {dirs} directions";
            }
            else if (hasX || hasY || hasZ)
            {
                string dir = hasX ? "X" : (hasY ? "Y" : "Z");
                return $"1D CHANNEL NETWORK - only {dir}-direction channels";
            }
            else
                return "No channel network - isolated cavities only";
        }

        public static string GenerateChannelReport(this VdWAnalysisResults results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=".PadRight(60, '='));
            sb.AppendLine("SEMI-PERIODIC CHANNEL ANALYSIS REPORT");
            sb.AppendLine("Channels detected using CavityPathFinder with wrap-around");
            sb.AppendLine("=".PadRight(60, '='));
            sb.AppendLine();

            int totalChannels = results.GetTotalChannelCount();
            int cavitiesWithChannels = results.GetCavitiesWithChannelsCount();

            sb.AppendLine($"Total cavities with channels: {cavitiesWithChannels}");
            sb.AppendLine($"Total channels: {totalChannels}");
            sb.AppendLine();

            var stats = results.GetChannelStatistics();
            sb.AppendLine("Channel direction distribution:");
            foreach (var kvp in stats.OrderBy(k => k.Key))
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value} channels");
            }
            sb.AppendLine();

            sb.AppendLine($"Network type: {results.GetChannelNetworkDescription()}");
            sb.AppendLine();

            sb.AppendLine("Detailed channel information:");
            sb.AppendLine("-".PadRight(60, '-'));
            sb.AppendLine();

            foreach (var kvp in results.CrystalChannelData.OrderBy(k => k.Key))
            {
                int cavityId = kvp.Key;
                var channels = kvp.Value;

                sb.AppendLine($"Cavity #{cavityId}:");
                foreach (var ch in channels.OrderBy(k => k.Key))
                {
                    string dir = ch.Key;
                    var result = ch.Value;

                    if (!result.IsConnected)
                    {
                        sb.AppendLine($"  {dir}: NOT CONNECTED");
                        continue;
                    }

                    sb.AppendLine($"  {dir}:");
                    sb.AppendLine($"    Length: {result.TotalLength:F2} Å");
                    sb.AppendLine($"    Width: {result.AverageWidth:F2} Å (min: {result.MinWidth:F2}, max: {result.MaxWidth:F2})");
                    sb.AppendLine($"    Effective diameter: {result.EffectiveDiameter:F2} Å");
                    sb.AppendLine($"    Helicity: {result.Helicity:F2}°");
                    sb.AppendLine($"    Path points: {result.PathPoints}");
                    sb.AppendLine($"    Cell crossings: {result.CellCrossings}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

 // ============================================================
 // GLOBAL VDW SCALE ANALYZER
 // ============================================================

 /// <summary>
 /// Main VdW scale analyzer - performs cavity detection at multiple VdW scales
 /// and collects geometric, electrostatic, and channel data.
 /// Uses MarchingCubesMeshGenerator directly for mesh generation.
 /// </summary>
    public static class VdWScaleAnalyzer
    {
        #region Constants

        private const float EPSILON = 0.001f;
        private const float WALL_THRESHOLD = 0.5f;
        private const float MIN_SDF_VALUE = 0.3f;

        #endregion

        #region Fields

        private static Molecule _molecule;
        private static CavityDetector _detector;
        private static VdWAnalysisResults _currentResults;
        private static bool _isRunning = false;
        private static float _progress = 0f;
        private static string _statusMessage = "";
        private static CancellationTokenSource _cancellationTokenSource;
        private static int _totalSteps = 0;
        private static int _completedSteps = 0;

        private static readonly object _logLock = new object();

        #endregion

        #region Events

        public static event Action<float, string> ProgressChanged;
        public static event Action<VdWAnalysisResults> AnalysisComplete;
        public static event Action<string> StatusChanged;
        public static event Action<string> ErrorOccurred;

        #endregion

        #region Properties

        public static VdWAnalysisResults CurrentResults => _currentResults;
        public static bool IsRunning => _isRunning;
        public static float Progress => _progress;
        public static string StatusMessage => _statusMessage;

        public static float[] DefaultScales { get; } = new float[]
        {
            2.00f, 1.95f, 1.90f, 1.85f, 1.80f, 1.75f, 1.70f, 1.65f, 1.60f, 1.55f,
            1.50f, 1.45f, 1.40f, 1.35f, 1.30f, 1.25f, 1.20f, 1.15f, 1.10f, 1.05f, 1.00f
        };

        #endregion

        #region Private Logging

        private static void LogToDebug(string message, System.Drawing.Color? color = null)
        {
            try
            {
                lock (_logLock)
                {
                    var mainForm = System.Windows.Forms.Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                    if (mainForm != null)
                    {
                        var debugWindow = System.Windows.Forms.Application.OpenForms.OfType<DebugWindow>().FirstOrDefault();
                        if (debugWindow != null)
                        {
                            if (color.HasValue)
                                debugWindow.Log(message, color.Value);
                            else
                                debugWindow.Log(message);
                        }
                        else
                        {
                            Debug.WriteLine($"[VDW] {message}");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"[VDW] {message}");
                    }
                }
            }
            catch
            {
                Debug.WriteLine($"[VDW] {message}");
            }
        }

        private static void SetStatus(string message, string icon = "")
        {
            _statusMessage = message;

            var mainForm = MainForm.Instance;

 // ⭐ Marshall eventy na UI thread if need to
            if (mainForm != null && mainForm.InvokeRequired)
            {
                try
                {
                    mainForm.BeginInvoke(new Action(() =>
                    {
                        StatusChanged?.Invoke(message);
                        ProgressChanged?.Invoke(_progress, message);
                        mainForm.SetStatus(message, "", icon);
                    }));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                return;
            }

 // We are on the UI thread
            StatusChanged?.Invoke(message);
            ProgressChanged?.Invoke(_progress, message);
            mainForm?.SetStatus(message, "", icon);
        }

        #endregion

        #region Public Methods

        public static void CancelAnalysis()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _statusMessage = "Analysis cancelled by user";
                StatusChanged?.Invoke(_statusMessage);
                SetStatus("VDW analysis cancelled!", "⚠️");
                _isRunning = false;
            }
        }

 /// <summary>
 /// Runs the VdW scale analysis.
 /// </summary>
 /// <param name="molecule">The molecule to analyze.</param>
 /// <param name="detector">The cavity detector instance.</param>
 /// <param name="scales">Optional custom scales. If null, uses DefaultScales.</param>
 /// <param name="progress">Progress reafterrter.</param>
 /// <param name="cancellationToken">Cancellation token.</param>
 /// <returns>Analysis results.</returns>
        public static async Task<VdWAnalysisResults> RunAnalysisAsync(
            Molecule molecule,
            CavityDetector detector,
            float[] scales = null,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (molecule == null)
                throw new ArgumentNullException(nameof(molecule), "Molecule cannot be null");

            if (detector == null)
                throw new ArgumentNullException(nameof(detector), "CavityDetector cannot be null");

            if (scales == null || scales.Length == 0)
                scales = DefaultScales;

            if (_isRunning)
                throw new InvalidOperationException("Analysis is already running");

 // ⭐ Remember original values for restoration
            float originalVdWValue = Flags.Instance.VdWValue;
            bool originalUseSDFCorrection = Flags.Instance.UseSDFCorrection;
            float originalRadiusMultiplier = detector.RadiusMultiplier;
            float originalScannerRadiusMultiplier = detector.Scanner?.RadiusMultiplier ?? originalRadiusMultiplier;

            _molecule = molecule;
            _detector = detector;
            _isRunning = true;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _progress = 0f;
            _statusMessage = "Starting VdW analysis...";
            _completedSteps = 0;
            _totalSteps = scales.Length;

            SetStatus("VDW Analysis started...", "🔬");

            var results = new VdWAnalysisResults();
            results.VdWScales = new List<float>(scales);
            results.StartTime = DateTime.Now;

 // Get cell data
            results.CellVolume = _molecule.Volume;
            CalculateCellContents(_molecule, out int atomsInCell, out float cellMass);
            results.AtomCount = atomsInCell > 0 ? atomsInCell : _molecule.NumberOfAtoms;
            results.CellMass = cellMass;
            results.VoxelVolume = CellParameters.GetVoxelVolume();
            LogToDebug($"========== VDW ANALYSIS START ==========", System.Drawing.Color.Cyan);
            LogToDebug($"Molecule: {molecule.NumberOfAtoms} atoms", System.Drawing.Color.LightGray);
            LogToDebug($"Cell volume: {results.CellVolume:F2} Å³", System.Drawing.Color.LightGray);
            LogToDebug($"Scales: {string.Join(", ", scales)}", System.Drawing.Color.LightGray);

            try
            {
                if (!detector.HasBaseSDF)
                {
                    _statusMessage = "No base SDF available. Please run cavity detection first.";
                    StatusChanged?.Invoke(_statusMessage);
                    SetStatus(_statusMessage, "❌");
                    return results;
                }

                for (int scaleIdx = 0; scaleIdx < scales.Length; scaleIdx++)
                {
                    float scale = scales[scaleIdx];

                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _statusMessage = "Analysis cancelled";
                        StatusChanged?.Invoke(_statusMessage);
                        SetStatus("VDW Analysis cancelled!", "⚠️");
                        return results;
                    }

                    string scaleMsg = $"VDW {scale:F2}: detecting cavities...";
                    SetStatus(scaleMsg, "🔍");
                    _progress = (float)scaleIdx / scales.Length;
                    progress?.Report(_progress);

                    LogToDebug($"Processing scale: {scale:F2}", System.Drawing.Color.LightGray);

                    try
                    {
                        await PerformCavityDetectionAsync(scale);
                    }
                    catch (OperationCanceledException)
                    {
                        LogToDebug($"Scale {scale:F2} cancelled", System.Drawing.Color.Orange);
                        _statusMessage = "Analysis cancelled";
                        StatusChanged?.Invoke(_statusMessage);
                        SetStatus("VDW Analysis cancelled!", "⚠️");
                        return results;
                    }
                    catch (Exception ex)
                    {
                        LogToDebug($"CRITICAL ERROR for scale {scale:F2}: {ex.Message}", System.Drawing.Color.Red);
                        LogToDebug($"StackTrace: {ex.StackTrace}", System.Drawing.Color.Red);
                        ErrorOccurred?.Invoke($"Error at VdW={scale:F2}: {ex.Message}");
                        SetStatus($"Error at VdW={scale:F2}: {ex.Message}", "❌");
                        continue;
                    }

                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _statusMessage = "Analysis cancelled";
                        StatusChanged?.Invoke(_statusMessage);
                        SetStatus("VDW Analysis cancelled!", "⚠️");
                        return results;
                    }

                    string collectMsg = $"VDW {scale:F2}: collecting data...";
                    SetStatus(collectMsg, "📊");

                    try
                    {
                        var cavities = detector.GetCavities();

                        foreach (var cavity in cavities)
                        {
                            if (cavity.IsPeriodicDuplicate)
                                continue;

                            var result = CollectCavityData(cavity, scale);
                            CollectElectrostaticData(cavity, result);

                            if (!results.CavityResults.ContainsKey(cavity.Id))
                                results.CavityResults[cavity.Id] = new List<VdWAnalysisResult>();

                            results.CavityResults[cavity.Id].Add(result);
                        }

                        LogToDebug($"Scale {scale:F2}: found {cavities.Count(c => !c.IsPeriodicDuplicate)} cavities",
                            System.Drawing.Color.LightGray);
                    }
                    catch (Exception ex)
                    {
                        LogToDebug($"Error collecting data for scale {scale:F2}: {ex.Message}", System.Drawing.Color.Red);
                        ErrorOccurred?.Invoke($"Data collection error: {ex.Message}");
                        continue;
                    }

                    _completedSteps = scaleIdx + 1;
                    _progress = _completedSteps / (float)_totalSteps;

                    string doneMsg = $"VDW {scale:F2}: done - {_detector.CavityCount} cavities";
                    SetStatus(doneMsg, "✅");
                    progress?.Report(_progress);
                }

 // Restore VdW = 1.0
                SetStatus("Restoring VdW to 1.0...", "🔄");
                try
                {
                    await PerformCavityDetectionAsync(1.0f);
                }
                catch (Exception ex)
                {
                    LogToDebug($"Error restoring VdW: {ex.Message}", System.Drawing.Color.Red);
                }

 // Save crystal channel data
                LogToDebug("Saving crystal channel data to results...", System.Drawing.Color.LightGray);
                int cavitiesWithChannels = 0;
                int totalChannels = 0;

                try
                {
                    var cavities = _detector.GetCavities();
                    foreach (var kvp in results.CavityResults)
                    {
                        int cavityId = kvp.Key;
                        var cavity = cavities.FirstOrDefault(c => c.Id == cavityId);

                        if (cavity != null && cavity.HasCrystalChannels &&
                            cavity.CrystalChannelResults != null && cavity.CrystalChannelResults.Count > 0)
                        {
                            results.CrystalChannelData[cavityId] = new Dictionary<string, CavityPathFinder.ChannelPathResult>(cavity.CrystalChannelResults);
                            cavitiesWithChannels++;
                            totalChannels += cavity.CrystalChannelResults.Count;
                            LogToDebug($"Saved crystal channels for cavity {cavityId}: {string.Join(", ", cavity.CrystalChannelResults.Keys)}",
                                System.Drawing.Color.LightGray);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogToDebug($"Error saving channel data: {ex.Message}", System.Drawing.Color.Red);
                }

                LogToDebug($"Crystal channel data saved: {cavitiesWithChannels} cavities, {totalChannels} channels",
                    System.Drawing.Color.LightGreen);

 // Sphere packing analysis
                LogToDebug("Analyzing sphere packing for all cavities...", System.Drawing.Color.LightGray);
                try
                {
                    var cavities = _detector.GetCavities();
                    foreach (var kvp in results.CavityResults)
                    {
                        int cavityId = kvp.Key;
                        var cavity = cavities.FirstOrDefault(c => c.Id == cavityId);

                        if (cavity != null && cavity.SphereFits != null && cavity.SphereFits.Count > 0)
                        {
                            var packing = AnalyzeSpherePacking(cavity, cavity.SphereFits);
                            results.SpherePacking[cavityId] = packing;
                            LogToDebug($"Sphere packing for cavity {cavityId}: {packing.SphereCount} spheres, " +
                                $"max R={packing.MaxSphereRadius:F2} Å, packing={packing.PackingEfficiency:F2}",
                                System.Drawing.Color.LightGray);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogToDebug($"Error analyzing sphere packing: {ex.Message}", System.Drawing.Color.Red);
                }

                results.EndTime = DateTime.Now;
                results.TotalProcessingTimeMs = (float)(results.EndTime - results.StartTime).TotalMilliseconds;
                results.TotalCavitiesAnalyzed = results.CavityResults.Count;

                try
                {
                    CalculateQSPRDescriptors(results);
                    TrackCavities(results);
                    DetectCriticalEvents(results);
                }
                catch (Exception ex)
                {
                    LogToDebug($"Error calculating QSPR: {ex.Message}", System.Drawing.Color.Red);
                }

                _currentResults = results;
                _statusMessage = $"Analysis complete! {results.CavityResults.Count} cavities, {scales.Length} scales, {totalChannels} crystal channels";
                _progress = 1f;

                SetStatus($"VDW Analysis complete! {results.CavityResults.Count} cavities", "🎉");
                progress?.Report(1f);
                AnalysisComplete?.Invoke(results);

                LogToDebug("========== VDW ANALYSIS COMPLETE ==========", System.Drawing.Color.Cyan);
                LogToDebug($"Total processing time: {results.TotalProcessingTimeMs / 1000:F2} s", System.Drawing.Color.LightGray);

                return results;
            }
            finally
            {
 // ⭐⭐⭐ RESTORE ALL SETTINGS TO INITIAL STATE ⭐⭐⭐
                LogToDebug("========== RESTORING SETTINGS AFTER VDW ANALYSIS ==========", System.Drawing.Color.Yellow);

                try
                {
 // 1. Restore VdW in the detector
                    if (_detector != null)
                    {
                        _detector.RadiusMultiplier = originalRadiusMultiplier;
                        _detector.UpdateVdwMultiplier(originalRadiusMultiplier);

                        if (_detector.Scanner != null)
                            _detector.Scanner.RadiusMultiplier = originalScannerRadiusMultiplier;
                    }

 // 2. Restore SDF flags
                    Flags.Instance.VdWValue = originalVdWValue;
                    Flags.Instance.UseSDFCorrection = originalUseSDFCorrection;
                    Flags.Instance.SDFNeedsRebuild = false;
                    Flags.Instance.ForceSDFRecalculation = false;

                    LogToDebug($"Restored: VdW={originalVdWValue:F2}, UseSDFCorrection={originalUseSDFCorrection}", System.Drawing.Color.LightGray);
                }
                catch (Exception ex)
                {
                    LogToDebug($"Error restoring settings: {ex.Message}", System.Drawing.Color.Red);
                }

 // 3. Reset stanu
                _isRunning = false;
                _progress = 0f;

 // 4. Disafterse token source
                try
                {
                    _cancellationTokenSource?.Dispose();
                }
                catch { /* ignore */ }
                _cancellationTokenSource = null;

 // 5. Release static referencje (ale NIE _currentResults!)
                _molecule = null;
                _detector = null;

                LogToDebug("========== SETTINGS RESTORED ==========", System.Drawing.Color.Yellow);
            }
        }

        #endregion

        #region Private Methods - Cavity Detection

        private static async Task PerformCavityDetectionAsync(float vdwMult)
        {
            LogToDebug($"PerformCavityDetection: VdW={vdwMult:F2}", System.Drawing.Color.LightGray);

            if (_molecule == null || _molecule.NumberOfAtoms == 0)
            {
                LogToDebug("No molecule loaded!", System.Drawing.Color.Red);
                SetStatus("No molecule loaded!", "❌");
                return;
            }

            if (_detector == null)
            {
                LogToDebug("No detector available!", System.Drawing.Color.Red);
                SetStatus("No detector available! Run cavity detection first.", "❌");
                return;
            }

            if (!_detector.HasBaseSDF)
            {
                LogToDebug("No base SDF available - cannot perform VDW analysis!", System.Drawing.Color.Red);
                SetStatus("No base SDF available! Run cavity detection first.", "❌");
                return;
            }

            _detector.RadiusMultiplier = vdwMult;
            _detector.UpdateVdwMultiplier(vdwMult);

            float resolution = Flags.Instance.Resolution > 0 ? Flags.Instance.Resolution : 0.15f;
            _detector.UpdateRes(resolution);
            if (_detector.Scanner != null)
                _detector.Scanner.VoxelSize = resolution;

            _detector.ProgressChanged = (msg, percent) =>
            {
                SetStatus($"{msg} ({percent:F0}%)", "⏳");
            };

            LogToDebug($"Using existing detector -> target VdW={vdwMult:F2} (SDF computed directly)",
                System.Drawing.Color.LightGray);

            bool found = false;
            try
            {
                found = await _detector.DetectVoidsAsync(targetVdwRadius: vdwMult);
            }
            catch (Exception ex)
            {
                LogToDebug($"Detection error for VdW={vdwMult:F2}: {ex.Message}", System.Drawing.Color.Red);
                SetStatus($"Error: {ex.Message}", "❌");
                return;
            }

            LogToDebug($"Detection complete: found={found}, cavities={_detector.CavityCount}",
                System.Drawing.Color.LightGray);

            if (found)
            {
                var cavities = _detector.GetCavities();

 // Update cavity helpers
                var helpers = new CavityHelpers(_molecule);
                helpers.SetCavities(cavities);

                LogToDebug($"Calculating sphere fits for VdW={vdwMult:F2}...", System.Drawing.Color.LightGray);
                try
                {
                    
                    int processed = SphereFitAnalyzer.AnalyzeAllCavities(cavities);
                    LogToDebug($"Sphere fit analysis complete: {processed} cavities processed",
                        System.Drawing.Color.LightGray);

                    if (processed > 0)
                    {
                        var largest = cavities
                            .Where(c => c.SphereFitCalculated && !c.IsPeriodicDuplicate)
                            .OrderByDescending(c => c.MaxSphereRadius)
                            .FirstOrDefault();

                        if (largest != null)
                        {
                            LogToDebug($"Largest sphere: R={largest.MaxSphereRadius:F3} Å, D={largest.MaxSphereRadius * 2:F3} Å (cavity #{largest.Id})",
                                System.Drawing.Color.LightGreen);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogToDebug($"Sphere fit error: {ex.Message}", System.Drawing.Color.Red);
                }

                int smoothingLevel = 1;
                var mcParams = CavityDetectorBase.GetMeshParametersForSmoothingLevel(smoothingLevel);

                try
                {
 // ⭐ USE MARCHING CUBES DIRECTLY ⭐
 // Filter cavities with volume > 1 Å³
                    var cavitiesToMesh = cavities.Where(c => c.Volume > 1.0f).ToList();

                    if (cavitiesToMesh.Count > 0)
                    {
                        LogToDebug($"Generating mesh for {cavitiesToMesh.Count} cavities...", System.Drawing.Color.LightGray);
                        SetStatus($"Generating mesh for {cavitiesToMesh.Count} cavities...", "⏳");

                        MarchingCubesMeshGenerator.GenerateAllMeshesParallel(
                            cavitiesToMesh,
                            helpers,
                            mcParams
                        );

                        LogToDebug($"Marching Cubes generation complete for {cavitiesToMesh.Count} cavities",
                            System.Drawing.Color.LightGray);

 // ⭐ KOPIUJ MESHE DO CAVITIES ⭐
                        foreach (var cavity in cavitiesToMesh)
                        {
                            if (cavity.TrianglesGenerated && cavity.Vertices != null && cavity.Vertices.Count > 0)
                            {
                                LogToDebug($"Cavity {cavity.Id}: mesh generated with {cavity.Indices.Count / 3} triangles",
                                    System.Drawing.Color.LightGray);
                            }
                        }
                    }

 // Crystal channel analysis for VdW=1.0
                    if (Math.Abs(vdwMult - 1.0f) < 0.001f)
                    {
                        LogToDebug("Analyzing crystal channels for VdW=1.0...", System.Drawing.Color.LightGray);
                        float probeRadius = 0.5f;
                        int analyzed = 0;
                        int withChannels = 0;

                        foreach (var cavity in cavities)
                        {
                            if (cavity.WallConnections > 0 && cavity.DensityGrid != null)
                            {
                                try
                                {
                                    analyzed++;
                                    cavity.EnsureCrystalChannelDataAnalyzed(probeRadius);
                                    if (cavity.HasCrystalChannels)
                                    {
                                        withChannels++;
                                        LogToDebug($"Cavity {cavity.Id}: Crystal channels: {string.Join(", ", cavity.CrystalChannelResults.Keys)}",
                                            System.Drawing.Color.LightGray);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogToDebug($"Error analyzing crystal channels for cavity {cavity.Id}: {ex.Message}",
                                        System.Drawing.Color.Red);
                                }
                            }
                        }

                        LogToDebug($"Crystal channel analysis: {withChannels}/{analyzed} cavities with channels",
                            System.Drawing.Color.LightGray);

 // Update cavity renderer if available
                        UpdateCavityRenderer(cavities);
                    }

                    helpers.SetCavities(cavities);

                    if (cavities.Any(c => c.SphereFitCalculated && !c.IsPeriodicDuplicate))
                    {
                        var largest = cavities
                            .Where(c => c.SphereFitCalculated && !c.IsPeriodicDuplicate)
                            .OrderByDescending(c => c.MaxSphereRadius)
                            .FirstOrDefault();

                        if (largest != null)
                        {
                            SetStatus($"VDW {vdwMult:F2}: {cavities.Count(c => !c.IsPeriodicDuplicate)} cavities, " +
                                $"largest sphere: {largest.MaxSphereRadius:F3} Å", "📐");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogToDebug($"MC generation error: {ex.Message}", System.Drawing.Color.Red);
                    SetStatus($"MC error: {ex.Message}", "❌");
                }
            }
            else
            {
                var helpers = new CavityHelpers(_molecule);
                helpers.ClearCavities();
                SetStatus($"No cavities found for VdW={vdwMult:F2}", "ℹ️");
            }
        }

 /// <summary>
 /// Updates CavityRenderer if available.
 /// </summary>
        private static void UpdateCavityRenderer(List<Cavity> cavities)
        {
            try
            {
                var mainForm = System.Windows.Forms.Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                if (mainForm != null)
                {
                    var doc = mainForm.GetActiveDocument();
                    if (doc != null)
                    {
                        var renderer = doc.GetCavityRenderer();
                        if (renderer != null)
                        {
                            renderer.SetCavities(cavities);
                            renderer.UpdateCrystalChannelPaths();
                            renderer.BuildRepeatedCavities();
                            renderer.ForceRefresh();
                            doc.RefreshView();
                            LogToDebug("CavityRenderer updated", System.Drawing.Color.LightGray);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogToDebug($"Error updating cavity renderer: {ex.Message}", System.Drawing.Color.Red);
            }
        }

        #endregion

        #region Private Methods - Data Collection

        private static VdWAnalysisResult CollectCavityData(Cavity cavity, float scale)
        {
            var helpers = new CavityHelpers(_molecule);

            float surfaceArea = cavity.SurfaceArea > 0 ? cavity.SurfaceArea : helpers.CalculateSurfaceArea(cavity);
            float surfaceError = helpers.CalculateSurfaceAreaError(cavity);
            float volumeError = helpers.CalculateVolumeError(cavity);
            float sphericity = cavity.Sphericity > 0 ? cavity.Sphericity : helpers.CalculateSphericity(cavity);
            float vsRatio = cavity.Volume > 0 && surfaceArea > 0 ? cavity.Volume / surfaceArea : 0f;

            return new VdWAnalysisResult
            {
                VdWScale = scale,
                CavityId = cavity.Id,
                Volume = cavity.Volume,
                VolumeError = volumeError,
                SurfaceArea = surfaceArea,
                SurfaceAreaError = surfaceError,
                Sphericity = sphericity,
                VolumeToSurfaceRatio = vsRatio,
                RatioError = vsRatio * 0.05f,
                TriangleCount = cavity.RawIndices?.Count / 3 ?? 0,
                VoxelCount = cavity.Voxels?.Count ?? 0,
                WallConnections = cavity.WallConnections,
                IsPeriodicDuplicate = cavity.IsPeriodicDuplicate
            };
        }

        private static void CollectElectrostaticData(Cavity cavity, VdWAnalysisResult result)
        {
            if (cavity == null || result == null)
                return;

            if (cavity.Voxels == null || cavity.Voxels.Count == 0)
                return;

 // Use stored voxel property data
            if (cavity.HasVoxelPropertyData)
            {
                int voxelCount = cavity.Voxels.Count;
                int step = Math.Max(1, voxelCount / 1000);
                int sampleCount = Math.Min(voxelCount, 1000);

                var charges = new List<float>(sampleCount);
                var hydro = new List<float>(sampleCount);
                var zEff = new List<float>(sampleCount);

                for (int i = 0; i < voxelCount; i += step)
                {
                    charges.Add(cavity.VoxelCharges[i]);
                    hydro.Add(cavity.VoxelHydrophobicity[i]);
                    zEff.Add(cavity.VoxelZEffective[i]);
                }

                if (charges.Count > 0)
                {
                    result.MeanCharge = charges.Average();
                    result.MeanHydrophobicity = hydro.Count > 0 ? hydro.Average() : 0;
                    result.MeanZEffective = zEff.Count > 0 ? zEff.Average() : 0;
                    result.ChargeVariance = charges.Count > 1 ? PopulationVariance(charges) : 0;
                    result.HydrophobicityVariance = hydro.Count > 1 ? PopulationVariance(hydro) : 0;
                }

 // Calculate diafterle moment
                if (charges.Count > 0 && cavity.Voxels.Count > 0)
                {
                    try
                    {
                        int count = Math.Min(cavity.Voxels.Count, charges.Count);
                        float totalCharge = charges.Sum();
                        float centerX = 0f, centerY = 0f, centerZ = 0f;

                        if (Math.Abs(totalCharge) > 0.0001f)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                var voxel = cavity.Voxels[i];
                                float q = charges[i];
                                centerX += q * voxel.X;
                                centerY += q * voxel.Y;
                                centerZ += q * voxel.Z;
                            }
                            centerX /= totalCharge;
                            centerY /= totalCharge;
                            centerZ /= totalCharge;
                        }

                        float dipoleX = 0f, dipoleY = 0f, dipoleZ = 0f;
                        for (int i = 0; i < count; i++)
                        {
                            var voxel = cavity.Voxels[i];
                            float q = charges[i];
                            dipoleX += q * (voxel.X - centerX);
                            dipoleY += q * (voxel.Y - centerY);
                            dipoleZ += q * (voxel.Z - centerZ);
                        }

                        result.DipoleMomentX = dipoleX;
                        result.DipoleMomentY = dipoleY;
                        result.DipoleMomentZ = dipoleZ;
                        result.DipoleMomentMagnitude = (float)Math.Sqrt(dipoleX * dipoleX + dipoleY * dipoleY + dipoleZ * dipoleZ);
                    }
                    catch (Exception ex)
                    {
                        LogToDebug($"Error calculating dipole moment: {ex.Message}", System.Drawing.Color.Red);
                        result.DipoleMomentX = 0;
                        result.DipoleMomentY = 0;
                        result.DipoleMomentZ = 0;
                        result.DipoleMomentMagnitude = 0;
                    }
                }
            }
        }

        #endregion

        #region Private Methods - Sphere Packing

        private static SpherePackingResult AnalyzeSpherePacking(Cavity cavity, List<SphereFitAnalyzer.SphereFitResult> spheres)
        {
            var result = new SpherePackingResult
            {
                CavityId = cavity.Id,
                CavityVolume = cavity.Volume,
                SphereCount = spheres?.Count ?? 0,
                HasChannels = cavity.HasCrystalChannels,
                ChannelInfo = cavity.HasCrystalChannels ? string.Join(",", cavity.CrystalChannelResults.Keys) : "none"
            };

            if (spheres == null || spheres.Count == 0)
            {
                result.MaxSphereRadius = 0f;
                result.EffectiveDiameter = 0f;
                result.UsableVolume = 0f;
                result.PackingEfficiency = 0f;
                result.UsableVolumeFraction = 0f;
                return result;
            }

            var sortedSpheres = spheres.OrderByDescending(s => s.MaxSphereRadius).ToList();

            var largest = sortedSpheres.First();
            result.MaxSphereRadius = largest.MaxSphereRadius;
            result.EffectiveDiameter = largest.Diameter;

            float totalSphereVolume = 0f;
            int sphereIdx = 0;

            foreach (var s in sortedSpheres)
            {
                float sphereVol = (4.0f / 3.0f) * (float)Math.PI * s.MaxSphereRadius * s.MaxSphereRadius * s.MaxSphereRadius;
                totalSphereVolume += sphereVol;

                bool isOnWall = s.FracX < 0.01f || s.FracX > 0.99f ||
                                s.FracY < 0.01f || s.FracY > 0.99f ||
                                s.FracZ < 0.01f || s.FracZ > 0.99f;

                float distToCenter = 0f;
                if (cavity.Center != SysVector3.Zero)
                {
                    float dx = s.SphereCenter.X - cavity.Center.X;
                    float dy = s.SphereCenter.Y - cavity.Center.Y;
                    float dz = s.SphereCenter.Z - cavity.Center.Z;
                    distToCenter = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                }

                result.Spheres.Add(new PackedSphere
                {
                    Index = sphereIdx++,
                    Center = s.SphereCenter,
                    Radius = s.MaxSphereRadius,
                    FracX = s.FracX,
                    FracY = s.FracY,
                    FracZ = s.FracZ,
                    IsOnWall = isOnWall,
                    DistanceToCenter = distToCenter
                });
            }

            result.UsableVolume = totalSphereVolume;
            result.PackingEfficiency = cavity.Volume > 0 ? totalSphereVolume / cavity.Volume : 0f;
            result.UsableVolumeFraction = Math.Min(result.PackingEfficiency, 1.0f);

            AnalyzeGasCompatibility(result, cavity);
            AnalyzeSolventCompatibility(result, cavity);

            return result;
        }

        #endregion

        #region Private Methods - Guest Compatibility

        private static List<GuestMolecule> GetGuestMolecules()
        {
            return new List<GuestMolecule>
            {
 // Organic solvents
                new GuestMolecule("Water", "H₂O", 30.0f, 3.1f, 0.0f, true, -1.0f, 1.0f, 18.0f, 1.85f),
                new GuestMolecule("Methanol", "CH₃OH", 67.0f, 4.5f, 0.3f, true, -0.5f, 0.79f, 32.0f, 1.70f),
                new GuestMolecule("Ethanol", "C₂H₅OH", 97.0f, 5.3f, 0.4f, true, -0.3f, 0.79f, 46.0f, 1.69f),
                new GuestMolecule("Acetone", "C₃H₆O", 130.0f, 5.8f, 0.6f, true, -0.2f, 0.78f, 58.0f, 2.88f),
                new GuestMolecule("Benzene", "C₆H₆", 130.0f, 5.8f, 1.0f, false, 0.8f, 0.88f, 78.0f, 0.0f),
                new GuestMolecule("DMSO", "C₂H₆OS", 140.0f, 6.0f, 0.7f, true, -0.4f, 1.10f, 78.0f, 3.96f),
                new GuestMolecule("Cyclohexane", "C₆H₁₂", 155.0f, 6.3f, 0.9f, false, 0.9f, 0.78f, 84.0f, 0.0f),
                new GuestMolecule("Toluene", "C₇H₈", 160.0f, 6.4f, 1.1f, false, 0.7f, 0.87f, 92.0f, 0.36f),
                new GuestMolecule("Chloroform", "CHCl₃", 130.0f, 5.9f, 0.8f, false, 0.5f, 1.48f, 119.0f, 1.04f),
                new GuestMolecule("THF", "C₄H₈O", 120.0f, 5.7f, 0.5f, true, -0.1f, 0.89f, 72.0f, 1.63f),
                new GuestMolecule("Pyridine", "C₅H₅N", 120.0f, 5.7f, 0.6f, true, 0.2f, 0.98f, 79.0f, 2.19f),
                new GuestMolecule("Acetonitrile", "CH₃CN", 80.0f, 5.0f, 0.4f, true, -0.6f, 0.79f, 41.0f, 3.92f),
                new GuestMolecule("DMF", "C₃H₇NO", 130.0f, 5.9f, 0.7f, true, -0.3f, 0.95f, 73.0f, 3.86f),
                new GuestMolecule("Hexane", "C₆H₁₄", 170.0f, 6.6f, 0.8f, false, 1.0f, 0.66f, 86.0f, 0.0f),

 // Gases
                new GuestMolecule("Hydrogen", "H₂", 20.0f, 2.9f, 0.0f, false, -0.5f, 0.09f, 2.0f, 0.0f, true),
                new GuestMolecule("Helium", "He", 15.0f, 2.6f, 0.0f, false, -0.5f, 0.18f, 4.0f, 0.0f, true),
                new GuestMolecule("Methane", "CH₄", 40.0f, 3.8f, 0.0f, false, -0.3f, 0.66f, 16.0f, 0.0f, true),
                new GuestMolecule("Carbon Dioxide", "CO₂", 55.0f, 4.2f, 0.0f, false, -0.2f, 1.98f, 44.0f, 0.0f, true),
                new GuestMolecule("Nitrogen", "N₂", 35.0f, 3.6f, 0.0f, false, -0.3f, 1.25f, 28.0f, 0.0f, true),
                new GuestMolecule("Oxygen", "O₂", 38.0f, 3.7f, 0.0f, false, -0.3f, 1.43f, 32.0f, 0.0f, true),
                new GuestMolecule("Argon", "Ar", 42.0f, 3.9f, 0.0f, false, -0.4f, 1.78f, 40.0f, 0.0f, true),
                new GuestMolecule("Ammonia", "NH₃", 50.0f, 4.0f, 0.0f, true, -0.5f, 0.77f, 17.0f, 1.47f, true),
            };
        }

        private static void AnalyzeGasCompatibility(SpherePackingResult packing, Cavity cavity)
        {
            var gases = GetGuestMolecules().Where(g => g.IsGas).ToList();

            foreach (var gas in gases)
            {
                float gasRadius = gas.Diameter / 2f;
                float maxRadius = packing.MaxSphereRadius;
                bool canFit = gasRadius <= maxRadius * 0.95f;

                float moleculeVolume = (4.0f / 3.0f) * (float)Math.PI * gasRadius * gasRadius * gasRadius;
                float maxCount = packing.UsableVolume > 0 ? packing.UsableVolume / moleculeVolume : 0f;

                bool isDiffusionLimited = false;
                if (packing.HasChannels && packing.ChannelInfo != "none")
                {
                    isDiffusionLimited = false;
                }
                else if (cavity.WallConnections > 0)
                {
                    isDiffusionLimited = true;
                }

                float score = 0f;
                string status = "POOR";
                string bestLocation = "none";

                if (canFit)
                {
                    float sizeMatch = 1.0f - Math.Abs(gasRadius / maxRadius - 0.5f) * 2f;
                    sizeMatch = Math.Clamp(sizeMatch, 0f, 1f);

                    bool hasSpheres = cavity.SphereFits != null && cavity.SphereFits.Count > 0;

                    float polarityMatch = 1.0f;
                    if (gas.IsPolar)
                    {
                        polarityMatch = hasSpheres && maxRadius > 1.0f ? 0.8f : 0.3f;
                    }
                    else
                    {
                        polarityMatch = hasSpheres ? 0.9f : 0.5f;
                    }

                    score = sizeMatch * 0.70f + polarityMatch * 0.30f;

                    if (score >= 0.7f)
                    {
                        status = "GOOD";
                        var bestSphere = packing.Spheres.OrderBy(s => Math.Abs(s.Radius - gasRadius)).FirstOrDefault();
                        if (bestSphere != null)
                        {
                            bestLocation = $"sphere #{bestSphere.Index} at ({bestSphere.FracX:F2}, {bestSphere.FracY:F2}, {bestSphere.FracZ:F2})";
                        }
                    }
                    else if (score >= 0.4f)
                    {
                        status = "FAIR";
                    }
                }

                packing.GasCompatibility.Add(new GasGuestResult
                {
                    Name = gas.Name,
                    Formula = gas.Formula,
                    KineticDiameter = gas.Diameter,
                    MolecularWeight = gas.MolecularWeight,
                    CalculatedVolume = gas.Volume,
                    VolumeRatio = packing.CavityVolume > 0 ? gas.Volume / packing.CavityVolume : 0f,
                    CanFit = canFit,
                    Score = score,
                    Status = status,
                    BestSphereLocation = bestLocation,
                    RecommendedCount = (float)Math.Floor(maxCount),
                    IsDiffusionLimited = isDiffusionLimited
                });
            }
        }

        private static void AnalyzeSolventCompatibility(SpherePackingResult packing, Cavity cavity)
        {
            var solvents = GetGuestMolecules().Where(g => !g.IsGas).ToList();

            if (packing.Spheres == null || packing.Spheres.Count == 0)
            {
                foreach (var solvent in solvents)
                {
                    packing.SolventCompatibility.Add(new SolventGuestResult
                    {
                        Name = solvent.Name,
                        Formula = solvent.Formula,
                        Volume = solvent.Volume,
                        Diameter = solvent.Diameter,
                        Score = 0f,
                        Status = "NO SPHERES",
                        CanFit = false,
                        RecommendedCount = 0,
                        VoxelClusterMatch = 0f
                    });
                }
                return;
            }

            var largestSphere = packing.Spheres.OrderByDescending(s => s.Radius).FirstOrDefault();
            if (largestSphere == null || largestSphere.Radius <= 0)
            {
                foreach (var solvent in solvents)
                {
                    packing.SolventCompatibility.Add(new SolventGuestResult
                    {
                        Name = solvent.Name,
                        Formula = solvent.Formula,
                        Volume = solvent.Volume,
                        Diameter = solvent.Diameter,
                        Score = 0f,
                        Status = "NO VALID SPHERE",
                        CanFit = false,
                        RecommendedCount = 0,
                        VoxelClusterMatch = 0f
                    });
                }
                return;
            }

            foreach (var solvent in solvents)
            {
                float solventRadius = solvent.Diameter / 2f;
                float maxRadius = packing.MaxSphereRadius;
                bool canFit = solventRadius <= maxRadius * 0.95f;

                float score = 0f;
                string status = "POOR";
                string bestLocation = "none";
                float voxelClusterMatch = 0f;
                float polarMatch = 0;


                if (canFit)
                {
 // ⭐ POLARIZABILITY MATCH - correctly zdefiniowane ⭐
                    polarMatch = 1.0f - Math.Min(Math.Abs(solvent.Polarizability - 0.5f) / 1.0f, 1.0f);

 // Size match
                    float sizeMatch = 1.0f - Math.Abs(solventRadius / maxRadius - 0.5f) * 2f;
                    sizeMatch = Math.Clamp(sizeMatch, 0f, 1f);

 // Charge compatibility
                    float chargeMatch = 1.0f;
                    if (solvent.IsPolar)
                    {
                        chargeMatch = 0.7f + 0.3f * (1.0f - Math.Min(Math.Abs(solvent.DipoleMoment) / 5.0f, 1.0f));
                    }

 // Hydrophobicity match
                    float hydroMatch = 1.0f - Math.Min(Math.Abs(solvent.Hydrophobicity) / 2.0f, 1.0f);

 // Shape match
                    float shapeMatch = 0.7f;

 // ⭐ NOW ALL VARIABLES ARE DEFINED ⭐
                    score = sizeMatch * 0.25f + polarMatch * 0.15f + chargeMatch * 0.15f +
                            hydroMatch * 0.15f + shapeMatch * 0.10f;

                    voxelClusterMatch = 0.5f + score * 0.5f;

                    if (score >= 0.7f)
                    {
                        status = "GOOD";
                        var bestSphere = packing.Spheres.OrderBy(s => Math.Abs(s.Radius - solventRadius)).FirstOrDefault();
                        if (bestSphere != null)
                        {
                            bestLocation = $"sphere #{bestSphere.Index} at ({bestSphere.FracX:F2}, {bestSphere.FracY:F2}, {bestSphere.FracZ:F2})";
                        }
                    }
                    else if (score >= 0.4f)
                    {
                        status = "FAIR";
                    }
                }

                float moleculeVolume = (4.0f / 3.0f) * (float)Math.PI * solventRadius * solventRadius * solventRadius;
                float maxCount = packing.UsableVolume > 0 ? packing.UsableVolume / moleculeVolume : 0f;
 // Afterlarizability match (20%)
               

                packing.SolventCompatibility.Add(new SolventGuestResult
                {
                    Name = solvent.Name,
                    Formula = solvent.Formula,
                    Volume = solvent.Volume,
                    Diameter = solvent.Diameter,
                    Score = score,
                    Status = status,
                    PolarizabilityMatch = polarMatch,
                    ChargeCompatibility = solvent.IsPolar ? 0.7f : 0.9f,
                    HydrophobicityMatch = 1.0f - Math.Min(Math.Abs(solvent.Hydrophobicity) / 2.0f, 1.0f),
                    ShapeMatch = 0.7f,
                    BestSphereLocation = bestLocation,
                    CanFit = canFit,
                    RecommendedCount = (float)Math.Floor(maxCount),
                    VoxelClusterMatch = voxelClusterMatch
                });
            }
        }

        #endregion

        #region Private Methods - QSPR

        private static void CalculateQSPRDescriptors(VdWAnalysisResults results)
        {
            if (results == null || results.CavityResults == null || results.CavityResults.Count == 0)
            {
                LogToDebug("No results for QSPR", System.Drawing.Color.Orange);
                return;
            }

            LogToDebug("Calculating QSPR descriptors...", System.Drawing.Color.LightGray);

            try
            {
 // Integrals below need increasing scales (delta > 0); the reafterrt order is descending.
                var scalesAscending = results.VdWScales.OrderBy(sc => sc).ToList();

 // I_frag: cavities >= MIN_CAVITY_VOLUME_FOR_DESCRIPTORS counted at the reference scale
 // (the one closest to VdW = 1.0), not the maximum over all scales (which is dominated
 // by sub-A^3 numerical afterckets at large scales).
                float referenceScale = scalesAscending.OrderBy(sc => Math.Abs(sc - 1.0f)).First();
                int cavitiesAtReference = 0;
                foreach (var kvp in results.CavityResults)
                {
                    if (kvp.Value == null) continue;
                    var result = kvp.Value.FirstOrDefault(r => r != null && Math.Abs(r.VdWScale - referenceScale) < 0.001f);
                    if (result != null && result.Volume >= MIN_CAVITY_VOLUME_FOR_DESCRIPTORS && !result.IsPeriodicDuplicate)
                        cavitiesAtReference++;
                }
                results.FragmentationIndex = results.AtomCount > 0 ? (float)cavitiesAtReference / results.AtomCount : 0f;

                results.CollapseScale = 2.0f;
                foreach (var scale in results.VdWScales.OrderBy(s => s))
                {
                    float totalVolume = 0f;
                    foreach (var kvp in results.CavityResults)
                    {
                        if (kvp.Value == null) continue;
                        var result = kvp.Value.FirstOrDefault(r => r != null && Math.Abs(r.VdWScale - scale) < 0.001f);
                        if (result != null && result.Volume > 0 && !result.IsPeriodicDuplicate)
                            totalVolume += result.Volume;
                    }
                    if (results.CellVolume > 0 && totalVolume / results.CellVolume < COLLAPSE_VOLUME_FRACTION)
                    {
                        results.CollapseScale = scale;
                        break;
                    }
                }

                results.ElectrostaticFrustration = 0f;
                for (int i = 0; i < scalesAscending.Count - 1; i++)
                {
                    try
                    {
                        float s1 = scalesAscending[i];
                        float s2 = scalesAscending[i + 1];
                        float delta = s2 - s1;

                        float v1 = GetTotalVolumeAtScale(results, s1);
                        float v2 = GetTotalVolumeAtScale(results, s2);
                        float avgVolume = (v1 + v2) / 2f;

                        float var1 = GetAverageChargeVarianceAtScale(results, s1);
                        float var2 = GetAverageChargeVarianceAtScale(results, s2);
                        float avgVariance = (var1 + var2) / 2f;

                        results.ElectrostaticFrustration += avgVariance * avgVolume * delta;
                    }
                    catch (Exception ex)
                    {
                        LogToDebug($"Error calculating ElectrostaticFrustration at scale {i}: {ex.Message}", System.Drawing.Color.Red);
                    }
                }

                results.HydrophobicFrustration = 0f;
                for (int i = 0; i < scalesAscending.Count - 1; i++)
                {
                    try
                    {
                        float s1 = scalesAscending[i];
                        float s2 = scalesAscending[i + 1];
                        float delta = s2 - s1;

                        float surf1 = GetTotalSurfaceAtScale(results, s1);
                        float surf2 = GetTotalSurfaceAtScale(results, s2);
                        float avgSurface = (surf1 + surf2) / 2f;

                        float var1 = GetAverageHydroVarianceAtScale(results, s1);
                        float var2 = GetAverageHydroVarianceAtScale(results, s2);
                        float avgVariance = (var1 + var2) / 2f;

                        results.HydrophobicFrustration += avgVariance * avgSurface * delta;
                    }
                    catch (Exception ex)
                    {
                        LogToDebug($"Error calculating HydrophobicFrustration at scale {i}: {ex.Message}", System.Drawing.Color.Red);
                    }
                }

                float totalZ = 0f;
                int totalVoxels = 0;
                foreach (var kvp in results.CavityResults)
                {
                    if (kvp.Value == null) continue;
                    foreach (var r in kvp.Value)
                    {
                        if (r == null) continue;
                        if (!r.IsPeriodicDuplicate && r.Volume > 0)
                        {
                            totalZ += r.MeanZEffective;
                            totalVoxels++;
                        }
                    }
                }
                results.MeanPolarizability = totalVoxels > 0 ? totalZ / totalVoxels : 0f;

                results.MaxDipoleSensitivity = 0f;
                for (int i = 0; i < scalesAscending.Count - 1; i++)
                {
                    try
                    {
                        float s1 = scalesAscending[i];
                        float s2 = scalesAscending[i + 1];
                        float delta = s2 - s1;

                        if (delta > 0)
                        {
                            float dip1 = GetTotalDipoleAtScale(results, s1);
                            float dip2 = GetTotalDipoleAtScale(results, s2);
                            float sensitivity = Math.Abs(dip2 - dip1) / delta;
                            if (sensitivity > results.MaxDipoleSensitivity)
                                results.MaxDipoleSensitivity = sensitivity;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToDebug($"Error calculating MaxDipoleSensitivity at scale {i}: {ex.Message}", System.Drawing.Color.Red);
                    }
                }

                LogToDebug($"QSPR: I_frag={results.FragmentationIndex:F6}, f_collapse={results.CollapseScale:F2}", System.Drawing.Color.LightGray);
                LogToDebug($"QSPR: Ω_Q={results.ElectrostaticFrustration:F6}, Ψ_H={results.HydrophobicFrustration:F6}", System.Drawing.Color.LightGray);
                LogToDebug($"QSPR: ⟨Z_cav⟩={results.MeanPolarizability:F4}, α_polar={results.MaxDipoleSensitivity:F6}", System.Drawing.Color.LightGray);
            }
            catch (Exception ex)
            {
                LogToDebug($"Error calculating QSPR descriptors: {ex.Message}", System.Drawing.Color.Red);
                LogToDebug($"StackTrace: {ex.StackTrace}", System.Drawing.Color.Red);
            }
        }

        #endregion

        #region Private Methods - QSPR Helpers

        private static float GetTotalVolumeAtScale(VdWAnalysisResults results, float scale)
        {
            float total = 0f;
            foreach (var kvp in results.CavityResults)
            {
                var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - scale) < 0.001f);
                if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                    total += r.Volume;
            }
            return total;
        }

        private static float GetTotalSurfaceAtScale(VdWAnalysisResults results, float scale)
        {
            float total = 0f;
            foreach (var kvp in results.CavityResults)
            {
                var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - scale) < 0.001f);
                if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                    total += r.SurfaceArea;
            }
            return total;
        }

        private static float GetAverageChargeVarianceAtScale(VdWAnalysisResults results, float scale)
        {
            float sum = 0f;
            int count = 0;
            foreach (var kvp in results.CavityResults)
            {
                var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - scale) < 0.001f);
                if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                {
                    sum += r.ChargeVariance;
                    count++;
                }
            }
            return count > 0 ? sum / count : 0f;
        }

        private static float GetAverageHydroVarianceAtScale(VdWAnalysisResults results, float scale)
        {
            float sum = 0f;
            int count = 0;
            foreach (var kvp in results.CavityResults)
            {
                var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - scale) < 0.001f);
                if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                {
                    sum += r.HydrophobicityVariance;
                    count++;
                }
            }
            return count > 0 ? sum / count : 0f;
        }

        private static float GetTotalDipoleAtScale(VdWAnalysisResults results, float scale)
        {
            float total = 0f;
            foreach (var kvp in results.CavityResults)
            {
                var r = kvp.Value.FirstOrDefault(x => Math.Abs(x.VdWScale - scale) < 0.001f);
                if (r != null && r.Volume > 0 && !r.IsPeriodicDuplicate)
                    total += r.DipoleMomentMagnitude;
            }
            return total;
        }

        #endregion

        #region Private Methods - Cavity Tracking

        private static void TrackCavities(VdWAnalysisResults results)
        {
            if (results == null || results.CavityResults == null || results.CavityResults.Count == 0)
            {
                LogToDebug("No results to track", System.Drawing.Color.Orange);
                return;
            }

            LogToDebug("Tracking cavities...", System.Drawing.Color.LightGray);

            foreach (var kvp in results.CavityResults)
            {
                try
                {
                    var track = new CavityTrackData { Id = kvp.Key };
                    float lastVolume = 0f;
                    float disappearScale = 2.0f;

                    bool hasChannels = results.CrystalChannelData != null &&
                                       results.CrystalChannelData.ContainsKey(kvp.Key) &&
                                       results.CrystalChannelData[kvp.Key].Count > 0;

                    foreach (var result in kvp.Value.OrderBy(r => r.VdWScale))
                    {
                        if (result == null) continue;

                        var snapshot = new CavitySnapshot
                        {
                            Scale = result.VdWScale,
                            Id = result.CavityId,
                            Volume = result.Volume,
                            SurfaceArea = result.SurfaceArea,
                            Sphericity = result.Sphericity,
                            VolumeToSurfaceRatio = result.VolumeToSurfaceRatio,
                            WallConnections = result.WallConnections,
                            TopologyType = result.WallConnections,
                            HasChannels = results.CrystalChannelData != null &&
                                          results.CrystalChannelData.ContainsKey(result.CavityId) &&
                                          results.CrystalChannelData[result.CavityId].Count > 0,
                            ChannelDirections = results.CrystalChannelData != null &&
                                                results.CrystalChannelData.ContainsKey(result.CavityId) ?
                                new HashSet<string>(results.CrystalChannelData[result.CavityId].Keys) :
                                new HashSet<string>(),
                            MeanCharge = result.MeanCharge,
                            ChargeVariance = result.ChargeVariance,
                            MeanHydrophobicity = result.MeanHydrophobicity,
                            HydrophobicityVariance = result.HydrophobicityVariance,
                            MeanZEffective = result.MeanZEffective,
                            DipoleMomentX = result.DipoleMomentX,
                            DipoleMomentY = result.DipoleMomentY,
                            DipoleMomentZ = result.DipoleMomentZ,
                            DipoleMomentMagnitude = result.DipoleMomentMagnitude
                        };

                        track.Snapshots[result.VdWScale] = snapshot;

                        if (result.Volume < EPSILON && lastVolume > EPSILON)
                            disappearScale = result.VdWScale;

                        lastVolume = result.Volume;
                    }

                    track.DisappearanceScale = disappearScale;
                    track.HasChannels = hasChannels;
                    track.ChannelDirections = hasChannels && results.CrystalChannelData.ContainsKey(kvp.Key) ?
                        new HashSet<string>(results.CrystalChannelData[kvp.Key].Keys) :
                        new HashSet<string>();

                    results.CavityTracks[kvp.Key] = track;
                }
                catch (Exception ex)
                {
                    LogToDebug($"Error tracking cavity {kvp.Key}: {ex.Message}", System.Drawing.Color.Red);
                }
            }
        }

        private static void DetectCriticalEvents(VdWAnalysisResults results)
        {
            LogToDebug("Detecting critical events...", System.Drawing.Color.LightGray);

            var sortedScales = results.VdWScales.OrderBy(s => s).ToList();

            for (int i = 1; i < sortedScales.Count; i++)
            {
                float prevScale = sortedScales[i - 1];
                float currScale = sortedScales[i];

                var prevCavities = results.CavityResults
                    .SelectMany(kvp => kvp.Value)
                    .Where(r => Math.Abs(r.VdWScale - prevScale) < 0.001f && !r.IsPeriodicDuplicate && r.Volume > 0)
                    .ToList();

                var currCavities = results.CavityResults
                    .SelectMany(kvp => kvp.Value)
                    .Where(r => Math.Abs(r.VdWScale - currScale) < 0.001f && !r.IsPeriodicDuplicate && r.Volume > 0)
                    .ToList();

                foreach (var cavity in prevCavities)
                {
                    if (!currCavities.Any(c => c.CavityId == cavity.CavityId))
                    {
                        results.CriticalEvents.Add(new CriticalEvent
                        {
                            Scale = currScale,
                            CavityId = cavity.CavityId,
                            Description = $"Cavity #{cavity.CavityId} disappeared (V={cavity.Volume:F2} Å³)",
                            Type = CriticalEvent.EventType.Disappearance
                        });
                    }
                }

                foreach (var cavity in currCavities)
                {
                    var prev = prevCavities.FirstOrDefault(p => p.CavityId == cavity.CavityId);
                    if (prev != null)
                    {
                        bool prevHasChannels = results.CrystalChannelData.ContainsKey(prev.CavityId) &&
                                               results.CrystalChannelData[prev.CavityId].Count > 0;
                        bool currHasChannels = results.CrystalChannelData.ContainsKey(cavity.CavityId) &&
                                               results.CrystalChannelData[cavity.CavityId].Count > 0;

                        if (prevHasChannels != currHasChannels)
                        {
                            string change = prevHasChannels ? "closed" : "opened";
                            results.CriticalEvents.Add(new CriticalEvent
                            {
                                Scale = currScale,
                                CavityId = cavity.CavityId,
                                Description = $"Cavity #{cavity.CavityId} crystal channel {change}",
                                Type = prevHasChannels ? CriticalEvent.EventType.ChannelClose : CriticalEvent.EventType.ChannelOpen
                            });
                        }
                    }
                }
            }

            LogToDebug($"Found {results.CriticalEvents.Count} critical events", System.Drawing.Color.LightGray);
        }

        #endregion

        #region Private Methods - Helpers

        private static float PopulationVariance(List<float> values)
        {
            if (values.Count < 2) return 0f;
            float mean = values.Average();
            return values.Sum(v => (v - mean) * (v - mean)) / values.Count;
        }

 /// <summary>Cavities smaller than this (A^3) are treated as numerical afterckets in QSPR descriptors.</summary>
        private const float MIN_CAVITY_VOLUME_FOR_DESCRIPTORS = 1.0f;

 /// <summary>f_collapse: total cavity volume below this fraction of the cell volume counts as collapsed.</summary>
        private const float COLLAPSE_VOLUME_FRACTION = 0.001f;

 /// <summary>Standard atomic weights (u) indexed by atomic number, Z = 1..96 (index 0 unused).</summary>
        private static readonly float[] AtomicWeights =
        {
            0f, 1.008f, 4.0026f, 6.94f, 9.0122f, 10.81f, 12.011f, 14.007f, 15.999f, 18.998f, 20.18f, 22.99f,
            24.305f, 26.982f, 28.085f, 30.974f, 32.06f, 35.45f, 39.948f, 39.098f, 40.078f, 44.956f, 47.867f,
            50.942f, 51.996f, 54.938f, 55.845f, 58.933f, 58.693f, 63.546f, 65.38f, 69.723f, 72.63f, 74.922f,
            78.971f, 79.904f, 83.798f, 85.468f, 87.62f, 88.906f, 91.224f, 92.906f, 95.95f, 98.0f, 101.07f,
            102.91f, 106.42f, 107.87f, 112.41f, 114.82f, 118.71f, 121.76f, 127.6f, 126.9f, 131.29f, 132.91f,
            137.33f, 138.91f, 140.12f, 140.91f, 144.24f, 145.0f, 150.36f, 151.96f, 157.25f, 158.93f, 162.5f,
            164.93f, 167.26f, 168.93f, 173.05f, 174.97f, 178.49f, 180.95f, 183.84f, 186.21f, 190.23f, 192.22f,
            195.08f, 196.97f, 200.59f, 204.38f, 207.2f, 208.98f, 209.0f, 210.0f, 222.0f, 223.0f, 226.0f,
            227.0f, 232.04f, 231.04f, 238.03f, 237.0f, 244.0f, 243.0f, 247.0f
        };

        private static float GetAtomicWeight(int atomicNumber)
        {
            if (atomicNumber >= 1 && atomicNumber < AtomicWeights.Length)
                return AtomicWeights[atomicNumber];
 // Z > 96: rough estimate, only to avoid a zero contribution.
            return atomicNumber > 0 ? 2.5f * atomicNumber : 0f;
        }

 /// <summary>
 /// Number of atoms in the unit cell and their total mass (u). Symmetry operations and
 /// de-duplication mirror VoxelScanner.GenerateAsymmetricAtomsWithNeighbors, so the atom count
 /// equals the scanner's "Cell atoms" and is consistent with the cell volume used for V/atom.
 /// </summary>
        private static void CalculateCellContents(Molecule molecule, out int atomsInCell, out float cellMass)
        {
            atomsInCell = 0;
            cellMass = 0f;

            if (molecule == null) return;

            var asymAtoms = molecule.SymetryEquivalents;
            var symOps = molecule.SymmetryOperations;
            if (asymAtoms == null || asymAtoms.Count == 0 || symOps == null || symOps.Count == 0) return;

            const float eps = 0.0001f;
            int maxSym = Math.Min(symOps.Count, 48);
            var seen = new HashSet<(int, int, int)>();

            foreach (var atom in asymAtoms)
            {
                int atomicNumber = VdwParameters.GetAtomicNumber(atom.name);
                if (atomicNumber <= 0) atomicNumber = 6;
                float weight = GetAtomicWeight(atomicNumber);

                for (int s = 0; s < maxSym; s++)
                {
                    var sym = symOps[s];

                    float nx = atom.x - (float)Math.Floor(atom.x);
                    float ny = atom.y - (float)Math.Floor(atom.y);
                    float nz = atom.z - (float)Math.Floor(atom.z);
                    if (nx > 1.0f - eps) nx = 0.0f;
                    if (ny > 1.0f - eps) ny = 0.0f;
                    if (nz > 1.0f - eps) nz = 0.0f;
                    if (nx < eps) nx = 0.0f;
                    if (ny < eps) ny = 0.0f;
                    if (nz < eps) nz = 0.0f;

                    float tx = 0, ty = 0, tz = 0;

                    if (sym.pos1[0] >= 0 && sym.pos1[0] < 3)
                        tx += sym.sign1[0] * (sym.pos1[0] == 0 ? nx : (sym.pos1[0] == 1 ? ny : nz));
                    if (sym.pos2[0] >= 0 && sym.pos2[0] < 3 && sym.sign2[0] != 0)
                        tx += sym.sign2[0] * (sym.pos2[0] == 0 ? nx : (sym.pos2[0] == 1 ? ny : nz));
                    tx += sym.shift[0];

                    if (sym.pos1[1] >= 0 && sym.pos1[1] < 3)
                        ty += sym.sign1[1] * (sym.pos1[1] == 0 ? nx : (sym.pos1[1] == 1 ? ny : nz));
                    if (sym.pos2[1] >= 0 && sym.pos2[1] < 3 && sym.sign2[1] != 0)
                        ty += sym.sign2[1] * (sym.pos2[1] == 0 ? nx : (sym.pos2[1] == 1 ? ny : nz));
                    ty += sym.shift[1];

                    if (sym.pos1[2] >= 0 && sym.pos1[2] < 3)
                        tz += sym.sign1[2] * (sym.pos1[2] == 0 ? nx : (sym.pos1[2] == 1 ? ny : nz));
                    if (sym.pos2[2] >= 0 && sym.pos2[2] < 3 && sym.sign2[2] != 0)
                        tz += sym.sign2[2] * (sym.pos2[2] == 0 ? nx : (sym.pos2[2] == 1 ? ny : nz));
                    tz += sym.shift[2];

                    tx = tx - (float)Math.Floor(tx);
                    ty = ty - (float)Math.Floor(ty);
                    tz = tz - (float)Math.Floor(tz);
                    if (tx > 1.0f - eps) tx = 0.0f;
                    if (ty > 1.0f - eps) ty = 0.0f;
                    if (tz > 1.0f - eps) tz = 0.0f;
                    if (tx < eps) tx = 0.0f;
                    if (ty < eps) ty = 0.0f;
                    if (tz < eps) tz = 0.0f;

                    var key = ((int)Math.Round(tx * 10000), (int)Math.Round(ty * 10000), (int)Math.Round(tz * 10000));
                    if (seen.Add(key))
                    {
                        atomsInCell++;
                        cellMass += weight;
                    }
                }
            }
        }

        #endregion

        #region Public Methods - Report Generation

 /// <summary>
 /// Generates a full text reafterrt of the analysis results.
 /// </summary>
        public static string GenerateFullReport(VdWAnalysisResults results)
        {
            if (results == null || results.CavityResults.Count == 0)
                return "No results to report.";

            var sb = new StringBuilder();
            var culture = CultureInfo.InvariantCulture;

 // ============================================================
 // HEADER
 // ============================================================
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine($"VDW SCALING ANALYSIS");
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();
            sb.AppendLine($"Cell volume:    {results.CellVolume:F4} Å³");
            sb.AppendLine($"Atoms in cell:  {results.AtomCount}");
            sb.AppendLine($"Cell mass:      {results.CellMass:F2} u");
            sb.AppendLine();
            sb.AppendLine($"Analysis time:  {results.TotalProcessingTimeMs / 1000:F2} s");
            sb.AppendLine($"Scales tested:  {string.Join(", ", results.VdWScales)}");
            sb.AppendLine($"Anisotropic:    {Flags.Instance.UseThermalMotion}");
            sb.AppendLine();

 // ============================================================
 // QSPR DESCRIPTORS
 // ============================================================
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("QSPR DESCRIPTORS SUMMARY");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();
            sb.AppendLine($"I_frag (Fragmentation Index):        {results.FragmentationIndex:F6}");
            sb.AppendLine($"  → Cavities (>= 1 Å³, at the reference scale VdW≈1.0) per atom in the cell");
            sb.AppendLine();
            sb.AppendLine($"f_collapse (Critical Collapse):      {results.CollapseScale:F2}");
            sb.AppendLine($"  → Smallest VdW scale at which total cavity volume falls below 0.1% of the cell volume (2.00 = not reached)");
            sb.AppendLine();
            sb.AppendLine($"Ω_Q (Electrostatic Frustration):     {results.ElectrostaticFrustration:F6}");
            sb.AppendLine($"  → Total charge distribution inhomogeneity on cavity surfaces");
            sb.AppendLine();
            sb.AppendLine($"Ψ_H (Hydrophobic Frustration):       {results.HydrophobicFrustration:F6}");
            sb.AppendLine($"  → Total hydrophobicity inhomogeneity on cavity surfaces");
            sb.AppendLine();
            sb.AppendLine($"⟨Z_cav⟩ (Mean Polarizability):       {results.MeanPolarizability:F4}");
            sb.AppendLine($"  → Average polarizability of cavities; measure of polarizability capacity");
            sb.AppendLine();
            sb.AppendLine($"α_polar (Max Dipole Sensitivity):    {results.MaxDipoleSensitivity:F6}");
            sb.AppendLine($"  → Maximum dipole sensitivity; response to external electric field");
            sb.AppendLine();

 // ============================================================
 // SCALE SUMMARY
 // ============================================================
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine("SCALE SUMMARY");
            sb.AppendLine("-".PadRight(80, '-'));
            sb.AppendLine();

            sb.AppendLine($"{"Scale",6} {"Cav",5} {"Vol(Å³)",12} {"Surf(Å²)",12} {"V/S",10} {"Vol/Vcell",12} {"Channels",10}");
            sb.AppendLine(new string('-', 80));

            var scaleData = new List<(float scale, int cav, float vol, float surf, float volPerCell)>();

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

                float volPerCell = results.CellVolume > 0 ? totalV / results.CellVolume : 0f;
                float vOverS = totalS > 0 ? totalV / totalS : 0f;

                scaleData.Add((scale, count, totalV, totalS, volPerCell));

                sb.AppendLine($"{scale,6:F2} {count,5} {totalV,12:F4} {totalS,12:F4} {vOverS,10:F3} {volPerCell,12:F6} {channels,10}");
            }
            sb.AppendLine();

 // ============================================================
 // CHANNEL ANALYSIS
 // ============================================================
            if (results.CrystalChannelData != null && results.CrystalChannelData.Count > 0)
            {
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine("CRYSTAL CHANNELS");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine();

                var stats = results.GetChannelStatistics();
                sb.AppendLine($"Total channels: {results.GetTotalChannelCount()}");
                sb.AppendLine($"Cavities with channels: {results.GetCavitiesWithChannelsCount()}");
                sb.AppendLine();
                sb.AppendLine($"Network type: {results.GetChannelNetworkDescription()}");
                sb.AppendLine();

                sb.AppendLine($"{"Cav",4} {"Dir",4} {"Length(Å)",8} {"AvgW(Å)",8} {"MinW(Å)",8} {"MaxW(Å)",8} {"EffW(Å)",8} {"Hel(°)",8}");
                sb.AppendLine(new string('-', 70));

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
                            $"{result.Helicity,8:F2}");
                    }
                }
                sb.AppendLine();
            }

 // ============================================================
 // SPHERE PACKING
 // ============================================================
            if (results.SpherePacking.Count > 0)
            {
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine("SPHERE PACKING");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine();

                sb.AppendLine($"{"Cav",4} {"Spheres",8} {"Max R(Å)",12} {"Diameter(Å)",12} {"Packing",12} {"Vol(Å³)",12}");
                sb.AppendLine(new string('-', 65));

                foreach (var kvp in results.SpherePacking.OrderBy(k => k.Key))
                {
                    var packing = kvp.Value;
                    sb.AppendLine($"{kvp.Key,4} {packing.SphereCount,8} {packing.MaxSphereRadius,12:F3} " +
                        $"{packing.EffectiveDiameter,12:F3} {packing.PackingEfficiency,12:F3} " +
                        $"{packing.UsableVolume,12:F2}");
                }
                sb.AppendLine();
            }

 // ============================================================
 // CRITICAL EVENTS
 // ============================================================
            if (results.CriticalEvents.Count > 0)
            {
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine("CRITICAL EVENTS");
                sb.AppendLine("-".PadRight(80, '-'));
                sb.AppendLine();

                foreach (var evt in results.CriticalEvents.OrderBy(e => e.Scale))
                {
                    sb.AppendLine($"Scale {evt.Scale:F2}: {evt.Description}");
                }
                sb.AppendLine();
            }

 // ============================================================
 // FOOTER
 // ============================================================
            sb.AppendLine();
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine($"Generated: {DateTime.Now}");
            sb.AppendLine("=".PadRight(80, '='));

            return sb.ToString();
        }

        #endregion

        #region QSPR Weights

        public static class QSPRWeights
        {
            public static float w_V = 0.15f;
            public static float w_Q = 0.25f;
            public static float w_H = 0.20f;
            public static float w_frag = 0.30f;
            public static float w_Z = 0.10f;
            public static float E0 = 0.0f;
        }

        #endregion
    }
}