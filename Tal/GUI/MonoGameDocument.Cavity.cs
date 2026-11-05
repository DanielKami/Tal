// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MonoGameDocument partial — cavity mesh integration.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Tal.Cavity;

namespace Tal.GUI
{
    public partial class MonoGameDocument
    {
        #region Cavity Detector / Helpers — getters & setters

        /// <summary>Gets the cavity detector.</summary>
        public CavityDetector GetCavityDetector() => _cavityDetector;

        /// <summary>Gets the cavity helpers.</summary>
        public CavityHelpers GetCavityHelpers() => _cavityHelpers;

        /// <summary>
        /// Sets the cavity detector. Used via MainForm do leniwej inicjalizacji
        /// when the detector has not been created yet (e.g. LoadMolecule instead of LoadFromStructure).
        /// </summary>
        public void SetCavityDetector(CavityDetector detector)
        {
            _cavityDetector = detector;
        }

        /// <summary>
        /// Sets the cavity helpers. Used via MainForm do leniwej inicjalizacji.
        /// </summary>
        public void SetCavityHelpers(CavityHelpers helpers)
        {
            _cavityHelpers = helpers;
        }

        #endregion

        #region Cavity Renderer (Lazy Init)

        /// <summary>
        /// Gets the cavity renderer, creating it lazily when the GraphicsDevice is ready.
        /// </summary>
        public CavityRenderer GetCavityRenderer()
        {
            if (_cavityRenderer != null)
                return _cavityRenderer;

            if (!_cavityRendererNeedsInit)
                return null;

            if (_molecule == null || _molecule.NumberOfAtoms == 0)
                return null;

            var graphicsDevice = _gameWindow?.GetGraphicsDevice();
            var camera = _gameWindow?.GetCamera();

            if (graphicsDevice == null || graphicsDevice.IsDisposed)
            {
                System.Diagnostics.Trace.WriteLine(
                    "[MonoGameDocument] ⏳ GraphicsDevice not ready yet — will retry");
                return null;
            }

            if (camera == null)
            {
                System.Diagnostics.Trace.WriteLine(
                    "[MonoGameDocument] ⏳ Camera not ready yet — will retry");
                return null;
            }

            try
            {
                System.Diagnostics.Trace.WriteLine(
                    "[MonoGameDocument] ✅ Creating CavityRenderer (lazy)");

                _cavityRenderer = new CavityRenderer(_molecule, graphicsDevice, camera);

                _cavityRenderer.ProgressUpdated = (msg) =>
                {
                    System.Diagnostics.Trace.WriteLine($"[CavityRenderer] {msg}");
                };

                if (_gameWindow != null && !_gameWindow.IsDisposed)
                {
                    _gameWindow.SetCavityRenderer(_cavityRenderer);
                }

                _cavityRenderer.SetMoleculeShift(_molecule.MoleculeShift);

                _cavityRendererNeedsInit = false;

                System.Diagnostics.Trace.WriteLine(
                    "[MonoGameDocument] ✅ CavityRenderer created (lazy)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[MonoGameDocument] ❌ CavityRenderer creation failed: {ex.Message}");
                _cavityRenderer = null;
            }

            return _cavityRenderer;
        }

        #endregion

        #region Property Mapper

        public CavityPropertyMapper CreatePropertyMapper()
        {
            var renderer = GetCavityRenderer();
            if (renderer == null) return null;

            var cavities = renderer.GetCavities();
            if (cavities == null || cavities.Count == 0) return null;

            var molecule = GetMolecule();
            if (molecule == null) return null;

            _propertyMapper = new CavityPropertyMapper(molecule, cavities);
            return _propertyMapper;
        }

        #endregion

        #region Cavity Data Management

        public void UpdateCavityData(List<Cavity.Cavity> cavities)
        {
            if (_cavityRenderer != null && cavities != null)
            {
                _cavityRenderer.SetCavities(cavities);
                _cavityRenderer.BuildRepeatedCavities();
                _cavityRenderer.CurrentDrawMode = CavityRenderer.DrawMode.MarchingCubesMesh;
                _cavityRenderer.ForceRefresh();
                RefreshView();
            }
        }

        public void RefreshCavityPosition()
        {
            if (_cavityRenderer != null && _molecule != null)
            {
                _cavityRenderer.SetMoleculeShift(_molecule.MoleculeShift);
                RefreshView();
            }
        }

        #endregion
    }
}