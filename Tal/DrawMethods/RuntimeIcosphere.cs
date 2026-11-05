// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: RuntimeIcosphere.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Runtime generator kul icosphere for MonoGame 3.8.
// Nie wymaga files .X — creates Model w memory przy starcie.
//
// Usage:
// var ball = RuntimeIcosphere.Create(graphicsDevice, subdivisionLevel: 3, radius: 3.937008f);
//
// Levely subdywizji:
// 0 → 12 verts, 20 tris.
// 1 → 42 verts, 80 tris.
// 2 → 162 verts, 320 tris.
// 3 → 642 verts, 1280 tris.
// 4 → 2562 verts, 5120 tris.
// 5 → 10242 verts, 20480 tris.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tal.DrawMethods
{
    public static class RuntimeIcosphere
    {
        #region Constants

 /// <summary>
 /// Radius bazowy zgodny z existcym ball.X (DirectX units).
 /// W kodzie skalowany przez 
 /// </summary>
 //public const float BASE_RADIUS = 4.37f;
        public const float BASE_RADIUS = 1f;
 /// <summary>Maksymalny level subdywizji (5 → 10242 vertices).</summary>
        public const int MAX_LEVEL = 5;

        #endregion

        #region Public API

 /// <summary>
 /// Creates a Model (MonoGame 3.8) representing an icosphere.
 /// </summary>
 /// <param name="device">GraphicsDevice.</param>
 /// <param name="subdivisionLevel">0..5.</param>
 /// <param name="radius">Radius kuli w jednostkach modelu.</param>
        public static Model Create(GraphicsDevice device, int subdivisionLevel, float radius)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (subdivisionLevel < 0 || subdivisionLevel > MAX_LEVEL)
                throw new ArgumentOutOfRangeException(nameof(subdivisionLevel),
                    $"Subdivision level must be in [0, {MAX_LEVEL}].");
            if (radius <= 0f)
                throw new ArgumentOutOfRangeException(nameof(radius),
                    "Radius must be positive.");

            var (positions, normals, indices) = BuildIcosphere(subdivisionLevel, radius);

 // ═══════════════════════════════════════════════════════════
 // 1. VertexBuffer
 // ═══════════════════════════════════════════════════════════
            var vertexBuffer = new VertexBuffer(
                device,
                VertexPositionNormalTexture.VertexDeclaration,
                positions.Length,
                BufferUsage.None);

            var vertexData = new VertexPositionNormalTexture[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                vertexData[i] = new VertexPositionNormalTexture(
                    positions[i],
                    normals[i],
                    Vector2.Zero);
            }
            vertexBuffer.SetData(vertexData);

 // ═══════════════════════════════════════════════════════════
 // 2. IndexBuffer (16-bit or 32-bit)
 // ═══════════════════════════════════════════════════════════
            bool use32Bit = positions.Length > 65535;

            var indexBuffer = new IndexBuffer(
                device,
                use32Bit ? IndexElementSize.ThirtyTwoBits : IndexElementSize.SixteenBits,
                indices.Length,
                BufferUsage.None);

            if (use32Bit)
            {
                indexBuffer.SetData(indices);
            }
            else
            {
                var shortIndices = new short[indices.Length];
                for (int i = 0; i < indices.Length; i++)
                    shortIndices[i] = (short)indices[i];
                indexBuffer.SetData(shortIndices);
            }

 // ═══════════════════════════════════════════════════════════
 // 3. BasicEffect (default efekt)
 // ═══════════════════════════════════════════════════════════
            var effect = new BasicEffect(device)
            {
                VertexColorEnabled = false,
                TextureEnabled = false,
                LightingEnabled = true,
                PreferPerPixelLighting = true,
            };
            effect.EnableDefaultLighting();

 // ═══════════════════════════════════════════════════════════
 // 4. ModelMeshPart — BEZ Effect (parent jest still null)
 // ═══════════════════════════════════════════════════════════
            var part = new ModelMeshPart
            {
                VertexBuffer = vertexBuffer,
                IndexBuffer = indexBuffer,
                NumVertices = positions.Length,
                PrimitiveCount = indices.Length / 3,
                StartIndex = 0,
                VertexOffset = 0,
            };

 // ═══════════════════════════════════════════════════════════
 // 5. ModelBone (root)
 // ═══════════════════════════════════════════════════════════
            var rootBone = new ModelBone
            {
                Name = "Root",
                Index = 0,
                Transform = Matrix.Identity,
            };

 // ═══════════════════════════════════════════════════════════
 // 6. ModelMesh — tu konstruktor setia part.parent = this
 // ═══════════════════════════════════════════════════════════
            var mesh = new ModelMesh(device, new List<ModelMeshPart> { part })
            {
                Name = "Sphere",
                ParentBone = rootBone,
            };

 // ═══════════════════════════════════════════════════════════
 // 7. Dopiero TERAZ setiamy Effect (parent already exists)
 // ═══════════════════════════════════════════════════════════
            part.Effect = effect;

 // ═══════════════════════════════════════════════════════════
 // 8. Model — MonoGame 3.8 ma public konstruktor
 // ═══════════════════════════════════════════════════════════
            var model = new Model(
                device,
                new List<ModelBone> { rootBone },
                new List<ModelMesh> { mesh });

            model.Root = rootBone;

 // Opcjonalnie, ale zalecane — afterbond mesha z bone
            rootBone.AddMesh(mesh);

            return model;
        }

        #endregion

        #region Icosphere construction

        private static (Vector3[] positions, Vector3[] normals, int[] indices)
            BuildIcosphere(int level, float radius)
        {
 // ═══════════════════════════════════════════════════════════
 // Base: icosahedron with 12 vertices
 //
 // 12 vertices are the vertices of 3 orthogonal golden rectangles:
 // (-1, φ, 0), ( 1, φ, 0), (-1, -φ, 0), ( 1, -φ, 0)
 // ( 0, -1, φ), ( 0, 1, φ), ( 0, -1, -φ), ( 0, 1, -φ)
 // ( φ, 0, -1), ( φ, 0, 1), (-φ, 0, -1), (-φ, 0, 1)
 //
 // where φ = (1 + √5) / 2 is the golden ratio.
 // ═══════════════════════════════════════════════════════════

            float φ = (1f + MathF.Sqrt(5f)) / 2f;

            var verts = new List<Vector3>
            {
                new Vector3(-1,  φ,  0),   // 0
                new Vector3( 1,  φ,  0),   // 1
                new Vector3(-1, -φ,  0),   // 2
                new Vector3( 1, -φ,  0),   // 3
                new Vector3( 0, -1,  φ),   // 4
                new Vector3( 0,  1,  φ),   // 5
                new Vector3( 0, -1, -φ),   // 6
                new Vector3( 0,  1, -φ),   // 7
                new Vector3( φ,  0, -1),   // 8
                new Vector3( φ,  0,  1),   // 9
                new Vector3(-φ,  0, -1),   // 10
                new Vector3(-φ,  0,  1),   // 11
            };

 // 20 triangles ikosaedru — kanoniczna list (CCW z outside)
            var tris = new List<(int a, int b, int c)>
            {
 // 5 triangles around the north pole (vertex 0)
                (0, 11,  5),
                (0,  5,  1),
                (0,  1,  7),
                (0,  7, 10),
                (0, 10, 11),

 // 5 triangles around bieguna dolnego (vertex 3)
                (3,  9,  4),
                (3,  4,  2),
                (3,  2,  6),
                (3,  6,  8),
                (3,  8,  9),

 // Middle band (10 triangles)
                ( 5, 11,  4), ( 4, 11,  2),
                ( 5,  4,  9), ( 5,  9,  1),
                ( 1,  9,  8), ( 1,  8,  7),
                ( 7,  8,  6), ( 7,  6, 10),
                (10,  6,  2), (10,  2, 11),
            };

 // Normalise bazowe vertices do sfery jednostkowej
            for (int i = 0; i < verts.Count; i++)
                verts[i] = Vector3.Normalize(verts[i]);

 // ═══════════════════════════════════════════════════════════
 // Subdywizja — each triangle dzielimy na 4
 // ═══════════════════════════════════════════════════════════
            for (int s = 0; s < level; s++)
            {
                var newTris = new List<(int a, int b, int c)>(tris.Count * 4);
                var midCache = new Dictionary<(int, int), int>();

                int GetMid(int i1, int i2)
                {
                    var key = i1 < i2 ? (i1, i2) : (i2, i1);
                    if (midCache.TryGetValue(key, out int cached))
                        return cached;

                    Vector3 mid = Vector3.Normalize((verts[i1] + verts[i2]) * 0.5f);
                    verts.Add(mid);
                    int idx = verts.Count - 1;
                    midCache[key] = idx;
                    return idx;
                }

                foreach (var (a, b, c) in tris)
                {
                    int ab = GetMid(a, b);
                    int bc = GetMid(b, c);
                    int ca = GetMid(c, a);

                    newTris.Add((a, ab, ca));
                    newTris.Add((ab, b, bc));
                    newTris.Add((ca, bc, c));
                    newTris.Add((ab, bc, ca));
                }

                tris = newTris;
            }

 // ═══════════════════════════════════════════════════════════
 // Scale do radius i zbuduj tablice
 // ═══════════════════════════════════════════════════════════
            var positions = new Vector3[verts.Count];
            var normals = new Vector3[verts.Count];

            for (int i = 0; i < verts.Count; i++)
            {
                positions[i] = verts[i] * radius;
                normals[i] = verts[i];   // na sferze jednostkowej normalna = pozycja
            }

            var indices = new int[tris.Count * 3];
            for (int i = 0; i < tris.Count; i++)
            {
 // REVERSED winding (CW → CCW or vice versa)
 // MonoGame by default: CullCounterClockwise
                indices[i * 3 + 0] = tris[i].a;
                indices[i * 3 + 1] = tris[i].c;   // było b
                indices[i * 3 + 2] = tris[i].b;   // było c
            }

            return (positions, normals, indices);
        }

        #endregion
    }
}