// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure file parsers: MolParser.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace Tal
{
    public class MolData
    {
        public string FileName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Comment { get; set; } = "";
        public string Program { get; set; } = "";

        public List<MolAtom> atom = new List<MolAtom>();
        public List<(int a1, int a2, int order)> Bonds = new List<(int, int, int)>(); // 1-based

        public bool completed;
        public bool isV3000;

        public void Clean()
        {
            completed = false;
            isV3000 = false;
            Title = Comment = Program = "";
            atom?.Clear();
            Bonds?.Clear();
        }
    }

    public class MolAtom
    {
        public string element = "";
        public int type;                  // atomic number
        public float x, y, z;             // Cartesian (Å)
        public int charge = 0;            // formal charge
        public int stereo = 0;
        public float iso = 0.05f;
        public string adp_type = "Uiso";
    }

 /// <summary>
 /// Parser for MDL Molfile / SDF (V2000).
 /// Supafterrts classic 3-line header + counts line + atom/bond blocks.
 /// Coordinates are Cartesian. No unit cell → treated as P1 molecule.
 /// </summary>
    public static class MolParser
    {
        private static readonly string[] _elements = new string[]
        {
            "H","He","Li","Be","B","C","N","O","F","Ne","Na","Mg",
            "Al","Si","P","S","Cl","Ar","K","Ca","Sc","Ti","V","Cr",
            "Mn","Fe","Co","Ni","Cu","Zn","Ga","Ge","As","Se","Br",
            "Kr","Rb","Sr","Y","Zr","Nb","Mo","Tc","Ru","Rh","Pd",
            "Ag","Cd","In","Sn","Sb","Te","I","Xe","Cs","Ba","La",
            "Ce","Pr","Nd","Pm","Sm","Eu","Gd","Tb","Dy","Ho","Er",
            "Tm","Yb","Lu","Hf","Ta","W","Re","Os","Ir","Pt","Au",
            "Hg","Tl","Pb","Bi","Po","At","Rn","Fr","Ra","Ac","Th",
            "Pa","U","Np","Pu","Am","Cm","Bk","Cf"
        };

        public static bool Parse(string content, MolData mol)
        {
            Debug.WriteLine("[MolParser] ========== START PARSING MOL/SDF ==========");

            if (string.IsNullOrEmpty(content) || mol == null)
                return false;

            mol.Clean();

            var lines = Regex.Split(content, "\r\n|\r|\n");
            if (lines.Length < 4)
            {
                Debug.WriteLine("[MolParser] Too few lines");
                return false;
            }

 // --- Header (3 lines) ---
            mol.Title = lines[0].Trim();
            mol.Program = lines.Length > 1 ? lines[1].Trim() : "";
            mol.Comment = lines.Length > 2 ? lines[2].Trim() : "";

 // Detect V3000
            string countsLine = lines[3].Trim();
            if (countsLine.Contains("V3000", StringComparison.OrdinalIgnoreCase))
            {
                mol.isV3000 = true;
                Debug.WriteLine("[MolParser] V3000 detected – not fully supported yet");
 // For now we only implement V2000 robustly
            }

 // --- Counts line (V2000) ---
 // Columns: aaabbblllfffcccsssxxxrrrpppiiimmmvvvvvv
 // aa = atoms, bb = bonds (fixed width 3)
            int numAtoms = 0;
            int numBonds = 0;

            if (!mol.isV3000)
            {
 // Robust parse: first two integers
                var countParts = Regex.Split(countsLine, @"\s+");
                if (countParts.Length >= 2)
                {
                    int.TryParse(countParts[0], out numAtoms);
                    int.TryParse(countParts[1], out numBonds);
                }
                else if (countsLine.Length >= 6)
                {
 // Fixed-width fallback
                    int.TryParse(countsLine.Substring(0, 3).Trim(), out numAtoms);
                    int.TryParse(countsLine.Substring(3, 3).Trim(), out numBonds);
                }
            }

            Debug.WriteLine($"[MolParser] Title: '{mol.Title}'");
            Debug.WriteLine($"[MolParser] Atoms expected: {numAtoms}, Bonds expected: {numBonds}");

            int lineIdx = 4; // start of atom block

 // --- Atom block ---
            for (int i = 0; i < numAtoms && lineIdx < lines.Length; i++, lineIdx++)
            {
                string line = lines[lineIdx];
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("M  ", StringComparison.Ordinal) ||
                    line.StartsWith("$$$$", StringComparison.Ordinal))
                    break;

                var atom = ParseAtomLine(line);
                if (atom != null)
                    mol.atom.Add(atom);
            }

 // --- Bond block ---
            for (int i = 0; i < numBonds && lineIdx < lines.Length; i++, lineIdx++)
            {
                string line = lines[lineIdx];
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("M  ", StringComparison.Ordinal) ||
                    line.StartsWith("$$$$", StringComparison.Ordinal))
                    break;

                var bond = ParseBondLine(line);
                if (bond.a1 > 0 && bond.a2 > 0)
                    mol.Bonds.Add(bond);
            }

 // Skip to M END or $$$$
            while (lineIdx < lines.Length)
            {
                string line = lines[lineIdx].Trim();
                if (line.StartsWith("M  END", StringComparison.OrdinalIgnoreCase) ||
                    line == "$$$$")
                    break;
                lineIdx++;
            }

            mol.completed = mol.atom.Count > 0;

            LogSummary(mol);
            return mol.completed;
        }

        private static MolAtom ParseAtomLine(string line)
        {
 // V2000 atom line (fixed width preferred, but we also accept space-separated)
 // xxxxx.xxxxyyyyy.yyyyzzzzz.zzzz aaaddcccssshhhbbbvvvHHHrrriiimmmnnneee
            try
            {
                float x, y, z;
                string elem;

 // Try fixed-width first (most reliable for CCDC/MOL)
                if (line.Length >= 34)
                {
                    x = ParseFloatFixed(line, 0, 10);
                    y = ParseFloatFixed(line, 10, 10);
                    z = ParseFloatFixed(line, 20, 10);
                    elem = line.Substring(31, Math.Min(3, line.Length - 31)).Trim();
                }
                else
                {
 // Fallback: space-separated
                    var parts = Regex.Split(line.Trim(), @"\s+");
                    if (parts.Length < 4) return null;
                    if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)) return null;
                    if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)) return null;
                    if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z)) return null;
                    elem = parts[3];
                }

                if (string.IsNullOrEmpty(elem)) return null;

 // Charge (optional, columns 37-39 in fixed format)
                int charge = 0;
                if (line.Length >= 39)
                {
                    string chStr = line.Substring(36, 3).Trim();
                    if (int.TryParse(chStr, out int ch))
                    {
 // MDL charge encoding: 0=none, 1=+3, 2=+2, 3=+1, 4=doublet, 5=-1, 6=-2, 7=-3
                        charge = ch switch
                        {
                            1 => 3,
                            2 => 2,
                            3 => 1,
                            5 => -1,
                            6 => -2,
                            7 => -3,
                            _ => 0
                        };
                    }
                }

                return new MolAtom
                {
                    element = elem,
                    type = GetAtomicNumber(elem),
                    x = x,
                    y = y,
                    z = z,
                    charge = charge
                };
            }
            catch
            {
                return null;
            }
        }

        private static (int a1, int a2, int order) ParseBondLine(string line)
        {
            try
            {
                int a1, a2, order = 1;

                if (line.Length >= 9)
                {
 // Fixed width: aaabbbttt
                    a1 = ParseIntFixed(line, 0, 3);
                    a2 = ParseIntFixed(line, 3, 3);
                    order = ParseIntFixed(line, 6, 3);
                }
                else
                {
                    var parts = Regex.Split(line.Trim(), @"\s+");
                    if (parts.Length < 2) return (0, 0, 0);
                    int.TryParse(parts[0], out a1);
                    int.TryParse(parts[1], out a2);
                    if (parts.Length >= 3) int.TryParse(parts[2], out order);
                }

                return (a1, a2, order);
            }
            catch
            {
                return (0, 0, 0);
            }
        }

        #region Conversion

        public static StructureData ConvertToStructure(MolData mol, string filePath)
        {
            if (mol == null || !mol.completed) return null;

            string name = !string.IsNullOrEmpty(mol.Title) ? mol.Title : Path.GetFileNameWithoutExtension(filePath);

            var structure = new StructureData
            {
                Name = name,
                FilePath = filePath,
 // No crystallographic cell → isolated molecule
                a = 1f,
                b = 1f,
                c = 1f,
                alpha = 90f,
                beta = 90f,
                gamma = 90f,
                SpaceGroup = 1,
                SpaceGroupName = "P1",
                ChemicalName = mol.Title
            };

            structure.SymmetryOperations.Add("x,y,z");

            int idx = 1;
            foreach (var ma in mol.atom)
            {
                var atom = new SimpleAtom
                {
                    Name = $"{ma.element}{idx++}",
                    Symbol = ma.element,
                    AtomicNumber = ma.type,
                    X = ma.x,          // Cartesian
                    Y = ma.y,
                    Z = ma.z,
                    Occupancy = 1f,
                    iso = ma.iso,
                    AdpType = ma.adp_type
                };
                structure.Atoms.Add(atom);
            }

 // Optional: you can later store bonds in StructureData if you extend it
 // structure.Bonds = mol.Bonds; // if you add such a property

            return structure;
        }

        #endregion

        #region Utilities

        private static float ParseFloatFixed(string line, int start, int length)
        {
            if (start + length > line.Length) return 0f;
            string s = line.Substring(start, length).Trim();
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                return v;
            return 0f;
        }

        private static int ParseIntFixed(string line, int start, int length)
        {
            if (start + length > line.Length) return 0;
            string s = line.Substring(start, length).Trim();
            if (int.TryParse(s, out int v)) return v;
            return 0;
        }

        private static int GetAtomicNumber(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return 6;
            string clean = symbol.Trim();
            for (int i = 0; i < _elements.Length; i++)
                if (string.Equals(_elements[i], clean, StringComparison.OrdinalIgnoreCase))
                    return i + 1;
            if (clean.Length >= 1)
            {
                string one = clean.Substring(0, 1);
                for (int i = 0; i < _elements.Length; i++)
                    if (string.Equals(_elements[i], one, StringComparison.OrdinalIgnoreCase))
                        return i + 1;
            }
            return 6;
        }

        private static void LogSummary(MolData mol)
        {
            Debug.WriteLine("[MolParser] ══════════════════════════════════════");
            Debug.WriteLine($"[MolParser] File: {mol.FileName}");
            Debug.WriteLine($"[MolParser] Title: {mol.Title}");
            Debug.WriteLine($"[MolParser] Program/Info: {mol.Program}");
            Debug.WriteLine($"[MolParser] Atoms: {mol.atom.Count}");
            Debug.WriteLine($"[MolParser] Bonds: {mol.Bonds.Count}");
            Debug.WriteLine($"[MolParser] V3000: {mol.isV3000}");
            Debug.WriteLine("[MolParser] ══════════════════════════════════════");
        }

        #endregion
    }
}