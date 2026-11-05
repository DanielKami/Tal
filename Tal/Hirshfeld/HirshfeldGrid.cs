// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldGrid.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HirshfeldGrid — mesh density Hirshfelda.
//
// ⭐ ZMIANA v3 (flat arrays — performance):
// All density arrays (W, RhoPro, RhoProm_Outer) are now
// FLAT: float[] of length nx*ny*nz instead of float[nx,ny,nz].
//
// Uzasadnienie:
// - float[,,] in .NET is NOT a contiguous block — it is a jagged array:
// float[,,] → float[][,] → float[][] → float[]
// Each [i,j,k] access is 3 pointer dereferences.
// - float[] with manual indexing is 1 pointer dereference.
// - Flat array has better cache locality (prefetch works linearly).
// - Flat array allows SIMD (Vector<float> on a contiguous buffer).
// - One large object instead of hundreds of small ones — less GC pressure.
//
// Layout: idx = i + nx * (j + ny * k)
// - Fastest iteration over k (innermost loop) — sequential access
// - Iteracja after j — stride nx
// - Iteracja after i — stride nx*ny
//
// To jest column-major for (i,j,k) z k jako najszybszym indexem.
//
// ⭐ ZMIANA v2 (semantyka — zachowana):
// Fallback for emptych voxels: 0.5f → 0f.
//
// Uzasadnienie:
// - Value 0.5 means the "true Hirshfeld surface" (rho_pro = rho_outer).
// - Empty voxels (prom < EPS) are NOT surface — that is true vacuum
// outside the range of all Gaussians.
// - Fallback 0.5 gave false "≈ 0.5" in ~67% of voxels (the entire grid vacuum),
// which caused Marching Cubes to generate false triangles.
// - Fallback 0 jest correct: vacuum = w = 0 = "outside target molecule".

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Tal.Cavity;
using SysVector3 = System.Numerics.Vector3;

namespace Tal.Hirshfeld
{
 /// <summary>
 /// Hirshfeld grid — wrapper over VoxelScanner.VoxelGrid + flat arrays
 /// density (W, RhoPro, RhoProm_Outer).
 ///
 /// ⭐ v3: all density arrays are FLAT (float[]).
 /// Indeksowanie przez Idx(i, j, k).
 /// </summary>
    public class HirshfeldGrid
    {
        #region Grid reference

 /// <summary>Siatka voxels — z VoxelScanner.CreateCrystalGrid.</summary>
        public VoxelScanner.VoxelGrid Grid;

        #endregion

        #region Flat density arrays

 /// <summary>
 /// Relative pro-molecule density w(r) ∈ [0, 1].
 /// Flat: idx = i + Nx * (j + Ny * k).
 /// </summary>
        public float[] W;

 /// <summary>
 /// Density from target atoms.
 /// Flat: idx = i + Nx * (j + Ny * k).
 /// </summary>
        public float[] RhoPro;

 /// <summary>
 /// Density from neighbours (NON-target).
 /// Flat: idx = i + Nx * (j + Ny * k).
 /// </summary>
        public float[] RhoProm_Outer;

        #endregion

        #region Dimensions

        public int ResX => Grid.ResX;
        public int ResY => Grid.ResY;
        public int ResZ => Grid.ResZ;

 /// <summary>Total voxel count = Nx * Ny * Nz.</summary>
        public int Length => ResX * ResY * ResZ;

 /// <summary>Stride for osi Y (przeskok between j i j+1).</summary>
        public int StrideY => ResX;

 /// <summary>Stride for osi Z (przeskok between k i k+1).</summary>
        public int StrideZ => ResX * ResY;

        public SysVector3 BoundsMin => Grid.BoundsMin;
        public SysVector3 BoundsMax => Grid.BoundsMax;

        #endregion

        #region Indexing

 /// <summary>
 /// ⭐ Flat indexing: idx = i + Nx * (j + Ny * k).
 ///
 /// Layout column-major z k jako najszybszym indexem:
 /// - iteracja after k → sequential access (najlepszy cache)
 /// - iteracja after j → stride Nx
 /// - iteracja after i → stride Nx*Ny
 ///
 /// JIT should inline this method. If not — use
 /// directly formulas in hot loops.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Idx(int i, int j, int k)
        {
            return i + ResX * (j + ResY * k);
        }

 /// <summary>
 /// ⭐ Wersja z pre-kalkulowanymi stride'ami — szybsza w hot loopach
 /// when Nx/Ny are already hoisted to locals.
 /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IdxFast(int i, int j, int k, int nx, int ny)
        {
            return i + nx * (j + ny * k);
        }

        #endregion

        #region Position

 /// <summary>
 /// Returns the Cartesian position of voxel (i, j, k).
 /// </summary>
        public Vector3 GetPosition(int i, int j, int k)
        {
            var p = Grid.GetPosition(i, j, k);
            return new Vector3(p.X, p.Y, p.Z);
        }

 /// <summary>
 /// ⭐ v3: Returns the Cartesian position of a voxel from a flat index.
 /// Splits idx into (i, j, k) and delegates to Grid.GetPosition.
 /// </summary>
        public Vector3 GetPositionFromFlat(int idx)
        {
            int nx = ResX;
            int ny = ResY;

            int k = idx / (nx * ny);
            int rem = idx - k * nx * ny;
            int j = rem / nx;
            int i = rem - j * nx;

            return GetPosition(i, j, k);
        }

        #endregion

        #region Allocation

 /// <summary>
 /// Allocates flat arrays W, RhoPro, RhoProm_Outer.
 /// Zeroed by default by .NET (new float[] yields zeros).
 /// </summary>
        public void Allocate()
        {
            int len = Length;

            W = new float[len];
            RhoPro = new float[len];
            RhoProm_Outer = new float[len];
        }

 /// <summary>
 /// ⭐ v3: Zeruje all tablice density BEZ realokacji.
 /// Faster than new float[] when reusing the grid.
 /// </summary>
        public void Clear()
        {
            W?.AsSpan().Clear();
            RhoPro?.AsSpan().Clear();
            RhoProm_Outer?.AsSpan().Clear();
        }

        #endregion

        #region Compute relative density

 /// <summary>
 /// Calculates w = rho_pro / (rho_pro + rho_outer) in every voxel.
 /// Call after filling RhoPro and RhoProm_Outer.
 ///
 /// ⭐ v3:
 /// - Flat arrays — linear iteration over the whole buffer.
 /// - SIMD-friendly: no dependencies between successive idx.
 /// - Dzielenie float — jedyny "koszt", ale to jedno na voxel.
 /// </summary>
        public void ComputeRelativeDensity()
        {
            int len = Length;
            if (len == 0) return;

            float[] pro = RhoPro;
            float[] outer = RhoProm_Outer;
            float[] w = W;

            const float EPS = 1e-10f;

 // ⭐ Iteracja liniowa — sequential access do all tablic.
 // Prefetch works ideally, no strides.
 // Parallel.For z zakresem [0, len) zamiast [0, nx).
            int maxParallel = Math.Max(1, Environment.ProcessorCount - 1);

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = maxParallel
            };

 // Split into blocks so each thread gets a contiguous memory range.
            int chunkSize = Math.Max(1024, len / (maxParallel * 8));
            int numChunks = (len + chunkSize - 1) / chunkSize;

            Parallel.For(0, numChunks, options, chunk =>
            {
                int start = chunk * chunkSize;
                int end = Math.Min(start + chunkSize, len);

                for (int idx = start; idx < end; idx++)
                {
                    float p = pro[idx];
                    float o = outer[idx];
                    float prom = p + o;

 // Fallback 0f for emptych voxels (v2)
                    w[idx] = prom > EPS ? p / prom : 0f;
                }
            });
        }

 /// <summary>
 /// ⭐ v3: Single-thread version (for debug / small grids).
 /// </summary>
        public void ComputeRelativeDensitySingleThread()
        {
            int len = Length;
            if (len == 0) return;

            float[] pro = RhoPro;
            float[] outer = RhoProm_Outer;
            float[] w = W;

            const float EPS = 1e-10f;

            for (int idx = 0; idx < len; idx++)
            {
                float p = pro[idx];
                float o = outer[idx];
                float prom = p + o;
                w[idx] = prom > EPS ? p / prom : 0f;
            }
        }

        #endregion

        #region Info

 /// <summary>
 /// Sumaryczne info debug.
 /// </summary>
        public string GetInfo()
        {
            int len = Length;
            double memMB = len * 4.0 * 3 / 1024.0 / 1024.0;

            return $"Grid {ResX}×{ResY}×{ResZ} " +
                   $"({len:N0} voxels, ~{memMB:F1} MB flat)";
        }

        #endregion

        #region Migration helpers (opcjonalne — ułatwiają przejście z [,,])

 /// <summary>
 /// ⭐ v3: Helper do conversion starego kodu [i,j,k] na flat.
 /// Use only in transitional code — in hot loops use Idx() directly.
 /// </summary>
        public float GetW(int i, int j, int k) => W[Idx(i, j, k)];
        public float GetRhoPro(int i, int j, int k) => RhoPro[Idx(i, j, k)];
        public float GetRhoOuter(int i, int j, int k) => RhoProm_Outer[Idx(i, j, k)];

        public void SetW(int i, int j, int k, float v) => W[Idx(i, j, k)] = v;
        public void SetRhoPro(int i, int j, int k, float v) => RhoPro[Idx(i, j, k)] = v;
        public void SetRhoOuter(int i, int j, int k, float v) => RhoProm_Outer[Idx(i, j, k)] = v;

        #endregion
    }
}