// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: CavityRenderer.Drawing.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;

namespace Tal.Cavity
{
    public partial class CavityRenderer
    {
 /// <summary>
 /// ⭐ Main path: CavitySurface shader + ONE shared VB/IB for the entire scene.
 /// Sortowanie back-to-front RAZ for entire sceny.
 ///
 /// ⭐ Wersja 3.1:
 /// - Sorting ALWAYS (all cavities are transparent)
 /// - Sortemy when: pierwszy raz after changesie batcha, camera rusza,
 /// or the camera stopped but rotated > 5° since the last sort
 /// </summary>
        private void DrawCavitiesWithShader(Matrix scaleRotate)
        {
            if (_camera == null || _graphicsDevice == null || _graphicsDevice.IsDisposed) return;
            if (_cavitySurfaceEffect == null) return;

            Matrix world = scaleRotate;
            Matrix view = _camera.View;
            Matrix proj = _camera.Projection;

            bool useVertexColors = (_currentDrawMode == DrawMode.MarchingCubesMeshPerVertex
                                 || _currentDrawMode == DrawMode.CavityColorMap);

 // ⭐ All cavities are transparent → always DepthRead + AlphaBlend + sorting.
 // DepthRead = ZWrite off, ZTest on — correct for transparency.
            _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            _graphicsDevice.BlendState = BlendState.AlphaBlend;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;

 // ═══ Rebuild batch if dirty ═══
            if (_sceneBatchDirty)
                BuildSceneBatch(useVertexColors);

            if (_sceneVB == null || _sceneIB == null || _sceneVertCount == 0)
                return;

 // ═══ Sortowanie sceny ═══
 // Always sort when needed — all cavities are transparent.
            bool activelyMoving = _camera != null && _camera.IsActivelyMoving;
            bool rotatedEnough = HasCameraRotatedEnoughForScene();

            if (!_sceneIndicesSorted || activelyMoving || rotatedEnough)
            {
                SortSceneTrianglesBackToFront(world, view);
                SaveSceneSortAngles();
            }

 // ═══ Set parametery globalne ═══
            var fx = _cavitySurfaceEffect;

            fx.Parameters["World"]?.SetValue(world);
            fx.Parameters["View"]?.SetValue(view);
            fx.Parameters["Projection"]?.SetValue(proj);
            fx.Parameters["CameraPosition"]?.SetValue(_camera.g_cameraPos);

            fx.Parameters["Light1Enabled"]?.SetValue(_lightingCache.Light1Enabled ? 1f : 0f);
            fx.Parameters["LightDirection"]?.SetValue(XnaVector3.Normalize(_lightingCache.Light1Direction));
            fx.Parameters["LightDiffuseColor"]?.SetValue(_lightingCache.Light1Diffuse);
            fx.Parameters["LightSpecularColor"]?.SetValue(_lightingCache.Light1Specular);

            fx.Parameters["Light2Enabled"]?.SetValue(_lightingCache.Light2Enabled ? 1f : 0f);
            fx.Parameters["Light2Direction"]?.SetValue(XnaVector3.Normalize(_lightingCache.Light2Direction));
            fx.Parameters["Light2DiffuseColor"]?.SetValue(_lightingCache.Light2Diffuse);
            fx.Parameters["Light2SpecularColor"]?.SetValue(_lightingCache.Light2Specular);

            XnaVector3 ambient = _lightingCache.AmbientColor;
            if (_lightingCache.Light2Enabled) ambient += _lightingCache.Light2Ambient * 0.3f;
            fx.Parameters["AmbientColor"]?.SetValue(ambient);
            fx.Parameters["SpecularColor"]?.SetValue(_lightingCache.Light1Specular);
            fx.Parameters["SpecularPower"]?.SetValue(_lightingCache.SpecularPower);
            fx.Parameters["Alpha"]?.SetValue(_transparency);

 // ═══ ONE draw call for the entire scene ═══
            _graphicsDevice.SetVertexBuffer(_sceneVB);
            _graphicsDevice.Indices = _sceneIB;

            foreach (var pass in fx.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0, 0,
                    _sceneVertCount,
                    0,
                    _sceneIndexCount / 3);
            }

            _graphicsDevice.SetVertexBuffer(null);
            _graphicsDevice.Indices = null;
        }
    }
}