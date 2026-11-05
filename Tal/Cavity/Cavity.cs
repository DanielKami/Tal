// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: Cavity.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using static Tal.Cavity.VoxelScanner;
using SysVector3 = System.Numerics.Vector3;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;

namespace Tal.Cavity
{
 /// <summary>
 /// Represents a single cavity (void) detected in the crystal structure.
 /// Stores all geometric, toafterlogscal, density, and analytical data.
 /// </summary>
    public class Cavity : IDisposable
    {
        #region Private Fields

        private Molecule _molecule;
        private bool _disposed = false;

        #endregion

        #region Constructors

 /// <summary>
 /// Initializes a new instance of the <see cref="Cavity"/> class.
 /// </summary>
        public Cavity()
        {
            InitializeCollections();
        }

 /// <summary>
 /// Initializes a new instance of the <see cref="Cavity"/> class with basic data.
 /// </summary>
 /// <param name="id">The cavity identifier.</param>
 /// <param name="volume">The cavity volume in cubic angstroms.</param>
 /// <param name="center">The cavity center aftersition.</param>
 /// <param name="color">The base color for rendering.</param>
        public Cavity(int id, float volume, SysVector3 center, XnaVector4? color = null)
        {
            InitializeCollections();
            Id = id;
            Volume = volume;
            Center = center;
            BaseColor4 = color ?? XnaVector4.One;
        }

        private void InitializeCollections()
        {
            RawVertices = new List<SysVector3>();
            RawIndices = new List<int>();
            Vertices = new List<SysVector3>();
            Indices = new List<int>();
            Voxels = new List<SysVector3>();
            CrystalChannelResults = new Dictionary<string, CavityPathFinder.ChannelPathResult>();
            CrystalHelicityData = new Dictionary<string, float>();
            CrystalChannelPaths = new Dictionary<string, List<Microsoft.Xna.Framework.Vector3>>();
            MultiChannelResults = new Dictionary<string, MultiChannelResult>();
            AllChannelPaths = new List<ExtendedChannelPathResult>();
            ConnectedCavities = new HashSet<int>();
            SphereFits = new List<SphereFitAnalyzer.SphereFitResult>();
        }

        #endregion

        #region Public Properties

 /// <summary>
 /// Gets or sets the last mapped property type.
 /// </summary>
        public CavityPropertyMapper.PropertyType LastMappedProperty { get; set; } = CavityPropertyMapper.PropertyType.None;

 /// <summary>
 /// Gets or sets the associated molecule.
 /// </summary>
        public Molecule Molecule
        {
            get => _molecule;
            set => _molecule = value;
        }

 /// <summary>
 /// Gets or sets the cavity identifier.
 /// </summary>
        public int Id { get; set; }

        /// <summary>When false, cavity is not drawn in 3D (per-cavity checkbox).</summary>
        public bool Visible { get; set; } = true;

        #endregion

        #region Raw Mesh Data

 /// <summary>
 /// Gets or sets the raw vertices before smoothing.
 /// </summary>
        public List<SysVector3> RawVertices { get; set; }

 /// <summary>
 /// Gets or sets the raw triangle indices before smoothing.
 /// </summary>
        public List<int> RawIndices { get; set; }

 /// <summary>
 /// Gets or sets a value indicating whether raw triangles have been generated.
 /// </summary>
        public bool RawTrianglesGenerated { get; set; } = false;

        #endregion

        #region Smoothed Mesh Data

 /// <summary>
 /// Gets or sets the smoothed vertices for display.
 /// </summary>
        public List<SysVector3> Vertices { get; set; }

 /// <summary>
 /// Gets or sets the smoothed triangle indices for display.
 /// </summary>
        public List<int> Indices { get; set; }

 /// <summary>
 /// Gets or sets a value indicating whether smoothed triangles have been generated.
 /// </summary>
        public bool TrianglesGenerated { get; set; } = false;

 /// <summary>
 /// Gets or sets the vertex colors as XnaVector4 array.
 /// </summary>
        public XnaVector4[] VertexColors4 { get; set; } = null;

 /// <summary>
 /// Gets a value indicating whether vertex colors are available.
 /// </summary>
        public bool HasVertexColors4 => VertexColors4 != null && VertexColors4.Length > 0;

 /// <summary>
 /// Gets or sets the base color for the cavity.
 /// </summary>
        public XnaVector4 BaseColor4 { get; set; } = XnaVector4.One;

        #endregion

        #region Geometric Data

 /// <summary>
 /// Gets or sets the list of voxels that belong to this cavity.
 /// </summary>
        public List<SysVector3> Voxels { get; set; }

 /// <summary>
 /// Gets or sets the cavity center aftersition.
 /// </summary>
        public SysVector3 Center { get; set; }

 /// <summary>
 /// Gets or sets the cavity volume in cubic angstroms.
 /// </summary>
        public float Volume { get; set; }

 /// <summary>
 /// Gets or sets the cavity surface area in square angstroms.
 /// </summary>
        public float SurfaceArea { get; set; }

 /// <summary>
 /// Gets or sets the sphericity value (1.0 = perfect sphere).
 /// </summary>
        public float Sphericity { get; set; }

 /// <summary>
 /// Gets or sets a value indicating whether this cavity touches the exterior.
 /// </summary>
        public bool IsExterior { get; set; } = false;

 /// <summary>
 /// Gets or sets the bounding box of the cavity.
 /// </summary>
        public BoundingBox BoundingBox { get; set; }

 /// <summary>
 /// Gets or sets the true cell bounding box.
 /// </summary>
        public BoundingBox TrueCellBox { get; set; }

        #endregion

        #region Density Grid Data

 /// <summary>
 /// Gets or sets the density grid values.
 /// </summary>
        public float[,,] DensityGrid { get; set; }

 /// <summary>
 /// Gets or sets the number of grid afterints in the X direction.
 /// </summary>
        public int DensityNx { get; set; }

 /// <summary>
 /// Gets or sets the number of grid afterints in the Y direction.
 /// </summary>
        public int DensityNy { get; set; }

 /// <summary>
 /// Gets or sets the number of grid afterints in the Z direction.
 /// </summary>
        public int DensityNz { get; set; }

 /// <summary>
 /// Gets the grid step in the X direction.
 /// </summary>
        public float DensityStepX => Grid?.StepX ?? CellParameters.StepX;

 /// <summary>
 /// Gets the grid step in the Y direction.
 /// </summary>
        public float DensityStepY => Grid?.StepY ?? CellParameters.StepY;

 /// <summary>
 /// Gets the grid step in the Z direction.
 /// </summary>
        public float DensityStepZ => Grid?.StepZ ?? CellParameters.StepZ;

 /// <summary>
 /// Gets or sets the voxel grid.
 /// </summary>
        public VoxelGrid Grid { get; set; }

 /// <summary>
 /// Gets or sets the inverse fractional to Cartesian transformation matrix.
 /// </summary>
        public Matrix InvFracToCart { get; set; }

 /// <summary>
 /// Gets or sets the minimum corner of the grid.
 /// </summary>
        public SysVector3 MinCorner { get; set; }

 /// <summary>
 /// Gets or sets the voxel mask indicating cavity membership.
 /// </summary>
        public bool[,,] VoxelMask { get; set; }

        #endregion

        #region Periodic Grid

 /// <summary>
 /// Gets or sets the periodic voxel grid.
 /// </summary>
        public VoxelGrid PeriodicGrid { get; set; }

 /// <summary>
 /// Gets or sets the X offset for periodic replication.
 /// </summary>
        public int OffsetX { get; set; }

 /// <summary>
 /// Gets or sets the Y offset for periodic replication.
 /// </summary>
        public int OffsetY { get; set; }

 /// <summary>
 /// Gets or sets the Z offset for periodic replication.
 /// </summary>
        public int OffsetZ { get; set; }

        #endregion

        #region Gradient Data

 /// <summary>
 /// Gets or sets the X-comafternent of the supercell gradient.
 /// </summary>
        public float[,,] SupercellGradientX { get; set; }

 /// <summary>
 /// Gets or sets the Y-comafternent of the supercell gradient.
 /// </summary>
        public float[,,] SupercellGradientY { get; set; }

 /// <summary>
 /// Gets or sets the Z-comafternent of the supercell gradient.
 /// </summary>
        public float[,,] SupercellGradientZ { get; set; }

        #endregion

        #region Wall Contact Data

 /// <summary>
 /// Gets or sets the number of wall contacts in the -X direction.
 /// </summary>
        public int WallXMin { get; set; } = 0;

 /// <summary>
 /// Gets or sets the number of wall contacts in the +X direction.
 /// </summary>
        public int WallXMax { get; set; } = 0;

 /// <summary>
 /// Gets or sets the number of wall contacts in the -Y direction.
 /// </summary>
        public int WallYMin { get; set; } = 0;

 /// <summary>
 /// Gets or sets the number of wall contacts in the +Y direction.
 /// </summary>
        public int WallYMax { get; set; } = 0;

 /// <summary>
 /// Gets or sets the number of wall contacts in the -Z direction.
 /// </summary>
        public int WallZMin { get; set; } = 0;

 /// <summary>
 /// Gets or sets the number of wall contacts in the +Z direction.
 /// </summary>
        public int WallZMax { get; set; } = 0;

 /// <summary>
 /// Gets or sets a value indicating whether the cavity touches the -X wall.
 /// </summary>
        public bool TouchesXMin { get; set; } = false;

 /// <summary>
 /// Gets or sets a value indicating whether the cavity touches the +X wall.
 /// </summary>
        public bool TouchesXMax { get; set; } = false;

 /// <summary>
 /// Gets or sets a value indicating whether the cavity touches the -Y wall.
 /// </summary>
        public bool TouchesYMin { get; set; } = false;

 /// <summary>
 /// Gets or sets a value indicating whether the cavity touches the +Y wall.
 /// </summary>
        public bool TouchesYMax { get; set; } = false;

 /// <summary>
 /// Gets or sets a value indicating whether the cavity touches the -Z wall.
 /// </summary>
        public bool TouchesZMin { get; set; } = false;

 /// <summary>
 /// Gets or sets a value indicating whether the cavity touches the +Z wall.
 /// </summary>
        public bool TouchesZMax { get; set; } = false;

 /// <summary>
 /// Gets the total number of wall connections.
 /// </summary>
        public int WallConnections => WallXMin + WallXMax + WallYMin + WallYMax + WallZMin + WallZMax;

 /// <summary>
 /// Gets the toafterlogy type based on wall connections.
 /// </summary>
        public int TopologyType => WallConnections;

 /// <summary>
 /// Gets a value indicating whether the cavity is periodic.
 /// </summary>
        public bool IsPeriodic => WallConnections > 0;

        #endregion

        #region Crystal Channel Data

 /// <summary>
 /// Gets or sets the crystal channel analysis results.
 /// </summary>
        public Dictionary<string, CavityPathFinder.ChannelPathResult> CrystalChannelResults { get; set; }

 /// <summary>
 /// Gets or sets the crystal helicity data.
 /// </summary>
        public Dictionary<string, float> CrystalHelicityData { get; set; }

 /// <summary>
 /// Gets or sets the crystal channel paths.
 /// </summary>
        public Dictionary<string, List<Microsoft.Xna.Framework.Vector3>> CrystalChannelPaths { get; set; }

 /// <summary>
 /// Gets a value indicating whether crystal channels are available.
 /// </summary>
        public bool HasCrystalChannels => CrystalChannelResults != null && CrystalChannelResults.Count > 0;

 /// <summary>
 /// Gets the set of crystal channel directions.
 /// </summary>
        public HashSet<string> CrystalChannels
        {
            get
            {
                if (CrystalChannelResults == null)
                    return new HashSet<string>();
                return new HashSet<string>(CrystalChannelResults.Keys);
            }
        }

        #endregion

        #region Multi-Channel Data

 /// <summary>
 /// Gets or sets the multi-channel analysis results for each direction.
 /// </summary>
        public Dictionary<string, MultiChannelResult> MultiChannelResults { get; set; }

 /// <summary>
 /// Gets or sets all channel paths as a flat list.
 /// </summary>
        public List<ExtendedChannelPathResult> AllChannelPaths { get; set; }

 /// <summary>
 /// Gets or sets the best overall path (widest across all directions).
 /// </summary>
        public ExtendedChannelPathResult BestOverallPath { get; set; }

 /// <summary>
 /// Gets a value indicating whether multi-channel data is available.
 /// </summary>
        public bool HasMultiChannelData => MultiChannelResults != null && MultiChannelResults.Count > 0;

 /// <summary>
 /// Gets the total number of channel paths.
 /// </summary>
        public int TotalPathCount => AllChannelPaths?.Count ?? 0;

        #endregion

        #region Sphere Fit Data

 /// <summary>
 /// Gets or sets the maximum sphere radius that fits in the cavity.
 /// </summary>
        public float MaxSphereRadius { get; set; } = 0f;

 /// <summary>
 /// Gets or sets the center of the maximum inscribed sphere.
 /// </summary>
        public SysVector3 SphereCenter { get; set; } = SysVector3.Zero;

 /// <summary>
 /// Gets or sets the voxel index of the sphere center.
 /// </summary>
        public (int x, int y, int z) SphereVoxelIndex { get; set; } = (-1, -1, -1);

 /// <summary>
 /// Gets or sets a value indicating whether sphere fit has been calculated.
 /// </summary>
        public bool SphereFitCalculated { get; set; } = false;

 /// <summary>
 /// Gets or sets the list of multiple sphere fits.
 /// </summary>
        public List<SphereFitAnalyzer.SphereFitResult> SphereFits { get; set; }

 /// <summary>
 /// Gets a value indicating whether multiple sphere fits are available.
 /// </summary>
        public bool HasMultipleSpheres => SphereFits != null && SphereFits.Count > 0;

 /// <summary>
 /// Gets the number of sphere fits.
 /// </summary>
        public int SphereCount => SphereFits?.Count ?? 0;

 /// <summary>
 /// Gets the largest sphere from the multi-sphere analysis.
 /// </summary>
        public SphereFitAnalyzer.SphereFitResult LargestMultiSphere =>
            SphereFits?.OrderByDescending(s => s.MaxSphereRadius).FirstOrDefault();

        #endregion

        #region Periodicity and Connectivity

 /// <summary>
 /// Gets or sets the set of connected cavity IDs.
 /// </summary>
        public HashSet<int> ConnectedCavities { get; set; }

 /// <summary>
 /// Gets or sets the periodic group identifier.
 /// </summary>
        public int PeriodicGroupId { get; set; } = -1;

 /// <summary>
 /// Gets or sets a value indicating whether this is a periodic duplicate.
 /// </summary>
        public bool IsPeriodicDuplicate { get; set; } = false;

        #endregion

        #region Voxel Property Data

 /// <summary>
 /// Gets or sets the pre-computed charge values for each voxel.
 /// </summary>
        public float[] VoxelCharges { get; set; } = null;

 /// <summary>
 /// Gets or sets the pre-computed hydrophobicity values for each voxel.
 /// </summary>
        public float[] VoxelHydrophobicity { get; set; } = null;

 /// <summary>
 /// Gets or sets the pre-computed Z-effective values for each voxel.
 /// </summary>
        public float[] VoxelZEffective { get; set; } = null;

 /// <summary>
 /// Gets or sets the fractional X coordinates for each voxel.
 /// </summary>
        public float[] VoxelFracX { get; set; } = null;

 /// <summary>
 /// Gets or sets the fractional Y coordinates for each voxel.
 /// </summary>
        public float[] VoxelFracY { get; set; } = null;

 /// <summary>
 /// Gets or sets the fractional Z coordinates for each voxel.
 /// </summary>
        public float[] VoxelFracZ { get; set; } = null;

 /// <summary>
 /// Gets a value indicating whether voxel property data is available.
 /// </summary>
        public bool HasVoxelPropertyData => VoxelCharges != null && VoxelCharges.Length > 0;

        #endregion

        #region Public Methods - Molecule Access

 /// <summary>
 /// Gets the van der Waals radius for a given atomic number.
 /// </summary>
 /// <param name="atomicNumber">The atomic number.</param>
 /// <returns>The van der Waals radius in angstroms.</returns>
        public float GetVdwRadius(int atomicNumber)
        {
            if (_molecule != null)
                return _molecule.GetVdwRadius(atomicNumber);
            return VdwParameters.GetRadius(atomicNumber);
        }

        #endregion

        #region Public Methods - Voxel Property Access

 /// <summary>
 /// Gets the charge for a voxel at the specified index.
 /// </summary>
 /// <param name="index">The voxel index.</param>
 /// <returns>The charge value, or 0 if not available.</returns>
        public float GetVoxelCharge(int index)
        {
            if (VoxelCharges != null && index >= 0 && index < VoxelCharges.Length)
                return VoxelCharges[index];
            return 0f;
        }

 /// <summary>
 /// Gets the hydrophobicity for a voxel at the specified index.
 /// </summary>
 /// <param name="index">The voxel index.</param>
 /// <returns>The hydrophobicity value, or 0 if not available.</returns>
        public float GetVoxelHydrophobicity(int index)
        {
            if (VoxelHydrophobicity != null && index >= 0 && index < VoxelHydrophobicity.Length)
                return VoxelHydrophobicity[index];
            return 0f;
        }

 /// <summary>
 /// Gets the Z-effective for a voxel at the specified index.
 /// </summary>
 /// <param name="index">The voxel index.</param>
 /// <returns>The Z-effective value, or 0 if not available.</returns>
        public float GetVoxelZEffective(int index)
        {
            if (VoxelZEffective != null && index >= 0 && index < VoxelZEffective.Length)
                return VoxelZEffective[index];
            return 0f;
        }

 /// <summary>
 /// Gets the fractional coordinates for a voxel at the specified index.
 /// </summary>
 /// <param name="index">The voxel index.</param>
 /// <returns>A tuple containing the fractional X, Y, and Z coordinates.</returns>
        public (float fx, float fy, float fz) GetVoxelFrac(int index)
        {
            if (VoxelFracX != null && index >= 0 && index < VoxelFracX.Length)
                return (VoxelFracX[index], VoxelFracY[index], VoxelFracZ[index]);
            return (0f, 0f, 0f);
        }

        #endregion

        #region Public Methods - Crystal Channels

 /// <summary>
 /// Ensures crystal channel data is analyzed.
 /// </summary>
 /// <param name="probeRadius">The probe radius for channel detection.</param>
 /// <param name="forceRefresh">Whether to force re-analysis.</param>
        public void EnsureCrystalChannelDataAnalyzed(float probeRadius = 0.5f, bool forceRefresh = false)
        {
            System.Diagnostics.Debug.WriteLine($"[CAVITY] EnsureCrystalChannelDataAnalyzed START - Cavity {Id}");

            if (!forceRefresh && CrystalChannelResults != null && CrystalChannelResults.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[CAVITY] Cavity {Id}: Crystal channel data already exists ({CrystalChannelResults.Count} channels)");
                return;
            }

            if (DensityGrid == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CAVITY] Cavity {Id}: No density grid available! Run DetectVoidsAsync() first.");
                return;
            }

            if (PeriodicGrid == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CAVITY] Cavity {Id}: PeriodicGrid is NULL! Run DetectVoidsAsync() first.");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[CAVITY] Cavity {Id}: Analyzing crystal channels...");

            if (CrystalChannelResults == null)
                CrystalChannelResults = new Dictionary<string, CavityPathFinder.ChannelPathResult>();
            if (CrystalHelicityData == null)
                CrystalHelicityData = new Dictionary<string, float>();
            if (CrystalChannelPaths == null)
                CrystalChannelPaths = new Dictionary<string, List<Microsoft.Xna.Framework.Vector3>>();

            if (forceRefresh)
            {
                CrystalChannelResults.Clear();
                CrystalHelicityData.Clear();
                CrystalChannelPaths.Clear();
            }

            var pathFinder = new CavityPathFinder(this, probeRadius);

            string[] directions = { "XX", "YY", "ZZ" };
            int analyzed = 0;
            int connected = 0;

            foreach (var direction in directions)
            {
                System.Diagnostics.Debug.WriteLine($"[CAVITY] Analyzing crystal channel {direction} for cavity {Id}...");
                try
                {
                    var result = pathFinder.AnalyzeChannel(direction);
                    analyzed++;

                    if (result != null && result.IsConnected)
                    {
                        CrystalChannelResults[direction] = result;
                        CrystalHelicityData[direction] = result.Helicity;
                        CrystalChannelPaths[direction] = result.WorldPathXna;
                        connected++;

                        System.Diagnostics.Debug.WriteLine($"[CAVITY] Cavity {Id}, Crystal Channel {direction}: CONNECTED - " +
                            $"len={result.TotalLength:F2} Å, helicity={result.Helicity:F2}°");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[CAVITY] Cavity {Id}, Crystal Channel {direction}: NOT CONNECTED");
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CAVITY] Cavity {Id}, Error analyzing {direction}: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[CAVITY] Cavity {Id}: Crystal channel analysis complete. Analyzed={analyzed}, Connected={connected}, Stored={CrystalChannelResults.Count}");
        }

 /// <summary>
 /// Clears all crystal channel data.
 /// </summary>
        public void ClearCrystalChannelData()
        {
            CrystalChannelResults?.Clear();
            CrystalHelicityData?.Clear();
            CrystalChannelPaths?.Clear();
        }

 /// <summary>
 /// Gets a value indicating whether crystal channel data is available.
 /// </summary>
        public bool HasCrystalChannelData => CrystalChannelResults != null && CrystalChannelResults.Count > 0;

 /// <summary>
 /// Gets the number of connected crystal channels.
 /// </summary>
 /// <param name="probeRadius">The probe radius for channel detection.</param>
 /// <returns>The number of connected channels.</returns>
        public int GetConnectedCrystalChannelCount(float probeRadius = 0.5f)
        {
            if (CrystalChannelResults == null || CrystalChannelResults.Count == 0)
            {
                EnsureCrystalChannelDataAnalyzed(probeRadius);
            }
            return CrystalChannelResults?.Count ?? 0;
        }

 /// <summary>
 /// Gets a description of the crystal channels.
 /// </summary>
 /// <returns>A comma-separated string of channel directions.</returns>
        public string GetCrystalChannelDescription()
        {
            if (CrystalChannelResults == null || CrystalChannelResults.Count == 0)
                return "none";
            return string.Join(",", CrystalChannelResults.Keys);
        }

 /// <summary>
 /// Gets the color representing the crystal channel state.
 /// </summary>
 /// <returns>LightGreen if channels exist, otherwise Gray.</returns>
        public Microsoft.Xna.Framework.Color GetCrystalChannelColor()
        {
            if (HasCrystalChannels)
                return Microsoft.Xna.Framework.Color.LightGreen;
            return Microsoft.Xna.Framework.Color.Gray;
        }

 /// <summary>
 /// Gets a description of wall contacts.
 /// </summary>
 /// <returns>A string describing which walls are touched.</returns>
        public string GetWallDescription()
        {
            int connections = WallConnections;
            if (connections == 0)
                return "0";

            string walls = "";
            if (TouchesXMin) walls += "-x";
            if (TouchesXMax) walls += "+x";
            if (TouchesYMin) walls += "-y";
            if (TouchesYMax) walls += "+y";
            if (TouchesZMin) walls += "-z";
            if (TouchesZMax) walls += "+z";
            return walls;
        }

        #endregion

        #region Public Methods - Multi-Channel Analysis

 /// <summary>
 /// Gets the best path for a specific direction.
 /// </summary>
 /// <param name="direction">The direction (e.g., "XX", "YY", "ZZ").</param>
 /// <returns>The best path, or null if not available.</returns>
        public ExtendedChannelPathResult GetBestPathForDirection(string direction)
        {
            if (string.IsNullOrEmpty(direction) || !HasMultiChannelData)
                return null;

            if (MultiChannelResults.TryGetValue(direction, out var result))
            {
                return result.BestPath as ExtendedChannelPathResult;
            }
            return null;
        }

 /// <summary>
 /// Gets all paths for a specific direction.
 /// </summary>
 /// <param name="direction">The direction (e.g., "XX", "YY", "ZZ").</param>
 /// <returns>A list of paths, or an empty list if not available.</returns>
        public List<ExtendedChannelPathResult> GetPathsForDirection(string direction)
        {
            if (string.IsNullOrEmpty(direction) || !HasMultiChannelData)
                return new List<ExtendedChannelPathResult>();

            if (MultiChannelResults.TryGetValue(direction, out var result))
            {
                return result.Paths.OfType<ExtendedChannelPathResult>().ToList();
            }
            return new List<ExtendedChannelPathResult>();
        }

 /// <summary>
 /// Analyzes all channels using multi-start path analysis.
 /// </summary>
 /// <param name="probeRadius">The probe radius for channel detection.</param>
 /// <param name="maxPaths">The maximum number of paths to find per direction.</param>
 /// <param name="forceRefresh">Whether to force re-analysis.</param>
        public void AnalyzeAllMultiChannels(float probeRadius = 0.5f, int maxPaths = 5, bool forceRefresh = false)
        {
            if (!forceRefresh && HasMultiChannelData)
            {
                System.Diagnostics.Debug.WriteLine($"[CAVITY] Cavity {Id}: Multi-channel data already exists ({TotalPathCount} paths)");
                return;
            }

            if (DensityGrid == null || PeriodicGrid == null)
            {
                System.Diagnostics.Debug.WriteLine($"[CAVITY] Cavity {Id}: Cannot analyze multi-channels - missing density or periodic grid");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[CAVITY] Cavity {Id}: Analyzing multi-channels (maxPaths={maxPaths})...");

            MultiChannelResults.Clear();
            AllChannelPaths.Clear();
            BestOverallPath = null;

            var pathFinder = new CavityPathFinder(this, probeRadius);
            pathFinder.EnableDetailedLogging = false;
            pathFinder.UseSDFWeight = true;
            pathFinder.SDFWeight = 0.3f;
            pathFinder.SmoothingIterations = 2;
            pathFinder.MaxPathLength = 50000;
            pathFinder.MaxStartPoints = maxPaths * 4;

            string[] directions = { "XX", "YY", "ZZ" };

            foreach (var dir in directions)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[CAVITY]   Analyzing {dir}...");

                    var result = pathFinder.AnalyzeChannel(dir);

                    if (result != null && result.IsConnected)
                    {
                        var multiResult = new MultiChannelResult
                        {
                            Direction = dir,
                            Paths = new List<CavityPathFinder.ChannelPathResult>()
                        };

                        if (result is ExtendedChannelPathResult extPath)
                        {
                            multiResult.Paths.Add(extPath);
                            AllChannelPaths.Add(extPath);
                        }
                        else
                        {
                            multiResult.Paths.Add(result);
                        }

                        MultiChannelResults[dir] = multiResult;

                        System.Diagnostics.Debug.WriteLine($"[CAVITY]   {dir}: CONNECTED - len={result.TotalLength:F2} A, diam={result.AverageWidth:F2} A");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[CAVITY]   {dir}: NOT CONNECTED");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CAVITY]   {dir}: ERROR - {ex.Message}");
                }
            }

            BestOverallPath = AllChannelPaths
                .OrderByDescending(p => p?.AverageWidth ?? 0)
                .FirstOrDefault();

            System.Diagnostics.Debug.WriteLine($"[CAVITY] Cavity {Id}: Multi-channel analysis complete - {TotalPathCount} total paths, best diam={BestOverallPath?.AverageWidth:F2 ?? 0} A");
        }

 /// <summary>
 /// Clears all multi-channel data.
 /// </summary>
        public void ClearMultiChannelData()
        {
            MultiChannelResults?.Clear();
            AllChannelPaths?.Clear();
            BestOverallPath = null;
        }

        #endregion

        #region Public Methods - Sphere Fit

 /// <summary>
 /// Gets the effective sphere radius after subtracting the probe radius.
 /// </summary>
 /// <param name="probeRadius">The probe radius to subtract.</param>
 /// <returns>The effective radius, or 0 if not calculated.</returns>
        public float GetEffectiveSphereRadius(float probeRadius = 0.5f)
        {
            if (!SphereFitCalculated || MaxSphereRadius <= 0)
                return 0f;
            return Math.Max(MaxSphereRadius - probeRadius, 0.1f);
        }

 /// <summary>
 /// Gets the sphere diameter.
 /// </summary>
 /// <returns>The diameter of the maximum inscribed sphere.</returns>
        public float GetSphereDiameter()
        {
            return MaxSphereRadius * 2f;
        }

 /// <summary>
 /// Gets the effective sphere diameter after subtracting the probe radius.
 /// </summary>
 /// <param name="probeRadius">The probe radius to subtract.</param>
 /// <returns>The effective diameter.</returns>
        public float GetEffectiveSphereDiameter(float probeRadius = 0.5f)
        {
            return GetEffectiveSphereRadius(probeRadius) * 2f;
        }

 /// <summary>
 /// Determines whether a sphere of the given radius can fit in the cavity.
 /// </summary>
 /// <param name="radius">The sphere radius.</param>
 /// <param name="probeRadius">The probe radius to subtract.</param>
 /// <returns>True if the sphere can fit, otherwise false.</returns>
        public bool CanFitSphere(float radius, float probeRadius = 0.5f)
        {
            return GetEffectiveSphereRadius(probeRadius) >= radius;
        }

        #endregion

        #region Public Methods - Color Helpers

 /// <summary>
 /// Sets all vertex colors to the specified color.
 /// </summary>
 /// <param name="color">The color to apply.</param>
        public void SetAllVertexColors(XnaVector4 color)
        {
            if (Vertices == null || Vertices.Count == 0)
                return;

            VertexColors4 = new XnaVector4[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
                VertexColors4[i] = color;
        }

 /// <summary>
 /// Resets all vertex colors to the base color.
 /// </summary>
        public void ResetVertexColorsToBase()
        {
            SetAllVertexColors(BaseColor4);
        }

 /// <summary>
 /// Checks if all vertices have the same color.
 /// </summary>
 /// <returns>True if all colors are uniform, otherwise false.</returns>
        public bool HasUniformColor()
        {
            if (VertexColors4 == null || VertexColors4.Length == 0)
                return true;

            XnaVector4 first = VertexColors4[0];
            for (int i = 1; i < VertexColors4.Length; i++)
            {
                if (Math.Abs(VertexColors4[i].X - first.X) > 0.001f ||
                    Math.Abs(VertexColors4[i].Y - first.Y) > 0.001f ||
                    Math.Abs(VertexColors4[i].Z - first.Z) > 0.001f)
                    return false;
            }
            return true;
        }

 /// <summary>
 /// Gets the average color of all vertices.
 /// </summary>
 /// <returns>The average color, or the base color if no vertices.</returns>
        public XnaVector4 GetAverageColor()
        {
            if (VertexColors4 == null || VertexColors4.Length == 0)
                return BaseColor4;

            XnaVector4 sum = XnaVector4.Zero;
            for (int i = 0; i < VertexColors4.Length; i++)
                sum += VertexColors4[i];

            return sum / VertexColors4.Length;
        }

        #endregion

        #region Public Methods - Resource Management

 /// <summary>
 /// Clears all data and releases resources.
 /// </summary>
        public void Clear()
        {
 // ⭐ Mesh data
            RawVertices?.Clear();
            RawIndices?.Clear();
            RawTrianglesGenerated = false;

            Vertices?.Clear();
            Indices?.Clear();
            TrianglesGenerated = false;

            VertexColors4 = null;

 // ⭐ Voxels i geometria
            Voxels?.Clear();
            Center = SysVector3.Zero;
            Volume = 0;
            SurfaceArea = 0;
            Sphericity = 0;
            IsExterior = false;

 // ⭐ Bounding boxy
            BoundingBox = new BoundingBox();
            TrueCellBox = new BoundingBox();

 // ⭐ Grid i density
            DensityGrid = null;
            DensityNx = 0;
            DensityNy = 0;
            DensityNz = 0;
            Grid = null;
            VoxelMask = null;
            MinCorner = SysVector3.Zero;
            InvFracToCart = Matrix.Identity;

 // ⭐ Periodic
            PeriodicGrid = null;
            OffsetX = 0;
            OffsetY = 0;
            OffsetZ = 0;

 // ⭐ Gradienty
            SupercellGradientX = null;
            SupercellGradientY = null;
            SupercellGradientZ = null;

 // ⭐ Wall flags
            WallXMin = 0;
            WallXMax = 0;
            WallYMin = 0;
            WallYMax = 0;
            WallZMin = 0;
            WallZMax = 0;

            TouchesXMin = false;
            TouchesXMax = false;
            TouchesYMin = false;
            TouchesYMax = false;
            TouchesZMin = false;
            TouchesZMax = false;

 // ⭐ Crystal channels
            CrystalChannelResults?.Clear();
            CrystalHelicityData?.Clear();
            CrystalChannelPaths?.Clear();

 // ⭐ Multi-channel
            MultiChannelResults?.Clear();
            AllChannelPaths?.Clear();
            BestOverallPath = null;

 // ⭐ Periodicity
            ConnectedCavities?.Clear();
            PeriodicGroupId = -1;
            IsPeriodicDuplicate = false;

 // ⭐ Sphere fits
            MaxSphereRadius = 0f;
            SphereCenter = SysVector3.Zero;
            SphereVoxelIndex = (-1, -1, -1);
            SphereFitCalculated = false;

            SphereFits?.Clear();

 // ⭐ Voxel properties
            VoxelCharges = null;
            VoxelHydrophobicity = null;
            VoxelZEffective = null;
            VoxelFracX = null;
            VoxelFracY = null;
            VoxelFracZ = null;

 // ⭐ Mapper state
            LastMappedProperty = CavityPropertyMapper.PropertyType.None;

 // ⭐ Molecule reference
            _molecule = null;
        }

        #endregion

        #region IDisposable Implementation

 /// <summary>
 /// Disafterses the cavity and releases all resources.
 /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

 /// <summary>
 /// Protected disafterse implementation.
 /// </summary>
 /// <param name="disaftersing">Whether to disafterse managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Clear();
            }

            _disposed = true;
        }

        #endregion

        #region Public Methods - Clone

 /// <summary>
 /// Creates a deep copy of the cavity.
 /// </summary>
 /// <returns>A new Cavity instance with copied data.</returns>
        public Cavity Clone()
        {
            return new Cavity
            {
                Id = Id,
                RawTrianglesGenerated = RawTrianglesGenerated,
                TrianglesGenerated = TrianglesGenerated,
                Volume = Volume,
                SurfaceArea = SurfaceArea,
                Sphericity = Sphericity,
                IsExterior = IsExterior,
                Center = Center,
                BoundingBox = BoundingBox,
                TrueCellBox = TrueCellBox,
                DensityNx = DensityNx,
                DensityNy = DensityNy,
                DensityNz = DensityNz,
                InvFracToCart = InvFracToCart,
                MinCorner = MinCorner,
                OffsetX = OffsetX,
                OffsetY = OffsetY,
                OffsetZ = OffsetZ,
                WallXMin = WallXMin,
                WallXMax = WallXMax,
                WallYMin = WallYMin,
                WallYMax = WallYMax,
                WallZMin = WallZMin,
                WallZMax = WallZMax,
                TouchesXMin = TouchesXMin,
                TouchesXMax = TouchesXMax,
                TouchesYMin = TouchesYMin,
                TouchesYMax = TouchesYMax,
                TouchesZMin = TouchesZMin,
                TouchesZMax = TouchesZMax,
                PeriodicGroupId = PeriodicGroupId,
                IsPeriodicDuplicate = IsPeriodicDuplicate,
                MaxSphereRadius = MaxSphereRadius,
                SphereCenter = SphereCenter,
                SphereVoxelIndex = SphereVoxelIndex,
                SphereFitCalculated = SphereFitCalculated,
                BaseColor4 = BaseColor4,

                RawVertices = RawVertices != null ? new List<SysVector3>(RawVertices) : new List<SysVector3>(),
                RawIndices = RawIndices != null ? new List<int>(RawIndices) : new List<int>(),
                Vertices = Vertices != null ? new List<SysVector3>(Vertices) : new List<SysVector3>(),
                Indices = Indices != null ? new List<int>(Indices) : new List<int>(),
                Voxels = Voxels != null ? new List<SysVector3>(Voxels) : new List<SysVector3>(),

                VertexColors4 = VertexColors4,

                DensityGrid = DensityGrid,
                VoxelMask = VoxelMask,

                SupercellGradientX = SupercellGradientX,
                SupercellGradientY = SupercellGradientY,
                SupercellGradientZ = SupercellGradientZ,

                Grid = Grid,
                PeriodicGrid = PeriodicGrid,

                CrystalChannelResults = CrystalChannelResults != null
                    ? new Dictionary<string, CavityPathFinder.ChannelPathResult>(CrystalChannelResults)
                    : new Dictionary<string, CavityPathFinder.ChannelPathResult>(),
                CrystalHelicityData = CrystalHelicityData != null
                    ? new Dictionary<string, float>(CrystalHelicityData)
                    : new Dictionary<string, float>(),
                CrystalChannelPaths = CrystalChannelPaths != null
                    ? new Dictionary<string, List<Microsoft.Xna.Framework.Vector3>>(CrystalChannelPaths)
                    : new Dictionary<string, List<Microsoft.Xna.Framework.Vector3>>(),

                MultiChannelResults = MultiChannelResults != null
                    ? new Dictionary<string, MultiChannelResult>(MultiChannelResults)
                    : new Dictionary<string, MultiChannelResult>(),
                AllChannelPaths = AllChannelPaths != null
                    ? new List<ExtendedChannelPathResult>(AllChannelPaths)
                    : new List<ExtendedChannelPathResult>(),
                BestOverallPath = BestOverallPath,

                ConnectedCavities = ConnectedCavities != null
                    ? new HashSet<int>(ConnectedCavities)
                    : new HashSet<int>(),

                SphereFits = SphereFits != null
                    ? new List<SphereFitAnalyzer.SphereFitResult>(SphereFits)
                    : new List<SphereFitAnalyzer.SphereFitResult>(),

                VoxelCharges = VoxelCharges != null ? (float[])VoxelCharges.Clone() : null,
                VoxelHydrophobicity = VoxelHydrophobicity != null ? (float[])VoxelHydrophobicity.Clone() : null,
                VoxelZEffective = VoxelZEffective != null ? (float[])VoxelZEffective.Clone() : null,
                VoxelFracX = VoxelFracX != null ? (float[])VoxelFracX.Clone() : null,
                VoxelFracY = VoxelFracY != null ? (float[])VoxelFracY.Clone() : null,
                VoxelFracZ = VoxelFracZ != null ? (float[])VoxelFracZ.Clone() : null
            };
        }

        #endregion

        #region Public Methods - ToString

 /// <summary>
 /// Returns a string representation of the cavity.
 /// </summary>
 /// <returns>A formatted string with cavity properties.</returns>
        public override string ToString()
        {
            string channelDesc = GetCrystalChannelDescription();
            if (channelDesc.Length > 20)
                channelDesc = channelDesc.Substring(0, 20) + "...";

            string multiPathInfo = HasMultiChannelData ? $", {TotalPathCount} paths" : "";

            return $"Cavity #{Id}: V={Volume:F2} Å³, S={SurfaceArea:F2} Å², Sphericity={Sphericity:F3}, " +
                   $"Channels={channelDesc}, Walls={WallConnections}{multiPathInfo}";
        }

        #endregion

        #region Static Factory Methods

 /// <summary>
 /// Creates a basic cavity with minimal data.
 /// </summary>
 /// <param name="id">The cavity identifier.</param>
 /// <param name="volume">The cavity volume.</param>
 /// <param name="center">The cavity center aftersition.</param>
 /// <param name="color">The base color for rendering.</param>
 /// <returns>A new Cavity instance.</returns>
        public static Cavity CreateBasic(int id, float volume, SysVector3 center, XnaVector4? color = null)
        {
            XnaVector4 baseColor = color ?? XnaVector4.One;

            return new Cavity
            {
                Id = id,
                Volume = volume,
                Center = center,
                BaseColor4 = baseColor,
                Vertices = new List<SysVector3>(),
                Indices = new List<int>(),
                Voxels = new List<SysVector3>()
            };
        }

 /// <summary>
 /// Creates a basic cavity with a Microsoft.Xna.Framework.Color.
 /// </summary>
 /// <param name="id">The cavity identifier.</param>
 /// <param name="volume">The cavity volume.</param>
 /// <param name="center">The cavity center aftersition.</param>
 /// <param name="color">The base color for rendering.</param>
 /// <returns>A new Cavity instance.</returns>
        public static Cavity CreateBasic(int id, float volume, SysVector3 center, Microsoft.Xna.Framework.Color color)
        {
            return CreateBasic(id, volume, center, new XnaVector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));
        }

        #endregion
    }
}