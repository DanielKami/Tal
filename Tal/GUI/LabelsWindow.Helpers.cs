// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Labels window helpers — property lists and formatting.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.GUI;

namespace Tal.Forms
{
    /// <summary>
    /// Logic pomocnicza for LabelsWindow:
    ///   - loading system fonts
    ///   - synchronizacja z Flags
    ///   - label preview refresh
    ///   - propagacja zmian do views 3D
    /// </summary>
    public partial class LabelsWindow
    {
        #region Font Loading

        /// <summary>
        /// Fills the combobox with available fonts.
        /// ⭐ Pokazujemy ONLY te, which we have w SpriteFont cache (MonoGame).
        /// </summary>
        private void LoadFonts()
        {
            _isUpdating = true;
            try
            {
                _cmbFontName.Items.Clear();

                // ⭐ Only fonts, which we have w .xnb (Content/Fonts/)
                _cmbFontName.Items.Add("Segoe UI");
                _cmbFontName.Items.Add("Arial");
                _cmbFontName.Items.Add("Times New Roman");

                // Set currently selected from Flags
                string current = Flags.Instance.LabelFontName ?? "Segoe UI";
                int index = _cmbFontName.Items.IndexOf(current);

                if (index >= 0)
                    _cmbFontName.SelectedIndex = index;
                else
                    _cmbFontName.SelectedIndex = 0;   // Segoe UI by default
            }
            finally
            {
                _isUpdating = false;
            }
        }

        #endregion

        #region Load from Flags

        /// <summary>
        /// Wczytuje current settings labels z Flags do kontrolek UI.
        /// ⭐ Offset: NDC (-0.2..+0.2) → suwak (-100..+100 px).
        /// </summary>
        private void LoadFromFlags()
        {
            _isUpdating = true;

            _chkLabels.Checked = Flags.Instance.labels;

            // ⭐ Font name (combo)
            string fontName = Flags.Instance.LabelFontName ?? "Segoe UI";
            int fontIndex = _cmbFontName.Items.IndexOf(fontName);
            if (fontIndex >= 0)
                _cmbFontName.SelectedIndex = fontIndex;

            // ⭐ Bold / Italic / Underline
            _chkBold.Checked = Flags.Instance.LabelFontBold;
            _chkItalic.Checked = Flags.Instance.LabelFontItalic;
            _chkUnderline.Checked = Flags.Instance.LabelFontUnderline;

            // ⭐ SKALA - przelicz na value trackbara
            float scale = Flags.Instance.LabelScale;
            int trackValue = (int)((scale - 0.1f) * 20);
            _trackFontSize.Value = Math.Clamp(trackValue, 0, 38);
            _labelFontSize.Text = scale.ToString("F2") + "x";

            // Colour fonts
            var color = Color.FromArgb(
                Flags.Instance.LabelColor.A,
                Flags.Instance.LabelColor.R,
                Flags.Instance.LabelColor.G,
                Flags.Instance.LabelColor.B
            );
            _panelColorPreview.BackColor = color;

            // Shadow
            _chkShadow.Checked = Flags.Instance.LabelShadow;
            var shadowColor = Color.FromArgb(
                Flags.Instance.LabelShadowColor.A,
                Flags.Instance.LabelShadowColor.R,
                Flags.Instance.LabelShadowColor.G,
                Flags.Instance.LabelShadowColor.B
            );
            _panelShadowPreview.BackColor = shadowColor;

            // ⭐ Offset: NDC (-0.2..+0.2) → suwak (-100..+100 px)
            int sliderX = (int)Math.Clamp(
                Flags.Instance.LabelOffsetX * OffsetNdcToSlider, -100, 100);
            int sliderY = (int)Math.Clamp(
                Flags.Instance.LabelOffsetY * OffsetNdcToSlider, -100, 100);

            _trackOffsetX.Value = sliderX;
            _trackOffsetY.Value = sliderY;
            _labelOffsetX.Text = $"{sliderX} px";
            _labelOffsetY.Text = $"{sliderY} px";

            _isUpdating = false;
            UpdatePreview();
        }

        #endregion

        #region Preview

        /// <summary>
        /// Refreshes the label preview ("Aa") in the Preview panel.
        /// ⭐ Uses aktualnego fontu z Flags (LabelFontName + style).
        /// </summary>
        private void UpdatePreview()
        {
            if (_isUpdating) return;

            string previewText = "Aa";
            float scale = Flags.Instance.LabelScale;

            // ⭐ Font + style z Flags
            string fontName = Flags.Instance.LabelFontName ?? "Segoe UI";

            FontStyle style = FontStyle.Regular;
            if (Flags.Instance.LabelFontBold) style |= FontStyle.Bold;
            if (Flags.Instance.LabelFontItalic) style |= FontStyle.Italic;
            if (Flags.Instance.LabelFontUnderline) style |= FontStyle.Underline;

            try
            {
                float baseSize = 16f;
                _labelPreview.Font = new Font(fontName, baseSize * scale, style);
            }
            catch
            {
                try
                {
                    _labelPreview.Font = new Font("Segoe UI", 16f * scale, style);
                }
                catch
                {
                    _labelPreview.Font = new Font("Segoe UI", 16f * scale);
                }
            }

            // Colour fonts
            _labelPreview.ForeColor = Color.FromArgb(
                Flags.Instance.LabelColor.A,
                Flags.Instance.LabelColor.R,
                Flags.Instance.LabelColor.G,
                Flags.Instance.LabelColor.B
            );

            // ⭐ COLOURED SHADOW
            if (Flags.Instance.LabelShadow)
            {
                _labelPreviewShadow.Visible = true;
                _labelPreviewShadow.Text = previewText;
                _labelPreviewShadow.Font = _labelPreview.Font;
                _labelPreviewShadow.ForeColor = Color.FromArgb(
                    Flags.Instance.LabelShadowColor.A,
                    Flags.Instance.LabelShadowColor.R,
                    Flags.Instance.LabelShadowColor.G,
                    Flags.Instance.LabelShadowColor.B
                );
                _labelPreviewShadow.Left = _labelPreview.Left + 2;
                _labelPreviewShadow.Top = _labelPreview.Top + 2;
            }
            else
            {
                _labelPreviewShadow.Visible = false;
            }

            _labelPreview.Text = previewText;
            _labelPreviewShadow.Text = previewText;
        }

        #endregion

        #region Apply Changes

        /// <summary>
        /// Propagates changes do all open views 3D.
        /// </summary>
        private void ApplyChanges()
        {
            Flags.Instance.MarkDirty();

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm != null)
            {
                var dockPanel = mainForm.GetDockPanel();
                if (dockPanel != null)
                {
                    foreach (var doc in dockPanel.Documents)
                    {
                        if (doc is MonoGameDocument monoDoc)
                        {
                            monoDoc.RefreshView();
                        }
                    }
                }

                mainForm.SetStatus("Labels updated",
                    $"Structures: {StructureStorage.Instance.StructureCount}", "🏷️");
            }
        }

        #endregion
    }
}