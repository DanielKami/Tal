// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: CavityRenderer.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using Tal.DrawMethods;
using Color = Microsoft.Xna.Framework.Color;
using SysVector3 = System.Numerics.Vector3;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;

namespace Tal.Cavity
{
 /// <summary>
 /// Renders cavities and molecular surfaces.
 ///
 /// ⭐ Wersja 3.0:
 /// - One shared VB/IB for the entire scene (all cavities together)
 /// - Jedno sortowanie back-to-front for entire sceny
 /// - One draw call per scene
 /// - Klasa afterdzielona na partial: Drawing, Fallback, Data, Batching
 /// </summary>
    public partial class CavityRenderer
    {
        #region Lighting Cache

        private struct LightingCache
        {
            public bool IsValid;
            public bool Light1Enabled;
            public XnaVector3 Light1Direction;
            public XnaVector3 Light1Diffuse;
            public XnaVector3 Light1Specular;
            public bool Light2Enabled;
            public XnaVector3 Light2Direction;
            public XnaVector3 Light2Diffuse;
            public XnaVector3 Light2Specular;
            public XnaVector3 Light2Ambient;
            public XnaVector3 AmbientColor;
            public float SpecularPower;
            public bool PerPixelLighting;
            public bool LightingEnabled;
        }

        private LightingCache _lightingCache = new LightingCache { IsValid = false };
        private bool _lightingCacheNeedsUpdate = true;

        #endregion

        #region Private Fields

        private readonly Molecule _molecule;
        private readonly GraphicsDevice _graphicsDevice;
        private Camera _camera;

        private List<RenderCavity> _renderCavities = new List<RenderCavity>();
        private List<Cavity> _cavities = new List<Cavity>();
        private List<RenderCavity> _originalRenderCavities = new List<RenderCavity>();
        private readonly object _cavitiesLock = new object();

        private List<RenderMolecularSurface> _renderMolecularSurfaces = new List<RenderMolecularSurface>();
        private List<RenderMolecularSurface> _originalRenderMolecularSurfaces = new List<RenderMolecularSurface>();
        private readonly object _molecularSurfacesLock = new object();

        private XnaVector3 _currentShift = XnaVector3.Zero;
        private XnaVector3 _lastAppliedShift = XnaVector3.Zero;
        private bool _isGenerating = false;
        private bool _showCavities = false;
        private bool _showMolecularSurfaces = false;
        private float _transparency = 1.0f;
        private DrawMode _currentDrawMode = DrawMode.Hidden;

        private BasicEffect _effect;
        private BasicEffect _lightingEffect;
        private Effect _cavitySurfaceEffect;

        private RasterizerState _cullNoneRasterizer;
        private DepthStencilState _depthStencilState;

        private readonly XnaVector3 _defaultLightDirection = new XnaVector3(0, 0, 0.5f);
        private readonly XnaVector3 _defaultAmbientColor = new XnaVector3(0.1f, 0.1f, 0.1f);
        private readonly XnaVector3 _defaultDiffuseColor = new XnaVector3(0.8f, 0.8f, 0.8f);
        private readonly XnaVector3 _defaultSpecularColor = new XnaVector3(0.9f, 0.9f, 0.9f);
        private readonly float _defaultSpecularPower = 20f;

        private Model _sphereModel;
        private Matrix[] _sphereTransforms;
        private ModelMesh[] _sphereMeshes;

        private List<RenderCavity> _cachedDrawList = new List<RenderCavity>();

        public Action<string> ProgressUpdated { get; set; }

        #endregion

        #region Constructor

        public CavityRenderer(Molecule molecule, GraphicsDevice graphicsDevice, Camera camera)
        {
            _molecule = molecule ?? throw new ArgumentNullException(nameof(molecule));
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));

            if (_graphicsDevice.IsDisposed)
                throw new InvalidOperationException("GraphicsDevice is disposed");

            InitializeEffects();
            _transparency = UpdateTransparency();
            UpdateLightingCache();
        }

        #endregion

        #region Initialization

        private void InitializeEffects()
        {
            if (_graphicsDevice == null || _graphicsDevice.IsDisposed)
            {
                Debug.WriteLine("[CavityRenderer] Cannot initialize effects");
                return;
            }

            _effect = new BasicEffect(_graphicsDevice)
            {
                VertexColorEnabled = true,
                LightingEnabled = false,
                Alpha = _transparency
            };

            _lightingEffect = new BasicEffect(_graphicsDevice)
            {
                VertexColorEnabled = true,
                LightingEnabled = true,
                Alpha = _transparency,
                AmbientLightColor = _defaultAmbientColor,
                SpecularColor = _defaultSpecularColor,
                SpecularPower = _defaultSpecularPower
            };

            _cullNoneRasterizer = new RasterizerState { CullMode = CullMode.None, FillMode = FillMode.Solid };
            _depthStencilState = DepthStencilState.Default;

            Debug.WriteLine("[CavityRenderer] Effects initialized");
        }

        public void SetCavitySurfaceEffect(Effect effect)
        {
            _cavitySurfaceEffect = effect;
            InvalidateSceneBatch();
            Debug.WriteLine($"[CavityRenderer] SetCavitySurfaceEffect: " +
                            $"{(effect != null ? "shader OK" : "null — fallback")}");
        }

        #endregion

        #region Public Properties

        public float Transparency
        {
            get => _transparency;
            set
            {
                float newValue = Math.Clamp(value, 0.0f, 1.0f);
                if (Math.Abs(_transparency - newValue) > 0.001f)
                {
                    _transparency = newValue;
                    if (_effect != null) _effect.Alpha = _transparency;
                    if (_lightingEffect != null) _lightingEffect.Alpha = _transparency;
                }
            }
        }

        public float UpdateTransparency()
        {
            float alpha = Flags.Instance.transparency / 10.0f;
            return Math.Clamp(alpha, 0.0f, 1.0f);
        }

        public DrawMode CurrentDrawMode
        {
            get => _currentDrawMode;
            set => _currentDrawMode = value;
        }

        #endregion

        #region Enums

        public enum DrawMode
        {
            Hidden,
            VoxelPoints,
            MarchingCubesWireframe,
            MarchingCubesMesh,
            MarchingCubesMeshPerVertex,
            CavityColorMap,
            TopologyColorMap,
            ChannelPathsOnly,
            ChannelPathsOverlay,
            SphereFitsOnly
        }

        #endregion

        #region Camera and Sphere Model

        public void SetCamera(Camera camera)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
        }

        public Camera GetCamera() => _camera;

        public void SetSphereModel(Model sphereModel)
        {
            _sphereModel = sphereModel;
            if (sphereModel != null)
            {
                _sphereTransforms = new Matrix[sphereModel.Bones.Count];
                sphereModel.CopyAbsoluteBoneTransformsTo(_sphereTransforms);
                _sphereMeshes = sphereModel.Meshes.ToArray();
            }
            else
            {
                _sphereTransforms = null;
                _sphereMeshes = null;
            }
        }

        #endregion

        #region Lighting Management

        private void UpdateLightingCache()
        {
            var flags = Flags.Instance;

            _lightingCache.Light1Enabled = flags.Enabled;
            _lightingCache.Light1Direction = new XnaVector3(flags.DirectionX, flags.DirectionY, flags.DirectionZ);
            _lightingCache.Light1Diffuse = new XnaVector3(flags.DiffuseIntensity, flags.DiffuseIntensity, flags.DiffuseIntensity);
            _lightingCache.Light1Specular = new XnaVector3(flags.SpecularIntensity, flags.SpecularIntensity, flags.SpecularIntensity);

            _lightingCache.Light2Enabled = flags.Light2Enabled;
            _lightingCache.Light2Direction = new XnaVector3(flags.Light2DirectionX, flags.Light2DirectionY, flags.Light2DirectionZ);
            _lightingCache.Light2Diffuse = new XnaVector3(flags.Light2DiffuseIntensity, flags.Light2DiffuseIntensity, flags.Light2DiffuseIntensity);
            _lightingCache.Light2Specular = new XnaVector3(flags.Light2SpecularIntensity, flags.Light2SpecularIntensity, flags.Light2SpecularIntensity);
            _lightingCache.Light2Ambient = new XnaVector3(flags.Light2AmbientIntensity, flags.Light2AmbientIntensity, flags.Light2AmbientIntensity);

            _lightingCache.AmbientColor = new XnaVector3(flags.AmbientIntensity, flags.AmbientIntensity, flags.AmbientIntensity);
            _lightingCache.SpecularPower = flags.SpecularPower;
            _lightingCache.PerPixelLighting = flags.PerPixelLighting;
            _lightingCache.LightingEnabled = flags.Enabled;
            _lightingCache.IsValid = true;
            _lightingCacheNeedsUpdate = false;
        }

        public void InvalidateLightingCache()
        {
            _lightingCacheNeedsUpdate = true;
            _lightingCache.IsValid = false;
        }

        private void EnsureLightingCacheValid()
        {
            if (_lightingCacheNeedsUpdate || !_lightingCache.IsValid)
                UpdateLightingCache();
        }

        public void SyncLightingFromFlags()
        {
            _lightingCacheNeedsUpdate = true;
            _lightingCache.IsValid = false;
            UpdateLightingCache();
        }

        #endregion

        #region Cavity Management

        public void SetCavities(List<Cavity> cavities)
        {
            lock (_cavitiesLock)
            {
                _cavities = new List<Cavity>(cavities ?? new List<Cavity>());

                _originalRenderCavities.Clear();
                _renderCavities.Clear();

                foreach (var source in _cavities)
                {
                    var rc = new RenderCavity(source);
                    _originalRenderCavities.Add(rc);
                    _renderCavities.Add(rc);
                }

                _showCavities = true;

                if (_lastAppliedShift != XnaVector3.Zero)
                {
                    _currentShift = _lastAppliedShift;
                    foreach (var c in _renderCavities)
                        c.ApplyShift(_currentShift);
                }
                else
                {
                    _currentShift = XnaVector3.Zero;
                }

                InvalidateSceneBatch();
            }
        }

        /// <summary>Toggle visibility of one cavity by Id; rebuilds GPU batch.</summary>
        public void SetCavityVisible(int cavityId, bool visible)
        {
            lock (_cavitiesLock)
            {
                int n = 0;
                if (_cavities != null)
                {
                    foreach (var c in _cavities)
                        if (c.Id == cavityId) { c.Visible = visible; n++; }
                }
                if (_renderCavities != null)
                {
                    foreach (var rc in _renderCavities)
                        if (rc.CavityId == cavityId) { rc.Visible = visible; n++; }
                }
                if (_originalRenderCavities != null)
                {
                    foreach (var rc in _originalRenderCavities)
                        if (rc.CavityId == cavityId) { rc.Visible = visible; n++; }
                }
                System.Diagnostics.Debug.WriteLine(
                    $"[CavityRenderer] SetCavityVisible id={cavityId} visible={visible} touched={n}");
                // Force GPU batch rebuild on next draw (drop old combined mesh)
                InvalidateSceneBatch();
            }
        }

        /// <summary>Show/hide all cavities.</summary>
        public void SetAllCavitiesVisible(bool visible)
        {
            lock (_cavitiesLock)
            {
                if (_cavities != null)
                    foreach (var c in _cavities) c.Visible = visible;
                if (_renderCavities != null)
                    foreach (var rc in _renderCavities) rc.Visible = visible;
                if (_originalRenderCavities != null)
                    foreach (var rc in _originalRenderCavities) rc.Visible = visible;
                InvalidateSceneBatch();
            }
        }

        public List<Cavity> GetCavities()
        {
            lock (_cavitiesLock)
                return _cavities != null ? new List<Cavity>(_cavities) : new List<Cavity>();
        }

        public List<RenderCavity> GetRenderCavities()
        {
            lock (_cavitiesLock)
                return _renderCavities != null ? new List<RenderCavity>(_renderCavities) : new List<RenderCavity>();
        }

        /// <summary>
        /// Same visibility rules as the 3D draw path: RenderCavity.Visible
        /// and underlying <see cref="Cavity.Visible"/> (checkbox in Cavity Settings).
        /// </summary>
        public List<RenderCavity> GetVisibleRenderCavitiesForExport()
        {
            lock (_cavitiesLock)
            {
                var result = new List<RenderCavity>();
                if (_renderCavities == null || _renderCavities.Count == 0)
                    return result;

                // Authoritative set from logical cavities (UI checkboxes)
                var allowedIds = new HashSet<int>();
                bool haveSource = _cavities != null && _cavities.Count > 0;
                if (haveSource)
                {
                    foreach (var c in _cavities)
                    {
                        if (c != null && c.Visible)
                            allowedIds.Add(c.Id);
                    }
                }

                foreach (var rc in _renderCavities)
                {
                    if (rc == null) continue;
                    if (!rc.Visible) continue;
                    if (haveSource && !allowedIds.Contains(rc.CavityId)) continue;
                    result.Add(rc);
                }
                return result;
            }
        }

        public void SetMoleculeShift(Vector3 shift)
        {
            XnaVector3 newShift = new XnaVector3(-shift.X, -shift.Y, -shift.Z);

            if (Vector3.Distance(newShift, _lastAppliedShift) > 0.0001f)
            {
                _lastAppliedShift = newShift;
                UpdateCavityPositions(newShift);
            }
        }

        public void UpdateCavityPositions(XnaVector3 shift)
        {
            lock (_cavitiesLock)
            {
                _currentShift = shift;
                foreach (var c in _renderCavities)
                    c.ApplyShift(shift);
                InvalidateSceneBatch();
            }
        }

        public void BuildRepeatedCavities()
        {
            lock (_cavitiesLock)
            {
                if (_originalRenderCavities.Count == 0) return;

                float minX = Flags.Instance.ShiftNegativeX;
                float maxX = Flags.Instance.ShiftPositiveX;
                float minY = Flags.Instance.ShiftNegativeY;
                float maxY = Flags.Instance.ShiftPositiveY;
                float minZ = Flags.Instance.ShiftNegativeZ;
                float maxZ = Flags.Instance.ShiftPositiveZ;

                int startX = (int)Math.Floor(minX);
                int endX = (int)Math.Ceiling(maxX);
                int startY = (int)Math.Floor(minY);
                int endY = (int)Math.Ceiling(maxY);
                int startZ = (int)Math.Floor(minZ);
                int endZ = (int)Math.Ceiling(maxZ);

                var (vecA, vecB, vecC) = ComputeUnitCellVectors();

                if (startX == 0 && endX == 1 && startY == 0 && endY == 1 && startZ == 0 && endZ == 1)
                {
                    _renderCavities.Clear();
                    foreach (var orig in _originalRenderCavities)
                        _renderCavities.Add(orig.CloneWithShift(XnaVector3.Zero));

                    if (_lastAppliedShift != XnaVector3.Zero)
                        foreach (var cCav in _renderCavities)
                            cCav.ApplyShift(_lastAppliedShift);

                    InvalidateSceneBatch();
                    ProgressUpdated?.Invoke($"Returned to original cell, {_renderCavities.Count} cavities");
                    return;
                }

                _renderCavities.Clear();
                var newCavities = new List<RenderCavity>();

                foreach (var orig in _originalRenderCavities)
                {
                    for (int x = startX; x < endX; x++)
                        for (int y = startY; y < endY; y++)
                            for (int z = startZ; z < endZ; z++)
                            {
                                var shift = x * vecA + y * vecB + z * vecC;
                                newCavities.Add(orig.CloneWithShift(shift));
                            }
                }

                _renderCavities = newCavities;

                if (_lastAppliedShift != XnaVector3.Zero)
                    foreach (var c_ in _renderCavities)
                        c_.ApplyShift(_lastAppliedShift);

                InvalidateSceneBatch();
                ProgressUpdated?.Invoke($"Built {_renderCavities.Count} repeated cavities");
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private (XnaVector3 vecA, XnaVector3 vecB, XnaVector3 vecC) ComputeUnitCellVectors()
        {
            float a = _molecule.a;
            float b = _molecule.b;
            float c = _molecule.c;
            float alpha = _molecule.alpha;
            float beta = _molecule.beta;
            float gamma = _molecule.gamma;

            var vecA = new XnaVector3(a, 0, 0);
            var vecB = new XnaVector3((float)(b * Math.Cos(gamma)), (float)(b * Math.Sin(gamma)), 0);

            float cosAlpha = (float)Math.Cos(alpha);
            float cosBeta = (float)Math.Cos(beta);
            float sinBeta = (float)Math.Sin(beta);
            float sinGamma = (float)Math.Sin(gamma);

            float cX = c * cosBeta;
            float cY = 0;
            if (sinGamma > 0.0001f)
                cY = c * (cosAlpha - cosBeta * (float)Math.Cos(gamma)) / sinGamma;
            float cZ = 0;
            float tempZ = 1 - cosBeta * cosBeta - (cY / c) * (cY / c);
            if (tempZ > 0.0001f) cZ = c * (float)Math.Sqrt(tempZ);
            else cZ = c * sinBeta;

            var vecC = new XnaVector3(cX, cY, cZ);
            return (vecA, vecB, vecC);
        }

        public void RefreshCavities()
        {
            lock (_cavitiesLock) BuildRepeatedCavities();
        }

        public void ClearOriginalCavities()
        {
            lock (_cavitiesLock)
            {
                _originalRenderCavities.Clear();
                _renderCavities.Clear();
                _showCavities = false;
                InvalidateSceneBatch();
            }
        }

        public void SetOriginalCavities(List<Cavity> cavities)
        {
            lock (_cavitiesLock)
            {
                _cavities = new List<Cavity>(cavities ?? new List<Cavity>());
                _originalRenderCavities.Clear();

                foreach (var source in _cavities)
                    _originalRenderCavities.Add(new RenderCavity(source));

                InvalidateSceneBatch();
            }
        }

        public void UpdateCavityColors()
        {
            lock (_cavitiesLock)
            {
                if (_cavities == null || _renderCavities == null) return;

                if (_molecule.ChargeMin < _molecule.ChargeMax)
                    ColorManager.SetDynamicRange(ColorManager.PropertyType.Charge,
                        _molecule.ChargeMin, _molecule.ChargeMax);

                if (_molecule.HydrophobicityMin < _molecule.HydrophobicityMax)
                    ColorManager.SetDynamicRange(ColorManager.PropertyType.Hydrophobicity,
                        _molecule.HydrophobicityMin, _molecule.HydrophobicityMax);

                var cavityLookup = _cavities.ToDictionary(c => c.Id, c => c);

                foreach (var rc in _renderCavities)
                {
                    if (cavityLookup.TryGetValue(rc.CavityId, out var source))
                    {
                        if (source.VertexColors4 != null && source.VertexColors4.Length > 0)
                        {
                            rc.VertexColors4 = source.VertexColors4;
                            rc.BaseColor4 = source.BaseColor4;
                        }
                        rc.NormalsCached = false;
                        rc.CachedNormals = null;
                    }
                }

                foreach (var orig in _originalRenderCavities)
                {
                    if (cavityLookup.TryGetValue(orig.CavityId, out var source))
                    {
                        if (source.VertexColors4 != null && source.VertexColors4.Length > 0)
                        {
                            orig.VertexColors4 = source.VertexColors4;
                            orig.BaseColor4 = source.BaseColor4;
                        }
                        orig.NormalsCached = false;
                        orig.CachedNormals = null;
                    }
                }

                InvalidateSceneBatch();
            }
        }

        public void CopyColorsToRenderCavities()
        {
            lock (_cavitiesLock)
            {
                if (_cavities == null || _renderCavities == null) return;

                foreach (var c in _cavities)
                {
                    if (c.VertexColors4 == null || c.VertexColors4.Length == 0) continue;

                    foreach (var rc in _renderCavities)
                    {
                        if (rc.CavityId == c.Id)
                        {
                            rc.VertexColors4 = c.VertexColors4;
                            rc.BaseColor4 = c.BaseColor4;
                            rc.NormalsCached = false;
                            rc.CachedNormals = null;
                            break;
                        }
                    }

                    foreach (var orig in _originalRenderCavities)
                    {
                        if (orig.CavityId == c.Id)
                        {
                            orig.VertexColors4 = c.VertexColors4;
                            orig.BaseColor4 = c.BaseColor4;
                            orig.NormalsCached = false;
                            orig.CachedNormals = null;
                            break;
                        }
                    }
                }

                InvalidateSceneBatch();
            }
        }

        public void InvalidateNormalsCache()
        {
            lock (_cavitiesLock)
                foreach (var rc in _renderCavities)
                {
                    rc.NormalsCached = false;
                    rc.CachedNormals = null;
                }

            lock (_molecularSurfacesLock)
                foreach (var rs in _renderMolecularSurfaces)
                {
                    rs.NormalsCached = false;
                    rs.CachedNormals = null;
                }
        }

        public void ForceRefresh()
        {
            lock (_cavitiesLock)
                foreach (var c in _renderCavities)
                {
                    c.NormalsCached = false;
                    c.CachedNormals = null;
                }

            lock (_molecularSurfacesLock)
                foreach (var s in _renderMolecularSurfaces)
                {
                    s.NormalsCached = false;
                    s.CachedNormals = null;
                }

            InvalidateSceneBatch();
        }

        public void ForceRefreshNormals() => ForceRefresh();

        public void InvalidateCache()
        {
            ForceRefresh();
            InvalidateLightingCache();
        }

        #endregion

        #region Molecular Surface Management

        public void SetMolecularSurfaces(List<MolecularSurface> molecularSurfaces)
        {
            lock (_molecularSurfacesLock)
            {
                _originalRenderMolecularSurfaces.Clear();
                _renderMolecularSurfaces.Clear();

                if (molecularSurfaces == null || molecularSurfaces.Count == 0)
                {
                    _showMolecularSurfaces = false;
                    return;
                }

                foreach (var source in molecularSurfaces)
                {
                    var rs = new RenderMolecularSurface(source);
                    _originalRenderMolecularSurfaces.Add(rs);
                    _renderMolecularSurfaces.Add(rs);
                }

                _showMolecularSurfaces = true;
                _currentShift = XnaVector3.Zero;
                ForceRefreshMolecularSurfaces();
            }
        }

        public void BuildRepeatedMolecularSurfaces()
        {
            lock (_molecularSurfacesLock)
            {
                if (_originalRenderMolecularSurfaces.Count == 0) return;

                int rx = Flags.Instance.repeat_x;
                int ry = Flags.Instance.repeat_y;
                int rz = Flags.Instance.repeat_z;

                var (vecA, vecB, vecC) = ComputeUnitCellVectors();

                if (rx == 1 && ry == 1 && rz == 1)
                {
                    _renderMolecularSurfaces.Clear();
                    foreach (var orig in _originalRenderMolecularSurfaces)
                        _renderMolecularSurfaces.Add(orig.CloneWithShift(XnaVector3.Zero));
                    ProgressUpdated?.Invoke($"Returned to original cell, {_renderMolecularSurfaces.Count} surfaces");
                    return;
                }

                _renderMolecularSurfaces.Clear();
                var newSurfaces = new List<RenderMolecularSurface>();

                foreach (var orig in _originalRenderMolecularSurfaces)
                    for (int x = 0; x < rx; x++)
                        for (int y = 0; y < ry; y++)
                            for (int z = 0; z < rz; z++)
                            {
                                var shift = x * vecA + y * vecB + z * vecC;
                                newSurfaces.Add(orig.CloneWithShift(shift));
                            }

                _renderMolecularSurfaces = newSurfaces;
                _showMolecularSurfaces = true;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public void UpdateMolecularSurfacePositions(XnaVector3 shift)
        {
            lock (_molecularSurfacesLock)
            {
                _currentShift = shift;
                foreach (var s in _renderMolecularSurfaces)
                    s.ApplyShift(shift);
            }
        }

        public void ForceRefreshMolecularSurfaces()
        {
            lock (_molecularSurfacesLock)
                foreach (var s in _renderMolecularSurfaces)
                {
                    s.NormalsCached = false;
                    s.CachedNormals = null;
                }
        }

        public void ClearMolecularSurfaces()
        {
            lock (_molecularSurfacesLock)
            {
                _renderMolecularSurfaces.Clear();
                _originalRenderMolecularSurfaces.Clear();
                _showMolecularSurfaces = false;
            }
        }

        public List<RenderMolecularSurface> GetMolecularSurfaces()
        {
            lock (_molecularSurfacesLock)
                return _renderMolecularSurfaces != null
                    ? new List<RenderMolecularSurface>(_renderMolecularSurfaces)
                    : new List<RenderMolecularSurface>();
        }

        public void ShowMolecularSurfaces(bool show)
        {
            lock (_molecularSurfacesLock) _showMolecularSurfaces = show;
        }

        public bool IsMolecularSurfacesShown()
        {
            lock (_molecularSurfacesLock) return _showMolecularSurfaces;
        }

        #endregion

        #region Mesh Generation

        public async Task RegenerateMeshWithSmoothingAsync(int smoothingLevel)
        {
            if (_isGenerating) return;
            _isGenerating = true;

            try
            {
                MeshParameters mcParams = GetMeshParametersForSmoothingLevel(smoothingLevel);

                List<Cavity> cavitiesToProcess;
                lock (_cavitiesLock)
                {
                    if (_cavities.Count == 0) return;
                    cavitiesToProcess = new List<Cavity>(_cavities);
                }

                await Task.Run(() =>
                {
                    var helpers = new CavityHelpers(_molecule);
                    MarchingCubesMeshGenerator.GenerateAllMeshesParallel(cavitiesToProcess, helpers, mcParams);

                    lock (_cavitiesLock)
                    {
                        foreach (var c in cavitiesToProcess)
                        {
                            if (c.TrianglesGenerated && c.Vertices?.Count > 0 && c.Indices?.Count > 0)
                            {
                                var mesh = new MeshDataXna
                                {
                                    Vertices = ToXnaList(c.Vertices),
                                    Indices = new List<int>(c.Indices)
                                };

                                foreach (var rc in _renderCavities)
                                    if (rc.CavityId == c.Id)
                                    {
                                        rc.BaseMarchingCubesMesh = mesh;
                                        rc.MarchingCubesMesh = mesh.CloneWithShiftXna(_currentShift);
                                        rc.MarchingCubesGenerated = true;
                                        rc.NormalsCached = false;
                                        rc.CachedNormals = null;
                                        break;
                                    }

                                foreach (var orig in _originalRenderCavities)
                                    if (orig.CavityId == c.Id)
                                    {
                                        orig.BaseMarchingCubesMesh = mesh;
                                        orig.MarchingCubesMesh = mesh.CloneWithShiftXna(XnaVector3.Zero);
                                        orig.MarchingCubesGenerated = true;
                                        orig.NormalsCached = false;
                                        orig.CachedNormals = null;
                                        break;
                                    }
                            }
                        }

                        InvalidateSceneBatch();
                    }
                });

                BuildRepeatedCavities();
                ForceRefresh();
                ProgressUpdated?.Invoke($"Mesh regenerated with smoothing={smoothingLevel}");
            }
            finally
            {
                _isGenerating = false;
            }
        }

        /// <summary>
        /// Maps the UI smoothing level (0–10, <see cref="Flags.smuth"/>) to marching-cubes mesh parameters.
        /// Single source of truth for detect + regenerate paths.
        /// </summary>
        public static MeshParameters GetMeshParametersForSmoothingLevel(int smoothingLevel)
        {
            var mcParams = new MeshParameters
            {
                IsoLevel = 0.0f,
                GenerateNormals = true,
                FixWinding = true
            };

            if (smoothingLevel == 0)
            {
                mcParams.SmoothingIterations = 0;
                mcParams.SmoothingFactor = 0.0f;
                mcParams.GaussianBlurIterations = 0;
                mcParams.GaussianSigma = 0.0f;
                mcParams.SnapTolerance = 0.0006f;
                mcParams.SmoothDensity = false;
                mcParams.GradientScale = 1.0f;
            }
            else if (smoothingLevel == 1)
            {
                mcParams.SmoothingIterations = 1;
                mcParams.SmoothingFactor = 0.35f;
                mcParams.GaussianBlurIterations = 0;
                mcParams.GaussianSigma = 0.0f;
                mcParams.SnapTolerance = 0.0006f;
                mcParams.SmoothDensity = true;
                mcParams.GradientScale = 1.0f;
            }
            else if (smoothingLevel == 2)
            {
                mcParams.SmoothingIterations = 2;
                mcParams.SmoothingFactor = 0.3f;
                mcParams.GaussianBlurIterations = 1;
                mcParams.GaussianSigma = 0.4f;
                mcParams.SnapTolerance = 0.0006f;
                mcParams.SmoothDensity = true;
                mcParams.GradientScale = 0.95f;
            }
            else if (smoothingLevel == 3)
            {
                mcParams.SmoothingIterations = 3;
                mcParams.SmoothingFactor = 0.25f;
                mcParams.GaussianBlurIterations = 1;
                mcParams.GaussianSigma = 0.5f;
                mcParams.SnapTolerance = 0.0005f;
                mcParams.SmoothDensity = true;
                mcParams.GradientScale = 0.90f;
            }
            else if (smoothingLevel == 4)
            {
                mcParams.SmoothingIterations = 3;
                mcParams.SmoothingFactor = 0.25f;
                mcParams.GaussianBlurIterations = 2;
                mcParams.GaussianSigma = 0.6f;
                mcParams.SnapTolerance = 0.0005f;
                mcParams.SmoothDensity = true;
                mcParams.GradientScale = 0.85f;
            }
            else if (smoothingLevel == 5)
            {
                mcParams.SmoothingIterations = 4;
                mcParams.SmoothingFactor = 0.30f;
                mcParams.GaussianBlurIterations = 2;
                mcParams.GaussianSigma = 0.7f;
                mcParams.SnapTolerance = 0.0003f;
                mcParams.SmoothDensity = true;
                mcParams.GradientScale = 0.80f;
            }
            else
            {
                mcParams.SmoothingIterations = Math.Min(smoothingLevel, 10);
                mcParams.SmoothingFactor = Math.Min(0.70f + (smoothingLevel - 5) * 0.05f, 1.0f);
                mcParams.GaussianBlurIterations = Math.Min(2 + (smoothingLevel - 5) / 2, 5);
                mcParams.GaussianSigma = Math.Min(1.0f + (smoothingLevel - 5) * 0.1f, 2.0f);
                mcParams.SnapTolerance = Math.Max(0.0006f - (smoothingLevel - 5) * 0.00005f, 0.0001f);
                mcParams.SmoothDensity = true;
                mcParams.GradientScale = Math.Max(1.0f - (smoothingLevel - 5) * 0.05f, 0.50f);
            }

            return mcParams;
        }

        public async Task GenerateAllDataAsync(bool generateMarchingCubes = false)
        {
            if (_isGenerating) return;
            _isGenerating = true;
            try
            {
                if (generateMarchingCubes)
                    await GenerateMarchingCubesMeshesAsync();
            }
            finally
            {
                _isGenerating = false;
            }
            ProgressUpdated?.Invoke("Ready");
        }

        public async Task GenerateMarchingCubesMeshesAsync(MeshParameters parameters = null)
        {
            List<Cavity> cavities;
            lock (_cavitiesLock)
            {
                if (_cavities.Count == 0) return;
                cavities = _cavities;
            }

            ForceRefreshNormals();

            if (parameters == null)
            {
                parameters = new MeshParameters
                {
                    IsoLevel = 0.0f,
                    GenerateNormals = true,
                    FixWinding = true,
                    SnapTolerance = 0.0006f
                };
            }

            parameters.ProgressCallback = (msg, percent, max) => ProgressUpdated?.Invoke(msg);
            ProgressUpdated?.Invoke($"MC: starting for {cavities.Count} cavities...");

            await Task.Run(() =>
            {
                var helpers = new CavityHelpers(_molecule);
                MarchingCubesMeshGenerator.GenerateAllMeshesParallel(cavities, helpers, parameters);
            });

            int updated = 0;
            lock (_cavitiesLock)
            {
                foreach (var c in cavities)
                {
                    if (c.TrianglesGenerated)
                    {
                        UpdateRenderCavity(c);
                        updated++;
                    }
                }

                InvalidateSceneBatch();
            }

            ProgressUpdated?.Invoke($"MC done: {updated}/{cavities.Count} cavities");
        }

        private void UpdateRenderCavity(Cavity source)
        {
            lock (_cavitiesLock)
            {
                foreach (var rc in _renderCavities)
                {
                    if (rc.CavityId != source.Id) continue;

                    rc.BaseColor4 = source.BaseColor4;
                    rc.VertexColors4 = source.VertexColors4;
                    rc.MaxSphereRadius = source.MaxSphereRadius;
                    rc.SphereCenter = source.SphereCenter;
                    rc.SphereFitCalculated = source.SphereFitCalculated;
                    rc.SphereFits = new List<SphereFitAnalyzer.SphereFitResult>(source.SphereFits);

                    if (source.Vertices?.Count > 0 && source.Indices?.Count > 0)
                    {
                        var mesh = new MeshDataXna
                        {
                            Vertices = ToXnaList(source.Vertices),
                            Indices = new List<int>(source.Indices ?? new List<int>())
                        };
                        rc.BaseMarchingCubesMesh = mesh;
                        rc.MarchingCubesMesh = mesh.CloneWithShiftXna(_currentShift);
                        rc.MarchingCubesGenerated = true;
                        rc.NormalsCached = false;
                        rc.CachedNormals = null;

                        rc.BaseCrystalChannelResults = new Dictionary<string, ChannelPathResultXna>();
                        rc.BaseCrystalChannelBothPaths = new Dictionary<string, (List<XnaVector3>, List<XnaVector3>)>();

                        if (source.CrystalChannelResults != null && source.CrystalChannelResults.Count > 0)
                        {
                            foreach (var kvp in source.CrystalChannelResults)
                            {
                                var result = kvp.Value;
                                var worldPathXna = result.WorldPathXna ?? new List<XnaVector3>();

                                var baseResult = new ChannelPathResultXna
                                {
                                    IsConnected = result.IsConnected,
                                    MinPath = new List<XnaVector3>(worldPathXna),
                                    MaxPath = new List<XnaVector3>(worldPathXna),
                                    WorldPath = new List<XnaVector3>(worldPathXna)
                                };
                                rc.BaseCrystalChannelResults[kvp.Key] = baseResult;
                                rc.BaseCrystalChannelBothPaths[kvp.Key] = (
                                    new List<XnaVector3>(worldPathXna),
                                    new List<XnaVector3>(worldPathXna)
                                );
                            }
                        }

                        rc.ApplyShift(_currentShift);
                    }
                    else
                    {
                        rc.MarchingCubesGenerated = false;
                        rc.NormalsCached = false;
                        rc.CachedNormals = null;
                    }

                    InvalidateSceneBatch();
                    return;
                }
            }
        }

        #endregion

        #region Channel Paths

        public void UpdateCrystalChannelPaths()
        {
            lock (_cavitiesLock)
            {
                foreach (var source in _cavities)
                {
                    if (source.CrystalChannelResults == null || source.CrystalChannelResults.Count == 0)
                        continue;

                    var rc = _renderCavities.FirstOrDefault(r => r.CavityId == source.Id);
                    if (rc != null) UpdateRenderCavityChannels(rc, source);

                    var orig = _originalRenderCavities.FirstOrDefault(r => r.CavityId == source.Id);
                    if (orig != null) UpdateRenderCavityChannels(orig, source);
                }

                BuildRepeatedCavities();
                InvalidateSceneBatch();
            }
        }

        private void UpdateRenderCavityChannels(RenderCavity rc, Cavity source)
        {
            if (rc == null || source == null) return;

            rc.BaseCrystalChannelResults = new Dictionary<string, ChannelPathResultXna>();
            rc.BaseCrystalChannelBothPaths = new Dictionary<string, (List<XnaVector3>, List<XnaVector3>)>();

            if (source.CrystalChannelResults != null)
            {
                foreach (var kvp in source.CrystalChannelResults)
                {
                    var result = kvp.Value;
                    if (result == null || !result.IsConnected) continue;

                    var worldPathXna = result.WorldPathXna ?? new List<XnaVector3>();
                    if (worldPathXna.Count == 0 && result.WorldPath != null && result.WorldPath.Count > 0)
                        worldPathXna = result.WorldPath.Select(v => new XnaVector3(v.X, v.Y, v.Z)).ToList();

                    if (worldPathXna.Count > 0)
                    {
                        var baseResult = new ChannelPathResultXna
                        {
                            IsConnected = result.IsConnected,
                            MinPath = new List<XnaVector3>(worldPathXna),
                            MaxPath = new List<XnaVector3>(worldPathXna),
                            WorldPath = new List<XnaVector3>(worldPathXna)
                        };
                        rc.BaseCrystalChannelResults[kvp.Key] = baseResult;
                        rc.BaseCrystalChannelBothPaths[kvp.Key] = (
                            new List<XnaVector3>(worldPathXna),
                            new List<XnaVector3>(worldPathXna)
                        );
                    }
                }
            }

            rc.ApplyShift(rc.CenterOffset);
        }

        #endregion

        #region Draw - Main Entry

        public void Draw(Matrix scaleRotate)
        {
            if (_camera == null || _graphicsDevice == null || _graphicsDevice.IsDisposed)
                return;

            if (_effect == null || _lightingEffect == null)
            {
                InitializeEffects();
                if (_effect == null || _lightingEffect == null) return;
            }

            if (_cullNoneRasterizer == null || _depthStencilState == null)
            {
                InitializeEffects();
                if (_cullNoneRasterizer == null || _depthStencilState == null) return;
            }

            EnsureLightingCacheValid();

            lock (_cavitiesLock)
            {
                if (_showCavities && _renderCavities.Count > 0)
                {
                    _cachedDrawList.Clear();
                    foreach (var rc in _renderCavities)
                    {
                        if (rc != null && rc.Visible)
                            _cachedDrawList.Add(rc);
                    }
                    DrawCavitiesInternal(scaleRotate);
                }
            }

            lock (_molecularSurfacesLock)
            {
                if (_showMolecularSurfaces && _renderMolecularSurfaces.Count > 0)
                    DrawMolecularSurfacesInternal(scaleRotate);
            }
        }

        private void DrawCavitiesInternal(Matrix scaleRotate)
        {
            if (_currentDrawMode == DrawMode.Hidden) return;

            bool drawChannelsOverlay = (_currentDrawMode == DrawMode.ChannelPathsOverlay);

            if (_currentDrawMode == DrawMode.ChannelPathsOnly)
            {
                DrawChannelPaths(scaleRotate, true);
                return;
            }

            if (_currentDrawMode == DrawMode.SphereFitsOnly)
            {
                DrawSphereFitsOnly(_cachedDrawList, scaleRotate);
                return;
            }

            bool useShader = _cavitySurfaceEffect != null
                && (_currentDrawMode == DrawMode.MarchingCubesMesh
                    || _currentDrawMode == DrawMode.MarchingCubesMeshPerVertex
                    || _currentDrawMode == DrawMode.CavityColorMap
                    || _currentDrawMode == DrawMode.TopologyColorMap
                    || _currentDrawMode == DrawMode.ChannelPathsOverlay);

            if (useShader)
                DrawCavitiesWithShader(scaleRotate);   // ← w CavityRenderer.Drawing.cs
            else
                DrawCavitiesFallback(scaleRotate);     // ← w CavityRenderer.Fallback.cs

            if (drawChannelsOverlay)
                DrawChannelPaths(scaleRotate, false);
        }

        #endregion

        #region Clear

        public void Clear()
        {
            lock (_cavitiesLock)
            {
                _cavities.Clear();
                _originalRenderCavities.Clear();
                _renderCavities.Clear();
                _cachedDrawList.Clear();
                _showCavities = false;
                _currentShift = XnaVector3.Zero;
                _lastAppliedShift = XnaVector3.Zero;
                _isGenerating = false;
                _currentDrawMode = DrawMode.Hidden;
            }

            lock (_molecularSurfacesLock)
            {
                _renderMolecularSurfaces.Clear();
                _originalRenderMolecularSurfaces.Clear();
                _showMolecularSurfaces = false;
                _currentShift = XnaVector3.Zero;
            }

            _lightingCache = new LightingCache { IsValid = false };
            _lightingCacheNeedsUpdate = true;

            _transparency = UpdateTransparency();
            if (_effect != null) _effect.Alpha = _transparency;
            if (_lightingEffect != null) _lightingEffect.Alpha = _transparency;

            _sphereModel = null;
            _sphereTransforms = null;
            _sphereMeshes = null;

            DisposeSceneBatch();

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void Reset()
        {
            Clear();
            _currentDrawMode = DrawMode.Hidden;
        }

        #endregion

        #region Getters

        public int CavityCount => _renderCavities.Count;
        public bool HasCavities => _showCavities && _renderCavities.Count > 0;
        public bool HasMolecularSurfaces => _showMolecularSurfaces && _renderMolecularSurfaces.Count > 0;
        public int MolecularSurfaceCount => _renderMolecularSurfaces.Count;

        public int GetTotalMarchingCubesTriangles()
        {
            lock (_cavitiesLock)
            {
                if (_renderCavities == null || _renderCavities.Count == 0) return 0;
                int total = 0;
                foreach (var rc in _renderCavities)
                    if (rc.MarchingCubesGenerated && rc.MarchingCubesMesh != null)
                        total += rc.MarchingCubesMesh.TriangleCount;
                return total;
            }
        }

        public int GetTotalVoxels()
        {
            lock (_cavitiesLock)
            {
                if (_renderCavities == null || _renderCavities.Count == 0) return 0;
                int total = 0;
                foreach (var rc in _renderCavities)
                    total += rc.BaseVoxels?.Length ?? 0;
                return total;
            }
        }

        public void CycleDrawMode()
        {
            var modes = Enum.GetValues(typeof(DrawMode));
            int idx = Array.IndexOf(modes, _currentDrawMode);
            _currentDrawMode = (DrawMode)modes.GetValue((idx + 1) % modes.Length);
            ProgressUpdated?.Invoke($"Mode: {_currentDrawMode}");
        }

        #endregion
    }
}