// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Display options window — presentation modes and visibility flags.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;
using Tal.Calculate;

namespace Tal.Forms
{
    public partial class DisplayOptionsWindow : DockContent
    {
        #region Fields

        // ⭐ UI flag: true when checkboxes are changed programmatically (guards against recursion)
        private bool _updatingMode = false;

        #endregion

        #region Constructor

        public DisplayOptionsWindow()
        {
            InitializeComponent();
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Text = "Display Options";
            this.DockAreas = DockAreas.DockBottom | DockAreas.DockTop | DockAreas.Float;
            this.BackColor = System.Drawing.Color.FromArgb(248, 248, 250);
            this.ClientSize = new System.Drawing.Size(500, 210);
            this.MinimumSize = new System.Drawing.Size(480, 190);
            this.CloseButton = true;
            this.CloseButtonVisible = true;
            // Hide instead of dispose on X — reopening keeps state and avoids DockHandler NRE
            this.HideOnClose = true;
            this.ShowHint = DockState.DockBottom;

            // ⭐ EVENT WIRING
            this._chkPacking.CheckedChanged += ChkPacking_CheckedChanged;
            this._chkShortContact.CheckedChanged += ChkShortContact_CheckedChanged;
            this._chkAsymmetricUnit.CheckedChanged += ChkAsymmetricUnit_CheckedChanged;
                this._chkAsymmetricUnit.MouseDown += _chkAsymmetricUnit_MouseDown;
            this._chkAsymmetricUnit.MouseDown += _chkAsymmetricUnit_MouseDown;
            this._chkHBond.CheckedChanged += ChkHBond_CheckedChanged;
            this._chkAutoCentre.CheckedChanged += ChkAutoCentre_CheckedChanged;
            this._chkShowHydrogens.CheckedChanged += ChkShowHydrogens_CheckedChanged;
            this._chkDepthCue.CheckedChanged += ChkDepthCue_CheckedChanged;
            this._chkShowCellAxes.CheckedChanged += ChkShowCellAxes_CheckedChanged;
            this._chkZClipping.CheckedChanged += ChkZClipping_CheckedChanged;
            this._chkLabelAtoms.CheckedChanged += ChkLabelAtoms_CheckedChanged;
            this._chkStereo.CheckedChanged += ChkStereo_CheckedChanged;

            this._btnReset.Click += BtnReset_Click;
            this._btnContacts.Click += BtnContacts_Click;
            this._btnMoreInfo.Click += BtnMoreInfo_Click;
            this._btnPowder.Click += BtnPowder_Click;

            this._chkHBond.MouseDown += _chkHBond_MouseDown;
            this._chkShortContact.MouseDown += _chkShortContact_MouseDown;
            this._chkDepthCue.MouseDown += _chkDepthCue_MouseDown;

            var tip = new ToolTip();
            tip.SetToolTip(_chkDepthCue,
                "Left click: toggle Depth Cue\nRight click: open Depth Cue settings");

            LoadFromFlags();
        }

        #endregion

        #region Load from Flags

        private void LoadFromFlags()
        {
            _chkPacking.Checked = Flags.Instance.DrawPacking || Flags.Instance.GenerateEquvalents == 2;
            _chkShortContact.Checked = Flags.Instance.ShortContact;
            // Mode is source of truth: 0 = asymmetric ON, 1/2 = OFF
            _chkAsymmetricUnit.Checked = Flags.Instance.GenerateEquvalents == 0;
            _chkHBond.Checked = Flags.Instance.ShowHBonds;
            _chkAutoCentre.Checked = Flags.Instance.AutoCentre;
            _chkShowHydrogens.Checked = Flags.Instance.ShowHydrogens;
            _chkDepthCue.Checked = Flags.Instance.DepthCue;
            _chkShowCellAxes.Checked = Flags.Instance.Draw_cell;
            _chkZClipping.Checked = Flags.Instance.ZClipping;
            _chkLabelAtoms.Checked = Flags.Instance.labels;
            _chkStereo.Checked = Flags.Instance.DrawStereo;
        }

        #endregion

        #region Public API

        public bool IsAutoCentreChecked() => _chkAutoCentre.Checked;

        public void SyncDepthCueCheckbox()
        {
            if (_chkDepthCue == null || _chkDepthCue.IsDisposed)
                return;

            if (_chkDepthCue.Checked == Flags.Instance.DepthCue)
                return;

            _chkDepthCue.CheckedChanged -= ChkDepthCue_CheckedChanged;
            _chkDepthCue.Checked = Flags.Instance.DepthCue;
            _chkDepthCue.CheckedChanged += ChkDepthCue_CheckedChanged;
        }

        public void SetAutoCentre(bool value) => _chkAutoCentre.Checked = value;

        public void SetPresentationMode(int mode)
        {
            // Do not bail out when _updatingMode is set elsewhere — MACE needs a reliable sync.
            int oldMode = Flags.Instance.GenerateEquvalents;
            bool needRebuild = oldMode != mode;

            System.Diagnostics.Debug.WriteLine(
                $"[DisplayOptions] SetPresentationMode: {oldMode} -> {mode} (rebuild={needRebuild})");

            try
            {
                _updatingMode = true;

                Flags.Instance.GenerateEquvalents = mode;
                Flags.Instance.DrawPacking = (mode == 2);
                // Asymmetric checkbox tracks mode 0 only (not a free-standing flag after mode switch)
                Flags.Instance.ShowAsymmetricUnit = (mode == 0);

                // Detach handlers so Checked= assignment cannot re-enter mode logic
                _chkAsymmetricUnit.CheckedChanged -= ChkAsymmetricUnit_CheckedChanged;
                _chkPacking.CheckedChanged -= ChkPacking_CheckedChanged;
                try
                {
                    _chkAsymmetricUnit.Checked = (mode == 0);
                    _chkPacking.Checked = (mode == 2);
                }
                finally
                {
                    _chkAsymmetricUnit.CheckedChanged += ChkAsymmetricUnit_CheckedChanged;
                    _chkPacking.CheckedChanged += ChkPacking_CheckedChanged;
                }

                if (mode == 2)
                {
                    EnableAllSymmetryOperations();
                    CloseSymmetryReplicationWindow();
                }

                foreach (var form in Application.OpenForms)
                {
                    if (form is PackingWindow packingWin)
                    {
                        packingWin.SetPackingChecked(mode == 2);
                        packingWin.UpdateFromFlags();
                    }
                }
            }
            finally
            {
                _updatingMode = false;
            }

            if (needRebuild)
            {
                RefreshStructure();
                RefreshView();
            }

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm != null)
            {
                string modeName = mode == 0 ? "Asymmetric"
                                : mode == 1 ? "Symmetry"
                                : "Packing";
                mainForm.SetStatus($"Mode: {modeName}",
                    $"Structures: {StructureStorage.Instance.StructureCount}", "🔄");
            }
        }

        public void SetPacking(bool value) { _chkPacking.Checked = value; }
        public void SetShortContact(bool value) { _chkShortContact.Checked = value; }

        /// <summary>
        /// Set checkbox + flags without running ChkShortContact_CheckedChanged
        /// (avoids double FindAllShortContacts when called from menu/Apply).
        /// </summary>
        public void SetShortContactSilent(bool value)
        {
            try
            {
                _updatingMode = true;
                _chkShortContact.CheckedChanged -= ChkShortContact_CheckedChanged;
                _chkShortContact.Checked = value;
                Flags.Instance.ShortContact = value;
                Flags.Instance.ShortContactsActive = value;
            }
            finally
            {
                _chkShortContact.CheckedChanged += ChkShortContact_CheckedChanged;
                _updatingMode = false;
            }
        }

        public void SetAsymmetricUnit(bool value)
        {
            try
            {
                _updatingMode = true;
                _chkAsymmetricUnit.CheckedChanged -= ChkAsymmetricUnit_CheckedChanged;
                _chkAsymmetricUnit.Checked = value;
                Flags.Instance.ShowAsymmetricUnit = value;
            }
            finally
            {
                _chkAsymmetricUnit.CheckedChanged += ChkAsymmetricUnit_CheckedChanged;
                _updatingMode = false;
            }
        }
        public void SetHBond(bool value) { _chkHBond.Checked = value; }

        public void SetShowHydrogens(bool value) { _chkShowHydrogens.Checked = value; }
        public void SetDepthCue(bool value) { _chkDepthCue.Checked = value; }
        public void SetShowCellAxes(bool value)
        {
            try
            {
                _updatingMode = true;
                _chkShowCellAxes.CheckedChanged -= ChkShowCellAxes_CheckedChanged;
                _chkShowCellAxes.Checked = value;
                Flags.Instance.Draw_cell = value;
            }
            finally
            {
                _chkShowCellAxes.CheckedChanged += ChkShowCellAxes_CheckedChanged;
                _updatingMode = false;
            }
        }

        public void SetZClipping(bool value) { _chkZClipping.Checked = value; }
        public void SetLabelAtoms(bool value) { _chkLabelAtoms.Checked = value; }
        public void SetStereo(bool value) { _chkStereo.Checked = value; }

        public bool IsPackingChecked() => _chkPacking.Checked;
        public bool IsAsymmetricChecked() => _chkAsymmetricUnit.Checked;

        #endregion

        #region Public Reset

        public void ResetForNewStructure()
        {
            System.Diagnostics.Debug.WriteLine(
                "[DisplayOptions] ResetForNewStructure — synchronizing UI with Flags");

            _updatingMode = true;
            try
            {
                LoadFromFlags();

                if (_lblStatus != null)
                    _lblStatus.Text = "Press the left mouse button and move the mouse to rotate the structure.";
            }
            finally
            {
                _updatingMode = false;
            }
        }

        #endregion

        #region Event Handlers — Mode (Packing / Asymmetric)

        private void ChkPacking_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingMode) return;

            Flags.Instance.DrawPacking = _chkPacking.Checked;

            if (_chkPacking.Checked)
            {
                // Packing always uses the FULL unit cell (all symmetry ops)
                EnableAllSymmetryOperations();
                CloseSymmetryReplicationWindow();
                SetPresentationMode(2);
            }
            else
            {
                if (!_chkAsymmetricUnit.Checked)
                {
                    SetPresentationMode(1);
                }
            }
        }

        /// <summary>
        /// Force every space-group operation back on (full cell content).
        /// </summary>
        private static void EnableAllSymmetryOperations()
        {
            var en = Flags.Instance.EnabledSymmetryOps;
            if (en == null || en.Length == 0)
            {
                // null already means "all on" — still try to size from active molecule
                var mol = MainForm.Instance?.GetActiveMolecule();
                if (mol != null)
                {
                    mol.EnsureSymmetryOpFlags();
                    en = Flags.Instance.EnabledSymmetryOps;
                }
            }
            if (en != null)
            {
                for (int i = 0; i < en.Length; i++)
                    en[i] = true;
                Flags.Instance.EnabledSymmetryOps = en;
            }
        }

        /// <summary>
        /// Hide Symmetry Replication dock when Packing takes over.
        /// </summary>
        private static void CloseSymmetryReplicationWindow()
        {
            try
            {
                var mainForm = MainForm.Instance
                               ?? Application.OpenForms["MainForm"] as MainForm;
                if (mainForm == null) return;
                var win = mainForm.GetSymmetryReplicationWindow();
                if (win == null || win.IsDisposed) return;
                if (win.Visible || win.DockState != WeifenLuo.WinFormsUI.Docking.DockState.Unknown)
                    win.Hide();
            }
            catch { /* ignore */ }
        }


        /// <summary>
        /// Right-click on Asymmetric Unit → Symmetry Replication (dock right).
        /// Toggle individual space-group operations for 3D atom images.
        /// </summary>
        private void _chkAsymmetricUnit_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            var mainForm = MainForm.Instance
                           ?? Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var win = mainForm.GetSymmetryReplicationWindow();
            if (win == null) return;

            var dock = mainForm.GetDockPanel();
            if (dock == null) return;

            if (win.DockPanel == null || win.IsHidden)
                win.Show(dock, DockState.DockRight);
            else
                win.Activate();

            if (mainForm.Width > 0)
                dock.DockRightPortion = Math.Max(dock.DockRightPortion, 320.0 / mainForm.Width);

            // Leaving Packing so selective ops make sense
            if (Flags.Instance.DrawPacking || Flags.Instance.GenerateEquvalents == 2)
            {
                Flags.Instance.DrawPacking = false;
                _updatingMode = true;
                _chkPacking.Checked = false;
                _updatingMode = false;
                Flags.Instance.GenerateEquvalents = 1;
            }

            win.ReloadFromMolecule();
            mainForm.SetStatus("Symmetry Replication opened",
                $"Structures: {StructureStorage.Instance.StructureCount}", "🔷");
        }

        private void ChkAsymmetricUnit_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingMode) return;

            Flags.Instance.ShowAsymmetricUnit = _chkAsymmetricUnit.Checked;

            if (_chkAsymmetricUnit.Checked)
            {
                SetPresentationMode(0);
            }
            else
            {
                if (!_chkPacking.Checked)
                {
                    SetPresentationMode(1);
                }
            }
        }

        #endregion

        #region Event Handlers — Short Contacts / H-Bonds

        /// <summary>
        /// ⭐ ON:  FindAllShortContacts (if no danych) + switch na Expand Contacts.
        /// ⭐ OFF: molecule.TurnOffShortContacts() + return to Rotate.
        /// </summary>
        private void ChkShortContact_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingMode) return;

            Flags.Instance.ShortContact = _chkShortContact.Checked;
            Flags.Instance.ShortContactsActive = _chkShortContact.Checked;

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null)
            {
                Flags.Instance.VisualRevision++;
                Flags.Instance.MarkDirty();
                RefreshView();
                return;
            }

            if (_chkShortContact.Checked)
            {
                // ⭐ ON — count if no danych
                var molecule = mainForm.GetActiveMolecule();

                bool hasContactData = false;
                if (molecule != null)
                {
                    for (int i = 0; i < molecule.NumberOfAtoms; i++)
                    {
                        if (molecule.atom[i].UseForShortContacts &&
                            molecule.atom[i].ContactCount > 0)
                        {
                            hasContactData = true;
                            break;
                        }
                    }
                }

                if (!hasContactData)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[DisplayOptions] Short Contacts ON — calculating");

                    mainForm.FindAllShortContacts();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[DisplayOptions] Short Contacts ON — data exists");
                }
            }
            else
            {
                // ⭐ OFF — to samo co "Remove hanging contacts"
                var molecule = mainForm.GetActiveMolecule();
                if (molecule != null)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[DisplayOptions] Short Contacts OFF — physical remove");

                    molecule.TurnOffShortContacts();

                    var doc = mainForm.GetActiveDocument();
                    doc?.LoadMolecule(molecule);
                }
            }

            // ⭐⭐⭐ AT THE END: SWITCH PICKING MODE ⭐⭐⭐
            //    ON  → Expand Contacts (index 3)
            //    OFF → Rotate (index 0)
            //    (SwitchToPickingMode NOT robi nic when index taki sam — no recursion)
            if (_chkShortContact.Checked)
            {
                mainForm.SwitchToPickingMode(3);
            }
            else
            {
                mainForm.SwitchToPickingMode(0);
            }

            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

            RefreshView();

            mainForm.SetStatus(
                _chkShortContact.Checked
                    ? "Short Contacts ON — Expand Contacts mode"
                    : "Short Contacts OFF — Rotate mode",
                $"Structures: {StructureStorage.Instance.StructureCount}",
                _chkShortContact.Checked ? "🔗" : "🔄");
        }

        /// <summary>
        /// ⭐ ON:  Calculate_Hbonds3D (przelicza H-bonds).
        /// ⭐ OFF: molecule.TurnOffHBonds() — physically removes acceptors from neighbouring cells.
        /// </summary>
        private void ChkHBond_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingMode) return;

            Flags.Instance.ShowHBonds = _chkHBond.Checked;
            Flags.Instance.Hydrogen_bonds1 = _chkHBond.Checked;

            // Same pipeline as HBondSettingsWindow.Apply / RefreshStructure:
            // every document, bonds → H-bonds → matrices → LoadMolecule → RefreshView.
            // (Previously only the active molecule was updated, and without
            // Calculate_Bonds3D first lattice D–H links could be stale.)
            var mainForm = MainForm.Instance
                           ?? Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null)
            {
                Flags.Instance.VisualRevision++;
                Flags.Instance.MarkDirty();
                RefreshView();
                return;
            }

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel != null)
            {
                foreach (var doc in dockPanel.Documents)
                {
                    if (doc is not MonoGameDocument monoDoc) continue;
                    var molecule = monoDoc.GetMolecule();
                    if (molecule == null || molecule.NumberOfAtoms == 0) continue;

                    if (_chkHBond.Checked)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "[DisplayOptions] H-Bonds ON — recalculating (all docs)");
                        // Fresh covalent graph (needed before H-bond donor→H walk)
                        molecule.Calculate_Bonds3D();
                        molecule.Calculate_Hbonds3D();
                        molecule.PrecomputeBondMatrices();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "[DisplayOptions] H-Bonds OFF — physical remove");
                        molecule.TurnOffHBonds();
                    }

                    monoDoc.LoadMolecule(molecule);
                    monoDoc.ResetDrawCache();
                    monoDoc.RefreshView();
                }
            }

            // Refresh open Info window so H-bond tables update
            RefreshStructureInfo(mainForm);

            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

            string mode = Flags.Instance.GenerateEquvalents == 0 ? "Asymmetric"
                        : Flags.Instance.GenerateEquvalents == 1 ? "Symmetry"
                        : "Packing";

            mainForm.SetStatus(
                _chkHBond.Checked
                    ? $"H-Bonds ON ({mode})"
                    : $"H-Bonds OFF ({mode})",
                $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
        }

        #endregion

        #region Event Handlers — Other Checkboxes

        private void ChkAutoCentre_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingMode) return;
            Flags.Instance.AutoCentre = _chkAutoCentre.Checked;

            RefreshStructure();
            RefreshView();
        }

        private void ChkShowHydrogens_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingMode) return;
            Flags.Instance.ShowHydrogens = _chkShowHydrogens.Checked;
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
            // Keep Display → menu item in sync
            MainForm.Instance?.NotifyHydrogensVisibilityChanged();
            RefreshView();
        }

        private void ChkDepthCue_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingMode) return;
            Flags.Instance.DepthCue = _chkDepthCue.Checked;
            RefreshView();
        }

        private void ChkShowCellAxes_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingMode) return;
            Flags.Instance.Draw_cell = _chkShowCellAxes.Checked;
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
            MainForm.Instance?.NotifyUnitCellVisibilityChanged();
            RefreshView();
        }

        private void ChkZClipping_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingMode) return;
            Flags.Instance.ZClipping = _chkZClipping.Checked;
            RefreshView();
        }

        private void ChkLabelAtoms_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingMode) return;

            var mainForm = MainForm.Instance;
            var molecule = mainForm?.GetActiveMolecule();

            if (_chkLabelAtoms.Checked)
            {
                if (molecule != null && molecule.NumberSelected > 0)
                {
                    int count = 0;
                    for (int i = 0; i < molecule.NumberOfAtoms; i++)
                    {
                        if (molecule.atom[i].selected)
                        {
                            molecule.atom[i].show_label = true;
                            molecule.atom[i].selected = false;
                            molecule.atom[i].SelectionOrder = 0;
                            count++;
                        }
                    }
                    molecule.NumberSelected = 0;

                    Flags.Instance.ShowLabelsFor = 2;
                    Flags.Instance.labels = true;

                    mainForm?.SetStatus($"Labeled {count} atoms (selection cleared)",
                                        $"Structures: {StructureStorage.Instance.StructureCount}",
                                        "🏷️");
                }
                else
                {
                    Flags.Instance.ShowLabelsFor = 0;
                    Flags.Instance.labels = true;
                }
            }
            else
            {
                Flags.Instance.labels = false;

                if (molecule != null)
                {
                    for (int i = 0; i < molecule.NumberOfAtoms; i++)
                    {
                        molecule.atom[i].show_label = false;
                    }
                }

                Flags.Instance.ShowLabelsFor = 3;
            }

            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
            RefreshView();
        }

        private void ChkStereo_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingMode) return;
            Flags.Instance.DrawStereo = _chkStereo.Checked;
            RefreshView();
        }

        #endregion

        #region Context Menu Handlers

        private void _chkHBond_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var mainForm = Application.OpenForms["MainForm"] as MainForm;
                if (mainForm != null)
                {
                    var settingsWin = mainForm.GetHBondSettingsWindow();

                    if (settingsWin.IsHidden)
                        settingsWin.Show(mainForm.GetDockPanel(), DockState.DockLeft);
                    else
                        settingsWin.Activate();

                    mainForm.GetDockPanel().DockLeftPortion =
                        320.0 / Math.Max(mainForm.Width, 1);
                    mainForm.SetStatus("H-Bond Settings opened",
                        $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
                }
            }
        }

        private void _chkShortContact_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var mainForm = Application.OpenForms["MainForm"] as MainForm;
                if (mainForm != null)
                {
                    var win = mainForm.GetShortContactsWindow();

                    if (win.IsHidden)
                        win.Show(mainForm.GetDockPanel(), DockState.DockLeft);
                    else
                        win.Activate();

                    mainForm.GetDockPanel().DockLeftPortion =
                        350.0 / Math.Max(mainForm.Width, 1);
                    mainForm.SetStatus("Short Contacts opened",
                        $"Structures: {StructureStorage.Instance.StructureCount}", "📏");
                }
            }
        }

        private void _chkDepthCue_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var mainForm = MainForm.Instance;
            if (mainForm == null)
                return;

            var win = mainForm.GetDepthCueWindow();
            if (win == null)
                return;

            win.UpdateFromFlags();

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel == null)
                return;

            if (win.IsHidden)
                win.Show(dockPanel, DockState.DockLeft);
            else
                win.Activate();

            dockPanel.DockLeftPortion = 340.0 / Math.Max(mainForm.Width, 1);
            dockPanel.Refresh();

            mainForm.SetStatus("Depth Cue settings opened",
                $"Structures: {StructureStorage.Instance.StructureCount}", "🌫️");
        }

        #endregion

        #region Event Handlers — Buttons

        private void BtnReset_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[DisplayOptions] Reset clicked");

            UnsubscribeEvents();

            try
            {
                _updatingMode = true;

                // Defaults must match Flags property initializers
                _chkPacking.Checked = false;
                _chkShortContact.Checked = false;
                _chkAsymmetricUnit.Checked = true;   // default ShowAsymmetricUnit = true
                _chkHBond.Checked = false;           // default ShowHBonds = false
                _chkAutoCentre.Checked = true;
                _chkShowHydrogens.Checked = true;    // default ShowHydrogens = true
                _chkDepthCue.Checked = false;
                _chkShowCellAxes.Checked = true;     // default Draw_cell = true
                _chkZClipping.Checked = false;
                _chkLabelAtoms.Checked = false;
                _chkStereo.Checked = false;

                Flags.Instance.DrawPacking = false;
                Flags.Instance.ShortContact = false;
                Flags.Instance.ShortContactsActive = false;
                Flags.Instance.ShowAsymmetricUnit = true;
                Flags.Instance.ShowHBonds = false;
                Flags.Instance.Hydrogen_bonds1 = false;
                Flags.Instance.Hydrogen_bonds2 = false;
                Flags.Instance.ShowHydrogenBonds = false;
                Flags.Instance.AutoCentre = true;
                Flags.Instance.ShowHydrogens = true;
                Flags.Instance.DepthCue = false;
                Flags.Instance.Draw_cell = true;
                Flags.Instance.ZClipping = false;
                Flags.Instance.labels = false;
                Flags.Instance.DrawStereo = false;
                Flags.Instance.GenerateEquvalents = 0;  // asymmetric mode
                Flags.Instance.LabelProperty = 0;

                Flags.Instance.repeat_x = 1;
                Flags.Instance.repeat_y = 1;
                Flags.Instance.repeat_z = 1;
                Flags.Instance.repeatOld_x = 1;
                Flags.Instance.repeatOld_y = 1;
                Flags.Instance.repeatOld_z = 1;
                Flags.Instance.repeat_p_x = 1;
                Flags.Instance.repeat_p_y = 1;
                Flags.Instance.repeat_p_z = 1;
                Flags.Instance.repeat2_p_x = 1;
                Flags.Instance.repeat2_p_y = 1;
                Flags.Instance.repeat2_p_z = 1;
                Flags.Instance.repeatOld2_x = 1;
                Flags.Instance.repeatOld2_y = 1;
                Flags.Instance.repeatOld2_z = 1;

                Flags.Instance.ShiftNegativeX = 0f;
                Flags.Instance.ShiftPositiveX = 1f;
                Flags.Instance.ShiftNegativeY = 0f;
                Flags.Instance.ShiftPositiveY = 1f;
                Flags.Instance.ShiftNegativeZ = 0f;
                Flags.Instance.ShiftPositiveZ = 1f;
            }
            finally
            {
                _updatingMode = false;
                SubscribeEvents();
            }

            foreach (var form in Application.OpenForms)
            {
                if (form is PackingWindow packingWin)
                {
                    packingWin.SetPackingChecked(false);
                    packingWin.UpdateFromFlags();
                }
            }

            CleanupExpandedAtomsPartial();

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            var molecule = mainForm?.GetActiveMolecule();
            if (molecule != null)
            {
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    molecule.atom[i].show_label = false;
                }
            }

            RefreshView();

            mainForm?.SetStatus("Structure reset to defaults (asymmetric unit)",
                    $"Structures: {StructureStorage.Instance.StructureCount}", "🔄");

            System.Diagnostics.Debug.WriteLine($"[DisplayOptions] Reset completed");
        }

        private void CleanupExpandedAtomsPartial()
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel == null) return;

            System.Diagnostics.Debug.WriteLine(
                "[DisplayOptions] CleanupExpandedAtomsPartial — START");

            foreach (var doc in dockPanel.Documents.ToList())
            {
                if (doc is MonoGameDocument monoDoc)
                {
                    var molecule = monoDoc.GetMolecule();
                    if (molecule == null) continue;

                    // 1. Physically drop H-bond / short-contact neighbor atoms
                    molecule.TurnOffShortContacts();
                    molecule.TurnOffHBonds();

                    // 2. Drop expand-contact extras (not tracked by HBond lists)
                    molecule.RemoveHangingExpandedAtoms();

                    // 3. Full rebuild from asymmetric unit in the current mode
                    //    (GenerateEquvalents already set by BtnReset — usually 0).
                    //    Presentation() rebuilds atom[] cleanly — leftover atoms gone.
                    int mode = Flags.Instance.GenerateEquvalents;
                    int style = Flags.Instance.Presentation;
                    PresentationKeepingForces(molecule, mode, style);

                    monoDoc.LoadMolecule(molecule);

                    var renderer = monoDoc.GetCavityRenderer();
                    if (renderer != null && renderer.HasCavities)
                    {
                        renderer.BuildRepeatedCavities();
                        renderer.SetMoleculeShift(molecule.MoleculeShift);
                    }

                    monoDoc.RefreshView();

                    System.Diagnostics.Debug.WriteLine(
                        $"[DisplayOptions] CleanupExpandedAtomsPartial: " +
                        $"reset on '{monoDoc.Text}' atoms={molecule.NumberOfAtoms}");
                }
            }

            System.Diagnostics.Debug.WriteLine(
                "[DisplayOptions] CleanupExpandedAtomsPartial — END");
        }

        private void BtnContacts_Click(object sender, EventArgs e)
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            mainForm?.SetStatus("Opening Contacts...", "Structures: 0", "📞");
        }

        private void BtnMoreInfo_Click(object sender, EventArgs e)
        {
            MainForm.Instance?.OnStructureInfo();
        }

        private void BtnPowder_Click(object sender, EventArgs e)
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel == null) return;

            var activeDoc = dockPanel.ActiveDocument as MonoGameDocument;
            if (activeDoc == null)
            {
                MessageBox.Show("Open a structure before starting powder diffraction.",
                    "Brak struktury", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var molecule = activeDoc.GetMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                MessageBox.Show("No atoms in the structure.",
                    "Brak danych", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PowderDiffractionWindow existing = null;
            foreach (var form in Application.OpenForms)
            {
                if (form is PowderDiffractionWindow pdw)
                {
                    existing = pdw;
                    break;
                }
            }

            if (existing != null)
            {
                if (existing.IsHidden) existing.Show();
                existing.Activate();
                return;
            }

            var powderWin = new PowderDiffractionWindow(molecule);
            powderWin.Show(dockPanel, DockState.Document);

            mainForm.SetStatus("Powder diffraction - calculation started",
                $"Structures: {StructureStorage.Instance.StructureCount}", "⚡");
        }

        #endregion

        #region Subscribe / Unsubscribe

        private void UnsubscribeEvents()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DisplayOptions] UnsubscribeEvents");

                this._chkPacking.CheckedChanged -= ChkPacking_CheckedChanged;
                this._chkShortContact.CheckedChanged -= ChkShortContact_CheckedChanged;
                this._chkAsymmetricUnit.CheckedChanged -= ChkAsymmetricUnit_CheckedChanged;
                this._chkAsymmetricUnit.MouseDown -= _chkAsymmetricUnit_MouseDown;
                this._chkHBond.CheckedChanged -= ChkHBond_CheckedChanged;
                this._chkAutoCentre.CheckedChanged -= ChkAutoCentre_CheckedChanged;

                this._chkShowHydrogens.CheckedChanged -= ChkShowHydrogens_CheckedChanged;
                this._chkDepthCue.CheckedChanged -= ChkDepthCue_CheckedChanged;
                this._chkShowCellAxes.CheckedChanged -= ChkShowCellAxes_CheckedChanged;
                this._chkZClipping.CheckedChanged -= ChkZClipping_CheckedChanged;
                this._chkLabelAtoms.CheckedChanged -= ChkLabelAtoms_CheckedChanged;
                this._chkStereo.CheckedChanged -= ChkStereo_CheckedChanged;

                this._btnReset.Click -= BtnReset_Click;
                this._btnContacts.Click -= BtnContacts_Click;
                this._btnMoreInfo.Click -= BtnMoreInfo_Click;
                this._btnPowder.Click -= BtnPowder_Click;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DisplayOptions] UnsubscribeEvents error: {ex.Message}");
            }
        }

        private void SubscribeEvents()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DisplayOptions] SubscribeEvents");

                this._chkPacking.CheckedChanged += ChkPacking_CheckedChanged;
                this._chkShortContact.CheckedChanged += ChkShortContact_CheckedChanged;
                this._chkAsymmetricUnit.CheckedChanged += ChkAsymmetricUnit_CheckedChanged;
                this._chkHBond.CheckedChanged += ChkHBond_CheckedChanged;
                this._chkAutoCentre.CheckedChanged += ChkAutoCentre_CheckedChanged;

                this._chkShowHydrogens.CheckedChanged += ChkShowHydrogens_CheckedChanged;
                this._chkDepthCue.CheckedChanged += ChkDepthCue_CheckedChanged;
                this._chkShowCellAxes.CheckedChanged += ChkShowCellAxes_CheckedChanged;
                this._chkZClipping.CheckedChanged += ChkZClipping_CheckedChanged;
                this._chkLabelAtoms.CheckedChanged += ChkLabelAtoms_CheckedChanged;
                this._chkStereo.CheckedChanged += ChkStereo_CheckedChanged;

                this._btnReset.Click += BtnReset_Click;
                this._btnContacts.Click += BtnContacts_Click;
                this._btnMoreInfo.Click += BtnMoreInfo_Click;
                this._btnPowder.Click += BtnPowder_Click;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DisplayOptions] SubscribeEvents error: {ex.Message}");
            }
        }

        #endregion

        #region Refresh

        private void ResetStructure3D()
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel == null) return;

            System.Diagnostics.Debug.WriteLine($"[DisplayOptions] ResetStructure3D - START");

            foreach (var doc in dockPanel.Documents.ToList())
            {
                if (doc is MonoGameDocument monoDoc)
                {
                    var molecule = monoDoc.GetMolecule();
                    if (molecule == null || molecule.AsymmetricPart.Count == 0)
                    {
                        continue;
                    }

                    molecule.Clear3DAtomsOnly();

                    int currentStyle = Flags.Instance.Presentation;
                    PresentationKeepingForces(molecule, Flags.Instance.GenerateEquvalents, currentStyle);

                    monoDoc.LoadMolecule(molecule);

                    var renderer = monoDoc.GetCavityRenderer();
                    if (renderer != null && renderer.HasCavities)
                    {
                        renderer.BuildRepeatedCavities();
                        renderer.SetMoleculeShift(molecule.MoleculeShift);
                    }

                    monoDoc.RefreshView();
                }
            }

            System.Diagnostics.Debug.WriteLine($"[DisplayOptions] ResetStructure3D - END");
        }


        private static void PresentationKeepingForces(Molecule molecule, int mode, int style)
        {
            // Rebuild display atoms for asymmetric / symmetry / packing
            molecule.Presentation(mode, style);
            // Re-apply cached MACE forces if available (optional — mode switch still works without)
            if (MaceForceCache.HasData)
                MaceForceCache.ApplyToMolecule(molecule);
        }

        private void RefreshStructure()
        {
            var mainForm = MainForm.Instance;
            if (mainForm == null) return;

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel == null) return;

            System.Diagnostics.Debug.WriteLine(
                $"[DisplayOptions] RefreshStructure — mode={Flags.Instance.GenerateEquvalents}, " +
                $"ShowHBonds={Flags.Instance.ShowHBonds}");

            foreach (var doc in dockPanel.Documents)
            {
                if (doc is MonoGameDocument monoDoc)
                {
                    var molecule = monoDoc.GetMolecule();
                    if (molecule != null && molecule.NumberOfAtoms > 0)
                    {
                        int currentStyle = Flags.Instance.Presentation;
                        PresentationKeepingForces(molecule, Flags.Instance.GenerateEquvalents, currentStyle);
                        monoDoc.LoadMolecule(molecule);

                        var renderer = monoDoc.GetCavityRenderer();
                        if (renderer != null && renderer.HasCavities)
                        {
                            renderer.BuildRepeatedCavities();
                            renderer.SetMoleculeShift(molecule.MoleculeShift);
                        }

                        monoDoc.RefreshView();
                    }
                }
            }
        }

        private void RefreshView()
        {
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm != null)
            {
                var dockPanel = mainForm.GetDockPanel();
                if (dockPanel != null)
                {
                    foreach (var doc in dockPanel.Documents)
                    {
                        if (doc is MonoGameDocument monoDoc)
                        {
                            monoDoc.RefreshView();
                        }
                    }
                }
            }
        }


        private void RefreshStructureInfo(MainForm mainForm)
        {
            if (mainForm == null) return;
            try
            {
                var info = mainForm.GetStructureInfoWindow();
                if (info == null || info.IsDisposed || !info.Visible) return;
                var doc = mainForm.GetActiveDocument();
                var mol = doc?.GetMolecule();
                if (mol == null) return;
                string id = doc?.Text ?? mol.Name ?? "(unknown)";
                info.LoadStructure(mol, id);
            }
            catch { /* info window may not be open */ }
        }

        #endregion
    }
}