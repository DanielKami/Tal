// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Runtime cone for force-arrow heads.
// Same axis as RuntimeCylinder: base at X = 0, tip at X = -BASE_LENGTH
// (BASE_LENGTH is negative, so the tip is on +X, where the rod actually ends).

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tal.DrawMethods
{
    public static class RuntimeCone
    {
        public const float BASE_LENGTH = RuntimeCylinder.BASE_LENGTH; // -39.370080
        public const float BASE_RADIUS = RuntimeIcosphere.BASE_RADIUS;

        public static Model Create(GraphicsDevice device, int segments = 24)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (segments < 3) segments = 3;

            var (positions, normals, indices) = BuildCone(segments);

            var vertexBuffer = new VertexBuffer(
                device, VertexPositionNormalTexture.VertexDeclaration,
                positions.Length, BufferUsage.None);

            var verts = new VertexPositionNormalTexture[positions.Length];
            for (int i = 0; i < positions.Length; i++)
                verts[i] = new VertexPositionNormalTexture(positions[i], normals[i], Vector2.Zero);
            vertexBuffer.SetData(verts);

            var indexBuffer = new IndexBuffer(
                device, IndexElementSize.SixteenBits, indices.Length, BufferUsage.None);
            var shorts = new short[indices.Length];
            for (int i = 0; i < indices.Length; i++) shorts[i] = (short)indices[i];
            indexBuffer.SetData(shorts);

            var effect = new BasicEffect(device)
            {
                VertexColorEnabled = false,
                TextureEnabled = false,
                LightingEnabled = true,
                PreferPerPixelLighting = true
            };
            effect.EnableDefaultLighting();

            var part = new ModelMeshPart
            {
                VertexBuffer = vertexBuffer,
                IndexBuffer = indexBuffer,
                NumVertices = positions.Length,
                PrimitiveCount = indices.Length / 3,
                StartIndex = 0,
                VertexOffset = 0
            };

            var rootBone = new ModelBone { Name = "Root", Index = 0, Transform = Matrix.Identity };
            var mesh = new ModelMesh(device, new List<ModelMeshPart> { part })
            {
                Name = "Cone",
                ParentBone = rootBone
            };
            part.Effect = effect;

            var model = new Model(
                device,
                new List<ModelBone> { rootBone },
                new List<ModelMesh> { mesh });
            model.Root = rootBone;
            rootBone.AddMesh(mesh);
            return model;
        }

        /// <summary>
        /// Cone along -X: base ring at X = 0, tip at X = -BASE_LENGTH.
        /// </summary>
        private static (Vector3[] positions, Vector3[] normals, int[] indices) BuildCone(int segments)
        {
            // base ring + tip + base centre
            int n = segments + 2;
            var positions = new Vector3[n];
            var normals = new Vector3[n];

            float xBase = 0f;
            // Match RuntimeCylinder: x2 = -BASE_LENGTH (positive X).
            float xTip = -BASE_LENGTH;
            float span = Math.Abs(xTip);
            float slant = (float)Math.Sqrt(BASE_RADIUS * BASE_RADIUS + span * span);

            for (int i = 0; i < segments; i++)
            {
                float th = MathHelper.TwoPi * i / segments;
                float c = (float)Math.Cos(th);
                float s = (float)Math.Sin(th);
                positions[i] = new Vector3(xBase, c * BASE_RADIUS, s * BASE_RADIUS);
                // outward normal of the side
                Vector3 side = Vector3.Normalize(new Vector3(-BASE_RADIUS, c * span, s * span));
                normals[i] = Vector3.Normalize(side);
            }
            int tip = segments;
            int centre = segments + 1;
            positions[tip] = new Vector3(xTip, 0f, 0f);
            normals[tip] = new Vector3(-1f, 0f, 0f);
            positions[centre] = new Vector3(xBase, 0f, 0f);
            normals[centre] = new Vector3(1f, 0f, 0f);

            var indices = new int[segments * 6];
            int ii = 0;
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                // side (CCW from outside)
                indices[ii++] = i;
                indices[ii++] = next;
                indices[ii++] = tip;
                // base cap
                indices[ii++] = centre;
                indices[ii++] = next;
                indices[ii++] = i;
            }
            return (positions, normals, indices);
        }
    }
}
