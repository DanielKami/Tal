// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MonoGameDocument partial — view / camera helpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Tal.DrawMethods;

namespace Tal.GUI
{
    public partial class MonoGameDocument
    {
        #region View Refresh

        /// <summary>
        /// Refreshes the 3D view.
        /// </summary>
        public void RefreshView()
        {
            if (_gameWindow != null && !_gameWindow.IsDisposed)
            {
                if (_cavityRenderer != null)
                {
                    _cavityRenderer.BuildRepeatedCavities();
                    _cavityRenderer.ForceRefresh();
                }

                // ⭐ Invalidate Draw cache
                _gameWindow.NotifyMoleculeChanged();

                _gameWindow.RefreshView();
            }
        }


        /// <summary>
        /// Call ONLY when the 3D scene geometry actually changes
        /// (new kontakty, poliedry, change molecule, new linie pomiarowe).
        /// </summary>
        public void InvalidateScene3D()
        {
            Flags.Instance.VisualRevision++;
            Flags.Instance.MarkDirty();
            RefreshView();


        }
        /// <summary>
        /// Resets the draw cache.
        /// </summary>
        public void ResetDrawCache()
        {
            if (_gameWindow != null && !_gameWindow.IsDisposed)
            {
                _gameWindow.ResetDrawCache();
            }
        }

        /// <summary>
        /// Checks if the game window is valid.
        /// </summary>
        public bool IsGameWindowValid()
        {
            return _gameWindow != null && !_gameWindow.IsDisposed && _gameWindow.IsValid();
        }

        /// <summary>
        /// Sets the reveal click position for symmetry visualization.
        /// </summary>
        /// <param name="position">The click position in world coordinates.</param>
        /// <param name="show">Whether to show the position marker.</param>
        public void SetRevealClickPosition(Vector3 position, bool show)
        {
            if (_gameWindow != null && !_gameWindow.IsDisposed)
            {
                _gameWindow.SetRevealClickPosition(position, show);
            }
        }

        #endregion

        #region Style

        /// <summary>
        /// Sets the rendering style for all atoms.
        /// </summary>
        /// <param name="styleIndex">The style index.</param>
        public void SetStyle(int styleIndex)
        {
            _gameWindow?.SetStyle(styleIndex);
            Flags.Instance.Presentation = styleIndex;

            // ⭐ Polyhedra: recompute and pass to Draw
            if (styleIndex == 4)
            {
                if (_molecule != null && _molecule.NumberOfAtoms > 0)
                {
                    Polihedrals.CurrentMolecule = _molecule;
                    Polihedrals.FindPolihedral();

                    System.Diagnostics.Debug.WriteLine(
                        $"[MonoGameDocument.SetStyle] Polyhedrals detected: " +
                        $"{Polihedrals.PolihedralsList.Count} polyhedra, " +
                        $"{Polihedrals.VertexesList.Count} meshes");
                }
            }
            else
            {
                Polihedrals.Clear();
            }

            // ⭐ Pass to Draw
            if (_gameWindow != null && !_gameWindow.IsDisposed)
            {
                _gameWindow.SetPolyhedrals(Polihedrals.VertexesList);

                System.Diagnostics.Debug.WriteLine(
                    $"[MonoGameDocument.SetStyle] Passed to Draw: {Polihedrals.VertexesList.Count}");
            }
        }

        #endregion

        #region Zoom / Fit / Reset

        /// <summary>
        /// Zooms in the view.
        /// </summary>
        public void ZoomIn()
        {
            var camera = GetCamera();
            if (camera != null)
            {
                camera.zoom *= 0.9f;
                camera.OneOverZoom = 1.0f / camera.zoom;
                camera.ApplyEffects();
                RefreshView();
            }
        }

        /// <summary>
        /// Zooms out the view.
        /// </summary>
        public void ZoomOut()
        {
            var camera = GetCamera();
            if (camera != null)
            {
                camera.zoom *= 1.1f;
                camera.OneOverZoom = 1.0f / camera.zoom;
                camera.ApplyEffects();
                RefreshView();
            }
        }

        /// <summary>
        /// Fits the view to show all atoms.
        /// </summary>
        public void FitView()
        {
            if (_molecule != null && _molecule.NumberOfAtoms > 0 && _gameWindow != null)
            {
                var camera = GetCamera();
                if (camera != null)
                {
                    Vector3 center = Vector3.Zero;
                    for (int i = 0; i < _molecule.NumberOfAtoms; i++)
                    {
                        center += _molecule.atom[i].Coordinate;
                    }
                    center /= _molecule.NumberOfAtoms;

                    camera.g_system_translate = -center;
                    camera.g_cameraPos = new Vector3(0, 0, 80);
                    camera.g_targetPos = Vector3.Zero;
                    camera.ApplyEffects();
                    RefreshView();
                }
            }
        }

        /// <summary>
        /// Resets the camera view.
        /// </summary>
        public void ResetView()
        {
            var camera = GetCamera();
            if (camera != null)
            {
                camera.Rotate = Matrix.Identity;
                camera.g_system_translate = Vector3.Zero;
                camera.zoom = 0.5f;
                camera.OneOverZoom = 1.0f / camera.zoom;
                camera.ApplyEffects();
                RefreshView();
            }
        }

        #endregion

        #region Render Target Passthrough

        public void DrawSceneToRenderTarget()
        {
            _gameWindow?.DrawSceneToRenderTarget();
        }

        public void DrawFullFrameToRenderTarget()
        {
            _gameWindow?.DrawFullFrameToRenderTarget();
        }

        #endregion
    }
}