// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Force arrows: shaft is a rod, head is a cone of constant length
// starting exactly where the shaft ends.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tal.Calculate;

namespace Tal.DrawMethods
{
    public partial class Draw
    {
        /// <summary>Arrow head length in the same units as a bond length (Å).</summary>
        private const float ArrowHeadLength = 0.5f;

        private void DrawForceArrows(Matrix scaleRotate)
        {
            if (!Flags.Instance.ShowForces && !Flags.Instance.ShowIntermolecular) return;
            if (_molecule?.atom == null) return;
            if (_cachedSmallRodMeshes == null || _smallRodTransforms == null) return;

            Vector3 origDiffuse = _effectCache.DiffuseColor;
            Vector3 origAmbient = _effectCache.AmbientLightColor;
            Vector3 origLight = _effectCache.LightDiffuseColor;
            Vector3 origSpec = _effectCache.SpecularColor;
            float origAlpha = _effectCache.Alpha;

            _effectCache.Alpha = 1f;
            _effectCache.SpecularColor = Vector3.Zero;

            float scale =  Flags.Instance.ForceArrowScale;

            if (Flags.Instance.ShowForces)
            {
                for (int i = 0; i < _molecule.NumberOfAtoms; i++)
                {
                    var a = _molecule.atom[i];
                    if (!a.HasForce || !a.show) continue;

                    DrawArrow(a.Coordinate, a.Force, Flags.Instance.ShowForceTotal,
                        new Vector3(0.85f, 0.12f, 0.12f), scale, scaleRotate);
                    DrawArrow(a.Coordinate, a.ForceElectro, Flags.Instance.ShowForceElectro,
                        new Vector3(0.95f, 0.55f, 0.05f), scale, scaleRotate);
                    DrawArrow(a.Coordinate, a.ForceDispersion, Flags.Instance.ShowForceDispersion,
                        new Vector3(0.15f, 0.65f, 0.25f), scale, scaleRotate);
                    DrawArrow(a.Coordinate, a.ForceRepulsion, Flags.Instance.ShowForceRepulsion,
                        new Vector3(0.25f, 0.40f, 0.95f), scale, scaleRotate);
                }
            }

            if (Flags.Instance.ShowIntermolecular && ForceDecomposition.Intermolecular != null)
            {
                foreach (var p in ForceDecomposition.Intermolecular)
                {
                    DrawInter(p.From, p.To, p.Total, Flags.Instance.ShowForceTotal,
                        new Vector3(0.75f, 0.08f, 0.08f), 0f, scaleRotate);
                    DrawInter(p.From, p.To, p.Electro, Flags.Instance.ShowForceElectro,
                        new Vector3(0.95f, 0.5f, 0.05f), 0.35f, scaleRotate);
                    DrawInter(p.From, p.To, p.Dispersion, Flags.Instance.ShowForceDispersion,
                        new Vector3(0.1f, 0.7f, 0.3f), 0.7f, scaleRotate);
                    DrawInter(p.From, p.To, p.Repulsion, Flags.Instance.ShowForceRepulsion,
                        new Vector3(0.2f, 0.35f, 0.95f), 1.05f, scaleRotate);
                }
            }

            _effectCache.DiffuseColor = origDiffuse;
            _effectCache.AmbientLightColor = origAmbient;
            _effectCache.LightDiffuseColor = origLight;
            _effectCache.SpecularColor = origSpec;
            _effectCache.Alpha = origAlpha;
        }

        private void DrawArrow(Vector3 origin, Vector3 force, bool enabled, Vector3 color,
            float scale, Matrix scaleRotate)
        {
            if (!enabled) return;
            float mag = force.Length();
            if (mag < 1e-5f) return;

            Vector3 dir = force / mag;
            // Scale must freely change length (no high floor that hides the slider).
            float shaft = Math.Clamp(mag * scale * 4f, 0.15f, 100f);
            float radius = Flags.Instance.StickRadius;

            _effectCache.DiffuseColor = color;
            _effectCache.AmbientLightColor = Vector3.One * 0.5f;
            _effectCache.LightDiffuseColor = color;

            DrawForceRod(origin, RodOrientation(origin, dir), shaft, radius, scaleRotate, true);
        }

        private void DrawInter(Vector3 from, Vector3 to, Vector3 force, bool enabled,
            Vector3 color, float offset, Matrix scaleRotate)
        {
            if (!enabled) return;
            Vector3 along = to - from;
            if (along.LengthSquared() < 1e-12f) return;

            float mag = force.Length();
            // Keep very small net molecular forces visible (equilibrium crystals ≈ 0)
            if (mag < 1e-8f)
            {
                force = Vector3.Normalize(along) * 1e-4f;
                mag = force.Length();
            }
            Vector3 side = Vector3.Cross(along, Vector3.UnitZ);
            if (side.LengthSquared() < 1e-6f) side = Vector3.Cross(along, Vector3.UnitY);
            side.Normalize();

            Vector3 dir = force / mag;
            float interScale = Flags.Instance.ForceArrowScale;
            float shaft = Math.Clamp(mag * interScale * 40f, 0.3f, 80f);
            float radius = Flags.Instance.StickRadius * 2.2f;
            Vector3 origin = from + side * offset;

            _effectCache.DiffuseColor = color;
            _effectCache.AmbientLightColor = Vector3.One * 0.55f;
            _effectCache.LightDiffuseColor = color;

            DrawForceRod(origin, RodOrientation(origin, dir), shaft, radius, scaleRotate, true);
        }

        private struct RodOrient
        {
            public float AngleZ;
            public float AngleXY;
            public Matrix Rotation;
        }

        /// <summary>
        /// Same angles as unit-cell edges. Rotation maps the rod's -X axis onto dir.
        /// </summary>
        private static RodOrient RodOrientation(Vector3 origin, Vector3 dir)
        {
            Vector3 end = origin + dir;
            float angleZ = (float)Math.Asin(Math.Clamp(origin.Z - end.Z, -1f, 1f));
            float angleXY = -(float)(Math.PI - Math.Atan2(origin.Y - end.Y, origin.X - end.X));
            return new RodOrient
            {
                AngleZ = angleZ,
                AngleXY = angleXY,
                Rotation = Matrix.CreateRotationY(angleZ) * Matrix.CreateRotationZ(angleXY)
            };
        }


        /// <summary>
        /// Rod and cone in one call. The cone uses the rod's View, Projection
        /// and the same Rotation / scaleRotate. Its translation is the rod tip.
        /// </summary>
        private void DrawForceRod(Vector3 origin, RodOrient o, float length, float radius,
            Matrix scaleRotate, bool withHead)
        {
            float xScale = Flags.Instance.StickRadiusFactor * length;
            Matrix m = Matrix.CreateScale(xScale, radius, radius);
            m *= o.Rotation;
            m *= Matrix.CreateTranslation(origin);
            m *= scaleRotate;

            BasicEffect rodEffect = null;
            foreach (var mesh in _cachedSmallRodMeshes)
            {
                Matrix world = _smallRodTransforms[mesh.ParentBone.Index] * m;
                foreach (BasicEffect effect in mesh.Effects)
                {
                    rodEffect = effect;
                    ApplyPerAtomEffect(effect, world, _effectCache);
                }
                mesh.Draw();
            }

            if (!withHead || _cachedConeMeshes == null) return;

            // Tip = rod mesh end transformed by the same bone * scale * rotation.
            var meshTip = new Vector3(-RuntimeCylinder.BASE_LENGTH, 0f, 0f);
            Matrix rodLocal = Matrix.CreateScale(xScale, 1f, 1f) * o.Rotation;
            int bi = _cachedSmallRodMeshes[0].ParentBone.Index;
            if (bi >= 0 && bi < _smallRodTransforms.Length)
                rodLocal = _smallRodTransforms[bi] * rodLocal;
            Vector3 offset = Vector3.Transform(meshTip, rodLocal);
            Vector3 tip = origin + offset;

            float headScale = Flags.Instance.ForceConePositionScale;
            if (headScale < 0.05f) headScale = 50f / 30f;
            Matrix cm = Matrix.CreateScale(
                Flags.Instance.StickRadiusFactor * ArrowHeadLength * headScale,
                radius * headScale,
                radius * headScale);
            cm *= o.Rotation;
            cm *= Matrix.CreateTranslation(tip);
            cm *= scaleRotate;

            foreach (var mesh in _cachedConeMeshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    if (rodEffect != null)
                    {
                        effect.View = rodEffect.View;
                        effect.Projection = rodEffect.Projection;
                    }
                    else
                    {
                        ApplyGlobalEffectStateTo(effect);
                    }
                    ApplyPerAtomEffect(effect, cm, _effectCache);
                }
                mesh.Draw();
            }
        }
    }
}
