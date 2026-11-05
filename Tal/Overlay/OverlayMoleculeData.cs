// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Molecule overlay module: OverlayMoleculeData.

﻿// OverlayMoleculeData.cs
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Tal;

namespace Tal.Overlay
{
 /// <summary>
 /// Przechowuje dane o afterjedynczej molekule dodanej do nakładania.
 /// </summary>
    public class OverlayMoleculeData
    {
 /// <summary>
 /// Unikalne ID w obrębie nakładania.
 /// </summary>
        public int Id { get; set; }

 /// <summary>
 /// Nazwa molecule (np. "Molecule 1", "Structure 2").
 /// </summary>
        public string Name { get; set; }

 /// <summary>
 /// Nazwa source (np. nazwa okna, z którego afterchodzi).
 /// </summary>
        public string SourceName { get; set; }

 /// <summary>
 /// Numer molecule w źródłowej strukturze (IndividualMoleculeInCell).
 /// </summary>
        public int SourceMoleculeNumber { get; set; }

 /// <summary>
 /// Numer operacji symmetry w źródłowej strukturze.
 /// </summary>
        public int SourceSymmetryIndex { get; set; }

 // ============================================================
 // ⭐ DANE - ORYGINAŁY I KOPIE ROBOCZE
 // ============================================================

 /// <summary>
 /// Oryginalne dane (always niezmienione - source prawdy).
 /// </summary>
        public List<Molecule.Atom> OriginalAtoms { get; set; } = new List<Molecule.Atom>();

 /// <summary>
 /// Bieżące dane robocze (after transformationch i dopasowaniu).
 /// </summary>
        public List<Molecule.Atom> WorkingAtoms { get; set; } = new List<Molecule.Atom>();

 /// <summary>
 /// Oryginalne punkty referencyjne (niezmienione).
 /// </summary>
        public Vector3[] OriginalReferencePoints { get; set; }

 /// <summary>
 /// Bieżące punkty referencyjne (after transformationch i dopasowaniu).
 /// </summary>
        public Vector3[] WorkingReferencePoints { get; set; }

 /// <summary>
 /// Nazwy atoms referencyjnych.
 /// </summary>
        public string[] ReferencePointNames { get; set; }

 /// <summary>
 /// Indeksy atoms referencyjnych w źródłowej molekule.
 /// </summary>
        public int[] ReferencePointIndices { get; set; }

        /// <summary>All ring atoms (5 or 6) used for the averaged pivot.</summary>
        public Vector3[] OriginalRingPoints { get; set; }
        public Vector3[] WorkingRingPoints { get; set; }

 /// <summary>
 /// Czy molecule ma setione punkty referencyjne.
 /// </summary>
        public bool HasReferencePoints => OriginalReferencePoints != null && OriginalReferencePoints.Length == 3;

 // ============================================================
 // ⭐ METADANE
 // ============================================================

 /// <summary>
 /// Aktualna transformation zastosowana do tej molecule.
 /// </summary>
        public TransformationMode CurrentTransformation { get; set; } = TransformationMode.None;

 /// <summary>
 /// Macierz transformation (calculatesna na afterdstawie CurrentTransformation).
 /// </summary>
        public Matrix TransformationMatrix { get; set; } = Matrix.Identity;

 /// <summary>
 /// Kolor molecule (do wyświetlania).
 /// </summary>
        public Vector3 Color { get; set; } = new Vector3(0.8f, 0.8f, 0.8f);

 /// <summary>
 /// Czy molecule jest visible.
 /// </summary>
        public bool IsVisible { get; set; } = true;

 /// <summary>
 /// Czy molecule afterchodzi z entire structure (czy only z molecule).
 /// </summary>
        public bool IsStructure { get; set; } = false;

 /// <summary>
 /// Czy molecule afterchodzi z zaznaczenia atoms (a nie z entire molecule).
 /// </summary>
        public bool IsSelection { get; set; } = false;

 // ============================================================
 // ⭐ ORYGINALNE MACIERZE (for ellipsoids)
 // ============================================================

 /// <summary>
 /// Przechowuje oryginalne matrixe before transformacją.
 /// </summary>
        public struct OriginalMatrixData
        {
            public Matrix Un;
            public Matrix Ue;
            public Matrix Transformation;
        }

 /// <summary>
 /// Lista oryginalnych matrixy for each atom.
 /// </summary>
        public List<OriginalMatrixData> OriginalMatrices { get; set; } = new List<OriginalMatrixData>();

 // ============================================================
 // ⭐ ZARZĄDZANIE DANYMI
 // ============================================================

 /// <summary>
 /// Zapisuje oryginalne matrixe (Un, Ue, Transformation).
 /// </summary>
        public void StoreOriginalMatrices()
        {
            OriginalMatrices.Clear();
            foreach (var atom in OriginalAtoms)
            {
                OriginalMatrices.Add(new OriginalMatrixData
                {
                    Un = atom.Un,
                    Ue = atom.Ue,
                    Transformation = atom.Transformation
                });
            }
        }

 /// <summary>
 /// Przywraca oryginalne matrixe (Un, Ue, Transformation).
 /// </summary>
        public void RestoreOriginalMatrices()
        {
            if (OriginalMatrices.Count != OriginalAtoms.Count) return;
            for (int i = 0; i < OriginalAtoms.Count && i < OriginalMatrices.Count; i++)
            {
                var atom = OriginalAtoms[i];
                atom.Un = OriginalMatrices[i].Un;
                atom.Ue = OriginalMatrices[i].Ue;
                atom.Transformation = OriginalMatrices[i].Transformation;
                OriginalAtoms[i] = atom;
            }
        }

 /// <summary>
 /// Inicjalizuje dane robocze - copye oryginały do roboczych.
 /// </summary>
        public void InitializeWorkingData()
        {
 // ⭐ KOPIUJ ATOMY
            WorkingAtoms.Clear();
            foreach (var atom in OriginalAtoms)
            {
                WorkingAtoms.Add(atom);
            }

 // ⭐ KOPIUJ PUNKTY REFERENCYJNE
            if (OriginalReferencePoints != null && OriginalReferencePoints.Length == 3)
            {
                WorkingReferencePoints = new Vector3[3];
                for (int i = 0; i < 3; i++)
                {
                    WorkingReferencePoints[i] = OriginalReferencePoints[i];
                }
            }
            if (OriginalRingPoints != null)
            {
                WorkingRingPoints = new Vector3[OriginalRingPoints.Length];
                for (int i = 0; i < OriginalRingPoints.Length; i++)
                    WorkingRingPoints[i] = OriginalRingPoints[i];
            }

 // ⭐ KOPIUJ MACIERZE
            StoreOriginalMatrices();
        }

 /// <summary>
 /// Przywraca dane robocze do stanu oryginalnego.
 /// </summary>
 // OverlayMoleculeData.cs - ResetToOriginal

 // OverlayMoleculeData.cs - ResetToOriginal

        public void ResetToOriginal()
        {
 // ⭐ ZAPISZ AKTUALNY KOLOR I STYL
            Vector3 currentColor = Color;
            int currentStyle = WorkingAtoms.Count > 0 ? WorkingAtoms[0].style : 2;

 // ⭐ PRZYWROC ATOMY
            WorkingAtoms.Clear();
            foreach (var atom in OriginalAtoms)
            {
                var newAtom = atom;
 // ⭐ PRZYWRÓĆ KOLOR I STYL
                newAtom.CustomColor = currentColor;
                newAtom.style = currentStyle;
                WorkingAtoms.Add(newAtom);
            }

 // ⭐ PRZYWROC PUNKTY REFERENCYJNE
            if (OriginalReferencePoints != null && OriginalReferencePoints.Length == 3)
            {
                WorkingReferencePoints = new Vector3[3];
                for (int i = 0; i < 3; i++)
                {
                    WorkingReferencePoints[i] = OriginalReferencePoints[i];
                }
            }
            if (OriginalRingPoints != null)
            {
                WorkingRingPoints = new Vector3[OriginalRingPoints.Length];
                for (int i = 0; i < OriginalRingPoints.Length; i++)
                    WorkingRingPoints[i] = OriginalRingPoints[i];
            }

 // ⭐ PRZYWROC MACIERZE
            RestoreOriginalMatrices();

 // ⭐ PRZYWRÓĆ KOLOR W MOLEKULE
            Color = currentColor;
        }

 // ============================================================
 // ⭐ METODY WYŚWIETLANIA
 // ============================================================

        public override string ToString()
        {
            string type = IsStructure ? "📂" : (IsSelection ? "✏️" : "🧪");
            string visibility = IsVisible ? "👁️" : "🙈";
            string trans = CurrentTransformation != TransformationMode.None ? $" [{CurrentTransformation}]" : "";
            string selectionInfo = IsSelection ? $" (selection: {WorkingAtoms?.Count ?? 0} atoms)" : "";
            string atomInfo = $" (atoms: {WorkingAtoms?.Count ?? 0})";
            return $"{visibility} {type} {Name}{atomInfo}{trans}{selectionInfo}";
        }

 /// <summary>
 /// Returns krótki opis do display w list.
 /// </summary>
        public string GetDisplayString()
        {
            string trans = CurrentTransformation != TransformationMode.None ? $" [{CurrentTransformation}]" : "";
            return $"{Name} (src: {SourceName}, mol: {SourceMoleculeNumber}){trans}";
        }
    }
}