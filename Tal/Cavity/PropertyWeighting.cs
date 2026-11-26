// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Cavity module: distance weights used when atomic properties are projected onto the cavity.

using System;
using System.Runtime.CompilerServices;

namespace Tal.Cavity
{
    /// <summary>Distance weighting used for the projection of atomic properties onto the cavity.</summary>
    public enum PropertyWeightMode
    {
        /// <summary>w = 1 / sqrt(d² + ε)  (≈ 1/d).</summary>
        InverseDistance = 0,

        /// <summary>w = 1 / (d² + ε)  (Shepard interpolation, Eq. 15 of the article).</summary>
        InverseDistanceSquared = 1
    }

    /// <summary>
    /// Weights w(d) of the normalized weighted average
    /// P(r) = Σ P_i w(d_i) / Σ w(d_i)
    /// over the atoms within the cutoff radius. Used for charge, hydrophobicity and the other atomic properties,
    /// both for cavity voxels and for mesh vertices.
    /// The default (1/d²) is Eq. (15) of the article: w_i = 1 / (‖v − r_i‖² + ε), ε = 1e-6 Å².
    /// </summary>
    public static class PropertyWeighting
    {
        /// <summary>ε (Å²) that prevents the singularity at d = 0 (value of the article, Eq. 15).</summary>
        public const float Epsilon = 1e-6f;

        /// <summary>Currently selected mode (from <see cref="Flags.CavityPropertyWeighting"/>; default 1/d²).</summary>
        public static PropertyWeightMode CurrentMode
        {
            get
            {
                return Flags.Instance.CavityPropertyWeighting == (int)PropertyWeightMode.InverseDistance
                    ? PropertyWeightMode.InverseDistance
                    : PropertyWeightMode.InverseDistanceSquared;
            }
        }

        /// <summary>Weight for distance <paramref name="distance"/> (Å).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Weight(float distance, PropertyWeightMode mode)
        {
            float d2 = distance * distance + Epsilon;
            return mode == PropertyWeightMode.InverseDistance ? 1.0f / MathF.Sqrt(d2) : 1.0f / d2;
        }

        /// <summary>Short label for reports.</summary>
        public static string Describe(PropertyWeightMode mode)
        {
            return mode == PropertyWeightMode.InverseDistance
                ? "1/d (w = 1/sqrt(d² + 1e-6))"
                : "1/d² (w = 1/(d² + 1e-6), Eq. 15 of the article)";
        }
    }
}
