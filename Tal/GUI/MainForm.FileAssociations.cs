// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — file association registration helpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Tal
{
    /// <summary>
    /// ⭐ Register file associations in HKCU (without admin).
    /// Called once at startup — idempotent, does not churn the registry without need.
    /// Adds TAL to "Open with" for all supported extensions.
    /// </summary>
    public partial class MainForm
    {
        #region P/Invoke

        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(
            uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        #endregion

        #region Constants

        private static readonly string[] SupportedExtensions = new[]
        {
            ".cif", ".res", ".ins", ".xyz",
            ".pdb", ".ent",
            ".mol", ".sdf", ".sd",
            ".mol2", ".ml2",
            ".gjf", ".com", ".gau"
        };

        private const string ProgIdPrefix = "TAL";

        #endregion

        #region Public API

        /// <summary>
        /// Registers skojarzenia files if still not zarejestrowane.
        /// Safe to call repeatedly — checks for an existing entry.
        /// </summary>
        public static void EnsureFileAssociations()
        {
            try
            {
                string exePath = Application.ExecutablePath;

                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    Debug.WriteLine("[Assoc] Executable path not available — skipping");
                    return;
                }

                bool anyChanged = false;

                foreach (var ext in SupportedExtensions)
                {
                    if (RegisterOneExtension(ext, exePath))
                        anyChanged = true;
                }

                if (anyChanged)
                {
                    try
                    {
                        SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
                    }
                    catch { /* ignore */ }

                    Debug.WriteLine("[Assoc] File associations updated");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Assoc] EnsureFileAssociations error: {ex.Message}");
            }
        }

        #endregion

        #region Private

        /// <summary>
        /// Returns true if something changed (shell needs notification).
        /// </summary>
        private static bool RegisterOneExtension(string ext, string exePath)
        {
            try
            {
                string progId = ProgIdPrefix + ext;    // np. "TAL.cif"
                bool changed = false;

                // ═══════════════════════════════════════════════════════
                // 1. HKCU\Software\Classes\.cif  →  "TAL.cif"
                // ═══════════════════════════════════════════════════════
                string extKeyPath = $@"Software\Classes\{ext}";

                using (var extKey = Registry.CurrentUser.CreateSubKey(extKeyPath))
                {
                    if (extKey == null) return false;

                    var current = extKey.GetValue("") as string;
                    if (!string.Equals(current, progId, StringComparison.OrdinalIgnoreCase))
                    {
                        extKey.SetValue("", progId);
                        changed = true;
                    }
                }

                // ═══════════════════════════════════════════════════════
                // 2. HKCU\Software\Classes\.cif\OpenWithProgids\TAL.cif = ""
                //    — so that TAL appears in "Open with"
                // ═══════════════════════════════════════════════════════
                string owpPath = $@"Software\Classes\{ext}\OpenWithProgids";
                using (var owpKey = Registry.CurrentUser.CreateSubKey(owpPath))
                {
                    if (owpKey != null)
                    {
                        if (owpKey.GetValue(progId) == null)
                        {
                            owpKey.SetValue(progId, new byte[0], RegistryValueKind.None);
                            changed = true;
                        }
                    }
                }

                // ═══════════════════════════════════════════════════════
                // 3. HKCU\Software\Classes\TAL.cif  (ProgID)
                // ═══════════════════════════════════════════════════════
                string progKeyPath = $@"Software\Classes\{progId}";

                using (var progKey = Registry.CurrentUser.CreateSubKey(progKeyPath))
                {
                    if (progKey == null) return changed;

                    string desc = $"TAL Structure File ({ext})";
                    var currentDesc = progKey.GetValue("") as string;
                    if (!string.Equals(currentDesc, desc, StringComparison.Ordinal))
                    {
                        progKey.SetValue("", desc);
                        changed = true;
                    }

                    // DefaultIcon
                    using (var iconKey = progKey.CreateSubKey("DefaultIcon"))
                    {
                        if (iconKey != null)
                        {
                            string iconVal = $"\"{exePath}\",0";
                            var currentIcon = iconKey.GetValue("") as string;
                            if (!string.Equals(currentIcon, iconVal, StringComparison.Ordinal))
                            {
                                iconKey.SetValue("", iconVal);
                                changed = true;
                            }
                        }
                    }

                    // shell\open\command
                    using (var shellKey = progKey.CreateSubKey(@"shell\open\command"))
                    {
                        if (shellKey != null)
                        {
                            // ⭐ %1 = pierwszy file (typowy dwuklik)
                            //    Use %* when the user selects multiple files and chooses "Open With"
                            string cmd = $"\"{exePath}\" \"%1\"";
                            var currentCmd = shellKey.GetValue("") as string;
                            if (!string.Equals(currentCmd, cmd, StringComparison.Ordinal))
                            {
                                shellKey.SetValue("", cmd);
                                changed = true;
                            }
                        }
                    }
                }

                return changed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Assoc] Register {ext} error: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
