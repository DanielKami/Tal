// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: Afterlihedrals.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tal.DrawMethods
{
 /// <summary>
 /// Provides functionality for detecting and rendering coordination afterlyhedra
 /// around metal centers in molecular structures.
 ///
 /// ⭐ Nowa version:
 /// - Rozafterznaje ligandy N, O, F, S, Cl, Br, I
 /// - Uses convex hull (QuickHull 3D) to triangulate an arbitrary polyhedron
 /// - Afterprawne normalne (outward from center)
 /// - Klasyfikuje typ polyhedron (tetraedr, oktaedr, piramida, pryzma...)
 /// - Fallback do starej triangulacji (fan) when convex hull zawiedzie
 /// </summary>
    public static class Polihedrals
    {
        #region Nested Types

        public struct VertexStruct
        {
            public Vector3 position;
            public Vector3 color;
            public VertexPositionNormalTexture[] vertices_n;
            public VertexPositionNormalTexture[] vertices_s;
            public PolyhedronType Type;      // ⭐ nowe
            public int LigandCount;           // ⭐ nowe
        }

        public struct PolihedralsStruct
        {
            public List<int> PolichedralAtoms;
            public int Nr_centralAtom;
            public Vector3 Center;
        }

 /// <summary>
 /// Rozafterznany typ polyhedron (do reafterrtu / debug).
 /// </summary>
        public enum PolyhedronType
        {
            Unknown,
            Linear,         // 2 ligandy
            TrigonalPlanar, // 3 ligandy
            Tetrahedral,    // 4 ligandy
            TrigonalBipyramidal, // 5 ligands (2 axial + 3 equatorial)
            SquarePyramidal,     // 5 ligands (1 apical + 4 base)
            Octahedral,     // 6 ligands
            PentagonalBipyramidal, // 7
            SquareAntiprismatic,   // 8
            Cubic,          // 8
            Other
        }

        #endregion

        #region Public Fields

        public static List<VertexStruct> VertexesList = new List<VertexStruct>();
        public static List<PolihedralsStruct> PolihedralsList = new List<PolihedralsStruct>();
        public static Molecule CurrentMolecule { get; set; }

 /// <summary>
 /// Atoms treated as ligands. Default: N, O, F, S, Cl, Br, I.
 /// Can be modified at runtime.
 /// </summary>
        public static HashSet<int> LigandAtomicNumbers = new HashSet<int>
        {
            7, 8, 9, 16, 17, 35, 53   // N, O, F, S, Cl, Br, I
        };

        #endregion

        #region Public Methods

        public static void FindPolihedral()
        {
            VertexesList.Clear();
            PolihedralsList.Clear();

            if (CurrentMolecule == null || CurrentMolecule.atom == null)
                return;

            int atomCount = CurrentMolecule.NumberOfAtoms;

            System.Diagnostics.Debug.WriteLine(
                $"[Polihedrals] Scanning {atomCount} atoms, " +
                $"Poli_dyst={Flags.Instance.Poli_dyst}");

 // ⭐ Faza 1: find centra metali i ligandy
            for (int i = 0; i < atomCount; i++)
            {
                int nu = CurrentMolecule.atom[i].AtomicNumber;

                if (!IsMetal(nu))
                    continue;

                var storage = new PolihedralsStruct
                {
                    PolichedralAtoms = new List<int>()
                };

 // Ligandy around metalu
                for (int j = 0; j < atomCount; j++)
                {
                    if (i == j) continue;

                    int z = CurrentMolecule.atom[j].AtomicNumber;
                    if (!LigandAtomicNumbers.Contains(z)) continue;

                    float dist = Vector3.Distance(
                        CurrentMolecule.atom[j].Coordinate,
                        CurrentMolecule.atom[i].Coordinate);

                    if (dist < Flags.Instance.Poli_dyst)
                    {
                        storage.PolichedralAtoms.Add(j);
                    }
                }

 // Min. 4 ligandy
                if (storage.PolichedralAtoms.Count < 4)
                    continue;

 // Centre = metal position (not the ligand average!)
 // ⭐ This matters: for a pyramid the apical ligand pulls the mean away from the metal
                storage.Center = CurrentMolecule.atom[i].Coordinate;
                storage.Nr_centralAtom = nu;

                PolihedralsList.Add(storage);

                System.Diagnostics.Debug.WriteLine(
                    $"[Polihedrals] Metal at idx={i} ({CurrentMolecule.atom[i].name}, Z={nu}) " +
                    $"has {storage.PolichedralAtoms.Count} ligands");
            }

 // ⭐ Phase 2: build geometry
            for (int i = 0; i < PolihedralsList.Count; i++)
            {
                VertexStruct primitive = new VertexStruct();
                PolihedralsStruct vertices = PolihedralsList[i];

                primitive.color = VdwParameters.color[vertices.Nr_centralAtom];
                primitive.position = vertices.Center;
                primitive.LigandCount = vertices.PolichedralAtoms.Count;

 // ⭐ Rozafterznaj typ
                primitive.Type = ClassifyPolyhedron(vertices);

 // ⭐ Buduj mesh (convex hull or fallback)
                BuildPolyhedronMesh(vertices, ref primitive);

                VertexesList.Add(primitive);

                System.Diagnostics.Debug.WriteLine(
                    $"[Polihedrals] Polyhedron #{i}: type={primitive.Type}, " +
                    $"ligands={primitive.LigandCount}, " +
                    $"triangles={primitive.vertices_n.Length / 3}");
            }

            System.Diagnostics.Debug.WriteLine(
                $"[Polihedrals] DONE: {PolihedralsList.Count} polyhedra detected");
        }

        public static void Clear()
        {
            VertexesList.Clear();
            PolihedralsList.Clear();
        }

        public static int GetPolyhedronCount() => PolihedralsList.Count;
        public static int GetVertexCount() => VertexesList.Count;

        #endregion

        #region Private — Detection

        private static bool IsMetal(int atomicNumber)
        {
            return atomicNumber == 3 ||
                   atomicNumber >= 11 && atomicNumber <= 14 ||
                   atomicNumber >= 19 && atomicNumber <= 30 ||
                   atomicNumber >= 37 && atomicNumber <= 50 ||
                   atomicNumber >= 55 && atomicNumber <= 83 ||
                   atomicNumber >= 87 && atomicNumber <= 118;
        }

 /// <summary>
 /// ⭐ Rozafterznaje typ polyhedron na based on liczby i geometrii ligands.
 /// </summary>
        private static PolyhedronType ClassifyPolyhedron(PolihedralsStruct vertices)
        {
            int n = vertices.PolichedralAtoms.Count;

            if (n < 4) return PolyhedronType.Unknown;
            if (n == 4) return PolyhedronType.Tetrahedral;
            if (n == 6) return PolyhedronType.Octahedral;
            if (n == 7) return PolyhedronType.PentagonalBipyramidal;
            if (n == 8) return PolyhedronType.SquareAntiprismatic;

 // For n=5: distinguish trigonal bipyramidal vs square pyramidal
            if (n == 5)
            {
                return Classify5Coordinate(vertices);
            }

            return PolyhedronType.Other;
        }

 /// <summary>
 /// For 5 ligands: check czy to bipiramida trygonalna czy piramida kwadratowa.
 ///
 /// Metoda: find wektory od centrum do ligands, znormalise.
 /// - Trigonal bipyramidal: 2 ligands are "opposite" (angle ~180°), 3 in plane.
 /// - Square pyramidal: 1 "apical" ligand (axis), 4 in plane.
 /// </summary>
        private static PolyhedronType Classify5Coordinate(PolihedralsStruct vertices)
        {
            var dirs = new List<Vector3>();
            Vector3 center = vertices.Center;

            foreach (int idx in vertices.PolichedralAtoms)
            {
                Vector3 d = Vector3.Normalize(
                    CurrentMolecule.atom[idx].Coordinate - center);
                dirs.Add(d);
            }

 // Count pairs with angle > 150° (nearly opposite)
            int oppositePairs = 0;
            for (int i = 0; i < dirs.Count; i++)
            {
                for (int j = i + 1; j < dirs.Count; j++)
                {
                    float dot = Vector3.Dot(dirs[i], dirs[j]);
                    if (dot < -0.9f) // angle > ~155°
                        oppositePairs++;
                }
            }

 // Trigonal bipyramidal: 1 para przeciwna (2 osiowe)
 // Square pyramidal: 0 opposite pairs (all roughly in one hemisphere)
            return oppositePairs >= 1
                ? PolyhedronType.TrigonalBipyramidal
                : PolyhedronType.SquarePyramidal;
        }

        #endregion

        #region Private — Mesh Building

        private static void BuildPolyhedronMesh(PolihedralsStruct vertices, ref VertexStruct primitive)
        {
            int n = vertices.PolichedralAtoms.Count;
            if (n < 4) return;

 // Zbierz positions ligands relative to centrum
            var points = new List<Vector3>(n);
            foreach (int idx in vertices.PolichedralAtoms)
            {
                points.Add(CurrentMolecule.atom[idx].Coordinate - vertices.Center);
            }

 // ⭐ Convex hull (QuickHull)
            List<(int, int, int)> triangles = ConvexHull3D.Compute(points);

            if (triangles == null || triangles.Count < 4)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Polihedrals] ⚠️ ConvexHull failed, falling back to legacy triangulation");

                triangles = LegacyTriangulatePolygon(vertices);
            }

 // Alokuj
            primitive.vertices_n = new VertexPositionNormalTexture[triangles.Count * 3];
            primitive.vertices_s = new VertexPositionNormalTexture[triangles.Count * 3];

 // Fill
            FillTriangleVertices(points, triangles, ref primitive);
        }

 /// <summary>
 /// Fills the vertex arrays with triangles from the convex hull.
 /// Normals always face outside (outward from centre — i.e. from (0,0,0) in local space).
 /// </summary>
        private static void FillTriangleVertices(
            List<Vector3> points,
            List<(int, int, int)> triangles,
            ref VertexStruct primitive)
        {
            for (int k = 0; k < triangles.Count; k++)
            {
                int i0 = triangles[k].Item1;
                int i1 = triangles[k].Item2;
                int i2 = triangles[k].Item3;
                int k3 = 3 * k;

                Vector3 p0 = points[i0];
                Vector3 p1 = points[i1];
                Vector3 p2 = points[i2];

 // ⭐ Normalna — always na outside.
 // Convex hull returns triangles CCW when viewed from outside,
 // so cross(p1-p0, p2-p0) wskazuje na outside.
                Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);
                float len = normal.Length();
                if (len < 1e-6f) continue;   // degenerate
                normal /= len;

 // ⭐ Ensure the normal is "outward":
 // rzut wektora od centre (0,0,0) do centre triangle
 // must agree with the normal
                Vector3 centroid = (p0 + p1 + p2) / 3f;
                if (Vector3.Dot(normal, centroid) < 0)
                {
 // Reverse vertex order
                    normal = -normal;
                    (i1, i2) = (i2, i1);
                    (p1, p2) = (p2, p1);
                }

 // Front face
                primitive.vertices_n[k3 + 0] = new VertexPositionNormalTexture
                {
                    Position = p0,
                    Normal = normal,
                    TextureCoordinate = Vector2.Zero
                };
                primitive.vertices_n[k3 + 1] = new VertexPositionNormalTexture
                {
                    Position = p1,
                    Normal = normal,
                    TextureCoordinate = Vector2.Zero
                };
                primitive.vertices_n[k3 + 2] = new VertexPositionNormalTexture
                {
                    Position = p2,
                    Normal = normal,
                    TextureCoordinate = Vector2.Zero
                };

 // Back face — reversed order, opposite normal
                primitive.vertices_s[k3 + 0] = new VertexPositionNormalTexture
                {
                    Position = p0,
                    Normal = -normal,
                    TextureCoordinate = Vector2.Zero
                };
                primitive.vertices_s[k3 + 1] = new VertexPositionNormalTexture
                {
                    Position = p2,
                    Normal = -normal,
                    TextureCoordinate = Vector2.Zero
                };
                primitive.vertices_s[k3 + 2] = new VertexPositionNormalTexture
                {
                    Position = p1,
                    Normal = -normal,
                    TextureCoordinate = Vector2.Zero
                };
            }
        }

 /// <summary>
 /// Fallback — old triangulation (fan). Used only when convex hull fails.
 /// </summary>
        private static List<(int, int, int)> LegacyTriangulatePolygon(PolihedralsStruct vertices)
        {
            var result = new List<(int, int, int)>();
            int n = vertices.PolichedralAtoms.Count;

            if (n < 4) return result;

 // Sort around centre
            Vector3 center = vertices.Center;
            var sorted = SortVerticesAroundCenter(vertices);

 // Fan
            for (int i = 1; i < n - 1; i++)
            {
                result.Add((sorted[0], sorted[i], sorted[i + 1]));
            }

            return result;
        }

        private static List<int> SortVerticesAroundCenter(PolihedralsStruct vertices)
        {
            int count = vertices.PolichedralAtoms.Count;
            var indices = new List<int>(count);
            for (int i = 0; i < count; i++) indices.Add(i);

            Vector3 center = vertices.Center;
            Vector3 refDir = Vector3.Normalize(
                CurrentMolecule.atom[vertices.PolichedralAtoms[0]].Coordinate - center);
            Vector3 refDir2 = Vector3.Normalize(
                CurrentMolecule.atom[vertices.PolichedralAtoms[1]].Coordinate - center);

            indices.Sort((a, b) =>
            {
                Vector3 va = Vector3.Normalize(
                    CurrentMolecule.atom[vertices.PolichedralAtoms[a]].Coordinate - center);
                Vector3 vb = Vector3.Normalize(
                    CurrentMolecule.atom[vertices.PolichedralAtoms[b]].Coordinate - center);

                float angleA = GetAngle(va, refDir, refDir2);
                float angleB = GetAngle(vb, refDir, refDir2);
                return angleA.CompareTo(angleB);
            });

            return indices;
        }

        private static float GetAngle(Vector3 v, Vector3 refDir, Vector3 refDir2)
        {
            float dot = Vector3.Dot(v, refDir);
            float angle = (float)Math.Acos(Math.Clamp(dot / v.Length(), -1.0f, 1.0f));
            Vector3 cross = Vector3.Cross(refDir, v);
            if (Vector3.Dot(cross, refDir2) < 0) angle = -angle;
            return angle;
        }

        #endregion
    }
}