// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — SafeDebugLogger drain timer integration.

﻿// MainForm.Logging.cs
// Author: Daniel M. Kaminski
// Year: 2026

using Tal.Diagnostics;

namespace Tal
{
    public partial class MainForm
    {
        #region Log Drain Timer

        /// <summary>
        /// Timer that drains the SafeDebugLogger queue once per second.
        /// Runs on the UI thread (System.Windows.Forms.Timer), so
        /// Drain() can safely call DebugWindow.Log without Invoke.
        /// </summary>
        private void InitializeLogDrainTimer()
        {
            _logDrainTimer = new System.Windows.Forms.Timer
            {
                Interval = SafeDebugLogger.DrainIntervalMs   // 1000 ms
            };
            _logDrainTimer.Tick += (s, e) => SafeDebugLogger.DrainIfDue();
            _logDrainTimer.Start();

            System.Diagnostics.Debug.WriteLine(
                "[MainForm] SafeDebugLogger drain timer started (1 Hz)");
        }

        /// <summary>
        /// Stops timer (called when closing MainForm).
        /// </summary>
        private void StopLogDrainTimer()
        {
            if (_logDrainTimer != null)
            {
                _logDrainTimer.Stop();
                _logDrainTimer.Dispose();
                _logDrainTimer = null;
            }

            // Final drain — flush whatever remains in the queue
            SafeDebugLogger.Drain();
            SafeDebugLogger.DetachDebugWindow();
        }

        #endregion
    }
}
