// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal planes module: PlanesWindow.Designer.

﻿namespace Tal.Forms
{
    partial class PlanesWindow
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
            this.components = new System.ComponentModel.Container();
            this.labelInfo = new System.Windows.Forms.Label();
            this.listViewPlanes = new System.Windows.Forms.ListView();
            this.columnHeader = new System.Windows.Forms.ColumnHeader();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.btnNewPlane = new System.Windows.Forms.Button();
            this.btnEdit = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
 // 
 // labelInfo
 // 
            this.labelInfo.Location = new System.Drawing.Point(12, 9);
            this.labelInfo.Name = "labelInfo";
            this.labelInfo.Size = new System.Drawing.Size(360, 40);
            this.labelInfo.TabIndex = 0;
            this.labelInfo.Text = "Pick an object from the list below, or in the\r\ngraphics window, or right-click on a list item";
 // 
 // listViewPlanes
 // 
            this.listViewPlanes.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader});
            this.listViewPlanes.FullRowSelect = true;
            this.listViewPlanes.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.listViewPlanes.HideSelection = false;
            this.listViewPlanes.LargeImageList = this.imageList;
            this.listViewPlanes.Location = new System.Drawing.Point(12, 52);
            this.listViewPlanes.MultiSelect = false;
            this.listViewPlanes.Name = "listViewPlanes";
            this.listViewPlanes.Size = new System.Drawing.Size(360, 250);
            this.listViewPlanes.SmallImageList = this.imageList;
            this.listViewPlanes.TabIndex = 1;
            this.listViewPlanes.UseCompatibleStateImageBehavior = false;
            this.listViewPlanes.View = System.Windows.Forms.View.Details;
            this.listViewPlanes.SelectedIndexChanged += new System.EventHandler(this.ListViewPlanes_SelectedIndexChanged);
 // 
 // columnHeader
 // 
            this.columnHeader.Width = 340;
 // 
 // imageList
 // 
            this.imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imageList.ImageSize = new System.Drawing.Size(16, 16);
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
 // 
 // btnNewPlane
 // 
            this.btnNewPlane.Location = new System.Drawing.Point(12, 315);
            this.btnNewPlane.Name = "btnNewPlane";
            this.btnNewPlane.Size = new System.Drawing.Size(110, 30);
            this.btnNewPlane.TabIndex = 2;
            this.btnNewPlane.Text = "New Plane...";
            this.btnNewPlane.UseVisualStyleBackColor = true;
            this.btnNewPlane.Click += new System.EventHandler(this.BtnNewPlane_Click);
 // 
 // btnEdit
 // 
            this.btnEdit.Enabled = false;
            this.btnEdit.Location = new System.Drawing.Point(128, 315);
            this.btnEdit.Name = "btnEdit";
            this.btnEdit.Size = new System.Drawing.Size(110, 30);
            this.btnEdit.TabIndex = 3;
            this.btnEdit.Text = "Edit...";
            this.btnEdit.UseVisualStyleBackColor = true;
            this.btnEdit.Click += new System.EventHandler(this.BtnEdit_Click);
 // 
 // btnDelete
 // 
            this.btnDelete.Enabled = false;
            this.btnDelete.Location = new System.Drawing.Point(244, 315);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(128, 30);
            this.btnDelete.TabIndex = 4;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);
 // 
 // btnClose
 // 
            this.btnClose.Location = new System.Drawing.Point(143, 355);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(100, 32);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
 // 
 // PlanesWindow
 // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 400);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnEdit);
            this.Controls.Add(this.btnNewPlane);
            this.Controls.Add(this.listViewPlanes);
            this.Controls.Add(this.labelInfo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PlanesWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Planes";
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Label labelInfo;
        private System.Windows.Forms.ListView listViewPlanes;
        private System.Windows.Forms.ColumnHeader columnHeader;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.Button btnNewPlane;
        private System.Windows.Forms.Button btnEdit;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnClose;
    }
}