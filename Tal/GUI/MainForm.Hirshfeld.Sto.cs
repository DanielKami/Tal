// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — Hirshfeld surface / STO-related actions.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Diagnostics;
using Tal.GUI;
using Tal.Hirshfeld.Sto;
using Tal.Utils;

namespace Tal
{
    public partial class MainForm
    {
        #region STO Database

        private static bool _stoLoadedOnce = false;

        public static void EnsureStoDatabaseLoaded()
        {
            if (_stoLoadedOnce) return;
            _stoLoadedOnce = true;

            var debugWin = GetDebugWindowStatic();

            void Log(string msg, System.Drawing.Color color)
            {
                debugWin?.Log(msg, color);
                System.Diagnostics.Debug.WriteLine(msg);
            }

            Log("════════════════════════════════════════════════════════════",
                System.Drawing.Color.Cyan);
            Log("🔬 HIRSHFELD — STO DATABASE INITIALIZATION",
                System.Drawing.Color.Cyan);
            Log("════════════════════════════════════════════════════════════",
                System.Drawing.Color.Cyan);

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? baseDir;

                string[] searchDirs =
                {
                    exeDir,
                    baseDir,
                    Path.Combine(exeDir, "Data"),
                    Path.Combine(baseDir, "Data"),
                    Path.Combine(exeDir, "Content", "STO"),
                    Path.Combine(baseDir, "Content", "STO"),
                    Path.Combine(exeDir, "STO"),
                    Path.Combine(baseDir, "STO"),
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
                    Log("❌ sto_atoms_neutral.json NOT FOUND in any search path:",
                        System.Drawing.Color.Red);
                    foreach (var dir in searchDirs)
                        Log($"      {dir}", System.Drawing.Color.Gray);
                    return;
                }

                Log($"  neutral: {neutral}", System.Drawing.Color.LightGray);
                Log($"  cation:  {cation ?? "(not found)"}", System.Drawing.Color.LightGray);
                Log($"  anion:   {anion ?? "(not found)"}", System.Drawing.Color.LightGray);

                var sw = Stopwatch.StartNew();
                StoDatabase.LoadAll(neutral, cation, anion);
                sw.Stop();

                Log($"✅ Loaded {StoDatabase.Count} atoms in {sw.ElapsedMilliseconds} ms",
                    System.Drawing.Color.LightGreen);

                RunStoDatabaseSelfTest(Log);

                // ⭐ Prebuild LUT — krok 0.02 Å
                var commonSymbols = new[] { "H", "C", "N", "O", "F", "P", "S", "CL", "BR", "I" };
                var swLut = Stopwatch.StartNew();
                int builtCount = StoDatabase.PrebuildLuts(commonSymbols, step: 0.02f);
                swLut.Stop();

                Log($"✅ Prebuilt {builtCount} LUT tables (step=0.02 Å) in {swLut.ElapsedMilliseconds} ms",
                    System.Drawing.Color.LightGreen);
                Log($"   {StoDatabase.GetLutCacheInfo()}",
                    System.Drawing.Color.LightGray);
            }
            catch (Exception ex)
            {
                Log($"❌ STO database load FAILED: {ex.GetType().Name}: {ex.Message}",
                    System.Drawing.Color.Red);
                Log($"   Stack: {ex.StackTrace}", System.Drawing.Color.Gray);
            }
        }

        private static void RunStoDatabaseSelfTest(Action<string, System.Drawing.Color> log)
        {
            if (!StoDatabase.IsLoaded)
            {
                log("  Self-test SKIPPED", System.Drawing.Color.Orange);
                return;
            }

            var testSymbols = new[] { "H", "C", "N", "O", "FE", "AG", "XE" };

            log("────────────────────────────────────────────────────────────",
                System.Drawing.Color.DarkGray);
            log("🔬 STO SELF-TEST — ∫ρ·4πr²·dr vs Z", System.Drawing.Color.Cyan);
            log("────────────────────────────────────────────────────────────",
                System.Drawing.Color.DarkGray);

            int passed = 0, failed = 0;
            var sw = Stopwatch.StartNew();

            foreach (var sym in testSymbols)
            {
                var atom = StoDatabase.Get(sym);
                if (atom == null)
                {
                    log($"  ⚠️  {sym,-4} not in database", System.Drawing.Color.Orange);
                    failed++;
                    continue;
                }

                int z = StoDatabase.SymbolToAtomicNumber(sym);
                int expected = z + atom.ChargeState;

                float nel = StoAtomDensity.IntegrateElectrons(atom);
                float diff = nel - expected;
                float pctErr = expected != 0 ? 100f * diff / expected : 0f;

                bool ok = Math.Abs(diff) < 0.05f;
                var color = ok ? System.Drawing.Color.LightGreen
                               : System.Drawing.Color.Orange;

                log($"  {(ok ? "✓" : "✗")} {sym,-4} " +
                    $"Z={z,3}  ∫ρ={nel,8:F4}  diff={diff,+7:F4} ({pctErr,+7:F3}%)",
                    color);

                if (!ok)
                {
                    var diagLines = StoAtomDensity.GetDiagnostics(atom);
                    foreach (var (text, c) in diagLines)
                        log(text, c);
                }

                if (ok) passed++;
                else failed++;
            }

            sw.Stop();

            log("────────────────────────────────────────────────────────────",
                System.Drawing.Color.DarkGray);
            log($"  Result: {passed} passed, {failed} failed " +
                $"(total {sw.ElapsedMilliseconds} ms)",
                failed == 0 ? System.Drawing.Color.LightGreen
                            : System.Drawing.Color.Orange);
            log("════════════════════════════════════════════════════════════",
                System.Drawing.Color.Cyan);
            log("", System.Drawing.Color.White);
        }

        private static DebugWindow GetDebugWindowStatic()
        {
            try
            {
                return Application.OpenForms
                    .OfType<DebugWindow>()
                    .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
