// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — molecule detection within the cell.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — wykrywanie indywidualnych molecules w cell.
//
// Version v2: entire BFS moved to Tal.Calculate.MoleculeFinder.
// Molecule.DetectMolecules jest teraz cienkim wrapperem.
//
// Thanks to this:
// - JEDNO source prawdy for BFS (used also przez Hirshfeld).
// - Consistent bond threshold (Flags.BOND_FACTOR) everywhere.
// - Fractional minimum-image (joins molecules across cell boundary).
// - Full IsHydrogenBond logic with donor search (step 4) —
// identyczna jak w oryginalnym Molecule.IsHydrogenBond.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Tal.Calculate;

namespace Tal
{
    public partial class Molecule
    {
        #region Molecule Detection

 /// <summary>
 /// Detects individual molecules in the given atom list using BFS
 /// over the bonding graph. Sets <c>IndividualMoleculeInCell</c> on each atom.
 ///
 /// ⭐ Deleguje do MoleculeFinder.Find (Tal.Calculate).
 /// Uses fractional minimum-image — molecules across cell boundary
 /// are correctly joined.
 /// donorSearchList: recreates the original Molecule.IsHydrogenBond logic —
 /// AsymmetricPart (if nieempty), inaczej SymetryEquivalents.
 /// </summary>
 /// <param name="atoms">List of atoms (asymmetric or symmetry equivalents).</param>
 /// <param name="resetExisting">If true, resets existing molecule IDs before detection.</param>
 /// <returns>Number of detected molecules.</returns>
        public int DetectMolecules(List<AtomSimple> atoms, bool resetExisting = true)
        {
            if (atoms == null || atoms.Count == 0)
            {
                Debug.WriteLine("[DetectMolecules] Atom list is empty");
                return 0;
            }

            var fracToCart = FractionaltoCarttesianMatrix(0, 0, 0);

 // ⭐ Odtworzenie oryginalnej logski Molecule.IsHydrogenBond:
 // donorSearchList = AsymmetricPart (if nieempty), inaczej SymetryEquivalents
            List<AtomSimple> donorSearchList =
                (AsymmetricPart != null && AsymmetricPart.Count > 0)
                    ? AsymmetricPart
                    : SymetryEquivalents;

            var fragments = MoleculeFinder.Find(
                atoms,
                fracToCart,
                resetExisting,
                donorSearchList,
                useMinimumImage: true,
                log: msg => Debug.WriteLine(msg),
                symmetryOps: SymmetryOperations);

            return fragments.Count;
        }

        #endregion

        #region Convenience Wrappers

 /// <summary>
 /// Runs molecule detection over symmetry equivalents
 /// and propagates the result to the 3D atoms (by aftersition match).
 /// </summary>

        /// <summary>
        /// Detect molecules inside one crystallographic unit cell for energy/force analysis.
        /// Uses covalent connectivity only — <b>no</b> fractional minimum-image and
        /// <b>no</b> symmetry-image bond expansion, so symmetry copies stay separate
        /// molecules (aspirin Z=4 → 4 IDs, ice Z=12 → 12 IDs when bonds resolve).
        /// </summary>
        public int DetectMoleculesInUnitCell(List<AtomSimple> atoms, bool resetExisting = true)
        {
            if (atoms == null || atoms.Count == 0)
            {
                Debug.WriteLine("[DetectMoleculesInUnitCell] empty list");
                return 0;
            }

            var fracToCart = FractionaltoCarttesianMatrix(0, 0, 0);
            var fragments = MoleculeFinder.Find(
                atoms,
                fracToCart,
                resetExisting,
                donorSearchList: atoms,
                useMinimumImage: false,
                log: msg => Debug.WriteLine(msg),
                symmetryOps: null);

            Debug.WriteLine(
                $"[DetectMoleculesInUnitCell] {fragments.Count} molecules in {atoms.Count} atoms");
            return fragments.Count;
        }

        /// <summary>
        /// Fallback when covalent detection finds fewer molecules than expected from
        /// symmetry: assign each (base asym molecule × symmetry op) a unique ID.
        /// Requires <see cref="DetectMolecules"/> on <see cref="AsymmetricPart"/> first
        /// and <c>SymmetryEquivalentNumberInCell</c> = op index on each equivalent.
        /// </summary>
        public int AssignMoleculeIdsBySymmetryOp(List<AtomSimple> equivalents)
        {
            if (equivalents == null || equivalents.Count == 0) return 0;
            if (AsymmetricPart == null || AsymmetricPart.Count == 0) return 0;

            // Base IDs on asymmetric part
            DetectMolecules(AsymmetricPart, true);
            int maxBase = -1;
            foreach (var a in AsymmetricPart)
                if (a.IndividualMoleculeInCell > maxBase)
                    maxBase = a.IndividualMoleculeInCell;
            int nBase = Math.Max(maxBase + 1, 1);

            // Map asym atom index by (name, Z) first occurrence — ambiguous names OK
            // Prefer sequential index: SymetryEquivalents built as for j in Asym for i in Ops
            // So we re-detect base id by matching frac of identity op or name+Z within asym.

            // Build asym molecule id lookup by name|Z (first wins) — weak if duplicate names
            var asymMolByNameZ = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int j = 0; j < AsymmetricPart.Count; j++)
            {
                var a = AsymmetricPart[j];
                string k = $"{a.name}|{a.AtomicNumber}";
                if (!asymMolByNameZ.ContainsKey(k))
                    asymMolByNameZ[k] = a.IndividualMoleculeInCell >= 0 ? a.IndividualMoleculeInCell : 0;
            }

            var used = new HashSet<int>();
            for (int i = 0; i < equivalents.Count; i++)
            {
                var s = equivalents[i];
                string k = $"{s.name}|{s.AtomicNumber}";
                int baseId = asymMolByNameZ.TryGetValue(k, out int b) ? b : 0;
                if (baseId < 0) baseId = 0;
                int op = s.SymmetryEquivalentNumberInCell;
                if (op < 0) op = 0;
                int molId = baseId + op * nBase;
                s.IndividualMoleculeInCell = molId;
                equivalents[i] = s;
                used.Add(molId);
            }

            Debug.WriteLine(
                $"[AssignMoleculeIdsBySymmetryOp] nBase={nBase}, unique mol IDs={used.Count}, atoms={equivalents.Count}");
            return used.Count;
        }

        public void DetectMoleculesInSymmetryEquivalents()
        {
            if (SymetryEquivalents == null || SymetryEquivalents.Count == 0)
                return;

            int moleculeCount = DetectMolecules(SymetryEquivalents, true);

            if (atom != null && NumberOfAtoms > 0)
            {
                for (int i = 0; i < NumberOfAtoms; i++)
                {
                    for (int j = 0; j < SymetryEquivalents.Count; j++)
                    {
                        float dx = Math.Abs(atom[i].Coordinate.X - SymetryEquivalents[j].x);
                        float dy = Math.Abs(atom[i].Coordinate.Y - SymetryEquivalents[j].y);
                        float dz = Math.Abs(atom[i].Coordinate.Z - SymetryEquivalents[j].z);

                        if (dx < 0.01f && dy < 0.01f && dz < 0.01f)
                        {
                            atom[i].IndividualMoleculeInCell = SymetryEquivalents[j].IndividualMoleculeInCell;
                            break;
                        }
                    }
                }
            }
        }

 /// <summary>
 /// Runs molecule detection over the asymmetric part.
 /// </summary>
        public void DetectMoleculesInAsymmetricPart()
        {
            if (AsymmetricPart == null || AsymmetricPart.Count == 0)
                return;

            int moleculeCount = DetectMolecules(AsymmetricPart, true);
            Debug.WriteLine($"[DetectMoleculesInAsymmetricPart] Detected {moleculeCount} molecules in asymmetric part");
        }

 /// <summary>
 /// Propaguje IndividualMoleculeInCell z SymetryEquivalents do AsymmetricPart
 /// przez dopasowanie positions frakcjonalnej (mod 1, minimum image).
 ///
 /// ⭐ KEY: molecule may be generowana przez operation symmetry z part
 /// independent part — only then do SymmetryEquivalents contain the COMPLETE molecule,
 /// a AsymmetricPart only jej fragment. Wykrywanie na SymetryEquivalents daje
 /// correct partition, and this mapping moves it back to the independent part.
 /// </summary>
        public void PropagateMoleculeIdsToAsymmetricPart()
        {
            if (AsymmetricPart == null || AsymmetricPart.Count == 0)
                return;
            if (SymetryEquivalents == null || SymetryEquivalents.Count == 0)
                return;

            float tol = Flags.Instance.MOLECULE_POSITION_TOLERANCE;
            float tolSq = tol * tol;

            int matched = 0;
            int unmatched = 0;

            for (int i = 0; i < AsymmetricPart.Count; i++)
            {
                var asymAtom = AsymmetricPart[i];

                float ax = asymAtom.x - (float)Math.Floor(asymAtom.x);
                float ay = asymAtom.y - (float)Math.Floor(asymAtom.y);
                float az = asymAtom.z - (float)Math.Floor(asymAtom.z);

                int bestMolId = -1;
                float bestDistSq = float.MaxValue;

                for (int j = 0; j < SymetryEquivalents.Count; j++)
                {
                    var symAtom = SymetryEquivalents[j];

                    float sx = symAtom.x - (float)Math.Floor(symAtom.x);
                    float sy = symAtom.y - (float)Math.Floor(symAtom.y);
                    float sz = symAtom.z - (float)Math.Floor(symAtom.z);

                    float dx = ax - sx;
                    float dy = ay - sy;
                    float dz = az - sz;
                    dx -= (float)Math.Round(dx);
                    dy -= (float)Math.Round(dy);
                    dz -= (float)Math.Round(dz);

                    float distSq = dx * dx + dy * dy + dz * dz;

                    if (distSq < bestDistSq && distSq < tolSq)
                    {
                        bestDistSq = distSq;
                        bestMolId = symAtom.IndividualMoleculeInCell;
                    }
                }

                if (bestMolId >= 0)
                {
                    asymAtom.IndividualMoleculeInCell = bestMolId;
                    AsymmetricPart[i] = asymAtom;
                    matched++;
                }
                else
                {
                    unmatched++;
                }
            }

            Debug.WriteLine($"[PropagateMoleculeIds] matched={matched}, " +
                            $"unmatched={unmatched} (of {AsymmetricPart.Count}, tol={tol:F4})");
        }

        #endregion

        #region Statistics & Accessors

 /// <summary>
 /// Returns all atoms from <paramref name="atoms"/> belonging to the given molecule number.
 /// </summary>
        public List<AtomSimple> GetMoleculeAtoms(List<AtomSimple> atoms, int moleculeNumber)
        {
            if (atoms == null)
                return new List<AtomSimple>();

            var result = new List<AtomSimple>();
            for (int i = 0; i < atoms.Count; i++)
            {
                if (atoms[i].IndividualMoleculeInCell == moleculeNumber)
                    result.Add(atoms[i]);
            }
            return result;
        }

 /// <summary>
 /// Returns indices (into <see cref="atom"/>) of all 3D atoms belonging to the given molecule.
 /// </summary>
        public List<int> GetMoleculeAtomIndices3D(int moleculeNumber)
        {
            List<int> result = new List<int>();
            if (atom == null || NumberOfAtoms == 0)
                return result;

            for (int i = 0; i < NumberOfAtoms; i++)
            {
                if (atom[i].IndividualMoleculeInCell == moleculeNumber)
                    result.Add(i);
            }
            return result;
        }

 /// <summary>
 /// Returns the number of distinct molecules in the given atom list.
 /// </summary>
        public int GetMoleculeCount(List<AtomSimple> atoms)
        {
            if (atoms == null || atoms.Count == 0)
                return 0;

            var seen = new HashSet<int>();
            for (int i = 0; i < atoms.Count; i++)
            {
                int id = atoms[i].IndividualMoleculeInCell;
                if (id >= 0) seen.Add(id);
            }
            return seen.Count;
        }

 /// <summary>
 /// Prints molecule statistics for the given atom list to the debug console.
 /// </summary>
        public void PrintMoleculeStatistics(List<AtomSimple> atoms, string listName = "")
        {
            if (atoms == null || atoms.Count == 0)
            {
                Debug.WriteLine($"[PrintMoleculeStatistics] List {listName} is empty");
                return;
            }

            var groups = new Dictionary<int, List<string>>();
            for (int i = 0; i < atoms.Count; i++)
            {
                int id = atoms[i].IndividualMoleculeInCell;
                if (id < 0) continue;

                if (!groups.TryGetValue(id, out var names))
                {
                    names = new List<string>();
                    groups[id] = names;
                }
                names.Add(atoms[i].name);
            }

            Debug.WriteLine($"=== MOLECULE STATISTICS - {listName} ({atoms.Count} atoms) ===");
            foreach (var kvp in groups)
            {
                Debug.WriteLine($"Molecule {kvp.Key}: {kvp.Value.Count} atoms");
            }
            Debug.WriteLine($"Total detected: {groups.Count} individual molecules");
            Debug.WriteLine("==========================================");
        }

 /// <summary>
 /// Prints molecule statistics for symmetry equivalents.
 /// </summary>
        public void PrintMoleculeStatistics()
        {
            PrintMoleculeStatistics(SymetryEquivalents, "SymetryEquivalents");
        }

        #endregion
    }
}