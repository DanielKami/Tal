// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Depth-cue (fog) settings window.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    /// <summary>
    /// Window with sliders for Depth Cue (depth fog) adjustment.
    /// Otwierane prawym klikiem na checkbox "Depth cue" w DisplayOptionsWindow.
    /// </summary>
    public partial class DepthCueWindow : DockContent
    {
        #region Fields

        // ⭐ Guards against recursion when controls are changed programmatically
        private bool _suppressEvents = false;

        // ⭐ Default values — used by Reset
        private const float DEFAULT_NEAR = 5f;
        private const float DEFAULT_FAR = 30f;
        private const float DEFAULT_STRENGTH = 0.6f;

        #endregion

        #region Constructor

        public DepthCueWindow()
        {
            InitializeComponent();
            DockWindowConfig.SetupWindow(this, "🌫️ Depth Cue");

            // ⭐ Event wiring
            _chkEnabled.CheckedChanged += ChkEnabled_CheckedChanged;
            _trkNear.ValueChanged += TrkNear_ValueChanged;
            _trkFar.ValueChanged += TrkFar_ValueChanged;
            _trkStrength.ValueChanged += TrkStrength_ValueChanged;
            _btnReset.Click += BtnReset_Click;

            // ⭐ Load current stan z Flags
            LoadFromFlags();
        }

        #endregion

        #region Load from Flags

        public void LoadFromFlags()
        {
            _suppressEvents = true;
            try
            {
                var f = Flags.Instance;

                _chkEnabled.Checked = f.DepthCue;

                _trkNear.Value = (int)Math.Clamp(
                    Math.Round(f.DepthCueNear * 10f),
                    _trkNear.Minimum, _trkNear.Maximum);

                _trkFar.Value = (int)Math.Clamp(
                    Math.Round(f.DepthCueFar * 10f),
                    _trkFar.Minimum, _trkFar.Maximum);

                _trkStrength.Value = (int)Math.Clamp(
                    Math.Round(f.DepthCueStrength * 100f),
                    _trkStrength.Minimum, _trkStrength.Maximum);

                UpdateLabels();
                UpdateEnabledState();
            }
            finally
            {
                _suppressEvents = false;
            }
        }

        public void UpdateFromFlags() => LoadFromFlags();

        #endregion

        #region Event Handlers

        private void ChkEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            ApplyToFlags();
        }

        private void TrkNear_ValueChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;

            // ⭐ Validation: Near must be < Far
            if (_trkNear.Value >= _trkFar.Value)
            {
                _suppressEvents = true;
                _trkFar.Value = Math.Min(_trkNear.Value + 10, _trkFar.Maximum);
                _suppressEvents = false;
            }

            ApplyToFlags();
        }

        private void TrkFar_ValueChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;

            // ⭐ Validation: Far must be > Near
            if (_trkFar.Value <= _trkNear.Value)
            {
                _suppressEvents = true;
                _trkNear.Value = Math.Max(_trkFar.Value - 10, _trkNear.Minimum);
                _suppressEvents = false;
            }

            ApplyToFlags();
        }

        private void TrkStrength_ValueChanged(object sender, EventArgs e)
        {
            if (_suppressEvents) return;
            ApplyToFlags();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            _suppressEvents = true;
            try
            {
                _trkNear.Value = (int)(DEFAULT_NEAR * 10f);
                _trkFar.Value = (int)(DEFAULT_FAR * 10f);
                _trkStrength.Value = (int)(DEFAULT_STRENGTH * 100f);
            }
            finally
            {
                _suppressEvents = false;
            }

            ApplyToFlags();
        }

        #endregion

        #region Apply / Update

        private void ApplyToFlags()
        {
            var f = Flags.Instance;

            f.DepthCue = _chkEnabled.Checked;
            f.DepthCueNear = _trkNear.Value / 10f;
            f.DepthCueFar = _trkFar.Value / 10f;
            f.DepthCueStrength = _trkStrength.Value / 100f;

            f.MarkDirty();
         
            UpdateLabels();
            UpdateEnabledState();

            // ⭐ Live preview we all open documents 3D
            var mainForm = MainForm.Instance;
            if (mainForm != null)
            {
                mainForm.RefreshAllDocuments();
                mainForm.RefreshMorphologyDocuments();

                // ⭐ Sync checkbox w DisplayOptionsWindow
                mainForm.GetDisplayOptionsWindow()?.SyncDepthCueCheckbox();
            }
        }

        private void UpdateLabels()
        {
            _lblNearVal.Text = $"{_trkNear.Value / 10f:F1} Å";
            _lblFarVal.Text = $"{_trkFar.Value / 10f:F1} Å";
            _lblStrengthVal.Text = $"{_trkStrength.Value}%";
        }

        private void UpdateEnabledState()
        {
            bool on = _chkEnabled.Checked;

            _trkNear.Enabled = on;
            _trkFar.Enabled = on;
            _trkStrength.Enabled = on;

            _lblNear.ForeColor = on ? System.Drawing.Color.FromArgb(51, 51, 51)
                                    : System.Drawing.Color.FromArgb(160, 160, 160);
            _lblFar.ForeColor = _lblNear.ForeColor;
            _lblStrength.ForeColor = _lblNear.ForeColor;
            _lblNearVal.ForeColor = _lblNear.ForeColor;
            _lblFarVal.ForeColor = _lblNear.ForeColor;
            _lblStrengthVal.ForeColor = _lblNear.ForeColor;
        }

        #endregion
    }
}