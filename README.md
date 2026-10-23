# Dimension Simulator

A 3D universe and higher-dimension simulator for Windows / macOS / Linux, built with
**C# /.NET 8 + AvaloniaUI** and a fully custom-built rendering library (SkiaSharp raster
backend — no external 3D engine, no image assets: every texture, starfield, nebula and
effect is generated procedurally at runtime).

## Build & run

Requires the .NET 8 SDK.

```bash
cd DimensionSimulator
dotnet run --project src/DimensionSimulator
```

or build a release binary: `dotnet build -c Release`
(output in `src/DimensionSimulator/bin/Release/net8.0/`).

Headless render verification (no display needed): `dotnet run --project tools/RenderTest`
writes PNG proof frames to `download/previews`.

Dimension-button regression check (needs a virtual display, e.g. `xvfb-run`):
`dotnet run --project src/DimensionSimulator -- --verify-buttons` raises real Click
events on every 3D–10D warp button and 4D–10D hypercube button and asserts the engine
lands in the matching dimension (exit code 0 = all pass).

## Controls

| Input | Action |
|---|---|
| Left-drag | Orbit camera |
| Wheel / +/- | Zoom (huge dynamic range) |
| Right-drag | Pan |
| Click | Select object |
| Double-click | Fly to object |
| Space | Pause time |
| O | Toggle orbit paths |
| Arrow keys | Orbit camera |

Bottom bar: pause, log-scaled time speed (2 min/s … 1 Myr/s), clock reset, orbit paths,
realistic-scale toggle. Top-right: PNG screenshot of the current frame.

## Views

1. **Solar System** — Sun, 8 planets, Pluto & Ceres, 14 moons, Halley's Comet,
   asteroid + Kuiper belts, all on true Keplerian orbits (Newton-solved eccentric
   anomaly, real elements). Procedural per-world textures, per-pixel sphere shading,
   Saturn's rings with Cassini division, atmosphere rim scattering.
2. **Deep Space** — ~40 real objects with full scientific data panels: 7 black holes
   (TON 618, Sagittarius A*, M87*, Phoenix A, Cygnus X-1, Gaia BH1, GW150914) with
   photon rings, Doppler-beamed accretion disks and lensing arcs; 16 stars incl.
   Betelgeuse, UY Scuti, Stephenson 2-18; pulsars; 7 nebulae; 8 galaxies; exoplanets.
   Relativity readouts (photon sphere, ISCO, time dilation) per black hole.
3. **Higher Dimensions** — N-dimensional polytope explorer: hypercubes 4D–10D
   (2^n vertices, up to the 5,120-edge dekeract), simplices, orthoplexes, glome and
   Klein bottle point clouds, rotated through n-D planes and successively projected
   to your screen, with rotation-speed and projection-distance controls.

## Dimension Warp — the Solar System & Deep Space in 4D–10D

The **DIMENSION WARP** panel (top-left of the Solar System *and* Deep Space views)
throws the whole view into a chosen higher dimension — one shared warp state, so the
dimension follows you when you switch views. Each dimension has its own mathematics,
visual signature and science note:

| Dim | View | What you see |
|-----|------|--------------|
| 3D | Normal space | Flat Keplerian Solar System |
| 4D | **Hyperspace** | Scene rotating through the XW/YW planes: bodies swell/shrink as they slide through our 3D slice, orbit rings twist into 4D curves, a real tesseract (16 vertices / 32 edges) frames the Sun, spacetime sheet ripples |
| 5D | **Kaluza-Klein** | Probability echoes drift beside every planet, orbits split into superposed bands, two wave sources interfere on the sheet |
| 6D | **Phase space** | World-line trails behind every body, deep general-relativity embedding funnel with frame-dragging swirl around the Sun |
| 7D | **Compactification** | Rotating Calabi-Yau style knots with chromatic fringing around every world, folded space creases, harmonic vibration |
| 8D | **E8 lattice** | The real 240-root E8 projection as a golden mandala, mirrored body echoes through the origin, quantized lattice sheet |
| 9D | **Spacetime fold** | Space folds like fabric, orbits wander onto Lissajous paths, string halos form around bodies |
| 10D | **Superstrings** | Every world dissolves into nested vibrating string resonances above dark cores, giant background strings sweep the frame |

In **Deep Space** the same warp displaces every star, black hole, nebula, galaxy and
exoplanet system (each system travels as one coherent w-band), warps exoplanet orbit
rings, and adds deep-space treatments: probability ghosts tinted per object class,
world-line trails, a tesseract around the selection, and 10D dissolves where stars
collapse to string resonances and black holes keep their event-horizon identity inside
string halos.

Mechanics: bodies are embedded in 4D (x, y, z, w), rotated with Givens rotations in
extra-dimensional planes, perspective-projected 4D→3D→screen; orbit rings are sampled
through the *same* transform so planets always sit exactly on their warped rings.
Switching dimensions plays an interdimensional flash and eases the warp in over ~2 s;
the active dimension's science explanation is shown in the left panel and a HUD badge.

## Project layout

```
DimensionSimulator.sln
src/DimensionEngine/           custom simulation + rendering library
  Maths/    Vec2/3/4, Mat4, NdVector
  Render/   RenderContext, SphereShader, PlanetTextureFactory, Effects, DimensionFX
  ND/       NDimensionalShape, DimensionWarp (warp math + per-dimension profiles)
  Sim/      KeplerOrbit, SimClock
  Camera/   OrbitCamera
  Universe/ SpaceObjects, catalogs (solar system, deep space), DimensionData
  Scenes/   SolarSystemScene (incl. warp render path), DeepSpaceScene (incl. warp render path), DimensionsScene
src/DimensionSimulator/        AvaloniaUI desktop app (MainWindow, UniverseCanvas)
tools/RenderTest/              headless PNG verification + benchmark harness
```
