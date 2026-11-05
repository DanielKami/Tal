// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Shared math helpers (angles, matrices, projections).

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

namespace Tal
{
 /// <summary>
 /// Math helper — konwersje stopnie ↔ radiany.
 /// Used w Camera, Measure, Lasso, Molecule i innych.
 /// </summary>
    public static class MathHelperTal
    {
 /// <summary>π (float).</summary>
        public const float Pi = (float)Math.PI;

 /// <summary>π/2 (float).</summary>
        public const float PiOver2 = Pi / 2.0f;

 /// <summary>Converte stopnie na radiany.</summary>
        public static float ToRadians(float degrees)
            => degrees * (float)(Math.PI / 180.0);

 /// <summary>Converte radiany na stopnie.</summary>
        public static float ToDegrees(float radians)
            => radians * (float)(180.0 / Math.PI);
    }
}