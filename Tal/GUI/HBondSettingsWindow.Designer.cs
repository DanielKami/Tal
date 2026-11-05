// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// HBondSettingsWindow designer-generated layout.

namespace Tal.Forms
{
    partial class HBondSettingsWindow
    {
        private System.ComponentModel.IContainer components = null;

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
            grpDistance = new GroupBox();
            rbActualDistance = new RadioButton();
            rbVdWDistance = new RadioButton();
            lblActualMin = new Label();
            numActualMin = new NumericUpDown();
            lblActualMax = new Label();
            numActualMax = new NumericUpDown();
            lblVdWMinus = new Label();
            cmbVdWMinusOp = new ComboBox();
            numVdWMinus = new NumericUpDown();
            lblVdWPlus = new Label();
            cmbVdWPlusOp = new ComboBox();
            numVdWPlus = new NumericUpDown();
            lblVdWSum = new Label();
            grpAngle = new GroupBox();
            lblAngle = new Label();
            numAngle = new NumericUpDown();
            grpOptions = new GroupBox();
            chkShowHBonds = new CheckBox();
            chkIntramolecular = new CheckBox();
            chkIntermolecular = new CheckBox();
            chkDrawFromHydrogen = new CheckBox();
            chkRequireHydrogen = new CheckBox();
            grpDonorsAcceptors = new GroupBox();
            chkSelectAllDonors = new CheckBox();
            chkSelectAllAcceptors = new CheckBox();
            lstDonors = new CheckedListBox();
            lstAcceptors = new CheckedListBox();
            lblDonors = new Label();
            lblAcceptors = new Label();
            btnHBondColor = new Button();
            lblHBondColor = new Label();
            lblWarning = new Label();
            grpActions = new Panel();
            btnOK = new Button();
            btnApply = new Button();
            btnDefault = new Button();
            grpDistance.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numActualMin).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numActualMax).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numVdWMinus).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numVdWPlus).BeginInit();
            grpAngle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numAngle).BeginInit();
            grpOptions.SuspendLayout();
            grpDonorsAcceptors.SuspendLayout();
            grpActions.SuspendLayout();
            SuspendLayout();
            // 
            // grpDistance
            // 
            grpDistance.Controls.Add(rbActualDistance);
            grpDistance.Controls.Add(rbVdWDistance);
            grpDistance.Controls.Add(lblActualMin);
            grpDistance.Controls.Add(numActualMin);
            grpDistance.Controls.Add(lblActualMax);
            grpDistance.Controls.Add(numActualMax);
            grpDistance.Controls.Add(lblVdWMinus);
            grpDistance.Controls.Add(cmbVdWMinusOp);
            grpDistance.Controls.Add(numVdWMinus);
            grpDistance.Controls.Add(lblVdWPlus);
            grpDistance.Controls.Add(cmbVdWPlusOp);
            grpDistance.Controls.Add(numVdWPlus);
            grpDistance.Controls.Add(lblVdWSum);
            grpDistance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpDistance.ForeColor = Color.FromArgb(51, 51, 51);
            grpDistance.Location = new Point(8, 8);
            grpDistance.Name = "grpDistance";
            grpDistance.Size = new Size(360, 180);
            grpDistance.TabIndex = 0;
            grpDistance.TabStop = false;
            grpDistance.Text = "Contact distance range";
            // 
            // rbActualDistance
            // 
            rbActualDistance.AutoSize = true;
            rbActualDistance.Font = new Font("Segoe UI", 9F);
            rbActualDistance.Location = new Point(10, 22);
            rbActualDistance.Name = "rbActualDistance";
            rbActualDistance.Size = new Size(131, 24);
            rbActualDistance.TabIndex = 0;
            rbActualDistance.Text = "Actual distance";
            rbActualDistance.UseVisualStyleBackColor = true;
            rbActualDistance.CheckedChanged += RbDistanceMode_CheckedChanged;
            // 
            // rbVdWDistance
            // 
            rbVdWDistance.AutoSize = true;
            rbVdWDistance.Checked = true;
            rbVdWDistance.Font = new Font("Segoe UI", 9F);
            rbVdWDistance.Location = new Point(180, 22);
            rbVdWDistance.Name = "rbVdWDistance";
            rbVdWDistance.Size = new Size(120, 24);
            rbVdWDistance.TabIndex = 1;
            rbVdWDistance.TabStop = true;
            rbVdWDistance.Text = "VdW distance";
            rbVdWDistance.UseVisualStyleBackColor = true;
            rbVdWDistance.CheckedChanged += RbDistanceMode_CheckedChanged;
            // 
            // lblActualMin
            // 
            lblActualMin.AutoSize = true;
            lblActualMin.Font = new Font("Segoe UI", 9F);
            lblActualMin.Location = new Point(10, 60);
            lblActualMin.Name = "lblActualMin";
            lblActualMin.Size = new Size(96, 20);
            lblActualMin.TabIndex = 2;
            lblActualMin.Text = "Minimum (Å)";
            // 
            // numActualMin
            // 
            numActualMin.DecimalPlaces = 2;
            numActualMin.Font = new Font("Segoe UI", 9F);
            numActualMin.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            numActualMin.Location = new Point(110, 58);
            numActualMin.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numActualMin.Minimum = new decimal(new int[] { 10, 0, 0, int.MinValue });
            numActualMin.Name = "numActualMin";
            numActualMin.Size = new Size(70, 27);
            numActualMin.TabIndex = 3;
            numActualMin.Value = new decimal(new int[] { 150, 0, 0, 131072 });
            // 
            // lblActualMax
            // 
            lblActualMax.AutoSize = true;
            lblActualMax.Font = new Font("Segoe UI", 9F);
            lblActualMax.Location = new Point(10, 93);
            lblActualMax.Name = "lblActualMax";
            lblActualMax.Size = new Size(99, 20);
            lblActualMax.TabIndex = 4;
            lblActualMax.Text = "Maximum (Å)";
            // 
            // numActualMax
            // 
            numActualMax.DecimalPlaces = 2;
            numActualMax.Font = new Font("Segoe UI", 9F);
            numActualMax.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            numActualMax.Location = new Point(110, 91);
            numActualMax.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numActualMax.Minimum = new decimal(new int[] { 10, 0, 0, int.MinValue });
            numActualMax.Name = "numActualMax";
            numActualMax.Size = new Size(70, 27);
            numActualMax.TabIndex = 5;
            numActualMax.Value = new decimal(new int[] { 350, 0, 0, 131072 });
            // 
            // lblVdWMinus
            // 
            lblVdWMinus.AutoSize = true;
            lblVdWMinus.Font = new Font("Segoe UI", 9F);
            lblVdWMinus.Location = new Point(10, 60);
            lblVdWMinus.Name = "lblVdWMinus";
            lblVdWMinus.Size = new Size(203, 20);
            lblVdWMinus.TabIndex = 6;
            lblVdWMinus.Text = "Minimum = sum of vdW radii";
            // 
            // cmbVdWMinusOp
            // 
            cmbVdWMinusOp.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVdWMinusOp.Font = new Font("Segoe UI", 9F);
            cmbVdWMinusOp.Items.AddRange(new object[] { "minus", "plus" });
            cmbVdWMinusOp.Location = new Point(200, 57);
            cmbVdWMinusOp.Name = "cmbVdWMinusOp";
            cmbVdWMinusOp.Size = new Size(70, 28);
            cmbVdWMinusOp.TabIndex = 7;
            // 
            // numVdWMinus
            // 
            numVdWMinus.DecimalPlaces = 2;
            numVdWMinus.Font = new Font("Segoe UI", 9F);
            numVdWMinus.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
            numVdWMinus.Location = new Point(280, 57);
            numVdWMinus.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numVdWMinus.Minimum = new decimal(new int[] { 10, 0, 0, int.MinValue });
            numVdWMinus.Name = "numVdWMinus";
            numVdWMinus.Size = new Size(70, 27);
            numVdWMinus.TabIndex = 8;
            numVdWMinus.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // lblVdWPlus
            // 
            lblVdWPlus.AutoSize = true;
            lblVdWPlus.Font = new Font("Segoe UI", 9F);
            lblVdWPlus.Location = new Point(10, 93);
            lblVdWPlus.Name = "lblVdWPlus";
            lblVdWPlus.Size = new Size(206, 20);
            lblVdWPlus.TabIndex = 9;
            lblVdWPlus.Text = "Maximum = sum of vdW radii";
            // 
            // cmbVdWPlusOp
            // 
            cmbVdWPlusOp.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVdWPlusOp.Font = new Font("Segoe UI", 9F);
            cmbVdWPlusOp.Items.AddRange(new object[] { "minus", "plus" });
            cmbVdWPlusOp.Location = new Point(200, 90);
            cmbVdWPlusOp.Name = "cmbVdWPlusOp";
            cmbVdWPlusOp.Size = new Size(70, 28);
            cmbVdWPlusOp.TabIndex = 10;
            // 
            // numVdWPlus
            // 
            numVdWPlus.DecimalPlaces = 2;
            numVdWPlus.Font = new Font("Segoe UI", 9F);
            numVdWPlus.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
            numVdWPlus.Location = new Point(280, 90);
            numVdWPlus.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            numVdWPlus.Minimum = new decimal(new int[] { 10, 0, 0, int.MinValue });
            numVdWPlus.Name = "numVdWPlus";
            numVdWPlus.Size = new Size(70, 27);
            numVdWPlus.TabIndex = 11;
            // 
            // lblVdWSum
            // 
            lblVdWSum.AutoSize = true;
            lblVdWSum.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            lblVdWSum.ForeColor = Color.FromArgb(100, 100, 100);
            lblVdWSum.Location = new Point(10, 145);
            lblVdWSum.Name = "lblVdWSum";
            lblVdWSum.Size = new Size(335, 19);
            lblVdWSum.TabIndex = 12;
            lblVdWSum.Text = "Min/Max = r_vdW(donor) + r_vdW(acceptor) ± value";
            // 
            // grpAngle
            // 
            grpAngle.Controls.Add(lblAngle);
            grpAngle.Controls.Add(numAngle);
            grpAngle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpAngle.ForeColor = Color.FromArgb(51, 51, 51);
            grpAngle.Location = new Point(8, 194);
            grpAngle.Name = "grpAngle";
            grpAngle.Size = new Size(360, 60);
            grpAngle.TabIndex = 1;
            grpAngle.TabStop = false;
            grpAngle.Text = "D-H...A angle";
            // 
            // lblAngle
            // 
            lblAngle.AutoSize = true;
            lblAngle.Font = new Font("Segoe UI", 9F);
            lblAngle.Location = new Point(10, 24);
            lblAngle.Name = "lblAngle";
            lblAngle.Size = new Size(180, 20);
            lblAngle.TabIndex = 0;
            lblAngle.Text = "Minimum angle (degrees)";
            // 
            // numAngle
            // 
            numAngle.DecimalPlaces = 1;
            numAngle.Font = new Font("Segoe UI", 9F);
            numAngle.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            numAngle.Location = new Point(220, 22);
            numAngle.Maximum = new decimal(new int[] { 180, 0, 0, 0 });
            numAngle.Minimum = new decimal(new int[] { 60, 0, 0, 0 });
            numAngle.Name = "numAngle";
            numAngle.Size = new Size(70, 27);
            numAngle.TabIndex = 1;
            numAngle.Value = new decimal(new int[] { 650, 0, 0, 65536 });
            // 
            // grpOptions
            // 
            grpOptions.Controls.Add(chkShowHBonds);
            grpOptions.Controls.Add(chkIntramolecular);
            grpOptions.Controls.Add(chkIntermolecular);
            grpOptions.Controls.Add(chkDrawFromHydrogen);
            grpOptions.Controls.Add(chkRequireHydrogen);
            grpOptions.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpOptions.ForeColor = Color.FromArgb(51, 51, 51);
            grpOptions.Location = new Point(8, 260);
            grpOptions.Name = "grpOptions";
            grpOptions.Size = new Size(360, 160);
            grpOptions.TabIndex = 2;
            grpOptions.TabStop = false;
            grpOptions.Text = "Options";
            // 
            // chkShowHBonds
            // 
            chkShowHBonds.AutoSize = true;
            chkShowHBonds.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            chkShowHBonds.ForeColor = Color.FromArgb(0, 100, 200);
            chkShowHBonds.Location = new Point(10, 22);
            chkShowHBonds.Name = "chkShowHBonds";
            chkShowHBonds.Size = new Size(211, 24);
            chkShowHBonds.TabIndex = 0;
            chkShowHBonds.Text = "Show H-Bonds in 3D view";
            chkShowHBonds.UseVisualStyleBackColor = true;
            chkShowHBonds.CheckedChanged += ChkShowHBonds_CheckedChanged;
            // 
            // chkIntramolecular
            // 
            chkIntramolecular.AutoSize = true;
            chkIntramolecular.Checked = true;
            chkIntramolecular.CheckState = CheckState.Checked;
            chkIntramolecular.Font = new Font("Segoe UI", 9F);
            chkIntramolecular.Location = new Point(150, 106);
            chkIntramolecular.Name = "chkIntramolecular";
            chkIntramolecular.Size = new Size(127, 24);
            chkIntramolecular.TabIndex = 4;
            chkIntramolecular.Text = "Intramolecular";
            chkIntramolecular.UseVisualStyleBackColor = true;
            // 
            // chkIntermolecular
            // 
            chkIntermolecular.AutoSize = true;
            chkIntermolecular.Checked = true;
            chkIntermolecular.CheckState = CheckState.Checked;
            chkIntermolecular.Font = new Font("Segoe UI", 9F);
            chkIntermolecular.Location = new Point(10, 106);
            chkIntermolecular.Name = "chkIntermolecular";
            chkIntermolecular.Size = new Size(127, 24);
            chkIntermolecular.TabIndex = 3;
            chkIntermolecular.Text = "Intermolecular";
            chkIntermolecular.UseVisualStyleBackColor = true;
            // 
            // chkDrawFromHydrogen
            // 
            chkDrawFromHydrogen.AutoSize = true;
            chkDrawFromHydrogen.Font = new Font("Segoe UI", 9F);
            chkDrawFromHydrogen.Location = new Point(10, 78);
            chkDrawFromHydrogen.Name = "chkDrawFromHydrogen";
            chkDrawFromHydrogen.Size = new Size(225, 24);
            chkDrawFromHydrogen.TabIndex = 2;
            chkDrawFromHydrogen.Text = "Draw H-bond from hydrogen";
            chkDrawFromHydrogen.UseVisualStyleBackColor = true;
            chkDrawFromHydrogen.CheckedChanged += chkDrawFromHydrogen_CheckedChanged;
            // 
            // chkRequireHydrogen
            // 
            chkRequireHydrogen.AutoSize = true;
            chkRequireHydrogen.Font = new Font("Segoe UI", 9F);
            chkRequireHydrogen.Location = new Point(10, 50);
            chkRequireHydrogen.Name = "chkRequireHydrogen";
            chkRequireHydrogen.Size = new Size(280, 24);
            chkRequireHydrogen.TabIndex = 1;
            chkRequireHydrogen.Text = "Require hydrogen atom to be present";
            chkRequireHydrogen.UseVisualStyleBackColor = true;
            // 
            // grpDonorsAcceptors
            // 
            grpDonorsAcceptors.Controls.Add(chkSelectAllDonors);
            grpDonorsAcceptors.Controls.Add(chkSelectAllAcceptors);
            grpDonorsAcceptors.Controls.Add(lstDonors);
            grpDonorsAcceptors.Controls.Add(lstAcceptors);
            grpDonorsAcceptors.Controls.Add(lblDonors);
            grpDonorsAcceptors.Controls.Add(lblAcceptors);
            grpDonorsAcceptors.Controls.Add(btnHBondColor);
            grpDonorsAcceptors.Controls.Add(lblHBondColor);
            grpDonorsAcceptors.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpDonorsAcceptors.ForeColor = Color.FromArgb(51, 51, 51);
            grpDonorsAcceptors.Location = new Point(8, 426);
            grpDonorsAcceptors.Name = "grpDonorsAcceptors";
            grpDonorsAcceptors.Size = new Size(360, 220);
            grpDonorsAcceptors.TabIndex = 3;
            grpDonorsAcceptors.TabStop = false;
            grpDonorsAcceptors.Text = "Donors / Acceptors / Color";
            // 
            // chkSelectAllDonors
            // 
            chkSelectAllDonors.AutoSize = true;
            chkSelectAllDonors.Checked = true;
            chkSelectAllDonors.CheckState = CheckState.Checked;
            chkSelectAllDonors.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
            chkSelectAllDonors.Location = new Point(10, 44);
            chkSelectAllDonors.Name = "chkSelectAllDonors";
            chkSelectAllDonors.Size = new Size(120, 20);
            chkSelectAllDonors.TabIndex = 2;
            chkSelectAllDonors.Text = "Select all donors";
            chkSelectAllDonors.UseVisualStyleBackColor = true;
            chkSelectAllDonors.CheckedChanged += ChkSelectAllDonors_CheckedChanged;
            // 
            // chkSelectAllAcceptors
            // 
            chkSelectAllAcceptors.AutoSize = true;
            chkSelectAllAcceptors.Checked = true;
            chkSelectAllAcceptors.CheckState = CheckState.Checked;
            chkSelectAllAcceptors.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
            chkSelectAllAcceptors.Location = new Point(185, 44);
            chkSelectAllAcceptors.Name = "chkSelectAllAcceptors";
            chkSelectAllAcceptors.Size = new Size(130, 20);
            chkSelectAllAcceptors.TabIndex = 5;
            chkSelectAllAcceptors.Text = "Select all acceptors";
            chkSelectAllAcceptors.UseVisualStyleBackColor = true;
            chkSelectAllAcceptors.CheckedChanged += ChkSelectAllAcceptors_CheckedChanged;
            // 
            // lstDonors
            // 
            lstDonors.CheckOnClick = true;
            lstDonors.Font = new Font("Segoe UI", 9F);
            lstDonors.FormattingEnabled = true;
            lstDonors.Location = new Point(10, 68);
            lstDonors.Name = "lstDonors";
            lstDonors.Size = new Size(165, 106);
            lstDonors.TabIndex = 3;
            // 
            // lstAcceptors
            // 
            lstAcceptors.CheckOnClick = true;
            lstAcceptors.Font = new Font("Segoe UI", 9F);
            lstAcceptors.FormattingEnabled = true;
            lstAcceptors.Location = new Point(185, 68);
            lstAcceptors.Name = "lstAcceptors";
            lstAcceptors.Size = new Size(165, 106);
            lstAcceptors.TabIndex = 6;
            // 
            // lblDonors
            // 
            lblDonors.AutoSize = true;
            lblDonors.Font = new Font("Segoe UI", 9F);
            lblDonors.Location = new Point(10, 20);
            lblDonors.Name = "lblDonors";
            lblDonors.Size = new Size(60, 20);
            lblDonors.TabIndex = 0;
            lblDonors.Text = "Donors:";
            // 
            // lblAcceptors
            // 
            lblAcceptors.AutoSize = true;
            lblAcceptors.Font = new Font("Segoe UI", 9F);
            lblAcceptors.Location = new Point(185, 20);
            lblAcceptors.Name = "lblAcceptors";
            lblAcceptors.Size = new Size(72, 20);
            lblAcceptors.TabIndex = 4;
            lblAcceptors.Text = "Acceptors:";
            // 
            // lblHBondColor
            // 
            lblHBondColor.AutoSize = true;
            lblHBondColor.Font = new Font("Segoe UI", 9F);
            lblHBondColor.Location = new Point(10, 185);
            lblHBondColor.Name = "lblHBondColor";
            lblHBondColor.Size = new Size(110, 20);
            lblHBondColor.TabIndex = 7;
            lblHBondColor.Text = "H-Bond color:";
            // 
            // btnHBondColor
            // 
            btnHBondColor.BackColor = Color.FromArgb(0, 230, 0);
            btnHBondColor.FlatAppearance.BorderColor = Color.FromArgb(120, 120, 120);
            btnHBondColor.FlatStyle = FlatStyle.Flat;
            btnHBondColor.Location = new Point(130, 181);
            btnHBondColor.Name = "btnHBondColor";
            btnHBondColor.Size = new Size(80, 28);
            btnHBondColor.TabIndex = 8;
            btnHBondColor.UseVisualStyleBackColor = false;
            btnHBondColor.Click += BtnHBondColor_Click;
            // 
            // lblWarning
            // 
            lblWarning.Dock = DockStyle.Bottom;
            lblWarning.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            lblWarning.ForeColor = Color.FromArgb(180, 50, 50);
            lblWarning.Location = new Point(0, 656);
            lblWarning.Name = "lblWarning";
            lblWarning.Padding = new Padding(8, 4, 8, 4);
            lblWarning.Size = new Size(376, 34);
            lblWarning.TabIndex = 5;
            lblWarning.Text = "Atom types may not be classified properly for non-CSD structures.";
            lblWarning.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // grpActions
            // 
            grpActions.Controls.Add(btnOK);
            grpActions.Controls.Add(btnApply);
            grpActions.Controls.Add(btnDefault);
            grpActions.Dock = DockStyle.Bottom;
            grpActions.Location = new Point(0, 690);
            grpActions.Name = "grpActions";
            grpActions.Size = new Size(376, 30);
            grpActions.TabIndex = 4;
            // 
            // btnOK
            // 
            btnOK.BackColor = Color.FromArgb(0, 122, 204);
            btnOK.FlatStyle = FlatStyle.Flat;
            btnOK.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnOK.ForeColor = Color.White;
            btnOK.Location = new Point(276, 0);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(90, 30);
            btnOK.TabIndex = 2;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = false;
            btnOK.Click += BtnOK_Click;
            // 
            // btnApply
            // 
            btnApply.BackColor = Color.FromArgb(230, 230, 235);
            btnApply.FlatStyle = FlatStyle.Flat;
            btnApply.Font = new Font("Segoe UI", 9F);
            btnApply.Location = new Point(110, 0);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(90, 30);
            btnApply.TabIndex = 1;
            btnApply.Text = "Apply";
            btnApply.UseVisualStyleBackColor = false;
            btnApply.Click += BtnApply_Click;
            // 
            // btnDefault
            // 
            btnDefault.BackColor = Color.FromArgb(230, 230, 235);
            btnDefault.FlatStyle = FlatStyle.Flat;
            btnDefault.Font = new Font("Segoe UI", 9F);
            btnDefault.Location = new Point(10, 0);
            btnDefault.Name = "btnDefault";
            btnDefault.Size = new Size(90, 30);
            btnDefault.TabIndex = 0;
            btnDefault.Text = "Default";
            btnDefault.UseVisualStyleBackColor = false;
            btnDefault.Click += BtnDefault_Click;
            // 
            // HBondSettingsWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(248, 248, 250);
            ClientSize = new Size(376, 720);
            Controls.Add(grpDistance);
            Controls.Add(grpAngle);
            Controls.Add(grpOptions);
            Controls.Add(grpDonorsAcceptors);
            Controls.Add(lblWarning);
            Controls.Add(grpActions);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(394, 760);
            Name = "HBondSettingsWindow";
            Text = "H-Bond Settings";
            grpDistance.ResumeLayout(false);
            grpDistance.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numActualMin).EndInit();
            ((System.ComponentModel.ISupportInitialize)numActualMax).EndInit();
            ((System.ComponentModel.ISupportInitialize)numVdWMinus).EndInit();
            ((System.ComponentModel.ISupportInitialize)numVdWPlus).EndInit();
            grpAngle.ResumeLayout(false);
            grpAngle.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numAngle).EndInit();
            grpOptions.ResumeLayout(false);
            grpOptions.PerformLayout();
            grpDonorsAcceptors.ResumeLayout(false);
            grpDonorsAcceptors.PerformLayout();
            grpActions.ResumeLayout(false);
            ResumeLayout(false);
        }

        // Controls
        private System.Windows.Forms.GroupBox grpDistance;
        private System.Windows.Forms.RadioButton rbActualDistance;
        private System.Windows.Forms.RadioButton rbVdWDistance;
        private System.Windows.Forms.Label lblActualMin;
        private System.Windows.Forms.NumericUpDown numActualMin;
        private System.Windows.Forms.Label lblActualMax;
        private System.Windows.Forms.NumericUpDown numActualMax;
        private System.Windows.Forms.Label lblVdWMinus;
        private System.Windows.Forms.ComboBox cmbVdWMinusOp;
        private System.Windows.Forms.NumericUpDown numVdWMinus;
        private System.Windows.Forms.Label lblVdWPlus;
        private System.Windows.Forms.ComboBox cmbVdWPlusOp;
        private System.Windows.Forms.NumericUpDown numVdWPlus;
        private System.Windows.Forms.Label lblVdWSum;
        private System.Windows.Forms.GroupBox grpAngle;
        private System.Windows.Forms.Label lblAngle;
        private System.Windows.Forms.NumericUpDown numAngle;
        private System.Windows.Forms.GroupBox grpOptions;
        private System.Windows.Forms.CheckBox chkShowHBonds;
        private System.Windows.Forms.CheckBox chkRequireHydrogen;
        private System.Windows.Forms.CheckBox chkDrawFromHydrogen;
        private System.Windows.Forms.CheckBox chkIntermolecular;
        private System.Windows.Forms.CheckBox chkIntramolecular;
        private System.Windows.Forms.GroupBox grpDonorsAcceptors;
        private System.Windows.Forms.CheckBox chkSelectAllDonors;
        private System.Windows.Forms.CheckBox chkSelectAllAcceptors;
        private System.Windows.Forms.CheckedListBox lstDonors;
        private System.Windows.Forms.CheckedListBox lstAcceptors;
        private System.Windows.Forms.Label lblDonors;
        private System.Windows.Forms.Label lblAcceptors;
        private System.Windows.Forms.Button btnHBondColor;
        private System.Windows.Forms.Label lblHBondColor;
        private System.Windows.Forms.Panel grpActions;
        private System.Windows.Forms.Button btnDefault;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Label lblWarning;
    }
}