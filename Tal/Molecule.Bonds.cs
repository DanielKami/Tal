// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule partial — covalent bonds and bond matrices.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Molecule partial — bonds:
// - Calculate_Bonds3D: covalent bonds w cell (without cross-cell)
// - PrecomputeBondMatrices: ready matrices for drawing sticks
// - ExpandWithNeighbors: dodanie neighbours z cells ±1 (for packing)
// - Calculate_Hbonds3D / ClearHBonds: delegacja do HBondFinder
// - RegisterExpandedAtoms: tracking atoms added by finders

using Microsoft.Xna.Framework;
using System.Diagnostics;
using Tal.Calculate;

namespace Tal
{
    public partial class Molecule
    {
        #region Covalent Bonds

 /// <summary>
 /// Calculates covalent bonds between atoms within the same unit cell
 /// (cross-cell bonds are filtered by CellShift check).
 /// Populates Distance/BondOccupancy/BondedAtom/AngleZ/AngleXY per atom.
 /// </summary>
 /// <summary>
 /// Calculates covalent bonds between atoms within the same unit cell
 /// (cross-cell bonds are filtered by CellShift check).
 /// Populates Distance/BondOccupancy/BondedAtom/AngleZ/AngleXY per atom.
 ///
 /// NEW: H⋯(N/O/F) pairs with distance > 1.3 Å are skipped —
 /// these are hydrogen bonds, handled by HBondFinder.
 /// </summary>
        public void Calculate_Bonds3D()
        {
            for (int i = 0; i < NumberOfAtoms; i++)
            {
                if (atom[i].Distance == null || atom[i].Distance.Length < Flags.Instance.MAX_BONDED_ATOMS)
                {
                    atom[i].Distance = new float[Flags.Instance.MAX_BONDED_ATOMS];
                    atom[i].BondOccupancy = new float[Flags.Instance.MAX_BONDED_ATOMS];
                    atom[i].BondedAtom = new int[Flags.Instance.MAX_BONDED_ATOMS];
                    atom[i].AngleZ = new float[Flags.Instance.MAX_BONDED_ATOMS];
                    atom[i].AngleXY = new float[Flags.Instance.MAX_BONDED_ATOMS];
                }

                atom[i].NrBondedAtoms = 0;
            }

            for (int i = 0; i < NumberOfAtoms; i++)
            {
                int n = 0;
                for (int j = 0; j < NumberOfAtoms; j++)
                {
                    if (i == j) continue;

 // Cross-cell — do not join covalently
                    if (atom[i].CellShiftX != atom[j].CellShiftX ||
                        atom[i].CellShiftY != atom[j].CellShiftY ||
                        atom[i].CellShiftZ != atom[j].CellShiftZ)
                        continue;

                    float distance = Vector3.Distance(atom[i].Coordinate, atom[j].Coordinate);

 // ═══════════════════════════════════════════════════════
 // ⭐ MOSTEK WODOROWY — nie traktuj jako covalent
 //
 // Para H + akceptor (N=7, O=8, F=9):
 // covalent H-O: ~0.96 Å
 // covalent H-N: ~1.01 Å
 // covalent H-F: ~0.92 Å
 // hydrogen bond: 1.5–2.2 Å
 //
 // Threshold 1.3 Å cuts bridges, leaves covalent.
 // ═══════════════════════════════════════════════════════
                    bool isHydrogen_i = (atom[i].AtomicNumber == 1);
                    bool isHydrogen_j = (atom[j].AtomicNumber == 1);

                    if (isHydrogen_i || isHydrogen_j)
                    {
                        int otherZ = isHydrogen_i ? atom[j].AtomicNumber : atom[i].AtomicNumber;
                        bool otherIsAcceptor = (otherZ == 7 || otherZ == 8 || otherZ == 9);

                        if (otherIsAcceptor && distance > 1.3f)
                        {
 // Hydrogen bridge — skip (HBondFinder will handle it)
                            continue;
                        }
                    }

 // ── Normalna logska kowalencyjna ──
                    float bond_corr = Flags.Instance.BOND_FACTOR;
                    int nu = atom[i].AtomicNumber;
                    if (IsMetal(nu))
                        bond_corr = Flags.Instance.BOND_FACTOR_METALS;

                    int mu = atom[j].AtomicNumber;
                    if (IsMetal(mu))
                        bond_corr = Flags.Instance.BOND_FACTOR_METALS;

                    if (atom[i].disorder_group == atom[j].disorder_group ||
                        atom[i].disorder_group == 0 ||
                        atom[j].disorder_group == 0)
                    {
                        if (distance < Flags.Instance.MAX_ATOM_DISTANCE &&
                            distance < (GetVdwRadius(atom[i].AtomicNumber) + GetVdwRadius(atom[j].AtomicNumber)) * bond_corr)
                        {
                            if (n >= Flags.Instance.MAX_BONDED_ATOMS) break;

                            float bond_occ = Math.Min(atom[i].occupancy, atom[j].occupancy);

                            if (atom[i].BondOccupancy == null || atom[i].BondOccupancy.Length <= n)
                            {
                                Array.Resize(ref atom[i].BondOccupancy, n + 1);
                                Array.Resize(ref atom[i].Distance, n + 1);
                                Array.Resize(ref atom[i].BondedAtom, n + 1);
                                Array.Resize(ref atom[i].AngleZ, n + 1);
                                Array.Resize(ref atom[i].AngleXY, n + 1);
                            }

                            atom[i].BondOccupancy[n] = bond_occ;
                            atom[i].Distance[n] = distance;
                            atom[i].BondedAtom[n] = j;
                            atom[i].AngleZ[n] = (float)Math.Asin((atom[i].Coordinate.Z - atom[j].Coordinate.Z) / distance);

                            if (atom[i].Coordinate.X - atom[j].Coordinate.X == 0.0f)
                            {
                                atom[i].AngleXY[n] = 0;
                            }
                            else
                            {
                                atom[i].AngleXY[n] = -(float)(MathHelperTal.ToRadians(180) -
                                    Math.Atan2(atom[i].Coordinate.Y - atom[j].Coordinate.Y,
                                               atom[i].Coordinate.X - atom[j].Coordinate.X));
                            }
                            n++;
                        }
                    }
                }
                atom[i].NrBondedAtoms = n;
            }

            Debug.WriteLine($"[Bonds] Calculated for {NumberOfAtoms} atoms (cross-cell filtered, H-bonds excluded)");

            // Bond orders + aromatic rings (used by Display → Bond Display)
            BondOrderAnalyzer.Analyze(this);
        }

        #endregion

        #region Metal Detection

 /// <summary>
 /// Returns true for elements treated as metals for bond-factor selection.
 /// </summary>
        private bool IsMetal(int atomicNumber)
        {
            return atomicNumber >= 3 && atomicNumber <= 4 ||
                   atomicNumber >= 11 && atomicNumber <= 14 ||
                   atomicNumber >= 19 && atomicNumber <= 30 ||
                   atomicNumber >= 37 && atomicNumber <= 50 ||
                   atomicNumber >= 55 && atomicNumber <= 83 ||
                   atomicNumber >= 87 && atomicNumber <= 118;
        }

        #endregion

        #region Hydrogen Bonds (delegation)

 /// <summary>
 /// Calculates hydrogen bonds. Delegates to <see cref="HBondFinder"/>.
 /// </summary>
        public void Calculate_Hbonds3D()
        {
            HBondFinder.Calculate(this);
        }

 /// <summary>
 /// Clears all H-bond data. Delegates to <see cref="HBondFinder"/>.
 /// </summary>
        private void ClearHBonds()
        {
            HBondFinder.Clear(this);
        }

        #endregion

        #region Expanded Atoms Tracking

 /// <summary>
 /// Registers indices of atoms added by Expand/Short Contacts
 /// (for later selective clearing without removing them).
 /// </summary>
        private void RegisterExpandedAtoms(int fromIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                ExpandedAtomIndices.Add(fromIndex + i);
            }
            Debug.WriteLine($"[Molecule] Registered {count} expanded atoms " +
                            $"(total in list: {ExpandedAtomIndices.Count})");
        }

        #endregion

        #region Expand With Neighbors

 /// <summary>
 /// Expands the 3D structure with all atoms from neighboring cells (±1 on each axis).
 /// Used for packing mode. Atoms are added with proper CellShift, and the bond list
 /// is recalculated.
 /// </summary>
        public void ExpandWithNeighbors()
        {
            if (SymetryEquivalents == null || SymetryEquivalents.Count == 0) return;

            bool alreadyExpanded = false;
            for (int i = 0; i < NumberOfAtoms; i++)
            {
                if (atom[i].CellShiftX != 0 || atom[i].CellShiftY != 0 || atom[i].CellShiftZ != 0)
                {
                    alreadyExpanded = true;
                    break;
                }
            }
            if (alreadyExpanded) return;

            Matrix U = Matrix.Transpose(FractionaltoCarttesianMatrix(0, 0, 0));
            Vector3 shift = MoleculeShift;
            int style = Flags.Instance.Presentation;

            var newAtoms = new List<AtomSimple>();

            foreach (var src in SymetryEquivalents)
            {
                for (int tx = -1; tx <= 1; tx++)
                    for (int ty = -1; ty <= 1; ty++)
                        for (int tz = -1; tz <= 1; tz++)
                        {
                            if (tx == 0 && ty == 0 && tz == 0) continue;

                            float fX = src.x + tx;
                            float fY = src.y + ty;
                            float fZ = src.z + tz;

                            Vector3 cartPos = Vector3.Transform(new Vector3(fX, fY, fZ), U) - shift;

                            bool exists = false;
                            for (int i = 0; i < NumberOfAtoms; i++)
                            {
                                if (Vector3.DistanceSquared(atom[i].Coordinate, cartPos) < 0.01f)
                                {
                                    exists = true;
                                    break;
                                }
                            }
                            if (exists) continue;

                            var newAtom = src;
                            newAtom.x = fX;
                            newAtom.y = fY;
                            newAtom.z = fZ;
                            newAtom.CellShiftX = tx;
                            newAtom.CellShiftY = ty;
                            newAtom.CellShiftZ = tz;
                            newAtoms.Add(newAtom);
                        }
            }

            if (newAtoms.Count == 0) return;

            int oldCount = NumberOfAtoms;
            var newArray = new Atom[oldCount + newAtoms.Count];
            Array.Copy(atom, newArray, oldCount);
            atom = newArray;

            foreach (var na in newAtoms)
            {
                int idx = NumberOfAtoms;

                atom[idx].Distance = new float[Flags.Instance.MAX_BONDED_ATOMS];
                atom[idx].BondOccupancy = new float[Flags.Instance.MAX_BONDED_ATOMS];
                atom[idx].BondedAtom = new int[Flags.Instance.MAX_BONDED_ATOMS];
                atom[idx].AngleZ = new float[Flags.Instance.MAX_BONDED_ATOMS];
                atom[idx].AngleXY = new float[Flags.Instance.MAX_BONDED_ATOMS];
                atom[idx].NrBondedAtoms = 0;

                atom[idx].H_Distance = new float[Flags.Instance.MAX_H_BONDED_ATOMS];
                atom[idx].H_AngleZ = new float[Flags.Instance.MAX_H_BONDED_ATOMS];
                atom[idx].H_AngleXY = new float[Flags.Instance.MAX_H_BONDED_ATOMS];
                atom[idx].H_BondedAtom = new int[Flags.Instance.MAX_H_BONDED_ATOMS];
                atom[idx].H_NrBondedAtoms = 0;

                Vector3 cartPos = Vector3.Transform(new Vector3(na.x, na.y, na.z), U) - shift;

                atom[idx].Coordinate = cartPos;
                atom[idx].AtomicNumber = na.AtomicNumber;
                atom[idx].name = na.name;
                atom[idx].style = style;
                atom[idx].occupancy = na.occupancy;
                atom[idx].show = true;
                atom[idx].selected = false;
                atom[idx].Coordinate_error = new Vector3(na.x_e, na.y_e, na.z_e);
                atom[idx].Ue = na.Ue;
                atom[idx].Un = na.Un;
                atom[idx].Transformation = na.Transformation;
                atom[idx].disorder_group = na.disorder_group;
                atom[idx].charge = na.charge;
                atom[idx].Hydrophobicity = na.Hydrophobicity;
                atom[idx].isAnisotropic = na.isAnisotropic;
                atom[idx].SymmetryEquivalentNumberInCell = na.SymmetryEquivalentNumberInCell;
                atom[idx].IndividualMoleculeInCell = na.IndividualMoleculeInCell;
                atom[idx].CustomColor = new Vector3(-1f, -1f, -1f);
                atom[idx].CellShiftX = na.CellShiftX;
                atom[idx].CellShiftY = na.CellShiftY;
                atom[idx].CellShiftZ = na.CellShiftZ;

                atom[idx].UseForHBonds = true;
                atom[idx].UseForShortContacts = true;

                NumberOfAtoms++;
            }

            Calculate_Bonds3D();

            Debug.WriteLine($"[ExpandWithNeighbors] {oldCount} → {NumberOfAtoms} atoms");
        }

        #endregion

        #region Precomputed Matrices

 /// <summary>
 /// Precomputes per-bond and per-H-bond matrices for drawing sticks.
 /// Must be called after Calculate_Bonds3D / Calculate_Hbonds3D.
 /// </summary>
        public void PrecomputeBondMatrices()
        {
            if (atom == null || NumberOfAtoms == 0) return;

            for (int i = 0; i < NumberOfAtoms; i++)
            {
                if (atom[i].NrBondedAtoms > 0)
                {
                    if (atom[i].PrecomputedBondMatrix == null ||
                        atom[i].PrecomputedBondMatrix.Length < atom[i].NrBondedAtoms)
                    {
                        atom[i].PrecomputedBondMatrix = new Matrix[Flags.Instance.MAX_BONDED_ATOMS];
                    }

                    for (int j = 0; j < atom[i].NrBondedAtoms; j++)
                    {
                        if (atom[i].Distance[j] <= 0)
                        {
                            atom[i].PrecomputedBondMatrix[j] = Matrix.Identity;
                            continue;
                        }

                        float scaleX = 0.0127f * atom[i].Distance[j];
                        float scaleY = 0.1f;
                        float scaleZ = 0.1f;

                        atom[i].PrecomputedBondMatrix[j] =
                            Matrix.CreateScale(scaleX, scaleY, scaleZ) *
                            Matrix.CreateRotationY(atom[i].AngleZ[j]) *
                            Matrix.CreateRotationZ(atom[i].AngleXY[j]);
                    }
                }

                if (atom[i].H_NrBondedAtoms > 0)
                {
                    if (atom[i].PrecomputedHBondMatrix == null ||
                        atom[i].PrecomputedHBondMatrix.Length < atom[i].H_NrBondedAtoms)
                    {
                        atom[i].PrecomputedHBondMatrix = new Matrix[Flags.Instance.MAX_H_BONDED_ATOMS];
                    }

                    for (int j = 0; j < atom[i].H_NrBondedAtoms; j++)
                    {
                        if (atom[i].H_Distance == null || atom[i].H_Distance[j] <= 0)
                        {
                            atom[i].PrecomputedHBondMatrix[j] = Matrix.Identity;
                            continue;
                        }

                        float scaleX = 0.0127f * atom[i].H_Distance[j];
                        float scaleY = 0.05f;
                        float scaleZ = 0.05f;

                        atom[i].PrecomputedHBondMatrix[j] =
                            Matrix.CreateScale(scaleX, scaleY, scaleZ) *
                            Matrix.CreateRotationY(atom[i].H_AngleZ[j]) *
                            Matrix.CreateRotationZ(atom[i].H_AngleXY[j]);
                    }
                }
                else
                {
                    atom[i].PrecomputedHBondMatrix = null;
                }
            }
        }

 /// <summary>
 /// Clears all precomputed matrices (used when structure changes).
 /// </summary>
        public void ClearPrecomputedMatrices()
        {
            for (int i = 0; i < NumberOfAtoms; i++)
            {
                atom[i].PrecomputedBondMatrix = null;
                atom[i].PrecomputedHBondMatrix = null;
            }
        }

        #endregion
    }
}