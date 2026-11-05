// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Scene quality (SSAA / MSAA / AA) settings window.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    /// <summary>
    /// Window with 3D scene image quality settings:
    ///   - SSAA (Supersampling) 1× / 2× / 4× — resolution multiplier of the 3D scene RT
    ///   - MSAA 3D scene (Off/2/4/8) — samples per pixel in the 3D scene RT
    ///   - Anizotropia tekstur
    ///
    /// ⭐ v3: SSAA ograniczone do {1, 2, 4} — radio buttons zamiast suwaka.
    ///        Value 3× removed (odd; no ideal mipmap/box
    ///        filtering; downsample produced visible aliasing).
    /// </summary>
    public partial class SceneQualityWindow : DockContent
    {
        #region Fields

        private bool _suppressEvents = false;

        // Default values — used by Reset
        private const int DEFAULT_SSAA = 2;
        private const int DEFAULT_MSAA = 4;
        private const int DEFAULT_ANISO_INDEX = 2;   // 4×

        // ⭐ Allowed SSAA values — consistent with Flags and GameWindow
        private static readonly int[] AllowedSSAA = { 1, 2, 4 };

        // Aniso Combo mapping → value
        private static readonly int[] AnisoMap = { 1, 2, 4, 8, 16 };

        #endregion

        #region Constructor

        public SceneQualityWindow()
        {
            InitializeComponent();
            DockWindowConfig.SetupWindow(this, "🎚️ Scene Quality");

            // ⭐ SSAA — radio buttons
            _rbSSAA1.CheckedChanged += RbSSAA_CheckedChanged;
            _rbSSAA2.CheckedChanged += RbSSAA_CheckedChanged;
            _rbSSAA4.CheckedChanged += RbSSAA_CheckedChanged;

            // ⭐ MSAA — radio buttons
            _rbMSAAOff.CheckedChanged += RbMSAA_CheckedChanged;
            _rbMSAA2.CheckedChanged += RbMSAA_CheckedChanged;
            _rbMSAA4.CheckedChanged += RbMSAA_CheckedChanged;
            _rbMSAA8.CheckedChanged += RbMSAA_CheckedChanged;

            _cmbAniso.SelectedIndexChanged += CmbAniso_SelectedIndexChanged;
            _btnReset.Click += BtnReset_Click;

            LoadFromFlags();
        }

        #endregion

        #region Load / Apply

        public void LoadFromFlags()
        {
            _suppressEvents = true;
            try
            {
                var f = Flags.Instance;

                // ⭐ SSAA — walidacja do {1, 2, 4}
                int ssaa = NormalizeSSAA(f.SSAASamples);
                _rbSSAA1.Checked = ssaa == 1;
                _rbSSAA2.Checked = ssaa == 2;
                _rbSSAA4.Checked = ssaa == 4;

                // ⭐ MSAA scene 3D
                int msaa = Math.Clamp(f.MSAAOnSceneRT, 0, 8);
                if (msaa != 0 && msaa != 2 && msaa != 4 && msaa != 8)
                    msaa = 4;

                _rbMSAAOff.Checked = msaa == 0;
                _rbMSAA2.Checked = msaa == 2;
                _rbMSAA4.Checked = msaa == 4;
                _rbMSAA8.Checked = msaa == 8;

                // Anizo
                int anisoIdx = Array.IndexOf(AnisoMap, f.AnisotropyLevel);
                _cmbAniso.SelectedIndex = anisoIdx >= 0 ? anisoIdx : DEFAULT_ANISO_INDEX;

                UpdateInfoLabel();
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        public void UpdateFromFlags() => LoadFromFlags();

        private void ApplyToFlags()
        {
            var f = Flags.Instance;

            // ⭐ SSAA — from radio buttons, with validation
            int ssaa = _rbSSAA1.Checked ? 1 : (_rbSSAA2.Checked ? 2 : 4);
            f.SSAASamples = NormalizeSSAA(ssaa);

            // ⭐ MSAA scene 3D
            if (_rbMSAAOff.Checked) f.MSAAOnSceneRT = 0;
            else if (_rbMSAA2.Checked) f.MSAAOnSceneRT = 2;
            else if (_rbMSAA4.Checked) f.MSAAOnSceneRT = 4;
            else if (_rbMSAA8.Checked) f.MSAAOnSceneRT = 8;

            f.AnisotropyLevel = AnisoMap[Math.Clamp(_cmbAniso.SelectedIndex, 0, 4)];

            f.MarkDirty();

            UpdateInfoLabel();

            // Refresh all 3D documents
            var mainForm = MainForm.Instance;
            if (mainForm != null)
            {
                mainForm.RefreshAllDocuments();
                mainForm.RefreshMorphologyDocuments();
            }
        }

        #endregion

        #region Event Handlers

        private void RbSSAA_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            ApplyToFlags();
        }

        private void RbMSAA_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            ApplyToFlags();
        }

        private void CmbAniso_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            ApplyToFlags();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            _suppressEvents = true;
            try
            {
                // SSAA
                _rbSSAA1.Checked = DEFAULT_SSAA == 1;
                _rbSSAA2.Checked = DEFAULT_SSAA == 2;
                _rbSSAA4.Checked = DEFAULT_SSAA == 4;

                // MSAA
                _rbMSAAOff.Checked = DEFAULT_MSAA == 0;
                _rbMSAA2.Checked = DEFAULT_MSAA == 2;
                _rbMSAA4.Checked = DEFAULT_MSAA == 4;
                _rbMSAA8.Checked = DEFAULT_MSAA == 8;

                _cmbAniso.SelectedIndex = DEFAULT_ANISO_INDEX;
            }
            finally
            {
                _suppressEvents = false;
            }
            ApplyToFlags();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// ⭐ Normalizuje SSAA do dozwolonych values {1, 2, 4}.
        /// Anything else → nearest allowed value.
        /// </summary>
        private static int NormalizeSSAA(int value)
        {
            if (value <= 1) return 1;
            if (value <= 2) return 2;
            return 4;
        }

        private void UpdateInfoLabel()
        {
            var f = Flags.Instance;

            int ssaa = NormalizeSSAA(f.SSAASamples);
            int msaa = Math.Clamp(f.MSAAOnSceneRT, 0, 8);
            if (msaa != 0 && msaa != 2 && msaa != 4 && msaa != 8)
                msaa = 4;

            // Rozmiar windows — bierzemy z active document
            int w = 1920, h = 1080;
            var doc = MainForm.Instance?.GetActiveDocument();
            var gd = doc?.GetGraphicsDevice();
            if (gd != null)
            {
                w = gd.PresentationParameters.BackBufferWidth;
                h = gd.PresentationParameters.BackBufferHeight;
            }

            int rtW = w * ssaa;
            int rtH = h * ssaa;

            double mpix = (rtW * (double)rtH) / 1_000_000.0;

            // ⭐ VRAM: colour (4 B/px) + depth (4 B/px), times MSAA (if > 0)
            int samples = msaa > 0 ? msaa : 1;
            double vramMb = (rtW * (double)rtH * 4 * 2 * samples) / (1024.0 * 1024.0);

            // ⭐ Effective samples per screen pixel:
            //    SSAA × SSAA (because the RT is N× larger in each dimension)
            //    × MSAA (if > 0)
            int effectiveSamples = ssaa * ssaa * (msaa > 0 ? msaa : 1);

            string ssaaText = ssaa <= 1 ? "Off (1×)" : $"{ssaa}×";
            string msaaText = msaa == 0 ? "Off" : $"{msaa}×";
            string downsampleMethod = ssaa switch
            {
                1 => "PointClamp (kopiuj 1:1)",
                2 => "LinearClamp (idealne 2×2)",
                4 => "Box 4×→2× + LinearClamp 2×→1×",
                _ => "—"
            };

            _lblInfo.Text =
                $"Window:                {w}×{h}\n" +
                $"3D RT (SSAA):          {rtW}×{rtH}  ({mpix:F1} Mpix)\n" +
                $"SSAA:                  {ssaaText}\n" +
                $"MSAA sceny 3D:         {msaaText}\n" +
                $"Downsample:            {downsampleMethod}\n" +
                $"VRAM (RT color+depth): ~{vramMb:F0} MB\n" +
                $"Effective samples:    {ssaa}² × MSAA{(msaa > 0 ? msaa.ToString() : "1")} = {effectiveSamples} / pixel\n" +
                $"\n" +
                $"MSAA works while moving and at rest.\n" +
                $"CPK impostors use only SSAA (shader discard).";
        }

        #endregion
    }
}