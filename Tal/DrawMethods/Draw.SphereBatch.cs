// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Sphere batch: balls (style 2/4) and ellipsoids (style 3) share the icosphere mesh.
// Same pattern as Draw.BondBatch — queue instances, flush once per opacity pass.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region Sphere batch state

        private struct SphereInstance
        {
            public Matrix World;   // local atom matrix (bone applied at flush)
            public Vector3 Color;
            public float Alpha;
            public int Lod;
        }

        private SphereInstance[] _sphereBatch;
        private int _sphereBatchCount;
        private const int SphereBatchInitialCapacity = 4096;

        private void EnsureSphereBatchCapacity(int needed)
        {
            if (_sphereBatch == null)
                _sphereBatch = new SphereInstance[Math.Max(SphereBatchInitialCapacity, needed)];
            else if (needed > _sphereBatch.Length)
                Array.Resize(ref _sphereBatch, Math.Max(needed, _sphereBatch.Length * 2));
        }

        private void ClearSphereBatch()
        {
            _sphereBatchCount = 0;
        }

        /// <summary>
        /// Queue a ball or ellipsoid body (icosphere) for the current opacity pass.
        /// </summary>
        private void EnqueueSphere(Matrix world, Vector3 color, float alpha, int lod)
        {
            if (lod < 0) lod = 0;
            if (lod > 2) lod = 2;
            if (alpha < 0.01f) return;

            EnsureSphereBatchCapacity(_sphereBatchCount + 1);
            _sphereBatch[_sphereBatchCount++] = new SphereInstance
            {
                World = world,
                Color = color,
                Alpha = alpha,
                Lod = lod
            };
        }

        /// <summary>
        /// Draw all queued spheres and clear. Call once after each atom opacity pass
        /// (after FlushBondBatch is fine).
        /// </summary>
        private void FlushSphereBatch()
        {
            int count = _sphereBatchCount;
            if (count == 0) return;
            if (_graphicsDevice == null || _graphicsDevice.IsDisposed)
            {
                ClearSphereBatch();
                return;
            }

            BasicEffect effect = _smallBallEffect ?? _bigBallEffect;
            if (effect == null && _cachedBallMeshesLod != null)
            {
                for (int t = 0; t < 3 && effect == null; t++)
                    effect = FindFirstBasicEffect(_cachedBallMeshesLod[t]);
            }
            if (effect == null)
            {
                // Fallback: draw nothing rather than crash
                ClearSphereBatch();
                return;
            }

            ApplyGlobalEffectStateTo(effect);
            effect.VertexColorEnabled = false;
            effect.TextureEnabled = false;
            effect.LightingEnabled = _effectCache.LightingEnabled;
            effect.PreferPerPixelLighting = _effectCache.PreferPerPixelLighting;
            effect.SpecularPower = _effectCache.SpecularPower;
            effect.SpecularColor = _effectCache.SpecularColor;

            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight0.Direction = _effectCache.LightDirection;
            effect.DirectionalLight0.SpecularColor = _effectCache.LightSpecularColor;

            effect.DirectionalLight1.Enabled = Flags.Instance.Light2Enabled;
            effect.DirectionalLight1.Direction = _effectCache.Light2Direction;
            effect.DirectionalLight1.DiffuseColor = _effectCache.Light2DiffuseColor;
            effect.DirectionalLight1.SpecularColor = _effectCache.Light2SpecularColor;

            for (int i = 0; i < count; i++)
            {
                ref SphereInstance s = ref _sphereBatch[i];

                ModelMesh[] meshes = _cachedBallMeshesLod != null && _cachedBallMeshesLod[s.Lod] != null
                    ? _cachedBallMeshesLod[s.Lod]
                    : (_cachedSmallBallMeshes ?? _cachedBigBallMeshes);
                Matrix[] transforms = _ballTransformsLod != null && _ballTransformsLod[s.Lod] != null
                    ? _ballTransformsLod[s.Lod]
                    : (_smallBallTransforms ?? _bigBallTransforms);

                if (meshes == null || transforms == null || meshes.Length == 0)
                    continue;

                effect.DiffuseColor = s.Color;
                effect.Alpha = s.Alpha;
                float amb = Math.Max(_effectCache.AmbientLightColor.X, 0.15f);
                effect.AmbientLightColor = s.Color * amb;
                effect.DirectionalLight0.DiffuseColor = s.Color;

                for (int m = 0; m < meshes.Length; m++)
                {
                    ModelMesh mesh = meshes[m];
                    int bone = 0;
                    if (mesh.ParentBone != null)
                    {
                        bone = mesh.ParentBone.Index;
                        if (bone < 0 || bone >= transforms.Length)
                            bone = 0;
                    }

                    effect.World = transforms[bone] * s.World;

                    // Prefer Mesh.Draw — keeps Model vertex bindings correct
                    foreach (BasicEffect meshEffect in mesh.Effects)
                    {
                        meshEffect.World = effect.World;
                        meshEffect.View = effect.View;
                        meshEffect.Projection = effect.Projection;
                        meshEffect.DiffuseColor = effect.DiffuseColor;
                        meshEffect.AmbientLightColor = effect.AmbientLightColor;
                        meshEffect.Alpha = effect.Alpha;
                        meshEffect.SpecularColor = effect.SpecularColor;
                        meshEffect.SpecularPower = effect.SpecularPower;
                        meshEffect.LightingEnabled = effect.LightingEnabled;
                        meshEffect.PreferPerPixelLighting = effect.PreferPerPixelLighting;
                        meshEffect.VertexColorEnabled = false;
                        meshEffect.TextureEnabled = false;
                        meshEffect.DirectionalLight0.Enabled = true;
                        meshEffect.DirectionalLight0.Direction = effect.DirectionalLight0.Direction;
                        meshEffect.DirectionalLight0.DiffuseColor = effect.DirectionalLight0.DiffuseColor;
                        meshEffect.DirectionalLight0.SpecularColor = effect.DirectionalLight0.SpecularColor;
                        meshEffect.DirectionalLight1.Enabled = effect.DirectionalLight1.Enabled;
                        meshEffect.DirectionalLight1.Direction = effect.DirectionalLight1.Direction;
                        meshEffect.DirectionalLight1.DiffuseColor = effect.DirectionalLight1.DiffuseColor;
                        meshEffect.DirectionalLight1.SpecularColor = effect.DirectionalLight1.SpecularColor;
                    }
                    mesh.Draw();
                }
            }

            effect.Alpha = 1f;
            ClearSphereBatch();
        }

        #endregion
    }
}
