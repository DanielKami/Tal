// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure list dock window.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.Overlay;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class StructureListWindow : DockContent
    {
        private StructureStorage _storage;
        private Dictionary<string, OverlayMoleculeData> _overlayMolecules = new Dictionary<string, OverlayMoleculeData>();

        public StructureListWindow()
        {
            InitializeComponent();
            this.Text = "📋 Struktury";
            this.DockAreas = DockAreas.DockLeft | DockAreas.DockRight | DockAreas.Float;
            this.BackColor = System.Drawing.Color.FromArgb(248, 248, 250);
            this.Width = 560;

            _storage = StructureStorage.Instance;
            _storage.PropertyChanged += Storage_PropertyChanged;

            UpdateList();
        }

        // ============================================================
        // ⭐ RYSOWANIE LISTY
        // ============================================================

        private void StructureList_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (e.Item == null) return;

            var structure = e.Item.Tag as StructureData;
            if (structure == null) return;

            bool isSelected = e.Item.Selected;

            // ⭐ BACKGROUND
            if (isSelected)
            {
                using (var brush = new SolidBrush(Color.FromArgb(0, 122, 204)))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }
            }
            else
            {
                Color backColor = e.Item.Index % 2 == 0 ? Color.White : Color.FromArgb(248, 248, 250);
                using (var brush = new SolidBrush(backColor))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }
            }

            Color foreColor = isSelected ? Color.White : Color.FromArgb(51, 51, 51);

            // ⭐ KOLUMNA 0 — NAZWA STRUKTURY
            var nameBounds = e.Item.SubItems[0].Bounds;
            using (var brush = new SolidBrush(foreColor))
            using (var font = new Font("Segoe UI", 10F, FontStyle.Regular))
            {
                e.Graphics.DrawString(
                    structure.Name,
                    font,
                    brush,
                    nameBounds.Left + 6,
                    nameBounds.Top + 3
                );
            }

            // ⭐ KOLUMNA 1 — GRUPA PRZESTRZENNA (space symmetry)
            var spaceGroupBounds = e.Item.SubItems[1].Bounds;
            string spaceGroupText = GetSpaceGroupText(structure);
            using (var brush = new SolidBrush(foreColor))
            using (var font = new Font("Segoe UI", 9F, FontStyle.Regular))
            {
                e.Graphics.DrawString(
                    spaceGroupText,
                    font,
                    brush,
                    spaceGroupBounds.Left + 6,
                    spaceGroupBounds.Top + 4
                );
            }

            // ⭐ KOLUMNA 2 — KOLOROWY KWADRACIK
            var colorBounds = e.Item.SubItems[2].Bounds;

            Color structureColor;
            if (structure.Color != null)
            {
                structureColor = Color.FromArgb(
                    (int)(structure.Color.X * 255),
                    (int)(structure.Color.Y * 255),
                    (int)(structure.Color.Z * 255)
                );
            }
            else
            {
                structureColor = Color.FromArgb(0, 122, 204);
            }

            if (!structure.IsVisible)
            {
                structureColor = Color.FromArgb(
                    structureColor.R / 3,
                    structureColor.G / 3,
                    structureColor.B / 3
                );
            }

            int squareSize = 16;
            int x = colorBounds.Left + (colorBounds.Width - squareSize) / 2;
            int y = e.Bounds.Top + (e.Bounds.Height - squareSize) / 2;

            // SWATCH BACKGROUND
            using (var brush = new SolidBrush(Color.White))
            {
                e.Graphics.FillRectangle(brush, x - 1, y - 1, squareSize + 2, squareSize + 2);
            }

            // KOLOROWY KWADRACIK
            using (var brush = new SolidBrush(structureColor))
            {
                e.Graphics.FillRectangle(brush, x, y, squareSize, squareSize);
            }

            // RAMKA
            using (var pen = new Pen(Color.FromArgb(150, 150, 150), 1))
            {
                e.Graphics.DrawRectangle(pen, x, y, squareSize, squareSize);
            }

            // ⭐ IF INVISIBLE — STRIKETHROUGH
            if (!structure.IsVisible)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawLine(pen, x, y, x + squareSize, y + squareSize);
                    e.Graphics.DrawLine(pen, x + squareSize, y, x, y + squareSize);
                }
            }

            // RAMKA ZAZNACZENIA
            if (isSelected)
            {
                using (var pen = new Pen(Color.FromArgb(0, 122, 204), 2))
                {
                    e.Graphics.DrawRectangle(pen, e.Bounds);
                }
            }
        }

        private void StructureList_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var brush = new SolidBrush(Color.FromArgb(240, 240, 242)))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            using (var brush = new SolidBrush(Color.FromArgb(51, 51, 51)))
            using (var font = new Font("Segoe UI", 9F, FontStyle.Bold))
            {
                e.Graphics.DrawString(
                    e.Header.Text,
                    font,
                    brush,
                    e.Bounds.Left + 4,
                    e.Bounds.Top + 4
                );
            }

            // Separator line
            using var pen = new Pen(Color.FromArgb(200, 200, 200));
            e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
        }

        private void StructureList_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            // ⭐ DO NOT DRAW SUBITEMS SEPARATELY - EVERYTHING IS IN DrawItem
            e.DrawDefault = false;
        }

        // ============================================================
        // ⭐ METODY POMOCNICZE
        // ============================================================

        /// <summary>
        /// ⭐ Returns the space-group name of the structure.
        /// Fallback: numer grupy, if nazwa pusta.
        /// </summary>
        private string GetSpaceGroupText(StructureData structure)
        {
            if (structure == null) return "—";

            // Priority: textual name (e.g. "P 21/c")
            if (!string.IsNullOrWhiteSpace(structure.SpaceGroupName))
                return structure.SpaceGroupName.Trim();

            // Fallback: numer grupy (np. "14")
            if (structure.SpaceGroup > 0)
                return $"#{structure.SpaceGroup}";

            return "—";
        }

        /// <summary>
        /// ⭐ Returns the overlay transform (currently unused — may be useful later).
        /// </summary>
        private string GetTransformationText(StructureData structure)
        {
            var overlayMolecule = GetOverlayMoleculeForStructure(structure);
            if (overlayMolecule != null)
            {
                switch (overlayMolecule.CurrentTransformation)
                {
                    case TransformationMode.None:
                        return "None";
                    case TransformationMode.Mirror:
                        return "Mirror";

                    case TransformationMode.SymmetryCenter:
                        return "Inversion";
                    default:
                        return "None";
                }
            }
            return "None";
        }

        private OverlayMoleculeData GetOverlayMoleculeForStructure(StructureData structure)
        {
            foreach (var form in Application.OpenForms)
            {
                if (form is MoleculeOverlayWindow overlayWin)
                {
                    var overlayManager = overlayWin.GetOverlayManager();
                    if (overlayManager == null) continue;

                    foreach (var mol in overlayManager.OverlayMolecules)
                    {
                        if (mol.SourceName.Contains(structure.Name) ||
                            mol.SourceName.Contains(System.IO.Path.GetFileName(structure.SourceFileName)))
                        {
                            return mol;
                        }
                    }
                }
            }
            return null;
        }

        private void StructureList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_structureList.SelectedItems.Count > 0)
            {
                var structure = _structureList.SelectedItems[0].Tag as StructureData;
                if (structure != null)
                {
                    _storage.SelectedStructure = structure;
                }
            }
        }

        private void StructureList_DoubleClick(object sender, EventArgs e)
        {
            BtnOpenSelected_Click(null, null);
        }

        // ============================================================
        // ⭐ PRZYCISKI
        // ============================================================

        private void BtnOpenSelected_Click(object sender, EventArgs e)
        {
            if (_structureList.SelectedItems.Count > 0)
            {
                var structure = _structureList.SelectedItems[0].Tag as StructureData;
                if (structure != null)
                {
                    OpenStructureInNewWindow(structure);
                }
            }
        }

        private void BtnOpenAll_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in _structureList.Items)
            {
                var structure = item.Tag as StructureData;
                if (structure != null)
                {
                    OpenStructureInNewWindow(structure);
                }
            }
        }

        private void BtnOpenNew_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "Pliki CIF (*.cif)|*.cif|Pliki RES (*.res)|*.res|Pliki XYZ (*.xyz)|*.xyz|Pliki PDB (*.pdb)|*.pdb|Pliki MOL (*.mol)|*.mol|Wszystkie pliki (*.*)|*.*";
            dialog.Title = "Open File";
            dialog.Multiselect = false;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                mainForm?.LoadCifFile(dialog.FileName);
            }
        }

        private void BtnCloseFile_Click(object sender, EventArgs e)
        {
            if (_structureList.SelectedItems.Count == 0) return;

            var structure = _structureList.SelectedItems[0].Tag as StructureData;
            if (structure == null) return;

            string filePath = structure.FilePath;

            List<StructureData> toRemove;
            if (!string.IsNullOrEmpty(filePath))
            {
                toRemove = _storage.Structures
                    .Where(s => string.Equals(s.FilePath, filePath,
                                              StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else
            {
                toRemove = _storage.Structures
                    .Where(s => s.SourceFileName == structure.SourceFileName)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                _storage.StructuresByFile.Remove(filePath);
            }

            foreach (var item in toRemove)
            {
                _storage.Structures.Remove(item);
            }

            if (_storage.SelectedStructure != null &&
                !_storage.Structures.Contains(_storage.SelectedStructure))
            {
                _storage.SelectedStructure = _storage.Structures.Count > 0
                    ? _storage.Structures[0]
                    : null;
            }

            UpdateList();

            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            mainForm?.SetStatus(
                    $"Closed file: {structure.SourceFileName}",
                    $"Structures: {_storage.StructureCount}",
                    "❌");
        }

        private void BtnCloseAll_Click(object sender, EventArgs e)
        {
            _storage.Structures.Clear();
            _storage.StructuresByFile.Clear();
            _storage.SelectedStructure = null;
            UpdateList();

            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            mainForm?.SetStatus("Closed all structures",
                    $"Structures: 0", "❌");
        }

        // ============================================================
        // ⭐ VISIBILITY UPDATE
        // ============================================================

        private void UpdateStructureVisibility(StructureData structure)
        {
            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm == null) return;

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel == null) return;

            bool anyChanged = false;

            foreach (var doc in dockPanel.Documents)
            {
                if (doc is GUI.MonoGameDocument monoDoc)
                {
                    var molecule = monoDoc.GetMolecule();
                    if (molecule == null || molecule.NumberOfAtoms == 0) continue;

                    if (monoDoc.MatchesStructure(structure))
                    {
                        for (int i = 0; i < molecule.NumberOfAtoms; i++)
                        {
                            molecule.atom[i].show = structure.IsVisible;
                        }
                        monoDoc.RefreshView();
                        anyChanged = true;
                    }
                }
            }

            if (anyChanged)
            {
                Flags.Instance.VisualRevision++;
                Flags.Instance.MarkDirty();
            }
        }

        private void UpdateOverlayVisibility(StructureData structure, bool isVisible)
        {
            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm == null) return;

            foreach (var form in Application.OpenForms)
            {
                if (form is MoleculeOverlayWindow overlayWin)
                {
                    var overlayManager = overlayWin.GetOverlayManager();
                    if (overlayManager == null) continue;

                    foreach (var mol in overlayManager.OverlayMolecules)
                    {
                        if (mol.SourceName.Contains(structure.Name) ||
                            mol.SourceName.Contains(System.IO.Path.GetFileName(structure.SourceFileName)))
                        {
                            mol.IsVisible = isVisible;

                            for (int i = 0; i < mol.WorkingAtoms.Count; i++)
                            {
                                var atom = mol.WorkingAtoms[i];
                                atom.show = isVisible;
                                mol.WorkingAtoms[i] = atom;
                            }
                        }
                    }

                    overlayWin.RefreshListPublic();
                    overlayWin.RefreshOverlayViewPublic();
                    break;
                }
            }
        }

        // ============================================================
        // ⭐ METODY POMOCNICZE
        // ============================================================

        private void OpenStructureInNewWindow(StructureData structure)
        {
            if (structure == null) return;

            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            mainForm?.OpenStructureInNewWindow(structure);
        }

        private void Storage_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StructureStorage.Structures))
            {
                UpdateList();
            }
        }

        private void UpdateStatus(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[StructureList] {message}");
        }

        // ============================================================
        // ⭐ METODY PUBLICZNE
        // ============================================================

        public void UpdateList()
        {
            _structureList.BeginUpdate();
            _structureList.Items.Clear();
            _structureList.Groups.Clear();

            // ──────────────────────────────────────────────
            // Grupowanie after nazwie file
            // ──────────────────────────────────────────────
            var grouped = new Dictionary<string, List<StructureData>>(StringComparer.OrdinalIgnoreCase);

            foreach (var structure in _storage.Structures)
            {
                string key = string.IsNullOrWhiteSpace(structure.SourceFileName)
                    ? "(unknown)"
                    : structure.SourceFileName;

                if (!grouped.ContainsKey(key))
                    grouped[key] = new List<StructureData>();
                grouped[key].Add(structure);
            }

            foreach (var group in grouped.OrderBy(g => g.Key))
            {
                string fileName = group.Key;
                int count = group.Value.Count;

                // Group header = file name + structure count
                var lvGroup = new ListViewGroup(
                    key: fileName,
                    headerText: count > 1
                        ? $"📄  {fileName}   ({count} struktur)"
                        : $"📄  {fileName}"
                )
                {
                    HeaderAlignment = HorizontalAlignment.Left
                };
                _structureList.Groups.Add(lvGroup);

                // Structures inside the group
                bool multi = count > 1;
                int idx = 0;

                foreach (var structure in group.Value)
                {
                    idx++;

                    // Indent + tree marker only when the file has >1 record
                    string displayName;
                    if (multi)
                    {
                        bool last = (idx == count);
                        string branch = last ? "└─  " : "├─  ";
                        displayName = branch + structure.Name;
                    }
                    else
                    {
                        displayName = structure.Name;
                    }

                    // ⭐ Kolumna 1 = grupa przestrzenna (space symmetry)
                    var item = new ListViewItem(new string[]
                    {
                        displayName,                        // kolumna 0 — nazwa
                        GetSpaceGroupText(structure),       // kolumna 1 — grupa przestrzenna
                        ""                                  // column 2 — colour (drawn manually)
                    })
                    {
                        Tag = structure,
                        Checked = structure.IsVisible,
                        Group = lvGroup
                    };

                    // Slightly different text colour for multi-CIF records
                    if (multi)
                        item.ForeColor = Color.FromArgb(40, 40, 40);

                    _structureList.Items.Add(item);
                }
            }

            _structureList.EndUpdate();

            _lblInfo.Text = grouped.Count == 0
                ? "Brak struktur"
                : $"{_storage.StructureCount} struktur w {grouped.Count} plikach";

            // Select first item if nothing is selected
            if (_structureList.Items.Count > 0 && _structureList.SelectedItems.Count == 0)
                _structureList.Items[0].Selected = true;
        }

        public void RefreshList()
        {
            UpdateList();
        }

        public void SetStructureVisibility(string structureName, bool visible)
        {
            foreach (ListViewItem item in _structureList.Items)
            {
                if (item.Text == structureName)
                {
                    item.Checked = visible;
                    break;
                }
            }
        }
    }
}