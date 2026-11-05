// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Calculation module: ShortContactsFinder.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace Tal.Calculate
{
 /// <summary>
 /// Statyczny finder krótkich contacts.
 ///
 /// ⭐ NOWE (2026): Zamiast usuwać atoms, we use flagi UseForShortContacts.
 /// - RemoveExpandedAtoms NIE usuwa atoms — only zeruje flag.
 /// - Calculate filtere after UseForShortContacts (KROK 6).
 /// - Indeksy w atom[] są stabilne → ContactAtom[] always wskazuje
 /// na correct atoms (koniec z długimi czerwonymi liniami).
 ///
 /// Wzorzec analogsczny do HBondFinder.Calculate(molecule).
 /// </summary>
    public static class ShortContactsFinder
    {
        #region Public API

 /// <summary>
 /// Main method — recalculates Short Contacts for molecule.
 ///
 /// Kolejność operacji:
 /// 1. ⭐ Zeruje flag UseForShortContacts na atomach rozszerzonych
 /// (NIE usuwa ich z atom[])
 /// 2. Clears stare dane Contact* na all atomach
 /// 3. Zaznacza wszystko if nic nie jest selected
 /// 4. Upewnia że SymetryEquivalents i molecule są wykryte
 /// 5. ⭐ For each atom z UseForShortContacts==true szuka neighbours
 /// 6. Adde znalezione atoms z flagami UseForShortContacts=true,
 /// UseForHBonds=false
 /// 7. Builds PrecomputedContactMatrix for drawing czerwonych kresek
 ///
 /// NIE woła Calculate_Bonds3D ani PrecomputeBondMatrices —
 /// to zostawiamy wywołującemu (MainForm.FindAllShortContacts).
 /// </summary>
        public static void Calculate(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null || molecule.NumberOfAtoms == 0)
            {
                Debug.WriteLine("[ShortContactsFinder] No atoms - skipping");
                return;
            }

            Debug.WriteLine("═══════════════════════════════════════════════════════");
            Debug.WriteLine($"[ShortContactsFinder] START: total={molecule.NumberOfAtoms}, " +
                            $"expanded={molecule.ExpandedAtomIndices.Count}");
            Debug.WriteLine("═══════════════════════════════════════════════════════");

 // KROK 1: ⭐ Zeruj flag UseForShortContacts na rozszerzonych atomach
            RemoveExpandedAtoms(molecule);

 // KROK 2: clear stare dane Contact*
            ClearAllContacts(molecule);

 // KROK 3: parametery z Flags
            float minDistance = Flags.Instance.ShortContactsVdwMinus;
            bool useVdwSum = Flags.Instance.ShortContactsUseVdwSum;
            bool allowInter = Flags.Instance.ShortContactsIntermolecular;
            bool allowIntra = Flags.Instance.ShortContactsIntramolecular;

            Debug.WriteLine($"[ShortContactsFinder] Params: useVdwSum={useVdwSum}, " +
                            $"minDist={minDistance:F2}, inter={allowInter}, intra={allowIntra}");

 // KROK 4: zaznacz wszystko if nic nie selected
            EnsureSelection(molecule);

 // KROK 5: upewnij że mamy SymetryEquivalents
            if (molecule.SymetryEquivalents == null || molecule.SymetryEquivalents.Count == 0)
            {
                molecule.CalculateSymmetryEquivalents();
                Debug.WriteLine($"[ShortContactsFinder] Recalculated SymetryEquivalents: " +
                                $"{molecule.SymetryEquivalents.Count}");
            }

 // KROK 5b: upewnij że molecule są wykryte
            EnsureMoleculeDetection(molecule);

 // KROK 6: main loop — ⭐ FILTRUJ UseForShortContacts
            int currentAtomCount = molecule.NumberOfAtoms;
            int totalContacts = 0;
            int atomsWithContacts = 0;

            for (int atomIdx = 0; atomIdx < currentAtomCount; atomIdx++)
            {
 // ⭐ POMIŃ atoms without flagi UseForShortContacts
                if (!molecule.atom[atomIdx].UseForShortContacts) continue;

                if (!molecule.atom[atomIdx].selected) continue;

 // skip atoms z neighbouring cells (already dodane wcześniej)
                if (molecule.atom[atomIdx].CellShiftX != 0 ||
                    molecule.atom[atomIdx].CellShiftY != 0 ||
                    molecule.atom[atomIdx].CellShiftZ != 0)
                    continue;

                Vector3 selectedPos = molecule.atom[atomIdx].Coordinate;

 // ⭐ Wspólna funkcja wyszukiwania neighbours
                var nearbyAtoms = FindNearbyAtomsFromSymmetry(
                    molecule, selectedPos, atomIdx,
                    filterIntraInter: true,
                    skipCellZero: false);

                if (nearbyAtoms.Count == 0) continue;

 // deduplikacja after coordinates fractional
                var uniqueAtoms = DeduplicateByPosition(nearbyAtoms);

 // add atoms do molecule.atom[]
                int oldCount = molecule.NumberOfAtoms;
                int added = AddNearbyAtomsInternal(molecule, uniqueAtoms);

                if (added == 0) continue;

 // save ContactDistance / ContactAtom / matrixe
                BuildContactMatrices(molecule, atomIdx, oldCount, selectedPos);

                totalContacts += molecule.atom[atomIdx].ContactCount;
                atomsWithContacts++;
            }

            Debug.WriteLine("═══════════════════════════════════════════════════════");
            Debug.WriteLine($"[ShortContactsFinder] END: contacts={totalContacts} " +
                            $"in {atomsWithContacts} atoms, total atoms={molecule.NumberOfAtoms}, " +
                            $"expanded={molecule.ExpandedAtomIndices.Count}");
            Debug.WriteLine("═══════════════════════════════════════════════════════");
        }

 /// <summary>
 /// Zeruje WSZYSTKIE afterla Contact* na all atomach molecule.
 /// Nie usuwa atoms — only clears dane contacts.
 /// </summary>
        public static void ClearAllContacts(Molecule molecule)
        {
            if (molecule == null || molecule.atom == null) return;

            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                ClearContactDataOnAtom(ref molecule.atom[i]);
            }

            Debug.WriteLine($"[ShortContactsFinder] ClearAllContacts: cleared {molecule.NumberOfAtoms} atoms");
        }

 /// <summary>
 /// ⭐ ZMIENIONE: NIE usuwa atoms z atom[].
 /// Tylko zeruje flag UseForShortContacts na atomach zarejestrowanych
 /// w ExpandedAtomIndices. Thanks to this indexy w atom[] afterzostają stabilne
 /// i ContactAtom[] always wskazuje na correct atoms.
 ///
 /// Dodatkowo clears dane Contact* na all atomach.
 /// </summary>
        public static void RemoveExpandedAtoms(Molecule molecule)
        {
            if (molecule.ExpandedAtomIndices == null || molecule.ExpandedAtomIndices.Count == 0)
            {
                Debug.WriteLine("[ShortContactsFinder] RemoveExpandedAtoms: nothing to reset");
                return;
            }

            int resetCount = 0;
            foreach (var idx in molecule.ExpandedAtomIndices)
            {
                if (idx < 0 || idx >= molecule.NumberOfAtoms) continue;
                if (!molecule.atom[idx].UseForShortContacts) continue;

                molecule.atom[idx].UseForShortContacts = false;
                resetCount++;
            }

            molecule.ExpandedAtomIndices.Clear();

            Debug.WriteLine($"[ShortContactsFinder] Reset UseForShortContacts on {resetCount} atoms " +
                            $"(total remains: {molecule.NumberOfAtoms})");

 // clear dane Contact* na afterzostałych
            ClearAllContacts(molecule);
        }

 /// <summary>
 /// Rejestruje indexy dodanych atoms w ExpandedAtomIndices.
 /// Called z AddNearbyAtomsInternal.
 /// </summary>
        public static void RegisterExpandedAtoms(Molecule molecule, int fromIndex, int count)
        {
            for (int i = 0; i < count; i++)
            {
                molecule.ExpandedAtomIndices.Add(fromIndex + i);
            }
            Debug.WriteLine($"[ShortContactsFinder] Registered {count} expanded atoms " +
                            $"(total: {molecule.ExpandedAtomIndices.Count})");
        }

 /// <summary>
 /// Adde atoms z list do molecule.atom[] z afterprawnym CellShift,
 /// alokacją tablic and rejestracją w ExpandedAtomIndices.
 ///
 /// ⭐ Setia UseForShortContacts = true, UseForHBonds = false.
 ///
 /// NOTE: NIE wywołuje Calculate_Bonds3D ani PrecomputeBondMatrices —
 /// to zostawiamy wywołującemu.
 ///
 /// Returns number dodanych atoms.
 /// </summary>
        public static int AddNearbyAtomsInternal(Molecule molecule, List<Molecule.AtomSimple> newAtoms)
        {
            if (molecule == null || newAtoms == null || newAtoms.Count == 0) return 0;

            int style = Flags.Instance.Presentation;
            Matrix U = Matrix.Transpose(molecule.FractionaltoCarttesianMatrix(0, 0, 0));
            Vector3 shift = molecule.MoleculeShift;

            int oldCount = molecule.NumberOfAtoms;
            var newArray = new Molecule.Atom[oldCount + newAtoms.Count];
            Array.Copy(molecule.atom, newArray, oldCount);
            molecule.atom = newArray;

            int added = 0;
            foreach (var na in newAtoms)
            {
                int idx = molecule.NumberOfAtoms;

 // ═══════════════════════════════════════════════════════
 // Alokacja tablic for bonds kowalencyjnych
 // ═══════════════════════════════════════════════════════
                molecule.atom[idx].Distance = new float[Flags.Instance.MAX_BONDED_ATOMS];
                molecule.atom[idx].BondOccupancy = new float[Flags.Instance.MAX_BONDED_ATOMS];
                molecule.atom[idx].BondedAtom = new int[Flags.Instance.MAX_BONDED_ATOMS];
                molecule.atom[idx].AngleZ = new float[Flags.Instance.MAX_BONDED_ATOMS];
                molecule.atom[idx].AngleXY = new float[Flags.Instance.MAX_BONDED_ATOMS];
                molecule.atom[idx].NrBondedAtoms = 0;

 // ═══════════════════════════════════════════════════════
 // Alokacja tablic for H-bondów
 // ═══════════════════════════════════════════════════════
                molecule.atom[idx].H_Distance = new float[Flags.Instance.MAX_H_BONDED_ATOMS];
                molecule.atom[idx].H_AngleZ = new float[Flags.Instance.MAX_H_BONDED_ATOMS];
                molecule.atom[idx].H_AngleXY = new float[Flags.Instance.MAX_H_BONDED_ATOMS];
                molecule.atom[idx].H_BondedAtom = new int[Flags.Instance.MAX_H_BONDED_ATOMS];
                molecule.atom[idx].H_NrBondedAtoms = 0;

 // ═══════════════════════════════════════════════════════
 // Afterzycja kartezjańska (fractional + translation → Cartesian - shift)
 // ═══════════════════════════════════════════════════════
                Vector3 fracPos = new Vector3(na.x, na.y, na.z);
                molecule.atom[idx].Coordinate = Vector3.Transform(fracPos, U) - shift;

 // ═══════════════════════════════════════════════════════
 // Afterdstawowe afterla atom
 // ═══════════════════════════════════════════════════════
                molecule.atom[idx].AtomicNumber = na.AtomicNumber;
                molecule.atom[idx].name = na.name;
                molecule.atom[idx].style = style;
                molecule.atom[idx].occupancy = na.occupancy;
                molecule.atom[idx].show = true;
                molecule.atom[idx].selected = false;
                molecule.atom[idx].SelectionOrder = 0;
                molecule.atom[idx].Coordinate_error = new Vector3(na.x_e, na.y_e, na.z_e);

 // ═══════════════════════════════════════════════════════
 // Macierze ADP / transformacje
 // ═══════════════════════════════════════════════════════
                molecule.atom[idx].Ue = na.Ue;
                molecule.atom[idx].Un = na.Un;
                molecule.atom[idx].Transformation = na.Transformation;

 // ═══════════════════════════════════════════════════════
 // Metadane structurelne
 // ═══════════════════════════════════════════════════════
                molecule.atom[idx].disorder_group = na.disorder_group;
                molecule.atom[idx].charge = na.charge;
                molecule.atom[idx].Hydrophobicity = na.Hydrophobicity;
                molecule.atom[idx].isAnisotropic = na.isAnisotropic;
                molecule.atom[idx].SymmetryEquivalentNumberInCell = na.SymmetryEquivalentNumberInCell;
                molecule.atom[idx].IndividualMoleculeInCell = na.IndividualMoleculeInCell;

 // ═══════════════════════════════════════════════════════
 // Custom color (reset do defaultgo)
 // ═══════════════════════════════════════════════════════
                molecule.atom[idx].CustomColor = new Vector3(-1f, -1f, -1f);

 // ═══════════════════════════════════════════════════════
 // CellShift — kluczowe for filtrowania cross-cell w Calculate_Bonds3D
 // ═══════════════════════════════════════════════════════
                molecule.atom[idx].CellShiftX = na.CellShiftX;
                molecule.atom[idx].CellShiftY = na.CellShiftY;
                molecule.atom[idx].CellShiftZ = na.CellShiftZ;

 // ═══════════════════════════════════════════════════════
 // Reset Contact fields (new atom nie ma still contacts)
 // ═══════════════════════════════════════════════════════
                molecule.atom[idx].ContactCount = 0;
                molecule.atom[idx].ContactDistance = null;
                molecule.atom[idx].ContactAtom = null;
                molecule.atom[idx].ContactAngleZ = null;
                molecule.atom[idx].ContactAngleXY = null;
                molecule.atom[idx].PrecomputedContactMatrix = null;
                molecule.atom[idx].ContactIsNew = null;

 // ═══════════════════════════════════════════════════════
 // Reset Precomputed matrices
 // ═══════════════════════════════════════════════════════
                molecule.atom[idx].PrecomputedBondMatrix = null;
                molecule.atom[idx].PrecomputedHBondMatrix = null;

 // ═══════════════════════════════════════════════════════
 // Z-depth
 // ═══════════════════════════════════════════════════════
                molecule.atom[idx].Z_Depth = 0;

 // ═══════════════════════════════════════════════════════
 // ⭐ FLAGI: only Short Contacts, NIE H-bonds
 // ═══════════════════════════════════════════════════════
                molecule.atom[idx].UseForShortContacts = true;
                molecule.atom[idx].UseForHBonds = false;

                molecule.NumberOfAtoms++;
                added++;
            }

 // ⭐ ZAREJESTRUJ new atoms jako "expanded"
            if (added > 0)
            {
                RegisterExpandedAtoms(molecule, oldCount, added);
            }

            Debug.WriteLine($"[ShortContactsFinder] AddNearbyAtomsInternal: added {added} " +
                            $"(total: {molecule.NumberOfAtoms}, " +
                            $"expanded: {molecule.ExpandedAtomIndices.Count})");

            return added;
        }

 /// <summary>
 /// Finds atoms z symmetry around afterdanej positions.
 /// Used zarówno przez Short Contacts (z filtrem intra/inter)
 /// jak i przez Expand Contacts (without filtra - afterkaż all neighbours).
 /// </summary>
        public static List<Molecule.AtomSimple> FindNearbyAtomsFromSymmetry(
            Molecule molecule,
            Vector3 selectedPos,
            int selectedAtomIndex,
            bool filterIntraInter = true,
            bool skipCellZero = false)
        {
            var result = new List<Molecule.AtomSimple>();

            if (molecule == null || molecule.SymetryEquivalents == null ||
                molecule.SymetryEquivalents.Count == 0)
                return result;

            float minDistance = Flags.Instance.ShortContactsVdwMinus;
            bool useVdwSum = Flags.Instance.ShortContactsUseVdwSum;
            bool allowInter = Flags.Instance.ShortContactsIntermolecular;
            bool allowIntra = Flags.Instance.ShortContactsIntramolecular;

            Matrix U = Matrix.Transpose(molecule.FractionaltoCarttesianMatrix(0, 0, 0));
            Vector3 shift = molecule.MoleculeShift;

 // Dane atom referencyjnego
            float selectedVdw = 1.7f;
            int selectedMol = -1;

            if (selectedAtomIndex >= 0 && selectedAtomIndex < molecule.NumberOfAtoms)
            {
                int selectedAN = molecule.atom[selectedAtomIndex].AtomicNumber;
                selectedVdw = molecule.GetVdwRadius(selectedAN);
                selectedMol = molecule.atom[selectedAtomIndex].IndividualMoleculeInCell;
            }

            foreach (var srcAtom in molecule.SymetryEquivalents)
            {
                for (int tx = -1; tx <= 1; tx++)
                    for (int ty = -1; ty <= 1; ty++)
                        for (int tz = -1; tz <= 1; tz++)
                        {
                            if (tx == 0 && ty == 0 && tz == 0) continue;

 // optional afterminięcie cell matrixystej
                            if (skipCellZero && tx == 0 && ty == 0 && tz == 0) continue;

                            float translatedX = srcAtom.x + tx;
                            float translatedY = srcAtom.y + ty;
                            float translatedZ = srcAtom.z + tz;

                            Vector3 fracPos = new Vector3(translatedX, translatedY, translatedZ);
                            Vector3 cartPos = Vector3.Transform(fracPos, U) - shift;

                            float distance = Vector3.Distance(cartPos, selectedPos);

 // ═══════════════════════════════════════════
 // Filtr distance
 // ═══════════════════════════════════════════
                            bool isContact = false;
                            if (useVdwSum)
                            {
                                float vdwRadius2 = molecule.GetVdwRadius(srcAtom.AtomicNumber);
                                if (distance < selectedVdw + vdwRadius2 && distance > 0.1f)
                                    isContact = true;
                            }
                            else
                            {
                                if (distance < minDistance && distance > 0.1f)
                                    isContact = true;
                            }
                            if (!isContact) continue;

 // ═══════════════════════════════════════════
 // Opcjonalny filtr intra/inter
 // ═══════════════════════════════════════════
                            if (filterIntraInter)
                            {
                                bool sameMolecule = srcAtom.IndividualMoleculeInCell == selectedMol;
                                bool sameCell = tx == 0 && ty == 0 && tz == 0;
                                bool isIntramolecular = sameMolecule && sameCell;

                                if (isIntramolecular && !allowIntra) continue;
                                if (!isIntramolecular && !allowInter) continue;
                            }

 // ═══════════════════════════════════════════
 // Check czy nie ma already takiego atom AKTYWNEGO w atom[]
 // ⭐ Filtr after UseForShortContacts — atom z flagą false
 // nie blokuje dodania newgo
 // ═══════════════════════════════════════════
                            bool exists = false;
                            for (int i = 0; i < molecule.NumberOfAtoms; i++)
                            {
                                if (i == selectedAtomIndex) continue;
                                if (!molecule.atom[i].UseForShortContacts) continue;

                                if (Vector3.DistanceSquared(molecule.atom[i].Coordinate, cartPos) < 0.01f)
                                {
                                    exists = true;
                                    break;
                                }
                            }
                            if (exists) continue;

 // ═══════════════════════════════════════════
 // Nowy sąsiad — przygotuj AtomSimple
 // ═══════════════════════════════════════════
                            var newAtom = srcAtom;
                            newAtom.x = translatedX;
                            newAtom.y = translatedY;
                            newAtom.z = translatedZ;
                            newAtom.CellShiftX = tx;
                            newAtom.CellShiftY = ty;
                            newAtom.CellShiftZ = tz;

                            result.Add(newAtom);
                        }
            }

            return result;
        }

 /// <summary>
 /// Builds matrixe drawing contacts for afterjedynczego atom.
 /// Used wewnętrznie przez Calculate and przez MainForm
 /// w HandleExpandContactsClick (for afterjedynczego kliknięcia).
 /// </summary>
        public static void BuildContactMatrices(
            Molecule molecule,
            int atomIdx,
            int oldCount,
            Vector3 selectedPos)
        {
            int added = molecule.NumberOfAtoms - oldCount;
            if (added <= 0) return;

            molecule.atom[atomIdx].ContactDistance = new float[added];
            molecule.atom[atomIdx].ContactAtom = new int[added];
            molecule.atom[atomIdx].ContactAngleZ = new float[added];
            molecule.atom[atomIdx].ContactAngleXY = new float[added];
            molecule.atom[atomIdx].PrecomputedContactMatrix = new Matrix[added];
            molecule.atom[atomIdx].ContactIsNew = new bool[added];

            int contactCount = 0;
            for (int i = oldCount; i < molecule.NumberOfAtoms; i++)
            {
                Vector3 newPos = molecule.atom[i].Coordinate;
                float distance = Vector3.Distance(selectedPos, newPos);

                if (distance < 1e-4f) continue;

                molecule.atom[atomIdx].ContactDistance[contactCount] = distance;
                molecule.atom[atomIdx].ContactAtom[contactCount] = i;
                molecule.atom[atomIdx].ContactIsNew[contactCount] = true;

 // Kąt Z (odchylenie w pionie)
                molecule.atom[atomIdx].ContactAngleZ[contactCount] = (float)Math.Asin(
                    Math.Clamp((selectedPos.Z - newPos.Z) / distance, -1f, 1f));

 // Kąt XY (obrót w plane)
                if (Math.Abs(selectedPos.X - newPos.X) < 1e-6f)
                {
                    molecule.atom[atomIdx].ContactAngleXY[contactCount] = 0f;
                }
                else
                {
                    molecule.atom[atomIdx].ContactAngleXY[contactCount] =
                        -(float)(MathHelperTal.ToRadians(180) -
                                 Math.Atan2(selectedPos.Y - newPos.Y,
                                            selectedPos.X - newPos.X));
                }

 // ═══════════════════════════════════════════════════
 // Macierz drawing (scaling + rotation)
 // ═══════════════════════════════════════════════════
                float scaleX = 0.0127f * distance;
                float scaleY = Flags.Instance.ExpandContactsLineThickness;
                float scaleZ = Flags.Instance.ExpandContactsLineThickness;

                molecule.atom[atomIdx].PrecomputedContactMatrix[contactCount] =
                    Matrix.CreateScale(scaleX, scaleY, scaleZ) *
                    Matrix.CreateRotationY(molecule.atom[atomIdx].ContactAngleZ[contactCount]) *
                    Matrix.CreateRotationZ(molecule.atom[atomIdx].ContactAngleXY[contactCount]);

                contactCount++;
            }

            molecule.atom[atomIdx].ContactCount = contactCount;
        }

        #endregion

        #region Private Helpers

 /// <summary>
 /// Zeruje afterla Contact* na afterjedynczym atomie (przez ref — Atom to struct).
 /// </summary>
        private static void ClearContactDataOnAtom(ref Molecule.Atom atom)
        {
            atom.ContactCount = 0;
            atom.ContactDistance = null;
            atom.ContactAtom = null;
            atom.ContactAngleZ = null;
            atom.ContactAngleXY = null;
            atom.PrecomputedContactMatrix = null;
            atom.ContactIsNew = null;
        }

 /// <summary>
 /// Zaznacza all atoms if nic nie jest selected.
 /// </summary>
        private static void EnsureSelection(Molecule molecule)
        {
            bool hasSelection = false;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected) { hasSelection = true; break; }
            }

            if (!hasSelection)
            {
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                    molecule.atom[i].selected = true;
                molecule.NumberSelected = molecule.NumberOfAtoms;
                Debug.WriteLine($"[ShortContactsFinder] No selection - selected all " +
                                $"{molecule.NumberOfAtoms}");
            }
        }

 /// <summary>
 /// Upewnia że each atom ma IndividualMoleculeInCell >= 0.
 /// If nie — wykrywa molecule i przepisuje dane z SymetryEquivalents.
 /// </summary>
        private static void EnsureMoleculeDetection(Molecule molecule)
        {
            bool needsDetection = false;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].IndividualMoleculeInCell < 0)
                {
                    needsDetection = true;
                    break;
                }
            }

            if (!needsDetection) return;

            Debug.WriteLine("[ShortContactsFinder] Detecting molecules...");
            molecule.DetectMolecules(molecule.SymetryEquivalents, true);

 // Przepisz IndividualMoleculeInCell z SymetryEquivalents do atom[]
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                for (int j = 0; j < molecule.SymetryEquivalents.Count; j++)
                {
                    float dx = Math.Abs(molecule.atom[i].Coordinate.X -
                                        molecule.SymetryEquivalents[j].x);
                    float dy = Math.Abs(molecule.atom[i].Coordinate.Y -
                                        molecule.SymetryEquivalents[j].y);
                    float dz = Math.Abs(molecule.atom[i].Coordinate.Z -
                                        molecule.SymetryEquivalents[j].z);

                    if (dx < 0.01f && dy < 0.01f && dz < 0.01f)
                    {
                        molecule.atom[i].IndividualMoleculeInCell =
                            molecule.SymetryEquivalents[j].IndividualMoleculeInCell;
                        break;
                    }
                }
            }
        }

 /// <summary>
 /// Deduplikacja atoms after coordinates fractional.
 /// </summary>
        private static List<Molecule.AtomSimple> DeduplicateByPosition(
            List<Molecule.AtomSimple> atoms)
        {
            var unique = new Dictionary<string, Molecule.AtomSimple>();
            foreach (var a in atoms)
            {
                string key = $"{a.x:F6},{a.y:F6},{a.z:F6}";
                if (!unique.ContainsKey(key))
                    unique[key] = a;
            }
            return new List<Molecule.AtomSimple>(unique.Values);
        }

        #endregion
    }
}