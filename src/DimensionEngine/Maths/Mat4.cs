namespace DimensionEngine.Maths;

/// <summary>
/// 4x4 matrix for 3D camera / projection math (row-major storage).
/// Convention: v' = v.TransformBy(m), column-vector style.
/// </summary>
public struct Mat4
{
    public double M11, M12, M13, M14;
    public double M21, M22, M23, M24;
    public double M31, M32, M33, M34;
    public double M41, M42, M43, M44;

    public static Mat4 Identity => new()
    {
        M11 = 1, M22 = 1, M33 = 1, M44 = 1
    };

    public static Mat4 Multiply(in Mat4 a, in Mat4 b)
    {
        return new Mat4
        {
            M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
            M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
            M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
            M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,

            M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
            M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
            M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
            M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,

            M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
            M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
            M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
            M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,

            M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
            M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
            M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
            M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44,
        };
    }

    /// <summary>
    /// View matrix from camera position and basis vectors (all normalized).
    /// Convention: camera looks down +Z (zView positive into screen), Y up.
    /// </summary>
    public static Mat4 LookAt(Vec3 eye, Vec3 forward, Vec3 up)
    {
        var f = forward.Normalized();
        var r = Vec3.Cross(f, up).Normalized();
        var u = Vec3.Cross(r, f);

        return new Mat4
        {
            M11 = r.X, M12 = r.Y, M13 = r.Z, M14 = -Vec3.Dot(r, eye),
            M21 = u.X, M22 = u.Y, M23 = u.Z, M24 = -Vec3.Dot(u, eye),
            M31 = f.X, M32 = f.Y, M33 = f.Z, M34 = -Vec3.Dot(f, eye),
            M44 = 1
        };
    }

    /// <summary>
    /// Perspective projection for +Z-into-screen view space.
    /// clip.x = (f/aspect)·x, clip.y = f·y, clip.w = z — TransformPoint's divide
    /// by w yields the correct NDC x/y.
    /// </summary>
    public static Mat4 Perspective(double fovYRad, double aspect, double zNear, double zFar)
    {
        double f = 1.0 / Math.Tan(fovYRad / 2.0);
        return new Mat4
        {
            M11 = f / aspect,
            M22 = f,
            M43 = 1
        };
    }

    /// <summary>Rotate around X axis.</summary>
    public static Mat4 RotationX(double a)
    {
        double c = Math.Cos(a), s = Math.Sin(a);
        return new Mat4 { M11 = 1, M22 = c, M23 = s, M32 = -s, M33 = c, M44 = 1 };
    }

    /// <summary>Rotate around Y axis.</summary>
    public static Mat4 RotationY(double a)
    {
        double c = Math.Cos(a), s = Math.Sin(a);
        return new Mat4 { M11 = c, M13 = -s, M31 = s, M22 = 1, M33 = c, M44 = 1 };
    }

    /// <summary>Rotate around Z axis.</summary>
    public static Mat4 RotationZ(double a)
    {
        double c = Math.Cos(a), s = Math.Sin(a);
        return new Mat4 { M11 = c, M12 = s, M21 = -s, M22 = c, M33 = 1, M44 = 1 };
    }

    public static Mat4 Translation(Vec3 t) => new()
    {
        M11 = 1, M22 = 1, M33 = 1, M44 = 1,
        M41 = t.X, M42 = t.Y, M43 = t.Z
    };

    public static Mat4 Scale(double s) => new()
    {
        M11 = s, M22 = s, M33 = s, M44 = 1
    };

    public static Mat4 Compose(params Mat4[] mats)
    {
        var r = Identity;
        foreach (var m in mats) r = Multiply(r, m);
        return r;
    }

    /// <summary>Transform a point (w=1).</summary>
    public readonly Vec3 TransformPoint(Vec3 p)
    {
        double x = p.X, y = p.Y, z = p.Z;
        double tx = M11 * x + M12 * y + M13 * z + M14;
        double ty = M21 * x + M22 * y + M23 * z + M24;
        double tz = M31 * x + M32 * y + M33 * z + M34;
        double tw = M41 * x + M42 * y + M43 * z + M44;
        return Math.Abs(tw) < 1e-12 ? new Vec3(tx, ty, tz) : new Vec3(tx / tw, ty / tw, tz / tw);
    }

    /// <summary>Transform a direction (w=0, no translation).</summary>
    public readonly Vec3 TransformDir(Vec3 v)
    {
        double x = v.X, y = v.Y, z = v.Z;
        return new Vec3(
            M11 * x + M12 * y + M13 * z,
            M21 * x + M22 * y + M23 * z,
            M31 * x + M32 * y + M33 * z);
    }

    /// <summary>Transform a 4D vector (for projecting from 4-space).</summary>
    public readonly Vec4 TransformVec4(Vec4 v)
    {
        return new Vec4(
            M11 * v.X + M12 * v.Y + M13 * v.Z + M14 * v.W,
            M21 * v.X + M22 * v.Y + M23 * v.Z + M24 * v.W,
            M31 * v.X + M32 * v.Y + M33 * v.Z + M34 * v.W,
            M41 * v.X + M42 * v.Y + M43 * v.Z + M44 * v.W);
    }
}

public static class MathUtil
{
    public const double DegToRad = Math.PI / 180.0;
    public const double RadToDeg = 180.0 / Math.PI;

    public static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
    public static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);

    public static double Lerp(double a, double b, double t) => a + (b - a) * t;

    public static double SmoothStep(double a, double b, double t)
    {
        t = Clamp((t - a) / (b - a), 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>Frame-rate independent exponential smoothing factor.</summary>
    public static double DampFactor(double halfLife, double dt)
    {
        if (halfLife <= 0) return 1;
        return 1.0 - Math.Pow(0.5, dt / halfLife);
    }
}
