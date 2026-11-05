// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Lighting / material settings window.

﻿using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class LightingWindow : DockContent
    {
        public LightingWindow()
        {
            InitializeComponent();
            DockWindowConfig.SetupWindow(this, "⚡ Lighting");
            InitializeCustomComponents();
            LoadFromFlags();
            UpdatePreview();
        }

        private void InitializeCustomComponents()
        {
            this.AutoScroll = true;

            // Podpinamy zdarzenia - FIRST LIGHT
            _chkLightingEnabled.CheckedChanged += ChkLightingEnabled_CheckedChanged;
            _chkPerPixelLighting.CheckedChanged += ChkPerPixelLighting_CheckedChanged;
            _trackDirectionX.Scroll += TrackDirectionX_Scroll;
            _trackDirectionY.Scroll += TrackDirectionY_Scroll;
            _trackDirectionZ.Scroll += TrackDirectionZ_Scroll;
            _trackSpecularPower.Scroll += TrackSpecularPower_Scroll;
            _trackSpecularIntensity.Scroll += TrackSpecularIntensity_Scroll;
            _trackDiffuseIntensity.Scroll += TrackDiffuseIntensity_Scroll;
            _trackAmbientIntensity.Scroll += TrackAmbientIntensity_Scroll;

            // ⭐⭐ Wire events - SECOND LIGHT ⭐⭐
            _chkLight2Enabled.CheckedChanged += ChkLight2Enabled_CheckedChanged;
            _trackLight2DirectionX.Scroll += TrackLight2DirectionX_Scroll;
            _trackLight2DirectionY.Scroll += TrackLight2DirectionY_Scroll;
            _trackLight2DirectionZ.Scroll += TrackLight2DirectionZ_Scroll;
            _trackLight2DiffuseIntensity.Scroll += TrackLight2DiffuseIntensity_Scroll;
            _trackLight2AmbientIntensity.Scroll += TrackLight2AmbientIntensity_Scroll;
            _trackLight2SpecularIntensity.Scroll += TrackLight2SpecularIntensity_Scroll;

            // Przyciski
            _btnReset.Click += BtnReset_Click;
            _btnApply.Click += BtnApply_Click;
        }



        ///
        /// ⭐ KLUCZOWE: inkrementuje LightingRevision, so that Progressive AA
        ///    reset frame accumulation (otherwise it mixes frames with different lighting).
        /// </summary>
        private void SyncLightingToRenderer()
        {
            var mainForm = MainForm.Instance;
            if (mainForm == null) return;

            // 1. CavityRenderer of the active document (if any)
            var doc = mainForm.GetActiveDocument();
            if (doc != null)
            {


                var renderer = doc.GetCavityRenderer();
                if (renderer != null)
                {
                    renderer.SyncLightingFromFlags();
                    renderer.ForceRefresh();
                }
            }


            Flags.Instance.VisualRevision++;
            // 3. ⭐ Refresh ALL 3D documents
            mainForm.RefreshAllDocuments();
            mainForm.RefreshMorphologyDocuments();
        }


        private void ChkLightingEnabled_CheckedChanged(object sender, EventArgs e)
        {
            Flags.Instance.Enabled = _chkLightingEnabled.Checked;
            Flags.Instance.VisualRevision++;
            UpdatePreview();
            SyncLightingToRenderer(); // ⭐ DODAJ
        }

        private void TrackDirectionX_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.DirectionX = _trackDirectionX.Value / 100f;
            Flags.Instance.VisualRevision++;
            UpdateLabels();
            UpdatePreview();
            SyncLightingToRenderer(); // ⭐ DODAJ
        }

        private void TrackDirectionY_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.DirectionY = _trackDirectionY.Value / 100f;
            Flags.Instance.VisualRevision++;
            UpdateLabels();
            UpdatePreview();
            SyncLightingToRenderer(); // ⭐ DODAJ
        }

        private void TrackDirectionZ_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.DirectionZ = _trackDirectionZ.Value / 100f;
            UpdateLabels();
            UpdatePreview();
            SyncLightingToRenderer(); // ⭐ DODAJ
        }

        private void TrackSpecularPower_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.SpecularPower = _trackSpecularPower.Value;
            _labelSpecularPower.Text = $"{Flags.Instance.SpecularPower:F0}";
            Flags.Instance.VisualRevision++;
            UpdatePreview();
            SyncLightingToRenderer(); // ⭐ DODAJ
        }

        private void TrackSpecularIntensity_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.SpecularIntensity = _trackSpecularIntensity.Value / 100f;
            _labelSpecularIntensity.Text = $"{Flags.Instance.SpecularIntensity:F2}";
            UpdatePreview();
            SyncLightingToRenderer(); // ⭐ DODAJ
        }

        private void TrackDiffuseIntensity_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.DiffuseIntensity = _trackDiffuseIntensity.Value / 100f;
            _labelDiffuseIntensity.Text = $"{Flags.Instance.DiffuseIntensity:F2}";
            Flags.Instance.VisualRevision++;
            UpdatePreview();
            SyncLightingToRenderer(); // ⭐ DODAJ
        }

        private void TrackAmbientIntensity_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.AmbientIntensity = _trackAmbientIntensity.Value / 100f;
            _labelAmbientIntensity.Text = $"{Flags.Instance.AmbientIntensity:F2}";
            Flags.Instance.VisualRevision++;
            UpdatePreview();
            SyncLightingToRenderer(); // ⭐ DODAJ
        }
        //........................

        // ============================================================
        // ⭐ ZDARZENIA - FIRST LIGHT
        // ============================================================


        // ============================================================
        // ⭐⭐ EVENTS - SECOND LIGHT ⭐⭐
        // ============================================================

        private void ChkLight2Enabled_CheckedChanged(object sender, EventArgs e)
        {
            Flags.Instance.Light2Enabled = _chkLight2Enabled.Checked;
            UpdatePreview();
            SyncLightingToRenderer();
        }

        private void TrackLight2DirectionX_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.Light2DirectionX = _trackLight2DirectionX.Value / 100f;
            Flags.Instance.VisualRevision++;
            UpdateLabelsLight2();
            UpdatePreview();
            SyncLightingToRenderer();
        }

        private void TrackLight2DirectionY_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.Light2DirectionY = _trackLight2DirectionY.Value / 100f;
            UpdateLabelsLight2();
            UpdatePreview();
            SyncLightingToRenderer();
        }

        private void TrackLight2DirectionZ_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.Light2DirectionZ = _trackLight2DirectionZ.Value / 100f;
            Flags.Instance.VisualRevision++;
            UpdateLabelsLight2();
            UpdatePreview();
            SyncLightingToRenderer();
        }

        private void TrackLight2DiffuseIntensity_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.Light2DiffuseIntensity = _trackLight2DiffuseIntensity.Value / 100f;
            _labelLight2DiffuseIntensity.Text = $"{Flags.Instance.Light2DiffuseIntensity:F2}";
            UpdatePreview();
            SyncLightingToRenderer();
        }

        private void TrackLight2AmbientIntensity_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.Light2AmbientIntensity = _trackLight2AmbientIntensity.Value / 100f;
            _labelLight2AmbientIntensity.Text = $"{Flags.Instance.Light2AmbientIntensity:F2}";
            Flags.Instance.VisualRevision++;
            UpdatePreview();
            SyncLightingToRenderer();
        }

        private void ChkPerPixelLighting_CheckedChanged(object sender, EventArgs e)
        {
            Flags.Instance.VisualRevision++;
            Flags.Instance.PerPixelLighting = _chkPerPixelLighting.Checked;
            SyncLightingToRenderer(); // ⭐ SYNCHRONIZUJ Z RENDEREREM
        }

        private void TrackLight2SpecularIntensity_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.Light2SpecularIntensity = _trackLight2SpecularIntensity.Value / 100f;
            _labelLight2SpecularIntensity.Text = $"{Flags.Instance.Light2SpecularIntensity:F2}";
            Flags.Instance.VisualRevision++;
            UpdatePreview();
            SyncLightingToRenderer();
        }

        // ============================================================
        // ⭐ ZDARZENIA - PRZYCISKI
        // ============================================================

        private void BtnReset_Click(object sender, EventArgs e)
        {
            Flags.Instance.ResetToDefault();
            Flags.Instance.VisualRevision++;
            LoadFromFlags();
            UpdatePreview();
            SyncLightingToRenderer();
            ApplyChanges();
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            Flags.Instance.VisualRevision++;
            SyncLightingToRenderer();
            ApplyChanges();
        }

        // ============================================================
        // ⭐ METODY POMOCNICZE
        // ============================================================

        private void LoadFromFlags()
        {
            // FIRST LIGHT
            _chkLightingEnabled.Checked = Flags.Instance.Enabled;
            _chkPerPixelLighting.Checked = Flags.Instance.PerPixelLighting;
            _trackDirectionX.Value = (int)(Flags.Instance.DirectionX * 100);
            _trackDirectionY.Value = (int)(Flags.Instance.DirectionY * 100);
            _trackDirectionZ.Value = (int)(Flags.Instance.DirectionZ * 100);
            _trackSpecularPower.Value = (int)Flags.Instance.SpecularPower;
            _trackSpecularIntensity.Value = (int)(Flags.Instance.SpecularIntensity * 100);
            _trackDiffuseIntensity.Value = (int)(Flags.Instance.DiffuseIntensity * 100);
            _trackAmbientIntensity.Value = (int)(Flags.Instance.AmbientIntensity * 100);

            // ⭐⭐ SECOND LIGHT ⭐⭐
            _chkLight2Enabled.Checked = Flags.Instance.Light2Enabled;
            _trackLight2DirectionX.Value = (int)(Flags.Instance.Light2DirectionX * 100);
            _trackLight2DirectionY.Value = (int)(Flags.Instance.Light2DirectionY * 100);
            _trackLight2DirectionZ.Value = (int)(Flags.Instance.Light2DirectionZ * 100);
            _trackLight2DiffuseIntensity.Value = (int)(Flags.Instance.Light2DiffuseIntensity * 100);
            _trackLight2AmbientIntensity.Value = (int)(Flags.Instance.Light2AmbientIntensity * 100);
            _trackLight2SpecularIntensity.Value = (int)(Flags.Instance.Light2SpecularIntensity * 100);

            Flags.Instance.VisualRevision++;
            UpdateLabels();
            UpdateLabelsLight2();
        }

        private void UpdateLabels()
        {
            _labelDirectionX.Text = $"{Flags.Instance.DirectionX:F2}";
            _labelDirectionY.Text = $"{Flags.Instance.DirectionY:F2}";
            _labelDirectionZ.Text = $"{Flags.Instance.DirectionZ:F2}";
            _labelSpecularPower.Text = $"{Flags.Instance.SpecularPower:F0}";
            _labelSpecularIntensity.Text = $"{Flags.Instance.SpecularIntensity:F2}";
            _labelDiffuseIntensity.Text = $"{Flags.Instance.DiffuseIntensity:F2}";
            _labelAmbientIntensity.Text = $"{Flags.Instance.AmbientIntensity:F2}";
            Flags.Instance.VisualRevision++;
        }

        private void UpdateLabelsLight2()
        {
            _labelLight2DirectionX.Text = $"{Flags.Instance.Light2DirectionX:F2}";
            _labelLight2DirectionY.Text = $"{Flags.Instance.Light2DirectionY:F2}";
            _labelLight2DirectionZ.Text = $"{Flags.Instance.Light2DirectionZ:F2}";
            _labelLight2DiffuseIntensity.Text = $"{Flags.Instance.Light2DiffuseIntensity:F2}";
            _labelLight2AmbientIntensity.Text = $"{Flags.Instance.Light2AmbientIntensity:F2}";
            _labelLight2SpecularIntensity.Text = $"{Flags.Instance.Light2SpecularIntensity:F2}";
            Flags.Instance.VisualRevision++;
        }

        private void UpdatePreview()
        {
            if (_colorPreviewPanel != null)
            {
                // Preview - sum of all intensities
                float totalIntensity = Flags.Instance.DiffuseIntensity +
                                      Flags.Instance.AmbientIntensity +
                                      Flags.Instance.Light2DiffuseIntensity +
                                      Flags.Instance.Light2AmbientIntensity;

                // Normalizuj do zakresu 0-1
                float normalized = Math.Min(totalIntensity, 1.0f);
                int val = (int)(normalized * 255);
                _colorPreviewPanel.BackColor = Color.FromArgb(val, val, val);
                Flags.Instance.VisualRevision++;
            }
        }

        private void ApplyChanges()
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm != null)
            {
                var dockPanel = mainForm.GetDockPanel();
                if (dockPanel != null)
                {
                    foreach (var doc in dockPanel.Documents)
                    {
                        if (doc is MonoGameDocument monoDoc)
                        {
                            monoDoc.RefreshView();
                        }
                    }
                }
            }
            Flags.Instance.Save();
            Flags.Instance.VisualRevision++;
        }


        private void _trackDirectionY_Scroll(object sender, EventArgs e)
        {
            Flags.Instance.VisualRevision++;
        }

        private void LightingWindow_Load(object sender, EventArgs e)
        {
            Flags.Instance.VisualRevision++;
        }

        private void _lblLight1Group_Click(object sender, EventArgs e)
        {
            Flags.Instance.VisualRevision++;
        }

        private void _labelPreview_Click(object sender, EventArgs e)
        {

        }
    }
}