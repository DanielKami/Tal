// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Atom colour helpers — apply modes, convert colours, preview.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Tal.Forms
{
    /// <summary>
    /// Atom colouring logic: conversions, applying mode, preview.
    /// Extracted from AtomColorWindow.cs to separate UI from logic.
    /// </summary>
    public partial class AtomColorWindow
    {
        #region Color Conversion

        /// <summary>
        /// Konwertuje System.Drawing.Color na Vector3 (zakres 0-1).
        /// </summary>
        private Vector3 SystemColorToVector3(System.Drawing.Color color)
        {
            float r = MathHelper.Clamp(color.R / 255f, 0f, 1f);
            float g = MathHelper.Clamp(color.G / 255f, 0f, 1f);
            float b = MathHelper.Clamp(color.B / 255f, 0f, 1f);
            return new Vector3(r, g, b);
        }

        /// <summary>
        /// Updates the Pick Color button appearance (background colour + text).
        /// </summary>
        private void UpdateColorButton()
        {
            try
            {
                float r = MathHelper.Clamp(_selectedIndividualColor.X, 0f, 1f);
                float g = MathHelper.Clamp(_selectedIndividualColor.Y, 0f, 1f);
                float b = MathHelper.Clamp(_selectedIndividualColor.Z, 0f, 1f);

                int red = (int)Math.Round(r * 255);
                int green = (int)Math.Round(g * 255);
                int blue = (int)Math.Round(b * 255);

                red = Math.Max(0, Math.Min(255, red));
                green = Math.Max(0, Math.Min(255, green));
                blue = Math.Max(0, Math.Min(255, blue));

                System.Drawing.Color color = System.Drawing.Color.FromArgb(255, red, green, blue);
                _btnPickColor.BackColor = color;
                _btnPickColor.Text = "";
                _btnPickColor.Image = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AtomColorWindow] UpdateColorButton error: {ex.Message}");
                _btnPickColor.BackColor = System.Drawing.Color.Red;
                _btnPickColor.Text = "Pick Color";
            }
        }

        #endregion

        #region Color Mode Application

        /// <summary>
        /// Applies the selected colouring mode to the active molecule.
        /// </summary>
        private void ApplyColorMode()
        {
            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm == null) return;

            var molecule = mainForm.GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                _lblStatus.Text = "No molecule loaded";
                return;
            }

            int mode = GetSelectedMode();

            // ⭐ TRYB INDIVIDUAL (4)
            if (mode == 4)
            {
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    molecule.atom[i].CustomColor = new Vector3(-1f, -1f, -1f);
                }
                Flags.Instance.ColorMode = mode;
                mainForm.RefreshAllDocuments();
                _lblStatus.Text = "Individual mode - select atoms and pick color";
                return;
            }

            // ⭐ TRYB CHARGE (5)
            if (mode == 5)
            {
                Tal.Cavity.ColorManager.SetDynamicRange(
                    Tal.Cavity.ColorManager.PropertyType.Charge,
                    molecule.ChargeMin, molecule.ChargeMax
                );
                Flags.Instance.ColorMode = mode;
                mainForm.RefreshAllDocuments();
                _lblStatus.Text = $"Applied Charge coloring (range: {molecule.ChargeMin:F2} to {molecule.ChargeMax:F2})";
                return;
            }

            // ⭐ TRYB HYDROPHOBICITY (6)
            if (mode == 6)
            {
                Tal.Cavity.ColorManager.SetDynamicRange(
                    Tal.Cavity.ColorManager.PropertyType.Hydrophobicity,
                    molecule.HydrophobicityMin, molecule.HydrophobicityMax
                );
                Flags.Instance.ColorMode = mode;
                mainForm.RefreshAllDocuments();
                _lblStatus.Text = $"Applied Hydrophobicity coloring (range: {molecule.HydrophobicityMin:F2} to {molecule.HydrophobicityMax:F2})";
                return;
            }

            // ⭐ OTHER MODES — clear CustomColor
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                molecule.atom[i].CustomColor = new Vector3(-1f, -1f, -1f);
            }

            Flags.Instance.ColorMode = mode;
            mainForm.RefreshAllDocuments();

            string modeName = GetModeName(mode);
            _lblStatus.Text = $"Applied {modeName} to {molecule.NumberOfAtoms} atoms";
        }

        /// <summary>
        /// Applies the selected individual colour to selected atoms.
        /// </summary>
        private void ApplyIndividualColor()
        {
            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm == null) return;

            var molecule = mainForm.GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                _lblStatus.Text = "No molecule loaded";
                return;
            }

            bool hasSelection = false;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected)
                {
                    hasSelection = true;
                    break;
                }
            }

            if (!hasSelection)
            {
                _lblStatus.Text = "No atoms selected - select atoms first";
                return;
            }

            // ⭐ APPLY Vector3 TO SELECTED ATOMS
            int appliedCount = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected)
                {
                    molecule.atom[i].CustomColor = _selectedIndividualColor;
                    appliedCount++;
                }
            }

            // ⭐ ODZNACZ ALL ATOMY
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                molecule.atom[i].selected = false;
            }
            molecule.NumberSelected = 0;

            Flags.Instance.ColorMode = 4;
            mainForm.RefreshAllDocuments();
         
            _lblStatus.Text = $"Applied individual color to {appliedCount} atoms (selection cleared)";
        }

        #endregion

        #region Mode Helpers

        private int GetSelectedMode()
        {
            if (_radioAtomicNumber.Checked) return 0;
            if (_radioSymmetry.Checked) return 1;
            if (_radioMolecule.Checked) return 2;
            if (_radioSymmetryMolecule.Checked) return 3;
            if (_radioIndividual.Checked) return 4;
            if (_radioChargeColor.Checked) return 5;
            if (_radioHydrophobicityColor.Checked) return 6;
            return 0;
        }

        private string GetModeName(int mode)
        {
            switch (mode)
            {
                case 0: return "Atomic Number";
                case 1: return "Symmetry";
                case 2: return "Molecule";
                case 3: return "Symmetry+Molecule";
                case 4: return "Individual";
                case 5: return "Charge";
                case 6: return "Hydrophobicity";
                default: return "Unknown";
            }
        }

        #endregion

        #region Preview / Status

        /// <summary>
        /// Updates tekst statusu na podstawie aktualnego trybu.
        /// </summary>
        private void UpdatePreview()
        {
            var mainForm = Application.OpenForms.OfType<MainForm>().FirstOrDefault();
            if (mainForm == null) return;

            var molecule = mainForm.GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                _lblStatus.Text = "No molecule loaded";
                return;
            }

            int mode = GetSelectedMode();

            if (mode == 4)
            {
                bool hasSelection = false;
                int selectedCount = 0;
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    if (molecule.atom[i].selected)
                    {
                        hasSelection = true;
                        selectedCount++;
                    }
                }

                if (hasSelection)
                {
                    _lblStatus.Text = $"Individual: {selectedCount} atoms selected - click Pick Color";
                }
                else
                {
                    _lblStatus.Text = "Individual: select atoms then click Pick Color";
                }
                return;
            }

            string modeName = GetModeName(mode);
            _lblStatus.Text = $"Mode: {modeName} - Applied to all {molecule.NumberOfAtoms} atoms";
        }

        #endregion
    }
}