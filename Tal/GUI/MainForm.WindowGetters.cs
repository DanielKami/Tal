// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — window getters. Lazy singleton accessors for all DockContent / tool windows.

using Tal.Diagnostics;
using Tal.Forms;
using Tal.GUI;
using Tal.Hirshfeld;
using Tal.Morphology;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal
{
    public partial class MainForm
    {
        #region Display Windows

        public DisplayOptionsWindow GetDisplayOptionsWindow()
        {
            if (_displayOptionsWindow == null || _displayOptionsWindow.IsDisposed)
            {
                _displayOptionsWindow = new DisplayOptionsWindow();
                _displayOptionsWindow.FormClosed += (s, e) => _displayOptionsWindow = null;
            }
            return _displayOptionsWindow;
        }

        public AtomColorWindow GetAtomColorWindow()
        {
            if (_atomColorWindow == null || _atomColorWindow.IsDisposed)
            {
                _atomColorWindow = new AtomColorWindow();
                _atomColorWindow.FormClosed += (s, e) => _atomColorWindow = null;
            }
            return _atomColorWindow;
        }

        public AtomDisplayWindow GetAtomDisplayWindow()
        {
            if (_atomDisplayWindow == null || _atomDisplayWindow.IsDisposed)
            {
                _atomDisplayWindow = new AtomDisplayWindow();
                _atomDisplayWindow.FormClosed += (s, e) => _atomDisplayWindow = null;
            }
            return _atomDisplayWindow;
        }

        public UnitCellDisplayWindow GetUnitCellDisplayWindow()
        {
            if (_unitCellDisplayWindow == null || _unitCellDisplayWindow.IsDisposed)
            {
                _unitCellDisplayWindow = new UnitCellDisplayWindow();
                _unitCellDisplayWindow.FormClosed += (s, e) => _unitCellDisplayWindow = null;
            }
            return _unitCellDisplayWindow;
        }

        public LabelsWindow GetLabelsWindow()
        {
            if (_labelsWindow == null || _labelsWindow.IsDisposed)
            {
                _labelsWindow = new LabelsWindow();
                _labelsWindow.FormClosed += (s, e) => _labelsWindow = null;
            }
            return _labelsWindow;
        }

        public BondDisplayWindow GetBondDisplayWindow()
        {
            if (_bondDisplayWindow == null || _bondDisplayWindow.IsDisposed)
            {
                _bondDisplayWindow = new BondDisplayWindow();
                _bondDisplayWindow.FormClosed += (s, e) => _bondDisplayWindow = null;
            }
            return _bondDisplayWindow;
        }

        public LightingWindow GetLightingWindow()
        {
            if (_lightingWindow == null || _lightingWindow.IsDisposed)
            {
                _lightingWindow = new LightingWindow();
                _lightingWindow.FormClosed += (s, e) => _lightingWindow = null;
            }
            return _lightingWindow;
        }

        public BackgroundColorWindow GetBackgroundColorWindow()
        {
            if (_backgroundColorWindow == null || _backgroundColorWindow.IsDisposed)
            {
                _backgroundColorWindow = new BackgroundColorWindow();
                _backgroundColorWindow.FormClosed += (s, e) => _backgroundColorWindow = null;
            }
            return _backgroundColorWindow;
        }

        public DepthCueWindow GetDepthCueWindow()
        {
            if (_depthCueWindow == null || _depthCueWindow.IsDisposed)
            {
                _depthCueWindow = new DepthCueWindow();
                _depthCueWindow.FormClosed += (s, e) => _depthCueWindow = null;
            }
            return _depthCueWindow;
        }

        public SceneQualityWindow GetSceneQualityWindow()
        {
            if (_sceneQualityWindow == null || _sceneQualityWindow.IsDisposed)
            {
                _sceneQualityWindow = new SceneQualityWindow();
                _sceneQualityWindow.FormClosed += (s, e) => _sceneQualityWindow = null;
            }
            return _sceneQualityWindow;
        }

        public AtomPropertiesWindow GetAtomPropertiesWindow()
        {
            if (_atomPropertiesWindow == null || _atomPropertiesWindow.IsDisposed)
            {
                _atomPropertiesWindow = new AtomPropertiesWindow();
                _atomPropertiesWindow.FormClosed += (s, e) => _atomPropertiesWindow = null;
            }
            return _atomPropertiesWindow;
        }

        #endregion

        #region Structure / Analysis Windows

        public CavitySettingsWindow GetCavitySettingsWindow()
        {
            if (_cavitySettingsWindow == null || _cavitySettingsWindow.IsDisposed)
            {
                _cavitySettingsWindow = new CavitySettingsWindow();
                _cavitySettingsWindow.FormClosed += (s, e) => _cavitySettingsWindow = null;
            }
            return _cavitySettingsWindow;
        }

        public PackingWindow GetPackingWindow()
        {
            if (_packingWindow == null || _packingWindow.IsDisposed)
            {
                _packingWindow = new PackingWindow();
                _packingWindow.FormClosed += (s, e) => _packingWindow = null;
            }
            return _packingWindow;
        }

        public ShortContactsWindow GetShortContactsWindow()
        {
            if (_shortContactsWindow == null || _shortContactsWindow.IsDisposed)
            {
                _shortContactsWindow = new ShortContactsWindow();
                _shortContactsWindow.FormClosed += (s, e) => _shortContactsWindow = null;
            }
            return _shortContactsWindow;
        }

        public HBondSettingsWindow GetHBondSettingsWindow()
        {
            if (_hBondSettingsWindow == null || _hBondSettingsWindow.IsDisposed)
            {
                _hBondSettingsWindow = new HBondSettingsWindow();
                _hBondSettingsWindow.FormClosed += (s, e) => _hBondSettingsWindow = null;
            }
            return _hBondSettingsWindow;
        }

        public AutoRotateWindow GetAutoRotateWindow()
        {
            if (_autoRotateWindow == null || _autoRotateWindow.IsDisposed)
            {
                _autoRotateWindow = new AutoRotateWindow();
                _autoRotateWindow.FormClosed += (s, e) => _autoRotateWindow = null;
            }
            return _autoRotateWindow;
        }

        public MoleculeOverlayWindow GetMoleculeOverlayWindow()
        {
            if (_moleculeOverlayWindow == null || _moleculeOverlayWindow.IsDisposed)
            {
                _moleculeOverlayWindow = new MoleculeOverlayWindow();
                _moleculeOverlayWindow.FormClosed += (s, e) => _moleculeOverlayWindow = null;
            }
            return _moleculeOverlayWindow;
        }

        public HydrogenNormalizationWindow GetHydrogenNormalizationWindow()
        {
            if (_hydrogenNormalizationWindow == null || _hydrogenNormalizationWindow.IsDisposed)
            {
                _hydrogenNormalizationWindow = new HydrogenNormalizationWindow();
                _hydrogenNormalizationWindow.FormClosed += (s, e) => _hydrogenNormalizationWindow = null;
            }
            return _hydrogenNormalizationWindow;
        }

        public MorphologyWindow GetMorphologyWindow()
        {
            if (_morphologyWindow == null || _morphologyWindow.IsDisposed)
            {
                _morphologyWindow = new MorphologyWindow();
                _morphologyWindow.FormClosed += (s, e) => _morphologyWindow = null;
            }
            return _morphologyWindow;
        }
      

        public SymmetryReplicationWindow GetSymmetryReplicationWindow()
        {
            if (_symmetryReplicationWindow == null || _symmetryReplicationWindow.IsDisposed)
            {
                _symmetryReplicationWindow = new SymmetryReplicationWindow();
                _symmetryReplicationWindow.FormClosed += (s, e) => _symmetryReplicationWindow = null;
            }
            return _symmetryReplicationWindow;
        }

        public SymmetryElementsWindow GetSymmetryElementsWindow()
        {
            if (_symmetryElementsWindow == null || _symmetryElementsWindow.IsDisposed)
            {
                _symmetryElementsWindow = new SymmetryElementsWindow();
                _symmetryElementsWindow.FormClosed += (s, e) => _symmetryElementsWindow = null;
            }
            return _symmetryElementsWindow;
        }

        public StructureInfoWindow GetStructureInfoWindow()
        {
            if (_structureInfoWindow == null || _structureInfoWindow.IsDisposed)
            {
                _structureInfoWindow = new StructureInfoWindow();
                _structureInfoWindow.FormClosed += (s, e) => _structureInfoWindow = null;
            }
            return _structureInfoWindow;
        }

        /// <summary>
        /// Returns the StructureListWindow if it is already open (or cached).
        /// Does NOT create a new instance — returns null when none exists.
        /// Useful after LoadDockLayout or when the list may have been restored from layout.
        /// </summary>
        public StructureListWindow GetStructureListWindow()
        {
            if (_structureListWindow != null && !_structureListWindow.IsDisposed)
                return _structureListWindow;

            foreach (var form in Application.OpenForms)
            {
                if (form is StructureListWindow existing && !existing.IsDisposed)
                {
                    _structureListWindow = existing;
                    return existing;
                }
            }

            return null;
        }

        #endregion

        #region Hirshfeld Windows

        public HirshfeldFingerprintWindow GetHirshfeldFingerprintWindow(string title = null)
        {
            if (_hirshfeldFingerprintWindow == null || _hirshfeldFingerprintWindow.IsDisposed)
            {
                _hirshfeldFingerprintWindow = new HirshfeldFingerprintWindow();
                if (!string.IsNullOrEmpty(title))
                    _hirshfeldFingerprintWindow.Text = title;
                _hirshfeldFingerprintWindow.FormClosed += (s, e) => _hirshfeldFingerprintWindow = null;
            }
            else if (!string.IsNullOrEmpty(title))
            {
                _hirshfeldFingerprintWindow.Text = title;
            }
            return _hirshfeldFingerprintWindow;
        }

        public HirshfeldDocument GetHirshfeldDocument()
        {
            if (_hirshfeldDocument == null || _hirshfeldDocument.IsDisposed)
            {
                _hirshfeldDocument = new HirshfeldDocument();
                _hirshfeldDocument.FormClosed += (s, e) => _hirshfeldDocument = null;
            }
            return _hirshfeldDocument;
        }

        #endregion

        #region Debug

        public DebugWindow GetDebugWindow()
        {
            if (_debugWindow == null || _debugWindow.IsDisposed)
            {
                _debugWindow = new DebugWindow();
                _debugWindow.FormClosed += (s, e) => _debugWindow = null;
            }
            return _debugWindow;
        }

        #endregion

        #region Show Tool Window Helper

        /// <summary>
        /// Shows a tool window on the left dock with a uniform default width.
        /// If the window is already open, activates it instead.
        /// </summary>
        public CrystalCalculationsWindow GetCrystalCalculationsWindow()
        {
            if (_crystalCalculationsWindow == null || _crystalCalculationsWindow.IsDisposed)
            {
                _crystalCalculationsWindow = new CrystalCalculationsWindow();
                _crystalCalculationsWindow.FormClosed += (s, e) => _crystalCalculationsWindow = null;
            }
            return _crystalCalculationsWindow;
        }

        public void ShowToolWindow(DockContent window, string title = null)
        {
            if (window == null || window.IsDisposed) return;
            if (_dockPanel == null || _dockPanel.IsDisposed) return;

            if (!string.IsNullOrEmpty(title))
                window.Text = title;

            // After close/dispose getters create a fresh DockContent with DockPanel == null.
            // IsHidden alone is not enough — must Show onto the panel first.
            if (window.DockPanel == null || window.IsHidden)
            {
                window.Show(_dockPanel, DockState.DockLeft);

                if (this.Width > 0)
                {
                    _dockPanel.DockLeftPortion = DockWindowConfig.DefaultDockPortionPx
                                               / Math.Max(this.Width, 1);
                }
                _dockPanel.Refresh();
            }
            else
            {
                window.Activate();
            }
        }

        #endregion
    }
}
