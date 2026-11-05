// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: StoTypes.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Collections.Generic;

namespace Tal.Hirshfeld.Sto
{
    public struct StoTerm
    {
        public int N;
        public float Xi;
        public float[] Coefs;
    }

    public class StoShell
    {
        public string[] Names;
        public List<StoTerm> Terms;

        public int[] OccAlpha;
        public int[] OccBeta;

        public int TotalAlpha
        {
            get
            {
                if (OccAlpha == null) return 0;
                int s = 0;
                foreach (var x in OccAlpha) s += x;
                return s;
            }
        }

        public int TotalBeta
        {
            get
            {
                if (OccBeta == null) return 0;
                int s = 0;
                foreach (var x in OccBeta) s += x;
                return s;
            }
        }
    }

    public class StoAtomData
    {
        public double TotalEnergy;
        public double KineticEnergy;
        public double PotentialEnergy;

        public StoShell S;
        public StoShell P;
        public StoShell D;
        public StoShell F;

        public string Config;
        public string Symbol;
        public int ChargeState;
        public string SourceFile;

        public Dictionary<string, (int alpha, int beta)> Occupations =
            new Dictionary<string, (int, int)>();

 // ⭐ LUT cache (built lazily on first use)
 // May be null — caller checks.
        public StoDensityLut Lut;

        public bool HasS => S != null && S.Terms != null && S.Terms.Count > 0;
        public bool HasP => P != null && P.Terms != null && P.Terms.Count > 0;
        public bool HasD => D != null && D.Terms != null && D.Terms.Count > 0;
        public bool HasF => F != null && F.Terms != null && F.Terms.Count > 0;

        public int TotalElectrons =>
            (S?.TotalAlpha ?? 0) + (S?.TotalBeta ?? 0) +
            (P?.TotalAlpha ?? 0) + (P?.TotalBeta ?? 0) +
            (D?.TotalAlpha ?? 0) + (D?.TotalBeta ?? 0) +
            (F?.TotalAlpha ?? 0) + (F?.TotalBeta ?? 0);
    }
}