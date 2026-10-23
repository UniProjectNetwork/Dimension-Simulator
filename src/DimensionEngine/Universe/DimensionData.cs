namespace DimensionEngine.Universe;

/// <summary>
/// Higher-dimension content: names, math stats and real physics context for
/// dimensions 0 through 10, plus the higher-dimensional shape catalog.
/// </summary>
public static class DimensionData
{
    public sealed record DimInfo(
        int D,
        string Name,
        string ShapeName,
        string MathNote,
        string PhysicsNote);

    public static readonly DimInfo[] Info =
    {
        new(0, "Zero Dimensions", "Point",
            "A single location: no length, width or depth. The product of zero axes.",
            "In cosmology, the entire universe may have begun as a singularity — a 0D point of infinite density at t = 0, the Big Bang."),
        new(1, "One Dimension", "Line (Monogon)",
            "A single axis. A 1D being knows only 'forward/backward' — two points define its whole world.",
            "Cosmic strings — 1D topological defects possibly woven during the Big Bang — could be thinner than a proton yet light-years long."),
        new(2, "Two Dimensions", "Square (Plane)",
            "Two perpendicular axes. Shapes here have area but zero volume; a 2D flatland dweller cannot step 'over' anything.",
            "Physicists study 2D systems seriously: graphene and the quantum Hall effect are effectively 2D universes with new states of matter."),
        new(3, "Three Dimensions", "Cube (Hexahedron)",
            "Our familiar space: length, width, height. 8 vertices, 12 edges, 6 faces.",
            "General relativity curves this 3-space with gravity; the cosmic web of galaxies is its largest-scale structure."),
        new(4, "Four Dimensions", "Tesseract (Hypercube)",
            "Add a W axis perpendicular to X, Y, Z: 16 vertices, 32 edges, 24 faces, 8 cubic cells. Rotating a tesseract in the XW plane is the classic 'cube within a cube'.",
            "Physics already uses 4D: Minkowski spacetime (3 space + 1 time). Relativity treats time as a dimension flowing past light cones."),
        new(5, "Five Dimensions", "Penteract",
            "32 vertices, 80 edges. Projections grow intricate as each new axis doubles the structure.",
            "Kaluza–Klein theory (1921–26) unified gravity and electromagnetism by adding a curled-up 5th dimension — an idea that blossomed into modern string theory."),
        new(6, "Six Dimensions", "Hexeract",
            "64 vertices, 192 edges. Six mutually perpendicular axes — impossible to visualize, easy to compute.",
            "Supergravity lives naturally in 6D; Calabi–Yau 3-fold spaces (6 real dimensions) are the leading candidates for how string theory's extra dimensions are curled up."),
        new(7, "Seven Dimensions", "Hepteract",
            "128 vertices, 448 edges.",
            "M-theory compactified on 7-dimensional G2 manifolds yields our 4D world with its exact particle families."),
        new(8, "Eight Dimensions", "Octeract",
            "256 vertices, 1,024 edges — the wireframe alone is mesmerizing.",
            "The E8 symmetry group — a 248-dimensional object mapping 8D space — underlies heterotic string theory and the 2024 'octonion' unification attempts."),
        new(9, "Nine Dimensions", "Enneract",
            "512 vertices, 2,304 edges. Only its successive shadow-projections reach your screen.",
            "Superstring theory requires 9 spatial dimensions: 6 of them compactified into Calabi–Yau shapes whose vibrations set particle properties."),
        new(10, "Ten Dimensions", "Dekeract",
            "1,024 vertices, 5,120 edges — 2^10 corners folding through 10 orthogonal axes.",
            "Superstring theory's total: 9 space + 1 time = 10D. All matter and forces as vibrating strings; branes, extra dimensions and the multiverse follow."),
    };

    /// <summary>Extra 4D+ shapes beyond the hypercube family.</summary>
    public static readonly (string Id, string Name, string Note)[] ExtraShapes =
    {
        ("simplex4", "5-Cell (4-Simplex)", "The 4D analog of the tetrahedron: 5 vertices, 10 edges, 10 triangular faces, 5 tetrahedral cells — the simplest possible 4D shape."),
        ("orthoplex4", "16-Cell (4-Orthoplex)", "The 4D analog of the octahedron: 8 vertices at ±X ±Y ±Z ±W, 24 edges. Dual to the tesseract."),
        ("glome", "Glome (3-Sphere)", "The 4D hypersphere — every point equidistant from the center in 4D. Shown as a rotating point-cloud of its hypersurface."),
        ("klein", "Klein Bottle", "A closed surface with no inside or outside that only fully exists in 4D — here shown with its 4th coordinate as color."),
    };

    public static string HypercubeFact(int n) => n switch
    {
        4 => "Salvador Dalí painted a tesseract as 'Corpus Hypercubus' (1954).",
        5 => "Each added dimension doubles the vertex count: 2^n.",
        8 => "An octeract has more edges (1,024) than the Milky Way has … no, wait — but that's a lot of edges.",
        10 => "The dekeract's 1,024 vertices would take over 28 minutes to visit at one per second.",
        _ => "m-faces of an n-cube: C(n,m) × 2^(n−m)."
    };
}
