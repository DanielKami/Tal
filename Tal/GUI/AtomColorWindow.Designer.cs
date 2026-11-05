// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// AtomColorWindow designer-generated layout.

﻿namespace Tal.Forms
{
    partial class AtomColorWindow
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.GroupBox _grpMode;
        private System.Windows.Forms.RadioButton _radioAtomicNumber;
        private System.Windows.Forms.RadioButton _radioSymmetry;
        private System.Windows.Forms.RadioButton _radioMolecule;
        private System.Windows.Forms.RadioButton _radioSymmetryMolecule;
        private System.Windows.Forms.RadioButton _radioIndividual;
        private System.Windows.Forms.RadioButton _radioChargeColor;
        private System.Windows.Forms.RadioButton _radioHydrophobicityColor;
        private System.Windows.Forms.GroupBox _grpOptions;
        // ⭐ USUWAMY _chkApplyToSelected
        private System.Windows.Forms.Panel _pnlColorPicker;
        private System.Windows.Forms.Button _btnPickColor;
        private System.Windows.Forms.Label _lblStatus;

        private System.Windows.Forms.GroupBox _grpProperties;
        private System.Windows.Forms.RadioButton _radioLabelNone;
        private System.Windows.Forms.RadioButton _radioLabelCharge;
        private System.Windows.Forms.RadioButton _radioLabelHydrophobicity;

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
            _grpMode = new System.Windows.Forms.GroupBox();
            _radioAtomicNumber = new System.Windows.Forms.RadioButton();
            _radioSymmetry = new System.Windows.Forms.RadioButton();
            _radioMolecule = new System.Windows.Forms.RadioButton();
            _radioSymmetryMolecule = new System.Windows.Forms.RadioButton();
            _radioIndividual = new System.Windows.Forms.RadioButton();
            _radioChargeColor = new System.Windows.Forms.RadioButton();
            _radioHydrophobicityColor = new System.Windows.Forms.RadioButton();
            _grpOptions = new System.Windows.Forms.GroupBox();
            // ⭐ USUWAMY _chkApplyToSelected
            _pnlColorPicker = new System.Windows.Forms.Panel();
            _btnPickColor = new System.Windows.Forms.Button();
            _lblStatus = new System.Windows.Forms.Label();

            _grpProperties = new System.Windows.Forms.GroupBox();
            _radioLabelNone = new System.Windows.Forms.RadioButton();
            _radioLabelCharge = new System.Windows.Forms.RadioButton();
            _radioLabelHydrophobicity = new System.Windows.Forms.RadioButton();

            _grpMode.SuspendLayout();
            _grpOptions.SuspendLayout();
            _pnlColorPicker.SuspendLayout();
            _grpProperties.SuspendLayout();
            this.SuspendLayout();

            // 
            // _grpMode
            // 
            _grpMode.Controls.Add(_radioAtomicNumber);
            _grpMode.Controls.Add(_radioSymmetry);
            _grpMode.Controls.Add(_radioMolecule);
            _grpMode.Controls.Add(_radioSymmetryMolecule);
            _grpMode.Controls.Add(_radioIndividual);
            _grpMode.Controls.Add(_radioChargeColor);
            _grpMode.Controls.Add(_radioHydrophobicityColor);
            _grpMode.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            _grpMode.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _grpMode.Location = new System.Drawing.Point(12, 12);
            _grpMode.Name = "_grpMode";
            _grpMode.Size = new System.Drawing.Size(256, 220);
            _grpMode.TabIndex = 0;
            _grpMode.TabStop = false;
            _grpMode.Text = "Color Mode";

            // 
            // _radioAtomicNumber
            // 
            _radioAtomicNumber.Font = new System.Drawing.Font("Segoe UI", 9F);
            _radioAtomicNumber.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _radioAtomicNumber.Location = new System.Drawing.Point(10, 22);
            _radioAtomicNumber.Name = "_radioAtomicNumber";
            _radioAtomicNumber.Size = new System.Drawing.Size(236, 22);
            _radioAtomicNumber.TabIndex = 0;
            _radioAtomicNumber.Text = "Atomic Number (Default)";
            _radioAtomicNumber.UseVisualStyleBackColor = true;

            // 
            // _radioSymmetry
            // 
            _radioSymmetry.Font = new System.Drawing.Font("Segoe UI", 9F);
            _radioSymmetry.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _radioSymmetry.Location = new System.Drawing.Point(10, 48);
            _radioSymmetry.Name = "_radioSymmetry";
            _radioSymmetry.Size = new System.Drawing.Size(236, 22);
            _radioSymmetry.TabIndex = 1;
            _radioSymmetry.Text = "Symmetry Equivalent";
            _radioSymmetry.UseVisualStyleBackColor = true;

            // 
            // _radioMolecule
            // 
            _radioMolecule.Font = new System.Drawing.Font("Segoe UI", 9F);
            _radioMolecule.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _radioMolecule.Location = new System.Drawing.Point(10, 74);
            _radioMolecule.Name = "_radioMolecule";
            _radioMolecule.Size = new System.Drawing.Size(236, 22);
            _radioMolecule.TabIndex = 2;
            _radioMolecule.Text = "Molecule Number";
            _radioMolecule.UseVisualStyleBackColor = true;

            // 
            // _radioSymmetryMolecule
            // 
            _radioSymmetryMolecule.Font = new System.Drawing.Font("Segoe UI", 9F);
            _radioSymmetryMolecule.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _radioSymmetryMolecule.Location = new System.Drawing.Point(10, 100);
            _radioSymmetryMolecule.Name = "_radioSymmetryMolecule";
            _radioSymmetryMolecule.Size = new System.Drawing.Size(236, 22);
            _radioSymmetryMolecule.TabIndex = 3;
            _radioSymmetryMolecule.Text = "Symmetry + Molecule";
            _radioSymmetryMolecule.UseVisualStyleBackColor = true;

            // 
            // _radioIndividual
            // 
            _radioIndividual.Font = new System.Drawing.Font("Segoe UI", 9F);
            _radioIndividual.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _radioIndividual.Location = new System.Drawing.Point(10, 126);
            _radioIndividual.Name = "_radioIndividual";
            _radioIndividual.Size = new System.Drawing.Size(236, 22);
            _radioIndividual.TabIndex = 4;
            _radioIndividual.Text = "Individual Color (Selected Atoms)";
            _radioIndividual.UseVisualStyleBackColor = true;

            // 
            // _radioChargeColor
            // 
            _radioChargeColor.Font = new System.Drawing.Font("Segoe UI", 9F);
            _radioChargeColor.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _radioChargeColor.Location = new System.Drawing.Point(10, 152);
            _radioChargeColor.Name = "_radioChargeColor";
            _radioChargeColor.Size = new System.Drawing.Size(236, 22);
            _radioChargeColor.TabIndex = 5;
            _radioChargeColor.Text = "Charge (Color by Charge)";
            _radioChargeColor.UseVisualStyleBackColor = true;

            // 
            // _radioHydrophobicityColor
            // 
            _radioHydrophobicityColor.Font = new System.Drawing.Font("Segoe UI", 9F);
            _radioHydrophobicityColor.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _radioHydrophobicityColor.Location = new System.Drawing.Point(10, 178);
            _radioHydrophobicityColor.Name = "_radioHydrophobicityColor";
            _radioHydrophobicityColor.Size = new System.Drawing.Size(236, 22);
            _radioHydrophobicityColor.TabIndex = 6;
            _radioHydrophobicityColor.Text = "Hydrophobicity (Color by Hydrophobicity)";
            _radioHydrophobicityColor.UseVisualStyleBackColor = true;

            // 
            // _grpOptions
            // 
            _grpOptions.Controls.Add(_pnlColorPicker);
            _grpOptions.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            _grpOptions.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _grpOptions.Location = new System.Drawing.Point(12, 290);
            _grpOptions.Name = "_grpOptions";
            _grpOptions.Size = new System.Drawing.Size(256, 60);
            _grpOptions.TabIndex = 1;
            _grpOptions.TabStop = false;
            _grpOptions.Text = "Individual Color";

            // 
            // _pnlColorPicker
            // 
            _pnlColorPicker.Controls.Add(_btnPickColor);
            _pnlColorPicker.Location = new System.Drawing.Point(10, 18);
            _pnlColorPicker.Name = "_pnlColorPicker";
            _pnlColorPicker.Size = new System.Drawing.Size(236, 40);
            _pnlColorPicker.TabIndex = 1;
            _pnlColorPicker.Visible = false;

            // 
            // _btnPickColor
            // 
            _btnPickColor.BackColor = System.Drawing.Color.Red;
            _btnPickColor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            _btnPickColor.Font = new System.Drawing.Font("Segoe UI", 9F);
            _btnPickColor.ForeColor = System.Drawing.Color.White;
            _btnPickColor.Location = new System.Drawing.Point(0, 4);
            _btnPickColor.Name = "_btnPickColor";
            _btnPickColor.Size = new System.Drawing.Size(236, 32);
            _btnPickColor.TabIndex = 0;
            _btnPickColor.Text = "Pick Color";
            _btnPickColor.UseVisualStyleBackColor = false;

            // 
            // _grpProperties
            // 
            _grpProperties.Controls.Add(_radioLabelNone);
            _grpProperties.Controls.Add(_radioLabelCharge);
            _grpProperties.Controls.Add(_radioLabelHydrophobicity);
            _grpProperties.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            _grpProperties.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _grpProperties.Location = new System.Drawing.Point(12, 238);
            _grpProperties.Name = "_grpProperties";
            _grpProperties.Size = new System.Drawing.Size(256, 46);
            _grpProperties.TabIndex = 2;
            _grpProperties.TabStop = false;
            _grpProperties.Text = "Label Display";

            // 
            // _radioLabelNone
            // 
            _radioLabelNone.Font = new System.Drawing.Font("Segoe UI", 9F);
            _radioLabelNone.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _radioLabelNone.Location = new System.Drawing.Point(10, 18);
            _radioLabelNone.Name = "_radioLabelNone";
            _radioLabelNone.Size = new System.Drawing.Size(60, 22);
            _radioLabelNone.TabIndex = 0;
            _radioLabelNone.Text = "None";
            _radioLabelNone.UseVisualStyleBackColor = true;
            _radioLabelNone.Checked = true;

            // 
            // _radioLabelCharge
            // 
            _radioLabelCharge.Font = new System.Drawing.Font("Segoe UI", 9F);
            _radioLabelCharge.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _radioLabelCharge.Location = new System.Drawing.Point(76, 18);
            _radioLabelCharge.Name = "_radioLabelCharge";
            _radioLabelCharge.Size = new System.Drawing.Size(70, 22);
            _radioLabelCharge.TabIndex = 1;
            _radioLabelCharge.Text = "Charge";
            _radioLabelCharge.UseVisualStyleBackColor = true;

            // 
            // _radioLabelHydrophobicity
            // 
            _radioLabelHydrophobicity.Font = new System.Drawing.Font("Segoe UI", 9F);
            _radioLabelHydrophobicity.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            _radioLabelHydrophobicity.Location = new System.Drawing.Point(152, 18);
            _radioLabelHydrophobicity.Name = "_radioLabelHydrophobicity";
            _radioLabelHydrophobicity.Size = new System.Drawing.Size(94, 22);
            _radioLabelHydrophobicity.TabIndex = 2;
            _radioLabelHydrophobicity.Text = "Hydrophob.";
            _radioLabelHydrophobicity.UseVisualStyleBackColor = true;

            // 
            // _lblStatus
            // 
            _lblStatus.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            _lblStatus.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120);
            _lblStatus.Location = new System.Drawing.Point(12, 398);
            _lblStatus.Name = "_lblStatus";
            _lblStatus.Size = new System.Drawing.Size(256, 30);
            _lblStatus.TabIndex = 3;
            _lblStatus.Text = "Select color mode - auto applied";
            _lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // 
            // AtomColorWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(248, 248, 250);
            this.ClientSize = new System.Drawing.Size(280, 440);
            this.Controls.Add(_grpMode);
            this.Controls.Add(_grpProperties);
            this.Controls.Add(_grpOptions);
            this.Controls.Add(_lblStatus);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(260, 440);
            this.Name = "AtomColorWindow";
            this.Text = "Atom Color";
            _grpMode.ResumeLayout(false);
            _grpOptions.ResumeLayout(false);
            _pnlColorPicker.ResumeLayout(false);
            _grpProperties.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}