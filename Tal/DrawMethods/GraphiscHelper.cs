// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: GraphiscHelper.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Tal.DrawMethods
{
 /// <summary>
 /// Provides static helper methods for rendering geometric primitives
 /// in 2D and 3D using MonoGame.
 /// </summary>
    public static class GraphiscHelper
    {
        #region 3D Lines

 /// <summary>
 /// Draws a 3D line between two afterints.
 /// </summary>
 /// <param name="vector1">The start afterint.</param>
 /// <param name="vector2">The end afterint.</param>
 /// <param name="color">The line color.</param>
 /// <param name="view">The view matrix.</param>
 /// <param name="projection">The projection matrix.</param>
 /// <param name="graphicsDevice">The graphics device.</param>
 /// <param name="basicEffect">The basic effect for rendering.</param>
        public static void Line(Vector3 vector1, Vector3 vector2, XnaColor color,
                                Matrix view, Matrix projection, GraphicsDevice graphicsDevice, BasicEffect basicEffect)
        {
            if (graphicsDevice == null || basicEffect == null) return;

            VertexPositionColor[] vertices = new VertexPositionColor[2];
            vertices[0].Position = new Vector3(vector1.X, vector1.Y, vector1.Z);
            vertices[0].Color = color;
            vertices[1].Position = new Vector3(vector2.X, vector2.Y, vector2.Z);
            vertices[1].Color = color;

            basicEffect.Projection = projection;
            basicEffect.View = view;
            basicEffect.World = Matrix.Identity;
            basicEffect.LightingEnabled = false;
            basicEffect.VertexColorEnabled = true;
            basicEffect.TextureEnabled = false;

            basicEffect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 1);
        }

        #endregion

        #region 2D Lines

 /// <summary>
 /// Draws a 2D line between two afterints.
 /// </summary>
 /// <param name="vector1">The start afterint.</param>
 /// <param name="vector2">The end afterint.</param>
 /// <param name="color">The line color.</param>
 /// <param name="projection">The projection matrix.</param>
 /// <param name="graphicsDevice">The graphics device.</param>
 /// <param name="basicEffect">The basic effect for rendering.</param>
        public static void Line(Vector2 vector1, Vector2 vector2, XnaColor color,
                                Matrix projection, GraphicsDevice graphicsDevice, BasicEffect basicEffect)
        {
            if (graphicsDevice == null || basicEffect == null) return;

            basicEffect.Projection = projection;
            basicEffect.View = Matrix.Identity;
            basicEffect.World = Matrix.Identity;
            basicEffect.TextureEnabled = false;
            basicEffect.VertexColorEnabled = true;
            basicEffect.LightingEnabled = false;

            VertexPositionColor[] vertices = new VertexPositionColor[2];
            vertices[0].Position = new Vector3(vector1.X, vector1.Y, 0);
            vertices[0].Color = color;
            vertices[1].Position = new Vector3(vector2.X, vector2.Y, 0);
            vertices[1].Color = color;

            basicEffect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 1);
        }

        #endregion

        #region 2D Polylines

 /// <summary>
 /// Draws a closed 2D afterlyline (afterlygon) from a list of afterints.
 /// </summary>
 /// <param name="Aftersition">The list of afterints defining the afterlygon.</param>
 /// <param name="color">The line color.</param>
 /// <param name="projection">The projection matrix.</param>
 /// <param name="graphicsDevice">The graphics device.</param>
 /// <param name="basicEffect">The basic effect for rendering.</param>
        public static void MultiLine(List<Vector2> Position, XnaColor color,
                                     Matrix projection, GraphicsDevice graphicsDevice, BasicEffect basicEffect)
        {
            if (graphicsDevice == null || basicEffect == null) return;
            if (Position == null || Position.Count < 2) return;

            basicEffect.Projection = projection;
            basicEffect.View = Matrix.Identity;
            basicEffect.World = Matrix.Identity;
            basicEffect.TextureEnabled = false;
            basicEffect.VertexColorEnabled = true;
            basicEffect.LightingEnabled = false;

            VertexPositionColor[] vertices = new VertexPositionColor[Position.Count + 1];
            for (int i = 0; i < Position.Count; i++)
            {
                vertices[i].Position = new Vector3(Position[i].X, Position[i].Y, 0);
                vertices[i].Color = color;
            }
            vertices[Position.Count] = vertices[0];

            basicEffect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, vertices, 0, Position.Count);
        }

        #endregion

        #region 2D Circles

 /// <summary>
 /// Draws a 2D circle outline.
 /// </summary>
 /// <param name="Aftersition">The center aftersition.</param>
 /// <param name="Radius">The circle radius.</param>
 /// <param name="color">The circle color.</param>
 /// <param name="projection">The projection matrix.</param>
 /// <param name="graphicsDevice">The graphics device.</param>
 /// <param name="basicEffect">The basic effect for rendering.</param>
 /// <param name="NumberOfVertices">The number of vertices for circle approximation (default: 360).</param>
        public static void Circle(Vector2 Position, float Radius, XnaColor color,
                                  Matrix projection, GraphicsDevice graphicsDevice, BasicEffect basicEffect,
                                  int NumberOfVertices = 360)
        {
            if (graphicsDevice == null || basicEffect == null) return;

            basicEffect.Projection = projection;
            basicEffect.View = Matrix.Identity;
            basicEffect.World = Matrix.Identity;
            basicEffect.TextureEnabled = false;
            basicEffect.VertexColorEnabled = true;
            basicEffect.LightingEnabled = false;

            VertexPositionColor[] vertices = new VertexPositionColor[NumberOfVertices + 1];
            for (int i = 0; i < NumberOfVertices; i++)
            {
                float angle = (float)(1.0f * i / NumberOfVertices * Math.PI * 2);
                vertices[i].Position = new Vector3(Position.X + (float)Math.Cos(angle) * Radius,
                                                   Position.Y + (float)Math.Sin(angle) * Radius, 0);
                vertices[i].Color = color;
            }
            vertices[NumberOfVertices] = vertices[0];

            basicEffect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, vertices, 0, NumberOfVertices);
        }

        #endregion

        #region 2D Filled Rectangles

 /// <summary>
 /// Draws a filled 2D rectangle with color gradient.
 /// </summary>
 /// <param name="Aftersition">The top-left aftersition.</param>
 /// <param name="DimensionX">The rectangle width.</param>
 /// <param name="DimensionY">The rectangle height.</param>
 /// <param name="z">The Z-depth value.</param>
 /// <param name="color1">The color at the top-left corner.</param>
 /// <param name="color2">The color at the bottom-right corner.</param>
 /// <param name="projection">The projection matrix.</param>
 /// <param name="graphicsDevice">The graphics device.</param>
 /// <param name="basicEffect">The basic effect for rendering.</param>
        public static void FiledRectangle(Vector2 Position, float DimensionX, float DimensionY, float z,
                                  XnaColor color1, XnaColor color2, Matrix projection,
                                  GraphicsDevice graphicsDevice, BasicEffect basicEffect)
        {
            if (graphicsDevice == null || basicEffect == null) return;

            VertexPositionColor[] vertices = new VertexPositionColor[6];

            vertices[0].Position = new Vector3(Position.X, Position.Y, z);
            vertices[0].Color = color1;
            vertices[1].Position = new Vector3(Position.X + DimensionX, Position.Y, z);
            vertices[1].Color = color1;
            vertices[2].Position = new Vector3(Position.X, Position.Y + DimensionY, z);
            vertices[2].Color = color2;

            vertices[3].Position = new Vector3(Position.X + DimensionX, Position.Y, z);
            vertices[3].Color = color1;
            vertices[4].Position = new Vector3(Position.X + DimensionX, Position.Y + DimensionY, z);
            vertices[4].Color = color2;
            vertices[5].Position = new Vector3(Position.X, Position.Y + DimensionY, z);
            vertices[5].Color = color2;

            basicEffect.Projection = projection;
            basicEffect.View = Matrix.Identity;
            basicEffect.World = Matrix.Identity;
            basicEffect.TextureEnabled = false;
            basicEffect.VertexColorEnabled = true;
            basicEffect.LightingEnabled = false;
            basicEffect.Alpha = 1;

            basicEffect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
        }

 /// <summary>
 /// Draws a filled 2D rectangle with color gradient using Vector4 colors.
 /// </summary>
 /// <param name="Aftersition">The top-left aftersition.</param>
 /// <param name="DimensionX">The rectangle width.</param>
 /// <param name="DimensionY">The rectangle height.</param>
 /// <param name="z">The Z-depth value.</param>
 /// <param name="color1">The color at the top-left corner (Vector4).</param>
 /// <param name="color2">The color at the bottom-right corner (Vector4).</param>
 /// <param name="projection">The projection matrix.</param>
 /// <param name="graphicsDevice">The graphics device.</param>
 /// <param name="basicEffect">The basic effect for rendering.</param>
        public static void FiledRectangle(Vector2 Position, float DimensionX, float DimensionY, float z,
                                          Vector4 color1, Vector4 color2, Matrix projection,
                                          GraphicsDevice graphicsDevice, BasicEffect basicEffect)
        {
            if (graphicsDevice == null || basicEffect == null) return;

 // Convert Vector4 to XnaColor
            XnaColor c1 = new XnaColor(color1.X, color1.Y, color1.Z, color1.W);
            XnaColor c2 = new XnaColor(color2.X, color2.Y, color2.Z, color2.W);

            FiledRectangle(Position, DimensionX, DimensionY, z, c1, c2, projection, graphicsDevice, basicEffect);
        }

        #endregion

        #region 2D Filled Rectangles with Texture

 /// <summary>
 /// Draws a filled 2D rectangle with a texture.
 /// </summary>
 /// <param name="Aftersition">The top-left aftersition.</param>
 /// <param name="DimensionX">The rectangle width.</param>
 /// <param name="DimensionY">The rectangle height.</param>
 /// <param name="z">The Z-depth value.</param>
 /// <param name="alpha">The transparency alpha value.</param>
 /// <param name="tex">The texture to apply.</param>
 /// <param name="graphicsDevice">The graphics device.</param>
 /// <param name="basicEffect">The basic effect for rendering.</param>
        public static void FiledRectangleTexture(Vector2 Position, float DimensionX, float DimensionY, float z,
                                                 float alpha, Texture2D tex, GraphicsDevice graphicsDevice,
                                                 BasicEffect basicEffect)
        {
            if (graphicsDevice == null || basicEffect == null) return;

            VertexPositionTexture[] vertices = new VertexPositionTexture[6];

            vertices[0].Position = new Vector3(Position.X, Position.Y, z);
            vertices[0].TextureCoordinate = new Vector2(0, 0);
            vertices[1].Position = new Vector3(Position.X + DimensionX, Position.Y, z);
            vertices[1].TextureCoordinate = new Vector2(1, 0);
            vertices[2].Position = new Vector3(Position.X, Position.Y + DimensionY, z);
            vertices[2].TextureCoordinate = new Vector2(0, 1);

            vertices[3].Position = new Vector3(Position.X + DimensionX, Position.Y, z);
            vertices[3].TextureCoordinate = new Vector2(1, 0);
            vertices[4].Position = new Vector3(Position.X + DimensionX, Position.Y + DimensionY, z);
            vertices[4].TextureCoordinate = new Vector2(1, 1);
            vertices[5].Position = new Vector3(Position.X, Position.Y + DimensionY, z);
            vertices[5].TextureCoordinate = new Vector2(0, 1);

            basicEffect.Texture = tex;
            basicEffect.TextureEnabled = true;
            basicEffect.Alpha = alpha;
            basicEffect.Projection = Matrix.Identity;
            basicEffect.View = Matrix.Identity;
            basicEffect.World = Matrix.Identity;

            basicEffect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
        }

        #endregion

        #region 2D Rectangle Outlines

 /// <summary>
 /// Draws a 2D rectangle outline.
 /// </summary>
 /// <param name="Aftersition">The top-left aftersition.</param>
 /// <param name="Width">The rectangle width.</param>
 /// <param name="Height">The rectangle height.</param>
 /// <param name="z">The Z-depth value.</param>
 /// <param name="color">The line color.</param>
 /// <param name="projection">The projection matrix.</param>
 /// <param name="graphicsDevice">The graphics device.</param>
 /// <param name="basicEffect">The basic effect for rendering.</param>
        public static void RectangleOutline(Vector2 Position, float Width, float Height, float z,
                                            XnaColor color, Matrix projection,
                                            GraphicsDevice graphicsDevice, BasicEffect basicEffect)
        {
            Vector2 p1 = Position;
            Vector2 p2 = new Vector2(Position.X + Width, Position.Y);
            Vector2 p3 = new Vector2(Position.X + Width, Position.Y + Height);
            Vector2 p4 = new Vector2(Position.X, Position.Y + Height);

            Line(p1, p2, color, projection, graphicsDevice, basicEffect);
            Line(p2, p3, color, projection, graphicsDevice, basicEffect);
            Line(p3, p4, color, projection, graphicsDevice, basicEffect);
            Line(p4, p1, color, projection, graphicsDevice, basicEffect);
        }

        #endregion

        #region 3D Filled Triangles

 /// <summary>
 /// Draws a filled 3D triangle with vertex colors.
 /// </summary>
 /// <param name="vertices">The triangle vertices with colors.</param>
 /// <param name="world">The world matrix.</param>
 /// <param name="view">The view matrix.</param>
 /// <param name="projection">The projection matrix.</param>
 /// <param name="VLight">The light direction.</param>
 /// <param name="graphicsDevice">The graphics device.</param>
 /// <param name="basicEffect">The basic effect for rendering.</param>
        public static void FiledTriangle(VertexPositionColor[] vertices, Matrix world, Matrix view,
                                         Matrix projection, Vector3 VLight,
                                         GraphicsDevice graphicsDevice, BasicEffect basicEffect)
        {
            if (graphicsDevice == null || basicEffect == null || vertices == null) return;

            basicEffect.Projection = projection;
            basicEffect.View = view;
            basicEffect.World = world;
            basicEffect.VertexColorEnabled = true;
            basicEffect.PreferPerPixelLighting = true;
            basicEffect.LightingEnabled = true;
            basicEffect.DirectionalLight0.DiffuseColor = basicEffect.AmbientLightColor = new Vector3(0.9f, 0.9f, 0.9f);
            basicEffect.DirectionalLight0.SpecularColor = basicEffect.SpecularColor = new Vector3(0.9f, 0.9f, 0.9f);
            basicEffect.SpecularPower = 10;
            basicEffect.Alpha = 0.05f;
            basicEffect.DirectionalLight0.Direction = VLight;

            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            graphicsDevice.RasterizerState = rasterizerState;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                basicEffect.CurrentTechnique.Passes[0].Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
            }
        }

 /// <summary>
 /// Draws a filled 3D triangle with normals and textures.
 /// </summary>
 /// <param name="vertices">The triangle vertices with normals and texture coordinates.</param>
 /// <param name="color">The base color.</param>
 /// <param name="world">The world matrix.</param>
 /// <param name="VLight">The light direction.</param>
 /// <param name="view">The view matrix.</param>
 /// <param name="projection">The projection matrix.</param>
 /// <param name="graphicsDevice">The graphics device.</param>
 /// <param name="basicEffect">The basic effect for rendering.</param>
        public static void FiledTriangle(VertexPositionNormalTexture[] vertices, Vector3 color, Matrix world,
                                         Vector3 VLight, Matrix view, Matrix projection,
                                         GraphicsDevice graphicsDevice, BasicEffect basicEffect)
        {
            if (graphicsDevice == null || basicEffect == null || vertices == null) return;

            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            graphicsDevice.RasterizerState = rasterizerState;

 // Create 1x1 white texture if needed
            Texture2D texture = new Texture2D(graphicsDevice, 1, 1);
            texture.SetData(new XnaColor[] { XnaColor.White });

            basicEffect.Texture = texture;
            basicEffect.TextureEnabled = true;
            basicEffect.LightingEnabled = true;
            basicEffect.VertexColorEnabled = false;

            basicEffect.DirectionalLight0.DiffuseColor = basicEffect.AmbientLightColor = color;
            basicEffect.DirectionalLight0.SpecularColor = basicEffect.SpecularColor = new Vector3(1f, 1f, 0.9f);
            basicEffect.SpecularPower = 20;
            basicEffect.Alpha = 0.2f;
            basicEffect.DirectionalLight0.Direction = VLight;

            basicEffect.Projection = projection;
            basicEffect.View = view;
            basicEffect.World = world;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                basicEffect.CurrentTechnique.Passes[0].Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length / 3);
            }
        }

        #endregion
    }
}