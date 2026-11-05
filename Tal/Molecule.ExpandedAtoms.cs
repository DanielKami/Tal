// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — expanded / packing atoms tracking.

﻿// Molecule.ExpandedAtoms.cs
// Author: Daniel M. Kaminski
// Year: 2026
//
// Molecule partial — selektywne usuwanie atoms z Expand Contacts.
// Preserves the transitive closure from ExpandedSeedAtomIndices.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Tal
{
    public partial class Molecule
    {
        #region Remove Hanging Expanded Atoms (keep seeds + descendants)

 /// <summary>
 /// Usuwa z atom[] only te atoms dodane przez Expand Contacts, which NIE
 /// belong to the transitive closure from ExpandedSeedAtomIndices.
 ///
 /// In other words:
 /// - atoms the user clicked in Expand Contacts mode (seed) — STAY
 /// - their neighbours in the bond graph (descendants) — STAY
 /// - "orphans" — atoms from ExpandedAtomIndices that nobody expanded — REMOVED
 ///
 /// All indices in BondedAtom / H_BondedAtom / ContactAtom are remapped.
 ///
 /// After finishing, clears ExpandedAtomIndices and ExpandedSeedAtomIndices.
 /// </summary>
 /// <returns>Number of removed atoms.</returns>
        public int RemoveHangingExpandedAtoms()
        {
            if (atom == null || NumberOfAtoms == 0) return 0;
            if (ExpandedAtomIndices == null || ExpandedAtomIndices.Count == 0) return 0;

 // ⭐ 1. Zbuduj graf neighbourhood z BondedAtom[] — only between existcymi atomami
            var adjacency = BuildBondAdjacency();

 // 2. Collect "keep" — transitive closure from seeds
            var keep = new HashSet<int>();

            if (ExpandedSeedAtomIndices != null && ExpandedSeedAtomIndices.Count > 0)
            {
                var queue = new Queue<int>();
                foreach (var seed in ExpandedSeedAtomIndices)
                {
                    if (seed >= 0 && seed < NumberOfAtoms && keep.Add(seed))
                        queue.Enqueue(seed);
                }

                while (queue.Count > 0)
                {
                    int cur = queue.Dequeue();
                    if (!adjacency.TryGetValue(cur, out var neighbors)) continue;

                    foreach (var nb in neighbors)
                    {
                        if (keep.Add(nb))
                            queue.Enqueue(nb);
                    }
                }
            }

 // 3. Atoms to remove = ExpandedAtomIndices \ keep
            var toRemove = new HashSet<int>();
            foreach (var idx in ExpandedAtomIndices)
            {
                if (idx < 0 || idx >= NumberOfAtoms) continue;
                if (keep.Contains(idx)) continue;
                toRemove.Add(idx);
            }

            if (toRemove.Count == 0)
            {
                Debug.WriteLine("[RemoveHangingExpandedAtoms] Nothing to remove " +
                                "(all expanded atoms are protected by seeds)");
                return 0;
            }

            Debug.WriteLine($"[RemoveHangingExpandedAtoms] keep={keep.Count}, " +
                            $"expanded={ExpandedAtomIndices.Count}, " +
                            $"remove={toRemove.Count}");

 // 4. Actual removal — identical to RemoveExpandedAtomsPhysically
 // clearExpandedLists = true → clear ExpandedAtomIndices i ExpandedSeedAtomIndices
            return RemoveAtomsInternal(toRemove, clearExpandedLists: true);
        }

        #endregion

        #region Build Bond Adjacency

 /// <summary>
 /// Builds graf neighbourhood (nieskierowany) z currentch bonds kowalencyjnych.
 /// </summary>
        private Dictionary<int, List<int>> BuildBondAdjacency()
        {
            var adj = new Dictionary<int, List<int>>();

            for (int i = 0; i < NumberOfAtoms; i++)
            {
                if (atom[i].BondedAtom == null) continue;

                int n = atom[i].NrBondedAtoms;
                if (n <= 0) continue;

                if (!adj.TryGetValue(i, out var list))
                {
                    list = new List<int>();
                    adj[i] = list;
                }

                for (int j = 0; j < n && j < atom[i].BondedAtom.Length; j++)
                {
                    int t = atom[i].BondedAtom[j];
                    if (t < 0 || t >= NumberOfAtoms) continue;

                    list.Add(t);

 // edge in the other direction
                    if (!adj.TryGetValue(t, out var list2))
                    {
                        list2 = new List<int>();
                        adj[t] = list2;
                    }
                    list2.Add(i);
                }
            }

            return adj;
        }

        #endregion

        #region Remove Atoms Internal (shared by all methods)

 /// <summary>
 /// Fizycznie usuwa afterdane indexy z atom[], przemaafterwuje all
 /// index arrays and compacts them.
 /// </summary>
 /// <param name="toRemove">Set of indices to remove.</param>
 /// <param name="clearExpandedLists">
 /// true — clear ExpandedAtomIndices i ExpandedSeedAtomIndices
 /// (default for Short Contacts).
 /// false — do NOT touch those lists (for H-Bonds, to preserve ExpandedAtomIndices).
 /// </param>
 /// <returns>Number of removed atoms.</returns>
        internal int RemoveAtomsInternal(HashSet<int> toRemove, bool clearExpandedLists = true)
        {
            if (toRemove.Count == 0) return 0;

 // ⭐ 1. Mapa old→new (-1 for usuwanych)
            var oldToNew = new int[NumberOfAtoms];
            int newCount = 0;
            for (int i = 0; i < NumberOfAtoms; i++)
            {
                oldToNew[i] = toRemove.Contains(i) ? -1 : newCount++;
            }

            if (newCount == NumberOfAtoms) return 0;

 // ⭐ 2. Przemapuj i skompaktuj all indexy w zachowanych atomach
            for (int i = 0; i < NumberOfAtoms; i++)
            {
                if (oldToNew[i] < 0) continue;

                RemapIndexArray(ref atom[i].BondedAtom, oldToNew);
                CompactBondArrays(ref atom[i]);

                RemapIndexArray(ref atom[i].H_BondedAtom, oldToNew);
                CompactHBondArrays(ref atom[i]);

                RemapIndexArray(ref atom[i].ContactAtom, oldToNew);
                CompactContactArrays(ref atom[i]);
            }

 // ⭐ 3. Nowa array atom[]
            var newAtoms = new Atom[newCount];
            int write = 0;
            for (int i = 0; i < NumberOfAtoms; i++)
            {
                if (oldToNew[i] < 0) continue;
                newAtoms[write++] = atom[i];
            }

            int removedCount = NumberOfAtoms - newCount;
            atom = newAtoms;
            NumberOfAtoms = newCount;

 // ⭐ 4. Clear list — TYLKO when clearExpandedLists == true.
 // For H-Bonds (false) zachowujemy ExpandedAtomIndices.
            if (clearExpandedLists)
            {
                ExpandedAtomIndices.Clear();
                ExpandedSeedAtomIndices.Clear();
            }

 // ⭐ 5. Przelicz zaznaczenie
            RecalculateSelectionOrder();

            Debug.WriteLine($"[RemoveAtomsInternal] Removed {removedCount} atoms " +
                            $"(new total: {NumberOfAtoms}, clearExpandedLists={clearExpandedLists})");

            return removedCount;
        }

        #endregion

        #region Helpers

        private static void RemapIndexArray(ref int[] indices, int[] oldToNew)
        {
            if (indices == null) return;
            for (int i = 0; i < indices.Length; i++)
            {
                int oldIdx = indices[i];
                if (oldIdx < 0 || oldIdx >= oldToNew.Length)
                    indices[i] = -1;
                else
                    indices[i] = oldToNew[oldIdx];
            }
        }

        private static void CompactBondArrays(ref Atom a)
        {
            if (a.BondedAtom == null) { a.NrBondedAtoms = 0; return; }

            int n = 0;
            for (int i = 0; i < a.BondedAtom.Length; i++)
            {
                if (a.BondedAtom[i] < 0) continue;
                a.BondedAtom[n] = a.BondedAtom[i];
                if (a.Distance != null && i < a.Distance.Length) a.Distance[n] = a.Distance[i];
                if (a.BondOccupancy != null && i < a.BondOccupancy.Length) a.BondOccupancy[n] = a.BondOccupancy[i];
                if (a.AngleZ != null && i < a.AngleZ.Length) a.AngleZ[n] = a.AngleZ[i];
                if (a.AngleXY != null && i < a.AngleXY.Length) a.AngleXY[n] = a.AngleXY[i];
                if (a.PrecomputedBondMatrix != null && i < a.PrecomputedBondMatrix.Length)
                    a.PrecomputedBondMatrix[n] = a.PrecomputedBondMatrix[i];
                n++;
            }

            for (int i = n; i < a.BondedAtom.Length; i++)
                a.BondedAtom[i] = -1;

            a.NrBondedAtoms = n;
        }

        private static void CompactHBondArrays(ref Atom a)
        {
            if (a.H_BondedAtom == null) { a.H_NrBondedAtoms = 0; return; }

            int n = 0;
            for (int i = 0; i < a.H_BondedAtom.Length; i++)
            {
                if (a.H_BondedAtom[i] < 0) continue;
                a.H_BondedAtom[n] = a.H_BondedAtom[i];
                if (a.H_Distance != null && i < a.H_Distance.Length) a.H_Distance[n] = a.H_Distance[i];
                if (a.H_AngleZ != null && i < a.H_AngleZ.Length) a.H_AngleZ[n] = a.H_AngleZ[i];
                if (a.H_AngleXY != null && i < a.H_AngleXY.Length) a.H_AngleXY[n] = a.H_AngleXY[i];
                n++;
            }

            for (int i = n; i < a.H_BondedAtom.Length; i++)
                a.H_BondedAtom[i] = -1;

            a.H_NrBondedAtoms = n;
        }

        private static void CompactContactArrays(ref Atom a)
        {
            if (a.ContactAtom == null) { a.ContactCount = 0; return; }

            int n = 0;
            for (int i = 0; i < a.ContactAtom.Length; i++)
            {
                if (a.ContactAtom[i] < 0) continue;
                a.ContactAtom[n] = a.ContactAtom[i];
                if (a.ContactDistance != null && i < a.ContactDistance.Length) a.ContactDistance[n] = a.ContactDistance[i];
                if (a.ContactAngleZ != null && i < a.ContactAngleZ.Length) a.ContactAngleZ[n] = a.ContactAngleZ[i];
                if (a.ContactAngleXY != null && i < a.ContactAngleXY.Length) a.ContactAngleXY[n] = a.ContactAngleXY[i];
                if (a.PrecomputedContactMatrix != null && i < a.PrecomputedContactMatrix.Length)
                    a.PrecomputedContactMatrix[n] = a.PrecomputedContactMatrix[i];
                if (a.ContactIsNew != null && i < a.ContactIsNew.Length) a.ContactIsNew[n] = a.ContactIsNew[i];
                n++;
            }

            for (int i = n; i < a.ContactAtom.Length; i++)
                a.ContactAtom[i] = -1;

            a.ContactCount = n;
        }

        private void RecalculateSelectionOrder()
        {
            int selected = 0;
            for (int i = 0; i < NumberOfAtoms; i++)
            {
                if (atom[i].selected)
                {
                    selected++;
                    atom[i].SelectionOrder = selected;
                }
                else
                {
                    atom[i].SelectionOrder = 0;
                }
            }
            NumberSelected = selected;
        }

        #endregion
    }
}