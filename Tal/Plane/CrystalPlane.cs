// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal planes module: CrystalPlane.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Drawing;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Tal.Core.PowderDiffraction
{
    public enum CrystalPlaneType
    {
        MeanPlane,
        HKL
    }

    public enum AtomSelectionMode
    {
        PickedAtom,
        PickedMolecule,
        Ring,
        All,
        NonHydrogen,
        Hydrogen
    }

 /// <summary>
 /// Represents a crystallographic plane in 3D space
 /// z gotowymi punktami do drawing.
 /// </summary>
    public class CrystalPlane
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Plane";
        public CrystalPlaneType Type { get; set; }
        public System.Drawing.Color Color { get; set; } = System.Drawing.Color.Red;
        public bool Visible { get; set; } = true;
        public bool ShowLabel { get; set; } = true;
        public bool Transparent { get; set; } = true;

 // For MeanPlane
        public AtomSelectionMode SelectionMode { get; set; } = AtomSelectionMode.PickedMolecule;
        public List<int> AtomIndices { get; set; } = new List<int>();
        public int AtomCount => AtomIndices.Count;

 // For HKL
        public int H { get; set; }
        public int K { get; set; }
        public int L { get; set; }

 // Równanie planes: ax + by + cz + d = 0
        public double A { get; set; }
        public double B { get; set; }
        public double C { get; set; }
        public double D { get; set; }

 // Plane centre (Cartesian coordinates, shifted by -MoleculeShift)
        public Vector3 Center { get; set; }

 // 4 corners rectangle do drawing (coordinates Cartesian, shifted)
        public Vector3[] Corners { get; set; } = new Vector3[4];

 // Rozmiar planes (radius rectangle)
        public float Size { get; set; } = 10f;

        public string GetDescription()
        {
            if (Type == CrystalPlaneType.HKL)
                return $"hkl: ({H} {K} {L})";

            string source = SelectionMode switch
            {
                AtomSelectionMode.PickedAtom => "picked atom",
                AtomSelectionMode.PickedMolecule => "molecule",
                AtomSelectionMode.Ring => "ring",
                AtomSelectionMode.All => "all atoms",
                AtomSelectionMode.NonHydrogen => "non-hydrogen atoms",
                AtomSelectionMode.Hydrogen => "hydrogen atoms",
                _ => "atoms"
            };

            return $"{source}: {AtomCount} atoms";
        }

        public override string ToString() => $"{Name} - {GetDescription()}";
    }
}