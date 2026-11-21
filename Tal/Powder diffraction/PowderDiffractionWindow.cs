// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Powder diffraction module: PowderDiffractionWindow.

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
        private List<DiffractionPeak> _peaks;
        private PowderDiffractionSettings _settings;

        private GdiBitmap _plotBitmap;
        private bool _needsRedraw = true;
        // Calculation runs on a worker thread; a newer request cancels the running one.
        private CancellationTokenSource _calcCts;
        private int _calcVersion;
        /// <summary>Range/wavelength the user already confirmed for a very large calculation.</summary>
        private string _largeCalcApprovedKey;

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

 // Pobierz name pliku z molecule
            string fileName = !string.IsNullOrEmpty(_molecule.Name)
                ? Path.GetFileNameWithoutExtension(_molecule.Name)
                : "Untitled";

            this.Text = $"Diffraction Pattern - {fileName}";

 // Set default values
            chkShowDScale.Checked = false;
            chkShowBackground.Checked = true;
            numericResolution.Value = 2;

            // This control only sets how densely the drawn curve is sampled. It is NOT the
            // instrumental resolution and does not change the calculated peaks in any way.
            const string pointsTip =
                "Number of profile points calculated per screen pixel of the plot width.\n" +
                "Higher = smoother curve, slower redraw (matters for narrow peaks).\n" +
                "It affects only how the curve is drawn: peak positions, intensities and FWHM stay the same.\n" +
                "It is not the instrumental resolution (use FWHM for peak width).";
            toolTip.SetToolTip(labelResolution, pointsTip);
            toolTip.SetToolTip(numericResolution, pointsTip);

            const string scaleTip =
                "Global multiplier of the atomic displacement parameters (Debye-Waller factor):\n" +
                "U(used) = scale × U, so 1.00 = values as given and 0 = no thermal damping.\n" +
                "Atoms without U in the file use the reference B = 0.50 Å² (U = 0.0063 Å²), also times the scale.";
            toolTip.SetToolTip(labelDebyeWaller, scaleTip);
            toolTip.SetToolTip(numericDebyeWaller, scaleTip);
            toolTip.SetToolTip(trackDebyeWaller, scaleTip);

            const string atomicUTip =
                "Take U (Uiso, or Ueq for anisotropic atoms) of each atom from the structure file.\n" +
                "Factor exp(-8π²·scale·U·s²). Atoms without U use the reference B scaled the same way.\n" +
                "Off = every atom uses the reference B = 0.50 Å², times the scale.";
            toolTip.SetToolTip(chkUseAtomicU, atomicUTip);

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

            chkUseAtomicU.CheckedChanged += (s, e) =>
            {
                _settings.UseAtomicDisplacement = chkUseAtomicU.Checked;
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
            numericDebyeWaller.Value = (decimal)Math.Clamp(_settings.DisplacementScale, 0.0, 10.0);
            numericResolution.Value = _settings.ProfileResolution;
            trackDebyeWaller.Value = (int)Math.Clamp(Math.Round(_settings.DisplacementScale * 10), 0, 100);
            comboProfile.SelectedIndex = (int)_settings.ProfileType;
            comboSource.SelectedIndex = (int)_settings.Source;
            chkShowDScale.Checked = _settings.XAxisMode == XAxisMode.DSpacing;
            chkShowBackground.Checked = _settings.ShowBackground;
            chkLPCorrection.Checked = _settings.UseLPCorrection;
            chkUseAtomicU.Checked = _settings.UseAtomicDisplacement;
        }

        private void SaveUIToSettings()
        {
            _settings.MinTwoTheta = (double)numericMinTheta.Value;
            _settings.MaxTwoTheta = (double)numericMaxTheta.Value;
            _settings.FWHM = (double)numericFWHM.Value;
            _settings.DisplacementScale = (double)numericDebyeWaller.Value;
            _settings.ProfileResolution = (int)numericResolution.Value;
            _settings.ProfileType = (PeakBroadening.ProfileType)comboProfile.SelectedIndex;
            _settings.Source = (XRaySource)comboSource.SelectedIndex;
            _settings.Wavelength = XRaySourceHelper.GetWavelength(_settings.Source);
            _settings.XAxisMode = chkShowDScale.Checked ? XAxisMode.DSpacing : XAxisMode.TwoTheta;
            _settings.ShowBackground = chkShowBackground.Checked;
            _settings.UseLPCorrection = chkLPCorrection.Checked;
            _settings.UseAtomicDisplacement = chkUseAtomicU.Checked;
        }

        #endregion

        #region Event Handlers — Controls

        private void NumericDebyeWaller_ValueChanged(object sender, EventArgs e)
        {
            _settings.DisplacementScale = (double)numericDebyeWaller.Value;
            trackDebyeWaller.Value = (int)Math.Clamp(Math.Round(_settings.DisplacementScale * 10), 0, 100);
            CalculatePattern();
        }

        private void TrackDebyeWaller_Scroll(object sender, EventArgs e)
        {
            numericDebyeWaller.Value = trackDebyeWaller.Value / 10m;
        }

        private void PowderDiffractionWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            _calcCts?.Cancel();
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

        /// <summary>Elements for which the unsupported-element dialog was already shown.</summary>
        private string _unsupportedWarningShownFor;

        /// <summary>
        /// Starts a (re)calculation of the peak list on a worker thread. A newer request cancels the
        /// running one, so dragging a slider never queues up stale calculations and the window stays
        /// responsive even for large cells.
        /// </summary>
        private async void CalculatePattern()
        {
            if (_isUpdatingUI) return;
            if (_molecule == null || _molecule.AsymmetricPart.Count == 0)
            {
                labelStatus.Text = "No atoms in structure";
                labelStatus.ForeColor = GdiColor.Red;
                return;
            }

            SaveUIToSettings();

            double minTT = _settings.MinTwoTheta, maxTT = _settings.MaxTwoTheta;
            double wavelength = _settings.Wavelength;
            double debyeB = _settings.DebyeWallerB;
            double dispScale = _settings.DisplacementScale;
            bool useLP = _settings.UseLPCorrection;
            bool useU = _settings.UseAtomicDisplacement;

            // A fresh engine per run: the engines of cancelled runs may still be finishing.
            var engine = new PowderDiffractionEngine(_molecule);

            // Warn before a huge calculation (large cell, high 2θ, many atoms).
            var work = engine.EstimateWork(minTT, maxTT, wavelength);
            double estimatedSeconds = work.StructureFactorTerms * 4e-8;   // ≈ 40 ns per sin/cos term
            string rangeKey = $"{minTT:F3}|{maxTT:F3}|{wavelength:F5}";
            if (estimatedSeconds > 15.0 && _largeCalcApprovedKey != rangeKey)
            {
                var answer = MessageBox.Show(this,
                    $"This calculation covers about {work.LatticePoints:N0} reflections and may take " +
                    $"roughly {FormatDuration(estimatedSeconds)}.\n\nReduce the 2θ range or the cell size to speed it up.\n\nContinue?",
                    "Powder diffraction", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    _calcCts?.Cancel();
                    labelStatus.Text = "Cancelled (large calculation)";
                    labelStatus.ForeColor = GdiColor.Orange;
                    return;
                }
                _largeCalcApprovedKey = rangeKey;
            }

            _calcCts?.Cancel();
            var cts = new CancellationTokenSource();
            _calcCts = cts;
            int version = ++_calcVersion;

            labelStatus.Text = "Calculating...";
            labelStatus.ForeColor = GdiColor.Orange;
            toolTip.SetToolTip(labelStatus, string.Empty);

            try
            {
                var token = cts.Token;
                List<DiffractionPeak> peaks = await Task.Run(
                    () => engine.CalculatePeaks(minTT, maxTT, wavelength, debyeB, useLP, useU, dispScale, token), token);

                if (version != _calcVersion || IsDisposed) return;   // superseded by a newer request

                engine.ApplyCorrections(peaks);
                _peaks = peaks.OrderBy(p => p.TwoTheta).ToList();

                toolTip.SetToolTip(labelStatus, string.Empty);
                _unsupportedWarningShownFor = null;

                labelPeakCount.Text = $"Peaks: {_peaks.Count}";
                labelStatus.Text = _peaks.Count > 0 ?
                    $"Calculated {_peaks.Count} peaks" :
                    "No peaks in range";
                labelStatus.ForeColor = _peaks.Count > 0 ? GdiColor.Green : GdiColor.Orange;

                _needsRedraw = true;
                panelPlot.Invalidate();
            }
            catch (OperationCanceledException)
            {
                // a newer calculation replaced this one - nothing to show
            }
            catch (UnsupportedElementException ex)
            {
                if (version != _calcVersion || IsDisposed) return;

                // Never show a pattern from an incomplete structure factor.
                _peaks = new List<DiffractionPeak>();
                labelPeakCount.Text = "Peaks: 0";
                labelStatus.Text = "No scattering factors: " + string.Join(", ", ex.Elements);
                labelStatus.ForeColor = GdiColor.Red;
                toolTip.SetToolTip(labelStatus, ex.Message);

                _needsRedraw = true;
                panelPlot.Invalidate();

                // one dialog per set of elements, not on every parameter change
                string key = string.Join(",", ex.Elements);
                if (_unsupportedWarningShownFor != key)
                {
                    _unsupportedWarningShownFor = key;
                    MessageBox.Show(this, ex.Message, "Powder diffraction",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                if (version != _calcVersion || IsDisposed) return;
                labelStatus.Text = $"Error: {ex.Message}";
                labelStatus.ForeColor = GdiColor.Red;
                System.Diagnostics.Debug.WriteLine($"[PowderDiffraction] Error: {ex}");
            }
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds < 90) return $"{seconds:F0} s";
            if (seconds < 5400) return $"{seconds / 60.0:F0} min";
            return $"{seconds / 3600.0:F1} h";
        }

        #endregion
    }
}