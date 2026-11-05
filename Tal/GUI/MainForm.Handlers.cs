// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — high-level command handlers invoked from menus and toolbars.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Tal.Calculate;
using Tal.Core.PowderDiffraction;
using Tal.DrawMethods;
using Tal.GUI;
using WeifenLuo.WinFormsUI.Docking;

namespace Tal
{
    public partial class MainForm
    {
        #region Reveal Symmetry

        public void HandleRevealSymmetryClick(System.Drawing.Point mousePosition,
                                             int viewportWidth,
                                             int viewportHeight)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[RevealSym] ENTER — mouse=({mousePosition.X},{mousePosition.Y}), " +
                $"viewport=({viewportWidth},{viewportHeight})");

            var molecule = GetActiveMolecule();
            var camera = GetActiveCamera();
            var doc = GetActiveDocument();

            if (molecule == null || camera == null || doc == null)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }

            if (molecule.AsymmetricPart.Count == 0)
            {
                SetStatus("No asymmetric part available", "Structures: 0", "⚠️");
                return;
            }

            try
            {
                // ⭐ Click marker (visual — dot at click position)
                Vector3 clickWorldPosition = ScreenToWorldPosition(
                    mousePosition, viewportWidth, viewportHeight, camera);
                doc.SetRevealClickPosition(clickWorldPosition, true);

                // ⭐ NEW: find molecule via projecting centres onto the screen
                var (bestTranslation, bestSymOpIndex, screenDistSq) =
                    FindRevealMoleculeAtScreenPosition(
                        molecule,
                        mousePosition,
                        viewportWidth,
                        viewportHeight,
                        camera,
                        rangeX: 5,
                        rangeY: 5,
                        rangeZ: 10,
                        pixelTolerance: 60);

                if (bestSymOpIndex < 0)
                {
                    float nearestPx = (float)Math.Sqrt(screenDistSq);
                    SetStatus(
                        $"No molecule near click point " +
                        $"(nearest was {nearestPx:F0} px away)",
                        "Structures: 0", "⚠️");
                    doc.SetRevealClickPosition(Vector3.Zero, false);
                    doc.RefreshView();
                    return;
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[RevealSym] Found molecule candidate: " +
                    $"symOp={bestSymOpIndex}, " +
                    $"trans=({bestTranslation.X},{bestTranslation.Y},{bestTranslation.Z}), " +
                    $"screenDist={Math.Sqrt(screenDistSq):F1}px");

                // ⭐ Generate atoms of the new molecule (after symop and translation)
                var newAtoms = GenerateTranslatedMoleculeWithSymmetry(
                    molecule, bestTranslation, bestSymOpIndex);

                if (newAtoms.Count == 0)
                {
                    SetStatus("Failed to generate symmetry molecule", "Structures: 0", "❌");
                    doc.SetRevealClickPosition(Vector3.Zero, false);
                    doc.RefreshView();
                    return;
                }

                // ⭐ Check collisions with existing atoms
                if (CheckForCollisions(molecule, newAtoms, 0.5f))
                {
                    SetStatus("Molecule would overlap with existing atoms - try different location",
                              "Structures: 0", "⚠️");
                    doc.SetRevealClickPosition(Vector3.Zero, false);
                    doc.RefreshView();
                    return;
                }

                // ⭐ Add atoms to the molecule
                int oldCount = molecule.NumberOfAtoms;
                int addedCount = ShortContactsFinder.AddNearbyAtomsInternal(molecule, newAtoms);

                if (addedCount == 0)
                {
                    SetStatus("No new atoms added (already present?)", "Structures: 0", "ℹ️");
                    doc.SetRevealClickPosition(Vector3.Zero, false);
                    doc.RefreshView();
                    return;
                }

                // ⭐ New atoms — oznacz jako "real" (not expand-contacts)
                for (int i = oldCount; i < molecule.NumberOfAtoms; i++)
                {
                    molecule.atom[i].UseForHBonds = true;
                    molecule.atom[i].UseForShortContacts = true;
                    molecule.atom[i].selected = true;
                }

                // ⭐ Remove new indices from ExpandedAtomIndices — these are not expanded contacts
                for (int i = molecule.ExpandedAtomIndices.Count - 1; i >= 0; i--)
                {
                    if (molecule.ExpandedAtomIndices[i] >= oldCount)
                        molecule.ExpandedAtomIndices.RemoveAt(i);
                }

                molecule.NumberSelected = addedCount;

                // ⭐ Recalculate bonds and H-bonds
                molecule.Calculate_Bonds3D();
                molecule.Calculate_Hbonds3D();
                molecule.PrecomputeBondMatrices();

                // ⭐ Hide click marker
                doc.SetRevealClickPosition(Vector3.Zero, false);

                // ⭐ Reset AA — new atoms added, scene changed
                Flags.Instance.VisualRevision++;

                // ⭐ Force refresh
                doc.RefreshView();

                // ⭐ Status
                string opInfo = bestSymOpIndex >= 0 ? $" (symOp {bestSymOpIndex})" : "";
                SetStatus(
                    $"Generated symmetry molecule with {addedCount} atoms " +
                    $"at translation ({bestTranslation.X}, {bestTranslation.Y}, " +
                    $"{bestTranslation.Z}){opInfo}",
                    $"Structures: {StructureStorage.Instance.StructureCount}", "🔮✅");

                System.Diagnostics.Debug.WriteLine(
                    $"[RevealSym] DONE — added {addedCount} atoms, " +
                    $"NumberOfAtoms={molecule.NumberOfAtoms}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", "Structures: 0", "❌");
                doc?.SetRevealClickPosition(Vector3.Zero, false);
                doc?.RefreshView();

                System.Diagnostics.Debug.WriteLine($"[RevealSym] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RevealSym] STACK: {ex.StackTrace}");
            }
        }

        #endregion

        #region Measurements

        public void HandleMeasurementClick(System.Drawing.Point mousePosition,
                                            int viewportWidth,
                                            int viewportHeight)
        {
            var molecule = GetActiveMolecule();
            var camera = GetActiveCamera();
            var doc = GetActiveDocument();
            var measure = GetActiveMeasure();
            _measure = measure;

            if (molecule == null || camera == null || doc == null || measure == null)
            {
                SetStatus("No molecule loaded", "Structures: 0", "⚠️");
                return;
            }
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

            int measurementType = Flags.Instance.MeasurementType;

            // Atom→plane mode (measurementType == 1) has separate logic
            if (measurementType == 1)
            {
                HandleDistanceMeasurementClick(mousePosition, viewportWidth, viewportHeight,
                    molecule, camera, doc, measure);
                return;
            }

            // === Original logic for angles and torsions ===
            int atomIndex = FindAtomAtPosition(mousePosition, viewportWidth, viewportHeight);
            if (atomIndex < 0) return;

            if (!molecule.atom[atomIndex].selected)
            {
                molecule.atom[atomIndex].selected = true;
                molecule.NumberSelected++;
                molecule.atom[atomIndex].SelectionOrder = molecule.NumberSelected;
            }
            else
            {
                molecule.atom[atomIndex].selected = false;
                molecule.atom[atomIndex].SelectionOrder = 0;
                molecule.NumberSelected--;
            }

            doc.RefreshView();

            bool measurementDone = false;
            int requiredAtoms = measurementType == 2 ? 3 : 4;

            if (molecule.NumberSelected >= requiredAtoms)
            {
                switch (measurementType)
                {
                    case 2:
                        if (measure.MeasureAngle())
                        {
                            SetStatus(measure.Message,
                                $"Structures: {StructureStorage.Instance.StructureCount}",
                                "📐");
                            measurementDone = true;
                        }
                        break;
                    case 3:
                        if (measure.MeasureTorsion())
                        {
                            SetStatus(measure.Message,
                                $"Structures: {StructureStorage.Instance.StructureCount}",
                                "🔄");
                            measurementDone = true;
                        }
                        break;
                }
            }

            if (measurementDone)
            {
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    molecule.atom[i].selected = false;
                    molecule.atom[i].SelectionOrder = 0;
                }
                molecule.NumberSelected = 0;
            }
            else if (molecule.NumberSelected > 0)
            {
                string mode = measurementType == 2 ? "angle" : "torsion";
                SetStatus($"Select {molecule.NumberSelected}/{requiredAtoms} atoms for {mode}",
                          "Structures: 0", "👆");
            }

            doc.RefreshView();
        }

        /// <summary>
        /// Handles clicks in Measure Distances mode. Detects whether the user
        /// clicked an atom or a plane, and applies one of:
        ///   - atom + atom       -> distance
        ///   - atom + plane      -> perpendicular distance
        ///   - plane + plane     -> not supported yet (resets)
        /// </summary>
        private void HandleDistanceMeasurementClick(
            System.Drawing.Point mousePosition, int viewportWidth, int viewportHeight,
            Molecule molecule, Camera camera, MonoGameDocument doc, Measure measure)
        {
            // 1. Try to find atom under the cursor
            int atomIndex = FindAtomAtPosition(mousePosition, viewportWidth, viewportHeight);

            // 2. If no atom, try to hit a plane
            CrystalPlane planeHit = null;
            if (atomIndex < 0)
            {
                planeHit = FindPlaneAtPosition(mousePosition, viewportWidth, viewportHeight,
                                                camera, molecule);
            }

            // 3. React na to co znaleziono
            if (atomIndex >= 0)
            {
                HandleDistanceAtomClick(atomIndex, molecule, doc, measure);
            }
            else if (planeHit != null)
            {
                HandleDistancePlaneClick(planeHit, molecule, doc, measure);
            }
            else
            {
                SetStatus("Click on an atom or a plane", "Structures: 0", "👆");
            }
        }

        private void HandleDistanceAtomClick(int atomIndex, Molecule molecule,
                                              MonoGameDocument doc, Measure measure)
        {
            // Stan: nic not wybrano -> save atom jako pierwszy punkt
            if (measure.PendingAtom1 < 0 && measure.PendingPlane1 == null)
            {
                measure.PendingAtom1 = atomIndex;
                measure.PendingPosition1 = molecule.atom[atomIndex].Coordinate;

                ClearAllAtomSelection(molecule);
                molecule.atom[atomIndex].selected = true;
                molecule.atom[atomIndex].SelectionOrder = 1;
                molecule.NumberSelected = 1;

                SetStatus($"First atom: {molecule.atom[atomIndex].name}. " +
                          $"Click another atom or a plane.",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "📍");
                doc.RefreshView();
                return;
            }

            // State: had atom, clicked second atom -> distance
            if (measure.PendingAtom1 >= 0)
            {
                int a1 = measure.PendingAtom1;
                measure.ResetPendingSelection();

                ClearAllAtomSelection(molecule);
                molecule.atom[a1].selected = true;
                molecule.atom[a1].SelectionOrder = 1;
                molecule.atom[atomIndex].selected = true;
                molecule.atom[atomIndex].SelectionOrder = 2;
                molecule.NumberSelected = 2;

                if (measure.MeasureDistance())
                {
                    SetStatus(measure.Message,
                        $"Structures: {StructureStorage.Instance.StructureCount}", "📏");
                }
                else
                {
                    SetStatus(measure.Message, "Structures: 0", "⚠️");
                }

                ClearAllAtomSelection(molecule);
                doc.RefreshView();
                return;
            }

            // State: had plane, clicked atom -> distance atom-plane
            if (measure.PendingPlane1 != null)
            {
                var plane = measure.PendingPlane1;
                measure.ResetPendingSelection();

                if (measure.MeasureAtomToPlane(atomIndex, plane, molecule.MoleculeShift))
                {
                    SetStatus(measure.Message,
                        $"Structures: {StructureStorage.Instance.StructureCount}", "📏");
                }
                else
                {
                    SetStatus(measure.Message, "Structures: 0", "⚠️");
                }

                ClearAllAtomSelection(molecule);
                doc.RefreshView();
                return;
            }
        }

        private void HandleDistancePlaneClick(CrystalPlane plane, Molecule molecule,
                                               MonoGameDocument doc, Measure measure)
        {
            // State: nothing selected -> store the plane
            if (measure.PendingAtom1 < 0 && measure.PendingPlane1 == null)
            {
                measure.PendingPlane1 = plane;
                SetStatus($"First: plane ({plane.H}{plane.K}{plane.L}). Click an atom.",
                          $"Structures: {StructureStorage.Instance.StructureCount}", "📍");
                doc.RefreshView();
                return;
            }

            // State: had atom, clicked plane -> distance atom-plane
            if (measure.PendingAtom1 >= 0)
            {
                int atomIdx = measure.PendingAtom1;
                measure.ResetPendingSelection();

                if (measure.MeasureAtomToPlane(atomIdx, plane, molecule.MoleculeShift))
                {
                    SetStatus(measure.Message,
                        $"Structures: {StructureStorage.Instance.StructureCount}", "📏");
                }
                else
                {
                    SetStatus(measure.Message, "Structures: 0", "⚠️");
                }

                ClearAllAtomSelection(molecule);
                doc.RefreshView();
                return;
            }

            // State: had plane, clicked second plane -> reset
            if (measure.PendingPlane1 != null)
            {
                measure.ResetPendingSelection();
                SetStatus("Plane-plane measurement is not supported yet. Reset.",
                          "Structures: 0", "ℹ️");
                doc.RefreshView();
                return;
            }
        }

        /// <summary>
        /// Finds the CrystalPlane under the mouse cursor by sampling points on the
        /// plane's rectangle and projecting them to screen. This uses the SAME
        /// transformation as FindAtomAtPosition, so it's guaranteed to work.
        /// </summary>
        private CrystalPlane FindPlaneAtPosition(
            System.Drawing.Point mousePos, int viewportWidth, int viewportHeight,
            Camera camera, Molecule molecule)
        {
            var planes = GetPlanes();
            if (planes == null || planes.Count == 0) return null;

            Matrix fullTransform = camera.Scale * camera.Rotate * camera.View * camera.Projection;

            Vector3 shift = molecule.MoleculeShift;
            CrystalPlane best = null;
            float bestDistance = float.MaxValue;

            foreach (var plane in planes)
            {
                if (plane == null || !plane.Visible) continue;
                if (plane.Corners == null || plane.Corners.Length < 4) continue;

                Vector3 c0 = plane.Corners[0] - shift;
                Vector3 c1 = plane.Corners[1] - shift;
                Vector3 c2 = plane.Corners[2] - shift;
                Vector3 c3 = plane.Corners[3] - shift;

                const int SAMPLES = 15;
                float bestSampleDistSq = float.MaxValue;
                bool anyVisible = false;

                for (int i = 0; i <= SAMPLES; i++)
                {
                    float u = i / (float)SAMPLES;
                    for (int j = 0; j <= SAMPLES; j++)
                    {
                        float v = j / (float)SAMPLES;

                        Vector3 p = (1 - u) * (1 - v) * c0
                                  + u * (1 - v) * c1
                                  + u * v * c2
                                  + (1 - u) * v * c3;

                        Matrix world = Matrix.CreateTranslation(p);
                        Matrix wvp = world * fullTransform;

                        float w = wvp.M44;
                        if (w <= 0) continue;

                        int sx = (int)(viewportWidth / 2 * (1.0 + wvp.M41 / w));
                        int sy = (int)(viewportHeight / 2 * (1.0 - wvp.M42 / w));

                        float dx = sx - mousePos.X;
                        float dy = sy - mousePos.Y;
                        float d2 = dx * dx + dy * dy;

                        anyVisible = true;

                        if (d2 < bestSampleDistSq)
                            bestSampleDistSq = d2;
                    }
                }

                if (!anyVisible) continue;

                float distPx = (float)Math.Sqrt(bestSampleDistSq);
                float tolerancePx = 30f;

                System.Diagnostics.Debug.WriteLine($"[FindPlane] plane={plane.Name} " +
                    $"bestSampleDist={distPx:F1}px");

                if (distPx > tolerancePx) continue;

                if (distPx < bestDistance)
                {
                    bestDistance = distPx;
                    best = plane;
                }
            }

            System.Diagnostics.Debug.WriteLine($"[FindPlane] best={best?.Name ?? "null"}");
            return best;
        }

        /// <summary>
        /// Checks whether a point (assumed to lie on the plane) is inside the
        /// rectangle defined by 4 corners. Uses the plane's normal and cross products
        /// to test each edge.
        /// </summary>
        private bool IsPointInsidePlaneRectangle(Vector3 point, Vector3[] corners)
        {
            if (corners == null || corners.Length < 4) return false;

            Vector3 c0 = corners[0];
            Vector3 c1 = corners[1];
            Vector3 c2 = corners[2];
            Vector3 c3 = corners[3];

            Vector3 edge1 = c1 - c0;
            Vector3 edge2 = c3 - c0;
            Vector3 normal = Vector3.Cross(edge1, edge2);
            if (normal.LengthSquared() < 1e-9f) return false;
            normal.Normalize();

            float SignDot(Vector3 edgeStart, Vector3 edgeEnd, Vector3 pt)
            {
                Vector3 edge = edgeEnd - edgeStart;
                Vector3 toPt = pt - edgeStart;
                return Vector3.Dot(Vector3.Cross(edge, toPt), normal);
            }

            float s0 = SignDot(c0, c1, point);
            float s1 = SignDot(c1, c2, point);
            float s2 = SignDot(c2, c3, point);
            float s3 = SignDot(c3, c0, point);

            return (s0 >= 0 && s1 >= 0 && s2 >= 0 && s3 >= 0) ||
                   (s0 <= 0 && s1 <= 0 && s2 <= 0 && s3 <= 0);
        }

        private void ClearAllAtomSelection(Molecule molecule)
        {
            if (molecule == null) return;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                molecule.atom[i].selected = false;
                molecule.atom[i].SelectionOrder = 0;
            }
            molecule.NumberSelected = 0;
        }

        #endregion

        #region Structure Info Window

        /// <summary>
        /// Opens the Structure Info window (called by menu, button, etc.).
        /// </summary>
        public void OnStructureInfo()
        {
            var win = GetStructureInfoWindow();
            if (win.IsHidden) win.Show(_dockPanel, DockState.DockLeft);
            win.Activate();

            var molecule = GetActiveMolecule();
            var doc = GetActiveDocument();
            if (molecule != null)
            {
                string id = doc?.Text ?? "(unknown)";
                win.LoadStructure(molecule, id);
            }

            _dockPanel.DockLeftPortion = 400.0 / Math.Max(this.Width, 1);
            SetStatus("Structure Info opened",
                      $"Structures: {StructureStorage.Instance.StructureCount}", "📋");
        }

        #endregion
    }
}
