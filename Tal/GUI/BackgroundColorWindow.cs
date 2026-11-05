// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Background colour settings window.

﻿using System;
using System.Drawing;
using System.Windows.Forms;
using Tal.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.GUI
{
    public partial class BackgroundColorWindow : DockContent
    {
        private Color _color1;
        private Color _color2;
        private bool _isUpdating = false;

        public Color Color1 => _color1;
        public Color Color2 => _color2;

        public BackgroundColorWindow()
        {
            // ⭐ GUARD FOR THE ENTIRE CONSTRUCTOR
            _isUpdating = true;

            InitializeComponent();

            DockWindowConfig.SetupWindow(this, "🎨 Background Colors");

            // ⭐ PODPISZ ZDARZENIA
            this.btnColor1.Click += BtnColor1_Click;
            this.btnColor2.Click += BtnColor2_Click;
            this.btnReset.Click += BtnReset_Click;
            this.cmbPresets.SelectedIndexChanged += CmbPresets_SelectedIndexChanged;

            // ⭐ WIRE EVENTS FOR SWATCHES
            foreach (Control ctrl in this.flpQuickColors.Controls)
            {
                if (ctrl is Panel panel)
                {
                    panel.Click += QuickColorPanel_Click;
                }
            }

            // ⭐ ORDER MATTERS:
            //    1. First fill the preset list (without SelectedIndex)
            //    2. Then load colours and select the matching preset
            UpdatePresetsCombo();
            LoadFromFlags();

            // ⭐ KONIEC ZABEZPIECZENIA
            _isUpdating = false;
        }

        // ============================================================
        // ⭐ WCZYTAJ Z FLAGS
        // ============================================================

        private void LoadFromFlags()
        {
            var flags = Flags.Instance;

            // ⭐ Pobierz XnaColor RAZ (jedna konwersja float→byte)
            var c1 = flags.color1;
            var c2 = flags.color2;

            _color1 = System.Drawing.Color.FromArgb(c1.A, c1.R, c1.G, c1.B);
            _color2 = System.Drawing.Color.FromArgb(c2.A, c2.R, c2.G, c2.B);

            System.Diagnostics.Debug.WriteLine(
                $"[BG] LoadFromFlags: color1={_color1}, color2={_color2}, " +
                $"Flags.Color1R={flags.Color1R}, Color1G={flags.Color1G}, Color1B={flags.Color1B}");
         
            UpdateUI();
        }

        // ============================================================
        // ⭐ SAVE TO FLAGS AND REFRESH
        // ============================================================

        private void ApplyColors()
        {
            System.Diagnostics.Debug.WriteLine(
                $"[BG] ApplyColors START: _color1={_color1}, _color2={_color2}");

            // ⭐ SAVE DO FLAGS
            Flags.Instance.color1 = new Microsoft.Xna.Framework.Color(
                _color1.R, _color1.G, _color1.B, _color1.A);

            Flags.Instance.color2 = new Microsoft.Xna.Framework.Color(
                _color2.R, _color2.G, _color2.B, _color2.A);

           
            Flags.Instance.Save();

            System.Diagnostics.Debug.WriteLine(
                $"[BG] ApplyColors saved: Flags.Color1R={Flags.Instance.Color1R}, " +
                $"Color1G={Flags.Instance.Color1G}, Color1B={Flags.Instance.Color1B}, " +
                $"BackgroundRevision={Flags.Instance.VisualRevision}");

            // ⭐ REFRESH ALL DOCUMENTS IMMEDIATELY
            var mainForm = MainForm.Instance;
            if (mainForm != null)
            {
                mainForm.RefreshAllDocuments();
                mainForm.RefreshMorphologyDocuments();
                mainForm.SetStatus($"Background colors updated",
                    $"Structures: {StructureStorage.Instance.StructureCount}", "🎨");
            }
            
            UpdateUI();
        }

        // ============================================================
        // ⭐ AKTUALIZACJA UI
        // ============================================================

        private void UpdateUI()
        {
            bool wasUpdating = _isUpdating;
            _isUpdating = true;

            btnColor1.BackColor = _color1;
            btnColor2.BackColor = _color2;
            previewPanel.Invalidate();

            // Aktualizuj tekst z kolorami w hex
            lblColor1Value.Text = $"#{_color1.R:X2}{_color1.G:X2}{_color1.B:X2}";
            lblColor2Value.Text = $"#{_color2.R:X2}{_color2.G:X2}{_color2.B:X2}";

            // ⭐ Select matching preset (with internal guard)
            SelectMatchingPreset();
            Flags.Instance.VisualRevision++;
            _isUpdating = wasUpdating;
        }

        private void SelectMatchingPreset()
        {
            // ⭐ Guard — setting SelectedItem fires the handler
            bool wasUpdating = _isUpdating;
            _isUpdating = true;

            var presets = GetPresets();
            foreach (var preset in presets)
            {
                if (ColorsMatch(_color1, preset.Color1) && ColorsMatch(_color2, preset.Color2))
                {
                    cmbPresets.SelectedItem = preset.Name;
                    _isUpdating = wasUpdating;
                    return;
                }
            }
            cmbPresets.SelectedItem = "Custom";
            _isUpdating = wasUpdating;
        }

        private bool ColorsMatch(Color a, Color b)
        {
            return Math.Abs(a.R - b.R) < 5 &&
                   Math.Abs(a.G - b.G) < 5 &&
                   Math.Abs(a.B - b.B) < 5;
        }

        private void UpdatePresetsCombo()
        {
            // ⭐ Guard — setting SelectedIndex/Items fires the handler
            bool wasUpdating = _isUpdating;
            _isUpdating = true;

            cmbPresets.Items.Clear();
            var presets = GetPresets();
            foreach (var preset in presets)
            {
                cmbPresets.Items.Add(preset.Name);
            }

            // ⭐ DO NOT set SelectedIndex = 0 — that would fire the handler and overwrite colours!
            // SelectedIndex is set by SelectMatchingPreset called from UpdateUI.

            _isUpdating = wasUpdating;
        }

        // ============================================================
        // ⭐ PRESETY
        // ============================================================

        private class Preset
        {
            public string Name { get; set; }
            public Color Color1 { get; set; }
            public Color Color2 { get; set; }
        }

        private Preset[] GetPresets()
        {
            return new Preset[]
            {
                new Preset { Name = "Night Sky", Color1 = Color.FromArgb(0, 0, 30), Color2 = Color.FromArgb(0, 0, 10) },
                new Preset { Name = "Ocean", Color1 = Color.FromArgb(0, 30, 60), Color2 = Color.FromArgb(0, 10, 30) },
                new Preset { Name = "Sunset", Color1 = Color.FromArgb(60, 20, 40), Color2 = Color.FromArgb(30, 10, 20) },
                new Preset { Name = "Forest", Color1 = Color.FromArgb(20, 40, 20), Color2 = Color.FromArgb(10, 20, 10) },
                new Preset { Name = "White", Color1 = Color.White, Color2 = Color.LightGray },
                new Preset { Name = "Black", Color1 = Color.Black, Color2 = Color.DarkGray },
                new Preset { Name = "Custom", Color1 = _color1, Color2 = _color2 }
            };
        }

        // ============================================================
        // ⭐ QUICK-PICK SWATCHES - CHANGE THE ENTIRE BACKGROUND IMMEDIATELY
        // ============================================================

        private void QuickColorPanel_Click(object sender, EventArgs e)
        {
            if (_isUpdating) return;

            Control ctrl = sender as Control;
            if (ctrl == null) return;

            // Find parent panel
            Panel panel = ctrl as Panel;
            if (panel == null)
            {
                panel = ctrl.Parent as Panel;
                if (panel == null) return;
            }

            // Pobierz colour z Tag
            if (panel.Tag is Color color)
            {
                // ⭐ NATYCHMIASTOWA ZMIANA - USTAWIAMY OBA COLOURS NA TEN SAM
                _color1 = color;
                _color2 = color;
                ApplyColors();
            }
        }

        // ============================================================
        // ⭐ EVENTS - TOP/BOTTOM COLOUR SELECTION
        // ============================================================

        private void BtnColor1_Click(object sender, EventArgs e)
        {
            using (var dialog = new ColorDialog())
            {
                dialog.Color = _color1;
                dialog.FullOpen = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _color1 = dialog.Color;
                    ApplyColors();
                }
            }
        }

        private void BtnColor2_Click(object sender, EventArgs e)
        {
            using (var dialog = new ColorDialog())
            {
                dialog.Color = _color2;
                dialog.FullOpen = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _color2 = dialog.Color;
                    ApplyColors();
                }
            }
        }
        private void CmbPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[BG] CmbPresets_SelectedIndexChanged: selected={cmbPresets.SelectedItem}, " +
                $"_isUpdating={_isUpdating}, Visible={Visible}");

            if (_isUpdating) return;

            // ⭐ DO NOT apply preset if the window is only opening (not yet visible)
            if (!Visible) return;

            string selected = cmbPresets.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected)) return;

            // ⭐ Skip "Custom" — no point applying it
            if (selected == "Custom") return;

            var presets = GetPresets();
            foreach (var preset in presets)
            {
                if (preset.Name == selected)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[BG] Applying preset '{selected}': color1={preset.Color1}, color2={preset.Color2}");

                    _color1 = preset.Color1;
                    _color2 = preset.Color2;
                    ApplyColors();
                    break;
                }
            }
        }
        private void BtnReset_Click(object sender, EventArgs e)
        {
            _color1 = Color.FromArgb(250, 250, 220);
            _color2 = Color.FromArgb(240, 240, 210);
            ApplyColors();
            // ⭐ DO NOT set cmbPresets.SelectedItem manually — SelectMatchingPreset (via UpdateUI in ApplyColors) does it
        }

        // ============================================================
        // ⭐ METODY PUBLICZNE
        // ============================================================

        public void UpdateFromFlags()
        {
            LoadFromFlags();
        }
    }
}