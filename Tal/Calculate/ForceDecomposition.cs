// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Approximate pairwise force split for display (not from MACE).
// Electrostatic from partial charges, dispersion and repulsion from a Lennard-Jones term.

using Microsoft.Xna.Framework;

namespace Tal.Calculate
{
    /// <summary>
    /// Splits interatomic forces into electrostatic / dispersion / repulsion
    /// using charges already on the molecule and a simple LJ term.
    /// These are estimates for the arrow checkboxes; MACE supplies only the total.
    /// </summary>
    public class InterForce
    {
        public Vector3 From;
        public Vector3 To;
        public Vector3 Electro;
        public Vector3 Dispersion;
        public Vector3 Repulsion;
        public Vector3 Total;
        public string Label;
    }

    public static class ForceDecomposition
    {
        public static List<InterForce> Intermolecular = new List<InterForce>();

        private const float Cut = 8f;
        private const float Kcoulomb = 14.4f; // eV·Å / e²
        private const float Eps = 0.01f;      // eV

        public static void Compute(Molecule mol, int[] indices)
        {
            if (mol?.atom == null || indices == null) return;

            // Ensure molecule IDs exist so intermolecular pairs can be formed
            try
            {
                bool anyId = false;
                foreach (int i in indices)
                {
                    if (mol.atom[i].IndividualMoleculeInCell > 0) { anyId = true; break; }
                }
                if (!anyId)
                {
                    mol.DetectMoleculesInAsymmetricPart();
                    mol.PropagateMoleculeIdsToAsymmetricPart();
                    // Copy IDs from AsymmetricPart onto parent-cell atoms by name/order when possible
                    if (mol.AsymmetricPart != null)
                    {
                        for (int k = 0; k < indices.Length && k < mol.AsymmetricPart.Count; k++)
                        {
                            var a = mol.atom[indices[k]];
                            a.IndividualMoleculeInCell = mol.AsymmetricPart[k].IndividualMoleculeInCell;
                            mol.atom[indices[k]] = a;
                        }
                    }
                }
            }
            catch { /* keep going */ }

            var electro = new Vector3[indices.Length];
            var disp = new Vector3[indices.Length];
            var rep = new Vector3[indices.Length];

            // Include nearby cell images so the split is periodic-ish
            for (int a = 0; a < indices.Length; a++)
            {
                var ia = mol.atom[indices[a]];
                for (int b = 0; b < indices.Length; b++)
                {
                    if (a == b) continue;
                    var ib = mol.atom[indices[b]];
                    for (int nx = -1; nx <= 1; nx++)
                    for (int ny = -1; ny <= 1; ny++)
                    for (int nz = -1; nz <= 1; nz++)
                    {
                        if (nx == 0 && ny == 0 && nz == 0 && a == b) continue;
                        Vector3 shift = CellShift(mol, nx, ny, nz);
                        Vector3 r = (ib.Coordinate + shift) - ia.Coordinate;
                        float d = r.Length();
                        if (d < 0.5f || d > Cut) continue;
                        Vector3 hat = r / d;

                        // Coulomb
                        float fe = Kcoulomb * ia.charge * ib.charge / (d * d);
                        electro[a] += fe * hat;

                        // LJ: V = 4 eps ((sig/r)^12 - (sig/r)^6)
                        float sig = 0.5f * (Vdw(ia.AtomicNumber) + Vdw(ib.AtomicNumber));
                        if (sig < 0.5f) sig = 3.0f;
                        float sr = sig / d;
                        float sr6 = sr * sr * sr * sr * sr * sr;
                        float sr12 = sr6 * sr6;
                        float fmag = 24f * Eps / d * (2f * sr12 - sr6);
                        // fmag > 0 means repulsive (force on i points away from j, opposite hat)
                        if (fmag >= 0f)
                            rep[a] -= fmag * hat;
                        else
                            disp[a] -= fmag * hat;
                    }
                }
            }

            for (int a = 0; a < indices.Length; a++)
            {
                var atom = mol.atom[indices[a]];
                atom.ForceElectro = electro[a];
                atom.ForceDispersion = disp[a];
                atom.ForceRepulsion = rep[a];
                mol.atom[indices[a]] = atom;
            }

            BuildIntermolecular(mol, indices);
        }

        /// <summary>
        /// Average intermolecular force between molecule centroids.
        /// Each pair gets electro / dispersion / repulsion / total.
        /// </summary>
        private static void BuildIntermolecular(Molecule mol, int[] indices)
        {
            Intermolecular = new List<InterForce>();
            var groups = new Dictionary<int, List<int>>();
            foreach (int i in indices)
            {
                int id = mol.atom[i].IndividualMoleculeInCell;
                if (!groups.ContainsKey(id)) groups[id] = new List<int>();
                groups[id].Add(i);
            }
            var ids = groups.Keys.ToList();
            for (int ia = 0; ia < ids.Count; ia++)
            for (int ib = ia + 1; ib < ids.Count; ib++)
            {
                var ga = groups[ids[ia]];
                var gb = groups[ids[ib]];
                Vector3 ca = Centroid(mol, ga);
                Vector3 cb = Centroid(mol, gb);
                Vector3 fe = Vector3.Zero, fd = Vector3.Zero, fr = Vector3.Zero;
                int n = 0;
                foreach (int a in ga)
                foreach (int b in gb)
                {
                    Vector3 r = mol.atom[b].Coordinate - mol.atom[a].Coordinate;
                    float d = r.Length();
                    if (d < 0.5f || d > Cut) continue;
                    Vector3 hat = r / d;
                    var aa = mol.atom[a];
                    var bb = mol.atom[b];
                    float fee = 14.4f * aa.charge * bb.charge / (d * d);
                    fe += fee * hat;
                    float sig = 0.5f * (Vdw(aa.AtomicNumber) + Vdw(bb.AtomicNumber));
                    float sr = sig / d;
                    float sr6 = (float)Math.Pow(sr, 6);
                    float sr12 = sr6 * sr6;
                    float fmag = 24f * 0.01f / d * (2f * sr12 - sr6);
                    if (fmag >= 0f) fr -= fmag * hat;
                    else fd -= fmag * hat;
                    n++;
                }
                if (n == 0) continue;
                fe /= n; fd /= n; fr /= n;
                Intermolecular.Add(new InterForce
                {
                    From = ca,
                    To = cb,
                    Electro = fe,
                    Dispersion = fd,
                    Repulsion = fr,
                    Total = fe + fd + fr,
                    Label = $"{ids[ia]}–{ids[ib]}"
                });
            }
        }

        private static Vector3 Centroid(Molecule mol, List<int> idx)
        {
            Vector3 c = Vector3.Zero;
            foreach (int i in idx) c += mol.atom[i].Coordinate;
            return c / Math.Max(idx.Count, 1);
        }

        /// <summary>Net force from assigned mol.atom forces (may be wrong if mapping failed).</summary>
        public static (Vector3 net, float fa, float fb, float fc) LatticeComponents(Molecule mol, int[] indices)
        {
            Vector3 net = Vector3.Zero;
            foreach (int i in indices)
                if (i >= 0 && i < mol.NumberOfAtoms && mol.atom[i].HasForce)
                    net += mol.atom[i].Force;
            return ProjectOnLattice(mol, net);
        }

        /// <summary>
        /// Net force from MACE CentralCellForces directly (authoritative; same order as SUM in log).
        /// </summary>
        public static (Vector3 net, float fa, float fb, float fc) LatticeComponentsFromForces(
            float[,] forces, Molecule mol)
        {
            Vector3 net = Vector3.Zero;
            if (forces == null) return ProjectOnLattice(mol, net);
            int n = forces.GetLength(0);
            for (int i = 0; i < n; i++)
                net += new Vector3(forces[i, 0], forces[i, 1], forces[i, 2]);
            return ProjectOnLattice(mol, net);
        }

        private static (Vector3 net, float fa, float fb, float fc) ProjectOnLattice(Molecule mol, Vector3 net)
        {
            Matrix U = mol.FractionaltoCarttesianMatrix(0, 0, 0);
            Vector3 a = Vector3.Normalize(new Vector3(U.M11, U.M12, U.M13));
            Vector3 b = Vector3.Normalize(new Vector3(U.M21, U.M22, U.M23));
            Vector3 c = Vector3.Normalize(new Vector3(U.M31, U.M32, U.M33));
            return (net, Vector3.Dot(net, a), Vector3.Dot(net, b), Vector3.Dot(net, c));
        }

        private static Vector3 CellShift(Molecule mol, int nx, int ny, int nz)
        {
            Matrix U = mol.FractionaltoCarttesianMatrix(0, 0, 0);
            return nx * new Vector3(U.M11, U.M12, U.M13)
                 + ny * new Vector3(U.M21, U.M22, U.M23)
                 + nz * new Vector3(U.M31, U.M32, U.M33);
        }

        private static float Vdw(int z)
        {
            switch (z)
            {
                case 1: return 1.20f;
                case 6: return 1.70f;
                case 7: return 1.55f;
                case 8: return 1.52f;
                case 9: return 1.47f;
                case 16: return 1.80f;
                case 17: return 1.75f;
                default: return 1.70f;
            }
        }
    }
}
