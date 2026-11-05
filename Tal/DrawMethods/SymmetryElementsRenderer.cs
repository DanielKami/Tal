// Author: Daniel M. Kaminski
// Year: 2026
// Location: Lublin
// Project: TAL
// License: see license.txt (Academic / Non-Commercial)
//
// 3D drawing / rendering module: SymmetryElementsRenderer.

﻿// Author: Daniel M. Kaminski / TAL
// Year: 2026
// Project: TAL
//
// ═══════════════════════════════════════════════════════════════════════
// CellSymmetryRenderer — elementy symmetry w stylu Mercury / ITA
// ═══════════════════════════════════════════════════════════════════════
//
// ⭐ LIVE COLOR READ:
// Colours are NOT cached in CellSymElement.Color. The renderer
// calls GetColorForElement(kind, order, sub) every frame
// w Draw() i przekazuje result do DrawAxisArrow / DrawPlane / itd.
// Zmiana Flags.Symmetry*Color* jest visible immediately.
//
// ⭐ LIVE SIZE READ:
// AxisThickness, MarkerSize, InversionR are properties
// reading Flags.Instance every frame.
//
// ⭐ Invalidate():
// Sets _needsRebuild = true. The next Draw() rebuilds _elements
// z _lastMolecule. Used when zmienia geometria (np. new grupa).
//
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Tal.DrawMethods
{
    #region Public data types

    public enum CellSymElementKind
    {
        RotationAxis,
        ScrewAxis,
        MirrorPlane,
        GlidePlane,
        InversionCenter,
        RotoinversionAxis
    }

 /// <summary>Afterjedyncza KOPIA elementu symmetry w cell (shifted-cartesian).</summary>
    public sealed class CellSymElement
    {
        public CellSymElementKind Kind;
        public int Order;
        public int ScrewSubscript;
        public string GlideLetter = "";

 // Axis / rotoinversion: segment clipped to the cell
        public Vector3 SegmentStart;
        public Vector3 SegmentEnd;
        public Vector3 Direction;
        public bool HasSegment;

 // Plane: polygon of intersection with the cell
        public Vector3[] Polygon = System.Array.Empty<Vector3>();

 // Punkt: centrum inversion
        public Vector3 Point;

 // Centre rotoinversion
        public bool HasCenter;
        public Vector3 CenterPoint;

 // ⚠️ Color is NOT used for drawing (live-read in Draw).
 // Kept for backward compatibility.
        public XnaColor Color;

        public string Label = "";
    }

    #endregion

    public sealed class CellSymmetryRenderer : IDisposable
    {
        #region Fields

        private readonly List<CellSymElement> _elements = new();
        private bool _disposed;

 // Backup rebuild
        private bool _needsRebuild = false;
        private Molecule _lastMolecule;

        private BasicEffect _markerEffect;
        private GraphicsDevice _cachedDevice;
        private VertexPositionColor[] _markerBuffer = new VertexPositionColor[8192];
        private VertexPositionColor[] _markerBufferTx = new VertexPositionColor[8192];
        private Matrix _sr = Matrix.Identity;

 // ═══ Arrow geometry constants ═══
        private const float ARROW_LEN = 0.35f;       // długość grotu (stożka)
        private const float ARROW_RAD = 0.09f;       // promień grotu
        private const float ARROW_GAP_FACTOR = 5f;   // grot odsunięty o 5×ARROW_LEN
        private const float GLIDE_ARROW_LEN = 0.8f;  // długość strzałki poślizgu

 // ═══ LIVE READ — z Flagi, w each klatce ═══
        private static float AxisThickness => Flags.Instance.SymmetryAxisThickness;
        private static float MarkerSize => Flags.Instance.SymmetryMarkerSize;
        private static float InversionR => Flags.Instance.SymmetryCenterRadius;

        #endregion

        #region Build

        private sealed class SymOp
        {
            public int[] R = new int[9];
            public double[] T = new double[3];
        }

        private enum OpClass { None, Rotation, Inversion, Mirror, Rotoinversion }

        public void BuildFromMolecule(Molecule molecule)
        {
            _elements.Clear();
            _lastMolecule = molecule;
            _needsRebuild = false;

            if (molecule == null || molecule.SymmetryOperations == null ||
                molecule.SymmetryOperations.Count == 0)
                return;

            Matrix U;
            try { U = Matrix.Transpose(molecule.FractionaltoCarttesianMatrix(0, 0, 0)); }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CellSym] FracToCart failed: {ex.Message}");
                return;
            }

            Vector3 shift = molecule.MoleculeShift;
            List<SymOp> ops = ExpandToGroup(molecule.SymmetryOperations);
            var seen = new HashSet<string>();

            foreach (SymOp op in ops)
            {
                OpClass cls = ClassifyR(op.R, out int order);
                switch (cls)
                {
                    case OpClass.Rotation: AddRotationFamily(op, order, U, shift, seen); break;
                    case OpClass.Rotoinversion: AddRotoinversionFamily(op, order, U, shift, seen); break;
                    case OpClass.Mirror: AddPlaneFamily(op, U, shift, seen); break;
                    case OpClass.Inversion: AddInversionFamily(op, U, shift, seen); break;
                }
            }

            Debug.WriteLine($"[CellSym] Built {_elements.Count} element copies");
        }

        private void AddRotationFamily(SymOp op, int order, Matrix U, Vector3 shift, HashSet<string> seen)
        {
            if (!NullVectorFromRows(IMinusR(op.R), out int[] d)) return;
            int sense = order > 2 ? RotationSense(op.R, d, U) : 1;
            double dd = d[0] * d[0] + d[1] * d[1] + d[2] * d[2];

            for (int lx = -2; lx <= 2; lx++)
                for (int ly = -2; ly <= 2; ly++)
                    for (int lz = -2; lz <= 2; lz++)
                    {
                        double[] tt = { op.T[0] + lx, op.T[1] + ly, op.T[2] + lz };
                        double[] w = Intrinsic(op.R, tt, order);
                        double[] loc = { tt[0] - w[0], tt[1] - w[1], tt[2] - w[2] };

                        if (!SolveOnLine(op.R, loc, d, out double[] p)) continue;
                        if (!ClipLine(p, d, out double t0, out double t1)) continue;

                        double lam = (w[0] * d[0] + w[1] * d[1] + w[2] * d[2]) / dd;
                        double mu = sense * lam;
                        double f = mu - Math.Floor(mu + 1e-9);
                        int sub = (int)Math.Round(f * order) % order;

                        CellSymElementKind kind = sub == 0
                            ? CellSymElementKind.RotationAxis
                            : CellSymElementKind.ScrewAxis;

                        string key = $"AX|{order}|{sub}|{d[0]},{d[1]},{d[2]}|{F3(p[0])},{F3(p[1])},{F3(p[2])}";
                        if (!seen.Add(key)) continue;

                        _elements.Add(new CellSymElement
                        {
                            Kind = kind,
                            Order = order,
                            ScrewSubscript = sub,
                            Label = sub == 0 ? $"{order}" : $"{order}({sub})",
                            Direction = DirCart(d, U),
                            SegmentStart = CartPoint(p, d, t0, U, shift),
                            SegmentEnd = CartPoint(p, d, t1, U, shift),
                            HasSegment = true,
                            Color = XnaColor.White
                        });
                    }
        }

        private void AddRotoinversionFamily(SymOp op, int order, Matrix U, Vector3 shift, HashSet<string> seen)
        {
            if (!NullVectorFromRows(IPlusR(op.R), out int[] d)) return;
            int[] imr = IMinusR(op.R);

            for (int lx = -2; lx <= 2; lx++)
                for (int ly = -2; ly <= 2; ly++)
                    for (int lz = -2; lz <= 2; lz++)
                    {
                        double[] tt = { op.T[0] + lx, op.T[1] + ly, op.T[2] + lz };
                        if (!SolveFull(imr, tt, out double[] p)) continue;

                        int k = FirstNonZero(d);
                        double s = p[k] / d[k];
                        double[] p0 = { p[0] - s * d[0], p[1] - s * d[1], p[2] - s * d[2] };

                        if (ClipLine(p0, d, out double t0, out double t1))
                        {
                            string keyL = $"RI|{order}|{d[0]},{d[1]},{d[2]}|{F3(p0[0])},{F3(p0[1])},{F3(p0[2])}";
                            if (seen.Add(keyL))
                            {
                                _elements.Add(new CellSymElement
                                {
                                    Kind = CellSymElementKind.RotoinversionAxis,
                                    Order = order,
                                    Label = $"-{order}",
                                    Direction = DirCart(d, U),
                                    SegmentStart = CartPoint(p0, d, t0, U, shift),
                                    SegmentEnd = CartPoint(p0, d, t1, U, shift),
                                    HasSegment = true,
                                    Color = XnaColor.White
                                });
                            }
                        }

                        if (InsideCell(p))
                        {
                            string keyC = $"RC|{order}|{d[0]},{d[1]},{d[2]}|{F3(p[0])},{F3(p[1])},{F3(p[2])}";
                            if (seen.Add(keyC))
                            {
                                _elements.Add(new CellSymElement
                                {
                                    Kind = CellSymElementKind.RotoinversionAxis,
                                    Order = order,
                                    Label = $"-{order}",
                                    Direction = DirCart(d, U),
                                    HasSegment = false,
                                    HasCenter = true,
                                    CenterPoint = Vector3.Transform(
                                        new Vector3((float)p[0], (float)p[1], (float)p[2]), U) - shift,
                                    Color = XnaColor.White
                                });
                            }
                        }
                    }
        }

        private void AddPlaneFamily(SymOp op, Matrix U, Vector3 shift, HashSet<string> seen)
        {
            int[] M = IMinusR(op.R);
            int row = -1, best = 0;
            for (int i = 0; i < 3; i++)
            {
                int sum = Math.Abs(M[i * 3]) + Math.Abs(M[i * 3 + 1]) + Math.Abs(M[i * 3 + 2]);
                if (sum > best) { best = sum; row = i; }
            }
            if (row < 0) return;

            int g = Gcd(Gcd(M[row * 3], M[row * 3 + 1]), M[row * 3 + 2]);
            if (g == 0) return;

            int[] r = { M[row * 3] / g, M[row * 3 + 1] / g, M[row * 3 + 2] / g };
            double sign = 1.0;
            int first = FirstNonZero(r);
            if (r[first] < 0) { r[0] = -r[0]; r[1] = -r[1]; r[2] = -r[2]; sign = -1.0; }

            for (int lx = -2; lx <= 2; lx++)
                for (int ly = -2; ly <= 2; ly++)
                    for (int lz = -2; lz <= 2; lz++)
                    {
                        double[] tt = { op.T[0] + lx, op.T[1] + ly, op.T[2] + lz };
                        double[] rt = MulRv(op.R, tt);
                        double[] w = { (tt[0] + rt[0]) * 0.5, (tt[1] + rt[1]) * 0.5, (tt[2] + rt[2]) * 0.5 };
                        double loc = tt[row] - w[row];
                        double c = sign * loc / g;

                        List<Vector3> polyFrac = PlanePolygonFrac(r, c);
                        if (polyFrac.Count < 3) continue;

                        Vector3[] polyCart = OrderPolygon(polyFrac, U, shift);
                        if (polyCart.Length < 3) continue;

                        string label = GlideLabel(w);
                        string key = $"PL|{label}|{r[0]},{r[1]},{r[2]}|{F3(c)}";
                        if (!seen.Add(key)) continue;

                        CellSymElementKind kind = label == "m"
                            ? CellSymElementKind.MirrorPlane
                            : CellSymElementKind.GlidePlane;

                        Vector3 centroid = Vector3.Zero;
                        foreach (Vector3 v in polyCart) centroid += v;
                        centroid /= polyCart.Length;

                        var el = new CellSymElement
                        {
                            Kind = kind,
                            Order = 2,
                            GlideLetter = label == "m" ? "" : label,
                            Label = label,
                            Polygon = polyCart,
                            Point = centroid,
                            Color = XnaColor.White
                        };

                        if (kind == CellSymElementKind.GlidePlane)
                        {
                            var wr = new Vector3(
                                (float)(w[0] - Math.Round(w[0])),
                                (float)(w[1] - Math.Round(w[1])),
                                (float)(w[2] - Math.Round(w[2])));
                            Vector3 gCart = Vector3.TransformNormal(wr, U);
                            if (gCart.LengthSquared() > 1e-8f)
                                el.Direction = Vector3.Normalize(gCart);
                        }

                        _elements.Add(el);
                    }
        }

        private void AddInversionFamily(SymOp op, Matrix U, Vector3 shift, HashSet<string> seen)
        {
            for (int lx = -2; lx <= 2; lx++)
                for (int ly = -2; ly <= 2; ly++)
                    for (int lz = -2; lz <= 2; lz++)
                    {
                        double[] p = {
                    (op.T[0] + lx) * 0.5,
                    (op.T[1] + ly) * 0.5,
                    (op.T[2] + lz) * 0.5
                };
                        if (!InsideCell(p)) continue;

                        string key = $"PT|{F3(p[0])},{F3(p[1])},{F3(p[2])}";
                        if (!seen.Add(key)) continue;

                        _elements.Add(new CellSymElement
                        {
                            Kind = CellSymElementKind.InversionCenter,
                            Point = Vector3.Transform(
                                new Vector3((float)p[0], (float)p[1], (float)p[2]), U) - shift,
                            Color = XnaColor.White,
                            Label = "-1"
                        });
                    }
        }

        #endregion

        #region Public — Invalidate / Dispose

 /// <summary>
 /// Marks the _elements cache as stale. The next Draw()
 /// rebuilds _elements z _lastMolecule.
 ///
 /// Nie jest required do changes colouru (colour live z Flagi),
 /// ale jest backupem for changes geometrii.
 /// </summary>
        public void Invalidate()
        {
            _needsRebuild = true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _elements.Clear();
            _markerEffect?.Dispose();
            _markerEffect = null;
        }

        #endregion

        #region Geometry helpers

        private static bool ClipLine(double[] p, int[] d, out double t0, out double t1)
        {
            t0 = double.NegativeInfinity;
            t1 = double.PositiveInfinity;

            for (int a = 0; a < 3; a++)
            {
                if (d[a] == 0)
                {
                    if (p[a] < -1e-6 || p[a] > 1.0 + 1e-6) return false;
                    continue;
                }

                double ta = (0.0 - p[a]) / d[a];
                double tb = (1.0 - p[a]) / d[a];
                if (ta > tb) { double tmp = ta; ta = tb; tb = tmp; }

                t0 = Math.Max(t0, ta);
                t1 = Math.Min(t1, tb);
                if (t0 > t1 + 1e-9) return false;
            }
            return t1 - t0 > 1e-6;
        }

        private static bool InsideCell(double[] p)
        {
            const double e = 1e-6;
            return p[0] > -e && p[0] < 1.0 + e &&
                   p[1] > -e && p[1] < 1.0 + e &&
                   p[2] > -e && p[2] < 1.0 + e;
        }

        private static List<Vector3> PlanePolygonFrac(int[] r, double c)
        {
            Vector3[] corners = {
                new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(0,1,0),
                new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1),
            };
            int[,] edges = {
                {0,1},{1,2},{2,3},{3,0},
                {4,5},{5,6},{6,7},{7,4},
                {0,4},{1,5},{2,6},{3,7},
            };

            var pts = new List<Vector3>();
            for (int e = 0; e < 12; e++)
            {
                Vector3 a = corners[edges[e, 0]];
                Vector3 b = corners[edges[e, 1]];

                double da = r[0] * a.X + r[1] * a.Y + r[2] * a.Z - c;
                double db = r[0] * b.X + r[1] * b.Y + r[2] * b.Z - c;

                if (Math.Abs(da) < 1e-7) AddUniquePoint(pts, a);
                if (Math.Abs(db) < 1e-7) AddUniquePoint(pts, b);

                if ((da < -1e-7 && db > 1e-7) || (da > 1e-7 && db < -1e-7))
                {
                    float t = (float)(da / (da - db));
                    AddUniquePoint(pts, a + (b - a) * t);
                }
            }
            return pts;
        }

        private static void AddUniquePoint(List<Vector3> pts, Vector3 v)
        {
            for (int i = 0; i < pts.Count; i++)
                if ((v - pts[i]).LengthSquared() < 1e-10f) return;
            pts.Add(v);
        }

        private static Vector3[] OrderPolygon(List<Vector3> fracPts, Matrix U, Vector3 shift)
        {
            int n = fracPts.Count;
            var cart = new Vector3[n];
            Vector3 c = Vector3.Zero;
            for (int i = 0; i < n; i++)
            {
                cart[i] = Vector3.Transform(fracPts[i], U) - shift;
                c += cart[i];
            }
            c /= n;

            Vector3 normal = Vector3.Zero;
            for (int i = 1; i < n && normal.LengthSquared() < 1e-10f; i++)
                for (int j = i + 1; j < n && normal.LengthSquared() < 1e-10f; j++)
                    normal = Vector3.Cross(cart[i] - cart[0], cart[j] - cart[0]);
            if (normal.LengthSquared() < 1e-10f) return Array.Empty<Vector3>();
            normal.Normalize();

            Vector3 s1 = Vector3.Zero;
            for (int i = 0; i < n; i++)
            {
                Vector3 v = cart[i] - c;
                if (v.LengthSquared() > 1e-10f) { s1 = Vector3.Normalize(v); break; }
            }
            if (s1.LengthSquared() < 1e-10f) return Array.Empty<Vector3>();
            Vector3 s2 = Vector3.Cross(normal, s1);

            var angles = new float[n];
            for (int i = 0; i < n; i++)
                angles[i] = MathF.Atan2(Vector3.Dot(cart[i] - c, s2), Vector3.Dot(cart[i] - c, s1));

            Array.Sort(angles, cart);
            return cart;
        }

        private static Vector3 CartPoint(double[] p, int[] d, double t, Matrix U, Vector3 shift)
        {
            var f = new Vector3(
                (float)(p[0] + d[0] * t),
                (float)(p[1] + d[1] * t),
                (float)(p[2] + d[2] * t));
            return Vector3.Transform(f, U) - shift;
        }

        private static Vector3 DirCart(int[] d, Matrix U)
        {
            Vector3 v = Vector3.TransformNormal(new Vector3(d[0], d[1], d[2]), U);
            return v.LengthSquared() > 1e-12f ? Vector3.Normalize(v) : Vector3.UnitZ;
        }

        private static string F3(double x)
            => (Math.Round(x, 3) + 0.0).ToString("F3", System.Globalization.CultureInfo.InvariantCulture);

        #endregion

        #region Analysis helpers

        private static List<SymOp> ExpandToGroup(List<SymmetryElements.ReadSymm> input)
        {
            var map = new Dictionary<string, SymOp>();
            var work = new List<SymOp>();

            foreach (var s in input)
            {
                SymOp o = FromReadSymm(s);
                if (map.TryAdd(OpKey(o), o)) work.Add(o);
            }

            const int MAX = 192;
            bool changed = true;
            int guard = 0;
            while (changed && guard++ < 16 && work.Count <= MAX)
            {
                changed = false;
                SymOp[] snapshot = work.ToArray();
                foreach (SymOp a in snapshot)
                    foreach (SymOp b in snapshot)
                    {
                        if (work.Count > MAX) break;
                        SymOp c = Compose(a, b);
                        if (c == null) continue;
                        if (map.TryAdd(OpKey(c), c)) { work.Add(c); changed = true; }
                    }
            }
            return work;
        }

        private static SymOp FromReadSymm(SymmetryElements.ReadSymm s)
        {
            var o = new SymOp();
            for (int row = 0; row < 3; row++)
            {
                int p1 = s.pos1[row], s1 = s.sign1[row];
                int p2 = s.pos2[row], s2 = s.sign2[row];
                if (p1 >= 0 && p1 < 3 && s1 != 0) o.R[row * 3 + p1] += s1;
                if (p2 >= 0 && p2 < 3 && s2 != 0) o.R[row * 3 + p2] += s2;
                o.T[row] = NormShift(s.shift[row]);
            }
            return o;
        }

        private static double NormShift(double x)
        {
            x -= Math.Floor(x);
            x = Math.Round(x * 144.0) / 144.0;
            if (x >= 1.0 - 1e-9) x = 0.0;
            return x;
        }

        private static SymOp Compose(SymOp a, SymOp b)
        {
            var c = new SymOp();
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    int sum = 0;
                    for (int k = 0; k < 3; k++)
                        sum += a.R[row * 3 + k] * b.R[k * 3 + col];
                    if (Math.Abs(sum) > 1) return null;
                    c.R[row * 3 + col] = sum;
                }

                c.T[row] = NormShift(
                    a.R[row * 3] * b.T[0] + a.R[row * 3 + 1] * b.T[1] + a.R[row * 3 + 2] * b.T[2]
                    + a.T[row]);
            }
            return c;
        }

        private static string OpKey(SymOp o)
        {
            return string.Join(",", o.R) + "|" +
                   (int)Math.Round(o.T[0] * 144.0) + "," +
                   (int)Math.Round(o.T[1] * 144.0) + "," +
                   (int)Math.Round(o.T[2] * 144.0);
        }

        private static OpClass ClassifyR(int[] R, out int order)
        {
            order = 0;
            int trace = R[0] + R[4] + R[8];
            int det = Det3i(R);

            if (det == 1)
            {
                switch (trace)
                {
                    case 3: return OpClass.None;
                    case -1: order = 2; return OpClass.Rotation;
                    case 0: order = 3; return OpClass.Rotation;
                    case 1: order = 4; return OpClass.Rotation;
                    case 2: order = 6; return OpClass.Rotation;
                }
                return OpClass.None;
            }

            if (det == -1)
            {
                switch (trace)
                {
                    case -3: order = 1; return OpClass.Inversion;
                    case 1: order = 2; return OpClass.Mirror;
                    case 0: order = 3; return OpClass.Rotoinversion;
                    case -1: order = 4; return OpClass.Rotoinversion;
                    case -2: order = 6; return OpClass.Rotoinversion;
                }
            }
            return OpClass.None;
        }

        private static int Det3i(int[] R)
            => R[0] * (R[4] * R[8] - R[5] * R[7])
             - R[1] * (R[3] * R[8] - R[5] * R[6])
             + R[2] * (R[3] * R[7] - R[4] * R[6]);

        private static double Det3d(double[] A)
            => A[0] * (A[4] * A[8] - A[5] * A[7])
             - A[1] * (A[3] * A[8] - A[5] * A[6])
             + A[2] * (A[3] * A[7] - A[4] * A[6]);

        private static int[] IMinusR(int[] R)
        {
            var m = new int[9];
            for (int i = 0; i < 9; i++) m[i] = ((i % 4 == 0) ? 1 : 0) - R[i];
            return m;
        }

        private static int[] IPlusR(int[] R)
        {
            var m = new int[9];
            for (int i = 0; i < 9; i++) m[i] = ((i % 4 == 0) ? 1 : 0) + R[i];
            return m;
        }

        private static double[] MulRv(int[] R, double[] v)
        {
            return new[]
            {
                R[0] * v[0] + R[1] * v[1] + R[2] * v[2],
                R[3] * v[0] + R[4] * v[1] + R[5] * v[2],
                R[6] * v[0] + R[7] * v[1] + R[8] * v[2]
            };
        }

        private static int[] MulRR(int[] A, int[] B)
        {
            var c = new int[9];
            for (int row = 0; row < 3; row++)
                for (int col = 0; col < 3; col++)
                {
                    int sum = 0;
                    for (int k = 0; k < 3; k++) sum += A[row * 3 + k] * B[k * 3 + col];
                    c[row * 3 + col] = sum;
                }
            return c;
        }

        private static int Gcd(int a, int b)
        {
            a = Math.Abs(a); b = Math.Abs(b);
            while (b != 0) { int t = a % b; a = b; b = t; }
            return a;
        }

        private static int FirstNonZero(int[] v)
            => v[0] != 0 ? 0 : (v[1] != 0 ? 1 : 2);

        private static bool NullVectorFromRows(int[] M, out int[] d)
        {
            d = new int[3];
            int[,] pairs = { { 0, 1 }, { 0, 2 }, { 1, 2 } };
            for (int q = 0; q < 3; q++)
            {
                int i = pairs[q, 0], j = pairs[q, 1];
                int ax = M[i * 3 + 1] * M[j * 3 + 2] - M[i * 3 + 2] * M[j * 3 + 1];
                int ay = M[i * 3 + 2] * M[j * 3 + 0] - M[i * 3 + 0] * M[j * 3 + 2];
                int az = M[i * 3 + 0] * M[j * 3 + 1] - M[i * 3 + 1] * M[j * 3 + 0];
                if (ax == 0 && ay == 0 && az == 0) continue;

                int g = Gcd(Gcd(ax, ay), az);
                ax /= g; ay /= g; az /= g;
                int first = ax != 0 ? ax : (ay != 0 ? ay : az);
                if (first < 0) { ax = -ax; ay = -ay; az = -az; }

                d[0] = ax; d[1] = ay; d[2] = az;
                return true;
            }
            return false;
        }

        private static double[] Intrinsic(int[] R, double[] t, int n)
        {
            var w = new double[3];
            int[] Rk = { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
            for (int k = 0; k < n; k++)
            {
                double[] v = MulRv(Rk, t);
                w[0] += v[0]; w[1] += v[1]; w[2] += v[2];
                Rk = MulRR(Rk, R);
            }
            w[0] /= n; w[1] /= n; w[2] /= n;
            return w;
        }

        private static bool SolveOnLine(int[] R, double[] rhs, int[] d, out double[] p)
        {
            p = new double[3];
            int[] M = IMinusR(R);
            int k = FirstNonZero(d);
            int j1 = k == 0 ? 1 : 0;
            int j2 = k == 2 ? 1 : 2;

            double[] c1 = { M[0 * 3 + j1], M[1 * 3 + j1], M[2 * 3 + j1] };
            double[] c2 = { M[0 * 3 + j2], M[1 * 3 + j2], M[2 * 3 + j2] };

            double a11 = c1[0] * c1[0] + c1[1] * c1[1] + c1[2] * c1[2];
            double a12 = c1[0] * c2[0] + c1[1] * c2[1] + c1[2] * c2[2];
            double a22 = c2[0] * c2[0] + c2[1] * c2[1] + c2[2] * c2[2];
            double b1 = c1[0] * rhs[0] + c1[1] * rhs[1] + c1[2] * rhs[2];
            double b2 = c2[0] * rhs[0] + c2[1] * rhs[1] + c2[2] * rhs[2];

            double det = a11 * a22 - a12 * a12;
            if (Math.Abs(det) < 1e-9) return false;

            p[j1] = (b1 * a22 - b2 * a12) / det;
            p[j2] = (a11 * b2 - a12 * b1) / det;
            p[k] = 0.0;

            double[] chk = MulRv(M, p);
            for (int i = 0; i < 3; i++)
                if (Math.Abs(chk[i] - rhs[i]) > 1e-4) return false;
            return true;
        }

        private static bool SolveFull(int[] M, double[] b, out double[] x)
        {
            x = new double[3];
            var A = new double[9];
            for (int i = 0; i < 9; i++) A[i] = M[i];
            double det = Det3d(A);
            if (Math.Abs(det) < 1e-9) return false;

            for (int col = 0; col < 3; col++)
            {
                var B = new double[9];
                for (int i = 0; i < 9; i++) B[i] = M[i];
                for (int row = 0; row < 3; row++) B[row * 3 + col] = b[row];
                x[col] = Det3d(B) / det;
            }
            return true;
        }

        private static int RotationSense(int[] R, int[] d, Matrix U)
        {
            Vector3 dc = Vector3.TransformNormal(new Vector3(d[0], d[1], d[2]), U);
            if (dc.LengthSquared() < 1e-12f) return 1;
            Vector3 dh = Vector3.Normalize(dc);

            int best = 0;
            float bestLen = -1f;
            for (int k = 0; k < 3; k++)
            {
                var e = new Vector3(k == 0 ? 1f : 0f, k == 1 ? 1f : 0f, k == 2 ? 1f : 0f);
                float len = Vector3.Cross(Vector3.TransformNormal(e, U), dh).Length();
                if (len > bestLen) { bestLen = len; best = k; }
            }

            var v = new Vector3(best == 0 ? 1f : 0f, best == 1 ? 1f : 0f, best == 2 ? 1f : 0f);
            var rv = new Vector3(R[0 * 3 + best], R[1 * 3 + best], R[2 * 3 + best]);

            Vector3 vc = Vector3.TransformNormal(v, U);
            Vector3 rc = Vector3.TransformNormal(rv, U);
            Vector3 vp = vc - Vector3.Dot(vc, dh) * dh;
            Vector3 rp = rc - Vector3.Dot(rc, dh) * dh;

            float s = Vector3.Dot(dh, Vector3.Cross(vp, rp));
            return s >= 0f ? 1 : -1;
        }

        private static string GlideLabel(double[] w)
        {
            var wc = new double[3];
            int nz = 0;
            bool allHalf = true, anyQuarter = false;
            int single = -1;

            for (int k = 0; k < 3; k++)
            {
                double x = w[k] - Math.Floor(w[k] + 1e-6);
                if (x > 1.0 - 1e-6) x = 0.0;
                wc[k] = x;
                if (x > 1e-6)
                {
                    nz++;
                    single = k;
                    if (Math.Abs(x - 0.5) > 1e-6) allHalf = false;
                    if (Math.Abs(x - 0.25) < 1e-6 || Math.Abs(x - 0.75) < 1e-6) anyQuarter = true;
                }
            }

            if (nz == 0) return "m";
            if (anyQuarter) return "d";
            if (allHalf) return nz == 1 ? (single == 0 ? "a" : (single == 1 ? "b" : "c")) : "n";
            return "g";
        }

        #endregion

        #region Colors — LIVE READ from Flags

 /// <summary>
 /// Returns current colour for elementu — odczytywany z Flags.Instance.
 /// Called przy each klatce w Draw(), so change w okienku
 /// Symmetry Elements jest visible immediately.
 /// </summary>
        private static XnaColor GetColorForElement(CellSymElementKind kind, int order, int sub)
        {
            var f = Flags.Instance;
            switch (kind)
            {
                case CellSymElementKind.RotationAxis:
                    return order switch
                    {
                        2 => new XnaColor(f.SymmetryAxis2ColorR, f.SymmetryAxis2ColorG, f.SymmetryAxis2ColorB),
                        3 => new XnaColor(f.SymmetryAxis3ColorR, f.SymmetryAxis3ColorG, f.SymmetryAxis3ColorB),
                        4 => new XnaColor(f.SymmetryAxis4ColorR, f.SymmetryAxis4ColorG, f.SymmetryAxis4ColorB),
                        6 => new XnaColor(f.SymmetryAxis6ColorR, f.SymmetryAxis6ColorG, f.SymmetryAxis6ColorB),
                        _ => XnaColor.White
                    };

                case CellSymElementKind.ScrewAxis:
                    return (order, sub) switch
                    {
                        (2, 1) => new XnaColor(f.SymmetryScrew21ColorR, f.SymmetryScrew21ColorG, f.SymmetryScrew21ColorB),
                        (3, 1) => new XnaColor(f.SymmetryScrew31ColorR, f.SymmetryScrew31ColorG, f.SymmetryScrew31ColorB),
                        (3, 2) => new XnaColor(f.SymmetryScrew32ColorR, f.SymmetryScrew32ColorG, f.SymmetryScrew32ColorB),
                        (4, 1) => new XnaColor(f.SymmetryScrew41ColorR, f.SymmetryScrew41ColorG, f.SymmetryScrew41ColorB),
                        (4, 2) => new XnaColor(f.SymmetryScrew42ColorR, f.SymmetryScrew42ColorG, f.SymmetryScrew42ColorB),
                        (4, 3) => new XnaColor(f.SymmetryScrew43ColorR, f.SymmetryScrew43ColorG, f.SymmetryScrew43ColorB),
                        (6, 1) => new XnaColor(f.SymmetryScrew61ColorR, f.SymmetryScrew61ColorG, f.SymmetryScrew61ColorB),
                        (6, 2) => new XnaColor(f.SymmetryScrew62ColorR, f.SymmetryScrew62ColorG, f.SymmetryScrew62ColorB),
                        (6, 3) => new XnaColor(f.SymmetryScrew63ColorR, f.SymmetryScrew63ColorG, f.SymmetryScrew63ColorB),
                        (6, 4) => new XnaColor(f.SymmetryScrew64ColorR, f.SymmetryScrew64ColorG, f.SymmetryScrew64ColorB),
                        (6, 5) => new XnaColor(f.SymmetryScrew65ColorR, f.SymmetryScrew65ColorG, f.SymmetryScrew65ColorB),
                        _ => new XnaColor(200, 200, 200)
                    };

                case CellSymElementKind.RotoinversionAxis:
                    return order switch
                    {
                        3 => new XnaColor(f.SymmetryRotoinv3ColorR, f.SymmetryRotoinv3ColorG, f.SymmetryRotoinv3ColorB),
                        4 => new XnaColor(f.SymmetryRotoinv4ColorR, f.SymmetryRotoinv4ColorG, f.SymmetryRotoinv4ColorB),
                        6 => new XnaColor(f.SymmetryRotoinv6ColorR, f.SymmetryRotoinv6ColorG, f.SymmetryRotoinv6ColorB),
                        _ => XnaColor.White
                    };

                case CellSymElementKind.MirrorPlane:
                    return new XnaColor(f.SymmetryMirrorColorR, f.SymmetryMirrorColorG, f.SymmetryMirrorColorB);

                case CellSymElementKind.GlidePlane:
                    return new XnaColor(f.SymmetryGlideColorR, f.SymmetryGlideColorG, f.SymmetryGlideColorB);

                case CellSymElementKind.InversionCenter:
                    return new XnaColor(f.SymmetryCenterColorR, f.SymmetryCenterColorG, f.SymmetryCenterColorB);

                default:
                    return XnaColor.White;
            }
        }

        #endregion

        #region Draw

        public void Draw(Draw draw)
        {
            if (_disposed || draw == null) return;

 // Backup rebuild — when someone called Invalidate()
            if (_needsRebuild && _lastMolecule != null)
            {
                BuildFromMolecule(_lastMolecule);
                _needsRebuild = false;
            }

            if (!Flags.Instance.ShowSymmetryElements) return;
            if (_elements.Count == 0) return;

            EnsureMarkerEffect(draw);
            Matrix scaleRotate = draw.GetScaleRotate();
            _sr = scaleRotate;

            foreach (var el in _elements)
            {
                if (!ShouldDraw(el)) continue;

 // ⭐ LIVE COLOR READ
                XnaColor liveColor = GetColorForElement(el.Kind, el.Order, el.ScrewSubscript);

                switch (el.Kind)
                {
                    case CellSymElementKind.RotationAxis:
                        DrawAxisArrow(draw, el, liveColor, scaleRotate, false);
                        break;
                    case CellSymElementKind.ScrewAxis:
                        DrawAxisArrow(draw, el, liveColor, scaleRotate, true);
                        break;
                    case CellSymElementKind.RotoinversionAxis:
                        DrawRotoinversionAxis(draw, el, liveColor, scaleRotate);
                        break;
                    case CellSymElementKind.MirrorPlane:
                        DrawPlane(draw, el, liveColor, scaleRotate, false);
                        break;
                    case CellSymElementKind.GlidePlane:
                        DrawPlane(draw, el, liveColor, scaleRotate, true);
                        break;
                    case CellSymElementKind.InversionCenter:
                        DrawInversionCenter(draw, el, liveColor, scaleRotate);
                        break;
                }
            }
        }

        private static bool ShouldDraw(CellSymElement el)
        {
            var f = Flags.Instance;
            switch (el.Kind)
            {
                case CellSymElementKind.RotationAxis:
                    if (!f.ShowRotationAxes) return false;
                    return el.Order switch
                    {
                        2 => f.ShowRotationAxis2,
                        3 => f.ShowRotationAxis3,
                        4 => f.ShowRotationAxis4,
                        6 => f.ShowRotationAxis6,
                        _ => true
                    };

                case CellSymElementKind.ScrewAxis:
                    if (!f.ShowScrewAxes) return false;
                    return (el.Order, el.ScrewSubscript) switch
                    {
                        (2, 1) => f.ShowScrew21,
                        (3, 1) => f.ShowScrew31,
                        (3, 2) => f.ShowScrew32,
                        (4, 1) => f.ShowScrew41,
                        (4, 2) => f.ShowScrew42,
                        (4, 3) => f.ShowScrew43,
                        (6, 1) => f.ShowScrew61,
                        (6, 2) => f.ShowScrew62,
                        (6, 3) => f.ShowScrew63,
                        (6, 4) => f.ShowScrew64,
                        (6, 5) => f.ShowScrew65,
                        _ => true
                    };

                case CellSymElementKind.RotoinversionAxis:
                    if (!f.ShowRotoinversionAxes) return false;
                    return el.Order switch
                    {
                        3 => f.ShowRotoinv3,
                        4 => f.ShowRotoinv4,
                        6 => f.ShowRotoinv6,
                        _ => true
                    };

                case CellSymElementKind.MirrorPlane: return f.ShowMirrorPlanes;
                case CellSymElementKind.GlidePlane: return f.ShowGlidePlanes;
                case CellSymElementKind.InversionCenter: return f.ShowInversionCenters;
                default: return false;
            }
        }

 // ── Axes: ARROW from face to tip (tip offset by 5×ARROW_LEN) ──

        private void DrawAxisArrow(Draw draw, CellSymElement el, XnaColor color,
                                   Matrix sr, bool dashed)
        {
            Vector3 dir = el.Direction;
            if (dir.LengthSquared() < 1e-9f) dir = Vector3.UnitZ;
            dir = Vector3.Normalize(dir);

            Vector3 start = el.SegmentStart;
            Vector3 end = el.SegmentEnd;

            float segLen = Vector3.Distance(start, end);
            if (segLen < 1e-4f) return;

            float arrowZone = Math.Min(ARROW_LEN * ARROW_GAP_FACTOR, segLen * 0.5f);
            Vector3 lineEnd = end - dir * arrowZone;

            SetColor(draw, color);

            if (dashed && el.Kind == CellSymElementKind.ScrewAxis)
            {
                float dash = GetScrewDashLength(el.Order, el.ScrewSubscript);
                DrawDashedRod(draw, start, lineEnd, sr, AxisThickness, dash, dash * 0.6f);
            }
            else
            {
                draw.DrawRodSegmentPublic(start, lineEnd, sr, AxisThickness);
            }

            DrawConeAt(draw, lineEnd, end, color, sr);

            if (el.Order != 2)
                DrawAxisMarker(draw, end - dir * (arrowZone + MarkerSize * 0.8f),
                    dir, el.Order, color, sr);
        }

        private static float GetScrewDashLength(int order, int sub)
        {
            if (sub == 1) return 0.06f;
            if (order == 6 && sub == 3) return 0.18f;
            return 0.12f;
        }

        private void DrawDashedRod(Draw draw, Vector3 a, Vector3 b, Matrix sr,
            float thickness, float dash, float gap)
        {
            float total = Vector3.Distance(a, b);
            if (total < 1e-5f) return;
            Vector3 dir = Vector3.Normalize(b - a);

            float t = 0f;
            while (t < total)
            {
                float seg = Math.Min(dash, total - t);
                if (seg > 1e-4f)
                    draw.DrawRodSegmentPublic(a + dir * t, a + dir * (t + seg), sr, thickness);
                t += dash + gap;
            }
        }

        private void DrawRotoinversionAxis(Draw draw, CellSymElement el, XnaColor color, Matrix sr)
        {
            SetColor(draw, color);

            if (el.HasSegment)
            {
                Vector3 dir = el.Direction;
                if (dir.LengthSquared() > 1e-9f) dir = Vector3.Normalize(dir);
                else dir = Vector3.UnitZ;

                Vector3 start = el.SegmentStart;
                Vector3 end = el.SegmentEnd;
                float segLen = Vector3.Distance(start, end);
                if (segLen > 1e-4f)
                {
                    float arrowZone = Math.Min(ARROW_LEN * ARROW_GAP_FACTOR, segLen * 0.5f);
                    Vector3 lineEnd = end - dir * arrowZone;

                    draw.DrawRodSegmentPublic(start, lineEnd, sr, AxisThickness);
                    DrawConeAt(draw, lineEnd, end, color, sr);

                    if (el.Order != 2)
                        DrawAxisMarker(draw, end - dir * (arrowZone + MarkerSize * 0.8f),
                            dir, el.Order, color, sr);
                }
            }

            if (el.HasCenter)
                DrawRing(draw, el.CenterPoint, el.Direction, MarkerSize * 0.55f, color);
        }

 // ── Centra inversion ──

        private void DrawInversionCenter(Draw draw, CellSymElement el, XnaColor color, Matrix sr)
        {
            draw.DrawSmallSphereAtPointPublic(el.Point, sr, color, InversionR);

            SetColor(draw, color);
            float r = InversionR * 1.6f;
            draw.DrawRodSegmentPublic(el.Point - Vector3.UnitX * r,
                el.Point + Vector3.UnitX * r, sr, AxisThickness * 0.6f);
            draw.DrawRodSegmentPublic(el.Point - Vector3.UnitY * r,
                el.Point + Vector3.UnitY * r, sr, AxisThickness * 0.6f);
            draw.DrawRodSegmentPublic(el.Point - Vector3.UnitZ * r,
                el.Point + Vector3.UnitZ * r, sr, AxisThickness * 0.6f);
        }

 // ── Planes ──

        private void DrawPlane(Draw draw, CellSymElement el, XnaColor color,
                               Matrix sr, bool glide)
        {
            var f = Flags.Instance;
            float alpha = glide ? f.SymmetryGlideAlpha : f.SymmetryMirrorAlpha;

            Vector4 fill = new Vector4(
                color.R / 255f, color.G / 255f, color.B / 255f, alpha);

            var poly = el.Polygon;

 // Fan triangulation of the fill
            for (int i = 1; i < poly.Length - 1; i++)
                draw.DrawPlaneQuadPublic(poly[0], poly[i], poly[i + 1], poly[0], fill, sr);

 // Edges as rods
            SetColor(draw, color);
            for (int i = 0; i < poly.Length; i++)
            {
                Vector3 a = poly[i];
                Vector3 b = poly[(i + 1) % poly.Length];
                draw.DrawRodSegmentPublic(a, b, sr, AxisThickness * 0.8f);
            }

            if (glide) DrawGlideArrow(draw, el, color, sr);
        }

        private void DrawGlideArrow(Draw draw, CellSymElement el, XnaColor color, Matrix sr)
        {
            Vector3 dir = el.Direction;
            if (dir.LengthSquared() < 1e-8f) return;
            dir = Vector3.Normalize(dir);

            Vector3 center = el.Point;
            Vector3 tail = center - dir * (GLIDE_ARROW_LEN * 0.5f);
            Vector3 tip = center + dir * (GLIDE_ARROW_LEN * 0.5f);
            Vector3 lineEnd = tip - dir * ARROW_LEN;

            SetColor(draw, color);
            draw.DrawRodSegmentPublic(tail, lineEnd, sr, AxisThickness * 0.7f);
            DrawConeAt(draw, lineEnd, tip, color, sr);
        }

        #endregion

        #region Markers

        private void DrawAxisMarker(Draw draw, Vector3 pos, Vector3 dir,
            int order, XnaColor color, Matrix sr)
        {
            if (order == 2) return;

            Vector3 up = Math.Abs(Vector3.Dot(dir, Vector3.UnitY)) < 0.9f
                ? Vector3.UnitY : Vector3.UnitX;
            Vector3 s1 = Vector3.Normalize(Vector3.Cross(dir, up));
            Vector3 s2 = Vector3.Normalize(Vector3.Cross(dir, s1));

            float rot = order == 4 ? MathHelper.PiOver4 : -MathHelper.PiOver2;
            DrawPolygon(draw, pos, s1, s2, order, MarkerSize, rot, ToVec(color));

            if (order == 6)
                DrawRing(draw, pos, dir, MarkerSize * 0.45f, color);
        }

 /// <summary>Cone (tip) from basePos to tipPos.</summary>
        private void DrawConeAt(Draw draw, Vector3 basePos, Vector3 tipPos,
            XnaColor color, Matrix sr)
        {
            Vector3 axis = tipPos - basePos;
            if (axis.LengthSquared() < 1e-9f) return;
            float len = axis.Length();
            Vector3 dir = axis / len;

            Vector3 up = Math.Abs(Vector3.Dot(dir, Vector3.UnitY)) < 0.9f
                ? Vector3.UnitY : Vector3.UnitX;
            Vector3 s1 = Vector3.Normalize(Vector3.Cross(dir, up));
            Vector3 s2 = Vector3.Normalize(Vector3.Cross(dir, s1));

            float rad = Math.Min(ARROW_RAD, len * 0.35f);

            const int seg = 12;
            float step = MathHelper.TwoPi / seg;
            EnsureBuffer(seg * 3);

            var xc = new XnaColor(color.R, color.G, color.B, (byte)255);
            int v = 0;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * step, a1 = (i + 1) * step;
                Vector3 p0 = basePos + (MathF.Cos(a0) * s1 + MathF.Sin(a0) * s2) * rad;
                Vector3 p1 = basePos + (MathF.Cos(a1) * s1 + MathF.Sin(a1) * s2) * rad;
                _markerBuffer[v++] = new VertexPositionColor(p0, xc);
                _markerBuffer[v++] = new VertexPositionColor(p1, xc);
                _markerBuffer[v++] = new VertexPositionColor(tipPos, xc);
            }
            FlushMarkers(draw, v);
        }

        private void DrawPolygon(Draw draw, Vector3 center,
            Vector3 s1, Vector3 s2, int sides, float radius,
            float angleOffset, Vector3 rgb)
        {
            if (sides < 3) return;
            float step = MathHelper.TwoPi / sides;
            EnsureBuffer(sides * 3);

            var xc = new XnaColor(rgb.X, rgb.Y, rgb.Z, 255);
            int v = 0;
            for (int i = 0; i < sides; i++)
            {
                float a0 = angleOffset + i * step;
                float a1 = angleOffset + ((i + 1) % sides) * step;
                Vector3 p0 = center + (MathF.Cos(a0) * s1 + MathF.Sin(a0) * s2) * radius;
                Vector3 p1 = center + (MathF.Cos(a1) * s1 + MathF.Sin(a1) * s2) * radius;
                _markerBuffer[v++] = new VertexPositionColor(center, xc);
                _markerBuffer[v++] = new VertexPositionColor(p0, xc);
                _markerBuffer[v++] = new VertexPositionColor(p1, xc);
            }
            FlushMarkers(draw, v);
        }

 /// <summary>Ring perpendicular to the axis.</summary>
        private void DrawRing(Draw draw, Vector3 center, Vector3 axis,
            float radius, XnaColor color)
        {
            Vector3 up = Math.Abs(Vector3.Dot(axis, Vector3.UnitY)) < 0.9f
                ? Vector3.UnitY : Vector3.UnitX;
            Vector3 s1 = Vector3.Normalize(Vector3.Cross(axis, up));
            Vector3 s2 = Vector3.Normalize(Vector3.Cross(axis, s1));

            const int seg = 24;
            float step = MathHelper.TwoPi / seg;
            float thick = radius * 0.25f;
            EnsureBuffer(seg * 6);

            var xc = new XnaColor(color.R, color.G, color.B, (byte)255);
            int v = 0;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * step, a1 = (i + 1) * step;
                Vector3 d0 = MathF.Cos(a0) * s1 + MathF.Sin(a0) * s2;
                Vector3 d1 = MathF.Cos(a1) * s1 + MathF.Sin(a1) * s2;

                _markerBuffer[v++] = new VertexPositionColor(center + d0 * radius, xc);
                _markerBuffer[v++] = new VertexPositionColor(center + d1 * radius, xc);
                _markerBuffer[v++] = new VertexPositionColor(center + d0 * (radius - thick), xc);

                _markerBuffer[v++] = new VertexPositionColor(center + d0 * (radius - thick), xc);
                _markerBuffer[v++] = new VertexPositionColor(center + d1 * radius, xc);
                _markerBuffer[v++] = new VertexPositionColor(center + d1 * (radius - thick), xc);
            }
            FlushMarkers(draw, v);
        }

        private void FlushMarkers(Draw draw, int count)
        {
            if (_markerEffect == null || _markerEffect.IsDisposed) return;
            if (_cachedDevice == null || _cachedDevice.IsDisposed) return;
            if (count <= 0) return;

            if (_markerBufferTx.Length < count)
                _markerBufferTx = new VertexPositionColor[count + 256];
            for (int i = 0; i < count; i++)
                _markerBufferTx[i] = new VertexPositionColor(
                    Vector3.Transform(_markerBuffer[i].Position, _sr),
                    _markerBuffer[i].Color);

            draw.DrawMarkersPublic(_cachedDevice, _markerEffect, _markerBufferTx, count);
        }

        private void EnsureBuffer(int needed)
        {
            if (_markerBuffer.Length < needed)
                _markerBuffer = new VertexPositionColor[needed + 256];
        }

        private void EnsureMarkerEffect(Draw draw)
        {
            var device = draw.GetGraphicsDeviceForRenderer();
            if (device == null || device.IsDisposed) return;

            if (_markerEffect == null || _markerEffect.IsDisposed || _cachedDevice != device)
            {
                _markerEffect?.Dispose();
                _markerEffect = new BasicEffect(device)
                {
                    VertexColorEnabled = true,
                    LightingEnabled = false,
                    TextureEnabled = false,
                    World = Matrix.Identity,
                    View = Matrix.Identity,
                    Projection = Matrix.Identity
                };
                _cachedDevice = device;
            }
        }

        private static void SetColor(Draw draw, XnaColor c)
        {
            draw.SetTemporaryColor(new Vector3(c.R / 255f, c.G / 255f, c.B / 255f));
        }

        private static Vector3 ToVec(XnaColor c)
            => new(c.R / 255f, c.G / 255f, c.B / 255f);

        #endregion
    }
}