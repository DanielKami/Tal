// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal morphology / Wulff module: MorphologyDocument.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// Wersja: T-junction removal + rozszerzony debug afterzostałych single edges

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Tal.DrawMethods;
using Tal.Utils;
using WeifenLuo.WinFormsUI.Docking;
using Color = Microsoft.Xna.Framework.Color;
using GdiColor = System.Drawing.Color;

namespace Tal.Morphology
{
    public class MorphologyDocument : DockContent
    {
        #region Fields

        private Panel _panel;
        private Panel _controlsHost;
        private FlowLayoutPanel _controls;

        private GraphicsDevice _device;
        private BasicEffect _effect;
        private Camera _camera;

        private MorphologyCrystal _fullCrystal;
        private MorphologyCrystal _visibleCrystal;

        private Dictionary<string, Color> _originalFaceColors = new Dictionary<string, Color>();

        private System.Windows.Forms.Timer _timer;

        private VertexBuffer _triVB;
        private VertexBuffer _lineVB;
        private int _lineVertCount;

 // ⭐ Afterdświetlanie faces
        private VertexBuffer _highlightVB;
        private int _highlightVertCount;
        private string _highlightedFaceName = null;

        private bool _deviceReady = false;
        private bool _disposed = false;

        private TrackBar _trackAlpha;
        private CheckBox _chkEdges;
        private CheckBox _chkLabels;
        private CheckBox _chkMonochrome;
        private Panel _colorSwatch;

        private Label _lblAlphaValue;
        private Label _lblZoomInfo;

        private int _alphaValue = 220;
        private bool _showEdges = true;

        private float _baseBoundingRadius = 1.0f;
        private CrystalMorphologyResult _morphResult;

        private float _sceneScale = 1.0f;
        private float _zoomFactor = 1.0f;

        private bool _monochrome = false;
        private Color _monochromeColor = new Color(120, 180, 240, 220);
        private GdiColor _monochromeGdiColor = GdiColor.FromArgb(220, 120, 180, 240);

        private SpriteBatch _spriteBatch;
        private SpriteFont _spriteFont;
        private Texture2D _pixelTexture;
        private ContentManager _content;

        private HashSet<char> _availableCharsCache;
        private SpriteFont _cachedFontRef;

 // ⭐ DEBUG — licznik wywołań RebuildLineBuffer, so that nie spamować konsoli
        private int _lineDebugCounter = 0;

        #endregion

        #region Debug Helpers

        private static void LogInfo(string msg) => ProgressReporter.Log(msg, GdiColor.White);
        private static void LogWarn(string msg) => ProgressReporter.Log(msg, GdiColor.Orange);
        private static void LogError(string msg) => ProgressReporter.Log(msg, GdiColor.Red);
        private static void LogSuccess(string msg) => ProgressReporter.Log(msg, GdiColor.LightGreen);

        #endregion

        #region Public Events

        public event Action CameraRotated;

        #endregion

        #region Constructor
        private static GdiColor XnaToGdi(Color c)
        {
            return GdiColor.FromArgb(c.A, c.R, c.G, c.B);
        }
        public MorphologyDocument(string title = "Crystal Morphology 3D")
        {
            Text = title;
            DockAreas = DockAreas.Document | DockAreas.Float;
            BackColor = XnaToGdi(Flags.Instance.color1);

            _controlsHost = new Panel
            {
                Dock = DockStyle.Right,
                Width = 240,
                BackColor = GdiColor.FromArgb(245, 245, 250)
            };
            BuildControlsPanel();

            _panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = XnaToGdi(Flags.Instance.color1)
            };

            _panel.HandleCreated += (s, e) =>
            {
                if (!_deviceReady)
                    InitializeGraphics();
            };

            _panel.MouseDown += Panel_MouseDown;
            _panel.MouseMove += Panel_MouseMove;
            _panel.MouseUp += Panel_MouseUp;
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

        public MorphologyCrystal GetCrystal() => _fullCrystal;
        public Matrix GetRotationMatrix() => _camera?.Rotate ?? Matrix.Identity;

        public void SetSpriteFont(SpriteFont font)
        {
            System.Diagnostics.Debug.WriteLine(
                "[Doc] SetSpriteFont called — IGNORED (using local font)");
        }

 /// <summary>
 /// ⭐ Afterdświetla ścianę o afterdanej nazwie (np. "(100)"). null = wyłącz.
 /// Afterdświetla WSZYSTKIE faces o tej nazwie.
 /// </summary>
        public void HighlightFace(string faceName)
        {
            _highlightedFaceName = faceName;
            RebuildHighlightBuffer();
        }

        #endregion

        #region UI Builder

        private void BuildControlsPanel()
        {
            var flags = Flags.Instance;

            _controls = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8),
                BackColor = GdiColor.FromArgb(245, 245, 250)
            };

            _controls.Controls.Add(new Label
            {
                Text = "Morphology Display (Å)",
                Width = 220,
                Height = 24,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = GdiColor.FromArgb(40, 40, 60),
                Margin = new Padding(0, 0, 0, 8)
            });

            _controls.Controls.Add(MakeLabel("Alpha:"));
            _lblAlphaValue = MakeValueLabel("220 / 255");
            _controls.Controls.Add(_lblAlphaValue);
            _trackAlpha = new TrackBar
            {
                Minimum = 0,
                Maximum = 255,
                Value = _alphaValue,
                TickFrequency = 51,
                LargeChange = 32,
                SmallChange = 8,
                Width = 220,
                Height = 28,
                Margin = new Padding(0, 0, 0, 8)
            };
            _trackAlpha.Scroll += TrackAlpha_Scroll;
            _controls.Controls.Add(_trackAlpha);

            _chkEdges = new CheckBox
            {
                Text = "Show Edges",
                Checked = _showEdges,
                Width = 220,
                Height = 22,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 0, 0, 4)
            };
            _chkEdges.CheckedChanged += ChkEdges_CheckedChanged;
            _controls.Controls.Add(_chkEdges);

            _chkLabels = new CheckBox
            {
                Text = "Show Face Labels",
                Checked = flags.labels,
                Width = 220,
                Height = 22,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 0, 0, 4)
            };
            _chkLabels.CheckedChanged += (s, e) =>
            {
                flags.labels = _chkLabels.Checked;
                flags.MarkDirty();
                flags.Save();
            };
            _controls.Controls.Add(_chkLabels);

            var monoPanel = new Panel
            {
                Width = 220,
                Height = 26,
                Margin = new Padding(0, 4, 0, 8)
            };

            _chkMonochrome = new CheckBox
            {
                Text = "Monochrome",
                Checked = _monochrome,
                Location = new System.Drawing.Point(0, 2),
                Width = 110,
                Height = 22,
                Font = new Font("Segoe UI", 9F)
            };
            _chkMonochrome.CheckedChanged += ChkMonochrome_CheckedChanged;
            monoPanel.Controls.Add(_chkMonochrome);

            _colorSwatch = new Panel
            {
                Location = new System.Drawing.Point(120, 2),
                Width = 100,
                Height = 22,
                BackColor = _monochromeGdiColor,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            _colorSwatch.Click += ColorSwatch_Click;
            monoPanel.Controls.Add(_colorSwatch);

            _controls.Controls.Add(monoPanel);

            var btnReset = new Button
            {
                Text = "Reset View",
                Width = 220,
                Height = 28,
                BackColor = GdiColor.FromArgb(230, 230, 235),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Margin = new Padding(0, 0, 0, 8)
            };
            btnReset.Click += (s, e) => ResetView();
            _controls.Controls.Add(btnReset);

            _lblZoomInfo = new Label
            {
                Text = "Zoom: 1.00× (mouse wheel)",
                Width = 220,
                Height = 22,
                Font = new Font("Consolas", 9F, FontStyle.Bold),
                ForeColor = GdiColor.FromArgb(0, 90, 160),
                Margin = new Padding(0, 0, 0, 4),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _controls.Controls.Add(_lblZoomInfo);

            _controls.Controls.Add(new Label
            {
                Text = "Left mouse: rotate\r\nWheel: zoom",
                Width = 220,
                Height = 40,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = GdiColor.FromArgb(100, 100, 120),
                Margin = new Padding(0, 5, 0, 0)
            });

            _controlsHost.Controls.Add(_controls);
        }

        private Label MakeLabel(string text) => new Label
        {
            Text = text,
            Width = 220,
            Height = 18,
            Font = new Font("Segoe UI", 9F),
            Margin = new Padding(0, 5, 0, 0)
        };

        private Label MakeValueLabel(string text) => new Label
        {
            Text = text,
            Width = 220,
            Height = 18,
            Font = new Font("Consolas", 9F, FontStyle.Bold),
            ForeColor = GdiColor.FromArgb(0, 90, 160),
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0)
        };

        #endregion

        #region UI Events

        private void TrackAlpha_Scroll(object sender, EventArgs e)
        {
            _alphaValue = _trackAlpha.Value;
            _lblAlphaValue.Text = $"{_alphaValue} / 255";
            ApplyAlpha();
            RebuildTriangleBuffer();
        }

        private void ChkEdges_CheckedChanged(object sender, EventArgs e)
        {
            _showEdges = _chkEdges.Checked;
            RebuildLineBuffer();
        }

        private void ChkMonochrome_CheckedChanged(object sender, EventArgs e)
        {
            _monochrome = _chkMonochrome.Checked;
            ApplyAlpha();
            RebuildTriangleBuffer();
        }

        private void ColorSwatch_Click(object sender, EventArgs e)
        {
            using var dlg = new ColorDialog();
            dlg.Color = _monochromeGdiColor;
            dlg.FullOpen = true;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _monochromeGdiColor = dlg.Color;
                _monochromeColor = new Color(
                    dlg.Color.R, dlg.Color.G, dlg.Color.B, (byte)_alphaValue);

                _colorSwatch.BackColor = dlg.Color;

                if (!_chkMonochrome.Checked)
                    _chkMonochrome.Checked = true;
                else
                {
                    ApplyAlpha();
                    RebuildTriangleBuffer();
                }
            }
        }

        #endregion

        #region Public API — Crystal

        public void SetCrystalFromResult(CrystalMorphologyResult result, string structureName)
        {
            _morphResult = result;
            Text = $"Crystal: {structureName}";
            RebuildCrystalFromResult();
            if (_camera != null) ResetView();
        }

        public void SetCrystal(MorphologyCrystal crystal)
        {
            _fullCrystal = crystal;
            _baseBoundingRadius = crystal?.BoundingRadius ?? 1f;

            _originalFaceColors.Clear();
            if (_fullCrystal != null)
            {
                foreach (var t in _fullCrystal.Triangles)
                {
                    if (!string.IsNullOrEmpty(t.FaceName))
                        _originalFaceColors[t.FaceName] = t.FaceColor;
                }
            }

            RebuildVisibleCrystal();

            if (_deviceReady && _visibleCrystal != null && _visibleCrystal.Triangles.Count > 0)
            {
                RebuildTriangleBuffer();
                RebuildLineBuffer();
                RebuildHighlightBuffer();
            }

            if (_camera != null && crystal != null) ResetView();
        }

        public void RefreshView() => RenderFrame();

        public void ResetView()
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
                _lblZoomInfo.Text = "Zoom: 1.00× (mouse wheel)";

            RebuildTriangleBuffer();
            RebuildLineBuffer();
            RebuildHighlightBuffer();

            CameraRotated?.Invoke();
        }

        #endregion

        #region Crystal Reconstruction

        private void RebuildCrystalFromResult()
        {
            if (_morphResult == null)
            {
                LogWarn("[Doc] Brak _morphResult → SimpleTest");
                SetCrystal(MorphologyCrystal.BuildSimpleTest());
                return;
            }

            var newCrystal = MorphologyCrystal.Build(_morphResult, scale: 1.0f, faceOffset: 1.0f);
            SetCrystal(newCrystal);
        }

        private Color GetFaceColor(string faceName, Color fallbackColor)
        {
            Color baseColor;

            if (_monochrome)
            {
                baseColor = _monochromeColor;
            }
            else if (!string.IsNullOrEmpty(faceName) &&
                     _originalFaceColors.TryGetValue(faceName, out var orig))
            {
                baseColor = orig;
            }
            else
            {
                baseColor = fallbackColor;
            }

            return new Color(baseColor.R, baseColor.G, baseColor.B, (byte)_alphaValue);
        }

        private void RebuildVisibleCrystal()
        {
            if (_fullCrystal == null)
            {
                _visibleCrystal = null;
                _triVB = null;
                _lineVB = null;
                _highlightVB = null;
                return;
            }

            _visibleCrystal = new MorphologyCrystal
            {
                BoundingRadius = _baseBoundingRadius
            };

            foreach (var t in _fullCrystal.Triangles)
            {
                _visibleCrystal.Triangles.Add(new MorphologyCrystal.Tri
                {
                    A = t.A,
                    B = t.B,
                    C = t.C,
                    Normal = t.Normal,
                    FaceName = t.FaceName,
                    FaceArea = t.FaceArea,
                    FaceColor = GetFaceColor(t.FaceName, t.FaceColor)
                });
            }

            if (_deviceReady)
            {
                RebuildTriangleBuffer();
                RebuildLineBuffer();
                RebuildHighlightBuffer();
            }
        }

        private void ApplyAlpha()
        {
            if (_visibleCrystal == null) return;

            foreach (var t in _visibleCrystal.Triangles)
            {
                t.FaceColor = GetFaceColor(t.FaceName, t.FaceColor);
            }
        }

        #endregion

        #region Graphics Init

        private void InitializeGraphics()
        {
            if (_deviceReady) return;

            try { _spriteBatch?.Dispose(); } catch { }
            _spriteBatch = null;
            try { _pixelTexture?.Dispose(); } catch { }
            _pixelTexture = null;
            try { _effect?.Dispose(); } catch { }
            _effect = null;
            try { _triVB?.Dispose(); } catch { }
            _triVB = null;
            try { _lineVB?.Dispose(); } catch { }
            _lineVB = null;
            try { _highlightVB?.Dispose(); } catch { }
            _highlightVB = null;
            try { _content?.Dispose(); } catch { }
            _content = null;

            _spriteFont = null;
            _availableCharsCache = null;
            _cachedFontRef = null;

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
                    TextureEnabled = false
                };

                _spriteBatch = new SpriteBatch(_device);

                _pixelTexture = new Texture2D(_device, 1, 1);
                _pixelTexture.SetData(new[] { Color.White });

                LoadLocalSpriteFont();

                _camera = new Camera();
                _camera.GraphicsDevice = _device;
                _camera.Initialize();
                _camera.g_cameraPos = new Vector3(0, 0, 100);
                _camera.g_targetPos = Vector3.Zero;
                _camera.zoom = 0.5f;
                _camera.OneOverZoom = 2.0f;
                _camera.ApplyEffects();

                _deviceReady = true;

                LogSuccess($"[Doc] ✅ Graphics initialized ({w}x{h})");

                if (_visibleCrystal != null && _visibleCrystal.Triangles.Count > 0)
                {
                    RebuildTriangleBuffer();
                    RebuildLineBuffer();
                    RebuildHighlightBuffer();
                }
            }
            catch (Exception ex)
            {
                LogError($"[Doc] ❌ Init error: {ex.Message}");
                MessageBox.Show($"Graphics init error: {ex.Message}",
                    "Morphology 3D", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLocalSpriteFont()
        {
            if (_device == null) return;

            string fontFile = null;

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? baseDir;

 // ⭐ Kolejność: najpierw katalog programu (dystrybucja — plik obok .exe),
 // then Content/, then dev (../../../Content).
                string[] candidates = new[]
                {
 // 1. Withoutafterśrednio obok .exe / w katalogu roboczym (dystrybucja)
            Path.Combine(exeDir, "SpriteFont.xnb"),
            Path.Combine(baseDir, "SpriteFont.xnb"),

 // 2. W afterdfolderze Content (dystrybucja or dev)
            Path.Combine(exeDir, "Content", "SpriteFont.xnb"),
            Path.Combine(baseDir, "Content", "SpriteFont.xnb"),

 // 3. Dev — bin/Debug → ../../../Content
            Path.Combine(exeDir, "..", "..", "..", "Content", "SpriteFont.xnb"),
            Path.Combine(baseDir, "..", "..", "..", "Content", "SpriteFont.xnb"),
        };

                foreach (var candidate in candidates)
                {
                    try
                    {
                        if (File.Exists(candidate))
                        {
                            fontFile = Path.GetFullPath(candidate);
                            break;
                        }
                    }
                    catch { }
                }

                if (fontFile == null)
                {
                    LogError("[Doc] ❌ Cannot find SpriteFont1.xnb (looked next to .exe and in Content/)");
                    _spriteFont = null;
                    return;
                }

 // ⭐ ContentManager aftertrzebuje katalogu bazowego i nazwy zasobu BEZ .xnb.
                string contentDir = Path.GetDirectoryName(fontFile);
                string assetName = Path.GetFileNameWithoutExtension(fontFile);   // "SpriteFont1"

                var services = new ServiceContainer();
                services.AddService<IGraphicsDeviceService>(
                    new SimpleGraphicsDeviceService(_device));

                _content = new ContentManager(services, contentDir);
                _spriteFont = _content.Load<SpriteFont>(assetName);

                LogSuccess($"[Doc] ✅ Local SpriteFont loaded from: {fontFile}");
            }
            catch (Exception ex)
            {
                LogError($"[Doc] ❌ SpriteFont load failed: {ex.Message}");
                _spriteFont = null;
            }
        }

        #endregion

        #region Buffer Building

        private float ComputeSceneScale()
        {
            float r = Math.Max(_visibleCrystal?.BoundingRadius ?? 1f, 0.1f);
            return 40f / r;
        }

 /// <summary>
 /// ⭐ Stabilny identyfikator faces (strona kryształu).
 /// Używamy TYLKO FaceName — without znaku normalnej.
 /// </summary>
        private static string GetFaceId(MorphologyCrystal.Tri tri, int triIndex)
        {
            if (!string.IsNullOrEmpty(tri.FaceName))
                return tri.FaceName;

            Vector3 n = tri.Normal;
            if (n.LengthSquared() < 1e-9f)
                return $"__null_{triIndex}";

            int sx = n.X > 0.5f ? 1 : (n.X < -0.5f ? -1 : 0);
            int sy = n.Y > 0.5f ? 1 : (n.Y < -0.5f ? -1 : 0);
            int sz = n.Z > 0.5f ? 1 : (n.Z < -0.5f ? -1 : 0);

            return $"__null|{sx},{sy},{sz}";
        }

        private void RebuildTriangleBuffer()
        {
            if (_visibleCrystal == null || _device == null || _device.IsDisposed) return;

            int n = _visibleCrystal.Triangles.Count;
            if (n == 0)
            {
                _triVB = null;
                return;
            }

            _sceneScale = ComputeSceneScale();
            float zoom = _zoomFactor;

            var verts = new VertexPositionColor[n * 3];
            for (int i = 0; i < n; i++)
            {
                var t = _visibleCrystal.Triangles[i];
                int b = i * 3;
                verts[b + 0] = new VertexPositionColor(t.A * _sceneScale * zoom, t.FaceColor);
                verts[b + 1] = new VertexPositionColor(t.B * _sceneScale * zoom, t.FaceColor);
                verts[b + 2] = new VertexPositionColor(t.C * _sceneScale * zoom, t.FaceColor);
            }

            _triVB?.Dispose();
            _triVB = new VertexBuffer(_device, VertexPositionColor.VertexDeclaration,
                                       verts.Length, BufferUsage.WriteOnly);
            _triVB.SetData(verts);
        }

 /// <summary>
 /// ⭐ Rysowanie edges — only granice between RÓŻNYMI ścianami.
 /// ⭐ T-junction removal + rozszerzony DEBUG afterzostałych single edges.
 /// </summary>
        private void RebuildLineBuffer()
        {
            if (_visibleCrystal == null || _device == null || _device.IsDisposed) return;

            int n = _visibleCrystal.Triangles.Count;
            if (n == 0 || !_showEdges)
            {
                _lineVB = null;
                _lineVertCount = 0;
                return;
            }

            float zoom = _zoomFactor;
            if (_sceneScale <= 0) _sceneScale = ComputeSceneScale();

 // ⭐ EPS w ORYGINALNEJ skali kryształu (Å) — NIE zależy od zoom.
            float crystalRadius = Math.Max(_visibleCrystal.BoundingRadius, 0.1f);
            float EPS_world = crystalRadius * 1e-4f;   // slightly looser than 5e-4

 // ═══ KROK 1: Zbierz unikalne vertices W ORYGINALNEJ SKALI ═══
            var vertexLookup = new Dictionary<(long, long, long), List<int>>(n * 3);
            var vertexPositions = new List<Vector3>(n * 3);   // w oryginalnej skali (Å)
            var triVertexIdx = new (int a, int b, int c)[n];

            long VKeyAxis(float x) => (long)Math.Floor(x / EPS_world);

            int GetVertexIndex(Vector3 v)
            {
                long ix = VKeyAxis(v.X);
                long iy = VKeyAxis(v.Y);
                long iz = VKeyAxis(v.Z);

 // ⭐ Check 27 neighbouring buckets — klasyczny bug z granicą.
                for (long dx = -1; dx <= 1; dx++)
                    for (long dy = -1; dy <= 1; dy++)
                        for (long dz = -1; dz <= 1; dz++)
                        {
                            var key = (ix + dx, iy + dy, iz + dz);
                            if (!vertexLookup.TryGetValue(key, out var list)) continue;

                            foreach (int idx in list)
                            {
                                if ((vertexPositions[idx] - v).LengthSquared() < EPS_world * EPS_world)
                                    return idx;
                            }
                        }

                int newIdx = vertexPositions.Count;
                vertexPositions.Add(v);
                var myKey = (ix, iy, iz);
                if (!vertexLookup.TryGetValue(myKey, out var myList))
                {
                    myList = new List<int>(2);
                    vertexLookup[myKey] = myList;
                }
                myList.Add(newIdx);
                return newIdx;
            }

 // ⭐ Łącz vertices w ORYGINALNEJ skali — without mnożenia przez sceneScale/zoom.
            for (int i = 0; i < n; i++)
            {
                var t = _visibleCrystal.Triangles[i];
                triVertexIdx[i] = (
                    GetVertexIndex(t.A),
                    GetVertexIndex(t.B),
                    GetVertexIndex(t.C)
                );
            }

 // ⭐ Dopiero teraz scale do pixeli — do drawing.
            float s = _sceneScale * zoom;
            var pixelPositions = new Vector3[vertexPositions.Count];
            for (int i = 0; i < vertexPositions.Count; i++)
                pixelPositions[i] = vertexPositions[i] * s;

 // ═══ KROK 1.5: T-junction removal ═══
            (int, int) EdgeKey(int ia, int ib) => ia < ib ? (ia, ib) : (ib, ia);

            var rawEdges = new HashSet<(int, int)>();
            for (int i = 0; i < n; i++)
            {
                var (ia, ib, ic) = triVertexIdx[i];
                rawEdges.Add(EdgeKey(ia, ib));
                rawEdges.Add(EdgeKey(ib, ic));
                rawEdges.Add(EdgeKey(ic, ia));
            }

 // ⭐ EPS_COLLINEAR względne — nie stałe. Prosta: EPS_world * 4 (w Å).
            float EPS_COLLINEAR_SQ = (EPS_world * 4f) * (EPS_world * 4f);

            var splitMap = new Dictionary<(int, int), List<int>>();

            foreach (var (v1, v2) in rawEdges)
            {
                Vector3 p1 = vertexPositions[v1];   // ORYGINALNA skala
                Vector3 p2 = vertexPositions[v2];
                Vector3 dir = p2 - p1;
                float len2 = dir.LengthSquared();

                if (len2 < 1e-20f) continue;

 // ⭐ EPS for parameteru t — w jednostkach t.
                float tEps = (float)Math.Sqrt(EPS_world * EPS_world / len2) * 4f;

                var onEdge = new List<(float t, int idx)>();

                for (int v = 0; v < vertexPositions.Count; v++)
                {
                    if (v == v1 || v == v2) continue;

                    Vector3 p = vertexPositions[v];
                    float t = Vector3.Dot(p - p1, dir) / len2;

                    if (t <= tEps || t >= 1f - tEps) continue;

                    Vector3 proj = p1 + dir * t;
                    float dist2 = (p - proj).LengthSquared();

                    if (dist2 < EPS_COLLINEAR_SQ)
                        onEdge.Add((t, v));
                }

                if (onEdge.Count > 0)
                {
                    onEdge.Sort((a, b) => a.t.CompareTo(b.t));
                    var sorted = new List<int> { v1 };
                    foreach (var (_, idx) in onEdge) sorted.Add(idx);
                    sorted.Add(v2);
                    splitMap[(v1, v2)] = sorted;
                }
            }

 // ═══ KROK 2: Mapa edges ═══
            var edgeToFaces = new Dictionary<(int, int), List<string>>(n * 3);

            for (int i = 0; i < n; i++)
            {
                var (ia, ib, ic) = triVertexIdx[i];
                string fid = GetFaceId(_visibleCrystal.Triangles[i], i);

                void AddEdge(int v1, int v2)
                {
                    var key = EdgeKey(v1, v2);
                    List<int> chain;
                    if (splitMap.TryGetValue(key, out var split))
                    {
                        chain = new List<int>(split);
                        if (key.Item1 != v1) chain.Reverse();
                    }
                    else chain = new List<int> { v1, v2 };

                    for (int k = 0; k < chain.Count - 1; k++)
                    {
                        var segKey = EdgeKey(chain[k], chain[k + 1]);
                        if (!edgeToFaces.TryGetValue(segKey, out var list))
                        {
                            list = new List<string>(2);
                            edgeToFaces[segKey] = list;
                        }
                        if (!list.Contains(fid))
                            list.Add(fid);
                    }
                }

                AddEdge(ia, ib);
                AddEdge(ib, ic);
                AddEdge(ic, ia);
            }

 // ═══ KROK 3: Zbierz krawędzie graniczne ═══
            var lineVerts = new List<VertexPositionColor>(n);
            Color lc = new Color(255, 255, 255, 240);
            var drawnEdges = new HashSet<(int, int)>();

            for (int i = 0; i < n; i++)
            {
                var (ia, ib, ic) = triVertexIdx[i];
                string fid = GetFaceId(_visibleCrystal.Triangles[i], i);

                void Check(int v1, int v2)
                {
                    var key = EdgeKey(v1, v2);
                    List<int> chain;
                    if (splitMap.TryGetValue(key, out var split))
                    {
                        chain = new List<int>(split);
                        if (key.Item1 != v1) chain.Reverse();
                    }
                    else chain = new List<int> { v1, v2 };

                    for (int k = 0; k < chain.Count - 1; k++)
                    {
                        var segKey = EdgeKey(chain[k], chain[k + 1]);

                        if (drawnEdges.Contains(segKey)) continue;
                        if (!edgeToFaces.TryGetValue(segKey, out var faces)) continue;

                        bool isBoundary = false;
                        foreach (var other in faces)
                        {
                            if (other != fid) { isBoundary = true; break; }
                        }

                        if (isBoundary)
                        {
                            drawnEdges.Add(segKey);
 // ⭐ Draw z pixelAftersitions (already przeskalowane), nie z vertexAftersitions.
                            lineVerts.Add(new VertexPositionColor(pixelPositions[chain[k]], lc));
                            lineVerts.Add(new VertexPositionColor(pixelPositions[chain[k + 1]], lc));
                        }
                    }
                }

                Check(ia, ib);
                Check(ib, ic);
                Check(ic, ia);
            }

            if (lineVerts.Count == 0)
            {
                _lineVB = null;
                _lineVertCount = 0;
                return;
            }

            _lineVertCount = lineVerts.Count;
            _lineVB?.Dispose();
            _lineVB = new VertexBuffer(_device, VertexPositionColor.VertexDeclaration,
                                        lineVerts.Count, BufferUsage.WriteOnly);
            _lineVB.SetData(lineVerts.ToArray());
        }

 /// <summary>
 /// ⭐ Builds buffer żółtych obwódek for afterdświetlonej faces.
 /// </summary>
        private void RebuildHighlightBuffer()
        {
            if (_visibleCrystal == null || _device == null || _device.IsDisposed)
            {
                _highlightVB = null;
                _highlightVertCount = 0;
                return;
            }

            if (string.IsNullOrEmpty(_highlightedFaceName))
            {
                _highlightVB = null;
                _highlightVertCount = 0;
                return;
            }

            int n = _visibleCrystal.Triangles.Count;
            if (n == 0) return;

            float zoom = _zoomFactor;
            if (_sceneScale <= 0) _sceneScale = ComputeSceneScale();

            float crystalRadius = Math.Max(_visibleCrystal.BoundingRadius, 0.1f);
            float EPS_world = crystalRadius * 5e-4f;
            float scaleFactor = Math.Max(_sceneScale * zoom, 1e-6f);
            float EPS = EPS_world / scaleFactor;

            var vertexLookup = new Dictionary<(long, long, long), int>(n * 3);
            var vertexPositions = new List<Vector3>(n * 3);
            var localTriIdx = new List<(int a, int b, int c)>(n);

            (long, long, long) VKey(Vector3 v)
                => ((long)Math.Round(v.X / EPS),
                    (long)Math.Round(v.Y / EPS),
                    (long)Math.Round(v.Z / EPS));

            int GetVertexIndex(Vector3 v)
            {
                var key = VKey(v);
                if (vertexLookup.TryGetValue(key, out int idx))
                    return idx;

                int newIdx = vertexPositions.Count;
                vertexPositions.Add(v);
                vertexLookup[key] = newIdx;
                return newIdx;
            }

            for (int i = 0; i < n; i++)
            {
                var t = _visibleCrystal.Triangles[i];
                if (t.FaceName != _highlightedFaceName) continue;

                Vector3 a = t.A * _sceneScale * zoom;
                Vector3 b = t.B * _sceneScale * zoom;
                Vector3 c = t.C * _sceneScale * zoom;

                localTriIdx.Add((
                    GetVertexIndex(a),
                    GetVertexIndex(b),
                    GetVertexIndex(c)
                ));
            }

            if (localTriIdx.Count == 0)
            {
                _highlightVB = null;
                _highlightVertCount = 0;
                return;
            }

            var edgeSet = new HashSet<(int, int)>();
            var lineVerts = new List<VertexPositionColor>();
            Color yellow = new Color(255, 255, 0, 255);

            (int, int) EdgeKey(int ia, int ib)
                => ia < ib ? (ia, ib) : (ib, ia);

            foreach (var (ia, ib, ic) in localTriIdx)
            {
                void Add(int v1, int v2)
                {
                    var key = EdgeKey(v1, v2);
                    if (edgeSet.Add(key))
                    {
                        lineVerts.Add(new VertexPositionColor(vertexPositions[v1], yellow));
                        lineVerts.Add(new VertexPositionColor(vertexPositions[v2], yellow));
                    }
                }

                Add(ia, ib);
                Add(ib, ic);
                Add(ic, ia);
            }

            if (lineVerts.Count == 0)
            {
                _highlightVB = null;
                _highlightVertCount = 0;
                return;
            }

            _highlightVertCount = lineVerts.Count;

            _highlightVB?.Dispose();
            _highlightVB = new VertexBuffer(_device, VertexPositionColor.VertexDeclaration,
                                             lineVerts.Count, BufferUsage.WriteOnly);
            _highlightVB.SetData(lineVerts.ToArray());
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

                            if (_camera != null)
                            {
                                _camera.GraphicsDevice = _device;
                                _camera.ApplyEffects();
                            }
                        }
                    }
                }

                _camera?.ApplyEffects();

                _device.Clear(Flags.Instance.color1);

                if (_visibleCrystal == null || _triVB == null) return;

                float aspect = _device.Viewport.AspectRatio;
                if (aspect < 0.001f) aspect = 1f;

                Matrix world =
                    _camera.Rotate *
                     _camera.Translate;

                Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 100), Vector3.Zero, Vector3.Up);

                bool usePerspective = Flags.Instance.PerspectiveMode;

                Matrix proj;
                if (usePerspective)
                {
                    proj = Matrix.CreatePerspectiveFieldOfView(
                        MathHelper.PiOver4, aspect, 0.1f, 10000f);
                }
                else
                {
                    float halfHeight = 40f;
                    float halfWidth = halfHeight * aspect;

                    proj = Matrix.CreateOrthographic(
                        halfWidth * 2, halfHeight * 2, 0.1f, 10000f);
                }

                _effect.World = world;
                _effect.View = view;
                _effect.Projection = proj;

                _device.BlendState = BlendState.AlphaBlend;
                _device.DepthStencilState = DepthStencilState.Default;
                _device.RasterizerState = RasterizerState.CullNone;

                _device.SetVertexBuffer(_triVB);

                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _device.DrawPrimitives(PrimitiveType.TriangleList, 0, _triVB.VertexCount / 3);
                }

                if (_showEdges && _lineVB != null && _lineVertCount > 0)
                {
                    _device.SetVertexBuffer(_lineVB);
                    foreach (var pass in _effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _device.DrawPrimitives(PrimitiveType.LineList, 0, _lineVertCount / 2);
                    }
                }

                if (_highlightVB != null && _highlightVertCount > 0)
                {
                    _device.DepthStencilState = DepthStencilState.None;

                    _device.SetVertexBuffer(_highlightVB);
                    foreach (var pass in _effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        _device.DrawPrimitives(PrimitiveType.LineList, 0, _highlightVertCount / 2);
                    }

                    _device.DepthStencilState = DepthStencilState.Default;
                }

                if (Flags.Instance.labels)
                {
                    DrawFaceLabels(world, view, proj);
                }

                _device.Present();
            }
            catch (Exception ex)
            {
                LogError($"[Doc] ❌ Render error: {ex.Message}");
            }
        }

        private void DrawFaceLabels(Matrix world, Matrix view, Matrix proj)
        {
            if (_spriteBatch == null || _spriteFont == null || _visibleCrystal == null)
                return;

            var flags = Flags.Instance;

            var faceSums = new Dictionary<string, (Vector3 sum, int count, string label)>();

            for (int i = 0; i < _visibleCrystal.Triangles.Count; i++)
            {
                var t = _visibleCrystal.Triangles[i];
                if (string.IsNullOrEmpty(t.FaceName)) continue;

                string key = GetFaceId(t, i);

                Vector3 center = (t.A + t.B + t.C) / 3f;

                if (faceSums.TryGetValue(key, out var acc))
                    faceSums[key] = (acc.sum + center, acc.count + 1, t.FaceName);
                else
                    faceSums[key] = (center, 1, t.FaceName);
            }

            float zoom = _zoomFactor;

            Matrix fullMatrix =
                Matrix.CreateScale(_sceneScale * zoom, _sceneScale * zoom, _sceneScale * zoom) *
                world * view * proj;

            int halfW = _device.Viewport.Width / 2;
            int halfH = _device.Viewport.Height / 2;

            var toDraw = new List<(string text, Vector2 pos, float depth)>();

            foreach (var kv in faceSums)
            {
                string label = kv.Value.label;
                Vector3 faceCenter = kv.Value.sum / kv.Value.count;

                Matrix wvp = Matrix.CreateTranslation(faceCenter) * fullMatrix;

                float w = wvp.M44;
                if (float.IsNaN(w) || w <= 0.001f) continue;

                float screenX = halfW + wvp.M41 / w * halfW;
                float screenY = halfH - wvp.M42 / w * halfH;
                float depth = wvp.M43 / w;

                if (float.IsNaN(screenX) || float.IsInfinity(screenX)) continue;
                if (float.IsNaN(screenY) || float.IsInfinity(screenY)) continue;

                toDraw.Add((label, new Vector2(screenX, screenY), depth));
            }

            if (toDraw.Count == 0) return;

            toDraw.Sort((a, b) => b.depth.CompareTo(a.depth));

            float labelScale = Math.Max(0.1f, flags.LabelScale);
            if (float.IsNaN(labelScale) || float.IsInfinity(labelScale))
                labelScale = 1.0f;

            bool shadow = flags.LabelShadow;
            Color shadowColor = flags.LabelShadowColor;
            Color labelColor = flags.LabelColor;

            bool batchBegun = false;
            try
            {
                _spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                batchBegun = true;

                foreach (var (text, pos, _) in toDraw)
                {
                    if (string.IsNullOrEmpty(text)) continue;
                    if (!IsTextSafeForFont(text, _spriteFont)) continue;
                    if (float.IsNaN(pos.X) || float.IsNaN(pos.Y)) continue;

                    Vector2 textSize;
                    try { textSize = _spriteFont.MeasureString(text); }
                    catch { continue; }

                    if (float.IsNaN(textSize.X) || textSize.X <= 0) continue;

                    Vector2 origin = textSize / 2f;

                    if (shadow)
                    {
                        _spriteBatch.DrawString(_spriteFont, text,
                            pos + new Vector2(1, 1), shadowColor,
                            0, origin, labelScale, SpriteEffects.None, 0f);
                    }

                    _spriteBatch.DrawString(_spriteFont, text,
                        pos, labelColor,
                        0, origin, labelScale, SpriteEffects.None, 0f);
                }
            }
            catch (Exception ex)
            {
                LogError($"[DrawFaceLabels] Error: {ex.Message}");
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

            if (_availableCharsCache.Count == 0) return false;

            foreach (char c in text)
                if (!_availableCharsCache.Contains(c)) return false;

            return true;
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
                CameraRotated?.Invoke();
            }
        }

        private void Panel_MouseUp(object sender, MouseEventArgs e)
        {
            _camera?.MouseUp(e);
            CameraRotated?.Invoke();
        }

        private void Panel_MouseWheel(object sender, MouseEventArgs e)
        {
            float delta = e.Delta / 120f;

            if (delta > 0) _zoomFactor *= 1.15f;
            else _zoomFactor *= 0.87f;

            _zoomFactor = Math.Clamp(_zoomFactor, 0.2f, 5.0f);

            if (_lblZoomInfo != null)
                _lblZoomInfo.Text = $"Zoom: {_zoomFactor:F2}× (mouse wheel)";

            RebuildTriangleBuffer();
            RebuildLineBuffer();
            RebuildHighlightBuffer();
        }

        #endregion

        #region Cleanup

        private void CleanupAll()
        {
            _disposed = true;

            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;

            try { _triVB?.Dispose(); } catch { }
            _triVB = null;

            try { _lineVB?.Dispose(); } catch { }
            _lineVB = null;

            try { _highlightVB?.Dispose(); } catch { }
            _highlightVB = null;

            try { _spriteBatch?.Dispose(); } catch { }
            _spriteBatch = null;

            try { _pixelTexture?.Dispose(); } catch { }
            _pixelTexture = null;

            try { _content?.Dispose(); } catch { }
            _content = null;

            _spriteFont = null;
            _availableCharsCache = null;
            _cachedFontRef = null;

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