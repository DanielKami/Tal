// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure file parsers: PdbParser.

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
 /// <summary>
 /// Data container for a parsed PDB file.
 /// </summary>
    public class PdbData
    {
        public string FileName { get; set; } = "";
        public string Title { get; set; } = "";

 // Unit cell (from CRYST1)
        public float a, b, c;
        public float alp, bet, gam;
        public float cell_volume;
        public int space_group_IT_number;
        public string space_group_name_HM = "";

        public List<PdbAtom> atom = new List<PdbAtom>();
        public List<string> SymmetryOperations = new List<string>();
        public List<(int, int)> Bonds = new List<(int, int)>(); // 1-based atom indices from CONECT

        public bool completed;

        public void Clean()
        {
            completed = false;
            a = b = c = 0;
            alp = bet = gam = 0;
            cell_volume = 0;
            space_group_IT_number = 0;
            space_group_name_HM = "";
            Title = "";
            atom?.Clear();
            SymmetryOperations?.Clear();
            Bonds?.Clear();
        }
    }

    public class PdbAtom
    {
        public string name = "";          // atom name (e.g. "S1", "C7")
        public string element = "";       // element symbol
        public int type;                  // atomic number
        public float x, y, z;             // Cartesian (Å)
        public float x_e, y_e, z_e;
        public float occ = 1.0f;
        public float iso = 0.05f;         // B-factor converted roughly or Uiso
        public string adp_type = "Uiso";
        public float u11, u22, u33, u12, u13, u23;
        public float u11_e, u22_e, u33_e, u12_e, u13_e, u23_e;
        public bool has_aniso = false;
        public int serial;                // PDB serial number
        public string residue = "UNK";
        public int resSeq = 1;
    }

 /// <summary>
 /// Parser for Protein Data Bank (PDB) files.
 /// Handles CRYST1, ATOM/HETATM, ANISOU, CONECT.
 /// Coordinates are Cartesian → converted to fractional when possible.
 /// </summary>
    public static class PdbParser
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

        public static bool Parse(string content, PdbData pdb)
        {
            Debug.WriteLine("[PdbParser] ========== START PARSING PDB ==========");

            if (string.IsNullOrEmpty(content) || pdb == null)
                return false;

            pdb.Clean();

            var lines = Regex.Split(content, "\r\n|\r|\n");
            Debug.WriteLine($"[PdbParser] Lines: {lines.Length}");

 // Temafterrary storage for ANISOU (keyed by serial)
            var anisoDict = new Dictionary<int, (float u11, float u22, float u33, float u12, float u13, float u23)>();

            foreach (var rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine)) continue;
                string record = rawLine.Length >= 6 ? rawLine.Substring(0, 6).Trim() : rawLine.Trim();

                switch (record)
                {
                    case "HEADER":
                    case "TITLE":
                        if (string.IsNullOrEmpty(pdb.Title))
                            pdb.Title = rawLine.Length > 10 ? rawLine.Substring(10).Trim() : "";
                        break;

                    case "CRYST1":
                        ParseCryst1(rawLine, pdb);
                        break;

                    case "ATOM":
                    case "HETATM":
                        var atom = ParseAtomRecord(rawLine);
                        if (atom != null)
                            pdb.atom.Add(atom);
                        break;

                    case "ANISOU":
                        ParseAnisou(rawLine, anisoDict);
                        break;

                    case "CONECT":
                        ParseConect(rawLine, pdb);
                        break;

                    case "END":
                    case "ENDMDL":
                        goto done;
                }
            }

        done:
 // Assign ANISOU data
            foreach (var atom in pdb.atom)
            {
                if (anisoDict.TryGetValue(atom.serial, out var u))
                {
 // PDB ANISOU is in 10^-4 Å²
                    atom.u11 = u.u11 / 10000f;
                    atom.u22 = u.u22 / 10000f;
                    atom.u33 = u.u33 / 10000f;
                    atom.u12 = u.u12 / 10000f;
                    atom.u13 = u.u13 / 10000f;
                    atom.u23 = u.u23 / 10000f;
                    atom.has_aniso = true;
                    atom.adp_type = "Uani";
                    atom.iso = (atom.u11 + atom.u22 + atom.u33) / 3.0f;
                }
                else if (atom.iso < 0.0001f)
                {
                    atom.iso = 0.05f;
                }
            }

 // Volume
            if (pdb.a > 0 && pdb.b > 0 && pdb.c > 0)
            {
                pdb.cell_volume = CalculateCellVolume(pdb.a, pdb.b, pdb.c,
                    MathHelper.ToRadians(pdb.alp),
                    MathHelper.ToRadians(pdb.bet),
                    MathHelper.ToRadians(pdb.gam));
            }

 // Fallback space group
            if (pdb.space_group_IT_number == 0 && !string.IsNullOrEmpty(pdb.space_group_name_HM))
            {
                string normalized = SymmetryElements.NormalizeSpaceGroupName(pdb.space_group_name_HM);
                if (SymmetryElements.GroupNameToNumber.TryGetValue(normalized, out int num))
                {
                    pdb.space_group_IT_number = num;
                }
            }

 // Default identity if no symmetry
            if (pdb.SymmetryOperations.Count == 0)
                pdb.SymmetryOperations.Add("x,y,z");

            pdb.completed = pdb.atom.Count > 0;

            LogSummary(pdb);
            return pdb.completed;
        }

        #region Private parsers

        private static void ParseCryst1(string line, PdbData pdb)
        {
 // Columns (1-based PDB):
 // 7-15 a, 16-24 b, 25-33 c, 34-40 α, 41-47 β, 48-54 γ, 56-66 space group
            try
            {
                if (line.Length < 54) return;

                pdb.a = ParseFloat(line, 6, 9);
                pdb.b = ParseFloat(line, 15, 9);
                pdb.c = ParseFloat(line, 24, 9);
                pdb.alp = ParseFloat(line, 33, 7);
                pdb.bet = ParseFloat(line, 40, 7);
                pdb.gam = ParseFloat(line, 47, 7);

                if (line.Length >= 66)
                    pdb.space_group_name_HM = line.Substring(55, Math.Min(11, line.Length - 55)).Trim();

                Debug.WriteLine($"[PdbParser] CRYST1: a={pdb.a:F4} b={pdb.b:F4} c={pdb.c:F4} " +
                                $"α={pdb.alp:F2} β={pdb.bet:F2} γ={pdb.gam:F2}  SG='{pdb.space_group_name_HM}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PdbParser] CRYST1 parse error: {ex.Message}");
            }
        }

        private static PdbAtom ParseAtomRecord(string line)
        {
 // ATOM/HETATM columns (1-based):
 // 7-11 serial, 13-16 name, 18-20 resName, 23-26 resSeq,
 // 31-38 x, 39-46 y, 47-54 z, 55-60 occ, 61-66 tempFactor, 77-78 element
            if (line.Length < 54) return null;

            try
            {
                var atom = new PdbAtom();

                atom.serial = ParseInt(line, 6, 5);
                atom.name = SafeSubstring(line, 12, 4).Trim();
                atom.residue = SafeSubstring(line, 17, 3).Trim();
                atom.resSeq = ParseInt(line, 22, 4);

                atom.x = ParseFloat(line, 30, 8);
                atom.y = ParseFloat(line, 38, 8);
                atom.z = ParseFloat(line, 46, 8);

                if (line.Length >= 60)
                    atom.occ = ParseFloat(line, 54, 6);
                if (line.Length >= 66)
                {
                    float bfac = ParseFloat(line, 60, 6);
 // Rough conversion B → Uiso (B = 8π²U)
                    atom.iso = bfac / (8f * (float)Math.PI * (float)Math.PI);
                    if (atom.iso < 0.001f) atom.iso = 0.05f;
                }

 // Element (preferred columns 77-78)
                string elem = "";
                if (line.Length >= 78)
                    elem = SafeSubstring(line, 76, 2).Trim();

                if (string.IsNullOrEmpty(elem))
                {
 // Fallback from atom name
                    elem = Regex.Replace(atom.name, @"[\d\s]+", "");
                    if (elem.Length > 2) elem = elem.Substring(0, 1);
                }

                atom.element = elem;
                atom.type = GetAtomicNumber(elem);

                return atom;
            }
            catch
            {
                return null;
            }
        }

        private static void ParseAnisou(string line, Dictionary<int, (float, float, float, float, float, float)> dict)
        {
 // ANISOU: serial 7-11, then U11 U22 U33 U12 U13 U23 (scaled by 10^4)
            if (line.Length < 70) return;

            try
            {
                int serial = ParseInt(line, 6, 5);
                float u11 = ParseFloat(line, 28, 7);
                float u22 = ParseFloat(line, 35, 7);
                float u33 = ParseFloat(line, 42, 7);
                float u12 = ParseFloat(line, 49, 7);
                float u13 = ParseFloat(line, 56, 7);
                float u23 = ParseFloat(line, 63, 7);

                dict[serial] = (u11, u22, u33, u12, u13, u23);
            }
            catch { /* ignore malformed */ }
        }

        private static void ParseConect(string line, PdbData pdb)
        {
 // CONECT serial 7-11, then up to 4 bonded serials
            if (line.Length < 11) return;

            try
            {
                int serial = ParseInt(line, 6, 5);
                for (int col = 11; col + 5 <= line.Length; col += 5)
                {
                    int bonded = ParseInt(line, col, 5);
                    if (bonded > 0)
                        pdb.Bonds.Add((serial, bonded));
                }
            }
            catch { }
        }

        #endregion

        #region Conversion to StructureData / Molecule helpers

        public static StructureData ConvertToStructure(PdbData pdb, string filePath)
        {
            if (pdb == null || !pdb.completed) return null;

            var structure = new StructureData
            {
                Name = !string.IsNullOrEmpty(pdb.Title) ? pdb.Title : Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath,
                a = pdb.a,
                b = pdb.b,
                c = pdb.c,
                alpha = pdb.alp,
                beta = pdb.bet,
                gamma = pdb.gam,
                Volume = pdb.cell_volume,
                SpaceGroup = pdb.space_group_IT_number,
                SpaceGroupName = pdb.space_group_name_HM ?? "",
                ChemicalName = pdb.Title
            };

            structure.SymmetryOperations.AddRange(pdb.SymmetryOperations);

 // Convert Cartesian → fractional if cell is known
            Matrix cartToFrac = Matrix.Identity;
            bool hasCell = pdb.a > 0.1f && pdb.b > 0.1f && pdb.c > 0.1f;

            if (hasCell)
            {
 // Build fractional ← Cartesian matrix (inverse of the usual transformation)
                cartToFrac = BuildCartesianToFractional(pdb.a, pdb.b, pdb.c,
                    MathHelper.ToRadians(pdb.alp),
                    MathHelper.ToRadians(pdb.bet),
                    MathHelper.ToRadians(pdb.gam));
            }

            foreach (var pa in pdb.atom)
            {
                Vector3 cart = new Vector3(pa.x, pa.y, pa.z);
                Vector3 frac = hasCell ? Vector3.Transform(cart, cartToFrac) : cart;

                var atom = new SimpleAtom
                {
                    Name = pa.name,
                    Symbol = pa.element,
                    AtomicNumber = pa.type,
                    X = frac.X,
                    Y = frac.Y,
                    Z = frac.Z,
                    Occupancy = pa.occ,
                    iso = pa.iso,
                    AdpType = pa.adp_type,
                    U11 = pa.u11,
                    U22 = pa.u22,
                    U33 = pa.u33,
                    U12 = pa.u12,
                    U13 = pa.u13,
                    U23 = pa.u23
                };
                structure.Atoms.Add(atom);
            }

            return structure;
        }

 /// <summary>
 /// Builds the Cartesian → fractional transformation matrix.
 /// </summary>
        private static Matrix BuildCartesianToFractional(float a, float b, float c,
                                                          float alpha, float beta, float gamma)
        {
 // Standard crystallographic transformation (orthogonal → fractional)
            float cosA = (float)Math.Cos(alpha);
            float cosB = (float)Math.Cos(beta);
            float cosG = (float)Math.Cos(gamma);
            float sinG = (float)Math.Sin(gamma);

            float v = (float)Math.Sqrt(1 - cosA * cosA - cosB * cosB - cosG * cosG + 2 * cosA * cosB * cosG);

 // Matrix that converts fractional → Cartesian (columns are a, b, c vectors)
            Matrix fracToCart = new Matrix(
                a, b * cosG, c * cosB, 0,
                0, b * sinG, c * (cosA - cosB * cosG) / sinG, 0,
                0, 0, c * v / sinG, 0,
                0, 0, 0, 1);

            return Matrix.Invert(fracToCart);
        }

        #endregion

        #region Utilities

        private static float ParseFloat(string line, int start, int length)
        {
            string s = SafeSubstring(line, start, length).Trim();
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                return v;
            return 0f;
        }

        private static int ParseInt(string line, int start, int length)
        {
            string s = SafeSubstring(line, start, length).Trim();
            if (int.TryParse(s, out int v)) return v;
            return 0;
        }

        private static string SafeSubstring(string s, int start, int length)
        {
            if (start >= s.Length) return "";
            if (start + length > s.Length) length = s.Length - start;
            return s.Substring(start, length);
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

        private static float CalculateCellVolume(float a, float b, float c, double alpha, double beta, double gamma)
        {
            double cosA = Math.Cos(alpha), cosB = Math.Cos(beta), cosG = Math.Cos(gamma);
            double vol = a * b * c * Math.Sqrt(1 - cosA * cosA - cosB * cosB - cosG * cosG + 2 * cosA * cosB * cosG);
            return (float)vol;
        }

        private static void LogSummary(PdbData pdb)
        {
            Debug.WriteLine("[PdbParser] ══════════════════════════════════════");
            Debug.WriteLine($"[PdbParser] File: {pdb.FileName}");
            Debug.WriteLine($"[PdbParser] Title: {pdb.Title}");
            Debug.WriteLine($"[PdbParser] Space group: {pdb.space_group_name_HM} (#{pdb.space_group_IT_number})");
            Debug.WriteLine($"[PdbParser] Cell: {pdb.a:F4} {pdb.b:F4} {pdb.c:F4}  α={pdb.alp:F2} β={pdb.bet:F2} γ={pdb.gam:F2}");
            Debug.WriteLine($"[PdbParser] Volume: {pdb.cell_volume:F2} Å³");
            Debug.WriteLine($"[PdbParser] Atoms: {pdb.atom.Count} (aniso: {pdb.atom.Count(a => a.has_aniso)})");
            Debug.WriteLine($"[PdbParser] Bonds (CONECT): {pdb.Bonds.Count}");
            Debug.WriteLine("[PdbParser] ══════════════════════════════════════");
        }

        #endregion
    }
}