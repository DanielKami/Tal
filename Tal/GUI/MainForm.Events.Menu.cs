// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — menu event handlers (File, View, Tools, Analysis, etc.).

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Tal.Cavity;
using Tal.DrawMethods;
using Tal.Forms;
using Tal.GUI;
using Tal.Utils;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal
{
    public partial class MainForm
    {
        #region File Menu Events

        private void OnOpen(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog();
            dialog.Filter =
                "All supported|" +
                    "*.cif;*.res;*.ins;*.xyz;*.pdb;*.ent;*.mol;*.sdf;*.sd;*.mol2;*.ml2;*.gjf;*.com;*.gau|" +
                "Pliki CIF (*.cif)|*.cif|" +
                "Pliki RES/INS (*.res;*.ins)|*.res;*.ins|" +
                "Pliki XYZ (*.xyz)|*.xyz|" +
                "Pliki PDB (*.pdb;*.ent)|*.pdb;*.ent|" +
                "Pliki MOL/SDF (*.mol;*.sdf;*.sd)|*.mol;*.sdf;*.sd|" +
                "Pliki MOL2 (*.mol2;*.ml2)|*.mol2;*.ml2|" +
                "Pliki Gaussian (*.gjf;*.com;*.gau)|*.gjf;*.com;*.gau|" +
                "Wszystkie pliki (*.*)|*.*";

            dialog.Title = "Open structure file";
            dialog.Multiselect = false;

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            // ⭐ All logic in one place — also used by OpenRecentFile, args, IPC, drop
            OpenFileInternal(dialog.FileName);
        }


        private void OnSaveAs(object sender, EventArgs e)
        {
            var storage = StructureStorage.Instance;
            var structure = storage.SelectedStructure;

            // Fallback – try to take the structure from the active document
            if (structure == null)
            {
                var doc = GetActiveDocument();
                // if the document holds a StructureData reference – use it
                // structure = doc?.Structure;
            }

            if (structure == null || structure.Atoms.Count == 0)
            {
                MessageBox.Show("No structure selected to save.", "Save As",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new SaveFileDialog();
            dialog.Filter =
                "CIF (*.cif)|*.cif|" +
                "SHELX RES (*.res)|*.res|" +
                "XYZ (*.xyz)|*.xyz|" +
                "PDB (*.pdb)|*.pdb|" +
                "MOL (*.mol)|*.mol|" +
                "MOL2 (*.mol2)|*.mol2|" +
                "Gaussian input (*.gjf)|*.gjf|" +
                "CRYSTAL17 input (*.d12)|*.d12|" +
                "All files (*.*)|*.*";
            dialog.Title = "Save Structure As";
            dialog.FileName = SanitizeFileName(structure.Name);
            dialog.DefaultExt = "cif";
            dialog.AddExtension = true;

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            SetStatus("Saving...", "", "⏳");
            SetProgressIndeterminate(true);

            try
            {
                bool ok = StructureWriter.Save(structure, dialog.FileName);

                if (ok)
                {
                    SetStatus($"Saved: {Path.GetFileName(dialog.FileName)}",
                              $"Structures: {storage.StructureCount}", "💾");
                }
                else
                {
                    MessageBox.Show("Failed to save file.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetStatus("Save failed", "", "❌");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Save error: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Save error", "", "❌");
            }
            finally
            {
                SetProgressIndeterminate(false);
            }
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "structure";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private void OnNormalizeHydrogens(object sender, EventArgs e)
        {
            var doc = GetActiveDocument();
            var molecule = doc?.GetMolecule();

            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                MessageBox.Show(
                    "No active molecule. Open a structure first.",
                    "Normalize Hydrogens",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var win = GetHydrogenNormalizationWindow();

            // ⭐ Load the molecule BEFORE showing the window (so the table is ready)
            win.LoadMolecule(molecule, doc);

            // ⭐ Left panel — consistent with other tool windows
            ShowToolWindow(win,
                           "🧪 Hydrogen Normalization" );

            SetStatus("Hydrogen Normalization window opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}",
                      "🧪");
        }

        private void OnSaveScreenshot(object sender, EventArgs e)
        {
            var doc = GetActiveDocument();
            if (doc == null)
            {
                SetStatus("No active document", "Structures: 0", "⚠️");
                return;
            }

            var device = doc.GetGraphicsDevice();
            if (device == null || device.IsDisposed)
            {
                SetStatus("GraphicsDevice not ready", "Structures: 0", "⚠️");
                return;
            }

            int baseW = device.PresentationParameters.BackBufferWidth;
            int baseH = device.PresentationParameters.BackBufferHeight;

            // ⭐ Multiplier selection dialog
            using var settingsDlg = new Tal.Forms.ScreenshotDialog(baseW, baseH);
            if (settingsDlg.ShowDialog(this) != DialogResult.OK) return;

            float scale = settingsDlg.Scale;
            bool fullFrame = settingsDlg.FullFrame;

            // ⭐ SaveFileDialog
            using var saveDlg = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png|All files (*.*)|*.*",
                Title = "Save Screenshot",
                FileName = $"TAL_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                DefaultExt = "png",
                AddExtension = true
            };

            if (saveDlg.ShowDialog(this) != DialogResult.OK) return;

            SetStatus($"Saving screenshot ({scale}×)...", "", "⏳");
            SetProgressIndeterminate(true);

            try
            {
                bool ok = doc.SaveScreenshotScaled(saveDlg.FileName, scale, fullFrame);

                if (ok)
                {
                    string fileName = Path.GetFileName(saveDlg.FileName);
                    SetStatus($"Screenshot saved: {fileName} ({scale}×)",
                              $"Structures: {StructureStorage.Instance.StructureCount}", "📷");

                    if (MessageBox.Show(
                            $"Saved to:\n{saveDlg.FileName}\n\nOpen in default viewer?",
                            "Screenshot saved",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDlg.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    SetStatus("Screenshot failed", "Structures: 0", "❌");
                    MessageBox.Show(
                        "Failed to save screenshot.\n" +
                        "Try a smaller scale — the GPU may not have enough VRAM.",
                        "Screenshot Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                SetProgressIndeterminate(false);
            }
        }

        private void OnExportPovRay(object sender, EventArgs e)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                MessageBox.Show("No molecule loaded.", "Export POV-Ray",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ⭐ Zdejmij prefiks "Structure: " (i ewentualny ".cif")
            string docTitle = GetActiveDocument()?.Text ?? "scene";
            docTitle = docTitle.Replace("Structure: ", "").Trim();

            // ⭐ Strip file extension if present
            string suggestedName = System.IO.Path.GetFileNameWithoutExtension(docTitle);
            if (string.IsNullOrWhiteSpace(suggestedName))
                suggestedName = "scene";

            using var dialog = new SaveFileDialog
            {
                Filter = "POV-Ray scene (*.pov)|*.pov|All files (*.*)|*.*",
                Title = "Export to POV-Ray",
                FileName = suggestedName + ".pov"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            SetStatus("Exporting POV-Ray...", "", "⏳");
            SetProgressIndeterminate(true);

            try
            {
                var generator = new PovRayGenerator(molecule);
                generator.SaveWithCavities(dialog.FileName);

                SetStatus($"Exported: {System.IO.Path.GetFileName(dialog.FileName)}",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "✅");

                // Zaproponuj otwarcie w edytorze
                if (MessageBox.Show($"Saved to:\n{dialog.FileName}\n\nOpen in default editor?",
                        "POV-Ray Export", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetStatus("Export error", "", "❌");
            }
            finally
            {
                SetProgressIndeterminate(false);

                // ⭐ Reset ColorManager after eksporcie
                try { ColorManager.ResetDynamicRange(); } catch { /* ignore */ }
            }
        }

        private void OnClose(object sender, EventArgs e)
        {
            var active = _dockPanel.ActiveDocument;
            if (active != null)
            {
                active.DockHandler.Close();
                SetStatus("Document closed", "Structures: 0", "✅");
            }
            else
            {
                SetStatus("No active document", "Structures: 0", "⚠️");
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            if (_isClosing) return;   // guard against double-click

            _isClosing = true;
            ProgressReporter.Enabled = false;   // ⭐ STOP raportowania

            try
            {
                this.Close();   // ⭐ Close MainForm — uruchomi FormClosing i zamknie wszystko
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainForm] OnExit error: {ex.Message}");
                Application.Exit();   // fallback
            }
        }

        #endregion

        #region Edit Menu Events

        private void OnUndo(object sender, EventArgs e) { SetStatus("Undo", "Structures: 0", "↩️"); }
        private void OnRedo(object sender, EventArgs e) { SetStatus("Redo", "Structures: 0", "↪️"); }
        private void OnCut(object sender, EventArgs e) { SetStatus("Cut", "Structures: 0", "✂️"); }
        private void OnCopy(object sender, EventArgs e) { SetStatus("Copy", "Structures: 0", "📋"); }
        private void OnPaste(object sender, EventArgs e) { SetStatus("Paste", "Structures: 0", "📄"); }
        private void OnSelectAll(object sender, EventArgs e) { SetStatus("Select All", "Structures: 0", "✅"); }

        #endregion

        #region Selection Menu Events

        private void OnSelectAtoms(object sender, EventArgs e)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null) return;

            int selected = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                // ⭐ NEW: skip hidden atoms (show == false)
                if (molecule.atom[i].style != 10 && molecule.atom[i].show)
                {
                    molecule.atom[i].selected = true;
                    selected++;
                }
            }
            molecule.NumberSelected = selected;
            SetStatus($"Selected {selected} atoms", "Structures: 0", "✅");
            RefreshActiveDocument();
        }

        private void OnSelectMolecules(object sender, EventArgs e)
        {
            // Enter pick mode; next left-click on an atom selects its whole molecule
            _isSelectMoleculeMode = true;
            if (_pickingModeComboBox != null && _pickingModeComboBox.SelectedIndex != 1)
            {
                SwitchToPickingMode(1); // fires SelectedIndexChanged (keeps mode when index==1)
            }
            else
            {
                var camera = GetActiveCamera();
                if (camera != null)
                {
                    camera.flag_rotating = false;
                    camera.flag_select = true;
                    camera.flag_lasso = false;
                }
                _isPickActive = true;
                _isLassoActive = false;
                Flags.Instance.PickingMode = 1;
            }
            // Re-assert after possible SelectedIndexChanged
            _isSelectMoleculeMode = true;
            SetStatus("Select molecule: click any atom to select the whole molecule",
                      "Structures: 0", "🧪");
        }

        private void OnSelectUnitCell(object sender, EventArgs e)
        {
            SetStatus("Select Unit Cell", "Structures: 0", "📐");
        }

        private void OnInvertSelection(object sender, EventArgs e)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null) return;

            int selected = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                // ⭐ NEW: skip hidden atoms (show == false)
                if (molecule.atom[i].style != 10 && molecule.atom[i].show)
                {
                    molecule.atom[i].selected = !molecule.atom[i].selected;
                    if (molecule.atom[i].selected) selected++;
                }
            }
            molecule.NumberSelected = selected;
            SetStatus($"Inverted selection - {selected} atoms selected", "Structures: 0", "🔄");
            RefreshActiveDocument();
        }

        private void OnClearSelection(object sender, EventArgs e)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null) return;

            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                molecule.atom[i].selected = false;
                molecule.atom[i].SelectionOrder = 0;
            }
            molecule.NumberSelected = 0;
            SetStatus("Cleared selection", "Structures: 0", "🧹");
            RefreshActiveDocument();
        }

        #endregion

        #region Display Menu Events

        private void OnDisplayStyle(object sender, EventArgs e)
        {
            SetStatus("Display Style", "Structures: 0", "🎨");
        }

        private void OnDisplayColour(object sender, EventArgs e)
        {
            ShowToolWindow(GetAtomColorWindow(), "🎨 Atom Color");
            UpdateOverlayColors();
            SetStatus("Atom Color window activated",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🎨");
        }

        private void OnBondDisplay(object sender, EventArgs e)
        {
            ShowToolWindow(GetBondDisplayWindow(), "🔗 Bond Display");
            SetStatus("Bond Display opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
        }

        private void OnShowLabels(object sender, EventArgs e)
        {
            ShowToolWindow(GetLabelsWindow(), "🏷️ Labels Settings");
            SetStatus("Labels Settings opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🏷️");
        }

        private void OnShowHydrogens(object sender, EventArgs e)
        {
            Flags.Instance.ShowHydrogens = !Flags.Instance.ShowHydrogens;
            UpdateShowHydrogensMenuText();

            // Sync Display Options checkbox if the window exists
            try
            {
                var disp = GetDisplayOptionsWindow();
                if (disp != null && !disp.IsDisposed)
                    disp.SetShowHydrogens(Flags.Instance.ShowHydrogens);
            }
            catch { /* window may not exist yet */ }

            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

            // Force redraw of all open structure documents
            var dock = GetDockPanel();
            if (dock != null)
            {
                foreach (var doc in dock.Documents)
                {
                    if (doc is MonoGameDocument monoDoc)
                        monoDoc.RefreshView();
                }
            }

            SetStatus(
                Flags.Instance.ShowHydrogens ? "Hydrogens shown" : "Hydrogens hidden",
                $"Structures: {StructureStorage.Instance.StructureCount}",
                "💧");
        }

        /// <summary>
        /// Menu text reflects current state: Show when hidden, Hide when visible.
        /// </summary>
        private void UpdateShowHydrogensMenuText()
        {
            if (_miShowHydrogens == null) return;
            _miShowHydrogens.Text = Flags.Instance.ShowHydrogens
                ? "Hide &Hydrogens"
                : "Show &Hydrogens";
            _miShowHydrogens.Checked = Flags.Instance.ShowHydrogens;
        }

        /// <summary>Called from Display Options when the hydrogens checkbox changes.</summary>
        public void NotifyHydrogensVisibilityChanged()
        {
            UpdateShowHydrogensMenuText();
        }

        private void OnShowUnitCell(object sender, EventArgs e)
        {
            Flags.Instance.Draw_cell = !Flags.Instance.Draw_cell;
            UpdateShowUnitCellMenuText();

            try
            {
                var disp = GetDisplayOptionsWindow();
                if (disp != null && !disp.IsDisposed)
                    disp.SetShowCellAxes(Flags.Instance.Draw_cell);
            }
            catch { /* window may not exist yet */ }

            try
            {
                var cellWin = GetUnitCellDisplayWindow();
                if (cellWin != null && !cellWin.IsDisposed)
                    cellWin.SetShowCellSilent(Flags.Instance.Draw_cell);
            }
            catch { }

            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

            var dock = GetDockPanel();
            if (dock != null)
            {
                foreach (var doc in dock.Documents)
                {
                    if (doc is MonoGameDocument monoDoc)
                        monoDoc.RefreshView();
                }
            }

            SetStatus(
                Flags.Instance.Draw_cell ? "Unit cell shown" : "Unit cell hidden",
                $"Structures: {StructureStorage.Instance.StructureCount}",
                "📦");
        }

        private void UpdateShowUnitCellMenuText()
        {
            if (_miShowUnitCell == null) return;
            _miShowUnitCell.Text = Flags.Instance.Draw_cell
                ? "Hide &Unit Cell"
                : "Show &Unit Cell";
            _miShowUnitCell.Checked = Flags.Instance.Draw_cell;
        }

        /// <summary>Called from Display Options when the cell-axes checkbox changes.</summary>
        public void NotifyUnitCellVisibilityChanged()
        {
            UpdateShowUnitCellMenuText();
            try
            {
                var cellWin = _unitCellDisplayWindow;
                if (cellWin != null && !cellWin.IsDisposed)
                    cellWin.SetShowCellSilent(Flags.Instance.Draw_cell);
            }
            catch { }
        }

        /// <summary>
        /// Resize the bottom dock so Display Options is not clipped.
        /// Uses content height + generous chrome margin (tabs, borders, DPI).
        /// </summary>
        private void EnsureDisplayOptionsDockHeight(DisplayOptionsWindow win = null)
        {
            if (_dockPanel == null || this.Height <= 0) return;

            win ??= GetDisplayOptionsWindow();
            // Preferred content height (client) + chrome
            int contentH = 210;
            if (win != null)
            {
                if (win.ClientSize.Height > contentH)
                    contentH = win.ClientSize.Height;
                if (win.MinimumSize.Height > contentH)
                    contentH = win.MinimumSize.Height;
            }

            const int DOCK_CHROME = 56; // tab strip + padding + DPI slack
            int dockHeight = contentH + DOCK_CHROME;

            // At least ~280 px, at most 45% of the main form (was 60% but too tall)
            double portion = dockHeight / (double)this.Height;
            portion = Math.Max(portion, 230.0 / this.Height);
            portion = Math.Min(portion, 0.45);

            _dockPanel.DockBottomPortion = portion;
        }

        private void OnToggleDisplayOptions(object sender, EventArgs e)
        {
            // Display Options docks at the bottom — do not use ShowToolWindow (left dock).
            var win = GetDisplayOptionsWindow();
            if (win == null || win.IsDisposed) return;

            if (win.DockPanel == null || win.IsHidden)
                win.Show(_dockPanel, DockState.DockBottom);
            else
                win.Activate();

            // Bottom dock must fit the whole Display Options client area
            // (groups + status strip) plus WeifenLuo tab strip / chrome.
            EnsureDisplayOptionsDockHeight(win);

            _dockPanel.Refresh();
            SetStatus("Display Options shown",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "✅");
        }

        // ⭐ Toggle Structure List z Window menu
        private void OnToggleStructureList(object sender, EventArgs e)
        {
            if (_structureListWindow == null || _structureListWindow.IsDisposed)
            {
                _structureListWindow = new StructureListWindow();
                _structureListWindow.FormClosed += (s, ev) => _structureListWindow = null;
                _structureListWindow.Show(_dockPanel, DockState.DockRight);
                _dockPanel.DockRightPortion = 350.0 / Math.Max(this.ClientSize.Width, 1);
                _dockPanel.Refresh();
                SetStatus("Structure List shown",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "📋");
                return;
            }

            if (_structureListWindow.IsHidden)
            {
                _structureListWindow.Show();
                _structureListWindow.Activate();
                _dockPanel.DockRightPortion = 350.0 / Math.Max(this.ClientSize.Width, 1);
                _dockPanel.Refresh();
                SetStatus("Structure List shown",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "📋");
            }
            else
            {
                _structureListWindow.Hide();
                SetStatus("Structure List hidden",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "👁️");
            }
        }
        private void OnShortContacts(object sender, EventArgs e)
        {
            var win = GetShortContactsWindow();
            win.UpdateFromFlags();
            ShowToolWindow(win, "📏 Define Short Contacts");
            SetStatus("Short Contacts window opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "📏");
        }

        private void OnResetView(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();
            if (camera != null)
            {
                camera.Rotate = Matrix.Identity;

                camera.g_system_translate = Vector3.Zero;
                camera.zoom = Camera.ZOOM;
                camera.OneOverZoom = 1.0f / camera.zoom;
                camera.ApplyEffects();
            }

            var active = GetActiveDocument();
            active?.FitView();

            SetStatus("Reset View", "Structures: 0", "🔄");
        }

        private void OnBackgroundColor(object sender, EventArgs e)
        {
            var win = GetBackgroundColorWindow();
            win.UpdateFromFlags();
            ShowToolWindow(win, "🎨 Background Colors");
            SetStatus("Background Color window activated",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🎨");
        }

        private void OnLighting(object sender, EventArgs e)
        {
            ShowToolWindow(GetLightingWindow(), "⚡ Lighting");
            SetStatus("Lighting settings opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "⚡");
        }

        private void OnUnitCellDisplay(object sender, EventArgs e)
        {
            var win = GetUnitCellDisplayWindow();
            win.UpdateFromFlags();
            ShowToolWindow(win, "📦 Unit Cell Frame");
            SetStatus("Unit Cell Frame settings opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "📦");
        }

        private void OnAtomDisplaySettings(object sender, EventArgs e)
        {
            ShowToolWindow(GetAtomDisplayWindow(), "🎨 Atom Display Settings");
            SetStatus("Atom Display Settings opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🎨");
        }

        #endregion

        #region Calculate Menu Events

        private void OnCalcCentroids(object sender, EventArgs e)
        {
            SetStatus("Calculating centroids...", "Structures: 0", "⏳");
        }

        private void OnCalcPlanes(object sender, EventArgs e)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null)
            {
                SetStatus("No molecule loaded - open a structure first",
                          "Structures: 0", "⚠️");
                return;
            }

            var win = GetPlanesWindow();
            if (win == null) return;

            if (win.IsDisposed)
            {
                _planesWindow = null;
                win = GetPlanesWindow();
                if (win == null) return;
            }

            if (win.IsHidden)
                win.Show(_dockPanel, DockState.DockLeft);
            else
                win.Activate();

            _dockPanel.DockLeftPortion = 340.0 / this.Width;
            _dockPanel.Refresh();
            SetStatus("Planes window opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "📐");
        }

        private void OnCalcPacking(object sender, EventArgs e)
        {
            ShowToolWindow(GetPackingWindow(), "📦 Packing");
            SetStatus("Packing window opened - set min/max for each axis",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "📦");
        }

        private void OnCalcContacts(object sender, EventArgs e)
        {
            OnShortContacts(sender, e);
            SetStatus("Calculating contacts...", "Structures: 0", "⏳");
        }

        private void OnCalcMolecularShell(object sender, EventArgs e)
        {
            SetStatus("Calculating molecular shell...", "Structures: 0", "⏳");
        }

        private void OnCalcGraphSets(object sender, EventArgs e)
        {
            SetStatus("Calculating graph sets...", "Structures: 0", "⏳");
        }

        private void OnCalcPowderPattern(object sender, EventArgs e)
        {
            // ⭐ Check for an active document with a structure
            var doc = GetActiveDocument();
            if (doc == null)
            {
                MessageBox.Show(
                    "Open a structure before starting powder diffraction.",
                    "Brak struktury",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var molecule = doc.GetMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                MessageBox.Show(
                    "No atoms in the structure.",
                    "Brak danych",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // ⭐ Check whether the window already exists — if so, activate
            PowderDiffractionWindow existing = null;
            foreach (var form in Application.OpenForms)
            {
                if (form is PowderDiffractionWindow pdw)
                {
                    existing = pdw;
                    break;
                }
            }

            if (existing != null && !existing.IsDisposed)
            {
                if (existing.IsHidden)
                    existing.Show(_dockPanel, DockState.Document);
                else
                    existing.Activate();

                SetStatus("Powder Diffraction window activated",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "⚡");
                return;
            }

            // ⭐ Create new window
            var powderWin = new PowderDiffractionWindow(molecule);
            powderWin.Show(_dockPanel, DockState.Document);

            SetStatus("Powder Diffraction — calculations started",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "⚡");
        }

        private void OnCavitySettings(object sender, EventArgs e)
        {
            var win = GetCavitySettingsWindow();
            win.RefreshStatus();
            win.UpdateFromFlags();
            ShowToolWindow(win, "🕳️ Cavity Settings");
            SetStatus("Cavity settings opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "⚡");
        }


        #region Transform Space Group

        private void OnTransformSpaceGroup(object sender, EventArgs e)
        {
            var doc = GetActiveDocument();
            if (doc == null)
            {
                SetStatus("No active document", "Structures: 0", "⚠️");
                return;
            }

            var molecule = doc.GetMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            var structure = StructureStorage.Instance.SelectedStructure;
            if (structure == null)
            {
                SetStatus("No structure selected", "Structures: 0", "⚠️");
                return;
            }

            // ── Ustal numer grupy ──
            int group = structure.SpaceGroup;
            if (group < 1)
            {
                string normalized = SymmetryElements.NormalizeSpaceGroupName(
                    structure.SpaceGroupName ?? "");
                if (SymmetryElements.GroupNameToNumber.TryGetValue(normalized, out int num))
                    group = num;
            }

            if (group < 1)
            {
                MessageBox.Show("Cannot determine space group number.",
                    "Transform space group", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // ── Fetch available settings ──
            var availableSettings = Tal.Saver.SpaceGroupTransformer.GetAvailableSettings(group);
            if (availableSettings.Count <= 1)
            {
                MessageBox.Show(
                    $"Only one setting is available for group {group}.\nNothing to transform.",
                    "Transform space group", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // ── Ustal obecne ustawienie ──
            string currentSetting = Tal.Saver.SpaceGroupTransformer.GetCurrentSetting(structure);

            // ── Show selection dialog ──
            using var dlg = new Tal.Forms.SpaceGroupSettingDialog(group, currentSetting, availableSettings);
            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                SetStatus("Transform cancelled",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "ℹ️");
                return;
            }

            string targetSetting = dlg.SelectedSetting;
            if (string.IsNullOrWhiteSpace(targetSetting))
                return;

            // ── Potwierdzenie ──
            string confirmMsg =
                $"Transform space group setting?\n\n" +
                $"Current:  {currentSetting}\n" +
                $"Target:   {targetSetting}\n\n" +
                $"⚠️ The current structure will be modified in-place.\n" +
                $"   Cell, atomic coordinates, ADP tensors and symmetry\n" +
                $"   operations will be transformed.";

            if (MessageBox.Show(confirmMsg, "Confirm transformation",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {

                // ⭐ BEFORE transformation — remember bonds (can also be after, but before is safer)
                var bondsBefore = Tal.Saver.SpaceGroupTransformer.DetectBondsInStructure(structure);
                System.Diagnostics.Debug.WriteLine(
                    $"[MainForm] Detected {bondsBefore.Count} bonds before transform");

                var result = Tal.Saver.SpaceGroupTransformer.TransformTo(structure, targetSetting);
                if (!result.Success) { /* obsługa */ }

                // ⭐ AFTER transformation — iterative atom attraction via bonds
                Tal.Saver.SpaceGroupTransformer.RefineMoleculePositionsAfterTransform(
                    structure, bondsBefore, maxIterations: 25);

                molecule.CleanupBeforeNewLoad();
                molecule.AsignXYZ(structure);

                // ═════════════════════════════════════════════════════════════
                // STEP 4: Refresh document and 3D scene
                // ═════════════════════════════════════════════════════════════
                doc.LoadMolecule(molecule);
                doc.RefreshView();

                // Space-group setting changed (e.g. P21/c → P21/n): same group NUMBER,
                // different operators + coordinates. Drop cached cavity state so the
                // next RunCavityDetection rebuilds with the new symmetry/cell.
                doc.GetCavityDetector()?.InvalidateSymmetryCache();
                doc.GetCavityDetector()?.Clear();
                doc.SetCavityDetector(null);
                doc.SetCavityHelpers(null);

                if (molecule != null)
                {
                    RaiseActiveMoleculeChanged(molecule, doc);
                }
                RefreshStructureInfoWindow();

                Flags.Instance.VisualRevision++;
                Flags.Instance.MarkDirty();

                // ═════════════════════════════════════════════════════════════
                // KROK 5: Status
                // ═════════════════════════════════════════════════════════════
                SetStatus($"Transformed: {currentSetting} → {structure.SpaceGroupName}",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "✅");

                System.Diagnostics.Debug.WriteLine(
                    $"[MainForm] {result.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Transformation failed:\n\n{ex.Message}",
                    "Transform space group", MessageBoxButtons.OK, MessageBoxIcon.Error);

                SetStatus("Transform failed",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "❌");

                System.Diagnostics.Debug.WriteLine(
                    $"[MainForm] Transform error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(
                    $"[MainForm] Stack: {ex.StackTrace}");
            }
        }

        #endregion


        #endregion

        #region Advanced Menu Events

        private void OnCrystalCalculations(object sender, EventArgs e)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                SetStatus("No molecule loaded — open a structure first", "Structures: 0", "⚠️");
                return;
            }
            var win = GetCrystalCalculationsWindow();
            ShowToolWindow(win, "⚡ Crystal Calculations");
            SetStatus("Crystal Calculations", $"Structures: {StructureStorage.Instance.StructureCount}", "⚡");
        }

        private void OnHirshfeldAnalysis(object sender, EventArgs e)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                SetStatus("No molecule loaded — open a structure first",
                          "Structures: 0", "⚠️");
                return;
            }

            // ⭐ Check whether molecules are detected (IndividualMoleculeInCell)
            bool hasMolecules = false;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].IndividualMoleculeInCell >= 0)
                {
                    hasMolecules = true;
                    break;
                }
            }

            if (!hasMolecules)
            {
                SetStatus("No individual molecules detected — run presentation first",
                          "Structures: 0", "⚠️");
                return;
            }

            var doc = GetHirshfeldDocument();
            doc.SetMolecule(molecule, initialMolId: 0);

            if (doc.IsHidden)
                doc.Show(_dockPanel, DockState.Document);   // was: DockState.Document

            doc.Activate();

            // ⭐ Set right dock portion width (like Morphology)
            _dockPanel.DockRightPortion = 420.0 / Math.Max(this.Width, 1);
            _dockPanel.Refresh();

            SetStatus("Hirshfeld Surface window opened — click Generate",
                      $"Structures: {StructureStorage.Instance.StructureCount}",
                      "🔬");
        }

        private void OnMorphology(object sender, EventArgs e)
        {
            // ⭐ NOTE: Morphology has its own docking preference (DockRight)
            var win = GetMorphologyWindow();
            if (win.IsHidden)
                win.Show(_dockPanel, DockState.DockRight);
            win.Activate();

            _dockPanel.DockRightPortion = 420.0 / Math.Max(this.Width, 1);
            _dockPanel.Refresh();

            SetStatus("Morphology window opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "💎");
        }

        private void OnMoleculePropertyMapping(object sender, EventArgs e)
        {
            SetStatus("Molecule Property Mapping...", "Structures: 0", "⏳");
        }

        private void OnStructureOverlay(object sender, EventArgs e)
        {
            // ⭐ NOTE: Overlay has a larger width — custom docking
            var win = GetMoleculeOverlayWindow();
            if (win.IsHidden)
                win.Show(_dockPanel, DockState.DockLeft);
            win.Activate();
            win.RefreshList();

            if (this.ClientSize.Width > 0)
            {
                _dockPanel.DockLeftPortion = 400.0 / this.ClientSize.Width;
            }
            else
            {
                _dockPanel.DockLeftPortion = 0.30;
            }
            _dockPanel.Refresh();

            SwitchToPickingMode(1);
            Flags.Instance.PickingMode = 1;

            SetStatus("Structure Overlay opened — pick atoms",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🔬");
        }

        #endregion

        #region View Menu Events

        private void OnResetLayout(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Reset layout to default? This will close all windows and restore default arrangement.",
                "Reset Layout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ResetDockLayout();
                SetStatus("Layout reset to default", "Structures: 0", "🔄");
            }
        }

        #endregion

        #region Help Menu Events

        private void OnHelp(object sender, EventArgs e)
        {
            try
            {
                // Directory that contains the .exe file
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string helpPath = Path.Combine(exeDir, "help.html");

                if (!File.Exists(helpPath))
                {
                    MessageBox.Show(
                        $"Help file not found:\n{helpPath}",
                        "Help",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Open in the default browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = helpPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open help:\n{ex.Message}",
                    "Help",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OnAbout(object sender, EventArgs e)
        {
            using var about = new AboutForm();
            about.ShowDialog(this);
        }

        #endregion

        #region H-Bond Settings

        private void OnHBondSettings(object sender, EventArgs e)
        {
            ShowToolWindow(GetHBondSettingsWindow(),
                           "🔗 H-Bond Settings" );
            SetStatus("H-Bond Settings opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
        }


        private void OnAtomProperties(object sender, EventArgs e)
        {
            var win = GetAtomPropertiesWindow();
            win.LoadCurrentColors();
            ShowToolWindow(win, "🎨 Atom Colors");

            SetStatus("Atom Colors window opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}",
                      "🎨");
        }

        private void OnSceneQuality(object sender, EventArgs e)
        {
            var win = GetSceneQualityWindow();
            win.UpdateFromFlags();
            ShowToolWindow(win, "🎚️ Scene Quality");

            SetStatus("Scene Quality opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🎚️");
        }

        private void OnSymmetryElements(object sender, EventArgs e)
        {
            var win = GetSymmetryElementsWindow();
            win.UpdateFromFlags();
            ShowToolWindow(win, "🔷 Symmetry Elements");

            SetStatus("Symmetry Elements window opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🔷");
        }
        #endregion
    }
}
