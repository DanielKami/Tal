// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — menu strip construction.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal
{
    public partial class MainForm
    {
        #region Menu

        private void InitializeMenu()
        {
            _menuStrip = new MenuStrip();
            _menuStrip.BackColor = _menuBackColor;
            _menuStrip.ForeColor = _menuForeColor;
            _menuStrip.Font = new System.Drawing.Font("Segoe UI", 10F);
            _menuStrip.Dock = DockStyle.Top;
            _menuStrip.Padding = new Padding(0, 0, 0, 0);
            _menuStrip.Margin = new Padding(0, 0, 0, 0);
            _menuStrip.Renderer = new LightMenuRenderer();
            _recentMenu = new ToolStripMenuItem("Open &Recent");

            // File menu
            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Open...", null, OnOpen) { ShortcutKeys = Keys.Control | Keys.O });
            fileMenu.DropDownItems.Add(_recentMenu);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Save", null, OnSaveAs) { ShortcutKeys = Keys.Control | Keys.S });
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("Save &As...", null, OnSaveAs));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Save Screenshot...", null, OnSaveScreenshot)
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.S
            });
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Export to POV-Ray...", null, OnExportPovRay));
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("&Close", null, OnClose) { ShortcutKeys = Keys.Control | Keys.W });
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(new ToolStripMenuItem("E&xit", null, OnExit) { ShortcutKeys = Keys.Alt | Keys.F4 });

            _menuStrip.Items.Add(fileMenu);

            // Edit menu
            var editMenu = new ToolStripMenuItem("&Edit");
            editMenu.DropDownItems.Add(new ToolStripMenuItem("&Undo", null, OnUndo) { ShortcutKeys = Keys.Control | Keys.Z });
            editMenu.DropDownItems.Add(new ToolStripMenuItem("&Redo", null, OnRedo) { ShortcutKeys = Keys.Control | Keys.Y });
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add(new ToolStripMenuItem("Cu&t", null, OnCut) { ShortcutKeys = Keys.Control | Keys.X });
            editMenu.DropDownItems.Add(new ToolStripMenuItem("&Copy", null, OnCopy) { ShortcutKeys = Keys.Control | Keys.C });
            editMenu.DropDownItems.Add(new ToolStripMenuItem("&Paste", null, OnPaste) { ShortcutKeys = Keys.Control | Keys.V });
            editMenu.DropDownItems.Add(new ToolStripSeparator());
            editMenu.DropDownItems.Add(new ToolStripMenuItem("Select &All", null, OnSelectAll) { ShortcutKeys = Keys.Control | Keys.A });
            _menuStrip.Items.Add(editMenu);

            // Selection menu
            var selectMenu = new ToolStripMenuItem("&Selection");
            selectMenu.DropDownItems.Add(new ToolStripMenuItem("Select &Atoms", null, OnSelectAtoms));
            selectMenu.DropDownItems.Add(new ToolStripMenuItem("Select &Molecules", null, OnSelectMolecules));
            selectMenu.DropDownItems.Add(new ToolStripMenuItem("Select &Unit Cell", null, OnSelectUnitCell));
            selectMenu.DropDownItems.Add(new ToolStripSeparator());
            selectMenu.DropDownItems.Add(new ToolStripMenuItem("Invert Selection", null, OnInvertSelection));
            selectMenu.DropDownItems.Add(new ToolStripMenuItem("Clear Selection", null, OnClearSelection));
            _menuStrip.Items.Add(selectMenu);

            // Display menu — without "Display Options" (przeniesione do Window)
            var displayMenu = new ToolStripMenuItem("&Display");
            displayMenu.DropDownItems.Add(new ToolStripMenuItem("&Style", null, OnDisplayStyle));
            displayMenu.DropDownItems.Add(new ToolStripMenuItem("&Colour atom", null, OnDisplayColour));
            displayMenu.DropDownItems.Add(new ToolStripSeparator());
            displayMenu.DropDownItems.Add(new ToolStripMenuItem("&Labels", null, OnShowLabels));
            displayMenu.DropDownItems.Add(new ToolStripMenuItem("🔗 Bond Display...", null, OnBondDisplay));
            _miShowHydrogens = new ToolStripMenuItem("Show &Hydrogens", null, OnShowHydrogens);
            UpdateShowHydrogensMenuText();
            displayMenu.DropDownItems.Add(_miShowHydrogens);
            _miShowUnitCell = new ToolStripMenuItem("Show &Unit Cell", null, OnShowUnitCell);
            UpdateShowUnitCellMenuText();
            displayMenu.DropDownItems.Add(_miShowUnitCell);
            displayMenu.DropDownItems.Add(new ToolStripSeparator());
            displayMenu.DropDownItems.Add(new ToolStripMenuItem("&Short Contacts...", null, OnShortContacts));
            displayMenu.DropDownItems.Add(new ToolStripSeparator());
            displayMenu.DropDownItems.Add(new ToolStripMenuItem("&Reset View", null, OnResetView));
            displayMenu.DropDownItems.Add(new ToolStripMenuItem("&Background...", null, OnBackgroundColor)
            {
                ShortcutKeys = Keys.Control | Keys.B
            });
            displayMenu.DropDownItems.Add(new ToolStripMenuItem("⚡ &Lighting...", null, OnLighting));
            displayMenu.DropDownItems.Add(new ToolStripMenuItem("📦 Unit Cell Frame...", null, OnUnitCellDisplay));
            displayMenu.DropDownItems.Add(new ToolStripMenuItem("🎨 Atom Display Settings", null, OnAtomDisplaySettings));
            displayMenu.DropDownItems.Add(new ToolStripMenuItem("🎨 Atom &Colors (global)...", null, OnAtomProperties));
            displayMenu.DropDownItems.Add(new ToolStripSeparator());
            displayMenu.DropDownItems.Add(new ToolStripMenuItem("🎚️ &Scene Quality...", null, OnSceneQuality));
            displayMenu.DropDownItems.Add(new ToolStripSeparator());
            displayMenu.DropDownItems.Add(new ToolStripMenuItem(
                "🔷 Symmetry Elements...",
                null,
                OnSymmetryElements)
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.E
            });
            _menuStrip.Items.Add(displayMenu);

            // Calculate menu
            var calculateMenu = new ToolStripMenuItem("&Calculate");
            calculateMenu.DropDownItems.Add(new ToolStripMenuItem("&Centroids...", null, OnCalcCentroids));
            calculateMenu.DropDownItems.Add(new ToolStripMenuItem("&Planes...", null, OnCalcPlanes));
            calculateMenu.DropDownItems.Add(new ToolStripMenuItem("&Packing...", null, OnCalcPacking));
            calculateMenu.DropDownItems.Add(new ToolStripMenuItem("&Contacts...", null, OnCalcContacts));
            calculateMenu.DropDownItems.Add(new ToolStripMenuItem("&Molecular Shell...", null, OnCalcMolecularShell));
            calculateMenu.DropDownItems.Add(new ToolStripMenuItem("&Graph Sets...", null, OnCalcGraphSets));
            calculateMenu.DropDownItems.Add(new ToolStripMenuItem("&Powder Pattern...", null, OnCalcPowderPattern));
            calculateMenu.DropDownItems.Add(new ToolStripSeparator());
            calculateMenu.DropDownItems.Add(new ToolStripMenuItem("🕳️ &Cavity Settings...", null, OnCavitySettings)
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.C
            });
            calculateMenu.DropDownItems.Add(new ToolStripSeparator());
            calculateMenu.DropDownItems.Add(new ToolStripMenuItem("Transform space group...", null, OnTransformSpaceGroup));
            // ⭐ New wpis — Normalize Hydrogens
            calculateMenu.DropDownItems.Add(new ToolStripSeparator());
            calculateMenu.DropDownItems.Add(new ToolStripMenuItem(
                "🧪 Normalize Hydrogens...",
                null,
                OnNormalizeHydrogens));
            _menuStrip.Items.Add(calculateMenu);

            // Advanced menu
            var advancedMenu = new ToolStripMenuItem("&Advanced");
            advancedMenu.DropDownItems.Add(new ToolStripMenuItem("&Hirshfeld Analysis...", null, OnHirshfeldAnalysis));
            advancedMenu.DropDownItems.Add(new ToolStripMenuItem("&Morphology...", null, OnMorphology));
            advancedMenu.DropDownItems.Add(new ToolStripMenuItem("&Molecule Property Mapping...", null, OnMoleculePropertyMapping));
            advancedMenu.DropDownItems.Add(new ToolStripSeparator());
            advancedMenu.DropDownItems.Add(new ToolStripMenuItem("🔬 Structure Overlay...", null, OnStructureOverlay));
            advancedMenu.DropDownItems.Add(new ToolStripMenuItem("⚡ Crystal Calculations...", null, OnCrystalCalculations));
            _menuStrip.Items.Add(advancedMenu);

            // View menu
            var viewMenu = new ToolStripMenuItem("&View");
            viewMenu.DropDownItems.Add(new ToolStripMenuItem("&Reset Layout", null, OnResetLayout));
            _menuStrip.Items.Add(viewMenu);

            // ⭐ Window menu — window management
            var windowMenu = new ToolStripMenuItem("&Window");
            windowMenu.DropDownItems.Add(new ToolStripMenuItem("📋 Structure List", null, OnToggleStructureList));
            windowMenu.DropDownItems.Add(new ToolStripMenuItem("🎨 Display Options", null, OnToggleDisplayOptions));
            windowMenu.DropDownItems.Add(new ToolStripSeparator());
            windowMenu.DropDownItems.Add(new ToolStripMenuItem("🐞 Debug — Symmetry", null, OnShowDebug));
            _menuStrip.Items.Add(windowMenu);

            // Help menu — without "Show Debug" (przeniesione do Window)
            var helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.Add(new ToolStripMenuItem("&Contents", null, OnHelp) { ShortcutKeys = Keys.F1 });
            helpMenu.DropDownItems.Add(new ToolStripSeparator());
            helpMenu.DropDownItems.Add(new ToolStripMenuItem("&About", null, OnAbout));
            _menuStrip.Items.Add(helpMenu);

            this.Controls.Add(_menuStrip);
            this.MainMenuStrip = _menuStrip;
        }

        #endregion
    }
}
