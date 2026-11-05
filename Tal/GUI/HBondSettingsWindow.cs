// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hydrogen-bond settings window.

// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Tal.GUI;

namespace Tal.Forms
{
    /// <summary>
    /// Dockable window for configuring hydrogen bond detection parameters.
    /// Two distance modes: actual (Å range) or VdW-based (sum of radii ± delta).
    ///
    /// ⭐ NEW: list of allowed donors and acceptors (per atomic Z),
    ///         and colour bonds hydrogen (ColorDialog).
    /// ⭐ NEW: "Select all" checkboxes for donors and acceptors.
    /// </summary>
    public partial class HBondSettingsWindow : DockContent
    {
        #region Private Fields

        private bool _loading = false;

        /// <summary>
        /// Available elements for the donor and acceptor lists.
        /// Order MUST be identical in both lists.
        /// </summary>
        private static readonly (int Z, string Name)[] HBondElements = new[]
        {
            (7,  "N"),
            (8,  "O"),
            (9,  "F"),
            (15, "P"),
            (16, "S"),
            (17, "Cl"),
            (35, "Br"),
            (53, "I"),
        };

        #endregion

        #region Constructor

        public HBondSettingsWindow()
        {
            InitializeComponent();

            DockWindowConfig.SetupWindow(this, "H-Bond Settings");

            // Extra event handlers not attached in the designer
            cmbVdWMinusOp.SelectedIndexChanged += (s, e) => { };
            cmbVdWPlusOp.SelectedIndexChanged += (s, e) => { };

            chkRequireHydrogen.CheckedChanged += (s, e) => { };
            chkIntermolecular.CheckedChanged += (s, e) => { };
            chkIntramolecular.CheckedChanged += (s, e) => { };

            // ⭐ Donor / acceptor lists — react to checkbox change
            lstDonors.ItemCheck += LstDonors_ItemCheck;
            lstAcceptors.ItemCheck += LstAcceptors_ItemCheck;

            // ⭐ Fill lists BEFORE LoadFromFlags
            PopulateElementLists();

            LoadFromFlags();
            UpdateEnabledState();
        }

        #endregion

        #region Element list helpers

        /// <summary>
        /// Fills both lists with element labels. Selection is set by LoadFromFlags.
        /// </summary>
        private void PopulateElementLists()
        {
            _loading = true;
            try
            {
                lstDonors.Items.Clear();
                lstAcceptors.Items.Clear();

                foreach (var (z, name) in HBondElements)
                {
                    lstDonors.Items.Add($"{name} (Z={z})", false);
                    lstAcceptors.Items.Add($"{name} (Z={z})", false);
                }
            }
            finally
            {
                _loading = false;
            }
        }

        #endregion

        #region Load / Save

        private void LoadFromFlags()
        {
            _loading = true;
            try
            {
                var f = Flags.Instance;

                // Distance mode radio buttons
                if (f.HBondDistanceMode == 0)
                    rbActualDistance.Checked = true;
                else
                    rbVdWDistance.Checked = true;

                // Actual distance values
                numActualMin.Value = ClampDecimal(
                    (decimal)f.HBondActualMinDistance,
                    numActualMin.Minimum, numActualMin.Maximum);

                numActualMax.Value = ClampDecimal(
                    (decimal)f.HBondActualMaxDistance,
                    numActualMax.Minimum, numActualMax.Maximum);

                // VdW parameters
                numVdWMinus.Value = ClampDecimal(
                    (decimal)f.HBondVdWMinus,
                    numVdWMinus.Minimum, numVdWMinus.Maximum);

                numVdWPlus.Value = ClampDecimal(
                    (decimal)f.HBondVdWPlus,
                    numVdWPlus.Minimum, numVdWPlus.Maximum);

                // Operator combo boxes - default to "minus" (index 0)
                cmbVdWMinusOp.SelectedIndex = 0;
                cmbVdWPlusOp.SelectedIndex = 0;

                // Angle
                numAngle.Value = ClampDecimal(
                    (decimal)f.HBondMinAngle,
                    numAngle.Minimum, numAngle.Maximum);

                // Options
                chkShowHBonds.Checked = f.ShowHBonds;
                chkRequireHydrogen.Checked = f.HBondRequireHydrogen;
                chkDrawFromHydrogen.Checked = f.HBondDrawFromHydrogen;
                chkIntermolecular.Checked = f.HBondIntermolecular;
                chkIntramolecular.Checked = f.HBondIntramolecular;

                // ⭐ Donor / acceptor lists
                // If no saved lists exist (null or empty), default to ALL ON.
                var donors = f.HBondAllowedDonors;
                var acceptors = f.HBondAllowedAcceptors;

                bool donorsEmpty = (donors == null || donors.Count == 0);
                bool acceptorsEmpty = (acceptors == null || acceptors.Count == 0);

                for (int i = 0; i < HBondElements.Length; i++)
                {
                    int z = HBondElements[i].Z;

                    bool donorChecked = donorsEmpty || donors.Contains(z);
                    bool acceptorChecked = acceptorsEmpty || acceptors.Contains(z);

                    if (i < lstDonors.Items.Count)
                        lstDonors.SetItemChecked(i, donorChecked);

                    if (i < lstAcceptors.Items.Count)
                        lstAcceptors.SetItemChecked(i, acceptorChecked);
                }

                // Update "Select all" checkboxes
                UpdateSelectAllCheckboxes();

                // ⭐ Colour H-bond
                var c = f.HBondColor;
                btnHBondColor.BackColor = Color.FromArgb(
                    Clamp255((int)(c.X * 255)),
                    Clamp255((int)(c.Y * 255)),
                    Clamp255((int)(c.Z * 255)));
            }
            finally
            {
                _loading = false;
            }
        }

        private void SaveToFlags()
        {
            var f = Flags.Instance;

            // Distance mode
            f.HBondDistanceMode = rbActualDistance.Checked ? 0 : 1;

            // Actual distance
            f.HBondActualMinDistance = (float)numActualMin.Value;
            f.HBondActualMaxDistance = (float)numActualMax.Value;

            // VdW parameters
            f.HBondVdWMinus = (float)numVdWMinus.Value;
            f.HBondVdWPlus = (float)numVdWPlus.Value;

            // Angle
            f.HBondMinAngle = (float)numAngle.Value;

            // Options
            f.ShowHBonds = chkShowHBonds.Checked;
            f.HBondRequireHydrogen = chkRequireHydrogen.Checked;
            f.HBondDrawFromHydrogen = chkDrawFromHydrogen.Checked;
            f.HBondIntermolecular = chkIntermolecular.Checked;
            f.HBondIntramolecular = chkIntramolecular.Checked;

            // ⭐ Donor / acceptor lists
            var donors = new List<int>();
            var acceptors = new List<int>();

            for (int i = 0; i < HBondElements.Length; i++)
            {
                int z = HBondElements[i].Z;

                if (i < lstDonors.Items.Count && lstDonors.GetItemChecked(i))
                    donors.Add(z);

                if (i < lstAcceptors.Items.Count && lstAcceptors.GetItemChecked(i))
                    acceptors.Add(z);
            }

            f.HBondAllowedDonors = donors;
            f.HBondAllowedAcceptors = acceptors;

            // ⭐ Colour H-bond
            var c = btnHBondColor.BackColor;
            f.HBondColor = new Microsoft.Xna.Framework.Vector3(
                c.R / 255f, c.G / 255f, c.B / 255f);

            f.MarkDirty();
        }

        #endregion

        #region UI State

        /// <summary>
        /// Enables/disables distance-mode controls based on which radio is active.
        /// </summary>
        private void UpdateEnabledState()
        {
            bool actual = rbActualDistance.Checked;

            // Actual distance controls
            lblActualMin.Enabled = actual;
            numActualMin.Enabled = actual;
            lblActualMax.Enabled = actual;
            numActualMax.Enabled = actual;

            // VdW distance controls
            bool vdw = rbVdWDistance.Checked;
            lblVdWMinus.Enabled = vdw;
            cmbVdWMinusOp.Enabled = vdw;
            numVdWMinus.Enabled = vdw;
            lblVdWPlus.Enabled = vdw;
            cmbVdWPlusOp.Enabled = vdw;
            numVdWPlus.Enabled = vdw;
            lblVdWSum.Enabled = vdw;
        }

        /// <summary>
        /// Updates the "Select all" checkboxes to reflect the current
        /// state of the corresponding checked list boxes.
        /// </summary>
        private void UpdateSelectAllCheckboxes()
        {
            _loading = true;
            try
            {
                bool allDonors = lstDonors.Items.Count > 0;
                for (int i = 0; i < lstDonors.Items.Count; i++)
                {
                    if (!lstDonors.GetItemChecked(i))
                    {
                        allDonors = false;
                        break;
                    }
                }
                chkSelectAllDonors.Checked = allDonors;

                bool allAcceptors = lstAcceptors.Items.Count > 0;
                for (int i = 0; i < lstAcceptors.Items.Count; i++)
                {
                    if (!lstAcceptors.GetItemChecked(i))
                    {
                        allAcceptors = false;
                        break;
                    }
                }
                chkSelectAllAcceptors.Checked = allAcceptors;
            }
            finally
            {
                _loading = false;
            }
        }

        #endregion

        #region Event Handlers - Distance Mode

        private void RbDistanceMode_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            UpdateEnabledState();
        }

        #endregion

        #region Event Handlers - Show H-Bonds Checkbox

        /// <summary>
        /// Toggling "Show H-Bonds" checkbox:
        ///   - Saves current flags
        ///   - Ensures H-bonds are calculated on active molecule
        ///   - Triggers a redraw of the 3D view
        /// </summary>
        private void ChkShowHBonds_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) return;

            // Save current UI values into Flags
            SaveToFlags();

            // Calculate H-bonds (if enabled) + refresh
            RefreshStructure();

            // Update status
            var mainForm = MainForm.Instance;
            if (mainForm != null)
            {
                string msg = chkShowHBonds.Checked
                    ? "H-Bonds ON"
                    : "H-Bonds OFF";
                mainForm.SetStatus(msg, $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
            }
        }

        #endregion

        #region Event Handlers - Select All

        private void ChkSelectAllDonors_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) return;

            _loading = true;
            try
            {
                bool state = chkSelectAllDonors.Checked;
                for (int i = 0; i < lstDonors.Items.Count; i++)
                    lstDonors.SetItemChecked(i, state);
            }
            finally
            {
                _loading = false;
            }

            SaveToFlags();
            RefreshStructure();

            MainForm.Instance?.SetStatus(
                chkSelectAllDonors.Checked ? "All H-Bond donors selected" : "All H-Bond donors deselected",
                $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
        }

        private void ChkSelectAllAcceptors_CheckedChanged(object sender, EventArgs e)
        {
            if (_loading) return;

            _loading = true;
            try
            {
                bool state = chkSelectAllAcceptors.Checked;
                for (int i = 0; i < lstAcceptors.Items.Count; i++)
                    lstAcceptors.SetItemChecked(i, state);
            }
            finally
            {
                _loading = false;
            }

            SaveToFlags();
            RefreshStructure();

            MainForm.Instance?.SetStatus(
                chkSelectAllAcceptors.Checked ? "All H-Bond acceptors selected" : "All H-Bond acceptors deselected",
                $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
        }

        #endregion

        #region Event Handlers - Donors / Acceptors

        /// <summary>
        /// ItemCheck fires BEFORE the checkbox state changes.
        /// Therefore BeginInvoke — so the state is read after the fact.
        /// </summary>
        private void LstDonors_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_loading) return;

            BeginInvoke(new Action(() =>
            {
                UpdateSelectAllCheckboxes();
                SaveToFlags();
                RefreshStructure();

                MainForm.Instance?.SetStatus("H-Bond donors updated",
                    $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
            }));
        }

        private void LstAcceptors_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_loading) return;

            BeginInvoke(new Action(() =>
            {
                UpdateSelectAllCheckboxes();
                SaveToFlags();
                RefreshStructure();

                MainForm.Instance?.SetStatus("H-Bond acceptors updated",
                    $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
            }));
        }

        #endregion

        #region Event Handlers - Color

        private void BtnHBondColor_Click(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog
            {
                Color = btnHBondColor.BackColor,
                FullOpen = true,
                AnyColor = true
            };

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            btnHBondColor.BackColor = dlg.Color;

            SaveToFlags();
            RefreshStructure();

            MainForm.Instance?.SetStatus("H-Bond color updated",
                $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
        }

        #endregion

        #region Event Handlers - Buttons

        private void BtnDefault_Click(object sender, EventArgs e)
        {
            _loading = true;
            try
            {
                rbVdWDistance.Checked = true;
                rbActualDistance.Checked = false;

                numActualMin.Value = 1.5m;
                numActualMax.Value = 3.5m;

                numVdWMinus.Value = 5.0m;
                numVdWPlus.Value = 0.0m;
                cmbVdWMinusOp.SelectedIndex = 0;
                cmbVdWPlusOp.SelectedIndex = 0;

                // ⭐ Default angle = 65 degrees
                numAngle.Value = 65.0m;

                chkRequireHydrogen.Checked = true;
                chkDrawFromHydrogen.Checked = false;
                chkIntermolecular.Checked = true;
                chkIntramolecular.Checked = true;

                // ⭐ Default donors / acceptors — ALL ON
                for (int i = 0; i < HBondElements.Length; i++)
                {
                    if (i < lstDonors.Items.Count)
                        lstDonors.SetItemChecked(i, true);

                    if (i < lstAcceptors.Items.Count)
                        lstAcceptors.SetItemChecked(i, true);
                }

                chkSelectAllDonors.Checked = true;
                chkSelectAllAcceptors.Checked = true;

                // ⭐ Default colour — green
                btnHBondColor.BackColor = Color.FromArgb(0, 230, 0);
            }
            finally
            {
                _loading = false;
            }

            UpdateEnabledState();

            // Persist defaults so the 3D view reflects them immediately
            SaveToFlags();
            RefreshStructure();

            MainForm.Instance?.SetStatus("H-Bond settings reset to defaults",
                $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            SaveToFlags();
            RefreshStructure();

            var mainForm = MainForm.Instance;
            mainForm?.SetStatus("H-Bond settings applied",
                $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            SaveToFlags();
            RefreshStructure();
            this.Close();
        }

        #endregion

        #region Refresh

        /// <summary>
        /// Applies the current flags to all loaded molecules:
        ///   - Recomputes H-bonds
        ///   - Recomputes precomputed bond matrices
        ///   - Redraws the 3D view
        /// </summary>
        private void RefreshStructure()
        {
            var mainForm = MainForm.Instance;
            if (mainForm == null) return;

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel == null) return;

            foreach (var doc in dockPanel.Documents)
            {
                if (doc is MonoGameDocument monoDoc)
                {
                    var molecule = monoDoc.GetMolecule();
                    if (molecule != null && molecule.NumberOfAtoms > 0)
                    {
                        // Bonds first so donor→H topology is current (esp. after SC expand)
                        molecule.Calculate_Bonds3D();
                        molecule.Calculate_Hbonds3D();
                        molecule.PrecomputeBondMatrices();
                        monoDoc.LoadMolecule(molecule);
                        monoDoc.ResetDrawCache();
                        monoDoc.RefreshView();
                    }
                }
            }
            Flags.Instance.VisualRevision++;
        }

        #endregion

        #region Helpers

        private decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static int Clamp255(int value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return value;
        }

        #endregion

        private void chkDrawFromHydrogen_CheckedChanged(object sender, EventArgs e)
        {
            SaveToFlags();
            RefreshStructure();
        }
    }
}