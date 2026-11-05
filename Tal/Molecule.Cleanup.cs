// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — cleanup helpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — resource cleanup:
// - Clear3DAtomsOnly: resetuje only atoms 3D (nie rusza AsymmetricPart)
// - CleanupBeforeNewLoad: full reset before loading a new structure

using Microsoft.Xna.Framework;
using System.Diagnostics;
using Tal.Calculate;

namespace Tal
{
    public partial class Molecule
    {
        #region Clear 3D Atoms Only

 /// <summary>
 /// Clears only 3D atoms — does NOT touch AsymmetricPart and SymmetryOperations.
 /// ⭐ Also clears ExpandedAtomIndices and HBondNeighborAtomIndices.
 /// </summary>
        public void Clear3DAtomsOnly()
        {
            Debug.WriteLine($"[Molecule] Clear3DAtomsOnly - START (atoms: {NumberOfAtoms})");

            if (atom != null)
            {
                for (int i = 0; i < atom.Length; i++)
                {
                    NullAtomArrays(ref atom[i]);
                    atom[i].show_label = false;
                }
            }

            atom = null;
            NumberOfAtoms = 0;
            NumberSelected = 0;
            LabelOffsets = null;
            AxisLabelOffsets = null;
            Planes?.Clear();
            SymetryEquivalents?.Clear();
            UnitCellFrame?.Clear();

 // ⭐ Reset flag normalisation wodoru
            HydrogenNormalizationApplied = false;
            HydrogenNormalizationDisabledByUser = false;

            HBondNeighborAtomIndices?.Clear();
            ExpandedAtomIndices?.Clear();

            Debug.WriteLine($"[Molecule] Clear3DAtomsOnly - END (AsymmetricPart intact: {AsymmetricPart.Count})");
        }

        #endregion

        #region Cleanup Before New Load

 /// <summary>
 /// Performs complete cleanup before loading new data.
 /// </summary>
        public void CleanupBeforeNewLoad()
        {
            try
            {
                if (atom != null)
                {
                    for (int i = 0; i < atom.Length; i++)
                    {
                        NullAtomArrays(ref atom[i]);

                        atom[i].charge = 0;
                        atom[i].Hydrophobicity = 0;
                        atom[i].isAnisotropic = false;
                        atom[i].Ue = Matrix.Identity;
                        atom[i].Un = Matrix.Identity;
                        atom[i].Transformation = Matrix.Identity;
                        atom[i].CustomColor = new Vector3(-1f, -1f, -1f);
                        atom[i].show_label = false;
                    }
                }

                Planes?.Clear();
                AsymmetricPart?.Clear();
                SymetryEquivalents?.Clear();
                UnitCellFrame?.Clear();
                SymmetryOperations?.Clear();

                if (atom != null)
                    Array.Clear(atom, 0, atom.Length);

                atom = null;
                NumberOfAtoms = 0;
                NumberSelected = 0;
                LabelOffsets = null;
            AxisLabelOffsets = null;

 // ⭐ Reset refinement / experiment metadata
                ClearMetadata();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cleanup error: {ex.Message}");
            }

 // ⭐ Reset flag normalisation wodoru
            HydrogenNormalizationApplied = false;
            HydrogenNormalizationDisabledByUser = false;
            HydrogenNormalizer.ClearCache(this);


            HBondNeighborAtomIndices?.Clear();
            ExpandedAtomIndices?.Clear();
        }

        #endregion

        #region Helpers

 /// <summary>
 /// Nulluje all tablice per-atomowe (bondy, H-bondy, contacts).
 /// Called z obu metod czyszczenia.
 /// </summary>
        private static void NullAtomArrays(ref Atom a)
        {
 // ── Bondy ──
            a.Distance = null;
            a.BondOccupancy = null;
            a.BondedAtom = null;
            a.AngleZ = null;
            a.AngleXY = null;
            a.PrecomputedBondMatrix = null;
            a.PrecomputedHBondMatrix = null;

 // ── H-bondy (bazowe) ──
            a.H_Distance = null;
            a.H_AngleZ = null;
            a.H_AngleXY = null;
            a.H_BondedAtom = null;

 // ── H-bondy (rozszerzone) ──
            a.H_HydrogenAtom = null;
            a.H_DonorAcceptorDistance = null;
            a.H_HydrogenAcceptorDistance = null;
            a.H_DHA_Angle = null;
            a.H_AccCellX = null;
            a.H_AccCellY = null;
            a.H_AccCellZ = null;
            a.H_AccSymmOpIndex = null;
            a.H_AccName = null;
            a.H_AccCartPos = null;
            a.H_AccAtomicNumber = null;
            a.H_AccMoleculeId = null;

 // ── Kontakty (Expand Contacts) ──
            a.ContactDistance = null;
            a.ContactAtom = null;
            a.ContactAngleZ = null;
            a.ContactAngleXY = null;
            a.PrecomputedContactMatrix = null;
            a.ContactIsNew = null;
        }

        #endregion
    }
}
