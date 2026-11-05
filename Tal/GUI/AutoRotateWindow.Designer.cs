// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// AutoRotateWindow designer-generated layout.

﻿using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class AutoRotateWindow : DockContent
    {
        // DEKLARACJE KONTROLEK
        private Label _lblTitle;
        private GroupBox _grpMode;
        private RadioButton _radioRotate;
        private RadioButton _radioWobble;
        private GroupBox _grpAxis;
        private RadioButton _radioX;
        private RadioButton _radioY;
        private RadioButton _radioZ;
        private GroupBox _grpSpeed;
        private TrackBar _trackSpeed;
        private Label _labelSpeedValue;
        private Label _labelSpeedMin;
        private Label _labelSpeedMax;
        private GroupBox _grpAmplitude;
        private TrackBar _trackAmplitude;
        private Label _labelAmplitudeValue;
        private Label _lblAmplitudeDeg;
        private Label _lblAmplitude;
        private Button _btnReset;
        private Label _labelRotateInfo;
        private Label _labelWobbleInfo;

        private void InitializeComponent()
        {
            _lblTitle = new Label();
            _grpMode = new GroupBox();
            _radioRotate = new RadioButton();
            _radioWobble = new RadioButton();
            _grpAxis = new GroupBox();
            _radioX = new RadioButton();
            _radioY = new RadioButton();
            _radioZ = new RadioButton();
            _grpSpeed = new GroupBox();
            _trackSpeed = new TrackBar();
            _labelSpeedValue = new Label();
            _labelSpeedMin = new Label();
            _labelSpeedMax = new Label();
            _grpAmplitude = new GroupBox();
            _trackAmplitude = new TrackBar();
            _labelAmplitudeValue = new Label();
            _lblAmplitudeDeg = new Label();
            _lblAmplitude = new Label();
            _btnReset = new Button();
            _labelRotateInfo = new Label();
            _labelWobbleInfo = new Label();
            _grpMode.SuspendLayout();
            _grpAxis.SuspendLayout();
            _grpSpeed.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_trackSpeed).BeginInit();
            _grpAmplitude.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_trackAmplitude).BeginInit();
            SuspendLayout();
            // 
            // _lblTitle
            // 
            _lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            _lblTitle.Location = new Point(15, 12);
            _lblTitle.Name = "_lblTitle";
            _lblTitle.Size = new Size(250, 30);
            _lblTitle.TabIndex = 0;
            _lblTitle.Text = "🔄 AUTO ROTATE";
            _lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _grpMode
            // 
            _grpMode.Controls.Add(_radioRotate);
            _grpMode.Controls.Add(_radioWobble);
            _grpMode.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpMode.Location = new Point(12, 48);
            _grpMode.Name = "_grpMode";
            _grpMode.Size = new Size(256, 72);
            _grpMode.TabIndex = 1;
            _grpMode.TabStop = false;
            _grpMode.Text = "MODE";
            // 
            // _radioRotate
            // 
            _radioRotate.Font = new Font("Segoe UI", 9F);
            _radioRotate.Location = new Point(15, 22);
            _radioRotate.Name = "_radioRotate";
            _radioRotate.Size = new Size(100, 24);
            _radioRotate.TabIndex = 0;
            _radioRotate.TabStop = true;
            _radioRotate.Text = "⭕ Rotate";
            _radioRotate.UseVisualStyleBackColor = true;
            // 
            // _radioWobble
            // 
            _radioWobble.Font = new Font("Segoe UI", 9F);
            _radioWobble.Location = new Point(15, 46);
            _radioWobble.Name = "_radioWobble";
            _radioWobble.Size = new Size(120, 24);
            _radioWobble.TabIndex = 1;
            _radioWobble.TabStop = true;
            _radioWobble.Text = "〰️ Wobble";
            _radioWobble.UseVisualStyleBackColor = true;
            // 
            // _grpAxis
            // 
            _grpAxis.Controls.Add(_radioX);
            _grpAxis.Controls.Add(_radioY);
            _grpAxis.Controls.Add(_radioZ);
            _grpAxis.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpAxis.Location = new Point(12, 126);
            _grpAxis.Name = "_grpAxis";
            _grpAxis.Size = new Size(256, 58);
            _grpAxis.TabIndex = 2;
            _grpAxis.TabStop = false;
            _grpAxis.Text = "AXIS";
            // 
            // _radioX
            // 
            _radioX.Font = new Font("Segoe UI", 9F);
            _radioX.Location = new Point(15, 22);
            _radioX.Name = "_radioX";
            _radioX.Size = new Size(50, 24);
            _radioX.TabIndex = 0;
            _radioX.TabStop = true;
            _radioX.Text = "X";
            _radioX.UseVisualStyleBackColor = true;
            // 
            // _radioY
            // 
            _radioY.Font = new Font("Segoe UI", 9F);
            _radioY.Location = new Point(95, 22);
            _radioY.Name = "_radioY";
            _radioY.Size = new Size(50, 24);
            _radioY.TabIndex = 1;
            _radioY.TabStop = true;
            _radioY.Text = "Y";
            _radioY.UseVisualStyleBackColor = true;
            // 
            // _radioZ
            // 
            _radioZ.Font = new Font("Segoe UI", 9F);
            _radioZ.Location = new Point(175, 22);
            _radioZ.Name = "_radioZ";
            _radioZ.Size = new Size(50, 24);
            _radioZ.TabIndex = 2;
            _radioZ.TabStop = true;
            _radioZ.Text = "Z";
            _radioZ.UseVisualStyleBackColor = true;
            // 
            // _grpSpeed
            // 
            _grpSpeed.Controls.Add(_trackSpeed);
            _grpSpeed.Controls.Add(_labelSpeedValue);
            _grpSpeed.Controls.Add(_labelSpeedMin);
            _grpSpeed.Controls.Add(_labelSpeedMax);
            _grpSpeed.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpSpeed.Location = new Point(12, 190);
            _grpSpeed.Name = "_grpSpeed";
            _grpSpeed.Size = new Size(256, 91);
            _grpSpeed.TabIndex = 3;
            _grpSpeed.TabStop = false;
            _grpSpeed.Text = "SPEED";
            // 
            // _trackSpeed
            // 
            _trackSpeed.AutoSize = false;
            _trackSpeed.Location = new Point(8, 47);
            _trackSpeed.Minimum = -10;
            _trackSpeed.Name = "_trackSpeed";
            _trackSpeed.Size = new Size(240, 28);
            _trackSpeed.TabIndex = 0;
            _trackSpeed.TickFrequency = 2;
            // 
            // _labelSpeedValue
            // 
            _labelSpeedValue.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            _labelSpeedValue.Location = new Point(105, 22);
            _labelSpeedValue.Name = "_labelSpeedValue";
            _labelSpeedValue.Size = new Size(40, 18);
            _labelSpeedValue.TabIndex = 3;
            _labelSpeedValue.Text = "0.0";
            _labelSpeedValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // _labelSpeedMin
            // 
            _labelSpeedMin.Font = new Font("Segoe UI", 8F);
            _labelSpeedMin.Location = new Point(8, 23);
            _labelSpeedMin.Name = "_labelSpeedMin";
            _labelSpeedMin.Size = new Size(25, 18);
            _labelSpeedMin.TabIndex = 1;
            _labelSpeedMin.Text = "-10";
            // 
            // _labelSpeedMax
            // 
            _labelSpeedMax.Font = new Font("Segoe UI", 8F);
            _labelSpeedMax.Location = new Point(236, 22);
            _labelSpeedMax.Name = "_labelSpeedMax";
            _labelSpeedMax.Size = new Size(20, 18);
            _labelSpeedMax.TabIndex = 2;
            _labelSpeedMax.Text = "10";
            // 
            // _grpAmplitude
            // 
            _grpAmplitude.Controls.Add(_labelAmplitudeValue);
            _grpAmplitude.Controls.Add(_lblAmplitudeDeg);
            _grpAmplitude.Controls.Add(_lblAmplitude);
            _grpAmplitude.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpAmplitude.Location = new Point(12, 287);
            _grpAmplitude.Name = "_grpAmplitude";
            _grpAmplitude.Size = new Size(256, 88);
            _grpAmplitude.TabIndex = 4;
            _grpAmplitude.TabStop = false;
            _grpAmplitude.Text = "AMPLITUDE (Wobble)";
            // 
            // _trackAmplitude
            // 
            _trackAmplitude.AutoSize = false;
            _trackAmplitude.Location = new Point(20, 332);
            _trackAmplitude.Maximum = 90;
            _trackAmplitude.Name = "_trackAmplitude";
            _trackAmplitude.Size = new Size(240, 28);
            _trackAmplitude.TabIndex = 0;
            _trackAmplitude.TickFrequency = 10;
            // 
            // _labelAmplitudeValue
            // 
            _labelAmplitudeValue.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            _labelAmplitudeValue.Location = new Point(105, 23);
            _labelAmplitudeValue.Name = "_labelAmplitudeValue";
            _labelAmplitudeValue.Size = new Size(40, 18);
            _labelAmplitudeValue.TabIndex = 3;
            _labelAmplitudeValue.Text = "0°";
            _labelAmplitudeValue.TextAlign = ContentAlignment.MiddleRight;
            // 
            // _lblAmplitudeDeg
            // 
            _lblAmplitudeDeg.Font = new Font("Segoe UI", 8F);
            _lblAmplitudeDeg.Location = new Point(232, 24);
            _lblAmplitudeDeg.Name = "_lblAmplitudeDeg";
            _lblAmplitudeDeg.Size = new Size(18, 18);
            _lblAmplitudeDeg.TabIndex = 2;
            _lblAmplitudeDeg.Text = "°";
            // 
            // _lblAmplitude
            // 
            _lblAmplitude.Font = new Font("Segoe UI", 8F);
            _lblAmplitude.Location = new Point(8, 23);
            _lblAmplitude.Name = "_lblAmplitude";
            _lblAmplitude.Size = new Size(25, 18);
            _lblAmplitude.TabIndex = 1;
            _lblAmplitude.Text = "0°";
            // 
            // _btnReset
            // 
            _btnReset.BackColor = Color.FromArgb(230, 230, 235);
            _btnReset.FlatStyle = FlatStyle.Flat;
            _btnReset.Font = new Font("Segoe UI", 9F);
            _btnReset.Location = new Point(82, 417);
            _btnReset.Name = "_btnReset";
            _btnReset.Size = new Size(110, 35);
            _btnReset.TabIndex = 5;
            _btnReset.Text = "↺ Reset";
            _btnReset.UseVisualStyleBackColor = false;
            // 
            // _labelRotateInfo
            // 
            _labelRotateInfo.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            _labelRotateInfo.ForeColor = Color.Gray;
            _labelRotateInfo.Location = new Point(4, 455);
            _labelRotateInfo.Name = "_labelRotateInfo";
            _labelRotateInfo.Size = new Size(256, 20);
            _labelRotateInfo.TabIndex = 6;
            _labelRotateInfo.Text = "▶ Continuous rotation (constant speed)";
            _labelRotateInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _labelWobbleInfo
            // 
            _labelWobbleInfo.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            _labelWobbleInfo.ForeColor = Color.Gray;
            _labelWobbleInfo.Location = new Point(9, 475);
            _labelWobbleInfo.Name = "_labelWobbleInfo";
            _labelWobbleInfo.Size = new Size(256, 20);
            _labelWobbleInfo.TabIndex = 7;
            _labelWobbleInfo.Text = "〰️ Oscillating rotation (cosine)";
            _labelWobbleInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // AutoRotateWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(280, 557);
            Controls.Add(_trackAmplitude);
            Controls.Add(_labelWobbleInfo);
            Controls.Add(_labelRotateInfo);
            Controls.Add(_btnReset);
            Controls.Add(_grpAmplitude);
            Controls.Add(_grpSpeed);
            Controls.Add(_grpAxis);
            Controls.Add(_grpMode);
            Controls.Add(_lblTitle);
            Font = new Font("Segoe UI", 9F);
            Name = "AutoRotateWindow";
            Text = "🔄 Auto Rotate";
            _grpMode.ResumeLayout(false);
            _grpAxis.ResumeLayout(false);
            _grpSpeed.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_trackSpeed).EndInit();
            _grpAmplitude.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_trackAmplitude).EndInit();
            ResumeLayout(false);
        }
    }
}