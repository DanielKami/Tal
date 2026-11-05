// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Bond Display settings: aromatic rings + multiple bonds.

using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    /// <summary>
    /// Display menu window: dashed aromatic rings and multi-bond sticks.
    /// </summary>
    public partial class BondDisplayWindow : DockContent
    {
        public BondDisplayWindow()
        {
            InitializeComponent();
            DockWindowConfig.SetupWindow(this, "🔗 Bond Display");
            LoadFromFlags();

            _chkAromatic.CheckedChanged += (s, e) =>
            {
                Flags.Instance.ShowAromaticRings = _chkAromatic.Checked;
                Flags.Instance.VisualRevision++;
                RefreshView();
            };

            _chkMultiple.CheckedChanged += (s, e) =>
            {
                Flags.Instance.ShowMultipleBonds = _chkMultiple.Checked;
                Flags.Instance.VisualRevision++;
                RefreshView();
            };
        }

        private void LoadFromFlags()
        {
            _chkAromatic.Checked = Flags.Instance.ShowAromaticRings;
            _chkMultiple.Checked = Flags.Instance.ShowMultipleBonds;
        }

        private void RefreshView()
        {
            var main = MainForm.Instance;
            if (main == null) return;
            var doc = main.GetActiveDocument();
            doc?.RefreshView();
        }
    }
}
