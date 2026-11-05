// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.Labels.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.Labels — 2D atom labels (sprite layer)
// ═══════════════════════════════════════════════════════════════════════
//
// Atom labels are drawn as a 2D overlay on top of the 3D scene.
// They use SpriteBatch in BackToFront sort mode so that text appears
// on top of all 3D geometry regardless of Z.
//
// Projection: each atom's world aftersition is projected to screen space
// using the same viewProj matrix as the 3D scene, then offset by
// per-atom (or global) label offset in NDC units.
//
// Per-atom label offsets are stored in Molecule.AtomLabelOffset[]:
// OffsetX, OffsetY — in NDC (relative to half viewafterrt size)
// IsCustom — if false, global flags are used instead
//
// Axis labels (a, b, c) are drawn at the ends of the unit-cell axes
// when Flags.ShowAxisLabels && Flags.Draw_cell — independent of atom Labels.
//
// ⚠️ This method MUST be called LAST in DrawScene (after all 3D content)
// because it opens its own SpriteBatch.Begin/End block.
//
// ⚠️ The label offset array is lazily initialized on first call. After
// that, it is preserved across frames — user drag operations modify
// individual entries via MainForm.HandleLabelDrag.
//
// ⚠️ Label scale combines three factors:
// LabelScale (user) × RenderTargetScale (SSAA) × SSAASamples (MSAA)
// This keeps labels readable at any supersampling level without
// user intervention.
//
// ═══════════════════════════════════════════════════════════════════════

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region DrawLabels — main label rendering

 /// <summary>
 /// Draws all visible atom labels and (optionally) axis labels.
 ///
 /// Label content depends on <see cref="Flags.LabelProperty"/>:
 /// 0 — atom name only
 /// 1 — name + charge
 /// 2 — name + hydrophobicity
 ///
 /// Which atoms get labels depends on <see cref="Flags.ShowLabelsFor"/>:
 /// 0 — all atoms
 /// 1 — non-hydrogen atoms
 /// 2 — selected atoms only (per atom show_label flag)
 /// 3 — none
 /// default — per atom show_label flag
 ///
 /// Selected atoms always get yellow labels, regardless of color mode.
 /// </summary>
        private void DrawLabels(Matrix scaleRotate)
        {
            if (_molecule == null) return;

            bool drawAtomLabels = Flags.Instance.labels
                && _molecule.atom != null
                && _molecule.NumberOfAtoms > 0;
            bool drawAxisLabels = Flags.Instance.ShowAxisLabels
                && Flags.Instance.Draw_cell
                && _molecule.UnitCellFrame != null
                && _molecule.UnitCellFrame.Count >= 9;

            // Atom labels and cell-axis labels are independent
            if (!drawAtomLabels && !drawAxisLabels) return;

            SpriteFont font = GetLabelFont();
            if (_spriteBatch == null || font == null || _camera == null) return;

            float rts = Flags.Instance.RenderTargetScale;
            int ssaaFactor = Math.Clamp(Flags.Instance.SSAASamples, 1, 4);
            float labelScale = Flags.Instance.LabelScale * rts * ssaaFactor;

            bool shadow = Flags.Instance.LabelShadow;
            XnaColor shadowColor = Flags.Instance.LabelShadowColor;
            XnaColor labelColor = Flags.Instance.LabelColor;

            int halfWidth = _graphicsDevice.Viewport.Width / 2;
            int halfHeight = _graphicsDevice.Viewport.Height / 2;

            if (drawAtomLabels)
            {
                if (_molecule.LabelOffsets == null || _molecule.LabelOffsets.Length != _molecule.NumberOfAtoms)
                {
                    _molecule.LabelOffsets = new Molecule.AtomLabelOffset[_molecule.NumberOfAtoms];
                    for (int i = 0; i < _molecule.NumberOfAtoms; i++)
                    {
                        _molecule.LabelOffsets[i] = new Molecule.AtomLabelOffset
                        {
                            OffsetX = Flags.Instance.LabelOffsetX,
                            OffsetY = Flags.Instance.LabelOffsetY,
                            IsCustom = false
                        };
                    }
                }
            }

            _spriteBatch.Begin(
                SpriteSortMode.BackToFront,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone);

            Matrix viewProj = scaleRotate * _camera.View * _camera.Projection;

            // ═══ Pass 1: atom labels (Display → Labels only) ═══
            if (drawAtomLabels)
            {
                int showLabelsFor = Flags.Instance.ShowLabelsFor;
                for (int i = 0; i < _molecule.NumberOfAtoms; i++)
                {
                    if (!_molecule.atom[i].show) continue;

                    bool showLabel = false;
                    switch (showLabelsFor)
                    {
                        case 0: showLabel = true; break;
                        case 1: showLabel = _molecule.atom[i].AtomicNumber != 1; break;
                        case 2: showLabel = _molecule.atom[i].selected || _molecule.atom[i].show_label; break;
                        case 3: showLabel = false; break;
                        default: showLabel = _molecule.atom[i].show_label; break;
                    }
                    if (_molecule.atom[i].selected) showLabel = true;
                    if (!showLabel) continue;

                    Vector3 worldPos = _molecule.atom[i].Coordinate;
                    Matrix worldMatrix = Matrix.CreateTranslation(worldPos);
                    Matrix fullMatrix = worldMatrix * viewProj;
                    float w = fullMatrix.M44;
                    if (w <= 0) continue;

                    Vector3 screenPos = Vector3.Zero;
                    screenPos.X = halfWidth + fullMatrix.M41 / w * halfWidth;
                    screenPos.Y = halfHeight - fullMatrix.M42 / w * halfHeight;

                    var offset = _molecule.LabelOffsets[i];
                    float offsetX = offset.OffsetX * halfWidth;
                    float offsetY = offset.OffsetY * halfHeight;
                    if (!offset.IsCustom)
                    {
                        offsetX = Flags.Instance.LabelOffsetX * halfWidth;
                        offsetY = Flags.Instance.LabelOffsetY * halfHeight;
                    }

                    string labelText = _molecule.atom[i].name;
                    int labelProperty = Flags.Instance.LabelProperty;
                    if (labelProperty == 1)
                        labelText += $" {_molecule.atom[i].charge:F2}";
                    else if (labelProperty == 2)
                        labelText += $" {_molecule.atom[i].Hydrophobicity:F2}";

                    XnaColor currentColor = _molecule.atom[i].selected ? XnaColor.Yellow : labelColor;
                    Vector2 textPos = new Vector2(screenPos.X + offsetX, screenPos.Y + offsetY);
                    Vector2 textSize = font.MeasureString(labelText) * labelScale;

                    if (shadow)
                    {
                        _spriteBatch.DrawString(font, labelText,
                            textPos - textSize / 2 + new Vector2(1, 1) * rts,
                            shadowColor, 0, Vector2.Zero, labelScale,
                            SpriteEffects.None, 0.95f);
                    }

                    _spriteBatch.DrawString(font, labelText,
                        textPos - textSize / 2,
                        currentColor, 0, Vector2.Zero, labelScale,
                        SpriteEffects.None, 0.96f);
                }
            }

            // ═══ Pass 2: unit-cell axis labels a, b, c (Unit Cell Frame window) ═══
            // Independent of Flags.labels. Movable via Move Labels (AxisLabelOffsets).
            // UnitCellFrame: edges 0/1/2 = a/b/c from origin; corners a/b/c at indices 3,6,8.
            if (drawAxisLabels)
            {
                _molecule.EnsureAxisLabelOffsets();

                string[] axisLabels = { "a", "b", "c" };
                XnaColor[] axisColors = { XnaColor.Red, XnaColor.LimeGreen, XnaColor.CornflowerBlue };
                float axisScale = Math.Max(0.35f, Flags.Instance.AxisLabelScale)
                    * 0.85f * rts * ssaaFactor;

                int[] endEdgeIndex = { 3, 6, 8 };
                // Default pixel-ish bias so labels sit slightly outside the corner
                float[] defOx = { 0.014f, -0.010f, 0.006f };
                float[] defOy = { -0.006f, -0.012f, -0.022f };

                for (int i = 0; i < 3; i++)
                {
                    int ei = endEdgeIndex[i];
                    if (ei >= _molecule.UnitCellFrame.Count) continue;
                    Vector3 worldPos = _molecule.UnitCellFrame[ei].position;

                    Matrix worldMatrix = Matrix.CreateTranslation(worldPos);
                    Matrix fullMatrix = worldMatrix * viewProj;
                    float w = fullMatrix.M44;
                    if (w <= 0) continue;

                    float sx = halfWidth + fullMatrix.M41 / w * halfWidth;
                    float sy = halfHeight - fullMatrix.M42 / w * halfHeight;

                    var axOff = _molecule.AxisLabelOffsets[i];
                    float ox = (axOff.IsCustom ? axOff.OffsetX : defOx[i]) * halfWidth;
                    float oy = (axOff.IsCustom ? axOff.OffsetY : defOy[i]) * halfHeight;
                    // When custom, still keep a small base bias so first click isn't on the edge
                    if (axOff.IsCustom)
                    {
                        ox = axOff.OffsetX * halfWidth;
                        oy = axOff.OffsetY * halfHeight;
                    }

                    Vector2 textPos = new Vector2(sx + ox, sy + oy);
                    Vector2 textSize = font.MeasureString(axisLabels[i]) * axisScale;

                    if (shadow)
                    {
                        _spriteBatch.DrawString(font, axisLabels[i],
                            textPos - textSize / 2 + new Vector2(1, 1) * rts,
                            shadowColor, 0, Vector2.Zero, axisScale,
                            SpriteEffects.None, 0.95f);
                    }

                    _spriteBatch.DrawString(font, axisLabels[i],
                        textPos - textSize / 2,
                        axisColors[i], 0, Vector2.Zero, axisScale,
                        SpriteEffects.None, 0.96f);
                }
            }

            _spriteBatch.End();
        }

        #endregion
    }
}