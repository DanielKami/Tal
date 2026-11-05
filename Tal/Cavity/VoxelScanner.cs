// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: VoxelScanner.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tal.Diagnostics;
using SysMatrix4x4 = System.Numerics.Matrix4x4;
using SysVector3 = System.Numerics.Vector3;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;

namespace Tal.Cavity
{
 /// <summary>
 /// VoxelScanner - Core class for generating voxel grids and SDF (Signed Distance Function)
 /// Fully optimized with spatial sorting, bounding boxes, and pre-computed anisotropic data.
 /// </summary>
    public class VoxelScanner : CavityDetectorBase
    {
        #region Constants

        private static readonly float[] _searchRadii = { 0.5f, 1.0f, 1.5f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f, 4.5f, 5.0f };
        private static readonly float[] _searchRadii2 = { 0.25f, 1.0f, 2.25f, 4.0f, 6.25f, 9.0f, 12.25f, 16.0f, 20.25f, 25.0f };

        #endregion

        #region Scanning Parameters

 /// <summary>Van der Waals radius multiplier.</summary>
        public float RadiusMultiplier { get; set; } = 1.1f;

 /// <summary>Probe radius for surface afterint detection.</summary>
        public float ProbeRadius { get; set; } = 0.0f;

 /// <summary>Whether to use crystallographic symmetry.</summary>
        public bool UseSymmetry { get; set; } = true;

 /// <summary>Maximum distance for coarse search.</summary>
        public float MaxDistCoarse => _maxDistFine;

 /// <summary>Scan margin in Angstroms.</summary>
        public float ScanMargin { get; set; } = 2.0f;

 /// <summary>Voxel size in Angstroms.</summary>
        public float VoxelSize { get; set; } = 0.15f;

 /// <summary>Indicates whether voxel size has changed and needs rebuilding.</summary>
        public bool IsVoxelSizeDirty { get; set; } = false;

        #endregion

        #region Crystallographic Data

        private readonly Molecule _molecule;
        private XnaMatrix _fracToCart;
        private SysMatrix4x4 _sysFracToCart;
        private FastVec3 _minCorner, _maxCorner;
        private float _cellVolume;
        private SysVector3 _cellSize;

        private AtomData[] _atomsForScanning;
        private float _maxDistFine;

        private readonly int _maxParallelism;

        #endregion

        #region Spatial Data Optimization

        private int[] _sortedIndicesX;
        private float[] _sortedX;
        private bool _spatialDataReady = false;

        private readonly object _atomsLock = new object();
        private readonly object _spatialDataLock = new object();
        private readonly object _resetLock = new object();

 /// <summary>
 /// Immutable snapshot of spatial data for lock-free reads on the hot path.
 /// Published atomically (reference assignment) after PrepareSpatialData completes.
 /// </summary>
        private sealed class SpatialSnapshot
        {
            public AtomData[] Atoms;
            public int[] SortedIndicesX;
            public float[] SortedX;
        }

        private volatile SpatialSnapshot _spatialSnapshot;

        #endregion

        #region Public Properties

        public AtomData[] Atoms
        {
            get
            {
                lock (_atomsLock)
                {
                    return _atomsForScanning;
                }
            }
        }

        public float MaxDistFine => _maxDistFine;
        public float CellVolume => _cellVolume;
        public FastVec3 MinCorner => _minCorner;
        public FastVec3 MaxCorner => _maxCorner;
        public XnaMatrix FracToCart => _fracToCart;
        public SysMatrix4x4 SysFracToCart => _sysFracToCart;

        public int[] SortedIndicesX
        {
            get
            {
                lock (_spatialDataLock)
                {
                    return _sortedIndicesX;
                }
            }
        }

        public float[] SortedX
        {
            get
            {
                lock (_spatialDataLock)
                {
                    return _sortedX;
                }
            }
        }

        public bool SpatialDataReady
        {
            get
            {
                lock (_spatialDataLock)
                {
                    return _spatialDataReady;
                }
            }
        }

        private bool _isReset = true;

        #endregion

        #region Atom Data Structure

 /// <summary>
 /// Atom data structure used for scanning and SDF computation.
 /// Identical to the CIF atom data structure.
 /// </summary>
        public struct AtomData
        {
            public float X, Y, Z;
            public float Radius;
            public float RadiusSq;
            public float FracX, FracY, FracZ;

            public float MinX, MinY, MinZ;
            public float MaxX, MaxY, MaxZ;

            public bool IsInternal;
            public bool HasAnisotropicData;

            public Matrix Un;
            public Matrix Ue;

            public int MoleculeId;
        }

        #endregion

        #region Voxel Grid Class

 /// <summary>
 /// Represents a 3D voxel grid with fractional coordinates and aftersitions.
 /// </summary>
        public class VoxelGrid
        {
            public float[,,] FracX { get; set; }
            public float[,,] FracY { get; set; }
            public float[,,] FracZ { get; set; }

            public short ResX { get; set; }
            public short ResY { get; set; }
            public short ResZ { get; set; }

            public SysVector3 BoundsMin { get; set; }
            public SysVector3 BoundsMax { get; set; }
            public bool IsFine { get; set; }

            public SysVector3 Origin { get; set; }
            public SysVector3 VecA { get; set; }
            public SysVector3 VecB { get; set; }
            public SysVector3 VecC { get; set; }

            private float _stepX, _stepY, _stepZ;
            public float StepX => _stepX > 0 ? _stepX : CellParameters.StepX;
            public float StepY => _stepY > 0 ? _stepY : CellParameters.StepY;
            public float StepZ => _stepZ > 0 ? _stepZ : CellParameters.StepZ;

            public void SetSteps(float stepX, float stepY, float stepZ)
            {
                _stepX = stepX;
                _stepY = stepY;
                _stepZ = stepZ;
            }

            private SysVector3[,,] _positionsCache;
            public SysVector3[,,] Positions
            {
                get
                {
                    if (_positionsCache == null)
                    {
                        _positionsCache = new SysVector3[ResX, ResY, ResZ];
                        for (int x = 0; x < ResX; x++)
                            for (int y = 0; y < ResY; y++)
                                for (int z = 0; z < ResZ; z++)
                                    _positionsCache[x, y, z] = GetPosition(x, y, z);
                    }
                    return _positionsCache;
                }
                set => _positionsCache = value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SysVector3 GetPosition(int x, int y, int z)
            {
                float fx = FracX[x, y, z];
                float fy = FracY[x, y, z];
                float fz = FracZ[x, y, z];

                SysVector3 origin = Origin;
                SysVector3 vecA = VecA;
                SysVector3 vecB = VecB;
                SysVector3 vecC = VecC;

                if (origin == SysVector3.Zero && vecA == SysVector3.Zero)
                {
                    origin = CellParameters.Origin;
                    vecA = CellParameters.VecA;
                    vecB = CellParameters.VecB;
                    vecC = CellParameters.VecC;
                }

                return origin + fx * vecA + fy * vecB + fz * vecC;
            }

            public void ClearPositionCache()
            {
                _positionsCache = null;
            }
        }

        #endregion

        #region Molecule Group

        private class MoleculeGroup
        {
            public int Id { get; set; }
            public List<AtomData> Atoms { get; set; } = new List<AtomData>();
            public List<int> AtomIndices { get; set; } = new List<int>();
            public int AtomCount => Atoms?.Count ?? 0;

            public override string ToString()
            {
                return $"Molecule #{Id}: {AtomCount} atoms";
            }
        }

        #endregion

        #region Fast Vector3 Struct

        [StructLayout(LayoutKind.Sequential)]
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
            SafeDebugLogger.LogTagged("[VoxelScanner]", message, color);
        }

 /// <summary>
 /// Logs progress information with percentage.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogProgress(string message, float percent, System.Drawing.Color? color = null)
        {
            SafeDebugLogger.LogTagged(
                "[VoxelScanner]",
                $"[{percent:F0}%] {message}",
                color ?? System.Drawing.Color.LightBlue);
        }

        #endregion

        #region Constructor

        public VoxelScanner(Molecule molecule) : base()
        {
            if (molecule == null)
                throw new ArgumentNullException(nameof(molecule), "Molecule cannot be null");

            _molecule = molecule;
            _maxParallelism = OperatingSystem.IsAndroid() ? 3 : Math.Max(1, Environment.ProcessorCount / 2);

            XnaMatrix fracToCart = _molecule.FractionaltoCarttesianMatrix(0, 0, 0);
            _fracToCart = XnaMatrix.Transpose(fracToCart);

            _sysFracToCart = new SysMatrix4x4(
                _fracToCart.M11, _fracToCart.M12, _fracToCart.M13, _fracToCart.M14,
                _fracToCart.M21, _fracToCart.M22, _fracToCart.M23, _fracToCart.M24,
                _fracToCart.M31, _fracToCart.M32, _fracToCart.M33, _fracToCart.M34,
                _fracToCart.M41, _fracToCart.M42, _fracToCart.M43, _fracToCart.M44
            );

            var minXna = XnaVector3.Transform(XnaVector3.Zero, _fracToCart);
            var maxXna = XnaVector3.Transform(new XnaVector3(1, 1, 1), _fracToCart);
            _minCorner = new FastVec3(new SysVector3(minXna.X, minXna.Y, minXna.Z));
            _maxCorner = new FastVec3(new SysVector3(maxXna.X, maxXna.Y, maxXna.Z));

            var v1 = (SysVector3)_maxCorner - (SysVector3)_minCorner;
            _cellVolume = Math.Abs(v1.X * v1.Y * v1.Z);
            _cellSize = v1;

            LogToDebug($"Cell volume: {_cellVolume:F2} A^3", System.Drawing.Color.LightGray);
            LogToDebug($"Max parallelism: {_maxParallelism}", System.Drawing.Color.LightGray);
        }

        #endregion

        #region Public Methods

        public void UpdateVoxelSize(float newSize)
        {
            if (Math.Abs(VoxelSize - newSize) > 0.0001f)
            {
                VoxelSize = newSize;
                IsVoxelSizeDirty = true;
                LogToDebug($"Voxel size updated to: {newSize:F4} A", System.Drawing.Color.LightGray);
            }
        }

        public void ClearDirtyFlag()
        {
            IsVoxelSizeDirty = false;
        }

        public void Reset()
        {
            lock (_resetLock)
            {
                LogToDebug("========== RESET START ==========", System.Drawing.Color.Orange);

                lock (_atomsLock)
                {
                    _atomsForScanning = null;
                }

                _maxDistFine = 0;
                _isReset = true;

 // ⭐ Reset flagi dirty
                IsVoxelSizeDirty = false;

                lock (_spatialDataLock)
                {
                    _sortedIndicesX = null;
                    _sortedX = null;
                    _spatialDataReady = false;
                    _spatialSnapshot = null; // ⭐ OPT: invalidate lock-free snapshot
                }

 // ⭐ Reset scan parameters
                RadiusMultiplier = 1.1f;
                ProbeRadius = 0.0f;
                UseSymmetry = true;
                ScanMargin = 2.0f;
                VoxelSize = 0.15f;

 // ⭐ Reset cell
                _minCorner = new FastVec3(SysVector3.Zero);
                _maxCorner = new FastVec3(SysVector3.Zero);
                _cellVolume = 0f;
                _cellSize = SysVector3.Zero;

 // ⭐ Reset matrixy
                _fracToCart = XnaMatrix.Identity;
                _sysFracToCart = SysMatrix4x4.Identity;

                LogToDebug("========== RESET COMPLETE ==========", System.Drawing.Color.Orange);
            }
        }

        #endregion

        #region Generate Atoms

        public AtomData[] GenerateAsymmetricAtomsWithNeighbors()
        {
            LogToDebug("========== GENERATE ATOMS START ==========", System.Drawing.Color.Cyan);

            lock (_resetLock)
            {
                LogToDebug($"_isReset: {_isReset}", System.Drawing.Color.LightGray);
            }

            lock (_atomsLock)
            {
                LogToDebug($"_atomsForScanning is null: {_atomsForScanning == null}", System.Drawing.Color.LightGray);
            }

            LogToDebug($"UseAnisotropicVdW: {Flags.Instance.UseThermalMotion}", System.Drawing.Color.LightGray);

            lock (_atomsLock)
            {
                _atomsForScanning = null;
            }

            _maxDistFine = 0;

            lock (_resetLock)
            {
                _isReset = true;
            }

            lock (_spatialDataLock)
            {
                _spatialDataReady = false;
                _spatialSnapshot = null; // ⭐ OPT: invalidate lock-free snapshot
            }

            var asymAtoms = _molecule.SymetryEquivalents;
            if (asymAtoms == null || asymAtoms.Count == 0)
            {
                LogToDebug("ERROR: No atoms in SymetryEquivalents!", System.Drawing.Color.Red);
                return new AtomData[0];
            }

            LogToDebug($"asymAtoms count: {asymAtoms.Count}", System.Drawing.Color.LightGray);

            int estimatedAtoms = asymAtoms.Count * 20;
            var result = new List<AtomData>(estimatedAtoms);
            var addedPositions = new HashSet<(int, int, int)>(estimatedAtoms);

            float radiusMult = RadiusMultiplier;
            float probeRad = ProbeRadius;
            var fracToCart = _fracToCart;
            bool useAnisotropic = Flags.Instance.UseThermalMotion;

            float marginFracX = ScanMargin / _cellSize.X;
            float marginFracY = ScanMargin / _cellSize.Y;
            float marginFracZ = ScanMargin / _cellSize.Z;
            float marginFrac = Math.Min(Math.Max(marginFracX, Math.Max(marginFracY, marginFracZ)), 0.3f);

            LogToDebug($"ScanMargin: {ScanMargin} A", System.Drawing.Color.LightGray);
            LogToDebug($"frac margin: X={marginFracX:F4}, Y={marginFracY:F4}, Z={marginFracZ:F4} -> using {marginFrac:F4}", System.Drawing.Color.LightGray);

            var symMatrix = _molecule.SymmetryOperations;

 // PATCH (ADP of symmetry copies): _molecule.SymetryEquivalents already contains the full cell,
 // with Un transformed per equivalent (Molecule.CalculateSymmetryEquivalents). Re-applying the
 // symmetry operations here made every orbit inherit the Un of its first atom (unrotated).
 // Set ReExpandSymmetry = true to restore the old behaviour for an A/B comparison.
            const bool ReExpandSymmetry = false;
            int maxSym = ReExpandSymmetry ? Math.Min(symMatrix.Count, 48) : Math.Min(symMatrix.Count, 1);

            var allCellAtoms = new List<AtomData>(asymAtoms.Count * maxSym);
            var cellAdded = new HashSet<(int, int, int)>(asymAtoms.Count * maxSym);

            const float eps = 0.0001f;

            LogProgress("Generating cell atoms...", 10);

            int atomCounter = 0;
            foreach (var atom in asymAtoms)
            {
                int atomicNumber = VdwParameters.GetAtomicNumber(atom.name);
                if (atomicNumber <= 0) atomicNumber = 6;

                float radius = VdwParameters.GetRadius(atom.name);
                if (radius <= 0) radius = VdwParameters.GetRadius(atomicNumber);

                float effRadius = radius * RadiusMultiplier + probeRad;
                float effRadiusSq = effRadius * effRadius;

 // ── Afterprawka: jednoznaczne przypisanie ──
 // Molecule.Atom.Un = matrix after Eigen.main (wektory own × semi-axes w wierszach)
 // Molecule.Atom.Ue = same as Un (Eigen.main(ref Ue) overwrote Ue)
 //
 // Do SDF we use TYLKO Un (patrz CavityDetector.ComputeSDFWithDensityOptimized).
 // Ue is copied for compatibility, but is not used for ellipsoid geometry.

                Matrix un_physical = atom.Un;
                Matrix ue_physical = atom.Ue;

                bool hasAnisotropicData = Flags.Instance.UseThermalMotion;

                for (int s = 0; s < maxSym; s++)
                {
                    var sym = symMatrix[s];
                    float fx = atom.x, fy = atom.y, fz = atom.z;

                    float nx = fx - (float)Math.Floor(fx);
                    float ny = fy - (float)Math.Floor(fy);
                    float nz = fz - (float)Math.Floor(fz);
                    if (nx > 1.0f - eps) nx = 0.0f;
                    if (ny > 1.0f - eps) ny = 0.0f;
                    if (nz > 1.0f - eps) nz = 0.0f;
                    if (nx < eps) nx = 0.0f;
                    if (ny < eps) ny = 0.0f;
                    if (nz < eps) nz = 0.0f;

                    float tx = 0, ty = 0, tz = 0;

                    if (sym.pos1[0] >= 0 && sym.pos1[0] < 3)
                    {
                        float coord = sym.pos1[0] == 0 ? nx : (sym.pos1[0] == 1 ? ny : nz);
                        tx += sym.sign1[0] * coord;
                    }
                    if (sym.pos2[0] >= 0 && sym.pos2[0] < 3 && sym.sign2[0] != 0)
                    {
                        float coord = sym.pos2[0] == 0 ? nx : (sym.pos2[0] == 1 ? ny : nz);
                        tx += sym.sign2[0] * coord;
                    }
                    tx += sym.shift[0];

                    if (sym.pos1[1] >= 0 && sym.pos1[1] < 3)
                    {
                        float coord = sym.pos1[1] == 0 ? nx : (sym.pos1[1] == 1 ? ny : nz);
                        ty += sym.sign1[1] * coord;
                    }
                    if (sym.pos2[1] >= 0 && sym.pos2[1] < 3 && sym.sign2[1] != 0)
                    {
                        float coord = sym.pos2[1] == 0 ? nx : (sym.pos2[1] == 1 ? ny : nz);
                        ty += sym.sign2[1] * coord;
                    }
                    ty += sym.shift[1];

                    if (sym.pos1[2] >= 0 && sym.pos1[2] < 3)
                    {
                        float coord = sym.pos1[2] == 0 ? nx : (sym.pos1[2] == 1 ? ny : nz);
                        tz += sym.sign1[2] * coord;
                    }
                    if (sym.pos2[2] >= 0 && sym.pos2[2] < 3 && sym.sign2[2] != 0)
                    {
                        float coord = sym.pos2[2] == 0 ? nx : (sym.pos2[2] == 1 ? ny : nz);
                        tz += sym.sign2[2] * coord;
                    }
                    tz += sym.shift[2];

 // PATCH: atom is already a symmetry equivalent - keep its own (wrapped) aftersition and its own Un
                    if (!ReExpandSymmetry) { tx = nx; ty = ny; tz = nz; }

                    tx = tx - (float)Math.Floor(tx);
                    ty = ty - (float)Math.Floor(ty);
                    tz = tz - (float)Math.Floor(tz);
                    if (tx > 1.0f - eps) tx = 0.0f;
                    if (ty > 1.0f - eps) ty = 0.0f;
                    if (tz > 1.0f - eps) tz = 0.0f;
                    if (tx < eps) tx = 0.0f;
                    if (ty < eps) ty = 0.0f;
                    if (tz < eps) tz = 0.0f;

                    if (tx >= -eps && tx < 1.0f + eps &&
                        ty >= -eps && ty < 1.0f + eps &&
                        tz >= -eps && tz < 1.0f + eps)
                    {
                        int keyX = (int)Math.Round(tx * 10000);
                        int keyY = (int)Math.Round(ty * 10000);
                        int keyZ = (int)Math.Round(tz * 10000);
                        var key = (keyX, keyY, keyZ);

                        if (cellAdded.Add(key))
                        {
                            var sysPos = SysVector3.Transform(
                                new SysVector3(tx, ty, tz),
                                _sysFracToCart);

                            AtomData atomData = new AtomData
                            {
                                X = sysPos.X,
                                Y = sysPos.Y,
                                Z = sysPos.Z,
                                Radius = effRadius,
                                RadiusSq = effRadiusSq,
                                FracX = tx,
                                FracY = ty,
                                FracZ = tz,
                                Un = un_physical,
                                Ue = ue_physical,
                                HasAnisotropicData = hasAnisotropicData,
                                MoleculeId = -1
                            };

                            allCellAtoms.Add(atomData);
                            atomCounter++;
                        }
                    }
                }
            }

            LogToDebug($"Generated {allCellAtoms.Count} atoms in cell", System.Drawing.Color.LightGray);

            foreach (var atom in allCellAtoms)
            {
                int keyX = (int)Math.Round(atom.FracX * 10000);
                int keyY = (int)Math.Round(atom.FracY * 10000);
                int keyZ = (int)Math.Round(atom.FracZ * 10000);
                addedPositions.Add((keyX, keyY, keyZ));
                result.Add(atom);
            }

            int estimatedNeighbors = allCellAtoms.Count * 26;
            var neighborAtoms = new List<AtomData>(estimatedNeighbors);

            var allNeighborOffsets = new[]
            {
                (-1, -1, -1), (-1, -1, 0), (-1, -1, 1),
                (-1, 0, -1), (-1, 0, 0), (-1, 0, 1),
                (-1, 1, -1), (-1, 1, 0), (-1, 1, 1),
                (0, -1, -1), (0, -1, 0), (0, -1, 1),
                (0, 0, -1),                   (0, 0, 1),
                (0, 1, -1), (0, 1, 0), (0, 1, 1),
                (1, -1, -1), (1, -1, 0), (1, -1, 1),
                (1, 0, -1), (1, 0, 0), (1, 0, 1),
                (1, 1, -1), (1, 1, 0), (1, 1, 1)
            };

            int neighborCounter = 0;
            foreach (var cellAtom in allCellAtoms)
            {
                foreach (var (dx, dy, dz) in allNeighborOffsets)
                {
                    if (dx == 0 && dy == 0 && dz == 0) continue;

                    float newFracX = cellAtom.FracX + dx;
                    float newFracY = cellAtom.FracY + dy;
                    float newFracZ = cellAtom.FracZ + dz;

                    bool isRelevant = newFracX >= -marginFrac && newFracX <= 1.0f + marginFrac &&
                                      newFracY >= -marginFrac && newFracY <= 1.0f + marginFrac &&
                                      newFracZ >= -marginFrac && newFracZ <= 1.0f + marginFrac;

                    if (!isRelevant) continue;

                    bool isNearWall = false;
                    if (newFracX < marginFrac || newFracX > 1.0f - marginFrac ||
                        newFracY < marginFrac || newFracY > 1.0f - marginFrac ||
                        newFracZ < marginFrac || newFracZ > 1.0f - marginFrac)
                    {
                        isNearWall = true;
                    }

                    if (!isNearWall) continue;

                    int keyX = (int)Math.Round(newFracX * 10000);
                    int keyY = (int)Math.Round(newFracY * 10000);
                    int keyZ = (int)Math.Round(newFracZ * 10000);
                    var key = (keyX, keyY, keyZ);

                    if (addedPositions.Add(key))
                    {
                        var sysPos = SysVector3.Transform(
                            new SysVector3(newFracX, newFracY, newFracZ),
                            _sysFracToCart);

                        AtomData neighborData = new AtomData
                        {
                            X = sysPos.X,
                            Y = sysPos.Y,
                            Z = sysPos.Z,
                            Radius = cellAtom.Radius,
                            RadiusSq = cellAtom.RadiusSq,
                            FracX = newFracX,
                            FracY = newFracY,
                            FracZ = newFracZ,
                            Un = cellAtom.Un,
                            Ue = cellAtom.Ue,
                            HasAnisotropicData = cellAtom.HasAnisotropicData,
                            MoleculeId = -1
                        };

                        neighborAtoms.Add(neighborData);
                        neighborCounter++;
                    }
                }
            }

            LogToDebug($"Generated {neighborAtoms.Count} neighbor atoms", System.Drawing.Color.LightGray);

            result.AddRange(neighborAtoms);

            result = RemoveDuplicateAtomsByCartesian(result);

 // Molecule detection on cell atoms [0,1)
            var cellAtomsForDetection = new List<AtomData>();
            var cellIndicesForDetection = new List<int>();

            for (int i = 0; i < result.Count; i++)
            {
                float fx = result[i].FracX;
                float fy = result[i].FracY;
                float fz = result[i].FracZ;

                if (fx >= 0.0f && fx < 1.0f &&
                    fy >= 0.0f && fy < 1.0f &&
                    fz >= 0.0f && fz < 1.0f)
                {
                    cellAtomsForDetection.Add(result[i]);
                    cellIndicesForDetection.Add(i);
                }
            }

            LogToDebug($"Cell atoms for molecule detection: {cellAtomsForDetection.Count}", System.Drawing.Color.LightGray);
            LogToDebug($"Neighbor atoms (SDF only): {result.Count - cellAtomsForDetection.Count}", System.Drawing.Color.LightGray);

            float contactScale = CavityDetector.BASE_SDF_RADIUS;
            var moleculeGroups = DetectMoleculesPeriodic(cellAtomsForDetection.ToArray(), contactScale);

            LogToDebug($"Detected {moleculeGroups.Count} molecules", System.Drawing.Color.LightGray);

            var moleculeMap = new Dictionary<(int, int, int), int>();
            const int scale = 10000;

            foreach (var group in moleculeGroups)
            {
                foreach (var atom in group.Atoms)
                {
                    int keyX = (int)Math.Round(atom.FracX * scale);
                    int keyY = (int)Math.Round(atom.FracY * scale);
                    int keyZ = (int)Math.Round(atom.FracZ * scale);
                    moleculeMap[(keyX, keyY, keyZ)] = group.Id;
                }
            }

            int assignedCount = 0;
            for (int i = 0; i < result.Count; i++)
            {
                float fx = result[i].FracX;
                float fy = result[i].FracY;
                float fz = result[i].FracZ;

                if (fx >= 0.0f && fx < 1.0f &&
                    fy >= 0.0f && fy < 1.0f &&
                    fz >= 0.0f && fz < 1.0f)
                {
                    int keyX = (int)Math.Round(fx * scale);
                    int keyY = (int)Math.Round(fy * scale);
                    int keyZ = (int)Math.Round(fz * scale);

                    if (moleculeMap.TryGetValue((keyX, keyY, keyZ), out int molId))
                    {
                        AtomData updatedAtom = result[i];
                        updatedAtom.MoleculeId = molId;
                        result[i] = updatedAtom;
                        assignedCount++;
                    }
                }
                else
                {
                    AtomData updatedAtom = result[i];
                    updatedAtom.MoleculeId = -1;
                    result[i] = updatedAtom;
                }
            }

            LogToDebug($"MoleculeId assigned: {assignedCount} atoms, {result.Count - assignedCount} atoms (neighbors) have MoleculeId = -1", System.Drawing.Color.LightGray);

            lock (_atomsLock)
            {
                _atomsForScanning = result.ToArray();
            }

            LogProgress("Detecting internal atoms...", 50);
            DetectInternalAtoms(_atomsForScanning);

            float maxRadius = 0;
            foreach (var atom in _atomsForScanning)
            {
                if (atom.Radius > maxRadius) maxRadius = atom.Radius;
            }
            _maxDistFine = 5.0f;

            LogToDebug($"maxRadius: {maxRadius:F4}, _maxDistFine: {_maxDistFine:F4}", System.Drawing.Color.LightGray);

            PrepareSpatialData();

            lock (_resetLock)
            {
                _isReset = false;
            }

            LogToDebug("===== SCANNING ATOMS SUMMARY =====", System.Drawing.Color.Cyan);
            LogToDebug($"Cell atoms: {allCellAtoms.Count}", System.Drawing.Color.LightGray);
            LogToDebug($"Neighbor atoms: {neighborAtoms.Count}", System.Drawing.Color.LightGray);
            LogToDebug($"Total atoms for scanning: {_atomsForScanning.Length}", System.Drawing.Color.LightGreen);
            LogToDebug($"Molecules detected: {moleculeGroups.Count}", System.Drawing.Color.LightGreen);
            LogToDebug("========== GENERATE ATOMS COMPLETE ==========", System.Drawing.Color.Cyan);

            return _atomsForScanning;
        }

        #endregion

        #region Helper Methods

        private List<AtomData> RemoveDuplicateAtomsByCartesian(List<AtomData> atoms, float tolerance = 0.001f)
        {
            if (atoms == null || atoms.Count == 0)
                return atoms;

            var uniqueAtoms = new List<AtomData>(atoms.Count);
            var seenPositions = new HashSet<(int, int, int)>(atoms.Count / 2);

            const int scale = 1000;
            int duplicateCount = 0;

            foreach (var atom in atoms)
            {
                int keyX = (int)Math.Round(atom.X * scale);
                int keyY = (int)Math.Round(atom.Y * scale);
                int keyZ = (int)Math.Round(atom.Z * scale);
                var key = (keyX, keyY, keyZ);

                if (seenPositions.Add(key))
                {
                    uniqueAtoms.Add(atom);
                }
                else
                {
                    duplicateCount++;
                }
            }

            if (duplicateCount > 0)
            {
                LogToDebug($"Removed {duplicateCount} duplicate atoms", System.Drawing.Color.LightGray);
            }

            return uniqueAtoms;
        }

        private List<MoleculeGroup> DetectMoleculesPeriodic(AtomData[] atoms, float vdwScale = 0.8f)
        {
            if (atoms == null || atoms.Length == 0)
                return new List<MoleculeGroup>();

            var groups = new List<MoleculeGroup>();
            int nextId = 0;
            var visited = new bool[atoms.Length];

            LogToDebug($"Detecting molecules on {atoms.Length} cell atoms", System.Drawing.Color.LightGray);

            for (int i = 0; i < atoms.Length; i++)
            {
                if (visited[i]) continue;

                var group = new MoleculeGroup { Id = nextId++ };
                var queue = new Queue<int>();
                queue.Enqueue(i);
                visited[i] = true;

                while (queue.Count > 0)
                {
                    int idx = queue.Dequeue();
                    group.Atoms.Add(atoms[idx]);
                    group.AtomIndices.Add(idx);

                    AtomData current = atoms[idx];

                    for (int j = 0; j < atoms.Length; j++)
                    {
                        if (visited[j]) continue;
                        if (j == idx) continue;

                        AtomData other = atoms[j];

                        float dFracX = other.FracX - current.FracX;
                        float dFracY = other.FracY - current.FracY;
                        float dFracZ = other.FracZ - current.FracZ;

                        if (dFracX > 0.5f) dFracX -= 1.0f;
                        else if (dFracX < -0.5f) dFracX += 1.0f;

                        if (dFracY > 0.5f) dFracY -= 1.0f;
                        else if (dFracY < -0.5f) dFracY += 1.0f;

                        if (dFracZ > 0.5f) dFracZ -= 1.0f;
                        else if (dFracZ < -0.5f) dFracZ += 1.0f;

                        var deltaVec = SysVector3.Transform(
                            new SysVector3(dFracX, dFracY, dFracZ),
                            _sysFracToCart
                        );

                        float dist = (float)Math.Sqrt(
                            deltaVec.X * deltaVec.X +
                            deltaVec.Y * deltaVec.Y +
                            deltaVec.Z * deltaVec.Z
                        );

                        if (dist < 0.001f)
                        {
                            visited[j] = true;
                            queue.Enqueue(j);
                            continue;
                        }

                        float contactRadius = CalculateContactRadius(current, other, dist, vdwScale);

                        if (dist < contactRadius)
                        {
                            visited[j] = true;
                            queue.Enqueue(j);
                        }
                    }
                }

                groups.Add(group);

 // ⭐ NOTE: LogToDebug per molecule REMOVED from the loop —
 // instead we only log a summary after the loop.
            }

            LogToDebug($"Total: {groups.Count} molecules, {groups.Sum(g => g.Atoms.Count)} atoms", System.Drawing.Color.LightGray);
            return groups;
        }

        private float CalculateContactRadius(AtomData a, AtomData b, float distance, float vdwScale)
        {
            string elemA = GetElementName(a);
            string elemB = GetElementName(b);

            int atomicA = VdwParameters.GetAtomicNumber(elemA);
            int atomicB = VdwParameters.GetAtomicNumber(elemB);

            bool isH_A = (elemA == "H" || elemA == "D" || elemA == "T");
            bool isH_B = (elemB == "H" || elemB == "D" || elemB == "T");

            bool isHal_A = (elemA == "F" || elemA == "Cl" || elemA == "Br" || elemA == "I" || elemA == "At");
            bool isHal_B = (elemB == "F" || elemB == "Cl" || elemB == "Br" || elemB == "I" || elemB == "At");

            bool isMetal_A = IsMetal(elemA);
            bool isMetal_B = IsMetal(elemB);

            if (isH_A || isH_B)
            {
                AtomData hydrogen, other;
                if (isH_A)
                {
                    hydrogen = a;
                    other = b;
                }
                else
                {
                    hydrogen = b;
                    other = a;
                }

                bool isCovalent = distance < 1.3f;

                if (isCovalent)
                {
                    float radiusH = hydrogen.Radius * 0.92f * vdwScale;
                    float radiusOther = other.Radius * vdwScale;
                    return radiusH + radiusOther;
                }
                else
                {
                    float radiusH = hydrogen.Radius * 0.60f * vdwScale;
                    float radiusOther = other.Radius * vdwScale;
                    return (radiusH + radiusOther) * 0.90f;
                }
            }

            if (isHal_A || isHal_B)
            {
                AtomData halogen, other;
                if (isHal_A)
                {
                    halogen = a;
                    other = b;
                }
                else
                {
                    halogen = b;
                    other = a;
                }

                bool isCovalent = distance < 2.2f;
                float halogenFactor = GetHalogenFactor(halogen);

                if (isCovalent)
                {
                    float radiusHal = halogen.Radius * 0.92f * vdwScale * halogenFactor;
                    float radiusOther = other.Radius * vdwScale;
                    return radiusHal + radiusOther;
                }
                else
                {
                    float radiusHal = halogen.Radius * 0.60f * vdwScale * halogenFactor;
                    float radiusOther = other.Radius * vdwScale;
                    return (radiusHal + radiusOther) * 0.90f;
                }
            }

            if (isMetal_A || isMetal_B)
            {
                float radiusA = a.Radius * vdwScale;
                float radiusB = b.Radius * vdwScale;
                float sumVdw = radiusA + radiusB;

                float maxBondLength = 3.5f;
                float covalentFactor = 0.85f;

                if (distance < maxBondLength && distance < sumVdw * covalentFactor)
                {
                    return sumVdw;
                }
                else
                {
                    return sumVdw * 0.95f;
                }
            }

            bool isOrganic_A = IsOrganic(elemA);
            bool isOrganic_B = IsOrganic(elemB);

            if (isOrganic_A && isOrganic_B)
            {
                float radiusA = a.Radius * vdwScale;
                float radiusB = b.Radius * vdwScale;
                float sumVdw = radiusA + radiusB;

                float covalentFactor = 0.70f;

                if (distance < sumVdw * covalentFactor)
                {
                    return sumVdw;
                }
                else
                {
                    return sumVdw * 0.95f;
                }
            }

            float radiusA_def = a.Radius * vdwScale;
            float radiusB_def = b.Radius * vdwScale;
            float sumVdw_def = radiusA_def + radiusB_def;

            if (distance < sumVdw_def * 0.70f)
            {
                return sumVdw_def;
            }

            return sumVdw_def * 0.95f;
        }

        private string GetElementName(AtomData atom)
        {
            var symAtoms = _molecule.SymetryEquivalents;
            if (symAtoms != null)
            {
                foreach (var symAtom in symAtoms)
                {
                    if (Math.Abs(symAtom.x - atom.FracX) < 0.001f &&
                        Math.Abs(symAtom.y - atom.FracY) < 0.001f &&
                        Math.Abs(symAtom.z - atom.FracZ) < 0.001f)
                    {
                        return symAtom.name;
                    }
                }
            }
            return "C";
        }

        private bool IsMetal(string element)
        {
            string[] metals = {
                "Li", "Na", "K", "Rb", "Cs", "Fr",
                "Be", "Mg", "Ca", "Sr", "Ba", "Ra",
                "Sc", "Ti", "V", "Cr", "Mn", "Fe", "Co", "Ni", "Cu", "Zn",
                "Y", "Zr", "Nb", "Mo", "Tc", "Ru", "Rh", "Pd", "Ag", "Cd",
                "Hf", "Ta", "W", "Re", "Os", "Ir", "Pt", "Au", "Hg",
                "Al", "Ga", "In", "Sn", "Tl", "Pb", "Bi",
                "La", "Ce", "Pr", "Nd", "Pm", "Sm", "Eu", "Gd", "Tb", "Dy",
                "Ho", "Er", "Tm", "Yb", "Lu"
            };
            return metals.Contains(element);
        }

        private bool IsOrganic(string element)
        {
            string[] organic = { "C", "N", "O", "S", "P", "Se", "Te", "B", "Si", "Ge", "As", "Sb" };
            return organic.Contains(element);
        }

        private float GetHalogenFactor(AtomData halogen)
        {
            string elem = GetElementName(halogen);
            switch (elem)
            {
                case "F": return 0.5f;
                case "Cl": return 1.0f;
                case "Br": return 1.3f;
                case "I": return 1.5f;
                case "At": return 1.7f;
                default: return 1.0f;
            }
        }

        #endregion

        #region Internal Atom Detection

        private void DetectInternalAtoms(AtomData[] atoms)
        {
            if (atoms == null || atoms.Length < 50)
            {
                for (int i = 0; i < atoms.Length; i++)
                {
                    AtomData updated = atoms[i];
                    updated.IsInternal = false;
                    atoms[i] = updated;
                }
                return;
            }

            int maxParallel = Math.Max(1, Environment.ProcessorCount / 2);
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel };

            float searchRadius = 2.8f;
            float searchRadiusSq = searchRadius * searchRadius;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            for (int i = 0; i < atoms.Length; i++)
            {
                float x = atoms[i].X, y = atoms[i].Y, z = atoms[i].Z;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                if (z < minZ) minZ = z;
                if (z > maxZ) maxZ = z;
            }

            float cellSize = searchRadius;
            int gridNX = (int)Math.Ceiling((maxX - minX) / cellSize) + 1;
            int gridNY = (int)Math.Ceiling((maxY - minY) / cellSize) + 1;
            int gridNZ = (int)Math.Ceiling((maxZ - minZ) / cellSize) + 1;

            int totalGridCells = gridNX * gridNY * gridNZ;
            int[] gridData = new int[totalGridCells * 8];
            int[] gridCount = new int[totalGridCells];

            for (int i = 0; i < atoms.Length; i++)
            {
                int gx = (int)((atoms[i].X - minX) / cellSize);
                int gy = (int)((atoms[i].Y - minY) / cellSize);
                int gz = (int)((atoms[i].Z - minZ) / cellSize);

                if (gx < 0) gx = 0;
                if (gx >= gridNX) gx = gridNX - 1;
                if (gy < 0) gy = 0;
                if (gy >= gridNY) gy = gridNY - 1;
                if (gz < 0) gz = 0;
                if (gz >= gridNZ) gz = gridNZ - 1;

                int cellIdx = (gx * gridNY + gy) * gridNZ + gz;
                int count = gridCount[cellIdx];
                if (count < 8)
                {
                    gridData[cellIdx * 8 + count] = i;
                    gridCount[cellIdx] = count + 1;
                }
            }

            var atomsLocal = atoms;
            var gridDataLocal = gridData;
            var gridCountLocal = gridCount;
            float minXLocal = minX, minYLocal = minY, minZLocal = minZ;
            float cellSizeLocal = cellSize;
            int gridNXLocal = gridNX, gridNYLocal = gridNY, gridNZLocal = gridNZ;
            float searchRadiusSqLocal = searchRadiusSq;

            Parallel.For(0, atoms.Length, options, i =>
            {
                float atomX = atomsLocal[i].X;
                float atomY = atomsLocal[i].Y;
                float atomZ = atomsLocal[i].Z;
                float atomRadius = atomsLocal[i].Radius;

                int octantMask = 0;
                int neighbors = 0;

                int gx = (int)((atomX - minXLocal) / cellSizeLocal);
                int gy = (int)((atomY - minYLocal) / cellSizeLocal);
                int gz = (int)((atomZ - minZLocal) / cellSizeLocal);

                if (gx < 0) gx = 0;
                if (gx >= gridNXLocal) gx = gridNXLocal - 1;
                if (gy < 0) gy = 0;
                if (gy >= gridNYLocal) gy = gridNYLocal - 1;
                if (gz < 0) gz = 0;
                if (gz >= gridNZLocal) gz = gridNZLocal - 1;

                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = gx + dx;
                    if (nx < 0 || nx >= gridNXLocal) continue;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = gy + dy;
                        if (ny < 0 || ny >= gridNYLocal) continue;

                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int nz = gz + dz;
                            if (nz < 0 || nz >= gridNZLocal) continue;

                            int cellIdx = (nx * gridNYLocal + ny) * gridNZLocal + nz;
                            int count = gridCountLocal[cellIdx];
                            int baseIdx = cellIdx * 8;

                            for (int k = 0; k < count; k++)
                            {
                                int j = gridDataLocal[baseIdx + k];
                                if (i == j) continue;

                                float dx2 = atomsLocal[j].X - atomX;
                                float dy2 = atomsLocal[j].Y - atomY;
                                float dz2 = atomsLocal[j].Z - atomZ;

                                float dist2 = dx2 * dx2 + dy2 * dy2 + dz2 * dz2;
                                if (dist2 > searchRadiusSqLocal) continue;

                                neighbors++;

                                if (dx2 > 0) octantMask |= 1;
                                if (dy2 > 0) octantMask |= 2;
                                if (dz2 > 0) octantMask |= 4;
                            }
                        }
                    }
                }

                int filledOctants = BitCount(octantMask);
                bool isInternal = (filledOctants >= 6);
                if (atomRadius < 0.5f)
                    isInternal = (filledOctants >= 7);

                AtomData updatedAtom = atomsLocal[i];
                updatedAtom.IsInternal = isInternal;
                atomsLocal[i] = updatedAtom;
            });

#if DEBUG
            int internalCount = 0;
            for (int i = 0; i < atoms.Length; i++)
                if (atoms[i].IsInternal) internalCount++;
            LogToDebug($"Internal atoms: {internalCount} / {atoms.Length} ({(float)internalCount / atoms.Length * 100:F1}%)", System.Drawing.Color.LightGray);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BitCount(int n)
        {
            n = n - ((n >> 1) & 0x55555555);
            n = (n & 0x33333333) + ((n >> 2) & 0x33333333);
            return ((n + (n >> 4) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        #endregion

        #region Spatial Data Preparation

        private void PrepareSpatialData()
        {
            AtomData[] atoms;
            lock (_atomsLock)
            {
                atoms = _atomsForScanning;
            }

            if (atoms == null || atoms.Length == 0)
                return;

            lock (_spatialDataLock)
            {
                int count = atoms.Length;
                float maxDist = _maxDistFine;

                LogToDebug($"Preparing spatial data for {count} atoms...", System.Drawing.Color.LightGray);

                for (int i = 0; i < count; i++)
                {
                    float searchRadius = atoms[i].Radius + maxDist;

                    AtomData updated = atoms[i];
                    updated.MinX = atoms[i].X - searchRadius;
                    updated.MaxX = atoms[i].X + searchRadius;
                    updated.MinY = atoms[i].Y - searchRadius;
                    updated.MaxY = atoms[i].Y + searchRadius;
                    updated.MinZ = atoms[i].Z - searchRadius;
                    updated.MaxZ = atoms[i].Z + searchRadius;

                    atoms[i] = updated;
                }

                _sortedIndicesX = new int[count];
                for (int i = 0; i < count; i++)
                    _sortedIndicesX[i] = i;

                Array.Sort(_sortedIndicesX, (a, b) => atoms[a].X.CompareTo(atoms[b].X));

                _sortedX = new float[count];
                for (int i = 0; i < count; i++)
                    _sortedX[i] = atoms[_sortedIndicesX[i]].X;

                _spatialDataReady = true;

 // ⭐ OPT: Publish the lock-free snapshot atomically (volatile write).
 // From this afterint on, FindAtomsGradualFast can read all spatial data
 // without acquiring any lock.
                _spatialSnapshot = new SpatialSnapshot
                {
                    Atoms = atoms,
                    SortedIndicesX = _sortedIndicesX,
                    SortedX = _sortedX
                };

                LogToDebug($"Spatial data prepared: {count} atoms sorted by X", System.Drawing.Color.LightGreen);
                LogToDebug($"  X: min={_sortedX[0]:F3}, max={_sortedX[count - 1]:F3}", System.Drawing.Color.LightGray);
                LogToDebug($"  Anisotropic atoms: {atoms.Count(a => a.HasAnisotropicData)}", System.Drawing.Color.LightGray);
            }
        }

        #endregion

        #region Find Atoms

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FindAtomsGradualFast(float px, float py, float pz, int[] output)
        {
            if (output == null)
                return 0;

 // ⭐ OPT: Lock-free read of the immutable snapshot. The snapshot is published
 // atomically once PrepareSpatialData() completes, so no lock is needed here
 // even when called from millions of parallel voxel-scanning threads.
            var snap = _spatialSnapshot;
            if (snap == null)
                return 0;

            int[] sortedIndicesX = snap.SortedIndicesX;
            float[] sortedX = snap.SortedX;
            AtomData[] atoms = snap.Atoms;

            if (atoms == null || atoms.Length == 0)
                return 0;

            int count = atoms.Length;
            int outIdx = 0;

            for (int rIdx = 0; rIdx < _searchRadii.Length; rIdx++)
            {
                float radius = _searchRadii[rIdx];
                float radius2 = _searchRadii2[rIdx];

                int startX = Array.BinarySearch(sortedX, px - radius);
                if (startX < 0) startX = ~startX;
                int endX = Array.BinarySearch(sortedX, px + radius);
                if (endX < 0) endX = ~endX;
                endX = Math.Min(count, endX);

                if (startX >= endX)
                    continue;

                for (int si = startX; si < endX; si++)
                {
                    int i = sortedIndicesX[si];

                    if (py < atoms[i].MinY || py > atoms[i].MaxY) continue;
                    if (pz < atoms[i].MinZ || pz > atoms[i].MaxZ) continue;
                    if (atoms[i].IsInternal) continue;

                    float dx = px - atoms[i].X;
                    float dy = py - atoms[i].Y;
                    float dz = pz - atoms[i].Z;
                    float dist2 = dx * dx + dy * dy + dz * dz;

                    if (dist2 > radius2) continue;

                    if (outIdx < output.Length)
                        output[outIdx++] = i;
                }

                if (outIdx > 0)
                    return outIdx;
            }

            return outIdx;
        }

        #endregion

        #region Create Crystal Grid

        public VoxelGrid CreateCrystalGrid(
            SysVector3 origin,
            SysVector3 vecA, SysVector3 vecB, SysVector3 vecC,
            int resX, int resY, int resZ,
            int marginX, int marginY, int marginZ,
            bool isFine = false)
        {
            int totalResX = resX + 2 * marginX;
            int totalResY = resY + 2 * marginY;
            int totalResZ = resZ + 2 * marginZ;

            float stepFracX = 1.0f / (resX - 1);
            float stepFracY = 1.0f / (resY - 1);
            float stepFracZ = 1.0f / (resZ - 1);

            float startFracX = -marginX * stepFracX;
            float startFracY = -marginY * stepFracY;
            float startFracZ = -marginZ * stepFracZ;

            var fracX = new float[totalResX, totalResY, totalResZ];
            var fracY = new float[totalResX, totalResY, totalResZ];
            var fracZ = new float[totalResX, totalResY, totalResZ];
            var positions = new SysVector3[totalResX, totalResY, totalResZ];

            int maxParallel = Math.Max(1, Environment.ProcessorCount / 2);
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel };

            Parallel.For(0, totalResX, options, ix =>
            {
                float fx = startFracX + ix * stepFracX;
                for (int iy = 0; iy < totalResY; iy++)
                {
                    float fy = startFracY + iy * stepFracY;
                    for (int iz = 0; iz < totalResZ; iz++)
                    {
                        float fz = startFracZ + iz * stepFracZ;
                        fracX[ix, iy, iz] = fx;
                        fracY[ix, iy, iz] = fy;
                        fracZ[ix, iy, iz] = fz;
                        positions[ix, iy, iz] = origin + fx * vecA + fy * vecB + fz * vecC;
                    }
                }
            });

            var minCorner = origin + startFracX * vecA + startFracY * vecB + startFracZ * vecC;
            var maxCorner = origin + (startFracX + (totalResX - 1) * stepFracX) * vecA
                                   + (startFracY + (totalResY - 1) * stepFracY) * vecB
                                   + (startFracZ + (totalResZ - 1) * stepFracZ) * vecC;

            CellParameters.Set(origin, vecA, vecB, vecC, stepFracX, stepFracY, stepFracZ, VoxelSize);

            LogToDebug($"Created crystal grid: {totalResX}x{totalResY}x{totalResZ}", System.Drawing.Color.LightGray);
            LogToDebug($"BoundsMin: {minCorner}", System.Drawing.Color.LightGray);
            LogToDebug($"BoundsMax: {maxCorner}", System.Drawing.Color.LightGray);

            return new VoxelGrid
            {
                FracX = fracX,
                FracY = fracY,
                FracZ = fracZ,
                Positions = positions,
                ResX = (short)totalResX,
                ResY = (short)totalResY,
                ResZ = (short)totalResZ,
                BoundsMin = minCorner,
                BoundsMax = maxCorner,
                IsFine = isFine,
                Origin = origin,
                VecA = vecA,
                VecB = vecB,
                VecC = vecC
            };
        }

        #endregion
    }
}