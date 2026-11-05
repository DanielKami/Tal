// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// UI progress reporting helper.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Tal.GUI;

namespace Tal.Utils
{
    /// <summary>
    /// Thread-safe progress reporter z throttlingiem.
    /// Calls from Task.Run / Parallel.For are safe.
    /// Uses existing MainForm.SetStatusSafe / SetProgressSafe.
    /// </summary>
    public static class ProgressReporter
    {
        #region Private Fields

        private static readonly object _lock = new object();
        private static readonly Stopwatch _sw = Stopwatch.StartNew();

        // Throttling
        private static long _lastUiTick = 0;
        private static long _lastDebugTick = 0;
        private static int _lastPercent = -1;
        private static string _lastMessage = null;

        // Window cache (do not iterate Application.OpenForms from a worker thread!)
        private static DebugWindow _debugWindow;
        private static readonly object _cacheLock = new object();

        // Throttling intervals (ms)
        private const int UI_THROTTLE_MS = 50;    // 20 Hz
        private const int DEBUG_THROTTLE_MS = 100; // 10 Hz

        private static volatile bool _enabled = true;

        #endregion

        #region Registration

        public static bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>Registers DebugWindow (call from the DebugWindow constructor).</summary>
        public static void RegisterDebugWindow(DebugWindow window)
        {
            lock (_cacheLock) { _debugWindow = window; }
        }

        /// <summary>Clears cache when closing windows.</summary>
        public static void ClearCache()
        {
            lock (_cacheLock) { _debugWindow = null; }
        }

        /// <summary>Reset stanu (before nowym zadaniem).</summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _lastUiTick = 0;
                _lastDebugTick = 0;
                _lastPercent = -1;
                _lastMessage = null;
            }
        }

        #endregion

        #region Report

        /// <summary>
        /// Reports progress. Thread-safe. Automatically throttles.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Report(
            int percent,
            string message = null,
            bool logToDebug = true,
            bool force = false)
        {
            if (!_enabled) return;
            percent = Math.Clamp(percent, 0, 100);

            long now = _sw.ElapsedMilliseconds;
            bool updateUi = force;
            bool updateDebug = force;

            lock (_lock)
            {
                // Skip if identical state
                if (!force &&
                    _lastPercent == percent &&
                    string.Equals(_lastMessage, message, StringComparison.Ordinal))
                {
                    return;
                }

                if (force || now - _lastUiTick >= UI_THROTTLE_MS)
                {
                    _lastUiTick = now;
                    updateUi = true;
                }

                if (logToDebug && (force || now - _lastDebugTick >= DEBUG_THROTTLE_MS))
                {
                    _lastDebugTick = now;
                    updateDebug = true;
                }

                _lastPercent = percent;
                _lastMessage = message;
            }

            if (updateUi)
                SafeUpdateMainForm(percent, message);

            if (updateDebug && logToDebug)
                SafeLogToDebug($"[{percent}%] {message ?? ""}", System.Drawing.Color.LightBlue);
        }

        /// <summary>Loguje only do DebugWindow.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(string message, System.Drawing.Color? color = null)
        {
            if (!_enabled) return;
            SafeLogToDebug(message, color);
        }

        /// <summary>Logs with progress to DebugWindow (without UI).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogProgress(string message, float percent, System.Drawing.Color? color = null)
        {
            if (!_enabled) return;
            SafeLogToDebug($"[{percent:F0}%] {message}", color ?? System.Drawing.Color.LightBlue);
        }

        #endregion

        #region Safe Calls

        private static void SafeUpdateMainForm(int percent, string message)
        {
            var form = MainForm.Instance;   // ⭐ Singleton już jest
            if (form == null) return;

            try
            {
                if (form.IsDisposed || !form.IsHandleCreated) return;

                // ⭐ SetStatusSafe / SetProgressSafe already do InvokeRequired + BeginInvoke
                if (!string.IsNullOrEmpty(message))
                    form.SetStatusSafe(message, "", "⏳");

                form.SetProgressSafe(percent);
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProgressReporter] {ex.Message}");
            }
        }

        private static void SafeLogToDebug(string message, System.Drawing.Color? color)
        {
            DebugWindow win;
            lock (_cacheLock) { win = _debugWindow; }

            // Fallback — if DebugWindow is not registered
            if (win == null || win.IsDisposed || !win.IsHandleCreated)
            {
                Debug.WriteLine($"[PROGRESS] {message}");
                return;
            }

            try
            {
                // ⭐ DebugWindow.Log has its own InvokeRequired + BeginInvoke
                win.Log(message, color);
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProgressReporter] {ex.Message}");
            }
        }

        #endregion
    }
}