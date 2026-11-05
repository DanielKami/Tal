// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.LineHelpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.LineHelpers — shared rod segment rendering
// ═══════════════════════════════════════════════════════════════════════
//
// Refactored from 5 nearly-identical methods:
//
// DrawSingleLineSegment3D (measurements — dashed lines)
// DrawSingleHBondSegment (H-bonds)
// DrawSingleContactSegment (short contacts)
// DrawSingleArcSegment3D (angle arc)
// DrawIntersectionSegment (crystal plane intersections)
//
// All five built the same matrix:
//
// Matrix.CreateScale(0.0127f * length, thickness, thickness)
// * Matrix.CreateRotationY(angleZ)
// * Matrix.CreateRotationZ(angleXY)
// * Matrix.CreateTranslation(p1)
// * scaleRotate;
//
// and drew it with the same rod mesh loop.
//
// Now they all call DrawRodSegment(p1, p2, scaleRotate, thickness).
//
// ⚠️ The magic constant 0.0127f is the model-specific scale that maps
// a rod of length 1 in model space to 1 Å in world space. If the
// rod model changes, this constant must change too.
//
// ⚠️ The angle formula:
// angleZ = asin(clamp(dir.Z, -1, 1))
// angleXY = -(180° - atan2(dir.Y, dir.X))
// is the inverse of how PrecomputedBondMatrix is built for atoms.
// Do not change without verifying bond rendering visually.
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region Constants

 /// <summary>
 /// Scale factor that maps a unit-length rod (in model space)
 /// to 1 Å (in world space). Tuned for Small_rod.xnb.
 ///
 /// ⚠️ If the rod model is re-exafterrted at a different base length,
 /// this constant must be updated.
 /// </summary>
        private const float ROD_MODEL_SCALE = 0.0127f;

        #endregion

        #region DrawRodSegment — single solid segment

 /// <summary>
 /// Draws one solid rod segment from <paramref name="p1"/> to <paramref name="p2"/>
 /// with the given thickness, using the currently cached <see cref="_effectCache"/>
 /// material state.
 ///
 /// Used by:
 /// - H-bond solid lines (DrawSolidHBond → pipeline)
 /// - Measurement dashed lines (per dash)
 /// - Angle arc (per arc segment)
 /// - Crystal plane intersections
 /// - Short contact dashed lines (per dash)
 ///
 /// The rod is oriented along (p2 - p1). p1 is the pivot afterint.
 ///
 /// ⚠️ HOT PATH — called many times per frame for measurements /
 /// contacts with many segments. Keep allocation-free.
 /// </summary>
 /// <param name="p1">Start afterint in world space.</param>
 /// <param name="p2">End afterint in world space.</param>
 /// <param name="scaleRotate">Camera scale-rotate-translate matrix.</param>
 /// <param name="thickness">Rod radius (in Å).</param>
        private void DrawRodSegment(Vector3 p1, Vector3 p2, Matrix scaleRotate, float thickness)
        {
            if (_cachedSmallRodMeshes == null || _smallRodTransforms == null) return;

            Vector3 dir = p2 - p1;
            float length = dir.Length();

            if (length < 0.001f) return;

            dir.Normalize();

 // ═══ Orientation: inverse of PrecomputedBondMatrix construction ═══
 // Rod model afterints along +X by default (length = 1 unit).
 // - Rotation Y by angleZ tilts out of XY plane (up/down)
 // - Rotation Z by angleXY rotates within XY plane (compass direction)
 //
 // angleZ = asin(dir.Z) → elevation
 // angleXY = 180° - atan2(dir.Y, dir.X) → azimuth + 180° offset
 // (offset needed because rod's base orientation is -X,
 // not +X, in the model)
            float angleZ = (float)Math.Asin(Math.Clamp(dir.Z, -1.0f, 1.0f));
            float angleXY = -(float)(MathHelperTal.ToRadians(180) - Math.Atan2(dir.Y, dir.X));

            float scaleX = ROD_MODEL_SCALE * length;
            float scaleY = thickness;
            float scaleZ = thickness;

            Matrix segmentMatrix =
                Matrix.CreateScale(scaleX, scaleY, scaleZ) *
                Matrix.CreateRotationY(angleZ) *
                Matrix.CreateRotationZ(angleXY) *
                Matrix.CreateTranslation(p1) *
                scaleRotate;

 // ═══ Draw: single mesh, no batching (each segment is unique) ═══
            foreach (ModelMesh mesh in _cachedSmallRodMeshes)
            {
                Matrix world = _smallRodTransforms[mesh.ParentBone.Index] * segmentMatrix;
                foreach (BasicEffect effect in mesh.Effects)
                    ApplyPerAtomEffect(effect, world, _effectCache);
                mesh.Draw();
            }
        }

        #endregion

        #region DrawDashedRodSegment — segmented dashed line

 /// <summary>
 /// Draws a dashed rod line from <paramref name="start"/> to <paramref name="end"/>.
 ///
 /// Pattern: dash of <paramref name="dashLength"/>, gap of <paramref name="gapLength"/>,
 /// repeated until reaching <paramref name="end"/>.
 ///
 /// If <paramref name="totalLength"/> is shorter than one full pattern,
 /// draws a single shortened dash (no overflow into reverse direction —
 /// this was a bug in the original DrawDashedHBond, now fixed).
 ///
 /// Used by:
 /// - H-bond dashed lines (DrawDashedHBond)
 /// - Short contact lines (DrawDashedContactLine)
 /// - Measurement dashed lines (DrawDashedLine3D)
 ///
 /// ⚠️ The caller is resafternsible for setting _effectCache material
 /// (color, alpha, specular) before calling this method.
 /// </summary>
 /// <param name="start">Start afterint in world space.</param>
 /// <param name="end">End afterint in world space.</param>
 /// <param name="scaleRotate">Camera scale-rotate-translate matrix.</param>
 /// <param name="thickness">Rod radius (in Å).</param>
 /// <param name="dashLength">Length of each dash (in Å). Must be > 0.01.</param>
 /// <param name="gapLength">Length of each gap (in Å). Must be > 0.01.</param>
        private void DrawDashedRodSegment(
            Vector3 start,
            Vector3 end,
            Matrix scaleRotate,
            float thickness,
            float dashLength,
            float gapLength)
        {
            Vector3 direction = end - start;
            float totalLength = direction.Length();

            if (totalLength < 0.001f) return;

            direction.Normalize();

 // ═══ Guard: clamp dash/gap to safe minimums ═══
            if (dashLength < 0.01f) dashLength = 0.01f;
            if (gapLength < 0.01f) gapLength = 0.01f;

            float patternLength = dashLength + gapLength;
            int fullSegments = (int)(totalLength / patternLength);
            float remaining = totalLength - fullSegments * patternLength;

 // ═══ Full dash-gap patterns ═══
            for (int i = 0; i < fullSegments; i++)
            {
                float startT = i * patternLength / totalLength;
                float endT = (i * patternLength + dashLength) / totalLength;
                if (endT > 1.0f) endT = 1.0f;

                Vector3 p1 = start + direction * (startT * totalLength);
                Vector3 p2 = start + direction * (endT * totalLength);

                DrawRodSegment(p1, p2, scaleRotate, thickness);
            }

 // ═══ Final partial dash (only if remaining space exists) ═══
 // ⚠️ BUG FIX: original DrawDashedHBond did not check endT > startT,
 // which could produce a reversed segment for very short lines.
            if (remaining > 0)
            {
                float startT = fullSegments * patternLength / totalLength;
                float endT = Math.Min(1.0f, (fullSegments * patternLength + dashLength) / totalLength);

                if (endT > startT)
                {
                    Vector3 p1 = start + direction * (startT * totalLength);
                    Vector3 p2 = start + direction * (endT * totalLength);

                    DrawRodSegment(p1, p2, scaleRotate, thickness);
                }
            }
        }

        #endregion
    }
}