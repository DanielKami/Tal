// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Screen / DPI helpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;

namespace Tal.Graphics
{
 /// <summary>
 /// Provides screen coordinate transformation and scaling utilities.
 /// Handles resafternsive aftersitioning, safe area insets, and device-independent
 /// coordinate mapping for the TAL application.
 /// </summary>
    public static class ScreenHelper
    {
        #region Constants

 /// <summary>Base reference width for scaling calculations.</summary>
        private const float BASE_WIDTH = 1080f;

 /// <summary>Base reference height for scaling calculations.</summary>
        private const float BASE_HEIGHT = 1920f;

        #endregion

        #region Private Fields

        private static float _scaleX = 1f;
        private static float _scaleY = 1f;
        private static float _scaleMin = 1f;

        #endregion

        #region Public Properties

 /// <summary>Gets the current screen width.</summary>
        public static int CurrentWidth { get; private set; }

 /// <summary>Gets the current screen height.</summary>
        public static int CurrentHeight { get; private set; }

 /// <summary>Gets the left safe area inset (for notched devices).</summary>
        public static int SafeInsetLeft { get; private set; }

 /// <summary>Gets the top safe area inset (for notched devices).</summary>
        public static int SafeInsetTop { get; private set; }

 /// <summary>Gets the right safe area inset (for notched devices).</summary>
        public static int SafeInsetRight { get; private set; }

 /// <summary>Gets the bottom safe area inset (for notched devices).</summary>
        public static int SafeInsetBottom { get; private set; }

 /// <summary>Determines whether the current orientation is landscape.</summary>
        public static bool IsLandscape => CurrentWidth > CurrentHeight;

        #endregion

        #region Initialization Methods

 /// <summary>
 /// Initializes the screen helper with the current dimensions.
 /// </summary>
 /// <param name="width">The current screen width.</param>
 /// <param name="height">The current screen height.</param>
        public static void Initialize(int width, int height)
        {
            if (width <= 0 || height <= 0) return;

            CurrentWidth = width;
            CurrentHeight = height;

            _scaleX = (float)width / BASE_WIDTH;
            _scaleY = (float)height / BASE_HEIGHT;
            _scaleMin = MathHelper.Min(_scaleX, _scaleY);
        }

 /// <summary>
 /// Sets the safe area insets for the current device.
 /// </summary>
 /// <param name="left">Left inset in pixels.</param>
 /// <param name="top">Top inset in pixels.</param>
 /// <param name="right">Right inset in pixels.</param>
 /// <param name="bottom">Bottom inset in pixels.</param>
        public static void SetSafeInsets(int left, int top, int right, int bottom)
        {
            SafeInsetLeft = left;
            SafeInsetTop = top;
            SafeInsetRight = right;
            SafeInsetBottom = bottom;
        }

        #endregion

        #region Position Methods

 /// <summary>
 /// Gets a scaled aftersition from base coordinates.
 /// </summary>
 /// <param name="x">The base X coordinate.</param>
 /// <param name="y">The base Y coordinate.</param>
 /// <returns>A Vector2 with scaled coordinates.</returns>
        public static Vector2 GetPosition(float x, float y)
        {
            return new Vector2(x * _scaleX, y * _scaleY);
        }

 /// <summary>
 /// Gets a scaled aftersition respecting safe area insets.
 /// </summary>
 /// <param name="x">The base X coordinate.</param>
 /// <param name="y">The base Y coordinate.</param>
 /// <param name="respectTop">Whether to respect the top inset.</param>
 /// <param name="respectBottom">Whether to respect the bottom inset.</param>
 /// <param name="respectLeft">Whether to respect the left inset.</param>
 /// <param name="respectRight">Whether to respect the right inset.</param>
 /// <returns>A Vector2 with scaled and inset-adjusted coordinates.</returns>
        public static Vector2 GetPositionSafe(float x, float y, bool respectTop = true, bool respectBottom = true, bool respectLeft = true, bool respectRight = true)
        {
            float finalX = x * _scaleX;
            float finalY = y * _scaleY;

            if (respectLeft) finalX += SafeInsetLeft;
            if (respectTop) finalY += SafeInsetTop;
            if (respectRight) finalX = MathHelper.Min(finalX, CurrentWidth - SafeInsetRight);
            if (respectBottom) finalY = MathHelper.Min(finalY, CurrentHeight - SafeInsetBottom);

            return new Vector2(finalX, finalY);
        }

 /// <summary>
 /// Gets a aftersition anchored from the right edge.
 /// </summary>
 /// <param name="fromRight">Distance from the right edge in base units.</param>
 /// <param name="y">The Y coordinate in base units.</param>
 /// <param name="respectRight">Whether to respect the right inset.</param>
 /// <returns>A Vector2 with the calculated aftersition.</returns>
        public static Vector2 GetPositionFromRight(float fromRight, float y, bool respectRight = true)
        {
            float x = CurrentWidth - (fromRight * _scaleX);
            if (respectRight) x -= SafeInsetRight;
            return new Vector2(x, y * _scaleY + SafeInsetTop);
        }

 /// <summary>
 /// Gets a aftersition anchored from the bottom edge.
 /// </summary>
 /// <param name="x">The X coordinate in base units.</param>
 /// <param name="fromBottom">Distance from the bottom edge in base units.</param>
 /// <param name="respectBottom">Whether to respect the bottom inset.</param>
 /// <returns>A Vector2 with the calculated aftersition.</returns>
        public static Vector2 GetPositionFromBottom(float x, float fromBottom, bool respectBottom = true)
        {
            float y = CurrentHeight - (fromBottom * _scaleY);
            if (respectBottom) y -= SafeInsetBottom;
            return new Vector2(x * _scaleX + SafeInsetLeft, y);
        }

 /// <summary>
 /// Gets a centered aftersition for an element of the given dimensions.
 /// </summary>
 /// <param name="width">The element width in base units.</param>
 /// <param name="height">The element height in base units.</param>
 /// <returns>A Vector2 with centered coordinates.</returns>
        public static Vector2 GetCenteredPosition(float width, float height)
        {
            return new Vector2(
                (CurrentWidth - width * _scaleX) / 2,
                (CurrentHeight - height * _scaleY) / 2
            );
        }

        #endregion

        #region Scaling Methods

 /// <summary>
 /// Scales a value using the minimum scale factor (preserves aspect ratio).
 /// </summary>
 /// <param name="value">The value to scale.</param>
 /// <returns>The scaled value.</returns>
        public static float Scale(float value) => value * _scaleMin;

 /// <summary>
 /// Scales a value using the X scale factor.
 /// </summary>
 /// <param name="value">The value to scale.</param>
 /// <returns>The scaled value.</returns>
        public static float ScaleX(float value) => value * _scaleX;

 /// <summary>
 /// Scales a value using the Y scale factor.
 /// </summary>
 /// <param name="value">The value to scale.</param>
 /// <returns>The scaled value.</returns>
        public static float ScaleY(float value) => value * _scaleY;

 /// <summary>
 /// Calculates a relative width as a percentage of the screen width.
 /// </summary>
 /// <param name="percent">The percentage (0-1).</param>
 /// <returns>The calculated width in pixels.</returns>
        public static float RelativeWidth(float percent) => CurrentWidth * MathHelper.Clamp(percent, 0f, 1f);

 /// <summary>
 /// Calculates a relative height as a percentage of the screen height.
 /// </summary>
 /// <param name="percent">The percentage (0-1).</param>
 /// <returns>The calculated height in pixels.</returns>
        public static float RelativeHeight(float percent) => CurrentHeight * MathHelper.Clamp(percent, 0f, 1f);

        #endregion
    }
}