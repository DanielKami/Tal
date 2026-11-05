// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal Calculations: MACE energy/forces only (no PEOE/LJ force arrows).
// Model medium/large + supercell 1³ or 3³ only. Background thread.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Tal.Calculate;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public class CrystalCalculationsWindow : DockContent
    {
        private readonly Label _lblModel;
        private readonly Label _lblEnergy;
        private readonly Label _lblLattice;
        private readonly Label _lblStatus;
        private readonly ProgressBar _progress;
        private readonly Label _lblModelWarning;
        private readonly DataGridView _grid;
        private readonly CheckBox _chkArrows;
        private readonly CheckBox _chkInter;
        private readonly NumericUpDown _numScale;
        private readonly Button _btnCalc;
        private readonly ComboBox _comboModel;
        private readonly RadioButton _rbSupercell1;
        private readonly RadioButton _rbSupercell3;
        private Molecule? _molecule;
        private bool _busy;

        private sealed class ModelItem
        {
            public string Label { get; }
            public string Value { get; }
            public ModelItem(string label, string value) { Label = label; Value = value; }
            public override string ToString() => Label;
        }

        private const string SettingsFileName = "tal_mace_settings.json";

        public CrystalCalculationsWindow()
        {
            Text = "Crystal Calculations";
            DockWindowConfig.SetupWindow(this, "⚡ Crystal Calculations");
            ClientSize = new System.Drawing.Size(368, 720);
            AutoScroll = true;

            int y = 8;

            _lblModel = new Label
            {
                AutoSize = false,
                Location = new System.Drawing.Point(8, y),
                Size = new System.Drawing.Size(352, 32),
                Text = "Model / evaluator: —"
            };
            Controls.Add(_lblModel);
            y += 36;

                        // ── Model MACE (ComboBox — single source of truth) ──
            var gbModel = new GroupBox
            {
                Text = "Model MACE",
                Location = new System.Drawing.Point(8, y),
                Size = new System.Drawing.Size(352, 88)
            };
            _comboModel = new ComboBox
            {
                Location = new System.Drawing.Point(12, 24),
                Size = new System.Drawing.Size(328, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            _comboModel.Items.Add(new ModelItem("MACE-OFF23 small (fast)", "off23-small"));
            _comboModel.Items.Add(new ModelItem("MACE-OFF23 medium (default)", "off23-medium"));
            _comboModel.Items.Add(new ModelItem("MACE-OFF23 large (highest accuracy)", "off23-large"));
            _comboModel.Items.Add(new ModelItem("MACE-MP-0 medium (materials)", "mp0-medium"));
            _comboModel.Items.Add(new ModelItem("MACE-MP-0 large (materials)", "mp0-large"));
            _comboModel.SelectedIndex = 1; // off23-medium
            _comboModel.SelectedIndexChanged += (s, e) =>
            {
                SaveSettings();
                UpdateWarning();
                UpdateModelLabel();
            };
            _lblModelWarning = new Label
            {
                Text = "",
                ForeColor = System.Drawing.Color.DarkOrange,
                Font = new Font("Segoe UI", 8.25f, FontStyle.Bold),
                AutoSize = false,
                Size = new System.Drawing.Size(328, 28),
                Visible = false,
                Location = new System.Drawing.Point(12, 54)
            };
            gbModel.Controls.Add(_comboModel);
            gbModel.Controls.Add(_lblModelWarning);
            Controls.Add(gbModel);
            y += 96;

// ── Supercell (only 1³ and 3³ — 2³ has no physical centre / incomplete shell)
            var gbSc = new GroupBox
            {
                Text = "Supercell",
                Location = new System.Drawing.Point(8, y),
                Size = new System.Drawing.Size(352, 78)
            };
            _rbSupercell1 = new RadioButton
            {
                Text = "1×1×1  (fast, incomplete environment)",
                Location = new System.Drawing.Point(12, 22),
                AutoSize = true
            };
            _rbSupercell3 = new RadioButton
            {
                Text = "3×3×3  (recommended — central cell)",
                Location = new System.Drawing.Point(12, 46),
                AutoSize = true,
                Checked = true
            };
            gbSc.Controls.Add(_rbSupercell1);
            gbSc.Controls.Add(_rbSupercell3);
            Controls.Add(gbSc);
            y += 86;

            _rbSupercell1.CheckedChanged += (s, e) => UpdateWarning();
            _rbSupercell3.CheckedChanged += (s, e) => UpdateWarning();

            // ── Calculate + arrows ──
            _btnCalc = new Button
            {
                Text = "Calculate",
                Location = new System.Drawing.Point(8, y),
                Size = new System.Drawing.Size(120, 30)
            };
            _btnCalc.Click += BtnCalculate_Click;
            Controls.Add(_btnCalc);

            _chkArrows = new CheckBox
            {
                Text = "Show MACE force arrows",
                Location = new System.Drawing.Point(140, y + 4),
                AutoSize = true,
                Checked = true
            };
            _chkArrows.CheckedChanged += (s, e) =>
            {
                Flags.Instance.ShowForces = _chkArrows.Checked;
                Flags.Instance.ShowForceTotal = _chkArrows.Checked;
                // Only MACE total — never approximate electro/disp/rep for display
                Flags.Instance.ShowForceElectro = false;
                Flags.Instance.ShowForceDispersion = false;
                Flags.Instance.ShowForceRepulsion = false;
                Flags.Instance.VisualRevision++;
                RefreshView();
            };
            Controls.Add(_chkArrows);
            y += 36;

            // Row: Arrow scale (own line — no overlap with intermolecular)
            var lblScale = new Label
            {
                Text = "Arrow scale",
                Location = new System.Drawing.Point(8, y + 3),
                AutoSize = true
            };
            Controls.Add(lblScale);
            _numScale = new NumericUpDown
            {
                Location = new System.Drawing.Point(100, y),
                Size = new System.Drawing.Size(80, 24),
                DecimalPlaces = 2,
                Increment = 0.25m,
                Minimum = 0.05m,
                Maximum = 50m,
                Value = 1.0m
            };
            Flags.Instance.ForceArrowScale = (float)_numScale.Value;
            _numScale.ValueChanged += (s, e) =>
            {
                Flags.Instance.ForceArrowScale = (float)_numScale.Value;
                Flags.Instance.VisualRevision++;
                RefreshView();
            };
            Controls.Add(_numScale);
            var lblScaleHint = new Label
            {
                Text = "(length ∝ |F| × scale)",
                Location = new System.Drawing.Point(190, y + 3),
                AutoSize = true,
                ForeColor = System.Drawing.Color.Gray
            };
            Controls.Add(lblScaleHint);
            y += 32;

            _chkInter = new CheckBox
            {
                Text = "Show intermolecular arrows (molecule COM)",
                Location = new System.Drawing.Point(8, y),
                AutoSize = true,
                Checked = false
            };
            _chkInter.CheckedChanged += (s, e) =>
            {
                Flags.Instance.ShowIntermolecular = _chkInter.Checked;
                Flags.Instance.VisualRevision++;
                RefreshView();
            };
            Controls.Add(_chkInter);
            y += 32;

            Flags.Instance.ForceConePositionScale = 2;
            Flags.Instance.ShowForceTotal = true;
            Flags.Instance.ShowForceElectro = false;
            Flags.Instance.ShowForceDispersion = false;
            Flags.Instance.ShowForceRepulsion = false;
            Flags.Instance.ShowIntermolecular = false;
            Flags.Instance.ShowForces = true;

            _lblLattice = new Label
            {
                AutoSize = false,
                Location = new System.Drawing.Point(8, y),
                Size = new System.Drawing.Size(352, 40),
                Text = "Net force: —"
            };
            Controls.Add(_lblLattice);
            y += 44;

            _lblEnergy = new Label
            {
                AutoSize = false,
                Location = new System.Drawing.Point(8, y),
                Size = new System.Drawing.Size(352, 40),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Text = "Energy: —"
            };
            Controls.Add(_lblEnergy);
            y += 44;

            _grid = new DataGridView
            {
                Location = new System.Drawing.Point(8, y),
                Size = new System.Drawing.Size(352, 260),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            _grid.Columns.Add("Atom", "Atom");
            _grid.Columns.Add("Fx", "Fx");
            _grid.Columns.Add("Fy", "Fy");
            _grid.Columns.Add("Fz", "Fz");
            _grid.Columns.Add("F", "|F| MACE");
            Controls.Add(_grid);

            // Status at bottom, progress bar just above it
            _lblStatus = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Bottom,
                Height = 24,
                Text = "Ready."
            };
            Controls.Add(_lblStatus);

            _progress = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 18,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };
            Controls.Add(_progress);

            LoadSettings();
            UpdateWarning();
            UpdateModelLabel();
        }

        private void UpdateWarning()
        {
            string model = SelectedModel();
            int sc = SelectedSupercell();
            bool isLarge = model.EndsWith("-large", StringComparison.Ordinal);
            if (sc == 1)
            {
                _lblModelWarning.Text =
                    "1×1×1 — incomplete environment (no surrounding cells). Prefer 3×3×3.";
                _lblModelWarning.ForeColor = System.Drawing.Color.DarkOrange;
                _lblModelWarning.Visible = true;
            }
            else if (isLarge && sc >= 3)
            {
                _lblModelWarning.Text =
                    "Large + 3×3×3 needs substantial GPU VRAM (>8–12 GB). OOM risk on 6 GB cards.";
                _lblModelWarning.ForeColor = System.Drawing.Color.DarkRed;
                _lblModelWarning.Visible = true;
            }
            else
            {
                _lblModelWarning.Visible = false;
            }
            UpdateModelLabel();
        }

        private void UpdateModelLabel()
        {
            string m = SelectedModel();
            MaceCrystalAnalyzer.IsReady(m, out string status);
            _lblModel.Text = status + $"  |  supercell {SelectedSupercell()}×{SelectedSupercell()}×{SelectedSupercell()}";
        }

        private string SelectedModel()
        {
            if (_comboModel.SelectedItem is ModelItem mi && !string.IsNullOrEmpty(mi.Value))
                return mi.Value;
            return MaceCrystalAnalyzer.DefaultModel;
        }

        private void SelectModelInCombo(string value)
        {
            string v = PythonMaceEvaluator.NormalizeModelName(value, out _);
            for (int i = 0; i < _comboModel.Items.Count; i++)
            {
                if (_comboModel.Items[i] is ModelItem mi && mi.Value == v)
                {
                    _comboModel.SelectedIndex = i;
                    return;
                }
            }
            // default medium
            for (int i = 0; i < _comboModel.Items.Count; i++)
            {
                if (_comboModel.Items[i] is ModelItem mi && mi.Value == MaceCrystalAnalyzer.DefaultModel)
                {
                    _comboModel.SelectedIndex = i;
                    return;
                }
            }
        }

        private int SelectedSupercell()
            => _rbSupercell1.Checked ? 1 : 3;

        private void LoadSettings()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                if (!File.Exists(path)) return;
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;
                if (root.TryGetProperty("mace_model", out var m))
                    SelectModelInCombo(m.GetString() ?? MaceCrystalAnalyzer.DefaultModel);
                else
                    SelectModelInCombo(MaceCrystalAnalyzer.DefaultModel);
                if (root.TryGetProperty("mace_supercell", out var s))
                {
                    int n = s.GetInt32();
                    if (n == 1) _rbSupercell1.Checked = true;
                    else _rbSupercell3.Checked = true; // 3×3×3 default; 2×2×2 no longer offered
                }
            }
            catch { /* ignore */ }
        }

        private void SaveSettings()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                var json = JsonSerializer.Serialize(new
                {
                    mace_model = SelectedModel(),
                    mace_supercell = SelectedSupercell()
                });
                File.WriteAllText(path, json);
            }
            catch { /* ignore */ }
        }

        private void Ui(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }

        /// <summary>Update progress bar + status (thread-safe).</summary>
        private void SetProgress(int percent, string? status = null)
        {
            void apply()
            {
                if (IsDisposed) return;
                int v = Math.Clamp(percent, 0, 100);
                if (_progress.Style != ProgressBarStyle.Continuous)
                    _progress.Style = ProgressBarStyle.Continuous;
                _progress.Value = v;
                if (status != null)
                    _lblStatus.Text = status.Length > 120 ? status[..117] + "…" : status;
            }
            Ui(apply);
        }

        private void SetProgressMarquee(string? status = null)
        {
            void apply()
            {
                if (IsDisposed) return;
                _progress.Style = ProgressBarStyle.Marquee;
                _progress.MarqueeAnimationSpeed = 30;
                if (status != null)
                    _lblStatus.Text = status.Length > 120 ? status[..117] + "…" : status;
            }
            Ui(apply);
        }

        private void Debug(string message, System.Drawing.Color? color = null)
        {
            void write()
            {
                if (IsDisposed) return;
                _lblStatus.Text = message.Length > 120 ? message[..117] + "…" : message;
                var main = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                var dbg = main?.GetDebugWindow();
                if (dbg != null)
                {
                    if (!dbg.Visible)
                        main!.ShowToolWindow(dbg, "Debug");
                    dbg.Log(message, color ?? System.Drawing.Color.LightGray);
                }
            }
            Ui(write);
        }

        private async void BtnCalculate_Click(object? sender, EventArgs e)
        {
            if (_busy) return;

            var main = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            var doc = main?.GetActiveDocument();
            _molecule = doc?.GetMolecule();
            if (_molecule == null || _molecule.NumberOfAtoms == 0)
            {
                Debug("Crystal Calculations: no structure loaded.", System.Drawing.Color.Orange);
                return;
            }

            string modelName = SelectedModel();
            int supercellSize = SelectedSupercell();
            SaveSettings();
            Debug($"  UI selection: ComboBox → \"{modelName}\"",
                System.Drawing.Color.Cyan);

            string modelFile = MaceCrystalAnalyzer.ModelFileName(modelName);
            string modelFull = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, modelFile);
            if (!File.Exists(modelFull))
            {
                MessageBox.Show(this,
                    $"Model file not found for selected option \"{modelName}\":\n{modelFull}\n\n" +
                    "Place the matching .pt file next to TAL.exe.",
                    "Crystal Calculations",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PythonMaceEvaluator.ExeFileName);
            if (!File.Exists(exe))
            {
                MessageBox.Show(this,
                    $"{PythonMaceEvaluator.ExeFileName} not found next to TAL.exe.\nRebuild the worker with PyInstaller.",
                    "Crystal Calculations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var warn = MessageBox.Show(this,
                "Hydrogens will be moved to neutron X–H distances before the calculation.\n\n" +
                $"Model: {modelName}   Supercell: {supercellSize}×{supercellSize}×{supercellSize}\n" +
                "Only MACE total forces are used for arrows (no PEOE/LJ heuristics).",
                "Crystal Calculations",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning);
            if (warn != DialogResult.OK) return;

            _busy = true;
            _btnCalc.Enabled = false;
            _btnCalc.Text = "Working…";
            SetProgress(0, "Starting…");
            UpdateModelLabel();

            try
            {
                Debug("════════ MACE Crystal Calculations ════════", System.Drawing.Color.DeepSkyBlue);
                Debug($"  model={modelName}  supercell={supercellSize}³  Rcut={MaceCrystalAnalyzer.RCut}", System.Drawing.Color.Cyan);

                Debug("Preparing structure & calling worker (background)…", System.Drawing.Color.Cyan);
                var sw = Stopwatch.StartNew();

                // Progress from background stages (worker itself has no % callback → marquee while it runs)
                var progress = new Progress<(int pct, string msg)>(p => SetProgress(p.pct, p.msg));

                // All heavy work off the UI thread: H-normalize, full cell, neighbour graph, worker.
                // Molecule is still shared with the 3D view — avoid user edits during _busy.
                var prep = await Task.Run(() =>
                {
                    void Report(int pct, string msg)
                    {
                        try { ((IProgress<(int, string)>)progress).Report((pct, msg)); }
                        catch { /* UI disposed */ }
                    }

                    Report(5, "Normalizing hydrogens…");
                    HydrogenNormalizer.Normalize(_molecule!, cutoffHeavy: 1.3f, skipBridging: true);
                    _molecule!.HydrogenNormalizationApplied = true;
                    _molecule.RebuildFromAsymmetricPart();

                    Report(20, "Building full unit cell…");
                    int n = _molecule.EnsureFullUnitCellForCalculations();
                    System.Diagnostics.Debug.WriteLine($"[CrystalCalc] EnsureFullUnitCell → {n} atoms");
                    try
                    {
                        if (_molecule.SymetryEquivalents?.Count > 0)
                        {
                            Report(30, "Detecting molecules…");
                            int nMol = _molecule.DetectMoleculesInUnitCell(_molecule.SymetryEquivalents, true);
                            int nOpsLocal = _molecule.SymmetryOperations?.Count ?? 1;
                            if (nMol < 2 && nOpsLocal > 1)
                                nMol = _molecule.AssignMoleculeIdsBySymmetryOp(_molecule.SymetryEquivalents);
                            _molecule.PropagateMoleculeIdsToAsymmetricPart();
                            System.Diagnostics.Debug.WriteLine($"[CrystalCalc] molecules in cell = {nMol}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("[CrystalCalc] molecule detect: " + ex.Message);
                    }

                    Report(40, "Building supercell & neighbour list…");
                    var geom = MaceCrystalAnalyzer.FromMolecule(_molecule);

                    // Worker has no granular progress — show marquee for the long GPU/CPU phase
                    Report(50, "Running MACE worker (GPU/CPU)…");
                    try
                    {
                        // Switch to marquee on UI for indeterminate worker time
                        if (!IsDisposed)
                        {
                            try { BeginInvoke(new Action(() => SetProgressMarquee("Running MACE worker (GPU/CPU)…"))); }
                            catch { }
                        }
                    }
                    catch { /* ignore */ }

                    var eval = MaceCrystalAnalyzer.Evaluate(
                        geom.z, geom.coords, geom.cell, modelName, supercellSize, geom.moleculeIds);

                    Report(90, "Worker finished — mapping forces…");
                    return (geom, eval);
                });
                sw.Stop();

                int[] z = prep.geom.z;
                float[] coords = prep.geom.coords;
                float[,] cell = prep.geom.cell;
                int[] indices = prep.geom.atomIndices;
                int[] molIds = prep.geom.moleculeIds;
                MaceCrystalAnalyzer.Result result = prep.eval;

                SetProgress(92, "Updating view & logs…");
                LogMoleculeIds();
                int nMolCell = molIds.Where(id => id >= 0).Distinct().Count();
                int nOps = _molecule.SymmetryOperations?.Count ?? 0;
                int nAsym = _molecule.AsymmetricPart?.Count ?? 0;
                Debug(
                    $"MACE unit cell: N={z.Length} (= SymetryEquivalents after full ops + dedupe; " +
                    $"AsymPart={nAsym}, Ops={nOps}; display mode ignored)",
                    System.Drawing.Color.Cyan);
                Debug($"  molecules in cell = {nMolCell}", System.Drawing.Color.Cyan);
                Debug(
                    $"  molecule_ids unique=[{string.Join(",", molIds.Where(id => id >= 0).Distinct().OrderBy(x => x))}]",
                    System.Drawing.Color.Cyan);
                if (nMolCell < 2)
                    Debug("  WARNING: only 1 molecule ID — intermolecular arrows will be empty",
                        System.Drawing.Color.Orange);
                Debug($"Finished in {sw.Elapsed.TotalSeconds:F2} s", System.Drawing.Color.Cyan);

                // Most reliable signal: stderr lines from the worker process
                if (!string.IsNullOrWhiteSpace(result.WorkerStderr))
                {
                    Debug("─── worker stderr ───", System.Drawing.Color.DeepSkyBlue);
                    foreach (var raw in result.WorkerStderr.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string line = raw.Trim();
                        if (line.Length == 0) continue;
                        bool isCuda = line.Contains("device=cuda", StringComparison.OrdinalIgnoreCase)
                                      || line.Contains("tensor_device=cuda", StringComparison.OrdinalIgnoreCase);
                        bool isCpu = line.Contains("device=cpu", StringComparison.OrdinalIgnoreCase)
                                     || line.Contains("tensor_device=cpu", StringComparison.OrdinalIgnoreCase);
                        var col = isCuda ? System.Drawing.Color.LightGreen
                                : isCpu ? System.Drawing.Color.Orange
                                : System.Drawing.Color.LightGray;
                        Debug("  " + line, col);
                    }
                }

                Debug(
                    $"[C#] użyto: requested={result.ModelName} used={result.ModelUsed} " +
                    $"params={result.ModelParams} file={result.ModelFile} fallback={result.FallbackUsed}",
                    System.Drawing.Color.Cyan);
                if (result.FallbackUsed)
                    Debug("  UWAGA: " + (string.IsNullOrEmpty(result.ModelWarning)
                        ? "worker użył innego modelu niż wybrany"
                        : result.ModelWarning), System.Drawing.Color.Orange);

                bool onCuda = result.DeviceName.Contains("cuda", StringComparison.OrdinalIgnoreCase);
                Debug($"  WORKER device = {result.DeviceName}  ← {(onCuda ? "GPU" : "CPU")}",
                    onCuda ? System.Drawing.Color.LightGreen : System.Drawing.Color.Orange);
                if (!string.IsNullOrWhiteSpace(result.DeviceReport))
                    Debug($"  GPU report: {result.DeviceReport}",
                        onCuda ? System.Drawing.Color.LightGreen : System.Drawing.Color.Orange);
                Debug($"  Molecules in cell (TAL) = {result.MoleculeCountInCell}", System.Drawing.Color.Cyan);

                // Paint MACE forces — ALWAYS via SymetryEquivalents order (= CentralCellForces)
                if (_molecule.SymetryEquivalents == null || _molecule.SymetryEquivalents.Count == 0)
                    throw new InvalidOperationException(
                        "SymetryEquivalents empty — cannot map MACE forces to atoms.");
                if (_molecule.SymetryEquivalents.Count != result.CentralCellForces.GetLength(0))
                    throw new InvalidOperationException(
                        $"Force count ({result.CentralCellForces.GetLength(0)}) != SymetryEquivalents " +
                        $"({_molecule.SymetryEquivalents.Count}).");

                MaceForceCache.StoreFromSymmetryEquivalents(
                    _molecule, result.CentralCellForces,
                    result.EnergyPerUnitCell, result.TotalSupercellEnergy);

                int applied = MaceForceCache.ApplyToMolecule(_molecule);
                Debug($"  Forces applied to {applied} atoms (by Cart+min-image)", System.Drawing.Color.Cyan);

                StampMoleculeIdsOntoDisplayAtoms(_molecule, molIds);
                LogForceTableMace(result);
                VerifyMappedForces(_molecule, result);

                // Group MACE forces by TAL Molecule ID → intermolecular arrows (COM→COM)
                BuildIntermolecularFromMace(
                    _molecule, result.CentralCellForces, molIds, indices);
                LogIntermolecular();

                // Net F from CentralCellForces (same source as SUM in log)
                var (net, fa, fb, fc) = ForceDecomposition.LatticeComponentsFromForces(
                    result.CentralCellForces, _molecule);
                _lblEnergy.Text =
                    $"E / cell = {result.EnergyPerUnitCell:F4} eV   ({result.ModelUsed}, {result.SupercellSize}³)\n" +
                    $"E supercell = {result.TotalSupercellEnergy:F4} eV   [{result.DeviceName}]";
                _lblLattice.Text =
                    $"Net |F| = {net.Length():F4} eV/Å\n" +
                    $"along a = {fa:F4}   b = {fb:F4}   c = {fc:F4} eV/Å";

                Debug("─── Net energy & net force (MACE only) ───", System.Drawing.Color.Yellow);
                Debug($"  E_cell = {result.EnergyPerUnitCell:F6} eV", System.Drawing.Color.Yellow);
                Debug($"  E_super= {result.TotalSupercellEnergy:F6} eV", System.Drawing.Color.Yellow);
                Debug($"  Net F  = ({net.X:F4},{net.Y:F4},{net.Z:F4})  |F|={net.Length():F4}", System.Drawing.Color.Yellow);

                _grid.Rows.Clear();
                int nF = result.CentralCellForces.GetLength(0);
                for (int k = 0; k < nF; k++)
                {
                    string name = (k < (_molecule.SymetryEquivalents?.Count ?? 0))
                        ? (_molecule.SymetryEquivalents![k].name ?? $"#{k}")
                        : $"#{k}";
                    float fx = result.CentralCellForces[k, 0];
                    float fy = result.CentralCellForces[k, 1];
                    float fz = result.CentralCellForces[k, 2];
                    float mag = MathF.Sqrt(fx * fx + fy * fy + fz * fz);
                    _grid.Rows.Add(name, fx.ToString("F4"), fy.ToString("F4"), fz.ToString("F4"), mag.ToString("F4"));
                }

                _chkArrows.Checked = true;
                _chkInter.Checked = false;
                Flags.Instance.ShowForces = true;
                Flags.Instance.ShowForceTotal = true;
                Flags.Instance.ShowForceElectro = false;
                Flags.Instance.ShowForceDispersion = false;
                Flags.Instance.ShowForceRepulsion = false;
                Flags.Instance.ShowIntermolecular = false;
                Flags.Instance.ForceArrowScale = (float)_numScale.Value;

                // Full unit cell (mode 1 = symmetry) + force Display Options "Asymmetric" OFF
                try
                {
                    Flags.Instance.GenerateEquvalents = 1;
                    Flags.Instance.ShowAsymmetricUnit = false;
                    Flags.Instance.DrawPacking = false;

                    var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault()
                                   ?? MainForm.Instance;
                    // Prefer the live dock window; also walk OpenForms in case of multiple instances
                    DisplayOptionsWindow? disp = mainForm?.GetDisplayOptionsWindow();
                    foreach (Form f in Application.OpenForms)
                    {
                        if (f is DisplayOptionsWindow d)
                        {
                            disp = d;
                            d.SetPresentationMode(1);
                        }
                    }
                    if (disp == null)
                    {
                        int style = Flags.Instance.AtomStyle;
                        _molecule.Presentation(1, style);
                    }

                    // Ensure checkbox state even if SetPresentationMode was a no-op path
                    if (disp != null)
                        disp.SetAsymmetricUnit(false);

                    int appliedSym = MaceForceCache.ApplyToMolecule(_molecule);
                    Debug($"  Presentation → symmetry; Asymmetric checkbox OFF; forces on {appliedSym} atoms",
                        System.Drawing.Color.Cyan);
                }
                catch (Exception ex)
                {
                    Debug("  Presentation(symmetry) failed: " + ex.Message, System.Drawing.Color.Orange);
                }

                Flags.Instance.VisualRevision++;
                RefreshView();

                Debug(
                    $"Done. cell={result.CentralAtomCount} super={result.SupercellAtomCount} edges={result.EdgeCount} " +
                    $"inter pairs={ForceDecomposition.Intermolecular?.Count ?? 0}",
                    System.Drawing.Color.LightGreen);
                SetProgress(100, "Done.");
            }
            catch (Exception ex)
            {
                Debug("Crystal Calculations error: " + ex.Message, System.Drawing.Color.Red);
                SetProgress(0, "Error.");
                MessageBox.Show(this, ex.Message, "Crystal Calculations",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _busy = false;
                _btnCalc.Enabled = true;
                _btnCalc.Text = "Calculate";
                // Keep bar at 100 after success; reset style to continuous
                if (_progress.Style == ProgressBarStyle.Marquee)
                {
                    _progress.Style = ProgressBarStyle.Continuous;
                    if (_progress.Value < 100 && !_lblStatus.Text.StartsWith("Error"))
                        _progress.Value = 100;
                }
            }
        }

        /// <summary>
        /// Intermolecular visualization — pure TAL side:
        /// 1) MACE returns forces[N,3] in the same order as unit-cell atoms / SymetryEquivalents
        /// 2) TAL groups atoms by IndividualMoleculeInCell (Molecule ID)
        /// 3) Sum F_i per molecule, draw arrow between centroids for contacting pairs
        /// Worker does not need molecule info for this.
        /// </summary>
        private void BuildIntermolecularFromMace(
            Molecule mol,
            float[,] forcesN3,
            int[] molIds,
            int[] displayIndices)
        {
            ForceDecomposition.Intermolecular = new List<InterForce>();
            if (mol == null || forcesN3 == null) return;

            int nForce = forcesN3.GetLength(0);

            // --- Preferred source: SymetryEquivalents + parallel molIds / forces ---
            var groups = new Dictionary<int, List<int>>(); // molId -> list of unit-cell atom indices
            Vector3[] pos = null!;
            int n = 0;

            if (mol.SymetryEquivalents != null && mol.SymetryEquivalents.Count > 0
                && molIds != null && molIds.Length == mol.SymetryEquivalents.Count
                && nForce >= mol.SymetryEquivalents.Count)
            {
                n = mol.SymetryEquivalents.Count;
                pos = new Vector3[n];
                // Same convention as MACE / Presentation: U = Transpose(Frac→Cart); cart = Transform(frac, U)
                Matrix raw = mol.FractionaltoCarttesianMatrix(0, 0, 0);
                Matrix Ut = Matrix.Transpose(raw);
                Vector3 shift = mol.MoleculeShift;
                for (int i = 0; i < n; i++)
                {
                    var s = mol.SymetryEquivalents[i];
                    Vector3 cart = Vector3.Transform(new Vector3(s.x, s.y, s.z), Ut) + shift;
                    pos[i] = cart;
                    int id = molIds[i] >= 0 ? molIds[i] : s.IndividualMoleculeInCell;
                    if (id < 0) id = 0;
                    if (!groups.ContainsKey(id)) groups[id] = new List<int>();
                    groups[id].Add(i);
                }
                Debug($"  Intermolecular grouping: SymetryEquivalents N={n}, molecules={groups.Count}",
                    System.Drawing.Color.Cyan);
            }
            else if (mol.atom != null && displayIndices != null)
            {
                // Fallback: display atoms with HasForce + IndividualMoleculeInCell
                var atomGroups = new Dictionary<int, List<int>>();
                for (int i = 0; i < mol.NumberOfAtoms; i++)
                {
                    var a = mol.atom[i];
                    if (!a.HasForce) continue;
                    if (a.CellShiftX != 0 || a.CellShiftY != 0 || a.CellShiftZ != 0) continue;
                    int id = a.IndividualMoleculeInCell >= 0 ? a.IndividualMoleculeInCell : 0;
                    if (!atomGroups.ContainsKey(id)) atomGroups[id] = new List<int>();
                    atomGroups[id].Add(i);
                }
                if (atomGroups.Count >= 2)
                {
                    BuildInterFromDisplayAtoms(mol, atomGroups);
                    return;
                }
                Debug("  Intermolecular: not enough molecule groups", System.Drawing.Color.Orange);
                return;
            }
            else
            {
                Debug("  Intermolecular: no SymetryEquivalents / forces alignment", System.Drawing.Color.Orange);
                return;
            }

            if (groups.Count < 2)
            {
                Debug($"  Intermolecular: only {groups.Count} molecule ID(s) — no pairs", System.Drawing.Color.Orange);
                return;
            }

            // ALL unique molecule pairs in the unit cell: C(nMol,2).
            // Do NOT filter by contact distance — that was dropping half the pairs (e.g. 3 of 6 for Z=4).
            var ids = groups.Keys.OrderBy(k => k).ToList();
            int expectedPairs = ids.Count * (ids.Count - 1) / 2;

            for (int ia = 0; ia < ids.Count; ia++)
            for (int ib = ia + 1; ib < ids.Count; ib++)
            {
                var ga = groups[ids[ia]];
                var gb = groups[ids[ib]];

                Vector3 ca = Vector3.Zero;
                foreach (int i in ga) ca += pos[i];
                ca /= Math.Max(ga.Count, 1);

                Vector3 cb = Vector3.Zero;
                foreach (int i in gb) cb += pos[i];
                cb /= Math.Max(gb.Count, 1);

                float minD = float.MaxValue;
                foreach (int i in ga)
                foreach (int j in gb)
                {
                    float d = (pos[j] - pos[i]).Length();
                    if (d < minD) minD = d;
                }

                Vector3 fA = Vector3.Zero;
                foreach (int i in ga)
                {
                    if (i >= nForce) continue;
                    fA += new Vector3(forcesN3[i, 0], forcesN3[i, 1], forcesN3[i, 2]);
                }
                Vector3 fB = Vector3.Zero;
                foreach (int i in gb)
                {
                    if (i >= nForce) continue;
                    fB += new Vector3(forcesN3[i, 0], forcesN3[i, 1], forcesN3[i, 2]);
                }

                // Primary arrow: net MACE force on molecule A, placed at its COM, pair line A→B
                ForceDecomposition.Intermolecular.Add(new InterForce
                {
                    From = ca,
                    To = cb,
                    Electro = Vector3.Zero,
                    Dispersion = Vector3.Zero,
                    Repulsion = Vector3.Zero,
                    Total = fA,
                    Label = $"mol {ids[ia]}→{ids[ib]}  minD={minD:F2}Å  |FA|={fA.Length():F3}"
                });
            }

            Debug(
                $"  Intermolecular pairs from MACE+MoleculeID: {ForceDecomposition.Intermolecular.Count} " +
                $"(expected C({ids.Count},2)={expectedPairs})",
                System.Drawing.Color.Magenta);
        }

        private void BuildInterFromDisplayAtoms(Molecule mol, Dictionary<int, List<int>> atomGroups)
        {
            var ids = atomGroups.Keys.OrderBy(k => k).ToList();
            for (int ia = 0; ia < ids.Count; ia++)
            for (int ib = ia + 1; ib < ids.Count; ib++)
            {
                var ga = atomGroups[ids[ia]];
                var gb = atomGroups[ids[ib]];
                Vector3 ca = Centroid(mol, ga);
                Vector3 cb = Centroid(mol, gb);
                Vector3 fA = Vector3.Zero;
                foreach (int a in ga)
                    if (mol.atom[a].HasForce) fA += mol.atom[a].Force;
                ForceDecomposition.Intermolecular.Add(new InterForce
                {
                    From = ca,
                    To = cb,
                    Total = fA,
                    Electro = Vector3.Zero,
                    Dispersion = Vector3.Zero,
                    Repulsion = Vector3.Zero,
                    Label = $"mol {ids[ia]}→{ids[ib]}"
                });
            }
            Debug($"  Intermolecular pairs (display atoms): {ForceDecomposition.Intermolecular.Count}",
                System.Drawing.Color.Magenta);
        }

        private static Vector3 Centroid(Molecule mol, List<int> idx)
        {
            Vector3 c = Vector3.Zero;
            foreach (int i in idx) c += mol.atom[i].Coordinate;
            return c / Math.Max(idx.Count, 1);
        }


        /// <summary>
        /// Copy Molecule ID from unit-cell (SymetryEquivalents / molIds) onto matching parent-cell display atoms.
        /// Key = name|Z|SymId.
        /// </summary>
        private static void StampMoleculeIdsOntoDisplayAtoms(Molecule mol, int[] molIds)
        {
            if (mol?.SymetryEquivalents == null || molIds == null) return;
            int n = Math.Min(mol.SymetryEquivalents.Count, molIds.Length);
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < n; i++)
            {
                var s = mol.SymetryEquivalents[i];
                string key = $"{(s.name ?? "").Trim()}|{s.AtomicNumber}|{s.SymmetryEquivalentNumberInCell}";
                map[key] = molIds[i] >= 0 ? molIds[i] : s.IndividualMoleculeInCell;
            }
            for (int i = 0; i < mol.NumberOfAtoms; i++)
            {
                var a = mol.atom[i];
                if (a.CellShiftX != 0 || a.CellShiftY != 0 || a.CellShiftZ != 0) continue;
                string key = $"{(a.name ?? "").Trim()}|{a.AtomicNumber}|{a.SymmetryEquivalentNumberInCell}";
                if (map.TryGetValue(key, out int mid) && mid >= 0)
                {
                    a.IndividualMoleculeInCell = mid;
                    mol.atom[i] = a;
                }
            }
        }

        /// <summary>
        /// Verify force mapping. Full unit-cell display → net on atoms must match MACE SUM (~0).
        /// Asymmetric / partial display → only a subset is mapped; compare count, not full net.
        /// </summary>
        private void VerifyMappedForces(Molecule mol, MaceCrystalAnalyzer.Result result)
        {
            double sumX = 0, sumY = 0, sumZ = 0;
            int nWith = 0;
            for (int i = 0; i < mol.NumberOfAtoms; i++)
            {
                var a = mol.atom[i];
                if (!a.HasForce) continue;
                if (a.CellShiftX != 0 || a.CellShiftY != 0 || a.CellShiftZ != 0) continue;
                sumX += a.Force.X; sumY += a.Force.Y; sumZ += a.Force.Z;
                nWith++;
            }
            double mag = Math.Sqrt(sumX * sumX + sumY * sumY + sumZ * sumZ);

            double cx = 0, cy = 0, cz = 0;
            int nForce = result.CentralCellForces.GetLength(0);
            for (int i = 0; i < nForce; i++)
            {
                cx += result.CentralCellForces[i, 0];
                cy += result.CentralCellForces[i, 1];
                cz += result.CentralCellForces[i, 2];
            }
            double cmag = Math.Sqrt(cx * cx + cy * cy + cz * cz);

            Debug(
                $"  Verify: sum(mol.atom.Force parent) = ({sumX:F4},{sumY:F4},{sumZ:F4}) |F|={mag:F4}  n={nWith}",
                System.Drawing.Color.Cyan);
            Debug(
                $"  Verify: sum(CentralCellForces)     = ({cx:F4},{cy:F4},{cz:F4}) |F|={cmag:F4}  n={nForce}",
                System.Drawing.Color.Cyan);

            if (nWith == 0)
            {
                Debug("  WARNING: no forces on display atoms — mapping failed", System.Drawing.Color.Orange);
            }
            else if (nWith < nForce)
            {
                // Expected in asymmetric view: one molecule (e.g. 21 of 84) or partial packing
                Debug(
                    $"  Verify: partial display — mapped {nWith}/{nForce} unit-cell sites " +
                    $"(asymmetric/symmetry view). Full-cell net is MACE SUM; display net is one fragment.",
                    System.Drawing.Color.LightGreen);
            }
            else if (Math.Abs(mag - cmag) > 0.05)
            {
                Debug("  WARNING: full display mapped, but net ≠ MACE SUM — check Cart matching",
                    System.Drawing.Color.Orange);
            }
            else
            {
                Debug("  Verify OK: full cell mapped, net matches MACE SUM", System.Drawing.Color.LightGreen);
            }
        }

        private void LogMoleculeIds()
        {
            if (_molecule?.SymetryEquivalents == null || _molecule.SymetryEquivalents.Count == 0)
            {
                Debug("  SymetryEquivalents empty — cannot group molecules", System.Drawing.Color.Orange);
                return;
            }
            int nMol = _molecule.GetMoleculeCount(_molecule.SymetryEquivalents);
            var hist = new Dictionary<int, int>();
            foreach (var s in _molecule.SymetryEquivalents)
            {
                int id = s.IndividualMoleculeInCell;
                if (id < 0) continue;
                hist.TryGetValue(id, out int c);
                hist[id] = c + 1;
            }
            Debug($"  TAL GetMoleculeCount(SymetryEquivalents) = {nMol}", System.Drawing.Color.Cyan);
            Debug($"  Molecules in cell: {hist.Count}  sizes=[{string.Join(",", hist.OrderBy(kv => kv.Key).Select(kv => $"mol{kv.Key}:{kv.Value}"))}]",
                System.Drawing.Color.Cyan);
            if (hist.Count <= 1)
                Debug("  WARNING: only 1 molecule ID — intermolecular arrows may be empty", System.Drawing.Color.Orange);
        }

        private void LogForceTableMace(MaceCrystalAnalyzer.Result result)
        {
            Debug("─── MACE total forces (central cell) [eV/Å] ───", System.Drawing.Color.LightGreen);
            int n = result.CentralCellForces.GetLength(0);
            double sx = 0, sy = 0, sz = 0;
            for (int k = 0; k < n; k++)
            {
                float fx = result.CentralCellForces[k, 0];
                float fy = result.CentralCellForces[k, 1];
                float fz = result.CentralCellForces[k, 2];
                float mag = MathF.Sqrt(fx * fx + fy * fy + fz * fz);
                sx += fx; sy += fy; sz += fz;
                string name = k < (_molecule?.SymetryEquivalents?.Count ?? 0)
                    ? (_molecule!.SymetryEquivalents[k].name ?? $"#{k}")
                    : $"#{k}";
                Debug($"  {name,-10} {fx,10:F4} {fy,10:F4} {fz,10:F4} {mag,10:F4}", System.Drawing.Color.LightGray);
            }
            double net = Math.Sqrt(sx * sx + sy * sy + sz * sz);
            Debug($"  SUM      {sx,10:F4} {sy,10:F4} {sz,10:F4} {net,10:F4}", System.Drawing.Color.Yellow);
        }

        private void LogIntermolecular()
        {
            var list = ForceDecomposition.Intermolecular;
            Debug("─── Intermolecular (MACE net force on mol A, COM→COM) ───", System.Drawing.Color.Magenta);
            if (list == null || list.Count == 0)
            {
                Debug("  (no pairs — check molecule detection / Molecule ID)", System.Drawing.Color.Orange);
                return;
            }
            foreach (var p in list)
            {
                Debug($"  {p.Label,-16} |F|={p.Total.Length():F4}  F=({p.Total.X:F3},{p.Total.Y:F3},{p.Total.Z:F3})",
                    System.Drawing.Color.Gray);
            }
        }

        private void RefreshView()
        {
            var main = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            main?.GetActiveDocument()?.RefreshView();
        }
    }
}
