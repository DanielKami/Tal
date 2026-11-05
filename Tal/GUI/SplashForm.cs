// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Application splash screen with loading dots, version and Friend line.

using System.Drawing;
using System.Windows.Forms;

namespace Tal
{
    /// <summary>
    /// Startup splash. Shows splash.png (if present) and a row of dots that fill
    /// from left to right while the application starts.
    /// </summary>
    public class SplashForm : Form
    {
        private readonly Panel _dotPanel;
        private readonly System.Windows.Forms.Timer _timer;
        private int _dotCount;
        private const int MaxDots = 40;
        private const int DotSize = 8;
        private const int DotGap = 6;

        public SplashForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = false;
            TopMost = true;
            ClientSize = new Size(800, 450);
            BackColor = Color.FromArgb(10, 20, 45);

            var pic = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = LoadSplashImage()
            };
            Controls.Add(pic);

            // Footer: loading dots + version + Friend
            _dotPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(10, 20, 45)
            };
            _dotPanel.Paint += DotPanel_Paint;
            Controls.Add(_dotPanel);
            _dotPanel.BringToFront();

            _timer = new System.Windows.Forms.Timer { Interval = 40 };
            _timer.Tick += (s, e) =>
            {
                _dotCount++;
                if (_dotCount > MaxDots) _dotCount = 0;
                _dotPanel.Invalidate();
            };
            _timer.Start();
        }

        private void DotPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Loading dots (top of footer)
            int startX = 8;
            int y = 8;

            using var brushOn = new SolidBrush(Color.FromArgb(180, 200, 230));
            using var brushOff = new SolidBrush(Color.FromArgb(40, 50, 70));

            for (int i = 0; i < MaxDots; i++)
            {
                int x = startX + i * (DotSize + DotGap);
                var brush = i < _dotCount ? brushOn : brushOff;
                g.FillEllipse(brush, x, y, DotSize, DotSize);
            }

            // Version (right side, next to dots row)
            string versionText = "Version " + Flags.VERSION;
            using (var fontVer = new Font("Segoe UI", 9f, FontStyle.Regular))
            using (var brushVer = new SolidBrush(Color.FromArgb(160, 180, 210)))
            {
                var size = g.MeasureString(versionText, fontVer);
                float vx = _dotPanel.Width - size.Width - 12;
                float vy = y + (DotSize - size.Height) / 2f;
                if (vx < startX + MaxDots * (DotSize + DotGap) + 8)
                    vx = startX + MaxDots * (DotSize + DotGap) + 8;
                g.DrawString(versionText, fontVer, brushVer, vx, vy);
            }

            // Friend line at the bottom of the footer (from Flags.Friend)
            string friendText = Flags.Friend ?? "";
            if (!string.IsNullOrWhiteSpace(friendText))
            {
                using (var fontFriend = new Font("Segoe UI", 8.5f, FontStyle.Regular))
                using (var brushFriend = new SolidBrush(Color.FromArgb(140, 160, 190)))
                {
                    var size = g.MeasureString(friendText, fontFriend);
                    float ax = Math.Max(8f, (_dotPanel.Width - size.Width) / 2f);
                    float ay = _dotPanel.Height - size.Height - 6;
                    g.DrawString(friendText, fontFriend, brushFriend, ax, ay);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _timer?.Stop();
            _timer?.Dispose();
            base.OnFormClosed(e);
        }

        private static Image LoadSplashImage()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "splash.png");
            if (File.Exists(path))
                return Image.FromFile(path);
            return null;
        }
    }
}
