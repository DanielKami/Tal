// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — crystal planes window and plane selection handling.

﻿using System.Collections.Generic;
using Tal.Core.PowderDiffraction;
using Tal.Forms;

namespace Tal
{
    public partial class MainForm
    {
        #region Planes Management

        private PlanesWindow _planesWindow;
        private static readonly List<CrystalPlane> _emptyPlanes = new List<CrystalPlane>();

        public PlanesWindow GetPlanesWindow()
        {
            var molecule = GetActiveMolecule();
            if (molecule == null)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return null;
            }

            if (_planesWindow == null || _planesWindow.IsDisposed)
            {
                _planesWindow = new PlanesWindow(molecule, molecule.Planes, OnPlanesChanged);
                _planesWindow.FormClosed += (s, e) => _planesWindow = null;
            }
            else
            {
                _planesWindow.UpdateMolecule(molecule);
                _planesWindow.UpdatePlanes(molecule.Planes);
            }

            return _planesWindow;
        }

        private void OnPlanesChanged()
        {
 
            RefreshAllDocuments();
            RefreshMorphologyDocuments();

            var molecule = GetActiveMolecule();
            int count = molecule?.Planes?.Count ?? 0;

            SetStatus($"Planes updated: {count} plane(s)",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "📐");
        }


        /// <summary>
        /// Refreshes PlanesWindow after the active document changes.
        /// Passes the new molecule and its plane list.
        /// </summary>
        public void RefreshPlanesWindowForActiveDocument()
        {
            if (_planesWindow == null || _planesWindow.IsDisposed)
                return;

            if (_planesWindow.IsHidden)
                return;

            var molecule = GetActiveMolecule();
            if (molecule == null)
            {
                // No molecule — clear the list
                _planesWindow.UpdateMolecule(null);
                return;
            }

            _planesWindow.UpdateMolecule(molecule);
            _planesWindow.UpdatePlanes(molecule.Planes);
        }


        /// <summary>
        /// Returns planes aktywnej molecule.
        /// </summary>
        public List<CrystalPlane> GetPlanes()
        {
            return GetActiveMolecule()?.Planes ?? _emptyPlanes;
        }

        public void ClearAllPlanes()
        {
            var molecule = GetActiveMolecule();
            if (molecule == null) return;

            molecule.Planes.Clear();
            RefreshAllDocuments();

            SetStatus("All planes cleared",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🧹");
        }

        #endregion

        /// <summary>
        /// Called by GameWindow when an atom is clicked in Pick Atoms mode.
        /// </summary>
        public bool HandlePlaneSelectionClick(int atomIndex)
        {
            if (atomIndex < 0) return false;

            var planeWindow = GetOpenPlanePropertiesWindow();
            if (planeWindow == null) return false;

            planeWindow.HandleAtomPick(atomIndex);
            return true;
        }

        public PlanePropertiesWindow GetOpenPlanePropertiesWindow()
        {
            foreach (var form in Application.OpenForms)
            {
                if (form is PlanePropertiesWindow p && !p.IsDisposed)
                    return p;
            }
            return null;
        }
    }
}
