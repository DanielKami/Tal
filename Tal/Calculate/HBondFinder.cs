// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Calculation module: HBondFinder.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Tal.Calculate
{
 /// <summary>
 /// Statyczna klasa do wykrywania bonds wodorowych w strukturze molekularnej.
 ///
 /// ⭐ NOWE (2026): Instead of removing atoms, we use UseForHBonds flags.
 /// - RemoveOwnNeighborAtoms NIE usuwa atoms — only zeruje flag.
 /// - Calculate filtere after UseForHBonds (KROK 5, 6, 7).
 /// - Indices in atom[] stay stable → ContactAtom[], BondedAtom[],
 /// H_BondedAtom[] always point to the correct atoms.
 ///
 /// ⭐ NOWE (2026): filtrowanie donors i acceptors after listch
 /// Flags.HBondAllowedDonors / Flags.HBondAllowedAcceptors.
 /// Atomowy Z must be na odafterwiedniej list, so that uczestniczyć
 /// w wiązaniu wodorowym.
 ///
 /// Logika:
 /// 1. Zeruje flag UseForHBonds na atomach, which SAM dodał w afterbeforenim
 /// wywołaniu (rejestrowane w Molecule.HBondNeighborAtomIndices).
 /// NIE rusza atoms structure (nawet z CellShift != 0).
 ///
 /// 2. H-bondy wewnątrzcząsteczkowe — szuka w atom[] (filtr UseForHBonds
 /// + filtr HBondAllowedDonors/Acceptors).
 ///
 /// 3. H-bondy międzycząsteczkowe — szuka acceptors w SymetryEquivalents
 /// × translacje ±1. Znalezione akceptory (te which faktycznie tworzą
 /// H-bond) są DODAWANE do atom[] z CellShift i flagami:
 /// UseForHBonds = true, UseForShortContacts = false.
 /// </summary>
    public static class HBondFinder
    {
 // Tolerancja for wykrywania duplikatów (w Å²)
        private const float DUPLICATE_TOLERANCE_SQ = 0.01f;

 // Twarde limity withoutpieczeństwa — żaden H-bond must not be dłuższy
        private const float MAX_H_BOND_DISTANCE = 5.0f;
        private const float MIN_H_BOND_DISTANCE = 0.5f;

        #region Public API

 /// <summary>
 /// Main method — wykrywa H-bonds w molekule.
 /// </summary>
        public static void Calculate(Molecule molecule)
        {
            if (molecule == null) return;
            if (molecule.atom == null || molecule.NumberOfAtoms == 0) return;

 // ═══════════════════════════════════════════════════════════
 // KROK 0: Diagnostyka
 // ═══════════════════════════════════════════════════════════
            int origCount = 0;
            int hBondActive = 0;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].UseForHBonds) hBondActive++;
                if (molecule.atom[i].CellShiftX == 0 &&
                    molecule.atom[i].CellShiftY == 0 &&
                    molecule.atom[i].CellShiftZ == 0)
                    origCount++;
            }
            Debug.WriteLine($"[HBondFinder] INPUT: total={molecule.NumberOfAtoms}, " +
                            $"orig={origCount}, UseForHBonds={hBondActive}, " +
                            $"registered={molecule.HBondNeighborAtomIndices?.Count ?? 0}");

 // ═══════════════════════════════════════════════════════════
 // KROK 1: Wyzeruj flag UseForHBonds na atomach, which
 // HBondFinder sam dodał w afterbeforenim wywołaniu.
 // NIE usuwamy ich z atom[] — indexy afterzostają stabilne.
 // ═══════════════════════════════════════════════════════════
            if (molecule.HBondNeighborAtomIndices != null &&
                molecule.HBondNeighborAtomIndices.Count > 0)
            {
                RemoveOwnNeighborAtoms(molecule);
                Debug.WriteLine($"[HBondFinder] After flag reset: total={molecule.NumberOfAtoms}");
            }

 // ═══════════════════════════════════════════════════════════
 // KROK 2: If H-bonds wyłączone — clear i wyjdź
 // ═══════════════════════════════════════════════════════════
            if (!Flags.Instance.ShowHBonds)
            {
                Clear(molecule);
                return;
            }

 // ═══════════════════════════════════════════════════════════
 // KROK 3: Inicjalizacja tablic H-bond
 // Ensure on ALL atoms — Short Contacts adds atoms with only
 // partial H_* arrays; donors/H can still host H-bond records.
 // ═══════════════════════════════════════════════════════════
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                EnsureArraysForAtom(molecule, i);
                molecule.atom[i].H_NrBondedAtoms = 0;
            }

 // ═══════════════════════════════════════════════════════════
 // KROK 4: Cache flags
 // ═══════════════════════════════════════════════════════════
            var f = Flags.Instance;
            int distanceMode = f.HBondDistanceMode;
            float actualMin = f.HBondActualMinDistance;
            float actualMax = f.HBondActualMaxDistance;
            float vdwMinus = f.HBondVdWMinus;
            float vdwPlus = f.HBondVdWPlus;
            float minAngle = f.HBondMinAngle;
            bool drawFromH = f.HBondDrawFromHydrogen;
            float bondCorr = f.BOND_FACTOR;
            bool allowInter = f.HBondIntermolecular;
            bool allowIntra = f.HBondIntramolecular;
            int maxHBond = f.MAX_H_BONDED_ATOMS;

 // ⭐ NEW: list dozwolonych donors i acceptors
            var allowedDonors = f.HBondAllowedDonors ?? new List<int>();
            var allowedAcceptors = f.HBondAllowedAcceptors ?? new List<int>();

            bool shouldAddNeighborAcceptors = allowInter;

            // Molecule IDs are required for inter/intra filtering. Presentation()
            // normally sets them; re-run if missing (e.g. after partial rebuilds).
            if (molecule.SymetryEquivalents != null && molecule.SymetryEquivalents.Count > 0)
            {
                bool anyUnset = false;
                foreach (var a in molecule.SymetryEquivalents)
                {
                    if (a.IndividualMoleculeInCell < 0) { anyUnset = true; break; }
                }
                if (anyUnset)
                {
                    molecule.DetectMoleculesInSymmetryEquivalents();
                    molecule.PropagateMoleculeIdsToAsymmetricPart();
                }
            }


            Debug.WriteLine($"[HBondFinder] Allowed donors: [{string.Join(",", allowedDonors)}], " +
                            $"acceptors: [{string.Join(",", allowedAcceptors)}]");

 // ═══════════════════════════════════════════════════════════
 // KROK 5–7: Bidirectional H-bond search
 //
 // PASS A — donor-centric (D–H already in atom[]):
 //   For every donor with bonded H, scan acceptors in atom[] and
 //   SymetryEquivalents × ±1 (wrapped), like ShortContacts.
 //
 // PASS B — acceptor-centric (A already in atom[], e.g. O in
 //   neighbouring cell from Short Contacts / expand):
 //   For every acceptor in atom[], scan lattice images of D–H pairs
 //   from SymetryEquivalents. This catches H-bonds that PASS A misses
 //   when the donor sits only in a lattice image, not in atom[].
 //
 // Donors are NOT required to have UseForHBonds=true up front —
 // Short Contacts marks neighbours with UseForHBonds=false, which
 // previously caused those D–H groups to be skipped entirely.
 // ═══════════════════════════════════════════════════════════
            if ((molecule.SymetryEquivalents == null || molecule.SymetryEquivalents.Count == 0) &&
                molecule.AsymmetricPart != null && molecule.AsymmetricPart.Count > 0)
            {
                molecule.CalculateSymmetryEquivalents();
            }

            var foundNeighborAcceptors = new List<AcceptorEntry>();
            var seenNeighbors = new HashSet<string>();
            // Dedup recorded bonds: donorKey|hKey|accKey
            var seenBonds = new HashSet<string>();
            int hBondCount = 0;

            Matrix U = Matrix.Transpose(molecule.FractionaltoCarttesianMatrix(0, 0, 0));
            Vector3 molShift = molecule.MoleculeShift;

            // ── helpers local to this search ──────────────────────────
            bool TryRecordBond(
                int donorIdx, int hIdx,
                Vector3 donorPos, Vector3 hPos,
                int donorMolId, int donorCellX, int donorCellY, int donorCellZ,
                AcceptorEntry acc)
            {
                int mu = acc.AtomicNumber;
                Vector3 accPos = acc.CartPos;
                int nu = molecule.atom[donorIdx].AtomicNumber;

                float dDA = Vector3.Distance(donorPos, accPos);
                float dHA = Vector3.Distance(hPos, accPos);
                float vdwSum = molecule.GetVdwRadius(nu) + molecule.GetVdwRadius(mu);

                if (dDA > MAX_H_BOND_DISTANCE || dDA < MIN_H_BOND_DISTANCE) return false;
                if (dHA > MAX_H_BOND_DISTANCE || dHA < MIN_H_BOND_DISTANCE) return false;

                bool distanceOk;
                if (distanceMode == 0)
                    distanceOk = dDA >= actualMin && dDA <= actualMax;
                else
                    distanceOk = dDA >= vdwSum - vdwMinus && dDA <= vdwSum + vdwPlus;
                if (!distanceOk) return false;
                if (dDA < vdwSum * bondCorr) return false;
                if (dHA >= dDA) return false;

                Vector3 v1 = donorPos - hPos;
                Vector3 v2 = accPos - hPos;
                float l1 = v1.Length(), l2 = v2.Length();
                if (l1 < 1e-4f || l2 < 1e-4f) return false;

                float cos = Math.Clamp(Vector3.Dot(v1, v2) / (l1 * l2), -1f, 1f);
                float angleDHA = MathHelperTal.ToDegrees((float)Math.Acos(cos));
                if (angleDHA < minAngle) return false;

                bool sameCell = (acc.CellX == donorCellX &&
                                 acc.CellY == donorCellY &&
                                 acc.CellZ == donorCellZ);
                bool sameMol = (acc.MoleculeId >= 0 && donorMolId >= 0 &&
                                acc.MoleculeId == donorMolId);
                bool isIntra = sameCell && sameMol;
                if (donorMolId < 0 || acc.MoleculeId < 0)
                {
                    // Unknown molecule IDs: only treat as intra if acceptor is the
                    // same atom[] entry clearly in the same cell shift.
                    isIntra = sameCell && acc.IsOriginal && acc.OriginalAtomIndex >= 0;
                }
                if (isIntra && !allowIntra) return false;
                if (!isIntra && !allowInter) return false;

                string bondKey =
                    $"{donorPos.X:F3},{donorPos.Y:F3},{donorPos.Z:F3}|" +
                    $"{hPos.X:F3},{hPos.Y:F3},{hPos.Z:F3}|" +
                    $"{accPos.X:F3},{accPos.Y:F3},{accPos.Z:F3}";
                if (!seenBonds.Add(bondKey)) return false;

                // Activate atoms involved (may have been SC-only)
                molecule.atom[donorIdx].UseForHBonds = true;
                molecule.atom[hIdx].UseForHBonds = true;
                if (acc.IsOriginal && acc.OriginalAtomIndex >= 0)
                    molecule.atom[acc.OriginalAtomIndex].UseForHBonds = true;

                if (!acc.IsOriginal)
                {
                    string key = $"{acc.Name}|{acc.CellX},{acc.CellY},{acc.CellZ}|" +
                                 $"{acc.CartPos.X:F4},{acc.CartPos.Y:F4},{acc.CartPos.Z:F4}";
                    if (seenNeighbors.Add(key))
                        foundNeighborAcceptors.Add(acc);
                }

                int hostIdx = drawFromH ? hIdx : donorIdx;
                EnsureArraysForAtom(molecule, hostIdx);
                EnsureArraysForAtom(molecule, donorIdx);
                EnsureArraysForAtom(molecule, hIdx);

                int n = molecule.atom[hostIdx].H_NrBondedAtoms;
                if (n >= maxHBond) return false;

                Vector3 hostPos = molecule.atom[hostIdx].Coordinate;

                molecule.atom[hostIdx].H_BondedAtom[n] =
                    acc.IsOriginal ? acc.OriginalAtomIndex : -1;
                molecule.atom[hostIdx].H_HydrogenAtom[n] = hIdx;
                molecule.atom[hostIdx].H_Distance[n] = dDA;
                molecule.atom[hostIdx].H_DonorAcceptorDistance[n] = dDA;
                molecule.atom[hostIdx].H_HydrogenAcceptorDistance[n] = dHA;
                molecule.atom[hostIdx].H_DHA_Angle[n] = angleDHA;
                molecule.atom[hostIdx].H_AngleZ[n] = (float)Math.Asin(
                    (hostPos.Z - accPos.Z) / Math.Max(dDA, 1e-4f));
                molecule.atom[hostIdx].H_AngleXY[n] = ComputeAngleXY(hostPos, accPos);
                molecule.atom[hostIdx].H_AccCellX[n] = acc.CellX;
                molecule.atom[hostIdx].H_AccCellY[n] = acc.CellY;
                molecule.atom[hostIdx].H_AccCellZ[n] = acc.CellZ;
                molecule.atom[hostIdx].H_AccSymmOpIndex[n] = acc.SymmOpIndex;
                molecule.atom[hostIdx].H_AccName[n] = acc.Name;
                molecule.atom[hostIdx].H_AccCartPos[n] = accPos;
                molecule.atom[hostIdx].H_AccAtomicNumber[n] = mu;
                molecule.atom[hostIdx].H_AccMoleculeId[n] = acc.MoleculeId;
                molecule.atom[hostIdx].H_NrBondedAtoms = n + 1;
                return true;
            }

            // ═══════════════ PASS A: donor-centric ═══════════════
            for (int donorIdx = 0; donorIdx < molecule.NumberOfAtoms; donorIdx++)
            {
                int nu = molecule.atom[donorIdx].AtomicNumber;
                if (!allowedDonors.Contains(nu)) continue;
                // Do NOT require UseForHBonds — SC neighbours have it false

                if (molecule.atom[donorIdx].BondedAtom == null) continue;

                Vector3 donorPos = molecule.atom[donorIdx].Coordinate;
                int donorMolId = molecule.atom[donorIdx].IndividualMoleculeInCell;
                int donorCellX = molecule.atom[donorIdx].CellShiftX;
                int donorCellY = molecule.atom[donorIdx].CellShiftY;
                int donorCellZ = molecule.atom[donorIdx].CellShiftZ;

                for (int k = 0; k < molecule.atom[donorIdx].NrBondedAtoms; k++)
                {
                    int hIdx = molecule.atom[donorIdx].BondedAtom[k];
                    if (hIdx < 0 || hIdx >= molecule.NumberOfAtoms) continue;
                    if (molecule.atom[hIdx].AtomicNumber != 1) continue;

                    Vector3 hPos = molecule.atom[hIdx].Coordinate;

                    // (A1) acceptors already in atom[] (any cell shift)
                    for (int i = 0; i < molecule.NumberOfAtoms; i++)
                    {
                        if (i == donorIdx || i == hIdx) continue;
                        int z = molecule.atom[i].AtomicNumber;
                        if (!allowedAcceptors.Contains(z)) continue;

                        var acc = new AcceptorEntry
                        {
                            Name = molecule.atom[i].name,
                            CartPos = molecule.atom[i].Coordinate,
                            AtomicNumber = z,
                            CellX = molecule.atom[i].CellShiftX,
                            CellY = molecule.atom[i].CellShiftY,
                            CellZ = molecule.atom[i].CellShiftZ,
                            SymmOpIndex = molecule.atom[i].SymmetryEquivalentNumberInCell,
                            MoleculeId = molecule.atom[i].IndividualMoleculeInCell,
                            IsOriginal = true,
                            OriginalAtomIndex = i
                        };
                        if (TryRecordBond(donorIdx, hIdx, donorPos, hPos,
                                donorMolId, donorCellX, donorCellY, donorCellZ, acc))
                            hBondCount++;
                    }

                    // (A2) lattice images of SymetryEquivalents as acceptors
                    if (shouldAddNeighborAcceptors &&
                        molecule.SymetryEquivalents != null &&
                        molecule.SymetryEquivalents.Count > 0)
                    {
                        foreach (var src in molecule.SymetryEquivalents)
                        {
                            int z = src.AtomicNumber;
                            if (!allowedAcceptors.Contains(z)) continue;

                            float fx = src.x - (float)Math.Floor(src.x);
                            float fy = src.y - (float)Math.Floor(src.y);
                            float fz = src.z - (float)Math.Floor(src.z);

                            for (int tx = -1; tx <= 1; tx++)
                            for (int ty = -1; ty <= 1; ty++)
                            for (int tz = -1; tz <= 1; tz++)
                            {
                                Vector3 cartPos = Vector3.Transform(
                                    new Vector3(fx + tx, fy + ty, fz + tz), U) - molShift;

                                float dGate = Vector3.Distance(hPos, cartPos);
                                if (dGate > MAX_H_BOND_DISTANCE || dGate < MIN_H_BOND_DISTANCE)
                                    continue;

                                // Prefer existing atom[] entry at this position
                                int existingIdx = -1;
                                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                                {
                                    if (Vector3.DistanceSquared(molecule.atom[i].Coordinate, cartPos)
                                        < DUPLICATE_TOLERANCE_SQ)
                                    {
                                        existingIdx = i;
                                        break;
                                    }
                                }
                                if (existingIdx >= 0)
                                {
                                    if (existingIdx == donorIdx || existingIdx == hIdx) continue;
                                    // already covered by (A1) — skip to avoid double work
                                    continue;
                                }

                                var acc = new AcceptorEntry
                                {
                                    Name = src.name,
                                    CartPos = cartPos,
                                    AtomicNumber = z,
                                    CellX = tx,
                                    CellY = ty,
                                    CellZ = tz,
                                    SymmOpIndex = src.SymmetryEquivalentNumberInCell,
                                    MoleculeId = src.IndividualMoleculeInCell,
                                    IsOriginal = false,
                                    OriginalAtomIndex = -1
                                };
                                if (TryRecordBond(donorIdx, hIdx, donorPos, hPos,
                                        donorMolId, donorCellX, donorCellY, donorCellZ, acc))
                                    hBondCount++;
                            }
                        }
                    }
                }
            }

            // ═══════════════ PASS B: acceptor-centric ═══════════════
            // Acceptors already in atom[] (main molecule) ← lattice D–H outside.
            // PASS A only walks donors that are already in atom[]; this pass is
            // required for external donors (neighbouring cells) H-bonding INTO
            // the displayed molecule's acceptors.
            //
            // Algorithm:
            //  1) Build lattice images of all donor heavies + hydrogens (±1, wrapped)
            //  2) For each acceptor in atom[], find H near it, then D covalently on H
            //  3) Add missing D/H to atom[] immediately, then TryRecordBond
            if (shouldAddNeighborAcceptors &&
                molecule.SymetryEquivalents != null &&
                molecule.SymetryEquivalents.Count > 0)
            {
                var latticeDonors = new List<(Vector3 pos, int z, int tx, int ty, int tz, int sym, int mol, string name)>();
                var latticeHydrogens = new List<(Vector3 pos, int tx, int ty, int tz, int sym, int mol, string name)>();

                foreach (var src in molecule.SymetryEquivalents)
                {
                    float fx = src.x - (float)Math.Floor(src.x);
                    float fy = src.y - (float)Math.Floor(src.y);
                    float fz = src.z - (float)Math.Floor(src.z);

                    for (int tx = -1; tx <= 1; tx++)
                    for (int ty = -1; ty <= 1; ty++)
                    for (int tz = -1; tz <= 1; tz++)
                    {
                        Vector3 cartPos = Vector3.Transform(
                            new Vector3(fx + tx, fy + ty, fz + tz), U) - molShift;

                        if (src.AtomicNumber == 1)
                        {
                            latticeHydrogens.Add((cartPos, tx, ty, tz,
                                src.SymmetryEquivalentNumberInCell,
                                src.IndividualMoleculeInCell, src.name));
                        }
                        else if (allowedDonors.Contains(src.AtomicNumber))
                        {
                            latticeDonors.Add((cartPos, src.AtomicNumber, tx, ty, tz,
                                src.SymmetryEquivalentNumberInCell,
                                src.IndividualMoleculeInCell, src.name));
                        }
                    }
                }

                const float maxDH = 1.40f;
                const float minDH = 0.65f;

                // Pending bonds that need D/H added first
                var pending = new List<(
                    AcceptorEntry donor, AcceptorEntry hydrogen,
                    int accIdx, Vector3 dPos, Vector3 hPos, Vector3 aPos,
                    int dZ, float dDA, float dHA, float angle)>();

                int passBFound = 0;

                for (int accIdx = 0; accIdx < molecule.NumberOfAtoms; accIdx++)
                {
                    int mu = molecule.atom[accIdx].AtomicNumber;
                    if (!allowedAcceptors.Contains(mu)) continue;
                    // Acceptors may come from SC with UseForHBonds=false — still valid targets
                    Vector3 accPos = molecule.atom[accIdx].Coordinate;
                    int accMol = molecule.atom[accIdx].IndividualMoleculeInCell;
                    int accCX = molecule.atom[accIdx].CellShiftX;
                    int accCY = molecule.atom[accIdx].CellShiftY;
                    int accCZ = molecule.atom[accIdx].CellShiftZ;

                    foreach (var H in latticeHydrogens)
                    {
                        float dHA = Vector3.Distance(H.pos, accPos);
                        if (dHA > MAX_H_BOND_DISTANCE || dHA < MIN_H_BOND_DISTANCE)
                            continue;

                        foreach (var D in latticeDonors)
                        {
                            float dDH = Vector3.Distance(D.pos, H.pos);
                            if (dDH > maxDH || dDH < minDH) continue;

                            float dDA = Vector3.Distance(D.pos, accPos);
                            if (dDA > MAX_H_BOND_DISTANCE || dDA < MIN_H_BOND_DISTANCE)
                                continue;

                            float vdwSum = molecule.GetVdwRadius(D.z) + molecule.GetVdwRadius(mu);
                            bool distanceOk;
                            if (distanceMode == 0)
                                distanceOk = dDA >= actualMin && dDA <= actualMax;
                            else
                                distanceOk = dDA >= vdwSum - vdwMinus && dDA <= vdwSum + vdwPlus;
                            if (!distanceOk) continue;
                            if (dDA < vdwSum * bondCorr) continue;
                            if (dHA >= dDA) continue; // H must be closer to A than D is

                            Vector3 v1 = D.pos - H.pos;
                            Vector3 v2 = accPos - H.pos;
                            float l1 = v1.Length(), l2 = v2.Length();
                            if (l1 < 1e-4f || l2 < 1e-4f) continue;
                            float cos = Math.Clamp(Vector3.Dot(v1, v2) / (l1 * l2), -1f, 1f);
                            float angleDHA = MathHelperTal.ToDegrees((float)Math.Acos(cos));
                            if (angleDHA < minAngle) continue;

                            // Inter/intra
                            bool sameCell = (accCX == D.tx && accCY == D.ty && accCZ == D.tz);
                            bool sameMol = (accMol >= 0 && D.mol >= 0 && accMol == D.mol);
                            bool isIntra = sameCell && sameMol;
                            if (isIntra && !allowIntra) continue;
                            if (!isIntra && !allowInter) continue;

                            string bondKey =
                                $"{D.pos.X:F3},{D.pos.Y:F3},{D.pos.Z:F3}|" +
                                $"{H.pos.X:F3},{H.pos.Y:F3},{H.pos.Z:F3}|" +
                                $"{accPos.X:F3},{accPos.Y:F3},{accPos.Z:F3}";
                            if (!seenBonds.Add(bondKey)) continue;

                            // Resolve D / H to existing atom[] if possible
                            int donorIdx = FindAtomAt(molecule, D.pos);
                            int hIdx = FindAtomAt(molecule, H.pos);

                            var dEntry = new AcceptorEntry
                            {
                                Name = D.name,
                                CartPos = D.pos,
                                AtomicNumber = D.z,
                                CellX = D.tx, CellY = D.ty, CellZ = D.tz,
                                SymmOpIndex = D.sym,
                                MoleculeId = D.mol,
                                IsOriginal = donorIdx >= 0,
                                OriginalAtomIndex = donorIdx
                            };
                            var hEntry = new AcceptorEntry
                            {
                                Name = H.name,
                                CartPos = H.pos,
                                AtomicNumber = 1,
                                CellX = H.tx, CellY = H.ty, CellZ = H.tz,
                                SymmOpIndex = H.sym,
                                MoleculeId = H.mol,
                                IsOriginal = hIdx >= 0,
                                OriginalAtomIndex = hIdx
                            };

                            if (donorIdx >= 0 && hIdx >= 0)
                            {
                                // Both already present — record directly
                                // (seenBonds already has key; TryRecordBond would skip)
                                molecule.atom[donorIdx].UseForHBonds = true;
                                molecule.atom[hIdx].UseForHBonds = true;
                                molecule.atom[accIdx].UseForHBonds = true;
                                LinkCovalentIfMissing(molecule, donorIdx, hIdx);

                                var acc = new AcceptorEntry
                                {
                                    Name = molecule.atom[accIdx].name,
                                    CartPos = accPos,
                                    AtomicNumber = mu,
                                    CellX = accCX, CellY = accCY, CellZ = accCZ,
                                    SymmOpIndex = molecule.atom[accIdx].SymmetryEquivalentNumberInCell,
                                    MoleculeId = accMol,
                                    IsOriginal = true,
                                    OriginalAtomIndex = accIdx
                                };
                                if (RecordBondDirect(molecule, donorIdx, hIdx,
                                        molecule.atom[donorIdx].Coordinate,
                                        molecule.atom[hIdx].Coordinate,
                                        acc, drawFromH, maxHBond))
                                {
                                    hBondCount++;
                                    passBFound++;
                                }
                            }
                            else
                            {
                                pending.Add((dEntry, hEntry, accIdx, D.pos, H.pos, accPos,
                                    D.z, dDA, dHA, angleDHA));
                                passBFound++;
                            }
                        }
                    }
                }

                Debug.WriteLine($"[HBondFinder] PASS B: geometry hits={passBFound}, pending add={pending.Count}");

                // Add missing D/H neighbours, then record bonds
                if (pending.Count > 0)
                {
                    var toAdd = new List<AcceptorEntry>();
                    var addKeys = new HashSet<string>();
                    foreach (var p in pending)
                    {
                        if (p.donor.OriginalAtomIndex < 0)
                        {
                            string k = $"D|{p.donor.CartPos.X:F4}|{p.donor.CartPos.Y:F4}|{p.donor.CartPos.Z:F4}";
                            if (addKeys.Add(k) && FindAtomAt(molecule, p.donor.CartPos) < 0)
                                toAdd.Add(p.donor);
                        }
                        if (p.hydrogen.OriginalAtomIndex < 0)
                        {
                            string k = $"H|{p.hydrogen.CartPos.X:F4}|{p.hydrogen.CartPos.Y:F4}|{p.hydrogen.CartPos.Z:F4}";
                            if (addKeys.Add(k) && FindAtomAt(molecule, p.hydrogen.CartPos) < 0)
                                toAdd.Add(p.hydrogen);
                        }
                    }

                    if (toAdd.Count > 0)
                    {
                        AddNeighborAcceptorsToMolecule(molecule, toAdd);
                        Debug.WriteLine($"[HBondFinder] PASS B: added {toAdd.Count} external D/H atoms");
                    }

                    // Record each pending bond now that D/H are in atom[]
                    foreach (var p in pending)
                    {
                        int donorIdx = FindAtomAt(molecule, p.dPos);
                        int hIdx = FindAtomAt(molecule, p.hPos);
                        if (donorIdx < 0 || hIdx < 0)
                        {
                            Debug.WriteLine($"[HBondFinder] PASS B: could not resolve D/H after add");
                            continue;
                        }

                        molecule.atom[donorIdx].UseForHBonds = true;
                        molecule.atom[hIdx].UseForHBonds = true;
                        molecule.atom[p.accIdx].UseForHBonds = true;

                        // Ensure covalent D–H link so later passes see the H
                        LinkCovalentIfMissing(molecule, donorIdx, hIdx);

                        var acc = new AcceptorEntry
                        {
                            Name = molecule.atom[p.accIdx].name,
                            CartPos = p.aPos,
                            AtomicNumber = molecule.atom[p.accIdx].AtomicNumber,
                            CellX = molecule.atom[p.accIdx].CellShiftX,
                            CellY = molecule.atom[p.accIdx].CellShiftY,
                            CellZ = molecule.atom[p.accIdx].CellShiftZ,
                            SymmOpIndex = molecule.atom[p.accIdx].SymmetryEquivalentNumberInCell,
                            MoleculeId = molecule.atom[p.accIdx].IndividualMoleculeInCell,
                            IsOriginal = true,
                            OriginalAtomIndex = p.accIdx
                        };

                        // Temporarily remove from seenBonds so TryRecordBond can add
                        // (we already inserted the key above to avoid duplicates in search)
                        // TryRecordBond uses seenBonds — key is already there, so call record directly
                        if (RecordBondDirect(molecule, donorIdx, hIdx,
                                molecule.atom[donorIdx].Coordinate,
                                molecule.atom[hIdx].Coordinate,
                                acc, drawFromH, maxHBond))
                        {
                            hBondCount++;
                        }
                    }
                }
            }

            Debug.WriteLine($"[HBondFinder] Found {hBondCount} H-bonds (bidirectional), " +
                            $"neighbor atoms queued: {foundNeighborAcceptors.Count}");

            // ═══════════════════════════════════════════════════════════
            // KROK 8: Add neighbour acceptors found in PASS A
            // ═══════════════════════════════════════════════════════════
            if (shouldAddNeighborAcceptors && foundNeighborAcceptors.Count > 0)
            {
                AddNeighborAcceptorsToMolecule(molecule, foundNeighborAcceptors);
                UpdateHBondAcceptorIndices(molecule, foundNeighborAcceptors);

                Debug.WriteLine($"[HBondFinder] After PASS A add: total={molecule.NumberOfAtoms}, " +
                                $"registered={molecule.HBondNeighborAtomIndices?.Count ?? 0}");
            }

            // Final covalent rebuild + index fix for any H-bonds with H_BondedAtom=-1
            molecule.Calculate_Bonds3D();
            UpdateHBondAcceptorIndices(molecule, foundNeighborAcceptors);

            // Guarantee: every inter-cell H-bond ends on a visible atom sphere
            EnsureHBondEndpointAtomsVisible(molecule);
            ForceShowAllHBondEndpoints(molecule);
        }

        /// <summary>
        /// Final pass: force every H-bond donor, H and acceptor sphere visible.
        /// Fixes cases where the dashed rod was drawn from H_AccCartPos but the
        /// acceptor atom stayed hidden (show=false / style=10 / missing flag).
        /// </summary>
        private static void ForceShowAllHBondEndpoints(Molecule molecule)
        {
            if (molecule?.atom == null) return;

            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                int n = molecule.atom[i].H_NrBondedAtoms;
                if (n <= 0) continue;

                // Host of the H-bond record (usually H or donor)
                molecule.atom[i].show = true;
                molecule.atom[i].UseForHBonds = true;

                if (molecule.atom[i].H_HydrogenAtom != null)
                {
                    for (int k = 0; k < n && k < molecule.atom[i].H_HydrogenAtom.Length; k++)
                    {
                        int h = molecule.atom[i].H_HydrogenAtom[k];
                        if (h >= 0 && h < molecule.NumberOfAtoms)
                        {
                            molecule.atom[h].show = true;
                            molecule.atom[h].UseForHBonds = true;
                        }
                    }
                }

                if (molecule.atom[i].H_BondedAtom == null) continue;
                for (int k = 0; k < n && k < molecule.atom[i].H_BondedAtom.Length; k++)
                {
                    int a = molecule.atom[i].H_BondedAtom[k];
                    if (a >= 0 && a < molecule.NumberOfAtoms)
                    {
                        molecule.atom[a].show = true;
                        molecule.atom[a].UseForHBonds = true;
                        if (molecule.atom[a].style < 0 || molecule.atom[a].style == 10)
                            molecule.atom[a].style = Flags.Instance.Presentation;
                    }
                }
            }

            if (molecule.HBondNeighborAtomIndices != null)
            {
                foreach (int idx in molecule.HBondNeighborAtomIndices)
                {
                    if (idx < 0 || idx >= molecule.NumberOfAtoms) continue;
                    molecule.atom[idx].show = true;
                    molecule.atom[idx].UseForHBonds = true;
                    if (molecule.atom[idx].style < 0 || molecule.atom[idx].style == 10)
                        molecule.atom[idx].style = Flags.Instance.Presentation;
                }
            }
        }

        /// <summary>
        /// For each recorded H-bond whose acceptor is still only a Cartesian
        /// position (H_BondedAtom == -1), create/reactivate the atom so the
        /// sphere is drawn together with the dashed H-bond rod.
        /// </summary>
        private static void EnsureHBondEndpointAtomsVisible(Molecule molecule)
        {
            var missing = new List<AcceptorEntry>();
            var keys = new HashSet<string>();

            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                int n = molecule.atom[i].H_NrBondedAtoms;
                if (n <= 0) continue;
                if (molecule.atom[i].H_BondedAtom == null) continue;

                for (int k = 0; k < n; k++)
                {
                    int accIdx = molecule.atom[i].H_BondedAtom[k];
                    if (accIdx >= 0 && accIdx < molecule.NumberOfAtoms)
                    {
                        molecule.atom[accIdx].show = true;
                        molecule.atom[accIdx].UseForHBonds = true;
                        if (molecule.atom[accIdx].style < 0 || molecule.atom[accIdx].style == 10)
                            molecule.atom[accIdx].style = Flags.Instance.Presentation;
                        RegisterHBondNeighborIfExtra(molecule, accIdx);
                        continue;
                    }

                    // No atom index — need to materialise from H_AccCartPos
                    if (molecule.atom[i].H_AccCartPos == null) continue;
                    Vector3 pos = molecule.atom[i].H_AccCartPos[k];
                    if (pos == Vector3.Zero && (molecule.atom[i].H_AccName == null
                        || string.IsNullOrEmpty(molecule.atom[i].H_AccName[k])))
                        continue;

                    // Already in atom[]?
                    int found = FindAtomAt(molecule, pos);
                    if (found >= 0)
                    {
                        molecule.atom[found].show = true;
                        molecule.atom[found].UseForHBonds = true;
                        if (molecule.atom[found].style < 0 || molecule.atom[found].style == 10)
                            molecule.atom[found].style = Flags.Instance.Presentation;
                        molecule.atom[i].H_BondedAtom[k] = found;
                        RegisterHBondNeighborIfExtra(molecule, found);
                        continue;
                    }

                    string key = $"{pos.X:F4}|{pos.Y:F4}|{pos.Z:F4}";
                    if (!keys.Add(key)) continue;

                    int z = 8;
                    if (molecule.atom[i].H_AccAtomicNumber != null &&
                        k < molecule.atom[i].H_AccAtomicNumber.Length &&
                        molecule.atom[i].H_AccAtomicNumber[k] > 0)
                        z = molecule.atom[i].H_AccAtomicNumber[k];

                    string name = molecule.atom[i].H_AccName != null &&
                                  k < molecule.atom[i].H_AccName.Length
                        ? molecule.atom[i].H_AccName[k] : "X";

                    missing.Add(new AcceptorEntry
                    {
                        Name = name ?? "X",
                        CartPos = pos,
                        AtomicNumber = z,
                        CellX = molecule.atom[i].H_AccCellX != null ? molecule.atom[i].H_AccCellX[k] : 0,
                        CellY = molecule.atom[i].H_AccCellY != null ? molecule.atom[i].H_AccCellY[k] : 0,
                        CellZ = molecule.atom[i].H_AccCellZ != null ? molecule.atom[i].H_AccCellZ[k] : 0,
                        SymmOpIndex = molecule.atom[i].H_AccSymmOpIndex != null
                            ? molecule.atom[i].H_AccSymmOpIndex[k] : 0,
                        MoleculeId = molecule.atom[i].H_AccMoleculeId != null
                            ? molecule.atom[i].H_AccMoleculeId[k] : -1,
                        IsOriginal = false,
                        OriginalAtomIndex = -1
                    });
                }
            }

            if (missing.Count > 0)
            {
                Debug.WriteLine($"[HBondFinder] Materialising {missing.Count} missing H-bond endpoint atoms");
                AddNeighborAcceptorsToMolecule(molecule, missing);
                UpdateHBondAcceptorIndices(molecule, missing);
            }
        }

        /// <summary>Find atom index at Cartesian position, or -1.</summary>
        private static int FindAtomAt(Molecule molecule, Vector3 cartPos)
        {
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (Vector3.DistanceSquared(molecule.atom[i].Coordinate, cartPos)
                    < DUPLICATE_TOLERANCE_SQ)
                    return i;
            }
            return -1;
        }

        /// <summary>Ensure donor lists H as covalently bonded (and vice versa).</summary>
        private static void LinkCovalentIfMissing(Molecule molecule, int donorIdx, int hIdx)
        {
            void Link(int from, int to)
            {
                EnsureBondArrays(molecule, from);
                int n = molecule.atom[from].NrBondedAtoms;
                for (int k = 0; k < n; k++)
                    if (molecule.atom[from].BondedAtom[k] == to) return;
                if (n >= Flags.Instance.MAX_BONDED_ATOMS) return;

                float dist = Vector3.Distance(
                    molecule.atom[from].Coordinate, molecule.atom[to].Coordinate);
                molecule.atom[from].BondedAtom[n] = to;
                molecule.atom[from].Distance[n] = dist;
                molecule.atom[from].BondOccupancy[n] =
                    Math.Min(molecule.atom[from].occupancy, molecule.atom[to].occupancy);
                molecule.atom[from].AngleZ[n] = 0;
                molecule.atom[from].AngleXY[n] = 0;
                molecule.atom[from].NrBondedAtoms = n + 1;
            }
            Link(donorIdx, hIdx);
            Link(hIdx, donorIdx);
        }

        private static void EnsureBondArrays(Molecule molecule, int i)
        {
            int max = Flags.Instance.MAX_BONDED_ATOMS;
            if (molecule.atom[i].BondedAtom == null || molecule.atom[i].BondedAtom.Length < max)
            {
                molecule.atom[i].BondedAtom = new int[max];
                molecule.atom[i].Distance = new float[max];
                molecule.atom[i].BondOccupancy = new float[max];
                molecule.atom[i].AngleZ = new float[max];
                molecule.atom[i].AngleXY = new float[max];
                molecule.atom[i].NrBondedAtoms = 0;
            }
        }

        /// <summary>
        /// Write H-bond record without seenBonds check (already deduped in PASS B).
        /// </summary>
        private static bool RecordBondDirect(
            Molecule molecule,
            int donorIdx, int hIdx,
            Vector3 donorPos, Vector3 hPos,
            AcceptorEntry acc,
            bool drawFromH, int maxHBond)
        {
            EnsureArraysForAtom(molecule, donorIdx);
            EnsureArraysForAtom(molecule, hIdx);

            int hostIdx = drawFromH ? hIdx : donorIdx;
            EnsureArraysForAtom(molecule, hostIdx);

            int n = molecule.atom[hostIdx].H_NrBondedAtoms;
            if (n >= maxHBond) return false;

            Vector3 accPos = acc.CartPos;
            float dDA = Vector3.Distance(donorPos, accPos);
            float dHA = Vector3.Distance(hPos, accPos);
            Vector3 hostPos = molecule.atom[hostIdx].Coordinate;

            // Avoid exact duplicate on host
            for (int k = 0; k < n; k++)
            {
                if (molecule.atom[hostIdx].H_AccCartPos != null &&
                    Vector3.DistanceSquared(molecule.atom[hostIdx].H_AccCartPos[k], accPos)
                    < DUPLICATE_TOLERANCE_SQ)
                    return false;
            }

            molecule.atom[donorIdx].UseForHBonds = true;
            molecule.atom[hIdx].UseForHBonds = true;
            if (acc.IsOriginal && acc.OriginalAtomIndex >= 0)
                molecule.atom[acc.OriginalAtomIndex].UseForHBonds = true;

            molecule.atom[hostIdx].H_BondedAtom[n] =
                acc.IsOriginal ? acc.OriginalAtomIndex : -1;
            molecule.atom[hostIdx].H_HydrogenAtom[n] = hIdx;
            molecule.atom[hostIdx].H_Distance[n] = dDA;
            molecule.atom[hostIdx].H_DonorAcceptorDistance[n] = dDA;
            molecule.atom[hostIdx].H_HydrogenAcceptorDistance[n] = dHA;
            molecule.atom[hostIdx].H_DHA_Angle[n] = 0; // filled for display if needed
            molecule.atom[hostIdx].H_AngleZ[n] = (float)Math.Asin(
                Math.Clamp((hostPos.Z - accPos.Z) / Math.Max(dDA, 1e-4f), -1f, 1f));
            molecule.atom[hostIdx].H_AngleXY[n] = ComputeAngleXY(hostPos, accPos);
            molecule.atom[hostIdx].H_AccCellX[n] = acc.CellX;
            molecule.atom[hostIdx].H_AccCellY[n] = acc.CellY;
            molecule.atom[hostIdx].H_AccCellZ[n] = acc.CellZ;
            molecule.atom[hostIdx].H_AccSymmOpIndex[n] = acc.SymmOpIndex;
            molecule.atom[hostIdx].H_AccName[n] = acc.Name;
            molecule.atom[hostIdx].H_AccCartPos[n] = accPos;
            molecule.atom[hostIdx].H_AccAtomicNumber[n] = acc.AtomicNumber;
            molecule.atom[hostIdx].H_AccMoleculeId[n] = acc.MoleculeId;
            molecule.atom[hostIdx].H_NrBondedAtoms = n + 1;
            return true;
        }

 /// <summary>
 /// Zeruje all H-bondy w molekule.
 /// </summary>
        public static void Clear(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null) return;

            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                molecule.atom[i].H_NrBondedAtoms = 0;
                molecule.atom[i].PrecomputedHBondMatrix = null;

                if (molecule.atom[i].H_BondedAtom != null)
                {
                    for (int k = 0; k < molecule.atom[i].H_BondedAtom.Length; k++)
                    {
                        molecule.atom[i].H_BondedAtom[k] = -1;
                        if (molecule.atom[i].H_HydrogenAtom != null)
                            molecule.atom[i].H_HydrogenAtom[k] = -1;
                        if (molecule.atom[i].H_AccName != null)
                            molecule.atom[i].H_AccName[k] = null;
                        if (molecule.atom[i].H_AccMoleculeId != null)
                            molecule.atom[i].H_AccMoleculeId[k] = -1;
                    }
                }
            }
        }

        #endregion

        #region Private — Remove (flag reset)

 /// <summary>
 /// ⭐ ZMIENIONE: NIE usuwa atoms. Tylko zeruje flag UseForHBonds
 /// na atomach zarejestrowanych w HBondNeighborAtomIndices.
 /// Thanks to this indexy w atom[] afterzostają stabilne, a stare atoms
 /// (z flagą = false) są after prostu invisible for kolejnych obliczeń.
 ///
 /// NIE rusza atoms structure (nawet z CellShift != 0).
 /// </summary>
        private static void RemoveOwnNeighborAtoms(Molecule molecule)
        {
            if (molecule.HBondNeighborAtomIndices == null ||
                molecule.HBondNeighborAtomIndices.Count == 0)
                return;

            int resetCount = 0;
            foreach (var idx in molecule.HBondNeighborAtomIndices)
            {
                if (idx < 0 || idx >= molecule.NumberOfAtoms) continue;
                if (!molecule.atom[idx].UseForHBonds) continue;

                molecule.atom[idx].UseForHBonds = false;
                resetCount++;
            }

            molecule.HBondNeighborAtomIndices.Clear();

            Debug.WriteLine($"[HBondFinder] Reset UseForHBonds on {resetCount} atoms " +
                            $"(total remains: {molecule.NumberOfAtoms})");
        }

 /// <summary>
 /// Adde wybrane akceptory z neighbouring cells do atom[].
 /// Rejestruje ich indexy w molecule.HBondNeighborAtomIndices,
 /// so that one can było je później wyłączyć flagą.
 ///
 /// ⭐ Setia UseForHBonds = true, UseForShortContacts = false.
 /// ⭐ Checks duplikaty after coordinates — nie adde atom if
 /// taki sam already exists i jest AKTYWNY for H-bondów.
 /// </summary>

        /// <summary>
        /// True only for atoms that were appended as lattice neighbours (not base-structure atoms).
        /// Base atoms must never enter HBondNeighborAtomIndices — TurnOffHBonds would delete them.
        /// </summary>
        private static bool IsExtraNeighborAtom(Molecule molecule, int idx)
        {
            if (idx < 0 || idx >= molecule.NumberOfAtoms) return false;
            var a = molecule.atom[idx];
            if (a.CellShiftX != 0 || a.CellShiftY != 0 || a.CellShiftZ != 0)
                return true;
            if (molecule.ExpandedAtomIndices != null && molecule.ExpandedAtomIndices.Contains(idx))
                return true;
            // Atom created solely for H-bonds: H-bonds on, short contacts off, and already tracked
            if (a.UseForHBonds && !a.UseForShortContacts
                && molecule.HBondNeighborAtomIndices != null
                && molecule.HBondNeighborAtomIndices.Contains(idx))
                return true;
            return false;
        }

        private static void RegisterHBondNeighborIfExtra(Molecule molecule, int idx)
        {
            if (!IsExtraNeighborAtom(molecule, idx))
                return;
            if (molecule.HBondNeighborAtomIndices == null)
                molecule.HBondNeighborAtomIndices = new List<int>();
            if (!molecule.HBondNeighborAtomIndices.Contains(idx))
                molecule.HBondNeighborAtomIndices.Add(idx);
        }

        /// <summary>Copy ADP (Ue/Un) from a same-name atom already in the molecule or from SymetryEquivalents source.</summary>
        private static void CopyAdpOntoAtom(Molecule molecule, int destIdx, string name, int atomicNumber)
        {
            if (destIdx < 0 || destIdx >= molecule.NumberOfAtoms) return;

            // Prefer an existing displayed atom with same name + Z that already has ADP
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (i == destIdx) continue;
                if (molecule.atom[i].AtomicNumber != atomicNumber) continue;
                if (!string.Equals(molecule.atom[i].name, name, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!molecule.atom[i].isAnisotropic && molecule.atom[i].Ue.M11 <= 0)
                    continue;
                molecule.atom[destIdx].isAnisotropic = molecule.atom[i].isAnisotropic;
                molecule.atom[destIdx].Ue = molecule.atom[i].Ue;
                molecule.atom[destIdx].Un = molecule.atom[i].Un;
                molecule.atom[destIdx].occupancy = molecule.atom[i].occupancy;
                return;
            }

            // Fallback: isotropic ball radius from element (keep isAnisotropic false)
            // Leave matrices as default; ellipsoid renderer will treat as iso if needed.
        }

        private static void AddNeighborAcceptorsToMolecule(
            Molecule molecule,
            List<AcceptorEntry> acceptors)
        {
            if (acceptors.Count == 0) return;

            int style = Flags.Instance.Presentation;
            int oldCount = molecule.NumberOfAtoms;

 // Afterlicz ile faktycznie dodamy (without duplikatów activech)
            var toAdd = new List<AcceptorEntry>();
            foreach (var acc in acceptors)
            {
                bool exists = false;
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    if (Vector3.DistanceSquared(molecule.atom[i].Coordinate, acc.CartPos)
                        < DUPLICATE_TOLERANCE_SQ)
                    {
                        // Reactivate + force visible (may have been SC-only or previous H-bond pass)
                        molecule.atom[i].UseForHBonds = true;
                        molecule.atom[i].show = true;
                        if (molecule.atom[i].style < 0)
                            molecule.atom[i].style = Flags.Instance.Presentation;
                        // NEVER register base-structure atoms — only true lattice extras
                        RegisterHBondNeighborIfExtra(molecule, i);
                        exists = true;
                        break;
                    }
                }
                if (!exists) toAdd.Add(acc);
            }

            if (toAdd.Count == 0)
            {
                Debug.WriteLine("[HBondFinder] All neighbor acceptors already active - skipping add");
                return;
            }

            var newArray = new Molecule.Atom[oldCount + toAdd.Count];
            Array.Copy(molecule.atom, newArray, oldCount);
            molecule.atom = newArray;

            foreach (var acc in toAdd)
            {
                int idx = molecule.NumberOfAtoms;

 // Alokacja tablic bonds kowalencyjnych
                molecule.atom[idx].Distance = new float[Flags.Instance.MAX_BONDED_ATOMS];
                molecule.atom[idx].BondOccupancy = new float[Flags.Instance.MAX_BONDED_ATOMS];
                molecule.atom[idx].BondedAtom = new int[Flags.Instance.MAX_BONDED_ATOMS];
                molecule.atom[idx].AngleZ = new float[Flags.Instance.MAX_BONDED_ATOMS];
                molecule.atom[idx].AngleXY = new float[Flags.Instance.MAX_BONDED_ATOMS];
                molecule.atom[idx].NrBondedAtoms = 0;

 // Alokacja tablic H-bond
                molecule.atom[idx].H_Distance = new float[Flags.Instance.MAX_H_BONDED_ATOMS];
                molecule.atom[idx].H_AngleZ = new float[Flags.Instance.MAX_H_BONDED_ATOMS];
                molecule.atom[idx].H_AngleXY = new float[Flags.Instance.MAX_H_BONDED_ATOMS];
                molecule.atom[idx].H_BondedAtom = new int[Flags.Instance.MAX_H_BONDED_ATOMS];
                molecule.atom[idx].H_NrBondedAtoms = 0;

 // Wypełnienie pól
                molecule.atom[idx].Coordinate = acc.CartPos;
                molecule.atom[idx].AtomicNumber = acc.AtomicNumber;
                molecule.atom[idx].name = acc.Name;
                molecule.atom[idx].style = style;
                molecule.atom[idx].occupancy = 1.0f;
                molecule.atom[idx].show = true;
                molecule.atom[idx].selected = false;
                molecule.atom[idx].isAnisotropic = false;
                molecule.atom[idx].SymmetryEquivalentNumberInCell = acc.SymmOpIndex;
                molecule.atom[idx].IndividualMoleculeInCell = acc.MoleculeId;
                molecule.atom[idx].CustomColor = new Vector3(-1f, -1f, -1f);
                molecule.atom[idx].CellShiftX = acc.CellX;
                molecule.atom[idx].CellShiftY = acc.CellY;
                molecule.atom[idx].CellShiftZ = acc.CellZ;

 // Reset contact fields (new atom nie ma still contacts)
                molecule.atom[idx].ContactCount = 0;
                molecule.atom[idx].ContactDistance = null;
                molecule.atom[idx].ContactAtom = null;
                molecule.atom[idx].ContactAngleZ = null;
                molecule.atom[idx].ContactAngleXY = null;
                molecule.atom[idx].PrecomputedContactMatrix = null;
                molecule.atom[idx].ContactIsNew = null;

 // Reset precomputed matrices
                molecule.atom[idx].PrecomputedBondMatrix = null;
                molecule.atom[idx].PrecomputedHBondMatrix = null;

                molecule.atom[idx].Z_Depth = 0;

 // ⭐ FLAGI: only H-bonds, NIE Short Contacts
                molecule.atom[idx].UseForHBonds = true;
                molecule.atom[idx].UseForShortContacts = false;

                // Full H-bond array set (Presentation / ShortContacts often leave holes)
                EnsureArraysForAtom(molecule, idx);

                // Copy ADP so ellipsoid mode can draw the acceptor like the source atom
                CopyAdpOntoAtom(molecule, idx, acc.Name, acc.AtomicNumber);
                // Prefer current presentation style (ellipsoids / balls&sticks)
                molecule.atom[idx].style = Flags.Instance.Presentation;

                // Register only newly appended atoms (safe to remove on TurnOffHBonds)
                if (molecule.HBondNeighborAtomIndices == null)
                    molecule.HBondNeighborAtomIndices = new List<int>();
                molecule.HBondNeighborAtomIndices.Add(idx);

                molecule.NumberOfAtoms++;
            }

            Debug.WriteLine($"[HBondFinder] Added {toAdd.Count} neighbor acceptors " +
                            $"(skipped {acceptors.Count - toAdd.Count} duplicates)");
        }

 /// <summary>
 /// After dodaniu acceptors z neighbouring cells — set H_BondedAtom[k]
 /// na true indexy w atom[] (te which have flag UseForHBonds=true).
 /// </summary>
        private static void UpdateHBondAcceptorIndices(
            Molecule molecule,
            List<AcceptorEntry> addedAcceptors)
        {
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                for (int k = 0; k < molecule.atom[i].H_NrBondedAtoms; k++)
                {
                    if (molecule.atom[i].H_BondedAtom[k] >= 0) continue;
                    if (molecule.atom[i].H_AccCartPos == null || k >= molecule.atom[i].H_AccCartPos.Length)
                        continue;

                    Vector3 accPos = molecule.atom[i].H_AccCartPos[k];

                    // Match ANY atom at this position (not only UseForHBonds) —
                    // neighbour acceptors may have been added without the flag
                    // set yet, or reactivated from short-contacts-only state.
                    for (int j = 0; j < molecule.NumberOfAtoms; j++)
                    {
                        if (Vector3.DistanceSquared(molecule.atom[j].Coordinate, accPos)
                            < DUPLICATE_TOLERANCE_SQ)
                        {
                            molecule.atom[j].UseForHBonds = true;
                            molecule.atom[j].show = true;
                            if (molecule.atom[j].style < 0 || molecule.atom[j].style == 10)
                                molecule.atom[j].style = Flags.Instance.Presentation;
                            molecule.atom[i].H_BondedAtom[k] = j;
                            RegisterHBondNeighborIfExtra(molecule, j);
                            break;
                        }
                    }
                }
            }
        }

        #endregion

        #region Private — Helpers

 /// <summary>
 /// Zapewnia że tablice H-bond for danego atom są zaalokowane.
 /// </summary>
        private static void EnsureArraysForAtom(Molecule molecule, int i)
        {
            if (i < 0 || i >= molecule.NumberOfAtoms) return;

            int n = Flags.Instance.MAX_H_BONDED_ATOMS;

            bool needAlloc =
                molecule.atom[i].H_BondedAtom == null || molecule.atom[i].H_BondedAtom.Length < n ||
                molecule.atom[i].H_HydrogenAtom == null || molecule.atom[i].H_HydrogenAtom.Length < n ||
                molecule.atom[i].H_Distance == null || molecule.atom[i].H_Distance.Length < n ||
                molecule.atom[i].H_DonorAcceptorDistance == null || molecule.atom[i].H_DonorAcceptorDistance.Length < n ||
                molecule.atom[i].H_HydrogenAcceptorDistance == null || molecule.atom[i].H_HydrogenAcceptorDistance.Length < n ||
                molecule.atom[i].H_DHA_Angle == null || molecule.atom[i].H_DHA_Angle.Length < n ||
                molecule.atom[i].H_AngleZ == null || molecule.atom[i].H_AngleZ.Length < n ||
                molecule.atom[i].H_AngleXY == null || molecule.atom[i].H_AngleXY.Length < n ||
                molecule.atom[i].H_AccCellX == null || molecule.atom[i].H_AccCellX.Length < n ||
                molecule.atom[i].H_AccName == null || molecule.atom[i].H_AccName.Length < n ||
                molecule.atom[i].H_AccCartPos == null || molecule.atom[i].H_AccCartPos.Length < n ||
                molecule.atom[i].H_AccAtomicNumber == null || molecule.atom[i].H_AccAtomicNumber.Length < n ||
                molecule.atom[i].H_AccMoleculeId == null || molecule.atom[i].H_AccMoleculeId.Length < n;

            if (!needAlloc) return;

            molecule.atom[i].H_BondedAtom = new int[n];
            molecule.atom[i].H_HydrogenAtom = new int[n];
            molecule.atom[i].H_Distance = new float[n];
            molecule.atom[i].H_DonorAcceptorDistance = new float[n];
            molecule.atom[i].H_HydrogenAcceptorDistance = new float[n];
            molecule.atom[i].H_DHA_Angle = new float[n];
            molecule.atom[i].H_AngleZ = new float[n];
            molecule.atom[i].H_AngleXY = new float[n];
            molecule.atom[i].PrecomputedHBondMatrix = new Matrix[n];

            molecule.atom[i].H_AccCellX = new int[n];
            molecule.atom[i].H_AccCellY = new int[n];
            molecule.atom[i].H_AccCellZ = new int[n];
            molecule.atom[i].H_AccSymmOpIndex = new int[n];
            molecule.atom[i].H_AccName = new string[n];
            molecule.atom[i].H_AccCartPos = new Vector3[n];
            molecule.atom[i].H_AccAtomicNumber = new int[n];
            molecule.atom[i].H_AccMoleculeId = new int[n];

            for (int k = 0; k < n; k++)
            {
                molecule.atom[i].H_BondedAtom[k] = -1;
                molecule.atom[i].H_HydrogenAtom[k] = -1;
                molecule.atom[i].H_Distance[k] = 0f;
                molecule.atom[i].H_DonorAcceptorDistance[k] = 0f;
                molecule.atom[i].H_HydrogenAcceptorDistance[k] = 0f;
                molecule.atom[i].H_DHA_Angle[k] = 0f;
                molecule.atom[i].H_AngleZ[k] = 0f;
                molecule.atom[i].H_AngleXY[k] = 0f;
                molecule.atom[i].PrecomputedHBondMatrix[k] = Matrix.Identity;

                molecule.atom[i].H_AccCellX[k] = 0;
                molecule.atom[i].H_AccCellY[k] = 0;
                molecule.atom[i].H_AccCellZ[k] = 0;
                molecule.atom[i].H_AccSymmOpIndex[k] = 0;
                molecule.atom[i].H_AccName[k] = null;
                molecule.atom[i].H_AccCartPos[k] = Vector3.Zero;
                molecule.atom[i].H_AccAtomicNumber[k] = 0;
                molecule.atom[i].H_AccMoleculeId[k] = -1;
            }
        }

        private static float ComputeAngleXY(Vector3 from, Vector3 to)
        {
            float dx = from.X - to.X;
            float dy = from.Y - to.Y;
            if (Math.Abs(dx) < 1e-6f) return 0f;
            return -(float)(MathHelperTal.ToRadians(180) - Math.Atan2(dy, dx));
        }

        #endregion

        #region Nested Types

        private struct AcceptorEntry
        {
            public string Name;
            public Vector3 CartPos;
            public int AtomicNumber;
            public int CellX, CellY, CellZ;
            public int SymmOpIndex;
            public int MoleculeId;
            public bool IsOriginal;
            public int OriginalAtomIndex;
        }

        #endregion
    }
}