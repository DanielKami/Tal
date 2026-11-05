// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Bridges TAL → evaluate_energy.exe.
// Contract: C# sends only model name (small|medium|large); worker returns
// model_used / fallback_used / model_params. See worker-contract.

using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Tal.Calculate
{
    public sealed class PythonMaceEvaluator : IDisposable
    {
        private readonly string _exePath;
        private readonly string _workingDir;
        private bool _disposed;

        public const string ExeFileName = "evaluate_energy.exe";

        /// <summary>Result of one worker invocation (energy + forces + model metadata).</summary>
        public readonly struct EnergyForces
        {
            public float Energy { get; init; }
            public float[,] Forces { get; init; }
            public string DeviceName { get; init; }
            public string DeviceReport { get; init; }
            public string Stderr { get; init; }
            public int MoleculeCount { get; init; }

            public string ModelRequested { get; init; }
            public string ModelUsed { get; init; }
            public string ModelFile { get; init; }
            public long ModelParams { get; init; }
            public bool FallbackUsed { get; init; }
            public string Warning { get; init; }
        }

        public PythonMaceEvaluator(string modelPath)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                throw new ArgumentException("Model path is required.", nameof(modelPath));

            _workingDir = Path.GetDirectoryName(Path.GetFullPath(modelPath))
                          ?? AppDomain.CurrentDomain.BaseDirectory;
            _exePath = Path.Combine(_workingDir, ExeFileName);

            if (!File.Exists(_exePath))
                throw new FileNotFoundException(
                    $"MACE evaluator not found. Place {ExeFileName} next to the model in:\n{_workingDir}",
                    _exePath);
        }

        public string WorkingDirectory => _workingDir;
        public string ExePath => _exePath;

        /// <summary>
        /// Normalize TAL model id to one of: off23-small|off23-medium|off23-large|mp0-medium|mp0-large.
        /// Legacy medium/large/small map to OFF23. Unknown → off23-medium + warning.
        /// </summary>
        public static string NormalizeModelName(string? name, out string? warning)
        {
            warning = null;
            string n = (name ?? MaceCrystalAnalyzer.DefaultModel).Trim().ToLowerInvariant();
            // legacy
            if (n == "small") n = "off23-small";
            else if (n == "medium") n = "off23-medium";
            else if (n == "large") n = "off23-large";

            foreach (string k in MaceCrystalAnalyzer.KnownModels)
            {
                if (n == k) return k;
            }
            warning = $"Nieznany model '{name}' — znormalizowano do '{MaceCrystalAnalyzer.DefaultModel}'";
            return MaceCrystalAnalyzer.DefaultModel;
        }

        public EnergyForces Evaluate(
            int[] atomicNumbers,
            float[] coordsFlat,
            long[] edgesSrc,
            long[] edgesDst,
            float[] cellShifts,
            float[] cellFlat,
            string modelName = "medium",
            int[]? moleculeIds = null)
        {
            if (atomicNumbers == null || atomicNumbers.Length == 0)
                throw new ArgumentException("No atoms.", nameof(atomicNumbers));
            if (coordsFlat == null || coordsFlat.Length != atomicNumbers.Length * 3)
                throw new ArgumentException("coordsFlat must be N×3.", nameof(coordsFlat));
            if (edgesSrc == null || edgesDst == null || edgesSrc.Length != edgesDst.Length)
                throw new ArgumentException("Edge lists must have equal length.");
            if (cellShifts == null || cellShifts.Length != edgesSrc.Length * 3)
                throw new ArgumentException("cellShifts must be E×3.");
            if (cellFlat == null || cellFlat.Length != 9)
                throw new ArgumentException("cellFlat must have 9 elements.");

            string model = NormalizeModelName(modelName, out string? normWarn);
            if (normWarn != null)
                Debug.WriteLine("[C#] " + normWarn);

            // Contract: send NAME only — worker builds mace_{model}.pt
            Debug.WriteLine($"[C#] wysyłam do workera: model={model}");

            int n = atomicNumbers.Length;
            int e = edgesSrc.Length;

            var pos = new float[n][];
            for (int i = 0; i < n; i++)
                pos[i] = new[] { coordsFlat[i * 3], coordsFlat[i * 3 + 1], coordsFlat[i * 3 + 2] };

            var shifts = new float[e][];
            for (int i = 0; i < e; i++)
                shifts[i] = new[] { cellShifts[i * 3], cellShifts[i * 3 + 1], cellShifts[i * 3 + 2] };

            var payload = new Dictionary<string, object>
            {
                ["model"] = model,
                ["pos"] = pos,
                ["src"] = edgesSrc,
                ["dst"] = edgesDst,
                ["cell_shifts"] = shifts,
                ["atomic_numbers"] = atomicNumbers,
                ["cell"] = cellFlat
            };

            if (moleculeIds != null && moleculeIds.Length == n)
                payload["molecule_ids"] = moleculeIds;

            string json = JsonSerializer.Serialize(payload);
            var (stdout, stderrText) = RunProcess(json);

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(stdout) ? "{}" : stdout);
            var root = doc.RootElement;

            // 1) error?
            if (root.TryGetProperty("error", out var errProp))
            {
                string msg = errProp.GetString() ?? "MACE evaluator error.";
                string errType = root.TryGetProperty("error_type", out var et)
                    ? et.GetString() ?? ""
                    : "";
                Debug.WriteLine($"[C#] worker error ({errType}): {msg}");
                if (msg.Contains("out of memory", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("CUDA", StringComparison.OrdinalIgnoreCase)
                    || stderrText.Contains("out of memory", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "GPU out of memory (OOM). Try model=medium and/or supercell 1×1×1.\n\n" + msg);
                }
                throw new InvalidOperationException(
                    string.IsNullOrEmpty(errType) ? msg : $"{errType}: {msg}");
            }

            if (!root.TryGetProperty("energy", out var energyProp))
                throw new InvalidOperationException(
                    "Worker response missing 'energy' (and no 'error'). Stdout:\n" + Truncate(stdout, 500));

            float energy = energyProp.GetSingle();
            float[,] forces = ParseForces(root, n);

            string modelRequested = root.TryGetProperty("model_requested", out var mr)
                ? mr.GetString() ?? model : model;
            string modelUsed = root.TryGetProperty("model_used", out var mu)
                ? mu.GetString() ?? model : model;
            string modelFile = root.TryGetProperty("model_file", out var mf)
                ? mf.GetString() ?? "" : "";
            long modelParams = root.TryGetProperty("model_params", out var mp) && mp.TryGetInt64(out long p)
                ? p : 0;
            bool fallbackUsed = root.TryGetProperty("fallback_used", out var fu) && fu.ValueKind == JsonValueKind.True;
            string warning = root.TryGetProperty("warning", out var w) ? w.GetString() ?? "" : "";
            if (normWarn != null && string.IsNullOrEmpty(warning))
                warning = normWarn;

            // 2) fallback?
            if (fallbackUsed)
                Debug.WriteLine($"[C#] UWAGA: {warning}");

            // 3) metadata log
            Debug.WriteLine(
                $"[C#] użyto: requested={modelRequested} used={modelUsed} " +
                $"params={modelParams} file={modelFile} fallback={fallbackUsed}");

            string device = root.TryGetProperty("device", out var d) ? d.GetString() ?? "unknown" : "unknown";
            string deviceReport = "";
            if (root.TryGetProperty("gpu", out var gpu) && gpu.ValueKind == JsonValueKind.Object)
            {
                if (gpu.TryGetProperty("gpu_name", out var gn) && gn.ValueKind == JsonValueKind.String)
                    deviceReport = gn.GetString() ?? "";
            }

            int molCount = 0;
            if (root.TryGetProperty("molecules", out var mols) && mols.ValueKind == JsonValueKind.Object
                && mols.TryGetProperty("count", out var mc))
                molCount = mc.GetInt32();

            return new EnergyForces
            {
                Energy = energy,
                Forces = forces,
                DeviceName = device,
                DeviceReport = deviceReport,
                Stderr = stderrText,
                MoleculeCount = molCount,
                ModelRequested = modelRequested,
                ModelUsed = modelUsed,
                ModelFile = modelFile,
                ModelParams = modelParams,
                FallbackUsed = fallbackUsed,
                Warning = warning ?? ""
            };
        }

        private static float[,] ParseForces(JsonElement root, int n)
        {
            var forces = new float[n, 3];
            if (!root.TryGetProperty("forces", out var forcesEl) || forcesEl.ValueKind != JsonValueKind.Array)
                return forces;

            int i = 0;
            foreach (var row in forcesEl.EnumerateArray())
            {
                if (i >= n) break;
                int j = 0;
                foreach (var v in row.EnumerateArray())
                {
                    if (j >= 3) break;
                    forces[i, j] = v.GetSingle();
                    j++;
                }
                i++;
            }
            return forces;
        }

        private static string Truncate(string s, int max)
            => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max) + "…");

        private (string stdout, string stderr) RunProcess(string json)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _exePath,
                WorkingDirectory = _workingDir,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var proc = new Process { StartInfo = psi };
            var stderr = new StringBuilder();
            proc.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    stderr.AppendLine(args.Data);
            };

            if (!proc.Start())
                throw new InvalidOperationException($"Failed to start {ExeFileName}.");

            proc.BeginErrorReadLine();
            proc.StandardInput.Write(json);
            proc.StandardInput.Close();

            string stdout = proc.StandardOutput.ReadToEnd();
            if (!proc.WaitForExit(600_000))
            {
                try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
                throw new TimeoutException($"{ExeFileName} did not finish within 10 minutes.");
            }

            string stderrText = stderr.ToString();

            // ExitCode != 0 may still have JSON error on stdout — parse in Evaluate
            if (proc.ExitCode != 0 && string.IsNullOrWhiteSpace(stdout))
            {
                throw new InvalidOperationException(
                    $"{ExeFileName} failed (exit {proc.ExitCode}): {stderrText}".Trim());
            }

            return (stdout, stderrText);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
