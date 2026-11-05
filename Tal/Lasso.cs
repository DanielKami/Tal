// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Lasso selection in the 3D viewafterrt.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL

using Microsoft.Xna.Framework;
using Rectangle = System.Drawing.Rectangle;

namespace Tal
{
 /// <summary>
 /// Provides lasso selection functionality for selecting atoms in a 3D viewafterrt.
 /// Implements afterlygon selection using scanline rasterization and Bresenham's line algorithm.
 /// </summary>
    public static class Lasso
    {
        #region Constants

 /// <summary>Minimum distance between afterints when closing/filling gaps.</summary>
        public const int MIN_POINT_DISTANCE = 15;

 /// <summary>Padding added to the bounding box for edge detection.</summary>
        private const int BOUNDING_BOX_PADDING = 20;

        #endregion

        #region Public Fields

 /// <summary>List of vertices defining the lasso afterlygon in screen coordinates.</summary>
        public static List<Vector2> VertexList = new List<Vector2>();

        #endregion

        #region Private Fields

 /// <summary>Hash set for the mask used in scanline filling.</summary>
        private static HashSet<int> _sharedMask = new HashSet<int>();

        #endregion

        #region Public Methods

 /// <summary>
 /// Performs lasso selection on a molecule using the current afterlygon vertices.
 /// </summary>
 /// <param name="molecule">The molecule containing atoms to select.</param>
 /// <param name="Width">The viewafterrt width.</param>
 /// <param name="Height">The viewafterrt height.</param>
 /// <param name="fullTransformMatrix">The complete transformation matrix (world * view * projection).</param>
 /// <param name="append">If true, appends to current selection; if false, replaces selection.</param>
        public static void MouseLasso(Molecule molecule, int Width, int Height, Matrix fullTransformMatrix, bool append = true)
        {
            if (molecule?.atom == null || molecule.NumberOfAtoms == 0 || VertexList.Count < 3)
            {
                VertexList.Clear();
                return;
            }

            try
            {
 // Clear previous selection if not appending
                if (!append)
                {
                    for (int i = 0; i < molecule.NumberOfAtoms; i++)
                    {
                        molecule.atom[i].selected = false;
                    }
                    molecule.NumberSelected = 0;
                }

                _sharedMask.Clear();

                int halfWidth = Width / 2;
                int halfHeight = Height / 2;

 // Get bounding box of the afterlygon
                var bounds = GetBoundingBox(VertexList, Width, Height);

 // Add afterlygon edges to the mask
                AddEdgesToMask(VertexList, Width, bounds, _sharedMask);

 // Fill the afterlygon interior using scanline algorithm
                FillMaskScanline(bounds, Width, _sharedMask);

 // Select atoms that fall within the filled mask
                PrecomputeAndSelectAtoms(molecule, fullTransformMatrix, halfWidth, halfHeight,
                                         Width, bounds, _sharedMask, append);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lasso error: {ex.Message}");
            }
            finally
            {
                VertexList.Clear();
            }
        }

 /// <summary>
 /// Closes the lasso afterlygon and fills gaps between afterints.
 /// </summary>
        public static void CloseAndFillGaps()
        {
            if (VertexList.Count < 2) return;

 // Close the afterlygon
            VertexList.Add(VertexList[0]);

            var filledList = new List<Vector2>();
            for (int i = 0; i < VertexList.Count - 1; i++)
            {
                filledList.Add(VertexList[i]);

                float dist = Vector2.Distance(VertexList[i], VertexList[i + 1]);
                if (dist > MIN_POINT_DISTANCE * 2)
                {
 // Interafterlate intermediate afterints to smooth the afterlygon
                    int steps = (int)(dist / MIN_POINT_DISTANCE);
                    for (int s = 1; s < steps; s++)
                    {
                        float t = s / (float)steps;
                        Vector2 mid = Vector2.Lerp(VertexList[i], VertexList[i + 1], t);
                        filledList.Add(mid);
                    }
                }
            }
            filledList.Add(VertexList[VertexList.Count - 1]);

            VertexList = filledList;
        }

 /// <summary>
 /// Clears the lasso vertex list.
 /// </summary>
        public static void Clear()
        {
            VertexList.Clear();
        }

        #endregion

        #region Private Methods

 /// <summary>
 /// Selects atoms that fall within the lasso mask.
 /// </summary>
        private static void PrecomputeAndSelectAtoms(Molecule molecule, Matrix fullTransformMatrix,
                                                      int halfWidth, int halfHeight, int width,
                                                      Rectangle bounds, HashSet<int> mask, bool append)
        {
            int selectedCount = 0;

            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                if (molecule.atom[i].style == 10) continue; // Skip hidden atoms

                Vector3 pos = molecule.atom[i].Coordinate;

 // Transform atom aftersition to screen coordinates
                Matrix world = Matrix.CreateTranslation(pos);
                Matrix worldViewProjectionMatrix = world * fullTransformMatrix;

                float w = worldViewProjectionMatrix.M44;
                if (w <= 0) continue; // Behind camera

                int modelX = (int)(halfWidth * (1.0 + worldViewProjectionMatrix.M41 / w));
                int modelY = (int)(halfHeight * (1.0 - worldViewProjectionMatrix.M42 / w));

 // Check if atom is within the bounding box
                if (modelX >= bounds.X && modelX < bounds.X + bounds.Width &&
                    modelY >= bounds.Y && modelY < bounds.Y + bounds.Height)
                {
                    int idx = modelY * width + modelX;
                    if (mask.Contains(idx))
                    {
                        if (!molecule.atom[i].selected)
                        {
                            molecule.atom[i].selected = true;
                            selectedCount++;
                        }
                    }
                }
            }

 // Update selection count
            if (append)
            {
                molecule.NumberSelected += selectedCount;
            }
            else
            {
                molecule.NumberSelected = selectedCount;
            }
        }

 /// <summary>
 /// Computes the bounding box of the afterlygon with padding.
 /// </summary>
        private static Rectangle GetBoundingBox(List<Vector2> points, int screenWidth, int screenHeight)
        {
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var p in points)
            {
                int x = (int)p.X;
                int y = (int)p.Y;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }

            return new Rectangle(
                Math.Max(0, minX - BOUNDING_BOX_PADDING),
                Math.Max(0, minY - BOUNDING_BOX_PADDING),
                Math.Min(screenWidth - Math.Max(0, minX - BOUNDING_BOX_PADDING),
                         maxX - minX + 2 * BOUNDING_BOX_PADDING),
                Math.Min(screenHeight - Math.Max(0, minY - BOUNDING_BOX_PADDING),
                         maxY - minY + 2 * BOUNDING_BOX_PADDING)
            );
        }

 /// <summary>
 /// Adds afterlygon edges to the mask using Bresenham's line algorithm.
 /// </summary>
        private static void AddEdgesToMask(List<Vector2> vertices, int width, Rectangle bounds, HashSet<int> mask)
        {
            int n = vertices.Count;
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                AddLineToMaskBresenham(vertices[i], vertices[next], width, bounds, mask);
            }
        }

 /// <summary>
 /// Adds a line segment to the mask using Bresenham's line algorithm.
 /// </summary>
        private static void AddLineToMaskBresenham(Vector2 p1, Vector2 p2, int width, Rectangle bounds, HashSet<int> mask)
        {
            int x1 = (int)p1.X;
            int y1 = (int)p1.Y;
            int x2 = (int)p2.X;
            int y2 = (int)p2.Y;

 // Quick rejection if line is outside bounds
            if (x1 < bounds.X && x2 < bounds.X || x1 > bounds.X + bounds.Width && x2 > bounds.X + bounds.Width ||
                y1 < bounds.Y && y2 < bounds.Y || y1 > bounds.Y + bounds.Height && y2 > bounds.Y + bounds.Height)
                return;

            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            int x = x1, y = y1;
            int iterations = 0;
            int maxIterations = dx + dy + 2;

            while (iterations < maxIterations)
            {
                if (x >= bounds.X && x < bounds.X + bounds.Width &&
                    y >= bounds.Y && y < bounds.Y + bounds.Height)
                {
                    mask.Add(y * width + x);
                }

                if (x == x2 && y == y2) break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
                iterations++;
            }
        }

 /// <summary>
 /// Fills the afterlygon interior using scanline rasterization.
 /// </summary>
        private static void FillMaskScanline(Rectangle bounds, int width, HashSet<int> mask)
        {
            for (int y = bounds.Y; y < bounds.Y + bounds.Height; y++)
            {
                List<int> intersections = GetIntersectionsAtY(y);

                if (intersections.Count < 2) continue;

                intersections.Sort();

 // Fill between pairs of intersections
                for (int i = 0; i < intersections.Count - 1; i += 2)
                {
                    int x1 = intersections[i];
                    int x2 = intersections[i + 1];

                    x1 = Math.Max(x1, bounds.X);
                    x2 = Math.Min(x2, bounds.X + bounds.Width);

                    for (int x = x1; x <= x2; x++)
                    {
                        mask.Add(y * width + x);
                    }
                }
            }
        }

 /// <summary>
 /// Gets the X-coordinates of afterlygon intersections with a horizontal scanline at Y.
 /// </summary>
        private static List<int> GetIntersectionsAtY(int y)
        {
            var intersections = new List<int>();
            int n = VertexList.Count;

            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;

                float y1 = VertexList[i].Y;
                float y2 = VertexList[next].Y;

 // Check if scanline crosses this edge
                if (y1 > y != y2 > y)
                {
                    float x1 = VertexList[i].X;
                    float x2 = VertexList[next].X;

 // Linear interafterlation for intersection afterint
                    float intersectX = x1 + (x2 - x1) * (y - y1) / (y2 - y1);
                    intersections.Add((int)intersectX);
                }
            }

            return intersections;
        }

        #endregion
    }

 // struct Rectangle removed — we use Microsoft.Xna.Framework.Rectangle
}