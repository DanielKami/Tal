using System;
// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — atom/measurement label drag and visibility logic.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;

namespace Tal
{
    public partial class MainForm
    {
        #region Label Move Mode


        /// <summary>
        /// Hit-test unit-cell axis labels a/b/c (same projection as Draw.Labels).
        /// Returns 0=a, 1=b, 2=c or -1.
        /// </summary>
        private int FindAxisLabelAtPosition(System.Drawing.Point mousePosition, int viewportWidth, int viewportHeight)
        {
            if (!Flags.Instance.ShowAxisLabels || !Flags.Instance.Draw_cell) return -1;

            var molecule = GetActiveMolecule();
            var camera = GetActiveCamera();
            if (molecule?.UnitCellFrame == null || molecule.UnitCellFrame.Count < 9 || camera == null)
                return -1;

            molecule.EnsureAxisLabelOffsets();

            // MUST match Draw.Labels: scaleRotate * View * Projection
            // scaleRotate = Scale * Rotate * Translate (see Draw.cs DrawScene)
            Matrix scaleRotate = camera.Scale * camera.Rotate * camera.Translate;
            Matrix viewProj = scaleRotate * camera.View * camera.Projection;

            int halfW = Math.Max(viewportWidth, 1) / 2;
            int halfH = Math.Max(viewportHeight, 1) / 2;
            float rts = Flags.Instance.RenderTargetScale;
            int ssaa = Math.Clamp(Flags.Instance.SSAASamples, 1, 4);
            float axisScale = Math.Max(0.35f, Flags.Instance.AxisLabelScale) * 0.85f * rts * ssaa;

            int[] endEdgeIndex = { 3, 6, 8 };
            float[] defOx = { 0.014f, -0.010f, 0.006f };
            float[] defOy = { -0.006f, -0.012f, -0.022f };

            int best = -1;
            float bestDistSq = float.MaxValue;
            // Generous pick radius (font size scales it)
            float radius = Math.Max(36f, 24f + axisScale * 40f);
            float radiusSq = radius * radius;

            for (int i = 0; i < 3; i++)
            {
                int ei = endEdgeIndex[i];
                if (ei >= molecule.UnitCellFrame.Count) continue;
                Vector3 worldPos = molecule.UnitCellFrame[ei].position;

                Matrix full = Matrix.CreateTranslation(worldPos) * viewProj;
                float w = full.M44;
                if (w <= 0) continue;

                // Same screen mapping as Draw.Labels / FindAtomAtPosition
                float sx = halfW * (1.0f + full.M41 / w);
                float sy = halfH * (1.0f - full.M42 / w);

                var ax = molecule.AxisLabelOffsets[i];
                float ox = (ax.IsCustom ? ax.OffsetX : defOx[i]) * halfW;
                float oy = (ax.IsCustom ? ax.OffsetY : defOy[i]) * halfH;

                float dx = mousePosition.X - (sx + ox);
                float dy = mousePosition.Y - (sy + oy);
                float d2 = dx * dx + dy * dy;
                if (d2 < radiusSq && d2 < bestDistSq)
                {
                    bestDistSq = d2;
                    best = i;
                }
            }
            return best;
        }

        public void HandleLabelClick(System.Drawing.Point mousePosition, int viewportWidth, int viewportHeight)
        {
            if (!_isLabelMoveMode)
                return;

            if (_isDraggingMeasurementLabel && _selectedMeasurementIndex >= 0)
            {
                _isDraggingMeasurementLabel = false;
                _selectedMeasurementIndex = -1;
                SetStatus("Measurement label placement finished", "Structures: 0", "✅");
                RefreshActiveDocument();
            }

            if (_isDraggingLabel && _selectedLabelIndex >= 0)
            {
                _isDraggingLabel = false;
                _selectedLabelIndex = -1;
                RefreshActiveDocument();
            }

            if (_isDraggingAxisLabel && _selectedAxisLabelIndex >= 0)
            {
                _isDraggingAxisLabel = false;
                _selectedAxisLabelIndex = -1;
                RefreshActiveDocument();
            }

            var camera = GetActiveCamera();
            if (camera == null) return;

            var doc = GetActiveDocument();
            if (doc == null) return;

            var molecule = doc.GetMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0) return;

            _measure = doc.GetMeasure();
            if (_measure == null)
            {
                _measure = new Measure(molecule);
            }

            int measurementIndex = FindMeasurementLabelAtPosition(mousePosition, viewportWidth, viewportHeight);
            if (measurementIndex >= 0)
            {
                _isDraggingMeasurementLabel = false;
                _selectedMeasurementIndex = measurementIndex;
                _isDraggingMeasurementLabel = true;
                _labelDragStart = mousePosition;

                if (_measure.MeasurementLabelOffsets.TryGetValue(measurementIndex, out var off))
                {
                    _labelDragStartOffsetX = off.OffsetX;
                    _labelDragStartOffsetY = off.OffsetY;
                }
                else
                {
                    _labelDragStartOffsetX = 0f;
                    _labelDragStartOffsetY = 0f;
                }

                var m = _measure.measurement[measurementIndex];
                SetStatus($"Moving measurement label {m.label} - drag to reposition", "Structures: 0", "🏷️");
                RefreshActiveDocument();
                return;
            }

            // Axis labels a/b/c (independent of atom Labels)
            {
                int axisIndex = FindAxisLabelAtPosition(mousePosition, viewportWidth, viewportHeight);
                if (axisIndex >= 0)
                {
                    _selectedAxisLabelIndex = axisIndex;
                    _isDraggingAxisLabel = true;
                    _isDraggingLabel = false;
                    _selectedLabelIndex = -1;
                    _labelDragStart = mousePosition;
                    var off = molecule.GetAxisLabelOffset(axisIndex);
                    // If never moved, seed from default bias so drag is continuous
                    if (!molecule.AxisLabelOffsets[axisIndex].IsCustom)
                    {
                        float[] defOx = { 0.014f, -0.010f, 0.006f };
                        float[] defOy = { -0.006f, -0.012f, -0.022f };
                        off = (defOx[axisIndex], defOy[axisIndex]);
                    }
                    _labelDragStartOffset = off;
                    string[] n = { "a", "b", "c" };
                    SetStatus($"Moving axis label {n[axisIndex]} - drag to reposition", "Structures: 0", "📦");
                    RefreshActiveDocument();
                    return;
                }
            }

            if (Flags.Instance.labels)
            {
                int atomIndex = FindAtomAtPosition(mousePosition, viewportWidth, viewportHeight);
                if (atomIndex >= 0)
                {
                    _isDraggingLabel = false;
                    _selectedLabelIndex = atomIndex;
                    _isDraggingLabel = true;
                    _labelDragStart = mousePosition;
                    var offset = molecule.GetLabelOffset(atomIndex);
                    _labelDragStartOffset = offset;

                    string atomName = molecule.atom[atomIndex].name;
                    SetStatus($"Moving label for {atomName} - drag to reposition", "Structures: 0", "🏷️");

                    for (int i = 0; i < molecule.NumberOfAtoms; i++)
                    {
                        molecule.atom[i].selected = (i == atomIndex);
                    }
                    molecule.NumberSelected = 1;

                    RefreshActiveDocument();
                    return;
                }
            }

            if (_isDraggingMeasurementLabel || _isDraggingLabel || _isDraggingAxisLabel)
            {
                _isDraggingMeasurementLabel = false;
                _isDraggingLabel = false;
                _isDraggingAxisLabel = false;
                _selectedMeasurementIndex = -1;
                _selectedLabelIndex = -1;
                _selectedAxisLabelIndex = -1;
                SetStatus("Label editing finished", "Structures: 0", "✅");
                RefreshActiveDocument();
                return;
            }

            SetStatus("Click on a measurement, atom label, or axis label (a/b/c) to move it", "Structures: 0", "🏷️");
        }

        public void HandleLabelMouseDown(System.Drawing.Point mousePosition, int viewportWidth, int viewportHeight)
        {
            if (!_isLabelMoveMode)
                return;

            if (_isDraggingMeasurementLabel)
            {
                _isDraggingMeasurementLabel = false;
                _selectedMeasurementIndex = -1;
            }
            if (_isDraggingLabel)
            {
                _isDraggingLabel = false;
                _selectedLabelIndex = -1;
            }
            if (_isDraggingAxisLabel)
            {
                _isDraggingAxisLabel = false;
                _selectedAxisLabelIndex = -1;
            }

            var camera = GetActiveCamera();
            if (camera == null) return;

            var doc = GetActiveDocument();
            if (doc == null) return;

            var molecule = doc.GetMolecule();
            if (molecule == null) return;

            _measure = doc.GetMeasure();
            if (_measure == null && molecule.NumberOfAtoms > 0)
            {
                _measure = new Measure(molecule);
            }

            int measurementIndex = molecule.NumberOfAtoms > 0
                ? FindMeasurementLabelAtPosition(mousePosition, viewportWidth, viewportHeight)
                : -1;
            if (measurementIndex >= 0)
            {
                _selectedMeasurementIndex = measurementIndex;
                _isDraggingMeasurementLabel = true;
                _labelDragStart = mousePosition;

                if (_measure.MeasurementLabelOffsets.TryGetValue(measurementIndex, out var off))
                {
                    _labelDragStartOffsetX = off.OffsetX;
                    _labelDragStartOffsetY = off.OffsetY;
                }
                else
                {
                    _labelDragStartOffsetX = 0f;
                    _labelDragStartOffsetY = 0f;
                }

                var m = _measure.measurement[measurementIndex];
                SetStatus($"Moving measurement label {m.label} - drag to reposition", "Structures: 0", "🏷️");
                RefreshActiveDocument();
                return;
            }

            // Axis labels a/b/c (independent of atom Labels)
            {
                int axisIndex = FindAxisLabelAtPosition(mousePosition, viewportWidth, viewportHeight);
                if (axisIndex >= 0)
                {
                    _selectedAxisLabelIndex = axisIndex;
                    _isDraggingAxisLabel = true;
                    _isDraggingLabel = false;
                    _selectedLabelIndex = -1;
                    _labelDragStart = mousePosition;
                    var off = molecule.GetAxisLabelOffset(axisIndex);
                    // If never moved, seed from default bias so drag is continuous
                    if (!molecule.AxisLabelOffsets[axisIndex].IsCustom)
                    {
                        float[] defOx = { 0.014f, -0.010f, 0.006f };
                        float[] defOy = { -0.006f, -0.012f, -0.022f };
                        off = (defOx[axisIndex], defOy[axisIndex]);
                    }
                    _labelDragStartOffset = off;
                    string[] n = { "a", "b", "c" };
                    SetStatus($"Moving axis label {n[axisIndex]} - drag to reposition", "Structures: 0", "📦");
                    RefreshActiveDocument();
                    return;
                }
            }

            if (Flags.Instance.labels)
            {
                int atomIndex = FindAtomAtPosition(mousePosition, viewportWidth, viewportHeight);
                if (atomIndex >= 0)
                {
                    _selectedLabelIndex = atomIndex;
                    _isDraggingLabel = true;
                    _labelDragStart = mousePosition;
                    var offset = molecule.GetLabelOffset(atomIndex);
                    _labelDragStartOffset = offset;

                    string atomName = molecule.atom[atomIndex].name;
                    SetStatus($"Moving label for {atomName} - drag to reposition", "Structures: 0", "🏷️");

                    for (int i = 0; i < molecule.NumberOfAtoms; i++)
                    {
                        molecule.atom[i].selected = (i == atomIndex);
                    }
                    molecule.NumberSelected = 1;

                    RefreshActiveDocument();
                    return;
                }
            }

            SetStatus("Click on a measurement, atom label, or axis label (a/b/c) to move it", "Structures: 0", "🏷️");
        }

        public void HandleLabelDrag(System.Drawing.Point mousePosition,
                               int viewportWidth,
                               int viewportHeight)
        {
            if (!_isLabelMoveMode) return;

            // ═══════════════════════════════════════════════════════════
            // POMIAR ETYKIETY POMIARU (measurement label)
            // ═══════════════════════════════════════════════════════════
            if (_isDraggingMeasurementLabel && _selectedMeasurementIndex >= 0)
            {
                float deltaX = mousePosition.X - _labelDragStart.X;
                float deltaY = mousePosition.Y - _labelDragStart.Y;

                if (!_measure.MeasurementLabelOffsets.ContainsKey(_selectedMeasurementIndex))
                {
                    _measure.MeasurementLabelOffsets[_selectedMeasurementIndex] = new Measure.MeasurementLabelOffset();
                }

                var offset = _measure.MeasurementLabelOffsets[_selectedMeasurementIndex];

                // ⭐ Conversion: pixel delta → NDC (divide by half viewport)
                float halfW = Math.Max(viewportWidth, 1) * 0.5f;
                float halfH = Math.Max(viewportHeight, 1) * 0.5f;

                offset.OffsetX = _labelDragStartOffsetX + deltaX / halfW;
                offset.OffsetY = _labelDragStartOffsetY + deltaY / halfH;
                offset.IsCustom = true;

                var m = _measure.measurement[_selectedMeasurementIndex];
                SetStatus($"Moving measurement label {m.label} - drag to reposition",
                          "Structures: 0", "🏷️");
                RefreshActiveDocument();
                return;
            }


            // ═══════════════════════════════════════════════════════════
            // AXIS LABEL a/b/c
            // ═══════════════════════════════════════════════════════════
            if (_isDraggingAxisLabel && _selectedAxisLabelIndex >= 0)
            {
                var molecule = GetActiveMolecule();
                if (molecule == null) return;

                float deltaX = mousePosition.X - _labelDragStart.X;
                float deltaY = mousePosition.Y - _labelDragStart.Y;
                float halfW = Math.Max(viewportWidth, 1) * 0.5f;
                float halfH = Math.Max(viewportHeight, 1) * 0.5f;

                float newOffsetX = _labelDragStartOffset.offsetX + deltaX / halfW;
                float newOffsetY = _labelDragStartOffset.offsetY + deltaY / halfH;
                molecule.SetAxisLabelOffset(_selectedAxisLabelIndex, newOffsetX, newOffsetY);
                Flags.Instance.VisualRevision++;
                Flags.Instance.MarkDirty();
                RefreshActiveDocument();
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // POMIAR ETYKIETY ATOMU (atom label)
            // ═══════════════════════════════════════════════════════════
            if (_isDraggingLabel && _selectedLabelIndex >= 0)
            {
                var molecule = GetActiveMolecule();
                if (molecule == null) return;

                float deltaX = mousePosition.X - _labelDragStart.X;
                float deltaY = mousePosition.Y - _labelDragStart.Y;

                // ⭐ Conversion: pixel delta → NDC (divide by half viewport)
                float halfW = Math.Max(viewportWidth, 1) * 0.5f;
                float halfH = Math.Max(viewportHeight, 1) * 0.5f;

                float newOffsetX = _labelDragStartOffset.offsetX + deltaX / halfW;
                float newOffsetY = _labelDragStartOffset.offsetY + deltaY / halfH;

                molecule.SetLabelOffset(_selectedLabelIndex, newOffsetX, newOffsetY);
                RefreshActiveDocument();
            }
        }

        public void EndLabelDrag()
        {
            if (_isDraggingMeasurementLabel && _selectedMeasurementIndex >= 0)
            {
                var m = _measure.measurement[_selectedMeasurementIndex];
                SetStatus($"Measurement label {m.label} placed at new position", "Structures: 0", "✅");
                _isDraggingMeasurementLabel = false;
                _selectedMeasurementIndex = -1;
                RefreshActiveDocument();
                return;
            }

            if (_isDraggingAxisLabel && _selectedAxisLabelIndex >= 0)
            {
                string[] n = { "a", "b", "c" };
                int ax = _selectedAxisLabelIndex;
                SetStatus($"Axis label {(ax >= 0 && ax < 3 ? n[ax] : "?")} placed", "Structures: 0", "✅");
                _isDraggingAxisLabel = false;
                _selectedAxisLabelIndex = -1;
                RefreshActiveDocument();
                return;
            }

            if (_isDraggingLabel && _selectedLabelIndex >= 0)
            {
                var molecule = GetActiveMolecule();
                if (molecule != null)
                {
                    var offset = molecule.GetLabelOffset(_selectedLabelIndex);
                    string atomName = molecule.atom[_selectedLabelIndex].name;
                    SetStatus($"Label for {atomName} moved to ({offset.offsetX:F0}, {offset.offsetY:F0})", "Structures: 0", "✅");

                    // Deselect atom so it is no longer highlighted yellow after release
                    for (int i = 0; i < molecule.NumberOfAtoms; i++)
                    {
                        molecule.atom[i].selected = false;
                        molecule.atom[i].SelectionOrder = 0;
                    }
                    molecule.NumberSelected = 0;
                }
                _isDraggingLabel = false;
                _selectedLabelIndex = -1;
                var cam = GetActiveCamera();
                if (cam != null)
                    cam.flag_selected = false;
                RefreshActiveDocument();
            }
        }

        public void ResetAllLabels()
        {
            var molecule = GetActiveMolecule();
            if (molecule == null) return;

            molecule.ResetAllLabelOffsets();
            _selectedLabelIndex = -1;
            _isDraggingLabel = false;
            _selectedMeasurementIndex = -1;
            _isDraggingMeasurementLabel = false;

            RefreshActiveDocument();
            SetStatus("All labels reset to default positions", "Structures: 0", "🔄");
        }

        #endregion
    }
}
