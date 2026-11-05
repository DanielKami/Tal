// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — hydrophobicity calculation.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — obliczenia hydrophobicity atoms
// together with propagation to 3D atoms and updating the ColorManager range.

using System.Diagnostics;
using Tal.Calculate;
using Tal.Cavity;

namespace Tal
{
    public partial class Molecule
    {
        #region Public API

 /// <summary>
 /// Calculates and assigns hydrophobicity values.
 /// </summary>
        public void CalculateAndAssignHydrophobicity()
        {
            try
            {
                var calculator = new HydrophobicityCalculator(this);
                calculator.Calculate();

                if (HydrophobicityMin < HydrophobicityMax)
                {
                    ColorManager.SetDynamicRange(
                        ColorManager.PropertyType.Hydrophobicity,
                        HydrophobicityMin,
                        HydrophobicityMax
                    );
                    Cavity.ColorManager.SetDynamicRange(
                        Cavity.ColorManager.PropertyType.Hydrophobicity,
                        HydrophobicityMin,
                        HydrophobicityMax
                    );
                }

                CopyHydrophobicityTo3DAtoms();

                Debug.WriteLine($"[Hydrophobicity] Assigned hydrophobicity values to {SymetryEquivalents.Count + AsymmetricPart.Count} atoms");
                Debug.WriteLine($"[Hydrophobicity] Range: {HydrophobicityMin:F4} to {HydrophobicityMax:F4}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Hydrophobicity] Error: {ex.Message}");
                SetDefaultHydrophobicity();
            }
        }

        #endregion

        #region Default Fallback

 /// <summary>
 /// Sets default hydrophobicity values based on atomic number.
 /// </summary>
        private void SetDefaultHydrophobicity()
        {
            for (int i = 0; i < SymetryEquivalents.Count; i++)
            {
                var atom = SymetryEquivalents[i];
                atom.Hydrophobicity = HydrophobicityCalculator.GetHydrophobicity(atom.AtomicNumber);
                SymetryEquivalents[i] = atom;
            }

            for (int i = 0; i < AsymmetricPart.Count; i++)
            {
                var atom = AsymmetricPart[i];
                atom.Hydrophobicity = HydrophobicityCalculator.GetHydrophobicity(atom.AtomicNumber);
                AsymmetricPart[i] = atom;
            }

            HydrophobicityMin = -0.5f;
            HydrophobicityMax = 0.73f;

            Cavity.ColorManager.SetDynamicRange(
                Cavity.ColorManager.PropertyType.Hydrophobicity,
                HydrophobicityMin,
                HydrophobicityMax
            );
        }

        #endregion

        #region Propagation to 3D atoms

 /// <summary>
 /// Copies hydrophobicity from symmetry equivalents to 3D atoms
 /// by matching Cartesian aftersitions.
 /// </summary>
        private void CopyHydrophobicityTo3DAtoms()
        {
            if (atom == null || NumberOfAtoms == 0 || SymetryEquivalents == null || SymetryEquivalents.Count == 0)
                return;

            for (int i = 0; i < NumberOfAtoms; i++)
            {
                for (int j = 0; j < SymetryEquivalents.Count; j++)
                {
                    float dx = Math.Abs(atom[i].Coordinate.X - SymetryEquivalents[j].x);
                    float dy = Math.Abs(atom[i].Coordinate.Y - SymetryEquivalents[j].y);
                    float dz = Math.Abs(atom[i].Coordinate.Z - SymetryEquivalents[j].z);

                    if (dx < 0.01f && dy < 0.01f && dz < 0.01f)
                    {
                        atom[i].Hydrophobicity = SymetryEquivalents[j].Hydrophobicity;
                        break;
                    }
                }
            }
        }

        #endregion
    }
}