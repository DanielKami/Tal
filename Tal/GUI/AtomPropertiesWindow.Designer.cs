// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// AtomPropertiesWindow designer-generated layout.

﻿namespace Tal.Forms
{
    partial class AtomPropertiesWindow
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.DataGridView dgvElements;
        private System.Windows.Forms.DataGridViewTextBoxColumn colZ;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSymbol;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colColor;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Button btnResetAll;
        private System.Windows.Forms.Button btnResetSelected;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            dgvElements = new DataGridView();
            colZ = new DataGridViewTextBoxColumn();
            colSymbol = new DataGridViewTextBoxColumn();
            colName = new DataGridViewTextBoxColumn();
            colColor = new DataGridViewTextBoxColumn();
            panelBottom = new Panel();
            lblInfo = new Label();
            btnResetAll = new Button();
            btnResetSelected = new Button();
            btnCancel = new Button();
            btnOK = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvElements).BeginInit();
            panelBottom.SuspendLayout();
            SuspendLayout();
            // 
            // dgvElements
            // 
            dgvElements.AllowUserToAddRows = false;
            dgvElements.AllowUserToDeleteRows = false;
            dgvElements.AllowUserToResizeRows = false;
            dgvElements.BackgroundColor = Color.White;
            dgvElements.BorderStyle = BorderStyle.None;
            dgvElements.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvElements.Columns.AddRange(new DataGridViewColumn[] { colZ, colSymbol, colName, colColor });
            dgvElements.Dock = DockStyle.Fill;
            dgvElements.Font = new Font("Segoe UI", 9F);
            dgvElements.Location = new Point(0, 0);
            dgvElements.Name = "dgvElements";
            dgvElements.RowHeadersVisible = false;
            dgvElements.RowHeadersWidth = 51;
            dgvElements.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvElements.Size = new Size(382, 407);
            dgvElements.TabIndex = 0;
            // 
            // colZ
            // 
            colZ.HeaderText = "Z";
            colZ.MinimumWidth = 6;
            colZ.Name = "colZ";
            colZ.ReadOnly = true;
            colZ.Width = 50;
            // 
            // colSymbol
            // 
            colSymbol.HeaderText = "Symbol";
            colSymbol.MinimumWidth = 6;
            colSymbol.Name = "colSymbol";
            colSymbol.ReadOnly = true;
            colSymbol.Width = 80;
            // 
            // colName
            // 
            colName.HeaderText = "Name";
            colName.MinimumWidth = 6;
            colName.Name = "colName";
            colName.ReadOnly = true;
            colName.Width = 160;
            // 
            // colColor
            // 
            colColor.HeaderText = "Color";
            colColor.MinimumWidth = 6;
            colColor.Name = "colColor";
            colColor.ReadOnly = true;
            colColor.Width = 180;
            // 
            // panelBottom
            // 
            panelBottom.BackColor = Color.FromArgb(248, 248, 250);
            panelBottom.Controls.Add(lblInfo);
            panelBottom.Controls.Add(btnResetAll);
            panelBottom.Controls.Add(btnResetSelected);
            panelBottom.Controls.Add(btnCancel);
            panelBottom.Controls.Add(btnOK);
            panelBottom.Dock = DockStyle.Bottom;
            panelBottom.Location = new Point(0, 407);
            panelBottom.Name = "panelBottom";
            panelBottom.Size = new Size(382, 69);
            panelBottom.TabIndex = 1;
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            lblInfo.ForeColor = Color.FromArgb(120, 120, 120);
            lblInfo.Location = new Point(71, 47);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(156, 19);
            lblInfo.TabIndex = 0;
            lblInfo.Text = "Click a color cell to edit";
            // 
            // btnResetAll
            // 
            btnResetAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnResetAll.BackColor = Color.FromArgb(230, 230, 235);
            btnResetAll.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnResetAll.FlatStyle = FlatStyle.Flat;
            btnResetAll.Location = new Point(111, 12);
            btnResetAll.Name = "btnResetAll";
            btnResetAll.Size = new Size(87, 32);
            btnResetAll.TabIndex = 4;
            btnResetAll.Text = "Reset All";
            btnResetAll.UseVisualStyleBackColor = false;
            btnResetAll.Click += BtnResetAll_Click;
            // 
            // btnResetSelected
            // 
            btnResetSelected.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnResetSelected.BackColor = Color.FromArgb(230, 230, 235);
            btnResetSelected.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnResetSelected.FlatStyle = FlatStyle.Flat;
            btnResetSelected.Location = new Point(45, 12);
            btnResetSelected.Name = "btnResetSelected";
            btnResetSelected.Size = new Size(60, 32);
            btnResetSelected.TabIndex = 3;
            btnResetSelected.Text = "Reset Selected";
            btnResetSelected.UseVisualStyleBackColor = false;
            btnResetSelected.Click += BtnResetSelected_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.BackColor = Color.FromArgb(230, 230, 235);
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Location = new Point(204, 12);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(80, 32);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            btnCancel.Click += BtnCancel_Click;
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOK.BackColor = Color.FromArgb(230, 230, 235);
            btnOK.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnOK.FlatStyle = FlatStyle.Flat;
            btnOK.Location = new Point(290, 12);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(80, 32);
            btnOK.TabIndex = 1;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = false;
            btnOK.Click += BtnOK_Click;
            // 
            // AtomPropertiesWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(382, 476);
            Controls.Add(dgvElements);
            Controls.Add(panelBottom);
            Font = new Font("Segoe UI", 9F);
            Name = "AtomPropertiesWindow";
            Text = "Atom Colors";
            ((System.ComponentModel.ISupportInitialize)dgvElements).EndInit();
            panelBottom.ResumeLayout(false);
            panelBottom.PerformLayout();
            ResumeLayout(false);
        }
    }
}