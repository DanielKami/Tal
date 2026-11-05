// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Selective symmetry replication: enable/disable individual space-group
// operations; 3D atoms appear/disappear accordingly.

using System;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Tal.GUI;

namespace Tal.Forms
{
    /// <summary>
    /// Dockable window listing every symmetry operation of the current
    /// space group. Each checkbox toggles whether that operation is used
    /// when expanding the asymmetric unit into the unit cell.
    /// </summary>
    public partial class SymmetryReplicationWindow : DockContent
    {
        private CheckedListBox _listOps;
        private Button _btnAll;
        private Button _btnNone;
        private Button _btnIdentityOnly;
        private Button _btnApply;
        private Label _lblInfo;
        private Label _lblHint;
        private bool _loading;

        public SymmetryReplicationWindow()
        {
            Text = "Symmetry Replication";
            DockAreas = DockAreas.DockRight | DockAreas.DockLeft | DockAreas.Float;
            ShowHint = DockState.DockRight;
            HideOnClose = true;
            CloseButton = true;
            CloseButtonVisible = true;
            BackColor = Color.FromArgb(248, 248, 250);
            ClientSize = new Size(320, 480);
            MinimumSize = new Size(260, 300);

            BuildUi();
        }

        private void BuildUi()
        {
            _lblHint = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(8, 6, 8, 2),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(80, 80, 90),
                Text = "Enable symmetry operations used to replicate the asymmetric unit. Uncheck to hide those images in 3D."
            };

            _lblInfo = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Padding = new Padding(8, 2, 8, 2),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 50),
                Text = "Space group: —"
            };

            var panelBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                Padding = new Padding(8)
            };

            _btnAll = MakeButton("Select all", 0);
            _btnNone = MakeButton("Select none", 1);
            _btnIdentityOnly = MakeButton("Identity only", 2);
            _btnApply = MakeButton("Apply", 3);
            _btnApply.BackColor = Color.FromArgb(70, 130, 200);
            _btnApply.ForeColor = Color.White;

            _btnAll.Click += (s, e) => SetAll(true);
            _btnNone.Click += (s, e) => SetAll(false);
            _btnIdentityOnly.Click += (s, e) => SetIdentityOnly();
            _btnApply.Click += (s, e) => ApplyAndRefresh();

            panelBottom.Controls.Add(_btnAll);
            panelBottom.Controls.Add(_btnNone);
            panelBottom.Controls.Add(_btnIdentityOnly);
            panelBottom.Controls.Add(_btnApply);

            _listOps = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                Font = new Font("Consolas", 9f),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false
            };
            _listOps.ItemCheck += ListOps_ItemCheck;

            Controls.Add(_listOps);
            Controls.Add(panelBottom);
            Controls.Add(_lblInfo);
            Controls.Add(_lblHint);
        }

        private Button MakeButton(string text, int row)
        {
            return new Button
            {
                Text = text,
                Width = 140,
                Height = 28,
                Left = 8 + (row % 2) * 148,
                Top = 8 + (row / 2) * 36,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
        }

        /// <summary>Reload list from the active molecule.</summary>
        public void ReloadFromMolecule()
        {
            _loading = true;
            _listOps.Items.Clear();

            var mol = MainForm.Instance?.GetActiveMolecule();
            if (mol == null || mol.SymmetryOperations == null || mol.SymmetryOperations.Count == 0)
            {
                _lblInfo.Text = "Space group: — (no operations)";
                _loading = false;
                return;
            }

            mol.EnsureSymmetryOpFlags();
            string sg = mol.SpaceGroupName ?? "";
            int num = mol.SpaceGroupNumber;
            _lblInfo.Text = string.IsNullOrEmpty(sg)
                ? $"Operations: {mol.SymmetryOperations.Count}"
                : $"SG: {sg}" + (num > 0 ? $" (#{num})" : "") +
                  $"  ·  {mol.SymmetryOperations.Count} ops";

            var en = Flags.Instance.EnabledSymmetryOps;
            for (int i = 0; i < mol.SymmetryOperations.Count; i++)
            {
                string label = $"{i + 1,2}. {mol.GetSymmetryOperationLabel(i)}";
                bool on = en == null || i >= en.Length || en[i];
                _listOps.Items.Add(label, on);
            }

            _loading = false;
        }

        private void ListOps_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_loading) return;
            // Defer until after check state updates
            BeginInvoke(new Action(() =>
            {
                SyncFlagsFromList();
                ApplyAndRefresh();
            }));
        }

        private void SetAll(bool value)
        {
            _loading = true;
            for (int i = 0; i < _listOps.Items.Count; i++)
                _listOps.SetItemChecked(i, value);
            _loading = false;
            SyncFlagsFromList();
            ApplyAndRefresh();
        }

        private void SetIdentityOnly()
        {
            _loading = true;
            for (int i = 0; i < _listOps.Items.Count; i++)
                _listOps.SetItemChecked(i, i == 0);
            _loading = false;
            SyncFlagsFromList();
            ApplyAndRefresh();
        }

        private void SyncFlagsFromList()
        {
            int n = _listOps.Items.Count;
            if (n == 0)
            {
                Flags.Instance.EnabledSymmetryOps = null;
                return;
            }
            var en = new bool[n];
            for (int i = 0; i < n; i++)
                en[i] = _listOps.GetItemChecked(i);
            // Never allow zero ops — force identity if all off
            bool any = false;
            for (int i = 0; i < n; i++)
                if (en[i]) { any = true; break; }
            if (!any && n > 0)
            {
                en[0] = true;
                _loading = true;
                _listOps.SetItemChecked(0, true);
                _loading = false;
            }
            Flags.Instance.EnabledSymmetryOps = en;
        }

        private void ApplyAndRefresh()
        {
            var mainForm = MainForm.Instance;
            if (mainForm == null) return;

            // Selective symmetry implies unit-cell (mode 1), not pure AU
            Flags.Instance.GenerateEquvalents = 1;
            Flags.Instance.ShowAsymmetricUnit = false;
            Flags.Instance.DrawPacking = false;

            try
            {
                mainForm.GetDisplayOptionsWindow()?.SetPresentationMode(1);
            }
            catch { /* window may not exist */ }

            var dock = mainForm.GetDockPanel();
            if (dock == null) return;

            int style = Flags.Instance.Presentation;
            foreach (var doc in dock.Documents)
            {
                if (doc is not MonoGameDocument monoDoc) continue;
                var molecule = monoDoc.GetMolecule();
                if (molecule == null || molecule.AsymmetricPart == null
                    || molecule.AsymmetricPart.Count == 0)
                    continue;

                molecule.EnsureSymmetryOpFlags();
                molecule.Presentation(1, style);
                monoDoc.LoadMolecule(molecule);
                monoDoc.ResetDrawCache();
                monoDoc.RefreshView();
            }

            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

            int on = 0;
            var en = Flags.Instance.EnabledSymmetryOps;
            if (en != null)
                foreach (bool b in en) if (b) on++;
            mainForm.SetStatus(
                $"Symmetry ops: {on}/{(en?.Length ?? 0)} active",
                $"Structures: {StructureStorage.Instance.StructureCount}",
                "🔷");
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ReloadFromMolecule();
        }
    }
}
