// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// SceneQualityWindow designer-generated layout.

﻿namespace Tal.Forms
{
    partial class SceneQualityWindow
    {
        private System.ComponentModel.IContainer components = null;

        // SSAA (radio buttons 1× / 2× / 4×)
        private System.Windows.Forms.GroupBox _grpSSAA;
        private System.Windows.Forms.RadioButton _rbSSAA1;
        private System.Windows.Forms.RadioButton _rbSSAA2;
        private System.Windows.Forms.RadioButton _rbSSAA4;

        // MSAA scene 3D
        private System.Windows.Forms.GroupBox _grpMSAA;
        private System.Windows.Forms.RadioButton _rbMSAAOff;
        private System.Windows.Forms.RadioButton _rbMSAA2;
        private System.Windows.Forms.RadioButton _rbMSAA4;
        private System.Windows.Forms.RadioButton _rbMSAA8;

        // Anizo
        private System.Windows.Forms.Label _lblAniso;
        private System.Windows.Forms.ComboBox _cmbAniso;

        // Dolny panel
        private System.Windows.Forms.Button _btnReset;
        private System.Windows.Forms.Label _lblInfo;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            _grpSSAA = new GroupBox();
            _rbSSAA1 = new RadioButton();
            _rbSSAA2 = new RadioButton();
            _rbSSAA4 = new RadioButton();

            _grpMSAA = new GroupBox();
            _rbMSAAOff = new RadioButton();
            _rbMSAA2 = new RadioButton();
            _rbMSAA4 = new RadioButton();
            _rbMSAA8 = new RadioButton();

            _lblAniso = new Label();
            _cmbAniso = new ComboBox();

            _btnReset = new Button();
            _lblInfo = new Label();

            _grpSSAA.SuspendLayout();
            _grpMSAA.SuspendLayout();
            SuspendLayout();

            // ─────────────────────────────────────────────
            // _grpSSAA
            _grpSSAA.Text = "SSAA (Supersampling sceny 3D)";
            _grpSSAA.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpSSAA.ForeColor = Color.FromArgb(51, 51, 51);
            _grpSSAA.Location = new Point(12, 12);
            _grpSSAA.Size = new Size(380, 60);

            // _rbSSAA1
            _rbSSAA1.Text = "Off (1×)";
            _rbSSAA1.Font = new Font("Segoe UI", 9F);
            _rbSSAA1.ForeColor = Color.FromArgb(51, 51, 51);
            _rbSSAA1.Location = new Point(10, 25);
            _rbSSAA1.Size = new Size(90, 24);

            // _rbSSAA2
            _rbSSAA2.Text = "2×";
            _rbSSAA2.Font = new Font("Segoe UI", 9F);
            _rbSSAA2.ForeColor = Color.FromArgb(51, 51, 51);
            _rbSSAA2.Location = new Point(110, 25);
            _rbSSAA2.Size = new Size(60, 24);

            // _rbSSAA4
            _rbSSAA4.Text = "4×";
            _rbSSAA4.Font = new Font("Segoe UI", 9F);
            _rbSSAA4.ForeColor = Color.FromArgb(51, 51, 51);
            _rbSSAA4.Location = new Point(180, 25);
            _rbSSAA4.Size = new Size(60, 24);

            // ─────────────────────────────────────────────
            // _grpMSAA
            _grpMSAA.Text = "MSAA sceny 3D (RT)";
            _grpMSAA.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpMSAA.ForeColor = Color.FromArgb(51, 51, 51);
            _grpMSAA.Location = new Point(12, 82);
            _grpMSAA.Size = new Size(380, 60);

            // _rbMSAAOff
            _rbMSAAOff.Text = "Off";
            _rbMSAAOff.Font = new Font("Segoe UI", 9F);
            _rbMSAAOff.ForeColor = Color.FromArgb(51, 51, 51);
            _rbMSAAOff.Location = new Point(10, 25);
            _rbMSAAOff.Size = new Size(70, 24);

            // _rbMSAA2
            _rbMSAA2.Text = "2×";
            _rbMSAA2.Font = new Font("Segoe UI", 9F);
            _rbMSAA2.ForeColor = Color.FromArgb(51, 51, 51);
            _rbMSAA2.Location = new Point(90, 25);
            _rbMSAA2.Size = new Size(60, 24);

            // _rbMSAA4
            _rbMSAA4.Text = "4×";
            _rbMSAA4.Font = new Font("Segoe UI", 9F);
            _rbMSAA4.ForeColor = Color.FromArgb(51, 51, 51);
            _rbMSAA4.Location = new Point(160, 25);
            _rbMSAA4.Size = new Size(60, 24);

            // _rbMSAA8
            _rbMSAA8.Text = "8×";
            _rbMSAA8.Font = new Font("Segoe UI", 9F);
            _rbMSAA8.ForeColor = Color.FromArgb(51, 51, 51);
            _rbMSAA8.Location = new Point(230, 25);
            _rbMSAA8.Size = new Size(60, 24);

            // ─────────────────────────────────────────────
            // _lblAniso
            _lblAniso.Text = "Anizotropia tekstur";
            _lblAniso.Font = new Font("Segoe UI", 9F);
            _lblAniso.ForeColor = Color.FromArgb(51, 51, 51);
            _lblAniso.Location = new Point(12, 162);
            _lblAniso.Size = new Size(160, 20);

            // _cmbAniso
            _cmbAniso.DropDownStyle = ComboBoxStyle.DropDownList;
            _cmbAniso.Font = new Font("Segoe UI", 9F);
            _cmbAniso.Location = new Point(180, 159);
            _cmbAniso.Size = new Size(100, 27);
            _cmbAniso.Items.AddRange(new object[]
            {
                "1×", "2×", "4×", "8×", "16×"
            });

            // ─────────────────────────────────────────────
            // _btnReset
            _btnReset.Text = "Reset to defaults";
            _btnReset.Font = new Font("Segoe UI", 9F);
            _btnReset.BackColor = Color.FromArgb(230, 230, 235);
            _btnReset.FlatStyle = FlatStyle.Flat;
            _btnReset.ForeColor = Color.FromArgb(51, 51, 51);
            _btnReset.Location = new Point(12, 200);
            _btnReset.Size = new Size(180, 30);

            // ─────────────────────────────────────────────
            // _lblInfo
            _lblInfo.Text = "Window: —";
            _lblInfo.Font = new Font("Consolas", 8.5F);
            _lblInfo.ForeColor = Color.FromArgb(90, 90, 90);
            _lblInfo.BackColor = Color.FromArgb(240, 240, 245);
            _lblInfo.BorderStyle = BorderStyle.FixedSingle;
            _lblInfo.Location = new Point(12, 240);
            _lblInfo.Size = new Size(380, 140);
            _lblInfo.Padding = new Padding(6);

            // ─────────────────────────────────────────────
            // SceneQualityWindow
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(248, 248, 250);
            ClientSize = new Size(404, 395);
            Controls.Add(_grpSSAA);
            Controls.Add(_grpMSAA);
            Controls.Add(_lblAniso);
            Controls.Add(_cmbAniso);
            Controls.Add(_btnReset);
            Controls.Add(_lblInfo);

            _grpSSAA.Controls.Add(_rbSSAA1);
            _grpSSAA.Controls.Add(_rbSSAA2);
            _grpSSAA.Controls.Add(_rbSSAA4);

            _grpMSAA.Controls.Add(_rbMSAAOff);
            _grpMSAA.Controls.Add(_rbMSAA2);
            _grpMSAA.Controls.Add(_rbMSAA4);
            _grpMSAA.Controls.Add(_rbMSAA8);

            Name = "SceneQualityWindow";
            Text = "Scene Quality";

            _grpSSAA.ResumeLayout(false);
            _grpMSAA.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}