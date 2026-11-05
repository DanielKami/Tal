// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldScene.

﻿// ═══════════════════════════════════════════════════════════════════════════
// HIRSHFELD SURFACE — PIPELINE OVERVIEW
// ═══════════════════════════════════════════════════════════════════════════
//
// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// ───────────────────────────────────────────────────────────────────────────
// PURPOSE
// ───────────────────────────────────────────────────────────────────────────
// Compute the Hirshfeld surface of a selected molecule (promolecule / host)
// in a crystal. The surface is the isosurface where the promolecule's
// electron density equals the density of all surrounding atoms:
//
// w(r) = rho_promolecule(r) / ( rho_promolecule(r) + rho_outer(r) ) = 0.5
//
// ⭐ v14:
// - SceneAtom ma areas Charge + Hydrophobicity (kopiowane z CellAtom).
// - CollectTargetAtoms i CollectNeighbors propagate these fields.
// - Zachowane v13: pre-kalkulacja cellAtomsCart + translations.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Tal.Calculate;

namespace Tal.Hirshfeld
{
    #region Public Types

    public class MoleculeFragment
    {
        public int FragmentId;
        public List<int> AtomIndices = new List<int>();
        public Vector3 CenterCart;
        public Vector3 BBoxMin, BBoxMax;
        public string Label;
        public int AtomCount => AtomIndices.Count;
    }

    public struct SceneAtom
    {
        public int SymmetryAtomIndex;
        public int CellShiftX, CellShiftY, CellShiftZ;
        public Vector3 FracPosition;
        public Vector3 CartPosition;
        public int AtomicNumber;
        public bool IsTarget;
        public bool IsAnisotropic;
        public Matrix Un;
        public float Occupancy;
        public string Name;
        public int MoleculeId;
        public int SourceAsymIndex;

 // ⭐ v14: areas propagowane z CellAtom
        public float Charge;
        public float Hydrophobicity;
    }

    #endregion

    public class HirshfeldScene
    {
        #region Configuration

        public const int MAX_CELL_RANGE_PER_AXIS = 8;

        #endregion

        #region Fields

        private readonly Molecule _molecule;
        private readonly Matrix _fracToCart;

        private readonly HirshfeldUnitCell _cell;

 // ⭐ Mutowalna kopia _cell.Atoms — z MoleculeId / IsHost
        private List<HirshfeldUnitCell.CellAtom> _workingAtoms;

        private List<MoleculeFragment> _fragments;
        private readonly List<SceneAtom> _targetAtoms = new List<SceneAtom>();
        private readonly List<SceneAtom> _outerAtoms = new List<SceneAtom>();

 // ⭐ Dedup by atom identity (not by positions)
        private readonly HashSet<(int mol, int src, int op, int tx, int ty, int tz)>
            _seenIdentities = new HashSet<(int, int, int, int, int, int)>();

        private float _cellA, _cellB, _cellC;
        private float _lastCutoff;
        private int _targetMoleculeId = -1;

        #endregion

        #region Properties

        public IReadOnlyList<MoleculeFragment> Fragments => _fragments;
        public IReadOnlyList<SceneAtom> TargetAtoms => _targetAtoms;
        public IReadOnlyList<SceneAtom> OuterAtoms => _outerAtoms;

        public Vector3 TargetCenter { get; private set; }
        public Vector3 TargetBBoxMin { get; private set; }
        public Vector3 TargetBBoxMax { get; private set; }
        public float TargetRadius { get; private set; }
        public int TargetMoleculeId { get; private set; }
        public int LastNeighborCount => _outerAtoms.Count;
        public float LastCutoff => _lastCutoff;

        #endregion

        #region Constructor

        public HirshfeldScene(Molecule molecule)
        {
            _molecule = molecule ?? throw new ArgumentNullException(nameof(molecule));

            var A = molecule.FractionaltoCarttesianMatrix(0, 0, 0);
            _fracToCart = Matrix.Transpose(A);

            _cellA = Math.Max(new Vector3(A.M11, A.M21, A.M31).Length(), 0.1f);
            _cellB = Math.Max(new Vector3(A.M12, A.M22, A.M32).Length(), 0.1f);
            _cellC = Math.Max(new Vector3(A.M13, A.M23, A.M33).Length(), 0.1f);

            _cell = new HirshfeldUnitCell(molecule);

            Debug.WriteLine($"[HirshfeldScene] Cell lengths: " +
                            $"a={_cellA:F3}, b={_cellB:F3}, c={_cellC:F3}");
            Debug.WriteLine($"[HirshfeldScene] {_cell.GetInfo()}");
        }

        #endregion

        #region Utilities

        private Vector3 FracToCart(Vector3 frac)
            => Vector3.Transform(frac, _fracToCart);

        #endregion

        #region Public API

        public bool Build(int targetMoleculeId, float cutoff = 5.0f)
        {
            _lastCutoff = cutoff;

            Debug.WriteLine($"[HirshfeldScene] ═══ Build START (v14) ═══");
            Debug.WriteLine($"[HirshfeldScene] target={targetMoleculeId}, cutoff={cutoff}");

            var sw = Stopwatch.StartNew();

            if (!_cell.IsValid)
            {
                Debug.WriteLine("[HirshfeldScene] ❌ UnitCell is empty");
                return false;
            }

            _workingAtoms = new List<HirshfeldUnitCell.CellAtom>(_cell.Atoms);

            if (!ExtractMolecules())
            {
                Debug.WriteLine("[HirshfeldScene] ❌ ExtractMolecules failed");
                return false;
            }

            Debug.WriteLine($"[HirshfeldScene] Detected {_fragments.Count} fragments");

            var target = _fragments.FirstOrDefault(f => f.FragmentId == targetMoleculeId);
            if (target == null)
            {
                Debug.WriteLine($"[HirshfeldScene] ❌ Fragment #{targetMoleculeId} not found");
                return false;
            }

            _targetMoleculeId = target.FragmentId;
            TargetMoleculeId = target.FragmentId;

            MarkHostAtoms(target);
            RefreshFragmentGeometry(target);

            TargetCenter = target.CenterCart;
            TargetBBoxMin = target.BBoxMin;
            TargetBBoxMax = target.BBoxMax;

            CollectTargetAtoms(target);

            _seenIdentities.Clear();
            CollectNeighbors(cutoff);

            float rmax = 0f;
            foreach (var atom in _targetAtoms)
            {
                float d = (atom.CartPosition - TargetCenter).Length();
                if (d > rmax) rmax = d;
            }
            TargetRadius = rmax;

            Debug.WriteLine($"[HirshfeldScene] ✅ Scene built ({sw.ElapsedMilliseconds} ms): " +
                            $"target={_targetAtoms.Count} atoms, " +
                            $"outer={_outerAtoms.Count} atoms, " +
                            $"R={TargetRadius:F2} Å");

            return true;
        }

        #endregion

        #region Step 1 — Molecule extraction

        private bool ExtractMolecules()
        {
            if (_cell.Count == 0)
            {
                Debug.WriteLine("[HirshfeldScene] UnitCell empty");
                return false;
            }

            int n = _cell.Count;

            var atomSimples = new List<Molecule.AtomSimple>(n);
            for (int i = 0; i < n; i++)
            {
                var c = _cell.Atoms[i];
                atomSimples.Add(new Molecule.AtomSimple
                {
                    x = c.Frac.X,
                    y = c.Frac.Y,
                    z = c.Frac.Z,
                    AtomicNumber = c.AtomicNumber,
                    name = c.Name,
                    Un = c.Un,
                    Ue = c.Ue,
                    isAnisotropic = c.IsAnisotropic,
                    occupancy = c.Occupancy,
                    disorder_group = c.DisorderGroup,
                    IndividualMoleculeInCell = -1,
                    charge = c.Charge,
                    Hydrophobicity = c.Hydrophobicity
                });
            }

            var fragments = MoleculeFinder.Find(
                atomSimples,
                _fracToCart,
                resetIndividualIds: true,
                donorSearchList: null,
                useMinimumImage: true,
                log: msg => Debug.WriteLine(msg));

            if (fragments.Count == 0)
            {
                Debug.WriteLine("[HirshfeldScene] MoleculeFinder returned 0 fragments");
                return false;
            }

            var result = new List<MoleculeFragment>(fragments.Count);

            foreach (var f in fragments)
            {
                var mf = new MoleculeFragment
                {
                    FragmentId = f.FragmentId,
                    AtomIndices = new List<int>(f.AtomIndices),
                    CenterCart = f.CenterCart,
                    BBoxMin = f.BBoxMin,
                    BBoxMax = f.BBoxMax,
                    Label = $"Fragment #{f.FragmentId} ({f.AtomIndices.Count} atoms)"
                };
                result.Add(mf);

                foreach (int idx in f.AtomIndices)
                {
                    var atom = _workingAtoms[idx];
                    atom.MoleculeId = f.FragmentId;
                    _workingAtoms[idx] = atom;
                }

                Debug.WriteLine($"[HirshfeldScene]   Fragment #{f.FragmentId}: " +
                                $"{f.AtomIndices.Count} atoms, " +
                                $"center=({f.CenterCart.X:F2}," +
                                $"{f.CenterCart.Y:F2}," +
                                $"{f.CenterCart.Z:F2})");
            }

            _fragments = result;
            return true;
        }

        #endregion

        #region Step 1.5 — Mark host atoms

        private void MarkHostAtoms(MoleculeFragment target)
        {
            for (int i = 0; i < _workingAtoms.Count; i++)
            {
                var a = _workingAtoms[i];
                a.IsHost = false;
                _workingAtoms[i] = a;
            }

            foreach (int idx in target.AtomIndices)
            {
                var a = _workingAtoms[idx];
                a.IsHost = true;
                _workingAtoms[idx] = a;
            }

            Debug.WriteLine($"[HirshfeldScene] Marked {target.AtomIndices.Count} host atoms");
        }

        #endregion

        #region Step 1.6 — Refresh fragment geometry

        private void RefreshFragmentGeometry(MoleculeFragment fragment)
        {
            if (fragment == null || fragment.AtomCount == 0) return;

            Vector3 sum = Vector3.Zero;
            Vector3 bmin = new Vector3(float.MaxValue);
            Vector3 bmax = new Vector3(float.MinValue);

            foreach (int idx in fragment.AtomIndices)
            {
                Vector3 p = _workingAtoms[idx].Cart;
                sum += p;
                bmin = Vector3.Min(bmin, p);
                bmax = Vector3.Max(bmax, p);
            }

            fragment.CenterCart = sum / fragment.AtomCount;
            fragment.BBoxMin = bmin;
            fragment.BBoxMax = bmax;
        }

        #endregion

        #region Step 2 — Target atoms

        private void CollectTargetAtoms(MoleculeFragment target)
        {
            _targetAtoms.Clear();

            foreach (var idx in target.AtomIndices)
            {
                var c = _workingAtoms[idx];

                _targetAtoms.Add(new SceneAtom
                {
                    SymmetryAtomIndex = idx,
                    CellShiftX = 0,
                    CellShiftY = 0,
                    CellShiftZ = 0,
                    FracPosition = c.Frac,
                    CartPosition = c.Cart,
                    AtomicNumber = c.AtomicNumber,
                    IsTarget = true,
                    IsAnisotropic = c.IsAnisotropic,
                    Un = c.Un,
                    Occupancy = c.Occupancy,
                    Name = c.Name,
                    MoleculeId = c.MoleculeId,
                    SourceAsymIndex = c.SourceAsymIndex,
 // ⭐ v14
                    Charge = c.Charge,
                    Hydrophobicity = c.Hydrophobicity
                });
            }

            Debug.WriteLine($"[HirshfeldScene] Target: {_targetAtoms.Count} atoms collected " +
                            $"(MoleculeId={_targetMoleculeId})");
        }

        #endregion

        #region Step 3 — Neighbors

        private void CollectNeighbors(float cutoff)
        {
            _outerAtoms.Clear();

            int cellCount = _workingAtoms.Count;
            if (cellCount == 0) return;

            float fracCutX = cutoff / _cellA;
            float fracCutY = cutoff / _cellB;
            float fracCutZ = cutoff / _cellC;

            int rangeX = Math.Min((int)MathF.Ceiling(fracCutX) + 1, MAX_CELL_RANGE_PER_AXIS);
            int rangeY = Math.Min((int)MathF.Ceiling(fracCutY) + 1, MAX_CELL_RANGE_PER_AXIS);
            int rangeZ = Math.Min((int)MathF.Ceiling(fracCutZ) + 1, MAX_CELL_RANGE_PER_AXIS);

            Debug.WriteLine($"[HirshfeldScene] Neighbors scan START (v14):");
            Debug.WriteLine($"  cutoff={cutoff:F2} Å, cell=({_cellA:F2},{_cellB:F2},{_cellC:F2})");
            Debug.WriteLine($"  range=({rangeX}, {rangeY}, {rangeZ}), cellCount={cellCount}");
            Debug.WriteLine($"  targetMoleculeId={_targetMoleculeId}");

            float cutoffSq = cutoff * cutoff;

            var cellAtomsCart = new Vector3[cellCount];
            for (int i = 0; i < cellCount; i++)
                cellAtomsCart[i] = _workingAtoms[i].Cart;

            var translations = new List<(int tx, int ty, int tz, Vector3 cart)>();
            for (int tx = -rangeX; tx <= rangeX; tx++)
                for (int ty = -rangeY; ty <= rangeY; ty++)
                    for (int tz = -rangeZ; tz <= rangeZ; tz++)
                    {
                        Vector3 tcart = Vector3.Transform(
                            new Vector3(tx, ty, tz), _fracToCart);
                        translations.Add((tx, ty, tz, tcart));
                    }

            Debug.WriteLine($"  translations={translations.Count}, cellAtomsCart cached");

            int added = 0;
            int skippedHostInBase = 0;
            int rejectedDx = 0;
            int rejectedDy = 0;
            int rejectedDz = 0;
            int rejectedDist = 0;
            int duplicates = 0;
            int totalIter = 0;

            foreach (var targetAtom in _targetAtoms)
            {
                Vector3 targetCart = targetAtom.CartPosition;

                foreach (var t in translations)
                {
                    bool isBaseCellTranslation = (t.tx == 0 && t.ty == 0 && t.tz == 0);
                    Vector3 tCart = t.cart;

                    for (int srcIdx = 0; srcIdx < cellCount; srcIdx++)
                    {
                        var src = _workingAtoms[srcIdx];

                        if (isBaseCellTranslation && src.IsHost)
                        {
                            skippedHostInBase++;
                            continue;
                        }

                        totalIter++;

                        Vector3 pos = cellAtomsCart[srcIdx] + tCart;

                        float dx = pos.X - targetCart.X;
                        if (dx > cutoff || dx < -cutoff) { rejectedDx++; continue; }

                        float dy = pos.Y - targetCart.Y;
                        if (dy > cutoff || dy < -cutoff) { rejectedDy++; continue; }

                        float dz = pos.Z - targetCart.Z;
                        if (dz > cutoff || dz < -cutoff) { rejectedDz++; continue; }

                        float d2 = dx * dx + dy * dy + dz * dz;
                        if (d2 > cutoffSq) { rejectedDist++; continue; }

                        var identity = (src.MoleculeId, src.SourceAsymIndex,
                                        src.SymmOpIndex, t.tx, t.ty, t.tz);
                        if (_seenIdentities.Contains(identity)) { duplicates++; continue; }
                        _seenIdentities.Add(identity);

                        _outerAtoms.Add(new SceneAtom
                        {
                            SymmetryAtomIndex = srcIdx,
                            CellShiftX = t.tx,
                            CellShiftY = t.ty,
                            CellShiftZ = t.tz,
                            FracPosition = new Vector3(
                                src.Frac.X + t.tx,
                                src.Frac.Y + t.ty,
                                src.Frac.Z + t.tz),
                            CartPosition = pos,
                            AtomicNumber = src.AtomicNumber,
                            IsTarget = false,
                            IsAnisotropic = src.IsAnisotropic,
                            Un = src.Un,
                            Occupancy = src.Occupancy,
                            Name = src.Name,
                            MoleculeId = src.MoleculeId,
                            SourceAsymIndex = src.SourceAsymIndex,
 // ⭐ v14
                            Charge = src.Charge,
                            Hydrophobicity = src.Hydrophobicity
                        });

                        added++;
                    }
                }
            }

            Debug.WriteLine($"[HirshfeldScene] Neighbors scan END:");
            Debug.WriteLine($"  iterations={totalIter}");
            Debug.WriteLine($"  added={added}, skippedHostInBase={skippedHostInBase}");
            Debug.WriteLine($"  rejected: dX={rejectedDx}, dY={rejectedDy}, " +
                            $"dZ={rejectedDz}, dist={rejectedDist}, duplicates={duplicates}");

            var byMol = new Dictionary<int, int>();
            foreach (var a in _outerAtoms)
            {
                if (!byMol.TryGetValue(a.MoleculeId, out int c)) c = 0;
                byMol[a.MoleculeId] = c + 1;
            }
            Debug.WriteLine($"  Outer by MoleculeId:");
            foreach (var kv in byMol)
                Debug.WriteLine($"    MoleculeId {kv.Key}: {kv.Value} atoms");
        }

        #endregion

        #region Debug helpers

        public string GetInfo()
        {
            return $"Scene: fragments={_fragments?.Count ?? 0}, " +
                   $"target=#{TargetMoleculeId} ({_targetAtoms.Count} atoms), " +
                   $"outer={_outerAtoms.Count} atoms, " +
                   $"cutoff={_lastCutoff:F2} Å, R={TargetRadius:F2} Å";
        }

        public List<(int tx, int ty, int tz)> GetOccupiedCellShifts()
        {
            var set = new HashSet<(int, int, int)>();
            foreach (var a in _outerAtoms)
                set.Add((a.CellShiftX, a.CellShiftY, a.CellShiftZ));
            var list = set.ToList();
            list.Sort();
            return list;
        }

        #endregion
    }
}