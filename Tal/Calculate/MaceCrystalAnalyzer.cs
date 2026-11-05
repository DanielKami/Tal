// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Periodic MACE evaluation via evaluate_energy.exe.
// Builds an N×N×N supercell and neighbour graph (R_cut = 5 Å).
// Uses Molecule.IndividualMoleculeInCell from SymetryEquivalents for grouping.

using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Tal.Calculate
{
    public static class MaceCrystalAnalyzer
    {
        public const float RCut = 5.0f;

        public class Result
        {
            public float EnergyPerUnitCell;
            public float TotalSupercellEnergy;
            public int CentralAtomCount;
            public int SupercellAtomCount;
            public int EdgeCount;
            public int SupercellSize;
            public int MoleculeCountInCell;
            public string ModelName = "medium";
            public string ModelUsed = "medium";
            public string ModelFile = "";
            public long ModelParams;
            public bool FallbackUsed;
            public string ModelWarning = "";
            public string DeviceName = "evaluate_energy.exe";
            public string DeviceReport = "";
            public string WorkerStderr = "";
            public float[,] CentralCellForces = new float[0, 3];
        }

        public static string ModelFileName(string modelName)
            => modelName?.ToLowerInvariant() switch
            {
                "off23-small" => "mace_off23_small.pt",
                "off23-medium" => "mace_off23_medium.pt",
                "off23-large" => "mace_off23_large.pt",
                "mp0-medium" => "mace_mp0_medium.pt",
                "mp0-large" => "mace_mp0_large.pt",
                // legacy aliases
                "small" => "mace_off23_small.pt",
                "medium" => "mace_off23_medium.pt",
                "large" => "mace_off23_large.pt",
                _ => "mace_off23_medium.pt"
            };

        public static readonly string[] KnownModels =
        {
            "off23-small", "off23-medium", "off23-large", "mp0-medium", "mp0-large"
        };

        public const string DefaultModel = "off23-medium";
        public static string? ModelPath(string modelName = DefaultModel)
        {
            // Exact file only — no silent swap between models.
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string preferred = ModelFileName(modelName);
            string path = Path.Combine(baseDir, preferred);
            return File.Exists(path) ? path : null;
        }

        public static bool IsReady(string modelName, out string status)
        {
            string? model = ModelPath(modelName);
            string exe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PythonMaceEvaluator.ExeFileName);
            if (model == null)
            {
                status = $"Model: {ModelFileName(modelName)} not found next to the executable.";
                return false;
            }
            if (!File.Exists(exe))
            {
                status = $"Evaluator: {PythonMaceEvaluator.ExeFileName} not found next to the executable.";
                return false;
            }
            status = $"Model: {Path.GetFileName(model)} + {PythonMaceEvaluator.ExeFileName}";
            return true;
        }

        public static bool IsReady(out string status) => IsReady("medium", out status);

        public static Result Evaluate(
            int[] unitCellZ,
            float[] unitCellCoords,
            float[,] cellMatrix,
            string modelName = "medium",
            int supercellSize = 3,
            int[]? unitCellMoleculeIds = null)
        {
            modelName = PythonMaceEvaluator.NormalizeModelName(modelName, out _);
            // only exact file for selected model (IsReady / pre-check)
            if (supercellSize < 1) supercellSize = 1;
            if (supercellSize > 3) supercellSize = 3;
            // 2×2×2 is not offered: no true central cell with a complete neighbour shell
            if (supercellSize == 2) supercellSize = 3;

            string? modelPath = ModelPath(modelName);
            if (modelPath == null)
                throw new FileNotFoundException(
                    $"MACE model not found. Place {ModelFileName(modelName)} next to the executable " +
                    $"(requested model=\"{modelName}\").");
            Debug.WriteLine($"[MACE] requested model=\"{modelName}\" path={modelPath}");

            string exe = Path.Combine(
                Path.GetDirectoryName(modelPath) ?? AppDomain.CurrentDomain.BaseDirectory,
                PythonMaceEvaluator.ExeFileName);
            if (!File.Exists(exe))
                throw new FileNotFoundException(
                    $"MACE evaluator not found. Place {PythonMaceEvaluator.ExeFileName} next to the model.",
                    exe);

            if (unitCellZ == null || unitCellZ.Length == 0)
                throw new ArgumentException("Unit cell has no atoms.");

            int nCell = unitCellZ.Length;
            int nRep = supercellSize;

            // molecule IDs for unit cell (from TAL Molecule detection)
            int[] cellMol = unitCellMoleculeIds;
            if (cellMol == null || cellMol.Length != nCell)
            {
                cellMol = new int[nCell]; // all 0 if missing
            }

            int molInCell = 0;
            {
                var seen = new HashSet<int>();
                foreach (int id in cellMol)
                    if (id >= 0) seen.Add(id);
                molInCell = Math.Max(seen.Count, 1);
            }

            var superZ = new List<int>();
            var superCoords = new List<float>();
            var superMol = new List<int>();
            var central = new List<int>();

            // Lattice vectors from cellMatrix rows (same as FromMolecule cart formula):
            // cellMatrix[row, col] with row = lattice vector index a,b,c.
            // Shift of image (nx,ny,nz): nx*a + ny*b + nz*c.
            //
            // CENTRAL image for force extraction:
            //   nRep=1 → (0,0,0)  — incomplete environment (user warned)
            //   nRep=3 → (1,1,1)  — true geometric centre of the 3×3×3 block
            int cx = nRep / 2, cy = nRep / 2, cz = nRep / 2;

            int stride = Math.Max(molInCell, 1) + 1;

            for (int nx = 0; nx < nRep; nx++)
            for (int ny = 0; ny < nRep; ny++)
            for (int nz = 0; nz < nRep; nz++)
            {
                float sx = nx * cellMatrix[0, 0] + ny * cellMatrix[1, 0] + nz * cellMatrix[2, 0];
                float sy = nx * cellMatrix[0, 1] + ny * cellMatrix[1, 1] + nz * cellMatrix[2, 1];
                float sz = nx * cellMatrix[0, 2] + ny * cellMatrix[1, 2] + nz * cellMatrix[2, 2];

                int imageKey = ((nx * nRep) + ny) * nRep + nz;
                bool isCentral = (nx == cx && ny == cy && nz == cz);

                for (int i = 0; i < nCell; i++)
                {
                    superZ.Add(unitCellZ[i]);
                    superCoords.Add(unitCellCoords[i * 3 + 0] + sx);
                    superCoords.Add(unitCellCoords[i * 3 + 1] + sy);
                    superCoords.Add(unitCellCoords[i * 3 + 2] + sz);

                    int baseId = cellMol[i] >= 0 ? cellMol[i] : 0;
                    superMol.Add(baseId + imageKey * stride);

                    if (isCentral)
                        central.Add(superZ.Count - 1);
                }
            }

            int nSuper = superZ.Count;
            float[] coords = superCoords.ToArray();
            int[] molIds = superMol.ToArray();

            // Supercell lattice = nRep × unit cell (for PBC neighbour list + MACE cell)
            var superCell = new float[3, 3];
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                superCell[r, c] = cellMatrix[r, c] * nRep;

            // Neighbour list with MINIMUM-IMAGE under supercell PBC.
            // cell_shifts = integer lattice hops of the supercell that map j near i.
            var src = new List<long>();
            var dst = new List<long>();
            var shifts = new List<float>();
            float r2max = RCut * RCut;

            for (int i = 0; i < nSuper; i++)
            {
                float ix = coords[i * 3], iy = coords[i * 3 + 1], iz = coords[i * 3 + 2];
                for (int j = 0; j < nSuper; j++)
                {
                    if (i == j) continue;
                    float dx = coords[j * 3] - ix;
                    float dy = coords[j * 3 + 1] - iy;
                    float dz = coords[j * 3 + 2] - iz;

                    // Approximate min-image via fractional coords in supercell basis
                    // frac = inv(superCell^T) · d  → wrap → back
                    MinImage(superCell, ref dx, ref dy, ref dz, out float sx, out float sy, out float sz);

                    if (dx * dx + dy * dy + dz * dz <= r2max)
                    {
                        src.Add(i);
                        dst.Add(j);
                        shifts.Add(sx); shifts.Add(sy); shifts.Add(sz);
                    }
                }
            }

            int eCount = src.Count;
            var cellShifts = shifts.ToArray();
            var cellFlat = new float[9];
            // MACE expects cell as 3×3 row-major: row0=a, row1=b, row2=c
            cellFlat[0] = superCell[0, 0]; cellFlat[1] = superCell[0, 1]; cellFlat[2] = superCell[0, 2];
            cellFlat[3] = superCell[1, 0]; cellFlat[4] = superCell[1, 1]; cellFlat[5] = superCell[1, 2];
            cellFlat[6] = superCell[2, 0]; cellFlat[7] = superCell[2, 1]; cellFlat[8] = superCell[2, 2];

            int nCopies = nRep * nRep * nRep;
            int uniqueMolSuper = molIds.Distinct().Count();
            Debug.WriteLine(
                $"[MACE] model={modelName} supercell={nRep}³ atoms={nSuper} edges={eCount} " +
                $"central_image=({cx},{cy},{cz}) molecules_in_cell={molInCell} molecules_in_super={uniqueMolSuper}");

            PythonMaceEvaluator.EnergyForces ef;
            using (var evaluator = new PythonMaceEvaluator(modelPath))
            {
                ef = evaluator.Evaluate(
                    superZ.ToArray(),
                    coords,
                    src.ToArray(),
                    dst.ToArray(),
                    cellShifts,
                    cellFlat,
                    modelName,
                    molIds);
            }

            var centralForces = new float[nCell, 3];
            if (ef.Forces != null && ef.Forces.GetLength(0) >= nSuper)
            {
                for (int i = 0; i < nCell; i++)
                {
                    int s = central[i];
                    centralForces[i, 0] = ef.Forces[s, 0];
                    centralForces[i, 1] = ef.Forces[s, 1];
                    centralForces[i, 2] = ef.Forces[s, 2];
                }
            }

            return new Result
            {
                EnergyPerUnitCell = ef.Energy / nCopies,
                TotalSupercellEnergy = ef.Energy,
                CentralAtomCount = nCell,
                SupercellAtomCount = nSuper,
                EdgeCount = eCount,
                SupercellSize = nRep,
                MoleculeCountInCell = molInCell,
                ModelName = modelName,
                ModelUsed = ef.ModelUsed ?? modelName,
                ModelFile = ef.ModelFile ?? "",
                ModelParams = ef.ModelParams,
                FallbackUsed = ef.FallbackUsed,
                ModelWarning = ef.Warning ?? "",
                DeviceName = ef.DeviceName ?? "unknown",
                DeviceReport = ef.DeviceReport ?? "",
                WorkerStderr = ef.Stderr ?? "",
                CentralCellForces = centralForces
            };
        }


        /// <summary>
        /// Minimum-image displacement under cell (rows = a,b,c). Returns Cartesian delta
        /// and integer supercell shifts (sx,sy,sz) applied to the second point.
        /// </summary>
        private static void MinImage(float[,] cell, ref float dx, ref float dy, ref float dz,
            out float sx, out float sy, out float sz)
        {
            // Solve cell^T · f = d  → f = (cell^T)^{-1} · d, wrap f to [-0.5,0.5), d' = cell^T · f
            // cell rows are lattice vectors; cell^T columns are lattice vectors.
            float a11 = cell[0, 0], a12 = cell[1, 0], a13 = cell[2, 0];
            float a21 = cell[0, 1], a22 = cell[1, 1], a23 = cell[2, 1];
            float a31 = cell[0, 2], a32 = cell[1, 2], a33 = cell[2, 2];
            // M = cell^T  so M * f = d; invert M
            float det = a11 * (a22 * a33 - a23 * a32)
                      - a12 * (a21 * a33 - a23 * a31)
                      + a13 * (a21 * a32 - a22 * a31);
            if (Math.Abs(det) < 1e-12f)
            {
                sx = sy = sz = 0;
                return;
            }
            float inv = 1f / det;
            // inv(M) * d = f
            float fx = inv * ((a22 * a33 - a23 * a32) * dx + (a13 * a32 - a12 * a33) * dy + (a12 * a23 - a13 * a22) * dz);
            float fy = inv * ((a23 * a31 - a21 * a33) * dx + (a11 * a33 - a13 * a31) * dy + (a13 * a21 - a11 * a23) * dz);
            float fz = inv * ((a21 * a32 - a22 * a31) * dx + (a12 * a31 - a11 * a32) * dy + (a11 * a22 - a12 * a21) * dz);

            sx = (float)Math.Round(fx);
            sy = (float)Math.Round(fy);
            sz = (float)Math.Round(fz);
            fx -= sx; fy -= sy; fz -= sz;

            // d' = M * f
            dx = a11 * fx + a12 * fy + a13 * fz;
            dy = a21 * fx + a22 * fy + a23 * fz;
            dz = a31 * fx + a32 * fy + a33 * fz;
        }

        /// <summary>
        /// Unit-cell geometry + molecule IDs from TAL SymetryEquivalents / DetectMolecules.
        /// Display mode (asymmetric/symmetry/packing) is ignored.
        /// </summary>
        public static (int[] z, float[] coords, float[,] cell, int[] atomIndices, int[] moleculeIds)
            FromMolecule(Molecule mol)
        {
            if (mol == null)
                throw new ArgumentException("No molecule.");

            // ALWAYS rebuild the full crystallographic unit cell with ALL symmetry ops
            // (Display Options may have disabled ops for asymmetric view).
            int nCell = mol.EnsureFullUnitCellForCalculations();
            Debug.WriteLine(
                $"[MACE] Full unit cell: {nCell} atoms " +
                $"(Asym={mol.AsymmetricPart?.Count ?? 0}, Ops={mol.SymmetryOperations?.Count ?? 0})");

            try
            {
                if (mol.SymetryEquivalents != null && mol.SymetryEquivalents.Count > 0)
                {
                    // Covalent components inside the unit cell (no MI / no sym-bond expand)
                    int nMol = mol.DetectMoleculesInUnitCell(mol.SymetryEquivalents, true);

                    // If detection collapsed symmetry copies (e.g. 1 instead of Z),
                    // assign IDs as (asym molecule) × (symmetry operation index).
                    int nOps = mol.SymmetryOperations?.Count ?? 1;
                    if (nMol < 2 && nOps > 1)
                    {
                        Debug.WriteLine(
                            $"[MACE] DetectMoleculesInUnitCell found {nMol} — " +
                            "falling back to AssignMoleculeIdsBySymmetryOp");
                        nMol = mol.AssignMoleculeIdsBySymmetryOp(mol.SymetryEquivalents);
                    }

                    mol.PropagateMoleculeIdsToAsymmetricPart();
                    mol.PrintMoleculeStatistics(mol.SymetryEquivalents, "MACE-FromMolecule");
                    Debug.WriteLine($"[MACE] molecules in unit cell = {nMol}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[MACE] DetectMolecules: " + ex.Message);
            }

            // Presentation convention: U = Transpose(Frac→Cart); cart = Transform(frac, U)
            Matrix raw = mol.FractionaltoCarttesianMatrix(0, 0, 0);
            Matrix Ut = Matrix.Transpose(raw);
            // cell rows = lattice vectors a,b,c in the SAME frame as cart positions
            var cell = new float[3, 3];
            Vector3 aV = Vector3.Transform(new Vector3(1, 0, 0), Ut);
            Vector3 bV = Vector3.Transform(new Vector3(0, 1, 0), Ut);
            Vector3 cV = Vector3.Transform(new Vector3(0, 0, 1), Ut);
            cell[0, 0] = aV.X; cell[0, 1] = aV.Y; cell[0, 2] = aV.Z;
            cell[1, 0] = bV.X; cell[1, 1] = bV.Y; cell[1, 2] = bV.Z;
            cell[2, 0] = cV.X; cell[2, 1] = cV.Y; cell[2, 2] = cV.Z;

            if (mol.SymetryEquivalents != null && mol.SymetryEquivalents.Count > 0)
            {
                int n = mol.SymetryEquivalents.Count;
                var z = new int[n];
                var coords = new float[n * 3];
                var molIds = new int[n];
                for (int i = 0; i < n; i++)
                {
                    var s = mol.SymetryEquivalents[i];
                    z[i] = s.AtomicNumber;
                    var cart = Vector3.Transform(new Vector3(s.x, s.y, s.z), Ut);
                    coords[i * 3]     = cart.X;
                    coords[i * 3 + 1] = cart.Y;
                    coords[i * 3 + 2] = cart.Z;
                    molIds[i] = s.IndividualMoleculeInCell >= 0 ? s.IndividualMoleculeInCell : 0;
                }

                int nMol = mol.GetMoleculeCount(mol.SymetryEquivalents);
                Debug.WriteLine(
                    $"[MACE] FromMolecule: SymetryEquivalents.Count={n} (not Asym×Ops), molecules={nMol}");

                var indices = MapSymToAtomIndices(mol);
                return (z, coords, cell, indices, molIds);
            }

            if (mol.atom == null || mol.NumberOfAtoms == 0)
                throw new ArgumentException("No molecule atoms.");

            var idx = new List<int>();
            for (int i = 0; i < mol.NumberOfAtoms; i++)
            {
                var a = mol.atom[i];
                if (a.CellShiftX == 0 && a.CellShiftY == 0 && a.CellShiftZ == 0 && a.AtomicNumber > 0)
                    idx.Add(i);
            }
            if (idx.Count == 0)
                throw new ArgumentException("No atoms in the parent unit cell.");

            var z2 = new int[idx.Count];
            var c2 = new float[idx.Count * 3];
            var m2 = new int[idx.Count];
            for (int k = 0; k < idx.Count; k++)
            {
                var a = mol.atom[idx[k]];
                z2[k] = a.AtomicNumber;
                c2[k * 3] = a.Coordinate.X;
                c2[k * 3 + 1] = a.Coordinate.Y;
                c2[k * 3 + 2] = a.Coordinate.Z;
                m2[k] = a.IndividualMoleculeInCell >= 0 ? a.IndividualMoleculeInCell : 0;
            }
            return (z2, c2, cell, idx.ToArray(), m2);
        }

        private static int[] MapSymToAtomIndices(Molecule mol)
        {
            if (mol.atom == null || mol.NumberOfAtoms == 0)
                return Array.Empty<int>();

            var list = new List<int>();
            for (int i = 0; i < mol.NumberOfAtoms; i++)
            {
                var a = mol.atom[i];
                if (a.CellShiftX == 0 && a.CellShiftY == 0 && a.CellShiftZ == 0 && a.AtomicNumber > 0)
                    list.Add(i);
            }
            return list.ToArray();
        }
    }
}
