// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.Cache.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.Cache — per-frame cache + effect state preparation
// ═══════════════════════════════════════════════════════════════════════
//
// This file contains cache preparation for the main render loop:
//
// PrepareAtomColors() — atom color array (depends on ColorMode flag)
// PrepareAtomRenderCache() — snapshot of Molecule.Atom fields into arrays
// BuildEllipsoidCache() — per-atom ellipsoid matrices for style 3
// UpdateSceneCache() — snapshot of Flags into _effectCache
// ApplyGlobalEffectState() — apply _effectCache to all 3 template effects
// ApplyGlobalEffectStateTo() — apply to one BasicEffect
// UpdateAtomCache() — set per-atom colors in _effectCache
// ApplyPerAtomEffect() — apply _effectCache + world to BasicEffect
// ApplyPerAtomEffectTransparent() — same, but with specular disabled
//
// ⚠️ HOT PATH: PrepareAtomColors, PrepareAtomRenderCache, UpdateAtomCache
// and ApplyPerAtomEffect* are called MANY times per frame.
// Keep them allocation-free and branch-light.
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tal.Cavity;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region PrepareAtomColors — per-frame color array

 /// <summary>
 /// Builds the per-atom color array for the current frame.
 ///
 /// Behavior depends on <see cref="Flags.ColorMode"/>:
 /// 0 — CPK (VdWParameters.color by AtomicNumber) ← fastest path
 /// 1 — symmetry equivalent color
 /// 2 — molecule color
 /// 3 — symmetry × molecule
 /// 4 — custom color (fallback to CPK if unset) ← fast path
 /// 5 — charge (ColorManager)
 /// 6 — hydrophobicity (ColorManager)
 /// other — CPK fallback
 ///
 /// Reuses <c>_atomColorsCache</c> — reallocated only when atom count changes.
 /// </summary>
        private Vector3[] PrepareAtomColors(int atomCount)
        {
            if (_atomColorsCache == null || _atomColorsCache.Length != atomCount)
                _atomColorsCache = new Vector3[atomCount];

            int mode = Flags.Instance.ColorMode;

 // ── Fast path: CPK ──
            if (mode == 0)
            {
                for (int i = 0; i < atomCount; i++)
                {
                    int atomicNumber = _molecule.atom[i].AtomicNumber;
                    _atomColorsCache[i] = VdwParameters.color[atomicNumber];
                }
                return _atomColorsCache;
            }

 // ── Fast path: custom color (with CPK fallback) ──
            if (mode == 4)
            {
                for (int i = 0; i < atomCount; i++)
                {
                    var atom = _molecule.atom[i];
                    if (atom.CustomColor.X >= 0f && atom.CustomColor.Y >= 0f && atom.CustomColor.Z >= 0f)
                        _atomColorsCache[i] = atom.CustomColor;
                    else
                        _atomColorsCache[i] = VdwParameters.color[atom.AtomicNumber];
                }
                return _atomColorsCache;
            }

 // ── Slow path: per-atom color (symmetry/molecule/property) ──
            for (int i = 0; i < atomCount; i++)
            {
                _atomColorsCache[i] = GetAtomColor(i);
            }
            return _atomColorsCache;
        }

        #endregion

        #region PrepareAtomRenderCache — per-frame snapshot of atom fields

 /// <summary>
 /// Copies all atom fields used by the render loop into flat arrays.
 ///
 /// Purafterse: avoid repeated <c>_molecule.atom[i].X</c> dereferences
 /// inside the hot per-atom loop (struct copy + cache locality).
 ///
 /// Cache is reallocated ONLY when atom count changes.
 /// </summary>
        private AtomRenderCache PrepareAtomRenderCache(int atomCount)
        {
            if (_atomRenderCacheCount != atomCount)
            {
                _atomRenderCache = new AtomRenderCache
                {
                    StyleCache = new int[atomCount],
                    OccupancyCache = new float[atomCount],
                    SelectedCache = new bool[atomCount],
                    AtomicNumberCache = new int[atomCount],
                    PositionCache = new Vector3[atomCount],
                    BondCountCache = new int[atomCount],
                    PrecomputedBondCache = new Matrix[atomCount][],
                    BondOccupancyCache = new float[atomCount][],
                    HBondCountCache = new int[atomCount],
                    PrecomputedHBondCache = new Matrix[atomCount][],
                    TransformationCache = new Matrix[atomCount],
                    UnCache = new Matrix[atomCount],
                    IsAnisotropicCache = new bool[atomCount]
                };
                _atomRenderCacheCount = atomCount;
            }

            var cache = _atomRenderCache;
            for (int i = 0; i < atomCount; i++)
            {
                ref var atom = ref _molecule.atom[i];
                cache.StyleCache[i] = atom.style;
                cache.OccupancyCache[i] = atom.occupancy;
                cache.SelectedCache[i] = atom.selected;
                cache.AtomicNumberCache[i] = atom.AtomicNumber;
                cache.PositionCache[i] = atom.Coordinate;
                cache.BondCountCache[i] = atom.NrBondedAtoms;
                cache.PrecomputedBondCache[i] = atom.PrecomputedBondMatrix;
                cache.BondOccupancyCache[i] = atom.BondOccupancy;
                cache.HBondCountCache[i] = atom.H_NrBondedAtoms;
                cache.PrecomputedHBondCache[i] = atom.PrecomputedHBondMatrix;
                cache.TransformationCache[i] = atom.Transformation;
                cache.UnCache[i] = atom.Un;
                cache.IsAnisotropicCache[i] = atom.isAnisotropic;
            }

            return cache;
        }

        #endregion

        #region BuildEllipsoidCache — per-atom ellipsoid matrices

 /// <summary>
 /// Builds per-atom ellipsoid matrices for the ATP style (style 3).
 ///
 /// For each atom:
 /// - If style != 3: identity, no ellipsoid matrices.
 /// - If style == 3: store UnOriginal + AtomMatrixBase = Transformation * Un.
 /// - If anisotropic + not hydrogen + disks enabled:
 /// allocate 3 disk matrices (X, Y, Z cross-sections).
 ///
 /// Disk matrices create the classic ORTEP-style ellipsoid slices.
 /// </summary>
        private void BuildEllipsoidCache(
            int atomCount,
            int[] styleCache,
            int[] atomicNumberCache,
            Matrix[] transformationCache,
            Matrix[] unCache,
            bool[] isAnisotropicCache)
        {
            float diskThickness = Flags.Instance.EllipsoidDiskThickness;
            float baseRadius = Flags.Instance.EllipsoidBaseRadius;
            bool disksEnabled = Flags.Instance.EllipsoidDisksEnabled;

            if (_ellipsoidCache == null || _ellipsoidCache.Length != atomCount)
                _ellipsoidCache = new EllipsoidCacheData[atomCount];

            for (int i = 0; i < atomCount; i++)
            {
                ref var data = ref _ellipsoidCache[i];

                data.IsHydrogen = atomicNumberCache[i] == 1;
                data.Size = 1f;

                if (styleCache[i] == 3)
                {
                    data.UnOriginal = unCache[i];
                    data.AtomMatrixBase = transformationCache[i] * unCache[i];
                }
                else
                {
                    data.AtomMatrixBase = Matrix.Identity;
                    data.UnOriginal = Matrix.Identity;
                }

                data.IsEllipsoid = styleCache[i] == 3;
                data.SizeElips = baseRadius;
                data.SizeLow = diskThickness;

 // ⚠️ Disk matrices allocated only when actually drawn — avoids
 // allocation for every non-ellipsoid atom in the scene.
                if (data.IsEllipsoid && isAnisotropicCache[i] && !data.IsHydrogen && disksEnabled)
                {
                    if (data.EllipsoidMatrices == null)
                        data.EllipsoidMatrices = new Matrix[3];

                    float radius = baseRadius * data.Size;
                    data.EllipsoidMatrices[0] = Matrix.CreateScale(diskThickness, radius, radius);
                    data.EllipsoidMatrices[1] = Matrix.CreateScale(radius, diskThickness, radius);
                    data.EllipsoidMatrices[2] = Matrix.CreateScale(radius, radius, diskThickness);
                }
                else
                {
                    data.EllipsoidMatrices = null;
                }
            }
        }

        #endregion

        #region UpdateSceneCache — snapshot of Flags

 /// <summary>
 /// Snapshots all lighting / camera flags into <see cref="_effectCache"/>.
 ///
 /// Called once per frame BEFORE any atom is drawn.
 /// All per-atom draws then read from _effectCache (no Flags.Instance access).
 /// </summary>
        private void UpdateSceneCache(Matrix view, Matrix projection)
        {
            _effectCache.PreferPerPixelLighting = Flags.Instance.PerPixelLighting;
            _effectCache.LightingEnabled = Flags.Instance.Enabled;
            _effectCache.SpecularColor = Flags.Instance.SpecularColor;
            _effectCache.SpecularPower = Flags.Instance.SpecularPower;

            _effectCache.LightDirection = Flags.Instance.LightDirection;
            _effectCache.LightSpecularColor = Flags.Instance.SpecularColor;
            _effectCache.LightDiffuseColor = new Vector3(Flags.Instance.DiffuseIntensity);
            _effectCache.AmbientLightColor = new Vector3(Flags.Instance.AmbientIntensity);

            _effectCache.Light2Direction = Flags.Instance.Light2Direction;
            _effectCache.Light2SpecularColor = Flags.Instance.Light2SpecularColor;

 // Light 2 diffuse combines diffuse intensity + ambient when enabled.
 // This is intentional — matches the original Draw.cs behavior.
            float light2Diffuse = Flags.Instance.Light2DiffuseIntensity
                + (Flags.Instance.Light2Enabled ? Flags.Instance.Light2AmbientIntensity : 0f);
            _effectCache.Light2DiffuseColor = new Vector3(light2Diffuse);

            _effectCache.View = view;
            _effectCache.Projection = projection;
            _effectCache.IsValid = true;
            _cacheNeedsUpdate = false;
        }

        #endregion

        #region ApplyGlobalEffectState — apply _effectCache to template effects

 /// <summary>
 /// Applies the global scene state to all template effects.
 ///
 /// Template effects are cached in _rodEffect, _bigBallEffect, _smallBallEffect
 /// — they were extracted from the first mesh of each model.
 ///
 /// After this call, per-atom draws only update World, DiffuseColor, Alpha, etc.
 /// View, Projection, lighting and per-pixel settings stay as set here.
 /// </summary>
        private void ApplyGlobalEffectState()
        {
            ApplyGlobalEffectStateTo(_rodEffect);
            ApplyGlobalEffectStateTo(_bigBallEffect);
            ApplyGlobalEffectStateTo(_smallBallEffect);
        }

        private void ApplyGlobalEffectStateTo(BasicEffect effect)
        {
            if (effect == null) return;

            effect.View = _effectCache.View;
            effect.Projection = _effectCache.Projection;

            effect.PreferPerPixelLighting = _effectCache.PreferPerPixelLighting;
            effect.LightingEnabled = _effectCache.LightingEnabled;
            effect.SpecularPower = _effectCache.SpecularPower;
            effect.VertexColorEnabled = false;

            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight0.Direction = _effectCache.LightDirection;

            effect.DirectionalLight1.Enabled = Flags.Instance.Light2Enabled;
            effect.DirectionalLight1.Direction = _effectCache.Light2Direction;
            effect.DirectionalLight1.DiffuseColor = _effectCache.Light2DiffuseColor;
            effect.DirectionalLight1.SpecularColor = _effectCache.Light2SpecularColor;
        }

        #endregion

        #region UpdateAtomCache — set per-atom colors in _effectCache

 /// <summary>
 /// Updates <see cref="_effectCache"/> with per-atom state:
 /// diffuse, ambient, specular, alpha.
 ///
 /// Called before each atom's draw to set up its material.
 /// The actual BasicEffect application happens in <see cref="ApplyPerAtomEffect"/>
 /// or <see cref="ApplyPerAtomEffectTransparent"/>.
 ///
 /// Handles three cases:
 /// 1. Selected atom — yellow diffuse, normal lighting
 /// 2. Normal atom — diffuse from atomColor, normal lighting
 /// 3. Transparent — brighter ambient, no specular (avoids artifacts)
 /// </summary>
        private void UpdateAtomCache(
            bool isSelected,
            Vector3 diffuseColor,
            Vector3 ambientColor,
            Vector3 lightDiffuseColor,
            float alpha,
            float lightSpecularFactor = 1.0f,
            bool isTransparent = false)
        {
            if (isSelected)
            {
                _effectCache.DiffuseColor = SelectedDiffuse;
                _effectCache.AmbientLightColor = Vector3.One * Flags.Instance.AmbientIntensity;
                _effectCache.LightDiffuseColor = SelectedDiffuse * Flags.Instance.DiffuseIntensity;
            }
            else
            {
                _effectCache.DiffuseColor = diffuseColor;
                _effectCache.AmbientLightColor = Vector3.One * Flags.Instance.AmbientIntensity;
                _effectCache.LightDiffuseColor = lightDiffuseColor;
            }
            _effectCache.Alpha = alpha;

            if (isTransparent)
            {
 // Transparent: double ambient to compensate for loss of specular,
 // zero specular to avoid bright artifacts through semi-transparent geometry.
                _effectCache.AmbientLightColor = Vector3.One * Flags.Instance.AmbientIntensity * 2.0f;
                _effectCache.SpecularColor = Vector3.Zero;
                _effectCache.LightSpecularColor = Vector3.Zero;
            }
            else
            {
                _effectCache.SpecularColor = Flags.Instance.SpecularColor;
                _effectCache.LightSpecularColor = Flags.Instance.SpecularColor * lightSpecularFactor;
            }
        }

        #endregion

        #region ApplyPerAtomEffect — apply per-atom state to BasicEffect

 /// <summary>
 /// Applies the per-atom state to a BasicEffect. Hot path — called once
 /// per mesh per atom per frame.
 ///
 /// Reads only from the passed-in <paramref name="cache"/> — no Flags access.
 /// </summary>
        private static void ApplyPerAtomEffect(BasicEffect effect, Matrix world, in EffectCache cache)
        {
            effect.World = world;
            effect.DiffuseColor = cache.DiffuseColor;
            effect.AmbientLightColor = cache.AmbientLightColor;
            effect.Alpha = cache.Alpha;
            effect.SpecularColor = cache.SpecularColor;
            effect.DirectionalLight0.DiffuseColor = cache.LightDiffuseColor;
            effect.DirectionalLight0.SpecularColor = cache.LightSpecularColor;
        }

 /// <summary>
 /// Same as <see cref="ApplyPerAtomEffect"/> but with specular zeroed out.
 /// Used for transparent atoms and bond rendering (avoids highlight artifacts).
 /// </summary>
        private static void ApplyPerAtomEffectTransparent(BasicEffect effect, Matrix world, in EffectCache cache)
        {
            effect.World = world;
            effect.DiffuseColor = cache.DiffuseColor;
            effect.AmbientLightColor = cache.AmbientLightColor;
            effect.Alpha = cache.Alpha;

            effect.SpecularColor = Vector3.Zero;
            effect.DirectionalLight0.SpecularColor = Vector3.Zero;
            effect.DirectionalLight1.SpecularColor = Vector3.Zero;

            effect.DirectionalLight0.DiffuseColor = cache.LightDiffuseColor;
            effect.DirectionalLight1.DiffuseColor = cache.Light2DiffuseColor;
        }

        #endregion
    }
}