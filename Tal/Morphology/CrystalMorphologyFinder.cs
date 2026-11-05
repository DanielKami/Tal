// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal morphology / Wulff module: CrystalMorphologyFinder.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// Wersja: BFDH z 5 trybami softening + PRAWDZIWA symmetry (space group)

using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Tal.Calculate
{
 /// <summary>
 /// Tryby osłabiania BFDH (kompresji DistFrac).
 /// NOTE: to NIE są modele fizyczne — to heurystyka wizualna.
 /// </summary>
    public enum SofteningMode
    {
        None = 0,     // brak kompresji — czysty BFDH
        Clamp = 1,    // twardy clamp do maxAllowedDist
        Sqrt = 2,     // kompresja sqrt — naturalna
        Tanh = 3,     // kompresja tanh — najbardziej elastyczna
        Linear = 4    // kompresja liniowa — zachowuje proporcje
    }

    public static class CrystalMorphologyFinder
    {
        #region Cache (żeby nie liczyć 2× dla tego samego kryształu)

        private static string _lastCacheKey;
        private static CrystalMorphologyResult _lastResult;

        private static string MakeCacheKey(Molecule mol, int maxIdx, float minD,
                                            float softening, SofteningMode mode)
        {
            return $"{mol?.Name}|{mol?.a:F3}|{mol?.b:F3}|{mol?.c:F3}|" +
                   $"{mol?.alpha:F4}|{mol?.beta:F4}|{mol?.gamma:F4}|" +
                   $"{mol?.SpaceGroupNumber}|{mol?.SpaceGroupName}|" +
                   $"{maxIdx}|{minD:F3}|{softening:F3}|{(int)mode}";
        }

        public static void ClearCache()
        {
            _lastCacheKey = null;
            _lastResult = null;
        }

        #endregion

        #region Public API

        public static CrystalMorphologyResult Calculate(
            Molecule molecule,
            int maxIndex = 3,
            float minDSpacing = 0.5f,
            float softening = 3.0f,
            SofteningMode mode = SofteningMode.Sqrt)
        {
 // ═══ CACHE HIT ═══
            string key = MakeCacheKey(molecule, maxIndex, minDSpacing, softening, mode);
            if (_lastCacheKey == key && _lastResult != null)
            {
                Debug.WriteLine($"[CrystalMorphologyFinder] ✅ CACHE HIT — zwracam poprzedni wynik");
                return _lastResult;
            }

            Debug.WriteLine($"[CrystalMorphologyFinder] 🔄 CACHE MISS — liczę od nowa");

            var result = new CrystalMorphologyResult();

            if (molecule == null)
            {
                Debug.WriteLine("[CrystalMorphologyFinder] No molecule");
                return result;
            }

            if (molecule.a <= 0 || molecule.b <= 0 || molecule.c <= 0)
            {
                Debug.WriteLine("[CrystalMorphologyFinder] Invalid cell parameters");
                return result;
            }

            result.CellA = molecule.a;
            result.CellB = molecule.b;
            result.CellC = molecule.c;
            result.CellAlpha = MathHelperTal.ToDegrees(molecule.alpha);
            result.CellBeta = MathHelperTal.ToDegrees(molecule.beta);
            result.CellGamma = MathHelperTal.ToDegrees(molecule.gamma);
            result.CellVolume = molecule.Volume;
            result.Molecule = molecule;

            Debug.WriteLine($"[CrystalMorphologyFinder] Cell: " +
                            $"a={molecule.a:F3} b={molecule.b:F3} c={molecule.c:F3}");
            Debug.WriteLine($"[CrystalMorphologyFinder] SpaceGroup: " +
                            $"{molecule.SpaceGroupNumber} ({molecule.SpaceGroupName})");

 // ═══ 1. Generate planes ═══
            var uniquePlanes = GenerateUniquePlanes(maxIndex);
            Debug.WriteLine($"[CrystalMorphologyFinder] Generated {uniquePlanes.Count} planes");

 // ═══ 2. Afterlicz d_hkl i multiplicity (PRAWDZIWĄ z space group) ═══
            var faces = new List<CrystalFace>();

            foreach (var (h, k, l) in uniquePlanes)
            {
                float d = CalculateDSpacing(molecule, h, k, l);
                if (d < minDSpacing || d <= 0) continue;

                int multiplicity = CountEquivalentPlanesReal(h, k, l, molecule);

                var face = new CrystalFace
                {
                    H = h,
                    K = k,
                    L = l,
                    DSpacing = d,
                    Multiplicity = multiplicity,
                    NormalCart = ComputeCartesianNormal(molecule, h, k, l)
                };

                faces.Add(face);
            }

            Debug.WriteLine($"[CrystalMorphologyFinder] Kept {faces.Count} faces");
            if (faces.Count == 0) return result;

 // ═══ 3. maxD ═══
            float maxD = 0f;
            foreach (var f in faces)
                if (f.DSpacing > maxD) maxD = f.DSpacing;

 // ═══ 4. RelativeGrowthRate ∝ 1/d ═══
            foreach (var f in faces)
                f.RelativeGrowthRate = 1.0f / f.DSpacing;

 // ═══ 4b. DistFrac = RelativeGrowthRate / minGrowth ═══
            float minGrowth = float.MaxValue;
            foreach (var f in faces)
                if (f.RelativeGrowthRate > 1e-9f && f.RelativeGrowthRate < minGrowth)
                    minGrowth = f.RelativeGrowthRate;

            foreach (var f in faces)
            {
                f.DistFrac = (minGrowth < float.MaxValue && minGrowth > 1e-9f)
                    ? f.RelativeGrowthRate / minGrowth
                    : 1f;
            }

 // ═══ 4c. SOFTENING — różne tryby ═══
            ApplySoftening(faces, softening, mode);

 // ═══ 5. RelativeArea — TYLKO jako info, NIE do sortowania ═══
 // ⚠️ Fizycznie: afterle faces zależy od DistFrac (depth cięcia),
 // a nie od Multiplicity. Fortego NIE mnożymy przez Multiplicity.
            float maxArea = 0f;
            foreach (var f in faces)
            {
                f.RelativeArea = 1.0f / f.DistFrac;   // im mniejsze DistFrac, tym większa ściana
                if (f.RelativeArea > maxArea) maxArea = f.RelativeArea;
            }

            if (maxArea > 0)
                foreach (var f in faces)
                    f.RelativeArea /= maxArea;

 // ═══ 6. Sort after DistFrac (od największej faces = najmniejszy DistFrac) ═══
            faces.Sort((a, b) => a.DistFrac.CompareTo(b.DistFrac));

            result.Faces = faces;
            result.Habitus = ClassifyHabitus(faces);

            Debug.WriteLine($"[CrystalMorphologyFinder] Habitus: {result.Habitus}, mode={mode}");
            for (int i = 0; i < Math.Min(10, faces.Count); i++)
            {
                var f = faces[i];
                Debug.WriteLine($"  {f.Name}  d={f.DSpacing:F3}  mult={f.Multiplicity}  DistFrac={f.DistFrac:F3}");
            }

 // ═══ CACHE STORE ═══
            _lastCacheKey = key;
            _lastResult = result;

            return result;
        }

        #endregion

        #region Softening

        private static void ApplySoftening(List<CrystalFace> faces, float strength, SofteningMode mode)
        {
            switch (mode)
            {
                case SofteningMode.None:
                    Debug.WriteLine("[Softening] None — brak kompresji");
                    break;

                case SofteningMode.Clamp:
                    Debug.WriteLine($"[Softening] Clamp — max={strength:F3}");
                    foreach (var f in faces)
                        if (f.DistFrac > strength)
                            f.DistFrac = strength;
                    break;

                case SofteningMode.Sqrt:
                    Debug.WriteLine("[Softening] Sqrt");
                    foreach (var f in faces)
                        f.DistFrac = (float)Math.Sqrt(f.DistFrac);
                    break;

                case SofteningMode.Tanh:
                    {
                        float s = Math.Max(strength, 0.05f);
                        Debug.WriteLine($"[Softening] Tanh — strength={s:F3}");
                        foreach (var f in faces)
                        {
                            float x = f.DistFrac - 1.0f;
                            f.DistFrac = 1.0f + (float)Math.Tanh(x * s) / s;
                        }
                    }
                    break;

                case SofteningMode.Linear:
                    {
                        float c = Math.Max(strength, 0.05f);
                        Debug.WriteLine($"[Softening] Linear — compression={c:F3}");
                        foreach (var f in faces)
                            f.DistFrac = 1.0f + (f.DistFrac - 1.0f) / c;
                    }
                    break;
            }
        }

        #endregion

        #region Plane Generation

        private static List<(int h, int k, int l)> GenerateUniquePlanes(int maxIndex)
        {
            var result = new List<(int, int, int)>();

            for (int h = -maxIndex; h <= maxIndex; h++)
                for (int k = -maxIndex; k <= maxIndex; k++)
                    for (int l = -maxIndex; l <= maxIndex; l++)
                    {
                        if (h == 0 && k == 0 && l == 0) continue;
                        if (Gcd3(h, k, l) > 1) continue;
                        result.Add((h, k, l));
                    }

            return result;
        }

        private static int Gcd3(int a, int b, int c)
            => Gcd(Gcd(Math.Abs(a), Math.Abs(b)), Math.Abs(c));

        private static int Gcd(int a, int b)
        {
            while (b != 0) { int t = b; b = a % b; a = t; }
            return a;
        }

        #endregion

        #region Equivalent Planes — PRAWDZIWA symetria

 /// <summary>
 /// ⭐ Prawdziwa multiplicity — orbita faces (hkl) afterd działaniem
 /// operacji symmetry z space group kryształu.
 ///
 /// Zamiast zakładać cubic — we use RZECZYWISTEJ grupy z Molecule.
 ///
 /// For ścian we use TYLKO part liniowej operacji symmetry
 /// (without translation — translacje nie zmieniają orientacji planes).
 /// </summary>
        private static int CountEquivalentPlanesReal(int h, int k, int l, Molecule molecule)
        {
            if (molecule == null)
                return CountEquivalentPlanesCubic(h, k, l);

            int groupNum = molecule.SpaceGroupNumber;
            if (groupNum < 1 || groupNum > 230)
            {
                Debug.WriteLine(
                    $"[Multiplicity] ⚠️ Invalid group number {groupNum} → cubic fallback");
                return CountEquivalentPlanesCubic(h, k, l);
            }

 // ⭐ Afterbierz operacje symmetry z grupy
            List<SymmetryElements.ReadSymm> ops;
            try
            {
                ops = SymmetryElements.CreateTransformations(
                    groupNum,
                    molecule.SpaceGroupName,
                    explicitOperations: null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"[Multiplicity] ⚠️ CreateTransformations failed: {ex.Message} → cubic fallback");
                return CountEquivalentPlanesCubic(h, k, l);
            }

            if (ops == null || ops.Count == 0)
            {
                Debug.WriteLine(
                    $"[Multiplicity] ⚠️ No symmetry operations for group {groupNum} → cubic fallback");
                return CountEquivalentPlanesCubic(h, k, l);
            }

 // ⭐ Zbierz orbitę (hkl)
            var orbit = new HashSet<(int, int, int)>();

            foreach (var op in ops)
            {
 // Zbuduj matrix liniową 3×3 operacji (wiersze = osie x', y', z')
 // Wiersz i: for each coordinatesj j, współczynnik przy x_j
                int[,] M = new int[3, 3];
                for (int row = 0; row < 3; row++)
                {
                    if (op.pos1[row] >= 0 && op.pos1[row] < 3 && op.sign1[row] != 0)
                        M[row, op.pos1[row]] += op.sign1[row];

                    if (op.pos2[row] >= 0 && op.pos2[row] < 3 && op.sign2[row] != 0)
                        M[row, op.pos2[row]] += op.sign2[row];
                }

 // ⭐ Transformacja kontrawariantna wektora normalnego (hkl):
 // h'_i = sum_j M[j, i] * h_j (transposition matrixy liniowej)
                int hNew = M[0, 0] * h + M[1, 0] * k + M[2, 0] * l;
                int kNew = M[0, 1] * h + M[1, 1] * k + M[2, 1] * l;
                int lNew = M[0, 2] * h + M[1, 2] * k + M[2, 2] * l;

 // Zredukuj do kanonicznej afterstaci
                var canonical = CanonicalHkl(hNew, kNew, lNew);
                orbit.Add(canonical);
            }

            int mult = orbit.Count;

#if DEBUG
            Debug.WriteLine(
                $"[Multiplicity] ({h}{k}{l}) in group {groupNum} → mult={mult} " +
                $"(cubic would give {CountEquivalentPlanesCubic(h, k, l)})");
#endif

            return mult;
        }

 /// <summary>
 /// ⭐ Kanoniczna become (hkl) — zredukowana przez GCD i ustalonym znakiem.
 /// Dwie faces (hkl) i (-h-k-l) to ta sama ściana geometrycznie.
 /// </summary>
        private static (int h, int k, int l) CanonicalHkl(int h, int k, int l)
        {
 // Remove wspólny dzielnik
            int g = Gcd3(h, k, l);
            if (g > 1)
            {
                h /= g;
                k /= g;
                l /= g;
            }

 // Ustal znak: pierwsza niezerowa współrzędna ma be dodatnia
            if (h < 0 || (h == 0 && k < 0) || (h == 0 && k == 0 && l < 0))
            {
                h = -h;
                k = -k;
                l = -l;
            }

            return (h, k, l);
        }

 /// <summary>
 /// ⭐ Fallback — zakłada symetrię kubiczną.
 /// Used TYLKO when nie mamy space group kryształu
 /// albo when CreateTransformations zwróciło 0 operacji.
 /// </summary>
        private static int CountEquivalentPlanesCubic(int h, int k, int l)
        {
            int ah = Math.Abs(h), ak = Math.Abs(k), al = Math.Abs(l);
            int nonzero = (ah != 0 ? 1 : 0) + (ak != 0 ? 1 : 0) + (al != 0 ? 1 : 0);
            if (nonzero == 0) return 1;

            int uniq = 0;
            if (ah != 0)
            {
                uniq++;
                if (ak != 0 && ak != ah) uniq++;
                if (al != 0 && al != ah && (ak == 0 || al != ak)) uniq++;
            }
            else if (ak != 0)
            {
                uniq++;
                if (al != 0 && al != ak) uniq++;
            }
            else uniq++;

            switch (nonzero)
            {
                case 1: return 6;
                case 2: return uniq == 1 ? 12 : 24;
                case 3:
                    if (uniq == 1) return 8;
                    if (uniq == 2) return 24;
                    return 48;
            }
            return 2;
        }

        #endregion

        #region d-Spacing

        private static float CalculateDSpacing(Molecule mol, int h, int k, int l)
        {
            double a = mol.a, b = mol.b, c = mol.c;
            double al = mol.alpha, be = mol.beta, ga = mol.gamma;

            double cosAl = Math.Cos(al), cosBe = Math.Cos(be), cosGa = Math.Cos(ga);
            double sinAl = Math.Sin(al);

            double V = a * b * c * Math.Sqrt(
                1 - cosAl * cosAl - cosBe * cosBe - cosGa * cosGa
                + 2 * cosAl * cosBe * cosGa);

            if (V < 1e-9) return 0;

            double s11 = b * b * c * c * sinAl * sinAl;
            double s22 = a * a * c * c * Math.Sin(be) * Math.Sin(be);
            double s33 = a * a * b * b * Math.Sin(ga) * Math.Sin(ga);
            double s12 = a * b * c * c * (cosAl * cosBe - cosGa);
            double s23 = a * a * b * c * (cosBe * cosGa - cosAl);
            double s13 = a * b * b * c * (cosGa * cosAl - cosBe);

            double invD2 = (s11 * h * h + s22 * k * k + s33 * l * l
                          + 2 * s12 * h * k + 2 * s23 * k * l + 2 * s13 * h * l) / (V * V);

            if (invD2 <= 0) return 0;

            float d = (float)(1.0 / Math.Sqrt(invD2));
            float maxCell = (float)Math.Sqrt(a * a + b * b + c * c);
            if (d > maxCell * 1.05f) return 0;

            return d;
        }

        #endregion

        #region Cartesian Normal

        private static Vector3 ComputeCartesianNormal(Molecule mol, int h, int k, int l)
        {
            Matrix A = mol.FractionaltoCarttesianMatrix(0, 0, 0);
            Matrix invA = Matrix.Invert(A);
            Vector3 hkl = new Vector3(h, k, l);
            Vector3 n = Vector3.TransformNormal(hkl, invA);
            if (n.LengthSquared() > 1e-9f) n.Normalize();
            return n;
        }

        #endregion

        #region Habitus

        private static string ClassifyHabitus(List<CrystalFace> faces)
        {
            if (faces.Count == 0) return "unknown";

            float sum100 = 0, sum010 = 0, sum001 = 0;
            float sum110 = 0, sum101 = 0, sum011 = 0;
            float sum111 = 0;

            foreach (var f in faces)
            {
                int h = Math.Abs(f.H), k = Math.Abs(f.K), l = Math.Abs(f.L);
                float a = f.RelativeArea;

                var sorted = new[] { h, k, l };
                Array.Sort(sorted);
                int s0 = sorted[0], s1 = sorted[1], s2 = sorted[2];

                if (s0 == 1 && s1 == 0 && s2 == 0)
                {
                    if (h == 1 && k == 0 && l == 0) sum100 += a;
                    else if (h == 0 && k == 1 && l == 0) sum010 += a;
                    else if (h == 0 && k == 0 && l == 1) sum001 += a;
                }
                else if (s0 == 1 && s1 == 1 && s2 == 0)
                {
                    if (h == 1 && k == 1 && l == 0) sum110 += a;
                    else if (h == 1 && k == 0 && l == 1) sum101 += a;
                    else if (h == 0 && k == 1 && l == 1) sum011 += a;
                }
                else if (s0 == 1 && s1 == 1 && s2 == 1) sum111 += a;
            }

            float maxAxial = Math.Max(sum100, Math.Max(sum010, sum001));
            float sumAll = sum100 + sum010 + sum001 + sum110 + sum101 + sum011 + sum111;
            if (sumAll < 0.001f) return "unknown";

            if (sum111 > 0.3f * sumAll && maxAxial > 0.2f * sumAll) return "cubic / octahedral";
            if (sum001 > 0.4f * sumAll && sum100 + sum010 > 0.3f * sumAll) return "plate";
            int bigAxes = 0;
            if (sum100 > 0.15f * sumAll) bigAxes++;
            if (sum010 > 0.15f * sumAll) bigAxes++;
            if (sum001 > 0.15f * sumAll) bigAxes++;
            if (bigAxes >= 2 && sum111 < 0.1f * sumAll) return "needle / prismatic";
            if (sum110 + sum101 + sum011 > 0.3f * sumAll) return "rhombic / dodecahedral";
            if (maxAxial < 0.4f * sumAll) return "equant (isometric)";
            return "prismatic";
        }

        #endregion
    }
}