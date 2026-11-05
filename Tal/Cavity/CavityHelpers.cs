// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: CavityHelpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Diagnostics;
using SysVector3 = System.Numerics.Vector3;

namespace Tal.Cavity
{
 /// <summary>
 /// Helper class for cavity analysis, statistics, and formatting.
 /// All surface areas and volumes are calculated from RAW mesh data (before smoothing).
 /// Errors are calculated using proper error propagation from voxel grid resolution.
 /// Channels are analyzed semi-periodically using CavityPathFinder with wall wrapping.
 /// </summary>
    public class CavityHelpers
    {
        #region Fields

        private Molecule _molecule;
        private List<Cavity> _cavities = new List<Cavity>();

        #endregion

        #region Constructor

        public CavityHelpers(Molecule molecule)
        {
            _molecule = molecule;
            _cavities = new List<Cavity>();
        }

        #endregion

        #region Cavity Storage

 /// <summary>Main list of all detected cavities.</summary>
        public List<Cavity> Cavities
        {
            get => _cavities;
            set => _cavities = value ?? new List<Cavity>();
        }

 /// <summary>Clears the cavity list.</summary>
        public void ClearCavities()
        {
            _cavities.Clear();
        }

 /// <summary>Sets the cavity list.</summary>
        public void SetCavities(List<Cavity> cavities)
        {
            _cavities = cavities ?? new List<Cavity>();
        }

 /// <summary>Adds a single cavity.</summary>
        public void AddCavity(Cavity cavity)
        {
            if (cavity != null)
                _cavities.Add(cavity);
        }

 /// <summary>Adds multiple cavities.</summary>
        public void AddCavities(IEnumerable<Cavity> cavities)
        {
            if (cavities != null)
                _cavities.AddRange(cavities);
        }

 /// <summary>Checks if there are any valid cavities.</summary>
        public bool HasValidCavities()
        {
            if (_cavities == null || _cavities.Count == 0)
                return false;
            return _cavities.Any(c => !c.IsPeriodicDuplicate && c.Volume > 0);
        }

 /// <summary>Gets a cavity by its ID.</summary>
        public Cavity GetCavityById(int id)
        {
            if (_cavities == null) return null;
            return _cavities.FirstOrDefault(c => c.Id == id);
        }

 /// <summary>Gets channel directions for a cavity.</summary>
        public List<string> GetChannelDirections(Cavity cavity)
        {
            if (cavity == null || cavity.CrystalChannelResults == null)
                return new List<string>();

            return cavity.CrystalChannelResults
                .Where(kvp => kvp.Value?.IsConnected == true)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        #endregion

        #region Total Statistics

 /// <summary>
 /// Calculates total volume of all non-duplicate cavities.
 /// Uses stored Volume values from cavity detection.
 /// </summary>
        public float GetTotalVoidsVolumeFromCavities(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return 0f;

            float total = 0f;
            foreach (var cavity in cavities)
            {
                if (!cavity.IsPeriodicDuplicate)
                    total += cavity.Volume;
            }
            return total;
        }

 /// <summary>
 /// Calculates total volume error using proper error propagation.
 /// sigma_total = sqrt(Sum sigma_i^2) where sigma_i is the volume error of each cavity.
 /// </summary>
        public float GetTotalVoidsVolumeErrorFromCavities(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return 0f;

            float errorSquared = 0f;
            foreach (var cavity in cavities)
            {
                if (!cavity.IsPeriodicDuplicate)
                {
                    float err = CalculateVolumeError(cavity);
                    errorSquared += err * err;
                }
            }
            return (float)Math.Sqrt(errorSquared);
        }

 /// <summary>
 /// Calculates total surface area of all non-duplicate cavities.
 /// Uses SurfaceArea values calculated from RAW mesh data.
 /// </summary>
        public float GetTotalSurfaceAreaFromCavities(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return 0f;

            float total = 0f;
            foreach (var cavity in cavities)
            {
                if (!cavity.IsPeriodicDuplicate)
                    total += CalculateSurfaceArea(cavity);
            }
            return total;
        }

 /// <summary>
 /// Calculates total surface area error using proper error propagation.
 /// sigma_total = sqrt(Sum sigma_i^2) where sigma_i is the surface error of each cavity.
 /// </summary>
        public float GetTotalSurfaceAreaErrorFromCavities(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return 0f;

            float errorSquared = 0f;
            foreach (var cavity in cavities)
            {
                if (!cavity.IsPeriodicDuplicate)
                {
                    float err = CalculateSurfaceAreaError(cavity);
                    errorSquared += err * err;
                }
            }
            return (float)Math.Sqrt(errorSquared);
        }

 /// <summary>
 /// Calculates volume-to-surface ratio (V/S) for all cavities.
 /// </summary>
        public float GetVolumeToSurfaceRatioFromCavities(List<Cavity> cavities)
        {
            float volume = GetTotalVoidsVolumeFromCavities(cavities);
            float surface = GetTotalSurfaceAreaFromCavities(cavities);
            return surface > 0 ? volume / surface : 0f;
        }

 /// <summary>
 /// Calculates error of volume-to-surface ratio using proper error propagation.
 /// For ratio R = V/S: sigma_R = R * sqrt((sigma_V/V)^2 + (sigma_S/S)^2)
 /// </summary>
        public float GetVolumeToSurfaceRatioErrorFromCavities(List<Cavity> cavities)
        {
            float volume = GetTotalVoidsVolumeFromCavities(cavities);
            float volumeErr = GetTotalVoidsVolumeErrorFromCavities(cavities);
            float surface = GetTotalSurfaceAreaFromCavities(cavities);
            float surfaceErr = GetTotalSurfaceAreaErrorFromCavities(cavities);

            if (surface <= 0 || volume <= 0) return 0f;

            float ratio = volume / surface;
            float relErr = (float)Math.Sqrt(
                (volumeErr / volume) * (volumeErr / volume) +
                (surfaceErr / surface) * (surfaceErr / surface)
            );
            return ratio * relErr;
        }

        #endregion

        #region Single Cavity Statistics

 /// <summary>Returns the volume of a single cavity.</summary>
        public float CalculateVolume(Cavity cavity)
        {
            return cavity?.Volume ?? 0f;
        }

 /// <summary>
 /// Calculates volume error for a single cavity based on voxel grid resolution.
 /// </summary>
        public float CalculateVolumeError(Cavity cavity)
        {
            if (cavity == null || cavity.Volume <= 0)
                return 0.01f;

            float voxelVolume = CalculateVoxelVolume(cavity);
            if (voxelVolume <= 0)
                return 0.01f;

            float voxelCount = cavity.Volume / voxelVolume;
            if (voxelCount <= 0)
                return 0.01f;

            float volumeError = voxelVolume * (float)Math.Sqrt(voxelCount);
            return Math.Max(volumeError, 0.01f);
        }

 /// <summary>
 /// Calculates the volume of a single voxel from the grid parameters.
 /// For non-orthogonal unit cells, uses the determinant of the transformation matrix.
 /// </summary>
        private float CalculateVoxelVolume(Cavity cavity)
        {
            if (cavity == null)
                return 0f;

 // Use CellParameters - handles triclinic systems
            if (CellParameters.IsInitialized)
            {
                float vol = CellParameters.GetVoxelVolume();
                return vol > 0 ? vol : 0.001f;
            }

 // Fallback: from Grid
            if (cavity.Grid != null)
            {
                float stepX = cavity.Grid.StepX;
                float stepY = cavity.Grid.StepY;
                float stepZ = cavity.Grid.StepZ;

                float stepX_Angstroms = stepX * cavity.Grid.VecA.Length();
                float stepY_Angstroms = stepY * cavity.Grid.VecB.Length();
                float stepZ_Angstroms = stepZ * cavity.Grid.VecC.Length();

                float vol = stepX_Angstroms * stepY_Angstroms * stepZ_Angstroms;
                return vol > 0 ? vol : 0.001f;
            }

 // Last resort
            if (cavity.Voxels != null && cavity.Voxels.Count > 0 && cavity.Volume > 0)
            {
                return cavity.Volume / cavity.Voxels.Count;
            }

            return 0.001f;
        }

 /// <summary>
 /// Calculates surface area for a single cavity.
 /// Uses RAW mesh data (RawVertices and RawIndices) - NOT smoothed data.
 /// </summary>
        public float CalculateSurfaceArea(Cavity cavity)
        {
            if (cavity == null)
                return 0f;

 // Use stored value (calculated from RAW mesh)
            if (cavity.SurfaceArea > 0)
                return cavity.SurfaceArea;

 // If not stored, calculate from raw mesh data
            if (cavity.RawTrianglesGenerated &&
                cavity.RawVertices != null && cavity.RawVertices.Count > 0 &&
                cavity.RawIndices != null && cavity.RawIndices.Count > 0)
            {
                float surface = 0f;
                var verts = cavity.RawVertices;
                var indices = cavity.RawIndices;

                for (int i = 0; i < indices.Count; i += 3)
                {
                    int i0 = indices[i];
                    int i1 = indices[i + 1];
                    int i2 = indices[i + 2];

                    if (i0 < verts.Count && i1 < verts.Count && i2 < verts.Count)
                    {
                        var v0 = verts[i0];
                        var v1 = verts[i1];
                        var v2 = verts[i2];

                        var e1 = v1 - v0;
                        var e2 = v2 - v0;
                        var cross = new SysVector3(
                            e1.Y * e2.Z - e1.Z * e2.Y,
                            e1.Z * e2.X - e1.X * e2.Z,
                            e1.X * e2.Y - e1.Y * e2.X
                        );
                        surface += cross.Length() * 0.5f;
                    }
                }

                if (surface > 0)
                {
                    cavity.SurfaceArea = surface;
                    return surface;
                }
            }

 // Fallback: approximate surface from volume (sphere approximation)
            if (cavity.Volume > 0)
            {
                float approxSurface = 4.835f * (float)Math.Pow(cavity.Volume, 2f / 3f);
                cavity.SurfaceArea = approxSurface;
                return approxSurface;
            }

            return 0f;
        }

 /// <summary>
 /// Calculates surface area error for a single cavity.
 /// </summary>
        public float CalculateSurfaceAreaError(Cavity cavity)
        {
            if (cavity == null)
                return 0.01f;

            float voxelVolume = CalculateVoxelVolume(cavity);
            if (voxelVolume <= 0)
                return 0.01f;

            float voxelArea = (float)Math.Pow(voxelVolume, 2.0 / 3.0);

            int triangleCount = 0;
            if (cavity.RawIndices != null)
                triangleCount = cavity.RawIndices.Count / 3;

            if (triangleCount > 0)
            {
                float surfaceError = voxelArea * (float)Math.Sqrt(triangleCount);
                return Math.Max(surfaceError, 0.01f);
            }

            float surface = CalculateSurfaceArea(cavity);
            return Math.Max(surface * 0.01f, 0.01f);
        }

 /// <summary>
 /// Calculates sphericity of a cavity (0-1, 1 = perfect sphere).
 /// </summary>
        public float CalculateSphericity(Cavity cavity)
        {
            if (cavity == null || cavity.Volume <= 0)
                return 0f;

            if (cavity.Sphericity > 0)
                return cavity.Sphericity;

            float surface = CalculateSurfaceArea(cavity);
            if (surface <= 0)
                return 0f;

            float volumeTerm = (float)(36.0 * Math.PI * cavity.Volume * cavity.Volume);
            float numerator = (float)Math.Pow(volumeTerm, 1.0 / 3.0);
            float sphericity = numerator / surface;

            cavity.Sphericity = sphericity;
            return sphericity;
        }

        #endregion

        #region Channel Methods

 /// <summary>
 /// Gets a string describing the crystal channels (semi-periodic).
 /// Uses CrystalChannelResults from CavityPathFinder.
 /// </summary>
        public string GetChannelString(Cavity cavity)
        {
            if (cavity == null) return "none";

            if (cavity.CrystalChannelResults == null || cavity.CrystalChannelResults.Count == 0)
                return "none";

            return string.Join(",", cavity.CrystalChannelResults.Keys);
        }

 /// <summary>
 /// Gets a string describing the crystal channels with additional details.
 /// </summary>
        public string GetDetailedChannelString(Cavity cavity)
        {
            if (cavity == null) return "none";

            if (cavity.CrystalChannelResults == null || cavity.CrystalChannelResults.Count == 0)
                return "none";

            var parts = new List<string>();
            foreach (var kvp in cavity.CrystalChannelResults)
            {
                var result = kvp.Value;
                if (result != null && result.IsConnected)
                {
                    parts.Add($"{kvp.Key}(L={result.TotalLength:F1} A, H={result.Helicity:F1}°)");
                }
                else
                {
                    parts.Add($"{kvp.Key}(disconnected)");
                }
            }
            return string.Join(", ", parts);
        }

 /// <summary>
 /// Checks if a cavity has channels in a specific direction.
 /// </summary>
        public bool HasChannelInDirection(Cavity cavity, string direction)
        {
            if (cavity == null || cavity.CrystalChannelResults == null)
                return false;

            return cavity.CrystalChannelResults.ContainsKey(direction) &&
                   cavity.CrystalChannelResults[direction]?.IsConnected == true;
        }

 /// <summary>
 /// Gets the number of crystal channels for a cavity.
 /// </summary>
        public int GetChannelCount(Cavity cavity)
        {
            if (cavity == null || cavity.CrystalChannelResults == null)
                return 0;

            return cavity.CrystalChannelResults.Count(kvp => kvp.Value?.IsConnected == true);
        }

 /// <summary>
 /// Gets the total length of all channels for a cavity.
 /// </summary>
        public float GetTotalChannelLength(Cavity cavity)
        {
            if (cavity == null || cavity.CrystalChannelResults == null)
                return 0f;

            float total = 0f;
            foreach (var kvp in cavity.CrystalChannelResults)
            {
                if (kvp.Value?.IsConnected == true)
                    total += kvp.Value.TotalLength;
            }
            return total;
        }

 /// <summary>
 /// Gets the average helicity of all channels for a cavity.
 /// </summary>
        public float GetAverageChannelHelicity(Cavity cavity)
        {
            if (cavity == null || cavity.CrystalChannelResults == null)
                return 0f;

            float sum = 0f;
            int count = 0;
            foreach (var kvp in cavity.CrystalChannelResults)
            {
                if (kvp.Value?.IsConnected == true)
                {
                    sum += kvp.Value.Helicity;
                    count++;
                }
            }
            return count > 0 ? sum / count : 0f;
        }

 /// <summary>
 /// Gets a summary of channel properties for a cavity.
 /// </summary>
        public string GetChannelSummary(Cavity cavity)
        {
            if (cavity == null) return "No cavity";

            if (!cavity.HasCrystalChannels)
                return "No channels";

            int count = GetChannelCount(cavity);
            float totalLen = GetTotalChannelLength(cavity);
            float avgHel = GetAverageChannelHelicity(cavity);

            return $"{count} channels, total length: {totalLen:F2} A, avg helicity: {avgHel:F2}°";
        }

 /// <summary>
 /// Gets a string describing the walls of a cavity.
 /// </summary>
        public string GetWallString(Cavity cavity)
        {
            if (cavity == null) return "0";

            int connections = cavity.WallConnections;
            if (connections == 0) return "0";

            string walls = "";
            if (cavity.TouchesXMin) walls += "-x";
            if (cavity.TouchesXMax) walls += "+x";
            if (cavity.TouchesYMin) walls += "-y";
            if (cavity.TouchesYMax) walls += "+y";
            if (cavity.TouchesZMin) walls += "-z";
            if (cavity.TouchesZMax) walls += "+z";

            return walls;
        }

 /// <summary>
 /// Gets the toafterlogy type description for a cavity.
 /// </summary>
        public string GetTopologyDescription(Cavity cavity)
        {
            if (cavity == null) return "unknown";

            int walls = cavity.WallConnections;
            if (walls == 0) return "closed (0 walls)";
            if (walls == 1) return "touches 1 wall";
            if (walls == 2) return "touches 2 walls";
            if (walls == 3) return "touches 3 walls";
            if (walls == 4) return "touches 4 walls";
            if (walls == 5) return "touches 5 walls";
            if (walls == 6) return "touches 6 walls";
            return $"touches {walls} walls";
        }

 /// <summary>
 /// Gets color based on channel presence.
 /// </summary>
        public Microsoft.Xna.Framework.Color GetChannelColor(Cavity cavity)
        {
            if (cavity == null) return Microsoft.Xna.Framework.Color.Gray;

            if (cavity.HasCrystalChannels)
                return Microsoft.Xna.Framework.Color.LightGreen;

            return Microsoft.Xna.Framework.Color.Gray;
        }

 /// <summary>
 /// Gets color based on toafterlogy type.
 /// </summary>
        public Microsoft.Xna.Framework.Color GetTopologyColor(Cavity cavity)
        {
            if (cavity == null) return Microsoft.Xna.Framework.Color.Gray;

            int walls = cavity.WallConnections;
            switch (walls)
            {
                case 0: return Microsoft.Xna.Framework.Color.Green;
                case 1: return Microsoft.Xna.Framework.Color.Yellow;
                case 2: return Microsoft.Xna.Framework.Color.Orange;
                case 3: return Microsoft.Xna.Framework.Color.Red;
                case 4: return Microsoft.Xna.Framework.Color.Magenta;
                case 5: return Microsoft.Xna.Framework.Color.Cyan;
                case 6: return Microsoft.Xna.Framework.Color.White;
                default: return Microsoft.Xna.Framework.Color.Gray;
            }
        }

        #endregion

        #region Sorting and Filtering

 /// <summary>Sorts cavities by volume.</summary>
        public List<Cavity> SortCavitiesByVolume(List<Cavity> cavities, bool descending = true)
        {
            if (cavities == null || cavities.Count == 0)
                return new List<Cavity>();

            var sorted = new List<Cavity>(cavities);
            if (descending)
                sorted.Sort((a, b) => b.Volume.CompareTo(a.Volume));
            else
                sorted.Sort((a, b) => a.Volume.CompareTo(b.Volume));

            return sorted;
        }

 /// <summary>Sorts cavities by surface area.</summary>
        public List<Cavity> SortCavitiesBySurfaceArea(List<Cavity> cavities, bool descending = true)
        {
            if (cavities == null || cavities.Count == 0)
                return new List<Cavity>();

            var sorted = new List<Cavity>(cavities);
            if (descending)
                sorted.Sort((a, b) => b.SurfaceArea.CompareTo(a.SurfaceArea));
            else
                sorted.Sort((a, b) => a.SurfaceArea.CompareTo(b.SurfaceArea));

            return sorted;
        }

 /// <summary>Sorts cavities by sphericity.</summary>
        public List<Cavity> SortCavitiesBySphericity(List<Cavity> cavities, bool descending = true)
        {
            if (cavities == null || cavities.Count == 0)
                return new List<Cavity>();

            var sorted = new List<Cavity>(cavities);
            if (descending)
                sorted.Sort((a, b) => b.Sphericity.CompareTo(a.Sphericity));
            else
                sorted.Sort((a, b) => a.Sphericity.CompareTo(b.Sphericity));

            return sorted;
        }

 /// <summary>Sorts cavities by channel count.</summary>
        public List<Cavity> SortCavitiesByChannelCount(List<Cavity> cavities, bool descending = true)
        {
            if (cavities == null || cavities.Count == 0)
                return new List<Cavity>();

            var sorted = new List<Cavity>(cavities);
            if (descending)
                sorted.Sort((a, b) => GetChannelCount(b).CompareTo(GetChannelCount(a)));
            else
                sorted.Sort((a, b) => GetChannelCount(a).CompareTo(GetChannelCount(b)));

            return sorted;
        }

 /// <summary>Sorts cavities by total channel length.</summary>
        public List<Cavity> SortCavitiesByChannelLength(List<Cavity> cavities, bool descending = true)
        {
            if (cavities == null || cavities.Count == 0)
                return new List<Cavity>();

            var sorted = new List<Cavity>(cavities);
            if (descending)
                sorted.Sort((a, b) => GetTotalChannelLength(b).CompareTo(GetTotalChannelLength(a)));
            else
                sorted.Sort((a, b) => GetTotalChannelLength(a).CompareTo(GetTotalChannelLength(b)));

            return sorted;
        }

 /// <summary>Filters cavities by minimum volume.</summary>
        public List<Cavity> FilterCavitiesByMinVolume(List<Cavity> cavities, float minVolume)
        {
            if (cavities == null || cavities.Count == 0)
                return new List<Cavity>();

            return cavities.Where(c => c.Volume >= minVolume).ToList();
        }

 /// <summary>Filters cavities by maximum volume.</summary>
        public List<Cavity> FilterCavitiesByMaxVolume(List<Cavity> cavities, float maxVolume)
        {
            if (cavities == null || cavities.Count == 0)
                return new List<Cavity>();

            return cavities.Where(c => c.Volume <= maxVolume).ToList();
        }

 /// <summary>Filters cavities by channel status.</summary>
        public List<Cavity> FilterChannelCavities(List<Cavity> cavities, bool onlyChannels = true)
        {
            if (cavities == null || cavities.Count == 0)
                return new List<Cavity>();

            return cavities.Where(c => c.HasCrystalChannels == onlyChannels).ToList();
        }

 /// <summary>Filters cavities by wall connections.</summary>
        public List<Cavity> FilterPeriodicCavities(List<Cavity> cavities, bool onlyPeriodic = true)
        {
            if (cavities == null || cavities.Count == 0)
                return new List<Cavity>();

            return cavities.Where(c => (c.WallConnections > 0) == onlyPeriodic).ToList();
        }

 /// <summary>Filters cavities by specific channel direction.</summary>
        public List<Cavity> FilterCavitiesByChannelDirection(List<Cavity> cavities, string direction)
        {
            if (cavities == null || cavities.Count == 0)
                return new List<Cavity>();

            return cavities.Where(c => HasChannelInDirection(c, direction)).ToList();
        }

        #endregion

        #region Extrema Finding

 /// <summary>Gets the smallest cavity by volume.</summary>
        public Cavity GetSmallestCavity(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return null;

            float minVolume = float.MaxValue;
            Cavity smallest = null;
            foreach (var c in cavities)
            {
                if (c.Volume < minVolume && !c.IsPeriodicDuplicate)
                {
                    minVolume = c.Volume;
                    smallest = c;
                }
            }
            return smallest;
        }

 /// <summary>Gets the largest cavity by volume.</summary>
        public Cavity GetLargestCavity(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return null;

            float maxVolume = float.MinValue;
            Cavity largest = null;
            foreach (var c in cavities)
            {
                if (c.Volume > maxVolume && !c.IsPeriodicDuplicate)
                {
                    maxVolume = c.Volume;
                    largest = c;
                }
            }
            return largest;
        }

 /// <summary>Gets the cavity with the largest surface area.</summary>
        public Cavity GetLargestSurfaceCavity(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return null;

            float maxSurface = float.MinValue;
            Cavity largest = null;
            foreach (var c in cavities)
            {
                float surface = CalculateSurfaceArea(c);
                if (surface > maxSurface && !c.IsPeriodicDuplicate)
                {
                    maxSurface = surface;
                    largest = c;
                }
            }
            return largest;
        }

 /// <summary>Gets the most spherical cavity.</summary>
        public Cavity GetMostSphericalCavity(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return null;

            float maxSphericity = float.MinValue;
            Cavity mostSpherical = null;
            foreach (var c in cavities)
            {
                float sphericity = CalculateSphericity(c);
                if (sphericity > maxSphericity && !c.IsPeriodicDuplicate)
                {
                    maxSphericity = sphericity;
                    mostSpherical = c;
                }
            }
            return mostSpherical;
        }

 /// <summary>Gets the cavity with most channels.</summary>
        public Cavity GetMostChanneledCavity(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return null;

            int maxChannels = 0;
            Cavity mostChanneled = null;
            foreach (var c in cavities)
            {
                int count = GetChannelCount(c);
                if (count > maxChannels && !c.IsPeriodicDuplicate)
                {
                    maxChannels = count;
                    mostChanneled = c;
                }
            }
            return mostChanneled;
        }

 /// <summary>Gets a string with the minimum cavity volume information.</summary>
        public string GetMinCavityVolumeString(string format = "F2")
        {
            if (_cavities == null || _cavities.Count == 0)
                return "Min volume: none";

            var smallest = GetSmallestCavity(_cavities);
            if (smallest == null)
                return "Min volume: none";

            return $"Min volume: {smallest.Volume.ToString(format)} A3 (cavity #{smallest.Id})";
        }

        #endregion

        #region Crystallographic Formatting

 /// <summary>
 /// Formats a value with error in crystallographic convention.
 /// Examples: 4.87 +/- 0.02 -> "4.87(2)", 4.87 +/- 0.14 -> "4.8(1)"
 /// </summary>
        public string FormatValueWithError(float value, float error)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return "N/A";

            if (float.IsNaN(error) || float.IsInfinity(error) || error <= 0)
                return value.ToString("F2");

            int errorDigits = (int)Math.Floor(Math.Log10(error));
            int roundedError = (int)Math.Round(error / (float)Math.Pow(10, errorDigits));

            if (roundedError >= 10)
            {
                roundedError = 1;
                errorDigits++;
            }

            int decimalPlaces = Math.Max(0, -errorDigits);
            float roundedValue = (float)Math.Round(value, decimalPlaces);

            if (errorDigits >= 0)
            {
                return $"{roundedValue:F0}({roundedError})";
            }
            else
            {
                string format = $"F{decimalPlaces}";
                return $"{roundedValue.ToString(format)}({roundedError})";
            }
        }

 /// <summary>Formats a value with error and unit.</summary>
        public string FormatValueWithErrorAndUnit(float value, float error, string unit)
        {
            string formatted = FormatValueWithError(value, error);
            if (string.IsNullOrEmpty(unit))
                return formatted;
            return $"{formatted} {unit}";
        }

        #endregion

        #region Summary and Export

 /// <summary>Updates cavity statistics (ensures all values are computed).</summary>
        public void UpdateCavityStatistics(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return;

            foreach (var cavity in cavities)
            {
                if (cavity.SurfaceArea <= 0 && cavity.RawTrianglesGenerated)
                {
                    CalculateSurfaceArea(cavity);
                }

                if (cavity.Sphericity <= 0 && cavity.Volume > 0)
                {
                    CalculateSphericity(cavity);
                }
            }
        }

 /// <summary>Gets a basic statistics summary as a string.</summary>
        public string GetStatisticsSummary()
        {
            if (_cavities == null || _cavities.Count == 0)
                return "No cavities detected";

            float volume = GetTotalVoidsVolumeFromCavities(_cavities);
            float surface = GetTotalSurfaceAreaFromCavities(_cavities);
            float ratio = GetVolumeToSurfaceRatioFromCavities(_cavities);

            int primaryCount = 0;
            int duplicateCount = 0;
            int channelCount = 0;

            foreach (var c in _cavities)
            {
                if (c.IsPeriodicDuplicate)
                    duplicateCount++;
                else
                    primaryCount++;

                if (c.HasCrystalChannels)
                    channelCount++;
            }

            return $"Cavities: {primaryCount} primary, {duplicateCount} duplicates, {channelCount} channels | " +
                   $"Volume: {volume:F2} A3 | Surface: {surface:F2} A2 | V/S: {ratio:F3} A";
        }

 /// <summary>Gets a detailed statistics summary with channel info.</summary>
        public string GetDetailedStatisticsSummary()
        {
            if (_cavities == null || _cavities.Count == 0)
                return "No cavities detected";

            float volume = GetTotalVoidsVolumeFromCavities(_cavities);
            float surface = GetTotalSurfaceAreaFromCavities(_cavities);
            float ratio = GetVolumeToSurfaceRatioFromCavities(_cavities);

            float avgSphericity = 0f;
            int sphericityCount = 0;
            float totalChannelLen = 0f;
            int cavitiesWithChannels = 0;

            foreach (var c in _cavities)
            {
                if (!c.IsPeriodicDuplicate)
                {
                    avgSphericity += CalculateSphericity(c);
                    sphericityCount++;

                    if (c.HasCrystalChannels)
                    {
                        cavitiesWithChannels++;
                        totalChannelLen += GetTotalChannelLength(c);
                    }
                }
            }
            avgSphericity = sphericityCount > 0 ? avgSphericity / sphericityCount : 0f;

            int primaryCount = _cavities.Count(c => !c.IsPeriodicDuplicate);
            int duplicateCount = _cavities.Count(c => c.IsPeriodicDuplicate);

            return $"Cavities: {primaryCount} primary, {duplicateCount} duplicates, {cavitiesWithChannels} with channels | " +
                   $"Volume: {volume:F2} A3 | Surface: {surface:F2} A2 | V/S: {ratio:F3} A | " +
                   $"Avg Sphericity: {avgSphericity:F3} | Total channel length: {totalChannelLen:F2} A";
        }

 /// <summary>Gets statistics as a dictionary.</summary>
        public Dictionary<string, object> GetStatisticsDictionary()
        {
            var stats = new Dictionary<string, object>();

            if (_cavities == null || _cavities.Count == 0)
            {
                stats["HasCavities"] = false;
                return stats;
            }

            stats["HasCavities"] = true;
            stats["TotalCavities"] = _cavities.Count;
            stats["TotalVolume"] = GetTotalVoidsVolumeFromCavities(_cavities);
            stats["TotalSurface"] = GetTotalSurfaceAreaFromCavities(_cavities);
            stats["VolumeSurfaceRatio"] = GetVolumeToSurfaceRatioFromCavities(_cavities);

            int primaryCount = 0;
            int duplicateCount = 0;
            int channelCount = 0;
            float avgSphericity = 0f;
            int sphericityCount = 0;
            float totalChannelLen = 0f;

            foreach (var c in _cavities)
            {
                if (c.IsPeriodicDuplicate)
                    duplicateCount++;
                else
                    primaryCount++;

                if (c.HasCrystalChannels)
                {
                    channelCount++;
                    totalChannelLen += GetTotalChannelLength(c);
                }

                if (!c.IsPeriodicDuplicate)
                {
                    avgSphericity += CalculateSphericity(c);
                    sphericityCount++;
                }
            }

            stats["PrimaryCavities"] = primaryCount;
            stats["DuplicateCavities"] = duplicateCount;
            stats["ChannelCavities"] = channelCount;
            stats["TotalChannelLength"] = totalChannelLen;
            stats["AverageSphericity"] = sphericityCount > 0 ? avgSphericity / sphericityCount : 0f;

            var smallest = GetSmallestCavity(_cavities);
            var largest = GetLargestCavity(_cavities);
            var mostSpherical = GetMostSphericalCavity(_cavities);
            var mostChanneled = GetMostChanneledCavity(_cavities);

            if (smallest != null)
                stats["SmallestVolume"] = smallest.Volume;
            if (largest != null)
                stats["LargestVolume"] = largest.Volume;
            if (mostSpherical != null)
                stats["MostSpherical"] = mostSpherical.Sphericity;
            if (mostChanneled != null)
                stats["MostChanneled"] = mostChanneled.Id;

 // Channel direction statistics
            var dirStats = new Dictionary<string, int>();
            foreach (var c in _cavities)
            {
                if (c.CrystalChannelResults != null)
                {
                    foreach (var dir in c.CrystalChannelResults.Keys)
                    {
                        if (!dirStats.ContainsKey(dir))
                            dirStats[dir] = 0;
                        dirStats[dir]++;
                    }
                }
            }
            stats["ChannelDirections"] = dirStats;

            return stats;
        }

 /// <summary>Exafterrts cavity data to CSV format.</summary>
        public string ExportCavitiesToCSV(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return "No cavities";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Id,Volume,SurfaceArea,Sphericity,WallConnections,IsPeriodicDuplicate,ChannelCount,ChannelDirections,TotalChannelLength,AvgHelicity");

            foreach (var c in cavities)
            {
                string channels = c.CrystalChannelResults != null ? string.Join(";", c.CrystalChannelResults.Keys) : "";
                int channelCount = GetChannelCount(c);
                float totalLen = GetTotalChannelLength(c);
                float avgHel = GetAverageChannelHelicity(c);

                sb.AppendLine($"{c.Id},{c.Volume:F6},{c.SurfaceArea:F6},{c.Sphericity:F6}," +
                    $"{c.WallConnections},{c.IsPeriodicDuplicate},{channelCount},{channels}," +
                    $"{totalLen:F6},{avgHel:F6}");
            }

            return sb.ToString();
        }

 /// <summary>Exafterrts channel data to CSV format.</summary>
        public string ExportChannelsToCSV(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return "No cavities";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("CavityId,Direction,IsConnected,Length,AvgWidth,MinWidth,MaxWidth,EffectiveDiameter,Helicity,PathPoints,CellCrossings");

            foreach (var c in cavities)
            {
                if (c.CrystalChannelResults == null) continue;

                foreach (var kvp in c.CrystalChannelResults)
                {
                    var result = kvp.Value;
                    if (result == null) continue;

                    sb.AppendLine($"{c.Id},{kvp.Key},{result.IsConnected},{result.TotalLength:F6}," +
                        $"{result.AverageWidth:F6},{result.MinWidth:F6},{result.MaxWidth:F6}," +
                        $"{result.EffectiveDiameter:F6},{result.Helicity:F6}," +
                        $"{result.PathPoints},{result.CellCrossings}");
                }
            }

            return sb.ToString();
        }

 /// <summary>
 /// Generates a channel network reafterrt for all cavities.
 /// </summary>
        public string GenerateChannelNetworkReport(List<Cavity> cavities)
        {
            if (cavities == null || cavities.Count == 0)
                return "No cavities";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=".PadRight(70, '='));
            sb.AppendLine("SEMI-PERIODIC CHANNEL NETWORK REPORT");
            sb.AppendLine("Channels detected using CavityPathFinder with wrap-around");
            sb.AppendLine("=".PadRight(70, '='));
            sb.AppendLine();

            int totalChannels = 0;
            int cavitiesWithChannels = 0;
            var dirStats = new Dictionary<string, int>();
            float totalLength = 0f;
            float avgHelicitySum = 0f;
            int helicityCount = 0;

            foreach (var c in cavities)
            {
                if (c.HasCrystalChannels)
                {
                    cavitiesWithChannels++;
                    foreach (var kvp in c.CrystalChannelResults)
                    {
                        if (kvp.Value?.IsConnected == true)
                        {
                            totalChannels++;
                            if (!dirStats.ContainsKey(kvp.Key))
                                dirStats[kvp.Key] = 0;
                            dirStats[kvp.Key]++;
                            totalLength += kvp.Value.TotalLength;
                            avgHelicitySum += kvp.Value.Helicity;
                            helicityCount++;
                        }
                    }
                }
            }

            float avgHelicity = helicityCount > 0 ? avgHelicitySum / helicityCount : 0f;

            sb.AppendLine($"Cavities with channels: {cavitiesWithChannels}/{cavities.Count}");
            sb.AppendLine($"Total channels: {totalChannels}");
            sb.AppendLine($"Total channel length: {totalLength:F2} A");
            sb.AppendLine($"Average helicity: {avgHelicity:F2}°");
            sb.AppendLine();

            sb.AppendLine("Channel direction distribution:");
            foreach (var kvp in dirStats.OrderBy(k => k.Key))
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value} channels");
            }
            sb.AppendLine();

 // Determine network type
            bool hasX = dirStats.ContainsKey("XX");
            bool hasY = dirStats.ContainsKey("YY");
            bool hasZ = dirStats.ContainsKey("ZZ");

            if (hasX && hasY && hasZ)
                sb.AppendLine("3D CHANNEL NETWORK - channels in all three directions");
            else if ((hasX && hasY) || (hasX && hasZ) || (hasY && hasZ))
            {
                string dirs = "";
                if (hasX && hasY) dirs = "X and Y";
                else if (hasX && hasZ) dirs = "X and Z";
                else if (hasY && hasZ) dirs = "Y and Z";
                sb.AppendLine($"2D CHANNEL NETWORK - channels in {dirs} directions");
            }
            else if (hasX || hasY || hasZ)
            {
                string dir = hasX ? "X" : (hasY ? "Y" : "Z");
                sb.AppendLine($"1D CHANNEL NETWORK - only {dir}-direction channels");
            }
            else
                sb.AppendLine("No channel network - isolated cavities only");

            sb.AppendLine();
            sb.AppendLine("=".PadRight(70, '='));

            return sb.ToString();
        }

        #endregion
    }
}