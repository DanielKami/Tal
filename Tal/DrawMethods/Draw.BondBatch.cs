// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Bond batching: collect stick world matrices, then draw with a shared rod mesh
// and a single BasicEffect setup path (fewer effect state changes than Model.Draw).
//
// Partial-occupancy half-bonds (bondOcc = min(occ_i, occ_j) < 1) are always
// queued from BOTH endpoints so the full-occupancy atom still contributes its
// coloured half-stick in the transparent pass.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region Bond batch state

        private struct BondInstance
        {
            public Matrix World;
            public Vector3 Color;
            public float Alpha;
            public int Lod; // 0=low 1=med 2=high
            public bool Transparent; // true when Alpha < ~1 (partial occupancy)
        }

        private BondInstance[] _bondBatch;
        private int _bondBatchCount;
        private const int BondBatchInitialCapacity = 4096;

        #endregion

        #region Ensure / clear

        private void EnsureBondBatchCapacity(int needed)
        {
            if (_bondBatch == null)
                _bondBatch = new BondInstance[Math.Max(BondBatchInitialCapacity, needed)];
            else if (needed > _bondBatch.Length)
                Array.Resize(ref _bondBatch, Math.Max(needed, _bondBatch.Length * 2));
        }

        private void ClearBondBatch()
        {
            _bondBatchCount = 0;
        }

        /// <summary>
        /// Queue one stick (half-bond) for the current frame.
        /// <paramref name="alpha"/> is typically min(occ_i, occ_j).
        /// </summary>
        private void EnqueueBond(Matrix world, Vector3 color, float alpha, int lod = 2)
        {
            if (lod < 0) lod = 0;
            if (lod > 2) lod = 2;
            if (alpha < 0.01f) return;

            EnsureBondBatchCapacity(_bondBatchCount + 1);
            _bondBatch[_bondBatchCount++] = new BondInstance
            {
                World = world,
                Color = color,
                Alpha = alpha,
                Lod = lod,
                Transparent = alpha < 0.999f
            };
        }

        #endregion

        #region Flush

        /// <summary>
        /// Draw queued bonds for one opacity pass.
        /// Opaque pass: only full-occupancy sticks; leave partial sticks in the batch.
        /// Transparent pass: only partial sticks, then clear the batch.
        /// </summary>
        private void FlushBondBatch(bool transparentPass)
        {
            if (_bondBatchCount == 0) return;
            if (_cachedSmallRodMeshes == null || _smallRodTransforms == null) return;
            if (_graphicsDevice == null || _graphicsDevice.IsDisposed) return;

            BasicEffect effect = _rodEffect;
            if (effect == null)
            {
                foreach (var mesh in _cachedSmallRodMeshes)
                {
                    foreach (var e in mesh.Effects)
                    {
                        if (e is BasicEffect be)
                        {
                            effect = be;
                            break;
                        }
                    }
                    if (effect != null) break;
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

            for (int i = 0; i < _bondBatchCount; i++)
            {
                ref BondInstance b = ref _bondBatch[i];
                if (b.Transparent != transparentPass)
                    continue;

                ModelMesh[] meshes = _cachedRodMeshesLod != null && _cachedRodMeshesLod[b.Lod] != null
                    ? _cachedRodMeshesLod[b.Lod]
                    : _cachedSmallRodMeshes;
                Matrix[] transforms = _rodTransformsLod != null && _rodTransformsLod[b.Lod] != null
                    ? _rodTransformsLod[b.Lod]
                    : _smallRodTransforms;

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

            if (transparentPass)
                ClearBondBatch();
        }

        #endregion
    }
}
