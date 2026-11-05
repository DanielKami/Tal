// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: AtomVisibilityManager.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Collections.Generic;
using System.Diagnostics;
using Tal.Calculate;

namespace Tal.DrawMethods
{
 /// <summary>
 /// Static atom visibility manager. Does not hold state —
 /// operates directly on atom.show flags.
 /// 
 /// Wzorzec analogsczny do ShortContactsFinder / HBondFinder.
 /// </summary>
    public static class AtomVisibilityManager
    {
        #region Hide Operations

 /// <summary>
 /// Ukrywa selected atoms.
 /// </summary>
        public static int HideSelected(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null) return 0;

            int count = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected && molecule.atom[i].show)
                {
                    molecule.atom[i].show = false;
                    count++;
                }
            }
            Debug.WriteLine($"[AtomVisibilityManager] HideSelected: {count} atoms hidden");
            return count;
        }

 /// <summary>
 /// Ukrywa NIEselected atoms (inverse HideSelected).
 /// </summary>
        public static int HideReverseSelected(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null) return 0;

            int count = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (!molecule.atom[i].selected && molecule.atom[i].show)
                {
                    molecule.atom[i].show = false;
                    count++;
                }
            }
            Debug.WriteLine($"[AtomVisibilityManager] HideReverseSelected: {count} atoms hidden");
            return count;
        }

 /// <summary>
 /// Afterkazuje all atoms.
 /// </summary>
        public static void ShowAll(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null) return;

            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                molecule.atom[i].show = true;
            }
            Debug.WriteLine($"[AtomVisibilityManager] ShowAll: {molecule.NumberOfAtoms} atoms shown");
        }

 /// <summary>
 /// Ukrywa afterjedynczy atom.
 /// </summary>
        public static void HideAtom(Molecule molecule, int atomIndex)
        {
            if (molecule == null || molecule.atom == null) return;
            if (atomIndex < 0 || atomIndex >= molecule.NumberOfAtoms) return;

            molecule.atom[atomIndex].show = false;
            Debug.WriteLine($"[AtomVisibilityManager] HideAtom: {molecule.atom[atomIndex].name}");
        }

 /// <summary>
 /// Hides all atoms belonging to the same molecule as the given atom.
 /// Molecule identified by IndividualMoleculeInCell + CellShift.
 /// </summary>
        public static int HideMolecule(Molecule molecule, int atomIndex)
        {
            if (molecule == null || molecule.atom == null) return 0;
            if (atomIndex < 0 || atomIndex >= molecule.NumberOfAtoms) return 0;

            int targetMol = molecule.atom[atomIndex].IndividualMoleculeInCell;
            int targetCellX = molecule.atom[atomIndex].CellShiftX;
            int targetCellY = molecule.atom[atomIndex].CellShiftY;
            int targetCellZ = molecule.atom[atomIndex].CellShiftZ;

            int count = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].IndividualMoleculeInCell == targetMol &&
                    molecule.atom[i].CellShiftX == targetCellX &&
                    molecule.atom[i].CellShiftY == targetCellY &&
                    molecule.atom[i].CellShiftZ == targetCellZ &&
                    molecule.atom[i].show)
                {
                    molecule.atom[i].show = false;
                    count++;
                }
            }
            Debug.WriteLine($"[AtomVisibilityManager] HideMolecule: {count} atoms hidden (mol={targetMol})");
            return count;
        }

 /// <summary>
 /// Ukrywa all atoms z tej samej symmetry equivalent co the given atom.
 /// SymmetryEquivalentNumberInCell identyfikuje operation symmetry.
 /// </summary>
        public static int HideSymmetryEquivalent(Molecule molecule, int atomIndex)
        {
            if (molecule == null || molecule.atom == null) return 0;
            if (atomIndex < 0 || atomIndex >= molecule.NumberOfAtoms) return 0;

            int targetSym = molecule.atom[atomIndex].SymmetryEquivalentNumberInCell;

            int count = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].SymmetryEquivalentNumberInCell == targetSym &&
                    molecule.atom[i].show)
                {
                    molecule.atom[i].show = false;
                    count++;
                }
            }
            Debug.WriteLine($"[AtomVisibilityManager] HideSymmetryEquivalent: {count} atoms hidden (sym={targetSym})");
            return count;
        }

 /// <summary>
 /// Ukrywa all czerwone przerywane linie (Short Contacts).
 /// Nie usuwa atoms — only clears dane Contact*.
 /// </summary>
        public static void HideShortContacts(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null) return;
            ShortContactsFinder.ClearAllContacts(molecule);
            Debug.WriteLine("[AtomVisibilityManager] HideShortContacts: cleared");
        }

 /// <summary>
 /// Removes "hanging" contacts — red lines leading to atoms,
 /// which no longer exist (or are invisible), OR when after removal
 /// zostaje only jeden koniec.
 /// 
 /// NOTE: Short Contacts creates atom→contactAtom pairs. If either
 /// end was hidden/removed, the other side has a "hanging" entry.
 /// This method clears only matching entries.
 /// </summary>
        public static int RemoveHangingContacts(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null) return 0;

            int removed = 0;

            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].ContactCount == 0) continue;

                var oldDistances = molecule.atom[i].ContactDistance;
                var oldAtoms = molecule.atom[i].ContactAtom;
                var oldAnglesZ = molecule.atom[i].ContactAngleZ;
                var oldAnglesXY = molecule.atom[i].ContactAngleXY;
                var oldMatrices = molecule.atom[i].PrecomputedContactMatrix;
                var oldIsNew = molecule.atom[i].ContactIsNew;

                if (oldAtoms == null) continue;

 // Collect "good" contacts — those whose other end exists and is visible
                var goodDistances = new List<float>();
                var goodAtoms = new List<int>();
                var goodAnglesZ = new List<float>();
                var goodAnglesXY = new List<float>();
                var goodMatrices = new List<Microsoft.Xna.Framework.Matrix>();
                var goodIsNew = new List<bool>();

                for (int j = 0; j < molecule.atom[i].ContactCount; j++)
                {
                    int targetIdx = oldAtoms[j];
                    if (targetIdx < 0 || targetIdx >= molecule.NumberOfAtoms)
                    {
                        removed++;
                        continue;
                    }

                    if (!molecule.atom[targetIdx].show)
                    {
                        removed++;
                        continue;
                    }

                    goodDistances.Add(oldDistances[j]);
                    goodAtoms.Add(oldAtoms[j]);
                    goodAnglesZ.Add(oldAnglesZ[j]);
                    goodAnglesXY.Add(oldAnglesXY[j]);
                    if (oldMatrices != null && j < oldMatrices.Length)
                        goodMatrices.Add(oldMatrices[j]);
                    if (oldIsNew != null && j < oldIsNew.Length)
                        goodIsNew.Add(oldIsNew[j]);
                }

                if (goodAtoms.Count == molecule.atom[i].ContactCount)
                    continue;  // nothing removed

 // Save przefiltrowane dane
                molecule.atom[i].ContactDistance = goodDistances.ToArray();
                molecule.atom[i].ContactAtom = goodAtoms.ToArray();
                molecule.atom[i].ContactAngleZ = goodAnglesZ.ToArray();
                molecule.atom[i].ContactAngleXY = goodAnglesXY.ToArray();
                molecule.atom[i].PrecomputedContactMatrix = goodMatrices.ToArray();
                molecule.atom[i].ContactIsNew = goodIsNew.ToArray();
                molecule.atom[i].ContactCount = goodAtoms.Count;
            }

            Debug.WriteLine($"[AtomVisibilityManager] RemoveHangingContacts: removed {removed}");
            return removed;
        }

        #endregion

        #region Statistics

 /// <summary>
 /// Returns ile atoms jest aktualnie visiblech.
 /// </summary>
        public static int CountVisible(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null) return 0;

            int count = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].show) count++;
            }
            return count;
        }

 /// <summary>
 /// Returns ile atoms jest hiddench.
 /// </summary>
        public static int CountHidden(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null) return 0;
            return molecule.NumberOfAtoms - CountVisible(molecule);
        }

        #endregion
    }
}