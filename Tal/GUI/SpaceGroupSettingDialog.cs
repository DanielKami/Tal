// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Space-group setting selection dialog.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Dialog wyboru docelowego settings space group.
// User picks one of the allowed settings (e.g. P21/c, P21/n, ...).

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Tal.Forms
{
    public partial class SpaceGroupSettingDialog : Form
    {
        #region Private Fields

        private readonly int _groupNumber;
        private readonly string _currentSetting;
        private readonly List<string> _availableSettings;

        #endregion

        #region Public Properties

        /// <summary>
        /// Wybrane docelowe ustawienie (np. "P21/n").
        /// Set after click "Transform".
        /// </summary>
        public string SelectedSetting { get; private set; } = "";

        #endregion

        #region Constructor

        public SpaceGroupSettingDialog(
            int groupNumber,
            string currentSetting,
            List<string> availableSettings)
        {
            _groupNumber = groupNumber;
            _currentSetting = currentSetting ?? "";
            _availableSettings = availableSettings ?? new List<string>();

            InitializeComponent();

            // Wire events (designer does not attach them automatically)
            btnTransform.Click += BtnTransform_Click;

            // Fill UI
            PopulateSettings();

            SelectedSetting = _currentSetting;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Fills the combo with available settings and selects the current one.
        /// </summary>
        private void PopulateSettings()
        {
            lblGroup.Text = $"Space group:  #{_groupNumber}";
            lblCurrent.Text = $"Current setting:  {_currentSetting}";

            cmbSettings.Items.Clear();
            foreach (var s in _availableSettings)
                cmbSettings.Items.Add(s);

            // Select current setting (if on the list)
            int idx = cmbSettings.Items.IndexOf(_currentSetting);
            if (idx >= 0)
                cmbSettings.SelectedIndex = idx;
            else if (cmbSettings.Items.Count > 0)
                cmbSettings.SelectedIndex = 0;
        }

        #endregion

        #region Event Handlers

        private void BtnTransform_Click(object sender, EventArgs e)
        {
            if (cmbSettings.SelectedItem == null)
            {
                MessageBox.Show(
                    "Please select a target setting.",
                    "Transform space group",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string selected = cmbSettings.SelectedItem.ToString() ?? "";

            // If identical to the current value — no point
            if (string.Equals(selected, _currentSetting, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "Selected setting is the same as the current one.",
                    "Transform space group",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SelectedSetting = selected;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        #endregion
    }
}