// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Short contacts settings window.

﻿using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class ShortContactsWindow : DockContent
    {
        // ============================================================
        // KONSTRUKTOR
        // ============================================================

        public ShortContactsWindow()
        {
            InitializeComponent();

            DockWindowConfig.SetupWindow(this, "Define Short Contacts");

            // ⭐ EVENT WIRING
            this._numVdwMinus.ValueChanged += NumVdwMinus_ValueChanged;
            this._rbFixedDistance.CheckedChanged += RbFixedDistance_CheckedChanged;
            this._rbVdwSum.CheckedChanged += RbVdwSum_CheckedChanged;
            this._chkIntermolecular.CheckedChanged += ChkIntermolecular_CheckedChanged;
            this._chkIntramolecular.CheckedChanged += ChkIntramolecular_CheckedChanged;
            this._chkSelectWholeMolecule.CheckedChanged += ChkSelectWholeMolecule_CheckedChanged;
            this._btnApply.Click += BtnApply_Click;
            this._btnOk.Click += BtnOk_Click;
            this._btnCancel.Click += BtnCancel_Click;

            // ⭐ WCZYTAJ STAN Z FLAGS
            LoadFromFlags();
        }

        // ============================================================
        // WCZYTAJ STAN Z FLAGS
        // ============================================================

        private void LoadFromFlags()
        {
            // ⭐ Unsubscribe events during update
            this._numVdwMinus.ValueChanged -= NumVdwMinus_ValueChanged;
            this._rbFixedDistance.CheckedChanged -= RbFixedDistance_CheckedChanged;
            this._rbVdwSum.CheckedChanged -= RbVdwSum_CheckedChanged;
            this._chkIntermolecular.CheckedChanged -= ChkIntermolecular_CheckedChanged;
            this._chkIntramolecular.CheckedChanged -= ChkIntramolecular_CheckedChanged;
            this._chkSelectWholeMolecule.CheckedChanged -= ChkSelectWholeMolecule_CheckedChanged;

            this._numVdwMinus.Value = (decimal)Flags.Instance.ShortContactsVdwMinus;
            this._rbFixedDistance.Checked = !Flags.Instance.ShortContactsUseVdwSum;
            this._rbVdwSum.Checked = Flags.Instance.ShortContactsUseVdwSum;
            this._chkIntermolecular.Checked = Flags.Instance.ShortContactsIntermolecular;
            this._chkIntramolecular.Checked = Flags.Instance.ShortContactsIntramolecular;
            this._chkSelectWholeMolecule.Checked = Flags.Instance.ShortContactsSelectWholeMolecule;

            // ⭐ Zasubskrybuj zdarzenia z powrotem
            this._numVdwMinus.ValueChanged += NumVdwMinus_ValueChanged;
            this._rbFixedDistance.CheckedChanged += RbFixedDistance_CheckedChanged;
            this._rbVdwSum.CheckedChanged += RbVdwSum_CheckedChanged;
            this._chkIntermolecular.CheckedChanged += ChkIntermolecular_CheckedChanged;
            this._chkIntramolecular.CheckedChanged += ChkIntramolecular_CheckedChanged;
            this._chkSelectWholeMolecule.CheckedChanged += ChkSelectWholeMolecule_CheckedChanged;

            UpdateStatus();
        }

        // ============================================================
        // EVENT HANDLERS
        // ============================================================

        private void NumVdwMinus_ValueChanged(object sender, EventArgs e)
        {
            Flags.Instance.ShortContactsVdwMinus = (float)this._numVdwMinus.Value;
            UpdateStatus();
        }

        private void RbVdwSum_CheckedChanged(object sender, EventArgs e)
        {
            if (_rbVdwSum.Checked)
            {
                Flags.Instance.ShortContactsUseVdwSum = true;

                _numVdwMinus.Enabled = true;

                System.Diagnostics.Debug.WriteLine($"[ShortContacts] RadioButton: vdWSum = TRUE, minDistance = {Flags.Instance.ShortContactsVdwMinus}");

                UpdateStatus();
            }
        }

        private void RbFixedDistance_CheckedChanged(object sender, EventArgs e)
        {
            if (_rbFixedDistance.Checked)
            {
                Flags.Instance.ShortContactsUseVdwSum = false;

                _numVdwMinus.Enabled = true;

                System.Diagnostics.Debug.WriteLine($"[ShortContacts] RadioButton: vdWSum = FALSE, minDistance = {Flags.Instance.ShortContactsVdwMinus}");

                UpdateStatus();
            }
        }

        private void ChkIntermolecular_CheckedChanged(object sender, EventArgs e)
        {
            Flags.Instance.ShortContactsIntermolecular = this._chkIntermolecular.Checked;
            UpdateStatus();
        }

        private void ChkIntramolecular_CheckedChanged(object sender, EventArgs e)
        {
            Flags.Instance.ShortContactsIntramolecular = this._chkIntramolecular.Checked;
            UpdateStatus();
        }

        private void ChkSelectWholeMolecule_CheckedChanged(object sender, EventArgs e)
        {
            Flags.Instance.ShortContactsSelectWholeMolecule = this._chkSelectWholeMolecule.Checked;

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            mainForm?.SetStatus(
                    this._chkSelectWholeMolecule.Checked ?
                    "Select Whole Molecule mode ON - click an atom to select entire molecule" :
                    "Select Whole Molecule mode OFF - click an atom to select only that atom",
                    $"Structures: {StructureStorage.Instance.StructureCount}",
                    this._chkSelectWholeMolecule.Checked ? "🧪" : "👆"
                );
            UpdateStatus();
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[ShortContacts] Apply clicked - finding short contacts");
            FindShortContacts();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[ShortContacts] OK clicked - finding short contacts and closing");
            FindShortContacts();
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[ShortContacts] Cancel clicked");
            this.Close();
        }

        // ============================================================
        // METODY POMOCNICZE
        // ============================================================

        private void UpdateStatus()
        {
            string mode = Flags.Instance.ShortContactsUseVdwSum ? "vdW sum" : "fixed distance";
            string info = $"Mode: {mode}, ";
            if (Flags.Instance.ShortContactsUseVdwSum)
            {
                info += $"Value: vdW sum (no minus)";  // ⭐ Informacja że numerek nie jest używany
            }
            else
            {
                info += $"Value: {Flags.Instance.ShortContactsVdwMinus:F2} Å";
            }
            info += $", Intermolecular: {(Flags.Instance.ShortContactsIntermolecular ? "ON" : "OFF")}, ";
            info += $"Intramolecular: {(Flags.Instance.ShortContactsIntramolecular ? "ON" : "OFF")}";

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            mainForm?.SetStatus($"Short Contacts: {info}", $"Structures: {StructureStorage.Instance.StructureCount}", "📏");

            _lblStatus.Text = info;
        }
        private void FindShortContacts()
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var molecule = mainForm.GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                mainForm.SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            Flags.Instance.ShortContact = true;
            Flags.Instance.ShortContactsActive = true;
            Flags.Instance.VisualRevision++;   // ⭐ reset Progressive AA
            Flags.Instance.MarkDirty();

            mainForm.FindAllShortContacts();

            mainForm.SetStatus(
                "Short contacts found - red dashed lines show contacts",
                $"Structures: {StructureStorage.Instance.StructureCount}",
                "🔗"
            );
        }

        // ============================================================
        // PUBLIC METHODS FOR EXTERNAL ACCESS
        // ============================================================

        public void UpdateFromFlags()
        {
            LoadFromFlags();
        }

        public void SetSelectWholeMolecule(bool value)
        {
            if (this._chkSelectWholeMolecule.Checked != value)
                this._chkSelectWholeMolecule.Checked = value;
        }

        public bool IsSelectWholeMoleculeChecked()
        {
            return this._chkSelectWholeMolecule.Checked;
        }

        public void SetVdwMinus(float value)
        {
            if (this._numVdwMinus.Value != (decimal)value)
                this._numVdwMinus.Value = (decimal)value;
        }

        public float GetVdwMinus()
        {
            return (float)this._numVdwMinus.Value;
        }

        public void SetIntermolecular(bool value)
        {
            if (this._chkIntermolecular.Checked != value)
                this._chkIntermolecular.Checked = value;
        }

        public bool IsIntermolecularChecked()
        {
            return this._chkIntermolecular.Checked;
        }

        public void SetIntramolecular(bool value)
        {
            if (this._chkIntramolecular.Checked != value)
                this._chkIntramolecular.Checked = value;
        }

        public bool IsIntramolecularChecked()
        {
            return this._chkIntramolecular.Checked;
        }
    }
}