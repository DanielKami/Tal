// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// About dialog logic. Layout lives in AboutForm.Designer.cs (VS2022 style).
// The scrollable credits box is placed strictly below the copyright text so the two never overlap.

using System.Drawing;
using System.Windows.Forms;

namespace Tal
{
    /// <summary>
    /// About dialog.
    /// Version comes from Flags.VERSION.
    /// </summary>
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();

            pictureSplash.Image = LoadSplash();

            labelInfo.Text =
                "Version " + Flags.VERSION + "\r\n" +
                Flags.Friend + "\r\n\r\n" +
                "Created by\r\n" +
                "  Daniel M. Kaminski\r\n" +
                "  Lublin, Poland\r\n\r\n" +
                "© 2026 Daniel M. Kaminski\r\n" +
                "All rights reserved.\r\n" +
                "Home: https://dkami.umcs.pl/tal/";

            textBoxRefs.Text =
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
                "  • SharpDX  (MIT)";

            textBoxRefs.SelectionStart = 0;
            textBoxRefs.SelectionLength = 0;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
