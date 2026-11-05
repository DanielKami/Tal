// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: StoDatabase.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// StoDatabase v2 — cache LUT per symbol (shared between atomami tego samego typu).

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using Tal.Utils;

namespace Tal.Hirshfeld.Sto
{
    public static class StoDatabase
    {
        private static Dictionary<string, StoAtomData> _atoms;
        private static readonly object _lock = new object();

 // ⭐ Cache LUT — klucz: symbol (np. "C", "FE"), value: LUT
        private static Dictionary<string, StoDensityLut> _lutCache;
        private static readonly object _lutLock = new object();

        public static bool IsLoaded => _atoms != null && _atoms.Count > 0;
        public static int Count => _atoms?.Count ?? 0;

 // ═══════════════════════════════════════════════════════════════
 // Load (without changes)
 // ═══════════════════════════════════════════════════════════════

        public static void LoadAll(
            string neutralPath,
            string cationPath = null,
            string anionPath = null)
        {
            if (IsLoaded) return;

            lock (_lock)
            {
                if (IsLoaded) return;

                var combined = new Dictionary<string, StoAtomData>(
                    StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrEmpty(neutralPath) && File.Exists(neutralPath))
                    MergeJson(combined, neutralPath, chargeOverride: 0);
                else if (!string.IsNullOrEmpty(neutralPath))
                    throw new FileNotFoundException(
                        $"Neutral STO file not found: {neutralPath}");

                if (!string.IsNullOrEmpty(cationPath) && File.Exists(cationPath))
                    MergeJson(combined, cationPath, chargeOverride: +1);

                if (!string.IsNullOrEmpty(anionPath) && File.Exists(anionPath))
                    MergeJson(combined, anionPath, chargeOverride: -1);

                _atoms = combined;

 // ⭐ Zainicjuj empty cache LUT
                _lutCache = new Dictionary<string, StoDensityLut>(
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void Load(string jsonPath)
        {
            if (IsLoaded) return;

            lock (_lock)
            {
                if (IsLoaded) return;

                if (!File.Exists(jsonPath))
                    throw new FileNotFoundException($"STO database not found: {jsonPath}");

                string json = File.ReadAllText(jsonPath);
                _atoms = ParseJson(json);

                _lutCache = new Dictionary<string, StoDensityLut>(
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void LoadFromJson(string json)
        {
            lock (_lock)
            {
                _atoms = ParseJson(json);

                _lutCache = new Dictionary<string, StoDensityLut>(
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void EnsureLoaded()
        {
            if (IsLoaded) return;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? baseDir;

            string[] searchDirs =
            {
                exeDir,
                baseDir,
                Path.Combine(exeDir, "Data"),
                Path.Combine(baseDir, "Data"),
                Path.Combine(exeDir, "STO"),
                Path.Combine(baseDir, "STO"),
                Path.Combine(exeDir, "Content", "STO"),
                Path.Combine(baseDir, "Content", "STO"),
            };

            string FindFile(string name)
            {
                foreach (var dir in searchDirs)
                {
                    string p = Path.Combine(dir, name);
                    if (File.Exists(p)) return p;
                }
                return null;
            }

            string neutral = FindFile("sto_atoms_neutral.json");
            string cation = FindFile("sto_atoms_cation.json");
            string anion = FindFile("sto_atoms_anion.json");

            if (neutral == null)
            {
                ProgressReporter.Log(
                    "[STO] ❌ sto_atoms_neutral.json not found in any search path",
                    System.Drawing.Color.Orange);
                return;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            LoadAll(neutral, cation, anion);
            sw.Stop();

            ProgressReporter.Log(
                $"[STO] ✓ Loaded {Count} atoms in {sw.ElapsedMilliseconds} ms",
                System.Drawing.Color.LightGreen);
        }

 // ═══════════════════════════════════════════════════════════════
 // Get + LUT cache
 // ═══════════════════════════════════════════════════════════════

        public static bool Contains(string symbol)
            => _atoms != null && _atoms.ContainsKey(symbol.ToUpperInvariant());

        public static StoAtomData Get(string symbol, int charge = 0)
        {
            if (_atoms == null || string.IsNullOrEmpty(symbol)) return null;

            symbol = symbol.ToUpperInvariant();

            if (charge != 0)
            {
                string charged = FormatChargedKey(symbol, charge);
                if (_atoms.TryGetValue(charged, out var d)) return d;

                foreach (var alt in EnumerateChargeVariants(symbol, charge))
                {
                    if (_atoms.TryGetValue(alt, out var d2)) return d2;
                }
            }

            if (_atoms.TryGetValue(symbol, out var neutral)) return neutral;

            if (charge == 0)
            {
                if (_atoms.TryGetValue(symbol + "+", out var plus)) return plus;
                if (_atoms.TryGetValue(symbol + "-", out var minus)) return minus;
            }

            return null;
        }

        public static StoAtomData GetByAtomicNumber(int z)
        {
            if (_atoms == null) return null;
            string symbol = AtomicNumberToSymbol(z);
            if (symbol == null) return null;
            return Get(symbol);
        }

 // ⭐═══════════════════════════════════════════════════════════════
 // ⭐ LUT cache — built once per symbol, shared between atoms.
 // ⭐═══════════════════════════════════════════════════════════════

 /// <summary>
 /// Returns LUT density radialnej for danego symbolu (np. "C").
 /// Builds raz, then cache'uje.
 /// </summary>
        public static StoDensityLut GetLut(string symbol,
                                            float step = 0.02f,
                                            float rMaxOverride = 0f)
        {
            if (_atoms == null || string.IsNullOrEmpty(symbol)) return null;

            symbol = symbol.ToUpperInvariant();

            if (_lutCache == null)
            {
                lock (_lutLock)
                {
                    if (_lutCache == null)
                        _lutCache = new Dictionary<string, StoDensityLut>(
                            StringComparer.OrdinalIgnoreCase);
                }
            }

            lock (_lutLock)
            {
                if (_lutCache.TryGetValue(symbol, out var cached))
                    return cached;

                var atom = Get(symbol);
                if (atom == null) return null;

                var lut = StoDensityLut.Build(atom, step, rMaxOverride);
                _lutCache[symbol] = lut;
                return lut;
            }
        }

 /// <summary>
 /// Prebudowuje LUT for list symboli (np. w tle przy starcie).
 /// </summary>
        public static int PrebuildLuts(IEnumerable<string> symbols,
                                        float step = 0.02f)
        {
            if (_atoms == null || symbols == null) return 0;

            int built = 0;
            foreach (var sym in symbols)
            {
                if (string.IsNullOrEmpty(sym)) continue;
                var lut = GetLut(sym, step);
                if (lut != null) built++;
            }
            return built;
        }

 /// <summary>
 /// Info diagnostyczne o cache LUT.
 /// </summary>
        public static string GetLutCacheInfo()
        {
            lock (_lutLock)
            {
                if (_lutCache == null || _lutCache.Count == 0)
                    return "StoDatabase LUT cache: empty";

                int totalBytes = 0;
                foreach (var lut in _lutCache.Values)
                    totalBytes += lut.Count * 4;

                return $"StoDatabase LUT cache: {_lutCache.Count} entries, " +
                       $"{totalBytes / 1024f:F1} KB total";
            }
        }

 // ═══════════════════════════════════════════════════════════════
 // Reszta without changes
 // ═══════════════════════════════════════════════════════════════

        public static IEnumerable<string> AllSymbols
            => _atoms?.Keys ?? (IEnumerable<string>)Array.Empty<string>();

        public static string GetInfo()
        {
            if (!IsLoaded) return "StoDatabase: not loaded";
            return $"StoDatabase: {_atoms.Count} atoms loaded";
        }

        private static Dictionary<string, StoAtomData> ParseJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("atoms", out var atomsEl))
                throw new InvalidDataException("JSON missing 'atoms' root property");

            var result = new Dictionary<string, StoAtomData>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var prop in atomsEl.EnumerateObject())
            {
                string symbol = prop.Name.ToUpperInvariant();
                var atom = ParseAtom(prop.Value);
                if (atom == null) continue;

                atom.Symbol = symbol;
                ParseOccupations(atom);
                result[symbol] = atom;
            }

            return result;
        }

        private static void MergeJson(
            Dictionary<string, StoAtomData> target,
            string path,
            int chargeOverride)
        {
            string json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("atoms", out var atomsEl))
                return;

            foreach (var prop in atomsEl.EnumerateObject())
            {
                string key = prop.Name.ToUpperInvariant();
                var atom = ParseAtom(prop.Value);
                if (atom == null) continue;

                atom.Symbol = key;
                atom.ChargeState = chargeOverride;
                ParseOccupations(atom);
                target[key] = atom;
            }
        }

        private static StoAtomData ParseAtom(JsonElement el)
        {
            if (el.ValueKind != JsonValueKind.Object)
                return null;

            var data = new StoAtomData
            {
                TotalEnergy = GetDouble(el, "total_energy"),
                KineticEnergy = GetDouble(el, "kinetic_energy"),
                PotentialEnergy = GetDouble(el, "potential_energy"),
                Config = GetString(el, "config"),
                SourceFile = GetString(el, "source_file"),
                ChargeState = GetInt(el, "charge_state"),
            };

            data.S = ParseShell(el, "s");
            data.P = ParseShell(el, "p");
            data.D = ParseShell(el, "d");
            data.F = ParseShell(el, "f");

            return data;
        }

        private static StoShell ParseShell(JsonElement parent, string key)
        {
            if (!parent.TryGetProperty(key, out var shellEl))
                return null;

            if (shellEl.ValueKind == JsonValueKind.Null)
                return null;

            if (shellEl.ValueKind != JsonValueKind.Object)
                return null;

            var shell = new StoShell();

            if (shellEl.TryGetProperty("names", out var namesEl) &&
                namesEl.ValueKind == JsonValueKind.Array)
            {
                shell.Names = new string[namesEl.GetArrayLength()];
                int i = 0;
                foreach (var n in namesEl.EnumerateArray())
                    shell.Names[i++] = n.GetString();
            }

            if (shellEl.TryGetProperty("terms", out var termsEl) &&
                termsEl.ValueKind == JsonValueKind.Array)
            {
                shell.Terms = new List<StoTerm>(termsEl.GetArrayLength());

                foreach (var termEl in termsEl.EnumerateArray())
                {
                    var term = new StoTerm
                    {
                        N = GetInt(termEl, "n"),
                        Xi = (float)GetDouble(termEl, "xi"),
                    };

                    if (termEl.TryGetProperty("coefs", out var coefsEl) &&
                        coefsEl.ValueKind == JsonValueKind.Array)
                    {
                        term.Coefs = new float[coefsEl.GetArrayLength()];
                        int j = 0;
                        foreach (var c in coefsEl.EnumerateArray())
                            term.Coefs[j++] = (float)c.GetDouble();
                    }

                    shell.Terms.Add(term);
                }
            }

            if (shell.Names != null)
            {
                shell.OccAlpha = new int[shell.Names.Length];
                shell.OccBeta = new int[shell.Names.Length];
            }

            return shell;
        }

        private static void ParseOccupations(StoAtomData atom)
        {
            atom.Occupations = ElectronConfigParser.Parse(atom.Config)
                ?? new Dictionary<string, (int, int)>();

            FillShellOccupations(atom, atom.S);
            FillShellOccupations(atom, atom.P);
            FillShellOccupations(atom, atom.D);
            FillShellOccupations(atom, atom.F);
        }

        private static void FillShellOccupations(StoAtomData atom, StoShell shell)
        {
            if (shell?.Names == null) return;

            int n = shell.Names.Length;

            if (shell.OccAlpha == null || shell.OccAlpha.Length != n)
                shell.OccAlpha = new int[n];
            if (shell.OccBeta == null || shell.OccBeta.Length != n)
                shell.OccBeta = new int[n];

            for (int i = 0; i < n; i++)
            {
                string name = shell.Names[i];

                if (atom.Occupations.TryGetValue(name, out var occ))
                {
                    shell.OccAlpha[i] = occ.alpha;
                    shell.OccBeta[i] = occ.beta;
                }
                else
                {
                    shell.OccAlpha[i] = 0;
                    shell.OccBeta[i] = 0;
                }
            }
        }

        private static double GetDouble(JsonElement el, string key)
        {
            if (!el.TryGetProperty(key, out var v)) return 0.0;
            if (v.ValueKind == JsonValueKind.Null) return 0.0;
            if (v.ValueKind != JsonValueKind.Number) return 0.0;
            return v.GetDouble();
        }

        private static int GetInt(JsonElement el, string key)
        {
            if (!el.TryGetProperty(key, out var v)) return 0;
            if (v.ValueKind == JsonValueKind.Null) return 0;
            if (v.ValueKind != JsonValueKind.Number) return 0;
            return v.GetInt32();
        }

        private static string GetString(JsonElement el, string key)
        {
            if (!el.TryGetProperty(key, out var v)) return null;
            if (v.ValueKind == JsonValueKind.Null) return null;
            if (v.ValueKind != JsonValueKind.String) return null;
            return v.GetString();
        }

        private static string FormatChargedKey(string symbol, int charge)
        {
            if (charge == 0) return symbol;
            if (charge == +1) return symbol + "+";
            if (charge == -1) return symbol + "-";
            if (charge > 0) return symbol + charge + "+";
            return symbol + Math.Abs(charge) + "-";
        }

        private static IEnumerable<string> EnumerateChargeVariants(
            string symbol, int charge)
        {
            if (charge == +1)
            {
                yield return symbol + "+";
                yield return symbol + "1+";
                yield return symbol + "(+)";
            }
            else if (charge == -1)
            {
                yield return symbol + "-";
                yield return symbol + "1-";
                yield return symbol + "(-)";
            }
            else if (charge > 0)
            {
                yield return symbol + charge + "+";
                yield return symbol + "+" + charge;
            }
            else if (charge < 0)
            {
                int abs = Math.Abs(charge);
                yield return symbol + abs + "-";
                yield return symbol + "-" + abs;
            }
        }

        private static readonly string[] _symbolsByZ = new[]
        {
            "",   "H",  "HE", "LI", "BE", "B",  "C",  "N",  "O",  "F",  "NE",
            "NA", "MG", "AL", "SI", "P",  "S",  "CL", "AR", "K",  "CA",
            "SC", "TI", "V",  "CR", "MN", "FE", "CO", "NI", "CU", "ZN",
            "GA", "GE", "AS", "SE", "BR", "KR", "RB", "SR", "Y",  "ZR",
            "NB", "MO", "TC", "RU", "RH", "PD", "AG", "CD", "IN", "SN",
            "SB", "TE", "I",  "XE", "CS", "BA", "LA", "CE", "PR", "ND",
            "PM", "SM", "EU", "GD", "TB", "DY", "HO", "ER", "TM", "YB",
            "LU", "HF", "TA", "W",  "RE", "OS", "IR", "PT", "AU", "HG",
            "TL", "PB", "BI", "PO", "AT", "RN", "FR", "RA", "AC", "TH",
            "PA", "U",  "NP", "PU", "AM", "CM", "BK", "CF", "ES", "FM",
            "MD", "NO", "LR"
        };

        public static string AtomicNumberToSymbol(int z)
        {
            if (z <= 0 || z >= _symbolsByZ.Length) return null;
            return _symbolsByZ[z];
        }

        public static int SymbolToAtomicNumber(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return -1;
            symbol = symbol.ToUpperInvariant();

            for (int i = 1; i < _symbolsByZ.Length; i++)
                if (_symbolsByZ[i] == symbol) return i;

            return -1;
        }

 /// <summary>
 /// ⭐ v8: Clears the LUT cache. Call after changing STO parameters
 /// (DefaultStep, ExpCutoff, MaxRMax) w Advanced panel.
 /// After clearing, LUTs will be rebuilt on the next GetLut().
 /// </summary>
        public static void ClearLutCache()
        {
            lock (_lutLock)
            {
                if (_lutCache != null)
                {
                    int count = _lutCache.Count;
                    _lutCache.Clear();
                    System.Diagnostics.Debug.WriteLine(
                        $"[StoDatabase] LUT cache cleared ({count} entries)");
                }
            }
        }
    }
}