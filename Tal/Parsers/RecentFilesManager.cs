// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure file parsers: RecentFilesManager.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Tal.Parsers
{
 /// <summary>
 /// Zarządza listą ostatnio otwieranych files.
 /// Przechowuje ją w %LocalAppData%\Tal\RecentFiles.json
 /// </summary>
    public sealed class RecentFilesManager
    {
        #region Singleton

        private static readonly Lazy<RecentFilesManager> _instance =
            new Lazy<RecentFilesManager>(() => new RecentFilesManager());

        public static RecentFilesManager Instance => _instance.Value;

        #endregion

        #region Constants

        public const int MaxEntries = 10;

        private static readonly string ConfigDir =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Tal");

        private static readonly string ConfigPath =
            Path.Combine(ConfigDir, "RecentFiles.json");

        #endregion

        #region State

        private readonly List<string> _files = new List<string>();
        private readonly object _lock = new object();

        #endregion

        #region Constructor

        private RecentFilesManager()
        {
            Load();
        }

        #endregion

        #region Public API

 /// <summary>
 /// Returns kopię list (without modyfikacji original).
 /// </summary>
        public IReadOnlyList<string> GetFiles()
        {
            lock (_lock)
            {
                return _files.ToArray();
            }
        }

 /// <summary>
 /// Adde plik na afterczątek list. Deduplikuje (case-insensitive),
 /// przesuwa existcy wpis na górę.
 /// </summary>
        public void Add(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(filePath);
            }
            catch
            {
                return; // np. nieprawidłowa ścieżka
            }

            lock (_lock)
            {
 // Remove duplikat (case-insensitive)
                _files.RemoveAll(p =>
                    string.Equals(p, fullPath, StringComparison.OrdinalIgnoreCase));

 // Insert na afterczątek
                _files.Insert(0, fullPath);

 // Przytnij do MaxEntries
                if (_files.Count > MaxEntries)
                    _files.RemoveRange(MaxEntries, _files.Count - MaxEntries);

                Save();
            }
        }

 /// <summary>
 /// Usuwa wpis z list (np. when plik already nie exists).
 /// </summary>
        public void Remove(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            lock (_lock)
            {
                int removed = _files.RemoveAll(p =>
                    string.Equals(p, filePath, StringComparison.OrdinalIgnoreCase));

                if (removed > 0)
                    Save();
            }
        }

 /// <summary>
 /// Clears całą list.
 /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                if (_files.Count == 0) return;
                _files.Clear();
                Save();
            }
        }

        #endregion

        #region Persistence

        private void Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                    return;

                string json = File.ReadAllText(ConfigPath);
                var loaded = JsonSerializer.Deserialize<List<string>>(json);

                if (loaded == null) return;

                lock (_lock)
                {
                    _files.Clear();

 // Filter: only existce pliki, without duplikatów
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var path in loaded)
                    {
                        if (string.IsNullOrWhiteSpace(path)) continue;
                        if (!seen.Add(path)) continue;

                        try
                        {
                            if (File.Exists(path))
                                _files.Add(path);
                        }
                        catch
                        {
 // ignore — np. nieprawidłowa ścieżka UNC
                        }

                        if (_files.Count >= MaxEntries) break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[RecentFiles] Load error: {ex.Message}");
            }
        }

        private void Save()
        {
            try
            {
                if (!Directory.Exists(ConfigDir))
                    Directory.CreateDirectory(ConfigDir);

                string json = JsonSerializer.Serialize(_files,
                    new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[RecentFiles] Save error: {ex.Message}");
            }
        }

        #endregion
    }
}