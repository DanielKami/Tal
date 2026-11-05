// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal packing controls window.
// Bounds map to Flags.ShiftNegative/Positive{X,Y,Z} as a half-open fractional box
// consumed by Molecule.Calculate_SymmetryAtoms3D_Packing.

﻿using System;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

using Tal.GUI;

namespace Tal.Forms
{
    public partial class PackingWindow : DockContent
    {
        // ============================================================
        // KONSTRUKTOR
        // ============================================================

        public PackingWindow()
        {
            InitializeComponent();
            DockWindowConfig.SetupWindow(this, "📦 Packing");
            // ⭐ LOAD VALUES FROM FLAGS ⭐
            LoadFromFlags();
        }

        // ============================================================
        // EVENT HANDLERS - NUMERIC UPDOWN (all lead to OnValueChanged)
        // ============================================================

        private void NumMinX_ValueChanged(object sender, EventArgs e) => OnValueChanged();
        private void NumMaxX_ValueChanged(object sender, EventArgs e) => OnValueChanged();
        private void NumMinY_ValueChanged(object sender, EventArgs e) => OnValueChanged();
        private void NumMaxY_ValueChanged(object sender, EventArgs e) => OnValueChanged();
        private void NumMinZ_ValueChanged(object sender, EventArgs e) => OnValueChanged();
        private void NumMaxZ_ValueChanged(object sender, EventArgs e) => OnValueChanged();

        // ============================================================
        // EVENT HANDLERS - PRZYCISKI OSI X
        // ============================================================

        private void BtnXPlusHalf_Click(object sender, EventArgs e)
        {
            SetAxisValues("X", 0m, 0.5m);
        }

        private void BtnX0to2_Click(object sender, EventArgs e)
        {
            SetAxisValues("X", 0m, 2m);
        }

        private void BtnX0to3_Click(object sender, EventArgs e)
        {
            SetAxisValues("X", 0m, 3m);
        }

        // ============================================================
        // EVENT HANDLERS - PRZYCISKI OSI Y
        // ============================================================

        private void BtnYPlusHalf_Click(object sender, EventArgs e)
        {
            SetAxisValues("Y", 0m, 0.5m);
        }

        private void BtnY0to2_Click(object sender, EventArgs e)
        {
            SetAxisValues("Y", 0m, 2m);
        }

        private void BtnY0to3_Click(object sender, EventArgs e)
        {
            SetAxisValues("Y", 0m, 3m);
        }

        // ============================================================
        // EVENT HANDLERS - PRZYCISKI OSI Z
        // ============================================================

        private void BtnZPlusHalf_Click(object sender, EventArgs e)
        {
            SetAxisValues("Z", 0m, 0.5m);
        }

        private void BtnZ0to2_Click(object sender, EventArgs e)
        {
            SetAxisValues("Z", 0m, 2m);
        }

        private void BtnZ0to3_Click(object sender, EventArgs e)
        {
            SetAxisValues("Z", 0m, 3m);
        }

        // ============================================================
        // EVENT HANDLERS - PRZYCISKI GLOBALNE
        // ============================================================

        private void BtnAllPlusHalf_Click(object sender, EventArgs e)
        {
            UnsubscribeEvents();

            this.numMinX.Value = 0m;
            this.numMaxX.Value = 0.5m;
            this.numMinY.Value = 0m;
            this.numMaxY.Value = 0.5m;
            this.numMinZ.Value = 0m;
            this.numMaxZ.Value = 0.5m;

            SubscribeEvents();
            OnValueChanged();
        }

        private void BtnAll0to2_Click(object sender, EventArgs e)
        {
            UnsubscribeEvents();

            this.numMinX.Value = 0m;
            this.numMaxX.Value = 2m;
            this.numMinY.Value = 0m;
            this.numMaxY.Value = 2m;
            this.numMinZ.Value = 0m;
            this.numMaxZ.Value = 2m;

            SubscribeEvents();
            OnValueChanged();
        }

        private void BtnAll0to3_Click(object sender, EventArgs e)
        {
            UnsubscribeEvents();

            this.numMinX.Value = 0m;
            this.numMaxX.Value = 3m;
            this.numMinY.Value = 0m;
            this.numMaxY.Value = 3m;
            this.numMinZ.Value = 0m;
            this.numMaxZ.Value = 3m;

            SubscribeEvents();
            OnValueChanged();
        }

        // ============================================================
        // EVENT HANDLERS - RESET
        // ============================================================

        private void BtnReset_Click(object sender, EventArgs e)
        {
            UnsubscribeEvents();

            this.numMinX.Value = 0m;
            this.numMaxX.Value = 1m;
            this.numMinY.Value = 0m;
            this.numMaxY.Value = 1m;
            this.numMinZ.Value = 0m;
            this.numMaxZ.Value = 1m;

            SubscribeEvents();
            OnValueChanged();
        }

        // ============================================================
        // EVENT HANDLERS - CHECKBOX PACKING
        // ============================================================

        private void ChkPacking_CheckedChanged(object sender, EventArgs e)
        {
            Flags.Instance.DrawPacking = chkPacking.Checked;
 

            // ⭐ SYCHRONIZACJA Z DOLNYM PANELEM ⭐
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm != null)
            {
                foreach (var form in Application.OpenForms)
                {
                    if (form is DisplayOptionsWindow displayWin)
                    {
                        displayWin.SetPacking(chkPacking.Checked);
                    }
                }
            }

            // ⭐ REFRESH STRUCTURE ⭐
            RefreshStructure();
        }

        // ============================================================
        // METODY POMOCNICZE
        // ============================================================

        private void SetAxisValues(string axis, decimal minVal, decimal maxVal)
        {
            NumericUpDown numMin = null;
            NumericUpDown numMax = null;

            switch (axis)
            {
                case "X": numMin = this.numMinX; numMax = this.numMaxX; break;
                case "Y": numMin = this.numMinY; numMax = this.numMaxY; break;
                case "Z": numMin = this.numMinZ; numMax = this.numMaxZ; break;
            }

            if (numMin != null && numMax != null)
            {
                UnsubscribeEvents();

                numMin.Value = minVal;
                numMax.Value = maxVal;

                SubscribeEvents();
                OnValueChanged();
            }
        }

        private void UnsubscribeEvents()
        {
            this.numMinX.ValueChanged -= NumMinX_ValueChanged;
            this.numMaxX.ValueChanged -= NumMaxX_ValueChanged;
            this.numMinY.ValueChanged -= NumMinY_ValueChanged;
            this.numMaxY.ValueChanged -= NumMaxY_ValueChanged;
            this.numMinZ.ValueChanged -= NumMinZ_ValueChanged;
            this.numMaxZ.ValueChanged -= NumMaxZ_ValueChanged;
        }

        private void SubscribeEvents()
        {
            this.numMinX.ValueChanged += NumMinX_ValueChanged;
            this.numMaxX.ValueChanged += NumMaxX_ValueChanged;
            this.numMinY.ValueChanged += NumMinY_ValueChanged;
            this.numMaxY.ValueChanged += NumMaxY_ValueChanged;
            this.numMinZ.ValueChanged += NumMinZ_ValueChanged;
            this.numMaxZ.ValueChanged += NumMaxZ_ValueChanged;
        }

        // ⭐ MAIN METHOD - CALLED AFTER EVERY CHANGE ⭐
        // ⭐ In PackingWindow.cs - CHANGE METHOD OnValueChanged()

        private void OnValueChanged()
        {
            // ⭐ 1. SAVE VALUES TO FLAGS ⭐
            Flags.Instance.ShiftNegativeX = (float)this.numMinX.Value;
            Flags.Instance.ShiftPositiveX = (float)this.numMaxX.Value;
            Flags.Instance.ShiftNegativeY = (float)this.numMinY.Value;
            Flags.Instance.ShiftPositiveY = (float)this.numMaxY.Value;
            Flags.Instance.ShiftNegativeZ = (float)this.numMinZ.Value;
            Flags.Instance.ShiftPositiveZ = (float)this.numMaxZ.Value;

            // ⭐ 2. ENABLE PACKING MODE (AUTOMATICALLY) ⭐
            if (!Flags.Instance.DrawPacking)
            {
                Flags.Instance.DrawPacking = true;
                this.chkPacking.Checked = true;
            }

            // ⭐ 3. FORCE REBUILD ⭐
            Flags.Instance.GenerateEquvalents = 2;

            
            // ⭐ 4. REFRESH STRUCTURE (CALL Presentation) ⭐
            RefreshStructure();

            // ⭐ 5. AKTUALIZUJ STATUS ⭐
            string info = $"X: [{Flags.Instance.ShiftNegativeX:F1}, {Flags.Instance.ShiftPositiveX:F1}], ";
            info += $"Y: [{Flags.Instance.ShiftNegativeY:F1}, {Flags.Instance.ShiftPositiveY:F1}], ";
            info += $"Z: [{Flags.Instance.ShiftNegativeZ:F1}, {Flags.Instance.ShiftPositiveZ:F1}]";

            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm != null)
            {
                mainForm.SetStatus($"Packing: {info}", "Structures: 0", "📦");

                // ⭐⭐⭐ REFRESH CAVITIES VIA MAINFORM ⭐⭐⭐
                mainForm.RefreshCavitiesAfterPacking();
            }
            
        }

        // ⭐⭐ NEW METHOD - REFRESHES STRUCTURE BY CALLING Presentation ⭐⭐
        // ⭐ In PackingWindow.cs - CHANGE METHOD RefreshStructure()

        private void RefreshStructure()
        {
            var mainForm = Application.OpenForms["MainForm"] as MainForm;
            if (mainForm == null) return;

            var dockPanel = mainForm.GetDockPanel();
            if (dockPanel == null) return;

            // Find active MonoGameDocument
            foreach (var doc in dockPanel.Documents)
            {
                if (doc is MonoGameDocument monoDoc)
                {
                    // Get molecule
                    var molecule = monoDoc.GetMolecule();
                    if (molecule != null && molecule.NumberOfAtoms > 0)
                    {
                        // ⭐⭐ CALL PRESENTATION WITH NEW FLAGS ⭐⭐
                        int currentStyle = Flags.Instance.Presentation;
                        molecule.Presentation(Flags.Instance.GenerateEquvalents, currentStyle);

                        // Pass the molecule back to the document
                        monoDoc.LoadMolecule(molecule);

                        // ⭐⭐⭐ PRZEBUDUJ JAMY (CAVITIES) ⭐⭐⭐
                        var renderer = monoDoc.GetCavityRenderer();
                        if (renderer != null && renderer.HasCavities)
                        {
                            // ⭐ REBUILD REPEATED CAVITIES WITH NEW PARAMETERS
                            renderer.BuildRepeatedCavities();

                            // ⭐ UPDATE SHIFT (if centring changed)
                            renderer.SetMoleculeShift(molecule.MoleculeShift);

                            System.Diagnostics.Debug.WriteLine($"[PackingWindow] Cavities rebuilt with new packing parameters");
                        }

                        monoDoc.RefreshView();
                        // ⭐ Reset Progressive AA — new contacts (lines) change the 3D scene
                        Flags.Instance.VisualRevision++;
                        Flags.Instance.MarkDirty();
                        System.Diagnostics.Debug.WriteLine($"[PackingWindow] Structure refreshed with new packing parameters");
                        System.Diagnostics.Debug.WriteLine($"  X: [{Flags.Instance.ShiftNegativeX:F1}, {Flags.Instance.ShiftPositiveX:F1}]");
                        System.Diagnostics.Debug.WriteLine($"  Y: [{Flags.Instance.ShiftNegativeY:F1}, {Flags.Instance.ShiftPositiveY:F1}]");
                        System.Diagnostics.Debug.WriteLine($"  Z: [{Flags.Instance.ShiftNegativeZ:F1}, {Flags.Instance.ShiftPositiveZ:F1}]");
                    }
                }
            }
        }

        private void LoadFromFlags()
        {
            UnsubscribeEvents();

            this.numMinX.Value = (decimal)Flags.Instance.ShiftNegativeX;
            this.numMaxX.Value = (decimal)Flags.Instance.ShiftPositiveX;
            this.numMinY.Value = (decimal)Flags.Instance.ShiftNegativeY;
            this.numMaxY.Value = (decimal)Flags.Instance.ShiftPositiveY;
            this.numMinZ.Value = (decimal)Flags.Instance.ShiftNegativeZ;
            this.numMaxZ.Value = (decimal)Flags.Instance.ShiftPositiveZ;
            this.chkPacking.Checked = Flags.Instance.DrawPacking;

            SubscribeEvents();
        }

        /// <summary>
        /// Updates controls na podstawie values z Flags
        /// </summary>
        public void UpdateFromFlags()
        {
            System.Diagnostics.Debug.WriteLine($"[PackingWindow] UpdateFromFlags - synchronizacja z Flags");

            // ⭐ Unsubscribe events during update
            UnsubscribeEvents();

            // ⭐ Update numeric values
            this.numMinX.Value = (decimal)Flags.Instance.ShiftNegativeX;
            this.numMaxX.Value = (decimal)Flags.Instance.ShiftPositiveX;
            this.numMinY.Value = (decimal)Flags.Instance.ShiftNegativeY;
            this.numMaxY.Value = (decimal)Flags.Instance.ShiftPositiveY;
            this.numMinZ.Value = (decimal)Flags.Instance.ShiftNegativeZ;
            this.numMaxZ.Value = (decimal)Flags.Instance.ShiftPositiveZ;

            // ⭐ Zaktualizuj checkbox Packing
            this.chkPacking.Checked = Flags.Instance.DrawPacking;

            // ⭐ Zasubskrybuj zdarzenia z powrotem
            SubscribeEvents();

            System.Diagnostics.Debug.WriteLine($"[PackingWindow] UpdateFromFlags - finished");
            System.Diagnostics.Debug.WriteLine($"  X: [{Flags.Instance.ShiftNegativeX:F1}, {Flags.Instance.ShiftPositiveX:F1}]");
            System.Diagnostics.Debug.WriteLine($"  Y: [{Flags.Instance.ShiftNegativeY:F1}, {Flags.Instance.ShiftPositiveY:F1}]");
            System.Diagnostics.Debug.WriteLine($"  Z: [{Flags.Instance.ShiftNegativeZ:F1}, {Flags.Instance.ShiftPositiveZ:F1}]");
            System.Diagnostics.Debug.WriteLine($"  DrawPacking: {Flags.Instance.DrawPacking}");
        }

        // ============================================================
        // PUBLICZNA METODA DO SYNCHRONIZACJI Z DOLNYM PANELEM
        // ============================================================

        public void SetPackingChecked(bool value)
        {
            if (this.chkPacking != null && this.chkPacking.Checked != value)
            {
                System.Diagnostics.Debug.WriteLine($"[PackingWindow] SetPackingChecked: {value}");

                // ⭐ Unsubscribe event during changes
                this.chkPacking.CheckedChanged -= ChkPacking_CheckedChanged;
                this.chkPacking.Checked = value;
                this.chkPacking.CheckedChanged += ChkPacking_CheckedChanged;

                // ⭐ Update flags
                Flags.Instance.DrawPacking = value;
            }
        }
    }
}