// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection settings window.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Tal.Cavity;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    /// <summary>
    /// Dockable window for configuring cavity detection, display settings,
    /// and property mapping for molecular cavities.
    /// </summary>
    public partial class CavitySettingsWindow : DockContent
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the CavitySettingsWindow.
        /// </summary>
        public CavitySettingsWindow()
        {
            InitializeComponent();
            DockWindowConfig.SetupWindow(this, "🕳️ Cavity Settings");

            LoadFromFlags();
            UpdateControlsState();
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Gets mesh parameters based on smoothing value.
        /// </summary>
        /// <param name="smoothingValue">Smoothing level (0-10).</param>
        /// <returns>MeshParameters for the specified smoothing level.</returns>
        /// <summary>Uses the shared 0–10 mapping (same as detection and regenerate).</summary>
        private MeshParameters GetMeshParametersFromSmoothing(int smoothingValue)
            => CavityRenderer.GetMeshParametersForSmoothingLevel(Math.Clamp(smoothingValue, 0, 10));



        private float ValidateResolution(float value) => Math.Clamp(value, 0.05f, 0.20f);
        private float ValidateVdwMultiplier(float value) => Math.Clamp(value, 0.05f, 3.0f);
        private int ValidateTransparency(int value) => Math.Clamp(value, 0, 10);
        private int ValidateSmoothing(int value) => Math.Clamp(value, 0, 10);
        private int ValidatePropertyType(int value) => Math.Clamp(value, 0, 4);

        #endregion

        #region State Management

        /// <summary>
        /// Loads settings from Flags into UI controls.
        /// </summary>
        private void LoadFromFlags()
        {
            var flags = Flags.Instance;

            this.chkEnableCavity.Checked = flags.ShowCavities;

            float resolution = ValidateResolution(flags.Resolution);
            this.numResolution.Value = (decimal)resolution;

            float vdwMultiplier = ValidateVdwMultiplier(flags.VdWValue);
            this.numVdwMultiplier.Value = (decimal)vdwMultiplier;

            int transparency = ValidateTransparency(flags.transparency);
            this.trackTransparency.Value = transparency;
            this.lblTransparencyValue.Text = (transparency / 10.0f).ToString("F1");

            int smoothing = ValidateSmoothing(flags.smuth);
            this.numSmoothing.Value = smoothing;

            this.chkUseThermalMotion.Checked = flags.UseThermalMotion;

            int drawMode = flags.CavityDrawMode;
            switch (drawMode)
            {
                case 0: this.rbHidden.Checked = true; break;
                case 1: this.rbNormal.Checked = true; break;
                case 2: this.rbPropertyMap.Checked = true; break;
                case 3: this.rbTopology.Checked = true; break;
                case 4: this.rbChannelPaths.Checked = true; break;
                case 5: this.rbSphereFits.Checked = true; break;
                default: this.rbNormal.Checked = true; break;
            }

            int propType = ValidatePropertyType(flags.CavityPropertyType);
            this.cmbPropertyType.SelectedIndex = propType;

            UpdateStatus();
        }

        /// <summary>
        /// Saves settings from UI controls to Flags.
        /// </summary>
        private void SaveToFlags()
        {
            var flags = Flags.Instance;

            float resolution = (float)this.numResolution.Value;
            flags.Resolution = ValidateResolution(resolution);

            float vdwMultiplier = (float)this.numVdwMultiplier.Value;
            flags.VdWValue = ValidateVdwMultiplier(vdwMultiplier);

            int transparency = this.trackTransparency.Value;
            flags.transparency = ValidateTransparency(transparency);

            int smoothing = (int)this.numSmoothing.Value;
            flags.smuth = ValidateSmoothing(smoothing);

            flags.UseThermalMotion = this.chkUseThermalMotion.Checked;
            flags.ShowCavities = this.chkEnableCavity.Checked;

            if (this.rbHidden.Checked) flags.CavityDrawMode = 0;
            else if (this.rbNormal.Checked) flags.CavityDrawMode = 1;
            else if (this.rbPropertyMap.Checked) flags.CavityDrawMode = 2;
            else if (this.rbTopology.Checked) flags.CavityDrawMode = 3;
            else if (this.rbChannelPaths.Checked) flags.CavityDrawMode = 4;
            else if (this.rbSphereFits.Checked) flags.CavityDrawMode = 5;

            int propType = this.cmbPropertyType.SelectedIndex;
            flags.CavityPropertyType = ValidatePropertyType(propType);
        }

        /// <summary>
        /// Updates the enabled state of controls based on current settings.
        /// </summary>
        private void UpdateControlsState()
        {
            bool enabled = this.chkEnableCavity.Checked;

            this.grpDetection.Enabled = enabled;
            this.numResolution.Enabled = enabled;
            this.numVdwMultiplier.Enabled = enabled;
            this.trackTransparency.Enabled = enabled;
            this.numSmoothing.Enabled = enabled;
            this.chkUseThermalMotion.Enabled = enabled;
            this.btnDetectCavities.Enabled = enabled;
            this.btnClearCavities.Enabled = enabled;

            this.grpDisplayMode.Enabled = enabled;
            this.rbHidden.Enabled = enabled;
            this.rbNormal.Enabled = enabled;
            this.rbPropertyMap.Enabled = enabled;
            this.rbTopology.Enabled = enabled;
            this.rbChannelPaths.Enabled = enabled;
            this.rbSphereFits.Enabled = enabled;

            bool isPropertyMap = this.rbPropertyMap.Checked && enabled;
            this.cmbPropertyType.Enabled = isPropertyMap;

            this.btnExportReport.Enabled = enabled;

            UpdateStatus();
        }

        /// <summary>
        /// Updates the status display with current cavity information.
        /// </summary>
        private void UpdateStatus()
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null)
            {
                this.lblCavityCount.Text = "Cavities: 0";
                lstCavityVisibility.Items.Clear();
                lstCavityVisibility.Tag = null;
                return;
            }

            var renderer = doc.GetCavityRenderer();
            if (renderer == null)
            {
                this.lblCavityCount.Text = "Cavities: 0";
                return;
            }

            int count = renderer.CavityCount;
            this.lblCavityCount.Text = $"Cavities: {count}";
            this.lblStatus.Text = this.chkEnableCavity.Checked ?
                $"✅ Cavity display ON - {count} cavities" :
                "⛔ Cavity display OFF";
        }

        #endregion

        #region Event Handlers - Main Toggle

        private void ChkEnableCavity_CheckedChanged(object sender, EventArgs e)
        {
            SaveToFlags();
            UpdateControlsState();

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            if (this.chkEnableCavity.Checked)
            {
                renderer.CurrentDrawMode = GetDrawModeFromRadio();
                renderer.ForceRefresh();
                doc.RefreshView();
                this.lblStatus.Text = $"✅ Cavity display ON - {renderer.CavityCount} cavities";
            }
            else
            {
                renderer.CurrentDrawMode = CavityRenderer.DrawMode.Hidden;
                renderer.ForceRefresh();
                doc.RefreshView();
                this.lblStatus.Text = "⛔ Cavity display OFF";
            }

            mainForm.SetStatus(
                this.chkEnableCavity.Checked ? "Cavity display enabled" : "Cavity display disabled",
                $"Structures: {StructureStorage.Instance.StructureCount}",
                this.chkEnableCavity.Checked ? "✅" : "⛔"
            );

            // ⭐ Reset Progressive AA + refresh the scene
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

        }

        #endregion

        #region Event Handlers - Parameters

        private void NumResolution_ValueChanged(object sender, EventArgs e)
        {
            var flags = Flags.Instance;
            float resolution = (float)this.numResolution.Value;
            flags.Resolution = ValidateResolution(resolution);
            flags.SDFNeedsRebuild = true;

            if (resolution != flags.Resolution)
            {
                this.numResolution.Value = (decimal)flags.Resolution;
            }

            this.lblStatus.Text = $"Resolution changed to {flags.Resolution:F2} Å (rebuild needed)";
        }

        private void NumVdwMultiplier_ValueChanged(object sender, EventArgs e)
        {
            var flags = Flags.Instance;
            float vdwMultiplier = (float)this.numVdwMultiplier.Value;
            flags.VdWValue = ValidateVdwMultiplier(vdwMultiplier);
            flags.SDFNeedsRebuild = true;

            if (vdwMultiplier != flags.VdWValue)
            {
                this.numVdwMultiplier.Value = (decimal)flags.VdWValue;
            }

            this.lblStatus.Text = $"VdW multiplier changed to {flags.VdWValue:F2} (rebuild needed)";
        }

        private void TrackTransparency_ValueChanged(object sender, EventArgs e)
        {
            float value = this.trackTransparency.Value / 10.0f;
            this.lblTransparencyValue.Text = value.ToString("F1");

            var flags = Flags.Instance;
            flags.transparency = ValidateTransparency(this.trackTransparency.Value);

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            renderer.Transparency = value;

            Flags.Instance.VisualRevision++;
            doc.RefreshView();
        }

        private void NumSmoothing_ValueChanged(object sender, EventArgs e)
        {
            int smoothing = (int)this.numSmoothing.Value;
            Flags.Instance.smuth = ValidateSmoothing(smoothing);

            if (smoothing != Flags.Instance.smuth)
            {
                this.numSmoothing.Value = Flags.Instance.smuth;
            }

            RegenerateCavityMesh();
            // ⭐ Reset Progressive AA + refresh the scene
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

            this.lblStatus.Text = $"Smoothing set to {Flags.Instance.smuth} - mesh regenerated";
        }

        private void ChkUseThermalMotion_CheckedChanged(object sender, EventArgs e)
        {
            var flags = Flags.Instance;
            flags.UseThermalMotion = this.chkUseThermalMotion.Checked;
            flags.SDFNeedsRebuild = true;
            this.lblStatus.Text = this.chkUseThermalMotion.Checked ?
                "Thermal motion enabled (rebuild needed)" :
                "Thermal motion disabled (rebuild needed)";
        }

        #endregion

        #region Event Handlers - Display Mode

        private async void RbDisplayMode_CheckedChanged(object sender, EventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio == null || !radio.Checked) return;

            this.cmbPropertyType.Enabled = this.rbPropertyMap.Checked && this.chkEnableCavity.Checked;

            if (!this.chkEnableCavity.Checked) return;

            SaveToFlags();

            if (this.rbPropertyMap.Checked)
            {
                await RegenerateAndApplyPropertyMapAsync();
            }
            else if (this.rbChannelPaths.Checked)
            {
                await CalculateChannelsForCavitiesAsync();
                ApplyDisplayMode();
            }
            else
            {
                ApplyDisplayMode();
            }
            // ⭐ Reset Progressive AA + refresh the scene
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        private async void CmbPropertyType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.chkEnableCavity.Checked) return;
            if (!this.rbPropertyMap.Checked) return;

            SaveToFlags();
            await ApplyCavityPropertyAsync(GetSelectedPropertyType());

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm != null)
            {
                var doc = mainForm.GetActiveDocument();
                if (doc != null)
                {
                    var renderer = doc.GetCavityRenderer();
                    if (renderer != null)
                    {
                        renderer.ForceRefresh();
                        doc.RefreshView();
                    }
                }
            }

            // ⭐ Reset Progressive AA + refresh the scene
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        #endregion

        #region Display Mode Management

        /// <summary>
        /// Gets the draw mode from the selected radio button.
        /// </summary>
        /// <returns>The selected CavityRenderer.DrawMode.</returns>
        private CavityRenderer.DrawMode GetDrawModeFromRadio()
        {
            if (this.rbHidden.Checked) return CavityRenderer.DrawMode.Hidden;
            if (this.rbNormal.Checked) return CavityRenderer.DrawMode.MarchingCubesMesh;
            if (this.rbPropertyMap.Checked) return CavityRenderer.DrawMode.CavityColorMap;
            if (this.rbTopology.Checked) return CavityRenderer.DrawMode.TopologyColorMap;
            if (this.rbChannelPaths.Checked) return CavityRenderer.DrawMode.ChannelPathsOverlay;
            if (this.rbSphereFits.Checked) return CavityRenderer.DrawMode.SphereFitsOnly;
            return CavityRenderer.DrawMode.MarchingCubesMesh;
           
        }

        /// <summary>
        /// Gets the currently selected draw mode.
        /// </summary>
        /// <returns>The selected CavityRenderer.DrawMode.</returns>
        public CavityRenderer.DrawMode GetSelectedDrawMode()
        {
            return GetDrawModeFromRadio();
        }

        /// <summary>
        /// Applies the current display mode to the cavity renderer.
        /// </summary>
        private void ApplyDisplayMode()
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            var mode = GetDrawModeFromRadio();
            renderer.CurrentDrawMode = mode;
            renderer.ForceRefresh();

            Flags.Instance.CavityDrawMode = (int)mode;

            // ⭐ Reset Progressive AA + refresh documents 3D
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
            mainForm.RefreshAllDocuments();
            mainForm.RefreshMorphologyDocuments();

            string modeName = mode.ToString();
            this.lblStatus.Text = $"Display mode: {modeName}";
            mainForm.SetStatus($"Cavity mode: {modeName}", $"Structures: {StructureStorage.Instance.StructureCount}", "🎨");
        }

        #endregion

        #region Property Mapping

        /// <summary>
        /// Gets the selected property type.
        /// </summary>
        /// <returns>The selected CavityPropertyMapper.PropertyType.</returns>
        public CavityPropertyMapper.PropertyType GetSelectedPropertyType()
        {
            int index = this.cmbPropertyType.SelectedIndex;
            return (CavityPropertyMapper.PropertyType)(index + 1);
        }

        /// <summary>
        /// Applies cavity property mapping.
        /// </summary>
        /// <param name="propertyType">The property type to map.</param>
        private async Task ApplyCavityPropertyAsync(CavityPropertyMapper.PropertyType propertyType)
        {
            System.Diagnostics.Debug.WriteLine("[CavitySettings] ========== APPLY PROPERTY START ==========");
            System.Diagnostics.Debug.WriteLine($"[CavitySettings] PropertyType: {propertyType}");

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            if (!renderer.HasCavities)
            {
                this.lblStatus.Text = "⚠️ No cavities to map properties - run detection first";
                return;
            }

            var cavities = renderer.GetCavities();

            // Check if cavities have mesh
            bool hasMesh = cavities.Any(c => c.TrianglesGenerated && c.Vertices != null && c.Vertices.Count > 0);

            if (!hasMesh)
            {
                this.lblStatus.Text = "🔄 No mesh found - generating...";
                this.Refresh();

                await renderer.RegenerateMeshWithSmoothingAsync(Flags.Instance.smuth);

                cavities = renderer.GetCavities();
                hasMesh = cavities.Any(c => c.TrianglesGenerated && c.Vertices != null && c.Vertices.Count > 0);

                if (!hasMesh)
                {
                    this.lblStatus.Text = "⚠️ Failed to generate mesh";
                    return;
                }
            }

            int totalVertices = 0;
            foreach (var c in cavities)
            {
                if (c.Vertices != null)
                    totalVertices += c.Vertices.Count;
            }

            if (totalVertices == 0)
            {
                this.lblStatus.Text = "⚠️ No mesh vertices - run detection first";
                return;
            }

            // Update ColorManager range from molecule
            var molecule = doc.GetMolecule();
            if (molecule != null)
            {
                if (propertyType == CavityPropertyMapper.PropertyType.Charge)
                {
                    if (molecule.ChargeMin < molecule.ChargeMax)
                    {
                        ColorManager.SetDynamicRange(
                            ColorManager.PropertyType.Charge,
                            molecule.ChargeMin,
                            molecule.ChargeMax
                        );
                    }
                }
                else if (propertyType == CavityPropertyMapper.PropertyType.Hydrophobicity)
                {
                    if (molecule.HydrophobicityMin < molecule.HydrophobicityMax)
                    {
                        ColorManager.SetDynamicRange(
                            ColorManager.PropertyType.Hydrophobicity,
                            molecule.HydrophobicityMin,
                            molecule.HydrophobicityMax
                        );
                    }
                }
            }

            // Create or update property mapper
            var mapper = doc.GetPropertyMapper();
            if (mapper == null || mapper.CurrentProperty != propertyType)
            {
                mapper = new CavityPropertyMapper(molecule, cavities);
                doc.SetPropertyMapper(mapper);
            }

            string propertyName = GetPropertyTypeName(propertyType);
            this.lblStatus.Text = $"🔄 Applying {propertyName}...";
            this.Refresh();

            try
            {
                await mapper.ApplyPropertyToCavitiesAsync(propertyType);

                renderer.CopyColorsToRenderCavities();
                renderer.InvalidateNormalsCache();
                renderer.UpdateCavityColors();

                renderer.ForceRefresh();
                doc.RefreshView();

                int coloredCavities = 0;
                int coloredVertices = 0;
                var renderCavities = renderer.GetRenderCavities();

                foreach (var rc in renderCavities)
                {
                    if (rc.VertexColors4 != null && rc.VertexColors4.Length > 0)
                    {
                        coloredCavities++;
                        coloredVertices += rc.VertexColors4.Length;
                    }
                }

                int cavityCount = renderer.CavityCount;
                this.lblStatus.Text = $"✅ Applied {propertyName} to {coloredCavities}/{cavityCount} cavities ({coloredVertices} vertices)";
                this.lblCavityCount.Text = $"Cavities: {cavityCount}";

                mainForm.SetStatus(
                    $"Cavity property: {propertyName} - {coloredCavities}/{cavityCount} cavities",
                    $"Structures: {StructureStorage.Instance.StructureCount}",
                    coloredCavities > 0 ? "🌈" : "⚠️"
                );
            }
            catch (Exception ex)
            {
                this.lblStatus.Text = $"❌ Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[CavitySettings] ❌ Apply property error: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the display name for a property type.
        /// </summary>
        private string GetPropertyTypeName(CavityPropertyMapper.PropertyType type)
        {
            switch (type)
            {
                case CavityPropertyMapper.PropertyType.None: return "None";
                case CavityPropertyMapper.PropertyType.Charge: return "Charge";
                case CavityPropertyMapper.PropertyType.Hydrophobicity: return "Hydrophobicity";
                case CavityPropertyMapper.PropertyType.AtomicNumber: return "Atomic Number";
                case CavityPropertyMapper.PropertyType.VdWRadius: return "VdW Radius";
                case CavityPropertyMapper.PropertyType.Occupancy: return "Occupancy";
                default: return "Unknown";
            }
        }

        #endregion

        #region Mesh Regeneration

        /// <summary>
        /// Regenerates the cavity mesh with property mapping.
        /// </summary>
        private async Task RegenerateAndApplyPropertyMapAsync()
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            if (!renderer.HasCavities)
            {
                this.lblStatus.Text = "⚠️ No cavities - run detection first";
                return;
            }

            var propertyType = GetSelectedPropertyType();

            // Check if mesh exists
            var cavities = renderer.GetCavities();
            bool needsMesh = true;

            if (cavities != null && cavities.Count > 0)
            {
                var first = cavities.FirstOrDefault(c => c.TrianglesGenerated && c.Vertices != null && c.Vertices.Count > 0);
                if (first != null)
                {
                    needsMesh = false;
                }
            }

            if (needsMesh)
            {
                this.lblStatus.Text = "🔄 Generating mesh...";
                this.Refresh();

                await renderer.RegenerateMeshWithSmoothingAsync(Flags.Instance.smuth);
                renderer.ForceRefresh();
                doc.RefreshView();

                cavities = renderer.GetCavities();
            }

            // Update ColorManager range from molecule
            var molecule = doc.GetMolecule();
            if (molecule != null)
            {
                if (propertyType == CavityPropertyMapper.PropertyType.Charge)
                {
                    if (molecule.ChargeMin < molecule.ChargeMax)
                    {
                        ColorManager.SetDynamicRange(
                            ColorManager.PropertyType.Charge,
                            molecule.ChargeMin,
                            molecule.ChargeMax
                        );
                    }
                }
                else if (propertyType == CavityPropertyMapper.PropertyType.Hydrophobicity)
                {
                    if (molecule.HydrophobicityMin < molecule.HydrophobicityMax)
                    {
                        ColorManager.SetDynamicRange(
                            ColorManager.PropertyType.Hydrophobicity,
                            molecule.HydrophobicityMin,
                            molecule.HydrophobicityMax
                        );
                    }
                }
            }

            // Apply property mapping
            await ApplyCavityPropertyAsync(propertyType);

            // Set display mode
            renderer.CurrentDrawMode = CavityRenderer.DrawMode.CavityColorMap;
            renderer.ForceRefresh();
            doc.RefreshView();

            string propertyName = GetPropertyTypeName(propertyType);
            this.lblStatus.Text = $"Property map: {propertyName}";
            mainForm.SetStatus($"Cavity property: {propertyName}", $"Structures: {StructureStorage.Instance.StructureCount}", "🌈");
        }

        /// <summary>
        /// Regenerates the cavity mesh with current smoothing settings.
        /// </summary>
        private async void RegenerateCavityMesh()
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            if (!renderer.HasCavities)
            {
                this.lblStatus.Text = "⚠️ No cavities to smooth - run detection first";
                return;
            }

            int smoothingValue = Flags.Instance.smuth;
            this.lblStatus.Text = $"🔄 Regenerating mesh with smoothing={smoothingValue}...";
            this.Refresh();

            try
            {
                await renderer.RegenerateMeshWithSmoothingAsync(smoothingValue);
                doc.RefreshView();

                int triCount = renderer.GetTotalMarchingCubesTriangles();
                int cavityCount = renderer.CavityCount;

                this.lblStatus.Text = $"✅ Mesh regenerated: {triCount:N0} triangles, {cavityCount} cavities (smoothing={smoothingValue})";
                this.lblCavityCount.Text = $"Cavities: {cavityCount}";

                mainForm.SetStatus(
                    $"Cavity mesh regenerated with smoothing={smoothingValue}",
                    $"Structures: {StructureStorage.Instance.StructureCount}",
                    "🔄"
                );
            }
            catch (Exception ex)
            {
                this.lblStatus.Text = $"❌ Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[CavitySettings] ❌ Regenerate mesh error: {ex.Message}");
            }
        }

        #endregion

        #region Channel Calculation

        /// <summary>
        /// Calculates channels for all cavities.
        /// </summary>
        private async Task CalculateChannelsForCavitiesAsync()
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            var cavities = renderer.GetCavities();
            if (cavities == null || cavities.Count == 0)
            {
                this.lblStatus.Text = "⚠️ No cavities to analyze channels";
                return;
            }

            var cavitiesWithMesh = cavities.Where(c => c.TrianglesGenerated && c.Vertices != null && c.Vertices.Count > 0).ToList();
            if (cavitiesWithMesh.Count == 0)
            {
                this.lblStatus.Text = "⚠️ No cavities with mesh - run detection first";
                return;
            }

            bool hasChannels = cavitiesWithMesh.Any(c => c.CrystalChannelResults != null && c.CrystalChannelResults.Count > 0);
            if (hasChannels)
            {
                renderer.UpdateCrystalChannelPaths();
                return;
            }

            this.lblStatus.Text = $"🔄 Calculating channels for {cavitiesWithMesh.Count} cavities...";
            this.Refresh();

            try
            {
                await Task.Run(() =>
                {
                    foreach (var cavity in cavitiesWithMesh)
                    {
                        try
                        {
                            var pathFinder = new CavityPathFinder(cavity, 0.5f);
                            pathFinder.EnableDetailedLogging = true;
                            pathFinder.MaxPathLength = 50000;
                            pathFinder.MaxStartPoints = 20;
                            pathFinder.SmoothingIterations = 3;

                            var results = pathFinder.AnalyzeAllChannels();
                            cavity.CrystalChannelResults = results;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CavitySettings] Error analyzing cavity {cavity.Id}: {ex.Message}");
                        }
                    }
                });

                renderer.UpdateCrystalChannelPaths();
                renderer.ForceRefresh();

                // ⭐ Reset Progressive AA + refresh the scene
                Flags.Instance.VisualRevision++;
                Flags.Instance.MarkDirty();

                var doc2 = mainForm.GetActiveDocument();
                if (doc2 != null) doc2.RefreshView();

                int totalChannels = cavitiesWithMesh.Sum(c => c.CrystalChannelResults?.Count(r => r.Value?.IsConnected == true) ?? 0);
                this.lblStatus.Text = $"✅ Channels calculated: {totalChannels} channels in {cavitiesWithMesh.Count} cavities";
                mainForm.SetStatus($"Channels: {totalChannels} found", $"Structures: {StructureStorage.Instance.StructureCount}", "🔀");
            }
            catch (Exception ex)
            {
                this.lblStatus.Text = $"❌ Error calculating channels: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[CavitySettings] ❌ Error: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers - Buttons

        private async void BtnDetectCavities_Click(object sender, EventArgs e)
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            this.btnDetectCavities.Enabled = false;
            this.btnClearCavities.Enabled = false;
            this.lblStatus.Text = "🔄 Detecting cavities...";
            this.Refresh();


            this.rbHidden.Checked =false;
            this.rbNormal.Checked = true;
            this.rbPropertyMap.Checked = false;
            this.rbTopology.Checked = false;
            this.rbChannelPaths.Checked = false;
            this.rbSphereFits.Checked = false;

            try
            {
                await mainForm.RunCavityDetection();

                var doc = mainForm.GetActiveDocument();
                if (doc != null)
                {
                    var renderer = doc.GetCavityRenderer();
                    var molecule = doc.GetMolecule();

                    if (renderer != null && renderer.HasCavities)
                    {
                        // Update ColorManager range from molecule
                        if (molecule != null)
                        {
                            if (molecule.ChargeMin < molecule.ChargeMax)
                            {
                                ColorManager.SetDynamicRange(
                                    ColorManager.PropertyType.Charge,
                                    molecule.ChargeMin,
                                    molecule.ChargeMax
                                );
                            }

                            if (molecule.HydrophobicityMin < molecule.HydrophobicityMax)
                            {
                                ColorManager.SetDynamicRange(
                                    ColorManager.PropertyType.Hydrophobicity,
                                    molecule.HydrophobicityMin,
                                    molecule.HydrophobicityMax
                                );
                            }
                        }

                        // Update property mapper with new cavities
                        var cavities = renderer.GetCavities();
                        var mapper = new CavityPropertyMapper(molecule, cavities);
                        doc.SetPropertyMapper(mapper);

                        // Apply property map if enabled
                        if (this.rbPropertyMap.Checked && this.chkEnableCavity.Checked)
                        {
                            var propertyType = GetSelectedPropertyType();
                            await ApplyCavityPropertyAsync(propertyType);
                        }
                        else
                        {
                            renderer.CurrentDrawMode = GetDrawModeFromRadio();
                            renderer.ForceRefresh();
                            doc.RefreshView();
                        }

                        int count = renderer.CavityCount;
                        int triCount = renderer.GetTotalMarchingCubesTriangles();
                        PopulateCavityVisibilityList();
                        this.lblCavityCount.Text = $"Cavities: {count}";
                        this.lblStatus.Text = $"✅ Detection complete: {count} cavities, {triCount:N0} triangles";

                        mainForm.SetStatus(
                            $"Cavities detected: {count}",
                            $"Structures: {StructureStorage.Instance.StructureCount}",
                            "🔬"
                        );
                    }
                    else
                    {
                        this.lblStatus.Text = "⚠️ No cavities detected";
                        this.lblCavityCount.Text = "Cavities: 0";
                        lstCavityVisibility.Items.Clear();
                        lstCavityVisibility.Tag = null;
                    }
                }
            }
            catch (Exception ex)
            {
                this.lblStatus.Text = $"❌ Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[CavitySettings] ❌ Detection error: {ex.Message}");
                mainForm.SetStatus($"Error: {ex.Message}", "", "❌");
            }
            finally
            {
                this.btnDetectCavities.Enabled = true;
                this.btnClearCavities.Enabled = true;
            }
        }

        private void BtnClearCavities_Click(object sender, EventArgs e)
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            try
            {
                // ⭐ Dispose old mapper before removal
                var oldMapper = doc.GetPropertyMapper();
                if (oldMapper != null)
                {
                    try { oldMapper.Dispose(); } catch { /* ignore */ }
                    doc.SetPropertyMapper(null);
                }

                // ⭐ Clear renderer
                renderer.Clear();
                renderer.CurrentDrawMode = CavityRenderer.DrawMode.Hidden;

                // ⭐ Clear detector and helpers
                var detector = doc.GetCavityDetector();
                if (detector != null)
                {
                    try { detector.Clear(); } catch { /* ignore */ }
                }

                var helpers = doc.GetCavityHelpers();
                if (helpers != null)
                {
                    try { helpers.ClearCavities(); } catch { /* ignore */ }
                }

                // ⭐ Reset ColorManager
                ColorManager.ResetDynamicRange();

                // ⭐ Restore flags to defaults
                Flags.Instance.CavityDrawMode = 0;
                Flags.Instance.CavityPropertyType = 0;
                Flags.Instance.UseSDFCorrection = false;
                Flags.Instance.SDFNeedsRebuild = false;
                Flags.Instance.ForceSDFRecalculation = false;

                // ⭐ Refresh
                doc.RefreshView();
                Flags.Instance.VisualRevision++;
                Flags.Instance.MarkDirty();

                this.lblCavityCount.Text = "Cavities: 0";
                this.lblStatus.Text = "🧹 Cavities cleared";

                mainForm.SetStatus("Cavities cleared", $"Structures: {StructureStorage.Instance.StructureCount}", "🧹");
            }
            catch (Exception ex)
            {
                this.lblStatus.Text = $"❌ Clear error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[CavitySettings] ❌ Clear error: {ex.Message}");
            }
        }

        private void BtnExportReport_Click(object sender, EventArgs e)
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null) return;

            var renderer = doc.GetCavityRenderer();
            if (renderer == null) return;

            if (renderer.CavityCount == 0)
            {
                this.lblStatus.Text = "⚠️ No cavities to export";
                return;
            }

            var molecule = doc.GetMolecule();
            var detector = doc.GetCavityDetector();
            var helpers = doc.GetCavityHelpers();
            var cavities = renderer.GetCavities();

            if (molecule == null || detector == null)
            {
                this.lblStatus.Text = "⚠️ Missing molecule or detector";
                return;
            }

            string report = CavitySingleRunReport.Generate(molecule, detector, cavities, helpers);

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                dialog.Title = "Export Cavity Report";
                dialog.FileName = $"cavity_report_{molecule.Name ?? "structure"}_{DateTime.Now:yyyyMMdd_HHmmss}";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(dialog.FileName, report, Encoding.UTF8);
                        this.lblStatus.Text = $"📊 Report exported: {Path.GetFileName(dialog.FileName)}";
                        mainForm.SetStatus("Cavity report exported",
                            $"Structures: {StructureStorage.Instance.StructureCount}", "📊");
                    }
                    catch (Exception ex)
                    {
                        this.lblStatus.Text = $"❌ Export error: {ex.Message}";
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the window from Flags settings.
        /// </summary>
        public void UpdateFromFlags()
        {
            LoadFromFlags();
            UpdateControlsState();
        }

        /// <summary>
        /// Refreshes the status display.
        /// </summary>
        public void RefreshStatus()
        {
            UpdateStatus();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // ⭐ Reset ColorManager when closing windows
            try
            {
                ColorManager.ResetDynamicRange();
            }
            catch { /* ignore */ }

            base.OnFormClosing(e);
        }
        #endregion

        #region Per-cavity visibility

        private bool _loadingCavityList;

        /// <summary>Fill checkbox list from cavities currently in the renderer.</summary>
        private void PopulateCavityVisibilityList()
        {
            _loadingCavityList = true;
            try
            {
                lstCavityVisibility.Items.Clear();
                var mainForm = Application.OpenForms["MainForm"] as MainForm;
                var doc = mainForm?.GetActiveDocument();
                var renderer = doc?.GetCavityRenderer();
                if (renderer == null) return;

                var cavities = renderer.GetCavities();
                if (cavities == null || cavities.Count == 0) return;

                // Stable order by Id
                foreach (var c in cavities.OrderBy(x => x.Id))
                {
                    string label = $"#{c.Id}  V={c.Volume:F1} Å³";
                    if (c.IsExterior) label += "  (ext)";
                    int idx = lstCavityVisibility.Items.Add(label);
                    lstCavityVisibility.SetItemChecked(idx, c.Visible);
                    // Tag via parallel: store id in item - CheckedListBox doesn't support Tag per item easily;
                    // encode id in front and parse, or keep list of ids
                }
                // Store ids in Tag of the list for lookup
                lstCavityVisibility.Tag = cavities.OrderBy(x => x.Id).Select(c => c.Id).ToList();
            }
            finally
            {
                _loadingCavityList = false;
            }
        }

        private void LstCavityVisibility_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_loadingCavityList) return;

            // Use NewValue — GetItemChecked is still the OLD state during ItemCheck
            bool visible = (e.NewValue == CheckState.Checked);
            int cavityId = ResolveCavityIdAtIndex(e.Index);
            if (cavityId < 0) return;

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            var doc = mainForm?.GetActiveDocument();
            var renderer = doc?.GetCavityRenderer();
            if (renderer == null) return;

            renderer.SetCavityVisible(cavityId, visible);
            renderer.ForceRefresh();
            doc.RefreshView();
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        private int ResolveCavityIdAtIndex(int index)
        {
            var ids = lstCavityVisibility.Tag as System.Collections.Generic.List<int>;
            if (ids != null && index >= 0 && index < ids.Count)
                return ids[index];

            string s = lstCavityVisibility.Items[index]?.ToString() ?? "";
            // "#12  V=..."
            if (s.StartsWith("#"))
            {
                int end = 1;
                while (end < s.Length && char.IsDigit(s[end])) end++;
                if (end > 1 && int.TryParse(s.Substring(1, end - 1), out int id))
                    return id;
            }
            return -1;
        }

        private void ApplyCavityVisibilityFromList()
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            var doc = mainForm?.GetActiveDocument();
            var renderer = doc?.GetCavityRenderer();
            if (renderer == null) return;

            for (int i = 0; i < lstCavityVisibility.Items.Count; i++)
            {
                int id = ResolveCavityIdAtIndex(i);
                if (id < 0) continue;
                renderer.SetCavityVisible(id, lstCavityVisibility.GetItemChecked(i));
            }

            renderer.ForceRefresh();
            doc.RefreshView();
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        private void BtnCavitiesAll_Click(object sender, EventArgs e)
        {
            _loadingCavityList = true;
            for (int i = 0; i < lstCavityVisibility.Items.Count; i++)
                lstCavityVisibility.SetItemChecked(i, true);
            _loadingCavityList = false;

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            var doc = mainForm?.GetActiveDocument();
            var r = doc?.GetCavityRenderer();
            r?.SetAllCavitiesVisible(true);
            r?.ForceRefresh();
            doc?.RefreshView();
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        private void BtnCavitiesNone_Click(object sender, EventArgs e)
        {
            _loadingCavityList = true;
            for (int i = 0; i < lstCavityVisibility.Items.Count; i++)
                lstCavityVisibility.SetItemChecked(i, false);
            _loadingCavityList = false;

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            var doc = mainForm?.GetActiveDocument();
            var r = doc?.GetCavityRenderer();
            r?.SetAllCavitiesVisible(false);
            r?.ForceRefresh();
            doc?.RefreshView();
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        #endregion


    }
}