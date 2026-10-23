namespace DimensionEngine.Maths;

/// <summary>2D vector (screen / 2D space).</summary>
public readonly struct Vec2(double x, double y)
{
    public readonly double X = x, Y = y;
    public static Vec2 Zero => new(0, 0);
    public double Length => Math.Sqrt(X * X + Y * Y);
    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator -(Vec2 a) => new(-a.X, -a.Y);
    public static Vec2 operator *(Vec2 v, double s) => new(v.X * s, v.Y * s);
    public static Vec2 operator *(double s, Vec2 v) => new(v.X * s, v.Y * s);
}

/// <summary>3D vector (world space).</summary>
public readonly struct Vec3(double x, double y, double z)
{
    public readonly double X = x, Y = y, Z = z;
    public static Vec3 Zero => new(0, 0, 0);
    public static Vec3 UnitX => new(1, 0, 0);
    public static Vec3 UnitY => new(0, 1, 0);
    public static Vec3 UnitZ => new(0, 0, 1);

    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
    public double LengthSquared => X * X + Y * Y + Z * Z;

    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vec3 operator -(Vec3 a) => new(-a.X, -a.Y, -a.Z);
    public static Vec3 operator *(Vec3 a, double s) => new(a.X * s, a.Y * s, a.Z * s);
    public static Vec3 operator *(double s, Vec3 a) => new(a.X * s, a.Y * s, a.Z * s);
    public static Vec3 operator /(Vec3 a, double s) => new(a.X / s, a.Y / s, a.Z / s);

    public static double Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    public static Vec3 Cross(Vec3 a, Vec3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    public Vec3 Normalized()
    {
        double len = Length;
        return len < 1e-12 ? Zero : this / len;
    }

    public Vec3 Lerp(Vec3 o, double t) => new(
        X + (o.X - X) * t,
        Y + (o.Y - Y) * t,
        Z + (o.Z - Z) * t);

    public double DistanceTo(Vec3 o) => (this - o).Length;

    /// <summary>Rotate around an arbitrary axis (Rodrigues formula). Axis must be normalized.</summary>
    public Vec3 RotateAround(Vec3 axis, double angleRad)
    {
        double c = Math.Cos(angleRad), s = Math.Sin(angleRad);
        return this * c + Vec3.Cross(axis, this) * s + Vec3.Cross(axis, Vec3.Cross(axis, this)) * (1 - c);
    }
}

/// <summary>4D vector — used for 4-space projections of higher-dimensional objects.</summary>
public readonly struct Vec4(double x, double y, double z, double w)
{
    public readonly double X = x, Y = y, Z = z, W = w;
    public static Vec4 operator +(Vec4 a, Vec4 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
    public static Vec4 operator -(Vec4 a, Vec4 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
    public static Vec4 operator *(Vec4 a, double s) => new(a.X * s, a.Y * s, a.Z * s, a.W * s);
    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
    public Vec3 XYZ => new(X, Y, Z);
}

/// <summary>Arbitrary-N dimensional vector.</summary>
public sealed class NdVector
{
    private readonly double[] _c;
    public int N => _c.Length;
    public double this[int i] => _c[i];

    public NdVector(double[] components) { _c = (double[])components.Clone(); }
    public NdVector(int n) { _c = new double[n]; }

    public static NdVector Basis(int n, int axis, double value = 1.0)
    {
        var v = new NdVector(n);
        v.SetComponent(axis, value);
        return v;
    }

    public void SetComponent(int i, double v) => _c[i] = v;

    public double Length
    {
        get
        {
            double s = 0;
            for (int i = 0; i < _c.Length; i++) s += _c[i] * _c[i];
            return Math.Sqrt(s);
        }
    }

    public NdVector Scaled(double s)
    {
        var r = new NdVector(N);
        for (int i = 0; i < N; i++) r.SetComponent(i, _c[i] * s);
        return r;
    }

    public static NdVector Add(NdVector a, NdVector b)
    {
        var r = new NdVector(a.N);
        for (int i = 0; i < a.N; i++) r.SetComponent(i, a[i] + b[i]);
        return r;
    }

    public double[] Copy() => (double[])_c.Clone();
}
