// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — startup file processing (command-line / IPC).

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System;
using System.IO;
using System.Windows.Forms;

namespace Tal
{
    /// <summary>
    /// ⭐ Handling drag & drop files na MainForm.
    /// Allows dragging file(s) from Explorer and dropping onto the TAL window.
    /// Shares logic with Main args, IPC and Open Recent via OpenFileInternal().
    /// </summary>
    public partial class MainForm
    {
        #region Initialization

        /// <summary>
        /// Called w konstruktorze MainForm.
        /// Enables drag & drop na formie, dock containerze i dock panelu.
        /// </summary>
        private void InitializeFileDrop()
        {
            // Forma
            AllowDrop = true;
            DragEnter += MainForm_DragEnter;
            DragOver += MainForm_DragOver;
            DragDrop += MainForm_DragDrop;

            // Docking panel (main surface)
            if (_dockContainer != null)
            {
                _dockContainer.AllowDrop = true;
                _dockContainer.DragEnter += MainForm_DragEnter;
                _dockContainer.DragOver += MainForm_DragOver;
                _dockContainer.DragDrop += MainForm_DragDrop;
            }

            // DockPanel — so drop works over the 3D surface
            if (_dockPanel != null)
            {
                _dockPanel.AllowDrop = true;
                _dockPanel.DragEnter += MainForm_DragEnter;
                _dockPanel.DragOver += MainForm_DragOver;
                _dockPanel.DragDrop += MainForm_DragDrop;
            }

            System.Diagnostics.Debug.WriteLine("[MainForm] Drag & drop initialized");
        }

        #endregion

        #region Handlers

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0)
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }

            e.Effect = DragDropEffects.None;
        }

        private void MainForm_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length == 0)
                return;

            foreach (var path in files)
            {
                try
                {
                    if (!File.Exists(path)) continue;

                    System.Diagnostics.Debug.WriteLine(
                        $"[MainForm.DragDrop] Dropped file: {path}");

                    OpenFileInternal(path);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[MainForm.DragDrop] Error opening '{path}': {ex.Message}");

                    MessageBox.Show(
                        $"Failed to open file:\n{path}\n\n{ex.Message}",
                        "TAL",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        #endregion
    }
}
