// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Powder diffraction module: StructureFactorCalculator.

namespace Tal.Core.PowderDiffraction
{
    /// <summary>
    /// Thrown when the structure contains atoms for which no X-ray scattering factor is tabulated.
    /// Such atoms would otherwise contribute nothing to F(hkl) and silently falsify the pattern.
    /// </summary>
    public class UnsupportedElementException : Exception
    {
        public IReadOnlyList<string> Elements { get; }

        public UnsupportedElementException(IReadOnlyList<string> elements)
            : base(BuildMessage(elements))
        {
            Elements = elements;
        }

        private static string BuildMessage(IReadOnlyList<string> elements)
        {
            return "No X-ray scattering factors are tabulated for: " + string.Join(", ", elements) +
                   ". The powder pattern was not calculated, because these atoms would contribute " +
                   "nothing to F(hkl) and the intensities would be wrong " +
                   "(tabulated range: H to Cf, Z = 1-" + CromerMannTable.MaxSupportedZ + ").";
        }
    }

    public class StructureFactorCalculator
    {
        private struct CellAtom
        {
            public int ElementIndex;   // index into _elementZ / _f0
            public double X, Y, Z;     // fractional, folded into [0,1)
            public double Occupancy;
            public double U;           // isotropic / equivalent U (Å²) from the file, 0 = unknown
        }

        private readonly Molecule _molecule;

        private CellAtom[] _cell = null;
        private int[] _elementZ = Array.Empty<int>();
        private double[] _f0 = Array.Empty<double>();

        /// <summary>Number of atoms in the complete unit cell used for F(hkl) (valid after PrepareCell).</summary>
        public int CellAtomCount => _cell?.Length ?? 0;

        public StructureFactorCalculator(Molecule molecule)
        {
            _molecule = molecule;
        }

        /// <summary>
        /// Distinct elements of the asymmetric unit that have no scattering-factor coefficients
        /// (Z outside H..Cf, or Z &lt;= 0 for unknown / dummy atoms). Empty list = pattern can be calculated.
        /// </summary>
        public IReadOnlyList<string> GetUnsupportedElements()
        {
            var result = new List<string>();
            var seen = new HashSet<int>();

            foreach (var atom in _molecule.AsymmetricPart)
            {
                int z = atom.AtomicNumber;
                if (!seen.Add(z)) continue;
                if (!CromerMannTable.Has(z))
                    result.Add(z <= 0 ? "unknown element (Z=" + z + ")" : CromerMannTable.Symbol(z));
            }
            return result;
        }

        /// <summary>
        /// Builds the complete unit cell from the asymmetric unit and ALL space-group operations.
        /// This deliberately ignores the "selective symmetry" toggles of the 3D view
        /// (Flags.EnabledSymmetryOps) and does not touch the molecule, so the pattern never depends
        /// on which symmetry operations happen to be displayed.
        /// Symmetry images of one atom that coincide (special positions) are counted once.
        /// </summary>
        /// <exception cref="UnsupportedElementException">An element has no tabulated scattering factor.</exception>
        public void PrepareCell()
        {
            var unsupported = GetUnsupportedElements();
            if (unsupported.Count > 0)
                throw new UnsupportedElementException(unsupported);

            double[,] g = DirectMetric();
            const double mergeDistance = 0.1;               // Å; images closer than this are one atom
            double mergeDistance2 = mergeDistance * mergeDistance;

            var ops = _molecule.SymmetryOperations;
            int nOps = ops?.Count ?? 0;

            var elementIndex = new Dictionary<int, int>();
            var cell = new List<CellAtom>();

            foreach (var atom in _molecule.AsymmetricPart)
            {
                if (!elementIndex.TryGetValue(atom.AtomicNumber, out int ei))
                {
                    ei = elementIndex.Count;
                    elementIndex[atom.AtomicNumber] = ei;
                }

                double[] pos = { atom.x, atom.y, atom.z };
                var images = new List<double[]>();

                if (nOps == 0)
                {
                    images.Add(Fold(pos));
                }
                else
                {
                    for (int i = 0; i < nOps; i++)
                    {
                        double[] f = Fold(ApplyOperation(ops[i], pos));

                        bool duplicate = false;
                        foreach (var other in images)
                        {
                            if (PeriodicDistance2(f, other, g) < mergeDistance2)
                            {
                                duplicate = true;
                                break;
                            }
                        }
                        if (!duplicate) images.Add(f);
                    }
                }

                foreach (var f in images)
                {
                    cell.Add(new CellAtom
                    {
                        ElementIndex = ei,
                        X = f[0],
                        Y = f[1],
                        Z = f[2],
                        Occupancy = atom.occupancy,
                        U = atom.iso > 0f ? atom.iso : 0.0
                    });
                }
            }

            _cell = cell.ToArray();
            _elementZ = new int[elementIndex.Count];
            foreach (var kv in elementIndex) _elementZ[kv.Value] = kv.Key;
            _f0 = new double[_elementZ.Length];
        }

        /// <summary>
        /// |F(hkl)| = | sum_j f_j(s) * occ_j * T_j(s) * exp(2 pi i (h x_j + k y_j + l z_j)) |
        /// summed over the whole unit cell, s = sin(theta)/lambda.
        /// T_j = exp(-8 pi^2 U_j s^2 · scale) with the atom's own U(iso/eq) when useAtomicU is set and
        /// the atom has one; every other atom uses the reference B: T_j = exp(-B s^2 · scale).
        /// 'scale' multiplies all displacement parameters (1 = as given).
        /// Anisotropic ADPs enter through their equivalent U only.
        /// </summary>
        public double Calculate(int h, int k, int l, double theta, double wavelength, double debyeWallerB,
                                bool useAtomicU = false, double displacementScale = 1.0)
        {
            if (_cell == null) PrepareCell();

            double s = Math.Sin(theta * Math.PI / 180.0) / wavelength;
            double s2 = s * s;
            double dwGlobal = Math.Exp(-debyeWallerB * displacementScale * s2);
            const double eightPiSq = 8.0 * Math.PI * Math.PI;

            for (int e = 0; e < _elementZ.Length; e++)
                _f0[e] = CromerMannTable.FormFactor(_elementZ[e], s2);

            double re = 0.0, im = 0.0;
            const double twoPi = 2.0 * Math.PI;

            for (int i = 0; i < _cell.Length; i++)
            {
                var a = _cell[i];
                double dw = (useAtomicU && a.U > 0.0) ? Math.Exp(-eightPiSq * a.U * displacementScale * s2) : dwGlobal;
                double w = _f0[a.ElementIndex] * dw * a.Occupancy;
                double phase = twoPi * (h * a.X + k * a.Y + l * a.Z);
                re += w * Math.Cos(phase);
                im += w * Math.Sin(phase);
            }

            return Math.Sqrt(re * re + im * im);
        }

        #region Helpers

        /// <summary>x' = R x + t, written exactly like Molecule.CalculateSymmetryEquivalents.</summary>
        private static double[] ApplyOperation(SymmetryElements.ReadSymm op, double[] pos)
        {
            double[] r = new double[3];
            for (int a = 0; a < 3; a++)
            {
                double v = op.shift[a];
                int p1 = op.pos1[a], s1 = op.sign1[a];
                int p2 = op.pos2[a], s2 = op.sign2[a];
                if (s1 != 0 && p1 >= 0 && p1 < 3) v += pos[p1] * s1;
                if (s2 != 0 && p2 >= 0 && p2 < 3) v += pos[p2] * s2;
                r[a] = v;
            }
            return r;
        }

        private static double[] Fold(double[] f)
        {
            double[] r = new double[3];
            for (int a = 0; a < 3; a++)
            {
                double v = f[a] - Math.Floor(f[a]);
                if (v >= 1.0) v -= 1.0;
                if (v < 0.0) v += 1.0;
                r[a] = v;
            }
            return r;
        }

        /// <summary>Squared distance (Å²) between two fractional points, minimum-image convention.</summary>
        private static double PeriodicDistance2(double[] f1, double[] f2, double[,] g)
        {
            double[] d = new double[3];
            for (int a = 0; a < 3; a++)
            {
                double v = f1[a] - f2[a];
                v -= Math.Round(v);
                d[a] = v;
            }

            double s = 0.0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    s += d[i] * g[i, j] * d[j];
            return s;
        }

        /// <summary>Direct metric tensor G (Å²) of the current cell (angles stored in radians).</summary>
        private double[,] DirectMetric()
        {
            double a = _molecule.a, b = _molecule.b, c = _molecule.c;
            double ca = Math.Cos(_molecule.alpha);
            double cb = Math.Cos(_molecule.beta);
            double cg = Math.Cos(_molecule.gamma);

            return new double[,]
            {
                { a * a,       a * b * cg, a * c * cb },
                { a * b * cg,  b * b,      b * c * ca },
                { a * c * cb,  b * c * ca, c * c      }
            };
        }

        #endregion
    }
}
