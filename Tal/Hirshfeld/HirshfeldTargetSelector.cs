// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldTargetSelector.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Molecule selection for Hirshfeld analysis and collecting target + neighbour atoms.
 ///
 /// "Target" atoms — belong to the selected molecule in the (0,0,0) cell.
 /// "Neighbour" atoms — all other atoms within R_max + margin of the target centre,
 /// together with their copies from cells ±1 (via symmetry).
 /// </summary>
    public static class HirshfeldTargetSelector
    {
 /// <summary>
 /// Info o molekule do dropdown w GUI.
 /// </summary>
        public class TargetMoleculeInfo
        {
            public int MoleculeId;
            public int AtomCount;
            public List<int> TargetAtomIndices;      // indeksy w molecule.atom[]
            public Vector3 Center;
            public string Label;

            public override string ToString() => Label;
        }

 /// <summary>
 /// Returns list molecules w activem Molecule.
 /// Grupuje after IndividualMoleculeInCell (ignoruje ujemne).
 /// </summary>
        public static List<TargetMoleculeInfo> EnumerateMolecules(Molecule molecule)
        {
            var result = new List<TargetMoleculeInfo>();
            if (molecule == null || molecule.atom == null) return result;

            var dict = new Dictionary<int, TargetMoleculeInfo>();

            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                int mid = molecule.atom[i].IndividualMoleculeInCell;
                if (mid < 0) continue;

                if (!dict.TryGetValue(mid, out var info))
                {
                    info = new TargetMoleculeInfo
                    {
                        MoleculeId = mid,
                        TargetAtomIndices = new List<int>()
                    };
                    dict[mid] = info;
                }

                info.TargetAtomIndices.Add(i);
            }

            foreach (var info in dict.Values)
            {
                info.AtomCount = info.TargetAtomIndices.Count;

                Vector3 sum = Vector3.Zero;
                foreach (var i in info.TargetAtomIndices)
                    sum += molecule.atom[i].Coordinate;
                info.Center = sum / info.AtomCount;

                info.Label = $"Molecule #{info.MoleculeId} ({info.AtomCount} atoms)";
                result.Add(info);
            }

            return result.OrderBy(x => x.MoleculeId).ToList();
        }

 /// <summary>
 /// Returns info o molekule o danym ID (or null).
 /// </summary>
        public static TargetMoleculeInfo GetMoleculeInfo(Molecule molecule, int molId)
        {
            var list = EnumerateMolecules(molecule);
            return list.FirstOrDefault(x => x.MoleculeId == molId);
        }
    }
}