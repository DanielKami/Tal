// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: SphereFitAnalyzer.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Tal.Diagnostics;
using Tal.GUI;
using SysVector3 = System.Numerics.Vector3;

namespace Tal.Cavity
{
 /// <summary>
 /// Fast sphere fit analyzer - optimized for large cavities.
 /// STATIC - no state, all results stored in Cavity objects.
 /// </summary>
    public static class SphereFitAnalyzer
    {
 // ============================================================
 // CONSTANTS
 // ============================================================

        private const float MIN_SDF_VALUE_SMALL = 0.3f;
        private const float MIN_SDF_VALUE_LARGE = 0.5f;
        private const int MIN_VOXELS_SMALL = 50;
        private const int MIN_VOXELS_ANY = 10;
        private const float MIN_DIST_FRAC = 0.05f;
        private const float SEPARATION_FACTOR = 0.5f;
        private const float WALL_THRESHOLD = 0.01f;

 // ============================================================
 // WYNIK - POJEDYNCZA KULA
 // ============================================================

        public class SphereFitResult
        {
            public int CavityId { get; set; }
            public int SphereIndex { get; set; }
            public float MaxSphereRadius { get; set; }
            public SysVector3 SphereCenter { get; set; }
            public SysVector3 VoxelPosition { get; set; }
            public float CavityVolume { get; set; }
            public int VoxelCount { get; set; }
            public bool HasChannels { get; set; }
            public string ChannelDirections { get; set; }
            public int WallConnections { get; set; }
            public float FracX { get; set; }
            public float FracY { get; set; }
            public float FracZ { get; set; }
            public bool IsOnWall => FracX < WALL_THRESHOLD || FracX > 1.0f - WALL_THRESHOLD ||
                                    FracY < WALL_THRESHOLD || FracY > 1.0f - WALL_THRESHOLD ||
                                    FracZ < WALL_THRESHOLD || FracZ > 1.0f - WALL_THRESHOLD;
            public float Diameter => MaxSphereRadius * 2f;
        }

 // ============================================================
 // STRUCTURA LOKALNEGO MAKSIMUM
 // ============================================================

        private class LocalMaximum
        {
            public SysVector3 position;
            public float sdfValue;
            public int x, y, z;
            public float fx, fy, fz;
            public bool IsOnWall => fx < WALL_THRESHOLD || fx > 1.0f - WALL_THRESHOLD ||
                                    fy < WALL_THRESHOLD || fy > 1.0f - WALL_THRESHOLD ||
                                    fz < WALL_THRESHOLD || fz > 1.0f - WALL_THRESHOLD;
        }

 // ============================================================
 // PRYWATNE LOGOWANIE
 //
 // ⭐ REFACTOR: LogToDebug deleguje do SafeDebugLogger.
 // - Worker threads (w tym Parallel.For): ConcurrentQueue.Enqueue
 // - UI thread: Drain() once per second (Timer in MainForm)
 // - ZERO Application.OpenForms w hot path
 // - ZERO cross-thread UI calls z threads roboczych
 // ============================================================

 /// <summary>
 /// Logs a message via SafeDebugLogger. Thread-safe, non-blocking.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogToDebug(string message, System.Drawing.Color? color = null)
        {
            SafeDebugLogger.LogTagged("[SphereFit]", message, color);
        }

 // ============================================================
 // PUBLICZNE METODY - STATYCZNE
 // ============================================================

 /// <summary>
 /// Analyzes all cavities and finds spheres.
 /// Results are stored directly in Cavity objects.
 /// </summary>
        public static int AnalyzeAllCavities(
            List<Cavity> cavities,
            Action<int, int> progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (cavities == null || cavities.Count == 0)
            {
                LogToDebug("No cavities to analyze for sphere fit", System.Drawing.Color.Orange);
                return 0;
            }

            var cavitiesToAnalyze = cavities
                .Where(c => c != null && !c.IsPeriodicDuplicate && c.Voxels != null && c.Voxels.Count > MIN_VOXELS_ANY)
                .ToList();

            if (cavitiesToAnalyze.Count == 0)
            {
                LogToDebug($"No cavities with enough voxels (> {MIN_VOXELS_ANY}) for sphere fit", System.Drawing.Color.Orange);
                return 0;
            }

            LogToDebug($"Analyzing {cavitiesToAnalyze.Count} cavities for sphere fits...", System.Drawing.Color.Cyan);

            int processed = 0;
            int total = cavitiesToAnalyze.Count;
            int totalSpheres = 0;
            var lockObj = new object();

            int maxParallel = Math.Min(Environment.ProcessorCount, total);
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallel,
                CancellationToken = cancellationToken
            };

            try
            {
                Parallel.For(0, total, options, (i, state) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var cavity = cavitiesToAnalyze[i];

                    try
                    {
                        if (cavity.Voxels.Count < MIN_VOXELS_SMALL)
                            FindSpheresSmallCavity(cavity);
                        else
                            FindSpheresPeriodicCell(cavity);

                        lock (lockObj)
                        {
                            processed++;
                            if (cavity.SphereFits != null)
                                totalSpheres += cavity.SphereFits.Count;
                            progressCallback?.Invoke(processed, total);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        LogToDebug($"Error analyzing cavity {cavity.Id}: {ex.Message}", System.Drawing.Color.Red);
                        cavity.SphereFitCalculated = false;
                        cavity.MaxSphereRadius = 0f;
                        cavity.SphereFits = new List<SphereFitResult>();
                        lock (lockObj) { processed++; }
                    }
                });
            }
            catch (OperationCanceledException)
            {
                LogToDebug("Sphere fit analysis cancelled", System.Drawing.Color.Orange);
                return processed;
            }

            LogToDebug($"Complete: {processed}/{total} cavities, {totalSpheres} spheres found", System.Drawing.Color.LightGreen);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return processed;
        }

 /// <summary>
 /// Gets the number of spheres for a cavity.
 /// </summary>
        public static int GetSphereCount(Cavity cavity)
        {
            return cavity?.SphereFits?.Count ?? 0;
        }

 /// <summary>
 /// Gets the largest sphere for a cavity.
 /// </summary>
        public static SphereFitResult GetLargestSphere(Cavity cavity)
        {
            if (cavity == null || cavity.SphereFits == null || cavity.SphereFits.Count == 0)
                return null;
            return cavity.SphereFits.OrderByDescending(s => s.MaxSphereRadius).FirstOrDefault();
        }

 /// <summary>
 /// Gets all spheres for a cavity.
 /// </summary>
        public static List<SphereFitResult> GetSpheres(Cavity cavity)
        {
            if (cavity == null || cavity.SphereFits == null)
                return new List<SphereFitResult>();
            return new List<SphereFitResult>(cavity.SphereFits);
        }

 // ============================================================
 // PRYWATNE METODY - ANALIZA
 // ============================================================

        private static void FindSpheresSmallCavity(Cavity cavity)
        {
            if (cavity.DensityGrid == null)
            {
                cavity.SphereFitCalculated = false;
                return;
            }

            var max = FindSingleMaximum(cavity);

            if (max != null && max.sdfValue > MIN_SDF_VALUE_SMALL)
            {
                cavity.SphereFits = new List<SphereFitResult>
                {
                    new SphereFitResult
                    {
                        CavityId = cavity.Id,
                        SphereIndex = 0,
                        MaxSphereRadius = max.sdfValue,
                        SphereCenter = max.position,
                        VoxelPosition = max.position,
                        FracX = max.fx,
                        FracY = max.fy,
                        FracZ = max.fz,
                        CavityVolume = cavity.Volume,
                        VoxelCount = cavity.Voxels?.Count ?? 0,
                        HasChannels = cavity.HasCrystalChannels,
                        ChannelDirections = cavity.HasCrystalChannels ? string.Join(",", cavity.CrystalChannelResults.Keys) : "none",
                        WallConnections = cavity.WallConnections
                    }
                };

                cavity.MaxSphereRadius = max.sdfValue;
                cavity.SphereCenter = max.position;
                cavity.SphereFitCalculated = true;
            }
            else
            {
                cavity.SphereFitCalculated = false;
                cavity.MaxSphereRadius = 0f;
                cavity.SphereFits = new List<SphereFitResult>();
            }
        }

        private static LocalMaximum FindSingleMaximum(Cavity cavity)
        {
            if (cavity?.DensityGrid == null)
                return null;

            var grid = cavity.DensityGrid;
            int nx = grid.GetLength(0);
            int ny = grid.GetLength(1);
            int nz = grid.GetLength(2);

            int ox = cavity.OffsetX;
            int oy = cavity.OffsetY;
            int oz = cavity.OffsetZ;

            int resX = cavity.PeriodicGrid?.ResX ?? (nx - 2 * ox);
            int resY = cavity.PeriodicGrid?.ResY ?? (ny - 2 * oy);
            int resZ = cavity.PeriodicGrid?.ResZ ?? (nz - 2 * oz);

            if (resX <= 0 || resY <= 0 || resZ <= 0)
                return null;

            float maxVal = 0;
            int maxX = ox, maxY = oy, maxZ = oz;

            int step = Math.Max(1, Math.Min(resX, Math.Min(resY, resZ)) / 20);

            for (int x = ox; x < ox + resX; x += step)
            {
                for (int y = oy; y < oy + resY; y += step)
                {
                    for (int z = oz; z < oz + resZ; z += step)
                    {
                        if (x >= nx || y >= ny || z >= nz) continue;
                        float val = grid[x, y, z];
                        if (val > maxVal)
                        {
                            maxVal = val;
                            maxX = x;
                            maxY = y;
                            maxZ = z;
                        }
                    }
                }
            }

            if (maxVal < MIN_SDF_VALUE_SMALL) return null;

            int radius = Math.Max(3, step * 2);
            for (int x = Math.Max(ox, maxX - radius); x < Math.Min(ox + resX, maxX + radius); x++)
            {
                for (int y = Math.Max(oy, maxY - radius); y < Math.Min(oy + resY, maxY + radius); y++)
                {
                    for (int z = Math.Max(oz, maxZ - radius); z < Math.Min(oz + resZ, maxZ + radius); z++)
                    {
                        if (x >= nx || y >= ny || z >= nz) continue;
                        float val = grid[x, y, z];
                        if (val > maxVal)
                        {
                            maxVal = val;
                            maxX = x;
                            maxY = y;
                            maxZ = z;
                        }
                    }
                }
            }

            int px = maxX - ox;
            int py = maxY - oy;
            int pz = maxZ - oz;

            SysVector3 pos;
            float fx, fy, fz;

            if (cavity.PeriodicGrid?.Positions != null &&
                px >= 0 && px < cavity.PeriodicGrid.ResX &&
                py >= 0 && py < cavity.PeriodicGrid.ResY &&
                pz >= 0 && pz < cavity.PeriodicGrid.ResZ)
            {
                pos = cavity.PeriodicGrid.Positions[px, py, pz];
                fx = cavity.PeriodicGrid.FracX[px, py, pz];
                fy = cavity.PeriodicGrid.FracY[px, py, pz];
                fz = cavity.PeriodicGrid.FracZ[px, py, pz];
            }
            else
            {
                pos = CellParameters.FracToCartesian(
                    (float)px / resX,
                    (float)py / resY,
                    (float)pz / resZ);
                fx = (float)px / resX;
                fy = (float)py / resY;
                fz = (float)pz / resZ;
            }

            fx = fx - (float)Math.Floor(fx);
            fy = fy - (float)Math.Floor(fy);
            fz = fz - (float)Math.Floor(fz);
            if (fx < 0) fx += 1.0f;
            if (fy < 0) fy += 1.0f;
            if (fz < 0) fz += 1.0f;

            return new LocalMaximum
            {
                position = pos,
                sdfValue = maxVal,
                x = px,
                y = py,
                z = pz,
                fx = fx,
                fy = fy,
                fz = fz
            };
        }

        private static void FindSpheresPeriodicCell(Cavity cavity)
        {
            var densityGrid = cavity.DensityGrid;
            if (densityGrid == null)
            {
                cavity.SphereFitCalculated = false;
                return;
            }

            int nx = densityGrid.GetLength(0);
            int ny = densityGrid.GetLength(1);
            int nz = densityGrid.GetLength(2);

            int ox = cavity.OffsetX;
            int oy = cavity.OffsetY;
            int oz = cavity.OffsetZ;

            int resX = cavity.PeriodicGrid?.ResX ?? (nx - 2 * ox);
            int resY = cavity.PeriodicGrid?.ResY ?? (ny - 2 * oy);
            int resZ = cavity.PeriodicGrid?.ResZ ?? (nz - 2 * oz);

            if (resX <= 0 || resY <= 0 || resZ <= 0)
            {
                cavity.SphereFitCalculated = false;
                return;
            }

            var allMaxima = FindLocalMaximaParallel(cavity, densityGrid, nx, ny, nz, ox, oy, oz, resX, resY, resZ);

            if (allMaxima.Count == 0)
            {
                cavity.SphereFitCalculated = false;
                cavity.MaxSphereRadius = 0f;
                cavity.SphereFits = new List<SphereFitResult>();
                return;
            }

            var validMaxima = allMaxima
                .Where(m => m.sdfValue >= MIN_SDF_VALUE_LARGE)
                .OrderByDescending(m => m.sdfValue)
                .ToList();

            if (validMaxima.Count == 0)
            {
                cavity.SphereFitCalculated = false;
                cavity.MaxSphereRadius = 0f;
                cavity.SphereFits = new List<SphereFitResult>();
                return;
            }

            var selected = SelectSpheres(validMaxima, SEPARATION_FACTOR);

            if (selected.Count == 0)
            {
                cavity.SphereFitCalculated = false;
                cavity.MaxSphereRadius = 0f;
                cavity.SphereFits = new List<SphereFitResult>();
                return;
            }

            int sphereIndex = 0;
            var results = new List<SphereFitResult>();

            foreach (var max in selected)
            {
                results.Add(new SphereFitResult
                {
                    CavityId = cavity.Id,
                    SphereIndex = sphereIndex++,
                    MaxSphereRadius = max.sdfValue,
                    SphereCenter = max.position,
                    VoxelPosition = max.position,
                    FracX = max.fx,
                    FracY = max.fy,
                    FracZ = max.fz,
                    CavityVolume = cavity.Volume,
                    VoxelCount = cavity.Voxels?.Count ?? 0,
                    HasChannels = cavity.HasCrystalChannels,
                    ChannelDirections = cavity.HasCrystalChannels ? string.Join(",", cavity.CrystalChannelResults.Keys) : "none",
                    WallConnections = cavity.WallConnections
                });
            }

            cavity.SphereFits = results;
            cavity.MaxSphereRadius = results.Count > 0 ? results.Max(r => r.MaxSphereRadius) : 0f;
            cavity.SphereFitCalculated = results.Count > 0;

            if (cavity.SphereFitCalculated)
            {
                var centerSpheres = results.Where(s => !s.IsOnWall).ToList();
                if (centerSpheres.Count > 0)
                    cavity.SphereCenter = centerSpheres.OrderByDescending(s => s.MaxSphereRadius).First().SphereCenter;
                else
                    cavity.SphereCenter = results.OrderByDescending(s => s.MaxSphereRadius).First().SphereCenter;
            }
        }

        private static List<LocalMaximum> FindLocalMaximaParallel(
            Cavity cavity,
            float[,,] densityGrid,
            int nx, int ny, int nz,
            int ox, int oy, int oz,
            int resX, int resY, int resZ)
        {
            var result = new ConcurrentBag<LocalMaximum>();

            int step = Math.Max(1, Math.Min(resX, Math.Min(resY, resZ)) / 15);
            int maxParallel = Math.Min(Environment.ProcessorCount, resX);

            Parallel.For(ox, ox + resX, new ParallelOptions { MaxDegreeOfParallelism = maxParallel }, x =>
            {
                for (int y = oy; y < oy + resY; y += step)
                {
                    for (int z = oz; z < oz + resZ; z += step)
                    {
                        if (x >= nx || y >= ny || z >= nz) continue;

                        if (!IsLocalMaximumFast(densityGrid, nx, ny, nz, x, y, z))
                            continue;

                        var max = RefineMaximum(densityGrid, nx, ny, nz, x, y, z, 3);

                        if (max != null && max.sdfValue > MIN_SDF_VALUE_SMALL)
                        {
                            int px = max.x - ox;
                            int py = max.y - oy;
                            int pz = max.z - oz;

                            if (px >= 0 && px < resX && py >= 0 && py < resY && pz >= 0 && pz < resZ)
                            {
                                SysVector3 pos;
                                float fx, fy, fz;

                                if (cavity.PeriodicGrid?.Positions != null)
                                {
                                    pos = cavity.PeriodicGrid.Positions[px, py, pz];
                                    fx = cavity.PeriodicGrid.FracX[px, py, pz];
                                    fy = cavity.PeriodicGrid.FracY[px, py, pz];
                                    fz = cavity.PeriodicGrid.FracZ[px, py, pz];
                                }
                                else
                                {
                                    pos = CellParameters.FracToCartesian(
                                        (float)px / resX,
                                        (float)py / resY,
                                        (float)pz / resZ);
                                    fx = (float)px / resX;
                                    fy = (float)py / resY;
                                    fz = (float)pz / resZ;
                                }

                                fx = fx - (float)Math.Floor(fx);
                                fy = fy - (float)Math.Floor(fy);
                                fz = fz - (float)Math.Floor(fz);
                                if (fx < 0) fx += 1.0f;
                                if (fy < 0) fy += 1.0f;
                                if (fz < 0) fz += 1.0f;

                                result.Add(new LocalMaximum
                                {
                                    position = pos,
                                    sdfValue = max.sdfValue,
                                    x = px,
                                    y = py,
                                    z = pz,
                                    fx = fx,
                                    fy = fy,
                                    fz = fz
                                });
                            }
                        }
                    }
                }
            });

            return result.ToList();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLocalMaximumFast(float[,,] grid, int nx, int ny, int nz, int x, int y, int z)
        {
            float center = grid[x, y, z];
            if (center <= MIN_SDF_VALUE_SMALL) return false;

            if (x > 0 && grid[x - 1, y, z] > center) return false;
            if (x < nx - 1 && grid[x + 1, y, z] > center) return false;
            if (y > 0 && grid[x, y - 1, z] > center) return false;
            if (y < ny - 1 && grid[x, y + 1, z] > center) return false;
            if (z > 0 && grid[x, y, z - 1] > center) return false;
            if (z < nz - 1 && grid[x, y, z + 1] > center) return false;

            return true;
        }

        private static LocalMaximum RefineMaximum(float[,,] grid, int nx, int ny, int nz, int x, int y, int z, int radius)
        {
            float maxVal = grid[x, y, z];
            int maxX = x, maxY = y, maxZ = z;

            int startX = Math.Max(0, x - radius);
            int endX = Math.Min(nx - 1, x + radius);
            int startY = Math.Max(0, y - radius);
            int endY = Math.Min(ny - 1, y + radius);
            int startZ = Math.Max(0, z - radius);
            int endZ = Math.Min(nz - 1, z + radius);

            for (int xi = startX; xi <= endX; xi++)
            {
                for (int yi = startY; yi <= endY; yi++)
                {
                    for (int zi = startZ; zi <= endZ; zi++)
                    {
                        float val = grid[xi, yi, zi];
                        if (val > maxVal)
                        {
                            maxVal = val;
                            maxX = xi;
                            maxY = yi;
                            maxZ = zi;
                        }
                    }
                }
            }

            if (maxVal < MIN_SDF_VALUE_SMALL) return null;

            return new LocalMaximum
            {
                position = SysVector3.Zero,
                sdfValue = maxVal,
                x = maxX,
                y = maxY,
                z = maxZ,
                fx = 0,
                fy = 0,
                fz = 0
            };
        }

        private static List<LocalMaximum> SelectSpheres(List<LocalMaximum> maxima, float separationFactor)
        {
            var selected = new List<LocalMaximum>();

            foreach (var candidate in maxima.OrderByDescending(m => m.sdfValue))
            {
                bool overlaps = false;

                foreach (var selectedSphere in selected)
                {
                    float dx = candidate.fx - selectedSphere.fx;
                    float dy = candidate.fy - selectedSphere.fy;
                    float dz = candidate.fz - selectedSphere.fz;

                    if (dx > 0.5f) dx -= 1.0f;
                    else if (dx < -0.5f) dx += 1.0f;
                    if (dy > 0.5f) dy -= 1.0f;
                    else if (dy < -0.5f) dy += 1.0f;
                    if (dz > 0.5f) dz -= 1.0f;
                    else if (dz < -0.5f) dz += 1.0f;

                    float dist = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    float minDist = (candidate.sdfValue + selectedSphere.sdfValue) * separationFactor / 10.0f;
                    float effectiveMinDist = Math.Max(minDist, MIN_DIST_FRAC);

                    if (dist < effectiveMinDist)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                    selected.Add(candidate);
            }

            return selected;
        }

 // ============================================================
 // KLASY POMOCNICZE
 // ============================================================

        public class CavitySpheresResult
        {
            public int CavityId { get; set; }
            public List<SphereFitResult> Spheres { get; set; } = new List<SphereFitResult>();
            public float CavityVolume { get; set; }
            public int VoxelCount { get; set; }
            public bool HasChannels { get; set; }
            public string ChannelDirections { get; set; }
            public int WallConnections { get; set; }
            public int Count => Spheres.Count;
            public SphereFitResult Largest => Spheres.OrderByDescending(s => s.MaxSphereRadius).FirstOrDefault();
        }
    }
}