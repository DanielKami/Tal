// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// SpaceGroupSettingDialog designer-generated layout.

﻿namespace Tal.Forms
{
    partial class SpaceGroupSettingDialog
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
            this.lblGroup = new System.Windows.Forms.Label();
            this.lblCurrent = new System.Windows.Forms.Label();
            this.lblTarget = new System.Windows.Forms.Label();
            this.cmbSettings = new System.Windows.Forms.ComboBox();
            this.lblInfo = new System.Windows.Forms.Label();
            this.btnTransform = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblGroup
            // 
            this.lblGroup.AutoSize = true;
            this.lblGroup.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblGroup.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(51)))), ((int)(((byte)(51)))));
            this.lblGroup.Location = new System.Drawing.Point(15, 15);
            this.lblGroup.Name = "lblGroup";
            this.lblGroup.Size = new System.Drawing.Size(130, 20);
            this.lblGroup.TabIndex = 0;
            this.lblGroup.Text = "Space group:  #14";
            // 
            // lblCurrent
            // 
            this.lblCurrent.AutoSize = true;
            this.lblCurrent.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCurrent.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblCurrent.Location = new System.Drawing.Point(15, 40);
            this.lblCurrent.Name = "lblCurrent";
            this.lblCurrent.Size = new System.Drawing.Size(180, 20);
            this.lblCurrent.TabIndex = 1;
            this.lblCurrent.Text = "Current setting:  P21/n";
            // 
            // lblTarget
            // 
            this.lblTarget.AutoSize = true;
            this.lblTarget.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblTarget.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(51)))), ((int)(((byte)(51)))));
            this.lblTarget.Location = new System.Drawing.Point(15, 75);
            this.lblTarget.Name = "lblTarget";
            this.lblTarget.Size = new System.Drawing.Size(100, 20);
            this.lblTarget.TabIndex = 2;
            this.lblTarget.Text = "Transform to:";
            // 
            // cmbSettings
            // 
            this.cmbSettings.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbSettings.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.cmbSettings.FormattingEnabled = true;
            this.cmbSettings.Location = new System.Drawing.Point(140, 72);
            this.cmbSettings.Name = "cmbSettings";
            this.cmbSettings.Size = new System.Drawing.Size(265, 28);
            this.cmbSettings.TabIndex = 3;
            // 
            // lblInfo
            // 
            this.lblInfo.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            this.lblInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.lblInfo.Location = new System.Drawing.Point(15, 115);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(390, 60);
            this.lblInfo.TabIndex = 4;
            this.lblInfo.Text = "The current structure will be modified in-place.\r\nAtomic coordinates, ADP tensors, cell parameters and\r\nsymmetry operations will be transformed.";
            // 
            // btnTransform
            // 
            this.btnTransform.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnTransform.FlatAppearance.BorderSize = 0;
            this.btnTransform.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(102)))), ((int)(((byte)(184)))));
            this.btnTransform.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.btnTransform.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnTransform.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnTransform.ForeColor = System.Drawing.Color.White;
            this.btnTransform.Location = new System.Drawing.Point(230, 185);
            this.btnTransform.Name = "btnTransform";
            this.btnTransform.Size = new System.Drawing.Size(85, 30);
            this.btnTransform.TabIndex = 5;
            this.btnTransform.Text = "Transform";
            this.btnTransform.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            this.btnCancel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(235)))));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(51)))), ((int)(((byte)(51)))));
            this.btnCancel.Location = new System.Drawing.Point(320, 185);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(85, 30);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = false;
            // 
            // SpaceGroupSettingDialog
            // 
            this.AcceptButton = this.btnTransform;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(250)))));
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(420, 230);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnTransform);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.cmbSettings);
            this.Controls.Add(this.lblTarget);
            this.Controls.Add(this.lblCurrent);
            this.Controls.Add(this.lblGroup);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SpaceGroupSettingDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Transform space group";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblGroup;
        private System.Windows.Forms.Label lblCurrent;
        private System.Windows.Forms.Label lblTarget;
        private System.Windows.Forms.ComboBox cmbSettings;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Button btnTransform;
        private System.Windows.Forms.Button btnCancel;
    }
}