// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// DepthCueWindow designer-generated layout.

﻿namespace Tal.Forms
{
    partial class DepthCueWindow
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.CheckBox _chkEnabled;
        private System.Windows.Forms.Label _lblNear;
        private System.Windows.Forms.TrackBar _trkNear;
        private System.Windows.Forms.Label _lblNearVal;
        private System.Windows.Forms.Label _lblFar;
        private System.Windows.Forms.TrackBar _trkFar;
        private System.Windows.Forms.Label _lblFarVal;
        private System.Windows.Forms.Label _lblStrength;
        private System.Windows.Forms.TrackBar _trkStrength;
        private System.Windows.Forms.Label _lblStrengthVal;
        private System.Windows.Forms.Button _btnReset;

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
            _chkEnabled = new CheckBox();
            _lblNear = new Label();
            _trkNear = new TrackBar();
            _lblNearVal = new Label();
            _lblFar = new Label();
            _trkFar = new TrackBar();
            _lblFarVal = new Label();
            _lblStrength = new Label();
            _trkStrength = new TrackBar();
            _lblStrengthVal = new Label();
            _btnReset = new Button();

            ((System.ComponentModel.ISupportInitialize)_trkNear).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trkFar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trkStrength).BeginInit();
            SuspendLayout();

            // _chkEnabled
            _chkEnabled.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _chkEnabled.ForeColor = Color.FromArgb(51, 51, 51);
            _chkEnabled.Location = new Point(12, 12);
            _chkEnabled.Name = "_chkEnabled";
            _chkEnabled.Size = new Size(280, 28);
            _chkEnabled.TabIndex = 0;
            _chkEnabled.Text = "Enable Depth Cue";

            // _lblNear
            _lblNear.Font = new Font("Segoe UI", 9F);
            _lblNear.ForeColor = Color.FromArgb(51, 51, 51);
            _lblNear.Location = new Point(12, 55);
            _lblNear.Name = "_lblNear";
            _lblNear.Size = new Size(180, 20);
            _lblNear.Text = "Start (Near)";

            // _trkNear
            _trkNear.Location = new Point(12, 78);
            _trkNear.Name = "_trkNear";
            _trkNear.Size = new Size(280, 45);
            _trkNear.Minimum = 0;
            _trkNear.Maximum = 200;      // 0 .. 20.0 Å
            _trkNear.TickFrequency = 20;
            _trkNear.SmallChange = 1;
            _trkNear.LargeChange = 10;

            // _lblNearVal
            _lblNearVal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblNearVal.ForeColor = Color.FromArgb(51, 51, 51);
            _lblNearVal.Location = new Point(300, 55);
            _lblNearVal.Name = "_lblNearVal";
            _lblNearVal.Size = new Size(80, 20);
            _lblNearVal.TextAlign = ContentAlignment.MiddleRight;
            _lblNearVal.Text = "5.0 Å";

            // _lblFar
            _lblFar.Font = new Font("Segoe UI", 9F);
            _lblFar.ForeColor = Color.FromArgb(51, 51, 51);
            _lblFar.Location = new Point(12, 130);
            _lblFar.Name = "_lblFar";
            _lblFar.Size = new Size(180, 20);
            _lblFar.Text = "End (Far)";

            // _trkFar
            _trkFar.Location = new Point(12, 153);
            _trkFar.Name = "_trkFar";
            _trkFar.Size = new Size(280, 45);
            _trkFar.Minimum = 0;
            _trkFar.Maximum = 500;       // 0 .. 50.0 Å
            _trkFar.TickFrequency = 50;
            _trkFar.SmallChange = 1;
            _trkFar.LargeChange = 20;

            // _lblFarVal
            _lblFarVal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblFarVal.ForeColor = Color.FromArgb(51, 51, 51);
            _lblFarVal.Location = new Point(300, 130);
            _lblFarVal.Name = "_lblFarVal";
            _lblFarVal.Size = new Size(80, 20);
            _lblFarVal.TextAlign = ContentAlignment.MiddleRight;
            _lblFarVal.Text = "30.0 Å";

            // _lblStrength
            _lblStrength.Font = new Font("Segoe UI", 9F);
            _lblStrength.ForeColor = Color.FromArgb(51, 51, 51);
            _lblStrength.Location = new Point(12, 205);
            _lblStrength.Name = "_lblStrength";
            _lblStrength.Size = new Size(180, 20);
            _lblStrength.Text = "Strength";

            // _trkStrength
            _trkStrength.Location = new Point(12, 228);
            _trkStrength.Name = "_trkStrength";
            _trkStrength.Size = new Size(280, 45);
            _trkStrength.Minimum = 0;
            _trkStrength.Maximum = 100;   // 0 .. 100 %
            _trkStrength.TickFrequency = 10;
            _trkStrength.SmallChange = 1;
            _trkStrength.LargeChange = 10;

            // _lblStrengthVal
            _lblStrengthVal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblStrengthVal.ForeColor = Color.FromArgb(51, 51, 51);
            _lblStrengthVal.Location = new Point(300, 205);
            _lblStrengthVal.Name = "_lblStrengthVal";
            _lblStrengthVal.Size = new Size(80, 20);
            _lblStrengthVal.TextAlign = ContentAlignment.MiddleRight;
            _lblStrengthVal.Text = "60%";

            // _btnReset
            _btnReset.BackColor = Color.FromArgb(230, 230, 235);
            _btnReset.FlatStyle = FlatStyle.Flat;
            _btnReset.Font = new Font("Segoe UI", 9F);
            _btnReset.ForeColor = Color.FromArgb(51, 51, 51);
            _btnReset.Location = new Point(12, 285);
            _btnReset.Name = "_btnReset";
            _btnReset.Size = new Size(160, 30);
            _btnReset.TabIndex = 4;
            _btnReset.Text = "Reset to defaults";
            _btnReset.UseVisualStyleBackColor = false;

            // DepthCueWindow
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(248, 248, 250);
            ClientSize = new Size(390, 330);
            Controls.Add(_chkEnabled);
            Controls.Add(_lblNear);
            Controls.Add(_trkNear);
            Controls.Add(_lblNearVal);
            Controls.Add(_lblFar);
            Controls.Add(_trkFar);
            Controls.Add(_lblFarVal);
            Controls.Add(_lblStrength);
            Controls.Add(_trkStrength);
            Controls.Add(_lblStrengthVal);
            Controls.Add(_btnReset);
            Name = "DepthCueWindow";
            Text = "Depth Cue";

            ((System.ComponentModel.ISupportInitialize)_trkNear).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trkFar).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trkStrength).EndInit();
            ResumeLayout(false);
        }
    }
}