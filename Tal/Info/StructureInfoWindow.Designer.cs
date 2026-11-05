// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure info UI: StructureInfoWindow.Designer.

﻿namespace Tal.Forms
{
    partial class StructureInfoWindow
    {
        private System.ComponentModel.IContainer components = null;

 

        private void InitializeComponent()
        {
            this.panelTop = new System.Windows.Forms.Panel();
            this.txtCurrentStructure = new System.Windows.Forms.TextBox();
            this.lblCurrentStructure = new System.Windows.Forms.Label();
            this.btnCustomise = new System.Windows.Forms.Button();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.btnStructure = new System.Windows.Forms.Button();
            this.btnDiagram = new System.Windows.Forms.Button();
            this.btnSmiles = new System.Windows.Forms.Button();
            this.btnAtoms = new System.Windows.Forms.Button();
            this.btnBonds = new System.Windows.Forms.Button();
            this.btnContacts = new System.Windows.Forms.Button();
            this.btnCentroids = new System.Windows.Forms.Button();
            this.btnPlanes = new System.Windows.Forms.Button();
            this.btnSymmetry = new System.Windows.Forms.Button();
            this.btnDistances = new System.Windows.Forms.Button();
            this.btnAngles = new System.Windows.Forms.Button();
            this.btnTorsions = new System.Windows.Forms.Button();
            this.btnAllAngles = new System.Windows.Forms.Button();
            this.btnAllTorsions = new System.Windows.Forms.Button();
            this.panelTableScroll = new System.Windows.Forms.Panel();
            this.table = new System.Windows.Forms.TableLayoutPanel();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnCopySmiles = new System.Windows.Forms.Button();
            this.btnExportExcel = new System.Windows.Forms.Button();
            this.btnHBonds = new System.Windows.Forms.Button();
            this.btnHBondTopo = new System.Windows.Forms.Button();
            this.panelTop.SuspendLayout();
            this.panelButtons.SuspendLayout();
            this.panelTableScroll.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();

 // 
 // panelTop
 // 
            this.panelTop.Controls.Add(this.btnCustomise);
            this.panelTop.Controls.Add(this.txtCurrentStructure);
            this.panelTop.Controls.Add(this.lblCurrentStructure);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.panelTop.Size = new System.Drawing.Size(900, 60);
            this.panelTop.TabIndex = 0;

 // 
 // lblCurrentStructure
 // 
            this.lblCurrentStructure.AutoSize = true;
            this.lblCurrentStructure.Location = new System.Drawing.Point(12, 10);
            this.lblCurrentStructure.Name = "lblCurrentStructure";
            this.lblCurrentStructure.Size = new System.Drawing.Size(110, 15);
            this.lblCurrentStructure.TabIndex = 0;
            this.lblCurrentStructure.Text = "Current structure:";

 // 
 // txtCurrentStructure
 // 
            this.txtCurrentStructure.Location = new System.Drawing.Point(130, 7);
            this.txtCurrentStructure.Name = "txtCurrentStructure";
            this.txtCurrentStructure.ReadOnly = true;
            this.txtCurrentStructure.Size = new System.Drawing.Size(500, 23);
            this.txtCurrentStructure.TabIndex = 1;

 // 
 // btnCustomise
 // 
            this.btnCustomise.Location = new System.Drawing.Point(12, 33);
            this.btnCustomise.Name = "btnCustomise";
            this.btnCustomise.Size = new System.Drawing.Size(90, 23);
            this.btnCustomise.TabIndex = 2;
            this.btnCustomise.Text = "Customise...";
            this.btnCustomise.UseVisualStyleBackColor = true;

 // 
 // panelButtons
 // 
            this.panelButtons.AutoScroll = true;
            this.panelButtons.BackColor = System.Drawing.SystemColors.Control;
            this.panelButtons.Controls.Add(this.btnStructure);
            this.panelButtons.Controls.Add(this.btnDiagram);
            this.panelButtons.Controls.Add(this.btnSmiles);
            this.panelButtons.Controls.Add(this.btnAtoms);
            this.panelButtons.Controls.Add(this.btnBonds);
            this.panelButtons.Controls.Add(this.btnContacts);
            this.panelButtons.Controls.Add(this.btnCentroids);
            this.panelButtons.Controls.Add(this.btnPlanes);
            this.panelButtons.Controls.Add(this.btnSymmetry);
            this.panelButtons.Controls.Add(this.btnDistances);
            this.panelButtons.Controls.Add(this.btnAngles);
            this.panelButtons.Controls.Add(this.btnTorsions);
            this.panelButtons.Controls.Add(this.btnAllAngles);
            this.panelButtons.Controls.Add(this.btnAllTorsions);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelButtons.Location = new System.Drawing.Point(0, 60);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Padding = new System.Windows.Forms.Padding(6);
            this.panelButtons.Size = new System.Drawing.Size(130, 540);
            this.panelButtons.TabIndex = 1;

 // 
 // btnStructure
 // 
            this.btnStructure.Location = new System.Drawing.Point(6, 6);
            this.btnStructure.Name = "btnStructure";
            this.btnStructure.Size = new System.Drawing.Size(110, 30);
            this.btnStructure.TabIndex = 0;
            this.btnStructure.Text = "Structure";
            this.btnStructure.UseVisualStyleBackColor = true;

 // 
 // btnDiagram
 // 
            this.btnDiagram.Location = new System.Drawing.Point(6, 40);
            this.btnDiagram.Name = "btnDiagram";
            this.btnDiagram.Size = new System.Drawing.Size(110, 30);
            this.btnDiagram.TabIndex = 1;
            this.btnDiagram.Text = "Diagram";
            this.btnDiagram.UseVisualStyleBackColor = true;

 // 
 // btnAtoms
 // 
            this.btnAtoms.Location = new System.Drawing.Point(6, 74);
            this.btnAtoms.Name = "btnAtoms";
            this.btnAtoms.Size = new System.Drawing.Size(110, 30);
            this.btnAtoms.TabIndex = 2;
            this.btnAtoms.Text = "Atoms";
            this.btnAtoms.UseVisualStyleBackColor = true;

 // 
 // btnBonds
 // 
            this.btnBonds.Location = new System.Drawing.Point(6, 108);
            this.btnBonds.Name = "btnBonds";
            this.btnBonds.Size = new System.Drawing.Size(110, 30);
            this.btnBonds.TabIndex = 3;
            this.btnBonds.Text = "Bonds";
            this.btnBonds.UseVisualStyleBackColor = true;

 // 
 // btnBonds
 // 
            this.btnHBonds.Location = new System.Drawing.Point(6, 448);
            this.btnHBonds.Name = "btnHBonds";
            this.btnHBonds.Size = new System.Drawing.Size(110, 30);
            this.btnHBonds.TabIndex = 13;
            this.btnHBonds.Text = "H-Bonds";
            this.btnHBonds.UseVisualStyleBackColor = true;
            this.panelButtons.Controls.Add(this.btnHBonds);

            this.btnHBondTopo.Location = new System.Drawing.Point(6, 516);
            this.btnHBondTopo.Name = "btnHBondTopo";
            this.btnHBondTopo.Size = new System.Drawing.Size(110, 30);
            this.btnHBondTopo.TabIndex = 15;
            this.btnHBondTopo.Text = "H-Bond Topology";
            this.btnHBondTopo.UseVisualStyleBackColor = true;
            this.panelButtons.Controls.Add(this.btnHBondTopo);

            // 
            // btnSmiles
            // 
            this.btnSmiles.Location = new System.Drawing.Point(6, 482);
            this.btnSmiles.Name = "btnSmiles";
            this.btnSmiles.Size = new System.Drawing.Size(110, 30);
            this.btnSmiles.TabIndex = 14;
            this.btnSmiles.Text = "SMILES";
            this.btnSmiles.UseVisualStyleBackColor = true;


 // 
 // btnContacts
 // 
            this.btnContacts.Location = new System.Drawing.Point(6, 142);
            this.btnContacts.Name = "btnContacts";
            this.btnContacts.Size = new System.Drawing.Size(110, 30);
            this.btnContacts.TabIndex = 4;
            this.btnContacts.Text = "Contacts";
            this.btnContacts.UseVisualStyleBackColor = true;

 // 
 // btnCentroids
 // 
            this.btnCentroids.Location = new System.Drawing.Point(6, 176);
            this.btnCentroids.Name = "btnCentroids";
            this.btnCentroids.Size = new System.Drawing.Size(110, 30);
            this.btnCentroids.TabIndex = 5;
            this.btnCentroids.Text = "Centroids";
            this.btnCentroids.UseVisualStyleBackColor = true;

 // 
 // btnPlanes
 // 
            this.btnPlanes.Location = new System.Drawing.Point(6, 210);
            this.btnPlanes.Name = "btnPlanes";
            this.btnPlanes.Size = new System.Drawing.Size(110, 30);
            this.btnPlanes.TabIndex = 6;
            this.btnPlanes.Text = "Planes";
            this.btnPlanes.UseVisualStyleBackColor = true;

 // 
 // btnSymmetry
 // 
            this.btnSymmetry.Location = new System.Drawing.Point(6, 244);
            this.btnSymmetry.Name = "btnSymmetry";
            this.btnSymmetry.Size = new System.Drawing.Size(110, 30);
            this.btnSymmetry.TabIndex = 7;
            this.btnSymmetry.Text = "Symmetry";
            this.btnSymmetry.UseVisualStyleBackColor = true;

 // 
 // btnDistances
 // 
            this.btnDistances.Location = new System.Drawing.Point(6, 278);
            this.btnDistances.Name = "btnDistances";
            this.btnDistances.Size = new System.Drawing.Size(110, 30);
            this.btnDistances.TabIndex = 8;
            this.btnDistances.Text = "Distances";
            this.btnDistances.UseVisualStyleBackColor = true;

 // 
 // btnAngles
 // 
            this.btnAngles.Location = new System.Drawing.Point(6, 312);
            this.btnAngles.Name = "btnAngles";
            this.btnAngles.Size = new System.Drawing.Size(110, 30);
            this.btnAngles.TabIndex = 9;
            this.btnAngles.Text = "Angles";
            this.btnAngles.UseVisualStyleBackColor = true;

 // 
 // btnTorsions
 // 
            this.btnTorsions.Location = new System.Drawing.Point(6, 346);
            this.btnTorsions.Name = "btnTorsions";
            this.btnTorsions.Size = new System.Drawing.Size(110, 30);
            this.btnTorsions.TabIndex = 10;
            this.btnTorsions.Text = "Torsions";
            this.btnTorsions.UseVisualStyleBackColor = true;

 // 
 // btnAllAngles
 // 
            this.btnAllAngles.Location = new System.Drawing.Point(6, 380);
            this.btnAllAngles.Name = "btnAllAngles";
            this.btnAllAngles.Size = new System.Drawing.Size(110, 30);
            this.btnAllAngles.TabIndex = 11;
            this.btnAllAngles.Text = "All Angles";
            this.btnAllAngles.UseVisualStyleBackColor = true;

 // 
 // btnAllTorsions
 // 
            this.btnAllTorsions.Location = new System.Drawing.Point(6, 414);
            this.btnAllTorsions.Name = "btnAllTorsions";
            this.btnAllTorsions.Size = new System.Drawing.Size(110, 30);
            this.btnAllTorsions.TabIndex = 12;
            this.btnAllTorsions.Text = "All Torsions";
            this.btnAllTorsions.UseVisualStyleBackColor = true;

 // 
 // panelTableScroll
 // 
            this.panelTableScroll.AutoScroll = true;
            this.panelTableScroll.BackColor = System.Drawing.Color.White;
            this.panelTableScroll.Controls.Add(this.table);
            this.panelTableScroll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTableScroll.Location = new System.Drawing.Point(130, 60);
            this.panelTableScroll.Name = "panelTableScroll";
            this.panelTableScroll.Size = new System.Drawing.Size(770, 540);
            this.panelTableScroll.TabIndex = 2;

 // 
 // table
 // 
            this.table.AutoSize = false;
            this.table.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.table.ColumnCount = 2;
            this.table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.table.Location = new System.Drawing.Point(0, 0);
            this.table.Name = "table";
            this.table.Padding = new System.Windows.Forms.Padding(4);
            this.table.RowCount = 1;
            this.table.Size = new System.Drawing.Size(8000, 40);
            this.table.TabIndex = 0;

 // 
 // panelBottom
 // 
            this.panelBottom.Controls.Add(this.btnExportExcel);
            this.panelBottom.Controls.Add(this.btnCopySmiles);
            this.panelBottom.Controls.Add(this.btnClose);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 600);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(900, 40);
            this.panelBottom.TabIndex = 3;

 // 
 // btnClose
 // 
            this.btnCopySmiles.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnCopySmiles.Location = new System.Drawing.Point(300, 6);
            this.btnCopySmiles.Name = "btnCopySmiles";
            this.btnCopySmiles.Size = new System.Drawing.Size(100, 28);
            this.btnCopySmiles.TabIndex = 1;
            this.btnCopySmiles.Text = "Copy SMILES";
            this.btnCopySmiles.UseVisualStyleBackColor = true;
            this.btnExportExcel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnExportExcel.Location = new System.Drawing.Point(180, 6);
            this.btnExportExcel.Name = "btnExportExcel";
            this.btnExportExcel.Size = new System.Drawing.Size(110, 28);
            this.btnExportExcel.TabIndex = 2;
            this.btnExportExcel.Text = "Export Excel…";
            this.btnExportExcel.UseVisualStyleBackColor = true;
            this.btnClose.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnClose.Location = new System.Drawing.Point(410, 6);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(90, 28);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;

 // 
 // StructureInfoWindow
 // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 640);
            this.Controls.Add(this.panelTableScroll);
            this.Controls.Add(this.panelButtons);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.panelBottom);
            this.Name = "StructureInfoWindow";
            this.Text = "Structure Info";
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelButtons.ResumeLayout(false);
            this.panelTableScroll.ResumeLayout(false);
            this.panelTableScroll.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }

 // Controls
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblCurrentStructure;
        private System.Windows.Forms.TextBox txtCurrentStructure;
        private System.Windows.Forms.Button btnCustomise;

        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Button btnStructure;
        private System.Windows.Forms.Button btnDiagram;
        private System.Windows.Forms.Button btnSmiles;
        private System.Windows.Forms.Button btnAtoms;
        private System.Windows.Forms.Button btnBonds;
        private System.Windows.Forms.Button btnContacts;
        private System.Windows.Forms.Button btnHBonds;
        private System.Windows.Forms.Button btnHBondTopo;
        private System.Windows.Forms.Button btnCentroids;
        private System.Windows.Forms.Button btnPlanes;
        private System.Windows.Forms.Button btnSymmetry;
        private System.Windows.Forms.Button btnDistances;
        private System.Windows.Forms.Button btnAngles;
        private System.Windows.Forms.Button btnTorsions;
        private System.Windows.Forms.Button btnAllAngles;
        private System.Windows.Forms.Button btnAllTorsions;

        private System.Windows.Forms.Panel panelTableScroll;
        private System.Windows.Forms.TableLayoutPanel table;

        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnCopySmiles;
        private System.Windows.Forms.Button btnExportExcel;
    }
}