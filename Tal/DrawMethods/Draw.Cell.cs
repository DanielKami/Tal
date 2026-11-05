// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.Cell.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.Cell — unit cell frame + reveal click marker
// ═══════════════════════════════════════════════════════════════════════
//
// This file contains two small, independent scene elements:
//
// DrawCell() — draws the 12 edges of the unit cell
// (from Molecule.UnitCellFrame)
// DrawRevealClickAftersition() — draws a small red sphere at the last
// click aftersition (Reveal Symmetry mode)
//
// Both methods use the rod/ball meshes and preserve the caller's
// effect cache state (save/restore pattern).
//
// ⚠️ UnitCellFrame is a flat list of 12 edges. Each edge stores:
// aftersition — start afterint (in world coords, already shifted)
// distance — edge length
// AngleZ — elevation
// AngleXY — azimuth
// color — RGB per edge (usually axis-aligned)
//
// ⚠️ The "2×" multiplier in CreateScale:
// stickFactor * 2 * distance
// is intentional — the cell edges are drawn twice as thick as
// a regular bond using the same stick radius. This makes the cell
// readable even when many atoms are nearby.
//
// ⚠️ Reveal click marker:
// - Size: 0.5 (hard-coded). This is BIG — visible even at distance.
// - Color: pure red with full white specular for visibility.
// - Uses _smallBallTransforms (cached), not ParentBone.Transform.
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region DrawCell — unit cell frame

 /// <summary>
 /// Draws the 12 edges of the unit cell frame.
 ///
 /// Each edge is rendered as a rod, oriented by precomputed
 /// AngleZ / AngleXY and scaled by the edge's distance.
 ///
 /// The caller must set <c>Flags.Instance.Draw_cell == true</c> —
 /// this method does not check the flag itself (it's checked in
 /// DrawScene before calling).
 ///
 /// ⚠️ Edges are drawn with a "2×" boost in thickness relative to
 /// a normal stick — see file header.
 /// </summary>
        private void DrawCell(Matrix scaleRotate)
        {
            if (_cachedSmallRodMeshes == null || _smallRodTransforms == null) return;
            if (_molecule == null || _molecule.UnitCellFrame == null || _molecule.UnitCellFrame.Count == 0) return;

 // ═══ Save the previous material ═══
            Vector3 origDiffuse = _effectCache.DiffuseColor;
            Vector3 origAmbient = _effectCache.AmbientLightColor;
            Vector3 origLightDiffuse = _effectCache.LightDiffuseColor;
            Vector3 origSpecular = _effectCache.SpecularColor;
            Vector3 origLightSpecular = _effectCache.LightSpecularColor;
            float origAlpha = _effectCache.Alpha;

 // ═══ Cell edges are opaque, no specular highlights ═══
            _effectCache.Alpha = 1.0f;
            _effectCache.SpecularColor = Vector3.Zero;
            _effectCache.LightSpecularColor = Vector3.Zero;

 // ═══ Draw each edge ═══
            for (int j = 0; j < _molecule.UnitCellFrame.Count; ++j)
            {
                float stickRadius = Flags.Instance.CellLineThickness;
                float stickFactor = Flags.Instance.CellLineFactor;

 // ⚠️ 2× multiplier — see file header for rationale
                Matrix cellMatrix = Matrix.CreateScale(
                    stickFactor * 2 * _molecule.UnitCellFrame[j].distance,
                    stickRadius, stickRadius);

                cellMatrix *= Matrix.CreateRotationY(_molecule.UnitCellFrame[j].AngleZ);
                cellMatrix *= Matrix.CreateRotationZ(_molecule.UnitCellFrame[j].AngleXY);
                cellMatrix *= Matrix.CreateTranslation(_molecule.UnitCellFrame[j].position);
                cellMatrix *= scaleRotate;

 // ═══ Per-edge color (usually axis-aligned RGB) ═══
                Vector3 cellColor = _molecule.UnitCellFrame[j].color;
                _effectCache.DiffuseColor = cellColor;
                _effectCache.AmbientLightColor = Vector3.One * Flags.Instance.AmbientIntensity;
                _effectCache.LightDiffuseColor = cellColor * Flags.Instance.DiffuseIntensity;

 // ═══ Draw rod mesh ═══
                foreach (ModelMesh mesh in _cachedSmallRodMeshes)
                {
                    Matrix world = _smallRodTransforms[mesh.ParentBone.Index] * cellMatrix;
                    foreach (BasicEffect effect in mesh.Effects)
                        ApplyPerAtomEffect(effect, world, _effectCache);
                    mesh.Draw();
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

        #region DrawRevealClickPosition — red marker sphere

 /// <summary>
 /// Draws a red sphere at the last Reveal Symmetry click aftersition.
 ///
 /// Used as a visual marker: the user clicks in 3D space to generate
 /// a symmetry-equivalent molecule there. The red dot shows exactly
 /// where the click landed.
 ///
 /// ⚠️ Size is hard-coded to 0.5 — intentionally larger than atoms
 /// so the marker stays visible even at typical zoom-out levels.
 ///
 /// ⚠️ Uses _smallBallMeshes (smaller, cheaper than big ball) but
 /// with a large scale factor. The visual size comes from the
 /// matrix scale, not the mesh resolution.
 /// </summary>
        private void DrawRevealClickPosition(Matrix scaleRotate)
        {
            if (_cachedSmallBallMeshes == null || _smallBallTransforms == null) return;
            if (_graphicsDevice == null) return;

 // ═══ Save the previous material ═══
            Vector3 origDiffuse = _effectCache.DiffuseColor;
            Vector3 origAmbient = _effectCache.AmbientLightColor;
            Vector3 origLightDiffuse = _effectCache.LightDiffuseColor;
            Vector3 origSpecular = _effectCache.SpecularColor;
            Vector3 origLightSpecular = _effectCache.LightSpecularColor;
            float origAlpha = _effectCache.Alpha;

 // ═══ Marker material: pure red with full specular ═══
            Vector3 redColor = new Vector3(1.0f, 0.0f, 0.0f);
            _effectCache.DiffuseColor = redColor;
            _effectCache.AmbientLightColor = Vector3.One * 0.5f;
            _effectCache.LightDiffuseColor = redColor;
            _effectCache.SpecularColor = Vector3.One;
            _effectCache.LightSpecularColor = Vector3.One;
            _effectCache.Alpha = 1.0f;

 // ⚠️ Hard-coded 0.5 scale — see docstring
            const float size = 0.5f;

            Matrix world = Matrix.CreateScale(size, size, size)
 * Matrix.CreateTranslation(RevealClickPosition);

 // ═══ Draw sphere mesh ═══
            foreach (ModelMesh mesh in _cachedSmallBallMeshes)
            {
                Matrix worldMatrix = _smallBallTransforms[mesh.ParentBone.Index] * world * scaleRotate;
                foreach (BasicEffect effect in mesh.Effects)
                    ApplyPerAtomEffect(effect, worldMatrix, _effectCache);
                mesh.Draw();
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
    }
}