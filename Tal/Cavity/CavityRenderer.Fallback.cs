// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: CavityRenderer.Fallback.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Buffers;
using System.Runtime.CompilerServices;
using SysVector3 = System.Numerics.Vector3;
using Color = Microsoft.Xna.Framework.Color;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;

namespace Tal.Cavity
{
    public partial class CavityRenderer
    {
        #region Draw - Fallback (BasicEffect)

        private void DrawCavitiesFallback(Matrix scaleRotate)
        {
            if (_camera == null || _graphicsDevice == null) return;

            bool lighting = _currentDrawMode == DrawMode.MarchingCubesMesh
                         || _currentDrawMode == DrawMode.MarchingCubesMeshPerVertex
                         || _currentDrawMode == DrawMode.CavityColorMap
                         || _currentDrawMode == DrawMode.TopologyColorMap
                         || _currentDrawMode == DrawMode.ChannelPathsOverlay;

            bool opaque = _transparency >= 0.90f;

            _graphicsDevice.DepthStencilState = opaque
                ? _depthStencilState
                : DepthStencilState.DepthRead;
            _graphicsDevice.BlendState = opaque ? BlendState.Opaque : BlendState.AlphaBlend;
            _graphicsDevice.RasterizerState = _cullNoneRasterizer;

            var effect = lighting ? _lightingEffect : _effect;
            if (effect == null) return;

            effect.World = scaleRotate;
            effect.View = _camera.View;
            effect.Projection = _camera.Projection;
            effect.Alpha = _transparency;

            if (lighting) ApplyLightingForDraw();

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                switch (_currentDrawMode)
                {
                    case DrawMode.VoxelPoints: DrawVoxelPoints(_cachedDrawList); break;
                    case DrawMode.MarchingCubesMesh:
                    case DrawMode.ChannelPathsOverlay: DrawMarchingCubesMesh(_cachedDrawList); break;
                    case DrawMode.MarchingCubesMeshPerVertex: DrawMarchingCubesMeshPerVertex(_cachedDrawList); break;
                    case DrawMode.MarchingCubesWireframe: DrawMarchingCubesWireframe(_cachedDrawList); break;
                    case DrawMode.CavityColorMap: DrawCavityColorMap(_cachedDrawList); break;
                    case DrawMode.TopologyColorMap: DrawTopologyColorMap(_cachedDrawList); break;
                }
            }

            _graphicsDevice.DepthStencilState = _depthStencilState;
        }

        #endregion

        #region Draw - Lighting (fallback)

        private void ApplyLightingForDraw()
        {
            if (_camera == null || _lightingEffect == null) return;

            XnaVector3 viewLightDir = XnaVector3.Normalize(
                XnaVector3.TransformNormal(_lightingCache.Light1Direction, _camera.View));

            _lightingEffect.DirectionalLight0.Enabled = _lightingCache.Light1Enabled;
            _lightingEffect.DirectionalLight0.Direction = viewLightDir;
            _lightingEffect.DirectionalLight0.DiffuseColor = _lightingCache.Light1Diffuse;
            _lightingEffect.DirectionalLight0.SpecularColor = _lightingCache.Light1Specular;

            if (_lightingCache.Light2Enabled)
            {
                XnaVector3 viewLight2Dir = XnaVector3.Normalize(
                    XnaVector3.TransformNormal(_lightingCache.Light2Direction, _camera.View));

                _lightingEffect.DirectionalLight1.Enabled = true;
                _lightingEffect.DirectionalLight1.Direction = viewLight2Dir;
                _lightingEffect.DirectionalLight1.DiffuseColor = _lightingCache.Light2Diffuse;
                _lightingEffect.DirectionalLight1.SpecularColor = _lightingCache.Light2Specular;
            }
            else
            {
                _lightingEffect.DirectionalLight1.Enabled = false;
            }

            _lightingEffect.DirectionalLight2.Enabled = true;
            XnaVector3 sideDir = XnaVector3.Normalize(XnaVector3.Cross(viewLightDir, XnaVector3.Up));
            if (sideDir.Length() < 0.001f)
                sideDir = XnaVector3.Normalize(XnaVector3.Cross(viewLightDir, XnaVector3.Right));
            _lightingEffect.DirectionalLight2.Direction = sideDir;
            _lightingEffect.DirectionalLight2.DiffuseColor = _lightingCache.Light1Diffuse * 0.25f;
            _lightingEffect.DirectionalLight2.SpecularColor = _lightingCache.Light1Specular * 0.1f;

            XnaVector3 ambient = _lightingCache.AmbientColor;
            if (_lightingCache.Light2Enabled) ambient += _lightingCache.Light2Ambient * 0.5f;
            _lightingEffect.AmbientLightColor = ambient;

            _lightingEffect.SpecularPower = _lightingCache.SpecularPower;
            _lightingEffect.PreferPerPixelLighting = _lightingCache.PerPixelLighting;
            _lightingEffect.LightingEnabled = _lightingCache.LightingEnabled;
        }

        #endregion

        #region Draw - Fallback mesh modes

        private void DrawVoxelPoints(List<RenderCavity> cavities)
        {
            int total = 0;
            foreach (var c in cavities) total += c.BaseVoxels.Length;
            if (total == 0) return;

            var oldDepthStencil = _graphicsDevice.DepthStencilState;
            var oldBlendState = _graphicsDevice.BlendState;

            _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            _graphicsDevice.BlendState = _transparency >= 0.90f ? BlendState.Opaque : BlendState.AlphaBlend;

            var vertices = ArrayPool<VertexPositionColor>.Shared.Rent(total);
            int idx = 0;
            foreach (var c in cavities)
            {
                XnaVector3 off = c.CenterOffset;
                float alpha = _transparency;
                XnaVector4 baseColor = c.BaseColor4;
                Color pointColor = new Color(baseColor.X, baseColor.Y, baseColor.Z, alpha);
                var voxels = c.BaseVoxels;
                for (int i = 0; i < voxels.Length; i++)
                    vertices[idx++] = new VertexPositionColor(voxels[i] + off, pointColor);
            }

            _graphicsDevice.DrawUserPrimitives(PrimitiveType.PointList, vertices, 0, total);
            ArrayPool<VertexPositionColor>.Shared.Return(vertices);

            _graphicsDevice.DepthStencilState = oldDepthStencil;
            _graphicsDevice.BlendState = oldBlendState;
        }

        private void DrawMarchingCubesMesh(List<RenderCavity> cavities)
        {
            if (_graphicsDevice == null || _camera == null) return;

            foreach (var c in cavities)
            {
                if (!c.MarchingCubesGenerated || c.MarchingCubesMesh == null) continue;
                var mesh = c.MarchingCubesMesh;
                if (!mesh.IsValid || mesh.Vertices.Count == 0) continue;

                var normals = GetOrComputeNormalsXna(c);
                int vertCount = mesh.Vertices.Count;

                if (!AreIndicesValid(mesh.Indices, vertCount)) continue;

                var vdata = ArrayPool<VertexPositionNormalColor4>.Shared.Rent(vertCount);
                var vertices = mesh.Vertices;
                XnaVector4 baseColor4 = c.BaseColor4;
                float alpha = _transparency;

                for (int i = 0; i < vertCount; i++)
                {
                    XnaVector4 color = baseColor4;
                    if (alpha < 0.99f) color = new XnaVector4(color.X, color.Y, color.Z, alpha);
                    vdata[i] = new VertexPositionNormalColor4(vertices[i], normals[i], color);
                }

                var indicesArray = ArrayPool<int>.Shared.Rent(mesh.Indices.Count);
                mesh.Indices.CopyTo(indicesArray, 0);

                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList, vdata, 0, vertCount,
                    indicesArray, 0, mesh.TriangleCount);

                ArrayPool<int>.Shared.Return(indicesArray);
                ArrayPool<VertexPositionNormalColor4>.Shared.Return(vdata);
            }
        }

        private void DrawMarchingCubesMeshPerVertex(List<RenderCavity> cavities)
        {
            if (_graphicsDevice == null || _camera == null) return;

            foreach (var c in cavities)
            {
                if (!c.MarchingCubesGenerated || c.MarchingCubesMesh == null) continue;
                var mesh = c.MarchingCubesMesh;
                if (!mesh.IsValid || mesh.Vertices.Count == 0) continue;

                var normals = GetOrComputeNormalsXna(c);
                int vertCount = mesh.Vertices.Count;

                if (!AreIndicesValid(mesh.Indices, vertCount)) continue;

                bool useVertexColors = c.HasVertexColors
                    && c.VertexColors4 != null
                    && c.VertexColors4.Length == vertCount;

                var vdata = ArrayPool<VertexPositionNormalColor4>.Shared.Rent(vertCount);
                var vertices = mesh.Vertices;
                XnaVector4 defaultColor = c.BaseColor4;
                XnaVector4[] vertexColors = useVertexColors ? c.VertexColors4 : null;
                float alpha = _transparency;

                for (int i = 0; i < vertCount; i++)
                {
                    XnaVector4 color = vertexColors != null && i < vertexColors.Length
                        ? vertexColors[i] : defaultColor;
                    if (alpha < 0.99f) color = new XnaVector4(color.X, color.Y, color.Z, alpha);
                    vdata[i] = new VertexPositionNormalColor4(vertices[i], normals[i], color);
                }

                var indicesArray = ArrayPool<int>.Shared.Rent(mesh.Indices.Count);
                mesh.Indices.CopyTo(indicesArray, 0);

                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList, vdata, 0, vertCount,
                    indicesArray, 0, mesh.TriangleCount);

                ArrayPool<int>.Shared.Return(indicesArray);
                ArrayPool<VertexPositionNormalColor4>.Shared.Return(vdata);
            }
        }

        private void DrawMarchingCubesWireframe(List<RenderCavity> cavities)
        {
            if (_graphicsDevice == null || _camera == null) return;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;

            foreach (var c in cavities)
            {
                if (!c.MarchingCubesGenerated || c.MarchingCubesMesh == null) continue;
                var mesh = c.MarchingCubesMesh;
                if (!mesh.IsValid || mesh.Indices.Count == 0) continue;

                var verts = mesh.Vertices;
                var indices = mesh.Indices;
                XnaVector4 baseColor4 = c.BaseColor4;
                float alpha = _transparency;
                Color color = new Color(baseColor4.X, baseColor4.Y, baseColor4.Z, alpha);

                var edges = new HashSet<(int, int)>(indices.Count / 2);
                var edgeList = new List<(int, int)>(indices.Count / 2);

                for (int i = 0; i < indices.Count; i += 3)
                {
                    int a = indices[i], b = indices[i + 1], cIdx = indices[i + 2];
                    AddEdge(edges, edgeList, a, b);
                    AddEdge(edges, edgeList, b, cIdx);
                    AddEdge(edges, edgeList, cIdx, a);
                }

                var lineVerts = ArrayPool<VertexPositionColor>.Shared.Rent(edgeList.Count * 2);
                int idx = 0;
                foreach (var edge in edgeList)
                {
                    lineVerts[idx++] = new VertexPositionColor(verts[edge.Item1], color);
                    lineVerts[idx++] = new VertexPositionColor(verts[edge.Item2], color);
                }

                if (idx > 0)
                    _graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, lineVerts, 0, idx / 2);

                ArrayPool<VertexPositionColor>.Shared.Return(lineVerts);
            }
        }

        private void DrawCavityColorMap(List<RenderCavity> cavities)
        {
            if (_graphicsDevice == null || _camera == null) return;

            foreach (var c in cavities)
            {
                if (!c.MarchingCubesGenerated || c.MarchingCubesMesh == null) continue;
                var mesh = c.MarchingCubesMesh;
                if (!mesh.IsValid) continue;

                var normals = GetOrComputeNormalsXna(c);
                int vertCount = mesh.Vertices.Count;
                if (!AreIndicesValid(mesh.Indices, vertCount)) continue;

                var vdata = ArrayPool<VertexPositionNormalColor4>.Shared.Rent(vertCount);
                var vertices = mesh.Vertices;

                bool useVertexColors = c.HasVertexColors
                    && c.VertexColors4 != null
                    && c.VertexColors4.Length == vertCount;

                XnaVector4 defaultColor = c.BaseColor4;
                XnaVector4[] vertexColors = useVertexColors ? c.VertexColors4 : null;
                float alpha = _transparency;

                for (int i = 0; i < vertCount; i++)
                {
                    XnaVector4 color = vertexColors != null && i < vertexColors.Length
                        ? vertexColors[i] : defaultColor;
                    if (alpha < 0.99f) color = new XnaVector4(color.X, color.Y, color.Z, alpha);
                    vdata[i] = new VertexPositionNormalColor4(vertices[i], normals[i], color);
                }

                var indicesArray = mesh.Indices.ToArray();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList, vdata, 0, vertCount,
                    indicesArray, 0, mesh.TriangleCount);

                ArrayPool<VertexPositionNormalColor4>.Shared.Return(vdata);
            }
        }

        private void DrawTopologyColorMap(List<RenderCavity> cavities)
        {
            if (_graphicsDevice == null || _camera == null) return;

            XnaVector4 GetTopologyColor4(int topologyType)
            {
                switch (topologyType)
                {
                    case 0: return new XnaVector4(0f, 1f, 0f, 1f);
                    case 1: return new XnaVector4(1f, 1f, 0f, 1f);
                    case 2: return new XnaVector4(1f, 0.65f, 0f, 1f);
                    case 3: return new XnaVector4(1f, 0f, 0f, 1f);
                    case 4: return new XnaVector4(1f, 0f, 1f, 1f);
                    case 5: return new XnaVector4(0f, 1f, 1f, 1f);
                    case 6: return new XnaVector4(1f, 1f, 1f, 1f);
                    default: return new XnaVector4(0.5f, 0.5f, 0.5f, 1f);
                }
            }

            foreach (var c in cavities)
            {
                if (!c.MarchingCubesGenerated || c.MarchingCubesMesh == null) continue;
                var mesh = c.MarchingCubesMesh;
                if (!mesh.IsValid) continue;

                XnaVector4 topologyColor = GetTopologyColor4(c.TopologyType);
                float alpha = _transparency;
                if (alpha < 0.99f)
                    topologyColor = new XnaVector4(topologyColor.X, topologyColor.Y, topologyColor.Z, alpha);

                var normals = GetOrComputeNormalsXna(c);
                int vertCount = mesh.Vertices.Count;
                if (!AreIndicesValid(mesh.Indices, vertCount)) continue;

                var vdata = ArrayPool<VertexPositionNormalColor4>.Shared.Rent(vertCount);
                var vertices = mesh.Vertices;

                for (int i = 0; i < vertCount; i++)
                    vdata[i] = new VertexPositionNormalColor4(vertices[i], normals[i], topologyColor);

                var indicesArray = mesh.Indices.ToArray();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList, vdata, 0, vertCount,
                    indicesArray, 0, mesh.TriangleCount);

                ArrayPool<VertexPositionNormalColor4>.Shared.Return(vdata);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreIndicesValid(List<int> indices, int vertCount)
        {
            for (int i = 0; i < indices.Count; i++)
                if (indices[i] < 0 || indices[i] >= vertCount) return false;
            return true;
        }

        private static void AddEdge(HashSet<(int, int)> edges, List<(int, int)> edgeList, int a, int b)
        {
            int min = Math.Min(a, b);
            int max = Math.Max(a, b);
            var key = (min, max);
            if (edges.Add(key)) edgeList.Add(key);
        }

        #endregion

        #region Draw - Channel Paths

        private void DrawChannelPaths(Matrix scaleRotate, bool useDepthTesting = false)
        {
            if (_camera == null || _graphicsDevice == null) return;
            if (_renderCavities == null || _renderCavities.Count == 0) return;

            int totalPaths = 0;
            foreach (var rc in _renderCavities)
            {
                if (rc.CrystalChannelBothPaths == null) continue;
                foreach (var kvp in rc.CrystalChannelBothPaths)
                {
                    totalPaths += (kvp.Value.MinPath?.Count ?? 0) + (kvp.Value.MaxPath?.Count ?? 0);
                }
            }
            if (totalPaths == 0) return;

            Color colorX = Color.Red;
            Color colorY = Color.Green;
            Color colorZ = Color.Blue;

            var oldDepthStencil = _graphicsDevice.DepthStencilState;
            var oldBlendState = _graphicsDevice.BlendState;
            var oldRasterizerState = _graphicsDevice.RasterizerState;

            if (useDepthTesting)
            {
                _graphicsDevice.DepthStencilState = DepthStencilState.Default;
                _graphicsDevice.BlendState = BlendState.AlphaBlend;
            }
            else
            {
                _graphicsDevice.DepthStencilState = DepthStencilState.None;
                _graphicsDevice.BlendState = BlendState.Opaque;
            }
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;

            foreach (var rc in _renderCavities)
            {
                if (rc.CrystalChannelBothPaths == null || rc.CrystalChannelBothPaths.Count == 0) continue;

                foreach (var kvp in rc.CrystalChannelBothPaths)
                {
                    Color color = kvp.Key switch
                    {
                        "XX" => colorX,
                        "YY" => colorY,
                        "ZZ" => colorZ,
                        _ => Color.White
                    };

                    var (minPath, maxPath) = kvp.Value;

                    if (minPath != null && minPath.Count > 1)
                    {
                        Color minColor = new Color(color.R / 2, color.G / 2, color.B / 2,
                            useDepthTesting ? 0.85f : 1.0f);
                        DrawThickLineStripXna(minPath, minColor, scaleRotate);
                    }

                    if (maxPath != null && maxPath.Count > 1)
                    {
                        Color maxColor = new Color(color.R, color.G, color.B,
                            useDepthTesting ? 0.95f : 1.0f);
                        DrawThickLineStripXna(maxPath, maxColor, scaleRotate);
                    }
                }
            }

            _graphicsDevice.DepthStencilState = oldDepthStencil;
            _graphicsDevice.BlendState = oldBlendState;
            _graphicsDevice.RasterizerState = oldRasterizerState;
        }

        private void DrawThickLineStripXna(List<XnaVector3> points, Color color, Matrix scaleRotate)
        {
            if (_camera == null || points == null || points.Count < 2 || _graphicsDevice == null)
                return;

            float lineThickness = 0.15f;

            var quadEffect = new BasicEffect(_graphicsDevice)
            {
                VertexColorEnabled = true,
                LightingEnabled = false,
                World = scaleRotate,
                View = _camera.View,
                Projection = _camera.Projection
            };

            XnaVector3 cameraPos = _camera.g_cameraPos;
            XnaVector3 viewDir = XnaVector3.Normalize(cameraPos - points[0]);

            int pointCount = points.Count;
            int vertexCount = pointCount * 2;
            var vertices = new VertexPositionColor[vertexCount];

            for (int i = 0; i < pointCount; i++)
            {
                XnaVector3 dir;
                if (i == 0) dir = XnaVector3.Normalize(points[1] - points[0]);
                else if (i == pointCount - 1) dir = XnaVector3.Normalize(points[i] - points[i - 1]);
                else
                {
                    XnaVector3 dirPrev = XnaVector3.Normalize(points[i] - points[i - 1]);
                    XnaVector3 dirNext = XnaVector3.Normalize(points[i + 1] - points[i]);
                    dir = XnaVector3.Normalize(dirPrev + dirNext);
                }

                XnaVector3 upDir = XnaVector3.Normalize(XnaVector3.Cross(viewDir, dir));
                if (upDir.Length() < 0.001f)
                {
                    upDir = XnaVector3.Up;
                    if (Math.Abs(XnaVector3.Dot(viewDir, upDir)) > 0.9f)
                        upDir = XnaVector3.Right;
                    upDir = XnaVector3.Normalize(XnaVector3.Cross(viewDir, upDir));
                }

                XnaVector3 offset = upDir * lineThickness * 0.5f;
                vertices[2 * i] = new VertexPositionColor(points[i] - offset, color);
                vertices[2 * i + 1] = new VertexPositionColor(points[i] + offset, color);
            }

            int indexCount = (pointCount - 1) * 6;
            var indices = new int[indexCount];

            for (int i = 0; i < pointCount - 1; i++)
            {
                int baseIndex = i * 6;
                int v0 = i * 2;
                int v1 = i * 2 + 1;
                int v2 = (i + 1) * 2;
                int v3 = (i + 1) * 2 + 1;

                indices[baseIndex + 0] = v0;
                indices[baseIndex + 1] = v2;
                indices[baseIndex + 2] = v1;
                indices[baseIndex + 3] = v1;
                indices[baseIndex + 4] = v2;
                indices[baseIndex + 5] = v3;
            }

            foreach (var pass in quadEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList, vertices, 0, vertexCount,
                    indices, 0, indexCount / 3);
            }

            quadEffect.Dispose();
        }

        #endregion

        #region Draw - Sphere Fits

        private void DrawSphereFitsOnly(List<RenderCavity> cavities, Matrix scaleRotate)
        {
            if (_camera == null || cavities == null || cavities.Count == 0 || _graphicsDevice == null)
                return;

            if (_sphereModel == null || _sphereMeshes == null || _sphereMeshes.Length == 0)
                return;

            const float SPHERE_MODEL_SCALE = 1.0f;

            var oldDepthStencil = _graphicsDevice.DepthStencilState;
            var oldBlendState = _graphicsDevice.BlendState;
            var oldRasterizerState = _graphicsDevice.RasterizerState;

            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;

            XnaVector3 lightDir = XnaVector3.Normalize(_lightingCache.Light1Direction);

            foreach (var rc in cavities)
            {
                var spheres = rc.SphereFits;
                if (spheres == null || spheres.Count == 0) continue;

                Vector3 lightColor = new Vector3(rc.BaseColor4.X, rc.BaseColor4.Y, rc.BaseColor4.Z);
                float maxRadius = spheres.Max(s => s.MaxSphereRadius);

                foreach (var sphere in spheres)
                {
                    Vector3 center = ToXna(sphere.SphereCenter);
                    center += rc.CenterOffset;

                    float radius = sphere.MaxSphereRadius;
                    if (float.IsNaN(center.X) || float.IsNaN(center.Y) || float.IsNaN(center.Z) ||
                        radius <= 0 || float.IsNaN(radius) || float.IsInfinity(radius)) continue;

                    float modelScale = radius * SPHERE_MODEL_SCALE;
                    Matrix sphereMatrix = Matrix.CreateScale(modelScale)
 * Matrix.CreateTranslation(center)
 * scaleRotate;

                    float brightness = 0.6f + 0.4f * (sphere.MaxSphereRadius / maxRadius);
                    Vector3 currentLightColor = lightColor * brightness;

                    foreach (ModelMesh mesh in _sphereMeshes)
                    {
                        Matrix meshTransform = _sphereTransforms[mesh.ParentBone.Index] * sphereMatrix;
                        foreach (BasicEffect effect in mesh.Effects)
                        {
                            effect.VertexColorEnabled = false;
                            effect.PreferPerPixelLighting = true;
                            effect.LightingEnabled = true;
                            effect.DiffuseColor = currentLightColor;
                            effect.AmbientLightColor = currentLightColor * 0.3f;
                            effect.SpecularColor = Vector3.One;
                            effect.SpecularPower = 30f;
                            effect.Alpha = 1.0f;
                            effect.DirectionalLight0.Enabled = true;
                            effect.DirectionalLight0.Direction = lightDir;
                            effect.DirectionalLight0.DiffuseColor = currentLightColor;
                            effect.DirectionalLight0.SpecularColor = currentLightColor;
                            effect.View = _camera.View;
                            effect.Projection = _camera.Projection;
                            effect.World = meshTransform;
                        }
                        mesh.Draw();
                    }
                }
            }

            _graphicsDevice.DepthStencilState = oldDepthStencil;
            _graphicsDevice.BlendState = oldBlendState;
            _graphicsDevice.RasterizerState = oldRasterizerState;
        }

        #endregion

        #region Draw - Molecular Surfaces

        private void DrawMolecularSurfacesInternal(Matrix scaleRotate)
        {
            if (_graphicsDevice == null || _renderMolecularSurfaces.Count == 0 || _camera == null)
                return;

            EnsureLightingCacheValid();

            var effect = _lightingEffect;
            effect.World = scaleRotate;
            effect.View = _camera.View;
            effect.Projection = _camera.Projection;
            effect.Alpha = _transparency;
            effect.VertexColorEnabled = true;
            effect.LightingEnabled = true;

            ApplyLightingForDraw();

            bool opaque = _transparency >= 0.90f;

            _graphicsDevice.DepthStencilState = opaque ? _depthStencilState : DepthStencilState.DepthRead;
            _graphicsDevice.BlendState = opaque ? BlendState.Opaque : BlendState.AlphaBlend;
            _graphicsDevice.RasterizerState = _cullNoneRasterizer;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                DrawMolecularSurfaceMesh(_renderMolecularSurfaces);
            }

            _graphicsDevice.RasterizerState = _cullNoneRasterizer;
            DrawRedSpots(scaleRotate);
        }

        private void DrawMolecularSurfaceMesh(List<RenderMolecularSurface> surfaces)
        {
            if (_graphicsDevice == null || _camera == null) return;

            foreach (var rs in surfaces)
            {
                if (!rs.IsValid) continue;

                var normals = GetOrComputeNormalsXna(rs);
                int vertCount = rs.Vertices.Count;

                if (!AreIndicesValid(rs.Indices, vertCount)) continue;

                var vdata = ArrayPool<VertexPositionNormalColor4>.Shared.Rent(vertCount);
                var vertices = rs.Vertices;

                bool useVertexColors = rs.HasVertexColors
                    && rs.VertexColors4 != null
                    && rs.VertexColors4.Length == vertCount;

                XnaVector4 defaultColor = rs.BaseColor4;
                XnaVector4[] vertexColors = useVertexColors ? rs.VertexColors4 : null;
                float alpha = _transparency;

                for (int i = 0; i < vertCount; i++)
                {
                    XnaVector4 color = vertexColors != null && i < vertexColors.Length
                        ? vertexColors[i] : defaultColor;
                    if (alpha < 0.99f) color = new XnaVector4(color.X, color.Y, color.Z, alpha);
                    vdata[i] = new VertexPositionNormalColor4(vertices[i], normals[i], color);
                }

                int indexCount = rs.Indices.Count;
                var indicesArray = ArrayPool<int>.Shared.Rent(indexCount);
                rs.Indices.CopyTo(indicesArray, 0);

                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList, vdata, 0, vertCount,
                    indicesArray, 0, rs.Indices.Count / 3);

                ArrayPool<int>.Shared.Return(indicesArray);
                ArrayPool<VertexPositionNormalColor4>.Shared.Return(vdata);
            }
        }

        private void DrawRedSpots(Matrix scaleRotate)
        {
            if (_camera == null || _sphereModel == null || _sphereMeshes == null || _sphereMeshes.Length == 0)
                return;

            const float SPHERE_MODEL_SCALE = 1.0f;

            var oldDepthStencil = _graphicsDevice.DepthStencilState;
            var oldBlendState = _graphicsDevice.BlendState;
            var oldRasterizerState = _graphicsDevice.RasterizerState;

            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.BlendState = BlendState.AlphaBlend;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;

            XnaVector3 lightDir = XnaVector3.Normalize(_lightingCache.Light1Direction);

            foreach (var rs in _renderMolecularSurfaces)
            {
                if (rs.RedSpots == null || rs.RedSpots.Count == 0) continue;

                foreach (var spot in rs.RedSpots)
                {
                    if (spot.Intensity < 0.1f) continue;

                    Vector3 center = new Vector3(spot.Position.X, spot.Position.Y, spot.Position.Z);
                    center += new Vector3(rs.CenterOffset.X, rs.CenterOffset.Y, rs.CenterOffset.Z);

                    float radius = 0.15f + spot.Intensity * 0.2f;
                    float modelScale = radius * SPHERE_MODEL_SCALE;
                    Matrix sphereMatrix = Matrix.CreateScale(modelScale)
 * Matrix.CreateTranslation(center)
 * scaleRotate;

                    Vector3 color = new Vector3(1.0f,
                        0.2f * (1.0f - spot.Intensity * 0.5f),
                        0.1f * (1.0f - spot.Intensity));

                    foreach (ModelMesh mesh in _sphereMeshes)
                    {
                        Matrix meshTransform = _sphereTransforms[mesh.ParentBone.Index] * sphereMatrix;
                        foreach (BasicEffect effect in mesh.Effects)
                        {
                            effect.VertexColorEnabled = false;
                            effect.PreferPerPixelLighting = true;
                            effect.LightingEnabled = true;
                            effect.DiffuseColor = color;
                            effect.AmbientLightColor = color * 0.3f;
                            effect.SpecularColor = Vector3.One;
                            effect.SpecularPower = 30f;
                            effect.Alpha = 0.9f;
                            effect.DirectionalLight0.Enabled = true;
                            effect.DirectionalLight0.Direction = lightDir;
                            effect.DirectionalLight0.DiffuseColor = color;
                            effect.DirectionalLight0.SpecularColor = color;
                            effect.View = _camera.View;
                            effect.Projection = _camera.Projection;
                            effect.World = meshTransform;
                        }
                        mesh.Draw();
                    }
                }
            }

            _graphicsDevice.DepthStencilState = oldDepthStencil;
            _graphicsDevice.BlendState = oldBlendState;
            _graphicsDevice.RasterizerState = oldRasterizerState;
        }

        #endregion

        #region Normals

        private XnaVector3[] GetOrComputeNormalsXna(RenderCavity cavity)
        {
            if (cavity.CachedNormals != null && cavity.CachedNormals.Length > 0)
                return cavity.CachedNormals;

            var mesh = cavity.MarchingCubesMesh;
            if (mesh?.Vertices == null || mesh.Indices == null || mesh.Vertices.Count == 0)
                return Array.Empty<XnaVector3>();

            int vertCount = mesh.Vertices.Count;
            var normals = new XnaVector3[vertCount];
            var verts = mesh.Vertices;
            var indices = mesh.Indices;

            if (!AreIndicesValid(indices, vertCount))
            {
                for (int i = 0; i < vertCount; i++) normals[i] = XnaVector3.Up;
                cavity.CachedNormals = normals;
                cavity.NormalsCached = true;
                return normals;
            }

            if (indices.Count > 10000)
            {
                int maxParallel = Math.Max(1, Environment.ProcessorCount / 2);
                var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel };
                var lockObj = new object();

                Parallel.For(0, indices.Count / 3, options, triIdx =>
                {
                    int idx = triIdx * 3;
                    int i0 = indices[idx], i1 = indices[idx + 1], i2 = indices[idx + 2];
                    if (i0 >= vertCount || i1 >= vertCount || i2 >= vertCount) return;

                    var v0 = verts[i0];
                    var v1 = verts[i1];
                    var v2 = verts[i2];
                    var n = XnaVector3.Cross(v1 - v0, v2 - v0);
                    float lenSq = n.LengthSquared();

                    if (lenSq > 1e-6f)
                    {
                        n /= MathF.Sqrt(lenSq);
                        lock (lockObj)
                        {
                            normals[i0] += n;
                            normals[i1] += n;
                            normals[i2] += n;
                        }
                    }
                });
            }
            else
            {
                for (int i = 0; i < indices.Count; i += 3)
                {
                    int i0 = indices[i], i1 = indices[i + 1], i2 = indices[i + 2];
                    if (i0 >= vertCount || i1 >= vertCount || i2 >= vertCount) continue;

                    var v0 = verts[i0];
                    var v1 = verts[i1];
                    var v2 = verts[i2];
                    var n = XnaVector3.Cross(v1 - v0, v2 - v0);
                    float lenSq = n.LengthSquared();
                    if (lenSq > 1e-6f)
                    {
                        n /= MathF.Sqrt(lenSq);
                        normals[i0] += n;
                        normals[i1] += n;
                        normals[i2] += n;
                    }
                }
            }

            for (int i = 0; i < normals.Length; i++)
            {
                float lenSq = normals[i].LengthSquared();
                normals[i] = lenSq > 1e-6f ? XnaVector3.Normalize(normals[i]) : XnaVector3.Up;
                normals[i] = -normals[i];
            }

            cavity.CachedNormals = normals;
            cavity.NormalsCached = true;
            return normals;
        }

        private XnaVector3[] GetOrComputeNormalsXna(RenderMolecularSurface surface)
        {
            if (surface.CachedNormals != null && surface.CachedNormals.Length > 0)
                return surface.CachedNormals;

            if (surface.Vertices == null || surface.Vertices.Count == 0
                || surface.Indices == null || surface.Indices.Count == 0)
                return Array.Empty<XnaVector3>();

            int vertCount = surface.Vertices.Count;
            var normals = new XnaVector3[vertCount];
            var verts = surface.Vertices;
            var indices = surface.Indices;

            if (!AreIndicesValid(indices, vertCount))
            {
                for (int i = 0; i < vertCount; i++) normals[i] = XnaVector3.Up;
                surface.CachedNormals = normals;
                surface.NormalsCached = true;
                return normals;
            }

            for (int i = 0; i < indices.Count; i += 3)
            {
                int i0 = indices[i], i1 = indices[i + 1], i2 = indices[i + 2];
                if (i0 >= vertCount || i1 >= vertCount || i2 >= vertCount) continue;

                var v0 = verts[i0];
                var v1 = verts[i1];
                var v2 = verts[i2];
                var n = XnaVector3.Cross(v1 - v0, v2 - v0);
                float lenSq = n.LengthSquared();
                if (lenSq > 1e-6f)
                {
                    n /= MathF.Sqrt(lenSq);
                    normals[i0] += n;
                    normals[i1] += n;
                    normals[i2] += n;
                }
            }

            for (int i = 0; i < normals.Length; i++)
            {
                float lenSq = normals[i].LengthSquared();
                normals[i] = lenSq > 1e-6f ? XnaVector3.Normalize(normals[i]) : XnaVector3.Up;
                normals[i] = -normals[i];
            }

            surface.CachedNormals = normals;
            surface.NormalsCached = true;
            return normals;
        }

        #endregion

        #region Conversion Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private XnaVector3 ToXna(SysVector3 v) => new XnaVector3(v.X, v.Y, v.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private List<XnaVector3> ToXnaList(List<SysVector3> source)
        {
            if (source == null || source.Count == 0) return new List<XnaVector3>();
            var result = new List<XnaVector3>(source.Count);
            for (int i = 0; i < source.Count; i++)
                result.Add(ToXna(source[i]));
            return result;
        }

        #endregion
    }
}