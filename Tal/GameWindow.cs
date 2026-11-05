// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MonoGame 3D viewport host — rendering, input, symmetry renderer integration.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Tal.Cavity;
using Tal.Core.PowderDiffraction;
using Tal.DrawMethods;
using Tal.Graphics;
using System.Windows.Forms;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Tal
{
 /// <summary>
 /// Manages a game window with 3D rendering capabilities using MonoGame.
 /// </summary>
    public class GameWindow : IDisposable
    {
        #region Private Fields

        private Measure? _measure;
        private readonly Panel _panel;
        private GraphicsDevice? _graphicsDevice;
        private BasicEffect? _basicEffect;
        private ContentManager? _content;
        private SpriteBatch? _spriteBatch;
        private SpriteFont? _spriteFont;
        private SpriteFont? _arialFont;
        private SpriteFont? _timesFont;

        private bool _isInitialized = false;
        private bool _isDisposed = false;

        private volatile bool _disposeInProgress = false;
        private readonly object _renderLock = new object();
        private volatile bool _renderingSuspended = false;

        private System.Windows.Forms.Timer? _renderTimer;

        private Camera? _camera;
        private Draw? _draw;
        private Molecule? _molecule;

        private Model? _smallBall;
        private Model? _bigBall;
        private Model? _smallRod;
        private Model? _ballLow;
        private Model? _ballMed;
        private Model? _ballHigh;
        private Model? _rodLow;
        private Model? _rodMed;
        private Model? _rodHigh;
        private Model? _cone;

        private Effect? _sphereImpostorEffect;
        private Effect? _cavitySurfaceEffect;

        private CavityRenderer? _cavityRenderer;

        private Texture2D? _debugPixelTexture;

        // Z-clip bar interaction (shared geometry with DrawZClipBar)
        private enum ZClipDragMode { None, Near, Far }
        private ZClipDragMode _zClipDragMode = ZClipDragMode.None;
        private Microsoft.Xna.Framework.Rectangle _zClipBarRect;
        private float _zClipRangeMin = 0.05f;
        private float _zClipRangeMax = 80f;
        private const int ZClipHandleHitPx = 14;

        // Right-button: drag = pan structure, click (no drag) = context menu
        private bool _rightButtonDown = false;
        private bool _rightDidPan = false;
        private System.Drawing.Point _rightDownPos;
        private const int RightClickDragThresholdPx = 4;

        private List<Molecule.Atom> _overlayAtoms = new List<Molecule.Atom>();

 // #1 Cache MainForm — instead of LINQ on every mouse event
        private MainForm? _mainForm;

 // ⭐ #2 Dirty flags
        private bool _moleculeDirty = false;
        private bool _measureDirty = false;
        private bool _planesDirty = false;

 // ⭐ #6 Cache Viewafterrt
        private Viewport _viewportSceneRT;
        private Viewport _viewportBackbuffer;
        private int _viewportSceneRTW = 0;
        private int _viewportSceneRTH = 0;
        private int _viewportBackbufferW = 0;
        private int _viewportBackbufferH = 0;

 // Main 3D scene render target (SSAA).
        private RenderTarget2D? _currentTarget;

        private int _currentMSAA = 0;
        private int _currentRequestedMSAA = -1;
        private int _currentRTWidth = 0;
        private int _currentRTHeight = 0;

 // ⭐ Tymczasowy RT for SSAA 4×.
        private RenderTarget2D? _tempDownsampleRT;
        private int _tempRTCachedW = 0;
        private int _tempRTCachedH = 0;

 // ⭐⭐ NEW: Symmetry elements renderer — builds list elements symmetry
 // i deleguje drawing z afterwrotem do Draw.
        private CellSymmetryRenderer? _symmetryRenderer;

        #endregion

        #region Public Properties

        public string Title { get; set; } = "Struktura 3D";
        public bool IsDisposed => _isDisposed;

        public bool RenderingSuspended
        {
            get => _renderingSuspended;
            set
            {
                if (_renderingSuspended == value) return;
                _renderingSuspended = value;

                if (value)
                {
                    try { _renderTimer?.Stop(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[GameWindow] Suspend: timer.Stop error: {ex.Message}");
                    }
                }
                else
                {
                    try { _renderTimer?.Start(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[GameWindow] Resume: timer.Start error: {ex.Message}");
                    }
                }
            }
        }

 /// <summary>
 /// ⭐ Returns renderer elements symmetry tego okna (may be null,
 /// if GameWindow has not been graphics-initialized yet).
 ///
 /// Used by MonoGameDocument.GetSymmetryElementsRenderer() i dalej
 /// przez MainForm.InvalidateSymmetryRenderers() do wymuszenia przebudowy
 /// cache symmetry elements after changes to colour / size / visibility
 /// w okienku Symmetry Elements.
 /// </summary>
        public CellSymmetryRenderer? GetSymmetryElementsRenderer()
        {
            return _symmetryRenderer;
        }

        public int GetPanelHash() => _panel?.GetHashCode() ?? 0;

        #endregion

        #region Constructor

        public GameWindow(Panel panel)
        {
            _panel = panel;
            RegisterEvents();

            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 16; // ~60 FPS
            _renderTimer.Tick += (s, e) => RenderFrame();
            _renderTimer.Start();

            System.Diagnostics.Debug.WriteLine(
                $"[GameWindow.CTOR] Panel.Hash={_panel.GetHashCode()}, Handle={_panel.Handle}");
        }

        #endregion

        #region Public Methods

        private MainForm? GetMainForm()
        {
            if (_mainForm == null || _mainForm.IsDisposed)
                _mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            return _mainForm;
        }

        public void SetMeasure(Measure? measure)
        {
            _measure = measure;
            _measureDirty = true;
            _draw?.SetMeasure(measure);
        }

        public void NotifyMoleculeChanged()
        {
            _moleculeDirty = true;
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public void NotifyMeasureChanged()
        {
            _measureDirty = true;
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public void NotifyPlanesChanged()
        {
            _planesDirty = true;
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public void SetPolyhedrals(List<Polihedrals.VertexStruct> list)
        {
            _draw?.SetPolyhedrals(list);
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public void SetCrystalPlanes(List<CrystalPlane> planes)
        {
            _draw?.SetCrystalPlanes(planes);
            _planesDirty = false;
            _moleculeDirty = true;
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public void SetOverlayAtoms(List<Molecule.Atom> atoms)
        {
            _overlayAtoms = atoms ?? new List<Molecule.Atom>();
            if (_draw != null) _draw.SetOverlayAtoms(_overlayAtoms);
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public void ClearOverlayAtoms()
        {
            _overlayAtoms.Clear();
            _draw?.SetOverlayAtoms(_overlayAtoms);
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public bool IsValid()
        {
            return !_isDisposed && _isInitialized &&
                   _graphicsDevice != null && !_graphicsDevice.IsDisposed &&
                   _panel != null && _panel.IsHandleCreated && _panel.Visible;
        }

        public GraphicsDevice? GetGraphicsDevice() => _graphicsDevice;
        public SpriteFont? GetSpriteFont() => _spriteFont;
        public SpriteFont? GetArialFont() => _arialFont;
        public SpriteFont? GetTimesFont() => _timesFont;

        public void SetCavityRenderer(CavityRenderer renderer)
        {
            _cavityRenderer = renderer;
            if (_draw != null)
                _draw.SetCavityRenderer(renderer);
            if (renderer != null && _cavitySurfaceEffect != null)
                renderer.SetCavitySurfaceEffect(_cavitySurfaceEffect);
        }

        public void UpdateCavityData(List<Cavity.Cavity> cavities)
        {
            if (_draw != null)
            {
                _draw.UpdateCavityData(cavities);
                RefreshView();
            }
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public void SetCavityDrawMode(CavityRenderer.DrawMode mode)
        {
            if (_draw != null)
            {
                _draw.SetCavityDrawMode(mode);
                RefreshView();
            }
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public void ShowCavities(bool show)
        {
            if (_draw != null)
            {
                _draw.ShowCavities(show);
                RefreshView();
            }
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public Camera? GetCamera() => _camera;
        public Measure? GetMeasure() => _measure;

        public void RefreshView()
        {
            if (_isDisposed || _disposeInProgress) return;
            if (!_isInitialized) return;
            if (_renderingSuspended) return;
            RenderFrame();
        }

        public void ResetDrawCache()
        {
            if (_draw != null)
                _draw.ResetCache();
            _moleculeDirty = true;
            _planesDirty = true;
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public void SetRevealClickPosition(Vector3 position, bool show)
        {
            if (_draw != null)
            {
                _draw.RevealClickPosition = position;
                _draw.ShowRevealClickPosition = show;
            }
        }

 /// <summary>
 /// Loads a new molecule into the scene.
 ///
 /// NEW: after setting molecule in Draw, also calls
 /// _symmetryRenderer.BuildFromMolecule(molecule) — so that renderer
 /// symmetry elements to rebuild their list.
 /// </summary>
        public void LoadMolecule(Molecule? molecule)
        {
            _molecule = molecule;
            _moleculeDirty = true;
            _planesDirty = true;

            if (_draw != null)
            {
                _draw.SetMolecule(molecule);
                _draw.SetCamera(_camera);

                if (_molecule != null)
                    _draw.SetCrystalPlanes(_molecule.Planes ?? new List<CrystalPlane>());

                if (_measure != null)
                    _draw.SetMeasure(_measure);

                if (_molecule != null && _molecule.NumberOfAtoms > 0)
                {
                    if (_camera != null)
                    {
                        _camera.g_cameraPos = new Vector3(0, 0, Camera.CAMERA_DISTANCE);
                        _camera.g_targetPos = Vector3.Zero;
                    }
                }
            }

            _moleculeDirty = false;
            _planesDirty = false;

 // ⭐⭐ NEW: rebuild list elements symmetry
            if (_symmetryRenderer != null && molecule != null)
            {
                _symmetryRenderer.BuildFromMolecule(molecule);
                System.Diagnostics.Debug.WriteLine(
                    $"[GameWindow.LoadMolecule] SymmetryRenderer rebuilt: " +
                    $"ops={molecule.SymmetryOperations?.Count ?? 0}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[GameWindow.LoadMolecule] ⚠️ SymmetryRenderer is NULL " +
                    $"or molecule is NULL — cannot build symmetry elements!");
            }

            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public void SetStyle(int styleIndex)
        {
            if (_molecule != null)
            {
                for (int i = 0; i < _molecule.NumberOfAtoms; i++)
                    _molecule.atom[i].style = styleIndex;
                Flags.Instance.Presentation = styleIndex;
                NotifyMoleculeChanged();
            }
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        public void CleanupBeforeClose()
        {
            try
            {
                _disposeInProgress = true;
                lock (_renderLock)
                {
                    if (_renderTimer != null)
                    {
                        try { _renderTimer.Stop(); }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[GameWindow.CleanupBeforeClose] error: {ex.Message}");
            }
        }

        public int FindAtomForContextMenu(System.Drawing.Point mousePosition,
                                           int viewportWidth,
                                           int viewportHeight)
        {
            return FindNearestAtomAtPosition(mousePosition, viewportWidth, viewportHeight, 20);
        }

        public void DrawBackgroundGradient(GraphicsDevice device, int rtW, int rtH)
        {
            if (device == null || device.IsDisposed) return;
            if (_spriteBatch == null || _spriteBatch.IsDisposed) return;
            if (_camera == null) return;
            if (_basicEffect == null) return;

            var savedBlend = device.BlendState;
            var savedDepth = device.DepthStencilState;
            var savedRaster = device.RasterizerState;

            device.BlendState = BlendState.Opaque;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;

            _camera.GraphicsDevice = device;
            _camera.ApplyEffects();

            _spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.Opaque);
            GraphiscHelper.FiledRectangle(
                new Vector2(0, 0),
                rtW,
                rtH,
                0,
                Flags.Instance.color1,
                Flags.Instance.color2,
                _camera.Projection2,
                device,
                _basicEffect);
            _spriteBatch.End();

            device.BlendState = savedBlend;
            device.DepthStencilState = savedDepth;
            device.RasterizerState = savedRaster;
        }

        #endregion

        #region Event Registration

        private void RegisterEvents()
        {
            _panel.HandleCreated += OnHandleCreated;
            _panel.Resize += OnResize;
            _panel.MouseDown += OnMouseDown;
            _panel.MouseUp += OnMouseUp;
            _panel.MouseMove += OnMouseMove;
            _panel.MouseWheel += OnMouseWheel;
            _panel.MouseClick += OnMouseClick;
            _panel.MouseDoubleClick += OnMouseDoubleClick;
            _panel.TabStop = true;
            _panel.KeyDown += OnKeyDown;
            _panel.KeyUp += OnKeyUp;
        }

        #endregion

        #region Graphics Initialization

        private void OnHandleCreated(object? sender, EventArgs e)
        {
            InitializeGraphics();
        }

        private string ResolveContentRoot()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? baseDir;

            string[] candidates =
            {
                Path.Combine(exeDir, "Content"),
                Path.Combine(baseDir, "Content"),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\Content")),
            };

            foreach (var c in candidates)
            {
                try { if (Directory.Exists(c)) return c; }
                catch { }
            }

            return exeDir;
        }

        private T? LoadContent<T>(string assetName) where T : class
        {
            if (_content == null) return null;

            try
            {
                return _content.Load<T>(assetName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[GameWindow] LoadContent<{typeof(T).Name}>('{assetName}') FAILED: {ex.Message}");
                return null;
            }
        }

        private Effect? LoadEffectWithVariants(string[] variants)
        {
            if (_content == null) return null;

            foreach (var variant in variants)
            {
                try
                {
                    var fx = _content.Load<Effect>(variant);
                    if (fx != null) return fx;
                }
                catch { }
            }

            return null;
        }

        private void InitializeGraphics()
        {
            System.Diagnostics.Debug.WriteLine(
                $"[GameWindow.INIT] Panel.Hash={_panel.GetHashCode()}, Title={Title}, START");

            try
            {
                var pp = new PresentationParameters
                {
                    IsFullScreen = false,
                    BackBufferWidth = Math.Max(_panel.Width, 1),
                    BackBufferHeight = Math.Max(_panel.Height, 1),
                    BackBufferFormat = SurfaceFormat.Color,
                    DepthStencilFormat = DepthFormat.Depth24Stencil8,
                    DeviceWindowHandle = _panel.Handle,
                    PresentationInterval = PresentInterval.Immediate,
                    MultiSampleCount = 0
                };

                _graphicsDevice = new GraphicsDevice(
                    GraphicsAdapter.DefaultAdapter,
                    GraphicsProfile.HiDef,
                    pp
                );

                _spriteBatch = new SpriteBatch(_graphicsDevice);
                _basicEffect = new BasicEffect(_graphicsDevice);
                _basicEffect.VertexColorEnabled = true;
                _basicEffect.LightingEnabled = true;
                _basicEffect.EnableDefaultLighting();

                string contentRoot = ResolveContentRoot();

                var services = new ServiceContainer();
                services.AddService<IGraphicsDeviceService>(
                    new SimpleGraphicsDeviceService(_graphicsDevice));
                _content = new ContentManager(services, contentRoot);

 // ⭐ Spheres — 3 quality tiers (LOD by camera distance in Draw)
                try
                {
                    _ballLow = RuntimeIcosphere.Create(
                        _graphicsDevice, subdivisionLevel: 1, radius: RuntimeIcosphere.BASE_RADIUS);
                    _ballMed = RuntimeIcosphere.Create(
                        _graphicsDevice, subdivisionLevel: 3, radius: RuntimeIcosphere.BASE_RADIUS);
                    _ballHigh = RuntimeIcosphere.Create(
                        _graphicsDevice, subdivisionLevel: 5, radius: RuntimeIcosphere.BASE_RADIUS);
                    // Legacy aliases used elsewhere
                    _smallBall = _ballMed;
                    _bigBall = _ballHigh;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameWindow] balls FAILED: {ex.Message}");
                }

 // ⭐ Cylinders — 3 quality tiers
                try
                {
                    _rodLow = RuntimeCylinder.Create(_graphicsDevice, segments: 8, capped: true);
                    _rodMed = RuntimeCylinder.Create(_graphicsDevice, segments: 12, capped: true);
                    _rodHigh = RuntimeCylinder.Create(_graphicsDevice, segments: 24, capped: true);
                    _smallRod = _rodHigh;
                    _cone = RuntimeCone.Create(_graphicsDevice, segments: 24);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GameWindow] rods FAILED: {ex.Message}");
                }

                _spriteFont = LoadContent<SpriteFont>(@"SpriteFont");
                _arialFont = LoadContent<SpriteFont>(@"SpriteFontArial");
                _timesFont = LoadContent<SpriteFont>(@"SpriteFontTimesNewRoman");

                _camera = new Camera();
                if (_graphicsDevice != null)
                {
                    _camera.GraphicsDevice = _graphicsDevice;
                    _camera.Initialize();
                    _camera.ApplyEffects();
                }

                ScreenHelper.Initialize(_panel.Width, _panel.Height);
                ScreenHelper.SetSafeInsets(0, 0, 0, 0);

                _draw = new Draw(_graphicsDevice, _spriteBatch, _spriteFont, _basicEffect);
                _draw.SpriteFont = _spriteFont;
                _draw.ArialFont = _arialFont;
                _draw.TimesFont = _timesFont;

                _draw.Big_ball = _bigBall;
                _draw.Small_ball = _smallBall;
                _draw.Small_rod = _smallRod;
                _draw.BallLow = _ballLow ?? _smallBall;
                _draw.BallMed = _ballMed ?? _smallBall;
                _draw.BallHigh = _ballHigh ?? _bigBall;
                _draw.RodLow = _rodLow ?? _smallRod;
                _draw.RodMed = _rodMed ?? _smallRod;
                _draw.RodHigh = _rodHigh ?? _smallRod;
                _draw.Cone = _cone;
                _draw.SetCamera(_camera);

 // NEW: create SymmetryElementsRenderer and attach to Draw
                try
                {
                    _symmetryRenderer = new CellSymmetryRenderer();
                    _draw.SetSymmetryElementsRenderer(_symmetryRenderer);

                    System.Diagnostics.Debug.WriteLine(
                        "[GameWindow.INIT] ✅ SymmetryElementsRenderer created and attached");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[GameWindow.INIT] ❌ SymmetryElementsRenderer FAILED: {ex.Message}");
                    _symmetryRenderer = null;
                }

                try
                {
                    _sphereImpostorEffect = LoadEffectWithVariants(new[]
                    {
                        "effects/SphereImpostor",
                        "Effects/SphereImpostor",
                        "SphereImpostor",
                    });

                    if (_sphereImpostorEffect != null)
                        _draw.LoadSphereImpostor(_sphereImpostorEffect);
                }
                catch { }

                try
                {
                    _cavitySurfaceEffect = LoadEffectWithVariants(new[]
                    {
                        "effects/CavitySurface",
                        "Effects/CavitySurface",
                        "CavitySurface",
                    });

                    if (_cavitySurfaceEffect != null && _cavityRenderer != null)
                        _cavityRenderer.SetCavitySurfaceEffect(_cavitySurfaceEffect);
                }
                catch { }

                if (_molecule != null)
                {
                    _draw.SetCrystalPlanes(_molecule.Planes ?? new List<CrystalPlane>());
                    _draw.SetMolecule(_molecule);
                    _planesDirty = false;
                    _moleculeDirty = false;

 // NEW: if molecule already existed, rebuild symmetry elements
                    _symmetryRenderer?.BuildFromMolecule(_molecule);
                }

                if (_measure != null)
                {
                    _draw.SetMeasure(_measure);
                    _measureDirty = false;
                }

                _isInitialized = true;

                System.Diagnostics.Debug.WriteLine(
                    $"[GameWindow.INIT] Panel.Hash={_panel.GetHashCode()}, Title={Title}, DONE");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Graphics initialization error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"STACK: {ex.StackTrace}");
            }
        }

        #endregion

 /// <summary>
 /// ⭐ Publiczny wrapper na Draw.DrawSpriteLayer() — used przez screenshot
 /// do drawing etykiet + color scale na docelowym RT.
 /// </summary>
        public void DrawSpriteLayer()
        {
            if (_draw == null) return;
            _draw.DrawSpriteLayer();
        }

        #region Helpers — MSAA / Viewport

        private static int GetSceneMSAA()
        {
            int msaa = Flags.Instance.MSAAOnSceneRT;
            if (msaa != 0 && msaa != 2 && msaa != 4 && msaa != 8)
                msaa = 4;
            return msaa;
        }

        private static int NormalizeSSAA(int value)
        {
            if (value <= 1) return 1;
            if (value <= 2) return 2;
            return 4;
        }

        private Viewport GetSceneRTViewport(int rtW, int rtH)
        {
            if (_viewportSceneRTW != rtW || _viewportSceneRTH != rtH)
            {
                _viewportSceneRT = new Viewport(0, 0, rtW, rtH);
                _viewportSceneRTW = rtW;
                _viewportSceneRTH = rtH;
            }
            return _viewportSceneRT;
        }

        private Viewport GetBackbufferViewport(int w, int h)
        {
            if (_viewportBackbufferW != w || _viewportBackbufferH != h)
            {
                _viewportBackbuffer = new Viewport(0, 0, w, h);
                _viewportBackbufferW = w;
                _viewportBackbufferH = h;
            }
            return _viewportBackbuffer;
        }

        #endregion

        #region Rendering

        private void RenderFrame()
        {
            if (_isDisposed || _disposeInProgress) return;
            if (_renderingSuspended) return;

            lock (_renderLock)
            {
                if (_isDisposed || _disposeInProgress) return;
                if (_renderingSuspended) return;

                if (!_isInitialized) return;
                if (_graphicsDevice == null || _graphicsDevice.IsDisposed) return;
                if (_spriteBatch == null || _spriteBatch.IsDisposed) return;
                if (_draw == null) return;
                if (_basicEffect == null || _basicEffect.IsDisposed) return;

                if (_panel == null || !_panel.IsHandleCreated || !_panel.Visible)
                    return;

                try
                {
 // Backbuffer resize check
                    if (_panel.Width > 0 && _panel.Height > 0)
                    {
                        var pp = _graphicsDevice.PresentationParameters;
                        if (pp.BackBufferWidth != _panel.Width || pp.BackBufferHeight != _panel.Height)
                        {
                            pp.BackBufferWidth = Math.Max(_panel.Width, 1);
                            pp.BackBufferHeight = Math.Max(_panel.Height, 1);
                            _graphicsDevice.Reset(pp);
                            ScreenHelper.Initialize(_panel.Width, _panel.Height);

                            if (_camera != null)
                                _camera.GraphicsDevice = _graphicsDevice;

                            _currentTarget?.Dispose();
                            _currentTarget = null;
                            _currentRTWidth = 0;
                            _currentRTHeight = 0;
                            _currentRequestedMSAA = -1;

                            _tempDownsampleRT?.Dispose();
                            _tempDownsampleRT = null;
                            _tempRTCachedW = 0;
                            _tempRTCachedH = 0;
                        }
                    }

                    int w = _graphicsDevice.PresentationParameters.BackBufferWidth;
                    int h = _graphicsDevice.PresentationParameters.BackBufferHeight;

                    int ssaaFactor = NormalizeSSAA(Flags.Instance.SSAASamples);

                    int rtW = Math.Max(1, w * ssaaFactor);
                    int rtH = Math.Max(1, h * ssaaFactor);

                    int requestedMSAA = GetSceneMSAA();
                    if (ssaaFactor > 1)
                        requestedMSAA = 0;

                    bool needRealloc =
                        _currentTarget == null ||
                        _currentTarget.IsDisposed ||
                        _currentRTWidth != rtW ||
                        _currentRTHeight != rtH ||
                        _currentRequestedMSAA != requestedMSAA;

                    if (needRealloc)
                    {
                        _currentTarget?.Dispose();

                        SurfaceFormat fmt = requestedMSAA > 0
                            ? SurfaceFormat.Color
                            : SurfaceFormat.HalfVector4;

                        _currentTarget = new RenderTarget2D(
                            _graphicsDevice,
                            rtW, rtH,
                            false,
                            fmt,
                            DepthFormat.Depth24Stencil8,
                            requestedMSAA,
                            RenderTargetUsage.PreserveContents);

                        int actualMSAA = _currentTarget.MultiSampleCount;

                        _currentRTWidth = rtW;
                        _currentRTHeight = rtH;
                        _currentMSAA = actualMSAA;
                        _currentRequestedMSAA = requestedMSAA;

                        System.Diagnostics.Debug.WriteLine(
                            $"[RF] _currentTarget: {rtW}×{rtH}, fmt={fmt}, " +
                            $"MSAA={actualMSAA} (requested {requestedMSAA}), SSAA={ssaaFactor}×");
                    }

                    if (ssaaFactor == 4)
                    {
                        int midW = Math.Max(1, w * 2);
                        int midH = Math.Max(1, h * 2);

                        if (_tempDownsampleRT == null ||
                            _tempDownsampleRT.IsDisposed ||
                            _tempRTCachedW != midW ||
                            _tempRTCachedH != midH)
                        {
                            _tempDownsampleRT?.Dispose();
                            _tempDownsampleRT = new RenderTarget2D(
                                _graphicsDevice,
                                midW, midH,
                                false,
                                SurfaceFormat.Color,
                                DepthFormat.None,
                                0,
                                RenderTargetUsage.DiscardContents);

                            _tempRTCachedW = midW;
                            _tempRTCachedH = midH;
                        }
                    }
                    else if (_tempDownsampleRT != null)
                    {
                        _tempDownsampleRT.Dispose();
                        _tempDownsampleRT = null;
                        _tempRTCachedW = 0;
                        _tempRTCachedH = 0;
                    }

                    RenderTarget2D currentTarget = _currentTarget!;

                    if (_moleculeDirty)
                    {
                        if (_molecule != null)
                            _draw.SetMolecule(_molecule);
                        _moleculeDirty = false;
                    }

                    if (_planesDirty)
                    {
                        if (_molecule != null)
                            _draw.SetCrystalPlanes(_molecule.Planes ?? new List<CrystalPlane>());
                        _planesDirty = false;
                    }

                    if (_measureDirty)
                    {
                        _draw.SetMeasure(_measure);
                        _measureDirty = false;
                    }

 // ⭐ 1. RENDER SCENY 3D → _currentTarget
                    _graphicsDevice.SetRenderTarget(currentTarget);
                    _graphicsDevice.Viewport = GetSceneRTViewport(rtW, rtH);

                    if (_camera != null)
                    {
                        _camera.GraphicsDevice = _graphicsDevice;
                        _camera.ApplyEffects();
                    }

                    _graphicsDevice.Clear(
                        ClearOptions.Target | ClearOptions.DepthBuffer,
                        new XnaColor(0, 0, 0, 0),
                        1.0f, 0);

                    _graphicsDevice.BlendState = BlendState.Opaque;
                    _graphicsDevice.DepthStencilState = DepthStencilState.None;
                    _graphicsDevice.RasterizerState = RasterizerState.CullNone;

                    _spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.Opaque);
                    GraphiscHelper.FiledRectangle(
                        new Vector2(0, 0),
                        rtW,
                        rtH,
                        0,
                        Flags.Instance.color1,
                        Flags.Instance.color2,
                        _camera!.Projection2,
                        _graphicsDevice,
                        _basicEffect);
                    _spriteBatch.End();

                    _graphicsDevice.BlendState = BlendState.Opaque;
                    _graphicsDevice.DepthStencilState = DepthStencilState.Default;
                    _graphicsDevice.RasterizerState = RasterizerState.CullNone;

                    _draw.DrawScene();

 // ⭐ 2. DOWNSAMPLE → BACKBUFFER
                    _graphicsDevice.SetRenderTarget(null);
                    _graphicsDevice.Viewport = GetBackbufferViewport(w, h);

                    if (_camera != null)
                    {
                        _camera.GraphicsDevice = _graphicsDevice;
                        _camera.ApplyEffects();
                    }

                    if (currentTarget != null && !currentTarget.IsDisposed)
                    {
                        switch (ssaaFactor)
                        {
                            case 1:
                                DrawFullscreen(currentTarget, w, h, SamplerState.PointClamp);
                                break;

                            case 2:
                                DrawFullscreen(currentTarget, w, h, SamplerState.LinearClamp);
                                break;

                            case 4:
                                if (_tempDownsampleRT != null && !_tempDownsampleRT.IsDisposed)
                                {
                                    DrawToRenderTarget(currentTarget, _tempDownsampleRT,
                                        SamplerState.LinearClamp);

                                    _graphicsDevice.SetRenderTarget(null);
                                    _graphicsDevice.Viewport = GetBackbufferViewport(w, h);
                                    DrawFullscreen(_tempDownsampleRT, w, h,
                                        SamplerState.LinearClamp);
                                }
                                else
                                {
                                    DrawFullscreen(currentTarget, w, h,
                                        SamplerState.LinearClamp);
                                }
                                break;

                            default:
                                DrawFullscreen(currentTarget, w, h,
                                    SamplerState.LinearClamp);
                                break;
                        }
                    }

 // ⭐ 3. SPRITE LAYER
                    _graphicsDevice.BlendState = BlendState.AlphaBlend;
                    _graphicsDevice.DepthStencilState = DepthStencilState.None;
                    _graphicsDevice.RasterizerState = RasterizerState.CullNone;

                    _draw.DrawSpriteLayer();

 // ⭐ 4. UI (lasso, crosshair)
                    _graphicsDevice.BlendState = BlendState.AlphaBlend;
                    _graphicsDevice.DepthStencilState = DepthStencilState.None;

                    _spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                    DrawDebugCrosshair();
                    DrawLasso();
                    DrawZClipBar();
                    _spriteBatch.End();

                    _graphicsDevice.Present();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Render error Panel.Hash={_panel.GetHashCode()}: " +
                        $"{ex.GetType().Name}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");

                    try
                    {
                        _graphicsDevice?.SetRenderTarget(null);
                        if (_graphicsDevice != null)
                        {
                            int w = _graphicsDevice.PresentationParameters.BackBufferWidth;
                            int h = _graphicsDevice.PresentationParameters.BackBufferHeight;
                            _graphicsDevice.Viewport = GetBackbufferViewport(w, h);
                        }
                    }
                    catch { }
                }
            }
        }

        private void DrawFullscreen(Texture2D source, int dstW, int dstH, SamplerState sampler)
        {
            if (source == null || source.IsDisposed) return;

            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.None;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque,
                sampler, DepthStencilState.None, RasterizerState.CullNone);

            _spriteBatch.Draw(
                source,
                new Microsoft.Xna.Framework.Rectangle(0, 0, dstW, dstH),
                XnaColor.White);

            _spriteBatch.End();
        }

        private void DrawToRenderTarget(Texture2D source, RenderTarget2D target, SamplerState sampler)
        {
            if (source == null || source.IsDisposed) return;
            if (target == null || target.IsDisposed) return;

            _graphicsDevice.SetRenderTarget(target);
            _graphicsDevice.Viewport = new Viewport(0, 0, target.Width, target.Height);

            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.None;
            _graphicsDevice.RasterizerState = RasterizerState.CullNone;

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque,
                sampler, DepthStencilState.None, RasterizerState.CullNone);

            _spriteBatch.Draw(
                source,
                new Microsoft.Xna.Framework.Rectangle(0, 0, target.Width, target.Height),
                XnaColor.White);

            _spriteBatch.End();
        }

        private void DrawDebugCrosshair()
        {
            if (Measure.ShowDebugCross && Measure.LastClickPosition.X >= 0 && Measure.LastClickPosition.Y >= 0)
            {
                int x = Measure.LastClickPosition.X;
                int y = Measure.LastClickPosition.Y;
                int size = 20;

                if (_debugPixelTexture == null)
                {
                    _debugPixelTexture = new Texture2D(_graphicsDevice, 1, 1);
                    _debugPixelTexture.SetData(new XnaColor[] { XnaColor.Red });
                }

                _spriteBatch!.Draw(_debugPixelTexture, new Microsoft.Xna.Framework.Rectangle(x - size, y - 1, size * 2, 2), XnaColor.Red);
                _spriteBatch.Draw(_debugPixelTexture, new Microsoft.Xna.Framework.Rectangle(x - 1, y - size, 2, size * 2), XnaColor.Red);
                _spriteBatch.Draw(_debugPixelTexture, new Microsoft.Xna.Framework.Rectangle(x - 3, y - 3, 6, 6), XnaColor.Red);
            }
        }


        /// <summary>
        /// Bottom HUD for Z-clipping: thick bar with molecule extent and
        /// draggable near/far scissors. Geometry is stored for mouse hit-tests.
        /// </summary>
        private void DrawZClipBar()
        {
            if (!Flags.Instance.ZClipping) return;
            if (_camera == null || _graphicsDevice == null) return;
            if (_spriteBatch == null) return;

            if (_debugPixelTexture == null)
            {
                _debugPixelTexture = new Texture2D(_graphicsDevice, 1, 1);
                _debugPixelTexture.SetData(new XnaColor[] { XnaColor.White });
            }

            int vpW = _graphicsDevice.Viewport.Width;
            int vpH = _graphicsDevice.Viewport.Height;
            if (vpW < 80 || vpH < 50) return;

            // 2× thicker than the first version
            int margin = 12;
            int barH = 36;
            int barY = vpH - barH - 8;
            int barX = margin;
            int barW = vpW - margin * 2;
            _zClipBarRect = new Microsoft.Xna.Framework.Rectangle(barX, barY, barW, barH);

            _zClipRangeMin = 0.05f;
            _zClipRangeMax = Math.Max(_camera.FarPlaneDistance + 5f, 40f);
            _zClipRangeMax = Math.Min(_zClipRangeMax, 120f);
            float span = Math.Max(_zClipRangeMax - _zClipRangeMin, 1f);

            float MapX(float z) => barX + (z - _zClipRangeMin) / span * barW;

            // Track background
            _spriteBatch.Draw(_debugPixelTexture, _zClipBarRect, new XnaColor(28, 28, 38, 220));

            // Molecule Z extent
            float molNear = float.MaxValue, molFar = float.MinValue;
            if (_molecule != null && _molecule.NumberOfAtoms > 0)
            {
                Matrix rotView = _camera.Rotate * _camera.View;
                for (int i = 0; i < _molecule.NumberOfAtoms; i++)
                {
                    if (!_molecule.atom[i].show) continue;
                    Vector3 v = Vector3.Transform(_molecule.atom[i].Coordinate, rotView);
                    float depth = -v.Z;
                    if (depth < molNear) molNear = depth;
                    if (depth > molFar) molFar = depth;
                }
            }

            if (molNear < float.MaxValue && molFar > float.MinValue)
            {
                molNear = Math.Clamp(molNear, _zClipRangeMin, _zClipRangeMax);
                molFar = Math.Clamp(molFar, _zClipRangeMin, _zClipRangeMax);
                int x0 = (int)MapX(molNear);
                int x1 = (int)MapX(molFar);
                if (x1 < x0) (x0, x1) = (x1, x0);
                int w = Math.Max(x1 - x0, 4);
                var molRect = new Microsoft.Xna.Framework.Rectangle(
                    x0, barY + 6, w, barH - 12);
                _spriteBatch.Draw(_debugPixelTexture, molRect, new XnaColor(70, 150, 255, 170));
            }

            // Visible window (near…far)
            {
                float n = Math.Clamp(_camera.NearPlaneDistance, _zClipRangeMin, _zClipRangeMax);
                float f = Math.Clamp(_camera.FarPlaneDistance, _zClipRangeMin, _zClipRangeMax);
                int x0 = (int)MapX(n);
                int x1 = (int)MapX(f);
                if (x1 < x0) (x0, x1) = (x1, x0);
                var vis = new Microsoft.Xna.Framework.Rectangle(
                    x0, barY, Math.Max(x1 - x0, 3), barH);
                _spriteBatch.Draw(_debugPixelTexture, vis, new XnaColor(40, 180, 90, 80));
            }

            // Handles (wider for easy grab)
            int nearX = (int)MapX(Math.Clamp(_camera.NearPlaneDistance, _zClipRangeMin, _zClipRangeMax));
            int farX = (int)MapX(Math.Clamp(_camera.FarPlaneDistance, _zClipRangeMin, _zClipRangeMax));
            int handleW = 8;
            var nearHandle = new Microsoft.Xna.Framework.Rectangle(
                nearX - handleW / 2, barY - 6, handleW, barH + 12);
            var farHandle = new Microsoft.Xna.Framework.Rectangle(
                farX - handleW / 2, barY - 6, handleW, barH + 12);

            // Highlight active handle
            var nearCol = _zClipDragMode == ZClipDragMode.Near
                ? new XnaColor(120, 255, 255, 255) : new XnaColor(0, 220, 255, 255);
            var farCol = _zClipDragMode == ZClipDragMode.Far
                ? new XnaColor(255, 220, 100, 255) : new XnaColor(255, 160, 40, 255);
            _spriteBatch.Draw(_debugPixelTexture, nearHandle, nearCol);
            _spriteBatch.Draw(_debugPixelTexture, farHandle, farCol);

            // Border
            void HLine(int x, int y, int w, XnaColor c) =>
                _spriteBatch.Draw(_debugPixelTexture, new Microsoft.Xna.Framework.Rectangle(x, y, w, 1), c);
            void VLine(int x, int y, int h, XnaColor c) =>
                _spriteBatch.Draw(_debugPixelTexture, new Microsoft.Xna.Framework.Rectangle(x, y, 1, h), c);
            var border = new XnaColor(200, 200, 210, 230);
            HLine(barX, barY, barW, border);
            HLine(barX, barY + barH - 1, barW, border);
            VLine(barX, barY, barH, border);
            VLine(barX + barW - 1, barY, barH, border);

            if (_spriteFont != null)
            {
                string nearLbl = $"near {_camera.NearPlaneDistance:F2}";
                string farLbl = $"far {_camera.FarPlaneDistance:F2}";
                _spriteBatch.DrawString(_spriteFont, nearLbl,
                    new Vector2(barX + 4, barY - 18), XnaColor.Cyan * 0.95f,
                    0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
                var farSize = _spriteFont.MeasureString(farLbl) * 0.75f;
                _spriteBatch.DrawString(_spriteFont, farLbl,
                    new Vector2(barX + barW - farSize.X - 4, barY - 18),
                    new XnaColor(255, 180, 80), 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_spriteFont, "drag handles · Shift+drag scene: far",
                    new Vector2(barX + barW * 0.30f, barY - 18),
                    new XnaColor(180, 180, 190), 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
            }
        }

        /// <summary>Map bar X pixel → view-depth value.</summary>
        private float ZClipXToDepth(int x)
        {
            float span = Math.Max(_zClipRangeMax - _zClipRangeMin, 1f);
            float t = (x - _zClipBarRect.X) / (float)_zClipBarRect.Width;
            t = Math.Clamp(t, 0f, 1f);
            return _zClipRangeMin + t * span;
        }

        /// <summary>True if mouse is over the Z-clip bar / handle area.</summary>
        private bool IsOverZClipBar(int x, int y)
        {
            if (!Flags.Instance.ZClipping || _camera == null) return false;
            if (_zClipBarRect.Width <= 0) return false;
            var hit = _zClipBarRect;
            hit.Inflate(0, 10); // include handle overhang
            return hit.Contains(x, y);
        }

        private ZClipDragMode HitTestZClipHandle(int x, int y)
        {
            if (!IsOverZClipBar(x, y) || _camera == null) return ZClipDragMode.None;

            float span = Math.Max(_zClipRangeMax - _zClipRangeMin, 1f);
            float MapX(float z) =>
                _zClipBarRect.X + (z - _zClipRangeMin) / span * _zClipBarRect.Width;

            int nearX = (int)MapX(Math.Clamp(_camera.NearPlaneDistance, _zClipRangeMin, _zClipRangeMax));
            int farX = (int)MapX(Math.Clamp(_camera.FarPlaneDistance, _zClipRangeMin, _zClipRangeMax));

            int dn = Math.Abs(x - nearX);
            int df = Math.Abs(x - farX);

            // Prefer closer handle within hit radius
            if (dn <= ZClipHandleHitPx && dn <= df) return ZClipDragMode.Near;
            if (df <= ZClipHandleHitPx) return ZClipDragMode.Far;

            // Click on bar body: nearer scissors by side of midpoint of visible window
            int mid = (nearX + farX) / 2;
            return x < mid ? ZClipDragMode.Near : ZClipDragMode.Far;
        }

        private void ApplyZClipDrag(int mouseX)
        {
            if (_camera == null || _zClipDragMode == ZClipDragMode.None) return;

            float depth = ZClipXToDepth(mouseX);

            if (_zClipDragMode == ZClipDragMode.Near)
            {
                _camera.NearPlaneDistance = Math.Clamp(
                    depth, 0.05f, _camera.FarPlaneDistance - 0.5f);
            }
            else if (_zClipDragMode == ZClipDragMode.Far)
            {
                _camera.FarPlaneDistance = Math.Clamp(
                    depth, _camera.NearPlaneDistance + 0.5f, 500f);
            }

            _camera.MarkChanged();
        }

        private void DrawLasso()
        {
            if (_camera != null && _camera.flag_lasso && Lasso.VertexList.Count > 0)
            {
                var projection = _camera.Projection2;
                GraphiscHelper.MultiLine(Lasso.VertexList, XnaColor.Yellow, projection, _graphicsDevice, _basicEffect);
            }
        }

        public void DrawSceneToRenderTarget()
        {
            if (_draw == null || _graphicsDevice == null || _graphicsDevice.IsDisposed)
                return;

            var camera = GetCamera();
            if (camera != null)
            {
                camera.GraphicsDevice = _graphicsDevice;
                camera.ApplyEffects();
            }

            _draw.DrawScene();
        }

        public void DrawFullFrameToRenderTarget()
        {
            if (_draw == null || _graphicsDevice == null || _graphicsDevice.IsDisposed)
                return;

            var camera = GetCamera();
            if (camera != null)
            {
                camera.GraphicsDevice = _graphicsDevice;
                camera.ApplyEffects();
            }

            _draw.DrawScene();
            _draw.DrawSpriteLayer();
        }

        #endregion

        #region Mouse Event Handlers

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (!_isInitialized || _camera == null) return;
            if (_isDisposed || _disposeInProgress) return;

            var mainForm = GetMainForm();
            if (mainForm == null) return;

            // Z-clip bar: grab near/far handle before any rotation/pan
            if (Flags.Instance.ZClipping && e.Button == MouseButtons.Left
                && IsOverZClipBar(e.X, e.Y))
            {
                _zClipDragMode = HitTestZClipHandle(e.X, e.Y);
                if (_zClipDragMode != ZClipDragMode.None)
                {
                    ApplyZClipDrag(e.X);
                    _panel.Focus();
                    _panel.Cursor = Cursors.SizeWE;
                    return;
                }
            }

            if (e.Button == MouseButtons.Right)
            {
                // Defer context menu: right-drag pans (Camera.MouseMove),
                // right-click without significant movement opens the menu on MouseUp.
                _rightButtonDown = true;
                _rightDidPan = false;
                _rightDownPos = e.Location;
                _camera.MouseDown(e);
                _panel.Focus();
                return;
            }

            if (mainForm.IsLassoActive && e.Button == MouseButtons.Left)
            {
                HandleLassoStart(e);
                return;
            }

            if (mainForm.IsLabelMoveModeActive && e.Button == MouseButtons.Left)
            {
                HandleLabelMoveStart(e, mainForm);
                return;
            }

            _camera.MouseDown(e);
            _panel.Focus();
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (!_isInitialized || _camera == null) return;
            if (_isDisposed || _disposeInProgress) return;

            if (_zClipDragMode != ZClipDragMode.None)
            {
                _zClipDragMode = ZClipDragMode.None;
                _panel.Cursor = Cursors.Default;
            }

            var mainForm = GetMainForm();
            if (mainForm == null) return;

            if (e.Button == MouseButtons.Right)
            {
                bool showMenu = _rightButtonDown && !_rightDidPan;
                _rightButtonDown = false;
                _rightDidPan = false;
                _panel.Cursor = Cursors.Default;

                // End any pan / rotation state started by right-drag
                _camera.flag_moving = false;
                _camera.flag_rotating = false;
                _camera.MarkChanged();

                if (showMenu)
                    HandleRightClickContextMenu(e, mainForm);
                return;
            }

            if (mainForm.IsLassoActive && e.Button == MouseButtons.Left)
            {
                HandleLassoEnd(mainForm);
                return;
            }

            if (mainForm.IsLabelMoveModeActive && e.Button == MouseButtons.Left)
            {
                HandleLabelMoveEnd(mainForm);
                return;
            }

            if (Flags.Instance.RevealSymmetry && e.Button == MouseButtons.Left)
            {
                _camera.flag_rotating = false;
                _camera.flag_moving = false;
                _camera.flag_zooming = false;
                _camera.MarkChanged();
                return;
            }

            if (Flags.Instance.ExpandContactsActive && e.Button == MouseButtons.Left)
            {
                _camera.flag_rotating = false;
                _camera.flag_moving = false;
                _camera.flag_zooming = false;
                _camera.MarkChanged();
                _panel.Focus();
                return;
            }

            _camera.MouseUp(e);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isInitialized || _camera == null) return;
            if (_isDisposed || _disposeInProgress) return;

            // Right-button drag → pan (mark so MouseUp will not open context menu)
            if (_rightButtonDown && e.Button == MouseButtons.Right)
            {
                int adx = Math.Abs(e.X - _rightDownPos.X);
                int ady = Math.Abs(e.Y - _rightDownPos.Y);
                if (adx >= RightClickDragThresholdPx || ady >= RightClickDragThresholdPx)
                {
                    _rightDidPan = true;
                    _panel.Cursor = Cursors.SizeAll;
                }
            }
            else if (!_rightButtonDown && _panel.Cursor == Cursors.SizeAll)
            {
                _panel.Cursor = Cursors.Default;
            }

            // Active drag of Z-clip scissors on the bar
            if (_zClipDragMode != ZClipDragMode.None && e.Button == MouseButtons.Left)
            {
                ApplyZClipDrag(e.X);
                return;
            }

            // Hover cursor over bar
            if (Flags.Instance.ZClipping && IsOverZClipBar(e.X, e.Y))
                _panel.Cursor = Cursors.SizeWE;
            else if (_zClipDragMode == ZClipDragMode.None
                     && _panel.Cursor == Cursors.SizeWE)
                _panel.Cursor = Cursors.Default;

            var mainForm = GetMainForm();

            // Right-drag: pan via Camera (also Middle); context menu only on click-without-drag
            if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Middle)
            {
                _camera.MouseMove(e, _panel);
                return;
            }

            if (Flags.Instance.RevealSymmetry)
            {
                if (e.Button == MouseButtons.Left && !_camera.flag_rotating) return;
                _camera.MouseMove(e, _panel);
                return;
            }

            if (Flags.Instance.ExpandContactsActive)
            {
                _camera.MouseMove(e, _panel);
                return;
            }

            if (mainForm != null && mainForm.IsLassoActive && e.Button == MouseButtons.Left)
            {
                HandleLassoMove(e);
                return;
            }

            if (mainForm != null && mainForm.IsLabelMoveModeActive)
            {
                if (e.Button == MouseButtons.Left &&
                    (mainForm.IsDraggingLabel
                     || mainForm.IsDraggingMeasurementLabel
                     || mainForm.IsDraggingAxisLabel))
                {
                    mainForm.HandleLabelDrag(new System.Drawing.Point(e.X, e.Y), _panel.Width, _panel.Height);
                }
                return;
            }

            if (Flags.Instance.IsMeasurementModeActive)
            {
                _camera.MouseMove(e, _panel);
                return;
            }

            _camera.MouseMove(e, _panel);
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            if (!_isInitialized || _camera == null) return;
            if (_isDisposed || _disposeInProgress) return;
            if (e.Button == MouseButtons.Right) return;

            var mainForm = GetMainForm();
            if (mainForm == null) return;

            if (mainForm.IsLassoActive || _camera.flag_lasso) return;

            if (mainForm.IsLabelMoveModeActive && e.Button == MouseButtons.Left)
            {
                mainForm.HandleLabelClick(new System.Drawing.Point(e.X, e.Y), _panel.Width, _panel.Height);
                _panel.Focus();
                return;
            }

            if (mainForm.IsPickActive && e.Button == MouseButtons.Left)
            {
                HandlePickClick(e, mainForm);
                return;
            }

            if (Flags.Instance.ExpandContactsActive && e.Button == MouseButtons.Left)
            {
                mainForm.HandleExpandContactsClick(new System.Drawing.Point(e.X, e.Y), _panel.Width, _panel.Height);
                _panel.Focus();
                return;
            }

            if (Flags.Instance.RevealSymmetry && e.Button == MouseButtons.Left)
            {
                mainForm.HandleRevealSymmetryClick(new System.Drawing.Point(e.X, e.Y), _panel.Width, _panel.Height);
                _panel.Focus();
                return;
            }

            if (Flags.Instance.IsMeasurementModeActive && e.Button == MouseButtons.Left)
            {
                mainForm.HandleMeasurementClick(new System.Drawing.Point(e.X, e.Y), _panel.Width, _panel.Height);
                _panel.Focus();
                return;
            }
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!_isInitialized || _camera == null) return;
            if (_isDisposed || _disposeInProgress) return;
            if (e.Button == MouseButtons.Right) return;

            var mainForm = GetMainForm();
            if (mainForm == null) return;

            if (mainForm.IsLabelMoveModeActive) return;

            if (Flags.Instance.ExpandContactsActive || mainForm.IsLassoActive || _camera.flag_lasso ||
                mainForm.IsPickActive || Flags.Instance.IsMeasurementModeActive)
            {
                mainForm.ClearAllSelection();
                RefreshView();
                mainForm.SetStatus("Cleared selection (double click)", "Structures: 0", "🧹");
                return;
            }

            if (Flags.Instance.RevealSymmetry) return;

            _camera.MouseDoubleClick(e);
            _camera.Reset();
            _camera.MarkChanged();
            mainForm.SetStatus("Reset view (double click)", "Structures: 0", "🔄");
        }

        private void OnMouseWheel(object sender, MouseEventArgs e)
        {
            if (!_isInitialized || _camera == null) return;
            if (_isDisposed || _disposeInProgress) return;

            _camera.MouseWheel(e);
        }

        #endregion

        #region Right-Click Context Menu

        private void HandleRightClickContextMenu(MouseEventArgs e, MainForm mainForm)
        {
            var molecule = mainForm.GetActiveMolecule();

            int atomIndex = -1;

            if (molecule != null && molecule.NumberOfAtoms > 0)
            {
                atomIndex = FindNearestAtomAtPosition(
                    new System.Drawing.Point(e.X, e.Y),
                    _panel.Width,
                    _panel.Height,
                    20);
            }

            System.Drawing.Point screenPos = _panel.PointToScreen(new System.Drawing.Point(e.X, e.Y));

            mainForm.Show3DContextMenu(atomIndex, screenPos);
        }

        #endregion

        #region Mouse Event Helpers

        private void HandleLassoStart(MouseEventArgs e)
        {
            _camera!.flag_rotating = false;
            _camera.flag_lasso = true;
            _camera.flag_select = false;
            _camera.flag_moving = false;
            _camera.flag_release = false;

            Lasso.VertexList.Clear();
            Lasso.VertexList.Add(new Vector2(e.X, e.Y));

            _camera.ptMousePrevious.X = e.X;
            _camera.ptMousePrevious.Y = e.Y;
            _camera.ptMouseCurrent.X = e.X;
            _camera.ptMouseCurrent.Y = e.Y;

            RefreshView();
            _panel.Focus();
        }

        private void HandleLassoEnd(MainForm mainForm)
        {
            if (Lasso.VertexList.Count > 2)
            {
                _camera!.ApplyEffects();
                mainForm.CloseLasso(_panel.Width, _panel.Height);
            }
            else
            {
                Lasso.VertexList.Clear();
            }

            _camera!.flag_lasso = false;
            _camera.flag_rotating = true;
            _camera.flag_release = false;

            RefreshView();
        }

        private void HandleLassoMove(MouseEventArgs e)
        {
            Vector2 currentPos = new Vector2(e.X, e.Y);
            if (Lasso.VertexList.Count > 0)
            {
                float dist = Vector2.Distance(currentPos, Lasso.VertexList[Lasso.VertexList.Count - 1]);
                if (dist > Lasso.MIN_POINT_DISTANCE)
                {
                    Lasso.VertexList.Add(currentPos);
                    _camera!.ptMouseCurrent.X = e.X;
                    _camera.ptMouseCurrent.Y = e.Y;
                }
            }
        }

        private void HandleLabelMoveStart(MouseEventArgs e, MainForm mainForm)
        {
            _camera!.flag_rotating = false;
            _camera.flag_moving = false;
            _camera.flag_select = false;
            _camera.flag_lasso = false;

            mainForm.HandleLabelMouseDown(new System.Drawing.Point(e.X, e.Y), _panel.Width, _panel.Height);

            _panel.Focus();
        }

        private void HandleLabelMoveEnd(MainForm mainForm)
        {
            mainForm.EndLabelDrag();
            _camera!.flag_rotating = true;
            RefreshView();
        }

        private void HandlePickClick(MouseEventArgs e, MainForm mainForm)
        {
            int atomIndex = FindAtomAtPosition(new System.Drawing.Point(e.X, e.Y), _panel.Width, _panel.Height);

            if (atomIndex >= 0)
            {
                var molecule = mainForm.GetActiveMolecule();
                if (molecule != null)
                {
                    if (mainForm.HandlePlaneSelectionClick(atomIndex))
                    {
                        RefreshView();
                        _panel.Focus();
                        return;
                    }

                    if (mainForm.IsSelectMoleculeMode)
                    {
                        mainForm.SelectMoleculeContainingAtom(atomIndex, clearPrevious: false);
                        NotifyMoleculeChanged();
                        RefreshView();
                        _panel.Focus();
                        return;
                    }

                    if (!molecule.atom[atomIndex].selected)
                    {
                        molecule.atom[atomIndex].selected = true;
                        molecule.NumberSelected++;
                        molecule.atom[atomIndex].SelectionOrder = molecule.NumberSelected;
                    }
                    else
                    {
                        molecule.atom[atomIndex].selected = false;
                        molecule.atom[atomIndex].SelectionOrder = 0;
                        molecule.NumberSelected--;
                    }

                    mainForm.SetStatus($"Selected {molecule.NumberSelected} atoms",
                        $"Structures: {StructureStorage.Instance.StructureCount}", "👆");

                    NotifyMoleculeChanged();
                    RefreshView();
                }
            }

            _panel.Focus();
        }

        #endregion

        #region Keyboard Event Handlers

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isInitialized || _camera == null) return;
            if (_isDisposed || _disposeInProgress) return;

            var mainForm = GetMainForm();

            if (mainForm != null && mainForm.IsLabelMoveModeActive)
                return;

            switch (e.KeyCode)
            {
                case Keys.R:
                    _camera.Reset();
                    if (_molecule != null && _molecule.NumberOfAtoms > 0)
                    {
                        Vector3 center = Vector3.Zero;
                        for (int i = 0; i < _molecule.NumberOfAtoms; i++)
                            center += _molecule.atom[i].Coordinate;
                        center /= _molecule.NumberOfAtoms;
                        _camera.g_system_translate = -center;
                        _camera.MarkChanged();
                    }
                    break;

                case Keys.Add:
                case Keys.Oemplus:
                    var argsPlus = new MouseEventArgs(MouseButtons.None, 0, 0, 0, 120);
                    _camera.MouseWheel(argsPlus);
                    break;

                case Keys.Subtract:
                case Keys.OemMinus:
                    var argsMinus = new MouseEventArgs(MouseButtons.None, 0, 0, 0, -120);
                    _camera.MouseWheel(argsMinus);
                    break;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e) { }

        #endregion

        #region Panel Event Handlers

        private void OnResize(object sender, EventArgs e) { }

        #endregion

        #region Helper Methods

        private int FindAtomAtPosition(System.Drawing.Point mousePosition, int viewportWidth, int viewportHeight)
        {
            var mainForm = GetMainForm();
            var molecule = mainForm?.GetActiveMolecule();
            var camera = _camera;

            if (molecule == null || camera == null || molecule.NumberOfAtoms == 0)
                return -1;

            const int TOLERANCE = 15;
            Matrix fullTransformMatrix = camera.Scale * camera.Rotate * camera.View *
                                          _camera.Translate * camera.Projection;

            for (int i = 0; i < molecule.NumberOfAtoms; ++i)
            {
                if (molecule.atom[i].style == 10) continue;
                if (!molecule.atom[i].show) continue;

                Vector3 pos = molecule.atom[i].Coordinate;
                Matrix world = Matrix.CreateTranslation(pos);
                Matrix worldViewProjectionMatrix = world * fullTransformMatrix;

                float w = worldViewProjectionMatrix.M44;
                if (w <= 0) continue;

                int modelX = (int)(viewportWidth / 2 * (1.0 + worldViewProjectionMatrix.M41 / w));
                int modelY = (int)(viewportHeight / 2 * (1.0 - worldViewProjectionMatrix.M42 / w));

                int dx = Math.Abs(modelX - mousePosition.X);
                int dy = Math.Abs(modelY - mousePosition.Y);

                if (dx < TOLERANCE && dy < TOLERANCE)
                    return i;
            }

            return -1;
        }

        private int FindNearestAtomAtPosition(System.Drawing.Point mousePosition,
                                               int viewportWidth,
                                               int viewportHeight,
                                               int tolerance = 20)
        {
            var mainForm = GetMainForm();
            var molecule = mainForm?.GetActiveMolecule();
            var camera = _camera;

            if (molecule == null || camera == null || molecule.NumberOfAtoms == 0)
                return -1;

            Matrix fullTransformMatrix = camera.Scale * camera.Rotate * camera.View *
                                          _camera.Translate * camera.Projection;

            int halfWidth = viewportWidth / 2;
            int halfHeight = viewportHeight / 2;

            int bestIndex = -1;
            float bestDistSq = float.MaxValue;
            float toleranceSq = tolerance * tolerance;

            for (int i = 0; i < molecule.NumberOfAtoms; ++i)
            {
                if (molecule.atom[i].style == 10) continue;
                if (!molecule.atom[i].show) continue;

                Vector3 pos = molecule.atom[i].Coordinate;
                Matrix world = Matrix.CreateTranslation(pos);
                Matrix worldViewProjectionMatrix = world * fullTransformMatrix;

                float w = worldViewProjectionMatrix.M44;
                if (w <= 0) continue;

                int modelX = (int)(halfWidth * (1.0 + worldViewProjectionMatrix.M41 / w));
                int modelY = (int)(halfHeight * (1.0 - worldViewProjectionMatrix.M42 / w));

                int dx = modelX - mousePosition.X;
                int dy = modelY - mousePosition.Y;
                float distSq = dx * dx + dy * dy;

                if (distSq < toleranceSq && distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (_isDisposed) return;

            _disposeInProgress = true;

            lock (_renderLock)
            {
                if (_isDisposed) return;

                try { _renderTimer?.Stop(); }
                catch { }

                try { _renderTimer?.Dispose(); }
                catch { }
                _renderTimer = null;

 // ⭐⭐ NEW: disafterse symmetry renderer razem z GameWindow
                try { _symmetryRenderer?.Dispose(); }
                catch { }
                _symmetryRenderer = null;

                _debugPixelTexture?.Dispose();
                _debugPixelTexture = null;

                _currentTarget?.Dispose();
                _currentTarget = null;

                _tempDownsampleRT?.Dispose();
                _tempDownsampleRT = null;

                _spriteBatch?.Dispose();
                _spriteBatch = null;

                _basicEffect?.Dispose();
                _basicEffect = null;

                _sphereImpostorEffect = null;
                _cavitySurfaceEffect = null;

                _content?.Dispose();
                _content = null;

                if (_graphicsDevice != null && !_graphicsDevice.IsDisposed)
                {
                    try { _graphicsDevice.Dispose(); }
                    catch { }
                    _graphicsDevice = null;
                }

                _camera = null;
                _draw = null;
                _molecule = null;
                _smallBall = null;
                _bigBall = null;
                _smallRod = null;
                _spriteFont = null;
                _cavityRenderer = null;
                _mainForm = null;

                _isDisposed = true;
            }
        }

        #endregion
    }

    #region Helper Classes

    public class ServiceContainer : IServiceProvider
    {
        private readonly System.Collections.Generic.Dictionary<Type, object> _services = new System.Collections.Generic.Dictionary<Type, object>();

        public void AddService<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        public object GetService(Type serviceType)
        {
            _services.TryGetValue(serviceType, out object service);
            return service;
        }
    }

    public class SimpleGraphicsDeviceService : IGraphicsDeviceService
    {
        private GraphicsDevice _graphicsDevice;

        public SimpleGraphicsDeviceService(GraphicsDevice device)
        {
            _graphicsDevice = device;
        }

        public GraphicsDevice GraphicsDevice => _graphicsDevice;

        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;
    }

    #endregion
}