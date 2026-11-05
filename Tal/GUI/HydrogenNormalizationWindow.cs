// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hydrogen bond-length normalisation window.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HydrogenNormalizationWindow — window GUI do normalizacji wodoru.
//
// ⭐ v2: Auto-refresh when the active molecule changes in MainForm.
//        Subscribes to MainForm.ActiveMoleculeChanged — when the user switches
//        tab or loads a new structure, the window automatically loads
//        a new molecule and rebuilds the table.
//        Event is unsubscribed in OnFormClosed (no leaks).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Tal.Calculate;
using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class HydrogenNormalizationWindow : DockContent
    {
        #region State

        private Molecule _molecule;
        private MonoGameDocument _document;
        private List<HydrogenNormalizer.HydrogenFix> _lastFixes;

        // ⭐ Track whether we are subscribed to the MainForm event
        private bool _subscribed = false;

        #endregion

        #region Constructor

        public HydrogenNormalizationWindow()
        {
            InitializeComponent();

            Text = "Hydrogen Normalization";
            DockAreas = DockAreas.Document | DockAreas.Float
                       | DockAreas.DockBottom | DockAreas.DockLeft | DockAreas.DockRight;

            _chkNormalize.CheckedChanged += (s, e) => RebuildTable();
            _chkSkipBridging.CheckedChanged += (s, e) => RebuildTable();

            _btnApply.Click += (s, e) => Apply();
            _btnReset.Click += (s, e) => Reset();
            _btnClose.Click += (s, e) => Close();

            _chkNormalize.Checked = true;
            _chkSkipBridging.Checked = true;

            // ⭐ Subscribe to MainForm event — auto-refresh on molecule change
            SubscribeToMainFormEvents();

            // ⭐ When the window is shown for the first time — try to load
            //    the active molecule (if any)
            this.Activated += (s, e) =>
            {
                // If we do not have a molecule yet — take the active one
                if (_molecule == null)
                {
                    TryLoadActiveMolecule();
                }
            };

            BuildBondTable();
            SetStatus("Select a molecule to begin.", Color.Gray);
        }

        #endregion

        #region MainForm Event Subscription

        /// <summary>
        /// Subskrybuje event ActiveMoleculeChanged in MainForm.
        /// Idempotent — safe to call repeatedly.
        /// </summary>
        private void SubscribeToMainFormEvents()
        {
            if (_subscribed) return;

            var mainForm = MainForm.Instance;
            if (mainForm == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[HydrogenNormalizationWindow] MainForm.Instance == null — " +
                    "subskrypcja odroczona do Activated");
                return;
            }

            mainForm.ActiveMoleculeChanged += OnActiveMoleculeChanged;
            _subscribed = true;

            System.Diagnostics.Debug.WriteLine(
                "[HydrogenNormalizationWindow] Subscribed to ActiveMoleculeChanged");
        }

        /// <summary>
        /// Unsubscribes the ActiveMoleculeChanged event from MainForm.
        /// Called w OnFormClosed.
        /// </summary>
        private void UnsubscribeFromMainFormEvents()
        {
            if (!_subscribed) return;

            var mainForm = MainForm.Instance;
            if (mainForm == null)
            {
                _subscribed = false;
                return;
            }

            try
            {
                mainForm.ActiveMoleculeChanged -= OnActiveMoleculeChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[HydrogenNormalizationWindow] Unsubscribe error: {ex.Message}");
            }

            _subscribed = false;

            System.Diagnostics.Debug.WriteLine(
                "[HydrogenNormalizationWindow] Unsubscribed from ActiveMoleculeChanged");
        }

        /// <summary>
        /// ⭐ Called when MainForm loads a new molecule or the user
        ///    switches the tab between documents.
        ///
        /// Auto-refreshes the table with the new molecule.
        /// </summary>
        private void OnActiveMoleculeChanged(Molecule molecule, MonoGameDocument doc)
        {
            if (IsDisposed || !IsHandleCreated) return;

            // ⭐ Call may come from another thread — guard
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnActiveMoleculeChanged(molecule, doc)));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                return;
            }

            // ⭐ Load new molecule (or clear when null)
            LoadMolecule(molecule, doc);

            System.Diagnostics.Debug.WriteLine(
                $"[HydrogenNormalizationWindow] Auto-refreshed for " +
                $"'{molecule?.Name ?? "null"}' " +
                $"({molecule?.NumberOfAtoms ?? 0} atoms)");
        }

        /// <summary>
        /// Safe attempt to load the active molecule from MainForm.
        /// Used w Activated when not we have still molecule.
        /// </summary>
        private void TryLoadActiveMolecule()
        {
            var mainForm = MainForm.Instance;
            if (mainForm == null) return;

            var molecule = mainForm.GetActiveMolecule();
            var doc = mainForm.GetActiveDocument();

            if (molecule != null && molecule.NumberOfAtoms > 0)
            {
                LoadMolecule(molecule, doc);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Loads the molecule into the window and rebuilds the table.
        /// Safe for null — clears the table.
        /// </summary>
        public void LoadMolecule(Molecule molecule, MonoGameDocument document)
        {
            _molecule = molecule;
            _document = document;
            _lastFixes = null;

            _dgvBonds.Rows.Clear();

            if (molecule == null)
            {
                _lblMoleculeInfo.Text = "Molecule: —";
                SetStatus("No molecule loaded.", Color.Orange);
                UpdateButtonStates();
                return;
            }

            _lblMoleculeInfo.Text =
                $"Molecule: {molecule.Name}\n" +
                $"Atoms: {molecule.NumberOfAtoms}, " +
                $"Hydrogens: {CountHydrogens(molecule)}";

            RebuildTable();
        }

        /// <summary>
        /// ⭐ Reload the current molecule (without changing the reference).
        /// Used when H were changed via another module
        /// (np. Hirshfeld auto-normalizacja).
        /// </summary>
        public void ReloadCurrentMolecule()
        {
            if (_molecule == null) return;

            LoadMolecule(_molecule, _document);
        }

        #endregion

        #region UI Helpers

        private void BuildBondTable()
        {
            _dgvBonds.Columns.Clear();
            _dgvBonds.Columns.Add("hName", "H");
            _dgvBonds.Columns.Add("heavy", "Heavy");
            _dgvBonds.Columns.Add("heavyZ", "Z");
            _dgvBonds.Columns.Add("orig", "XRD (Å)");
            _dgvBonds.Columns.Add("target", "Neutron (Å)");
            _dgvBonds.Columns.Add("delta", "Δ (Å)");
            _dgvBonds.Columns.Add("status", "Status");

            _dgvBonds.Columns["hName"].Width = 60;
            _dgvBonds.Columns["heavy"].Width = 60;
            _dgvBonds.Columns["heavyZ"].Width = 40;
            _dgvBonds.Columns["orig"].Width = 70;
            _dgvBonds.Columns["target"].Width = 80;
            _dgvBonds.Columns["delta"].Width = 70;
            _dgvBonds.Columns["status"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            _dgvBonds.AllowUserToAddRows = false;
            _dgvBonds.AllowUserToDeleteRows = false;
            _dgvBonds.ReadOnly = true;
            _dgvBonds.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _dgvBonds.MultiSelect = false;
            _dgvBonds.RowHeadersVisible = false;
        }

        private void UpdateButtonStates()
        {
            bool hasMolecule = _molecule != null;
            bool hasResult = _lastFixes != null && _lastFixes.Count > 0;
            bool hasCache = hasMolecule && HydrogenNormalizer.HasOriginalPositions(_molecule);

            _btnApply.Enabled = hasMolecule && hasResult &&
                                _lastFixes.Any(f => !f.Skipped);
            _btnReset.Enabled = hasCache;
        }

        private void SetStatus(string text, Color color)
        {
            _lblStatus.Text = text;
            _lblStatus.ForeColor = color;
        }

        private static int CountHydrogens(Molecule molecule)
        {
            if (molecule?.atom == null) return 0;
            int c = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
                if (molecule.atom[i].AtomicNumber == 1) c++;
            return c;
        }

        #endregion

        #region Rebuild Table

        private void RebuildTable()
        {
            if (_molecule == null)
            {
                _lastFixes = null;
                _dgvBonds.Rows.Clear();
                _rtbLog.Clear();
                UpdateButtonStates();
                return;
            }

            _rtbLog.Clear();

            var fixes = HydrogenNormalizer.Check(
                _molecule,
                cutoffHeavy: 1.3f,
                skipBridging: _chkSkipBridging.Checked);

            _lastFixes = fixes;

            PopulateTable(fixes);
            WriteLog(fixes);

            int toFix = fixes.Count(f => !f.Skipped);
            int skipped = fixes.Count(f => f.Skipped);

            bool hasCache = HydrogenNormalizer.HasOriginalPositions(_molecule);

            if (hasCache && _molecule.HydrogenNormalizationApplied)
            {
                SetStatus($"Applied. {toFix} more to fix, {skipped} skipped.",
                          Color.DarkGreen);
            }
            else if (toFix == 0 && skipped > 0)
            {
                SetStatus($"All hydrogens already OK ({skipped} skipped).",
                          Color.Gray);
            }
            else
            {
                SetStatus($"{toFix} hydrogen(s) to normalize, {skipped} skipped.",
                          toFix > 0 ? Color.Black : Color.Gray);
            }

            UpdateButtonStates();
        }

        #endregion

        #region Actions

        private void Apply()
        {
            if (_molecule == null || _lastFixes == null)
            {
                SetStatus("No molecule loaded.", Color.Orange);
                return;
            }

            int toFix = _lastFixes.Count(f => !f.Skipped);
            if (toFix == 0)
            {
                SetStatus("Nothing to normalize.", Color.Gray);
                return;
            }

            var confirm = MessageBox.Show(
                $"Normalize {toFix} hydrogen atoms?\n\n" +
                $"This modifies the current molecule in memory.\n" +
                $"You can Reset to original positions at any time.",
                "Hydrogen Normalization",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.OK) return;

            // ⭐ Normalizacja — modyfikuje AsymmetricPart
            HydrogenNormalizer.Normalize(
                _molecule,
                cutoffHeavy: 1.3f,
                skipBridging: _chkSkipBridging.Checked);

            // ⭐ Flagi
            _molecule.HydrogenNormalizationApplied = true;
            _molecule.HydrogenNormalizationDisabledByUser = false;

            // ⭐ Rebuild scene 3D z nowego AsymmetricPart
            _molecule.RebuildFromAsymmetricPart();

            // ⭐ Refresh all documents + reset Progressive AA
            if (MainForm.Instance != null)
            {
                MainForm.Instance.InvalidateScene3D();
            }

            // ⭐ Rebuild table
            RebuildTable();
        }

        private void Reset()
        {
            if (_molecule == null)
            {
                SetStatus("No molecule loaded.", Color.Orange);
                return;
            }

            bool ok = HydrogenNormalizer.ResetToOriginal(_molecule);

            if (ok)
            {
                // ⭐ Rebuild 3D scene from restored AsymmetricPart
                _molecule.RebuildFromAsymmetricPart();

                // ⭐ Refresh all documents + reset Progressive AA
                if (MainForm.Instance != null)
                {
                    MainForm.Instance.InvalidateScene3D();
                }

                // ⭐ Rebuild table
                RebuildTable();
            }
            else
            {
                SetStatus("Nothing to reset (no cached originals).", Color.Gray);
            }

            UpdateButtonStates();
        }

        #endregion

        #region Table + Log

        private void PopulateTable(List<HydrogenNormalizer.HydrogenFix> fixes)
        {
            _dgvBonds.Rows.Clear();

            if (fixes == null) return;

            foreach (var f in fixes)
            {
                int idx = _dgvBonds.Rows.Add();
                var row = _dgvBonds.Rows[idx];

                row.Cells["hName"].Value = f.HydrogenName;
                row.Cells["heavy"].Value = f.HeavyName;
                row.Cells["heavyZ"].Value = f.HeavyZ > 0 ? f.HeavyZ.ToString() : "—";

                if (f.Skipped)
                {
                    row.Cells["orig"].Value = f.OriginalLength > 0
                        ? $"{f.OriginalLength:F3}" : "—";
                    row.Cells["target"].Value = "—";
                    row.Cells["delta"].Value = "—";
                    row.Cells["status"].Value = f.SkipReason ?? "skipped";

                    row.DefaultCellStyle.ForeColor = Color.Gray;
                }
                else
                {
                    row.Cells["orig"].Value = $"{f.OriginalLength:F3}";
                    row.Cells["target"].Value = $"{f.TargetLength:F3}";
                    row.Cells["delta"].Value = $"{f.Delta:+0.000;-0.000;0.000}";
                    row.Cells["status"].Value = "OK";

                    row.DefaultCellStyle.ForeColor = Color.DarkGreen;
                }
            }
        }

        private void WriteLog(List<HydrogenNormalizer.HydrogenFix> fixes)
        {
            _rtbLog.Clear();

            if (_molecule == null || fixes == null) return;

            var lines = HydrogenNormalizer.FormatFixes(_molecule, fixes);

            foreach (var line in lines)
            {
                Color color = Color.Black;

                if (line.Contains("═══════"))
                    color = Color.FromArgb(0, 100, 200);
                else if (line.Contains("→"))
                    color = Color.DarkGreen;
                else if (line.Contains("—") && (line.Contains("skipped") ||
                                                 line.Contains("bridging") ||
                                                 line.Contains("no heavy") ||
                                                 line.Contains("already normalized")))
                    color = Color.Gray;

                AppendColored(line, color);
            }
        }

        private void AppendColored(string text, Color color)
        {
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.SelectionLength = 0;
            _rtbLog.SelectionColor = color;
            _rtbLog.AppendText(text + "\n");
            _rtbLog.SelectionColor = _rtbLog.ForeColor;
        }

        #endregion

        #region Form Lifecycle

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }

        /// <summary>
        /// ⭐ Unsubscribe event MainForm.ActiveMoleculeChanged when closing windows.
        /// Without this MainForm keeps a reference to the closed window (memory leak).
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            UnsubscribeFromMainFormEvents();

            base.OnFormClosed(e);
        }

        #endregion
    }
}