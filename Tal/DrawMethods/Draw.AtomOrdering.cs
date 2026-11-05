// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.AtomOrdering.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// Draw.AtomOrdering — depth sort for correct back-to-front rendering
// ═══════════════════════════════════════════════════════════════════════
//
// (file header unchanged)
//
// ⭐ TIER 2.1 OPTIMIZATION APPLIED:
// OrderAtoms3D now additionally fills _visibleAtomIndices[0..N) —
// a compact list of visible atom indices in back-to-front order.
//
// Both _zDepthCache and _visibleAtomIndices are filled in one pass.
// The DrawScene and DrawCPKImafterstors loops iterate _visibleAtomIndices
// instead of the full atomCount range, eliminating:
// - the `orderedZ < 0` check
// - the `_molecule.atom[orderedZ].show` check
// per iteration per pass.
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using System.Collections.Generic;

using Tal;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region OrderAtoms3D — depth sort + visible atom list

 /// <summary>
 /// Sorts atoms by view-space depth (Z), farthest first.
 ///
 /// ⭐ TIER 2.1: also fills _visibleAtomIndices with the compact
 /// list of visible atoms in render order. The list is reused
 /// across frames (reallocated only when atom count changes).
 ///
 /// Side effects:
 /// - _molecule.atom[i].Z_Depth — render index (0 = first drawn)
 /// - _zDepthCache — full array including hidden atoms (index = -1)
 /// - _visibleAtomIndices — ONLY visible atoms in render order
 /// - _visibleAtomCount — number of entries filled
 /// </summary>
        private void OrderAtoms3D(Matrix rotateViewProjMatrix)
        {
            if (_molecule == null || _molecule.atom == null || _molecule.NumberOfAtoms <= 0)
            {
                _visibleAtomCount = 0;
                return;
            }

            int atomCount = _molecule.NumberOfAtoms;

 // ═══ Ensure both caches are allocated ═══
            if (_zDepthCache == null || _zDepthCache.Length != atomCount)
            {
                _zDepthCache = new (int index, float z)[atomCount];
            }
            if (_visibleAtomIndices == null || _visibleAtomIndices.Length != atomCount)
            {
                _visibleAtomIndices = new int[atomCount];
            }

 // ═══ Pass 1: compute view-space Z for every atom ═══
            bool showH = Flags.Instance.ShowHydrogens;

            for (int i = 0; i < atomCount; i++)
            {
                if (!_molecule.atom[i].show)
                {
                    _molecule.atom[i].Z_Depth = -1;
                    _zDepthCache[i] = (-1, float.MinValue);
                    continue;
                }

                // Hide hydrogens when "Show Hydrogens" is off
                if (!showH && _molecule.atom[i].AtomicNumber == 1)
                {
                    _molecule.atom[i].Z_Depth = -1;
                    _zDepthCache[i] = (-1, float.MinValue);
                    continue;
                }

                var vReal = Vector3.Transform(_molecule.atom[i].Coordinate, rotateViewProjMatrix);
                _zDepthCache[i] = (i, vReal.Z);
            }

 // ═══ Pass 2: sort by Z (descending, far → near) ═══
            Array.Sort(_zDepthCache, ZComparer.Instance);

 // ═══ Pass 3: assign render order + build visible list ═══
            int renderIndex = 0;
            int visibleIndex = 0;

            for (int k = 0; k < atomCount; k++)
            {
                int atomIndex = _zDepthCache[k].index;
                if (atomIndex < 0) continue;

                _molecule.atom[atomIndex].Z_Depth = renderIndex;
                _visibleAtomIndices[visibleIndex++] = atomIndex;
                renderIndex++;
            }

            _visibleAtomCount = visibleIndex;
        }

        #endregion

        #region ZComparer — allocation-free comparer

 /// <summary>
 /// Static singleton comparer for (index, z) tuples.
 /// Descending by Z — farthest atoms first.
 /// </summary>
        private sealed class ZComparer : IComparer<(int index, float z)>
        {
            public static readonly ZComparer Instance = new ZComparer();

            public int Compare((int index, float z) a, (int index, float z) b)
                => b.z.CompareTo(a.z);
        }

        #endregion
    }
}