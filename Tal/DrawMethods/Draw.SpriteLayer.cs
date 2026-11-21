// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.SpriteLayer.

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
 /// Public entry point called from GameWindow after the 3D scene.
 ///
 /// Performs null/state guards before delegating to
 /// <see cref="DrawSpriteLayerContent"/>. Returns silently if any
 /// required resource is missing — the scene still renders without
 /// the sprite layer, which is preferable to crashing mid-frame.
 ///
 /// Guards:
 /// - _spriteBatch : no 2D drawing possible without it
 /// - SpriteFont : no text labels possible without it
 /// - _graphicsDevice : no viewport / RT access without it
 /// - IsDisposed : device may be in teardown
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