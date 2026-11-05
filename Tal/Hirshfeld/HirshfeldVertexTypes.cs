// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldVertexTypes.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Vertex typu Position + Normal + Color (Vector4) — dedykowany for
 /// meshu surface Hirshfelda. Afterzwala na lighting per-pixel
 /// (BasicEffect.PreferPerPixelLighting) + colour per-vertex
 /// (d_norm / d_e / d_i or own colour bazowy).
 ///
 /// Layout: Position (12B) | Normal (12B) | Color (16B) = 40 bytes.
 /// </summary>
    public struct VertexPositionNormalColor4 : IVertexType
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 Color;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(24, VertexElementFormat.Vector4, VertexElementUsage.Color, 0)
        );

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public VertexPositionNormalColor4(Vector3 position, Vector3 normal, Vector4 color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }

        public const int SizeInBytes = 40;
    }
}