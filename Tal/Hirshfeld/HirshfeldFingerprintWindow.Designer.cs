// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldFingerprintWindow.Designer.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal.Hirshfeld
{
    partial class HirshfeldFingerprintWindow
    {
 /// <summary>
 /// Required designer variable.
 /// </summary>
        private System.ComponentModel.IContainer components = null;

 /// <summary>
 /// Clean up any resources being used.
 /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

 /// <summary>
 /// Required method for Designer supafterrt - do not modify
 /// the contents of this method with the code editor.
 /// </summary>
        private void InitializeComponent()
        {
            this._panel = new System.Windows.Forms.Panel();
            this._controlsHost = new System.Windows.Forms.Panel();
            this._controls = new System.Windows.Forms.FlowLayoutPanel();

 // ── Header ──
            this._lblHeader = new System.Windows.Forms.Label();

 // ── Font size ──
            this._lblFontSizeTitle = new System.Windows.Forms.Label();
            this._numFontSize = new System.Windows.Forms.NumericUpDown();

 // ── Bin size ──
            this._lblBinSizeTitle = new System.Windows.Forms.Label();
            this._numBinSize = new System.Windows.Forms.NumericUpDown();

 // ── Range min ──
            this._lblRangeMinTitle = new System.Windows.Forms.Label();
            this._numRangeMin = new System.Windows.Forms.NumericUpDown();

 // ── Reset ──
            this._btnResetOffsets = new System.Windows.Forms.Button();

 // ── Display options ──
            this._lblDisplayTitle = new System.Windows.Forms.Label();
            this._chkShowAxes = new System.Windows.Forms.CheckBox();
            this._chkShowDiagonal = new System.Windows.Forms.CheckBox();
            this._chkShowMajorGrid = new System.Windows.Forms.CheckBox();
            this._chkShowMinorGrid = new System.Windows.Forms.CheckBox();

 // ── Contact decomafterse ──
            this._lblDecomposeTitle = new System.Windows.Forms.Label();
            this._lstContactTypes = new System.Windows.Forms.CheckedListBox();
            this._btnSelectAllTypes = new System.Windows.Forms.Button();
            this._btnClearTypes = new System.Windows.Forms.Button();
            this._lblDecomposeTableTitle = new System.Windows.Forms.Label();
            this._txtDecomposeTable = new System.Windows.Forms.TextBox();
            this._btnCopyTable = new System.Windows.Forms.Button();

 // ── Exafterrt ──
            this._btnSavePng = new System.Windows.Forms.Button();

 // ── Status ──
            this._lblStatus = new System.Windows.Forms.Label();

 // ── Hint ──
            this._lblHint = new System.Windows.Forms.Label();

            this._controlsHost.SuspendLayout();
            this._controls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numFontSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numBinSize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._numRangeMin)).BeginInit();
            this.SuspendLayout();

 // ═══════════════════════════════════════════════════════════
 // _panel (canvas 2D — heatmapa fingerprintu)
 // ═══════════════════════════════════════════════════════════
            this._panel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._panel.Location = new System.Drawing.Point(0, 0);
            this._panel.Name = "_panel";
            this._panel.Size = new System.Drawing.Size(840, 1000);
            this._panel.TabIndex = 0;
            this._panel.BackColor = System.Drawing.Color.White;

 // ═══════════════════════════════════════════════════════════
 // _controlsHost (prawy panel)
 // ═══════════════════════════════════════════════════════════
            this._controlsHost.Dock = System.Windows.Forms.DockStyle.Right;
            this._controlsHost.Location = new System.Drawing.Point(840, 0);
            this._controlsHost.Name = "_controlsHost";
            this._controlsHost.Size = new System.Drawing.Size(360, 1000);
            this._controlsHost.TabIndex = 1;
            this._controlsHost.BackColor = System.Drawing.Color.FromArgb(245, 245, 250);

 // ═══════════════════════════════════════════════════════════
 // _controls (FlowLayoutPanel — scroll)
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
            this._lblHeader.Size = new System.Drawing.Size(320, 22);
            this._lblHeader.TabIndex = 0;
            this._lblHeader.Text = "Fingerprint";
            this._lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

 // ═══════════════════════════════════════════════════════════
 // _lblFontSizeTitle
 // ═══════════════════════════════════════════════════════════
            this._lblFontSizeTitle.AutoSize = false;
            this._lblFontSizeTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblFontSizeTitle.Location = new System.Drawing.Point(13, 36);
            this._lblFontSizeTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblFontSizeTitle.Name = "_lblFontSizeTitle";
            this._lblFontSizeTitle.Size = new System.Drawing.Size(320, 15);
            this._lblFontSizeTitle.TabIndex = 1;
            this._lblFontSizeTitle.Text = "Font size (pt):";

 // ═══════════════════════════════════════════════════════════
 // _numFontSize
 // ═══════════════════════════════════════════════════════════
            this._numFontSize.DecimalPlaces = 0;
            this._numFontSize.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._numFontSize.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this._numFontSize.Location = new System.Drawing.Point(13, 54);
            this._numFontSize.Margin = new System.Windows.Forms.Padding(3, 0, 3, 4);
            this._numFontSize.Maximum = new decimal(new int[] { 36, 0, 0, 0 });
            this._numFontSize.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            this._numFontSize.Name = "_numFontSize";
            this._numFontSize.Size = new System.Drawing.Size(100, 23);
            this._numFontSize.TabIndex = 2;
            this._numFontSize.Value = new decimal(new int[] { 4, 0, 0, 0 });

 // ═══════════════════════════════════════════════════════════
 // _lblBinSizeTitle
 // ═══════════════════════════════════════════════════════════
            this._lblBinSizeTitle.AutoSize = false;
            this._lblBinSizeTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblBinSizeTitle.Location = new System.Drawing.Point(13, 85);
            this._lblBinSizeTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblBinSizeTitle.Name = "_lblBinSizeTitle";
            this._lblBinSizeTitle.Size = new System.Drawing.Size(320, 15);
            this._lblBinSizeTitle.TabIndex = 3;
            this._lblBinSizeTitle.Text = "Bin size (Å):";

 // ═══════════════════════════════════════════════════════════
 // _numBinSize — 3 miejsca after przecinku
 // ═══════════════════════════════════════════════════════════
            this._numBinSize.DecimalPlaces = 3;
            this._numBinSize.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._numBinSize.Increment = new decimal(new int[] { 1, 0, 0, 196608 });
            this._numBinSize.Location = new System.Drawing.Point(13, 103);
            this._numBinSize.Margin = new System.Windows.Forms.Padding(3, 0, 3, 4);
            this._numBinSize.Maximum = new decimal(new int[] { 50, 0, 0, 196608 });
            this._numBinSize.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            this._numBinSize.Name = "_numBinSize";
            this._numBinSize.Size = new System.Drawing.Size(100, 23);
            this._numBinSize.TabIndex = 4;
            this._numBinSize.Value = new decimal(new int[] { 5, 0, 0, 196608 });

 // ═══════════════════════════════════════════════════════════
 // _lblRangeMinTitle
 // ═══════════════════════════════════════════════════════════
            this._lblRangeMinTitle.AutoSize = false;
            this._lblRangeMinTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._lblRangeMinTitle.Location = new System.Drawing.Point(13, 134);
            this._lblRangeMinTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 0);
            this._lblRangeMinTitle.Name = "_lblRangeMinTitle";
            this._lblRangeMinTitle.Size = new System.Drawing.Size(320, 15);
            this._lblRangeMinTitle.TabIndex = 5;
            this._lblRangeMinTitle.Text = "Range start (Å):";

 // ═══════════════════════════════════════════════════════════
 // _numRangeMin
 // ═══════════════════════════════════════════════════════════
            this._numRangeMin.DecimalPlaces = 2;
            this._numRangeMin.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._numRangeMin.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            this._numRangeMin.Location = new System.Drawing.Point(13, 152);
            this._numRangeMin.Margin = new System.Windows.Forms.Padding(3, 0, 3, 4);
            this._numRangeMin.Maximum = new decimal(new int[] { 300, 0, 0, 131072 });
            this._numRangeMin.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this._numRangeMin.Name = "_numRangeMin";
            this._numRangeMin.Size = new System.Drawing.Size(100, 23);
            this._numRangeMin.TabIndex = 6;
            this._numRangeMin.Value = new decimal(new int[] { 0, 0, 0, 0 });

 // ═══════════════════════════════════════════════════════════
 // _btnResetOffsets — full width
 // ═══════════════════════════════════════════════════════════
            this._btnResetOffsets.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnResetOffsets.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnResetOffsets.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnResetOffsets.Location = new System.Drawing.Point(13, 183);
            this._btnResetOffsets.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this._btnResetOffsets.Name = "_btnResetOffsets";
            this._btnResetOffsets.Size = new System.Drawing.Size(320, 28);
            this._btnResetOffsets.TabIndex = 7;
            this._btnResetOffsets.Text = "Reset offsets + zoom";
            this._btnResetOffsets.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _lblDisplayTitle
 // ═══════════════════════════════════════════════════════════
            this._lblDisplayTitle.AutoSize = false;
            this._lblDisplayTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this._lblDisplayTitle.ForeColor = System.Drawing.Color.FromArgb(60, 60, 80);
            this._lblDisplayTitle.Location = new System.Drawing.Point(13, 221);
            this._lblDisplayTitle.Margin = new System.Windows.Forms.Padding(3, 6, 3, 2);
            this._lblDisplayTitle.Name = "_lblDisplayTitle";
            this._lblDisplayTitle.Size = new System.Drawing.Size(320, 15);
            this._lblDisplayTitle.TabIndex = 8;
            this._lblDisplayTitle.Text = "Display:";

 // ═══════════════════════════════════════════════════════════
 // _chkShowAxes
 // ═══════════════════════════════════════════════════════════
            this._chkShowAxes.AutoSize = true;
            this._chkShowAxes.Checked = true;
            this._chkShowAxes.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkShowAxes.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._chkShowAxes.Location = new System.Drawing.Point(13, 242);
            this._chkShowAxes.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._chkShowAxes.Name = "_chkShowAxes";
            this._chkShowAxes.Size = new System.Drawing.Size(130, 19);
            this._chkShowAxes.TabIndex = 9;
            this._chkShowAxes.Text = "Show axes + labels";
            this._chkShowAxes.UseVisualStyleBackColor = true;

 // ═══════════════════════════════════════════════════════════
 // _chkShowDiagonal
 // ═══════════════════════════════════════════════════════════
            this._chkShowDiagonal.AutoSize = true;
            this._chkShowDiagonal.Checked = true;
            this._chkShowDiagonal.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkShowDiagonal.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._chkShowDiagonal.Location = new System.Drawing.Point(13, 263);
            this._chkShowDiagonal.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._chkShowDiagonal.Name = "_chkShowDiagonal";
            this._chkShowDiagonal.Size = new System.Drawing.Size(110, 19);
            this._chkShowDiagonal.TabIndex = 10;
            this._chkShowDiagonal.Text = "Show diagonal";
            this._chkShowDiagonal.UseVisualStyleBackColor = true;

 // ═══════════════════════════════════════════════════════════
 // _chkShowMajorGrid
 // ═══════════════════════════════════════════════════════════
            this._chkShowMajorGrid.AutoSize = true;
            this._chkShowMajorGrid.Checked = true;
            this._chkShowMajorGrid.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkShowMajorGrid.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._chkShowMajorGrid.Location = new System.Drawing.Point(13, 284);
            this._chkShowMajorGrid.Margin = new System.Windows.Forms.Padding(3, 1, 3, 1);
            this._chkShowMajorGrid.Name = "_chkShowMajorGrid";
            this._chkShowMajorGrid.Size = new System.Drawing.Size(120, 19);
            this._chkShowMajorGrid.TabIndex = 11;
            this._chkShowMajorGrid.Text = "Show major grid";
            this._chkShowMajorGrid.UseVisualStyleBackColor = true;

 // ═══════════════════════════════════════════════════════════
 // _chkShowMinorGrid
 // ═══════════════════════════════════════════════════════════
            this._chkShowMinorGrid.AutoSize = true;
            this._chkShowMinorGrid.Checked = true;
            this._chkShowMinorGrid.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkShowMinorGrid.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._chkShowMinorGrid.Location = new System.Drawing.Point(13, 305);
            this._chkShowMinorGrid.Margin = new System.Windows.Forms.Padding(3, 1, 3, 4);
            this._chkShowMinorGrid.Name = "_chkShowMinorGrid";
            this._chkShowMinorGrid.Size = new System.Drawing.Size(120, 19);
            this._chkShowMinorGrid.TabIndex = 12;
            this._chkShowMinorGrid.Text = "Show minor grid";
            this._chkShowMinorGrid.UseVisualStyleBackColor = true;

 // ═══════════════════════════════════════════════════════════
 // _lblDecomafterseTitle
 // ═══════════════════════════════════════════════════════════
            this._lblDecomposeTitle.AutoSize = false;
            this._lblDecomposeTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this._lblDecomposeTitle.ForeColor = System.Drawing.Color.FromArgb(60, 60, 80);
            this._lblDecomposeTitle.Location = new System.Drawing.Point(13, 334);
            this._lblDecomposeTitle.Margin = new System.Windows.Forms.Padding(3, 6, 3, 2);
            this._lblDecomposeTitle.Name = "_lblDecomposeTitle";
            this._lblDecomposeTitle.Size = new System.Drawing.Size(320, 15);
            this._lblDecomposeTitle.TabIndex = 13;
            this._lblDecomposeTitle.Text = "Contact types (check = show):";

 // ═══════════════════════════════════════════════════════════
 // _lstContactTypes
 // ═══════════════════════════════════════════════════════════
            this._lstContactTypes.CheckOnClick = true;
            this._lstContactTypes.Font = new System.Drawing.Font("Consolas", 8.5F);
            this._lstContactTypes.FormattingEnabled = true;
            this._lstContactTypes.IntegralHeight = false;
            this._lstContactTypes.Location = new System.Drawing.Point(13, 355);
            this._lstContactTypes.Margin = new System.Windows.Forms.Padding(3, 2, 3, 4);
            this._lstContactTypes.Name = "_lstContactTypes";
            this._lstContactTypes.Size = new System.Drawing.Size(320, 140);
            this._lstContactTypes.TabIndex = 14;

 // ═══════════════════════════════════════════════════════════
 // _btnSelectAllTypes — ⭐ WIDE (2 buttons in a row)
 // ═══════════════════════════════════════════════════════════
            this._btnSelectAllTypes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSelectAllTypes.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnSelectAllTypes.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnSelectAllTypes.Location = new System.Drawing.Point(13, 503);
            this._btnSelectAllTypes.Margin = new System.Windows.Forms.Padding(3, 0, 3, 4);
            this._btnSelectAllTypes.Name = "_btnSelectAllTypes";
            this._btnSelectAllTypes.Size = new System.Drawing.Size(156, 26);
            this._btnSelectAllTypes.TabIndex = 15;
            this._btnSelectAllTypes.Text = "Select all";
            this._btnSelectAllTypes.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _btnClearTypes — ⭐ SZEROKI
 // ═══════════════════════════════════════════════════════════
            this._btnClearTypes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClearTypes.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnClearTypes.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnClearTypes.Location = new System.Drawing.Point(177, 503);
            this._btnClearTypes.Margin = new System.Windows.Forms.Padding(3, 0, 3, 4);
            this._btnClearTypes.Name = "_btnClearTypes";
            this._btnClearTypes.Size = new System.Drawing.Size(156, 26);
            this._btnClearTypes.TabIndex = 16;
            this._btnClearTypes.Text = "Clear selection";
            this._btnClearTypes.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _lblDecomafterseTableTitle
 // ═══════════════════════════════════════════════════════════
            this._lblDecomposeTableTitle.AutoSize = false;
            this._lblDecomposeTableTitle.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this._lblDecomposeTableTitle.ForeColor = System.Drawing.Color.FromArgb(60, 60, 80);
            this._lblDecomposeTableTitle.Location = new System.Drawing.Point(13, 539);
            this._lblDecomposeTableTitle.Margin = new System.Windows.Forms.Padding(3, 6, 3, 2);
            this._lblDecomposeTableTitle.Name = "_lblDecomposeTableTitle";
            this._lblDecomposeTableTitle.Size = new System.Drawing.Size(320, 15);
            this._lblDecomposeTableTitle.TabIndex = 17;
            this._lblDecomposeTableTitle.Text = "Decompose table:";

 // ═══════════════════════════════════════════════════════════
 // _txtDecomafterseTable
 // ═══════════════════════════════════════════════════════════
            this._txtDecomposeTable.Font = new System.Drawing.Font("Consolas", 8F);
            this._txtDecomposeTable.Location = new System.Drawing.Point(13, 560);
            this._txtDecomposeTable.Margin = new System.Windows.Forms.Padding(3, 2, 3, 4);
            this._txtDecomposeTable.Multiline = true;
            this._txtDecomposeTable.Name = "_txtDecomposeTable";
            this._txtDecomposeTable.ReadOnly = true;
            this._txtDecomposeTable.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._txtDecomposeTable.Size = new System.Drawing.Size(320, 160);
            this._txtDecomposeTable.TabIndex = 18;
            this._txtDecomposeTable.WordWrap = false;

 // ═══════════════════════════════════════════════════════════
 // _btnCopyTable — full width
 // ═══════════════════════════════════════════════════════════
            this._btnCopyTable.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnCopyTable.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnCopyTable.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnCopyTable.Location = new System.Drawing.Point(13, 728);
            this._btnCopyTable.Margin = new System.Windows.Forms.Padding(3, 4, 3, 6);
            this._btnCopyTable.Name = "_btnCopyTable";
            this._btnCopyTable.Size = new System.Drawing.Size(320, 28);
            this._btnCopyTable.TabIndex = 19;
            this._btnCopyTable.Text = "Copy table";
            this._btnCopyTable.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _btnSavePng — full width
 // ═══════════════════════════════════════════════════════════
            this._btnSavePng.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnSavePng.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnSavePng.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnSavePng.Location = new System.Drawing.Point(13, 762);
            this._btnSavePng.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this._btnSavePng.Name = "_btnSavePng";
            this._btnSavePng.Size = new System.Drawing.Size(320, 28);
            this._btnSavePng.TabIndex = 20;
            this._btnSavePng.Text = "Save PNG...";
            this._btnSavePng.UseVisualStyleBackColor = false;

 // ═══════════════════════════════════════════════════════════
 // _lblStatus
 // ═══════════════════════════════════════════════════════════
            this._lblStatus.AutoSize = false;
            this._lblStatus.Font = new System.Drawing.Font("Consolas", 8F);
            this._lblStatus.ForeColor = System.Drawing.Color.FromArgb(0, 90, 160);
            this._lblStatus.Location = new System.Drawing.Point(13, 796);
            this._lblStatus.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(320, 110);
            this._lblStatus.TabIndex = 21;
            this._lblStatus.Text = "Ready";

 // ═══════════════════════════════════════════════════════════
 // _lblHint
 // ═══════════════════════════════════════════════════════════
            this._lblHint.AutoSize = false;
            this._lblHint.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this._lblHint.ForeColor = System.Drawing.Color.FromArgb(100, 100, 120);
            this._lblHint.Location = new System.Drawing.Point(13, 914);
            this._lblHint.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this._lblHint.Name = "_lblHint";
            this._lblHint.Size = new System.Drawing.Size(320, 200);
            this._lblHint.TabIndex = 22;
            this._lblHint.Text = "═══ Interaction ═══\r\n" +
                                 "LMB click on map: select bin\r\n" +
                                 "LMB drag on map: zoom to rect\r\n" +
                                 "LMB drag on label: move it\r\n" +
                                 "RMB click on map: marker\r\n" +
                                 "Mouse wheel: zoom in/out\r\n" +
                                 "\r\n" +
                                 "═══ Contact types ═══\r\n" +
                                 "Uncheck a type to gray-out\r\n" +
                                 "its region on the 3D mesh.\r\n" +
                                 "All checked = no filter.";

 // ═══════════════════════════════════════════════════════════
 // _controls — add all
 // ═══════════════════════════════════════════════════════════
            this._controls.Controls.Add(this._lblHeader);
            this._controls.Controls.Add(this._lblFontSizeTitle);
            this._controls.Controls.Add(this._numFontSize);
            this._controls.Controls.Add(this._lblBinSizeTitle);
            this._controls.Controls.Add(this._numBinSize);
            this._controls.Controls.Add(this._lblRangeMinTitle);
            this._controls.Controls.Add(this._numRangeMin);
            this._controls.Controls.Add(this._btnResetOffsets);
            this._controls.Controls.Add(this._lblDisplayTitle);
            this._controls.Controls.Add(this._chkShowAxes);
            this._controls.Controls.Add(this._chkShowDiagonal);
            this._controls.Controls.Add(this._chkShowMajorGrid);
            this._controls.Controls.Add(this._chkShowMinorGrid);
            this._controls.Controls.Add(this._lblDecomposeTitle);
            this._controls.Controls.Add(this._lstContactTypes);
            this._controls.Controls.Add(this._btnSelectAllTypes);
            this._controls.Controls.Add(this._btnClearTypes);
            this._controls.Controls.Add(this._lblDecomposeTableTitle);
            this._controls.Controls.Add(this._txtDecomposeTable);
            this._controls.Controls.Add(this._btnCopyTable);
            this._controls.Controls.Add(this._btnSavePng);
            this._controls.Controls.Add(this._lblStatus);
            this._controls.Controls.Add(this._lblHint);

 // ═══════════════════════════════════════════════════════════
 // _controlsHost
 // ═══════════════════════════════════════════════════════════
            this._controlsHost.Controls.Add(this._controls);

 // ═══════════════════════════════════════════════════════════
 // HirshfeldFingerprintWindow
 // ═══════════════════════════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 1000);
            this.Controls.Add(this._panel);
            this.Controls.Add(this._controlsHost);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(500, 500);
            this.Name = "HirshfeldFingerprintWindow";
            this.Text = "Hirshfeld Fingerprint";
            this.DockAreas = WeifenLuo.WinFormsUI.Docking.DockAreas.Document
                           | WeifenLuo.WinFormsUI.Docking.DockAreas.Float;
            this.ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.Document;

            this._controlsHost.ResumeLayout(false);
            this._controls.ResumeLayout(false);
            this._controls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._numFontSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numBinSize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._numRangeMin)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        #region Designer fields

        private System.Windows.Forms.Panel _panel;
        private System.Windows.Forms.Panel _controlsHost;
        private System.Windows.Forms.FlowLayoutPanel _controls;

        private System.Windows.Forms.Label _lblHeader;

        private System.Windows.Forms.Label _lblFontSizeTitle;
        private System.Windows.Forms.NumericUpDown _numFontSize;

        private System.Windows.Forms.Label _lblBinSizeTitle;
        private System.Windows.Forms.NumericUpDown _numBinSize;

        private System.Windows.Forms.Label _lblRangeMinTitle;
        private System.Windows.Forms.NumericUpDown _numRangeMin;

        private System.Windows.Forms.Button _btnResetOffsets;

        private System.Windows.Forms.Label _lblDisplayTitle;
        private System.Windows.Forms.CheckBox _chkShowAxes;
        private System.Windows.Forms.CheckBox _chkShowDiagonal;
        private System.Windows.Forms.CheckBox _chkShowMajorGrid;
        private System.Windows.Forms.CheckBox _chkShowMinorGrid;

        private System.Windows.Forms.Label _lblDecomposeTitle;
        private System.Windows.Forms.CheckedListBox _lstContactTypes;
        private System.Windows.Forms.Button _btnSelectAllTypes;
        private System.Windows.Forms.Button _btnClearTypes;
        private System.Windows.Forms.Label _lblDecomposeTableTitle;
        private System.Windows.Forms.TextBox _txtDecomposeTable;
        private System.Windows.Forms.Button _btnCopyTable;

        private System.Windows.Forms.Button _btnSavePng;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.Label _lblHint;

        #endregion
    }
}