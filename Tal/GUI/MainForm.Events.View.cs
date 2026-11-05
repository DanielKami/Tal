// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — view-related event handlers (camera, projection, reset view).

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Tal.DrawMethods;

namespace Tal
{
    public partial class MainForm
    {
        #region View Events

        private void OnViewButtonClick(string view)
        {
            var doc = GetActiveDocument();
            if (doc == null)
            {
                SetStatus("No active document", "Structures: 0", "⚠️");
                return;
            }

            var camera = doc.GetCamera();
            var molecule = doc.GetMolecule();

            if (camera == null)
            {
                SetStatus("No active document", "Structures: 0", "⚠️");
                return;
            }

            camera.SetView(view, molecule);
            doc.RefreshView();

            string status = view switch
            {
                "a" => "View along a (X)",
                "b" => "View along b (Y)",
                "c" => "View along c (Z)",
                "a*" => "View along a*",
                "b*" => "View along b*",
                "c*" => "View along c*",
                "+X" => "Rotate +1° around X",
                "-X" => "Rotate -1° around X",
                "+Y" => "Rotate +1° around Y",
                "-Y" => "Rotate -1° around Y",
                "+Z" => "Rotate +1° around Z",
                "-Z" => "Rotate -1° around Z",
                _ => $"Unknown view: {view}"
            };
            SetStatus(status, "Structures: 0", "📐");
        }

        private void BtnRotateLeft90_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();
            if (camera == null) return;
            camera.Rotate *= Matrix.CreateRotationY(-ROTATION_90_STEP);
            camera.MarkChanged();
            camera.ApplyEffects();
            SetStatus("Rotate 90° left", "Structures: 0", "←90");
            RefreshActiveDocument();
        }

        private void BtnRotateRight90_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();
            if (camera == null) return;
            camera.Rotate *= Matrix.CreateRotationY(ROTATION_90_STEP);
            camera.MarkChanged();
            camera.ApplyEffects();
            SetStatus("Rotate 90° right", "Structures: 0", "→90");
            RefreshActiveDocument();
        }

        private void BtnRotateUp90_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();
            if (camera == null) return;
            camera.Rotate *= Matrix.CreateRotationX(-ROTATION_90_STEP);
            camera.MarkChanged();
            camera.ApplyEffects();
            SetStatus("Rotate 90° up", "Structures: 0", "↑90");
            RefreshActiveDocument();
        }

        private void BtnRotateDown90_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();
            if (camera == null) return;
            camera.Rotate *= Matrix.CreateRotationX(ROTATION_90_STEP);
            camera.MarkChanged();
            camera.ApplyEffects();
            SetStatus("Rotate 90° down", "Structures: 0", "↓90");
            RefreshActiveDocument();
        }

        private void BtnMoveLeft_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();
            if (camera == null) return;
            camera.g_system_translate.X -= TRANSLATION_STEP;
            camera.MarkChanged();
            camera.ApplyEffects();
            SetStatus($"Move left (X: {camera.g_system_translate.X:F1})", "Structures: 0", "←");
            RefreshActiveDocument();
        }

        private void BtnMoveRight_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();
            if (camera == null) return;
            camera.g_system_translate.X += TRANSLATION_STEP;
            camera.MarkChanged();
            camera.ApplyEffects();
            SetStatus($"Move right (X: {camera.g_system_translate.X:F1})", "Structures: 0", "→");
            RefreshActiveDocument();
        }

        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();
            if (camera == null) return;
            camera.g_system_translate.Y += TRANSLATION_STEP;
            camera.MarkChanged();
            camera.ApplyEffects();
            SetStatus($"Move up (Y: {camera.g_system_translate.Y:F1})", "Structures: 0", "↑");
            RefreshActiveDocument();
        }

        private void BtnMoveDown_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();
            if (camera == null) return;
            camera.g_system_translate.Y -= TRANSLATION_STEP;
            camera.MarkChanged();
            camera.ApplyEffects();
            SetStatus($"Move down (Y: {camera.g_system_translate.Y:F1})", "Structures: 0", "↓");
            RefreshActiveDocument();
        }

        private void BtnZoomIn_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();
            if (camera == null) return;
            camera.zoom *= 0.9f;
            camera.OneOverZoom = 1.0f / camera.zoom;
            camera.MarkChanged();
            camera.ApplyEffects();
            SetStatus($"Zoom in (zoom={camera.zoom:F2})", "Structures: 0", "🔍+");
            RefreshActiveDocument();
        }

        private void BtnZoomOut_Click(object sender, EventArgs e)
        {
            var camera = GetActiveCamera();
            if (camera == null) return;
            camera.zoom *= 1.1f;
            camera.OneOverZoom = 1.0f / camera.zoom;
            camera.MarkChanged();
            camera.ApplyEffects();
            SetStatus($"Zoom out (zoom={camera.zoom:F2})", "Structures: 0", "🔍-");
            RefreshActiveDocument();
        }

        private void BtnPerspective_Click(object sender, EventArgs e)
        {
            bool isPerspective = _btnPerspective.Checked;

            // ⭐ Set global flags — all cameras (MonoGame + Morphology) will react
            Flags.Instance.PerspectiveMode = isPerspective;
            Flags.Instance.MarkDirty();

            if (isPerspective)
            {
                _btnPerspective.Text = "Persp.";
                SetStatus("Orthographic view", "Structures: 0", "📐");
            }
            else
            {
                _btnPerspective.Text = "Ortho.";
                SetStatus("Perspective view ON", "Structures: 0", "🎯");
            }

            // ⭐ Update camera of the active document
            var camera = GetActiveCamera();
            if (camera != null)
            {
                camera.PerspectiveAngle = isPerspective
                    ? Camera.DEFAULT_PERSPECTIVE_ANGLE
                    : Camera.DEFAULT_PERSPECTIVE_ANGLE_PERSPECTIVE;

                camera.UpdatePerspective();
                camera.ApplyEffects();
            }

            // ⭐⭐ RESET PROGRESSIVE AA — projection changed (ortho↔persp),
            //     jitter in NDC uses M34, which differs in both modes.
            //     Without this the accumulator mixes frames from different projections.
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();

            // ⭐ Refresh all documents (MonoGame + Morphology + Wulff)
            RefreshAllDocuments();
            RefreshMorphologyDocuments();

            System.Diagnostics.Debug.WriteLine(
                $"[MainForm] Perspective: {isPerspective}, " +
                $"Flags.PerspectiveMode = {Flags.Instance.PerspectiveMode}, " +
                $"VisualRevision = {Flags.Instance.VisualRevision}");
        }


        private void BtnAnimate_Click(object sender, EventArgs e)
    => ShowToolWindow(GetAutoRotateWindow(), "🔄 Auto Rotate");

        #endregion
    }
}
