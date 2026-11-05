// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: FillAndSpred.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Tal.Diagnostics;
using Tal.GUI;
using Color = Microsoft.Xna.Framework.Color;
using SysVector3 = System.Numerics.Vector3;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;

namespace Tal.Cavity
{
 /// <summary>
 /// Performs flood-fill cavity detection on a 3D density grid.
 /// Optimized with flat arrays, queue afteroling, and minimal memory allocations.
 /// </summary>
    public class CavityFillAndSpread
    {
        #region Queue Pool

        private static class QueuePool<T>
        {
            private static readonly Stack<Queue<T>> _pool = new Stack<Queue<T>>();
            private static readonly object _lock = new object();
            private const int DEFAULT_CAPACITY = 1024;

            public static Queue<T> Rent(int capacity = DEFAULT_CAPACITY)
            {
                lock (_lock)
                {
                    if (_pool.Count > 0)
                    {
                        var queue = _pool.Pop();
                        queue.Clear();
                        return queue;
                    }
                }
                return new Queue<T>(capacity);
            }

            public static void Return(Queue<T> queue)
            {
                if (queue == null) return;
                lock (_lock)
                {
                    queue.Clear();
                    _pool.Push(queue);
                }
            }
        }

        #endregion

        #region Structures

 /// <summary>3D integer vector for voxel indices.</summary>
        public struct IntVec3
        {
            public int x, y, z;
            public IntVec3(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        }

 /// <summary>Represents a detected cavity region.</summary>
        public class CavityRegion
        {
            public int Id { get; set; }
            public List<IntVec3> Voxels { get; set; } = new List<IntVec3>();
            public List<SysVector3> WorldPositions { get; set; } = new List<SysVector3>();
            public SysVector3 Center { get; set; }
            public float Volume { get; set; }
            public SysVector3 BoundingBoxMin { get; set; }
            public SysVector3 BoundingBoxMax { get; set; }
            public Color DisplayColor { get; set; }

            public int WallXMin { get; set; } = 0;
            public int WallXMax { get; set; } = 0;
            public int WallYMin { get; set; } = 0;
            public int WallYMax { get; set; } = 0;
            public int WallZMin { get; set; } = 0;
            public int WallZMax { get; set; } = 0;

            public bool TouchesXMin { get; set; } = false;
            public bool TouchesXMax { get; set; } = false;
            public bool TouchesYMin { get; set; } = false;
            public bool TouchesYMax { get; set; } = false;
            public bool TouchesZMin { get; set; } = false;
            public bool TouchesZMax { get; set; } = false;

            public int WallConnections => WallXMin + WallXMax + WallYMin + WallYMax + WallZMin + WallZMax;
            public int TopologyType => WallConnections;
            public bool IsPeriodic => WallConnections > 0;

            public HashSet<int> ConnectedCavities { get; set; } = new HashSet<int>();
            public bool IsExterior { get; set; } = false;
            public int PeriodicGroupId { get; set; } = -1;
            public bool IsPeriodicDuplicate { get; set; } = false;

            public int WrapXMinus { get; set; } = 0;
            public int WrapXPlus { get; set; } = 0;
            public int WrapYMinus { get; set; } = 0;
            public int WrapYPlus { get; set; } = 0;
            public int WrapZMinus { get; set; } = 0;
            public int WrapZPlus { get; set; } = 0;
        }

        #endregion

        #region Private Fields

        private int _resX, _resY, _resZ;
        private int _totalVoxels;
        private float[] _positionsFlat;
        private float[] _fracXFlat;
        private float[] _fracYFlat;
        private float[] _fracZFlat;
        private float _wallThreshold;

        private const float FLOOD_TOLERANCE = -0.001f;

        private int[] _hashCache;
        private int _hashCacheResX, _hashCacheResY, _hashCacheResZ;
        private readonly object _hashCacheLock = new object();

        #endregion

        #region Private Logging Methods
 //
 // ⭐ REFACTOR: LogToDebug deleguje do SafeDebugLogger.
 // - Worker threads: only ConcurrentQueue.Enqueue (~50-100 ns)
 // - UI thread: Drain() once per second (Timer in MainForm)
 // - ZERO Application.OpenForms w hot path
 // - ZERO cross-thread UI calls z threads roboczych
 //
 // Note: old _logLock was REMOVED — SafeDebugLogger
 // sam jest thread-safe przez ConcurrentQueue.
 //

 /// <summary>
 /// Logs a message via SafeDebugLogger. Thread-safe, non-blocking.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogToDebug(string message, System.Drawing.Color? color = null)
        {
            SafeDebugLogger.LogTagged("[FILL&SPREAD]", message, color);
        }

        #endregion

        #region Constructor

        public CavityFillAndSpread()
        {
            _resX = 0;
            _resY = 0;
            _resZ = 0;
            _totalVoxels = 0;
            _wallThreshold = 0;
            _positionsFlat = null;
            _fracXFlat = null;
            _fracYFlat = null;
            _fracZFlat = null;
            _hashCache = null;
        }

        #endregion

        #region Fast Access Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetIndex(int x, int y, int z)
        {
            return (x * _resY + y) * _resZ + z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetPosIndex(int idx)
        {
            return idx * 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SysVector3 GetPosition(int idx)
        {
            int pi = idx * 3;
            return new SysVector3(_positionsFlat[pi], _positionsFlat[pi + 1], _positionsFlat[pi + 2]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetFracX(int idx) => _fracXFlat[idx];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetFracY(int idx) => _fracYFlat[idx];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetFracZ(int idx) => _fracZFlat[idx];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetVoxelHash(int idx)
        {
            if (_hashCache != null && _hashCacheResX == _resX && _hashCacheResY == _resY && _hashCacheResZ == _resZ)
            {
                return _hashCache[idx];
            }

            lock (_hashCacheLock)
            {
                if (_hashCache != null && _hashCacheResX == _resX && _hashCacheResY == _resY && _hashCacheResZ == _resZ)
                {
                    return _hashCache[idx];
                }

                _hashCacheResX = _resX;
                _hashCacheResY = _resY;
                _hashCacheResZ = _resZ;
                _hashCache = new int[_totalVoxels];
                for (int i = 0; i < _totalVoxels; i++)
                    _hashCache[i] = i;

                return _hashCache[idx];
            }
        }

        #endregion

        #region Main Detection Method

 /// <summary>
 /// Detects cavities in the density grid using flood fill.
 /// </summary>
 /// <param name="densityGrid3D">3D density grid with SDF values.</param>
 /// <param name="grid">Voxel grid with aftersitions and fractional coordinates.</param>
 /// <param name="invFracToCart">Inverse fractional to Cartesian transformation matrix.</param>
 /// <param name="minCorner">Minimum corner of the cell.</param>
 /// <param name="minVolume">Minimum volume for a cavity to be reafterrted.</param>
 /// <param name="minVoxels">Minimum number of voxels for a cavity.</param>
 /// <param name="wallThreshold">Threshold for wall detection.</param>
 /// <param name="marginX">X margin for search range.</param>
 /// <param name="marginY">Y margin for search range.</param>
 /// <param name="marginZ">Z margin for search range.</param>
 /// <param name="voxelVolume">Volume of a single voxel.</param>
 /// <returns>List of detected cavity regions.</returns>
        public List<CavityRegion> DetectCavities(
            float[,,] densityGrid3D,
            VoxelScanner.VoxelGrid grid,
            XnaMatrix invFracToCart,
            SysVector3 minCorner,
            float minVolume = 0.5f,
            int minVoxels = 3,
            float wallThreshold = 0.02f,
            int marginX = 0,
            int marginY = 0,
            int marginZ = 0,
            float voxelVolume = 0.0102f)
        {
            LogToDebug("========== DETECTION START ==========", System.Drawing.Color.Cyan);
            LogToDebug($"Min volume: {minVolume:F3} Å³, Min voxels: {minVoxels}", System.Drawing.Color.LightGray);

            _resX = grid.ResX;
            _resY = grid.ResY;
            _resZ = grid.ResZ;
            _totalVoxels = _resX * _resY * _resZ;
            _wallThreshold = wallThreshold;

            LogToDebug($"Grid resolution: {_resX}x{_resY}x{_resZ} ({_totalVoxels} voxels)", System.Drawing.Color.LightGray);

 // ⭐ NEW: limit check (in case something bypassed validation)
            long totalVoxelsLong = (long)_resX * _resY * _resZ;
            if (totalVoxelsLong > Flags.MAX_VOXELS)
            {
                LogToDebug($"Grid too large: {totalVoxelsLong:N0} voxels " +
                           $"(max: {Flags.MAX_VOXELS:N0}) - aborting detection",
                           System.Drawing.Color.Red);
                return new List<CavityRegion>();
            }

 // Flatten aftersitions
            var positions3D = grid.Positions;
            _positionsFlat = new float[_totalVoxels * 3];
            int idx = 0;
            for (int x = 0; x < _resX; x++)
            {
                for (int y = 0; y < _resY; y++)
                {
                    for (int z = 0; z < _resZ; z++)
                    {
                        int pi = idx * 3;
                        var pos = positions3D[x, y, z];
                        _positionsFlat[pi] = pos.X;
                        _positionsFlat[pi + 1] = pos.Y;
                        _positionsFlat[pi + 2] = pos.Z;
                        idx++;
                    }
                }
            }

 // Flatten fractional coordinates
            var fracX3D = grid.FracX;
            var fracY3D = grid.FracY;
            var fracZ3D = grid.FracZ;
            _fracXFlat = new float[_totalVoxels];
            _fracYFlat = new float[_totalVoxels];
            _fracZFlat = new float[_totalVoxels];
            idx = 0;
            for (int x = 0; x < _resX; x++)
            {
                for (int y = 0; y < _resY; y++)
                {
                    for (int z = 0; z < _resZ; z++)
                    {
                        _fracXFlat[idx] = fracX3D[x, y, z];
                        _fracYFlat[idx] = fracY3D[x, y, z];
                        _fracZFlat[idx] = fracZ3D[x, y, z];
                        idx++;
                    }
                }
            }

 // Flatten density
            float[] densityFlat = new float[_totalVoxels];
            Buffer.BlockCopy(densityGrid3D, 0, densityFlat, 0, _totalVoxels * sizeof(float));

            lock (_hashCacheLock)
            {
                _hashCache = null;
                _hashCacheResX = 0;
                _hashCacheResY = 0;
                _hashCacheResZ = 0;
            }

            LogToDebug($"Wall threshold: {_wallThreshold:F4}", System.Drawing.Color.LightGray);

 // Count empty voxels
            int emptyCount = 0;
            for (int i = 0; i < _totalVoxels; i++)
                if (densityFlat[i] >= 0)
                    emptyCount++;

            LogToDebug($"Grid has {emptyCount} empty voxels / {_totalVoxels} total ({100.0f * emptyCount / _totalVoxels:F1}%)", System.Drawing.Color.LightGray);

            if (emptyCount == 0)
            {
                LogToDebug("No empty voxels in grid!", System.Drawing.Color.Orange);
                return new List<CavityRegion>();
            }

            bool[] found = new bool[_totalVoxels];
            var regions = new List<CavityRegion>();
            int cavityId = 0;
            var lockObj = new object();

            int extend = 2;
            int startX = Math.Max(0, marginX - extend);
            int endX = Math.Min(_resX, _resX - marginX + extend);
            int startY = Math.Max(0, marginY - extend);
            int endY = Math.Min(_resY, _resY - marginY + extend);
            int startZ = Math.Max(0, marginZ - extend);
            int endZ = Math.Min(_resZ, _resZ - marginZ + extend);

            LogToDebug($"Search range: X=[{startX}-{endX}], Y=[{startY}-{endY}], Z=[{startZ}-{endZ}]", System.Drawing.Color.LightGray);

            int processedVoxels = 0;
            int progressStep = Math.Max(1, (_resX * _resY * _resZ) / 20);

 // Flood fill - optimized
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    for (int z = startZ; z < endZ; z++)
                    {
                        int idxStart = GetIndex(x, y, z);
                        int hash = GetVoxelHash(idxStart);

                        if (!found[hash] && densityFlat[idxStart] >= 0)
                        {
                            found[hash] = true;
                            var region = FindCavity3DWithBoundaryFlat(
                                idxStart, x, y, z,
                                densityFlat, found,
                                voxelVolume);

                            if (region.Voxels.Count >= minVoxels && region.Volume >= minVolume)
                            {
                                lock (lockObj)
                                {
                                    region.Id = cavityId;
                                    cavityId++;
                                    regions.Add(region);
                                }
                            }
                        }

                        processedVoxels++;
                        if (processedVoxels % progressStep == 0)
                        {
                            float percent = 100.0f * processedVoxels / _totalVoxels;
                            LogToDebug($"Progress: {processedVoxels}/{_totalVoxels} voxels ({percent:F1}%)", System.Drawing.Color.LightBlue);
                        }
                    }
                }
            }

            LogToDebug($"Found {regions.Count} cavities", System.Drawing.Color.LightGreen);

            if (regions.Count > 0)
            {
                foreach (var region in regions)
                {
                    region.IsExterior = region.WallConnections > 0;
                }
                LogToDebug($"Detected {regions.Count} cavities, {regions.Count(r => r.IsExterior)} exterior", System.Drawing.Color.LightGray);
            }

            AssignColors(regions);

 // Cleanup
            _positionsFlat = null;
            _fracXFlat = null;
            _fracYFlat = null;
            _fracZFlat = null;

            LogToDebug("========== DETECTION COMPLETE ==========", System.Drawing.Color.Cyan);

            return regions;
        }

        #endregion

        #region Flood Fill

        private CavityRegion FindCavity3DWithBoundaryFlat(
            int startIdx,
            int startX, int startY, int startZ,
            float[] densityFlat,
            bool[] found,
            float voxelVolume)
        {
            var region = new CavityRegion();

            var queue = QueuePool<IntVec3>.Rent(1024);

            try
            {
                queue.Enqueue(new IntVec3(startX, startY, startZ));
                found[startIdx] = true;
                region.Voxels.Add(new IntVec3(startX, startY, startZ));
                region.WorldPositions.Add(GetPosition(startIdx));

                int wrapXMinus = 0, wrapXPlus = 0;
                int wrapYMinus = 0, wrapYPlus = 0;
                int wrapZMinus = 0, wrapZPlus = 0;

                var density = densityFlat;
                var foundArray = found;
                int resX = _resX, resY = _resY, resZ = _resZ;
                float tolerance = FLOOD_TOLERANCE;

                while (queue.Count > 0)
                {
                    var v = queue.Dequeue();
                    int x = v.x;
                    int y = v.y;
                    int z = v.z;
                    int idx = GetIndex(x, y, z);

 // X-
                    int nx = x - 1;
                    if (nx < 0) { nx = resX - 1; wrapXMinus++; }
                    int nIdx = GetIndex(nx, y, z);
                    if (density[nIdx] >= tolerance)
                    {
                        int hash = GetVoxelHash(nIdx);
                        if (!foundArray[hash])
                        {
                            foundArray[hash] = true;
                            queue.Enqueue(new IntVec3(nx, y, z));
                            region.Voxels.Add(new IntVec3(nx, y, z));
                            region.WorldPositions.Add(GetPosition(nIdx));
                        }
                    }

 // X+
                    nx = x + 1;
                    if (nx >= resX) { nx = 0; wrapXPlus++; }
                    nIdx = GetIndex(nx, y, z);
                    if (density[nIdx] >= tolerance)
                    {
                        int hash = GetVoxelHash(nIdx);
                        if (!foundArray[hash])
                        {
                            foundArray[hash] = true;
                            queue.Enqueue(new IntVec3(nx, y, z));
                            region.Voxels.Add(new IntVec3(nx, y, z));
                            region.WorldPositions.Add(GetPosition(nIdx));
                        }
                    }

 // Y-
                    int ny = y - 1;
                    if (ny < 0) { ny = resY - 1; wrapYMinus++; }
                    nIdx = GetIndex(x, ny, z);
                    if (density[nIdx] >= tolerance)
                    {
                        int hash = GetVoxelHash(nIdx);
                        if (!foundArray[hash])
                        {
                            foundArray[hash] = true;
                            queue.Enqueue(new IntVec3(x, ny, z));
                            region.Voxels.Add(new IntVec3(x, ny, z));
                            region.WorldPositions.Add(GetPosition(nIdx));
                        }
                    }

 // Y+
                    ny = y + 1;
                    if (ny >= resY) { ny = 0; wrapYPlus++; }
                    nIdx = GetIndex(x, ny, z);
                    if (density[nIdx] >= tolerance)
                    {
                        int hash = GetVoxelHash(nIdx);
                        if (!foundArray[hash])
                        {
                            foundArray[hash] = true;
                            queue.Enqueue(new IntVec3(x, ny, z));
                            region.Voxels.Add(new IntVec3(x, ny, z));
                            region.WorldPositions.Add(GetPosition(nIdx));
                        }
                    }

 // Z-
                    int nz = z - 1;
                    if (nz < 0) { nz = resZ - 1; wrapZMinus++; }
                    nIdx = GetIndex(x, y, nz);
                    if (density[nIdx] >= tolerance)
                    {
                        int hash = GetVoxelHash(nIdx);
                        if (!foundArray[hash])
                        {
                            foundArray[hash] = true;
                            queue.Enqueue(new IntVec3(x, y, nz));
                            region.Voxels.Add(new IntVec3(x, y, nz));
                            region.WorldPositions.Add(GetPosition(nIdx));
                        }
                    }

 // Z+
                    nz = z + 1;
                    if (nz >= resZ) { nz = 0; wrapZPlus++; }
                    nIdx = GetIndex(x, y, nz);
                    if (density[nIdx] >= tolerance)
                    {
                        int hash = GetVoxelHash(nIdx);
                        if (!foundArray[hash])
                        {
                            foundArray[hash] = true;
                            queue.Enqueue(new IntVec3(x, y, nz));
                            region.Voxels.Add(new IntVec3(x, y, nz));
                            region.WorldPositions.Add(GetPosition(nIdx));
                        }
                    }
                }

                region.WrapXMinus = wrapXMinus;
                region.WrapXPlus = wrapXPlus;
                region.WrapYMinus = wrapYMinus;
                region.WrapYPlus = wrapYPlus;
                region.WrapZMinus = wrapZMinus;
                region.WrapZPlus = wrapZPlus;

                region.Volume = region.Voxels.Count * voxelVolume;
                region.Center = CalculateCenter(region.WorldPositions);
                var (min, max) = CalculateBoundingBox(region.WorldPositions);
                region.BoundingBoxMin = min;
                region.BoundingBoxMax = max;

                DetectWallsFlat(region);

                return region;
            }
            finally
            {
                QueuePool<IntVec3>.Return(queue);
            }
        }

        #endregion

        #region Wall Detection

        private void DetectWallsFlat(CavityRegion region)
        {
            bool touchesXMin = false, touchesXMax = false;
            bool touchesYMin = false, touchesYMax = false;
            bool touchesZMin = false, touchesZMax = false;

            foreach (var v in region.Voxels)
            {
                int idx = GetIndex(v.x, v.y, v.z);
                float fx = _fracXFlat[idx];
                float fy = _fracYFlat[idx];
                float fz = _fracZFlat[idx];

                if (fx < _wallThreshold) touchesXMin = true;
                if (fx > 1.0f - _wallThreshold) touchesXMax = true;
                if (fy < _wallThreshold) touchesYMin = true;
                if (fy > 1.0f - _wallThreshold) touchesYMax = true;
                if (fz < _wallThreshold) touchesZMin = true;
                if (fz > 1.0f - _wallThreshold) touchesZMax = true;
            }

            region.TouchesXMin = touchesXMin;
            region.TouchesXMax = touchesXMax;
            region.TouchesYMin = touchesYMin;
            region.TouchesYMax = touchesYMax;
            region.TouchesZMin = touchesZMin;
            region.TouchesZMax = touchesZMax;

            region.WallXMin = touchesXMin ? 1 : 0;
            region.WallXMax = touchesXMax ? 1 : 0;
            region.WallYMin = touchesYMin ? 1 : 0;
            region.WallYMax = touchesYMax ? 1 : 0;
            region.WallZMin = touchesZMin ? 1 : 0;
            region.WallZMax = touchesZMax ? 1 : 0;
        }

        #endregion

        #region Helper Methods

        private SysVector3 CalculateCenter(List<SysVector3> positions)
        {
            if (positions.Count == 0) return SysVector3.Zero;
            SysVector3 sum = SysVector3.Zero;
            foreach (var p in positions) sum += p;
            return sum / positions.Count;
        }

        private (SysVector3 min, SysVector3 max) CalculateBoundingBox(List<SysVector3> positions)
        {
            if (positions.Count == 0) return (SysVector3.Zero, SysVector3.Zero);
            SysVector3 min = new SysVector3(float.MaxValue);
            SysVector3 max = new SysVector3(float.MinValue);
            foreach (var p in positions)
            {
                min = SysVector3.Min(min, p);
                max = SysVector3.Max(max, p);
            }
            return (min, max);
        }

        private void AssignColors(List<CavityRegion> cavities)
        {
            for (int i = 0; i < cavities.Count; i++)
            {
                cavities[i].DisplayColor = CavityDetectorBase.GenerateColorFromId(i);
            }
        }

        #endregion

        #region Report Generation

 /// <summary>
 /// Generates a toafterlogy reafterrt for the detected cavities.
 /// </summary>
 /// <param name="cavities">List of detected cavities.</param>
 /// <returns>Formatted reafterrt string.</returns>
        public string GenerateTopologyReport(List<CavityRegion> cavities)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=========================================");
            sb.AppendLine("   CAVITY TOPOLOGY REPORT");
            sb.AppendLine("=========================================");
            sb.AppendLine($"Total cavities: {cavities.Count}");

            var stats = new Dictionary<int, int>();

            foreach (var c in cavities)
            {
                if (c.IsPeriodicDuplicate) continue;

                int t = c.TopologyType;
                if (stats.ContainsKey(t)) stats[t]++;
                else stats[t] = 1;
            }

            sb.AppendLine("Topology statistics (wall contacts):");
            var sortedKeys = new List<int>(stats.Keys);
            sortedKeys.Sort();
            foreach (var key in sortedKeys)
            {
                string desc = key == 0 ? "closed" : $"touches {key} wall(s)";
                sb.AppendLine($"  Type {key}: {stats[key]} cavities ({desc})");
            }

            sb.AppendLine("\nDetails:");
            foreach (var c in cavities)
            {
                if (c.IsPeriodicDuplicate) continue;

                string periodic = c.IsPeriodic ? $" (group {c.PeriodicGroupId})" : "";
                sb.AppendLine($"Cavity #{c.Id}{periodic}: Vol={c.Volume:F2} Topo={c.TopologyType}");
                sb.AppendLine($"  Wraps: X-={c.WrapXMinus} X+={c.WrapXPlus} Y-={c.WrapYMinus} Y+={c.WrapYPlus} Z-={c.WrapZMinus} Z+={c.WrapZPlus}");
            }
            sb.AppendLine("=========================================");
            return sb.ToString();
        }

        #endregion

        #region Reset

 /// <summary>
 /// Resets the detector state and releases resources.
 /// </summary>
        public void Reset()
        {
            _resX = 0;
            _resY = 0;
            _resZ = 0;
            _totalVoxels = 0;
            _positionsFlat = null;
            _fracXFlat = null;
            _fracYFlat = null;
            _fracZFlat = null;
            _wallThreshold = 0;

            lock (_hashCacheLock)
            {
                _hashCache = null;
                _hashCacheResX = 0;
                _hashCacheResY = 0;
                _hashCacheResZ = 0;
            }

            LogToDebug("Reset complete", System.Drawing.Color.LightGray);
        }

        #endregion
    }
}