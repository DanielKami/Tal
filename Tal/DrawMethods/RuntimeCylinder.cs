// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: RuntimeCylinder.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Runtime generator cylindra (rod) for MonoGame 3.8.
// Nie wymaga pliku rod.X — creates Model w memory przy starcie.
//
// ⭐ Kompatybilny z oryginalnym rod.X:
// - Base length = 39.370080 (as original after FrameTransformMatrix)
// - Along the -X axis (from X=0 to X=-BASE_LENGTH)
// - Radius bazowy = RuntimeIcosphere.BASE_RADIUS (1.0)
// - Without FrameTransformMatrix (identity)
//
// Usage (in GameWindow.InitializeGraphics):
// _smallRod = RuntimeCylinder.Create(_graphicsDevice, segments: 24, capped: true);

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tal.DrawMethods
{
    public static class RuntimeCylinder
    {
        #region Constants

 /// <summary>
 /// ⭐ Length bazowa — odafterwiada oryginalnemu rod.X (39.370080 = 1 metr w calach).
 /// DO NOT CHANGE — Draw.cs assumes this value via stickFactor.
 /// </summary>
        public const float BASE_LENGTH = -39.370080f;

 /// <summary>
 /// ⭐ Base radius — consistent with RuntimeIcosphere.BASE_RADIUS.
 /// </summary>
        public const float BASE_RADIUS = RuntimeIcosphere.BASE_RADIUS;

 /// <summary>Default number of segments around the axis.</summary>
        public const int DEFAULT_SEGMENTS = 24;

        #endregion

        #region Public API

 /// <summary>
 /// ⭐ Creates a Model (MonoGame 3.8) representing a cylinder (stick).
 /// Identyczny interfejs jak RuntimeIcosphere.Create.
 /// </summary>
 /// <param name="device">GraphicsDevice.</param>
 /// <param name="segments">Number of segments around the axis (min 3).</param>
 /// <param name="capped">Whether to close the ends with caps.</param>
        public static Model Create(GraphicsDevice device, int segments = DEFAULT_SEGMENTS, bool capped = true)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (segments < 3) segments = 3;

            var (positions, normals, indices) = BuildCylinder(segments, capped);

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
 // 6. ModelMesh — konstruktor setia part.parent = this
 // ═══════════════════════════════════════════════════════════
            var mesh = new ModelMesh(device, new List<ModelMeshPart> { part })
            {
                Name = "Rod",
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

 // Afterbond mesha z bone
            rootBone.AddMesh(mesh);

            return model;
        }

        #endregion

        #region Cylinder construction

        private static (Vector3[] positions, Vector3[] normals, int[] indices)
            BuildCylinder(int segments, bool capped)
        {
 // ═══════════════════════════════════════════════════════════
 // Geometria:
 // - 2 rings of `segments` vertices (side faces)
 // - 2 centres (caps) if capped
 //
 // Konwencja -X: cylinder idzie od X = 0 do X = -BASE_LENGTH
 // ═══════════════════════════════════════════════════════════

            int ringVerts = segments * 2;
            int capVerts = capped ? 2 : 0;
            int totalVerts = ringVerts + capVerts;

            var positions = new Vector3[totalVerts];
            var normals = new Vector3[totalVerts];

            int vi = 0;

            float x1 = 0f;
            float x2 = -BASE_LENGTH;

 // ═══ Rings ═══
            for (int i = 0; i < segments; i++)
            {
                float theta = MathHelper.TwoPi * i / segments;
                float cosT = (float)Math.Cos(theta);
                float sinT = (float)Math.Sin(theta);

                Vector3 normal = new Vector3(0f, cosT, sinT);

 // Ring 1 (X = 0)
                positions[vi] = new Vector3(x1, cosT * BASE_RADIUS, sinT * BASE_RADIUS);
                normals[vi] = normal;
                vi++;

 // Ring 2 (X = -BASE_LENGTH)
                positions[vi] = new Vector3(x2, cosT * BASE_RADIUS, sinT * BASE_RADIUS);
                normals[vi] = normal;
                vi++;
            }

 // ═══ Kapelusze ═══
            int capStartIndex = vi;
            if (capped)
            {
 // Centre kapelusza 1 (X = 0), normalna +X
                positions[vi] = new Vector3(x1, 0f, 0f);
                normals[vi] = new Vector3(1f, 0f, 0f);
                vi++;

 // Centre kapelusza 2 (X = -BASE_LENGTH), normalna -X
                positions[vi] = new Vector3(x2, 0f, 0f);
                normals[vi] = new Vector3(-1f, 0f, 0f);
                vi++;
            }

 // ═══ Indeksy ═══
            int sideIndices = segments * 6;
            int capIndices = capped ? segments * 6 : 0;
            var indices = new int[sideIndices + capIndices];
            int ii = 0;

 // ─── Side faces ───
 // Each segment connects 2 vertices of ring 1 (i, i+1)
 // with 2 vertices of ring 2 (i, i+1).
 //
 // ⚠️ Winding: CCW when viewed from outside the cylinder
 // (MonoGame by default CullCounterClockwiseFace)
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;

                int v1a = i * 2;            // pierścień 1, kąt i
                int v1b = next * 2;         // pierścień 1, kąt i+1
                int v2a = i * 2 + 1;        // pierścień 2, kąt i
                int v2b = next * 2 + 1;     // pierścień 2, kąt i+1

 // Triangle 1
                indices[ii++] = v1a;
                indices[ii++] = v2a;
                indices[ii++] = v1b;

 // Triangle 2
                indices[ii++] = v1b;
                indices[ii++] = v2a;
                indices[ii++] = v2b;
            }

 // ─── Kapelusze ───
            if (capped)
            {
                int center1 = capStartIndex;         // środek pierścienia 1 (X=0)
                int center2 = capStartIndex + 1;     // środek pierścienia 2 (X=-L)

                for (int i = 0; i < segments; i++)
                {
                    int next = (i + 1) % segments;

                    int v1a = i * 2;
                    int v1b = next * 2;
                    int v2a = i * 2 + 1;
                    int v2b = next * 2 + 1;

 // Cap 1 (X = 0), viewed from +X — CCW
                    indices[ii++] = center1;
                    indices[ii++] = v1b;
                    indices[ii++] = v1a;

 // Cap 2 (X = -L), viewed from -X — CCW
                    indices[ii++] = center2;
                    indices[ii++] = v2a;
                    indices[ii++] = v2b;
                }
            }

            return (positions, normals, indices);
        }

        #endregion
    }
}