// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// DebugWindow designer-generated layout.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal.GUI
{
    partial class DebugWindow
    {
        private System.ComponentModel.IContainer components = null;

        // ⭐ DEKLARACJE KONTROLEK
        private System.Windows.Forms.RichTextBox _rtbLog;
        private System.Windows.Forms.Button _btnClear;
        private System.Windows.Forms.Button _btnRefresh;
        private System.Windows.Forms.Button _btnExport;
        private System.Windows.Forms.CheckBox _chkAutoScroll;
        private System.Windows.Forms.TableLayoutPanel _mainLayout;

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
            _mainLayout = new TableLayoutPanel();
            _rtbLog = new RichTextBox();
            _btnClear = new Button();
            _btnRefresh = new Button();
            _btnExport = new Button();
            _chkAutoScroll = new CheckBox();

            _mainLayout.SuspendLayout();
            SuspendLayout();

            // 
            // _mainLayout
            // 
            _mainLayout.ColumnCount = 4;
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            _mainLayout.Controls.Add(_rtbLog, 0, 1);
            _mainLayout.Controls.Add(_btnClear, 0, 0);
            _mainLayout.Controls.Add(_btnExport, 1, 0);
            _mainLayout.Controls.Add(_chkAutoScroll, 2, 0);
            _mainLayout.Controls.Add(_btnRefresh, 3, 0);
            _mainLayout.Dock = DockStyle.Fill;
            _mainLayout.Location = new Point(0, 0);
            _mainLayout.Name = "_mainLayout";
            _mainLayout.Padding = new Padding(5);
            _mainLayout.RowCount = 2;
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _mainLayout.Size = new Size(450, 400);
            _mainLayout.TabIndex = 0;

            // 
            // _rtbLog
            // 
            _mainLayout.SetColumnSpan(_rtbLog, 4);
            _rtbLog.BackColor = Color.FromArgb(30, 30, 30);
            _rtbLog.BorderStyle = BorderStyle.FixedSingle;
            _rtbLog.Dock = DockStyle.Fill;
            _rtbLog.Font = new Font("Consolas", 9F);
            _rtbLog.ForeColor = Color.FromArgb(220, 220, 220);
            _rtbLog.Location = new Point(8, 43);
            _rtbLog.Name = "_rtbLog";
            _rtbLog.Size = new Size(434, 349);
            _rtbLog.TabIndex = 0;
            _rtbLog.Text = "";
            _rtbLog.WordWrap = false;
            _rtbLog.ScrollBars = RichTextBoxScrollBars.Vertical;
            // ⭐ ReadOnly + keyboard shortcuts (Ctrl+C / Ctrl+A / context menu)
            _rtbLog.ReadOnly = true;
            _rtbLog.ShortcutsEnabled = true;

            // 
            // _btnClear
            // 
            _btnClear.BackColor = Color.FromArgb(230, 230, 235);
            _btnClear.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _btnClear.FlatStyle = FlatStyle.Flat;
            _btnClear.ForeColor = Color.FromArgb(51, 51, 51);
            _btnClear.Location = new Point(8, 8);
            _btnClear.Name = "_btnClear";
            _btnClear.Size = new Size(70, 27);
            _btnClear.TabIndex = 1;
            _btnClear.Text = "Clear";
            _btnClear.UseVisualStyleBackColor = false;
            _btnClear.Click += BtnClear_Click;

            // 
            // _btnExport
            // 
            _btnExport.BackColor = Color.FromArgb(230, 230, 235);
            _btnExport.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _btnExport.FlatStyle = FlatStyle.Flat;
            _btnExport.ForeColor = Color.FromArgb(51, 51, 51);
            _btnExport.Location = new Point(88, 8);
            _btnExport.Name = "_btnExport";
            _btnExport.Size = new Size(70, 27);
            _btnExport.TabIndex = 4;
            _btnExport.Text = "Export";
            _btnExport.UseVisualStyleBackColor = false;
            _btnExport.Click += BtnExport_Click;

            // 
            // _btnRefresh
            // 
            _btnRefresh.BackColor = Color.FromArgb(0, 122, 204);
            _btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(0, 122, 204);
            _btnRefresh.FlatStyle = FlatStyle.Flat;
            _btnRefresh.ForeColor = Color.White;
            _btnRefresh.Location = new Point(372, 8);
            _btnRefresh.Name = "_btnRefresh";
            _btnRefresh.Size = new Size(70, 27);
            _btnRefresh.TabIndex = 2;
            _btnRefresh.Text = "Refresh";
            _btnRefresh.UseVisualStyleBackColor = false;
            _btnRefresh.Click += BtnRefresh_Click;

            // 
            // _chkAutoScroll
            // 
            _chkAutoScroll.AutoSize = true;
            _chkAutoScroll.Checked = true;
            _chkAutoScroll.CheckState = CheckState.Checked;
            _chkAutoScroll.Font = new Font("Segoe UI", 9F);
            _chkAutoScroll.ForeColor = Color.FromArgb(51, 51, 51);
            _chkAutoScroll.Location = new Point(178, 8);
            _chkAutoScroll.Name = "_chkAutoScroll";
            _chkAutoScroll.Size = new Size(96, 24);
            _chkAutoScroll.TabIndex = 3;
            _chkAutoScroll.Text = "Auto scroll";
            _chkAutoScroll.UseVisualStyleBackColor = true;

            // 
            // DebugWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(450, 400);
            Controls.Add(_mainLayout);
            Font = new Font("Segoe UI", 9F);
            Name = "DebugWindow";
            Text = "🐞 Debug - Symmetry";
            _mainLayout.ResumeLayout(false);
            _mainLayout.PerformLayout();
            ResumeLayout(false);
        }
    }
}