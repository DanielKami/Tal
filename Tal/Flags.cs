// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Global application flags / settings singleton.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// Last update: 2026-02
//
// ═══════════════════════════════════════════════════════════════════════════
// Flags — TAL application configuration singleton
// ═══════════════════════════════════════════════════════════════════════════
//
// Stores all user preferences, display settings and runtime flags.
// Persisted to a JSON file (tal_settings.json).
// System limits (MAX_VOXELS, etc.) are STATIC readonly — NOT serialized.
//
// Sections:
// [1] System Limits (static, not serialized)
// [2] Singleton Implementation
// [3] Static Constructor — system limit detection
// [4] Win32 Interop
// [5] Static Helpers (voxel safety, memory formatting)
// [6] Runtime — Visual Revision (for Progressive AA reset)
// [7] Runtime — Render State (not serialized)
// [8] Rendering Quality / Anti-Aliasing
// [9] Atom Color Overrides
// [10] Background Colors
// [11] Lighting Parameters (primary + secondary)
// [12] Application Settings
// [13] Bond and Atom Settings
// [14] Balls & Sticks / Style
// [15] Atom Display / Ellipsoid
// [16] Coloring & Labels
// [17] Drawing / View / Cell
// [18] Display Options
// [19] Unit Cell Line Settings
// [20] Cell Repetition Settings
// [21] Expand Repetition Settings
// [22] Z-Clipping
// [23] Depth Cue
// [24] Selection Settings
// [25] Hydrogen Bond Settings
// [26] Short Contacts Settings
// [27] Expand Contacts Settings
// [28] SDF / Cavities
// [29] Interaction / Measurements
// [30] Interaction Mode (secondary window)
// [31] Legacy Settings
// [32] Hirshfeld d_norm color mapping
// [33] File
// [34] Symmetry Elements Display
// [35] Serialization
// [36] CopyFrom (JSON load merge)
// [37] Reset methods
//
// ═══════════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Tal
{
 /// <summary>
 /// Singleton configuration manager for the TAL application.
 /// </summary>
    public sealed class Flags
    {
        public static string VERSION = "3.70";
        public static string Friend = "Symmetry replication · directional packing · cavity visibility";

 // ─────────────────────────────────────────────────────────────────
 // [1] SYSTEM LIMITS (STATIC — NOT SERIALIZED)
 // ─────────────────────────────────────────────────────────────────

        #region System Limits (static, not serialized)

 /// <summary>True if process is 64-bit.</summary>
        public static readonly bool Is64Bit = Environment.Is64BitProcess;

 /// <summary>pointer size in bytes (4 for 32-bit, 8 for 64-bit).</summary>
        public static readonly int PointerSize = IntPtr.Size;

 /// <summary>Detected physical memory in GB.</summary>
        public static readonly double PhysicalMemoryGB;

 /// <summary>Maximum number of voxels in the detection grid.</summary>
        public static readonly long MAX_VOXELS;

 /// <summary>Maximum number of voxels for SDF operations.</summary>
        public static readonly long MAX_SDF_VOXELS;

 /// <summary>Maximum number of voxels for Marching Cubes.</summary>
        public static readonly long MAX_MC_VOXELS;

 /// <summary>Warning threshold for large grids.</summary>
        public static readonly long WARNING_VOXELS;

 /// <summary>Maximum number of mesh vertices.</summary>
        public static readonly int MAX_VERTICES;

 /// <summary>Maximum number of mesh triangles.</summary>
        public static readonly int MAX_TRIANGLES;

 /// <summary>Maximum single grid dimension.</summary>
        public static readonly int MAX_GRID_DIMENSION;

 /// <summary>Maximum array length (.NET hard limit).</summary>
        public static readonly int MAX_ARRAY_LENGTH = 0x7FEFFFFF;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [2] SINGLETON IMPLEMENTATION
 // ─────────────────────────────────────────────────────────────────

        #region Singleton Implementation

        private static Flags _instance = null;
        private static readonly object _lock = new object();
        private static readonly string CONFIG_FILE =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tal_settings.json");
        private bool _isLoading = false;
        private bool _isDirty = false;

 /// <summary>Public ctor — required for JSON deserialization.</summary>
        public Flags() { }

 /// <summary>Singleton instance.</summary>
        public static Flags Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new Flags();
                            _instance.Load();
                        }
                    }
                }
                return _instance;
            }
        }

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [3] STATIC CONSTRUCTOR — SYSTEM LIMIT DETECTION
 // ─────────────────────────────────────────────────────────────────

        #region Static Constructor

        static Flags()
        {
            try
            {
                PhysicalMemoryGB = DetectPhysicalMemoryGB();
            }
            catch
            {
                PhysicalMemoryGB = 8.0;
            }

            if (Is64Bit)
            {
                double memoryFactor = Math.Clamp(PhysicalMemoryGB / 16.0, 0.5, 8.0);
                long baseVoxels = (long)(200_000_000 * memoryFactor);
                const long HARD_LIMIT_VOXELS = 800_000_000L;

                MAX_VOXELS = Math.Min(baseVoxels, HARD_LIMIT_VOXELS);
                MAX_SDF_VOXELS = (long)(MAX_VOXELS * 0.75);
                MAX_MC_VOXELS = (long)(MAX_VOXELS * 0.50);
                WARNING_VOXELS = (long)(MAX_VOXELS * 0.50);

                MAX_VERTICES = 200_000_000;
                MAX_TRIANGLES = 100_000_000;
                MAX_GRID_DIMENSION = 100_000;
            }
            else
            {
                MAX_VOXELS = 50_000_000;
                MAX_SDF_VOXELS = 40_000_000;
                MAX_MC_VOXELS = 30_000_000;
                WARNING_VOXELS = 25_000_000;

                MAX_VERTICES = 10_000_000;
                MAX_TRIANGLES = 3_000_000;
                MAX_GRID_DIMENSION = 32_000;
            }

            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════");
            System.Diagnostics.Debug.WriteLine($"[Flags] SYSTEM LIMITS INITIALIZED");
            System.Diagnostics.Debug.WriteLine($"  Architecture:    {(Is64Bit ? "64-bit" : "32-bit")}");
            System.Diagnostics.Debug.WriteLine($"  Pointer size:    {PointerSize} bytes");
            System.Diagnostics.Debug.WriteLine($"  Physical RAM:    {PhysicalMemoryGB:F1} GB");
            System.Diagnostics.Debug.WriteLine($"  MAX_VOXELS:      {MAX_VOXELS:N0}");
            System.Diagnostics.Debug.WriteLine($"  MAX_SDF_VOXELS:  {MAX_SDF_VOXELS:N0}");
            System.Diagnostics.Debug.WriteLine($"  MAX_MC_VOXELS:   {MAX_MC_VOXELS:N0}");
            System.Diagnostics.Debug.WriteLine($"  WARNING_VOXELS:  {WARNING_VOXELS:N0}");
            System.Diagnostics.Debug.WriteLine($"  MAX_VERTICES:    {MAX_VERTICES:N0}");
            System.Diagnostics.Debug.WriteLine($"  MAX_TRIANGLES:   {MAX_TRIANGLES:N0}");
            System.Diagnostics.Debug.WriteLine("═══════════════════════════════════════════════");
        }

        private static double DetectPhysicalMemoryGB()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var memStatus = new MEMORYSTATUSEX();
                    if (GlobalMemoryStatusEx(memStatus))
                        return memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                }
            }
            catch { }

            var gcInfo = GC.GetGCMemoryInfo();
            return gcInfo.TotalAvailableMemoryBytes / (1024.0 * 1024.0 * 1024.0);
        }

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [4] WIN32 INTEROP
 // ─────────────────────────────────────────────────────────────────

        #region Win32 Interop

        [StructLayout(LayoutKind.Sequential)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailableExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [5] STATIC HELPERS
 // ─────────────────────────────────────────────────────────────────

        #region Static Helpers

        public enum VoxelOperation { General, SDF, MarchingCubes }

 /// <summary>Checks if a voxel count is safe for a given operation.</summary>
        public static bool IsVoxelCountSafe(long voxelCount, VoxelOperation operation = VoxelOperation.General)
        {
            long limit = operation switch
            {
                VoxelOperation.SDF => MAX_SDF_VOXELS,
                VoxelOperation.MarchingCubes => MAX_MC_VOXELS,
                _ => MAX_VOXELS
            };
            return voxelCount <= limit;
        }

 /// <summary>True if voxel count should trigger a warning.</summary>
        public static bool ShouldWarnAboutVoxelCount(long voxelCount)
            => voxelCount > WARNING_VOXELS;

 /// <summary>Estimated memory usage for a voxel grid.</summary>
        public static long EstimateMemoryUsage(long voxelCount, int bytesPerVoxel = 8)
            => voxelCount * bytesPerVoxel;

 /// <summary>Formats a byte size to human-readable text.</summary>
        public static string FormatMemorySize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024L * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [6] RUNTIME — VISUAL REVISION
 // ─────────────────────────────────────────────────────────────────

        #region Runtime — Visual Revision

 /// <summary>
 /// Incremented on every change affecting 3D scene appearance.
 /// Progressive AA compares this to reset accumulation.
 /// </summary>
        public int VisualRevision { get; set; } = 0;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [7] RUNTIME — RENDER STATE (NOT SERIALIZED)
 // ─────────────────────────────────────────────────────────────────

        #region Runtime Render State (not serialized)

 /// <summary>
 /// Read-only in Draw.cs. Ratio of the current RenderTarget size to the
 /// window backbuffer size.
 /// </summary>
        [JsonIgnore]
        public float RenderTargetScale { get; set; } = 1.0f;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [8] RENDERING QUALITY / ANTI-ALIASING
 // ─────────────────────────────────────────────────────────────────

        #region Rendering Quality / Anti-Aliasing

 /// <summary>Scene supersampling (SSAA). 1 = off, 2 = 2×, 3 = 3×, 4 = 4×.</summary>
        public int SSAASamples { get; set; } = 2;

 /// <summary>MSAA for the 3D scene RenderTarget (not backbuffer). 0/2/4/8.</summary>
        public int MSAAOnSceneRT { get; set; } = 4;

 /// <summary>Progressive AA — accumulate N frames when idle. 1/2/4/8/16.</summary>
        public int ProgressiveAASamples { get; set; } = 4;

 /// <summary>Backbuffer MSAA: 0/2/4/8. Requires window restart.</summary>
        public int MSAAOnBackbuffer { get; set; } = 8;

 /// <summary>Texture anisotropy level (1/2/4/8/16).</summary>
        public int AnisotropyLevel { get; set; } = 4;

 /// <summary>Render scale for 3D scene (0.5 = downscale, 1 = native, &gt;1 = SSAA).</summary>
        public float RenderScale { get; set; } = 1.0f;

 /// <summary>Sharpen strength after downsample (0..1).</summary>
        public float SharpenStrength { get; set; } = 0.0f;

 /// <summary>Generic smoothing parameter (legacy).</summary>
        public int smuth { get; set; } = 3;

 /// <summary>Transparency level (legacy, 0..100).</summary>
        public int transparency { get; set; } = 10;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [9] ATOM COLOR OVERRIDES
 // ─────────────────────────────────────────────────────────────────

        #region Atom Color Overrides

 /// <summary>
 /// Per-element color overrides. Key = atomic number, value = [R, G, B].
 /// Only entries that differ from VdwParameters.DefaultColor are stored.
 /// Applied on Load() and after every change in AtomPropertiesWindow.
 /// </summary>
        [JsonInclude]
        public Dictionary<int, float[]> AtomColorOverrides { get; set; }
            = new Dictionary<int, float[]>();

 /// <summary>
 /// Applies overrides on top of VdwParameters.color.
 /// First resets VdwParameters.color to defaults, then applies each entry.
 /// </summary>
        public void ApplyAtomColorOverrides()
        {
            VdwParameters.ResetAllColors();

            foreach (var kvp in AtomColorOverrides)
            {
                int z = kvp.Key;
                var v = kvp.Value;

                if (z >= 0 && z < VdwParameters.color.Length && v != null && v.Length >= 3)
                {
                    VdwParameters.color[z] = new Vector3(v[0], v[1], v[2]);
                }
            }

            System.Diagnostics.Debug.WriteLine(
                $"[Flags] Applied {AtomColorOverrides.Count} atom color overrides");
        }

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [10] BACKGROUND COLORS
 // ─────────────────────────────────────────────────────────────────

        #region Background Colors

        private float _color1R = 1.0f;
        private float _color1G = 0.95f;
        private float _color1B = 0.95f;
        private float _color1A = 1.0f;

        private float _color2R = 1.0f;
        private float _color2G = 1.0f;
        private float _color2B = 1.0f;
        private float _color2A = 1.0f;

        [JsonInclude] public float Color1R { get => _color1R; set => _color1R = value; }
        [JsonInclude] public float Color1G { get => _color1G; set => _color1G = value; }
        [JsonInclude] public float Color1B { get => _color1B; set => _color1B = value; }
        [JsonInclude] public float Color1A { get => _color1A; set => _color1A = value; }

        [JsonInclude] public float Color2R { get => _color2R; set => _color2R = value; }
        [JsonInclude] public float Color2G { get => _color2G; set => _color2G = value; }
        [JsonInclude] public float Color2B { get => _color2B; set => _color2B = value; }
        [JsonInclude] public float Color2A { get => _color2A; set => _color2A = value; }

        [JsonIgnore]
        public XnaColor color1
        {
            get => new XnaColor(_color1R, _color1G, _color1B, _color1A);
            set
            {
                _color1R = value.R / 255f;
                _color1G = value.G / 255f;
                _color1B = value.B / 255f;
                _color1A = value.A / 255f;
            }
        }

        [JsonIgnore]
        public XnaColor color2
        {
            get => new XnaColor(_color2R, _color2G, _color2B, _color2A);
            set
            {
                _color2R = value.R / 255f;
                _color2G = value.G / 255f;
                _color2B = value.B / 255f;
                _color2A = value.A / 255f;
            }
        }

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [11] LIGHTING PARAMETERS
 // ─────────────────────────────────────────────────────────────────

        #region Lighting Parameters

        public bool Enabled { get; set; } = true;
        public bool PerPixelLighting { get; set; } = true;
        public float DirectionX { get; set; } = 0.5f;
        public float DirectionY { get; set; } = 0.5f;
        public float DirectionZ { get; set; } = -1.0f;
        public float SpecularPower { get; set; } = 10f;
        public float SpecularIntensity { get; set; } = 0.9f;
        public float DiffuseIntensity { get; set; } = 0.9f;
        public float AmbientIntensity { get; set; } = 0.05f;

        public bool Light2Enabled { get; set; } = true;
        public float Light2DirectionX { get; set; } = -0.5f;
        public float Light2DirectionY { get; set; } = -0.5f;
        public float Light2DirectionZ { get; set; } = -0.35f;
        public float Light2DiffuseIntensity { get; set; } = 0.6f;
        public float Light2AmbientIntensity { get; set; } = 0.05f;
        public float Light2SpecularIntensity { get; set; } = 0.5f;

        #endregion

        #region Computed Lighting Properties (Read-Only)

        public Vector3 SpecularColor => new Vector3(SpecularIntensity, SpecularIntensity, SpecularIntensity);
        public Vector3 DiffuseColor => new Vector3(DiffuseIntensity, DiffuseIntensity, DiffuseIntensity);
        public Vector3 AmbientColor => new Vector3(AmbientIntensity, AmbientIntensity, AmbientIntensity);
        public Vector3 LightDirection => new Vector3(DirectionX, DirectionY, DirectionZ);

        public Vector3 Light2Direction => new Vector3(Light2DirectionX, Light2DirectionY, Light2DirectionZ);
        public Vector3 Light2DiffuseColor => new Vector3(Light2DiffuseIntensity, Light2DiffuseIntensity, Light2DiffuseIntensity);
        public Vector3 Light2AmbientColor => new Vector3(Light2AmbientIntensity, Light2AmbientIntensity, Light2AmbientIntensity);
        public Vector3 Light2SpecularColor => new Vector3(Light2SpecularIntensity, Light2SpecularIntensity, Light2SpecularIntensity);

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [12] APPLICATION SETTINGS
 // ─────────────────────────────────────────────────────────────────

        #region Application Settings

        public string version { get; set; } = "1.0";
        public bool flag_start { get; set; } = true;
        public float intro_duration { get; set; } = 3;
        public float remainingDelay { get; set; } = 3;
        public int GenerateEquvalents { get; set; } = 0;
        public int show_properity { get; set; } = 0;
        public int Presentation { get; set; } = 0;
        public bool ATP_ortep { get; set; } = true;
        public int ScreenPresentation { get; set; } = 0;
        public int ColourPresentation { get; set; } = 0;
        public int AtomSelected { get; set; } = 0;
        public int PickingMode { get; set; } = 0;
        public bool CleanMeasurements { get; set; } = false;
        public bool RevealSymmetry { get; set; } = false;
        public int ShowLabelsFor { get; set; } = 0;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [13] BOND AND ATOM SETTINGS
 // ─────────────────────────────────────────────────────────────────

        #region Bond and Atom Settings

        public float MAX_ATOM_DISTANCE { get; set; } = 3.0f;
        public float MAX_H_ATOM_DISTANCE { get; set; } = 3.1f;
        public int MAX_BONDED_ATOMS { get; set; } = 10;
        public int MAX_H_BONDED_ATOMS { get; set; } = 5;
        public float BOND_FACTOR { get; set; } = 0.58f;
        public float H_acceptance_angle { get; set; } = 63.0f;
        public float BOND_FACTOR_METALS { get; set; } = 0.79f;

 /// <summary>Hard cutoff for covalent H–X bond (Å).</summary>
        public float H_COVALENT_MAX_DISTANCE { get; set; } = 1.3f;

        public bool MoleculeUseBondFactorFromBonds { get; set; } = true;
        public float MOLECULE_BOND_FACTOR { get; set; } = 0.65f;
        public float MOLECULE_POSITION_TOLERANCE { get; set; } = 0.01f;
        public int MOLECULE_MAX_ATOMS { get; set; } = 10000;

        #endregion

        #region ATP / Polyhedron

        public int ATP_ProbabilityPercent { get; set; } = 50;
        public float ATP_Probability { get; set; } = 1.538f;
        public int AtomicRadiiType { get; set; } = 0;
        public float Poli_dyst { get; set; } = 2.9f;
        public float PolyhedronAlpha = 0.25f;

        #endregion

        public int view { get; set; } = 0;

 // ─────────────────────────────────────────────────────────────────
 // [14] BALLS & STICKS / STYLE
 // ─────────────────────────────────────────────────────────────────

        #region Balls and Sticks

        public float BallRadiusNonMetal { get; set; } = 0.08f;
        public float BallRadiusMetal { get; set; } = 0.12f;
        public float StickRadius { get; set; } = 0.03f;
        public float StickRadiusFactor { get; set; } = 0.0127f;

        #endregion

        #region Style Settings

        public float sticsSize { get; set; } = 0.1f;
        public float ballSize { get; set; } = 0.3f;
        public int AtomStyle { get; set; } = 2;
        public bool ShowHydrogenBonds { get; set; } = false;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [15] ATOM DISPLAY / ELLIPSOID
 // ─────────────────────────────────────────────────────────────────

        #region Atom Display / Ellipsoid

        public float HydrogenBallScale { get; set; } = 0.7f;

        public float EllipsoidDiskThickness { get; set; } = 0.05f;
        public float EllipsoidBaseRadius { get; set; } = 1.05f;
        public float EllipsoidProbability { get; set; } = 0.5f;
        public bool EllipsoidDisksEnabled { get; set; } = true;
        public bool ShowHydrogenIsoSpheres { get; set; } = true;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [16] COLORING & LABELS
 // ─────────────────────────────────────────────────────────────────

        #region Coloring and Labels

        public int ColorMode { get; set; } = 0;
        public Vector3 IndividualColor { get; set; } = new Vector3(1f, 0f, 0f);
        public int LabelProperty { get; set; } = 0;

        #endregion

        #region Label Settings

        public XnaColor LabelColor { get; set; } = XnaColor.Black;
        public XnaColor LabelShadowColor { get; set; } = XnaColor.White;
        public float LabelScale { get; set; } = 0.5f;
        public bool LabelShadow { get; set; } = true;
        public float LabelOffsetY { get; set; } = -0.04f;
        public float LabelOffsetX { get; set; } = 0f;
        public string LabelFontName { get; set; } = "Segoe UI";
        public float LabelFontSize { get; set; } = 12f;
        public bool LabelFontBold { get; set; } = false;
        public bool LabelFontItalic { get; set; } = false;
        public bool LabelFontUnderline { get; set; } = false;
        public bool ShowAxisLabels { get; set; } = true;
        /// <summary>Font scale for crystallographic a/b/c axis labels (relative, ~0.3–2.0).</summary>
        public float AxisLabelScale { get; set; } = 1.0f;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [17] DRAWING / VIEW / CELL
 // ─────────────────────────────────────────────────────────────────

        #region Drawing / View Settings

        public bool Draw_cell { get; set; } = true;
        public bool DrawStereo { get; set; } = false;
        public float Shift { get; set; } = 0.5f;
        public bool DrawCharges { get; set; } = false;
        public bool DrawPlihedral { get; set; } = false;
        public bool labels { get; set; } = false;
        public bool PerspectiveMode { get; set; } = true;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [18] DISPLAY OPTIONS
 // ─────────────────────────────────────────────────────────────────

        #region Display Options

        public bool DrawPacking { get; set; } = false;
        public bool ShortContact { get; set; } = false;
        /// <summary>
        /// Per-operation visibility for symmetry expansion.
        /// null or empty = all operations enabled.
        /// Index matches Molecule.SymmetryOperations.
        /// </summary>
        public bool[] EnabledSymmetryOps { get; set; }

        public bool ShowAsymmetricUnit { get; set; } = true;
        public bool AutoCentre { get; set; } = true;
        public bool ShowHydrogens { get; set; } = true;
        public bool Stereo { get; set; } = false;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [19] UNIT CELL LINE SETTINGS
 // ─────────────────────────────────────────────────────────────────

        #region Unit Cell Line Settings

        public float CellLineThickness { get; set; } = 0.015f;
        public float CellLineFactor { get; set; } = 0.0127f;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [20] CELL REPETITION SETTINGS
 // ─────────────────────────────────────────────────────────────────

        #region Cell Repetition Settings

        public int repeat_x { get; set; } = 1;
        public int repeat_y { get; set; } = 1;
        public int repeat_z { get; set; } = 1;
        public int repeatOld_x { get; set; } = 1;
        public int repeatOld_y { get; set; } = 1;
        public int repeatOld_z { get; set; } = 1;

        public float ShiftNegativeX { get; set; } = 0f;
        public float ShiftPositiveX { get; set; } = 1f;
        public float ShiftNegativeY { get; set; } = 0f;
        public float ShiftPositiveY { get; set; } = 1f;
        public float ShiftNegativeZ { get; set; } = 0f;
        public float ShiftPositiveZ { get; set; } = 1f;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [21] EXPAND REPETITION SETTINGS
 // ─────────────────────────────────────────────────────────────────

        #region Expand Repetition Settings

        public int repeat_p_x { get; set; } = 1;
        public int repeat_p_y { get; set; } = 1;
        public int repeat_p_z { get; set; } = 1;
        public int repeat2_p_x { get; set; } = 1;
        public int repeat2_p_y { get; set; } = 1;
        public int repeat2_p_z { get; set; } = 1;
        public int repeatOld2_x { get; set; } = 1;
        public int repeatOld2_y { get; set; } = 1;
        public int repeatOld2_z { get; set; } = 1;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [22] Z-CLIPPING
 // ─────────────────────────────────────────────────────────────────

        #region Z-Clipping

        public bool ZClipping = false;
        public float ZClipPosition = 0.0f;
        public float ZClipThickness = 5.0f;
        public bool ZClipShowFront = true;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [23] DEPTH CUE
 // ─────────────────────────────────────────────────────────────────

        #region Depth Cue

        public bool DepthCue { get; set; } = false;
        public float DepthCueStrength { get; set; } = 0.8f;
        public float DepthCueNear { get; set; } = 20.0f;
        public float DepthCueFar { get; set; } = 50.0f;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [24] SELECTION SETTINGS
 // ─────────────────────────────────────────────────────────────────

        #region Selection Settings

        public bool SelectHydrogens { get; set; } = true;
        public bool SelectOnlyVisible { get; set; } = true;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [25] HYDROGEN BOND SETTINGS
 // ─────────────────────────────────────────────────────────────────

        #region Bond Display Settings

        /// <summary>Draw dashed ring inside detected aromatic rings.</summary>
        public bool ShowAromaticRings { get; set; } = false;

        /// <summary>Draw 2/3 parallel sticks for double/triple bonds.</summary>
        public bool ShowMultipleBonds { get; set; } = false;

        #endregion

        #region Hydrogen Bond Settings

        public bool ShowHBonds { get; set; } = false;

        /// <summary>Draw MACE force arrows on unit-cell atoms.</summary>
        public bool ShowForces { get; set; } = false;
        /// <summary>Scale of force arrows (Å per eV/Å).</summary>
        public float ForceArrowScale { get; set; } = 0.8f;
        public bool ShowForceTotal { get; set; } = true;
        public bool ShowForceElectro { get; set; } = false;
        public bool ShowForceDispersion { get; set; } = false;
        public bool ShowForceRepulsion { get; set; } = false;
        public bool ShowIntermolecular { get; set; } = false;
        /// <summary>Cone size relative to a bond-scale head. 50/30: the 50× heads were ~30× too big.</summary>
        public float ForceConePositionScale { get; set; } = 50f / 30f;
        public bool HBondDrawFromHydrogen { get; set; } = false;
        public bool HBondDashed { get; set; } = true;
        public float HBondDashLength { get; set; } = 0.25f;
        public float HBondGapLength { get; set; } = 0.10f;
        public float HBondThickness { get; set; } = 0.025f;
        public bool HBondRequireHydrogen { get; set; } = true;
        public float HBondMinAngle { get; set; } = 63.0f;
        public bool HBondUseVdW { get; set; } = true;
        public float HBondVdWMinus { get; set; } = 5.0f;
        public float HBondVdWPlus { get; set; } = 0.0f;
        public float HBondMinDistance { get; set; } = 1.5f;
        public float HBondMaxDistance { get; set; } = 3.5f;
        public bool HBondIntermolecular { get; set; } = true;
        public bool HBondIntramolecular { get; set; } = true;
        public int HBondMinBondSeparation { get; set; } = 3;
        public bool HBondShowLabels { get; set; } = false;
        public float HBondLineWidth { get; set; } = 1.0f;

        private int _hbondDistanceMode = 1;
        [JsonInclude]
        public int HBondDistanceMode
        {
            get => _hbondDistanceMode;
            set { _hbondDistanceMode = Math.Clamp(value, 0, 1); MarkDirty(); }
        }

        private float _hbondActualMinDistance = 1.5f;
        [JsonInclude]
        public float HBondActualMinDistance
        {
            get => _hbondActualMinDistance;
            set { _hbondActualMinDistance = Math.Max(0.1f, Math.Min(10f, value)); MarkDirty(); }
        }

        private float _hbondActualMaxDistance = 3.5f;
        [JsonInclude]
        public float HBondActualMaxDistance
        {
            get => _hbondActualMaxDistance;
            set { _hbondActualMaxDistance = Math.Max(0.1f, Math.Min(10f, value)); MarkDirty(); }
        }

        [JsonInclude]
        public List<int> HBondAllowedDonors { get; set; } = new List<int> { 7, 8, 16 };

        [JsonInclude]
        public List<int> HBondAllowedAcceptors { get; set; } = new List<int> { 7, 8, 9, 16, 17, 35, 53 };

        private float _hBondColorR = 0.0f;
        private float _hBondColorG = 0.9f;
        private float _hBondColorB = 0.0f;

        [JsonInclude] public float HBondColorR { get => _hBondColorR; set => _hBondColorR = value; }
        [JsonInclude] public float HBondColorG { get => _hBondColorG; set => _hBondColorG = value; }
        [JsonInclude] public float HBondColorB { get => _hBondColorB; set => _hBondColorB = value; }

        [JsonIgnore]
        public Vector3 HBondColor
        {
            get => new Vector3(_hBondColorR, _hBondColorG, _hBondColorB);
            set
            {
                _hBondColorR = value.X;
                _hBondColorG = value.Y;
                _hBondColorB = value.Z;
            }
        }

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [26] SHORT CONTACTS SETTINGS
 // ─────────────────────────────────────────────────────────────────

        #region Short Contacts Settings

        private float _shortContactsVdwMinus = 3.0f;
        private bool _shortContactsIntermolecular = true;
        private bool _shortContactsIntramolecular = false;
        private bool _shortContactsActive = false;
        private bool _shortContactsSelectWholeMolecule = false;
        private bool _shortContactsUseVdwSum = false;

        [JsonInclude]
        public float ShortContactsVdwMinus
        {
            get => _shortContactsVdwMinus;
            set { _shortContactsVdwMinus = Math.Max(0.5f, Math.Min(10.0f, value)); MarkDirty(); }
        }

        [JsonInclude]
        public bool ShortContactsIntermolecular
        {
            get => _shortContactsIntermolecular;
            set { _shortContactsIntermolecular = value; MarkDirty(); }
        }

        [JsonInclude]
        public bool ShortContactsIntramolecular
        {
            get => _shortContactsIntramolecular;
            set { _shortContactsIntramolecular = value; MarkDirty(); }
        }

        [JsonInclude]
        public bool ShortContactsActive
        {
            get => _shortContactsActive;
            set { _shortContactsActive = value; MarkDirty(); }
        }

        [JsonInclude]
        public bool ShortContactsSelectWholeMolecule
        {
            get => _shortContactsSelectWholeMolecule;
            set { _shortContactsSelectWholeMolecule = value; MarkDirty(); }
        }

        [JsonInclude]
        public bool ShortContactsUseVdwSum
        {
            get => _shortContactsUseVdwSum;
            set { _shortContactsUseVdwSum = value; MarkDirty(); }
        }

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [27] EXPAND CONTACTS SETTINGS
 // ─────────────────────────────────────────────────────────────────

        #region Expand Contacts Settings

        private float _expandContactsMinDistance = 3.5f;
        private int _expandContactsSearchRange = 1;
        private bool _expandContactsActive = false;
        private bool _expandContactsShowAll = true;
        private float _expandContactsLineThickness = 0.025f;
        private float _expandContactsDashLength = 0.08f;
        private float _expandContactsGapLength = 0.12f;
        private bool _expandContactsShowLabels = false;
        private int _expandContactsMaxContacts = 20;

        [JsonInclude]
        public float ExpandContactsMinDistance
        {
            get => _expandContactsMinDistance;
            set { _expandContactsMinDistance = value; MarkDirty(); }
        }

        [JsonInclude]
        public int ExpandContactsSearchRange
        {
            get => _expandContactsSearchRange;
            set { _expandContactsSearchRange = Math.Max(1, Math.Min(5, value)); MarkDirty(); }
        }

        public bool ExpandContactsActive { get; set; } = false;

        [JsonInclude]
        public bool ExpandContactsShowAll
        {
            get => _expandContactsShowAll;
            set { _expandContactsShowAll = value; MarkDirty(); }
        }

        [JsonInclude]
        public float ExpandContactsLineThickness
        {
            get => _expandContactsLineThickness;
            set { _expandContactsLineThickness = Math.Max(0.005f, Math.Min(0.1f, value)); MarkDirty(); }
        }

        [JsonInclude]
        public float ExpandContactsDashLength
        {
            get => _expandContactsDashLength;
            set { _expandContactsDashLength = Math.Max(0.02f, Math.Min(0.5f, value)); MarkDirty(); }
        }

        [JsonInclude]
        public float ExpandContactsGapLength
        {
            get => _expandContactsGapLength;
            set { _expandContactsGapLength = Math.Max(0.02f, Math.Min(0.5f, value)); MarkDirty(); }
        }

        [JsonInclude]
        public bool ExpandContactsShowLabels
        {
            get => _expandContactsShowLabels;
            set { _expandContactsShowLabels = value; MarkDirty(); }
        }

        [JsonInclude]
        public int ExpandContactsMaxContacts
        {
            get => _expandContactsMaxContacts;
            set { _expandContactsMaxContacts = Math.Max(5, Math.Min(50, value)); MarkDirty(); }
        }

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [28] SDF / CAVITIES
 // ─────────────────────────────────────────────────────────────────

        #region SDF / Cavity Settings

        public bool ShowCavities { get; set; } = false;
        public float Resolution { get; set; } = 0.10f;
        public float VdWValue { get; set; } = 1.00f;
        public int CavityDrawMode { get; set; } = 0;
        public int CavityPropertyType { get; set; } = 1;

 /// <summary>
 /// Single switch for cavity computation mode.
 ///
 /// false → pure VdW: effective radius = r_vdw * VdWValue + probe.
 /// Thermal motion (Uij/Uiso) is NOT read.
 ///
 /// true → thermal motion as found in the file:
 /// - atom with full Uij → ellipsoid
 /// - atom with only Uiso → sphere from sqrt(Uiso)
 /// - atom without data → pure VdW
 ///
 /// ⚠️ This flag does NOT distinguish anisotropy vs isotropy.
 /// The calculator differentiates based on atom data.
 ///
 /// ⚠️ JSON name kept as "UseAnisotropicVdW" for backward compat.
 /// </summary>
        [JsonPropertyName("UseAnisotropicVdW")]
        public bool UseThermalMotion { get; set; } = false;

        public bool ForceSDFRecalculation { get; set; } = false;
        public bool SDFNeedsRebuild { get; set; } = false;
        public bool UseSDFCorrection { get; set; } = true;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [29] INTERACTION / MEASUREMENTS
 // ─────────────────────────────────────────────────────────────────

        #region Measurements

        public bool IsMeasurementModeActive { get; set; } = false;
        public int MeasurementType { get; set; } = 0;

        #endregion

        #region Interaction Mode Settings

        public bool Lasso { get; set; } = false;
        public bool LassoAtoms { get; set; } = false;
        public bool PickAtoms { get; set; } = false;
        public bool MeasureDistances { get; set; } = false;
        public bool MeasureAngles { get; set; } = false;
        public bool MeasureTorsian { get; set; } = false;
        public bool FindSurroundings { get; set; } = false;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [30] INTERACTION MODE (SECONDARY WINDOW)
 // ─────────────────────────────────────────────────────────────────

        #region Secondary Window Settings

        public bool LassoAtoms2 { get; set; } = false;
        public bool PickAtoms2 { get; set; } = false;
        public bool MeasureDistances2 { get; set; } = false;
        public bool MeasureAngles2 { get; set; } = false;
        public bool MeasureTorsian2 { get; set; } = false;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [31] LEGACY SETTINGS
 // ─────────────────────────────────────────────────────────────────

        #region Legacy Settings

        public bool Hydrogen_bonds1 { get; set; } = false;
        public bool Hydrogen_bonds2 { get; set; } = false;
        public bool expand1 { get; set; } = false;
        public bool expand2 { get; set; } = false;
        public bool merge { get; set; } = false;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [32] HIRSHFELD d_norm COLOR MAPPING
 // ─────────────────────────────────────────────────────────────────

        #region Hirshfeld d_norm color mapping

        public float HirshfeldDNormScale { get; set; } = 0.9f;
        public float HirshfeldDNormWhiteBand { get; set; } = 0.1f;
        public float HirshfeldDNormColorClamp { get; set; } = 3.0f;

        #endregion

        public bool NormalizeHydrogensInPresentation = false;

 // ─────────────────────────────────────────────────────────────────
 // [33] FILE
 // ─────────────────────────────────────────────────────────────────

        #region File Settings

        public string CifFileName { get; set; } = "";

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [34] SYMMETRY ELEMENTS DISPLAY
 // ─────────────────────────────────────────────────────────────────

        #region Symmetry Elements Display

 // ── Master switch ────────────────────────────────────────────────
        public bool ShowSymmetryElements { get; set; } = false;

 // ── Rotation axes: 2, 3, 4, 6 ────────────────────────────────────
        public bool ShowRotationAxes { get; set; } = true;   // OR of 2/3/4/6 (compat)
        public bool ShowRotationAxis2 { get; set; } = true;
        public bool ShowRotationAxis3 { get; set; } = true;
        public bool ShowRotationAxis4 { get; set; } = true;
        public bool ShowRotationAxis6 { get; set; } = true;

 // ── Screw axes: per subscript ────────────────────────────────────
        public bool ShowScrewAxes { get; set; } = true;      // OR of all below (compat)
        public bool ShowScrew21 { get; set; } = true;
        public bool ShowScrew31 { get; set; } = true;
        public bool ShowScrew32 { get; set; } = true;
        public bool ShowScrew41 { get; set; } = true;
        public bool ShowScrew42 { get; set; } = true;
        public bool ShowScrew43 { get; set; } = true;
        public bool ShowScrew61 { get; set; } = true;
        public bool ShowScrew62 { get; set; } = true;
        public bool ShowScrew63 { get; set; } = true;
        public bool ShowScrew64 { get; set; } = true;
        public bool ShowScrew65 { get; set; } = true;

 // ── Rotoinversion axes: −3, −4, −6 ───────────────────────────────
        public bool ShowRotoinversionAxes { get; set; } = true;  // OR of 3/4/6
        public bool ShowRotoinv3 { get; set; } = true;
        public bool ShowRotoinv4 { get; set; } = true;
        public bool ShowRotoinv6 { get; set; } = true;

 // ── Planes / centers ─────────────────────────────────────────────
        public bool ShowMirrorPlanes { get; set; } = true;
        public bool ShowGlidePlanes { get; set; } = true;
        public bool ShowInversionCenters { get; set; } = true;

 // ── Sizes ────────────────────────────────────────────────────────
 /// <summary>Rod thickness for axes (Å).</summary>
        public float SymmetryAxisThickness { get; set; } = 0.020f;

 /// <summary>Scale factor for ITA markers (triangle, rhombus, hexagon, ring).</summary>
        public float SymmetryMarkerSize { get; set; } = 0.22f;

 /// <summary>Sphere radius for inversion centers (Å).</summary>
        public float SymmetryCenterRadius { get; set; } = 0.07f;

 // ── Plane transparency (0..1) ────────────────────────────────────
        public float SymmetryMirrorAlpha { get; set; } = 0.18f;
        public float SymmetryGlideAlpha { get; set; } = 0.13f;

 // ── Colors: rotation axes 2/3/4/6 ────────────────────────────────
        public float SymmetryAxis2ColorR { get; set; } = 1.00f;
        public float SymmetryAxis2ColorG { get; set; } = 0.00f;
        public float SymmetryAxis2ColorB { get; set; } = 0.00f;

        public float SymmetryAxis3ColorR { get; set; } = 0.00f;
        public float SymmetryAxis3ColorG { get; set; } = 1.00f;
        public float SymmetryAxis3ColorB { get; set; } = 0.00f;

        public float SymmetryAxis4ColorR { get; set; } = 0.00f;
        public float SymmetryAxis4ColorG { get; set; } = 0.50f;
        public float SymmetryAxis4ColorB { get; set; } = 1.00f;

        public float SymmetryAxis6ColorR { get; set; } = 1.00f;
        public float SymmetryAxis6ColorG { get; set; } = 0.50f;
        public float SymmetryAxis6ColorB { get; set; } = 0.00f;

 // ── Colors: screw axes (per subscript) ───────────────────────────
 // 2₁
        public float SymmetryScrew21ColorR { get; set; } = 1.00f;
        public float SymmetryScrew21ColorG { get; set; } = 0.55f;
        public float SymmetryScrew21ColorB { get; set; } = 0.55f;

 // 3₁
        public float SymmetryScrew31ColorR { get; set; } = 0.55f;
        public float SymmetryScrew31ColorG { get; set; } = 0.85f;
        public float SymmetryScrew31ColorB { get; set; } = 0.55f;

 // 3₂
        public float SymmetryScrew32ColorR { get; set; } = 0.35f;
        public float SymmetryScrew32ColorG { get; set; } = 0.70f;
        public float SymmetryScrew32ColorB { get; set; } = 0.35f;

 // 4₁
        public float SymmetryScrew41ColorR { get; set; } = 0.55f;
        public float SymmetryScrew41ColorG { get; set; } = 0.70f;
        public float SymmetryScrew41ColorB { get; set; } = 1.00f;

 // 4₂
        public float SymmetryScrew42ColorR { get; set; } = 0.35f;
        public float SymmetryScrew42ColorG { get; set; } = 0.55f;
        public float SymmetryScrew42ColorB { get; set; } = 0.90f;

 // 4₃
        public float SymmetryScrew43ColorR { get; set; } = 0.20f;
        public float SymmetryScrew43ColorG { get; set; } = 0.40f;
        public float SymmetryScrew43ColorB { get; set; } = 0.75f;

 // 6₁
        public float SymmetryScrew61ColorR { get; set; } = 1.00f;
        public float SymmetryScrew61ColorG { get; set; } = 0.75f;
        public float SymmetryScrew61ColorB { get; set; } = 0.45f;

 // 6₂
        public float SymmetryScrew62ColorR { get; set; } = 0.95f;
        public float SymmetryScrew62ColorG { get; set; } = 0.60f;
        public float SymmetryScrew62ColorB { get; set; } = 0.30f;

 // 6₃
        public float SymmetryScrew63ColorR { get; set; } = 0.85f;
        public float SymmetryScrew63ColorG { get; set; } = 0.45f;
        public float SymmetryScrew63ColorB { get; set; } = 0.15f;

 // 6₄
        public float SymmetryScrew64ColorR { get; set; } = 0.75f;
        public float SymmetryScrew64ColorG { get; set; } = 0.35f;
        public float SymmetryScrew64ColorB { get; set; } = 0.10f;

 // 6₅
        public float SymmetryScrew65ColorR { get; set; } = 0.65f;
        public float SymmetryScrew65ColorG { get; set; } = 0.25f;
        public float SymmetryScrew65ColorB { get; set; } = 0.05f;

 // ── Colors: rotoinversion axes ───────────────────────────────────
 // Generic fallback (used by older renderer code paths).
        public float SymmetryRotoinvColorR { get; set; } = 0.67f;
        public float SymmetryRotoinvColorG { get; set; } = 0.35f;
        public float SymmetryRotoinvColorB { get; set; } = 0.90f;

 // −3
        public float SymmetryRotoinv3ColorR { get; set; } = 0.85f;
        public float SymmetryRotoinv3ColorG { get; set; } = 0.45f;
        public float SymmetryRotoinv3ColorB { get; set; } = 1.00f;

 // −4
        public float SymmetryRotoinv4ColorR { get; set; } = 0.60f;
        public float SymmetryRotoinv4ColorG { get; set; } = 0.30f;
        public float SymmetryRotoinv4ColorB { get; set; } = 0.90f;

 // −6
        public float SymmetryRotoinv6ColorR { get; set; } = 0.40f;
        public float SymmetryRotoinv6ColorG { get; set; } = 0.20f;
        public float SymmetryRotoinv6ColorB { get; set; } = 0.75f;

 // ── Colors: planes ───────────────────────────────────────────────
        public float SymmetryMirrorColorR { get; set; } = 0.50f;
        public float SymmetryMirrorColorG { get; set; } = 0.50f;
        public float SymmetryMirrorColorB { get; set; } = 1.00f;

        public float SymmetryGlideColorR { get; set; } = 0.60f;
        public float SymmetryGlideColorG { get; set; } = 0.80f;
        public float SymmetryGlideColorB { get; set; } = 1.00f;

 // ── Colors: inversion centers ────────────────────────────────────
        public float SymmetryCenterColorR { get; set; } = 1.00f;
        public float SymmetryCenterColorG { get; set; } = 0.00f;
        public float SymmetryCenterColorB { get; set; } = 0.00f;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [35] SERIALIZATION
 // ─────────────────────────────────────────────────────────────────

        #region Serialization Methods

        public void Save()
        {
            if (_isLoading) return;
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(CONFIG_FILE, json);
                _isDirty = false;
                System.Diagnostics.Debug.WriteLine($"[Flags] Saved to {CONFIG_FILE}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Flags] Save error: {ex.Message}");
            }
        }

        public void SaveForce()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(CONFIG_FILE, json);
                _isDirty = false;
                System.Diagnostics.Debug.WriteLine($"[Flags] Force saved to {CONFIG_FILE}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Flags] Force save error: {ex.Message}");
            }
        }

        public void Load()
        {
            try
            {
                _isLoading = true;
                if (File.Exists(CONFIG_FILE))
                {
                    string json = File.ReadAllText(CONFIG_FILE);
                    var loaded = JsonSerializer.Deserialize<Flags>(json);
                    if (loaded != null)
                    {
                        CopyFrom(loaded);
                        _isDirty = false;

                        ApplyAtomColorOverrides();

                        System.Diagnostics.Debug.WriteLine($"[Flags] ========================================");
                        System.Diagnostics.Debug.WriteLine($"[Flags] Loaded from: {CONFIG_FILE}");
                        System.Diagnostics.Debug.WriteLine($"[Flags] UseThermalMotion: {UseThermalMotion}");
                        System.Diagnostics.Debug.WriteLine($"[Flags] VdWValue: {VdWValue:F2}");
                        System.Diagnostics.Debug.WriteLine($"[Flags] Resolution: {Resolution:F3}");
                        System.Diagnostics.Debug.WriteLine($"[Flags] AtomColorOverrides: {AtomColorOverrides.Count}");
                        System.Diagnostics.Debug.WriteLine($"[Flags] ========================================");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Flags] No config file found, using defaults");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Flags] Load error: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        public void MarkDirty()
        {
            if (!_isLoading)
            {
                _isDirty = true;
            }
        }

        public bool IsDirty => _isDirty;

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [36] COPY FROM (JSON load merge)
 // ─────────────────────────────────────────────────────────────────

        #region Copy Methods

        private void CopyFrom(Flags other)
        {
            if (other == null) return;

 // ── Application ──
            version = other.version;
            flag_start = other.flag_start;
            intro_duration = other.intro_duration;
            remainingDelay = other.remainingDelay;
            GenerateEquvalents = other.GenerateEquvalents;
            show_properity = other.show_properity;
            Presentation = other.Presentation;
            ATP_ortep = other.ATP_ortep;
            ScreenPresentation = other.ScreenPresentation;
            ColourPresentation = other.ColourPresentation;
            AtomSelected = other.AtomSelected;
            PickingMode = other.PickingMode;
            CleanMeasurements = other.CleanMeasurements;

 // ── Labels ──
            LabelColor = other.LabelColor;
            LabelShadowColor = other.LabelShadowColor;
            LabelScale = other.LabelScale;
            LabelShadow = other.LabelShadow;
            LabelOffsetY = other.LabelOffsetY;
            LabelOffsetX = other.LabelOffsetX;
            LabelFontName = other.LabelFontName;
            LabelFontSize = other.LabelFontSize;
            LabelFontBold = other.LabelFontBold;
            LabelFontItalic = other.LabelFontItalic;
            LabelFontUnderline = other.LabelFontUnderline;
            ShowAxisLabels = other.ShowAxisLabels;
            AxisLabelScale = other.AxisLabelScale;

 // ── Display options ──
            DrawPacking = other.DrawPacking;
            ShortContact = other.ShortContact;
            ShowAsymmetricUnit = other.ShowAsymmetricUnit;
            AutoCentre = other.AutoCentre;
            ShowHydrogens = other.ShowHydrogens;
            DepthCue = other.DepthCue;
            ZClipping = other.ZClipping;
            Stereo = other.Stereo;
            SelectHydrogens = other.SelectHydrogens;
            SelectOnlyVisible = other.SelectOnlyVisible;

 // ── Cell repetition ──
            repeat_x = other.repeat_x;
            repeat_y = other.repeat_y;
            repeat_z = other.repeat_z;
            repeatOld_x = other.repeatOld_x;
            repeatOld_y = other.repeatOld_y;
            repeatOld_z = other.repeatOld_z;
            ShiftNegativeX = other.ShiftNegativeX;
            ShiftPositiveX = other.ShiftPositiveX;
            ShiftNegativeY = other.ShiftNegativeY;
            ShiftPositiveY = other.ShiftPositiveY;
            ShiftNegativeZ = other.ShiftNegativeZ;
            ShiftPositiveZ = other.ShiftPositiveZ;

 // ── H-bonds ──
            ShowAromaticRings = other.ShowAromaticRings;
            ShowMultipleBonds = other.ShowMultipleBonds;
            ShowHBonds = other.ShowHBonds;
            HBondDrawFromHydrogen = other.HBondDrawFromHydrogen;
            HBondDashed = other.HBondDashed;
            HBondDashLength = other.HBondDashLength;
            HBondGapLength = other.HBondGapLength;
            HBondThickness = other.HBondThickness;
            HBondRequireHydrogen = other.HBondRequireHydrogen;
            HBondMinAngle = other.HBondMinAngle;
            HBondUseVdW = other.HBondUseVdW;
            HBondVdWMinus = other.HBondVdWMinus;
            HBondVdWPlus = other.HBondVdWPlus;
            HBondMinDistance = other.HBondMinDistance;
            HBondMaxDistance = other.HBondMaxDistance;
            HBondIntermolecular = other.HBondIntermolecular;
            HBondIntramolecular = other.HBondIntramolecular;
            HBondMinBondSeparation = other.HBondMinBondSeparation;
            HBondShowLabels = other.HBondShowLabels;
            HBondLineWidth = other.HBondLineWidth;
            _hbondDistanceMode = other._hbondDistanceMode;
            _hbondActualMinDistance = other._hbondActualMinDistance;
            _hbondActualMaxDistance = other._hbondActualMaxDistance;

            HBondAllowedDonors = other.HBondAllowedDonors != null
                ? new List<int>(other.HBondAllowedDonors)
                : new List<int> { 7, 8, 16 };

            HBondAllowedAcceptors = other.HBondAllowedAcceptors != null
                ? new List<int>(other.HBondAllowedAcceptors)
                : new List<int> { 7, 8, 9, 16, 17, 35, 53 };

            _hBondColorR = other._hBondColorR;
            _hBondColorG = other._hBondColorG;
            _hBondColorB = other._hBondColorB;

 // ── Atom color overrides ──
            AtomColorOverrides = other.AtomColorOverrides != null
                ? new Dictionary<int, float[]>(other.AtomColorOverrides)
                : new Dictionary<int, float[]>();

 // ── Background colors ──
            _color1R = other._color1R;
            _color1G = other._color1G;
            _color1B = other._color1B;
            _color1A = other._color1A;
            _color2R = other._color2R;
            _color2G = other._color2G;
            _color2B = other._color2B;
            _color2A = other._color2A;

 // ── Style ──
            smuth = other.smuth;
            transparency = other.transparency;
            BallRadiusNonMetal = other.BallRadiusNonMetal;
            BallRadiusMetal = other.BallRadiusMetal;
            StickRadius = other.StickRadius;
            StickRadiusFactor = other.StickRadiusFactor;

 // ── SDF / Cavity ──
            UseThermalMotion = other.UseThermalMotion;
            ForceSDFRecalculation = other.ForceSDFRecalculation;
            SDFNeedsRebuild = other.SDFNeedsRebuild;
            UseSDFCorrection = other.UseSDFCorrection;
            ShowCavities = other.ShowCavities;
            Resolution = other.Resolution;
            VdWValue = other.VdWValue;
            CavityDrawMode = other.CavityDrawMode;
            CavityPropertyType = other.CavityPropertyType;

 // ── Bonds / atoms ──
            MAX_ATOM_DISTANCE = other.MAX_ATOM_DISTANCE;
            MAX_H_ATOM_DISTANCE = other.MAX_H_ATOM_DISTANCE;
            MAX_BONDED_ATOMS = other.MAX_BONDED_ATOMS;
            MAX_H_BONDED_ATOMS = other.MAX_H_BONDED_ATOMS;
            BOND_FACTOR = other.BOND_FACTOR;
            H_acceptance_angle = other.H_acceptance_angle;
            BOND_FACTOR_METALS = other.BOND_FACTOR_METALS;
            ATP_ProbabilityPercent = other.ATP_ProbabilityPercent;
            ATP_Probability = other.ATP_Probability;
            AtomicRadiiType = other.AtomicRadiiType;
            Poli_dyst = other.Poli_dyst;
            PolyhedronAlpha = other.PolyhedronAlpha;
            view = other.view;

 // ── Drawing / view ──
            Draw_cell = other.Draw_cell;
            DrawStereo = other.DrawStereo;
            Shift = other.Shift;
            DrawCharges = other.DrawCharges;
            DrawPlihedral = other.DrawPlihedral;
            labels = other.labels;
            PerspectiveMode = other.PerspectiveMode;

 // ── Legacy ──
            Hydrogen_bonds1 = other.Hydrogen_bonds1;
            Hydrogen_bonds2 = other.Hydrogen_bonds2;
            expand1 = other.expand1;
            expand2 = other.expand2;
            merge = other.merge;

 // ── Expand repetition ──
            repeat_p_x = other.repeat_p_x;
            repeat_p_y = other.repeat_p_y;
            repeat_p_z = other.repeat_p_z;
            repeat2_p_x = other.repeat2_p_x;
            repeat2_p_y = other.repeat2_p_y;
            repeat2_p_z = other.repeat2_p_z;
            repeatOld2_x = other.repeatOld2_x;
            repeatOld2_y = other.repeatOld2_y;
            repeatOld2_z = other.repeatOld2_z;

 // ── Style ──
            sticsSize = other.sticsSize;
            ballSize = other.ballSize;
            AtomStyle = other.AtomStyle;
            ShowHydrogenBonds = other.ShowHydrogenBonds;

 // ── Interaction ──
            Lasso = other.Lasso;
            LassoAtoms = other.LassoAtoms;
            PickAtoms = other.PickAtoms;
            MeasureDistances = other.MeasureDistances;
            MeasureAngles = other.MeasureAngles;
            MeasureTorsian = other.MeasureTorsian;
            FindSurroundings = other.FindSurroundings;

            LassoAtoms2 = other.LassoAtoms2;
            PickAtoms2 = other.PickAtoms2;
            MeasureDistances2 = other.MeasureDistances2;
            MeasureAngles2 = other.MeasureAngles2;
            MeasureTorsian2 = other.MeasureTorsian2;

 // ── File ──
            CifFileName = other.CifFileName;

 // ── Lighting ──
            Enabled = other.Enabled;
            PerPixelLighting = other.PerPixelLighting;
            DirectionX = other.DirectionX;
            DirectionY = other.DirectionY;
            DirectionZ = other.DirectionZ;
            SpecularPower = other.SpecularPower;
            SpecularIntensity = other.SpecularIntensity;
            DiffuseIntensity = other.DiffuseIntensity;
            AmbientIntensity = other.AmbientIntensity;
            Light2Enabled = other.Light2Enabled;
            Light2DirectionX = other.Light2DirectionX;
            Light2DirectionY = other.Light2DirectionY;
            Light2DirectionZ = other.Light2DirectionZ;
            Light2DiffuseIntensity = other.Light2DiffuseIntensity;
            Light2AmbientIntensity = other.Light2AmbientIntensity;
            Light2SpecularIntensity = other.Light2SpecularIntensity;

 // ── Atom display ──
            HydrogenBallScale = other.HydrogenBallScale;

 // ── Ellipsoid ──
            EllipsoidDiskThickness = other.EllipsoidDiskThickness;
            EllipsoidBaseRadius = other.EllipsoidBaseRadius;
            ShowHydrogenIsoSpheres = other.ShowHydrogenIsoSpheres;
            EllipsoidProbability = other.EllipsoidProbability;

 // ── Expand contacts ──
            ExpandContactsMinDistance = other.ExpandContactsMinDistance;
            ExpandContactsSearchRange = other.ExpandContactsSearchRange;
            ExpandContactsLineThickness = other.ExpandContactsLineThickness;
            ExpandContactsDashLength = other.ExpandContactsDashLength;
            ExpandContactsGapLength = other.ExpandContactsGapLength;
            ExpandContactsShowLabels = other.ExpandContactsShowLabels;
            ExpandContactsMaxContacts = other.ExpandContactsMaxContacts;

 // ── Short contacts ──
            ShortContactsVdwMinus = other.ShortContactsVdwMinus;
            ShortContactsIntermolecular = other.ShortContactsIntermolecular;
            ShortContactsIntramolecular = other.ShortContactsIntramolecular;
            ShortContactsActive = other.ShortContactsActive;
            ShortContactsSelectWholeMolecule = other.ShortContactsSelectWholeMolecule;
            ShortContactsUseVdwSum = other.ShortContactsUseVdwSum;

 // ── Cell lines ──
            CellLineThickness = other.CellLineThickness;
            CellLineFactor = other.CellLineFactor;

 // ── Rendering quality ──
            SSAASamples = other.SSAASamples;
            MSAAOnSceneRT = other.MSAAOnSceneRT;
            ProgressiveAASamples = other.ProgressiveAASamples;
            MSAAOnBackbuffer = other.MSAAOnBackbuffer;
            AnisotropyLevel = other.AnisotropyLevel;
            RenderScale = other.RenderScale;
            SharpenStrength = other.SharpenStrength;

 // ── Hirshfeld ──
            HirshfeldDNormScale = other.HirshfeldDNormScale;
            HirshfeldDNormWhiteBand = other.HirshfeldDNormWhiteBand;
            HirshfeldDNormColorClamp = other.HirshfeldDNormColorClamp;

            NormalizeHydrogensInPresentation = other.NormalizeHydrogensInPresentation;

 // ═════════════════════════════════════════════════════════════
 // Symmetry Elements
 // ═════════════════════════════════════════════════════════════
            ShowSymmetryElements = other.ShowSymmetryElements;

 // Visibility flags
            ShowRotationAxes = other.ShowRotationAxes;
            ShowRotationAxis2 = other.ShowRotationAxis2;
            ShowRotationAxis3 = other.ShowRotationAxis3;
            ShowRotationAxis4 = other.ShowRotationAxis4;
            ShowRotationAxis6 = other.ShowRotationAxis6;

            ShowScrewAxes = other.ShowScrewAxes;
            ShowScrew21 = other.ShowScrew21;
            ShowScrew31 = other.ShowScrew31;
            ShowScrew32 = other.ShowScrew32;
            ShowScrew41 = other.ShowScrew41;
            ShowScrew42 = other.ShowScrew42;
            ShowScrew43 = other.ShowScrew43;
            ShowScrew61 = other.ShowScrew61;
            ShowScrew62 = other.ShowScrew62;
            ShowScrew63 = other.ShowScrew63;
            ShowScrew64 = other.ShowScrew64;
            ShowScrew65 = other.ShowScrew65;

            ShowRotoinversionAxes = other.ShowRotoinversionAxes;
            ShowRotoinv3 = other.ShowRotoinv3;
            ShowRotoinv4 = other.ShowRotoinv4;
            ShowRotoinv6 = other.ShowRotoinv6;

            ShowMirrorPlanes = other.ShowMirrorPlanes;
            ShowGlidePlanes = other.ShowGlidePlanes;
            ShowInversionCenters = other.ShowInversionCenters;

 // Sizes
            SymmetryAxisThickness = other.SymmetryAxisThickness;
            SymmetryMarkerSize = other.SymmetryMarkerSize;
            SymmetryCenterRadius = other.SymmetryCenterRadius;

 // Plane transparency
            SymmetryMirrorAlpha = other.SymmetryMirrorAlpha;
            SymmetryGlideAlpha = other.SymmetryGlideAlpha;

 // Axis colors
            SymmetryAxis2ColorR = other.SymmetryAxis2ColorR;
            SymmetryAxis2ColorG = other.SymmetryAxis2ColorG;
            SymmetryAxis2ColorB = other.SymmetryAxis2ColorB;
            SymmetryAxis3ColorR = other.SymmetryAxis3ColorR;
            SymmetryAxis3ColorG = other.SymmetryAxis3ColorG;
            SymmetryAxis3ColorB = other.SymmetryAxis3ColorB;
            SymmetryAxis4ColorR = other.SymmetryAxis4ColorR;
            SymmetryAxis4ColorG = other.SymmetryAxis4ColorG;
            SymmetryAxis4ColorB = other.SymmetryAxis4ColorB;
            SymmetryAxis6ColorR = other.SymmetryAxis6ColorR;
            SymmetryAxis6ColorG = other.SymmetryAxis6ColorG;
            SymmetryAxis6ColorB = other.SymmetryAxis6ColorB;

 // Screw colors (per subscript)
            SymmetryScrew21ColorR = other.SymmetryScrew21ColorR;
            SymmetryScrew21ColorG = other.SymmetryScrew21ColorG;
            SymmetryScrew21ColorB = other.SymmetryScrew21ColorB;
            SymmetryScrew31ColorR = other.SymmetryScrew31ColorR;
            SymmetryScrew31ColorG = other.SymmetryScrew31ColorG;
            SymmetryScrew31ColorB = other.SymmetryScrew31ColorB;
            SymmetryScrew32ColorR = other.SymmetryScrew32ColorR;
            SymmetryScrew32ColorG = other.SymmetryScrew32ColorG;
            SymmetryScrew32ColorB = other.SymmetryScrew32ColorB;
            SymmetryScrew41ColorR = other.SymmetryScrew41ColorR;
            SymmetryScrew41ColorG = other.SymmetryScrew41ColorG;
            SymmetryScrew41ColorB = other.SymmetryScrew41ColorB;
            SymmetryScrew42ColorR = other.SymmetryScrew42ColorR;
            SymmetryScrew42ColorG = other.SymmetryScrew42ColorG;
            SymmetryScrew42ColorB = other.SymmetryScrew42ColorB;
            SymmetryScrew43ColorR = other.SymmetryScrew43ColorR;
            SymmetryScrew43ColorG = other.SymmetryScrew43ColorG;
            SymmetryScrew43ColorB = other.SymmetryScrew43ColorB;
            SymmetryScrew61ColorR = other.SymmetryScrew61ColorR;
            SymmetryScrew61ColorG = other.SymmetryScrew61ColorG;
            SymmetryScrew61ColorB = other.SymmetryScrew61ColorB;
            SymmetryScrew62ColorR = other.SymmetryScrew62ColorR;
            SymmetryScrew62ColorG = other.SymmetryScrew62ColorG;
            SymmetryScrew62ColorB = other.SymmetryScrew62ColorB;
            SymmetryScrew63ColorR = other.SymmetryScrew63ColorR;
            SymmetryScrew63ColorG = other.SymmetryScrew63ColorG;
            SymmetryScrew63ColorB = other.SymmetryScrew63ColorB;
            SymmetryScrew64ColorR = other.SymmetryScrew64ColorR;
            SymmetryScrew64ColorG = other.SymmetryScrew64ColorG;
            SymmetryScrew64ColorB = other.SymmetryScrew64ColorB;
            SymmetryScrew65ColorR = other.SymmetryScrew65ColorR;
            SymmetryScrew65ColorG = other.SymmetryScrew65ColorG;
            SymmetryScrew65ColorB = other.SymmetryScrew65ColorB;

 // Rotoinversion colors — generic fallback
            SymmetryRotoinvColorR = other.SymmetryRotoinvColorR;
            SymmetryRotoinvColorG = other.SymmetryRotoinvColorG;
            SymmetryRotoinvColorB = other.SymmetryRotoinvColorB;

 // Rotoinversion colors — per order
            SymmetryRotoinv3ColorR = other.SymmetryRotoinv3ColorR;
            SymmetryRotoinv3ColorG = other.SymmetryRotoinv3ColorG;
            SymmetryRotoinv3ColorB = other.SymmetryRotoinv3ColorB;
            SymmetryRotoinv4ColorR = other.SymmetryRotoinv4ColorR;
            SymmetryRotoinv4ColorG = other.SymmetryRotoinv4ColorG;
            SymmetryRotoinv4ColorB = other.SymmetryRotoinv4ColorB;
            SymmetryRotoinv6ColorR = other.SymmetryRotoinv6ColorR;
            SymmetryRotoinv6ColorG = other.SymmetryRotoinv6ColorG;
            SymmetryRotoinv6ColorB = other.SymmetryRotoinv6ColorB;

 // Plane colors
            SymmetryMirrorColorR = other.SymmetryMirrorColorR;
            SymmetryMirrorColorG = other.SymmetryMirrorColorG;
            SymmetryMirrorColorB = other.SymmetryMirrorColorB;
            SymmetryGlideColorR = other.SymmetryGlideColorR;
            SymmetryGlideColorG = other.SymmetryGlideColorG;
            SymmetryGlideColorB = other.SymmetryGlideColorB;

 // Center color
            SymmetryCenterColorR = other.SymmetryCenterColorR;
            SymmetryCenterColorG = other.SymmetryCenterColorG;
            SymmetryCenterColorB = other.SymmetryCenterColorB;
        }

        #endregion

 // ─────────────────────────────────────────────────────────────────
 // [37] RESET METHODS
 // ─────────────────────────────────────────────────────────────────

        #region Reset Methods

        public void ResetToDefault()
        {
            Enabled = true;
            PerPixelLighting = true;
            DirectionX = 0.21f;
            DirectionY = 0.5f;
            DirectionZ = -1.0f;
            SpecularPower = 30f;
            SpecularIntensity = 0.9f;
            DiffuseIntensity = 0.82f;
            AmbientIntensity = 0.06f;

            Light2Enabled = true;
            Light2DirectionX = -0.52f;
            Light2DirectionY = -0.5f;
            Light2DirectionZ = -5.0f;
            Light2DiffuseIntensity = 0.28f;
            Light2AmbientIntensity = 0.1f;
            Light2SpecularIntensity = 0.18f;

            HydrogenBallScale = 0.7f;

            EllipsoidDiskThickness = 0.15f;
            EllipsoidBaseRadius = 1.05f;
            EllipsoidProbability = 0.5f;
            EllipsoidDisksEnabled = true;
            ShowHydrogenIsoSpheres = true;

            ExpandContactsMinDistance = 3.5f;
            ExpandContactsSearchRange = 1;
            ExpandContactsActive = false;
            ExpandContactsShowAll = true;
            ExpandContactsLineThickness = 0.025f;
            ExpandContactsDashLength = 0.08f;
            ExpandContactsGapLength = 0.12f;
            ExpandContactsShowLabels = false;
            ExpandContactsMaxContacts = 20;

            ShortContactsVdwMinus = 0.0f;
            ShortContactsIntermolecular = true;
            ShortContactsIntramolecular = true;
            ShortContactsActive = false;
            ShortContactsSelectWholeMolecule = false;
            ShowLabelsFor = 0;

            HBondAllowedDonors = new List<int> { 7, 8, 16 };
            HBondAllowedAcceptors = new List<int> { 7, 8, 9, 16, 17, 35, 53 };
            _hBondColorR = 0.0f;
            _hBondColorG = 0.9f;
            _hBondColorB = 0.0f;

            MSAAOnSceneRT = 4;

            AtomColorOverrides.Clear();
            VdwParameters.ResetAllColors();

            MarkDirty();
            Save();
        }

        public void Reset()
        {
            ResetToDefault();

            view = 0;
            GenerateEquvalents = 0;
            Presentation = 2;
            repeat_x = 1;
            repeat_y = 1;
            repeat_z = 1;
            repeatOld_x = 1;
            repeatOld_y = 1;
            repeatOld_z = 1;
            repeat_p_x = 1;
            repeat_p_y = 1;
            repeat_p_z = 1;
            repeat2_p_x = 1;
            repeat2_p_y = 1;
            repeat2_p_z = 1;

            ShowCavities = true;
            UseThermalMotion = false;
            ForceSDFRecalculation = false;
            VdWValue = 1.0f;
            Resolution = 0.15f;
            CavityDrawMode = 0;
            CavityPropertyType = 0;
            SDFNeedsRebuild = false;
            UseSDFCorrection = true;

            Draw_cell = true;
            DrawStereo = false;
            DrawCharges = false;
            DrawPlihedral = false;
            labels = false;
            Hydrogen_bonds1 = false;
            Hydrogen_bonds2 = false;
            expand1 = false;
            expand2 = false;
            merge = false;

            Lasso = false;
            LassoAtoms = false;
            PickAtoms = false;
            MeasureDistances = false;
            MeasureAngles = false;
            MeasureTorsian = false;
            FindSurroundings = false;

            SelectHydrogens = true;
            SelectOnlyVisible = true;

            CellLineThickness = 0.015f;
            CellLineFactor = 0.0127f;

            ATP_ProbabilityPercent = 50;
            ATP_Probability = 1.538f;

            HirshfeldDNormScale = 1.2f;
            HirshfeldDNormWhiteBand = 0.08f;
            HirshfeldDNormColorClamp = 3.0f;

            MarkDirty();
            Save();
        }

        public void RequestSDFRebuild()
        {
            SDFNeedsRebuild = true;
            ForceSDFRecalculation = true;
            System.Diagnostics.Debug.WriteLine("[Flags] SDF rebuild requested");
            MarkDirty();
            Save();
        }

        public void ClearSDFRebuildFlags()
        {
            SDFNeedsRebuild = false;
            ForceSDFRecalculation = false;
            MarkDirty();
            Save();
        }

        public void AutoSave()
        {
            if (!_isLoading)
            {
                Save();
            }
        }

        #endregion
    }
}