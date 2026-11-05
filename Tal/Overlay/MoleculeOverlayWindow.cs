// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule overlay module: MoleculeOverlayWindow.

﻿// MoleculeOverlayWindow.cs - POPRAWIONY PLIK
using Microsoft.Xna.Framework;
using Tal.GUI;
using Tal.Overlay;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class MoleculeOverlayWindow : DockContent
    {
        public OverlayManager _overlayManager = new OverlayManager();
        private ColorDialog _colorDialog;
        private Vector3 _selectedColor = new Vector3(0f, 0.478f, 0.8f);
        private OverlayMoleculeData _selectedMolecule = null;
        private MainForm _mainForm;

 /// <summary>
 /// Public method to refresh the overlay view.
 /// </summary>
        public void RefreshOverlayViewPublic()
        {
            RefreshOverlayView();
        }

 /// <summary>
 /// Returns the overlay manager.
 /// </summary>
        public OverlayManager GetOverlayManager()
        {
            return _overlayManager;
        }

 /// <summary>
 /// Public access to the overlay manager.
 /// </summary>
        public OverlayManager OverlayManager => _overlayManager;

 /// <summary>
 /// Public method to refresh the lists.
 /// </summary>
        public void RefreshListPublic()
        {
            UpdateMoleculeList();
        }


        public MoleculeOverlayWindow()
        {
            InitializeComponent();

 // ⭐ ZNAJDŹ MAINFORM
            _mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();

            this.Text = "🧬 Overlay Manager";
            this.DockAreas = DockAreas.DockLeft | DockAreas.DockRight | DockAreas.Float;
            this.BackColor = System.Drawing.Color.FromArgb(248, 248, 250);
            this.MaximumSize = new System.Drawing.Size(1200, 800);
            this.ClientSize = new System.Drawing.Size(650, 600);
            this.MinimumSize = new System.Drawing.Size(450, 400);
            this.CloseButton = true;
            this.CloseButtonVisible = true;
            this.ShowHint = DockState.DockLeft;

            _colorDialog = new ColorDialog();
            _colorDialog.FullOpen = true;

 // ⭐ SUBSCRIBE DO ZDARZEŃ MENEDŻERA
            _overlayManager.MoleculesChanged += OnMoleculesChanged;

 // ⭐ PODPISZ ZDARZENIA
            this.btnAddMolecule.Click += BtnAddMolecule_Click;
            this.btnAddStructure.Click += BtnAddStructure_Click;
 //this.btnAddSelection.Click += BtnAddSelection_Click;
            this.btnRemoveSelected.Click += BtnRemoveSelected_Click;
            this.btnClearAll.Click += BtnClearAll_Click;
            this.btnShowOverlay.Click += BtnShowOverlay_Click;
            this.btnColor.Click += BtnColor_Click;
            this.btnCenter.Click += BtnCenter_Click;
            this.lstMolecules.SelectedIndexChanged += LstMolecules_SelectedIndexChanged;
            this.chkShowAll.CheckedChanged += ChkShowAll_CheckedChanged;

 // ⭐ RADIOBUTTONY TRANSFORMACJI - TYLKO DLA ISTNIEJĄCYCH
            this.rbTransformationNone.CheckedChanged += RbTransformation_CheckedChanged;
            this.rbTransformationMirror.CheckedChanged += RbTransformation_CheckedChanged;

 // ⭐ UKRYJ NIEUŻYWANE RADIOBUTTONY


 // ⭐ DOMYŚLNY KOLOR
            UpdateColorButton();

 // ⭐ STYL VS2022
            ApplyVS2022Style();

 // ⭐ AKTUALIZUJ LISTĘ NA START
            UpdateMoleculeList();

            UpdateStatus("Ready - select atoms and click 'Add Molecule' or load a structure");

 // ⭐⭐ SPRAWDŹ CZY OKNO OVERLAY VIEW JUŻ ISTNIEJE I MA NULL MANAGER ⭐⭐
            FixExistingOverlayView();
        }

 /// <summary>
 /// ⭐ SPRAWDZA CZY ISTNIEJE OKNO OVERLAY VIEW I IF MA NULL MANAGER - USTAWIAMY GO
 /// </summary>
        private void FixExistingOverlayView()
        {
            foreach (var form in Application.OpenForms)
            {
                if (form is OverlayViewWindow overlayView)
                {
                    if (!overlayView.HasValidManager())
                    {
                        overlayView.SetOverlayManager(_overlayManager);
                        System.Diagnostics.Debug.WriteLine("[MoleculeOverlay] Fixed OverlayViewWindow with null manager");
                    }
                }
            }
        }

 // ============================================================
 // ⭐ ZDARZENIA - PRZYCISKI
 // ============================================================

        private void BtnAddMolecule_Click(object sender, EventArgs e)
        {
            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null)
            {
                UpdateStatus("No active document");
                return;
            }

            var mode = GetTransformationMode();
            _overlayManager.AddSelectedMolecule(doc, mode);
            mainForm.ClearAllSelection();
            UpdateStatus($"Added molecules from selected atoms.");
            UpdateButtonStates();
        }

        private void BtnAddStructure_Click(object sender, EventArgs e)
        {
            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null)
            {
                UpdateStatus("No active document");
                return;
            }

            var mode = GetTransformationMode();
            bool ok = _overlayManager.AddMoleculeFromRing(doc, mode);
            if (ok)
            {
                mainForm.ClearAllSelection();
                UpdateStatus("Added molecule from ring.");
            }
            else
                UpdateStatus("Select one atom of a 5- or 6-membered ring.");
            UpdateButtonStates();
        }

        private void BtnAddSelection_Click(object sender, EventArgs e)
        {
            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null)
            {
                UpdateStatus("No active document");
                return;
            }

            var mode = GetTransformationMode();
            _overlayManager.AddSelectedAtoms(doc, mode);
            UpdateStatus($"Added atoms from selected atoms.");
        }

        private void BtnRemoveSelected_Click(object sender, EventArgs e)
        {
            if (_selectedMolecule == null)
            {
                UpdateStatus("No molecule selected");
                return;
            }

 // ⭐ SPRAWDŹ CZY TO BAZOWA (ZABLOKUJ USUWANIE)
            if (_overlayManager.IsBaseMolecule(_selectedMolecule.Id) && _overlayManager.OverlayMolecules.Count > 1)
            {
                UpdateStatus("❌ Cannot remove BASE molecule");
                return;
            }

            if (_overlayManager.RemoveMolecule(_selectedMolecule.Id))
            {
                _selectedMolecule = null;
                UpdateStatus($"Removed molecule.");
                UpdateButtonStates();
            }
        }

        private void BtnClearAll_Click(object sender, EventArgs e)
        {
            if (_overlayManager.TotalAtomCount == 0) return;

            if (MessageBox.Show($"Remove all {_overlayManager.TotalAtomCount} atoms from {_overlayManager.OverlayMolecules.Count} molecules?", "Clear All",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _overlayManager.ClearOverlayMolecules();
                _selectedMolecule = null;
                UpdateStatus("Cleared all overlay molecules");
            }
        }

        private void BtnShowOverlay_Click(object sender, EventArgs e)
        {
            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm == null) return;

 // ⭐ ZNAJDŹ LUB UTWÓRZ OKNO OVERLAY VIEW
            OverlayViewWindow overlayView = null;
            foreach (var form in Application.OpenForms)
            {
                if (form is OverlayViewWindow existing)
                {
                    overlayView = existing;

 // ⭐⭐ SPRAWDŹ CZY OKNO MA PRAWIDŁOWY MENEDŻER ⭐⭐
                    if (!overlayView.HasValidManager())
                    {
 // ⭐ USTAW MENEDŻERA Z TEGO OKNA
                        overlayView.SetOverlayManager(_overlayManager);
                        System.Diagnostics.Debug.WriteLine("[MoleculeOverlay] Set OverlayManager in existing OverlayViewWindow");
                    }

                    if (existing.IsHidden) existing.Show();
                    existing.Activate();
                    break;
                }
            }

            if (overlayView == null)
            {
                overlayView = new OverlayViewWindow(_overlayManager);
                overlayView.Show(mainForm.GetDockPanel(), DockState.Document);
            }

            overlayView.RefreshView();
            UpdateStatus("Overlay View opened.");
        }

        private void BtnColor_Click(object sender, EventArgs e)
        {
            if (_selectedMolecule == null)
            {
                UpdateStatus("No molecule selected");
                return;
            }

            if (_colorDialog.ShowDialog() == DialogResult.OK)
            {
                var newColor = SystemColorToVector3(_colorDialog.Color);
                _selectedColor = newColor;
                UpdateColorButton();

 // ⭐ ZMIEŃ KOLOR - ZAWSZE MOŻNA (NAWET DLA BAZOWEJ)
                _overlayManager.SetMoleculeColor(_selectedMolecule.Id, newColor);
                UpdateMoleculeList();
                RefreshOverlayView();

                bool isBase = _overlayManager.IsBaseMolecule(_selectedMolecule.Id);
                if (isBase && _overlayManager.OverlayMolecules.Count > 1)
                {
                    UpdateStatus($"🎨 Color changed for BASE: {_selectedMolecule.Name}");
                }
                else
                {
                    UpdateStatus($"🎨 Color changed for {_selectedMolecule.Name}");
                }
            }
        }



        private void BtnCenter_Click(object sender, EventArgs e)
        {
            if (_selectedMolecule == null)
            {
                UpdateStatus("No molecule selected");
                return;
            }

            if (_selectedMolecule.WorkingAtoms.Count == 0) return;

            Vector3 center = Vector3.Zero;
            for (int i = 0; i < _selectedMolecule.WorkingAtoms.Count; i++)
            {
                center += _selectedMolecule.WorkingAtoms[i].Coordinate;
            }
            center /= _selectedMolecule.WorkingAtoms.Count;

            for (int i = 0; i < _selectedMolecule.WorkingAtoms.Count; i++)
            {
                var atom = _selectedMolecule.WorkingAtoms[i];
                atom.Coordinate -= center;
                _selectedMolecule.WorkingAtoms[i] = atom;
            }

            _overlayManager.SetTransformationForMolecule(_selectedMolecule.Id, _selectedMolecule.CurrentTransformation);
            UpdateStatus($"Centered molecule at origin");
        }

 // ============================================================
 // ⭐ ZDARZENIA - LISTA
 // ============================================================

        private void LstMolecules_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstMolecules.SelectedIndex < 0 || lstMolecules.SelectedIndex >= _overlayManager.OverlayMolecules.Count)
            {
                _selectedMolecule = null;
                UpdateButtonStates();
                return;
            }

            _selectedMolecule = _overlayManager.OverlayMolecules[lstMolecules.SelectedIndex];

            if (_selectedMolecule != null)
            {
 // ⭐ AKTUALIZUJ KOLOR
                int r = (int)(_selectedMolecule.Color.X * 255);
                int g = (int)(_selectedMolecule.Color.Y * 255);
                int b = (int)(_selectedMolecule.Color.Z * 255);
                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);
                btnColor.BackColor = System.Drawing.Color.FromArgb(r, g, b);
                _selectedColor = _selectedMolecule.Color;

 // ⭐ AKTUALIZUJ RADIOBUTTONY
                switch (_selectedMolecule.CurrentTransformation)
                {
                    case TransformationMode.None:
                        rbTransformationNone.Checked = true;
                        break;
                    case TransformationMode.Mirror:
                        rbTransformationMirror.Checked = true;
                        break;
                    default:
                        rbTransformationNone.Checked = true;
                        break;
                }

 // ⭐ SPRAWDŹ CZY TO BAZOWA
                bool isBase = _overlayManager.IsBaseMolecule(_selectedMolecule.Id);
                bool hasMoreThanOne = _overlayManager.OverlayMolecules.Count > 1;

                if (isBase && hasMoreThanOne)
                {
                    UpdateStatus($"🔒 BASE: {_selectedMolecule.Name} (atoms: {_selectedMolecule.WorkingAtoms.Count}) - only color can be changed");
                }
                else
                {
                    UpdateStatus($"Selected: {_selectedMolecule.Name} (atoms: {_selectedMolecule.WorkingAtoms.Count}, source: {_selectedMolecule.SourceName})");
                }

                UpdateButtonStates();
            }
        }
        private void ChkShowAll_CheckedChanged(object sender, EventArgs e)
        {
            bool show = chkShowAll.Checked;
            foreach (var mol in _overlayManager.OverlayMolecules)
            {
                _overlayManager.ToggleVisibility(mol.Id);
            }
            UpdateStatus(show ? "Showing all overlay molecules" : "Hiding all overlay molecules");
            RefreshOverlayView();
        }

 // ============================================================
 // ⭐ ZDARZENIA - RADIOBUTTONY TRANSFORMACJI
 // ============================================================

        private void RbTransformation_CheckedChanged(object sender, EventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio == null || !radio.Checked) return;

            if (_selectedMolecule == null)
            {
                UpdateStatus("No molecule selected - select one from the list first");
                return;
            }

 // ⭐ SPRAWDŹ CZY TO BAZOWA (ZABLOKUJ TRANSFORMACJE)
            if (_overlayManager.IsBaseMolecule(_selectedMolecule.Id) && _overlayManager.OverlayMolecules.Count > 1)
            {
                UpdateStatus("❌ Cannot transform BASE molecule");
 // ⭐ PRZYWRÓĆ POPRZEDNI STAN RADIOBUTTONA
                RestoreTransformationRadioButton();
                return;
            }

            var mode = GetTransformationMode();

 // ⭐ DLA WSZYSTKICH TRANSFORMACJI UŻYJ TEJ SAMEJ METODY
            _overlayManager.SetTransformationForMolecule(_selectedMolecule.Id, mode);

            UpdateStatus($"Transformation set to: {mode} for {_selectedMolecule.Name}");

 // ⭐ AUTOMATYCZNIE DOPASUJ
            AutoFitSelectedMolecule();

 // ⭐ ODSWIEŻ WIDOK
            RefreshOverlayView();
            UpdateMoleculeList();
        }

 /// <summary>
 /// Przywraca stan radiobuttona do currentj transformation molecule.
 /// </summary>
        private void RestoreTransformationRadioButton()
        {
            if (_selectedMolecule == null) return;

            switch (_selectedMolecule.CurrentTransformation)
            {
                case TransformationMode.None:
                    rbTransformationNone.Checked = true;
                    break;
                case TransformationMode.Mirror:
                    rbTransformationMirror.Checked = true;
                    break;
                default:
                    rbTransformationNone.Checked = true;
                    break;
            }
        }

 /// <summary>
 /// Automatycznie dopasowuje wybraną molecule do bazy (3 selected atoms).
 /// </summary>

        private void AutoFitSelectedMolecule()
        {
            LogToDebug("🔍 AutoFitSelectedMolecule - START", System.Drawing.Color.Cyan);

            if (_selectedMolecule == null)
            {
                LogToDebug("❌ No molecule selected", System.Drawing.Color.Red);
                UpdateStatus("No molecule selected");
                return;
            }

            var firstMolecule = _overlayManager.OverlayMolecules.FirstOrDefault();
            if (firstMolecule == null)
            {
                LogToDebug("❌ No molecules in overlay", System.Drawing.Color.Red);
                return;
            }

            if (_selectedMolecule.Id == firstMolecule.Id)
            {
                LogToDebug($"📌 {_selectedMolecule.Name} is BASE - no fitting", System.Drawing.Color.Yellow);
                UpdateStatus("Base molecule - no fitting");
                return;
            }

            if (!_selectedMolecule.HasReferencePoints)
            {
                LogToDebug($"❌ {_selectedMolecule.Name} has no reference points", System.Drawing.Color.Red);
                UpdateStatus($"Select 3 atoms in {_selectedMolecule.Name}");
                return;
            }

            if (!firstMolecule.HasReferencePoints)
            {
                LogToDebug($"❌ Base {firstMolecule.Name} has no reference points", System.Drawing.Color.Red);
                UpdateStatus($"Select 3 atoms in base molecule");
                return;
            }

            LogToDebug($"📦 Source: {_selectedMolecule.Name}", System.Drawing.Color.LightGray);
            for (int i = 0; i < 3; i++)
            {
                LogToDebug($"  SOURCE P{i + 1}: ({_selectedMolecule.WorkingReferencePoints[i].X:F4}, {_selectedMolecule.WorkingReferencePoints[i].Y:F4}, {_selectedMolecule.WorkingReferencePoints[i].Z:F4}) [{_selectedMolecule.ReferencePointNames[i]}]", System.Drawing.Color.LightBlue);
            }

            LogToDebug($"📦 Base: {firstMolecule.Name}", System.Drawing.Color.LightGray);
            for (int i = 0; i < 3; i++)
            {
                LogToDebug($"  BASE P{i + 1}: ({firstMolecule.WorkingReferencePoints[i].X:F4}, {firstMolecule.WorkingReferencePoints[i].Y:F4}, {firstMolecule.WorkingReferencePoints[i].Z:F4}) [{firstMolecule.ReferencePointNames[i]}]", System.Drawing.Color.LightGreen);
            }

            try
            {
                _overlayManager.FitMoleculeToPoints(
                    _selectedMolecule,
                    _selectedMolecule.WorkingReferencePoints,
                    firstMolecule.WorkingReferencePoints
                );

                LogToDebug($"✅ {_selectedMolecule.Name} fitted to base", System.Drawing.Color.Green);
                UpdateStatus($"Fitted to base");

 // ⭐⭐⭐ AUTOMATYCZNIE CENTRUJ IF AUTO CENTRE JEST WŁĄCZONE ⭐⭐⭐
                if (IsAutoCentreEnabled())
                {
                    LogToDebug("🎯 AutoCentre enabled - centering view", System.Drawing.Color.Yellow);
                    CenterView();
                }
            }
            catch (Exception ex)
            {
                LogToDebug($"❌ Error: {ex.Message}", System.Drawing.Color.Red);
                UpdateStatus($"Error: {ex.Message}");
            }

            LogToDebug("🔍 AutoFitSelectedMolecule - END", System.Drawing.Color.Cyan);
        }

 /// <summary>
 /// Checks czy AutoCentre jest włączone w DisplayOptions.
 /// </summary>
        private bool IsAutoCentreEnabled()
        {
            foreach (var form in Application.OpenForms)
            {
                if (form is DisplayOptionsWindow displayWin)
                {
                    return displayWin.IsAutoCentreChecked();
                }
            }
            return false;
        }

 /// <summary>
 /// Centruje widok w activem oknie 3D.
 /// </summary>
        private void CenterView()
        {
            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm == null) return;

            var doc = mainForm.GetActiveDocument();
            if (doc == null) return;

            var molecule = doc.GetMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0) return;

            var camera = doc.GetCamera();
            if (camera == null) return;

            Vector3 center = Vector3.Zero;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                center += molecule.atom[i].Coordinate;
            }
            center /= molecule.NumberOfAtoms;

            camera.g_system_translate = -center;
            camera.g_cameraPos = new Vector3(0, 0, 80);
            camera.g_targetPos = Vector3.Zero;
            camera.ApplyEffects();

            doc.RefreshView();
            LogToDebug($"🎯 View centered at: ({center.X:F2}, {center.Y:F2}, {center.Z:F2})", System.Drawing.Color.Green);
        }

        private TransformationMode GetTransformationMode()
        {
            if (rbTransformationNone.Checked) return TransformationMode.None;
            if (rbTransformationMirror.Checked) return TransformationMode.Mirror;
            return TransformationMode.None;
        }

 // ============================================================
 // ⭐ ZDARZENIA - MENEDŻER
 // ============================================================

 // MoleculeOverlayWindow.cs - zaktualizuj metodę OnMoleculesChanged

        private void OnMoleculesChanged(object sender, EventArgs e)
        {
            UpdateMoleculeList();
            RefreshOverlayView();

 // ⭐ AUTOMATYCZNIE ZAZNACZ NOWO DODANĄ MOLEKUŁĘ (OSTATNIĄ)
            if (_overlayManager.OverlayMolecules.Count > 0)
            {
                int lastIndex = _overlayManager.OverlayMolecules.Count - 1;
                lstMolecules.SelectedIndex = lastIndex;

 // ⭐ AKTUALIZUJ STAN PRZYCISKÓW (BLOKUJ BAZOWĄ)
                UpdateButtonStates();
            }
        }

 // ============================================================
 // ⭐ METODY POMOCNICZE
 // ============================================================

        public void RefreshOverlayView()
        {
            foreach (var form in Application.OpenForms)
            {
                if (form is OverlayViewWindow overlayView)
                {
                    overlayView.RefreshView();
                    break;
                }
            }
        }

        private void UpdateMoleculeList()
        {
            lstMolecules.Items.Clear();

            var molecules = _overlayManager.OverlayMolecules;
            int baseId = _overlayManager.GetBaseMoleculeId();
            bool hasMoreThanOne = molecules.Count > 1;

            foreach (var mol in molecules)
            {
                int atomCount = mol.WorkingAtoms?.Count ?? 0;

 // ⭐ UŻYJ ORYGINALNEGO KOLORU MOLEKUŁY (NIE ZMIENIAJ GO!)
                var color = System.Drawing.Color.FromArgb(
                    (int)(mol.Color.X * 255),
                    (int)(mol.Color.Y * 255),
                    (int)(mol.Color.Z * 255)
                );

                string trans = mol.CurrentTransformation != TransformationMode.None ? $" [{mol.CurrentTransformation}]" : "";
                string type = mol.IsStructure ? "📂" : (mol.IsSelection ? "✏️" : "🧪");
                string visibility = mol.IsVisible ? "👁️" : "🙈";
                string selectionInfo = mol.IsSelection ? $" (selection: {atomCount} atoms)" : "";

 // ⭐ OZNACZ BAZOWĄ (ALE ZACHOWAJ ORYGINALNY KOLOR)
                string baseMarker = "";
                if (hasMoreThanOne && mol.Id == baseId)
                {
                    baseMarker = " ⭐ BASE";
                }

                string displayText = $"{visibility} {type} {mol.Name}{baseMarker}{trans}{selectionInfo} (atoms: {atomCount})";

                var item = new ListBoxItem(displayText, color);
                lstMolecules.Items.Add(item);
            }

            btnRemoveSelected.Enabled = lstMolecules.Items.Count > 0;
            btnClearAll.Enabled = lstMolecules.Items.Count > 0;
            btnCenter.Enabled = lstMolecules.Items.Count > 0;
            btnShowOverlay.Enabled = lstMolecules.Items.Count > 0;

            lblAtomCount.Text = $"🧪 Atoms: {_overlayManager.TotalAtomCount} | Molecules: {_overlayManager.OverlayMolecules.Count}";
        }

 // MoleculeOverlayWindow.cs - add tę metodę

 /// <summary>
 /// Updates stan przycisków - blokuje operacje na bazowej molekule.
 /// </summary>
        private void UpdateButtonStates()
        {
            bool hasMolecules = _overlayManager.OverlayMolecules.Count > 0;
            bool isBaseSelected = false;
            bool isFirstMolecule = _overlayManager.OverlayMolecules.Count == 1;

            if (_selectedMolecule != null)
            {
                isBaseSelected = _overlayManager.IsBaseMolecule(_selectedMolecule.Id);
            }

 // ⭐ BLOKUJ USUWANIE BAZOWEJ (IF WIĘCEJ NIŻ 1 MOLEKUŁA)
            bool canRemove = hasMolecules &&
                             !isBaseSelected &&
                             _overlayManager.OverlayMolecules.Count > 1;

            if (_overlayManager.OverlayMolecules.Count == 1 && _selectedMolecule != null)
            {
                canRemove = true;
            }

            btnRemoveSelected.Enabled = canRemove;

 // ⭐ BLOKUJ CENTROWANIE BAZOWEJ (IF WIĘCEJ NIŻ 1 MOLEKUŁA)
            bool canCenter = hasMolecules &&
                             !isBaseSelected &&
                             _selectedMolecule != null;

            if (_overlayManager.OverlayMolecules.Count == 1 && _selectedMolecule != null)
            {
                canCenter = true;
            }

            btnCenter.Enabled = canCenter;

 // ⭐ BLOKUJ TRANSFORMACJE DLA BAZOWEJ (IF WIĘCEJ NIŻ 1 MOLEKUŁA)
            bool canTransform = hasMolecules &&
                                !isBaseSelected &&
                                _selectedMolecule != null;

            if (_overlayManager.OverlayMolecules.Count == 1 && _selectedMolecule != null)
            {
                canTransform = true;
            }

            rbTransformationNone.Enabled = canTransform;
            rbTransformationMirror.Enabled = canTransform;

 // ⭐⭐⭐ KOLOR - ZAWSZE MOŻNA ZMIENIĆ (NAWET DLA BAZOWEJ) ⭐⭐⭐
            btnColor.Enabled = hasMolecules && _selectedMolecule != null;

 // ⭐ AKTUALIZUJ KOLOR PRZYCISKU
            if (_selectedMolecule != null)
            {
                int r = (int)(_selectedMolecule.Color.X * 255);
                int g = (int)(_selectedMolecule.Color.Y * 255);
                int b = (int)(_selectedMolecule.Color.Z * 255);
                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);
                btnColor.BackColor = System.Drawing.Color.FromArgb(r, g, b);
            }

 // ⭐ STATUS - INFORMACJA O BLOKADACH
            if (isBaseSelected && _overlayManager.OverlayMolecules.Count > 1)
            {
                lblStatus.Text = $"🔒 {_selectedMolecule.Name} is BASE - only color can be changed";
                lblStatus.ForeColor = System.Drawing.Color.FromArgb(200, 100, 100);
            }
            else
            {
                lblStatus.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120);
            }
        }
        private void UpdateStatus(string message)
        {
            lblStatus.Text = message;
            System.Diagnostics.Debug.WriteLine($"[Overlay] {message}");
        }

        private void UpdateColorButton()
        {
            int r = (int)(_selectedColor.X * 255);
            int g = (int)(_selectedColor.Y * 255);
            int b = (int)(_selectedColor.Z * 255);
            r = Math.Clamp(r, 0, 255);
            g = Math.Clamp(g, 0, 255);
            b = Math.Clamp(b, 0, 255);
            btnColor.BackColor = System.Drawing.Color.FromArgb(r, g, b);
        }

        private Vector3 SystemColorToVector3(System.Drawing.Color color)
        {
            return new Vector3(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f
            );
        }

 // ============================================================
 // ⭐ ZMIANA STYLU
 // ============================================================

        public void ApplyStyleToSelectedMolecule(int styleIndex)
        {
            if (_selectedMolecule == null)
            {
                UpdateStatus("No molecule selected");
                return;
            }

            for (int i = 0; i < _selectedMolecule.WorkingAtoms.Count; i++)
            {
                var atom = _selectedMolecule.WorkingAtoms[i];
                atom.style = styleIndex;
                _selectedMolecule.WorkingAtoms[i] = atom;
            }

            RefreshOverlayView();
            UpdateStatus($"Style changed to: {GetStyleName(styleIndex)} for {_selectedMolecule.Name}");
        }

        public void ApplyStyleToSelectedAtoms(int styleIndex)
        {
            if (_selectedMolecule == null)
            {
                UpdateStatus("No molecule selected");
                return;
            }

            bool hasSelection = false;
            for (int i = 0; i < _selectedMolecule.WorkingAtoms.Count; i++)
            {
                if (_selectedMolecule.WorkingAtoms[i].selected)
                {
                    hasSelection = true;
                    break;
                }
            }

            if (!hasSelection)
            {
                UpdateStatus("No atoms selected in this molecule - applying to all atoms");
                ApplyStyleToSelectedMolecule(styleIndex);
                return;
            }

            int changedCount = 0;
            for (int i = 0; i < _selectedMolecule.WorkingAtoms.Count; i++)
            {
                var atom = _selectedMolecule.WorkingAtoms[i];
                if (atom.selected)
                {
                    atom.style = styleIndex;
                    _selectedMolecule.WorkingAtoms[i] = atom;
                    changedCount++;
                }
            }

            RefreshOverlayView();
            UpdateStatus($"Style changed to: {GetStyleName(styleIndex)} for {changedCount} selected atoms in {_selectedMolecule.Name}");
        }

        public void ApplyStyleToAllSelectedAtoms(int styleIndex)
        {
            int changedCount = 0;
            int moleculeCount = 0;

            foreach (var mol in _overlayManager.OverlayMolecules)
            {
                bool hasSelection = false;
                for (int i = 0; i < mol.WorkingAtoms.Count; i++)
                {
                    if (mol.WorkingAtoms[i].selected)
                    {
                        hasSelection = true;
                        break;
                    }
                }

                if (!hasSelection) continue;

                moleculeCount++;
                for (int i = 0; i < mol.WorkingAtoms.Count; i++)
                {
                    var atom = mol.WorkingAtoms[i];
                    if (atom.selected)
                    {
                        atom.style = styleIndex;
                        mol.WorkingAtoms[i] = atom;
                        changedCount++;
                    }
                }
            }

            if (changedCount == 0)
            {
                UpdateStatus("No atoms selected in any molecule");
                return;
            }

            RefreshOverlayView();
            UpdateStatus($"Style changed to: {GetStyleName(styleIndex)} for {changedCount} selected atoms in {moleculeCount} molecules");
        }

        private string GetStyleName(int styleIndex)
        {
            switch (styleIndex)
            {
                case 0: return "CPK";
                case 1: return "Sticks";
                case 2: return "Balls & Sticks";
                case 3: return "Ellipsoids";
                default: return "Unknown";
            }
        }

        public void UpdateOverlayColors()
        {
            bool changed = false;

            foreach (var mol in _overlayManager.OverlayMolecules)
            {
                for (int i = 0; i < mol.WorkingAtoms.Count; i++)
                {
                    var atom = mol.WorkingAtoms[i];
                    if (atom.CustomColor != mol.Color)
                    {
                        mol.Color = atom.CustomColor;
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                UpdateMoleculeList();
                RefreshOverlayView();
                UpdateStatus("Overlay colors updated");
            }
        }



 // ============================================================
 // ⭐ STYLOWANIE
 // ============================================================

        private void ApplyVS2022Style()
        {
            this.BackColor = System.Drawing.Color.FromArgb(248, 248, 250);

            foreach (var btn in new Button[] {
 //btnAddMolecule, btnAddStructure, btnAddSelection,
                btnShowOverlay, btnCenter })
            {
                btn.BackColor = System.Drawing.Color.FromArgb(0, 122, 204);
                btn.ForeColor = System.Drawing.Color.White;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold);
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.Height = 26;
            }

            btnColor.FlatStyle = FlatStyle.Flat;
            btnColor.FlatAppearance.BorderSize = 1;
            btnColor.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(200, 200, 200);
            btnColor.Font = new System.Drawing.Font("Segoe UI", 9F);
            btnColor.Height = 26;

            lstMolecules.Dock = DockStyle.Fill;
            lstMolecules.BackColor = System.Drawing.Color.White;
            lstMolecules.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            lstMolecules.BorderStyle = BorderStyle.FixedSingle;
            lstMolecules.Font = new System.Drawing.Font("Segoe UI", 9F);
            lstMolecules.DrawMode = DrawMode.OwnerDrawVariable;
            lstMolecules.DrawItem += LstMolecules_DrawItem;
            lstMolecules.ItemHeight = 22;

            lblTransformation.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblTransformation.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);

            foreach (var rb in new RadioButton[] {
                rbTransformationNone, rbTransformationMirror })
            {
                rb.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
                rb.BackColor = System.Drawing.Color.Transparent;
                rb.Font = new System.Drawing.Font("Segoe UI", 8F);
            }

            chkShowAll.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            chkShowAll.Font = new System.Drawing.Font("Segoe UI", 9F);

            lblStatus.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120);
            lblStatus.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic);

            lblAtomCount.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51);
            lblAtomCount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        }

        private void LstMolecules_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= lstMolecules.Items.Count) return;

            var item = lstMolecules.Items[e.Index] as ListBoxItem;
            if (item == null) return;

            e.DrawBackground();

            var foreColor = item.Color;

 // ⭐ IF ZAZNACZONE - UŻYJ KONTRASTOWEGO KOLORU
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
 // Afterdświetlenie for selectedgo
                using (var brush = new SolidBrush(System.Drawing.Color.FromArgb(0, 122, 204)))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }
                foreColor = System.Drawing.Color.White;
            }

            using (var brush = new SolidBrush(foreColor))
            {
                e.Graphics.DrawString(
                    item.Text,
                    e.Font ?? this.Font,
                    brush,
                    e.Bounds.Left + 4,
                    e.Bounds.Top + 2
                );
            }

 // ⭐ RAMKA DLA ZAZNACZONEGO
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                using var pen = new Pen(System.Drawing.Color.FromArgb(0, 80, 160), 2);
                e.Graphics.DrawRectangle(pen, e.Bounds);
            }
        }

        private class ListBoxItem
        {
            public string Text { get; set; }
            public System.Drawing.Color Color { get; set; }

            public ListBoxItem(string text, System.Drawing.Color color)
            {
                Text = text;
                Color = color;
            }

            public override string ToString()
            {
                return Text;
            }
        }

        public void RefreshList()
        {
            UpdateMoleculeList();
        }

 // ============================================================
 // ⭐ LOGOWANIE DO OKNA DEBUG
 // ============================================================

        private void LogToDebug(string message, System.Drawing.Color? color = null)
        {
            try
            {
                var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                if (mainForm != null)
                {
                    var debugWindow = Application.OpenForms.OfType<DebugWindow>().FirstOrDefault();
                    if (debugWindow != null)
                    {
                        if (color.HasValue)
                            debugWindow.Log(message, color.Value);
                        else
                            debugWindow.Log(message);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[OVERLAY] {message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[OVERLAY] {message}");
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"[OVERLAY] {message}");
            }
        }
    }
}