// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.Helpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.Helpers — small utility methods
// ═══════════════════════════════════════════════════════════════════════
//
// This file contains small helper methods used across the rendering pipeline:
//
// GetAtomColor() — atom color depending on ColorMode flag
// GetSymmetryColor() — palette for symmetry equivalent index
// GetMoleculeColor() — palette for molecule index
// GetSymmetryMoleculeColor() — hash-based color for (sym, mol) pair
// IsMetal() — atomic number → metal classification
// GetBackgroundColorVector() — Flags.color2 → Vector3
// GetLabelFont() — font selection (SpriteFont / Arial / Times)
//
// ⚠️ GetAtomColor is called per-atom in PrepareAtomColors (slow path).
// Others are called on demand — not in the hot per-atom loop.
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tal.Cavity;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region GetAtomColor — per-atom color dispatcher

 /// <summary>
 /// Returns the display color of an atom according to the current
 /// <see cref="Flags.ColorMode"/> flag.
 ///
 /// Modes:
 /// 0 — CPK (default)
 /// 1 — symmetry equivalent
 /// 2 — molecule
 /// 3 — symmetry × molecule
 /// 4 — custom color (fallback to CPK)
 /// 5 — charge (from ColorManager)
 /// 6 — hydrophobicity (from ColorManager)
 /// other — CPK fallback
 ///
 /// ⚠️ This is the SLOW PATH — only called when ColorMode is 1, 2, 3, 5, 6.
 /// Modes 0 and 4 are handled inline in PrepareAtomColors.
 /// </summary>
        private Vector3 GetAtomColor(int atomIndex)
        {
            if (_molecule == null || atomIndex < 0 || atomIndex >= _molecule.NumberOfAtoms)
                return new Vector3(0.6f, 0.6f, 0.6f);

            var atom = _molecule.atom[atomIndex];
            int mode = Flags.Instance.ColorMode;

            if (mode == 4)
            {
                if (atom.CustomColor.X >= 0f && atom.CustomColor.Y >= 0f && atom.CustomColor.Z >= 0f)
                    return atom.CustomColor;
                return VdwParameters.color[atom.AtomicNumber];
            }

            switch (mode)
            {
                case 0: return VdwParameters.color[atom.AtomicNumber];
                case 1: return GetSymmetryColor(atom.SymmetryEquivalentNumberInCell);
                case 2: return GetMoleculeColor(atom.IndividualMoleculeInCell);
                case 3:
                    int key = atom.SymmetryEquivalentNumberInCell * 100 + atom.IndividualMoleculeInCell;
                    return GetSymmetryMoleculeColor(key);

                case 5:
                    try
                    {
                        var color = ColorManager.ValueToVector3(atom.charge, ColorManager.PropertyType.Charge);
                        if (float.IsNaN(color.X) || float.IsNaN(color.Y) || float.IsNaN(color.Z))
                            return new Vector3(0.5f, 0.5f, 0.5f);
                        return color;
                    }
                    catch { return new Vector3(0.5f, 0.5f, 0.5f); }

                case 6:
                    try
                    {
                        var color = ColorManager.ValueToVector3(atom.Hydrophobicity, ColorManager.PropertyType.Hydrophobicity);
                        if (float.IsNaN(color.X) || float.IsNaN(color.Y) || float.IsNaN(color.Z))
                            return new Vector3(0.5f, 0.5f, 0.5f);
                        return color;
                    }
                    catch { return new Vector3(0.5f, 0.5f, 0.5f); }

                default: return VdwParameters.color[atom.AtomicNumber];
            }
        }

        #endregion

        #region Palettes — symmetry / molecule

 /// <summary>
 /// Fixed palette for symmetry equivalents (mode 1).
 /// Negative index → neutral gray.
 /// </summary>
        private Vector3 GetSymmetryColor(int symNum)
        {
            if (symNum < 0) return new Vector3(0.6f, 0.6f, 0.6f);

            Vector3[] colors = new Vector3[]
            {
                new Vector3(1f, 0.2f, 0.2f), new Vector3(0.2f, 1f, 0.2f),
                new Vector3(0.2f, 0.2f, 1f), new Vector3(1f, 1f, 0.2f),
                new Vector3(1f, 0.2f, 1f), new Vector3(0.2f, 1f, 1f),
                new Vector3(1f, 0.6f, 0.2f), new Vector3(0.6f, 0.2f, 1f),
                new Vector3(0.2f, 0.8f, 0.4f), new Vector3(0.8f, 0.2f, 0.4f),
                new Vector3(0.4f, 0.8f, 0.8f), new Vector3(0.8f, 0.6f, 0.2f),
                new Vector3(0.2f, 0.4f, 0.8f), new Vector3(0.8f, 0.4f, 0.8f),
                new Vector3(0.4f, 0.8f, 0.2f), new Vector3(0.2f, 0.6f, 0.6f),
            };

            return colors[symNum % colors.Length];
        }

 /// <summary>
 /// Fixed palette for individual molecules (mode 2).
 /// Negative index → neutral gray.
 /// </summary>
        private Vector3 GetMoleculeColor(int molNum)
        {
            if (molNum < 0) return new Vector3(0.6f, 0.6f, 0.6f);

            Vector3[] colors = new Vector3[]
            {
                new Vector3(1f, 0.4f, 0.4f), new Vector3(0.4f, 1f, 0.4f),
                new Vector3(0.4f, 0.4f, 1f), new Vector3(1f, 1f, 0.4f),
                new Vector3(1f, 0.4f, 1f), new Vector3(0.4f, 1f, 1f),
                new Vector3(1f, 0.6f, 0.2f), new Vector3(0.6f, 0.2f, 1f),
                new Vector3(0.2f, 0.8f, 0.4f), new Vector3(0.8f, 0.2f, 0.4f),
            };

            return colors[molNum % colors.Length];
        }

 /// <summary>
 /// Deterministic hash-based color for (symmetry, molecule) pairs (mode 3).
 ///
 /// Same key always produces the same color — good for stable visualization
 /// of many small molecules in a cell.
 /// </summary>
        private Vector3 GetSymmetryMoleculeColor(int key)
        {
            if (key < 0) return new Vector3(0.6f, 0.6f, 0.6f);
            Random rand = new Random(key);
            return new Vector3(
                (float)(rand.NextDouble() * 0.8 + 0.2),
                (float)(rand.NextDouble() * 0.8 + 0.2),
                (float)(rand.NextDouble() * 0.8 + 0.2));
        }

        #endregion

        #region IsMetal — atomic number classification

 /// <summary>
 /// Returns true if the atomic number corresafternds to a metal.
 ///
 /// Used to pick BallRadiusMetal vs BallRadiusNonMetal.
 ///
 /// Groups included:
 /// - Alkali + alkaline earth: 3-4, 11-12
 /// - Transition + afterst-transition: 19-31, 37-50, 55-83
 /// - Actinides up to Z=112
 ///
 /// Note: includes metalloids like B (5) are NOT included, but
 /// includes elements up to Copernicium (112).
 /// </summary>
        private bool IsMetal(int atomicNumber)
        {
            return atomicNumber >= 3 && atomicNumber <= 4 ||
                   atomicNumber >= 11 && atomicNumber <= 12 ||
                   atomicNumber >= 19 && atomicNumber <= 31 ||
                   atomicNumber >= 37 && atomicNumber <= 50 ||
                   atomicNumber >= 55 && atomicNumber <= 83 ||
                   atomicNumber >= 87 && atomicNumber <= 112;
        }

        #endregion

        #region GetBackgroundColorVector — Flags.color2 → Vector3

 /// <summary>
 /// Returns the background color as a normalized Vector3.
 /// Used for depth cue blending.
 /// </summary>
        private Vector3 GetBackgroundColorVector()
        {
            var bg = Flags.Instance.color2;
            return new Vector3(bg.R / 255f, bg.G / 255f, bg.B / 255f);
        }

        #endregion

        #region GetLabelFont — font selection

 /// <summary>
 /// Returns the SpriteFont to use for labels, based on
 /// <see cref="Flags.LabelFontName"/>.
 ///
 /// Fallback chain:
 /// "Arial" → ArialFont (if loaded) → SpriteFont
 /// "Times New Roman" → TimesFont (if loaded) → SpriteFont
 /// anything else → SpriteFont (default)
 ///
 /// ⚠️ TODO: <c>Flags.LabelFontBold</c> is read in the original code
 /// but never used. Likely dead code — verify whether bold variants
 /// are pre-loaded by the content pipeline, and if so, remove the flag
 /// or implement bold selection here.
 /// </summary>
        private SpriteFont GetLabelFont()
        {
            string name = Flags.Instance.LabelFontName ?? "Segoe UI";

 // TODO: unused — verify if needed in future refactor
            bool bold = Flags.Instance.LabelFontBold;

            if (name.Equals("Arial", StringComparison.OrdinalIgnoreCase))
            {
                if (ArialFont != null) return ArialFont;
            }

            if (name.Equals("Times New Roman", StringComparison.OrdinalIgnoreCase))
            {
                if (TimesFont != null) return TimesFont;
            }

            return SpriteFont;
        }

        #endregion
    }
}