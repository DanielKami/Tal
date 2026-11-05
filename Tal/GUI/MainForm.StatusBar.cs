// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — status bar construction and SetStatus helpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal
{
    public partial class MainForm
    {
        #region Status Bar

        private void InitializeStatusBar()
        {
            _statusStrip = new StatusStrip();
            _statusStrip.BackColor = System.Drawing.Color.FromArgb(240, 240, 242);
            _statusStrip.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _statusStrip.Font = new System.Drawing.Font("Segoe UI", 9F);
            _statusStrip.SizingGrip = false;
            _statusStrip.Dock = DockStyle.Bottom;

            _statusLabelLeft = new ToolStripStatusLabel();
            _statusLabelLeft.Text = "Ready";
            _statusLabelLeft.BorderSides = ToolStripStatusLabelBorderSides.Right;
            _statusLabelLeft.BorderStyle = Border3DStyle.Etched;
            _statusLabelLeft.Padding = new Padding(5, 0, 5, 0);
            _statusStrip.Items.Add(_statusLabelLeft);

            _statusLabelCenter = new ToolStripStatusLabel();
            _statusLabelCenter.Text = "Structures: 0";
            _statusLabelCenter.BorderSides = ToolStripStatusLabelBorderSides.Right;
            _statusLabelCenter.BorderStyle = Border3DStyle.Etched;
            _statusLabelCenter.Padding = new Padding(5, 0, 5, 0);
            _statusStrip.Items.Add(_statusLabelCenter);

            _statusProgressBar = new ToolStripProgressBar();
            _statusProgressBar.Minimum = 0;
            _statusProgressBar.Maximum = 100;
            _statusProgressBar.Value = 0;
            _statusProgressBar.Width = 100;
            _statusProgressBar.Style = ProgressBarStyle.Continuous;
            _statusStrip.Items.Add(_statusProgressBar);

            _statusLabelRight = new ToolStripStatusLabel();
            _statusLabelRight.Text = "✅";
            _statusLabelRight.Padding = new Padding(10, 0, 5, 0);
            _statusStrip.Items.Add(_statusLabelRight);

            this.Controls.Add(_statusStrip);
        }

        #endregion
    }
}
