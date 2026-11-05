// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — custom menu renderer / styling.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal
{
    /// <summary>
    /// Niestandardowy renderer for MenuStrip — jasny motyw VS2022-style.
    /// Ensures consistent menu look with the rest of the UI (light blue selection,
    /// jasnoszare background, subtelne separators).
    /// </summary>
    internal class LightMenuRenderer : ToolStripProfessionalRenderer
    {
        // ═══════════════════════════════════════════════════════════
        // COLOURS — one place do changes palety
        // ═══════════════════════════════════════════════════════════

        private static readonly System.Drawing.Color SelectionBack =
            System.Drawing.Color.FromArgb(0, 122, 204);

        private static readonly System.Drawing.Color SelectionFore =
            System.Drawing.Color.White;

        private static readonly System.Drawing.Color TextColor =
            System.Drawing.Color.FromArgb(51, 51, 51);

        private static readonly System.Drawing.Color MenuBackground =
            System.Drawing.Color.FromArgb(240, 240, 242);

        private static readonly System.Drawing.Color SeparatorColor =
            System.Drawing.Color.FromArgb(200, 200, 200);

        // ═══════════════════════════════════════════════════════════
        // OVERRIDES
        // ═══════════════════════════════════════════════════════════

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                using (var brush = new System.Drawing.SolidBrush(SelectionBack))
                {
                    e.Graphics.FillRectangle(
                        brush,
                        new System.Drawing.Rectangle(0, 0, e.Item.Width, e.Item.Height));
                }
                e.Item.ForeColor = SelectionFore;
            }
            else
            {
                e.Item.ForeColor = TextColor;
                base.OnRenderMenuItemBackground(e);
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (var brush = new System.Drawing.SolidBrush(MenuBackground))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (var pen = new System.Drawing.Pen(SeparatorColor))
            {
                e.Graphics.DrawLine(
                    pen,
                    e.Item.Bounds.Left + 10,
                    e.Item.Bounds.Top + 2,
                    e.Item.Bounds.Right - 10,
                    e.Item.Bounds.Top + 2);
            }
        }
    }
}
