// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// HydrogenNormalizationWindow designer-generated layout.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal.Forms
{
    partial class HydrogenNormalizationWindow
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this._rootPanel = new System.Windows.Forms.Panel();
            this._controls = new System.Windows.Forms.FlowLayoutPanel();

            // ── Header ──
            this._lblHeader = new System.Windows.Forms.Label();
            this._lblMoleculeInfo = new System.Windows.Forms.Label();

            // ── Options ──
            this._lblOptionsTitle = new System.Windows.Forms.Label();
            this._chkNormalize = new System.Windows.Forms.CheckBox();
            this._chkSkipBridging = new System.Windows.Forms.CheckBox();

            // ── Table ──
            this._lblTableTitle = new System.Windows.Forms.Label();
            this._dgvBonds = new System.Windows.Forms.DataGridView();

            // ── Buttons (without Scan) ──
            this._btnApply = new System.Windows.Forms.Button();
            this._btnReset = new System.Windows.Forms.Button();
            this._btnClose = new System.Windows.Forms.Button();

            // ── Log ──
            this._lblLogTitle = new System.Windows.Forms.Label();
            this._rtbLog = new System.Windows.Forms.RichTextBox();

            // ── Status ──
            this._lblStatus = new System.Windows.Forms.Label();

            this._rootPanel.SuspendLayout();
            this._controls.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._dgvBonds)).BeginInit();
            this.SuspendLayout();

            // ═══════════════════════════════════════════════════════════
            //  _rootPanel
            // ═══════════════════════════════════════════════════════════
            this._rootPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._rootPanel.BackColor = System.Drawing.Color.FromArgb(245, 245, 250);
            this._rootPanel.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
            this._rootPanel.Name = "_rootPanel";
            this._rootPanel.TabIndex = 0;

            // ═══════════════════════════════════════════════════════════
            //  _controls (FlowLayoutPanel)
            // ═══════════════════════════════════════════════════════════
            this._controls.Dock = System.Windows.Forms.DockStyle.Fill;
            this._controls.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this._controls.WrapContents = false;
            this._controls.AutoScroll = true;
            this._controls.BackColor = System.Drawing.Color.FromArgb(245, 245, 250);
            this._controls.Name = "_controls";
            this._controls.TabIndex = 0;

            // ═══════════════════════════════════════════════════════════
            //  _lblHeader
            // ═══════════════════════════════════════════════════════════
            this._lblHeader.AutoSize = false;
            this._lblHeader.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this._lblHeader.ForeColor = System.Drawing.Color.FromArgb(40, 40, 60);
            this._lblHeader.Location = new System.Drawing.Point(3, 0);
            this._lblHeader.Margin = new System.Windows.Forms.Padding(3, 0, 3, 6);
            this._lblHeader.Name = "_lblHeader";
            this._lblHeader.Size = new System.Drawing.Size(420, 22);
            this._lblHeader.TabIndex = 0;
            this._lblHeader.Text = "🧪 Hydrogen Normalization";
            this._lblHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // ═══════════════════════════════════════════════════════════
            //  _lblMoleculeInfo
            // ═══════════════════════════════════════════════════════════
            this._lblMoleculeInfo.AutoSize = false;
            this._lblMoleculeInfo.Font = new System.Drawing.Font("Consolas", 8.5F);
            this._lblMoleculeInfo.ForeColor = System.Drawing.Color.FromArgb(60, 60, 80);
            this._lblMoleculeInfo.Location = new System.Drawing.Point(3, 28);
            this._lblMoleculeInfo.Margin = new System.Windows.Forms.Padding(3, 0, 3, 10);
            this._lblMoleculeInfo.Name = "_lblMoleculeInfo";
            this._lblMoleculeInfo.Size = new System.Drawing.Size(420, 34);
            this._lblMoleculeInfo.TabIndex = 1;
            this._lblMoleculeInfo.Text = "Molecule: —";

            // ═══════════════════════════════════════════════════════════
            //  _lblOptionsTitle
            // ═══════════════════════════════════════════════════════════
            this._lblOptionsTitle.AutoSize = false;
            this._lblOptionsTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblOptionsTitle.ForeColor = System.Drawing.Color.FromArgb(60, 60, 80);
            this._lblOptionsTitle.Location = new System.Drawing.Point(3, 72);
            this._lblOptionsTitle.Margin = new System.Windows.Forms.Padding(3, 0, 3, 4);
            this._lblOptionsTitle.Name = "_lblOptionsTitle";
            this._lblOptionsTitle.Size = new System.Drawing.Size(420, 16);
            this._lblOptionsTitle.TabIndex = 2;
            this._lblOptionsTitle.Text = "Options";

            // ═══════════════════════════════════════════════════════════
            //  _chkNormalize
            // ═══════════════════════════════════════════════════════════
            this._chkNormalize.AutoSize = true;
            this._chkNormalize.Checked = true;
            this._chkNormalize.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkNormalize.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._chkNormalize.Location = new System.Drawing.Point(3, 94);
            this._chkNormalize.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this._chkNormalize.Name = "_chkNormalize";
            this._chkNormalize.Size = new System.Drawing.Size(400, 19);
            this._chkNormalize.TabIndex = 3;
            this._chkNormalize.Text = "Normalize X-H bond lengths to neutron values";
            this._chkNormalize.UseVisualStyleBackColor = true;

            // ═══════════════════════════════════════════════════════════
            //  _chkSkipBridging
            // ═══════════════════════════════════════════════════════════
            this._chkSkipBridging.AutoSize = true;
            this._chkSkipBridging.Checked = true;
            this._chkSkipBridging.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkSkipBridging.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._chkSkipBridging.Location = new System.Drawing.Point(3, 117);
            this._chkSkipBridging.Margin = new System.Windows.Forms.Padding(3, 2, 3, 10);
            this._chkSkipBridging.Name = "_chkSkipBridging";
            this._chkSkipBridging.Size = new System.Drawing.Size(400, 19);
            this._chkSkipBridging.TabIndex = 4;
            this._chkSkipBridging.Text = "Skip bridging hydrogens (H between 2+ heavy atoms)";
            this._chkSkipBridging.UseVisualStyleBackColor = true;

            // ═══════════════════════════════════════════════════════════
            //  _lblTableTitle
            // ═══════════════════════════════════════════════════════════
            this._lblTableTitle.AutoSize = false;
            this._lblTableTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblTableTitle.ForeColor = System.Drawing.Color.FromArgb(60, 60, 80);
            this._lblTableTitle.Location = new System.Drawing.Point(3, 146);
            this._lblTableTitle.Margin = new System.Windows.Forms.Padding(3, 0, 3, 4);
            this._lblTableTitle.Name = "_lblTableTitle";
            this._lblTableTitle.Size = new System.Drawing.Size(420, 16);
            this._lblTableTitle.TabIndex = 5;
            this._lblTableTitle.Text = "Bond length table";

            // ═══════════════════════════════════════════════════════════
            //  _dgvBonds
            // ═══════════════════════════════════════════════════════════
            this._dgvBonds.ColumnHeadersHeightSizeMode =
                System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._dgvBonds.Location = new System.Drawing.Point(3, 168);
            this._dgvBonds.Margin = new System.Windows.Forms.Padding(3, 2, 3, 10);
            this._dgvBonds.Name = "_dgvBonds";
            this._dgvBonds.Size = new System.Drawing.Size(420, 240);
            this._dgvBonds.TabIndex = 6;
            this._dgvBonds.BackgroundColor = System.Drawing.Color.White;

            // ═══════════════════════════════════════════════════════════
            //  _btnApply
            // ═══════════════════════════════════════════════════════════
            this._btnApply.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnApply.BackColor = System.Drawing.Color.FromArgb(220, 240, 220);
            this._btnApply.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Bold);
            this._btnApply.Location = new System.Drawing.Point(3, 420);
            this._btnApply.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._btnApply.Name = "_btnApply";
            this._btnApply.Size = new System.Drawing.Size(204, 28);
            this._btnApply.TabIndex = 7;
            this._btnApply.Text = "✓ Apply normalization";
            this._btnApply.UseVisualStyleBackColor = false;

            // ═══════════════════════════════════════════════════════════
            //  _btnReset
            // ═══════════════════════════════════════════════════════════
            this._btnReset.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnReset.BackColor = System.Drawing.Color.FromArgb(240, 230, 220);
            this._btnReset.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnReset.Location = new System.Drawing.Point(213, 420);
            this._btnReset.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._btnReset.Name = "_btnReset";
            this._btnReset.Size = new System.Drawing.Size(100, 28);
            this._btnReset.TabIndex = 8;
            this._btnReset.Text = "↶ Reset";
            this._btnReset.UseVisualStyleBackColor = false;

            // ═══════════════════════════════════════════════════════════
            //  _btnClose
            // ═══════════════════════════════════════════════════════════
            this._btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this._btnClose.BackColor = System.Drawing.Color.FromArgb(230, 230, 235);
            this._btnClose.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this._btnClose.Location = new System.Drawing.Point(319, 420);
            this._btnClose.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(104, 28);
            this._btnClose.TabIndex = 9;
            this._btnClose.Text = "Close";
            this._btnClose.UseVisualStyleBackColor = false;

            // ═══════════════════════════════════════════════════════════
            //  _lblLogTitle
            // ═══════════════════════════════════════════════════════════
            this._lblLogTitle.AutoSize = false;
            this._lblLogTitle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblLogTitle.ForeColor = System.Drawing.Color.FromArgb(60, 60, 80);
            this._lblLogTitle.Location = new System.Drawing.Point(3, 457);
            this._lblLogTitle.Margin = new System.Windows.Forms.Padding(3, 6, 3, 4);
            this._lblLogTitle.Name = "_lblLogTitle";
            this._lblLogTitle.Size = new System.Drawing.Size(420, 16);
            this._lblLogTitle.TabIndex = 10;
            this._lblLogTitle.Text = "Log";

            // ═══════════════════════════════════════════════════════════
            //  _rtbLog
            // ═══════════════════════════════════════════════════════════
            this._rtbLog.Font = new System.Drawing.Font("Consolas", 8F);
            this._rtbLog.Location = new System.Drawing.Point(3, 479);
            this._rtbLog.Margin = new System.Windows.Forms.Padding(3, 2, 3, 10);
            this._rtbLog.Name = "_rtbLog";
            this._rtbLog.ReadOnly = true;
            this._rtbLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this._rtbLog.Size = new System.Drawing.Size(420, 180);
            this._rtbLog.TabIndex = 11;
            this._rtbLog.Text = "";
            this._rtbLog.BackColor = System.Drawing.Color.White;

            // ═══════════════════════════════════════════════════════════
            //  _lblStatus
            // ═══════════════════════════════════════════════════════════
            this._lblStatus.AutoSize = false;
            this._lblStatus.Font = new System.Drawing.Font("Consolas", 8.5F);
            this._lblStatus.ForeColor = System.Drawing.Color.FromArgb(0, 90, 160);
            this._lblStatus.Location = new System.Drawing.Point(3, 669);
            this._lblStatus.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(420, 30);
            this._lblStatus.TabIndex = 12;
            this._lblStatus.Text = "Ready";

            // ═══════════════════════════════════════════════════════════
            //  _controls — add all
            // ═══════════════════════════════════════════════════════════
            this._controls.Controls.Add(this._lblHeader);
            this._controls.Controls.Add(this._lblMoleculeInfo);
            this._controls.Controls.Add(this._lblOptionsTitle);
            this._controls.Controls.Add(this._chkNormalize);
            this._controls.Controls.Add(this._chkSkipBridging);
            this._controls.Controls.Add(this._lblTableTitle);
            this._controls.Controls.Add(this._dgvBonds);
            this._controls.Controls.Add(this._btnApply);
            this._controls.Controls.Add(this._btnReset);
            this._controls.Controls.Add(this._btnClose);
            this._controls.Controls.Add(this._lblLogTitle);
            this._controls.Controls.Add(this._rtbLog);
            this._controls.Controls.Add(this._lblStatus);

            this._rootPanel.Controls.Add(this._controls);

            // ═══════════════════════════════════════════════════════════
            //  HydrogenNormalizationWindow
            // ═══════════════════════════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(450, 710);
            this.Controls.Add(this._rootPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "HydrogenNormalizationWindow";
            this.Text = "Hydrogen Normalization";

            this._rootPanel.ResumeLayout(false);
            this._controls.ResumeLayout(false);
            this._controls.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._dgvBonds)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        #region Designer fields

        private System.Windows.Forms.Panel _rootPanel;
        private System.Windows.Forms.FlowLayoutPanel _controls;

        private System.Windows.Forms.Label _lblHeader;
        private System.Windows.Forms.Label _lblMoleculeInfo;

        private System.Windows.Forms.Label _lblOptionsTitle;
        private System.Windows.Forms.CheckBox _chkNormalize;
        private System.Windows.Forms.CheckBox _chkSkipBridging;

        private System.Windows.Forms.Label _lblTableTitle;
        private System.Windows.Forms.DataGridView _dgvBonds;

        private System.Windows.Forms.Button _btnApply;
        private System.Windows.Forms.Button _btnReset;
        private System.Windows.Forms.Button _btnClose;

        private System.Windows.Forms.Label _lblLogTitle;
        private System.Windows.Forms.RichTextBox _rtbLog;

        private System.Windows.Forms.Label _lblStatus;

        #endregion
    }
}