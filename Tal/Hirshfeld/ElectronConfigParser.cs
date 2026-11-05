// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: ElectronConfigParser.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ElectronConfigParser v2 — fix for letter-catching bugs in names
// elements (CARBON → O, IRON → O) and in orbital symbols (2P → P).

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tal.Hirshfeld.Sto
{
    public static class ElectronConfigParser
    {
 // K(2) = 1S(2), L(8) = 2S(2)2P(6), M(18) = 3S(2)3P(6)3D(10)
        private static readonly Dictionary<string, string> _shellExpansion =
            new Dictionary<string, string>
        {
            { "K", "1S(2)" },
            { "L", "2S(2)2P(6)" },
            { "M", "3S(2)3P(6)3D(10)" },
            { "N", "4S(2)4P(6)4D(10)4F(14)" },
            { "O", "5S(2)5P(6)5D(10)5F(14)" },
            { "P", "6S(2)6P(6)6D(10)" },
            { "Q", "7S(2)7P(6)" },
        };

 // Regex for orbitali: 1S(2), 2S(2), 2P(2), 3D(6), ...
        private static readonly Regex _orbitalRegex =
            new Regex(@"(\d)([SPDF])\((\d+)\)", RegexOptions.Compiled);

 // ⭐ v2: regex for abbreviations shells — z negative lookbehind.
 // Abbreviation shellsi (K/L/M/N/O/P/Q) must not be preceded
 // a letter or digit. Thanks to this:
 // - "CARBON 1S(2)..." — O w "CARBON" NIE jest matchowany
 // - "2P(2)" — P after cyfrze NIE jest matchowany
 // - "K(2)L(8)M(18)" — K/L/M ARE matched (spacja / `)`)
        private static readonly Regex _shellRegex =
            new Regex(@"(?<![a-zA-Z0-9])([KLMNOPQ])\((\d+)\)",
                      RegexOptions.Compiled);

        public static Dictionary<string, (int alpha, int beta)> Parse(string config)
        {
            var result = new Dictionary<string, (int, int)>(
                StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(config))
                return result;

 // 1) Expand abbreviations shells K/L/M/N/O/P/Q
            string expanded = ExpandShells(config);

 // 2) Extract orbitals and electron counts
            var raw = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (Match m in _orbitalRegex.Matches(expanded))
            {
                string n = m.Groups[1].Value;
                string l = m.Groups[2].Value;
                int count = int.Parse(m.Groups[3].Value);

                string key = n + l;

                if (raw.TryGetValue(key, out int prev))
                    raw[key] = prev + count;
                else
                    raw[key] = count;
            }

 // 3) Split electrons into alpha/beta with Hund's rule
            return ApplyHundsRule(raw);
        }

        private static string ExpandShells(string config)
        {
            return _shellRegex.Replace(config, match =>
            {
                string shell = match.Groups[1].Value;
                if (_shellExpansion.TryGetValue(shell, out string expansion))
                    return expansion;
                return match.Value;
            });
        }

 /// <summary>
 /// Hund's rule: alfa = min(N, 2l+1), beta = max(0, N - (2l+1)).
 /// </summary>
        private static Dictionary<string, (int, int)> ApplyHundsRule(
            Dictionary<string, int> raw)
        {
            var result = new Dictionary<string, (int, int)>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var kv in raw)
            {
                string key = kv.Key;
                int electrons = kv.Value;
                int nOrbitals = GetOrbitalCount(key);

                int alpha = Math.Min(electrons, nOrbitals);
                int beta = Math.Max(0, electrons - nOrbitals);

                result[key] = (alpha, beta);
            }

            return result;
        }

 /// <summary>
 /// Number of orbitals in afterdshell (2l+1): S=1, P=3, D=5, F=7.
 /// </summary>
        private static int GetOrbitalCount(string orbital)
        {
            if (string.IsNullOrEmpty(orbital) || orbital.Length < 2)
                return 1;

            char l = orbital[orbital.Length - 1];
            switch (l)
            {
                case 'S': return 1;
                case 'P': return 3;
                case 'D': return 5;
                case 'F': return 7;
                default: return 1;
            }
        }
    }
}