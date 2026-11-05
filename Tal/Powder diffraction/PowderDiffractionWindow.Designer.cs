// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Afterwder diffraction module: AfterwderDiffractionWindow.Designer.

﻿namespace Tal.Forms
{
    partial class PowderDiffractionWindow
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            if (disposing && _plotBitmap != null)
            {
                _plotBitmap.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            panelControls = new Panel();
            groupBoxRange = new GroupBox();
            labelMinTheta = new Label();
            numericMinTheta = new NumericUpDown();
            labelMaxTheta = new Label();
            numericMaxTheta = new NumericUpDown();
            groupBoxPeak = new GroupBox();
            labelResolution = new Label();
            numericResolution = new NumericUpDown();
            labelFWHM = new Label();
            numericFWHM = new NumericUpDown();
            labelProfile = new Label();
            comboProfile = new ComboBox();
            groupBoxDebye = new GroupBox();
            labelDebyeWaller = new Label();
            numericDebyeWaller = new NumericUpDown();
            trackDebyeWaller = new TrackBar();
            groupBoxSource = new GroupBox();
            labelSource = new Label();
            comboSource = new ComboBox();
            groupBoxDisplay = new GroupBox();
            chkShowDScale = new CheckBox();
            chkShowBackground = new CheckBox();
            chkLPCorrection = new CheckBox();
            groupBoxHover = new GroupBox();
            labelStatus = new Label();
            labelPeakCount = new Label();
            btnExport = new Button();
            btnCopy = new Button();
            panelPlot = new BufferedPanel();
            labelHoverDetail = new Label();
            labelHoverInfo = new Label();
            toolTip = new ToolTip(components);
            panelControls.SuspendLayout();
            groupBoxRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericMinTheta).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericMaxTheta).BeginInit();
            groupBoxPeak.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericResolution).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericFWHM).BeginInit();
            groupBoxDebye.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericDebyeWaller).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackDebyeWaller).BeginInit();
            groupBoxSource.SuspendLayout();
            groupBoxDisplay.SuspendLayout();
            groupBoxHover.SuspendLayout();
            panelPlot.SuspendLayout();
            SuspendLayout();
 // 
 // panelControls
 // 
            panelControls.BackColor = SystemColors.Control;
            panelControls.Controls.Add(groupBoxRange);
            panelControls.Controls.Add(groupBoxPeak);
            panelControls.Controls.Add(groupBoxDebye);
            panelControls.Controls.Add(groupBoxSource);
            panelControls.Controls.Add(groupBoxDisplay);
            panelControls.Controls.Add(groupBoxHover);
            panelControls.Controls.Add(btnExport);
            panelControls.Controls.Add(btnCopy);
            panelControls.Dock = DockStyle.Bottom;
            panelControls.Location = new Point(0, 358);
            panelControls.Margin = new Padding(3, 4, 3, 4);
            panelControls.Name = "panelControls";
            panelControls.Size = new Size(1307, 150);
            panelControls.TabIndex = 1;
 // 
 // groupBoxRange
 // 
            groupBoxRange.Controls.Add(labelMinTheta);
            groupBoxRange.Controls.Add(numericMinTheta);
            groupBoxRange.Controls.Add(labelMaxTheta);
            groupBoxRange.Controls.Add(numericMaxTheta);
            groupBoxRange.Location = new Point(12, 8);
            groupBoxRange.Margin = new Padding(3, 4, 3, 4);
            groupBoxRange.Name = "groupBoxRange";
            groupBoxRange.Padding = new Padding(3, 4, 3, 4);
            groupBoxRange.Size = new Size(250, 80);
            groupBoxRange.TabIndex = 0;
            groupBoxRange.TabStop = false;
            groupBoxRange.Text = "Range 2θ";
 // 
 // labelMinTheta
 // 
            labelMinTheta.Location = new Point(8, 26);
            labelMinTheta.Name = "labelMinTheta";
            labelMinTheta.Size = new Size(51, 25);
            labelMinTheta.TabIndex = 0;
            labelMinTheta.Text = "Min:";
 // 
 // numericMinTheta
 // 
            numericMinTheta.DecimalPlaces = 1;
            numericMinTheta.Location = new Point(65, 24);
            numericMinTheta.Margin = new Padding(3, 4, 3, 4);
            numericMinTheta.Maximum = new decimal(new int[] { 90, 0, 0, 0 });
            numericMinTheta.Name = "numericMinTheta";
            numericMinTheta.Size = new Size(64, 27);
            numericMinTheta.TabIndex = 1;
            numericMinTheta.Value = new decimal(new int[] { 5, 0, 0, 0 });
 // 
 // labelMaxTheta
 // 
            labelMaxTheta.Location = new Point(139, 27);
            labelMaxTheta.Name = "labelMaxTheta";
            labelMaxTheta.Size = new Size(44, 25);
            labelMaxTheta.TabIndex = 2;
            labelMaxTheta.Text = "Max:";
 // 
 // numericMaxTheta
 // 
            numericMaxTheta.DecimalPlaces = 1;
            numericMaxTheta.Location = new Point(189, 24);
            numericMaxTheta.Margin = new Padding(3, 4, 3, 4);
            numericMaxTheta.Maximum = new decimal(new int[] { 180, 0, 0, 0 });
            numericMaxTheta.Name = "numericMaxTheta";
            numericMaxTheta.Size = new Size(55, 27);
            numericMaxTheta.TabIndex = 3;
            numericMaxTheta.Value = new decimal(new int[] { 90, 0, 0, 0 });
 // 
 // groupBoxPeak
 // 
            groupBoxPeak.Controls.Add(labelResolution);
            groupBoxPeak.Controls.Add(numericResolution);
            groupBoxPeak.Controls.Add(labelFWHM);
            groupBoxPeak.Controls.Add(numericFWHM);
            groupBoxPeak.Controls.Add(labelProfile);
            groupBoxPeak.Controls.Add(comboProfile);
            groupBoxPeak.Location = new Point(268, 9);
            groupBoxPeak.Margin = new Padding(3, 4, 3, 4);
            groupBoxPeak.Name = "groupBoxPeak";
            groupBoxPeak.Padding = new Padding(3, 4, 3, 4);
            groupBoxPeak.Size = new Size(240, 135);
            groupBoxPeak.TabIndex = 1;
            groupBoxPeak.TabStop = false;
            groupBoxPeak.Text = "Peak parameters";
 // 
 // labelResolution
 // 
            labelResolution.Location = new Point(8, 100);
            labelResolution.Name = "labelResolution";
            labelResolution.Size = new Size(92, 25);
            labelResolution.TabIndex = 4;
            labelResolution.Text = "Resolution:";
 // 
 // numericResolution
 // 
            numericResolution.Location = new Point(123, 98);
            numericResolution.Margin = new Padding(3, 4, 3, 4);
            numericResolution.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numericResolution.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericResolution.Name = "numericResolution";
            numericResolution.Size = new Size(55, 27);
            numericResolution.TabIndex = 5;
            numericResolution.Value = new decimal(new int[] { 2, 0, 0, 0 });
 // 
 // labelFWHM
 // 
            labelFWHM.Location = new Point(8, 26);
            labelFWHM.Name = "labelFWHM";
            labelFWHM.Size = new Size(55, 25);
            labelFWHM.TabIndex = 0;
            labelFWHM.Text = "FWHM:";
 // 
 // numericFWHM
 // 
            numericFWHM.DecimalPlaces = 2;
            numericFWHM.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numericFWHM.Location = new Point(70, 23);
            numericFWHM.Margin = new Padding(3, 4, 3, 4);
            numericFWHM.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numericFWHM.Minimum = new decimal(new int[] { 1, 0, 0, 131072 });
            numericFWHM.Name = "numericFWHM";
            numericFWHM.Size = new Size(83, 27);
            numericFWHM.TabIndex = 1;
            numericFWHM.Value = new decimal(new int[] { 5, 0, 0, 65536 });
 // 
 // labelProfile
 // 
            labelProfile.Location = new Point(8, 62);
            labelProfile.Name = "labelProfile";
            labelProfile.Size = new Size(55, 25);
            labelProfile.TabIndex = 2;
            labelProfile.Text = "Profile:";
 // 
 // comboProfile
 // 
            comboProfile.DropDownStyle = ComboBoxStyle.DropDownList;
            comboProfile.Items.AddRange(new object[] { "Gaussian", "Lorentzian", "Pseudo-Voigt" });
            comboProfile.Location = new Point(70, 60);
            comboProfile.Margin = new Padding(3, 4, 3, 4);
            comboProfile.Name = "comboProfile";
            comboProfile.Size = new Size(160, 28);
            comboProfile.TabIndex = 3;
 // 
 // groupBoxDebye
 // 
            groupBoxDebye.Controls.Add(labelDebyeWaller);
            groupBoxDebye.Controls.Add(numericDebyeWaller);
            groupBoxDebye.Controls.Add(trackDebyeWaller);
            groupBoxDebye.Location = new Point(547, 8);
            groupBoxDebye.Margin = new Padding(3, 4, 3, 4);
            groupBoxDebye.Name = "groupBoxDebye";
            groupBoxDebye.Padding = new Padding(3, 4, 3, 4);
            groupBoxDebye.Size = new Size(210, 135);
            groupBoxDebye.TabIndex = 2;
            groupBoxDebye.TabStop = false;
            groupBoxDebye.Text = "Debye-Waller (B)";
 // 
 // labelDebyeWaller
 // 
            labelDebyeWaller.Location = new Point(8, 33);
            labelDebyeWaller.Name = "labelDebyeWaller";
            labelDebyeWaller.Size = new Size(55, 25);
            labelDebyeWaller.TabIndex = 0;
            labelDebyeWaller.Text = "B (Å²):";
 // 
 // numericDebyeWaller
 // 
            numericDebyeWaller.DecimalPlaces = 2;
            numericDebyeWaller.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            numericDebyeWaller.Location = new Point(69, 33);
            numericDebyeWaller.Margin = new Padding(3, 4, 3, 4);
            numericDebyeWaller.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            numericDebyeWaller.Name = "numericDebyeWaller";
            numericDebyeWaller.Size = new Size(76, 27);
            numericDebyeWaller.TabIndex = 1;
            numericDebyeWaller.Value = new decimal(new int[] { 5, 0, 0, 65536 });
            numericDebyeWaller.ValueChanged += NumericDebyeWaller_ValueChanged;
 // 
 // trackDebyeWaller
 // 
            trackDebyeWaller.Location = new Point(8, 68);
            trackDebyeWaller.Margin = new Padding(3, 4, 3, 4);
            trackDebyeWaller.Maximum = 200;
            trackDebyeWaller.Name = "trackDebyeWaller";
            trackDebyeWaller.Size = new Size(190, 56);
            trackDebyeWaller.TabIndex = 2;
            trackDebyeWaller.TickFrequency = 10;
            trackDebyeWaller.Value = 5;
            trackDebyeWaller.Scroll += TrackDebyeWaller_Scroll;
 // 
 // groupBoxSource
 // 
            groupBoxSource.Controls.Add(labelSource);
            groupBoxSource.Controls.Add(comboSource);
            groupBoxSource.Location = new Point(763, 8);
            groupBoxSource.Margin = new Padding(3, 4, 3, 4);
            groupBoxSource.Name = "groupBoxSource";
            groupBoxSource.Padding = new Padding(3, 4, 3, 4);
            groupBoxSource.Size = new Size(233, 60);
            groupBoxSource.TabIndex = 3;
            groupBoxSource.TabStop = false;
            groupBoxSource.Text = "Radiation source";
 // 
 // labelSource
 // 
            labelSource.Location = new Point(8, 26);
            labelSource.Name = "labelSource";
            labelSource.Size = new Size(50, 25);
            labelSource.TabIndex = 0;
            labelSource.Text = "Source:";
 // 
 // comboSource
 // 
            comboSource.DropDownStyle = ComboBoxStyle.DropDownList;
            comboSource.Items.AddRange(new object[] { "Cu Kα (1.5406 Å)", "Mo Kα (0.7107 Å)", "Co Kα (1.7889 Å)", "Fe Kα (1.9360 Å)", "Cr Kα (2.2910 Å)", "Ag Kα (0.5609 Å)" });
            comboSource.Location = new Point(64, 24);
            comboSource.Margin = new Padding(3, 4, 3, 4);
            comboSource.Name = "comboSource";
            comboSource.Size = new Size(162, 28);
            comboSource.TabIndex = 1;
 // 
 // groupBoxDisplay
 // 
            groupBoxDisplay.Controls.Add(chkShowDScale);
            groupBoxDisplay.Controls.Add(chkShowBackground);
            groupBoxDisplay.Controls.Add(chkLPCorrection);
            groupBoxDisplay.Location = new Point(763, 76);
            groupBoxDisplay.Margin = new Padding(3, 4, 3, 4);
            groupBoxDisplay.Name = "groupBoxDisplay";
            groupBoxDisplay.Padding = new Padding(3, 4, 3, 4);
            groupBoxDisplay.Size = new Size(233, 68);
            groupBoxDisplay.TabIndex = 4;
            groupBoxDisplay.TabStop = false;
            groupBoxDisplay.Text = "Display options";
 // 
 // chkShowDScale
 // 
            chkShowDScale.AutoSize = true;
            chkShowDScale.Location = new Point(8, 20);
            chkShowDScale.Margin = new Padding(3, 4, 3, 4);
            chkShowDScale.Name = "chkShowDScale";
            chkShowDScale.Size = new Size(117, 24);
            chkShowDScale.TabIndex = 2;
            chkShowDScale.Text = "Show d scale";
            chkShowDScale.UseVisualStyleBackColor = true;
 // 
 // chkShowBackground
 // 
            chkShowBackground.AutoSize = true;
            chkShowBackground.Checked = true;
            chkShowBackground.CheckState = CheckState.Checked;
            chkShowBackground.Location = new Point(8, 43);
            chkShowBackground.Margin = new Padding(3, 4, 3, 4);
            chkShowBackground.Name = "chkShowBackground";
            chkShowBackground.Size = new Size(110, 24);
            chkShowBackground.TabIndex = 0;
            chkShowBackground.Text = "Background";
            chkShowBackground.UseVisualStyleBackColor = true;
 // 
 // chkLPCorrection
 // 
            chkLPCorrection.AutoSize = true;
            chkLPCorrection.Checked = true;
            chkLPCorrection.CheckState = CheckState.Checked;
            chkLPCorrection.Location = new Point(147, 20);
            chkLPCorrection.Margin = new Padding(3, 4, 3, 4);
            chkLPCorrection.Name = "chkLPCorrection";
            chkLPCorrection.Size = new Size(79, 24);
            chkLPCorrection.TabIndex = 1;
            chkLPCorrection.Text = "LP corr.";
            chkLPCorrection.UseVisualStyleBackColor = true;
 // 
 // groupBoxHover
 // 
            groupBoxHover.Controls.Add(labelStatus);
            groupBoxHover.Controls.Add(labelPeakCount);
            groupBoxHover.Location = new Point(1002, 9);
            groupBoxHover.Margin = new Padding(3, 4, 3, 4);
            groupBoxHover.Name = "groupBoxHover";
            groupBoxHover.Padding = new Padding(3, 4, 3, 4);
            groupBoxHover.Size = new Size(170, 135);
            groupBoxHover.TabIndex = 5;
            groupBoxHover.TabStop = false;
            groupBoxHover.Text = "Info";
 // 
 // labelStatus
 // 
            labelStatus.Font = new Font("Segoe UI", 8F);
            labelStatus.ForeColor = Color.Gray;
            labelStatus.Location = new Point(8, 24);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(155, 24);
            labelStatus.TabIndex = 0;
            labelStatus.Text = "Ready";
 // 
 // labelPeakCount
 // 
            labelPeakCount.Font = new Font("Segoe UI", 8F);
            labelPeakCount.ForeColor = Color.Gray;
            labelPeakCount.Location = new Point(8, 52);
            labelPeakCount.Name = "labelPeakCount";
            labelPeakCount.Size = new Size(155, 24);
            labelPeakCount.TabIndex = 1;
            labelPeakCount.Text = "Peaks: 0";
 // 
 // btnExafterrt
 // 
            btnExport.BackColor = Color.FromArgb(60, 60, 70);
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.ForeColor = Color.White;
            btnExport.Location = new Point(1186, 47);
            btnExport.Margin = new Padding(3, 4, 3, 4);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(90, 31);
            btnExport.TabIndex = 6;
            btnExport.Text = "📁 Export";
            btnExport.UseVisualStyleBackColor = false;
 // 
 // btnCopy
 // 
            btnCopy.BackColor = Color.FromArgb(60, 60, 70);
            btnCopy.FlatStyle = FlatStyle.Flat;
            btnCopy.ForeColor = Color.White;
            btnCopy.Location = new Point(1186, 8);
            btnCopy.Margin = new Padding(3, 4, 3, 4);
            btnCopy.Name = "btnCopy";
            btnCopy.Size = new Size(90, 31);
            btnCopy.TabIndex = 7;
            btnCopy.Text = "📋 Copy";
            btnCopy.UseVisualStyleBackColor = false;
 // 
 // panelPlot
 // 
            panelPlot.BackColor = Color.White;
            panelPlot.Controls.Add(labelHoverDetail);
            panelPlot.Controls.Add(labelHoverInfo);
            panelPlot.Dock = DockStyle.Fill;
            panelPlot.Location = new Point(0, 0);
            panelPlot.Margin = new Padding(3, 4, 3, 4);
            panelPlot.Name = "panelPlot";
            panelPlot.Size = new Size(1307, 358);
            panelPlot.TabIndex = 0;
            panelPlot.Paint += PanelPlot_Paint;
            panelPlot.MouseDown += PanelPlot_MouseDown;
            panelPlot.MouseLeave += PanelPlot_MouseLeave;
            panelPlot.MouseMove += PanelPlot_MouseMove;
            panelPlot.MouseUp += PanelPlot_MouseUp;
            panelPlot.Resize += PanelPlot_Resize;
 // 
 // labelHoverDetail
 // 
            labelHoverDetail.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            labelHoverDetail.Font = new Font("Segoe UI", 8.5F);
            labelHoverDetail.ForeColor = Color.FromArgb(60, 60, 70);
            labelHoverDetail.Location = new Point(910, 12);
            labelHoverDetail.Name = "labelHoverDetail";
            labelHoverDetail.Size = new Size(366, 29);
            labelHoverDetail.TabIndex = 0;
            labelHoverDetail.Text = "d: - | hkl: -";
 // 
 // labelHoverInfo
 // 
            labelHoverInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            labelHoverInfo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            labelHoverInfo.ForeColor = Color.FromArgb(0, 120, 212);
            labelHoverInfo.Location = new Point(557, 11);
            labelHoverInfo.Name = "labelHoverInfo";
            labelHoverInfo.Size = new Size(135, 29);
            labelHoverInfo.TabIndex = 1;
            labelHoverInfo.Text = "2θ: -";
 // 
 // AfterwderDiffractionWindow
 // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1307, 508);
            Controls.Add(panelPlot);
            Controls.Add(panelControls);
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new Size(912, 350);
            Name = "PowderDiffractionWindow";
            Text = "Powder Diffraction";
            FormClosing += PowderDiffractionWindow_FormClosing;
            panelControls.ResumeLayout(false);
            groupBoxRange.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numericMinTheta).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericMaxTheta).EndInit();
            groupBoxPeak.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numericResolution).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericFWHM).EndInit();
            groupBoxDebye.ResumeLayout(false);
            groupBoxDebye.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericDebyeWaller).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackDebyeWaller).EndInit();
            groupBoxSource.ResumeLayout(false);
            groupBoxDisplay.ResumeLayout(false);
            groupBoxDisplay.PerformLayout();
            groupBoxHover.ResumeLayout(false);
            panelPlot.ResumeLayout(false);
            ResumeLayout(false);
        }

 // Deklaracje kontrolek
        private BufferedPanel panelPlot;
        private System.Windows.Forms.Panel panelControls;

        private System.Windows.Forms.GroupBox groupBoxRange;
        private System.Windows.Forms.Label labelMinTheta;
        private System.Windows.Forms.Label labelMaxTheta;
        private System.Windows.Forms.NumericUpDown numericMinTheta;
        private System.Windows.Forms.NumericUpDown numericMaxTheta;

        private System.Windows.Forms.GroupBox groupBoxPeak;
        private System.Windows.Forms.Label labelResolution;
        private System.Windows.Forms.NumericUpDown numericResolution;
        private System.Windows.Forms.Label labelFWHM;
        private System.Windows.Forms.NumericUpDown numericFWHM;
        private System.Windows.Forms.Label labelProfile;
        private System.Windows.Forms.ComboBox comboProfile;

        private System.Windows.Forms.GroupBox groupBoxDebye;
        private System.Windows.Forms.Label labelDebyeWaller;
        private System.Windows.Forms.NumericUpDown numericDebyeWaller;
        private System.Windows.Forms.TrackBar trackDebyeWaller;

        private System.Windows.Forms.GroupBox groupBoxSource;
        private System.Windows.Forms.Label labelSource;
        private System.Windows.Forms.ComboBox comboSource;

        private System.Windows.Forms.GroupBox groupBoxDisplay;
        private System.Windows.Forms.CheckBox chkShowDScale;
        private System.Windows.Forms.CheckBox chkShowBackground;
        private System.Windows.Forms.CheckBox chkLPCorrection;

        private System.Windows.Forms.GroupBox groupBoxHover;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label labelPeakCount;

        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnCopy;

        private System.Windows.Forms.Label labelHoverInfo;
        private System.Windows.Forms.Label labelHoverDetail;
        private System.Windows.Forms.ToolTip toolTip;
    }
}