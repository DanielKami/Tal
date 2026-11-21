// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Draw.Contacts.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        #region DrawContacts — main contact drawing loop

 /// <summary>
 /// Draws all short contacts for all atoms in the molecule.
 ///
 /// Uses the red contact color and the per-atom Save/Restore
 /// pattern for _effectCache — the caller's material state is
 /// preserved after the method returns.
 ///
 /// ⚠️ Called once per frame from DrawScene. Cost is O(total contacts × 2)
 /// because each bond is drawn twice (once per endpoint).
 /// </summary>
        private void DrawContacts(Matrix scaleRotate)
        {
            if (_molecule == null || _cachedSmallRodMeshes == null || _smallRodTransforms == null)
                return;

            for (int i = 0; i < _molecule.NumberOfAtoms; i++)
            {
 // ═══ Skip atoms without contacts or hidden ═══
                if (_molecule.atom[i].ContactCount == 0) continue;
                if (!_molecule.atom[i].show) continue;
                if (!_molecule.atom[i].UseForShortContacts) continue;

                Vector3 startPos = _molecule.atom[i].Coordinate;

                for (int j = 0; j < _molecule.atom[i].ContactCount; j++)
                {
                    int targetIdx = _molecule.atom[i].ContactAtom[j];

 // ═══ Guard: valid target + visible + participating ═══
                    if (targetIdx < 0 || targetIdx >= _molecule.NumberOfAtoms) continue;
                    if (!_molecule.atom[targetIdx].show) continue;
                    if (!_molecule.atom[targetIdx].UseForShortContacts) continue;

                    Vector3 endPos = _molecule.atom[targetIdx].Coordinate;

 // ═══ Save material (may be atom's or previous contact's) ═══
                    Vector3 origDiffuse = _effectCache.DiffuseColor;
                    Vector3 origAmbient = _effectCache.AmbientLightColor;
                    Vector3 origLightDiffuse = _effectCache.LightDiffuseColor;
                    Vector3 origSpecular = _effectCache.SpecularColor;
                    Vector3 origLightSpecular = _effectCache.LightSpecularColor;

 // ═══ Contact material: pure red, no specular ═══
                    Vector3 redColor = new Vector3(1.0f, 0.0f, 0.0f);
                    _effectCache.DiffuseColor = redColor;
                    _effectCache.AmbientLightColor = Vector3.One * 0.5f;
                    _effectCache.LightDiffuseColor = redColor;
                    _effectCache.SpecularColor = Vector3.Zero;
                    _effectCache.LightSpecularColor = Vector3.Zero;
                    _effectCache.Alpha = 1.0f;

                    DrawDashedContactLine(
                        startPos,
                        endPos,
                        scaleRotate,
                        Flags.Instance.ExpandContactsLineThickness);

 // ═══ Restore material ═══
                    _effectCache.DiffuseColor = origDiffuse;
                    _effectCache.AmbientLightColor = origAmbient;
                    _effectCache.LightDiffuseColor = origLightDiffuse;
                    _effectCache.SpecularColor = origSpecular;
                    _effectCache.LightSpecularColor = origLightSpecular;
                }
            }
        }

        #endregion

        #region DrawDashedContactLine — dashed contact line

 /// <summary>
 /// Draws a dashed red contact line using the shared
 /// <see cref="DrawDashedRodSegment"/> helper.
 ///
 /// The <paramref name="thickness"/> parameter is currently unused
 /// in the body (the shared helper reads thickness from its own
 /// parameter) — kept for API compatibility with the original code.
 /// The actual thickness comes from
 /// <c>Flags.Instance.ExpandContactsLineThickness</c> and is passed
 /// by <see cref="DrawContacts"/>.
 ///
 /// Dash/gap lengths come from:
 /// Flags.Instance.ExpandContactsDashLength
 /// Flags.Instance.ExpandContactsGapLength
 /// </summary>
        private void DrawDashedContactLine(
            Vector3 start,
            Vector3 end,
            Matrix scaleRotate,
            float thickness = 0.025f)
        {
            Vector3 direction = end - start;
            float totalLength = direction.Length();

            if (totalLength < 0.001f) return;

            direction.Normalize();

 // ═══ Read dash pattern from flags ═══
            float dashLength = Flags.Instance.ExpandContactsDashLength;
            float gapLength = Flags.Instance.ExpandContactsGapLength;
            float lineThickness = Flags.Instance.ExpandContactsLineThickness;

 // ⚠️ Note: we use Flags.Instance.ExpandContactsLineThickness
 // (not the `thickness` parameter) to preserve exact behavior
 // from the original DrawDashedContactLine, which also read
 // from flags internally.
            DrawDashedRodSegment(start, end, scaleRotate, lineThickness, dashLength, gapLength);
        }

        #endregion
    }
}