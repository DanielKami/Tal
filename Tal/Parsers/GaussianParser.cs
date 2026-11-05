// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure file parsers: GaussianParser.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace Tal.Parsers
{
    public class GaussianData
    {
        public string FileName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Route { get; set; } = "";           // np. "#p B3LYP/6-31G(d) Opt"
        public int Charge { get; set; } = 0;
        public int Multiplicity { get; set; } = 1;

        public List<GaussianAtom> atom = new List<GaussianAtom>();
        public bool completed;
        public bool isBohr;                               // true jeśli współrzędne w Bohr

        public void Clean()
        {
            completed = false;
            isBohr = false;
            Title = Route = "";
            Charge = 0;
            Multiplicity = 1;
            atom?.Clear();
        }
    }

    public class GaussianAtom
    {
        public string element = "";
        public int type;                  // atomic number
        public float x, y, z;             // Cartesian (Å po konwersji)
        public int freeze = 0;            // 0 = free, -1 = frozen (z Opt)
        public string name = "";          // opcjonalna etykieta
    }

 /// <summary>
 /// Parser files wsadowych Gaussian (.gjf / .com / .gau).
 /// Wyciąga:
 /// - Route section
 /// - Title
 /// - Charge / Multiplicity
 /// - Geometrię kartezjańską (Angstrom or Bohr)
 /// </summary>
    public static class GaussianParser
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

 // Dummy / ghost atoms
        private static readonly HashSet<string> _dummies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "X", "BQ", "XX", "GH"
        };

        public static bool Parse(string content, GaussianData gau)
        {
            Debug.WriteLine("[GaussianParser] ========== START PARSING GAUSSIAN INPUT ==========");

            if (string.IsNullOrEmpty(content) || gau == null)
                return false;

            gau.Clean();

            var lines = Regex.Split(content, "\r\n|\r|\n");
            int i = 0;

 // ---------- 1. Link0 lines (%...) ----------
            while (i < lines.Length)
            {
                string t = lines[i].Trim();
                if (t.StartsWith("%"))
                {
                    i++;
                    continue;
                }
                break;
            }

 // ---------- 2. Route section (#...) ----------
 // Może be wieloliniowa
            var routeLines = new List<string>();
            while (i < lines.Length)
            {
                string t = lines[i].Trim();
                if (string.IsNullOrEmpty(t))
                {
                    i++;
                    break;          // empty line ends Route
                }
                if (t.StartsWith("#"))
                {
                    routeLines.Add(t);
                    i++;
                    continue;
                }
 // Czasem Route without # na kolejnych liniach
                if (routeLines.Count > 0)
                {
                    routeLines.Add(t);
                    i++;
                    continue;
                }
                break;
            }
            gau.Route = string.Join(" ", routeLines).Trim();

 // Skip ewentualne empty linie
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i])) i++;

 // ---------- 3. Title ----------
            if (i < lines.Length)
            {
                gau.Title = lines[i].Trim();
                i++;
            }

 // Skip empty linie after tytule
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i])) i++;

 // ---------- 4. Charge Multiplicity ----------
            if (i < lines.Length)
            {
                var cm = Regex.Split(lines[i].Trim(), @"\s+");
                if (cm.Length >= 2)
                {
                    int.TryParse(cm[0], out int chg);
                    int.TryParse(cm[1], out int mult);
                    gau.Charge = chg;
                    gau.Multiplicity = mult;
                }
                i++;
            }

 // ---------- 5. Geometry (Cartesian) ----------
 // Formaty:
 // C 0.000 0.000 0.000
 // C 0 0.000 0.000 0.000 (z freeze flag)
 // 6 0.000 0.000 0.000 (numer atomowy)
 // C(Fragment=1) 0.0 0.0 0.0 (rzadziej)

            while (i < lines.Length)
            {
                string line = lines[i].Trim();
                i++;

 // Koniec geometrii – empty linia or new sekcja
                if (string.IsNullOrWhiteSpace(line))
                    break;
                if (line.StartsWith("--") || line.StartsWith("Variables:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("Constants:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("Recover", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("The", StringComparison.OrdinalIgnoreCase))
                    break;

                var atom = ParseCartesianAtom(line);
                if (atom != null)
                    gau.atom.Add(atom);
                else
                {
 // If linia nie pasuje do atom – prawdoafterdobnie koniec bloku
 // (np. Z-matrix or connectivity)
                    break;
                }
            }

 // Konversion Bohr → Å if wykryto
            if (gau.isBohr)
            {
                const float bohr2ang = 0.529177210903f;
                foreach (var a in gau.atom)
                {
                    a.x *= bohr2ang;
                    a.y *= bohr2ang;
                    a.z *= bohr2ang;
                }
                Debug.WriteLine("[GaussianParser] Coordinates converted from Bohr to Å");
            }

            gau.completed = gau.atom.Count > 0;

            LogSummary(gau);
            return gau.completed;
        }

        private static GaussianAtom ParseCartesianAtom(string line)
        {
 // Remove ewentualne komentarze at the end linii
            int excl = line.IndexOf('!');
            if (excl >= 0) line = line.Substring(0, excl).Trim();

            var parts = Regex.Split(line, @"\s+");
            if (parts.Length < 4) return null;

            try
            {
                string elemToken = parts[0];

 // Czasem element ma dodatkowe info: C(Fragment=1) or C-13
                elemToken = Regex.Replace(elemToken, @"\(.*\)", "");   // usuń (Fragment=…)
                elemToken = Regex.Replace(elemToken, @"-.*", "");      // usuń -13 (izotop)

                string element;
                int atomicNum = 0;

 // Czy to numer atomowy?
                if (int.TryParse(elemToken, out int z) && z > 0 && z <= 118)
                {
                    atomicNum = z;
                    element = z <= _elements.Length ? _elements[z - 1] : "X";
                }
                else
                {
                    element = elemToken;
 // Dummy atoms
                    if (_dummies.Contains(element))
                        element = "X";
                    atomicNum = GetAtomicNumber(element);
                }

                int idx = 1;
                int freeze = 0;

 // Opcjonalna flaga freeze (0 or -1)
                if (parts.Length >= 5 && (parts[1] == "0" || parts[1] == "-1"))
                {
                    int.TryParse(parts[1], out freeze);
                    idx = 2;
                }

                if (parts.Length < idx + 3) return null;

                if (!float.TryParse(parts[idx], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) return null;
                if (!float.TryParse(parts[idx + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) return null;
                if (!float.TryParse(parts[idx + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out float zcoord)) return null;

                return new GaussianAtom
                {
                    element = element,
                    type = atomicNum > 0 ? atomicNum : GetAtomicNumber(element),
                    x = x,
                    y = y,
                    z = zcoord,
                    freeze = freeze,
                    name = element
                };
            }
            catch
            {
                return null;
            }
        }

        #region Conversion

        public static StructureData ConvertToStructure(GaussianData gau, string filePath)
        {
            if (gau == null || !gau.completed) return null;

            string name = !string.IsNullOrEmpty(gau.Title)
                ? gau.Title
                : Path.GetFileNameWithoutExtension(filePath);

 // Skróć bardzo długie tytuły
            if (name.Length > 80) name = name.Substring(0, 77) + "...";

            var structure = new StructureData
            {
                Name = name,
                FilePath = filePath,
                a = 1f,
                b = 1f,
                c = 1f,
                alpha = 90f,
                beta = 90f,
                gamma = 90f,
                SpaceGroup = 1,
                SpaceGroupName = "P1",
                ChemicalName = gau.Title
            };

            structure.SymmetryOperations.Add("x,y,z");

            int idx = 1;
            foreach (var ga in gau.atom)
            {
 // Aftermicavities dummy atoms (X, Bq) – optionally
                if (ga.element.Equals("X", StringComparison.OrdinalIgnoreCase) ||
                    ga.element.Equals("Bq", StringComparison.OrdinalIgnoreCase))
                    continue;

                var atom = new SimpleAtom
                {
                    Name = $"{ga.element}{idx++}",
                    Symbol = ga.element,
                    AtomicNumber = ga.type,
                    X = ga.x,
                    Y = ga.y,
                    Z = ga.z,
                    Occupancy = 1f,
                    iso = 0.05f,
                    AdpType = "Uiso"
                };
                structure.Atoms.Add(atom);
            }

            return structure;
        }

        #endregion

        #region Utilities

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

        private static void LogSummary(GaussianData gau)
        {
            Debug.WriteLine("[GaussianParser] ══════════════════════════════════════");
            Debug.WriteLine($"[GaussianParser] File: {gau.FileName}");
            Debug.WriteLine($"[GaussianParser] Title: {gau.Title}");
            Debug.WriteLine($"[GaussianParser] Route: {gau.Route}");
            Debug.WriteLine($"[GaussianParser] Charge: {gau.Charge}   Multiplicity: {gau.Multiplicity}");
            Debug.WriteLine($"[GaussianParser] Atoms: {gau.atom.Count}");
            Debug.WriteLine("[GaussianParser] ══════════════════════════════════════");
        }

        #endregion
    }
}