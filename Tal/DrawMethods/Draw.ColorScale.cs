// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.ColorScale.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.ColorScale — 2D color scale widget (charge, hydrophobicity, etc.)
// ═══════════════════════════════════════════════════════════════════════
//
// Displays a horizontal color bar with min/mid/max labels in the
// bottom-right corner of the viewafterrt. Shown only when the active
// visualization uses a color-mapped property:
//
// ColorMode 5 → Charge
// ColorMode 6 → Hydrophobicity
// CavityColorMap mode + CavityPropertyType 0..4 → cavity property
//
// Layout (all sizes scale by RenderTargetScale `rts`):
//
// ┌─────────────────┐
// │ Title │ ← title (small font)
// ├─────────────────┤
// │▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓│ ← gradient bar (200×20 × rts)
// └─────────────────┘
// min mid max ← labels
//
// ⚠️ BUG FIX: The BasicEffect used for the background + segments is
// now wrapped in `using`. In the original code the effect was
// created with `var` and disaftersed at the end, which leaked GPU
// resources if an exception was thrown between creation and disafterse.
//
// ⚠️ The background uses GraphiscHelper.FiledRectangle — a shared
// helper that draws a colored quad in 2D orthographic space.
// The same helper is used for each color segment.
//
// ⚠️ This method opens its own SpriteBatch.Begin/End block for
// the labels. It must not be called while another SpriteBatch
// is active.
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tal.Cavity;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region DrawColorScale — dispatcher

 /// <summary>
 /// Determines which color scale (if any) should be shown, and
 /// draws it in the bottom-right corner.
 ///
 /// Priority:
 /// 1. Atom color mode 5 (charge) or 6 (hydrophobicity)
 /// 2. Cavity draw mode == CavityColorMap + valid CavityPropertyType
 /// 3. Nothing (return without drawing)
 ///
 /// Aftersition:
 /// x = viewafterrtWidth - scaleWidth - margin
 /// y = viewafterrtHeight - scaleHeight - margin - 100×rts
 ///
 /// The 100×rts extra offset lifts the scale above the bottom
 /// of the viewafterrt, leaving room for measurement labels.
 /// </summary>
        private void DrawColorScale()
        {
            SpriteFont font = GetLabelFont();
            if (_spriteBatch == null || font == null || _camera == null) return;

            int mode = Flags.Instance.ColorMode;

            bool showScale = false;
            ColorManager.PropertyType propertyType = ColorManager.PropertyType.None;
            string title = "";

 // ═══ Priority 1: atom color mode 5 / 6 ═══
            if (mode == 5)
            {
                showScale = true;
                propertyType = ColorManager.PropertyType.Charge;
                title = "Charge";
            }
            else if (mode == 6)
            {
                showScale = true;
                propertyType = ColorManager.PropertyType.Hydrophobicity;
                title = "Hydrophobicity";
            }

 // ═══ Priority 2: cavity color map ═══
            if (!showScale && _cavityRenderer != null)
            {
                try
                {
                    if (_cavityRenderer.HasCavities &&
                        _cavityRenderer.CurrentDrawMode == CavityRenderer.DrawMode.CavityColorMap)
                    {
                        int cavityProperty = Flags.Instance.CavityPropertyType;

                        switch (cavityProperty)
                        {
                            case 0:
                                showScale = true;
                                propertyType = ColorManager.PropertyType.Charge;
                                title = "Cavity Charge";
                                break;
                            case 1:
                                showScale = true;
                                propertyType = ColorManager.PropertyType.Hydrophobicity;
                                title = "Cavity Hydrophobicity";
                                break;
                            case 2:
                                showScale = true;
                                propertyType = ColorManager.PropertyType.AtomicNumber;
                                title = "Cavity Atomic Number";
                                break;
                            case 3:
                                showScale = true;
                                propertyType = ColorManager.PropertyType.VdWRadius;
                                title = "Cavity VdW Radius";
                                break;
                            case 4:
                                showScale = true;
                                propertyType = ColorManager.PropertyType.Occupancy;
                                title = "Cavity Occupancy";
                                break;
                            default:
                                showScale = false;
                                break;
                        }
                    }
                }
                catch { /* cavity renderer query failed — skip scale */ }
            }

            if (!showScale) return;

 // ═══ Compute aftersition (bottom-right corner) ═══
            float rts = Flags.Instance.RenderTargetScale;

            int viewportWidth = _graphicsDevice.Viewport.Width;
            int viewportHeight = _graphicsDevice.Viewport.Height;

            float scaleWidth = 200 * rts;
            float scaleHeight = 20 * rts;
            float margin = 30 * rts;
            float x = viewportWidth - scaleWidth - margin;
            float y = viewportHeight - scaleHeight - margin - 100 * rts;

            DrawColorScaleInternal(x, y, scaleWidth, scaleHeight, propertyType, title);
        }

        #endregion

        #region DrawColorScaleInternal — actual drawing

 /// <summary>
 /// Draws the color scale widget at the given aftersition.
 ///
 /// Layers (bottom to top):
 /// 1. Semi-transparent black background (rounded look via padding)
 /// 2. Horizontal gradient bar (segments via GraphiscHelper)
 /// 3. min / mid / max numeric labels
 /// 4. Title (top center)
 ///
 /// ⚠️ BUG FIX: scaleEffect is now in `using`. The original code
 /// could leak the BasicEffect if an exception was thrown
 /// between creation and disafterse.
 ///
 /// ⚠️ The background size is computed from:
 /// padX = 12% of width
 /// padY = 8% of height
 /// titleH = max(15% of height, 10×rts) + measured title height
 /// labelH = 30×rts
 /// labelPad = 15×rts
 /// These paddings ensure the background encloses the title above,
 /// the bar, and the labels below without cutting them off.
 /// </summary>
        private void DrawColorScaleInternal(
            float x, float y, float width, float height,
            ColorManager.PropertyType propertyType, string title)
        {
            SpriteFont font = GetLabelFont();
            if (_spriteBatch == null || font == null || _camera == null) return;

            float rts = Flags.Instance.RenderTargetScale;

 // ═══ Read range from ColorManager ═══
            var range = ColorManager.GetCurrentRange();
            float min = range.min;
            float max = range.max;

 // ═══ Create effect for 2D quads (background + segments) ═══
 // ⚠️ BUG FIX: now in `using` — see docstring.
            using (var scaleEffect = new BasicEffect(_graphicsDevice)
            {
                VertexColorEnabled = true,
                LightingEnabled = false,
                TextureEnabled = false,
                View = Matrix.Identity,
                World = Matrix.Identity,
                Projection = Matrix.CreateOrthographicOffCenter(
                    0, _graphicsDevice.Viewport.Width,
                    _graphicsDevice.Viewport.Height, 0,
                    0, 1000)
            })
            {
 // ═══ Compute background dimensions ═══
                float padX = width * 0.12f;
                float padY = height * 0.08f;
                float titleAreaHeight = Math.Max(height * 0.15f, 10f * rts);

                Vector2 titleSizeBg = font.MeasureString(title ?? "") * (0.45f * rts);
                titleAreaHeight += titleSizeBg.Y;

                float labelAreaHeight = 30f * rts;
                float labelAreaExtraPad = 15f * rts;

 // ═══ Background color ═══
                Vector4 bgColor = new Vector4(0, 0, 0, 0.45f);

                float bgTop = y - padY - titleAreaHeight;
                float bgHeight = height + padY + titleAreaHeight + padY
                                 + labelAreaHeight + labelAreaExtraPad;

 // ═══ Layer 1: background panel ═══
                GraphiscHelper.FiledRectangle(
                    new Vector2(x - padX, bgTop),
                    width + 2 * padX,
                    bgHeight,
                    0,
                    bgColor, bgColor,
                    scaleEffect.Projection,
                    _graphicsDevice,
                    scaleEffect);

 // ═══ Layer 2: gradient bar (segments) ═══
 // Each segment is 4×rts pixels wide; count depends on
 // total width so the bar covers the full area.
                int segWidthPx = Math.Max(1, (int)(4 * rts));
                int segments = (int)(width / segWidthPx);
                if (segments < 1) segments = 1;
                float segWidth = width / segments;

                for (int i = 0; i < segments; i++)
                {
                    float t = i / (float)segments;
                    float value = min + t * (max - min);
                    Vector4 colorVec = ColorManager.ValueToVector4(value, propertyType);

                    float segX = x + i * segWidth;
 // Last segment extends to the exact right edge — fixes
 // a sub-pixel gap due to floating-afterint rounding.
                    float segW = i == segments - 1 ? width - i * segWidth : segWidth;

                    GraphiscHelper.FiledRectangle(
                        new Vector2(segX, y),
                        segW, height, 0,
                        colorVec, colorVec,
                        scaleEffect.Projection,
                        _graphicsDevice,
                        scaleEffect);
                }
            }

 // ═══ Begin SpriteBatch for text labels ═══
            _spriteBatch.Begin(
                SpriteSortMode.BackToFront,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone);

            float labelScale = 0.40f * rts;
            float titleScale = 0.45f * rts;
            float textYOffset = 2f * rts;

            string minText = ColorManager.GetMinLabel(propertyType);
            string midText = ColorManager.GetMidLabel(propertyType);
            string maxText = ColorManager.GetMaxLabel(propertyType);
            string titleText = title;

            Vector2 minSize = font.MeasureString(minText) * labelScale;
            Vector2 midSize = font.MeasureString(midText) * labelScale;
            Vector2 maxSize = font.MeasureString(maxText) * labelScale;
            Vector2 titleSize = font.MeasureString(titleText) * titleScale;

 // ═══ Layer 3: min / mid / max labels below the bar ═══
            float labelsY = y + height + 2 * rts + textYOffset;

            _spriteBatch.DrawString(font, minText,
                new Vector2(x - minSize.X / 2, labelsY),
                XnaColor.White, 0, Vector2.Zero, labelScale, SpriteEffects.None, 0.95f);

            if (!string.IsNullOrEmpty(midText))
            {
                _spriteBatch.DrawString(font, midText,
                    new Vector2(x + width / 2 - midSize.X / 2, labelsY),
                    XnaColor.White, 0, Vector2.Zero, labelScale, SpriteEffects.None, 0.95f);
            }

            _spriteBatch.DrawString(font, maxText,
                new Vector2(x + width - maxSize.X / 2, labelsY),
                XnaColor.White, 0, Vector2.Zero, labelScale, SpriteEffects.None, 0.95f);

 // ═══ Layer 4: title above the bar ═══
            _spriteBatch.DrawString(font, titleText,
                new Vector2(x + width / 2 - titleSize.X / 2, y - titleSize.Y - 2 * rts + textYOffset),
                XnaColor.White, 0, Vector2.Zero, titleScale, SpriteEffects.None, 0.95f);

            _spriteBatch.End();
        }

        #endregion
    }
}