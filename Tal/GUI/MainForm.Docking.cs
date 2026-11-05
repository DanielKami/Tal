// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — docking layout: dock panel init, layout save/load, structure list refresh, tool window open helpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.Forms;
using Tal.GUI;
using Tal.Hirshfeld;
using Tal.Morphology;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal
{
    public partial class MainForm
    {
        #region Dock Container

        private void InitializeDockContainer()
        {
            _dockContainer = new Panel();
            _dockContainer.Dock = DockStyle.Fill;
            _dockContainer.BackColor = _dockContainerBackColor;
            _dockContainer.Padding = new Padding(0, 120, 0, 0);
            this.Controls.Add(_dockContainer);
        }

        #endregion

        #region Docking

        private void InitializeDocking()
        {
            _dockPanel = new DockPanel();
            _dockPanel.Dock = DockStyle.Fill;
            _dockPanel.Theme = new VS2015LightTheme();
            _dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
            _dockPanel.AllowEndUserDocking = true;
            _dockContainer.Controls.Add(_dockPanel);
            this.Text = "Tal - Crystallographic Application";
            this.WindowState = FormWindowState.Maximized;

            // ⭐ React to active document change (tab switch)
            _dockPanel.ActiveDocumentChanged += (s, e) => OnActiveDocumentChanged();

            // ⭐ Also react to document CLOSE — ActiveDocumentChanged
            //    does not always fire when the last document is closed.
            _dockPanel.ContentRemoved += (s, e) => OnContentRemoved(e.Content);

            _dockPanel.DockLeftPortion = 0.25;
            _dockPanel.DockRightPortion = 0.25;
            _dockPanel.DockBottomPortion = 0.28;
        }

        private void CreateSampleWindows()
        {
            // Display Options — always Show onto the dock panel.
            // Setting DockState without a prior Show(_dockPanel, ...) throws NRE
            // (DockHandler is null on a content that was closed/disposed and recreated).
            var displayWin = GetDisplayOptionsWindow();
            if (displayWin == null || displayWin.IsDisposed)
                return;

            if (displayWin.DockPanel == null || displayWin.IsHidden)
                displayWin.Show(_dockPanel, DockState.DockBottom);
            else
                displayWin.Activate();

            displayWin.Height = 200;
            if (this.Height > 0)
                EnsureDisplayOptionsDockHeight(displayWin);
            _bottomPanel = displayWin;

            bool hasDocument = false;
            foreach (var doc in _dockPanel.Documents)
            {
                if (doc is MonoGameDocument)
                {
                    hasDocument = true;
                    break;
                }
            }

            if (!hasDocument)
            {
                var startDoc = new MonoGameDocument("🔬 Welcome");
                startDoc.Show(_dockPanel, DockState.Document);

                // ⭐ Mark as startup — will be eligible for replacement
                startDoc.MarkAsStartupDocument();
            }
        }

        #endregion

        #region Active Document Changed

        /// <summary>
        /// ⭐ Called when user switches tab / aktywuje inny document
        /// or zamyka document (via DocumentClosed).
        ///
        /// Strategia:
        ///   - ONLY active window renderuje (60 FPS)
        ///   - All nieaktywne windows → RenderingSuspended = true (timer zatrzymany, 0% CPU/GPU)
        ///   - After closing ostatniego document → all remaining resumed
        ///
        /// Reset accumulation Progressive AA w nowym aktywnym dokumencie is konieczny,
        /// bo snapshoty (stara camera, stara VisualRevision) are nieaktualne.
        /// </summary>
        /// <summary>
        /// ⭐ Called when user switches tab / aktywuje inny document
        /// or zamyka document (via DocumentClosed).
        ///
        /// Strategia:
        ///   - ONLY active window renderuje (60 FPS)
        ///   - All nieaktywne windows → RenderingSuspended = true (timer zatrzymany, 0% CPU/GPU)
        ///   - After closing ostatniego document → all remaining resumed
        ///
        /// Reset accumulation Progressive AA w nowym aktywnym dokumencie is konieczny,
        /// bo snapshoty (stara camera, stara VisualRevision) are nieaktualne.
        ///
        /// ⭐ NEW: Raises ActiveMoleculeChanged — tool windows
        ///    (HydrogenNormalizationWindow and others) can auto-refresh.
        /// </summary>
        private void OnActiveDocumentChanged()
        {
            var dockPanel = _dockPanel;
            if (dockPanel == null) return;

            var activeDoc = dockPanel.ActiveDocument as MonoGameDocument;

            // ═══════════════════════════════════════════════════════════════
            // STEP 1: Update state of ALL documents
            //         (active → resumed, nieaktywne → suspended)
            // ═══════════════════════════════════════════════════════════════
            foreach (var doc in dockPanel.Documents)
            {
                if (doc is MonoGameDocument monoDoc)
                {
                    // ⭐ Skip already disposed
                    if (monoDoc.IsDisposed) continue;

                    var gw = monoDoc.GetGameWindow();
                    if (gw == null || gw.IsDisposed) continue;

                    bool shouldSuspend = (monoDoc != activeDoc);

                    if (gw.RenderingSuspended != shouldSuspend)
                    {
                        gw.RenderingSuspended = shouldSuspend;

                        System.Diagnostics.Debug.WriteLine(
                            $"[MainForm] Render {(shouldSuspend ? "SUSPENDED" : "RESUMED")} " +
                            $"for '{monoDoc.Text}'");
                    }
                }
            }

            // ═══════════════════════════════════════════════════════════════
            // STEP 2: If there is no active document (all closed)
            //         — nothing more to do, exit.
            // ═══════════════════════════════════════════════════════════════
            if (activeDoc == null || activeDoc.IsDisposed)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[MainForm] OnActiveDocumentChanged: no active document");

                // ⭐ Notify tool windows — no active molecule
                RaiseActiveMoleculeChanged(null, null);
                return;
            }

            // ═══════════════════════════════════════════════════════════════
            // KROK 3: Reset accumulation + deferred refresh active document
            //
            // ⭐ DO NOT call RefreshView synchronously — we are inside a handler
            //    ActiveDocumentChanged/DocumentClosed, the document may be in the middle of
            //    layout change / Dispose. Deferred call via BeginInvoke
            //    guarantees the render happens AFTER the current cycle finishes.
            // ═══════════════════════════════════════════════════════════════
            try
            {
                activeDoc.ResetAccumulationState();

                activeDoc.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (!activeDoc.IsDisposed)
                            activeDoc.RefreshView();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[MainForm] Deferred refresh error: {ex.Message}");
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MainForm] OnActiveDocumentChanged — reset error: {ex.Message}");
            }

            // ═══════════════════════════════════════════════════════════════
            // STEP 4: Refresh dependent windows
            // ═══════════════════════════════════════════════════════════════
            RefreshStructureList();
            RefreshStructureInfoWindow();
            RefreshPlanesWindowForActiveDocument();
            GetDisplayOptionsWindow()?.ResetForNewStructure();

            // ═══════════════════════════════════════════════════════════════
            // STEP 5: ⭐ Notify tool windows about molecule change
            //
            //    HydrogenNormalizationWindow (and other windows) subscribe to this event
            //    and auto-refresh content for the new active structure.
            //
            //    Note: the call may be slightly deferred — tool windows
            //    already use BeginInvoke on their side.
            // ═══════════════════════════════════════════════════════════════
            var activeMolecule = activeDoc.GetMolecule();
            RaiseActiveMoleculeChanged(activeMolecule, activeDoc);
        }


        /// <summary>
        /// ⭐ Called when a DockContent is removed from the DockPanel.
        /// Used instead of the non-existent DocumentClosed event.
        ///
        /// Scenariusz: user zamyka active window z 3 open.
        ///   - ContentRemoved leci DLA ZAMYKANEGO document (still before Dispose)
        ///   - ActiveDocumentChanged may NOT fire (DockPanel does not switch by itself)
        ///   - We must manually: (a) set the new active, (b) resume it, (c) suspend the rest
        /// </summary>
        /// <summary>
        /// ⭐ Called when a DockContent is removed from the DockPanel.
        /// Used instead of the non-existent DocumentClosed event.
        ///
        /// KEY: when closing MainForm, the Handle may already be destroyed
        /// (albo during niszczenia). Wtedy BeginInvoke rzuca InvalidOperationException.
        /// The IsHandleCreated + IsDisposed guard catches this.
        /// </summary>
        private void OnContentRemoved(IDockContent content)
        {
            var removedDoc = content as MonoGameDocument;

            System.Diagnostics.Debug.WriteLine(
                $"[MainForm.OnContentRemoved] '{(removedDoc != null ? removedDoc.Text : "?")}', " +
                $"IsHandleCreated={IsHandleCreated}, IsDisposed={IsDisposed}");

            // ⭐ STEP 1: Check whether we can call BeginInvoke at all
            if (IsDisposed || !IsHandleCreated)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[MainForm.OnContentRemoved] MainForm nie ma uchwytu okna — pomijam BeginInvoke");
                return;
            }

            // ⭐ STEP 2: Check whether MainForm is currently closing
            //    (use _isClosing which you already have for OnExit)
            if (_isClosing)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[MainForm.OnContentRemoved] MainForm jest zamykany — pomijam BeginInvoke");
                return;
            }

            // ⭐ KROK 3: Safe BeginInvoke w try/catch
            try
            {
                BeginInvoke(new Action(() =>
                {
                    // ⭐ Guard #3: Form may have been closed between BeginInvoke and execution
                    if (IsDisposed || !IsHandleCreated) return;
                    if (_isClosing) return;

                    try
                    {
                        OnActiveDocumentChanged();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[MainForm] OnContentRemoved deferred error: " +
                            $"{ex.GetType().Name}: {ex.Message}");
                    }
                }));
            }
            catch (InvalidOperationException ex)
            {
                // Handle disappeared between check and BeginInvoke — ignore
                System.Diagnostics.Debug.WriteLine(
                    $"[MainForm.OnContentRemoved] BeginInvoke threw InvalidOperationException: " +
                    $"{ex.Message}");
            }
        }

        #endregion

        #region Dock Layout

        private void LoadDockLayout()
        {
            try
            {
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Tal",
                    "DockLayout.xml"
                );

                if (File.Exists(configPath))
                {
                    _dockPanel.LoadFromXml(configPath, (typeName) =>
                    {
                        // ⭐ USE GETTERS INSTEAD OF NEW
                        if (typeName == typeof(StructureListWindow).ToString())
                        {
                            return new StructureListWindow();
                        }
                        else if (typeName == typeof(DisplayOptionsWindow).ToString())
                        {
                            return GetDisplayOptionsWindow();
                        }
                        else if (typeName == typeof(CavitySettingsWindow).ToString())
                        {
                            return GetCavitySettingsWindow();
                        }
                        else if (typeName == typeof(PackingWindow).ToString())
                        {
                            return GetPackingWindow();
                        }
                        else if (typeName == typeof(LightingWindow).ToString())
                        {
                            return GetLightingWindow();
                        }
                        else if (typeName == typeof(BackgroundColorWindow).ToString())
                        {
                            return GetBackgroundColorWindow();
                        }
                        else if (typeName == typeof(MoleculeOverlayWindow).ToString())
                        {
                            return GetMoleculeOverlayWindow();
                        }
                        else if (typeName == typeof(OverlayViewWindow).ToString())
                        {
                            return new OverlayViewWindow(null);
                        }
                        else if (typeName == typeof(AutoRotateWindow).ToString())
                        {
                            return GetAutoRotateWindow();
                        }
                        else if (typeName == typeof(AtomColorWindow).ToString())
                        {
                            return GetAtomColorWindow();
                        }
                        else if (typeName == typeof(ShortContactsWindow).ToString())
                        {
                            return GetShortContactsWindow();
                        }
                        else if (typeName == typeof(LabelsWindow).ToString())
                        {
                            return GetLabelsWindow();
                        }
                        else if (typeName == typeof(BondDisplayWindow).ToString())
                        {
                            return GetBondDisplayWindow();
                        }
                        else if (typeName == typeof(AtomDisplayWindow).ToString())
                        {
                            return GetAtomDisplayWindow();
                        }
                        else if (typeName == typeof(DebugWindow).ToString())
                        {
                            return GetDebugWindow();
                        }
                        else if (typeName == typeof(HirshfeldDocument).ToString())
                        {
                            return GetHirshfeldDocument();
                        }
                        else if (typeName == typeof(HydrogenNormalizationWindow).ToString())
                        {
                            return GetHydrogenNormalizationWindow();
                        }
                        else
                        {
                            return null;
                        }
                    });

                    System.Diagnostics.Debug.WriteLine($"[DockLayout] Loaded from: {configPath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DockLayout] No saved layout found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DockLayout] Load error: {ex.Message}");
            }
        }

        private void SaveDockLayout()
        {
            try
            {
                // ⭐ Hide the window before saving so it is not written into the layout
                if (_structureInfoWindow != null && !_structureInfoWindow.IsDisposed)
                {
                    _structureInfoWindow.Hide();
                }

                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Tal",
                    "DockLayout.xml"
                );

                string directory = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _dockPanel.SaveAsXml(configPath);
                System.Diagnostics.Debug.WriteLine($"[DockLayout] Saved to: {configPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DockLayout] Save error: {ex.Message}");
            }
        }

        public void ResetDockLayout()
        {
            try
            {
                string configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Tal",
                    "DockLayout.xml"
                );

                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                    System.Diagnostics.Debug.WriteLine("[DockLayout] Deleted saved layout");
                }

                // Snapshot first — Closing a form removes it from Application.OpenForms,
                // which invalidates a live foreach ("Collection was modified").
                var toClose = new System.Collections.Generic.List<System.Windows.Forms.Form>();
                foreach (System.Windows.Forms.Form form in Application.OpenForms)
                {
                    if (form != null && form != this && !form.IsDisposed)
                        toClose.Add(form);
                }
                foreach (var form in toClose)
                {
                    try
                    {
                        form.Close();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DockLayout] Error closing form: {ex.Message}");
                    }
                }

                CreateSampleWindows();
                _dockPanel?.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DockLayout] Reset error: {ex.Message}");
                MessageBox.Show($"Error resetting layout: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Structure List Window

        public void RefreshStructureList()
        {
            var win = GetStructureListWindow();
            if (win == null) return;
            if (win.IsDisposed) return;

            try
            {
                win.UpdateList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MainForm] RefreshStructureList error: {ex.Message}");
            }
        }

        #endregion

        #region Window Open Methods

        private void OnMoleculeOverlay(object sender, EventArgs e)
        {
            var win = GetMoleculeOverlayWindow();
            if (win == null || win.IsDisposed) return;
            if (win.DockPanel == null || win.IsHidden)
                win.Show(_dockPanel, DockState.DockLeft);
            else
                win.Activate();

            if (this.ClientSize.Width > 0)
            {
                _dockPanel.DockLeftPortion = 350.0 / this.ClientSize.Width;
            }
            else
            {
                _dockPanel.DockLeftPortion = 0.25;
            }
            _dockPanel.Refresh();

            // Check whether OverlayViewWindow exists
            OverlayViewWindow overlayView = null;
            foreach (var form in Application.OpenForms)
            {
                if (form is OverlayViewWindow existing)
                {
                    overlayView = existing;
                    if (existing.IsHidden) existing.Show();
                    existing.Activate();
                    break;
                }
            }

            if (overlayView == null)
            {
                var manager = win.GetOverlayManager();
                if (manager != null)
                {
                    overlayView = new OverlayViewWindow(manager);
                    overlayView.Show(_dockPanel, DockState.Document);
                }
            }

            overlayView?.RefreshView();

            // Overlay uses single-atom pick (hand), not rotate
            SwitchToPickingMode(1);
            Flags.Instance.PickingMode = 1;

            SetStatus("Molecule Overlay opened — pick atoms", $"Structures: {StructureStorage.Instance.StructureCount}", "🧬");
        }

        private void OnShowDebug(object sender, EventArgs e)
        {
            var win = GetDebugWindow();
            win.RefreshDebugInfo();
            ShowToolWindow(win, "🐞 Debug - Symmetry");
        }

        private void OnRefreshDebug(object sender, EventArgs e)
        {
            var win = GetDebugWindow();
            if (win.IsHidden) win.Show();
            win.Activate();
            win.RefreshDebugInfo();
            SetStatus("Debug window refreshed", $"Structures: {StructureStorage.Instance.StructureCount}", "🐞");
        }

        /// <summary>
        /// ⭐ Refreshes all MorphologyDocument and WulffPlotDocument windows (if open).
        /// </summary>
        public void RefreshMorphologyDocuments()
        {
            if (_dockPanel == null) return;

            foreach (var doc in _dockPanel.Documents)
            {
                if (doc is MorphologyDocument morphDoc && !morphDoc.IsDisposed)
                {
                    try { morphDoc.RefreshView(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[MainForm] RefreshMorphologyDocuments error: {ex.Message}");
                    }
                }
                else if (doc is WulffPlotDocument wulffDoc && !wulffDoc.IsDisposed)
                {
                    try { wulffDoc.RefreshFromCrystal(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[MainForm] RefreshMorphologyDocuments (wulff) error: {ex.Message}");
                    }
                }
            }
        }

        #endregion
    }
}
