// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — symmetry reveal / apply helpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════════
//  MainForm.Symmetry.cs
// ═══════════════════════════════════════════════════════════════════════════
//
//  Helpers for refreshing symmetry elements on all open
//  documents. Called from SymmetryElementsWindow (and possibly
//  other windows) when colour / size / visibility changes.
//
//  Filozofia:
//    - SymmetryElementsRenderer reads COLOURS from Flags.Instance live,
//      so changing the Flag alone is enough — no need to rebuild _elements.
//    - But if someone later changes the model (colour cache in CellSymElement),
//      InvalidateSymmetryRenderers() forces a full rebuild.
//
//  Usage in SymmetryElementsWindow:
//      MainForm.Instance?.InvalidateSymmetryRenderers();
//      MainForm.Instance?.RefreshAllDocuments();
//
// ═══════════════════════════════════════════════════════════════════════════

using System.Diagnostics;
using Tal.DrawMethods;
using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal
{
    public partial class MainForm
    {
        #region Symmetry Renderers — Invalidate / Refresh

        /// <summary>
        /// Forces full rebuild elements symmetry (cache <c>_elements</c>)
        /// we all open documents MonoGameDocument.
        ///
        /// Used when a parameter change affects element GEOMETRY
        /// (np. change space group, change operacji symmetry),
        /// and not only colour — colours are read live from Flags anyway.
        ///
        /// Safe to call repeatedly.
        /// </summary>
        public void InvalidateSymmetryRenderers()
        {
            var dockPanel = GetDockPanel();
            if (dockPanel == null) return;

            int count = 0;

            foreach (var doc in dockPanel.Documents)
            {
                if (doc is not MonoGameDocument monoDoc) continue;
                if (monoDoc.IsDisposed) continue;

                try
                {
                    var renderer = monoDoc.GetSymmetryElementsRenderer();
                    if (renderer == null) continue;

                    renderer.Invalidate();
                    count++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        $"[MainForm] InvalidateSymmetryRenderers error " +
                        $"for '{monoDoc.Text}': {ex.Message}");
                }
            }

            Debug.WriteLine(
                $"[MainForm] InvalidateSymmetryRenderers: invalidated {count} renderer(s)");
        }

        /// <summary>
        /// Visually refreshes symmetry elements:
        ///   1. Podbija VisualRevision (reset Progressive AA)
        ///   2. Invalidate + Refresh na all documents
        ///
        /// This is the "one button" to call from the Symmetry Elements window
        /// after every colour / size / visibility change.
        /// </summary>
        public void RefreshSymmetryElements()
        {
            // 1) Reset Progressive AA + signal for renderers
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

            // 2) Force cache rebuild (safe — colours are live anyway)
            InvalidateSymmetryRenderers();

            // 3) Refresh all documents
            RefreshAllDocuments();
        }

        #endregion
    }
}
