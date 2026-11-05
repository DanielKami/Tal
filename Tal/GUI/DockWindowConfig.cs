// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Shared dock window size / portion configuration.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    /// <summary>
    /// Centralna konfiguracja look i zachowania okien DockContent.
    /// All tool windows should call SetupWindow() in the constructor,
    /// that have a uniform size and are docked on the left side.
    /// </summary>
    public static class DockWindowConfig
    {
        // ⭐ Target size of all tool windows (px)
        public const int DefaultWidth = 340;
        public const int DefaultHeight = 700;

        public const int MinWidth = 320;
        public const int MinHeight = 500;

        // ⭐ Default left dock portion width (px)
        public const double DefaultDockPortionPx = 360.0;

        // ⭐ Shared VS2022-style colours
        public static readonly System.Drawing.Color BackColor =
            System.Drawing.Color.FromArgb(248, 248, 250);
        public static readonly System.Drawing.Color ForeColor =
            System.Drawing.Color.FromArgb(51, 51, 51);

        /// <summary>
        /// Sets jednolity size, obszary dokowania i hint for windows.
        /// Call w konstruktorze PO InitializeComponent(),
        /// ale BEFORE LoadFromFlags().
        /// </summary>
        public static void SetupWindow(DockContent window, string title)
        {
            if (window == null) return;

            window.Text = title;
            window.ClientSize = new System.Drawing.Size(DefaultWidth, DefaultHeight);
            window.MinimumSize = new System.Drawing.Size(MinWidth, MinHeight);
            window.DockAreas = DockAreas.DockLeft
                               | DockAreas.DockRight
                               | DockAreas.Float;
            window.ShowHint = DockState.DockLeft;
            window.CloseButton = true;
            window.CloseButtonVisible = true;
            window.BackColor = BackColor;
            window.ForeColor = ForeColor;
        }
    }
}