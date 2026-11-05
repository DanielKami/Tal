// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// LightingWindow designer-generated layout.

﻿namespace Tal.Forms
{
    partial class LightingWindow
    {
        private System.ComponentModel.IContainer components = null;

        // ⭐ DEKLARACJE KONTROLEK
        private System.Windows.Forms.Label _lblTitle;

        // ⭐ FIRST LIGHT
        private System.Windows.Forms.CheckBox _chkLightingEnabled;
        private System.Windows.Forms.CheckBox _chkPerPixelLighting;
        private System.Windows.Forms.Label _sep1;
        private System.Windows.Forms.Label _lblLight1Group;
        private System.Windows.Forms.Label _lblDir;
        private System.Windows.Forms.Label _lblX;
        private System.Windows.Forms.TrackBar _trackDirectionX;
        private System.Windows.Forms.Label _labelDirectionX;
        private System.Windows.Forms.Label _lblY;
        private System.Windows.Forms.TrackBar _trackDirectionY;
        private System.Windows.Forms.Label _labelDirectionY;
        private System.Windows.Forms.Label _lblZ;
        private System.Windows.Forms.TrackBar _trackDirectionZ;
        private System.Windows.Forms.Label _labelDirectionZ;
        private System.Windows.Forms.Label _lblIntensity;
        private System.Windows.Forms.Label _lblSpecPower;
        private System.Windows.Forms.TrackBar _trackSpecularPower;
        private System.Windows.Forms.Label _labelSpecularPower;
        private System.Windows.Forms.Label _lblSpecIntensity;
        private System.Windows.Forms.TrackBar _trackSpecularIntensity;
        private System.Windows.Forms.Label _labelSpecularIntensity;
        private System.Windows.Forms.Label _lblDiffIntensity;
        private System.Windows.Forms.TrackBar _trackDiffuseIntensity;
        private System.Windows.Forms.Label _labelDiffuseIntensity;
        private System.Windows.Forms.Label _lblAmbIntensity;
        private System.Windows.Forms.TrackBar _trackAmbientIntensity;
        private System.Windows.Forms.Label _labelAmbientIntensity;

        // ⭐⭐ SECOND LIGHT ⭐⭐
        private System.Windows.Forms.Label _sep2;
        private System.Windows.Forms.Label _lblLight2Group;
        private System.Windows.Forms.CheckBox _chkLight2Enabled;
        private System.Windows.Forms.Label _lblLight2X;
        private System.Windows.Forms.TrackBar _trackLight2DirectionX;
        private System.Windows.Forms.Label _labelLight2DirectionX;
        private System.Windows.Forms.Label _lblLight2Y;
        private System.Windows.Forms.TrackBar _trackLight2DirectionY;
        private System.Windows.Forms.Label _labelLight2DirectionY;
        private System.Windows.Forms.Label _lblLight2Z;
        private System.Windows.Forms.TrackBar _trackLight2DirectionZ;
        private System.Windows.Forms.Label _labelLight2DirectionZ;
        private System.Windows.Forms.Label _lblLight2DiffIntensity;
        private System.Windows.Forms.TrackBar _trackLight2DiffuseIntensity;
        private System.Windows.Forms.Label _labelLight2DiffuseIntensity;
        private System.Windows.Forms.Label _lblLight2AmbIntensity;
        private System.Windows.Forms.TrackBar _trackLight2AmbientIntensity;
        private System.Windows.Forms.Label _labelLight2AmbientIntensity;
        private System.Windows.Forms.Label _lblLight2SpecIntensity;
        private System.Windows.Forms.TrackBar _trackLight2SpecularIntensity;
        private System.Windows.Forms.Label _labelLight2SpecularIntensity;

        // ⭐ PREVIEW AND BUTTONS
        private System.Windows.Forms.Panel _colorPreviewPanel;
        private System.Windows.Forms.Label _labelPreview;
        private System.Windows.Forms.Button _btnReset;
        private System.Windows.Forms.Button _btnApply;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            _lblTitle = new Label();
            _chkLightingEnabled = new CheckBox();
            _chkPerPixelLighting = new CheckBox();
            _lblLight1Group = new Label();
            _lblX = new Label();
            _trackDirectionX = new TrackBar();
            _labelDirectionX = new Label();
            _lblY = new Label();
            _trackDirectionY = new TrackBar();
            _labelDirectionY = new Label();
            _lblZ = new Label();
            _trackDirectionZ = new TrackBar();
            _labelDirectionZ = new Label();
            _sep1 = new Label();
            _lblIntensity = new Label();
            _lblSpecPower = new Label();
            _trackSpecularPower = new TrackBar();
            _labelSpecularPower = new Label();
            _lblSpecIntensity = new Label();
            _trackSpecularIntensity = new TrackBar();
            _labelSpecularIntensity = new Label();
            _lblDiffIntensity = new Label();
            _trackDiffuseIntensity = new TrackBar();
            _labelDiffuseIntensity = new Label();
            _lblAmbIntensity = new Label();
            _trackAmbientIntensity = new TrackBar();
            _labelAmbientIntensity = new Label();
            _sep2 = new Label();
            _lblLight2Group = new Label();
            _chkLight2Enabled = new CheckBox();
            _lblLight2X = new Label();
            _trackLight2DirectionX = new TrackBar();
            _labelLight2DirectionX = new Label();
            _lblLight2Y = new Label();
            _trackLight2DirectionY = new TrackBar();
            _labelLight2DirectionY = new Label();
            _lblLight2Z = new Label();
            _trackLight2DirectionZ = new TrackBar();
            _labelLight2DirectionZ = new Label();
            _lblLight2DiffIntensity = new Label();
            _trackLight2DiffuseIntensity = new TrackBar();
            _labelLight2DiffuseIntensity = new Label();
            _lblLight2AmbIntensity = new Label();
            _trackLight2AmbientIntensity = new TrackBar();
            _labelLight2AmbientIntensity = new Label();
            _lblLight2SpecIntensity = new Label();
            _trackLight2SpecularIntensity = new TrackBar();
            _labelLight2SpecularIntensity = new Label();
            _colorPreviewPanel = new Panel();
            _labelPreview = new Label();
            _btnReset = new Button();
            _btnApply = new Button();
            label1 = new Label();
            ((System.ComponentModel.ISupportInitialize)_trackDirectionX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackDirectionY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackDirectionZ).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackSpecularPower).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackSpecularIntensity).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackDiffuseIntensity).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackAmbientIntensity).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackLight2DirectionX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackLight2DirectionY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackLight2DirectionZ).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackLight2DiffuseIntensity).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackLight2AmbientIntensity).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackLight2SpecularIntensity).BeginInit();
            SuspendLayout();
            // 
            // _lblTitle
            // 
            _lblTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _lblTitle.Location = new Point(10, 10);
            _lblTitle.Name = "_lblTitle";
            _lblTitle.Size = new Size(290, 28);
            _lblTitle.TabIndex = 0;
            _lblTitle.Text = "LIGHTING CONTROLS";
            _lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _chkLightingEnabled
            // 
            _chkLightingEnabled.Location = new Point(10, 44);
            _chkLightingEnabled.Name = "_chkLightingEnabled";
            _chkLightingEnabled.Size = new Size(120, 22);
            _chkLightingEnabled.TabIndex = 1;
            _chkLightingEnabled.Text = "Enable Lighting";
            _chkLightingEnabled.UseVisualStyleBackColor = true;
            // 
            // _chkPerPixelLighting
            // 
            _chkPerPixelLighting.Location = new Point(140, 44);
            _chkPerPixelLighting.Name = "_chkPerPixelLighting";
            _chkPerPixelLighting.Size = new Size(130, 22);
            _chkPerPixelLighting.TabIndex = 2;
            _chkPerPixelLighting.Text = "Per-Pixel Lighting";
            _chkPerPixelLighting.UseVisualStyleBackColor = true;
            // 
            // _lblLight1Group
            // 
            _lblLight1Group.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblLight1Group.ForeColor = Color.FromArgb(0, 122, 204);
            _lblLight1Group.Location = new Point(10, 72);
            _lblLight1Group.Name = "_lblLight1Group";
            _lblLight1Group.Size = new Size(200, 20);
            _lblLight1Group.TabIndex = 3;
            _lblLight1Group.Text = "▶ LIGHT 1 DIRECTION";
            _lblLight1Group.TextAlign = ContentAlignment.MiddleLeft;
            _lblLight1Group.Click += _lblLight1Group_Click;
            // 
            // _lblX
            // 
            _lblX.Location = new Point(10, 96);
            _lblX.Name = "_lblX";
            _lblX.Size = new Size(20, 22);
            _lblX.TabIndex = 4;
            _lblX.Text = "X:";
            _lblX.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackDirectionX
            // 
            _trackDirectionX.Location = new Point(32, 96);
            _trackDirectionX.Maximum = 100;
            _trackDirectionX.Minimum = -100;
            _trackDirectionX.Name = "_trackDirectionX";
            _trackDirectionX.Size = new Size(190, 56);
            _trackDirectionX.TabIndex = 5;
            _trackDirectionX.TickFrequency = 10;
            // 
            // _labelDirectionX
            // 
            _labelDirectionX.Location = new Point(225, 96);
            _labelDirectionX.Name = "_labelDirectionX";
            _labelDirectionX.Size = new Size(45, 22);
            _labelDirectionX.TabIndex = 6;
            _labelDirectionX.Text = "0.00";
            _labelDirectionX.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblY
            // 
            _lblY.Location = new Point(10, 144);
            _lblY.Name = "_lblY";
            _lblY.Size = new Size(20, 22);
            _lblY.TabIndex = 7;
            _lblY.Text = "Y:";
            _lblY.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackDirectionY
            // 
            _trackDirectionY.Location = new Point(32, 144);
            _trackDirectionY.Maximum = 100;
            _trackDirectionY.Minimum = -100;
            _trackDirectionY.Name = "_trackDirectionY";
            _trackDirectionY.Size = new Size(190, 56);
            _trackDirectionY.TabIndex = 8;
            _trackDirectionY.TickFrequency = 10;
            _trackDirectionY.Scroll += _trackDirectionY_Scroll;
            // 
            // _labelDirectionY
            // 
            _labelDirectionY.Location = new Point(225, 145);
            _labelDirectionY.Name = "_labelDirectionY";
            _labelDirectionY.Size = new Size(45, 22);
            _labelDirectionY.TabIndex = 9;
            _labelDirectionY.Text = "0.00";
            _labelDirectionY.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblZ
            // 
            _lblZ.Location = new Point(10, 193);
            _lblZ.Name = "_lblZ";
            _lblZ.Size = new Size(20, 22);
            _lblZ.TabIndex = 10;
            _lblZ.Text = "Z:";
            _lblZ.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackDirectionZ
            // 
            _trackDirectionZ.Location = new Point(29, 192);
            _trackDirectionZ.Maximum = 100;
            _trackDirectionZ.Minimum = -100;
            _trackDirectionZ.Name = "_trackDirectionZ";
            _trackDirectionZ.Size = new Size(190, 56);
            _trackDirectionZ.TabIndex = 11;
            _trackDirectionZ.TickFrequency = 10;
            _trackDirectionZ.Value = -100;
            // 
            // _labelDirectionZ
            // 
            _labelDirectionZ.Location = new Point(225, 192);
            _labelDirectionZ.Name = "_labelDirectionZ";
            _labelDirectionZ.Size = new Size(45, 22);
            _labelDirectionZ.TabIndex = 12;
            _labelDirectionZ.Text = "-1.00";
            _labelDirectionZ.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _sep1
            // 
            _sep1.BackColor = Color.LightGray;
            _sep1.ForeColor = Color.LightGray;
            _sep1.Location = new Point(10, 236);
            _sep1.Name = "_sep1";
            _sep1.Size = new Size(280, 2);
            _sep1.TabIndex = 13;
            // 
            // _lblIntensity
            // 
            _lblIntensity.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblIntensity.ForeColor = Color.FromArgb(192, 64, 0);
            _lblIntensity.Location = new Point(288, 251);
            _lblIntensity.Name = "_lblIntensity";
            _lblIntensity.Size = new Size(200, 20);
            _lblIntensity.TabIndex = 14;
            _lblIntensity.Text = "▶ LIGHT 2 INTENSITY";
            _lblIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblSpecPower
            // 
            _lblSpecPower.Location = new Point(10, 285);
            _lblSpecPower.Name = "_lblSpecPower";
            _lblSpecPower.Size = new Size(120, 22);
            _lblSpecPower.TabIndex = 15;
            _lblSpecPower.Text = "Specular Power:";
            _lblSpecPower.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackSpecularPower
            // 
            _trackSpecularPower.Location = new Point(122, 285);
            _trackSpecularPower.Maximum = 100;
            _trackSpecularPower.Minimum = 1;
            _trackSpecularPower.Name = "_trackSpecularPower";
            _trackSpecularPower.Size = new Size(114, 56);
            _trackSpecularPower.TabIndex = 16;
            _trackSpecularPower.TickFrequency = 5;
            _trackSpecularPower.Value = 10;
            // 
            // _labelSpecularPower
            // 
            _labelSpecularPower.Location = new Point(242, 285);
            _labelSpecularPower.Name = "_labelSpecularPower";
            _labelSpecularPower.Size = new Size(45, 22);
            _labelSpecularPower.TabIndex = 17;
            _labelSpecularPower.Text = "10";
            _labelSpecularPower.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblSpecIntensity
            // 
            _lblSpecIntensity.Location = new Point(10, 331);
            _lblSpecIntensity.Name = "_lblSpecIntensity";
            _lblSpecIntensity.Size = new Size(106, 26);
            _lblSpecIntensity.TabIndex = 18;
            _lblSpecIntensity.Text = "Specular:";
            _lblSpecIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackSpecularIntensity
            // 
            _trackSpecularIntensity.Location = new Point(122, 331);
            _trackSpecularIntensity.Maximum = 100;
            _trackSpecularIntensity.Name = "_trackSpecularIntensity";
            _trackSpecularIntensity.Size = new Size(114, 56);
            _trackSpecularIntensity.TabIndex = 19;
            _trackSpecularIntensity.TickFrequency = 10;
            _trackSpecularIntensity.Value = 90;
            // 
            // _labelSpecularIntensity
            // 
            _labelSpecularIntensity.Location = new Point(242, 335);
            _labelSpecularIntensity.Name = "_labelSpecularIntensity";
            _labelSpecularIntensity.Size = new Size(45, 22);
            _labelSpecularIntensity.TabIndex = 20;
            _labelSpecularIntensity.Text = "0.90";
            _labelSpecularIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblDiffIntensity
            // 
            _lblDiffIntensity.Location = new Point(10, 376);
            _lblDiffIntensity.Name = "_lblDiffIntensity";
            _lblDiffIntensity.Size = new Size(106, 28);
            _lblDiffIntensity.TabIndex = 21;
            _lblDiffIntensity.Text = "Diffuse:";
            _lblDiffIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackDiffuseIntensity
            // 
            _trackDiffuseIntensity.Location = new Point(122, 376);
            _trackDiffuseIntensity.Maximum = 100;
            _trackDiffuseIntensity.Name = "_trackDiffuseIntensity";
            _trackDiffuseIntensity.Size = new Size(114, 56);
            _trackDiffuseIntensity.TabIndex = 22;
            _trackDiffuseIntensity.TickFrequency = 10;
            _trackDiffuseIntensity.Value = 90;
            // 
            // _labelDiffuseIntensity
            // 
            _labelDiffuseIntensity.Location = new Point(242, 385);
            _labelDiffuseIntensity.Name = "_labelDiffuseIntensity";
            _labelDiffuseIntensity.Size = new Size(45, 22);
            _labelDiffuseIntensity.TabIndex = 23;
            _labelDiffuseIntensity.Text = "0.90";
            _labelDiffuseIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblAmbIntensity
            // 
            _lblAmbIntensity.Location = new Point(10, 428);
            _lblAmbIntensity.Name = "_lblAmbIntensity";
            _lblAmbIntensity.Size = new Size(95, 22);
            _lblAmbIntensity.TabIndex = 24;
            _lblAmbIntensity.Text = "Ambient:";
            _lblAmbIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackAmbientIntensity
            // 
            _trackAmbientIntensity.Location = new Point(122, 428);
            _trackAmbientIntensity.Maximum = 100;
            _trackAmbientIntensity.Name = "_trackAmbientIntensity";
            _trackAmbientIntensity.Size = new Size(114, 56);
            _trackAmbientIntensity.TabIndex = 25;
            _trackAmbientIntensity.TickFrequency = 10;
            _trackAmbientIntensity.Value = 20;
            // 
            // _labelAmbientIntensity
            // 
            _labelAmbientIntensity.Location = new Point(242, 426);
            _labelAmbientIntensity.Name = "_labelAmbientIntensity";
            _labelAmbientIntensity.Size = new Size(45, 22);
            _labelAmbientIntensity.TabIndex = 26;
            _labelAmbientIntensity.Text = "0.20";
            _labelAmbientIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _sep2
            // 
            _sep2.BackColor = Color.LightGray;
            _sep2.ForeColor = Color.LightGray;
            _sep2.Location = new Point(7, 480);
            _sep2.Name = "_sep2";
            _sep2.Size = new Size(280, 2);
            _sep2.TabIndex = 27;
            // 
            // _lblLight2Group
            // 
            _lblLight2Group.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _lblLight2Group.ForeColor = Color.FromArgb(200, 100, 50);
            _lblLight2Group.Location = new Point(288, 72);
            _lblLight2Group.Name = "_lblLight2Group";
            _lblLight2Group.Size = new Size(207, 20);
            _lblLight2Group.TabIndex = 28;
            _lblLight2Group.Text = "▶ LIGHT 2 DIRECTION";
            _lblLight2Group.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _chkLight2Enabled
            // 
            _chkLight2Enabled.Location = new Point(288, 45);
            _chkLight2Enabled.Name = "_chkLight2Enabled";
            _chkLight2Enabled.Size = new Size(70, 20);
            _chkLight2Enabled.TabIndex = 29;
            _chkLight2Enabled.Text = "Enable";
            _chkLight2Enabled.UseVisualStyleBackColor = true;
            // 
            // _lblLight2X
            // 
            _lblLight2X.Location = new Point(288, 96);
            _lblLight2X.Name = "_lblLight2X";
            _lblLight2X.Size = new Size(20, 22);
            _lblLight2X.TabIndex = 31;
            _lblLight2X.Text = "X:";
            _lblLight2X.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackLight2DirectionX
            // 
            _trackLight2DirectionX.Location = new Point(314, 97);
            _trackLight2DirectionX.Maximum = 100;
            _trackLight2DirectionX.Minimum = -100;
            _trackLight2DirectionX.Name = "_trackLight2DirectionX";
            _trackLight2DirectionX.Size = new Size(190, 56);
            _trackLight2DirectionX.TabIndex = 32;
            _trackLight2DirectionX.TickFrequency = 10;
            _trackLight2DirectionX.Value = -50;
            // 
            // _labelLight2DirectionX
            // 
            _labelLight2DirectionX.Location = new Point(506, 96);
            _labelLight2DirectionX.Name = "_labelLight2DirectionX";
            _labelLight2DirectionX.Size = new Size(45, 22);
            _labelLight2DirectionX.TabIndex = 33;
            _labelLight2DirectionX.Text = "-0.50";
            _labelLight2DirectionX.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblLight2Y
            // 
            _lblLight2Y.Location = new Point(288, 144);
            _lblLight2Y.Name = "_lblLight2Y";
            _lblLight2Y.Size = new Size(20, 22);
            _lblLight2Y.TabIndex = 34;
            _lblLight2Y.Text = "Y:";
            _lblLight2Y.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackLight2DirectionY
            // 
            _trackLight2DirectionY.Location = new Point(314, 144);
            _trackLight2DirectionY.Maximum = 100;
            _trackLight2DirectionY.Minimum = -100;
            _trackLight2DirectionY.Name = "_trackLight2DirectionY";
            _trackLight2DirectionY.Size = new Size(190, 56);
            _trackLight2DirectionY.TabIndex = 35;
            _trackLight2DirectionY.TickFrequency = 10;
            _trackLight2DirectionY.Value = -50;
            // 
            // _labelLight2DirectionY
            // 
            _labelLight2DirectionY.Location = new Point(506, 144);
            _labelLight2DirectionY.Name = "_labelLight2DirectionY";
            _labelLight2DirectionY.Size = new Size(45, 22);
            _labelLight2DirectionY.TabIndex = 36;
            _labelLight2DirectionY.Text = "-0.50";
            _labelLight2DirectionY.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblLight2Z
            // 
            _lblLight2Z.Location = new Point(288, 193);
            _lblLight2Z.Name = "_lblLight2Z";
            _lblLight2Z.Size = new Size(20, 22);
            _lblLight2Z.TabIndex = 37;
            _lblLight2Z.Text = "Z:";
            _lblLight2Z.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackLight2DirectionZ
            // 
            _trackLight2DirectionZ.Location = new Point(314, 182);
            _trackLight2DirectionZ.Maximum = 100;
            _trackLight2DirectionZ.Minimum = -100;
            _trackLight2DirectionZ.Name = "_trackLight2DirectionZ";
            _trackLight2DirectionZ.Size = new Size(190, 56);
            _trackLight2DirectionZ.TabIndex = 38;
            _trackLight2DirectionZ.TickFrequency = 10;
            _trackLight2DirectionZ.Value = 100;
            // 
            // _labelLight2DirectionZ
            // 
            _labelLight2DirectionZ.Location = new Point(506, 182);
            _labelLight2DirectionZ.Name = "_labelLight2DirectionZ";
            _labelLight2DirectionZ.Size = new Size(45, 22);
            _labelLight2DirectionZ.TabIndex = 39;
            _labelLight2DirectionZ.Text = "1.00";
            _labelLight2DirectionZ.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblLight2DiffIntensity
            // 
            _lblLight2DiffIntensity.Location = new Point(302, 383);
            _lblLight2DiffIntensity.Name = "_lblLight2DiffIntensity";
            _lblLight2DiffIntensity.Size = new Size(80, 26);
            _lblLight2DiffIntensity.TabIndex = 41;
            _lblLight2DiffIntensity.Text = "Diffuse:";
            _lblLight2DiffIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackLight2DiffuseIntensity
            // 
            _trackLight2DiffuseIntensity.Location = new Point(365, 385);
            _trackLight2DiffuseIntensity.Maximum = 100;
            _trackLight2DiffuseIntensity.Name = "_trackLight2DiffuseIntensity";
            _trackLight2DiffuseIntensity.Size = new Size(130, 56);
            _trackLight2DiffuseIntensity.TabIndex = 42;
            _trackLight2DiffuseIntensity.TickFrequency = 10;
            _trackLight2DiffuseIntensity.Value = 60;
            // 
            // _labelLight2DiffuseIntensity
            // 
            _labelLight2DiffuseIntensity.Location = new Point(506, 382);
            _labelLight2DiffuseIntensity.Name = "_labelLight2DiffuseIntensity";
            _labelLight2DiffuseIntensity.Size = new Size(45, 22);
            _labelLight2DiffuseIntensity.TabIndex = 43;
            _labelLight2DiffuseIntensity.Text = "0.60";
            _labelLight2DiffuseIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblLight2AmbIntensity
            // 
            _lblLight2AmbIntensity.Location = new Point(302, 426);
            _lblLight2AmbIntensity.Name = "_lblLight2AmbIntensity";
            _lblLight2AmbIntensity.Size = new Size(80, 24);
            _lblLight2AmbIntensity.TabIndex = 44;
            _lblLight2AmbIntensity.Text = "Ambient:";
            _lblLight2AmbIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackLight2AmbientIntensity
            // 
            _trackLight2AmbientIntensity.Location = new Point(365, 426);
            _trackLight2AmbientIntensity.Maximum = 100;
            _trackLight2AmbientIntensity.Name = "_trackLight2AmbientIntensity";
            _trackLight2AmbientIntensity.Size = new Size(130, 56);
            _trackLight2AmbientIntensity.TabIndex = 45;
            _trackLight2AmbientIntensity.TickFrequency = 10;
            _trackLight2AmbientIntensity.Value = 10;
            // 
            // _labelLight2AmbientIntensity
            // 
            _labelLight2AmbientIntensity.Location = new Point(506, 426);
            _labelLight2AmbientIntensity.Name = "_labelLight2AmbientIntensity";
            _labelLight2AmbientIntensity.Size = new Size(45, 22);
            _labelLight2AmbientIntensity.TabIndex = 46;
            _labelLight2AmbientIntensity.Text = "0.10";
            _labelLight2AmbientIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _lblLight2SpecIntensity
            // 
            _lblLight2SpecIntensity.Location = new Point(302, 339);
            _lblLight2SpecIntensity.Name = "_lblLight2SpecIntensity";
            _lblLight2SpecIntensity.Size = new Size(80, 22);
            _lblLight2SpecIntensity.TabIndex = 47;
            _lblLight2SpecIntensity.Text = "Specular:";
            _lblLight2SpecIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _trackLight2SpecularIntensity
            // 
            _trackLight2SpecularIntensity.Location = new Point(365, 335);
            _trackLight2SpecularIntensity.Maximum = 100;
            _trackLight2SpecularIntensity.Name = "_trackLight2SpecularIntensity";
            _trackLight2SpecularIntensity.Size = new Size(130, 56);
            _trackLight2SpecularIntensity.TabIndex = 48;
            _trackLight2SpecularIntensity.TickFrequency = 10;
            _trackLight2SpecularIntensity.Value = 50;
            // 
            // _labelLight2SpecularIntensity
            // 
            _labelLight2SpecularIntensity.Location = new Point(506, 339);
            _labelLight2SpecularIntensity.Name = "_labelLight2SpecularIntensity";
            _labelLight2SpecularIntensity.Size = new Size(45, 22);
            _labelLight2SpecularIntensity.TabIndex = 49;
            _labelLight2SpecularIntensity.Text = "0.50";
            _labelLight2SpecularIntensity.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _colorPreviewPanel
            // 
            _colorPreviewPanel.BackColor = Color.Black;
            _colorPreviewPanel.BorderStyle = BorderStyle.FixedSingle;
            _colorPreviewPanel.Location = new Point(232, 499);
            _colorPreviewPanel.Name = "_colorPreviewPanel";
            _colorPreviewPanel.Size = new Size(28, 28);
            _colorPreviewPanel.TabIndex = 50;
            // 
            // _labelPreview
            // 
            _labelPreview.Location = new Point(10, 499);
            _labelPreview.Name = "_labelPreview";
            _labelPreview.Size = new Size(100, 28);
            _labelPreview.TabIndex = 51;
            _labelPreview.Text = "Light Preview";
            _labelPreview.TextAlign = ContentAlignment.MiddleLeft;
            _labelPreview.Click += _labelPreview_Click;
            // 
            // _btnReset
            // 
            _btnReset.BackColor = Color.FromArgb(230, 230, 235);
            _btnReset.FlatStyle = FlatStyle.Flat;
            _btnReset.Location = new Point(10, 548);
            _btnReset.Name = "_btnReset";
            _btnReset.Size = new Size(85, 32);
            _btnReset.TabIndex = 52;
            _btnReset.Text = "↺ Reset";
            _btnReset.UseVisualStyleBackColor = false;
            // 
            // _btnApply
            // 
            _btnApply.BackColor = Color.FromArgb(0, 122, 204);
            _btnApply.FlatStyle = FlatStyle.Flat;
            _btnApply.ForeColor = Color.White;
            _btnApply.Location = new Point(175, 548);
            _btnApply.Name = "_btnApply";
            _btnApply.Size = new Size(85, 32);
            _btnApply.TabIndex = 53;
            _btnApply.Text = "Apply";
            _btnApply.UseVisualStyleBackColor = false;
            // 
            // label1
            // 
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            label1.ForeColor = Color.FromArgb(0, 122, 204);
            label1.Location = new Point(10, 251);
            label1.Name = "label1";
            label1.Size = new Size(200, 20);
            label1.TabIndex = 54;
            label1.Text = "▶ LIGHT 1 INTENSITY";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // LightingWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            ClientSize = new Size(554, 700);
            Controls.Add(_trackLight2AmbientIntensity);
            Controls.Add(_trackLight2DiffuseIntensity);
            Controls.Add(_trackLight2SpecularIntensity);
            Controls.Add(_sep2);
            Controls.Add(label1);
            Controls.Add(_trackLight2DirectionZ);
            Controls.Add(_trackLight2DirectionY);
            Controls.Add(_trackAmbientIntensity);
            Controls.Add(_lblIntensity);
            Controls.Add(_trackDiffuseIntensity);
            Controls.Add(_trackSpecularIntensity);
            Controls.Add(_sep1);
            Controls.Add(_trackDirectionZ);
            Controls.Add(_trackDirectionY);
            Controls.Add(_lblTitle);
            Controls.Add(_chkLightingEnabled);
            Controls.Add(_chkPerPixelLighting);
            Controls.Add(_lblLight1Group);
            Controls.Add(_lblX);
            Controls.Add(_trackDirectionX);
            Controls.Add(_labelDirectionX);
            Controls.Add(_lblY);
            Controls.Add(_labelDirectionY);
            Controls.Add(_lblZ);
            Controls.Add(_labelDirectionZ);
            Controls.Add(_lblSpecPower);
            Controls.Add(_trackSpecularPower);
            Controls.Add(_labelSpecularPower);
            Controls.Add(_lblSpecIntensity);
            Controls.Add(_labelSpecularIntensity);
            Controls.Add(_lblDiffIntensity);
            Controls.Add(_labelDiffuseIntensity);
            Controls.Add(_lblAmbIntensity);
            Controls.Add(_labelAmbientIntensity);
            Controls.Add(_lblLight2Group);
            Controls.Add(_chkLight2Enabled);
            Controls.Add(_lblLight2X);
            Controls.Add(_trackLight2DirectionX);
            Controls.Add(_labelLight2DirectionX);
            Controls.Add(_lblLight2Y);
            Controls.Add(_labelLight2DirectionY);
            Controls.Add(_lblLight2Z);
            Controls.Add(_labelLight2DirectionZ);
            Controls.Add(_lblLight2DiffIntensity);
            Controls.Add(_labelLight2DiffuseIntensity);
            Controls.Add(_lblLight2AmbIntensity);
            Controls.Add(_labelLight2AmbientIntensity);
            Controls.Add(_lblLight2SpecIntensity);
            Controls.Add(_labelLight2SpecularIntensity);
            Controls.Add(_colorPreviewPanel);
            Controls.Add(_labelPreview);
            Controls.Add(_btnReset);
            Controls.Add(_btnApply);
            Font = new Font("Segoe UI", 8.25F);
            MinimumSize = new Size(320, 450);
            Name = "LightingWindow";
            Text = "⚡ Lighting Settings";
            Load += LightingWindow_Load;
            ((System.ComponentModel.ISupportInitialize)_trackDirectionX).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackDirectionY).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackDirectionZ).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackSpecularPower).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackSpecularIntensity).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackDiffuseIntensity).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackAmbientIntensity).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackLight2DirectionX).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackLight2DirectionY).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackLight2DirectionZ).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackLight2DiffuseIntensity).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackLight2AmbientIntensity).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackLight2SpecularIntensity).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        private Label label1;
    }
}