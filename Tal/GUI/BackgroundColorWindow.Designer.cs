// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// BackgroundColorWindow designer-generated layout.
// ⭐ v4: 20 swatchy w siatce 5×4, węższe okno (260 px).

namespace Tal.GUI
{
    partial class BackgroundColorWindow
    {
        private System.ComponentModel.IContainer components = null;

        // ============================================================
        // DEKLARACJE KONTROLEK
        // ============================================================

        private System.Windows.Forms.Button btnColor1;
        private System.Windows.Forms.Button btnColor2;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.Panel previewPanel;
        private System.Windows.Forms.Label lblColor1;
        private System.Windows.Forms.Label lblColor2;
        private System.Windows.Forms.Label lblColor1Value;
        private System.Windows.Forms.Label lblColor2Value;
        private System.Windows.Forms.ComboBox cmbPresets;
        private System.Windows.Forms.Label lblPresets;
        private System.Windows.Forms.Label lblQuickColors;
        private System.Windows.Forms.FlowLayoutPanel flpQuickColors;
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
            btnColor1 = new Button();
            btnColor2 = new Button();
            btnReset = new Button();
            previewPanel = new Panel();
            lblColor1 = new Label();
            lblColor2 = new Label();
            lblColor1Value = new Label();
            lblColor2Value = new Label();
            cmbPresets = new ComboBox();
            lblPresets = new Label();
            lblQuickColors = new Label();
            flpQuickColors = new FlowLayoutPanel();
            lblInfo = new Label();
            SuspendLayout();

            // ============================================================
            // ROZMIARY OKNA — 5 swatchy w rzędzie, wąskie okno
            // ============================================================
            // Szerokość wewnętrzna: 260 px
            //   lewy margines:    12
            //   prawy margines:   12
            //   szerokość treści: 236
            //
            // Swatche 5 w rzędzie: 5 · (36 + 8) = 220 px — mieści się w 236 px.
            // Wysokość panelu: 4 rzędy · (36 + 8) + padding = 4·44 + 8 = 184 px → panel 200 px.

            // 
            // btnColor1
            // 
            btnColor1.BackColor = Color.FromArgb(30, 30, 50);
            btnColor1.Cursor = Cursors.Hand;
            btnColor1.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnColor1.FlatStyle = FlatStyle.Flat;
            btnColor1.ForeColor = Color.White;
            btnColor1.Location = new Point(78, 20);
            btnColor1.Margin = new Padding(3, 4, 3, 4);
            btnColor1.Name = "btnColor1";
            btnColor1.Size = new Size(94, 30);
            btnColor1.TabIndex = 1;
            btnColor1.UseVisualStyleBackColor = false;

            // 
            // btnColor2
            // 
            btnColor2.BackColor = Color.FromArgb(10, 10, 30);
            btnColor2.Cursor = Cursors.Hand;
            btnColor2.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnColor2.FlatStyle = FlatStyle.Flat;
            btnColor2.ForeColor = Color.White;
            btnColor2.Location = new Point(78, 58);
            btnColor2.Margin = new Padding(3, 4, 3, 4);
            btnColor2.Name = "btnColor2";
            btnColor2.Size = new Size(94, 30);
            btnColor2.TabIndex = 4;
            btnColor2.UseVisualStyleBackColor = false;

            // 
            // previewPanel
            // 
            previewPanel.BackColor = Color.White;
            previewPanel.BorderStyle = BorderStyle.FixedSingle;
            previewPanel.Location = new Point(12, 102);
            previewPanel.Margin = new Padding(3, 4, 3, 4);
            previewPanel.Name = "previewPanel";
            previewPanel.Size = new Size(236, 80);
            previewPanel.TabIndex = 6;
            previewPanel.Paint += PreviewPanel_Paint;

            // 
            // lblColor1
            // 
            lblColor1.Font = new Font("Segoe UI", 9F);
            lblColor1.ForeColor = Color.FromArgb(51, 51, 51);
            lblColor1.Location = new Point(12, 24);
            lblColor1.Name = "lblColor1";
            lblColor1.Size = new Size(60, 22);
            lblColor1.TabIndex = 0;
            lblColor1.Text = "Top:";

            // 
            // lblColor2
            // 
            lblColor2.Font = new Font("Segoe UI", 9F);
            lblColor2.ForeColor = Color.FromArgb(51, 51, 51);
            lblColor2.Location = new Point(12, 62);
            lblColor2.Name = "lblColor2";
            lblColor2.Size = new Size(60, 22);
            lblColor2.TabIndex = 3;
            lblColor2.Text = "Bottom:";

            // 
            // lblColor1Value
            // 
            lblColor1Value.Font = new Font("Consolas", 8F);
            lblColor1Value.ForeColor = Color.FromArgb(120, 120, 120);
            lblColor1Value.Location = new Point(178, 24);
            lblColor1Value.Name = "lblColor1Value";
            lblColor1Value.Size = new Size(70, 22);
            lblColor1Value.TabIndex = 2;
            lblColor1Value.Text = "#1E1E32";
            lblColor1Value.TextAlign = ContentAlignment.MiddleRight;

            // 
            // lblColor2Value
            // 
            lblColor2Value.Font = new Font("Consolas", 8F);
            lblColor2Value.ForeColor = Color.FromArgb(120, 120, 120);
            lblColor2Value.Location = new Point(178, 62);
            lblColor2Value.Name = "lblColor2Value";
            lblColor2Value.Size = new Size(70, 22);
            lblColor2Value.TabIndex = 5;
            lblColor2Value.Text = "#0A0A1E";
            lblColor2Value.TextAlign = ContentAlignment.MiddleRight;

            // 
            // lblQuickColors
            // 
            lblQuickColors.Font = new Font("Segoe UI", 9F);
            lblQuickColors.ForeColor = Color.FromArgb(51, 51, 51);
            lblQuickColors.Location = new Point(12, 195);
            lblQuickColors.Name = "lblQuickColors";
            lblQuickColors.Size = new Size(236, 22);
            lblQuickColors.TabIndex = 7;
            lblQuickColors.Text = "⚡ Quick Colors:";

            // 
            // flpQuickColors
            // 
            // ⭐ 5 swatche w rzędzie × 4 rzędy = 20 kolorów.
            //    Swatch: 36×36 px + margines 4 px → szerokość rzędu = 5·(36+8) = 220 px.
            //    Panel: 236 × 200 px (padding 4 px + 4 rzędy × 44 px + luz 8 px).
            // 
            flpQuickColors.BackColor = Color.FromArgb(240, 240, 242);
            flpQuickColors.Location = new Point(12, 220);
            flpQuickColors.Margin = new Padding(3, 4, 3, 4);
            flpQuickColors.Name = "flpQuickColors";
            flpQuickColors.Padding = new Padding(4);
            flpQuickColors.Size = new Size(236, 200);
            flpQuickColors.TabIndex = 8;
            flpQuickColors.FlowDirection = FlowDirection.LeftToRight;
            flpQuickColors.WrapContents = true;
            flpQuickColors.AutoSize = false;

            // ⭐ 20 SWATCHY — 5 w rzędzie, 4 rzędy
            // ============================================================
            // Rząd 1 — JASNE (ciepłe i chłodne)
            // ============================================================
            AddQuickColorPanel(Color.White, "White");
            AddQuickColorPanel(Color.FromArgb(173, 216, 230), "Light Blue");
            AddQuickColorPanel(Color.FromArgb(255, 235, 200), "Straw");
            AddQuickColorPanel(Color.FromArgb(255, 255, 220), "Canary");
            AddQuickColorPanel(Color.FromArgb(240, 230, 245), "Lavender");

            // ============================================================
            // Rząd 2 — CIEPŁE i PASTELOWE
            // ============================================================
            AddQuickColorPanel(Color.FromArgb(255, 200, 100), "Golden");
            AddQuickColorPanel(Color.FromArgb(255, 240, 222), "Peach");
            AddQuickColorPanel(Color.FromArgb(222, 184, 135), "Sand");
            AddQuickColorPanel(Color.FromArgb(210, 180, 140), "Tan");
            AddQuickColorPanel(Color.FromArgb(188, 143, 143), "Rose");

            // ============================================================
            // Rząd 3 — RETRO CRT + KOLORY ŚREDNIE
            // ============================================================
            AddQuickColorPanel(Color.FromArgb(255, 176, 0), "Amber CRT");    // P3 phosphor
            AddQuickColorPanel(Color.FromArgb(51, 255, 51), "Green CRT");    // P1 phosphor
            AddQuickColorPanel(Color.FromArgb(120, 0, 0), "Dark Red");     // bordo
            AddQuickColorPanel(Color.FromArgb(10, 10, 80), "Dark Blue");    // navy
            AddQuickColorPanel(Color.FromArgb(75, 0, 130), "Indigo");

            // ============================================================
            // Rząd 4 — CIEMNE
            // ============================================================
            AddQuickColorPanel(Color.Black, "Black");
            AddQuickColorPanel(Color.FromArgb(30, 30, 50), "Charcoal");
            AddQuickColorPanel(Color.FromArgb(0, 30, 60), "Ocean");
            AddQuickColorPanel(Color.FromArgb(20, 40, 20), "Forest");
            AddQuickColorPanel(Color.FromArgb(0, 100, 100), "Teal");

            // 
            // lblInfo
            // 
            lblInfo.Font = new Font("Segoe UI", 8F, FontStyle.Italic);
            lblInfo.ForeColor = Color.FromArgb(120, 120, 120);
            lblInfo.Location = new Point(12, 430);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new Size(236, 40);
            lblInfo.TabIndex = 11;
            lblInfo.Text = "Use Top/Bottom pickers for gradient,\nor click a square for solid color.";

            // 
            // lblPresets
            // 
            lblPresets.Font = new Font("Segoe UI", 9F);
            lblPresets.ForeColor = Color.FromArgb(51, 51, 51);
            lblPresets.Location = new Point(12, 480);
            lblPresets.Name = "lblPresets";
            lblPresets.Size = new Size(64, 22);
            lblPresets.TabIndex = 9;
            lblPresets.Text = "Presets:";

            // 
            // cmbPresets
            // 
            cmbPresets.BackColor = Color.White;
            cmbPresets.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPresets.FlatStyle = FlatStyle.Flat;
            cmbPresets.ForeColor = Color.FromArgb(51, 51, 51);
            cmbPresets.Location = new Point(78, 477);
            cmbPresets.Margin = new Padding(3, 4, 3, 4);
            cmbPresets.Name = "cmbPresets";
            cmbPresets.Size = new Size(170, 28);
            cmbPresets.TabIndex = 10;

            // 
            // btnReset
            // 
            btnReset.BackColor = Color.FromArgb(230, 230, 235);
            btnReset.Cursor = Cursors.Hand;
            btnReset.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            btnReset.FlatStyle = FlatStyle.Flat;
            btnReset.ForeColor = Color.FromArgb(51, 51, 51);
            btnReset.Location = new Point(12, 517);
            btnReset.Margin = new Padding(3, 4, 3, 4);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(236, 34);
            btnReset.TabIndex = 12;
            btnReset.Text = "🔄 Reset to Default";
            btnReset.UseVisualStyleBackColor = false;

            // 
            // BackgroundColorWindow
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(248, 248, 250);
            ClientSize = new Size(260, 565);          // ⭐ węższe niż v3 (300 px)
            Controls.Add(lblColor1);
            Controls.Add(btnColor1);
            Controls.Add(lblColor1Value);
            Controls.Add(lblColor2);
            Controls.Add(btnColor2);
            Controls.Add(lblColor2Value);
            Controls.Add(previewPanel);
            Controls.Add(lblQuickColors);
            Controls.Add(flpQuickColors);
            Controls.Add(lblInfo);
            Controls.Add(lblPresets);
            Controls.Add(cmbPresets);
            Controls.Add(btnReset);
            ForeColor = Color.FromArgb(51, 51, 51);
            Margin = new Padding(3, 4, 3, 4);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(240, 520);
            Name = "BackgroundColorWindow";
            Text = "🎨 Background Colors";
            ResumeLayout(false);
        }

        // ============================================================
        // ⭐ METODA POMOCNICZA — DODAWANIE KWADRACIKA
        // ============================================================
        // Rozmiar 36×36 px, margines 4 px → 5 swatchy w rzędzie zajmują 220 px,
        // mieszczą się w panelu o szerokości 236 px z paddingiem.
        // ============================================================

        private void AddQuickColorPanel(System.Drawing.Color color, string name)
        {
            var panel = new System.Windows.Forms.Panel();
            panel.Size = new System.Drawing.Size(36, 36);
            panel.BackColor = color;
            panel.Tag = color;
            panel.Cursor = System.Windows.Forms.Cursors.Hand;
            panel.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

            var toolTip = new System.Windows.Forms.ToolTip();
            toolTip.SetToolTip(panel, $"{name}\nClick to set solid background color");

            this.flpQuickColors.Controls.Add(panel);
        }

        // ============================================================
        // ⭐ PREVIEW DRAWING
        // ============================================================

        private void PreviewPanel_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                this.previewPanel.ClientRectangle,
                this._color1,
                this._color2,
                System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(brush, this.previewPanel.ClientRectangle);
            }

            e.Graphics.DrawRectangle(
                new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 200, 200)),
                0, 0, this.previewPanel.Width - 1, this.previewPanel.Height - 1
            );

            using (var font = new System.Drawing.Font("Segoe UI", 8F))
            using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(80, 80, 80)))
            {
                e.Graphics.DrawString("Top", font, brush, 8, 8);
                e.Graphics.DrawString("Bottom", font, brush, 8, this.previewPanel.Height - 22);
            }
        }
    }
}