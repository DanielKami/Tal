// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Afterwder diffraction module: AfterwderDiffractionWindow.Rendering.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.Core.PowderDiffraction;
using GdiBitmap = System.Drawing.Bitmap;
using GdiColor = System.Drawing.Color;
using GdiDashStyle = System.Drawing.Drawing2D.DashStyle;
using GdiFont = System.Drawing.Font;
using GdiGraphics = System.Drawing.Graphics;
using GdiPen = System.Drawing.Pen;
using GdiPointF = System.Drawing.PointF;
using GdiRectangle = System.Drawing.Rectangle;
using GdiSizeF = System.Drawing.SizeF;
using GdiSmoothingMode = System.Drawing.Drawing2D.SmoothingMode;
using GdiSolidBrush = System.Drawing.SolidBrush;
using GdiTextRenderingHint = System.Drawing.Text.TextRenderingHint;

namespace Tal.Forms
{
 /// <summary>
 /// Rendering warstwa for <see cref="AfterwderDiffractionWindow"/> — drawing
 /// tła, mesh, osi, wzorca dyfractionalgo i afterdafterwiedzi myszy.
 /// </summary>
    public partial class PowderDiffractionWindow
    {
        #region Painting

        private void PanelPlot_Paint(object sender, PaintEventArgs e)
        {
            if (_needsRedraw || _plotBitmap == null || _plotBitmap.Size != panelPlot.Size)
            {
                RenderPlot();
                _needsRedraw = false;
            }

            if (_plotBitmap != null)
            {
                e.Graphics.DrawImage(_plotBitmap, 0, 0);

                if (_isZooming && _zoomRect.Width > 0 && _zoomRect.Height > 0)
                {
                    using (var pen = new GdiPen(ZoomRectColor, 2))
                    {
                        pen.DashStyle = GdiDashStyle.Dash;
                        e.Graphics.DrawRectangle(pen, _zoomRect);
                    }
                    using var brush = new GdiSolidBrush(GdiColor.FromArgb(30, 0, 120, 255));
                    e.Graphics.FillRectangle(brush, _zoomRect);
                }
            }
        }

        private void RenderPlot()
        {
            if (panelPlot.Width <= 0 || panelPlot.Height <= 0)
                return;

            if (_plotBitmap == null || _plotBitmap.Size != panelPlot.Size)
            {
                _plotBitmap?.Dispose();
                _plotBitmap = new GdiBitmap(panelPlot.Width, panelPlot.Height);
            }

            using var g = GdiGraphics.FromImage(_plotBitmap);
            g.SmoothingMode = GdiSmoothingMode.AntiAlias;
            g.TextRenderingHint = GdiTextRenderingHint.AntiAlias;
            g.Clear(PlotBackground);

            var rect = GetPlotRectangle();
            DrawGrid(g, rect);
            DrawPattern(g, rect);
            DrawAxes(g, rect);
        }

        private GdiRectangle GetPlotRectangle()
        {
            int margin = 50;
            int topMargin = chkShowDScale.Checked ? 75 : 50;
            int bottomMargin = 45;
            return new GdiRectangle(
                margin,
                topMargin,
                Math.Max(1, panelPlot.Width - 2 * margin),
                Math.Max(1, panelPlot.Height - margin - bottomMargin - topMargin)
            );
        }

        private void DrawGrid(GdiGraphics g, GdiRectangle rect)
        {
            using var pen = new GdiPen(GridColor, 1);

            for (int i = 0; i <= 20; i++)
            {
                double fraction = i / 20.0;
                double twoTheta = _settings.MinTwoTheta + fraction * (_settings.MaxTwoTheta - _settings.MinTwoTheta);
                int x = MapTwoThetaToX(twoTheta, rect);
                if (x >= rect.Left && x <= rect.Right)
                    g.DrawLine(pen, x, rect.Top, x, rect.Bottom);
            }

            for (int i = 0; i <= 10; i++)
            {
                float y = rect.Bottom - (i / 10f) * rect.Height;
                g.DrawLine(pen, rect.Left, y, rect.Right, y);
            }
        }

        private void DrawAxes(GdiGraphics g, GdiRectangle rect)
        {
            using var pen = new GdiPen(AxisColor, 2);
            using var font = new GdiFont("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            using var brush = new GdiSolidBrush(GdiColor.FromArgb(80, 60, 60, 70));

 // Oś X (dolna)
            g.DrawLine(pen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
 // Oś Y
            g.DrawLine(pen, rect.Left, rect.Top, rect.Left, rect.Bottom);

 // ---- DOLNA SKALA: 2θ ----
            using var fontAxis = new GdiFont("Segoe UI", 8.5f);
            using var brushAxis = new GdiSolidBrush(AxisColor);

            for (int i = 0; i <= 20; i++)
            {
                double fraction = i / 20.0;
                double twoTheta = _settings.MinTwoTheta + fraction * (_settings.MaxTwoTheta - _settings.MinTwoTheta);

                int x = MapTwoThetaToX(twoTheta, rect);
                if (x < rect.Left - 10 || x > rect.Right + 10) continue;

                string label = twoTheta.ToString("F1");

                if (i % 2 == 0)
                {
                    GdiSizeF size = g.MeasureString(label, fontAxis);
                    g.DrawString(label, fontAxis, brushAxis, x - size.Width / 2, rect.Bottom + 5);
                }

                g.DrawLine(pen, x, rect.Bottom, x, rect.Bottom + (i % 2 == 0 ? 6 : 3));
            }

 // Etykieta dolnej osi
            g.DrawString("2θ (°)", fontAxis, brushAxis, rect.Right - 50, rect.Bottom + 22);

 // ---- GÓRNA SKALA: d (Å) - if włączona ----
            if (chkShowDScale.Checked)
            {
                using var bgBrush = new GdiSolidBrush(GdiColor.FromArgb(20, 0, 120, 212));
                g.FillRectangle(bgBrush, rect.Left, rect.Top - 25, rect.Width, 25);

                using var penTop = new GdiPen(DScaleColor, 1);
                g.DrawLine(penTop, rect.Left, rect.Top - 2, rect.Right, rect.Top - 2);

                for (int i = 0; i <= 20; i++)
                {
                    double fraction = i / 20.0;
                    double twoTheta = _settings.MinTwoTheta + fraction * (_settings.MaxTwoTheta - _settings.MinTwoTheta);
                    double d = GetDSpacing(twoTheta);

                    int x = MapTwoThetaToX(twoTheta, rect);
                    if (x < rect.Left - 10 || x > rect.Right + 10) continue;

                    if (i % 2 == 0 && d < 100 && d > 0.1)
                    {
                        string label = d.ToString("F2");
                        GdiSizeF size = g.MeasureString(label, fontAxis);
                        g.DrawString(label, fontAxis, brushAxis, x - size.Width / 2, rect.Top - 22);
                    }

                    g.DrawLine(penTop, x, rect.Top - 2, x, rect.Top - 2 - (i % 2 == 0 ? 6 : 3));
                }

                g.DrawString("d (Å)", fontAxis, brushAxis, rect.Left + 5, rect.Top - 22);
            }

 // ---- LEWA SKALA: Intensywność — obrócona o 90° ----
            using var fontY = new GdiFont("Segoe UI", 9f, System.Drawing.FontStyle.Bold);
            using var brushY = new GdiSolidBrush(GdiColor.FromArgb(80, 60, 60, 70));

            var state = g.Save();
            float xPos = rect.Left - 38;
            float yPos = (rect.Top + rect.Bottom) / 2;

            g.TranslateTransform(xPos, yPos);
            g.RotateTransform(-90);

            GdiSizeF textSize = g.MeasureString("Intensity", fontY);
            g.DrawString("Intensity", fontY, brushY, -textSize.Width / 2, 0);

            g.Restore(state);
        }

        private void DrawPattern(GdiGraphics g, GdiRectangle rect)
        {
            if (_peaks == null || _peaks.Count == 0) return;

            double maxIntensity = _peaks.Max(p => p.Intensity);
            if (maxIntensity <= 0) return;

            double scale = _settings.NormalizeIntensities ? 1.0 / maxIntensity : 1.0;
            int resolution = Math.Max(1, _settings.ProfileResolution);
            int points = Math.Max(2, rect.Width * resolution);
            double[] twoThetaValues = new double[points];
            double[] yValues = new double[points];

            for (int i = 0; i < points; i++)
            {
                double fraction = i / (double)(points - 1);
                double twoTheta = _settings.MinTwoTheta + fraction * (_settings.MaxTwoTheta - _settings.MinTwoTheta);
                twoThetaValues[i] = twoTheta;
                yValues[i] = 0;

                foreach (var peak in _peaks)
                {
                    double fwhm = _settings.FWHM;
                    double contribution = peak.Intensity * scale *
                        PeakBroadening.ProfileFunction(twoTheta, peak.TwoTheta, fwhm, _settings.ProfileType);
                    yValues[i] += contribution;
                }
            }

            double maxY = yValues.Max();
            if (maxY <= 0) return;

            using (var pen = new GdiPen(PatternColor, 2))
            {
                GdiPointF[] pointsArray = new GdiPointF[points];
                for (int i = 0; i < points; i++)
                {
                    int x = MapTwoThetaToX(twoThetaValues[i], rect);
                    float y = rect.Bottom - (float)((yValues[i] / maxY) * rect.Height);
                    pointsArray[i] = new GdiPointF(x, y);
                }
                g.DrawLines(pen, pointsArray);
            }

            if (chkShowBackground.Checked)
            {
                using var brush = new GdiSolidBrush(GdiColor.FromArgb(40, PatternColor));
                GdiPointF[] fillPoints = new GdiPointF[points + 2];
                fillPoints[0] = new GdiPointF(rect.Left, rect.Bottom);
                for (int i = 0; i < points; i++)
                {
                    int x = MapTwoThetaToX(twoThetaValues[i], rect);
                    float y = rect.Bottom - (float)((yValues[i] / maxY) * rect.Height);
                    fillPoints[i + 1] = new GdiPointF(x, y);
                }
                fillPoints[points + 1] = new GdiPointF(rect.Right, rect.Bottom);
                g.FillPolygon(brush, fillPoints);
            }
        }

        #endregion

        #region Coordinate Mapping

        private int MapTwoThetaToX(double twoTheta, GdiRectangle rect)
        {
            double minTheta = _settings.MinTwoTheta;
            double maxTheta = _settings.MaxTwoTheta;
            double range = maxTheta - minTheta;
            if (range <= 0.001) return rect.Left;

            double fraction = (twoTheta - minTheta) / range;
            return rect.Left + (int)(fraction * rect.Width);
        }

        private double GetDSpacing(double twoTheta)
        {
            if (twoTheta <= 0.001) return 1000;
            double thetaRad = (twoTheta / 2) * Math.PI / 180;
            double sinTheta = Math.Sin(thetaRad);
            if (sinTheta < 1e-10) return 1000;
            return _settings.Wavelength / (2 * sinTheta);
        }

        #endregion

        #region Mouse Info Tooltip

        private void UpdateMouseInfo(System.Drawing.Point mousePos)
        {
            if (_peaks == null || _peaks.Count == 0) return;
            if (panelPlot.Width <= 0 || panelPlot.Height <= 0) return;

            var rect = GetPlotRectangle();
            if (rect.Width <= 0 || rect.Height <= 0) return;

            if (mousePos.X < rect.Left || mousePos.X > rect.Right ||
                mousePos.Y < rect.Top || mousePos.Y > rect.Bottom)
            {
                labelHoverInfo.Text = "2θ: -";
                labelHoverDetail.Text = "d: - | hkl: -";
                return;
            }

            double fraction = (double)(mousePos.X - rect.Left) / rect.Width;
            double currentTwoTheta = _settings.MinTwoTheta + fraction * (_settings.MaxTwoTheta - _settings.MinTwoTheta);
            double currentD = GetDSpacing(currentTwoTheta);

            DiffractionPeak closestPeak = null;
            double minDist = double.MaxValue;

            foreach (var peak in _peaks)
            {
                double dist = Math.Abs(peak.TwoTheta - currentTwoTheta);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestPeak = peak;
                }
            }

            double range = _settings.MaxTwoTheta - _settings.MinTwoTheta;
            double tolerance = range * 0.02;

            if (chkShowDScale.Checked)
            {
                labelHoverInfo.Text = $"d: {currentD:F3} Å";
            }
            else
            {
                labelHoverInfo.Text = $"2θ: {currentTwoTheta:F2}°";
            }

            if (closestPeak != null && minDist < tolerance)
            {
                labelHoverDetail.Text = $"2θ: {closestPeak.TwoTheta:F2}° | d: {closestPeak.DSpacing:F3} Å | hkl: ({closestPeak.H}{closestPeak.K}{closestPeak.L}) | I: {closestPeak.Intensity:F0}";
            }
            else
            {
                labelHoverDetail.Text = $"d: {currentD:F3} Å | hkl: -";
            }
        }

        #endregion
    }
}