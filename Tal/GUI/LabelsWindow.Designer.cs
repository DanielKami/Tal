// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// LabelsWindow designer-generated layout.

﻿using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class LabelsWindow : DockContent
    {
        private System.ComponentModel.IContainer components = null;

        // DEKLARACJE KONTROLEK
        private Label _lblTitle;
        private CheckBox _chkLabels;
        private GroupBox _grpFont;
        private ComboBox _cmbFontName;
        private Label _lblFontName;
        private TrackBar _trackFontSize;
        private Label _labelFontSize;
        private Label _lblFontSize;
        private CheckBox _chkBold;
        private CheckBox _chkItalic;
        private CheckBox _chkUnderline;
        private GroupBox _grpColor;
        private Button _btnFontColor;
        private Panel _panelColorPreview;
        private GroupBox _grpShadow;
        private CheckBox _chkShadow;
        private Button _btnShadowColor;
        private Panel _panelShadowPreview;
        private GroupBox _grpOffset;
        private TrackBar _trackOffsetX;
        private Label _labelOffsetX;
        private Label _lblOffsetX;
        private TrackBar _trackOffsetY;
        private Label _labelOffsetY;
        private Label _lblOffsetY;
        private Button _btnReset;
        private Button _btnApply;
        private Panel _panelPreview;
        private Label _labelPreview;
        private Label _labelPreviewShadow;
        private Label _lblPreview;

        private void InitializeComponent()
        {
            _lblTitle = new Label();
            _chkLabels = new CheckBox();
            _grpFont = new GroupBox();
            _lblFontName = new Label();
            _cmbFontName = new ComboBox();
            _lblFontSize = new Label();
            _trackFontSize = new TrackBar();
            _labelFontSize = new Label();
            _chkBold = new CheckBox();
            _chkItalic = new CheckBox();
            _chkUnderline = new CheckBox();
            _grpColor = new GroupBox();
            _btnFontColor = new Button();
            _panelColorPreview = new Panel();
            _grpShadow = new GroupBox();
            _chkShadow = new CheckBox();
            _btnShadowColor = new Button();
            _panelShadowPreview = new Panel();
            _grpOffset = new GroupBox();
            _lblOffsetX = new Label();
            _trackOffsetX = new TrackBar();
            _labelOffsetX = new Label();
            _lblOffsetY = new Label();
            _trackOffsetY = new TrackBar();
            _labelOffsetY = new Label();
            _btnReset = new Button();
            _btnApply = new Button();
            _panelPreview = new Panel();
            _labelPreviewShadow = new Label();
            _labelPreview = new Label();
            _lblPreview = new Label();
            _grpFont.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_trackFontSize).BeginInit();
            _grpColor.SuspendLayout();
            _grpShadow.SuspendLayout();
            _grpOffset.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_trackOffsetX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackOffsetY).BeginInit();
            _panelPreview.SuspendLayout();
            SuspendLayout();
            // 
            // _lblTitle
            // 
            _lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            _lblTitle.Location = new Point(15, 12);
            _lblTitle.Name = "_lblTitle";
            _lblTitle.Size = new Size(280, 30);
            _lblTitle.TabIndex = 0;
            _lblTitle.Text = "🏷️ LABELS SETTINGS";
            _lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _chkLabels
            // 
            _chkLabels.Font = new Font("Segoe UI", 10F);
            _chkLabels.Location = new Point(15, 48);
            _chkLabels.Name = "_chkLabels";
            _chkLabels.Size = new Size(120, 28);
            _chkLabels.TabIndex = 1;
            _chkLabels.Text = "Show Labels";
            _chkLabels.UseVisualStyleBackColor = true;
            // 
            // _grpFont
            // 
            _grpFont.Controls.Add(_lblFontName);
            _grpFont.Controls.Add(_cmbFontName);
            _grpFont.Controls.Add(_lblFontSize);
            _grpFont.Controls.Add(_trackFontSize);
            _grpFont.Controls.Add(_labelFontSize);
            _grpFont.Controls.Add(_chkBold);
            _grpFont.Controls.Add(_chkItalic);
            _grpFont.Controls.Add(_chkUnderline);
            _grpFont.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpFont.Location = new Point(12, 82);
            _grpFont.Name = "_grpFont";
            _grpFont.Size = new Size(296, 120);
            _grpFont.TabIndex = 2;
            _grpFont.TabStop = false;
            _grpFont.Text = "FONT";
            // 
            // _lblFontName
            // 
            _lblFontName.Font = new Font("Segoe UI", 9F);
            _lblFontName.Location = new Point(10, 22);
            _lblFontName.Name = "_lblFontName";
            _lblFontName.Size = new Size(60, 22);
            _lblFontName.TabIndex = 0;
            _lblFontName.Text = "Font:";
            _lblFontName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _cmbFontName
            // 
            _cmbFontName.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbFontName.Font = new Font("Segoe UI", 9F);
            _cmbFontName.Location = new Point(70, 22);
            _cmbFontName.Name = "_cmbFontName";
            _cmbFontName.Size = new Size(210, 28);
            _cmbFontName.TabIndex = 1;
            // 
            // _lblFontSize
            // 
            _lblFontSize.Font = new Font("Segoe UI", 9F);
            _lblFontSize.Location = new Point(10, 55);
            _lblFontSize.Name = "_lblFontSize";
            _lblFontSize.Size = new Size(60, 22);
            _lblFontSize.TabIndex = 2;
            _lblFontSize.Text = "Size:";
            _lblFontSize.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackFontSize
            // 
            _trackFontSize.AutoSize = false;
            _trackFontSize.Location = new Point(70, 55);
            _trackFontSize.Maximum = 38;
            _trackFontSize.Name = "_trackFontSize";
            _trackFontSize.Size = new Size(180, 24);
            _trackFontSize.TabIndex = 3;
            _trackFontSize.TickFrequency = 2;
            _trackFontSize.Value = 10;
            // 
            // _labelFontSize
            // 
            _labelFontSize.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _labelFontSize.Location = new Point(255, 55);
            _labelFontSize.Name = "_labelFontSize";
            _labelFontSize.Size = new Size(40, 22);
            _labelFontSize.TabIndex = 4;
            _labelFontSize.Text = "0.60x";
            _labelFontSize.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _chkBold
            // 
            _chkBold.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _chkBold.Location = new Point(10, 85);
            _chkBold.Name = "_chkBold";
            _chkBold.Size = new Size(60, 24);
            _chkBold.TabIndex = 5;
            _chkBold.Text = "Bold";
            _chkBold.UseVisualStyleBackColor = true;
            // 
            // _chkItalic
            // 
            _chkItalic.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            _chkItalic.Location = new Point(80, 85);
            _chkItalic.Name = "_chkItalic";
            _chkItalic.Size = new Size(60, 24);
            _chkItalic.TabIndex = 6;
            _chkItalic.Text = "Italic";
            _chkItalic.UseVisualStyleBackColor = true;
            // 
            // _chkUnderline
            // 
            _chkUnderline.Font = new Font("Segoe UI", 9F, FontStyle.Underline);
            _chkUnderline.Location = new Point(150, 85);
            _chkUnderline.Name = "_chkUnderline";
            _chkUnderline.Size = new Size(80, 24);
            _chkUnderline.TabIndex = 7;
            _chkUnderline.Text = "Underline";
            _chkUnderline.UseVisualStyleBackColor = true;
            // 
            // _grpColor
            // 
            _grpColor.Controls.Add(_btnFontColor);
            _grpColor.Controls.Add(_panelColorPreview);
            _grpColor.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpColor.Location = new Point(12, 208);
            _grpColor.Name = "_grpColor";
            _grpColor.Size = new Size(296, 60);
            _grpColor.TabIndex = 3;
            _grpColor.TabStop = false;
            _grpColor.Text = "COLOR";
            // 
            // _btnFontColor
            // 
            _btnFontColor.Font = new Font("Segoe UI", 9F);
            _btnFontColor.Location = new Point(10, 22);
            _btnFontColor.Name = "_btnFontColor";
            _btnFontColor.Size = new Size(100, 30);
            _btnFontColor.TabIndex = 0;
            _btnFontColor.Text = "Choose Color";
            _btnFontColor.UseVisualStyleBackColor = true;
            // 
            // _panelColorPreview
            // 
            _panelColorPreview.BackColor = Color.Aqua;
            _panelColorPreview.BorderStyle = BorderStyle.FixedSingle;
            _panelColorPreview.Location = new Point(248, 22);
            _panelColorPreview.Name = "_panelColorPreview";
            _panelColorPreview.Size = new Size(32, 30);
            _panelColorPreview.TabIndex = 1;
            // 
            // _grpShadow
            // 
            _grpShadow.Controls.Add(_chkShadow);
            _grpShadow.Controls.Add(_btnShadowColor);
            _grpShadow.Controls.Add(_panelShadowPreview);
            _grpShadow.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpShadow.Location = new Point(12, 274);
            _grpShadow.Name = "_grpShadow";
            _grpShadow.Size = new Size(296, 86);
            _grpShadow.TabIndex = 4;
            _grpShadow.TabStop = false;
            _grpShadow.Text = "SHADOW";
            // 
            // _chkShadow
            // 
            _chkShadow.Font = new Font("Segoe UI", 9F);
            _chkShadow.Location = new Point(10, 22);
            _chkShadow.Name = "_chkShadow";
            _chkShadow.Size = new Size(120, 24);
            _chkShadow.TabIndex = 0;
            _chkShadow.Text = "Enable Shadow";
            _chkShadow.UseVisualStyleBackColor = true;
            // 
            // _btnShadowColor
            // 
            _btnShadowColor.Font = new Font("Segoe UI", 9F);
            _btnShadowColor.Location = new Point(10, 46);
            _btnShadowColor.Name = "_btnShadowColor";
            _btnShadowColor.Size = new Size(100, 34);
            _btnShadowColor.TabIndex = 1;
            _btnShadowColor.Text = "Shadow Color";
            _btnShadowColor.UseVisualStyleBackColor = true;
            // 
            // _panelShadowPreview
            // 
            _panelShadowPreview.BackColor = Color.Black;
            _panelShadowPreview.BorderStyle = BorderStyle.FixedSingle;
            _panelShadowPreview.Location = new Point(248, 46);
            _panelShadowPreview.Name = "_panelShadowPreview";
            _panelShadowPreview.Size = new Size(32, 34);
            _panelShadowPreview.TabIndex = 2;
            // 
            // _grpOffset
            // 
            _grpOffset.Controls.Add(_lblOffsetX);
            _grpOffset.Controls.Add(_trackOffsetX);
            _grpOffset.Controls.Add(_labelOffsetX);
            _grpOffset.Controls.Add(_lblOffsetY);
            _grpOffset.Controls.Add(_trackOffsetY);
            _grpOffset.Controls.Add(_labelOffsetY);
            _grpOffset.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpOffset.Location = new Point(12, 366);
            _grpOffset.Name = "_grpOffset";
            _grpOffset.Size = new Size(296, 80);
            _grpOffset.TabIndex = 5;
            _grpOffset.TabStop = false;
            _grpOffset.Text = "OFFSET";
            // 
            // _lblOffsetX
            // 
            _lblOffsetX.Font = new Font("Segoe UI", 9F);
            _lblOffsetX.Location = new Point(10, 22);
            _lblOffsetX.Name = "_lblOffsetX";
            _lblOffsetX.Size = new Size(40, 22);
            _lblOffsetX.TabIndex = 0;
            _lblOffsetX.Text = "X:";
            _lblOffsetX.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackOffsetX
            // 
            _trackOffsetX.AutoSize = false;
            _trackOffsetX.Location = new Point(50, 23);
            _trackOffsetX.Maximum = 100;
            _trackOffsetX.Minimum = -100;
            _trackOffsetX.Name = "_trackOffsetX";
            _trackOffsetX.Size = new Size(180, 24);
            _trackOffsetX.TabIndex = 1;
            _trackOffsetX.TickFrequency = 10;
            // 
            // _labelOffsetX
            // 
            _labelOffsetX.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _labelOffsetX.Location = new Point(235, 22);
            _labelOffsetX.Name = "_labelOffsetX";
            _labelOffsetX.Size = new Size(40, 22);
            _labelOffsetX.TabIndex = 2;
            _labelOffsetX.Text = "0";
            _labelOffsetX.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblOffsetY
            // 
            _lblOffsetY.Font = new Font("Segoe UI", 9F);
            _lblOffsetY.Location = new Point(10, 50);
            _lblOffsetY.Name = "_lblOffsetY";
            _lblOffsetY.Size = new Size(40, 22);
            _lblOffsetY.TabIndex = 3;
            _lblOffsetY.Text = "Y:";
            _lblOffsetY.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackOffsetY
            // 
            _trackOffsetY.AutoSize = false;
            _trackOffsetY.Location = new Point(50, 50);
            _trackOffsetY.Maximum = 100;
            _trackOffsetY.Minimum = -100;
            _trackOffsetY.Name = "_trackOffsetY";
            _trackOffsetY.Size = new Size(180, 24);
            _trackOffsetY.TabIndex = 4;
            _trackOffsetY.TickFrequency = 10;
            _trackOffsetY.Value = -20;
            // 
            // _labelOffsetY
            // 
            _labelOffsetY.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _labelOffsetY.Location = new Point(235, 50);
            _labelOffsetY.Name = "_labelOffsetY";
            _labelOffsetY.Size = new Size(40, 22);
            _labelOffsetY.TabIndex = 5;
            _labelOffsetY.Text = "-20";
            _labelOffsetY.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _btnReset
            // 
            _btnReset.BackColor = Color.FromArgb(230, 230, 235);
            _btnReset.FlatStyle = FlatStyle.Flat;
            _btnReset.Font = new Font("Segoe UI", 9F);
            _btnReset.Location = new Point(12, 454);
            _btnReset.Name = "_btnReset";
            _btnReset.Size = new Size(85, 35);
            _btnReset.TabIndex = 6;
            _btnReset.Text = "↺ Reset";
            _btnReset.UseVisualStyleBackColor = false;
            // 
            // _btnApply
            // 
            _btnApply.BackColor = Color.FromArgb(0, 122, 204);
            _btnApply.FlatStyle = FlatStyle.Flat;
            _btnApply.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnApply.ForeColor = Color.White;
            _btnApply.Location = new Point(222, 454);
            _btnApply.Name = "_btnApply";
            _btnApply.Size = new Size(85, 35);
            _btnApply.TabIndex = 7;
            _btnApply.Text = "Apply";
            _btnApply.UseVisualStyleBackColor = false;
            // 
            // _panelPreview
            // 
            _panelPreview.BackColor = Color.FromArgb(40, 40, 60);
            _panelPreview.BorderStyle = BorderStyle.FixedSingle;
            _panelPreview.Controls.Add(_labelPreviewShadow);
            _panelPreview.Controls.Add(_labelPreview);
            _panelPreview.Location = new Point(12, 507);
            _panelPreview.Name = "_panelPreview";
            _panelPreview.Size = new Size(296, 30);
            _panelPreview.TabIndex = 8;
            // 
            // _labelPreviewShadow
            // 
            _labelPreviewShadow.BackColor = Color.Transparent;
            _labelPreviewShadow.Font = new Font("Segoe UI", 16F);
            _labelPreviewShadow.ForeColor = Color.Black;
            _labelPreviewShadow.Location = new Point(118, 492);
            _labelPreviewShadow.Name = "_labelPreviewShadow";
            _labelPreviewShadow.Size = new Size(50, 40);
            _labelPreviewShadow.TabIndex = 9;
            _labelPreviewShadow.Text = "Aa";
            _labelPreviewShadow.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _labelPreview
            // 
            _labelPreview.BackColor = Color.Transparent;
            _labelPreview.Font = new Font("Segoe UI", 16F);
            _labelPreview.ForeColor = Color.Aqua;
            _labelPreview.Location = new Point(116, 490);
            _labelPreview.Name = "_labelPreview";
            _labelPreview.Size = new Size(50, 40);
            _labelPreview.TabIndex = 10;
            _labelPreview.Text = "Aa";
            _labelPreview.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _lblPreview
            // 
            _lblPreview.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            _lblPreview.ForeColor = Color.Gray;
            _lblPreview.Location = new Point(12, 542);
            _lblPreview.Name = "_lblPreview";
            _lblPreview.Size = new Size(296, 20);
            _lblPreview.TabIndex = 11;
            _lblPreview.Text = "Preview";
            _lblPreview.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // LabelsWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(320, 575);
            Controls.Add(_lblPreview);
            Controls.Add(_panelPreview);
            Controls.Add(_btnApply);
            Controls.Add(_btnReset);
            Controls.Add(_grpOffset);
            Controls.Add(_grpShadow);
            Controls.Add(_grpColor);
            Controls.Add(_grpFont);
            Controls.Add(_chkLabels);
            Controls.Add(_lblTitle);
            Font = new Font("Segoe UI", 9F);
            Name = "LabelsWindow";
            Text = "🏷️ Labels Settings";
            _grpFont.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_trackFontSize).EndInit();
            _grpColor.ResumeLayout(false);
            _grpShadow.ResumeLayout(false);
            _grpOffset.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_trackOffsetX).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackOffsetY).EndInit();
            _panelPreview.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}