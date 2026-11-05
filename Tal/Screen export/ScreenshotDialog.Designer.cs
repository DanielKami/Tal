// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Screen capture / exafterrt: ScreenshotDialog.Designer.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal.Forms
{
    partial class ScreenshotDialog
    {
 /// <summary>
 /// Required designer variable.
 /// </summary>
        private System.ComponentModel.IContainer components = null;

 /// <summary>
 /// Clean up any resources being used.
 /// </summary>
 /// <param name="disaftersing">true if managed resources should be disaftersed; otherwise, false.</param>
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
 /// Required method for Designer supafterrt - do not modify
 /// the contents of this method with the code editor.
 /// </summary>
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblScale = new System.Windows.Forms.Label();
            this.cmbScale = new System.Windows.Forms.ComboBox();
            this.lblResolution = new System.Windows.Forms.Label();
            this.grpContent = new System.Windows.Forms.GroupBox();
            this.radioFull = new System.Windows.Forms.RadioButton();
            this.radioScene = new System.Windows.Forms.RadioButton();
            this.grpAdvanced = new System.Windows.Forms.GroupBox();
            this.chkSupersample = new System.Windows.Forms.CheckBox();
            this.lblInfo = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.grpContent.SuspendLayout();
            this.grpAdvanced.SuspendLayout();
            this.SuspendLayout();
 // 
 // lblTitle
 // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(51)))), ((int)(((byte)(51)))));
            this.lblTitle.Location = new System.Drawing.Point(15, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(134, 21);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Save Screenshot";
 // 
 // lblScale
 // 
            this.lblScale.AutoSize = true;
            this.lblScale.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.lblScale.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(51)))), ((int)(((byte)(51)))));
            this.lblScale.Location = new System.Drawing.Point(18, 55);
            this.lblScale.Name = "lblScale";
            this.lblScale.Size = new System.Drawing.Size(37, 15);
            this.lblScale.TabIndex = 1;
            this.lblScale.Text = "Scale:";
 // 
 // cmbScale
 // 
            this.cmbScale.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbScale.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbScale.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.cmbScale.FormattingEnabled = true;
            this.cmbScale.Location = new System.Drawing.Point(80, 52);
            this.cmbScale.Name = "cmbScale";
            this.cmbScale.Size = new System.Drawing.Size(120, 23);
            this.cmbScale.TabIndex = 2;
 // 
 // lblResolution
 // 
            this.lblResolution.AutoSize = true;
            this.lblResolution.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.lblResolution.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(100)))), ((int)(((byte)(160)))));
            this.lblResolution.Location = new System.Drawing.Point(210, 55);
            this.lblResolution.Name = "lblResolution";
            this.lblResolution.Size = new System.Drawing.Size(95, 15);
            this.lblResolution.TabIndex = 3;
            this.lblResolution.Text = "→ 1200 × 800 px";
 // 
 // grpContent
 // 
            this.grpContent.Controls.Add(this.radioFull);
            this.grpContent.Controls.Add(this.radioScene);
            this.grpContent.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.grpContent.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(51)))), ((int)(((byte)(51)))));
            this.grpContent.Location = new System.Drawing.Point(15, 90);
            this.grpContent.Name = "grpContent";
            this.grpContent.Size = new System.Drawing.Size(340, 70);
            this.grpContent.TabIndex = 4;
            this.grpContent.TabStop = false;
            this.grpContent.Text = "Content";
 // 
 // radioFull
 // 
            this.radioFull.AutoSize = true;
            this.radioFull.Checked = true;
            this.radioFull.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.radioFull.Location = new System.Drawing.Point(12, 20);
            this.radioFull.Name = "radioFull";
            this.radioFull.Size = new System.Drawing.Size(245, 19);
            this.radioFull.TabIndex = 0;
            this.radioFull.TabStop = true;
            this.radioFull.Text = "Full frame  (3D + labels + color scale)";
            this.radioFull.UseVisualStyleBackColor = true;
 // 
 // radioScene
 // 
            this.radioScene.AutoSize = true;
            this.radioScene.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.radioScene.Location = new System.Drawing.Point(12, 42);
            this.radioScene.Name = "radioScene";
            this.radioScene.Size = new System.Drawing.Size(135, 19);
            this.radioScene.TabIndex = 1;
            this.radioScene.Text = "Scene only  (pure 3D)";
            this.radioScene.UseVisualStyleBackColor = true;
 // 
 // grpAdvanced
 // 
            this.grpAdvanced.Controls.Add(this.chkSupersample);
            this.grpAdvanced.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.grpAdvanced.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(51)))), ((int)(((byte)(51)))));
            this.grpAdvanced.Location = new System.Drawing.Point(15, 168);
            this.grpAdvanced.Name = "grpAdvanced";
            this.grpAdvanced.Size = new System.Drawing.Size(340, 55);
            this.grpAdvanced.TabIndex = 5;
            this.grpAdvanced.TabStop = false;
            this.grpAdvanced.Text = "Advanced";
 // 
 // chkSupersample
 // 
            this.chkSupersample.AutoSize = true;
            this.chkSupersample.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.chkSupersample.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this.chkSupersample.Location = new System.Drawing.Point(12, 22);
            this.chkSupersample.Name = "chkSupersample";
            this.chkSupersample.Size = new System.Drawing.Size(290, 19);
            this.chkSupersample.TabIndex = 0;
            this.chkSupersample.Text = "Supersample  (better anti-aliasing, slower)";
            this.chkSupersample.UseVisualStyleBackColor = true;
 // 
 // lblInfo
 // 
            this.lblInfo.AutoSize = true;
            this.lblInfo.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.lblInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.lblInfo.Location = new System.Drawing.Point(18, 230);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(326, 13);
            this.lblInfo.TabIndex = 6;
            this.lblInfo.Text = "Everything (atoms, labels, color scale) scales proportionally.";
 // 
 // btnSave
 // 
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnSave.FlatAppearance.BorderSize = 0;
            this.btnSave.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(28)))), ((int)(((byte)(151)))), ((int)(((byte)(234)))));
            this.btnSave.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(102)))), ((int)(((byte)(184)))));
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnSave.ForeColor = System.Drawing.Color.White;
            this.btnSave.Location = new System.Drawing.Point(180, 258);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(85, 30);
            this.btnSave.TabIndex = 7;
            this.btnSave.Text = "Save...";
            this.btnSave.UseVisualStyleBackColor = false;
 // 
 // btnCancel
 // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.btnCancel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(51)))), ((int)(((byte)(51)))));
            this.btnCancel.Location = new System.Drawing.Point(270, 258);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(85, 30);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
 // 
 // ScreenshotDialog
 // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(248)))));
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(370, 300);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.grpAdvanced);
            this.Controls.Add(this.grpContent);
            this.Controls.Add(this.lblResolution);
            this.Controls.Add(this.cmbScale);
            this.Controls.Add(this.lblScale);
            this.Controls.Add(this.lblTitle);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ScreenshotDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Save Screenshot";
            this.grpContent.ResumeLayout(false);
            this.grpContent.PerformLayout();
            this.grpAdvanced.ResumeLayout(false);
            this.grpAdvanced.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblScale;
        private System.Windows.Forms.ComboBox cmbScale;
        private System.Windows.Forms.Label lblResolution;
        private System.Windows.Forms.GroupBox grpContent;
        private System.Windows.Forms.RadioButton radioFull;
        private System.Windows.Forms.RadioButton radioScene;
        private System.Windows.Forms.GroupBox grpAdvanced;
        private System.Windows.Forms.CheckBox chkSupersample;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}