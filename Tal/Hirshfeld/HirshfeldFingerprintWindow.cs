// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldFingerprintWindow.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Tal.Utils;
using WeifenLuo.WinFormsUI.Docking;
using Color = Microsoft.Xna.Framework.Color;
using GdiColor = System.Drawing.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// ⭐ v18 — Contact decomafterse:
 /// - CheckedListBox z typami contacts (C···H, O···H, itd.)
 /// - Tabelka decomafterse (area %, verts, sumy)
 /// - Filtr przekazywany do HirshfeldDocument przez OnContactFilterChanged
 /// - Start: wszystko selected = no filtra (null)
 /// - Suppress events while filling lists (avoid "only O" effect)
 /// - Filtrowanie heatmapy 2D after typie contactu (RebuildHeatmapFromRaw)
 /// </summary>
    public partial class HirshfeldFingerprintWindow : DockContent
    {
        #region Drag target enum

        private enum DragTarget
        {
            None,
            Title,
            AxisXLabel,
            AxisYLabel,
            TickNumbers
        }

        #endregion

        #region Constants

        private const float BASE_FONT_PT = 12f;
        private const float BIN_SIZE_DEFAULT = 0.005f;
        private const float RANGE_MAX_DEFAULT = 3.4f;

        private const int DENSITY_MULT = 1;
        private const int BIN_PIXELS = 1;

        private const int MAX_TEX_SIZE = 8192;
        private const int MAX_TOTAL_CELLS = 8192;
        private const long MAX_TEXTURE_BYTES = 512L * 1024 * 1024;

        private const float ZOOM_MIN = 0.5f;
        private const float ZOOM_MAX = 8.0f;
        private const float ZOOM_STEP = 1.15f;

        #endregion

        #region Fields

        private GraphicsDevice _device;
        private SpriteBatch _spriteBatch;
        private SpriteFont _spriteFont;
        private ContentManager _content;

        public Action<HashSet<string>> OnContactFilterChanged;

        private System.Windows.Forms.Timer _timer;
        private bool _deviceReady = false;
        private bool _disposed = false;

        private Texture2D _heatmapTexture;
        private RenderTarget2D _renderTarget;
        private Texture2D _whitePixel;
        private Texture2D _circleTexture;

        private HirshfeldAnalysisResults _analysis;

        private int _binsInRange = 60;
        private int _texSize = 0;

        private float _binSize = BIN_SIZE_DEFAULT;
        private float _rangeMin = 0.0f;
        private float _rangeMax = RANGE_MAX_DEFAULT;

        private float _origRangeMin = 0.0f;
        private float _origRangeMax = RANGE_MAX_DEFAULT;
        private float _origBinSize = BIN_SIZE_DEFAULT;

        private float[] _rawDE;
        private float[] _rawDI;
        private string[] _rawInnerAtom;
        private string[] _rawOuterAtom;

        private bool _showAxes = true;
        private bool _showDiagonal = true;
        private bool _showMajorGrid = true;
        private bool _showMinorGrid = true;

        private Vector2 _titleOffset = Vector2.Zero;
        private Vector2 _axisXLabelOffset = Vector2.Zero;
        private Vector2 _axisYLabelOffset = Vector2.Zero;
        private Vector2 _tickNumbersOffset = Vector2.Zero;

        private float _fontScale = 4f / BASE_FONT_PT;

        private float _zoomFactor = 1.0f;

        private bool _isDraggingLabel = false;
        private DragTarget _dragTarget = DragTarget.None;
        private System.Drawing.Point _lastMousePos;

        private bool _isZoomRectActive = false;
        private System.Drawing.Point _zoomRectStart;
        private System.Drawing.Point _zoomRectEnd;
        private bool _isLmbDownOnPlot = false;
        private System.Drawing.Point _lmbDownPos;

        private System.Drawing.Point? _clickMarkerPixel = null;

        public Action<int, int> OnRegionClicked;
        public Action<(int binDe, int binDi)[]> OnVertexBinsUpdated;

        private int _selectedBinDe = -1;
        private int _selectedBinDi = -1;

        private int _lastPlotX = 0;
        private int _lastPlotY = 0;
        private int _lastPlotSize = 0;

 // ⭐ Kontakt typu / filtr
        private List<string> _contactTypeKeys = new List<string>();
        private bool _suppressFilterEvent = false;
        private HashSet<string> _contactFilter = null;

        #endregion

        #region Constructor

        public HirshfeldFingerprintWindow(string title = "Hirshfeld Fingerprint")
        {
            InitializeComponent();

            Text = title;

            this.ClientSize = new Size(1100, 1000);
            this.MinimumSize = new Size(500, 500);

            BackColor = XnaToGdi(Flags.Instance.color1);
            _panel.BackColor = XnaToGdi(Flags.Instance.color1);

            _panel.HandleCreated += (s, e) =>
            {
                if (!_deviceReady) InitializeGraphics();
            };

            _panel.MouseDown += Panel_MouseDown;
            _panel.MouseMove += Panel_MouseMove;
            _panel.MouseUp += Panel_MouseUp;
            _panel.MouseWheel += Panel_MouseWheel;

            _btnSavePng.Click += (s, e) => SavePng();

            _numFontSize.ValueChanged += (s, e) =>
            {
                _fontScale = (float)_numFontSize.Value / BASE_FONT_PT;
            };

            _numBinSize.ValueChanged += (s, e) =>
            {
                float newBin = (float)_numBinSize.Value;
                if (MathF.Abs(newBin - _binSize) < 1e-6f) return;

                _binSize = newBin;
                RebuildHeatmapFromRaw();
                _lblStatus.Text = $"Bin size = {_binSize:F4} Å (re-bucketed)";
            };

            _numRangeMin.ValueChanged += (s, e) =>
            {
                float newMin = (float)_numRangeMin.Value;
                if (MathF.Abs(newMin - _rangeMin) < 1e-6f) return;

                _rangeMin = newMin;
                RebuildHeatmapFromRaw();
                _lblStatus.Text = $"Range start = {_rangeMin:F2} Å";
            };

            _btnResetOffsets.Click += (s, e) => ResetView();

            _chkShowAxes.CheckedChanged += (s, e) => _showAxes = _chkShowAxes.Checked;
            _chkShowDiagonal.CheckedChanged += (s, e) => _showDiagonal = _chkShowDiagonal.Checked;
            _chkShowMajorGrid.CheckedChanged += (s, e) => _showMajorGrid = _chkShowMajorGrid.Checked;
            _chkShowMinorGrid.CheckedChanged += (s, e) => _showMinorGrid = _chkShowMinorGrid.Checked;

            if (_lstContactTypes != null)
            {
                _lstContactTypes.ItemCheck += (s, e) =>
                {
                    if (_suppressFilterEvent) return;
                    BeginInvoke(new Action(ApplyContactFilterFromList));
                };
            }

            if (_btnSelectAllTypes != null)
                _btnSelectAllTypes.Click += (s, e) => SetAllContactTypes(true);

            if (_btnClearTypes != null)
                _btnClearTypes.Click += (s, e) => SetAllContactTypes(false);

            if (_btnCopyTable != null)
                _btnCopyTable.Click += (s, e) => CopyDecomposeTable();

            _timer = new System.Windows.Forms.Timer { Interval = 33 };
            _timer.Tick += (s, e) => RenderFrame();
            _timer.Start();

            Disposed += (s, e) => CleanupAll();
        }

        #endregion

        #region Public API

        public HirshfeldAnalysisResults GetCurrentAnalysis() => _analysis;

        public void SetRawData(float[] de, float[] di, string[] innerAtom, string[] outerAtom)
        {
            _rawDE = de;
            _rawDI = di;
            _rawInnerAtom = innerAtom;
            _rawOuterAtom = outerAtom;

            RebuildHeatmapFromRaw();
        }

        public void SetAnalysis(HirshfeldAnalysisResults analysis)
        {
            _analysis = analysis;

            _selectedBinDe = -1;
            _selectedBinDi = -1;
            _clickMarkerPixel = null;

            if (analysis != null)
            {
                if (analysis.FingerprintRangeMax > 0)
                {
                    _origRangeMax = analysis.FingerprintRangeMax;
                    _origRangeMin = analysis.FingerprintRangeMin;
                }

                if (analysis.FingerprintBinSize > 0)
                    _origBinSize = analysis.FingerprintBinSize;

                _rangeMax = _origRangeMax;
                _rangeMin = _origRangeMin;
                _binSize = _origBinSize;

                _numRangeMin.Value = Math.Clamp(
                    (decimal)_rangeMin,
                    _numRangeMin.Minimum,
                    _numRangeMin.Maximum);

                _numBinSize.Value = Math.Clamp(
                    (decimal)_binSize,
                    _numBinSize.Minimum,
                    _numBinSize.Maximum);

                if (_rawDE == null && analysis.Surface != null)
                {
                    var surf = analysis.Surface;
                    _rawDE = surf.DE;
                    _rawDI = surf.DI;
                    _rawInnerAtom = surf.NearestInnerAtomName;
                    _rawOuterAtom = surf.NearestOuterAtomName;
                }
            }

            PopulateContactTypeList(analysis);
            PopulateDecomposeTable(analysis);

            RebuildHeatmapFromRaw();

            _lblStatus.Text =
                $"Fingerprint ready\n" +
                $"bin = {_binSize:F4} Å, " +
                $"range = [{_rangeMin:F2}–{_rangeMax:F2}] Å\n" +
                $"binsInRange = {_binsInRange}, tex = {_texSize}px, " +
                $"zoom = {_zoomFactor:F2}×\n" +
                $"contacts = {_contactTypeKeys.Count} types\n" +
                $"LMB = bin, LMB drag = zoom, RMB dbl = reset";
        }

        private void ResetView()
        {
            _titleOffset = Vector2.Zero;
            _axisXLabelOffset = Vector2.Zero;
            _axisYLabelOffset = Vector2.Zero;
            _tickNumbersOffset = Vector2.Zero;

            _numFontSize.Value = 4;
            _fontScale = (float)_numFontSize.Value / BASE_FONT_PT;

            _zoomFactor = 1.0f;

            _rangeMin = _origRangeMin;
            _rangeMax = _origRangeMax;
            _binSize = _origBinSize;

            _numRangeMin.Value = Math.Clamp(
                (decimal)_rangeMin,
                _numRangeMin.Minimum,
                _numRangeMin.Maximum);

            _numBinSize.Value = Math.Clamp(
                (decimal)_binSize,
                _numBinSize.Minimum,
                _numBinSize.Maximum);

            _clickMarkerPixel = null;
            _selectedBinDe = -1;
            _selectedBinDi = -1;
            OnRegionClicked?.Invoke(-1, -1);

            RebuildHeatmapFromRaw();

            _lblStatus.Text =
                $"Reset — range = [{_rangeMin:F2}–{_rangeMax:F2}] Å, " +
                $"bin = {_binSize:F4} Å, zoom = {_zoomFactor:F2}×";
        }

        #endregion

        #region Contact types — list + table + filter

        private void PopulateContactTypeList(HirshfeldAnalysisResults analysis)
        {
            if (_lstContactTypes == null) return;

            _suppressFilterEvent = true;

            try
            {
                _lstContactTypes.Items.Clear();
                _contactTypeKeys.Clear();

                if (analysis?.ContactTypePercentages == null
                    || analysis.ContactTypePercentages.Count == 0)
                {
                    _contactFilter = null;
                    return;
                }

                foreach (var kv in analysis.ContactTypePercentages
                             .OrderByDescending(x => x.Value))
                {
                    _contactTypeKeys.Add(kv.Key);

                    int verts = analysis.ContactTypeCounts != null
                        && analysis.ContactTypeCounts.TryGetValue(kv.Key, out int c)
                        ? c : 0;

                    string label = $"{kv.Key}  ({kv.Value:F1}%, {verts} v)";
                    _lstContactTypes.Items.Add(label, true);
                }
            }
            finally
            {
                _suppressFilterEvent = false;
            }

            _contactFilter = null;
            OnContactFilterChanged?.Invoke(null);
        }

        private void PopulateDecomposeTable(HirshfeldAnalysisResults analysis)
        {
            if (_txtDecomposeTable == null) return;

            if (analysis == null || string.IsNullOrEmpty(analysis.DecomposeTableText))
            {
                _txtDecomposeTable.Text = "(no decompose data)";
                return;
            }

            _txtDecomposeTable.Text = analysis.DecomposeTableText;
        }

        private void SetAllContactTypes(bool check)
        {
            if (_lstContactTypes == null) return;

            _suppressFilterEvent = true;
            try
            {
                for (int i = 0; i < _lstContactTypes.Items.Count; i++)
                    _lstContactTypes.SetItemChecked(i, check);
            }
            finally
            {
                _suppressFilterEvent = false;
            }

            ApplyContactFilterFromList();
        }

        private void ApplyContactFilterFromList()
        {
            if (_suppressFilterEvent) return;
            if (_lstContactTypes == null) return;

            var set = new HashSet<string>();
            for (int i = 0; i < _lstContactTypes.Items.Count; i++)
            {
                if (_lstContactTypes.GetItemChecked(i) && i < _contactTypeKeys.Count)
                    set.Add(_contactTypeKeys[i]);
            }

            bool allOn = _contactTypeKeys.Count > 0
                      && set.Count == _contactTypeKeys.Count;
            bool none = set.Count == 0;

            if (allOn)
                _contactFilter = null;
            else if (none)
                _contactFilter = new HashSet<string>();
            else
                _contactFilter = set;

            OnContactFilterChanged?.Invoke(_contactFilter);
            RebuildHeatmapFromRaw();

            if (_contactFilter == null)
                _lblStatus.Text = $"Filter: ALL ({_contactTypeKeys.Count} types)";
            else if (_contactFilter.Count == 0)
                _lblStatus.Text = "Filter: NONE (mesh grayed out)";
            else
                _lblStatus.Text = "Filter: " + string.Join(", ", _contactFilter);
        }

        private void CopyDecomposeTable()
        {
            if (_txtDecomposeTable == null) return;

            try
            {
                if (!string.IsNullOrEmpty(_txtDecomposeTable.Text))
                {
                    Clipboard.SetText(_txtDecomposeTable.Text);
                    _lblStatus.Text = "Decompose table copied to clipboard.";
                }
            }
            catch (Exception ex)
            {
                _lblStatus.Text = $"Copy failed: {ex.Message}";
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
                    GraphicsProfile.HiDef,
                    pp);

                _spriteBatch = new SpriteBatch(_device);

                _whitePixel = new Texture2D(_device, 1, 1);
                _whitePixel.SetData(new[] { Color.White });

                _circleTexture = CreateCircleTexture(32);

                LoadLocalSpriteFont();

                _deviceReady = true;

                LogSuccess($"[Fingerprint] Graphics initialized ({w}x{h}), " +
                           $"font={(_spriteFont != null ? "OK" : "NULL")}");

                if (_rawDE != null)
                    RebuildHeatmapFromRaw();
            }
            catch (Exception ex)
            {
                LogError($"[Fingerprint] Init error: {ex.Message}");
            }
        }

        private Texture2D CreateCircleTexture(int size)
        {
            var data = new Color[size * size];
            float radius = size / 2f - 1f;
            float cx = size / 2f - 0.5f;
            float cy = size / 2f - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float d = MathF.Sqrt(dx * dx + dy * dy);

                    float alpha;
                    if (d <= radius - 0.5f) alpha = 1f;
                    else if (d >= radius + 0.5f) alpha = 0f;
                    else alpha = radius + 0.5f - d;

                    data[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            var tex = new Texture2D(_device, size, size, false, SurfaceFormat.Color);
            tex.SetData(data);
            return tex;
        }

        private void LoadLocalSpriteFont()
        {
            if (_device == null) return;

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? baseDir;

                string fontName = Flags.Instance.LabelFontName ?? "Segoe UI";
                string[] fontNames = GetFontAssetCandidates(fontName);

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
                    foreach (var name in new[] { "SpriteFont1", "SpriteFont" })
                    {
                        string[] candidates = new[]
                        {
                            Path.Combine(exeDir, name + ".xnb"),
                            Path.Combine(baseDir, name + ".xnb"),
                            Path.Combine(exeDir, "Content", name + ".xnb"),
                            Path.Combine(baseDir, "Content", name + ".xnb"),
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
                }

                if (fontFile == null)
                {
                    LogWarn("[Fingerprint] ❌ SpriteFont NOT FOUND");
                    _spriteFont = null;
                    return;
                }

                string contentDir = Path.GetDirectoryName(fontFile);

                var services = new ServiceContainer();
                services.AddService<IGraphicsDeviceService>(
                    new SimpleGraphicsDeviceService(_device));

                _content = new ContentManager(services, contentDir);
                _spriteFont = _content.Load<SpriteFont>(assetName);

                LogSuccess($"[Fingerprint] ✓ SpriteFont loaded: {fontFile}");
            }
            catch (Exception ex)
            {
                LogWarn($"[Fingerprint] ❌ SpriteFont load FAILED: {ex.Message}");
                _spriteFont = null;
            }
        }

        private static string[] GetFontAssetCandidates(string fontName)
        {
            switch (fontName)
            {
                case "Arial":
                    return new[] { "Arial", "SpriteFont1" };
                case "Times New Roman":
                    return new[] { "TimesNewRoman", "Times New Roman", "SpriteFont1" };
                case "Segoe UI":
                default:
                    return new[] { "SegoeUI", "Segoe UI", "SpriteFont1" };
            }
        }

        #endregion

        #region Heatmap

        private void RebuildHeatmapFromRaw()
        {
            if (_rawDE == null || _rawDI == null || _device == null) return;

            if (_rangeMax <= _rangeMin) _rangeMax = _rangeMin + RANGE_MAX_DEFAULT;
            if (_binSize <= 0) _binSize = BIN_SIZE_DEFAULT;

            float span = _rangeMax - _rangeMin;
            int binsInRange = Math.Max(1, (int)MathF.Ceiling(span / _binSize));

            if (binsInRange > MAX_TOTAL_CELLS)
            {
                LogWarn($"[Fingerprint] binsInRange={binsInRange} > {MAX_TOTAL_CELLS}, clamping");
                binsInRange = MAX_TOTAL_CELLS;
            }

            _binsInRange = binsInRange;
            _texSize = binsInRange * DENSITY_MULT * BIN_PIXELS;

            var bins = new int[binsInRange, binsInRange];
            int maxCount = 0;
            int n = _rawDE.Length;

            var vertexBins = new (int binDe, int binDi)[n];
            for (int i = 0; i < n; i++)
                vertexBins[i] = (-1, -1);

            string[] contactTypes = _analysis?.ContactTypes;
            HashSet<string> filter = _contactFilter;

            for (int i = 0; i < n; i++)
            {
 // ⭐ Filtr typu contactu
                if (filter != null)
                {
                    if (filter.Count == 0) continue;

                    if (contactTypes != null && i < contactTypes.Length)
                    {
                        string typ = contactTypes[i];
                        if (string.IsNullOrEmpty(typ) || !filter.Contains(typ))
                            continue;
                    }
                }

                float de = _rawDE[i];
                float di = _rawDI[i];

                if (de < _rangeMin || de > _rangeMax) continue;
                if (di < _rangeMin || di > _rangeMax) continue;

                int binDe = (int)MathF.Floor((de - _rangeMin) / _binSize);
                int binDi = (int)MathF.Floor((di - _rangeMin) / _binSize);

                if (binDe < 0 || binDe >= binsInRange) continue;
                if (binDi < 0 || binDi >= binsInRange) continue;

                vertexBins[i] = (binDe, binDi);
                bins[binDi, binDe]++;

                if (bins[binDi, binDe] > maxCount)
                    maxCount = bins[binDi, binDe];
            }

            if (_analysis != null)
            {
                var hist = new Dictionary<(int binDe, int binDi), int>();
                for (int bi = 0; bi < binsInRange; bi++)
                    for (int bj = 0; bj < binsInRange; bj++)
                        if (bins[bi, bj] > 0)
                            hist[(bj, bi)] = bins[bi, bj];

                _analysis.FingerprintHistogram = hist;
                _analysis.FingerprintBinSize = _binSize;
                _analysis.FingerprintRangeMin = _rangeMin;
                _analysis.FingerprintRangeMax = _rangeMax;
                _analysis.VertexBins = vertexBins;
            }

            OnVertexBinsUpdated?.Invoke(vertexBins);

            if (maxCount == 0)
            {
                _heatmapTexture?.Dispose();
                _heatmapTexture = null;
                LogWarn("[Fingerprint] Empty histogram (filter active?)");
                return;
            }

            long totalBytes = (long)_texSize * _texSize * 4;
            if (totalBytes > MAX_TEXTURE_BYTES)
            {
                LogError($"[Fingerprint] Texture too large: {totalBytes:N0} bytes");
                return;
            }

            var pixels = new uint[_texSize * _texSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = 0xFFFFFFFF;

            float logMax = MathF.Log(1f + maxCount);

            for (int bj = 0; bj < binsInRange; bj++)
            {
                int cellRowBase = (binsInRange - 1 - bj) * DENSITY_MULT;

                for (int bi = 0; bi < binsInRange; bi++)
                {
                    int count = bins[bi, bj];
                    if (count <= 0) continue;

                    float intensity = MathF.Log(1f + count) / logMax;
                    intensity = MathF.Pow(intensity, 0.6f);
                    if (intensity < 0.15f) intensity = 0.15f;

                    byte r, g, b;
                    if (intensity < 0.25f)
                    {
                        float t = intensity / 0.25f;
                        r = (byte)(255 - 255 * t); g = 255; b = 255;
                    }
                    else if (intensity < 0.50f)
                    {
                        float t = (intensity - 0.25f) / 0.25f;
                        r = 0; g = (byte)(255 - 100 * t); b = 255;
                    }
                    else if (intensity < 0.75f)
                    {
                        float t = (intensity - 0.50f) / 0.25f;
                        r = (byte)(155 * t); g = (byte)(155 - 155 * t); b = 255;
                    }
                    else
                    {
                        float t = (intensity - 0.75f) / 0.25f;
                        r = (byte)(155 + 100 * t); g = 0; b = (byte)(255 - 255 * t);
                    }

                    uint col = (uint)(0xFF << 24 | b << 16 | g << 8 | r);
                    int cellColBase = bi * DENSITY_MULT;

                    for (int dy = 0; dy < DENSITY_MULT; dy++)
                    {
                        int subRow = cellRowBase + dy;
                        int pixRowStart = subRow * BIN_PIXELS;
                        int pixRowEnd = pixRowStart + BIN_PIXELS;
                        if (pixRowStart < 0 || pixRowEnd > _texSize) continue;

                        for (int dx = 0; dx < DENSITY_MULT; dx++)
                        {
                            int subCol = cellColBase + dx;
                            int pixColStart = subCol * BIN_PIXELS;
                            int pixColEnd = pixColStart + BIN_PIXELS;
                            if (pixColStart < 0 || pixColEnd > _texSize) continue;

                            for (int py = pixRowStart; py < pixRowEnd; py++)
                            {
                                int rowOffset = py * _texSize;
                                for (int px = pixColStart; px < pixColEnd; px++)
                                    pixels[rowOffset + px] = col;
                            }
                        }
                    }
                }
            }

            _heatmapTexture?.Dispose();
            _heatmapTexture = null;

            try
            {
                _heatmapTexture = new Texture2D(_device, _texSize, _texSize,
                    false, SurfaceFormat.Color);
                _heatmapTexture.SetData(pixels);

                string filterInfo = _contactFilter == null
                    ? "ALL"
                    : (_contactFilter.Count == 0 ? "NONE" : $"{_contactFilter.Count} types");

                LogSuccess($"[Fingerprint] ✓ Heatmap: {_texSize}×{_texSize} px, " +
                           $"bins={binsInRange}, binSize={_binSize:F4}, " +
                           $"range=[{_rangeMin:F2}-{_rangeMax:F2}], " +
                           $"filter={filterInfo}, maxCount={maxCount}");
            }
            catch (Exception ex)
            {
                LogError($"[Fingerprint] ❌ Texture creation failed: {ex.Message}");
                _heatmapTexture = null;
            }
        }

        #endregion

        #region Rendering

        private void RenderFrame()
        {
            if (_disposed || !_deviceReady || _device == null || _device.IsDisposed) return;
            if (_panel == null || !_panel.IsHandleCreated || !_panel.Visible) return;

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
                        }
                    }
                }

                _device.Clear(Flags.Instance.color1);

                if (_heatmapTexture == null || _spriteBatch == null)
                {
                    _device.Present();
                    return;
                }

                ComputePlotLayout(_device.Viewport.Width, _device.Viewport.Height,
                    out int plotX, out int plotY, out int plotSize);

                _lastPlotX = plotX;
                _lastPlotY = plotY;
                _lastPlotSize = plotSize;

                _spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone);

                _spriteBatch.Draw(
                    _heatmapTexture,
                    new Rectangle(plotX, plotY, plotSize, plotSize),
                    Color.White);

                if (_showMajorGrid) DrawMajorGrid(plotX, plotY, plotSize);
                if (_showMinorGrid) DrawMinorGrid(plotX, plotY, plotSize);
                if (_showDiagonal) DrawDiagonal(plotX, plotY, plotSize);

                DrawHighlightedBin(plotX, plotY, plotSize);

                if (_showAxes)
                {
                    if (_spriteFont != null)
                        DrawAxesFull(plotX, plotY, plotSize);
                    else
                        DrawAxesWithoutText(plotX, plotY, plotSize);
                }

                if (_isZoomRectActive)
                    DrawZoomRectangle();

                if (_clickMarkerPixel.HasValue)
                    DrawClickMarker(_clickMarkerPixel.Value);

                _spriteBatch.End();

                _device.Present();
            }
            catch (Exception ex)
            {
                LogError($"[Fingerprint] Render error: {ex.Message}");
            }
        }

        private void ComputePlotLayout(int w, int h, out int plotX, out int plotY, out int plotSize)
        {
            int marginLeft = 110;
            int marginRight = 30;
            int marginTop = 60;
            int marginBottom = 110;

            int availW = Math.Max(100, w - marginLeft - marginRight);
            int availH = Math.Max(100, h - marginTop - marginBottom);

            int basePlotSize = Math.Min(availW, availH);

            plotSize = (int)(basePlotSize * _zoomFactor);
            if (plotSize < 100) plotSize = 100;

            plotX = marginLeft + (availW - plotSize) / 2;
            plotY = marginTop + (availH - plotSize) / 2;
        }

        private void DrawMajorGrid(int px, int py, int plotSize)
        {
            float span = _rangeMax - _rangeMin;
            if (span <= 0) return;

            float majorStep = ComputeMajorStep(span);

            Color majorColor = new Color((byte)120, (byte)120, (byte)120, (byte)255);

            float v = _rangeMin + majorStep;
            while (v < _rangeMax - 0.001f)
            {
                float t = (v - _rangeMin) / span;
                int x = px + (int)(t * plotSize);
                int y = py + plotSize - (int)(t * plotSize);

                _spriteBatch.Draw(_whitePixel,
                    new Rectangle(x - 1, py, 2, plotSize), majorColor);
                _spriteBatch.Draw(_whitePixel,
                    new Rectangle(px, y - 1, plotSize, 2), majorColor);

                v += majorStep;
            }
        }

        private void DrawMinorGrid(int px, int py, int plotSize)
        {
            float span = _rangeMax - _rangeMin;
            if (span <= 0) return;

            float majorStep = ComputeMajorStep(span);
            float minorStep = majorStep / 2f;

            Color minorColor = new Color((byte)200, (byte)200, (byte)200, (byte)255);

            float v = _rangeMin + minorStep;
            while (v < _rangeMax - 0.001f)
            {
                float rel = (v - _rangeMin) / majorStep;
                if (MathF.Abs(rel - MathF.Round(rel)) < 0.01f)
                {
                    v += minorStep;
                    continue;
                }

                float t = (v - _rangeMin) / span;
                int x = px + (int)(t * plotSize);
                int y = py + plotSize - (int)(t * plotSize);

                _spriteBatch.Draw(_whitePixel,
                    new Rectangle(x, py, 1, plotSize), minorColor);
                _spriteBatch.Draw(_whitePixel,
                    new Rectangle(px, y, plotSize, 1), minorColor);

                v += minorStep;
            }
        }

        private static float ComputeMajorStep(float span)
        {
            if (span <= 1.0f) return 0.1f;
            if (span <= 2.0f) return 0.25f;
            if (span <= 5.0f) return 0.5f;
            if (span <= 10.0f) return 1.0f;
            return 2.0f;
        }

        private void DrawDiagonal(int px, int py, int plotSize)
        {
            var start = new Vector2(px, py + plotSize);
            var end = new Vector2(px + plotSize, py);

            DrawLine(start, end, new Color((byte)255, (byte)255, (byte)255, (byte)220), 4);
            DrawLine(start, end, new Color((byte)0, (byte)0, (byte)0, (byte)255), 2);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 diff = end - start;
            float length = diff.Length();
            if (length < 0.001f) return;

            float angle = MathF.Atan2(diff.Y, diff.X);

            _spriteBatch.Draw(
                _whitePixel,
                start,
                null,
                color,
                angle,
                new Vector2(0, 0.5f),
                new Vector2(length, thickness),
                SpriteEffects.None,
                0.95f);
        }

        private void DrawHighlightedBin(int px, int py, int plotSize)
        {
            if (_selectedBinDe < 0 || _selectedBinDi < 0) return;
            if (_binsInRange <= 0) return;

            float cellW = plotSize / (float)_binsInRange;
            float cellH = plotSize / (float)_binsInRange;

            float x = px + _selectedBinDe * cellW;
            float y = py + plotSize - (_selectedBinDi + 1) * cellH;

            int ix = (int)x;
            int iy = (int)y;
            int iw = Math.Max(2, (int)cellW);
            int ih = Math.Max(2, (int)cellH);

            Color highlight = new Color((byte)255, (byte)255, (byte)0, (byte)255);

            _spriteBatch.Draw(_whitePixel, new Rectangle(ix, iy, iw, 2), highlight);
            _spriteBatch.Draw(_whitePixel, new Rectangle(ix, iy + ih - 2, iw, 2), highlight);
            _spriteBatch.Draw(_whitePixel, new Rectangle(ix, iy, 2, ih), highlight);
            _spriteBatch.Draw(_whitePixel, new Rectangle(ix + iw - 2, iy, 2, ih), highlight);
        }

        private void DrawClickMarker(System.Drawing.Point pos)
        {
            const int DIAM = 14;
            int half = DIAM / 2;

            var dest = new Rectangle(pos.X - half, pos.Y - half, DIAM, DIAM);
            var whiteRing = new Rectangle(pos.X - half - 1, pos.Y - half - 1, DIAM + 2, DIAM + 2);

            _spriteBatch.Draw(_circleTexture, whiteRing, Color.White);
            _spriteBatch.Draw(_circleTexture, dest, Color.Red);
        }

        private void DrawZoomRectangle()
        {
            int x1 = Math.Min(_zoomRectStart.X, _zoomRectEnd.X);
            int y1 = Math.Min(_zoomRectStart.Y, _zoomRectEnd.Y);
            int x2 = Math.Max(_zoomRectStart.X, _zoomRectEnd.X);
            int y2 = Math.Max(_zoomRectStart.Y, _zoomRectEnd.Y);

            int w = x2 - x1;
            int h = y2 - y1;

            if (w < 1 || h < 1) return;

            Color fill = new Color((byte)0, (byte)120, (byte)215, (byte)60);
            Color border = new Color((byte)0, (byte)120, (byte)215, (byte)255);

            _spriteBatch.Draw(_whitePixel, new Rectangle(x1, y1, w, h), fill);

            _spriteBatch.Draw(_whitePixel, new Rectangle(x1, y1, w, 1), border);
            _spriteBatch.Draw(_whitePixel, new Rectangle(x1, y2 - 1, w, 1), border);
            _spriteBatch.Draw(_whitePixel, new Rectangle(x1, y1, 1, h), border);
            _spriteBatch.Draw(_whitePixel, new Rectangle(x2 - 1, y1, 1, h), border);
        }

        #endregion

        #region Axes rendering

        private void DrawAxesFull(int px, int py, int plotSize)
        {
            float span = _rangeMax - _rangeMin;
            if (span <= 0) return;

            float scale = _fontScale;

            Color textColor = new Color((byte)40, (byte)40, (byte)40, (byte)255);
            Color axisColor = new Color((byte)80, (byte)80, (byte)80, (byte)255);

            int axisThickness = 2;

            _spriteBatch.Draw(_whitePixel,
                new Rectangle(px, py + plotSize, plotSize, axisThickness),
                axisColor);
            _spriteBatch.Draw(_whitePixel,
                new Rectangle(px - axisThickness, py, axisThickness, plotSize),
                axisColor);

            float majorStep = ComputeMajorStep(span);
            float tickLabelScale = scale;
            int tickLen = 14;
            int tickLabelGap = 22;

            float numOffX = _tickNumbersOffset.X;
            float numOffY = _tickNumbersOffset.Y;

            for (float v = _rangeMin; v <= _rangeMax + 0.001f; v += majorStep)
            {
                float value = MathF.Round(v * 100f) / 100f;

                float t = (value - _rangeMin) / span;
                if (t < -0.001f || t > 1.001f) continue;

                int tickX = px + (int)(t * plotSize);
                int tickY = py + plotSize - (int)(t * plotSize);

                _spriteBatch.Draw(_whitePixel,
                    new Rectangle(tickX - 1, py + plotSize, 3, tickLen),
                    axisColor);
                _spriteBatch.Draw(_whitePixel,
                    new Rectangle(px - tickLen, tickY - 1, tickLen, 3),
                    axisColor);

                string text = value >= 1f ? value.ToString("F1") : value.ToString("F2");

                Vector2 szX = _spriteFont.MeasureString(text) * tickLabelScale;
                _spriteBatch.DrawString(_spriteFont, text,
                    new Vector2(tickX - szX.X / 2 + numOffX,
                                py + plotSize + tickLen + tickLabelGap + numOffY),
                    textColor, 0, Vector2.Zero, tickLabelScale,
                    SpriteEffects.None, 0.95f);

                Vector2 szY = _spriteFont.MeasureString(text) * tickLabelScale;
                _spriteBatch.DrawString(_spriteFont, text,
                    new Vector2(px - tickLen - tickLabelGap - szY.X + numOffX,
                                tickY - szY.Y / 2 + numOffY),
                    textColor, 0, Vector2.Zero, tickLabelScale,
                    SpriteEffects.None, 0.95f);
            }

            float axisLabelScale = 1.2f * scale;
            string xLabel = "d_e (Å)";
            Vector2 xSize = _spriteFont.MeasureString(xLabel) * axisLabelScale;
            Vector2 xLabelPos = new Vector2(
                px + plotSize / 2 - xSize.X / 2 + _axisXLabelOffset.X,
                py + plotSize + tickLen + tickLabelGap + 50 * scale + _axisXLabelOffset.Y);

            _spriteBatch.DrawString(_spriteFont, xLabel,
                xLabelPos, textColor, 0, Vector2.Zero, axisLabelScale,
                SpriteEffects.None, 0.95f);

            string yLabel = "d_i (Å)";
            Vector2 ySize = _spriteFont.MeasureString(yLabel) * axisLabelScale;
            Vector2 yLabelPos = new Vector2(
                px - 100 * scale + _axisYLabelOffset.X,
                py + plotSize / 2 + ySize.X / 2 + _axisYLabelOffset.Y);

            _spriteBatch.DrawString(_spriteFont, yLabel,
                yLabelPos, textColor, -MathHelper.PiOver2, Vector2.Zero,
                axisLabelScale, SpriteEffects.None, 0.95f);

            string title = "Hirshfeld Fingerprint";
            float titleScale = 1.4f * scale;
            Vector2 titleSize = _spriteFont.MeasureString(title) * titleScale;

            Vector2 titlePos = new Vector2(
                px + plotSize / 2 - titleSize.X / 2 + _titleOffset.X,
                py - titleSize.Y - 30 + _titleOffset.Y);

            _spriteBatch.DrawString(_spriteFont, title,
                titlePos, textColor, 0, Vector2.Zero, titleScale,
                SpriteEffects.None, 0.95f);
        }

        private void DrawAxesWithoutText(int px, int py, int plotSize)
        {
            int axisThickness = 2;
            Color axisColor = new Color((byte)80, (byte)80, (byte)80, (byte)255);

            _spriteBatch.Draw(_whitePixel,
                new Rectangle(px, py + plotSize, plotSize, axisThickness),
                axisColor);
            _spriteBatch.Draw(_whitePixel,
                new Rectangle(px - axisThickness, py, axisThickness, plotSize),
                axisColor);
        }

        #endregion

        #region Mouse

        private void Panel_MouseDown(object sender, MouseEventArgs e)
        {
            _panel.Focus();

            if (e.Button == MouseButtons.Right && e.Clicks == 2)
            {
                ResetView();
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                bool clickedInPlot =
                    e.X >= _lastPlotX && e.X < _lastPlotX + _lastPlotSize &&
                    e.Y >= _lastPlotY && e.Y < _lastPlotY + _lastPlotSize;

                if (clickedInPlot)
                {
                    _isLmbDownOnPlot = true;
                    _lmbDownPos = e.Location;
                    _zoomRectStart = e.Location;
                    _zoomRectEnd = e.Location;
                }
                else
                {
                    _dragTarget = PickDragTarget(e);

                    if (_dragTarget != DragTarget.None)
                    {
                        _isDraggingLabel = true;
                        _lastMousePos = e.Location;

                        if (_lblStatus != null)
                        {
                            string name = _dragTarget switch
                            {
                                DragTarget.Title => "title",
                                DragTarget.AxisXLabel => "d_e (Å)",
                                DragTarget.AxisYLabel => "d_i (Å)",
                                DragTarget.TickNumbers => "tick numbers",
                                _ => "?"
                            };
                            _lblStatus.Text = $"Dragging: {name}";
                        }
                    }
                    else if (_selectedBinDe >= 0)
                    {
                        _selectedBinDe = -1;
                        _selectedBinDi = -1;
                        _clickMarkerPixel = null;
                        OnRegionClicked?.Invoke(-1, -1);
                        _lblStatus.Text = "Highlight cleared.";
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                bool clickedInPlot =
                    e.X >= _lastPlotX && e.X < _lastPlotX + _lastPlotSize &&
                    e.Y >= _lastPlotY && e.Y < _lastPlotY + _lastPlotSize;

                if (clickedInPlot && _analysis != null && _binsInRange > 0)
                {
                    float tx = (e.X - _lastPlotX) / (float)_lastPlotSize;
                    float ty = (e.Y - _lastPlotY) / (float)_lastPlotSize;

                    int binDe = (int)(tx * _binsInRange);
                    int binDi = (int)((1f - ty) * _binsInRange);

                    binDe = Math.Clamp(binDe, 0, _binsInRange - 1);
                    binDi = Math.Clamp(binDi, 0, _binsInRange - 1);

                    _selectedBinDe = binDe;
                    _selectedBinDi = binDi;
                    _clickMarkerPixel = new System.Drawing.Point(e.X, e.Y);

                    OnRegionClicked?.Invoke(binDe, binDi);

                    float deVal = _rangeMin + binDe * _binSize;
                    float diVal = _rangeMin + binDi * _binSize;

                    int count = 0;
                    if (_analysis.FingerprintHistogram.TryGetValue((binDe, binDi), out int c))
                        count = c;

                    _lblStatus.Text =
                        $"Selected bin: d_e={deVal:F3} Å, d_i={diVal:F3} Å\n" +
                        $"Vertices: {count}";
                }
                else
                {
                    _isZoomRectActive = true;
                    _zoomRectStart = e.Location;
                    _zoomRectEnd = e.Location;
                }
            }
        }

        private DragTarget PickDragTarget(MouseEventArgs e)
        {
            var mousePos = new Vector2(e.X, e.Y);

            const float MAX_PICK_DIST = 70f;

            float bestDist = MAX_PICK_DIST;
            DragTarget best = DragTarget.None;

            float scale = _fontScale;

            {
                var text = "Hirshfeld Fingerprint";
                float titleScale = 1.4f * scale;
                var size = (_spriteFont?.MeasureString(text) ?? Vector2.Zero) * titleScale;

                float tx = _lastPlotX + _lastPlotSize / 2 - size.X / 2 + _titleOffset.X;
                float ty = _lastPlotY - size.Y - 30 + _titleOffset.Y;

                var center = new Vector2(tx + size.X / 2, ty + size.Y / 2);
                float d = Vector2.Distance(mousePos, center);
                if (d < bestDist) { bestDist = d; best = DragTarget.Title; }
            }

            {
                var text = "d_e (Å)";
                float axisLabelScale = 1.2f * scale;
                var size = (_spriteFont?.MeasureString(text) ?? Vector2.Zero) * axisLabelScale;

                float tx = _lastPlotX + _lastPlotSize / 2 - size.X / 2 + _axisXLabelOffset.X;
                float ty = _lastPlotY + _lastPlotSize + 14 + 22 + 50 * scale + _axisXLabelOffset.Y;

                var center = new Vector2(tx + size.X / 2, ty + size.Y / 2);
                float d = Vector2.Distance(mousePos, center);
                if (d < bestDist) { bestDist = d; best = DragTarget.AxisXLabel; }
            }

            {
                var text = "d_i (Å)";
                float axisLabelScale = 1.2f * scale;
                var size = (_spriteFont?.MeasureString(text) ?? Vector2.Zero) * axisLabelScale;

                float tx = _lastPlotX - 100 * scale + _axisYLabelOffset.X;
                float ty = _lastPlotY + _lastPlotSize / 2 + size.X / 2 + _axisYLabelOffset.Y;

                var center = new Vector2(tx + size.Y / 2, ty - size.X / 2);
                float d = Vector2.Distance(mousePos, center);
                if (d < bestDist) { bestDist = d; best = DragTarget.AxisYLabel; }
            }

            {
                float labelScale = scale;
                var sampleSize = (_spriteFont?.MeasureString("0.00") ?? Vector2.Zero) * labelScale;

                float sampleX = _lastPlotX + _lastPlotSize / 2 + _tickNumbersOffset.X;
                float sampleY = _lastPlotY + _lastPlotSize + 14 + 22 + _tickNumbersOffset.Y;

                var center = new Vector2(sampleX, sampleY + sampleSize.Y / 2);
                float d = Vector2.Distance(mousePos, center);
                if (d < bestDist) { bestDist = d; best = DragTarget.TickNumbers; }
            }

            return best;
        }

        private void Panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isLmbDownOnPlot && e.Button == MouseButtons.Left)
            {
                int dx = Math.Abs(e.X - _lmbDownPos.X);
                int dy = Math.Abs(e.Y - _lmbDownPos.Y);

                const int DRAG_THRESHOLD = 6;

                if (dx > DRAG_THRESHOLD || dy > DRAG_THRESHOLD)
                {
                    _isZoomRectActive = true;
                    _zoomRectEnd = e.Location;
                }
                return;
            }

            if (_isZoomRectActive)
            {
                _zoomRectEnd = e.Location;
                return;
            }

            if (!_isDraggingLabel || e.Button != MouseButtons.Left) return;

            float ddx = e.X - _lastMousePos.X;
            float ddy = e.Y - _lastMousePos.Y;

            switch (_dragTarget)
            {
                case DragTarget.Title:
                    _titleOffset += new Vector2(ddx, ddy);
                    break;
                case DragTarget.AxisXLabel:
                    _axisXLabelOffset += new Vector2(ddx, ddy);
                    break;
                case DragTarget.AxisYLabel:
                    _axisYLabelOffset += new Vector2(ddx, ddy);
                    break;
                case DragTarget.TickNumbers:
                    _tickNumbersOffset += new Vector2(ddx, ddy);
                    break;
            }

            _lastMousePos = e.Location;
        }

        private void Panel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_isLmbDownOnPlot)
                {
                    if (_isZoomRectActive)
                    {
                        _zoomRectEnd = e.Location;
                        ApplyZoomRect();
                        _isZoomRectActive = false;
                    }
                    else
                    {
                        bool clickedInPlot =
                            e.X >= _lastPlotX && e.X < _lastPlotX + _lastPlotSize &&
                            e.Y >= _lastPlotY && e.Y < _lastPlotY + _lastPlotSize;

                        if (clickedInPlot && _analysis != null && _binsInRange > 0)
                        {
                            float tx = (e.X - _lastPlotX) / (float)_lastPlotSize;
                            float ty = (e.Y - _lastPlotY) / (float)_lastPlotSize;

                            int binDe = (int)(tx * _binsInRange);
                            int binDi = (int)((1f - ty) * _binsInRange);

                            binDe = Math.Clamp(binDe, 0, _binsInRange - 1);
                            binDi = Math.Clamp(binDi, 0, _binsInRange - 1);

                            _selectedBinDe = binDe;
                            _selectedBinDi = binDi;
                            _clickMarkerPixel = new System.Drawing.Point(e.X, e.Y);

                            OnRegionClicked?.Invoke(binDe, binDi);

                            float deVal = _rangeMin + binDe * _binSize;
                            float diVal = _rangeMin + binDi * _binSize;

                            int count = 0;
                            if (_analysis.FingerprintHistogram.TryGetValue((binDe, binDi), out int c))
                                count = c;

                            _lblStatus.Text =
                                $"Selected bin: d_e={deVal:F3} Å, d_i={diVal:F3} Å\n" +
                                $"Vertices: {count}";
                        }
                    }

                    _isLmbDownOnPlot = false;
                }

                _isDraggingLabel = false;
                _dragTarget = DragTarget.None;
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (_isZoomRectActive)
                {
                    _zoomRectEnd = e.Location;
                    ApplyZoomRect();
                    _isZoomRectActive = false;
                }
            }
        }

        private void ApplyZoomRect()
        {
            int x1 = Math.Min(_zoomRectStart.X, _zoomRectEnd.X);
            int y1 = Math.Min(_zoomRectStart.Y, _zoomRectEnd.Y);
            int x2 = Math.Max(_zoomRectStart.X, _zoomRectEnd.X);
            int y2 = Math.Max(_zoomRectStart.Y, _zoomRectEnd.Y);

            int w = x2 - x1;
            int h = y2 - y1;

            const int MIN_RECT = 15;
            if (w < MIN_RECT || h < MIN_RECT) return;

            float span = _rangeMax - _rangeMin;
            if (span <= 0 || _lastPlotSize <= 0) return;

            float tx1 = (x1 - _lastPlotX) / (float)_lastPlotSize;
            float tx2 = (x2 - _lastPlotX) / (float)_lastPlotSize;
            float ty1 = (_lastPlotY + _lastPlotSize - y1) / (float)_lastPlotSize;
            float ty2 = (_lastPlotY + _lastPlotSize - y2) / (float)_lastPlotSize;

            float newMin = _rangeMin + span * Math.Max(tx1, Math.Min(ty1, ty2));
            float newMax = _rangeMin + span * Math.Min(tx2, Math.Max(ty1, ty2));

            newMin = Math.Max(newMin, 0f);
            newMax = Math.Min(newMax, _origRangeMax);

            if (newMax - newMin < 0.05f) return;

            float newSpan = newMax - newMin;
            int newBins = (int)MathF.Ceiling(newSpan / _binSize);
            if (newBins < 10) newBins = 10;
            if (newBins > MAX_TOTAL_CELLS) newBins = MAX_TOTAL_CELLS;

            _rangeMin = newMin;
            _rangeMax = newMax;

            float adjustedBin = newSpan / newBins;
            if (adjustedBin < 0.001f) adjustedBin = 0.001f;
            if (adjustedBin > 0.05f) adjustedBin = 0.05f;

            _binSize = adjustedBin;

            _numRangeMin.Value = Math.Clamp(
                (decimal)_rangeMin,
                _numRangeMin.Minimum,
                _numRangeMin.Maximum);

            _numBinSize.Value = Math.Clamp(
                (decimal)_binSize,
                _numBinSize.Minimum,
                _numBinSize.Maximum);

            RebuildHeatmapFromRaw();

            _lblStatus.Text =
                $"Zoom: range = [{_rangeMin:F2}–{_rangeMax:F2}] Å, " +
                $"bin = {_binSize:F4} Å";
        }

        private void Panel_MouseWheel(object sender, MouseEventArgs e)
        {
            float delta = e.Delta / 120f;

            float span = _rangeMax - _rangeMin;
            float zoomStep = delta > 0 ? 0.85f : 1.18f;

            float center = (_rangeMin + _rangeMax) * 0.5f;
            float newSpan = span * zoomStep;

            newSpan = Math.Clamp(newSpan, 0.1f, _origRangeMax - _origRangeMin);

            _rangeMin = Math.Max(0f, center - newSpan * 0.5f);
            _rangeMax = _rangeMin + newSpan;

            if (_rangeMax > _origRangeMax)
            {
                _rangeMax = _origRangeMax;
                _rangeMin = _rangeMax - newSpan;
                if (_rangeMin < 0f) _rangeMin = 0f;
            }

            _numRangeMin.Value = Math.Clamp(
                (decimal)_rangeMin,
                _numRangeMin.Minimum,
                _numRangeMin.Maximum);

            RebuildHeatmapFromRaw();

            if (_lblStatus != null)
                _lblStatus.Text = $"Zoom: range = [{_rangeMin:F2}–{_rangeMax:F2}] Å";
        }

        #endregion

        #region Save PNG

        private void SavePng()
        {
            if (!_deviceReady || _device == null || _heatmapTexture == null)
            {
                MessageBox.Show("No heatmap to save.", "Fingerprint",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new SaveFileDialog
            {
                Title = "Save Fingerprint as PNG",
                Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*",
                FileName = "hirshfeld_fingerprint.png",
                DefaultExt = "png"
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                int renderSize = 2048;

                _renderTarget?.Dispose();
                _renderTarget = new RenderTarget2D(_device, renderSize, renderSize,
                    false, SurfaceFormat.Color, DepthFormat.None);

                _device.SetRenderTarget(_renderTarget);
                _device.Clear(Flags.Instance.color1);

                ComputePlotLayout(renderSize, renderSize,
                    out int plotX, out int plotY, out int plotSize);

                _spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.PointClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone);

                _spriteBatch.Draw(_heatmapTexture,
                    new Rectangle(plotX, plotY, plotSize, plotSize), Color.White);

                if (_showMajorGrid) DrawMajorGrid(plotX, plotY, plotSize);
                if (_showMinorGrid) DrawMinorGrid(plotX, plotY, plotSize);
                if (_showDiagonal) DrawDiagonal(plotX, plotY, plotSize);

                DrawHighlightedBin(plotX, plotY, plotSize);

                if (_showAxes)
                {
                    if (_spriteFont != null)
                        DrawAxesFull(plotX, plotY, plotSize);
                    else
                        DrawAxesWithoutText(plotX, plotY, plotSize);
                }

                if (_clickMarkerPixel.HasValue)
                {
                    float k = renderSize / (float)Math.Max(_panel.Width, 1);
                    var scaled = new System.Drawing.Point(
                        (int)(_clickMarkerPixel.Value.X * k),
                        (int)(_clickMarkerPixel.Value.Y * k));
                    DrawClickMarker(scaled);
                }

                _spriteBatch.End();

                _device.SetRenderTarget(null);

                using var fs = new FileStream(dlg.FileName, FileMode.Create);
                _renderTarget.SaveAsPng(fs, renderSize, renderSize);

                _lblStatus.Text = $"Saved: {Path.GetFileName(dlg.FileName)}";
                LogSuccess($"[Fingerprint] Saved PNG: {dlg.FileName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save failed:\n{ex.Message}", "Fingerprint",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Debug helpers

        private static void LogInfo(string msg) => ProgressReporter.Log(msg, GdiColor.White);
        private static void LogWarn(string msg) => ProgressReporter.Log(msg, GdiColor.Orange);
        private static void LogError(string msg) => ProgressReporter.Log(msg, GdiColor.Red);
        private static void LogSuccess(string msg) => ProgressReporter.Log(msg, GdiColor.LightGreen);

        private static GdiColor XnaToGdi(Color c) => GdiColor.FromArgb(c.A, c.R, c.G, c.B);

        #endregion

        #region Cleanup

        private void CleanupAll()
        {
            _disposed = true;

            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;

            try { _heatmapTexture?.Dispose(); } catch { }
            _heatmapTexture = null;

            try { _renderTarget?.Dispose(); } catch { }
            _renderTarget = null;

            try { _whitePixel?.Dispose(); } catch { }
            _whitePixel = null;

            try { _circleTexture?.Dispose(); } catch { }
            _circleTexture = null;

            try { _spriteBatch?.Dispose(); } catch { }
            _spriteBatch = null;

            try { _content?.Dispose(); } catch { }
            _content = null;

            _spriteFont = null;

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