// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal planes module: PlanesWindow.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.Core.PowderDiffraction;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class PlanesWindow : DockContent
    {
        #region Private Fields

        private Molecule _molecule;
        private List<CrystalPlane> _planes;
        private readonly Action _onPlanesChanged;

 // Cache obrazków colourów - klucz: ARGB colouru, value: index w ImageList
        private readonly Dictionary<int, int> _colorImageCache = new Dictionary<int, int>();

        #endregion

        #region Constructor

        public PlanesWindow(Molecule molecule, List<CrystalPlane> planes, Action onPlanesChanged = null)
        {
            InitializeComponent();
            _molecule = molecule ?? throw new ArgumentNullException(nameof(molecule));
            _planes = planes ?? new List<CrystalPlane>();
            _onPlanesChanged = onPlanesChanged;

 // ⭐ NOWE - właściwości DockContent
            this.Text = "Planes";
            this.DockAreas = DockAreas.DockLeft | DockAreas.DockRight | DockAreas.Float;
            this.CloseButton = true;
            this.CloseButtonVisible = true;
            this.ShowHint = DockState.DockLeft;

            RefreshList();
        }

        public void UpdateMolecule(Molecule molecule)
        {
            Flags.Instance.VisualRevision++;
            _molecule = molecule;   // allow null — no molecule = no list
            RefreshList();
        }

 /// <summary>
 /// Afterdmienia list planes (when zmienia active dokument).
 /// </summary>
        public void UpdatePlanes(List<CrystalPlane> planes)
        {
            if (planes == null) return;
            if (ReferenceEquals(_planes, planes)) return;
            Flags.Instance.VisualRevision++;
            _planes = planes;
            RefreshList();
        }

        #endregion

        #region List Management

        private void RefreshList()
        {
            listViewPlanes.BeginUpdate();
            listViewPlanes.Items.Clear();

            if (_planes != null)
            {
                foreach (var plane in _planes)
                {
                    var item = new ListViewItem(plane.GetDescription());
                    item.Tag = plane;
                    item.BackColor = plane.Visible ? SystemColors.Window : SystemColors.ControlLight;
                    item.ForeColor = plane.Visible ? SystemColors.ControlText : SystemColors.GrayText;

                    item.ImageIndex = GetOrCreateColorImage(plane.Color);

                    listViewPlanes.Items.Add(item);
                }
            }
            Flags.Instance.VisualRevision++;
            listViewPlanes.EndUpdate();
            UpdateButtonsState();
        }

 /// <summary>
 /// Creates (raz) i returns index obrazka w ImageList for danego colouru.
 /// </summary>
        private int GetOrCreateColorImage(Color color)
        {
            int key = color.ToArgb();

            if (_colorImageCache.TryGetValue(key, out int existingIndex))
                return existingIndex;

 // Utwórz bitmapę 16x16 z colourowym kwadratem
            var bmp = new Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);

 // Wypełnienie
                using var brush = new SolidBrush(color);
                g.FillRectangle(brush, 1, 1, 14, 14);

 // Ciemna obwódka
                using var pen = new Pen(Color.FromArgb(80, 80, 80));
                g.DrawRectangle(pen, 0, 0, 15, 15);
            }

            imageList.Images.Add(bmp);
            int newIndex = imageList.Images.Count - 1;
            _colorImageCache[key] = newIndex;
            Flags.Instance.VisualRevision++;
            return newIndex;
        }

        private void UpdateButtonsState()
        {
            bool hasSelection = listViewPlanes.SelectedItems.Count > 0;
            btnEdit.Enabled = hasSelection;
            btnDelete.Enabled = hasSelection;
            Flags.Instance.VisualRevision++;
        }

        #endregion

        #region Event Handlers

        private void ListViewPlanes_SelectedIndexChanged(object sender, EventArgs e)
        {
            Flags.Instance.VisualRevision++;
            UpdateButtonsState();
        }

        private void BtnNewPlane_Click(object sender, EventArgs e)
        {
            var dialog = new PlanePropertiesWindow(_molecule, null);
            dialog.StartPosition = FormStartPosition.CenterParent;

 // ⭐ Show zamiast ShowDialog – niemodalne, one can klikać w 3D
            dialog.FormClosed += (s, args) =>
            {
                if (dialog.DialogResult == DialogResult.OK && dialog.Result != null)
                {
                    _planes.Add(dialog.Result);
                    RefreshList();
                    _onPlanesChanged?.Invoke();
                }
            };
            Flags.Instance.VisualRevision++;
            dialog.Show(this);
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (listViewPlanes.SelectedItems.Count == 0) return;
            var plane = listViewPlanes.SelectedItems[0].Tag as CrystalPlane;
            if (plane == null) return;

            var dialog = new PlanePropertiesWindow(_molecule, plane);
            dialog.StartPosition = FormStartPosition.CenterParent;

            dialog.FormClosed += (s, args) =>
            {
                if (dialog.DialogResult == DialogResult.OK && dialog.Result != null)
                {
                    int index = _planes.IndexOf(plane);
                    if (index >= 0)
                        _planes[index] = dialog.Result;

                    RefreshList();
                    _onPlanesChanged?.Invoke();
                }
            };
            Flags.Instance.VisualRevision++;
            dialog.Show(this);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (listViewPlanes.SelectedItems.Count == 0) return;
            var plane = listViewPlanes.SelectedItems[0].Tag as CrystalPlane;
            if (plane == null) return;
            Flags.Instance.VisualRevision++;
            var result = MessageBox.Show(
                $"Delete plane \"{plane.Name}\"?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _planes.Remove(plane);
                RefreshList();
                _onPlanesChanged?.Invoke();
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}