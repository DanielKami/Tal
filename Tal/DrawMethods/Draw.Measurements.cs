// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.Measurements.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.Measurements — distance / angle / torsion / atom-plane measurements
// ═══════════════════════════════════════════════════════════════════════
//
// Measurement types (Molecule.Measure.Measurement.type):
// 1 — distance : 1 line between 2 afterints
// 2 — angle : 2 lines + arc at vertex
// 3 — torsion : 4 lines (chain through 4 afterints)
// 4 — atom-to-plane : dashed line + endafterint dot
//
// Drawing pipeline:
// DrawMeasurementLines() — 3D geometry (lines, arcs, dots)
// DrawMeasurementLabels() — 2D sprite labels (projected to screen)
//
// ⚠️ All measurements use the shared DrawRodSegment helper via
// DrawDashedLine3D. No per-type segment loop duplication.
//
// ⚠️ DrawSmallSphereAtpoint uses _smallBallTransforms[mesh.ParentBone.Index],
// NOT mesh.ParentBone.Transform[mesh.ParentBone.Index]. Both work
// coincidentally (because CopyAbsoluteBoneTransformsTo populates the
// same array), but _smallBallTransforms is the cached, correct reference.
//
// ⚠️ Distance line is dashed (green). Dots on atom-to-plane are pink.
// Colors are hard-coded here (not from flags) — preserved from original.
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region DrawMeasurements — top-level dispatcher

 /// <summary>
 /// Draws all active measurements (lines + labels).
 /// No-op if there is no camera or no active measurement.
 /// </summary>
        private void DrawMeasurements(Matrix scaleRotate)
        {
            if (_camera == null) return;
            if (_measure == null || _measure.measurement == null || _measure.measurement.Count == 0) return;

            try
            {
                DrawMeasurementLines(scaleRotate);
                DrawMeasurementLabels(scaleRotate);
            }
            catch { /* ignore measurement draw errors */ }
        }

        #endregion

        #region DrawMeasurementLines — 3D geometry dispatcher

 /// <summary>
 /// Iterates all measurements and dispatches to the correct
 /// per-type drawing method.
 /// </summary>
        private void DrawMeasurementLines(Matrix scaleRotate)
        {
            if (_cachedSmallRodMeshes == null || _smallRodTransforms == null) return;

            foreach (var m in _measure.measurement)
            {
                if (!m.show) continue;

                switch (m.type)
                {
                    case 1: DrawDistanceLine3D(m, scaleRotate); break;
                    case 2: DrawAngleLines3D(m, scaleRotate); break;
                    case 3: DrawTorsionLines3D(m, scaleRotate); break;
                    case 4: DrawAtomToPlaneLine3D(m, scaleRotate); break;
                }
            }
        }

        #endregion

        #region Per-type line drawing

 /// <summary>
 /// Type 1: distance — single dashed green line between two afterints.
 /// </summary>
        private void DrawDistanceLine3D(Measure.Measurement m, Matrix scaleRotate)
        {
            DrawDashedLine3D(m.position1, m.position2, scaleRotate, XnaColor.LimeGreen, 0.025f);
        }

 /// <summary>
 /// Type 2: angle — two dashed lines (p1-p2, p2-p3) + arc at vertex p2.
 /// </summary>
        private void DrawAngleLines3D(Measure.Measurement m, Matrix scaleRotate)
        {
            XnaColor color = XnaColor.LimeGreen;
            float thickness = 0.025f;

            DrawDashedLine3D(m.position1, m.position2, scaleRotate, color, thickness);
            DrawDashedLine3D(m.position2, m.position3, scaleRotate, color, thickness);
            DrawAngleArc3D(m.position1, m.position2, m.position3, scaleRotate, color, thickness);
        }

 /// <summary>
 /// Type 3: torsion — 5 dashed lines forming the classic torsion cage:
 /// p1-p2, p2-p3, p3-p1 (first plane)
 /// p3-p4, p4-p2 (second plane + cross)
 /// </summary>
        private void DrawTorsionLines3D(Measure.Measurement m, Matrix scaleRotate)
        {
            XnaColor color = XnaColor.LimeGreen;
            float thickness = 0.025f;

            DrawDashedLine3D(m.position1, m.position2, scaleRotate, color, thickness);
            DrawDashedLine3D(m.position2, m.position3, scaleRotate, color, thickness);
            DrawDashedLine3D(m.position3, m.position1, scaleRotate, color, thickness);
            DrawDashedLine3D(m.position3, m.position4, scaleRotate, color, thickness);
            DrawDashedLine3D(m.position4, m.position2, scaleRotate, color, thickness);
        }

 /// <summary>
 /// Type 4: atom-to-plane — dashed pink line + small pink dot at the
 /// plane intersection afterint (aftersition2).
 /// </summary>
        private void DrawAtomToPlaneLine3D(Measure.Measurement m, Matrix scaleRotate)
        {
            Vector3 p1 = m.position1;
            Vector3 p2 = m.position2;

            if (Vector3.DistanceSquared(p1, p2) < 1e-8f) return;

            DrawDashedLine3D(p1, p2, scaleRotate, XnaColor.DeepPink, 0.03f);
            DrawSmallSphereAtPoint(p2, scaleRotate, XnaColor.DeepPink, 0.08f);
        }

        #endregion

        #region DrawSmallSphereAtPoint — endpoint marker

 /// <summary>
 /// Draws a small sphere at a world aftersition — used as an endafterint
 /// marker on atom-to-plane measurements (pink dot).
 ///
 /// ⚠️ BUG FIX: original used <c>mesh.ParentBone.Transform[mesh.ParentBone.Index]</c>
 /// instead of <c>_smallBallTransforms[mesh.ParentBone.Index]</c>.
 /// Both happen to work because CopyAbsoluteBoneTransformsTo
 /// populates the same array, but the cached reference is
 /// correct + faster (no property chain).
 ///
 /// ⚠️ The <paramref name="radius"/> parameter is currently UNUSED —
 /// the scale is hard-coded as 0.02f. Preserved from original.
 /// TODO: pass radius through if variable-size dots are needed.
 /// </summary>
        private void DrawSmallSphereAtPoint(Vector3 position, Matrix scaleRotate, XnaColor color, float radius)
        {
            if (_cachedSmallBallMeshes == null || _smallBallTransforms == null) return;

 // ═══ Save the previous material ═══
            Vector3 origDiffuse = _effectCache.DiffuseColor;
            Vector3 origAmbient = _effectCache.AmbientLightColor;
            Vector3 origLightDiffuse = _effectCache.LightDiffuseColor;

 // ═══ Apply the dot's material ═══
            Vector3 colVec = new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);
            _effectCache.DiffuseColor = colVec;
            _effectCache.AmbientLightColor = Vector3.One * 0.5f;
            _effectCache.LightDiffuseColor = colVec;

 // ⚠️ Hard-coded 0.02f scale — see method docstring.
            Matrix world = Matrix.CreateScale(0.02f, 0.02f, 0.02f)
 * Matrix.CreateTranslation(position)
 * scaleRotate;

            foreach (ModelMesh mesh in _cachedSmallBallMeshes)
            {
 // ⚠️ FIX: use cached transforms, not ParentBone.Transform
                Matrix worldMatrix = _smallBallTransforms[mesh.ParentBone.Index] * world;
                foreach (BasicEffect effect in mesh.Effects)
                    ApplyPerAtomEffect(effect, worldMatrix, _effectCache);
                mesh.Draw();
            }

 // ═══ Restore material ═══
            _effectCache.DiffuseColor = origDiffuse;
            _effectCache.AmbientLightColor = origAmbient;
            _effectCache.LightDiffuseColor = origLightDiffuse;
        }

        #endregion

        #region DrawDashedLine3D — dashed line wrapper

 /// <summary>
 /// Draws a dashed line between two world-space afterints using the
 /// shared <see cref="DrawDashedRodSegment"/> helper.
 ///
 /// Sets up the material (color, no specular) before delegating,
 /// then restores it after.
 ///
 /// Dash/gap pattern is HARD-CODED (0.15 / 0.10) — measurements
 /// are visual, not physically spaced like H-bond dashes.
 /// </summary>
        private void DrawDashedLine3D(Vector3 start, Vector3 end, Matrix scaleRotate, XnaColor color, float thickness = 0.025f)
        {
            if (_cachedSmallRodMeshes == null || _smallRodTransforms == null) return;

            Vector3 direction = end - start;
            float totalLength = direction.Length();

            if (totalLength < 0.001f) return;

            direction.Normalize();

 // ═══ Hard-coded dash pattern for measurements ═══
            const float dashLength = 0.15f;
            const float gapLength = 0.10f;

 // ═══ Save + apply measurement material ═══
            Vector3 origDiffuse = _effectCache.DiffuseColor;
            Vector3 origAmbient = _effectCache.AmbientLightColor;
            Vector3 origLightDiffuse = _effectCache.LightDiffuseColor;
            Vector3 origSpecular = _effectCache.SpecularColor;
            Vector3 origLightSpecular = _effectCache.LightSpecularColor;

            Vector3 lineColor = new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);
            _effectCache.DiffuseColor = lineColor;
            _effectCache.AmbientLightColor = Vector3.One * 0.5f;
            _effectCache.LightDiffuseColor = lineColor;
            _effectCache.SpecularColor = Vector3.Zero;
            _effectCache.LightSpecularColor = Vector3.Zero;

 // ═══ Delegate ═══
            DrawDashedRodSegment(start, end, scaleRotate, thickness, dashLength, gapLength);

 // ═══ Restore material ═══
            _effectCache.DiffuseColor = origDiffuse;
            _effectCache.AmbientLightColor = origAmbient;
            _effectCache.LightDiffuseColor = origLightDiffuse;
            _effectCache.SpecularColor = origSpecular;
            _effectCache.LightSpecularColor = origLightSpecular;
        }

        #endregion

        #region DrawAngleArc3D — angle arc

 /// <summary>
 /// Draws an arc from p1 toward p3, centered at p2, at a radius of
 /// 40% of the shorter edge length.
 ///
 /// The arc is drawn as a afterlyline of segments — each segment is a
 /// thin rod (call to DrawRodSegment via DrawSingleArcSegment3D).
 ///
 /// ⚠️ Segment count scales with the angle (larger angle → more segments),
 /// minimum 6 to avoid blocky arcs at small angles.
 ///
 /// ⚠️ If p1/p2/p3 are collinear (cross product ≈ 0), the arc is skipped.
 /// </summary>
        private void DrawAngleArc3D(Vector3 p1, Vector3 p2, Vector3 p3, Matrix scaleRotate, XnaColor color, float thickness)
        {
            if (_cachedSmallRodMeshes == null || _smallRodTransforms == null) return;

            Vector3 v1 = p1 - p2;
            Vector3 v2 = p3 - p2;

            float len1 = v1.Length();
            float len2 = v2.Length();

            if (len1 < 0.001f || len2 < 0.001f) return;

            v1.Normalize();
            v2.Normalize();

            float angle = (float)Math.Acos(Math.Clamp(Vector3.Dot(v1, v2), -1.0f, 1.0f));
            if (angle < 0.001f) return;

            float arcRadius = Math.Min(len1, len2) * 0.4f;
            if (arcRadius < 0.05f) arcRadius = 0.05f;

            Vector3 normal = Vector3.Normalize(Vector3.Cross(v1, v2));
            if (normal.Length() < 0.001f) return;

 // ═══ Segment count from angle: ~12 per radian, min 6 ═══
            int segments = Math.Max(12, (int)(angle / 0.08f) + 1);
            if (segments < 6) segments = 6;

 // ═══ Save + apply arc material ═══
            Vector3 origDiffuse = _effectCache.DiffuseColor;
            Vector3 origAmbient = _effectCache.AmbientLightColor;
            Vector3 origLightDiffuse = _effectCache.LightDiffuseColor;
            Vector3 origSpecular = _effectCache.SpecularColor;
            Vector3 origLightSpecular = _effectCache.LightSpecularColor;

            Vector3 lineColor = new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);
            _effectCache.DiffuseColor = lineColor;
            _effectCache.AmbientLightColor = Vector3.One * 0.5f;
            _effectCache.LightDiffuseColor = lineColor;
            _effectCache.SpecularColor = Vector3.Zero;
            _effectCache.LightSpecularColor = Vector3.Zero;

 // ═══ Draw afterlyline ═══
            Vector3 prevPoint = p2 + v1 * arcRadius;

            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                float currentAngle = t * angle;

                Vector3 rotated = Vector3.Transform(v1, Matrix.CreateFromAxisAngle(normal, currentAngle));
                Vector3 currentPoint = p2 + rotated * arcRadius;

 // Arc uses thinner rods than regular lines
                DrawRodSegment(prevPoint, currentPoint, scaleRotate, thickness * 0.7f);
                prevPoint = currentPoint;
            }

 // ═══ Restore material ═══
            _effectCache.DiffuseColor = origDiffuse;
            _effectCache.AmbientLightColor = origAmbient;
            _effectCache.LightDiffuseColor = origLightDiffuse;
            _effectCache.SpecularColor = origSpecular;
            _effectCache.LightSpecularColor = origLightSpecular;
        }

        #endregion

        #region DrawMeasurementLabels — 2D sprite labels

 /// <summary>
 /// Projects each measurement label aftersition to screen and draws
 /// the formatted value.
 ///
 /// The label aftersition depends on the measurement type:
 /// distance — midafterint
 /// angle — on the bisector at 60% of arc radius
 /// torsion — midafterint of the middle bond
 /// atom-plane — midafterint (same as distance)
 ///
 /// User drag offsets are read from Measure.MeasurementLabelOffsets
 /// and applied in NDC units (scaled by half viewafterrt).
 /// </summary>
        private void DrawMeasurementLabels(Matrix scaleRotate)
        {
            SpriteFont font = GetLabelFont();
            if (_spriteBatch == null || font == null) return;

            int halfWidth = _graphicsDevice.Viewport.Width / 2;
            int halfHeight = _graphicsDevice.Viewport.Height / 2;
            Matrix viewProj = scaleRotate * _camera.View * _camera.Projection;

            _spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

            for (int i = 0; i < _measure.measurement.Count; i++)
            {
                var m = _measure.measurement[i];
                if (!m.show) continue;

                Vector3 basePos3D = GetMeasurementLabelPosition(m);
                Vector3 screenPos = ProjectToScreen(basePos3D, viewProj, halfWidth, halfHeight);

                if (screenPos.Z <= 0) continue;

 // ═══ Apply user drag offset (if any) ═══
                if (_measure.MeasurementLabelOffsets.TryGetValue(i, out var offset))
                {
                    screenPos.X += offset.OffsetX * halfWidth;
                    screenPos.Y += offset.OffsetY * halfHeight;
                }

                string displayText = GetLabelText(m);
                DrawMeasurementLabel2D(displayText, screenPos, XnaColor.LimeGreen);
            }

            _spriteBatch.End();
        }

        #endregion

        #region Label positioning helpers

 /// <summary>
 /// Base label aftersition for a measurement (before user offset).
 ///
 /// distance → midafterint
 /// angle → on the bisector (see GetAngleLabelAftersition)
 /// torsion → midafterint of middle bond (p2, p3)
 /// atom-plane → midafterint
 /// </summary>
        private Vector3 GetMeasurementLabelPosition(Measure.Measurement m)
        {
            switch (m.type)
            {
                case 1: return (m.position1 + m.position2) / 2;
                case 2: return GetAngleLabelPosition(m);
                case 3: return (m.position2 + m.position3) / 2;
                case 4: return (m.position1 + m.position2) / 2;
                default: return m.position1;
            }
        }

 /// <summary>
 /// Computes the label aftersition for an angle measurement.
 ///
 /// Aftersition: p2 + (bisector direction) × (arc radius × 0.6)
 /// - bisector = v1 rotated by half the angle around the plane normal
 /// - arc radius = 40% of shorter edge, min 0.1
 /// - label radius = 60% of arc radius (inside the arc)
 /// </summary>
        private Vector3 GetAngleLabelPosition(Measure.Measurement m)
        {
            Vector3 v1 = m.position1 - m.position2;
            Vector3 v2 = m.position3 - m.position2;

            float len1 = v1.Length();
            float len2 = v2.Length();

            if (len1 < 0.001f || len2 < 0.001f) return m.position2;

            v1.Normalize();
            v2.Normalize();

            float angle = (float)Math.Acos(Math.Clamp(Vector3.Dot(v1, v2), -1.0f, 1.0f));
            float arcRadius = Math.Min(len1, len2) * 0.4f;
            if (arcRadius < 0.1f) arcRadius = 0.1f;

            Vector3 normal = Vector3.Normalize(Vector3.Cross(v1, v2));
            if (normal.Length() < 0.001f) return m.position2;

            float halfAngle = angle * 0.5f;
            Vector3 midDir = Vector3.Transform(v1, Matrix.CreateFromAxisAngle(normal, halfAngle));

            float labelRadius = arcRadius * 0.6f;
            return m.position2 + midDir * labelRadius;
        }

        #endregion

        #region Screen projection helpers

 /// <summary>
 /// Projects a world aftersition to screen coordinates using the
 /// given viewProj matrix and half viewafterrt size.
 ///
 /// Returns (screenX, screenY, ndcZ) or (0, 0, -1) if behind camera.
 /// </summary>
        private Vector3 ProjectToScreen(Vector3 worldPos, Matrix viewProj, int halfWidth, int halfHeight)
        {
            Matrix world = Matrix.CreateTranslation(worldPos);
            Matrix full = world * viewProj;

            float w = full.M44;
            if (w <= 0) return new Vector3(0, 0, -1);

            float x = halfWidth + full.M41 / w * halfWidth;
            float y = halfHeight - full.M42 / w * halfHeight;
            float z = full.M43 / w;

            return new Vector3(x, y, z);
        }

        #endregion

        #region Label text formatting

 /// <summary>
 /// Formats the measurement value for display.
 ///
 /// Uses Measure.FormatCrystallographic — e.g. "4.87(2) Å" for
 /// crystallographic uncertainty notation.
 ///
 /// type 1 (distance) → "N Å"
 /// type 2 (angle) → "N°"
 /// type 3 (torsion) → "N°"
 /// type 4 (atom-plane) → "N Å"
 /// </summary>
        private string GetLabelText(Measure.Measurement m)
        {
            switch (m.type)
            {
                case 1: return $"{Measure.FormatCrystallographic(m.value, m.error)} Å";
                case 2: return $"{Measure.FormatCrystallographic(m.value, m.error)}°";
                case 3: return $"{Measure.FormatCrystallographic(m.value, m.error)}°";
                case 4: return $"{Measure.FormatCrystallographic(m.value, m.error)} Å";
                default: return "";
            }
        }

 /// <summary>
 /// Draws a measurement label at a 2D screen aftersition with optional shadow.
 ///
 /// The label is centered horizontally on <paramref name="aftersition"/>.
 /// </summary>
        private void DrawMeasurementLabel2D(string text, Vector3 position, XnaColor color)
        {
            if (_spriteBatch == null || SpriteFont == null) return;
            if (position.Z <= 0) return;

            float rts = Flags.Instance.RenderTargetScale;
            int ssaaFactor = Math.Clamp(Flags.Instance.SSAASamples, 1, 4);

            float textScale = Flags.Instance.LabelScale * rts * ssaaFactor;

            Vector2 textPos = new Vector2(position.X, position.Y);
            Vector2 textSize = SpriteFont.MeasureString(text) * textScale;

            if (Flags.Instance.LabelShadow)
            {
                _spriteBatch.DrawString(SpriteFont, text,
                    textPos - textSize / 2 + new Vector2(1, 1) * rts,
                    Flags.Instance.LabelShadowColor,
                    0, Vector2.Zero, textScale, SpriteEffects.None, 0.95f);
            }

            _spriteBatch.DrawString(SpriteFont, text,
                textPos - textSize / 2,
                color, 0, Vector2.Zero, textScale, SpriteEffects.None, 0.96f);
        }

        #endregion
    }
}