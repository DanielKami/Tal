// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — recent files menu and persistence.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System;
using System.IO;
using System.Windows.Forms;
using Tal.Core;
using Tal.Parsers;

namespace Tal
{
    public partial class MainForm
    {
        #region Fields

        // Menu item "Open Recent" — filled dynamically
        private ToolStripMenuItem _recentMenu;

        #endregion

        #region Public API

        /// <summary>
        /// Adds a file to the recently opened list.
        /// Called from OnOpen and LoadCifFile after a successful load.
        /// </summary>
        public void AddToRecentFiles(string filePath)
        {
            RecentFilesManager.Instance.Add(filePath);
            UpdateRecentMenu();
        }

        /// <summary>
        /// Rebuilds the "Open Recent" menu from the current list.
        /// </summary>
        public void UpdateRecentMenu()
        {
            if (_recentMenu == null || _recentMenu.IsDisposed) return;

            _recentMenu.DropDownItems.Clear();

            var files = RecentFilesManager.Instance.GetFiles();

            if (files.Count == 0)
            {
                var empty = new ToolStripMenuItem("(no recent files)")
                {
                    Enabled = false
                };
                _recentMenu.DropDownItems.Add(empty);
                return;
            }

            for (int i = 0; i < files.Count; i++)
            {
                string path = files[i];
                string display = $"{i + 1}. {Path.GetFileName(path)}";

                var item = new ToolStripMenuItem(display)
                {
                    ToolTipText = path,
                    Tag = path
                };

                // Shortcut Ctrl+1..Ctrl+9 for the first 9 entries
                if (i < 9)
                {
                    item.ShortcutKeys = Keys.Control | (Keys)((int)Keys.D1 + i);
                    item.ShowShortcutKeys = true;
                }

                item.Click += (s, e) => OpenRecentFile((string)((ToolStripMenuItem)s).Tag);
                _recentMenu.DropDownItems.Add(item);
            }

            _recentMenu.DropDownItems.Add(new ToolStripSeparator());

            var clearItem = new ToolStripMenuItem("Clear list");
            clearItem.Click += (s, e) =>
            {
                if (MessageBox.Show(
                        "Clear the list of recently opened files?",
                        "Open Recent",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    RecentFilesManager.Instance.Clear();
                    UpdateRecentMenu();
                    SetStatus("Recent files list cleared",
                              $"Structures: {StructureStorage.Instance.StructureCount}",
                              "🧹");
                }
            };
            _recentMenu.DropDownItems.Add(clearItem);
        }

        #endregion

        #region Internal

        /// <summary>
        /// Opens a file from the recent list. Handles missing files and load errors.
        /// </summary>
        private void OpenRecentFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            if (!File.Exists(filePath))
            {
                MessageBox.Show(
                    $"File does not exist:\n{filePath}\n\nIt will be removed from the list.",
                    "Open Recent",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                RecentFilesManager.Instance.Remove(filePath);
                UpdateRecentMenu();
                return;
            }

            // ⭐ All logic in one place
            if (!OpenFileInternal(filePath))
            {
                // If the file disappeared / is corrupted — remove from the list
                if (!File.Exists(filePath))
                {
                    RecentFilesManager.Instance.Remove(filePath);
                    UpdateRecentMenu();
                }
            }
        }

        #endregion
    }
}
