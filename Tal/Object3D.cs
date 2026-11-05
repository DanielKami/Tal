// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Base 3D object helpers.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;

namespace Tal
{
 /// <summary>
 /// Represents a 3D segment (unit-cell edge) — position, orientation and length.
 /// Used w <see cref="Molecule.UnitCellFrame"/> do drawing ramki cell.
 /// </summary>
    public class object3d
    {
 /// <summary>Punkt startowy segmentu (coordinates Cartesian).</summary>
        public Vector3 position = new Vector3();

 /// <summary>Rotation angle around the Z axis (radians).</summary>
        public float AngleZ;

 /// <summary>Rotation angle in the XY plane (radians).</summary>
        public float AngleXY;

 /// <summary>Length segmentu w Å.</summary>
        public float distance;

 /// <summary>Kolor segmentu (RGB 0-1).</summary>
        public Vector3 color;
    }
}