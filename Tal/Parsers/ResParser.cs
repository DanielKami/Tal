// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure file parsers: ResParser.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// EXTENDED: REM line parsing (refinement statistics) + hydrogen AFIX U_iso recalculation

using Microsoft.Xna.Framework;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Tal.Core;
using Tal.GUI;

namespace Tal.Parsers
{
 /// <summary>
 /// Parser for SHELX RES/INS files.
 /// Handles unit cell parameters, symmetry operations, atoms, refinement data
 /// (including REM comment lines with statistics), and metadata.
 /// Uses SymmetryElements for group resolution and validation.
 ///
 /// ⭐ NEW: Drugi przebieg — przeliczenie U_iso wodoru geometrycznego (AFIX)
 /// z U_eq neighbour: U_iso(H) = |multiplier| × U_eq(neighbor).
 /// </summary>
    public static class ResParser
    {
        #region Elements & Instructions

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

        private static readonly HashSet<string> _knownInstructions = new HashSet<string>
        {
            "TITL", "CELL", "LATT", "SYMM", "SFAC", "UNIT", "L.S.", "L.S",
            "ACTA", "BOND", "FMAP", "PLAN", "SHEL", "SIZE", "TEMP",
            "WGHT", "FVAR", "MORE", "ANIS", "PART", "HKLF", "REM",
            "DAMP", "STIR", "EXTI", "SWAT", "TWIN", "BASF", "EADP",
            "EXYZ", "EQIV", "HTAB", "ISOR", "SIMU", "DELU", "DFIX",
            "DANG", "FLAT", "SADI", "CHIV", "CONF", "MPLA", "RTAB",
            "RIGU", "SHELXA", "SHELXB", "END", "LIST", "OMIT", "AFIX",
            "Q", "ZERR", "MERG"
        };

 /// <summary>
 /// Radius afterszukiwań neighbour wodoru (Å) w drugim przebiegu AFIX.
 /// Kowalencyjne H-C/N/O: 0.92–1.10 Å; hydrogen bonds > 1.4 Å.
 /// </summary>
        private const float HYDROGEN_NEIGHBOR_SEARCH_RADIUS = 1.3f;

        #endregion

        #region Main Parse

 /// <summary>
 /// Parses the content of a SHELX RES/INS file and populates a ResData object.
 /// </summary>
 /// <param name="content">The complete file content.</param>
 /// <param name="res">The ResData object to populate.</param>
 /// <returns>True if parsing was successful and at least one atom was found.</returns>
        public static bool Parse(string content, ResData res)
        {
            Debug.WriteLine("[ResParser] ========================================");
            Debug.WriteLine("[ResParser] START PARSING RES/INS FILE");

            if (string.IsNullOrEmpty(content)) return false;
            if (res == null) return false;

            res.Clean();

            var lines = Regex.Split(content, "\r\n|\r|\n");
            Debug.WriteLine($"[ResParser] Lines: {lines.Length}");

            var mergedLines = MergeContinuationLines(lines);
            Debug.WriteLine($"[ResParser] Merged to {mergedLines.Count} lines");

            var sfacMapping = new Dictionary<int, string>();
            float currentPart = 1.0f;
            int currentPartNumber = 1;
            var fileSymmetryOps = new List<string>();

            for (int i = 0; i < mergedLines.Count; i++)
            {
                string line = mergedLines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

 // ⭐ REM lines — parse refinement statistics instead of skipping
                if (line.StartsWith("REM", StringComparison.OrdinalIgnoreCase))
                {
                    ParseRemLine(line, res);
                    continue;
                }

                if (line.Length < 3) continue;

                if (line.StartsWith("TITL", StringComparison.OrdinalIgnoreCase))
                {
                    ParseTITL(line, res);
                    continue;
                }

                if (line.StartsWith("CELL", StringComparison.OrdinalIgnoreCase))
                {
                    ParseCELL(line, res);
                    continue;
                }

                if (line.StartsWith("ZERR", StringComparison.OrdinalIgnoreCase))
                {
                    ParseZERR(line, res);
                    continue;
                }

                if (line.StartsWith("LATT", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"[ResParser] LATT: {line.Substring(4).Trim()}");
                    continue;
                }

                if (line.StartsWith("SYMM", StringComparison.OrdinalIgnoreCase))
                {
                    string op = line.Substring(4).Trim();
                    if (!string.IsNullOrEmpty(op))
                    {
                        fileSymmetryOps.Add(op);
                        Debug.WriteLine($"[ResParser] SYMM (file): '{op}'");
                    }
                    continue;
                }

                if (line.StartsWith("SFAC", StringComparison.OrdinalIgnoreCase))
                {
                    ParseSFAC(line, sfacMapping, res);
                    continue;
                }

                if (line.StartsWith("UNIT", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (line.StartsWith("FVAR", StringComparison.OrdinalIgnoreCase))
                {
                    ParseFVAR(line, res);
                    continue;
                }

                if (line.StartsWith("WGHT", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (line.StartsWith("PART", StringComparison.OrdinalIgnoreCase))
                {
                    ParsePART(line, ref currentPartNumber, ref currentPart);
                    continue;
                }

                if (line.StartsWith("ANIS", StringComparison.OrdinalIgnoreCase)) continue;

                if (line.StartsWith("TEMP", StringComparison.OrdinalIgnoreCase))
                {
                    ParseTEMP(line, res);
                    continue;
                }

                if (line.StartsWith("AFIX", StringComparison.OrdinalIgnoreCase)) continue;
                if (line.StartsWith("HKLF", StringComparison.OrdinalIgnoreCase)) continue;
                if (line.StartsWith("MERG", StringComparison.OrdinalIgnoreCase)) continue;

                if (IsKnownInstruction(line)) continue;

                if (line.StartsWith("END", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine("[ResParser] END - end of file");
                    break;
                }

                if (line.StartsWith("Q", StringComparison.OrdinalIgnoreCase)) continue;

 // Atom line
                if (IsAtomLine(line, sfacMapping))
                {
                    var atom = ParseAtomLine(line, sfacMapping, currentPart, currentPartNumber);
                    if (atom != null)
                    {
                        if (atom.occ >= 10.0f)
                        {
                            atom.occ -= 10.0f;
                            atom.is_fixed_occ = true;
                        }

                        if (sfacMapping.TryGetValue(atom.type, out string element))
                            atom.element = element;
                        else if (atom.is_hydrogen)
                            atom.element = "H";
                        else
                            atom.element = GetElementSymbol(atom.type);

                        res.atom.Add(atom);
                    }
                    continue;
                }
            }

 // ⭐ Drugi przebieg: przelicz U_iso wodoru geometrycznego (AFIX)
 // z U_eq neighbour: U_iso(H) = |multiplier| × U_eq(neighbor)
            RecalculateHydrogenUiso(res);

 // Symmetry strategy with validation
            DetermineSymmetryOperations(res, fileSymmetryOps);

 // Legacy refinement extraction (R-factors embedded in some RES files)
            ExtractRefinementData(content, res);

            res.cell_volume = CalculateCellVolume(res.a, res.b, res.c,
                MathHelper.ToRadians(res.alp),
                MathHelper.ToRadians(res.bet),
                MathHelper.ToRadians(res.gam));

            res.completed = res.atom.Count > 0;

            if (!res.completed)
            {
                Debug.WriteLine("[ResParser] ❌ No atoms found!");
                return false;
            }

            LogParsingSummary(res);
            return true;
        }

        #endregion

        #region Hydrogen U_iso Recalculation (AFIX second pass)

 /// <summary>
 /// Drugi przebieg: for each atom wodoru z AFIX multiplier
 /// (afix_multiplier &gt; 0), find najbliższy atom ciężki (C/N/O/S),
 /// oblicz jego U_eq = (U11 + U22 + U33) / 3 i set:
 ///
 /// U_iso(H) = afix_multiplier × U_eq(neighbor)
 ///
 /// Atomy wodoru without AFIX (multiplier == 0) są skipped —
 /// ich U_iso zostało afterdane wprost przez SHELX i jest already correct.
 /// </summary>
        private static void RecalculateHydrogenUiso(ResData res)
        {
            if (res?.atom == null || res.atom.Count == 0)
                return;

 // Zbuduj matrix frac → cart raz (użyta w każdym sąsiedztwie)
            Matrix fracToCart = BuildFracToCart(res);

            int totalHydrogens = 0;
            int recalcCount = 0;
            int notFoundCount = 0;
            int skippedCount = 0;

            foreach (var hAtom in res.atom)
            {
                if (!hAtom.is_hydrogen) continue;
                totalHydrogens++;

 // ⭐ Interesują nas TYLKO wodory z AFIX multiplier
                if (hAtom.afix_multiplier <= 0f)
                {
                    skippedCount++;
                    continue;
                }

 // ⭐ Find najbliższy atom ciężki w promieniu HYDROGEN_NEIGHBOR_SEARCH_RADIUS
                ResAtom nearest = null;
                float bestDistance = float.MaxValue;

                foreach (var other in res.atom)
                {
                    if (ReferenceEquals(other, hAtom)) continue;
                    if (other.is_hydrogen) continue;

 // Wektor frakcyjny z uwzględnieniem periodyczności
                    float dx = other.x - hAtom.x;
                    float dy = other.y - hAtom.y;
                    float dz = other.z - hAtom.z;

                    dx -= (float)Math.Round(dx);
                    dy -= (float)Math.Round(dy);
                    dz -= (float)Math.Round(dz);

                    Vector3 fracDelta = new Vector3(dx, dy, dz);
                    Vector3 cartDelta = Vector3.Transform(fracDelta, fracToCart);
                    float distance = cartDelta.Length();

                    if (distance < HYDROGEN_NEIGHBOR_SEARCH_RADIUS && distance < bestDistance)
                    {
                        bestDistance = distance;
                        nearest = other;
                    }
                }

                if (nearest == null)
                {
                    notFoundCount++;
                    Debug.WriteLine(
                        $"[ResParser][H-Uiso] '{hAtom.name}': " +
                        $"no neighbour found within radius {HYDROGEN_NEIGHBOR_SEARCH_RADIUS:F1} Å " +
                        $"(zostaje U_iso = {hAtom.iso:F6})");
                    continue;
                }

 // ⭐ U_eq neighbour
                float uEq;
                if (nearest.has_aniso)
                {
                    uEq = (nearest.u11 + nearest.u22 + nearest.u33) / 3.0f;
                }
                else
                {
                    uEq = nearest.iso;
                }

                if (uEq <= 0.0001f)
                {
                    Debug.WriteLine(
                        $"[ResParser][H-Uiso] '{hAtom.name}': neighbour '{nearest.name}' " +
                        $"ma U_eq = 0 — pomijam");
                    continue;
                }

 // ⭐ Finalne U_iso
                float newIso = hAtom.afix_multiplier * uEq;

                Debug.WriteLine(
                    $"[ResParser][H-Uiso] '{hAtom.name}': " +
                    $"AFIX = {hAtom.afix_multiplier:F2} × " +
                    $"U_eq('{nearest.name}', d={bestDistance:F3} Å, " +
                    $"U_eq={uEq:F4}) = {newIso:F6} Å²");

                hAtom.iso = newIso;
                recalcCount++;
            }

            Debug.WriteLine(
                $"[ResParser][H-Uiso] ⭐ Recalculated {recalcCount} hydrogens from AFIX, " +
                $"skipped {skippedCount} without AFIX, " +
                $"no neighbour found for {notFoundCount} (of {totalHydrogens} total)");
        }

 /// <summary>
 /// Builds matrix konversion frakcjonalne → Cartesian
 /// z parameterów cell w ResData.
 /// </summary>
        private static Matrix BuildFracToCart(ResData res)
        {
            double alpha = MathHelper.ToRadians(res.alp);
            double beta = MathHelper.ToRadians(res.bet);
            double gamma = MathHelper.ToRadians(res.gam);

            float a = res.a;
            float b = res.b;
            float c = res.c;

            double sinAlp = Math.Sin(alpha);
            double cosAlp = Math.Cos(alpha);
            double sinBet = Math.Sin(beta);
            double cosBet = Math.Cos(beta);
            double sinGam = Math.Sin(gamma);
            double cosGam = Math.Cos(gamma);

            double V = a * b * c * Math.Sqrt(
                1.0 - cosAlp * cosAlp - cosBet * cosBet - cosGam * cosGam
                + 2.0 * cosAlp * cosBet * cosGam);

            if (V < 1e-9)
                return Matrix.Identity;

            double cosAlpStar = (cosBet * cosGam - cosAlp) / (sinBet * sinGam);
            double sinAlpStar = Math.Sqrt(Math.Max(0.0, 1.0 - cosAlpStar * cosAlpStar));

            Matrix M = new Matrix();

            M.M11 = (float)a;
            M.M12 = (float)(b * cosGam);
            M.M13 = (float)(c * cosBet);
            M.M14 = 0;

            M.M21 = 0;
            M.M22 = (float)(b * sinGam);
            M.M23 = (float)(-c * sinBet * cosAlpStar);
            M.M24 = 0;

            M.M31 = 0;
            M.M32 = 0;
            M.M33 = (float)(c * sinBet * sinAlpStar);
            M.M34 = 0;

            M.M41 = 0; M.M42 = 0; M.M43 = 0; M.M44 = 1;

            return M;
        }

        #endregion

        #region REM Line Parsing

 /// <summary>
 /// Parses a REM line from a RES file. REM lines typically carry
 /// refinement statistics, F(000), and other metadata.
 /// </summary>
        private static void ParseRemLine(string line, ResData res)
        {
            if (string.IsNullOrEmpty(line)) return;

            string body = line.Length > 3 ? line.Substring(3).Trim() : "";
            if (string.IsNullOrEmpty(body)) return;

 // ─── R1 ──────────────────────────────────────────────
            var mR1 = Regex.Match(body,
                @"R1\s*=\s*([\d.]+)\s*for\s*(\d+)\s*Fo\s*>\s*4sig\s*\(\s*Fo\s*\)\s*and\s*([\d.]+)\s*for\s*all\s*(\d+)\s*data",
                RegexOptions.IgnoreCase);
            if (mR1.Success)
            {
                res.s_R_factor_gt = ParseFloat(mR1.Groups[1].Value);
                res.refine_nreflns_gt = ParseInt(mR1.Groups[2].Value);
                res._refine_ls_R_factor_all = ParseFloat(mR1.Groups[3].Value);
                res.refine_nreflns_all = ParseInt(mR1.Groups[4].Value);
                Debug.WriteLine($"[ResParser] REM R1 parsed: " +
                                $"R1(gt)={res.s_R_factor_gt:F4} ({res.refine_nreflns_gt}), " +
                                $"R1(all)={res._refine_ls_R_factor_all:F4} ({res.refine_nreflns_all})");
                return;
            }

 // ─── wR2 + GooF ──────────────────────────────────────
            var mWR2 = Regex.Match(body,
                @"wR2\s*=\s*([\d.]+)\s*,\s*GooF\s*=\s*S\s*=\s*([\d.]+)",
                RegexOptions.IgnoreCase);
            if (mWR2.Success)
            {
                res.wR2 = ParseFloat(mWR2.Groups[1].Value);
                res.goof = ParseFloat(mWR2.Groups[2].Value);
                Debug.WriteLine($"[ResParser] REM wR2/GooF parsed: wR2={res.wR2:F4}, GooF={res.goof:F3}");
                return;
            }

 // ─── wR2 fallback (without GooF) ─────────────────────────
            if (res.wR2 == 0)
            {
                var mWR2b = Regex.Match(body, @"wR2\s*=\s*([\d.]+)", RegexOptions.IgnoreCase);
                if (mWR2b.Success)
                {
                    res.wR2 = ParseFloat(mWR2b.Groups[1].Value);
                    Debug.WriteLine($"[ResParser] REM wR2 (fallback): {res.wR2:F4}");
                }
            }

 // ─── GooF fallback ───────────────────────────────────
            if (res.goof == 0)
            {
                var mGooF = Regex.Match(body, @"GooF\s*=\s*S?\s*=\s*([\d.]+)", RegexOptions.IgnoreCase);
                if (mGooF.Success)
                {
                    res.goof = ParseFloat(mGooF.Groups[1].Value);
                    Debug.WriteLine($"[ResParser] REM GooF (fallback): {res.goof:F3}");
                }
            }

 // ─── parameters / restraints ─────────────────────────
            var mParams = Regex.Match(body,
                @"(\d+)\s*parameters?\s*refined\s*using\s*(\d+)\s*restraints?",
                RegexOptions.IgnoreCase);
            if (mParams.Success)
            {
                res.refine_nparams = ParseInt(mParams.Groups[1].Value);
                res.refine_nrestraints = ParseInt(mParams.Groups[2].Value);
                Debug.WriteLine($"[ResParser] REM params/restraints parsed: " +
                                $"nparams={res.refine_nparams}, nrestraints={res.refine_nrestraints}");
                return;
            }

 // ─── difference density ──────────────────────────────
            var mDiff = Regex.Match(body,
                @"Highest difference peak\s*([-\d.]+)\s*,\s*deepest hole\s*([-\d.]+)",
                RegexOptions.IgnoreCase);
            if (mDiff.Success)
            {
                res.refine_diff_max = ParseFloat(mDiff.Groups[1].Value);
                res.refine_diff_min = ParseFloat(mDiff.Groups[2].Value);
                Debug.WriteLine($"[ResParser] REM Δρ parsed: max={res.refine_diff_max:F3}, " +
                                $"min={res.refine_diff_min:F3}");
                return;
            }

 // ─── F(000) ──────────────────────────────────────────
            var mF000 = Regex.Match(body, @"F\s*\(\s*000\s*\)\s*=\s*([\d.]+)", RegexOptions.IgnoreCase);
            if (mF000.Success)
            {
                res.F_000 = ParseFloat(mF000.Groups[1].Value);
                Debug.WriteLine($"[ResParser] REM F(000) parsed: {res.F_000:F1}");
                return;
            }

 // ─── shift/su max ────────────────────────────────────
            var mShiftMax = Regex.Match(body,
                @"shift(?:/su)?[_\s]*max[_\s]*=\s*([\d.]+)", RegexOptions.IgnoreCase);
            if (mShiftMax.Success)
            {
                res.refine_shift_max = ParseFloat(mShiftMax.Groups[1].Value);
            }

 // ─── shift/su mean ───────────────────────────────────
            var mShiftMean = Regex.Match(body,
                @"shift(?:/su)?[_\s]*mean[_\s]*=\s*([\d.]+)", RegexOptions.IgnoreCase);
            if (mShiftMean.Success)
            {
                res.refine_shift_mean = ParseFloat(mShiftMean.Groups[1].Value);
            }
        }

        private static float ParseFloat(string s)
        {
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;
        }

        private static int ParseInt(string s)
        {
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        #endregion

        #region Symmetry — VALIDATION STRATEGY

 /// <summary>
 /// Determines symmetry operations with VALIDATION against the internal database.
 /// </summary>
        private static void DetermineSymmetryOperations(ResData res, List<string> fileSymmetryOps)
        {
            Debug.WriteLine("[ResParser] ========== SYMMETRY DETERMINATION ==========");

            bool hasFileOps = fileSymmetryOps.Count > 0;
            bool hasGroupName = !string.IsNullOrEmpty(res.space_group_name_HM);

 // 1. Determine group number from TITL name
            int groupNumber = res.space_group_IT_number;
            string normalizedName = "";

            if (hasGroupName)
            {
                normalizedName = SymmetryElements.NormalizeSpaceGroupName(res.space_group_name_HM);

                if (SymmetryElements.GroupNameToNumber.TryGetValue(normalizedName, out int num))
                {
                    groupNumber = num;
                    res.space_group_IT_number = num;
                    Debug.WriteLine($"[ResParser] Group resolution: '{res.space_group_name_HM}' → '{normalizedName}' → #{num}");
                }
                else
                {
                    Debug.WriteLine($"[ResParser] ⚠️ Group name '{res.space_group_name_HM}' → '{normalizedName}' not found");
                }
            }

 // 2. Get expected ops from database
            List<SymmetryElements.ReadSymm> dbOps = null;
            if (groupNumber > 0)
            {
                dbOps = SymmetryElements.CreateTransformations(groupNumber, normalizedName, null);
                Debug.WriteLine($"[ResParser] Database has {dbOps?.Count ?? 0} ops for group #{groupNumber}");
            }

 // 3. Count file ops (with identity check)
            bool fileHasIdentity = false;
            foreach (var op in fileSymmetryOps)
            {
                string clean = op.Replace(" ", "").ToLowerInvariant();
                if (clean == "x,y,z" || clean == "x,y,z,")
                {
                    fileHasIdentity = true;
                    break;
                }
            }

            int fileOpsCount = fileSymmetryOps.Count + (fileHasIdentity ? 0 : 1);
            int dbOpsCount = dbOps?.Count ?? 0;

            Debug.WriteLine($"[ResParser] File ops: {fileSymmetryOps.Count} (+identity={!fileHasIdentity}) → effective {fileOpsCount}");
            Debug.WriteLine($"[ResParser] DB ops: {dbOpsCount}");

 // 4. Decide source
            bool useFile = false;

            if (hasFileOps)
            {
                if (dbOpsCount == 0)
                {
                    useFile = true;
                    Debug.WriteLine("[ResParser] No DB reference — trusting FILE");
                }
                else if (fileOpsCount >= dbOpsCount)
                {
                    useFile = true;
                    Debug.WriteLine($"[ResParser] ✅ FILE has enough ops ({fileOpsCount} >= {dbOpsCount})");
                }
                else
                {
                    useFile = false;
                    Debug.WriteLine($"[ResParser] ⚠️ FILE has too few ops ({fileOpsCount} < {dbOpsCount}) — using DATABASE");
                }
            }

 // 5. Apply decision
            res.SymmetryOperations.Clear();

            if (useFile)
            {
                res.SymmetryOperations.AddRange(fileSymmetryOps);
                EnsureIdentityFirst(res.SymmetryOperations);
                res.symmetry_source = "file";
                Debug.WriteLine($"[ResParser] ✅ Using FILE ops ({res.SymmetryOperations.Count})");
            }
            else if (dbOps != null && dbOps.Count > 0)
            {
                foreach (var op in dbOps)
                {
                    res.SymmetryOperations.Add(ConvertSymmToString(op));
                }
                res.symmetry_source = "database";
                Debug.WriteLine($"[ResParser] ✅ Using DATABASE ops ({res.SymmetryOperations.Count})");
            }
            else
            {
                res.SymmetryOperations.Add("x,y,z");
                res.symmetry_source = "fallback";
                Debug.WriteLine("[ResParser] ⚠️ FALLBACK: identity only");
            }

 // 6. Debug output
            Debug.WriteLine($"[ResParser] Final symmetry ops ({res.symmetry_source}, {res.SymmetryOperations.Count}):");
            for (int i = 0; i < res.SymmetryOperations.Count; i++)
            {
                Debug.WriteLine($"[ResParser]   [{i}] '{res.SymmetryOperations[i]}'");
            }
            Debug.WriteLine("[ResParser] ========================================");
        }

 /// <summary>
 /// Ensures "x,y,z" is at aftersition 0.
 /// </summary>
        private static void EnsureIdentityFirst(List<string> ops)
        {
            int identityIndex = -1;
            for (int i = 0; i < ops.Count; i++)
            {
                string clean = ops[i].Replace(" ", "").ToLowerInvariant();
                if (clean == "x,y,z" || clean == "x,y,z,")
                {
                    identityIndex = i;
                    break;
                }
            }

            if (identityIndex > 0)
            {
                string identity = ops[identityIndex];
                ops.RemoveAt(identityIndex);
                ops.Insert(0, identity);
                Debug.WriteLine("[ResParser] Moved identity to front");
            }
            else if (identityIndex < 0)
            {
                ops.Insert(0, "x,y,z");
                Debug.WriteLine("[ResParser] Inserted identity at front");
            }
        }

 /// <summary>
 /// Converts ReadSymm to a string like "x,y,z", "-x+1/2,y+1/2,-z+1/2".
 /// </summary>
        private static string ConvertSymmToString(SymmetryElements.ReadSymm op)
        {
            string[] axes = new string[3];
            string[] varNames = { "x", "y", "z" };

            for (int axis = 0; axis < 3; axis++)
            {
                var sb = new System.Text.StringBuilder();
                bool hasTerm = false;

                if (op.sign1[axis] != 0 && op.pos1[axis] >= 0 && op.pos1[axis] < 3)
                {
                    if (op.sign1[axis] < 0) sb.Append('-');
                    sb.Append(varNames[op.pos1[axis]]);
                    hasTerm = true;
                }

                if (op.sign2[axis] != 0 && op.pos2[axis] >= 0 && op.pos2[axis] < 3)
                {
                    if (hasTerm && op.sign2[axis] > 0) sb.Append('+');
                    if (op.sign2[axis] < 0) sb.Append('-');
                    sb.Append(varNames[op.pos2[axis]]);
                    hasTerm = true;
                }

                if (Math.Abs(op.shift[axis]) > 1e-6f)
                {
                    if (hasTerm && op.shift[axis] > 0) sb.Append('+');
                    if (op.shift[axis] < 0)
                    {
                        sb.Append('-');
                        sb.Append(FormatFraction(Math.Abs(op.shift[axis])));
                    }
                    else
                    {
                        sb.Append(FormatFraction(op.shift[axis]));
                    }
                    hasTerm = true;
                }

                axes[axis] = hasTerm ? sb.ToString() : "0";
            }

            return $"{axes[0]},{axes[1]},{axes[2]}";
        }

        private static string FormatFraction(float value)
        {
            float[] knownFractions = { 0.25f, 1f / 3f, 0.5f, 2f / 3f, 0.75f };
            string[] knownStrings = { "1/4", "1/3", "1/2", "2/3", "3/4" };

            for (int i = 0; i < knownFractions.Length; i++)
            {
                if (Math.Abs(value - knownFractions[i]) < 0.01f)
                    return knownStrings[i];
            }

            return value.ToString("F4", CultureInfo.InvariantCulture);
        }

        #endregion

        #region Card Parsers

        private static void ParseTITL(string line, ResData res)
        {
            string titlContent = line.Substring(4).Trim();
            res.TITL = titlContent;
            Debug.WriteLine($"[ResParser] TITL: '{titlContent}'");

            string extractedGroup = ExtractSpaceGroupFromTITL(titlContent);
            if (!string.IsNullOrEmpty(extractedGroup))
            {
                res.space_group_name_HM = extractedGroup;

                string normalized = SymmetryElements.NormalizeSpaceGroupName(extractedGroup);

                if (SymmetryElements.GroupNameToNumber.TryGetValue(normalized, out int groupNumber))
                {
                    res.space_group_IT_number = groupNumber;
                    Debug.WriteLine($"[ResParser] ✅ Space group: '{extractedGroup}' → '{normalized}' (#{groupNumber})");
                }
                else
                {
                    Debug.WriteLine($"[ResParser] ⚠️ Unknown space group: '{extractedGroup}' → '{normalized}'");
                }
            }
            else
            {
                Debug.WriteLine($"[ResParser] ⚠️ Could not extract space group from TITL");
            }
        }

        private static void ParseCELL(string line, ResData res)
        {
            var parts = SplitLine(line.Substring(4));
            if (parts.Length >= 7)
            {
                res.wavelength = GetFloat(parts[0]);
                res.a = GetFloat(parts[1]);
                res.b = GetFloat(parts[2]);
                res.c = GetFloat(parts[3]);
                res.alp = GetFloat(parts[4]);
                res.bet = GetFloat(parts[5]);
                res.gam = GetFloat(parts[6]);

                ParseErrors(parts, out res.a_e, out res.b_e, out res.c_e,
                            out res.alp_e, out res.bet_e, out res.gam_e);

                Debug.WriteLine($"[ResParser] CELL: a={res.a:F4} b={res.b:F4} c={res.c:F4} " +
                                $"α={res.alp:F4} β={res.bet:F4} γ={res.gam:F4}");
            }
        }

        private static void ParseZERR(string line, ResData res)
        {
            var parts = SplitLine(line.Substring(4));
            if (parts.Length >= 1)
            {
                res.formula_units_Z = GetFloat(parts[0]);
                Debug.WriteLine($"[ResParser] ZERR: Z={res.formula_units_Z}");
            }
        }

 /// <summary>
 /// Parses SFAC card. Supafterrts both formats:
 /// SFAC C H N O
 /// SFAC C 0.003 H 0.001 N 0.006 O 0.004
 /// Skips numeric scattering-factor coefficients after each symbol.
 /// </summary>
        private static void ParseSFAC(string line, Dictionary<int, string> sfacMapping, ResData res)
        {
            var tokens = SplitLine(line.Substring(4));
            if (tokens.Length == 0) return;

            var symbols = new List<string>();

            int i = 0;
            while (i < tokens.Length)
            {
                string t = tokens[i];

 // Symbol elementu: litera + optionally cyfry (np. "C", "O", "C1")
                if (t.Length > 0 && char.IsLetter(t[0]))
                {
                    string cleanSymbol = Regex.Replace(t, @"\d", "");
                    if (!string.IsNullOrEmpty(cleanSymbol))
                    {
                        symbols.Add(cleanSymbol);

 // Skip ewentualne liczby after symbolu (współczynniki)
                        i++;
                        while (i < tokens.Length && IsNumeric(tokens[i]))
                            i++;
                        continue;
                    }
                }

                i++;
            }

            for (int idx = 0; idx < symbols.Count; idx++)
            {
                sfacMapping[idx + 1] = symbols[idx];
                res.sfacList.Add(symbols[idx]);
            }

            Debug.WriteLine($"[ResParser] SFAC: {string.Join(", ", res.sfacList)}");
        }

        private static bool IsNumeric(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            string clean = CleanNumber(s);
            return float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        private static void ParseFVAR(string line, ResData res)
        {
            var parts = SplitLine(line.Substring(4));
            if (parts.Length > 0)
            {
                res.global_scale_factor = GetFloat(parts[0]);
                Debug.WriteLine($"[ResParser] FVAR: {res.global_scale_factor:F4}");
            }
        }

        private static void ParsePART(string line, ref int partNumber, ref float partOcc)
        {
            var parts = SplitLine(line.Substring(4));
            if (parts.Length > 0)
            {
                partNumber = GetInt(parts[0]);
                partOcc = parts.Length > 1 ? GetFloat(parts[1]) : 1.0f;
                Debug.WriteLine($"[ResParser] PART: nr={partNumber}, occ={partOcc:F4}");
            }
        }

        private static void ParseTEMP(string line, ResData res)
        {
            var parts = SplitLine(line.Substring(4));
            if (parts.Length > 0)
            {
                res.measurement_temperature = GetFloat(parts[0]);
                Debug.WriteLine($"[ResParser] TEMP: {res.measurement_temperature:F2}K");
            }
        }

        #endregion

        #region Space Group Extraction

 /// <summary>
 /// Extracts space group name from TITL line.
 /// </summary>
        private static string ExtractSpaceGroupFromTITL(string titl)
        {
            if (string.IsNullOrEmpty(titl)) return null;

            var patterns = new[]
            {
                @"\b([PABCIFR][-\s]?[0-9]+(?:\([0-9]+\))?(?:\s*/\s*[a-z][0-9]?)?)\b",
                @"\b([PABCIFR]\s+[0-9]+(?:\s*/\s*[a-z])?\s*[0-9]?)\b",
                @"\b([PABCIFR][a-z]{2,}[-\s]?[0-9]*[a-z]?)\b",
                @"\b([PABCIFR][\w\-/()]+)\b"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(titl, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    string candidate = match.Groups[1].Value.Trim();
                    if (string.IsNullOrEmpty(candidate) || candidate.Length < 2)
                        continue;

                    string toCheck = candidate.Replace("(", "").Replace(")", "");
                    string normalized = SymmetryElements.NormalizeSpaceGroupName(toCheck);

                    if (SymmetryElements.GroupNameToNumber.ContainsKey(normalized))
                    {
                        Debug.WriteLine($"[ResParser] Extracted: '{candidate}' → '{normalized}'");
                        return candidate;
                    }
                }
            }

            Debug.WriteLine($"[ResParser] No space group found in: '{titl}'");
            return null;
        }

        #endregion

        #region Atom Line Parsing

        private static bool IsAtomLine(string line, Dictionary<int, string> sfacMapping)
        {
            if (string.IsNullOrEmpty(line)) return false;

            string trimmed = line.Trim();
            if (trimmed.EndsWith("="))
                trimmed = trimmed.Substring(0, trimmed.Length - 1).Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed == "=")
                return false;

            var parts = SplitLine(trimmed);
            if (parts.Length < 5) return false;

            string firstWord = parts[0].ToUpperInvariant();
            foreach (var instr in _knownInstructions)
            {
                if (firstWord == instr || firstWord.StartsWith(instr))
                    return false;
            }

            if (!char.IsUpper(parts[0][0])) return false;
            if (!int.TryParse(parts[1], out int type)) return false;
            if (!sfacMapping.ContainsKey(type)) return false;

            for (int i = 2; i < 5 && i < parts.Length; i++)
            {
                string clean = CleanNumber(parts[i]);
                if (!float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    return false;
            }

            return true;
        }

        private static ResAtom ParseAtomLine(string line, Dictionary<int, string> sfacMapping,
                                              float partOcc, int partNumber)
        {
            string cleanLine = line.Trim();
            if (cleanLine.EndsWith("="))
                cleanLine = cleanLine.Substring(0, cleanLine.Length - 1).Trim();

            var parts = SplitLine(cleanLine);
            if (parts.Length < 5) return null;

            var atom = new ResAtom();
            atom.name = parts[0];

            if (!int.TryParse(parts[1], out int type)) return null;
            atom.type = type;

            atom.x = GetFloat(parts[2]);
            atom.y = GetFloat(parts[3]);
            atom.z = GetFloat(parts[4]);

            atom.is_hydrogen = atom.name.StartsWith("H") || atom.type == 2;

            ParseCoordErrors(parts, out atom.x_e, out atom.y_e, out atom.z_e);

            int currentIndex = 5;

 // Occupancy
            if (parts.Length > currentIndex)
            {
                string occPart = CleanNumber(parts[currentIndex]);
                if (float.TryParse(occPart, NumberStyles.Float, CultureInfo.InvariantCulture, out float occ))
                {
                    atom.occ = occ;
                    atom.is_fixed_occ = occ >= 10.0f;
                    if (atom.is_fixed_occ) atom.occ -= 10.0f;
                    ParseSingleError(parts[currentIndex], out atom.occ_e);
                    currentIndex++;
                }
                else
                {
                    atom.occ = partOcc;
                }
            }
            else
            {
                atom.occ = partOcc;
            }

            int remaining = parts.Length - currentIndex;

            if (remaining >= 6)
            {
 // Anisotropic (6 values)
                atom.adp_type = "Uani";
                atom.has_aniso = true;

                atom.u11 = GetFloat(parts[currentIndex]);
                atom.u22 = GetFloat(parts[currentIndex + 1]);
                atom.u33 = GetFloat(parts[currentIndex + 2]);
                atom.u23 = GetFloat(parts[currentIndex + 3]);
                atom.u13 = GetFloat(parts[currentIndex + 4]);
                atom.u12 = GetFloat(parts[currentIndex + 5]);

                ParseSingleError(parts[currentIndex], out atom.u11_e);
                ParseSingleError(parts[currentIndex + 1], out atom.u22_e);
                ParseSingleError(parts[currentIndex + 2], out atom.u33_e);
                ParseSingleError(parts[currentIndex + 3], out atom.u23_e);
                ParseSingleError(parts[currentIndex + 4], out atom.u13_e);
                ParseSingleError(parts[currentIndex + 5], out atom.u12_e);

                atom.iso = (atom.u11 + atom.u22 + atom.u33) / 3.0f;
            }
            else if (remaining >= 1)
            {
 // ═══════════════════════════════════════════════════════════
 // ⭐ SHELX U-iso konwencja:
 //
 // iso >= 0: U_iso = iso (Å²) — withoutafterśrednia value
 // iso < 0: AFIX multiplier |iso| — U_iso zostanie
 // przeliczone w drugim przebiegu:
 // U_iso(H) = |iso| × U_eq(neighbor)
 // iso >= 10: U_iso = iso - 10 (fixed U marker)
 // ═══════════════════════════════════════════════════════════
                string isoPart = CleanNumber(parts[currentIndex]);
                if (float.TryParse(isoPart, NumberStyles.Float, CultureInfo.InvariantCulture, out float iso))
                {
                    if (iso < 0)
                    {
 // ⭐ AFIX multiplier — save oryginalną value,
 // U_iso zostanie przeliczone w RecalculateHydrogenUiso
                        atom.is_fixed_iso = true;
                        atom.afix_multiplier = Math.Abs(iso);

 // Tymczasowa value (fallback whenby nie znaleziono neighbour)
                        atom.iso = atom.afix_multiplier * 0.01f;

                        Debug.WriteLine(
                            $"[ResParser] Atom '{atom.name}': iso<0 ({iso:F4}) → " +
                            $"AFIX multiplier = {atom.afix_multiplier:F4} (do przeliczenia)");
                    }
                    else if (iso >= 10.0f)
                    {
                        atom.is_fixed_iso = true;
                        atom.iso = iso - 10.0f;

                        Debug.WriteLine(
                            $"[ResParser] Atom '{atom.name}': iso>=10 ({iso:F4}) → " +
                            $"U_iso = {atom.iso:F6} Å² (fixed marker)");
                    }
                    else
                    {
                        atom.iso = iso;
                    }

                    ParseSingleError(parts[currentIndex], out atom.iso_e);
                }
                else
                {
                    atom.iso = 0.05f;
                }
            }
            else
            {
                atom.iso = 0.05f;
            }

            atom.part_number = partNumber;
            atom.adp_type = atom.has_aniso ? "Uani" : "Uiso";

            if (sfacMapping.TryGetValue(atom.type, out string element))
                atom.element = element;
            else if (atom.is_hydrogen)
                atom.element = "H";
            else
                atom.element = GetElementSymbol(atom.type);

            return atom;
        }

        #endregion

        #region Conversion to Molecule

 /// <summary>
 /// Converts ResData to a Molecule.
 /// </summary>
        public static Molecule ConvertToMolecule(ResData resData)
        {
            if (resData == null || !resData.completed) return null;
            if (resData.atom == null || resData.atom.Count == 0) return null;

            var molecule = new Molecule();

            molecule.a = resData.a;
            molecule.b = resData.b;
            molecule.c = resData.c;
            molecule.alpha = MathHelper.ToRadians(resData.alp);
            molecule.beta = MathHelper.ToRadians(resData.bet);
            molecule.gamma = MathHelper.ToRadians(resData.gam);

            if (!string.IsNullOrEmpty(resData.space_group_name_HM))
            {
                molecule.SetSpaceGroupInfo(resData.space_group_name_HM, resData.space_group_IT_number);
            }

            string normalizedName = SymmetryElements.NormalizeSpaceGroupName(resData.space_group_name_HM);

            if (resData.symmetry_source == "database" && resData.space_group_IT_number > 0)
            {
                var symOps = SymmetryElements.CreateTransformations(
                    resData.space_group_IT_number,
                    normalizedName,
                    null);

                if (symOps != null && symOps.Count > 0)
                {
                    molecule.SymmetryOperations = symOps;
                    Debug.WriteLine($"[ResParser] Using {symOps.Count} ops from SymmetryElements DB");
                }
                else
                {
                    Debug.WriteLine("[ResParser] ⚠️ DB failed — falling back to file ops");
                    foreach (var op in resData.SymmetryOperations)
                        molecule.AddSymmetryOperationFromFile(op);
                }
            }
            else
            {
                foreach (var op in resData.SymmetryOperations)
                {
                    if (!string.IsNullOrEmpty(op))
                    {
                        molecule.AddSymmetryOperationFromFile(op);
                    }
                }
            }

            EnsureIdentityOperation(molecule);

            molecule.AsignXYZFromRes(resData);

            Debug.WriteLine($"[ResParser] AsymmetricPart.Count: {molecule.AsymmetricPart.Count}");

            if (molecule.AsymmetricPart.Count == 0)
            {
                Debug.WriteLine("[ResParser] ❌ No atoms in AsymmetricPart!");
                return null;
            }

            molecule.Presentation(Flags.Instance.GenerateEquvalents, Flags.Instance.Presentation);

            Debug.WriteLine($"[ResParser] ✅ Success: {molecule.NumberOfAtoms} atoms, {molecule.SymmetryOperations.Count} sym ops");
            return molecule;
        }

        private static void EnsureIdentityOperation(Molecule molecule)
        {
            bool hasIdentity = false;
            foreach (var op in molecule.SymmetryOperations)
            {
                if (op.sign1[0] == 1 && op.pos1[0] == 0 &&
                    op.sign1[1] == 1 && op.pos1[1] == 1 &&
                    op.sign1[2] == 1 && op.pos1[2] == 2 &&
                    op.sign2[0] == 0 && op.sign2[1] == 0 && op.sign2[2] == 0 &&
                    Math.Abs(op.shift[0]) < 0.001f &&
                    Math.Abs(op.shift[1]) < 0.001f &&
                    Math.Abs(op.shift[2]) < 0.001f)
                {
                    hasIdentity = true;
                    break;
                }
            }

            if (!hasIdentity)
            {
                var identity = new SymmetryElements.ReadSymm();
                identity.sign1 = new int[] { 1, 1, 1 };
                identity.sign2 = new int[] { 0, 0, 0 };
                identity.pos1 = new int[] { 0, 1, 2 };
                identity.pos2 = new int[] { 0, 0, 0 };
                identity.shift = new float[] { 0, 0, 0 };
                identity.orientation = 0;

                molecule.SymmetryOperations.Insert(0, identity);
                Debug.WriteLine("[ResParser] Added identity operation");
            }
        }

        #endregion

        #region Utility

        private static List<string> MergeContinuationLines(string[] lines)
        {
            var mergedLines = new List<string>();
            int skipCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                if (skipCount > 0) { skipCount--; continue; }

                string line = lines[i];

                if (line.TrimEnd().EndsWith("="))
                {
                    string combined = line.TrimEnd().Substring(0, line.TrimEnd().Length - 1).TrimEnd();

                    int nextIndex = i + 1;
                    while (nextIndex < lines.Length && lines[nextIndex].TrimEnd().EndsWith("="))
                    {
                        combined += " " + lines[nextIndex].TrimEnd().Substring(0, lines[nextIndex].TrimEnd().Length - 1).TrimEnd();
                        nextIndex++;
                    }

                    if (nextIndex < lines.Length)
                    {
                        combined += " " + lines[nextIndex].TrimStart();
                        skipCount = nextIndex - i;
                    }

                    mergedLines.Add(combined);
                }
                else
                {
                    mergedLines.Add(line);
                }
            }

            return mergedLines;
        }

        private static bool IsKnownInstruction(string line)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) return false;

            string firstWord = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0].ToUpperInvariant();

            foreach (var instr in _knownInstructions)
            {
                if (firstWord == instr || firstWord.StartsWith(instr))
                    return true;
            }
            return false;
        }

        private static string[] SplitLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return Array.Empty<string>();
            return line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static float GetFloat(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            string clean = CleanNumber(value);
            if (float.TryParse(clean, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;
            return 0;
        }

        private static int GetInt(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            string clean = CleanNumber(value);
            if (int.TryParse(clean, out int result))
                return result;
            return 0;
        }

        private static string CleanNumber(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            value = Regex.Replace(value, @"\([^)]*\)", "");
            value = value.Replace(",", "").Replace(";", "");
            return value.Trim();
        }

        private static void ParseSingleError(string value, out float error)
        {
            error = 0;
            if (string.IsNullOrEmpty(value)) return;
            var match = Regex.Match(value, @"\((\d+)\)");
            if (match.Success)
            {
                int digits = match.Groups[1].Value.Length;
                float mul = (float)Math.Pow(10, digits);
                float val = float.Parse(match.Groups[1].Value);
                error = val / mul;
            }
        }

        private static void ParseCoordErrors(string[] parts, out float xe, out float ye, out float ze)
        {
            xe = ye = ze = 0;
            if (parts.Length >= 5)
            {
                ParseSingleError(parts[2], out xe);
                ParseSingleError(parts[3], out ye);
                ParseSingleError(parts[4], out ze);
            }
        }

        private static void ParseErrors(string[] parts, out float ae, out float be, out float ce,
                                        out float alpe, out float bete, out float game)
        {
            ae = be = ce = alpe = bete = game = 0;
            if (parts.Length >= 4)
            {
                ParseSingleError(parts[1], out ae);
                ParseSingleError(parts[2], out be);
                ParseSingleError(parts[3], out ce);
            }
            if (parts.Length >= 7)
            {
                ParseSingleError(parts[4], out alpe);
                ParseSingleError(parts[5], out bete);
                ParseSingleError(parts[6], out game);
            }
        }

        private static void ExtractRefinementData(string content, ResData res)
        {
            var lines = Regex.Split(content, "\r\n|\r|\n");
            foreach (var line in lines)
            {
                var r1Match = Regex.Match(line, @"R1\s*=\s*([\d.]+).*for\s*(\d+).*Fo\s*>\s*4sig.*and\s*([\d.]+).*for\s*all\s*(\d+)");
                if (r1Match.Success)
                {
                    res.s_R_factor_gt = float.Parse(r1Match.Groups[1].Value, CultureInfo.InvariantCulture);
                    res._refine_ls_R_factor_all = float.Parse(r1Match.Groups[3].Value, CultureInfo.InvariantCulture);
                }

                var wR2Match = Regex.Match(line, @"wR2\s*=\s*([\d.]+)");
                if (wR2Match.Success)
                    res.wR2 = float.Parse(wR2Match.Groups[1].Value, CultureInfo.InvariantCulture);

                var goofMatch = Regex.Match(line, @"GooF\s*=\s*S\s*=\s*([\d.]+)");
                if (goofMatch.Success)
                    res.goof = float.Parse(goofMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            }
        }

        private static float CalculateCellVolume(float a, float b, float c,
                                                 double alpha, double beta, double gamma)
        {
            double cosAlp = Math.Cos(alpha);
            double cosBet = Math.Cos(beta);
            double cosGam = Math.Cos(gamma);

            double volume = a * b * c * Math.Sqrt(
                1.0 - cosAlp * cosAlp - cosBet * cosBet - cosGam * cosGam
                + 2.0 * cosAlp * cosBet * cosGam);

            return (float)volume;
        }

        private static string GetElementSymbol(int atomicNumber)
        {
            if (atomicNumber > 0 && atomicNumber <= _elements.Length)
                return _elements[atomicNumber - 1];
            return "C";
        }

        #endregion

        #region Logging

        private static void LogParsingSummary(ResData res)
        {
            Debug.WriteLine("[ResParser] ══════════════════════════════════════════");
            Debug.WriteLine($"[ResParser] File: {res.FileName}");
            Debug.WriteLine($"[ResParser] Space group: '{res.space_group_name_HM}' (#{res.space_group_IT_number})");
            Debug.WriteLine($"[ResParser] Symmetry source: {res.symmetry_source}");
            Debug.WriteLine($"[ResParser] Symmetry ops: {res.SymmetryOperations.Count}");
            for (int i = 0; i < Math.Min(res.SymmetryOperations.Count, 12); i++)
            {
                Debug.WriteLine($"[ResParser]   [{i}] '{res.SymmetryOperations[i]}'");
            }
            if (res.SymmetryOperations.Count > 12)
                Debug.WriteLine($"[ResParser]   ... and {res.SymmetryOperations.Count - 12} more");
            Debug.WriteLine($"[ResParser] Cell: a={res.a:F4} b={res.b:F4} c={res.c:F4} " +
                            $"α={res.alp:F2} β={res.bet:F2} γ={res.gam:F2}");
            Debug.WriteLine($"[ResParser] Volume: {res.cell_volume:F2} Å³");
            Debug.WriteLine($"[ResParser] Atoms: {res.atom.Count} (aniso: {res.atom.Count(a => a.has_aniso)})");

 // ⭐ Extended refinement summary
            Debug.WriteLine($"[ResParser] R1(F>4σ)={res.s_R_factor_gt:F4} R1(all)={res._refine_ls_R_factor_all:F4} " +
                            $"wR2={res.wR2:F4} GooF={res.goof:F3}");
            Debug.WriteLine($"[ResParser] Reflns: gt={res.refine_nreflns_gt}, all={res.refine_nreflns_all} " +
                            $"Params: {res.refine_nparams} Restraints: {res.refine_nrestraints}");
            Debug.WriteLine($"[ResParser] F(000)={res.F_000:F1} Δρ max/min={res.refine_diff_max:F3}/{res.refine_diff_min:F3}");
            Debug.WriteLine($"[ResParser] ══════════════════════════════════════════");

 // Also log to DebugWindow if it's open
            try
            {
                DebugWindow debugWin = null;
                foreach (var form in Application.OpenForms)
                {
                    if (form is DebugWindow dw) { debugWin = dw; break; }
                }
                if (debugWin == null) return;

                debugWin.Log($"📄 RES: {Path.GetFileName(res.FileName)}", System.Drawing.Color.Cyan);
                debugWin.Log($"  Space group: {res.space_group_name_HM} (#{res.space_group_IT_number})");
                debugWin.Log($"  Symmetry: {res.SymmetryOperations.Count} ops (source: {res.symmetry_source})");
                for (int i = 0; i < Math.Min(res.SymmetryOperations.Count, 8); i++)
                {
                    debugWin.Log($"    [{i}] '{res.SymmetryOperations[i]}'");
                }
                debugWin.Log($"  Cell: {res.a:F4} {res.b:F4} {res.c:F4}  α={res.alp:F2} β={res.bet:F2} γ={res.gam:F2}");
                debugWin.Log($"  Atoms: {res.atom.Count} (aniso: {res.atom.Count(a => a.has_aniso)})");

 // ⭐ Extended summary
                debugWin.Log($"  R1(F>4σ)={res.s_R_factor_gt:F4}  R1(all)={res._refine_ls_R_factor_all:F4}  " +
                             $"wR2={res.wR2:F4}  GooF={res.goof:F3}");
                if (res.refine_nreflns_all > 0 || res.refine_nparams > 0)
                    debugWin.Log($"  Reflns: {res.refine_nreflns_gt}/{res.refine_nreflns_all}  " +
                                 $"Params: {res.refine_nparams}  Restraints: {res.refine_nrestraints}");
                if (res.F_000 > 0)
                    debugWin.Log($"  F(000)={res.F_000:F1}");

                debugWin.Log("────────────────────────────────────────────", System.Drawing.Color.Gray);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ResParser] LogToDebugWindow error: {ex.Message}");
            }
        }

        #endregion
    }
}