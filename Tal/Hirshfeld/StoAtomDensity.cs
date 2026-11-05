// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: StoAtomDensity.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// StoAtomDensity v4 — dodano FindRadiusAtThreshold (cutoff na density).

using System;
using System.Collections.Generic;

namespace Tal.Hirshfeld.Sto
{
    public static class StoAtomDensity
    {
 // ═══════════════════════════════════════════════════════════════
 // Radial STO
 // ═══════════════════════════════════════════════════════════════

        public static float RadialSto(int n, float xi, float r)
        {
            if (r <= 0f) return 0f;
            if (n < 1) return 0f;
            if (xi <= 0f) return 0f;

            double twoXi = 2.0 * xi;
            double norm = Math.Pow(twoXi, n + 0.5) / Math.Sqrt(Factorial(2 * n));
            double rn1 = Math.Pow(r, n - 1);
            double exp = Math.Exp(-xi * r);

            return (float)(norm * rn1 * exp);
        }

        public static float RadialStoDerivative(int n, float xi, float r, float R)
        {
            if (r <= 0f) return 0f;
            return ((n - 1) / r - xi) * R;
        }


 /// <summary>
 /// ⭐ v8: GetMaxRadius z konfigurowalnym cutoffem.
 /// rMax = expCutoff / minXi.
 /// </summary>
        public static float GetMaxRadiusWithCutoff(StoAtomData atom, float expCutoff)
        {
            if (atom == null) return 10f;
            if (expCutoff <= 0f) expCutoff = 10f;

            float minXi = float.MaxValue;
            CollectMinXi(atom.S, ref minXi);
            CollectMinXi(atom.P, ref minXi);
            CollectMinXi(atom.D, ref minXi);
            CollectMinXi(atom.F, ref minXi);

            if (minXi >= float.MaxValue) return 10f;
            if (minXi < 1e-6f) minXi = 1e-6f;

            return expCutoff / minXi;


        }
 // ═══════════════════════════════════════════════════════════════
 // Density
 // ═══════════════════════════════════════════════════════════════

        public static float ComputeDensity(StoAtomData atom, float r)
        {
            if (atom == null || r <= 0f) return 0f;

            float sA = ComputeShellDensity(atom.S, r, true);
            float sB = ComputeShellDensity(atom.S, r, false);
            float pA = ComputeShellDensity(atom.P, r, true);
            float pB = ComputeShellDensity(atom.P, r, false);
            float dA = ComputeShellDensity(atom.D, r, true);
            float dB = ComputeShellDensity(atom.D, r, false);
            float fA = ComputeShellDensity(atom.F, r, true);
            float fB = ComputeShellDensity(atom.F, r, false);

            return (sA + sB + pA + pB + dA + dB + fA + fB) / (4f * MathF.PI);
        }

 /// <summary>
 /// Density of one block (S/P/D/F) in a given spin channel.
 /// Returns Σ occ·|ψ|² — BEZ dzielenia przez 4π.
 /// </summary>
        private static float ComputeShellDensity(StoShell shell, float r, bool alpha)
        {
            if (shell == null || shell.Terms == null || shell.Terms.Count == 0)
                return 0f;

            int nOrbitals = shell.Terms[0].Coefs?.Length ?? 0;
            if (nOrbitals == 0) return 0f;

            int nTerms = shell.Terms.Count;
            var R = new float[nTerms];
            for (int i = 0; i < nTerms; i++)
            {
                var t = shell.Terms[i];
                R[i] = RadialSto(t.N, t.Xi, r);
            }

            int[] occ = alpha ? shell.OccAlpha : shell.OccBeta;

            float sum = 0f;
            for (int orb = 0; orb < nOrbitals; orb++)
            {
                int occValue = (occ != null && orb < occ.Length) ? occ[orb] : 0;
                if (occValue == 0) continue;

                float psi = 0f;
                for (int i = 0; i < nTerms; i++)
                    psi += shell.Terms[i].Coefs[orb] * R[i];

                sum += occValue * psi * psi;
            }

            return sum;
        }

 // ═══════════════════════════════════════════════════════════════
 // Integration
 // ═══════════════════════════════════════════════════════════════

        public static float IntegrateElectrons(
            StoAtomData atom,
            float rMax = 0f,
            int steps = 4000)
        {
            if (atom == null) return 0f;
            if (steps < 1) steps = 1;

            if (rMax <= 0f)
                rMax = GetMaxRadius(atom);

            float dr = rMax / steps;
            double sum = 0.0;
            double fourPi = 4.0 * Math.PI;

            for (int i = 0; i < steps; i++)
            {
                float r = (i + 0.5f) * dr;
                float rho = ComputeDensity(atom, r);
                sum += rho * fourPi * r * r * dr;
            }

            return (float)sum;
        }

 // ═══════════════════════════════════════════════════════════════
 // Cut-off — dwa warianty
 // ═══════════════════════════════════════════════════════════════

 /// <summary>
 /// ⭐ v4: Finds the radius at which ρ(r) falls below the threshold.
 /// Used do wyznaczenia cutoff w LUT (performance).
 ///
 /// Bisection — fast and accurate.
 /// </summary>
        public static float FindRadiusAtThreshold(StoAtomData atom,
                                                   float threshold = 1e-3f,
                                                   float rMax = 30f)
        {
            if (atom == null) return 10f;
            if (threshold <= 0f) threshold = 1e-3f;

 // Quick check: do we reach the threshold at all?
            float rhoNear = ComputeDensity(atom, 0.01f);
            if (rhoNear < threshold) return 1.0f;

 // Bisekcja
            float lo = 0.01f;
            float hi = rMax;

 // Expand hi while ρ(hi) > threshold
            while (hi < 50f && ComputeDensity(atom, hi) > threshold)
                hi *= 1.5f;

            for (int iter = 0; iter < 40; iter++)
            {
                float mid = (lo + hi) * 0.5f;
                float rhoMid = ComputeDensity(atom, mid);

                if (rhoMid > threshold) lo = mid;
                else hi = mid;
            }

            return hi;
        }

 /// <summary>
 /// Old variant — radius at which exp(-ξ·r) ~ 1e-12.
 /// Used for electron integration and debugging.
 /// </summary>
        public static float GetMaxRadius(StoAtomData atom)
        {
            if (atom == null) return 10f;

            float minXi = float.MaxValue;
            CollectMinXi(atom.S, ref minXi);
            CollectMinXi(atom.P, ref minXi);
            CollectMinXi(atom.D, ref minXi);
            CollectMinXi(atom.F, ref minXi);

            if (minXi >= float.MaxValue) return 10f;
            if (minXi < 1e-6f) minXi = 1e-6f;

            return 27.6f / minXi;
        }

        private static void CollectMinXi(StoShell shell, ref float minXi)
        {
            if (shell?.Terms == null) return;
            foreach (var t in shell.Terms)
                if (t.Xi > 0f && t.Xi < minXi)
                    minXi = t.Xi;
        }

 // ═══════════════════════════════════════════════════════════════
 // DIAGNOSTYKA — log per shell (S/P/D/F)
 // ═══════════════════════════════════════════════════════════════

        public static List<(string text, System.Drawing.Color color)>
            GetDiagnostics(StoAtomData atom)
        {
            var lines = new List<(string, System.Drawing.Color)>();

            if (atom == null)
            {
                lines.Add(("(null atom)", System.Drawing.Color.Red));
                return lines;
            }

            var cNeutral = System.Drawing.Color.LightGray;
            var cS = System.Drawing.Color.Cyan;
            var cP = System.Drawing.Color.LightGreen;
            var cD = System.Drawing.Color.Orange;
            var cF = System.Drawing.Color.Magenta;

            lines.Add(($"  Symbol: {atom.Symbol}, Z={StoDatabase.SymbolToAtomicNumber(atom.Symbol)}, " +
                       $"charge={atom.ChargeState:+0;-0;0}", cNeutral));
            lines.Add(($"  Config: {atom.Config}", cNeutral));
            lines.Add(($"  Occupations dict: {atom.Occupations?.Count ?? 0} entries", cNeutral));

            if (atom.Occupations != null && atom.Occupations.Count > 0)
            {
                var parts = new List<string>();
                foreach (var kv in atom.Occupations)
                    parts.Add($"{kv.Key}=({kv.Value.alpha},{kv.Value.beta})");
                lines.Add(($"    {string.Join("  ", parts)}", cNeutral));
            }

            float sA = IntegrateShell(atom.S, true);
            float sB = IntegrateShell(atom.S, false);
            float pA = IntegrateShell(atom.P, true);
            float pB = IntegrateShell(atom.P, false);
            float dA = IntegrateShell(atom.D, true);
            float dB = IntegrateShell(atom.D, false);
            float fA = IntegrateShell(atom.F, true);
            float fB = IntegrateShell(atom.F, false);

            float sumS = sA + sB;
            float sumP = pA + pB;
            float sumD = dA + dB;
            float sumF = fA + fB;
            float total = sumS + sumP + sumD + sumF;

            lines.Add(($"  ∫ρ  S: α={sA,7:F3}  β={sB,7:F3}  Σ={sumS,7:F3}  " +
                       $"(occ: α={atom.S?.TotalAlpha ?? 0}, β={atom.S?.TotalBeta ?? 0})", cS));

            if (atom.P != null && atom.P.Terms != null && atom.P.Terms.Count > 0)
                lines.Add(($"  ∫ρ  P: α={pA,7:F3}  β={pB,7:F3}  Σ={sumP,7:F3}  " +
                           $"(occ: α={atom.P.TotalAlpha}, β={atom.P.TotalBeta})", cP));

            if (atom.D != null && atom.D.Terms != null && atom.D.Terms.Count > 0)
                lines.Add(($"  ∫ρ  D: α={dA,7:F3}  β={dB,7:F3}  Σ={sumD,7:F3}  " +
                           $"(occ: α={atom.D.TotalAlpha}, β={atom.D.TotalBeta})", cD));

            if (atom.F != null && atom.F.Terms != null && atom.F.Terms.Count > 0)
                lines.Add(($"  ∫ρ  F: α={fA,7:F3}  β={fB,7:F3}  Σ={sumF,7:F3}  " +
                           $"(occ: α={atom.F.TotalAlpha}, β={atom.F.TotalBeta})", cF));

            int z = StoDatabase.SymbolToAtomicNumber(atom.Symbol);
            int expected = z + atom.ChargeState;

            var cTotal = Math.Abs(total - expected) < 0.05f
                ? System.Drawing.Color.LightGreen
                : System.Drawing.Color.Red;

            lines.Add(($"  ∫ρ TOTAL = {total:F4}  (expected {expected})  diff = {total - expected:+0.0000;-0.0000;0.0000}",
                       cTotal));

 // ⭐ v4: cutoffy
            float rThreshold = FindRadiusAtThreshold(atom, 1e-3f);
            float rThreshold4 = FindRadiusAtThreshold(atom, 1e-4f);
            float rOld = GetMaxRadius(atom);

            lines.Add(($"  cutoffs: ρ<1e-3 → {rThreshold:F2} Å | " +
                       $"ρ<1e-4 → {rThreshold4:F2} Å | " +
                       $"exp(-ξr)<1e-12 → {rOld:F2} Å", cNeutral));

            return lines;
        }

        private static float IntegrateShell(StoShell shell, bool alpha)
        {
            if (shell == null || shell.Terms == null || shell.Terms.Count == 0)
                return 0f;

            float rMax = 20f;
            float minXi = float.MaxValue;
            foreach (var t in shell.Terms)
                if (t.Xi > 0f && t.Xi < minXi) minXi = t.Xi;
            if (minXi < float.MaxValue && minXi > 1e-6f)
                rMax = 27.6f / minXi;

            const int steps = 3000;
            float dr = rMax / steps;
            double sum = 0.0;

            for (int i = 0; i < steps; i++)
            {
                float r = (i + 0.5f) * dr;
                float rho = ComputeShellDensity(shell, r, alpha);
                sum += rho * r * r * dr;
            }

            return (float)sum;
        }

 // ═══════════════════════════════════════════════════════════════
 // Helpers
 // ═══════════════════════════════════════════════════════════════

        private static double Factorial(int n)
        {
            double r = 1.0;
            for (int i = 2; i <= n; i++) r *= i;
            return r;
        }
    }
}