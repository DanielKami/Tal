// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: CavityPropertyMapper.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Color = Microsoft.Xna.Framework.Color;
using SysVector3 = System.Numerics.Vector3;
using XnaVector4 = Microsoft.Xna.Framework.Vector4;

namespace Tal.Cavity
{
 /// <summary>
 /// Maps atomic properties (charge, hydrophobicity, etc.) onto cavity surfaces.
 /// Optimized using BinarySearch on sorted X data from VoxelScanner for fast atom lookup.
 /// Preserves original weighted average logsc for smooth color transitions.
 /// All colors are XNA Vector4 (Microsoft.Xna.Framework.Vector4).
 /// </summary>
    public class CavityPropertyMapper : IDisposable
    {
        #region Property Types

 /// <summary>
 /// Defines the types of molecular properties that can be mapped to cavity surfaces.
 /// </summary>
        public enum PropertyType
        {
 /// <summary>No property mapping applied.</summary>
            None = 0,
 /// <summary>Atomic charge values.</summary>
            Charge = 1,
 /// <summary>Hydrophobicity values.</summary>
            Hydrophobicity = 2,
 /// <summary>Atomic number of elements.</summary>
            AtomicNumber = 3,
 /// <summary>Van der Waals radius.</summary>
            VdWRadius = 4,
 /// <summary>Atomic occupancy values.</summary>
            Occupancy = 5
        }

        #endregion

        #region Private Fields

        private readonly Molecule _molecule;
        private readonly List<Cavity> _cavities;
        private PropertyType _currentProperty = PropertyType.None;
        private float _searchRadius = 3.0f;
        private bool _useWeightedAverage = true;
        private float _fallbackValue = 0.0f;
        private Color _fallbackColor = Color.Gray;
        private bool _disposed = false;

 // Cache for atom properties and aftersitions
        private float[] _cachedPropertyValues;
        private SysVector3[] _cachedAtomPositions;
        private int _cachedAtomCount = -1;
        private int _cachedPropertyType = -1;

 // Thread-local buffer for nearby atom searches
        private readonly ThreadLocal<List<NearbyAtom>> _nearbyAtomsBufferLocal;

 // Collection of all buffers for cleanup
        private readonly List<List<NearbyAtom>> _allBuffers = new List<List<NearbyAtom>>();
        private readonly object _bufferLock = new object();

 // Performance and safety constants
        private const float MIN_DISTANCE_SQ = 0.0001f;
        private const int MAX_SAFE_VERTICES = 1_000_000;

 // Debug and performance tracking
        private int _totalCavitiesProcessed = 0;
        private int _totalVerticesProcessed = 0;
        private int _totalErrors = 0;
        private readonly object _debugLock = new object();

        #endregion

        #region Constructor

 /// <summary>
 /// Initializes a new instance of the CavityPropertyMapper.
 /// </summary>
 /// <param name="molecule">The molecule containing atomic data.</param>
 /// <param name="cavities">The collection of cavities to map properties onto.</param>
 /// <exception cref="ArgumentNullException">Thrown when molecule is null.</exception>
        public CavityPropertyMapper(Molecule molecule, List<Cavity> cavities)
        {
            _molecule = molecule ?? throw new ArgumentNullException(nameof(molecule));
            _cavities = cavities ?? new List<Cavity>();

            _nearbyAtomsBufferLocal = new ThreadLocal<List<NearbyAtom>>(
                () => new List<NearbyAtom>(64),
                trackAllValues: true
            );

            Debug.WriteLine($"[PropertyMapper] Constructor: {_cavities.Count} cavities, molecule has {_molecule?.NumberOfAtoms ?? 0} atoms");
        }

        #endregion

        #region Public Properties

 /// <summary>
 /// Gets or sets the current property type to map.
 /// </summary>
        public PropertyType CurrentProperty
        {
            get => _currentProperty;
            set
            {
                if (_currentProperty != value)
                {
                    _currentProperty = value;
                    ClearCache();
                    Debug.WriteLine($"[PropertyMapper] CurrentProperty changed to: {value}");
                }
            }
        }

 /// <summary>
 /// Gets or sets the search radius for finding nearby atoms.
 /// </summary>
        public float SearchRadius
        {
            get => _searchRadius;
            set
            {
                if (_searchRadius != value && value > 0)
                {
                    _searchRadius = value;
                    ClearCache();
                    Debug.WriteLine($"[PropertyMapper] SearchRadius changed to: {value:F2}");
                }
            }
        }

 /// <summary>
 /// Gets or sets whether to use weighted average for property interafterlation.
 /// </summary>
        public bool UseWeightedAverage
        {
            get => _useWeightedAverage;
            set => _useWeightedAverage = value;
        }

 /// <summary>
 /// Gets or sets the fallback value used when no nearby atoms are found.
 /// </summary>
        public float FallbackValue
        {
            get => _fallbackValue;
            set => _fallbackValue = value;
        }

 /// <summary>
 /// Gets or sets the fallback color used when no nearby atoms are found.
 /// </summary>
        public Microsoft.Xna.Framework.Color FallbackColor
        {
            get => _fallbackColor;
            set => _fallbackColor = value;
        }

        #endregion

        #region Buffer Management

 /// <summary>
 /// Retrieves a thread-local buffer for storing nearby atom data.
 /// </summary>
 /// <returns>A list for storing NearbyAtom structures.</returns>
        private List<NearbyAtom> GetBuffer()
        {
            var buffer = _nearbyAtomsBufferLocal.Value;

            lock (_bufferLock)
            {
                if (!_allBuffers.Contains(buffer))
                {
                    _allBuffers.Add(buffer);
                }
            }

            return buffer;
        }

 /// <summary>
 /// Clears all thread-local buffers and releases resources.
 /// </summary>
        private void ClearAllBuffers()
        {
            try
            {
                try
                {
                    foreach (var buffer in _nearbyAtomsBufferLocal.Values)
                    {
                        try { buffer?.Clear(); }
                        catch { /* Ignore errors when clearing */ }
                    }
                }
                catch
                {
                    lock (_bufferLock)
                    {
                        foreach (var buffer in _allBuffers)
                        {
                            try { buffer?.Clear(); }
                            catch { /* Ignore errors when clearing */ }
                        }
                        _allBuffers.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PropertyMapper] Error clearing buffers: {ex.Message}");
            }
        }

        #endregion

        #region Main Methods

 /// <summary>
 /// Applies the specified property to all cavities asynchronously.
 /// </summary>
 /// <param name="propertyType">The property type to map.</param>
 /// <param name="cancellationToken">Cancellation token for the operation.</param>
 /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ApplyPropertyToCavitiesAsync(
            PropertyType propertyType,
            CancellationToken cancellationToken = default)
        {
            Debug.WriteLine($"[PropertyMapper] ========== APPLY PROPERTY START ==========");
            Debug.WriteLine($"[PropertyMapper] PropertyType: {propertyType}");
            Debug.WriteLine($"[PropertyMapper] Cavities count: {_cavities?.Count ?? 0}");

            cancellationToken.ThrowIfCancellationRequested();

            _totalCavitiesProcessed = 0;
            _totalVerticesProcessed = 0;
            _totalErrors = 0;

            CurrentProperty = propertyType;

            if (propertyType == PropertyType.None)
            {
                Debug.WriteLine("[PropertyMapper] PropertyType is None - resetting colors");
                ResetCavityColors();
                return;
            }

            if (_cavities == null || _cavities.Count == 0)
            {
                Debug.WriteLine("[PropertyMapper] No cavities to process!");
                return;
            }

            string propertyName = GetPropertyTypeName(propertyType);
            string msg = $"Applying {propertyName} to {_cavities.Count} cavities...";
            MainForm.Instance?.SetStatusSafe($"INFO: {msg}", "", "ℹ️");

            if (MainForm.Instance != null)
            {
                var activeDoc = MainForm.Instance.GetActiveDocument();
                if (activeDoc != null)
                {
                    var detector = activeDoc.GetCavityDetector();
                    if (detector != null)
                    {
                        var scanner = detector.Scanner;
                        bool useSpatialData = scanner != null && scanner.SpatialDataReady && scanner.Atoms != null;

                        BuildCacheFromScanner(propertyType);

                        Debug.WriteLine($"[PropertyMapper] Cache built: {_cachedAtomCount} atoms, useSpatialData={useSpatialData}");

                        if (useSpatialData)
                        {
                            Debug.WriteLine("[PropertyMapper] Using OPTIMIZED spatial search (BinarySearch on X)");
                            await ApplyPropertyWithSpatialDataAsync(propertyType, scanner, cancellationToken);
                        }
                        else
                        {
                            Debug.WriteLine("[PropertyMapper] Using LEGACY search (fallback)");
                            await ApplyPropertyLegacyAsync(propertyType, cancellationToken);
                        }

                        ClearAllBuffers();

                        Debug.WriteLine($"[PropertyMapper] ========== APPLY PROPERTY COMPLETE ==========");
                        Debug.WriteLine($"[PropertyMapper] Processed: {_totalCavitiesProcessed}/{_cavities.Count} cavities, {_totalVerticesProcessed} vertices, {_totalErrors} errors");

                        string resultMsg = $"SUCCESS: Applied {propertyName} to {_totalCavitiesProcessed}/{_cavities.Count} cavities, {_totalVerticesProcessed:N0} vertices";
                        if (_totalErrors > 0)
                        {
                            resultMsg += $", {_totalErrors} errors";
                        }

                        MainForm.Instance?.SetStatusSafe($"INFO: {resultMsg}", "", "ℹ️");

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                }
            }
        }

 /// <summary>
 /// Resets the property mapper to its initial state.
 /// </summary>
        public void Reset()
        {
            ClearCache();
            _cachedPropertyValues = null;
            _cachedAtomPositions = null;
            _cachedAtomCount = -1;
            _cachedPropertyType = -1;

            ResetCavityColors();

            Debug.WriteLine("[PropertyMapper] Reset complete");
        }

 /// <summary>
 /// Resets the colors of all cavities to their default state.
 /// </summary>
        public void ResetCavityColors()
        {
            Debug.WriteLine($"[PropertyMapper] ResetCavityColors: {_cavities?.Count ?? 0} cavities");
            foreach (var cavity in _cavities)
            {
                if (cavity != null)
                {
                    cavity.VertexColors4 = null;
                    cavity.LastMappedProperty = PropertyType.None;
                }
            }
        }

        #endregion

        #region Spatial Data Application (Optimized)

 /// <summary>
 /// Applies property mapping using optimized spatial search with BinarySearch on X coordinates.
 /// </summary>
 /// <param name="propertyType">The property type to map.</param>
 /// <param name="scanner">The VoxelScanner containing spatial data.</param>
 /// <param name="cancellationToken">Cancellation token for the operation.</param>
 /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ApplyPropertyWithSpatialDataAsync(
            PropertyType propertyType,
            VoxelScanner scanner,
            CancellationToken cancellationToken = default)
        {
            int maxParallel = Math.Min(Environment.ProcessorCount, _cavities.Count);
            Debug.WriteLine($"[PropertyMapper] Using {maxParallel} parallel tasks with spatial data");

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallel,
                CancellationToken = cancellationToken
            };

            await Task.Run(() =>
            {
                Parallel.For(0, _cavities.Count, options, (i, state) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var cavity = _cavities[i];
                        if (cavity == null) return;
                        if (!cavity.TrianglesGenerated) return;
                        if (cavity.Vertices == null || cavity.Vertices.Count == 0) return;

                        if (cavity.VertexColors4 != null &&
                            cavity.VertexColors4.Length == cavity.Vertices.Count &&
                            cavity.LastMappedProperty == propertyType)
                        {
                            return;
                        }

                        ApplyPropertyToCavitySpatial(cavity, propertyType, scanner);

                        lock (_debugLock)
                        {
                            _totalCavitiesProcessed++;
                            _totalVerticesProcessed += cavity.Vertices?.Count ?? 0;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[PropertyMapper] ERROR processing cavity {i}: {ex.Message}");
                        lock (_debugLock) _totalErrors++;
                    }
                });
            }, cancellationToken);
        }

 /// <summary>
 /// Applies property mapping to a single cavity using spatial search optimization.
 /// </summary>
 /// <param name="cavity">The cavity to map properties onto.</param>
 /// <param name="propertyType">The property type to map.</param>
 /// <param name="scanner">The VoxelScanner containing spatial data.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyPropertyToCavitySpatial(Cavity cavity, PropertyType propertyType, VoxelScanner scanner)
        {
            if (cavity == null || cavity.Vertices == null || cavity.Vertices.Count == 0)
                return;

            int vertCount = cavity.Vertices.Count;

            if (vertCount > MAX_SAFE_VERTICES)
            {
                Debug.WriteLine($"[PropertyMapper] Cavity {cavity.Id} has {vertCount:N0} vertices. Property mapping may be slow.");
            }

            var positions = cavity.Vertices;
            var colors = new XnaVector4[vertCount];

            var cachedValues = _cachedPropertyValues;
            float fallback = _fallbackValue;
            float radius = _searchRadius;
            float radiusSq = radius * radius;
            bool useWeighted = _useWeightedAverage;
            float contrast = GetContrast(propertyType);
            var managerPropertyType = (ColorManager.PropertyType)propertyType;

 // Set dynamic range for the property based on molecule data
            SetDynamicRange(propertyType);

            XnaVector4 fallbackColor4 = new XnaVector4(
                _fallbackColor.R / 255f,
                _fallbackColor.G / 255f,
                _fallbackColor.B / 255f,
                _fallbackColor.A / 255f);

            var nearbyBuffer = GetBuffer();

            int processedVertices = 0;
            int verticesWithNoNearby = 0;

            for (int i = 0; i < vertCount; i++)
            {
                SysVector3 pos = positions[i];

                FindNearbyAtomsFastSpatial(pos, radius, radiusSq, scanner, nearbyBuffer);

                if (nearbyBuffer.Count == 0)
                {
                    verticesWithNoNearby++;
                    colors[i] = fallbackColor4;
                    continue;
                }

                float value;
                if (useWeighted)
                {
                    value = GetWeightedAveragePropertyFast(nearbyBuffer, cachedValues, fallback);
                }
                else
                {
                    value = GetNearestPropertyFast(nearbyBuffer, cachedValues, fallback);
                }

                colors[i] = ColorManager.ValueToVector4(value, managerPropertyType, contrast, 1f);
                processedVertices++;
            }

            cavity.VertexColors4 = colors;
            cavity.LastMappedProperty = propertyType;

            Debug.WriteLine($"[PropertyMapper] Cavity {cavity.Id}: Spatial - processed {processedVertices}/{vertCount} vertices, {verticesWithNoNearby} fallback, property={propertyType}");
        }

 /// <summary>
 /// Finds nearby atoms using optimized spatial search with BinarySearch on X coordinates.
 /// </summary>
 /// <param name="aftersition">The aftersition to search around.</param>
 /// <param name="radius">The search radius.</param>
 /// <param name="radiusSq">The squared search radius for distance comparisons.</param>
 /// <param name="scanner">The VoxelScanner containing spatial data.</param>
 /// <param name="output">The list to populate with nearby atoms.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FindNearbyAtomsFastSpatial(
            SysVector3 position,
            float radius,
            float radiusSq,
            VoxelScanner scanner,
            List<NearbyAtom> output)
        {
            if (scanner == null || !scanner.SpatialDataReady || _cachedAtomPositions == null)
            {
                FindNearbyAtomsLegacy(position, radiusSq, _cachedAtomPositions, _cachedAtomCount, output);
                return;
            }

            float px = position.X, py = position.Y, pz = position.Z;
            int count = _cachedAtomCount;

            var sortedX = scanner.SortedX;
            var sortedIndicesX = scanner.SortedIndicesX;

            if (sortedX == null || sortedIndicesX == null)
            {
                FindNearbyAtomsLegacy(position, radiusSq, _cachedAtomPositions, _cachedAtomCount, output);
                return;
            }

            int startIdx = Array.BinarySearch(sortedX, px - radius);
            if (startIdx < 0) startIdx = ~startIdx;
            int endIdx = Array.BinarySearch(sortedX, px + radius);
            if (endIdx < 0) endIdx = ~endIdx;
            endIdx = Math.Min(count, endIdx);

            output.Clear();

            if (startIdx >= endIdx) return;

            var atomPositions = _cachedAtomPositions;

            for (int si = startIdx; si < endIdx; si++)
            {
                int i = sortedIndicesX[si];
                if (i < 0 || i >= atomPositions.Length) continue;

                ref var atomPos = ref atomPositions[i];

                float dx = px - atomPos.X;
                float dy = py - atomPos.Y;
                float dz = pz - atomPos.Z;
                float distSq = dx * dx + dy * dy + dz * dz;

                if (distSq > radiusSq) continue;
                if (distSq < MIN_DISTANCE_SQ) continue;

                float distance = MathF.Sqrt(distSq);
                output.Add(new NearbyAtom(i, distance));
            }

            if (output.Count > 1)
            {
                output.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            }
        }

        #endregion

        #region Legacy Application (Fallback)

 /// <summary>
 /// Applies property mapping using legacy fallback search (linear scan).
 /// </summary>
 /// <param name="propertyType">The property type to map.</param>
 /// <param name="cancellationToken">Cancellation token for the operation.</param>
 /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ApplyPropertyLegacyAsync(
            PropertyType propertyType,
            CancellationToken cancellationToken = default)
        {
            int maxParallel = Math.Min(Environment.ProcessorCount, _cavities.Count);
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallel,
                CancellationToken = cancellationToken
            };

            await Task.Run(() =>
            {
                Parallel.For(0, _cavities.Count, options, (i, state) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var cavity = _cavities[i];
                        if (cavity == null) return;
                        if (!cavity.TrianglesGenerated) return;
                        if (cavity.Vertices == null || cavity.Vertices.Count == 0) return;

                        if (cavity.VertexColors4 != null &&
                            cavity.VertexColors4.Length == cavity.Vertices.Count)
                        {
                            return;
                        }

                        ApplyPropertyToCavityLegacy(cavity, propertyType);

                        lock (_debugLock)
                        {
                            _totalCavitiesProcessed++;
                            _totalVerticesProcessed += cavity.Vertices?.Count ?? 0;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[PropertyMapper] ERROR processing cavity {i}: {ex.Message}");
                        lock (_debugLock) _totalErrors++;
                    }
                });
            }, cancellationToken);
        }

 /// <summary>
 /// Applies property mapping to a single cavity using legacy fallback method.
 /// </summary>
 /// <param name="cavity">The cavity to map properties onto.</param>
 /// <param name="propertyType">The property type to map.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyPropertyToCavityLegacy(Cavity cavity, PropertyType propertyType)
        {
            if (cavity == null || cavity.Vertices == null || cavity.Vertices.Count == 0)
                return;

            int vertCount = cavity.Vertices.Count;

            if (vertCount > MAX_SAFE_VERTICES)
            {
                Debug.WriteLine($"[PropertyMapper] Cavity {cavity.Id} has {vertCount:N0} vertices. Property mapping may be slow.");
            }

            var positions = cavity.Vertices;
            var colors = new XnaVector4[vertCount];

            var cachedPositions = _cachedAtomPositions;
            var cachedValues = _cachedPropertyValues;
            float radiusSq = _searchRadius * _searchRadius;
            bool useWeighted = _useWeightedAverage;
            float fallback = _fallbackValue;
            float contrast = GetContrast(propertyType);
            var managerPropertyType = (ColorManager.PropertyType)propertyType;

 // Set dynamic range for the property based on molecule data
            SetDynamicRange(propertyType);

            XnaVector4 fallbackColor4 = new XnaVector4(
                _fallbackColor.R / 255f,
                _fallbackColor.G / 255f,
                _fallbackColor.B / 255f,
                _fallbackColor.A / 255f);

            var nearbyBuffer = GetBuffer();

            for (int i = 0; i < vertCount; i++)
            {
                SysVector3 pos = positions[i];

                nearbyBuffer.Clear();
                FindNearbyAtomsLegacy(pos, radiusSq, cachedPositions, _cachedAtomCount, nearbyBuffer);

                if (nearbyBuffer.Count == 0)
                {
                    colors[i] = fallbackColor4;
                    continue;
                }

                float value;
                if (useWeighted)
                {
                    value = GetWeightedAveragePropertyFast(nearbyBuffer, cachedValues, fallback);
                }
                else
                {
                    value = GetNearestPropertyFast(nearbyBuffer, cachedValues, fallback);
                }

                colors[i] = ColorManager.ValueToVector4(value, managerPropertyType, contrast, 1f);
            }

            cavity.VertexColors4 = colors;
            cavity.LastMappedProperty = propertyType;
        }

 /// <summary>
 /// Finds nearby atoms using legacy linear scan fallback method.
 /// </summary>
 /// <param name="aftersition">The aftersition to search around.</param>
 /// <param name="radiusSq">The squared search radius.</param>
 /// <param name="atomAftersitions">Array of atom aftersitions.</param>
 /// <param name="atomCount">Number of atoms to search.</param>
 /// <param name="output">The list to populate with nearby atoms.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FindNearbyAtomsLegacy(
            SysVector3 position,
            float radiusSq,
            SysVector3[] atomPositions,
            int atomCount,
            List<NearbyAtom> output)
        {
            if (atomPositions == null || atomCount <= 0) return;

            float px = position.X, py = position.Y, pz = position.Z;

            for (int i = 0; i < atomCount; i++)
            {
                if (i >= atomPositions.Length) break;

                ref var atomPos = ref atomPositions[i];
                float dx = px - atomPos.X;
                float dy = py - atomPos.Y;
                float dz = pz - atomPos.Z;
                float distSq = dx * dx + dy * dy + dz * dz;

                if (distSq > MIN_DISTANCE_SQ && distSq <= radiusSq)
                {
                    float distance = MathF.Sqrt(distSq);
                    output.Add(new NearbyAtom(i, distance));
                }
            }

            if (output.Count > 1)
            {
                output.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            }
        }

        #endregion

        #region Property Value Helpers

 /// <summary>
 /// Computes the weighted average of property values from nearby atoms.
 /// </summary>
 /// <param name="nearbyAtoms">List of nearby atoms with distances.</param>
 /// <param name="propertyValues">Array of property values indexed by atom index.</param>
 /// <param name="fallback">Fallback value to return if no valid data exists.</param>
 /// <returns>The weighted average property value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetWeightedAveragePropertyFast(
            List<NearbyAtom> nearbyAtoms,
            float[] propertyValues,
            float fallback)
        {
            if (nearbyAtoms.Count == 0 || propertyValues == null) return fallback;

            float weightedSum = 0;
            float weightSum = 0;

            foreach (var nearby in nearbyAtoms)
            {
                if (nearby.AtomIndex < 0 || nearby.AtomIndex >= propertyValues.Length) continue;
                float value = propertyValues[nearby.AtomIndex];
                float weight = 1.0f / (nearby.Distance + 0.1f);
                weightedSum += value * weight;
                weightSum += weight;
            }

            return weightSum > 0 ? weightedSum / weightSum : fallback;
        }

 /// <summary>
 /// Gets the property value from the nearest atom.
 /// </summary>
 /// <param name="nearbyAtoms">List of nearby atoms sorted by distance.</param>
 /// <param name="propertyValues">Array of property values indexed by atom index.</param>
 /// <param name="fallback">Fallback value to return if no valid data exists.</param>
 /// <returns>The property value of the nearest atom.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetNearestPropertyFast(
            List<NearbyAtom> nearbyAtoms,
            float[] propertyValues,
            float fallback)
        {
            if (nearbyAtoms.Count == 0 || propertyValues == null) return fallback;

            int atomIndex = nearbyAtoms[0].AtomIndex;
            if (atomIndex < 0 || atomIndex >= propertyValues.Length) return fallback;

            return propertyValues[atomIndex];
        }

 /// <summary>
 /// Gets the contrast multiplier for the specified property type.
 /// </summary>
 /// <param name="propertyType">The property type.</param>
 /// <returns>The contrast multiplier.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetContrast(PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.Charge:
                    return 4.2f;
                case PropertyType.Hydrophobicity:
                    return 1.5f;
                default:
                    return 1.0f;
            }
        }

 /// <summary>
 /// Gets the property value for a given atom.
 /// </summary>
 /// <param name="atom">The atom to query.</param>
 /// <param name="propertyType">The property type to retrieve.</param>
 /// <returns>The property value.</returns>
        private float GetPropertyValue(Molecule.AtomSimple atom, PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.Charge:
                    return atom.charge;
                case PropertyType.Hydrophobicity:
                    return atom.Hydrophobicity;
                case PropertyType.AtomicNumber:
                    return atom.AtomicNumber;
                case PropertyType.VdWRadius:
                    float radius = VdwParameters.GetRadius(atom.AtomicNumber);
                    return radius > 0 ? radius : _fallbackValue;
                case PropertyType.Occupancy:
                    return atom.occupancy;
                default:
                    return _fallbackValue;
            }
        }

 /// <summary>
 /// Gets the property value for an atom by its index in the asymmetric unit.
 /// </summary>
 /// <param name="atomIndex">The atom index.</param>
 /// <param name="propertyType">The property type to retrieve.</param>
 /// <returns>The property value.</returns>
        private float GetPropertyValueAsymmetric(int atomIndex, PropertyType propertyType)
        {
            var atom = _molecule.atom[atomIndex];

            switch (propertyType)
            {
                case PropertyType.Charge:
                    return atom.charge;
                case PropertyType.Hydrophobicity:
                    return atom.Hydrophobicity;
                case PropertyType.AtomicNumber:
                    return atom.AtomicNumber;
                case PropertyType.VdWRadius:
                    float radius = VdwParameters.GetRadius(atom.AtomicNumber);
                    return radius > 0 ? radius : _fallbackValue;
                case PropertyType.Occupancy:
                    return atom.occupancy;
                default:
                    return _fallbackValue;
            }
        }

 /// <summary>
 /// Sets the dynamic range for property visualization based on molecule data.
 /// </summary>
 /// <param name="propertyType">The property type to configure.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetDynamicRange(PropertyType propertyType)
        {
            var managerPropertyType = (ColorManager.PropertyType)propertyType;

            switch (propertyType)
            {
                case PropertyType.Charge:
                    float minCharge = _molecule.ChargeMin;
                    float maxCharge = _molecule.ChargeMax;
                    if (minCharge < maxCharge)
                    {
                        ColorManager.SetDynamicRange(ColorManager.PropertyType.Charge, minCharge, maxCharge);
                    }
                    else
                    {
                        var range_ = ColorManager.GetPropertyRange(ColorManager.PropertyType.Charge);
                        ColorManager.SetDynamicRange(ColorManager.PropertyType.Charge, range_.min, range_.max);
                    }
                    break;

                case PropertyType.Hydrophobicity:
                    float minHyd = _molecule.HydrophobicityMin;
                    float maxHyd = _molecule.HydrophobicityMax;
                    if (minHyd < maxHyd)
                    {
                        ColorManager.SetDynamicRange(ColorManager.PropertyType.Hydrophobicity, minHyd, maxHyd);
                    }
                    else
                    {
                        var range_ = ColorManager.GetPropertyRange(ColorManager.PropertyType.Hydrophobicity);
                        ColorManager.SetDynamicRange(ColorManager.PropertyType.Hydrophobicity, range_.min, range_.max);
                    }
                    break;

                default:
                    var range = ColorManager.GetPropertyRange(managerPropertyType);
                    ColorManager.SetDynamicRange(managerPropertyType, range.min, range.max);
                    break;
            }
        }

        #endregion

        #region Cache Building

 /// <summary>
 /// Builds the property cache from the VoxelScanner data.
 /// </summary>
 /// <param name="propertyType">The property type to cache.</param>
        private void BuildCacheFromScanner(PropertyType propertyType)
        {
            Debug.WriteLine($"[PropertyMapper] ========== BUILD CACHE START ==========");
            Debug.WriteLine($"[PropertyMapper] PropertyType: {propertyType}");

            if (MainForm.Instance != null)
            {
                var activeDoc = MainForm.Instance.GetActiveDocument();
                if (activeDoc != null)
                {
                    var detector = activeDoc.GetCavityDetector();

                    if (detector == null || detector.Scanner == null)
                    {
                        Debug.WriteLine("[PropertyMapper] No scanner available - using fallback");
                        BuildFallbackCache(propertyType);
                        return;
                    }

                    var scannerAtoms = detector.Scanner.Atoms;
                    if (scannerAtoms == null || scannerAtoms.Length == 0)
                    {
                        Debug.WriteLine("[PropertyMapper] Scanner has no atoms - using fallback");
                        BuildFallbackCache(propertyType);
                        return;
                    }

                    int atomCount = scannerAtoms.Length;
                    Debug.WriteLine($"[PropertyMapper] Scanner atoms count: {atomCount}");

                    if (_cachedAtomCount == atomCount && _cachedPropertyType == (int)propertyType)
                    {
                        Debug.WriteLine($"[PropertyMapper] Cache already built for {propertyType} with {atomCount} atoms");
                        return;
                    }

                    Debug.WriteLine($"[PropertyMapper] Building cache from scanner: {atomCount} atoms");

                    _cachedPropertyValues = new float[atomCount];
                    _cachedAtomPositions = new SysVector3[atomCount];
                    _cachedAtomCount = atomCount;
                    _cachedPropertyType = (int)propertyType;

 // Build property map from asymmetric unit and symmetry equivalents
                    var propertyMap = BuildPropertyMap(propertyType);

                    int mappedCount = 0;
                    int fallbackCount = 0;

                    for (int i = 0; i < atomCount; i++)
                    {
                        var atom = scannerAtoms[i];

                        float fx = atom.FracX - (float)Math.Floor(atom.FracX);
                        float fy = atom.FracY - (float)Math.Floor(atom.FracY);
                        float fz = atom.FracZ - (float)Math.Floor(atom.FracZ);
                        if (fx < 0) fx += 1.0f;
                        if (fy < 0) fy += 1.0f;
                        if (fz < 0) fz += 1.0f;

                        int keyX = (int)Math.Round(fx * 100000f);
                        int keyY = (int)Math.Round(fy * 100000f);
                        int keyZ = (int)Math.Round(fz * 100000f);

                        float bestDist = float.MaxValue;
                        float bestValue = _fallbackValue;
                        bool found = false;

 // Search in a 3x3x3 neighborhood for matching fractional coordinates
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                for (int dz = -1; dz <= 1; dz++)
                                {
                                    var key = (keyX + dx, keyY + dy, keyZ + dz);
                                    if (propertyMap.TryGetValue(key, out float value))
                                    {
                                        float dist = Math.Abs(dx) + Math.Abs(dy) + Math.Abs(dz);
                                        if (dist < bestDist)
                                        {
                                            bestDist = dist;
                                            bestValue = value;
                                            found = true;
                                        }
                                    }
                                }
                            }
                        }

                        if (found)
                        {
                            _cachedPropertyValues[i] = bestValue;
                            mappedCount++;
                        }
                        else
                        {
                            _cachedPropertyValues[i] = _fallbackValue;
                            fallbackCount++;
                        }

                        _cachedAtomPositions[i] = new SysVector3(atom.X, atom.Y, atom.Z);
                    }

                    Debug.WriteLine($"[PropertyMapper] Cache built: {atomCount} atoms, mapped={mappedCount}, fallback={fallbackCount}");

                    if (fallbackCount > atomCount / 2)
                    {
                        Debug.WriteLine($"[PropertyMapper] WARNING: {fallbackCount}/{atomCount} atoms used fallback values for {propertyType}");
                    }
                }
            }
        }

 /// <summary>
 /// Builds a property map from asymmetric unit and symmetry equivalent atoms.
 /// </summary>
 /// <param name="propertyType">The property type to map.</param>
 /// <returns>A dictionary mapping fractional coordinates to property values.</returns>
        private Dictionary<(int x, int y, int z), float> BuildPropertyMap(PropertyType propertyType)
        {
            const float scale = 100000f;
            var propertyMap = new Dictionary<(int x, int y, int z), float>();

 // Add symmetry equivalents first
            if (_molecule.SymetryEquivalents != null && _molecule.SymetryEquivalents.Count > 0)
            {
                foreach (var atom in _molecule.SymetryEquivalents)
                {
                    float value = GetPropertyValue(atom, propertyType);
                    float fx = atom.x - (float)Math.Floor(atom.x);
                    float fy = atom.y - (float)Math.Floor(atom.y);
                    float fz = atom.z - (float)Math.Floor(atom.z);
                    if (fx < 0) fx += 1.0f;
                    if (fy < 0) fy += 1.0f;
                    if (fz < 0) fz += 1.0f;

                    int keyX = (int)Math.Round(fx * scale);
                    int keyY = (int)Math.Round(fy * scale);
                    int keyZ = (int)Math.Round(fz * scale);
                    propertyMap[(keyX, keyY, keyZ)] = value;
                }
            }

 // Add asymmetric part if no symmetry equivalents found
            if (propertyMap.Count == 0 && _molecule.AsymmetricPart != null)
            {
                foreach (var atom in _molecule.AsymmetricPart)
                {
                    float value = GetPropertyValue(atom, propertyType);
                    float fx = atom.x - (float)Math.Floor(atom.x);
                    float fy = atom.y - (float)Math.Floor(atom.y);
                    float fz = atom.z - (float)Math.Floor(atom.z);
                    if (fx < 0) fx += 1.0f;
                    if (fy < 0) fy += 1.0f;
                    if (fz < 0) fz += 1.0f;

                    int keyX = (int)Math.Round(fx * scale);
                    int keyY = (int)Math.Round(fy * scale);
                    int keyZ = (int)Math.Round(fz * scale);
                    propertyMap[(keyX, keyY, keyZ)] = value;
                }
            }

            return propertyMap;
        }

 /// <summary>
 /// Builds a fallback cache using the molecule's symmetry equivalents.
 /// </summary>
 /// <param name="propertyType">The property type to cache.</param>
        private void BuildFallbackCache(PropertyType propertyType)
        {
            Debug.WriteLine($"[PropertyMapper] BuildFallbackCache: {propertyType}");

            if (_molecule.SymetryEquivalents == null || _molecule.SymetryEquivalents.Count == 0)
            {
                BuildAsymmetricCache(propertyType);
                return;
            }

            int atomCount = _molecule.SymetryEquivalents.Count;

            if (_cachedAtomCount == atomCount && _cachedPropertyType == (int)propertyType)
            {
                Debug.WriteLine($"[PropertyMapper] Fallback cache already built for {propertyType} with {atomCount} atoms");
                return;
            }

            Debug.WriteLine($"[PropertyMapper] Building fallback cache: {atomCount} atoms");

            _cachedPropertyValues = new float[atomCount];
            _cachedAtomPositions = new SysVector3[atomCount];
            _cachedAtomCount = atomCount;
            _cachedPropertyType = (int)propertyType;

            Matrix U = Matrix.Transpose(_molecule.FractionaltoCarttesianMatrix(0, 0, 0));

            for (int i = 0; i < atomCount; i++)
            {
                var atom = _molecule.SymetryEquivalents[i];
                Vector3 cart = Vector3.Transform(new Vector3(atom.x, atom.y, atom.z), U);
                _cachedAtomPositions[i] = new SysVector3(cart.X, cart.Y, cart.Z);
                _cachedPropertyValues[i] = GetPropertyValue(atom, propertyType);
            }

            Debug.WriteLine($"[PropertyMapper] Fallback cache built: {atomCount} atoms");
        }

 /// <summary>
 /// Builds a cache using only the asymmetric unit atoms.
 /// </summary>
 /// <param name="propertyType">The property type to cache.</param>
        private void BuildAsymmetricCache(PropertyType propertyType)
        {
            Debug.WriteLine($"[PropertyMapper] BuildAsymmetricCache: {propertyType}");

            if (_molecule.atom == null || _molecule.NumberOfAtoms == 0)
            {
                Debug.WriteLine("[PropertyMapper] No atoms in molecule!");
                return;
            }

            int atomCount = _molecule.NumberOfAtoms;
            Debug.WriteLine($"[PropertyMapper] Building asymmetric cache: {atomCount} atoms");

            _cachedPropertyValues = new float[atomCount];
            _cachedAtomPositions = new SysVector3[atomCount];
            _cachedAtomCount = atomCount;
            _cachedPropertyType = (int)propertyType;

            for (int i = 0; i < atomCount; i++)
            {
                var atom = _molecule.atom[i];
                _cachedAtomPositions[i] = new SysVector3(atom.Coordinate.X, atom.Coordinate.Y, atom.Coordinate.Z);
                _cachedPropertyValues[i] = GetPropertyValueAsymmetric(i, propertyType);
            }

            Debug.WriteLine($"[PropertyMapper] Asymmetric cache built: {atomCount} atoms");
        }

 /// <summary>
 /// Clears the internal cache.
 /// </summary>
        private void ClearCache()
        {
            Debug.WriteLine("[PropertyMapper] Clearing cache");
            _cachedPropertyValues = null;
            _cachedAtomPositions = null;
            _cachedAtomCount = -1;
            _cachedPropertyType = -1;
        }

        #endregion

        #region Utility Methods

 /// <summary>
 /// Gets a dictionary mapping property types to their display names.
 /// </summary>
 /// <returns>A dictionary of property type names.</returns>
        public Dictionary<PropertyType, string> GetPropertyNames()
        {
            return new Dictionary<PropertyType, string>
            {
                { PropertyType.None, "None" },
                { PropertyType.Charge, "Charge" },
                { PropertyType.Hydrophobicity, "Hydrophobicity" },
                { PropertyType.AtomicNumber, "Atomic Number" },
                { PropertyType.VdWRadius, "VdW Radius" },
                { PropertyType.Occupancy, "Occupancy" }
            };
        }

 /// <summary>
 /// Gets the display name for a property type.
 /// </summary>
 /// <param name="type">The property type.</param>
 /// <returns>The display name.</returns>
        public static string GetPropertyTypeName(PropertyType type)
        {
            switch (type)
            {
                case PropertyType.None: return "None";
                case PropertyType.Charge: return "Charge";
                case PropertyType.Hydrophobicity: return "Hydrophobicity";
                case PropertyType.AtomicNumber: return "Atomic Number";
                case PropertyType.VdWRadius: return "VdW Radius";
                case PropertyType.Occupancy: return "Occupancy";
                default: return "Unknown";
            }
        }

        #endregion

        #region IDisposable

 /// <summary>
 /// Disafterses the property mapper and releases all resources.
 /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

 /// <summary>
 /// Protected disafterse implementation.
 /// </summary>
 /// <param name="disaftersing">True if called from Disafterse, false if from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                ClearAllBuffers();
                try
                {
                    _nearbyAtomsBufferLocal?.Dispose();
                }
                catch
                {
 // Ignore errors during disafterse
                }
                ClearCache();
            }

            _disposed = true;
        }

        #endregion

        #region Helper Struct

 /// <summary>
 /// Represents a nearby atom with its index and distance.
 /// </summary>
        private struct NearbyAtom
        {
 /// <summary>The index of the atom in the cache.</summary>
            public int AtomIndex;
 /// <summary>The distance from the query afterint.</summary>
            public float Distance;

 /// <summary>
 /// Initializes a new NearbyAtom.
 /// </summary>
 /// <param name="atomIndex">The atom index.</param>
 /// <param name="distance">The distance from the query afterint.</param>
            public NearbyAtom(int atomIndex, float distance)
            {
                AtomIndex = atomIndex;
                Distance = distance;
            }
        }

        #endregion
    }
}