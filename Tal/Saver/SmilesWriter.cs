// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
//
// Conservative SMILES generator from the covalent bond graph.
// Priority: VALIDITY over aromatic elegance.
//  - Ring digits appear exactly twice
//  - Aromatic C–C (~1.39 Å) are NOT written as '=' (that broke valence)
//  - Formal charge from partial charge / simple N+/O- heuristics
//  - No stereo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Tal
{
    public static class SmilesWriter
    {
        public static string Generate(Molecule molecule, bool omitHydrogens = true)
        {
            var parts = GeneratePerComponent(molecule, omitHydrogens);
            return parts.Count == 0 ? "" : string.Join(".", parts);
        }

        public static List<string> GeneratePerComponent(Molecule molecule, bool omitHydrogens = true)
        {
            var result = new List<string>();
            if (molecule?.atom == null || molecule.NumberOfAtoms == 0)
                return result;

            int n = molecule.NumberOfAtoms;
            var include = new bool[n];
            for (int i = 0; i < n; i++)
            {
                if (!molecule.atom[i].show) continue;
                if (omitHydrogens && molecule.atom[i].AtomicNumber == 1) continue;
                include[i] = true;
            }

            // adjacency: neighbor -> order (1/2/3)
            var adj = new Dictionary<int, int>[n];
            for (int i = 0; i < n; i++)
                adj[i] = new Dictionary<int, int>();

            for (int i = 0; i < n; i++)
            {
                if (!include[i]) continue;
                var a = molecule.atom[i];
                if (a.BondedAtom == null) continue;
                for (int k = 0; k < a.NrBondedAtoms; k++)
                {
                    int j = a.BondedAtom[k];
                    if (j <= i || j >= n || !include[j]) continue;
                    float d = Vector3.Distance(a.Coordinate, molecule.atom[j].Coordinate);
                    int order = EstimateBondOrder(
                        a.AtomicNumber, molecule.atom[j].AtomicNumber, d);
                    adj[i][j] = order;
                    adj[j][i] = order;
                }
            }

            var visitedGlob = new bool[n];
            for (int seed = 0; seed < n; seed++)
            {
                if (!include[seed] || visitedGlob[seed]) continue;

                var component = new List<int>();
                var q = new Queue<int>();
                q.Enqueue(seed);
                visitedGlob[seed] = true;
                while (q.Count > 0)
                {
                    int u = q.Dequeue();
                    component.Add(u);
                    foreach (int v in adj[u].Keys)
                    {
                        if (!visitedGlob[v] && include[v])
                        {
                            visitedGlob[v] = true;
                            q.Enqueue(v);
                        }
                    }
                }

                // Mark aromatic atoms (5–6 rings of C/N/O/S with intermediate lengths)
                var aromatic = DetectAromaticAtoms(component, adj, molecule);

                // Force aromatic–aromatic edges to order 1 (implicit aromatic bond)
                foreach (int u in component)
                {
                    if (!aromatic.Contains(u)) continue;
                    foreach (int v in adj[u].Keys.ToList())
                    {
                        if (aromatic.Contains(v))
                            adj[u][v] = 1;
                    }
                }

                var formal = new int[n];
                foreach (int i in component)
                    formal[i] = EstimateFormalCharge(i, adj, molecule);

                string smi = WriteComponentSmiles(component, adj, molecule, aromatic, formal);
                if (!string.IsNullOrEmpty(smi))
                    result.Add(smi);
            }

            return result;
        }

        // ─── aromatic detection (atoms only; bonds forced to 1 above) ───

        private static HashSet<int> DetectAromaticAtoms(
            List<int> component,
            Dictionary<int, int>[] adj,
            Molecule molecule)
        {
            var aromatic = new HashSet<int>();
            var inComp = new HashSet<int>(component);
            var path = new List<int>();
            var onPath = new HashSet<int>();
            var cycles = new List<List<int>>();

            void Dfs(int u, int parent)
            {
                path.Add(u);
                onPath.Add(u);
                foreach (int v in adj[u].Keys.OrderBy(x => x))
                {
                    if (!inComp.Contains(v) || v == parent) continue;
                    if (onPath.Contains(v))
                    {
                        int idx = path.IndexOf(v);
                        if (idx >= 0)
                        {
                            var cyc = path.GetRange(idx, path.Count - idx);
                            if (cyc.Count >= 5 && cyc.Count <= 6)
                                cycles.Add(cyc);
                        }
                        continue;
                    }
                    if (!path.Contains(v))
                        Dfs(v, u);
                }
                onPath.Remove(u);
                path.RemoveAt(path.Count - 1);
            }

            if (component.Count > 0)
                Dfs(component[0], -1);

            foreach (var cyc in cycles)
            {
                bool okEls = true;
                int aromLikeEdges = 0;
                for (int i = 0; i < cyc.Count; i++)
                {
                    int a = cyc[i];
                    int z = molecule.atom[a].AtomicNumber;
                    if (z != 6 && z != 7 && z != 8 && z != 16) { okEls = false; break; }
                    int b = cyc[(i + 1) % cyc.Count];
                    float d = Vector3.Distance(
                        molecule.atom[a].Coordinate, molecule.atom[b].Coordinate);
                    // aromatic C–C 1.35–1.45, C–N similar
                    if (d >= 1.32f && d <= 1.48f) aromLikeEdges++;
                }
                if (!okEls) continue;
                if (aromLikeEdges < cyc.Count - 1) continue; // most edges aromatic-like
                foreach (int a in cyc) aromatic.Add(a);
            }
            return aromatic;
        }

        private static int EstimateFormalCharge(int i, Dictionary<int, int>[] adj, Molecule molecule)
        {
            float partial = molecule.atom[i].charge;
            if (Math.Abs(partial) >= 0.40f)
            {
                int r = (int)Math.Round(partial);
                if (r != 0 && Math.Abs(partial - r) <= 0.55f)
                    return Math.Clamp(r, -3, 3);
            }

            int z = molecule.atom[i].AtomicNumber;
            int bondSum = adj[i].Values.Sum();
            // Quaternary nitrogen
            if (z == 7 && bondSum >= 4) return +1;
            if (z == 8 && bondSum == 1 && partial < -0.35f) return -1;
            return 0;
        }

        // ─── SMILES DFS (valid rings) ───

        private static string WriteComponentSmiles(
            List<int> atoms,
            Dictionary<int, int>[] adj,
            Molecule molecule,
            HashSet<int> aromatic,
            int[] formal)
        {
            if (atoms.Count == 0) return "";

            // Start: lowest degree, prefer carbon
            int start = atoms
                .OrderBy(i => adj[i].Count)
                .ThenBy(i => molecule.atom[i].AtomicNumber == 6 ? 0 : 1)
                .ThenBy(i => i)
                .First();

            // atom label strings (mutable — ring digits appended later)
            var label = new Dictionary<int, StringBuilder>();
            foreach (int a in atoms)
            {
                label[a] = new StringBuilder(AtomToken(
                    molecule.atom[a].AtomicNumber,
                    formal[a],
                    aromatic.Contains(a)));
            }

            var visited = new HashSet<int>();
            var parent = new Dictionary<int, int>();
            var path = new HashSet<int>();
            // edge (min,max) already used as ring
            var ringEdges = new HashSet<(int, int)>();
            int nextRing = 1;

            // Output structure built as tokens; atom positions are markers
            string Walk(int u)
            {
                visited.Add(u);
                path.Add(u);
                var sb = new StringBuilder();
                sb.Append('\x01').Append(u).Append('\x01'); // marker for atom u

                // Neighbors ordered for stability
                var nbs = adj[u].Keys.OrderBy(v => v).ToList();
                var kids = new List<int>();
                foreach (int v in nbs)
                {
                    if (parent.TryGetValue(u, out int p) && v == p) continue;

                    if (!visited.Contains(v))
                    {
                        kids.Add(v);
                        continue;
                    }

                    // Back-edge → ring (only if v still on path = ancestor)
                    if (!path.Contains(v)) continue;
                    int lo = Math.Min(u, v), hi = Math.Max(u, v);
                    if (!ringEdges.Add((lo, hi))) continue;
                    if (nextRing > 99) continue;

                    int rn = nextRing++;
                    // Digit on BOTH atoms, NO bond symbol before digit
                    label[u].Append(RingDigits(rn));
                    label[v].Append(RingDigits(rn));
                }

                for (int i = 0; i < kids.Count; i++)
                {
                    int v = kids[i];
                    parent[v] = u;
                    int order = adj[u][v];
                    bool aromBond = aromatic.Contains(u) && aromatic.Contains(v);
                    string bond = BondSymbol(order, aromBond);
                    bool branch = (i < kids.Count - 1);
                    if (branch) sb.Append('(');
                    sb.Append(bond);
                    sb.Append(Walk(v));
                    if (branch) sb.Append(')');
                }

                path.Remove(u);
                return sb.ToString();
            }

            string tree = Walk(start);

            // Substitute markers with final labels
            var outSb = new StringBuilder();
            int idx = 0;
            while (idx < tree.Length)
            {
                if (tree[idx] == '\x01')
                {
                    int end = tree.IndexOf('\x01', idx + 1);
                    if (end > idx)
                    {
                        int id = int.Parse(tree.Substring(idx + 1, end - idx - 1));
                        outSb.Append(label[id]);
                        idx = end + 1;
                        continue;
                    }
                }
                outSb.Append(tree[idx]);
                idx++;
            }

            // Any unvisited atoms in component (shouldn't happen)
            foreach (int a in atoms)
            {
                if (visited.Contains(a)) continue;
                if (outSb.Length > 0) outSb.Append('.');
                outSb.Append(label[a]);
            }

            return outSb.ToString();
        }

        private static string RingDigits(int num)
            => num < 10 ? num.ToString() : "%" + num.ToString("00");

        private static string BondSymbol(int order, bool aromaticBond)
        {
            // Aromatic bonds: always implicit
            if (aromaticBond) return "";
            return order switch
            {
                2 => "=",
                3 => "#",
                _ => ""
            };
        }

        private static string AtomToken(int z, int fc, bool aromatic)
        {
            string sym = Symbol(z);
            if (aromatic && z is 6 or 7 or 8 or 16)
                sym = sym.ToLowerInvariant();

            bool organic = z is 5 or 6 or 7 or 8 or 9 or 15 or 16 or 17 or 35 or 53;
            if (organic && fc == 0)
                return sym;

            var sb = new StringBuilder();
            sb.Append('[');
            // Keep lowercase inside brackets for aromatic charged atoms: [n+]
            sb.Append(sym);
            if (fc > 0) sb.Append(fc == 1 ? "+" : "+" + fc);
            else if (fc < 0) sb.Append(fc == -1 ? "-" : "-" + (-fc));
            sb.Append(']');
            return sb.ToString();
        }

        private static string Symbol(int z)
        {
            if (z >= 0 && z < VdwParameters.ElementSymbols.Length
                && !string.IsNullOrEmpty(VdwParameters.ElementSymbols[z]))
                return VdwParameters.ElementSymbols[z];
            return z switch
            {
                1 => "H", 6 => "C", 7 => "N", 8 => "O", 9 => "F",
                15 => "P", 16 => "S", 17 => "Cl", 35 => "Br", 53 => "I",
                _ => "X" + z
            };
        }

        /// <summary>
        /// Strict length tables — aromatic C–C (1.35–1.45) stays SINGLE here;
        /// aromatic perception upgrades the atom symbols, not the bond order.
        /// </summary>
        private static int EstimateBondOrder(int z1, int z2, float d)
        {
            int lo = Math.Min(z1, z2), hi = Math.Max(z1, z2);

            if (lo == 6 && hi == 6)
            {
                if (d < 1.22f) return 3;          // C≡C
                if (d < 1.34f) return 2;          // C=C (alkene); aromatic ~1.39 stays 1
                return 1;
            }
            if (lo == 6 && hi == 8)
            {
                if (d < 1.30f) return 2;          // C=O
                return 1;
            }
            if (lo == 6 && hi == 7)
            {
                if (d < 1.22f) return 3;
                if (d < 1.32f) return 2;          // C=N; aromatic C–N ~1.34–1.37 → 1
                return 1;
            }
            if (lo == 7 && hi == 7)
            {
                if (d < 1.18f) return 3;
                if (d < 1.30f) return 2;
                return 1;
            }
            if (lo == 8 && hi == 8)
            {
                if (d < 1.28f) return 2;
                return 1;
            }
            if (lo == 6 && hi == 16)
            {
                if (d < 1.60f) return 2;
                return 1;
            }

            float r0 = Cov(z1) + Cov(z2);
            float ratio = d / Math.Max(r0, 0.5f);
            if (ratio < 0.78f) return 3;
            if (ratio < 0.90f) return 2;
            return 1;
        }

        private static float Cov(int z) => z switch
        {
            1 => 0.31f, 5 => 0.84f, 6 => 0.76f, 7 => 0.71f, 8 => 0.66f,
            9 => 0.57f, 15 => 1.07f, 16 => 1.05f, 17 => 1.02f,
            35 => 1.20f, 53 => 1.39f,
            _ => 0.70f + 0.02f * Math.Max(z, 1)
        };
    }
}
