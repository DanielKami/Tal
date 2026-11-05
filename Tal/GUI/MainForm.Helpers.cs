// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// MainForm partial — helper methods: atom picking under mouse, measurement label positions, mouse ray, shared utilities.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Tal.Core.PowderDiffraction;
using Tal.DrawMethods;

namespace Tal
{
    public partial class MainForm
    {
        #region Helper Methods

        private int FindAtomAtPosition(System.Drawing.Point mousePosition, int viewportWidth, int viewportHeight)
        {
            var molecule = GetActiveMolecule();
            var camera = GetActiveCamera();

            if (molecule == null || camera == null || molecule.NumberOfAtoms == 0)
                return -1;

            int TOLERANCE = 15;
            Matrix fullTransformMatrix = camera.Scale * camera.Rotate * camera.View * camera.Translate * camera.Projection;

            for (int i = 0; i < molecule.NumberOfAtoms; ++i)
            {
                if (molecule.atom[i].style == 10) continue;

                // ⭐ NEW: skip hidden atoms (not clickable)
                if (!molecule.atom[i].show) continue;

                Vector3 pos = molecule.atom[i].Coordinate;
                Matrix world = Matrix.CreateTranslation(pos);
                Matrix worldViewProjectionMatrix = world * fullTransformMatrix;

                float w = worldViewProjectionMatrix.M44;
                if (w <= 0) continue;

                int modelX = (int)(viewportWidth / 2 * (1.0 + worldViewProjectionMatrix.M41 / w));
                int modelY = (int)(viewportHeight / 2 * (1.0 - worldViewProjectionMatrix.M42 / w));

                int dx = Math.Abs(modelX - mousePosition.X);
                int dy = Math.Abs(modelY - mousePosition.Y);

                if (dx < TOLERANCE && dy < TOLERANCE)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindNearestAtomAtPosition(System.Drawing.Point mousePosition, int viewportWidth, int viewportHeight, Camera camera, Molecule molecule, int tolerance = 20)
        {
            if (molecule == null || camera == null || molecule.NumberOfAtoms == 0)
                return -1;

            Matrix fullTransformMatrix = camera.Scale * camera.Rotate * camera.View *
                                          camera.Translate * camera.Projection;

            int halfWidth = viewportWidth / 2;
            int halfHeight = viewportHeight / 2;

            for (int i = 0; i < molecule.NumberOfAtoms; ++i)
            {
                if (molecule.atom[i].style == 10) continue;

                // ⭐ NEW: skip hidden atoms (not clickable)
                if (!molecule.atom[i].show) continue;

                Vector3 pos = molecule.atom[i].Coordinate;

                Matrix world = Matrix.CreateTranslation(pos);
                Matrix worldViewProjectionMatrix = world * fullTransformMatrix;

                float w = worldViewProjectionMatrix.M44;
                if (w <= 0) continue;

                int modelX = (int)(halfWidth * (1.0 + worldViewProjectionMatrix.M41 / w));
                int modelY = (int)(halfHeight * (1.0 - worldViewProjectionMatrix.M42 / w));

                int dx = Math.Abs(modelX - mousePosition.X);
                int dy = Math.Abs(modelY - mousePosition.Y);

                if (dx < tolerance && dy < tolerance)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindMeasurementLabelAtPosition(System.Drawing.Point mousePosition, int viewportWidth, int viewportHeight)
        {
            if (_measure.measurement == null || _measure.measurement.Count == 0)
                return -1;

            var camera = GetActiveCamera();
            if (camera == null) return -1;

            int halfWidth = viewportWidth / 2;
            int halfHeight = viewportHeight / 2;

            Matrix ScaleRotate = Matrix.CreateScale(camera.OneOverZoom, camera.OneOverZoom, camera.OneOverZoom)
                                 * camera.Rotate
                                 * camera.Translate;

            Matrix viewProj = ScaleRotate * camera.View * camera.Projection;

            int TOLERANCE = 30;

            for (int i = 0; i < _measure.measurement.Count; i++)
            {
                var m = _measure.measurement[i];
                if (!m.show) continue;

                Vector3 basePos3D = GetMeasurementLabelPosition(m);
                Vector3 screenPos = ProjectToScreen(basePos3D, viewProj, halfWidth, halfHeight);

                if (screenPos.Z <= 0) continue;

                if (_measure.MeasurementLabelOffsets.TryGetValue(i, out var offset))
                {
                    screenPos.X += offset.OffsetX;
                    screenPos.Y += offset.OffsetY;
                }

                int dx = Math.Abs((int)screenPos.X - mousePosition.X);
                int dy = Math.Abs((int)screenPos.Y - mousePosition.Y);

                if (dx < TOLERANCE && dy < TOLERANCE)
                {
                    return i;
                }
            }

            return -1;
        }

        private Vector3 GetMeasurementLabelPosition(Measure.Measurement m)
        {
            switch (m.type)
            {
                case 1:
                    return (m.position1 + m.position2) / 2;
                case 2:
                    return GetAngleLabelPosition(m);
                case 3:
                    return (m.position2 + m.position3) / 2;
                default:
                    return m.position1;
            }
        }

        private Vector3 GetAngleLabelPosition(Measure.Measurement m)
        {
            Vector3 v1 = m.position1 - m.position2;
            Vector3 v2 = m.position3 - m.position2;

            float len1 = v1.Length();
            float len2 = v2.Length();

            if (len1 < 0.001f || len2 < 0.001f) return m.position2;

            v1.Normalize();
            v2.Normalize();

            float angle = (float)Math.Acos(Math.Clamp(Vector3.Dot(v1, v2), -1.0f, 1.0f));
            float arcRadius = Math.Min(len1, len2) * 0.4f;
            if (arcRadius < 0.1f) arcRadius = 0.1f;

            Vector3 normal = Vector3.Normalize(Vector3.Cross(v1, v2));
            if (normal.Length() < 0.001f) return m.position2;

            float halfAngle = angle * 0.5f;
            Vector3 midDir = Vector3.Transform(v1, Matrix.CreateFromAxisAngle(normal, halfAngle));

            float labelRadius = arcRadius * 0.6f;
            return m.position2 + midDir * labelRadius;
        }

        private Vector3 ProjectToScreen(Vector3 worldPos, Matrix viewProj, int halfWidth, int halfHeight)
        {
            Matrix world = Matrix.CreateTranslation(worldPos);
            Matrix full = world * viewProj;

            float w = full.M44;
            if (w <= 0) return new Vector3(0, 0, -1);

            float x = halfWidth + (full.M41 / w) * halfWidth;
            float y = halfHeight - (full.M42 / w) * halfHeight;
            float z = full.M43 / w;

            return new Vector3(x, y, z);
        }

        private Vector3 ScreenToWorldPosition(System.Drawing.Point mousePosition, int viewportWidth, int viewportHeight, Camera camera)
        {
            float ndcX = (2.0f * mousePosition.X / viewportWidth) - 1.0f;
            float ndcY = 1.0f - (2.0f * mousePosition.Y / viewportHeight);

            Vector4 ndcPos = new Vector4(ndcX, ndcY, 0.5f, 1.0f);

            Matrix invProjection = Matrix.Invert(camera.Projection);
            Matrix invView = Matrix.Invert(camera.View);
            Matrix invScaleRotate = Matrix.Invert(camera.Scale * camera.Rotate);

            Vector4 worldPos = Vector4.Transform(ndcPos, invProjection);
            worldPos = Vector4.Transform(worldPos, invView);
            worldPos = Vector4.Transform(worldPos, invScaleRotate);

            Vector3 result = new Vector3(worldPos.X, worldPos.Y, worldPos.Z) - camera.g_system_translate;

            return result;
        }

        #endregion

        #region Rey plane intersection
        /// <summary>
        /// Builds a world-space ray from a mouse position through the camera.
        /// Returns (rayOrigin, rayDirection) with normalized direction.
        /// </summary>
        private (Vector3 origin, Vector3 direction) GetMouseRay(
            System.Drawing.Point mousePos, int viewportWidth, int viewportHeight, Camera camera)
        {
            float ndcX = (2.0f * mousePos.X / viewportWidth) - 1.0f;
            float ndcY = 1.0f - (2.0f * mousePos.Y / viewportHeight);

            Matrix full = camera.Scale * camera.Rotate * camera.View * camera.Projection;
            Matrix invFull = Matrix.Invert(full);

            Vector4 p1 = Vector4.Transform(new Vector4(ndcX, ndcY, 0.5f, 1f), invFull);
            Vector4 p2 = Vector4.Transform(new Vector4(ndcX, ndcY, 0.9f, 1f), invFull);

            if (Math.Abs(p1.W) < 1e-6f || Math.Abs(p2.W) < 1e-6f)
            {
                System.Diagnostics.Debug.WriteLine($"[GetMouseRay] W too small: p1.W={p1.W}, p2.W={p2.W}");
                return (Vector3.Zero, Vector3.UnitZ);
            }

            Vector3 nearWorld = new Vector3(p1.X, p1.Y, p1.Z) / p1.W;
            Vector3 farWorld = new Vector3(p2.X, p2.Y, p2.Z) / p2.W;

            Vector3 dir = farWorld - nearWorld;
            if (dir.LengthSquared() < 1e-9f) return (nearWorld, Vector3.UnitZ);
            dir.Normalize();

            System.Diagnostics.Debug.WriteLine($"[GetMouseRay] near={nearWorld} far={farWorld} dir={dir}");
            return (nearWorld, dir);
        }

        /// <summary>
        /// Intersects a ray with a CrystalPlane. Returns the hit point in SHIFTED
        /// coordinates (i.e. matching atom.Coordinate), or null if no intersection.
        /// </summary>
        private Vector3? RayPlaneIntersection(
            Vector3 rayOrigin, Vector3 rayDir, CrystalPlane plane, Vector3 moleculeShift)
        {
            if (plane == null) return null;

            Vector3 n = new Vector3((float)plane.A, (float)plane.B, (float)plane.C);
            float nLen = n.Length();
            if (nLen < 1e-6f) return null;

            n /= nLen;
            float d = (float)plane.D / nLen;

            Vector3 oRaw = rayOrigin + moleculeShift;
            Vector3 dirRaw = rayDir;

            float denom = Vector3.Dot(n, dirRaw);
            if (Math.Abs(denom) < 1e-9f) return null;

            float t = -(Vector3.Dot(n, oRaw) + d) / denom;
            if (t < 0f) return null;

            Vector3 hitRaw = oRaw + t * dirRaw;
            Vector3 hitShifted = hitRaw - moleculeShift;
            return hitShifted;
        }
        #endregion
    }
}
