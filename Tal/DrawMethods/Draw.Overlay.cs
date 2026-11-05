// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.Overlay.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.Overlay — overlay atoms (other molecules alongside the main one)
// ═══════════════════════════════════════════════════════════════════════
//
// (file header unchanged)
//
// ⭐ TIER 2.2 OPTIMIZATION APPLIED:
// Overlay grouping is now cached in _cachedOverlayGroups and rebuilt
// only when _overlayGroupingDirty is true (set by SetOverlayAtoms and
// RefreshOverlayCache). Eliminates the per-frame ToLookup allocation
// (~2-5 KB per frame for typical overlay sizes).
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region DrawOverlayAtoms — main overlay entry point

 /// <summary>
 /// Draws all overlay atoms grouped by (color, style).
 ///
 /// ⭐ TIER 2.2: uses cached _cachedOverlayGroups. The cache is
 /// rebuilt only when _overlayGroupingDirty is set — usually
 /// once per SetOverlayAtoms, not every frame.
 /// </summary>
        private void DrawOverlayAtoms(Matrix scaleRotate)
        {
            if (_overlayAtoms.Count == 0) return;

 // ═══ Ensure models are loaded ═══
            if (_modelsNeedInit)
            {
                InitializeCache();
                _modelsNeedInit = false;
            }

            if (_cachedSmallBallMeshes == null || _cachedSmallBallMeshes.Length == 0 ||
                _cachedSmallRodMeshes == null || _cachedSmallRodMeshes.Length == 0 ||
                _cachedBigBallMeshes == null || _cachedBigBallMeshes.Length == 0)
            {
                InitializeCache();
                if (_cachedSmallBallMeshes == null || _cachedSmallBallMeshes.Length == 0)
                    return;
            }

 // ═══ Rebuild overlay groups if dirty ═══
            if (_overlayGroupingDirty || _cachedOverlayGroups == null)
            {
                RebuildOverlayGroups();
                _overlayGroupingDirty = false;
            }

            ApplyGlobalEffectState();

 // ═══ Save current material ═══
            var origDiffuse = _effectCache.DiffuseColor;
            var origAmbient = _effectCache.AmbientLightColor;
            var origLightDiffuse = _effectCache.LightDiffuseColor;
            var origSpecular = _effectCache.SpecularColor;
            var origLightSpecular = _effectCache.LightSpecularColor;
            var origAlpha = _effectCache.Alpha;

 // ═══ Draw each cached group ═══
            foreach (var group in _cachedOverlayGroups)
            {
                Vector3 color = group.Color;
                int style = group.Style;
                var atoms = group.Atoms;

 // ── Set material for the whole group ──
                _effectCache.DiffuseColor = color;
                _effectCache.AmbientLightColor = Vector3.One * 0.5f;
                _effectCache.LightDiffuseColor = color;
                _effectCache.SpecularColor = new Vector3(0.5f, 0.5f, 0.5f);
                _effectCache.LightSpecularColor = new Vector3(0.5f, 0.5f, 0.5f);
                _effectCache.Alpha = 1.0f;

 // ── Dispatch by style ──
                switch (style)
                {
                    case 0: DrawOverlayCPK(atoms, scaleRotate); break;
                    case 1: DrawOverlaySticksOnly(atoms, scaleRotate); break;
                    case 2: DrawOverlayBallsAndSticks(atoms, scaleRotate); break;
                    case 3: DrawOverlayEllipsoids(atoms, scaleRotate); break;
                    default: DrawOverlayBallsOnly(atoms, scaleRotate); break;
                }
            }

 // ═══ Restore material ═══
            _effectCache.DiffuseColor = origDiffuse;
            _effectCache.AmbientLightColor = origAmbient;
            _effectCache.LightDiffuseColor = origLightDiffuse;
            _effectCache.SpecularColor = origSpecular;
            _effectCache.LightSpecularColor = origLightSpecular;
            _effectCache.Alpha = origAlpha;
        }

        #endregion

        #region RebuildOverlayGroups — ⭐ TIER 2.2

 /// <summary>
 /// Rebuilds the cached overlay group list from _overlayAtoms.
 ///
 /// Called only when _overlayGroupingDirty is true (i.e. when
 /// SetOverlayAtoms or RefreshOverlayCache was called).
 ///
 /// Groups are keyed by (CustomColor, style). Custom colors are
 /// resolved here — if CustomColor is unset (-1), the CPK color
 /// of the first atom in the group is used.
 /// </summary>
        private void RebuildOverlayGroups()
        {
            _cachedOverlayGroups = new List<OverlayGroup>();

            if (_overlayAtoms == null || _overlayAtoms.Count == 0)
                return;

 // ═══ Group by (color, style) via ToLookup — single pass ═══
            var lookup = _overlayAtoms
                .ToLookup(a => (Color: a.CustomColor, Style: a.style));

            foreach (var group in lookup)
            {
                Vector3 color = group.Key.Color;
                int style = group.Key.Style;

 // ═══ Fallback: unset custom color → CPK color of first atom ═══
                if (color.X < 0 || color.Y < 0 || color.Z < 0)
                {
                    var firstAtom = group.First();
                    if (firstAtom.AtomicNumber >= 0 && firstAtom.AtomicNumber < VdwParameters.color.Length)
                        color = VdwParameters.color[firstAtom.AtomicNumber];
                    else
                        color = new Vector3(0.6f, 0.6f, 0.6f);
                }

 // ═══ Materialize the atoms list once — reused every frame ═══
                var atomsList = new List<Molecule.Atom>(group);

                _cachedOverlayGroups.Add(new OverlayGroup
                {
                    Color = color,
                    Style = style,
                    Atoms = atomsList
                });
            }

            System.Diagnostics.Debug.WriteLine(
                $"[Draw] RebuildOverlayGroups: {_cachedOverlayGroups.Count} groups from {_overlayAtoms.Count} atoms");
        }

        #endregion

        #region DrawOverlayBallsOnly

        private void DrawOverlayBallsOnly(List<Molecule.Atom> atoms, Matrix scaleRotate)
        {
            const float ballRadius = 0.15f;

            foreach (var atom in atoms)
            {
                Matrix atomMatrix = Matrix.CreateScale(ballRadius, ballRadius, ballRadius);
                atomMatrix *= Matrix.CreateTranslation(atom.Coordinate);
                atomMatrix *= scaleRotate;

                foreach (ModelMesh mesh in _cachedSmallBallMeshes)
                {
                    Matrix world = _smallBallTransforms[mesh.ParentBone.Index] * atomMatrix;
                    foreach (BasicEffect effect in mesh.Effects)
                        ApplyPerAtomEffect(effect, world, _effectCache);
                    mesh.Draw();
                }
            }
        }

        #endregion

        #region DrawOverlayCPK

        private void DrawOverlayCPK(List<Molecule.Atom> atoms, Matrix scaleRotate)
        {
            foreach (var atom in atoms)
            {
                float radius = VdwParameters.GetRadius(atom.AtomicNumber) / RuntimeIcosphere.BASE_RADIUS;
                if (radius < 0.05f) radius = 0.05f;
                if (radius > 1.0f) radius = 1.0f;

                Matrix atomMatrix = Matrix.CreateScale(radius, radius, radius);
                atomMatrix *= Matrix.CreateTranslation(atom.Coordinate);
                atomMatrix *= scaleRotate;

                foreach (ModelMesh mesh in _cachedBigBallMeshes)
                {
                    Matrix world = _bigBallTransforms[mesh.ParentBone.Index] * atomMatrix;
                    foreach (BasicEffect effect in mesh.Effects)
                        ApplyPerAtomEffect(effect, world, _effectCache);
                    mesh.Draw();
                }
            }
        }

        #endregion

        #region DrawOverlaySticksOnly

        private void DrawOverlaySticksOnly(List<Molecule.Atom> atoms, Matrix scaleRotate)
        {
            float stickRadius = Flags.Instance.StickRadius;
            float stickFactor = Flags.Instance.StickRadiusFactor;

            foreach (var atom in atoms)
            {
                if (atom.NrBondedAtoms <= 0 || atom.PrecomputedBondMatrix == null) continue;

                for (int j = 0; j < atom.NrBondedAtoms; j++)
                {
                    float distance = atom.Distance[j];
                    if (distance <= 0) continue;

                    float angleZ = atom.AngleZ[j];
                    float angleXY = atom.AngleXY[j];

                    Matrix bondMatrix = Matrix.CreateScale(stickFactor * distance, stickRadius, stickRadius);
                    bondMatrix *= Matrix.CreateRotationY(angleZ);
                    bondMatrix *= Matrix.CreateRotationZ(angleXY);

                    Matrix atomMatrix = bondMatrix;
                    atomMatrix *= Matrix.CreateTranslation(atom.Coordinate);
                    atomMatrix *= scaleRotate;

                    foreach (ModelMesh mesh in _cachedSmallRodMeshes)
                    {
                        Matrix world = _smallRodTransforms[mesh.ParentBone.Index] * atomMatrix;
                        foreach (BasicEffect effect in mesh.Effects)
                            ApplyPerAtomEffect(effect, world, _effectCache);
                        mesh.Draw();
                    }
                }
            }
        }

        #endregion

        #region DrawOverlayBallsAndSticks

        private void DrawOverlayBallsAndSticks(List<Molecule.Atom> atoms, Matrix scaleRotate)
        {
            DrawOverlaySticksOnly(atoms, scaleRotate);

            foreach (var atom in atoms)
            {
                float radius;
                if (atom.AtomicNumber == 1)
                {
                    float baseRadius = Flags.Instance.BallRadiusNonMetal;
                    radius = baseRadius * Flags.Instance.HydrogenBallScale;
                }
                else
                    radius = IsMetal(atom.AtomicNumber)
                        ? Flags.Instance.BallRadiusMetal
                        : Flags.Instance.BallRadiusNonMetal;

                Matrix atomMatrix = Matrix.CreateScale(radius, radius, radius);
                atomMatrix *= Matrix.CreateTranslation(atom.Coordinate);
                atomMatrix *= scaleRotate;

                foreach (ModelMesh mesh in _cachedSmallBallMeshes)
                {
                    Matrix world = _smallBallTransforms[mesh.ParentBone.Index] * atomMatrix;
                    foreach (BasicEffect effect in mesh.Effects)
                        ApplyPerAtomEffect(effect, world, _effectCache);
                    mesh.Draw();
                }
            }
        }

        #endregion

        #region DrawOverlayEllipsoids

        private void DrawOverlayEllipsoids(List<Molecule.Atom> atoms, Matrix scaleRotate)
        {
            DrawOverlaySticksOnly(atoms, scaleRotate);

            float probability = Flags.Instance.ATP_Probability;

            foreach (var atom in atoms)
            {
                bool hasEllipsoidData = atom.isAnisotropic && atom.Ue != Matrix.Identity;

                if (hasEllipsoidData)
                {
                    Matrix scaleMatrix = Matrix.CreateScale(probability, probability, probability);
                    Matrix scaledUn = atom.Un * scaleMatrix;

                    Matrix atomMatrix = Matrix.CreateScale(1, 1, 1);
                    atomMatrix *= atom.Transformation;
                    atomMatrix *= scaledUn;
                    atomMatrix *= Matrix.CreateTranslation(atom.Coordinate);
                    atomMatrix *= scaleRotate;

                    foreach (ModelMesh mesh in _cachedBigBallMeshes)
                    {
                        Matrix world = _bigBallTransforms[mesh.ParentBone.Index] * atomMatrix;
                        foreach (BasicEffect effect in mesh.Effects)
                            ApplyPerAtomEffect(effect, world, _effectCache);
                        mesh.Draw();
                    }
                }
                else
                {
                    float radius = IsMetal(atom.AtomicNumber)
                        ? Flags.Instance.BallRadiusMetal
                        : Flags.Instance.BallRadiusNonMetal;

                    if (atom.AtomicNumber == 1)
                        radius = radius * Flags.Instance.HydrogenBallScale;

                    Matrix atomMatrix = Matrix.CreateScale(radius, radius, radius);
                    atomMatrix *= Matrix.CreateTranslation(atom.Coordinate);
                    atomMatrix *= scaleRotate;

                    foreach (ModelMesh mesh in _cachedSmallBallMeshes)
                    {
                        Matrix world = _smallBallTransforms[mesh.ParentBone.Index] * atomMatrix;
                        foreach (BasicEffect effect in mesh.Effects)
                            ApplyPerAtomEffect(effect, world, _effectCache);
                        mesh.Draw();
                    }
                }
            }
        }

        #endregion

        #region DrawOverlayOnly

        private void DrawOverlayOnly(Matrix scaleRotate)
        {
            if (_camera == null) return;

            var cameraView = _camera.View;
            var cameraProjection = _camera.Projection;

            UpdateSceneCache(cameraView, cameraProjection);
            ApplyGlobalEffectState();

            if (_showOverlay && _overlayAtoms.Count > 0)
            {
                DrawOverlayAtoms(scaleRotate);
            }
        }

        #endregion
    }
}