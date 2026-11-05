// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldExpLut.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HirshfeldExpLut — fast exp(-x) approximation via LUT table
// with linear interpolation.
//
// ⭐ ZAMIENNIK MathF.Exp w hot loopach FillDensity.
//
// Uzasadnienie:
// - MathF.Exp to ~20-40 cykli CPU (f2xm1 + fscale).
// - Przy h=0.05 Å i 100 atomach → ~15M calls per thread.
// - LUT with linear interpolation: ~5 cycles (2 load + FMA).
// - Max error ~1e-5 for x ∈ [0, 25] — visually invisible.
//
// Zakres:
// - x ∈ [0, 25] → returns exp(-x) ∈ [1, ~1.4e-11]
// - x < 0 → returns 1.0 (should not happen, but safe)
// - x > 25 → returns 0.0 (exp(-25) ≈ 1.4e-11, below visual precision)
//
// Rozmiar LUT: 4096 entries × 4 B = 16 KB — fits in L1 cache.

using System;
using System.Runtime.CompilerServices;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Szybka aproksymacja exp(-x) for x ∈ [0, 25].
 ///
 /// Usage:
 /// float rho = norm * HirshfeldExpLut.FastExpNeg(0.5f * d2 * invSigSq);
 ///
 /// Semantyka identyczna jak MathF.Exp(-x), ale ~5-6× szybsza.
 /// </summary>
    public static class HirshfeldExpLut
    {
        #region Constants

 /// <summary>Liczba entries w LUT (without sentinela at the end).</summary>
        private const int SIZE = 4096;

 /// <summary>Maksymalny argument x (exp(-25) ≈ 1.4e-11).</summary>
        private const float MAX_X = 25f;

 /// <summary>Skala: x → index w LUT (SIZE / MAX_X).</summary>
        private const float SCALE = SIZE / MAX_X;

 /// <summary>Inverse scale — to convert index to x.</summary>
        private const float INV_SCALE = MAX_X / SIZE;

        #endregion

        #region LUT

 /// <summary>
 /// Tablica exp(-x) for x = i * INV_SCALE, i ∈ [0, SIZE].
 /// Rozmiar SIZE+1 — ostatni wpis for x = MAX_X.
 /// </summary>
        private static readonly float[] _lut;

        static HirshfeldExpLut()
        {
            _lut = new float[SIZE + 1];
            for (int i = 0; i <= SIZE; i++)
            {
                float x = i * INV_SCALE;
                _lut[i] = MathF.Exp(-x);
            }
        }

        #endregion

        #region FastExpNeg

 /// <summary>
 /// ⭐ Szybka aproksymacja exp(-x) with linear interpolation.
 ///
 /// For x ∈ [0, 25] returns a value close to MathF.Exp(-x).
 /// Max error ~1e-5 (relative ~1e-4).
 ///
 /// For x ≤ 0 returns 1.0.
 /// For x ≥ 25 returns 0.0.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FastExpNeg(float x)
        {
            if (x <= 0f) return 1f;
            if (x >= MAX_X) return 0f;

            float fi = x * SCALE;
            int i0 = (int)fi;
            float frac = fi - i0;

 // Liniowa interpolation: lut[i0] + frac * (lut[i0+1] - lut[i0])
            float a = _lut[i0];
            float b = _lut[i0 + 1];
            return a + (b - a) * frac;
        }

 /// <summary>
 /// ⭐ Wersja without interpolation — najszybsza, ale error ~0.3%.
 /// Use only when visually acceptable (np. pre-blur iterations > 0).
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FastExpNegNearest(float x)
        {
            if (x <= 0f) return 1f;
            if (x >= MAX_X) return 0f;

            return _lut[(int)(x * SCALE)];
        }

 /// <summary>
 /// ⭐ Version for x *already* divided by 2 — avoids multiplying by 0.5f
 /// w hot loopie when i tak liczymy 0.5f * x.
 ///
 /// Semantics: FastExpNeg(0.5f * x) — without multiplying by 0.5f on the client side.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FastExpNegHalf(float x)
        {
 // exp(-0.5 * x) = exp(-(x/2))
 // Rescale input: y = x/2, then FastExpNeg(y)
            float half = x * 0.5f;
            return FastExpNeg(half);
        }

        #endregion

        #region Diagnostics (opcjonalne)

 /// <summary>
 /// Returns informacje diagnostyczne o LUT.
 /// </summary>
        public static string GetInfo()
        {
            return $"HirshfeldExpLut: {SIZE} entries, " +
                   $"x ∈ [0, {MAX_X}], " +
                   $"size = {SIZE * 4 / 1024} KB";
        }

        #endregion
    }
}