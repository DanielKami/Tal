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
    /// About dialog. TAL title is large, blue and italic. Version comes from Flags.VERSION.
    /// A small splash.png is shown in the upper-right corner when present next to the executable.
    /// </summary>
    public class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "About Tal";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(500, 620);
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9f);

            // Small splash in upper-right
            var pic = new PictureBox
            {
                Size = new Size(120, 68),
                Location = new Point(ClientSize.Width - 136, 12),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(10, 20, 45)
            };
            pic.Image = LoadSplash();
            Controls.Add(pic);

            var title = new Label
            {
                Text = "TAL",
                Font = new Font("Segoe UI", 36f, FontStyle.Italic | FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                AutoSize = true,
                Location = new Point(16, 8)
            };
            Controls.Add(title);

            var sub = new Label
            {
                Text = "Crystallographic Application",
                Font = new Font("Segoe UI", 11f, FontStyle.Italic),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Location = new Point(18, 62)
            };
            Controls.Add(sub);

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
                    "Home: https://dkami.umcs.pl/tal/\r\n\r\n" +
                    "Computational package (Crystal Calculations):\r\n" +
                    "  • MACE — Multi-ACE machine-learning interatomic\r\n" +
                    "    potential (energy & forces, F = −∇E)\r\n" +
                    "  • Authors: I. Batatia, D. P. Kovács, G. N. C. Simm,\r\n" +
                    "    C. Ortner, G. Csányi\r\n" +
                    "  • Paper: arXiv:2206.07697\r\n" +
                    "  • Project: github.com/ACEsuit/mace\r\n" +
                    "  • Runtime: evaluate_energy.exe (PyTorch / xnn\r\n" +
                    "    TorchScript forward_lammps) + mace_medium.pt /\r\n" +
                    "    mace_large.pt next to TAL.exe\r\n" +
                    "  • Menu: Advanced → Crystal Calculations…\r\n\r\n" +
                    "Built with:\r\n" +
                    "  • .NET 8  © Microsoft\r\n" +
                    "  • MonoGame  (MS-PL)\r\n" +
                    "  • DockPanel Suite  (MIT)\r\n" +
                    "  • SharpDX  (MIT)",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Black,
                AutoSize = false,
                Location = new Point(18, 96),
                Size = new Size(460, 470)
            };
            Controls.Add(info);

            var ok = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(90, 28),
                Location = new Point(ClientSize.Width - 110, ClientSize.Height - 44)
            };
            Controls.Add(ok);
            AcceptButton = ok;
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
