// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal morphology / Wulff module: MorphologyWindow.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// Morphology window: description table; BFDH (without F/S/K) as the physical method

using System;
using System.Drawing;
using System.Windows.Forms;
using Tal.Calculate;
using Tal.Morphology;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public class MorphologyWindow : DockContent
    {
        #region Fields

        private ListView _listFaces;
        private Label _lblHabitus;
        private Label _lblCell;
        private Label _lblMethodNote;
        private NumericUpDown _numMaxIndex;
        private NumericUpDown _numMinD;
        private NumericUpDown _numSoftening;
        private ComboBox _cmbSofteningMode;
        private Button _btnCalculate;
        private Button _btnClose;
        private Button _btnWulffPlot;

        private CrystalMorphologyResult _lastResult;
        private MorphologyDocument _doc3D;
        private WulffPlotDocument _wulffDoc;

        private string _lastParamsKey;
        private Molecule _lastMolecule;
        private string _lastMoleculeFingerprint;

        private bool _isAutoUpdatingSoftening = false;

        #endregion

        #region Constructor

        public MorphologyWindow()
        {
            Text = "Crystal Morphology (BFDH)";
            DockAreas = DockAreas.DockLeft | DockAreas.DockRight | DockAreas.Float;
            BackColor = System.Drawing.Color.FromArgb(248, 248, 250);
            ClientSize = new System.Drawing.Size(520, 620);
            MinimumSize = new System.Drawing.Size(480, 480);
            ShowHint = DockState.DockRight;

            BuildUI();
        }

        #endregion

        #region UI Builder

        private void BuildUI()
        {
 // ═══════════════════════════════════════════════════════════
 // PANEL GÓRNY — parametery + przyciski
 // ═══════════════════════════════════════════════════════════
            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 200,
                Padding = new Padding(10),
                BackColor = System.Drawing.Color.FromArgb(248, 248, 250)
            };

            var lbl1 = new Label
            {
                Text = "Max Miller index:",
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true
            };
            _numMaxIndex = new NumericUpDown
            {
                Location = new System.Drawing.Point(200, 8),
                Width = 80,
                Minimum = 1,
                Maximum = 6,
                Value = 3
            };

            var lbl2 = new Label
            {
                Text = "Min d-spacing (Å):",
                Location = new System.Drawing.Point(10, 40),
                AutoSize = true
            };
            _numMinD = new NumericUpDown
            {
                Location = new System.Drawing.Point(200, 38),
                Width = 80,
                Minimum = 0.1M,
                Maximum = 10M,
                DecimalPlaces = 2,
                Increment = 0.1M,
                Value = 0.5M
            };

            var lbl3 = new Label
            {
                Text = "Softening strength:",
                Location = new System.Drawing.Point(10, 70),
                AutoSize = true
            };
            _numSoftening = new NumericUpDown
            {
                Location = new System.Drawing.Point(200, 68),
                Width = 80,
                Minimum = 0.05M,
                Maximum = 20.0M,
                DecimalPlaces = 2,
                Increment = 0.1M,
                Value = 1.00M
            };

            var lbl4 = new Label
            {
                Text = "Softening mode:",
                Location = new System.Drawing.Point(10, 100),
                AutoSize = true
            };
            _cmbSofteningMode = new ComboBox
            {
                Location = new System.Drawing.Point(200, 98),
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbSofteningMode.Items.AddRange(new object[]
            {
                "None (pure BFDH)",
                "Clamp (visual only)",
                "Sqrt (visual only)",
                "Tanh (visual only)",
                "Linear (visual only)"
            });
            _cmbSofteningMode.SelectedIndex = 0;   // default None
            _cmbSofteningMode.SelectedIndexChanged += CmbSofteningMode_SelectedIndexChanged;

 // Note on methods
            _lblMethodNote = new Label
            {
                Text = "Only 'None' is pure BFDH. Other modes are visual adjustments without physical basis.",
                Location = new System.Drawing.Point(10, 126),
                Width = 480,
                Height = 20,
                Font = new System.Drawing.Font("Segoe UI", 7.5F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.FromArgb(140, 80, 20)
            };

            _btnCalculate = new Button
            {
                Text = "Calculate",
                Location = new System.Drawing.Point(10, 155),
                Width = 140,
                Height = 32
            };
            _btnCalculate.Click += BtnCalculate_Click;

            _btnWulffPlot = new Button
            {
                Text = "Wulff Plot",
                Location = new System.Drawing.Point(160, 155),
                Width = 140,
                Height = 32
            };
            _btnWulffPlot.Click += BtnWulffPlot_Click;

            _btnClose = new Button
            {
                Text = "Close",
                Location = new System.Drawing.Point(310, 155),
                Width = 100,
                Height = 32
            };
            _btnClose.Click += (s, e) => Close();

            panelTop.Controls.AddRange(new Control[]
            {
                lbl1, _numMaxIndex,
                lbl2, _numMinD,
                lbl3, _numSoftening,
                lbl4, _cmbSofteningMode,
                _lblMethodNote,
                _btnCalculate, _btnWulffPlot, _btnClose
            });

 // ═══════════════════════════════════════════════════════════
 // PANEL INFO — habitus + cell
 // ═══════════════════════════════════════════════════════════
            var panelInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                Padding = new Padding(10),
                BackColor = System.Drawing.Color.FromArgb(248, 248, 250)
            };

            _lblHabitus = new Label
            {
                Text = "Habitus: —",
                Location = new System.Drawing.Point(10, 10),
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold)
            };

            _lblCell = new Label
            {
                Text = "Cell: —",
                Location = new System.Drawing.Point(10, 35),
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 8.5F)
            };

            panelInfo.Controls.AddRange(new Control[] { _lblHabitus, _lblCell });

 // ═══════════════════════════════════════════════════════════
 // LISTVIEW — tabela ścian
 // ═══════════════════════════════════════════════════════════
            _listFaces = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                Font = new System.Drawing.Font("Consolas", 9F),
                OwnerDraw = true
            };

 // ⭐ Kolumny z dokładniejszymi opisami
            _listFaces.Columns.Add("Face", 100);
            _listFaces.Columns.Add("d (Å)", 80);
            _listFaces.Columns.Add("R (rel.)", 80);
            _listFaces.Columns.Add("DistFrac", 80);
            _listFaces.Columns.Add("Mult", 60);
            _listFaces.Columns.Add("Description", 260);

            _listFaces.DrawColumnHeader += ListFaces_DrawColumnHeader;
            _listFaces.DrawItem += ListFaces_DrawItem;
            _listFaces.DrawSubItem += ListFaces_DrawSubItem;

            _listFaces.SelectedIndexChanged += ListFaces_SelectedIndexChanged;

            Controls.Add(_listFaces);
            Controls.Add(panelInfo);
            Controls.Add(panelTop);
        }

        #endregion

        #region OwnerDraw ListView

        private void ListFaces_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void ListFaces_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
 // Drawemy w DrawSubItem — nic tu nie robimy
        }

        private void ListFaces_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
 // ⭐ Tło wiersza
            System.Drawing.Color rowBg = e.Item.Selected
                ? System.Drawing.Color.FromArgb(0, 120, 215)
                : (e.ItemIndex % 2 == 0
                    ? System.Drawing.Color.White
                    : System.Drawing.Color.FromArgb(245, 245, 250));

            using (var brush = new SolidBrush(rowBg))
                e.Graphics.FillRectangle(brush, e.Bounds);

 // ⭐ Kolumna 0 — colourowy pasek + tekst
            if (e.ColumnIndex == 0)
            {
                string faceName = e.SubItem.Text;

                var xnaColor = MorphologyCrystal.NameToColor(faceName);
                var gdiColor = System.Drawing.Color.FromArgb(
                    xnaColor.R, xnaColor.G, xnaColor.B);

                var colorBarRect = new Rectangle(
                    e.Bounds.Left + 2,
                    e.Bounds.Top + 3,
                    12,
                    e.Bounds.Height - 6);

                using (var brush = new SolidBrush(gdiColor))
                    e.Graphics.FillRectangle(brush, colorBarRect);

                using (var pen = new Pen(System.Drawing.Color.FromArgb(80, 80, 80), 1))
                    e.Graphics.DrawRectangle(pen, colorBarRect);

                var textRect = new Rectangle(
                    e.Bounds.Left + 18,
                    e.Bounds.Top,
                    e.Bounds.Width - 18,
                    e.Bounds.Height);

                System.Drawing.Color textColor = e.Item.Selected
                    ? System.Drawing.Color.White
                    : System.Drawing.Color.Black;

                TextRenderer.DrawText(
                    e.Graphics,
                    faceName,
                    e.SubItem.Font,
                    textRect,
                    textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
            else
            {
 // Inne kolumny — tekst
                System.Drawing.Color textColor = e.Item.Selected
                    ? System.Drawing.Color.White
                    : System.Drawing.Color.Black;

                TextRenderer.DrawText(
                    e.Graphics,
                    e.SubItem.Text,
                    e.SubItem.Font,
                    e.Bounds,
                    textColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }

        private void ListFaces_SelectedIndexChanged(object sender, EventArgs e)
        {
 // ⭐ Afterdświetl wybraną ścianę w 3D
            if (_doc3D == null || _doc3D.IsDisposed) return;

            if (_listFaces.SelectedItems.Count == 0)
            {
                _doc3D.HighlightFace(null);
                return;
            }

            string faceName = _listFaces.SelectedItems[0].Text;
            _doc3D.HighlightFace(faceName);

            System.Diagnostics.Debug.WriteLine(
                $"[MorphWindow] Selected face: {faceName} → highlight in 3D");
        }

        #endregion

        #region Auto Softening

        private static float GetSuggestedSoftening(SofteningMode mode)
        {
            switch (mode)
            {
                case SofteningMode.None: return 1.00f;
                case SofteningMode.Clamp: return 2.50f;
                case SofteningMode.Sqrt: return 1.00f;
                case SofteningMode.Tanh: return 0.50f;
                case SofteningMode.Linear: return 5.00f;
                default: return 1.00f;
            }
        }

        private void CmbSofteningMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isAutoUpdatingSoftening) return;

            var mode = (SofteningMode)_cmbSofteningMode.SelectedIndex;
            float suggested = GetSuggestedSoftening(mode);

            _isAutoUpdatingSoftening = true;
            try
            {
                _numSoftening.Value = (decimal)Math.Clamp(suggested, 0.05, 20.0);
            }
            finally
            {
                _isAutoUpdatingSoftening = false;
            }

            _lastParamsKey = null;
        }

        #endregion

        #region Fingerprint Helpers

        private static string MoleculeFingerprint(Molecule mol)
        {
            if (mol == null) return "null";
            return $"{mol.Name}|" +
                   $"{mol.a:F5}|{mol.b:F5}|{mol.c:F5}|" +
                   $"{mol.alpha:F5}|{mol.beta:F5}|{mol.gamma:F5}|" +
                   $"{mol.SpaceGroupNumber}|" +
                   $"atoms={mol.NumberOfAtoms}";
        }

        private static string BfdhFingerprint(int maxIdx, float minD, float softening, SofteningMode mode)
        {
            return $"maxIdx={maxIdx}|minD={minD:F4}|soft={softening:F4}|mode={(int)mode}";
        }

        #endregion

        #region Calculate

        private void BtnCalculate_Click(object sender, EventArgs e)
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;

            Molecule molecule = null;
            string source = "";

            var active = mainForm?.GetActiveMolecule();
            if (active != null)
            {
                molecule = active;
                source = "Active";
            }
            else if (_lastMolecule != null)
            {
                molecule = _lastMolecule;
                source = "Cached";
            }

            if (molecule == null)
            {
                mainForm?.SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            string newMolFp = MoleculeFingerprint(molecule);
            bool sameMolecule = (newMolFp == _lastMoleculeFingerprint);

            int maxIdx = (int)_numMaxIndex.Value;
            float minD = (float)_numMinD.Value;
            float softening = (float)_numSoftening.Value;
            var mode = (SofteningMode)_cmbSofteningMode.SelectedIndex;

            string newBfdhFp = BfdhFingerprint(maxIdx, minD, softening, mode);
            bool sameBfdh = (newBfdhFp == _lastParamsKey);

            bool useCache = sameMolecule && sameBfdh && (_lastResult != null);

            if (!useCache)
            {
                _lastResult = CrystalMorphologyFinder.Calculate(
                    molecule, maxIdx, minD, softening, mode);

                _lastMoleculeFingerprint = newMolFp;
                _lastParamsKey = newBfdhFp;
            }

            _lastMolecule = molecule;
            Text = $"Crystal Morphology (BFDH) — {molecule.Name}";

            PopulateList(_lastResult);
            PopulateInfo(_lastResult, molecule);

            OpenOrUpdateMorphologyViewer(molecule.Name);

            string methodLabel = mode == SofteningMode.None
                ? "BFDH"
                : $"BFDH + {mode} (visual)";

            mainForm?.SetStatus(
                $"Morphology: {_lastResult.Faces.Count} faces, " +
                $"habitus: {_lastResult.Habitus}, method: {methodLabel}" +
                (useCache ? " (cache)" : ""),
                $"Structures: {StructureStorage.Instance.StructureCount}",
                "💎");
        }

        #endregion

        #region Wulff Plot

        private void BtnWulffPlot_Click(object sender, EventArgs e)
        {
            if (_lastResult == null)
            {
                MessageBox.Show(
                    "Please click Calculate first to generate the morphology.",
                    "Wulff Plot",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel == null) return;

            string structureName = _lastMolecule?.Name ?? "structure";

            MorphologyCrystal crystal = _doc3D?.GetCrystal();

            if (_wulffDoc != null && !_wulffDoc.IsDisposed)
            {
                _wulffDoc.SetData(_lastResult, crystal, _doc3D, structureName);
                _wulffDoc.Activate();
                return;
            }

            _wulffDoc = new WulffPlotDocument($"Wulff Plot: {structureName}");
            _wulffDoc.Show(dockPanel, DockState.Document);
            _wulffDoc.SetData(_lastResult, crystal, _doc3D, structureName);
            _wulffDoc.Activate();

            mainForm.SetStatus(
                $"Wulff Plot opened — {_lastResult.Faces.Count} faces",
                $"Structures: {StructureStorage.Instance.StructureCount}",
                "🎯");
        }

        #endregion

        #region Populate

        private void PopulateList(CrystalMorphologyResult result)
        {
            _listFaces.BeginUpdate();
            _listFaces.Items.Clear();

            if (result == null || result.Faces == null)
            {
                _listFaces.EndUpdate();
                return;
            }

            foreach (var face in result.Faces)
            {
                var item = new ListViewItem(face.Name);
                item.SubItems.Add(face.DSpacing.ToString("F3"));
                item.SubItems.Add(face.RelativeGrowthRate.ToString("F3"));
                item.SubItems.Add(face.DistFrac.ToString("F3"));
                item.SubItems.Add(face.Multiplicity.ToString());
                item.SubItems.Add(DescribeFace(face));

                _listFaces.Items.Add(item);
            }

            _listFaces.EndUpdate();
        }

        private void PopulateInfo(CrystalMorphologyResult result, Molecule molecule)
        {
            if (result == null)
            {
                _lblHabitus.Text = "Habitus: —";
                _lblCell.Text = "Cell: —";
                return;
            }

 // ⭐ Habitus — krótko after angielsku z krótkim dopiskiem
            string habitusNote = GetHabitusNote(result.Habitus);
            _lblHabitus.Text = $"Habitus: {result.Habitus}{habitusNote}";

            _lblCell.Text = $"Cell: a={result.CellA:F3} b={result.CellB:F3} c={result.CellC:F3} Å  " +
                            $"α={result.CellAlpha:F1}° β={result.CellBeta:F1}° γ={result.CellGamma:F1}°  " +
                            $"V={result.CellVolume:F2} Å³";
        }

        #endregion

        #region Descriptions

 /// <summary>
 /// ⭐ Krótki opis faces w tabeli:
 /// - znaczenie krystalograficzne
 /// - rola morfologsczna
 /// </summary>
        private static string DescribeFace(CrystalFace face)
        {
            if (face == null) return "";

            int h = Math.Abs(face.H);
            int k = Math.Abs(face.K);
            int l = Math.Abs(face.L);

 // ─── Klasyfikacja morfologsczna ────────────────────────────
            string morph = ClassifyMorphRole(face, h, k, l);

 // ─── Klasyfikacja krystalograficzna ────────────────────────
            string cryst = ClassifyCrystalRole(h, k, l);

 // ─── Rozmiar ───────────────────────────────────────────────
            string size = face.DistFrac < 1.2f ? "large"
                        : face.DistFrac < 2.0f ? "medium"
                        : face.DistFrac < 4.0f ? "small"
                        : "tiny";

            return $"{morph} · {cryst} · {size}";
        }

        private static string ClassifyMorphRole(CrystalFace f, int h, int k, int l)
        {
 // Największe faces (DistFrac ≈ 1) — dominujące
            if (f.DistFrac < 1.05f) return "dominant";
            if (f.DistFrac < 1.5f) return "major";
            if (f.DistFrac < 3.0f) return "minor";
            return "trace";
        }

        private static string ClassifyCrystalRole(int h, int k, int l)
        {
 // Zlicz niezerowe
            int nz = (h != 0 ? 1 : 0) + (k != 0 ? 1 : 0) + (l != 0 ? 1 : 0);

            if (nz == 1)
            {
 // Płaszczyzny osiowe
                if (h != 0) return "basal a";
                if (k != 0) return "basal b";
                return "basal c";
            }

            if (nz == 2)
            {
 // Płaszczyzny diagonalne (2 osie)
                if (h == k && l == 0) return "prism ab";
                if (h == l && k == 0) return "prism ac";
                if (k == l && h == 0) return "prism bc";
                return "diagonal";
            }

 // nz == 3
            if (h == k && k == l) return "octahedral";
            if (h == k || h == l || k == l) return "pyramidal";
            return "general";
        }

 /// <summary>
 /// ⭐ Krótki dopisek do habitusu — after angielsku.
 /// </summary>
        private static string GetHabitusNote(string habitus)
        {
            if (string.IsNullOrEmpty(habitus)) return "";

            switch (habitus.ToLowerInvariant())
            {
                case "cubic / octahedral":
                    return "  (isometric)";
                case "plate":
                    return "  (flattened)";
                case "needle / prismatic":
                    return "  (elongated)";
                case "rhombic / dodecahedral":
                    return "  (rhombic)";
                case "equant (isometric)":
                    return "  (equant)";
                case "prismatic":
                    return "  (columnar)";
                case "unknown":
                    return "  (unclassified)";
                default:
                    return "";
            }
        }

        #endregion

        #region Viewer Management

        private void OpenOrUpdateMorphologyViewer(string structureName)
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel == null) return;

            if (_doc3D != null && !_doc3D.IsDisposed)
            {
                _doc3D.Text = $"Crystal: {structureName}";
                _doc3D.SetCrystalFromResult(_lastResult, structureName);
                _doc3D.Activate();
                return;
            }

            _doc3D = new MorphologyDocument($"Crystal: {structureName}");
            _doc3D.Show(dockPanel, DockState.Document);
            _doc3D.SetCrystalFromResult(_lastResult, structureName);
            _doc3D.Activate();
        }

        #endregion
    }
}