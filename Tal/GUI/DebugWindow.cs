// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Debug / log output window.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.Forms;
using Tal.Utils;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.GUI
{
    public partial class DebugWindow : DockContent
    {
        #region Constructor

        public DebugWindow()
        {
            InitializeComponent();
            ProgressReporter.RegisterDebugWindow(this);

            DockWindowConfig.SetupWindow(this, "🐞 Debug - Symmetry");
        }

        #endregion

        #region Event Handlers — Buttons

        private void BtnClear_Click(object sender, EventArgs e)
        {
            _rtbLog.Clear();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            RefreshDebugInfo();
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog();
            sfd.Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*";
            sfd.FileName = $"DebugLog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, _rtbLog.Text);
                    MessageBox.Show($"Saved to:\n{sfd.FileName}", "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Save error: {ex.Message}", "Export",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion

        #region Debug Info Refresh

        public void RefreshDebugInfo()
        {
            _rtbLog.Clear();

            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm == null)
            {
                AppendText("❌ MainForm not found!", Color.Red);
                return;
            }

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel == null)
            {
                AppendText("❌ DockPanel not found!", Color.Red);
                return;
            }

            bool foundStructure = false;

            foreach (var doc in dockPanel.Documents)
            {
                if (doc is MonoGameDocument monoDoc)
                {
                    var molecule = monoDoc.GetMolecule();
                    if (molecule == null) continue;

                    foundStructure = true;

                    AppendHeader($"=== {monoDoc.Text} ===", Color.Cyan);

                    // ⭐ INFORMACJE O SYMETRII
                    AppendHeader("📐 Cell Parameters:", Color.Yellow);
                    AppendText($"  a = {molecule.a:F4} Å");
                    AppendText($"  b = {molecule.b:F4} Å");
                    AppendText($"  c = {molecule.c:F4} Å");
                    AppendText($"  α = {MathHelperTal.ToDegrees(molecule.alpha):F2}°");
                    AppendText($"  β = {MathHelperTal.ToDegrees(molecule.beta):F2}°");
                    AppendText($"  γ = {MathHelperTal.ToDegrees(molecule.gamma):F2}°");
                    AppendText("");

                    // ⭐ OPERACJE SYMETRII
                    AppendHeader($"🔁 Symmetry Operations: {molecule.SymmetryOperations.Count}", Color.Yellow);

                    if (molecule.SymmetryOperations.Count > 0)
                    {
                        for (int i = 0; i < molecule.SymmetryOperations.Count; i++)
                        {
                            var op = molecule.SymmetryOperations[i];
                            string opString = $"{op.sign1[0]}*{op.pos1[0]}+{op.sign2[0]}*{op.pos2[0]}+{op.shift[0]:F3}, " +
                                             $"{op.sign1[1]}*{op.pos1[1]}+{op.sign2[1]}*{op.pos2[1]}+{op.shift[1]:F3}, " +
                                             $"{op.sign1[2]}*{op.pos1[2]}+{op.sign2[2]}*{op.pos2[2]}+{op.shift[2]:F3}";

                            bool isIdentity = op.sign1[0] == 1 && op.pos1[0] == 0 &&
                                              op.sign1[1] == 1 && op.pos1[1] == 1 &&
                                              op.sign1[2] == 1 && op.pos1[2] == 2 &&
                                              op.sign2[0] == 0 && op.sign2[1] == 0 && op.sign2[2] == 0 &&
                                              Math.Abs(op.shift[0]) < 0.001f &&
                                              Math.Abs(op.shift[1]) < 0.001f &&
                                              Math.Abs(op.shift[2]) < 0.001f;

                            Color color = isIdentity ? Color.LightGreen : Color.White;
                            AppendText($"  [{i}] {opString}", color);
                        }
                    }
                    else
                    {
                        AppendText("  ⚠️ No symmetry operations found!", Color.Orange);
                    }

                    // ⭐ ATOMY
                    AppendHeader($"📦 Atoms: {molecule.NumberOfAtoms}", Color.Yellow);
                    AppendText($"  Asymmetric: {molecule.AsymmetricPart.Count}");
                    AppendText($"  Symmetry equivalents: {molecule.SymetryEquivalents.Count}");
                    AppendText("");

                    // ⭐ FIRST 5 ATOMS FROM ASYMMETRIC
                    AppendHeader("  First 5 asymmetric atoms:", Color.LightGray);
                    int count = Math.Min(5, molecule.AsymmetricPart.Count);
                    for (int i = 0; i < count; i++)
                    {
                        var atom = molecule.AsymmetricPart[i];
                        AppendText($"    [{i}] {atom.name} ({atom.AtomicNumber}) " +
                                  $"({atom.x:F4}, {atom.y:F4}, {atom.z:F4}) " +
                                  $"occ={atom.occupancy:F4}", Color.LightBlue);
                    }
                    if (molecule.AsymmetricPart.Count > 5)
                    {
                        AppendText($"    ... and {molecule.AsymmetricPart.Count - 5} more", Color.Gray);
                    }

                    AppendText("");
                    AppendSeparator();
                }
            }

            if (!foundStructure)
            {
                AppendText("⚠️ No structure loaded in any document!", Color.Orange);
                AppendText("Open a CIF or RES file first.");
            }

            // Auto scroll
            if (_chkAutoScroll.Checked)
            {
                _rtbLog.SelectionStart = _rtbLog.Text.Length;
                _rtbLog.ScrollToCaret();
            }
        }

        #endregion

        #region Append Helpers

        private void AppendHeader(string text, Color color)
        {
            AppendText(text, color);
        }

        private void AppendText(string text, Color? color = null)
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (_rtbLog == null || _rtbLog.IsDisposed) return;

            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => AppendText(text, color))); }
                catch { }
                return;
            }

            if (color.HasValue)
            {
                _rtbLog.SelectionStart = _rtbLog.TextLength;
                _rtbLog.SelectionLength = 0;
                _rtbLog.SelectionColor = color.Value;
                _rtbLog.AppendText(text + "\n");
                _rtbLog.SelectionColor = _rtbLog.ForeColor;
            }
            else
            {
                _rtbLog.AppendText(text + "\n");
            }
        }

        private void AppendSeparator()
        {
            AppendText("────────────────────────────────────────────", Color.Gray);
        }

        #endregion

        #region Public API — Log / Clear

 

        /// <summary>
        /// Thread-safe log — may be called from any thread.
        /// </summary>
        public void Log(string message, Color? color = null)
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (_rtbLog == null || _rtbLog.IsDisposed) return;

            // ⭐ Marshall na UI thread if need to
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => Log(message, color)));
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                return;
            }

            // We are on the UI thread
            if (color.HasValue)
            {
                _rtbLog.SelectionStart = _rtbLog.TextLength;
                _rtbLog.SelectionLength = 0;
                _rtbLog.SelectionColor = color.Value;
                _rtbLog.AppendText(message + "\n");
                _rtbLog.SelectionColor = _rtbLog.ForeColor;
            }
            else
            {
                _rtbLog.AppendText(message + "\n");
            }

            if (_chkAutoScroll != null && _chkAutoScroll.Checked)
            {
                _rtbLog.SelectionStart = _rtbLog.Text.Length;
                _rtbLog.ScrollToCaret();
            }
        }

        public void LogSymmetryOperations(string title, List<string> operations, string source)
        {
            Log($"🔁 {title}: {operations.Count} operations (source: {source})", Color.Yellow);

            if (operations.Count == 0)
            {
                Log("  ⚠️ No symmetry operations found!", Color.Orange);
                return;
            }

            for (int i = 0; i < Math.Min(operations.Count, 15); i++)
            {
                Log($"    [{i}] {operations[i]}", Color.LightGray);
            }

            if (operations.Count > 15)
            {
                Log($"    ... and {operations.Count - 15} more", Color.Gray);
            }
        }

        public new void Clear()
        {
            if (IsDisposed || !IsHandleCreated) return;
            if (_rtbLog == null || _rtbLog.IsDisposed) return;

            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(Clear)); }
                catch { }
                return;
            }

            _rtbLog.Clear();
        }

        #endregion
    }
}