// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Thread-safe debug log queue drained on the UI thread.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Collections.Concurrent;
using System.Diagnostics;
using Tal.GUI;

namespace Tal.Diagnostics
{
    /// <summary>
    /// Thread-safe logger with throttling (default 1 Hz).
    ///
    /// - Worker threads: only Enqueue (lock-free ConcurrentQueue, ~50-100 ns)
    /// - UI thread: Drain() once per second (WinForms Timer — runs on the UI thread)
    /// - ZERO Application.OpenForms w hot path
    /// - ZERO cross-thread UI calls z threads worker
    ///
    /// ⭐ Usage from a worker thread:
    ///     SafeDebugLogger.Log("message", Color.Red);
    ///
    /// ⭐ Inicjalizacja in MainForm:
    ///     _drainTimer = new Timer { Interval = 1000 };
    ///     _drainTimer.Tick += (s, e) => SafeDebugLogger.DrainIfDue();
    ///     _drainTimer.Start();
    /// </summary>
    public static class SafeDebugLogger
    {
        #region Configuration

        /// <summary>Queue drain interval (ms). 1000 = once per second.</summary>
        public static int DrainIntervalMs { get; set; } = 1000;

        /// <summary>Max messages per Drain (protects against UI freeze).</summary>
        public static int MaxMessagesPerDrain { get; set; } = 500;

        /// <summary>Maksymalny size kolejki (ochrona before OOM).</summary>
        public static int MaxQueueSize { get; set; } = 100_000;

        /// <summary>If true — duplikaty w oknie czasowym are liczone, not logowane osobno.</summary>
        public static bool CollapseDuplicates { get; set; } = true;

        /// <summary>Time window for collapsing duplicates (ms).</summary>
        public static int DuplicateWindowMs { get; set; } = 500;

        #endregion

        #region State

        private static volatile DebugWindow _debugWindow;
        private static volatile bool _hasDebugWindow;

        private static readonly ConcurrentQueue<LogEntry> _queue = new();

        private static long _lastDrainTicks;
        private static int _droppedCount;

        // Collapse duplicates
        private static readonly ConcurrentDictionary<string, long> _lastSeen = new();
        private static readonly ConcurrentDictionary<string, int> _repeatCount = new();

        #endregion

        #region Log Entry

        private readonly struct LogEntry
        {
            public readonly string Message;
            public readonly System.Drawing.Color? Color;
            public readonly long TimestampTicks;

            public LogEntry(string message, System.Drawing.Color? color)
            {
                Message = message;
                Color = color;
                TimestampTicks = Stopwatch.GetTimestamp();
            }
        }

        #endregion

        #region Attach / Detach

        /// <summary>
        /// Call RAZ z UI thread after otwarciu DebugWindow.
        /// From this moment Log() may be called from any thread.
        /// </summary>
        public static void AttachDebugWindow(DebugWindow window)
        {
            _debugWindow = window;
            _hasDebugWindow = window != null;
            _lastDrainTicks = Stopwatch.GetTimestamp();
        }

        public static void DetachDebugWindow()
        {
            _debugWindow = null;
            _hasDebugWindow = false;
        }

        public static bool IsEnabled => _hasDebugWindow;

        #endregion

        #region Log (hot path — wywoływane z wątków roboczych)

        /// <summary>
        /// Log z dowolnego thread. NIGDY not dotyka UI.
        /// Koszt: ~50-100 ns (ConcurrentQueue.Enqueue + opcjonalny collapse).
        /// </summary>
        public static void Log(string message, System.Drawing.Color? color = null)
        {
            // Szybki wyjazd — no DebugWindow
            if (!_hasDebugWindow)
            {
                Debug.WriteLine(message);
                return;
            }

            // Ochrona before OOM
            if (_queue.Count >= MaxQueueSize)
            {
                Interlocked.Increment(ref _droppedCount);
                return;
            }

            // Optional collapse of duplicates
            if (CollapseDuplicates && message != null)
            {
                long now = Stopwatch.GetTimestamp();
                long windowTicks = (long)(DuplicateWindowMs * (double)Stopwatch.Frequency / 1000.0);

                if (_lastSeen.TryGetValue(message, out long lastTicks))
                {
                    if (now - lastTicks < windowTicks)
                    {
                        // This sam komunikat w oknie czasowym — licz, not loguj
                        _repeatCount.AddOrUpdate(message, 1, (_, c) => c + 1);
                        _lastSeen[message] = now;
                        return;
                    }
                    else
                    {
                        // Window elapsed — flush summary of previous repetitions
                        if (_repeatCount.TryRemove(message, out int repeats) && repeats > 0)
                        {
                            _queue.Enqueue(new LogEntry(
                                $"{message}  [×{repeats + 1}]",
                                color));
                        }
                    }
                }

                _lastSeen[message] = now;
            }

            _queue.Enqueue(new LogEntry(message, color));
        }

        /// <summary>
        /// Log z prefiksem (np. "[CAVITY]", "[MC]", "[VoxelScanner]").
        /// </summary>
        public static void LogTagged(string tag, string message, System.Drawing.Color? color = null)
        {
            Log($"{tag} {message}", color);
        }

        #endregion

        #region Drain (UI thread only)

        /// <summary>
        /// Call from the UI thread (Timer 1 Hz). ALWAYS checks the interval —
        /// you may call it every Update (60 Hz); it actually drains once per DrainIntervalMs.
        /// </summary>
        public static void DrainIfDue()
        {
            long now = Stopwatch.GetTimestamp();
            long intervalTicks = (long)(DrainIntervalMs * (double)Stopwatch.Frequency / 1000.0);

            if (now - _lastDrainTicks < intervalTicks)
                return;

            _lastDrainTicks = now;
            Drain();
        }

        /// <summary>
        /// Force immediate flush (e.g. when closing windows).
        /// </summary>
        public static void Drain()
        {
            var dw = _debugWindow;
            if (dw == null || dw.IsDisposed || !dw.IsHandleCreated)
                return;

            int n = 0;
            while (n < MaxMessagesPerDrain && _queue.TryDequeue(out var entry))
            {
                try
                {
                    if (entry.Color.HasValue)
                        dw.Log(entry.Message, entry.Color.Value);
                    else
                        dw.Log(entry.Message);
                }
                catch (ObjectDisposedException) { return; }
                catch (InvalidOperationException) { return; }
                n++;
            }

            // Raportuj drop
            int dropped = Interlocked.Exchange(ref _droppedCount, 0);
            if (dropped > 0)
            {
                try
                {
                    dw.Log($"... {dropped} messages dropped (queue overflow)",
                           System.Drawing.Color.OrangeRed);
                }
                catch { /* ignore */ }
            }

            // Clear collapse cache if the queue is empty
            if (_queue.IsEmpty)
            {
                _lastSeen.Clear();
                _repeatCount.Clear();
            }
        }

        public static void Clear()
        {
            while (_queue.TryDequeue(out _)) { }
            _lastSeen.Clear();
            _repeatCount.Clear();
            Interlocked.Exchange(ref _droppedCount, 0);
            _lastDrainTicks = Stopwatch.GetTimestamp();
        }

        /// <summary>Number of messages waiting in the queue.</summary>
        public static int PendingCount => _queue.Count;

        #endregion
    }
}