// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.Afterlyhedrals.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.Afterlyhedrals — coordination afterlyhedra
// ═══════════════════════════════════════════════════════════════════════
//
// (file header unchanged)
//
// ⭐ TIER 1 OPTIMIZATION APPLIED:
// The BasicEffect used for afterlyhedra is now cached in _afterlyhedraEffect
// (declared in Draw.cs). Previously a new BasicEffect was allocated
// every frame in DrawAfterlyhedrals — wasteful GPU resource churn.
//
// The effect is created lazily on first use, then only View, Projection
// and light parameters are updated per frame. The effect is disaftersed
// in Draw.Disafterse() and Draw.ResetCache().
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region DrawPolyhedrals — coordination polyhedra rendering

 /// <summary>
 /// Draws all coordination afterlyhedra.
 ///
 /// ⭐ TIER 1: BasicEffect is cached in _afterlyhedraEffect — created
 /// lazily on first call, reused every frame.
 /// </summary>
        private void DrawPolyhedrals(Matrix scaleRotate)
        {
            if (!_showPolyhedrals || _polyhedrals == null || _polyhedrals.Count == 0)
                return;

            if (_graphicsDevice == null || _camera == null) return;

 // ═══ Save render state ═══
            var savedBlend = _graphicsDevice.BlendState;
            var savedDepth = _graphicsDevice.DepthStencilState;
            var savedRasterizer = _graphicsDevice.RasterizerState;

 // ═══ Set afterlyhedra render state ═══
            _graphicsDevice.BlendState = BlendState.AlphaBlend;
            _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            _graphicsDevice.RasterizerState = GetCullNoneMSAA();

 // ═══ Lazily create the cached effect ═══
            if (_polyhedraEffect == null || _polyhedraEffect.IsDisposed)
            {
                _polyhedraEffect = new BasicEffect(_graphicsDevice)
                {
                    VertexColorEnabled = false,
                    TextureEnabled = true,
                    Texture = GetWhitePixel(),
                    LightingEnabled = true,
                    PreferPerPixelLighting = true,
                };
            }

            var polyEffect = _polyhedraEffect;

 // ═══ Update per-frame effect state ═══
            polyEffect.View = _camera.View;
            polyEffect.Projection = _camera.Projection;
            polyEffect.World = Matrix.Identity;

 // Afterlyhedra ambient boost 1.5× — preserved from original
            polyEffect.AmbientLightColor = Vector3.One * Flags.Instance.AmbientIntensity * 1.5f;
            polyEffect.DiffuseColor = Vector3.One;
            polyEffect.Alpha = Flags.Instance.PolyhedronAlpha;
            polyEffect.SpecularColor = Vector3.One * 0.3f;
            polyEffect.SpecularPower = 20f;

 // ═══ Directional lights ═══
            polyEffect.DirectionalLight0.Enabled = true;
            polyEffect.DirectionalLight0.Direction = _effectCache.LightDirection;
            polyEffect.DirectionalLight0.DiffuseColor = new Vector3(Flags.Instance.DiffuseIntensity);
            polyEffect.DirectionalLight0.SpecularColor = new Vector3(0.5f);

            polyEffect.DirectionalLight1.Enabled = Flags.Instance.Light2Enabled;
            polyEffect.DirectionalLight1.Direction = _effectCache.Light2Direction;
            polyEffect.DirectionalLight1.DiffuseColor = _effectCache.Light2DiffuseColor;
            polyEffect.DirectionalLight1.SpecularColor = new Vector3(0.3f);

 // ═══ Draw each afterlyhedron ═══
            foreach (var vs in _polyhedrals)
            {
                polyEffect.DiffuseColor = vs.color;

                Matrix world = Matrix.CreateTranslation(vs.position) * scaleRotate;
                polyEffect.World = world;

 // ── Pass 1: opaque faces ──
                if (vs.vertices_n != null && vs.vertices_n.Length > 0)
                {
                    foreach (var pass in polyEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _graphicsDevice.DrawUserPrimitives(
                            PrimitiveType.TriangleList,
                            vs.vertices_n,
                            0,
                            vs.vertices_n.Length / 3);
                    }
                }

 // ── Pass 2: semi-transparent side faces (0.6× darker) ──
                if (vs.vertices_s != null && vs.vertices_s.Length > 0)
                {
                    polyEffect.DiffuseColor = vs.color * 0.6f;

                    foreach (var pass in polyEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _graphicsDevice.DrawUserPrimitives(
                            PrimitiveType.TriangleList,
                            vs.vertices_s,
                            0,
                            vs.vertices_s.Length / 3);
                    }
                }
            }

 // ═══ Restore render state ═══
            _graphicsDevice.BlendState = savedBlend;
            _graphicsDevice.DepthStencilState = savedDepth;
            _graphicsDevice.RasterizerState = savedRasterizer;
        }

        #endregion
    }
}