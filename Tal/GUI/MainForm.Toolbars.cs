// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — style, view and picking toolbar construction and handlers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.GUI;

namespace Tal
{
    public partial class MainForm
    {
        #region Style Strip

        private void InitializeStyleStrip()
        {
            _styleStrip = new ToolStrip();
            _styleStrip.Dock = DockStyle.Top;
            _styleStrip.BackColor = _toolStripBackColor;
            _styleStrip.ForeColor = _toolStripForeColor;
            _styleStrip.Font = new System.Drawing.Font("Segoe UI", 10F);
            _styleStrip.Padding = new Padding(5, 2, 5, 2);
            _styleStrip.Margin = new Padding(0, 0, 0, 0);
            _styleStrip.GripStyle = ToolStripGripStyle.Hidden;
            _styleStrip.CanOverflow = false;

            _styleComboBox = new ToolStripComboBox();
            _styleComboBox.Items.AddRange(new object[] { "CPK", "Sticks", "Balls and Sticks", "Ellipsoids", "Polyhedrals" });
            _styleComboBox.SelectedIndex = 2;
            _styleComboBox.AutoSize = false;
            _styleComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _styleComboBox.Width = 180;
            _styleComboBox.BackColor = _comboBoxBackColor;
            _styleComboBox.ForeColor = _comboBoxForeColor;
            _styleComboBox.SelectedIndexChanged += StyleComboBox_SelectedIndexChanged;

            _styleStrip.Items.Add(new ToolStripLabel("Style:"));
            _styleStrip.Items.Add(_styleComboBox);

            this.Controls.Add(_styleStrip);
        }

        private void StyleComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int style = _styleComboBox.SelectedIndex;
            var molecule = GetActiveMolecule();

            bool hasSelection = false;
            if (molecule != null)
            {
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    if (molecule.atom[i].selected)
                    {
                        hasSelection = true;
                        break;
                    }
                }
            }

            if (hasSelection)
            {
                SetStyleForSelectedAtoms(style);
            }
            else
            {
                Flags.Instance.Presentation = style;
                foreach (var doc in _dockPanel.Documents)
                {
                    if (doc is MonoGameDocument monoDoc)
                    {
                        monoDoc.SetStyle(style);
                    }
                }
            }
            ApplyStyleToOverlaySelection(style);
        }

        public void SetStyleForSelectedAtoms(int styleIndex)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            int selectedCount = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected)
                {
                    molecule.atom[i].style = styleIndex;
                    selectedCount++;
                }
            }

            if (selectedCount > 0)
            {
                SetStatus($"Changed style for {selectedCount} selected atoms", "Structures: 0", "🎨");
                RefreshActiveDocument();
            }
            else
            {
                SetStatus("No atoms selected - style change applied to all atoms", "Structures: 0", "⚠️");
                var active = GetActiveDocument();
                active?.SetStyle(styleIndex);
            }
        }

        #endregion

        #region View Strip

        private void InitializeViewStrip()
        {
            _viewStrip = new ToolStrip();
            _viewStrip.Dock = DockStyle.Top;
            _viewStrip.BackColor = _toolStripBackColor;
            _viewStrip.ForeColor = _toolStripForeColor;
            _viewStrip.Font = new System.Drawing.Font("Segoe UI", 10F);
            _viewStrip.Padding = new Padding(5, 2, 5, 2);
            _viewStrip.Margin = new Padding(0, 0, 0, 0);
            _viewStrip.GripStyle = ToolStripGripStyle.Hidden;
            _viewStrip.CanOverflow = true;

            // Animate button
            _btnAnimate = new ToolStripButton("▶ Animate");
            _btnAnimate.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnAnimate.ForeColor = _toolStripForeColor;
            _btnAnimate.BackColor = System.Drawing.Color.Transparent;
            _btnAnimate.ToolTipText = "Animate structure";
            _btnAnimate.Click += BtnAnimate_Click;
            _viewStrip.Items.Add(_btnAnimate);

            _viewStrip.Items.Add(new ToolStripSeparator());

            // View buttons
            _viewStrip.Items.Add(new ToolStripLabel("View:"));

            _btnViewA = CreateViewButton("a", "View along a");
            _viewStrip.Items.Add(_btnViewA);

            _btnViewB = CreateViewButton("b", "View along b");
            _viewStrip.Items.Add(_btnViewB);

            _btnViewC = CreateViewButton("c", "View along c");
            _viewStrip.Items.Add(_btnViewC);

            _btnViewAStar = CreateViewButton("a*", "View along a*");
            _viewStrip.Items.Add(_btnViewAStar);

            _btnViewBStar = CreateViewButton("b*", "View along b*");
            _viewStrip.Items.Add(_btnViewBStar);

            _btnViewCStar = CreateViewButton("c*", "View along c*");
            _viewStrip.Items.Add(_btnViewCStar);

            _viewStrip.Items.Add(new ToolStripSeparator());

            // Rotate 1° buttons
            _viewStrip.Items.Add(new ToolStripLabel("Rot 1°:"));

            _btnViewXPlus = CreateViewButton("+X", "Rotate +1° around X");
            _viewStrip.Items.Add(_btnViewXPlus);

            _btnViewXMinus = CreateViewButton("-X", "Rotate -1° around X");
            _viewStrip.Items.Add(_btnViewXMinus);

            _btnViewYPlus = CreateViewButton("+Y", "Rotate +1° around Y");
            _viewStrip.Items.Add(_btnViewYPlus);

            _btnViewYMinus = CreateViewButton("-Y", "Rotate -1° around Y");
            _viewStrip.Items.Add(_btnViewYMinus);

            _btnViewZPlus = CreateViewButton("+Z", "Rotate +1° around Z");
            _viewStrip.Items.Add(_btnViewZPlus);

            _btnViewZMinus = CreateViewButton("-Z", "Rotate -1° around Z");
            _viewStrip.Items.Add(_btnViewZMinus);

            _viewStrip.Items.Add(new ToolStripSeparator());

            // Rotate 90° buttons
            _viewStrip.Items.Add(new ToolStripLabel("Rot 90°:"));

            _btnRotateLeft90 = new ToolStripButton("←90");
            _btnRotateLeft90.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnRotateLeft90.ForeColor = _toolStripForeColor;
            _btnRotateLeft90.BackColor = System.Drawing.Color.Transparent;
            _btnRotateLeft90.ToolTipText = "Rotate 90° left";
            _btnRotateLeft90.Click += BtnRotateLeft90_Click;
            _viewStrip.Items.Add(_btnRotateLeft90);

            _btnRotateRight90 = new ToolStripButton("→90");
            _btnRotateRight90.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnRotateRight90.ForeColor = _toolStripForeColor;
            _btnRotateRight90.BackColor = System.Drawing.Color.Transparent;
            _btnRotateRight90.ToolTipText = "Rotate 90° right";
            _btnRotateRight90.Click += BtnRotateRight90_Click;
            _viewStrip.Items.Add(_btnRotateRight90);

            _btnRotateUp90 = new ToolStripButton("↑90");
            _btnRotateUp90.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnRotateUp90.ForeColor = _toolStripForeColor;
            _btnRotateUp90.BackColor = System.Drawing.Color.Transparent;
            _btnRotateUp90.ToolTipText = "Rotate 90° up";
            _btnRotateUp90.Click += BtnRotateUp90_Click;
            _viewStrip.Items.Add(_btnRotateUp90);

            _btnRotateDown90 = new ToolStripButton("↓90");
            _btnRotateDown90.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnRotateDown90.ForeColor = _toolStripForeColor;
            _btnRotateDown90.BackColor = System.Drawing.Color.Transparent;
            _btnRotateDown90.ToolTipText = "Rotate 90° down";
            _btnRotateDown90.Click += BtnRotateDown90_Click;
            _viewStrip.Items.Add(_btnRotateDown90);

            _viewStrip.Items.Add(new ToolStripSeparator());

            // Move buttons
            _viewStrip.Items.Add(new ToolStripLabel("Move:"));

            _btnMoveLeft = new ToolStripButton("←");
            _btnMoveLeft.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnMoveLeft.ForeColor = _toolStripForeColor;
            _btnMoveLeft.BackColor = System.Drawing.Color.Transparent;
            _btnMoveLeft.ToolTipText = "Move left";
            _btnMoveLeft.Click += BtnMoveLeft_Click;
            _viewStrip.Items.Add(_btnMoveLeft);

            _btnMoveRight = new ToolStripButton("→");
            _btnMoveRight.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnMoveRight.ForeColor = _toolStripForeColor;
            _btnMoveRight.BackColor = System.Drawing.Color.Transparent;
            _btnMoveRight.ToolTipText = "Move right";
            _btnMoveRight.Click += BtnMoveRight_Click;
            _viewStrip.Items.Add(_btnMoveRight);

            _btnMoveUp = new ToolStripButton("↑");
            _btnMoveUp.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnMoveUp.ForeColor = _toolStripForeColor;
            _btnMoveUp.BackColor = System.Drawing.Color.Transparent;
            _btnMoveUp.ToolTipText = "Move up";
            _btnMoveUp.Click += BtnMoveUp_Click;
            _viewStrip.Items.Add(_btnMoveUp);

            _btnMoveDown = new ToolStripButton("↓");
            _btnMoveDown.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnMoveDown.ForeColor = _toolStripForeColor;
            _btnMoveDown.BackColor = System.Drawing.Color.Transparent;
            _btnMoveDown.ToolTipText = "Move down";
            _btnMoveDown.Click += BtnMoveDown_Click;
            _viewStrip.Items.Add(_btnMoveDown);

            _viewStrip.Items.Add(new ToolStripSeparator());

            // Zoom buttons
            _btnZoomIn = new ToolStripButton("Zoom+");
            _btnZoomIn.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnZoomIn.ForeColor = _toolStripForeColor;
            _btnZoomIn.BackColor = System.Drawing.Color.Transparent;
            _btnZoomIn.ToolTipText = "Zoom in";
            _btnZoomIn.Click += BtnZoomIn_Click;
            _viewStrip.Items.Add(_btnZoomIn);

            _btnZoomOut = new ToolStripButton("Zoom-");
            _btnZoomOut.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnZoomOut.ForeColor = _toolStripForeColor;
            _btnZoomOut.BackColor = System.Drawing.Color.Transparent;
            _btnZoomOut.ToolTipText = "Zoom out";
            _btnZoomOut.Click += BtnZoomOut_Click;
            _viewStrip.Items.Add(_btnZoomOut);

            _viewStrip.Items.Add(new ToolStripSeparator());

            _btnPerspective = new ToolStripButton("Persp.");
            _btnPerspective.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnPerspective.ForeColor = _toolStripForeColor;
            _btnPerspective.BackColor = System.Drawing.Color.Transparent;
            _btnPerspective.ToolTipText = "Toggle perspective/orthographic view";
            _btnPerspective.CheckOnClick = true;

            // ⭐ Set state from flags — consistent with Flags.PerspectiveMode
            bool initialPerspective = Flags.Instance.PerspectiveMode;
            _btnPerspective.Checked = initialPerspective;
            _btnPerspective.Text = initialPerspective ? "Ortho." : "Persp.";

            _btnPerspective.Click += BtnPerspective_Click;
            _viewStrip.Items.Add(_btnPerspective);

            this.Controls.Add(_viewStrip);
        }

        private ToolStripButton CreateViewButton(string text, string tooltip)
        {
            var btn = new ToolStripButton(text);
            btn.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btn.ForeColor = _toolStripForeColor;
            btn.BackColor = System.Drawing.Color.Transparent;
            btn.ToolTipText = tooltip;
            btn.Click += (s, e) => OnViewButtonClick(text);
            return btn;
        }

        #endregion

        #region Picking Strip

        private void InitializePickingStrip()
        {
            _pickingStrip = new ToolStrip();
            _pickingStrip.Dock = DockStyle.Top;
            _pickingStrip.BackColor = _toolStripBackColor;
            _pickingStrip.ForeColor = _toolStripForeColor;
            _pickingStrip.Font = new System.Drawing.Font("Segoe UI", 10F);
            _pickingStrip.Padding = new Padding(5, 2, 5, 2);
            _pickingStrip.Margin = new Padding(0, 0, 0, 0);
            _pickingStrip.GripStyle = ToolStripGripStyle.Hidden;
            _pickingStrip.CanOverflow = true;

            _pickingModeComboBox = new ToolStripComboBox();
            _pickingModeComboBox.Items.AddRange(new object[]
            {
                "Rotate",
                "Pick Atoms",
                "Lasso Atoms",
                "Expand Contacts",
                "Toggle Labels",
                "Move Labels",
                "Measure Distances",
                "Measure Angles",
                "Measure Torsions",
                "Reveal Symmetry-Generated Molecules"
            });
            _pickingModeComboBox.SelectedIndex = 0;
            _pickingModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _pickingModeComboBox.Width = 180;
            _pickingModeComboBox.AutoSize = false;
            _pickingStrip.CanOverflow = false;
            _pickingModeComboBox.ComboBox.DropDownWidth = 350;
            _pickingModeComboBox.BackColor = _comboBoxBackColor;
            _pickingModeComboBox.ForeColor = _comboBoxForeColor;
            _pickingModeComboBox.SelectedIndexChanged += PickingModeComboBox_SelectedIndexChanged;

            _pickingStrip.Items.Add(new ToolStripLabel("Picking Mode:"));
            _pickingStrip.Items.Add(_pickingModeComboBox);
            _pickingStrip.Items.Add(new ToolStripSeparator());

            _btnCleanMeasurements = new ToolStripButton("Clear Measurements");
            _btnCleanMeasurements.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnCleanMeasurements.Font = new Font("Segoe UI", 8F);
            _btnCleanMeasurements.ForeColor = _toolStripForeColor;
            _btnCleanMeasurements.BackColor = System.Drawing.Color.Transparent;
            _btnCleanMeasurements.Click += BtnCleanMeasurements_Click;
            _pickingStrip.Items.Add(_btnCleanMeasurements);
            _pickingStrip.Items.Add(new ToolStripSeparator());

            _showLabelsComboBox = new ToolStripComboBox();
            _showLabelsComboBox.Items.AddRange(new object[]
            {
                "All atoms",
                "Not hydrogen atoms",
                "Selected atoms",
                "None"
            });
            _showLabelsComboBox.SelectedIndex = 0;
            _showLabelsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _showLabelsComboBox.Width = 150;
            _showLabelsComboBox.BackColor = _comboBoxBackColor;
            _showLabelsComboBox.ForeColor = _comboBoxForeColor;
            _showLabelsComboBox.SelectedIndexChanged += ShowLabelsComboBox_SelectedIndexChanged;

            _pickingStrip.Items.Add(new ToolStripLabel("Show Labels for:"));
            _pickingStrip.Items.Add(_showLabelsComboBox);
            _pickingStrip.Items.Add(new ToolStripSeparator());

            _btnPickAtom = new ToolStripButton("👆 Pick");
            _btnPickAtom.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnPickAtom.ForeColor = _toolStripForeColor;
            _btnPickAtom.BackColor = System.Drawing.Color.Transparent;
            _btnPickAtom.ToolTipText = "Pick Atom";
            _btnPickAtom.Click += BtnPickAtom_Click;
            _pickingStrip.Items.Add(_btnPickAtom);

            _btnLasso = new ToolStripButton();
            _btnLasso.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            _btnLasso.Image = Properties.Resources.lasso;
            _btnLasso.ImageScaling = ToolStripItemImageScaling.SizeToFit;
            _btnLasso.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            _btnLasso.Text = " Lasso";
            _btnLasso.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            _btnLasso.ForeColor = _toolStripForeColor;
            _btnLasso.BackColor = System.Drawing.Color.Transparent;
            _btnLasso.ToolTipText = "Lasso Selection";
            _btnLasso.Click += BtnLasso_Click;
            _pickingStrip.Items.Add(_btnLasso);

            _btnSelectAll = new ToolStripButton("Select All");
            _btnSelectAll.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnSelectAll.ForeColor = _toolStripForeColor;
            _btnSelectAll.BackColor = System.Drawing.Color.Transparent;
            _btnSelectAll.ToolTipText = "Select all atoms";
            _btnSelectAll.Click += BtnSelectAll_Click;
            _pickingStrip.Items.Add(_btnSelectAll);

            _btnSelectAllExceptHydrogens = new ToolStripButton("Select All (no H)");
            _btnSelectAllExceptHydrogens.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnSelectAllExceptHydrogens.ForeColor = _toolStripForeColor;
            _btnSelectAllExceptHydrogens.BackColor = System.Drawing.Color.Transparent;
            _btnSelectAllExceptHydrogens.ToolTipText = "Select all atoms except hydrogen";
            _btnSelectAllExceptHydrogens.Click += BtnSelectAllExceptHydrogens_Click;
            _pickingStrip.Items.Add(_btnSelectAllExceptHydrogens);

            _btnClearSelection = new ToolStripButton("Clear Selection");
            _btnClearSelection.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnClearSelection.ForeColor = _toolStripForeColor;
            _btnClearSelection.BackColor = System.Drawing.Color.Transparent;
            _btnClearSelection.ToolTipText = "Clear selection";
            _btnClearSelection.Click += BtnClearSelection_Click;
            _pickingStrip.Items.Add(_btnClearSelection);
            _pickingStrip.Items.Add(new ToolStripSeparator());

            _btnRevealSymmetry = new ToolStripButton("🔮 Reveal Sym");
            _btnRevealSymmetry.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnRevealSymmetry.ForeColor = _toolStripForeColor;
            _btnRevealSymmetry.BackColor = System.Drawing.Color.Transparent;
            _btnRevealSymmetry.ToolTipText = "Reveal Symmetry Molecules";
            _btnRevealSymmetry.Click += BtnRevealSymmetry_Click;
            _pickingStrip.Items.Add(_btnRevealSymmetry);
            _pickingStrip.Items.Add(new ToolStripSeparator());

            _btnCavity = new ToolStripButton("Cavity");
            _btnCavity.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnCavity.ForeColor = _toolStripForeColor;
            _btnCavity.BackColor = System.Drawing.Color.Transparent;
            _btnCavity.Click += BtnCavity_Click;
            _pickingStrip.Items.Add(_btnCavity);

            _btnCavityReport = new ToolStripButton("Cavity Report");
            _btnCavityReport.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _btnCavityReport.ForeColor = _toolStripForeColor;
            _btnCavityReport.BackColor = System.Drawing.Color.Transparent;
            _btnCavityReport.Click += BtnCavityReport_Click;
            _pickingStrip.Items.Add(_btnCavityReport);

            this.Controls.Add(_pickingStrip);
        }

        #endregion


    }
}
