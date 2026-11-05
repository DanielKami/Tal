// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldDocument.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HirshfeldDocument v10 — Advanced panel for STO tuning.
//
// ⭐ v10 ZMIANY:
// - Removed Pre-blur (grid) — always disabled (blur=0).
// - Removed White band — always 0 (no band).
// - Dodano Advanced checkbox + panel z 3 suwakami:
// DEFAULT_STEP, EXP_CUTOFF, MAX_RMAX
// - Rebuild LUT cache + Regenerate przyciskiem.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection.Metadata;
using Tal.DrawMethods;
using Tal.Hirshfeld.Sto;
using Tal.Utils;
using WeifenLuo.WinFormsUI.Docking;
using Color = Microsoft.Xna.Framework.Color;
using GdiColor = System.Drawing.Color;

namespace Tal.Hirshfeld
{
    public partial class HirshfeldDocument : DockContent
    {
        #region Graphics fields

        private GraphicsDevice _device;
        private BasicEffect _effect;
        private BasicEffect _atomEffect;
        private BasicEffect _surfaceEffect;
        private Effect _hirshfeldSurfaceEffect;

        private Camera _camera;
        private SpriteBatch _spriteBatch;
        private SpriteFont _spriteFont;
        private ContentManager _content;
        private Texture2D _pixelTexture;

        private System.Windows.Forms.Timer _timer;
        private bool _deviceReady = false;
        private bool _disposed = false;

        private VertexBuffer _surfaceVB;
        private int _surfaceVertCount;

        private IndexBuffer _surfaceIBOpaque;
        private IndexBuffer _surfaceIBTransparent;
        private int _surfaceIndexCount;

        private VertexPositionNormalColor4[] _surfaceVertexDataCache;

        private VertexBuffer _wireframeVB;
        private int _wireframeVertCount;

        private VertexBuffer _axesVB;
        private int _axesVertCount;

        private VertexBuffer _cellVB;
        private int _cellVertCount;

        private Model _smallBall;
        private Model _bigBall;
        private Model _smallRod;

        private Matrix[] _smallBallTransforms;
        private Matrix[] _bigBallTransforms;
        private Matrix[] _smallRodTransforms;

        private ModelMesh[] _smallBallMeshes;
        private ModelMesh[] _bigBallMeshes;
        private ModelMesh[] _smallRodMeshes;

        private Matrix _lastSortMatrix = Matrix.Identity;
        private (int index, float z)[] _zDepthCache;
        private bool _sortDirty = true;

        private int[] _sortedIndices;
        private bool _indicesSorted = false;

        private float _lastSortYaw = 0f;
        private float _lastSortPitch = 0f;
        private const float SORT_ANGLE_THRESHOLD_DEG = 5f;

        private DepthStencilState _depthReadNoWrite;
        private DepthStencilState _depthDefaultWrite;

        #endregion

        #region Application state

        private HirshfeldSurface _surface;
        private Molecule _molecule;
        private int _targetMolId = 0;

        private float _sceneScale = 1.0f;
        private float _zoomFactor = 1.0f;

        private byte _alphaValue = 200;
        private bool _showMesh = true;
        private bool _showWireframe = false;
        private bool _showMolecule = true;
        private bool _showAxes = false;
        private bool _showCell = false;

        private HirshfeldPropertyMode _propertyMode = HirshfeldPropertyMode.DNorm;
        private bool _dirtyVertexColors = true;

 // ⭐ Pre-blur REMOVED — always disabled (blur = 0).
 // Fields _blurLevel i BLUR_MAX removed.

 // ⭐ d_norm scale — steruje Flags.HirshfeldDNormScale
        private float _dNormScaleFactor = 1.0f;
        private const float DNORM_SCALE_MIN = 0.2f;
        private const float DNORM_SCALE_MAX = 3.0f;

 // ⭐ White band REMOVED — always 0. Afterle _whiteBandFactor removed.

 // ⭐ Used by DE/DI palety
        private float _thresholdGamma = 0.55f;

        private CancellationTokenSource _cts;
        private bool _isGenerating = false;

 // ⭐ Advanced panel — stan
        private bool _advancedVisible = false;

        private HirshfeldFingerprintWindow _fingerprintWindow;

        private Vector3 _lastMoleculeShift = new Vector3(float.NaN, float.NaN, float.NaN);

        private (int binDe, int binDi)[] _vertexBins;
        private (int binDe, int binDi) _highlightedBin = (-1, -1);

        private HirshfeldAtomDistanceDiagnostics _lastDiagnostics;

        private HashSet<string> _activeContactFilter;
        private string[] _contactTypes;

        #endregion

        #region Debug helpers

        private static void LogInfo(string msg) => ProgressReporter.Log(msg, GdiColor.White);
        private static void LogWarn(string msg) => ProgressReporter.Log(msg, GdiColor.Orange);
        private static void LogError(string msg) => ProgressReporter.Log(msg, GdiColor.Red);
        private static void LogSuccess(string msg) => ProgressReporter.Log(msg, GdiColor.LightGreen);

        private static GdiColor XnaToGdi(Color c) => GdiColor.FromArgb(c.A, c.R, c.G, c.B);

        #endregion

        #region Constructor

        public HirshfeldDocument(string title = "Hirshfeld Surface")
        {
            InitializeComponent();

            EnsureDefaults();

            Text = title;

            BackColor = XnaToGdi(Flags.Instance.color1);
            _panel.BackColor = XnaToGdi(Flags.Instance.color1);

            _panel.HandleCreated += (s, e) =>
            {
                if (!_deviceReady) InitializeGraphics();

                if (_lblTransparencyValue != null && _trackTransparency != null)
                    _lblTransparencyValue.Text = $"{_trackTransparency.Value} / 255";

 // ── d_norm scale ──
                if (_lblDNormScaleValue != null && _trackDNormScale != null)
                {
                    float v = Math.Clamp(Flags.Instance.HirshfeldDNormScale,
                                         DNORM_SCALE_MIN, DNORM_SCALE_MAX);

                    float t = (v - DNORM_SCALE_MIN) / (DNORM_SCALE_MAX - DNORM_SCALE_MIN);
                    _trackDNormScale.Value = (int)Math.Clamp(t * 100f, 0, 100);

                    _dNormScaleFactor = v;
                    _lblDNormScaleValue.Text = $"{_dNormScaleFactor:F2}×";
                }
            };

            _panel.MouseDown += Panel_MouseDown;
            _panel.MouseMove += Panel_MouseMove;
            _panel.MouseUp += Panel_MouseUp;
            _panel.MouseWheel += Panel_MouseWheel;

            _btnUseSelection.Click += (s, e) => UseCurrentSelection();
            _btnGenerate.Click += async (s, e) => await GenerateAsync();
            _btnClear.Click += (s, e) => ClearSurface();
            _btnSavePly.Click += (s, e) => SavePly();
            _btnExportPng.Click += (s, e) => SavePng();
            _btnFingerprint.Click += (s, e) => ShowFingerprintWindow();
            _btnShowDiagnostics.Click += (s, e) => OpenDiagnosticsInDebugWindow();

            _numVoxelSize.ValueChanged += (s, e) => { };
            _numMargin.ValueChanged += (s, e) => { };

            _cmbPropertyMode.SelectedIndexChanged += (s, e) =>
            {
                _propertyMode = (HirshfeldPropertyMode)_cmbPropertyMode.SelectedIndex;
                _dirtyVertexColors = true;
            };

            _trackTransparency.Scroll += (s, e) =>
            {
                byte newAlpha = (byte)_trackTransparency.Value;
                bool wasTransparent = _alphaValue < 240;
                bool nowTransparent = newAlpha < 240;

                _alphaValue = newAlpha;
                _lblTransparencyValue.Text = $"{_alphaValue} / 255";
                _dirtyVertexColors = true;

                if (wasTransparent != nowTransparent)
                {
                    _indicesSorted = false;
                }
            };

 // ⭐ d_norm scale
            _trackDNormScale.Scroll += (s, e) =>
            {
                float t = _trackDNormScale.Value / 100f;
                _dNormScaleFactor = DNORM_SCALE_MIN
                                  + t * (DNORM_SCALE_MAX - DNORM_SCALE_MIN);

                Flags.Instance.HirshfeldDNormScale = _dNormScaleFactor;
                _lblDNormScaleValue.Text = $"{_dNormScaleFactor:F2}×";

                _dirtyVertexColors = true;
            };

 // ⭐ White band REMOVED — _trackWhiteBand no longer exists

 // ⭐ Advanced panel
            _chkAdvanced.CheckedChanged += (s, e) =>
            {
                _advancedVisible = _chkAdvanced.Checked;
                _pnlAdvanced.Visible = _advancedVisible;

 // ⭐ Refresh system FlowLayoutPanel
                _controls.PerformLayout();
                _controls.ScrollControlIntoView(_pnlAdvanced);
            };

 // Wczytaj current values STO do kontrolek Advanced
            _numAdvStep.Value = Math.Clamp(
                (decimal)StoDensityLut.DefaultStep,
                _numAdvStep.Minimum, _numAdvStep.Maximum);

            _numAdvExpCutoff.Value = Math.Clamp(
                (decimal)StoDensityLut.ExpCutoff,
                _numAdvExpCutoff.Minimum, _numAdvExpCutoff.Maximum);

            _numAdvMaxRMax.Value = Math.Clamp(
                (decimal)StoDensityLut.MaxRMax,
                _numAdvMaxRMax.Minimum, _numAdvMaxRMax.Maximum);

            _btnAdvRebuild.Click += async (s, e) => await RebuildLutAndRegenerateAsync();

            _chkShowMesh.CheckedChanged += (s, e) => _showMesh = _chkShowMesh.Checked;
            _chkShowWireframe.CheckedChanged += (s, e) =>
            {
                _showWireframe = _chkShowWireframe.Checked;
                RebuildWireframeBuffer();
            };
            _chkShowMolecule.CheckedChanged += (s, e) =>
            {
                _showMolecule = _chkShowMolecule.Checked;
                _sortDirty = true;
            };
            _chkShowAxes.CheckedChanged += (s, e) =>
            {
                _showAxes = _chkShowAxes.Checked;
                RebuildAxesBuffer();
            };
            _chkShowCell.CheckedChanged += (s, e) =>
            {
                _showCell = _chkShowCell.Checked;
                RebuildCellBuffer();
            };

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.Control && e.KeyCode == Keys.S)
                {
                    SavePng();
                    e.Handled = true;
                }
            };

            _timer = new System.Windows.Forms.Timer { Interval = 33 };
            _timer.Tick += (s, e) => RenderFrame();
            _timer.Start();

            Disposed += (s, e) => CleanupAll();
        }

 /// <summary>
 /// ⭐ Ensures controls have sensible default values
 /// (in case the designer did not set them).
 /// </summary>
        private void EnsureDefaults()
        {
 // ComboBox: Density model
            if (_cmbDensityModel != null
                && _cmbDensityModel.SelectedIndex < 0
                && _cmbDensityModel.Items.Count > 0)
            {
                _cmbDensityModel.SelectedIndex = 0;
            }

 // ComboBox: Density mode
            if (_cmbDensityMode != null
                && _cmbDensityMode.SelectedIndex < 0
                && _cmbDensityMode.Items.Count > 0)
            {
                _cmbDensityMode.SelectedIndex = 0;
            }

 // ComboBox: Property mode
            if (_cmbPropertyMode != null
                && _cmbPropertyMode.SelectedIndex < 0
                && _cmbPropertyMode.Items.Count > 0)
            {
                _cmbPropertyMode.SelectedIndex = 1;
            }

 // Numeric: Voxel size
            if (_numVoxelSize != null && _numVoxelSize.Value < 0.01m)
                _numVoxelSize.Value = 0.10m;

 // Numeric: Grid margin
            if (_numMargin != null && _numMargin.Value < 1.0m)
                _numMargin.Value = 3.0m;

 // Numeric: Iso level
            if (_numIsoLevel != null
                && (_numIsoLevel.Value < 0.30m || _numIsoLevel.Value > 0.70m))
                _numIsoLevel.Value = 0.50m;

 // Numeric: Sigma multiplier
            if (_numSigmaMult != null
                && (_numSigmaMult.Value < 0.8m || _numSigmaMult.Value > 2.0m))
                _numSigmaMult.Value = 1.00m;
        }

        #endregion

        #region Public API

        public void SetMolecule(Molecule mol, int initialMolId = 0)
        {
            _molecule = mol;
            _targetMolId = initialMolId;
            _sortDirty = true;

            _lastMoleculeShift = new Vector3(float.NaN, float.NaN, float.NaN);

            RefreshMoleculeList();

            if (mol != null)
            {
                try
                {
                    var state = mol.CheckHydrogenState(out var fixes);

                    switch (state)
                    {
                        case Molecule.HydrogenState.NeedsNormalization:
                            {
                                int n = 0;
                                for (int i = 0; i < fixes.Count; i++)
                                    if (!fixes[i].Skipped) n++;

                                LogWarn($"[Hirshfeld] ⚠ {n} hydrogen(s) need normalization. " +
                                        "Will auto-normalize on Generate.");
                            }
                            break;

                        case Molecule.HydrogenState.AlreadyNormalized:
                            LogSuccess("[Hirshfeld] ✓ Hydrogens already normalized.");
                            break;

                        case Molecule.HydrogenState.DisabledByUser:
                            LogWarn("[Hirshfeld] ⚠ H normalization disabled by user. " +
                                    "Hirshfeld surface will use X-ray H positions.");
                            break;

                        case Molecule.HydrogenState.NoHydrogens:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    LogWarn($"[Hirshfeld] H state check failed: {ex.Message}");
                }
            }
        }

        public HirshfeldSurface GetSurface() => _surface;
        public GraphicsDevice GetDevice() => _device;
        public Camera GetCamera() => _camera;

        public HirshfeldAtomDistanceDiagnostics GetDiagnostics() => _lastDiagnostics;

        #endregion

        #region Molecule list

        private void RefreshMoleculeList()
        {
            _cmbMolecule.Items.Clear();
            if (_molecule == null) return;

            var list = HirshfeldTargetSelector.EnumerateMolecules(_molecule);
            foreach (var info in list)
                _cmbMolecule.Items.Add(info.Label);

            if (_cmbMolecule.Items.Count > 0)
            {
                int idx = list.FindIndex(x => x.MoleculeId == _targetMolId);
                _cmbMolecule.SelectedIndex = idx >= 0 ? idx : 0;
            }
        }

        private void UseCurrentSelection()
        {
            if (_molecule == null) return;

            var selectedMols = new HashSet<int>();
            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
            {
                if (_molecule.atom[i].selected && _molecule.atom[i].IndividualMoleculeInCell >= 0)
                    selectedMols.Add(_molecule.atom[i].IndividualMoleculeInCell);
            }

            if (selectedMols.Count == 0)
            {
                MessageBox.Show("No atoms selected.", "Hirshfeld",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (selectedMols.Count > 1)
            {
                MessageBox.Show(
                    $"Multiple molecules selected ({selectedMols.Count}). " +
                    "Select atoms from one molecule only.",
                    "Hirshfeld", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int molId = selectedMols.First();
            var list = HirshfeldTargetSelector.EnumerateMolecules(_molecule);
            int idx = list.FindIndex(x => x.MoleculeId == molId);
            if (idx >= 0) _cmbMolecule.SelectedIndex = idx;
        }

        #endregion

        #region Advanced panel — Rebuild LUT

 /// <summary>
 /// ⭐ Zapisuje values z Advanced panel do StoDensityLut,
 /// clears cache LUT, przebudowuje LUT i regenerates surface.
 /// </summary>
        private async Task RebuildLutAndRegenerateAsync()
        {
            if (_isGenerating) return;

            try
            {
 // ── Wczytaj values ──
                float newStep = (float)_numAdvStep.Value;
                float newExpCutoff = (float)_numAdvExpCutoff.Value;
                float newMaxRMax = (float)_numAdvMaxRMax.Value;

 // ── Sanity ──
                if (newStep < 0.005f) newStep = 0.005f;
                if (newStep > 0.10f) newStep = 0.10f;
                if (newExpCutoff < 5f) newExpCutoff = 5f;
                if (newExpCutoff > 40f) newExpCutoff = 40f;
                if (newMaxRMax < 5f) newMaxRMax = 5f;
                if (newMaxRMax > 50f) newMaxRMax = 50f;

 // ── Save do StoDensityLut ──
                StoDensityLut.DefaultStep = newStep;
                StoDensityLut.ExpCutoff = newExpCutoff;
                StoDensityLut.MaxRMax = newMaxRMax;

                LogInfo($"[Hirshfeld.Advanced] Nowe parametry STO: " +
                        $"step={newStep:F3}, expCutoff={newExpCutoff:F1}, " +
                        $"maxRMax={newMaxRMax:F1}");

 // ── Clear cache LUT ──
                StoDatabase.ClearLutCache();

 // ── Rebuild LUT for common symbols ──
                var commonSymbols = new[]
                {
                    "H", "C", "N", "O", "F", "P", "S", "CL", "BR", "I"
                };

                LogInfo("[Hirshfeld.Advanced] Rebuilding LUT cache...");
                var sw = System.Diagnostics.Stopwatch.StartNew();
                int builtCount = StoDatabase.PrebuildLuts(commonSymbols, step: newStep);
                sw.Stop();

                LogSuccess($"[Hirshfeld.Advanced] ✓ Prebuilt {builtCount} LUT tables " +
                           $"in {sw.ElapsedMilliseconds} ms");

 // ── Regenerate surface ──
                await GenerateAsync();
            }
            catch (Exception ex)
            {
                LogError($"[Hirshfeld.Advanced] Rebuild failed: {ex.Message}");
                MessageBox.Show($"Advanced rebuild failed:\n{ex.Message}",
                    "Hirshfeld Advanced", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Diagnostics

        private void OpenDiagnosticsInDebugWindow()
        {
            if (_lastDiagnostics == null)
            {
                MessageBox.Show(
                    "Generate a surface first.",
                    "Diagnostics",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm == null) return;

            var debugWin = mainForm.GetDebugWindow();

            if (debugWin.IsHidden)
            {
                try
                {
                    debugWin.Show();
                }
                catch
                {
                    try
                    {
                        debugWin.Show(mainForm.GetDockPanel(), DockState.DockBottomAutoHide);
                    }
                    catch
                    {
                        debugWin.Show(mainForm.GetDockPanel(), DockState.Float);
                    }
                }
            }

            debugWin.Show();
            debugWin.BringToFront();
            debugWin.Activate();

            debugWin.RefreshDebugInfo();

            string report = BuildDiagnosticsReport(_lastDiagnostics);
            debugWin.Log(report, GdiColor.Cyan);

            LogInfo("[Hirshfeld] Diagnostics sent to DebugWindow");
        }

        private string BuildDiagnosticsReport(HirshfeldAtomDistanceDiagnostics diag)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine();
            sb.AppendLine("════════════════════════════════════════════");
            sb.AppendLine("🔬 HIRSHFELD ATOM DISTANCE DISTRIBUTION");
            sb.AppendLine("════════════════════════════════════════════");
            sb.AppendLine();

            sb.AppendLine("📦 Grid Info:");
            sb.AppendLine($"  TargetCenter = ({diag.TargetCenter.X:F2}, " +
                          $"{diag.TargetCenter.Y:F2}, {diag.TargetCenter.Z:F2}) Å");
            sb.AppendLine($"  R_target = {diag.RTarget:F3} Å");
            sb.AppendLine($"  R_grid   = {diag.RGrid:F3} Å  (margin = {diag.Margin:F2} Å)");
            sb.AppendLine($"  Voxel size = {diag.VoxelSize:F4} Å");
            sb.AppendLine($"  Grid = {diag.GridResX} × {diag.GridResY} × {diag.GridResZ}");
            sb.AppendLine($"  Total atoms = {diag.TotalAtoms}  " +
                          $"(target: {diag.TargetCount}, outer: {diag.OuterCount})");
            sb.AppendLine();

            sb.AppendLine("🎯 Target Atoms:");
            if (diag.TargetCount > 0)
            {
                sb.AppendLine($"  min={diag.TargetMin:F3}  max={diag.TargetMax:F3}  " +
                              $"median={diag.TargetMedian:F3}");
                sb.AppendLine($"  p95={diag.TargetP95:F3}  p99={diag.TargetP99:F3}");
            }
            else
            {
                sb.AppendLine("  (no target atoms)");
            }
            sb.AppendLine();

            sb.AppendLine("🌐 Outer Atoms:");
            if (diag.OuterCount > 0)
            {
                sb.AppendLine($"  min={diag.OuterMin:F3}  max={diag.OuterMax:F3}  " +
                              $"median={diag.OuterMedian:F3}");
                sb.AppendLine($"  p95={diag.OuterP95:F3}  p99={diag.OuterP99:F3}");
            }
            else
            {
                sb.AppendLine("  (no outer atoms)");
            }
            sb.AppendLine();

            if (diag.Histogram != null && diag.Histogram.Length > 0)
            {
                sb.AppendLine($"📊 Histogram (bin = {diag.HistogramBinSize:F1} Å):");

                const int MAX_BAR = 60;
                for (int b = 0; b < diag.Histogram.Length; b++)
                {
                    int count = diag.Histogram[b];
                    if (count == 0) continue;

                    float lo = b * diag.HistogramBinSize;
                    float hi = lo + diag.HistogramBinSize;

                    int barLen = diag.HistogramMaxCount > 0
                        ? (int)(MAX_BAR * count / (float)diag.HistogramMaxCount)
                        : 0;
                    barLen = Math.Max(1, barLen);
                    string bar = new string('█', barLen);

                    sb.AppendLine($"    {lo,5:F1}–{hi,5:F1} Å : {count,5}  {bar}");
                }
                sb.AppendLine();
            }

            if (diag.CutoffAnalysis != null && diag.CutoffAnalysis.Count > 0)
            {
                sb.AppendLine("✂️ Cutoff Analysis (atomy z d > cutoff = odrzucone):");

                foreach (var (dCutoff, inside, outside) in diag.CutoffAnalysis)
                {
                    int total = inside + outside;
                    float pctOut = total > 0 ? 100f * outside / total : 0f;
                    float delta = dCutoff - diag.RTarget;

                    sb.AppendLine($"    R_target + {delta:F0} Å = {dCutoff:F2} Å  →  " +
                                  $"inside {inside,5}  outside {outside,5}  " +
                                  $"({pctOut:F1}% odrzucone)");
                }
                sb.AppendLine();
            }

            sb.AppendLine("💡 Interpretation:");
            sb.AppendLine("  • Atomy poza R_grid + 5σ_max są kandydatami do odrzucenia.");
            sb.AppendLine("  • Bezpieczne odrzucenie: d > R_target + 5 Å.");
            sb.AppendLine("  • Agresywne odrzucenie: d > R_target + 4 Å (ryzyko artefaktów).");
            sb.AppendLine();
            sb.AppendLine("════════════════════════════════════════════");

            return sb.ToString();
        }

        #endregion

        #region Fingerprint window

        private void ShowFingerprintWindow()
        {
            if (_surface == null || !_surface.IsValid)
            {
                MessageBox.Show("Generate a surface first.", "Fingerprint",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();

            if (mainForm == null)
            {
                MessageBox.Show("Main window not available.",
                    "Fingerprint", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string title = $"Fingerprint: {_molecule?.Name ?? "?"} / mol #{_targetMolId}";
            var fpWindow = mainForm.GetHirshfeldFingerprintWindow(title);

            fpWindow.OnVertexBinsUpdated = (newVertexBins) =>
            {
                _vertexBins = newVertexBins;
                _dirtyVertexColors = true;
                RebuildSurfaceBuffer();

                LogInfo($"[Hirshfeld] VertexBins synced from fingerprint " +
                        $"({newVertexBins?.Length ?? 0} vertices)");
            };

            fpWindow.OnRegionClicked = (binDe, binDi) =>
            {
                _highlightedBin = (binDe, binDi);
                _dirtyVertexColors = true;
                RebuildSurfaceBuffer();

                LogInfo($"[Hirshfeld] Region clicked: bin=({binDe},{binDi})");
            };

            fpWindow.OnContactFilterChanged = (filter) =>
            {
                _activeContactFilter = filter;
                _dirtyVertexColors = true;
                RebuildSurfaceBuffer();

                if (filter == null)
                    LogInfo("[Hirshfeld] Contact filter: ALL");
                else if (filter.Count == 0)
                    LogInfo("[Hirshfeld] Contact filter: NONE");
                else
                    LogInfo("[Hirshfeld] Contact filter: " + string.Join(", ", filter));
            };

            fpWindow.SetRawData(
                _surface.DE,
                _surface.DI,
                _surface.NearestInnerAtomName,
                _surface.NearestOuterAtomName);

            if (fpWindow.IsHidden)
            {
                fpWindow.Show(mainForm.GetDockPanel(), DockState.Document);
            }
            else
            {
                fpWindow.Activate();
                if (fpWindow.WindowState == FormWindowState.Minimized)
                    fpWindow.WindowState = FormWindowState.Normal;
            }

            var analysis = BuildFingerprintAnalysis();
            _vertexBins = analysis.VertexBins;
            _contactTypes = analysis.ContactTypes;
            _activeContactFilter = null;
            _highlightedBin = (-1, -1);

            fpWindow.SetAnalysis(analysis);

            ProgressReporter.Log(
                $"🔬 Fingerprint: mol #{_targetMolId}, " +
                $"contacts = {analysis.ContactTypePercentages?.Count ?? 0} types, " +
                $"verts = {_surface.VertexCount}",
                GdiColor.Cyan);

            LogInfo($"[Hirshfeld] Fingerprint window opened (mol #{_targetMolId}), " +
                    $"contacts = {analysis.ContactTypePercentages?.Count ?? 0} types");
        }

        private HirshfeldAnalysisResults BuildFingerprintAnalysis()
        {
            const float RANGE_MIN = 0.0f;
            const float RANGE_MAX = 3.1f;
            const float BIN_SIZE = 0.005f;

            var result = new HirshfeldAnalysisResults
            {
                Surface = _surface,
                FingerprintBinSize = BIN_SIZE,
                FingerprintRangeMin = RANGE_MIN,
                FingerprintRangeMax = RANGE_MAX,
                FingerprintHistogram = new Dictionary<(int binDe, int binDi), int>(),
                VertexBins = new (int binDe, int binDi)[_surface.VertexCount]
            };

            int n = _surface.VertexCount;
            float binSize = result.FingerprintBinSize;

            for (int i = 0; i < n; i++)
                result.VertexBins[i] = (-1, -1);

            for (int i = 0; i < n; i++)
            {
                float de = _surface.DE != null && i < _surface.DE.Length ? _surface.DE[i] : 0f;
                float di = _surface.DI != null && i < _surface.DI.Length ? _surface.DI[i] : 0f;

                if (de < RANGE_MIN || de > RANGE_MAX) continue;
                if (di < RANGE_MIN || di > RANGE_MAX) continue;

                int binDe = (int)MathF.Floor((de - RANGE_MIN) / binSize);
                int binDi = (int)MathF.Floor((di - RANGE_MIN) / binSize);

                result.VertexBins[i] = (binDe, binDi);

                var key = (binDe, binDi);
                if (result.FingerprintHistogram.TryGetValue(key, out int count))
                    result.FingerprintHistogram[key] = count + 1;
                else
                    result.FingerprintHistogram[key] = 1;
            }

            HirshfeldContactDecompose.Compute(result, _surface);

            return result;
        }

        #endregion

        #region Generate

        private void NormalizeHydrogensForHirshfeldSilently()
        {
            if (_molecule == null) return;

            Molecule.HydrogenState state;
            List<Tal.Calculate.HydrogenNormalizer.HydrogenFix> fixes;

            try
            {
                state = _molecule.CheckHydrogenState(
                    out fixes,
                    cutoffHeavy: 1.3f,
                    skipBridging: true);
            }
            catch (Exception ex)
            {
                LogWarn($"[Hirshfeld] H state check failed: {ex.Message}");
                return;
            }

            switch (state)
            {
                case Molecule.HydrogenState.NoStructure:
                case Molecule.HydrogenState.NoHydrogens:
                    return;

                case Molecule.HydrogenState.AlreadyNormalized:
                    LogSuccess("[Hirshfeld] ✓ Hydrogens already normalized (neutron lengths).");
                    return;

                case Molecule.HydrogenState.DisabledByUser:
                    LogInfo("[Hirshfeld] H normalization was disabled for 3D view — " +
                            "but Hirshfeld REQUIRES normalized H. Overriding.");
                    break;

                case Molecule.HydrogenState.NeedsNormalization:
                    break;

                default:
                    return;
            }

            int toFix = 0;
            if (fixes != null)
            {
                for (int i = 0; i < fixes.Count; i++)
                    if (!fixes[i].Skipped) toFix++;
            }

            if (toFix == 0) return;

            LogInfo($"[Hirshfeld] Normalizing {toFix} hydrogens to neutron lengths " +
                    "(standard for Hirshfeld analysis)...");

            List<Tal.Calculate.HydrogenNormalizer.HydrogenFix> applied;
            try
            {
                applied = _molecule.ApplyHydrogenNormalization(
                    cutoffHeavy: 1.3f,
                    skipBridging: true);
            }
            catch (Exception ex)
            {
                LogError($"[Hirshfeld] Hydrogen normalization FAILED: {ex.Message}");
                return;
            }

            int appliedCount = 0;
            int skippedCount = 0;
            if (applied != null)
            {
                for (int i = 0; i < applied.Count; i++)
                {
                    if (applied[i].Skipped) skippedCount++;
                    else appliedCount++;
                }
            }

            LogSuccess($"[Hirshfeld] ✓ Normalized {appliedCount} hydrogens " +
                       $"({skippedCount} skipped). Scene rebuilt: " +
                       $"{_molecule.NumberOfAtoms} atoms.");

            if (_fingerprintWindow != null && !_fingerprintWindow.IsDisposed)
            {
                try { _fingerprintWindow.Close(); } catch { }
                _fingerprintWindow = null;
            }

            if (_surface != null)
            {
                ClearSurface();
            }
        }

        private async Task GenerateAsync()
        {
            if (_molecule == null)
            {
                MessageBox.Show("No molecule loaded.", "Hirshfeld",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_isGenerating) return;

            _isGenerating = true;
            _btnGenerate.Enabled = false;
            _btnClear.Enabled = false;

            try
            {
 // ⭐ Hirshfeld always normalisee H
                NormalizeHydrogensForHirshfeldSilently();

 // ⭐ STO database
                MainForm.EnsureStoDatabaseLoaded();

                _cts = new CancellationTokenSource();

                float voxelSize = (float)_numVoxelSize.Value;
                float sigmaMult = (float)_numSigmaMult.Value;
                float isoLevel = (float)_numIsoLevel.Value;
                bool useAnisotropic = _cmbDensityModel.SelectedIndex == 1;
                bool useSto = _cmbDensityMode != null && _cmbDensityMode.SelectedIndex == 0;

 // ⭐ Pre-blur REMOVED — always 0
                float blurSigma = 0f;
                int blurIterations = 0;

 // ⭐ White band REMOVED — always 0
                Flags.Instance.HirshfeldDNormWhiteBand = 0f;

                var list = HirshfeldTargetSelector.EnumerateMolecules(_molecule);
                if (_cmbMolecule.SelectedIndex < 0 || _cmbMolecule.SelectedIndex >= list.Count)
                {
                    MessageBox.Show("Select a target molecule first.", "Hirshfeld",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                int molId = list[_cmbMolecule.SelectedIndex].MoleculeId;
                _targetMolId = molId;

                var detector = new HirshfeldDetector(_molecule);

                int lastPct = -1;
                var progressAdapter = new Action<string, float>((msg, p) =>
                {
                    if (IsDisposed) return;

                    int pct = Math.Clamp((int)(p * 100), 0, 100);
                    if (pct == lastPct) return;
                    lastPct = pct;

                    if (InvokeRequired)
                    {
                        try
                        {
                            BeginInvoke(new Action(() =>
                            {
                                _lblStatus.Text = msg;
                                _progressBar.Value = pct;
                            }));
                        }
                        catch { }
                    }
                    else
                    {
                        _lblStatus.Text = msg;
                        _progressBar.Value = pct;
                    }
                });
                float margin = (float)_numMargin.Value;

                bool ok = await detector.GenerateAsync(
                    targetMolId: molId,
                    voxelSize: voxelSize,
                    sigmaMultiplier: sigmaMult,
                    useAnisotropic: useAnisotropic,
                    isoLevel: isoLevel,
                    margin: margin,
                    blurSigma: blurSigma,
                    blurIterations: blurIterations,
                    neighborCutoff: HirshfeldParameters.CUT_OFF,
                    useSto: useSto,
                    progress: progressAdapter,
                    ct: _cts.Token);

                if (!ok)
                {
                    _lblStatus.Text = "Generation failed or cancelled.";
                    return;
                }

                _surface = detector.Surface;

                _lastDiagnostics = HirshfeldAtomDistanceDiagnostics.Compute(
                    detector.Scene,
                    detector.Grid,
                    _surface);

                _vertexBins = null;
                _highlightedBin = (-1, -1);

                _lastMoleculeShift = _molecule?.MoleculeShift ?? Vector3.Zero;

                _dirtyVertexColors = true;

                RebuildSurfaceBuffer();
                RebuildWireframeBuffer();
                RebuildAxesBuffer();
                RebuildCellBuffer();

                ResetView();

                Text = $"Hirshfeld: {_molecule.Name} / molecule #{molId}";

                Flags.Instance.VisualRevision++;
                Flags.Instance.MarkDirty();

                _progressBar.Value = 100;
                _lblStatus.Text = $"Done. {_surface.VertexCount} verts, " +
                                  $"{_surface.TriangleCount} tris.";

                _progressBar.Refresh();

                LogSuccess($"[Hirshfeld] {_surface.GetInfo()}");

                if (_lastDiagnostics != null)
                {
                    LogInfo($"[Hirshfeld] Diagnostics: " +
                            $"R_target={_lastDiagnostics.RTarget:F2}, " +
                            $"R_grid={_lastDiagnostics.RGrid:F2}, " +
                            $"target={_lastDiagnostics.TargetCount}, " +
                            $"outer={_lastDiagnostics.OuterCount}");
                }

                if (_fingerprintWindow != null && !_fingerprintWindow.IsDisposed)
                {
                    var analysis = BuildFingerprintAnalysis();
                    _vertexBins = analysis.VertexBins;
                    _fingerprintWindow.SetAnalysis(analysis);
                }
            }
            catch (OperationCanceledException)
            {
                _lblStatus.Text = "Cancelled.";
            }
            catch (Exception ex)
            {
                LogError($"[Hirshfeld] {ex.Message}");
                _lblStatus.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Hirshfeld error:\n{ex.Message}",
                    "Hirshfeld", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isGenerating = false;
                _btnGenerate.Enabled = true;
                _btnClear.Enabled = true;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void ClearSurface()
        {
            if (_surface != null)
                HirshfeldSmoothing.ClearCache(_surface);

            _surface = null;

            _vertexBins = null;
            _highlightedBin = (-1, -1);

            _lastDiagnostics = null;

            _surfaceVB?.Dispose(); _surfaceVB = null;
            _surfaceVertCount = 0;

            _surfaceIBOpaque?.Dispose(); _surfaceIBOpaque = null;
            _surfaceIBTransparent?.Dispose(); _surfaceIBTransparent = null;
            _surfaceIndexCount = 0;

            _wireframeVB?.Dispose(); _wireframeVB = null;
            _wireframeVertCount = 0;

            _cellVB?.Dispose(); _cellVB = null;
            _cellVertCount = 0;

            _surfaceVertexDataCache = null;

            _sortedIndices = null;
            _indicesSorted = false;

            _lastMoleculeShift = new Vector3(float.NaN, float.NaN, float.NaN);

            Text = "Hirshfeld Surface";
            _lblStatus.Text = "Cleared.";
            _progressBar.Value = 0;

            _activeContactFilter = null;
            _contactTypes = null;
            _highlightedBin = (-1, -1);
            _vertexBins = null;

            RenderFrame();
        }

        #endregion

        #region Save PLY

        private void SavePly()
        {
            if (_surface == null || !_surface.IsValid)
            {
                MessageBox.Show("No surface to save.", "Hirshfeld",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title = "Save Hirshfeld Surface as PLY",
                Filter = "PLY files (*.ply)|*.ply|All files (*.*)|*.*",
                FileName = $"hirshfeld_{_surface.TargetMoleculeId}.ply",
                DefaultExt = "ply"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                using var sw = new StreamWriter(dlg.FileName);

                int vc = _surface.VertexCount;
                int tc = _surface.TriangleCount;

                sw.WriteLine("ply");
                sw.WriteLine("format ascii 1.0");
                sw.WriteLine($"comment Hirshfeld surface, molecule #{_surface.TargetMoleculeId}");
                sw.WriteLine($"element vertex {vc}");
                sw.WriteLine("property float x");
                sw.WriteLine("property float y");
                sw.WriteLine("property float z");
                sw.WriteLine("property float nx");
                sw.WriteLine("property float ny");
                sw.WriteLine("property float nz");
                sw.WriteLine("property uchar red");
                sw.WriteLine("property uchar green");
                sw.WriteLine("property uchar blue");
                sw.WriteLine($"element face {tc}");
                sw.WriteLine("property list uchar int vertex_indices");
                sw.WriteLine("end_header");

                for (int i = 0; i < vc; i++)
                {
                    var v = _surface.Vertices[i];
                    var n = _surface.Normals != null && i < _surface.Normals.Count
                        ? _surface.Normals[i]
                        : Vector3.Zero;

                    var c = ResolveVertexColor(i);

                    sw.WriteLine(
                        $"{v.X:F6} {v.Y:F6} {v.Z:F6} " +
                        $"{n.X:F6} {n.Y:F6} {n.Z:F6} " +
                        $"{c.R} {c.G} {c.B}");
                }

                for (int i = 0; i < _surface.Indices.Count; i += 3)
                {
                    int a = _surface.Indices[i];
                    int b = _surface.Indices[i + 1];
                    int c = _surface.Indices[i + 2];
                    sw.WriteLine($"3 {a} {b} {c}");
                }

                _lblStatus.Text = $"Saved: {Path.GetFileName(dlg.FileName)}";
                LogSuccess($"[Hirshfeld] Saved PLY: {dlg.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed:\n{ex.Message}", "Hirshfeld",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Save PNG

        private void SavePng()
        {
            if (_device == null || _device.IsDisposed || !_deviceReady)
            {
                MessageBox.Show("Graphics device not ready.", "Hirshfeld",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_surface == null || !_surface.IsValid)
            {
                MessageBox.Show("Generate a surface first.", "Hirshfeld",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title = "Export view as PNG",
                Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*",
                FileName = $"hirshfeld_{_surface.TargetMoleculeId}.png",
                DefaultExt = "png"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            RenderTarget2D rt = null;

            try
            {
                int w = Math.Max(_panel.Width, 1);
                int h = Math.Max(_panel.Height, 1);

                const int SSAA = 2;
                int rw = w * SSAA;
                int rh = h * SSAA;

                rt = new RenderTarget2D(
                    _device,
                    rw, rh,
                    mipMap: false,
                    preferredFormat: SurfaceFormat.Color,
                    preferredDepthFormat: DepthFormat.Depth24Stencil8);

                _device.SetRenderTarget(rt);
                _device.Clear(Flags.Instance.color1);

                float aspect = rw / (float)rh;
                if (aspect < 0.001f) aspect = 1f;

                Matrix world = _camera.Rotate * _camera.Translate;
                Matrix view = Matrix.CreateLookAt(
                    new Vector3(0, 0, 100), Vector3.Zero, Vector3.Up);

                Matrix proj = Flags.Instance.PerspectiveMode
                    ? Matrix.CreatePerspectiveFieldOfView(
                        MathHelper.PiOver4, aspect, 0.1f, 10000f)
                    : Matrix.CreateOrthographic(80 * aspect, 80, 0.1f, 10000f);

                if (_surfaceEffect != null)
                {
                    _surfaceEffect.World = world;
                    _surfaceEffect.View = view;
                    _surfaceEffect.Projection = proj;
                }

                if (_showMolecule)
                {
                    DrawMoleculeTALStyle(world, view, proj);
                }

                if (_showMesh
                    && _surfaceVB != null && _surfaceVertCount > 0
                    && _surfaceIndexCount >= 3
                    && _surfaceIBOpaque != null
                    && _surfaceIBTransparent != null)
                {
                    _device.Indices = _surfaceIBTransparent;
                    _device.SetVertexBuffer(_surfaceVB);

                    if (_hirshfeldSurfaceEffect != null)
                    {
                        DrawSurfaceWithShader(world, view, proj);
                    }
                    else if (_surfaceEffect != null)
                    {
                        bool opaque = _alphaValue >= 240;

                        _device.BlendState = opaque
                            ? BlendState.Opaque
                            : BlendState.AlphaBlend;
                        _device.DepthStencilState = opaque
                            ? _depthDefaultWrite
                            : _depthReadNoWrite;
                        _device.RasterizerState = RasterizerState.CullNone;

                        foreach (var pass in _surfaceEffect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            _device.DrawIndexedPrimitives(
                                PrimitiveType.TriangleList,
                                0, 0,
                                _surfaceVertCount,
                                0,
                                _surfaceIndexCount / 3);
                        }
                    }

                    _device.Indices = null;
                }

                if (_showWireframe && _wireframeVB != null && _wireframeVertCount > 0)
                {
                    _device.DepthStencilState = DepthStencilState.DepthRead;
                    _device.BlendState = BlendState.AlphaBlend;
                    _device.RasterizerState = RasterizerState.CullNone;

                    _effect.World = world;
                    _effect.View = view;
                    _effect.Projection = proj;

                    _device.SetVertexBuffer(_wireframeVB);
                    foreach (var pass in _effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _device.DrawPrimitives(PrimitiveType.LineList, 0,
                            _wireframeVertCount / 2);
                    }
                    _device.DepthStencilState = _depthDefaultWrite;
                }

                if (_showAxes && _axesVB != null && _axesVertCount > 0)
                {
                    _device.DepthStencilState = DepthStencilState.None;
                    _device.BlendState = BlendState.Opaque;
                    _device.RasterizerState = RasterizerState.CullNone;

                    _effect.World = world;
                    _effect.View = view;
                    _effect.Projection = proj;

                    _device.SetVertexBuffer(_axesVB);
                    foreach (var pass in _effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _device.DrawPrimitives(PrimitiveType.LineList, 0, _axesVertCount / 2);
                    }
                    _device.DepthStencilState = _depthDefaultWrite;
                }

                if (_showCell && _cellVB != null && _cellVertCount > 0)
                {
                    _device.DepthStencilState = DepthStencilState.None;
                    _device.BlendState = BlendState.Opaque;
                    _device.RasterizerState = RasterizerState.CullNone;

                    _effect.World = world;
                    _effect.View = view;
                    _effect.Projection = proj;

                    _device.SetVertexBuffer(_cellVB);
                    foreach (var pass in _effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _device.DrawPrimitives(PrimitiveType.LineList, 0, _cellVertCount / 2);
                    }
                    _device.DepthStencilState = _depthDefaultWrite;
                }

                _device.SetRenderTarget(null);

                using var fs = new FileStream(dlg.FileName, FileMode.Create);
                rt.SaveAsPng(fs, rw, rh);

                _lblStatus.Text = $"Saved: {Path.GetFileName(dlg.FileName)} ({rw}×{rh})";
                LogSuccess($"[Hirshfeld] Saved PNG: {dlg.FileName} ({rw}×{rh})");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save PNG failed:\n{ex.Message}", "Hirshfeld",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogError($"[Hirshfeld] Save PNG error: {ex.Message}");
            }
            finally
            {
                try { rt?.Dispose(); } catch { }
                try { _device?.SetRenderTarget(null); } catch { }
            }
        }

        #endregion

        #region Graphics Init

        private void InitializeGraphics()
        {
            if (_deviceReady) return;

            try
            {
                int w = Math.Max(_panel.Width, 1);
                int h = Math.Max(_panel.Height, 1);

                var pp = new PresentationParameters
                {
                    IsFullScreen = false,
                    BackBufferWidth = w,
                    BackBufferHeight = h,
                    BackBufferFormat = SurfaceFormat.Color,
                    DepthStencilFormat = DepthFormat.Depth24Stencil8,
                    DeviceWindowHandle = _panel.Handle,
                    PresentationInterval = PresentInterval.Immediate,
                    MultiSampleCount = 4
                };

                _device = new GraphicsDevice(
                    GraphicsAdapter.DefaultAdapter,
                    GraphicsProfile.Reach,
                    pp);

                _depthReadNoWrite = new DepthStencilState
                {
                    DepthBufferEnable = true,
                    DepthBufferWriteEnable = false,
                    DepthBufferFunction = CompareFunction.LessEqual,
                    StencilEnable = false,
                };

                _depthDefaultWrite = new DepthStencilState
                {
                    DepthBufferEnable = true,
                    DepthBufferWriteEnable = true,
                    DepthBufferFunction = CompareFunction.LessEqual,
                    StencilEnable = false,
                };

                _effect = new BasicEffect(_device)
                {
                    VertexColorEnabled = true,
                    LightingEnabled = false,
                    TextureEnabled = false
                };

                _spriteBatch = new SpriteBatch(_device);

                _pixelTexture = new Texture2D(_device, 1, 1);
                _pixelTexture.SetData(new[] { Color.White });

                LoadAtomModels();
                InitAtomEffect();
                InitSurfaceEffect();
                LoadHirshfeldSurfaceEffect();

                LoadLocalSpriteFont();

                _camera = new Camera();
                _camera.GraphicsDevice = _device;
                _camera.Initialize();
                _camera.g_cameraPos = new Vector3(0, 0, 100);
                _camera.g_targetPos = Vector3.Zero;
                _camera.zoom = 0.5f;
                _camera.OneOverZoom = 2.0f;
                _camera.MarkChanged();
                _camera.ApplyEffects();

                _deviceReady = true;

                LogSuccess($"[Hirshfeld] Graphics initialized ({w}x{h})");

                if (_surface != null)
                {
                    RebuildSurfaceBuffer();
                    RebuildWireframeBuffer();
                    RebuildAxesBuffer();
                    RebuildCellBuffer();
                }
                else
                {
                    RebuildAxesBuffer();
                }
            }
            catch (Exception ex)
            {
                LogError($"[Hirshfeld] Init error: {ex.Message}");
                MessageBox.Show($"Graphics init error: {ex.Message}",
                    "Hirshfeld", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadAtomModels()
        {
            if (_device == null || _device.IsDisposed) return;

            try
            {
                _smallBall = RuntimeIcosphere.Create(_device, subdivisionLevel: 3,
                                                      radius: RuntimeIcosphere.BASE_RADIUS);
                _bigBall = RuntimeIcosphere.Create(_device, subdivisionLevel: 5,
                                                    radius: RuntimeIcosphere.BASE_RADIUS);

                _smallRod = LoadModelFromContent("rod");

                if (_smallBall != null)
                {
                    _smallBallTransforms = new Matrix[_smallBall.Bones.Count];
                    _smallBall.CopyAbsoluteBoneTransformsTo(_smallBallTransforms);
                    _smallBallMeshes = _smallBall.Meshes.ToArray();
                }

                if (_bigBall != null)
                {
                    _bigBallTransforms = new Matrix[_bigBall.Bones.Count];
                    _bigBall.CopyAbsoluteBoneTransformsTo(_bigBallTransforms);
                    _bigBallMeshes = _bigBall.Meshes.ToArray();
                }

                if (_smallRod != null)
                {
                    _smallRodTransforms = new Matrix[_smallRod.Bones.Count];
                    _smallRod.CopyAbsoluteBoneTransformsTo(_smallRodTransforms);
                    _smallRodMeshes = _smallRod.Meshes.ToArray();
                }

                LogSuccess($"[Hirshfeld] Models loaded: ball={_smallBallMeshes?.Length ?? 0}, " +
                           $"bigBall={_bigBallMeshes?.Length ?? 0}, rod={_smallRodMeshes?.Length ?? 0}");
            }
            catch (Exception ex)
            {
                LogError($"[Hirshfeld] LoadAtomModels error: {ex.Message}");
            }
        }

        private Model LoadModelFromContent(string assetName)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? baseDir;

                string[] candidates = new[]
                {
                    Path.Combine(exeDir, assetName + ".xnb"),
                    Path.Combine(baseDir, assetName + ".xnb"),
                    Path.Combine(exeDir, "Content", assetName + ".xnb"),
                    Path.Combine(baseDir, "Content", assetName + ".xnb"),
                    Path.Combine(exeDir, "..", "..", "..", "Content", assetName + ".xnb"),
                    Path.Combine(baseDir, "..", "..", "..", "Content", assetName + ".xnb"),
                };

                foreach (var candidate in candidates)
                {
                    try
                    {
                        if (!File.Exists(candidate)) continue;

                        string fullPath = Path.GetFullPath(candidate);
                        string contentDir = Path.GetDirectoryName(fullPath);

                        var services = new ServiceContainer();
                        services.AddService<IGraphicsDeviceService>(
                            new SimpleGraphicsDeviceService(_device));

                        var cm = new ContentManager(services, contentDir);
                        return cm.Load<Model>(assetName);
                    }
                    catch { }
                }

                LogWarn($"[Hirshfeld] Model '{assetName}.xnb' not found");
                return null;
            }
            catch (Exception ex)
            {
                LogWarn($"[Hirshfeld] LoadModelFromContent('{assetName}') failed: {ex.Message}");
                return null;
            }
        }

        private void InitAtomEffect()
        {
            _atomEffect = new BasicEffect(_device)
            {
                VertexColorEnabled = false,
                LightingEnabled = true,
                PreferPerPixelLighting = true,
                TextureEnabled = false,
                View = Matrix.Identity,
                Projection = Matrix.Identity,
                World = Matrix.Identity,
                AmbientLightColor = Vector3.One * Flags.Instance.AmbientIntensity,
                DiffuseColor = Vector3.One,
                SpecularColor = Flags.Instance.SpecularColor,
                SpecularPower = Flags.Instance.SpecularPower,
                Alpha = 1.0f,
            };

            _atomEffect.DirectionalLight0.Enabled = true;
            _atomEffect.DirectionalLight0.Direction = Vector3.Normalize(Flags.Instance.LightDirection);
            _atomEffect.DirectionalLight0.DiffuseColor = Vector3.One * Flags.Instance.DiffuseIntensity;
            _atomEffect.DirectionalLight0.SpecularColor = Flags.Instance.SpecularColor;

            _atomEffect.DirectionalLight1.Enabled = Flags.Instance.Light2Enabled;
            _atomEffect.DirectionalLight1.Direction = Vector3.Normalize(Flags.Instance.Light2Direction);
            _atomEffect.DirectionalLight1.DiffuseColor = Flags.Instance.Light2DiffuseColor;
            _atomEffect.DirectionalLight1.SpecularColor = Flags.Instance.Light2SpecularColor;
        }

        private void InitSurfaceEffect()
        {
            _surfaceEffect = new BasicEffect(_device)
            {
                VertexColorEnabled = true,
                LightingEnabled = true,
                PreferPerPixelLighting = true,
                TextureEnabled = false,
                AmbientLightColor = Vector3.One * Flags.Instance.AmbientIntensity,
                DiffuseColor = Vector3.One,
                SpecularColor = Flags.Instance.SpecularColor * 0.5f,
                SpecularPower = Flags.Instance.SpecularPower,
                Alpha = 1.0f,
            };

            _surfaceEffect.DirectionalLight0.Enabled = true;
            _surfaceEffect.DirectionalLight0.Direction = Vector3.Normalize(Flags.Instance.LightDirection);
            _surfaceEffect.DirectionalLight0.DiffuseColor = Vector3.One * Flags.Instance.DiffuseIntensity;
            _surfaceEffect.DirectionalLight0.SpecularColor = Flags.Instance.SpecularColor * 0.5f;

            _surfaceEffect.DirectionalLight1.Enabled = Flags.Instance.Light2Enabled;
            _surfaceEffect.DirectionalLight1.Direction = Vector3.Normalize(Flags.Instance.Light2Direction);
            _surfaceEffect.DirectionalLight1.DiffuseColor = Flags.Instance.Light2DiffuseColor;
            _surfaceEffect.DirectionalLight1.SpecularColor = Flags.Instance.Light2SpecularColor * 0.3f;

            _surfaceEffect.DirectionalLight2.Enabled = true;
            Vector3 side = Vector3.Cross(Flags.Instance.LightDirection, Vector3.Up);
            if (side.LengthSquared() > 1e-6f)
                side.Normalize();
            else
                side = Vector3.Right;
            _surfaceEffect.DirectionalLight2.Direction = side;
            _surfaceEffect.DirectionalLight2.DiffuseColor = Vector3.One * Flags.Instance.DiffuseIntensity * 0.25f;
            _surfaceEffect.DirectionalLight2.SpecularColor = Vector3.Zero;
        }

        private void LoadHirshfeldSurfaceEffect()
        {
            if (_device == null || _device.IsDisposed) return;

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? baseDir;

                string[] candidates = new[]
                {
                    Path.Combine(exeDir, "Effects", "HirshfeldSurface.xnb"),
                    Path.Combine(baseDir, "Effects", "HirshfeldSurface.xnb"),
                    Path.Combine(exeDir, "Content", "Effects", "HirshfeldSurface.xnb"),
                    Path.Combine(baseDir, "Content", "Effects", "HirshfeldSurface.xnb"),
                    Path.Combine(exeDir, "..", "..", "..", "Content", "Effects", "HirshfeldSurface.xnb"),
                    Path.Combine(baseDir, "..", "..", "..", "Content", "Effects", "HirshfeldSurface.xnb"),
                };

                string fxFile = null;
                foreach (var c in candidates)
                {
                    try
                    {
                        if (File.Exists(c))
                        {
                            fxFile = Path.GetFullPath(c);
                            break;
                        }
                    }
                    catch { }
                }

                if (fxFile == null)
                {
                    LogWarn("[Hirshfeld] HirshfeldSurface.xnb not found — using BasicEffect fallback");
                    return;
                }

                string contentDir = Path.GetDirectoryName(Path.GetDirectoryName(fxFile));
                string assetName = "Effects/HirshfeldSurface";

                var services = new ServiceContainer();
                services.AddService<IGraphicsDeviceService>(
                    new SimpleGraphicsDeviceService(_device));

                var cm = new ContentManager(services, contentDir);
                _hirshfeldSurfaceEffect = cm.Load<Effect>(assetName);

                LogSuccess($"[Hirshfeld] Shader loaded: {fxFile}");
            }
            catch (Exception ex)
            {
                LogWarn($"[Hirshfeld] Shader load failed: {ex.Message} — using BasicEffect fallback");
                _hirshfeldSurfaceEffect = null;
            }
        }

        private void LoadLocalSpriteFont()
        {
            if (_device == null) return;

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? baseDir;

                string[] fontNames = { "SpriteFont1", "SpriteFont" };

                string fontFile = null;
                string assetName = null;

                foreach (var name in fontNames)
                {
                    string[] candidates = new[]
                    {
                        Path.Combine(exeDir, name + ".xnb"),
                        Path.Combine(baseDir, name + ".xnb"),
                        Path.Combine(exeDir, "Content", name + ".xnb"),
                        Path.Combine(baseDir, "Content", name + ".xnb"),
                        Path.Combine(exeDir, "..", "..", "..", "Content", name + ".xnb"),
                        Path.Combine(baseDir, "..", "..", "..", "Content", name + ".xnb"),
                    };

                    foreach (var candidate in candidates)
                    {
                        try
                        {
                            if (File.Exists(candidate))
                            {
                                fontFile = Path.GetFullPath(candidate);
                                assetName = name;
                                break;
                            }
                        }
                        catch { }
                    }

                    if (fontFile != null) break;
                }

                if (fontFile == null)
                {
                    LogWarn("[Hirshfeld] SpriteFont1.xnb / SpriteFont.xnb not found");
                    return;
                }

                string contentDir = Path.GetDirectoryName(fontFile);

                var services = new ServiceContainer();
                services.AddService<IGraphicsDeviceService>(
                    new SimpleGraphicsDeviceService(_device));

                _content = new ContentManager(services, contentDir);
                _spriteFont = _content.Load<SpriteFont>(assetName);

                LogSuccess($"[Hirshfeld] SpriteFont loaded: {fontFile}");
            }
            catch (Exception ex)
            {
                LogWarn($"[Hirshfeld] SpriteFont load failed: {ex.Message}");
                _spriteFont = null;
            }
        }

        #endregion

        #region Vertex buffers — surface

        private void RebuildSurfaceBuffer()
        {
            if (_surface == null || _device == null || _device.IsDisposed) return;

            int n = _surface.VertexCount;
            if (n == 0 || _surface.Indices == null || _surface.Indices.Count < 3)
            {
                _surfaceVB?.Dispose(); _surfaceVB = null;
                _surfaceIBOpaque?.Dispose(); _surfaceIBOpaque = null;
                _surfaceIBTransparent?.Dispose(); _surfaceIBTransparent = null;
                _surfaceVertCount = 0;
                _surfaceIndexCount = 0;
                _surfaceVertexDataCache = null;
                return;
            }

            float r = _surface.BoundingRadius;
            _sceneScale = r > 0.01f ? (30f / r) : 1.0f;
            float s = _sceneScale * _zoomFactor;

            Vector3 shift = _molecule?.MoleculeShift ?? Vector3.Zero;

            Vector3[] normals = EnsureSurfaceNormals();

            var verts = new VertexPositionNormalColor4[n];
            _surfaceVertexDataCache = verts;

            for (int i = 0; i < n; i++)
            {
                Vector3 correctedPos = _surface.Vertices[i] - shift;
                Vector3 pos = correctedPos * s;

                Color c = ResolveVertexColor(i);
                Vector4 colVec = new Vector4(
                    c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);

                verts[i] = new VertexPositionNormalColor4(
                    pos, normals[i], colVec);
            }

            if (_highlightedBin.binDe >= 0)
            {
                if (_vertexBins == null)
                {
                    LogWarn("[Hirshfeld.DEBUG] _highlightedBin set but _vertexBins = NULL");
                }
                else
                {
                    int matchCount = 0;
                    for (int i = 0; i < n && i < _vertexBins.Length; i++)
                    {
                        if (_vertexBins[i].binDe == _highlightedBin.binDe
                            && _vertexBins[i].binDi == _highlightedBin.binDi)
                            matchCount++;
                    }

                    LogInfo($"[Hirshfeld.DEBUG] Highlight ({_highlightedBin.binDe},{_highlightedBin.binDi}) " +
                            $"→ {matchCount}/{n} vertices match");
                }
            }

            _surfaceVB?.Dispose();
            _surfaceVB = new VertexBuffer(_device,
                VertexPositionNormalColor4.VertexDeclaration,
                n, BufferUsage.WriteOnly);
            _surfaceVB.SetData(verts);
            _surfaceVertCount = n;

            var indices = _surface.Indices.ToArray();
            _surfaceIndexCount = indices.Length;

            _surfaceIBOpaque?.Dispose();
            _surfaceIBOpaque = new IndexBuffer(_device, IndexElementSize.ThirtyTwoBits,
                indices.Length, BufferUsage.WriteOnly);
            _surfaceIBOpaque.SetData(indices);

            _surfaceIBTransparent?.Dispose();
            _surfaceIBTransparent = new IndexBuffer(_device, IndexElementSize.ThirtyTwoBits,
                indices.Length, BufferUsage.WriteOnly);
            _surfaceIBTransparent.SetData(indices);

            if (_sortedIndices == null || _sortedIndices.Length != indices.Length)
                _sortedIndices = new int[indices.Length];

            _indicesSorted = false;
        }

        private Vector3[] EnsureSurfaceNormals()
        {
            int n = _surface.VertexCount;

            if (_surface.Normals != null && _surface.Normals.Count == n)
            {
                bool anyNonZero = false;
                for (int i = 0; i < n; i++)
                {
                    if (_surface.Normals[i].LengthSquared() > 1e-6f)
                    {
                        anyNonZero = true;
                        break;
                    }
                }
                if (anyNonZero)
                    return _surface.Normals.ToArray();
            }

            var normals = new Vector3[n];
            var verts = _surface.Vertices;
            var inds = _surface.Indices;

            for (int i = 0; i < inds.Count; i += 3)
            {
                int ia = inds[i], ib = inds[i + 1], ic = inds[i + 2];
                if (ia >= n || ib >= n || ic >= n) continue;

                Vector3 a = verts[ia], b = verts[ib], c = verts[ic];
                Vector3 nrm = Vector3.Cross(b - a, c - a);
                float lenSq = nrm.LengthSquared();
                if (lenSq < 1e-9f) continue;

                normals[ia] += nrm;
                normals[ib] += nrm;
                normals[ic] += nrm;
            }

            for (int i = 0; i < n; i++)
            {
                float len = normals[i].Length();
                normals[i] = len > 1e-9f ? normals[i] / len : Vector3.UnitY;
                normals[i] = -normals[i];
            }

            _surface.Normals = new List<Vector3>(normals);
            return normals;
        }

        private Color ResolveVertexColor(int vertexIndex)
        {
            byte a = _alphaValue;
            byte r, g, b;

 // ═══ 1) Highlight binu ═══
            if (_highlightedBin.binDe >= 0 && _vertexBins != null
                && vertexIndex < _vertexBins.Length
                && _vertexBins[vertexIndex].binDe == _highlightedBin.binDe
                && _vertexBins[vertexIndex].binDi == _highlightedBin.binDi)
            {
                return new Color((byte)255, (byte)255, (byte)0, a);
            }

 // ═══ 2) Gray-out outside filtrem ═══
            if (_activeContactFilter != null
                && _contactTypes != null
                && vertexIndex < _contactTypes.Length)
            {
                string typ = _contactTypes[vertexIndex];
                bool pass = _activeContactFilter.Count > 0
                            && !string.IsNullOrEmpty(typ)
                            && _activeContactFilter.Contains(typ);

                if (!pass)
                {
                    byte grayA = (byte)Math.Min((int)a, 120);
                    return new Color((byte)180, (byte)180, (byte)180, grayA);
                }
            }

 // ═══ 3) Maafterwanie property ═══
            switch (_propertyMode)
            {
                case HirshfeldPropertyMode.BaseColor:
                    r = 100; g = 170; b = 240;
                    break;

                case HirshfeldPropertyMode.DNorm:
                    {
                        float d = _surface.DNorm != null && vertexIndex < _surface.DNorm.Length
                            ? _surface.DNorm[vertexIndex] : 0f;

                        float clampMax = Flags.Instance.HirshfeldDNormColorClamp;
                        if (d < -clampMax) d = -clampMax;
                        if (d > clampMax) d = clampMax;

                        float scale = Math.Max(1e-4f, Flags.Instance.HirshfeldDNormScale);
                        float whiteBand = 0f;  // white band removed — always 0

                        float absD = MathF.Abs(d);

                        if (absD <= whiteBand)
                        {
                            r = g = b = 255;
                        }
                        else
                        {
                            float t = Math.Clamp(
                                (absD - whiteBand) / (scale - whiteBand),
                                0f, 1f);

                            if (d < 0f)
                            {
                                r = 255;
                                g = (byte)(255f * (1f - t));
                                b = (byte)(255f * (1f - t));
                            }
                            else
                            {
                                r = (byte)(255f * (1f - t));
                                g = (byte)(255f * (1f - t));
                                b = 255;
                            }
                        }
                    }
                    break;

                case HirshfeldPropertyMode.DE:
                    {
                        float d = _surface.DE != null && vertexIndex < _surface.DE.Length
                            ? _surface.DE[vertexIndex] : 0f;

                        float range = 2.5f * Math.Clamp(_thresholdGamma, 0.5f, 2.0f);
                        float t = Math.Clamp(d / range, 0f, 1f);
                        t = t * t * (3f - 2f * t);

                        if (t < 0.33f)
                        {
                            float u = t / 0.33f;
                            r = (byte)(255 * u);
                            g = 0;
                            b = 0;
                        }
                        else if (t < 0.66f)
                        {
                            float u = (t - 0.33f) / 0.33f;
                            r = 255;
                            g = (byte)(255 * u);
                            b = 0;
                        }
                        else
                        {
                            float u = (t - 0.66f) / 0.34f;
                            r = 255;
                            g = 255;
                            b = (byte)(255 * u);
                        }
                    }
                    break;

                case HirshfeldPropertyMode.DI:
                    {
                        float d = _surface.DI != null && vertexIndex < _surface.DI.Length
                            ? _surface.DI[vertexIndex] : 0f;

                        float range = 2.5f * Math.Clamp(_thresholdGamma, 0.5f, 2.0f);
                        float t = Math.Clamp(d / range, 0f, 1f);
                        t = t * t * (3f - 2f * t);

                        if (t < 0.33f)
                        {
                            float u = t / 0.33f;
                            r = 0;
                            g = (byte)(255 * u);
                            b = 0;
                        }
                        else if (t < 0.66f)
                        {
                            float u = (t - 0.33f) / 0.33f;
                            r = 0;
                            g = 255;
                            b = (byte)(255 * u);
                        }
                        else
                        {
                            float u = (t - 0.66f) / 0.34f;
                            r = (byte)(255 * u);
                            g = 255;
                            b = 255;
                        }
                    }
                    break;

                case HirshfeldPropertyMode.Charge:
                    {
                        float q = _surface.Charge != null && vertexIndex < _surface.Charge.Length
                            ? _surface.Charge[vertexIndex] : 0f;

                        float qMin = _surface.ChargeMin;
                        float qMax = _surface.ChargeMax;
                        if (qMax - qMin < 1e-4f) { qMin = -0.5f; qMax = +0.5f; }

                        float mid = (qMin + qMax) * 0.5f;
                        float half = MathF.Max(MathF.Abs(qMax - mid), MathF.Abs(qMin - mid));
                        if (half < 1e-4f) half = 0.5f;

                        float t = Math.Clamp((q - mid) / half, -1f, 1f);

                        if (t < 0f)
                        {
                            float u = -t;
                            r = (byte)(0 + 255 * u);
                            g = (byte)(200 + 55 * u);
                            b = 0;
                        }
                        else
                        {
                            float u = t;
                            r = 255;
                            g = (byte)(255 * (1f - u));
                            b = 0;
                        }
                    }
                    break;

                case HirshfeldPropertyMode.Hydrophobicity:
                    {
                        float h = _surface.Hydrophobicity != null
                            && vertexIndex < _surface.Hydrophobicity.Length
                            ? _surface.Hydrophobicity[vertexIndex] : 0f;

                        float hMin = _surface.HydrophobicityMin;
                        float hMax = _surface.HydrophobicityMax;
                        if (hMax - hMin < 1e-4f) { hMin = -0.5f; hMax = +0.5f; }

                        float mid = (hMin + hMax) * 0.5f;
                        float half = MathF.Max(MathF.Abs(hMax - mid), MathF.Abs(hMin - mid));
                        if (half < 1e-4f) half = 0.5f;

                        float t = Math.Clamp((h - mid) / half, -1f, 1f);

                        if (t < 0f)
                        {
                            float u = -t;
                            r = (byte)(50 + 205 * u);
                            g = (byte)(100 + 155 * u);
                            b = 255;
                        }
                        else
                        {
                            float u = t;
                            r = (byte)(255 - 115 * u);
                            g = (byte)(255 - 175 * u);
                            b = (byte)(255 - 225 * u);
                        }
                    }
                    break;

                default:
                    r = g = b = 200;
                    break;
            }

            return new Color(r, g, b, a);
        }

        private (float yaw, float pitch) GetCameraAngles()
        {
            if (_camera == null) return (0f, 0f);

            Matrix r = _camera.Rotate;

            float pitch = MathF.Asin(Math.Clamp(-r.M32, -1f, 1f));
            float yaw = MathF.Atan2(r.M31, r.M33);

            return (yaw, pitch);
        }

        private bool HasCameraRotatedEnough()
        {
            var (yaw, pitch) = GetCameraAngles();

            float dyaw = MathF.Abs(yaw - _lastSortYaw);
            float dpitch = MathF.Abs(pitch - _lastSortPitch);

            if (dyaw > MathF.PI)
                dyaw = 2 * MathF.PI - dyaw;

            float thresholdRad = SORT_ANGLE_THRESHOLD_DEG * MathF.PI / 180f;

            return dyaw > thresholdRad || dpitch > thresholdRad;
        }

        private void SaveSortAngles()
        {
            var (yaw, pitch) = GetCameraAngles();
            _lastSortYaw = yaw;
            _lastSortPitch = pitch;
        }

        private void SortSurfaceTrianglesBackToFront(Matrix world, Matrix view)
        {
            if (_surface == null || !_surface.IsValid) return;
            if (_surfaceIBTransparent == null) return;
            if (_surfaceVertexDataCache == null) return;

            int triCount = _surfaceIndexCount / 3;
            if (triCount == 0) return;

            if (_sortedIndices == null || _sortedIndices.Length != _surfaceIndexCount)
                _sortedIndices = new int[_surfaceIndexCount];

            Matrix wv = world * view;

            var triZ = new (int idx, float z)[triCount];
            var inds = _surface.Indices;

            for (int t = 0; t < triCount; t++)
            {
                int i0 = inds[t * 3];
                int i1 = inds[t * 3 + 1];
                int i2 = inds[t * 3 + 2];

                Vector3 c0 = _surfaceVertexDataCache[i0].Position;
                Vector3 c1 = _surfaceVertexDataCache[i1].Position;
                Vector3 c2 = _surfaceVertexDataCache[i2].Position;

                Vector3 center = (c0 + c1 + c2) / 3f;
                Vector3 viewPos = Vector3.Transform(center, wv);

                triZ[t] = (t, viewPos.Z);
            }

            Array.Sort(triZ, (a, b) => a.z.CompareTo(b.z));

            for (int t = 0; t < triCount; t++)
            {
                int src = triZ[t].idx * 3;
                int dst = t * 3;

                _sortedIndices[dst] = inds[src];
                _sortedIndices[dst + 1] = inds[src + 1];
                _sortedIndices[dst + 2] = inds[src + 2];
            }

            _surfaceIBTransparent.SetData(_sortedIndices);

            _indicesSorted = true;
        }

        private void RebuildWireframeBuffer()
        {
            if (_surface == null || _device == null || _device.IsDisposed)
            {
                _wireframeVB?.Dispose();
                _wireframeVB = null;
                _wireframeVertCount = 0;
                return;
            }

            if (_surface.TriangleCount > 200000)
            {
                LogWarn("[Hirshfeld] Wireframe skipped — too many triangles");
                _wireframeVB?.Dispose();
                _wireframeVB = null;
                _wireframeVertCount = 0;
                return;
            }

            var edges = new HashSet<(int, int)>();
            var indices = _surface.Indices;

            for (int i = 0; i < indices.Count; i += 3)
            {
                int a = indices[i];
                int b = indices[i + 1];
                int c = indices[i + 2];

                void AddEdge(int v1, int v2)
                {
                    int lo = Math.Min(v1, v2);
                    int hi = Math.Max(v1, v2);
                    edges.Add((lo, hi));
                }

                AddEdge(a, b);
                AddEdge(b, c);
                AddEdge(c, a);
            }

            var lineVerts = new List<VertexPositionColor>(edges.Count * 2);
            Color lc = new Color((byte)40, (byte)40, (byte)40, (byte)200);

            float s = _sceneScale * _zoomFactor;
            Vector3 shift = _molecule?.MoleculeShift ?? Vector3.Zero;

            foreach (var (v1, v2) in edges)
            {
                var p1 = (_surface.Vertices[v1] - shift) * s;
                var p2 = (_surface.Vertices[v2] - shift) * s;
                lineVerts.Add(new VertexPositionColor(p1, lc));
                lineVerts.Add(new VertexPositionColor(p2, lc));
            }

            _wireframeVB?.Dispose();
            _wireframeVB = new VertexBuffer(_device, VertexPositionColor.VertexDeclaration,
                lineVerts.Count, BufferUsage.WriteOnly);
            _wireframeVB.SetData(lineVerts.ToArray());
            _wireframeVertCount = lineVerts.Count;
        }

        #endregion

        #region Molecule rendering — TAL style

        private void DrawMoleculeTALStyle(Matrix world, Matrix view, Matrix proj)
        {
            if (_molecule == null || _device == null || _device.IsDisposed) return;
            if (_atomEffect == null) return;

            if (_smallBallMeshes == null || _smallRodMeshes == null || _bigBallMeshes == null)
            {
                LoadAtomModels();
                if (_smallBallMeshes == null || _smallRodMeshes == null) return;
            }

            float s = _sceneScale * _zoomFactor;
            Matrix scaleRotate = Matrix.CreateScale(s, s, s) * world;

            int atomCount = _molecule.NumberOfAtoms;
            Matrix sortMatrix = world * view * proj;

            if (_sortDirty || sortMatrix != _lastSortMatrix ||
                _zDepthCache == null || _zDepthCache.Length != atomCount)
            {
                OrderAtoms3D(sortMatrix, atomCount);
                _lastSortMatrix = sortMatrix;
                _sortDirty = false;
            }

            _atomEffect.View = view;
            _atomEffect.Projection = proj;

            _atomEffect.AmbientLightColor = Vector3.One * Flags.Instance.AmbientIntensity;
            _atomEffect.SpecularColor = Flags.Instance.SpecularColor;
            _atomEffect.SpecularPower = Flags.Instance.SpecularPower;
            _atomEffect.Alpha = 1.0f;

            _atomEffect.DirectionalLight0.Enabled = Flags.Instance.Enabled;
            _atomEffect.DirectionalLight0.Direction = Vector3.Normalize(Flags.Instance.LightDirection);
            _atomEffect.DirectionalLight0.DiffuseColor = Vector3.One * Flags.Instance.DiffuseIntensity;
            _atomEffect.DirectionalLight0.SpecularColor = Flags.Instance.SpecularColor;

            if (Flags.Instance.Light2Enabled)
            {
                _atomEffect.DirectionalLight1.Enabled = true;
                _atomEffect.DirectionalLight1.Direction = Vector3.Normalize(Flags.Instance.Light2Direction);
                _atomEffect.DirectionalLight1.DiffuseColor = Flags.Instance.Light2DiffuseColor;
                _atomEffect.DirectionalLight1.SpecularColor = Flags.Instance.Light2SpecularColor;
            }
            else
            {
                _atomEffect.DirectionalLight1.Enabled = false;
            }

            _device.DepthStencilState = _depthDefaultWrite;
            _device.BlendState = BlendState.Opaque;
            _device.RasterizerState = RasterizerState.CullCounterClockwise;

            int presentation = Flags.Instance.Presentation;

            for (int i = 0; i < atomCount; i++)
            {
                int atomIdx = _zDepthCache[i].index;
                if (atomIdx < 0) continue;

                var atom = _molecule.atom[atomIdx];
                if (!atom.show) continue;

                Vector3 pos = atom.Coordinate;
                Vector3 color = GetAtomColorVec(atom);

                _atomEffect.DiffuseColor = color;
                _atomEffect.AmbientLightColor = color * Flags.Instance.AmbientIntensity;
                _atomEffect.DirectionalLight0.DiffuseColor = color * Flags.Instance.DiffuseIntensity;
                if (_atomEffect.DirectionalLight1.Enabled)
                    _atomEffect.DirectionalLight1.DiffuseColor = color * Flags.Instance.Light2DiffuseColor;

                switch (presentation)
                {
                    case 0:
                        DrawAtomCPK(atom, pos, scaleRotate);
                        break;

                    case 1:
                        DrawAtomSticks(atomIdx, scaleRotate);
                        break;

                    case 2:
                        DrawAtomSticks(atomIdx, scaleRotate);
                        DrawAtomBalls(atom, pos, scaleRotate);
                        break;

                    case 3:
                        DrawAtomSticks(atomIdx, scaleRotate);
                        DrawAtomEllipsoid(atom, pos, scaleRotate);
                        break;

                    default:
                        DrawAtomBalls(atom, pos, scaleRotate);
                        break;
                }
            }
        }

        private void OrderAtoms3D(Matrix rotateViewProjMatrix, int atomCount)
        {
            if (_zDepthCache == null || _zDepthCache.Length != atomCount)
                _zDepthCache = new (int index, float z)[atomCount];

            for (int i = 0; i < atomCount; i++)
            {
                if (!_molecule.atom[i].show)
                {
                    _zDepthCache[i] = (-1, float.MinValue);
                    continue;
                }

                var v = Vector3.Transform(_molecule.atom[i].Coordinate, rotateViewProjMatrix);
                _zDepthCache[i] = (i, v.Z);
            }

            Array.Sort(_zDepthCache, (a, b) => b.z.CompareTo(a.z));
        }

        private void DrawAtomCPK(Molecule.Atom atom, Vector3 pos, Matrix scaleRotate)
        {
            float size = _molecule.GetVdwRadius(atom.AtomicNumber) / RuntimeIcosphere.BASE_RADIUS;

            Matrix atomMatrix = Matrix.CreateScale(size, size, size)
 * Matrix.CreateTranslation(pos)
 * scaleRotate;

            DrawModelWithEffect(_bigBallMeshes, _bigBallTransforms, atomMatrix);
        }

        private void DrawAtomBalls(Molecule.Atom atom, Vector3 pos, Matrix scaleRotate)
        {
            float baseRadius = IsMetal(atom.AtomicNumber)
                ? Flags.Instance.BallRadiusMetal
                : Flags.Instance.BallRadiusNonMetal;

            if (atom.AtomicNumber == 1)
                baseRadius *= Flags.Instance.HydrogenBallScale;

            Matrix atomMatrix = Matrix.CreateScale(baseRadius, baseRadius, baseRadius)
 * Matrix.CreateTranslation(pos)
 * scaleRotate;

            DrawModelWithEffect(_bigBallMeshes, _bigBallTransforms, atomMatrix);
        }

        private void DrawAtomEllipsoid(Molecule.Atom atom, Vector3 pos, Matrix scaleRotate)
        {
            float probability = Flags.Instance.ATP_Probability;
            Matrix ellipsoidMatrix;

            if (atom.isAnisotropic && atom.Un != Matrix.Identity)
            {
                Matrix scaleMatrix = Matrix.CreateScale(probability, probability, probability);
                Matrix scaledUn = atom.Un * scaleMatrix;

                ellipsoidMatrix = atom.Transformation
 * scaledUn
 * Matrix.CreateTranslation(pos)
 * scaleRotate;
            }
            else
            {
                float r = IsMetal(atom.AtomicNumber)
                    ? Flags.Instance.BallRadiusMetal
                    : Flags.Instance.BallRadiusNonMetal;

                if (atom.AtomicNumber == 1)
                    r *= Flags.Instance.HydrogenBallScale;

                ellipsoidMatrix = Matrix.CreateScale(r, r, r)
 * Matrix.CreateTranslation(pos)
 * scaleRotate;
            }

            DrawModelWithEffect(_bigBallMeshes, _bigBallTransforms, ellipsoidMatrix);
        }

        private void DrawAtomSticks(int atomIdx, Matrix scaleRotate)
        {
            var atom = _molecule.atom[atomIdx];
            int bondCount = atom.NrBondedAtoms;
            if (bondCount <= 0) return;

            float stickRadius = Flags.Instance.StickRadius;
            float stickFactor = Flags.Instance.StickRadiusFactor;

            for (int j = 0; j < bondCount; j++)
            {
                float distance = atom.Distance[j];
                if (distance <= 0) continue;

                float angleZ = atom.AngleZ[j];
                float angleXY = atom.AngleXY[j];

                Matrix bondMatrix = Matrix.CreateScale(
                                        stickFactor * distance,
                                        stickRadius,
                                        stickRadius)
 * Matrix.CreateRotationY(angleZ)
 * Matrix.CreateRotationZ(angleXY);

                Matrix atomMatrix = bondMatrix
 * Matrix.CreateTranslation(atom.Coordinate)
 * scaleRotate;

                DrawModelWithEffect(_smallRodMeshes, _smallRodTransforms, atomMatrix);
            }
        }

        private void DrawModelWithEffect(ModelMesh[] meshes, Matrix[] transforms, Matrix atomMatrix)
        {
            if (meshes == null || transforms == null) return;

            foreach (var mesh in meshes)
            {
                Matrix world = transforms[mesh.ParentBone.Index] * atomMatrix;

                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = world;
                    effect.View = _atomEffect.View;
                    effect.Projection = _atomEffect.Projection;
                    effect.LightingEnabled = _atomEffect.LightingEnabled;
                    effect.PreferPerPixelLighting = _atomEffect.PreferPerPixelLighting;
                    effect.VertexColorEnabled = false;
                    effect.TextureEnabled = false;

                    effect.AmbientLightColor = _atomEffect.AmbientLightColor;
                    effect.DiffuseColor = _atomEffect.DiffuseColor;
                    effect.SpecularColor = _atomEffect.SpecularColor;
                    effect.SpecularPower = _atomEffect.SpecularPower;
                    effect.Alpha = _atomEffect.Alpha;

                    effect.DirectionalLight0.Enabled = _atomEffect.DirectionalLight0.Enabled;
                    effect.DirectionalLight0.Direction = _atomEffect.DirectionalLight0.Direction;
                    effect.DirectionalLight0.DiffuseColor = _atomEffect.DirectionalLight0.DiffuseColor;
                    effect.DirectionalLight0.SpecularColor = _atomEffect.DirectionalLight0.SpecularColor;

                    effect.DirectionalLight1.Enabled = _atomEffect.DirectionalLight1.Enabled;
                    effect.DirectionalLight1.Direction = _atomEffect.DirectionalLight1.Direction;
                    effect.DirectionalLight1.DiffuseColor = _atomEffect.DirectionalLight1.DiffuseColor;
                    effect.DirectionalLight1.SpecularColor = _atomEffect.DirectionalLight1.SpecularColor;
                }

                mesh.Draw();
            }
        }

        private Vector3 GetAtomColorVec(Molecule.Atom atom)
        {
            int mode = Flags.Instance.ColorMode;

            if (mode == 4)
            {
                if (atom.CustomColor.X >= 0f && atom.CustomColor.Y >= 0f && atom.CustomColor.Z >= 0f)
                    return atom.CustomColor;
            }

            int z = atom.AtomicNumber;
            if (z > 0 && z < VdwParameters.color.Length)
                return VdwParameters.color[z];

            return new Vector3(0.6f, 0.6f, 0.6f);
        }

        private static bool IsMetal(int atomicNumber)
        {
            return atomicNumber >= 3 && atomicNumber <= 4 ||
                   atomicNumber >= 11 && atomicNumber <= 12 ||
                   atomicNumber >= 19 && atomicNumber <= 31 ||
                   atomicNumber >= 37 && atomicNumber <= 50 ||
                   atomicNumber >= 55 && atomicNumber <= 83 ||
                   atomicNumber >= 87 && atomicNumber <= 112;
        }

        #endregion

        #region Vertex buffers — axes, cell

        private void RebuildAxesBuffer()
        {
            if (_device == null || _device.IsDisposed) return;

            float L = 20f;

            var verts = new VertexPositionColor[]
            {
                new VertexPositionColor(new Vector3(0, 0, 0), new Color(255, 0, 0)),
                new VertexPositionColor(new Vector3(L, 0, 0), new Color(255, 0, 0)),

                new VertexPositionColor(new Vector3(0, 0, 0), new Color(0, 255, 0)),
                new VertexPositionColor(new Vector3(0, L, 0), new Color(0, 255, 0)),

                new VertexPositionColor(new Vector3(0, 0, 0), new Color(0, 128, 255)),
                new VertexPositionColor(new Vector3(0, 0, L), new Color(0, 128, 255)),
            };

            _axesVB?.Dispose();
            _axesVB = new VertexBuffer(_device, VertexPositionColor.VertexDeclaration,
                verts.Length, BufferUsage.WriteOnly);
            _axesVB.SetData(verts);
            _axesVertCount = verts.Length;
        }

        private void RebuildCellBuffer()
        {
            if (_device == null || _device.IsDisposed || _molecule == null)
            {
                _cellVB?.Dispose();
                _cellVB = null;
                _cellVertCount = 0;
                return;
            }

            Matrix fracToCart = Matrix.Transpose(_molecule.FractionaltoCarttesianMatrix(0, 0, 0));

            float s = _sceneScale * _zoomFactor;
            Vector3 shift = _molecule.MoleculeShift;

            Vector3[] corners = new Vector3[8];
            for (int ix = 0; ix < 2; ix++)
                for (int iy = 0; iy < 2; iy++)
                    for (int iz = 0; iz < 2; iz++)
                    {
                        int idx = (ix << 2) | (iy << 1) | iz;
                        Vector3 frac = new Vector3(ix, iy, iz);
                        Vector3 cart = Vector3.Transform(frac, fracToCart);
                        cart -= shift;
                        corners[idx] = cart * s;
                    }

            var edges = new List<(int, int)>();
            for (int a = 0; a < 8; a++)
            {
                for (int bit = 0; bit < 3; bit++)
                {
                    int b = a | (1 << bit);
                    if (b != a && b < 8)
                    {
                        int lo = Math.Min(a, b);
                        int hi = Math.Max(a, b);
                        if (!edges.Contains((lo, hi)))
                            edges.Add((lo, hi));
                    }
                }
            }

            var lineVerts = new List<VertexPositionColor>(edges.Count * 2);
            Color cellColor = new Color((byte)255, (byte)200, (byte)80, (byte)220);

            foreach (var (c1, c2) in edges)
            {
                lineVerts.Add(new VertexPositionColor(corners[c1], cellColor));
                lineVerts.Add(new VertexPositionColor(corners[c2], cellColor));
            }

            _cellVB?.Dispose();
            _cellVB = new VertexBuffer(_device, VertexPositionColor.VertexDeclaration,
                lineVerts.Count, BufferUsage.WriteOnly);
            _cellVB.SetData(lineVerts.ToArray());
            _cellVertCount = lineVerts.Count;
        }

        #endregion

        #region Rendering

        private void RenderFrame()
        {
            if (_disposed || !_deviceReady || _device == null || _device.IsDisposed) return;
            if (_panel == null || !_panel.IsHandleCreated || !_panel.Visible) return;
            if (_isGenerating) return;
            try
            {
                if (_panel.Width > 0 && _panel.Height > 0)
                {
                    var pp = _device.PresentationParameters;
                    if (pp.BackBufferWidth != _panel.Width || pp.BackBufferHeight != _panel.Height)
                    {
                        int newW = Math.Max(_panel.Width, 1);
                        int newH = Math.Max(_panel.Height, 1);

                        if (Math.Abs(pp.BackBufferWidth - newW) > 4 ||
                            Math.Abs(pp.BackBufferHeight - newH) > 4)
                        {
                            pp.BackBufferWidth = newW;
                            pp.BackBufferHeight = newH;

                            try { _device.Reset(pp); }
                            catch { return; }

                            if (_camera != null)
                            {
                                _camera.GraphicsDevice = _device;
                                _camera.MarkChanged();
                                _camera.ApplyEffects();
                            }

                            _sortDirty = true;
                            _indicesSorted = false;
                        }
                    }
                }

                Vector3 currentShift = _molecule?.MoleculeShift ?? Vector3.Zero;
                if (currentShift != _lastMoleculeShift)
                {
                    LogInfo($"[Hirshfeld] MoleculeShift changed: " +
                            $"({_lastMoleculeShift.X:F3},{_lastMoleculeShift.Y:F3},{_lastMoleculeShift.Z:F3}) → " +
                            $"({currentShift.X:F3},{currentShift.Y:F3},{currentShift.Z:F3})");

                    _lastMoleculeShift = currentShift;

                    if (_surface != null && _surface.IsValid)
                    {
                        RebuildSurfaceBuffer();
                        RebuildWireframeBuffer();
                        _indicesSorted = false;
                        _sortDirty = true;
                    }

                    RebuildCellBuffer();
                }

                if (_dirtyVertexColors && _surface != null)
                {
                    RebuildSurfaceBuffer();
                    _dirtyVertexColors = false;
                }
                _camera.MarkChanged();
                _camera?.ApplyEffects();

                _device.Clear(Flags.Instance.color1);

                float aspect = _device.Viewport.AspectRatio;
                if (aspect < 0.001f) aspect = 1f;

                Matrix world = _camera.Rotate * _camera.Translate;
                Matrix view = Matrix.CreateLookAt(
                    new Vector3(0, 0, 100), Vector3.Zero, Vector3.Up);

                Matrix proj = Flags.Instance.PerspectiveMode
                    ? Matrix.CreatePerspectiveFieldOfView(
                        MathHelper.PiOver4, aspect, 0.1f, 10000f)
                    : Matrix.CreateOrthographic(80 * aspect, 80, 0.1f, 10000f);

                if (_surfaceEffect != null)
                {
                    _surfaceEffect.World = world;
                    _surfaceEffect.View = view;
                    _surfaceEffect.Projection = proj;
                }

                if (_showMolecule)
                {
                    DrawMoleculeTALStyle(world, view, proj);
                }

                if (_showMesh
                    && _surfaceVB != null && _surfaceVertCount > 0
                    && _surfaceIndexCount >= 3
                    && _surfaceIBOpaque != null
                    && _surfaceIBTransparent != null)
                {
                    {
                        bool cameraStopped = _camera != null && !_camera.IsActivelyMoving;
                        bool cameraRotatedEnough = HasCameraRotatedEnough();
                        bool activelyMoving = _camera != null && _camera.IsActivelyMoving;

                        if (!_indicesSorted
                            || (cameraStopped && cameraRotatedEnough)
                            || activelyMoving)
                        {
                            SortSurfaceTrianglesBackToFront(world, view);
                            SaveSortAngles();
                        }

                        _device.Indices = _surfaceIBTransparent;
                    }

                    _device.SetVertexBuffer(_surfaceVB);

                    if (_hirshfeldSurfaceEffect != null)
                    {
                        DrawSurfaceWithShader(world, view, proj);
                    }
                    else if (_surfaceEffect != null)
                    {
                        bool opaque = _alphaValue >= 240;

                        _device.BlendState = opaque
                            ? BlendState.Opaque
                            : BlendState.AlphaBlend;
                        _device.DepthStencilState = opaque
                            ? _depthDefaultWrite
                            : _depthReadNoWrite;
                        _device.RasterizerState = RasterizerState.CullNone;

                        foreach (var pass in _surfaceEffect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            _device.DrawIndexedPrimitives(
                                PrimitiveType.TriangleList,
                                0, 0,
                                _surfaceVertCount,
                                0,
                                _surfaceIndexCount / 3);
                        }
                    }

                    _device.Indices = null;
                }

                if (_showWireframe && _wireframeVB != null && _wireframeVertCount > 0)
                {
                    _device.DepthStencilState = DepthStencilState.DepthRead;
                    _device.BlendState = BlendState.AlphaBlend;
                    _device.RasterizerState = RasterizerState.CullNone;

                    _effect.World = world;
                    _effect.View = view;
                    _effect.Projection = proj;

                    _device.SetVertexBuffer(_wireframeVB);
                    foreach (var pass in _effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _device.DrawPrimitives(PrimitiveType.LineList, 0,
                            _wireframeVertCount / 2);
                    }
                    _device.DepthStencilState = _depthDefaultWrite;
                }

                if (_showAxes && _axesVB != null && _axesVertCount > 0)
                {
                    _device.DepthStencilState = DepthStencilState.None;
                    _device.BlendState = BlendState.Opaque;
                    _device.RasterizerState = RasterizerState.CullNone;

                    _effect.World = world;
                    _effect.View = view;
                    _effect.Projection = proj;

                    _device.SetVertexBuffer(_axesVB);
                    foreach (var pass in _effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _device.DrawPrimitives(PrimitiveType.LineList, 0, _axesVertCount / 2);
                    }
                    _device.DepthStencilState = _depthDefaultWrite;
                }

                if (_showCell && _cellVB != null && _cellVertCount > 0)
                {
                    _device.DepthStencilState = DepthStencilState.None;
                    _device.BlendState = BlendState.Opaque;
                    _device.RasterizerState = RasterizerState.CullNone;

                    _effect.World = world;
                    _effect.View = view;
                    _effect.Projection = proj;

                    _device.SetVertexBuffer(_cellVB);
                    foreach (var pass in _effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _device.DrawPrimitives(PrimitiveType.LineList, 0, _cellVertCount / 2);
                    }
                    _device.DepthStencilState = _depthDefaultWrite;
                }

                _device.Present();
            }
            catch (Exception ex)
            {
                LogError($"[Hirshfeld] Render error: {ex.Message}");
            }
        }

        private void DrawSurfaceWithShader(Matrix world, Matrix view, Matrix proj)
        {
            if (_hirshfeldSurfaceEffect == null) return;

            var fx = _hirshfeldSurfaceEffect;

            fx.Parameters["World"]?.SetValue(world);
            fx.Parameters["View"]?.SetValue(view);
            fx.Parameters["Projection"]?.SetValue(proj);
            fx.Parameters["CameraPosition"]?.SetValue(_camera.g_cameraPos);

            fx.Parameters["Light1Enabled"]?.SetValue(Flags.Instance.Enabled ? 1f : 0f);
            fx.Parameters["LightDirection"]?.SetValue(
                Vector3.Normalize(Flags.Instance.LightDirection));
            fx.Parameters["LightDiffuseColor"]?.SetValue(
                Vector3.One * Flags.Instance.DiffuseIntensity);
            fx.Parameters["LightSpecularColor"]?.SetValue(
                Flags.Instance.SpecularColor);

            fx.Parameters["Light2Enabled"]?.SetValue(Flags.Instance.Light2Enabled ? 1f : 0f);
            fx.Parameters["Light2Direction"]?.SetValue(
                Vector3.Normalize(Flags.Instance.Light2Direction));
            fx.Parameters["Light2DiffuseColor"]?.SetValue(
                Flags.Instance.Light2DiffuseColor);
            fx.Parameters["Light2SpecularColor"]?.SetValue(
                Flags.Instance.Light2SpecularColor);

            fx.Parameters["AmbientColor"]?.SetValue(
                Vector3.One * Flags.Instance.AmbientIntensity);
            fx.Parameters["SpecularColor"]?.SetValue(
                Flags.Instance.SpecularColor * 0.5f);
            fx.Parameters["SpecularPower"]?.SetValue(
                Flags.Instance.SpecularPower);

            float alphaNormalized = _alphaValue / 255f;
            fx.Parameters["Alpha"]?.SetValue(alphaNormalized);

            float fresnelStrength = Math.Clamp(1.0f - alphaNormalized, 0f, 1f) * 0.85f;
            fx.Parameters["FrontFadeStrength"]?.SetValue(fresnelStrength);

            bool opaque = _alphaValue >= 240;

            _device.DepthStencilState = opaque
                ? _depthDefaultWrite
                : _depthReadNoWrite;

            _device.BlendState = opaque
                ? BlendState.Opaque
                : BlendState.AlphaBlend;

            _device.RasterizerState = RasterizerState.CullNone;

            foreach (var pass in fx.CurrentTechnique.Passes)
            {
                pass.Apply();
                _device.DrawIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    0, 0,
                    _surfaceVertCount,
                    0,
                    _surfaceIndexCount / 3);
            }
        }

        #endregion

        #region Mouse

        private void Panel_MouseDown(object sender, MouseEventArgs e)
        {
            _camera?.MouseDown(e);
            _panel.Focus();
        }

        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_camera != null && e.Button == MouseButtons.Left)
            {
                _camera.MouseMove(e, _panel);
                _sortDirty = true;
            }
        }

        private void Panel_MouseUp(object sender, MouseEventArgs e)
        {
            _camera?.MouseUp(e);
            _sortDirty = true;
        }

        private void Panel_MouseWheel(object sender, MouseEventArgs e)
        {
            float delta = e.Delta / 120f;

            if (delta > 0) _zoomFactor *= 1.05f;
            else _zoomFactor *= 0.95f;

            _zoomFactor = Math.Clamp(_zoomFactor, 0.2f, 8.0f);

            if (_lblZoomInfo != null)
                _lblZoomInfo.Text = $"Zoom: {_zoomFactor:F2}× (mouse wheel)";

            RebuildSurfaceBuffer();
            RebuildWireframeBuffer();
            RebuildCellBuffer();

            _sortDirty = true;
        }

        #endregion

        #region Reset View

        private void ResetView()
        {
            if (_camera == null) return;

            _camera.Rotate = Matrix.Identity;
            _camera.g_system_translate = Vector3.Zero;
            _camera.g_cameraPos = new Vector3(0, 0, 100);
            _camera.g_targetPos = Vector3.Zero;
            _camera.zoom = 0.5f;
            _camera.OneOverZoom = 2.0f;
            _camera.ApplyEffects();

            _zoomFactor = 1.0f;

            if (_lblZoomInfo != null)
                _lblZoomInfo.Text = "Zoom: 3.00× (mouse wheel)";

            RebuildSurfaceBuffer();
            RebuildWireframeBuffer();
            RebuildCellBuffer();

            _sortDirty = true;
        }

        #endregion

        #region Cleanup

        private void CleanupAll()
        {
            _disposed = true;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;

            if (_surface != null)
                HirshfeldSmoothing.ClearCache(_surface);

            try { _surfaceVB?.Dispose(); } catch { }
            _surfaceVB = null;

            try { _surfaceIBOpaque?.Dispose(); } catch { }
            _surfaceIBOpaque = null;

            try { _surfaceIBTransparent?.Dispose(); } catch { }
            _surfaceIBTransparent = null;

            try { _wireframeVB?.Dispose(); } catch { }
            _wireframeVB = null;

            try { _axesVB?.Dispose(); } catch { }
            _axesVB = null;

            try { _cellVB?.Dispose(); } catch { }
            _cellVB = null;

            try { _spriteBatch?.Dispose(); } catch { }
            _spriteBatch = null;

            try { _pixelTexture?.Dispose(); } catch { }
            _pixelTexture = null;

            try { _content?.Dispose(); } catch { }
            _content = null;

            _spriteFont = null;

            try { _effect?.Dispose(); } catch { }
            _effect = null;

            try { _atomEffect?.Dispose(); } catch { }
            _atomEffect = null;

            try { _surfaceEffect?.Dispose(); } catch { }
            _surfaceEffect = null;

            try { _hirshfeldSurfaceEffect?.Dispose(); } catch { }
            _hirshfeldSurfaceEffect = null;

            try { _depthReadNoWrite?.Dispose(); } catch { }
            _depthReadNoWrite = null;

            try { _depthDefaultWrite?.Dispose(); } catch { }
            _depthDefaultWrite = null;

            _sortedIndices = null;

            try
            {
                if (_fingerprintWindow != null && !_fingerprintWindow.IsDisposed)
                    _fingerprintWindow.Close();
            }
            catch { }
            _fingerprintWindow = null;

            _smallBall = null;
            _bigBall = null;
            _smallRod = null;

            _smallBallTransforms = null;
            _bigBallTransforms = null;
            _smallRodTransforms = null;

            _smallBallMeshes = null;
            _bigBallMeshes = null;
            _smallRodMeshes = null;

            _zDepthCache = null;

            _lastDiagnostics = null;

            if (_device != null && !_device.IsDisposed)
            {
                try { _device.Dispose(); } catch { }
                _device = null;
            }

            _deviceReady = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timer?.Stop();
            base.OnFormClosing(e);
        }

        #endregion
    }
}