// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// Hydrogen-bond graph-set topology (Etter / Bernstein style).
// Classifies motifs as rings (R), chains (C), discrete (D), or self (S).

using System.Collections.Generic;
using System.Diagnostics;

namespace Tal.Calculate
{
    /// <summary>
    /// One H-bond motif with graph-set notation.
    /// </summary>
    public class HBondMotif
    {
        public string Type;          // R, C, D, S
        public string Notation;      // e.g. R2^2(8), C(4), D, S(6)
        public int Donors;
        public int Acceptors;
        public int HBonds;
        public int Degree;           // atoms in the pattern (approx)
        public string Description;
    }

    /// <summary>
    /// Analyses recorded H-bonds and returns topology motifs.
    /// </summary>
    public static class HBondTopology
    {
        private struct Edge
        {
            public int DonorIdx;
            public int AccIdx;
            public int DonorMol;
            public int AccMol;
            public int DonorCellX, DonorCellY, DonorCellZ;
            public int AccCellX, AccCellY, AccCellZ;
            public string DonorName;
            public string AccName;
        }

        public static List<HBondMotif> Analyze(Molecule molecule)
        {
            var result = new List<HBondMotif>();
            if (molecule?.atom == null || molecule.NumberOfAtoms == 0)
                return result;

            var edges = CollectEdges(molecule);
            if (edges.Count == 0)
            {
                result.Add(new HBondMotif
                {
                    Type = "—",
                    Notation = "—",
                    Description = "No hydrogen bonds recorded"
                });
                return result;
            }

            // Separate intramolecular (S) from intermolecular
            var intra = new List<Edge>();
            var inter = new List<Edge>();
            foreach (var e in edges)
            {
                bool sameMol = e.DonorMol >= 0 && e.DonorMol == e.AccMol;
                bool sameCell = e.DonorCellX == e.AccCellX &&
                                e.DonorCellY == e.AccCellY &&
                                e.DonorCellZ == e.AccCellZ;
                if (sameMol && sameCell)
                    intra.Add(e);
                else
                    inter.Add(e);
            }

            // Self (intramolecular)
            if (intra.Count > 0)
            {
                int donors = UniqueDonors(intra);
                int accs = UniqueAcceptors(intra);
                int degree = EstimateDegree(intra);
                result.Add(new HBondMotif
                {
                    Type = "S",
                    Notation = $"S({degree})",
                    Donors = donors,
                    Acceptors = accs,
                    HBonds = intra.Count,
                    Degree = degree,
                    Description = "Intramolecular (self) H-bonds"
                });
            }

            if (inter.Count == 0)
            {
                Debug.WriteLine($"[HBondTopology] {result.Count} motifs (intra only)");
                return result;
            }

            // Build adjacency for intermolecular graph (nodes = molecule instances)
            // Node key: (molId, cellX, cellY, cellZ)
            var nodes = new Dictionary<string, int>();
            var adj = new Dictionary<int, List<int>>();
            int nextId = 0;

            int GetNode(int mol, int cx, int cy, int cz)
            {
                string key = $"{mol}|{cx},{cy},{cz}";
                if (!nodes.TryGetValue(key, out int id))
                {
                    id = nextId++;
                    nodes[key] = id;
                    adj[id] = new List<int>();
                }
                return id;
            }

            foreach (var e in inter)
            {
                int a = GetNode(e.DonorMol, e.DonorCellX, e.DonorCellY, e.DonorCellZ);
                int b = GetNode(e.AccMol, e.AccCellX, e.AccCellY, e.AccCellZ);
                if (a == b) continue;
                if (!adj[a].Contains(b)) adj[a].Add(b);
                if (!adj[b].Contains(a)) adj[b].Add(a);
            }

            // Detect cycles → rings
            var visited = new HashSet<int>();
            var rings = new List<List<int>>();
            foreach (var start in adj.Keys)
            {
                if (visited.Contains(start)) continue;
                var path = new List<int>();
                var pathSet = new HashSet<int>();
                FindCycles(start, -1, path, pathSet, adj, rings, visited);
            }

            // Rings
            var usedInRing = new HashSet<int>();
            foreach (var cycle in rings)
            {
                foreach (int n in cycle) usedInRing.Add(n);
                int hbonds = CountEdgesInCycle(cycle, inter, nodes);
                int donors = UniqueDonorsInNodes(cycle, inter, nodes);
                int accs = UniqueAcceptorsInNodes(cycle, inter, nodes);
                int degree = System.Math.Max(hbonds * 2, cycle.Count * 2);
                result.Add(new HBondMotif
                {
                    Type = "R",
                    Notation = $"R{donors}^{accs}({degree})",
                    Donors = donors,
                    Acceptors = accs,
                    HBonds = hbonds,
                    Degree = degree,
                    Description = $"Ring of {cycle.Count} molecules, {hbonds} H-bonds"
                });
            }

            // Chains: components that extend via cell translation
            var components = FindComponents(adj);
            foreach (var comp in components)
            {
                bool extends = ComponentExtendsViaCell(comp, nodes);
                bool allInRing = true;
                foreach (int n in comp)
                    if (!usedInRing.Contains(n)) { allInRing = false; break; }
                if (allInRing && !extends) continue;

                int donors = 0, accs = 0, hb = 0;
                foreach (var e in inter)
                {
                    int a = NodeId(e.DonorMol, e.DonorCellX, e.DonorCellY, e.DonorCellZ, nodes);
                    int b = NodeId(e.AccMol, e.AccCellX, e.AccCellY, e.AccCellZ, nodes);
                    if (comp.Contains(a) || comp.Contains(b))
                    {
                        hb++;
                        donors++;
                        accs++;
                    }
                }

                if (extends)
                {
                    result.Add(new HBondMotif
                    {
                        Type = "C",
                        Notation = $"C({System.Math.Max(4, comp.Count * 2)})",
                        Donors = donors,
                        Acceptors = accs,
                        HBonds = hb,
                        Degree = System.Math.Max(4, comp.Count * 2),
                        Description = $"Chain extending via cell translation ({comp.Count} sites)"
                    });
                }
                else if (!allInRing)
                {
                    result.Add(new HBondMotif
                    {
                        Type = "D",
                        Notation = "D",
                        Donors = donors,
                        Acceptors = accs,
                        HBonds = hb,
                        Degree = comp.Count * 2,
                        Description = $"Discrete finite motif ({comp.Count} molecules)"
                    });
                }
            }

            Debug.WriteLine($"[HBondTopology] {result.Count} motifs from {edges.Count} H-bonds");
            return result;
        }

        private static List<Edge> CollectEdges(Molecule molecule)
        {
            var edges = new List<Edge>();
            var seen = new HashSet<string>();

            for (int i = 0; i < molecule.NumberOfAtoms; i++)
            {
                var a = molecule.atom[i];
                if (a.H_NrBondedAtoms <= 0 || a.H_BondedAtom == null) continue;

                for (int k = 0; k < a.H_NrBondedAtoms; k++)
                {
                    int acc = a.H_BondedAtom[k];
                    if (acc < 0 || acc >= molecule.NumberOfAtoms) continue;

                    // Host is either H or donor depending on draw mode
                    int donor = i;
                    if (a.AtomicNumber == 1)
                    {
                        // Find donor bonded to this H
                        for (int j = 0; j < a.NrBondedAtoms; j++)
                        {
                            int c = a.BondedAtom[j];
                            if (c >= 0 && c < molecule.NumberOfAtoms &&
                                molecule.atom[c].AtomicNumber != 1)
                            { donor = c; break; }
                        }
                    }

                    var d = molecule.atom[donor];
                    var ac = molecule.atom[acc];
                    string key = $"{donor}|{acc}|{d.CellShiftX},{d.CellShiftY},{d.CellShiftZ}";
                    if (!seen.Add(key)) continue;

                    edges.Add(new Edge
                    {
                        DonorIdx = donor,
                        AccIdx = acc,
                        DonorMol = d.IndividualMoleculeInCell,
                        AccMol = ac.IndividualMoleculeInCell,
                        DonorCellX = d.CellShiftX,
                        DonorCellY = d.CellShiftY,
                        DonorCellZ = d.CellShiftZ,
                        AccCellX = ac.CellShiftX,
                        AccCellY = ac.CellShiftY,
                        AccCellZ = ac.CellShiftZ,
                        DonorName = d.name ?? "?",
                        AccName = ac.name ?? "?"
                    });
                }
            }
            return edges;
        }

        private static int UniqueDonors(List<Edge> edges)
        {
            var set = new HashSet<string>();
            foreach (var e in edges)
                set.Add($"{e.DonorMol}|{e.DonorName}");
            return set.Count;
        }

        private static int UniqueAcceptors(List<Edge> edges)
        {
            var set = new HashSet<string>();
            foreach (var e in edges)
                set.Add($"{e.AccMol}|{e.AccName}");
            return set.Count;
        }

        private static int EstimateDegree(List<Edge> edges)
        {
            // Rough: each H-bond contributes ~3 atoms (D, H, A) but shared
            return System.Math.Max(4, edges.Count * 2 + 2);
        }

        private static void FindCycles(
            int node, int parent,
            List<int> path, HashSet<int> pathSet,
            Dictionary<int, List<int>> adj,
            List<List<int>> rings, HashSet<int> visited)
        {
            visited.Add(node);
            path.Add(node);
            pathSet.Add(node);

            if (adj.TryGetValue(node, out var neighbors))
            {
                foreach (int next in neighbors)
                {
                    if (next == parent) continue;
                    if (pathSet.Contains(next))
                    {
                        // Cycle found
                        int idx = path.IndexOf(next);
                        if (idx >= 0 && path.Count - idx >= 3)
                        {
                            var cycle = path.GetRange(idx, path.Count - idx);
                            if (cycle.Count <= 8) // reasonable ring size
                                rings.Add(cycle);
                        }
                    }
                    else if (!visited.Contains(next))
                    {
                        FindCycles(next, node, path, pathSet, adj, rings, visited);
                    }
                }
            }

            path.RemoveAt(path.Count - 1);
            pathSet.Remove(node);
        }

        private static int CountEdgesInCycle(List<int> cycle, List<Edge> edges, Dictionary<string, int> nodes)
        {
            int count = 0;
            var set = new HashSet<int>(cycle);
            foreach (var e in edges)
            {
                int a = NodeId(e.DonorMol, e.DonorCellX, e.DonorCellY, e.DonorCellZ, nodes);
                int b = NodeId(e.AccMol, e.AccCellX, e.AccCellY, e.AccCellZ, nodes);
                if (set.Contains(a) && set.Contains(b)) count++;
            }
            return System.Math.Max(count, cycle.Count);
        }

        private static int UniqueDonorsInNodes(List<int> cycle, List<Edge> edges, Dictionary<string, int> nodes)
        {
            var set = new HashSet<int>(cycle);
            var donors = new HashSet<string>();
            foreach (var e in edges)
            {
                int a = NodeId(e.DonorMol, e.DonorCellX, e.DonorCellY, e.DonorCellZ, nodes);
                if (set.Contains(a))
                    donors.Add($"{e.DonorMol}|{e.DonorName}");
            }
            return donors.Count;
        }

        private static int UniqueAcceptorsInNodes(List<int> cycle, List<Edge> edges, Dictionary<string, int> nodes)
        {
            var set = new HashSet<int>(cycle);
            var accs = new HashSet<string>();
            foreach (var e in edges)
            {
                int b = NodeId(e.AccMol, e.AccCellX, e.AccCellY, e.AccCellZ, nodes);
                if (set.Contains(b))
                    accs.Add($"{e.AccMol}|{e.AccName}");
            }
            return accs.Count;
        }

        private static int NodeId(int mol, int cx, int cy, int cz, Dictionary<string, int> nodes)
        {
            string key = $"{mol}|{cx},{cy},{cz}";
            return nodes.TryGetValue(key, out int id) ? id : -1;
        }

        private static List<HashSet<int>> FindComponents(Dictionary<int, List<int>> adj)
        {
            var result = new List<HashSet<int>>();
            var seen = new HashSet<int>();
            foreach (var start in adj.Keys)
            {
                if (seen.Contains(start)) continue;
                var comp = new HashSet<int>();
                var stack = new Stack<int>();
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int n = stack.Pop();
                    if (!comp.Add(n)) continue;
                    seen.Add(n);
                    if (adj.TryGetValue(n, out var nbrs))
                        foreach (int m in nbrs)
                            if (!comp.Contains(m)) stack.Push(m);
                }
                result.Add(comp);
            }
            return result;
        }

        private static bool ComponentExtendsViaCell(HashSet<int> comp, Dictionary<string, int> nodes)
        {
            // If any node key has non-zero cell shift and the component has >1 node, it extends
            var reverse = new Dictionary<int, string>();
            foreach (var kv in nodes)
                reverse[kv.Value] = kv.Key;

            bool hasZero = false, hasNonZero = false;
            foreach (int n in comp)
            {
                if (!reverse.TryGetValue(n, out string key)) continue;
                // key = "mol|cx,cy,cz"
                var parts = key.Split('|');
                if (parts.Length < 2) continue;
                var cell = parts[1].Split(',');
                bool zero = cell.Length == 3 && cell[0] == "0" && cell[1] == "0" && cell[2] == "0";
                if (zero) hasZero = true;
                else hasNonZero = true;
            }
            return hasZero && hasNonZero;
        }
    }
}
