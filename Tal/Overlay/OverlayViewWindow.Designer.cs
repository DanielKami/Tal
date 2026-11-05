// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule overlay module: OverlayViewWindow.Designer.

﻿// OverlayViewWindow.Designer.cs
namespace Tal.Forms
{
    partial class OverlayViewWindow
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

        private void InitializeComponent()
        {
            this.SuspendLayout();
 // 
 // OverlayViewWindow
 // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 50);
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "OverlayViewWindow";
            this.Text = "🔬 Overlay View";
            this.ResumeLayout(false);
        }
    }
}