// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Auto-rotate settings window.

﻿using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class AutoRotateWindow : DockContent
    {
        private System.Windows.Forms.Timer _rotationTimer;
        private float _wobbleTime = 0f;
        private float _lastWobbleAngle = 0f;
        private System.Diagnostics.Stopwatch _rotationClock = System.Diagnostics.Stopwatch.StartNew();
        private long _lastTickMs = 0;
        public AutoRotateWindow()
        {
            InitializeComponent();
            InitializeCustomComponents();
            UpdateControls();

            DockWindowConfig.SetupWindow(this, "🔄 Auto Rotate");

            _rotationTimer = new System.Windows.Forms.Timer();
            _rotationTimer.Interval = 16;
            _rotationTimer.Tick += RotationTimer_Tick;
            _rotationTimer.Start();
        }

        private void InitializeCustomComponents()
        {


            // Podpinamy zdarzenia
            _radioRotate.CheckedChanged += RadioMode_CheckedChanged;
            _radioWobble.CheckedChanged += RadioMode_CheckedChanged;
            _trackSpeed.Scroll += TrackSpeed_Scroll;
            _trackAmplitude.Scroll += TrackAmplitude_Scroll;
            _radioX.CheckedChanged += RadioAxis_CheckedChanged;
            _radioY.CheckedChanged += RadioAxis_CheckedChanged;
            _radioZ.CheckedChanged += RadioAxis_CheckedChanged;
            _btnReset.Click += BtnReset_Click;

            // Default Rotate X
            _radioRotate.Checked = true;
            _radioX.Checked = true;
        }

        // ⭐============================================================
        // ⭐ ZDARZENIA KONTROLEK
        // ⭐============================================================

        private void RadioMode_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControls();
            _wobbleTime = 0f;
            _lastWobbleAngle = 0f;
        }

        private void RadioAxis_CheckedChanged(object sender, EventArgs e)
        {
            _wobbleTime = 0f;
            _lastWobbleAngle = 0f;
        }

        private void TrackSpeed_Scroll(object sender, EventArgs e)
        {
            _labelSpeedValue.Text = _trackSpeed.Value.ToString("F1");
        }

        private void TrackAmplitude_Scroll(object sender, EventArgs e)
        {
            _labelAmplitudeValue.Text = _trackAmplitude.Value.ToString("F0") + "°";
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            _trackSpeed.Value = 0;
            _trackAmplitude.Value = 0;
            _wobbleTime = 0f;
            _lastWobbleAngle = 0f;
            _radioRotate.Checked = true;
            _radioX.Checked = true;
            _labelSpeedValue.Text = "0.0";
            _labelAmplitudeValue.Text = "0°";
            UpdateControls();
        }

        // ⭐============================================================
        // ⭐ TIMER - AUTO ROTATE
        // ⭐============================================================

        private void RotationTimer_Tick(object sender, EventArgs e)
        {
            if (!this.Visible) return;

            long nowMs = _rotationClock.ElapsedMilliseconds;
            long deltaMs = nowMs - _lastTickMs;
            _lastTickMs = nowMs;

            if (deltaMs <= 0) deltaMs = 16;   // zabezpieczenie przy pierwszym ticku

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel == null) return;

            float speed = _trackSpeed.Value;
            float deltaAngle = 0f;

            if (_radioRotate.Checked)
            {
                if (Math.Abs(speed) < 0.0001f) return;

                // ⭐ deltaAngle proportional to real time (not ticks)
                //    0.006f * 16.67 = 0.1 per frame at 60 FPS — that was the previous speed.
                //    Teraz: 0.1 * deltaMs / 16.67
                deltaAngle = speed * 0.003f * (deltaMs / 16.67f);
            }
            else if (_radioWobble.Checked)
            {
                float amplitude = GetAmplitude();
                if (amplitude < 0.001f || Math.Abs(speed) < 0.0001f) return;

                // ⭐ Wobble also depends on real time
                float wobbleSpeed = speed * 0.12f;

                // _wobbleTime was previously += wobbleSpeed * 0.05f * (wobble every frame)
                // Teraz: += wobbleSpeed * 0.05f * (deltaMs / 16.67f)
                _wobbleTime += wobbleSpeed * 0.05f * (deltaMs / 16.67f);

                float currentAngle = (float)Math.Cos(_wobbleTime) * amplitude;
                deltaAngle = currentAngle - _lastWobbleAngle;
                _lastWobbleAngle = currentAngle;
            }

            // ⭐ APPLY ROTATION
            if (Math.Abs(deltaAngle) > 0.000001f)
            {
                foreach (var doc in dockPanel.Documents)
                {
                    if (doc is MonoGameDocument monoDoc)
                    {
                        var camera = monoDoc.GetCamera();
                        if (camera == null) continue;

                        if (_radioX.Checked)
                            camera.RotateX(deltaAngle);
                        else if (_radioY.Checked)
                            camera.RotateY(deltaAngle);
                        else if (_radioZ.Checked)
                            camera.RotateZ(deltaAngle);

                        camera.ApplyEffects();
                        monoDoc.RefreshView();
                    }
                }
            }
        }

        // ⭐============================================================
        // ⭐ METODY POMOCNICZE
        // ⭐============================================================

        private void UpdateControls()
        {
            bool isRotate = _radioRotate.Checked;
            bool isWobble = _radioWobble.Checked;

            _trackSpeed.Enabled = true;

            _trackAmplitude.Enabled = isWobble;
            _lblAmplitude.Enabled = isWobble;
            _labelAmplitudeValue.Enabled = isWobble;
            _lblAmplitudeDeg.Enabled = isWobble;

            _radioX.Enabled = true;
            _radioY.Enabled = true;
            _radioZ.Enabled = true;

            _labelRotateInfo.Text = isRotate ? "▶ Continuous rotation (constant speed)" : "";
            _labelWobbleInfo.Text = isWobble ? "〰️ Oscillating rotation (cosine, 20x faster)" : "";
        }

        private float GetAmplitude()
        {
            // ⭐ AMPLITUDA od 0 do 90 stopni (konwersja na radiany)
            return _trackAmplitude.Value * (float)(Math.PI / 180.0);
        }

        // ⭐============================================================
        // ⭐ ZWOLNIJ ZASOBY PRZY ZAMYKANIU
        // ⭐============================================================

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_rotationTimer != null)
                {
                    _rotationTimer.Stop();
                    _rotationTimer.Dispose();
                    _rotationTimer = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}