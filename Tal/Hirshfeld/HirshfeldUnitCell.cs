// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldUnitCell.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HirshfeldUnitCell — INDEPENDENT unit-cell representation.
//
// ⭐ v14:
// - CellAtom ma areas Charge i Hydrophobicity (kopiowane z AtomSimple).
// - Per-op translation + dedup (jak v13).
//
// ⭐ Cartesian coordinates (CellAtom.Cart) are in the RAW frame:
// Cart = FracToCart(frac) (BEZ -MoleculeShift)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Independent unit-cell representation for Hirshfeld analysis.
 /// Built solely from AsymmetricPart + SymmetryOperations.
 /// Does NOT depend on the UI presentation mode or MoleculeShift.
 /// </summary>
    public class HirshfeldUnitCell
    {
        #region Nested types

        public struct CellAtom
        {
            public Vector3 Frac;      // frakcjonalne (po przesunięciu — mogą być poza [0,1))
            public Vector3 Cart;      // kartezjańskie RAW: FracToCart(Frac)
            public int AtomicNumber;
            public string Name;
            public bool IsAnisotropic;
            public Matrix Un;
            public Matrix Ue;
            public float Occupancy;
            public int DisorderGroup;
            public int SymmOpIndex;      // która operacja symetrii
            public int SourceAsymIndex;  // który atom w AsymmetricPart

 // ⭐ v12 areas (setiane przez HirshfeldScene)
            public int MoleculeId;       // numer fragmentu z MoleculeFinder (-1 = nieprzypisany)
            public bool IsHost;          // czy belongs to molekuły target

 // ⭐ v14 areas (kopiowane z AtomSimple)
            public float Charge;
            public float Hydrophobicity;
        }

        #endregion

        #region Constants

        private const float DEDUP_TOL_FRAC = 0.005f;
        private const float DEDUP_TOL_FRAC_SQ = DEDUP_TOL_FRAC * DEDUP_TOL_FRAC;

        #endregion

        #region Fields

        private readonly Molecule _molecule;
        private readonly Matrix _fracToCart;

        private readonly List<CellAtom> _atoms = new List<CellAtom>();

        public float CellLenA { get; private set; }
        public float CellLenB { get; private set; }
        public float CellLenC { get; private set; }

        #endregion

        #region Properties

        public IReadOnlyList<CellAtom> Atoms => _atoms;
        public int Count => _atoms.Count;
        public bool IsValid => _atoms.Count > 0;
        public Matrix FracToCart => _fracToCart;

        #endregion

        #region Constructor

        public HirshfeldUnitCell(Molecule molecule)
        {
            _molecule = molecule ?? throw new ArgumentNullException(nameof(molecule));

            var A = molecule.FractionaltoCarttesianMatrix(0, 0, 0);
            _fracToCart = Matrix.Transpose(A);

            var vecA = new Vector3(A.M11, A.M21, A.M31);
            var vecB = new Vector3(A.M12, A.M22, A.M32);
            var vecC = new Vector3(A.M13, A.M23, A.M33);
            CellLenA = Math.Max(vecA.Length(), 0.1f);
            CellLenB = Math.Max(vecB.Length(), 0.1f);
            CellLenC = Math.Max(vecC.Length(), 0.1f);

            Build();
        }

        #endregion

        #region Build

        private void Build()
        {
            _atoms.Clear();

            var asym = _molecule.AsymmetricPart;
            var ops = _molecule.SymmetryOperations;

            if (asym == null || asym.Count == 0)
            {
                Debug.WriteLine("[HirshfeldUnitCell] AsymmetricPart empty");
                return;
            }

            if (ops == null || ops.Count == 0)
            {
                Debug.WriteLine("[HirshfeldUnitCell] SymmetryOperations empty — " +
                                "using AsymmetricPart as-is (P1)");

                for (int i = 0; i < asym.Count; i++)
                {
                    var a = asym[i];
                    var frac = new Vector3(a.x, a.y, a.z);
                    var cart = Vector3.Transform(frac, _fracToCart);

                    _atoms.Add(new CellAtom
                    {
                        Frac = frac,
                        Cart = cart,
                        AtomicNumber = a.AtomicNumber,
                        Name = a.name,
                        IsAnisotropic = a.isAnisotropic,
                        Un = a.Un,
                        Ue = a.Ue,
                        Occupancy = a.occupancy,
                        DisorderGroup = a.disorder_group,
                        SymmOpIndex = 0,
                        SourceAsymIndex = i,
                        MoleculeId = -1,
                        IsHost = false,
 // ⭐ v14
                        Charge = a.charge,
                        Hydrophobicity = a.Hydrophobicity
                    });
                }

                Debug.WriteLine($"[HirshfeldUnitCell] Built {_atoms.Count} atoms (no symm)");
                return;
            }

 // ═══════════════════════════════════════════════════════════
 // STEP 1: Generate AsymmetricPart × ops (without shifts)
 // ═══════════════════════════════════════════════════════════
            int rawCount = 0;
            int skippedDuplicates = 0;

            for (int i = 0; i < asym.Count; i++)
            {
                var a = asym[i];

                float ax = a.x;
                float ay = a.y;
                float az = a.z;

                for (int opIdx = 0; opIdx < ops.Count; opIdx++)
                {
                    var m = ops[opIdx];

                    float[] src = { ax, ay, az };

                    float fx = src[m.pos1[0]] * m.sign1[0]
                             + src[m.pos2[0]] * m.sign2[0]
                             + m.shift[0];

                    float fy = src[m.pos1[1]] * m.sign1[1]
                             + src[m.pos2[1]] * m.sign2[1]
                             + m.shift[1];

                    float fz = src[m.pos1[2]] * m.sign1[2]
                             + src[m.pos2[2]] * m.sign2[2]
                             + m.shift[2];

                    Vector3 frac = new Vector3(fx, fy, fz);

                    rawCount++;

                    if (IsDuplicate(frac))
                    {
                        skippedDuplicates++;
                        continue;
                    }

                    var cart = Vector3.Transform(frac, _fracToCart);

                    _atoms.Add(new CellAtom
                    {
                        Frac = frac,
                        Cart = cart,
                        AtomicNumber = a.AtomicNumber,
                        Name = a.name,
                        IsAnisotropic = a.isAnisotropic,
                        Un = a.Un,
                        Ue = a.Ue,
                        Occupancy = a.occupancy,
                        DisorderGroup = a.disorder_group,
                        SymmOpIndex = opIdx,
                        SourceAsymIndex = i,
                        MoleculeId = -1,
                        IsHost = false,
 // ⭐ v14
                        Charge = a.charge,
                        Hydrophobicity = a.Hydrophobicity
                    });
                }
            }

            Debug.WriteLine(
                $"[HirshfeldUnitCell] Raw generation: {_atoms.Count} atoms " +
                $"(raw={rawCount}, dups={skippedDuplicates}, " +
                $"asym={asym.Count}, ops={ops.Count})");

 // ═══════════════════════════════════════════════════════════
 // KROK 2: Jedna translation per OPERACJA.
 // ═══════════════════════════════════════════════════════════
            int totalMoved = 0;

            int anchorSourceIdx = -1;
            Vector3 anchorFrac = Vector3.Zero;
            for (int i = 0; i < _atoms.Count; i++)
            {
                if (_atoms[i].SymmOpIndex == 0 && _atoms[i].AtomicNumber != 1)
                {
                    anchorSourceIdx = _atoms[i].SourceAsymIndex;
                    anchorFrac = _atoms[i].Frac;
                    break;
                }
            }

            if (anchorSourceIdx < 0)
            {
                Debug.WriteLine("[HirshfeldUnitCell] No anchor found — skipping per-op translation");
            }
            else
            {
                var transByOp = new Dictionary<int, Vector3>();
                transByOp[0] = Vector3.Zero;

                for (int i = 0; i < _atoms.Count; i++)
                {
                    var atom = _atoms[i];
                    if (atom.SymmOpIndex == 0) continue;
                    if (atom.SourceAsymIndex != anchorSourceIdx) continue;

                    float dfx = atom.Frac.X - anchorFrac.X;
                    float dfy = atom.Frac.Y - anchorFrac.Y;
                    float dfz = atom.Frac.Z - anchorFrac.Z;

                    float tx = (float)Math.Round(dfx, MidpointRounding.AwayFromZero);
                    float ty = (float)Math.Round(dfy, MidpointRounding.AwayFromZero);
                    float tz = (float)Math.Round(dfz, MidpointRounding.AwayFromZero);

                    transByOp[atom.SymmOpIndex] = new Vector3(tx, ty, tz);
                }

                for (int i = 0; i < _atoms.Count; i++)
                {
                    var atom = _atoms[i];
                    if (atom.SymmOpIndex == 0) continue;

                    if (!transByOp.TryGetValue(atom.SymmOpIndex, out var t)) continue;

                    bool needsMove = Math.Abs(t.X) > 1e-6f
                                  || Math.Abs(t.Y) > 1e-6f
                                  || Math.Abs(t.Z) > 1e-6f;
                    if (!needsMove) continue;

                    Vector3 newFrac = new Vector3(
                        atom.Frac.X - t.X,
                        atom.Frac.Y - t.Y,
                        atom.Frac.Z - t.Z);

                    Vector3 newCart = Vector3.Transform(newFrac, _fracToCart);

                    atom.Frac = newFrac;
                    atom.Cart = newCart;
                    _atoms[i] = atom;

                    totalMoved++;
                }

                Debug.WriteLine($"[HirshfeldUnitCell] Per-op translation applied " +
                                $"(moved={totalMoved}, ops={transByOp.Count})");
            }

 // ═══════════════════════════════════════════════════════════
 // STEP 3: Dedup after shift.
 // ═══════════════════════════════════════════════════════════
            var deduped = new List<CellAtom>(_atoms.Count);
            int postDedupRemoved = 0;

            for (int i = 0; i < _atoms.Count; i++)
            {
                var atom = _atoms[i];
                if (IsDuplicateInList(deduped, atom.Frac))
                {
                    postDedupRemoved++;
                    continue;
                }
                deduped.Add(atom);
            }

            _atoms.Clear();
            _atoms.AddRange(deduped);

            Debug.WriteLine(
                $"[HirshfeldUnitCell] Built {_atoms.Count} atoms " +
                $"(raw={rawCount}, dups_gen={skippedDuplicates}, " +
                $"moved={totalMoved}, dups_post={postDedupRemoved}, " +
                $"asym={asym.Count}, ops={ops.Count})");
        }

        #endregion

        #region Dedup

        private bool IsDuplicate(Vector3 frac)
        {
            for (int i = 0; i < _atoms.Count; i++)
            {
                float dx = frac.X - _atoms[i].Frac.X;
                float dy = frac.Y - _atoms[i].Frac.Y;
                float dz = frac.Z - _atoms[i].Frac.Z;

                dx -= MathF.Round(dx);
                dy -= MathF.Round(dy);
                dz -= MathF.Round(dz);

                float d2 = dx * dx + dy * dy + dz * dz;
                if (d2 < DEDUP_TOL_FRAC_SQ)
                    return true;
            }
            return false;
        }

        private static bool IsDuplicateInList(List<CellAtom> list, Vector3 frac)
        {
            for (int i = 0; i < list.Count; i++)
            {
                float dx = frac.X - list[i].Frac.X;
                float dy = frac.Y - list[i].Frac.Y;
                float dz = frac.Z - list[i].Frac.Z;

                float d2 = dx * dx + dy * dy + dz * dz;
                if (d2 < DEDUP_TOL_FRAC_SQ)
                    return true;
            }
            return false;
        }

        #endregion

        #region Debug

        public string GetInfo()
        {
            return $"HirshfeldUnitCell: {_atoms.Count} atoms, " +
                   $"cell=({CellLenA:F2},{CellLenB:F2},{CellLenC:F2}) Å";
        }

        #endregion
    }
}