// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Atom properties inspector window.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using WeifenLuo.WinFormsUI.Docking;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace Tal.Forms
{
    /// <summary>
    /// Window edycji GLOBALNEJ tabeli colours elements (VdwParameters.color).
    ///
    /// Table contains 118 elements. Changes go to VdwParameters.color
    /// and do Flags.AtomColorOverrides DOPIERO after click OK.
    /// Cancel zamyka window without zapisu — VdwParameters.color pozostaje without zmian.
    ///
    /// ⭐ Zapis:
    ///     OK  → VdwParameters.color[z] = new colour (immediately)
    ///         → Flags.AtomColorOverrides[z] = [R,G,B] (persistent, to JSON)
    ///         → Flags.SaveForce()
    ///         → VisualRevision++ → RefreshAllDocuments()
    ///
    /// ⭐ Rows with an overridden colour (different from DefaultColor) get a cream background.
    ///
    /// ⚠️ We use CellClick (not CellContentClick) — because the colColor cell
    ///    is ReadOnly with an empty value and CellContentClick does not fire.
    /// </summary>
    public partial class AtomPropertiesWindow : DockContent
    {
        #region Fields

        // Working copy — index = atomic number, value = current working colour.
        // Filled in LoadCurrentColors() from VdwParameters.color.
        // Zmieniana via ColorDialog i Reset; zapisywana do VdwParameters.color
        // dopiero w ApplyChanges().
        private readonly Dictionary<int, Vector3> _workingColors
            = new Dictionary<int, Vector3>();

        #endregion

        #region Constructor

        public AtomPropertiesWindow()
        {
            InitializeComponent();
            DockWindowConfig.SetupWindow(this, "🎨 Atom Colors");

            // ⭐ CellClick (not CellContentClick) — patrz opis class.
            dgvElements.CellClick += DgvElements_CellClick;
            dgvElements.CellPainting += DgvElements_CellPainting;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Fills the table with current colours from VdwParameters.color.
        /// Called every time the window is shown.
        /// </summary>
        public void LoadCurrentColors()
        {
            _workingColors.Clear();
            dgvElements.Rows.Clear();

            int count = Math.Min(
                VdwParameters.color.Length,
                Math.Min(VdwParameters.ElementSymbols.Length,
                         VdwParameters.ElementNames.Length));

            // ⭐ Start od 1 — pomijamy indeks 0 (default gray, not pierwiastek)
            for (int z = 1; z < count; z++)
            {
                Vector3 c = VdwParameters.color[z];
                _workingColors[z] = c;

                int rowIdx = dgvElements.Rows.Add();
                var row = dgvElements.Rows[rowIdx];
                row.Tag = z;

                row.Cells[colZ.Index].Value = z.ToString();
                row.Cells[colSymbol.Index].Value = VdwParameters.ElementSymbols[z];
                row.Cells[colName.Index].Value = VdwParameters.ElementNames[z];
                row.Cells[colColor.Index].Value = "";   // rysowane przez CellPainting

                // ⭐ Rows with overridden colour — cream background
                if (VdwParameters.IsColorModified(z))
                {
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(255, 250, 220);
                }
            }

            lblInfo.Text = $"Loaded {count - 1} elements. Click a color cell to edit.";
        }

        #endregion

        #region DataGridView Events

        /// <summary>
        /// Opens ColorDialog after clicking the Color cell.
        /// We use CellClick (not CellContentClick), because the Color cell is ReadOnly
        /// with an empty value — CellContentClick would not fire.
        /// </summary>
        private void DgvElements_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colColor.Index) return;

            int z = (int)dgvElements.Rows[e.RowIndex].Tag;
            if (!_workingColors.TryGetValue(z, out var current))
                current = new Vector3(0.5f, 0.5f, 0.5f);

            using var dlg = new ColorDialog
            {
                Color = Color.FromArgb(
                    255,
                    (int)Math.Clamp(current.X * 255f, 0, 255),
                    (int)Math.Clamp(current.Y * 255f, 0, 255),
                    (int)Math.Clamp(current.Z * 255f, 0, 255)),
                FullOpen = true,
                AnyColor = true
            };

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            _workingColors[z] = new Vector3(
                dlg.Color.R / 255f,
                dlg.Color.G / 255f,
                dlg.Color.B / 255f);
            Flags.Instance.VisualRevision++;
            dgvElements.InvalidateRow(e.RowIndex);
        }

        /// <summary>
        /// Draws a colour swatch in the Color cell.
        /// </summary>
        private void DgvElements_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colColor.Index) return;

            int z = (int)dgvElements.Rows[e.RowIndex].Tag;
            if (!_workingColors.TryGetValue(z, out var c))
                c = new Vector3(0.5f, 0.5f, 0.5f);

            e.PaintBackground(e.CellBounds, true);

            Color col = Color.FromArgb(
                255,
                (int)Math.Clamp(c.X * 255f, 0, 255),
                (int)Math.Clamp(c.Y * 255f, 0, 255),
                (int)Math.Clamp(c.Z * 255f, 0, 255));

            var rect = new Rectangle(
                e.CellBounds.X + 4,
                e.CellBounds.Y + 4,
                e.CellBounds.Width - 8,
                e.CellBounds.Height - 8);

            using var brush = new SolidBrush(col);
            using var pen = new Pen(Color.FromArgb(80, 80, 80));
            e.Graphics.FillRectangle(brush, rect);
            e.Graphics.DrawRectangle(pen, rect);
            Flags.Instance.VisualRevision++;
            e.Handled = true;
        }

        #endregion

        #region Buttons

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Flags.Instance.VisualRevision++;
            ApplyChanges();
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            // ⭐ Nic not zapisujemy — VdwParameters.color pozostaje without zmian,
            //    the window closes, _workingColors is discarded on the next Load.
            Close();
        }

        private void BtnResetSelected_Click(object sender, EventArgs e)
        {
            if (dgvElements.SelectedRows.Count == 0) return;

            foreach (DataGridViewRow row in dgvElements.SelectedRows)
            {
                int z = (int)row.Tag;
                if (z >= 0 && z < VdwParameters.DefaultColor.Length)
                {
                    _workingColors[z] = VdwParameters.DefaultColor[z];
                }
            }
            Flags.Instance.VisualRevision++;
            dgvElements.Invalidate();
        }

        private void BtnResetAll_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "Reset colors of ALL elements to defaults?",
                    "Reset",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            for (int z = 1; z < VdwParameters.DefaultColor.Length; z++)
            {
                _workingColors[z] = VdwParameters.DefaultColor[z];
            }
            Flags.Instance.VisualRevision++;
            dgvElements.Invalidate();
        }

        #endregion

        #region Apply

        /// <summary>
        /// Saves changes:
        ///   1. VdwParameters.color[z] = new colour (immediately)
        ///   2. Flags.AtomColorOverrides[z] = [R,G,B] for entries that differ from the default
        ///   3. Zapis do JSON (SaveForce)
        ///   4. VisualRevision++ → RefreshAllDocuments()
        ///
        /// ⭐ Polyhedra hold a copied colour — if the active style is Polyhedrals,
        ///    need to rebuild them via SetStyle(4).
        /// </summary>
        private void ApplyChanges()
        {
            var overrides = Flags.Instance.AtomColorOverrides;
            overrides.Clear();

            int changed = 0;

            foreach (var kvp in _workingColors)
            {
                int z = kvp.Key;
                Vector3 newColor = kvp.Value;

                // ⭐ 1. Write to VdwParameters.color (directly)
                if (z >= 0 && z < VdwParameters.color.Length)
                {
                    VdwParameters.color[z] = newColor;
                }

                // ⭐ 2. If different from default — write to overrides (JSON)
                if (z < VdwParameters.DefaultColor.Length)
                {
                    Vector3 def = VdwParameters.DefaultColor[z];
                    bool different =
                        Math.Abs(newColor.X - def.X) > 1e-4f ||
                        Math.Abs(newColor.Y - def.Y) > 1e-4f ||
                        Math.Abs(newColor.Z - def.Z) > 1e-4f;

                    if (different)
                    {
                        overrides[z] = new[] { newColor.X, newColor.Y, newColor.Z };
                        changed++;
                    }
                }
            }

            // ⭐ 3. Poliedry — colour kopiowany raz w FindPolihedral().
            //    If the current style is Polyhedrals, rebuild.
            if (Flags.Instance.Presentation == 4)
            {
                var doc = MainForm.Instance?.GetActiveDocument();
                doc?.SetStyle(4);   // FindPolihedral + SetPolyhedrals + RefreshView
            }

            // ⭐ 4. Force redraw of 3D scenes in all documents
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
            Flags.Instance.SaveForce();
            Flags.Instance.VisualRevision++;
            MainForm.Instance?.RefreshAllDocuments();

            MainForm.Instance?.SetStatus(
                $"Atom colors updated: {changed} elements changed",
                $"Structures: {StructureStorage.Instance.StructureCount}",
                "🎨");

            System.Diagnostics.Debug.WriteLine(
                $"[AtomPropertiesWindow] Applied: {changed} overrides saved");
        }

        #endregion
    }
}