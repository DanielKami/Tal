// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldMarchingCubes.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HirshfeldMarchingCubes v2.1 — flat arrays.
//
// ⭐ ZMIANY relative to v2.0:
// - NIE alokuje mcInput (float[] z (W - isoLevel)).
// - Passes W (flat) + isoLevel DIRECTLY to the generator.
// Generator odejmuje isoLevel inline w hot loopie.
// - Savings: 55 MB allocations + copying at 240³.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Tal.Cavity;
using SysVector3 = System.Numerics.Vector3;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Custom Marching Cubes version for the Hirshfeld surface.
 /// Bazowana na MarchingCubesMeshGenerator.GenerateTriangles.
 ///
 /// Principle: pass the W array (flat) and isoLevel to the generator.
 /// Generator compares values > isoLevel (= "inside").
 /// </summary>
    public static class HirshfeldMarchingCubes
    {
 /// <summary>
 /// Ekstrahuje surface z mesh W (flat).
 /// </summary>
        public static HirshfeldSurface Extract(
            HirshfeldGrid grid,
            float isoLevel = 0.5f,
            int smoothingIterations = 1,
            float smoothingFactor = 0.3f,
            Action<string, float> progress = null,
            System.Threading.CancellationToken ct = default)
        {
            if (grid?.W == null)
            {
                progress?.Invoke("No grid data", 1.0f);
                return null;
            }

            int nx = grid.ResX;
            int ny = grid.ResY;
            int nz = grid.ResZ;

            progress?.Invoke("Running Marching Cubes...", 0.84f);

            var mcOptions = new HirshfeldMarchingCubesGenerator.Options
            {
                IsoLevel = isoLevel,       // directly — without subtraction
                GenerateNormals = true,
                FixWinding = false,
                ProgressPercentStart = 84,
                ProgressPercentEnd = 92,
                ProgressCallback = progress
            };

 // ⭐ Przekazujemy W (flat) + isoLevel — generator sam odejmie inline
            var mcMesh = HirshfeldMarchingCubesGenerator.Extract(
                grid.W,
                nx, ny, nz,
                grid.Grid,
                mcOptions,
                ct);

            if (mcMesh == null || !mcMesh.IsValid)
            {
                progress?.Invoke("MC returned empty mesh", 1.0f);
                return null;
            }

            progress?.Invoke("Converting mesh...", 0.94f);

 // ═══ Konversion SysVector3 → XNA Vector3 ═══
            var surface = new HirshfeldSurface();
            int vc = mcMesh.VertexCount;

            surface.Vertices.Capacity = vc;
            for (int i = 0; i < vc; i++)
            {
                var v = mcMesh.Vertices[i];
                surface.Vertices.Add(new Vector3(v.X, v.Y, v.Z));
            }

            surface.Indices = new List<int>(mcMesh.Indices);

            if (mcMesh.Normals != null && mcMesh.Normals.Count == vc)
            {
                surface.Normals.Capacity = vc;
                for (int i = 0; i < vc; i++)
                {
                    var nrm = mcMesh.Normals[i];
                    surface.Normals.Add(new Vector3(nrm.X, nrm.Y, nrm.Z));
                }
            }
            else
            {
                surface.Normals = null;
            }

            surface.IsoLevel = isoLevel;

            if (smoothingIterations > 0 && vc > 3)
            {
                progress?.Invoke("Smoothing mesh...", 0.96f);
                SmoothMesh(surface, smoothingIterations, smoothingFactor);
            }

            progress?.Invoke("Mesh extraction complete", 0.98f);

            return surface;
        }

 /// <summary>
 /// Proste Laplacian smoothing — without changes toafterlogsi.
 /// </summary>
        private static void SmoothMesh(HirshfeldSurface surface, int iterations, float factor)
        {
            if (surface.VertexCount < 3 || surface.Indices.Count < 3) return;

            var neighbors = new HashSet<int>[surface.VertexCount];
            for (int i = 0; i < surface.VertexCount; i++)
                neighbors[i] = new HashSet<int>();

            for (int i = 0; i < surface.Indices.Count; i += 3)
            {
                int a = surface.Indices[i];
                int b = surface.Indices[i + 1];
                int c = surface.Indices[i + 2];

                neighbors[a].Add(b); neighbors[a].Add(c);
                neighbors[b].Add(a); neighbors[b].Add(c);
                neighbors[c].Add(a); neighbors[c].Add(b);
            }

            var newVerts = new Vector3[surface.VertexCount];

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int i = 0; i < surface.VertexCount; i++)
                {
                    var nb = neighbors[i];
                    if (nb.Count == 0)
                    {
                        newVerts[i] = surface.Vertices[i];
                        continue;
                    }

                    Vector3 sum = Vector3.Zero;
                    foreach (int j in nb)
                        sum += surface.Vertices[j];

                    Vector3 avg = sum / nb.Count;
                    newVerts[i] = Vector3.Lerp(surface.Vertices[i], avg, factor);
                }

                for (int i = 0; i < surface.VertexCount; i++)
                    surface.Vertices[i] = newVerts[i];
            }
        }
    }
}