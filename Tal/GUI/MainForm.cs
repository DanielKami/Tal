// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm core — fields, constructor, form events, file open, active document/molecule accessors, view helpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.Hirshfeld;
using Tal.DrawMethods;
using Tal.Forms;
using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal
{
    /// <summary>
    /// Main application window for the TAL crystallographic application.
    /// Provides a docking interface with 3D visualization, structure management,
    /// and various analysis tools.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Singleton

        public static MainForm Instance { get; private set; }

        #endregion

        #region Private Fields — Core

        private Measure _measure;
        private DockPanel _dockPanel;
        private Panel _dockContainer;
        private MenuStrip _menuStrip;
        private ToolStripMenuItem _miShowHydrogens;
        private ToolStripMenuItem _miShowUnitCell;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabelLeft;
        private ToolStripStatusLabel _statusLabelCenter;
        private ToolStripStatusLabel _statusLabelRight;
        private ToolStripProgressBar _statusProgressBar;
        private int _windowCounter = 1;
        private StructureListWindow _structureListWindow;
        private DockContent _leftPanel;
        private DockContent _rightPanel;
        private DockContent _bottomPanel;

        #endregion

        #region Private Fields — Singleton Windows

        // Display / rendering
        private DisplayOptionsWindow _displayOptionsWindow;
        private AtomColorWindow _atomColorWindow;
        private AtomDisplayWindow _atomDisplayWindow;
        private UnitCellDisplayWindow _unitCellDisplayWindow;
        private LabelsWindow _labelsWindow;
        private BondDisplayWindow _bondDisplayWindow;
        private LightingWindow _lightingWindow;
        private BackgroundColorWindow _backgroundColorWindow;
        private DepthCueWindow _depthCueWindow;
        private SceneQualityWindow _sceneQualityWindow;
        private AtomPropertiesWindow _atomPropertiesWindow;

        // Structure / analysis
        private CavitySettingsWindow _cavitySettingsWindow;
        private PackingWindow _packingWindow;
        private ShortContactsWindow _shortContactsWindow;
        private HBondSettingsWindow _hBondSettingsWindow;
        private SymmetryReplicationWindow _symmetryReplicationWindow;
        private AutoRotateWindow _autoRotateWindow;
        private MoleculeOverlayWindow _moleculeOverlayWindow;
        private CrystalCalculationsWindow _crystalCalculationsWindow;
        private HydrogenNormalizationWindow _hydrogenNormalizationWindow;
        private MorphologyWindow _morphologyWindow;
        private StructureInfoWindow _structureInfoWindow;
        SymmetryElementsWindow _symmetryElementsWindow;

        // Hirshfeld
        private HirshfeldFingerprintWindow _hirshfeldFingerprintWindow;
        private HirshfeldDocument _hirshfeldDocument;

        // Debug
        private DebugWindow _debugWindow;

        #endregion

        #region Private Fields — Toolbars

        private ToolStrip _styleStrip;
        private ToolStripComboBox _styleComboBox;
        private ToolStrip _viewStrip;
        private ToolStrip _pickingStrip;
        private ToolStripComboBox _pickingModeComboBox;
        private ToolStripComboBox _showLabelsComboBox;

        #endregion

        #region Private Fields — View Buttons

        private ToolStripButton _btnViewA;
        private ToolStripButton _btnViewB;
        private ToolStripButton _btnViewC;
        private ToolStripButton _btnViewAStar;
        private ToolStripButton _btnViewBStar;
        private ToolStripButton _btnViewCStar;
        private ToolStripButton _btnViewXPlus;
        private ToolStripButton _btnViewXMinus;
        private ToolStripButton _btnViewYPlus;
        private ToolStripButton _btnViewYMinus;
        private ToolStripButton _btnViewZPlus;
        private ToolStripButton _btnViewZMinus;
        private ToolStripButton _btnRotateLeft90;
        private ToolStripButton _btnRotateRight90;
        private ToolStripButton _btnRotateUp90;
        private ToolStripButton _btnRotateDown90;
        private ToolStripButton _btnMoveLeft;
        private ToolStripButton _btnMoveRight;
        private ToolStripButton _btnMoveUp;
        private ToolStripButton _btnMoveDown;
        private ToolStripButton _btnZoomIn;
        private ToolStripButton _btnZoomOut;
        private ToolStripButton _btnPerspective;
        private ToolStripButton _btnAnimate;

        #endregion

        #region Private Fields — Picking Buttons

        private ToolStripButton _btnCleanMeasurements;
        private ToolStripButton _btnPickAtom;
        private ToolStripButton _btnLasso;
        private ToolStripButton _btnSelectAll;
        private ToolStripButton _btnSelectAllExceptHydrogens;
        private ToolStripButton _btnClearSelection;
        private ToolStripButton _btnRevealSymmetry;
        private ToolStripButton _btnCavity;
        private ToolStripButton _btnCavityReport;
        private ToolStripButton _btnResetMeasurements;

        #endregion

        #region Private Fields — Colors

        private System.Drawing.Color _menuBackColor = System.Drawing.Color.FromArgb(240, 240, 242);
        private System.Drawing.Color _menuForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
        private System.Drawing.Color _toolStripBackColor = System.Drawing.Color.FromArgb(248, 248, 250);
        private System.Drawing.Color _toolStripForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
        private System.Drawing.Color _comboBoxBackColor = System.Drawing.Color.White;
        private System.Drawing.Color _comboBoxForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
        private System.Drawing.Color _dockContainerBackColor = System.Drawing.Color.FromArgb(238, 238, 242);

        #endregion


        #region Private Fields — Constants

        private const float TRANSLATION_STEP = 0.02f;
        private const float ROTATION_90_STEP = MathHelperTal.PiOver2;

        #endregion

        #region Private Fields — State

        private bool _isLassoActive = false;
        private bool _isPickActive = false;
        private bool _isMeasurementMode = false;

        // Label dragging
        private bool _isLabelMoveMode = false;
        private bool _isSelectMoleculeMode = false;
        private int _selectedLabelIndex = -1;
        private System.Drawing.Point _labelDragStart;
        private (float offsetX, float offsetY) _labelDragStartOffset;
        private bool _isDraggingLabel = false;
        private bool _isDraggingAxisLabel = false;
        private int _selectedAxisLabelIndex = -1; // 0=a, 1=b, 2=c
        private bool _isLabelsVisible = false;

        // Measurement label dragging
        private int _selectedMeasurementIndex = -1;
        private bool _isDraggingMeasurementLabel = false;
        private float _labelDragStartOffsetX = 0f;
        private float _labelDragStartOffsetY = 0f;

        private bool _isClosing = false;

        // ⭐ List of files to open after startup (from Main arguments)
        private List<string> _pendingStartupFiles;

        #endregion

        #region Private Fields — Logging

        /// <summary>
        /// Timer that drains the SafeDebugLogger queue once per second.
        /// Runs on the UI thread (System.Windows.Forms.Timer), so
        /// Drain() can safely call DebugWindow.Log without Invoke.
        /// </summary>
        private System.Windows.Forms.Timer _logDrainTimer;

        #endregion

        #region Public Properties

        public bool IsLabelMoveModeActive => _isLabelMoveMode;
        public bool IsPickActive => _isPickActive;
        public bool IsSelectMoleculeMode => _isSelectMoleculeMode;
        public bool IsLassoActive => _isLassoActive;
        public bool IsDraggingLabel => _isDraggingLabel;
        public bool IsDraggingAxisLabel => _isDraggingAxisLabel;
        public bool IsDraggingMeasurementLabel => _isDraggingMeasurementLabel;
        public bool IsMeasurementModeActive => GetPickingMode() >= 6 && GetPickingMode() <= 8;

        #endregion

        #region Events

        /// <summary>
        /// Event raised when the molecule of the active document changes
        /// (new structure loaded, structure transformed, molecule reloaded).
        ///
        /// Used by tool windows (e.g. HydrogenNormalizationWindow)
        /// to auto-refresh their content.
        /// </summary>
        public event Action<Molecule, MonoGameDocument> ActiveMoleculeChanged;

        /// <summary>
        /// Raise the ActiveMoleculeChanged event.
        /// Call after every molecule load/rebuild.
        /// </summary>
        public void RaiseActiveMoleculeChanged(Molecule molecule, MonoGameDocument doc)
        {
            try
            {
                ActiveMoleculeChanged?.Invoke(molecule, doc);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MainForm] ActiveMoleculeChanged handler error: {ex.Message}");
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor — used by the designer and normal start (no files).
        /// </summary>
        public MainForm() : this(null)
        {
        }

        /// <summary>
        /// Constructor with a list of files to open after startup (from Main args / double-click / IPC).
        /// </summary>
        public MainForm(List<string> startupFiles)
        {
            Instance = this;
            var flags = Flags.Instance;

            _pendingStartupFiles = startupFiles;

            InitializeComponent();
            this.IsMdiContainer = true;
            this.Name = "MainForm";

            InitializeStyleStrip();
            InitializeViewStrip();
            InitializePickingStrip();
            InitializeMenu();
            UpdateRecentMenu();
            InitializeStatusBar();
            InitializeDockContainer();
            InitializeDocking();
            InitializeFileDrop();

            // ⭐ NEW: Timer to drain SafeDebugLogger once per second
            InitializeLogDrainTimer();

            this.Load += MainForm_Load;
            this.Shown += MainForm_Shown;
            this.Resize += MainForm_Resize;
            this.FormClosing += MainForm_FormClosing;
            this.AutoScaleMode = AutoScaleMode.None;
        }

        #endregion

        #region Form Events

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadDockLayout();

            this.BeginInvoke(new Action(() =>
            {
                CreateSampleWindows();
                SetStatus("Ready", "Structures: 0", "✅");
            }));
        }

        /// <summary>
        /// ⭐ Opens pliki przekazane w argumentach Main — dopiero when window is already visible
        /// i GameWindow has zainicjalizowany GraphicsDevice.
        /// </summary>
        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (_pendingStartupFiles == null || _pendingStartupFiles.Count == 0)
                return;

            var files = _pendingStartupFiles;
            _pendingStartupFiles = null;   // only once

            // Deferred call — DockPanel + GameWindow must be ready
            this.BeginInvoke(new Action(() =>
            {
                foreach (var path in files)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[MainForm.Shown] Opening startup file: {path}");

                        OpenFileInternal(path);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[MainForm.Shown] Error opening '{path}': {ex.Message}");

                        MessageBox.Show(
                            $"Failed to open file:\n{path}\n\n{ex.Message}",
                            "TAL",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }));
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (_dockPanel != null && _structureListWindow != null && !_structureListWindow.IsHidden)
            {
                _dockPanel.DockRightPortion = 0.35;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isClosing = true;

            // ⭐ Stop SafeDebugLogger drain timer and flush remaining logs
            StopLogDrainTimer();

            SaveDockLayout();

            try
            {
                var documents = _dockPanel.Documents.ToList();

                for (int i = documents.Count - 1; i >= 0; i--)
                {
                    if (documents[i] is MonoGameDocument monoDoc)
                    {
                        try
                        {
                            var gameWindow = monoDoc.GetType().GetField("_gameWindow",
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (gameWindow != null)
                            {
                                var gw = gameWindow.GetValue(monoDoc) as GameWindow;
                                gw?.CleanupBeforeClose();
                                gw?.Dispose();
                            }
                            monoDoc.Close();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainForm] Error closing document: {ex.Message}");
                        }
                    }
                }

                StructureStorage.Instance.Clear();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                System.Diagnostics.Debug.WriteLine("[MainForm] Cleanup completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainForm] Error during cleanup: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MainForm] Stack: {ex.StackTrace}");
            }

            try
            {
                Flags.Instance.SaveForce();
                System.Diagnostics.Debug.WriteLine("[MainForm] Settings saved on exit");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainForm] Error saving settings: {ex.Message}");
            }
        }

        #endregion

        #region Log Drain Timer (SafeDebugLogger)

 

 

        #endregion

        #region Public Methods — File Open (unified)

        /// <summary>
        /// ⭐ Shared file-open logic — used via:
        ///   - OnOpen (menu File → Open)
        ///   - OpenRecentFile (menu Open Recent)
        ///   - MainForm_Shown (argumenty Main / dwuklik)
        ///   - Pipe server (drugi proces z plikami)
        ///   - Drag & drop
        ///
        /// Returns true when the file was loaded (regardless of how many structures).
        /// </summary>
        public bool OpenFileInternal(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MainForm] OpenFileInternal: file does not exist: {filePath}");
                return false;
            }

            SetStatus("Loading...", $"Structures: {StructureStorage.Instance.StructureCount}", "⏳");
            SetProgressIndeterminate(true);
            Application.DoEvents();

            try
            {
                var storage = StructureStorage.Instance;

                if (!storage.LoadFile(filePath))
                {
                    SetStatus("Load error", $"Structures: {storage.StructureCount}", "❌");
                    MessageBox.Show(
                        $"Failed to load file:\n{filePath}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }

                _structureListWindow?.UpdateList();
                AddToRecentFiles(filePath);

                // 1) Structures by full path
                var structures = storage.GetStructuresFromFile(filePath);

                // 2) Fallback after nazwie file
                if (structures == null || structures.Count == 0)
                {
                    string fileName = Path.GetFileName(filePath);
                    structures = storage.GetStructuresBySourceFileName(fileName);
                }

                if (structures == null || structures.Count == 0)
                {
                    SetStatus($"Loaded: {Path.GetFileName(filePath)} (0 struktur)",
                              $"Structures: {storage.StructureCount}", "⚠️");
                    return false;
                }

                // ⭐ Single structure — open without asking
                if (structures.Count == 1)
                {
                    OpenStructureInNewWindow(structures[0]);
                    SetStatus($"Opened: {structures[0].Name}",
                              $"Structures: {storage.StructureCount}", "🔬");
                    return true;
                }

                // ⭐ Multiple structures — ask the user
                var choice = MessageBox.Show(
                    $"Znaleziono {structures.Count} struktur w pliku:\n{Path.GetFileName(filePath)}\n\n" +
                    $"YES     — open ALL\n" +
                    $"NO      — open only the first\n" +
                    $"CANCEL — open none",
                    "Wiele struktur w pliku",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (choice == DialogResult.Yes)
                {
                    foreach (var s in structures)
                        OpenStructureInNewWindow(s);

                    SetStatus($"Opened all {structures.Count} structures from {Path.GetFileName(filePath)}",
                              $"Structures: {storage.StructureCount}", "🔬");
                    return true;
                }
                else if (choice == DialogResult.No)
                {
                    OpenStructureInNewWindow(structures[0]);
                    SetStatus($"Opened: {structures[0].Name}",
                              $"Structures: {storage.StructureCount}", "🔬");
                    return true;
                }
                else
                {
                    SetStatus($"Loaded: {Path.GetFileName(filePath)} ({structures.Count} struktur w bazie)",
                              $"Structures: {storage.StructureCount}", "ℹ️");
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Load error", $"Structures: {StructureStorage.Instance.StructureCount}", "❌");
                return false;
            }
            finally
            {
                SetProgressIndeterminate(false);
            }
        }

        #endregion

        #region Public Methods — View / Status

        public void RefreshActiveView()
        {
            RefreshActiveDocument();
        }

        public void SetStatus(string left, string center, string right = "✅")
        {
            if (_statusLabelLeft != null) _statusLabelLeft.Text = left;
            if (_statusLabelCenter != null) _statusLabelCenter.Text = center;
            if (_statusLabelRight != null) _statusLabelRight.Text = right;
        }

        public void SetProgress(int value)
        {
            if (_statusProgressBar != null)
                _statusProgressBar.Value = Math.Clamp(value, 0, 100);
        }

        public void SetProgressIndeterminate(bool active)
        {
            if (_statusProgressBar != null)
                _statusProgressBar.Style = active ? ProgressBarStyle.Marquee : ProgressBarStyle.Continuous;
        }

        public Molecule GetActiveMolecule()
        {
            var active = _dockPanel?.ActiveDocument as MonoGameDocument;
            if (active == null) return null;
            return active.GetMolecule();
        }

        // ⭐ Cache — SpriteFont pobierany raz, potem reuse
        private Microsoft.Xna.Framework.Graphics.SpriteFont _cachedSpriteFont;

        public Microsoft.Xna.Framework.Graphics.SpriteFont GetSpriteFont()
        {
            if (_cachedSpriteFont != null) return _cachedSpriteFont;

            var active = _dockPanel?.ActiveDocument as MonoGameDocument;
            if (active != null)
            {
                _cachedSpriteFont = active.GetSpriteFont();
                if (_cachedSpriteFont != null) return _cachedSpriteFont;
            }

            if (_dockPanel != null)
            {
                foreach (var doc in _dockPanel.Documents)
                {
                    if (doc is MonoGameDocument monoDoc)
                    {
                        _cachedSpriteFont = monoDoc.GetSpriteFont();
                        if (_cachedSpriteFont != null) return _cachedSpriteFont;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine(
                "[MainForm] ⚠️ GetSpriteFont: no SpriteFont available");
            return null;
        }

        public Camera GetActiveCamera()
        {
            var active = _dockPanel?.ActiveDocument as MonoGameDocument;
            if (active == null) return null;
            return active.GetCamera();
        }

        public Measure GetActiveMeasure()
        {
            var active = _dockPanel?.ActiveDocument as MonoGameDocument;
            if (active == null) return null;
            return active.GetMeasure();
        }

        public MonoGameDocument GetActiveDocument()
        {
            return _dockPanel.ActiveDocument as MonoGameDocument;
        }

        public DockPanel GetDockPanel() => _dockPanel;

        public int GetPickingMode()
        {
            return _pickingModeComboBox?.SelectedIndex ?? -1;
        }

        public void UpdateStyleForActiveDocument()
        {
            var active = GetActiveDocument();
            if (active != null)
            {
                int style = _styleComboBox.SelectedIndex;
                active.SetStyle(style);
            }
        }

        public void RefreshActiveDocument()
        {
            var doc = GetActiveDocument();
            doc?.RefreshView();
        }

        public void RefreshAllDocuments()
        {
            var dockPanel = GetDockPanel();
            if (dockPanel == null) return;

            foreach (var doc in dockPanel.Documents)
            {
                if (doc is MonoGameDocument monoDoc)
                {
                    monoDoc.RefreshView();
                }
            }
        }

        /// <summary>
        /// Force full 3D scene re-init (reset Progressive AA + refresh).
        /// Use ONLY when realnej zmianie geometry.
        /// </summary>
        public void InvalidateScene3D()
        {
            var dockPanel = GetDockPanel();
            if (dockPanel == null) return;

            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

            foreach (var doc in dockPanel.Documents)
            {
                if (doc is MonoGameDocument monoDoc)
                {
                    monoDoc.RefreshView();
                }
            }
        }

        public void ApplyStyleToOverlaySelection(int styleIndex)
        {
            var win = GetMoleculeOverlayWindow();
            win.ApplyStyleToSelectedMolecule(styleIndex);
        }

        public void SetColorModeForSelectedAtoms(int colorMode)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0) return;

            int selectedCount = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected) selectedCount++;
            }

            if (selectedCount > 0)
            {
                Flags.Instance.show_properity = colorMode;
                RefreshActiveDocument();
                SetStatus($"Changed colors for {selectedCount} selected atoms", "Structures: 0", "🌈");
            }
            else
            {
                Flags.Instance.show_properity = colorMode;
                RefreshActiveDocument();
                SetStatus($"Changed colors for all atoms", "Structures: 0", "🌈");
            }
        }

        public void UpdateOverlayColors()
        {
            var win = GetMoleculeOverlayWindow();
            foreach (var mol in win._overlayManager.OverlayMolecules)
            {
                for (int i = 0; i < mol.WorkingAtoms.Count; i++)
                {
                    var atom = mol.WorkingAtoms[i];
                    atom.CustomColor = mol.Color;
                    mol.WorkingAtoms[i] = atom;
                }
            }
            win.RefreshList();
            win.RefreshOverlayView();
        }

        #endregion

        #region Public Methods — Thread-safe Wrappers
        //
        // ⭐ Dwa zestawy metod:
        //
        //   1. SetStatusSafe / SetProgressSafe / SetProgressIndeterminateSafe
        //      → conditional marshalling: if InvokeRequired, BeginInvoke;
        //        otherwise a synchronous call.
        //      → SAFE only if the caller knows it is on the UI thread
        //        OR checked InvokeRequired.
        //      → RISK: race — InvokeRequired may return false when the handle
        //        does not exist yet, and then SetStatus would run from a worker thread.
        //
        //   2. PostStatus / PostProgress / PostProgressIndeterminate  ⭐ NOWE
        //      → ALWAYS marshal via BeginInvoke (fire-and-forget).
        //      → SAFE from any thread — never call SetStatus
        //        synchronously from a worker thread.
        //      → USE THESE from worker threads (CavityDetector, MarchingCubes,
        //        VdWAnalysis, SphereFitAnalyzer, itd.).
        //

        /// <summary>
        /// Conditional marshalling — safe only when the caller knows it is
        /// on the UI thread, or checked InvokeRequired. From worker threads
        /// use PostStatus (always BeginInvoke).
        /// </summary>
        public void SetStatusSafe(string left, string center = "", string right = "✅")
        {
            if (IsDisposed || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => SetStatus(left, center, right))); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                return;
            }

            SetStatus(left, center, right);
        }

        /// <summary>
        /// Warunkowe marshallowanie — patrz SetStatusSafe.
        /// </summary>
        public void SetProgressSafe(int value)
        {
            if (IsDisposed || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => SetProgress(value))); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                return;
            }

            SetProgress(value);
        }

        /// <summary>
        /// Warunkowe marshallowanie — patrz SetStatusSafe.
        /// </summary>
        public void SetProgressIndeterminateSafe(bool active)
        {
            if (IsDisposed || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => SetProgressIndeterminate(active))); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
                return;
            }

            SetProgressIndeterminate(active);
        }

        /// <summary>
        /// ⭐ ALWAYS-ASYNC status update — safe z threads worker.
        ///
        /// Difference vs SetStatusSafe:
        ///   SetStatusSafe:  InvokeRequired ? BeginInvoke : sync
        ///   PostStatus:     ALWAYS BeginInvoke (fire-and-forget)
        ///
        /// Dlaczego PostStatus is potrzebny:
        ///   On worker threads InvokeRequired may return false when:
        ///     1. Handle windows still not exists (IsHandleCreated == false)
        ///     2. Handle exists, but WinForms treats the current thread as the UI thread
        ///        (np. w specyficznym stanie begin/end invoke)
        ///   W takich przypadkach SetStatusSafe wykona SetStatus() SYNCHRONICZNIE
        ///   z thread worker → cross-thread UI access → crash or race.
        ///
        ///   PostStatus omija this problem: always BeginInvoke.
        ///   Fire-and-forget — not czeka na wykonanie.
        /// </summary>
        public void PostStatus(string left, string center = "", string right = "✅")
        {
            if (IsDisposed || !IsHandleCreated) return;

            try
            {
                BeginInvoke(new Action(() => SetStatus(left, center, right)));
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        /// <summary>
        /// ⭐ ALWAYS-ASYNC progress update — safe z threads worker.
        /// </summary>
        public void PostProgress(int value)
        {
            if (IsDisposed || !IsHandleCreated) return;

            try
            {
                BeginInvoke(new Action(() => SetProgress(value)));
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        /// <summary>
        /// ⭐ ALWAYS-ASYNC indeterminate progress toggle — safe z threads worker.
        /// </summary>
        public void PostProgressIndeterminate(bool active)
        {
            if (IsDisposed || !IsHandleCreated) return;

            try
            {
                BeginInvoke(new Action(() => SetProgressIndeterminate(active)));
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        #endregion

        #region SizeChanged

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            Flags.Instance.VisualRevision++;
        }

        #endregion
    }
}
