// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// BondDisplayWindow designer.

namespace Tal.Forms
{
    partial class BondDisplayWindow
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.CheckBox _chkAromatic;
        private System.Windows.Forms.CheckBox _chkMultiple;
        private System.Windows.Forms.Label _lblHint;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            _lblTitle = new System.Windows.Forms.Label();
            _chkAromatic = new System.Windows.Forms.CheckBox();
            _chkMultiple = new System.Windows.Forms.CheckBox();
            _lblHint = new System.Windows.Forms.Label();

            SuspendLayout();

            _lblTitle.AutoSize = true;
            _lblTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            _lblTitle.Location = new System.Drawing.Point(12, 12);
            _lblTitle.Name = "_lblTitle";
            _lblTitle.Text = "Bond Display";

            _chkAromatic.AutoSize = true;
            _chkAromatic.Font = new System.Drawing.Font("Segoe UI", 9F);
            _chkAromatic.Location = new System.Drawing.Point(16, 48);
            _chkAromatic.Name = "_chkAromatic";
            _chkAromatic.Text = "Dashed ring inside aromatic rings";
            _chkAromatic.UseVisualStyleBackColor = true;

            _chkMultiple.AutoSize = true;
            _chkMultiple.Font = new System.Drawing.Font("Segoe UI", 9F);
            _chkMultiple.Location = new System.Drawing.Point(16, 80);
            _chkMultiple.Name = "_chkMultiple";
            _chkMultiple.Text = "Show double / triple bonds (parallel sticks)";
            _chkMultiple.UseVisualStyleBackColor = true;

            _lblHint.AutoSize = false;
            _lblHint.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);
            _lblHint.ForeColor = System.Drawing.Color.Gray;
            _lblHint.Location = new System.Drawing.Point(16, 120);
            _lblHint.Size = new System.Drawing.Size(280, 48);
            _lblHint.Text = "Orders are estimated from bond lengths.\nAromatic rings: 5- and 6-membered C/N rings.";

            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(320, 180);
            Controls.Add(_lblTitle);
            Controls.Add(_chkAromatic);
            Controls.Add(_chkMultiple);
            Controls.Add(_lblHint);
            Name = "BondDisplayWindow";
            Text = "Bond Display";

            ResumeLayout(false);
            PerformLayout();
        }
    }
}
