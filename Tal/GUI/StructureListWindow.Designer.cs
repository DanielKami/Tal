// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// StructureListWindow designer-generated layout.

﻿namespace Tal.Forms
{
    partial class StructureListWindow
    {
        private System.ComponentModel.IContainer components = null;

        // ⭐ DEKLARACJE KONTROLEK
        private System.Windows.Forms.ListView _structureList;
        private System.Windows.Forms.Button _btnOpenSelected;
        private System.Windows.Forms.Button _btnOpenAll;
        private System.Windows.Forms.Button _btnOpenNew;
        private System.Windows.Forms.Button _btnCloseFile;
        private System.Windows.Forms.Button _btnCloseAll;
        private System.Windows.Forms.Label _lblInfo;
        private System.Windows.Forms.Panel topPanel;

        // ⭐ KOLUMNY
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colTransformation;
        private System.Windows.Forms.ColumnHeader colColor;

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
            _structureList = new ListView();
            colName = new ColumnHeader();
            colTransformation = new ColumnHeader();
            colColor = new ColumnHeader();
            topPanel = new Panel();
            _btnOpenSelected = new Button();
            _btnOpenAll = new Button();
            _btnOpenNew = new Button();
            _btnCloseFile = new Button();
            _btnCloseAll = new Button();
            _lblInfo = new Label();
            topPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _structureList
            // 
            _structureList.CheckBoxes = false;
            _structureList.Columns.AddRange(new ColumnHeader[] {  colName, colTransformation, colColor });
            _structureList.Dock = DockStyle.Fill;
            _structureList.FullRowSelect = true;
            _structureList.Location = new Point(0, 110);
            _structureList.Name = "_structureList";
            _structureList.OwnerDraw = true;
            _structureList.Size = new Size(560, 409);
            _structureList.TabIndex = 0;
            _structureList.UseCompatibleStateImageBehavior = false;
            _structureList.View = View.Details;
            _structureList.DrawColumnHeader += StructureList_DrawColumnHeader;
            _structureList.DrawItem += StructureList_DrawItem;
            _structureList.DrawSubItem += StructureList_DrawSubItem;
            _structureList.SelectedIndexChanged += StructureList_SelectedIndexChanged;
            _structureList.DoubleClick += StructureList_DoubleClick;
            // 
            // colName
            // 
            colName.Text = "Name";
            colName.Width = 235;
            // 
            // colTransformation
            // 
            colTransformation.Text = "Transform";
            colTransformation.TextAlign = HorizontalAlignment.Center;
            colTransformation.Width = 100;
            // 
            // colColor
            // 
            colColor.Text = "Color";
            colColor.TextAlign = HorizontalAlignment.Center;
            colColor.Width = 50;
            // 
            // topPanel
            // 
            topPanel.BackColor = Color.FromArgb(240, 240, 242);
            topPanel.Controls.Add(_btnOpenSelected);
            topPanel.Controls.Add(_btnOpenAll);
            topPanel.Controls.Add(_btnOpenNew);
            topPanel.Controls.Add(_btnCloseFile);
            topPanel.Controls.Add(_btnCloseAll);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(560, 110);
            topPanel.TabIndex = 1;
            // 
            // _btnOpenSelected
            // 
            _btnOpenSelected.BackColor = Color.FromArgb(230, 230, 235);
            _btnOpenSelected.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _btnOpenSelected.FlatStyle = FlatStyle.Flat;
            _btnOpenSelected.ForeColor = Color.FromArgb(51, 51, 51);
            _btnOpenSelected.Location = new Point(10, 10);
            _btnOpenSelected.Name = "_btnOpenSelected";
            _btnOpenSelected.Size = new Size(120, 28);
            _btnOpenSelected.TabIndex = 0;
            _btnOpenSelected.Text = "📂 Open selected";
            _btnOpenSelected.UseVisualStyleBackColor = false;
            _btnOpenSelected.Click += BtnOpenSelected_Click;
            // 
            // _btnOpenAll
            // 
            _btnOpenAll.BackColor = Color.FromArgb(230, 230, 235);
            _btnOpenAll.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _btnOpenAll.FlatStyle = FlatStyle.Flat;
            _btnOpenAll.ForeColor = Color.FromArgb(51, 51, 51);
            _btnOpenAll.Location = new Point(136, 10);
            _btnOpenAll.Name = "_btnOpenAll";
            _btnOpenAll.Size = new Size(167, 28);
            _btnOpenAll.TabIndex = 1;
            _btnOpenAll.Text = "📂 Open all";
            _btnOpenAll.UseVisualStyleBackColor = false;
            _btnOpenAll.Click += BtnOpenAll_Click;
            // 
            // _btnOpenNew
            // 
            _btnOpenNew.BackColor = Color.FromArgb(0, 122, 204);
            _btnOpenNew.FlatAppearance.BorderColor = Color.FromArgb(0, 122, 204);
            _btnOpenNew.FlatStyle = FlatStyle.Flat;
            _btnOpenNew.ForeColor = Color.White;
            _btnOpenNew.Location = new Point(10, 75);
            _btnOpenNew.Name = "_btnOpenNew";
            _btnOpenNew.Size = new Size(536, 28);
            _btnOpenNew.TabIndex = 4;
            _btnOpenNew.Text = "📂 Open new file";
            _btnOpenNew.UseVisualStyleBackColor = false;
            _btnOpenNew.Click += BtnOpenNew_Click;
            // 
            // _btnCloseFile
            // 
            _btnCloseFile.BackColor = Color.FromArgb(230, 230, 235);
            _btnCloseFile.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _btnCloseFile.FlatStyle = FlatStyle.Flat;
            _btnCloseFile.ForeColor = Color.FromArgb(51, 51, 51);
            _btnCloseFile.Location = new Point(10, 43);
            _btnCloseFile.Name = "_btnCloseFile";
            _btnCloseFile.Size = new Size(120, 28);
            _btnCloseFile.TabIndex = 2;
            _btnCloseFile.Text = "❌ Zamknij plik";
            _btnCloseFile.UseVisualStyleBackColor = false;
            _btnCloseFile.Click += BtnCloseFile_Click;
            // 
            // _btnCloseAll
            // 
            _btnCloseAll.BackColor = Color.FromArgb(230, 230, 235);
            _btnCloseAll.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _btnCloseAll.FlatStyle = FlatStyle.Flat;
            _btnCloseAll.ForeColor = Color.FromArgb(51, 51, 51);
            _btnCloseAll.Location = new Point(136, 44);
            _btnCloseAll.Name = "_btnCloseAll";
            _btnCloseAll.Size = new Size(167, 28);
            _btnCloseAll.TabIndex = 3;
            _btnCloseAll.Text = "❌ Zamknij wszystkie";
            _btnCloseAll.UseVisualStyleBackColor = false;
            _btnCloseAll.Click += BtnCloseAll_Click;
            // 
            // _lblInfo
            // 
            _lblInfo.BackColor = Color.White;
            _lblInfo.Dock = DockStyle.Bottom;
            _lblInfo.ForeColor = Color.FromArgb(120, 120, 120);
            _lblInfo.Location = new Point(0, 519);
            _lblInfo.Name = "_lblInfo";
            _lblInfo.Size = new Size(560, 25);
            _lblInfo.TabIndex = 2;
            _lblInfo.Text = "Brak struktur";
            _lblInfo.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // StructureListWindow
            // 
            ClientSize = new Size(560, 544);
            Controls.Add(_structureList);
            Controls.Add(topPanel);
            Controls.Add(_lblInfo);
            Name = "StructureListWindow";
            Text = "📋 Struktury";
            topPanel.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}