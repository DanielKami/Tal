// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Crystal morphology / Wulff module: MorphologyCrystal.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// Wersja: dokładna normalna z CrystalFace.NormalCart (stabilne FaceId for edges)

using System.Linq;
using Microsoft.Xna.Framework;
using Tal.Utils;
using Color = Microsoft.Xna.Framework.Color;

namespace Tal
{
    public class MorphologyCrystal
    {
        public class Tri
        {
            public Vector3 A, B, C;
            public Vector3 Normal;
            public string FaceName;
            public float FaceArea;
            public Color FaceColor;
        }

        public List<Tri> Triangles = new List<Tri>();
        public float BoundingRadius = 1f;

        private static void LogDetail(string msg) => ProgressReporter.Log(msg, System.Drawing.Color.LightBlue);
        private static void LogWarn(string msg) => ProgressReporter.Log(msg, System.Drawing.Color.Orange);
        private static void LogSuccess(string msg) => ProgressReporter.Log(msg, System.Drawing.Color.LightGreen);
        private static void LogError(string msg) => ProgressReporter.Log(msg, System.Drawing.Color.Red);

        public static MorphologyCrystal Build(CrystalMorphologyResult result,
                                             float scale = 1.0f,
                                             float faceOffset = 1.0f)
        {
            if (result == null || result.Faces == null || result.Faces.Count == 0)
            {
                LogWarn("[Build] Brak wyniku BFDH → fallback do BuildSimpleTest");
                return BuildSimpleTest();
            }

            var crystal = new MorphologyCrystal();

            LogDetail($"═══ BUILD: faces={result.Faces.Count}, scale={scale:F2} ═══");

            float boxR = 3.0f * Math.Max(scale, 0.5f);
            var poly = ConvexPolyhedronFrac.FromBox(boxR);

            const int MaxSuccessfulClips = 14;
            int clipCount = 0, changed = 0, destroyed = 0, successful = 0;
 // ⭐ Transposition — spójnie z Molecule.PrepareUnitCell.
 // FractionaltoCarttesianMatrix ma kolumny = wektory a,b,c (konwencja x_cart = M·x_frac),
 // a XNA Vector3.Transform robi v·M (= M^T·v for v jako kolumny),
 // so mustmy transafternewć, so that dostać M·v.
            Matrix fracToCart;
            if (result.Molecule != null)
                fracToCart = Matrix.Transpose(result.Molecule.FractionaltoCarttesianMatrix(0, 0, 0));
            else
            {
                LogWarn("[Build] ⚠️ No Molecule — Identity (cubic assumption)");
                fracToCart = Matrix.Identity;
            }

 // cartToFrac = (M^T)^{-1} = (M^{-1})^T — TransformNormal(hkl, cartToFrac) da M^{-1}·hkl.
            Matrix cartToFrac = Matrix.Invert(fracToCart);

 // ⭐ Sort TYLKO after DistFrac (od największej faces).
 // NIE after |h|+|k|+|l| — to zakłada symetrię kubiczną i jest błędne
 // for cells niekubicznych. ThenBy(h,k,l) only for determinizmu.
            var ordered = result.Faces
                .OrderBy(f => f.DistFrac)
                .ThenBy(f => f.H)
                .ThenBy(f => f.K)
                .ThenBy(f => f.L)
                .ToList();

            foreach (var face in ordered)
            {
                if (successful >= MaxSuccessfulClips) break;

                int h = face.H, k = face.K, l = face.L;
                if (h == 0 && k == 0 && l == 0) continue;

 // ⭐ ‖g‖ — length wektora normalnego (hkl) w metryce Cartesianj.
 // g = U^{-T} · (h,k,l) — to samo co w ComputeCartesianNormal,
 // only without normalisation (aftertrzebujemy length).
                Vector3 g = Vector3.TransformNormal(new Vector3(h, k, l), cartToFrac);
                float norm = g.Length();

                if (norm < 1e-9f) continue;

 // dScaled = distance od afterczątku do planes (hkl)
 // wyrażona w jednostkach ułamkowych.
 // d_cart = DistFrac * d_hkl (w Å, wzdłuż normalnej)
 // d_frac = d_cart / ‖g‖ (bo ‖g‖ ma jednostkę 1/Å)
                float dScaled = face.DistFrac * norm * faceOffset;

                float F(Vector3 p) => h * p.X + k * p.Y + l * p.Z - dScaled;
                const float EPS = 1e-5f;

                bool canCut = false;
                foreach (var tri in poly.Triangles)
                {
                    if (F(tri.A) > EPS || F(tri.B) > EPS || F(tri.C) > EPS)
                    {
                        canCut = true;
                        break;
                    }
                }
                if (!canCut) continue;

                var backup = poly.Triangles;
                int before = poly.Triangles.Count;

                poly.ClipByPlane(h, k, l, dScaled, face.Name);
                clipCount++;

                if (poly.Triangles.Count == 0)
                {
                    poly.Triangles = new List<ConvexPolyhedronFrac.Tri>(backup);
                    destroyed++;
                }
                else if (poly.Triangles.Count != before)
                {
                    changed++;
                    successful++;
                }
            }

            LogDetail($"[Build] Clips: {clipCount}, successful: {successful}, destroyed: {destroyed}, tris: {poly.Triangles.Count}");

            if (poly.Triangles.Count == 0)
            {
                LogError("[Build] ❌ Pusty wynik — fallback");
                return BuildSimpleTest();
            }

 // ⭐ Konversion końcowa: ułamkowe → Cartesian.
 // (Ta sama matrix fracToCart, użyta do transformation vertices.)
            Matrix finalFracToCart = fracToCart;

 // ⭐ Mapa FaceName → dokładna normalna z BFDH.
            var faceNormals = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            if (result.Faces != null)
            {
                foreach (var f in result.Faces)
                {
                    if (string.IsNullOrEmpty(f.Name)) continue;
                    faceNormals[f.Name] = f.NormalCart;
                }
            }

 // Konversion + obliczenie pól
            var faceAreas = new Dictionary<string, float>(StringComparer.Ordinal);

            foreach (var tri in poly.Triangles)
            {
                Vector3 A = Vector3.TransformNormal(tri.A, finalFracToCart);
                Vector3 B = Vector3.TransformNormal(tri.B, finalFracToCart);
                Vector3 C = Vector3.TransformNormal(tri.C, finalFracToCart);

                float area = 0.5f * Vector3.Cross(B - A, C - A).Length();

 // ⭐ Dokładna normalna z CrystalFace (if dostępna).
                Vector3 normal;
                if (!string.IsNullOrEmpty(tri.FaceName) &&
                    faceNormals.TryGetValue(tri.FaceName, out var exactNormal) &&
                    exactNormal.LengthSquared() > 1e-9f)
                {
                    normal = exactNormal;
                }
                else
                {
                    normal = Vector3.Cross(B - A, C - A);
                    if (normal.LengthSquared() > 1e-9f)
                        normal.Normalize();
                    else
                        normal = Vector3.Zero;
                }

                if (!faceAreas.TryGetValue(tri.FaceName, out var sum))
                    sum = 0f;
                faceAreas[tri.FaceName] = sum + area;

                crystal.Triangles.Add(new Tri
                {
                    A = A,
                    B = B,
                    C = C,
                    Normal = normal,
                    FaceName = tri.FaceName,
                    FaceArea = area,
                    FaceColor = NameToColor(tri.FaceName)
                });
            }

            float maxFaceArea = faceAreas.Count > 0 ? faceAreas.Values.Max() : 0f;
            if (maxFaceArea > 1e-9f)
            {
                foreach (var t in crystal.Triangles)
                    t.FaceArea = faceAreas[t.FaceName] / maxFaceArea;
            }

            float r2 = 0f;
            foreach (var tri in crystal.Triangles)
            {
                r2 = Math.Max(r2, tri.A.LengthSquared());
                r2 = Math.Max(r2, tri.B.LengthSquared());
                r2 = Math.Max(r2, tri.C.LengthSquared());
            }
            crystal.BoundingRadius = (float)Math.Sqrt(r2);

            LogSuccess($"[Build] ✅ END: {crystal.Triangles.Count} tris, R={crystal.BoundingRadius:F3} Å");

            return crystal;
        }

        public static MorphologyCrystal BuildSimpleTest()
        {
            var crystal = new MorphologyCrystal();

            float boxR = 2.0f;
            var poly = ConvexPolyhedronFrac.FromBox(boxR);

            float d = 1.0f;

            var faces = new List<(int h, int k, int l, string name)>
            {
                (1, 0, 0, "(100)"), (-1, 0, 0, "(-100)"),
                (0, 1, 0, "(010)"), (0, -1, 0, "(0-10)"),
                (0, 0, 1, "(001)"), (0, 0, -1, "(00-1)"),
                (1, 1, 0, "(110)"), (1, -1, 0, "(1-10)"),
                (-1, 1, 0, "(-110)"), (-1, -1, 0, "(-1-10)"),
                (1, 0, 1, "(101)"), (1, 0, -1, "(10-1)"),
                (-1, 0, 1, "(-101)"), (-1, 0, -1, "(-10-1)"),
                (0, 1, 1, "(011)"), (0, 1, -1, "(01-1)"),
                (0, -1, 1, "(0-11)"), (0, -1, -1, "(0-1-1)"),
                (1, 1, 1, "(111)"), (1, 1, -1, "(11-1)"),
                (1, -1, 1, "(1-11)"), (1, -1, -1, "(1-1-1)"),
                (-1, 1, 1, "(-111)"), (-1, 1, -1, "(-11-1)"),
                (-1, -1, 1, "(-1-11)"), (-1, -1, -1, "(-1-1-1)")
            };

            foreach (var (h, k, l, name) in faces)
            {
                float norm = (float)Math.Sqrt(h * h + k * k + l * l);
                float dScaled = d * norm;

                var backup = poly.Triangles;
                poly.ClipByPlane(h, k, l, dScaled, name);

                if (poly.Triangles.Count == 0)
                    poly.Triangles = new List<ConvexPolyhedronFrac.Tri>(backup);
            }

            Matrix fracToCart = Matrix.Identity;

 // ⭐ W BuildSimpleTest also buildsmy mapę FaceName → normalna
 // z list planes (choć tutaj all są already w Cartesian)
            var faceNormals = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            foreach (var (h, k, l, name) in faces)
            {
                Vector3 n = new Vector3(h, k, l);
                if (n.LengthSquared() > 1e-9f)
                    n.Normalize();
                faceNormals[name] = n;
            }

            foreach (var tri in poly.Triangles)
            {
                Vector3 A = Vector3.TransformNormal(tri.A, fracToCart);
                Vector3 B = Vector3.TransformNormal(tri.B, fracToCart);
                Vector3 C = Vector3.TransformNormal(tri.C, fracToCart);

 // ⭐ Dokładna normalna z mapy
                Vector3 normal;
                if (!string.IsNullOrEmpty(tri.FaceName) &&
                    faceNormals.TryGetValue(tri.FaceName, out var exactNormal))
                {
                    normal = exactNormal;
                }
                else
                {
                    normal = Vector3.Cross(B - A, C - A);
                    if (normal.LengthSquared() > 1e-9f)
                        normal.Normalize();
                    else
                        normal = Vector3.Zero;
                }

                crystal.Triangles.Add(new Tri
                {
                    A = A,
                    B = B,
                    C = C,
                    Normal = normal,
                    FaceName = tri.FaceName,
                    FaceArea = 1f,
                    FaceColor = NameToColor(tri.FaceName)
                });
            }

            float r2 = 0f;
            foreach (var tri in crystal.Triangles)
            {
                r2 = Math.Max(r2, tri.A.LengthSquared());
                r2 = Math.Max(r2, tri.B.LengthSquared());
                r2 = Math.Max(r2, tri.C.LengthSquared());
            }
            crystal.BoundingRadius = (float)Math.Sqrt(r2);

            return crystal;
        }

 /// <summary>
 /// ⭐ Kolor for faces — deterministyczny hash FNV-1a z nazwy.
 /// Każda ściana (każda nazwa) ma own colour.
 /// Without gruafterwania after "rodzinach" — to wymaga correctj symmetry krystalograficznej.
 /// </summary>
        public static Color NameToColor(string name)
        {
            if (string.IsNullOrEmpty(name))
                return new Color(128, 128, 128, 220);

 // cell-* (sześcian startowy) — szary
            if (name.StartsWith("cell-"))
                return new Color(150, 150, 150, 180);

            unchecked
            {
 // FNV-1a — stabilny between uruchomieniami (GetHashCode NIE jest)
                uint hash = 2166136261u;
                foreach (char c in name)
                {
                    hash ^= c;
                    hash *= 16777619u;
                }

 // ⭐ 1024 kubełki — small ryzyko kolizji przy ~50 ścianach
                float hue = (hash % 1024u) / 1024f;
                return HsvToRgb(hue, 0.65f, 0.95f, 220);
            }
        }

        private static Color HsvToRgb(float h, float s, float v, byte a)
        {
            h = h - (float)Math.Floor(h);
            float r, g, b;
            int i = (int)(h * 6);
            float f = h * 6 - i;
            float p = v * (1 - s);
            float q = v * (1 - f * s);
            float t = v * (1 - (1 - f) * s);

            switch (i % 6)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                default: r = v; g = p; b = q; break;
            }

            return new Color((byte)(r * 255), (byte)(g * 255), (byte)(b * 255), a);
        }
    }
}