// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: ChannelPathFinder.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Runtime.CompilerServices;
using SysMatrix4x4 = System.Numerics.Matrix4x4;
using SysVector3 = System.Numerics.Vector3;
using Vector3 = System.Numerics.Vector3;

namespace Tal.Cavity
{
 /// <summary>
 /// Multi-channel result containing multiple paths for a direction.
 /// </summary>
    public class MultiChannelResult
    {
        public string Direction { get; set; }
        public List<CavityPathFinder.ChannelPathResult> Paths { get; set; } = new List<CavityPathFinder.ChannelPathResult>();
        public CavityPathFinder.ChannelPathResult BestPath => Paths.OrderByDescending(p => p?.AverageWidth ?? 0).FirstOrDefault();
        public CavityPathFinder.ChannelPathResult ShortestPath => Paths.OrderBy(p => p?.TotalLength ?? float.MaxValue).FirstOrDefault();
        public CavityPathFinder.ChannelPathResult StraightestPath => Paths.OrderBy(p => Math.Abs(p?.Helicity ?? 0)).FirstOrDefault();
        public float BestDiameter => BestPath?.AverageWidth ?? 0;
        public int PathCount => Paths.Count;
        public bool IsConnected => Paths.Count > 0;

        public override string ToString()
        {
            return $"[{Direction}] {Paths.Count} paths, best diam: {BestDiameter:F2} A";
        }
    }

 /// <summary>
 /// Extended channel path result with additional properties.
 /// </summary>
    public class ExtendedChannelPathResult : CavityPathFinder.ChannelPathResult
    {
        public float MinDiameter { get; set; }
        public float MaxDiameter { get; set; }
        public (int x, int y, int z) StartVoxelIndex { get; set; }
        public (int x, int y, int z) EndVoxelIndex { get; set; }
        public int StartWallType { get; set; }
        public float AspectRatio => TotalLength > 0 ? TotalLength / (AverageWidth + 0.01f) : 0;
        public bool IsStraight => Math.Abs(Helicity) < 3.0f;
        public bool IsWide => AverageWidth > 1.0f;
    }

 /// <summary>
 /// Main class for finding paths through cavities using A* algorithm.
 /// Optimized with flat arrays, minimal allocations, and parallel processing.
 /// </summary>
    public class CavityPathFinder
    {
        #region Channel Path Result

        public class ChannelPathResult
        {
            public string Direction { get; set; }
            public bool IsConnected { get; set; }
            public float TotalLength { get; set; }
            public float OriginalLength { get; set; }
            public float SmoothedLength { get; set; }
            public float SmoothingError { get; set; }
            public float SmoothingErrorPercent { get; set; }
            public float AverageWidth { get; set; }
            public float MinWidth { get; set; }
            public float MaxWidth { get; set; }
            public float MinDiameter { get; set; }
            public float AvgDiameter { get; set; }
            public float EffectiveDiameter { get; set; }
            public float Helicity { get; set; }
            public Vector3 StartPoint { get; set; }
            public Vector3 EndPoint { get; set; }
            public (int x, int y, int z) StartIndex { get; set; }
            public (int x, int y, int z) EndIndex { get; set; }
            public float StartX => StartPoint.X;
            public float StartY => StartPoint.Y;
            public float StartZ => StartPoint.Z;
            public float EndX => EndPoint.X;
            public float EndY => EndPoint.Y;
            public float EndZ => EndPoint.Z;
            public int PeriodicShiftX { get; set; }
            public int PeriodicShiftY { get; set; }
            public int PeriodicShiftZ { get; set; }
            public int CellCrossings { get; set; }
            public int PathPoints { get; set; }
            public int PathVoxelCount { get; set; }
            public List<Vector3> WorldPath { get; set; } = new List<Vector3>();
            public List<Vector3> FracPath { get; set; } = new List<Vector3>();
            public List<Microsoft.Xna.Framework.Vector3> WorldPathXna { get; set; } = new List<Microsoft.Xna.Framework.Vector3>();
            public List<Vector3> MinPath { get; set; } = new List<Vector3>();
            public List<Vector3> MaxPath { get; set; } = new List<Vector3>();
            public List<Microsoft.Xna.Framework.Vector3> MinPathXna { get; set; } = new List<Microsoft.Xna.Framework.Vector3>();
            public List<Microsoft.Xna.Framework.Vector3> MaxPathXna { get; set; } = new List<Microsoft.Xna.Framework.Vector3>();
            public int SelectedWall { get; set; } = 0;
            public List<int> WrapCounts { get; set; } = new List<int>();
            public Dictionary<string, float> Properties { get; set; } = new Dictionary<string, float>();

            public virtual void ConvertToXna()
            {
                WorldPathXna = WorldPath.Select(v => new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z)).ToList();
                MinPathXna = MinPath.Select(v => new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z)).ToList();
                MaxPathXna = MaxPath.Select(v => new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z)).ToList();
            }

            public override string ToString()
            {
                return $"[{Direction}] Connected={IsConnected}, Len={TotalLength:F2}A, " +
                       $"Hel={Helicity:F2}°, Pts={PathPoints}, Wall={(SelectedWall == 0 ? "MIN" : "MAX")}";
            }
        }

        #endregion

        #region Constants

        private static readonly (int dx, int dy, int dz)[] NeighborOffsets = new (int, int, int)[]
        {
            (-1, -1, -1), (-1, -1, 0), (-1, -1, 1),
            (-1, 0, -1), (-1, 0, 0), (-1, 0, 1),
            (-1, 1, -1), (-1, 1, 0), (-1, 1, 1),
            (0, -1, -1), (0, -1, 0), (0, -1, 1),
            (0, 0, -1), (0, 0, 1),
            (0, 1, -1), (0, 1, 0), (0, 1, 1),
            (1, -1, -1), (1, -1, 0), (1, -1, 1),
            (1, 0, -1), (1, 0, 0), (1, 0, 1),
            (1, 1, -1), (1, 1, 0), (1, 1, 1)
        };

        private static readonly int[] NeighborDx = new int[] { -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
        private static readonly int[] NeighborDy = new int[] { -1, -1, -1, 0, 0, 0, 1, 1, 1, -1, -1, -1, 0, 1, 1, 1, 1, -1, -1, -1, 0, 0, 0, 1, 1, 1 };
        private static readonly int[] NeighborDz = new int[] { -1, 0, 1, -1, 0, 1, -1, 0, 1, -1, 0, 1, 1, -1, 0, 1, 1, -1, 0, 1, -1, 0, 1, -1, 0, 1 };

        private static readonly int NEIGHBOR_COUNT = NeighborOffsets.Length;

        #endregion

        #region Control Parameters

        public float MaxStepDistance { get; set; } = 2.0f;
        public int MaxPathLength { get; set; } = 50000;
        public float WallThreshold { get; set; } = 0.0f;
        public float ProbeRadius { get; set; } = 0.5f;
        public bool UseSDFWeight { get; set; } = true;
        public float SDFWeight { get; set; } = 0.3f;
        public int SmoothingIterations { get; set; } = 3;
        public bool EnableDetailedLogging { get; set; } = true;
        public int MaxStartPoints { get; set; } = 20;

        #endregion

        #region Private Fields

        private readonly Cavity _cavity;
        private readonly int _nx, _ny, _nz;
        private readonly int _totalVoxels;
        private readonly int _offsetX, _offsetY, _offsetZ;
        private readonly int _periodicResX, _periodicResY, _periodicResZ;

        private readonly float[] _densityFlat;
        private readonly SysVector3[] _positionsFlat;
        private readonly float[] _fracXFlat;
        private readonly float[] _fracYFlat;
        private readonly float[] _fracZFlat;

        private readonly SysMatrix4x4 _sysInvFracToCart;
        private readonly SysMatrix4x4 _sysFracToCart;
        private readonly Vector3 _minCornerVec;

        private int _totalWallChecks = 0;
        private int _totalNeighborChecks = 0;
        private float _maxSDFInCavity = 1.0f;

        private static readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());

        private float[] _precomputedSin;
        private float[] _precomputedCos;
        private int _precomputedSteps;

        #endregion

        #region Constructor

        public CavityPathFinder(Cavity cavity, float probeRadius = 0.5f)
        {
            _cavity = cavity ?? throw new ArgumentNullException(nameof(cavity));
            ProbeRadius = probeRadius;

            _offsetX = cavity.OffsetX;
            _offsetY = cavity.OffsetY;
            _offsetZ = cavity.OffsetZ;

            var density = cavity.DensityGrid;
            _nx = density?.GetLength(0) ?? 0;
            _ny = density?.GetLength(1) ?? 0;
            _nz = density?.GetLength(2) ?? 0;
            _totalVoxels = _nx * _ny * _nz;

            _periodicResX = cavity.PeriodicGrid?.ResX ?? (_nx - 2 * _offsetX);
            _periodicResY = cavity.PeriodicGrid?.ResY ?? (_ny - 2 * _offsetY);
            _periodicResZ = cavity.PeriodicGrid?.ResZ ?? (_nz - 2 * _offsetZ);

 // Flatten density
            _densityFlat = new float[_totalVoxels];
            if (density != null)
            {
                Buffer.BlockCopy(density, 0, _densityFlat, 0, _totalVoxels * sizeof(float));
            }

 // Flatten aftersitions
            var grid = cavity.Grid;
            _positionsFlat = new SysVector3[_totalVoxels];
            if (grid?.Positions != null)
            {
                int idx = 0;
                for (int x = 0; x < _nx; x++)
                {
                    for (int y = 0; y < _ny; y++)
                    {
                        for (int z = 0; z < _nz; z++)
                        {
                            _positionsFlat[idx++] = grid.Positions[x, y, z];
                        }
                    }
                }
            }

 // Flatten fractional coordinates
            _fracXFlat = new float[_totalVoxels];
            _fracYFlat = new float[_totalVoxels];
            _fracZFlat = new float[_totalVoxels];
            if (grid?.FracX != null)
            {
                int idx = 0;
                for (int x = 0; x < _nx; x++)
                {
                    for (int y = 0; y < _ny; y++)
                    {
                        for (int z = 0; z < _nz; z++)
                        {
                            _fracXFlat[idx] = grid.FracX[x, y, z];
                            _fracYFlat[idx] = grid.FracY[x, y, z];
                            _fracZFlat[idx] = grid.FracZ[x, y, z];
                            idx++;
                        }
                    }
                }
            }

 // System.Numerics matrices
            var invFrac = cavity.InvFracToCart;
            _sysInvFracToCart = new SysMatrix4x4(
                invFrac.M11, invFrac.M12, invFrac.M13, invFrac.M14,
                invFrac.M21, invFrac.M22, invFrac.M23, invFrac.M24,
                invFrac.M31, invFrac.M32, invFrac.M33, invFrac.M34,
                invFrac.M41, invFrac.M42, invFrac.M43, invFrac.M44
            );

            var frac = cavity.InvFracToCart;
            _sysFracToCart = new SysMatrix4x4(
                frac.M11, frac.M12, frac.M13, frac.M14,
                frac.M21, frac.M22, frac.M23, frac.M24,
                frac.M31, frac.M32, frac.M33, frac.M34,
                frac.M41, frac.M42, frac.M43, frac.M44
            );

            _minCornerVec = new Vector3(cavity.MinCorner.X, cavity.MinCorner.Y, cavity.MinCorner.Z);

            PrecomputeAngles(20);
            FindMaxSDF();
        }

        #endregion

        #region Initialization

        private void PrecomputeAngles(int steps)
        {
            _precomputedSteps = steps;
            _precomputedSin = new float[steps * 2];
            _precomputedCos = new float[steps * 2];
            for (int i = 0; i < steps * 2; i++)
            {
                float theta = (float)(i * Math.PI / steps);
                _precomputedSin[i] = MathF.Sin(theta);
                _precomputedCos[i] = MathF.Cos(theta);
            }
        }

        private void FindMaxSDF()
        {
            _maxSDFInCavity = 1.0f;
            if (_densityFlat == null || _densityFlat.Length == 0) return;

            float maxVal = 0;
            int startX = Math.Max(0, _offsetX);
            int endX = Math.Min(_nx, _offsetX + _periodicResX);
            int startY = Math.Max(0, _offsetY);
            int endY = Math.Min(_ny, _offsetY + _periodicResY);
            int startZ = Math.Max(0, _offsetZ);
            int endZ = Math.Min(_nz, _offsetZ + _periodicResZ);

            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    for (int z = startZ; z < endZ; z++)
                    {
                        int idx = GetIndex(x, y, z);
                        float val = _densityFlat[idx];
                        if (val > 0 && val > maxVal) maxVal = val;
                    }
                }
            }

            if (maxVal > 0.1f)
            {
                _maxSDFInCavity = maxVal;
                if (EnableDetailedLogging)
                    System.Diagnostics.Debug.WriteLine($"[PATH] Max SDF in cavity: {_maxSDFInCavity:F3}");
            }
        }

        #endregion

        #region Fast Access Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetIndex(int x, int y, int z)
        {
            return (x * _ny + y) * _nz + z;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsValidIndex(int idx)
        {
            return idx >= 0 && idx < _totalVoxels;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsWall(int idx)
        {
            _totalWallChecks++;
            return _densityFlat[idx] < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetSDFValue(int idx)
        {
            return _densityFlat[idx];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SysVector3 GetPosition(int idx)
        {
            return _positionsFlat[idx];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 GetPositionVec3(int idx)
        {
            var pos = _positionsFlat[idx];
            return new Vector3(pos.X, pos.Y, pos.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetFracX(int idx) => _fracXFlat[idx];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetFracY(int idx) => _fracYFlat[idx];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetFracZ(int idx) => _fracZFlat[idx];

        #endregion

        #region Neighbor Access

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetNeighbors(int idx, Span<(int idx, int x, int y, int z)> output, out int count)
        {
            int x = idx / (_ny * _nz);
            int y = (idx / _nz) % _ny;
            int z = idx % _nz;

            count = 0;
            for (int i = 0; i < NEIGHBOR_COUNT; i++)
            {
                int nx = x + NeighborDx[i];
                int ny = y + NeighborDy[i];
                int nz = z + NeighborDz[i];

                if (nx < 0) nx = _nx - 1;
                else if (nx >= _nx) nx = 0;

                if (ny < 0) ny = _ny - 1;
                else if (ny >= _ny) ny = 0;

                if (nz < 0) nz = _nz - 1;
                else if (nz >= _nz) nz = 0;

                int nIdx = GetIndex(nx, ny, nz);
                output[count++] = (nIdx, nx, ny, nz);
            }

            _totalNeighborChecks += count;
        }

        #endregion

        #region Main Analysis Method

        public ChannelPathResult AnalyzeChannel(string direction)
        {
            if (string.IsNullOrEmpty(direction))
                return null;

            if (_densityFlat == null || _densityFlat.Length == 0)
            {
                return new ChannelPathResult { Direction = direction, IsConnected = false };
            }

            ResetStats();

            if (EnableDetailedLogging)
                System.Diagnostics.Debug.WriteLine($"[PATH] ===== ANALYZING {direction} (MULTI-START) =====");

            int wallMin, wallMax;
            string axisName;
            GetWallCoordinates(direction, out wallMin, out wallMax, out axisName);

            if (EnableDetailedLogging)
                System.Diagnostics.Debug.WriteLine($"[PATH] Walls: {axisName} MIN={wallMin}, MAX={wallMax}");

            var startPoints = FindAllStartPoints(direction, MaxStartPoints);

            if (startPoints.Count == 0)
            {
                if (EnableDetailedLogging)
                    System.Diagnostics.Debug.WriteLine($"[PATH] No start points found for {direction}");
                return new ChannelPathResult { Direction = direction, IsConnected = false };
            }

            if (EnableDetailedLogging)
                System.Diagnostics.Debug.WriteLine($"[PATH] Found {startPoints.Count} start points");

            var allPaths = new List<ExtendedChannelPathResult>();
            int totalAttempts = 0;
            int successfulPaths = 0;

            Span<(int idx, int x, int y, int z)> neighborBuffer = stackalloc (int, int, int, int)[NEIGHBOR_COUNT];

            foreach (var start in startPoints)
            {
                totalAttempts++;
                int startIdx = GetIndex(start.x, start.y, start.z);

                if (EnableDetailedLogging)
                    System.Diagnostics.Debug.WriteLine($"[PATH] Trying start point ({start.x},{start.y},{start.z})");

                var pathToMin = FindPathToWall(startIdx, wallMin, direction, neighborBuffer);
                var pathToMax = FindPathToWall(startIdx, wallMax, direction, neighborBuffer);

                if (pathToMin != null && pathToMin.Count > 1 && pathToMax != null && pathToMax.Count > 1)
                {
                    var fullPath = new List<PathPoint>();
                    var reversedMin = new List<PathPoint>(pathToMin);
                    reversedMin.Reverse();
                    fullPath.AddRange(reversedMin);
                    fullPath.AddRange(pathToMax.Skip(1));

                    if (fullPath.Count > 2)
                    {
                        var path = CreateFullPathResult(fullPath, start, wallMin, wallMax, direction);
                        if (path != null)
                        {
                            allPaths.Add(path);
                            successfulPaths++;
                            if (EnableDetailedLogging)
                                System.Diagnostics.Debug.WriteLine($"[PATH]   ✓ Found FULL path: {path.PathPoints} pts, len={path.TotalLength:F2} A");
                        }
                    }
                }
                else
                {
                    if (EnableDetailedLogging)
                    {
                        if (pathToMin == null || pathToMin.Count <= 1)
                            System.Diagnostics.Debug.WriteLine($"[PATH]   ✗ No path to MIN");
                        if (pathToMax == null || pathToMax.Count <= 1)
                            System.Diagnostics.Debug.WriteLine($"[PATH]   ✗ No path to MAX");
                    }
                }
            }

            var stats = new ChannelPathStatistics
            {
                Direction = direction,
                TotalPathsFound = totalAttempts,
                ValidPathsFound = allPaths.Count
            };

            if (allPaths.Count > 0)
            {
                stats.MinLength = allPaths.Min(p => p.TotalLength);
                stats.MaxLength = allPaths.Max(p => p.TotalLength);
                stats.AvgLength = allPaths.Average(p => p.TotalLength);

                stats.MinDiameter = allPaths.Min(p => p.AverageWidth);
                stats.MaxDiameter = allPaths.Max(p => p.AverageWidth);
                stats.AvgDiameter = allPaths.Average(p => p.AverageWidth);

                stats.MinHelicity = allPaths.Min(p => p.Helicity);
                stats.MaxHelicity = allPaths.Max(p => p.Helicity);
                stats.AvgHelicity = allPaths.Average(p => p.Helicity);

                stats.ShortestPath = allPaths.OrderBy(p => p.TotalLength).FirstOrDefault();
                stats.WidestPath = allPaths.OrderByDescending(p => p.AverageWidth).FirstOrDefault();
                stats.StraightestPath = allPaths.OrderBy(p => Math.Abs(p.Helicity)).FirstOrDefault();
            }

            ChannelPathResult bestPath = null;

            if (allPaths.Count > 0)
            {
                var widePaths = allPaths.Where(p => p.AverageWidth > 0.8f).ToList();
                if (widePaths.Count > 0)
                {
                    bestPath = widePaths.OrderByDescending(p => p.AverageWidth).First();
                }
                else
                {
                    bestPath = allPaths.OrderByDescending(p => p.AverageWidth).First();
                }
            }

            if (EnableDetailedLogging)
            {
                System.Diagnostics.Debug.WriteLine($"[PATH] ===== {direction} STATISTICS =====");
                System.Diagnostics.Debug.WriteLine($"[PATH] {stats}");

                if (stats.ShortestPath != null)
                    System.Diagnostics.Debug.WriteLine($"[PATH] Shortest: {stats.ShortestPath.TotalLength:F2} A, diam: {stats.ShortestPath.AverageWidth:F2} A");
                if (stats.WidestPath != null)
                    System.Diagnostics.Debug.WriteLine($"[PATH] Widest:   {stats.WidestPath.TotalLength:F2} A, diam: {stats.WidestPath.AverageWidth:F2} A");
                if (stats.StraightestPath != null)
                    System.Diagnostics.Debug.WriteLine($"[PATH] Straight: {stats.StraightestPath.TotalLength:F2} A, hel: {stats.StraightestPath.Helicity:F2}°");

                System.Diagnostics.Debug.WriteLine($"[PATH] All {allPaths.Count} full paths:");
                int displayCount = Math.Min(allPaths.Count, 10);
                for (int i = 0; i < displayCount; i++)
                {
                    var p = allPaths[i];
                    System.Diagnostics.Debug.WriteLine($"[PATH]   {i + 1}: len={p.TotalLength:F2} A, diam={p.AverageWidth:F2} A, hel={p.Helicity:F2}°");
                }
                if (allPaths.Count > 10)
                    System.Diagnostics.Debug.WriteLine($"[PATH]   ... and {allPaths.Count - 10} more");

                System.Diagnostics.Debug.WriteLine($"[PATH] Success rate: {successfulPaths}/{totalAttempts} ({100.0f * successfulPaths / Math.Max(1, totalAttempts):F1}%)");
                System.Diagnostics.Debug.WriteLine($"[PATH] ===== {direction} DONE =====");
            }

            if (bestPath != null)
            {
                bestPath.Properties["TotalPathsFound"] = stats.TotalPathsFound;
                bestPath.Properties["ValidPathsFound"] = stats.ValidPathsFound;
                bestPath.Properties["AvgLength"] = stats.AvgLength;
                bestPath.Properties["AvgDiameter"] = stats.AvgDiameter;
                bestPath.Properties["AvgHelicity"] = stats.AvgHelicity;
                bestPath.Properties["MinLength"] = stats.MinLength;
                bestPath.Properties["MaxLength"] = stats.MaxLength;
                bestPath.Properties["MinDiameter"] = stats.MinDiameter;
                bestPath.Properties["MaxDiameter"] = stats.MaxDiameter;
                bestPath.Properties["MinHelicity"] = stats.MinHelicity;
                bestPath.Properties["MaxHelicity"] = stats.MaxHelicity;
                bestPath.Properties["TotalAttempts"] = stats.TotalPathsFound;
                bestPath.Properties["SuccessRate"] = stats.ValidPathsFound / (float)Math.Max(1, stats.TotalPathsFound);

                bestPath.IsConnected = true;
                bestPath.ConvertToXna();
            }
            else
            {
                if (EnableDetailedLogging)
                    System.Diagnostics.Debug.WriteLine($"[PATH] No valid paths found for {direction}");
                return new ChannelPathResult { Direction = direction, IsConnected = false };
            }

            return bestPath;
        }

        #endregion

        #region Path Finding

        private List<PathPoint> FindPathToWall(
            int startIdx,
            int wallCoord,
            string direction,
            Span<(int idx, int x, int y, int z)> neighborBuffer)
        {
            if (!IsValidIndex(startIdx) || IsWall(startIdx))
                return null;

            int estimatedSize = Math.Min(MaxPathLength / 10, 10000);
            var cameFrom = new Dictionary<int, int>(estimatedSize);
            var gScore = new Dictionary<int, float>(estimatedSize);
            var fScore = new Dictionary<int, float>(estimatedSize);

            var openSet = new PriorityQueue<int, float>();

            gScore[startIdx] = 0;
            fScore[startIdx] = HeuristicToWall(startIdx, wallCoord, direction);
            openSet.Enqueue(startIdx, fScore[startIdx]);

            int iterations = 0;
            var visited = new HashSet<int>(estimatedSize);

            while (openSet.Count > 0 && iterations < MaxPathLength)
            {
                iterations++;

                if (!openSet.TryDequeue(out int current))
                    break;

                if (visited.Contains(current)) continue;
                visited.Add(current);

                int cx = current / (_ny * _nz);
                int cy = (current / _nz) % _ny;
                int cz = current % _nz;

                bool reachedWall = (direction == "XX" && cx == wallCoord) ||
                                   (direction == "YY" && cy == wallCoord) ||
                                   (direction == "ZZ" && cz == wallCoord);

                if (reachedWall && !IsWall(current))
                {
                    return ReconstructPath(cameFrom, current);
                }

                GetNeighbors(current, neighborBuffer, out int neighborCount);

                for (int i = 0; i < neighborCount; i++)
                {
                    var neighbor = neighborBuffer[i];
                    if (visited.Contains(neighbor.idx) || IsWall(neighbor.idx))
                        continue;

                    float moveCost = ComputeMoveCost(current, neighbor.idx);
                    float tentativeG = gScore[current] + moveCost;

                    if (!gScore.TryGetValue(neighbor.idx, out float existingG) || tentativeG < existingG)
                    {
                        cameFrom[neighbor.idx] = current;
                        gScore[neighbor.idx] = tentativeG;
                        float heuristic = HeuristicToWall(neighbor.idx, wallCoord, direction);
                        fScore[neighbor.idx] = tentativeG + heuristic;
                        if (!openSet.Contains(neighbor.idx))
                            openSet.Enqueue(neighbor.idx, fScore[neighbor.idx]);
                    }
                }
            }

            if (iterations >= MaxPathLength && EnableDetailedLogging)
            {
                System.Diagnostics.Debug.WriteLine($"[PATH] Path search exceeded max iterations ({MaxPathLength})");
            }

            return null;
        }

        #endregion

        #region Path Reconstruction

        private List<PathPoint> ReconstructPath(Dictionary<int, int> cameFrom, int current)
        {
            int estimatedSize = Math.Min(cameFrom.Count, 1000);
            var path = new List<PathPoint>(estimatedSize);

            int node = current;
            int safety = 0;

            while (cameFrom.TryGetValue(node, out int prev) && safety < 100000)
            {
                var pos = GetPosition(node);
                path.Add(new PathPoint(
                    node / (_ny * _nz),
                    (node / _nz) % _ny,
                    node % _nz,
                    pos
                ));
                node = prev;
                safety++;
            }

            if (safety >= 100000 && EnableDetailedLogging)
            {
                System.Diagnostics.Debug.WriteLine($"[PATH] Path reconstruction exceeded 100000 steps");
            }

            var startPos = GetPosition(node);
            path.Add(new PathPoint(
                node / (_ny * _nz),
                (node / _nz) % _ny,
                node % _nz,
                startPos
            ));

            path.Reverse();
            return path;
        }

        #endregion

        #region Heuristics and Cost

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float HeuristicToWall(int idx, int wallCoord, string direction)
        {
            int x = idx / (_ny * _nz);
            int y = (idx / _nz) % _ny;
            int z = idx % _nz;

            if (direction == "XX")
                return Math.Abs(wallCoord - x);
            else if (direction == "YY")
                return Math.Abs(wallCoord - y);
            else
                return Math.Abs(wallCoord - z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float ComputeMoveCost(int fromIdx, int toIdx)
        {
            var posFrom = GetPosition(fromIdx);
            var posTo = GetPosition(toIdx);

            float dx = posTo.X - posFrom.X;
            float dy = posTo.Y - posFrom.Y;
            float dz = posTo.Z - posFrom.Z;
            float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

            float cost = distance;

            if (UseSDFWeight)
            {
                float sdfValue = GetSDFValue(toIdx);
                float normalizedSDF = Math.Clamp(sdfValue / _maxSDFInCavity, 0.001f, 1.0f);
                float sdfPenalty = (1.0f - normalizedSDF) * SDFWeight;
                cost += distance * sdfPenalty;
            }

            return Math.Max(0.001f, cost);
        }

        #endregion

        #region Start Points

        private List<(int x, int y, int z)> FindAllStartPoints(string direction, int maxPoints)
        {
            var points = new List<(int x, int y, int z)>();

            int cx = _offsetX + _periodicResX / 2;
            int cy = _offsetY + _periodicResY / 2;
            int cz = _offsetZ + _periodicResZ / 2;

            int maxRadius = Math.Max(_periodicResX, Math.Max(_periodicResY, _periodicResZ)) / 2;
            int step = Math.Max(1, maxRadius / 8);

            for (int r = 0; r < maxRadius && points.Count < maxPoints; r += step)
            {
                for (int dx = -r; dx <= r && points.Count < maxPoints; dx += step)
                {
                    for (int dy = -r; dy <= r && points.Count < maxPoints; dy += step)
                    {
                        for (int dz = -r; dz <= r && points.Count < maxPoints; dz += step)
                        {
                            if (r > 0 && Math.Abs(dx) != r && Math.Abs(dy) != r && Math.Abs(dz) != r)
                                continue;

                            int x = cx + dx;
                            int y = cy + dy;
                            int z = cz + dz;

                            if (x < 0 || x >= _nx || y < 0 || y >= _ny || z < 0 || z >= _nz)
                                continue;

                            int idx = GetIndex(x, y, z);
                            if (!IsWall(idx) && HasSpaceAround(idx, 2))
                            {
                                points.Add((x, y, z));
                            }
                        }
                    }
                }
            }

            if (points.Count < 3)
            {
                if (EnableDetailedLogging)
                    System.Diagnostics.Debug.WriteLine($"[PATH] Not enough start points, using random sampling...");

                int attempts = 0;
                int maxAttempts = 10000;

                while (points.Count < maxPoints && attempts < maxAttempts)
                {
                    attempts++;
                    int x = _offsetX + _random.Value.Next(_periodicResX);
                    int y = _offsetY + _random.Value.Next(_periodicResY);
                    int z = _offsetZ + _random.Value.Next(_periodicResZ);

                    if (x < 0 || x >= _nx || y < 0 || y >= _ny || z < 0 || z >= _nz)
                        continue;

                    int idx = GetIndex(x, y, z);
                    if (!IsWall(idx) && HasSpaceAround(idx, 2))
                    {
                        points.Add((x, y, z));
                    }
                }
            }

            return points;
        }

        private bool HasSpaceAround(int idx, int radius)
        {
            int x = idx / (_ny * _nz);
            int y = (idx / _nz) % _ny;
            int z = idx % _nz;

            int[] dx = { 1, -1, 0, 0, 0, 0 };
            int[] dy = { 0, 0, 1, -1, 0, 0 };
            int[] dz = { 0, 0, 0, 0, 1, -1 };

            for (int i = 0; i < 6; i++)
            {
                int nx = x + dx[i] * radius;
                int ny = y + dy[i] * radius;
                int nz = z + dz[i] * radius;

                if (nx < 0 || nx >= _nx || ny < 0 || ny >= _ny || nz < 0 || nz >= _nz)
                    return false;

                int nIdx = GetIndex(nx, ny, nz);
                if (IsWall(nIdx))
                    return false;
            }

            return true;
        }

        #endregion

        #region Helper Methods

        private void GetWallCoordinates(string direction, out int wallMin, out int wallMax, out string axisName)
        {
            if (direction == "XX")
            {
                wallMin = _offsetX;
                wallMax = _offsetX + _periodicResX - 1;
                axisName = "X";
            }
            else if (direction == "YY")
            {
                wallMin = _offsetY;
                wallMax = _offsetY + _periodicResY - 1;
                axisName = "Y";
            }
            else
            {
                wallMin = _offsetZ;
                wallMax = _offsetZ + _periodicResZ - 1;
                axisName = "Z";
            }
        }

        private void ResetStats()
        {
            _totalWallChecks = 0;
            _totalNeighborChecks = 0;
        }

        #endregion

        #region Path Result Creation

        private ExtendedChannelPathResult CreateFullPathResult(
            List<PathPoint> fullPath,
            (int x, int y, int z) start,
            int wallMin,
            int wallMax,
            string direction)
        {
            if (fullPath == null || fullPath.Count < 3)
                return null;

            var result = new ExtendedChannelPathResult
            {
                Direction = direction,
                IsConnected = true,
                StartPoint = new Vector3(fullPath.First().Position.X, fullPath.First().Position.Y, fullPath.First().Position.Z),
                EndPoint = new Vector3(fullPath.Last().Position.X, fullPath.Last().Position.Y, fullPath.Last().Position.Z),
                StartIndex = (fullPath.First().X, fullPath.First().Y, fullPath.First().Z),
                EndIndex = (fullPath.Last().X, fullPath.Last().Y, fullPath.Last().Z),
                StartVoxelIndex = (fullPath.First().X, fullPath.First().Y, fullPath.First().Z),
                EndVoxelIndex = (fullPath.Last().X, fullPath.Last().Y, fullPath.Last().Z),
                StartWallType = 0,
                PathPoints = fullPath.Count,
                WorldPath = fullPath.Select(p => new Vector3(p.Position.X, p.Position.Y, p.Position.Z)).ToList(),
                WorldPathXna = fullPath.Select(p => new Microsoft.Xna.Framework.Vector3(p.Position.X, p.Position.Y, p.Position.Z)).ToList(),
                SelectedWall = 0
            };

            result.TotalLength = CalculatePathLength(fullPath);
            result.OriginalLength = result.TotalLength;
            result.Helicity = CalculateHelicity(fullPath, direction);
            result.CellCrossings = CalculateCellCrossings(fullPath);

            float avgDiam = EstimateAverageDiameter(fullPath);
            float minDiam = EstimateMinDiameter(fullPath);
            float maxDiam = EstimateMaxDiameter(fullPath);

            result.AverageWidth = avgDiam;
            result.MinDiameter = minDiam;
            result.MinWidth = minDiam;
            result.MaxDiameter = maxDiam;
            result.MaxWidth = maxDiam;
            result.AvgDiameter = avgDiam;
            result.EffectiveDiameter = avgDiam * 0.8f;

            if (SmoothingIterations > 0 && result.WorldPath.Count > 3)
            {
                var smoothed = SmoothPath(result.WorldPath, SmoothingIterations);
                result.SmoothedLength = CalculateSmoothedLength(smoothed);
                result.SmoothingError = CalculateSmoothingError(result.WorldPath, smoothed);
                result.SmoothingErrorPercent = result.OriginalLength > 0 ?
                    (result.SmoothingError / result.OriginalLength) * 100f : 0;
                result.WorldPath = smoothed;
                result.WorldPathXna = smoothed.Select(v => new Microsoft.Xna.Framework.Vector3(v.X, v.Y, v.Z)).ToList();
                result.TotalLength = result.SmoothedLength;
            }

            return result;
        }

        #endregion

        #region Path Calculations

        private float CalculatePathLength(List<PathPoint> path)
        {
            if (path.Count < 2) return 0;

            float length = 0;
            for (int i = 1; i < path.Count; i++)
            {
                float dx = path[i].Position.X - path[i - 1].Position.X;
                float dy = path[i].Position.Y - path[i - 1].Position.Y;
                float dz = path[i].Position.Z - path[i - 1].Position.Z;
                length += MathF.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            return length;
        }

        private float CalculateHelicity(List<PathPoint> path, string direction)
        {
            if (path.Count < 3) return 0;

            int mid = path.Count / 2;
            var p1 = new Vector3(path[0].Position.X, path[0].Position.Y, path[0].Position.Z);
            var p2 = new Vector3(path[mid].Position.X, path[mid].Position.Y, path[mid].Position.Z);
            var p3 = new Vector3(path[path.Count - 1].Position.X, path[path.Count - 1].Position.Y, path[path.Count - 1].Position.Z);

            var v1 = p2 - p1;
            var v2 = p3 - p2;

            float dot = Vector3.Dot(v1, v2);
            float len1 = v1.Length();
            float len2 = v2.Length();

            if (len1 < 0.001f || len2 < 0.001f) return 0;

            float cosAngle = dot / (len1 * len2);
            cosAngle = Math.Clamp(cosAngle, -1f, 1f);
            float angle = MathF.Acos(cosAngle) * 180f / MathF.PI;

            var cross = Vector3.Cross(v1, v2);
            float sign = 1;
            if (direction == "XX" && cross.X < 0) sign = -1;
            else if (direction == "YY" && cross.Y < 0) sign = -1;
            else if (direction == "ZZ" && cross.Z < 0) sign = -1;

            return angle * sign;
        }

        private int CalculateCellCrossings(List<PathPoint> path)
        {
            if (path.Count < 2) return 0;

            int crossings = 0;
            var prevFrac = WorldToFrac(new Vector3(path[0].Position.X, path[0].Position.Y, path[0].Position.Z));

            for (int i = 1; i < path.Count; i++)
            {
                var currFrac = WorldToFrac(new Vector3(path[i].Position.X, path[i].Position.Y, path[i].Position.Z));
                if (Math.Abs(currFrac.X - prevFrac.X) > 0.5f) crossings++;
                if (Math.Abs(currFrac.Y - prevFrac.Y) > 0.5f) crossings++;
                if (Math.Abs(currFrac.Z - prevFrac.Z) > 0.5f) crossings++;
                prevFrac = currFrac;
            }
            return crossings;
        }

        private Vector3 WorldToFrac(Vector3 pos)
        {
            var translated = pos - _minCornerVec;
            float fx = _sysInvFracToCart.M11 * translated.X + _sysInvFracToCart.M12 * translated.Y + _sysInvFracToCart.M13 * translated.Z + _sysInvFracToCart.M14;
            float fy = _sysInvFracToCart.M21 * translated.X + _sysInvFracToCart.M22 * translated.Y + _sysInvFracToCart.M23 * translated.Z + _sysInvFracToCart.M24;
            float fz = _sysInvFracToCart.M31 * translated.X + _sysInvFracToCart.M32 * translated.Y + _sysInvFracToCart.M33 * translated.Z + _sysInvFracToCart.M34;
            return new Vector3(fx, fy, fz);
        }

        #endregion

        #region Diameter Estimation

        private float EstimateAverageDiameter(List<PathPoint> path)
        {
            if (path.Count < 2) return 0;

            int samples = Math.Min(path.Count, 20);
            int step = Math.Max(1, path.Count / samples);
            float total = 0;
            int count = 0;

            for (int i = 0; i < path.Count && count < samples; i += step, count++)
            {
                int idx = GetIndex(path[i].X, path[i].Y, path[i].Z);
                float radius = EstimateLocalRadius(idx);
                total += radius * 2;
            }

            return count > 0 ? total / count : 0;
        }

        private float EstimateMinDiameter(List<PathPoint> path)
        {
            if (path.Count < 2) return 0;

            int samples = Math.Min(path.Count, 20);
            int step = Math.Max(1, path.Count / samples);
            float min = float.MaxValue;
            int count = 0;

            for (int i = 0; i < path.Count && count < samples; i += step, count++)
            {
                int idx = GetIndex(path[i].X, path[i].Y, path[i].Z);
                float radius = EstimateLocalRadius(idx);
                min = Math.Min(min, radius * 2);
            }

            return min == float.MaxValue ? 0 : min;
        }

        private float EstimateMaxDiameter(List<PathPoint> path)
        {
            if (path.Count < 2) return 0;

            int samples = Math.Min(path.Count, 20);
            int step = Math.Max(1, path.Count / samples);
            float max = 0;
            int count = 0;

            for (int i = 0; i < path.Count && count < samples; i += step, count++)
            {
                int idx = GetIndex(path[i].X, path[i].Y, path[i].Z);
                float radius = EstimateLocalRadius(idx);
                max = Math.Max(max, radius * 2);
            }

            return max;
        }

        private float EstimateLocalRadius(int idx)
        {
            return EstimateLocalWidth(idx) / 2;
        }

        private float EstimateLocalWidth(int idx)
        {
            int x = idx / (_ny * _nz);
            int y = (idx / _nz) % _ny;
            int z = idx % _nz;

            float maxRadius = 5.0f;
            float step = 0.3f;
            int maxIterations = 0;

            for (float r = step; r < maxRadius; r += step)
            {
                int steps = Math.Max(1, (int)(r / step));
                bool foundWall = false;
                int iterationsThisRadius = 0;

                for (int i = 0; i < steps * 2 && iterationsThisRadius < 500; i++)
                {
                    float theta = _precomputedSin[i % _precomputedSin.Length];
                    float cosTheta = _precomputedCos[i % _precomputedCos.Length];

                    for (int j = 0; j < steps * 2 && iterationsThisRadius < 500; j++)
                    {
                        float phi = (float)(j * 2 * Math.PI / steps);
                        int dx = (int)(r * theta * MathF.Cos(phi));
                        int dy = (int)(r * theta * MathF.Sin(phi));
                        int dz = (int)(r * cosTheta);

                        int nx = x + dx;
                        int ny = y + dy;
                        int nz = z + dz;

                        if (nx < 0 || nx >= _nx || ny < 0 || ny >= _ny || nz < 0 || nz >= _nz)
                        {
                            foundWall = true;
                            break;
                        }

                        int nIdx = GetIndex(nx, ny, nz);
                        if (IsWall(nIdx))
                        {
                            foundWall = true;
                            break;
                        }
                        iterationsThisRadius++;
                    }
                    if (foundWall) break;
                    maxIterations += iterationsThisRadius;
                }
                if (foundWall) return r * 2;

                if (maxIterations > 10000) break;
            }
            return maxRadius * 2;
        }

        #endregion

        #region Smoothing

        private List<Vector3> SmoothPath(List<Vector3> path, int iterations)
        {
            if (path.Count < 3 || iterations <= 0)
                return new List<Vector3>(path);

            var smoothed = new List<Vector3>(path);

            for (int iter = 0; iter < iterations; iter++)
            {
                var current = new List<Vector3>(smoothed);
                for (int i = 1; i < smoothed.Count - 1; i++)
                {
                    smoothed[i] = (current[i - 1] + current[i] * 2.0f + current[i + 1]) / 4.0f;
                }
            }

            return smoothed;
        }

        private float CalculateSmoothingError(List<Vector3> original, List<Vector3> smoothed)
        {
            if (original.Count != smoothed.Count || original.Count < 2)
                return 0;

            float sumSq = 0;
            for (int i = 0; i < original.Count; i++)
            {
                float dx = original[i].X - smoothed[i].X;
                float dy = original[i].Y - smoothed[i].Y;
                float dz = original[i].Z - smoothed[i].Z;
                sumSq += dx * dx + dy * dy + dz * dz;
            }

            return MathF.Sqrt(sumSq / original.Count);
        }

        private float CalculateSmoothedLength(List<Vector3> path)
        {
            if (path.Count < 2) return 0;
            float length = 0;
            for (int i = 1; i < path.Count; i++)
            {
                float dx = path[i].X - path[i - 1].X;
                float dy = path[i].Y - path[i - 1].Y;
                float dz = path[i].Z - path[i - 1].Z;
                length += MathF.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            return length;
        }

        #endregion

        #region All Channels Analysis

        public Dictionary<string, ChannelPathResult> AnalyzeAllChannels(CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<string, ChannelPathResult>();
            var directions = new[] { "XX", "YY", "ZZ" };
            var lockObj = new object();

            if (EnableDetailedLogging)
                System.Diagnostics.Debug.WriteLine($"[PATH] ===== Analyzing ALL channels (MULTI-START) =====");

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Min(3, Environment.ProcessorCount),
                CancellationToken = cancellationToken
            };

            Parallel.ForEach(directions, options, dir =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var localPathFinder = new CavityPathFinder(_cavity, ProbeRadius);
                localPathFinder.MaxPathLength = this.MaxPathLength;
                localPathFinder.EnableDetailedLogging = this.EnableDetailedLogging;
                localPathFinder.UseSDFWeight = this.UseSDFWeight;
                localPathFinder.SDFWeight = this.SDFWeight;
                localPathFinder.SmoothingIterations = this.SmoothingIterations;
                localPathFinder.MaxStartPoints = this.MaxStartPoints;

                if (EnableDetailedLogging)
                    System.Diagnostics.Debug.WriteLine($"[PATH] --- Starting {dir} ---");

                try
                {
                    var result = localPathFinder.AnalyzeChannel(dir);
                    lock (lockObj)
                    {
                        results[dir] = result;
                    }
                }
                catch (OperationCanceledException)
                {
                    lock (lockObj)
                    {
                        results[dir] = new ChannelPathResult { Direction = dir, IsConnected = false };
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PATH] ERROR in {dir}: {ex.Message}");
                    lock (lockObj)
                    {
                        results[dir] = new ChannelPathResult { Direction = dir, IsConnected = false };
                    }
                }

                if (EnableDetailedLogging)
                    System.Diagnostics.Debug.WriteLine($"[PATH] --- {dir} DONE ---");
            });

            if (EnableDetailedLogging)
            {
                System.Diagnostics.Debug.WriteLine($"[PATH] ===== SUMMARY =====");
                foreach (var kvp in results)
                {
                    if (kvp.Value != null && kvp.Value.IsConnected)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PATH]   {kvp.Key}: {kvp.Value.TotalLength:F2}A, diam={kvp.Value.AverageWidth:F2}A");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[PATH]   {kvp.Key}: NOT CONNECTED");
                    }
                }
            }

            return results;
        }

        #endregion

        #region Priority Queue

        private class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
        {
            private readonly SortedDictionary<TPriority, Queue<TElement>> _dict =
                new SortedDictionary<TPriority, Queue<TElement>>();

            public int Count { get; private set; }

            public void Enqueue(TElement element, TPriority priority)
            {
                if (!_dict.TryGetValue(priority, out var queue))
                {
                    queue = new Queue<TElement>();
                    _dict[priority] = queue;
                }
                queue.Enqueue(element);
                Count++;
            }

            public TElement Dequeue()
            {
                if (Count == 0)
                    throw new InvalidOperationException("Queue is empty");

                var first = _dict.First();
                var element = first.Value.Dequeue();
                if (first.Value.Count == 0)
                    _dict.Remove(first.Key);
                Count--;
                return element;
            }

            public bool TryDequeue(out TElement element)
            {
                if (Count == 0)
                {
                    element = default;
                    return false;
                }

                var first = _dict.First();
                element = first.Value.Dequeue();
                if (first.Value.Count == 0)
                    _dict.Remove(first.Key);
                Count--;
                return true;
            }

            public bool Contains(TElement element)
            {
                foreach (var queue in _dict.Values)
                    if (queue.Contains(element)) return true;
                return false;
            }
        }

        #endregion
    }

    #region Path Point

    public class PathPoint
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        public SysVector3 Position { get; }

        public PathPoint(int x, int y, int z, SysVector3 position)
        {
            X = x;
            Y = y;
            Z = z;
            Position = position;
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z}) @ ({Position.X:F3}, {Position.Y:F3}, {Position.Z:F3})";
        }
    }

    #endregion

    #region Channel Path Statistics

    public class ChannelPathStatistics
    {
        public string Direction { get; set; }
        public int TotalPathsFound { get; set; }
        public int ValidPathsFound { get; set; }

        public float MinLength { get; set; } = float.MaxValue;
        public float MaxLength { get; set; } = float.MinValue;
        public float AvgLength { get; set; }

        public float MinDiameter { get; set; } = float.MaxValue;
        public float MaxDiameter { get; set; } = float.MinValue;
        public float AvgDiameter { get; set; }

        public float MinHelicity { get; set; } = float.MaxValue;
        public float MaxHelicity { get; set; } = float.MinValue;
        public float AvgHelicity { get; set; }

        public CavityPathFinder.ChannelPathResult ShortestPath { get; set; }
        public CavityPathFinder.ChannelPathResult WidestPath { get; set; }
        public CavityPathFinder.ChannelPathResult StraightestPath { get; set; }

        public override string ToString()
        {
            return $"[{Direction}] Paths: {ValidPathsFound}/{TotalPathsFound}, " +
                   $"Length: {MinLength:F2}-{MaxLength:F2} (avg: {AvgLength:F2}) A, " +
                   $"Diameter: {MinDiameter:F2}-{MaxDiameter:F2} (avg: {AvgDiameter:F2}) A, " +
                   $"Helicity: {MinHelicity:F2}-{MaxHelicity:F2} (avg: {AvgHelicity:F2}) deg";
        }
    }

    #endregion
}