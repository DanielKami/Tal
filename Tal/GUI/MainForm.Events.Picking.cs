// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — picking-mode event handlers and atom/bond selection logic.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal
{
    public partial class MainForm
    {
        #region Picking Events

        /// <summary>
        /// ⭐ Public method to switch picking mode from outside
        /// (np. z DisplayOptionsWindow).
        ///
        /// Safe: when index == current, do NOTHING (do not call the handler
        /// manually — that leads to recursion).
        /// </summary>
        public void SwitchToPickingMode(int index)
        {
            if (_pickingModeComboBox == null || _pickingModeComboBox.IsDisposed)
                return;

            // ⭐ If index changed — SelectedIndexChanged will run the logic.
            //    If the index is the same — do NOTHING (avoid recursion).
            if (_pickingModeComboBox.SelectedIndex != index)
            {
                _pickingModeComboBox.SelectedIndex = index;
            }
        }

        private void PickingModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = _pickingModeComboBox.SelectedIndex;
            System.Diagnostics.Debug.WriteLine($"[PickingMode] SelectedIndexChanged: {index} - {_pickingModeComboBox.Text}");

            Flags.Instance.PickingMode = index;

            var camera = GetActiveCamera();

            // Leaving Move Labels → clear atom selection (labels stay where dragged)
            bool leavingLabelMove = _isLabelMoveMode && index != 5;

            // ⭐ RESET ALL MODE FLAGS AT START
            if (camera != null)
            {
                camera.flag_rotating = false;
                camera.flag_select = false;
                camera.flag_lasso = false;
                camera.flag_moving = false;
                camera.flag_zooming = false;
                camera.flag_release = false;
            }

            _isPickActive = false;
            _isLassoActive = false;
            _isLabelMoveMode = false;
            // Keep select-molecule mode only while staying in Pick Atoms (index 1)
            if (index != 1)
                _isSelectMoleculeMode = false;
            _isMeasurementMode = false;
            _isDraggingLabel = false;
            _isDraggingMeasurementLabel = false;
            _selectedLabelIndex = -1;
            _selectedMeasurementIndex = -1;
            Lasso.VertexList.Clear();

            // ⭐ ALWAYS ZERUJ FLAGI GLOBALNE
            Flags.Instance.RevealSymmetry = false;
            Flags.Instance.IsMeasurementModeActive = false;
            Flags.Instance.MeasurementType = 0;
            Flags.Instance.ExpandContactsActive = false;   // ← KLUCZOWE

            switch (index)
            {
                case 0: // Rotate
                    if (camera != null)
                    {
                        camera.flag_rotating = true;
                        camera.flag_select = false;
                        camera.flag_lasso = false;
                    }
                    SetStatus("Mode: Rotate", "Structures: 0", "🔄");
                    break;

                case 1: // Pick Atoms
                    if (camera != null)
                    {
                        camera.flag_rotating = false;
                        camera.flag_select = true;
                        camera.flag_lasso = false;
                    }
                    _isPickActive = true;

                    var molecule = GetActiveMolecule();
                    if (molecule != null)
                    {
                        for (int i = 0; i < molecule.NumberOfAtoms; i++)
                        {
                            molecule.atom[i].selected = false;
                            molecule.atom[i].SelectionOrder = 0;
                        }
                        molecule.NumberSelected = 0;
                    }
                    _measure?.ClearMeasurements();
                    SetStatus("Mode: Pick Atoms - click atoms to select", "Structures: 0", "👆");
                    break;

                case 2: // Lasso
                    if (camera != null)
                    {
                        camera.flag_rotating = false;
                        camera.flag_lasso = true;
                        camera.flag_select = false;
                    }
                    _isLassoActive = true;
                    SetStatus("Mode: Lasso - draw a shape to select atoms (stays active)", "Structures: 0", "🔲");
                    break;

                case 3: // Expand Contacts
                    if (camera != null)
                    {
                        camera.flag_rotating = true;
                        camera.flag_select = false;
                        camera.flag_lasso = false;
                    }
                    Flags.Instance.ExpandContactsActive = true;
                    SetStatus("Mode: Expand Contacts - click an atom to find nearby atoms", "Structures: 0", "🔗");
                    break;

                case 4: // Toggle Labels
                    if (camera != null)
                    {
                        camera.flag_rotating = true;
                        camera.flag_select = false;
                        camera.flag_lasso = false;
                    }

                    var mol = GetActiveMolecule();
                    if (mol != null && mol.NumberSelected > 0)
                    {
                        int count = 0;
                        for (int i = 0; i < mol.NumberOfAtoms; i++)
                        {
                            if (mol.atom[i].selected)
                            {
                                mol.atom[i].show_label = true;
                                mol.atom[i].selected = false;
                                mol.atom[i].SelectionOrder = 0;
                                count++;
                            }
                        }
                        mol.NumberSelected = 0;
                        Flags.Instance.ShowLabelsFor = 2;
                        Flags.Instance.labels = true;
                        SetStatus($"Labeled {count} atoms (selection cleared)", "Structures: 0", "🏷️");
                    }
                    else
                    {
                        Flags.Instance.labels = !Flags.Instance.labels;
                        SetStatus(Flags.Instance.labels ? "Labels ON" : "Labels OFF",
                                  "Structures: 0", "🏷️");
                    }
                    RefreshAllDocuments();
                    break;

                case 5: // Move Labels
                    if (camera != null)
                    {
                        camera.flag_rotating = false;
                        camera.flag_select = false;
                        camera.flag_lasso = false;
                        camera.flag_moving = false;
                        camera.flag_zooming = false;
                    }
                    _isLabelMoveMode = true;
                    SetStatus("Mode: Move Labels - click a label to drag", "Structures: 0", "🏷️");
                    break;

                case 6: // Measure Distances
                    if (camera != null)
                    {
                        camera.flag_rotating = true;
                        camera.flag_select = false;
                        camera.flag_lasso = false;
                    }
                    Flags.Instance.IsMeasurementModeActive = true;
                    Flags.Instance.MeasurementType = 1;
                    _isMeasurementMode = true;
                    SetStatus("Mode: Measure Distances - click atoms (2 for distance)", "Structures: 0", "📏");
                    break;

                case 7: // Measure Angles
                    if (camera != null)
                    {
                        camera.flag_rotating = true;
                        camera.flag_select = false;
                        camera.flag_lasso = false;
                    }
                    Flags.Instance.IsMeasurementModeActive = true;
                    Flags.Instance.MeasurementType = 2;
                    _isMeasurementMode = true;
                    SetStatus("Mode: Measure Angles - click atoms (3 for angle)", "Structures: 0", "📐");
                    break;

                case 8: // Measure Torsions
                    if (camera != null)
                    {
                        camera.flag_rotating = true;
                        camera.flag_select = false;
                        camera.flag_lasso = false;
                    }
                    Flags.Instance.IsMeasurementModeActive = true;
                    Flags.Instance.MeasurementType = 3;
                    _isMeasurementMode = true;
                    SetStatus("Mode: Measure Torsions - click atoms (4 for torsion)", "Structures: 0", "🔄");
                    break;

                case 9: // Reveal Symmetry
                    if (camera != null)
                    {
                        camera.flag_rotating = true;
                        camera.flag_select = false;
                        camera.flag_lasso = false;
                    }
                    Flags.Instance.RevealSymmetry = true;
                    SetStatus("Mode: Reveal Symmetry - click on screen to generate molecule", "Structures: 0", "🔮");
                    break;

                default:
                    if (camera != null)
                    {
                        camera.flag_rotating = true;
                        camera.flag_select = false;
                        camera.flag_lasso = false;
                    }
                    SetStatus($"Mode: {_pickingModeComboBox.Text}", "Structures: 0", "👆");
                    break;
            }

            // ⭐⭐⭐ NA KONCU: ZSYNCHRONIZUJ EXPAND Z INDEKSEM COMBOBOX ⭐⭐⭐
            Flags.Instance.ExpandContactsActive = (index == 3);

            if (leavingLabelMove)
            {
                var mol = GetActiveMolecule();
                if (mol != null && mol.atom != null)
                {
                    for (int i = 0; i < mol.NumberOfAtoms; i++)
                    {
                        mol.atom[i].selected = false;
                        mol.atom[i].SelectionOrder = 0;
                    }
                    mol.NumberSelected = 0;
                }
                if (camera != null)
                    camera.flag_selected = false;
            }

            camera.MarkChanged();
            RefreshActiveDocument();

            var measure = GetActiveMeasure();
            measure?.ResetPendingSelection();

            System.Diagnostics.Debug.WriteLine($"[PickingMode] KONIEC - tryb: {_pickingModeComboBox.Text}, " +
                                               $"ExpandContactsActive={Flags.Instance.ExpandContactsActive}");
        }

        private void BtnCleanMeasurements_Click(object sender, EventArgs e)
        {
            Flags.Instance.CleanMeasurements = true;
            _measure?.ClearMeasurements();
            SetStatus("Cleared measurements", "Structures: 0", "🧹");
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
        }

        private void ShowLabelsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = _showLabelsComboBox.SelectedIndex;
            Flags.Instance.ShowLabelsFor = index;
            Flags.Instance.labels = true;

            string labelMode = index switch
            {
                0 => "All atoms",
                1 => "Not hydrogen atoms",
                2 => "Selected atoms",
                3 => "None",
                _ => "All atoms"
            };
            SetStatus($"Labels for: {labelMode}", "Structures: 0", "🏷️");

            RefreshAllDocuments();
        }

        private void BtnPickAtom_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[BtnPickAtom_Click] _isPickActive before: {_isPickActive}");

            var camera = GetActiveCamera();
            var molecule = GetActiveMolecule();
            var doc = GetActiveDocument();

            if (camera == null || molecule == null)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            if (_isPickActive)
            {
                // ... (zakomentowane w oryginale)
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[BtnPickAtom_Click] Enabling Pick mode");

                _isPickActive = true;
                _isLassoActive = false;
                _isLabelMoveMode = false;
                _isMeasurementMode = false;
                Lasso.VertexList.Clear();

                camera.flag_rotating = false;
                camera.flag_select = true;
                camera.flag_lasso = false;
                camera.flag_release = false;
                camera.flag_moving = false;

                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    molecule.atom[i].selected = false;
                    molecule.atom[i].SelectionOrder = 0;
                }
                molecule.NumberSelected = 0;

                Flags.Instance.PickingMode = 1;
                Flags.Instance.IsMeasurementModeActive = false;
                Flags.Instance.MeasurementType = 0;
                Flags.Instance.RevealSymmetry = false;

                _pickingModeComboBox.SelectedIndex = 1;

                SetStatus("Pick Atom - click an atom to select (stays active)", "Structures: 0", "👆");
            }

            System.Diagnostics.Debug.WriteLine($"[BtnPickAtom_Click] _isPickActive after: {_isPickActive}");

            if (doc != null)
            {
                doc.RefreshView();
            }
            else
            {
                RefreshAllDocuments();
            }
        }

        private void BtnLasso_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();
            var molecule = GetActiveMolecule();
            var doc = GetActiveDocument();

            if (camera == null || molecule == null)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            if (_isLassoActive)
            {
                // ... (zakomentowane w oryginale)
            }
            else
            {
                camera.flag_rotating = false;
                camera.flag_lasso = true;
                camera.flag_select = false;
                _isLassoActive = true;
                _isPickActive = false;
                _isLabelMoveMode = false;
                Lasso.VertexList.Clear();
                camera.flag_release = false;
                _pickingModeComboBox.SelectedIndex = 2;
                Flags.Instance.PickingMode = 2;
                SetStatus("Lasso - click and drag to select atoms (stays active)", "Structures: 0", "🔲");
            }
            camera.MarkChanged();
            if (doc != null)
            {
                doc.RefreshView();
            }
            else
            {
                RefreshAllDocuments();
            }
        }

        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            int selected = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].style != 10 && molecule.atom[i].show)
                {
                    molecule.atom[i].selected = true;
                    selected++;
                }
            }
            molecule.NumberSelected = selected;
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
            SetStatus($"Selected all {molecule.NumberSelected} atoms", "Structures: 0", "✅");

            RefreshActiveDocument();
        }

        private void BtnSelectAllExceptHydrogens_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();

            var molecule = GetActiveMolecule();
            if (molecule == null)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            int selected = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].style != 10
                    && molecule.atom[i].show
                    && molecule.atom[i].AtomicNumber != 1)
                {
                    molecule.atom[i].selected = true;
                    selected++;
                }
                else
                {
                    molecule.atom[i].selected = false;
                }
            }
            molecule.NumberSelected = selected;
            camera.MarkChanged();
            SetStatus($"Selected {selected} atoms (excluding hydrogens)", "Structures: 0", "✅");
            RefreshActiveDocument();
        }

        private void BtnClearSelection_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();

            var molecule = GetActiveMolecule();
            if (molecule == null)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                molecule.atom[i].selected = false;
                molecule.atom[i].SelectionOrder = 0;
            }
            molecule.NumberSelected = 0;
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
            SetStatus("Cleared selection", "Structures: 0", "🧹");
            camera.MarkChanged();
            var doc = GetActiveDocument();
            if (doc != null)
            {
                doc.RefreshView();
            }
            else
            {
                RefreshAllDocuments();
            }
        }

        private void BtnRevealSymmetry_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[RevealSymmetry] Reveal Symmetry button clicked");

            if (_pickingModeComboBox.SelectedIndex == 9)
            {
                _pickingModeComboBox.SelectedIndex = 0;
            }
            else
            {
                _pickingModeComboBox.SelectedIndex = 9;
            }
            Flags.Instance.VisualRevision++;
        }

        #endregion

        #region Plane Selection Mode (used by PlanePropertiesWindow)

        private int _pickingModeBeforePlaneSelection = -1;

        public void EnterPickingModeForPlaneSelection()
        {
            var camera = GetActiveCamera();
            if (_pickingModeBeforePlaneSelection < 0)
                _pickingModeBeforePlaneSelection = _pickingModeComboBox?.SelectedIndex ?? 0;

            if (_pickingModeComboBox != null && _pickingModeComboBox.SelectedIndex != 1)
            {
                _pickingModeComboBox.SelectedIndex = 1;
            }
            else
            {
                if (camera != null)
                {
                    camera.flag_rotating = false;
                    camera.flag_select = true;
                    camera.flag_lasso = false;
                }
                _isPickActive = true;
                _isLassoActive = false;
                _isLabelMoveMode = false;
                _isMeasurementMode = false;
                Flags.Instance.PickingMode = 1;
            }

            camera.MarkChanged();
        }

        public void ExitPickingModeForPlaneSelection()
        {
            if (_pickingModeBeforePlaneSelection < 0)
                return;

            int previous = _pickingModeBeforePlaneSelection;
            _pickingModeBeforePlaneSelection = -1;

            if (_pickingModeComboBox != null && !_pickingModeComboBox.IsDisposed)
            {
                _pickingModeComboBox.SelectedIndex = previous;
            }
            var camera = GetActiveCamera();
            if (camera != null)
            {
                camera.MarkChanged();
            }
        }

        #endregion
    }
}
