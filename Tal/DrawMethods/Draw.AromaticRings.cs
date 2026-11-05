// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Dashed ring inside aromatic rings — fixed gray color.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        /// <summary>
        /// Draw a dashed circle in the plane of each detected aromatic ring.
        /// Color is always the same gray (not affected by atom color or rotation).
        /// </summary>
        private void DrawAromaticRings(Matrix scaleRotate)
        {
            if (!Flags.Instance.ShowAromaticRings) return;
            if (_molecule?.AromaticRings == null || _molecule.AromaticRings.Count == 0) return;
            if (_cachedSmallRodMeshes == null || _smallRodTransforms == null) return;

            float thickness = Flags.Instance.StickRadius * 0.6f;
            const int segments = 24;
            var gray = new Vector3(0.55f, 0.55f, 0.55f);

            foreach (var ring in _molecule.AromaticRings)
            {
                if (ring.AtomIndices == null || ring.AtomIndices.Count < 5) continue;

                Vector3 n = ring.Normal;
                if (n.LengthSquared() < 1e-8f) continue;
                n.Normalize();

                Vector3 u = Vector3.Cross(n, Vector3.UnitY);
                if (u.LengthSquared() < 0.01f)
                    u = Vector3.Cross(n, Vector3.UnitX);
                u.Normalize();
                Vector3 v = Vector3.Cross(n, u);
                v.Normalize();

                float r = ring.Radius;
                if (r < 0.2f) r = 0.8f;

                for (int s = 0; s < segments; s += 2)
                {
                    float a1 = s * MathHelper.TwoPi / segments;
                    float a2 = (s + 1) * MathHelper.TwoPi / segments;

                    Vector3 p1 = ring.Center + u * (float)System.Math.Cos(a1) * r
                                             + v * (float)System.Math.Sin(a1) * r;
                    Vector3 p2 = ring.Center + u * (float)System.Math.Cos(a2) * r
                                             + v * (float)System.Math.Sin(a2) * r;

                    DrawGrayRodSegment(p1, p2, scaleRotate, thickness, gray);
                }
            }
        }

        /// <summary>
        /// Draw a rod segment with a fixed color (bypasses atom color cache).
        /// </summary>
        private void DrawGrayRodSegment(Vector3 p1, Vector3 p2, Matrix scaleRotate, float thickness, Vector3 color)
        {
            if (_cachedSmallRodMeshes == null || _smallRodTransforms == null) return;

            Vector3 dir = p2 - p1;
            float length = dir.Length();
            if (length < 0.001f) return;
            dir.Normalize();

            float angleZ = (float)System.Math.Asin(System.Math.Clamp(dir.Z, -1.0f, 1.0f));
            float angleXY = -(float)(System.Math.PI - System.Math.Atan2(dir.Y, dir.X));

            float scaleX = 0.0127f * length;
            Matrix segmentMatrix =
                Matrix.CreateScale(scaleX, thickness, thickness) *
                Matrix.CreateRotationY(angleZ) *
                Matrix.CreateRotationZ(angleXY) *
                Matrix.CreateTranslation(p1) *
                scaleRotate;

            foreach (ModelMesh mesh in _cachedSmallRodMeshes)
            {
                Matrix world = _smallRodTransforms[mesh.ParentBone.Index] * segmentMatrix;
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = world;
                    effect.DiffuseColor = color;
                    effect.AmbientLightColor = color * 0.6f;
                    effect.Alpha = 1f;
                    effect.SpecularColor = Vector3.Zero;
                    effect.DirectionalLight0.DiffuseColor = color;
                    effect.DirectionalLight0.SpecularColor = Vector3.Zero;
                    effect.DirectionalLight1.DiffuseColor = color * 0.3f;
                    effect.DirectionalLight1.SpecularColor = Vector3.Zero;
                }
                mesh.Draw();
            }
        }
    }
}
