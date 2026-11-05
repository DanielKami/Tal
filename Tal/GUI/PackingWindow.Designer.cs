// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// PackingWindow designer-generated layout.

﻿namespace Tal.Forms
{
    partial class PackingWindow
    {
        private System.ComponentModel.IContainer components = null;

        // ============================================================
        // DEKLARACJE KONTROLEK
        // ============================================================

        // X axis
        private System.Windows.Forms.NumericUpDown numMinX;
        private System.Windows.Forms.NumericUpDown numMaxX;
        private System.Windows.Forms.Button btnXPlusHalf;
        private System.Windows.Forms.Button btnX0to2;
        private System.Windows.Forms.Button btnX0to3;

        // Y axis
        private System.Windows.Forms.NumericUpDown numMinY;
        private System.Windows.Forms.NumericUpDown numMaxY;
        private System.Windows.Forms.Button btnYPlusHalf;
        private System.Windows.Forms.Button btnY0to2;
        private System.Windows.Forms.Button btnY0to3;

        // Z axis
        private System.Windows.Forms.NumericUpDown numMinZ;
        private System.Windows.Forms.NumericUpDown numMaxZ;
        private System.Windows.Forms.Button btnZPlusHalf;
        private System.Windows.Forms.Button btnZ0to2;
        private System.Windows.Forms.Button btnZ0to3;

        // Przyciski globalne
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Button btnAllPlusHalf;
        private System.Windows.Forms.Button btnAll0to2;
        private System.Windows.Forms.Button btnAll0to3;

        // Checkbox Packing
        private System.Windows.Forms.CheckBox chkPacking;

        // Etykiety
        private System.Windows.Forms.Label lblMin;
        private System.Windows.Forms.Label lblMax;
        private System.Windows.Forms.Label lblX;
        private System.Windows.Forms.Label lblY;
        private System.Windows.Forms.Label lblZ;
        private System.Windows.Forms.Label lblInfo;

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
            numMinX = new NumericUpDown();
            numMaxX = new NumericUpDown();
            btnXPlusHalf = new Button();
            btnX0to2 = new Button();
            btnX0to3 = new Button();
            numMinY = new NumericUpDown();
            numMaxY = new NumericUpDown();
            btnYPlusHalf = new Button();
            btnY0to2 = new Button();
            btnY0to3 = new Button();
            numMinZ = new NumericUpDown();
            numMaxZ = new NumericUpDown();
            btnZPlusHalf = new Button();
            btnZ0to2 = new Button();
            btnZ0to3 = new Button();
            btnReset = new Button();
            btnAllPlusHalf = new Button();
            btnAll0to2 = new Button();
            btnAll0to3 = new Button();
            chkPacking = new CheckBox();
            lblMin = new Label();
            lblMax = new Label();
            lblX = new Label();
            lblY = new Label();
            lblZ = new Label();
            lblInfo = new Label();
            ((System.ComponentModel.ISupportInitialize)numMinX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxX).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMinY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMinZ).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxZ).BeginInit();
            SuspendLayout();
            // 
            // numMinX
            // 
            numMinX.BackColor = Color.White;
            numMinX.BorderStyle = BorderStyle.FixedSingle;
            numMinX.DecimalPlaces = 1;
            numMinX.ForeColor = Color.FromArgb(51, 51, 51);
            numMinX.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numMinX.Location = new Point(24, 127);
            numMinX.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            numMinX.Minimum = new decimal(new int[] { 5, 0, 0, int.MinValue });
            numMinX.Name = "numMinX";
            numMinX.Size = new Size(55, 27);
            numMinX.TabIndex = 0;
            numMinX.TextAlign = HorizontalAlignment.Center;
            // ⭐ ZDARZENIE ⭐
            numMinX.ValueChanged += NumMinX_ValueChanged;
            // 
            // numMaxX
            // 
            numMaxX.BackColor = Color.White;
            numMaxX.BorderStyle = BorderStyle.FixedSingle;
            numMaxX.DecimalPlaces = 1;
            numMaxX.ForeColor = Color.FromArgb(51, 51, 51);
            numMaxX.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numMaxX.Location = new Point(84, 127);
            numMaxX.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            numMaxX.Minimum = new decimal(new int[] { 5, 0, 0, int.MinValue });
            numMaxX.Name = "numMaxX";
            numMaxX.Size = new Size(55, 27);
            numMaxX.TabIndex = 1;
            numMaxX.TextAlign = HorizontalAlignment.Center;
            numMaxX.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // ⭐ ZDARZENIE ⭐
            numMaxX.ValueChanged += NumMaxX_ValueChanged;
            // 
            // numMinY
            // 
            numMinY.BackColor = Color.White;
            numMinY.BorderStyle = BorderStyle.FixedSingle;
            numMinY.DecimalPlaces = 1;
            numMinY.ForeColor = Color.FromArgb(51, 51, 51);
            numMinY.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numMinY.Location = new Point(24, 167);
            numMinY.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            numMinY.Minimum = new decimal(new int[] { 5, 0, 0, int.MinValue });
            numMinY.Name = "numMinY";
            numMinY.Size = new Size(55, 27);
            numMinY.TabIndex = 2;
            numMinY.TextAlign = HorizontalAlignment.Center;
            // ⭐ ZDARZENIE ⭐
            numMinY.ValueChanged += NumMinY_ValueChanged;
            // 
            // numMaxY
            // 
            numMaxY.BackColor = Color.White;
            numMaxY.BorderStyle = BorderStyle.FixedSingle;
            numMaxY.DecimalPlaces = 1;
            numMaxY.ForeColor = Color.FromArgb(51, 51, 51);
            numMaxY.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numMaxY.Location = new Point(84, 167);
            numMaxY.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            numMaxY.Minimum = new decimal(new int[] { 5, 0, 0, int.MinValue });
            numMaxY.Name = "numMaxY";
            numMaxY.Size = new Size(55, 27);
            numMaxY.TabIndex = 3;
            numMaxY.TextAlign = HorizontalAlignment.Center;
            numMaxY.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // ⭐ ZDARZENIE ⭐
            numMaxY.ValueChanged += NumMaxY_ValueChanged;
            // 
            // numMinZ
            // 
            numMinZ.BackColor = Color.White;
            numMinZ.BorderStyle = BorderStyle.FixedSingle;
            numMinZ.DecimalPlaces = 1;
            numMinZ.ForeColor = Color.FromArgb(51, 51, 51);
            numMinZ.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numMinZ.Location = new Point(24, 207);
            numMinZ.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            numMinZ.Minimum = new decimal(new int[] { 5, 0, 0, int.MinValue });
            numMinZ.Name = "numMinZ";
            numMinZ.Size = new Size(55, 27);
            numMinZ.TabIndex = 4;
            numMinZ.TextAlign = HorizontalAlignment.Center;
            // ⭐ ZDARZENIE ⭐
            numMinZ.ValueChanged += NumMinZ_ValueChanged;
            // 
            // numMaxZ
            // 
            numMaxZ.BackColor = Color.White;
            numMaxZ.BorderStyle = BorderStyle.FixedSingle;
            numMaxZ.DecimalPlaces = 1;
            numMaxZ.ForeColor = Color.FromArgb(51, 51, 51);
            numMaxZ.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            numMaxZ.Location = new Point(84, 207);
            numMaxZ.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            numMaxZ.Minimum = new decimal(new int[] { 5, 0, 0, int.MinValue });
            numMaxZ.Name = "numMaxZ";
            numMaxZ.Size = new Size(55, 27);
            numMaxZ.TabIndex = 5;
            numMaxZ.TextAlign = HorizontalAlignment.Center;
            numMaxZ.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // ⭐ ZDARZENIE ⭐
            numMaxZ.ValueChanged += NumMaxZ_ValueChanged;
            // 
            // btnXPlusHalf
            // 
            btnXPlusHalf.BackColor = Color.FromArgb(230, 230, 235);
            btnXPlusHalf.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnXPlusHalf.FlatStyle = FlatStyle.Flat;
            btnXPlusHalf.Font = new Font("Segoe UI", 7F);
            btnXPlusHalf.ForeColor = Color.FromArgb(51, 51, 51);
            btnXPlusHalf.Location = new Point(144, 127);
            btnXPlusHalf.Name = "btnXPlusHalf";
            btnXPlusHalf.Size = new Size(42, 26);
            btnXPlusHalf.TabIndex = 6;
            btnXPlusHalf.Tag = "X";
            btnXPlusHalf.Text = "+0.5";
            btnXPlusHalf.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnXPlusHalf.Click += BtnXPlusHalf_Click;
            // 
            // btnX0to2
            // 
            btnX0to2.BackColor = Color.FromArgb(230, 230, 235);
            btnX0to2.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnX0to2.FlatStyle = FlatStyle.Flat;
            btnX0to2.Font = new Font("Segoe UI", 7F);
            btnX0to2.ForeColor = Color.FromArgb(51, 51, 51);
            btnX0to2.Location = new Point(190, 127);
            btnX0to2.Name = "btnX0to2";
            btnX0to2.Size = new Size(42, 26);
            btnX0to2.TabIndex = 7;
            btnX0to2.Tag = "X";
            btnX0to2.Text = "0→2";
            btnX0to2.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnX0to2.Click += BtnX0to2_Click;
            // 
            // btnX0to3
            // 
            btnX0to3.BackColor = Color.FromArgb(230, 230, 235);
            btnX0to3.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnX0to3.FlatStyle = FlatStyle.Flat;
            btnX0to3.Font = new Font("Segoe UI", 7F);
            btnX0to3.ForeColor = Color.FromArgb(51, 51, 51);
            btnX0to3.Location = new Point(236, 127);
            btnX0to3.Name = "btnX0to3";
            btnX0to3.Size = new Size(42, 26);
            btnX0to3.TabIndex = 8;
            btnX0to3.Tag = "X";
            btnX0to3.Text = "0→3";
            btnX0to3.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnX0to3.Click += BtnX0to3_Click;
            // 
            // btnYPlusHalf
            // 
            btnYPlusHalf.BackColor = Color.FromArgb(230, 230, 235);
            btnYPlusHalf.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnYPlusHalf.FlatStyle = FlatStyle.Flat;
            btnYPlusHalf.Font = new Font("Segoe UI", 7F);
            btnYPlusHalf.ForeColor = Color.FromArgb(51, 51, 51);
            btnYPlusHalf.Location = new Point(144, 167);
            btnYPlusHalf.Name = "btnYPlusHalf";
            btnYPlusHalf.Size = new Size(42, 26);
            btnYPlusHalf.TabIndex = 9;
            btnYPlusHalf.Tag = "Y";
            btnYPlusHalf.Text = "+0.5";
            btnYPlusHalf.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnYPlusHalf.Click += BtnYPlusHalf_Click;
            // 
            // btnY0to2
            // 
            btnY0to2.BackColor = Color.FromArgb(230, 230, 235);
            btnY0to2.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnY0to2.FlatStyle = FlatStyle.Flat;
            btnY0to2.Font = new Font("Segoe UI", 7F);
            btnY0to2.ForeColor = Color.FromArgb(51, 51, 51);
            btnY0to2.Location = new Point(190, 167);
            btnY0to2.Name = "btnY0to2";
            btnY0to2.Size = new Size(42, 26);
            btnY0to2.TabIndex = 10;
            btnY0to2.Tag = "Y";
            btnY0to2.Text = "0→2";
            btnY0to2.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnY0to2.Click += BtnY0to2_Click;
            // 
            // btnY0to3
            // 
            btnY0to3.BackColor = Color.FromArgb(230, 230, 235);
            btnY0to3.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnY0to3.FlatStyle = FlatStyle.Flat;
            btnY0to3.Font = new Font("Segoe UI", 7F);
            btnY0to3.ForeColor = Color.FromArgb(51, 51, 51);
            btnY0to3.Location = new Point(236, 167);
            btnY0to3.Name = "btnY0to3";
            btnY0to3.Size = new Size(42, 26);
            btnY0to3.TabIndex = 11;
            btnY0to3.Tag = "Y";
            btnY0to3.Text = "0→3";
            btnY0to3.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnY0to3.Click += BtnY0to3_Click;
            // 
            // btnZPlusHalf
            // 
            btnZPlusHalf.BackColor = Color.FromArgb(230, 230, 235);
            btnZPlusHalf.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnZPlusHalf.FlatStyle = FlatStyle.Flat;
            btnZPlusHalf.Font = new Font("Segoe UI", 7F);
            btnZPlusHalf.ForeColor = Color.FromArgb(51, 51, 51);
            btnZPlusHalf.Location = new Point(144, 207);
            btnZPlusHalf.Name = "btnZPlusHalf";
            btnZPlusHalf.Size = new Size(42, 26);
            btnZPlusHalf.TabIndex = 12;
            btnZPlusHalf.Tag = "Z";
            btnZPlusHalf.Text = "+0.5";
            btnZPlusHalf.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnZPlusHalf.Click += BtnZPlusHalf_Click;
            // 
            // btnZ0to2
            // 
            btnZ0to2.BackColor = Color.FromArgb(230, 230, 235);
            btnZ0to2.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnZ0to2.FlatStyle = FlatStyle.Flat;
            btnZ0to2.Font = new Font("Segoe UI", 7F);
            btnZ0to2.ForeColor = Color.FromArgb(51, 51, 51);
            btnZ0to2.Location = new Point(190, 207);
            btnZ0to2.Name = "btnZ0to2";
            btnZ0to2.Size = new Size(42, 26);
            btnZ0to2.TabIndex = 13;
            btnZ0to2.Tag = "Z";
            btnZ0to2.Text = "0→2";
            btnZ0to2.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnZ0to2.Click += BtnZ0to2_Click;
            // 
            // btnZ0to3
            // 
            btnZ0to3.BackColor = Color.FromArgb(230, 230, 235);
            btnZ0to3.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnZ0to3.FlatStyle = FlatStyle.Flat;
            btnZ0to3.Font = new Font("Segoe UI", 7F);
            btnZ0to3.ForeColor = Color.FromArgb(51, 51, 51);
            btnZ0to3.Location = new Point(236, 207);
            btnZ0to3.Name = "btnZ0to3";
            btnZ0to3.Size = new Size(42, 26);
            btnZ0to3.TabIndex = 14;
            btnZ0to3.Tag = "Z";
            btnZ0to3.Text = "0→3";
            btnZ0to3.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnZ0to3.Click += BtnZ0to3_Click;
            // 
            // btnAllPlusHalf
            // 
            btnAllPlusHalf.BackColor = Color.FromArgb(230, 230, 235);
            btnAllPlusHalf.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnAllPlusHalf.FlatStyle = FlatStyle.Flat;
            btnAllPlusHalf.ForeColor = Color.FromArgb(51, 51, 51);
            btnAllPlusHalf.Location = new Point(4, 254);
            btnAllPlusHalf.Name = "btnAllPlusHalf";
            btnAllPlusHalf.Size = new Size(80, 28);
            btnAllPlusHalf.TabIndex = 15;
            btnAllPlusHalf.Text = "All +0.5";
            btnAllPlusHalf.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnAllPlusHalf.Click += BtnAllPlusHalf_Click;
            // 
            // btnAll0to2
            // 
            btnAll0to2.BackColor = Color.FromArgb(230, 230, 235);
            btnAll0to2.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnAll0to2.FlatStyle = FlatStyle.Flat;
            btnAll0to2.ForeColor = Color.FromArgb(51, 51, 51);
            btnAll0to2.Location = new Point(89, 254);
            btnAll0to2.Name = "btnAll0to2";
            btnAll0to2.Size = new Size(80, 28);
            btnAll0to2.TabIndex = 16;
            btnAll0to2.Text = "All 0→2";
            btnAll0to2.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnAll0to2.Click += BtnAll0to2_Click;
            // 
            // btnAll0to3
            // 
            btnAll0to3.BackColor = Color.FromArgb(230, 230, 235);
            btnAll0to3.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnAll0to3.FlatStyle = FlatStyle.Flat;
            btnAll0to3.ForeColor = Color.FromArgb(51, 51, 51);
            btnAll0to3.Location = new Point(174, 254);
            btnAll0to3.Name = "btnAll0to3";
            btnAll0to3.Size = new Size(80, 28);
            btnAll0to3.TabIndex = 17;
            btnAll0to3.Text = "All 0→3";
            btnAll0to3.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnAll0to3.Click += BtnAll0to3_Click;
            // 
            // btnReset
            // 
            btnReset.BackColor = Color.FromArgb(230, 230, 235);
            btnReset.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnReset.FlatStyle = FlatStyle.Flat;
            btnReset.ForeColor = Color.FromArgb(51, 51, 51);
            btnReset.Location = new Point(4, 294);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(120, 32);
            btnReset.TabIndex = 18;
            btnReset.Text = "Reset";
            btnReset.UseVisualStyleBackColor = false;
            // ⭐ ZDARZENIE ⭐
            btnReset.Click += BtnReset_Click;
            // 
            // chkPacking
            // 
            chkPacking.Font = new Font("Segoe UI", 10F);
            chkPacking.ForeColor = Color.FromArgb(51, 51, 51);
            chkPacking.Location = new Point(12, 22);
            chkPacking.Name = "chkPacking";
            chkPacking.Size = new Size(100, 41);
            chkPacking.TabIndex = 20;
            chkPacking.Text = "Packing";
            chkPacking.UseVisualStyleBackColor = true;
            // ⭐ ZDARZENIE ⭐
            chkPacking.CheckedChanged += ChkPacking_CheckedChanged;
            // 
            // lblMin
            // 
            lblMin.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblMin.ForeColor = Color.FromArgb(51, 51, 51);
            lblMin.Location = new Point(24, 104);
            lblMin.Name = "lblMin";
            lblMin.Size = new Size(42, 20);
            lblMin.TabIndex = 21;
            lblMin.Text = "← Min";
            lblMin.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblMax
            // 
            lblMax.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblMax.ForeColor = Color.FromArgb(51, 51, 51);
            lblMax.Location = new Point(84, 104);
            lblMax.Name = "lblMax";
            lblMax.Size = new Size(40, 20);
            lblMax.TabIndex = 22;
            lblMax.Text = "Max →";
            lblMax.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblX
            // 
            lblX.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblX.ForeColor = Color.FromArgb(200, 50, 50);
            lblX.Location = new Point(4, 129);
            lblX.Name = "lblX";
            lblX.Size = new Size(18, 20);
            lblX.TabIndex = 23;
            lblX.Text = "X";
            lblX.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblY
            // 
            lblY.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblY.ForeColor = Color.FromArgb(50, 160, 50);
            lblY.Location = new Point(4, 169);
            lblY.Name = "lblY";
            lblY.Size = new Size(18, 20);
            lblY.TabIndex = 24;
            lblY.Text = "Y";
            lblY.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblZ
            // 
            lblZ.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblZ.ForeColor = Color.FromArgb(50, 50, 200);
            lblZ.Location = new Point(4, 209);
            lblZ.Name = "lblZ";
            lblZ.Size = new Size(18, 20);
            lblZ.TabIndex = 25;
            lblZ.Text = "Z";
            lblZ.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblInfo
            // 
            lblInfo.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            lblInfo.ForeColor = Color.FromArgb(120, 120, 120);
            lblInfo.Location = new Point(12, 66);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(250, 20);
            lblInfo.TabIndex = 26;
            lblInfo.Text = "Set min/max for each axis";
            // 
            // PackingWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(248, 248, 250);
            ClientSize = new Size(295, 370);
            Controls.Add(numMinX);
            Controls.Add(numMaxX);
            Controls.Add(numMinY);
            Controls.Add(numMaxY);
            Controls.Add(numMinZ);
            Controls.Add(numMaxZ);
            Controls.Add(btnXPlusHalf);
            Controls.Add(btnX0to2);
            Controls.Add(btnX0to3);
            Controls.Add(btnYPlusHalf);
            Controls.Add(btnY0to2);
            Controls.Add(btnY0to3);
            Controls.Add(btnZPlusHalf);
            Controls.Add(btnZ0to2);
            Controls.Add(btnZ0to3);
            Controls.Add(btnAllPlusHalf);
            Controls.Add(btnAll0to2);
            Controls.Add(btnAll0to3);
            Controls.Add(btnReset);
            Controls.Add(chkPacking);
            Controls.Add(lblMin);
            Controls.Add(lblMax);
            Controls.Add(lblX);
            Controls.Add(lblY);
            Controls.Add(lblZ);
            Controls.Add(lblInfo);
            ForeColor = Color.FromArgb(51, 51, 51);
            Name = "PackingWindow";
            Text = "Packing";
            ((System.ComponentModel.ISupportInitialize)numMinX).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxX).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMinY).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxY).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMinZ).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxZ).EndInit();
            ResumeLayout(false);
        }
    }
}