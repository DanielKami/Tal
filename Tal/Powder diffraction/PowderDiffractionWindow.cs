// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Afterwder diffraction module: AfterwderDiffractionWindow.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.Core.PowderDiffraction;
using WeifenLuo.WinFormsUI.Docking;
using GdiBitmap = System.Drawing.Bitmap;
using GdiColor = System.Drawing.Color;
using GdiPoint = System.Drawing.Point;
using GdiRectangle = System.Drawing.Rectangle;
using GdiSize = System.Drawing.Size;

namespace Tal.Forms
{
    public partial class PowderDiffractionWindow : DockContent
    {
        #region Private Fields

        private readonly Molecule _molecule;
        private PowderDiffractionEngine _engine;
        private List<DiffractionPeak> _peaks;
        private PowderDiffractionSettings _settings;

        private GdiBitmap _plotBitmap;
        private bool _needsRedraw = true;
        private bool _isCalculating = false;

        private bool _isZooming = false;
        private GdiPoint _zoomStartPoint;
        private GdiPoint _zoomStart;
        private GdiRectangle _zoomRect;

        private bool _isUpdatingUI = false;

 // Kolory — used w Rendering.cs
        internal static readonly GdiColor PlotBackground = GdiColor.White;
        internal static readonly GdiColor GridColor = GdiColor.FromArgb(230, 230, 235);
        internal static readonly GdiColor AxisColor = GdiColor.FromArgb(60, 60, 70);
        internal static readonly GdiColor PatternColor = GdiColor.FromArgb(0, 120, 212);
        internal static readonly GdiColor ZoomRectColor = GdiColor.FromArgb(80, 0, 120, 255);
        internal static readonly GdiColor DScaleColor = GdiColor.FromArgb(0, 120, 212);

        #endregion

        #region Constructor

        public PowderDiffractionWindow(Molecule molecule)
        {
            InitializeComponent();

            _molecule = molecule ?? throw new ArgumentNullException(nameof(molecule));
            _settings = new PowderDiffractionSettings();
            _peaks = new List<DiffractionPeak>();
            _engine = new PowderDiffractionEngine(molecule);

 // Afterbierz name pliku z molecule
            string fileName = !string.IsNullOrEmpty(_molecule.Name)
                ? Path.GetFileNameWithoutExtension(_molecule.Name)
                : "Untitled";

            this.Text = $"Diffraction Pattern - {fileName}";

 // Set default values
            chkShowDScale.Checked = false;
            chkShowBackground.Checked = true;
            numericResolution.Value = 2;

            WireEvents();
            LoadSettingsToUI();
            CalculatePattern();
        }

        #endregion

        #region Event Wiring

        private void WireEvents()
        {
 // Kontrolki
            numericMinTheta.ValueChanged += (s, e) =>
            {
                _settings.MinTwoTheta = (double)numericMinTheta.Value;
                CalculatePattern();
            };

            numericMaxTheta.ValueChanged += (s, e) =>
            {
                _settings.MaxTwoTheta = (double)numericMaxTheta.Value;
                CalculatePattern();
            };

            numericFWHM.ValueChanged += (s, e) =>
            {
                _settings.FWHM = (double)numericFWHM.Value;
                if (_peaks.Count > 0) { _needsRedraw = true; panelPlot.Invalidate(); }
            };

            numericResolution.ValueChanged += (s, e) =>
            {
                _settings.ProfileResolution = (int)numericResolution.Value;
                if (_peaks.Count > 0) { _needsRedraw = true; panelPlot.Invalidate(); }
            };

            comboProfile.SelectedIndexChanged += (s, e) =>
            {
                _settings.ProfileType = (PeakBroadening.ProfileType)comboProfile.SelectedIndex;
                if (_peaks.Count > 0) { _needsRedraw = true; panelPlot.Invalidate(); }
            };

            comboSource.SelectedIndexChanged += (s, e) =>
            {
                _settings.Source = (XRaySource)comboSource.SelectedIndex;
                _settings.Wavelength = XRaySourceHelper.GetWavelength(_settings.Source); // no-op setter, kept for clarity of intent
                CalculatePattern();
            };

            chkShowDScale.CheckedChanged += (s, e) =>
            {
                _settings.XAxisMode = chkShowDScale.Checked ? XAxisMode.DSpacing : XAxisMode.TwoTheta;
                _needsRedraw = true;
                panelPlot.Invalidate();
            };

            chkShowBackground.CheckedChanged += (s, e) =>
            {
                _settings.ShowBackground = chkShowBackground.Checked;
                _needsRedraw = true;
                panelPlot.Invalidate();
            };

            chkLPCorrection.CheckedChanged += (s, e) =>
            {
                _settings.UseLPCorrection = chkLPCorrection.Checked;
                CalculatePattern();
            };

 // Mysz
            panelPlot.MouseDown += PanelPlot_MouseDown;
            panelPlot.MouseMove += PanelPlot_MouseMove;
            panelPlot.MouseUp += PanelPlot_MouseUp;
            panelPlot.MouseWheel += PanelPlot_MouseWheel;
            panelPlot.DoubleClick += PanelPlot_DoubleClick;
            panelPlot.MouseEnter += (s, e) => panelPlot.Focus();

 // Przyciski
            btnExport.Click += BtnExport_Click;
            btnCopy.Click += BtnCopy_Click;
        }

        #endregion

        #region UI ↔ Settings

        private void LoadSettingsToUI()
        {
            numericMinTheta.Value = (decimal)_settings.MinTwoTheta;
            numericMaxTheta.Value = (decimal)_settings.MaxTwoTheta;
            numericFWHM.Value = (decimal)_settings.FWHM;
            numericDebyeWaller.Value = (decimal)_settings.DebyeWallerB;
            numericResolution.Value = _settings.ProfileResolution;
            trackDebyeWaller.Value = (int)(_settings.DebyeWallerB * 10);
            comboProfile.SelectedIndex = (int)_settings.ProfileType;
            comboSource.SelectedIndex = (int)_settings.Source;
            chkShowDScale.Checked = _settings.XAxisMode == XAxisMode.DSpacing;
            chkShowBackground.Checked = _settings.ShowBackground;
            chkLPCorrection.Checked = _settings.UseLPCorrection;
        }

        private void SaveUIToSettings()
        {
            _settings.MinTwoTheta = (double)numericMinTheta.Value;
            _settings.MaxTwoTheta = (double)numericMaxTheta.Value;
            _settings.FWHM = (double)numericFWHM.Value;
            _settings.DebyeWallerB = (double)numericDebyeWaller.Value;
            _settings.ProfileResolution = (int)numericResolution.Value;
            _settings.ProfileType = (PeakBroadening.ProfileType)comboProfile.SelectedIndex;
            _settings.Source = (XRaySource)comboSource.SelectedIndex;
            _settings.Wavelength = XRaySourceHelper.GetWavelength(_settings.Source);
            _settings.XAxisMode = chkShowDScale.Checked ? XAxisMode.DSpacing : XAxisMode.TwoTheta;
            _settings.ShowBackground = chkShowBackground.Checked;
            _settings.UseLPCorrection = chkLPCorrection.Checked;
        }

        #endregion

        #region Event Handlers — Controls

        private void NumericDebyeWaller_ValueChanged(object sender, EventArgs e)
        {
            _settings.DebyeWallerB = (double)numericDebyeWaller.Value;
            trackDebyeWaller.Value = (int)(_settings.DebyeWallerB * 10);
            CalculatePattern();
        }

        private void TrackDebyeWaller_Scroll(object sender, EventArgs e)
        {
            numericDebyeWaller.Value = trackDebyeWaller.Value / 10m;
        }

        private void PowderDiffractionWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            _plotBitmap?.Dispose();
        }

        private void PanelPlot_Resize(object sender, EventArgs e)
        {
            _needsRedraw = true;
            panelPlot.Invalidate();
        }

        private void BtnExport_Click(object sender, EventArgs e) => ExportData();
        private void BtnCopy_Click(object sender, EventArgs e) => CopyPlotImageToClipboard();

        #endregion

        #region Mouse Events

        private void PanelPlot_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var rect = GetPlotRectangle();
                if (e.X >= rect.Left && e.X <= rect.Right && e.Y >= rect.Top && e.Y <= rect.Bottom)
                {
                    _isZooming = true;
                    _zoomStartPoint = e.Location;
                    _zoomStart = e.Location;
                    _zoomRect = new GdiRectangle(e.Location, new GdiSize(0, 0));
                }
            }
        }

        private void PanelPlot_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isZooming)
            {
                int x = Math.Min(e.Location.X, _zoomStart.X);
                int y = Math.Min(e.Location.Y, _zoomStart.Y);
                int width = Math.Abs(e.Location.X - _zoomStart.X);
                int height = Math.Abs(e.Location.Y - _zoomStart.Y);
                _zoomRect = new GdiRectangle(x, y, width, height);
                panelPlot.Invalidate();
                return;
            }

            UpdateMouseInfo(e.Location);
        }

        private void PanelPlot_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isZooming)
            {
                _isZooming = false;
                int width = Math.Abs(e.Location.X - _zoomStartPoint.X);
                if (width > 10)
                {
                    ApplyZoomRange(_zoomStartPoint.X, e.Location.X);
                }
                _zoomRect = new GdiRectangle(0, 0, 0, 0);
                panelPlot.Invalidate();
            }
        }

        private void PanelPlot_MouseWheel(object sender, MouseEventArgs e)
        {
            var rect = GetPlotRectangle();
            if (e.X < rect.Left || e.X > rect.Right || e.Y < rect.Top || e.Y > rect.Bottom)
                return;

            double minTheta = _settings.MinTwoTheta;
            double maxTheta = _settings.MaxTwoTheta;
            double currentSpan = maxTheta - minTheta;

            double factor = e.Delta > 0 ? 0.8 : 1.25;

            double mouseFraction = (double)(e.X - rect.Left) / rect.Width;
            double mouseTheta = minTheta + mouseFraction * currentSpan;

            double newSpan = currentSpan * factor;
            double newMin = mouseTheta - mouseFraction * newSpan;
            double newMax = newMin + newSpan;

            _settings.MinTwoTheta = Math.Max(0, newMin);
            _settings.MaxTwoTheta = Math.Min(180, newMax);

            _isUpdatingUI = true;
            try
            {
                numericMinTheta.Value = (decimal)_settings.MinTwoTheta;
                numericMaxTheta.Value = (decimal)_settings.MaxTwoTheta;
            }
            finally
            {
                _isUpdatingUI = false;
            }

            CalculatePattern();
        }

        private void PanelPlot_DoubleClick(object sender, EventArgs e)
        {
            ResetZoom();
        }

        private void PanelPlot_MouseLeave(object sender, EventArgs e)
        {
            labelHoverInfo.Text = "2θ: -";
            labelHoverDetail.Text = "d: - | hkl: -";
        }

        #endregion

        #region Zoom Logic

        private void ApplyZoomRange(int pixelX1, int pixelX2)
        {
            var plotRect = GetPlotRectangle();
            if (plotRect.Width <= 0) return;

            int leftPx = Math.Min(pixelX1, pixelX2);
            int rightPx = Math.Max(pixelX1, pixelX2);

            double minTheta = _settings.MinTwoTheta;
            double maxTheta = _settings.MaxTwoTheta;

            double fractionLeft = (double)(leftPx - plotRect.Left) / plotRect.Width;
            double fractionRight = (double)(rightPx - plotRect.Left) / plotRect.Width;

            double newMinTheta = minTheta + fractionLeft * (maxTheta - minTheta);
            double newMaxTheta = minTheta + fractionRight * (maxTheta - minTheta);

            if (newMinTheta > newMaxTheta)
            {
                (newMinTheta, newMaxTheta) = (newMaxTheta, newMinTheta);
            }

            _settings.MinTwoTheta = Math.Max(0, newMinTheta);
            _settings.MaxTwoTheta = Math.Min(180, newMaxTheta);

            _isUpdatingUI = true;
            try
            {
                numericMinTheta.Value = (decimal)_settings.MinTwoTheta;
                numericMaxTheta.Value = (decimal)_settings.MaxTwoTheta;
            }
            finally
            {
                _isUpdatingUI = false;
            }

            CalculatePattern();
        }

        private void ResetZoom()
        {
            _settings.MinTwoTheta = 0.0;
            _settings.MaxTwoTheta = 90.0;
            numericMinTheta.Value = 0m;
            numericMaxTheta.Value = 90m;
            CalculatePattern();
        }

        #endregion

        #region Calculate Pattern

        private void CalculatePattern()
        {
            if (_isCalculating || _isUpdatingUI) return;
            if (_molecule == null || _molecule.AsymmetricPart.Count == 0)
            {
                labelStatus.Text = "No atoms in structure";
                labelStatus.ForeColor = GdiColor.Red;
                return;
            }

            try
            {
                _isCalculating = true;
                SaveUIToSettings();

                labelStatus.Text = "Calculating...";
                labelStatus.ForeColor = GdiColor.Orange;
                this.Cursor = Cursors.WaitCursor;
                Application.DoEvents();

                _peaks = _engine.CalculatePeaks(
                    _settings.MinTwoTheta,
                    _settings.MaxTwoTheta,
                    _settings.Wavelength,
                    _settings.DebyeWallerB,
                    _settings.UseLPCorrection
                );

                _engine.ApplyCorrections(_peaks);
                _peaks = _peaks.OrderBy(p => p.TwoTheta).ToList();

                labelPeakCount.Text = $"Peaks: {_peaks.Count}";
                labelStatus.Text = _peaks.Count > 0 ?
                    $"Calculated {_peaks.Count} peaks" :
                    "No peaks in range";
                labelStatus.ForeColor = _peaks.Count > 0 ? GdiColor.Green : GdiColor.Orange;

                _needsRedraw = true;
                panelPlot.Invalidate();
            }
            catch (Exception ex)
            {
                labelStatus.Text = $"Error: {ex.Message}";
                labelStatus.ForeColor = GdiColor.Red;
                System.Diagnostics.Debug.WriteLine($"[PowderDiffraction] Error: {ex.Message}");
            }
            finally
            {
                _isCalculating = false;
                this.Cursor = Cursors.Default;
            }
        }

        #endregion
    }
}