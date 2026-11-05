// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule overlay module: OverlayManager.

﻿// OverlayManager.cs
using Microsoft.Xna.Framework;
using System.Linq;
using Tal.GUI;

namespace Tal.Overlay
{
    /// <summary>Orientation applied before the 3-point fit.</summary>
    public enum TransformationMode
    {
        None,
        /// <summary>Reflection in the XY plane (Z → −Z). Sufficient; inversion is not exposed in the UI.</summary>
        Mirror,
        RotationY,
        RotationZ,
        /// <summary>Point inversion (scale −1). Kept for compatibility; UI uses Mirror only.</summary>
        SymmetryCenter
    }

    /// <summary>
    /// Holds overlay molecules and fits them to the first (base) entry.
    /// Fit is a 3-point rotation (frame origin = first point), then a translation
    /// that places the centroid of the fitted points on the base centroid.
    /// Ring mode uses atoms 0,2,4 for the rotation and all ring atoms for the pivot.
    /// </summary>
    public class OverlayManager
    {
        private List<OverlayMoleculeData> _overlayMolecules = new List<OverlayMoleculeData>();
        private int _nextId = 1;
        private Random _random = new Random();


        public event EventHandler MoleculesChanged;

        public IReadOnlyList<OverlayMoleculeData> OverlayMolecules => _overlayMolecules.AsReadOnly();
        public int TotalAtomCount => _overlayMolecules.Sum(m => m.WorkingAtoms?.Count ?? 0);

 // ============================================================
 // ⭐ LOGOWANIE
 // ============================================================

        private void LogToDebug(string message, System.Drawing.Color? color = null)
        {
            try
            {
                var mainForm = System.Windows.Forms.Application.OpenForms.OfType<MainForm>().FirstOrDefault();
                if (mainForm != null)
                {
                    var debugWindow = System.Windows.Forms.Application.OpenForms.OfType<DebugWindow>().FirstOrDefault();
                    if (debugWindow != null)
                    {
                        if (color.HasValue)
                            debugWindow.Log(message, color.Value);
                        else
                            debugWindow.Log(message);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[OVERLAY] {message}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[OVERLAY] {message}");
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine($"[OVERLAY] {message}");
            }
        }

        private void LogToDebugHeader(string message, System.Drawing.Color? color = null)
        {
            LogToDebug($"\n{message}", color ?? System.Drawing.Color.Yellow);
        }

 // ============================================================
 // ⭐ METODY CENTROWANIA
 // ============================================================

 // OverlayManager.cs - CenterMolecule

 // OverlayManager.cs - CenterMolecule

        private void CenterMolecule(OverlayMoleculeData molData)
        {
            if (molData.WorkingAtoms == null || molData.WorkingAtoms.Count == 0) return;

 // ⭐ ZAPISZ KOLOR I STYL
            Vector3 currentColor = molData.Color;
            int currentStyle = molData.WorkingAtoms.Count > 0 ? molData.WorkingAtoms[0].style : 2;

            Vector3 center = Vector3.Zero;
            foreach (var atom in molData.WorkingAtoms)
            {
                center += atom.Coordinate;
            }
            center /= molData.WorkingAtoms.Count;

            LogToDebug($"  Centering molecule: {molData.Name} (center: {center.X:F2}, {center.Y:F2}, {center.Z:F2})", System.Drawing.Color.LightGray);

            for (int i = 0; i < molData.WorkingAtoms.Count; i++)
            {
                var atom = molData.WorkingAtoms[i];
                atom.Coordinate -= center;
                atom.CustomColor = currentColor;
                atom.style = currentStyle;
                molData.WorkingAtoms[i] = atom;
            }

            for (int i = 0; i < molData.OriginalAtoms.Count; i++)
            {
                var atom = molData.OriginalAtoms[i];
                atom.Coordinate -= center;
                atom.CustomColor = currentColor;
                atom.style = currentStyle;
                molData.OriginalAtoms[i] = atom;
            }

            if (molData.WorkingReferencePoints != null)
            {
                for (int i = 0; i < molData.WorkingReferencePoints.Length; i++)
                {
                    molData.WorkingReferencePoints[i] -= center;
                }
            }
            if (molData.OriginalReferencePoints != null)
            {
                for (int i = 0; i < molData.OriginalReferencePoints.Length; i++)
                {
                    molData.OriginalReferencePoints[i] -= center;
                }
            }
            if (molData.WorkingRingPoints != null)
                for (int i = 0; i < molData.WorkingRingPoints.Length; i++)
                    molData.WorkingRingPoints[i] -= center;
            if (molData.OriginalRingPoints != null)
                for (int i = 0; i < molData.OriginalRingPoints.Length; i++)
                    molData.OriginalRingPoints[i] -= center;

 // ⭐ PRZYWRÓĆ KOLOR
            molData.Color = currentColor;

            LogToDebug($"  ✅ Molecule centered", System.Drawing.Color.LightGreen);
        }
        private Vector3 GetMoleculeCenter(OverlayMoleculeData molecule)
        {
            if (molecule.WorkingAtoms == null || molecule.WorkingAtoms.Count == 0)
                return Vector3.Zero;

            Vector3 center = Vector3.Zero;
            foreach (var atom in molecule.WorkingAtoms)
            {
                center += atom.Coordinate;
            }
            return center / molecule.WorkingAtoms.Count;
        }

        private Vector3 GetMoleculeCenterFromPoints(Vector3[] points)
        {
            if (points == null || points.Length == 0) return Vector3.Zero;
            return (points[0] + points[1] + points[2]) / 3;
        }

 // ============================================================
 // ⭐ DODAWANIE MOLEKUŁ
 // ============================================================

 // OverlayManager.cs - Fixed method AddSelectedMolecule


        /// <summary>
        /// Add the molecule that contains the single selected atom.
        /// Detects its 5- or 6-membered ring, numbers the ring atoms, stores atoms
        /// 0, 2 and 4 as the fit points, and fits immediately so Show displays the overlay.
        /// The pivot is the centroid of all 5 or 6 ring atoms after the rotation.
        /// </summary>

        /// <summary>Index of an atom in OriginalAtoms by name, or −1.</summary>
        private static int IndexByName(OverlayMoleculeData mol, string name)
        {
            if (mol?.OriginalAtoms == null || string.IsNullOrEmpty(name)) return -1;
            for (int i = 0; i < mol.OriginalAtoms.Count; i++)
                if (mol.OriginalAtoms[i].name == name) return i;
            return -1;
        }

        public bool AddMoleculeFromRing(MonoGameDocument sourceDocument, TransformationMode transformationMode)
        {
            if (sourceDocument == null) return false;
            var molecule = sourceDocument.GetMolecule();
            if (molecule?.atom == null) return false;

            int seed = -1;
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected) { seed = i; break; }
            }
            if (seed < 0) return false;

            var ring = FindRingContaining(molecule, seed);
            if (ring == null) return false;

            // Number and select the ring in order
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                molecule.atom[i].selected = false;
                molecule.atom[i].SelectionOrder = 0;
            }
            for (int k = 0; k < ring.Count; k++)
            {
                molecule.atom[ring[k]].selected = true;
                molecule.atom[ring[k]].SelectionOrder = k + 1;
            }
            molecule.NumberSelected = ring.Count;

            // Reuse molecule-add path by temporarily keeping only ring selected,
            // then overwrite reference points with 0,2,4 and store full ring.
            AddSelectedMolecule(sourceDocument, transformationMode);
            var added = _overlayMolecules.LastOrDefault();
            if (added == null) return false;

            // OriginalAtoms are already centered. Take ring atoms 0,2,4 from them
            // (match by source name) and fit immediately — do not leave the
            // first-3-selected fit, and do not reset without a new fit.
            string n0 = molecule.atom[ring[0]].name;
            string n2 = molecule.atom[ring[2]].name;
            string n4 = molecule.atom[ring[4]].name;
            int j0 = IndexByName(added, n0);
            int j2 = IndexByName(added, n2);
            int j4 = IndexByName(added, n4);
            if (j0 < 0 || j2 < 0 || j4 < 0) return false;

            added.OriginalReferencePoints = new[]
            {
                added.OriginalAtoms[j0].Coordinate,
                added.OriginalAtoms[j2].Coordinate,
                added.OriginalAtoms[j4].Coordinate
            };
            added.ReferencePointNames = new[] { n0 ?? "0", n2 ?? "2", n4 ?? "4" };
            added.ReferencePointIndices = new[] { j0, j2, j4 };

            var ringPts = new List<Vector3>();
            foreach (int src in ring)
            {
                int j = IndexByName(added, molecule.atom[src].name);
                if (j >= 0) ringPts.Add(added.OriginalAtoms[j].Coordinate);
            }
            added.OriginalRingPoints = ringPts.ToArray();

            added.InitializeWorkingData();
            if (_overlayMolecules.Count > 1)
                AutoFitMolecule(added);

            LogToDebug($"  Ring {ring.Count}: fitted 0,2,4 immediately", System.Drawing.Color.LightGreen);
            return true;
        }

        private List<int> FindRingContaining(Molecule molecule, int seed)
        {
            int n = molecule.NumberOfAtoms;
            var nbr = new List<int>[n];
            for (int i = 0; i < n; i++) nbr[i] = new List<int>();
            for (int i = 0; i < n; i++)
            {
                int zi = molecule.atom[i].AtomicNumber;
                if (zi != 6 && zi != 7 && zi != 8 && zi != 16) continue;
                for (int j = i + 1; j < n; j++)
                {
                    int zj = molecule.atom[j].AtomicNumber;
                    if (zj != 6 && zj != 7 && zj != 8 && zj != 16) continue;
                    float d = Vector3.Distance(molecule.atom[i].Coordinate, molecule.atom[j].Coordinate);
                    if (d > 1.2f && d < 1.65f)
                    {
                        nbr[i].Add(j);
                        nbr[j].Add(i);
                    }
                }
            }

            foreach (int size in new[] { 6, 5 })
            {
                var path = new List<int> { seed };
                var set = new HashSet<int> { seed };
                var seen = new HashSet<string>();
                var found = DfsRing(seed, seed, 1, size, path, set, nbr, seen);
                if (found != null) return found;
            }
            return null;
        }

        private List<int> DfsRing(int start, int cur, int depth, int size,
            List<int> path, HashSet<int> set, List<int>[] nbr, HashSet<string> seen)
        {
            if (depth > size) return null;
            foreach (int next in nbr[cur])
            {
                if (next == start && depth == size)
                    return new List<int>(path);
                if (set.Contains(next)) continue;
                path.Add(next);
                set.Add(next);
                var r = DfsRing(start, next, depth + 1, size, path, set, nbr, seen);
                if (r != null) return r;
                path.RemoveAt(path.Count - 1);
                set.Remove(next);
            }
            return null;
        }

        public void AddSelectedMolecule(MonoGameDocument sourceDocument, TransformationMode transformationMode)
        {
            LogToDebugHeader("═══════════════════════════════════════════════════", System.Drawing.Color.Cyan);
            LogToDebugHeader("🧪 ADD SELECTED MOLECULE (WHOLE MOLECULES AS ONE RECORD)", System.Drawing.Color.Cyan);
            LogToDebugHeader("═══════════════════════════════════════════════════", System.Drawing.Color.Cyan);

            if (sourceDocument == null)
            {
                LogToDebug("❌ Source document is null", System.Drawing.Color.Red);
                return;
            }

            var molecule = sourceDocument.GetMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                LogToDebug("❌ No molecule loaded in source document", System.Drawing.Color.Red);
                return;
            }

            LogToDebug($"📄 Source document: {sourceDocument.Text}", System.Drawing.Color.LightGray);
            LogToDebug($"📦 Molecule: {molecule.NumberOfAtoms} atoms", System.Drawing.Color.LightGray);

 // ⭐ ZBIERZ NUMERY MOLEKUŁ Z ZAZNACZONYCH ATOMÓW
            var selectedMoleculeNumbers = new HashSet<int>();
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected)
                {
                    int molNum = molecule.atom[i].IndividualMoleculeInCell;
                    if (molNum >= 0)
                    {
                        selectedMoleculeNumbers.Add(molNum);
                        LogToDebug($"  Selected atom {i}: {molecule.atom[i].name} (mol: {molNum})", System.Drawing.Color.LightBlue);
                    }
                    else
                    {
 // ⭐ ATOMY BEZ NUMERU MOLEKUŁY - traktujemy jako osobne
                        selectedMoleculeNumbers.Add(-i - 1);
                        LogToDebug($"  Selected atom {i}: {molecule.atom[i].name} (no molecule number)", System.Drawing.Color.LightBlue);
                    }
                }
            }

            if (selectedMoleculeNumbers.Count == 0)
            {
                LogToDebug("⚠️ No atoms selected!", System.Drawing.Color.Orange);
                return;
            }

            LogToDebug($"🎯 Found {selectedMoleculeNumbers.Count} unique molecules to add", System.Drawing.Color.LightGray);

 // ⭐⭐⭐ ZAWSZE DODAJEMY WSZYSTKIE MOLEKUŁY JAKO JEDEN REKORD ⭐⭐⭐

            var molData = new OverlayMoleculeData
            {
                Id = _nextId++,
                Name = $"Molecule {_nextId - 1}",
                SourceName = sourceDocument.Text,
                SourceMoleculeNumber = -1, // -1 means this is a mix of molecules
                SourceSymmetryIndex = 0,
                CurrentTransformation = transformationMode,
                Color = GetRandomColor(),
                IsStructure = false,
                IsSelection = false
            };

 // ⭐ ZBIERZ WSZYSTKIE ATOMY Z WSZYSTKICH ZAZNACZONYCH MOLEKUŁ
            var refPoints = new List<Vector3>();
            var refNames = new List<string>();
            var refIndices = new List<int>();
            int totalAtomsAdded = 0;

            foreach (int molNum in selectedMoleculeNumbers)
            {
 // ⭐ DODAJ WSZYSTKIE ATOMY Z TEJ MOLEKUŁY
                for (int i = 0; i < molecule.NumberOfAtoms; i++)
                {
                    int atomMolNum = molecule.atom[i].IndividualMoleculeInCell;
                    bool shouldAdd = false;

                    if (molNum >= 0 && atomMolNum == molNum)
                    {
                        shouldAdd = true;
                    }
                    else if (molNum < 0 && atomMolNum < 0 && molNum == atomMolNum)
                    {
                        shouldAdd = true;
                    }

                    if (shouldAdd)
                    {
                        var atom = CopyAtom(molecule.atom[i]);
                        atom.CustomColor = molData.Color;
                        molData.OriginalAtoms.Add(atom);
                        totalAtomsAdded++;

 // ⭐ ZBIERZ PUNKTY REFERENCYJNE (TYLKO Z ZAZNACZONYCH ATOMÓW)
                        if (molecule.atom[i].selected && refPoints.Count < 3)
                        {
                            refPoints.Add(molecule.atom[i].Coordinate);
                            refNames.Add(molecule.atom[i].name);
                            refIndices.Add(i);
                            LogToDebug($"  Reference point {refPoints.Count}: {molecule.atom[i].name} from mol {molNum}", System.Drawing.Color.LightBlue);
                        }
                    }
                }
            }

            LogToDebug($"  Added {totalAtomsAdded} atoms from {selectedMoleculeNumbers.Count} molecules", System.Drawing.Color.LightGray);

 // ⭐ ZAPISZ ORYGINALNE PUNKTY REFERENCYJNE
            if (refPoints.Count == 3)
            {
                molData.OriginalReferencePoints = refPoints.ToArray();
                molData.ReferencePointNames = refNames.ToArray();
                molData.ReferencePointIndices = refIndices.ToArray();
                LogToDebug($"✅ Stored 3 reference points for {molData.Name}", System.Drawing.Color.Green);
            }
            else
            {
                LogToDebug($"⚠️ Only {refPoints.Count} reference points for {molData.Name} (need 3 for fitting)", System.Drawing.Color.Orange);
            }

 // ⭐ INICJALIZUJ DANE ROBOCZE (KOPIA ORYGINAŁÓW)
            molData.InitializeWorkingData();
            LogToDebug($"  Initialized working data with {molData.WorkingAtoms.Count} atoms", System.Drawing.Color.LightGray);

 // ⭐ CENTRUJ
            CenterMolecule(molData);
            LogToDebug($"  ✅ Molecule centered", System.Drawing.Color.Green);

 // ⭐ ZASTOSUJ TRANSFORMACJĘ NA ROBOCZYCH DANYCH
            ApplyTransformationToMolecule(molData);

            _overlayMolecules.Add(molData);
            LogToDebug($"  ✅ {molData.Name} added to overlay", System.Drawing.Color.Green);

 // ⭐⭐⭐ AUTOMATYCZNIE DOPASUJ IF TO NIE PIERWSZA MOLEKUŁA ⭐⭐⭐
            if (_overlayMolecules.Count > 1)
            {
                LogToDebug($"  🔧 Auto-fitting {molData.Name} to base molecule", System.Drawing.Color.Yellow);
                AutoFitMolecule(molData);
            }

            LogToDebug($"✅ Added {totalAtomsAdded} atoms from {selectedMoleculeNumbers.Count} molecules as single overlay entry", System.Drawing.Color.Green);
            LogToDebugHeader("═══════════════════════════════════════════════════", System.Drawing.Color.Cyan);

            OnMoleculesChanged();
        }

 // ============================================================
 // Automatic fit to the base molecule
 // ============================================================

        private void AutoFitMolecule(OverlayMoleculeData molecule)
        {
            var baseMolecule = _overlayMolecules.FirstOrDefault();
            if (baseMolecule == null || baseMolecule.Id == molecule.Id) return;

            if (!molecule.HasReferencePoints || !baseMolecule.HasReferencePoints)
            {
                LogToDebug($"  ⚠️ Cannot auto-fit - missing reference points", System.Drawing.Color.Orange);
                return;
            }

            LogToDebug($"  📌 Fitting {molecule.Name} to base {baseMolecule.Name}", System.Drawing.Color.LightGray);

            try
            {
                FitMoleculeToPoints(
                    molecule,
                    molecule.WorkingReferencePoints,
                    baseMolecule.WorkingReferencePoints
                );
                LogToDebug($"  ✅ Auto-fit complete", System.Drawing.Color.Green);
            }
            catch (Exception ex)
            {
                LogToDebug($"  ❌ Auto-fit error: {ex.Message}", System.Drawing.Color.Red);
            }
        }

 // ============================================================
 // ⭐ TRANSFORMACJE
 // ============================================================

 // OverlayManager.cs - SetTransformationForMolecule

        public void SetTransformationForMolecule(int moleculeId, TransformationMode newMode)
        {
            var molData = _overlayMolecules.FirstOrDefault(m => m.Id == moleculeId);
            if (molData == null)
            {
                LogToDebug($"❌ Molecule with ID {moleculeId} not found", System.Drawing.Color.Red);
                return;
            }

            LogToDebugHeader($"🔄 TRANSFORMATION: {molData.CurrentTransformation} → {newMode}", System.Drawing.Color.Cyan);
            LogToDebug($"📦 Molecule: {molData.Name}", System.Drawing.Color.LightGray);

 // ⭐⭐ DLA MIRROR - UŻYJ OSOBNEJ METODY ⭐⭐
            if (newMode == TransformationMode.Mirror)
            {
                ApplyMirrorToMolecule(moleculeId);
                return;
            }

 // ⭐ ZAPISZ KOLOR I STYL
            Vector3 currentColor = molData.Color;
            int currentStyle = molData.WorkingAtoms.Count > 0 ? molData.WorkingAtoms[0].style : 2;

            molData.CurrentTransformation = newMode;

            molData.ResetToOriginal();

 // ⭐ PRZYWRÓĆ KOLOR I STYL
            molData.Color = currentColor;
            for (int i = 0; i < molData.WorkingAtoms.Count; i++)
            {
                var atom = molData.WorkingAtoms[i];
                atom.CustomColor = currentColor;
                atom.style = currentStyle;
                molData.WorkingAtoms[i] = atom;
            }

            ApplyTransformationToMolecule(molData);

            LogToDebug($"✅ Transformation applied: {newMode}", System.Drawing.Color.Green);

            OnMoleculesChanged();
        }
 /// <summary>
 /// Afterbiera styl z pierwszeo atom w molekule.
 /// </summary>
        private int GetMoleculeStyle(OverlayMoleculeData molData)
        {
            if (molData.WorkingAtoms == null || molData.WorkingAtoms.Count == 0)
                return 2; // Domyślnie Balls & Sticks

            return molData.WorkingAtoms[0].style;
        }
 // OverlayManager.cs - ApplyTransformationToMolecule

        private void ApplyTransformationToMolecule(OverlayMoleculeData molData)
        {
            if (molData.WorkingAtoms == null || molData.WorkingAtoms.Count == 0) return;

            Matrix transform = GetTransformationMatrix(molData.CurrentTransformation);

            LogToDebug($"  Transform matrix:", System.Drawing.Color.LightGray);
            LogToDebug($"    [{transform.M11:F2}, {transform.M12:F2}, {transform.M13:F2}]", System.Drawing.Color.LightGray);
            LogToDebug($"    [{transform.M21:F2}, {transform.M22:F2}, {transform.M23:F2}]", System.Drawing.Color.LightGray);
            LogToDebug($"    [{transform.M31:F2}, {transform.M32:F2}, {transform.M33:F2}]", System.Drawing.Color.LightGray);

 // ⭐ ZAPISZ AKTUALNY KOLOR I STYL
            Vector3 currentColor = molData.Color;
            int currentStyle = molData.WorkingAtoms.Count > 0 ? molData.WorkingAtoms[0].style : 2;

            for (int i = 0; i < molData.WorkingAtoms.Count; i++)
            {
                var atom = molData.WorkingAtoms[i];
                atom.Coordinate = Vector3.Transform(atom.Coordinate, transform);

                if (atom.Un != Matrix.Identity)
                    atom.Un = TransformMatrix(atom.Un, transform);
                if (atom.Ue != Matrix.Identity)
                    atom.Ue = TransformMatrix(atom.Ue, transform);
                if (atom.Transformation != Matrix.Identity)
                    atom.Transformation = TransformMatrix(atom.Transformation, transform);

 // ⭐ ZACHOWAJ KOLOR I STYL
                atom.CustomColor = currentColor;
                atom.style = currentStyle;

                molData.WorkingAtoms[i] = atom;
            }

 // ⭐ TRANSFORMUJ PUNKTY REFERENCYJNE
            if (molData.WorkingReferencePoints != null)
            {
                for (int i = 0; i < molData.WorkingReferencePoints.Length; i++)
                {
                    molData.WorkingReferencePoints[i] = Vector3.Transform(molData.WorkingReferencePoints[i], transform);
                }
                LogToDebug($"  Updated reference points after transformation", System.Drawing.Color.LightGray);
            }
            if (molData.WorkingRingPoints != null)
            {
                for (int i = 0; i < molData.WorkingRingPoints.Length; i++)
                    molData.WorkingRingPoints[i] = Vector3.Transform(molData.WorkingRingPoints[i], transform);
            }

 // ⭐ ZACHOWAJ KOLOR W MOLEKULE
            molData.Color = currentColor;

            RecalculateBondsForMolecule(molData);
        }

        private Matrix TransformMatrix(Matrix matrix, Matrix transform)
        {
            return transform * matrix * Matrix.Transpose(transform);
        }

        private Matrix GetTransformationMatrix(TransformationMode mode)
        {
            switch (mode)
            {
                case TransformationMode.Mirror:
                    return Matrix.CreateRotationX(MathHelper.Pi);
                case TransformationMode.RotationY:
                    return Matrix.CreateRotationY(MathHelper.Pi);
                case TransformationMode.RotationZ:
                    return Matrix.CreateRotationZ(MathHelper.Pi);
                case TransformationMode.SymmetryCenter:
                    return Matrix.CreateScale(-1f);
                case TransformationMode.None:
                default:
                    return Matrix.Identity;
            }
        }

 // ============================================================
 // 3-point fit, then centroid shift
 // ============================================================

 // OverlayManager.cs - FitMoleculeTopoints (z iteraccavitiesi)


        /// <summary>
        /// Set 3 reference points from a detected 5- or 6-membered ring
        /// (atoms taken in ring order, evenly spaced).
        /// </summary>
        public void FitMoleculeToPoints(
            OverlayMoleculeData sourceMolecule,
            Vector3[] sourcePoints,
            Vector3[] targetPoints)
        {
 // ⭐ UŻYJ ITERACYJNEGO DOPASOWANIA (5 ITERACJI)
            IterativeFit(sourceMolecule, sourcePoints, targetPoints);
        }

 // ============================================================
 // ⭐ ROTACJA 3-PUNKTOWA
 // ============================================================

        private Quaternion FindBestRotation3Points(Vector3[] source, Vector3[] target)
        {
            Vector3 v1 = source[1] - source[0];
            Vector3 v2 = target[1] - target[0];
            v1.Normalize();
            v2.Normalize();

            Vector3 w1 = source[2] - source[0];
            Vector3 w2 = target[2] - target[0];

            w1 = w1 - Vector3.Dot(w1, v1) * v1;
            w2 = w2 - Vector3.Dot(w2, v2) * v2;
            w1.Normalize();
            w2.Normalize();

            Vector3 u1 = Vector3.Cross(v1, w1);
            Vector3 u2 = Vector3.Cross(v2, w2);
            u1.Normalize();
            u2.Normalize();

            Matrix R1 = new Matrix();
            R1.M11 = v1.X; R1.M12 = w1.X; R1.M13 = u1.X; R1.M14 = 0;
            R1.M21 = v1.Y; R1.M22 = w1.Y; R1.M23 = u1.Y; R1.M24 = 0;
            R1.M31 = v1.Z; R1.M32 = w1.Z; R1.M33 = u1.Z; R1.M34 = 0;
            R1.M44 = 1;

            Matrix R2 = new Matrix();
            R2.M11 = v2.X; R2.M12 = w2.X; R2.M13 = u2.X; R2.M14 = 0;
            R2.M21 = v2.Y; R2.M22 = w2.Y; R2.M23 = u2.Y; R2.M24 = 0;
            R2.M31 = v2.Z; R2.M32 = w2.Z; R2.M33 = u2.Z; R2.M34 = 0;
            R2.M44 = 1;

            Matrix R = R2 * Matrix.Transpose(R1);

            LogToDebugHeader("🔄 ROTATION MATRIX R:", System.Drawing.Color.Yellow);
            LogToDebug($"  [{R.M11:F4}, {R.M12:F4}, {R.M13:F4}]", System.Drawing.Color.LightGray);
            LogToDebug($"  [{R.M21:F4}, {R.M22:F4}, {R.M23:F4}]", System.Drawing.Color.LightGray);
            LogToDebug($"  [{R.M31:F4}, {R.M32:F4}, {R.M33:F4}]", System.Drawing.Color.LightGray);

            return MatrixToQuaternion(R);
        }

        private Quaternion MatrixToQuaternion(Matrix R)
        {
            float trace = R.M11 + R.M22 + R.M33;
            Quaternion q;

            if (trace > 0)
            {
                float s = 0.5f / (float)Math.Sqrt(trace + 1.0f);
                q = new Quaternion(
                    (R.M23 - R.M32) * s,
                    (R.M31 - R.M13) * s,
                    (R.M12 - R.M21) * s,
                    0.25f / s
                );
            }
            else
            {
                if (R.M11 > R.M22 && R.M11 > R.M33)
                {
                    float s = 2.0f * (float)Math.Sqrt(1.0f + R.M11 - R.M22 - R.M33);
                    q = new Quaternion(
                        0.25f * s,
                        (R.M12 + R.M21) / s,
                        (R.M13 + R.M31) / s,
                        (R.M23 - R.M32) / s
                    );
                }
                else if (R.M22 > R.M33)
                {
                    float s = 2.0f * (float)Math.Sqrt(1.0f + R.M22 - R.M11 - R.M33);
                    q = new Quaternion(
                        (R.M12 + R.M21) / s,
                        0.25f * s,
                        (R.M23 + R.M32) / s,
                        (R.M31 - R.M13) / s
                    );
                }
                else
                {
                    float s = 2.0f * (float)Math.Sqrt(1.0f + R.M33 - R.M11 - R.M22);
                    q = new Quaternion(
                        (R.M13 + R.M31) / s,
                        (R.M23 + R.M32) / s,
                        0.25f * s,
                        (R.M12 - R.M21) / s
                    );
                }
            }

            float norm = q.Length();
            if (norm > 0.0001f)
            {
                q.X /= norm;
                q.Y /= norm;
                q.Z /= norm;
                q.W /= norm;
            }

            return q;
        }

 // ============================================================
 // ⭐ PRZELICZANIE WIĄZAŃ
 // ============================================================

        private void RecalculateBondsForMolecule(OverlayMoleculeData molData)
        {
            if (molData.WorkingAtoms == null || molData.WorkingAtoms.Count < 2) return;

            for (int i = 0; i < molData.WorkingAtoms.Count; i++)
            {
                var atomI = molData.WorkingAtoms[i];

                atomI.NrBondedAtoms = 0;

                if (atomI.Distance == null || atomI.Distance.Length < molData.WorkingAtoms.Count)
                {
                    int size = Math.Max(molData.WorkingAtoms.Count, 10);
                    atomI.Distance = new float[size];
                    atomI.BondedAtom = new int[size];
                    atomI.AngleZ = new float[size];
                    atomI.AngleXY = new float[size];
                    atomI.PrecomputedBondMatrix = new Matrix[size];
                    atomI.BondOccupancy = new float[size];
                }

                for (int j = 0; j < molData.WorkingAtoms.Count; j++)
                {
                    if (i == j) continue;

                    var atomJ = molData.WorkingAtoms[j];

                    float distance = Vector3.Distance(atomI.Coordinate, atomJ.Coordinate);

                    float bond_corr = Flags.Instance.BOND_FACTOR;
                    if (IsMetal(atomI.AtomicNumber) || IsMetal(atomJ.AtomicNumber))
                    {
                        bond_corr = Flags.Instance.BOND_FACTOR_METALS;
                    }

                    float maxDistance = (GetVdwRadius(atomI.AtomicNumber) + GetVdwRadius(atomJ.AtomicNumber)) * bond_corr;

                    if (distance < Flags.Instance.MAX_ATOM_DISTANCE && distance < maxDistance && distance > 0.01f)
                    {
                        int bondIndex = atomI.NrBondedAtoms;

                        if (atomI.Distance == null || atomI.Distance.Length <= bondIndex)
                        {
                            int newSize = Math.Max(bondIndex + 1, 10);
                            Array.Resize(ref atomI.Distance, newSize);
                            Array.Resize(ref atomI.BondedAtom, newSize);
                            Array.Resize(ref atomI.AngleZ, newSize);
                            Array.Resize(ref atomI.AngleXY, newSize);
                            Array.Resize(ref atomI.PrecomputedBondMatrix, newSize);
                            Array.Resize(ref atomI.BondOccupancy, newSize);
                        }

                        atomI.Distance[bondIndex] = distance;
                        atomI.BondedAtom[bondIndex] = j;
                        atomI.BondOccupancy[bondIndex] = Math.Min(atomI.occupancy, atomJ.occupancy);

                        float deltaZ = atomI.Coordinate.Z - atomJ.Coordinate.Z;
                        float deltaY = atomI.Coordinate.Y - atomJ.Coordinate.Y;
                        float deltaX = atomI.Coordinate.X - atomJ.Coordinate.X;

                        atomI.AngleZ[bondIndex] = (float)Math.Asin(Math.Clamp(deltaZ / distance, -1.0f, 1.0f));

                        if (Math.Abs(deltaX) < 0.0001f)
                        {
                            atomI.AngleXY[bondIndex] = 0;
                        }
                        else
                        {
                            atomI.AngleXY[bondIndex] = -(float)(MathHelperTal.ToRadians(180) -
                                Math.Atan2(deltaY, deltaX));
                        }

                        float scaleX = 0.0127f * distance;
                        float scaleY = 0.1f;
                        float scaleZ = 0.1f;

                        atomI.PrecomputedBondMatrix[bondIndex] =
                            Matrix.CreateScale(scaleX, scaleY, scaleZ) *
                            Matrix.CreateRotationY(atomI.AngleZ[bondIndex]) *
                            Matrix.CreateRotationZ(atomI.AngleXY[bondIndex]);

                        atomI.NrBondedAtoms++;
                    }
                }

                molData.WorkingAtoms[i] = atomI;
            }
        }

        private bool IsMetal(int atomicNumber)
        {
            return (atomicNumber >= 3 && atomicNumber <= 4) ||
                   (atomicNumber >= 11 && atomicNumber <= 12) ||
                   (atomicNumber >= 19 && atomicNumber <= 31) ||
                   (atomicNumber >= 37 && atomicNumber <= 50) ||
                   (atomicNumber >= 55 && atomicNumber <= 83) ||
                   (atomicNumber >= 87 && atomicNumber <= 112);
        }

        private float GetVdwRadius(int atomicNumber)
        {
            return VdwParameters.GetRadius(atomicNumber);
        }

 // ============================================================
 // ⭐ METODY POMOCNICZE
 // ============================================================

        private Molecule.Atom CopyAtom(Molecule.Atom source)
        {
            var copy = source;

            if (source.Distance != null)
            {
                copy.Distance = (float[])source.Distance.Clone();
                copy.BondOccupancy = (float[])source.BondOccupancy.Clone();
                copy.BondedAtom = (int[])source.BondedAtom.Clone();
                copy.AngleZ = (float[])source.AngleZ.Clone();
                copy.AngleXY = (float[])source.AngleXY.Clone();
                copy.PrecomputedBondMatrix = (Matrix[])source.PrecomputedBondMatrix?.Clone();
            }

            if (source.H_Distance != null)
            {
                copy.H_Distance = (float[])source.H_Distance.Clone();
                copy.H_AngleZ = (float[])source.H_AngleZ.Clone();
                copy.H_AngleXY = (float[])source.H_AngleXY.Clone();
                copy.H_BondedAtom = (int[])source.H_BondedAtom.Clone();
                copy.PrecomputedHBondMatrix = (Matrix[])source.PrecomputedHBondMatrix?.Clone();
            }

            if (source.ContactDistance != null)
            {
                copy.ContactDistance = (float[])source.ContactDistance.Clone();
                copy.ContactAtom = (int[])source.ContactAtom.Clone();
                copy.ContactAngleZ = (float[])source.ContactAngleZ.Clone();
                copy.ContactAngleXY = (float[])source.ContactAngleXY.Clone();
                copy.PrecomputedContactMatrix = (Matrix[])source.PrecomputedContactMatrix?.Clone();
                copy.ContactIsNew = (bool[])source.ContactIsNew?.Clone();
            }

            copy.style = source.style;
            return copy;
        }

        private Vector3 GetRandomColor()
        {
            float hue = (float)_random.NextDouble();
            return new Vector3(
                0.5f + 0.5f * (float)Math.Sin(hue * 2 * Math.PI),
                0.5f + 0.5f * (float)Math.Sin((hue + 0.33f) * 2 * Math.PI),
                0.5f + 0.5f * (float)Math.Sin((hue + 0.67f) * 2 * Math.PI)
            );
        }

 // ============================================================
 // ⭐ ZARZĄDZANIE LISTĄ
 // ============================================================

        public void ClearOverlayMolecules()
        {
            LogToDebug("🗑️ Clearing all overlay molecules", System.Drawing.Color.Orange);
            _overlayMolecules.Clear();
            OnMoleculesChanged();
        }

        public bool RemoveMolecule(int id)
        {
            var mol = _overlayMolecules.FirstOrDefault(m => m.Id == id);
            if (mol != null)
            {
                LogToDebug($"❌ Removing molecule: {mol.Name}", System.Drawing.Color.Orange);
                _overlayMolecules.Remove(mol);
                OnMoleculesChanged();
                return true;
            }
            return false;
        }

        public void ToggleVisibility(int id)
        {
            var mol = _overlayMolecules.FirstOrDefault(m => m.Id == id);
            if (mol != null)
            {
                mol.IsVisible = !mol.IsVisible;
                LogToDebug($"👁️ Toggle visibility for {mol.Name}: {mol.IsVisible}", System.Drawing.Color.LightGray);
                OnMoleculesChanged();
            }
        }

 // ============================================================
 // ⭐ KOLORY I STYLE
 // ============================================================

        public void SetMoleculeColor(int moleculeId, Vector3 color)
        {
            var molData = _overlayMolecules.FirstOrDefault(m => m.Id == moleculeId);
            if (molData == null) return;

            LogToDebug($"🎨 Setting color for {molData.Name}: ({color.X:F2}, {color.Y:F2}, {color.Z:F2})", System.Drawing.Color.LightGray);

            molData.Color = color;

            for (int i = 0; i < molData.WorkingAtoms.Count; i++)
            {
                var atom = molData.WorkingAtoms[i];
                atom.CustomColor = color;
                molData.WorkingAtoms[i] = atom;
            }

            OnMoleculesChanged();
        }

        public void SetMoleculeStyle(int moleculeId, int styleIndex)
        {
            var molData = _overlayMolecules.FirstOrDefault(m => m.Id == moleculeId);
            if (molData == null) return;

            LogToDebug($"🎨 Setting style for {molData.Name}: {styleIndex}", System.Drawing.Color.LightGray);

            for (int i = 0; i < molData.WorkingAtoms.Count; i++)
            {
                var atom = molData.WorkingAtoms[i];
                atom.style = styleIndex;
                molData.WorkingAtoms[i] = atom;
            }
            OnMoleculesChanged();
        }


 // OverlayManager.cs - AddSelectedAtoms

 /// <summary>
 /// Adds atoms based on selected atoms from the active document.
 /// Zaznaczone atoms są always dodawane jako JEDEN obiekt.
 /// </summary>
        public void AddSelectedAtoms(MonoGameDocument sourceDocument, TransformationMode transformationMode)
        {
            LogToDebugHeader("═══════════════════════════════════════════════════", System.Drawing.Color.Cyan);
            LogToDebugHeader("✏️ ADD SELECTED ATOMS", System.Drawing.Color.Cyan);
            LogToDebugHeader("═══════════════════════════════════════════════════", System.Drawing.Color.Cyan);

            if (sourceDocument == null)
            {
                LogToDebug("❌ Source document is null", System.Drawing.Color.Red);
                return;
            }

            var molecule = sourceDocument.GetMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                LogToDebug("❌ No molecule loaded in source document", System.Drawing.Color.Red);
                return;
            }

            LogToDebug($"📄 Source document: {sourceDocument.Text}", System.Drawing.Color.LightGray);
            LogToDebug($"📦 Molecule: {molecule.NumberOfAtoms} atoms", System.Drawing.Color.LightGray);

 // ⭐ ZBIERZ ZAZNACZONE ATOMY
            var selectedIndices = new List<int>();
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].selected)
                {
                    selectedIndices.Add(i);
                    LogToDebug($"  Selected atom {i}: {molecule.atom[i].name} at ({molecule.atom[i].Coordinate.X:F3}, {molecule.atom[i].Coordinate.Y:F3}, {molecule.atom[i].Coordinate.Z:F3})", System.Drawing.Color.LightBlue);
                }
            }

            if (selectedIndices.Count == 0)
            {
                LogToDebug("⚠️ No atoms selected!", System.Drawing.Color.Orange);
                return;
            }

            LogToDebug($"🎯 Found {selectedIndices.Count} selected atoms", System.Drawing.Color.LightGray);

 // ⭐ ZAWSZE DODAJEMY WSZYSTKIE ZAZNACZONE ATOMY JAKO JEDEN OBIEKT
            var molData = new OverlayMoleculeData
            {
                Id = _nextId++,
                Name = $"Selection {_nextId - 1}",
                SourceName = sourceDocument.Text,
                SourceMoleculeNumber = -1,
                SourceSymmetryIndex = 0,
                CurrentTransformation = transformationMode,
                Color = GetRandomColor(),
                IsStructure = false,
                IsSelection = true
            };

            LogToDebug($"  Creating {molData.Name} with {selectedIndices.Count} atoms", System.Drawing.Color.LightGray);

 // ⭐ ZBIERZ 3 ZAZNACZONE ATOMY (PUNKTY REFERENCYJNE)
            var refPoints = new List<Vector3>();
            var refNames = new List<string>();
            var refIndices = new List<int>();

 // ⭐ DODAJ WSZYSTKIE ZAZNACZONE ATOMY
            foreach (int idx in selectedIndices)
            {
                var atom = CopyAtom(molecule.atom[idx]);
                atom.CustomColor = molData.Color;
                molData.OriginalAtoms.Add(atom);

                if (refPoints.Count < 3)
                {
                    refPoints.Add(molecule.atom[idx].Coordinate);
                    refNames.Add(molecule.atom[idx].name);
                    refIndices.Add(idx);
                    LogToDebug($"  Reference point {refPoints.Count}: {molecule.atom[idx].name} at ({molecule.atom[idx].Coordinate.X:F4}, {molecule.atom[idx].Coordinate.Y:F4}, {molecule.atom[idx].Coordinate.Z:F4})", System.Drawing.Color.LightBlue);
                }
            }

 // ⭐ ZAPISZ ORYGINALNE PUNKTY REFERENCYJNE
            if (refPoints.Count == 3)
            {
                molData.OriginalReferencePoints = refPoints.ToArray();
                molData.ReferencePointNames = refNames.ToArray();
                molData.ReferencePointIndices = refIndices.ToArray();
                LogToDebug($"✅ Stored 3 reference points for {molData.Name}", System.Drawing.Color.Green);
            }
            else
            {
                LogToDebug($"⚠️ Only {refPoints.Count} reference points for {molData.Name} (need 3 for fitting)", System.Drawing.Color.Orange);
            }

 // ⭐ INICJALIZUJ DANE ROBOCZE (KOPIA ORYGINAŁÓW)
            molData.InitializeWorkingData();
            LogToDebug($"  Initialized working data with {molData.WorkingAtoms.Count} atoms", System.Drawing.Color.LightGray);

 // ⭐ CENTRUJ
            CenterMolecule(molData);
            LogToDebug($"  ✅ Molecule centered", System.Drawing.Color.Green);

 // ⭐ ZASTOSUJ TRANSFORMACJĘ NA ROBOCZYCH DANYCH
            ApplyTransformationToMolecule(molData);

            _overlayMolecules.Add(molData);
            LogToDebug($"  ✅ {molData.Name} added to overlay", System.Drawing.Color.Green);

 // ⭐⭐⭐ AUTOMATYCZNIE DOPASUJ IF TO NIE PIERWSZA MOLEKUŁA ⭐⭐⭐
            if (_overlayMolecules.Count > 1)
            {
                LogToDebug($"  🔧 Auto-fitting {molData.Name} to base molecule", System.Drawing.Color.Yellow);
                AutoFitMolecule(molData);
            }

            LogToDebug($"✅ Selection added to overlay", System.Drawing.Color.Green);
            LogToDebugHeader("═══════════════════════════════════════════════════", System.Drawing.Color.Cyan);

            OnMoleculesChanged();
        }

 // OverlayManager.cs - add tę metodę at the end klasy

 /// <summary>
 /// Checks czy molecule jest bazową (pierwszą) w nakładce.
 /// </summary>
        public bool IsBaseMolecule(int moleculeId)
        {
            var first = _overlayMolecules.FirstOrDefault();
            if (first == null) return false;
            return first.Id == moleculeId;
        }

 /// <summary>
 /// Returns ID pierwszej (bazowej) molecule or -1 if no.
 /// </summary>
        public int GetBaseMoleculeId()
        {
            var first = _overlayMolecules.FirstOrDefault();
            return first?.Id ?? -1;
        }
 /// <summary>
 /// Adds all atoms from the symmetry equivalents of the active document.
 /// </summary>
        public void AddSelectedStructure(MonoGameDocument sourceDocument, TransformationMode transformationMode)
        {
            LogToDebugHeader("═══════════════════════════════════════════════════", System.Drawing.Color.Cyan);
            LogToDebugHeader("📂 ADD SELECTED STRUCTURE", System.Drawing.Color.Cyan);
            LogToDebugHeader("═══════════════════════════════════════════════════", System.Drawing.Color.Cyan);

            if (sourceDocument == null)
            {
                LogToDebug("❌ Source document is null", System.Drawing.Color.Red);
                return;
            }

            var molecule = sourceDocument.GetMolecule();
            if (molecule == null || molecule.NumberOfAtoms == 0)
            {
                LogToDebug("❌ No molecule loaded in source document", System.Drawing.Color.Red);
                return;
            }

            LogToDebug($"📄 Source document: {sourceDocument.Text}", System.Drawing.Color.LightGray);
            LogToDebug($"📦 Molecule: {molecule.NumberOfAtoms} atoms", System.Drawing.Color.LightGray);

            var molData = new OverlayMoleculeData
            {
                Id = _nextId++,
                Name = $"Structure {_nextId - 1}",
                SourceName = sourceDocument.Text,
                SourceMoleculeNumber = -1,
                SourceSymmetryIndex = -1,
                CurrentTransformation = transformationMode,
                Color = GetRandomColor(),
                IsStructure = true,
                IsSelection = false
            };

            LogToDebug($"  Creating structure {molData.Name}", System.Drawing.Color.LightGray);

 // ⭐ ZBIERZ 3 ZAZNACZONE ATOMY (PUNKTY REFERENCYJNE)
            var refPoints = new List<Vector3>();
            var refNames = new List<string>();
            var refIndices = new List<int>();

 // ⭐ DODAJ WSZYSTKIE ATOMY
            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                var atom = CopyAtom(molecule.atom[i]);
                atom.CustomColor = molData.Color;
                molData.OriginalAtoms.Add(atom);

                if (molecule.atom[i].selected && refPoints.Count < 3)
                {
                    refPoints.Add(molecule.atom[i].Coordinate);
                    refNames.Add(molecule.atom[i].name);
                    refIndices.Add(i);
                    LogToDebug($"  Reference point {refPoints.Count}: {molecule.atom[i].name} at ({molecule.atom[i].Coordinate.X:F4}, {molecule.atom[i].Coordinate.Y:F4}, {molecule.atom[i].Coordinate.Z:F4})", System.Drawing.Color.LightBlue);
                }
            }

 // ⭐ ZAPISZ ORYGINALNE PUNKTY REFERENCYJNE
            if (refPoints.Count == 3)
            {
                molData.OriginalReferencePoints = refPoints.ToArray();
                molData.ReferencePointNames = refNames.ToArray();
                molData.ReferencePointIndices = refIndices.ToArray();
                LogToDebug($"✅ Stored 3 reference points for {molData.Name}", System.Drawing.Color.Green);
            }
            else
            {
                LogToDebug($"⚠️ Only {refPoints.Count} reference points for {molData.Name} (need 3 for fitting)", System.Drawing.Color.Orange);
            }

 // ⭐ INICJALIZUJ DANE ROBOCZE (KOPIA ORYGINAŁÓW)
            molData.InitializeWorkingData();
            LogToDebug($"  Initialized working data with {molData.WorkingAtoms.Count} atoms", System.Drawing.Color.LightGray);

 // ⭐ CENTRUJ
            CenterMolecule(molData);
            LogToDebug($"  ✅ Molecule centered", System.Drawing.Color.Green);

 // ⭐ ZASTOSUJ TRANSFORMACJĘ NA ROBOCZYCH DANYCH
            ApplyTransformationToMolecule(molData);

            _overlayMolecules.Add(molData);
            LogToDebug($"  ✅ {molData.Name} added to overlay", System.Drawing.Color.Green);

 // ⭐⭐⭐ AUTOMATYCZNIE DOPASUJ IF TO NIE PIERWSZA MOLEKUŁA ⭐⭐⭐
            if (_overlayMolecules.Count > 1)
            {
                LogToDebug($"  🔧 Auto-fitting {molData.Name} to base molecule", System.Drawing.Color.Yellow);
                AutoFitMolecule(molData);
            }

            LogToDebug($"✅ Structure added to overlay", System.Drawing.Color.Green);
            LogToDebugHeader("═══════════════════════════════════════════════════", System.Drawing.Color.Cyan);

            OnMoleculesChanged();
        }

 // OverlayManager.cs - MapBasisToBasis
 // Mapuje punkt świata z lokalnej bazy SOURCE (pivotSource, xs,ys,zs)
 // do odafterwiadającego punktu w bazie TARGET (pivotTarget, xt,yt,zt).
 // Obie bazy muszą be ortonormalne i have tę samą skrętność (obie
 // zbudowane przez ys/yt = Cross(zs/zt, xs/xt)) — wtedy result jest
 // afterprawnym obrotem sztywnym (rigid rotation), without aftertrzeby
 // ręcznego checksnia/odwracania wyznacznika.
        private Vector3 MapBasisToBasis(
            Vector3 worldPoint,
            Vector3 pivotSource, Vector3 pivotTarget,
            Vector3 xs, Vector3 ys, Vector3 zs,
            Vector3 xt, Vector3 yt, Vector3 zt)
        {
            Vector3 v = worldPoint - pivotSource;
            float a = Vector3.Dot(v, xs);
            float b = Vector3.Dot(v, ys);
            float c = Vector3.Dot(v, zs);
            return pivotTarget + a * xt + b * yt + c * zt;
        }

 // OverlayManager.cs - IterativeFit
 //sourceMolecule molecule to rotate after finding rotations
 //sourcepoints - 3 afterints of the molecule we want to rotate so they overlay targetpoints
 //targetpoints afterints to which we wont rotate our ourcepoints 
        public void IterativeFit(
            OverlayMoleculeData sourceMolecule,
            Vector3[] sourcePoints,
            Vector3[] targetPoints
             )
        {
            LogToDebugHeader("═══════════════════════════════════════════════════", System.Drawing.Color.Cyan);
            LogToDebugHeader("🔧 3-POINT FIT – mapowanie baza→baza (POPRAWIONY v2)", System.Drawing.Color.Cyan);
            LogToDebugHeader("═══════════════════════════════════════════════════", System.Drawing.Color.Cyan);

            if (sourcePoints == null || targetPoints == null ||
                sourcePoints.Length != 3 || targetPoints.Length != 3)
                throw new ArgumentException("Dokładnie 3 punkty");

            Vector3 currentColor = sourceMolecule.Color;
            int currentStyle = sourceMolecule.WorkingAtoms.Count > 0
                ? sourceMolecule.WorkingAtoms[0].style : 2;

            float initialError = 0f;
            for (int i = 0; i < 3; i++)
                initialError += Vector3.Distance(sourcePoints[i], targetPoints[i]);
            LogToDebug($"  Initial error: {initialError:F4} Å", System.Drawing.Color.Red);

 // ------------------------------------------------------------------
 // 1. Lokalna baza SOURCE (rightskrętna, licząc od sourcepoints[0])
 // ------------------------------------------------------------------
            Vector3 xs = sourcePoints[1] - sourcePoints[0];
            if (xs.LengthSquared() < 1e-12f) { LogToDebug("❌ P0-P1 (source) za blisko siebie", System.Drawing.Color.Red); return; }
            xs.Normalize();

            Vector3 tmpS = sourcePoints[2] - sourcePoints[0];
            Vector3 zs = Vector3.Cross(xs, tmpS);
            if (zs.LengthSquared() < 1e-12f) { LogToDebug("❌ Source points are collinear", System.Drawing.Color.Red); return; }
            zs.Normalize();

            Vector3 ys = Vector3.Cross(zs, xs); // already normalised (xs, zs orthogonal and unit)

 // ------------------------------------------------------------------
 // 2. Lokalna baza TARGET (ta sama konwencja => ta sama skrętność)
 // ------------------------------------------------------------------
            Vector3 xt = targetPoints[1] - targetPoints[0];
            if (xt.LengthSquared() < 1e-12f) { LogToDebug("❌ P0-P1 (target) za blisko siebie", System.Drawing.Color.Red); return; }
            xt.Normalize();

            Vector3 tmpT = targetPoints[2] - targetPoints[0];
            Vector3 zt = Vector3.Cross(xt, tmpT);
            if (zt.LengthSquared() < 1e-12f) { LogToDebug("❌ Target points are collinear", System.Drawing.Color.Red); return; }
            zt.Normalize();

            Vector3 yt = Vector3.Cross(zt, xt);

 // ------------------------------------------------------------------
 // 3. Maafterwanie punkt-after-punkcie between bazami (without mnożenia matrixy!)
 //
 // For dowolnego wektora v liczonego relative to sourcepoints[0]:
 // a = v·xs, b = v·ys, c = v·zs (coordinates w bazie SOURCE)
 // Ten sam punkt w bazie TARGET (czyli after obrocie) to:
 // v' = a*xt + b*yt + c*zt
 // Because obie bazy są zbudowane tą samą konwencją (right-handed),
 // to jest afterprawny obrót — without aftertrzeby ręcznego odwracania det.
 //
 // (Previous version budowała matrixe Ms/Mt i liczyła
 // R = Mtᵀ * Ms, after czym stosowała ją jako v*R. W konwencji
 // wektorów-wierszy używanej przez Vector3.Transform to dawało
 // obrót ODWROTNY do zamierzonego — stąd błędny result.)
 // ------------------------------------------------------------------
            Vector3 pivotSource = sourcePoints[0];
            Vector3 pivotTarget = targetPoints[0];

            LogToDebugHeader("🔄 LOCAL BASES:", System.Drawing.Color.Yellow);
            LogToDebug($"  xs=({xs.X:F3},{xs.Y:F3},{xs.Z:F3}) ys=({ys.X:F3},{ys.Y:F3},{ys.Z:F3}) zs=({zs.X:F3},{zs.Y:F3},{zs.Z:F3})");
            LogToDebug($"  xt=({xt.X:F3},{xt.Y:F3},{xt.Z:F3}) yt=({yt.X:F3},{yt.Y:F3},{yt.Z:F3}) zt=({zt.X:F3},{zt.Y:F3},{zt.Z:F3})");

 // ------------------------------------------------------------------
 // 4. Aplikacja do molecule
 // ------------------------------------------------------------------
            for (int i = 0; i < sourceMolecule.WorkingAtoms.Count; i++)
            {
                var atom = sourceMolecule.WorkingAtoms[i];

                atom.Coordinate = MapBasisToBasis(atom.Coordinate, pivotSource, pivotTarget, xs, ys, zs, xt, yt, zt);

                atom.CustomColor = currentColor;
                atom.style = currentStyle;
                sourceMolecule.WorkingAtoms[i] = atom;
            }

            if (sourceMolecule.WorkingReferencePoints != null)
            {
                for (int i = 0; i < sourceMolecule.WorkingReferencePoints.Length; i++)
                {
                    sourceMolecule.WorkingReferencePoints[i] = MapBasisToBasis(
                        sourceMolecule.WorkingReferencePoints[i], pivotSource, pivotTarget, xs, ys, zs, xt, yt, zt);
                }
            }

            // Pivot after rotation. Prefer the full ring (5 or 6) when both sides have it.
            Vector3 srcPivot = Vector3.Zero;
            Vector3 tgtPivot = Vector3.Zero;
            var baseMol = _overlayMolecules.FirstOrDefault();
            bool useRing = sourceMolecule.WorkingRingPoints != null &&
                           baseMol?.WorkingRingPoints != null &&
                           sourceMolecule.WorkingRingPoints.Length == baseMol.WorkingRingPoints.Length &&
                           sourceMolecule.WorkingRingPoints.Length >= 5;
            if (useRing)
            {
                // Map ring points with the same transform, then average all of them
                for (int i = 0; i < sourceMolecule.WorkingRingPoints.Length; i++)
                {
                    var mapped = MapBasisToBasis(sourceMolecule.WorkingRingPoints[i], pivotSource, pivotTarget, xs, ys, zs, xt, yt, zt);
                    sourceMolecule.WorkingRingPoints[i] = mapped;
                    srcPivot += mapped;
                    tgtPivot += baseMol.WorkingRingPoints[i];
                }
                srcPivot /= sourceMolecule.WorkingRingPoints.Length;
                tgtPivot /= baseMol.WorkingRingPoints.Length;
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    srcPivot += sourceMolecule.WorkingReferencePoints[i];
                    tgtPivot += targetPoints[i];
                }
                srcPivot /= 3f;
                tgtPivot /= 3f;
            }
            Vector3 shift = tgtPivot - srcPivot;
            if (shift.LengthSquared() > 1e-12f)
            {
                for (int i = 0; i < sourceMolecule.WorkingAtoms.Count; i++)
                {
                    var atom = sourceMolecule.WorkingAtoms[i];
                    atom.Coordinate += shift;
                    sourceMolecule.WorkingAtoms[i] = atom;
                }
                if (sourceMolecule.WorkingReferencePoints != null)
                {
                    for (int i = 0; i < sourceMolecule.WorkingReferencePoints.Length; i++)
                        sourceMolecule.WorkingReferencePoints[i] += shift;
                }
                if (sourceMolecule.WorkingRingPoints != null)
                {
                    for (int i = 0; i < sourceMolecule.WorkingRingPoints.Length; i++)
                        sourceMolecule.WorkingRingPoints[i] += shift;
                }
                LogToDebug($"  Pivot shift: ({shift.X:F4}, {shift.Y:F4}, {shift.Z:F4}) Å", System.Drawing.Color.LightGray);
            }

            RecalculateBondsForMolecule(sourceMolecule);
            sourceMolecule.Color = currentColor;

 // ------------------------------------------------------------------
 // 5. Raafterrt
 // ------------------------------------------------------------------
            float finalError = 0f;
            for (int i = 0; i < 3; i++)
                finalError += Vector3.Distance(
                    sourceMolecule.WorkingReferencePoints[i],
                    targetPoints[i]);

            LogToDebugHeader("📈 FINAL RESULT:", System.Drawing.Color.Yellow);
            LogToDebug($"  Error before: {initialError:F4} Å", System.Drawing.Color.Red);
            LogToDebug($"  Error after : {finalError:F4} Å", System.Drawing.Color.Green);
            float improvement = initialError - finalError;
            LogToDebug($"  Improvement : {improvement:F4} Å ({(initialError > 1e-6f ? improvement / initialError * 100f : 0f):F1}%)",
                improvement > 0 ? System.Drawing.Color.Green : System.Drawing.Color.Orange);

            LogToDebugHeader("✅ FIT COMPLETE", System.Drawing.Color.Green);
            LogToDebugHeader("═══════════════════════════════════════════════════", System.Drawing.Color.Cyan);
        }


 // OverlayManager.cs - ApplyMirrorToMolecule

        public enum MirrorAxis
        {
            X,  // Reflection in YZ plane (invert X)
            Y,  // Reflection in XZ plane (invert Y)
            Z   // Reflection in XY plane (invert Z)
        }

 /// <summary>
 /// Zastosowuje operation lustra (odbicia) do molecule.
 /// </summary>
 // OverlayManager.cs - ApplyMirrorToMolecule
 /// <summary>
 /// Zastosowuje operation lustra (odbicie relative to planes XY).
 /// To wystarczy, afternieważ algorytm dopasowania i tak dostosuje orientację.
 /// </summary>
        public void ApplyMirrorToMolecule(int moleculeId)
        {
            var molData = _overlayMolecules.FirstOrDefault(m => m.Id == moleculeId);
            if (molData == null) return;

            LogToDebugHeader($"🪞 MIRROR (reflection across XY plane)", System.Drawing.Color.Cyan);
            LogToDebug($"📦 Molecule: {molData.Name}", System.Drawing.Color.LightGray);

 // ⭐ ZAPISZ KOLOR I STYL
            Vector3 currentColor = molData.Color;
            int currentStyle = molData.WorkingAtoms.Count > 0 ? molData.WorkingAtoms[0].style : 2;

 // ⭐ RESETUJ DO ORYGINAŁÓW
            molData.ResetToOriginal();

 // Mirror: reflect in the XY plane (Z → −Z)
            for (int i = 0; i < molData.WorkingAtoms.Count; i++)
            {
                var atom = molData.WorkingAtoms[i];
                atom.Coordinate = new Vector3(atom.Coordinate.X, atom.Coordinate.Y, -atom.Coordinate.Z);

 // ⭐ ZACHOWAJ KOLOR I STYL
                atom.CustomColor = currentColor;
                atom.style = currentStyle;

                molData.WorkingAtoms[i] = atom;
            }

 // ⭐ ZAKTUALIZUJ PUNKTY REFERENCYJNE
            if (molData.WorkingReferencePoints != null)
            {
                for (int i = 0; i < molData.WorkingReferencePoints.Length; i++)
                {
                    molData.WorkingReferencePoints[i] = new Vector3(
                        molData.WorkingReferencePoints[i].X,
                        molData.WorkingReferencePoints[i].Y,
                        -molData.WorkingReferencePoints[i].Z
                    );
                }
                LogToDebug($"  Updated reference points after mirror", System.Drawing.Color.LightGray);
            }
            if (molData.WorkingRingPoints != null)
            {
                for (int i = 0; i < molData.WorkingRingPoints.Length; i++)
                {
                    var p = molData.WorkingRingPoints[i];
                    molData.WorkingRingPoints[i] = new Vector3(p.X, p.Y, -p.Z);
                }
            }

 // ⭐⭐⭐ USTAW TRANSFORMACJĘ ⭐⭐⭐
            molData.CurrentTransformation = TransformationMode.Mirror;

 // ⭐ ZACHOWAJ KOLOR
            molData.Color = currentColor;

 // ⭐ PRZELICZ WIĄZANIA
            RecalculateBondsForMolecule(molData);

            LogToDebug($"✅ Mirror applied", System.Drawing.Color.Green);

            OnMoleculesChanged();
        }

 // ============================================================
 // ⭐ ZDARZENIA
 // ============================================================

        private void OnMoleculesChanged()
        {
            MoleculesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}