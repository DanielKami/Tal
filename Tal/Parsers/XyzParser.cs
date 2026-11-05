// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure file parsers: XyzParser.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Tal.Parsers
{
    public class XyzData
    {
        public string FileName { get; set; } = "";
        public string Comment { get; set; } = "";
        public List<XyzAtom> atom = new List<XyzAtom>();
        public bool completed;

        public void Clean()
        {
            completed = false;
            Comment = "";
            atom?.Clear();
        }
    }

    public class XyzAtom
    {
        public string element = "";
        public int type;
        public float x, y, z;           // Cartesian
        public float charge = 0f;       // optional 5th column
    }

 /// <summary>
 /// Simple XYZ parser (Cartesian coordinates).
 /// Format:
 /// N
 /// comment line
 /// Element x y z [charge]
 /// </summary>
    public static class XyzParser
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

        public static bool Parse(string content, XyzData xyz)
        {
            Debug.WriteLine("[XyzParser] ========== START PARSING XYZ ==========");

            if (string.IsNullOrEmpty(content) || xyz == null)
                return false;

            xyz.Clean();

            var lines = Regex.Split(content, "\r\n|\r|\n")
                             .Select(l => l.Trim())
                             .Where(l => !string.IsNullOrEmpty(l))
                             .ToList();

            if (lines.Count < 2)
            {
                Debug.WriteLine("[XyzParser] Too few lines");
                return false;
            }

 // Line 0: number of atoms
            if (!int.TryParse(lines[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int expected))
            {
                Debug.WriteLine($"[XyzParser] First line is not a number: '{lines[0]}'");
                return false;
            }

 // Line 1: comment
            xyz.Comment = lines.Count > 1 ? lines[1] : "";

 // Atoms start from line 2
            for (int i = 2; i < lines.Count; i++)
            {
                var parts = Regex.Split(lines[i], @"\s+");
                if (parts.Length < 4) continue;

                string elem = parts[0];
                if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) continue;

                float charge = 0f;
                if (parts.Length >= 5)
                    float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out charge);

                var atom = new XyzAtom
                {
                    element = elem,
                    type = GetAtomicNumber(elem),
                    x = x,
                    y = y,
                    z = z,
                    charge = charge
                };
                xyz.atom.Add(atom);
            }

            xyz.completed = xyz.atom.Count > 0;

            Debug.WriteLine($"[XyzParser] Expected: {expected}, Parsed: {xyz.atom.Count}");
            Debug.WriteLine($"[XyzParser] Comment: {xyz.Comment}");
            Debug.WriteLine("[XyzParser] ========================================");

            return xyz.completed;
        }

        public static StructureData ConvertToStructure(XyzData xyz, string filePath)
        {
            if (xyz == null || !xyz.completed) return null;

            var structure = new StructureData
            {
                Name = !string.IsNullOrEmpty(xyz.Comment)
                    ? xyz.Comment
                    : Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath,
 // No cell → treat as isolated molecule (P1)
                a = 1f,
                b = 1f,
                c = 1f,
                alpha = 90f,
                beta = 90f,
                gamma = 90f,
                SpaceGroup = 1,
                SpaceGroupName = "P1"
            };

            structure.SymmetryOperations.Add("x,y,z");

            int idx = 1;
            foreach (var xa in xyz.atom)
            {
                var atom = new SimpleAtom
                {
                    Name = $"{xa.element}{idx++}",
                    Symbol = xa.element,
                    AtomicNumber = xa.type,
                    X = xa.x,          // keep Cartesian (Molecule can handle it)
                    Y = xa.y,
                    Z = xa.z,
                    Occupancy = 1f,
                    iso = 0.05f,
                    AdpType = "Uiso"
                };
                structure.Atoms.Add(atom);
            }

            return structure;
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
    }
}