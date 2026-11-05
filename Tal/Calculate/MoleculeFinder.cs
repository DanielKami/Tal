// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Calculation module: MoleculeFinder.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// MoleculeFinder — generyczny BFS do wykrywania molecules w dowolnym zbiorze atoms.
//
// ⭐ Wspólne source prawdy for:
// - Molecule.DetectMolecules (existce API)
// - HirshfeldScene.ExtractMolecules (new — builds własną cell)
//
// ⭐ KLUCZOWE cechy:
// - useMinimumImage: parameter decydujący czy BFS uses minimum-image
// (frakcjonalna redukcja mod 1) przy liczeniu distance bonds.
// Domyślnie TRUE (for Molecule.DetectMolecules).
// For Hirshfelda FALSE — bo cell bazowa zawiera full molecule,
// a false bonds przez boundary (np. between dwiema niezależnymi
// moleculemi w P-1) NIE są afterżądane.
// - Próg bonds spójny z Flags (BOND_FACTOR, BOND_FACTOR_METALS,
// H_COVALENT_MAX_DISTANCE) — identyczny jak Calculate_Bonds3D.
// - IsHydrogenBond przerywa graf na mostkach H···N/O/F — PEŁNA logska
// oryginalna z Molecule.DetectMolecules (z szukaniem donora).
// IsHydrogenBond ZAWSZE uses minimum-image for distance H···A —
// hydrogen bond przez boundary cell jest realny chemicznie.
// - Mutuje IndividualMoleculeInCell na atomach (if resetIndividualIds=true).
// - Returns list fragmentów z indexami atoms, centre, bbox.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Tal.Calculate
{
    public static class MoleculeFinder
    {
        #region Types

 /// <summary>
 /// Fragment (molecule) znaleziony przez BFS.
 /// </summary>
        public class Fragment
        {
            public int FragmentId;
            public List<int> AtomIndices = new List<int>();
            public Vector3 CenterCart;
            public Vector3 BBoxMin;
            public Vector3 BBoxMax;

            public int AtomCount => AtomIndices.Count;
        }

        #endregion

        #region Constants

 // Progi for IsHydrogenBond — DOKŁADNIE jak w oryginalnej
 // Molecule.IsHydrogenBond (stałe z Molecule.cs).
        private const float HYDROGEN_BOND_THRESHOLD = 1.35f; // below this = covalent X–H, not bridge
        private const float HYDROGEN_BOND_MAX_DISTANCE = 3.0f;

 // Fallback for H_COVALENT_MAX_DISTANCE when Flags.Instance zwróci 0
        private const float H_COVALENT_THRESHOLD_DEFAULT = 1.3f;

 // Zawithoutpieczenie before nieskończoną pętlą
        private const int MAX_ATOMS_PER_MOLECULE = 100000;

        #endregion

        #region Public API

 /// <summary>
 /// Finds molecule w afterdanej list atoms.
 ///
 /// ⭐ useMinimumImage:
 /// - TRUE (by default) — BFS uses minimum-image (redukcja mod 1).
 /// Łączy molecule przez boundary cell.
 /// Used by Molecule.DetectMolecules.
 /// - FALSE — BFS uses distance withoutafterśredniej (without redukcji).
 /// NIE łączy przez boundary cell.
 /// Used by HirshfeldScene.
 ///
 /// IsHydrogenBond ZAWSZE uses minimum-image (niezależnie od flagi) —
 /// hydrogen bond przez boundary cell jest realny chemicznie.
 /// </summary>
 /// <param name="atoms">Lista atoms (mutowana — IndividualMoleculeInCell).</param>
 /// <param name="fracToCart">Macierz frakcjonalne → Cartesian.</param>
 /// <param name="resetIndividualIds">Czy wyzerować IndividualMoleculeInCell before BFS.</param>
 /// <param name="donorSearchList">
 /// Lista do wyszukiwania donors H-bond (krok 4 IsHydrogenBond).
 /// If null — użyta zostanie ta sama list co <paramref name="atoms"/>.
 /// </param>
 /// <param name="useMinimumImage">
 /// Czy BFS uses minimum-image przy liczeniu distance bonds.
 /// Domyślnie TRUE. For Hirshfelda — FALSE.
 /// </param>
 /// <param name="log">Callback logowania (opcjonalny).</param>
 /// <returns>Lista fragmentów (molecules).</returns>
        public static List<Fragment> Find(
            List<Molecule.AtomSimple> atoms,
            Matrix fracToCart,
            bool resetIndividualIds = true,
            List<Molecule.AtomSimple> donorSearchList = null,
            bool useMinimumImage = true,
            Action<string> log = null,
            IList<SymmetryElements.ReadSymm> symmetryOps = null)
        {
            var fragments = new List<Fragment>();

            if (atoms == null || atoms.Count == 0)
            {
                log?.Invoke("[MoleculeFinder] Atom list is empty");
                return fragments;
            }

            int n = atoms.Count;

            log?.Invoke($"[MoleculeFinder] START — {n} atoms");
            log?.Invoke($"[MoleculeFinder] useMinimumImage = {useMinimumImage}");

 // ⭐ Domyślna list donors = ta sama list
            if (donorSearchList == null)
                donorSearchList = atoms;

            log?.Invoke($"[MoleculeFinder] donorSearchList.Count = {donorSearchList.Count}");

 // ═══ Reset ID ═══
            if (resetIndividualIds)
            {
                for (int i = 0; i < n; i++)
                {
                    var a = atoms[i];
                    a.IndividualMoleculeInCell = -1;
                    atoms[i] = a;
                }
            }

 // ═══ Cache flags ═══
            var f = Flags.Instance;
            // Prefer MOLECULE_BOND_FACTOR (looser than display BOND_FACTOR) so fragments
            // that are still one chemical molecule but have slightly long bonds join.
            float bondFactor = f.MOLECULE_BOND_FACTOR > 0.1f
                ? f.MOLECULE_BOND_FACTOR
                : f.BOND_FACTOR;
            float bondFactorMetals = f.BOND_FACTOR_METALS > bondFactor
                ? f.BOND_FACTOR_METALS
                : Math.Max(bondFactor, f.BOND_FACTOR_METALS);
            float hCovalentMax = f.H_COVALENT_MAX_DISTANCE > 0f
                ? f.H_COVALENT_MAX_DISTANCE
                : H_COVALENT_THRESHOLD_DEFAULT;
            // Slightly more tolerant X–H for molecule assembly (elongated refined H)
            if (hCovalentMax < 1.35f)
                hCovalentMax = 1.35f;

            log?.Invoke($"[MoleculeFinder] Flags: moleculeBondFactor={bondFactor:F3}, " +
                        $"metals={bondFactorMetals:F3}, hCovalentMax={hCovalentMax:F3}");

 // ═══ Frakcjonalne positions — kopia for minimum-image ═══
            var fracPos = new Vector3[n];
            var zList = new int[n];
            for (int i = 0; i < n; i++)
            {
                fracPos[i] = new Vector3(atoms[i].x, atoms[i].y, atoms[i].z);
                zList[i] = atoms[i].AtomicNumber;
            }

            int currentFragmentId = 0;
            int assignedCount = 0;

 // ═══ Main loop ═══
            while (assignedCount < n)
            {
                int startIndex = -1;
                for (int i = 0; i < n; i++)
                {
                    if (atoms[i].IndividualMoleculeInCell == -1)
                    {
                        startIndex = i;
                        break;
                    }
                }

                if (startIndex == -1) break;

                var fragment = new Fragment { FragmentId = currentFragmentId };

                var queue = new Queue<int>();
                queue.Enqueue(startIndex);

                {
                    var startAtom = atoms[startIndex];
                    startAtom.IndividualMoleculeInCell = currentFragmentId;
                    atoms[startIndex] = startAtom;
                    assignedCount++;
                }

                int fragmentSize = 1;

 // ═══ BFS ═══
                while (queue.Count > 0)
                {
                    int curIdx = queue.Dequeue();
                    int curZ = zList[curIdx];
                    Vector3 curFrac = fracPos[curIdx];

                    for (int j = 0; j < n; j++)
                    {
                        if (atoms[j].IndividualMoleculeInCell != -1) continue;

                        int jZ = zList[j];
                        Vector3 jFrac = fracPos[j];

 // ═══ Mostek wodorowy — przerywa graf ═══
 // ⭐ IsHydrogenBond ZAWSZE uses minimum-image (niezależnie
 // od useMinimumImage) — mostek przez boundary jest realny.
                        if (IsHydrogenBond(
                                atoms[curIdx],
                                atoms[j],
                                fracToCart,
                                donorSearchList))
                            continue;

 // ═══ Bond distance (MI and/or symmetry images) ═══
                        float distance = MinInteratomicDistance(
                            curFrac, jFrac, fracToCart, useMinimumImage, symmetryOps);

 // ═══ Próg bonds ═══
                        float threshold = GetBondThreshold(curZ, jZ, bondFactor, bondFactorMetals, hCovalentMax);

                        if (distance < threshold)
                        {
                            var newAtom = atoms[j];
                            newAtom.IndividualMoleculeInCell = currentFragmentId;
                            atoms[j] = newAtom;
                            assignedCount++;
                            fragmentSize++;

                            queue.Enqueue(j);

                            if (fragmentSize > MAX_ATOMS_PER_MOLECULE)
                            {
                                log?.Invoke($"[MoleculeFinder] ⚠️ Fragment {currentFragmentId} " +
                                            $"exceeds MAX_ATOMS_PER_MOLECULE — breaking");
                                goto endBfs;
                            }
                        }
                    }
                }

            endBfs:

 // ═══ Zbierz indexy + statystyki ═══
                Vector3 sum = Vector3.Zero;
                Vector3 bmin = new Vector3(float.MaxValue);
                Vector3 bmax = new Vector3(float.MinValue);

                for (int i = 0; i < n; i++)
                {
                    if (atoms[i].IndividualMoleculeInCell != currentFragmentId) continue;

                    fragment.AtomIndices.Add(i);

                    var cart = Vector3.Transform(fracPos[i], fracToCart);
                    sum += cart;
                    bmin = Vector3.Min(bmin, cart);
                    bmax = Vector3.Max(bmax, cart);
                }

                if (fragment.AtomCount > 0)
                {
                    fragment.CenterCart = sum / fragment.AtomCount;
                    fragment.BBoxMin = bmin;
                    fragment.BBoxMax = bmax;
                }
                else
                {
                    fragment.CenterCart = Vector3.Zero;
                    fragment.BBoxMin = Vector3.Zero;
                    fragment.BBoxMax = Vector3.Zero;
                }

                log?.Invoke($"[MoleculeFinder] Fragment #{currentFragmentId}: " +
                            $"{fragment.AtomCount} atoms, " +
                            $"center=({fragment.CenterCart.X:F2}," +
                            $"{fragment.CenterCart.Y:F2}," +
                            $"{fragment.CenterCart.Z:F2})");

                fragments.Add(fragment);
                currentFragmentId++;
            }

            // ═══ Second pass: merge fragments linked by a near-covalent contact ═══
            // Catches cases where first BFS missed a slightly long bond (disorder,
            // heavy-atom distances, metal–ligand) without bridging true H-bonds.
            fragments = MergeNearbyFragments(
                atoms, fragments, fracPos, zList, fracToCart,
                donorSearchList, bondFactor, bondFactorMetals, hCovalentMax,
                useMinimumImage, symmetryOps, log);

            log?.Invoke($"[MoleculeFinder] FINISHED — {fragments.Count} fragments");

            return fragments;
        }

        /// <summary>
        /// Merge pairs of fragments if any inter-fragment atom pair is within an
        /// expanded covalent threshold and is not a hydrogen-bond bridge.
        /// </summary>
        private static List<Fragment> MergeNearbyFragments(
            List<Molecule.AtomSimple> atoms,
            List<Fragment> fragments,
            Vector3[] fracPos,
            int[] zList,
            Matrix fracToCart,
            List<Molecule.AtomSimple> donorSearchList,
            float bondFactor,
            float bondFactorMetals,
            float hCovalentMax,
            bool useMinimumImage,
            IList<SymmetryElements.ReadSymm> symmetryOps,
            Action<string> log)
        {
            if (fragments == null || fragments.Count <= 1)
                return fragments;

            const float expand = 1.12f; // ~12% more generous than first pass
            int n = atoms.Count;
            int fragCount = fragments.Count;

            // Union-Find
            int[] parent = new int[fragCount];
            for (int i = 0; i < fragCount; i++) parent[i] = i;

            int Find(int x)
            {
                while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
                return x;
            }
            void Union(int a, int b)
            {
                a = Find(a); b = Find(b);
                if (a != b) parent[b] = a;
            }

            // Map atom -> fragment index in list
            var atomToFrag = new int[n];
            for (int i = 0; i < n; i++) atomToFrag[i] = -1;
            for (int fi = 0; fi < fragCount; fi++)
            {
                foreach (int ai in fragments[fi].AtomIndices)
                    if (ai >= 0 && ai < n) atomToFrag[ai] = fi;
            }

            for (int i = 0; i < n; i++)
            {
                int fi = atomToFrag[i];
                if (fi < 0) continue;

                for (int j = i + 1; j < n; j++)
                {
                    int fj = atomToFrag[j];
                    if (fj < 0 || fj == fi) continue;
                    if (Find(fi) == Find(fj)) continue;

                    if (IsHydrogenBond(atoms[i], atoms[j], fracToCart, donorSearchList))
                        continue;

                    float distance = MinInteratomicDistance(
                        fracPos[i], fracPos[j], fracToCart, useMinimumImage, symmetryOps);
                    float threshold = GetBondThreshold(zList[i], zList[j], bondFactor, bondFactorMetals, hCovalentMax) * expand;

                    if (distance < threshold)
                        Union(fi, fj);
                }
            }

            // Rebuild fragments from unions
            var groups = new Dictionary<int, List<int>>();
            for (int fi = 0; fi < fragCount; fi++)
            {
                int root = Find(fi);
                if (!groups.TryGetValue(root, out var list))
                {
                    list = new List<int>();
                    groups[root] = list;
                }
                list.AddRange(fragments[fi].AtomIndices);
            }

            if (groups.Count == fragCount)
                return fragments; // nothing merged

            var merged = new List<Fragment>();
            int newId = 0;
            foreach (var kvp in groups)
            {
                var indices = kvp.Value;
                // unique
                var set = new HashSet<int>(indices);
                var frag = new Fragment { FragmentId = newId };
                Vector3 sum = Vector3.Zero;
                Vector3 bmin = new Vector3(float.MaxValue);
                Vector3 bmax = new Vector3(float.MinValue);

                foreach (int ai in set)
                {
                    frag.AtomIndices.Add(ai);
                    var a = atoms[ai];
                    a.IndividualMoleculeInCell = newId;
                    atoms[ai] = a;
                    var cart = Vector3.Transform(fracPos[ai], fracToCart);
                    sum += cart;
                    bmin = Vector3.Min(bmin, cart);
                    bmax = Vector3.Max(bmax, cart);
                }

                if (frag.AtomCount > 0)
                {
                    frag.CenterCart = sum / frag.AtomCount;
                    frag.BBoxMin = bmin;
                    frag.BBoxMax = bmax;
                }
                merged.Add(frag);
                newId++;
            }

            log?.Invoke($"[MoleculeFinder] Merge pass: {fragCount} → {merged.Count} fragments");
            return merged;
        }

        #endregion


        #region Distance with symmetry images

        /// <summary>
        /// Minimum Cartesian distance between two fractional sites, considering
        /// unit-cell translations and (optionally) space-group images of site J.
        /// Needed for molecules on special positions (e.g. biphenyl C9–C9 across a 2-fold).
        /// </summary>
        private static float MinInteratomicDistance(
            Vector3 fracI,
            Vector3 fracJ,
            Matrix fracToCart,
            bool useMinimumImage,
            System.Collections.Generic.IList<SymmetryElements.ReadSymm> symmetryOps)
        {
            float best = float.MaxValue;

            void consider(Vector3 j)
            {
                if (useMinimumImage)
                {
                    float dfx = fracI.X - j.X;
                    float dfy = fracI.Y - j.Y;
                    float dfz = fracI.Z - j.Z;
                    dfx -= (float)Math.Round(dfx);
                    dfy -= (float)Math.Round(dfy);
                    dfz -= (float)Math.Round(dfz);
                    float d = Vector3.Transform(new Vector3(dfx, dfy, dfz), fracToCart).Length();
                    if (d < best) best = d;
                }
                else
                {
                    for (int tx = -1; tx <= 1; tx++)
                    for (int ty = -1; ty <= 1; ty++)
                    for (int tz = -1; tz <= 1; tz++)
                    {
                        float dfx = fracI.X - (j.X + tx);
                        float dfy = fracI.Y - (j.Y + ty);
                        float dfz = fracI.Z - (j.Z + tz);
                        float d = Vector3.Transform(new Vector3(dfx, dfy, dfz), fracToCart).Length();
                        if (d < best) best = d;
                    }
                }
            }

            consider(fracJ);

            if (symmetryOps != null)
            {
                for (int o = 0; o < symmetryOps.Count; o++)
                {
                    Vector3 img = ApplyReadSymm(symmetryOps[o], fracJ);
                    if (Math.Abs(img.X - fracJ.X) < 1e-5f
                        && Math.Abs(img.Y - fracJ.Y) < 1e-5f
                        && Math.Abs(img.Z - fracJ.Z) < 1e-5f)
                        continue;
                    consider(img);
                }
            }

            return best;
        }

        private static Vector3 ApplyReadSymm(SymmetryElements.ReadSymm matrix, Vector3 pos)
        {
            float[] p = { pos.X, pos.Y, pos.Z };
            float x = p[matrix.pos1[0]] * matrix.sign1[0] + p[matrix.pos2[0]] * matrix.sign2[0] + matrix.shift[0];
            float y = p[matrix.pos1[1]] * matrix.sign1[1] + p[matrix.pos2[1]] * matrix.sign2[1] + matrix.shift[1];
            float z = p[matrix.pos1[2]] * matrix.sign1[2] + p[matrix.pos2[2]] * matrix.sign2[2] + matrix.shift[2];
            return new Vector3(x, y, z);
        }

        #endregion

        #region Bond threshold

 /// <summary>
 /// Próg bonds — spójny z Molecule.Calculate_Bonds3D.
 /// </summary>
        private static float GetBondThreshold(
            int z1, int z2,
            float bondFactor,
            float bondFactorMetals,
            float hCovalentMax)
        {
            if (z1 == 1 || z2 == 1)
                return hCovalentMax;

            float bondCorr = (IsMetal(z1) || IsMetal(z2))
                ? bondFactorMetals
                : bondFactor;

            float r1 = VdwParameters.GetRadius(z1);
            float r2 = VdwParameters.GetRadius(z2);
            return (r1 + r2) * bondCorr;
        }

        private static bool IsMetal(int z)
        {
            return z == 3 || z == 4 ||
                   z >= 11 && z <= 14 ||
                   z >= 19 && z <= 30 ||
                   z >= 37 && z <= 50 ||
                   z >= 55 && z <= 83 ||
                   z >= 87 && z <= 118;
        }

        #endregion

        #region Hydrogen bond detection (pełna logika oryginału)

 /// <summary>
 /// Returns true if dwa atoms tworzą hydrogen bond (H···N/O/F).
 /// Mostek przerywa graf — H-bondowane molecule zostają osobne.
 ///
 /// ⭐ PEŁNA logska z Molecule.IsHydrogenBond.
 /// ⭐ ZAWSZE uses minimum-image for distance H···A (niezależnie od
 /// useMinimumImage w Find) — mostek przez boundary jest realny.
 /// </summary>
        private static bool IsHydrogenBond(
            Molecule.AtomSimple atom1,
            Molecule.AtomSimple atom2,
            Matrix fracToCart,
            List<Molecule.AtomSimple> donorSearchList)
        {
            bool atom1IsH = atom1.AtomicNumber == 1;
            bool atom2IsH = atom2.AtomicNumber == 1;

            if (atom1IsH == atom2IsH) return false;

            var hydrogenAtom = atom1IsH ? atom1 : atom2;
            var acceptorAtom = atom1IsH ? atom2 : atom1;

            int acceptorNumber = acceptorAtom.AtomicNumber;
            if (acceptorNumber != 7 && acceptorNumber != 8 && acceptorNumber != 9)
                return false;

 // ═══ Odległość H···A (minimum-image, frakcjonalne) ═══
            float dxHA = hydrogenAtom.x - acceptorAtom.x;
            float dyHA = hydrogenAtom.y - acceptorAtom.y;
            float dzHA = hydrogenAtom.z - acceptorAtom.z;

            dxHA -= (float)Math.Round(dxHA);
            dyHA -= (float)Math.Round(dyHA);
            dzHA -= (float)Math.Round(dzHA);

            if (Math.Abs(dxHA) > HYDROGEN_BOND_MAX_DISTANCE ||
                Math.Abs(dyHA) > HYDROGEN_BOND_MAX_DISTANCE ||
                Math.Abs(dzHA) > HYDROGEN_BOND_MAX_DISTANCE)
                return false;

            Vector3 fracDiffHA = new Vector3(dxHA, dyHA, dzHA);
            Vector3 cartDiffHA = Vector3.Transform(fracDiffHA, fracToCart);
            float distanceHA = cartDiffHA.Length();

            if (distanceHA < HYDROGEN_BOND_THRESHOLD) return false;
            if (distanceHA > HYDROGEN_BOND_MAX_DISTANCE) return false;

 // ═══ KROK 4 — szukanie donora ═══
            Molecule.AtomSimple? donorAtom = null;
            float minDonorDistance = float.MaxValue;

            if (donorSearchList != null)
            {
                for (int k = 0; k < donorSearchList.Count; k++)
                {
                    var atom = donorSearchList[k];

                    if (Math.Abs(atom.x - hydrogenAtom.x) > 1.5f ||
                        Math.Abs(atom.y - hydrogenAtom.y) > 1.5f ||
                        Math.Abs(atom.z - hydrogenAtom.z) > 1.5f)
                        continue;

                    if (Math.Abs(atom.x - hydrogenAtom.x) < 0.0001f &&
                        Math.Abs(atom.y - hydrogenAtom.y) < 0.0001f &&
                        Math.Abs(atom.z - hydrogenAtom.z) < 0.0001f)
                        continue;

                    if (Math.Abs(atom.x - acceptorAtom.x) < 0.0001f &&
                        Math.Abs(atom.y - acceptorAtom.y) < 0.0001f &&
                        Math.Abs(atom.z - acceptorAtom.z) < 0.0001f)
                        continue;

                    if (atom.AtomicNumber != 7 &&
                        atom.AtomicNumber != 8 &&
                        atom.AtomicNumber != 9)
                        continue;

                    float dx = atom.x - hydrogenAtom.x;
                    float dy = atom.y - hydrogenAtom.y;
                    float dz = atom.z - hydrogenAtom.z;

                    dx -= (float)Math.Round(dx);
                    dy -= (float)Math.Round(dy);
                    dz -= (float)Math.Round(dz);

                    if (Math.Abs(dx) > 1.5f || Math.Abs(dy) > 1.5f || Math.Abs(dz) > 1.5f)
                        continue;

                    Vector3 fracDiff = new Vector3(dx, dy, dz);
                    Vector3 cartDiff = Vector3.Transform(fracDiff, fracToCart);
                    float distance = cartDiff.Length();

                    if (distance < 1.2f && distance < minDonorDistance)
                    {
                        minDonorDistance = distance;
                        donorAtom = atom;
                    }
                }
            }

            if (donorAtom == null) return false;

 // ═══ Filtr końcowy: donor i akceptor w tej samej molekule? ═══
            if (donorAtom.Value.IndividualMoleculeInCell != -1 &&
                acceptorAtom.IndividualMoleculeInCell != -1 &&
                donorAtom.Value.IndividualMoleculeInCell ==
                acceptorAtom.IndividualMoleculeInCell)
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}