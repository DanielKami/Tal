// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Atom / measurement labels settings window.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class LabelsWindow : DockContent
    {
        #region Private Fields

        private bool _isUpdating = false;

        /// <summary>
        /// NDC ↔ pixel conversion scale for offset sliders.
        /// Suwak -100..+100 px ↔ NDC -0.2..+0.2 (typowy viewport ~1000 px).
        /// </summary>
        private const float OffsetNdcToSlider = 500f;

        #endregion

        #region Constructor

        public LabelsWindow()
        {
            InitializeComponent();
            DockWindowConfig.SetupWindow(this, "🏷️ Labels Settings");
            InitializeCustomComponents();
            LoadFromFlags();
        }

        #endregion

        #region Initialization

        private void InitializeCustomComponents()
        {
            // Podpinamy zdarzenia
            _chkLabels.CheckedChanged += ChkLabels_CheckedChanged;
            _trackFontSize.Scroll += TrackFontSize_Scroll;
            _btnFontColor.Click += BtnFontColor_Click;
            _chkShadow.CheckedChanged += ChkShadow_CheckedChanged;
            _btnShadowColor.Click += BtnShadowColor_Click;
            _trackOffsetX.Scroll += TrackOffsetX_Scroll;
            _trackOffsetY.Scroll += TrackOffsetY_Scroll;
            _btnReset.Click += BtnReset_Click;
            _btnApply.Click += BtnApply_Click;

            // ⭐ Font name + style (bold / italic / underline)
            _cmbFontName.SelectedIndexChanged += CmbFontName_SelectedIndexChanged;
            _chkBold.CheckedChanged += ChkBold_CheckedChanged;
            _chkItalic.CheckedChanged += ChkItalic_CheckedChanged;
            _chkUnderline.CheckedChanged += ChkUnderline_CheckedChanged;

            // Fill font list (method in LabelsWindow.Helpers.cs)
            LoadFonts();
        }

        #endregion

        #region Event Handlers

        private void ChkLabels_CheckedChanged(object sender, EventArgs e)
        {
            Flags.Instance.labels = _chkLabels.Checked;
            Flags.Instance.VisualRevision++;
            ApplyChanges();
        }

        private void TrackFontSize_Scroll(object sender, EventArgs e)
        {
            // ⭐ SKALA OD 0.1 DO 2.0 (krok 0.05)
            float scale = _trackFontSize.Value / 20f + 0.1f;
            Flags.Instance.LabelScale = scale;
            _labelFontSize.Text = scale.ToString("F2") + "x";
            Flags.Instance.VisualRevision++;
            UpdatePreview();
        }

        private void BtnFontColor_Click(object sender, EventArgs e)
        {
            using var dialog = new ColorDialog();
            dialog.Color = Color.FromArgb(
                Flags.Instance.LabelColor.A,
                Flags.Instance.LabelColor.R,
                Flags.Instance.LabelColor.G,
                Flags.Instance.LabelColor.B
            );
            dialog.FullOpen = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Flags.Instance.LabelColor = new Microsoft.Xna.Framework.Color(
                    dialog.Color.R,
                    dialog.Color.G,
                    dialog.Color.B,
                    dialog.Color.A
                );
                _panelColorPreview.BackColor = dialog.Color;
                Flags.Instance.VisualRevision++;
                UpdatePreview();
            }
        }

        private void ChkShadow_CheckedChanged(object sender, EventArgs e)
        {
            Flags.Instance.LabelShadow = _chkShadow.Checked;
            Flags.Instance.VisualRevision++;
            UpdatePreview();
        }

        private void BtnShadowColor_Click(object sender, EventArgs e)
        {
            using var dialog = new ColorDialog();
            dialog.Color = Color.FromArgb(
                Flags.Instance.LabelShadowColor.A,
                Flags.Instance.LabelShadowColor.R,
                Flags.Instance.LabelShadowColor.G,
                Flags.Instance.LabelShadowColor.B
            );
            dialog.FullOpen = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Flags.Instance.LabelShadowColor = new Microsoft.Xna.Framework.Color(
                    dialog.Color.R,
                    dialog.Color.G,
                    dialog.Color.B,
                    dialog.Color.A
                );
                _panelShadowPreview.BackColor = dialog.Color;
                UpdatePreview();
            }
        }

        private void TrackOffsetX_Scroll(object sender, EventArgs e)
        {
            // ⭐ Suwak -100..100 px → NDC -0.2..+0.2
            float ndc = _trackOffsetX.Value / OffsetNdcToSlider;
            Flags.Instance.LabelOffsetX = ndc;

            _labelOffsetX.Text = $"{_trackOffsetX.Value} px";
            Flags.Instance.VisualRevision++;
            UpdatePreview();
        }

        private void TrackOffsetY_Scroll(object sender, EventArgs e)
        {
            // ⭐ Suwak -100..100 px → NDC -0.2..+0.2
            float ndc = _trackOffsetY.Value / OffsetNdcToSlider;
            Flags.Instance.LabelOffsetY = ndc;

            _labelOffsetY.Text = $"{_trackOffsetY.Value} px";
            UpdatePreview();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            // Reset all settings to defaults
            Flags.Instance.labels = false;
            Flags.Instance.LabelColor = Microsoft.Xna.Framework.Color.Aqua;
            Flags.Instance.LabelScale = 0.6f;
            Flags.Instance.LabelShadow = true;
            Flags.Instance.LabelShadowColor = Microsoft.Xna.Framework.Color.Black;

            // ⭐ Offsets in NDC (correspond to 0 px / -20 px at ~1000 px viewport)
            Flags.Instance.LabelOffsetX = 0f;
            Flags.Instance.LabelOffsetY = -0.04f;

            // ⭐ Reset fontu
            Flags.Instance.LabelFontName = "Segoe UI";
            Flags.Instance.LabelFontBold = false;
            Flags.Instance.LabelFontItalic = false;
            Flags.Instance.LabelFontUnderline = false;
            Flags.Instance.VisualRevision++;
            LoadFromFlags();
            ApplyChanges();
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            ApplyChanges();
        }

        private void CmbFontName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;
            if (_cmbFontName.SelectedItem == null) return;

            Flags.Instance.LabelFontName = _cmbFontName.SelectedItem.ToString();
            Flags.Instance.VisualRevision++;
            UpdatePreview();
            ApplyChanges();
        }

        private void ChkBold_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;

            Flags.Instance.LabelFontBold = _chkBold.Checked;
            Flags.Instance.VisualRevision++;
            UpdatePreview();
            ApplyChanges();
        }

        private void ChkItalic_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;

            Flags.Instance.LabelFontItalic = _chkItalic.Checked;
            UpdatePreview();
            ApplyChanges();
        }

        private void ChkUnderline_CheckedChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;

            Flags.Instance.LabelFontUnderline = _chkUnderline.Checked;
            Flags.Instance.VisualRevision++;
            UpdatePreview();
            ApplyChanges();
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}