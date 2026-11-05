// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — van der Waals analysis run and reporting.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Diagnostics;
using System.Text;
using Tal.Cavity;
using Tal.GUI;

namespace Tal
{
    public partial class MainForm
    {
        #region VdW Analysis

        private async Task RunVdWAnalysisAsync(Molecule molecule, CavityDetector detector, DebugWindow debugWin)
        {
            VdWAnalysisResults results = null;

            try
            {
                debugWin.Log("═══════════════════════════════════════════════════════════════", System.Drawing.Color.Cyan);
                debugWin.Log("              VDW SCALING ANALYSIS STARTED", System.Drawing.Color.Cyan);
                debugWin.Log($"              {DateTime.Now}", System.Drawing.Color.Cyan);
                debugWin.Log("═══════════════════════════════════════════════════════════════", System.Drawing.Color.Cyan);

                if (!detector.HasBaseSDF)
                {
                    debugWin.Log("No base SDF available. Computing base SDF first...", System.Drawing.Color.Yellow);
                    SetStatus("Computing base SDF...", "⏳");

                    bool baseOk = await detector.ComputeBaseSDFAsync(CavityDetector.BASE_SDF_RADIUS);
                    if (!baseOk)
                    {
                        debugWin.Log("❌ Failed to compute base SDF!", System.Drawing.Color.Red);
                        SetStatus("Failed to compute base SDF", "❌");
                        return;
                    }
                    debugWin.Log("✅ Base SDF computed successfully", System.Drawing.Color.LightGreen);
                }

                detector.RadiusMultiplier = CavityDetector.BASE_SDF_RADIUS;
                detector.UpdateVdwMultiplier(CavityDetector.BASE_SDF_RADIUS);
                detector.UpdateRes(Flags.Instance.Resolution);
                if (detector.Scanner != null)
                {
                    detector.Scanner.VoxelSize = Flags.Instance.Resolution;
                    detector.Scanner.RadiusMultiplier = CavityDetector.BASE_SDF_RADIUS;
                }
                Flags.Instance.UseSDFCorrection = true;

                debugWin.Log($"Using resolution: {Flags.Instance.Resolution:F3} Å", System.Drawing.Color.LightGray);
                debugWin.Log($"Anisotropic: {Flags.Instance.UseThermalMotion}", System.Drawing.Color.LightGray);

                var progress = new Progress<float>(p =>
                {
                    int percent = (int)(p * 100);
                    if (percent % 10 == 0 || percent == 100)
                    {
                        SetProgress(percent);
                        SetStatus($"VDW analysis: {percent}%", "⏳");
                    }
                });

                debugWin.Log("Starting VdW scale analysis...", System.Drawing.Color.Cyan);

                results = await VdWScaleAnalyzer.RunAnalysisAsync(
                    molecule,
                    detector,
                    scales: null,
                    progress: progress,
                    cancellationToken: CancellationToken.None
                );

                if (results != null && results.CavityResults.Count > 0)
                {
                    string report = VdWAnalysisReport.GenerateTxtReport(results);

                    debugWin.Log("", System.Drawing.Color.LightGray);
                    debugWin.Log("═══════════════════════════════════════════════════════════════", System.Drawing.Color.Cyan);
                    debugWin.Log("                    ANALYSIS COMPLETE", System.Drawing.Color.Cyan);
                    debugWin.Log("═══════════════════════════════════════════════════════════════", System.Drawing.Color.Cyan);
                    debugWin.Log(report, System.Drawing.Color.White);

                    debugWin.Log("", System.Drawing.Color.LightGray);
                    debugWin.Log("═══════════════════════════════════════════════════════════════", System.Drawing.Color.Cyan);
                    debugWin.Log($"Analysis completed: {DateTime.Now}", System.Drawing.Color.Cyan);
                    debugWin.Log($"Total cavities analyzed: {results.CavityResults.Count}", System.Drawing.Color.LightGreen);
                    debugWin.Log($"Total processing time: {results.TotalProcessingTimeMs / 1000:F2} s", System.Drawing.Color.LightGreen);

                    int totalChannels = results.GetTotalChannelCount();
                    int cavitiesWithChannels = results.GetCavitiesWithChannelsCount();
                    debugWin.Log($"Channels found: {totalChannels} channels in {cavitiesWithChannels} cavities", System.Drawing.Color.LightGreen);
                    debugWin.Log("═══════════════════════════════════════════════════════════════", System.Drawing.Color.Cyan);

                    SetProgress(100);
                    SetStatus($"Report generated - {results.CavityResults.Count} cavities analyzed", "📊");

                    var result = MessageBox.Show(
                        $"Analysis complete! {results.CavityResults.Count} cavities analyzed.\n\nDo you want to save the report?",
                        "VdW Analysis Complete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        SaveReportDialog(results);
                    }
                }
                else
                {
                    debugWin.Log("❌ No results from VdW analysis", System.Drawing.Color.Orange);
                    SetStatus("No results from VdW analysis", "ℹ️");
                }
            }
            catch (OperationCanceledException)
            {
                debugWin.Log("⚠️ Analysis cancelled by user", System.Drawing.Color.Orange);
                SetStatus("Analysis cancelled", "⚠️");
            }
            catch (Exception ex)
            {
                debugWin.Log($"❌ ERROR: {ex.Message}", System.Drawing.Color.Red);
                debugWin.Log($"Stack: {ex.StackTrace}", System.Drawing.Color.Red);
                SetStatus($"VdW analysis error: {ex.Message}", "❌");
            }
            finally
            {
                SetProgressIndeterminate(false);

                if (detector != null)
                {
                    float currentVdw = Flags.Instance.VdWValue;
                    detector.RadiusMultiplier = currentVdw;
                    detector.UpdateVdwMultiplier(currentVdw);
                }

                Flags.Instance.smuth = 1;
                Debug.WriteLine("[VDW] Analysis complete - Smoothing reset to 1");
            }
        }

        private void SaveReportDialog(VdWAnalysisResults results)
        {
            try
            {
                using var dialog = new SaveFileDialog();
                dialog.Title = "Save VdW Analysis Report";
                dialog.FileName = $"VdW_Analysis_{DateTime.Now:yyyy-MM-dd_HH-mm}";
                dialog.Filter = "Text files (*.txt)|*.txt|HTML files (*.html)|*.html|All files (*.*)|*.*";
                dialog.DefaultExt = "txt";
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = dialog.FileName;
                    string extension = Path.GetExtension(filePath).ToLower();

                    bool saved = false;

                    if (extension == ".html")
                    {
                        string htmlReport = VdWAnalysisReport.GenerateHtmlReport(results);
                        File.WriteAllText(filePath, htmlReport, Encoding.UTF8);
                        saved = true;
                    }
                    else
                    {
                        string txtReport = VdWAnalysisReport.GenerateTxtReport(results);
                        File.WriteAllText(filePath, txtReport, Encoding.UTF8);
                        saved = true;
                    }

                    if (saved)
                    {
                        SetStatus($"Report saved: {Path.GetFileName(filePath)}",
                                  $"Structures: {StructureStorage.Instance.StructureCount}", "💾");

                        var debugWin = Application.OpenForms.OfType<DebugWindow>().FirstOrDefault();
                        debugWin?.Log($"✅ Report saved to: {filePath}", System.Drawing.Color.LightGreen);
                    }
                    else
                    {
                        SetStatus("Failed to save report", "Structures: 0", "❌");
                    }
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Save error: {ex.Message}", "Structures: 0", "❌");
                MessageBox.Show($"Error saving report: {ex.Message}", "Save Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnVdWProgressChanged(float progress, string message)
        {
            int percent = (int)(progress * 100);
            SetProgress(percent);
            SetStatus($"{message} ({percent}%)", $"Structures: {StructureStorage.Instance.StructureCount}", "⏳");
        }

        private void OnVdWStatusChanged(string message)
        {
            SetStatus(message, $"Structures: {StructureStorage.Instance.StructureCount}", "⏳");
        }

        private void OnVdWErrorOccurred(string message)
        {
            SetStatus($"Error: {message}", "Structures: 0", "❌");
            var debugWin = Application.OpenForms.OfType<DebugWindow>().FirstOrDefault();
            debugWin?.Log($"❌ ERROR: {message}", System.Drawing.Color.Red);
        }

        #endregion
    }
}
