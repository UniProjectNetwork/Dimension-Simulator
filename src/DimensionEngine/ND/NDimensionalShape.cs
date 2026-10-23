using DimensionEngine.Maths;

namespace DimensionEngine.ND;

/// <summary>
/// A point (vertex) in N-dimensional space with a per-step "depth" trail used to color
/// projections (e.g. the 4th coordinate W of a tesseract).
/// </summary>
public sealed class NdVertex
{
    public double[] C;          // current nD coordinates
    public double ExtraDepth;   // last-dropped coordinate magnitude (for coloring)
    public NdVertex(double[] c) { C = c; ExtraDepth = 0; }
}

/// <summary>An edge connecting two vertices of an nD shape (by index).</summary>
public readonly record struct NdEdge(int A, int B);

/// <summary>
/// Generic N-dimensional polytope: vertices + edges, with animated Givens rotations
/// applied in configurable coordinate planes, then successively perspective-projected
/// from N dimensions down to 2D (N → N-1 → ... → 3 → 2).
/// </summary>
public sealed class NDimensionalShape
{
    public readonly int N;
    private readonly NdVertex[] _vertices;
    private readonly NdEdge[] _edges;

    /// <summary>Rotation planes: each entry is (axisA, axisB, angularSpeed rad/s).</summary>
    public List<(int A, int B, double Speed)> RotationPlanes { get; } = new();

    /// <summary>Perspective distances used at each projection step.</summary>
    public double PerspectiveDistance { get; set; } = 2.2;

    /// <summary>Overall display scale (world units per shape radius).</summary>
    public double Scale { get; set; } = 1.0;

    public NDimensionalShape(double[][] vertices, int[][] edges, int n)
    {
        N = n;
        _vertices = new NdVertex[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            _vertices[i] = new NdVertex((double[])vertices[i].Clone());
        _edges = new NdEdge[edges.Length];
        for (int i = 0; i < edges.Length; i++)
            _edges[i] = new NdEdge(edges[i][0], edges[i][1]);
    }

    /// <summary>Advance all rotations by dt seconds (angles integrated per plane).</summary>
    public void Update(double dt)
    {
        foreach (var (a, b, speed) in RotationPlanes)
            RotatePlaneInPlace(a, b, speed * dt);
    }

    /// <summary>Apply a Givens rotation in the (axisA, axisB) plane to every vertex.</summary>
    public void RotatePlaneInPlace(int axisA, int axisB, double angle)
    {
        if (angle == 0 || axisA == axisB) return;
        double c = Math.Cos(angle), s = Math.Sin(angle);
        foreach (var v in _vertices)
        {
            double a = v.C[axisA], b = v.C[axisB];
            v.C[axisA] = a * c - b * s;
            v.C[axisB] = a * s + b * c;
        }
    }

    /// <summary>
    /// Successive perspective projection from N dims to 2 dims.
    /// Returns list of projected edges with a 0..1 "depth" value (higher = "closer"/smaller coordinate).
    /// </summary>
    public List<(Vec2 A, Vec2 B, double DepthA, double DepthB)> GetProjectedEdges()
    {
        // Work on a scratch copy; project from N down to 2.
        int dims = N;
        var pts = new double[_vertices.Length][];
        for (int i = 0; i < _vertices.Length; i++)
            pts[i] = (double[])_vertices[i].C.Clone();

        // depth color per vertex: magnitude of the coordinates dropped beyond 3D,
        // accumulated so lower-dimensional viewers still get color cues.
        var depth = new double[_vertices.Length];
        int dropFrom = N - 1;                   // start dropping the last axis

        while (dims > 3)
        {
            double d = PerspectiveDistance * ScaleFactor(dims);
            for (int i = 0; i < pts.Length; i++)
            {
                double w = pts[i][dropFrom];
                double denom = d - w;
                double k = denom > 1e-6 ? MathUtil.Clamp(d / Math.Max(denom, 1e-6), 0.5, 3.0) : 3.0;
                for (int j = 0; j < dims - 1; j++)
                    pts[i][j] *= k;
                depth[i] += w;
            }
            dims--;
            dropFrom--;
        }

        // Final 3D -> 2D: gentle perspective so shapes read as solid.
        double d3 = 4.2;
        for (int i = 0; i < pts.Length; i++)
        {
            double z = pts[i][2];
            double k = MathUtil.Clamp(d3 / Math.Max(d3 - z, 1e-6), 0.55, 2.6);
            pts[i][0] *= k;
            pts[i][1] *= k;
        }

        var result = new List<(Vec2, Vec2, double, double)>(_edges.Length);
        foreach (var e in _edges)
        {
            var a = new Vec2(pts[e.A][0] * Scale, pts[e.A][1] * Scale);
            var b = new Vec2(pts[e.B][0] * Scale, pts[e.B][1] * Scale);
            result.Add((a, b, NormalizedDepth(depth[e.A]), NormalizedDepth(depth[e.B])));
        }
        return result;
    }

    private static double ScaleFactor(int dims) => 1.0 + 0.35 * (dims - 4);

    private double NormalizedDepth(double raw)
    {
        // map accumulated dropped-coordinate sum into 0..1 for coloring
        double t = (raw / Math.Max(1, N - 3)) * 0.5 + 0.5;
        return MathUtil.Clamp(t, 0, 1);
    }

    public IReadOnlyList<NdVertex> Vertices => _vertices;
    public IReadOnlyList<NdEdge> Edges => _edges;

    // ---------- shape builders ----------

    /// <summary>n-dimensional hypercube: 2^n vertices at (±1)^n, edges between vertices of Hamming distance 1.</summary>
    public static NDimensionalShape Hypercube(int n)
    {
        int count = 1 << n;
        var verts = new double[count][];
        for (int i = 0; i < count; i++)
        {
            var c = new double[n];
            for (int d = 0; d < n; d++)
                c[d] = ((i >> d) & 1) == 1 ? 1.0 : -1.0;
            verts[i] = c;
        }

        var edgeList = new List<int[]>();
        for (int i = 0; i < count; i++)
        {
            for (int d = 0; d < n; d++)
            {
                int j = i ^ (1 << d);
                if (j > i) edgeList.Add(new[] { i, j });
            }
        }

        var shape = new NDimensionalShape(verts, edgeList.ToArray(), n);
        // classic look: rotate simultaneously in several planes for a mesmerizing motion
        shape.RotationPlanes.Add((0, 3, 0.55));               // XW
        if (n >= 5) shape.RotationPlanes.Add((1, 4, 0.42));   // YV
        if (n >= 6) shape.RotationPlanes.Add((2, 5, 0.33));   // ZU
        if (n >= 7) shape.RotationPlanes.Add((3, 6, 0.26));   // WT
        if (n >= 8) shape.RotationPlanes.Add((4, 7, 0.21));   // VS
        if (n >= 9) shape.RotationPlanes.Add((5, 8, 0.17));   // UR
        if (n >= 10) shape.RotationPlanes.Add((6, 9, 0.13));  // TQ
        shape.RotationPlanes.Add((0, 1, 0.30));               // XY
        if (n >= 4) shape.RotationPlanes.Add((2, 3, 0.24));   // ZW
        return shape;
    }

    /// <summary>n-simplex (regular simplex in n dims): n+1 vertices, all mutually connected.</summary>
    public static NDimensionalShape Simplex(int n)
    {
        int count = n + 1;
        var verts = new double[count][];
        // Standard regular-simplex coordinates.
        for (int i = 0; i < count; i++)
        {
            var c = new double[n];
            for (int j = 0; j < n; j++)
            {
                if (i == n)
                    c[j] = -Math.Sqrt((j + 1.0) / (2.0 * (j + 2.0)));
                else
                    c[j] = (i == j) ? Math.Sqrt((j + 1.0) / (2.0 * (j + 2.0))) : 0;
            }
            verts[i] = c;
        }
        var edges = new List<int[]>();
        for (int i = 0; i < count; i++)
            for (int j = i + 1; j < count; j++)
                edges.Add(new[] { i, j });
        var shape = new NDimensionalShape(verts, edges.ToArray(), n);
        shape.RotationPlanes.Add((0, 2, 0.5));
        if (n >= 4) shape.RotationPlanes.Add((1, 3, 0.38));
        if (n >= 6) shape.RotationPlanes.Add((4, 5, 0.27));
        return shape;
    }

    /// <summary>n-orthoplex (cross-polytope): 2n vertices at ±e_i; all edges except opposite poles.</summary>
    public static NDimensionalShape Orthoplex(int n)
    {
        int count = 2 * n;
        var verts = new double[count][];
        for (int i = 0; i < count; i++)
        {
            var c = new double[n];
            c[i / 2] = (i % 2 == 0) ? 1.0 : -1.0;
            verts[i] = c;
        }
        var edges = new List<int[]>();
        for (int i = 0; i < count; i++)
            for (int j = i + 1; j < count; j++)
                if ((i ^ 1) != j)
                    edges.Add(new[] { i, j });
        var shape = new NDimensionalShape(verts, edges.ToArray(), n);
        shape.RotationPlanes.Add((0, 2, 0.55));
        if (n >= 4) shape.RotationPlanes.Add((1, 3, 0.4));
        if (n >= 5) shape.RotationPlanes.Add((2, 4, 0.3));
        return shape;
    }

    /// <summary>
    /// Glome (3-sphere, 4D hypersphere) surface as a point cloud in 4D.
    /// S³: (sinψ·cosθ, sinψ·sinθ, cosψ·cosφ, cosψ·sinφ).
    /// </summary>
    public static double[][] GlomePoints(int meridians = 22, int parallels = 14, int phiSteps = 14)
    {
        var pts = new List<double[]>();
        for (int i = 0; i <= parallels; i++)
        {
            double psi = Math.PI * i / parallels;         // hyperspherical latitude 0..π
            double sinPsi = Math.Sin(psi), cosPsi = Math.Cos(psi);
            for (int j = 0; j < meridians; j++)
            {
                double theta = 2 * Math.PI * j / meridians;
                for (int k = 0; k < phiSteps; k++)
                {
                    double phi = 2 * Math.PI * k / phiSteps;
                    pts.Add(new[]
                    {
                        sinPsi * Math.Cos(theta),
                        sinPsi * Math.Sin(theta),
                        cosPsi * Math.Cos(phi),
                        cosPsi * Math.Sin(phi)
                    });
                }
            }
        }
        return pts.ToArray();
    }

    /// <summary>Klein bottle parametrization (embedded in 4D, immersed in 3D).</summary>
    public static double[][] KleinBottlePoints(int uSteps = 48, int vSteps = 24, double R = 2.0, double r = 1.0)
    {
        var pts = new List<double[]>();
        for (int i = 0; i < uSteps; i++)
        {
            double u = 2 * Math.PI * i / uSteps;
            for (int j = 0; j < vSteps; j++)
            {
                double v = 2 * Math.PI * j / vSteps;
                double cu = Math.Cos(u), su = Math.Sin(u);
                double cv = Math.Cos(v), sv = Math.Sin(v);
                double x = (R + r * cv) * cu;
                double y = (R + r * cv) * su;
                double z = r * sv;
                double w = r * cv; // 4th coordinate for coloring
                pts.Add(new[] { x, y, z, w });
            }
        }
        return pts.ToArray();
    }
}
