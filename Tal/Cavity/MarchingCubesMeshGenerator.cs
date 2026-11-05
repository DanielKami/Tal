// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: MarchingCubesMeshGenerator.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Tal.Diagnostics;
using Tal.GUI;
using static Tal.Cavity.CavityDetectorBase;
using Color = Microsoft.Xna.Framework.Color;

using SysMatrix4x4 = System.Numerics.Matrix4x4;
using SysVector3 = System.Numerics.Vector3;
using SysVector4 = System.Numerics.Vector4;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;

namespace Tal.Cavity
{
 /// <summary>
 /// Generates triangle meshes from density grids using the Marching Cubes algorithm.
 /// Fully optimized with parallel processing, pre-computed tables, fast vertex merging, and smoothing.
 /// All vectors use System.Numerics types except for final rendering conversion.
 /// </summary>
    public static class MarchingCubesMeshGenerator
    {
        #region Thread-Safe List Pool

        private static class ListPool<T>
        {
            private static readonly System.Collections.Concurrent.ConcurrentBag<List<T>> _pool
                = new System.Collections.Concurrent.ConcurrentBag<List<T>>();

            public static List<T> Rent()
            {
                if (_pool.TryTake(out var list))
                {
                    list.Clear();
                    return list;
                }
                return new List<T>();
            }

            public static void Return(List<T> list)
            {
                if (list == null) return;
                list.Clear();
                _pool.Add(list);
            }
        }

        #endregion

        #region Cube Data

        private struct CubeData
        {
            public int EdgeFlag;
            public int[] ActiveEdges;
            public (int e0, int e1, int e2)[] Triangles;
        }

 /// <summary>Represents a triangle mesh with vertices, indices, normals and color.</summary>
        public class MeshData : IDisposable
        {
            public List<SysVector3> Vertices { get; set; } = new List<SysVector3>();
            public List<int> Indices { get; set; } = new List<int>();
            public List<SysVector3> Normals { get; set; } = new List<SysVector3>();
            public Color Color { get; set; } = Color.White;
            public int CavityId { get; set; } = -1;

            public int TriangleCount => Indices?.Count / 3 ?? 0;
            public bool IsValid => Vertices != null && Vertices.Count > 0 && Indices != null && Indices.Count > 0;

            public MeshData Clone()
            {
                return new MeshData
                {
                    Vertices = new List<SysVector3>(Vertices ?? new List<SysVector3>()),
                    Normals = new List<SysVector3>(Normals ?? new List<SysVector3>()),
                    Indices = new List<int>(Indices ?? new List<int>()),
                    Color = Color,
                    CavityId = CavityId
                };
            }

            public MeshData CloneWithShift(SysVector3 shift)
            {
                var cloned = new MeshData
                {
                    Vertices = new List<SysVector3>(Vertices?.Count ?? 0),
                    Normals = new List<SysVector3>(Normals ?? new List<SysVector3>()),
                    Indices = new List<int>(Indices ?? new List<int>()),
                    Color = Color,
                    CavityId = CavityId
                };

                if (Vertices != null)
                {
                    foreach (var v in Vertices)
                    {
                        cloned.Vertices.Add(v + shift);
                    }
                }

                return cloned;
            }

            public void Dispose()
            {
                Vertices?.Clear();
                Indices?.Clear();
                Normals?.Clear();
                Vertices = null;
                Indices = null;
                Normals = null;
            }
        }

 /// <summary>Result containing both raw and smoothed mesh.</summary>
        public class MeshResult
        {
            public MeshData RawMesh { get; set; }
            public MeshData SmoothedMesh { get; set; }
            public bool IsValid => RawMesh != null && RawMesh.IsValid;
        }

        #endregion

        #region Pre-Computed Tables

        private static readonly int[] _edgeFlags;
        private static readonly (int e0, int e1, int e2)[][] _trianglesByCase;
        private static readonly int[][] _activeEdgesByCase;
        private static readonly int[,] _edgeConnections;
        private static readonly (int v1, int v2)[] _edgeVertices;
        private static readonly int[] _edgeVertexCount;
        private static readonly CubeData[] _cubeData;

        private const float Epsilon = 0.00001f;
        private const int MAX_SAFE_VERTICES = 200_000_000;

        #endregion

        #region Private Logging Methods
 //
 // ⭐ REFACTOR: LogToDebug / LogProgress delegate to SafeDebugLogger.
 // - Worker threads (w tym Parallel.For): only ConcurrentQueue.Enqueue
 // - UI thread: Drain() once per second (Timer in MainForm)
 // - ZERO Application.OpenForms w hot path
 // - ZERO cross-thread UI calls z threads roboczych
 //

 /// <summary>
 /// Logs a message via SafeDebugLogger. Thread-safe, non-blocking.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogToDebug(string message, System.Drawing.Color? color = null)
        {
            SafeDebugLogger.LogTagged("[MC]", message, color);
        }

 /// <summary>
 /// Logs progress information with percentage.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LogProgress(string message, float percent, System.Drawing.Color? color = null)
        {
            SafeDebugLogger.LogTagged(
                "[MC]",
                $"[{percent:F0}%] {message}",
                color ?? System.Drawing.Color.LightBlue);
        }

        #endregion

        #region Safe UI Access
 //
 // ⭐ REFACTOR: SafeSetStatus / SafeSetStatusWithDetails delegate to
 // MainForm.Instance.AfterstStatus, which ZAWSZE marshalluje przez
 // BeginInvoke (fire-and-forget) — safe z threads roboczych.
 //

 /// <summary>
 /// Safe status update from any thread. Always marshalls to UI thread.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SafeSetStatus(string message, string icon = "")
        {
            MainForm.Instance?.PostStatus(message, "", icon);
        }

 /// <summary>
 /// Safe status update with details from any thread. Always marshalls to UI thread.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SafeSetStatusWithDetails(string message, string details, string icon = "")
        {
            MainForm.Instance?.PostStatus(message, details, icon);
        }

        #endregion

        #region Static Constructor

        static MarchingCubesMeshGenerator()
        {
            var edgeTable = MarchingCubesTables.EdgeTable;
            var triTable = MarchingCubesTables.TriTable;
            var edgeConn = MarchingCubesTables.EdgeConnections;

            _edgeFlags = new int[256];
            Array.Copy(edgeTable, _edgeFlags, 256);

            _trianglesByCase = new (int, int, int)[256][];
            _activeEdgesByCase = new int[256][];

            for (int caseIdx = 0; caseIdx < 256; caseIdx++)
            {
                var tris = new List<(int, int, int)>();
                var edges = new List<int>();

                for (int i = 0; i < 16; i += 3)
                {
                    int e0 = triTable[caseIdx, i];
                    if (e0 == -1) break;
                    int e1 = triTable[caseIdx, i + 1];
                    int e2 = triTable[caseIdx, i + 2];
                    tris.Add((e0, e1, e2));

                    if (!edges.Contains(e0)) edges.Add(e0);
                    if (!edges.Contains(e1)) edges.Add(e1);
                    if (!edges.Contains(e2)) edges.Add(e2);
                }

                _trianglesByCase[caseIdx] = tris.ToArray();
                _activeEdgesByCase[caseIdx] = edges.ToArray();
            }

            _edgeConnections = new int[12, 2];
            for (int i = 0; i < 12; i++)
            {
                _edgeConnections[i, 0] = edgeConn[i, 0];
                _edgeConnections[i, 1] = edgeConn[i, 1];
            }

            _edgeVertices = new (int v1, int v2)[12];
            for (int i = 0; i < 12; i++)
            {
                _edgeVertices[i] = (edgeConn[i, 0], edgeConn[i, 1]);
            }

            _edgeVertexCount = new int[256];
            for (int i = 0; i < 256; i++)
            {
                int count = 0;
                int flags = _edgeFlags[i];
                for (int e = 0; e < 12; e++)
                {
                    if ((flags & (1 << e)) != 0) count++;
                }
                _edgeVertexCount[i] = count;
            }

            _cubeData = new CubeData[256];
            for (int i = 0; i < 256; i++)
            {
                _cubeData[i] = new CubeData
                {
                    EdgeFlag = _edgeFlags[i],
                    ActiveEdges = _activeEdgesByCase[i],
                    Triangles = _trianglesByCase[i]
                };
            }

            LogToDebug("Pre-computed tables ready. Cases: 256", System.Drawing.Color.LightGreen);
        }

        #endregion

        #region Inline Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetEdgeFlag(int cubeIndex) => _edgeFlags[cubeIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (int e0, int e1, int e2)[] GetTriangles(int cubeIndex) => _trianglesByCase[cubeIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int[] GetActiveEdges(int cubeIndex) => _activeEdgesByCase[cubeIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref readonly CubeData GetCubeData(int cubeIndex) => ref _cubeData[cubeIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FastClamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        #endregion

        #region Main Generation Method

        public static void GenerateAllMeshesParallel(
            List<Cavity> cavities,
            CavityHelpers helpers,
            MeshParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            if (cavities == null || cavities.Count == 0)
            {
                LogToDebug("No cavities to process", System.Drawing.Color.Orange);
                helpers.ClearCavities();
                return;
            }

            if (parameters == null)
                parameters = new MeshParameters();

            LogToDebug($"========== GENERATE MESHES START ==========", System.Drawing.Color.Cyan);
            LogToDebug($"Cavities to process: {cavities.Count}", System.Drawing.Color.LightGray);
            LogProgress("Initializing mesh generation...", 0);

            helpers.SetCavities(cavities);
            helpers.UpdateCavityStatistics(cavities);

            int maxParallel = Math.Min(Environment.ProcessorCount, cavities.Count);
            LogToDebug($"Max parallelism: {maxParallel}", System.Drawing.Color.LightGray);

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallel,
                CancellationToken = cancellationToken
            };

            int completed = 0;
            int total = cavities.Count;
            int nextProgress = 0;
            int progressStep = Math.Max(1, total / 10);
            var lockObj = new object();

            using (var semaphore = new SemaphoreSlim(maxParallel))
            {
                Parallel.For(0, cavities.Count, options, (i, state) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    semaphore.Wait(cancellationToken);

                    try
                    {
                        var cavity = cavities[i];

                        if (cavity == null)
                        {
                            LogToDebug($"Cavity {i} is null - skipping", System.Drawing.Color.Orange);
                            return;
                        }

                        LogToDebug($"Processing cavity {cavity.Id} (Volume: {cavity.Volume:F2} Å³, Voxels: {cavity.Voxels?.Count ?? 0})", System.Drawing.Color.LightGray);

                        try
                        {
                            var (rawMesh, smoothedMesh) = GenerateMeshForSingleCavity(
                                cavity.DensityGrid,
                                cavity.Grid,
                                cavity.MinCorner,
                                cavity.InvFracToCart,
                                parameters,
                                cavity.Center,
                                cavity.VoxelMask,
                                cavity.Id,
                                false,
                                cancellationToken
                            );

                            if (rawMesh != null && rawMesh.IsValid)
                            {
                                cavity.RawVertices = rawMesh.Vertices;
                                cavity.RawIndices = rawMesh.Indices;
                                cavity.RawTrianglesGenerated = true;

                                float rawSurface = CalculateSurfaceAreaFromMesh(rawMesh);
                                cavity.SurfaceArea = rawSurface;

                                LogToDebug($"Cavity {cavity.Id}: Raw mesh = {rawMesh.TriangleCount} tris, Surface = {rawSurface:F2} Å²", System.Drawing.Color.LightGreen);
                            }
                            else
                            {
                                cavity.RawVertices = new List<SysVector3>();
                                cavity.RawIndices = new List<int>();
                                cavity.RawTrianglesGenerated = false;
                                cavity.SurfaceArea = 0f;
                                LogToDebug($"Cavity {cavity.Id}: Raw mesh EMPTY or INVALID", System.Drawing.Color.Orange);
                            }

                            if (smoothedMesh != null && smoothedMesh.IsValid)
                            {
                                cavity.Vertices = smoothedMesh.Vertices;
                                cavity.Indices = smoothedMesh.Indices;
                                cavity.TrianglesGenerated = true;
                                LogToDebug($"Cavity {cavity.Id}: Smoothed mesh = {smoothedMesh.TriangleCount} tris", System.Drawing.Color.LightGreen);
                            }
                            else
                            {
                                cavity.Vertices = new List<SysVector3>();
                                cavity.Indices = new List<int>();
                                cavity.TrianglesGenerated = false;
                                LogToDebug($"Cavity {cavity.Id}: Smoothed mesh EMPTY or INVALID", System.Drawing.Color.Orange);
                            }

                            if (cavity.Volume > 0 && cavity.SurfaceArea > 0)
                            {
                                cavity.Sphericity = CalculateSphericity(cavity.Volume, cavity.SurfaceArea);
                            }
                            else
                            {
                                cavity.Sphericity = 0f;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            LogToDebug($"ERROR generating cavity {cavity.Id}: {ex.Message}", System.Drawing.Color.Red);
                            cavity.TrianglesGenerated = false;
                            cavity.RawTrianglesGenerated = false;
                            cavity.Vertices = new List<SysVector3>();
                            cavity.Indices = new List<int>();
                            cavity.RawVertices = new List<SysVector3>();
                            cavity.RawIndices = new List<int>();
                            cavity.SurfaceArea = 0f;
                            cavity.Sphericity = 0f;
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }

                    int current = Interlocked.Increment(ref completed);
                    if (current - nextProgress >= progressStep)
                    {
                        lock (lockObj)
                        {
                            nextProgress = current;
                            int percent = Math.Min(100, current * 100 / total);

 // ⭐ SafeDebugLogger — without Application.OpenForms, queue 1 Hz
                            LogProgress($"MC progress: {current}/{total} cavities done", percent);

 // ⭐ AfterstStatus — ZAWSZE BeginInvoke (fire-and-forget)
                            MainForm.Instance?.PostStatus(
                                $"Generating mesh: {current}/{total} cavities", "", "⏳");
                            MainForm.Instance?.PostProgress(percent);

                            parameters.ProgressCallback?.Invoke(
                                $"MC: {current}/{total} cavities done", percent, 100);
                        }
                    }
                });
            }

            helpers.SetCavities(cavities);
            helpers.UpdateCavityStatistics(cavities);

            float totalVolume = helpers.GetTotalVoidsVolumeFromCavities(cavities);
            float totalSurface = helpers.GetTotalSurfaceAreaFromCavities(cavities);

            int totalTriangles = 0;
            int largeMeshes = 0;
            int cavitiesWithMesh = 0;
            foreach (var c in cavities)
            {
                if (c.TrianglesGenerated && c.Indices != null)
                {
                    int triCount = c.Indices.Count / 3;
                    totalTriangles += triCount;
                    cavitiesWithMesh++;
                    if (triCount > 1_000_000) largeMeshes++;
                }
            }

            LogToDebug($"========== GENERATE MESHES COMPLETE ==========", System.Drawing.Color.Cyan);
            LogToDebug($"Processed: {completed}/{total} cavities, {cavitiesWithMesh} with meshes", System.Drawing.Color.LightGray);
            LogToDebug($"Total volume: {totalVolume:F2} Å³, Total surface: {totalSurface:F2} Å²", System.Drawing.Color.LightGray);
            LogToDebug($"Total triangles: {totalTriangles:N0}, Large meshes (>1M): {largeMeshes}", System.Drawing.Color.LightGray);

            if (totalTriangles > 5_000_000)
            {
                LogToDebug($"WARNING: Total mesh has {totalTriangles:N0} triangles! Rendering may be slow.", System.Drawing.Color.Orange);
                SafeSetStatus($"WARNING: Total mesh has {totalTriangles:N0} triangles! Rendering may be slow.", "⚠️");
            }

            if (largeMeshes > 0)
            {
                LogToDebug($"WARNING: {largeMeshes} cavities have >1M triangles. Rendering may be slow.", System.Drawing.Color.Orange);
                SafeSetStatus($"WARNING: {largeMeshes} cavities have >1M triangles. Rendering may be slow.", "⚠️");
            }

            string msg = $"MC done: {completed}/{total} cavities, V={totalVolume:F1} Å³, {totalTriangles:N0} triangles";
            LogToDebug(msg, System.Drawing.Color.LightGreen);
            SafeSetStatus($"SUCCESS: {msg}", "✅");
            parameters.ProgressCallback?.Invoke($"MC done: {completed}/{total} cavities, V={totalVolume:F1} Å³", 100, 100);
        }

        #endregion

        #region Surface Area Calculation

        public static float CalculateSurfaceAreaFromMesh(MeshData mesh)
        {
            if (mesh == null || mesh.Indices == null || mesh.Vertices == null)
                return 0.0f;

            var vertices = mesh.Vertices;
            var indices = mesh.Indices;

            if (indices.Count < 3 || vertices.Count == 0)
                return 0.0f;

            float totalArea = 0.0f;

            for (int i = 0; i < indices.Count; i += 3)
            {
                int idx0 = indices[i];
                int idx1 = indices[i + 1];
                int idx2 = indices[i + 2];

                if (idx0 >= vertices.Count || idx1 >= vertices.Count || idx2 >= vertices.Count)
                    continue;

                SysVector3 v0 = vertices[idx0];
                SysVector3 v1 = vertices[idx1];
                SysVector3 v2 = vertices[idx2];

                SysVector3 e1 = v1 - v0;
                SysVector3 e2 = v2 - v0;
                SysVector3 cross = SysVector3.Cross(e1, e2);

                totalArea += cross.Length();
            }

            return totalArea * 0.5f;
        }

        private static float CalculateSphericity(float volume, float surfaceArea)
        {
            if (volume <= 0 || surfaceArea <= 0)
                return 0f;

            float volumeTerm = (float)(36.0 * Math.PI * volume * volume);
            float numerator = (float)Math.Pow(volumeTerm, 1.0 / 3.0);
            return numerator / surfaceArea;
        }

        #endregion

        #region Single Cavity Mesh Generation

        public static (MeshData rawMesh, MeshData smoothedMesh) GenerateMeshForSingleCavity(
             float[,,] density,
             VoxelScanner.VoxelGrid grid,
             SysVector3 minCorner,
             XnaMatrix invFracToCart,
             MeshParameters parameters = null,
             SysVector3? cavityCenter = null,
             bool[,,] cavityMask = null,
             int cavityId = -1,
             bool isMolecularSurface = false,
             CancellationToken cancellationToken = default)
        {
            if (parameters == null) parameters = new MeshParameters();
            if (grid == null)
            {
                LogToDebug($"Cavity {cavityId}: Grid is null - skipping", System.Drawing.Color.Orange);
                return (null, null);
            }

            if (density == null)
            {
                LogToDebug($"Cavity {cavityId}: Density grid is null - skipping", System.Drawing.Color.Orange);
                return (null, null);
            }

            cancellationToken.ThrowIfCancellationRequested();

            LogToDebug($"Cavity {cavityId}: Starting mesh generation...", System.Drawing.Color.LightGray);

            var sysInvFracToCart = new SysMatrix4x4(
                invFracToCart.M11, invFracToCart.M12, invFracToCart.M13, invFracToCart.M14,
                invFracToCart.M21, invFracToCart.M22, invFracToCart.M23, invFracToCart.M24,
                invFracToCart.M31, invFracToCart.M32, invFracToCart.M33, invFracToCart.M34,
                invFracToCart.M41, invFracToCart.M42, invFracToCart.M43, invFracToCart.M44
            );

            float[,,] densityCopy;
            if (cavityMask != null)
            {
                LogToDebug($"Cavity {cavityId}: Applying mask to density", System.Drawing.Color.LightGray);
                densityCopy = ApplyMaskToDensity(density, cavityMask);
            }
            else
            {
                densityCopy = CopyDensityGrid(density);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!isMolecularSurface)
            {
                LogToDebug($"Cavity {cavityId}: Setting boundary as occupied", System.Drawing.Color.LightGray);
                SetBoundaryAsOccupiedForMC(densityCopy, grid);
            }
            else
            {
                LogToDebug($"Cavity {cavityId}: Skipping boundary setting for molecular surface", System.Drawing.Color.LightGray);
            }

            float minVal = float.MaxValue, maxVal = float.MinValue;
            int nx = densityCopy.GetLength(0);
            int ny = densityCopy.GetLength(1);
            int nz = densityCopy.GetLength(2);
            long totalVoxels = (long)nx * ny * nz;

            LogToDebug($"Cavity {cavityId}: Grid size = {nx}x{ny}x{nz} ({totalVoxels:N0} voxels)", System.Drawing.Color.LightGray);

            for (int x = 0; x < nx; x++)
                for (int y = 0; y < ny; y++)
                    for (int z = 0; z < nz; z++)
                    {
                        float v = densityCopy[x, y, z];
                        if (v < minVal) minVal = v;
                        if (v > maxVal) maxVal = v;
                    }

            float range = maxVal - minVal;
            LogToDebug($"Cavity {cavityId}: SDF range = min={minVal:F3}, max={maxVal:F3}, range={range:F3}", System.Drawing.Color.LightGray);

            bool shouldNormalize = !isMolecularSurface;

            if (shouldNormalize)
            {
                LogToDebug($"Cavity {cavityId}: Normalizing SDF", System.Drawing.Color.LightGray);
                NormalizeSDF(densityCopy);
            }
            else
            {
                LogToDebug($"Cavity {cavityId}: Skipping SDF normalization for molecular surface", System.Drawing.Color.LightGray);
            }

            if (cavityCenter.HasValue)
            {
                LogToDebug($"Cavity {cavityId}: Ensuring consistent sign", System.Drawing.Color.LightGray);
                EnsureConsistentSign(densityCopy, cavityCenter.Value, grid);
            }

            MeshData rawMesh = null;
            var rawDensityCopy = CopyDensityGrid(densityCopy);

            LogToDebug($"Cavity {cavityId}: Generating raw mesh...", System.Drawing.Color.LightGray);
            var tempRawMesh = GenerateTriangles(rawDensityCopy, grid, parameters, cavityId, cancellationToken);
            if (tempRawMesh != null && tempRawMesh.IsValid)
            {
                if (!isMolecularSurface)
                {
                    ClipMeshToCell(tempRawMesh, minCorner, sysInvFracToCart);
                    LogToDebug($"Cavity {cavityId}: ClipMeshToCell applied to raw mesh", System.Drawing.Color.LightGray);
                }
                else
                {
                    LogToDebug($"Cavity {cavityId}: Skipping ClipMeshToCell for molecular surface", System.Drawing.Color.LightGray);
                }

                rawMesh = tempRawMesh.Clone();
                rawMesh.CavityId = cavityId;
                LogToDebug($"Cavity {cavityId}: Raw mesh = {rawMesh.Vertices.Count} verts, {rawMesh.Indices.Count} indices", System.Drawing.Color.LightGreen);
            }
            else
            {
                LogToDebug($"Cavity {cavityId}: Raw mesh generation FAILED!", System.Drawing.Color.Red);
                rawMesh = new MeshData { CavityId = cavityId };
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (parameters.GaussianBlurIterations > 0)
            {
                LogToDebug($"Cavity {cavityId}: Applying Gaussian blur ({parameters.GaussianBlurIterations} iterations)", System.Drawing.Color.LightGray);
                GaussianBlurDensity(densityCopy, parameters.GaussianBlurIterations, parameters.GaussianSigma);
            }

            if (parameters.GradientScale != 1.0f)
            {
                LogToDebug($"Cavity {cavityId}: Scaling gradient (scale={parameters.GradientScale:F2})", System.Drawing.Color.LightGray);
                ScaleGradient(densityCopy, parameters.GradientScale);
            }

            if (parameters.SmoothDensity && parameters.SmoothingIterations > 0)
            {
                LogToDebug($"Cavity {cavityId}: Smoothing density ({parameters.SmoothingIterations} iterations)", System.Drawing.Color.LightGray);
                SmoothDensityGrid(densityCopy, parameters.SmoothingIterations, parameters.SmoothingFactor);
            }

            cancellationToken.ThrowIfCancellationRequested();

            MeshData smoothedMesh = null;
            LogToDebug($"Cavity {cavityId}: Generating smoothed mesh...", System.Drawing.Color.LightGray);
            smoothedMesh = GenerateTriangles(densityCopy, grid, parameters, cavityId, cancellationToken);
            if (smoothedMesh != null && smoothedMesh.IsValid)
            {
                if (!isMolecularSurface)
                {
                    ClipMeshToCell(smoothedMesh, minCorner, sysInvFracToCart);
                    LogToDebug($"Cavity {cavityId}: ClipMeshToCell applied to smoothed mesh", System.Drawing.Color.LightGray);
                }
                else
                {
                    LogToDebug($"Cavity {cavityId}: Skipping ClipMeshToCell for smoothed molecular surface", System.Drawing.Color.LightGray);
                }

                SnapSharedVertices(smoothedMesh, parameters.SnapTolerance);

                if (parameters.GenerateNormals)
                {
                    LogToDebug($"Cavity {cavityId}: Generating normals", System.Drawing.Color.LightGray);
                    smoothedMesh.Normals = CalculateNormalsFromSDF(smoothedMesh, densityCopy, grid);
                }
                smoothedMesh.CavityId = cavityId;
                LogToDebug($"Cavity {cavityId}: Smoothed mesh = {smoothedMesh.Vertices.Count} verts, {smoothedMesh.Indices.Count} indices", System.Drawing.Color.LightGreen);
            }
            else
            {
                LogToDebug($"Cavity {cavityId}: Smoothed mesh generation FAILED!", System.Drawing.Color.Red);
                smoothedMesh = new MeshData { CavityId = cavityId };
            }

            LogToDebug($"Cavity {cavityId}: Mesh generation complete", System.Drawing.Color.LightGreen);
            return (rawMesh, smoothedMesh);
        }

        #endregion

        #region Triangle Generation

        private static MeshData GenerateTriangles(
            float[,,] density,
            VoxelScanner.VoxelGrid grid,
            MeshParameters parameters,
            int cavityId = -1,
            CancellationToken cancellationToken = default)
        {
            int nx = density.GetLength(0);
            int ny = density.GetLength(1);
            int nz = density.GetLength(2);

            long totalVoxels = (long)nx * ny * nz;
            long MAX_SAFE_VOXELS = Flags.MAX_MC_VOXELS;

            LogToDebug($"Cavity {cavityId}: GenerateTriangles - {nx}x{ny}x{nz} ({totalVoxels:N0} voxels)", System.Drawing.Color.LightGray);

            if (totalVoxels > MAX_SAFE_VOXELS)
            {
                string msg = $"Cavity {cavityId}: GRID TOO LARGE: {totalVoxels:N0} voxels (max: {MAX_SAFE_VOXELS:N0}). Please increase voxel size.";
                LogToDebug(msg, System.Drawing.Color.Red);
                SafeSetStatus($"ERROR: {msg}", "❌");
                return new MeshData { CavityId = cavityId };
            }

            if (totalVoxels > 15_000_000)
            {
                string msg = $"Cavity {cavityId}: Large grid: {totalVoxels:N0} voxels. This may take a while.";
                LogToDebug(msg, System.Drawing.Color.Orange);
                SafeSetStatus($"WARNING: {msg}", "⚠️");
            }

            int estimatedVerts = (nx - 1) * (ny - 1) * (nz - 1) * 3;

            if (estimatedVerts > 20_000_000)
            {
                string msg = $"Cavity {cavityId}: Estimated vertices: {estimatedVerts:N0}. May cause performance issues.";
                LogToDebug(msg, System.Drawing.Color.Orange);
                SafeSetStatus($"WARNING: {msg}", "⚠️");
            }

            SysVector3[] vertices = ArrayPool<SysVector3>.Shared.Rent(estimatedVerts);
            int[] indices = ArrayPool<int>.Shared.Rent(estimatedVerts);
            int vertCount = 0;
            int indexCount = 0;

            try
            {
                var pos = grid.Positions;
                float iso = parameters.IsoLevel;
                var edgeConn = _edgeConnections;

                float[] densityFlat = new float[nx * ny * nz];
                Buffer.BlockCopy(density, 0, densityFlat, 0, density.Length * sizeof(float));

                int voxelCount = (nx - 1) * (ny - 1) * (nz - 1);
                int[] voxelIndices = ArrayPool<int>.Shared.Rent(voxelCount);

                try
                {
                    int idx = 0;
                    for (int x = 0; x < nx - 1; x++)
                        for (int y = 0; y < ny - 1; y++)
                            for (int z = 0; z < nz - 1; z++)
                                voxelIndices[idx++] = (x * ny + y) * nz + z;

                    int maxParallel = OperatingSystem.IsAndroid() ? 3 : Math.Min(Environment.ProcessorCount, 8);
                    var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel };

                    LogToDebug($"Cavity {cavityId}: Processing {voxelCount} voxels with {maxParallel} threads", System.Drawing.Color.LightGray);

                    var results = new System.Collections.Concurrent.ConcurrentBag<(int count, int indexCount, SysVector3[] verts, int[] inds)>();
                    int processedVoxels = 0;
                    int progressStepVoxels = Math.Max(1, voxelCount / 10);

                    Parallel.For(0, voxelCount, options, vi =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        int baseIdx = voxelIndices[vi];
                        int x = baseIdx / (ny * nz);
                        int y = (baseIdx / nz) % ny;
                        int z = baseIdx % nz;

                        int x1 = x + 1, y1 = y + 1, z1 = z + 1;

                        int b000 = (x * ny + y) * nz + z;
                        int b100 = (x1 * ny + y) * nz + z;
                        int b010 = (x * ny + y1) * nz + z;
                        int b110 = (x1 * ny + y1) * nz + z;
                        int b001 = (x * ny + y) * nz + z1;
                        int b101 = (x1 * ny + y) * nz + z1;
                        int b011 = (x * ny + y1) * nz + z1;
                        int b111 = (x1 * ny + y1) * nz + z1;

                        float v0 = densityFlat[b000];
                        float v1 = densityFlat[b100];
                        float v2 = densityFlat[b110];
                        float v3 = densityFlat[b010];
                        float v4 = densityFlat[b001];
                        float v5 = densityFlat[b101];
                        float v6 = densityFlat[b111];
                        float v7 = densityFlat[b011];

                        int cubeIndex = 0;
                        if (v0 > iso) cubeIndex |= 1;
                        if (v1 > iso) cubeIndex |= 2;
                        if (v2 > iso) cubeIndex |= 4;
                        if (v3 > iso) cubeIndex |= 8;
                        if (v4 > iso) cubeIndex |= 16;
                        if (v5 > iso) cubeIndex |= 32;
                        if (v6 > iso) cubeIndex |= 64;
                        if (v7 > iso) cubeIndex |= 128;

                        if (cubeIndex == 0 || cubeIndex == 255) return;

                        SysVector3 p0 = grid.GetPosition(x, y, z);
                        SysVector3 p1 = grid.GetPosition(x1, y, z);
                        SysVector3 p2 = grid.GetPosition(x1, y1, z);
                        SysVector3 p3 = grid.GetPosition(x, y1, z);
                        SysVector3 p4 = grid.GetPosition(x, y, z1);
                        SysVector3 p5 = grid.GetPosition(x1, y, z1);
                        SysVector3 p6 = grid.GetPosition(x1, y1, z1);
                        SysVector3 p7 = grid.GetPosition(x, y1, z1);

                        Span<SysVector3> points = stackalloc SysVector3[8] { p0, p1, p2, p3, p4, p5, p6, p7 };

                        Span<float> edgeT = stackalloc float[12];
                        int edgeFlag = _edgeFlags[cubeIndex];

                        ref float e0 = ref edgeT[0];
                        ref float e1 = ref edgeT[1];
                        ref float e2 = ref edgeT[2];
                        ref float e3 = ref edgeT[3];
                        ref float e4 = ref edgeT[4];
                        ref float e5 = ref edgeT[5];
                        ref float e6 = ref edgeT[6];
                        ref float e7 = ref edgeT[7];
                        ref float e8 = ref edgeT[8];
                        ref float e9 = ref edgeT[9];
                        ref float e10 = ref edgeT[10];
                        ref float e11 = ref edgeT[11];

                        if ((edgeFlag & 1) != 0)
                        {
                            float diff = v1 - v0;
                            e0 = (diff < -Epsilon || diff > Epsilon) ? FastClamp((iso - v0) / diff, 0.0f, 1.0f) : 0.5f;
                        }
                        if ((edgeFlag & 2) != 0)
                        {
                            float diff = v2 - v1;
                            e1 = (diff < -Epsilon || diff > Epsilon) ? FastClamp((iso - v1) / diff, 0.0f, 1.0f) : 0.5f;
                        }
                        if ((edgeFlag & 4) != 0)
                        {
                            float diff = v3 - v2;
                            e2 = (diff < -Epsilon || diff > Epsilon) ? FastClamp((iso - v2) / diff, 0.0f, 1.0f) : 0.5f;
                        }
                        if ((edgeFlag & 8) != 0)
                        {
                            float diff = v0 - v3;
                            e3 = (diff < -Epsilon || diff > Epsilon) ? FastClamp((iso - v3) / diff, 0.0f, 1.0f) : 0.5f;
                        }
                        if ((edgeFlag & 16) != 0)
                        {
                            float diff = v5 - v4;
                            e4 = (diff < -Epsilon || diff > Epsilon) ? FastClamp((iso - v4) / diff, 0.0f, 1.0f) : 0.5f;
                        }
                        if ((edgeFlag & 32) != 0)
                        {
                            float diff = v6 - v5;
                            e5 = (diff < -Epsilon || diff > Epsilon) ? FastClamp((iso - v5) / diff, 0.0f, 1.0f) : 0.5f;
                        }
                        if ((edgeFlag & 64) != 0)
                        {
                            float diff = v7 - v6;
                            e6 = (diff < -Epsilon || diff > Epsilon) ? FastClamp((iso - v6) / diff, 0.0f, 1.0f) : 0.5f;
                        }
                        if ((edgeFlag & 128) != 0)
                        {
                            float diff = v4 - v7;
                            e7 = (diff < -Epsilon || diff > Epsilon) ? FastClamp((iso - v7) / diff, 0.0f, 1.0f) : 0.5f;
                        }
                        if ((edgeFlag & 256) != 0)
                        {
                            float diff = v4 - v0;
                            e8 = (diff < -Epsilon || diff > Epsilon) ? FastClamp((iso - v0) / diff, 0.0f, 1.0f) : 0.5f;
                        }
                        if ((edgeFlag & 512) != 0)
                        {
                            float diff = v5 - v1;
                            e9 = (diff < -Epsilon || diff > Epsilon) ? FastClamp((iso - v1) / diff, 0.0f, 1.0f) : 0.5f;
                        }
                        if ((edgeFlag & 1024) != 0)
                        {
                            float diff = v6 - v2;
                            e10 = (diff < -Epsilon || diff > Epsilon) ? FastClamp((iso - v2) / diff, 0.0f, 1.0f) : 0.5f;
                        }
                        if ((edgeFlag & 2048) != 0)
                        {
                            float diff = v7 - v3;
                            e11 = (diff < -Epsilon || diff > Epsilon) ? FastClamp((iso - v3) / diff, 0.0f, 1.0f) : 0.5f;
                        }

                        var triangles = _trianglesByCase[cubeIndex];
                        int localVertCount = 0;
                        int localIndexCount = 0;

                        SysVector3[] localVerts = ArrayPool<SysVector3>.Shared.Rent(triangles.Length * 3);
                        int[] localIndices = ArrayPool<int>.Shared.Rent(triangles.Length * 3);

                        try
                        {
                            for (int ti = 0; ti < triangles.Length; ti++)
                            {
                                var tri = triangles[ti];
                                int edgeIdx0 = tri.e0;
                                int edgeIdx1 = tri.e1;
                                int edgeIdx2 = tri.e2;

                                float t0 = edgeT[edgeIdx0];
                                float t1 = edgeT[edgeIdx1];
                                float t2 = edgeT[edgeIdx2];

                                int a0 = edgeConn[edgeIdx0, 0];
                                int b0 = edgeConn[edgeIdx0, 1];
                                int a1 = edgeConn[edgeIdx1, 0];
                                int b1 = edgeConn[edgeIdx1, 1];
                                int a2 = edgeConn[edgeIdx2, 0];
                                int b2 = edgeConn[edgeIdx2, 1];

                                SysVector3 pa0 = points[a0];
                                SysVector3 pb0 = points[b0];
                                SysVector3 pa1 = points[a1];
                                SysVector3 pb1 = points[b1];
                                SysVector3 pa2 = points[a2];
                                SysVector3 pb2 = points[b2];

                                localVerts[localVertCount] = pa0 + (pb0 - pa0) * t0;
                                localVerts[localVertCount + 1] = pa1 + (pb1 - pa1) * t1;
                                localVerts[localVertCount + 2] = pa2 + (pb2 - pa2) * t2;

                                localIndices[localIndexCount] = localVertCount;
                                localIndices[localIndexCount + 1] = localVertCount + 1;
                                localIndices[localIndexCount + 2] = localVertCount + 2;

                                localVertCount += 3;
                                localIndexCount += 3;
                            }

                            if (localVertCount > 0)
                            {
                                lock (vertices)
                                {
                                    if (vertCount + localVertCount > vertices.Length)
                                    {
                                        EnsureCapacity(ref vertices, ref indices, vertCount, indexCount, localVertCount, cavityId);
                                    }

                                    Array.Copy(localVerts, 0, vertices, vertCount, localVertCount);
                                    for (int i = 0; i < localIndexCount; i++)
                                    {
                                        indices[indexCount + i] = localIndices[i] + vertCount;
                                    }

                                    vertCount += localVertCount;
                                    indexCount += localIndexCount;
                                }
                            }
                        }
                        finally
                        {
                            ArrayPool<SysVector3>.Shared.Return(localVerts);
                            ArrayPool<int>.Shared.Return(localIndices);
                        }

                        int processed = Interlocked.Increment(ref processedVoxels);
                        if (processed % progressStepVoxels == 0)
                        {
                            float percent = 20 + 60.0f * processed / voxelCount;

 // ⭐ SafeDebugLogger — queue 1 Hz, without Application.OpenForms
                            LogProgress($"Cavity {cavityId}: MC processing {processed}/{voxelCount} voxels", percent);
                        }
                    });

                    LogToDebug($"Cavity {cavityId}: Voxel processing complete, generated {vertCount} vertices", System.Drawing.Color.LightGray);
                }
                finally
                {
                    ArrayPool<int>.Shared.Return(voxelIndices);
                }

                var mesh = new MeshData
                {
                    Vertices = new List<SysVector3>(vertCount),
                    Indices = new List<int>(indexCount),
                    CavityId = cavityId
                };

                for (int i = 0; i < vertCount; i++)
                    mesh.Vertices.Add(vertices[i]);

                for (int i = 0; i < indexCount; i++)
                    mesh.Indices.Add(indices[i]);

                if (mesh.Vertices.Count > 0)
                {
                    LogToDebug($"Cavity {cavityId}: Merging {mesh.Vertices.Count} vertices...", System.Drawing.Color.LightGray);
                    MergeVerticesFast(mesh);
                    LogToDebug($"Cavity {cavityId}: Merged to {mesh.Vertices.Count} unique vertices", System.Drawing.Color.LightGray);
                }

                if (mesh.TriangleCount > 1_000_000)
                {
                    string msg = $"Cavity {cavityId}: Large mesh with {mesh.TriangleCount:N0} triangles!";
                    LogToDebug(msg, System.Drawing.Color.Orange);
                    SafeSetStatus($"WARNING: {msg}", "⚠️");
                }

                LogToDebug($"Cavity {cavityId}: Triangle generation complete - {mesh.TriangleCount} triangles", System.Drawing.Color.LightGreen);
                return mesh;
            }
            finally
            {
                ArrayPool<SysVector3>.Shared.Return(vertices);
                ArrayPool<int>.Shared.Return(indices);
            }
        }

        #endregion

        #region Capacity Management

        private static void EnsureCapacity(ref SysVector3[] vertices, ref int[] indices,
                                           int vertCount, int indexCount, int additionalVerts,
                                           int cavityId = -1)
        {
            int requiredVerts = vertCount + additionalVerts;
            int requiredIndices = indexCount + additionalVerts;

            if (requiredVerts > vertices.Length)
            {
                int oldSize = vertices.Length;
                int newSize = Math.Max(vertices.Length * 2, requiredVerts + 1000);

                if (newSize > 10_000_000)
                {
                    LogToDebug($"Cavity {cavityId}: Very large mesh detected! Vertices growing to {newSize:N0}.", System.Drawing.Color.Orange);
                    SafeSetStatus($"WARNING: Very large mesh detected!", "⚠️");
                }

                if (newSize > MAX_SAFE_VERTICES)
                {
                    string msg = $"Cavity {cavityId}: Mesh too large ({newSize:N0} vertices)! Consider increasing voxel size.";
                    LogToDebug(msg, System.Drawing.Color.Red);
                    SafeSetStatus($"ERROR: {msg}", "❌");
                    throw new InvalidOperationException(msg);
                }

                var newVerts = ArrayPool<SysVector3>.Shared.Rent(newSize);
                Array.Copy(vertices, newVerts, vertCount);
                ArrayPool<SysVector3>.Shared.Return(vertices);
                vertices = newVerts;

                if (requiredIndices > indices.Length)
                {
                    var newIndices = ArrayPool<int>.Shared.Rent(newSize);
                    Array.Copy(indices, newIndices, indexCount);
                    ArrayPool<int>.Shared.Return(indices);
                    indices = newIndices;
                }

                LogToDebug($"Cavity {cavityId}: Resized arrays: {oldSize} -> {newSize}", System.Drawing.Color.LightGray);
            }
            else if (requiredIndices > indices.Length)
            {
                int newSize = Math.Max(indices.Length * 2, requiredIndices + 1000);
                var newIndices = ArrayPool<int>.Shared.Rent(newSize);
                Array.Copy(indices, newIndices, indexCount);
                ArrayPool<int>.Shared.Return(indices);
                indices = newIndices;
            }
        }

        #endregion

        #region Vertex Merging

        private static void MergeVerticesFast(MeshData mesh)
        {
            if (mesh.Vertices.Count < 3) return;

            int vertCount = mesh.Vertices.Count;

            if (vertCount < 1000)
            {
                MergeVerticesSimple(mesh);
                return;
            }

            var verts = mesh.Vertices;

            const float tolerance = 0.0001f;
            const float scale = 1f / tolerance;

            var map = new Dictionary<long, int>(vertCount / 2);

            var uniqueVerts = new List<SysVector3>(vertCount / 2);
            var indexMap = new int[vertCount];

            for (int i = 0; i < vertCount; i++)
            {
                SysVector3 pos = verts[i];

                long key = ((long)(pos.X * scale) << 42) ^
                           ((long)(pos.Y * scale) << 21) ^
                           (long)(pos.Z * scale);

                if (map.TryGetValue(key, out int existing))
                {
                    indexMap[i] = existing;
                }
                else
                {
                    int newIdx = uniqueVerts.Count;
                    map[key] = newIdx;
                    indexMap[i] = newIdx;
                    uniqueVerts.Add(pos);
                }
            }

            var indices = mesh.Indices;
            for (int i = 0; i < indices.Count; i++)
            {
                indices[i] = indexMap[indices[i]];
            }

            mesh.Vertices = uniqueVerts;

            if (uniqueVerts.Count > 1_000_000)
            {
                LogToDebug($"Cavity {mesh.CavityId}: {uniqueVerts.Count:N0} unique vertices - large mesh!", System.Drawing.Color.Orange);
                SafeSetStatus($"WARNING: Large mesh with {uniqueVerts.Count:N0} unique vertices!", "⚠️");
            }
        }

        private static void MergeVerticesSimple(MeshData mesh)
        {
            var verts = mesh.Vertices;
            var uniqueVerts = new List<SysVector3>(verts.Count / 2);
            var indexMap = new int[verts.Count];
            float tolerance = 0.0001f;

            for (int i = 0; i < verts.Count; i++)
            {
                SysVector3 pos = verts[i];
                int existing = -1;

                for (int j = 0; j < uniqueVerts.Count; j++)
                {
                    float dx = uniqueVerts[j].X - pos.X;
                    float dy = uniqueVerts[j].Y - pos.Y;
                    float dz = uniqueVerts[j].Z - pos.Z;
                    if ((dx > -tolerance && dx < tolerance) &&
                        (dy > -tolerance && dy < tolerance) &&
                        (dz > -tolerance && dz < tolerance))
                    {
                        existing = j;
                        break;
                    }
                }

                if (existing >= 0)
                    indexMap[i] = existing;
                else
                {
                    indexMap[i] = uniqueVerts.Count;
                    uniqueVerts.Add(pos);
                }
            }

            var indices = mesh.Indices;
            for (int i = 0; i < indices.Count; i++)
            {
                indices[i] = indexMap[indices[i]];
            }

            mesh.Vertices = uniqueVerts;
        }

        #endregion

        #region Helper Methods

        private static void SetBoundaryAsOccupiedForMC(float[,,] density, VoxelScanner.VoxelGrid grid)
        {
            int nx = density.GetLength(0);
            int ny = density.GetLength(1);
            int nz = density.GetLength(2);
            int wallThickness = 4;

            for (int x = 0; x < nx; x++)
            {
                for (int y = 0; y < ny; y++)
                {
                    for (int z = 0; z < nz; z++)
                    {
                        bool onBoundary = (x < wallThickness || x >= nx - wallThickness ||
                                           y < wallThickness || y >= ny - wallThickness ||
                                           z < wallThickness || z >= nz - wallThickness);

                        if (onBoundary)
                        {
                            density[x, y, z] = -1.0f;
                        }
                    }
                }
            }
        }

        private static float[,,] ApplyMaskToDensity(float[,,] density, bool[,,] mask)
        {
            int nx = density.GetLength(0);
            int ny = density.GetLength(1);
            int nz = density.GetLength(2);

            float[,,] masked = new float[nx, ny, nz];

            for (int x = 0; x < nx; x++)
            {
                for (int y = 0; y < ny; y++)
                {
                    for (int z = 0; z < nz; z++)
                    {
                        masked[x, y, z] = mask[x, y, z] ? density[x, y, z] : -10.0f;
                    }
                }
            }

            return masked;
        }

        private static float[,,] CopyDensityGrid(float[,,] density)
        {
            if (density == null) return null;

            int nx = density.GetLength(0);
            int ny = density.GetLength(1);
            int nz = density.GetLength(2);

            if (nx == 0 || ny == 0 || nz == 0)
                return new float[0, 0, 0];

            float[,,] copy = new float[nx, ny, nz];
            Buffer.BlockCopy(density, 0, copy, 0, density.Length * sizeof(float));
            return copy;
        }

        private static void NormalizeSDF(float[,,] density)
        {
            int nx = density.GetLength(0);
            int ny = density.GetLength(1);
            int nz = density.GetLength(2);

            float min = float.MaxValue, max = float.MinValue;

            for (int x = 0; x < nx; x++)
            {
                for (int y = 0; y < ny; y++)
                {
                    for (int z = 0; z < nz; z++)
                    {
                        float v = density[x, y, z];
                        if (v < min) min = v;
                        if (v > max) max = v;
                    }
                }
            }

            float range = max - min;
            if (range < 0.0001f) return;

            for (int x = 0; x < nx; x++)
            {
                for (int y = 0; y < ny; y++)
                {
                    for (int z = 0; z < nz; z++)
                    {
                        bool onBoundary = x == 0 || x == nx - 1 || y == 0 || y == ny - 1 || z == 0 || z == nz - 1;
                        if (!onBoundary)
                            density[x, y, z] = 2.0f * (density[x, y, z] - min) / range - 1.0f;
                    }
                }
            }
        }

        private static void EnsureConsistentSign(float[,,] density, SysVector3 cavityCenter, VoxelScanner.VoxelGrid grid)
        {
            int nx = density.GetLength(0);
            int ny = density.GetLength(1);
            int nz = density.GetLength(2);

            float stepX = CellParameters.StepX;
            float stepY = CellParameters.StepY;
            float stepZ = CellParameters.StepZ;

            int cx = Math.Clamp((int)((cavityCenter.X - grid.BoundsMin.X) / stepX), 0, nx - 1);
            int cy = Math.Clamp((int)((cavityCenter.Y - grid.BoundsMin.Y) / stepY), 0, ny - 1);
            int cz = Math.Clamp((int)((cavityCenter.Z - grid.BoundsMin.Z) / stepZ), 0, nz - 1);

            if (density[cx, cy, cz] > 0)
            {
                for (int x = 0; x < nx; x++)
                    for (int y = 0; y < ny; y++)
                        for (int z = 0; z < nz; z++)
                            density[x, y, z] = -density[x, y, z];
            }
        }

        #endregion

        #region Gaussian Blur

        private static void GaussianBlurDensity(float[,,] density, int iterations, float sigma = 1.0f)
        {
            if (iterations <= 0) return;

            int nx = density.GetLength(0);
            int ny = density.GetLength(1);
            int nz = density.GetLength(2);

            float[] kernel = ComputeGaussianKernel(sigma);
            int radius = kernel.Length / 2;

            float[,,] temp1 = new float[nx, ny, nz];
            float[,,] temp2 = new float[nx, ny, nz];

            float[,,] source = density;
            float[,,] target = temp1;

            for (int iter = 0; iter < iterations; iter++)
            {
                BlurX_Scalar(source, target, kernel, radius, nx, ny, nz);
                BlurY_Scalar(target, source, kernel, radius, nx, ny, nz);
                BlurZ_Scalar(source, target, kernel, radius, nx, ny, nz);

                var temp = source;
                source = target;
                target = temp;
            }

            if (source != density)
            {
                Buffer.BlockCopy(source, 0, density, 0, source.Length * sizeof(float));
            }
        }

        private static void BlurX_Scalar(float[,,] source, float[,,] target, float[] kernel, int radius, int nx, int ny, int nz)
        {
            Parallel.For(0, nx, i =>
            {
                for (int y = 0; y < ny; y++)
                {
                    for (int z = 0; z < nz; z++)
                    {
                        float sum = 0f;
                        int start = i - radius;
                        for (int k = start; k <= start + 2 * radius; k++)
                        {
                            int x = k;
                            if (x < 0) x = -x;
                            else if (x >= nx) x = 2 * nx - x - 1;
                            sum += source[x, y, z] * kernel[k - start];
                        }
                        target[i, y, z] = sum;
                    }
                }
            });
        }

        private static void BlurY_Scalar(float[,,] source, float[,,] target, float[] kernel, int radius, int nx, int ny, int nz)
        {
            Parallel.For(0, ny, j =>
            {
                for (int x = 0; x < nx; x++)
                {
                    for (int z = 0; z < nz; z++)
                    {
                        float sum = 0f;
                        int start = j - radius;
                        for (int k = start; k <= start + 2 * radius; k++)
                        {
                            int y = k;
                            if (y < 0) y = -y;
                            else if (y >= ny) y = 2 * ny - y - 1;
                            sum += source[x, y, z] * kernel[k - start];
                        }
                        target[x, j, z] = sum;
                    }
                }
            });
        }

        private static void BlurZ_Scalar(float[,,] source, float[,,] target, float[] kernel, int radius, int nx, int ny, int nz)
        {
            Parallel.For(0, nz, k =>
            {
                for (int x = 0; x < nx; x++)
                {
                    for (int y = 0; y < ny; y++)
                    {
                        float sum = 0f;
                        int start = k - radius;
                        for (int i = start; i <= start + 2 * radius; i++)
                        {
                            int z = i;
                            if (z < 0) z = -z;
                            else if (z >= nz) z = 2 * nz - z - 1;
                            sum += source[x, y, z] * kernel[i - start];
                        }
                        target[x, y, k] = sum;
                    }
                }
            });
        }

        private static float[] ComputeGaussianKernel(float sigma)
        {
            int radius = Math.Max(1, (int)(sigma * 2.5f));
            int size = radius * 2 + 1;
            float[] kernel = new float[size];
            float sum = 0f;
            float twoSigmaSq = 2f * sigma * sigma;
            float factor = 1f / (MathF.Sqrt(2f * MathF.PI) * sigma);

            for (int i = -radius; i <= radius; i++)
            {
                float value = factor * MathF.Exp(-(i * i) / twoSigmaSq);
                kernel[i + radius] = value;
                sum += value;
            }

            float invSum = 1f / sum;
            for (int i = 0; i < size; i++)
                kernel[i] *= invSum;

            return kernel;
        }

        #endregion

        #region Gradient Scaling and Smoothing

        private static void ScaleGradient(float[,,] density, float scale)
        {
            int nx = density.GetLength(0);
            int ny = density.GetLength(1);
            int nz = density.GetLength(2);

            float sum = 0;
            for (int x = 0; x < nx; x++)
                for (int y = 0; y < ny; y++)
                    for (int z = 0; z < nz; z++)
                        sum += density[x, y, z];

            float mean = sum / (nx * ny * nz);

            for (int x = 0; x < nx; x++)
                for (int y = 0; y < ny; y++)
                    for (int z = 0; z < nz; z++)
                        density[x, y, z] = mean + (density[x, y, z] - mean) * scale;
        }

        private static void SmoothDensityGrid(float[,,] density, int iterations, float factor)
        {
            int nx = density.GetLength(0);
            int ny = density.GetLength(1);
            int nz = density.GetLength(2);

            float[,,] temp = new float[nx, ny, nz];
            float[,,] current = density;
            float[,,] next = temp;

            int maxX = nx, maxY = ny, maxZ = nz;
            int maxParallel = Math.Min(Environment.ProcessorCount, maxX);
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel };

            for (int iter = 0; iter < iterations; iter++)
            {
                float[,,] curr = current;
                float[,,] nxt = next;
                float f = factor;

                Parallel.For(1, maxX - 1, options, x =>
                {
                    float[,,] c = curr;
                    float[,,] n = nxt;
                    int maxYLocal = maxY;
                    int maxZLocal = maxZ;
                    float fLocal = f;

                    for (int y = 1; y < maxYLocal - 1; y++)
                    {
                        for (int z = 1; z < maxZLocal - 1; z++)
                        {
                            float center = c[x, y, z];
                            float sum = center
                                + c[x - 1, y, z]
                                + c[x + 1, y, z]
                                + c[x, y - 1, z]
                                + c[x, y + 1, z]
                                + c[x, y, z - 1]
                                + c[x, y, z + 1];

                            n[x, y, z] = center + fLocal * ((sum / 7.0f) - center);
                        }
                    }
                });

                var swap = current;
                current = next;
                next = swap;
            }

            if (current != density)
            {
                for (int x = 0; x < maxX; x++)
                    for (int y = 0; y < maxY; y++)
                        for (int z = 0; z < maxZ; z++)
                            density[x, y, z] = current[x, y, z];
            }
        }

        #endregion

        #region Clipping and Snapping

        private static void ClipMeshToCell(MeshData mesh, SysVector3 minCorner, SysMatrix4x4 sysInvFracToCart)
        {
            if (mesh.Vertices == null || mesh.Indices == null || mesh.Indices.Count == 0) return;

            const float overlap = 0.001f;
            int triangleCount = mesh.Indices.Count / 3;
            if (triangleCount == 0) return;

            var verts = mesh.Vertices;
            var indices = mesh.Indices;

            var newIndices = new List<int>(indices.Count);

            for (int i = 0; i < triangleCount; i++)
            {
                int idx = i * 3;
                int i0 = indices[idx];
                int i1 = indices[idx + 1];
                int i2 = indices[idx + 2];

                if (i0 >= verts.Count || i1 >= verts.Count || i2 >= verts.Count)
                    continue;

                var v0 = verts[i0] - minCorner;
                var v1 = verts[i1] - minCorner;
                var v2 = verts[i2] - minCorner;

                var frac4_0 = SysVector4.Transform(new SysVector4(v0, 1), sysInvFracToCart);
                var frac4_1 = SysVector4.Transform(new SysVector4(v1, 1), sysInvFracToCart);
                var frac4_2 = SysVector4.Transform(new SysVector4(v2, 1), sysInvFracToCart);

                bool inside0 = frac4_0.X >= -overlap && frac4_0.X <= 1.0f + overlap &&
                               frac4_0.Y >= -overlap && frac4_0.Y <= 1.0f + overlap &&
                               frac4_0.Z >= -overlap && frac4_0.Z <= 1.0f + overlap;
                bool inside1 = frac4_1.X >= -overlap && frac4_1.X <= 1.0f + overlap &&
                               frac4_1.Y >= -overlap && frac4_1.Y <= 1.0f + overlap &&
                               frac4_1.Z >= -overlap && frac4_1.Z <= 1.0f + overlap;
                bool inside2 = frac4_2.X >= -overlap && frac4_2.X <= 1.0f + overlap &&
                               frac4_2.Y >= -overlap && frac4_2.Y <= 1.0f + overlap &&
                               frac4_2.Z >= -overlap && frac4_2.Z <= 1.0f + overlap;

                if (inside0 && inside1 && inside2)
                {
                    newIndices.Add(i0);
                    newIndices.Add(i1);
                    newIndices.Add(i2);
                }
            }

            mesh.Indices = newIndices;
        }

        private static void SnapSharedVertices(MeshData mesh, float tolerance)
        {
            var comparer = new SysVector3Comparer();
            var map = new Dictionary<SysVector3, int>(comparer);

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                SysVector3 pos = mesh.Vertices[i];
                if (map.TryGetValue(pos, out int existing))
                    mesh.Vertices[i] = mesh.Vertices[existing];
                else
                    map[pos] = i;
            }
        }

        #endregion

        #region Normal Calculation

        private static List<SysVector3> CalculateNormalsFromSDF(MeshData mesh, float[,,] density, VoxelScanner.VoxelGrid grid)
        {
            if (mesh.Vertices == null || mesh.Vertices.Count == 0)
                return new List<SysVector3>();

            int nx = density.GetLength(0);
            int ny = density.GetLength(1);
            int nz = density.GetLength(2);

            int vertexCount = mesh.Vertices.Count;
            var normals = new SysVector3[vertexCount];
            var vertices = mesh.Vertices;

            int maxParallel = Math.Min(Environment.ProcessorCount, vertexCount);
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel };

            Parallel.For(0, vertexCount, options, i =>
            {
                SysVector3 pos = vertices[i];
                SysVector3 gradient = ComputeGradient(density, pos, grid, nx, ny, nz);
                float len = gradient.Length();

                if (len <= 1e-10f)
                {
                    normals[i] = new SysVector3(0, 1, 0);
                }
                else
                {
                    normals[i] = -gradient / len;
                }
            });

            return new List<SysVector3>(normals);
        }

        private static SysVector3 ComputeGradient(float[,,] density, SysVector3 position, VoxelScanner.VoxelGrid grid, int nx, int ny, int nz)
        {
            float stepX = CellParameters.StepX;
            float stepY = CellParameters.StepY;
            float stepZ = CellParameters.StepZ;

            float fx = (position.X - grid.BoundsMin.X) / stepX;
            float fy = (position.Y - grid.BoundsMin.Y) / stepY;
            float fz = (position.Z - grid.BoundsMin.Z) / stepZ;

            int x0 = Math.Clamp((int)MathF.Floor(fx), 0, nx - 2);
            int y0 = Math.Clamp((int)MathF.Floor(fy), 0, ny - 2);
            int z0 = Math.Clamp((int)MathF.Floor(fz), 0, nz - 2);

            float v000 = GetSafe(density, x0, y0, z0, nx, ny, nz);
            float v100 = GetSafe(density, x0 + 1, y0, z0, nx, ny, nz);
            float v010 = GetSafe(density, x0, y0 + 1, z0, nx, ny, nz);
            float v110 = GetSafe(density, x0 + 1, y0 + 1, z0, nx, ny, nz);
            float v001 = GetSafe(density, x0, y0, z0 + 1, nx, ny, nz);
            float v101 = GetSafe(density, x0 + 1, y0, z0 + 1, nx, ny, nz);
            float v011 = GetSafe(density, x0, y0 + 1, z0 + 1, nx, ny, nz);
            float v111 = GetSafe(density, x0 + 1, y0 + 1, z0 + 1, nx, ny, nz);

            float dx = ((v100 - v000) + (v110 - v010) + (v101 - v001) + (v111 - v011)) / (4.0f * stepX);
            float dy = ((v010 - v000) + (v110 - v100) + (v011 - v001) + (v111 - v101)) / (4.0f * stepY);
            float dz = ((v001 - v000) + (v101 - v100) + (v011 - v010) + (v111 - v110)) / (4.0f * stepZ);

            return new SysVector3(dx, dy, dz);
        }

        private static float GetSafe(float[,,] density, int x, int y, int z, int nx, int ny, int nz)
        {
            x = Math.Clamp(x, 0, nx - 1);
            y = Math.Clamp(y, 0, ny - 1);
            z = Math.Clamp(z, 0, nz - 1);
            return density[x, y, z];
        }

        #endregion

        #region Mesh Parameters

        public static MeshParameters GetMeshParametersForResolution(int resolution)
        {
            return CavityDetectorBase.GetMeshParametersForSmoothingLevel(resolution);
        }

        #endregion
    }
}