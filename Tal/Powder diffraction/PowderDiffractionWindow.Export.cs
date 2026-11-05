// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Afterwder diffraction module: AfterwderDiffractionWindow.Exafterrt.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Drawing.Imaging;
using System.Text;
using Tal.Core.PowderDiffraction;
using GdiBitmap = System.Drawing.Bitmap;
using GdiColor = System.Drawing.Color;
using GdiPoint = System.Drawing.Point;
using GdiSize = System.Drawing.Size;

namespace Tal.Forms
{
 /// <summary>
 /// Eksafterrt danych i obrazu for <see cref="AfterwderDiffractionWindow"/>:
 /// CSV, PNG, JPEG, BMP, clipboard.
 /// </summary>
    public partial class PowderDiffractionWindow
    {
        #region Export Entry Point

        private void ExportData()
        {
            if (_peaks == null || _peaks.Count == 0)
            {
                MessageBox.Show("No data to export.", "No Data",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new Form();
            dialog.Text = "Export Diffraction Pattern";
            dialog.Size = new GdiSize(350, 180);
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;

            var label = new Label();
            label.Text = "Select export format:";
            label.Location = new GdiPoint(20, 20);
            label.Size = new GdiSize(300, 25);
            dialog.Controls.Add(label);

            var btnCSV = new Button();
            btnCSV.Text = "📊 CSV Data";
            btnCSV.Location = new GdiPoint(20, 60);
            btnCSV.Size = new GdiSize(130, 40);
            btnCSV.DialogResult = DialogResult.Yes;
            dialog.Controls.Add(btnCSV);

            var btnImage = new Button();
            btnImage.Text = "🖼️ PNG Image";
            btnImage.Location = new GdiPoint(170, 60);
            btnImage.Size = new GdiSize(130, 40);
            btnImage.DialogResult = DialogResult.No;
            dialog.Controls.Add(btnImage);

            var btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new GdiPoint(20, 110);
            btnCancel.Size = new GdiSize(280, 30);
            btnCancel.DialogResult = DialogResult.Cancel;
            dialog.Controls.Add(btnCancel);

            var result = dialog.ShowDialog();

            if (result == DialogResult.Yes)
            {
                ExportDataCSV();
            }
            else if (result == DialogResult.No)
            {
                ExportPlotImage();
            }
        }

        #endregion

        #region CSV Export

        private void ExportDataCSV()
        {
            using var dialog = new SaveFileDialog();
            dialog.Title = "Export Diffraction Data";
            dialog.Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt";
            dialog.FileName = $"powder_diffraction_{DateTime.Now:yyyy-MM-dd_HH-mm}";
            dialog.DefaultExt = "csv";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("# 2θ(°), d(Å), Q(Å⁻¹), h, k, l, Intensity, Multiplicity, |F|");

                    foreach (var peak in _peaks)
                    {
                        sb.AppendLine($"{peak.TwoTheta:F3},{peak.DSpacing:F4},{peak.Q:F4}," +
                            $"{peak.H},{peak.K},{peak.L},{peak.Intensity:F2},{peak.Multiplicity},{peak.StructureFactor:F2}");
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                    labelStatus.Text = $"Exported to: {Path.GetFileName(dialog.FileName)}";
                    labelStatus.ForeColor = GdiColor.Green;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export error: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        #region Image Export

        private void CopyPlotImageToClipboard()
        {
            if (_plotBitmap == null || _plotBitmap.Width <= 0 || _plotBitmap.Height <= 0)
            {
                MessageBox.Show("No image to copy. Generate diffraction pattern first.",
                    "No Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                using var ms = new MemoryStream();
                _plotBitmap.Save(ms, ImageFormat.Png);
                var dataObject = new DataObject();
                dataObject.SetData(DataFormats.Bitmap, _plotBitmap);
                dataObject.SetData("PNG", ms.ToArray());
                Clipboard.SetDataObject(dataObject, true);

                labelStatus.Text = "Image copied to clipboard";
                labelStatus.ForeColor = GdiColor.Green;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Copy error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportPlotImage()
        {
            if (_plotBitmap == null || _plotBitmap.Width <= 0 || _plotBitmap.Height <= 0)
            {
                MessageBox.Show("No image to export. Generate diffraction pattern first.",
                    "No Image", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog();
            dialog.Title = "Export Diffraction Image";
            dialog.Filter = "PNG Image (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|BMP Image (*.bmp)|*.bmp";
            dialog.FileName = $"diffractogram_{DateTime.Now:yyyy-MM-dd_HH-mm}";
            dialog.DefaultExt = "png";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var extension = Path.GetExtension(dialog.FileName).ToLower();
                    ImageFormat format = extension switch
                    {
                        ".jpg" or ".jpeg" => ImageFormat.Jpeg,
                        ".bmp" => ImageFormat.Bmp,
                        _ => ImageFormat.Png
                    };

                    if (format == ImageFormat.Jpeg)
                    {
                        var encoderParameters = new EncoderParameters(1);
                        encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
                        var codecInfo = GetEncoderInfo("image/jpeg");
                        if (codecInfo != null)
                        {
                            _plotBitmap.Save(dialog.FileName, codecInfo, encoderParameters);
                        }
                        else
                        {
                            _plotBitmap.Save(dialog.FileName, format);
                        }
                    }
                    else
                    {
                        _plotBitmap.Save(dialog.FileName, format);
                    }

                    labelStatus.Text = $"Image saved to: {Path.GetFileName(dialog.FileName)}";
                    labelStatus.ForeColor = GdiColor.Green;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export error: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
            {
                if (codec.MimeType == mimeType)
                    return codec;
            }
            return null;
        }

        #endregion
    }
}