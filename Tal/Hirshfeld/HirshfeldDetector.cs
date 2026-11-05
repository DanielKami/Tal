// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hirshfeld surface module: HirshfeldDetector.

﻿// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// HirshfeldDetector v4 — STO (LUT) + Gaussian fallback.
//
// ⭐ v4 ZMIANY:
// - FillDensity uses ALL cores (was: ProcessorCount - 1).
// - Round-robin partition atoms → even thread load.
// - Fallback single-thread for small grids.
// - OOM fallback → half the threads.
// - Grid blur DISABLED by default (unless the user explicitly sets it).
// - FillDensitySto zoptymalizowane (inline, cutoff per-i-j).
//
// Historia:
// v3 — STO + Gaussian fallback, threadCount = ProcessorCount - 1.
// v4 — full core usage + sharper surface.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Tal.Cavity;
using Tal.Hirshfeld.Sto;
using SysVector3 = System.Numerics.Vector3;

namespace Tal.Hirshfeld
{
    public class HirshfeldDetector
    {
        #region Configuration

        public const float DEFAULT_NEIGHBOR_CUTOFF = HirshfeldParameters.CUT_OFF;

        private const long FILL_DENSITY_MEMORY_BUDGET = 512L * 1024 * 1024;

        #endregion

        #region Types

        private struct AtomEntry
        {
            public int AtomIdx;
            public int MoleculeId;
            public Vector3 Pos;
            public AtomGaussParams Gauss;
            public AtomStoParams Sto;
            public bool HasSto;
            public bool IsTarget;
            public int CellShiftX;
            public int CellShiftY;
            public int CellShiftZ;
        }

        #endregion

        #region Fields

        private readonly Molecule _molecule;
        private HirshfeldScene _scene;
        private HirshfeldGrid _grid;
        private HirshfeldSurface _surface;

        private readonly List<AtomEntry> _allAtoms = new List<AtomEntry>();

        public float BlurSigma { get; set; } = 0f;   // ⭐ domyślnie 0
        public int BlurIterations { get; set; } = 0; // ⭐ domyślnie 0

 /// <summary>
 /// Density computation mode: STO (by default) or Gaussian.
 /// </summary>
        public bool UseSto { get; set; } = true;

        public HirshfeldSurface Surface => _surface;
        public HirshfeldGrid Grid => _grid;
        public HirshfeldScene Scene => _scene;

        private static readonly Stopwatch _globalSw = Stopwatch.StartNew();
        private static string TS => $"[t={_globalSw.ElapsedMilliseconds,7}ms]";

        #endregion

        #region Constructor

        public HirshfeldDetector(Molecule molecule)
        {
            _molecule = molecule ?? throw new ArgumentNullException(nameof(molecule));
        }

        #endregion

        #region Logging helpers

        private static void Log(string msg)
            => Debug.WriteLine($"{TS} [HirshfeldDetector] {msg}");

        private static void LogStep(string step, string detail = "")
            => Debug.WriteLine($"{TS} [HirshfeldDetector] ═══ {step} {detail}");

        private static void LogDone(string step, long elapsedMs, string detail = "")
            => Debug.WriteLine($"{TS} [HirshfeldDetector] ✓ {step} ({elapsedMs} ms) {detail}");

        #endregion

        #region Public API

        public async Task<bool> GenerateAsync(
            int targetMolId,
            float voxelSize = 0.12f,
            float sigmaMultiplier = 1.0f,
            bool useAnisotropic = false,
            float isoLevel = 0.5f,
            float margin = 4.0f,
            float blurSigma = 0f,          // default 0
            int blurIterations = 0,        // default 0
            float neighborCutoff = DEFAULT_NEIGHBOR_CUTOFF,
            bool useSto = true,
            Action<string, float> progress = null,
            CancellationToken ct = default)
        {
            var totalSw = Stopwatch.StartNew();

            UseSto = useSto;

            Log($"═══════════ GENERATE START (v4) ═══════════");
            Log($"  target={targetMolId}, voxel={voxelSize:F3} Å, " +
                $"densityMode={(UseSto ? "STO (LUT)" : "Gaussian")}");
            Log($"  iso={isoLevel:F2}, margin={margin:F2}, " +
                $"blur σ={blurSigma:F2} it={blurIterations}, cutoff={neighborCutoff}");

            try
            {
                BlurSigma = blurSigma;
                BlurIterations = blurIterations;

                LogStep("KROK 1/9", "Building HirshfeldScene");
                progress?.Invoke("Building scene...", 0.02f);

                Tal.Utils.ProgressReporter.Log(
                    $"═══════════ HIRSHFELD GENERATE START ═══════════",
                    System.Drawing.Color.Cyan);
                Tal.Utils.ProgressReporter.Log(
                    $"  target={targetMolId}, voxel={voxelSize:F3} Å, " +
                    $"mode={(UseSto ? "STO" : "Gaussian")}, iso={isoLevel:F2}",
                    System.Drawing.Color.LightGray);

                var sw = Stopwatch.StartNew();
                _scene = new HirshfeldScene(_molecule);

                if (!_scene.Build(targetMolId, neighborCutoff))
                {
                    Log($"❌ Scene.Build returned false");
                    progress?.Invoke($"Molecule #{targetMolId} not found", 1.0f);
                    return false;
                }
                LogDone("Scene built", sw.ElapsedMilliseconds,
                        $"target={_scene.TargetAtoms.Count}, " +
                        $"outer={_scene.OuterAtoms.Count}, " +
                        $"R={_scene.TargetRadius:F2} Å");

                ct.ThrowIfCancellationRequested();

                LogStep("KROK 2/9", "Preparing atom entries");
                progress?.Invoke("Preparing atom parameters...", 0.06f);

                sw.Restart();
                PrepareAtomEntries(useAnisotropic, sigmaMultiplier);
                LogDone("Atom entries ready", sw.ElapsedMilliseconds,
                        $"total={_allAtoms.Count}");

                ct.ThrowIfCancellationRequested();

                LogStep("KROK 3/9", "Building density grid");
                progress?.Invoke("Building grid...", 0.10f);

                sw.Restart();
                if (!BuildGrid(voxelSize, margin, progress, ct))
                {
                    Log($"❌ BuildGrid returned false");
                    return false;
                }

                long totalVoxels = _grid.Length;
                double memMB = totalVoxels * 4.0 * 3 / 1024.0 / 1024.0;
                LogDone("Grid built", sw.ElapsedMilliseconds,
                        $"{_grid.ResX}×{_grid.ResY}×{_grid.ResZ} " +
                        $"({totalVoxels:N0} voxels, ~{memMB:F1} MB flat)");

                ct.ThrowIfCancellationRequested();

                LogStep("KROK 4/9", "Filling density grid");
                progress?.Invoke("Filling density grid...", 0.15f);

                sw.Restart();
                await Task.Run(() => FillDensity(progress, ct), ct);
                LogDone("Density filled", sw.ElapsedMilliseconds);

                ct.ThrowIfCancellationRequested();

                LogStep("KROK 5/9", "Computing w(r)");
                progress?.Invoke("Computing w(r)...", 0.78f);

                sw.Restart();
                _grid.ComputeRelativeDensity();
                LogDone("w(r) computed", sw.ElapsedMilliseconds);

                LogWRDistribution("after compute");
                ct.ThrowIfCancellationRequested();

 // ⭐ Blur — only if user jawnie seti (by default OFF)
                if (BlurSigma > 0.01f && BlurIterations > 0)
                {
                    LogStep("KROK 6/9",
                        $"Blur pass 1 (σ={BlurSigma:F2}) — ⚠ rounds the surface");
                    progress?.Invoke($"Blur pass 1...", 0.79f);

                    sw.Restart();
                    await Task.Run(() => BlurDensityField(_grid, BlurSigma, BlurIterations, ct), ct);
                    LogDone("Blur pass 1", sw.ElapsedMilliseconds);

                    ct.ThrowIfCancellationRequested();

                    if (BlurIterations >= 2)
                    {
                        LogStep("KROK 6b/9",
                            $"Blur pass 2 (σ={BlurSigma * 1.8f:F2}) — ⚠ rounds");
                        progress?.Invoke($"Blur pass 2...", 0.805f);

                        sw.Restart();
                        await Task.Run(() => BlurDensityField(_grid, BlurSigma * 1.8f, 1, ct), ct);
                        LogDone("Blur pass 2", sw.ElapsedMilliseconds);

                        ct.ThrowIfCancellationRequested();
                    }
                }
                else
                {
                    LogStep("KROK 6/9", "Blur SKIPPED — ostra powierzchnia");
                }

                LogWRDistribution("before MC");

                LogStep("KROK 7/9", "Marching Cubes");
                progress?.Invoke("Extracting surface...", 0.82f);

                sw.Restart();
                _surface = HirshfeldMarchingCubes.Extract(
                    _grid, isoLevel: isoLevel,
                    smoothingIterations: 0, smoothingFactor: 0f,
                    progress: progress, ct: ct);

                if (_surface == null || !_surface.IsValid)
                {
                    Log($"❌ MC returned empty/invalid surface");
                    progress?.Invoke("Surface extraction failed", 1.0f);
                    return false;
                }

                LogDone("MC extraction", sw.ElapsedMilliseconds,
                        $"{_surface.VertexCount} verts, {_surface.TriangleCount} tris");

                LogStep("KROK 8/9", "Attaching surface metadata");
                _surface.TargetMoleculeId = targetMolId;
                _surface.TargetAtomCount = _scene.TargetAtoms.Count;
                _surface.OuterAtomCount = _scene.OuterAtoms.Count;
                _surface.Center = _scene.TargetCenter;
                _surface.UsedAnisotropic = useAnisotropic;
                _surface.ComputeBoundingRadius();

                ct.ThrowIfCancellationRequested();

                LogStep("KROK 9/9", "Computing d_e / d_i / d_norm");
                progress?.Invoke("Mapping d_e / d_i / d_norm...", 0.92f);

                sw.Restart();
                HirshfeldProperties.Compute(
                    _surface, _scene.TargetAtoms, _scene.OuterAtoms,
                    _molecule, progress, ct);

                LogDone("Properties computed", sw.ElapsedMilliseconds);

                progress?.Invoke("Done", 1.0f);

                Log($"═══════════ GENERATE DONE (total {totalSw.ElapsedMilliseconds} ms) ═══════════");
                Log($"  {_surface.GetInfo()}");

                Tal.Utils.ProgressReporter.Log(
                    $"═══════════ HIRSHFELD GENERATE DONE ═══════════",
                    System.Drawing.Color.Cyan);
                Tal.Utils.ProgressReporter.Log(
                    $"  {_surface.VertexCount} verts, {_surface.TriangleCount} tris",
                    System.Drawing.Color.LightGreen);
                Tal.Utils.ProgressReporter.Log(
                    $"  total: {totalSw.ElapsedMilliseconds} ms",
                    System.Drawing.Color.LightGreen);

                return true;
            }
            catch (OperationCanceledException)
            {
                Log($"⚠️ CANCELLED");
                progress?.Invoke("Cancelled", 1.0f);
                return false;
            }
            catch (OutOfMemoryException ex)
            {
                Log($"❌ OOM: {ex.Message}");
                progress?.Invoke("Out of memory", 1.0f);
                return false;
            }
            catch (Exception ex)
            {
                Log($"❌ EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                Log($"   Stack: {ex.StackTrace}");
                progress?.Invoke($"Error: {ex.Message}", 1.0f);
                return false;
            }
        }

        #endregion

        #region Diagnostics

        private void LogWRDistribution(string label)
        {
            if (_grid?.W == null) return;

            int len = _grid.Length;
            int step = Math.Max(1, len / 100000);

            int zero = 0, below = 0, around = 0, above = 0, one = 0, total = 0;
            float wmin = 1f, wmax = 0f;

            var w = _grid.W;

            for (int idx = 0; idx < len; idx += step)
            {
                float wv = w[idx];
                total++;
                if (wv < wmin) wmin = wv;
                if (wv > wmax) wmax = wv;

                if (wv <= 0.001f) zero++;
                else if (wv < 0.45f) below++;
                else if (wv > 0.999f) one++;
                else if (wv > 0.55f) above++;
                else around++;
            }

            Log($"  w(r) [{label}] sampled {total} voxels:");
            Log($"    = 0: {100f * zero / total:F1}%   " +
                $"< 0.45: {100f * below / total:F1}%   " +
                $"≈ 0.5: {100f * around / total:F1}%   " +
                $"> 0.55: {100f * above / total:F1}%   " +
                $"= 1: {100f * one / total:F1}%");
            Log($"    range: [{wmin:F3}, {wmax:F3}]");
        }

        #endregion

        #region Prepare AtomEntries

        private void PrepareAtomEntries(bool useAnisotropic, float sigmaMultiplier)
        {
            _allAtoms.Clear();

            int stoTarget = 0, gaussTarget = 0;
            int stoOuter = 0, gaussOuter = 0;

            Log("  Preparing target atoms...");

            foreach (var sa in _scene.TargetAtoms)
            {
                var entry = BuildAtomEntry(sa, isTarget: true,
                                            useAnisotropic, sigmaMultiplier,
                                            ref stoTarget, ref gaussTarget);
                _allAtoms.Add(entry);
            }

            Log($"  ✓ {_scene.TargetAtoms.Count} target atoms prepared " +
                $"(STO={stoTarget}, Gaussian={gaussTarget})");
            Log("  Preparing outer atoms...");

            foreach (var sa in _scene.OuterAtoms)
            {
                var entry = BuildAtomEntry(sa, isTarget: false,
                                            useAnisotropic, sigmaMultiplier,
                                            ref stoOuter, ref gaussOuter);
                _allAtoms.Add(entry);
            }

            Log($"  ✓ {_scene.OuterAtoms.Count} outer atoms prepared " +
                $"(STO={stoOuter}, Gaussian={gaussOuter})");

            int totalSto = stoTarget + stoOuter;
            int totalGauss = gaussTarget + gaussOuter;

            Log($"  Total: STO={totalSto}, Gaussian={totalGauss}, " +
                $"entries={_allAtoms.Count}");
        }

        private AtomEntry BuildAtomEntry(
            SceneAtom sa,
            bool isTarget,
            bool useAnisotropic,
            float sigmaMultiplier,
            ref int stoCount,
            ref int gaussCount)
        {
 // ⭐ Try STO — if the database is loaded and the LUT exists
            if (UseSto && StoDatabase.IsLoaded)
            {
                string symbol = StoDatabase.AtomicNumberToSymbol(sa.AtomicNumber);
                if (!string.IsNullOrEmpty(symbol))
                {
                    var lut = StoDatabase.GetLut(symbol, step: 0.02f);
                    if (lut != null)
                    {
                        stoCount++;
                        return new AtomEntry
                        {
                            AtomIdx = sa.SymmetryAtomIndex,
                            MoleculeId = isTarget ? _scene.TargetMoleculeId : -1,
                            Pos = sa.CartPosition,
                            Sto = new AtomStoParams(lut, sa.CartPosition, symbol),
                            HasSto = true,
                            IsTarget = isTarget,
                            CellShiftX = sa.CellShiftX,
                            CellShiftY = sa.CellShiftY,
                            CellShiftZ = sa.CellShiftZ,
                        };
                    }
                }
            }

 // ⭐ Fallback: Gaussian
            gaussCount++;

            var atomSimple = new Molecule.AtomSimple
            {
                AtomicNumber = sa.AtomicNumber,
                name = sa.Name,
                Un = sa.Un,
                Ue = sa.Un,
                isAnisotropic = sa.IsAnisotropic,
                x = sa.FracPosition.X,
                y = sa.FracPosition.Y,
                z = sa.FracPosition.Z,
                occupancy = sa.Occupancy,
                CellShiftX = sa.CellShiftX,
                CellShiftY = sa.CellShiftY,
                CellShiftZ = sa.CellShiftZ,
                IndividualMoleculeInCell = -1
            };

            var gauss = HirshfeldParameters.BuildFromAtomSimple(
                atomSimple, sa.CartPosition, sa.SymmetryAtomIndex,
                useAnisotropic, sigmaMultiplier);

            return new AtomEntry
            {
                AtomIdx = sa.SymmetryAtomIndex,
                MoleculeId = isTarget ? _scene.TargetMoleculeId : -1,
                Pos = sa.CartPosition,
                Gauss = gauss,
                HasSto = false,
                IsTarget = isTarget,
                CellShiftX = sa.CellShiftX,
                CellShiftY = sa.CellShiftY,
                CellShiftZ = sa.CellShiftZ,
            };
        }

        #endregion

        #region Grid building

        private bool BuildGrid(
            float voxelSize, float margin,
            Action<string, float> progress, CancellationToken ct)
        {
            var targetPositions = _allAtoms
                .Where(a => a.IsTarget)
                .Select(a => a.Pos)
                .ToList();

            if (targetPositions.Count == 0)
            {
                Log("❌ No target positions for grid");
                return false;
            }

            Vector3 bboxMin = new Vector3(float.MaxValue);
            Vector3 bboxMax = new Vector3(float.MinValue);

            foreach (var p in targetPositions)
            {
                bboxMin = Vector3.Min(bboxMin, p);
                bboxMax = Vector3.Max(bboxMax, p);
            }

            Vector3 center = (bboxMin + bboxMax) * 0.5f;

            float rmax = 0f;
            foreach (var p in targetPositions)
            {
                float d = (p - center).Length();
                if (d > rmax) rmax = d;
            }

            float R = rmax + margin;

            Log($"  bbox={bboxMax - bboxMin}, center={center}");
            Log($"  rmax_target={rmax:F3} Å, margin={margin:F2} Å, R={R:F3} Å");

            SysVector3 minCorner = new SysVector3(center.X - R, center.Y - R, center.Z - R);

            int n = (int)Math.Ceiling(2 * R / voxelSize);
            if (n < 8) n = 8;

            int nx = n, ny = n, nz = n;

            long totalVoxels = (long)nx * ny * nz;
            double memMB = totalVoxels * 4.0 * 3 / 1024.0 / 1024.0;

            Log($"  grid size: {nx}×{ny}×{nz} = {totalVoxels:N0} voxels (~{memMB:F1} MB)");

            if (totalVoxels > Flags.MAX_VOXELS)
            {
                Log($"❌ Grid too large");
                progress?.Invoke($"Grid too large", 1.0f);
                return false;
            }

            var grid = new VoxelScanner.VoxelGrid
            {
                FracX = new float[nx, ny, nz],
                FracY = new float[nx, ny, nz],
                FracZ = new float[nx, ny, nz],
                ResX = (short)Math.Min(nx, short.MaxValue),
                ResY = (short)Math.Min(ny, short.MaxValue),
                ResZ = (short)Math.Min(nz, short.MaxValue),
                BoundsMin = minCorner,
                BoundsMax = new SysVector3(center.X + R, center.Y + R, center.Z + R),
                IsFine = true,
                Origin = minCorner,
                VecA = new SysVector3(2 * R, 0, 0),
                VecB = new SysVector3(0, 2 * R, 0),
                VecC = new SysVector3(0, 0, 2 * R),
            };

            for (int i = 0; i < nx; i++)
                for (int j = 0; j < ny; j++)
                    for (int k = 0; k < nz; k++)
                    {
                        grid.FracX[i, j, k] = i / (float)(nx - 1);
                        grid.FracY[i, j, k] = j / (float)(ny - 1);
                        grid.FracZ[i, j, k] = k / (float)(nz - 1);
                    }

            _grid = new HirshfeldGrid { Grid = grid };
            _grid.Allocate();

            Log($"  ✓ Grid allocated, voxel ≈ {2 * R / (nx - 1):F4} Å");
            return true;
        }

        #endregion

        #region Gaussian Blur

        private static void BlurDensityField(
            HirshfeldGrid grid, float sigma, int iterations,
            CancellationToken ct = default)
        {
            if (grid?.W == null) return;
            if (sigma <= 0.01f || iterations <= 0) return;

            int nx = grid.ResX;
            int ny = grid.ResY;
            int nz = grid.ResZ;

            float[] w = grid.W;

            int kernelRadius = (int)Math.Ceiling(sigma * 4.0f);
            if (kernelRadius < 1) kernelRadius = 1;
            if (kernelRadius > 30) kernelRadius = 30;

            int kSize = 2 * kernelRadius + 1;
            var kernel = new float[kSize];
            float twoSigmaSq = 2.0f * sigma * sigma;
            float sum = 0f;

            for (int i = 0; i < kSize; i++)
            {
                int x = i - kernelRadius;
                kernel[i] = MathF.Exp(-(x * x) / twoSigmaSq);
                sum += kernel[i];
            }

            for (int i = 0; i < kSize; i++)
                kernel[i] /= sum;

            Log($"    Blur: grid={nx}×{ny}×{nz}, σ={sigma:F3}, " +
                $"kernel={kSize}, it={iterations}");

            var tmp = new float[w.Length];

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            };

            int nxLocal = nx, nyLocal = ny, nzLocal = nz;

            for (int iter = 0; iter < iterations; iter++)
            {
                ct.ThrowIfCancellationRequested();
                var iterSw = Stopwatch.StartNew();

 // X pass
                Parallel.For(0, nyLocal, options, j =>
                {
                    int jStride = nxLocal * j;
                    for (int k = 0; k < nzLocal; k++)
                    {
                        int kStride = nxLocal * nyLocal * k;
                        int rowBase = jStride + kStride;

                        for (int i = 0; i < nxLocal; i++)
                        {
                            float acc = 0f;
                            for (int t = -kernelRadius; t <= kernelRadius; t++)
                            {
                                int ii = MirrorIndex(i + t, nxLocal);
                                acc += w[rowBase + ii] * kernel[t + kernelRadius];
                            }
                            tmp[rowBase + i] = acc;
                        }
                    }
                });

 // Y pass
                Parallel.For(0, nxLocal, options, i =>
                {
                    for (int k = 0; k < nzLocal; k++)
                    {
                        int kStride = nxLocal * nyLocal * k;
                        for (int j = 0; j < nyLocal; j++)
                        {
                            int rowBase = i + nxLocal * j + kStride;
                            float acc = 0f;
                            for (int t = -kernelRadius; t <= kernelRadius; t++)
                            {
                                int jj = MirrorIndex(j + t, nyLocal);
                                int idx = i + nxLocal * jj + kStride;
                                acc += tmp[idx] * kernel[t + kernelRadius];
                            }
                            w[rowBase] = acc;
                        }
                    }
                });

 // Z pass
                Parallel.For(0, nxLocal, options, i =>
                {
                    for (int j = 0; j < nyLocal; j++)
                    {
                        int baseIdx = i + nxLocal * j;
                        for (int k = 0; k < nzLocal; k++)
                        {
                            float acc = 0f;
                            for (int t = -kernelRadius; t <= kernelRadius; t++)
                            {
                                int kk = MirrorIndex(k + t, nzLocal);
                                int idx = baseIdx + nxLocal * nyLocal * kk;
                                acc += w[idx] * kernel[t + kernelRadius];
                            }
                            tmp[baseIdx + nxLocal * nyLocal * k] = acc;
                        }
                    }
                });

                int len = w.Length;
                int chunkSize = Math.Max(4096, len / (Environment.ProcessorCount * 4));
                int numChunks = (len + chunkSize - 1) / chunkSize;

                Parallel.For(0, numChunks, options, chunk =>
                {
                    int start = chunk * chunkSize;
                    int end = Math.Min(start + chunkSize, len);
                    Array.Copy(tmp, start, w, start, end - start);
                });

                Log($"    Iter {iter + 1}/{iterations} done ({iterSw.ElapsedMilliseconds} ms)");
            }
        }

        private static int MirrorIndex(int idx, int n)
        {
            if (n <= 1) return 0;
            if (idx < 0) return -idx;
            if (idx >= n) return 2 * n - idx - 2;
            return idx;
        }

        #endregion

        #region Density filling

        private void FillDensity(Action<string, float> progress, CancellationToken ct)
        {
            int nx = _grid.ResX;
            int ny = _grid.ResY;
            int nz = _grid.ResZ;
            int flatLen = nx * ny * nz;

            float step = (float)(_grid.BoundsMax.X - _grid.BoundsMin.X) / (nx - 1);
            float invStep = 1.0f / step;

            int totalAtoms = _allAtoms.Count;

            Log($"  Filling {totalAtoms} atoms into {nx}×{ny}×{nz} grid");

            var sw = Stopwatch.StartNew();

 // ⭐ Optymalizacja threads — we use WSZYSTKICH rdzeni
            int threadCount = Environment.ProcessorCount;
            if (threadCount < 1) threadCount = 1;
            if (threadCount > totalAtoms) threadCount = Math.Max(1, totalAtoms);

            long bytesPerThread = (long)flatLen * 4L * 2L;
            long totalBytes = bytesPerThread * threadCount;

            Log($"  Parallel config: threads={threadCount}, " +
                $"bytes/thread={bytesPerThread / 1024.0 / 1024.0:F1} MB, " +
                $"total={totalBytes / 1024.0 / 1024.0:F1} MB");

            if (threadCount <= 1)
            {
                Log($"  → single-thread mode");
                FillDensitySingleThread(progress, ct, sw);
                Log($"  ✓ Density filled in {sw.ElapsedMilliseconds} ms (single-thread)");
                return;
            }

            var localRhoPro = new float[threadCount][];
            var localRhoOuter = new float[threadCount][];

            try
            {
                for (int t = 0; t < threadCount; t++)
                {
                    localRhoPro[t] = new float[flatLen];
                    localRhoOuter[t] = new float[flatLen];
                }
            }
            catch (OutOfMemoryException)
            {
                Log($"  ⚠ OOM at {threadCount} threads, trying with fewer");

                threadCount = Math.Max(1, threadCount / 2);

                localRhoPro = new float[threadCount][];
                localRhoOuter = new float[threadCount][];

                for (int t = 0; t < threadCount; t++)
                {
                    localRhoPro[t] = new float[flatLen];
                    localRhoOuter[t] = new float[flatLen];
                }

                Log($"  → fallback: threads={threadCount}");
            }

            float bminX = _grid.BoundsMin.X;
            float bminY = _grid.BoundsMin.Y;
            float bminZ = _grid.BoundsMin.Z;

            int nxL = nx, nyL = ny, nzL = nz;
            int strideZ = nx * ny;

 // ⭐ Round-robin partition — even load
            var threadIndices = new List<int>[threadCount];
            for (int t = 0; t < threadCount; t++)
                threadIndices[t] = new List<int>(totalAtoms / threadCount + 1);

            for (int i = 0; i < totalAtoms; i++)
                threadIndices[i % threadCount].Add(i);

            int atomsDone = 0;
            int lastReportedPercent = -1;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = threadCount,
                CancellationToken = ct
            };

 // ⭐ Main loop — Parallel.For over threads
            Parallel.For(0, threadCount, parallelOptions, threadIdx =>
            {
                var myIndices = threadIndices[threadIdx];
                var rhoProLocal = localRhoPro[threadIdx];
                var rhoOuterLocal = localRhoOuter[threadIdx];

                foreach (int atomIdx in myIndices)
                {
                    ct.ThrowIfCancellationRequested();

                    var entry = _allAtoms[atomIdx];
                    Vector3 pos = entry.Pos;
                    bool isTarget = entry.IsTarget;

                    float posX = pos.X, posY = pos.Y, posZ = pos.Z;

                    var rhoTarget = isTarget ? rhoProLocal : rhoOuterLocal;

                    if (entry.HasSto)
                    {
                        FillDensitySto(entry.Sto, rhoTarget,
                                       posX, posY, posZ,
                                       bminX, bminY, bminZ,
                                       invStep, step,
                                       nxL, nyL, nzL, strideZ);
                    }
                    else
                    {
                        FillDensityGauss(entry.Gauss, rhoTarget,
                                          posX, posY, posZ,
                                          bminX, bminY, bminZ,
                                          invStep, step,
                                          nxL, nyL, nzL, strideZ);
                    }

                    int done = Interlocked.Increment(ref atomsDone);

                    if (done % 4 == 0 || done == totalAtoms)
                    {
                        int pct = (int)(100f * done / totalAtoms);
                        if (pct != lastReportedPercent)
                        {
                            lastReportedPercent = pct;
                            float p = 0.15f + 0.60f * done / (float)totalAtoms;
                            progress?.Invoke($"Filling density ({done}/{totalAtoms})", p);
                        }
                    }
                }
            });

            Log($"  Summing {threadCount} per-thread buffers...");
            var sumSw = Stopwatch.StartNew();

            var rhoProFinal = _grid.RhoPro;
            var rhoOuterFinal = _grid.RhoProm_Outer;

            int sumChunkSize = Math.Max(4096, flatLen / (threadCount * 4));
            int sumChunks = (flatLen + sumChunkSize - 1) / sumChunkSize;

            Parallel.For(0, sumChunks, parallelOptions, chunk =>
            {
                int start = chunk * sumChunkSize;
                int end = Math.Min(start + sumChunkSize, flatLen);

                for (int idx = start; idx < end; idx++)
                {
                    float proSum = 0f;
                    float outerSum = 0f;

                    for (int t = 0; t < threadCount; t++)
                    {
                        proSum += localRhoPro[t][idx];
                        outerSum += localRhoOuter[t][idx];
                    }

                    rhoProFinal[idx] = proSum;
                    rhoOuterFinal[idx] = outerSum;
                }
            });

            Log($"  ✓ Summing done ({sumSw.ElapsedMilliseconds} ms)");
            Log($"  ✓ Density filled in {sw.ElapsedMilliseconds} ms");
        }

        private static void FillDensitySto(
            AtomStoParams sto, float[] rhoTarget,
            float posX, float posY, float posZ,
            float bminX, float bminY, float bminZ,
            float invStep, float step,
            int nxL, int nyL, int nzL, int strideZ)
        {
            float cutoffSq = sto.CutoffSq;
            float cutoff = MathF.Sqrt(cutoffSq);

            int i0 = Math.Max(0, (int)MathF.Floor((posX - cutoff - bminX) * invStep));
            int i1 = Math.Min(nxL - 1, (int)MathF.Ceiling((posX + cutoff - bminX) * invStep));
            int j0 = Math.Max(0, (int)MathF.Floor((posY - cutoff - bminY) * invStep));
            int j1 = Math.Min(nyL - 1, (int)MathF.Ceiling((posY + cutoff - bminY) * invStep));
            int k0 = Math.Max(0, (int)MathF.Floor((posZ - cutoff - bminZ) * invStep));
            int k1 = Math.Min(nzL - 1, (int)MathF.Ceiling((posZ + cutoff - bminZ) * invStep));

            if (i0 > i1 || j0 > j1 || k0 > k1) return;

 // ⭐ Zoptymalizowana loop:
 // - cutoff checksny raz per (i,j), nie per (i,j,k)
 // - sequential access do rhoTarget in the inner k loop
            for (int i = i0; i <= i1; i++)
            {
                float px = bminX + i * step;
                float dx = px - posX;
                float dx2 = dx * dx;
                if (dx2 > cutoffSq) continue;

                for (int j = j0; j <= j1; j++)
                {
                    float py = bminY + j * step;
                    float dy = py - posY;
                    float dxy2 = dx2 + dy * dy;
                    if (dxy2 > cutoffSq) continue;

                    float r2Rem = cutoffSq - dxy2;
                    float r = MathF.Sqrt(r2Rem);

                    int kStart = (int)MathF.Ceiling((posZ - r - bminZ) * invStep);
                    int kEnd = (int)MathF.Floor((posZ + r - bminZ) * invStep);

                    if (kStart < k0) kStart = k0;
                    if (kEnd > k1) kEnd = k1;
                    if (kStart > kEnd) continue;

                    int rowBase = i + nxL * j;
                    float pzStart = bminZ + kStart * step;

                    for (int k = kStart; k <= kEnd; k++)
                    {
                        float dz = pzStart + (k - kStart) * step - posZ;
                        float d2 = dxy2 + dz * dz;
                        if (d2 > cutoffSq) continue;

                        float rho = sto.EvaluateD2(d2);
                        rhoTarget[rowBase + strideZ * k] += rho;
                    }
                }
            }
        }

        private static void FillDensityGauss(
            AtomGaussParams gauss, float[] rhoTarget,
            float posX, float posY, float posZ,
            float bminX, float bminY, float bminZ,
            float invStep, float step,
            int nxL, int nyL, int nzL, int strideZ)
        {
            float cutoff = gauss.CutoffRadius;
            float cutoffSq = cutoff * cutoff;

            int i0 = Math.Max(0, (int)MathF.Floor((posX - cutoff - bminX) * invStep));
            int i1 = Math.Min(nxL - 1, (int)MathF.Ceiling((posX + cutoff - bminX) * invStep));
            int j0 = Math.Max(0, (int)MathF.Floor((posY - cutoff - bminY) * invStep));
            int j1 = Math.Min(nyL - 1, (int)MathF.Ceiling((posY + cutoff - bminY) * invStep));
            int k0 = Math.Max(0, (int)MathF.Floor((posZ - cutoff - bminZ) * invStep));
            int k1 = Math.Min(nzL - 1, (int)MathF.Ceiling((posZ + cutoff - bminZ) * invStep));

            if (i0 > i1 || j0 > j1 || k0 > k1) return;

            float norm = gauss.NormFactor;

            if (!gauss.IsAnisotropic)
            {
                float invSigSq = gauss.InvSigmaSq;
                float halfInvSigSq = 0.5f * invSigSq;

                for (int i = i0; i <= i1; i++)
                {
                    float px = bminX + i * step;
                    float dx = px - posX;
                    float dx2 = dx * dx;
                    if (dx2 > cutoffSq) continue;

                    for (int j = j0; j <= j1; j++)
                    {
                        float py = bminY + j * step;
                        float dy = py - posY;
                        float dxy2 = dx2 + dy * dy;
                        if (dxy2 > cutoffSq) continue;

                        float r2Rem = cutoffSq - dxy2;
                        float r = MathF.Sqrt(r2Rem);

                        int kStart = (int)MathF.Ceiling((posZ - r - bminZ) * invStep);
                        int kEnd = (int)MathF.Floor((posZ + r - bminZ) * invStep);

                        if (kStart < k0) kStart = k0;
                        if (kEnd > k1) kEnd = k1;
                        if (kStart > kEnd) continue;

                        int rowBase = i + nxL * j;
                        float pzStart = bminZ + kStart * step;

                        for (int k = kStart; k <= kEnd; k++)
                        {
                            float dz = pzStart + (k - kStart) * step - posZ;
                            float d2 = dxy2 + dz * dz;
                            if (d2 > cutoffSq) continue;

                            float rho = norm * HirshfeldExpLut.FastExpNeg(halfInvSigSq * d2);
                            rhoTarget[rowBase + strideZ * k] += rho;
                        }
                    }
                }
            }
            else
            {
                float e1x = gauss.E1.X, e1y = gauss.E1.Y, e1z = gauss.E1.Z;
                float e2x = gauss.E2.X, e2y = gauss.E2.Y, e2z = gauss.E2.Z;
                float e3x = gauss.E3.X, e3y = gauss.E3.Y, e3z = gauss.E3.Z;
                float invA1Sq = gauss.InvA1Sq;
                float invA2Sq = gauss.InvA2Sq;
                float invA3Sq = gauss.InvA3Sq;

                float pxBase = bminX + i0 * step;
                float pyBase = bminY + j0 * step;
                float pzBase = bminZ + k0 * step;

                for (int i = i0; i <= i1; i++, pxBase += step)
                {
                    float dx = pxBase - posX;

                    float pyLocal = pyBase;
                    for (int j = j0; j <= j1; j++, pyLocal += step)
                    {
                        float dy = pyLocal - posY;

                        int rowBase = i + nxL * j;

                        float pzLocal = pzBase;
                        for (int k = k0; k <= k1; k++, pzLocal += step)
                        {
                            float dz = pzLocal - posZ;

                            float d1 = dx * e1x + dy * e1y + dz * e1z;
                            float d2 = dx * e2x + dy * e2y + dz * e2z;
                            float d3 = dx * e3x + dy * e3y + dz * e3z;

                            float quad = d1 * d1 * invA1Sq
                                       + d2 * d2 * invA2Sq
                                       + d3 * d3 * invA3Sq;

                            float rho = norm * HirshfeldExpLut.FastExpNeg(0.5f * quad);
                            rhoTarget[rowBase + strideZ * k] += rho;
                        }
                    }
                }
            }
        }

        private void FillDensitySingleThread(
            Action<string, float> progress, CancellationToken ct, Stopwatch sw)
        {
            int nx = _grid.ResX;
            int ny = _grid.ResY;
            int nz = _grid.ResZ;

            float step = (float)(_grid.BoundsMax.X - _grid.BoundsMin.X) / (nx - 1);
            float invStep = 1.0f / step;

            float bminX = _grid.BoundsMin.X;
            float bminY = _grid.BoundsMin.Y;
            float bminZ = _grid.BoundsMin.Z;

            int totalAtoms = _allAtoms.Count;
            int atomsDone = 0;
            int lastReportedPercent = -1;

            int nxL = nx, nyL = ny, nzL = nz;
            int strideZ = nx * ny;

            foreach (var entry in _allAtoms)
            {
                ct.ThrowIfCancellationRequested();

                Vector3 pos = entry.Pos;
                bool isTarget = entry.IsTarget;

                float posX = pos.X, posY = pos.Y, posZ = pos.Z;

                var rhoTarget = isTarget ? _grid.RhoPro : _grid.RhoProm_Outer;

                if (entry.HasSto)
                {
                    FillDensitySto(entry.Sto, rhoTarget,
                                   posX, posY, posZ,
                                   bminX, bminY, bminZ,
                                   invStep, step, nxL, nyL, nzL, strideZ);
                }
                else
                {
                    FillDensityGauss(entry.Gauss, rhoTarget,
                                      posX, posY, posZ,
                                      bminX, bminY, bminZ,
                                      invStep, step, nxL, nyL, nzL, strideZ);
                }

                atomsDone++;

                if (atomsDone % 50 == 0 || atomsDone == totalAtoms)
                {
                    int pct = (int)(100f * atomsDone / totalAtoms);
                    if (pct != lastReportedPercent)
                    {
                        lastReportedPercent = pct;
                        float p = 0.15f + 0.60f * atomsDone / (float)totalAtoms;
                        progress?.Invoke($"Filling density ({atomsDone}/{totalAtoms})", p);
                    }
                }
            }
        }

        #endregion
    }
}