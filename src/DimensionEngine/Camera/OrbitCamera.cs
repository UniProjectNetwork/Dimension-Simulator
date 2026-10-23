using DimensionEngine.Maths;

namespace DimensionEngine.Camera;

/// <summary>
/// Orbit camera with zoom (exponential, huge dynamic range), pan, rotation and
/// smooth animated fly-to targeting. Works in engine world units.
/// </summary>
public sealed class OrbitCamera
{
    private Vec3 _target = Vec3.Zero;
    private double _distance = 400;
    private double _yaw = 35 * MathUtil.DegToRad;    // around Y (up)
    private double _pitch = 22 * MathUtil.DegToRad;  // above horizon
    private double _fov = 55 * MathUtil.DegToRad;

    // animation state
    private bool _animating;
    private Vec3 _fromTarget, _toTarget;
    private double _fromDist, _toDist;
    private double _animT, _animDuration = 1.2;

    public double MinDistance { get; set; } = 0.02;
    public double MaxDistance { get; set; } = 200_000;
    public Vec3 Target => _target;
    public double Distance => _distance;
    public double Pitch => _pitch;
    public double Yaw => _yaw;
    public double ProjFovY => _fov;

    public OrbitCamera()
    {
    }

    public void SetImmediate(Vec3 target, double distance, double yawDeg = double.NaN, double pitchDeg = double.NaN)
    {
        _target = target;
        _distance = MathUtil.Clamp(distance, MinDistance, MaxDistance);
        if (!double.IsNaN(yawDeg)) _yaw = yawDeg * MathUtil.DegToRad;
        if (!double.IsNaN(pitchDeg)) _pitch = MathUtil.Clamp(pitchDeg * MathUtil.DegToRad, -1.45, 1.45);
        _animating = false;
    }

    public void FlyTo(Vec3 target, double distance, double? durationSec = null)
    {
        _fromTarget = _target;
        _toTarget = target;
        _fromDist = _distance;
        _toDist = MathUtil.Clamp(distance, MinDistance, MaxDistance);
        _animT = 0;
        _animDuration = durationSec ?? MathUtil.Clamp(0.6 + 0.25 * Math.Log10(1 + Math.Abs(_fromDist - _toDist)), 0.8, 2.2);
        _animating = true;
    }

    public void Tick(double dt)
    {
        if (!_animating) return;
        _animT += dt / _animDuration;
        if (_animT >= 1)
        {
            _animT = 1;
            _animating = false;
        }
        double t = _animT;
        // smoothstep easing
        t = t * t * (3 - 2 * t);
        _target = _fromTarget.Lerp(_toTarget, t);
        _distance = MathUtil.Lerp(_fromDist, _toDist, t) * (1 - t) + _toDist * t;
        // log-space blend for distance feels better across magnitudes:
        _distance = Math.Exp(MathUtil.Lerp(Math.Log(Math.Max(_fromDist, 1e-6)), Math.Log(Math.Max(_toDist, 1e-6)), t));
    }

    public void Orbit(double dxPixels, double dyPixels)
    {
        _yaw -= dxPixels * 0.005;
        _pitch = MathUtil.Clamp(_pitch + dyPixels * 0.005, -1.45, 1.45);
        _animating = false;
    }

    /// <summary>Zoom by multiplicative factor (e.g. 1.2 in, 1/1.2 out).</summary>
    public void ZoomBy(double factor)
    {
        _animating = false;
        _distance = MathUtil.Clamp(_distance / factor, MinDistance, MaxDistance);
    }

    public void Pan(double dxPixels, double dyPixels, double viewportHeight)
    {
        _animating = false;
        double worldPerPix = 2.0 * _distance * Math.Tan(_fov / 2) / Math.Max(viewportHeight, 1);
        var f = Forward();
        var right = Vec3.Cross(f, Up()).Normalized();
        var up = Vec3.Cross(right, f).Normalized();
        _target -= right * (-dxPixels * worldPerPix) + up * (dyPixels * worldPerPix);
    }

    public Vec3 Forward()
    {
        double cp = Math.Cos(_pitch);
        return new Vec3(Math.Sin(_yaw) * cp, Math.Sin(_pitch), Math.Cos(_yaw) * cp);
    }

    public Vec3 Eye => _target - Forward() * _distance;
    public Vec3 Up() => Vec3.UnitY;

    public Mat4 ViewMatrix() => Mat4.LookAt(Eye, Forward(), Up());

    public Mat4 ProjMatrix(double aspect, double near, double far)
        => Mat4.Perspective(_fov, aspect, near, far);

    /// <summary>Screen projection: world point → (x_px, y_px, depth) with depth = view-space Z (positive in front).</summary>
    public (double x, double y, double zView, bool inFront) Project(Vec3 world, Mat4 view, Mat4 proj, int width, int height)
    {
        var v = view.TransformPoint(world);
        var c = proj.TransformPoint(v);
        bool front = v.Z > nearEps;
        return (width / 2.0 + c.X * width / 2.0, height / 2.0 - c.Y * height / 2.0, v.Z, front);
    }

    private const double nearEps = 1e-9;

    /// <summary>Estimate on-screen pixel radius for a world-space sphere radius at given depth.</summary>
    public double PixelRadius(double worldRadius, double zView, int height)
    {
        if (zView <= nearEps) return 0;
        return worldRadius / zView * (height / 2.0) / Math.Tan(_fov / 2);
    }
}
