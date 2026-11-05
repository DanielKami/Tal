// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure file parsers: Mol2Parser.

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
    public class Mol2Data
    {
        public string FileName { get; set; } = "";
        public string MoleculeName { get; set; } = "";
        public string Comment { get; set; } = "";
        public string MolType { get; set; } = "";      // SMALL / PROTEIN / ...
        public string ChargeType { get; set; } = "";   // NO_CHARGES / GASTEIGER / ...

 // Unit cell from @<TRIPOS>CRYSIN (optional)
        public float a, b, c;
        public float alp, bet, gam;
        public int space_group_IT_number;
        public int crystal_setting;                     // usually 1
        public float cell_volume;
        public string space_group_name_HM = "";

        public List<Mol2Atom> atom = new List<Mol2Atom>();
        public List<(int a1, int a2, string type)> Bonds = new List<(int, int, string)>(); // 1-based
        public List<string> SymmetryOperations = new List<string>();

        public bool completed;
        public bool hasCrysin;

        public void Clean()
        {
            completed = false;
            hasCrysin = false;
            MoleculeName = Comment = MolType = ChargeType = space_group_name_HM = "";
            a = b = c = alp = bet = gam = cell_volume = 0;
            space_group_IT_number = 0;
            crystal_setting = 1;
            atom?.Clear();
            Bonds?.Clear();
            SymmetryOperations?.Clear();
        }
    }

    public class Mol2Atom
    {
        public int id;                    // 1-based
        public string name = "";          // e.g. S1, C7, H4A
        public string sybylType = "";     // e.g. S.3, C.2, O.2, H
        public string element = "";       // extracted element
        public int type;                  // atomic number
        public float x, y, z;             // Cartesian
        public float charge = 0f;
        public int substId = 1;
        public string substName = "RES1";
        public float iso = 0.05f;
        public string adp_type = "Uiso";
    }

 /// <summary>
 /// Parser for Triafters MOL2 files (common CSD / Sybyl exafterrt).
 /// Supafterrts:
 /// @&lt;TRIPOS&gt;MOLECULE
 /// @&lt;TRIPOS&gt;ATOM
 /// @&lt;TRIPOS&gt;BOND
 /// @&lt;TRIPOS&gt;CRYSIN ← unit cell + space-group number
 /// Coordinates are Cartesian; converted to fractional when CRYSIN is present.
 /// </summary>
    public static class Mol2Parser
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

        public static bool Parse(string content, Mol2Data mol2)
        {
            Debug.WriteLine("[Mol2Parser] ========== START PARSING MOL2 ==========");

            if (string.IsNullOrEmpty(content) || mol2 == null)
                return false;

            mol2.Clean();

            var lines = Regex.Split(content, "\r\n|\r|\n");
            string currentSection = "";

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].TrimEnd();
                string trimmed = line.Trim();

 // Section headers
                if (trimmed.StartsWith("@<TRIPOS>", StringComparison.OrdinalIgnoreCase))
                {
                    currentSection = trimmed.Substring(9).Trim().ToUpperInvariant();
                    continue;
                }

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                    continue;

                switch (currentSection)
                {
                    case "MOLECULE":
                        ParseMoleculeSection(lines, ref i, mol2);
                        currentSection = ""; // section is multi-line, we advance i
                        break;

                    case "ATOM":
                        var atom = ParseAtomLine(trimmed);
                        if (atom != null)
                            mol2.atom.Add(atom);
                        break;

                    case "BOND":
                        var bond = ParseBondLine(trimmed);
                        if (bond.a1 > 0)
                            mol2.Bonds.Add(bond);
                        break;

                    case "CRYSIN":
                        ParseCrysin(trimmed, mol2);
                        break;

 // SUBSTRUCTURE, SET, etc. – ignore for now
                }
            }

 // Afterst-processing
            if (mol2.a > 0.1f && mol2.b > 0.1f && mol2.c > 0.1f)
            {
                mol2.cell_volume = CalculateCellVolume(mol2.a, mol2.b, mol2.c,
                    MathHelper.ToRadians(mol2.alp),
                    MathHelper.ToRadians(mol2.bet),
                    MathHelper.ToRadians(mol2.gam));
            }

 // Try to resolve space-group name from number
            if (mol2.space_group_IT_number > 0 && mol2.space_group_IT_number <= 230)
            {
 // Optional: if you have SymmetryElements.NumberToName dictionary
 // mol2.space_group_name_HM = SymmetryElements.GetName(mol2.space_group_IT_number);
            }

            if (mol2.SymmetryOperations.Count == 0)
                mol2.SymmetryOperations.Add("x,y,z");

            mol2.completed = mol2.atom.Count > 0;

            LogSummary(mol2);
            return mol2.completed;
        }

        #region Section parsers

        private static void ParseMoleculeSection(string[] lines, ref int i, Mol2Data mol2)
        {
 // After @<TRIPOS>MOLECULE the next lines are:
 // 1. molecule name
 // 2. num_atoms num_bonds num_subst num_feat num_sets
 // 3. mol_type
 // 4. charge_type
 // 5. status_bits (optional)
 // 6. comment (optional)

            if (i + 1 < lines.Length)
                mol2.MoleculeName = lines[++i].Trim();

            if (i + 1 < lines.Length)
            {
 // counts line – we already know from ATOM section, just skip
                i++;
            }

            if (i + 1 < lines.Length)
                mol2.MolType = lines[++i].Trim();

            if (i + 1 < lines.Length)
                mol2.ChargeType = lines[++i].Trim();

 // Optional status + comment
            while (i + 1 < lines.Length)
            {
                string next = lines[i + 1].Trim();
                if (next.StartsWith("@<TRIPOS>", StringComparison.OrdinalIgnoreCase))
                    break;
                i++;
                if (!string.IsNullOrEmpty(next) && next != "****")
                    mol2.Comment += (string.IsNullOrEmpty(mol2.Comment) ? "" : " ") + next;
            }
        }

        private static Mol2Atom ParseAtomLine(string line)
        {
 // atom_id atom_name x y z atom_type [subst_id [subst_name [charge [status]]]]
            var parts = Regex.Split(line.Trim(), @"\s+");
            if (parts.Length < 6) return null;

            try
            {
                var atom = new Mol2Atom();

                if (!int.TryParse(parts[0], out atom.id)) return null;
                atom.name = parts[1];

                if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out atom.x)) return null;
                if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out atom.y)) return null;
                if (!float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out atom.z)) return null;

                atom.sybylType = parts[5];
                atom.element = ExtractElement(atom.sybylType);
                atom.type = GetAtomicNumber(atom.element);

                if (parts.Length >= 7)
                    int.TryParse(parts[6], out atom.substId);
                if (parts.Length >= 8)
                    atom.substName = parts[7];
                if (parts.Length >= 9)
                    float.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out atom.charge);

                return atom;
            }
            catch
            {
                return null;
            }
        }

        private static (int a1, int a2, string type) ParseBondLine(string line)
        {
 // bond_id origin_atom_id target_atom_id bond_type [status]
            var parts = Regex.Split(line.Trim(), @"\s+");
            if (parts.Length < 4) return (0, 0, "");

            int.TryParse(parts[1], out int a1);
            int.TryParse(parts[2], out int a2);
            string type = parts[3];          // "1", "2", "3", "am", "ar", "du", "un", "nc" ...

            return (a1, a2, type);
        }

        private static void ParseCrysin(string line, Mol2Data mol2)
        {
 // a b c alpha beta gamma space_group_number [setting]
            var parts = Regex.Split(line.Trim(), @"\s+");
            if (parts.Length < 7) return;

            try
            {
                mol2.a = float.Parse(parts[0], CultureInfo.InvariantCulture);
                mol2.b = float.Parse(parts[1], CultureInfo.InvariantCulture);
                mol2.c = float.Parse(parts[2], CultureInfo.InvariantCulture);
                mol2.alp = float.Parse(parts[3], CultureInfo.InvariantCulture);
                mol2.bet = float.Parse(parts[4], CultureInfo.InvariantCulture);
                mol2.gam = float.Parse(parts[5], CultureInfo.InvariantCulture);
                int.TryParse(parts[6], out mol2.space_group_IT_number);

                if (parts.Length >= 8)
                    int.TryParse(parts[7], out mol2.crystal_setting);

                mol2.hasCrysin = true;

                Debug.WriteLine($"[Mol2Parser] CRYSIN: a={mol2.a:F4} b={mol2.b:F4} c={mol2.c:F4} " +
                                $"α={mol2.alp:F2} β={mol2.bet:F2} γ={mol2.gam:F2}  SG#={mol2.space_group_IT_number}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Mol2Parser] CRYSIN parse error: {ex.Message}");
            }
        }

        #endregion

        #region Conversion to StructureData

        public static StructureData ConvertToStructure(Mol2Data mol2, string filePath)
        {
            if (mol2 == null || !mol2.completed) return null;

            string name = !string.IsNullOrEmpty(mol2.MoleculeName)
                ? mol2.MoleculeName
                : Path.GetFileNameWithoutExtension(filePath);

            var structure = new StructureData
            {
                Name = name,
                FilePath = filePath,
                a = mol2.a > 0.1f ? mol2.a : 1f,
                b = mol2.b > 0.1f ? mol2.b : 1f,
                c = mol2.c > 0.1f ? mol2.c : 1f,
                alpha = mol2.alp > 0.1f ? mol2.alp : 90f,
                beta = mol2.bet > 0.1f ? mol2.bet : 90f,
                gamma = mol2.gam > 0.1f ? mol2.gam : 90f,
                Volume = mol2.cell_volume,
                SpaceGroup = mol2.space_group_IT_number > 0 ? mol2.space_group_IT_number : 1,
                SpaceGroupName = mol2.space_group_name_HM,
                ChemicalName = mol2.MoleculeName
            };

            structure.SymmetryOperations.AddRange(mol2.SymmetryOperations);

            bool hasCell = mol2.hasCrysin && mol2.a > 0.1f;

            Matrix cartToFrac = Matrix.Identity;
            if (hasCell)
            {
                cartToFrac = BuildCartesianToFractional(mol2.a, mol2.b, mol2.c,
                    MathHelper.ToRadians(mol2.alp),
                    MathHelper.ToRadians(mol2.bet),
                    MathHelper.ToRadians(mol2.gam));
            }

            foreach (var ma in mol2.atom)
            {
                Vector3 cart = new Vector3(ma.x, ma.y, ma.z);
                Vector3 frac = hasCell ? Vector3.Transform(cart, cartToFrac) : cart;

                var atom = new SimpleAtom
                {
                    Name = ma.name,
                    Symbol = ma.element,
                    AtomicNumber = ma.type,
                    X = frac.X,
                    Y = frac.Y,
                    Z = frac.Z,
                    Occupancy = 1f,
                    iso = ma.iso,
                    AdpType = ma.adp_type
                };
                structure.Atoms.Add(atom);
            }

            return structure;
        }

        private static Matrix BuildCartesianToFractional(float a, float b, float c,
                                                          float alpha, float beta, float gamma)
        {
            float cosA = (float)Math.Cos(alpha);
            float cosB = (float)Math.Cos(beta);
            float cosG = (float)Math.Cos(gamma);
            float sinG = (float)Math.Sin(gamma);

            float v = (float)Math.Sqrt(1 - cosA * cosA - cosB * cosB - cosG * cosG + 2 * cosA * cosB * cosG);

            Matrix fracToCart = new Matrix(
                a, b * cosG, c * cosB, 0,
                0, b * sinG, c * (cosA - cosB * cosG) / sinG, 0,
                0, 0, c * v / sinG, 0,
                0, 0, 0, 1);

            return Matrix.Invert(fracToCart);
        }

        #endregion

        #region Utilities

 /// <summary>
 /// Extracts element symbol from Sybyl atom type (S.3 → S, C.ar → C, H → H, Cl → Cl)
 /// </summary>
        private static string ExtractElement(string sybylType)
        {
            if (string.IsNullOrEmpty(sybylType)) return "C";

 // Remove anything after '.' 
            int dot = sybylType.IndexOf('.');
            string core = dot >= 0 ? sybylType.Substring(0, dot) : sybylType;

 // Some types are just "H", "LP", "Du" ...
            core = core.Trim();
            if (core.Equals("LP", StringComparison.OrdinalIgnoreCase) ||
                core.Equals("Du", StringComparison.OrdinalIgnoreCase))
                return "H"; // lone pair / dummy → treat as H for now

            return core;
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

        private static void LogSummary(Mol2Data mol2)
        {
            Debug.WriteLine("[Mol2Parser] ══════════════════════════════════════");
            Debug.WriteLine($"[Mol2Parser] File: {mol2.FileName}");
            Debug.WriteLine($"[Mol2Parser] Molecule: {mol2.MoleculeName}");
            Debug.WriteLine($"[Mol2Parser] Type: {mol2.MolType}   Charges: {mol2.ChargeType}");
            Debug.WriteLine($"[Mol2Parser] Atoms: {mol2.atom.Count}   Bonds: {mol2.Bonds.Count}");
            if (mol2.hasCrysin)
            {
                Debug.WriteLine($"[Mol2Parser] CRYSIN cell: {mol2.a:F4} {mol2.b:F4} {mol2.c:F4}  " +
                                $"α={mol2.alp:F2} β={mol2.bet:F2} γ={mol2.gam:F2}");
                Debug.WriteLine($"[Mol2Parser] Space group # {mol2.space_group_IT_number}  Volume: {mol2.cell_volume:F2} Å³");
            }
            else
            {
                Debug.WriteLine("[Mol2Parser] No CRYSIN – treated as isolated molecule (P1)");
            }
            Debug.WriteLine("[Mol2Parser] ══════════════════════════════════════");
        }

        #endregion
    }
}