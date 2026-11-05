// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — partial charge assignment.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — obliczenia charges atomowych (SQE, Gasteiger/PEOE)
// together with propagation to 3D atoms and updating the ColorManager range.

using System.Diagnostics;
using Tal.Calculate;
using Tal.Cavity;

namespace Tal
{
    public partial class Molecule
    {
        #region Enum

 /// <summary>Charge calculation method.</summary>
        public enum ChargeMethod
        {
            Gasteiger,
            SQE
        }

        #endregion

        #region Public API

 /// <summary>
 /// Calculates and assigns charges using the specified method.
 /// </summary>
        public void CalculateAndAssignCharges(ChargeMethod method = ChargeMethod.SQE)
        {
            try
            {
                Debug.WriteLine($"[Charges] Calculating charges using {method} method...");

                if (method == ChargeMethod.SQE)
                {
                    var calculator = new SqeChargeCalculator(this);
                    calculator.CalculateSqeChargesForSymmetryEquivalents();
                    UpdateChargeStatistics();

                    if (ChargeMin < ChargeMax)
                    {
                        ColorManager.SetDynamicRange(
                            ColorManager.PropertyType.Charge,
                            ChargeMin,
                            ChargeMax
                        );
                        Cavity.ColorManager.SetDynamicRange(
                            Cavity.ColorManager.PropertyType.Charge,
                            ChargeMin,
                            ChargeMax
                        );
                    }

                    Debug.WriteLine($"[SQE] Charges calculated and mapped successfully.");
                    Debug.WriteLine($"[SQE] Total charge: {TotalCharge:F6}");
                    Debug.WriteLine($"[SQE] Charge range: {ChargeMin:F4} to {ChargeMax:F4}");
                }
                else if (method == ChargeMethod.Gasteiger)
                {
                    var calculator = new PEOEChargeCalculator(this);
                    calculator.CalculateCharges();
                    UpdateChargeStatistics();

                    if (ChargeMin < ChargeMax)
                    {
                        ColorManager.SetDynamicRange(
                            ColorManager.PropertyType.Charge,
                            ChargeMin,
                            ChargeMax
                        );
                        Cavity.ColorManager.SetDynamicRange(
                            Cavity.ColorManager.PropertyType.Charge,
                            ChargeMin,
                            ChargeMax
                        );
                    }

                    Debug.WriteLine($"[PEOE] Charges calculated and mapped successfully.");
                    Debug.WriteLine($"[PEOE] Total charge: {TotalCharge:F6}");
                    Debug.WriteLine($"[PEOE] Charge range: {ChargeMin:F4} to {ChargeMax:F4}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Charges] Error: {ex.Message}");
                SetDefaultCharges();
            }
        }

 /// <summary>
 /// Calculates and assigns SQE charges. Falls back to Gasteiger on error.
 /// </summary>
        public void CalculateAndAssignSqeCharges()
        {
            try
            {
                var calculator = new SqeChargeCalculator(this);
                calculator.CalculateSqeChargesForSymmetryEquivalents();
                UpdateChargeStatistics();

                if (ChargeMin < ChargeMax)
                {
                    Cavity.ColorManager.SetDynamicRange(
                        Cavity.ColorManager.PropertyType.Charge,
                        ChargeMin,
                        ChargeMax
                    );
                }

                Debug.WriteLine($"[SQE] Charges calculated and mapped successfully.");
                Debug.WriteLine($"[SQE] Total charge: {TotalCharge:F6}");
                Debug.WriteLine($"[SQE] Charge range: {ChargeMin:F4} to {ChargeMax:F4}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SQE] Error: {ex.Message}");
                try
                {
                    CalculateAndAssignGasteigerCharges();
                }
                catch (Exception fallbackEx)
                {
                    Debug.WriteLine($"[SQE] Fallback to Gasteiger failed: {fallbackEx.Message}");
                    SetDefaultCharges();
                }
            }
        }

 /// <summary>
 /// Calculates and assigns Gasteiger (PEOE) charges.
 /// </summary>
        public void CalculateAndAssignGasteigerCharges()
        {
            try
            {
                var calculator = new PEOEChargeCalculator(this);
                calculator.CalculateCharges();
                UpdateChargeStatistics();

                if (ChargeMin < ChargeMax)
                {
                    Cavity.ColorManager.SetDynamicRange(
                        Cavity.ColorManager.PropertyType.Charge,
                        ChargeMin,
                        ChargeMax
                    );
                }

                CopyChargesTo3DAtoms();

                Debug.WriteLine($"[PEOE] Charges assigned to {SymetryEquivalents.Count + AsymmetricPart.Count} atoms");
                Debug.WriteLine($"[PEOE] Total charge: {TotalCharge:F6}");
                Debug.WriteLine($"[PEOE] Charge range: {ChargeMin:F4} to {ChargeMax:F4}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PEOE] Error: {ex.Message}");
                SetDefaultCharges();
            }
        }

        #endregion

        #region Statistics Update

 /// <summary>
 /// Updates charge statistics (min/max/total) from symmetry equivalents.
 /// </summary>
        private void UpdateChargeStatistics()
        {
            if (SymetryEquivalents == null || SymetryEquivalents.Count == 0)
                return;

            float total = 0f;
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = 0; i < SymetryEquivalents.Count; i++)
            {
                float charge = SymetryEquivalents[i].charge;
                float occ = SymetryEquivalents[i].occupancy;

                total += charge * occ;
                if (charge < min) min = charge;
                if (charge > max) max = charge;
            }

            TotalCharge = total;
            ChargeMin = min;
            ChargeMax = max;

            if (min < max)
            {
                ColorManager.SetDynamicRange(
                    ColorManager.PropertyType.Charge,
                    min,
                    max
                );
            }
        }

        #endregion

        #region Default Fallback

 /// <summary>
 /// Sets default (zero) charges for all atoms.
 /// </summary>
        private void SetDefaultCharges()
        {
            for (int i = 0; i < SymetryEquivalents.Count; i++)
            {
                var atom = SymetryEquivalents[i];
                atom.charge = 0f;
                SymetryEquivalents[i] = atom;
            }
            for (int i = 0; i < AsymmetricPart.Count; i++)
            {
                var atom = AsymmetricPart[i];
                atom.charge = 0f;
                AsymmetricPart[i] = atom;
            }
            if (atom != null && NumberOfAtoms > 0)
            {
                for (int i = 0; i < NumberOfAtoms; i++)
                {
                    atom[i].charge = 0f;
                }
            }
            ChargeMin = -0.5f;
            ChargeMax = 0.5f;
            TotalCharge = 0f;

            Cavity.ColorManager.SetDynamicRange(
                Cavity.ColorManager.PropertyType.Charge,
                ChargeMin,
                ChargeMax
            );
        }

        #endregion

        #region Propagation to 3D atoms

 /// <summary>
 /// Copies charges from symmetry equivalents to 3D atoms
 /// by matching Cartesian aftersitions.
 /// </summary>
        private void CopyChargesTo3DAtoms()
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
                        atom[i].charge = SymetryEquivalents[j].charge;
                        break;
                    }
                }
            }
        }

        #endregion
    }
}