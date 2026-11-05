// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule overlay module: MoleculeOverlayWindow.Designer.

﻿// MoleculeOverlayWindow.Designer.cs - POPRAWIONY
namespace Tal.Forms
{
    partial class MoleculeOverlayWindow
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Button btnAddMolecule;
        private System.Windows.Forms.Button btnAddStructure;
        private System.Windows.Forms.Button btnRemoveSelected;
        private System.Windows.Forms.Button btnClearAll;
        private System.Windows.Forms.Button btnColor;
        private System.Windows.Forms.Button btnCenter;
        private System.Windows.Forms.Button btnShowOverlay;

        private System.Windows.Forms.ListBox lstMolecules;
        private System.Windows.Forms.CheckBox chkShowAll;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblAtomCount;

        private System.Windows.Forms.Label lblTransformation;
        private System.Windows.Forms.RadioButton rbTransformationNone;
        private System.Windows.Forms.RadioButton rbTransformationMirror;

        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Panel middlePanel;
        private System.Windows.Forms.Panel bottomPanel;

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
            topPanel = new Panel();
            btnAddMolecule = new Button();
            btnAddStructure = new Button();
            btnRemoveSelected = new Button();
            btnClearAll = new Button();
            btnCenter = new Button();
            btnShowOverlay = new Button();
            btnColor = new Button();
            chkShowAll = new CheckBox();
            lblAtomCount = new Label();
            middlePanel = new Panel();
            lstMolecules = new ListBox();
            bottomPanel = new Panel();
            lblTransformation = new Label();
            rbTransformationNone = new RadioButton();
            rbTransformationMirror = new RadioButton();
            lblStatus = new Label();
            topPanel.SuspendLayout();
            middlePanel.SuspendLayout();
            bottomPanel.SuspendLayout();
            SuspendLayout();
 // 
 // topPanel
 // 
            topPanel.BackColor = Color.FromArgb(248, 248, 250);
            topPanel.Controls.Add(btnAddMolecule);
            topPanel.Controls.Add(btnAddStructure);
            topPanel.Controls.Add(btnRemoveSelected);
            topPanel.Controls.Add(btnClearAll);
            topPanel.Controls.Add(btnCenter);
            topPanel.Controls.Add(btnShowOverlay);
            topPanel.Controls.Add(btnColor);
            topPanel.Controls.Add(chkShowAll);
            topPanel.Controls.Add(lblAtomCount);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Margin = new Padding(3, 4, 3, 4);
            topPanel.Name = "topPanel";
            topPanel.Padding = new Padding(5);
            topPanel.Size = new Size(432, 120);
            topPanel.TabIndex = 0;
 // 
 // btnAddMolecule
 // 
            btnAddMolecule.Location = new Point(121, 7);
            btnAddMolecule.Margin = new Padding(3, 4, 3, 4);
            btnAddMolecule.Name = "btnAddMolecule";
            btnAddMolecule.Size = new Size(129, 35);
            btnAddMolecule.TabIndex = 0;
            btnAddMolecule.Text = "Add molecule";
            btnAddMolecule.UseVisualStyleBackColor = true;
 // 
 // btnAddStructure
 // 
            btnAddStructure.Location = new Point(256, 7);
            btnAddStructure.Margin = new Padding(3, 4, 3, 4);
            btnAddStructure.Name = "btnAddStructure";
            btnAddStructure.Size = new Size(150, 35);
            btnAddStructure.TabIndex = 1;
            btnAddStructure.Text = "Add from ring";
            btnAddStructure.UseVisualStyleBackColor = true;
 // 
 // btnRemoveSelected
 // 
            btnRemoveSelected.Location = new Point(5, 45);
            btnRemoveSelected.Margin = new Padding(3, 4, 3, 4);
            btnRemoveSelected.Name = "btnRemoveSelected";
            btnRemoveSelected.Size = new Size(96, 35);
            btnRemoveSelected.TabIndex = 3;
            btnRemoveSelected.Text = "❌ Remove";
            btnRemoveSelected.UseVisualStyleBackColor = true;
 // 
 // btnClearAll
 // 
            btnClearAll.Location = new Point(107, 45);
            btnClearAll.Margin = new Padding(3, 4, 3, 4);
            btnClearAll.Name = "btnClearAll";
            btnClearAll.Size = new Size(84, 35);
            btnClearAll.TabIndex = 4;
            btnClearAll.Text = "🗑️ Clear";
            btnClearAll.UseVisualStyleBackColor = true;
 // 
 // btnCenter
 // 
            btnCenter.Location = new Point(197, 45);
            btnCenter.Margin = new Padding(3, 4, 3, 4);
            btnCenter.Name = "btnCenter";
            btnCenter.Size = new Size(42, 35);
            btnCenter.TabIndex = 5;
            btnCenter.Text = "🎯 Center";
            btnCenter.UseVisualStyleBackColor = true;
 // 
 // btnShowOverlay
 // 
            btnShowOverlay.Location = new Point(245, 45);
            btnShowOverlay.Margin = new Padding(3, 4, 3, 4);
            btnShowOverlay.Name = "btnShowOverlay";
            btnShowOverlay.Size = new Size(100, 35);
            btnShowOverlay.TabIndex = 8;
            btnShowOverlay.Text = "👁️ Show";
            btnShowOverlay.UseVisualStyleBackColor = true;
 // 
 // btnColor
 // 
            btnColor.BackColor = Color.FromArgb(0, 122, 204);
            btnColor.Location = new Point(351, 45);
            btnColor.Margin = new Padding(3, 4, 3, 4);
            btnColor.Name = "btnColor";
            btnColor.Size = new Size(45, 35);
            btnColor.TabIndex = 7;
            btnColor.UseVisualStyleBackColor = false;
 // 
 // chkShowAll
 // 
            chkShowAll.AutoSize = true;
            chkShowAll.Checked = true;
            chkShowAll.CheckState = CheckState.Checked;
            chkShowAll.Location = new Point(12, 13);
            chkShowAll.Margin = new Padding(3, 4, 3, 4);
            chkShowAll.Name = "chkShowAll";
            chkShowAll.Size = new Size(89, 24);
            chkShowAll.TabIndex = 9;
            chkShowAll.Text = "Show All";
            chkShowAll.UseVisualStyleBackColor = true;
 // 
 // lblAtomCount
 // 
            lblAtomCount.AutoSize = true;
            lblAtomCount.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblAtomCount.ForeColor = Color.FromArgb(51, 51, 51);
            lblAtomCount.Location = new Point(5, 91);
            lblAtomCount.Name = "lblAtomCount";
            lblAtomCount.Size = new Size(156, 20);
            lblAtomCount.TabIndex = 10;
            lblAtomCount.Text = "\U0001f9ea Atoms: 0 | Mol: 0";
 // 
 // middlePanel
 // 
            middlePanel.BackColor = Color.FromArgb(248, 248, 250);
            middlePanel.Controls.Add(lstMolecules);
            middlePanel.Dock = DockStyle.Fill;
            middlePanel.Location = new Point(0, 120);
            middlePanel.Margin = new Padding(3, 4, 3, 4);
            middlePanel.Name = "middlePanel";
            middlePanel.Padding = new Padding(5);
            middlePanel.Size = new Size(432, 587);
            middlePanel.TabIndex = 1;
 // 
 // lstMolecules
 // 
            lstMolecules.Dock = DockStyle.Fill;
            lstMolecules.FormattingEnabled = true;
            lstMolecules.Location = new Point(5, 5);
            lstMolecules.Margin = new Padding(3, 4, 3, 4);
            lstMolecules.Name = "lstMolecules";
            lstMolecules.Size = new Size(422, 577);
            lstMolecules.TabIndex = 18;
 // 
 // bottomPanel
 // 
            bottomPanel.BackColor = Color.FromArgb(248, 248, 250);
            bottomPanel.Controls.Add(lblTransformation);
            bottomPanel.Controls.Add(rbTransformationNone);
            bottomPanel.Controls.Add(rbTransformationMirror);
            bottomPanel.Controls.Add(lblStatus);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Location = new Point(0, 707);
            bottomPanel.Margin = new Padding(3, 4, 3, 4);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Padding = new Padding(5);
            bottomPanel.Size = new Size(432, 93);
            bottomPanel.TabIndex = 2;
 // 
 // lblTransformation
 // 
            lblTransformation.AutoSize = true;
            lblTransformation.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTransformation.ForeColor = Color.FromArgb(51, 51, 51);
            lblTransformation.Location = new Point(5, 5);
            lblTransformation.Name = "lblTransformation";
            lblTransformation.Size = new Size(122, 20);
            lblTransformation.TabIndex = 11;
            lblTransformation.Text = "Transformation:";
 // 
 // rbTransformationNone
 // 
            rbTransformationNone.AutoSize = true;
            rbTransformationNone.Checked = true;
            rbTransformationNone.Font = new Font("Segoe UI", 8F);
            rbTransformationNone.ForeColor = Color.FromArgb(51, 51, 51);
            rbTransformationNone.Location = new Point(142, 5);
            rbTransformationNone.Margin = new Padding(3, 4, 3, 4);
            rbTransformationNone.Name = "rbTransformationNone";
            rbTransformationNone.Size = new Size(63, 23);
            rbTransformationNone.TabIndex = 12;
            rbTransformationNone.TabStop = true;
            rbTransformationNone.Text = "None";
            rbTransformationNone.UseVisualStyleBackColor = true;
 // 
 // rbTransformationMirror
 // 
            rbTransformationMirror.AutoSize = true;
            rbTransformationMirror.Font = new Font("Segoe UI", 8F);
            rbTransformationMirror.ForeColor = Color.FromArgb(51, 51, 51);
            rbTransformationMirror.Location = new Point(211, 5);
            rbTransformationMirror.Margin = new Padding(3, 4, 3, 4);
            rbTransformationMirror.Name = "rbTransformationMirror";
            rbTransformationMirror.Size = new Size(69, 23);
            rbTransformationMirror.TabIndex = 13;
            rbTransformationMirror.Text = "Mirror";
            rbTransformationMirror.UseVisualStyleBackColor = true;


 // 
 // lblStatus
 // 
            lblStatus.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            lblStatus.ForeColor = Color.FromArgb(120, 120, 120);
            lblStatus.Location = new Point(5, 35);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(600, 53);
            lblStatus.TabIndex = 17;
            lblStatus.Text = "Ready - select atoms and click 'Add Molecule' or load a structure";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
 // 
 // MoleculeOverlayWindow
 // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(248, 248, 250);
            ClientSize = new Size(432, 800);
            Controls.Add(middlePanel);
            Controls.Add(topPanel);
            Controls.Add(bottomPanel);
            Font = new Font("Segoe UI", 9F);
            Margin = new Padding(3, 4, 3, 4);
            MinimumSize = new Size(450, 518);
            Name = "MoleculeOverlayWindow";
            Text = "\U0001f9ec Overlay Manager";
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            middlePanel.ResumeLayout(false);
            bottomPanel.ResumeLayout(false);
            bottomPanel.PerformLayout();
            ResumeLayout(false);
        }
    }
}