// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal planes module: PlanePropertiesWindow.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Tal.Core.PowderDiffraction;
using Color = System.Drawing.Color;

namespace Tal.Forms
{
    public partial class PlanePropertiesWindow : Form
    {
        #region Private Fields

        private readonly Molecule _molecule;
        private readonly CrystalPlane _originalPlane;
        private CrystalPlane _workingPlane;

        private readonly Matrix _fracToCartMatrix;

        private bool _inPlaneSelectionMode = false;
        private System.Windows.Forms.Timer _selectionTimer;

 // ⭐ Zaafterbiega afternownemu wejściu do handlerów RadioButton
        private bool _updatingRadioButtons = false;

        #endregion

        #region Public Properties

        public CrystalPlane Result { get; private set; }

        #endregion

        #region Constructor

        public PlanePropertiesWindow(Molecule molecule, CrystalPlane existingPlane)
        {
            InitializeComponent();
            _molecule = molecule ?? throw new ArgumentNullException(nameof(molecule));
            _originalPlane = existingPlane;

            _fracToCartMatrix = _molecule.FractionaltoCarttesianMatrix(0, 0, 0);

            if (existingPlane != null)
            {
                this.Text = "Plane Properties - Edit";
                _workingPlane = ClonePlane(existingPlane);
            }
            else
            {
                this.Text = "Plane Properties - New";
                _workingPlane = new CrystalPlane
                {
                    Name = "mean",
                    Type = CrystalPlaneType.MeanPlane,
                    Color = Color.Red,
                    Size = 10f,
                    SelectionMode = AtomSelectionMode.PickedAtom
                };

                _workingPlane.AtomIndices = MeanPlaneCalculator
                    .CollectSelectedAtomIndices(_molecule);
            }

            LoadPlaneToUI();

            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            if (tabControl.SelectedIndex == 0)
            {
                EnterPlaneSelectionMode();
            }

            _selectionTimer = new System.Windows.Forms.Timer();
            _selectionTimer.Interval = 300;
            _selectionTimer.Tick += SelectionTimer_Tick;
            _selectionTimer.Start();
        }

        #endregion

        #region Form Lifecycle

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _selectionTimer?.Stop();
            _selectionTimer?.Dispose();
            _selectionTimer = null;

            ExitPlaneSelectionMode();

            base.OnFormClosing(e);
        }

        #endregion

        #region Clone & Load

        private CrystalPlane ClonePlane(CrystalPlane source)
        {
            return new CrystalPlane
            {
                Id = source.Id,
                Name = source.Name,
                Type = source.Type,
                Color = source.Color,
                Visible = source.Visible,
                ShowLabel = source.ShowLabel,
                Transparent = source.Transparent,
                SelectionMode = source.SelectionMode,
                AtomIndices = new List<int>(source.AtomIndices ?? new List<int>()),
                H = source.H,
                K = source.K,
                L = source.L,
                A = source.A,
                B = source.B,
                C = source.C,
                D = source.D,
                Center = source.Center,
                Corners = source.Corners != null ? (Vector3[])source.Corners.Clone() : new Vector3[4],
                Size = source.Size
            };
        }

        private void LoadPlaneToUI()
        {
 // ⭐ Blokuj reakcje RadioButton-ów afterdczas ładowania
            _updatingRadioButtons = true;

            tabControl.SelectedIndex = _workingPlane.Type == CrystalPlaneType.HKL ? 1 : 0;

            switch (_workingPlane.SelectionMode)
            {
                case AtomSelectionMode.PickedAtom: rbPickedAtom.Checked = true; break;
                case AtomSelectionMode.PickedMolecule: rbPickedMolecule.Checked = true; break;
                case AtomSelectionMode.Ring: rbRing.Checked = true; break;
            }

            numericH.Value = Math.Max(numericH.Minimum, Math.Min(numericH.Maximum, _workingPlane.H));
            numericK.Value = Math.Max(numericK.Minimum, Math.Min(numericK.Maximum, _workingPlane.K));
            numericL.Value = Math.Max(numericL.Minimum, Math.Min(numericL.Maximum, _workingPlane.L));

            chkShow.Checked = _workingPlane.Visible;
            chkLabel.Checked = _workingPlane.ShowLabel;
            chkTransparent.Checked = _workingPlane.Transparent;
            txtLabel.Text = _workingPlane.Name;
            panelColor.BackColor = _workingPlane.Color;

            _updatingRadioButtons = false;

            UpdateSelectionInfo();
        }

        #endregion

        #region Save

        private void SaveUIToPlane()
        {
            _workingPlane.Type = tabControl.SelectedIndex == 1
                ? CrystalPlaneType.HKL
                : CrystalPlaneType.MeanPlane;

            _workingPlane.H = (int)numericH.Value;
            _workingPlane.K = (int)numericK.Value;
            _workingPlane.L = (int)numericL.Value;

            _workingPlane.Visible = chkShow.Checked;
            _workingPlane.ShowLabel = chkLabel.Checked;
            _workingPlane.Transparent = chkTransparent.Checked;
            _workingPlane.Name = txtLabel.Text;
            _workingPlane.Color = panelColor.BackColor;

            CalculatePlaneGeometry();
        }

        private void CalculatePlaneGeometry()
        {
            if (_workingPlane.Type == CrystalPlaneType.HKL)
            {
                if (_workingPlane.H == 0 && _workingPlane.K == 0 && _workingPlane.L == 0)
                    _workingPlane.H = 1;

                PlaneGeometry.CalculateHKLPlane(
                    _workingPlane,
                    _fracToCartMatrix,
                    Vector3.Zero,
                    size: 10f
                );
            }
            else
            {
                bool ok = MeanPlaneCalculator.CalculateMeanPlane(
                    _molecule,
                    _workingPlane,
                    _workingPlane.AtomIndices,
                    size: 10f
                );

                if (!ok)
                {
                    _workingPlane.Center = Vector3.Zero;
                    _workingPlane.Corners = new Vector3[4];
                }
            }
        }

        #endregion

        #region Picking Mode Integration

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex == 0)
                EnterPlaneSelectionMode();
            else
                ExitPlaneSelectionMode();

            UpdateSelectionInfo();
        }

        private void EnterPlaneSelectionMode()
        {
            if (_inPlaneSelectionMode) return;

            var mainForm = MainForm.Instance;
            if (mainForm == null) return;

            mainForm.EnterPickingModeForPlaneSelection();
            _inPlaneSelectionMode = true;
        }

        private void ExitPlaneSelectionMode()
        {
            if (!_inPlaneSelectionMode) return;

            var mainForm = MainForm.Instance;
            if (mainForm != null)
            {
                mainForm.ExitPickingModeForPlaneSelection();
            }
            _inPlaneSelectionMode = false;
        }

 /// <summary>
 /// Timer tick – updates the selected atom counter in real time.
 /// Does NOT modify AtomIndices unless the count actually changed.
 /// </summary>
        private void SelectionTimer_Tick(object sender, EventArgs e)
        {
            if (!_inPlaneSelectionMode) return;

            int liveCount = MeanPlaneCalculator.CountSelectedAtoms(_molecule);
            int cachedCount = _workingPlane.AtomIndices?.Count ?? 0;

            if (liveCount != cachedCount)
            {
                _workingPlane.AtomIndices = MeanPlaneCalculator
                    .CollectSelectedAtomIndices(_molecule);

                UpdateSelectionInfo();
            }
        }

        #endregion

        #region Public Entry Point (called by MainForm)

 /// <summary>
 /// Called by MainForm when an atom is clicked in the 3D view while
 /// this dialog is open in Mean Plane mode. Routes the click through
 /// the current SelectionMode (atom / molecule / ring).
 /// </summary>
        public void HandleAtomPick(int atomIndex)
        {
            if (_molecule == null || _molecule.atom == null) return;
            if (atomIndex < 0 || atomIndex >= _molecule.NumberOfAtoms) return;
            if (!_inPlaneSelectionMode) return;

            switch (_workingPlane.SelectionMode)
            {
                case AtomSelectionMode.PickedAtom:
                    ToggleSingleAtom(atomIndex);
                    break;

                case AtomSelectionMode.PickedMolecule:
                    SelectWholeMolecule(atomIndex);
                    break;

                case AtomSelectionMode.Ring:
                    SelectRing(atomIndex);
                    break;
            }

 // Sync AtomIndices with the molecule's selection state
            _workingPlane.AtomIndices = MeanPlaneCalculator
                .CollectSelectedAtomIndices(_molecule);

            UpdateSelectionInfo();
 
 // Refresh 3D view
            var doc = MainForm.Instance?.GetActiveDocument();
            doc?.RefreshView();
        }

        #endregion

        #region Selection Helpers

 /// <summary>
 /// Toggle a single atom's selection (PickedAtom mode).
 /// </summary>
        private void ToggleSingleAtom(int atomIndex)
        {
            bool newState = !_molecule.atom[atomIndex].selected;

            if (newState)
            {
                _molecule.NumberSelected++;
                _molecule.atom[atomIndex].SelectionOrder = _molecule.NumberSelected;
            }
            else
            {
                _molecule.atom[atomIndex].SelectionOrder = 0;
                if (_molecule.NumberSelected > 0) _molecule.NumberSelected--;
            }

            _molecule.atom[atomIndex].selected = newState;
        }

 /// <summary>
 /// Select all atoms of the same molecule as the clicked atom (PickedMolecule mode).
 /// </summary>
        private void SelectWholeMolecule(int atomIndex)
        {
            int molNum = _molecule.atom[atomIndex].IndividualMoleculeInCell;

            int order = 0;
            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
            {
                bool match = (_molecule.atom[i].IndividualMoleculeInCell == molNum);
                _molecule.atom[i].selected = match;
                _molecule.atom[i].SelectionOrder = match ? ++order : 0;
            }
            _molecule.NumberSelected = order;
        }

 /// <summary>
 /// Select the smallest ring containing the clicked atom (Ring mode).
 /// </summary>
        private void SelectRing(int atomIndex)
        {
            var ring = RingFinder.FindSmallestRing(
                _molecule,
                atomIndex,
                maxRingSize: 12,
                requirePlanar: true,
                planarityTolerance: 0.15f
            );

            ClearSelectionInMolecule();

            if (ring == null || ring.Count < 3)
            {
 // Fallback – select single atom
                _molecule.atom[atomIndex].selected = true;
                _molecule.atom[atomIndex].SelectionOrder = 1;
                _molecule.NumberSelected = 1;
                return;
            }

            for (int i = 0; i < ring.Count; i++)
            {
                int idx = ring[i];
                _molecule.atom[idx].selected = true;
                _molecule.atom[idx].SelectionOrder = i + 1;
            }
            _molecule.NumberSelected = ring.Count;
        }

        private void ClearSelectionInMolecule()
        {
            if (_molecule == null || _molecule.atom == null) return;
            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
            {
                _molecule.atom[i].selected = false;
                _molecule.atom[i].SelectionOrder = 0;
            }
            _molecule.NumberSelected = 0;
        }

        #endregion

        #region Selection Handlers (UI)

        private void RbPickedAtom_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingRadioButtons) return;
            if (!rbPickedAtom.Checked) return;

            _workingPlane.SelectionMode = AtomSelectionMode.PickedAtom;

 // Clear selekcję przy changesie trybu
            ClearSelectionInMolecule();
            _workingPlane.AtomIndices.Clear();
            UpdateSelectionInfo();

            MainForm.Instance?.GetActiveDocument()?.RefreshView();
        }

        private void RbPickedMolecule_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingRadioButtons) return;
            if (!rbPickedMolecule.Checked) return;

            _workingPlane.SelectionMode = AtomSelectionMode.PickedMolecule;

            ClearSelectionInMolecule();
            _workingPlane.AtomIndices.Clear();
            UpdateSelectionInfo();

            MainForm.Instance?.GetActiveDocument()?.RefreshView();
        }

        private void RbRing_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingRadioButtons) return;
            if (!rbRing.Checked) return;

            _workingPlane.SelectionMode = AtomSelectionMode.Ring;

            ClearSelectionInMolecule();
            _workingPlane.AtomIndices.Clear();
            UpdateSelectionInfo();

            MainForm.Instance?.GetActiveDocument()?.RefreshView();
        }

        private void BtnRefreshFromSelection_Click(object sender, EventArgs e)
        {
            _workingPlane.AtomIndices = MeanPlaneCalculator
                .CollectSelectedAtomIndices(_molecule);

            UpdateSelectionInfo();
        }

        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
 // Zaznacz all atoms w molekule
            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
            {
                _molecule.atom[i].selected = true;
                _molecule.atom[i].SelectionOrder = i + 1;
            }
            _molecule.NumberSelected = _molecule.NumberOfAtoms;

            _workingPlane.AtomIndices = MeanPlaneCalculator
                .CollectSelectedAtomIndices(_molecule);

            UpdateSelectionInfo();

            MainForm.Instance?.GetActiveDocument()?.RefreshView();
        }

        private void BtnSelectNonHydrogen_Click(object sender, EventArgs e)
        {
            int order = 0;
            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
            {
                bool match = _molecule.atom[i].AtomicNumber > 1;
                _molecule.atom[i].selected = match;
                _molecule.atom[i].SelectionOrder = match ? ++order : 0;
            }
            _molecule.NumberSelected = order;

            _workingPlane.AtomIndices = MeanPlaneCalculator
                .CollectSelectedAtomIndices(_molecule);

            UpdateSelectionInfo();

            MainForm.Instance?.GetActiveDocument()?.RefreshView();
        }

        private void BtnSelectHydrogen_Click(object sender, EventArgs e)
        {
            int order = 0;
            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
            {
                bool match = _molecule.atom[i].AtomicNumber == 1;
                _molecule.atom[i].selected = match;
                _molecule.atom[i].SelectionOrder = match ? ++order : 0;
            }
            _molecule.NumberSelected = order;

            _workingPlane.AtomIndices = MeanPlaneCalculator
                .CollectSelectedAtomIndices(_molecule);

            UpdateSelectionInfo();

            MainForm.Instance?.GetActiveDocument()?.RefreshView();
        }

        private void UpdateSelectionInfo()
        {
            int count = _workingPlane.AtomIndices?.Count ?? 0;

            if (_workingPlane.Type == CrystalPlaneType.MeanPlane)
            {
                if (count < 3)
                {
                    labelSelectionInfo.ForeColor = Color.Firebrick;
                    labelSelectionInfo.Text = $"Selected: {count} atoms (need at least 3)";
                }
                else
                {
                    labelSelectionInfo.ForeColor = Color.ForestGreen;
                    labelSelectionInfo.Text = $"Selected: {count} atoms";
                }
            }
            else
            {
                labelSelectionInfo.ForeColor = SystemColors.ControlText;
                labelSelectionInfo.Text = "Mean plane requires at least 3 atoms";
            }
        }

        #endregion

        #region Color Picker

        private void PanelColor_Click(object sender, EventArgs e)
        {
            using var dialog = new ColorDialog();
            dialog.Color = panelColor.BackColor;
            dialog.FullOpen = true;
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                panelColor.BackColor = dialog.Color;
            }
        }

        #endregion

        #region OK / Cancel

        private void BtnOk_Click(object sender, EventArgs e)
        {
            SaveUIToPlane();

            if (_workingPlane.Type == CrystalPlaneType.MeanPlane)
            {
                int count = _workingPlane.AtomIndices?.Count ?? 0;
                if (count < 3)
                {
                    var result = MessageBox.Show(
                        "Select at least three atoms to calculate least squares plane.\n\n" +
                        "Continue anyway? (plane will be empty)",
                        "Not enough atoms",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                        return;
                }
            }

            if (_workingPlane.Type == CrystalPlaneType.HKL &&
                _workingPlane.H == 0 && _workingPlane.K == 0 && _workingPlane.L == 0)
            {
                MessageBox.Show(
                    "At least one of h, k, l must be non-zero.",
                    "Invalid Miller indices",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_workingPlane.Name))
            {
                _workingPlane.Name = _workingPlane.Type == CrystalPlaneType.HKL
                    ? $"hkl ({_workingPlane.H}{_workingPlane.K}{_workingPlane.L})"
                    : "mean";
            }

            Result = _workingPlane;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        #endregion
    }
}