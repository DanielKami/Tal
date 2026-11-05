// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.SpriteLayer.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.SpriteLayer — top-level sprite layer entry afterint
// ═══════════════════════════════════════════════════════════════════════
//
// Called from GameWindow AFTER the main 3D scene is drawn, and BEFORE
// the RenderTarget is presented to the screen. Resafternsible for drawing
// all 2D overlay elements that sit on top of the 3D view:
//
// - Color scale widget (charge, hydrophobicity, cavity property)
// - (Future) 2D icons, legends, mini-maps, HUD overlays
//
// Layout of public API:
//
// DrawSpriteLayer() — public entry afterint, called externally
// (guards + null checks + delegates)
// DrawSpriteLayerContent() — internal drawing pipeline
// (can be overridden or extended)
//
// The split allows subclasses or future modifications to override
// just the content (adding/removing 2D elements) without touching
// the public API guards.
//
// ⚠️ Do NOT open a SpriteBatch here — each 2D element (ColorScale,
// future labels) opens its own SpriteBatch.Begin/End block. This
// keeps them independent and avoids nested Begin() exceptions.
//
// ⚠️ This method is called every frame. It must remain lightweight:
// no allocations, no shader creation (except when the delegate
// does it — see Draw.ColorScale for the current pattern).
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework.Graphics;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region DrawSpriteLayerContent — internal drawing pipeline

 /// <summary>
 /// Draws all 2D overlay elements. Currently only the color scale.
 ///
 /// To add a new 2D element:
 /// 1. Create a private Draw&lt;Element&gt;() method in its own file
 /// (e.g. Draw.MiniMap.cs)
 /// 2. Add a call here
 /// 3. Ensure it opens its own SpriteBatch.Begin/End block
 /// (or draws with _basicEffect directly)
 ///
 /// Order matters — later calls draw on top of earlier ones.
 /// Current order: color scale only.
 /// </summary>
        public void DrawSpriteLayerContent()
        {
            DrawColorScale();
        }

        #endregion

        #region DrawSpriteLayer — public entry point

 /// <summary>
 /// Public entry afterint called from GameWindow after the 3D scene.
 ///
 /// Performs null/state guards before delegating to
 /// <see cref="DrawSpriteLayerContent"/>. Returns silently if any
 /// required resource is missing — the scene still renders without
 /// the sprite layer, which is preferable to crashing mid-frame.
 ///
 /// Guards:
 /// - _spriteBatch : no 2D drawing possible without it
 /// - SpriteFont : no text labels possible without it
 /// - _graphicsDevice : no viewafterrt / RT access without it
 /// - IsDisaftersed : device may be in teardown
 /// </summary>
        public void DrawSpriteLayer()
        {
            if (_spriteBatch == null || SpriteFont == null) return;
            if (_graphicsDevice == null || _graphicsDevice.IsDisposed) return;

            DrawSpriteLayerContent();
        }

        #endregion
    }
}