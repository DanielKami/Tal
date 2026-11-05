// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Ball batching: queue sphere world matrices and draw with shared LOD mesh + BasicEffect
// (same pattern as Draw.BondBatch for sticks).

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region Ball batch state

        private struct BallInstance
        {
            public Matrix World;
            public Vector3 Color;
            public float Alpha;
            public int Lod;
            public bool Transparent;
        }

        private BallInstance[] _ballBatch;
        private int _ballBatchCount;
        private const int BallBatchInitialCapacity = 4096;

        private void EnsureBallBatchCapacity(int needed)
        {
            if (_ballBatch == null)
                _ballBatch = new BallInstance[Math.Max(BallBatchInitialCapacity, needed)];
            else if (needed > _ballBatch.Length)
                Array.Resize(ref _ballBatch, Math.Max(needed, _ballBatch.Length * 2));
        }

        private void ClearBallBatch()
        {
            _ballBatchCount = 0;
        }

        /// <summary>Queue one ball (style 2 / 4) for batched draw.</summary>
        private void EnqueueBall(Matrix world, Vector3 color, float alpha, int lod, bool transparent)
        {
            if (lod < 0) lod = 0;
            if (lod > 2) lod = 2;
            EnsureBallBatchCapacity(_ballBatchCount + 1);
            _ballBatch[_ballBatchCount++] = new BallInstance
            {
                World = world,
                Color = color,
                Alpha = alpha,
                Lod = lod,
                Transparent = transparent
            };
        }

        /// <summary>
        /// Draw queued balls and clear. Call after opaque atom pass and after transparent pass.
        /// </summary>
        private void FlushBallBatch(bool transparentPass)
        {
            if (_ballBatchCount == 0) return;
            if (_graphicsDevice == null || _graphicsDevice.IsDisposed) return;

            BasicEffect effect = _smallBallEffect ?? _bigBallEffect;
            if (effect == null)
            {
                // Try from LOD cache
                if (_cachedBallMeshesLod != null)
                {
                    for (int t = 0; t < 3 && effect == null; t++)
                    {
                        var meshes = _cachedBallMeshesLod[t];
                        if (meshes == null) continue;
                        effect = FindFirstBasicEffect(meshes);
                    }
                }
            }
            if (effect == null) return;

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

            for (int i = 0; i < _ballBatchCount; i++)
            {
                ref BallInstance b = ref _ballBatch[i];
                if (b.Transparent != transparentPass)
                    continue;

                ModelMesh[] meshes = _cachedBallMeshesLod != null && _cachedBallMeshesLod[b.Lod] != null
                    ? _cachedBallMeshesLod[b.Lod]
                    : _cachedSmallBallMeshes;
                Matrix[] transforms = _ballTransformsLod != null && _ballTransformsLod[b.Lod] != null
                    ? _ballTransformsLod[b.Lod]
                    : _smallBallTransforms;

                if (meshes == null || transforms == null) continue;

                effect.DiffuseColor = b.Color;
                effect.Alpha = b.Alpha;
                effect.AmbientLightColor = b.Color * Math.Max(_effectCache.AmbientLightColor.X, 0.15f);
                effect.DirectionalLight0.DiffuseColor = b.Color;

                for (int m = 0; m < meshes.Length; m++)
                {
                    ModelMesh mesh = meshes[m];
                    int bone = mesh.ParentBone != null ? mesh.ParentBone.Index : 0;
                    if (bone < 0 || bone >= transforms.Length)
                        bone = 0;

                    effect.World = transforms[bone] * b.World;

                    foreach (ModelMeshPart part in mesh.MeshParts)
                    {
                        if (part.VertexBuffer == null || part.IndexBuffer == null)
                            continue;
                        if (part.PrimitiveCount <= 0)
                            continue;

                        _graphicsDevice.SetVertexBuffer(part.VertexBuffer);
                        _graphicsDevice.Indices = part.IndexBuffer;

                        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            _graphicsDevice.DrawIndexedPrimitives(
                                PrimitiveType.TriangleList,
                                part.VertexOffset,
                                part.StartIndex,
                                part.PrimitiveCount);
                        }
                    }
                }
            }

            effect.Alpha = 1f;

            // Only clear after transparent pass (opaque pass leaves transparent entries)
            if (transparentPass)
                ClearBallBatch();
        }

        #endregion
    }
}
