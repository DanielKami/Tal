// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — short contacts / expand contacts orchestration.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Tal.Calculate;
using Tal.GUI;

namespace Tal
{
    public partial class MainForm
    {
        #region Short Contacts / Expand Contacts

 
        /// Finds all short contacts in the active molecule.
        /// All calculation logic lives in ShortContactsFinder.
        ///
        /// ⭐ After completion deselect all atoms — Find Short Contacts
        ///    must not leave yellow (selected) atoms.
        /// </summary>
        public void FindAllShortContacts()
        {
            var molecule = GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            // Menu / Apply path must match Display Options checkbox
            Flags.Instance.ShortContact = true;
            Flags.Instance.ShortContactsActive = true;
            SyncShortContactCheckbox(true);

            // Ensure molecules are detected (needed for intra/inter filter)
            if (molecule.SymetryEquivalents == null || molecule.SymetryEquivalents.Count == 0)
            {
                molecule.CalculateSymmetryEquivalents();
            }

            molecule.DetectMoleculesInSymmetryEquivalents();

            // ⭐ All logic in the finder
            molecule.UpdateShortContacts();

            // Recalculate bonds and H-bonds from scratch
            molecule.Calculate_Bonds3D();
            molecule.Calculate_Hbonds3D();
            molecule.PrecomputeBondMatrices();

            // ⭐ Deselect all — Find Short Contacts must not leave yellow atoms
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                molecule.atom[i].selected = false;
                molecule.atom[i].SelectionOrder = 0;
            }
            molecule.NumberSelected = 0;

     

            RefreshActiveDocument();

            // Podlicz kontakty do statusu
            // ⭐ Licz only active kontakty (UseForShortContacts == true)
            int totalContacts = 0;
            int atomsWithContacts = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (!molecule.atom[i].UseForShortContacts) continue;

                if (molecule.atom[i].ContactCount > 0)
                {
                    totalContacts += molecule.atom[i].ContactCount;
                    atomsWithContacts++;
                }
            }

            SetStatus($"Short Contacts: {totalContacts} contacts in {atomsWithContacts} atoms (red dashed lines)",
                      $"Structures: {StructureStorage.Instance.StructureCount}",
                      totalContacts > 0 ? "🔗" : "ℹ️");
        }

        /// <summary>
        /// Sync Display Options "Short Contact" checkbox without re-entering calculation logic.
        /// </summary>
        private void SyncShortContactCheckbox(bool on)
        {
            try
            {
                var disp = GetDisplayOptionsWindow();
                if (disp == null) return;
                disp.SetShortContactSilent(on);
            }
            catch
            {
                // Display Options may not be open yet
            }
        }

        /// <summary>
        /// Clears all short-contact data on the active molecule.
        /// Deleguje do Molecule.ClearAllContacts() → ShortContactsFinder.
        /// </summary>
        public void ClearAllContacts()
        {
            var molecule = GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0) return;

            molecule.ClearAllContacts();
 
            RefreshActiveDocument();
            SetStatus("All contacts cleared",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "🧹");
        }

        /// <summary>
        /// Clears selection and contacts on ALL open documents.
        /// ⭐ NEW: also clears the UseForShortContacts flags and ExpandedAtomIndices
        ///         (so no "ghost" atoms from old expansions remain in atom[]).
        /// </summary>
        public void ClearAllSelection()
        {
            var dockPanel = GetDockPanel();
            if (dockPanel == null) return;

            bool anyCleared = false;

            foreach (var doc in dockPanel.Documents)
            {
                if (doc is MonoGameDocument monoDoc)
                {
                    var molecule = monoDoc.GetMolecule();
                    if (molecule == null || molecule.NumberOfAtoms == 0) continue;

                    int clearedCount = 0;
                    for (int i = 0; i < molecule.NumberOfAtoms; i++)
                    {
                        if (molecule.atom[i].selected)
                        {
                            molecule.atom[i].selected = false;
                            molecule.atom[i].SelectionOrder = 0;
                            clearedCount++;
                        }
                    }
                    molecule.NumberSelected = 0;

                    // ⭐ NEW: RemoveExpandedAtoms clears the UseForShortContacts flags
                    //         and clears ExpandedAtomIndices, as well as (via ClearAllContacts)
                    //         clears Contact* data on all atoms.
                    molecule.RemoveExpandedAtoms();

                    if (clearedCount > 0)
                    {
                        anyCleared = true;
                        monoDoc.RefreshView();
                    }
                }
            }

            Lasso.VertexList.Clear();
            var camera = GetActiveCamera();
            if (camera != null)
            {
                camera.flag_lasso = false;
                camera.flag_release = false;
                camera.flag_selected = false;
            }


    


            SetStatus(anyCleared ? "Cleared all selections and contacts" : "No atoms were selected",
                      $"Structures: {StructureStorage.Instance.StructureCount}",
                      anyCleared ? "🧹" : "ℹ️");

            RefreshAllDocuments();
        }

        #endregion

        #region Expand Contacts

        /// <summary>
        /// Handles an atom click in Expand Contacts mode.
        /// Finds symmetry neighbours and adds them to the molecule.
        ///
        /// ⭐ NEW: every clicked atom (seed) is recorded in
        ///          molecule.ExpandedSeedAtomIndices — so that
        ///          "Remove hanging contacts" keeps it and its entire
        ///          offspring (transitive closure over the bond graph).
        ///
        /// ⭐ NEW: after completion deselect all atoms — Expand Contacts
        ///          must not leave yellow (selected) atoms.
        /// </summary>
        public void HandleExpandContactsClick(System.Drawing.Point mousePosition,
                                               int viewportWidth,
                                               int viewportHeight)
        {
            var molecule = GetActiveMolecule();
            var camera = GetActiveCamera();
            var doc = GetActiveDocument();

            if (molecule == null || camera == null || doc == null)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            try
            {
                int atomIndex = FindNearestAtomAtPosition(
                    mousePosition, viewportWidth, viewportHeight, camera, molecule, 20);

                if (atomIndex < 0)
                {
                    SetStatus("No atom found at this position - click on an atom",
                              "Structures: 0", "⚠️");
                    return;
                }

                // ═══════════════════════════════════════════════════════════
                // ⭐ SEED — remember the clicked atom as "protected".
                //    RemoveHangingExpandedAtoms keeps it together with its entire
                //    transitive closure over the bond graph.
                // ═══════════════════════════════════════════════════════════
                if (!molecule.ExpandedSeedAtomIndices.Contains(atomIndex))
                {
                    molecule.ExpandedSeedAtomIndices.Add(atomIndex);
                    System.Diagnostics.Debug.WriteLine(
                        $"[ExpandContacts] Seed registered: atom[{atomIndex}] " +
                        $"{molecule.atom[atomIndex].name}");
                }

                Vector3 selectedPos = molecule.atom[atomIndex].Coordinate;

                // ═══════════════════════════════════════════════════════════
                // Mode "Select Whole Molecule" — expand selection to the whole molecule
                // ═══════════════════════════════════════════════════════════
                if (Flags.Instance.ShortContactsSelectWholeMolecule)
                {
                    HandleSelectWholeMolecule(molecule);
                    return;
                }

                // ═══════════════════════════════════════════════════════════
                // Standard Expand Contacts — find neighbours
                // ═══════════════════════════════════════════════════════════
                var nearbyAtoms = molecule.FindNearbyAtomsFromSymmetry(
                    selectedPos, atomIndex,
                    filterIntraInter: false,   // Expand Contacts: show all
                    skipCellZero: true);        // but skip the parent cell

                if (nearbyAtoms.Count == 0)
                {
                    SetStatus($"Expand Contacts: No new atoms found nearby " +
                              $"(min distance: {Flags.Instance.ExpandContactsMinDistance} Å)",
                              $"Structures: {StructureStorage.Instance.StructureCount}", "ℹ️");
                    return;
                }

                // Add atoms + build contact matrices
                int oldCount = molecule.NumberOfAtoms;
                int addedCount = ShortContactsFinder.AddNearbyAtomsInternal(molecule, nearbyAtoms);

                if (addedCount == 0)
                {
                    SetStatus("Expand Contacts: No new atoms added",
                              $"Structures: {StructureStorage.Instance.StructureCount}", "ℹ️");
                    return;
                }

                BuildContactMatricesForAtom(molecule, atomIndex, oldCount, selectedPos);

                // Recalculate wszystko od new
                molecule.Calculate_Bonds3D();
                molecule.Calculate_Hbonds3D();
                molecule.PrecomputeBondMatrices();

                // ⭐ Deselect all — Expand Contacts must not leave yellow atoms
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    molecule.atom[i].selected = false;
                    molecule.atom[i].SelectionOrder = 0;
                }
                molecule.NumberSelected = 0;
                // ⭐ Reset Progressive AA — new contacts (lines) change the 3D scene
                Flags.Instance.VisualRevision++;
                Flags.Instance.MarkDirty();
                doc.RefreshView();

                SetStatus($"Expand Contacts: Added {addedCount} new atoms " +
                          $"(red dashed lines show contacts)",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "🔗");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", "Structures: 0", "❌");
            }
        }

        /// <summary>
        /// Expands selection to the whole molecule that contains the clicked atom.
        /// </summary>
        private void HandleSelectWholeMolecule(Molecule molecule)
        {
            bool hasSelection = false;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected) { hasSelection = true; break; }
            }

            if (hasSelection)
            {
                // Expand to all molecules that contain the selected atoms
                var selectedMoleculeNumbers = new HashSet<int>();
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    if (molecule.atom[i].selected)
                        selectedMoleculeNumbers.Add(molecule.atom[i].IndividualMoleculeInCell);
                }

                int selectedCount = 0;
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    if (selectedMoleculeNumbers.Contains(molecule.atom[i].IndividualMoleculeInCell))
                    {
                        molecule.atom[i].selected = true;
                        selectedCount++;
                    }
                }
                molecule.NumberSelected = selectedCount;

                SetStatus($"Selection expanded to {selectedMoleculeNumbers.Count} molecules " +
                          $"- {selectedCount} atoms (Select Whole Molecule ON)",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "🧪");
            }
            else
            {
                // Select wszystko
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                    molecule.atom[i].selected = true;
                molecule.NumberSelected = molecule.NumberOfAtoms;

                SetStatus($"Selected all atoms - {molecule.NumberOfAtoms} atoms " +
                          $"(Select Whole Molecule ON)",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "🧪");
            }
            // ⭐ Reset Progressive AA — new contacts (lines) change the 3D scene
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
            RefreshActiveDocument();
        }

        /// <summary>
        /// Builds contact drawing matrices for a single atom.
        /// Used via HandleExpandContactsClick (one-shot expansion),
        /// podczas when ShortContactsFinder.Calculate robi to samo for all atoms.
        /// </summary>
        private void BuildContactMatricesForAtom(Molecule molecule, int atomIdx,
                                                  int oldCount, Vector3 selectedPos)
        {
            int added = molecule.NumberOfAtoms - oldCount;
            if (added <= 0) return;

            molecule.atom[atomIdx].ContactDistance = new float[added];
            molecule.atom[atomIdx].ContactAtom = new int[added];
            molecule.atom[atomIdx].ContactAngleZ = new float[added];
            molecule.atom[atomIdx].ContactAngleXY = new float[added];
            molecule.atom[atomIdx].PrecomputedContactMatrix = new Matrix[added];
            molecule.atom[atomIdx].ContactIsNew = new bool[added];

            int contactCount = 0;
            for (int i = oldCount; i < molecule.NumberOfAtoms; i++)
            {
                Vector3 newPos = molecule.atom[i].Coordinate;
                float distance = Vector3.Distance(selectedPos, newPos);

                if (distance < 1e-4f) continue;

                molecule.atom[atomIdx].ContactDistance[contactCount] = distance;
                molecule.atom[atomIdx].ContactAtom[contactCount] = i;
                molecule.atom[atomIdx].ContactIsNew[contactCount] = true;

                molecule.atom[atomIdx].ContactAngleZ[contactCount] = (float)Math.Asin(
                    Math.Clamp((selectedPos.Z - newPos.Z) / distance, -1f, 1f));

                if (Math.Abs(selectedPos.X - newPos.X) < 1e-6f)
                {
                    molecule.atom[atomIdx].ContactAngleXY[contactCount] = 0f;
                }
                else
                {
                    molecule.atom[atomIdx].ContactAngleXY[contactCount] =
                        -(float)(MathHelperTal.ToRadians(180) -
                                 Math.Atan2(selectedPos.Y - newPos.Y,
                                            selectedPos.X - newPos.X));
                }

                float scaleX = 0.0127f * distance;
                float scaleY = Flags.Instance.ExpandContactsLineThickness;
                float scaleZ = Flags.Instance.ExpandContactsLineThickness;

                molecule.atom[atomIdx].PrecomputedContactMatrix[contactCount] =
                    Matrix.CreateScale(scaleX, scaleY, scaleZ) *
                    Matrix.CreateRotationY(molecule.atom[atomIdx].ContactAngleZ[contactCount]) *
                    Matrix.CreateRotationZ(molecule.atom[atomIdx].ContactAngleXY[contactCount]);

                contactCount++;
            }

            molecule.atom[atomIdx].ContactCount = contactCount;
        }

        #endregion

        #region Find All H-Bonds

        /// <summary>
        /// Finds all bonds hydrogen w aktywnej molekule.
        /// </summary>
        public void FindAllHBonds()
        {
            var molecule = GetActiveMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            molecule.Calculate_Hbonds3D();
            molecule.PrecomputeBondMatrices();

            RefreshActiveDocument();

            // ⭐ NEW: licz only active H-bondy (UseForHBonds == true)
            int count = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (!molecule.atom[i].UseForHBonds) continue;
                count += molecule.atom[i].H_NrBondedAtoms;
            }

            SetStatus($"H-Bonds: Found {count} bonds",
                      $"Structures: {StructureStorage.Instance.StructureCount}",
                      count > 0 ? "🔗" : "ℹ️");
        }

        #endregion
    }
}
