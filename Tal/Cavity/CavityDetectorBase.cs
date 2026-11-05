// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: CavityDetectorBase.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Tal.Cavity
{
 /// <summary>
 /// Parameters for mesh generation using Marching Cubes algorithm.
 /// </summary>
    public class MeshParameters
    {
        public float IsoLevel { get; set; } = 0.0f;
        public float SnapTolerance { get; set; } = 0.0006f;
        public bool GenerateNormals { get; set; } = true;
        public bool DebugMode { get; set; } = true;
        public bool FixWinding { get; set; } = true;
        public float GradientScale { get; set; } = 1.0f;
        public bool SmoothDensity { get; set; } = true;
        public int SmoothingIterations { get; set; } = 2;
        public float SmoothingFactor { get; set; } = 0.3f;
        public int GaussianBlurIterations { get; set; } = 2;
        public float GaussianSigma { get; set; } = 0.8f;
        public float BoundaryMargin { get; set; } = 0.12f;
        public Action<string, int, int> ProgressCallback { get; set; }
    }

 /// <summary>
 /// Base class for cavity detection with shared utilities.
 /// Provides VdW radius lookup, symmetry operations, color generation, and mesh parameters.
 /// </summary>
    public class CavityDetectorBase
    {
        #region Constants

        protected static readonly (int, int, int)[] Neighbors = new[]
        {
            (1, 0, 0), (-1, 0, 0),
            (0, 1, 0), (0, -1, 0),
            (0, 0, 1), (0, 0, -1)
        };

        private const int DefaultAtomicNumber = 6;

        #endregion

        #region Caches

        private readonly Dictionary<string, float> _vdwRadiusCache = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _atomicNumberCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private List<SymmetryElements.ReadSymm> _cachedSymMatrix;
        private int _cachedSpaceGroup = -1;
        private readonly object _symCacheLock = new object();

        #endregion

        #region Structures

 /// <summary>
 /// Minimal atom data structure for cavity detection.
 /// </summary>
        public struct AtomData
        {
            public float X, Y, Z;
            public float Radius;
            public float RadiusSq;

            public AtomData(float x, float y, float z, float radius)
            {
                X = x;
                Y = y;
                Z = z;
                Radius = radius;
                RadiusSq = radius * radius;
            }
        }

        #endregion

        #region VdW Radius and Atomic Number Lookup

 /// <summary>
 /// Gets the van der Waals radius for an atom name and atomic number.
 /// Uses the central VdwParameters table.
 /// </summary>
        protected float GetVdwRadius(string atomType, int atomicNumber)
        {
            if (string.IsNullOrEmpty(atomType))
                return VdwParameters.GetRadius(atomicNumber);

 // Check cache
            if (_vdwRadiusCache.TryGetValue(atomType, out float radius))
                return radius;

 // Get from central table
            radius = VdwParameters.GetRadius(atomType);

 // If not found, use atomic number
            if (radius <= 0)
                radius = VdwParameters.GetRadius(atomicNumber);

 // Store in cache
            _vdwRadiusCache[atomType] = radius;
            return radius;
        }

 /// <summary>
 /// Gets the van der Waals radius for an atomic number.
 /// </summary>
        protected float GetVdwRadiusByAtomicNumber(int atomicNumber)
        {
            return VdwParameters.GetRadius(atomicNumber);
        }

 /// <summary>
 /// Gets the atomic number for an atom name.
 /// </summary>
        protected int GetAtomicNumber(string atomType)
        {
            if (string.IsNullOrEmpty(atomType))
                return DefaultAtomicNumber;

            if (_atomicNumberCache.TryGetValue(atomType, out int number))
                return number;

            number = VdwParameters.GetAtomicNumber(atomType);
            if (number <= 0)
                number = DefaultAtomicNumber;

            _atomicNumberCache[atomType] = number;
            return number;
        }

 /// <summary>
 /// Gets the ionic radius for an atom name.
 /// </summary>
        protected float GetIonicRadius(string atomType)
        {
            if (string.IsNullOrEmpty(atomType))
                return 1.0f;

            return VdwParameters.GetIonicRadius(atomType);
        }

 /// <summary>
 /// Gets the ionic radius for an atomic number.
 /// </summary>
        protected float GetIonicRadius(int atomicNumber)
        {
            return VdwParameters.GetIonicRadius(atomicNumber);
        }

        #endregion

        #region Symmetry Operations

 /// <summary>
 /// Gets the symmetry matrix for a molecule.
 /// Uses caching for performance.
 /// </summary>
        protected List<SymmetryElements.ReadSymm> GetSymmetryMatrixForMolecule(Molecule molecule)
        {
            if (molecule == null)
                return GetIdentitySymmetry();

            int spaceGroup = molecule.SpaceGroupNumber;

            if (spaceGroup <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid SpaceGroupNumber: {spaceGroup}. " +
                    "Space group must be set from CIF/RES data (1-230). " +
                    "Check if molecule was properly loaded from file."
                );
            }

            lock (_symCacheLock)
            {
                // Prefer live molecule operations FIRST.
                // P21/c and P21/n share space-group number 14 — caching only by
                // number would keep the old setting's operators after a transform.
                if (molecule.SymmetryOperations != null && molecule.SymmetryOperations.Count > 0)
                {
                    _cachedSymMatrix = molecule.SymmetryOperations;
                    _cachedSpaceGroup = spaceGroup;
                    return _cachedSymMatrix;
                }

                if (_cachedSymMatrix != null && _cachedSpaceGroup == spaceGroup)
                    return _cachedSymMatrix;

                var ops = SymmetryElements.CreateTransformations(spaceGroup);
                if (ops != null && ops.Count > 0)
                {
                    _cachedSymMatrix = ops;
                    _cachedSpaceGroup = spaceGroup;
                    return _cachedSymMatrix;
                }

                _cachedSymMatrix = GetIdentitySymmetry();
                _cachedSpaceGroup = spaceGroup;
                return _cachedSymMatrix;
            }
        }

 /// <summary>
 /// Gets the number of symmetry operations for a molecule.
 /// </summary>
        protected int GetSymmetryCount(Molecule molecule)
        {
            var matrix = GetSymmetryMatrixForMolecule(molecule);
            return matrix?.Count ?? 1;
        }

        /// <summary>
        /// Clears the cached symmetry operators (call after space-group setting transform).
        /// </summary>
        public void InvalidateSymmetryCache()
        {
            lock (_symCacheLock)
            {
                _cachedSymMatrix = null;
                _cachedSpaceGroup = -1;
            }
        }

 /// <summary>
 /// Creates the identity symmetry matrix (space group P1).
 /// </summary>
        private List<SymmetryElements.ReadSymm> GetIdentitySymmetry()
        {
            var list = new List<SymmetryElements.ReadSymm>();
            var identity = new SymmetryElements.ReadSymm();
            identity.sign1 = new int[] { 1, 1, 1 };
            identity.sign2 = new int[] { 0, 0, 0 };
            identity.pos1 = new int[] { 0, 1, 2 };
            identity.pos2 = new int[] { 0, 0, 0 };
            identity.shift = new float[] { 0, 0, 0 };
            identity.orientation = 0;
            list.Add(identity);
            return list;
        }

 /// <summary>
 /// Deprecated - use GetSymmetryMatrixForMolecule(Molecule molecule) instead.
 /// </summary>
        [Obsolete("Use GetSymmetryMatrixForMolecule(Molecule molecule) instead")]
        protected int GetSymmetryCount()
        {
            return 1;
        }

        #endregion

        #region Color Generation

 /// <summary>
 /// Generates a color from HSV values.
 /// </summary>
        public static Microsoft.Xna.Framework.Color ColorFromHSV(float h, float s, float v)
        {
            h = h - (float)Math.Floor(h);
            s = Math.Clamp(s, 0f, 1f);
            v = Math.Clamp(v, 0f, 1f);

            int hi = (int)(h * 6) % 6;
            float f = h * 6 - hi;
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            switch (hi)
            {
                case 0: return new Microsoft.Xna.Framework.Color(v, t, p);
                case 1: return new Microsoft.Xna.Framework.Color(q, v, p);
                case 2: return new Microsoft.Xna.Framework.Color(p, v, t);
                case 3: return new Microsoft.Xna.Framework.Color(p, q, v);
                case 4: return new Microsoft.Xna.Framework.Color(t, p, v);
                case 5: return new Microsoft.Xna.Framework.Color(v, p, q);
                default: return Microsoft.Xna.Framework.Color.White;
            }
        }

 /// <summary>
 /// Generates a deterministic color from an ID using golden ratio.
 /// </summary>
        public static Microsoft.Xna.Framework.Color GenerateColorFromId(int id)
        {
            float goldenRatio = 0.618033988749895f;
            float hue = (id * goldenRatio) % 1.0f;
            return ColorFromHSV(hue, 0.88f, 0.95f);
        }

        #endregion

        #region Mesh Parameters

 /// <summary>
 /// Gets mesh parameters for a given smoothing level (1-5).
 /// </summary>
        public static MeshParameters GetMeshParametersForSmoothingLevel(int smoothingLevel)
        {
            // Delegate to CavityRenderer — same table as the Cavity Settings spinner (0–10).
            return CavityRenderer.GetMeshParametersForSmoothingLevel(smoothingLevel);
        }


        #endregion

        #region Vector3 Comparer

 /// <summary>
 /// Equality comparer for System.Numerics.Vector3 with tolerance.
 /// </summary>
        public class SysVector3Comparer : IEqualityComparer<System.Numerics.Vector3>
        {
            private const float Tolerance = 0.0001f;

            public bool Equals(System.Numerics.Vector3 a, System.Numerics.Vector3 b)
            {
                return Math.Abs(a.X - b.X) < Tolerance &&
                       Math.Abs(a.Y - b.Y) < Tolerance &&
                       Math.Abs(a.Z - b.Z) < Tolerance;
            }

            public int GetHashCode(System.Numerics.Vector3 v)
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

        #region Resolution Validation

 /// <summary>
 /// Checks if the resolution is appropriate for the cell size.
 /// Uses static Flags.MAX_VOXELS limit (auto-detected for 64-bit systems).
 /// </summary>
        public static bool IsResolutionValidForCell(float resolution, Molecule molecule)
        {
            if (molecule == null) return true;

            float a = molecule.a;
            float b = molecule.b;
            float c = molecule.c;

            double cosAlpha = Math.Cos(molecule.alpha);
            double cosBeta = Math.Cos(molecule.beta);
            double cosGamma = Math.Cos(molecule.gamma);

            double realVolume = a * b * c * Math.Sqrt(
                1 - cosAlpha * cosAlpha - cosBeta * cosBeta - cosGamma * cosGamma
                + 2 * cosAlpha * cosBeta * cosGamma
            );

 // ⭐ Use double for calculation, then check against long limit
            double estimatedVoxelsDouble = realVolume / (resolution * resolution * resolution);

            if (estimatedVoxelsDouble > long.MaxValue)
                return false;

            long estimatedVoxels = (long)estimatedVoxelsDouble;
            return estimatedVoxels <= Flags.MAX_VOXELS;
        }

 /// <summary>
 /// Gets the recommended resolution for a given cell.
 /// Uses static Flags.MAX_VOXELS limit (auto-detected for 64-bit systems).
 /// </summary>
        public static float GetRecommendedResolution(Molecule molecule)
        {
            if (molecule == null) return 0.15f;

            float a = molecule.a;
            float b = molecule.b;
            float c = molecule.c;

            float volume = a * b * c;
 // ⭐ Use static Flags.MAX_VOXELS (not Flags.Instance)
            float recommendedRes = (float)Math.Pow(volume / Flags.MAX_VOXELS, 1.0 / 3.0);
            recommendedRes = (float)Math.Ceiling(recommendedRes * 100) / 100;
            return MathHelper.Clamp(recommendedRes, 0.08f, 0.25f);
        }

        #endregion
    }
}