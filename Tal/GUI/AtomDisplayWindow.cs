// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Atom display settings — ball sizes, stick thickness, ADP ellipsoids.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Atom display settings window — ball sizes, stick thickness,
// ADP ellipsoid parameters.
//
// ⭐ KEY: this window is the ONLY place that converts probability percent
//             of ADP ellipsoids into the scale factor (√χ²).
//             Both fields are written to Flags consistently:
//               - ATP_ProbabilityPercent  (raw percent, source of truth)
//               - ATP_Probability         (wyliczony scale factor, cache for Draw.cs)

using System;
using System.Drawing;
using System.Windows.Forms;
using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class AtomDisplayWindow : DockContent
    {
        private bool _isUpdating = false;

        #region Chi² — tabela i interpolacja

        /// <summary>
        /// Reference χ²(3) values for typical confidence levels.
        /// Formula: scale = √χ²(3, p).
        /// </summary>
        private static readonly int[] ATP_PERCENTS =
        {
            1, 5, 10, 20, 30, 40, 50, 60, 70, 80, 90, 95, 99
        };

        private static readonly float[] ATP_CHI2 =
        {
            0.115f, 0.352f, 0.584f, 1.005f, 1.000f,
            1.360f, 2.366f, 3.665f, 5.318f, 7.815f,
            9.348f, 11.345f, 14.16f
        };

        /// <summary>
        /// Convert probability percent → scale factor for ADP ellipsoids.
        /// Formula: scale = √χ²(3, p).
        ///
        /// Reference values:
        ///   30% → 1.000   (klasyczny ORTEP)
        ///   50% → 1.538   (IUCr standard)
        ///   70% → 2.306
        ///   90% → 3.057
        ///   99% → 3.762
        /// </summary>
        public static float ProbabilityToScaleFactor(int percent)
        {
            percent = Math.Clamp(percent, 1, 99);

            // Find interval in the table
            for (int i = 0; i < ATP_PERCENTS.Length - 1; i++)
            {
                if (percent >= ATP_PERCENTS[i] && percent <= ATP_PERCENTS[i + 1])
                {
                    float t = (percent - ATP_PERCENTS[i]) /
                              (float)(ATP_PERCENTS[i + 1] - ATP_PERCENTS[i]);
                    float chi2Interp = ATP_CHI2[i] +
                                       t * (ATP_CHI2[i + 1] - ATP_CHI2[i]);
                    return (float)Math.Sqrt(chi2Interp);
                }
            }

            return 1.538f;   // fallback = 50%
        }

        /// <summary>
        /// Sets ADP ellipsoid probability level and recomputes the scale factor.
        /// Call this instead of setting Flags.ATP_ProbabilityPercent directly.
        ///
        /// Ensures CONSISTENCY of both Flags fields and resets Progressive AA.
        /// </summary>
        public static void SetAtpProbability(int percent)
        {
            percent = Math.Clamp(percent, 1, 99);

            Flags.Instance.ATP_ProbabilityPercent = percent;
            Flags.Instance.ATP_Probability = ProbabilityToScaleFactor(percent);
            Flags.Instance.VisualRevision++;
          
        }

        #endregion

        #region Constructor

        public AtomDisplayWindow()
        {
            InitializeComponent();
            DockWindowConfig.SetupWindow(this, "🎨 Atom Display Settings");
            InitializeCustomComponents();
            LoadFromFlags();
        }

        private void InitializeCustomComponents()
        {
            // ⭐ Rozmiary kulek
            _trackNonMetal.Scroll += TrackNonMetal_Scroll;
            _trackMetal.Scroll += TrackMetal_Scroll;
            _trackHydrogen.Scroll += TrackHydrogen_Scroll;

            // ⭐ Stick thickness
            _trackStickRadius.Scroll += TrackStickRadius_Scroll;
            _trackStickFactor.Scroll += TrackStickFactor_Scroll;

            // ⭐ Parametry elipsoid
            _chkEllipsoidDisks.CheckedChanged += ChkEllipsoidDisks_CheckedChanged;
            _chkShowHydrogenIsoSpheres.CheckedChanged += ChkShowHydrogenIsoSpheres_CheckedChanged;
            _trackEllipsoidProbability.Scroll += TrackEllipsoidProbability_Scroll;
            _trackEllipsoidDiskThickness.Scroll += TrackEllipsoidDiskThickness_Scroll;
            _trackEllipsoidBaseRadius.Scroll += TrackEllipsoidBaseRadius_Scroll;

            // ⭐ Przyciski
            _btnDefault.Click += BtnDefault_Click;
            _btnReset.Click += BtnReset_Click;
            _btnApply.Click += BtnApply_Click;

            this.AutoScroll = true;
        }

        #endregion

        #region Events — rozmiary kulek

        private void TrackNonMetal_Scroll(object sender, EventArgs e)
        {
            float value = _trackNonMetal.Value / 100f;
            Flags.Instance.BallRadiusNonMetal = value;
            _labelNonMetalValue.Text = value.ToString("F2");
            RefreshScene();
        }

        private void TrackMetal_Scroll(object sender, EventArgs e)
        {
            float value = _trackMetal.Value / 100f;
            Flags.Instance.BallRadiusMetal = value;
            _labelMetalValue.Text = value.ToString("F2");
            RefreshScene();
        }

        private void TrackHydrogen_Scroll(object sender, EventArgs e)
        {
            float value = _trackHydrogen.Value / 100f;
            Flags.Instance.HydrogenBallScale = value;
            _labelHydrogenValue.Text = $"{_trackHydrogen.Value}%";
            RefreshScene();
        }

        #endregion

        #region Events — stick thickness

        private void TrackStickRadius_Scroll(object sender, EventArgs e)
        {
            float value = _trackStickRadius.Value / 100f;
            Flags.Instance.StickRadius = value;
            _labelStickRadiusValue.Text = value.ToString("F2");
            RefreshScene();
        }

        private void TrackStickFactor_Scroll(object sender, EventArgs e)
        {
            float value = _trackStickFactor.Value / 10000f;
            Flags.Instance.StickRadiusFactor = value;
            _labelStickFactorValue.Text = value.ToString("F4");
            RefreshScene();
        }

        #endregion

        #region Events — parametry elipsoid

        private void ChkEllipsoidDisks_CheckedChanged(object sender, EventArgs e)
        {
            Flags.Instance.EllipsoidDisksEnabled = _chkEllipsoidDisks.Checked;
            RefreshScene();
        }

        private void ChkShowHydrogenIsoSpheres_CheckedChanged(object sender, EventArgs e)
        {
            Flags.Instance.ShowHydrogenIsoSpheres = _chkShowHydrogenIsoSpheres.Checked;
            RefreshScene();
        }

        /// <summary>
        /// ⭐ KLUCZOWE: slider edytuje PROCENT (1-99), not scale factor.
        ///
        /// After zmianie:
        ///   1. Zapisujemy czysty procent do Flags.ATP_ProbabilityPercent
        ///   2. Przeliczamy √χ²(3, p) → Flags.ATP_Probability
        ///   3. Inkrementujemy VisualRevision — reset Progressive AA
        ///   4. Refresh the scene
        /// </summary>
        private void TrackEllipsoidProbability_Scroll(object sender, EventArgs e)
        {
            int percent = _trackEllipsoidProbability.Value;

            // ⭐ Set both fields consistently via helper
            SetAtpProbability(percent);

            // ⭐ Refresh label — show percent and resulting scale
            float scale = Flags.Instance.ATP_Probability;
            _labelEllipsoidProbabilityValue.Text = $"{percent}%  (×{scale:F3})";

            RefreshScene();
        }

        private void TrackEllipsoidDiskThickness_Scroll(object sender, EventArgs e)
        {
            float value = _trackEllipsoidDiskThickness.Value / 100f;
            Flags.Instance.EllipsoidDiskThickness = value;
            _labelEllipsoidDiskThicknessValue.Text = value.ToString("F2");
            RefreshScene();
        }

        private void TrackEllipsoidBaseRadius_Scroll(object sender, EventArgs e)
        {
            float value = _trackEllipsoidBaseRadius.Value / 100f;
            Flags.Instance.EllipsoidBaseRadius = value;
            _labelEllipsoidBaseRadiusValue.Text = value.ToString("F2");
            RefreshScene();
        }

        #endregion

        #region Events — przyciski

        private void BtnDefault_Click(object sender, EventArgs e)
        {
            _isUpdating = true;

            // Rozmiary kulek
            _trackNonMetal.Value = 8;
            _trackMetal.Value = 12;
            _trackHydrogen.Value = 70;

            // Stick thickness
            _trackStickRadius.Value = 3;
            _trackStickFactor.Value = 127;


            // Parametry elipsoid
            _chkEllipsoidDisks.Checked = true;
            _chkShowHydrogenIsoSpheres.Checked = true;
            _trackEllipsoidProbability.Value = 50;      // 50% standard
            _trackEllipsoidDiskThickness.Value = 15;    // 0.15
            _trackEllipsoidBaseRadius.Value = 105;      // 1.05

            // ⭐ Set procent + przelicz scale factor
            SetAtpProbability(_trackEllipsoidProbability.Value);

            UpdateLabels();
            LoadFromFlags();
            UpdatePreview();
            _isUpdating = false;
            Flags.Instance.VisualRevision++;
            ApplyChanges();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            _isUpdating = true;
            LoadFromFlags();
            UpdateLabels();
            UpdatePreview();
            _isUpdating = false;
            Flags.Instance.VisualRevision++;
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            Flags.Instance.VisualRevision++;
            ApplyChanges();
        }

        #endregion

        #region Metody pomocnicze

        private void LoadFromFlags()
        {
            _isUpdating = true;

            // ── Rozmiary kulek ──
            _trackNonMetal.Value = (int)(Flags.Instance.BallRadiusNonMetal * 100);
            _trackMetal.Value = (int)(Flags.Instance.BallRadiusMetal * 100);
            _trackHydrogen.Value = (int)(Flags.Instance.HydrogenBallScale * 100);

            // ── Stick thickness ──
            _trackStickRadius.Value = (int)(Flags.Instance.StickRadius * 100);
            _trackStickFactor.Value = (int)(Flags.Instance.StickRadiusFactor * 10000);

            // ── Parametry elipsoid ──
            _chkEllipsoidDisks.Checked = Flags.Instance.EllipsoidDisksEnabled;
            _chkShowHydrogenIsoSpheres.Checked = Flags.Instance.ShowHydrogenIsoSpheres;

            // ⭐ Sanity check + migracja starego formatu
            if (Flags.Instance.ATP_ProbabilityPercent <= 0 ||
                Flags.Instance.ATP_ProbabilityPercent > 99)
            {
                // Old config — set default 50%
                Flags.Instance.ATP_ProbabilityPercent = 50;
                Flags.Instance.ATP_Probability = ProbabilityToScaleFactor(50);

                System.Diagnostics.Debug.WriteLine(
                    "[AtomDisplayWindow] Migrated ATP → 50% (×1.538)");
            }
            else
            {
                // ⭐ ALWAYS przelicz scale factor z procentu —
                //    guarantees consistency even if the JSON had an old value
                Flags.Instance.ATP_Probability = ProbabilityToScaleFactor(
                    Flags.Instance.ATP_ProbabilityPercent);
            }

            _trackEllipsoidProbability.Value = Math.Clamp(
                Flags.Instance.ATP_ProbabilityPercent, 1, 99);

            _trackEllipsoidDiskThickness.Value = (int)(Flags.Instance.EllipsoidDiskThickness * 100);
            _trackEllipsoidBaseRadius.Value = (int)(Flags.Instance.EllipsoidBaseRadius * 100);


            UpdateLabels();
            _isUpdating = false;
        }

        private void UpdateLabels()
        {
            // ── Rozmiary kulek ──
            _labelNonMetalValue.Text = (_trackNonMetal.Value / 100f).ToString("F2");
            _labelMetalValue.Text = (_trackMetal.Value / 100f).ToString("F2");
            _labelHydrogenValue.Text = $"{_trackHydrogen.Value}%";

            // ── Stick thickness ──
            _labelStickRadiusValue.Text = (_trackStickRadius.Value / 100f).ToString("F2");
            _labelStickFactorValue.Text = (_trackStickFactor.Value / 10000f).ToString("F4");

            // ── Parametry elipsoid ──
            // ⭐ Show procent + wyliczony scale factor
            int percent = _trackEllipsoidProbability.Value;
            float scale = ProbabilityToScaleFactor(percent);
            _labelEllipsoidProbabilityValue.Text = $"{percent}%  (×{scale:F3})";

            _labelEllipsoidDiskThicknessValue.Text = (_trackEllipsoidDiskThickness.Value / 100f).ToString("F2");
            _labelEllipsoidBaseRadiusValue.Text = (_trackEllipsoidBaseRadius.Value / 100f).ToString("F2");
        }

        /// <summary>
        /// Placeholder for a future preview — currently unused.
        /// </summary>
        private void UpdatePreview()
        {
            // Optional preview — e.g. change panel colour or icon
        }

        /// <summary>
        /// Shared refresh method after any parameter change
        /// size / geometry / elipsoid.
        ///
        /// VisualRevision is inkrementowany w setterze ATP_ProbabilityPercent
        /// (via SetAtpProbability), so Progressive AA will reset
        /// automatycznie.
        /// </summary>
        private void RefreshScene()
        {
            if (_isUpdating) return;

            var mainForm = MainForm.Instance;
            if (mainForm == null) return;
            Flags.Instance.VisualRevision++;
            mainForm.RefreshAllDocuments();
            mainForm.RefreshMorphologyDocuments();
        }

        private void ApplyChanges()
        {
            // ⭐ Save do JSON
            Flags.Instance.Save();

            // ⭐ Refresh all 3D documents
            var mainForm = MainForm.Instance;
            if (mainForm != null)
            {
                Flags.Instance.VisualRevision++;
                mainForm.RefreshAllDocuments();
                mainForm.RefreshMorphologyDocuments();

                mainForm.SetStatus("Atom display settings applied",
                    $"Structures: {StructureStorage.Instance.StructureCount}", "🎨");
            }
        }

        #endregion
    }
}