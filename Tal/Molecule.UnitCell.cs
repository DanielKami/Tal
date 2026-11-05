// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — unit-cell geometry.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — przygotowanie ramki cell elementarnej:
// 8 corners + 12 edges + 3 axes (a, b, c) in RGB colours.

using Microsoft.Xna.Framework;

namespace Tal
{
    public partial class Molecule
    {
        #region Unit Cell Preparation

 /// <summary>
 /// Builds the unit cell frame (UnitCellFrame list) — 8 corners and
 /// 12 edges transformed to Cartesian and shifted by MoleculeShift.
 /// Edges along a, b, c are drawn with colors R, G, B;
 /// the rest with light gray.
 /// </summary>
        private void PrepareUnitCell()
        {
            UnitCellFrame.Clear();

            Vector3[] position = new Vector3[8];

            position[0] = new Vector3(0, 0, 0);
            position[1] = new Vector3(1, 0, 0);
            position[2] = new Vector3(0, 1, 0);
            position[3] = new Vector3(0, 0, 1);
            position[4] = new Vector3(1, 1, 0);
            position[5] = new Vector3(1, 0, 1);
            position[6] = new Vector3(1, 1, 1);
            position[7] = new Vector3(0, 1, 1);

            Matrix U = Matrix.Transpose(FractionaltoCarttesianMatrix(0, 0, 0));

            for (int i = 0; i < position.Length; i++)
            {
                position[i] = Vector3.Transform(position[i], U);
                position[i] -= MoleculeShift;
            }

 // ── Axis a (red) ──
            object3d ob = CalcAngles(position[0], position[1]);
            ob.color = new Vector3(0.9f, 0.0f, 0.0f);
            UnitCellFrame.Add(ob);

 // ── Axis b (green) ──
            ob = CalcAngles(position[0], position[2]);
            ob.color = new Vector3(0.0f, 0.9f, 0.0f);
            UnitCellFrame.Add(ob);

 // ── Axis c (blue) ──
            ob = CalcAngles(position[0], position[3]);
            ob.color = new Vector3(0.0f, 0.0f, 0.9f);
            UnitCellFrame.Add(ob);

 // ── Remaining 9 edges (light grey) ──
            AddCellEdge(position[1], position[4]);
            AddCellEdge(position[1], position[5]);
            AddCellEdge(position[4], position[6]);
            AddCellEdge(position[2], position[7]);
            AddCellEdge(position[2], position[4]);
            AddCellEdge(position[3], position[7]);
            AddCellEdge(position[3], position[5]);
            AddCellEdge(position[5], position[6]);
            AddCellEdge(position[6], position[7]);
        }

        #endregion

        #region Edge Construction

 /// <summary>
 /// Adds a cell edge with the default light-gray color.
 /// </summary>
        private void AddCellEdge(Vector3 v1, Vector3 v2)
        {
            object3d ob = CalcAngles(v1, v2);
            ob.color = new Vector3(0.9f, 0.9f, 0.9f);
            UnitCellFrame.Add(ob);
        }

 /// <summary>
 /// Builds an <see cref="object3d"/> describing the segment from v1 to v2:
 /// aftersition, distance, and the two angles used by the renderer.
 /// </summary>
        private object3d CalcAngles(Vector3 v1, Vector3 v2)
        {
            object3d res = new object3d();

            res.position = v1;
            res.distance = Vector3.Distance(v1, v2);
            res.AngleZ = (float)Math.Asin((v1.Z - v2.Z) / res.distance);
            res.AngleXY = -(float)(MathHelperTal.ToRadians(180) - Math.Atan2(v1.Y - v2.Y, v1.X - v2.X));
            return res;
        }

        #endregion
    }
}