// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity detection / analysis module: ColorManager.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;
using Color = Microsoft.Xna.Framework.Color;

namespace Tal.Cavity
{
 /// <summary>
 /// Maps property values (charge, hydrophobicity, atomic number, etc.) to colors.
 /// Uses precomputed palettes with caching for high performance.
 /// </summary>
    public static class ColorManager
    {
        #region Enums

 /// <summary>
 /// Types of properties that can be mapped to colors.
 /// </summary>
        public enum PropertyType
        {
            None = 0,
            Charge = 1,
            Hydrophobicity = 2,
            AtomicNumber = 3,
            VdWRadius = 4,
            Occupancy = 5
        }

        #endregion

        #region Palettes

 // Charge palette: blue (negative) -> white -> red (aftersitive)
        private static readonly Vector4[] _chargePalette = new Vector4[]
        {
            new Vector4(0.00f, 0.00f, 0.30f, 1f),  // -1.0  - dark blue
            new Vector4(0.00f, 0.00f, 0.45f, 1f),  // -0.85 - blue
            new Vector4(0.00f, 0.10f, 0.65f, 1f),  // -0.70 - medium blue
            new Vector4(0.00f, 0.30f, 0.80f, 1f),  // -0.55 - light blue
            new Vector4(0.20f, 0.55f, 0.95f, 1f),  // -0.40 - azure
            new Vector4(0.50f, 0.80f, 1.00f, 1f),  // -0.25 - light azure
            new Vector4(0.80f, 0.95f, 1.00f, 1f),  // -0.10 - very light blue
            new Vector4(1.00f, 1.00f, 1.00f, 1f),  //  0.00 - WHITE (neutral)
            new Vector4(1.00f, 1.00f, 0.85f, 1f),  //  0.10 - light yellow
            new Vector4(1.00f, 0.95f, 0.65f, 1f),  //  0.25 - yellow
            new Vector4(1.00f, 0.85f, 0.40f, 1f),  //  0.40 - orange
            new Vector4(1.00f, 0.65f, 0.20f, 1f),  //  0.55 - dark orange
            new Vector4(1.00f, 0.40f, 0.05f, 1f),  //  0.70 - red-orange
            new Vector4(1.00f, 0.15f, 0.00f, 1f),  //  0.85 - red
            new Vector4(0.80f, 0.00f, 0.00f, 1f),  //  1.0  - dark red
        };

 // Hydrophobicity palette: blue (hydrophilic) -> white -> red (hydrophobic)
        private static readonly Vector4[] _hydrophobicityPalette = new Vector4[]
        {
            new Vector4(0.00f, 0.00f, 0.50f, 1f),  // -1.0 - dark blue (hydrophilic)
            new Vector4(0.00f, 0.10f, 0.70f, 1f),  // -0.80
            new Vector4(0.00f, 0.35f, 0.85f, 1f),  // -0.60
            new Vector4(0.20f, 0.60f, 0.95f, 1f),  // -0.40
            new Vector4(0.55f, 0.85f, 1.00f, 1f),  // -0.20
            new Vector4(0.85f, 0.95f, 1.00f, 1f),  // -0.10
            new Vector4(1.00f, 1.00f, 1.00f, 1f),  //  0.00 - WHITE (neutral)
            new Vector4(1.00f, 1.00f, 0.85f, 1f),  //  0.10
            new Vector4(1.00f, 0.95f, 0.60f, 1f),  //  0.20
            new Vector4(1.00f, 0.85f, 0.35f, 1f),  //  0.40
            new Vector4(1.00f, 0.65f, 0.15f, 1f),  //  0.60
            new Vector4(1.00f, 0.40f, 0.00f, 1f),  //  0.80
            new Vector4(0.80f, 0.15f, 0.00f, 1f),  //  1.0 - dark red (hydrophobic)
        };

 // Atomic number palette
        private static readonly Vector4[] _atomicNumberPalette = new Vector4[]
        {
            new Vector4(0.50f, 0.50f, 0.50f, 1f),
            new Vector4(1.00f, 1.00f, 1.00f, 1f),
            new Vector4(0.75f, 0.75f, 0.75f, 1f),
            new Vector4(1.00f, 0.65f, 0.00f, 1f),
            new Vector4(0.40f, 0.40f, 0.40f, 1f),
            new Vector4(0.82f, 0.71f, 0.55f, 1f),
            new Vector4(0.40f, 0.40f, 0.40f, 1f),
            new Vector4(0.00f, 0.00f, 1.00f, 1f),
            new Vector4(1.00f, 0.00f, 0.00f, 1f),
            new Vector4(0.56f, 0.93f, 0.56f, 1f),
            new Vector4(0.00f, 1.00f, 0.00f, 1f),
        };

 // Van der Waals radius palette
        private static readonly Vector4[] _vdwRadiusPalette = new Vector4[]
        {
            new Vector4(0.00f, 0.00f, 1.00f, 1f),
            new Vector4(0.00f, 1.00f, 1.00f, 1f),
            new Vector4(0.00f, 1.00f, 0.00f, 1f),
            new Vector4(1.00f, 1.00f, 0.00f, 1f),
            new Vector4(1.00f, 0.00f, 0.00f, 1f),
        };

 // Occupancy palette
        private static readonly Vector4[] _occupancyPalette = new Vector4[]
        {
            new Vector4(1.00f, 0.00f, 0.00f, 1f),
            new Vector4(1.00f, 0.65f, 0.00f, 1f),
            new Vector4(1.00f, 1.00f, 0.00f, 1f),
            new Vector4(0.56f, 0.93f, 0.56f, 1f),
            new Vector4(0.00f, 1.00f, 0.00f, 1f),
        };

        #endregion

        #region Range and Cache

        private static float _currentMin = -0.5f;
        private static float _currentMax = 0.5f;
        private static bool _hasDynamicRange = false;
        private static PropertyType _currentPropertyType = PropertyType.None;
        private const float MIN_RANGE_WIDTH = 0.001f;

        private static Vector4[] _cachedChargeValues;
        private static Vector4[] _cachedHydrophobicityValues;
        private static Vector4[] _cachedAtomicNumberValues;
        private static Vector4[] _cachedVdwRadiusValues;
        private static Vector4[] _cachedOccupancyValues;

        private const int CACHE_RESOLUTION = 1024;
        private const float CACHE_STEP = 1.0f / (CACHE_RESOLUTION - 1);

        #endregion

        #region Static Constructor

        static ColorManager()
        {
            PrecomputeColorCache();
        }

        #endregion

        #region Cache Initialization

        private static void PrecomputeColorCache()
        {
            _cachedChargeValues = PrecomputePalette(_chargePalette);
            _cachedHydrophobicityValues = PrecomputePalette(_hydrophobicityPalette);
            _cachedAtomicNumberValues = PrecomputePalette(_atomicNumberPalette);
            _cachedVdwRadiusValues = PrecomputePalette(_vdwRadiusPalette);
            _cachedOccupancyValues = PrecomputePalette(_occupancyPalette);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector4[] PrecomputePalette(Vector4[] palette)
        {
            var result = new Vector4[CACHE_RESOLUTION];
            for (int i = 0; i < CACHE_RESOLUTION; i++)
            {
                float t = i * CACHE_STEP;
                result[i] = LerpColorFast(palette, t);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector4 LerpColorFast(Vector4[] palette, float t)
        {
            if (palette == null || palette.Length == 0)
                return new Vector4(0.5f, 0.5f, 0.5f, 1f);

            if (palette.Length == 1)
                return palette[0];

            float scaled = t * (palette.Length - 1);
            int index = (int)scaled;
            float frac = scaled - index;

            if (index >= palette.Length - 1)
                return palette[palette.Length - 1];

            Vector4 c1 = palette[index];
            Vector4 c2 = palette[index + 1];

            return new Vector4(
                c1.X + (c2.X - c1.X) * frac,
                c1.Y + (c2.Y - c1.Y) * frac,
                c1.Z + (c2.Z - c1.Z) * frac,
                1f
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector4[] GetPrecomputedPalette(PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.Charge: return _cachedChargeValues;
                case PropertyType.Hydrophobicity: return _cachedHydrophobicityValues;
                case PropertyType.AtomicNumber: return _cachedAtomicNumberValues;
                case PropertyType.VdWRadius: return _cachedVdwRadiusValues;
                case PropertyType.Occupancy: return _cachedOccupancyValues;
                default: return _cachedChargeValues;
            }
        }

        #endregion

        #region Range Management

 /// <summary>
 /// Gets the default range for a property type.
 /// </summary>
 /// <summary>
 /// Gets the default range for a property type.
 /// </summary>
        public static (float min, float max) GetPropertyRange(PropertyType propertyType)
        {
 // ⭐ If we have a dynamic range for this type - return it ⭐
            if (_hasDynamicRange && _currentPropertyType == propertyType)
                return (_currentMin, _currentMax);

            switch (propertyType)
            {
                case PropertyType.Charge: return (-0.5f, 0.5f);
                case PropertyType.Hydrophobicity: return (-0.5f, 0.5f);
                case PropertyType.AtomicNumber: return (1f, 10f);
                case PropertyType.VdWRadius: return (0.5f, 2.5f);
                case PropertyType.Occupancy: return (0f, 1f);
                default: return (-0.5f, 0.5f);
            }
        }
 /// <summary>
 /// Sets a dynamic range for color mapping.
 /// </summary>
        public static void SetDynamicRange(PropertyType propertyType, float min, float max)
        {
            _currentPropertyType = propertyType;
            float rangeWidth = max - min;

            if (rangeWidth < MIN_RANGE_WIDTH)
            {
                _currentMin = -0.5f;
                _currentMax = 0.5f;
                _hasDynamicRange = false;
            }
            else
            {
                if (propertyType == PropertyType.Charge || propertyType == PropertyType.Hydrophobicity)
                {
                    float absMax = Math.Max(Math.Abs(min), Math.Abs(max));
                    if (absMax < 0.001f) absMax = 0.5f;
                    _currentMin = -absMax;
                    _currentMax = absMax;
                }
                else
                {
                    _currentMin = min;
                    _currentMax = max;
                }
                _hasDynamicRange = true;
            }
        }

 /// <summary>
 /// Resets to the default range.
 /// </summary>
        public static void ResetDynamicRange()
        {
            _hasDynamicRange = false;
            _currentMin = -0.5f;
            _currentMax = 0.5f;
        }

 /// <summary>
 /// Gets the current range.
 /// </summary>
        public static (float min, float max) GetCurrentRange()
        {
            return (_currentMin, _currentMax);
        }

 /// <summary>
 /// Updates the current range.
 /// </summary>
        public static void UpdateRange(float min, float max)
        {
            if (_currentPropertyType == PropertyType.Charge || _currentPropertyType == PropertyType.Hydrophobicity)
            {
                float absMax = Math.Max(Math.Abs(min), Math.Abs(max));
                if (absMax < 0.001f) absMax = 0.5f;
                _currentMin = -absMax;
                _currentMax = absMax;
            }
            else
            {
                _currentMin = min;
                _currentMax = max;
            }
            _hasDynamicRange = true;
        }

        #endregion

        #region Labels

 /// <summary>
 /// Gets the minimum label for a property type.
 /// </summary>
        public static string GetMinLabel(PropertyType propertyType)
        {
            if (_hasDynamicRange && _currentPropertyType == propertyType)
                return FormatValue(_currentMin, propertyType);

            switch (propertyType)
            {
                case PropertyType.Charge: return "-0.50";
                case PropertyType.Hydrophobicity: return "-0.50";
                case PropertyType.AtomicNumber: return "Low Z";
                case PropertyType.VdWRadius: return "Small";
                case PropertyType.Occupancy: return "0.0";
                default: return "";
            }
        }

 /// <summary>
 /// Gets the middle label for a property type.
 /// </summary>
        public static string GetMidLabel(PropertyType propertyType)
        {
            if (_hasDynamicRange && _currentPropertyType == propertyType)
            {
                float mid = (_currentMin + _currentMax) / 2f;
                return FormatValue(mid, propertyType);
            }

            switch (propertyType)
            {
                case PropertyType.Charge: return "0.00";
                case PropertyType.Hydrophobicity: return "0.00";
                case PropertyType.AtomicNumber: return "";
                case PropertyType.VdWRadius: return "";
                case PropertyType.Occupancy: return "0.5";
                default: return "";
            }
        }

 /// <summary>
 /// Gets the maximum label for a property type.
 /// </summary>
        public static string GetMaxLabel(PropertyType propertyType)
        {
            if (_hasDynamicRange && _currentPropertyType == propertyType)
                return FormatValue(_currentMax, propertyType);

            switch (propertyType)
            {
                case PropertyType.Charge: return "+0.50";
                case PropertyType.Hydrophobicity: return "+0.50";
                case PropertyType.AtomicNumber: return "High Z";
                case PropertyType.VdWRadius: return "Large";
                case PropertyType.Occupancy: return "1.0";
                default: return "";
            }
        }

 /// <summary>
 /// Gets the display name for a property type.
 /// </summary>
        public static string GetPropertyName(PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.Charge: return "Charge";
                case PropertyType.Hydrophobicity: return "Hydrophobicity";
                case PropertyType.AtomicNumber: return "Atomic Number";
                case PropertyType.VdWRadius: return "VdW Radius";
                case PropertyType.Occupancy: return "Occupancy";
                default: return "None";
            }
        }

        private static string FormatValue(float value, PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.Charge:
                case PropertyType.Hydrophobicity:
                    return value.ToString("F2");
                case PropertyType.AtomicNumber:
                    return ((int)value).ToString();
                case PropertyType.VdWRadius:
                    return value.ToString("F2");
                case PropertyType.Occupancy:
                    return value.ToString("F2");
                default:
                    return value.ToString("F2");
            }
        }

        #endregion

        #region Color Conversion

 /// <summary>
 /// Converts a property value to a Vector4 color.
 /// </summary>
 /// <param name="value">Property value.</param>
 /// <param name="propertyType">Type of property.</param>
 /// <param name="contrast">Contrast adjustment (1.0 = default).</param>
 /// <param name="alpha">Alpha channel (1.0 = opaque).</param>
 /// <returns>Color as Vector4.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ValueToVector4(float value, PropertyType propertyType, float contrast = 1.0f, float alpha = 1.0f)
        {
            if (propertyType == PropertyType.None)
                return new Vector4(0.5f, 0.5f, 0.5f, alpha);

 // ⭐⭐⭐ CHARGE AND HYDROPHOBICITY - SYMMETRIC AROUND ZERO ⭐⭐⭐
            if (propertyType == PropertyType.Charge || propertyType == PropertyType.Hydrophobicity)
            {
                float min, max;
                if (_hasDynamicRange && _currentPropertyType == propertyType)
                {
                    min = _currentMin;
                    max = _currentMax;
                }
                else
                {
                    var range = GetPropertyRange(propertyType);
                    min = range.min;
                    max = range.max;
                }

                float absMax = Math.Max(Math.Abs(min), Math.Abs(max));
                if (absMax < 0.001f) absMax = 0.5f;

                float normalized = Math.Clamp(value / absMax, -1f, 1f);
                float t = (normalized + 1f) / 2f;
                t = Math.Clamp(t, 0f, 1f);

                if (contrast != 1.0f)
                {
                    t = (t - 0.5f) * contrast + 0.5f;
                    t = Math.Clamp(t, 0f, 1f);
                }

                var cache = GetPrecomputedPalette(propertyType);
                if (cache == null || cache.Length == 0)
                {
                    return new Vector4(
                        Math.Clamp(t * 2f - 0.5f, 0f, 1f),
                        Math.Clamp(1f - Math.Abs(t - 0.5f) * 2f, 0f, 1f),
                        Math.Clamp(1f - t * 2f + 0.5f, 0f, 1f),
                        alpha
                    );
                }

                int idx = (int)(t * (CACHE_RESOLUTION - 1));
                idx = Math.Clamp(idx, 0, CACHE_RESOLUTION - 1);

                Vector4 color = cache[idx];
                if (alpha < 0.999f)
                    color.W = alpha;
                return color;
            }

 // ⭐⭐⭐ ATOMIC NUMBER ⭐⭐⭐
            if (propertyType == PropertyType.AtomicNumber)
            {
                float min, max;
                if (_hasDynamicRange && _currentPropertyType == propertyType)
                {
                    min = _currentMin;
                    max = _currentMax;
                }
                else
                {
                    var range = GetPropertyRange(propertyType);
                    min = range.min;
                    max = range.max;
                }

                float clampedValue = Math.Clamp(value, min, max);
                float t;
                if (Math.Abs(max - min) < 0.0001f)
                {
                    t = 0.5f;
                }
                else
                {
                    t = (clampedValue - min) / (max - min);
                    t = Math.Clamp(t, 0f, 1f);
                }

                if (contrast != 1.0f)
                {
                    t = (t - 0.5f) * contrast + 0.5f;
                    t = Math.Clamp(t, 0f, 1f);
                }

                var cache = GetPrecomputedPalette(propertyType);
                if (cache == null || cache.Length == 0)
                {
                    return new Vector4(
                        Math.Clamp(t * 1.5f, 0f, 1f),
                        Math.Clamp(1f - Math.Abs(t - 0.5f) * 2.5f, 0f, 1f),
                        Math.Clamp(1f - t * 1.5f, 0f, 1f),
                        alpha
                    );
                }

                int idx = (int)(t * (CACHE_RESOLUTION - 1));
                idx = Math.Clamp(idx, 0, CACHE_RESOLUTION - 1);

                Vector4 color = cache[idx];
                if (alpha < 0.999f)
                    color.W = alpha;
                return color;
            }

 // ⭐⭐⭐ INNE TYPY - MAPOWANIE LINIOWE ⭐⭐⭐
            float min2, max2;
            if (_hasDynamicRange && _currentPropertyType == propertyType)
            {
                min2 = _currentMin;
                max2 = _currentMax;
            }
            else
            {
                var range = GetPropertyRange(propertyType);
                min2 = range.min;
                max2 = range.max;
            }

            float t2;
            if (Math.Abs(max2 - min2) < 0.0001f)
            {
                t2 = 0.5f;
            }
            else
            {
                t2 = (value - min2) / (max2 - min2);
                t2 = Math.Clamp(t2, 0f, 1f);
            }

            if (contrast != 1.0f)
            {
                t2 = (t2 - 0.5f) * contrast + 0.5f;
                t2 = Math.Clamp(t2, 0f, 1f);
            }

            var cache2 = GetPrecomputedPalette(propertyType);
            if (cache2 == null || cache2.Length == 0)
                return new Vector4(0.5f, 0.5f, 0.5f, alpha);

            int idx2 = (int)(t2 * (CACHE_RESOLUTION - 1));
            idx2 = Math.Clamp(idx2, 0, CACHE_RESOLUTION - 1);

            Vector4 color2 = cache2[idx2];
            if (alpha < 0.999f)
                color2.W = alpha;
            return color2;
        }

 /// <summary>
 /// Converts a property value to a Vector3 color.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ValueToVector3(float value, PropertyType propertyType, float contrast = 1.0f)
        {
            Vector4 v4 = ValueToVector4(value, propertyType, contrast, 1f);
            return new Vector3(v4.X, v4.Y, v4.Z);
        }

 /// <summary>
 /// Converts a property value to a Color (XNA).
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color ValueToColor(float value, PropertyType propertyType, float contrast = 1.0f)
        {
            Vector4 v4 = ValueToVector4(value, propertyType, contrast, 1f);
            return new Color(v4.X, v4.Y, v4.Z);
        }

 /// <summary>
 /// Gets the raw palette for a property type.
 /// </summary>
        public static Vector4[] GetPalette(PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.Charge: return _chargePalette;
                case PropertyType.Hydrophobicity: return _hydrophobicityPalette;
                case PropertyType.AtomicNumber: return _atomicNumberPalette;
                case PropertyType.VdWRadius: return _vdwRadiusPalette;
                case PropertyType.Occupancy: return _occupancyPalette;
                default: return _chargePalette;
            }
        }

        #endregion
    }
}