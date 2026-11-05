// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Extra parallel sticks for double/triple bonds.
// Uses the same AngleZ/AngleXY as the original stick so direction matches.

using Microsoft.Xna.Framework;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        /// <summary>
        /// Enqueue extra parallel sticks for a double (2) or triple (3) bond.
        /// Direction uses the precomputed bond angles (same as the main stick).
        /// </summary>
        private void EnqueueMultipleBondSticks(
            int orderedZ,
            int bondIndex,
            Vector3 position,
            Matrix scaleRotate,
            float stickRadius,
            float stickFactor,
            float bondAlpha,
            int bondLod,
            Vector3 bondColor)
        {
            if (!Flags.Instance.ShowMultipleBonds) return;
            if (_molecule.atom[orderedZ].BondOrder == null) return;
            if (bondIndex >= _molecule.atom[orderedZ].BondOrder.Length) return;

            int order = _molecule.atom[orderedZ].BondOrder[bondIndex];
            if (order < 2 || order == 4) return; // aromatic uses dashed ring

            int partner = _molecule.atom[orderedZ].BondedAtom[bondIndex];
            if (partner < 0 || partner >= _molecule.NumberOfAtoms) return;

            // Draw from BOTH atoms so each half has its own color + transparency
            // (same as single bonds). Overlap in the middle is intentional.

            float distance = _molecule.atom[orderedZ].Distance[bondIndex];
            if (distance <= 0) return;

            // Use the SAME angles as the original stick (correct direction)
            float angleZ = _molecule.atom[orderedZ].AngleZ[bondIndex];
            float angleXY = _molecule.atom[orderedZ].AngleXY[bondIndex];

            // Perpendicular offset in world space
            Vector3 partnerPos = _molecule.atom[partner].Coordinate;
            Vector3 dir = partnerPos - position;
            float len = dir.Length();
            if (len < 0.05f) return;
            dir /= len;

            Vector3 perp = Vector3.Cross(dir, Vector3.UnitY);
            if (perp.LengthSquared() < 0.01f)
                perp = Vector3.Cross(dir, Vector3.UnitX);
            perp.Normalize();

            float offset = stickRadius * 1.3f;

            // Double: two sticks at ±offset (no center — center is skipped in DrawSticksAtom)
            // Triple: center (already drawn) + ±offset
            if (order == 2)
            {
                EnqueueOffsetStick(position, angleZ, angleXY, perp, offset, distance,
                    scaleRotate, stickRadius, stickFactor, bondAlpha, bondLod, bondColor);
                EnqueueOffsetStick(position, angleZ, angleXY, perp, -offset, distance,
                    scaleRotate, stickRadius, stickFactor, bondAlpha, bondLod, bondColor);
            }
            else if (order == 3)
            {
                EnqueueOffsetStick(position, angleZ, angleXY, perp, offset, distance,
                    scaleRotate, stickRadius, stickFactor, bondAlpha, bondLod, bondColor);
                EnqueueOffsetStick(position, angleZ, angleXY, perp, -offset, distance,
                    scaleRotate, stickRadius, stickFactor, bondAlpha, bondLod, bondColor);
            }
        }

        private void EnqueueOffsetStick(
            Vector3 position,
            float angleZ,
            float angleXY,
            Vector3 perp,
            float offset,
            float distance,
            Matrix scaleRotate,
            float stickRadius,
            float stickFactor,
            float bondAlpha,
            int bondLod,
            Vector3 bondColor)
        {
            // Same orientation as the original stick, just translated perpendicular
            Matrix bondMatrix = Matrix.CreateScale(stickFactor * distance, stickRadius, stickRadius);
            bondMatrix *= Matrix.CreateRotationY(angleZ);
            bondMatrix *= Matrix.CreateRotationZ(angleXY);
            bondMatrix *= Matrix.CreateTranslation(position + perp * offset);
            bondMatrix *= scaleRotate;

            EnqueueBond(bondMatrix, bondColor, bondAlpha, bondLod);
        }
    }
}
