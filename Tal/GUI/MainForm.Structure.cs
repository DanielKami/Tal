// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — structure load/open, view-mode reset, structure info helpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.Forms;
using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Tal
{
    public partial class MainForm
    {

        #region Structure Management

        public void LoadCifFile(string filePath)
        {
            System.Diagnostics.Debug.WriteLine($"[MainForm] LoadCifFile: {filePath}");

            var storage = StructureStorage.Instance;
            if (!storage.LoadFile(filePath))
            {
                SetStatus("Load error", "Structures: 0", "❌");
                MessageBox.Show("Failed to load CIF file.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _structureListWindow?.UpdateList();
            AddToRecentFiles(filePath);

            string fileName = System.IO.Path.GetFileName(filePath);
            SetStatus($"Loaded: {fileName}",
                $"Structures: {storage.StructureCount}", "✅");

            // ⭐ KEY: fetch structure by FULL PATH (unique key),
            //    not by file name. So two files from the same
            //    names from different directories are not mixed up.
            var structuresFromThisFile = storage.GetStructuresFromFile(filePath);

            if (structuresFromThisFile == null || structuresFromThisFile.Count == 0)
            {
                // Fallback — bardzo rzadko potrzebny
                System.Diagnostics.Debug.WriteLine(
                    $"[MainForm] ⚠️ No structures for path: {filePath}, trying by file name");
                structuresFromThisFile = storage.GetStructuresBySourceFileName(fileName);
            }

            if (structuresFromThisFile == null || structuresFromThisFile.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MainForm] Brak struktur z pliku: {fileName}");
                return;
            }

            System.Diagnostics.Debug.WriteLine(
                $"[MainForm] Otwieram {structuresFromThisFile.Count} struktur z {fileName}");

            // ⭐ Open ALL structures from this file
            foreach (var structure in structuresFromThisFile)
            {
                OpenStructureInNewWindow(structure);
            }
        }

        public MonoGameDocument FindOpenStructureWindow(StructureData structure)
        {
            if (structure == null) return null;

            foreach (var doc in _dockPanel.Documents)
            {
                if (doc is MonoGameDocument monoDoc)
                {
                    // ⭐ NEW signature — pass the ENTIRE structure,
                    //    so that file paths are compared
                    if (monoDoc.MatchesStructure(structure))
                    {
                        return monoDoc;
                    }
                }
            }
            return null;
        }

        public void OpenStructureInNewWindow(StructureData structure)
        {
            if (structure == null) return;

            try
            {
                // ⭐ Check whether a window with this structure already exists
                var existingDoc = FindOpenStructureWindow(structure);
                if (existingDoc != null)
                {
                    existingDoc.Activate();
                    SetStatus($"Activated: {structure.Name}",
                        $"Structures: {StructureStorage.Instance.StructureCount}", "🔄");

                    // ⭐ Notify tool windows — user returned to an existing structure
                    var existingMolecule = existingDoc.GetMolecule();
                    if (existingMolecule != null)
                    {
                        RaiseActiveMoleculeChanged(existingMolecule, existingDoc);
                    }

                    return;
                }

                // ⭐ RESET VIEW MODE BEFORE WCZYTANIEM NOWEJ STRUKTURY
                //    So that Molecule.AsignXYZ → Presentation() will use:
                //      p=0 (Asymmetric), style z Presentation
                ResetViewModeForNewStructure();

                // ⭐ NEW: check for a startup window to replace
                var startupDoc = FindStartupDocument();
                MonoGameDocument doc;

                if (startupDoc != null)
                {
                    // Replace the startup window — do not create a new one
                    System.Diagnostics.Debug.WriteLine(
                        "[MainForm] Reusing startup document for structure");

                    doc = startupDoc;
                    doc.ClearStartupFlag();
                    // doc.HideCommercial();          // hide commercial
                    doc.Text = $"Structure: {structure.Name}";
                    doc.LoadFromStructure(structure);   // this loads the molecule and shows GameWindow
                }
                else
                {
                    // Normalnie — new window
                    doc = new MonoGameDocument($"Structure: {structure.Name}");
                    doc.LoadFromStructure(structure);
                    doc.Show(_dockPanel, DockState.Document);
                }

                // Zastosuj current styl
                int currentStyle = _styleComboBox.SelectedIndex;
                doc.SetStyle(currentStyle);

                // ⭐ Reset Progressive AA — new contacts (lines) change the 3D scene
                Flags.Instance.VisualRevision++;
                Flags.Instance.MarkDirty();

                SetStatus($"Opened: {structure.Name}",
                    $"Structures: {StructureStorage.Instance.StructureCount}", "🔬");

                // ⭐ NEW: Refresh window info if is open
                RefreshStructureInfoWindow();

                // ⭐ NEW: Zresetuj controls DisplayOptions do aktualnego stanu Flags
                GetDisplayOptionsWindow()?.ResetForNewStructure();

                // ═══════════════════════════════════════════════════════════════
                // ⭐ Notify tool windows (HydrogenNormalizationWindow and others)
                //    about the new molecule — auto-refresh the table.
                //
                //    Call at the END of the method, when:
                //      - molecule is already loaded (AsignXYZ has done its work)
                //      - styl is zastosowany
                //      - StructureInfoWindow and DisplayOptions are synchronized
                // ═══════════════════════════════════════════════════════════════
                var newMolecule = doc.GetMolecule();
                if (newMolecule != null)
                {
                    RaiseActiveMoleculeChanged(newMolecule, doc);

                    System.Diagnostics.Debug.WriteLine(
                        $"[MainForm] OpenStructureInNewWindow — raised ActiveMoleculeChanged " +
                        $"for '{newMolecule.Name}' ({newMolecule.NumberOfAtoms} atoms)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainForm] Error opening structure: {ex.Message}");
                MessageBox.Show($"Error opening structure: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Refreshes Structure Info window content if it is open.
        /// Called when:
        ///   - wczytano new file / otworzono structure
        ///   - zmieniono active document (klik na tab)
        /// </summary>
        public void RefreshStructureInfoWindow()
        {
            // If the window does not exist or is closed — do nothing
            if (_structureInfoWindow == null || _structureInfoWindow.IsDisposed)
                return;

            // If the window is hidden in the dock — do nothing either
            if (_structureInfoWindow.IsHidden)
                return;

            var molecule = GetActiveMolecule();
            var doc = GetActiveDocument();

            if (molecule == null)
            {
                // No active molecule — clear the window
                _structureInfoWindow.LoadStructure(null, "(no structure)");
                return;
            }

            string id = doc?.Text ?? "(unknown)";
            _structureInfoWindow.LoadStructure(molecule, id);
        }

        /// <summary>
        /// Finds window startup (z ad) ready do replacement.
        /// </summary>
        private MonoGameDocument FindStartupDocument()
        {
            if (_dockPanel == null) return null;

            foreach (var doc in _dockPanel.Documents)
            {
                if (doc is MonoGameDocument monoDoc && monoDoc.IsStartupDocument)
                {
                    return monoDoc;
                }
            }
            return null;
        }


        #endregion

        #region View Mode Reset

        /// <summary>
        /// Resets presentation mode and related flags to defaults after loading a new structure.
        /// Called from OpenStructureInNewWindow BEFORE loading the molecule,
        /// so that Molecule.AsignXYZ uses the correct initial mode.
        /// 
        /// Sets:
        ///   - GenerateEquvalents = 0     (Asymmetric)
        ///   - DrawPacking = false
        ///   - ShowAsymmetricUnit = true
        ///   - ShortContact = false
        ///   - ShowHBonds = false
        /// 
        /// Also syncs DisplayOptionsWindow and PackingWindow checkboxes.
        /// </summary>
        private void ResetViewModeForNewStructure()
        {
            var flags = Flags.Instance;

            System.Diagnostics.Debug.WriteLine(
                "[MainForm] ResetViewModeForNewStructure: " +
                $"before → GenerateEquvalents={flags.GenerateEquvalents}, " +
                $"DrawPacking={flags.DrawPacking}, " +
                $"ShowAsymmetricUnit={flags.ShowAsymmetricUnit}");

            // ⭐ 1. Reset flags (single source of truth)
            flags.GenerateEquvalents = 0;            // Asymmetric
            flags.DrawPacking = false;
            flags.ShowAsymmetricUnit = true;
            flags.ShortContact = false;
            flags.ShowHBonds = false;

            // ⭐ 2. Reset cell repetition (so packing does not return from the previous session)
            flags.repeat_x = 1;
            flags.repeat_y = 1;
            flags.repeat_z = 1;
            flags.repeatOld_x = 1;
            flags.repeatOld_y = 1;
            flags.repeatOld_z = 1;
            flags.repeat_p_x = 1;
            flags.repeat_p_y = 1;
            flags.repeat_p_z = 1;
            flags.repeat2_p_x = 1;
            flags.repeat2_p_y = 1;
            flags.repeat2_p_z = 1;
            flags.repeatOld2_x = 1;
            flags.repeatOld2_y = 1;
            flags.repeatOld2_z = 1;

            // ⭐ 3. Reset packing shifts
            flags.ShiftNegativeX = 0f;
            flags.ShiftPositiveX = 1f;
            flags.ShiftNegativeY = 0f;
            flags.ShiftPositiveY = 1f;
            flags.ShiftNegativeZ = 0f;
            flags.ShiftPositiveZ = 1f;

            // ⭐ 4. Sync DisplayOptionsWindow checkboxy
            GetDisplayOptionsWindow()?.ResetForNewStructure();

            // ⭐ 5. Sync PackingWindow
            foreach (var form in Application.OpenForms)
            {
                if (form is PackingWindow packingWin)
                {
                    packingWin.SetPackingChecked(false);
                    packingWin.UpdateFromFlags();
                }
            }

            System.Diagnostics.Debug.WriteLine(
                "[MainForm] ResetViewModeForNewStructure: " +
                $"after → GenerateEquvalents={flags.GenerateEquvalents}, " +
                $"DrawPacking={flags.DrawPacking}, " +
                $"ShowAsymmetricUnit={flags.ShowAsymmetricUnit}");
        }

        #endregion
    }
}
