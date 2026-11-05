// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal morphology / Wulff module: WulffPlotDocument.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// Wulff Plot v7 — zoom with mouse, prosta rotation, all etykiety
// Tło z Flags.Instance.color1

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Tal.Utils;
using WeifenLuo.WinFormsUI.Docking;
using Color = Microsoft.Xna.Framework.Color;
using GdiColor = System.Drawing.Color;

namespace Tal.Morphology
{
    public class WulffPlotDocument : DockContent
    {
        #region Fields

        private Panel _panel;
        private Panel _controlsHost;
        private FlowLayoutPanel _controls;

        private GraphicsDevice _device;
        private BasicEffect _effect;
        private SpriteBatch _spriteBatch;
        private SpriteFont _spriteFont;
        private Texture2D _pixelTexture;
        private ContentManager _content;

        private CrystalMorphologyResult _morphResult;
        private MorphologyCrystal _crystalRef;
        private MorphologyDocument _doc3D;
        private bool _deviceReady = false;
        private bool _disposed = false;

        private System.Windows.Forms.Timer _timer;

 // ⭐ Bufory
        private VertexBuffer _circleVB;
        private VertexBuffer _gridVB;
        private VertexBuffer _pointsVB;
        private int _circleVertCount;
        private int _gridVertCount;
        private int _pointsVertCount;
        private int _screenW = 800;
        private int _screenH = 600;

 // ⭐ Cache points
        private Dictionary<string, Vector2> _plotPositions = new Dictionary<string, Vector2>();
        private Dictionary<string, Color> _plotColors = new Dictionary<string, Color>();
        private Dictionary<string, bool> _plotIsUpper = new Dictionary<string, bool>();

 // ⭐ Kontrolki
        private CheckBox _chkLabels;
        private CheckBox _chkLabelsAll;
        private CheckBox _chkGrid;
        private CheckBox _chkSphereCircle;
        private CheckBox _chkColorByD;
        private CheckBox _chkFollow3D;

 // ⭐ Radio — półkula
        private RadioButton _rbUpperOnly;
        private RadioButton _rbLowerOnly;
        private RadioButton _rbBothHemispheres;
        private GroupBox _grpHemisphere;

        private TrackBar _trackPointSize;
        private Label _lblPointSizeValue;
        private float _pointSize = 10f;

 // ⭐ Zoom — TYLKO with mouse (without suwaka)
        private float _zoomFactor = 1.0f;
        private Label _lblZoomInfo;

        private HashSet<char> _availableCharsCache;
        private SpriteFont _cachedFontRef;

        #endregion

        #region Debug Helpers

        private static void LogWarn(string msg) => ProgressReporter.Log(msg, GdiColor.Orange);
        private static void LogError(string msg) => ProgressReporter.Log(msg, GdiColor.Red);
        private static void LogSuccess(string msg) => ProgressReporter.Log(msg, GdiColor.LightGreen);

        #endregion

        #region Helper — XNA → GDI

 /// <summary>
 /// ⭐ Konversion XNA Color na GDI Color (for WinForms BackColor).
 /// </summary>
        private static GdiColor XnaToGdi(Color c)
        {
            return GdiColor.FromArgb(c.A, c.R, c.G, c.B);
        }

        #endregion

        #region Constructor

        public WulffPlotDocument(string title = "Wulff Plot")
        {
            Text = title;
            DockAreas = DockAreas.Document | DockAreas.Float;
            BackColor = XnaToGdi(Flags.Instance.color1);   // ⭐ tło z Flagi

            _controlsHost = new Panel
            {
                Dock = DockStyle.Right,
                Width = 280,
                BackColor = GdiColor.FromArgb(245, 245, 250)   // panel kontrolek — jasny
            };
            BuildControlsPanel();

            _panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = XnaToGdi(Flags.Instance.color1)   // background from Flags
            };

            _panel.HandleCreated += (s, e) =>
            {
                if (!_deviceReady)
                    InitializeGraphics();
            };

 // ⭐ Zoom with mouse
            _panel.MouseWheel += Panel_MouseWheel;

            Controls.Add(_panel);
            Controls.Add(_controlsHost);

            _timer = new System.Windows.Forms.Timer { Interval = 33 };
            _timer.Tick += (s, e) => RenderFrame();
            _timer.Start();

            Disposed += (s, e) => CleanupAll();
        }

        #endregion

        #region Public API

        public void SetData(CrystalMorphologyResult result,
                            MorphologyCrystal crystal,
                            MorphologyDocument doc3D,
                            string structureName)
        {
            _morphResult = result;
            _crystalRef = crystal;
            _doc3D = doc3D;
            Text = $"Wulff Plot: {structureName}";

            if (_doc3D != null)
            {
                _doc3D.CameraRotated -= OnCameraRotated;
                _doc3D.CameraRotated += OnCameraRotated;
            }

            ExtractPlotPositions();

            if (_deviceReady)
                RebuildBuffers();

            RenderFrame();
        }

        public void RefreshFromCrystal()
        {
            if (!_deviceReady) return;

            if (_doc3D != null)
            {
                try { _crystalRef = _doc3D.GetCrystal(); } catch { }
            }

            ExtractPlotPositions();
            RebuildBuffers();
        }

        private void OnCameraRotated()
        {
            if (!_deviceReady) return;

            if (_chkFollow3D != null && _chkFollow3D.Checked)
            {
                ExtractPlotPositions();
                RebuildBuffers();
            }
        }

        #endregion

        #region Extract Plot Positions

        private void ExtractPlotPositions()
        {
            _plotPositions.Clear();
            _plotColors.Clear();
            _plotIsUpper.Clear();

            if (_morphResult == null || _morphResult.Faces == null || _morphResult.Faces.Count == 0)
                return;

            HashSet<string> activeFaces = new HashSet<string>();
            if (_crystalRef != null && _crystalRef.Triangles != null)
            {
                foreach (var t in _crystalRef.Triangles)
                {
                    if (!string.IsNullOrEmpty(t.FaceName))
                        activeFaces.Add(t.FaceName);
                }
            }

 // ⭐ Rotacja — BEZ Gram-Schmidt, prosta transformation
            Matrix rotationMatrix = Matrix.Identity;
            bool follow3D = _chkFollow3D != null && _chkFollow3D.Checked;
            if (follow3D && _doc3D != null)
            {
                try
                {
                    Matrix raw = _doc3D.GetRotationMatrix();

 // Wyciągnij samą rotację 3×3 (without translation)
                    Matrix clean = Matrix.Identity;
                    clean.M11 = raw.M11; clean.M12 = raw.M12; clean.M13 = raw.M13;
                    clean.M21 = raw.M21; clean.M22 = raw.M22; clean.M23 = raw.M23;
                    clean.M31 = raw.M31; clean.M32 = raw.M32; clean.M33 = raw.M33;

 // ⭐ KEY: dołóż view — dokładnie ten sam, co w MorphologyDocument.RenderFrame()
                    Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 100), Vector3.Zero, Vector3.Up);

 // Normalna ma przejść: kryształ → world → view (without projection!)
 // Note: TransformNormal ignoruje translację, so translation z view (0,0,-100)
 // i tak nie wpłynie na kierunek — ale rotation z view already tak.
                    rotationMatrix = clean * view;
                }
                catch
                {
                    rotationMatrix = Matrix.Identity;
                }
            }

            float maxD = 0f;
            foreach (var f in _morphResult.Faces)
                if (f.DSpacing > maxD) maxD = f.DSpacing;

            int hemiMode = 2;
            if (_rbUpperOnly != null && _rbUpperOnly.Checked) hemiMode = 0;
            else if (_rbLowerOnly != null && _rbLowerOnly.Checked) hemiMode = 1;
            else hemiMode = 2;

            bool showAllFaces = _chkLabelsAll != null && _chkLabelsAll.Checked;

            foreach (var face in _morphResult.Faces)
            {
                string key = face.Name;

                if (!showAllFaces && activeFaces.Count > 0 && !activeFaces.Contains(key))
                    continue;

                if (_plotPositions.ContainsKey(key))
                    continue;

                Vector3 normal = face.NormalCart;
                if (follow3D)
                    normal = Vector3.TransformNormal(normal, rotationMatrix);

                bool isUpper = normal.Z >= 0f;

                if (hemiMode == 0 && !isUpper) continue;
                if (hemiMode == 1 && isUpper) continue;

                Vector2? plotPos = StereographicProject(normal, out bool isLowerHemi);
                if (!plotPos.HasValue) continue;

                Vector2 p = plotPos.Value;

                _plotPositions[key] = p;
                _plotIsUpper[key] = !isLowerHemi;

#if DEBUG
                if (key == "(100)" || key == "(010)" || key == "(001)" ||
                    key == "(-100)" || key == "(0-10)" || key == "(00-1)")
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[WulffPlot] {key} NC=({normal.X:F4},{normal.Y:F4},{normal.Z:F4}) " +
                        $"plot=({p.X:F4},{p.Y:F4}) |r|={p.Length():F4}");
                }
#endif

                Color pointColor;
                if (_chkColorByD != null && _chkColorByD.Checked)
                {
                    float t = maxD > 0 ? face.DSpacing / maxD : 0.5f;
                    t = Math.Clamp(t, 0f, 1f);

                    int rC = (int)(255 * t);
                    int gC = (int)(100 + 100 * (1 - Math.Abs(t - 0.5f) * 2));
                    int bC = (int)(255 * (1 - t));
                    pointColor = new Color(rC, gC, bC, 255);
                }
                else
                {
 // ⭐ Use tego samego colouru co w 3D (hash FNV-1a z nazwy)
                    pointColor = MorphologyCrystal.NameToColor(key);
                }

                _plotColors[key] = pointColor;
            }
        }

        #endregion

        #region Mouse (zoom myszą)

        private void Panel_MouseWheel(object sender, MouseEventArgs e)
        {
 // ⭐ Zoom with mouse
            float delta = e.Delta / 120f;

            if (delta > 0)
                _zoomFactor *= 1.15f;
            else
                _zoomFactor *= 0.87f;

            _zoomFactor = Math.Clamp(_zoomFactor, 0.3f, 5.0f);

            if (_lblZoomInfo != null)
                _lblZoomInfo.Text = $"Zoom: {_zoomFactor:F2}× (mouse wheel)";

            RebuildBuffers();
        }

        #endregion

        #region UI Builder

        private void BuildControlsPanel()
        {
            _controls = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = GdiColor.FromArgb(245, 245, 250)
            };

            _controls.Controls.Add(new Label
            {
                Text = "Wulff Plot",
                Width = 240,
                Height = 24,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = GdiColor.FromArgb(40, 40, 60),
                Margin = new Padding(0, 0, 0, 10)
            });

            _chkFollow3D = new CheckBox
            {
                Text = "Follow 3D Rotation",
                Checked = true,
                Width = 240,
                Height = 22,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 0, 0, 4)
            };
            _chkFollow3D.CheckedChanged += (s, e) =>
            {
                ExtractPlotPositions();
                RebuildBuffers();
            };
            _controls.Controls.Add(_chkFollow3D);

            _chkLabels = new CheckBox
            {
                Text = "Show Labels (hkl)",
                Checked = true,
                Width = 240,
                Height = 22,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 0, 0, 4)
            };
            _chkLabels.CheckedChanged += (s, e) => RenderFrame();
            _controls.Controls.Add(_chkLabels);

            _chkLabelsAll = new CheckBox
            {
                Text = "Show ALL faces (not just in crystal)",
                Checked = false,
                Width = 240,
                Height = 22,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 0, 0, 8)
            };
            _chkLabelsAll.CheckedChanged += (s, e) =>
            {
                ExtractPlotPositions();
                RebuildBuffers();
            };
            _controls.Controls.Add(_chkLabelsAll);

            _chkGrid = new CheckBox
            {
                Text = "Show Grid (10°)",
                Checked = true,
                Width = 240,
                Height = 22,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 0, 0, 4)
            };
            _chkGrid.CheckedChanged += (s, e) => RebuildBuffers();
            _controls.Controls.Add(_chkGrid);

            _chkSphereCircle = new CheckBox
            {
                Text = "Show Equator (θ=90°)",
                Checked = true,
                Width = 240,
                Height = 22,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 0, 0, 4)
            };
            _chkSphereCircle.CheckedChanged += (s, e) => RebuildBuffers();
            _controls.Controls.Add(_chkSphereCircle);

            _chkColorByD = new CheckBox
            {
                Text = "Color by d-spacing",
                Checked = false,
                Width = 240,
                Height = 22,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 0, 0, 8)
            };
            _chkColorByD.CheckedChanged += (s, e) =>
            {
                ExtractPlotPositions();
                RebuildBuffers();
            };
            _controls.Controls.Add(_chkColorByD);

            _grpHemisphere = new GroupBox
            {
                Text = "Hemisphere",
                Width = 240,
                Height = 100,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 4, 0, 8),
                ForeColor = GdiColor.FromArgb(40, 40, 60)
            };

            _rbUpperOnly = new RadioButton
            {
                Text = "Upper only (filled)",
                Location = new System.Drawing.Point(10, 20),
                Width = 220,
                Checked = false
            };
            _rbUpperOnly.CheckedChanged += (s, e) =>
            {
                if (_rbUpperOnly.Checked)
                {
                    ExtractPlotPositions();
                    RebuildBuffers();
                }
            };

            _rbLowerOnly = new RadioButton
            {
                Text = "Lower only (hollow)",
                Location = new System.Drawing.Point(10, 44),
                Width = 220,
                Checked = false
            };
            _rbLowerOnly.CheckedChanged += (s, e) =>
            {
                if (_rbLowerOnly.Checked)
                {
                    ExtractPlotPositions();
                    RebuildBuffers();
                }
            };

            _rbBothHemispheres = new RadioButton
            {
                Text = "Both (upper + lower)",
                Location = new System.Drawing.Point(10, 68),
                Width = 220,
                Checked = true
            };
            _rbBothHemispheres.CheckedChanged += (s, e) =>
            {
                if (_rbBothHemispheres.Checked)
                {
                    ExtractPlotPositions();
                    RebuildBuffers();
                }
            };

            _grpHemisphere.Controls.Add(_rbUpperOnly);
            _grpHemisphere.Controls.Add(_rbLowerOnly);
            _grpHemisphere.Controls.Add(_rbBothHemispheres);
            _controls.Controls.Add(_grpHemisphere);

            _controls.Controls.Add(MakeLabel("Point Size:"));
            _lblPointSizeValue = MakeValueLabel("10");
            _controls.Controls.Add(_lblPointSizeValue);

            _trackPointSize = new TrackBar
            {
                Minimum = 4,
                Maximum = 24,
                Value = 10,
                TickFrequency = 4,
                LargeChange = 4,
                SmallChange = 2,
                Width = 240,
                Height = 40,
                Margin = new Padding(0, 0, 0, 8)
            };
            _trackPointSize.Scroll += (s, e) =>
            {
                _pointSize = _trackPointSize.Value;
                _lblPointSizeValue.Text = _pointSize.ToString("F0");
                RebuildBuffers();
            };
            _controls.Controls.Add(_trackPointSize);

 // ⭐ Info o zoomie with mouse
            _lblZoomInfo = new Label
            {
                Text = "Zoom: 1.00× (mouse wheel)",
                Width = 240,
                Height = 24,
                Font = new Font("Consolas", 9F, FontStyle.Bold),
                ForeColor = GdiColor.FromArgb(0, 90, 160),
                Margin = new Padding(0, 4, 0, 8),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _controls.Controls.Add(_lblZoomInfo);

            var btnResetZoom = new Button
            {
                Text = "Reset Zoom",
                Width = 240,
                Height = 28,
                BackColor = GdiColor.FromArgb(230, 230, 235),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 0, 0, 8)
            };
            btnResetZoom.Click += (s, e) =>
            {
                _zoomFactor = 1.0f;
                if (_lblZoomInfo != null)
                    _lblZoomInfo.Text = $"Zoom: 1.00× (mouse wheel)";
                RebuildBuffers();
            };
            _controls.Controls.Add(btnResetZoom);

            var btnRefresh = new Button
            {
                Text = "Refresh",
                Width = 240,
                Height = 28,
                BackColor = GdiColor.FromArgb(230, 230, 235),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 0, 0, 10)
            };
            btnRefresh.Click += (s, e) => RefreshFromCrystal();
            _controls.Controls.Add(btnRefresh);

            _controls.Controls.Add(new Label
            {
                Text = "Stereographic projection\r\n" +
                       "Center = +c axis\r\n" +
                       "Two-pole Wulff net\r\n" +
                       "Filled = upper, hollow = lower\r\n" +
                       "Mouse wheel = zoom",
                Width = 240,
                Height = 100,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = GdiColor.FromArgb(100, 100, 120),
                Margin = new Padding(0, 5, 0, 0)
            });

            _controlsHost.Controls.Add(_controls);
        }

        private Label MakeLabel(string text) => new Label
        {
            Text = text,
            Width = 240,
            Height = 18,
            Font = new Font("Segoe UI", 9F),
            Margin = new Padding(0, 5, 0, 0)
        };

        private Label MakeValueLabel(string text) => new Label
        {
            Text = text,
            Width = 240,
            Height = 18,
            Font = new Font("Consolas", 9F, FontStyle.Bold),
            ForeColor = GdiColor.FromArgb(0, 90, 160),
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0)
        };

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

                _device = new GraphicsDevice(GraphicsAdapter.DefaultAdapter,
                    GraphicsProfile.Reach, pp);

                _effect = new BasicEffect(_device)
                {
                    VertexColorEnabled = true,
                    LightingEnabled = false,
                    TextureEnabled = false,
                    View = Matrix.Identity,
                    World = Matrix.Identity,
                    Projection = Matrix.CreateOrthographicOffCenter(0, w, h, 0, 0, 1)
                };

                _spriteBatch = new SpriteBatch(_device);

                _pixelTexture = new Texture2D(_device, 1, 1);
                _pixelTexture.SetData(new[] { Color.White });

                LoadLocalSpriteFont();

                _deviceReady = true;
                _screenW = w;
                _screenH = h;

                LogSuccess($"[WulffPlot] ✅ Graphics initialized ({w}x{h})");

                ExtractPlotPositions();
                RebuildBuffers();
            }
            catch (Exception ex)
            {
                LogError($"[WulffPlot] ❌ Init error: {ex.Message}");
                MessageBox.Show($"Graphics init error: {ex.Message}",
                    "Wulff Plot", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLocalSpriteFont()
        {
            if (_device == null) return;

            string contentPath = null;

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? baseDir;

                string[] candidates = new[]
                {
                    Path.Combine(baseDir, "Content"),
                    Path.Combine(exeDir, "Content"),
                    Path.Combine(baseDir, "..", "..", "..", "Content"),
                    Path.Combine(exeDir, "..", "..", "..", "Content"),
                };

                foreach (var candidate in candidates)
                {
                    if (Directory.Exists(candidate))
                    {
                        string testFile = Path.Combine(candidate, "SpriteFont.xnb");
                        if (File.Exists(testFile))
                        {
                            contentPath = Path.GetFullPath(candidate);
                            break;
                        }
                    }
                }

                if (contentPath == null)
                {
                    LogError("[WulffPlot] ❌ Cannot find Content folder");
                    return;
                }

                var services = new ServiceContainer();
                services.AddService<IGraphicsDeviceService>(
                    new SimpleGraphicsDeviceService(_device));

                _content = new ContentManager(services, contentPath);
                _spriteFont = _content.Load<SpriteFont>("SpriteFont");

                LogSuccess($"[WulffPlot] ✅ SpriteFont loaded — " +
                           $"{_spriteFont.Characters.Count} chars");
            }
            catch (Exception ex)
            {
                LogError($"[WulffPlot] ❌ SpriteFont load failed: {ex.Message}");
                _spriteFont = null;
            }
        }

        #endregion

        #region Stereographic Projection

        private Vector2? StereographicProject(Vector3 normalCart, out bool isLower)
        {
            isLower = false;

            if (normalCart.LengthSquared() < 1e-9f) return null;

            Vector3 n = Vector3.Normalize(normalCart);

            if (n.Z < 0)
            {
                isLower = true;
                n.Z = -n.Z;
            }

            float nz = Math.Clamp(n.Z, 0f, 1f);
            float theta = (float)Math.Acos(nz);
            float rPlot = (float)Math.Tan(theta * 0.5f);

            if (float.IsNaN(rPlot) || float.IsInfinity(rPlot))
                return null;

            float phi = (float)Math.Atan2(n.Y, n.X);

            return new Vector2(
                rPlot * (float)Math.Cos(phi),
                rPlot * (float)Math.Sin(phi));
        }

 /// <summary>
 /// ⭐ Konversion plot → pixele. Kwadratowy obszar, so that angley były wierne!
 /// </summary>
        private Vector2 PlotToScreen(Vector2 plotCoords, float panelRadius)
        {
            float cx = _screenW / 2f;
            float cy = _screenH / 2f;

            float effectiveRadius = panelRadius * _zoomFactor;

            return new Vector2(
                cx + plotCoords.X * effectiveRadius,
                cy - plotCoords.Y * effectiveRadius);
        }

 /// <summary>
 /// ⭐ Panel radius — always MIN(width, height) so that zachować proafterrcje angleowe.
 /// </summary>
        private float GetPanelRadius()
        {
            return Math.Min(_screenW, _screenH) / 2f * 0.85f;
        }

        #endregion

        #region Buffer Building

        private void RebuildBuffers()
        {
            if (!_deviceReady) return;

            float panelRadius = GetPanelRadius();

 // ═══ 1. Okrąg external ═══
            var circleVerts = new List<VertexPositionColor>();
            const int CIRCLE_SEGMENTS = 128;
            Color circleColor = new Color(180, 180, 200, 255);

            for (int i = 0; i < CIRCLE_SEGMENTS; i++)
            {
                float a1 = (float)(i * 2 * Math.PI / CIRCLE_SEGMENTS);
                float a2 = (float)((i + 1) * 2 * Math.PI / CIRCLE_SEGMENTS);

                Vector2 p1 = PlotToScreen(new Vector2((float)Math.Cos(a1), (float)Math.Sin(a1)), panelRadius);
                Vector2 p2 = PlotToScreen(new Vector2((float)Math.Cos(a2), (float)Math.Sin(a2)), panelRadius);

                circleVerts.Add(new VertexPositionColor(new Vector3(p1, 0), circleColor));
                circleVerts.Add(new VertexPositionColor(new Vector3(p2, 0), circleColor));
            }

            _circleVB?.Dispose();
            if (circleVerts.Count > 0)
            {
                _circleVB = new VertexBuffer(_device, VertexPositionColor.VertexDeclaration,
                    circleVerts.Count, BufferUsage.WriteOnly);
                _circleVB.SetData(circleVerts.ToArray());
            }
            _circleVertCount = circleVerts.Count;

 // ═══ 2. Siatka ═══
            var gridVerts = new List<VertexPositionColor>();
            Color gridColor = new Color(60, 60, 90, 200);

            if (_chkGrid != null && _chkGrid.Checked)
            {
                for (int thetaDeg = 10; thetaDeg <= 80; thetaDeg += 10)
                {
                    float theta = thetaDeg * MathF.PI / 180f;
                    float r = (float)Math.Tan(theta * 0.5f);
                    if (r > 1.0f) continue;

                    const int SEGMENTS = 64;
                    for (int i = 0; i < SEGMENTS; i++)
                    {
                        float a1 = (float)(i * 2 * Math.PI / SEGMENTS);
                        float a2 = (float)((i + 1) * 2 * Math.PI / SEGMENTS);

                        Vector2 p1 = PlotToScreen(new Vector2(r * (float)Math.Cos(a1), r * (float)Math.Sin(a1)), panelRadius);
                        Vector2 p2 = PlotToScreen(new Vector2(r * (float)Math.Cos(a2), r * (float)Math.Sin(a2)), panelRadius);

                        gridVerts.Add(new VertexPositionColor(new Vector3(p1, 0), gridColor));
                        gridVerts.Add(new VertexPositionColor(new Vector3(p2, 0), gridColor));
                    }
                }

                for (int phiDeg = 0; phiDeg < 360; phiDeg += 30)
                {
                    float phi = phiDeg * MathF.PI / 180f;
                    Vector2 p1 = PlotToScreen(Vector2.Zero, panelRadius);
                    Vector2 p2 = PlotToScreen(new Vector2(
                        (float)Math.Cos(phi),
                        (float)Math.Sin(phi)), panelRadius);

                    gridVerts.Add(new VertexPositionColor(new Vector3(p1, 0), gridColor));
                    gridVerts.Add(new VertexPositionColor(new Vector3(p2, 0), gridColor));
                }
            }

 // ═══ 3. Równik ═══
            if (_chkSphereCircle != null && _chkSphereCircle.Checked)
            {
                Color equatorColor = new Color(100, 200, 255, 255);
                const int SEGMENTS = 128;

                for (int i = 0; i < SEGMENTS; i++)
                {
                    float a1 = (float)(i * 2 * Math.PI / SEGMENTS);
                    float a2 = (float)((i + 1) * 2 * Math.PI / SEGMENTS);

                    Vector2 p1 = PlotToScreen(new Vector2((float)Math.Cos(a1), (float)Math.Sin(a1)), panelRadius);
                    Vector2 p2 = PlotToScreen(new Vector2((float)Math.Cos(a2), (float)Math.Sin(a2)), panelRadius);

                    gridVerts.Add(new VertexPositionColor(new Vector3(p1, 0), equatorColor));
                    gridVerts.Add(new VertexPositionColor(new Vector3(p2, 0), equatorColor));
                }
            }

            _gridVB?.Dispose();
            if (gridVerts.Count > 0)
            {
                _gridVB = new VertexBuffer(_device, VertexPositionColor.VertexDeclaration,
                    gridVerts.Count, BufferUsage.WriteOnly);
                _gridVB.SetData(gridVerts.ToArray());
            }
            _gridVertCount = gridVerts.Count;

 // ═══ 4. Punkty ścian ═══
            var pointsVerts = new List<VertexPositionColor>();
            const int CIRCLE_POINT_SEGMENTS = 20;

            float rOuter = _pointSize / 2f;
            float rInner = rOuter * 0.55f;

            foreach (var kv in _plotPositions)
            {
                string faceName = kv.Key;
                Vector2 plotPos = kv.Value;
                Vector2 center = PlotToScreen(plotPos, panelRadius);

                Color pointColor = _plotColors.ContainsKey(faceName)
                    ? _plotColors[faceName]
                    : new Color(255, 255, 255, 255);

                bool isUpper = !_plotIsUpper.ContainsKey(faceName) || _plotIsUpper[faceName];

                if (isUpper)
                {
                    for (int i = 0; i < CIRCLE_POINT_SEGMENTS; i++)
                    {
                        float a1 = (float)(i * 2 * Math.PI / CIRCLE_POINT_SEGMENTS);
                        float a2 = (float)((i + 1) * 2 * Math.PI / CIRCLE_POINT_SEGMENTS);

                        Vector2 p1 = center + new Vector2(
                            rOuter * (float)Math.Cos(a1),
                            rOuter * (float)Math.Sin(a1));
                        Vector2 p2 = center + new Vector2(
                            rOuter * (float)Math.Cos(a2),
                            rOuter * (float)Math.Sin(a2));

                        pointsVerts.Add(new VertexPositionColor(new Vector3(center, 0), pointColor));
                        pointsVerts.Add(new VertexPositionColor(new Vector3(p1, 0), pointColor));
                        pointsVerts.Add(new VertexPositionColor(new Vector3(p2, 0), pointColor));
                    }
                }
                else
                {
                    for (int i = 0; i < CIRCLE_POINT_SEGMENTS; i++)
                    {
                        float a1 = (float)(i * 2 * Math.PI / CIRCLE_POINT_SEGMENTS);
                        float a2 = (float)((i + 1) * 2 * Math.PI / CIRCLE_POINT_SEGMENTS);

                        Vector2 outer1 = center + new Vector2(
                            rOuter * (float)Math.Cos(a1),
                            rOuter * (float)Math.Sin(a1));
                        Vector2 outer2 = center + new Vector2(
                            rOuter * (float)Math.Cos(a2),
                            rOuter * (float)Math.Sin(a2));
                        Vector2 inner1 = center + new Vector2(
                            rInner * (float)Math.Cos(a1),
                            rInner * (float)Math.Sin(a1));
                        Vector2 inner2 = center + new Vector2(
                            rInner * (float)Math.Cos(a2),
                            rInner * (float)Math.Sin(a2));

                        pointsVerts.Add(new VertexPositionColor(new Vector3(outer1, 0), pointColor));
                        pointsVerts.Add(new VertexPositionColor(new Vector3(outer2, 0), pointColor));
                        pointsVerts.Add(new VertexPositionColor(new Vector3(inner2, 0), pointColor));

                        pointsVerts.Add(new VertexPositionColor(new Vector3(outer1, 0), pointColor));
                        pointsVerts.Add(new VertexPositionColor(new Vector3(inner2, 0), pointColor));
                        pointsVerts.Add(new VertexPositionColor(new Vector3(inner1, 0), pointColor));
                    }
                }
            }

            _pointsVB?.Dispose();
            if (pointsVerts.Count > 0)
            {
                _pointsVB = new VertexBuffer(_device, VertexPositionColor.VertexDeclaration,
                    pointsVerts.Count, BufferUsage.WriteOnly);
                _pointsVB.SetData(pointsVerts.ToArray());
            }
            _pointsVertCount = pointsVerts.Count;
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

                            try
                            {
                                _device.Reset(pp);
                                _screenW = newW;
                                _screenH = newH;

                                _effect.Projection = Matrix.CreateOrthographicOffCenter(
                                    0, newW, newH, 0, 0, 1);

                                RebuildBuffers();
                            }
                            catch (Exception ex)
                            {
                                LogError($"[WulffPlot] Reset failed: {ex.Message}");
                                return;
                            }
                        }
                    }
                }

 // ⭐ Tło z Flagi
                _device.Clear(Flags.Instance.color1);

                _device.RasterizerState = RasterizerState.CullNone;
                _device.BlendState = BlendState.AlphaBlend;
                _device.DepthStencilState = DepthStencilState.None;

                DrawVertexBuffer(_gridVB, _gridVertCount, PrimitiveType.LineList);
                DrawVertexBuffer(_circleVB, _circleVertCount, PrimitiveType.LineList);
                DrawVertexBuffer(_pointsVB, _pointsVertCount, PrimitiveType.TriangleList);

                if (_chkLabels != null && _chkLabels.Checked && _spriteFont != null)
                {
                    DrawLabels();
                }

                _device.Present();
            }
            catch (Exception ex)
            {
                LogError($"[WulffPlot] Render error: {ex.Message}");
            }
        }

        private void DrawVertexBuffer(VertexBuffer vb, int count, PrimitiveType type)
        {
            if (vb == null || count == 0) return;

            _device.SetVertexBuffer(vb);
            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                int primCount = type == PrimitiveType.TriangleList
                    ? count / 3
                    : count / 2;

                _device.DrawPrimitives(type, 0, primCount);
            }
        }

        private void DrawLabels()
        {
            if (_spriteBatch == null || _spriteFont == null) return;

            var flags = Flags.Instance;
            float labelScale = Math.Max(0.2f, flags.LabelScale);
            Color labelColor = flags.LabelColor;
            bool shadow = flags.LabelShadow;
            Color shadowColor = flags.LabelShadowColor;

            float panelRadius = GetPanelRadius();

            bool batchBegun = false;
            try
            {
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                batchBegun = true;

                foreach (var kv in _plotPositions)
                {
                    string text = kv.Key;
                    Vector2 plotPos = kv.Value;
                    Vector2 screen = PlotToScreen(plotPos, panelRadius);

                    if (!IsTextSafeForFont(text, _spriteFont)) continue;

                    Vector2 textSize;
                    try { textSize = _spriteFont.MeasureString(text); }
                    catch { continue; }

                    if (textSize.X <= 0 || textSize.Y <= 0) continue;

                    Vector2 origin = textSize / 2f;
                    Vector2 pos = screen + new Vector2(0, -_pointSize * 0.4f - textSize.Y * 0.4f);

                    if (shadow)
                    {
                        _spriteBatch.DrawString(
                            _spriteFont, text,
                            pos + new Vector2(1, 1),
                            shadowColor,
                            0, origin, labelScale,
                            SpriteEffects.None, 0f);
                    }

                    _spriteBatch.DrawString(
                        _spriteFont, text,
                        pos,
                        labelColor,
                        0, origin, labelScale,
                        SpriteEffects.None, 0f);
                }
            }
            catch (Exception ex)
            {
                LogError($"[WulffPlot] Label draw error: {ex.Message}");
            }
            finally
            {
                if (batchBegun)
                {
                    try { _spriteBatch.End(); } catch { }
                }
            }
        }

        private bool IsTextSafeForFont(string text, SpriteFont font)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (font == null) return false;

            if (_cachedFontRef != font || _availableCharsCache == null)
            {
                try
                {
                    _availableCharsCache = new HashSet<char>(font.Characters);
                    _cachedFontRef = font;
                }
                catch { return false; }
            }

            foreach (char c in text)
                if (!_availableCharsCache.Contains(c)) return false;

            return true;
        }

        #endregion

        #region Cleanup

        private void CleanupAll()
        {
            _disposed = true;

            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;

            if (_doc3D != null)
            {
                try { _doc3D.CameraRotated -= OnCameraRotated; } catch { }
            }

            try { _circleVB?.Dispose(); } catch { }
            _circleVB = null;

            try { _gridVB?.Dispose(); } catch { }
            _gridVB = null;

            try { _pointsVB?.Dispose(); } catch { }
            _pointsVB = null;

            try { _spriteBatch?.Dispose(); } catch { }
            _spriteBatch = null;

            try { _pixelTexture?.Dispose(); } catch { }
            _pixelTexture = null;

            try { _content?.Dispose(); } catch { }
            _content = null;

            _spriteFont = null;

            try { _effect?.Dispose(); } catch { }
            _effect = null;

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