// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: StoDensityLut.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// StoDensityLut v8 — runtime-konfigurowalne parametery (Advanced panel).
//
// ⭐ v8 ZMIANY:
// - All parameters (DEFAULT_STEP, EXP_CUTOFF, MAX_RMAX) are now
// STATIC with public setters — can be changed at runtime from the GUI.
// - Zmiana wymaga wyczyszczenia cache LUT (StoDatabase.ClearLutCache())
// i afternownego prebuildu.
// - Default values consistent with v7.

using System;
using System.Runtime.CompilerServices;

namespace Tal.Hirshfeld.Sto
{
    public sealed class StoDensityLut
    {
 // ═══════════════════════════════════════════════════════════════
 // KONFIGURACJA (runtime-mutable)
 // ═══════════════════════════════════════════════════════════════

        #region Configuration

 /// <summary>Default LUT mesh step (Å).</summary>
        public static float DefaultStep { get; set; } = 0.02f;

 /// <summary>Minimum rMax (Å). Constant — not configurable.</summary>
        public const float MinRMax = 3.0f;

 /// <summary>
 /// Maksymalny rMax (Å) — hard limit.
 /// Konfigurowalny w runtime przez Advanced panel.
 /// </summary>
        public static float MaxRMax { get; set; } = 15.0f;

 /// <summary>
 /// Coefficient for GetMaxRadius: rMax = ExpCutoff / minXi.
 /// Konfigurowalny w runtime przez Advanced panel.
 /// </summary>
        public static float ExpCutoff { get; set; } = 20.0f;

 /// <summary>Radius for punktu r=0.</summary>
        public const float R0Epsilon = 1e-6f;

        #endregion

        #region Properties

        public float Step { get; }
        public float MaxRadius { get; }
        public float DensityThreshold { get; }
        public int Count => _table.Length;

        private readonly float[] _table;
        private readonly float _invStep;

        #endregion

        #region Constructor

        private StoDensityLut(float step, float maxRadius,
                              float densityThreshold, float[] table)
        {
            Step = step;
            MaxRadius = maxRadius;
            DensityThreshold = densityThreshold;
            _table = table;
            _invStep = 1.0f / step;
        }

        #endregion

        #region Build

 /// <summary>
 /// Builds LUT for danego atom.
 /// Uses current DefaultStep / ExpCutoff / MaxRMax values.
 /// </summary>
        public static StoDensityLut Build(StoAtomData atom,
                                            float step = -1f,
                                            float rMaxOverride = 0f,
                                            float densityThreshold = 0f)
        {
            if (atom == null)
                throw new ArgumentNullException(nameof(atom));

 // ⭐ -1 = use current runtime values
            if (step <= 0f) step = DefaultStep;
            if (step < 0.005f) step = 0.005f;
            if (step > 0.10f) step = 0.10f;

            float rMax;
            if (rMaxOverride > 0f)
            {
                rMax = rMaxOverride;
            }
            else
            {
 // ⭐ Use runtime ExpCutoff
                rMax = StoAtomDensity.GetMaxRadiusWithCutoff(atom, ExpCutoff);
            }

 // ⭐ Use runtime MaxRMax
            if (rMax < MinRMax) rMax = MinRMax;
            if (rMax > MaxRMax) rMax = MaxRMax;

            int n = (int)MathF.Ceiling(rMax / step) + 1;
            var table = new float[n];

            table[0] = StoAtomDensity.ComputeDensity(atom, R0Epsilon);

            for (int i = 1; i < n; i++)
            {
                float r = i * step;
                table[i] = StoAtomDensity.ComputeDensity(atom, r);
            }

            var lut = new StoDensityLut(step, rMax, 0f, table);

            System.Diagnostics.Debug.WriteLine(
                $"[StoDensityLut] {atom.Symbol}: step={step:F3}, rMax={rMax:F2} Å, " +
                $"points={n}, expCutoff={ExpCutoff:F1}, maxRMax={MaxRMax:F1}");

            return lut;
        }

        #endregion

        #region Sample

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Sample(float r)
        {
            if (r <= 0f) return _table[0];
            if (r >= MaxRadius) return 0f;

            float fi = r * _invStep;
            int i0 = (int)fi;
            float frac = fi - i0;

            int i1 = i0 + 1;
            if (i1 >= _table.Length) i1 = _table.Length - 1;

            float a = _table[i0];
            float b = _table[i1];
            return a + (b - a) * frac;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float SampleSq(float r2)
        {
            if (r2 <= 0f) return _table[0];

            float rMaxSq = MaxRadius * MaxRadius;
            if (r2 >= rMaxSq) return 0f;

            return Sample(MathF.Sqrt(r2));
        }

        #endregion

        #region Info

        public string GetInfo()
        {
            return $"StoDensityLut: {_table.Length} pts, " +
                   $"step={Step:F4} Å, rMax={MaxRadius:F2} Å, " +
                   $"mem={_table.Length * 4 / 1024f:F1} KB";
        }

        #endregion
    }
}