// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — cavity detection orchestration and UI glue.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Diagnostics;
using Tal.Cavity;
using Tal.Forms;
using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal
{
    public partial class MainForm
    {
        #region Cavity Detection

        public async Task RunCavityDetection()
        {
            var doc = GetActiveDocument();
            if (doc == null)
            {
                SetStatus("No active document", "Structures: 0", "⚠️");
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // 1. DETECTOR — if present, reuse; otherwise create
            // ═══════════════════════════════════════════════════════════
            var detector = doc.GetCavityDetector();

            if (detector == null)
            {
                var molecule_ = doc.GetMolecule();
                if (molecule_ == null || molecule_.NumberOfAtoms == 0)
                {
                    SetStatus("No molecule loaded — open a structure first",
                              "Structures: 0", "⚠️");
                    return;
                }

                SetStatus("Initializing cavity detector...", "Structures: 0", "⏳");

                detector = new CavityDetector(molecule_);
                detector.RadiusMultiplier = CavityDetector.BASE_SDF_RADIUS;
                detector.UpdateVdwMultiplier(CavityDetector.BASE_SDF_RADIUS);
                detector.UpdateRes(Flags.Instance.Resolution);

                if (detector.Scanner != null)
                {
                    detector.Scanner.VoxelSize = Flags.Instance.Resolution;
                    detector.Scanner.RadiusMultiplier = CavityDetector.BASE_SDF_RADIUS;
                }

                doc.SetCavityDetector(detector);

                Debug.WriteLine("[RunCavityDetection] CavityDetector created");
            }

            // ═══════════════════════════════════════════════════════════
            // 2. HELPERS — if present, reuse; otherwise create
            // ═══════════════════════════════════════════════════════════
            var helpers = doc.GetCavityHelpers();

            if (helpers == null)
            {
                var molecule_ = doc.GetMolecule();
                if (molecule_ == null)
                {
                    SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                    return;
                }

                helpers = new CavityHelpers(molecule_);
                doc.SetCavityHelpers(helpers);

                Debug.WriteLine("[RunCavityDetection] CavityHelpers created");
            }

            // ═══════════════════════════════════════════════════════════
            // 3. RENDERER — retry loop (GraphicsDevice may not be ready yet)
            // ═══════════════════════════════════════════════════════════
            var renderer = doc.GetCavityRenderer();

            if (renderer == null)
            {
                SetStatus("Initializing cavity renderer...", "Structures: 0", "⏳");

                for (int attempt = 0; attempt < 15; attempt++)
                {
                    await Task.Delay(200);

                    renderer = doc.GetCavityRenderer();
                    if (renderer != null)
                    {
                        Debug.WriteLine(
                            $"[RunCavityDetection] CavityRenderer ready after {(attempt + 1) * 200} ms");
                        break;
                    }
                }

                if (renderer == null)
                {
                    SetStatus("Cavity renderer not initialized — try again in a moment",
                              "Structures: 0", "⚠️");
                    return;
                }
            }

            SetStatus("Calculating cavities...", "Structures: 0", "⏳");

            try
            {
                SetStatus("Detecting cavities...", "Structures: 0", "⏳");

                // ⭐ Heavy detection in the background — do NOT block the UI thread
                bool success = await Task.Run(async () =>
                {
                    return await detector.DetectVoidsAsync(Flags.Instance.VdWValue);
                });

                if (success && detector.HasCavities)
                {
                    var cavities = detector.GetCavities();
                    helpers.SetCavities(cavities);

                    SetStatus($"Found {cavities.Count} cavities, generating mesh...",
                              $"Structures: {StructureStorage.Instance.StructureCount}", "⏳");

                    // Use the same mapping as the Cavity Settings "Smoothing" control (Flags.smuth)
                    int smoothLevel = Math.Clamp(Flags.Instance.smuth, 0, 10);
                    var meshParams = CavityRenderer.GetMeshParametersForSmoothingLevel(smoothLevel);
                    System.Diagnostics.Debug.WriteLine(
                        $"[RunCavityDetection] mesh smoothing level={smoothLevel} " +
                        $"(iter={meshParams.SmoothingIterations}, factor={meshParams.SmoothingFactor})");

                    var cavitiesToMesh = cavities.Where(c => c.Volume > 1.0f).ToList();

                    if (cavitiesToMesh.Count > 0)
                    {
                        SetStatus($"Generating mesh for {cavitiesToMesh.Count} cavities...",
                                  $"Structures: {StructureStorage.Instance.StructureCount}", "⏳");

                        MarchingCubesMeshGenerator.GenerateAllMeshesParallel(
                            cavitiesToMesh,
                            helpers,
                            meshParams
                        );
                    }

                    doc.UpdateCavityData(cavitiesToMesh);

                    var settingsWin = Application.OpenForms
                        .OfType<CavitySettingsWindow>().FirstOrDefault();

                    if (settingsWin != null)
                    {
                        var mode = settingsWin.GetSelectedDrawMode();
                        renderer.CurrentDrawMode = mode;

                        if (mode == CavityRenderer.DrawMode.CavityColorMap)
                        {
                            var propertyType = settingsWin.GetSelectedPropertyType();
                            var mapper = doc.GetPropertyMapper();
                            if (mapper != null)
                            {
                                mapper.CurrentProperty = propertyType;
                            }
                        }
                    }
                    else
                    {
                        renderer.CurrentDrawMode = CavityRenderer.DrawMode.MarchingCubesMesh;
                    }

                    renderer.ForceRefresh();
                    doc.RefreshView();

                    // ⭐ Reset Progressive AA — new geometry jam
                    InvalidateScene3D();




                    SetStatus($"Found {cavities.Count} cavities, mesh generated, {renderer.CavityCount} rendered",
                              $"Structures: {StructureStorage.Instance.StructureCount}", "✅");
                }
                else
                {
                    SetStatus("No cavities found", "Structures: 0", "ℹ️");
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", "Structures: 0", "❌");
                Debug.WriteLine($"[Cavity] Error: {ex.Message}");
                Debug.WriteLine($"[Cavity] Stack: {ex.StackTrace}");
            }
        }

        public void RefreshRepeatedCavities()
        {
            var doc = GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            renderer.BuildRepeatedCavities();
            doc.RefreshView();

            // ⭐ Reset Progressive AA + refresh the scene
            InvalidateScene3D();

            SetStatus($"Cavities rebuilt: {renderer.CavityCount} cavities",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🔄");
        }

        public void RefreshCavitiesAfterPacking()
        {
            var doc = GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            var molecule = doc.GetMolecule();
            if (molecule == null) return;

            renderer.BuildRepeatedCavities();
            renderer.SetMoleculeShift(molecule.MoleculeShift);
             
            InvalidateScene3D();

            SetStatus($"Cavities rebuilt with packing: {renderer.CavityCount} cavities",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "📦");
        }

        public void RefreshCavitiesAfterStructureChange()
        {
            var doc = GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            var molecule = doc.GetMolecule();
            if (molecule == null) return;

            renderer.BuildRepeatedCavities();
            renderer.SetMoleculeShift(molecule.MoleculeShift);
            InvalidateScene3D();

            SetStatus($"Cavities rebuilt: {renderer.CavityCount} cavities",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🔄");
        }

        #endregion

        #region Cavity Buttons

        private async void BtnCavity_Click(object sender, EventArgs e)
        {
            await RunCavityDetection();
        }

        private async void BtnCavityReport_Click(object sender, EventArgs e)
        {
            try
            {
                var doc = GetActiveDocument();
                if (doc == null)
                {
                    SetStatus("No active document", "Structures: 0", "⚠️");
                    return;
                }

                // ═══════════════════════════════════════════════════════════
                // 1. DETECTOR — if present, reuse; otherwise create
                // ═══════════════════════════════════════════════════════════
                var detector = doc.GetCavityDetector();

                if (detector == null)
                {
                    var molecule_ = doc.GetMolecule();
                    if (molecule_ == null || molecule_.NumberOfAtoms == 0)
                    {
                        SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                        return;
                    }

                    SetStatus("Initializing cavity detector...", "Structures: 0", "⏳");

                    detector = new CavityDetector(molecule_);
                    detector.RadiusMultiplier = CavityDetector.BASE_SDF_RADIUS;
                    detector.UpdateVdwMultiplier(CavityDetector.BASE_SDF_RADIUS);
                    detector.UpdateRes(Flags.Instance.Resolution);

                    if (detector.Scanner != null)
                    {
                        detector.Scanner.VoxelSize = Flags.Instance.Resolution;
                        detector.Scanner.RadiusMultiplier = CavityDetector.BASE_SDF_RADIUS;
                    }

                    doc.SetCavityDetector(detector);

                    Debug.WriteLine("[CavityReport] CavityDetector created");
                }

                SetStatus("Starting VdW analysis...", "Structures: 0", "⏳");

                var molecule = doc.GetMolecule();
                if (molecule == null)
                {
                    SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                    return;
                }

                // ⭐ Debug window — show and activate
                var debugWin = GetDebugWindow();
                if (debugWin.IsHidden) debugWin.Show();
                debugWin.Activate();

                await RunVdWAnalysisAsync(molecule, detector, debugWin);
            }
            catch (Exception ex)
            {
                SetStatus($"Report error: {ex.Message}", "Structures: 0", "❌");
                Debug.WriteLine($"[CavityReport] Error: {ex.Message}");
                Debug.WriteLine($"[CavityReport] Stack: {ex.StackTrace}");
            }
        }

        #endregion
    }
}
