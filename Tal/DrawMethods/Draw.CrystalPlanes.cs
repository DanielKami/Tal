// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.CrystalPlanes.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.CrystalPlanes — crystallographic planes + their intersections
// ═══════════════════════════════════════════════════════════════════════
//
// (file header unchanged)
//
// ⭐ TIER 1 OPTIMIZATION APPLIED:
// DrawPlaneQuad and DrawPlaneEdges now use pre-allocated vertex
// buffers from Draw.cs (_planeQuadBuffer / _planeEdgesBuffer)
// instead of allocating a new VertexAftersitionColor[] every frame.
// Eliminates 2 allocations per plane per frame.
//
// Why pre-allocated arrays instead of stackalloc?
// MonoGame's DrawUserPrimitives only accepts T[] — not Span<T>.
// Since the buffer sizes are fixed (6 quad verts, 8 edge verts),
// a single field-level array can be reused indefinitely with
// zero allocation and zero GC pressure.
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tal.Core.PowderDiffraction;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region DrawCrystalPlanes — main dispatcher

 /// <summary>
 /// Draws all visible crystal planes with their edges.
 /// </summary>
        private void DrawCrystalPlanes(Matrix scaleRotate)
        {
            if (_crystalPlanes == null || _crystalPlanes.Count == 0) return;
            if (_graphicsDevice == null || _camera == null || _molecule == null) return;

            Vector3 shift = _molecule.MoleculeShift;

            var savedBlendState = _graphicsDevice.BlendState;
            var savedDepthStencil = _graphicsDevice.DepthStencilState;
            var savedRasterizer = _graphicsDevice.RasterizerState;

            _graphicsDevice.BlendState = BlendState.AlphaBlend;
            _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            _graphicsDevice.RasterizerState = GetCullNone();

            foreach (var plane in _crystalPlanes)
            {
                if (plane == null || !plane.Visible) continue;
                if (plane.Corners == null || plane.Corners.Length < 4) continue;

                DrawSingleCrystalPlane(plane, scaleRotate, shift);
            }

            _graphicsDevice.BlendState = savedBlendState;
            _graphicsDevice.DepthStencilState = savedDepthStencil;
            _graphicsDevice.RasterizerState = savedRasterizer;
        }

        #endregion

        #region DrawSingleCrystalPlane — quad + edges

 /// <summary>
 /// Draws one crystal plane as a filled quad plus its edges.
 /// </summary>
        private void DrawSingleCrystalPlane(CrystalPlane plane, Matrix scaleRotate, Vector3 shift)
        {
            Vector4 fillColor = new Vector4(
                plane.Color.R / 255f,
                plane.Color.G / 255f,
                plane.Color.B / 255f,
                plane.Transparent ? 0.35f : 0.85f);

            Vector4 edgeColor = new Vector4(
                plane.Color.R / 255f * 0.7f,
                plane.Color.G / 255f * 0.7f,
                plane.Color.B / 255f * 0.7f,
                1.0f);

            Vector3 negShift = -shift;

            Vector3 c0 = plane.Corners[0] + negShift;
            Vector3 c1 = plane.Corners[1] + negShift;
            Vector3 c2 = plane.Corners[2] + negShift;
            Vector3 c3 = plane.Corners[3] + negShift;

            DrawPlaneQuad(c0, c1, c2, c3, fillColor, scaleRotate);
            DrawPlaneEdges(c0, c1, c2, c3, edgeColor, scaleRotate);
        }

        #endregion

        #region DrawPlaneQuad — 2 triangles (⭐ pre-allocated buffer)

 /// <summary>
 /// Draws a filled quad as two triangles.
 ///
 /// ⭐ TIER 1: uses the pre-allocated _planeQuadBuffer (6 vertices)
 /// from Draw.cs. No heap allocation per frame.
 /// </summary>
        private void DrawPlaneQuad(
            Vector3 c0, Vector3 c1, Vector3 c2, Vector3 c3,
            Vector4 color, Matrix scaleRotate)
        {
            XnaColor xnaColor = new XnaColor(color.X, color.Y, color.Z, color.W);

 // ═══ Reuse pre-allocated buffer (fixed size = 6) ═══
            var vertices = _planeQuadBuffer;

            vertices[0] = new VertexPositionColor(c0, xnaColor);
            vertices[1] = new VertexPositionColor(c1, xnaColor);
            vertices[2] = new VertexPositionColor(c2, xnaColor);
            vertices[3] = new VertexPositionColor(c0, xnaColor);
            vertices[4] = new VertexPositionColor(c2, xnaColor);
            vertices[5] = new VertexPositionColor(c3, xnaColor);

            _basicEffect.World = scaleRotate;
            _basicEffect.View = _camera.View;
            _basicEffect.Projection = _camera.Projection;
            _basicEffect.VertexColorEnabled = true;
            _basicEffect.LightingEnabled = false;
            _basicEffect.TextureEnabled = false;

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
            }
        }

        #endregion

        #region DrawPlaneEdges — 4 lines (⭐ pre-allocated buffer)

 /// <summary>
 /// Draws the 4 edges of a crystal plane quad.
 ///
 /// ⭐ TIER 1: uses the pre-allocated _planeEdgesBuffer (8 vertices)
 /// from Draw.cs. No heap allocation per frame.
 /// </summary>
        private void DrawPlaneEdges(
            Vector3 c0, Vector3 c1, Vector3 c2, Vector3 c3,
            Vector4 color, Matrix scaleRotate)
        {
            XnaColor xnaColor = new XnaColor(color.X, color.Y, color.Z, color.W);

 // ═══ Reuse pre-allocated buffer (fixed size = 8) ═══
            var vertices = _planeEdgesBuffer;

            vertices[0] = new VertexPositionColor(c0, xnaColor);
            vertices[1] = new VertexPositionColor(c1, xnaColor);
            vertices[2] = new VertexPositionColor(c1, xnaColor);
            vertices[3] = new VertexPositionColor(c2, xnaColor);
            vertices[4] = new VertexPositionColor(c2, xnaColor);
            vertices[5] = new VertexPositionColor(c3, xnaColor);
            vertices[6] = new VertexPositionColor(c3, xnaColor);
            vertices[7] = new VertexPositionColor(c0, xnaColor);

            _basicEffect.World = scaleRotate;
            _basicEffect.View = _camera.View;
            _basicEffect.Projection = _camera.Projection;
            _basicEffect.VertexColorEnabled = true;
            _basicEffect.LightingEnabled = false;
            _basicEffect.TextureEnabled = false;

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 4);
            }
        }

        #endregion

        #region DrawPlaneIntersections — yellow intersection lines

 /// <summary>
 /// Draws yellow intersection lines between pairs of crystal planes.
 /// Not called from DrawScene currently.
 /// </summary>
        private void DrawPlaneIntersections(Matrix scaleRotate, Vector3 shift)
        {
            RebuildPlaneIntersections();

            if (_planeIntersectionLines.Count == 0) return;
            if (_cachedSmallRodMeshes == null || _smallRodTransforms == null) return;

            var savedDepthStencil = _graphicsDevice.DepthStencilState;
            _graphicsDevice.DepthStencilState = DepthStencilState.None;

            Vector3 origDiffuse = _effectCache.DiffuseColor;
            Vector3 origAmbient = _effectCache.AmbientLightColor;
            Vector3 origLightDiffuse = _effectCache.LightDiffuseColor;
            Vector3 origSpecular = _effectCache.SpecularColor;
            Vector3 origLightSpecular = _effectCache.LightSpecularColor;
            float origAlpha = _effectCache.Alpha;

            Vector3 lineColor = new Vector3(1.0f, 1.0f, 0.0f);
            _effectCache.DiffuseColor = lineColor;
            _effectCache.AmbientLightColor = Vector3.One * 0.7f;
            _effectCache.LightDiffuseColor = lineColor;
            _effectCache.SpecularColor = Vector3.Zero;
            _effectCache.LightSpecularColor = Vector3.Zero;
            _effectCache.Alpha = 1.0f;

            const float lineThickness = 0.04f;

            Vector3 negShift = -shift;

            foreach (var (startRaw, endRaw) in _planeIntersectionLines)
            {
                Vector3 start = startRaw + negShift;
                Vector3 end = endRaw + negShift;

                DrawIntersectionSegment(start, end, scaleRotate, lineThickness);
            }

            _effectCache.DiffuseColor = origDiffuse;
            _effectCache.AmbientLightColor = origAmbient;
            _effectCache.LightDiffuseColor = origLightDiffuse;
            _effectCache.SpecularColor = origSpecular;
            _effectCache.LightSpecularColor = origLightSpecular;
            _effectCache.Alpha = origAlpha;

            _graphicsDevice.DepthStencilState = savedDepthStencil;
        }

        #endregion

        #region DrawIntersectionSegment — delegates to DrawRodSegment

        private void DrawIntersectionSegment(Vector3 p1, Vector3 p2, Matrix scaleRotate, float thickness)
        {
            DrawRodSegment(p1, p2, scaleRotate, thickness);
        }

        #endregion

        #region RebuildPlaneIntersections — compute intersection lines

        private void RebuildPlaneIntersections()
        {
            _planeIntersectionLines.Clear();

            if (_crystalPlanes == null || _crystalPlanes.Count < 2) return;

            var visible = _crystalPlanes.Where(p => p != null && p.Visible).ToList();
            if (visible.Count < 2) return;

            for (int i = 0; i < visible.Count; i++)
            {
                for (int j = i + 1; j < visible.Count; j++)
                {
                    var inter = PlaneGeometry.ComputeIntersection(visible[i], visible[j]);

                    if (!inter.IsValid) continue;

                    Vector3 pA = inter.Point - inter.Direction * inter.HalfLength;
                    Vector3 pB = inter.Point + inter.Direction * inter.HalfLength;

                    _planeIntersectionLines.Add((pA, pB));
                }
            }
        }

        #endregion
    }
}