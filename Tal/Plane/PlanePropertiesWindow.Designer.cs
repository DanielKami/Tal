// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal planes module: PlanePropertiesWindow.Designer.

﻿namespace Tal.Forms
{
    partial class PlanePropertiesWindow
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            tabControl = new TabControl();
            tabMeanPlane = new TabPage();
            labelSelectionInfo = new Label();
            groupBoxOrSelect = new GroupBox();
            btnSelectHydrogen = new Button();
            btnSelectNonHydrogen = new Button();
            btnSelectAll = new Button();
            groupBoxPick = new GroupBox();
            rbRing = new RadioButton();
            rbPickedMolecule = new RadioButton();
            rbPickedAtom = new RadioButton();
            labelSelectAtoms = new Label();
            tabHKL = new TabPage();
            numericL = new NumericUpDown();
            labelL = new Label();
            numericK = new NumericUpDown();
            labelK = new Label();
            numericH = new NumericUpDown();
            labelH = new Label();
            groupBoxCommon = new GroupBox();
            panelColor = new Panel();
            labelColor = new Label();
            chkTransparent = new CheckBox();
            txtLabel = new TextBox();
            chkLabel = new CheckBox();
            chkShow = new CheckBox();
            btnOk = new Button();
            btnCancel = new Button();
            tabControl.SuspendLayout();
            tabMeanPlane.SuspendLayout();
            groupBoxOrSelect.SuspendLayout();
            groupBoxPick.SuspendLayout();
            tabHKL.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericL).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericK).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericH).BeginInit();
            groupBoxCommon.SuspendLayout();
            SuspendLayout();
 // 
 // tabControl
 // 
            tabControl.Controls.Add(tabMeanPlane);
            tabControl.Controls.Add(tabHKL);
            tabControl.Location = new Point(14, 16);
            tabControl.Margin = new Padding(3, 4, 3, 4);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(430, 400);
            tabControl.TabIndex = 0;
 // 
 // tabMeanPlane
 // 
            tabMeanPlane.Controls.Add(labelSelectionInfo);
            tabMeanPlane.Controls.Add(groupBoxOrSelect);
            tabMeanPlane.Controls.Add(groupBoxPick);
            tabMeanPlane.Controls.Add(labelSelectAtoms);
            tabMeanPlane.Location = new Point(4, 29);
            tabMeanPlane.Margin = new Padding(3, 4, 3, 4);
            tabMeanPlane.Name = "tabMeanPlane";
            tabMeanPlane.Padding = new Padding(3, 4, 3, 4);
            tabMeanPlane.Size = new Size(422, 367);
            tabMeanPlane.TabIndex = 0;
            tabMeanPlane.Text = "Mean plane";
            tabMeanPlane.UseVisualStyleBackColor = true;
 // 
 // labelSelectionInfo
 // 
            labelSelectionInfo.Location = new Point(11, 324);
            labelSelectionInfo.Name = "labelSelectionInfo";
            labelSelectionInfo.Size = new Size(398, 27);
            labelSelectionInfo.TabIndex = 3;
            labelSelectionInfo.Text = "Selected: 0 atoms";
 // 
 // groupBoxOrSelect
 // 
            groupBoxOrSelect.Controls.Add(btnSelectHydrogen);
            groupBoxOrSelect.Controls.Add(btnSelectNonHydrogen);
            groupBoxOrSelect.Controls.Add(btnSelectAll);
            groupBoxOrSelect.Location = new Point(11, 211);
            groupBoxOrSelect.Margin = new Padding(3, 4, 3, 4);
            groupBoxOrSelect.Name = "groupBoxOrSelect";
            groupBoxOrSelect.Padding = new Padding(3, 4, 3, 4);
            groupBoxOrSelect.Size = new Size(398, 107);
            groupBoxOrSelect.TabIndex = 2;
            groupBoxOrSelect.TabStop = false;
            groupBoxOrSelect.Text = "Or select:";
 // 
 // btnSelectHydrogen
 // 
            btnSelectHydrogen.Location = new Point(272, 29);
            btnSelectHydrogen.Margin = new Padding(3, 4, 3, 4);
            btnSelectHydrogen.Name = "btnSelectHydrogen";
            btnSelectHydrogen.Size = new Size(114, 33);
            btnSelectHydrogen.TabIndex = 2;
            btnSelectHydrogen.Text = "Hydrogen";
            btnSelectHydrogen.UseVisualStyleBackColor = true;
            btnSelectHydrogen.Click += BtnSelectHydrogen_Click;
 // 
 // btnSelectNonHydrogen
 // 
            btnSelectNonHydrogen.Location = new Point(139, 29);
            btnSelectNonHydrogen.Margin = new Padding(3, 4, 3, 4);
            btnSelectNonHydrogen.Name = "btnSelectNonHydrogen";
            btnSelectNonHydrogen.Size = new Size(126, 33);
            btnSelectNonHydrogen.TabIndex = 1;
            btnSelectNonHydrogen.Text = "Non-hydrogen";
            btnSelectNonHydrogen.UseVisualStyleBackColor = true;
            btnSelectNonHydrogen.Click += BtnSelectNonHydrogen_Click;
 // 
 // btnSelectAll
 // 
            btnSelectAll.Location = new Point(17, 29);
            btnSelectAll.Margin = new Padding(3, 4, 3, 4);
            btnSelectAll.Name = "btnSelectAll";
            btnSelectAll.Size = new Size(114, 33);
            btnSelectAll.TabIndex = 0;
            btnSelectAll.Text = "All";
            btnSelectAll.UseVisualStyleBackColor = true;
            btnSelectAll.Click += BtnSelectAll_Click;
 // 
 // groupBoxPick
 // 
            groupBoxPick.Controls.Add(rbRing);
            groupBoxPick.Controls.Add(rbPickedMolecule);
            groupBoxPick.Controls.Add(rbPickedAtom);
            groupBoxPick.Location = new Point(11, 67);
            groupBoxPick.Margin = new Padding(3, 4, 3, 4);
            groupBoxPick.Name = "groupBoxPick";
            groupBoxPick.Padding = new Padding(3, 4, 3, 4);
            groupBoxPick.Size = new Size(398, 133);
            groupBoxPick.TabIndex = 1;
            groupBoxPick.TabStop = false;
            groupBoxPick.Text = "Pick atom to select:";
 // 
 // rbRing
 // 
            rbRing.AutoSize = true;
            rbRing.Location = new Point(17, 93);
            rbRing.Margin = new Padding(3, 4, 3, 4);
            rbRing.Name = "rbRing";
            rbRing.Size = new Size(60, 24);
            rbRing.TabIndex = 2;
            rbRing.Text = "Ring";
            rbRing.UseVisualStyleBackColor = true;
            rbRing.Checked = false;
            rbRing.CheckedChanged += RbRing_CheckedChanged;
 // 
 // rbPickedMolecule
 // 
            rbPickedMolecule.AutoSize = true;
            rbPickedMolecule.Location = new Point(17, 61);
            rbPickedMolecule.Margin = new Padding(3, 4, 3, 4);
            rbPickedMolecule.Name = "rbPickedMolecule";
            rbPickedMolecule.Size = new Size(138, 24);
            rbPickedMolecule.TabIndex = 1;
            rbPickedMolecule.TabStop = true;
            rbPickedMolecule.Text = "Picked molecule";
            rbPickedMolecule.UseVisualStyleBackColor = true;
            rbPickedMolecule.Checked = false;
            rbPickedMolecule.CheckedChanged += RbPickedMolecule_CheckedChanged;
          
 // 
 // rbPickedAtom
 // 
            rbPickedAtom.AutoSize = true;
            rbPickedAtom.Checked = true;
            rbPickedAtom.Location = new Point(17, 29);
            rbPickedAtom.Margin = new Padding(3, 4, 3, 4);
            rbPickedAtom.Name = "rbPickedAtom";
            rbPickedAtom.Size = new Size(112, 24);
            rbPickedAtom.TabIndex = 0;
            rbPickedAtom.TabStop = true;
            rbPickedAtom.Text = "Picked atom";
            rbPickedAtom.UseVisualStyleBackColor = true;
            rbPickedAtom.CheckedChanged += RbPickedAtom_CheckedChanged;
            
 // 
 // labelSelectAtoms
 // 
            labelSelectAtoms.Location = new Point(11, 11);
            labelSelectAtoms.Name = "labelSelectAtoms";
            labelSelectAtoms.Size = new Size(400, 48);
            labelSelectAtoms.TabIndex = 0;
            labelSelectAtoms.Text = "Select at least three atoms to\r\ncalculate least squares plane:";
 // 
 // tabHKL
 // 
            tabHKL.Controls.Add(numericL);
            tabHKL.Controls.Add(labelL);
            tabHKL.Controls.Add(numericK);
            tabHKL.Controls.Add(labelK);
            tabHKL.Controls.Add(numericH);
            tabHKL.Controls.Add(labelH);
            tabHKL.Location = new Point(4, 29);
            tabHKL.Margin = new Padding(3, 4, 3, 4);
            tabHKL.Name = "tabHKL";
            tabHKL.Padding = new Padding(3, 4, 3, 4);
            tabHKL.Size = new Size(422, 367);
            tabHKL.TabIndex = 1;
            tabHKL.Text = "hkl";
            tabHKL.UseVisualStyleBackColor = true;
 // 
 // numericL
 // 
            numericL.Location = new Point(80, 157);
            numericL.Margin = new Padding(3, 4, 3, 4);
            numericL.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            numericL.Minimum = new decimal(new int[] { 20, 0, 0, int.MinValue });
            numericL.Name = "numericL";
            numericL.Size = new Size(91, 27);
            numericL.TabIndex = 5;
 // 
 // labelL
 // 
            labelL.Location = new Point(34, 160);
            labelL.Name = "labelL";
            labelL.Size = new Size(34, 33);
            labelL.TabIndex = 4;
            labelL.Text = "l:";
 // 
 // numericK
 // 
            numericK.Location = new Point(80, 104);
            numericK.Margin = new Padding(3, 4, 3, 4);
            numericK.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            numericK.Minimum = new decimal(new int[] { 20, 0, 0, int.MinValue });
            numericK.Name = "numericK";
            numericK.Size = new Size(91, 27);
            numericK.TabIndex = 3;
 // 
 // labelK
 // 
            labelK.Location = new Point(34, 107);
            labelK.Name = "labelK";
            labelK.Size = new Size(34, 33);
            labelK.TabIndex = 2;
            labelK.Text = "k:";
 // 
 // numericH
 // 
            numericH.Location = new Point(80, 51);
            numericH.Margin = new Padding(3, 4, 3, 4);
            numericH.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            numericH.Minimum = new decimal(new int[] { 20, 0, 0, int.MinValue });
            numericH.Name = "numericH";
            numericH.Size = new Size(91, 27);
            numericH.TabIndex = 1;
 // 
 // labelH
 // 
            labelH.Location = new Point(34, 53);
            labelH.Name = "labelH";
            labelH.Size = new Size(34, 33);
            labelH.TabIndex = 0;
            labelH.Text = "h:";
 // 
 // groupBoxCommon
 // 
            groupBoxCommon.Controls.Add(panelColor);
            groupBoxCommon.Controls.Add(labelColor);
            groupBoxCommon.Controls.Add(chkTransparent);
            groupBoxCommon.Controls.Add(txtLabel);
            groupBoxCommon.Controls.Add(chkLabel);
            groupBoxCommon.Controls.Add(chkShow);
            groupBoxCommon.Location = new Point(14, 424);
            groupBoxCommon.Margin = new Padding(3, 4, 3, 4);
            groupBoxCommon.Name = "groupBoxCommon";
            groupBoxCommon.Padding = new Padding(3, 4, 3, 4);
            groupBoxCommon.Size = new Size(430, 173);
            groupBoxCommon.TabIndex = 1;
            groupBoxCommon.TabStop = false;
 // 
 // panelColor
 // 
            panelColor.BackColor = Color.Red;
            panelColor.BorderStyle = BorderStyle.FixedSingle;
            panelColor.Cursor = Cursors.Hand;
            panelColor.Location = new Point(80, 124);
            panelColor.Margin = new Padding(3, 4, 3, 4);
            panelColor.Name = "panelColor";
            panelColor.Size = new Size(57, 33);
            panelColor.TabIndex = 5;
            panelColor.Click += PanelColor_Click;
 // 
 // labelColor
 // 
            labelColor.Location = new Point(11, 127);
            labelColor.Name = "labelColor";
            labelColor.Size = new Size(57, 33);
            labelColor.TabIndex = 4;
            labelColor.Text = "Colour:";
 // 
 // chkTransparent
 // 
            chkTransparent.AutoSize = true;
            chkTransparent.Checked = true;
            chkTransparent.CheckState = CheckState.Checked;
            chkTransparent.Location = new Point(11, 93);
            chkTransparent.Margin = new Padding(3, 4, 3, 4);
            chkTransparent.Name = "chkTransparent";
            chkTransparent.Size = new Size(108, 24);
            chkTransparent.TabIndex = 3;
            chkTransparent.Text = "Transparent";
            chkTransparent.UseVisualStyleBackColor = true;
 // 
 // txtLabel
 // 
            txtLabel.Location = new Point(80, 57);
            txtLabel.Margin = new Padding(3, 4, 3, 4);
            txtLabel.Name = "txtLabel";
            txtLabel.Size = new Size(338, 27);
            txtLabel.TabIndex = 2;
            txtLabel.Text = "mean:";
 // 
 // chkLabel
 // 
            chkLabel.AutoSize = true;
            chkLabel.Checked = true;
            chkLabel.CheckState = CheckState.Checked;
            chkLabel.Location = new Point(11, 60);
            chkLabel.Margin = new Padding(3, 4, 3, 4);
            chkLabel.Name = "chkLabel";
            chkLabel.Size = new Size(67, 24);
            chkLabel.TabIndex = 1;
            chkLabel.Text = "Label";
            chkLabel.UseVisualStyleBackColor = true;
 // 
 // chkShow
 // 
            chkShow.AutoSize = true;
            chkShow.Checked = true;
            chkShow.CheckState = CheckState.Checked;
            chkShow.Location = new Point(11, 27);
            chkShow.Margin = new Padding(3, 4, 3, 4);
            chkShow.Name = "chkShow";
            chkShow.Size = new Size(67, 24);
            chkShow.TabIndex = 0;
            chkShow.Text = "Show";
            chkShow.UseVisualStyleBackColor = true;
 // 
 // btnOk
 // 
            btnOk.Location = new Point(229, 613);
            btnOk.Margin = new Padding(3, 4, 3, 4);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(103, 40);
            btnOk.TabIndex = 2;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += BtnOk_Click;
 // 
 // btnCancel
 // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(338, 613);
            btnCancel.Margin = new Padding(3, 4, 3, 4);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(103, 40);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancel_Click;
 // 
 // PlanePropertiesWindow
 // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(457, 667);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Controls.Add(groupBoxCommon);
            Controls.Add(tabControl);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PlanePropertiesWindow";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Plane Properties";
            tabControl.ResumeLayout(false);
            tabMeanPlane.ResumeLayout(false);
            groupBoxOrSelect.ResumeLayout(false);
            groupBoxPick.ResumeLayout(false);
            groupBoxPick.PerformLayout();
            tabHKL.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numericL).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericK).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericH).EndInit();
            groupBoxCommon.ResumeLayout(false);
            groupBoxCommon.PerformLayout();
            ResumeLayout(false);
        }

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabMeanPlane;
        private System.Windows.Forms.Label labelSelectAtoms;
        private System.Windows.Forms.GroupBox groupBoxPick;
        private System.Windows.Forms.RadioButton rbRing;
        private System.Windows.Forms.RadioButton rbPickedMolecule;
        private System.Windows.Forms.RadioButton rbPickedAtom;
        private System.Windows.Forms.GroupBox groupBoxOrSelect;
        private System.Windows.Forms.Button btnSelectHydrogen;
        private System.Windows.Forms.Button btnSelectNonHydrogen;
        private System.Windows.Forms.Button btnSelectAll;
        private System.Windows.Forms.Label labelSelectionInfo;
        private System.Windows.Forms.TabPage tabHKL;
        private System.Windows.Forms.NumericUpDown numericL;
        private System.Windows.Forms.Label labelL;
        private System.Windows.Forms.NumericUpDown numericK;
        private System.Windows.Forms.Label labelK;
        private System.Windows.Forms.NumericUpDown numericH;
        private System.Windows.Forms.Label labelH;
        private System.Windows.Forms.GroupBox groupBoxCommon;
        private System.Windows.Forms.CheckBox chkShow;
        private System.Windows.Forms.CheckBox chkLabel;
        private System.Windows.Forms.TextBox txtLabel;
        private System.Windows.Forms.CheckBox chkTransparent;
        private System.Windows.Forms.Label labelColor;
        private System.Windows.Forms.Panel panelColor;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}