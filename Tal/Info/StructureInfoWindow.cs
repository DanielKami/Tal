// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Structure info UI: StructureInfoWindow.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using Tal.Core.PowderDiffraction;
using SymmetryElements = Tal.SymmetryElements;
using Color = System.Drawing.Color;

namespace Tal.Forms
{
 /// <summary>
 /// Dockable window showing detailed structure information.
 /// Left panel: navigation buttons.
 /// Right panel: scrollable table with sections, filled according to
 /// the currently selected button. Each section defines its own columns.
 /// </summary>
    public partial class StructureInfoWindow : DockContent
    {
        #region Section Model

        private class Section
        {
            public string[] Headers;
            public List<string[]> Rows = new List<string[]>();

            public Section(params string[] headers)
            {
                Headers = headers;
            }

            public void Add(params string[] cells)
            {
                var row = new string[Headers.Length];
                for (int i = 0; i < Headers.Length; i++)
                    row[i] = i < cells.Length ? (cells[i] ?? "") : "";
                Rows.Add(row);
            }
        }

        #endregion

        #region Private Fields

        private readonly Dictionary<string, Section> _sections = new Dictionary<string, Section>();
        private DataGridView _grid;

        private string _currentSection = "Structure";
        private Molecule _molecule;
        private Measure _measure;
        private string _currentStructureId;

        #endregion

        #region Constructor

        public StructureInfoWindow()
        {
            InitializeComponent();

            btnStructure.Click += (s, e) => SelectSection("Structure");
            btnDiagram.Click += (s, e) => SelectSection("Diagram");
            btnSmiles.Click += (s, e) => SelectSection("SMILES");
            btnAtoms.Click += (s, e) => SelectSection("Atoms");
            btnBonds.Click += (s, e) => SelectSection("Bonds");
            btnContacts.Click += (s, e) => SelectSection("Contacts");
            btnHBonds.Click += (s, e) => SelectSection("H-Bonds");
            btnHBondTopo.Click += (s, e) => SelectSection("H-Bond Topology");
            btnCentroids.Click += (s, e) => SelectSection("Centroids");
            btnPlanes.Click += (s, e) => SelectSection("Planes");
            btnSymmetry.Click += (s, e) => SelectSection("Symmetry");
            btnDistances.Click += (s, e) => SelectSection("Distances");
            btnAngles.Click += (s, e) => SelectSection("Angles");
            btnTorsions.Click += (s, e) => SelectSection("Torsions");
            btnAllAngles.Click += (s, e) => SelectSection("All Angles");
            btnAllTorsions.Click += (s, e) => SelectSection("All Torsions");
            btnClose.Click += (s, e) => this.Close();
            btnCopySmiles.Click += BtnCopySmiles_Click;
            btnExportExcel.Click += BtnExportExcel_Click;

            InitializeEmptySections();
            SelectSection("Structure");
        }

        #endregion

        #region Public API

        public void LoadStructure(Molecule molecule, string structureId)
        {
            _molecule = molecule;
            _currentStructureId = structureId ?? "(unknown)";

            var doc = MainForm.Instance?.GetActiveDocument();
            _measure = doc?.GetMeasure();

            txtCurrentStructure.Text = _currentStructureId;

            PopulateSections();
            SelectSection(_currentSection);
        }

        #endregion

        #region Section Management

        private void SelectSection(string name)
        {
            _currentSection = name;
            HighlightActiveButton(name);

 // Angles — all possible angles from bonds
            if (name == "Angles")
            {
                PopulateAngles();
            }
 // Torsions — all possible torsions from bonds
            else if (name == "Torsions")
            {
                PopulateTorsions();
            }

            if (_sections.TryGetValue(name, out var section))
                FillTable(section);
            else
                FillTable(new Section("Message"));
        }

        private void HighlightActiveButton(string name)
        {
            var buttons = new[]
            {
                btnStructure, btnDiagram, btnSmiles, btnAtoms, btnBonds, btnContacts,
                btnHBonds, btnHBondTopo, btnCentroids, btnPlanes, btnSymmetry, btnDistances,
                btnAngles, btnTorsions, btnAllAngles, btnAllTorsions
            };

            foreach (var btn in buttons)
            {
                bool active = btn.Text == name;
                btn.BackColor = active
                    ? Color.FromArgb(200, 220, 240)
                    : SystemColors.Control;
                btn.FlatStyle = active ? FlatStyle.Standard : FlatStyle.Flat;
            }
        }

        private const int MaxLabelRows = 250;

        private void FillTable(Section section)
        {
            // Dispose previous controls to release window handles
            DisposeTableControls();

            if (section.Rows.Count > MaxLabelRows)
            {
                FillWithGrid(section);
                return;
            }

            table.Visible = true;
            if (_grid != null) _grid.Visible = false;

            table.SuspendLayout();
            table.Controls.Clear();
            table.RowStyles.Clear();
            table.ColumnStyles.Clear();

            table.RowCount = section.Rows.Count + 1;
            table.ColumnCount = section.Headers.Length;

            table.AutoSize = false;
            table.AutoSizeMode = AutoSizeMode.GrowOnly;
            table.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
            table.Padding = new Padding(4);

            ApplyColumnStyles(section);

            int rowHeight = 26;
            for (int r = 0; r < table.RowCount; r++)
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));

            for (int c = 0; c < section.Headers.Length; c++)
            {
                var h = new Label
                {
                    Text = section.Headers[c],
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(6, 4, 6, 4),
                    Margin = new Padding(0),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.Black,
                    BackColor = Color.FromArgb(225, 228, 232)
                };
                table.Controls.Add(h, c, 0);
            }

            for (int r = 0; r < section.Rows.Count; r++)
            {
                var row = section.Rows[r];
                for (int c = 0; c < section.Headers.Length; c++)
                {
                    string cellText = c < row.Length ? row[c] : "";
                    string hdr = section.Headers[c];

                    Color foreColor = Color.Black;
                    bool bold = false;

                    if (hdr == "Atom1" || hdr == "Atom2" ||
                        hdr == "Atom3" || hdr == "Atom4" ||
                        hdr == "Donor" || hdr == "H" || hdr == "Acceptor" ||
                        hdr == "Atom 1" || hdr == "Atom 2" ||
                        hdr == "Atom 3" || hdr == "Atom 4")
                    {
                        var col = GuessElementColor(cellText);
                        if (col.HasValue) { foreColor = col.Value; bold = true; }
                    }

                    Color backColor = (r % 2 == 0) ? Color.White : Color.FromArgb(245, 247, 250);
                    if ((hdr == "Cell.1" || hdr == "Cell.2") &&
                        cellText != "(0,0,0)" && !string.IsNullOrWhiteSpace(cellText))
                        backColor = Color.FromArgb(255, 245, 220);

                    if (_currentSection == "Structure" && c == 0)
                        bold = true;

                    if (hdr == "Error" && !string.IsNullOrEmpty(cellText) && cellText != "—")
                        backColor = Color.FromArgb(245, 250, 245);

                    var lbl = new Label
                    {
                        Text = cellText,
                        AutoSize = false,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(6, 4, 6, 4),
                        Margin = new Padding(0),
                        Font = new Font("Consolas", 9f, bold ? FontStyle.Bold : FontStyle.Regular),
                        ForeColor = foreColor,
                        BackColor = backColor
                    };
                    table.Controls.Add(lbl, c, r + 1);
                }
            }

            table.Width = CalculateTableWidth(section);
            table.Height = table.RowCount * rowHeight + table.Padding.Vertical;

            table.ResumeLayout();
            panelTableScroll.PerformLayout();
        }

        private void DisposeTableControls()
        {
            var toDispose = new List<Control>();
            foreach (Control c in table.Controls)
                toDispose.Add(c);
            table.Controls.Clear();
            foreach (var c in toDispose)
                c.Dispose();
        }

        private void FillWithGrid(Section section)
        {
            table.Visible = false;

            if (_grid == null)
            {
                _grid = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AllowUserToResizeRows = false,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                    BackgroundColor = Color.White,
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Consolas", 9f),
                    AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
                    {
                        BackColor = Color.FromArgb(245, 247, 250)
                    }
                };
                panelTableScroll.Controls.Add(_grid);
            }

            _grid.Visible = true;
            _grid.Columns.Clear();
            _grid.Rows.Clear();

            foreach (var h in section.Headers)
                _grid.Columns.Add(h, h);

            // Structure: bold first column
            bool structure = _currentSection == "Structure";

            foreach (var row in section.Rows)
            {
                var cells = new string[section.Headers.Length];
                for (int i = 0; i < cells.Length; i++)
                    cells[i] = i < row.Length ? row[i] : "";
                int idx = _grid.Rows.Add(cells);
                if (structure)
                    _grid.Rows[idx].Cells[0].Style.Font = new Font("Consolas", 9f, FontStyle.Bold);
            }

            // Note if truncated
            if (section.Rows.Count >= 2000)
            {
                _grid.Rows.Insert(0, new object[section.Headers.Length]);
                _grid.Rows[0].Cells[0].Value = $"Showing {section.Rows.Count} rows (large table — use Export Excel for full data)";
                _grid.Rows[0].DefaultCellStyle.ForeColor = Color.DarkBlue;
            }

            _grid.BringToFront();
        }

        #endregion

        #region Column Styles

        private void ApplyColumnStyles(Section section)
        {
            int n = section.Headers.Length;

            if (n == 1)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 500f));
                return;
            }

            if (n == 2)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 520f));
                return;
            }

 // Symmetry — 6 columns
            if (n == 6 && section.Headers[0] == "Number" && section.Headers[1] == "Symm. Op.")
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 295f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                return;
            }

 // Contacts — 20 columns
            if (n >= 15)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
                for (int i = 8; i < n; i++)
                    table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65f));
                return;
            }

 // H-Bonds — 13 columns
            if (n >= 10 && section.Headers[0] == "#")
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                return;
            }

 // Atoms — 10 columns
            if (n == 10)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40f));
                for (int i = 0; i < 6; i++)
                    table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75f));
                return;
            }

 // Bonds — 6 columns: #, Atom1, Atom2, Length (Å), Error, Occ
            if (n == 6 && section.Headers[1] == "Atom1" && section.Headers[2] == "Atom2")
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
                return;
            }

 // Distances — 5 columns: #, Atom 1, Atom 2, Distance, Error
            if (n == 5 && section.Headers[1] == "Atom 1" && section.Headers[2] == "Atom 2")
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                return;
            }

 // Angles — 6 columns: #, Atom 1, Atom 2, Atom 3, Angle, Error
            if (n == 6 && section.Headers[1] == "Atom 1" && section.Headers[3] == "Atom 3")
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                return;
            }

 // Torsions — 7 columns: #, Atom 1, Atom 2, Atom 3, Atom 4, Torsion, Error
            if (n == 7 && section.Headers[1] == "Atom 1" && section.Headers[4] == "Atom 4")
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                return;
            }

 // Generic 5 columns fallback
            if (n == 5)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75f));
                return;
            }

 // Fallback for 5-7 columns
            if (n >= 5 && n <= 7)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40f));
                for (int i = 1; i < n - 1; i++)
                    table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
                return;
            }

 // H-Bond Topology — 8 columns, last (Description) wide
            if (n == 8 && section.Headers[1] == "Type")
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320f));
                return;
            }

            // Structure — 2 columns, narrower
            if (n == 2 && section.Headers[0] == "Property")
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280f));
                return;
            }

 // Default
            for (int i = 0; i < n; i++)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
        }

        private int CalculateTableWidth(Section section)
        {
            int total = 0;
            foreach (ColumnStyle cs in table.ColumnStyles)
            {
                if (cs.SizeType == SizeType.Absolute)
                    total += (int)cs.Width;
                else if (cs.SizeType == SizeType.Percent)
                {
                    int panelWidth = panelTableScroll.ClientSize.Width - 10;
                    total += (int)(panelWidth * cs.Width / 100f);
                }
            }

            int minWidth = panelTableScroll.ClientSize.Width - 10;
            return Math.Max(total, minWidth);
        }

        private Color? GuessElementColor(string atomName)
        {
            if (string.IsNullOrEmpty(atomName)) return null;

            var sb = new StringBuilder();
            foreach (char c in atomName)
            {
                if (char.IsLetter(c)) sb.Append(c);
                else break;
            }
            string symbol = sb.ToString();
            if (string.IsNullOrEmpty(symbol)) return null;

            int z = VdwParameters.GetAtomicNumber(symbol);
            if (z <= 0) return null;

            var v = VdwParameters.color[Math.Min(z, VdwParameters.color.Length - 1)];

            int r = Math.Clamp((int)(v.X * 255 * 0.7f), 0, 255);
            int g = Math.Clamp((int)(v.Y * 255 * 0.7f), 0, 255);
            int b = Math.Clamp((int)(v.Z * 255 * 0.7f), 0, 255);

            return Color.FromArgb(r, g, b);
        }

        #endregion

        #region Empty Sections

        private void InitializeEmptySections()
        {
            _sections["Structure"] = new Section("Property", "Value");
            _sections["Diagram"] = new Section("#", "Atom1", "Atom2", "Length (Å)", "Order", "Type", "Ratio");
            _sections["SMILES"] = new Section("#", "Component", "SMILES", "Atoms");
            _sections["Atoms"] = new Section("Name", "El", "x", "y", "z", "σx", "σy", "σz", "Occ", "Ueq");

 // ⭐ Bonds — with Error column
            _sections["Bonds"] = new Section("#", "Atom1", "Atom2", "Length (Å)", "Error", "Occ");

            _sections["Contacts"] = new Section(
                "#", "Atom1", "Atom2", "Length", "Length-VdW",
                "Symm.op.1", "Symm.op.2", "Expanded",
                "X1", "Y1", "Z1", "X2", "Y2", "Z2",
                "Xfrac1", "Yfrac1", "Zfrac1", "Xfrac2", "Yfrac2", "Zfrac2");
            _sections["H-Bond Topology"] = new Section("#", "Type", "Notation", "Donors", "Acceptors", "H-bonds", "Degree", "Description");
            _sections["H-Bonds"] = new Section(
                "#", "Donor", "H", "Acceptor",
                "D⋯A (Å)", "H⋯A (Å)", "Lenght-VdW", "∠D-H⋯A (°)",
                "Intermolecular",
                "Symm.op.1", "Symm.op.2",
                "Cell.1", "Cell.2");
            _sections["Centroids"] = new Section("Message");
            _sections["Planes"] = new Section("#", "Name", "Type", "hkl", "Atoms", "Color", "A", "B", "C", "D");
            _sections["Symmetry"] = new Section(
                "Number", "Symm. Op.", "Description", "Detailed Description", "Order", "Type");

 // Distances / Angles / Torsions — with Error column
            _sections["Distances"] = new Section("#", "Atom 1", "Atom 2", "Distance (Å)", "Error");
            _sections["Angles"] = new Section("#", "Atom 1", "Atom 2", "Atom 3", "Angle (°)", "Error");
            _sections["Torsions"] = new Section("#", "Atom 1", "Atom 2", "Atom 3", "Atom 4", "Torsion (°)", "Error");
            _sections["All Angles"] = new Section("#", "Atom 1", "Atom 2", "Atom 3", "Angle (°)", "Error");
            _sections["All Torsions"] = new Section("#", "Atom 1", "Atom 2", "Atom 3", "Atom 4", "Torsion (°)", "Error");
        }

        #endregion

        #region Data Population

        private void PopulateSections()
        {
            InitializeEmptySections();
            if (_molecule == null) return;

            PopulateStructure();
            PopulateAtoms();
            PopulateBonds();
            PopulateDiagram();
            PopulateSmiles();
            PopulateContacts();
            PopulateHBonds();
            PopulateHBondTopology();
            PopulateSymmetry();
            PopulateMeasurements();
            PopulatePlanes();
        }

 /// <summary>
 /// Populates the "Structure" section with basic information,
 /// unit cell, chemical metadata, refinement statistics, reflection
 /// statistics, experiment details, and publication metadata.
 /// </summary>
        private void PopulateStructure()
        {
            var s = new Section("Property", "Value");

 // ═══════════════════════════════════════════════════════════
 // IDENTIFICATION
 // ═══════════════════════════════════════════════════════════
            s.Add("Identifier", _currentStructureId ?? "");
            s.Add("Literature Reference", string.IsNullOrEmpty(_molecule.JournalReference)
                                            ? "Unknown" : _molecule.JournalReference);
            s.Add("Publication Title", _molecule.PublTitle ?? "");
            s.Add("Journal", _molecule.JournalName ?? "");
            s.Add("DOI", _molecule.DOI ?? "");
            s.Add("Audit Creation Date", _molecule.AuditCreationDate ?? "");
            s.Add("Audit Creation Method", _molecule.AuditCreationMethod ?? "");

 // ═══════════════════════════════════════════════════════════
 // CHEMICAL
 // ═══════════════════════════════════════════════════════════
            s.Add("Formula", GetFormula(_molecule));
            s.Add("Compound Name", _molecule.Name ?? "");
            s.Add("Space Group", FormatSpaceGroup(_molecule));

 // ═══════════════════════════════════════════════════════════
 // UNIT CELL
 // ═══════════════════════════════════════════════════════════
            s.Add("a (Å)", FormatWithError(_molecule.a, _molecule.a_e));
            s.Add("b (Å)", FormatWithError(_molecule.b, _molecule.b_e));
            s.Add("c (Å)", FormatWithError(_molecule.c, _molecule.c_e));
            s.Add("α (°)", FormatAngleWithError(_molecule.alpha, _molecule.alpha_e));
            s.Add("β (°)", FormatAngleWithError(_molecule.beta, _molecule.beta_e));
            s.Add("γ (°)", FormatAngleWithError(_molecule.gamma, _molecule.gamma_e));
            s.Add("Cell Volume (Å³)", FormatVolume(_molecule));
            s.Add("Z, Z'", "");
            s.Add("Disorder", "");
            s.Add("Polymorph", "");

 // ═══════════════════════════════════════════════════════════
 // REFINEMENT STATISTICS
 // ═══════════════════════════════════════════════════════════
            if (_molecule.R1_Observed > 0)
                s.Add("R1 (F > 4σ)", _molecule.R1_Observed.ToString("F4"));
            if (_molecule.R1_All > 0)
                s.Add("R1 (all data)", _molecule.R1_All.ToString("F4"));
            if (_molecule.wR2_All > 0)
                s.Add("wR2 (all data)", _molecule.wR2_All.ToString("F4"));
            if (_molecule.wR2_Observed > 0 && _molecule.wR2_Observed != _molecule.wR2_All)
                s.Add("wR2 (observed)", _molecule.wR2_Observed.ToString("F4"));
            if (_molecule.GooF > 0)
                s.Add("GooF (S)", _molecule.GooF.ToString("F3"));

            if (_molecule.RefinementReflections > 0)
                s.Add("Reflections used", _molecule.RefinementReflections.ToString());
            if (_molecule.RefinementParameters > 0)
                s.Add("Parameters refined", _molecule.RefinementParameters.ToString());
            if (_molecule.RefinementRestraints > 0)
                s.Add("Restraints applied", _molecule.RefinementRestraints.ToString());

            if (_molecule.ShiftMax > 0)
                s.Add("Shift/su max", _molecule.ShiftMax.ToString("F4"));
            if (_molecule.ShiftMean > 0)
                s.Add("Shift/su mean", _molecule.ShiftMean.ToString("F4"));

            if (_molecule.DiffDensityMax != 0)
                s.Add("Δρ max (e/Å³)", _molecule.DiffDensityMax.ToString("F3"));
            if (_molecule.DiffDensityMin != 0)
                s.Add("Δρ min (e/Å³)", _molecule.DiffDensityMin.ToString("F3"));
            if (_molecule.DiffDensityRMS > 0)
                s.Add("Δρ RMS (e/Å³)", _molecule.DiffDensityRMS.ToString("F3"));

            if (!string.IsNullOrEmpty(_molecule.WeightingScheme))
                s.Add("Weighting scheme", _molecule.WeightingScheme);

 // ═══════════════════════════════════════════════════════════
 // REFLECTION STATISTICS
 // ═══════════════════════════════════════════════════════════
            if (_molecule.ReflnsTotal > 0)
                s.Add("Reflections total", _molecule.ReflnsTotal.ToString());
            if (_molecule.ReflnsObserved > 0)
                s.Add("Reflections observed", _molecule.ReflnsObserved.ToString());
            if (!string.IsNullOrEmpty(_molecule.ReflnsThreshold))
                s.Add("Reflections threshold", _molecule.ReflnsThreshold);

            if (_molecule.ResolutionHigh > 0)
                s.Add("Resolution d_min (Å)", _molecule.ResolutionHigh.ToString("F4"));
            if (_molecule.ResolutionLow > 0)
                s.Add("Resolution d_max (Å)", _molecule.ResolutionLow.ToString("F4"));
            if (_molecule.ThetaMin > 0)
                s.Add("θ_min (°)", _molecule.ThetaMin.ToString("F2"));
            if (_molecule.ThetaMax > 0)
                s.Add("θ_max (°)", _molecule.ThetaMax.ToString("F2"));

 // ═══════════════════════════════════════════════════════════
 // EXPERIMENT
 // ═══════════════════════════════════════════════════════════
            if (_molecule.F000 > 0)
                s.Add("F(000)", _molecule.F000.ToString("F1"));

            if (_molecule.Wavelength > 0)
                s.Add("Wavelength (Å)", _molecule.Wavelength.ToString("F4"));

            if (!string.IsNullOrEmpty(_molecule.RadiationType))
                s.Add("Radiation type", _molecule.RadiationType);

            if (!string.IsNullOrEmpty(_molecule.MeasurementDevice))
                s.Add("Measurement device", _molecule.MeasurementDevice);

            if (_molecule.Temperature > 0)
                s.Add("Temperature (K)", _molecule.Temperature.ToString("F1"));

            if (!string.IsNullOrEmpty(_molecule.CrystalColour))
                s.Add("Crystal colour", _molecule.CrystalColour);

            if (!string.IsNullOrEmpty(_molecule.CrystalDescription))
                s.Add("Crystal shape", _molecule.CrystalDescription);

            if (_molecule.CrystalSizeMax > 0)
            {
                string sizes = $"{_molecule.CrystalSizeMax:F4} × " +
                               $"{_molecule.CrystalSizeMid:F4} × " +
                               $"{_molecule.CrystalSizeMin:F4} mm";
                s.Add("Crystal size", sizes);
            }

            if (_molecule.AbsorptionCoefficient > 0)
                s.Add("Absorption coefficient (mm⁻¹)", _molecule.AbsorptionCoefficient.ToString("F3"));

            if (!string.IsNullOrEmpty(_molecule.AbsorptionCorrectionType))
                s.Add("Absorption correction", _molecule.AbsorptionCorrectionType);

            if (_molecule.AbsCorrectionTMin > 0 || _molecule.AbsCorrectionTMax > 0)
            {
                s.Add("Absorption T_min / T_max",
                      $"{_molecule.AbsCorrectionTMin:F4} / {_molecule.AbsCorrectionTMax:F4}");
            }

            if (_molecule.DensityDiffrn > 0)
                s.Add("Density (g/cm³)", _molecule.DensityDiffrn.ToString("F4"));

            if (_molecule.RFactor > 0 && _molecule.R1_Observed == 0)
                s.Add("R-Factor (%)", (_molecule.RFactor * 100f).ToString("F2"));

            _sections["Structure"] = s;
        }

        private void PopulateAtoms()
        {
            var s = new Section("Name", "El", "x", "y", "z", "σx", "σy", "σz", "Occ", "Ueq");

            if (_molecule.atom == null) { _sections["Atoms"] = s; return; }

            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
            {
                var a = _molecule.atom[i];
                float ueq = (a.Ue.M11 + a.Ue.M22 + a.Ue.M33) / 3f;
                s.Add(
                    a.name ?? $"atom{i}",
                    GetElementSymbol(a.AtomicNumber),
                    a.Coordinate.X.ToString("F4"),
                    a.Coordinate.Y.ToString("F4"),
                    a.Coordinate.Z.ToString("F4"),
                    a.Coordinate_error.X.ToString("F4"),
                    a.Coordinate_error.Y.ToString("F4"),
                    a.Coordinate_error.Z.ToString("F4"),
                    a.occupancy.ToString("F3"),
                    ueq.ToString("F4")
                );
            }

            _sections["Atoms"] = s;
        }

 /// <summary>
 /// Populates the "Bonds" section from the covalent bond graph.
 /// Includes an Error column with crystallographic error estimate.
 /// </summary>

        /// <summary>
        /// Chemical-diagram style table: covalent bonds with estimated bond
        /// multiplicity from length vs sum of covalent radii (Pauling-like).
        /// Order ≈ exp((R₀ − d) / 0.28); Type = single / double / triple / arom?.
        /// </summary>
        private void PopulateDiagram()
        {
            var s = new Section("#", "Atom1", "Atom2", "Length (Å)", "Order", "Type", "Ratio");

            if (_molecule?.atom == null)
            {
                _sections["Diagram"] = s;
                return;
            }

            var bondSet = new HashSet<(int, int)>();
            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
            {
                var a = _molecule.atom[i];
                if (a.BondedAtom == null) continue;
                for (int k = 0; k < a.NrBondedAtoms; k++)
                {
                    int j = a.BondedAtom[k];
                    if (j < 0 || j >= _molecule.NumberOfAtoms || j == i) continue;
                    bondSet.Add((Math.Min(i, j), Math.Max(i, j)));
                }
            }

            int idx = 1;
            int singles = 0, doubles = 0, triples = 0, arom = 0;

            foreach (var (i, j) in bondSet.OrderBy(b => _molecule.atom[b.Item1].name)
                                          .ThenBy(b => _molecule.atom[b.Item2].name))
            {
                var a1 = _molecule.atom[i];
                var a2 = _molecule.atom[j];
                float d = Vector3.Distance(a1.Coordinate, a2.Coordinate);
                float r0 = CovalentRadius(a1.AtomicNumber) + CovalentRadius(a2.AtomicNumber);
                if (r0 < 0.2f) r0 = 1.5f;

                // Pauling bond-order estimate
                float order = (float)Math.Exp((r0 - d) / 0.28f);
                if (order < 0.5f) order = 0.5f;
                if (order > 3.2f) order = 3.2f;

                float ratio = d / r0;
                string type;
                if (IsAromaticCandidate(a1.AtomicNumber, a2.AtomicNumber, d))
                {
                    type = "arom?";
                    arom++;
                }
                else if (order >= 2.5f)
                {
                    type = "triple";
                    triples++;
                }
                else if (order >= 1.6f)
                {
                    type = "double";
                    doubles++;
                }
                else
                {
                    type = "single";
                    singles++;
                }

                s.Add(
                    idx.ToString(),
                    a1.name,
                    a2.name,
                    d.ToString("F3"),
                    order.ToString("F2"),
                    type,
                    ratio.ToString("F3"));
                idx++;
            }

            // Summary row
            s.Add(
                "—",
                "Σ",
                $"{bondSet.Count} bonds",
                "",
                "",
                $"1×{singles}  2×{doubles}  3×{triples}  arom?{arom}",
                "");

            _sections["Diagram"] = s;
        }

        /// <summary>Approximate covalent radii (Å) for common elements.</summary>
        private static float CovalentRadius(int z)
        {
            return z switch
            {
                1 => 0.31f,   // H
                5 => 0.84f,   // B
                6 => 0.76f,   // C
                7 => 0.71f,   // N
                8 => 0.66f,   // O
                9 => 0.57f,   // F
                14 => 1.11f,  // Si
                15 => 1.07f,  // P
                16 => 1.05f,  // S
                17 => 1.02f,  // Cl
                35 => 1.20f,  // Br
                53 => 1.39f,  // I
                26 => 1.32f,  // Fe
                27 => 1.26f,  // Co
                28 => 1.24f,  // Ni
                29 => 1.32f,  // Cu
                30 => 1.22f,  // Zn
                _ => 0.70f + 0.02f * Math.Max(z, 1)
            };
        }

        private static bool IsAromaticCandidate(int z1, int z2, float d)
        {
            // Typical aromatic C–C ~1.39 Å; C–N ~1.34 Å
            bool organic = (z1 == 6 || z1 == 7) && (z2 == 6 || z2 == 7);
            if (!organic) return false;
            return d >= 1.32f && d <= 1.45f;
        }

        private void PopulateBonds()
        {
            var s = new Section("#", "Atom1", "Atom2", "Length (Å)", "Error", "Occ");

            if (_molecule.atom == null) { _sections["Bonds"] = s; return; }

            var bondSet = new HashSet<(int, int)>();

            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
            {
                var a = _molecule.atom[i];
                for (int k = 0; k < a.NrBondedAtoms; k++)
                {
                    int j = a.BondedAtom[k];
                    if (j < 0 || j >= _molecule.NumberOfAtoms || j == i) continue;
                    int lo = Math.Min(i, j);
                    int hi = Math.Max(i, j);
                    bondSet.Add((lo, hi));
                }
            }

            var sorted = bondSet.OrderBy(b => _molecule.atom[b.Item1].name)
                                .ThenBy(b => _molecule.atom[b.Item2].name)
                                .ToList();

            int idx = 1;
            foreach (var (i, j) in sorted)
            {
                var a1 = _molecule.atom[i];
                var a2 = _molecule.atom[j];
                float d = Vector3.Distance(a1.Coordinate, a2.Coordinate);
                float occ = Math.Min(a1.occupancy, a2.occupancy);

                float err = EstimateBondError(a1, a2);

                s.Add(
                    idx.ToString(),
                    a1.name,
                    a2.name,
                    d.ToString("F4"),
                    err > 0 ? err.ToString("F4") : "—",
                    occ.ToString("F3"));
                idx++;
            }

            _sections["Bonds"] = s;
        }

        private void PopulateContacts()
        {
            var s = new Section(
                "#", "Atom1", "Atom2", "Length", "Length-VdW",
                "Symm.op.1", "Symm.op.2", "Expanded",
                "X1", "Y1", "Z1", "X2", "Y2", "Z2",
                "Xfrac1", "Yfrac1", "Zfrac1", "Xfrac2", "Yfrac2", "Zfrac2");

            if (_molecule?.atom == null) { _sections["Contacts"] = s; return; }

            var pairs = new HashSet<(int, int)>();
            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
            {
                var a = _molecule.atom[i];
                if (a.ContactCount <= 0) continue;
                for (int k = 0; k < a.ContactCount; k++)
                {
                    int j = a.ContactAtom[k];
                    if (j < 0 || j >= _molecule.NumberOfAtoms || j == i) continue;
                    int lo = Math.Min(i, j);
                    int hi = Math.Max(i, j);
                    pairs.Add((lo, hi));
                }
            }

            FillContactLikeRows(s, pairs);
            _sections["Contacts"] = s;
        }


        private void PopulateHBondTopology()
        {
            var s = new Section("#", "Type", "Notation", "Donors", "Acceptors", "H-bonds", "Degree", "Description");
            if (_molecule?.atom == null) { _sections["H-Bond Topology"] = s; return; }

            var motifs = Tal.Calculate.HBondTopology.Analyze(_molecule);
            int idx = 1;
            foreach (var m in motifs)
            {
                s.Add(
                    idx.ToString(),
                    m.Type ?? "",
                    m.Notation ?? "",
                    m.Donors.ToString(),
                    m.Acceptors.ToString(),
                    m.HBonds.ToString(),
                    m.Degree.ToString(),
                    m.Description ?? "");
                idx++;
            }
            if (s.Rows.Count == 0 || (s.Rows.Count == 1 && s.Rows[0][1] == "—"))
            {
                s.Rows.Clear();
                s.Add("—", "—", "—", "—", "—", "—", "—",
                      "Enable H-bonds in Display Options to search for topology");
            }
            _sections["H-Bond Topology"] = s;
        }

        private void PopulateHBonds()
        {
            var s = new Section(
                "#", "Donor", "H", "Acceptor",
                "D⋯A (Å)", "H⋯A (Å)", "H⋯A - VdW", "∠D-H⋯A (°)",
                "Intermolecular",
                "Symm.op.1", "Symm.op.2",
                "Cell.1", "Cell.2");

            if (_molecule?.atom == null) { _sections["H-Bonds"] = s; return; }

            var seen = new HashSet<(int, int, int)>();
            int idx = 1;

            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
            {
                var a = _molecule.atom[i];
                if (a.H_NrBondedAtoms <= 0) continue;

                bool drawFromH = Flags.Instance.HBondDrawFromHydrogen;

                if (drawFromH && a.AtomicNumber == 1)
                {
                    int donor = -1;
                    for (int j = 0; j < a.NrBondedAtoms; j++)
                    {
                        int cand = a.BondedAtom[j];
                        if (cand >= 0 && cand < _molecule.NumberOfAtoms)
                        {
                            int z = _molecule.atom[cand].AtomicNumber;
                            if (z == 7 || z == 8 || z == 9) { donor = cand; break; }
                        }
                    }
                    if (donor < 0) continue;

                    for (int k = 0; k < a.H_NrBondedAtoms; k++)
                    {
                        int acc = a.H_BondedAtom[k];
                        if (acc < 0 || acc >= _molecule.NumberOfAtoms) continue;
                        if (!seen.Add((donor, i, acc))) continue;
                        AddHBondRow(s, ref idx, donor, i, acc);
                    }
                }
                else if (!drawFromH && (a.AtomicNumber == 7 || a.AtomicNumber == 8 || a.AtomicNumber == 9))
                {
                    int hIdx = -1;
                    for (int j = 0; j < a.NrBondedAtoms; j++)
                    {
                        int cand = a.BondedAtom[j];
                        if (cand >= 0 && cand < _molecule.NumberOfAtoms &&
                            _molecule.atom[cand].AtomicNumber == 1)
                        {
                            hIdx = cand;
                            break;
                        }
                    }
                    if (hIdx < 0) continue;

                    for (int k = 0; k < a.H_NrBondedAtoms; k++)
                    {
                        int acc = a.H_BondedAtom[k];
                        if (acc < 0 || acc >= _molecule.NumberOfAtoms) continue;
                        if (!seen.Add((i, hIdx, acc))) continue;
                        AddHBondRow(s, ref idx, i, hIdx, acc);
                    }
                }
            }

            if (s.Rows.Count == 0)
                s.Add("—", "—", "—", "—", "—", "—", "—", "—",
                      "Enable H-bonds in Display Options to search", "—", "—", "—", "—");
            _sections["H-Bonds"] = s;
        }

        private void AddHBondRow(Section s, ref int idx, int donorIdx, int hIdx, int acceptorIdx)
        {
            var donor = _molecule.atom[donorIdx];
            var h = _molecule.atom[hIdx];
            var acceptor = _molecule.atom[acceptorIdx];

            float dDA = Vector3.Distance(donor.Coordinate, acceptor.Coordinate);
            float dHA = Vector3.Distance(h.Coordinate, acceptor.Coordinate);

            float vdwH = VdwParameters.GetRadius(h.AtomicNumber);
            float vdwAcceptor = VdwParameters.GetRadius(acceptor.AtomicNumber);
            float hMinusVdW = dHA - (vdwH + vdwAcceptor);

            Vector3 v1 = donor.Coordinate - h.Coordinate;
            Vector3 v2 = acceptor.Coordinate - h.Coordinate;
            float l1 = v1.Length(), l2 = v2.Length();
            float angleDHA = 0f;
            if (l1 > 1e-4f && l2 > 1e-4f)
            {
                float cos = Math.Clamp(Vector3.Dot(v1, v2) / (l1 * l2), -1f, 1f);
                angleDHA = MathHelperTal.ToDegrees((float)Math.Acos(cos));
            }

            bool diffCell = (donor.CellShiftX != acceptor.CellShiftX ||
                             donor.CellShiftY != acceptor.CellShiftY ||
                             donor.CellShiftZ != acceptor.CellShiftZ);
            bool diffMol = donor.IndividualMoleculeInCell != acceptor.IndividualMoleculeInCell;
            bool inter = diffCell || diffMol;

            string symmD = FormatSymmetryOpFull(donor.SymmetryEquivalentNumberInCell,
                donor.CellShiftX, donor.CellShiftY, donor.CellShiftZ);
            string symmA = FormatSymmetryOpFull(acceptor.SymmetryEquivalentNumberInCell,
                acceptor.CellShiftX, acceptor.CellShiftY, acceptor.CellShiftZ);

            string cellStrD = $"({donor.CellShiftX},{donor.CellShiftY},{donor.CellShiftZ})";
            string cellStrA = $"({acceptor.CellShiftX},{acceptor.CellShiftY},{acceptor.CellShiftZ})";

            s.Add(
                idx.ToString(),
                donor.name ?? $"atom{donorIdx}",
                h.name ?? $"H{hIdx}",
                acceptor.name ?? $"atom{acceptorIdx}",
                dDA.ToString("F3"),
                dHA.ToString("F3"),
                hMinusVdW.ToString("F3"),
                angleDHA.ToString("F1"),
                inter ? "Yes" : "No",
                symmD,
                symmA,
                cellStrD,
                cellStrA
            );
            idx++;
        }

        private void FillContactLikeRows(Section s, HashSet<(int, int)> pairs)
        {
            Matrix fracToCart = _molecule.FractionaltoCarttesianMatrix(0, 0, 0);
            Matrix cartToFrac = Matrix.Invert(Matrix.Transpose(fracToCart));

            int idx = 1;
            foreach (var (i, j) in pairs.OrderBy(b => _molecule.atom[b.Item1].name))
            {
                var a1 = _molecule.atom[i];
                var a2 = _molecule.atom[j];

                float length = Vector3.Distance(a1.Coordinate, a2.Coordinate);

                float vdw1 = VdwParameters.GetRadius(a1.AtomicNumber);
                float vdw2 = VdwParameters.GetRadius(a2.AtomicNumber);
                float lengthVdW = length - (vdw1 + vdw2);

                string symm1 = FormatSymmetryOpFull(a1.SymmetryEquivalentNumberInCell,
                    a1.CellShiftX, a1.CellShiftY, a1.CellShiftZ);
                string symm2 = FormatSymmetryOpFull(a2.SymmetryEquivalentNumberInCell,
                    a2.CellShiftX, a2.CellShiftY, a2.CellShiftZ);

                bool expanded = false;
                if (a1.ContactIsNew != null && a1.ContactCount > 0)
                {
                    for (int k = 0; k < a1.ContactCount; k++)
                    {
                        if (a1.ContactAtom[k] == j && a1.ContactIsNew[k])
                        {
                            expanded = true;
                            break;
                        }
                    }
                }

                var p1 = a1.Coordinate;
                var p2 = a2.Coordinate;

                Vector3 shift = _molecule.MoleculeShift;
                var p1raw = p1 + shift;
                var p2raw = p2 + shift;
                var f1 = Vector3.Transform(p1raw, cartToFrac);
                var f2 = Vector3.Transform(p2raw, cartToFrac);

                s.Add(
                    idx.ToString(),
                    a1.name,
                    a2.name,
                    length.ToString("F3"),
                    lengthVdW.ToString("F3"),
                    symm1,
                    symm2,
                    expanded ? "Yes" : "No",
                    p1.X.ToString("F4"), p1.Y.ToString("F4"), p1.Z.ToString("F4"),
                    p2.X.ToString("F4"), p2.Y.ToString("F4"), p2.Z.ToString("F4"),
                    f1.X.ToString("F4"), f1.Y.ToString("F4"), f1.Z.ToString("F4"),
                    f2.X.ToString("F4"), f2.Y.ToString("F4"), f2.Z.ToString("F4")
                );
                idx++;
            }
        }

        private void PopulateSymmetry()
        {
            var s = new Section("Number", "Symm. Op.", "Description", "Detailed Description", "Order", "Type");

            if (_molecule.SymmetryOperations == null) { _sections["Symmetry"] = s; return; }

            for (int i = 0; i < _molecule.SymmetryOperations.Count; i++)
            {
                var op = _molecule.SymmetryOperations[i];
                string opStr = FormatSymmetryOpString(op);
                var (description, detailed, order, type) = ClassifySymmetryOp(op);

                s.Add(
                    (i + 1).ToString(),
                    opStr,
                    description,
                    detailed,
                    order.ToString(),
                    type
                );
            }

            _sections["Symmetry"] = s;
        }

 /// <summary>
 /// Populates the "Distances" section from Measure measurements.
 /// Includes an Error column with crystallographic error estimate.
 /// </summary>
        private void PopulateMeasurements()
        {
            var dist = new Section("#", "Atom 1", "Atom 2", "Distance (Å)", "Error");

            if (_measure == null || _measure.measurement == null)
            {
                _sections["Distances"] = dist;
                return;
            }

            int dc = 0;
            foreach (var m in _measure.measurement)
            {
                switch (m.type)
                {
                    case 1:
                        dist.Add((++dc).ToString(),
                            GetAtomName(m.Atom1), GetAtomName(m.Atom2),
                            m.value.ToString("F4"),
                            m.error > 0 ? m.error.ToString("F4") : "—");
                        break;
                    case 4:
                        dist.Add((++dc).ToString(),
                            GetAtomName(m.Atom1), "→plane",
                            m.value.ToString("F4"),
                            m.error > 0 ? m.error.ToString("F4") : "—");
                        break;
                }
            }

            _sections["Distances"] = dist;
        }


        private void PopulateSmiles()
        {
            var s = new Section("#", "Component", "SMILES", "Atoms");
            if (_molecule == null || _molecule.NumberOfAtoms == 0)
            {
                s.Add("—", "—", "(no molecule)", "0");
                _sections["SMILES"] = s;
                return;
            }

            var parts = SmilesWriter.GeneratePerComponent(_molecule, omitHydrogens: true);
            if (parts.Count == 0)
            {
                s.Add("—", "—", "(no heavy-atom components)", "0");
                _sections["SMILES"] = s;
                return;
            }

            // Atom counts per component approx: split by generation again is heavy —
            // show combined count of non-H shown atoms
            int heavy = 0;
            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
                if (_molecule.atom[i].show && _molecule.atom[i].AtomicNumber != 1)
                    heavy++;

            for (int i = 0; i < parts.Count; i++)
            {
                s.Add(
                    (i + 1).ToString(),
                    $"mol {i + 1}",
                    parts[i],
                    i == 0 ? heavy.ToString() : "");
            }

            // Full string as last row for easy copy-all
            if (parts.Count > 1)
                s.Add("—", "all", string.Join(".", parts), heavy.ToString());

            _sections["SMILES"] = s;
        }

        private void BtnCopySmiles_Click(object sender, EventArgs e)
        {
            if (_molecule == null || _molecule.NumberOfAtoms == 0)
            {
                MessageBox.Show("No molecule loaded.", "SMILES",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string smiles = SmilesWriter.Generate(_molecule, omitHydrogens: true);
            if (string.IsNullOrEmpty(smiles))
            {
                MessageBox.Show("Could not build SMILES (no heavy atoms / bonds?).",
                    "SMILES", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                Clipboard.SetText(smiles);
                MessageBox.Show(
                    "SMILES copied to clipboard:\n\n" + TruncateForMsg(smiles, 500),
                    "SMILES", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Clipboard error: " + ex.Message, "SMILES",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Export to Excel / CSV

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            if (_sections == null || _sections.Count == 0)
            {
                MessageBox.Show(this, "No table data to export.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string safeName = string.IsNullOrWhiteSpace(_currentStructureId)
                ? "structure"
                : string.Join("_", _currentStructureId.Split(Path.GetInvalidFileNameChars()));

            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Export structure tables";
                dlg.FileName = $"{safeName}_{_currentSection.Replace(' ', '_')}";
                dlg.Filter =
                    "CSV for Excel (*.csv)|*.csv|" +
                    "Excel XML workbook – all sections (*.xls)|*.xls|" +
                    "Tab-separated (*.tsv)|*.tsv";
                dlg.FilterIndex = 1;
                dlg.AddExtension = true;
                dlg.OverwritePrompt = true;

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    string path = dlg.FileName;
                    string ext = Path.GetExtension(path).ToLowerInvariant();

                    if (ext == ".xls" || dlg.FilterIndex == 2)
                        ExportAllSectionsExcelXml(path);
                    else if (ext == ".tsv" || dlg.FilterIndex == 3)
                        ExportSectionDelimited(path, '\t', currentOnly: true);
                    else
                        ExportSectionDelimited(path, ',', currentOnly: true);

                    MessageBox.Show(this,
                        $"Exported:\n{path}",
                        "Export",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        $"Export failed:\n{ex.Message}",
                        "Export",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>Write current (or all) section(s) as CSV/TSV with UTF-8 BOM for Excel.</summary>
        private void ExportSectionDelimited(string filePath, char separator, bool currentOnly)
        {
            var sb = new StringBuilder();
            void writeSection(Section sec, string title)
            {
                if (sec == null) return;
                if (!currentOnly)
                {
                    sb.Append(EscapeDelimited($"# {title}", separator));
                    sb.AppendLine();
                }
                if (sec.Headers != null && sec.Headers.Length > 0)
                {
                    sb.AppendLine(string.Join(separator.ToString(),
                        sec.Headers.Select(h => EscapeDelimited(h, separator))));
                }
                foreach (var row in sec.Rows)
                {
                    var cells = new string[sec.Headers.Length];
                    for (int i = 0; i < sec.Headers.Length; i++)
                        cells[i] = EscapeDelimited(i < row.Length ? row[i] : "", separator);
                    sb.AppendLine(string.Join(separator.ToString(), cells));
                }
                if (!currentOnly)
                    sb.AppendLine();
            }

            if (currentOnly)
            {
                if (!_sections.TryGetValue(_currentSection, out var sec) || sec == null)
                    throw new InvalidOperationException($"Section '{_currentSection}' is empty.");
                writeSection(sec, _currentSection);
            }
            else
            {
                foreach (var kvp in _sections)
                    writeSection(kvp.Value, kvp.Key);
            }

            // UTF-8 BOM so Excel opens Polish characters correctly
            var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            File.WriteAllText(filePath, sb.ToString(), utf8Bom);
        }

        private static string EscapeDelimited(string value, char separator)
        {
            if (value == null) value = "";
            bool needQuotes = value.IndexOfAny(new[] { separator, '"', '\r', '\n' }) >= 0;
            if (needQuotes)
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        /// <summary>
        /// Excel 2003 XML Spreadsheet (opens in Excel without extra libraries).
        /// One worksheet per section.
        /// </summary>
        private void ExportAllSectionsExcelXml(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");

            foreach (var kvp in _sections)
            {
                var sec = kvp.Value;
                if (sec == null) continue;
                string sheetName = SanitizeSheetName(kvp.Key);
                sb.AppendLine($" <Worksheet ss:Name=\"{XmlEscape(sheetName)}\">");
                sb.AppendLine("  <Table>");

                if (sec.Headers != null)
                {
                    sb.AppendLine("   <Row>");
                    foreach (var h in sec.Headers)
                    {
                        sb.AppendLine("    <Cell><Data ss:Type=\"String\">"
                            + XmlEscape(h ?? "") + "</Data></Cell>");
                    }
                    sb.AppendLine("   </Row>");
                }

                foreach (var row in sec.Rows)
                {
                    sb.AppendLine("   <Row>");
                    int n = sec.Headers?.Length ?? row.Length;
                    for (int i = 0; i < n; i++)
                    {
                        string cell = i < row.Length ? (row[i] ?? "") : "";
                        string type = LooksNumeric(cell) ? "Number" : "String";
                        if (type == "Number")
                            sb.AppendLine("    <Cell><Data ss:Type=\"Number\">"
                                + XmlEscape(cell.Replace(',', '.')) + "</Data></Cell>");
                        else
                            sb.AppendLine("    <Cell><Data ss:Type=\"String\">"
                                + XmlEscape(cell) + "</Data></Cell>");
                    }
                    sb.AppendLine("   </Row>");
                }

                sb.AppendLine("  </Table>");
                sb.AppendLine(" </Worksheet>");
            }

            sb.AppendLine("</Workbook>");
            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(false));
        }

        private static string SanitizeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Sheet";
            var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            foreach (var c in invalid)
                name = name.Replace(c, '_');
            if (name.Length > 31) name = name.Substring(0, 31);
            return name;
        }

        private static string XmlEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;");
        }

        private static bool LooksNumeric(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.Trim().Replace(',', '.');
            return double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out _);
        }

        #endregion



        private static string TruncateForMsg(string s, int max)
            => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max) + "…");

        private void PopulatePlanes()
        {
            var s = new Section("#", "Name", "Type", "hkl", "Atoms", "Color", "A", "B", "C", "D");

            var planes = MainForm.Instance?.GetPlanes();
            if (planes == null || planes.Count == 0)
            {
                _sections["Planes"] = s;
                return;
            }

            int idx = 1;
            foreach (var p in planes)
            {
                // Miller indices when HKL plane
                string hkl = "—";
                if (p.Type == CrystalPlaneType.HKL)
                    hkl = $"({p.H} {p.K} {p.L})";

                // Atoms the plane was built from (mean plane / selection)
                string atoms = "—";
                if (p.AtomIndices != null && p.AtomIndices.Count > 0)
                {
                    var names = new List<string>();
                    foreach (int ai in p.AtomIndices)
                    {
                        if (_molecule != null && _molecule.atom != null
                            && ai >= 0 && ai < _molecule.NumberOfAtoms)
                        {
                            string n = _molecule.atom[ai].name;
                            names.Add(string.IsNullOrEmpty(n) ? $"#{ai}" : n);
                        }
                        else
                        {
                            names.Add($"#{ai}");
                        }
                    }
                    // Keep table readable — show first few + count
                    if (names.Count <= 8)
                        atoms = string.Join(", ", names);
                    else
                        atoms = string.Join(", ", names.Take(6))
                              + $" … (+{names.Count - 6})";
                }
                else if (p.Type != CrystalPlaneType.HKL)
                {
                    atoms = p.GetDescription(); // e.g. "molecule: 12 atoms"
                }

                // Color as #RRGGBB and friendly name if known
                var col = p.Color;
                string colorStr = $"#{col.R:X2}{col.G:X2}{col.B:X2}";
                if (!string.IsNullOrEmpty(col.Name) && !col.Name.StartsWith("ff", StringComparison.OrdinalIgnoreCase)
                    && col.IsNamedColor)
                    colorStr = $"{col.Name} ({colorStr})";

                s.Add(
                    idx.ToString(),
                    p.Name ?? "Plane",
                    p.Type.ToString(),
                    hkl,
                    atoms,
                    colorStr,
                    p.A.ToString("F4"),
                    p.B.ToString("F4"),
                    p.C.ToString("F4"),
                    p.D.ToString("F4")
                );
                idx++;
            }

            _sections["Planes"] = s;
        }

        #endregion

        #region Angles — All from bonds

 /// <summary>
 /// Populates the "Angles" section with all possible A-B-C angles
 /// derived from the covalent bond graph. Includes an Error column.
 /// </summary>
        private void PopulateAngles()
        {
            var s = new Section("#", "Atom 1", "Atom 2", "Atom 3", "Angle (°)", "Error");

            if (_molecule?.atom == null)
            {
                _sections["Angles"] = s;
                return;
            }

            var seen = new HashSet<(int, int, int)>();
            int idx = 1;

            for (int b = 0; b < _molecule.NumberOfAtoms; b++)
            {
                var atomB = _molecule.atom[b];
                if (atomB.NrBondedAtoms < 2) continue;

                for (int k1 = 0; k1 < atomB.NrBondedAtoms; k1++)
                {
                    int a = atomB.BondedAtom[k1];
                    if (a < 0 || a >= _molecule.NumberOfAtoms) continue;

                    for (int k2 = k1 + 1; k2 < atomB.NrBondedAtoms; k2++)
                    {
                        int c = atomB.BondedAtom[k2];
                        if (c < 0 || c >= _molecule.NumberOfAtoms) continue;
                        if (a == c) continue;

                        int lo = Math.Min(a, c);
                        int hi = Math.Max(a, c);

                        if (!seen.Add((lo, b, hi))) continue;

                        var atomA = _molecule.atom[a];
                        var atomC = _molecule.atom[c];

                        Vector3 v1 = atomA.Coordinate - atomB.Coordinate;
                        Vector3 v2 = atomC.Coordinate - atomB.Coordinate;

                        float l1 = v1.Length();
                        float l2 = v2.Length();

                        if (l1 < 1e-4f || l2 < 1e-4f) continue;

                        float cos = Math.Clamp(Vector3.Dot(v1, v2) / (l1 * l2), -1f, 1f);
                        float angleDeg = MathHelperTal.ToDegrees((float)Math.Acos(cos));

                        float err = EstimateAngleError(atomA, atomB, atomC);

                        s.Add(
                            idx.ToString(),
                            atomA.name ?? $"atom{a}",
                            atomB.name ?? $"atom{b}",
                            atomC.name ?? $"atom{c}",
                            angleDeg.ToString("F2"),
                            err > 0 ? err.ToString("F2") : "—"
                        );
                        idx++;

                        if (idx > 2000)
                        {
                            s.Add("…", "—", "—", "—", "—", "truncated at 2000 — export for full list");
                            goto doneAngles;
                        }
                    }
                }
            }

        doneAngles:
            _sections["Angles"] = s;
            System.Diagnostics.Debug.WriteLine($"[StructureInfo] Angles: {s.Rows.Count} rows");
        }

        #endregion

        #region Torsions — All from bonds

 /// <summary>
 /// Populates the "Torsions" section with all possible A-B-C-D torsions
 /// derived from the covalent bond graph. Includes an Error column.
 /// </summary>
        private void PopulateTorsions()
        {
            var s = new Section("#", "Atom 1", "Atom 2", "Atom 3", "Atom 4", "Torsion (°)", "Error");

            if (_molecule?.atom == null)
            {
                _sections["Torsions"] = s;
                return;
            }

            var seen = new HashSet<(int, int, int, int)>();
            int idx = 1;

            for (int b = 0; b < _molecule.NumberOfAtoms; b++)
            {
                var atomB = _molecule.atom[b];
                if (atomB.NrBondedAtoms < 1) continue;

                for (int kbc = 0; kbc < atomB.NrBondedAtoms; kbc++)
                {
                    int c = atomB.BondedAtom[kbc];
                    if (c < 0 || c >= _molecule.NumberOfAtoms) continue;

                    var atomC = _molecule.atom[c];
                    if (atomC.NrBondedAtoms < 1) continue;

                    for (int ka = 0; ka < atomB.NrBondedAtoms; ka++)
                    {
                        int a = atomB.BondedAtom[ka];
                        if (a < 0 || a >= _molecule.NumberOfAtoms) continue;
                        if (a == c) continue;

                        for (int kd = 0; kd < atomC.NrBondedAtoms; kd++)
                        {
                            int d = atomC.BondedAtom[kd];
                            if (d < 0 || d >= _molecule.NumberOfAtoms) continue;
                            if (d == b) continue;
                            if (d == a) continue;

                            int first = Math.Min(a, d);
                            int last = Math.Max(a, d);

                            int a1, a2, a3, a4;
                            if (b <= c)
                            {
                                a1 = first; a2 = b; a3 = c; a4 = last;
                            }
                            else
                            {
                                a1 = last; a2 = c; a3 = b; a4 = first;
                            }

                            if (!seen.Add((a1, a2, a3, a4))) continue;

                            var atomA = _molecule.atom[a1];
                            var atomB2 = _molecule.atom[a2];
                            var atomC2 = _molecule.atom[a3];
                            var atomD = _molecule.atom[a4];

                            double torsionRad = CalculateTorsionAngle(
                                atomA.Coordinate,
                                atomB2.Coordinate,
                                atomC2.Coordinate,
                                atomD.Coordinate
                            );
                            double torsionDeg = MathHelperTal.ToDegrees((float)torsionRad);

                            float err = EstimateTorsionError(atomA, atomB2, atomC2, atomD);

                            s.Add(
                                idx.ToString(),
                                atomA.name ?? $"atom{a1}",
                                atomB2.name ?? $"atom{a2}",
                                atomC2.name ?? $"atom{a3}",
                                atomD.name ?? $"atom{a4}",
                                torsionDeg.ToString("F2"),
                                err > 0 ? err.ToString("F2") : "—"
                            );
                            idx++;

                            if (idx > 2000)
                            {
                                s.Add("…", "—", "—", "—", "—", "—", "truncated at 2000");
                                goto doneTorsions;
                            }
                        }
                    }
                }
            }

        doneTorsions:
            _sections["Torsions"] = s;
            System.Diagnostics.Debug.WriteLine($"[StructureInfo] Torsions: {s.Rows.Count} rows");
        }

        #endregion

        #region Geometry Helpers

 /// <summary>
 /// Calculates the dihedral (torsion) angle A-B-C-D in radians.
 /// </summary>
        private double CalculateTorsionAngle(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            Vector3 a = v1 - v2;
            Vector3 b = v3 - v2;
            Vector3 c = v4 - v3;
            Vector3 d = v2 - v3;

            Vector3 n1 = Vector3.Cross(a, b);
            Vector3 n2 = Vector3.Cross(c, d);

            float n1Len = n1.Length();
            float n2Len = n2.Length();
            if (n1Len < 1e-6f || n2Len < 1e-6f) return 0;

            n1 /= n1Len;
            n2 /= n2Len;

            double dot = Math.Clamp(Vector3.Dot(n1, n2), -1.0, 1.0);
            double angle = Math.Acos(dot);

            Vector3 reference = Vector3.Normalize(b);
            Vector3 cross = Vector3.Cross(n1, n2);

            if (Vector3.Dot(cross, reference) < 0)
                angle = -angle;

            return angle;
        }

 /// <summary>
 /// Estimates the bond length error from per-atom aftersitional errors.
 /// Uses the same formula as Measure.CalculateDistanceError, but the
 /// cell-error contribution is already embedded in Coordinate_error.
 /// </summary>
        private float EstimateBondError(Molecule.Atom a1, Molecule.Atom a2)
        {
            Vector3 diff = a1.Coordinate - a2.Coordinate;
            float d = diff.Length();

            if (d < 1e-4f) return 0.001f;

            Vector3 e1 = a1.Coordinate_error;
            Vector3 e2 = a2.Coordinate_error;

            float dx = diff.X / d;
            float dy = diff.Y / d;
            float dz = diff.Z / d;

            float errorSq =
                dx * dx * e1.X * e1.X + dy * dy * e1.Y * e1.Y + dz * dz * e1.Z * e1.Z +
                dx * dx * e2.X * e2.X + dy * dy * e2.Y * e2.Y + dz * dz * e2.Z * e2.Z;

            return (float)Math.Sqrt(Math.Max(errorSq, 1e-12));
        }

 /// <summary>
 /// Estimates the angle error (aftersitional + cell contribution).
 /// Consistent with Measure.CalculateAngleError.
 /// </summary>
        private float EstimateAngleError(Molecule.Atom a, Molecule.Atom b, Molecule.Atom c)
        {
            Vector3 v1 = a.Coordinate - b.Coordinate;
            Vector3 v2 = c.Coordinate - b.Coordinate;

            float l1 = v1.Length();
            float l2 = v2.Length();
            if (l1 < 1e-4f || l2 < 1e-4f) return 0.5f;

            float e1 = a.Coordinate_error.Length();
            float e2 = b.Coordinate_error.Length();
            float e3 = c.Coordinate_error.Length();

            float avgPosErr = (e1 + e2 + e3) / 3f;
            float minLen = Math.Min(l1, l2);
            if (minLen < 1e-4f) return 0.5f;

            float errRad = (float)Math.Sqrt(2.0) * avgPosErr / minLen;
            float errDeg = MathHelperTal.ToDegrees(errRad);

            return Math.Clamp(errDeg, 0.001f, 10.0f);
        }

 /// <summary>
 /// Estimates the torsion error (aftersitional + cell contribution).
 /// Consistent with Measure.CalculateTorsionError.
 /// </summary>
        private float EstimateTorsionError(Molecule.Atom a, Molecule.Atom b, Molecule.Atom c, Molecule.Atom d)
        {
            float lBC = Vector3.Distance(b.Coordinate, c.Coordinate);
            if (lBC < 1e-4f) return 1.0f;

            float e1 = a.Coordinate_error.Length();
            float e2 = b.Coordinate_error.Length();
            float e3 = c.Coordinate_error.Length();
            float e4 = d.Coordinate_error.Length();

            float avgPosErr = (e1 + e2 + e3 + e4) / 4f;

            float errDeg = (float)Math.Sqrt(2.0) * avgPosErr / lBC * 57.2958f;

            return Math.Clamp(errDeg, 0.001f, 30.0f);
        }

        #endregion

        #region Symmetry Op Formatting

        private string VarName(int idx) => idx switch { 0 => "x", 1 => "y", 2 => "z", _ => "?" };

        private string FormatSymmetryOpString(SymmetryElements.ReadSymm op)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(BuildAxis(op, i));
            }
            return sb.ToString();
        }

        private string FormatSymmetryOpFull(int opIndex, int tx, int ty, int tz)
        {
            if (_molecule?.SymmetryOperations == null) return "x,y,z";
            if (opIndex < 0 || opIndex >= _molecule.SymmetryOperations.Count)
                return "x,y,z";

            var op = _molecule.SymmetryOperations[opIndex];
            var sb = new StringBuilder();
            int[] shifts = { tx, ty, tz };

            for (int i = 0; i < 3; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(BuildAxisWithShift(op, i, shifts[i]));
            }
            return sb.ToString();
        }

        private string BuildAxis(SymmetryElements.ReadSymm op, int axis)
        {
            var sb = new StringBuilder();
            bool anyVar = false;

            if (op.pos1[axis] >= 0 && op.sign1[axis] != 0)
                AppendTerm(sb, op.sign1[axis], VarName(op.pos1[axis]), ref anyVar);

            if (op.pos2[axis] >= 0 && op.sign2[axis] != 0)
                AppendTerm(sb, op.sign2[axis], VarName(op.pos2[axis]), ref anyVar);

            float sh = op.shift[axis];
            if (Math.Abs(sh) > 1e-6f)
            {
                string shiftStr = FormatFractionAbs(Math.Abs(sh));
                if (anyVar)
                    sb.Append(sh >= 0 ? "+" : "-").Append(shiftStr);
                else
                {
                    if (sh < 0) sb.Append("-");
                    sb.Append(shiftStr);
                }
            }

            if (sb.Length == 0) sb.Append("0");
            return sb.ToString();
        }

        private string BuildAxisWithShift(SymmetryElements.ReadSymm op, int axis, int cellShift)
        {
            float totalShift = op.shift[axis] + cellShift;

            bool hasFirstVar = (op.pos1[axis] >= 0 && op.sign1[axis] != 0);
            bool hasSecondVar = (op.pos2[axis] >= 0 && op.sign2[axis] != 0);

            var varPart = new StringBuilder();

            if (hasFirstVar)
            {
                string v = VarName(op.pos1[axis]);
                varPart.Append(op.sign1[axis] >= 0 ? "+" : "-").Append(v);
            }

            if (hasSecondVar)
            {
                string v = VarName(op.pos2[axis]);
                varPart.Append(op.sign2[axis] >= 0 ? "+" : "-").Append(v);
            }

            if (Math.Abs(totalShift) > 1e-6f)
            {
                string shiftStr = FormatFractionAbs(Math.Abs(totalShift));

                if (varPart.Length == 0)
                {
                    return totalShift >= 0 ? shiftStr : $"-{shiftStr}";
                }

                if (totalShift >= 0)
                {
                    return $"{shiftStr}{varPart}";
                }
                else
                {
                    return $"-{shiftStr}{varPart}";
                }
            }

            if (varPart.Length == 0) return "0";

            string result = varPart.ToString();
            if (result.StartsWith("+"))
                result = result.Substring(1);
            return result;
        }

        private void AppendTerm(StringBuilder sb, int sign, string var, ref bool anyVar)
        {
            if (anyVar)
                sb.Append(sign >= 0 ? "+" : "-");
            else if (sign < 0)
                sb.Append("-");

            sb.Append(var);
            anyVar = true;
        }

        private string FormatFractionAbs(float absV)
        {
            if (absV < 1e-6f) return "0";

            float half = absV * 2f;
            if (Math.Abs(half - Math.Round(half)) < 1e-4f)
            {
                int num = (int)Math.Round(half);
                int den = 2;
                int g = GCD(num, den);
                num /= g;
                den /= g;
                if (den == 1) return num.ToString();
                return $"{num}/{den}";
            }

            float quarter = absV * 4f;
            if (Math.Abs(quarter - Math.Round(quarter)) < 1e-4f)
            {
                int num = (int)Math.Round(quarter);
                int den = 4;
                int g = GCD(num, den);
                num /= g;
                den /= g;
                if (den == 1) return num.ToString();
                return $"{num}/{den}";
            }

            float third = absV * 3f;
            if (Math.Abs(third - Math.Round(third)) < 1e-3f)
            {
                int num = (int)Math.Round(third);
                int den = 3;
                int g = GCD(num, den);
                num /= g;
                den /= g;
                if (den == 1) return num.ToString();
                return $"{num}/{den}";
            }

            float sixth = absV * 6f;
            if (Math.Abs(sixth - Math.Round(sixth)) < 1e-3f)
            {
                int num = (int)Math.Round(sixth);
                int den = 6;
                int g = GCD(num, den);
                num /= g;
                den /= g;
                if (den == 1) return num.ToString();
                return $"{num}/{den}";
            }

            float eighth = absV * 8f;
            if (Math.Abs(eighth - Math.Round(eighth)) < 1e-3f)
            {
                int num = (int)Math.Round(eighth);
                int den = 8;
                int g = GCD(num, den);
                num /= g;
                den /= g;
                if (den == 1) return num.ToString();
                return $"{num}/{den}";
            }

            if (Math.Abs(absV - Math.Round(absV)) < 1e-4f)
                return ((int)Math.Round(absV)).ToString();

            return absV.ToString("F3").TrimEnd('0').TrimEnd('.');
        }

        private static int GCD(int a, int b)
        {
            a = Math.Abs(a);
            b = Math.Abs(b);
            while (b != 0)
            {
                int t = b;
                b = a % b;
                a = t;
            }
            return a == 0 ? 1 : a;
        }

        private (string description, string detailed, int order, string type) ClassifySymmetryOp(
            SymmetryElements.ReadSymm op)
        {
            int flips = 0;
            for (int i = 0; i < 3; i++)
            {
                if (op.pos1[i] == i && op.sign1[i] < 0) flips++;
                else if (op.pos2[i] == i && op.sign2[i] < 0) flips++;
            }

            bool isIdentity = flips == 0 &&
                              Math.Abs(op.shift[0]) < 1e-6f &&
                              Math.Abs(op.shift[1]) < 1e-6f &&
                              Math.Abs(op.shift[2]) < 1e-6f;

            if (isIdentity)
                return ("Identity", "Identity", 1, "1");

            if (flips == 3)
            {
                bool isInv = true;
                for (int i = 0; i < 3; i++)
                {
                    bool negSelf = (op.pos1[i] == i && op.sign1[i] < 0);
                    if (!negSelf) isInv = false;
                }
                if (isInv && Math.Abs(op.shift[0]) < 1e-6f &&
                    Math.Abs(op.shift[1]) < 1e-6f && Math.Abs(op.shift[2]) < 1e-6f)
                {
                    return ("Inversion centre", "Inversion at [0, 0, 0]", 2, "-1");
                }
            }

            if (flips == 1)
                return ("2-fold rotation", "2-fold axis", 2, "2");

            return ("Symmetry operation", FormatSymmetryOpString(op), 1, "?");
        }

        #endregion

        #region Helpers

        private string GetAtomName(int atomIdx)
        {
            if (_molecule == null || atomIdx < 0 || atomIdx >= _molecule.NumberOfAtoms)
                return "?";
            return _molecule.atom[atomIdx].name ?? $"atom{atomIdx}";
        }

        private string FormatWithError(double value, double error)
        {
            if (error <= 0 || double.IsNaN(error))
                return value.ToString("F4");

            int decimals = 0;
            double temp = error;
            while (temp < 1.0 && decimals < 8) { temp *= 10.0; decimals++; }

            double roundedValue = Math.Round(value, decimals);
            double roundedError = Math.Round(error, decimals);
            int errDigits = (int)Math.Round(roundedError * Math.Pow(10, decimals));
            return $"{roundedValue.ToString("F" + decimals)}({errDigits})";
        }

        private string FormatAngleWithError(float angleRad, float errorRad)
        {
            float angleDeg = MathHelperTal.ToDegrees(angleRad);
            float errorDeg = MathHelperTal.ToDegrees(errorRad);
            return FormatWithError(angleDeg, errorDeg);
        }

        private string GetFormula(Molecule molecule)
        {
            if (molecule == null) return "";
            var counts = new Dictionary<int, int>();
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                int z = molecule.atom[i].AtomicNumber;
                if (!counts.ContainsKey(z)) counts[z] = 0;
                counts[z]++;
            }
            string result = "";
            if (counts.TryGetValue(6, out int c)) { result += $"C{c}"; counts.Remove(6); }
            if (counts.TryGetValue(1, out int h)) { result += $" H{h}"; counts.Remove(1); }
            foreach (var kv in counts.OrderBy(x => x.Key))
                result += $" {GetElementSymbol(kv.Key)}{kv.Value}";
            return result.Trim();
        }

        private string GetElementSymbol(int atomicNumber)
        {
            switch (atomicNumber)
            {
                case 1: return "H";
                case 6: return "C";
                case 7: return "N";
                case 8: return "O";
                case 9: return "F";
                case 15: return "P";
                case 16: return "S";
                case 17: return "Cl";
                case 35: return "Br";
                case 53: return "I";
                default: return $"X{atomicNumber}";
            }
        }

        private string FormatSpaceGroup(Molecule molecule)
        {
            if (molecule == null) return "";
            string name = molecule.SpaceGroupName ?? "";
            int number = molecule.SpaceGroupNumber;
            return number > 0 ? $"{name} ({number})" : name;
        }

        private string FormatVolume(Molecule molecule)
        {
            if (molecule == null) return "";
            double volumeError = ComputeVolumeError(
                molecule.a, molecule.b, molecule.c,
                molecule.alpha, molecule.beta, molecule.gamma,
                molecule.a_e, molecule.b_e, molecule.c_e,
                molecule.alpha_e, molecule.beta_e, molecule.gamma_e);
            if (volumeError <= 0) return $"{molecule.Volume:F3}";
            return FormatWithError(molecule.Volume, volumeError);
        }

        private double ComputeVolumeError(
            float a, float b, float c,
            float alpha, float beta, float gamma,
            float a_e, float b_e, float c_e,
            float alpha_e, float beta_e, float gamma_e)
        {
            if (a <= 0 || b <= 0 || c <= 0) return 0;
            double ca = Math.Cos(alpha), cb = Math.Cos(beta), cg = Math.Cos(gamma);
            double sa = Math.Sin(alpha), sb = Math.Sin(beta), sg = Math.Sin(gamma);

            double L = 1.0 - ca * ca - cb * cb - cg * cg + 2.0 * ca * cb * cg;
            if (L <= 0) return 0;
            double K = Math.Sqrt(L);

            double dV_da = b * c * K;
            double dV_db = a * c * K;
            double dV_dc = a * b * K;

            double dL_dAlpha = 2.0 * sa * (ca - cb * cg);
            double dL_dBeta = 2.0 * sb * (cb - ca * cg);
            double dL_dGamma = 2.0 * sg * (cg - ca * cb);

            double dV_dAlpha = a * b * c * dL_dAlpha / (2.0 * K);
            double dV_dBeta = a * b * c * dL_dBeta / (2.0 * K);
            double dV_dGamma = a * b * c * dL_dGamma / (2.0 * K);

            double variance =
                dV_da * dV_da * a_e * a_e +
                dV_db * dV_db * b_e * b_e +
                dV_dc * dV_dc * c_e * c_e +
                dV_dAlpha * dV_dAlpha * alpha_e * alpha_e +
                dV_dBeta * dV_dBeta * beta_e * beta_e +
                dV_dGamma * dV_dGamma * gamma_e * gamma_e;

            return Math.Sqrt(variance);
        }

        #endregion
    }
}