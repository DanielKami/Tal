// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// SymmetryElementsWindow designer-generated layout.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal.GUI
{
    partial class SymmetryElementsWindow
    {
        private System.ComponentModel.IContainer components = null;

        // ── Master switch ──
        private System.Windows.Forms.CheckBox _chkShowSymmetryElements;

        // ── Element types section ──
        private System.Windows.Forms.GroupBox _grpElementTypes;
        private System.Windows.Forms.TableLayoutPanel _gridElements;

        // Rotation axes
        private System.Windows.Forms.CheckBox _chkShowRotation2;
        private System.Windows.Forms.Button _btnColorRotation2;
        private System.Windows.Forms.CheckBox _chkShowRotation3;
        private System.Windows.Forms.Button _btnColorRotation3;
        private System.Windows.Forms.CheckBox _chkShowRotation4;
        private System.Windows.Forms.Button _btnColorRotation4;
        private System.Windows.Forms.CheckBox _chkShowRotation6;
        private System.Windows.Forms.Button _btnColorRotation6;

        // Screw axes
        private System.Windows.Forms.CheckBox _chkShowScrew21;
        private System.Windows.Forms.Button _btnColorScrew21;
        private System.Windows.Forms.CheckBox _chkShowScrew31;
        private System.Windows.Forms.Button _btnColorScrew31;
        private System.Windows.Forms.CheckBox _chkShowScrew32;
        private System.Windows.Forms.Button _btnColorScrew32;
        private System.Windows.Forms.CheckBox _chkShowScrew41;
        private System.Windows.Forms.Button _btnColorScrew41;
        private System.Windows.Forms.CheckBox _chkShowScrew42;
        private System.Windows.Forms.Button _btnColorScrew42;
        private System.Windows.Forms.CheckBox _chkShowScrew43;
        private System.Windows.Forms.Button _btnColorScrew43;
        private System.Windows.Forms.CheckBox _chkShowScrew61;
        private System.Windows.Forms.Button _btnColorScrew61;
        private System.Windows.Forms.CheckBox _chkShowScrew62;
        private System.Windows.Forms.Button _btnColorScrew62;
        private System.Windows.Forms.CheckBox _chkShowScrew63;
        private System.Windows.Forms.Button _btnColorScrew63;
        private System.Windows.Forms.CheckBox _chkShowScrew64;
        private System.Windows.Forms.Button _btnColorScrew64;
        private System.Windows.Forms.CheckBox _chkShowScrew65;
        private System.Windows.Forms.Button _btnColorScrew65;

        // Rotoinversion axes
        private System.Windows.Forms.CheckBox _chkShowRotoinv3;
        private System.Windows.Forms.Button _btnColorRotoinv3;
        private System.Windows.Forms.CheckBox _chkShowRotoinv4;
        private System.Windows.Forms.Button _btnColorRotoinv4;
        private System.Windows.Forms.CheckBox _chkShowRotoinv6;
        private System.Windows.Forms.Button _btnColorRotoinv6;

        // Planes / centers
        private System.Windows.Forms.CheckBox _chkShowMirrorPlanes;
        private System.Windows.Forms.Button _btnColorMirror;
        private System.Windows.Forms.CheckBox _chkShowGlidePlanes;
        private System.Windows.Forms.Button _btnColorGlide;
        private System.Windows.Forms.CheckBox _chkShowInversionCenters;
        private System.Windows.Forms.Button _btnColorCenter;

        // ── Sliders section ──
        private System.Windows.Forms.GroupBox _grpSliders;
        private System.Windows.Forms.TableLayoutPanel _gridSliders;

        private System.Windows.Forms.Label _lblAxisThickness;
        private System.Windows.Forms.TrackBar _trkAxisThickness;
        private System.Windows.Forms.Label _lblAxisThicknessVal;

        private System.Windows.Forms.Label _lblMarkerSize;
        private System.Windows.Forms.TrackBar _trkMarkerSize;
        private System.Windows.Forms.Label _lblMarkerSizeVal;

        private System.Windows.Forms.Label _lblMirrorAlpha;
        private System.Windows.Forms.TrackBar _trkMirrorAlpha;
        private System.Windows.Forms.Label _lblMirrorAlphaVal;

        private System.Windows.Forms.Label _lblGlideAlpha;
        private System.Windows.Forms.TrackBar _trkGlideAlpha;
        private System.Windows.Forms.Label _lblGlideAlphaVal;

        private System.Windows.Forms.Label _lblCenterRadius;
        private System.Windows.Forms.TrackBar _trkCenterRadius;
        private System.Windows.Forms.Label _lblCenterRadiusVal;

        // ── Buttons ──
        private System.Windows.Forms.Button _btnReset;
        private System.Windows.Forms.Button _btnRefresh;

        // ── Layout ──
        private System.Windows.Forms.TableLayoutPanel _mainLayout;
        private System.Windows.Forms.TableLayoutPanel _buttonRow;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            _mainLayout = new TableLayoutPanel();
            _buttonRow = new TableLayoutPanel();

            _chkShowSymmetryElements = new CheckBox();

            _grpElementTypes = new GroupBox();
            _gridElements = new TableLayoutPanel();

            _chkShowRotation2 = new CheckBox(); _btnColorRotation2 = new Button();
            _chkShowRotation3 = new CheckBox(); _btnColorRotation3 = new Button();
            _chkShowRotation4 = new CheckBox(); _btnColorRotation4 = new Button();
            _chkShowRotation6 = new CheckBox(); _btnColorRotation6 = new Button();

            _chkShowScrew21 = new CheckBox(); _btnColorScrew21 = new Button();
            _chkShowScrew31 = new CheckBox(); _btnColorScrew31 = new Button();
            _chkShowScrew32 = new CheckBox(); _btnColorScrew32 = new Button();
            _chkShowScrew41 = new CheckBox(); _btnColorScrew41 = new Button();
            _chkShowScrew42 = new CheckBox(); _btnColorScrew42 = new Button();
            _chkShowScrew43 = new CheckBox(); _btnColorScrew43 = new Button();
            _chkShowScrew61 = new CheckBox(); _btnColorScrew61 = new Button();
            _chkShowScrew62 = new CheckBox(); _btnColorScrew62 = new Button();
            _chkShowScrew63 = new CheckBox(); _btnColorScrew63 = new Button();
            _chkShowScrew64 = new CheckBox(); _btnColorScrew64 = new Button();
            _chkShowScrew65 = new CheckBox(); _btnColorScrew65 = new Button();

            _chkShowRotoinv3 = new CheckBox(); _btnColorRotoinv3 = new Button();
            _chkShowRotoinv4 = new CheckBox(); _btnColorRotoinv4 = new Button();
            _chkShowRotoinv6 = new CheckBox(); _btnColorRotoinv6 = new Button();

            _chkShowMirrorPlanes = new CheckBox(); _btnColorMirror = new Button();
            _chkShowGlidePlanes = new CheckBox(); _btnColorGlide = new Button();
            _chkShowInversionCenters = new CheckBox(); _btnColorCenter = new Button();

            _grpSliders = new GroupBox();
            _gridSliders = new TableLayoutPanel();

            _lblAxisThickness = new Label(); _trkAxisThickness = new TrackBar(); _lblAxisThicknessVal = new Label();
            _lblMarkerSize = new Label(); _trkMarkerSize = new TrackBar(); _lblMarkerSizeVal = new Label();
            _lblMirrorAlpha = new Label(); _trkMirrorAlpha = new TrackBar(); _lblMirrorAlphaVal = new Label();
            _lblGlideAlpha = new Label(); _trkGlideAlpha = new TrackBar(); _lblGlideAlphaVal = new Label();
            _lblCenterRadius = new Label(); _trkCenterRadius = new TrackBar(); _lblCenterRadiusVal = new Label();

            _btnReset = new Button();
            _btnRefresh = new Button();

            _mainLayout.SuspendLayout();
            _buttonRow.SuspendLayout();
            _grpElementTypes.SuspendLayout();
            _gridElements.SuspendLayout();
            _grpSliders.SuspendLayout();
            _gridSliders.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_trkAxisThickness).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trkMarkerSize).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trkMirrorAlpha).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trkGlideAlpha).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trkCenterRadius).BeginInit();
            SuspendLayout();

            // ═══════════════════════════════════════════════════════
            // _mainLayout
            // ═══════════════════════════════════════════════════════
            _mainLayout.ColumnCount = 1;
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _mainLayout.Controls.Add(_chkShowSymmetryElements, 0, 0);
            _mainLayout.Controls.Add(_grpElementTypes, 0, 1);
            _mainLayout.Controls.Add(_grpSliders, 0, 2);
            _mainLayout.Controls.Add(_buttonRow, 0, 3);
            _mainLayout.Dock = DockStyle.Top;
            _mainLayout.Location = new Point(0, 0);
            _mainLayout.Name = "_mainLayout";
            _mainLayout.Padding = new Padding(10);
            _mainLayout.RowCount = 4;
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 850F));   // 23 rows × 32 + headers
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 300F));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            _mainLayout.Size = new Size(760, 1260);
            _mainLayout.TabIndex = 0;

            // _chkShowSymmetryElements
            _chkShowSymmetryElements.AutoSize = false;
            _chkShowSymmetryElements.Dock = DockStyle.Fill;
            _chkShowSymmetryElements.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            _chkShowSymmetryElements.ForeColor = Color.FromArgb(51, 51, 51);
            _chkShowSymmetryElements.Name = "_chkShowSymmetryElements";
            _chkShowSymmetryElements.TabIndex = 0;
            _chkShowSymmetryElements.Text = "  Show Symmetry Elements";
            _chkShowSymmetryElements.UseVisualStyleBackColor = true;
            _chkShowSymmetryElements.CheckedChanged += OnSettingChanged;

            // _grpElementTypes
            _grpElementTypes.Controls.Add(_gridElements);
            _grpElementTypes.Dock = DockStyle.Fill;
            _grpElementTypes.Font = new Font("Segoe UI", 9F);
            _grpElementTypes.ForeColor = Color.FromArgb(51, 51, 51);
            _grpElementTypes.Name = "_grpElementTypes";
            _grpElementTypes.Padding = new Padding(10);
            _grpElementTypes.TabIndex = 1;
            _grpElementTypes.TabStop = false;
            _grpElementTypes.Text = "Element Types && Colors";

            // ═══════════════════════════════════════════════════════
            // _gridElements — 2 kolumny × 23 wierszy
            // ═══════════════════════════════════════════════════════
            _gridElements.ColumnCount = 2;
            _gridElements.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 82F));
            _gridElements.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            _gridElements.Dock = DockStyle.Fill;
            _gridElements.Name = "_gridElements";
            _gridElements.Padding = new Padding(6);
            _gridElements.RowCount = 23;
            for (int i = 0; i < 23; i++)
                _gridElements.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

            int row = 0;

            // Rotation axes
            AddElementRow(_gridElements, _chkShowRotation2, _btnColorRotation2, "Rotation Axis 2", ref row);
            AddElementRow(_gridElements, _chkShowRotation3, _btnColorRotation3, "Rotation Axis 3", ref row);
            AddElementRow(_gridElements, _chkShowRotation4, _btnColorRotation4, "Rotation Axis 4", ref row);
            AddElementRow(_gridElements, _chkShowRotation6, _btnColorRotation6, "Rotation Axis 6", ref row);

            // Screw axes
            AddSectionHeader(_gridElements, "───  Screw Axes (2₁ … 6₅)  ───", ref row);
            AddElementRow(_gridElements, _chkShowScrew21, _btnColorScrew21, "2₁ Screw Axis", ref row);
            AddElementRow(_gridElements, _chkShowScrew31, _btnColorScrew31, "3₁ Screw Axis", ref row);
            AddElementRow(_gridElements, _chkShowScrew32, _btnColorScrew32, "3₂ Screw Axis", ref row);
            AddElementRow(_gridElements, _chkShowScrew41, _btnColorScrew41, "4₁ Screw Axis", ref row);
            AddElementRow(_gridElements, _chkShowScrew42, _btnColorScrew42, "4₂ Screw Axis", ref row);
            AddElementRow(_gridElements, _chkShowScrew43, _btnColorScrew43, "4₃ Screw Axis", ref row);
            AddElementRow(_gridElements, _chkShowScrew61, _btnColorScrew61, "6₁ Screw Axis", ref row);
            AddElementRow(_gridElements, _chkShowScrew62, _btnColorScrew62, "6₂ Screw Axis", ref row);
            AddElementRow(_gridElements, _chkShowScrew63, _btnColorScrew63, "6₃ Screw Axis", ref row);
            AddElementRow(_gridElements, _chkShowScrew64, _btnColorScrew64, "6₄ Screw Axis", ref row);
            AddElementRow(_gridElements, _chkShowScrew65, _btnColorScrew65, "6₅ Screw Axis", ref row);

            // Rotoinversion
            AddSectionHeader(_gridElements, "───  Rotoinversion Axes  ───", ref row);
            AddElementRow(_gridElements, _chkShowRotoinv3, _btnColorRotoinv3, "Rotoinversion Axis  −3", ref row);
            AddElementRow(_gridElements, _chkShowRotoinv4, _btnColorRotoinv4, "Rotoinversion Axis  −4", ref row);
            AddElementRow(_gridElements, _chkShowRotoinv6, _btnColorRotoinv6, "Rotoinversion Axis  −6", ref row);

            // ⭐ Planes & centers — NOT UCINAMY!
            AddSectionHeader(_gridElements, "───  Planes & Centers  ───", ref row);
            AddElementRow(_gridElements, _chkShowMirrorPlanes, _btnColorMirror, "Mirror Plane  (m)", ref row);
            AddElementRow(_gridElements, _chkShowGlidePlanes, _btnColorGlide, "Glide Plane  (a, b, c, n, d, g)", ref row);
            AddElementRow(_gridElements, _chkShowInversionCenters, _btnColorCenter, "Inversion Center  (−1)", ref row);

            // _grpSliders
            _grpSliders.Controls.Add(_gridSliders);
            _grpSliders.Dock = DockStyle.Fill;
            _grpSliders.Font = new Font("Segoe UI", 9F);
            _grpSliders.ForeColor = Color.FromArgb(51, 51, 51);
            _grpSliders.Name = "_grpSliders";
            _grpSliders.Padding = new Padding(10);
            _grpSliders.TabIndex = 2;
            _grpSliders.TabStop = false;
            _grpSliders.Text = "Sizes && Transparency";

            _gridSliders.ColumnCount = 3;
            _gridSliders.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            _gridSliders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _gridSliders.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            _gridSliders.Dock = DockStyle.Fill;
            _gridSliders.Name = "_gridSliders";
            _gridSliders.Padding = new Padding(6);
            _gridSliders.RowCount = 5;
            for (int i = 0; i < 5; i++)
                _gridSliders.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

            _gridSliders.Controls.Add(_lblAxisThickness, 0, 0);
            _gridSliders.Controls.Add(_trkAxisThickness, 1, 0);
            _gridSliders.Controls.Add(_lblAxisThicknessVal, 2, 0);

            _gridSliders.Controls.Add(_lblMarkerSize, 0, 1);
            _gridSliders.Controls.Add(_trkMarkerSize, 1, 1);
            _gridSliders.Controls.Add(_lblMarkerSizeVal, 2, 1);

            _gridSliders.Controls.Add(_lblMirrorAlpha, 0, 2);
            _gridSliders.Controls.Add(_trkMirrorAlpha, 1, 2);
            _gridSliders.Controls.Add(_lblMirrorAlphaVal, 2, 2);

            _gridSliders.Controls.Add(_lblGlideAlpha, 0, 3);
            _gridSliders.Controls.Add(_trkGlideAlpha, 1, 3);
            _gridSliders.Controls.Add(_lblGlideAlphaVal, 2, 3);

            _gridSliders.Controls.Add(_lblCenterRadius, 0, 4);
            _gridSliders.Controls.Add(_trkCenterRadius, 1, 4);
            _gridSliders.Controls.Add(_lblCenterRadiusVal, 2, 4);

            SetupSlider(_lblAxisThickness, "Axis Thickness  (Å):",
                        _trkAxisThickness, 1, 200, 20, _lblAxisThicknessVal);
            SetupSlider(_lblMarkerSize, "Marker Size:",
                        _trkMarkerSize, 5, 200, 22, _lblMarkerSizeVal);
            SetupSlider(_lblMirrorAlpha, "Mirror Plane α  (%):",
                        _trkMirrorAlpha, 0, 100, 18, _lblMirrorAlphaVal);
            SetupSlider(_lblGlideAlpha, "Glide Plane α  (%):",
                        _trkGlideAlpha, 0, 100, 13, _lblGlideAlphaVal);
            SetupSlider(_lblCenterRadius, "Center Radius  (Å):",
                        _trkCenterRadius, 1, 200, 7, _lblCenterRadiusVal);

            // ═══════════════════════════════════════════════════════
            // _buttonRow — 2 buttons (Refresh / Reset)
            // ═══════════════════════════════════════════════════════
            _buttonRow.ColumnCount = 2;
            _buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            _buttonRow.Dock = DockStyle.Fill;
            _buttonRow.Name = "_buttonRow";
            _buttonRow.RowCount = 1;
            _buttonRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _buttonRow.Controls.Add(_btnRefresh, 0, 0);
            _buttonRow.Controls.Add(_btnReset, 1, 0);

            _btnRefresh.BackColor = Color.FromArgb(220, 240, 250);
            _btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(0, 122, 204);
            _btnRefresh.FlatStyle = FlatStyle.Flat;
            _btnRefresh.ForeColor = Color.FromArgb(0, 90, 160);
            _btnRefresh.Dock = DockStyle.Fill;
            _btnRefresh.Margin = new Padding(3);
            _btnRefresh.Name = "_btnRefresh";
            _btnRefresh.TabIndex = 3;
            _btnRefresh.Text = "🔄 Refresh";
            _btnRefresh.UseVisualStyleBackColor = false;
            _btnRefresh.Click += BtnRefresh_Click;

            _btnReset.BackColor = Color.FromArgb(230, 230, 235);
            _btnReset.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _btnReset.FlatStyle = FlatStyle.Flat;
            _btnReset.ForeColor = Color.FromArgb(51, 51, 51);
            _btnReset.Dock = DockStyle.Fill;
            _btnReset.Margin = new Padding(3);
            _btnReset.Name = "_btnReset";
            _btnReset.TabIndex = 4;
            _btnReset.Text = "↺ Reset to Defaults";
            _btnReset.UseVisualStyleBackColor = false;
            _btnReset.Click += BtnReset_Click;

            // SymmetryElementsWindow
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            ClientSize = new Size(760, 960);
            Controls.Add(_mainLayout);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(640, 560);
            Name = "SymmetryElementsWindow";
            Text = "🔷 Symmetry Elements";

            _mainLayout.ResumeLayout(false);
            _mainLayout.PerformLayout();
            _buttonRow.ResumeLayout(false);
            _grpElementTypes.ResumeLayout(false);
            _gridElements.ResumeLayout(false);
            _gridElements.PerformLayout();
            _grpSliders.ResumeLayout(false);
            _gridSliders.ResumeLayout(false);
            _gridSliders.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_trkAxisThickness).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trkMarkerSize).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trkMirrorAlpha).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trkGlideAlpha).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trkCenterRadius).EndInit();
            ResumeLayout(false);
        }

        private void AddElementRow(TableLayoutPanel grid, CheckBox chk, Button btnColor,
                                   string text, ref int row)
        {
            SetupElementCheckBox(chk, text);
            SetupColorSwatch(btnColor);

            grid.Controls.Add(chk, 0, row);
            grid.Controls.Add(btnColor, 1, row);
            row++;
        }

        private void AddSectionHeader(TableLayoutPanel grid, string text, ref int row)
        {
            var lbl = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(110, 110, 110),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                Height = 22
            };

            grid.Controls.Add(lbl, 0, row);
            grid.SetColumnSpan(lbl, 2);
            row++;
        }

        private void SetupElementCheckBox(CheckBox chk, string text)
        {
            chk.AutoSize = false;
            chk.Dock = DockStyle.Fill;
            chk.Padding = new Padding(4, 0, 0, 0);
            chk.Text = "  " + text;
            chk.TextAlign = ContentAlignment.MiddleLeft;
            chk.CheckedChanged += OnSettingChanged;
        }

        private void SetupColorSwatch(Button btn)
        {
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(6, 4, 6, 4);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btn.FlatAppearance.BorderSize = 1;
            btn.BackColor = Color.Gray;
            btn.Text = "";
            btn.UseVisualStyleBackColor = false;
            btn.Cursor = Cursors.Hand;
            btn.Click += ColorButton_Click;
        }

        private void SetupSlider(Label lbl, string text, TrackBar trk,
                                 int min, int max, int value, Label valLbl)
        {
            lbl.AutoSize = false;
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Text = text;

            trk.Dock = DockStyle.Fill;
            trk.Margin = new Padding(4, 10, 4, 10);
            trk.Minimum = min;
            trk.Maximum = max;
            trk.Value = Math.Clamp(value, min, max);
            trk.TickFrequency = Math.Max(1, (max - min) / 10);
            trk.SmallChange = 1;
            trk.LargeChange = Math.Max(1, (max - min) / 10);
            trk.ValueChanged += OnSliderChanged;

            valLbl.AutoSize = false;
            valLbl.Dock = DockStyle.Fill;
            valLbl.TextAlign = ContentAlignment.MiddleRight;
            valLbl.Text = value.ToString();
            valLbl.ForeColor = Color.FromArgb(80, 80, 80);
        }
    }
}