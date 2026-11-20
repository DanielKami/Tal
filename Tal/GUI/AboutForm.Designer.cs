// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// About dialog designer-generated layout (VS2022 style).
// Controls are positioned so the scrollable credits box never overlaps the copyright text.

using System.Drawing;
using System.Windows.Forms;

namespace Tal
{
    partial class AboutForm
    {
        private System.ComponentModel.IContainer components = null;

        private Panel panelHeader;
        private PictureBox pictureSplash;
        private Label labelTitle;
        private Label labelSubtitle;
        private Label labelInfo;
        private Label labelRefsCaption;
        private TextBox textBoxRefs;
        private Button buttonOK;

        private void InitializeComponent()
        {
            panelHeader = new Panel();
            pictureSplash = new PictureBox();
            labelTitle = new Label();
            labelSubtitle = new Label();
            labelInfo = new Label();
            labelRefsCaption = new Label();
            textBoxRefs = new TextBox();
            buttonOK = new Button();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureSplash).BeginInit();
            SuspendLayout();

            // panelHeader
            panelHeader.BackColor = Color.White;
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Height = 120;
            panelHeader.Name = "panelHeader";
            panelHeader.TabIndex = 0;
            panelHeader.Controls.Add(pictureSplash);
            panelHeader.Controls.Add(labelTitle);
            panelHeader.Controls.Add(labelSubtitle);

            // pictureSplash — right side, clear of text
            pictureSplash.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pictureSplash.BackColor = Color.FromArgb(10, 20, 45);
            pictureSplash.Location = new Point(360, 20);
            pictureSplash.Name = "pictureSplash";
            pictureSplash.Size = new Size(120, 80);
            pictureSplash.SizeMode = PictureBoxSizeMode.Zoom;
            pictureSplash.TabIndex = 0;
            pictureSplash.TabStop = false;

            // labelTitle
            labelTitle.AutoSize = true;
            labelTitle.Font = new Font("Segoe UI", 36F, FontStyle.Italic | FontStyle.Bold);
            labelTitle.ForeColor = Color.FromArgb(0, 102, 204);
            labelTitle.Location = new Point(16, 12);
            labelTitle.Name = "labelTitle";
            labelTitle.TabIndex = 1;
            labelTitle.Text = "TAL";

            // labelSubtitle
            labelSubtitle.AutoSize = true;
            labelSubtitle.Font = new Font("Segoe UI", 11F, FontStyle.Italic);
            labelSubtitle.ForeColor = Color.FromArgb(60, 60, 60);
            labelSubtitle.Location = new Point(18, 72);
            labelSubtitle.Name = "labelSubtitle";
            labelSubtitle.TabIndex = 2;
            labelSubtitle.Text = "Crystallographic Application";

            // labelInfo  (starts below header)
            labelInfo.Font = new Font("Segoe UI", 9F);
            labelInfo.ForeColor = Color.Black;
            labelInfo.Location = new Point(18, 132);
            labelInfo.Name = "labelInfo";
            labelInfo.Size = new Size(464, 170);
            labelInfo.TabIndex = 1;

            // labelRefsCaption
            labelRefsCaption.AutoSize = true;
            labelRefsCaption.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            labelRefsCaption.Location = new Point(18, 310);
            labelRefsCaption.Name = "labelRefsCaption";
            labelRefsCaption.TabIndex = 2;
            labelRefsCaption.Text = "Based on / third-party projects:";

            // textBoxRefs
            textBoxRefs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxRefs.BackColor = Color.FromArgb(248, 248, 248);
            textBoxRefs.BorderStyle = BorderStyle.FixedSingle;
            textBoxRefs.Font = new Font("Segoe UI", 9F);
            textBoxRefs.ForeColor = Color.Black;
            textBoxRefs.Location = new Point(16, 334);
            textBoxRefs.Multiline = true;
            textBoxRefs.Name = "textBoxRefs";
            textBoxRefs.ReadOnly = true;
            textBoxRefs.ScrollBars = ScrollBars.Vertical;
            textBoxRefs.Size = new Size(468, 200);
            textBoxRefs.TabIndex = 3;
            textBoxRefs.TabStop = false;
            textBoxRefs.WordWrap = true;

            // buttonOK
            buttonOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonOK.DialogResult = DialogResult.OK;
            buttonOK.Location = new Point(394, 546);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new Size(90, 28);
            buttonOK.TabIndex = 4;
            buttonOK.Text = "OK";
            buttonOK.UseVisualStyleBackColor = true;

            // AboutForm
            AcceptButton = buttonOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(500, 586);
            Controls.Add(buttonOK);
            Controls.Add(textBoxRefs);
            Controls.Add(labelRefsCaption);
            Controls.Add(labelInfo);
            Controls.Add(panelHeader);
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "About Tal";
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureSplash).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
