// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldSurface.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Hirshfeld surface extraction result — mesh + per-vertex properties.
 /// </summary>
    public class HirshfeldSurface
    {
 // ── Mesh ──
        public List<Vector3> Vertices = new List<Vector3>();
        public List<int> Indices = new List<int>();
        public List<Vector3> Normals = new List<Vector3>();

 // ⭐ Kopia oryginalnych vertices (before mesh smoothingiem)
        public List<Vector3> VerticesOriginal { get; set; } = new List<Vector3>();

 // ── Per-vertex properties ──

 /// <summary>Distance to the nearest OUTER (outside target) atom.</summary>
        public float[] DE;

 /// <summary>Distance to the nearest INNER (target) atom.</summary>
        public float[] DI;

 /// <summary>Znormalizowana distance: (d_e - r_vdW)/r_vdW + (d_i - r_vdW)/r_vdW.</summary>
        public float[] DNorm;

 /// <summary>Indeks nearest atom externalgo w list outerAtoms.</summary>
        public int[] NearestOuterAtom;

 /// <summary>Indeks nearest atom internalgo w list targetAtoms.</summary>
        public int[] NearestInnerAtom;

 /// <summary>Nazwa nearest atom externalgo (np. "H12").</summary>
        public string[] NearestOuterAtomName;

 /// <summary>Nazwa nearest atom internalgo (np. "O1").</summary>
        public string[] NearestInnerAtomName;

 // ⭐═══════════════════════════════════════════════════════════════
 // ⭐ Charge & Hydrophobicity per-vertex (weighted average)
 // ═══════════════════════════════════════════════════════════════

 /// <summary>
 /// Partial charge at the vertex (weighted average over atoms).
 /// Jednostka: e (electron charge). Typically [-0.5, +0.5].
 /// </summary>
        public float[] Charge;

 /// <summary>
 /// Hydrophobicity at the vertex (weighted average over atoms).
 /// Typically [-0.5, +0.5].
 /// </summary>
        public float[] Hydrophobicity;

 /// <summary>
 /// Zakres charges na tej surface (min, max) — do auto-scaling colouru.
 /// </summary>
        public float ChargeMin = -0.5f;
        public float ChargeMax = +0.5f;

 /// <summary>
 /// Zakres hydrophobicity na tej surface (min, max).
 /// </summary>
        public float HydrophobicityMin = -0.5f;
        public float HydrophobicityMax = +0.5f;

 // ── Metadata ──
        public int TargetMoleculeId;
        public int TargetAtomCount;
        public int OuterAtomCount;

 /// <summary>Geometric centre of the target (Cartesian, same frame as Vertices).</summary>
        public Vector3 Center;

 /// <summary>Radius of the sphere enclosing the surface (from Center).</summary>
        public float BoundingRadius;

 /// <summary>Iso level used in MC (typically 0.5).</summary>
        public float IsoLevel = 0.5f;

 /// <summary>Was ADP anisotropy used?</summary>
        public bool UsedAnisotropic;

 // ⭐═══════════════════════════════════════════════════════════════
 // ⭐ SHIFT KOMPENSACJA
 // ═══════════════════════════════════════════════════════════════
 /// <summary>MoleculeShift w momencie generation surface.</summary>
        public Vector3 ShiftAtGeneration = Vector3.Zero;

        public int VertexCount => Vertices.Count;
        public int TriangleCount => Indices.Count / 3;

        public bool IsValid => Vertices.Count > 0 && Indices.Count > 0;

 // ⭐ Oryginalne values (before color smoothingiem) — do deterministycznego suwaka
        public float[] DNormOriginal;
        public float[] DEOriginal;
        public float[] DIOriginal;

 /// <summary>
 /// Calculates BoundingRadius od Center.
 /// </summary>
        public void ComputeBoundingRadius()
        {
            float r2 = 0f;
            for (int i = 0; i < Vertices.Count; i++)
            {
                float d2 = (Vertices[i] - Center).LengthSquared();
                if (d2 > r2) r2 = d2;
            }
            BoundingRadius = MathF.Sqrt(r2);
            if (BoundingRadius < 0.01f) BoundingRadius = 1.0f;
        }

 /// <summary>
 /// Info debug.
 /// </summary>
        public string GetInfo()
        {
            return $"Surface: {VertexCount} verts, {TriangleCount} tris, " +
                   $"target=#{TargetMoleculeId} ({TargetAtomCount} atoms), " +
                   $"outer={OuterAtomCount} atoms, R={BoundingRadius:F2} Å, " +
                   $"charge=[{ChargeMin:F2},{ChargeMax:F2}], " +
                   $"hydro=[{HydrophobicityMin:F2},{HydrophobicityMax:F2}], " +
                   $"shiftAtGen=({ShiftAtGeneration.X:F2}," +
                   $"{ShiftAtGeneration.Y:F2},{ShiftAtGeneration.Z:F2})";
        }
    }
}