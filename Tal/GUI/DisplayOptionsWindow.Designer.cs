// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// DisplayOptionsWindow designer-generated layout.

﻿namespace Tal.Forms
{
    partial class DisplayOptionsWindow
    {
        private System.ComponentModel.IContainer components = null;

        // ⭐ DEKLARACJE KONTROLEK
        private System.Windows.Forms.GroupBox _grpDisplay;
        private System.Windows.Forms.GroupBox _grpOptions;
        private System.Windows.Forms.GroupBox _grpInfo;
        private System.Windows.Forms.Label _lblStatus;

        // ⭐ DISPLAY - LEWA KOLUMNA (checkboxy)
        private System.Windows.Forms.CheckBox _chkPacking;
        private System.Windows.Forms.CheckBox _chkShortContact;
        private System.Windows.Forms.CheckBox _chkAsymmetricUnit;
        private System.Windows.Forms.CheckBox _chkHBond;
        private System.Windows.Forms.CheckBox _chkAutoCentre;
        private System.Windows.Forms.Button _btnReset;

        // ⭐ DISPLAY - PRAWA KOLUMNA (buttons)
        private System.Windows.Forms.Button _btnContacts;
        private System.Windows.Forms.Button _btnMoreInfo;
        private System.Windows.Forms.Button _btnPowder;

        // ⭐ OPTIONS - 2 kolumny
        private System.Windows.Forms.CheckBox _chkShowHydrogens;
        private System.Windows.Forms.CheckBox _chkDepthCue;
        private System.Windows.Forms.CheckBox _chkShowCellAxes;
        private System.Windows.Forms.CheckBox _chkZClipping;
        private System.Windows.Forms.CheckBox _chkLabelAtoms;
        private System.Windows.Forms.CheckBox _chkStereo;

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
            _grpDisplay = new GroupBox();
            _chkPacking = new CheckBox();
            _chkShortContact = new CheckBox();
            _chkAsymmetricUnit = new CheckBox();
            _chkHBond = new CheckBox();
            _chkAutoCentre = new CheckBox();
            _btnReset = new Button();
            _btnContacts = new Button();
            _btnMoreInfo = new Button();
            _btnPowder = new Button();
            _grpOptions = new GroupBox();
            _chkShowHydrogens = new CheckBox();
            _chkShowCellAxes = new CheckBox();
            _chkLabelAtoms = new CheckBox();
            _chkDepthCue = new CheckBox();
            _chkZClipping = new CheckBox();
            _chkStereo = new CheckBox();
            _grpInfo = new GroupBox();
            _lblStatus = new Label();
            _grpDisplay.SuspendLayout();
            _grpOptions.SuspendLayout();
            _grpInfo.SuspendLayout();
            SuspendLayout();
            // 
            // _grpDisplay
            // 
            _grpDisplay.Controls.Add(_chkPacking);
            _grpDisplay.Controls.Add(_chkShortContact);
            _grpDisplay.Controls.Add(_chkAsymmetricUnit);
            _grpDisplay.Controls.Add(_chkHBond);
            _grpDisplay.Controls.Add(_chkAutoCentre);
            _grpDisplay.Controls.Add(_btnReset);
            _grpDisplay.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpDisplay.ForeColor = Color.FromArgb(51, 51, 51);
            _grpDisplay.Location = new Point(8, 8);
            _grpDisplay.Name = "_grpDisplay";
            _grpDisplay.Size = new Size(435, 134);
            _grpDisplay.TabIndex = 0;
            _grpDisplay.TabStop = false;
            _grpDisplay.Text = "Display";
            // 
            // _chkPacking
            // 
            _chkPacking.Font = new Font("Segoe UI", 9F);
            _chkPacking.ForeColor = Color.FromArgb(51, 51, 51);
            _chkPacking.Location = new Point(8, 22);
            _chkPacking.Name = "_chkPacking";
            _chkPacking.Size = new Size(100, 32);
            _chkPacking.TabIndex = 0;
            _chkPacking.Text = "Packing";
            // 
            // _chkShortContact
            // 
            _chkShortContact.Font = new Font("Segoe UI", 9F);
            _chkShortContact.ForeColor = Color.FromArgb(51, 51, 51);
            _chkShortContact.Location = new Point(271, 53);
            _chkShortContact.Name = "_chkShortContact";
            _chkShortContact.Size = new Size(149, 27);
            _chkShortContact.TabIndex = 1;
            _chkShortContact.Text = "Short Contact";
            // 
            // _chkAsymmetricUnit
            // 
            _chkAsymmetricUnit.Font = new Font("Segoe UI", 9F);
            _chkAsymmetricUnit.ForeColor = Color.FromArgb(51, 51, 51);
            _chkAsymmetricUnit.Location = new Point(8, 53);
            _chkAsymmetricUnit.Name = "_chkAsymmetricUnit";
            _chkAsymmetricUnit.Size = new Size(130, 28);
            _chkAsymmetricUnit.TabIndex = 2;
            _chkAsymmetricUnit.Text = "Asymmetric Unit";
            // 
            // _chkHBond
            // 
            _chkHBond.Font = new Font("Segoe UI", 9F);
            _chkHBond.ForeColor = Color.FromArgb(51, 51, 51);
            _chkHBond.Location = new Point(271, 27);
            _chkHBond.Name = "_chkHBond";
            _chkHBond.Size = new Size(80, 22);
            _chkHBond.TabIndex = 3;
            _chkHBond.Text = "H-Bond";
            // 
            // _chkAutoCentre
            // 
            _chkAutoCentre.Checked = true;
            _chkAutoCentre.CheckState = CheckState.Checked;
            _chkAutoCentre.Font = new Font("Segoe UI", 9F);
            _chkAutoCentre.ForeColor = Color.FromArgb(51, 51, 51);
            _chkAutoCentre.Location = new Point(8, 84);
            _chkAutoCentre.Name = "_chkAutoCentre";
            _chkAutoCentre.Size = new Size(139, 29);
            _chkAutoCentre.TabIndex = 4;
            _chkAutoCentre.Text = "Auto centre";
            // 
            // _btnReset
            // 
            _btnReset.BackColor = Color.FromArgb(0, 122, 204);
            _btnReset.FlatStyle = FlatStyle.Flat;
            _btnReset.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            _btnReset.ForeColor = Color.White;
            _btnReset.Location = new Point(271, 87);
            _btnReset.Name = "_btnReset";
            _btnReset.Size = new Size(105, 28);
            _btnReset.TabIndex = 5;
            _btnReset.Text = "Reset";
            _btnReset.UseVisualStyleBackColor = false;
            // 
            // _btnContacts
            // 
            _btnContacts.BackColor = Color.FromArgb(230, 230, 235);
            _btnContacts.FlatStyle = FlatStyle.Flat;
            _btnContacts.Font = new Font("Segoe UI", 8F);
            _btnContacts.ForeColor = Color.FromArgb(51, 51, 51);
            _btnContacts.Location = new Point(783, 23);
            _btnContacts.Name = "_btnContacts";
            _btnContacts.Size = new Size(113, 29);
            _btnContacts.TabIndex = 6;
            _btnContacts.Text = "Contacts...";
            _btnContacts.TextAlign = ContentAlignment.MiddleLeft;
            _btnContacts.UseVisualStyleBackColor = false;
            // 
            // _btnMoreInfo
            // 
            _btnMoreInfo.BackColor = Color.FromArgb(230, 230, 235);
            _btnMoreInfo.FlatStyle = FlatStyle.Flat;
            _btnMoreInfo.Font = new Font("Segoe UI", 8F);
            _btnMoreInfo.ForeColor = Color.FromArgb(51, 51, 51);
            _btnMoreInfo.Location = new Point(783, 61);
            _btnMoreInfo.Name = "_btnMoreInfo";
            _btnMoreInfo.Size = new Size(113, 28);
            _btnMoreInfo.TabIndex = 7;
            _btnMoreInfo.Text = "More Info";
            _btnMoreInfo.TextAlign = ContentAlignment.MiddleLeft;
            _btnMoreInfo.UseVisualStyleBackColor = false;
            // 
            // _btnPowder
            // 
            _btnPowder.BackColor = Color.FromArgb(0, 122, 204);
            _btnPowder.FlatStyle = FlatStyle.Flat;
            _btnPowder.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            _btnPowder.ForeColor = Color.White;
            _btnPowder.Location = new Point(920, 20);
            _btnPowder.Name = "_btnPowder";
            _btnPowder.Size = new Size(135, 35);
            _btnPowder.TabIndex = 8;
            _btnPowder.Text = "Powder pattern";
            _btnPowder.TextAlign = ContentAlignment.MiddleLeft;
            _btnPowder.UseVisualStyleBackColor = false;
            // 
            // _grpOptions
            // 
            _grpOptions.Controls.Add(_chkShowHydrogens);
            _grpOptions.Controls.Add(_chkShowCellAxes);
            _grpOptions.Controls.Add(_chkLabelAtoms);
            _grpOptions.Controls.Add(_chkDepthCue);
            _grpOptions.Controls.Add(_chkZClipping);
            _grpOptions.Controls.Add(_chkStereo);
            _grpOptions.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _grpOptions.ForeColor = Color.FromArgb(51, 51, 51);
            _grpOptions.Location = new Point(538, 8);
            _grpOptions.Name = "_grpOptions";
            _grpOptions.Size = new Size(239, 134);
            _grpOptions.TabIndex = 1;
            _grpOptions.TabStop = false;
            _grpOptions.Text = "Options";
            // 
            // _chkShowHydrogens
            // 
            _chkShowHydrogens.Font = new Font("Segoe UI", 9F);
            _chkShowHydrogens.ForeColor = Color.FromArgb(51, 51, 51);
            _chkShowHydrogens.Location = new Point(8, 27);
            _chkShowHydrogens.Name = "_chkShowHydrogens";
            _chkShowHydrogens.Size = new Size(110, 32);
            _chkShowHydrogens.TabIndex = 0;
            _chkShowHydrogens.Text = "Show H";
            // 
            // _chkShowCellAxes
            // 
            _chkShowCellAxes.Checked = true;
            _chkShowCellAxes.CheckState = CheckState.Checked;
            _chkShowCellAxes.Font = new Font("Segoe UI", 9F);
            _chkShowCellAxes.ForeColor = Color.FromArgb(51, 51, 51);
            _chkShowCellAxes.Location = new Point(8, 59);
            _chkShowCellAxes.Name = "_chkShowCellAxes";
            _chkShowCellAxes.Size = new Size(110, 25);
            _chkShowCellAxes.TabIndex = 1;
            _chkShowCellAxes.Text = "Show cell axes";
            // 
            // _chkLabelAtoms
            // 
            _chkLabelAtoms.Font = new Font("Segoe UI", 9F);
            _chkLabelAtoms.ForeColor = Color.FromArgb(51, 51, 51);
            _chkLabelAtoms.Location = new Point(8, 94);
            _chkLabelAtoms.Name = "_chkLabelAtoms";
            _chkLabelAtoms.Size = new Size(100, 22);
            _chkLabelAtoms.TabIndex = 2;
            _chkLabelAtoms.Text = "Label atoms";
            // 
            // _chkDepthCue
            // 
            _chkDepthCue.Font = new Font("Segoe UI", 9F);
            _chkDepthCue.ForeColor = Color.FromArgb(51, 51, 51);
            _chkDepthCue.Location = new Point(125, 22);
            _chkDepthCue.Name = "_chkDepthCue";
            _chkDepthCue.Size = new Size(100, 27);
            _chkDepthCue.TabIndex = 3;
            _chkDepthCue.Text = "Depth cue";
            // 
            // _chkZClipping
            // 
            _chkZClipping.Font = new Font("Segoe UI", 9F);
            _chkZClipping.ForeColor = Color.FromArgb(51, 51, 51);
            _chkZClipping.Location = new Point(125, 57);
            _chkZClipping.Name = "_chkZClipping";
            _chkZClipping.Size = new Size(100, 28);
            _chkZClipping.TabIndex = 4;
            _chkZClipping.Text = "Z-Clipping";
            // 
            // _chkStereo
            // 
            _chkStereo.Font = new Font("Segoe UI", 9F);
            _chkStereo.ForeColor = Color.FromArgb(51, 51, 51);
            _chkStereo.Location = new Point(125, 94);
            _chkStereo.Name = "_chkStereo";
            _chkStereo.Size = new Size(100, 22);
            _chkStereo.TabIndex = 5;
            _chkStereo.Text = "Stereo";
            // 
            // _grpInfo
            // 
            _grpInfo.BackColor = Color.FromArgb(248, 248, 250);
            _grpInfo.Controls.Add(_lblStatus);
            _grpInfo.Location = new Point(8, 184);
            _grpInfo.Name = "_grpInfo";
            _grpInfo.Size = new Size(483, 28);
            _grpInfo.TabIndex = 2;
            _grpInfo.TabStop = false;
            // 
            // _lblStatus
            // 
            _lblStatus.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            _lblStatus.ForeColor = Color.FromArgb(120, 120, 120);
            _lblStatus.Location = new Point(6, 6);
            _lblStatus.Name = "_lblStatus";
            _lblStatus.Size = new Size(470, 18);
            _lblStatus.TabIndex = 0;
            _lblStatus.Text = "Press the left mouse button and move the mouse to rotate the structure.";
            _lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // DisplayOptionsWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1110, 210);
            Controls.Add(_grpDisplay);
            Controls.Add(_grpOptions);
            Controls.Add(_grpInfo);
            Controls.Add(_btnContacts);
            Controls.Add(_btnMoreInfo);
            Controls.Add(_btnPowder);
            MinimumSize = new Size(480, 190);
            Name = "DisplayOptionsWindow";
            Padding = new Padding(4);
            _grpDisplay.ResumeLayout(false);
            _grpOptions.ResumeLayout(false);
            _grpInfo.ResumeLayout(false);
            ResumeLayout(false);
        }
    }
}