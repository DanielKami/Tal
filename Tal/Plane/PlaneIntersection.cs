// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal planes module: PlaneIntersection.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;

namespace Tal.Core.PowderDiffraction
{
 /// <summary>
 /// Linia przecięcia dwóch planes w przestrzeni 3D.
 /// </summary>
    public struct PlaneIntersection
    {
 /// <summary>Punkt na linii (dowolny).</summary>
        public Vector3 Point;

 /// <summary>Znormalizowany kierunek linii.</summary>
        public Vector3 Direction;

 /// <summary>Segment half-length for drawing (od point w obu kierunkach).</summary>
        public float HalfLength;

 /// <summary>
 /// Returns true if linia jest prawidłowa (planes nie są równoległe).
 /// </summary>
        public bool IsValid => Direction.LengthSquared() > 1e-9f;
    }
}
