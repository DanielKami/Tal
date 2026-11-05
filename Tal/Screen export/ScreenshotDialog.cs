// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Screen capture / exafterrt: ScreenshotDialog.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System;
using System.Windows.Forms;

namespace Tal.Forms
{
 /// <summary>
 /// Okno dialogowe wyboru setień screenshota.
 /// Użytkownik wybiera mnożnik (1×, 2×, 3×, 4×, 6×, 8×) and zavalue (scena / full klatka).
 /// </summary>
    public partial class ScreenshotDialog : Form
    {
        #region Constants

 /// <summary>Maksymalny size afterjedynczego wymiaru — spójny z SaveScreenshotScaled.</summary>
        private const int MAX_DIMENSION = 8192;

 /// <summary>
 /// Dostępne mnożniki — order MUSI odafterwiadać indexom w cmbScale.
 /// </summary>
        private static readonly float[] SCALE_VALUES = { 1.0f, 2.0f, 3.0f, 4.0f, 6.0f, 8.0f };

        private static readonly string[] SCALE_LABELS =
        {
            "1×   (original)",
            "2×   (HD)",
            "3×",
            "4×   (4K)",
            "6×",
            "8×   (max)"
        };

        #endregion

        #region Private Fields

        private readonly int _baseWidth;
        private readonly int _baseHeight;

        #endregion

        #region Public Properties

 /// <summary>Wybrany mnożnik (1.0, 2.0, 3.0, 4.0, 6.0, 8.0).</summary>
        public float Scale { get; private set; } = 2.0f;

 /// <summary>Czy zapisać pełną klatkę (z etykietami i skalą colourów)?</summary>
        public bool FullFrame { get; private set; } = true;

 /// <summary>Czy włączyć supersampling?</summary>
        public bool Supersample { get; private set; } = false;

 /// <summary>Szerokość obrazu resultowego w pixelach.</summary>
        public int ResultWidth { get; private set; }

 /// <summary>Wysokość obrazu resultowego w pixelach.</summary>
        public int ResultHeight { get; private set; }

        #endregion

        #region Constructor

 /// <summary>
 /// Inicjalizuje new okno dialogowe.
 /// </summary>
 /// <param name="baseWidth">Szerokość bazowa (current viewafterrt, np. 1200).</param>
 /// <param name="baseHeight">Wysokość bazowa (current viewafterrt, np. 800).</param>
        public ScreenshotDialog(int baseWidth, int baseHeight)
        {
            _baseWidth = Math.Max(1, baseWidth);
            _baseHeight = Math.Max(1, baseHeight);

            InitializeComponent();

            InitializeComboItems();
            WireEvents();

 // Domyślnie 2×
            cmbScale.SelectedIndex = 1;
            UpdateResolutionLabel();
        }

        #endregion

        #region Initialization

        private void InitializeComboItems()
        {
            cmbScale.Items.Clear();
            foreach (var label in SCALE_LABELS)
                cmbScale.Items.Add(label);
        }

        private void WireEvents()
        {
            cmbScale.SelectedIndexChanged += (s, e) => UpdateResolutionLabel();

            btnSave.Click += (s, e) => CommitSelection();
        }

        #endregion

        #region Logic

 /// <summary>
 /// Odczytuje current stan kontrolek i zapisuje w properties.
 /// Called przy kliknięciu "Save...".
 /// </summary>
        private void CommitSelection()
        {
            int idx = cmbScale.SelectedIndex;
            if (idx < 0 || idx >= SCALE_VALUES.Length) idx = 1;

            Scale = SCALE_VALUES[idx];
            FullFrame = radioFull.Checked;
            Supersample = chkSupersample.Checked;

 // Finalna rozdzielczość (z uwzględnieniem limitu)
            int w = (int)MathF.Round(_baseWidth * Scale);
            int h = (int)MathF.Round(_baseHeight * Scale);

            if (w > MAX_DIMENSION) w = MAX_DIMENSION;
            if (h > MAX_DIMENSION) h = MAX_DIMENSION;

            ResultWidth = w;
            ResultHeight = h;
        }

 /// <summary>
 /// Updates etykietę z resultową rozdzielczością.
 /// </summary>
        private void UpdateResolutionLabel()
        {
            int idx = cmbScale.SelectedIndex;
            if (idx < 0 || idx >= SCALE_VALUES.Length) idx = 1;

            float scale = SCALE_VALUES[idx];

            int w = (int)MathF.Round(_baseWidth * scale);
            int h = (int)MathF.Round(_baseHeight * scale);

 // Limit
            bool clamped = false;
            if (w > MAX_DIMENSION) { w = MAX_DIMENSION; clamped = true; }
            if (h > MAX_DIMENSION) { h = MAX_DIMENSION; clamped = true; }

            lblResolution.Text = clamped
                ? $"→ {w} × {h} px  (max)"
                : $"→ {w} × {h} px";
        }

        #endregion

        #region Static Helper

 /// <summary>
 /// Afterkazuje okno dialogowe i returns result. Wygodny jednolinijkowiec.
 /// </summary>
 /// <param name="owner">Okno właściciela (np. MainForm).</param>
 /// <param name="baseWidth">Szerokość bazowa (current backbuffer, np. 1200).</param>
 /// <param name="baseHeight">Wysokość bazowa (current backbuffer, np. 800).</param>
 /// <param name="scale">Wyjście: wybrany mnożnik.</param>
 /// <param name="fullFrame">Wyjście: full klatka vs sama scena 3D.</param>
 /// <param name="supersample">Wyjście: czy włączony supersampling.</param>
 /// <returns>true if użytkownik kliknął "Save...", false if anulował.</returns>
        public static bool ShowSettings(IWin32Window owner,
                                        int baseWidth, int baseHeight,
                                        out float scale, out bool fullFrame,
                                        out bool supersample)
        {
            using var dlg = new ScreenshotDialog(baseWidth, baseHeight);

            if (dlg.ShowDialog(owner) != DialogResult.OK)
            {
                scale = 2.0f;
                fullFrame = true;
                supersample = false;
                return false;
            }

            scale = dlg.Scale;
            fullFrame = dlg.FullFrame;
            supersample = dlg.Supersample;
            return true;
        }

        #endregion
    }
}