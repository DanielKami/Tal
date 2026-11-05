// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// CavitySettingsWindow designer-generated layout.

﻿namespace Tal.Forms
{
    partial class CavitySettingsWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            chkEnableCavity = new CheckBox();
            grpDetection = new GroupBox();
            numVdwMultiplier = new NumericUpDown();
            lblVdwMultiplier = new Label();
            numResolution = new NumericUpDown();
            chkUseThermalMotion = new CheckBox();
            lblResolution = new Label();
            btnClearCavities = new Button();
            btnDetectCavities = new Button();
            numSmoothing = new NumericUpDown();
            lblSmoothing = new Label();
            lblTransparencyValue = new Label();
            trackTransparency = new TrackBar();
            lblTransparency = new Label();
            grpDisplayMode = new GroupBox();
            cmbPropertyType = new ComboBox();
            rbSphereFits = new RadioButton();
            rbChannelPaths = new RadioButton();
            rbTopology = new RadioButton();
            rbPropertyMap = new RadioButton();
            rbNormal = new RadioButton();
            rbHidden = new RadioButton();
            btnExportReport = new Button();
            lblStatus = new Label();
            lblCavityCount = new Label();
            grpCavityList = new GroupBox();
            lstCavityVisibility = new CheckedListBox();
            btnCavitiesAll = new Button();
            btnCavitiesNone = new Button();
            grpDetection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numVdwMultiplier).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numResolution).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numSmoothing).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackTransparency).BeginInit();
            grpDisplayMode.SuspendLayout();
            grpCavityList.SuspendLayout();
            SuspendLayout();
            // 
            // chkEnableCavity
            // 
            chkEnableCavity.AutoSize = true;
            chkEnableCavity.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            chkEnableCavity.Location = new Point(12, 12);
            chkEnableCavity.Name = "chkEnableCavity";
            chkEnableCavity.Size = new Size(204, 27);
            chkEnableCavity.TabIndex = 0;
            chkEnableCavity.Text = "Enable Cavity Display";
            chkEnableCavity.UseVisualStyleBackColor = true;
            chkEnableCavity.CheckedChanged += ChkEnableCavity_CheckedChanged;
            // 
            // grpDetection
            // 
            grpDetection.Controls.Add(numVdwMultiplier);
            grpDetection.Controls.Add(lblVdwMultiplier);
            grpDetection.Controls.Add(numResolution);
            grpDetection.Controls.Add(chkUseThermalMotion);
            grpDetection.Controls.Add(lblResolution);
            grpDetection.Controls.Add(btnClearCavities);
            grpDetection.Controls.Add(btnDetectCavities);
            grpDetection.Location = new Point(12, 41);
            grpDetection.Name = "grpDetection";
            grpDetection.Size = new Size(301, 195);
            grpDetection.TabIndex = 1;
            grpDetection.TabStop = false;
            grpDetection.Text = "🔬 Detection Parameters";
            // 
            // numVdwMultiplier
            // 
            numVdwMultiplier.DecimalPlaces = 2;
            numVdwMultiplier.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numVdwMultiplier.Location = new Point(140, 72);
            numVdwMultiplier.Maximum = new decimal(new int[] { 30, 0, 0, 65536 });
            numVdwMultiplier.Minimum = new decimal(new int[] { 8, 0, 0, 131072 });
            numVdwMultiplier.Name = "numVdwMultiplier";
            numVdwMultiplier.Size = new Size(80, 27);
            numVdwMultiplier.TabIndex = 3;
            numVdwMultiplier.Value = new decimal(new int[] { 10, 0, 0, 65536 });
            numVdwMultiplier.ValueChanged += NumVdwMultiplier_ValueChanged;
            // 
            // lblVdwMultiplier
            // 
            lblVdwMultiplier.AutoSize = true;
            lblVdwMultiplier.Location = new Point(10, 74);
            lblVdwMultiplier.Name = "lblVdwMultiplier";
            lblVdwMultiplier.Size = new Size(111, 20);
            lblVdwMultiplier.TabIndex = 2;
            lblVdwMultiplier.Text = "VdW Multiplier:";
            // 
            // numResolution
            // 
            numResolution.DecimalPlaces = 2;
            numResolution.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numResolution.Location = new Point(140, 40);
            numResolution.Maximum = new decimal(new int[] { 20, 0, 0, 131072 });
            numResolution.Minimum = new decimal(new int[] { 5, 0, 0, 131072 });
            numResolution.Name = "numResolution";
            numResolution.Size = new Size(80, 27);
            numResolution.TabIndex = 1;
            numResolution.Value = new decimal(new int[] { 15, 0, 0, 131072 });
            numResolution.ValueChanged += NumResolution_ValueChanged;
            // 
            // chkUseAnisotropicVdW
            // 
            chkUseThermalMotion.AutoSize = true;
            chkUseThermalMotion.Location = new Point(10, 119);
            chkUseThermalMotion.Name = "chkUseThermalMotion";
            chkUseThermalMotion.Size = new Size(247, 24);
            chkUseThermalMotion.TabIndex = 9;
            chkUseThermalMotion.Text = "Use thermal motion (Uij/Uiso)";
            chkUseThermalMotion.UseVisualStyleBackColor = true;
            chkUseThermalMotion.CheckedChanged += ChkUseThermalMotion_CheckedChanged;
            // 
            // lblResolution
            // 
            lblResolution.AutoSize = true;
            lblResolution.Location = new Point(10, 42);
            lblResolution.Name = "lblResolution";
            lblResolution.Size = new Size(106, 20);
            lblResolution.TabIndex = 0;
            lblResolution.Text = "Resolution (Å):";
            // 
            // btnClearCavities
            // 
            btnClearCavities.BackColor = Color.FromArgb(200, 50, 50);
            btnClearCavities.FlatStyle = FlatStyle.Flat;
            btnClearCavities.ForeColor = Color.White;
            btnClearCavities.Location = new Point(155, 158);
            btnClearCavities.Name = "btnClearCavities";
            btnClearCavities.Size = new Size(135, 30);
            btnClearCavities.TabIndex = 11;
            btnClearCavities.Text = "\U0001f9f9 Clear Cavities";
            btnClearCavities.UseVisualStyleBackColor = false;
            btnClearCavities.Click += BtnClearCavities_Click;
            // 
            // btnDetectCavities
            // 
            btnDetectCavities.BackColor = Color.FromArgb(0, 120, 215);
            btnDetectCavities.FlatStyle = FlatStyle.Flat;
            btnDetectCavities.ForeColor = Color.White;
            btnDetectCavities.Location = new Point(10, 158);
            btnDetectCavities.Name = "btnDetectCavities";
            btnDetectCavities.Size = new Size(135, 30);
            btnDetectCavities.TabIndex = 10;
            btnDetectCavities.Text = "🔬 Detect Cavities";
            btnDetectCavities.UseVisualStyleBackColor = false;
            btnDetectCavities.Click += BtnDetectCavities_Click;
            // 
            // numSmoothing
            // 
            numSmoothing.Location = new Point(125, 71);
            numSmoothing.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numSmoothing.Name = "numSmoothing";
            numSmoothing.Size = new Size(80, 27);
            numSmoothing.TabIndex = 8;
            numSmoothing.Value = new decimal(new int[] { 3, 0, 0, 0 });
            numSmoothing.ValueChanged += NumSmoothing_ValueChanged;
            // 
            // lblSmoothing
            // 
            lblSmoothing.AutoSize = true;
            lblSmoothing.Location = new Point(10, 73);
            lblSmoothing.Name = "lblSmoothing";
            lblSmoothing.Size = new Size(85, 20);
            lblSmoothing.TabIndex = 7;
            lblSmoothing.Text = "Smoothing:";
            // 
            // lblTransparencyValue
            // 
            lblTransparencyValue.AutoSize = true;
            lblTransparencyValue.Location = new Point(235, 26);
            lblTransparencyValue.Name = "lblTransparencyValue";
            lblTransparencyValue.Size = new Size(28, 20);
            lblTransparencyValue.TabIndex = 6;
            lblTransparencyValue.Text = "0.5";
            // 
            // trackTransparency
            // 
            trackTransparency.Location = new Point(125, 26);
            trackTransparency.Name = "trackTransparency";
            trackTransparency.Size = new Size(104, 56);
            trackTransparency.TabIndex = 5;
            trackTransparency.Value = 5;
            trackTransparency.ValueChanged += TrackTransparency_ValueChanged;
            // 
            // lblTransparency
            // 
            lblTransparency.AutoSize = true;
            lblTransparency.Location = new Point(10, 26);
            lblTransparency.Name = "lblTransparency";
            lblTransparency.Size = new Size(98, 20);
            lblTransparency.TabIndex = 4;
            lblTransparency.Text = "Transparency:";
            // 
            // grpDisplayMode
            // 
            grpDisplayMode.Controls.Add(numSmoothing);
            grpDisplayMode.Controls.Add(lblTransparencyValue);
            grpDisplayMode.Controls.Add(cmbPropertyType);
            grpDisplayMode.Controls.Add(trackTransparency);
            grpDisplayMode.Controls.Add(lblTransparency);
            grpDisplayMode.Controls.Add(rbSphereFits);
            grpDisplayMode.Controls.Add(rbChannelPaths);
            grpDisplayMode.Controls.Add(rbTopology);
            grpDisplayMode.Controls.Add(lblSmoothing);
            grpDisplayMode.Controls.Add(rbPropertyMap);
            grpDisplayMode.Controls.Add(rbNormal);
            grpDisplayMode.Controls.Add(rbHidden);
            grpDisplayMode.Location = new Point(12, 242);
            grpDisplayMode.Name = "grpDisplayMode";
            grpDisplayMode.Size = new Size(301, 286);
            grpDisplayMode.TabIndex = 2;
            grpDisplayMode.TabStop = false;
            grpDisplayMode.Text = "🎨 Display Mode";
            // 
            // cmbPropertyType
            // 
            cmbPropertyType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPropertyType.Enabled = false;
            cmbPropertyType.Items.AddRange(new object[] { "Charge", "Hydrophobicity", "Atomic Number", "VdW Radius", "Occupancy" });
            cmbPropertyType.Location = new Point(160, 244);
            cmbPropertyType.Name = "cmbPropertyType";
            cmbPropertyType.Size = new Size(130, 28);
            cmbPropertyType.TabIndex = 6;
            cmbPropertyType.SelectedIndexChanged += CmbPropertyType_SelectedIndexChanged;
            // 
            // rbSphereFits
            // 
            rbSphereFits.AutoSize = true;
            rbSphereFits.Location = new Point(10, 246);
            rbSphereFits.Name = "rbSphereFits";
            rbSphereFits.Size = new Size(102, 24);
            rbSphereFits.TabIndex = 5;
            rbSphereFits.Text = "Sphere Fits";
            rbSphereFits.UseVisualStyleBackColor = true;
            rbSphereFits.CheckedChanged += RbDisplayMode_CheckedChanged;
            // 
            // rbChannelPaths
            // 
            rbChannelPaths.AutoSize = true;
            rbChannelPaths.Location = new Point(10, 220);
            rbChannelPaths.Name = "rbChannelPaths";
            rbChannelPaths.Size = new Size(121, 24);
            rbChannelPaths.TabIndex = 4;
            rbChannelPaths.Text = "Channel Paths";
            rbChannelPaths.UseVisualStyleBackColor = true;
            rbChannelPaths.CheckedChanged += RbDisplayMode_CheckedChanged;
            // 
            // rbTopology
            // 
            rbTopology.AutoSize = true;
            rbTopology.Location = new Point(10, 194);
            rbTopology.Name = "rbTopology";
            rbTopology.Size = new Size(127, 24);
            rbTopology.TabIndex = 3;
            rbTopology.Text = "Topology Map";
            rbTopology.UseVisualStyleBackColor = true;
            rbTopology.CheckedChanged += RbDisplayMode_CheckedChanged;
            // 
            // rbPropertyMap
            // 
            rbPropertyMap.AutoSize = true;
            rbPropertyMap.Location = new Point(10, 168);
            rbPropertyMap.Name = "rbPropertyMap";
            rbPropertyMap.Size = new Size(120, 24);
            rbPropertyMap.TabIndex = 2;
            rbPropertyMap.Text = "Property Map";
            rbPropertyMap.UseVisualStyleBackColor = true;
            rbPropertyMap.CheckedChanged += RbDisplayMode_CheckedChanged;
            // 
            // rbNormal
            // 
            rbNormal.AutoSize = true;
            rbNormal.Checked = true;
            rbNormal.Location = new Point(10, 142);
            rbNormal.Name = "rbNormal";
            rbNormal.Size = new Size(124, 24);
            rbNormal.TabIndex = 1;
            rbNormal.TabStop = true;
            rbNormal.Text = "Normal Cavity";
            rbNormal.UseVisualStyleBackColor = true;
            rbNormal.CheckedChanged += RbDisplayMode_CheckedChanged;
            // 
            // rbHidden
            // 
            rbHidden.AutoSize = true;
            rbHidden.Location = new Point(10, 116);
            rbHidden.Name = "rbHidden";
            rbHidden.Size = new Size(79, 24);
            rbHidden.TabIndex = 0;
            rbHidden.Text = "Hidden";
            rbHidden.UseVisualStyleBackColor = true;
            rbHidden.CheckedChanged += RbDisplayMode_CheckedChanged;

            // 
            // grpCavityList
            // 
            grpCavityList.Controls.Add(lstCavityVisibility);
            grpCavityList.Controls.Add(btnCavitiesAll);
            grpCavityList.Controls.Add(btnCavitiesNone);
            grpCavityList.Location = new Point(12, 520);
            grpCavityList.Name = "grpCavityList";
            grpCavityList.Size = new Size(301, 160);
            grpCavityList.TabIndex = 8;
            grpCavityList.TabStop = false;
            grpCavityList.Text = "Detected cavities (show/hide)";
            // 
            // lstCavityVisibility
            // 
            lstCavityVisibility.CheckOnClick = true;
            lstCavityVisibility.FormattingEnabled = true;
            lstCavityVisibility.IntegralHeight = false;
            lstCavityVisibility.Location = new Point(10, 26);
            lstCavityVisibility.Name = "lstCavityVisibility";
            lstCavityVisibility.Size = new Size(281, 96);
            lstCavityVisibility.TabIndex = 0;
            lstCavityVisibility.ItemCheck += LstCavityVisibility_ItemCheck;
            // 
            // btnCavitiesAll
            // 
            btnCavitiesAll.Location = new Point(10, 126);
            btnCavitiesAll.Name = "btnCavitiesAll";
            btnCavitiesAll.Size = new Size(130, 26);
            btnCavitiesAll.TabIndex = 1;
            btnCavitiesAll.Text = "Show all";
            btnCavitiesAll.UseVisualStyleBackColor = true;
            btnCavitiesAll.Click += BtnCavitiesAll_Click;
            // 
            // btnCavitiesNone
            // 
            btnCavitiesNone.Location = new Point(161, 126);
            btnCavitiesNone.Name = "btnCavitiesNone";
            btnCavitiesNone.Size = new Size(130, 26);
            btnCavitiesNone.TabIndex = 2;
            btnCavitiesNone.Text = "Hide all";
            btnCavitiesNone.UseVisualStyleBackColor = true;
            btnCavitiesNone.Click += BtnCavitiesNone_Click;
            // 
            // 
            // btnExportReport
            // 
            btnExportReport.Location = new Point(12, 688);
            btnExportReport.Name = "btnExportReport";
            btnExportReport.Size = new Size(301, 30);
            btnExportReport.TabIndex = 5;
            btnExportReport.Text = "📊 Export Report";
            btnExportReport.UseVisualStyleBackColor = true;
            btnExportReport.Click += BtnExportReport_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.ForeColor = Color.Gray;
            lblStatus.Location = new Point(12, 726);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(50, 20);
            lblStatus.TabIndex = 6;
            lblStatus.Text = "Ready";
            // 
            // lblCavityCount
            // 
            lblCavityCount.AutoSize = true;
            lblCavityCount.ForeColor = Color.Gray;
            lblCavityCount.Location = new Point(227, 726);
            lblCavityCount.Name = "lblCavityCount";
            lblCavityCount.Size = new Size(75, 20);
            lblCavityCount.TabIndex = 7;
            lblCavityCount.Text = "Cavities: 0";
            lblCavityCount.TextAlign = ContentAlignment.MiddleRight;
            // 
            // CavitySettingsWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(248, 248, 250);
            ClientSize = new Size(340, 760);
            Controls.Add(lblCavityCount);
            Controls.Add(lblStatus);
            Controls.Add(btnExportReport);
            Controls.Add(grpCavityList);
            Controls.Add(grpDisplayMode);
            Controls.Add(grpDetection);
            Controls.Add(chkEnableCavity);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 238);
            MinimumSize = new Size(350, 720);
            Name = "CavitySettingsWindow";
            Text = "🕳️ Cavity Settings";
            grpDetection.ResumeLayout(false);
            grpDetection.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numVdwMultiplier).EndInit();
            ((System.ComponentModel.ISupportInitialize)numResolution).EndInit();
            ((System.ComponentModel.ISupportInitialize)numSmoothing).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackTransparency).EndInit();
            grpDisplayMode.ResumeLayout(false);
            grpDisplayMode.PerformLayout();
            grpCavityList.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.CheckBox chkEnableCavity;
        private System.Windows.Forms.GroupBox grpDetection;
        private System.Windows.Forms.Button btnClearCavities;
        private System.Windows.Forms.Button btnDetectCavities;
        private System.Windows.Forms.CheckBox chkUseThermalMotion;
        private System.Windows.Forms.NumericUpDown numSmoothing;
        private System.Windows.Forms.Label lblSmoothing;
        private System.Windows.Forms.Label lblTransparencyValue;
        private System.Windows.Forms.TrackBar trackTransparency;
        private System.Windows.Forms.Label lblTransparency;
        private System.Windows.Forms.NumericUpDown numVdwMultiplier;
        private System.Windows.Forms.Label lblVdwMultiplier;
        private System.Windows.Forms.NumericUpDown numResolution;
        private System.Windows.Forms.Label lblResolution;
        private System.Windows.Forms.GroupBox grpDisplayMode;
        private System.Windows.Forms.ComboBox cmbPropertyType;
        private System.Windows.Forms.RadioButton rbSphereFits;
        private System.Windows.Forms.RadioButton rbChannelPaths;
        private System.Windows.Forms.RadioButton rbTopology;
        private System.Windows.Forms.RadioButton rbPropertyMap;
        private System.Windows.Forms.RadioButton rbNormal;
        private System.Windows.Forms.RadioButton rbHidden;
        private System.Windows.Forms.Button btnExportReport;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblCavityCount;
        private System.Windows.Forms.GroupBox grpCavityList;
        private System.Windows.Forms.CheckedListBox lstCavityVisibility;
        private System.Windows.Forms.Button btnCavitiesAll;
        private System.Windows.Forms.Button btnCavitiesNone;
    }
}