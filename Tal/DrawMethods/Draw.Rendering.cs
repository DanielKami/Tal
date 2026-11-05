// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.Rendering.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.Rendering — per-atom rendering
// ═══════════════════════════════════════════════════════════════════════
//
// (file header unchanged)
//
// ⭐ TIER 1: inline transform in DrawCPKImafterstors (Vector3.Transform → mul)
// ⭐ TIER 2.1: DrawCPKImafterstors iterates _visibleAtomIndices instead of
// the full atomCount loop. Removes per-iteration checks for:
// - `_zDepthCache[i].index < 0`
// - `_molecule.atom[idx].show`
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tal.Cavity;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region DrawSingleAtom — per-atom dispatcher

        private void DrawSingleAtom(
            int orderedZ,
            in AtomRenderCache atomCache,
            Vector3[] atomColors,
            Matrix scaleRotate,
            Matrix cameraRotate,
            Matrix cameraView,
            bool depthCueEnabled,
            float DEPTH_CUE_NEAR,
            float DEPTH_CUE_FAR,
            float DEPTH_CUE_STRENGTH,
            bool isHydrogenOn,
            bool isEndRotation,
            bool isTransparent)
        {
            int style = atomCache.StyleCache[orderedZ];
            float occupancy = atomCache.OccupancyCache[orderedZ];
            bool isSelected = atomCache.SelectedCache[orderedZ];
            int atomicNumber = atomCache.AtomicNumberCache[orderedZ];
            Vector3 position = atomCache.PositionCache[orderedZ];
            Vector3 atomColor = atomColors[orderedZ];

            float ambientIntensity = Flags.Instance.AmbientIntensity;
            float diffuseIntensity = Flags.Instance.DiffuseIntensity;

            float t = 1;
            if (depthCueEnabled && !isSelected)
            {
                Vector3 viewPos = Vector3.Transform(position,
                    Matrix.CreateTranslation(position) * cameraRotate * cameraView);

                float dist = -viewPos.Z;

                t = (dist - DEPTH_CUE_NEAR) / (DEPTH_CUE_FAR - DEPTH_CUE_NEAR);
                t = Math.Clamp(t, 0f, 1f) * DEPTH_CUE_STRENGTH;

                Vector3 bgColor = GetBackgroundColorVector();
                atomColor = Vector3.Lerp(atomColor, bgColor, t);

                t = 1.0f - t / 500;
            }

            Vector3 ambientColor = atomColor * ambientIntensity;
            Vector3 lightDiffuseColor = atomColor * diffuseIntensity;

            UpdateAtomCache(isSelected, atomColor, ambientColor, lightDiffuseColor,
                            occupancy, t, isTransparent);

            if (style == 0)
            {
 // no-op — handled by batch pass
            }

            if (style == 1 || style == 2 || style == 3 || style == 4)
            {
                DrawSticksAtom(orderedZ, style, atomicNumber, position, scaleRotate,
                               atomCache.BondCountCache, atomCache.PrecomputedBondCache,
                               atomCache.BondOccupancyCache, occupancy, isEndRotation,
                               isTransparent);
            }

            if (style == 2 || style == 4)
            {
                DrawBallsAtom(atomicNumber, position, scaleRotate, isEndRotation, isTransparent);
            }

            if (style == 3)
            {
                DrawEllipsoidAtom(orderedZ, atomicNumber, position, scaleRotate, isEndRotation,
                                  atomCache.TransformationCache, atomCache.IsAnisotropicCache,
                                  isTransparent);
            }

            // H-bonds are drawn in a dedicated scene pass (DrawAllHydrogenBonds),
            // same pattern as short contacts — ensures every bond is rendered
            // after neighbour atoms are added, not only those visited in this atom loop.
        }

        #endregion

        #region DrawSticksAtom

        private void DrawSticksAtom(
            int orderedZ,
            int style,
            int atomicNumber,
            Vector3 position,
            Matrix scaleRotate,
            int[] bondCountCache,
            Matrix[][] precomputedBondCache,
            float[][] bondOccupancyCache,
            float occupancy,
            bool isEndRotation,
            bool isTransparent)
        {
            int bondCount = bondCountCache[orderedZ];
            var bondOcc = bondOccupancyCache[orderedZ];

            float stickRadius = Flags.Instance.StickRadius;
            float stickFactor = Flags.Instance.StickRadiusFactor;

            for (int j = 0; j < bondCount; ++j)
            {
                // Hide bond stub when the partner atom is hidden
                // or is a hydrogen while ShowHydrogens is off
                if (_molecule.atom[orderedZ].BondedAtom != null &&
                    j < _molecule.atom[orderedZ].BondedAtom.Length)
                {
                    int partner = _molecule.atom[orderedZ].BondedAtom[j];
                    if (partner >= 0 && partner < _molecule.NumberOfAtoms)
                    {
                        if (!_molecule.atom[partner].show) continue;
                        if (!Flags.Instance.ShowHydrogens &&
                            _molecule.atom[partner].AtomicNumber == 1)
                            continue;
                    }
                }

                float distance = _molecule.atom[orderedZ].Distance[j];
                if (distance <= 0) continue;

                float angleZ = _molecule.atom[orderedZ].AngleZ[j];
                float angleXY = _molecule.atom[orderedZ].AngleXY[j];

                Matrix bondMatrix = Matrix.CreateScale(stickFactor * distance, stickRadius, stickRadius);
                bondMatrix *= Matrix.CreateRotationY(angleZ);
                bondMatrix *= Matrix.CreateRotationZ(angleXY);

                Matrix atomMatrix = bondMatrix;
                atomMatrix *= Matrix.CreateTranslation(position);
                atomMatrix *= scaleRotate;

                // bondOcc = min(occ_i, occ_j) from Calculate_Bonds3D
                float bondOccVal = bondOcc != null && j < bondOcc.Length ? bondOcc[j] : occupancy;
                float bondAlpha = Math.Clamp(bondOccVal, 0.05f, 1.0f);
                bool bondPartial = bondOccVal < 0.999f;

                // Opaque pass: queue full bonds AND partial bonds (from full-occ atoms).
                // Transparent pass: queue only partial bonds (partial-occ atoms).
                // FlushBondBatch(false/true) splits by Alpha so partial sticks from C(100%)
                // toward O(30%) still get C colour + 30% alpha in the transparent pass.
                if (isTransparent && !bondPartial)
                    continue;

                Vector3 bondColor = _effectCache.LightDiffuseColor;
                if (bondColor.LengthSquared() < 1e-8f)
                    bondColor = _effectCache.AmbientLightColor;
                int bondLod = PickMeshLod(ViewSpaceZ(position, scaleRotate));

                // Multi-bond: skip center stick for doubles (two parallel), keep for singles/triples
                bool skipCenter = false;
                if (Flags.Instance.ShowMultipleBonds &&
                    _molecule.atom[orderedZ].BondOrder != null &&
                    j < _molecule.atom[orderedZ].BondOrder.Length &&
                    _molecule.atom[orderedZ].BondOrder[j] == 2)
                    skipCenter = true;

                if (!skipCenter)
                    EnqueueBond(atomMatrix, bondColor, bondAlpha, bondLod);

                // Extra parallel sticks for double/triple (if enabled)
                EnqueueMultipleBondSticks(orderedZ, j, position, scaleRotate,
                    stickRadius, stickFactor, bondAlpha, bondLod, bondColor);
            }
        }

        #endregion

        #region DrawBallsAtom

        private void DrawBallsAtom(
            int atomicNumber,
            Vector3 position,
            Matrix scaleRotate,
            bool isEndRotation,
            bool isTransparent)
        {
            float ballRadius;
            if (atomicNumber == 1)
            {
                float baseRadius = IsMetal(atomicNumber)
                    ? Flags.Instance.BallRadiusMetal
                    : Flags.Instance.BallRadiusNonMetal;
                ballRadius = baseRadius * Flags.Instance.HydrogenBallScale;
            }
            else
            {
                ballRadius = IsMetal(atomicNumber)
                    ? Flags.Instance.BallRadiusMetal
                    : Flags.Instance.BallRadiusNonMetal;
            }

            Matrix atomMatrix = Matrix.CreateScale(ballRadius, ballRadius, ballRadius);
            atomMatrix *= Matrix.CreateTranslation(position);
            atomMatrix *= scaleRotate;

            int lod = PickMeshLod(ViewSpaceZ(position, scaleRotate));
            ModelMesh[] meshes = (_cachedBallMeshesLod != null && _cachedBallMeshesLod[lod] != null)
                ? _cachedBallMeshesLod[lod]
                : (isEndRotation ? _cachedBigBallMeshes : _cachedSmallBallMeshes);
            Matrix[] transforms = (_ballTransformsLod != null && _ballTransformsLod[lod] != null)
                ? _ballTransformsLod[lod]
                : (isEndRotation ? _bigBallTransforms : _smallBallTransforms);

            if (meshes == null || transforms == null)
                return;

            Vector3 ballColor = _effectCache.LightDiffuseColor;
            if (ballColor.LengthSquared() < 1e-8f)
                ballColor = _effectCache.AmbientLightColor;

            float alpha = isTransparent ? Math.Max(_effectCache.Alpha, 0.05f) : 1f;
            // atomMatrix only — bone transform applied in FlushSphereBatch
            EnqueueSphere(atomMatrix, ballColor, alpha, lod);
        }

        #endregion

        #region DrawEllipsoidAtom

        private void DrawEllipsoidAtom(
            int orderedZ,
            int atomicNumber,
            Vector3 position,
            Matrix scaleRotate,
            bool isEndRotation,
            Matrix[] transformationCache,
            bool[] isAnisotropicCache,
            bool isTransparent)
        {
            float probability = Flags.Instance.ATP_Probability;
            ref var ellipsoidData = ref _ellipsoidCache[orderedZ];

            bool showHydrogenAsSphere = ellipsoidData.IsHydrogen
                                        && !Flags.Instance.ShowHydrogenIsoSpheres;

            int lod = PickMeshLod(ViewSpaceZ(position, scaleRotate));
            ModelMesh[] meshes = (_cachedBallMeshesLod != null && _cachedBallMeshesLod[lod] != null)
                ? _cachedBallMeshesLod[lod]
                : (isEndRotation ? _cachedBigBallMeshes : _cachedSmallBallMeshes);
            Matrix[] transforms = (_ballTransformsLod != null && _ballTransformsLod[lod] != null)
                ? _ballTransformsLod[lod]
                : (isEndRotation ? _bigBallTransforms : _smallBallTransforms);

            Matrix atomMatrix;
            Matrix scaleRotateTranslation = Matrix.CreateTranslation(position) * scaleRotate;

            if (showHydrogenAsSphere)
            {
                float baseRadius = IsMetal(atomicNumber)
                    ? Flags.Instance.BallRadiusMetal
                    : Flags.Instance.BallRadiusNonMetal;
                float ballRadius = baseRadius * Flags.Instance.HydrogenBallScale;

                atomMatrix = Matrix.CreateScale(ballRadius, ballRadius, ballRadius);
                atomMatrix *= Matrix.CreateTranslation(position);
                atomMatrix *= scaleRotate;
            }
            else
            {
                Matrix scaleMatrix = Matrix.CreateScale(probability, probability, probability);
                Matrix scaledUn = ellipsoidData.UnOriginal * scaleMatrix;

                atomMatrix = Matrix.CreateScale(1, 1, 1);
                atomMatrix *= transformationCache[orderedZ];
                atomMatrix *= scaledUn;
                atomMatrix *= scaleRotateTranslation;
            }

            if (meshes != null && transforms != null)
            {
                Vector3 ellColor = _effectCache.LightDiffuseColor;
                if (ellColor.LengthSquared() < 1e-8f)
                    ellColor = _effectCache.AmbientLightColor;
                float alpha = isTransparent ? Math.Max(_effectCache.Alpha, 0.05f) : 1f;
                EnqueueSphere(atomMatrix, ellColor, alpha, lod);
            }

            if (ellipsoidData.IsEllipsoid
                && isAnisotropicCache[orderedZ]
                && !ellipsoidData.IsHydrogen
                && Flags.Instance.EllipsoidDisksEnabled)
            {
                DrawEllipsoidDisks(orderedZ, isEndRotation, meshes, transforms,
                                   transformationCache, scaleRotateTranslation, isTransparent);
            }
        }

        #endregion

        #region DrawEllipsoidDisks

        private void DrawEllipsoidDisks(
            int orderedZ,
            bool isEndRotation,
            ModelMesh[] meshes,
            Matrix[] transforms,
            Matrix[] transformationCache,
            Matrix scaleRotateTranslation,
            bool isTransparent)
        {
            Vector3 origDiffuse = _effectCache.DiffuseColor;
            Vector3 origAmbient = _effectCache.AmbientLightColor;
            Vector3 origLightDiffuse = _effectCache.LightDiffuseColor;
            Vector3 origSpecular = _effectCache.SpecularColor;
            Vector3 origLightSpecular = _effectCache.LightSpecularColor;

            _effectCache.DiffuseColor = Vector3.Zero;
            _effectCache.AmbientLightColor = Vector3.Zero;
            _effectCache.LightDiffuseColor = Vector3.Zero;
            _effectCache.SpecularColor = Vector3.Zero;
            _effectCache.LightSpecularColor = Vector3.Zero;

            float probability = Flags.Instance.ATP_Probability;
            float diskThickness = Flags.Instance.EllipsoidDiskThickness;
            float baseRadius = Flags.Instance.EllipsoidBaseRadius;

            ref var ellipsoidData = ref _ellipsoidCache[orderedZ];

            Matrix shapeMatrix = transformationCache[orderedZ]
 * ellipsoidData.UnOriginal
 * Matrix.CreateScale(probability, probability, probability);

            Vector3 axisX = new Vector3(shapeMatrix.M11, shapeMatrix.M12, shapeMatrix.M13);
            Vector3 axisY = new Vector3(shapeMatrix.M21, shapeMatrix.M22, shapeMatrix.M23);
            Vector3 axisZ = new Vector3(shapeMatrix.M31, shapeMatrix.M32, shapeMatrix.M33);

            float lenX = axisX.Length();
            float lenY = axisY.Length();
            float lenZ = axisZ.Length();

            const float R_MODEL = RuntimeIcosphere.BASE_RADIUS;

            for (int elips = 0; elips < 3; elips++)
            {
                float sx, sy, sz;

                if (elips == 0)
                {
                    sx = lenX > 1e-6f ? diskThickness / (lenX * R_MODEL) : baseRadius;
                    sy = baseRadius;
                    sz = baseRadius;
                }
                else if (elips == 1)
                {
                    sx = baseRadius;
                    sy = lenY > 1e-6f ? diskThickness / (lenY * R_MODEL) : baseRadius;
                    sz = baseRadius;
                }
                else
                {
                    sx = baseRadius;
                    sy = baseRadius;
                    sz = lenZ > 1e-6f ? diskThickness / (lenZ * R_MODEL) : baseRadius;
                }

                Matrix diskScale = Matrix.CreateScale(sx, sy, sz);
                Matrix atomElipse = diskScale * shapeMatrix * scaleRotateTranslation;

                foreach (ModelMesh mesh in meshes)
                {
                    Matrix world = transforms[mesh.ParentBone.Index] * atomElipse;
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        ApplyPerAtomEffect(effect, world, _effectCache);
                    }
                    mesh.Draw();
                }
            }

            _effectCache.DiffuseColor = origDiffuse;
            _effectCache.AmbientLightColor = origAmbient;
            _effectCache.LightDiffuseColor = origLightDiffuse;
            _effectCache.SpecularColor = origSpecular;
            _effectCache.LightSpecularColor = origLightSpecular;
        }

        #endregion

        #region DrawCPKAtom — legacy

        private void DrawCPKAtom(int atomicNumber, Vector3 position, Matrix scaleRotate, bool isTransparent)
        {
            float size = _molecule.GetVdwRadius(atomicNumber) / RuntimeIcosphere.BASE_RADIUS;
            Matrix atomMatrix = Matrix.CreateScale(size, size, size);
            atomMatrix *= Matrix.CreateTranslation(position);
            atomMatrix *= scaleRotate;

            foreach (ModelMesh mesh in _cachedBigBallMeshes)
            {
                Matrix world = _bigBallTransforms[mesh.ParentBone.Index] * atomMatrix;
                foreach (BasicEffect effect in mesh.Effects)
                {
                    if (isTransparent)
                        ApplyPerAtomEffectTransparent(effect, world, _effectCache);
                    else
                        ApplyPerAtomEffect(effect, world, _effectCache);
                }
                mesh.Draw();
            }
        }

        #endregion

        #region DrawCPKImpostors — ⭐ TIER 1 + TIER 2.1

 /// <summary>
 /// Draws all visible CPK atoms as screen-space quad imafterstors.
 ///
 /// ⭐ TIER 2.1: iterates _visibleAtomIndices[0.._visibleAtomCount)
 /// instead of the full atomCount range. Removes per-iteration
 /// checks for `_zDepthCache[i].index < 0` and `atom.show`.
 ///
 /// ⭐ TIER 1: inline matrix multiply for the aftersition transform.
 /// </summary>
        private void DrawCPKImpostors(
            in AtomRenderCache atomCache,
            Vector3[] atomColors,
            Matrix scaleRotate,
            bool isTransparent,
            bool depthCueEnabled,
            float depthCueNear,
            float depthCueFar,
            float depthCueStrength)
        {
            if (_sphereImpostorEffect == null || _sphereVerts == null || _molecule == null || _camera == null)
                return;

            const float ImpostorZoomBoost = 1.0f;
            float s = _camera.OneOverZoom * ImpostorZoomBoost;

            int count = 0;
            int visibleCount = _visibleAtomCount;
            var visibleIndices = _visibleAtomIndices;

            Matrix rotTrans = Matrix.CreateScale(s, s, s)
 * _camera.Rotate
 * _camera.Translate;

            Vector3 bgColor = GetBackgroundColorVector();
            float cueNear = depthCueNear * s;
            float cueFar = depthCueFar * s;
            float cueInvSpan = 1.0f / Math.Max(cueFar - cueNear, 1e-6f);

 // ⭐ TIER 1: extract matrix elements once
            float m11 = rotTrans.M11, m12 = rotTrans.M12, m13 = rotTrans.M13;
            float m21 = rotTrans.M21, m22 = rotTrans.M22, m23 = rotTrans.M23;
            float m31 = rotTrans.M31, m32 = rotTrans.M32, m33 = rotTrans.M33;
            float m41 = rotTrans.M41, m42 = rotTrans.M42, m43 = rotTrans.M43;

 // ⭐ TIER 2.1: iterate only visible atoms (in render order)
            for (int i = 0; i < visibleCount; i++)
            {
                int idx = visibleIndices[i];

                if (atomCache.StyleCache[idx] != 0) continue;

                float occ = atomCache.OccupancyCache[idx];
                if (isTransparent ? occ >= 0.999f : occ < 0.999f)
                    continue;

 // ⭐ TIER 1: inline Vector3.Transform(atomCache.AftersitionCache[idx], rotTrans)
                Vector3 posIn = atomCache.PositionCache[idx];
                Vector3 pos = new Vector3(
                    posIn.X * m11 + posIn.Y * m21 + posIn.Z * m31 + m41,
                    posIn.X * m12 + posIn.Y * m22 + posIn.Z * m32 + m42,
                    posIn.X * m13 + posIn.Y * m23 + posIn.Z * m33 + m43);

                float radius = _molecule.GetVdwRadius(atomCache.AtomicNumberCache[idx]) * s;

                Vector3 color = atomColors[idx];
                bool isSelected = atomCache.SelectedCache[idx];
                if (isSelected)
                    color = SelectedDiffuse;

                const float ImpostorDepthCueBoost = 1.5f;

                if (depthCueEnabled && !isSelected)
                {
                    float dist = -pos.Z;

                    float cue = (dist - cueNear) * cueInvSpan;
                    cue = Math.Clamp(cue, 0f, 1f) * depthCueStrength * ImpostorDepthCueBoost;

                    if (cue > 1f) cue = 1f;

                    color = Vector3.Lerp(color, bgColor, cue);
                }

                AddSphereQuad(ref count, pos, radius, color);
            }

            if (count == 0) return;

            Matrix view = _camera.View;
            Matrix proj = _camera.Projection;

            Matrix viewProjection = view * proj;
            Matrix inverseViewProjection = Matrix.Invert(viewProjection);

            _sphereImpostorEffect.Parameters["View"].SetValue(view);
            _sphereImpostorEffect.Parameters["Projection"].SetValue(proj);
            _sphereImpostorEffect.Parameters["ViewProjection"].SetValue(viewProjection);
            _sphereImpostorEffect.Parameters["InverseViewProjection"].SetValue(inverseViewProjection);
            _sphereImpostorEffect.Parameters["CameraPosition"].SetValue(_camera.g_cameraPos);

            bool light1Enabled = Flags.Instance.Enabled;
            _sphereImpostorEffect.Parameters["Light1Enabled"].SetValue(light1Enabled ? 1f : 0f);
            _sphereImpostorEffect.Parameters["LightDirection"].SetValue(
                Vector3.Normalize(Flags.Instance.LightDirection));
            _sphereImpostorEffect.Parameters["LightDiffuseColor"].SetValue(
                Vector3.One * Flags.Instance.DiffuseIntensity);
            _sphereImpostorEffect.Parameters["LightSpecularColor"].SetValue(
                Flags.Instance.SpecularColor);

            bool light2Enabled = Flags.Instance.Light2Enabled;
            _sphereImpostorEffect.Parameters["Light2Enabled"].SetValue(light2Enabled ? 1f : 0f);
            _sphereImpostorEffect.Parameters["Light2Direction"].SetValue(
                Vector3.Normalize(Flags.Instance.Light2Direction));
            _sphereImpostorEffect.Parameters["Light2DiffuseColor"].SetValue(
                _effectCache.Light2DiffuseColor);
            _sphereImpostorEffect.Parameters["Light2SpecularColor"].SetValue(
                Flags.Instance.Light2SpecularColor);

            _sphereImpostorEffect.Parameters["AmbientColor"].SetValue(
                Vector3.One * Flags.Instance.AmbientIntensity);
            _sphereImpostorEffect.Parameters["SpecularColor"].SetValue(
                new Vector3(0.25f));
            _sphereImpostorEffect.Parameters["SpecularPower"].SetValue(_effectCache.SpecularPower);
            _sphereImpostorEffect.Parameters["Alpha"].SetValue(isTransparent ? 0.55f : 1f);

            var savedBlend = _graphicsDevice.BlendState;
            var savedDepth = _graphicsDevice.DepthStencilState;
            var savedRaster = _graphicsDevice.RasterizerState;

            _graphicsDevice.BlendState = isTransparent ? BlendState.AlphaBlend : BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.RasterizerState = GetCullNoneMSAA();

            foreach (EffectPass pass in _sphereImpostorEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserPrimitives(
                    PrimitiveType.TriangleList,
                    _sphereVerts,
                    0,
                    count / 3);
            }

            _graphicsDevice.BlendState = savedBlend;
            _graphicsDevice.DepthStencilState = savedDepth;
            _graphicsDevice.RasterizerState = savedRaster;
        }

        #endregion

        #region AddSphereQuad

        private void AddSphereQuad(ref int count, Vector3 center, float radius, Vector3 color)
        {
            if (count + 6 > _sphereVerts.Length)
                return;

            _sphereVerts[count++] = new SphereImpostorVertex
            {
                Center = center,
                Corner = new Vector2(-1f, -1f),
                Radius = radius,
                Color = color
            };
            _sphereVerts[count++] = new SphereImpostorVertex
            {
                Center = center,
                Corner = new Vector2(-1f, 1f),
                Radius = radius,
                Color = color
            };
            _sphereVerts[count++] = new SphereImpostorVertex
            {
                Center = center,
                Corner = new Vector2(1f, -1f),
                Radius = radius,
                Color = color
            };

            _sphereVerts[count++] = new SphereImpostorVertex
            {
                Center = center,
                Corner = new Vector2(1f, -1f),
                Radius = radius,
                Color = color
            };
            _sphereVerts[count++] = new SphereImpostorVertex
            {
                Center = center,
                Corner = new Vector2(-1f, 1f),
                Radius = radius,
                Color = color
            };
            _sphereVerts[count++] = new SphereImpostorVertex
            {
                Center = center,
                Corner = new Vector2(1f, 1f),
                Radius = radius,
                Color = color
            };
        }

        #endregion
    }
}