// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal morphology / Wulff module: CrystalMorphology.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;

namespace Tal
{
    public class CrystalFace
    {
        public int H, K, L;
        public float DSpacing;
        public int Multiplicity;
        public float RelativeGrowthRate;
        public float RelativeArea;
        public Vector3 NormalCart;
        public bool Visible = true;

        public float Weight = 1f;
        public float DistFrac = 1f;

        public string Name => FormatHkl(H, K, L);

 /// <summary>
 /// ⭐ Formatuje (hkl) używając ASCII minus '-'.
 /// Zamiast combining macron '̄' (U+0304) which nie jest w SpriteFont.
 /// </summary>
        public static string FormatHkl(int h, int k, int l)
        {
            string Format(int v) => v < 0 ? $"-{-v}" : v.ToString();
            return $"({Format(h)}{Format(k)}{Format(l)})";
        }
    }

    public class CrystalMorphologyResult
    {
        public List<CrystalFace> Faces = new List<CrystalFace>();
        public float CellA, CellB, CellC;
        public float CellAlpha, CellBeta, CellGamma;
        public float CellVolume;

        public string Habitus = "unknown";
        public Vector3 AspectRatio = new Vector3(1, 1, 1);

        public float MaxDSpacing = 1f;
        public float MinDSpacing = 1f;

        public Molecule Molecule;
    }
}