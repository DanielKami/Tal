// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule overlay module: OverlayViewWindow.

﻿// OverlayViewWindow.cs
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Tal.Graphics;
using Tal.Overlay;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal.Forms
{
    public partial class OverlayViewWindow : DockContent
    {
        private GameWindow _gameWindow;
        private Panel _gamePanel;
        private OverlayManager _overlayManager;
        private List<Molecule.Atom> _displayAtoms = new List<Molecule.Atom>();

        public OverlayViewWindow(OverlayManager manager)
        {
            InitializeComponent();

 // ⭐ ZAPISZ MENEDŻERA (MOŻE BYĆ NULL PRZY WCZYTYWANIU Z UKŁADU)
            _overlayManager = manager;

            if (_overlayManager != null)
            {
                _overlayManager.MoleculesChanged += OnMoleculesChanged;
            }

            this.Text = "🔬 Overlay View";
            this.DockAreas = DockAreas.Document | DockAreas.Float;
            this.BackColor = System.Drawing.Color.FromArgb(30, 30, 50);

 // ⭐ TWORZYMY PANEL DO RENDEROWANIA
            _gamePanel = new Panel();
            _gamePanel.Dock = DockStyle.Fill;
            _gamePanel.BackColor = System.Drawing.Color.FromArgb(30, 30, 50);

 // ⭐ TWORZYMY OKNO GRY
            _gameWindow = new GameWindow(_gamePanel);
            _gameWindow.Title = "Overlay View";

            Controls.Add(_gamePanel);

 // ⭐ ZAŁADUJ PUSTĄ MOLEKUŁĘ (IF NIE MA MENEDŻERA)
            LoadEmptyMolecule();

 // ⭐ IF JEST MENEDŻER - ODSWIEŻ WIDOK
            if (_overlayManager != null)
            {
                UpdateDisplay();
            }

            this.FormClosing += OverlayViewWindow_FormClosing;
        }

 /// <summary>
 /// ⭐ USTAWIA MENEDŻERA NAKŁADKI I SUBSKRYBUJE ZDARZENIA
 /// </summary>
        public void SetOverlayManager(OverlayManager manager)
        {
            if (_overlayManager != null)
            {
                _overlayManager.MoleculesChanged -= OnMoleculesChanged;
            }

            _overlayManager = manager;

            if (_overlayManager != null)
            {
                _overlayManager.MoleculesChanged += OnMoleculesChanged;
                UpdateDisplay();
            }
        }

 /// <summary>
 /// ⭐ SPRAWDZA CZY OKNO MA PRAWIDŁOWO USTAWIONY MENEDŻER
 /// </summary>
        public bool HasValidManager()
        {
            return _overlayManager != null;
        }

        private void LoadEmptyMolecule()
        {
            var molecule = new Molecule();
            molecule.a = 10;
            molecule.b = 10;
            molecule.c = 10;
            molecule.alpha = MathHelperTal.ToRadians(90);
            molecule.beta = MathHelperTal.ToRadians(90);
            molecule.gamma = MathHelperTal.ToRadians(90);
            molecule.SpaceGroupNumber = 1;
            molecule.SpaceGroupName = "P1";
            molecule.atom = new Molecule.Atom[0];
            molecule.NumberOfAtoms = 0;

            _gameWindow.LoadMolecule(molecule);
            _gameWindow.RefreshView();

            System.Diagnostics.Debug.WriteLine("[OverlayView] Empty molecule loaded");
        }

        private void OnMoleculesChanged(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[OverlayView] Molecules changed - refreshing display");
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_gameWindow == null || _gameWindow.IsDisposed)
            {
                System.Diagnostics.Debug.WriteLine("[OverlayView] GameWindow is null or disposed");
                return;
            }

            if (_overlayManager == null)
            {
                System.Diagnostics.Debug.WriteLine("[OverlayView] OverlayManager is null - showing empty view");
                _displayAtoms.Clear();
                _gameWindow.SetOverlayAtoms(_displayAtoms);
                _gameWindow.RefreshView();
                return;
            }

 // ⭐ POBERZ WSZYSTKIE WIDOCZNE ATOMY Z NAKŁADKI
            _displayAtoms.Clear();
            foreach (var mol in _overlayManager.OverlayMolecules)
            {
                if (mol.IsVisible)
                {
                    _displayAtoms.AddRange(mol.WorkingAtoms);
                    System.Diagnostics.Debug.WriteLine($"[OverlayView] Added {mol.WorkingAtoms.Count} atoms from {mol.Name}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"[OverlayView] Total display atoms: {_displayAtoms.Count}");

 // ⭐ PRZEKAŻ ATOMY DO RENDERERA
            _gameWindow.SetOverlayAtoms(_displayAtoms);

 // ⭐ WYWOŁAJ ODSWIEŻENIE
            _gameWindow.RefreshView();

            UpdateStatus($"Displaying {_displayAtoms.Count} atoms from {_overlayManager.OverlayMolecules.Count(m => m.IsVisible)} molecules");
        }

        private void UpdateStatus(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[OverlayView] {message}");
        }

        public void RefreshView()
        {
            System.Diagnostics.Debug.WriteLine("[OverlayView] RefreshView called");

 // ⭐ IF MENEDŻER JEST NULL, NIE ODSWIEŻAJ
            if (_overlayManager == null)
            {
                System.Diagnostics.Debug.WriteLine("[OverlayView] Cannot refresh - manager is null");
                return;
            }

            UpdateDisplay();
        }

        private void OverlayViewWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
 // ⭐ PRZY ZAMYKANIU OKNA - WYCZYŚĆ MENEDŻER
            if (_overlayManager != null)
            {
                _overlayManager.MoleculesChanged -= OnMoleculesChanged;
            }

 // ⭐ WYCZYŚĆ ATOMY W RENDERERZE
            if (_gameWindow != null && !_gameWindow.IsDisposed)
            {
                _displayAtoms.Clear();
                _gameWindow.ClearOverlayAtoms();
                _gameWindow.RefreshView();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_gameWindow != null)
            {
                _gameWindow.CleanupBeforeClose();
                _gameWindow.Dispose();
                _gameWindow = null;
            }
            base.OnFormClosing(e);
        }
    }
}