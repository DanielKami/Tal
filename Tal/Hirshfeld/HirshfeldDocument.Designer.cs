// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldDocument.Designer.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal.Hirshfeld
{
    partial class HirshfeldDocument
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this._panel = new System.Windows.Forms.Panel();
            this._controlsHost = new System.Windows.Forms.Panel();
            this._controls = new System.Windows.Forms.FlowLayoutPanel();

 // ── Header ──
            this._lblHeader = new System.Windows.Forms.Label();

 // ── Molecule ──
            this._lblMoleculeTitle = new System.Windows.Forms.Label();
            this._cmbMolecule = new System.Windows.Forms.ComboBox();
            this._btnUseSelection = new System.Windows.Forms.Button();

 // ── Density model ──
            this._lblDensityModelTitle = new System.Windows.Forms.Label();
            this._cmbDensityModel = new System.Windows.Forms.ComboBox();

 // ── Parameters ──
            this._lblVoxelSizeTitle = new System.Windows.Forms.Label();
            this._numVoxelSize = new System.Windows.Forms.NumericUpDown();
            this._lblSigmaMultTitle = new System.Windows.Forms.Label();
            this._numSigmaMult = new System.Windows.Forms.NumericUpDown();
            this._lblIsoLevelTitle = new System.Windows.Forms.Label();
            this._numIsoLevel = new System.Windows.Forms.NumericUpDown();
            this._lblMarginTitle = new System.Windows.Forms.Label();
            this._numMargin = new System.Windows.Forms.NumericUpDown();

 // ── Density mode ──
            this._lblDensityModeTitle = new System.Windows.Forms.Label();
            this._cmbDensityMode = new System.Windows.Forms.ComboBox();

 // ⭐ ── Advanced ──
            this._chkAdvanced = new System.Windows.Forms.CheckBox();
            this._pnlAdvanced = new System.Windows.Forms.Panel();
            this._lblAdvStep = new System.Windows.Forms.Label();
            this._numAdvStep = new System.Windows.Forms.NumericUpDown();
            this._lblAdvExpCutoff = new System.Windows.Forms.Label();
            this._numAdvExpCutoff = new System.Windows.Forms.NumericUpDown();
            this._lblAdvMaxRMax = new System.Windows.Forms.Label();
            this._numAdvMaxRMax = new System.Windows.Forms.NumericUpDown();
            this._btnAdvRebuild = new System.Windows.Forms.Button();

 // ── Property ──
            this._lblPropertyTitle = new System.Windows.Forms.Label();
            this._cmbPropertyMode = new System.Windows.Forms.ComboBox();

 // ── Transparency ──
            this._lblTransparencyTitle = new System.Windows.Forms.Label();
            this._lblTransparencyValue = new System.Windows.Forms.Label();
            this._trackTransparency = new System.Windows.Forms.TrackBar();

 // ── d_norm scale ──
            this._lblDNormScaleTitle = new System.Windows.Forms.Label();
            this._lblDNormScaleValue = new System.Windows.Forms.Label();
            this._trackDNormScale = new System.Windows.Forms.TrackBar();

 // ── Checkboxes ──
            this._chkShowMesh = new System.Windows.Forms.CheckBox();
            this._chkShowWireframe = new System.Windows.Forms.CheckBox();
            this._chkShowMolecule = new System.Windows.Forms.CheckBox();
            this._chkShowAxes = new System.Windows.Forms.CheckBox();
            this._chkShowCell = new System.Windows.Forms.CheckBox();

 // ── Progress + status ──
            this._progressBar = new System.Windows.Forms.ProgressBar();
            this._lblStatus = new System.Windows.Forms.Label();

 // ── Buttons ──
            this._btnGenerate = new System.Windows.Forms.Button();
            this._btnClear = new System.Windows.Forms.Button();
            this._btnSavePly = new System.Windows.Forms.Button();
            this._btnExportPng = new System.Windows.Forms.Button();
            this._btnFingerprint = new System.Windows.Forms.Button();
            this._btnShowDiagnostics = new System.Windows.Forms.Button();

 // ── Info ──
            this._lblZoomInfo = new System.Windows.Forms.Label();
            this._lblHint = new System.Windows.Forms.Label();

            this._controlsHost.SuspendLayout();
            this._controls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numVoxelSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numSigmaMult)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numIsoLevel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numMargin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numAdvStep)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numAdvExpCutoff)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numAdvMaxRMax)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._trackTransparency)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._trackDNormScale)).BeginInit();
            this.SuspendLayout();

 // ═══════════════════════════════════════════════════════════
 // _panel (canvas 3D)
 // ═══════════════════════════════════════════════════════════
            this._panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._panel.Location = new System.Drawing.Point(0, 0);
            this._panel.Name = "_panel";
            this._panel.Size = new System.Drawing.Size(700, 700);
            this._panel.TabIndex = 0;
            this._panel.BackColor = System.Drawing.Color.FromArgb(240, 240, 245);

 // ═══════════════════════════════════════════════════════════
 // _controlsHost
 // ═══════════════════════════════════════════════════════════
            this._controlsHost.Dock = System.Windows.Forms.DockStyle.Right;
            this._controlsHost.Location = new System.Drawing.Point(700, 0);
            this._controlsHost.Name = "_controlsHost";
            this._controlsHost.Size = new System.Drawing.Size(300, 700);
            this._controlsHost.TabIndex = 1;
            this._controlsHost.BackColor = System.Drawing.Color.FromArgb(245, 245, 250);

 // ═══════════════════════════════════════════════════════════
 // _controls
 // ═══════════════════════════════════════════════════════════
            this._controls.Dock = System.Windows.Forms.DockStyle.Fill;
            this._controls.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this._controls.WrapContents = false;
            this._controls.AutoScroll = true;
            this._controls.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this._controls.BackColor = System.Drawing.Color.FromArgb(245, 245, 250);
            this._controls.Name = "_controls";
            this._controls.TabIndex = 0;

 // ═══════════════════════════════════════════════════════════
 // _lblHeader
 // ═══════════════════════════════════════════════════════════
            this._lblHeader.AutoSize = false;
            this._lblHeader.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this._lblHeader.ForeColor = System.Drawing.Color.FromArgb(40, 40, 60);
            this._lblHeader.Location = new System.Drawing.Point(13, 8);
            this._lblHeader.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this._lblHeader.Name = "_lblHeader";
            this._lblHeader.Size = new System.Drawing.Size(260, 22);
            this._lblHeader.TabIndex = 0;
            this._lblHeader.Text = "Hirshfeld Surface";
            this._lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

 // ═══════════════════════════════════════════════════════════
 // _lblMoleculeTitle
 // ═══════════════════════════════════════════════════════════
            this._lblMoleculeTitle.AutoSize = false;
            this._lblMoleculeTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblMoleculeTitle.Location = new System.Drawing.Point(13, 36);
            this._lblMoleculeTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblMoleculeTitle.Name = "_lblMoleculeTitle";
            this._lblMoleculeTitle.Size = new System.Drawing.Size(260, 15);
            this._lblMoleculeTitle.TabIndex = 1;
            this._lblMoleculeTitle.Text = "Target molecule:";

 // ═══════════════════════════════════════════════════════════
 // _cmbMolecule
 // ═══════════════════════════════════════════════════════════
            this._cmbMolecule.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbMolecule.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._cmbMolecule.FormattingEnabled = true;
            this._cmbMolecule.Location = new System.Drawing.Point(13, 54);
            this._cmbMolecule.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._cmbMolecule.Name = "_cmbMolecule";
            this._cmbMolecule.Size = new System.Drawing.Size(260, 21);
            this._cmbMolecule.TabIndex = 2;

 // ═══════════════════════════════════════════════════════════
 // _btnUseSelection
 // ═══════════════════════════════════════════════════════════
            this._btnUseSelection.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnUseSelection.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnUseSelection.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnUseSelection.Location = new System.Drawing.Point(13, 81);
            this._btnUseSelection.Margin = new System.Windows.Forms.Padding(3, 0, 3, 4);
            this._btnUseSelection.Name = "_btnUseSelection";
            this._btnUseSelection.Size = new System.Drawing.Size(260, 26);
            this._btnUseSelection.TabIndex = 3;
            this._btnUseSelection.Text = "Use current selection";
            this._btnUseSelection.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _lblDensityModelTitle
 // ═══════════════════════════════════════════════════════════
            this._lblDensityModelTitle.AutoSize = false;
            this._lblDensityModelTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblDensityModelTitle.Location = new System.Drawing.Point(13, 115);
            this._lblDensityModelTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblDensityModelTitle.Name = "_lblDensityModelTitle";
            this._lblDensityModelTitle.Size = new System.Drawing.Size(260, 15);
            this._lblDensityModelTitle.TabIndex = 4;
            this._lblDensityModelTitle.Text = "Density model:";

 // ═══════════════════════════════════════════════════════════
 // _cmbDensityModel
 // ═══════════════════════════════════════════════════════════
            this._cmbDensityModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbDensityModel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._cmbDensityModel.FormattingEnabled = true;
            this._cmbDensityModel.Items.AddRange(new object[] {
                                                "Spherical (isotropic)",
                                                "Anisotropic (ADP)"});
            this._cmbDensityModel.SelectedIndex = 0;
            this._cmbDensityModel.Location = new System.Drawing.Point(13, 133);
            this._cmbDensityModel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._cmbDensityModel.Name = "_cmbDensityModel";
            this._cmbDensityModel.Size = new System.Drawing.Size(260, 21);
            this._cmbDensityModel.TabIndex = 5;

 // ═══════════════════════════════════════════════════════════
 // _lblVoxelSizeTitle
 // ═══════════════════════════════════════════════════════════
            this._lblVoxelSizeTitle.AutoSize = false;
            this._lblVoxelSizeTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblVoxelSizeTitle.Location = new System.Drawing.Point(13, 161);
            this._lblVoxelSizeTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblVoxelSizeTitle.Name = "_lblVoxelSizeTitle";
            this._lblVoxelSizeTitle.Size = new System.Drawing.Size(260, 15);
            this._lblVoxelSizeTitle.TabIndex = 6;
            this._lblVoxelSizeTitle.Text = "Voxel size (Å):";

 // ═══════════════════════════════════════════════════════════
 // _numVoxelSize — 0.10 Å by default
 // ═══════════════════════════════════════════════════════════
            this._numVoxelSize.DecimalPlaces = 2;
            this._numVoxelSize.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._numVoxelSize.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this._numVoxelSize.Location = new System.Drawing.Point(13, 179);
            this._numVoxelSize.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._numVoxelSize.Maximum = new decimal(new int[] { 20, 0, 0, 131072 });
            this._numVoxelSize.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            this._numVoxelSize.Name = "_numVoxelSize";
            this._numVoxelSize.Size = new System.Drawing.Size(260, 23);
            this._numVoxelSize.TabIndex = 7;
            this._numVoxelSize.Value = new decimal(new int[] { 10, 0, 0, 131072 }); // 0.10

 // ═══════════════════════════════════════════════════════════
 // _lblSigmaMultTitle
 // ═══════════════════════════════════════════════════════════
            this._lblSigmaMultTitle.AutoSize = false;
            this._lblSigmaMultTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblSigmaMultTitle.Location = new System.Drawing.Point(13, 209);
            this._lblSigmaMultTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblSigmaMultTitle.Name = "_lblSigmaMultTitle";
            this._lblSigmaMultTitle.Size = new System.Drawing.Size(260, 15);
            this._lblSigmaMultTitle.TabIndex = 8;
            this._lblSigmaMultTitle.Text = "Sigma multiplier:";

 // ═══════════════════════════════════════════════════════════
 // _numSigmaMult
 // ═══════════════════════════════════════════════════════════
            this._numSigmaMult.DecimalPlaces = 2;
            this._numSigmaMult.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._numSigmaMult.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            this._numSigmaMult.Location = new System.Drawing.Point(13, 227);
            this._numSigmaMult.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._numSigmaMult.Maximum = new decimal(new int[] { 200, 0, 0, 131072 });
            this._numSigmaMult.Minimum = new decimal(new int[] { 80, 0, 0, 131072 });
            this._numSigmaMult.Name = "_numSigmaMult";
            this._numSigmaMult.Size = new System.Drawing.Size(260, 23);
            this._numSigmaMult.TabIndex = 9;
            this._numSigmaMult.Value = new decimal(new int[] { 100, 0, 0, 131072 });

 // ═══════════════════════════════════════════════════════════
 // _lblIsoLevelTitle
 // ═══════════════════════════════════════════════════════════
            this._lblIsoLevelTitle.AutoSize = false;
            this._lblIsoLevelTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblIsoLevelTitle.Location = new System.Drawing.Point(13, 257);
            this._lblIsoLevelTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblIsoLevelTitle.Name = "_lblIsoLevelTitle";
            this._lblIsoLevelTitle.Size = new System.Drawing.Size(260, 15);
            this._lblIsoLevelTitle.TabIndex = 10;
            this._lblIsoLevelTitle.Text = "Iso level:";

 // ═══════════════════════════════════════════════════════════
 // _numIsoLevel
 // ═══════════════════════════════════════════════════════════
            this._numIsoLevel.DecimalPlaces = 2;
            this._numIsoLevel.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._numIsoLevel.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this._numIsoLevel.Location = new System.Drawing.Point(13, 275);
            this._numIsoLevel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._numIsoLevel.Maximum = new decimal(new int[] { 70, 0, 0, 131072 });
            this._numIsoLevel.Minimum = new decimal(new int[] { 30, 0, 0, 131072 });
            this._numIsoLevel.Name = "_numIsoLevel";
            this._numIsoLevel.Size = new System.Drawing.Size(260, 23);
            this._numIsoLevel.TabIndex = 11;
            this._numIsoLevel.Value = new decimal(new int[] { 50, 0, 0, 131072 });

 // ═══════════════════════════════════════════════════════════
 // _lblMarginTitle
 // ═══════════════════════════════════════════════════════════
            this._lblMarginTitle.AutoSize = false;
            this._lblMarginTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblMarginTitle.Location = new System.Drawing.Point(13, 305);
            this._lblMarginTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblMarginTitle.Name = "_lblMarginTitle";
            this._lblMarginTitle.Size = new System.Drawing.Size(260, 15);
            this._lblMarginTitle.TabIndex = 12;
            this._lblMarginTitle.Text = "Grid margin (Å):";

 // ═══════════════════════════════════════════════════════════
 // _numMargin — 3.00 Å by default
 // ═══════════════════════════════════════════════════════════
            this._numMargin.DecimalPlaces = 2;
            this._numMargin.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._numMargin.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this._numMargin.Location = new System.Drawing.Point(13, 323);
            this._numMargin.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._numMargin.Maximum = new decimal(new int[] { 800, 0, 0, 131072 });
            this._numMargin.Minimum = new decimal(new int[] { 100, 0, 0, 131072 });
            this._numMargin.Name = "_numMargin";
            this._numMargin.Size = new System.Drawing.Size(260, 23);
            this._numMargin.TabIndex = 13;
            this._numMargin.Value = new decimal(new int[] { 300, 0, 0, 131072 }); // 3.00

 // ═══════════════════════════════════════════════════════════
 // _lblDensityModeTitle
 // ═══════════════════════════════════════════════════════════
            this._lblDensityModeTitle.AutoSize = false;
            this._lblDensityModeTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblDensityModeTitle.Location = new System.Drawing.Point(13, 353);
            this._lblDensityModeTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblDensityModeTitle.Name = "_lblDensityModeTitle";
            this._lblDensityModeTitle.Size = new System.Drawing.Size(260, 15);
            this._lblDensityModeTitle.TabIndex = 14;
            this._lblDensityModeTitle.Text = "Density mode:";

 // ═══════════════════════════════════════════════════════════
 // _cmbDensityMode
 // ═══════════════════════════════════════════════════════════
            this._cmbDensityMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbDensityMode.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._cmbDensityMode.FormattingEnabled = true;
            this._cmbDensityMode.Items.AddRange(new object[] {
                                                "STO (Slater-Type Orbitals)",
                                                "Gaussian (approx)"});
            this._cmbDensityMode.SelectedIndex = 0;
            this._cmbDensityMode.Location = new System.Drawing.Point(13, 371);
            this._cmbDensityMode.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this._cmbDensityMode.Name = "_cmbDensityMode";
            this._cmbDensityMode.Size = new System.Drawing.Size(260, 21);
            this._cmbDensityMode.TabIndex = 15;

 // ═══════════════════════════════════════════════════════════
 // ⭐ _chkAdvanced
 // ═══════════════════════════════════════════════════════════
            this._chkAdvanced.AutoSize = true;
            this._chkAdvanced.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._chkAdvanced.ForeColor = System.Drawing.Color.FromArgb(60, 60, 80);
            this._chkAdvanced.Location = new System.Drawing.Point(13, 402);
            this._chkAdvanced.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._chkAdvanced.Name = "_chkAdvanced";
            this._chkAdvanced.Size = new System.Drawing.Size(140, 19);
            this._chkAdvanced.TabIndex = 16;
            this._chkAdvanced.Text = "⚙ Advanced (STO)";
            this._chkAdvanced.UseVisualStyleBackColor = true;

 // ═══════════════════════════════════════════════════════════
 // ⭐ _pnlAdvanced (panel z suwakami)
 // ═══════════════════════════════════════════════════════════
            this._pnlAdvanced.BackColor = System.Drawing.Color.FromArgb(238, 238, 245);
            this._pnlAdvanced.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._pnlAdvanced.Location = new System.Drawing.Point(13, 428);
            this._pnlAdvanced.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this._pnlAdvanced.Name = "_pnlAdvanced";
            this._pnlAdvanced.Size = new System.Drawing.Size(260, 200);
            this._pnlAdvanced.TabIndex = 17;
            this._pnlAdvanced.Visible = false;   // ⭐ ukryty domyślnie

 // ── _lblAdvStep ──
            this._lblAdvStep.AutoSize = false;
            this._lblAdvStep.Font = new System.Drawing.Font("Consolas", 8F);
            this._lblAdvStep.ForeColor = System.Drawing.Color.FromArgb(60, 60, 80);
            this._lblAdvStep.Location = new System.Drawing.Point(8, 8);
            this._lblAdvStep.Name = "_lblAdvStep";
            this._lblAdvStep.Size = new System.Drawing.Size(240, 14);
            this._lblAdvStep.TabIndex = 0;
            this._lblAdvStep.Text = "DEFAULT_STEP (Å):";

 // ── _numAdvStep ──
            this._numAdvStep.DecimalPlaces = 3;
            this._numAdvStep.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._numAdvStep.Increment = new decimal(new int[] { 5, 0, 0, 196608 }); // 0.005
            this._numAdvStep.Location = new System.Drawing.Point(8, 24);
            this._numAdvStep.Maximum = new decimal(new int[] { 100, 0, 0, 196608 }); // 0.100
            this._numAdvStep.Minimum = new decimal(new int[] { 5, 0, 0, 196608 });    // 0.005
            this._numAdvStep.Name = "_numAdvStep";
            this._numAdvStep.Size = new System.Drawing.Size(244, 23);
            this._numAdvStep.TabIndex = 1;
            this._numAdvStep.Value = new decimal(new int[] { 30, 0, 0, 196608 });    // 0.030

 // ── _lblAdvExpCutoff ──
            this._lblAdvExpCutoff.AutoSize = false;
            this._lblAdvExpCutoff.Font = new System.Drawing.Font("Consolas", 8F);
            this._lblAdvExpCutoff.ForeColor = System.Drawing.Color.FromArgb(60, 60, 80);
            this._lblAdvExpCutoff.Location = new System.Drawing.Point(8, 56);
            this._lblAdvExpCutoff.Name = "_lblAdvExpCutoff";
            this._lblAdvExpCutoff.Size = new System.Drawing.Size(240, 14);
            this._lblAdvExpCutoff.TabIndex = 2;
            this._lblAdvExpCutoff.Text = "EXP_CUTOFF (rMax = EC / ξ):";

 // ── _numAdvExpCutoff ──
            this._numAdvExpCutoff.DecimalPlaces = 1;
            this._numAdvExpCutoff.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._numAdvExpCutoff.Increment = new decimal(new int[] { 5, 0, 0, 65536 });  // 0.5
            this._numAdvExpCutoff.Location = new System.Drawing.Point(8, 72);
            this._numAdvExpCutoff.Maximum = new decimal(new int[] { 400, 0, 0, 65536 });  // 40.0
            this._numAdvExpCutoff.Minimum = new decimal(new int[] { 50, 0, 0, 65536 });   // 5.0
            this._numAdvExpCutoff.Name = "_numAdvExpCutoff";
            this._numAdvExpCutoff.Size = new System.Drawing.Size(244, 23);
            this._numAdvExpCutoff.TabIndex = 3;
            this._numAdvExpCutoff.Value = new decimal(new int[] { 100, 0, 0, 65536 });   // 10.0

 // ── _lblAdvMaxRMax ──
            this._lblAdvMaxRMax.AutoSize = false;
            this._lblAdvMaxRMax.Font = new System.Drawing.Font("Consolas", 8F);
            this._lblAdvMaxRMax.ForeColor = System.Drawing.Color.FromArgb(60, 60, 80);
            this._lblAdvMaxRMax.Location = new System.Drawing.Point(8, 104);
            this._lblAdvMaxRMax.Name = "_lblAdvMaxRMax";
            this._lblAdvMaxRMax.Size = new System.Drawing.Size(240, 14);
            this._lblAdvMaxRMax.TabIndex = 4;
            this._lblAdvMaxRMax.Text = "MAX_RMAX (Å):";

 // ── _numAdvMaxRMax ──
            this._numAdvMaxRMax.DecimalPlaces = 1;
            this._numAdvMaxRMax.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._numAdvMaxRMax.Increment = new decimal(new int[] { 10, 0, 0, 65536 });  // 1.0
            this._numAdvMaxRMax.Location = new System.Drawing.Point(8, 120);
            this._numAdvMaxRMax.Maximum = new decimal(new int[] { 500, 0, 0, 65536 });   // 50.0
            this._numAdvMaxRMax.Minimum = new decimal(new int[] { 50, 0, 0, 65536 });    // 5.0
            this._numAdvMaxRMax.Name = "_numAdvMaxRMax";
            this._numAdvMaxRMax.Size = new System.Drawing.Size(244, 23);
            this._numAdvMaxRMax.TabIndex = 5;
            this._numAdvMaxRMax.Value = new decimal(new int[] { 150, 0, 0, 65536 });     // 15.0

 // ── _btnAdvRebuild ──
            this._btnAdvRebuild.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnAdvRebuild.BackColor = System.Drawing.Color.FromArgb(255, 220, 180);
            this._btnAdvRebuild.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this._btnAdvRebuild.Location = new System.Drawing.Point(8, 152);
            this._btnAdvRebuild.Name = "_btnAdvRebuild";
            this._btnAdvRebuild.Size = new System.Drawing.Size(244, 36);
            this._btnAdvRebuild.TabIndex = 6;
            this._btnAdvRebuild.Text = "Rebuild LUT cache && Regenerate";
            this._btnAdvRebuild.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _lblPropertyTitle
 // ═══════════════════════════════════════════════════════════
            this._lblPropertyTitle.AutoSize = false;
            this._lblPropertyTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblPropertyTitle.Location = new System.Drawing.Point(13, 638);
            this._lblPropertyTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblPropertyTitle.Name = "_lblPropertyTitle";
            this._lblPropertyTitle.Size = new System.Drawing.Size(260, 15);
            this._lblPropertyTitle.TabIndex = 18;
            this._lblPropertyTitle.Text = "Property:";

 // ═══════════════════════════════════════════════════════════
 // _cmbPropertyMode
 // ═══════════════════════════════════════════════════════════
            this._cmbPropertyMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbPropertyMode.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._cmbPropertyMode.FormattingEnabled = true;
            this._cmbPropertyMode.Items.AddRange(new object[] {
                                                "Base color",
                                                "d_norm (blue-white-red)",
                                                "d_e (external)",
                                                "d_i (internal)",
                                                "Charge (green-yellow-red)",
                                                "Hydrophobicity (blue-white-brown)"});
            this._cmbPropertyMode.SelectedIndex = 1;
            this._cmbPropertyMode.Location = new System.Drawing.Point(13, 656);
            this._cmbPropertyMode.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this._cmbPropertyMode.Name = "_cmbPropertyMode";
            this._cmbPropertyMode.Size = new System.Drawing.Size(260, 21);
            this._cmbPropertyMode.TabIndex = 19;

 // ═══════════════════════════════════════════════════════════
 // _lblTransparencyTitle
 // ═══════════════════════════════════════════════════════════
            this._lblTransparencyTitle.AutoSize = false;
            this._lblTransparencyTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblTransparencyTitle.Location = new System.Drawing.Point(13, 687);
            this._lblTransparencyTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblTransparencyTitle.Name = "_lblTransparencyTitle";
            this._lblTransparencyTitle.Size = new System.Drawing.Size(260, 15);
            this._lblTransparencyTitle.TabIndex = 20;
            this._lblTransparencyTitle.Text = "Transparency:";

 // ═══════════════════════════════════════════════════════════
 // _lblTransparencyValue
 // ═══════════════════════════════════════════════════════════
            this._lblTransparencyValue.AutoSize = false;
            this._lblTransparencyValue.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Bold);
            this._lblTransparencyValue.ForeColor = System.Drawing.Color.FromArgb(0, 90, 160);
            this._lblTransparencyValue.Location = new System.Drawing.Point(13, 702);
            this._lblTransparencyValue.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this._lblTransparencyValue.Name = "_lblTransparencyValue";
            this._lblTransparencyValue.Size = new System.Drawing.Size(260, 15);
            this._lblTransparencyValue.TabIndex = 21;
            this._lblTransparencyValue.Text = "200 / 255";
            this._lblTransparencyValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

 // ═══════════════════════════════════════════════════════════
 // _trackTransparency
 // ═══════════════════════════════════════════════════════════
            this._trackTransparency.Location = new System.Drawing.Point(13, 720);
            this._trackTransparency.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this._trackTransparency.Maximum = 255;
            this._trackTransparency.Minimum = 40;
            this._trackTransparency.Name = "_trackTransparency";
            this._trackTransparency.Size = new System.Drawing.Size(260, 30);
            this._trackTransparency.TabIndex = 22;
            this._trackTransparency.TickFrequency = 50;
            this._trackTransparency.Value = 200;

 // ═══════════════════════════════════════════════════════════
 // _lblDNormScaleTitle
 // ═══════════════════════════════════════════════════════════
            this._lblDNormScaleTitle.AutoSize = false;
            this._lblDNormScaleTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblDNormScaleTitle.Location = new System.Drawing.Point(13, 760);
            this._lblDNormScaleTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblDNormScaleTitle.Name = "_lblDNormScaleTitle";
            this._lblDNormScaleTitle.Size = new System.Drawing.Size(260, 15);
            this._lblDNormScaleTitle.TabIndex = 23;
            this._lblDNormScaleTitle.Text = "d_norm scale:";

 // ═══════════════════════════════════════════════════════════
 // _lblDNormScaleValue
 // ═══════════════════════════════════════════════════════════
            this._lblDNormScaleValue.AutoSize = false;
            this._lblDNormScaleValue.Font = new System.Drawing.Font("Consolas", 8.5F, System.Drawing.FontStyle.Bold);
            this._lblDNormScaleValue.ForeColor = System.Drawing.Color.FromArgb(0, 90, 160);
            this._lblDNormScaleValue.Location = new System.Drawing.Point(13, 775);
            this._lblDNormScaleValue.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this._lblDNormScaleValue.Name = "_lblDNormScaleValue";
            this._lblDNormScaleValue.Size = new System.Drawing.Size(260, 15);
            this._lblDNormScaleValue.TabIndex = 24;
            this._lblDNormScaleValue.Text = "1.00×";
            this._lblDNormScaleValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

 // ═══════════════════════════════════════════════════════════
 // _trackDNormScale
 // ═══════════════════════════════════════════════════════════
            this._trackDNormScale.Location = new System.Drawing.Point(13, 793);
            this._trackDNormScale.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this._trackDNormScale.Maximum = 100;
            this._trackDNormScale.Minimum = 0;
            this._trackDNormScale.Name = "_trackDNormScale";
            this._trackDNormScale.Size = new System.Drawing.Size(260, 30);
            this._trackDNormScale.TabIndex = 25;
            this._trackDNormScale.TickFrequency = 10;
            this._trackDNormScale.Value = 31;

 // ═══════════════════════════════════════════════════════════
 // _chkShowMesh
 // ═══════════════════════════════════════════════════════════
            this._chkShowMesh.AutoSize = true;
            this._chkShowMesh.Checked = true;
            this._chkShowMesh.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkShowMesh.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._chkShowMesh.Location = new System.Drawing.Point(13, 833);
            this._chkShowMesh.Margin = new System.Windows.Forms.Padding(3, 4, 3, 1);
            this._chkShowMesh.Name = "_chkShowMesh";
            this._chkShowMesh.Size = new System.Drawing.Size(85, 19);
            this._chkShowMesh.TabIndex = 26;
            this._chkShowMesh.Text = "Show mesh";
            this._chkShowMesh.UseVisualStyleBackColor = true;

 // ═══════════════════════════════════════════════════════════
 // _chkShowWireframe
 // ═══════════════════════════════════════════════════════════
            this._chkShowWireframe.AutoSize = true;
            this._chkShowWireframe.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._chkShowWireframe.Location = new System.Drawing.Point(13, 854);
            this._chkShowWireframe.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._chkShowWireframe.Name = "_chkShowWireframe";
            this._chkShowWireframe.Size = new System.Drawing.Size(112, 19);
            this._chkShowWireframe.TabIndex = 27;
            this._chkShowWireframe.Text = "Show wireframe";
            this._chkShowWireframe.UseVisualStyleBackColor = true;

 // ═══════════════════════════════════════════════════════════
 // _chkShowMolecule
 // ═══════════════════════════════════════════════════════════
            this._chkShowMolecule.AutoSize = true;
            this._chkShowMolecule.Checked = true;
            this._chkShowMolecule.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkShowMolecule.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._chkShowMolecule.Location = new System.Drawing.Point(13, 875);
            this._chkShowMolecule.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._chkShowMolecule.Name = "_chkShowMolecule";
            this._chkShowMolecule.Size = new System.Drawing.Size(105, 19);
            this._chkShowMolecule.TabIndex = 28;
            this._chkShowMolecule.Text = "Show molecule";
            this._chkShowMolecule.UseVisualStyleBackColor = true;

 // ═══════════════════════════════════════════════════════════
 // _chkShowAxes
 // ═══════════════════════════════════════════════════════════
            this._chkShowAxes.AutoSize = true;
            this._chkShowAxes.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._chkShowAxes.Location = new System.Drawing.Point(13, 896);
            this._chkShowAxes.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._chkShowAxes.Name = "_chkShowAxes";
            this._chkShowAxes.Size = new System.Drawing.Size(80, 19);
            this._chkShowAxes.TabIndex = 29;
            this._chkShowAxes.Text = "Show axes";
            this._chkShowAxes.UseVisualStyleBackColor = true;

 // ═══════════════════════════════════════════════════════════
 // _chkShowCell
 // ═══════════════════════════════════════════════════════════
            this._chkShowCell.AutoSize = true;
            this._chkShowCell.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._chkShowCell.Location = new System.Drawing.Point(13, 917);
            this._chkShowCell.Margin = new System.Windows.Forms.Padding(3, 1, 3, 4);
            this._chkShowCell.Name = "_chkShowCell";
            this._chkShowCell.Size = new System.Drawing.Size(105, 19);
            this._chkShowCell.TabIndex = 30;
            this._chkShowCell.Text = "Show unit cell";
            this._chkShowCell.UseVisualStyleBackColor = true;

 // ═══════════════════════════════════════════════════════════
 // _progressBar
 // ═══════════════════════════════════════════════════════════
            this._progressBar.Location = new System.Drawing.Point(13, 946);
            this._progressBar.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
            this._progressBar.Maximum = 100;
            this._progressBar.Minimum = 0;
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(260, 18);
            this._progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this._progressBar.TabIndex = 31;

 // ═══════════════════════════════════════════════════════════
 // _lblStatus
 // ═══════════════════════════════════════════════════════════
            this._lblStatus.AutoSize = false;
            this._lblStatus.Font = new System.Drawing.Font("Consolas", 8.5F);
            this._lblStatus.ForeColor = System.Drawing.Color.FromArgb(0, 90, 160);
            this._lblStatus.Location = new System.Drawing.Point(13, 967);
            this._lblStatus.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(260, 32);
            this._lblStatus.TabIndex = 32;
            this._lblStatus.Text = "Ready";

 // ═══════════════════════════════════════════════════════════
 // _btnGenerate
 // ═══════════════════════════════════════════════════════════
            this._btnGenerate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnGenerate.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnGenerate.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnGenerate.Location = new System.Drawing.Point(13, 1005);
            this._btnGenerate.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._btnGenerate.Name = "_btnGenerate";
            this._btnGenerate.Size = new System.Drawing.Size(260, 28);
            this._btnGenerate.TabIndex = 33;
            this._btnGenerate.Text = "Generate";
            this._btnGenerate.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _btnClear
 // ═══════════════════════════════════════════════════════════
            this._btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClear.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnClear.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnClear.Location = new System.Drawing.Point(13, 1036);
            this._btnClear.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._btnClear.Name = "_btnClear";
            this._btnClear.Size = new System.Drawing.Size(260, 28);
            this._btnClear.TabIndex = 34;
            this._btnClear.Text = "Clear";
            this._btnClear.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _btnSavePly
 // ═══════════════════════════════════════════════════════════
            this._btnSavePly.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSavePly.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnSavePly.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnSavePly.Location = new System.Drawing.Point(13, 1067);
            this._btnSavePly.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._btnSavePly.Name = "_btnSavePly";
            this._btnSavePly.Size = new System.Drawing.Size(260, 28);
            this._btnSavePly.TabIndex = 35;
            this._btnSavePly.Text = "Save .ply";
            this._btnSavePly.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _btnExafterrtPng
 // ═══════════════════════════════════════════════════════════
            this._btnExportPng.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnExportPng.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnExportPng.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnExportPng.Location = new System.Drawing.Point(13, 1098);
            this._btnExportPng.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._btnExportPng.Name = "_btnExportPng";
            this._btnExportPng.Size = new System.Drawing.Size(260, 28);
            this._btnExportPng.TabIndex = 36;
            this._btnExportPng.Text = "Save png";
            this._btnExportPng.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _btnFingerprint
 // ═══════════════════════════════════════════════════════════
            this._btnFingerprint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnFingerprint.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnFingerprint.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnFingerprint.Location = new System.Drawing.Point(13, 1129);
            this._btnFingerprint.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._btnFingerprint.Name = "_btnFingerprint";
            this._btnFingerprint.Size = new System.Drawing.Size(260, 28);
            this._btnFingerprint.TabIndex = 37;
            this._btnFingerprint.Text = "Show Fingerprint...";
            this._btnFingerprint.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _btnShowDiagnostics
 // ═══════════════════════════════════════════════════════════
            this._btnShowDiagnostics.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnShowDiagnostics.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnShowDiagnostics.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnShowDiagnostics.Location = new System.Drawing.Point(13, 1160);
            this._btnShowDiagnostics.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._btnShowDiagnostics.Name = "_btnShowDiagnostics";
            this._btnShowDiagnostics.Size = new System.Drawing.Size(260, 28);
            this._btnShowDiagnostics.TabIndex = 38;
            this._btnShowDiagnostics.Text = "Show Diagnostics in Debug...";
            this._btnShowDiagnostics.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _lblZoomInfo
 // ═══════════════════════════════════════════════════════════
            this._lblZoomInfo.AutoSize = false;
            this._lblZoomInfo.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Bold);
            this._lblZoomInfo.ForeColor = System.Drawing.Color.FromArgb(0, 90, 160);
            this._lblZoomInfo.Location = new System.Drawing.Point(13, 1197);
            this._lblZoomInfo.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this._lblZoomInfo.Name = "_lblZoomInfo";
            this._lblZoomInfo.Size = new System.Drawing.Size(260, 22);
            this._lblZoomInfo.TabIndex = 39;
            this._lblZoomInfo.Text = "Zoom: 3.00× (mouse wheel)";
            this._lblZoomInfo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

 // ═══════════════════════════════════════════════════════════
 // _lblHint
 // ═══════════════════════════════════════════════════════════
            this._lblHint.AutoSize = false;
            this._lblHint.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this._lblHint.ForeColor = System.Drawing.Color.FromArgb(100, 100, 120);
            this._lblHint.Location = new System.Drawing.Point(13, 1224);
            this._lblHint.Margin = new System.Windows.Forms.Padding(3, 5, 3, 0);
            this._lblHint.Name = "_lblHint";
            this._lblHint.Size = new System.Drawing.Size(260, 150);
            this._lblHint.TabIndex = 40;
            this._lblHint.Text = "Left mouse: rotate\r\n" +
                                 "Wheel: zoom\r\n" +
                                 "Right: (drag = move)\r\n" +
                                 "\r\n" +
                                 "Density mode:\r\n" +
                                 "  STO  = Slater-Type Orbitals\r\n" +
                                 "  Gaus = Gaussian (approx)\r\n" +
                                 "\r\n" +
                                 "Advanced panel: STO tuning\r\n" +
                                 "  STEP = LUT resolution\r\n" +
                                 "  EXP_CUTOFF = rMax = EC / ξ\r\n" +
                                 "  MAX_RMAX = hard limit\r\n" +
                                 "\r\n" +
                                 "Charge: green <0, red >0\r\n" +
                                 "Hydro: blue hydro, brown phobic";

 // ═══════════════════════════════════════════════════════════
 // _pnlAdvanced — add controls
 // ═══════════════════════════════════════════════════════════
            this._pnlAdvanced.Controls.Add(this._lblAdvStep);
            this._pnlAdvanced.Controls.Add(this._numAdvStep);
            this._pnlAdvanced.Controls.Add(this._lblAdvExpCutoff);
            this._pnlAdvanced.Controls.Add(this._numAdvExpCutoff);
            this._pnlAdvanced.Controls.Add(this._lblAdvMaxRMax);
            this._pnlAdvanced.Controls.Add(this._numAdvMaxRMax);
            this._pnlAdvanced.Controls.Add(this._btnAdvRebuild);

 // ═══════════════════════════════════════════════════════════
 // _controls — add all
 // ═══════════════════════════════════════════════════════════
            this._controls.Controls.Add(this._lblHeader);
            this._controls.Controls.Add(this._lblMoleculeTitle);
            this._controls.Controls.Add(this._cmbMolecule);
            this._controls.Controls.Add(this._btnUseSelection);
            this._controls.Controls.Add(this._lblDensityModelTitle);
            this._controls.Controls.Add(this._cmbDensityModel);
            this._controls.Controls.Add(this._lblVoxelSizeTitle);
            this._controls.Controls.Add(this._numVoxelSize);
            this._controls.Controls.Add(this._lblSigmaMultTitle);
            this._controls.Controls.Add(this._numSigmaMult);
            this._controls.Controls.Add(this._lblIsoLevelTitle);
            this._controls.Controls.Add(this._numIsoLevel);
            this._controls.Controls.Add(this._lblMarginTitle);
            this._controls.Controls.Add(this._numMargin);
            this._controls.Controls.Add(this._lblDensityModeTitle);
            this._controls.Controls.Add(this._cmbDensityMode);
            this._controls.Controls.Add(this._chkAdvanced);       // ⭐
            this._controls.Controls.Add(this._pnlAdvanced);       // ⭐
            this._controls.Controls.Add(this._lblPropertyTitle);
            this._controls.Controls.Add(this._cmbPropertyMode);
            this._controls.Controls.Add(this._lblTransparencyTitle);
            this._controls.Controls.Add(this._lblTransparencyValue);
            this._controls.Controls.Add(this._trackTransparency);
            this._controls.Controls.Add(this._lblDNormScaleTitle);
            this._controls.Controls.Add(this._lblDNormScaleValue);
            this._controls.Controls.Add(this._trackDNormScale);
            this._controls.Controls.Add(this._chkShowMesh);
            this._controls.Controls.Add(this._chkShowWireframe);
            this._controls.Controls.Add(this._chkShowMolecule);
            this._controls.Controls.Add(this._chkShowAxes);
            this._controls.Controls.Add(this._chkShowCell);
            this._controls.Controls.Add(this._progressBar);
            this._controls.Controls.Add(this._lblStatus);
            this._controls.Controls.Add(this._btnGenerate);
            this._controls.Controls.Add(this._btnClear);
            this._controls.Controls.Add(this._btnSavePly);
            this._controls.Controls.Add(this._btnExportPng);
            this._controls.Controls.Add(this._btnFingerprint);
            this._controls.Controls.Add(this._btnShowDiagnostics);
            this._controls.Controls.Add(this._lblZoomInfo);
            this._controls.Controls.Add(this._lblHint);

 // ═══════════════════════════════════════════════════════════
 // _controlsHost
 // ═══════════════════════════════════════════════════════════
            this._controlsHost.Controls.Add(this._controls);

 // ═══════════════════════════════════════════════════════════
 // HirshfeldDocument
 // ═══════════════════════════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 700);
            this.Controls.Add(this._panel);
            this.Controls.Add(this._controlsHost);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "HirshfeldDocument";
            this.Text = "Hirshfeld Surface";
            this.DockAreas = WeifenLuo.WinFormsUI.Docking.DockAreas.Document
                           | WeifenLuo.WinFormsUI.Docking.DockAreas.Float;

            this._controlsHost.ResumeLayout(false);
            this._controls.ResumeLayout(false);
            this._controls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numVoxelSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numSigmaMult)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numIsoLevel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numMargin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numAdvStep)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numAdvExpCutoff)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numAdvMaxRMax)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._trackTransparency)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._trackDNormScale)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        #region Designer fields

        private System.Windows.Forms.Panel _panel;
        private System.Windows.Forms.Panel _controlsHost;
        private System.Windows.Forms.FlowLayoutPanel _controls;

        private System.Windows.Forms.Label _lblHeader;
        private System.Windows.Forms.Label _lblMoleculeTitle;
        private System.Windows.Forms.ComboBox _cmbMolecule;
        private System.Windows.Forms.Button _btnUseSelection;

        private System.Windows.Forms.Label _lblDensityModelTitle;
        private System.Windows.Forms.ComboBox _cmbDensityModel;

        private System.Windows.Forms.Label _lblVoxelSizeTitle;
        private System.Windows.Forms.NumericUpDown _numVoxelSize;
        private System.Windows.Forms.Label _lblSigmaMultTitle;
        private System.Windows.Forms.NumericUpDown _numSigmaMult;
        private System.Windows.Forms.Label _lblIsoLevelTitle;
        private System.Windows.Forms.NumericUpDown _numIsoLevel;
        private System.Windows.Forms.Label _lblMarginTitle;
        private System.Windows.Forms.NumericUpDown _numMargin;

        private System.Windows.Forms.Label _lblDensityModeTitle;
        private System.Windows.Forms.ComboBox _cmbDensityMode;

 // ⭐ Advanced
        private System.Windows.Forms.CheckBox _chkAdvanced;
        private System.Windows.Forms.Panel _pnlAdvanced;
        private System.Windows.Forms.Label _lblAdvStep;
        private System.Windows.Forms.NumericUpDown _numAdvStep;
        private System.Windows.Forms.Label _lblAdvExpCutoff;
        private System.Windows.Forms.NumericUpDown _numAdvExpCutoff;
        private System.Windows.Forms.Label _lblAdvMaxRMax;
        private System.Windows.Forms.NumericUpDown _numAdvMaxRMax;
        private System.Windows.Forms.Button _btnAdvRebuild;

        private System.Windows.Forms.Label _lblPropertyTitle;
        private System.Windows.Forms.ComboBox _cmbPropertyMode;

        private System.Windows.Forms.Label _lblTransparencyTitle;
        private System.Windows.Forms.Label _lblTransparencyValue;
        private System.Windows.Forms.TrackBar _trackTransparency;

        private System.Windows.Forms.Label _lblDNormScaleTitle;
        private System.Windows.Forms.Label _lblDNormScaleValue;
        private System.Windows.Forms.TrackBar _trackDNormScale;

        private System.Windows.Forms.CheckBox _chkShowMesh;
        private System.Windows.Forms.CheckBox _chkShowWireframe;
        private System.Windows.Forms.CheckBox _chkShowMolecule;
        private System.Windows.Forms.CheckBox _chkShowAxes;
        private System.Windows.Forms.CheckBox _chkShowCell;

        private System.Windows.Forms.ProgressBar _progressBar;
        private System.Windows.Forms.Label _lblStatus;

        private System.Windows.Forms.Button _btnGenerate;
        private System.Windows.Forms.Button _btnClear;
        private System.Windows.Forms.Button _btnSavePly;
        private System.Windows.Forms.Button _btnExportPng;
        private System.Windows.Forms.Button _btnFingerprint;
        private System.Windows.Forms.Button _btnShowDiagnostics;

        private System.Windows.Forms.Label _lblZoomInfo;
        private System.Windows.Forms.Label _lblHint;

        #endregion
    }
}