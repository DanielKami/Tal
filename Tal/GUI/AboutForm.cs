// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// About dialog: large italic TAL title, version, author, credits, optional splash.

using System.Drawing;
using System.Windows.Forms;

namespace Tal
{
    /// <summary>
    /// About dialog. Layout (top to bottom, nothing overlaps):
    ///   1. header panel  - TAL title + subtitle on the left, splash.png on the right
    ///   2. summary label - version, author, copyright, home page
    ///   3. scrollable box (vertical scrollbar) - third-party projects TAL builds on
    ///   4. OK button
    /// Version comes from Flags.VERSION.
    /// </summary>
    public class AboutForm : Form
    {
        private const int Margin = 16;
        private const int HeaderHeight = 92;
        private const int SplashWidth = 120;
        private const int SplashHeight = 68;

        public AboutForm()
        {
            Text = "About Tal";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(500, 560);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9f);

            // ── 1. Header: separate panel, so the splash can never cover text below it ──
            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(ClientSize.Width, HeaderHeight),
                BackColor = Color.White
            };
            Controls.Add(header);

            var pic = new PictureBox
            {
                Size = new Size(SplashWidth, SplashHeight),
                Location = new Point(header.Width - SplashWidth - Margin, 12),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(10, 20, 45),
                Image = LoadSplash()
            };
            header.Controls.Add(pic);

            var title = new Label
            {
                Text = "TAL",
                Font = new Font("Segoe UI", 36f, FontStyle.Italic | FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                AutoSize = true,
                Location = new Point(Margin, 4)
            };
            header.Controls.Add(title);

            var sub = new Label
            {
                Text = "Crystallographic Application",
                Font = new Font("Segoe UI", 11f, FontStyle.Italic),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Location = new Point(Margin + 2, title.Bottom + 2)
            };
            header.Controls.Add(sub);
            header.Height = Math.Max(HeaderHeight, Math.Max(sub.Bottom, 12 + SplashHeight) + 8);

            // ── 2. Summary (fixed text) ──
            var info = new Label
            {
                Text =
                    "Version " + Flags.VERSION + "\r\n" +
                    Flags.Friend + "\r\n\r\n" +
                    "Created by\r\n" +
                    "  Daniel M. Kaminski\r\n" +
                    "  Lublin, Poland\r\n\r\n" +
                    "© 2026 Daniel M. Kaminski\r\n" +
                    "All rights reserved.\r\n" +
                    "Home: https://dkami.umcs.pl/tal/",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Black,
                AutoSize = false,
                Location = new Point(Margin + 2, header.Bottom + 4),
                Size = new Size(ClientSize.Width - 2 * Margin - 2, 150)
            };
            Controls.Add(info);

            // ── 3. Scrollable references (projects TAL is based on) ──
            var refsCaption = new Label
            {
                Text = "Based on / third-party projects:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(Margin + 2, info.Bottom + 4)
            };
            Controls.Add(refsCaption);

            var ok = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(90, 28),
                Location = new Point(ClientSize.Width - 90 - Margin, ClientSize.Height - 28 - 12)
            };
            Controls.Add(ok);
            AcceptButton = ok;

            int refsTop = refsCaption.Bottom + 4;
            var refs = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 248, 248),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9f),
                TabStop = false,
                Location = new Point(Margin, refsTop),
                Size = new Size(ClientSize.Width - 2 * Margin, ok.Top - 10 - refsTop),
                Text =
                    "Computational package (Crystal Calculations):\r\n" +
                    "  • MACE — Multi-ACE machine-learning interatomic potential (energy & forces, F = −∇E)\r\n" +
                    "  • Authors: I. Batatia, D. P. Kovács, G. N. C. Simm, C. Ortner, G. Csányi\r\n" +
                    "  • Paper: arXiv:2206.07697\r\n" +
                    "  • Project: github.com/ACEsuit/mace\r\n" +
                    "  • Runtime: evaluate_energy.exe (PyTorch / xnn TorchScript forward_lammps) + " +
                    "mace_medium.pt / mace_large.pt next to TAL.exe\r\n" +
                    "  • Menu: Advanced → Crystal Calculations…\r\n\r\n" +
                    "Built with:\r\n" +
                    "  • .NET 8  © Microsoft\r\n" +
                    "  • MonoGame  (MS-PL)\r\n" +
                    "  • DockPanel Suite  (MIT)\r\n" +
                    "  • SharpDX  (MIT)"
            };
            Controls.Add(refs);
            refs.SelectionStart = 0;
            refs.SelectionLength = 0;
        }

        private static Image? LoadSplash()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "splash.png");
                if (File.Exists(path))
                    return Image.FromFile(path);
            }
            catch { }
            return null;
        }
    }
}
