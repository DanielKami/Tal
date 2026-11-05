// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — 3D viewport context menu (build once, update before show).

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System;
using System.Drawing;
using System.Windows.Forms;
using Tal.DrawMethods;
using Tal.GUI;

namespace Tal
{
    public partial class MainForm
    {
        #region Fields

        // ⭐ Cache — menu tworzone RAZ, potem only aktualizowane before pokazaniem
        private ContextMenuStrip _contextMenu;

        // ⭐ Item references — used when updating Enabled/Text
        private ToolStripMenuItem _miHideSelected;
        private ToolStripMenuItem _miHideReverse;
        private ToolStripMenuItem _miShowAll;
        private ToolStripMenuItem _miHideAtom;
        private ToolStripMenuItem _miHideMolecule;
        private ToolStripMenuItem _miHideSymmetry;
        private ToolStripMenuItem _miNoAtom;
        private ToolStripMenuItem _miHideShort;
        private ToolStripMenuItem _miRemoveHanging;
        private ToolStripMenuItem _miSelectMolecule;

        #endregion

        #region Public Entry Point

        /// <summary>
        /// Called via GameWindow when prawym click.
        /// Pokazuje context menu z akcjami Hide.
        /// </summary>
        /// <param name="atomIndex">Indeks clicked atom (-1 if not trafiono).</param>
        /// <param name="screenPos">Screen position where to show the menu.</param>
        public void Show3DContextMenu(int atomIndex, Point screenPos)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0) return;

            EnsureContextMenuBuilt();
            UpdateContextMenuState(atomIndex, molecule);
            _contextMenu.Show(screenPos);
        }

        #endregion

        #region Menu Builder (one-time)

        /// <summary>
        /// Builds the menu ONCE. Later calls are skipped thanks to the null check.
        /// </summary>
        private void EnsureContextMenuBuilt()
        {
            if (_contextMenu != null && !_contextMenu.IsDisposed)
                return;

            _contextMenu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                Font = new Font("Segoe UI", 9F)
            };

            // ═══════════════════════════════════════════════════════
            // HIDE (main)
            // ═══════════════════════════════════════════════════════
            var hideMenu = new ToolStripMenuItem("Hide");

            // Hide selected
            _miHideSelected = new ToolStripMenuItem("Hide selected");
            _miHideSelected.Click += (s, e) => HideAction(HideKind.Selected, -1);
            hideMenu.DropDownItems.Add(_miHideSelected);

            // Hide reverse selected
            _miHideReverse = new ToolStripMenuItem("Hide reverse selected");
            _miHideReverse.Click += (s, e) => HideAction(HideKind.ReverseSelected, -1);
            hideMenu.DropDownItems.Add(_miHideReverse);

            // separator
            hideMenu.DropDownItems.Add(new ToolStripSeparator());

            // Show all
            _miShowAll = new ToolStripMenuItem("Show all");
            _miShowAll.Click += (s, e) => HideAction(HideKind.ShowAll, -1);
            hideMenu.DropDownItems.Add(_miShowAll);

            // separator
            hideMenu.DropDownItems.Add(new ToolStripSeparator());

            // ⭐ Atom/Molecule/Symmetry — text i Enabled aktualizowane dynamicznie
            _miHideAtom = new ToolStripMenuItem("Hide this atom");
            _miHideAtom.Click += (s, e) =>
            {
                int idx = (s as ToolStripMenuItem)?.Tag is int i ? i : -1;
                HideAction(HideKind.ThisAtom, idx);
            };
            hideMenu.DropDownItems.Add(_miHideAtom);

            _miHideMolecule = new ToolStripMenuItem("Hide this molecule");
            _miHideMolecule.Click += (s, e) =>
            {
                int idx = (s as ToolStripMenuItem)?.Tag is int i ? i : -1;
                HideAction(HideKind.ThisMolecule, idx);
            };
            hideMenu.DropDownItems.Add(_miHideMolecule);

            _miHideSymmetry = new ToolStripMenuItem("Hide this symmetry equivalent");
            _miHideSymmetry.Click += (s, e) =>
            {
                int idx = (s as ToolStripMenuItem)?.Tag is int i ? i : -1;
                HideAction(HideKind.ThisSymmetry, idx);
            };
            hideMenu.DropDownItems.Add(_miHideSymmetry);

            // Placeholder when no atom pod kursorem
            _miNoAtom = new ToolStripMenuItem("(no atom under cursor)") { Enabled = false };
            hideMenu.DropDownItems.Add(_miNoAtom);

            // separator
            hideMenu.DropDownItems.Add(new ToolStripSeparator());

            // Short Contacts actions
            _miHideShort = new ToolStripMenuItem("Hide short contacts");
            _miHideShort.Click += (s, e) => HideAction(HideKind.HideShortContacts, -1);
            hideMenu.DropDownItems.Add(_miHideShort);

            _miRemoveHanging = new ToolStripMenuItem("Remove hanging contacts");
            _miRemoveHanging.Click += (s, e) => HideAction(HideKind.RemoveHanging, -1);
            hideMenu.DropDownItems.Add(_miRemoveHanging);

            _miSelectMolecule = new ToolStripMenuItem("Select this molecule");
            _miSelectMolecule.Click += (s, e) =>
            {
                int idx = (s as ToolStripMenuItem)?.Tag is int i ? i : -1;
                SelectMoleculeContainingAtom(idx);
            };
            _contextMenu.Items.Add(_miSelectMolecule);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(hideMenu);
        }

        #endregion

        #region Menu State Update (per show)

        /// <summary>
        /// Updates Enabled/Text/Visible based on the current click context.
        /// Called before every Show().
        /// </summary>
        private void UpdateContextMenuState(int atomIndex, Molecule molecule)
        {
            bool hasSelection = HasAnySelected(molecule);
            bool hasContacts = HasAnyContacts(molecule);
            bool hasAtom = atomIndex >= 0 && atomIndex < molecule.NumberOfAtoms;

            _miHideSelected.Enabled = hasSelection;
            _miHideReverse.Enabled = hasSelection;
            _miShowAll.Enabled = true;

            _miHideShort.Enabled = hasContacts;
            _miRemoveHanging.Enabled = hasContacts;

            // ⭐ Atom / Molecule / Symmetry — visible only when an atom was clicked
            _miHideAtom.Visible = hasAtom;
            _miHideMolecule.Visible = hasAtom;
            _miHideSymmetry.Visible = hasAtom;
            _miNoAtom.Visible = !hasAtom;
            if (_miSelectMolecule != null)
            {
                _miSelectMolecule.Visible = hasAtom;
                if (hasAtom)
                    _miSelectMolecule.Text = $"Select this molecule ({molecule.atom[atomIndex].name})";
            }

            if (hasAtom)
            {
                string atomName = molecule.atom[atomIndex].name;
                _miHideAtom.Text = $"Hide this atom ({atomName})";

                // ⭐ Tag holds current atom index — used in Click handler
                _miHideAtom.Tag = atomIndex;
                _miHideMolecule.Tag = atomIndex;
                _miHideSymmetry.Tag = atomIndex;
                if (_miSelectMolecule != null)
                    _miSelectMolecule.Tag = atomIndex;
            }
            else
            {
                _miHideAtom.Tag = null;
                _miHideMolecule.Tag = null;
                _miHideSymmetry.Tag = null;
                if (_miSelectMolecule != null)
                    _miSelectMolecule.Tag = null;
            }
        }

        #endregion

        #region Actions

        private enum HideKind
        {
            Selected,
            ReverseSelected,
            ShowAll,
            ThisAtom,
            ThisMolecule,
            ThisSymmetry,
            HideShortContacts,
            RemoveHanging
        }

        private void HideAction(HideKind kind, int atomIndex)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null) return;

            int affected = 0;
            string statusMsg = "";

            switch (kind)
            {
                case HideKind.Selected:
                    affected = AtomVisibilityManager.HideSelected(molecule);
                    statusMsg = $"Hid {affected} selected atoms";
                    break;

                case HideKind.ReverseSelected:
                    affected = AtomVisibilityManager.HideReverseSelected(molecule);
                    statusMsg = $"Hid {affected} non-selected atoms";
                    break;

                case HideKind.ShowAll:
                    AtomVisibilityManager.ShowAll(molecule);
                    affected = molecule.NumberOfAtoms;
                    statusMsg = $"Shown all {affected} atoms";
                    break;

                case HideKind.ThisAtom:
                    if (atomIndex >= 0 && atomIndex < molecule.NumberOfAtoms)
                    {
                        string name = molecule.atom[atomIndex].name;
                        AtomVisibilityManager.HideAtom(molecule, atomIndex);
                        statusMsg = $"Hid atom {name}";
                        affected = 1;
                    }
                    break;

                case HideKind.ThisMolecule:
                    if (atomIndex >= 0)
                    {
                        affected = AtomVisibilityManager.HideMolecule(molecule, atomIndex);
                        statusMsg = $"Hid molecule ({affected} atoms)";
                    }
                    break;

                case HideKind.ThisSymmetry:
                    if (atomIndex >= 0)
                    {
                        affected = AtomVisibilityManager.HideSymmetryEquivalent(molecule, atomIndex);
                        statusMsg = $"Hid symmetry equivalent ({affected} atoms)";
                    }
                    break;

                case HideKind.HideShortContacts:
                    AtomVisibilityManager.HideShortContacts(molecule);
                    statusMsg = "Hid short contacts";
                    affected = 1;
                    break;

                case HideKind.RemoveHanging:
                    affected = molecule.RemoveHangingExpandedAtoms();

                    if (affected > 0)
                    {
                        molecule.Calculate_Bonds3D();
                        molecule.Calculate_Hbonds3D();
                        molecule.PrecomputeBondMatrices();
                    }

                    statusMsg = affected > 0
                        ? $"Removed {affected} hanging atoms (kept expanded branches)"
                        : "Nothing to remove — all expanded atoms are protected";
                    break;
            }

 
            SetStatus(statusMsg,
                      $"Structures: {StructureStorage.Instance.StructureCount}",
                      "👁️");

            RefreshAllDocuments();
        }

        #endregion


        /// <summary>
        /// Select all visible atoms belonging to the same chemical molecule
        /// (IndividualMoleculeInCell + cell shift) as the given atom.
        /// </summary>
        public int SelectMoleculeContainingAtom(int atomIndex, bool clearPrevious = false)
        {
            var molecule = GetActiveMolecule();
            if (molecule == null || molecule.atom == null || molecule.NumberOfAtoms == 0)
                return 0;
            if (atomIndex < 0 || atomIndex >= molecule.NumberOfAtoms)
                return 0;

            EnsureMoleculeIdsForSelection(molecule);

            int targetMol = molecule.atom[atomIndex].IndividualMoleculeInCell;
            int cx = molecule.atom[atomIndex].CellShiftX;
            int cy = molecule.atom[atomIndex].CellShiftY;
            int cz = molecule.atom[atomIndex].CellShiftZ;

            if (clearPrevious)
            {
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    molecule.atom[i].selected = false;
                    molecule.atom[i].SelectionOrder = 0;
                }
                molecule.NumberSelected = 0;
            }

            int count = 0;

            if (targetMol < 0)
            {
                if (!molecule.atom[atomIndex].selected)
                {
                    molecule.atom[atomIndex].selected = true;
                    molecule.NumberSelected++;
                    molecule.atom[atomIndex].SelectionOrder = molecule.NumberSelected;
                    count = 1;
                }
                SetStatus($"Selected atom (no molecule id) — {molecule.atom[atomIndex].name}",
                          "Structures: 0", "🧪");
                Flags.Instance.VisualRevision++;
                RefreshActiveDocument();
                return count;
            }

            int orderBase = molecule.NumberSelected;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (!molecule.atom[i].show) continue;
                if (molecule.atom[i].style == 10) continue;
                if (molecule.atom[i].IndividualMoleculeInCell != targetMol) continue;
                if (molecule.atom[i].CellShiftX != cx) continue;
                if (molecule.atom[i].CellShiftY != cy) continue;
                if (molecule.atom[i].CellShiftZ != cz) continue;

                if (!molecule.atom[i].selected)
                {
                    molecule.atom[i].selected = true;
                    orderBase++;
                    molecule.atom[i].SelectionOrder = orderBase;
                    count++;
                }
            }

            molecule.NumberSelected = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
                if (molecule.atom[i].selected) molecule.NumberSelected++;

            SetStatus($"Selected molecule #{targetMol}: {count} new atoms ({molecule.NumberSelected} total)",
                      "Structures: 0", "🧪");
            Flags.Instance.VisualRevision++;
            RefreshActiveDocument();
            return count;
        }

        /// <summary>
        /// Ensure IndividualMoleculeInCell is assigned on 3D atoms (runs detection if needed).
        /// </summary>
        private void EnsureMoleculeIdsForSelection(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null) return;
            bool anySet = false;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].IndividualMoleculeInCell >= 0)
                {
                    anySet = true;
                    break;
                }
            }
            if (anySet) return;

            try
            {
                if (molecule.SymetryEquivalents != null && molecule.SymetryEquivalents.Count > 0)
                    molecule.DetectMoleculesInSymmetryEquivalents();
                else if (molecule.AsymmetricPart != null && molecule.AsymmetricPart.Count > 0)
                    molecule.DetectMolecules(molecule.AsymmetricPart, true);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SelectMolecule] Detect failed: {ex.Message}");
            }
        }

        #region Helpers

        private bool HasAnySelected(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null) return false;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected) return true;
            }
            return false;
        }

        private bool HasAnyContacts(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null) return false;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                // ⭐ Only active kontakty
                if (!molecule.atom[i].UseForShortContacts) continue;

                if (molecule.atom[i].ContactCount > 0) return true;
            }
            return false;
        }

        #endregion
    }
}
