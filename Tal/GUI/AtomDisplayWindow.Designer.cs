// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// AtomDisplayWindow designer-generated layout.

﻿using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class AtomDisplayWindow : DockContent
    {
        private System.ComponentModel.IContainer components = null;

        // ⭐ DEKLARACJE KONTROLEK
        private Label _lblTitle;

        // ⭐ ROZMIARY KULEK
        private GroupBox _grpBallSizes;
        private Label _lblNonMetal;
        private TrackBar _trackNonMetal;
        private Label _labelNonMetalValue;
        private Label _lblMetal;
        private TrackBar _trackMetal;
        private Label _labelMetalValue;
        private Label _lblHydrogen;
        private TrackBar _trackHydrogen;
        private Label _labelHydrogenValue;

        // ⭐ STICK THICKNESS
        private GroupBox _grpStickSizes;
        private Label _lblStickRadius;
        private TrackBar _trackStickRadius;
        private Label _labelStickRadiusValue;
        private Label _lblStickFactor;
        private TrackBar _trackStickFactor;
        private Label _labelStickFactorValue;

        // ⭐ UNIT-CELL LINES

        // ⭐ PARAMETRY ELIPSOID
        private GroupBox _grpEllipsoid;
        private CheckBox _chkEllipsoidDisks;
        private CheckBox _chkShowHydrogenIsoSpheres;
        private Label _lblEllipsoidProbability;
        private TrackBar _trackEllipsoidProbability;
        private Label _labelEllipsoidProbabilityValue;
        private Label _lblEllipsoidDiskThickness;
        private TrackBar _trackEllipsoidDiskThickness;
        private Label _labelEllipsoidDiskThicknessValue;
        private Label _lblEllipsoidBaseRadius;
        private TrackBar _trackEllipsoidBaseRadius;
        private Label _labelEllipsoidBaseRadiusValue;

        // ⭐ PRZYCISKI
        private Button _btnReset;
        private Button _btnApply;
        private Button _btnDefault;

        // ⭐ SEPARATORY
        private Label _sep1;
        private Label _sep2;
        private Label _sep3;
        private Label _sep4;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            _lblTitle = new Label();
            _grpBallSizes = new GroupBox();
            _trackHydrogen = new TrackBar();
            _trackMetal = new TrackBar();
            _lblNonMetal = new Label();
            _trackNonMetal = new TrackBar();
            _labelNonMetalValue = new Label();
            _lblMetal = new Label();
            _labelMetalValue = new Label();
            _lblHydrogen = new Label();
            _labelHydrogenValue = new Label();
            _sep1 = new Label();
            _grpStickSizes = new GroupBox();
            _trackStickFactor = new TrackBar();
            _lblStickRadius = new Label();
            _trackStickRadius = new TrackBar();
            _labelStickRadiusValue = new Label();
            _lblStickFactor = new Label();
            _labelStickFactorValue = new Label();
            _sep2 = new Label();
            _sep3 = new Label();
            _grpEllipsoid = new GroupBox();
            _chkShowHydrogenIsoSpheres = new CheckBox();
            _chkEllipsoidDisks = new CheckBox();
            _trackEllipsoidBaseRadius = new TrackBar();
            _trackEllipsoidDiskThickness = new TrackBar();
            _lblEllipsoidProbability = new Label();
            _trackEllipsoidProbability = new TrackBar();
            _labelEllipsoidProbabilityValue = new Label();
            _lblEllipsoidDiskThickness = new Label();
            _labelEllipsoidDiskThicknessValue = new Label();
            _lblEllipsoidBaseRadius = new Label();
            _labelEllipsoidBaseRadiusValue = new Label();
            _sep4 = new Label();
            _btnDefault = new Button();
            _btnReset = new Button();
            _btnApply = new Button();

            _grpBallSizes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_trackHydrogen).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackMetal).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackNonMetal).BeginInit();
            _grpStickSizes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_trackStickFactor).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackStickRadius).BeginInit();
            _grpEllipsoid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_trackEllipsoidBaseRadius).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackEllipsoidDiskThickness).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackEllipsoidProbability).BeginInit();
            SuspendLayout();

            // 
            // _lblTitle
            // 
            _lblTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _lblTitle.Location = new Point(10, 10);
            _lblTitle.Name = "_lblTitle";
            _lblTitle.Size = new Size(300, 28);
            _lblTitle.TabIndex = 0;
            _lblTitle.Text = "🎨 ATOM DISPLAY SETTINGS";
            _lblTitle.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _grpBallSizes
            // 
            // ⭐ HEIGHT 170 (was 140) — 3 TrackBars in separate rows (24, 64, 104)
            _grpBallSizes.Controls.Add(_trackHydrogen);
            _grpBallSizes.Controls.Add(_trackMetal);
            _grpBallSizes.Controls.Add(_lblNonMetal);
            _grpBallSizes.Controls.Add(_trackNonMetal);
            _grpBallSizes.Controls.Add(_labelNonMetalValue);
            _grpBallSizes.Controls.Add(_lblMetal);
            _grpBallSizes.Controls.Add(_labelMetalValue);
            _grpBallSizes.Controls.Add(_lblHydrogen);
            _grpBallSizes.Controls.Add(_labelHydrogenValue);
            _grpBallSizes.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpBallSizes.ForeColor = Color.FromArgb(51, 51, 51);
            _grpBallSizes.Location = new Point(10, 44);
            _grpBallSizes.Name = "_grpBallSizes";
            _grpBallSizes.Size = new Size(300, 170);
            _grpBallSizes.TabIndex = 1;
            _grpBallSizes.TabStop = false;
            _grpBallSizes.Text = "BALL SIZES";

            // 
            // _trackHydrogen
            // 
            _trackHydrogen.Location = new Point(120, 104);
            _trackHydrogen.Maximum = 100;
            _trackHydrogen.Minimum = 10;
            _trackHydrogen.Name = "_trackHydrogen";
            _trackHydrogen.Size = new Size(120, 45);
            _trackHydrogen.TabIndex = 7;
            _trackHydrogen.TickFrequency = 5;
            _trackHydrogen.Value = 70;

            // 
            // _trackMetal
            // 
            _trackMetal.Location = new Point(120, 64);
            _trackMetal.Maximum = 100;
            _trackMetal.Minimum = 1;
            _trackMetal.Name = "_trackMetal";
            _trackMetal.Size = new Size(120, 45);
            _trackMetal.TabIndex = 4;
            _trackMetal.TickFrequency = 5;
            _trackMetal.Value = 12;

            // 
            // _lblNonMetal
            // 
            _lblNonMetal.Font = new Font("Segoe UI", 8.25F);
            _lblNonMetal.Location = new Point(10, 24);
            _lblNonMetal.Name = "_lblNonMetal";
            _lblNonMetal.Size = new Size(105, 20);
            _lblNonMetal.TabIndex = 0;
            _lblNonMetal.Text = "Non-Metal:";
            _lblNonMetal.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _trackNonMetal
            // 
            _trackNonMetal.Location = new Point(120, 24);
            _trackNonMetal.Maximum = 100;
            _trackNonMetal.Minimum = 1;
            _trackNonMetal.Name = "_trackNonMetal";
            _trackNonMetal.Size = new Size(120, 45);
            _trackNonMetal.TabIndex = 1;
            _trackNonMetal.TickFrequency = 5;
            _trackNonMetal.Value = 8;

            // 
            // _labelNonMetalValue
            // 
            _labelNonMetalValue.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            _labelNonMetalValue.Location = new Point(245, 24);
            _labelNonMetalValue.Name = "_labelNonMetalValue";
            _labelNonMetalValue.Size = new Size(50, 20);
            _labelNonMetalValue.TabIndex = 2;
            _labelNonMetalValue.Text = "0.08";
            _labelNonMetalValue.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _lblMetal
            // 
            _lblMetal.Font = new Font("Segoe UI", 8.25F);
            _lblMetal.Location = new Point(10, 64);
            _lblMetal.Name = "_lblMetal";
            _lblMetal.Size = new Size(105, 20);
            _lblMetal.TabIndex = 3;
            _lblMetal.Text = "Metal:";
            _lblMetal.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _labelMetalValue
            // 
            _labelMetalValue.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            _labelMetalValue.Location = new Point(245, 64);
            _labelMetalValue.Name = "_labelMetalValue";
            _labelMetalValue.Size = new Size(50, 20);
            _labelMetalValue.TabIndex = 5;
            _labelMetalValue.Text = "0.12";
            _labelMetalValue.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _lblHydrogen
            // 
            _lblHydrogen.Font = new Font("Segoe UI", 8.25F);
            _lblHydrogen.Location = new Point(10, 104);
            _lblHydrogen.Name = "_lblHydrogen";
            _lblHydrogen.Size = new Size(105, 20);
            _lblHydrogen.TabIndex = 6;
            _lblHydrogen.Text = "Hydrogen (%):";
            _lblHydrogen.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _labelHydrogenValue
            // 
            _labelHydrogenValue.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            _labelHydrogenValue.Location = new Point(245, 104);
            _labelHydrogenValue.Name = "_labelHydrogenValue";
            _labelHydrogenValue.Size = new Size(50, 20);
            _labelHydrogenValue.TabIndex = 8;
            _labelHydrogenValue.Text = "70%";
            _labelHydrogenValue.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _sep1
            // 
            _sep1.BackColor = Color.LightGray;
            _sep1.ForeColor = Color.LightGray;
            _sep1.Location = new Point(10, 220);
            _sep1.Name = "_sep1";
            _sep1.Size = new Size(300, 2);
            _sep1.TabIndex = 2;

            // 
            // _grpStickSizes
            // 
            // ⭐ HEIGHT 130 (was 100) — 2 TrackBars (24, 64)
            _grpStickSizes.Controls.Add(_trackStickFactor);
            _grpStickSizes.Controls.Add(_lblStickRadius);
            _grpStickSizes.Controls.Add(_trackStickRadius);
            _grpStickSizes.Controls.Add(_labelStickRadiusValue);
            _grpStickSizes.Controls.Add(_lblStickFactor);
            _grpStickSizes.Controls.Add(_labelStickFactorValue);
            _grpStickSizes.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpStickSizes.ForeColor = Color.FromArgb(51, 51, 51);
            _grpStickSizes.Location = new Point(10, 228);
            _grpStickSizes.Name = "_grpStickSizes";
            _grpStickSizes.Size = new Size(300, 130);
            _grpStickSizes.TabIndex = 3;
            _grpStickSizes.TabStop = false;
            _grpStickSizes.Text = "STICK SIZES";

            // 
            // _trackStickFactor
            // 
            _trackStickFactor.Location = new Point(120, 64);
            _trackStickFactor.Maximum = 200;
            _trackStickFactor.Minimum = 50;
            _trackStickFactor.Name = "_trackStickFactor";
            _trackStickFactor.Size = new Size(120, 45);
            _trackStickFactor.TabIndex = 4;
            _trackStickFactor.TickFrequency = 10;
            _trackStickFactor.Value = 127;

            // 
            // _lblStickRadius
            // 
            _lblStickRadius.Font = new Font("Segoe UI", 8.25F);
            _lblStickRadius.Location = new Point(10, 26);
            _lblStickRadius.Name = "_lblStickRadius";
            _lblStickRadius.Size = new Size(105, 20);
            _lblStickRadius.TabIndex = 0;
            _lblStickRadius.Text = "Radius:";
            _lblStickRadius.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _trackStickRadius
            // 
            _trackStickRadius.Location = new Point(120, 26);
            _trackStickRadius.Maximum = 100;
            _trackStickRadius.Minimum = 1;
            _trackStickRadius.Name = "_trackStickRadius";
            _trackStickRadius.Size = new Size(120, 45);
            _trackStickRadius.TabIndex = 1;
            _trackStickRadius.TickFrequency = 5;
            _trackStickRadius.Value = 3;

            // 
            // _labelStickRadiusValue
            // 
            _labelStickRadiusValue.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            _labelStickRadiusValue.Location = new Point(245, 26);
            _labelStickRadiusValue.Name = "_labelStickRadiusValue";
            _labelStickRadiusValue.Size = new Size(50, 20);
            _labelStickRadiusValue.TabIndex = 2;
            _labelStickRadiusValue.Text = "0.03";
            _labelStickRadiusValue.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _lblStickFactor
            // 
            _lblStickFactor.Font = new Font("Segoe UI", 8.25F);
            _lblStickFactor.Location = new Point(10, 64);
            _lblStickFactor.Name = "_lblStickFactor";
            _lblStickFactor.Size = new Size(105, 20);
            _lblStickFactor.TabIndex = 3;
            _lblStickFactor.Text = "Length Factor:";
            _lblStickFactor.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _labelStickFactorValue
            // 
            _labelStickFactorValue.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            _labelStickFactorValue.Location = new Point(245, 64);
            _labelStickFactorValue.Name = "_labelStickFactorValue";
            _labelStickFactorValue.Size = new Size(50, 20);
            _labelStickFactorValue.TabIndex = 5;
            _labelStickFactorValue.Text = "0.0127";
            _labelStickFactorValue.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _sep2
            // 
            _sep2.BackColor = Color.LightGray;
            _sep2.ForeColor = Color.LightGray;
            _sep2.Location = new Point(10, 364);
            _sep2.Name = "_sep2";
            _sep2.Size = new Size(300, 2);
            _sep2.TabIndex = 4;

            // 
            // _sep3
            // 
            _sep3.BackColor = Color.LightGray;
            _sep3.ForeColor = Color.LightGray;
            _sep3.Location = new Point(10, 508);
            _sep3.Name = "_sep3";
            _sep3.Size = new Size(300, 2);
            _sep3.TabIndex = 6;

            // 
            // _grpEllipsoid
            // 
            // ⭐ HEIGHT 245 (was 195) — 2 CheckBoxy + 3 TrackBars
            _grpEllipsoid.Controls.Add(_chkShowHydrogenIsoSpheres);
            _grpEllipsoid.Controls.Add(_chkEllipsoidDisks);
            _grpEllipsoid.Controls.Add(_trackEllipsoidBaseRadius);
            _grpEllipsoid.Controls.Add(_trackEllipsoidDiskThickness);
            _grpEllipsoid.Controls.Add(_lblEllipsoidProbability);
            _grpEllipsoid.Controls.Add(_trackEllipsoidProbability);
            _grpEllipsoid.Controls.Add(_labelEllipsoidProbabilityValue);
            _grpEllipsoid.Controls.Add(_lblEllipsoidDiskThickness);
            _grpEllipsoid.Controls.Add(_labelEllipsoidDiskThicknessValue);
            _grpEllipsoid.Controls.Add(_lblEllipsoidBaseRadius);
            _grpEllipsoid.Controls.Add(_labelEllipsoidBaseRadiusValue);
            _grpEllipsoid.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpEllipsoid.ForeColor = Color.FromArgb(51, 51, 51);
            _grpEllipsoid.Location = new Point(10, 516);
            _grpEllipsoid.Name = "_grpEllipsoid";
            _grpEllipsoid.Size = new Size(300, 245);
            _grpEllipsoid.TabIndex = 7;
            _grpEllipsoid.TabStop = false;
            _grpEllipsoid.Text = "ELLIPSOID PARAMETERS";

            // 
            // _chkShowHydrogenIsoSpheres
            // 
            _chkShowHydrogenIsoSpheres.Checked = true;
            _chkShowHydrogenIsoSpheres.CheckState = CheckState.Checked;
            _chkShowHydrogenIsoSpheres.Font = new Font("Segoe UI", 8.25F);
            _chkShowHydrogenIsoSpheres.Location = new Point(10, 214);
            _chkShowHydrogenIsoSpheres.Name = "_chkShowHydrogenIsoSpheres";
            _chkShowHydrogenIsoSpheres.Size = new Size(220, 22);
            _chkShowHydrogenIsoSpheres.TabIndex = 10;
            _chkShowHydrogenIsoSpheres.Text = "Show Hydrogen as Sphere";
            _chkShowHydrogenIsoSpheres.UseVisualStyleBackColor = true;

            // 
            // _chkEllipsoidDisks
            // 
            _chkEllipsoidDisks.Checked = true;
            _chkEllipsoidDisks.CheckState = CheckState.Checked;
            _chkEllipsoidDisks.Font = new Font("Segoe UI", 8.25F);
            _chkEllipsoidDisks.Location = new Point(10, 24);
            _chkEllipsoidDisks.Name = "_chkEllipsoidDisks";
            _chkEllipsoidDisks.Size = new Size(140, 22);
            _chkEllipsoidDisks.TabIndex = 0;
            _chkEllipsoidDisks.Text = "Show Disks";
            _chkEllipsoidDisks.UseVisualStyleBackColor = true;

            // 
            // _trackEllipsoidBaseRadius
            // 
            _trackEllipsoidBaseRadius.Location = new Point(120, 154);
            _trackEllipsoidBaseRadius.Maximum = 200;
            _trackEllipsoidBaseRadius.Minimum = 50;
            _trackEllipsoidBaseRadius.Name = "_trackEllipsoidBaseRadius";
            _trackEllipsoidBaseRadius.Size = new Size(120, 45);
            _trackEllipsoidBaseRadius.TabIndex = 8;
            _trackEllipsoidBaseRadius.TickFrequency = 10;
            _trackEllipsoidBaseRadius.Value = 105;

            // 
            // _trackEllipsoidDiskThickness
            // 
            _trackEllipsoidDiskThickness.Location = new Point(120, 114);
            _trackEllipsoidDiskThickness.Maximum = 50;
            _trackEllipsoidDiskThickness.Minimum = 1;
            _trackEllipsoidDiskThickness.Name = "_trackEllipsoidDiskThickness";
            _trackEllipsoidDiskThickness.Size = new Size(120, 45);
            _trackEllipsoidDiskThickness.TabIndex = 5;
            _trackEllipsoidDiskThickness.TickFrequency = 5;
            _trackEllipsoidDiskThickness.Value = 5;

            // 
            // _lblEllipsoidProbability
            // 
            _lblEllipsoidProbability.Font = new Font("Segoe UI", 8.25F);
            _lblEllipsoidProbability.Location = new Point(10, 74);
            _lblEllipsoidProbability.Name = "_lblEllipsoidProbability";
            _lblEllipsoidProbability.Size = new Size(105, 20);
            _lblEllipsoidProbability.TabIndex = 1;
            _lblEllipsoidProbability.Text = "Probability (%):";
            _lblEllipsoidProbability.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _trackEllipsoidProbability
            // 
            _trackEllipsoidProbability.Location = new Point(120, 74);
            _trackEllipsoidProbability.Maximum = 100;
            _trackEllipsoidProbability.Minimum = 1;
            _trackEllipsoidProbability.Name = "_trackEllipsoidProbability";
            _trackEllipsoidProbability.Size = new Size(120, 45);
            _trackEllipsoidProbability.TabIndex = 2;
            _trackEllipsoidProbability.TickFrequency = 5;
            _trackEllipsoidProbability.Value = 50;

            // 
            // _labelEllipsoidProbabilityValue
            // 
            _labelEllipsoidProbabilityValue.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            _labelEllipsoidProbabilityValue.Location = new Point(245, 74);
            _labelEllipsoidProbabilityValue.Name = "_labelEllipsoidProbabilityValue";
            _labelEllipsoidProbabilityValue.Size = new Size(50, 20);
            _labelEllipsoidProbabilityValue.TabIndex = 3;
            _labelEllipsoidProbabilityValue.Text = "50%";
            _labelEllipsoidProbabilityValue.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _lblEllipsoidDiskThickness
            // 
            _lblEllipsoidDiskThickness.Font = new Font("Segoe UI", 8.25F);
            _lblEllipsoidDiskThickness.Location = new Point(10, 114);
            _lblEllipsoidDiskThickness.Name = "_lblEllipsoidDiskThickness";
            _lblEllipsoidDiskThickness.Size = new Size(105, 20);
            _lblEllipsoidDiskThickness.TabIndex = 4;
            _lblEllipsoidDiskThickness.Text = "Disk Thickness:";
            _lblEllipsoidDiskThickness.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _labelEllipsoidDiskThicknessValue
            // 
            _labelEllipsoidDiskThicknessValue.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            _labelEllipsoidDiskThicknessValue.Location = new Point(245, 114);
            _labelEllipsoidDiskThicknessValue.Name = "_labelEllipsoidDiskThicknessValue";
            _labelEllipsoidDiskThicknessValue.Size = new Size(50, 20);
            _labelEllipsoidDiskThicknessValue.TabIndex = 6;
            _labelEllipsoidDiskThicknessValue.Text = "0.15";
            _labelEllipsoidDiskThicknessValue.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _lblEllipsoidBaseRadius
            // 
            _lblEllipsoidBaseRadius.Font = new Font("Segoe UI", 8.25F);
            _lblEllipsoidBaseRadius.Location = new Point(10, 154);
            _lblEllipsoidBaseRadius.Name = "_lblEllipsoidBaseRadius";
            _lblEllipsoidBaseRadius.Size = new Size(105, 20);
            _lblEllipsoidBaseRadius.TabIndex = 7;
            _lblEllipsoidBaseRadius.Text = "Base Radius:";
            _lblEllipsoidBaseRadius.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _labelEllipsoidBaseRadiusValue
            // 
            _labelEllipsoidBaseRadiusValue.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            _labelEllipsoidBaseRadiusValue.Location = new Point(245, 154);
            _labelEllipsoidBaseRadiusValue.Name = "_labelEllipsoidBaseRadiusValue";
            _labelEllipsoidBaseRadiusValue.Size = new Size(50, 20);
            _labelEllipsoidBaseRadiusValue.TabIndex = 9;
            _labelEllipsoidBaseRadiusValue.Text = "1.05";
            _labelEllipsoidBaseRadiusValue.TextAlign = ContentAlignment.MiddleLeft;

            // 
            // _sep4
            // 
            _sep4.BackColor = Color.LightGray;
            _sep4.ForeColor = Color.LightGray;
            _sep4.Location = new Point(10, 767);
            _sep4.Name = "_sep4";
            _sep4.Size = new Size(300, 2);
            _sep4.TabIndex = 8;

            // 
            // _btnDefault
            // 
            _btnDefault.BackColor = Color.FromArgb(230, 230, 235);
            _btnDefault.FlatStyle = FlatStyle.Flat;
            _btnDefault.Location = new Point(10, 779);
            _btnDefault.Name = "_btnDefault";
            _btnDefault.Size = new Size(90, 32);
            _btnDefault.TabIndex = 9;
            _btnDefault.Text = "Default";
            _btnDefault.UseVisualStyleBackColor = false;

            // 
            // _btnReset
            // 
            _btnReset.BackColor = Color.FromArgb(230, 230, 235);
            _btnReset.FlatStyle = FlatStyle.Flat;
            _btnReset.Location = new Point(106, 779);
            _btnReset.Name = "_btnReset";
            _btnReset.Size = new Size(90, 32);
            _btnReset.TabIndex = 10;
            _btnReset.Text = "↺ Reset";
            _btnReset.UseVisualStyleBackColor = false;

            // 
            // _btnApply
            // 
            _btnApply.BackColor = Color.FromArgb(0, 122, 204);
            _btnApply.FlatStyle = FlatStyle.Flat;
            _btnApply.ForeColor = Color.White;
            _btnApply.Location = new Point(210, 779);
            _btnApply.Name = "_btnApply";
            _btnApply.Size = new Size(95, 32);
            _btnApply.TabIndex = 11;
            _btnApply.Text = "Apply";
            _btnApply.UseVisualStyleBackColor = false;

            // 
            // AtomDisplayWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            ClientSize = new Size(330, 830);
            Controls.Add(_lblTitle);
            Controls.Add(_grpBallSizes);
            Controls.Add(_sep1);
            Controls.Add(_grpStickSizes);
            Controls.Add(_sep2);
            Controls.Add(_sep3);
            Controls.Add(_grpEllipsoid);
            Controls.Add(_sep4);
            Controls.Add(_btnDefault);
            Controls.Add(_btnReset);
            Controls.Add(_btnApply);
            Font = new Font("Segoe UI", 8.25F);
            MinimumSize = new Size(330, 560);
            Name = "AtomDisplayWindow";
            Text = "🎨 Atom Display Settings";

            _grpBallSizes.ResumeLayout(false);
            _grpBallSizes.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_trackHydrogen).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackMetal).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackNonMetal).EndInit();
            _grpStickSizes.ResumeLayout(false);
            _grpStickSizes.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_trackStickFactor).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackStickRadius).EndInit();
            _grpEllipsoid.ResumeLayout(false);
            _grpEllipsoid.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_trackEllipsoidBaseRadius).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackEllipsoidDiskThickness).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackEllipsoidProbability).EndInit();
            ResumeLayout(false);
        }
    }
}