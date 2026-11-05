// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: CavityDetector.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Tal.Diagnostics;
using Tal.GUI;
using SysMatrix4x4 = System.Numerics.Matrix4x4;
using SysVector3 = System.Numerics.Vector3;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;

namespace Tal.Cavity
{
 /// <summary>
 /// Main cavity detection engine using SDF (Signed Distance Function) and flood fill.
 /// Supafterrts all crystal systems including triclinic (skewed cells) with proper fractional coordinate handling.
 /// Optimized for performance with parallel processing and minimal memory allocations.
 /// The SDF is computed directly for the requested VdW multiplier: every atom contributes
 /// d_i - (r_i * multiplier + probe). Changing the multiplier moves each element's surface by a
 /// different amount (r_i * delta), so the grid cannot be obtained from another multiplier by a
 /// constant shift. Any change of multiplier, probe radius, resolution or anisotropic mode
 /// therefore rebuilds the atoms and the SDF from scratch.
 /// </summary>
    public class CavityDetector : CavityDetectorBase
    {
        #region Constants

 /// <summary>
 /// Default (initial) VdW multiplier, also the contact scale used by VoxelScanner to group atoms
 /// into molecules. The SDF itself is no longer derived from this value.
 /// </summary>
        public const float BASE_SDF_RADIUS = 1.0f;

 /// <summary>Scan margin in Angstroms - expands supercell and determines neighbor atom range.</summary>
        public const float ScanMargin = 2f;

        #endregion

        #region Control Parameters

 /// <summary>Probe radius for surface afterint detection.</summary>
        public float ProbeRadius { get; set; } = 0.0f;

 /// <summary>Minimum volume for a cavity to be reafterrted (in A³).</summary>
        public static float MinCavityVolume { get; set; } = 0.1f;

 /// <summary>Van der Waals radius multiplier for non-hydrogen atoms.</summary>
        public float RadiusMultiplier { get; set; } = 1.1f;

 /// <summary>Whether to use crystallographic symmetry for atom generation.</summary>
        public bool UseSymmetry { get; set; } = true;

 /// <summary>Whether to use symmetry for isosurface extraction.</summary>
        public bool UseSymmetryForIsoSurface { get; set; } = true;

 /// <summary>Voxel scanner instance.</summary>
        public VoxelScanner Scanner { get; private set; }

 /// <summary>Whether to generate isosurface afterints.</summary>
        public bool GenerateIsoSurfacePoints { get; set; } = true;

        #endregion

        #region Base SDF Cache

        private float[,,] _baseSDFGrid;
        private float _baseVdwRadius = 0.5f;
        private bool _hasBaseSDF = false;

        #endregion

        #region State Tracking

        private float _lastAppliedResolution = 0.15f;
        private bool _lastUseThermalMotion = false;
        private float _lastAppliedVdw = BASE_SDF_RADIUS;
        private float _lastAppliedProbe = 0f;

        #endregion

        #region Detection Results

 /// <summary>List of detected cavities.</summary>
        public List<Cavity> Cavities { get; private set; } = new List<Cavity>();

 /// <summary>Current status message for progress reafterrting.</summary>
        public static string StatusMessage { get; private set; } = "";

 /// <summary>Total volume of all detected voids in A³.</summary>
        public static float TotalVoidsVolume { get; private set; } = 0;

 /// <summary>Total number of detected voids.</summary>
        public int TotalVoidsCount { get; private set; } = 0;

 /// <summary>Progress callback with message and percentage.</summary>
        public Action<string, float> ProgressChanged { get; set; }

 /// <summary>Gets the molecule instance.</summary>
        public Molecule Molecule => _molecule;

        #endregion

        #region Private Fields

        private readonly Molecule _molecule;
        private SysVector3 _minCorner;
        private SysVector3 _maxCorner;
        private BoundingBox _trueCellBox;
        private float _cellVolume;
        private int _symmetryCount;
        private BoundingBox _fullCellBox;

        private int _atomCount;
        private readonly int _maxParallelism;
        private readonly ConcurrentDictionary<int, List<FastVec3>> _isoSurfaceCache =
            new ConcurrentDictionary<int, List<FastVec3>>();

        private Matrix _invFracToCart;
        private Matrix _fracToCart;

        private float[,,] _periodicDensityCache;
        private int[,,] _regionMapCache;

        private VoxelScanner.VoxelGrid _fineGrid;
        private VoxelScanner.VoxelGrid _periodicGrid;
        private SysVector3 _supercellMin;
        private SysVector3 _cellMin;
        private SysVector3 _cellMax;

        private int _marginX, _marginY, _marginZ;
        private int _periodicResX, _periodicResY, _periodicResZ;

        private SysMatrix4x4 _sysFracToCart;
        private SysMatrix4x4 _sysInvFracToCart;

        private float[,,] _supercellGradientX;
        private float[,,] _supercellGradientY;
        private float[,,] _supercellGradientZ;

        #endregion

        #region FastVec3 Struct

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public readonly struct FastVec3
        {
            public readonly float X, Y, Z;

            public FastVec3(float x, float y, float z) => (X, Y, Z) = (x, y, z);
            public FastVec3(SysVector3 v) => (X, Y, Z) = (v.X, v.Y, v.Z);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator SysVector3(FastVec3 v) => new SysVector3(v.X, v.Y, v.Z);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator FastVec3(SysVector3 v) => new FastVec3(v.X, v.Y, v.Z);
        }

        public class FastVec3Comparer : IEqualityComparer<FastVec3>
        {
            private const float Tolerance = 0.0001f;

            public bool Equals(FastVec3 a, FastVec3 b)
            {
                return Math.Abs(a.X - b.X) < Tolerance &&
                       Math.Abs(a.Y - b.Y) < Tolerance &&
                       Math.Abs(a.Z - b.Z) < Tolerance;
            }

            public int GetHashCode(FastVec3 v)
            {
                int ix = (int)Math.Round(v.X / Tolerance);
                int iy = (int)Math.Round(v.Y / Tolerance);
                int iz = (int)Math.Round(v.Z / Tolerance);

                unchecked
                {
                    int hash = ix;
                    hash = hash * 397 ^ iy;
                    hash = hash * 397 ^ iz;
                    return hash;
                }
            }
        }

        #endregion

        #region Constructor

        public CavityDetector(Molecule molecule)
        {
            if (molecule == null)
                throw new ArgumentNullException(nameof(molecule), "Molecule cannot be null");

            float vdwMult = Flags.Instance.VdWValue;
            RadiusMultiplier = vdwMult;

            LogToDebug("========== CAVITY DETECTOR CONSTRUCTOR START ==========", System.Drawing.Color.Cyan);
            LogToDebug($"Molecule: {molecule.NumberOfAtoms} atoms", System.Drawing.Color.LightGray);

            _molecule = molecule;
            _maxParallelism = OperatingSystem.IsAndroid() ? 3 : Math.Max(1, Environment.ProcessorCount / 2);

            Scanner = new VoxelScanner(molecule);

            Scanner.RadiusMultiplier = RadiusMultiplier;
            Scanner.ProbeRadius = ProbeRadius;
            Scanner.UseSymmetry = UseSymmetry;
            Scanner.ScanMargin = ScanMargin;

            _minCorner = (SysVector3)Scanner.MinCorner;
            _maxCorner = (SysVector3)Scanner.MaxCorner;

            var minXna = new XnaVector3(_minCorner.X, _minCorner.Y, _minCorner.Z);
            var maxXna = new XnaVector3(_maxCorner.X, _maxCorner.Y, _maxCorner.Z);

            _cellVolume = Scanner.CellVolume;
            _symmetryCount = GetSymmetryCount(_molecule);
            _fullCellBox = new BoundingBox(minXna, maxXna);

            _fracToCart = Scanner.FracToCart;
            _invFracToCart = Matrix.Invert(_fracToCart);

            _sysFracToCart = new SysMatrix4x4(
                _fracToCart.M11, _fracToCart.M12, _fracToCart.M13, _fracToCart.M14,
                _fracToCart.M21, _fracToCart.M22, _fracToCart.M23, _fracToCart.M24,
                _fracToCart.M31, _fracToCart.M32, _fracToCart.M33, _fracToCart.M34,
                _fracToCart.M41, _fracToCart.M42, _fracToCart.M43, _fracToCart.M44
            );

            _sysInvFracToCart = SysMatrix4x4.Invert(_sysFracToCart, out var inv) ? inv : SysMatrix4x4.Identity;

            _trueCellBox = GetTrueCellBoundingBox();

            LogToDebug($"Cell volume: {_cellVolume:F2} Å³", System.Drawing.Color.LightGray);
            LogToDebug($"Symmetry count: {_symmetryCount}", System.Drawing.Color.LightGray);
            LogToDebug($"Max parallelism: {_maxParallelism}", System.Drawing.Color.LightGray);
            LogToDebug($"Initialized with VdW = {RadiusMultiplier:F2}", System.Drawing.Color.LightGreen);
            LogToDebug("========== CONSTRUCTOR END ==========", System.Drawing.Color.Cyan);

            UpdateProgress("Initializing...", 0);
        }

        #endregion

        #region Private Logging Methods
 //
 // ⭐ REFACTOR: LogToDebug / LogProgress delegate to SafeDebugLogger.
 // - Worker threads: only ConcurrentQueue.Enqueue (~50-100 ns)
 // - UI thread: Drain() once per second (Timer in MainForm)
 // - ZERO Application.OpenForms w hot path
 // - ZERO cross-thread UI calls z threads roboczych
 //

 /// <summary>
 /// Logs a message via SafeDebugLogger. Thread-safe, non-blocking.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogToDebug(string message, System.Drawing.Color? color = null)
        {
            SafeDebugLogger.LogTagged("[CAVITY]", message, color);
        }

 /// <summary>
 /// Logs progress information with percentage.
 /// UpdateProgress() marshalluje na UI thread (BeginInvoke).
 /// </summary>
        private void LogProgress(string message, float percent, System.Drawing.Color? color = null)
        {
            SafeDebugLogger.LogTagged(
                "[CAVITY]",
                $"[{percent:F0}%] {message}",
                color ?? System.Drawing.Color.LightBlue);

            UpdateProgress(message, percent);
        }

        #endregion

        #region Public Methods - Results
        public int GridResX => _periodicResX;
        public int GridResY => _periodicResY;
        public int GridResZ => _periodicResZ;
        public int GridTotalVoxels => _periodicResX * _periodicResY * _periodicResZ;
        public int MarginX => _marginX;
        public int MarginY => _marginY;
        public int MarginZ => _marginZ;
        public int SupercellResX => _fineGrid?.ResX ?? 0;
        public int SupercellResY => _fineGrid?.ResY ?? 0;
        public int SupercellResZ => _fineGrid?.ResZ ?? 0;
        public bool HasGridPrepared => _fineGrid != null;
        public List<Cavity> GetCavities()
        {
            return Cavities != null ? new List<Cavity>(Cavities) : new List<Cavity>();
        }

        public int CavityCount => Cavities?.Count ?? 0;

        public bool HasCavities => Cavities != null && Cavities.Count > 0;

        #endregion

        #region Public Methods - Base SDF Management

        public void SyncVdwMultiplierFromFlags()
        {
            float multiplier = Flags.Instance.VdWValue;
            if (Math.Abs(RadiusMultiplier - multiplier) > 0.001f)
            {
                LogToDebug($"Syncing VdW multiplier: {RadiusMultiplier:F2} -> {multiplier:F2}", System.Drawing.Color.Yellow);
                RadiusMultiplier = multiplier;

                if (Scanner != null)
                {
                    Scanner.RadiusMultiplier = multiplier;
                }

                Flags.Instance.SDFNeedsRebuild = true;
            }
        }

        public bool HasBaseSDF => _hasBaseSDF;
        public float BaseVdwRadius => _baseVdwRadius;
        public float[,,] GetBaseSDF() => _baseSDFGrid;

        public void SetBaseSDF(float[,,] sdf, float vdwRadius)
        {
            _baseSDFGrid = sdf;
            _baseVdwRadius = vdwRadius;
            _hasBaseSDF = sdf != null;
            LogToDebug($"Base SDF set: VdW={vdwRadius:F2}, Grid size={sdf?.GetLength(0) ?? 0}x{sdf?.GetLength(1) ?? 0}x{sdf?.GetLength(2) ?? 0}", System.Drawing.Color.LightGreen);
        }

        public void ClearBaseSDF()
        {
            _baseSDFGrid = null;
            _hasBaseSDF = false;
            _baseVdwRadius = 0.5f;

 // ⭐ Reset flag SDF
            Flags.Instance.SDFNeedsRebuild = false;

            LogToDebug("Base SDF cleared", System.Drawing.Color.Orange);
        }

        public void NotifyAnisotropicModeChanged()
        {
            LogToDebug($"Thermal motion changed to: {Flags.Instance.UseThermalMotion}", System.Drawing.Color.Yellow);
            Flags.Instance.SDFNeedsRebuild = true;
        }

        public void NotifyResolutionChanged(float newResolution)
        {
            LogToDebug($"Resolution changed to: {newResolution:F3}", System.Drawing.Color.Yellow);
            Flags.Instance.Resolution = newResolution;
            Flags.Instance.SDFNeedsRebuild = true;

            if (Scanner != null)
            {
                Scanner.VoxelSize = newResolution;
                Scanner.UpdateVoxelSize(newResolution);
            }
        }

        public void ForceReset()
        {
            LogToDebug("ForceReset - clearing all data", System.Drawing.Color.Orange);

            try
            {
                ClearBaseSDF();
            }
            catch { /* ignore */ }

            try
            {
                Clear();
            }
            catch { /* ignore */ }

            try
            {
                Scanner?.Reset();
            }
            catch { /* ignore */ }

 // ⭐ Reset flag SDF
            Flags.Instance.SDFNeedsRebuild = false;
            Flags.Instance.ForceSDFRecalculation = false;

 // ⭐ Reset state tracking
            _lastAppliedResolution = 0.15f;
            _lastUseThermalMotion = false;
            _lastAppliedVdw = BASE_SDF_RADIUS;
            _lastAppliedProbe = 0f;
        }

        #endregion

        #region Public Methods - Parameter Updates

        public void UpdateVdwMultiplier(float multiplier)
        {
            LogToDebug($"Updating VdW multiplier to: {multiplier:F2}", System.Drawing.Color.LightGray);
            RadiusMultiplier = multiplier;

            if (Scanner != null)
            {
                Scanner.RadiusMultiplier = multiplier;
            }
        }

        public void UpdateRes(float resolution)
        {
            LogToDebug($"Updating resolution to: {resolution:F4} A", System.Drawing.Color.LightGray);

            Scanner?.UpdateVoxelSize(resolution);
        }

        #endregion

        #region Public Methods - Detection

        public async Task<bool> ComputeBaseSDFAsync(float baseVdwRadius = 0.8f)
        {
            LogToDebug("========== COMPUTE BASE SDF START ==========", System.Drawing.Color.Cyan);
            LogToDebug($"Base VdW radius: {baseVdwRadius:F2}", System.Drawing.Color.LightGray);

            if (_hasBaseSDF && Math.Abs(_baseVdwRadius - baseVdwRadius) < 0.001f)
            {
                LogToDebug($"Base SDF already exists for VdW={_baseVdwRadius:F2}", System.Drawing.Color.LightGreen);
                UpdateProgress($"Base SDF already exists (VdW={_baseVdwRadius:F2})", 40);
                return true;
            }

            float oldRadiusMult = RadiusMultiplier;

            try
            {
                RadiusMultiplier = baseVdwRadius;
                if (Scanner != null)
                {
                    Scanner.RadiusMultiplier = baseVdwRadius;

                    float currentResolution = Flags.Instance.Resolution > 0 ? Flags.Instance.Resolution : Scanner.VoxelSize;
                    if (Math.Abs(Scanner.VoxelSize - currentResolution) > 0.0001f)
                    {
                        Scanner.VoxelSize = currentResolution;
                        LogToDebug($"Using resolution: {currentResolution:F3}", System.Drawing.Color.LightGray);
                    }
                }

 // Atom radii (r_i * multiplier + probe) are baked into the atoms when they are generated,
 // so the atoms must be regenerated for every new multiplier / probe radius.
                LogProgress("Generating atoms...", 8);
                if (Scanner != null) Scanner.ProbeRadius = ProbeRadius;
                Scanner.GenerateAsymmetricAtomsWithNeighbors();
                LogToDebug($"Generated {Scanner.Atoms?.Length ?? 0} atoms", System.Drawing.Color.LightGray);

                LogProgress("Preparing grid...", 12);
                if (!PrepareGridForDetection())
                {
                    LogToDebug("Failed to prepare grid", System.Drawing.Color.Red);
                    return false;
                }

                LogProgress("Computing SDF...", 20);
                var atoms = Scanner.Atoms;
                var (_, densityGrid) = ComputeSDFWithDensityOptimized(_fineGrid, atoms, atoms?.Length ?? 0);

                _baseSDFGrid = CopyDensityGrid(densityGrid);
                _baseVdwRadius = baseVdwRadius;
                _hasBaseSDF = true;

                LogToDebug($"Base SDF computed: {_baseSDFGrid.GetLength(0)}x{_baseSDFGrid.GetLength(1)}x{_baseSDFGrid.GetLength(2)}", System.Drawing.Color.LightGreen);
                LogToDebug($"Base VdW radius: {_baseVdwRadius:F2}", System.Drawing.Color.LightGreen);
                LogToDebug("========== COMPUTE BASE SDF END ==========", System.Drawing.Color.Cyan);

                UpdateProgress($"Base SDF computed for VdW={baseVdwRadius:F2}", 40);
                return true;
            }
            catch (Exception ex)
            {
                LogToDebug($"Error computing base SDF: {ex.Message}", System.Drawing.Color.Red);
                _hasBaseSDF = false;
                return false;
            }
            finally
            {
                RadiusMultiplier = oldRadiusMult;
                if (Scanner != null)
                {
                    Scanner.RadiusMultiplier = oldRadiusMult;
                }
            }
        }

        public async Task<bool> DetectVoidsAsync(
            float targetVdwRadius = 1.0f,
            CancellationToken cancellationToken = default)
        {
            LogToDebug("========== DETECT_VOIDS_START ==========", System.Drawing.Color.Cyan);
            LogToDebug($"Target VdW: {targetVdwRadius:F2}", System.Drawing.Color.LightGray);
            LogToDebug($"Resolution: {Flags.Instance.Resolution:F3}", System.Drawing.Color.LightGray);
            LogToDebug($"Thermal motion changed to: {Flags.Instance.UseThermalMotion}", System.Drawing.Color.LightGray);
            LogToDebug($"Base SDF exists: {_hasBaseSDF}", System.Drawing.Color.LightGray);

            cancellationToken.ThrowIfCancellationRequested();

 // Step 1: decide whether atoms + SDF have to be (re)computed.
 // Atom radii (r_i * multiplier + probe) are baked into Scanner.Atoms and into the SDF, and an
 // SDF cannot be converted between multipliers by a constant shift (each element's surface
 // moves by r_i * delta). Any change below therefore forces a rebuild for the requested multiplier.
            bool resolutionChanged = Math.Abs(_lastAppliedResolution - Flags.Instance.Resolution) > 0.001f;
            bool anisotropicChanged = _lastUseThermalMotion != Flags.Instance.UseThermalMotion;
            bool vdwChanged = Math.Abs(_lastAppliedVdw - targetVdwRadius) > 0.001f;
            bool probeChanged = Math.Abs(_lastAppliedProbe - ProbeRadius) > 0.001f;
            bool forced = Flags.Instance.SDFNeedsRebuild || Flags.Instance.ForceSDFRecalculation;
            bool sdfIsStale = !_hasBaseSDF || Math.Abs(_baseVdwRadius - targetVdwRadius) > 0.001f;
            bool needsRebuild = resolutionChanged || anisotropicChanged || vdwChanged || probeChanged || forced || sdfIsStale;

            LogToDebug($"Changes: Res={resolutionChanged}, Aniso={anisotropicChanged}, VdW={vdwChanged}, Probe={probeChanged}, Forced={forced}, Stale={sdfIsStale}", System.Drawing.Color.LightGray);

            if (needsRebuild)
            {
                LogToDebug($"SDF REBUILD for VdW={targetVdwRadius:F2}, probe={ProbeRadius:F2}, res={Flags.Instance.Resolution:F3}", System.Drawing.Color.Yellow);

                ClearBaseSDF();
                Flags.Instance.ClearSDFRebuildFlags();

                LogProgress("Preparing scanner...", 5);
                RadiusMultiplier = targetVdwRadius;

                if (Scanner != null)
                {
                    Scanner.VoxelSize = Flags.Instance.Resolution;
                    Scanner.UpdateVoxelSize(Flags.Instance.Resolution);
                    Scanner.RadiusMultiplier = targetVdwRadius;
                    Scanner.ProbeRadius = ProbeRadius;
                }

                LogProgress("Computing SDF...", 10);
                bool sdfOk = await ComputeBaseSDFAsync(targetVdwRadius);
                if (!sdfOk)
                {
                    LogToDebug("Failed to compute SDF", System.Drawing.Color.Red);
                    return false;
                }

                _lastAppliedResolution = Flags.Instance.Resolution;
                _lastUseThermalMotion = Flags.Instance.UseThermalMotion;
                _lastAppliedVdw = targetVdwRadius;
                _lastAppliedProbe = ProbeRadius;

                LogToDebug($"SDF computed directly for VdW={targetVdwRadius:F2}", System.Drawing.Color.LightGreen);
            }

            if (!IsResolutionValidForCell(Scanner.VoxelSize, _molecule))
            {
                float recommended = GetRecommendedResolution(_molecule);
                string msg_ = $"Resolution {Scanner.VoxelSize:F3} A is too fine. Please increase to at least {recommended:F3} A";
                LogToDebug(msg_, System.Drawing.Color.Orange);
                StatusMessage = msg_;
                MainForm.Instance?.PostStatus($"ERROR: {msg_}", "", "❌");
                ProgressChanged?.Invoke(msg_, 0);
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

 // Step 2: working density = SDF computed directly for the requested multiplier
            LogProgress("Preparing density grid...", 30);

            float[,,] workingDensity = CopyDensityGrid(_baseSDFGrid);
            LogProgress($"SDF ready (VdW={targetVdwRadius:F2})", 50);

            if (workingDensity == null)
            {
                LogToDebug("Failed to get working density!", System.Drawing.Color.Red);
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

 // Step 3: Prepare grid
            LogProgress("Preparing grid...", 55);
            if (!PrepareGridForDetection())
            {
                string msg_ = "Failed to prepare grid for detection!";
                LogToDebug(msg_, System.Drawing.Color.Red);
                StatusMessage = msg_;
                MainForm.Instance?.PostStatus($"ERROR: {msg_}", "", "❌");
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

 // Step 4: Copy density to periodic cache
            LogProgress("Copying density to periodic grid...", 60);

            int srcResX = workingDensity.GetLength(0);
            int srcResY = workingDensity.GetLength(1);
            int srcResZ = workingDensity.GetLength(2);

            LogToDebug($"Density grid dimensions: {srcResX}x{srcResY}x{srcResZ}", System.Drawing.Color.LightGray);

            int cellResX = _periodicResX;
            int cellResY = _periodicResY;
            int cellResZ = _periodicResZ;

            int cellStartX = _marginX;
            int cellStartY = _marginY;
            int cellStartZ = _marginZ;

            _periodicDensityCache = new float[cellResX, cellResY, cellResZ];

            int rowSizeBytes = cellResZ * sizeof(float);
            int srcRowSizeBytes = srcResZ * sizeof(float);

            Parallel.For(0, cellResX, new ParallelOptions { MaxDegreeOfParallelism = _maxParallelism }, x =>
            {
                int sx = x + cellStartX;
                int srcBase = ((sx * srcResY) + cellStartY) * srcResZ + cellStartZ;
                int dstBase = (x * cellResY) * cellResZ;

                for (int y = 0; y < cellResY; y++)
                {
                    int srcOffset = srcBase + y * srcResZ;
                    int dstOffset = dstBase + y * cellResZ;
                    Buffer.BlockCopy(
                        workingDensity, srcOffset * sizeof(float),
                        _periodicDensityCache, dstOffset * sizeof(float),
                        rowSizeBytes
                    );
                }
            });

            cancellationToken.ThrowIfCancellationRequested();

 // Step 5: Compute gradient
            LogProgress("Computing gradient...", 65);

            bool isNonOrthogonal = CellParameters.IsNonOrthogonal();

            float[,,] gradX, gradY, gradZ;

            if (isNonOrthogonal)
            {
                LogToDebug("Non-orthogonal cell detected - using metric tensor for gradient", System.Drawing.Color.LightGray);
                (gradX, gradY, gradZ) = ComputeGradientWithMetricFastest(
                    workingDensity,
                    srcResX, srcResY, srcResZ
                );
            }
            else
            {
                LogToDebug("Orthogonal cell - using simple gradient", System.Drawing.Color.LightGray);
                (gradX, gradY, gradZ) = ComputeGradientSimpleFast(
                    workingDensity,
                    srcResX, srcResY, srcResZ
                );
            }

            _supercellGradientX = gradX;
            _supercellGradientY = gradY;
            _supercellGradientZ = gradZ;

            LogToDebug("Gradient computed", System.Drawing.Color.LightGreen);

 // Step 6: Copy gradient to periodic cell
            LogProgress("Copying gradient to periodic cell...", 70);

            float[,,] periodicGradX = new float[cellResX, cellResY, cellResZ];
            float[,,] periodicGradY = new float[cellResX, cellResY, cellResZ];
            float[,,] periodicGradZ = new float[cellResX, cellResY, cellResZ];

            int maxParallelGradCopy = Math.Max(1, Environment.ProcessorCount / 2);

            Parallel.For(0, cellResX, new ParallelOptions { MaxDegreeOfParallelism = maxParallelGradCopy }, x =>
            {
                int sx = x + cellStartX;
                for (int y = 0; y < cellResY; y++)
                {
                    int sy = y + cellStartY;
                    for (int z = 0; z < cellResZ; z++)
                    {
                        int sz = z + cellStartZ;

                        if (sx >= 0 && sx < srcResX && sy >= 0 && sy < srcResY && sz >= 0 && sz < srcResZ)
                        {
                            periodicGradX[x, y, z] = _supercellGradientX[sx, sy, sz];
                            periodicGradY[x, y, z] = _supercellGradientY[sx, sy, sz];
                            periodicGradZ[x, y, z] = _supercellGradientZ[sx, sy, sz];
                        }
                        else
                        {
                            periodicGradX[x, y, z] = 0f;
                            periodicGradY[x, y, z] = 0f;
                            periodicGradZ[x, y, z] = 0f;
                        }
                    }
                }
            });

            cancellationToken.ThrowIfCancellationRequested();

 // Step 7: Detect cavities with flood fill
            LogProgress("Detecting cavities with flood fill...", 75);

            float wallThreshold = Math.Min(
                0.02f,
                Math.Min(0.5f / cellResX, Math.Min(0.5f / cellResY, 0.5f / cellResZ))
            );

            float voxelVolume = CellParameters.IsInitialized ?
                CellParameters.GetVoxelVolume() :
                Scanner.VoxelSize * Scanner.VoxelSize * Scanner.VoxelSize;

            int minVoxelCount = (int)Math.Ceiling(MinCavityVolume / voxelVolume);
            if (minVoxelCount < 1) minVoxelCount = 1;

            LogToDebug($"Voxel volume: {voxelVolume:F6}, min voxels: {minVoxelCount}", System.Drawing.Color.LightGray);

            var periodicGrid = _periodicGrid;

            var fillAndSpread = new CavityFillAndSpread();

            var regions = fillAndSpread.DetectCavities(
                _periodicDensityCache,
                periodicGrid,
                _invFracToCart,
                _cellMin,
                MinCavityVolume,
                minVoxelCount,
                wallThreshold,
                0, 0, 0,
                voxelVolume
            );

            LogToDebug($"Detection found {regions.Count} regions", System.Drawing.Color.LightGreen);

            Cavities.Clear();

            if (regions.Count == 0)
            {
                string msg_ = "No cavities found. Try increasing voxel size or decreasing MinCavityVolume.";
                LogToDebug(msg_, System.Drawing.Color.Orange);
                StatusMessage = msg_;
                MainForm.Instance?.PostStatus($"INFO: {msg_}", "", "ℹ️");
                UpdateProgress("No cavities found", 100);
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

 // Step 8: Create cavity objects
            LogProgress("Creating cavity objects...", 85);

            int totalResX = srcResX;
            int totalResY = srcResY;
            int totalResZ = srcResZ;

            float[,,] graphicDensity = new float[totalResX, totalResY, totalResZ];

            Parallel.For(0, totalResX, new ParallelOptions { MaxDegreeOfParallelism = _maxParallelism }, x =>
            {
                for (int y = 0; y < totalResY; y++)
                {
                    for (int z = 0; z < totalResZ; z++)
                    {
                        graphicDensity[x, y, z] = -1.0f;
                    }
                }
            });

            Parallel.For(0, cellResX, new ParallelOptions { MaxDegreeOfParallelism = _maxParallelism }, x =>
            {
                int gx = x + cellStartX;
                for (int y = 0; y < cellResY; y++)
                {
                    int gy = y + cellStartY;
                    for (int z = 0; z < cellResZ; z++)
                    {
                        int gz = z + cellStartZ;
                        float val = _periodicDensityCache[x, y, z];
                        if (val >= 0)
                        {
                            graphicDensity[gx, gy, gz] = val;
                        }
                    }
                }
            });

            float totalVolume = 0;
            int cavityCount = regions.Count;
            int processedRegions = 0;

            for (int i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                float volume = region.Voxels.Count * voxelVolume;
                totalVolume += volume;

                bool[,,] voxelMask = new bool[totalResX, totalResY, totalResZ];
                foreach (var voxel in region.Voxels)
                {
                    int gx = voxel.x + cellStartX;
                    int gy = voxel.y + cellStartY;
                    int gz = voxel.z + cellStartZ;
                    if (gx >= 0 && gx < totalResX && gy >= 0 && gy < totalResY && gz >= 0 && gz < totalResZ)
                    {
                        voxelMask[gx, gy, gz] = true;
                    }
                }

                var cavity = new Cavity
                {
                    Id = region.Id,
                    Voxels = region.WorldPositions,
                    Volume = volume,
                    Center = region.Center,
                    BoundingBox = new BoundingBox(
                        new XnaVector3(region.BoundingBoxMin.X, region.BoundingBoxMin.Y, region.BoundingBoxMin.Z),
                        new XnaVector3(region.BoundingBoxMax.X, region.BoundingBoxMax.Y, region.BoundingBoxMax.Z)
                    ),
                    TrueCellBox = new BoundingBox(
                        new XnaVector3(_cellMin.X, _cellMin.Y, _cellMin.Z),
                        new XnaVector3(_cellMax.X, _cellMax.Y, _cellMax.Z)
                    ),
                    BaseColor4 = new XnaVector4(
                        region.DisplayColor.R / 255f,
                        region.DisplayColor.G / 255f,
                        region.DisplayColor.B / 255f,
                        region.DisplayColor.A / 255f
                    ),
                    IsExterior = region.IsExterior,

                    DensityGrid = graphicDensity,
                    Grid = _fineGrid,
                    InvFracToCart = _invFracToCart,
                    MinCorner = _minCorner,
                    DensityNx = totalResX,
                    DensityNy = totalResY,
                    DensityNz = totalResZ,
                    VoxelMask = voxelMask,

                    WallXMin = region.WallXMin,
                    WallXMax = region.WallXMax,
                    WallYMin = region.WallYMin,
                    WallYMax = region.WallYMax,
                    WallZMin = region.WallZMin,
                    WallZMax = region.WallZMax,

                    TouchesXMin = region.TouchesXMin,
                    TouchesXMax = region.TouchesXMax,
                    TouchesYMin = region.TouchesYMin,
                    TouchesYMax = region.TouchesYMax,
                    TouchesZMin = region.TouchesZMin,
                    TouchesZMax = region.TouchesZMax,

                    ConnectedCavities = region.ConnectedCavities,
                    PeriodicGroupId = region.PeriodicGroupId,
                    IsPeriodicDuplicate = region.IsPeriodicDuplicate,

                    TrianglesGenerated = false,

                    PeriodicGrid = periodicGrid,
                    OffsetX = _marginX,
                    OffsetY = _marginY,
                    OffsetZ = _marginZ,

                    SupercellGradientX = _supercellGradientX,
                    SupercellGradientY = _supercellGradientY,
                    SupercellGradientZ = _supercellGradientZ
                };

 // Calculate voxel properties with accurate fractional coordinates
                if (cavity.Voxels != null && cavity.Voxels.Count > 0)
                {
                    int voxelCount = cavity.Voxels.Count;
                    cavity.VoxelCharges = new float[voxelCount];
                    cavity.VoxelHydrophobicity = new float[voxelCount];
                    cavity.VoxelZEffective = new float[voxelCount];
                    cavity.VoxelFracX = new float[voxelCount];
                    cavity.VoxelFracY = new float[voxelCount];
                    cavity.VoxelFracZ = new float[voxelCount];

                    var molecule = _molecule;
                    float searchRadius = 3.0f;
                    float searchRadiusSq = searchRadius * searchRadius;

                    int atomCountLocal = molecule?.NumberOfAtoms ?? 0;
                    Vector3[] atomPos = null;
                    float[] atomCharge = null;
                    float[] atomHydro = null;
                    float[] atomZ = null;

                    if (atomCountLocal > 0 && molecule.atom != null)
                    {
                        atomPos = new Vector3[atomCountLocal];
                        atomCharge = new float[atomCountLocal];
                        atomHydro = new float[atomCountLocal];
                        atomZ = new float[atomCountLocal];

                        for (int j = 0; j < atomCountLocal; j++)
                        {
                            var atom = molecule.atom[j];
                            atomPos[j] = new Vector3(atom.Coordinate.X, atom.Coordinate.Y, atom.Coordinate.Z);
                            atomCharge[j] = atom.charge;
                            atomHydro[j] = atom.Hydrophobicity;
                            atomZ[j] = atom.AtomicNumber;
                        }
                    }

                    for (int vi = 0; vi < voxelCount; vi++)
                    {
                        var voxel = cavity.Voxels[vi];
                        Vector3 pos = new Vector3(voxel.X, voxel.Y, voxel.Z);

                        float chargeSum = 0f, hydroSum = 0f, zSum = 0f;
                        float weightSum = 0f;

                        if (atomCountLocal > 0 && atomPos != null)
                        {
                            for (int j = 0; j < atomCountLocal; j++)
                            {
                                float dx = atomPos[j].X - pos.X;
                                float dy = atomPos[j].Y - pos.Y;
                                float dz = atomPos[j].Z - pos.Z;
                                float distSq = dx * dx + dy * dy + dz * dz;

                                if (distSq < searchRadiusSq && distSq > 0.0001f)
                                {
                                    float dist = MathF.Sqrt(distSq);
                                    float w = 1.0f / (dist + 0.1f);
                                    chargeSum += atomCharge[j] * w;
                                    hydroSum += atomHydro[j] * w;
                                    zSum += atomZ[j] * w;
                                    weightSum += w;
                                }
                            }
                        }

                        if (weightSum > 0)
                        {
                            cavity.VoxelCharges[vi] = chargeSum / weightSum;
                            cavity.VoxelHydrophobicity[vi] = hydroSum / weightSum;
                            cavity.VoxelZEffective[vi] = zSum / weightSum;
                        }
                        else
                        {
                            cavity.VoxelCharges[vi] = 0f;
                            cavity.VoxelHydrophobicity[vi] = 0f;
                            cavity.VoxelZEffective[vi] = 0f;
                        }
                    }

 // Calculate accurate fractional coordinates from PeriodicGrid
                    if (periodicGrid != null && periodicGrid.FracX != null && region.Voxels != null)
                    {
                        int pgResX = periodicGrid.ResX;
                        int pgResY = periodicGrid.ResY;
                        int pgResZ = periodicGrid.ResZ;

                        bool hasIndices = region.Voxels != null && region.Voxels.Count == voxelCount;

                        if (hasIndices)
                        {
                            for (int vi = 0; vi < voxelCount; vi++)
                            {
                                var voxelIdx = region.Voxels[vi];

                                int px = voxelIdx.x + _marginX;
                                int py = voxelIdx.y + _marginY;
                                int pz = voxelIdx.z + _marginZ;

                                if (px >= 0 && px < pgResX && py >= 0 && py < pgResY && pz >= 0 && pz < pgResZ)
                                {
                                    float fx = periodicGrid.FracX[px, py, pz];
                                    float fy = periodicGrid.FracY[px, py, pz];
                                    float fz = periodicGrid.FracZ[px, py, pz];

                                    fx = fx - (float)Math.Floor(fx);
                                    fy = fy - (float)Math.Floor(fy);
                                    fz = fz - (float)Math.Floor(fz);

                                    if (fx < 0) fx += 1.0f;
                                    if (fy < 0) fy += 1.0f;
                                    if (fz < 0) fz += 1.0f;

                                    cavity.VoxelFracX[vi] = fx;
                                    cavity.VoxelFracY[vi] = fy;
                                    cavity.VoxelFracZ[vi] = fz;
                                }
                                else
                                {
                                    float minX = cavity.BoundingBox.Min.X;
                                    float minY = cavity.BoundingBox.Min.Y;
                                    float minZ = cavity.BoundingBox.Min.Z;
                                    float rangeX = cavity.BoundingBox.Max.X - minX + 0.001f;
                                    float rangeY = cavity.BoundingBox.Max.Y - minY + 0.001f;
                                    float rangeZ = cavity.BoundingBox.Max.Z - minZ + 0.001f;

                                    var voxel = cavity.Voxels[vi];
                                    cavity.VoxelFracX[vi] = Math.Clamp((voxel.X - minX) / rangeX, 0f, 1f);
                                    cavity.VoxelFracY[vi] = Math.Clamp((voxel.Y - minY) / rangeY, 0f, 1f);
                                    cavity.VoxelFracZ[vi] = Math.Clamp((voxel.Z - minZ) / rangeZ, 0f, 1f);
                                }
                            }
                        }
                        else
                        {
                            var fracDict = new Dictionary<long, (float, float, float)>();
                            const float scale = 1000f;

                            int step = Math.Max(1, (pgResX * pgResY * pgResZ) / 5000);

                            for (int px = 0; px < pgResX; px += step)
                            {
                                for (int py = 0; py < pgResY; py += step)
                                {
                                    for (int pz = 0; pz < pgResZ; pz += step)
                                    {
                                        var pos = periodicGrid.Positions[px, py, pz];
                                        float fx = periodicGrid.FracX[px, py, pz];
                                        float fy = periodicGrid.FracY[px, py, pz];
                                        float fz = periodicGrid.FracZ[px, py, pz];

                                        long key = ((long)(pos.X * scale) << 42) ^
                                                   ((long)(pos.Y * scale) << 21) ^
                                                   (long)(pos.Z * scale);

                                        fracDict[key] = (fx, fy, fz);
                                    }
                                }
                            }

                            for (int vi = 0; vi < voxelCount; vi++)
                            {
                                var voxel = cavity.Voxels[vi];
                                long key = ((long)(voxel.X * scale) << 42) ^
                                           ((long)(voxel.Y * scale) << 21) ^
                                           (long)(voxel.Z * scale);

                                if (fracDict.TryGetValue(key, out var frac))
                                {
                                    cavity.VoxelFracX[vi] = frac.Item1;
                                    cavity.VoxelFracY[vi] = frac.Item2;
                                    cavity.VoxelFracZ[vi] = frac.Item3;
                                }
                                else
                                {
                                    float minX = cavity.BoundingBox.Min.X;
                                    float minY = cavity.BoundingBox.Min.Y;
                                    float minZ = cavity.BoundingBox.Min.Z;
                                    float rangeX = cavity.BoundingBox.Max.X - minX + 0.001f;
                                    float rangeY = cavity.BoundingBox.Max.Y - minY + 0.001f;
                                    float rangeZ = cavity.BoundingBox.Max.Z - minZ + 0.001f;

                                    cavity.VoxelFracX[vi] = Math.Clamp((voxel.X - minX) / rangeX, 0f, 1f);
                                    cavity.VoxelFracY[vi] = Math.Clamp((voxel.Y - minY) / rangeY, 0f, 1f);
                                    cavity.VoxelFracZ[vi] = Math.Clamp((voxel.Z - minZ) / rangeZ, 0f, 1f);
                                }
                            }
                        }
                    }
                    else
                    {
                        float minX = cavity.BoundingBox.Min.X;
                        float minY = cavity.BoundingBox.Min.Y;
                        float minZ = cavity.BoundingBox.Min.Z;
                        float rangeX = cavity.BoundingBox.Max.X - minX + 0.001f;
                        float rangeY = cavity.BoundingBox.Max.Y - minY + 0.001f;
                        float rangeZ = cavity.BoundingBox.Max.Z - minZ + 0.001f;

                        for (int vi = 0; vi < voxelCount; vi++)
                        {
                            var voxel = cavity.Voxels[vi];
                            cavity.VoxelFracX[vi] = Math.Clamp((voxel.X - minX) / rangeX, 0f, 1f);
                            cavity.VoxelFracY[vi] = Math.Clamp((voxel.Y - minY) / rangeY, 0f, 1f);
                            cavity.VoxelFracZ[vi] = Math.Clamp((voxel.Z - minZ) / rangeZ, 0f, 1f);
                        }
                    }
                }

                Cavities.Add(cavity);

                processedRegions++;
                if (processedRegions % Math.Max(1, cavityCount / 10) == 0 || processedRegions == cavityCount)
                {
                    int percent = 85 + (int)(15.0f * processedRegions / cavityCount);
                    LogProgress($"Creating cavity objects... {processedRegions}/{cavityCount}", percent);
                }
            }

            LogToDebug($"Created {Cavities.Count} cavities", System.Drawing.Color.LightGreen);
            LogToDebug($"Total volume: {totalVolume:F2} Å³", System.Drawing.Color.LightGreen);

            TotalVoidsCount = Cavities.Count;
            TotalVoidsVolume = Cavities.Sum(c => c.Volume);

            string msg = $"Found {TotalVoidsCount} voids, volume: {TotalVoidsVolume:F1} Å³";

            MainForm.Instance?.PostStatus($"INFO: {msg}", "", "ℹ️");
            UpdateProgress(msg, 100);

            LogToDebug("========== DETECT_VOIDS_END ==========", System.Drawing.Color.Cyan);

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Cavities.Count > 0;
        }

        #endregion

        #region Public Methods - Mesh Parameters

        public static new MeshParameters GetMeshParametersForResolution(int resolution)
        {
            return CavityDetectorBase.GetMeshParametersForSmoothingLevel(resolution);
        }

        #endregion

        #region Public Methods - Clear

        public void Clear()
        {
            LogToDebug("========== CLEAR START ==========", System.Drawing.Color.Orange);

 // Disafterse oldch cavities
            if (Cavities != null)
            {
                foreach (var cavity in Cavities)
                {
                    try { cavity?.Dispose(); } catch { /* ignore */ }
                }
                Cavities.Clear();
            }

            StatusMessage = "";
            TotalVoidsVolume = 0;
            TotalVoidsCount = 0;

            _isoSurfaceCache.Clear();

            _periodicDensityCache = null;
            _regionMapCache = null;

            _fineGrid = null;
            _periodicGrid = null;
            _supercellMin = SysVector3.Zero;

            _atomCount = 0;

            _invFracToCart = Matrix.Identity;
            _fracToCart = Matrix.Identity;

            _baseSDFGrid = null;
            _hasBaseSDF = false;
            _baseVdwRadius = 0.5f;

 // ⭐ Reset tracking state
            _lastAppliedResolution = 0.15f;
            _lastUseThermalMotion = false;
            _lastAppliedVdw = BASE_SDF_RADIUS;
            _lastAppliedProbe = 0f;

 // ⭐ Reset supercell gradients
            _supercellGradientX = null;
            _supercellGradientY = null;
            _supercellGradientZ = null;

 // ⭐ Reset margins and periodic resolution
            _marginX = 0;
            _marginY = 0;
            _marginZ = 0;
            _periodicResX = 0;
            _periodicResY = 0;
            _periodicResZ = 0;

 // ⭐ Reset cells
            _cellMin = SysVector3.Zero;
            _cellMax = SysVector3.Zero;
            _supercellMin = SysVector3.Zero;

 // ⭐ Reset bounding boxes
            _trueCellBox = new BoundingBox();
            _fullCellBox = new BoundingBox();

 // ⭐ Reset matrixy
            _sysFracToCart = SysMatrix4x4.Identity;
            _sysInvFracToCart = SysMatrix4x4.Identity;

 // ⭐ Reset metadanych cell
            _cellVolume = 0f;
            _symmetryCount = 0;
            _minCorner = SysVector3.Zero;
            _maxCorner = SysVector3.Zero;

            LogToDebug("========== CLEAR END ==========", System.Drawing.Color.Orange);

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        #endregion

        #region Private Methods

 /// <summary>
 /// Reafterrts progress to UI thread. ALWAYS marshalls via BeginInvoke —
 /// even when InvokeRequired == false, because IsHandleCreated race
 /// would otherwise allow calling handler from worker thread.
 /// </summary>
        private void UpdateProgress(string message, float percent)
        {
            StatusMessage = message;

            var handler = ProgressChanged;
            if (handler == null) return;

            var mainForm = MainForm.Instance;

 // ⭐ ZAWSZE marshalluj na UI thread — nawet if InvokeRequired == false.
 // Without tego there is a race: the handle may disappear between the check
 // InvokeRequired a the call handler() — i handler() will run
 // z worker thread → cross-thread exception albo crash UI.
            if (mainForm != null && !mainForm.IsDisposed && mainForm.IsHandleCreated)
            {
                try
                {
                    mainForm.BeginInvoke(new Action(() => handler(message, percent)));
                    return;
                }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }

 // No UI — do nothing (do not call the handler from a worker thread!)
 // Otherwise handler() may try to touch the UI and throw.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateProgressWithDebug(string message, float percent, string debugInfo = "")
        {
            string fullMsg = message;
            if (!string.IsNullOrEmpty(debugInfo))
                fullMsg = $"{message} [{debugInfo}]";

            StatusMessage = fullMsg;
            SafeDebugLogger.LogTagged("[CAVITY]", $"{fullMsg} ({percent:F1}%)",
                System.Drawing.Color.LightBlue);
            UpdateProgress(fullMsg, percent);
        }

        private BoundingBox GetTrueCellBoundingBox()
        {
            SysVector3[] corners = new SysVector3[]
            {
                new SysVector3(0, 0, 0),
                new SysVector3(1, 0, 0),
                new SysVector3(0, 1, 0),
                new SysVector3(0, 0, 1),
                new SysVector3(1, 1, 0),
                new SysVector3(1, 0, 1),
                new SysVector3(0, 1, 1),
                new SysVector3(1, 1, 1)
            };

            var xnaFracToCart = _fracToCart;
            var cartCorners = new SysVector3[8];
            for (int i = 0; i < 8; i++)
            {
                var xnaPos = XnaVector3.Transform(
                    new XnaVector3(corners[i].X, corners[i].Y, corners[i].Z),
                    xnaFracToCart);
                cartCorners[i] = new SysVector3(xnaPos.X, xnaPos.Y, xnaPos.Z);
            }

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var v in cartCorners)
            {
                if (v.X < minX) minX = v.X;
                if (v.X > maxX) maxX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Y > maxY) maxY = v.Y;
                if (v.Z < minZ) minZ = v.Z;
                if (v.Z > maxZ) maxZ = v.Z;
            }

            return new BoundingBox(
                new XnaVector3(minX, minY, minZ),
                new XnaVector3(maxX, maxY, maxZ)
            );
        }

        private bool PrepareGridForDetection()
        {
            LogToDebug("Preparing grid for detection...", System.Drawing.Color.LightGray);

            if (Scanner == null)
            {
                string msg = "Scanner is null!";
                LogToDebug(msg, System.Drawing.Color.Red);
                MainForm.Instance?.PostStatus($"ERROR: {msg}", "", "❌");
                return false;
            }

            float currentResolution = Flags.Instance.Resolution > 0 ? Flags.Instance.Resolution : Scanner.VoxelSize;
            if (Math.Abs(Scanner.VoxelSize - currentResolution) > 0.0001f)
            {
                LogToDebug($"Updating scanner resolution: {Scanner.VoxelSize:F3} -> {currentResolution:F3}", System.Drawing.Color.LightGray);
                Scanner.VoxelSize = currentResolution;
            }

            var molecule = _molecule;
            float a = molecule.a;
            float b = molecule.b;
            float c = molecule.c;
            float alpha = molecule.alpha;
            float beta = molecule.beta;
            float gamma = molecule.gamma;

            var xnaFracToCart = Matrix.Transpose(molecule.FractionaltoCarttesianMatrix(0, 0, 0));
            _fracToCart = xnaFracToCart;
            _invFracToCart = Matrix.Invert(_fracToCart);

            var originXna = XnaVector3.Transform(XnaVector3.Zero, xnaFracToCart);
            var cornerXXna = XnaVector3.Transform(new XnaVector3(1, 0, 0), xnaFracToCart);
            var cornerYXna = XnaVector3.Transform(new XnaVector3(0, 1, 0), xnaFracToCart);
            var cornerZXna = XnaVector3.Transform(new XnaVector3(0, 0, 1), xnaFracToCart);

            var origin = new SysVector3(originXna.X, originXna.Y, originXna.Z);
            var cornerX = new SysVector3(cornerXXna.X, cornerXXna.Y, cornerXXna.Z);
            var cornerY = new SysVector3(cornerYXna.X, cornerYXna.Y, cornerYXna.Z);
            var cornerZ = new SysVector3(cornerZXna.X, cornerZXna.Y, cornerZXna.Z);

            var vecA = cornerX - origin;
            var vecB = cornerY - origin;
            var vecC = cornerZ - origin;

            float lenA = vecA.Length();
            float lenB = vecB.Length();
            float lenC = vecC.Length();

            int cellResX = Math.Max(8, (int)(lenA / Scanner.VoxelSize) + 1);
            int cellResY = Math.Max(8, (int)(lenB / Scanner.VoxelSize) + 1);
            int cellResZ = Math.Max(8, (int)(lenC / Scanner.VoxelSize) + 1);

            _periodicResX = cellResX;
            _periodicResY = cellResY;
            _periodicResZ = cellResZ;

            float marginAngstroms = Math.Max(ScanMargin, Math.Max(lenA, Math.Max(lenB, lenC)) * 0.05f);

            _marginX = (int)(marginAngstroms / Scanner.VoxelSize) + 3;
            _marginY = (int)(marginAngstroms / Scanner.VoxelSize) + 3;
            _marginZ = (int)(marginAngstroms / Scanner.VoxelSize) + 3;

            int totalResX = cellResX + 2 * _marginX;
            int totalResY = cellResY + 2 * _marginY;
            int totalResZ = cellResZ + 2 * _marginZ;

 // ⭐ Sprawdzenie limitu sizeu wymiaru (short max = 32767)
            const int SHORT_MAX = 32767;
            if (totalResX > SHORT_MAX || totalResY > SHORT_MAX || totalResZ > SHORT_MAX)
            {
                string msg = $"Grid dimension exceeds short limit: {totalResX}x{totalResY}x{totalResZ} " +
                             $"(max per axis: {SHORT_MAX}). Please increase voxel size (current: {Scanner.VoxelSize:F3} A).";
                LogToDebug(msg, System.Drawing.Color.Red);
                MainForm.Instance?.PostStatus($"ERROR: {msg}", "", "❌");
                return false;
            }

            long totalVoxels = (long)totalResX * totalResY * totalResZ;

 // ⭐ STATIC limit + info o memory systemowej
            if (totalVoxels > Flags.MAX_VOXELS)
            {
                string msg = $"Detection grid too large: {totalVoxels:N0} voxels " +
                             $"(max: {Flags.MAX_VOXELS:N0}, RAM: {Flags.PhysicalMemoryGB:F1} GB). " +
                             $"Est. memory: {Flags.FormatMemorySize(Flags.EstimateMemoryUsage(totalVoxels))}. " +
                             $"Please increase voxel size (current: {Scanner.VoxelSize:F3} A).";
                LogToDebug(msg, System.Drawing.Color.Red);
                MainForm.Instance?.PostStatus($"ERROR: {msg}", "", "❌");
                return false;
            }

 // ⭐ STATIC warning threshold
            if (Flags.ShouldWarnAboutVoxelCount(totalVoxels))
            {
                string msg = $"Large detection grid: {totalVoxels:N0} voxels. " +
                             $"Est. memory: {Flags.FormatMemorySize(Flags.EstimateMemoryUsage(totalVoxels))}. " +
                             $"This may take a while.";
                LogToDebug(msg, System.Drawing.Color.Orange);
                MainForm.Instance?.PostStatus($"Warning: {msg}", "", "!");
            }

            _fineGrid = Scanner.CreateCrystalGrid(
                origin, vecA, vecB, vecC,
                cellResX, cellResY, cellResZ,
                _marginX, _marginY, _marginZ,
                isFine: true
            );

            if (_fineGrid == null)
            {
                string msg = "Failed to create fine grid!";
                LogToDebug(msg, System.Drawing.Color.Red);
                MainForm.Instance?.PostStatus($"ERROR: {msg}", "", "❌");
                return false;
            }

            _supercellMin = _fineGrid.BoundsMin;
            _cellMin = origin;
            _cellMax = origin + vecA + vecB + vecC;

            _periodicGrid = new VoxelScanner.VoxelGrid
            {
                FracX = new float[cellResX, cellResY, cellResZ],
                FracY = new float[cellResX, cellResY, cellResZ],
                FracZ = new float[cellResX, cellResY, cellResZ],
                Positions = new SysVector3[cellResX, cellResY, cellResZ],
                ResX = (short)cellResX,
                ResY = (short)cellResY,
                ResZ = (short)cellResZ,
                BoundsMin = _cellMin,
                BoundsMax = _cellMax,
                IsFine = true,
                Origin = _fineGrid.Origin,
                VecA = _fineGrid.VecA,
                VecB = _fineGrid.VecB,
                VecC = _fineGrid.VecC
            };

            Parallel.For(0, cellResX, new ParallelOptions { MaxDegreeOfParallelism = _maxParallelism }, x =>
            {
                int sx = x + _marginX;
                for (int y = 0; y < cellResY; y++)
                {
                    int sy = y + _marginY;
                    for (int z = 0; z < cellResZ; z++)
                    {
                        int sz = z + _marginZ;
                        _periodicGrid.Positions[x, y, z] = _fineGrid.Positions[sx, sy, sz];
                        _periodicGrid.FracX[x, y, z] = _fineGrid.FracX[sx, sy, sz];
                        _periodicGrid.FracY[x, y, z] = _fineGrid.FracY[sx, sy, sz];
                        _periodicGrid.FracZ[x, y, z] = _fineGrid.FracZ[sx, sy, sz];
                    }
                }
            });

            LogToDebug("Grid preparation complete", System.Drawing.Color.LightGreen);
            return true;
        }

        private float[,,] CopyDensityGrid(float[,,] density)
        {
            int nx = density.GetLength(0);
            int ny = density.GetLength(1);
            int nz = density.GetLength(2);
            float[,,] copy = new float[nx, ny, nz];
            Buffer.BlockCopy(density, 0, copy, 0, density.Length * sizeof(float));
            return copy;
        }

        private (float[,,] gradX, float[,,] gradY, float[,,] gradZ) ComputeGradientWithMetricFastest(
            float[,,] density,
            int gradNx, int gradNy, int gradNz)
        {
            var gradX = new float[gradNx, gradNy, gradNz];
            var gradY = new float[gradNx, gradNy, gradNz];
            var gradZ = new float[gradNx, gradNy, gradNz];

            float stepX_frac = CellParameters.StepX;
            float stepY_frac = CellParameters.StepY;
            float stepZ_frac = CellParameters.StepZ;

            var vecA = CellParameters.VecA;
            var vecB = CellParameters.VecB;
            var vecC = CellParameters.VecC;

            float g00 = SysVector3.Dot(vecA, vecA);
            float g01 = SysVector3.Dot(vecA, vecB);
            float g02 = SysVector3.Dot(vecA, vecC);
            float g11 = SysVector3.Dot(vecB, vecB);
            float g12 = SysVector3.Dot(vecB, vecC);
            float g22 = SysVector3.Dot(vecC, vecC);

            float det = g00 * (g11 * g22 - g12 * g12) -
                        g01 * (g01 * g22 - g12 * g02) +
                        g02 * (g01 * g12 - g11 * g02);

            if (Math.Abs(det) < 1e-12f)
            {
                LogToDebug("WARNING: Degenerate cell detected! Using simple gradient.", System.Drawing.Color.Orange);
                return ComputeGradientSimpleFast(density, gradNx, gradNy, gradNz);
            }

            float invDet = 1.0f / det;
            float ig00 = (g11 * g22 - g12 * g12) * invDet;
            float ig01 = (g02 * g12 - g01 * g22) * invDet;
            float ig02 = (g01 * g12 - g02 * g11) * invDet;
            float ig11 = (g00 * g22 - g02 * g02) * invDet;
            float ig12 = (g01 * g02 - g00 * g12) * invDet;
            float ig22 = (g00 * g11 - g01 * g01) * invDet;

            float inv2x = 1.0f / (2.0f * stepX_frac);
            float inv2y = 1.0f / (2.0f * stepY_frac);
            float inv2z = 1.0f / (2.0f * stepZ_frac);

            int total = gradNx * gradNy * gradNz;
            float[] densFlat = new float[total];
            float[] gradXFlat = new float[total];
            float[] gradYFlat = new float[total];
            float[] gradZFlat = new float[total];

            Buffer.BlockCopy(density, 0, densFlat, 0, total * sizeof(float));

            int nx = gradNx, ny = gradNy, nz = gradNz;
            int maxParallel = Math.Max(1, Environment.ProcessorCount / 2);
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel };

            Parallel.For(1, nx - 1, options, x =>
            {
                float[] d = densFlat;
                float[] gx = gradXFlat;
                float[] gy = gradYFlat;
                float[] gz = gradZFlat;

                int xOffset = x * ny * nz;
                int xm1Offset = (x - 1) * ny * nz;
                int xp1Offset = (x + 1) * ny * nz;

                float inv2xLocal = inv2x;
                float inv2yLocal = inv2y;
                float inv2zLocal = inv2z;
                int nzLocal = nz;
                int nyLocal = ny;

                for (int y = 1; y < nyLocal - 1; y++)
                {
                    int yOffset = y * nzLocal;
                    int ym1Offset = (y - 1) * nzLocal;
                    int yp1Offset = (y + 1) * nzLocal;

                    int baseIdx = xOffset + yOffset;

                    for (int z = 1; z < nzLocal - 1; z++)
                    {
                        int idx = baseIdx + z;

                        float dfx = (d[xp1Offset + yOffset + z] - d[xm1Offset + yOffset + z]) * inv2xLocal;
                        float dfy = (d[xOffset + yp1Offset + z] - d[xOffset + ym1Offset + z]) * inv2yLocal;
                        float dfz = (d[xOffset + yOffset + z + 1] - d[xOffset + yOffset + z - 1]) * inv2zLocal;

                        gx[idx] = ig00 * dfx + ig01 * dfy + ig02 * dfz;
                        gy[idx] = ig01 * dfx + ig11 * dfy + ig12 * dfz;
                        gz[idx] = ig02 * dfx + ig12 * dfy + ig22 * dfz;
                    }
                }
            });

            Buffer.BlockCopy(gradXFlat, 0, gradX, 0, total * sizeof(float));
            Buffer.BlockCopy(gradYFlat, 0, gradY, 0, total * sizeof(float));
            Buffer.BlockCopy(gradZFlat, 0, gradZ, 0, total * sizeof(float));

            return (gradX, gradY, gradZ);
        }

        private (float[,,] gradX, float[,,] gradY, float[,,] gradZ) ComputeGradientSimpleFast(
            float[,,] density,
            int gradNx, int gradNy, int gradNz)
        {
            var gradX = new float[gradNx, gradNy, gradNz];
            var gradY = new float[gradNx, gradNy, gradNz];
            var gradZ = new float[gradNx, gradNy, gradNz];

            float stepX = CellParameters.GetStepX_Angstroms();
            float stepY = CellParameters.GetStepY_Angstroms();
            float stepZ = CellParameters.GetStepZ_Angstroms();

            float inv2x = 1.0f / (2.0f * stepX);
            float inv2y = 1.0f / (2.0f * stepY);
            float inv2z = 1.0f / (2.0f * stepZ);

            int total = gradNx * gradNy * gradNz;
            float[] densFlat = new float[total];
            float[] gradXFlat = new float[total];
            float[] gradYFlat = new float[total];
            float[] gradZFlat = new float[total];

            Buffer.BlockCopy(density, 0, densFlat, 0, total * sizeof(float));

            int nx = gradNx, ny = gradNy, nz = gradNz;
            int maxParallel = Math.Max(1, Environment.ProcessorCount / 2);
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel };

            Parallel.For(1, nx - 1, options, x =>
            {
                float[] d = densFlat;
                float[] gx = gradXFlat;
                float[] gy = gradYFlat;
                float[] gz = gradZFlat;

                int xOffset = x * ny * nz;
                int xm1Offset = (x - 1) * ny * nz;
                int xp1Offset = (x + 1) * ny * nz;

                float inv2xLocal = inv2x;
                float inv2yLocal = inv2y;
                float inv2zLocal = inv2z;
                int nzLocal = nz;
                int nyLocal = ny;

                for (int y = 1; y < nyLocal - 1; y++)
                {
                    int yOffset = y * nzLocal;
                    int ym1Offset = (y - 1) * nzLocal;
                    int yp1Offset = (y + 1) * nzLocal;

                    int baseIdx = xOffset + yOffset;

                    for (int z = 1; z < nzLocal - 1; z++)
                    {
                        int idx = baseIdx + z;

                        gx[idx] = (d[xp1Offset + yOffset + z] - d[xm1Offset + yOffset + z]) * inv2xLocal;
                        gy[idx] = (d[xOffset + yp1Offset + z] - d[xOffset + ym1Offset + z]) * inv2yLocal;
                        gz[idx] = (d[xOffset + yOffset + z + 1] - d[xOffset + yOffset + z - 1]) * inv2zLocal;
                    }
                }
            });

            Buffer.BlockCopy(gradXFlat, 0, gradX, 0, total * sizeof(float));
            Buffer.BlockCopy(gradYFlat, 0, gradY, 0, total * sizeof(float));
            Buffer.BlockCopy(gradZFlat, 0, gradZ, 0, total * sizeof(float));

            return (gradX, gradY, gradZ);
        }

        private (List<SysVector3> voxels, float[,,] densityGrid) ComputeSDFWithDensityOptimized(
            VoxelScanner.VoxelGrid grid,
            VoxelScanner.AtomData[] atoms,
            int atomCount,
            CancellationToken cancellationToken = default)
        {
            int nx = grid.ResX;
            int ny = grid.ResY;
            int nz = grid.ResZ;

            long totalVoxels = (long)nx * ny * nz;

            if (totalVoxels > Flags.MAX_VOXELS)
            {
                string msg = $"SDF grid too large: {totalVoxels:N0} voxels (max: {Flags.MAX_VOXELS:N0}). Please increase voxel size.";
                LogToDebug(msg, System.Drawing.Color.Red);
                MainForm.Instance?.PostStatus($"ERROR: {msg}", "", "❌");
                return (new List<SysVector3>(), new float[nx, ny, nz]);
            }

            if (totalVoxels > 15_000_000)
            {
                string msg = $"Large SDF grid: {totalVoxels:N0} voxels. This may take a while. Consider increasing voxel size.";
                LogToDebug(msg, System.Drawing.Color.Orange);
                MainForm.Instance?.PostStatus($"Warning: {msg}", "", "!");
            }

            float maxDist = Scanner.MaxDistFine;
            float maxDist2 = maxDist * maxDist;
            int total = nx * ny * nz;
            float[] density = new float[total];
            var voxelLists = new ConcurrentBag<List<SysVector3>>();

            int progressStep = Math.Max(1, total / 20);
            int processed = 0;

            var scanner = this.Scanner;
            if (scanner == null || !scanner.SpatialDataReady || scanner.Atoms == null)
            {
                string msg = "Spatial data not ready! Cannot compute SDF.";
                LogToDebug(msg, System.Drawing.Color.Red);
                MainForm.Instance?.PostStatus($"ERROR: {msg}", "", "❌");
                return (new List<SysVector3>(), new float[nx, ny, nz]);
            }

            float[,,] fracX = grid.FracX;
            float[,,] fracY = grid.FracY;
            float[,,] fracZ = grid.FracZ;

            float marginAngstroms = 2.0f;
            float cellSizeX = _molecule.a;
            float cellSizeY = _molecule.b;
            float cellSizeZ = _molecule.c;

            float marginFracX = Math.Min(marginAngstroms / cellSizeX, 0.50f);
            float marginFracY = Math.Min(marginAngstroms / cellSizeY, 0.50f);
            float marginFracZ = Math.Min(marginAngstroms / cellSizeZ, 0.50f);

            int maxParallel = Math.Max(1, Environment.ProcessorCount / 2);
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel };
            var positions = grid.Positions;

            var atomsLocal = atoms;
            var densityLocal = density;
            var positionsLocal = positions;
            var scannerLocal = scanner;
            float maxDistLocal = maxDist;
            float maxDist2Local = maxDist2;
            float marginFracXLocal = marginFracX;
            float marginFracYLocal = marginFracY;
            float marginFracZLocal = marginFracZ;

            bool UseThermalMotion = Flags.Instance.UseThermalMotion;
            int atomCountLocal = atomCount;

            float[] atomX = new float[atomCount];
            float[] atomY = new float[atomCount];
            float[] atomZ = new float[atomCount];
            float[] atomRadius = new float[atomCount];
            float[] atomRadius2 = new float[atomCount];
            float[] atomRange = new float[atomCount];
            bool[] atomIsInternal = new bool[atomCount];
            float[] atomMinY = new float[atomCount];
            float[] atomMaxY = new float[atomCount];
            float[] atomMinZ = new float[atomCount];
            float[] atomMaxZ = new float[atomCount];

            bool[] atomHasAnisotropic = new bool[atomCount];
            float[,] atomUn = new float[atomCount, 9];
            float[] atomRx = new float[atomCount];
            float[] atomRy = new float[atomCount];
            float[] atomRz = new float[atomCount];
            float[] atomInvRx2 = new float[atomCount];
            float[] atomInvRy2 = new float[atomCount];
            float[] atomInvRz2 = new float[atomCount];

            for (int i = 0; i < atomCount; i++)
            {
                ref var atom = ref atoms[i];
                atomX[i] = atom.X;
                atomY[i] = atom.Y;
                atomZ[i] = atom.Z;

                float radius = atom.Radius;
                atomRadius[i] = radius;
                atomRadius2[i] = radius * radius;
                atomRange[i] = radius + maxDist;
                atomIsInternal[i] = atom.IsInternal;

                float r = atomRange[i];
                atomMinY[i] = atom.Y - r;
                atomMaxY[i] = atom.Y + r;
                atomMinZ[i] = atom.Z - r;
                atomMaxZ[i] = atom.Z + r;

                bool hasAniso = UseThermalMotion && atom.HasAnisotropicData;
                atomHasAnisotropic[i] = hasAniso;

                if (hasAniso)
                {
 // ── Un * Eigen.main(ref Ue) ma become: Un = -D · Eᵀ
 // gdzie E = eigenvectors (kolumny), D = diag(√|λ|)
 // so WIERSZ i matrixy Un = -√|λ_i| · e_iᵀ
 // → row length i = √|λ_i| = SEMI-AXIS ellipsoids in the direction of e_i
 // → normalised wiersz i = -e_i = EIGENVECTOR (orientation)

                    var un = atom.Un;

                    Vector3 row0 = new Vector3(un.M11, un.M12, un.M13);
                    Vector3 row1 = new Vector3(un.M21, un.M22, un.M23);
                    Vector3 row2 = new Vector3(un.M31, un.M32, un.M33);

 // ⭐ Row lengths BEFORE normalisation = ellipsoid semi-axes
                    float sqrtLam1 = row0.Length();
                    float sqrtLam2 = row1.Length();
                    float sqrtLam3 = row2.Length();

 // Ortonormalisation (Grama-Schmidt) → wektory own
                    row0 = Vector3.Normalize(row0);
                    row1 = Vector3.Normalize(row1 - Vector3.Dot(row1, row0) * row0);
                    row2 = Vector3.Normalize(row2 - Vector3.Dot(row2, row0) * row0 - Vector3.Dot(row2, row1) * row1);

                    atomUn[i, 0] = row0.X; atomUn[i, 1] = row0.Y; atomUn[i, 2] = row0.Z;
                    atomUn[i, 3] = row1.X; atomUn[i, 4] = row1.Y; atomUn[i, 5] = row1.Z;
                    atomUn[i, 6] = row2.X; atomUn[i, 7] = row2.Y; atomUn[i, 8] = row2.Z;

                    float vdwRadius = radius;
                    atomRx[i] = vdwRadius + sqrtLam1;
                    atomRy[i] = vdwRadius + sqrtLam2;
                    atomRz[i] = vdwRadius + sqrtLam3;

                    if (atomRx[i] < 0.01f) atomRx[i] = 0.01f;
                    if (atomRy[i] < 0.01f) atomRy[i] = 0.01f;
                    if (atomRz[i] < 0.01f) atomRz[i] = 0.01f;

                    atomInvRx2[i] = 1.0f / (atomRx[i] * atomRx[i]);
                    atomInvRy2[i] = 1.0f / (atomRy[i] * atomRy[i]);
                    atomInvRz2[i] = 1.0f / (atomRz[i] * atomRz[i]);
                }
                else
                {
                    atomRx[i] = radius;
                    atomRy[i] = radius;
                    atomRz[i] = radius;
                    atomInvRx2[i] = 1.0f / (radius * radius);
                    atomInvRy2[i] = 1.0f / (radius * radius);
                    atomInvRz2[i] = 1.0f / (radius * radius);
                }
            }

            Parallel.For(0, nx, options, (x, state) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var localVoxels = new List<SysVector3>(total / nx / 3);
                int[] atomBuffer = new int[64];

                for (int y = 0; y < ny; y++)
                {
                    for (int z = 0; z < nz; z++)
                    {
                        int idx = (x * ny + y) * nz + z;

                        float fx = fracX[x, y, z];
                        float fy = fracY[x, y, z];
                        float fz = fracZ[x, y, z];

                        fx = fx - (float)Math.Floor(fx);
                        fy = fy - (float)Math.Floor(fy);
                        fz = fz - (float)Math.Floor(fz);

                        float distToCellX = 0;
                        if (fx < 0) distToCellX = -fx;
                        else if (fx > 1.0f) distToCellX = fx - 1.0f;

                        float distToCellY = 0;
                        if (fy < 0) distToCellY = -fy;
                        else if (fy > 1.0f) distToCellY = fy - 1.0f;

                        float distToCellZ = 0;
                        if (fz < 0) distToCellZ = -fz;
                        else if (fz > 1.0f) distToCellZ = fz - 1.0f;

                        if (distToCellX > marginFracXLocal ||
                            distToCellY > marginFracYLocal ||
                            distToCellZ > marginFracZLocal)
                        {
                            densityLocal[idx] = maxDistLocal;
                            continue;
                        }

                        ref SysVector3 p = ref positionsLocal[x, y, z];
                        float px = p.X, py = p.Y, pz = p.Z;

                        int count = scannerLocal.FindAtomsGradualFast(px, py, pz, atomBuffer);

                        float minDistToSurface = float.MaxValue;
                        bool isOccupied = false;

                        if (count > 0)
                        {
                            for (int bi = 0; bi < count; bi++)
                            {
                                int i = atomBuffer[bi];

                                if (i < 0 || i >= atomCountLocal) continue;

                                ref var atom = ref atomsLocal[i];

                                float dx = px - atom.X;
                                float dy = py - atom.Y;
                                float dz = pz - atom.Z;
                                float dist2 = dx * dx + dy * dy + dz * dz;

                                if (dist2 > maxDist2Local)
                                    continue;

                                float distToSurface;

                                if (atomHasAnisotropic[i])
                                {
                                    float ex = atomUn[i, 0] * dx + atomUn[i, 1] * dy + atomUn[i, 2] * dz;
                                    float ey = atomUn[i, 3] * dx + atomUn[i, 4] * dy + atomUn[i, 5] * dz;
                                    float ez = atomUn[i, 6] * dx + atomUn[i, 7] * dy + atomUn[i, 8] * dz;

                                    float ellipsoidDist2 = ex * ex * atomInvRx2[i] +
                                                           ey * ey * atomInvRy2[i] +
                                                           ez * ez * atomInvRz2[i];

                                    float dist = MathF.Sqrt(dist2);

                                    if (ellipsoidDist2 <= 1.0f)
                                    {
                                        isOccupied = true;
                                        float scale = MathF.Sqrt(ellipsoidDist2);
                                        if (scale < 0.0001f) scale = 0.0001f;
                                        distToSurface = -(scale - 1.0f) * dist / scale;
                                        if (distToSurface > minDistToSurface)
                                            minDistToSurface = distToSurface;
                                    }
                                    else
                                    {
                                        float scale = MathF.Sqrt(ellipsoidDist2);
                                        distToSurface = (scale - 1.0f) * dist / scale;
                                        if (distToSurface < minDistToSurface)
                                            minDistToSurface = distToSurface;
                                    }
                                }
                                else
                                {
                                    float radius = atom.Radius;
                                    float radiusSq = atom.RadiusSq;

                                    if (dist2 < radiusSq)
                                    {
                                        isOccupied = true;
                                        float dist = MathF.Sqrt(dist2);
                                        distToSurface = -(radius - dist);
                                        if (distToSurface > minDistToSurface)
                                            minDistToSurface = distToSurface;
                                    }
                                    else
                                    {
                                        float dist = MathF.Sqrt(dist2);
                                        distToSurface = dist - radius;
                                        if (distToSurface < minDistToSurface)
                                            minDistToSurface = distToSurface;
                                    }
                                }
                            }
                        }

                        float finalSdf;
                        if (isOccupied)
                        {
                            finalSdf = -0.5f;
                        }
                        else if (minDistToSurface == float.MaxValue)
                        {
                            finalSdf = maxDistLocal;
                        }
                        else
                        {
                            finalSdf = Math.Min(minDistToSurface, maxDistLocal);
                        }

                        densityLocal[idx] = finalSdf;

                        if (finalSdf >= 0 && finalSdf < maxDistLocal)
                        {
                            localVoxels.Add(p);
                        }

                        int proc = Interlocked.Increment(ref processed);
                        if (proc % progressStep == 0)
                        {
                            float percent = 20 + 30.0f * proc / total;
                            UpdateProgressWithDebug("Computing SDF", percent);
                        }
                    }
                }

                if (localVoxels.Count > 0)
                    voxelLists.Add(localVoxels);
            });

            LogToDebug($"SDF computation complete: {total} voxels processed", System.Drawing.Color.LightGreen);
            UpdateProgressWithDebug("SDF complete", 50, $"{total} voxels processed");

            int totalVoxelsCount = 0;
            foreach (var list in voxelLists)
                totalVoxelsCount += list.Count;

            var voxelList = new List<SysVector3>(totalVoxelsCount);
            foreach (var list in voxelLists)
                voxelList.AddRange(list);

            float[,,] densityGrid = new float[nx, ny, nz];
            Buffer.BlockCopy(density, 0, densityGrid, 0, density.Length * sizeof(float));

            return (voxelList, densityGrid);
        }

        #endregion
    }
}