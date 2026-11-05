// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: CavityRenderer.Batching.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Buffers;
using System.Diagnostics;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;

namespace Tal.Cavity
{
    public partial class CavityRenderer
    {
        #region Scene Batch State

        private VertexBuffer _sceneVB;
        private IndexBuffer _sceneIB;
        private int _sceneVertCount;
        private int _sceneIndexCount;

        private int[] _sceneSortedIndices;

        private bool _sceneBatchDirty = true;
        private bool _sceneIndicesSorted = false;

        private float _sceneLastSortYaw = 0f;
        private float _sceneLastSortPitch = 0f;

        private const float SORT_ANGLE_THRESHOLD_DEG = 8f; // was 5° — fewer sorts while orbiting

        private struct CavityRange
        {
            public int StartVertex;
            public int VertexCount;
            public RenderCavity Cavity;
        }

        private CavityRange[] _sceneRanges;
        private int _sceneRangeCount;

        private float[] _sceneVerticesFlat;
        private int[] _sceneOriginalIndices;

        #endregion

        #region Invalidate

 /// <summary>
 /// Call when cavities, their colours, shift, or mesh changed — anything
 /// which requires rebuilding the shared VB/IB.
 /// </summary>
        public void InvalidateSceneBatch()
        {
            _sceneBatchDirty = true;
            _sceneIndicesSorted = false;
        }

        #endregion

        #region Scene Batch Build

 /// <summary>
 /// Builds a shared VB/IB from all cavities in _cachedDrawList.
 /// Called raz, when batch jest dirty.
 /// </summary>
        private void BuildSceneBatch(bool useVertexColors)
        {
            if (_graphicsDevice == null || _graphicsDevice.IsDisposed) return;

 // ═══ Afterlicz size ═══
            int totalVerts = 0;
            int totalIndices = 0;
            int cavityCount = 0;

            foreach (var c in _cachedDrawList)
            {
                if (c == null || !c.Visible) continue;
                if (!c.MarchingCubesGenerated || c.MarchingCubesMesh == null) continue;
                var mesh = c.MarchingCubesMesh;
                if (!mesh.IsValid) continue;

                totalVerts += mesh.Vertices.Count;
                totalIndices += mesh.Indices.Count;
                cavityCount++;
            }

            if (totalVerts == 0 || totalIndices == 0)
            {
                DisposeSceneBatch();
                return;
            }

 // ═══ Afterbierz buffery z puli ═══
            var vertexData = ArrayPool<VertexPositionNormalColor4>.Shared.Rent(totalVerts);
            var indices = ArrayPool<int>.Shared.Rent(totalIndices);

 // ⭐ Allocate flat arrays (if too small)
            if (_sceneVerticesFlat == null || _sceneVerticesFlat.Length < totalVerts * 3)
                _sceneVerticesFlat = new float[totalVerts * 3];

            if (_sceneOriginalIndices == null || _sceneOriginalIndices.Length < totalIndices)
                _sceneOriginalIndices = new int[totalIndices];

 // ═══ Allocate ranges array ═══
            if (_sceneRanges == null || _sceneRanges.Length < cavityCount)
                _sceneRanges = new CavityRange[cavityCount];
            _sceneRangeCount = 0;

            int vertOffset = 0;
            int idxOffset = 0;

            foreach (var cavity in _cachedDrawList)
            {
                if (cavity == null || !cavity.Visible) continue;
                if (!cavity.MarchingCubesGenerated || cavity.MarchingCubesMesh == null) continue;
                var mesh = cavity.MarchingCubesMesh;
                if (!mesh.IsValid) continue;

                int vertCount = mesh.Vertices.Count;

                var normals = GetOrComputeNormalsXna(cavity);
                if (normals == null || normals.Length != vertCount) continue;

                bool hasVC = useVertexColors
                    && cavity.VertexColors4 != null
                    && cavity.VertexColors4.Length == vertCount;

                XnaVector4 defaultColor = cavity.BaseColor4;

 // ═══ Vertices ═══
                for (int i = 0; i < vertCount; i++)
                {
                    var pos = mesh.Vertices[i];
                    var color = hasVC ? cavity.VertexColors4[i] : defaultColor;
                    vertexData[vertOffset + i] = new VertexPositionNormalColor4(pos, normals[i], color);

                    int flatIdx = (vertOffset + i) * 3;
                    _sceneVerticesFlat[flatIdx] = pos.X;
                    _sceneVerticesFlat[flatIdx + 1] = pos.Y;
                    _sceneVerticesFlat[flatIdx + 2] = pos.Z;
                }

 // ═══ Indeksy z shiftm ═══
                int idxCount = mesh.Indices.Count;
                var srcIndices = mesh.Indices;
                for (int i = 0; i < idxCount; i++)
                {
                    int idx = srcIndices[i] + vertOffset;
                    indices[idxOffset + i] = idx;
                    _sceneOriginalIndices[idxOffset + i] = idx;
                }

 // ═══ Save zakres ═══
                _sceneRanges[_sceneRangeCount++] = new CavityRange
                {
                    StartVertex = vertOffset,
                    VertexCount = vertCount,
                    Cavity = cavity
                };

                vertOffset += vertCount;
                idxOffset += idxCount;
            }

 // ═══ Create VB ═══
            _sceneVB?.Dispose();
            _sceneVB = new VertexBuffer(
                _graphicsDevice,
                VertexPositionNormalColor4.VertexDeclaration,
                totalVerts,
                BufferUsage.WriteOnly);
            _sceneVB.SetData(vertexData, 0, totalVerts);

 // ═══ Create IB ═══
            _sceneIB?.Dispose();
            _sceneIB = new IndexBuffer(
                _graphicsDevice,
                IndexElementSize.ThirtyTwoBits,
                totalIndices,
                BufferUsage.WriteOnly);
            _sceneIB.SetData(indices, 0, totalIndices);

            _sceneVertCount = totalVerts;
            _sceneIndexCount = totalIndices;

 // ═══ Przygotuj buffer na aftersortowane indexy ═══
            if (_sceneSortedIndices == null || _sceneSortedIndices.Length < totalIndices)
                _sceneSortedIndices = new int[totalIndices];

            Array.Copy(indices, _sceneSortedIndices, totalIndices);

 // ═══ Return buffers to the pool ═══
            ArrayPool<VertexPositionNormalColor4>.Shared.Return(vertexData);
            ArrayPool<int>.Shared.Return(indices);

            _sceneBatchDirty = false;
            _sceneIndicesSorted = false;

            Debug.WriteLine($"[CavityRenderer] Scene batch built: " +
                            $"{cavityCount} cavities, {totalVerts} verts, {totalIndices / 3} tris");
        }

        private void DisposeSceneBatch()
        {
            _sceneVB?.Dispose(); _sceneVB = null;
            _sceneIB?.Dispose(); _sceneIB = null;
            _sceneVertCount = 0;
            _sceneIndexCount = 0;
            _sceneSortedIndices = null;
            _sceneVerticesFlat = null;
            _sceneOriginalIndices = null;
            _sceneRanges = null;
            _sceneRangeCount = 0;
            _sceneBatchDirty = true;
            _sceneIndicesSorted = false;
        }

        #endregion

        #region Scene Sorting

        private (float yaw, float pitch) GetCameraAngles()
        {
            if (_camera == null) return (0f, 0f);
            Matrix r = _camera.Rotate;
            float pitch = MathF.Asin(Math.Clamp(-r.M32, -1f, 1f));
            float yaw = MathF.Atan2(r.M31, r.M33);
            return (yaw, pitch);
        }

        private bool HasCameraRotatedEnoughForScene()
        {
            var (yaw, pitch) = GetCameraAngles();
            float dyaw = MathF.Abs(yaw - _sceneLastSortYaw);
            float dpitch = MathF.Abs(pitch - _sceneLastSortPitch);
            if (dyaw > MathF.PI) dyaw = 2 * MathF.PI - dyaw;
            float thresholdRad = SORT_ANGLE_THRESHOLD_DEG * MathF.PI / 180f;
            return dyaw > thresholdRad || dpitch > thresholdRad;
        }

        private void SaveSceneSortAngles()
        {
            var (yaw, pitch) = GetCameraAngles();
            _sceneLastSortYaw = yaw;
            _sceneLastSortPitch = pitch;
        }

 /// <summary>
 /// ⭐ Sorte WSZYSTKIE triangles sceny back-to-front.
 /// One sort for the entire scene, not per-cavity.
 ///
 /// ⭐ Wersja 2.0 — zoptymalizowana:
 /// - Array.Sort(keys, indices) zamiast Array.Sort(tuple, comparer)
 /// - float[] zamiast (int idx, float z)[] — mniej memory, no virtual calls
 /// - ArrayAfterol for obu tablic
 /// - Reversed copy order → back-to-front without Array.Reverse
 ///
 /// Gain: ~30-50% sort time on large scenes.
 /// </summary>
        private void SortSceneTrianglesBackToFront(Matrix world, Matrix view)
        {
            if (_sceneIB == null || _sceneSortedIndices == null) return;
            if (_sceneOriginalIndices == null || _sceneVerticesFlat == null) return;

            int triCount = _sceneIndexCount / 3;
            if (triCount == 0) return;

            Matrix wv = world * view;

 // ⭐ Dwie osobne tablice: klucze (float Z) + indexy (int)
 // Array.Sort(keys, indices) sorte obie razem, without virtual calls,
 // without tuple boxing — znacznie szybciej than Array.Sort(tuple, IComparer).
            var keys = ArrayPool<float>.Shared.Rent(triCount);
            var order = ArrayPool<int>.Shared.Rent(triCount);

            try
            {
                var original = _sceneOriginalIndices;
                var verts = _sceneVerticesFlat;

 // ═══ Phase 1: view-space Z for each triangle centre ═══
                float m13 = wv.M13, m23 = wv.M23, m33 = wv.M33, m43 = wv.M43;
                if (triCount >= 2048)
                {
                    System.Threading.Tasks.Parallel.For(0, triCount, t =>
                    {
                        int i0 = original[t * 3];
                        int i1 = original[t * 3 + 1];
                        int i2 = original[t * 3 + 2];
                        float cx = (verts[i0 * 3] + verts[i1 * 3] + verts[i2 * 3]) * (1f / 3f);
                        float cy = (verts[i0 * 3 + 1] + verts[i1 * 3 + 1] + verts[i2 * 3 + 1]) * (1f / 3f);
                        float cz = (verts[i0 * 3 + 2] + verts[i1 * 3 + 2] + verts[i2 * 3 + 2]) * (1f / 3f);
                        keys[t] = cx * m13 + cy * m23 + cz * m33 + m43;
                        order[t] = t;
                    });
                }
                else
                {
                    for (int t = 0; t < triCount; t++)
                    {
                        int i0 = original[t * 3];
                        int i1 = original[t * 3 + 1];
                        int i2 = original[t * 3 + 2];
                        float cx = (verts[i0 * 3] + verts[i1 * 3] + verts[i2 * 3]) * (1f / 3f);
                        float cy = (verts[i0 * 3 + 1] + verts[i1 * 3 + 1] + verts[i2 * 3 + 1]) * (1f / 3f);
                        float cz = (verts[i0 * 3 + 2] + verts[i1 * 3 + 2] + verts[i2 * 3 + 2]) * (1f / 3f);
                        keys[t] = cx * m13 + cy * m23 + cz * m33 + m43;
                        order[t] = t;
                    }
                }

                // ═══ Phase 2: sort ascending by Z ═══
Array.Sort(keys, order, 0, triCount);

 // ═══ Faza 3: scopy indexy w order back-to-front ═══
                var sorted = _sceneSortedIndices;
                for (int t = 0; t < triCount; t++)
                {
                    int src = order[t] * 3;
                    int dst = t * 3;
                    sorted[dst] = original[src];
                    sorted[dst + 1] = original[src + 1];
                    sorted[dst + 2] = original[src + 2];
                }

 // ═══ Faza 4: upload IB na GPU ═══
                _sceneIB.SetData(sorted, 0, _sceneIndexCount);
                _sceneIndicesSorted = true;
            }
            finally
            {
                ArrayPool<float>.Shared.Return(keys);
                ArrayPool<int>.Shared.Return(order);
            }
        }

        #endregion
    }
}