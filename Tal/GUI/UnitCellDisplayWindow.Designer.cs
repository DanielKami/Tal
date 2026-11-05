// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal.Forms
{
    partial class UnitCellDisplayWindow
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.CheckBox _chkShowCell;
        private System.Windows.Forms.CheckBox _chkAxisLabels;
        private System.Windows.Forms.GroupBox _grpLines;
        private System.Windows.Forms.Label _lblThickness;
        private System.Windows.Forms.TrackBar _trackThickness;
        private System.Windows.Forms.Label _lblThicknessVal;
        private System.Windows.Forms.Label _lblFactor;
        private System.Windows.Forms.TrackBar _trackFactor;
        private System.Windows.Forms.Label _lblFactorVal;
        private System.Windows.Forms.GroupBox _grpLabels;
        private System.Windows.Forms.Label _lblFont;
        private System.Windows.Forms.TrackBar _trackFont;
        private System.Windows.Forms.Label _lblFontVal;
        private System.Windows.Forms.Button _btnResetOffsets;
        private System.Windows.Forms.Button _btnDefault;
        private System.Windows.Forms.Button _btnApply;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            _lblTitle = new System.Windows.Forms.Label();
            _chkShowCell = new System.Windows.Forms.CheckBox();
            _chkAxisLabels = new System.Windows.Forms.CheckBox();
            _grpLines = new System.Windows.Forms.GroupBox();
            _lblThickness = new System.Windows.Forms.Label();
            _trackThickness = new System.Windows.Forms.TrackBar();
            _lblThicknessVal = new System.Windows.Forms.Label();
            _lblFactor = new System.Windows.Forms.Label();
            _trackFactor = new System.Windows.Forms.TrackBar();
            _lblFactorVal = new System.Windows.Forms.Label();
            _grpLabels = new System.Windows.Forms.GroupBox();
            _lblFont = new System.Windows.Forms.Label();
            _trackFont = new System.Windows.Forms.TrackBar();
            _lblFontVal = new System.Windows.Forms.Label();
            _btnResetOffsets = new System.Windows.Forms.Button();
            _btnDefault = new System.Windows.Forms.Button();
            _btnApply = new System.Windows.Forms.Button();

            ((System.ComponentModel.ISupportInitialize)_trackThickness).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackFactor).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_trackFont).BeginInit();
            _grpLines.SuspendLayout();
            _grpLabels.SuspendLayout();
            SuspendLayout();

            _lblTitle.AutoSize = true;
            _lblTitle.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            _lblTitle.Location = new System.Drawing.Point(12, 12);
            _lblTitle.Name = "_lblTitle";
            _lblTitle.Text = "Unit Cell Frame";

            _chkShowCell.AutoSize = true;
            _chkShowCell.Font = new System.Drawing.Font("Segoe UI", 9F);
            _chkShowCell.Location = new System.Drawing.Point(16, 44);
            _chkShowCell.Name = "_chkShowCell";
            _chkShowCell.Text = "Show unit cell";
            _chkShowCell.UseVisualStyleBackColor = true;

            _chkAxisLabels.AutoSize = true;
            _chkAxisLabels.Font = new System.Drawing.Font("Segoe UI", 9F);
            _chkAxisLabels.Location = new System.Drawing.Point(16, 72);
            _chkAxisLabels.Name = "_chkAxisLabels";
            _chkAxisLabels.Text = "Show axis labels (a, b, c)";
            _chkAxisLabels.UseVisualStyleBackColor = true;

            _grpLines.Controls.Add(_lblThickness);
            _grpLines.Controls.Add(_trackThickness);
            _grpLines.Controls.Add(_lblThicknessVal);
            _grpLines.Controls.Add(_lblFactor);
            _grpLines.Controls.Add(_trackFactor);
            _grpLines.Controls.Add(_lblFactorVal);
            _grpLines.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            _grpLines.Location = new System.Drawing.Point(12, 104);
            _grpLines.Name = "_grpLines";
            _grpLines.Size = new System.Drawing.Size(300, 120);
            _grpLines.TabStop = false;
            _grpLines.Text = "LINE STYLE";

            _lblThickness.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            _lblThickness.Location = new System.Drawing.Point(10, 28);
            _lblThickness.Size = new System.Drawing.Size(100, 20);
            _lblThickness.Text = "Thickness:";
            _lblThickness.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            _trackThickness.Location = new System.Drawing.Point(110, 24);
            _trackThickness.Minimum = 1;
            _trackThickness.Maximum = 100;
            _trackThickness.TickFrequency = 5;
            _trackThickness.Value = 15;
            _trackThickness.Size = new System.Drawing.Size(120, 45);

            _lblThicknessVal.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            _lblThicknessVal.Location = new System.Drawing.Point(235, 28);
            _lblThicknessVal.Size = new System.Drawing.Size(55, 20);
            _lblThicknessVal.Text = "0.015";
            _lblThicknessVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            _lblFactor.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            _lblFactor.Location = new System.Drawing.Point(10, 72);
            _lblFactor.Size = new System.Drawing.Size(100, 20);
            _lblFactor.Text = "Length factor:";
            _lblFactor.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            _trackFactor.Location = new System.Drawing.Point(110, 68);
            _trackFactor.Minimum = 50;
            _trackFactor.Maximum = 200;
            _trackFactor.TickFrequency = 10;
            _trackFactor.Value = 127;
            _trackFactor.Size = new System.Drawing.Size(120, 45);

            _lblFactorVal.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            _lblFactorVal.Location = new System.Drawing.Point(235, 72);
            _lblFactorVal.Size = new System.Drawing.Size(55, 20);
            _lblFactorVal.Text = "0.0127";
            _lblFactorVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            _grpLabels.Controls.Add(_lblFont);
            _grpLabels.Controls.Add(_trackFont);
            _grpLabels.Controls.Add(_lblFontVal);
            _grpLabels.Controls.Add(_btnResetOffsets);
            _grpLabels.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            _grpLabels.Location = new System.Drawing.Point(12, 232);
            _grpLabels.Name = "_grpLabels";
            _grpLabels.Size = new System.Drawing.Size(300, 110);
            _grpLabels.TabStop = false;
            _grpLabels.Text = "AXIS LABELS a / b / c";

            _lblFont.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            _lblFont.Location = new System.Drawing.Point(10, 28);
            _lblFont.Size = new System.Drawing.Size(100, 20);
            _lblFont.Text = "Font size:";
            _lblFont.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            _trackFont.Location = new System.Drawing.Point(110, 24);
            _trackFont.Minimum = 30;
            _trackFont.Maximum = 200;
            _trackFont.TickFrequency = 10;
            _trackFont.Value = 100;
            _trackFont.Size = new System.Drawing.Size(120, 45);

            _lblFontVal.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            _lblFontVal.Location = new System.Drawing.Point(235, 28);
            _lblFontVal.Size = new System.Drawing.Size(55, 20);
            _lblFontVal.Text = "1.00";
            _lblFontVal.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            _btnResetOffsets.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            _btnResetOffsets.Location = new System.Drawing.Point(10, 70);
            _btnResetOffsets.Size = new System.Drawing.Size(160, 26);
            _btnResetOffsets.Text = "Reset label positions";
            _btnResetOffsets.UseVisualStyleBackColor = true;

            _btnDefault.Location = new System.Drawing.Point(12, 352);
            _btnDefault.Size = new System.Drawing.Size(90, 28);
            _btnDefault.Text = "Default";
            _btnDefault.UseVisualStyleBackColor = true;

            _btnApply.Location = new System.Drawing.Point(222, 352);
            _btnApply.Size = new System.Drawing.Size(90, 28);
            _btnApply.Text = "Apply";
            _btnApply.UseVisualStyleBackColor = true;

            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(328, 396);
            Controls.Add(_lblTitle);
            Controls.Add(_chkShowCell);
            Controls.Add(_chkAxisLabels);
            Controls.Add(_grpLines);
            Controls.Add(_grpLabels);
            Controls.Add(_btnDefault);
            Controls.Add(_btnApply);
            Name = "UnitCellDisplayWindow";
            Text = "Unit Cell Frame";

            ((System.ComponentModel.ISupportInitialize)_trackThickness).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackFactor).EndInit();
            ((System.ComponentModel.ISupportInitialize)_trackFont).EndInit();
            _grpLines.ResumeLayout(false);
            _grpLabels.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
