// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// ShortContactsWindow designer-generated layout.

﻿namespace Tal.Forms
{
    partial class ShortContactsWindow
    {
        private System.ComponentModel.IContainer components = null;

        // ⭐ DEKLARACJE KONTROLEK
        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.NumericUpDown _numVdwMinus;
        private System.Windows.Forms.Label _lblVdwMinusUnit;
        private System.Windows.Forms.RadioButton _rbFixedDistance;
        private System.Windows.Forms.RadioButton _rbVdwSum;
        private System.Windows.Forms.CheckBox _chkIntermolecular;
        private System.Windows.Forms.CheckBox _chkIntramolecular;
        private System.Windows.Forms.CheckBox _chkSelectWholeMolecule;
        private System.Windows.Forms.Button _btnApply;
        private System.Windows.Forms.Button _btnOk;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.GroupBox _grpOptions;
        private System.Windows.Forms.GroupBox _grpActions;
        private System.Windows.Forms.GroupBox _grpMode;

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
            _lblTitle = new Label();
            _numVdwMinus = new NumericUpDown();
            _lblVdwMinusUnit = new Label();
            _rbFixedDistance = new RadioButton();
            _rbVdwSum = new RadioButton();
            _chkIntermolecular = new CheckBox();
            _chkIntramolecular = new CheckBox();
            _chkSelectWholeMolecule = new CheckBox();
            _btnApply = new Button();
            _btnOk = new Button();
            _btnCancel = new Button();
            _lblStatus = new Label();
            _grpOptions = new GroupBox();
            _grpActions = new GroupBox();
            _grpMode = new GroupBox();
            ((System.ComponentModel.ISupportInitialize)_numVdwMinus).BeginInit();
            _grpOptions.SuspendLayout();
            _grpActions.SuspendLayout();
            _grpMode.SuspendLayout();
            SuspendLayout();
            // 
            // _lblTitle
            // 
            _lblTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _lblTitle.ForeColor = Color.FromArgb(51, 51, 51);
            _lblTitle.Location = new Point(12, 12);
            _lblTitle.Name = "_lblTitle";
            _lblTitle.Size = new Size(350, 25);
            _lblTitle.TabIndex = 0;
            _lblTitle.Text = "Define Short Contacts";
            _lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _numVdwMinus
            // 
            _numVdwMinus.BackColor = Color.White;
            _numVdwMinus.BorderStyle = BorderStyle.FixedSingle;
            _numVdwMinus.DecimalPlaces = 2;
            _numVdwMinus.Font = new Font("Segoe UI", 9F);
            _numVdwMinus.ForeColor = Color.FromArgb(51, 51, 51);
            _numVdwMinus.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            _numVdwMinus.Location = new Point(200, 19);
            _numVdwMinus.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            _numVdwMinus.Name = "_numVdwMinus";
            _numVdwMinus.Size = new Size(60, 27);
            _numVdwMinus.TabIndex = 3;
            _numVdwMinus.TextAlign = HorizontalAlignment.Center;
            _numVdwMinus.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // _lblVdwMinusUnit
            // 
            _lblVdwMinusUnit.Font = new Font("Segoe UI", 9F);
            _lblVdwMinusUnit.ForeColor = Color.FromArgb(51, 51, 51);
            _lblVdwMinusUnit.Location = new Point(266, 19);
            _lblVdwMinusUnit.Name = "_lblVdwMinusUnit";
            _lblVdwMinusUnit.Size = new Size(50, 25);
            _lblVdwMinusUnit.TabIndex = 4;
            _lblVdwMinusUnit.Text = "Å";
            _lblVdwMinusUnit.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _rbFixedDistance
            // 
            _rbFixedDistance.AutoSize = true;
            _rbFixedDistance.Font = new Font("Segoe UI", 9F);
            _rbFixedDistance.ForeColor = Color.FromArgb(51, 51, 51);
            _rbFixedDistance.Location = new Point(10, 22);
            _rbFixedDistance.Name = "_rbFixedDistance";
            _rbFixedDistance.Size = new Size(148, 24);
            _rbFixedDistance.TabIndex = 0;
            _rbFixedDistance.TabStop = true;
            _rbFixedDistance.Text = "Fixed distance (Å)";
            _rbFixedDistance.UseVisualStyleBackColor = true;
            // 
            // _rbVdwSum
            // 
            _rbVdwSum.AutoSize = true;
            _rbVdwSum.Font = new Font("Segoe UI", 9F);
            _rbVdwSum.ForeColor = Color.FromArgb(51, 51, 51);
            _rbVdwSum.Location = new Point(10, 52);
            _rbVdwSum.Name = "_rbVdwSum";
            _rbVdwSum.Size = new Size(145, 24);
            _rbVdwSum.TabIndex = 1;
            _rbVdwSum.TabStop = true;
            _rbVdwSum.Text = "Sum of vdW radii";
            _rbVdwSum.UseVisualStyleBackColor = true;
            // 
            // _chkIntermolecular
            // 
            _chkIntermolecular.Checked = true;
            _chkIntermolecular.CheckState = CheckState.Checked;
            _chkIntermolecular.Font = new Font("Segoe UI", 9F);
            _chkIntermolecular.ForeColor = Color.FromArgb(51, 51, 51);
            _chkIntermolecular.Location = new Point(10, 22);
            _chkIntermolecular.Name = "_chkIntermolecular";
            _chkIntermolecular.Size = new Size(200, 25);
            _chkIntermolecular.TabIndex = 0;
            _chkIntermolecular.Text = "Intermolecular";
            _chkIntermolecular.UseVisualStyleBackColor = true;
            // 
            // _chkIntramolecular
            // 
            _chkIntramolecular.Font = new Font("Segoe UI", 9F);
            _chkIntramolecular.ForeColor = Color.FromArgb(51, 51, 51);
            _chkIntramolecular.Location = new Point(10, 50);
            _chkIntramolecular.Name = "_chkIntramolecular";
            _chkIntramolecular.Size = new Size(250, 25);
            _chkIntramolecular.TabIndex = 1;
            _chkIntramolecular.Text = "Intramolecular (separated by > 3 bonds)";
            _chkIntramolecular.UseVisualStyleBackColor = true;
            // 
            // _chkSelectWholeMolecule
            // 
            _chkSelectWholeMolecule.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _chkSelectWholeMolecule.ForeColor = Color.FromArgb(0, 122, 204);
            _chkSelectWholeMolecule.Location = new Point(10, 78);
            _chkSelectWholeMolecule.Name = "_chkSelectWholeMolecule";
            _chkSelectWholeMolecule.Size = new Size(334, 25);
            _chkSelectWholeMolecule.TabIndex = 2;
            _chkSelectWholeMolecule.Text = "Select whole molecule (Expand Contacts)";
            _chkSelectWholeMolecule.UseVisualStyleBackColor = true;
            // 
            // _btnApply
            // 
            _btnApply.BackColor = Color.FromArgb(230, 230, 235);
            _btnApply.FlatStyle = FlatStyle.Flat;
            _btnApply.Font = new Font("Segoe UI", 9F);
            _btnApply.ForeColor = Color.FromArgb(51, 51, 51);
            _btnApply.Location = new Point(6, 26);
            _btnApply.Name = "_btnApply";
            _btnApply.Size = new Size(75, 29);
            _btnApply.TabIndex = 0;
            _btnApply.Text = "Apply";
            _btnApply.UseVisualStyleBackColor = false;
            // 
            // _btnOk
            // 
            _btnOk.BackColor = Color.FromArgb(0, 122, 204);
            _btnOk.FlatStyle = FlatStyle.Flat;
            _btnOk.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnOk.ForeColor = Color.White;
            _btnOk.Location = new Point(87, 26);
            _btnOk.Name = "_btnOk";
            _btnOk.Size = new Size(75, 29);
            _btnOk.TabIndex = 1;
            _btnOk.Text = "OK";
            _btnOk.UseVisualStyleBackColor = false;
            // 
            // _btnCancel
            // 
            _btnCancel.BackColor = Color.FromArgb(230, 230, 235);
            _btnCancel.FlatStyle = FlatStyle.Flat;
            _btnCancel.Font = new Font("Segoe UI", 9F);
            _btnCancel.ForeColor = Color.FromArgb(51, 51, 51);
            _btnCancel.Location = new Point(168, 26);
            _btnCancel.Name = "_btnCancel";
            _btnCancel.Size = new Size(75, 29);
            _btnCancel.TabIndex = 2;
            _btnCancel.Text = "Cancel";
            _btnCancel.UseVisualStyleBackColor = false;
            // 
            // _lblStatus
            // 
            _lblStatus.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            _lblStatus.ForeColor = Color.FromArgb(120, 120, 120);
            _lblStatus.Location = new Point(12, 417);
            _lblStatus.Name = "_lblStatus";
            _lblStatus.Size = new Size(350, 20);
            _lblStatus.TabIndex = 4;
            _lblStatus.Text = "Ready";
            _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _grpOptions
            // 
            _grpOptions.Controls.Add(_chkIntermolecular);
            _grpOptions.Controls.Add(_chkIntramolecular);
            _grpOptions.Controls.Add(_chkSelectWholeMolecule);
            _grpOptions.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpOptions.ForeColor = Color.FromArgb(51, 51, 51);
            _grpOptions.Location = new Point(12, 197);
            _grpOptions.Name = "_grpOptions";
            _grpOptions.Size = new Size(350, 110);
            _grpOptions.TabIndex = 2;
            _grpOptions.TabStop = false;
            _grpOptions.Text = "Options";
            // 
            // _grpActions
            // 
            _grpActions.Controls.Add(_btnApply);
            _grpActions.Controls.Add(_btnOk);
            _grpActions.Controls.Add(_btnCancel);
            _grpActions.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpActions.ForeColor = Color.FromArgb(51, 51, 51);
            _grpActions.Location = new Point(12, 335);
            _grpActions.Name = "_grpActions";
            _grpActions.Size = new Size(350, 61);
            _grpActions.TabIndex = 3;
            _grpActions.TabStop = false;
            _grpActions.Text = "Actions";
            // 
            // _grpMode
            // 
            _grpMode.Controls.Add(_rbFixedDistance);
            _grpMode.Controls.Add(_rbVdwSum);
            _grpMode.Controls.Add(_numVdwMinus);
            _grpMode.Controls.Add(_lblVdwMinusUnit);
            _grpMode.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpMode.ForeColor = Color.FromArgb(51, 51, 51);
            _grpMode.Location = new Point(12, 65);
            _grpMode.Name = "_grpMode";
            _grpMode.Size = new Size(350, 93);
            _grpMode.TabIndex = 1;
            _grpMode.TabStop = false;
            _grpMode.Text = "Contact Criteria";
            // 
            // ShortContactsWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(248, 248, 250);
            ClientSize = new Size(380, 446);
            Controls.Add(_lblTitle);
            Controls.Add(_grpMode);
            Controls.Add(_grpOptions);
            Controls.Add(_grpActions);
            Controls.Add(_lblStatus);
            MinimumSize = new Size(350, 280);
            Name = "ShortContactsWindow";
            Text = "Define Short Contacts";
            ((System.ComponentModel.ISupportInitialize)_numVdwMinus).EndInit();
            _grpOptions.ResumeLayout(false);
            _grpActions.ResumeLayout(false);
            _grpMode.ResumeLayout(false);
            _grpMode.PerformLayout();
            ResumeLayout(false);
        }
    }
}