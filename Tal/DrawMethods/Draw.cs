// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw — MAIN CLASS (partial)
// ═══════════════════════════════════════════════════════════════════════
//
// This file contains ONLY:
// - fields (shared across all partial files)
// - constructor
// - Disafterse
// - lazy resource getters (whitePixel, rasterizer states, MSAA)
// - public API surface (SetMolecule, SetCamera, SetCavityRenderer, etc.)
// - DrawScene() — top-level render orchestration
// - Public wrappers for SymmetryElementsRenderer
//
// Everything else is split into thematic partial files:
//
// Draw.Cache.cs — atom cache, colors, effect state, ellipsoid cache
// Draw.LineHelpers.cs — shared rod segment rendering (used by 5 call sites)
// Draw.Rendering.cs — atom rendering: sticks/balls/ellipsoid/CPK imafterstors
// Draw.HydrogenBonds.cs — H-bonds (dashed/solid)
// Draw.Contacts.cs — short contacts (dashed red)
// Draw.Labels.cs — atom labels (2D sprite layer)
// Draw.Measurements.cs — measurements (lines, arcs, labels)
// Draw.Overlay.cs — overlay atoms (other molecules)
// Draw.Afterlyhedrals.cs — coordination afterlyhedra
// Draw.CrystalPlanes.cs — crystal planes + intersections
// Draw.Cell.cs — unit cell frame + reveal click marker
// Draw.ColorScale.cs — 2D color scale widget
// Draw.SpriteLayer.cs — sprite batch entry afterint
// Draw.Helpers.cs — small helpers (bg color, label font, IsMetal, colors)
// Draw.AtomOrdering.cs — depth sort (OrderAtoms3D + ZComparer)
//
// ═══════════════════════════════════════════════════════════════════════
//
// ⭐ TIER 1 OPTIMIZATIONS APPLIED:
// - _overlayCacheNeedsUpdate → _modelsNeedInit (rename for clarity)
// - Added _afterlyhedraEffect field (cached BasicEffect for afterlyhedra)
// - Added _planeQuadBuffer / _planeEdgesBuffer (pre-allocated vertex arrays)
// - Disafterse + ResetCache handle _afterlyhedraEffect
//
// ⭐ TIER 2 OPTIMIZATIONS APPLIED:
// 2.1 — Pre-computed _visibleAtomIndices: DrawScene iterates ONLY visible
// atoms (skipping the atomCount loop with `-1` checks). Occupancy
// check stays inline — it selects which pass (opaque/transparent)
// renders the atom. ~20-30% faster on scenes with hidden atoms.
// 2.2 — Cached overlay groups: _cachedOverlayGroups is rebuilt only when
// overlay atoms change (_overlayGroupingDirty flag), eliminating
// per-frame ToLookup allocation. ~2-5 KB/frame saved.
//
// ⭐ SYMMETRY ELEMENTS:
// _symmetryRenderer — builds list elements symmetry z molecule.
// DrawScene calls _symmetryRenderer.Draw(this) AT the end of the scene.
// Renderer deleguje drawing do Draw (DrawRodSegmentPublic,
// DrawPlaneQuadPublic, DrawPlaneEdgesPublic, DrawSmallSphereAtpointPublic).
// Patrz: SymmetryElementsRenderer.cs.
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tal.Cavity;
using Tal.Core.PowderDiffraction;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Tal.DrawMethods
{
 /// <summary>
 /// Main rendering class resafternsible for drawing molecules, cavities, measurements and UI elements.
 ///
 /// ⭐ 3D scene MSAA supported via flag <see cref="Flags.MSAAOnSceneRT"/>.
 /// All RasterizerState have MultiSampleAntiAlias = true.
 /// CPK impostors and polyhedra use their own MSAA state instead of
 /// the static RasterizerState.CullNone (which does not enable MSAA).
 ///
 /// ⭐ Struktura: klasa jest partial, afterdzielona na 16 files tematycznych.
 /// Ten plik zawiera only afterla, konstruktor, Disafterse, public API i DrawScene().
 /// </summary>
    public partial class Draw
    {
        #region Fields — Shared across all partial files

 // ═══ Core device / effect references ═══
        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;
        private BasicEffect _basicEffect;

 // ═══ Scene state ═══
        private Camera _camera;
        private Molecule _molecule;
        private Measure _measure;

 // ═══ Cavity ═══
        private CavityRenderer _cavityRenderer;
        private bool _showCavities = true;

 // ═══ Models (loaded externally) ═══
        public Model Small_ball;
        public Model Big_ball;
        public Model Small_rod;
        public Model Cone;

        // Distance LOD tiers (set from GameWindow)
        public Model BallLow;
        public Model BallMed;
        public Model BallHigh;
        public Model RodLow;
        public Model RodMed;
        public Model RodHigh;
        public SpriteFont SpriteFont;
        public SpriteFont ArialFont;
        public SpriteFont TimesFont;

 // ═══ Model bone transforms (cached once) ═══
        private Matrix[] _smallRodTransforms;
        private Matrix[] _bigBallTransforms;
        private Matrix[] _smallBallTransforms;
        private ModelMesh[] _cachedSmallRodMeshes;
        private ModelMesh[] _cachedConeMeshes;
        private Matrix[] _coneTransforms;
        private ModelMesh[] _cachedBigBallMeshes;
        private ModelMesh[] _cachedSmallBallMeshes;

        // LOD caches: index 0=low, 1=med, 2=high
        private ModelMesh[][] _cachedBallMeshesLod = new ModelMesh[3][];
        private Matrix[][] _ballTransformsLod = new Matrix[3][];
        private ModelMesh[][] _cachedRodMeshesLod = new ModelMesh[3][];
        private Matrix[][] _rodTransformsLod = new Matrix[3][];

 // ═══ Per-model BasicEffect (used as template for per-atom state) ═══
        private BasicEffect _rodEffect;
        private BasicEffect _bigBallEffect;
        private BasicEffect _smallBallEffect;

 // ═══ CPK sphere imafterstor ═══
        private Effect _sphereImpostorEffect;
        private SphereImpostorVertex[] _sphereVerts;
        private const int MaxCpkAtoms = 8192;

 // ═══ Afterlyhedra (TIER 1: cached effect) ═══
        private BasicEffect _polyhedraEffect;

 // ═══ Overlay (other molecules) ═══
        private List<Molecule.Atom> _overlayAtoms = new List<Molecule.Atom>();
        private bool _showOverlay = true;
        private bool _modelsNeedInit = true;

 // ⭐ TIER 2.2: cached overlay groups
        private List<OverlayGroup> _cachedOverlayGroups;
        private bool _overlayGroupingDirty = true;

 // ═══ Afterlyhedrals ═══
        private List<Polihedrals.VertexStruct> _polyhedrals = new List<Polihedrals.VertexStruct>();
        private bool _showPolyhedrals = false;

 // ═══ Crystal planes ═══
        private List<CrystalPlane> _crystalPlanes = new List<CrystalPlane>();
        private List<(Vector3 start, Vector3 end)> _planeIntersectionLines =
            new List<(Vector3 start, Vector3 end)>();

 // ═══ Lazy GPU resources ═══
        private Texture2D _whitePixel;

 // ⭐ TIER 1: pre-allocated vertex buffers for crystal planes
        private readonly VertexPositionColor[] _planeQuadBuffer = new VertexPositionColor[6];
        private readonly VertexPositionColor[] _planeEdgesBuffer = new VertexPositionColor[8];

 // ═══ MSAA-aware rasterizer states ═══
        private RasterizerState _rasterizerCullBack;
        private RasterizerState _rasterizerCullNone;
        private RasterizerState _rasterizerCullNoneMSAA;

 // ═══ Atom render cache (rebuilt when atom count changes) ═══
        private AtomRenderCache _atomRenderCache;
        private int _atomRenderCacheCount = -1;

 // ═══ Atom colors cache (rebuilt when atom count changes) ═══
        private Vector3[] _atomColorsCache;

 // ═══ Depth sort cache ═══
        private (int index, float z)[] _zDepthCache;
        private Matrix _lastSortMatrix = Matrix.Identity;
        private bool _sortDirty = true;

 // ⭐ TIER 2.1: pre-computed list of visible atoms in render order
        private int[] _visibleAtomIndices;
        private int _visibleAtomCount = 0;

 // ═══ Effect cache (lighting / view / projection) ═══
        private EffectCache _effectCache;
        private bool _cacheNeedsUpdate = true;

 // ═══ Ellipsoid cache (per-atom ellipsoid matrices) ═══
        private EllipsoidCacheData[] _ellipsoidCache;

 // ═══ Color scale cache ═══
        private Vector3[] _colorTableInt;
        private float _cachedContrastInt = -1f;
        private int _cachedPalette = -1;

 // ═══ Selection color (yellow) ═══
        private static readonly Vector3 SelectedDiffuse = new Vector3(0.9f, 0.9f, 0.1f);

 // ═══ Reveal click marker ═══
        public Vector3 RevealClickPosition { get; set; } = Vector3.Zero;
        public bool ShowRevealClickPosition { get; set; } = false;

 // ═══ Symmetry elements renderer ═══
        private CellSymmetryRenderer _symmetryRenderer;

        #endregion

        #region Nested types

        private struct EllipsoidCacheData
        {
            public bool IsHydrogen;
            public float Size;
            public Matrix AtomMatrixBase;
            public Matrix[] EllipsoidMatrices;
            public bool IsEllipsoid;
            public float SizeElips;
            public float SizeLow;
            public Matrix UnOriginal;
        }

        private struct AtomRenderCache
        {
            public int[] StyleCache;
            public float[] OccupancyCache;
            public bool[] SelectedCache;
            public int[] AtomicNumberCache;
            public Vector3[] PositionCache;
            public int[] BondCountCache;
            public Matrix[][] PrecomputedBondCache;
            public float[][] BondOccupancyCache;
            public int[] HBondCountCache;
            public Matrix[][] PrecomputedHBondCache;
            public Matrix[] TransformationCache;
            public Matrix[] UnCache;
            public bool[] IsAnisotropicCache;
        }

        private struct EffectCache
        {
            public bool PreferPerPixelLighting;
            public bool LightingEnabled;
            public Vector3 SpecularColor;
            public float SpecularPower;
            public Vector3 LightDirection;
            public Vector3 LightSpecularColor;
            public Vector3 Light2Direction;
            public Vector3 Light2SpecularColor;
            public Matrix View;
            public Matrix Projection;
            public Vector3 DiffuseColor;
            public Vector3 AmbientLightColor;
            public Vector3 LightDiffuseColor;
            public Vector3 Light2DiffuseColor;
            public float Alpha;
            public bool IsValid;
        }

        private sealed class OverlayGroup
        {
            public Vector3 Color;
            public int Style;
            public List<Molecule.Atom> Atoms;
        }

        #endregion

        #region Properties

        public SpriteFont SpriteFontProperty
        {
            get => SpriteFont;
            set => SpriteFont = value;
        }

        public Model SmallBallProperty
        {
            get => Small_ball;
            set => Small_ball = value;
        }

        public Model BigBallProperty
        {
            get => Big_ball;
            set => Big_ball = value;
        }

        public Model SmallRodProperty
        {
            get => Small_rod;
            set => Small_rod = value;
        }

        #endregion

        #region Constructor

        public Draw(GraphicsDevice device, SpriteBatch spriteBatch, SpriteFont font, BasicEffect basicEffect)
        {
            _graphicsDevice = device;
            _spriteBatch = spriteBatch;
            SpriteFont = font;
            _basicEffect = basicEffect;
            _effectCache.IsValid = false;
            _cacheNeedsUpdate = true;
            _modelsNeedInit = true;

            InitializeCache();

            System.Diagnostics.Debug.WriteLine("[Draw] Constructor - cache initialized");
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            System.Diagnostics.Debug.WriteLine("[Draw] Dispose - freeing GPU resources");

 // ⚠️ UWAGA: NIE zwalniamy _symmetryRenderer tutaj.
 // The renderer is owned by GameWindow, which creates it
 // and disposes it together with itself. Draw only holds a reference.

            _whitePixel?.Dispose();
            _whitePixel = null;

            _rasterizerCullBack?.Dispose();
            _rasterizerCullBack = null;

            _rasterizerCullNone?.Dispose();
            _rasterizerCullNone = null;

            _rasterizerCullNoneMSAA?.Dispose();
            _rasterizerCullNoneMSAA = null;

            _polyhedraEffect?.Dispose();
            _polyhedraEffect = null;
        }

        #endregion

        #region Lazy resource getters

        private Texture2D GetWhitePixel()
        {
            if (_whitePixel == null || _whitePixel.IsDisposed)
            {
                _whitePixel = new Texture2D(_graphicsDevice, 1, 1);
                _whitePixel.SetData(new[] { XnaColor.White });
            }
            return _whitePixel;
        }

        private static int GetSceneMSAA()
        {
            int msaa = Flags.Instance.MSAAOnSceneRT;
            if (msaa != 0 && msaa != 2 && msaa != 4 && msaa != 8)
                msaa = 4;
            return msaa;
        }

        private RasterizerState GetCullBack()
        {
            if (_rasterizerCullBack == null || _rasterizerCullBack.IsDisposed)
            {
                _rasterizerCullBack = new RasterizerState
                {
                    CullMode = CullMode.CullCounterClockwiseFace,
                    FillMode = FillMode.Solid,
                    MultiSampleAntiAlias = true
                };
            }
            return _rasterizerCullBack;
        }

        private RasterizerState GetCullNone()
        {
            if (_rasterizerCullNone == null || _rasterizerCullNone.IsDisposed)
            {
                _rasterizerCullNone = new RasterizerState
                {
                    CullMode = CullMode.None,
                    FillMode = FillMode.Solid,
                    MultiSampleAntiAlias = true
                };
            }
            return _rasterizerCullNone;
        }

        private RasterizerState GetCullNoneMSAA()
        {
            if (_rasterizerCullNoneMSAA == null || _rasterizerCullNoneMSAA.IsDisposed)
            {
                _rasterizerCullNoneMSAA = new RasterizerState
                {
                    CullMode = CullMode.None,
                    FillMode = FillMode.Solid,
                    MultiSampleAntiAlias = true
                };
            }
            return _rasterizerCullNoneMSAA;
        }

        #endregion

        #region Public API — scene setup

        public void LoadSphereImpostor(Effect effect)
        {
            _sphereImpostorEffect = effect;
            _sphereVerts = new SphereImpostorVertex[MaxCpkAtoms * 6];
        }

        public void SetMeasure(Measure measure)
        {
            _measure = measure;
        }

        public void SetMolecule(Molecule molecule)
        {
            _molecule = molecule;
            _cacheNeedsUpdate = true;
            _sortDirty = true;
        }

        public void SetCamera(Camera camera)
        {
            _camera = camera;
            _cacheNeedsUpdate = true;
            _sortDirty = true;
        }

        public void SetCavityRenderer(CavityRenderer renderer)
        {
            _cavityRenderer = renderer;
            _showCavities = renderer != null;
        }

        public void UpdateCavityData(List<Cavity.Cavity> cavities)
        {
            if (_cavityRenderer != null && cavities != null)
            {
                _cavityRenderer.SetCavities(cavities);
                _cavityRenderer.ForceRefresh();
                _showCavities = true;
            }
        }

        public void SetCavityDrawMode(CavityRenderer.DrawMode mode)
        {
            if (_cavityRenderer != null)
            {
                _cavityRenderer.CurrentDrawMode = mode;
            }
        }

        public void ShowCavities(bool show)
        {
            _showCavities = show;
        }

        public void SetCrystalPlanes(List<CrystalPlane> planes)
        {
            _crystalPlanes = planes ?? new List<CrystalPlane>();
        }

        public void SetPolyhedrals(List<Polihedrals.VertexStruct> polyhedrals)
        {
            _polyhedrals = polyhedrals != null
                ? new List<Polihedrals.VertexStruct>(polyhedrals)
                : new List<Polihedrals.VertexStruct>();

            _showPolyhedrals = _polyhedrals.Count > 0;

            System.Diagnostics.Debug.WriteLine(
                $"[Draw] SetPolyhedrals: {_polyhedrals.Count} items, show={_showPolyhedrals}");
        }

        public void SetOverlayAtoms(List<Molecule.Atom> atoms)
        {
            _overlayAtoms = atoms ?? new List<Molecule.Atom>();
            _modelsNeedInit = true;
            _overlayGroupingDirty = true;
            System.Diagnostics.Debug.WriteLine($"[Draw] SetOverlayAtoms: {_overlayAtoms.Count} atoms");
        }

        public void RefreshOverlayCache()
        {
            _modelsNeedInit = true;
            _overlayGroupingDirty = true;
        }

 /// <summary>
 /// Sets renderer elements symmetry. Called przez GameWindow.
 /// </summary>
        public void SetSymmetryElementsRenderer(CellSymmetryRenderer renderer)
        {
            _symmetryRenderer = renderer;
        }

        #endregion

        #region Public API — wrappery dla SymmetryElementsRenderer

 /// <summary>
 /// Publiczny wrapper na DrawRodSegment.
 /// </summary>
        public void DrawRodSegmentPublic(Vector3 p1, Vector3 p2,
                                          Matrix scaleRotate, float thickness)
        {
            DrawRodSegment(p1, p2, scaleRotate, thickness);
        }

 /// <summary>
 /// Publiczny wrapper na DrawPlaneQuad.
 /// </summary>
        public void DrawPlaneQuadPublic(Vector3 c0, Vector3 c1, Vector3 c2, Vector3 c3,
                                         Vector4 color, Matrix scaleRotate)
        {
            DrawPlaneQuad(c0, c1, c2, c3, color, scaleRotate);
        }

 /// <summary>
 /// Publiczny wrapper na DrawPlaneEdges.
 /// </summary>
        public void DrawPlaneEdgesPublic(Vector3 c0, Vector3 c1, Vector3 c2, Vector3 c3,
                                          Vector4 color, Matrix scaleRotate)
        {
            DrawPlaneEdges(c0, c1, c2, c3, color, scaleRotate);
        }

 /// <summary>
 /// Publiczny wrapper na DrawSmallSphereAtpoint.
 /// </summary>
        public void DrawSmallSphereAtPointPublic(Vector3 position, Matrix scaleRotate,
                                                  XnaColor color, float radius)
        {
            DrawSmallSphereAtPoint(position, scaleRotate, color, radius);
        }

 /// <summary>
 /// Tymczasowo setia colour w _effectCache.
 /// </summary>
        public void SetTemporaryColor(Vector3 rgb)
        {
            _effectCache.DiffuseColor = rgb;
            _effectCache.AmbientLightColor = Vector3.One * 0.5f;
            _effectCache.LightDiffuseColor = rgb;
            _effectCache.SpecularColor = Vector3.Zero;
            _effectCache.LightSpecularColor = Vector3.Zero;
            _effectCache.Alpha = 1.0f;
        }

 /// <summary>
 /// Publiczny getter na scaleRotate.
 /// </summary>
        public Matrix GetScaleRotate()
        {
            if (_camera == null) return Matrix.Identity;

            float oneOverZoom = _camera.OneOverZoom;
            return Matrix.CreateScale(oneOverZoom, oneOverZoom, oneOverZoom)
 * _camera.Rotate
 * _camera.Translate;
        }

 /// <summary>
 /// Publiczny getter GraphicsDevice — for SymmetryElementsRenderer
 /// (creates its own BasicEffect for markers).
 /// </summary>
        public GraphicsDevice GetGraphicsDeviceForRenderer() => _graphicsDevice;

 /// <summary>
 /// Publiczny getter camera — for SymmetryElementsRenderer.
 /// </summary>
        public Camera GetCameraForRenderer() => _camera;

 /// <summary>
 /// Draws the marker buffer (arrows, polygons) using the camera.
 /// Called przez SymmetryElementsRenderer.
 ///
 /// ⚠️ Oczekuje coordinates w WORLD (shifted), nie w NDC.
 /// View/Projection setia na based on _camera.
 /// </summary>
        public void DrawMarkersPublic(
            GraphicsDevice device,
            BasicEffect markerEffect,
            VertexPositionColor[] buffer,
            int vertexCount)
        {
            if (device == null || device.IsDisposed) return;
            if (markerEffect == null || markerEffect.IsDisposed) return;
            if (buffer == null || vertexCount <= 0) return;
            if (_camera == null) return;

            var savedBlend = device.BlendState;
            var savedDepth = device.DepthStencilState;
            var savedRaster = device.RasterizerState;

            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.DepthRead;
            device.RasterizerState = RasterizerState.CullNone;

            markerEffect.View = _camera.View;
            markerEffect.Projection = _camera.Projection;
            markerEffect.World = Matrix.Identity;

            foreach (var pass in markerEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserPrimitives(
                    PrimitiveType.TriangleList,
                    buffer, 0, vertexCount / 3);
            }

            device.BlendState = savedBlend;
            device.DepthStencilState = savedDepth;
            device.RasterizerState = savedRaster;
        }

        #endregion

        #region Public API — cache lifecycle

        public void InitializeCache()
        {
            System.Diagnostics.Debug.WriteLine("[Draw] InitializeCache - START");

            if (Small_rod != null && _smallRodTransforms == null)
            {
                _smallRodTransforms = new Matrix[Small_rod.Bones.Count];
                Small_rod.CopyAbsoluteBoneTransformsTo(_smallRodTransforms);
                _cachedSmallRodMeshes = Small_rod.Meshes.ToArray();
                _rodEffect = FindFirstBasicEffect(_cachedSmallRodMeshes);
            }
            if (Cone != null && _coneTransforms == null)
            {
                _coneTransforms = new Matrix[Cone.Bones.Count];
                Cone.CopyAbsoluteBoneTransformsTo(_coneTransforms);
                _cachedConeMeshes = Cone.Meshes.ToArray();
            }

            if (Big_ball != null && _bigBallTransforms == null)
            {
                _bigBallTransforms = new Matrix[Big_ball.Bones.Count];
                Big_ball.CopyAbsoluteBoneTransformsTo(_bigBallTransforms);
                _cachedBigBallMeshes = Big_ball.Meshes.ToArray();
                _bigBallEffect = FindFirstBasicEffect(_cachedBigBallMeshes);
            }

            if (Small_ball != null && _smallBallTransforms == null)
            {
                _smallBallTransforms = new Matrix[Small_ball.Bones.Count];
                Small_ball.CopyAbsoluteBoneTransformsTo(_smallBallTransforms);
                _cachedSmallBallMeshes = Small_ball.Meshes.ToArray();
                _smallBallEffect = FindFirstBasicEffect(_cachedSmallBallMeshes);
            }

            // ── Distance LOD tiers ──
            CacheLodModel(BallLow ?? Small_ball, 0, isBall: true);
            CacheLodModel(BallMed ?? Small_ball, 1, isBall: true);
            CacheLodModel(BallHigh ?? Big_ball ?? Small_ball, 2, isBall: true);
            CacheLodModel(RodLow ?? Small_rod, 0, isBall: false);
            CacheLodModel(RodMed ?? Small_rod, 1, isBall: false);
            CacheLodModel(RodHigh ?? Small_rod, 2, isBall: false);

            // Fallback: if a tier is missing, copy from nearest
            for (int i = 0; i < 3; i++)
            {
                if (_cachedBallMeshesLod[i] == null)
                    _cachedBallMeshesLod[i] = _cachedSmallBallMeshes ?? _cachedBigBallMeshes;
                if (_ballTransformsLod[i] == null)
                    _ballTransformsLod[i] = _smallBallTransforms ?? _bigBallTransforms;
                if (_cachedRodMeshesLod[i] == null)
                    _cachedRodMeshesLod[i] = _cachedSmallRodMeshes;
                if (_rodTransformsLod[i] == null)
                    _rodTransformsLod[i] = _smallRodTransforms;
            }

            _modelsNeedInit = false;
            System.Diagnostics.Debug.WriteLine("[Draw] InitializeCache - END (with distance LOD)");
        }

        private void CacheLodModel(Model model, int tier, bool isBall)
        {
            if (model == null || tier < 0 || tier > 2) return;
            var transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);
            var meshes = model.Meshes.ToArray();
            if (isBall)
            {
                _ballTransformsLod[tier] = transforms;
                _cachedBallMeshesLod[tier] = meshes;
            }
            else
            {
                _rodTransformsLod[tier] = transforms;
                _cachedRodMeshesLod[tier] = meshes;
            }
        }

        /// <summary>
        /// Pick mesh LOD from view-space depth (larger −Z = farther in typical MonoGame view).
        /// Near → high poly, far → low poly.
        /// </summary>
        private static int PickMeshLod(float viewSpaceZ)
        {
            // viewSpaceZ is usually negative in front of camera; use absolute depth
            float depth = viewSpaceZ < 0 ? -viewSpaceZ : viewSpaceZ;
            if (depth < 12f) return 2;   // high
            if (depth < 35f) return 1;   // medium
            return 0;                   // low
        }

        private float ViewSpaceZ(Vector3 worldPos, Matrix scaleRotate)
        {
            if (_camera == null) return 0f;
            Vector3 p = Vector3.Transform(worldPos, scaleRotate * _camera.View);
            return p.Z;
        }

        private static BasicEffect FindFirstBasicEffect(ModelMesh[] meshes)
        {
            if (meshes == null) return null;
            foreach (var mesh in meshes)
            {
                foreach (var effect in mesh.Effects)
                {
                    if (effect is BasicEffect be)
                        return be;
                }
            }
            return null;
        }

        public void ResetCache()
        {
            _ellipsoidCache = null;
            _zDepthCache = null;
            _colorTableInt = null;
            _cachedContrastInt = -1f;
            _cachedPalette = -1;
            _effectCache.IsValid = false;
            _cacheNeedsUpdate = true;
            _sortDirty = true;

            _modelsNeedInit = true;
            _cachedSmallBallMeshes = null;
            _smallBallTransforms = null;

            _visibleAtomIndices = null;
            _visibleAtomCount = 0;

            _cachedOverlayGroups = null;
            _overlayGroupingDirty = true;

            _polyhedrals?.Clear();
            _showPolyhedrals = false;

            _whitePixel?.Dispose();
            _whitePixel = null;

            _rasterizerCullBack?.Dispose();
            _rasterizerCullBack = null;

            _rasterizerCullNone?.Dispose();
            _rasterizerCullNone = null;

            _rasterizerCullNoneMSAA?.Dispose();
            _rasterizerCullNoneMSAA = null;

            _polyhedraEffect?.Dispose();
            _polyhedraEffect = null;

            _atomRenderCache = default;
            _atomRenderCacheCount = -1;
        }

        #endregion

        #region DrawScene — top-level orchestration

        public void DrawScene()
        {
            if (_molecule == null) return;

            if (_cachedSmallRodMeshes == null)
                InitializeCache();

            if (_cachedSmallRodMeshes == null || _cachedBigBallMeshes == null || _cachedSmallBallMeshes == null)
            {
                InitializeCache();
                if (_cachedSmallRodMeshes == null || _cachedBigBallMeshes == null || _cachedSmallBallMeshes == null)
                {
                    System.Diagnostics.Debug.WriteLine("[Draw] ❌ Models not loaded - cannot draw!");
                    return;
                }
            }

            int atomCount = _molecule.NumberOfAtoms;
            var cameraView = _camera.View;
            var cameraProjection = _camera.Projection;
            var cameraRotate = _camera.Rotate;
            float oneOverZoom = _camera.OneOverZoom;
            bool isHydrogenOn = Flags.Instance.ShowHBonds;

            bool depthCueEnabled = Flags.Instance.DepthCue;
            float DEPTH_CUE_NEAR = Flags.Instance.DepthCueNear;
            float DEPTH_CUE_FAR = Flags.Instance.DepthCueFar;
            float DEPTH_CUE_STRENGTH = Flags.Instance.DepthCueStrength;

            bool isEndRotation = _camera.flag_end_rotation;
            ClearBondBatch();
            ClearSphereBatch();

            Matrix scaleRotate = Matrix.CreateScale(oneOverZoom, oneOverZoom, oneOverZoom)
 * cameraRotate
 * _camera.Translate;

            Matrix sortMatrix = cameraRotate * cameraView * cameraProjection;
            if (_sortDirty || sortMatrix != _lastSortMatrix)
            {
                OrderAtoms3D(sortMatrix);
                _lastSortMatrix = sortMatrix;
                _sortDirty = false;
            }

            _graphicsDevice.DepthStencilState = DepthStencilState.Default;

            if (_molecule == null || _camera == null || _molecule.NumberOfAtoms == 0)
            {
                if (_showOverlay && _overlayAtoms.Count > 0)
                {
                    DrawOverlayOnly(scaleRotate);
                }
                return;
            }

            Vector3[] atomColors = PrepareAtomColors(atomCount);
            AtomRenderCache atomCache = PrepareAtomRenderCache(atomCount);

            BuildEllipsoidCache(atomCount, atomCache.StyleCache, atomCache.AtomicNumberCache,
                                atomCache.TransformationCache, atomCache.UnCache,
                                atomCache.IsAnisotropicCache);
            UpdateSceneCache(cameraView, cameraProjection);

            ApplyGlobalEffectState();

 // ═══ PASS 1 — OPAQUE ═══
            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.RasterizerState = GetCullBack();

            if (_sphereImpostorEffect != null)
                DrawCPKImpostors(atomCache, atomColors, scaleRotate,
                 isTransparent: false,
                 depthCueEnabled: depthCueEnabled,
                 depthCueNear: DEPTH_CUE_NEAR,
                 depthCueFar: DEPTH_CUE_FAR,
                 depthCueStrength: DEPTH_CUE_STRENGTH);

            int visibleCount = _visibleAtomCount;
            var visibleIndices = _visibleAtomIndices;

            for (int i = 0; i < visibleCount; i++)
            {
                int idx = visibleIndices[i];
                if (atomCache.OccupancyCache[idx] < 0.999f) continue;

                DrawSingleAtom(idx, atomCache, atomColors, scaleRotate,
                               cameraRotate, cameraView,
                               depthCueEnabled, DEPTH_CUE_NEAR, DEPTH_CUE_FAR, DEPTH_CUE_STRENGTH,
                               isHydrogenOn, isEndRotation, isTransparent: false);
            }

            FlushBondBatch(transparentPass: false);
            FlushSphereBatch();

 // ═══ PASS 2 — TRANSPARENT ═══
            _graphicsDevice.BlendState = BlendState.AlphaBlend;
            _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            _graphicsDevice.RasterizerState = GetCullBack();

            if (_sphereImpostorEffect != null)
                DrawCPKImpostors(atomCache, atomColors, scaleRotate,
                 isTransparent: true,
                 depthCueEnabled: depthCueEnabled,
                 depthCueNear: DEPTH_CUE_NEAR,
                 depthCueFar: DEPTH_CUE_FAR,
                 depthCueStrength: DEPTH_CUE_STRENGTH);

            for (int i = 0; i < visibleCount; i++)
            {
                int idx = visibleIndices[i];
                if (atomCache.OccupancyCache[idx] >= 0.999f) continue;

                DrawSingleAtom(idx, atomCache, atomColors, scaleRotate,
                               cameraRotate, cameraView,
                               depthCueEnabled, DEPTH_CUE_NEAR, DEPTH_CUE_FAR, DEPTH_CUE_STRENGTH,
                               isHydrogenOn, isEndRotation, isTransparent: true);
            }

            FlushBondBatch(transparentPass: true);
            FlushSphereBatch();

            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.RasterizerState = GetCullBack();

            DrawMeasurements(scaleRotate);

            if (ShowRevealClickPosition && _cachedSmallBallMeshes != null)
            {
                DrawRevealClickPosition(scaleRotate);
            }

            DrawContacts(scaleRotate);
            DrawAllHydrogenBonds(scaleRotate);
            DrawAromaticRings(scaleRotate);

            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.RasterizerState = GetCullBack();

            _cavityRenderer?.Draw(scaleRotate);

            if (_showOverlay && _overlayAtoms.Count > 0)
            {
                DrawOverlayAtoms(scaleRotate);
            }

            if (Flags.Instance.Draw_cell)
                DrawCell(scaleRotate);

            DrawForceArrows(scaleRotate);

            DrawCrystalPlanes(scaleRotate);

            if (_showPolyhedrals)
            {
                DrawPolyhedrals(scaleRotate);
            }

 // ═══ Symmetry elements ═══
            if (Flags.Instance.ShowSymmetryElements && _symmetryRenderer != null)
            {
                _graphicsDevice.BlendState = BlendState.AlphaBlend;
                _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
                _graphicsDevice.RasterizerState = GetCullNone();

                _symmetryRenderer.Draw(this);

                _graphicsDevice.BlendState = BlendState.Opaque;
                _graphicsDevice.DepthStencilState = DepthStencilState.Default;
                _graphicsDevice.RasterizerState = GetCullBack();
            }

            DrawLabels(scaleRotate);

            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
            _graphicsDevice.RasterizerState = GetCullBack();
        }

        #endregion
    }

 /// <summary>
 /// Vertex format for CPK sphere imafterstors.
 /// </summary>
    public struct SphereImpostorVertex : IVertexType
    {
        public Vector3 Center;
        public Vector2 Corner;
        public float Radius;
        public Vector3 Color;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(20, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(24, VertexElementFormat.Vector3, VertexElementUsage.Color, 0)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public const int SizeInBytes = 36;
    }
}