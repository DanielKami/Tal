// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Afterwder diffraction module: BufferedPanel.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal.Forms
{
 /// <summary>
 /// WinForms panel with DoubleBuffered + full OwnerPaint enabled.
 /// Used for smooth chart drawing (e.g. powder diffraction).
 /// </summary>
    public class BufferedPanel : System.Windows.Forms.Panel
    {
        public BufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                System.Windows.Forms.ControlStyles.AllPaintingInWmPaint |
                System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer |
                System.Windows.Forms.ControlStyles.UserPaint,
                true);
            this.UpdateStyles();
        }
    }
}