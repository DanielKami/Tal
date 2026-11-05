// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.HydrogenBonds.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region DrawAllHydrogenBonds — scene-level pass

        /// <summary>
        /// Draws every H-bond in the molecule in one pass (like DrawContacts).
        ///
        /// Why not inside DrawSingleAtom:
        /// - After checkbox ON, neighbour acceptors are appended to atom[] and
        ///   H_BondedAtom / H_AccCartPos are updated. A per-atom pass can miss
        ///   bonds whose host was already processed, or depend on stale cache.
        /// - Apply "fixed" it only because it forced a full reload + redraw.
        ///
        /// Endpoint resolution (in order):
        /// 1) H_BondedAtom[j] if valid index and atom is shown
        /// 2) H_AccCartPos[j] (lattice acceptor not yet indexed / off-screen host)
        /// Skip if either host or resolved acceptor is hidden, or length ~ 0.
        /// </summary>
        private void DrawAllHydrogenBonds(Matrix scaleRotate)
        {
            if (_molecule == null || _molecule.atom == null) return;
            if (!Flags.Instance.ShowHBonds) return;
            if (_cachedSmallRodMeshes == null || _smallRodTransforms == null) return;

            Vector3 origDiffuse = _effectCache.DiffuseColor;
            Vector3 origAmbient = _effectCache.AmbientLightColor;
            Vector3 origLightDiffuse = _effectCache.LightDiffuseColor;
            Vector3 origSpecular = _effectCache.SpecularColor;
            Vector3 origLightSpecular = _effectCache.LightSpecularColor;

            Vector3 hBondColor = Flags.Instance.HBondColor;
            _effectCache.DiffuseColor = hBondColor;
            _effectCache.AmbientLightColor = Vector3.One * 0.5f;
            _effectCache.LightDiffuseColor = hBondColor;
            _effectCache.SpecularColor = Vector3.Zero;
            _effectCache.LightSpecularColor = Vector3.Zero;

            float lineThickness = Flags.Instance.HBondDrawFromHydrogen ? 0.02f : 0.025f;
            if (Flags.Instance.HBondThickness > 0f)
                lineThickness = Flags.Instance.HBondThickness;

            bool dashed = Flags.Instance.HBondDashed;
            int nAtoms = _molecule.NumberOfAtoms;

            for (int i = 0; i < nAtoms; i++)
            {
                if (!_molecule.atom[i].show) continue;
                if (!_molecule.atom[i].UseForHBonds) continue;

                int hCount = _molecule.atom[i].H_NrBondedAtoms;
                if (hCount <= 0) continue;
                if (_molecule.atom[i].H_BondedAtom == null) continue;

                Vector3 startPos = _molecule.atom[i].Coordinate;

                for (int j = 0; j < hCount; j++)
                {
                    // Optional: skip if the H of this bridge is hidden
                    if (_molecule.atom[i].H_HydrogenAtom != null &&
                        j < _molecule.atom[i].H_HydrogenAtom.Length)
                    {
                        int hIdx = _molecule.atom[i].H_HydrogenAtom[j];
                        if (hIdx >= 0 && hIdx < nAtoms && !_molecule.atom[hIdx].show)
                            continue;
                    }

                    Vector3 endPos;
                    int accIdx = _molecule.atom[i].H_BondedAtom[j];
                    if (accIdx >= 0 && accIdx < nAtoms)
                    {
                        // Keep acceptor sphere visible for this frame
                        _molecule.atom[accIdx].show = true;
                        _molecule.atom[accIdx].UseForHBonds = true;
                        endPos = _molecule.atom[accIdx].Coordinate;
                    }
                    else if (_molecule.atom[i].H_AccCartPos != null &&
                             j < _molecule.atom[i].H_AccCartPos.Length)
                    {
                        endPos = _molecule.atom[i].H_AccCartPos[j];
                    }
                    else
                    {
                        continue;
                    }

                    float len = Vector3.Distance(startPos, endPos);
                    if (len < 0.05f || len > 10f) continue;

                    if (dashed)
                        DrawDashedHBond(startPos, endPos, scaleRotate, lineThickness);
                    else
                        DrawSolidHBondLine(startPos, endPos, scaleRotate, lineThickness);
                }
            }

            _effectCache.DiffuseColor = origDiffuse;
            _effectCache.AmbientLightColor = origAmbient;
            _effectCache.LightDiffuseColor = origLightDiffuse;
            _effectCache.SpecularColor = origSpecular;
            _effectCache.LightSpecularColor = origLightSpecular;
        }

        #endregion

        #region DrawDashedHBond / DrawSolidHBondLine

        private void DrawDashedHBond(
            Vector3 start,
            Vector3 end,
            Matrix scaleRotate,
            float lineThickness)
        {
            Vector3 direction = end - start;
            if (direction.LengthSquared() < 1e-6f) return;
            direction.Normalize();

            float dashLength = Flags.Instance.HBondDashLength;
            float gapLength = Flags.Instance.HBondGapLength;
            DrawDashedRodSegment(start, end, scaleRotate, lineThickness, dashLength, gapLength);
        }

        /// <summary>
        /// Solid H-bond as a single rod between two Cartesian points
        /// (no precomputed matrix — always matches current atom positions).
        /// </summary>
        private void DrawSolidHBondLine(
            Vector3 start,
            Vector3 end,
            Matrix scaleRotate,
            float lineThickness)
        {
            // Reuse dashed helper with full segment = solid look
            float len = Vector3.Distance(start, end);
            if (len < 1e-4f) return;
            // One long "dash" covering the whole bond
            DrawDashedRodSegment(start, end, scaleRotate, lineThickness, len + 0.01f, 0f);
        }

        #endregion
    }
}
