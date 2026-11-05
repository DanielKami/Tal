// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldMarchingCubesGenerator.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// KOPIA MarchingCubesMeshGenerator — dedykowana for Hirshfelda.
// Vertex positions are taken from grid.GetPosition(x, y, z), same as in cavities.
//
// ⭐ v2.1 (STABILNA — without per-thread list, without precompute):
// - Sygnatura Extract przyjmuje float[] density (flat) + nx, ny, nz + isoLevel.
// - Hot loop czyta density[i + nx*(j + ny*k)] (1 pointer dereference).
// - isoLevel odejmowane inline (without alokacji mcInput).
// - stackalloc afterints/edgeT/localVerts/localIndices RAZ na thread.
// - lock (vertices) — wolne, ale POPRAWNE.
// - GetPosition per voxel — without precompute.
// - Debug.Assert na triangles.Length.
// - Span.CopyTo zamiast Array.Copy.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Tal.Cavity;
using SysVector3 = System.Numerics.Vector3;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Marching Cubes for surface Hirshfelda (flat arrays, stabilna).
 /// Kopia MarchingCubesMeshGenerator.GenerateTriangles z tabelami MarchingCubesTables.
 /// Does not modify the input density array.
 /// </summary>
    public static class HirshfeldMarchingCubesGenerator
    {
        #region Constants

        private const float Epsilon = 0.00001f;
        private const int MAX_SAFE_VERTICES = 500_000_000;

        private const int MAX_TRIS_PER_CUBE = 5;
        private const int MAX_VERTS_PER_CUBE = MAX_TRIS_PER_CUBE * 3;

        #endregion

        #region Pre-computed tables

        private static readonly int[] _edgeFlags;
        private static readonly (int e0, int e1, int e2)[][] _trianglesByCase;
        private static readonly int[,] _edgeConnections;

        static HirshfeldMarchingCubesGenerator()
        {
            var edgeTable = MarchingCubesTables.EdgeTable;
            var triTable = MarchingCubesTables.TriTable;
            var edgeConn = MarchingCubesTables.EdgeConnections;

            _edgeFlags = new int[256];
            Array.Copy(edgeTable, _edgeFlags, 256);

            _trianglesByCase = new (int, int, int)[256][];
            for (int caseIdx = 0; caseIdx < 256; caseIdx++)
            {
                var tris = new List<(int, int, int)>();
                for (int i = 0; i < 16; i += 3)
                {
                    int e0 = triTable[caseIdx, i];
                    if (e0 == -1) break;
                    int e1 = triTable[caseIdx, i + 1];
                    int e2 = triTable[caseIdx, i + 2];
                    tris.Add((e0, e1, e2));
                }
                _trianglesByCase[caseIdx] = tris.ToArray();

                if (_trianglesByCase[caseIdx].Length > MAX_TRIS_PER_CUBE)
                {
                    throw new InvalidOperationException(
                        $"MarchingCubesTables.TriTable[{caseIdx}] has " +
                        $"{_trianglesByCase[caseIdx].Length} triangles " +
                        $"(max {MAX_TRIS_PER_CUBE}).");
                }
            }

            _edgeConnections = new int[12, 2];
            for (int i = 0; i < 12; i++)
            {
                _edgeConnections[i, 0] = edgeConn[i, 0];
                _edgeConnections[i, 1] = edgeConn[i, 1];
            }
        }

        #endregion

        #region Inline helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float FastClamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        #endregion

        #region Public API

        public class HirshfeldMesh
        {
            public List<SysVector3> Vertices = new List<SysVector3>();
            public List<int> Indices = new List<int>();
            public List<SysVector3> Normals = new List<SysVector3>();

            public int VertexCount => Vertices.Count;
            public int TriangleCount => Indices.Count / 3;
            public bool IsValid => Vertices.Count > 0 && Indices.Count > 0;
        }

        public class Options
        {
            public float IsoLevel = 0.0f;
            public bool GenerateNormals = true;
            public bool FixWinding = false;
            public float SnapTolerance = 0.0001f;
            public int ProgressPercentStart = 82;
            public int ProgressPercentEnd = 92;
            public Action<string, float> ProgressCallback;
        }

 /// <summary>
 /// ⭐ v2.1 STABILNA: flat arrays + inline isoLevel + lock(vertices).
 ///
 /// Inside: reads density[i + nx*(j + ny*k)] and compares with isoLevel.
 /// "inside" = density[idx] > isoLevel.
 /// </summary>
        public static HirshfeldMesh Extract(
            float[] density,
            int nx, int ny, int nz,
            VoxelScanner.VoxelGrid grid,
            Options options = null,
            System.Threading.CancellationToken ct = default)
        {
            if (options == null) options = new Options();

            if (density == null || grid == null)
                return new HirshfeldMesh();

            if (nx < 2 || ny < 2 || nz < 2)
                return new HirshfeldMesh();

            long expectedLen = (long)nx * ny * nz;
            if (density.Length < expectedLen)
            {
                options.ProgressCallback?.Invoke(
                    $"MC: density.Length={density.Length} < {expectedLen}", 1.0f);
                return new HirshfeldMesh();
            }

            long totalVoxels = (long)nx * ny * nz;
            if (totalVoxels > Flags.MAX_MC_VOXELS)
            {
                options.ProgressCallback?.Invoke(
                    $"MC grid too large: {totalVoxels:N0} (max: {Flags.MAX_MC_VOXELS:N0})",
                    1.0f);
                return new HirshfeldMesh();
            }

            int estimatedVerts = Math.Max(64, ((nx - 1) * (ny - 1) * (nz - 1)) / 3);
            if (estimatedVerts > MAX_SAFE_VERTICES)
            {
                options.ProgressCallback?.Invoke(
                    $"Too many estimated vertices: {estimatedVerts:N0}", 1.0f);
                return new HirshfeldMesh();
            }

            SysVector3[] vertices = ArrayPool<SysVector3>.Shared.Rent(Math.Max(estimatedVerts, 64));
            int[] indices = ArrayPool<int>.Shared.Rent(Math.Max(estimatedVerts, 64));
            int vertCount = 0;
            int indexCount = 0;

            try
            {
                float iso = options.IsoLevel;

 // ⭐ Stride'y for flat indexing: idx = i + nx*(j + ny*k)
                int strideY = nx;
                int strideZ = nx * ny;

                int voxelCount = (nx - 1) * (ny - 1) * (nz - 1);

                int maxParallel = OperatingSystem.IsAndroid()
                    ? 3
                    : Math.Min(Environment.ProcessorCount, 8);

                var parallelOptions = new System.Threading.Tasks.ParallelOptions
                {
                    MaxDegreeOfParallelism = maxParallel,
                    CancellationToken = ct
                };

                int processedVoxels = 0;
                int progressStepVoxels = Math.Max(1, voxelCount / 20);

                var optionsLocal = options;
                var gridLocal = grid;

 // ⭐ Hoist do lokalnych — unikamy property access w hot loop
                int nxL = nx;
                int nyL = ny;
                int nzL = nz;
                int strideYL = strideY;
                int strideZL = strideZ;
                float isoL = iso;

                System.Threading.Tasks.Parallel.For(0, nxL - 1, parallelOptions, x =>
                {
                    ct.ThrowIfCancellationRequested();

 // ⭐ stackalloc RAZ na thread
                    Span<SysVector3> points = stackalloc SysVector3[8];
                    Span<float> edgeT = stackalloc float[12];
                    Span<SysVector3> localVerts = stackalloc SysVector3[MAX_VERTS_PER_CUBE];
                    Span<int> localIndices = stackalloc int[MAX_VERTS_PER_CUBE];

                    for (int y = 0; y < nyL - 1; y++)
                    {
                        for (int z = 0; z < nzL - 1; z++)
                        {
 // ⭐ Flat indexing — 1 pointer dereference per voxel
                            int baseIdx = x + strideYL * y + strideZL * z;

                            float v0 = density[baseIdx];                            // (x+0, y+0, z+0)
                            float v1 = density[baseIdx + 1];                        // (x+1, y+0, z+0)
                            float v2 = density[baseIdx + 1 + strideYL];             // (x+1, y+1, z+0)
                            float v3 = density[baseIdx + strideYL];                 // (x+0, y+1, z+0)
                            float v4 = density[baseIdx + strideZL];                 // (x+0, y+0, z+1)
                            float v5 = density[baseIdx + 1 + strideZL];             // (x+1, y+0, z+1)
                            float v6 = density[baseIdx + 1 + strideYL + strideZL];  // (x+1, y+1, z+1)
                            float v7 = density[baseIdx + strideYL + strideZL];      // (x+0, y+1, z+1)

                            int cubeIndex = 0;
                            if (v0 > isoL) cubeIndex |= 1;
                            if (v1 > isoL) cubeIndex |= 2;
                            if (v2 > isoL) cubeIndex |= 4;
                            if (v3 > isoL) cubeIndex |= 8;
                            if (v4 > isoL) cubeIndex |= 16;
                            if (v5 > isoL) cubeIndex |= 32;
                            if (v6 > isoL) cubeIndex |= 64;
                            if (v7 > isoL) cubeIndex |= 128;

                            if (cubeIndex == 0 || cubeIndex == 255) continue;

 // ═══ Positions of the 8 cube vertices ═══
                            points[0] = gridLocal.GetPosition(x, y, z);
                            points[1] = gridLocal.GetPosition(x + 1, y, z);
                            points[2] = gridLocal.GetPosition(x + 1, y + 1, z);
                            points[3] = gridLocal.GetPosition(x, y + 1, z);
                            points[4] = gridLocal.GetPosition(x, y, z + 1);
                            points[5] = gridLocal.GetPosition(x + 1, y, z + 1);
                            points[6] = gridLocal.GetPosition(x + 1, y + 1, z + 1);
                            points[7] = gridLocal.GetPosition(x, y + 1, z + 1);

                            edgeT.Clear();

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
                                e0 = (diff < -Epsilon || diff > Epsilon)
                                    ? FastClamp((isoL - v0) / diff, 0f, 1f) : 0.5f;
                            }
                            if ((edgeFlag & 2) != 0)
                            {
                                float diff = v2 - v1;
                                e1 = (diff < -Epsilon || diff > Epsilon)
                                    ? FastClamp((isoL - v1) / diff, 0f, 1f) : 0.5f;
                            }
                            if ((edgeFlag & 4) != 0)
                            {
                                float diff = v3 - v2;
                                e2 = (diff < -Epsilon || diff > Epsilon)
                                    ? FastClamp((isoL - v2) / diff, 0f, 1f) : 0.5f;
                            }
                            if ((edgeFlag & 8) != 0)
                            {
                                float diff = v0 - v3;
                                e3 = (diff < -Epsilon || diff > Epsilon)
                                    ? FastClamp((isoL - v3) / diff, 0f, 1f) : 0.5f;
                            }
                            if ((edgeFlag & 16) != 0)
                            {
                                float diff = v5 - v4;
                                e4 = (diff < -Epsilon || diff > Epsilon)
                                    ? FastClamp((isoL - v4) / diff, 0f, 1f) : 0.5f;
                            }
                            if ((edgeFlag & 32) != 0)
                            {
                                float diff = v6 - v5;
                                e5 = (diff < -Epsilon || diff > Epsilon)
                                    ? FastClamp((isoL - v5) / diff, 0f, 1f) : 0.5f;
                            }
                            if ((edgeFlag & 64) != 0)
                            {
                                float diff = v7 - v6;
                                e6 = (diff < -Epsilon || diff > Epsilon)
                                    ? FastClamp((isoL - v6) / diff, 0f, 1f) : 0.5f;
                            }
                            if ((edgeFlag & 128) != 0)
                            {
                                float diff = v4 - v7;
                                e7 = (diff < -Epsilon || diff > Epsilon)
                                    ? FastClamp((isoL - v7) / diff, 0f, 1f) : 0.5f;
                            }
                            if ((edgeFlag & 256) != 0)
                            {
                                float diff = v4 - v0;
                                e8 = (diff < -Epsilon || diff > Epsilon)
                                    ? FastClamp((isoL - v0) / diff, 0f, 1f) : 0.5f;
                            }
                            if ((edgeFlag & 512) != 0)
                            {
                                float diff = v5 - v1;
                                e9 = (diff < -Epsilon || diff > Epsilon)
                                    ? FastClamp((isoL - v1) / diff, 0f, 1f) : 0.5f;
                            }
                            if ((edgeFlag & 1024) != 0)
                            {
                                float diff = v6 - v2;
                                e10 = (diff < -Epsilon || diff > Epsilon)
                                    ? FastClamp((isoL - v2) / diff, 0f, 1f) : 0.5f;
                            }
                            if ((edgeFlag & 2048) != 0)
                            {
                                float diff = v7 - v3;
                                e11 = (diff < -Epsilon || diff > Epsilon)
                                    ? FastClamp((isoL - v3) / diff, 0f, 1f) : 0.5f;
                            }

                            var triangles = _trianglesByCase[cubeIndex];
                            if (triangles.Length == 0) continue;

                            System.Diagnostics.Debug.Assert(
                                triangles.Length <= MAX_TRIS_PER_CUBE,
                                $"MC table returned {triangles.Length} triangles " +
                                $"(max {MAX_TRIS_PER_CUBE}).");

                            int localVertCount = 0;
                            int localIndexCount = 0;

                            for (int ti = 0; ti < triangles.Length; ti++)
                            {
                                var tri = triangles[ti];
                                int e0i = tri.e0;
                                int e1i = tri.e1;
                                int e2i = tri.e2;

                                float t0 = edgeT[e0i];
                                float t1 = edgeT[e1i];
                                float t2 = edgeT[e2i];

                                int a0 = _edgeConnections[e0i, 0];
                                int b0 = _edgeConnections[e0i, 1];
                                int a1 = _edgeConnections[e1i, 0];
                                int b1 = _edgeConnections[e1i, 1];
                                int a2 = _edgeConnections[e2i, 0];
                                int b2 = _edgeConnections[e2i, 1];

                                SysVector3 pa0 = points[a0];
                                SysVector3 pb0 = points[b0];
                                SysVector3 pa1 = points[a1];
                                SysVector3 pb1 = points[b1];
                                SysVector3 pa2 = points[a2];
                                SysVector3 pb2 = points[b2];

                                SysVector3 va = pa0 + (pb0 - pa0) * t0;
                                SysVector3 vb = pa1 + (pb1 - pa1) * t1;
                                SysVector3 vc = pa2 + (pb2 - pa2) * t2;

                                localVerts[localVertCount + 0] = va;
                                localVerts[localVertCount + 1] = vb;
                                localVerts[localVertCount + 2] = vc;

                                localIndices[localIndexCount + 0] = localVertCount + 0;
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
                                        int newSize = Math.Max(
                                            vertices.Length * 2,
                                            vertCount + localVertCount + 1024);

                                        if (newSize > MAX_SAFE_VERTICES)
                                        {
                                            throw new InvalidOperationException(
                                                $"Mesh too large: {newSize:N0} vertices");
                                        }

                                        var nv = ArrayPool<SysVector3>.Shared.Rent(newSize);
                                        Array.Copy(vertices, nv, vertCount);
                                        ArrayPool<SysVector3>.Shared.Return(vertices);
                                        vertices = nv;

                                        if (indexCount + localIndexCount > indices.Length)
                                        {
                                            var ni = ArrayPool<int>.Shared.Rent(newSize);
                                            Array.Copy(indices, ni, indexCount);
                                            ArrayPool<int>.Shared.Return(indices);
                                            indices = ni;
                                        }
                                    }
                                    else if (indexCount + localIndexCount > indices.Length)
                                    {
                                        int newSize = Math.Max(
                                            indices.Length * 2,
                                            indexCount + localIndexCount + 1024);
                                        var ni = ArrayPool<int>.Shared.Rent(newSize);
                                        Array.Copy(indices, ni, indexCount);
                                        ArrayPool<int>.Shared.Return(indices);
                                        indices = ni;
                                    }

                                    localVerts.Slice(0, localVertCount)
                                              .CopyTo(vertices.AsSpan(vertCount));

                                    for (int k = 0; k < localIndexCount; k++)
                                        indices[indexCount + k] = localIndices[k] + vertCount;

                                    vertCount += localVertCount;
                                    indexCount += localIndexCount;
                                }
                            }
                        }
                    }

                    int processed = System.Threading.Interlocked.Add(
                        ref processedVoxels, (nyL - 1) * (nzL - 1));

                    if (processed >= progressStepVoxels)
                    {
                        processedVoxels = 0;
                        float pct = optionsLocal.ProgressPercentStart +
                                     (optionsLocal.ProgressPercentEnd - optionsLocal.ProgressPercentStart)
 * 0.5f;
                        optionsLocal.ProgressCallback?.Invoke("Extracting surface...", pct / 100f);
                    }
                });

                if (vertCount == 0)
                {
                    return new HirshfeldMesh();
                }

                var mesh = new HirshfeldMesh
                {
                    Vertices = new List<SysVector3>(vertCount),
                    Indices = new List<int>(indexCount)
                };

                for (int i = 0; i < vertCount; i++)
                    mesh.Vertices.Add(vertices[i]);
                for (int i = 0; i < indexCount; i++)
                    mesh.Indices.Add(indices[i]);

                MergeVerticesFast(mesh);

                if (options.GenerateNormals)
                {
                    mesh.Normals = ComputeVertexNormals(mesh);
                }

                return mesh;
            }
            finally
            {
                ArrayPool<SysVector3>.Shared.Return(vertices);
                ArrayPool<int>.Shared.Return(indices);
            }
        }

        #endregion

        #region Vertex merging

        private static void MergeVerticesFast(HirshfeldMesh mesh)
        {
            if (mesh.VertexCount < 3) return;

            const float tolerance = 0.0001f;
            const float invEps = 1f / tolerance;

            var result = new List<SysVector3>(mesh.VertexCount);
            var grid = new Dictionary<long, List<int>>(mesh.VertexCount);
            var indexMap = new int[mesh.VertexCount];

            float epsSq = tolerance * tolerance;

            long Hash(int ix, int iy, int iz)
                => ((long)ix * 73856093L) ^
                   ((long)iy * 19349663L) ^
                   ((long)iz * 83492791L);

            for (int i = 0; i < mesh.VertexCount; i++)
            {
                SysVector3 p = mesh.Vertices[i];

                int ix = (int)MathF.Round(p.X * invEps);
                int iy = (int)MathF.Round(p.Y * invEps);
                int iz = (int)MathF.Round(p.Z * invEps);

                int found = -1;

                for (int dx = -1; dx <= 1 && found < 0; dx++)
                    for (int dy = -1; dy <= 1 && found < 0; dy++)
                        for (int dz = -1; dz <= 1 && found < 0; dz++)
                        {
                            long h = Hash(ix + dx, iy + dy, iz + dz);
                            if (!grid.TryGetValue(h, out var list)) continue;

                            for (int k = 0; k < list.Count; k++)
                            {
                                int idx = list[k];
                                SysVector3 q = result[idx];
                                float ddx = p.X - q.X;
                                float ddy = p.Y - q.Y;
                                float ddz = p.Z - q.Z;
                                float d2 = ddx * ddx + ddy * ddy + ddz * ddz;
                                if (d2 < epsSq)
                                {
                                    found = idx;
                                    break;
                                }
                            }
                        }

                if (found >= 0)
                {
                    indexMap[i] = found;
                }
                else
                {
                    int newIdx = result.Count;
                    result.Add(p);
                    indexMap[i] = newIdx;

                    long h = Hash(ix, iy, iz);
                    if (!grid.TryGetValue(h, out var list))
                    {
                        list = new List<int>(2);
                        grid[h] = list;
                    }
                    list.Add(newIdx);
                }
            }

            for (int i = 0; i < mesh.Indices.Count; i++)
                mesh.Indices[i] = indexMap[mesh.Indices[i]];

            mesh.Vertices = result;
        }

        #endregion

        #region Normals

 /// <summary>
 /// Computes per-vertex normals.
 ///
 /// ⭐ CHANGE: normals are now weighted by TRIANGLE AREA (not unit-normalized
 /// before accumulation). Marching Cubes produces very thin, needle-like
 /// triangles whose unit normal would otherwise have the same weight as
 /// a large triangle — this caused "speaky" lighting artifacts.
 ///
 /// Using the raw cross product (length = 2 × triangle area) gives a
 /// natural area weighting and produces smooth, physically plausible
 /// lighting on the surface.
 /// </summary>
        private static List<SysVector3> ComputeVertexNormals(HirshfeldMesh mesh)
        {
            int vc = mesh.VertexCount;
            var normals = new SysVector3[vc];

            var verts = mesh.Vertices;
            var inds = mesh.Indices;

            for (int i = 0; i < inds.Count; i += 3)
            {
                int ia = inds[i];
                int ib = inds[i + 1];
                int ic = inds[i + 2];

                if (ia >= vc || ib >= vc || ic >= vc) continue;

                SysVector3 a = verts[ia];
                SysVector3 b = verts[ib];
                SysVector3 c = verts[ic];

 // ⭐ CHANGE: do NOT normalize the cross product.
 // Its length equals 2 × triangle area — natural area weighting.
                SysVector3 n = SysVector3.Cross(b - a, c - a);
                float lenSq = n.LengthSquared();
                if (lenSq < 1e-12f) continue;

                normals[ia] += n;
                normals[ib] += n;
                normals[ic] += n;
            }

            var result = new List<SysVector3>(vc);
            for (int i = 0; i < vc; i++)
            {
                float len = normals[i].Length();
                if (len < 1e-9f)
                    result.Add(new SysVector3(0, 0, 1));
                else
                    result.Add(normals[i] / len);
            }

            return result;
        }

        #endregion
    }
}