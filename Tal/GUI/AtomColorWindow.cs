// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Atom colour settings window — colouring modes and custom colours.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class AtomColorWindow : DockContent
    {
        #region Private Fields

        private ColorDialog _colorDialog;
        private Vector3 _selectedIndividualColor = new Vector3(1f, 0f, 0f);
        private bool _isUpdating = false;

        #endregion

        #region Constructor

        public AtomColorWindow()
        {
            InitializeComponent();

            DockWindowConfig.SetupWindow(this, "Atom Color");

            // ⭐ Podpisz zdarzenia
            _radioAtomicNumber.CheckedChanged += Radio_CheckedChanged;
            _radioSymmetry.CheckedChanged += Radio_CheckedChanged;
            _radioMolecule.CheckedChanged += Radio_CheckedChanged;
            _radioSymmetryMolecule.CheckedChanged += Radio_CheckedChanged;
            _radioIndividual.CheckedChanged += Radio_CheckedChanged;
            _radioChargeColor.CheckedChanged += Radio_CheckedChanged;
            _radioHydrophobicityColor.CheckedChanged += Radio_CheckedChanged;
            _radioLabelNone.CheckedChanged += RadioLabel_CheckedChanged;
            _radioLabelCharge.CheckedChanged += RadioLabel_CheckedChanged;
            _radioLabelHydrophobicity.CheckedChanged += RadioLabel_CheckedChanged;
            _btnPickColor.Click += BtnPickColor_Click;

            _colorDialog = new ColorDialog();
            _colorDialog.FullOpen = true;

            // ⭐ WCZYTAJ ZAPISANY TRYB KOLOROWANIA
            int savedColorMode = Flags.Instance.ColorMode;
            switch (savedColorMode)
            {
                case 0: _radioAtomicNumber.Checked = true; break;
                case 1: _radioSymmetry.Checked = true; break;
                case 2: _radioMolecule.Checked = true; break;
                case 3: _radioSymmetryMolecule.Checked = true; break;
                case 4: _radioIndividual.Checked = true; break;
                case 5: _radioChargeColor.Checked = true; break;
                case 6: _radioHydrophobicityColor.Checked = true; break;
                default: _radioAtomicNumber.Checked = true; break;
            }

            // ⭐ LOAD SAVED PROPERTY FOR LABELS
            switch (Flags.Instance.LabelProperty)
            {
                case 0: _radioLabelNone.Checked = true; break;
                case 1: _radioLabelCharge.Checked = true; break;
                case 2: _radioLabelHydrophobicity.Checked = true; break;
                default: _radioLabelNone.Checked = true; break;
            }

            // ⭐ WCZYTAJ ZAPISANY COLOUR INDYWIDUALNY
            _selectedIndividualColor = Flags.Instance.IndividualColor;
            Flags.Instance.VisualRevision++;
            UpdateColorButton();
            ApplyVS2022Style();
            UpdatePreview();
        }

        #endregion

        #region VS2022 Style

        private void ApplyVS2022Style()
        {
            this.BackColor = System.Drawing.Color.FromArgb(248, 248, 250);

            _grpMode.BackColor = System.Drawing.Color.FromArgb(248, 248, 250);
            _grpOptions.BackColor = System.Drawing.Color.FromArgb(248, 248, 250);
            _grpProperties.BackColor = System.Drawing.Color.FromArgb(248, 248, 250);

            foreach (var radio in new RadioButton[] {
                _radioAtomicNumber, _radioSymmetry, _radioMolecule,
                _radioSymmetryMolecule, _radioIndividual,
                _radioChargeColor, _radioHydrophobicityColor,
                _radioLabelNone, _radioLabelCharge, _radioLabelHydrophobicity })
            {
                radio.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
                radio.BackColor = System.Drawing.Color.Transparent;
            }

            _btnPickColor.FlatStyle = FlatStyle.Flat;
            _btnPickColor.FlatAppearance.BorderSize = 1;
            _btnPickColor.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 200, 200);

            _lblStatus.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120);
        }

        #endregion

        #region Event Handlers

        private void Radio_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;

            var radio = sender as RadioButton;
            if (radio == null || !radio.Checked) return;

            bool isIndividual = (sender == _radioIndividual);
            _pnlColorPicker.Visible = isIndividual;
            Flags.Instance.VisualRevision++;
            if (!isIndividual)
            {
                ApplyColorMode();
            }
            else
            {
                UpdatePreview();
            }
        }

        private void RadioLabel_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;

            var radio = sender as RadioButton;
            if (radio == null || !radio.Checked) return;

            int property = 0;
            if (_radioLabelCharge.Checked) property = 1;
            else if (_radioLabelHydrophobicity.Checked) property = 2;

            Flags.Instance.LabelProperty = property;

            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            mainForm?.RefreshAllDocuments();

            string propName = property == 0 ? "None" : (property == 1 ? "Charge" : "Hydrophobicity");
            _lblStatus.Text = $"Labels showing: {propName}";
            Flags.Instance.VisualRevision++;
        }

        private void BtnPickColor_Click(object sender, EventArgs e)
        {
            if (_colorDialog.ShowDialog() == DialogResult.OK)
            {
                _selectedIndividualColor = SystemColorToVector3(_colorDialog.Color);
                Flags.Instance.IndividualColor = _selectedIndividualColor;
                Flags.Instance.VisualRevision++;
                UpdateColorButton();
                ApplyIndividualColor();
            }
        }

        #endregion

        #region Visibility

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            Flags.Instance.VisualRevision++;
            if (Visible)
            {
                UpdatePreview();
            }
        }

        public void RefreshColors()
        {
            Flags.Instance.VisualRevision++;
            UpdatePreview();
        }

        #endregion
    }
}