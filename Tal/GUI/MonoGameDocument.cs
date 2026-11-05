// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Dockable MonoGame 3D document host.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using Tal.Cavity;
using Tal.DrawMethods;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.GUI
{
    /// <summary>
    /// Represents a dockable document window hosting a 3D molecular visualization.
    /// Contains a GameWindow for rendering, manages molecule data,
    /// cavity detection, and property mapping.
    ///
    /// ⭐ v5: When closing windows disables Progressive AA BEFORE Dispose,
    ///     to avoid NullReference in AccumulateFrame. After closing
    ///     przywraca poprzednie settings AA (OnFormClosed).
    /// </summary>
    public partial class MonoGameDocument : DockContent
    {
        #region Private Fields

        private Panel _gamePanel;
        private GameWindow _gameWindow;
        private Molecule _molecule;

        // ⭐ Structure identification — use FULL PATH as the primary key
        private string _structureName = "";
        private string _sourceFileName = "";
        private string _sourceFilePath = "";     // ⭐ NOWE — pełna ścieżka pliku

        private CavityDetector _cavityDetector;
        private CavityHelpers _cavityHelpers;
        private CavityRenderer _cavityRenderer;
        private CavityPropertyMapper _propertyMapper;

        private Measure _measure;
        private bool _cavityRendererNeedsInit = false;

        // ⭐ Startup document — ad window ready to be replaced by a structure
        private bool _isStartupDocument = false;

        // ⭐ Prevents double Dispose of GameWindow
        private bool _gameWindowDisposed = false;

        // ⭐ v5: remembered AA settings (restored in OnFormClosed)
        private int _savedProgressiveAA = 4;
        private int _savedSSAA = 2;
        private bool _aaSaved = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MonoGameDocument with default title.
        /// </summary>
        public MonoGameDocument() : this("Struktura 3D")
        {
        }

        /// <summary>
        /// Initializes a new instance of the MonoGameDocument with specified title.
        /// </summary>
        /// <param name="title">The window title.</param>
        public MonoGameDocument(string title)
        {
            Text = title;
            DockAreas = DockAreas.Document | DockAreas.Float;
            BackColor = System.Drawing.Color.FromArgb(30, 30, 50);

            _gamePanel = new Panel();
            _gamePanel.Dock = DockStyle.Fill;
            _gamePanel.BackColor = System.Drawing.Color.FromArgb(30, 30, 50);

            _gameWindow = new GameWindow(_gamePanel);
            _gameWindow.Title = title;

            Controls.Add(_gamePanel);
            Disposed += MonoGameDocument_Disposed;

            // ⭐ DIAG — log utworzenia document
            System.Diagnostics.Debug.WriteLine(
                $"[DOC.CTOR] Title='{title}', " +
                $"DOC.Hash={GetHashCode()}, " +
                $"Panel.Hash={_gamePanel.GetHashCode()}, " +
                $"GameWindow.Hash={_gameWindow.GetHashCode()}");

            // ⭐ DIAG — log panel visibility changes
            _gamePanel.VisibleChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DOC.Panel] Title='{Text}', " +
                    $"Panel.Hash={_gamePanel.GetHashCode()}, " +
                    $"Visible → {_gamePanel.Visible}, " +
                    $"DOC.Visible={Visible}, DockState={DockState}");
            };



            // ⭐ Log visibility changes + refresh when it becomes visible
            this.VisibleChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DOC] Title='{Text}', DOC.Visible → {Visible}, DockState={DockState}");

                // ⭐ When the document becomes visible (e.g. user switched tab),
                //    reset the AA accumulation state and refresh the scene.
                //
                //    Uwaga: MainForm.OnActiveDocumentChanged i yes to zrobi for
                //    active document. This handler covers the case when
                //    the document becomes visible without changing ActiveDocument
                //    (e.g. restore from hidden state, window restore).
                if (Visible && !IsDisposed)
                {
                    try
                    {
                        ResetAccumulationState();

                        // Deferred refresh — wait until the panel gets a real size
                        BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                if (!IsDisposed)
                                    RefreshView();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"[DOC] Deferred RefreshView after Visible failed: {ex.Message}");
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[DOC] ResetAccumulationState on Visible failed: {ex.Message}");
                    }
                }
            };

            if (_gameWindow != null && !_gameWindow.IsDisposed)
            {
                _gameWindow.RefreshView();
            }
        }

        #endregion

        #region Disposal

        private void MonoGameDocument_Disposed(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[DOC.DISPOSE] Title='{Text}', DOC.Hash={GetHashCode()}");

            Cleanup();

            // ⭐ Guard against double Dispose of GameWindow
            DisposeGameWindowOnce();
        }

        /// <summary>
        /// ⭐ Safe (idempotentne) zwolnienie GameWindow.
        /// May be called from both OnFormClosing and Disposed —
        /// ale faktycznie zwalnia only raz.
        /// </summary>
        private void DisposeGameWindowOnce()
        {
            if (_gameWindowDisposed) return;
            _gameWindowDisposed = true;

            if (_gameWindow != null)
            {
                try
                {
                    _gameWindow.CleanupBeforeClose();
                    _gameWindow.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[DOC] DisposeGameWindowOnce error: {ex.Message}");
                }
                _gameWindow = null;
            }
        }

        /// <summary>
        /// Performs cleanup of resources (without GPU — dane i eventy).
        /// </summary>
        public void Cleanup()
        {
            // ⭐ Unsubscribe event
            if (_molecule != null)
            {
                try
                {
                    _molecule.MoleculeShiftChanged -= OnMoleculeShiftChanged;
                }
                catch { /* ignore */ }
            }

            // ⭐ Dispose property mappera
            try
            {
                _propertyMapper?.Dispose();
            }
            catch { /* ignore */ }
            _propertyMapper = null;

            // ⭐ Clear cavity renderer
            try
            {
                _cavityRenderer?.Clear();
            }
            catch { /* ignore */ }
            _cavityRenderer = null;

            // ⭐ Clear cavity detector
            try
            {
                _cavityDetector?.Clear();
            }
            catch { /* ignore */ }
            _cavityDetector = null;

            // ⭐ Clear cavity helpers
            try
            {
                _cavityHelpers?.ClearCavities();
            }
            catch { /* ignore */ }
            _cavityHelpers = null;

            _cavityRendererNeedsInit = false;
            _measure = null;
        }

        /// <summary>
        /// ⭐ v5: When closing windows FIRST disable Progressive AA,
        ///     wymuszamy jeden render without AA, a DOPIERO POTEM zwalniamy zasoby.
        ///
        /// Reason: when AA is enabled, AccumulateFrame may enter
        /// NullReference if Dispose zwalnia RenderTargety during
        /// accumulation (race between the timer and Dispose).
        ///
        /// Instead of fighting the race — simply disable accumulation for the duration of
        /// closing. After OnFormClosed przywracamy poprzednie values.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[DOC.OnFormClosing] Title='{Text}', DOC.Hash={GetHashCode()}, Reason={e.CloseReason}");
            //    So that OnContentRemoved (and others) know that MainForm is closing
           
            // ═══════════════════════════════════════════════════════════════
            // ⭐ v5: DISABLE AA BEFORE CLOSING
            // ═══════════════════════════════════════════════════════════════
            try
            {
                if (!_aaSaved)
                {
                    _savedProgressiveAA = Flags.Instance.ProgressiveAASamples;
                    _savedSSAA = Flags.Instance.SSAASamples;
                    _aaSaved = true;
                }

                // Disable accumulation — AccumulateFrame will not be called
                Flags.Instance.ProgressiveAASamples = 1;

                // Force refresh — one frame without AA, then Dispose
                if (_gameWindow != null && !_gameWindow.IsDisposed)
                {
                    try
                    {
                      //  _gameWindow.RefreshView();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"[DOC.OnFormClosing] RefreshView error: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[DOC.OnFormClosing] ProgressiveAA disabled ({_savedProgressiveAA} → 1)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DOC.OnFormClosing] AA disable error: {ex.Message}");
            }

            // ⭐ Full data cleanup (without GPU)
            Cleanup();

            // ⭐ Idempotentne zwolnienie GameWindow — zatrzymuje timer + Dispose GPU
             DisposeGameWindowOnce();

            base.OnFormClosing(e);
        }

        /// <summary>
        /// ⭐ v5: Restore settings Progressive AA after closing windows.
        /// So other open documents do not inherit disabled AA.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                if (_aaSaved)
                {
                    Flags.Instance.ProgressiveAASamples = _savedProgressiveAA;
                    Flags.Instance.SSAASamples = _savedSSAA;
                    Flags.Instance.MarkDirty();

                    System.Diagnostics.Debug.WriteLine(
                        $"[DOC.OnFormClosed] ProgressiveAA restored to {_savedProgressiveAA}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DOC.OnFormClosed] AA restore error: {ex.Message}");
            }

            base.OnFormClosed(e);
        }

        #endregion

        #region Public Properties

        /// <summary>Gets or sets the measure instance for this document.</summary>
        public Measure Measure
        {
            get => _measure;
            set
            {
                _measure = value;
                if (_gameWindow != null && !_gameWindow.IsDisposed)
                {
                    _gameWindow.SetMeasure(_measure);
                }
            }
        }

        /// <summary>
        /// Whether to window startup (z ad) ready do replacement na structure?
        /// </summary>
        public bool IsStartupDocument => _isStartupDocument;

        #endregion

        #region Accessors
        /// <summary>Gets the property mapper.</summary>
        public CavityPropertyMapper GetPropertyMapper() => _propertyMapper;

        /// <summary>Sets the property mapper.</summary>
        public void SetPropertyMapper(CavityPropertyMapper mapper)
        {
            _propertyMapper = mapper;
        }

        /// <summary>
        /// ⭐ Returns renderer elements symmetry this document (may be null).
        /// Proxy do GameWindow.GetSymmetryElementsRenderer().
        ///
        /// Used via MainForm.InvalidateSymmetryRenderers() do wymuszenia
        /// rebuild symmetry-element cache after settings change in the window
        /// Symmetry Elements.
        /// </summary>
        public CellSymmetryRenderer GetSymmetryElementsRenderer()
        {
            return _gameWindow?.GetSymmetryElementsRenderer();
        }

        #endregion
        /// <summary>Gets the camera instance.</summary>
        public Camera GetCamera() => _gameWindow?.GetCamera();

        /// <summary>Gets the molecule instance.</summary>
        public Molecule GetMolecule() => _molecule;

        /// <summary>Gets the measure instance.</summary>
        public Measure GetMeasure() => _measure;

        /// <summary>Gets the graphics device.</summary>
        public GraphicsDevice GetGraphicsDevice()
        {
            return _gameWindow?.GetGraphicsDevice();
        }

        /// <summary>
        /// ⭐ Returns the SpriteFont from the internal GameWindow.
        /// </summary>
        public SpriteFont GetSpriteFont()
        {
            return _gameWindow?.GetSpriteFont();
        }

    

        #region Startup Document

        /// <summary>
        /// Marks to window jako startup (z ad).
        /// </summary>
        public void MarkAsStartupDocument()
        {
            _isStartupDocument = true;
        }

        /// <summary>
        /// Clears the startup flag (after loading a structure).
        /// </summary>
        public void ClearStartupFlag()
        {
            _isStartupDocument = false;
        }

        #endregion

        #region Accumulation Reset (Progressive AA)

        /// <summary>
        /// ⭐ Resetuje stan accumulation Progressive AA w GameWindow this document.
        /// Called when:
        ///   - document becomes visible (tab switch)
        ///   - MainForm.ActiveDocumentChanged
        ///
        /// Without this GameWindow nowego active document kontynuuje accumulation
        /// z nieaktualnymi snapshotami (stara camera, stara VisualRevision) i miesza
        /// frames from different scene states → blurred image.
        /// </summary>
        public void ResetAccumulationState()
        {
            if (_gameWindow == null || _gameWindow.IsDisposed) return;

            try
            {
              

                System.Diagnostics.Debug.WriteLine(
                    $"[MonoGameDocument] Accumulation reset: {Text}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MonoGameDocument] ResetAccumulationState error: {ex.Message}");
            }
        }

        #endregion

        #region Document Management

        /// <summary>
        /// Closes the document.
        /// </summary>
        public void CloseDocument() => Close();

        #endregion

        #region Event Handlers

        private void OnMoleculeShiftChanged(Vector3 newShift)
        {
            Debug.WriteLine($"[MonoGameDocument] MoleculeShift changed to: ({newShift.X:F2}, {newShift.Y:F2}, {newShift.Z:F2})");

            if (_cavityRenderer != null)
            {
                _cavityRenderer.SetMoleculeShift(newShift);
                RefreshView();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // ⭐ DIAG — log resize
            System.Diagnostics.Debug.WriteLine(
                $"[DOC.RESIZE] Title='{Text}', " +
                $"ClientSize={ClientSize.Width}x{ClientSize.Height}, " +
                $"Visible={Visible}, DockState={DockState}, " +
                $"Panel.Visible={_gamePanel?.Visible}, " +
                $"Panel.Size={_gamePanel?.Width}x{_gamePanel?.Height}");

            RefreshView();
        }

        #endregion
    }
}