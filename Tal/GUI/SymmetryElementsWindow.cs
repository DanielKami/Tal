// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Symmetry elements visibility / style window.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.Forms;
using Tal.Utils;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.GUI
{
    /// <summary>
    /// Symmetry elements display settings window.
    /// Every element has its own checkbox and color swatch.
    /// Sizes, transparency, and colors act on-the-fly (no Apply button).
    /// Any change instantly refreshes the 3D view.
    /// </summary>
    public partial class SymmetryElementsWindow : DockContent
    {
        private bool _isUpdating = false;

        public SymmetryElementsWindow()
        {
            InitializeComponent();
            DockWindowConfig.SetupWindow(this, "🔷 Symmetry Elements");
            WireColorButtonNames();
            UpdateFromFlags();
        }

        private void WireColorButtonNames()
        {
            _btnColorRotation2.Name = "_btnColorRotation2";
            _btnColorRotation3.Name = "_btnColorRotation3";
            _btnColorRotation4.Name = "_btnColorRotation4";
            _btnColorRotation6.Name = "_btnColorRotation6";

            _btnColorScrew21.Name = "_btnColorScrew21";
            _btnColorScrew31.Name = "_btnColorScrew31";
            _btnColorScrew32.Name = "_btnColorScrew32";
            _btnColorScrew41.Name = "_btnColorScrew41";
            _btnColorScrew42.Name = "_btnColorScrew42";
            _btnColorScrew43.Name = "_btnColorScrew43";
            _btnColorScrew61.Name = "_btnColorScrew61";
            _btnColorScrew62.Name = "_btnColorScrew62";
            _btnColorScrew63.Name = "_btnColorScrew63";
            _btnColorScrew64.Name = "_btnColorScrew64";
            _btnColorScrew65.Name = "_btnColorScrew65";

            _btnColorRotoinv3.Name = "_btnColorRotoinv3";
            _btnColorRotoinv4.Name = "_btnColorRotoinv4";
            _btnColorRotoinv6.Name = "_btnColorRotoinv6";

            _btnColorMirror.Name = "_btnColorMirror";
            _btnColorGlide.Name = "_btnColorGlide";
            _btnColorCenter.Name = "_btnColorCenter";
        }

        // ═══════════════════════════════════════════════════════════════
        //  FLAGS → UI
        // ═══════════════════════════════════════════════════════════════

        public void UpdateFromFlags()
        {
            _isUpdating = true;
            var f = Flags.Instance;

            _chkShowSymmetryElements.Checked = f.ShowSymmetryElements;

            // Visibility
            _chkShowRotation2.Checked = f.ShowRotationAxis2;
            _chkShowRotation3.Checked = f.ShowRotationAxis3;
            _chkShowRotation4.Checked = f.ShowRotationAxis4;
            _chkShowRotation6.Checked = f.ShowRotationAxis6;

            _chkShowScrew21.Checked = f.ShowScrew21;
            _chkShowScrew31.Checked = f.ShowScrew31;
            _chkShowScrew32.Checked = f.ShowScrew32;
            _chkShowScrew41.Checked = f.ShowScrew41;
            _chkShowScrew42.Checked = f.ShowScrew42;
            _chkShowScrew43.Checked = f.ShowScrew43;
            _chkShowScrew61.Checked = f.ShowScrew61;
            _chkShowScrew62.Checked = f.ShowScrew62;
            _chkShowScrew63.Checked = f.ShowScrew63;
            _chkShowScrew64.Checked = f.ShowScrew64;
            _chkShowScrew65.Checked = f.ShowScrew65;

            _chkShowRotoinv3.Checked = f.ShowRotoinv3;
            _chkShowRotoinv4.Checked = f.ShowRotoinv4;
            _chkShowRotoinv6.Checked = f.ShowRotoinv6;

            _chkShowMirrorPlanes.Checked = f.ShowMirrorPlanes;
            _chkShowGlidePlanes.Checked = f.ShowGlidePlanes;
            _chkShowInversionCenters.Checked = f.ShowInversionCenters;

            // Colors — osie obrotu
            _btnColorRotation2.BackColor = FloatToColor(f.SymmetryAxis2ColorR, f.SymmetryAxis2ColorG, f.SymmetryAxis2ColorB);
            _btnColorRotation3.BackColor = FloatToColor(f.SymmetryAxis3ColorR, f.SymmetryAxis3ColorG, f.SymmetryAxis3ColorB);
            _btnColorRotation4.BackColor = FloatToColor(f.SymmetryAxis4ColorR, f.SymmetryAxis4ColorG, f.SymmetryAxis4ColorB);
            _btnColorRotation6.BackColor = FloatToColor(f.SymmetryAxis6ColorR, f.SymmetryAxis6ColorG, f.SymmetryAxis6ColorB);

            // Colours — screw axes
            _btnColorScrew21.BackColor = FloatToColor(f.SymmetryScrew21ColorR, f.SymmetryScrew21ColorG, f.SymmetryScrew21ColorB);
            _btnColorScrew31.BackColor = FloatToColor(f.SymmetryScrew31ColorR, f.SymmetryScrew31ColorG, f.SymmetryScrew31ColorB);
            _btnColorScrew32.BackColor = FloatToColor(f.SymmetryScrew32ColorR, f.SymmetryScrew32ColorG, f.SymmetryScrew32ColorB);
            _btnColorScrew41.BackColor = FloatToColor(f.SymmetryScrew41ColorR, f.SymmetryScrew41ColorG, f.SymmetryScrew41ColorB);
            _btnColorScrew42.BackColor = FloatToColor(f.SymmetryScrew42ColorR, f.SymmetryScrew42ColorG, f.SymmetryScrew42ColorB);
            _btnColorScrew43.BackColor = FloatToColor(f.SymmetryScrew43ColorR, f.SymmetryScrew43ColorG, f.SymmetryScrew43ColorB);
            _btnColorScrew61.BackColor = FloatToColor(f.SymmetryScrew61ColorR, f.SymmetryScrew61ColorG, f.SymmetryScrew61ColorB);
            _btnColorScrew62.BackColor = FloatToColor(f.SymmetryScrew62ColorR, f.SymmetryScrew62ColorG, f.SymmetryScrew62ColorB);
            _btnColorScrew63.BackColor = FloatToColor(f.SymmetryScrew63ColorR, f.SymmetryScrew63ColorG, f.SymmetryScrew63ColorB);
            _btnColorScrew64.BackColor = FloatToColor(f.SymmetryScrew64ColorR, f.SymmetryScrew64ColorG, f.SymmetryScrew64ColorB);
            _btnColorScrew65.BackColor = FloatToColor(f.SymmetryScrew65ColorR, f.SymmetryScrew65ColorG, f.SymmetryScrew65ColorB);

            // Colors — rotoinwersje
            _btnColorRotoinv3.BackColor = FloatToColor(f.SymmetryRotoinv3ColorR, f.SymmetryRotoinv3ColorG, f.SymmetryRotoinv3ColorB);
            _btnColorRotoinv4.BackColor = FloatToColor(f.SymmetryRotoinv4ColorR, f.SymmetryRotoinv4ColorG, f.SymmetryRotoinv4ColorB);
            _btnColorRotoinv6.BackColor = FloatToColor(f.SymmetryRotoinv6ColorR, f.SymmetryRotoinv6ColorG, f.SymmetryRotoinv6ColorB);

            // ⭐ Colours — planes and centres (do NOT clip!)
            _btnColorMirror.BackColor = FloatToColor(f.SymmetryMirrorColorR, f.SymmetryMirrorColorG, f.SymmetryMirrorColorB);
            _btnColorGlide.BackColor = FloatToColor(f.SymmetryGlideColorR, f.SymmetryGlideColorG, f.SymmetryGlideColorB);
            _btnColorCenter.BackColor = FloatToColor(f.SymmetryCenterColorR, f.SymmetryCenterColorG, f.SymmetryCenterColorB);

            // Sliders (clamp!)
            _trkAxisThickness.Value = Math.Clamp(
                (int)Math.Round(f.SymmetryAxisThickness * 1000f),
                _trkAxisThickness.Minimum, _trkAxisThickness.Maximum);

            _trkMarkerSize.Value = Math.Clamp(
                (int)Math.Round(f.SymmetryMarkerSize * 100f),
                _trkMarkerSize.Minimum, _trkMarkerSize.Maximum);

            _trkMirrorAlpha.Value = Math.Clamp(
                (int)Math.Round(f.SymmetryMirrorAlpha * 100f),
                _trkMirrorAlpha.Minimum, _trkMirrorAlpha.Maximum);

            _trkGlideAlpha.Value = Math.Clamp(
                (int)Math.Round(f.SymmetryGlideAlpha * 100f),
                _trkGlideAlpha.Minimum, _trkGlideAlpha.Maximum);

            _trkCenterRadius.Value = Math.Clamp(
                (int)Math.Round(f.SymmetryCenterRadius * 100f),
                _trkCenterRadius.Minimum, _trkCenterRadius.Maximum);

            UpdateSliderLabels();
            _isUpdating = false;
        }

        private void UpdateSliderLabels()
        {
            _lblAxisThicknessVal.Text = (_trkAxisThickness.Value / 1000f).ToString("F3");
            _lblMarkerSizeVal.Text = (_trkMarkerSize.Value / 100f).ToString("F2");
            _lblMirrorAlphaVal.Text = _trkMirrorAlpha.Value + "%";
            _lblGlideAlphaVal.Text = _trkGlideAlpha.Value + "%";
            _lblCenterRadiusVal.Text = (_trkCenterRadius.Value / 100f).ToString("F2");
        }

        private static Color FloatToColor(float r, float g, float b)
        {
            return Color.FromArgb(
                Math.Clamp((int)(r * 255), 0, 255),
                Math.Clamp((int)(g * 255), 0, 255),
                Math.Clamp((int)(b * 255), 0, 255));
        }

        private static void ColorToFloat(Color c, out float r, out float g, out float b)
        {
            r = c.R / 255f;
            g = c.G / 255f;
            b = c.B / 255f;
        }

        // ═══════════════════════════════════════════════════════════════
        //  LIVE REFRESH — this sam schemat co AtomColorWindow
        // ═══════════════════════════════════════════════════════════════

        private static void RefreshView()
        {
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

            var mainForm = MainForm.Instance;
            if (mainForm == null) return;

            mainForm.InvalidateSymmetryRenderers();
            mainForm.RefreshAllDocuments();
        }

        // ═══════════════════════════════════════════════════════════════
        //  UI → FLAGS
        // ═══════════════════════════════════════════════════════════════

        private void OnSettingChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;
            var f = Flags.Instance;

            f.ShowSymmetryElements = _chkShowSymmetryElements.Checked;

            f.ShowRotationAxis2 = _chkShowRotation2.Checked;
            f.ShowRotationAxis3 = _chkShowRotation3.Checked;
            f.ShowRotationAxis4 = _chkShowRotation4.Checked;
            f.ShowRotationAxis6 = _chkShowRotation6.Checked;
            f.ShowRotationAxes = f.ShowRotationAxis2 || f.ShowRotationAxis3
                              || f.ShowRotationAxis4 || f.ShowRotationAxis6;

            f.ShowScrew21 = _chkShowScrew21.Checked;
            f.ShowScrew31 = _chkShowScrew31.Checked;
            f.ShowScrew32 = _chkShowScrew32.Checked;
            f.ShowScrew41 = _chkShowScrew41.Checked;
            f.ShowScrew42 = _chkShowScrew42.Checked;
            f.ShowScrew43 = _chkShowScrew43.Checked;
            f.ShowScrew61 = _chkShowScrew61.Checked;
            f.ShowScrew62 = _chkShowScrew62.Checked;
            f.ShowScrew63 = _chkShowScrew63.Checked;
            f.ShowScrew64 = _chkShowScrew64.Checked;
            f.ShowScrew65 = _chkShowScrew65.Checked;
            f.ShowScrewAxes = f.ShowScrew21 || f.ShowScrew31 || f.ShowScrew32
                           || f.ShowScrew41 || f.ShowScrew42 || f.ShowScrew43
                           || f.ShowScrew61 || f.ShowScrew62 || f.ShowScrew63
                           || f.ShowScrew64 || f.ShowScrew65;

            f.ShowRotoinv3 = _chkShowRotoinv3.Checked;
            f.ShowRotoinv4 = _chkShowRotoinv4.Checked;
            f.ShowRotoinv6 = _chkShowRotoinv6.Checked;
            f.ShowRotoinversionAxes = f.ShowRotoinv3 || f.ShowRotoinv4 || f.ShowRotoinv6;

            f.ShowMirrorPlanes = _chkShowMirrorPlanes.Checked;
            f.ShowGlidePlanes = _chkShowGlidePlanes.Checked;
            f.ShowInversionCenters = _chkShowInversionCenters.Checked;

            f.MarkDirty();
            RefreshView();
        }

        private void OnSliderChanged(object sender, EventArgs e)
        {
            if (_isUpdating) return;
            var f = Flags.Instance;

            f.SymmetryAxisThickness = _trkAxisThickness.Value / 1000f;
            f.SymmetryMarkerSize = _trkMarkerSize.Value / 100f;
            f.SymmetryMirrorAlpha = _trkMirrorAlpha.Value / 100f;
            f.SymmetryGlideAlpha = _trkGlideAlpha.Value / 100f;
            f.SymmetryCenterRadius = _trkCenterRadius.Value / 100f;

            UpdateSliderLabels();
            f.MarkDirty();
            RefreshView();
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            if (_isUpdating) return;
            var btn = sender as Button;
            if (btn == null) return;

            using var dlg = new ColorDialog();
            dlg.Color = btn.BackColor;
            dlg.FullOpen = true;
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            btn.BackColor = dlg.Color;
            ColorToFloat(dlg.Color, out float r, out float g, out float b);
            var f = Flags.Instance;

            switch (btn.Name)
            {
                case "_btnColorRotation2": f.SymmetryAxis2ColorR = r; f.SymmetryAxis2ColorG = g; f.SymmetryAxis2ColorB = b; break;
                case "_btnColorRotation3": f.SymmetryAxis3ColorR = r; f.SymmetryAxis3ColorG = g; f.SymmetryAxis3ColorB = b; break;
                case "_btnColorRotation4": f.SymmetryAxis4ColorR = r; f.SymmetryAxis4ColorG = g; f.SymmetryAxis4ColorB = b; break;
                case "_btnColorRotation6": f.SymmetryAxis6ColorR = r; f.SymmetryAxis6ColorG = g; f.SymmetryAxis6ColorB = b; break;

                case "_btnColorScrew21": f.SymmetryScrew21ColorR = r; f.SymmetryScrew21ColorG = g; f.SymmetryScrew21ColorB = b; break;
                case "_btnColorScrew31": f.SymmetryScrew31ColorR = r; f.SymmetryScrew31ColorG = g; f.SymmetryScrew31ColorB = b; break;
                case "_btnColorScrew32": f.SymmetryScrew32ColorR = r; f.SymmetryScrew32ColorG = g; f.SymmetryScrew32ColorB = b; break;
                case "_btnColorScrew41": f.SymmetryScrew41ColorR = r; f.SymmetryScrew41ColorG = g; f.SymmetryScrew41ColorB = b; break;
                case "_btnColorScrew42": f.SymmetryScrew42ColorR = r; f.SymmetryScrew42ColorG = g; f.SymmetryScrew42ColorB = b; break;
                case "_btnColorScrew43": f.SymmetryScrew43ColorR = r; f.SymmetryScrew43ColorG = g; f.SymmetryScrew43ColorB = b; break;
                case "_btnColorScrew61": f.SymmetryScrew61ColorR = r; f.SymmetryScrew61ColorG = g; f.SymmetryScrew61ColorB = b; break;
                case "_btnColorScrew62": f.SymmetryScrew62ColorR = r; f.SymmetryScrew62ColorG = g; f.SymmetryScrew62ColorB = b; break;
                case "_btnColorScrew63": f.SymmetryScrew63ColorR = r; f.SymmetryScrew63ColorG = g; f.SymmetryScrew63ColorB = b; break;
                case "_btnColorScrew64": f.SymmetryScrew64ColorR = r; f.SymmetryScrew64ColorG = g; f.SymmetryScrew64ColorB = b; break;
                case "_btnColorScrew65": f.SymmetryScrew65ColorR = r; f.SymmetryScrew65ColorG = g; f.SymmetryScrew65ColorB = b; break;

                case "_btnColorRotoinv3": f.SymmetryRotoinv3ColorR = r; f.SymmetryRotoinv3ColorG = g; f.SymmetryRotoinv3ColorB = b; break;
                case "_btnColorRotoinv4": f.SymmetryRotoinv4ColorR = r; f.SymmetryRotoinv4ColorG = g; f.SymmetryRotoinv4ColorB = b; break;
                case "_btnColorRotoinv6": f.SymmetryRotoinv6ColorR = r; f.SymmetryRotoinv6ColorG = g; f.SymmetryRotoinv6ColorB = b; break;

                case "_btnColorMirror": f.SymmetryMirrorColorR = r; f.SymmetryMirrorColorG = g; f.SymmetryMirrorColorB = b; break;
                case "_btnColorGlide": f.SymmetryGlideColorR = r; f.SymmetryGlideColorG = g; f.SymmetryGlideColorB = b; break;
                case "_btnColorCenter": f.SymmetryCenterColorR = r; f.SymmetryCenterColorG = g; f.SymmetryCenterColorB = b; break;
            }

            f.MarkDirty();
            RefreshView();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            RefreshView();
            MainForm.Instance?.SetStatus("Symmetry elements refreshed", "", "🔄");
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            var f = Flags.Instance;

            f.SymmetryAxisThickness = 0.020f;
            f.SymmetryMarkerSize = 0.22f;
            f.SymmetryMirrorAlpha = 0.18f;
            f.SymmetryGlideAlpha = 0.13f;
            f.SymmetryCenterRadius = 0.07f;

            f.SymmetryAxis2ColorR = 1.00f; f.SymmetryAxis2ColorG = 0.00f; f.SymmetryAxis2ColorB = 0.00f;
            f.SymmetryAxis3ColorR = 0.00f; f.SymmetryAxis3ColorG = 1.00f; f.SymmetryAxis3ColorB = 0.00f;
            f.SymmetryAxis4ColorR = 0.00f; f.SymmetryAxis4ColorG = 0.50f; f.SymmetryAxis4ColorB = 1.00f;
            f.SymmetryAxis6ColorR = 1.00f; f.SymmetryAxis6ColorG = 0.50f; f.SymmetryAxis6ColorB = 0.00f;

            f.SymmetryScrew21ColorR = 1.00f; f.SymmetryScrew21ColorG = 0.55f; f.SymmetryScrew21ColorB = 0.55f;
            f.SymmetryScrew31ColorR = 0.55f; f.SymmetryScrew31ColorG = 0.85f; f.SymmetryScrew31ColorB = 0.55f;
            f.SymmetryScrew32ColorR = 0.35f; f.SymmetryScrew32ColorG = 0.70f; f.SymmetryScrew32ColorB = 0.35f;
            f.SymmetryScrew41ColorR = 0.55f; f.SymmetryScrew41ColorG = 0.70f; f.SymmetryScrew41ColorB = 1.00f;
            f.SymmetryScrew42ColorR = 0.35f; f.SymmetryScrew42ColorG = 0.55f; f.SymmetryScrew42ColorB = 0.90f;
            f.SymmetryScrew43ColorR = 0.20f; f.SymmetryScrew43ColorG = 0.40f; f.SymmetryScrew43ColorB = 0.75f;
            f.SymmetryScrew61ColorR = 1.00f; f.SymmetryScrew61ColorG = 0.75f; f.SymmetryScrew61ColorB = 0.45f;
            f.SymmetryScrew62ColorR = 0.95f; f.SymmetryScrew62ColorG = 0.60f; f.SymmetryScrew62ColorB = 0.30f;
            f.SymmetryScrew63ColorR = 0.85f; f.SymmetryScrew63ColorG = 0.45f; f.SymmetryScrew63ColorB = 0.15f;
            f.SymmetryScrew64ColorR = 0.75f; f.SymmetryScrew64ColorG = 0.35f; f.SymmetryScrew64ColorB = 0.10f;
            f.SymmetryScrew65ColorR = 0.65f; f.SymmetryScrew65ColorG = 0.25f; f.SymmetryScrew65ColorB = 0.05f;

            f.SymmetryRotoinv3ColorR = 0.85f; f.SymmetryRotoinv3ColorG = 0.45f; f.SymmetryRotoinv3ColorB = 1.00f;
            f.SymmetryRotoinv4ColorR = 0.60f; f.SymmetryRotoinv4ColorG = 0.30f; f.SymmetryRotoinv4ColorB = 0.90f;
            f.SymmetryRotoinv6ColorR = 0.40f; f.SymmetryRotoinv6ColorG = 0.20f; f.SymmetryRotoinv6ColorB = 0.75f;

            f.SymmetryMirrorColorR = 0.50f; f.SymmetryMirrorColorG = 0.50f; f.SymmetryMirrorColorB = 1.00f;
            f.SymmetryGlideColorR = 0.60f; f.SymmetryGlideColorG = 0.80f; f.SymmetryGlideColorB = 1.00f;
            f.SymmetryCenterColorR = 1.00f; f.SymmetryCenterColorG = 0.00f; f.SymmetryCenterColorB = 0.00f;

            f.ShowRotationAxis2 = true; f.ShowRotationAxis3 = true;
            f.ShowRotationAxis4 = true; f.ShowRotationAxis6 = true;
            f.ShowRotationAxes = true;

            f.ShowScrew21 = true; f.ShowScrew31 = true; f.ShowScrew32 = true;
            f.ShowScrew41 = true; f.ShowScrew42 = true; f.ShowScrew43 = true;
            f.ShowScrew61 = true; f.ShowScrew62 = true; f.ShowScrew63 = true;
            f.ShowScrew64 = true; f.ShowScrew65 = true;
            f.ShowScrewAxes = true;

            f.ShowRotoinv3 = true; f.ShowRotoinv4 = true; f.ShowRotoinv6 = true;
            f.ShowRotoinversionAxes = true;

            f.ShowMirrorPlanes = true;
            f.ShowGlidePlanes = true;
            f.ShowInversionCenters = true;

            UpdateFromFlags();
            RefreshView();
        }
    }
}