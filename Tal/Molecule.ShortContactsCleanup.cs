// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — short-contact cleanup.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — odznaczanie Short Contacts / H-Bonds.
//
// Unchecking the Short Contacts checkbox does EXACTLY the same,
// co menu kontekstowe → "Remove hanging contacts":
// - Physical removal of atoms from ExpandedAtomIndices (and orphans)
// - Remapping of all indices (RemoveAtomsInternal)
// - Recalculation of bonds / H-bonds / matrix

using System.Collections.Generic;
using System.Diagnostics;
using Tal.Calculate;   // ⭐ HBondFinder

namespace Tal
{
    public partial class Molecule
    {
        #region Turn Off Short Contacts (== Remove hanging contacts)

 /// <summary>
 /// ⭐ Odznaczenie checkboxa "Short Contacts".
 ///
 /// Does EXACTLY the same as the context menu → "Remove hanging contacts":
 /// 1. Usuwa fizycznie atoms z ExpandedAtomIndices
 /// (preserves seeds + transitive closure over the bond graph)
 /// 2. Remapuje all indexy (BondedAtom / H_BondedAtom / ContactAtom)
 /// 3. Clears Contact* data on remaining atoms
 /// 4. Clears list HBondNeighborAtomIndices (niecurrent after removal)
 /// 5. Recalculates bonds / H-bonds / matrixe
 /// 6. Setia Flags.Instance.ShortContact = false
 /// </summary>
        public void TurnOffShortContacts()
        {
            Debug.WriteLine("═══════════════════════════════════════════════════════");
            Debug.WriteLine($"[Molecule] TurnOffShortContacts: START " +
                            $"(atoms={NumberOfAtoms}, expanded={ExpandedAtomIndices?.Count ?? 0})");

 // ⭐ 1. Remove fizycznie atoms dodane przez Expand Contacts
 // (to samo co HideKind.RemoveHanging w ContextMenu)
            int removed = RemoveHangingExpandedAtoms();

 // 2. Clear contact data on remaining atoms
            ClearAllContacts();

 // 3. Clear H-bond neighbour lists — after atom removal
 // their indices are stale (may point to -1 or wrong atoms)
            HBondNeighborAtomIndices?.Clear();

 // ⭐ 4. Przelicz bonds / H-bonds / matrixe od new
            if (removed > 0)
            {
                Calculate_Bonds3D();
                Calculate_Hbonds3D();
                PrecomputeBondMatrices();
            }

 // ⭐ 5. Globalna flaga
            Flags.Instance.ShortContact = false;
            Flags.Instance.ShortContactsActive = false;

            Debug.WriteLine($"[Molecule] TurnOffShortContacts: END " +
                            $"(removed={removed}, atoms={NumberOfAtoms})");
            Debug.WriteLine("═══════════════════════════════════════════════════════");
        }

        #endregion

        #region Turn Off H-Bonds

 /// <summary>
 /// ⭐ Odznaczenie checkboxa "H-Bonds".
 ///
 /// Analogscznie do Short Contacts — usuwa fizycznie atoms dodane
 /// przez HBondFinder, clears dane H-bond, recalculates bonds.
 ///
 /// Difference relative to TurnOffShortContacts:
 /// - clearExpandedLists: false → NIE ruszamy ExpandedAtomIndices
 /// (so that Short Contacts can be turned on after H-Bonds)
 /// - HBondNeighborAtomIndices.Clear() after remaafterwaniu
 /// </summary>
        public void TurnOffHBonds()
        {
            Debug.WriteLine(
                $"[Molecule] TurnOffHBonds: START " +
                $"(atoms={NumberOfAtoms}, hBondNeighbors={HBondNeighborAtomIndices?.Count ?? 0})");

 // ⭐ 1. Remove fizycznie atoms dodane przez HBondFinder
            int removed = RemoveHBondNeighborAtomsPhysical();

 // 2. Clear H-bond data on remaining atoms
            HBondFinder.Clear(this);

 // 3. Recalculate bonds from scratch (because atoms disappeared)
            if (removed > 0)
            {
                Calculate_Bonds3D();
                PrecomputeBondMatrices();
            }

 // ⭐ 4. Globalna flaga
            Flags.Instance.ShowHBonds = false;
            Flags.Instance.Hydrogen_bonds1 = false;

            Debug.WriteLine($"[Molecule] TurnOffHBonds: END " +
                            $"(removed={removed}, atoms={NumberOfAtoms})");
        }

 /// <summary>
 /// ⭐ Usuwa fizycznie atoms z HBondNeighborAtomIndices
 /// (analogous to RemoveHangingExpandedAtoms, but for H-bonds).
 /// </summary>
 /// <returns>Number of removed atoms.</returns>
        private int RemoveHBondNeighborAtomsPhysical()
        {
            if (HBondNeighborAtomIndices == null || HBondNeighborAtomIndices.Count == 0)
                return 0;

            // Only delete true H-bond lattice neighbours — never base structure atoms.
            var toRemove = new HashSet<int>();
            foreach (int idx in HBondNeighborAtomIndices)
            {
                if (idx < 0 || idx >= NumberOfAtoms) continue;
                var a = atom[idx];
                bool cellShifted = a.CellShiftX != 0 || a.CellShiftY != 0 || a.CellShiftZ != 0;
                bool expanded = ExpandedAtomIndices != null && ExpandedAtomIndices.Contains(idx);
                // Pure H-bond append: tracked, H-bond only, no covalent network typical of core
                bool pureHBondAppend = a.UseForHBonds && !a.UseForShortContacts && cellShifted;
                // New atom at 000 from neighbour image folded in: no expanded, H-bond only, few bonds
                bool likelyAppended = a.UseForHBonds && !a.UseForShortContacts
                    && !expanded && a.NrBondedAtoms == 0;

                if (cellShifted || pureHBondAppend || likelyAppended)
                    toRemove.Add(idx);
                // Base atoms (000, covalent bonds) are never removed even if mis-listed
            }
            if (toRemove.Count == 0)
            {
                HBondNeighborAtomIndices.Clear();
                return 0;
            }

            int removed = RemoveAtomsInternal(toRemove, clearExpandedLists: false);

            HBondNeighborAtomIndices.Clear();

            return removed;
        }

        #endregion
    }
}