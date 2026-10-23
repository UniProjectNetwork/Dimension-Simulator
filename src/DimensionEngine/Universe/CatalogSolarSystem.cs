using DimensionEngine.Maths;
using DimensionEngine.Render;
using DimensionEngine.Sim;
using SkiaSharp;

namespace DimensionEngine.Universe;

/// <summary>
/// Solar System catalog: the Sun, all 8 planets (+ Pluto, Ceres), major moons,
/// asteroid & Kuiper belts and Halley's Comet — with real orbital elements
/// (a, e, i, period, phase) and physical data.
/// </summary>
public static class CatalogSolarSystem
{
    public const double AU = 240.0;              // scene units per AU

    public static List<SpaceObject> Build()
    {
        var list = new List<SpaceObject>();

        // ---- SUN ----
        var sun = new StarObject
        {
            Id = "sol", Name = "Sun", Category = "Star",
            EmTag = "G2V · Yellow dwarf",
            VisualRadius = 16,
            StarColor = new SKColor(255, 236, 170),
            IsSun = true,
            Info = new CelestialInfo
            {
                Type = "G2V main-sequence star (yellow dwarf)",
                MassText = "1.989 × 10^30 kg (333,000 Earths)",
                RadiusText = "696,340 km (109 × Earth)",
                TempText = "5,778 K surface · 15.7 million K core",
                DistanceText = "1 AU from Earth (8 min 20 s light)",
                SpectralText = "G2V",
                LuminosityText = "3.828 × 10^26 W",
                AgeText = "≈ 4.6 billion years",
                CompositionText = "≈73% hydrogen, 25% helium, 2% heavier elements",
                GravityText = "274 m/s² (27.9 g)",
                DayText = "≈25 days (equator) · 35 days (poles)",
                Facts = new[]
                {
                    "Contains 99.86% of all the mass in the Solar System.",
                    "Fuses ≈600 million tonnes of hydrogen into helium every second.",
                    "The corona is mysteriously hotter than the surface — over 1,000,000 K.",
                    "In ~5 billion years it will swell into a red giant, likely engulfing Earth."
                }
            }
        };
        list.Add(sun);

        // ---- PLANETS ----
        list.Add(Planet("mercury", "Mercury", "Planet", "Smallest planet · scorched world",
            BodyTextureKind.Mercury, SKColors.Transparent, 1.5,
            orbit: new KeplerOrbit(a: 0.387 * AU, e: 0.2056, inclinationDeg: 7.0, periodDays: 87.97, meanAnomaly0Deg: 174),
            spinDays: 58.65, tiltDeg: 0.03,
            info: new CelestialInfo
            {
                Type = "Terrestrial planet",
                MassText = "3.301 × 10^23 kg (0.055 Earths)",
                RadiusText = "2,439.7 km",
                TempText = "−173 °C night to 427 °C day",
                DistanceText = "0.387 AU (57.9 million km)",
                YearText = "88 Earth days",
                DayText = "58.6 Earth days",
                GravityText = "3.7 m/s² (0.38 g)",
                EscapeVelText = "4.25 km/s",
                MoonsText = "0",
                AtmosphereText = "Virtually none (exosphere)",
                Facts = new[]
                {
                    "A Mercury year lasts less than two Mercury days (88 vs 176 day solar day).",
                    "Its iron core spans ~85% of the planet's radius.",
                    "Water ice survives in permanently shadowed polar craters."
                }
            }));

        list.Add(Planet("venus", "Venus", "Planet", "Earth's evil twin · runaway greenhouse",
            BodyTextureKind.Venus, new SKColor(255, 214, 130, 120), 2.4,
            orbit: new KeplerOrbit(a: 0.723 * AU, e: 0.0068, inclinationDeg: 3.39, periodDays: 224.7, meanAnomaly0Deg: 50),
            spinDays: -243, tiltDeg: 177.4,
            info: new CelestialInfo
            {
                Type = "Terrestrial planet",
                MassText = "4.867 × 10^24 kg (0.815 Earths)",
                RadiusText = "6,051.8 km",
                TempText = "737 K (464 °C) — hottest planet",
                DistanceText = "0.723 AU (108.2 million km)",
                YearText = "224.7 Earth days",
                DayText = "243 Earth days (retrograde)",
                GravityText = "8.87 m/s² (0.90 g)",
                EscapeVelText = "10.36 km/s",
                MoonsText = "0",
                AtmosphereText = "96.5% CO₂ · 92 bar surface pressure",
                Facts = new[]
                {
                    "Surface pressure equals 900 m deep ocean on Earth.",
                    "Rotates backwards — the Sun rises in the west.",
                    "Its day is longer than its year.",
                    "Sulfuric-acid clouds reflect 75% of sunlight."
                }
            }));

        var earth = Planet("earth", "Earth", "Planet", "The Pale Blue Dot · our home",
            BodyTextureKind.Earth, new SKColor(90, 160, 255, 110), 2.5,
            orbit: new KeplerOrbit(a: 1.0 * AU, e: 0.0167, inclinationDeg: 0.0, periodDays: 365.256, meanAnomaly0Deg: 358),
            spinDays: 0.9973, tiltDeg: 23.44, specular: 0.9,
            info: new CelestialInfo
            {
                Type = "Terrestrial planet · habitable",
                MassText = "5.972 × 10^24 kg",
                RadiusText = "6,371 km",
                TempText = "−89 °C to 58 °C (avg 15 °C)",
                DistanceText = "1 AU (149.6 million km)",
                YearText = "365.256 days",
                DayText = "23h 56m 4s",
                GravityText = "9.807 m/s² (1 g)",
                EscapeVelText = "11.186 km/s",
                MoonsText = "1 (the Moon)",
                AtmosphereText = "78% N₂ · 21% O₂ · 1% Ar",
                CompositionText = "Iron core, silicate mantle, liquid-water oceans covering 71%",
                AgeText = "4.54 billion years",
                Facts = new[]
                {
                    "The only known world with life — ~8.7 million species.",
                    "Magnetic field deflects the solar wind and deadly radiation.",
                    "Plate tectonics recycles the crust — unique in the Solar System.",
                    "Moon's tides stabilized Earth's climate for billions of years."
                }
            });
            list.Add(earth);

            var moon = Planet("moon", "Moon", "Moon", "Earth's natural satellite",
                BodyTextureKind.Moon, SKColors.Transparent, 0.68,
                orbit: new KeplerOrbit(a: 5.5, e: 0.0549, periodDays: 27.322, meanAnomaly0Deg: 115),
                spinDays: 27.322,
                info: new CelestialInfo
                {
                    Type = "Natural satellite of Earth",
                    MassText = "7.342 × 10^22 kg (0.0123 Earths)",
                    RadiusText = "1,737.4 km",
                    TempText = "−173 °C to 127 °C",
                    DistanceText = "384,400 km from Earth (1.28 light-seconds)",
                    YearText = "27.32 days around Earth",
                    GravityText = "1.62 m/s² (0.166 g)",
                    EscapeVelText = "2.38 km/s",
                    AtmosphereText = "Trace exosphere",
                    Facts = new[]
                    {
                        "Tidally locked — the same face always points at Earth.",
                        "Formed ~4.5 Gyr ago from debris of a Mars-sized impact (Theia).",
                        "Drifting away from Earth at 3.8 cm per year.",
                        "Only 12 humans have ever walked on it (Apollo, 1969-72)."
                    }
                });
            moon.OrbitCenterRef = earth;   // follows Earth each frame
            list.Add(moon);

        // moons follow their parents; wire after creation
        // (OrbitCenterOverride is a marker; the scene resolves parent links)

        list.Add(Planet("mars", "Mars", "Planet", "The Red Planet · rusted deserts",
            BodyTextureKind.Mars, new SKColor(255, 160, 110, 70), 1.9,
            orbit: new KeplerOrbit(a: 1.524 * AU, e: 0.0934, inclinationDeg: 1.85, periodDays: 686.98, meanAnomaly0Deg: 19),
            spinDays: 1.026, tiltDeg: 25.19,
            info: new CelestialInfo
            {
                Type = "Terrestrial planet",
                MassText = "6.417 × 10^23 kg (0.107 Earths)",
                RadiusText = "3,389.5 km",
                TempText = "−153 °C to 20 °C",
                DistanceText = "1.524 AU (227.9 million km)",
                YearText = "687 Earth days",
                DayText = "24h 37m",
                GravityText = "3.72 m/s² (0.38 g)",
                EscapeVelText = "5.03 km/s",
                MoonsText = "2 (Phobos, Deimos)",
                AtmosphereText = "95% CO₂ · 0.006 bar",
                Facts = new[]
                {
                    "Olympus Mons is the tallest volcano known — 21.9 km high.",
                    "Valles Marineris canyon would stretch across the USA.",
                    "Global dust storms can shroud the whole planet for weeks.",
                    "Ancient river deltas show it once had flowing water."
                }
            }));

        list.Add(Planet("phobos", "Phobos", "Moon", "Doomed inner moon of Mars",
            BodyTextureKind.RockyGeneric, SKColors.Transparent, 0.38,
            orbit: new KeplerOrbit(a: 3.6, periodDays: 0.3189),
            spinDays: 0.3189,
            info: new CelestialInfo
            {
                Type = "Moon of Mars",
                MassText = "1.0659 × 10^16 kg",
                RadiusText = "11.27 km (mean)",
                DistanceText = "9,376 km from Mars",
                YearText = "7h 39m — faster than Mars rotates",
                Facts = new[]
                {
                    "Spirals inward ~1.8 cm/year; will crash or break up in ~50 Myr.",
                    "Orbits three times per Martian day."
                }
            }));

        list.Add(Planet("deimos", "Deimos", "Moon", "Tiny outer moon of Mars",
            BodyTextureKind.RockyGeneric, SKColors.Transparent, 0.32,
            orbit: new KeplerOrbit(a: 4.9, periodDays: 1.263),
            spinDays: 1.263,
            info: new CelestialInfo
            {
                Type = "Moon of Mars",
                MassText = "1.4762 × 10^15 kg",
                RadiusText = "6.2 km (mean)",
                DistanceText = "23,463 km from Mars",
                YearText = "30.3 hours",
                Facts = new[] { "One of the smallest known moons in the Solar System." }
            }));

        var jupiter = Planet("jupiter", "Jupiter", "Planet", "King of planets · Great Red Spot",
            BodyTextureKind.Jupiter, new SKColor(255, 220, 180, 60), 6.4,
            orbit: new KeplerOrbit(a: 5.204 * AU, e: 0.0489, inclinationDeg: 1.303, periodDays: 4332.59, meanAnomaly0Deg: 20),
            spinDays: 0.4135, tiltDeg: 3.13,
            info: new CelestialInfo
            {
                Type = "Gas giant",
                MassText = "1.898 × 10^27 kg (318 Earths · 2.5× all other planets combined)",
                RadiusText = "69,911 km (11.2 × Earth)",
                TempText = "−145 °C cloud tops",
                DistanceText = "5.204 AU (778.5 million km)",
                YearText = "11.86 Earth years",
                DayText = "9h 56m — fastest spin of any planet",
                GravityText = "24.79 m/s² (2.53 g)",
                EscapeVelText = "59.5 km/s",
                MoonsText = "95 confirmed",
                AtmosphereText = "90% H₂ · 10% He",
                CompositionText = "Metallic hydrogen mantle; likely diffuse rocky core",
                Facts = new[]
                {
                    "The Great Red Spot is a storm larger than Earth, raging 350+ years.",
                    "Its magnetosphere is the largest structure in the Solar System.",
                    "Jupiter's gravity shields inner planets from many comets.",
                    "It radiates more heat than it receives from the Sun."
                }
            });
            list.Add(jupiter);

            list.Add(MoonOf("io", "Io", jupiter, "Most volcanically active world",
                BodyTextureKind.Io, 0.75, 9.5, 1.769,
                new CelestialInfo
                {
                    Type = "Galilean moon of Jupiter",
                    MassText = "8.932 × 10^22 kg",
                    RadiusText = "1,821.6 km",
                    DistanceText = "421,700 km from Jupiter",
                    YearText = "1.77 days",
                    Facts = new[]
                    {
                        "400+ active volcanoes; sulfur plumes 500 km high.",
                        "Squeezed by tidal flexing from Jupiter and the other moons."
                    }
                }, BodyTextureKind.Io));

            list.Add(MoonOf("europa", "Europa", jupiter, "Ocean world · life candidate",
                BodyTextureKind.Europa, 0.65, 11.5, 3.551,
                new CelestialInfo
                {
                    Type = "Galilean moon of Jupiter",
                    MassText = "4.8 × 10^22 kg",
                    RadiusText = "1,560.8 km",
                    DistanceText = "670,900 km from Jupiter",
                    YearText = "3.55 days",
                    Facts = new[]
                    {
                        "Subsurface saltwater ocean holds ~2× Earth's liquid water.",
                        "Ice shell cracked into 'lineae' — ridges from tidal stress.",
                        "NASA's Europa Clipper launched 2024 to study habitability."
                    }
                }));

            list.Add(MoonOf("ganymede", "Ganymede", jupiter, "Largest moon in the Solar System",
                BodyTextureKind.Ganymede, 1.05, 13.8, 7.155,
                new CelestialInfo
                {
                    Type = "Galilean moon of Jupiter",
                    MassText = "1.4819 × 10^23 kg",
                    RadiusText = "2,634.1 km — bigger than Mercury",
                    DistanceText = "1,070,400 km from Jupiter",
                    YearText = "7.15 days",
                    Facts = new[]
                    {
                        "Only moon with its own magnetic field.",
                        "Hosts a subsurface ocean beneath ~150 km of ice."
                    }
                }));

            list.Add(MoonOf("callisto", "Callisto", jupiter, "Cratered ancient ice world",
                BodyTextureKind.Callisto, 0.95, 16.5, 16.689,
                new CelestialInfo
                {
                    Type = "Galilean moon of Jupiter",
                    MassText = "1.0759 × 10^23 kg",
                    RadiusText = "2,410.3 km",
                    DistanceText = "1,882,700 km from Jupiter",
                    YearText = "16.69 days",
                    Facts = new[]
                    {
                        "The most heavily cratered object known in the Solar System.",
                        "Considered a future base site — outside Jupiter's worst radiation."
                    }
                }));

        var saturn = Planet("saturn", "Saturn", "Planet", "Jewel of the Solar System · ringed giant",
            BodyTextureKind.Saturn, new SKColor(255, 230, 170, 55), 5.6,
            orbit: new KeplerOrbit(a: 9.583 * AU, e: 0.0565, inclinationDeg: 2.485, periodDays: 10759.22, meanAnomaly0Deg: 317),
            spinDays: 0.444, tiltDeg: 26.73,
            info: new CelestialInfo
            {
                Type = "Gas giant",
                MassText = "5.683 × 10^26 kg (95 Earths)",
                RadiusText = "58,232 km (9.14 × Earth)",
                TempText = "−178 °C cloud tops",
                DistanceText = "9.583 AU (1.43 billion km)",
                YearText = "29.45 Earth years",
                DayText = "10h 42m",
                GravityText = "10.44 m/s² (1.06 g)",
                EscapeVelText = "35.5 km/s",
                MoonsText = "146 confirmed",
                AtmosphereText = "96% H₂ · 3% He",
                CompositionText = "Least dense planet — would float on water (0.687 g/cm³)",
                Facts = new[]
                {
                    "Rings span 282,000 km but are only ~10-100 m thick in places.",
                    "Rings are 99.9% water ice, from microscopic dust to house-sized boulders.",
                    "A hexagonal jet stream circles its north pole.",
                    "Cassini spacecraft orbited it 2004-2017."
                }
            });
            saturn.HasRings = true; saturn.RingInner = 1.25; saturn.RingOuter = 2.35; saturn.RingTiltDeg = 26.7;
            list.Add(saturn);

            list.Add(MoonOf("titan", "Titan", saturn, "Thick atmosphere · methane lakes",
                BodyTextureKind.Titan, 1.0, 10.8, 15.945,
                new CelestialInfo
                {
                    Type = "Moon of Saturn",
                    MassText = "1.3452 × 10^23 kg",
                    RadiusText = "2,574.7 km — larger than Mercury",
                    DistanceText = "1,221,870 km from Saturn",
                    YearText = "15.95 days",
                    AtmosphereText = "95% N₂ + methane · 1.45 bar",
                    Facts = new[]
                    {
                        "Only moon with a dense atmosphere — orange organic haze.",
                        "Lakes and rivers of liquid methane/ethane; rain and clouds.",
                        "Huygens probe landed 2005; NASA Dragonfly rotorcraft launches 2028."
                    }
                }));

            list.Add(MoonOf("enceladus", "Enceladus", saturn, "Cryovolcanic plumes of ocean water",
                BodyTextureKind.Enceladus, 0.4, 8.2, 1.37,
                new CelestialInfo
                {
                    Type = "Moon of Saturn",
                    MassText = "1.08 × 10^20 kg",
                    RadiusText = "252.1 km",
                    DistanceText = "238,040 km from Saturn",
                    YearText = "1.37 days",
                    Facts = new[]
                    {
                        "Geysers at its south pole jet ocean water into space.",
                        "Cassini 'tasted' the plumes: salts, organics, and silica — habitable chemistry.",
                        "Most reflective body in the Solar System (albedo 0.99)."
                    }
                }));

            list.Add(MoonOf("rhea", "Rhea", saturn, "Second-largest Saturnian moon",
                BodyTextureKind.IceGeneric, 0.6, 9.3, 4.518,
                new CelestialInfo
                {
                    Type = "Moon of Saturn",
                    MassText = "2.31 × 10^21 kg",
                    RadiusText = "763.8 km",
                    DistanceText = "527,100 km from Saturn",
                    YearText = "4.52 days",
                    Facts = new[] { "May once have had its own faint ring system." }
                }));

        var uranus = Planet("uranus", "Uranus", "Planet", "Sideways ice giant",
            BodyTextureKind.Uranus, new SKColor(150, 230, 235, 55), 3.6,
            orbit: new KeplerOrbit(a: 19.19 * AU, e: 0.0457, inclinationDeg: 0.773, periodDays: 30688.5, meanAnomaly0Deg: 142),
            spinDays: -0.718, tiltDeg: 97.77,
            info: new CelestialInfo
            {
                Type = "Ice giant",
                MassText = "8.681 × 10^25 kg (14.5 Earths)",
                RadiusText = "25,362 km (3.98 × Earth)",
                TempText = "−224 °C — coldest atmosphere in the Solar System",
                DistanceText = "19.19 AU (2.87 billion km)",
                YearText = "84 Earth years",
                DayText = "17h 14m (retrograde)",
                GravityText = "8.87 m/s² (0.90 g)",
                EscapeVelText = "21.3 km/s",
                MoonsText = "28 known",
                AtmosphereText = "83% H₂ · 15% He · 2% CH₄ (methane makes it cyan)",
                Facts = new[]
                {
                    "Tipped on its side 98° — it orbits like a rolling ball.",
                    "Each pole gets 42 years of sunlight, then 42 years of darkness.",
                    "Discovered in 1781 by William Herschel — first telescope-found planet.",
                    "Visited only once: Voyager 2 flyby, 1986."
                }
            });
            uranus.HasRings = true; uranus.RingInner = 1.6; uranus.RingOuter = 2.0; uranus.RingTiltDeg = 97.8;
            list.Add(uranus);

            list.Add(MoonOf("titania", "Titania", uranus, "Largest moon of Uranus",
                BodyTextureKind.IceGeneric, 0.55, 6.4, 8.706,
                new CelestialInfo
                {
                    Type = "Moon of Uranus",
                    MassText = "3.4 × 10^21 kg",
                    RadiusText = "788.4 km",
                    DistanceText = "435,900 km from Uranus",
                    YearText = "8.71 days",
                    Facts = new[] { "Huge fault canyons suggest its interior once expanded." }
                }));

        var neptune = Planet("neptune", "Neptune", "Planet", "Windswept azure giant",
            BodyTextureKind.Neptune, new SKColor(80, 130, 255, 65), 3.5,
            orbit: new KeplerOrbit(a: 30.07 * AU, e: 0.0113, inclinationDeg: 1.77, periodDays: 60195, meanAnomaly0Deg: 256),
            spinDays: 0.671, tiltDeg: 28.32,
            info: new CelestialInfo
            {
                Type = "Ice giant",
                MassText = "1.024 × 10^26 kg (17.1 Earths)",
                RadiusText = "24,622 km (3.86 × Earth)",
                TempText = "−214 °C cloud tops",
                DistanceText = "30.07 AU (4.50 billion km)",
                YearText = "164.8 Earth years",
                DayText = "16h 6m",
                GravityText = "11.15 m/s² (1.14 g)",
                EscapeVelText = "23.5 km/s",
                MoonsText = "16 known",
                AtmosphereText = "80% H₂ · 19% He · CH₄",
                Facts = new[]
                {
                    "Fastest winds in the Solar System: up to 2,100 km/h.",
                    "Found by mathematics in 1846 — predicted from Uranus's wobble.",
                    "Radiates 2.6× more energy than it absorbs from the Sun.",
                    "One Neptune year = 165 Earth years (first orbit completed 2011)."
                }
            });
            list.Add(neptune);

            list.Add(MoonOf("triton", "Triton", neptune, "Captured Kuiper Belt world",
                BodyTextureKind.Triton, 0.6, 6.2, -5.877,   // retrograde
                new CelestialInfo
                {
                    Type = "Moon of Neptune",
                    MassText = "2.14 × 10^22 kg",
                    RadiusText = "1,353.4 km",
                    DistanceText = "354,760 km from Neptune",
                    YearText = "5.88 days (retrograde!)",
                    Facts = new[]
                    {
                        "Orbits backwards — almost certainly a captured Kuiper Belt object.",
                        "Nitrogen geysers erupt 8 km high from its −235 °C surface.",
                        "Slowly spiraling inward; will be shredded into a ring in ~3.6 Gyr."
                    }
                }));

        var pluto = Planet("pluto", "Pluto", "Dwarf Planet", "Heart-marked Kuiper Belt king",
            BodyTextureKind.Pluto, new SKColor(220, 200, 180, 40), 0.75,
            orbit: new KeplerOrbit(a: 39.48 * AU, e: 0.2488, inclinationDeg: 17.16, periodDays: 90560, meanAnomaly0Deg: 14),
            spinDays: 6.387, tiltDeg: 122.5,
            info: new CelestialInfo
            {
                Type = "Dwarf planet (Kuiper Belt)",
                MassText = "1.303 × 10^22 kg (0.0022 Earths)",
                RadiusText = "1,188.3 km",
                TempText = "−229 °C",
                DistanceText = "39.48 AU average (29.7–49.3 AU)",
                YearText = "248 Earth years",
                DayText = "6.39 days",
                GravityText = "0.62 m/s² (0.063 g)",
                EscapeVelText = "1.21 km/s",
                MoonsText = "5 (Charon, Nix, Hydra, Kerberos, Styx)",
                AtmosphereText = "Thin N₂/CH₄ — freezes out as it recedes from the Sun",
                Facts = new[]
                {
                    "New Horizons revealed its nitrogen-ice 'heart' (Sputnik Planitia) in 2015.",
                    "Pluto and Charon orbit a point between them — a true double system.",
                    "Reclassified from planet to dwarf planet in 2006.",
                    "Some days, you could walk faster than Pluto's atmosphere-cycling winds."
                }
            });
            list.Add(pluto);

            list.Add(MoonOf("charon", "Charon", pluto, "Half of a double world",
                BodyTextureKind.Charon, 0.4, 2.2, 6.387,
                new CelestialInfo
                {
                    Type = "Moon of Pluto",
                    MassText = "1.586 × 10^21 kg",
                    RadiusText = "606 km — half Pluto's size",
                    DistanceText = "19,591 km from Pluto",
                    YearText = "6.39 days (mutually locked)",
                    Facts = new[]
                    {
                        "Pluto and Charon face each other forever — both tidally locked.",
                        "Its north pole is stained dark red by escaping methane from Pluto."
                    }
                }));

        list.Add(Planet("ceres", "Ceres", "Dwarf Planet", "Queen of the asteroid belt",
            BodyTextureKind.RockyGeneric, SKColors.Transparent, 0.45,
            orbit: new KeplerOrbit(a: 2.767 * AU, e: 0.0758, inclinationDeg: 10.59, periodDays: 1681.6, meanAnomaly0Deg: 95),
            spinDays: 0.378,
            info: new CelestialInfo
            {
                Type = "Dwarf planet (main belt)",
                MassText = "9.38 × 10^20 kg (25% of the belt's mass)",
                RadiusText = "469.7 km",
                DistanceText = "2.77 AU (413.7 million km)",
                YearText = "4.6 Earth years",
                DayText = "9h 4m",
                Facts = new[]
                {
                    "Bright spots in Occator crater are salt from a subsurface brine.",
                    "Likely has a muddy ice-water ocean beneath its crust.",
                    "First asteroid ever discovered (Piazzi, 1801)."
                }
            }));

        // Halley's Comet — high eccentricity retrograde orbit
        var halley = Planet("halley", "Halley's Comet", "Comet", "The famous periodic comet",
            BodyTextureKind.IceGeneric, SKColors.Transparent, 0.3,
            orbit: new KeplerOrbit(a: 17.834 * AU, e: 0.967, inclinationDeg: 162.3, periodDays: 27510, meanAnomaly0Deg: 38),
            spinDays: 2.2,
            info: new CelestialInfo
            {
                Type = "Short-period comet (1P/Halley)",
                MassText = "2.2 × 10^14 kg (est.)",
                RadiusText = "5.5 × 4 × 0.8 km nucleus",
                DistanceText = "0.586 – 35.1 AU from the Sun",
                YearText = "75–79 years",
                Facts = new[]
                {
                    "Recorded since 240 BC; appears in the Bayeux Tapestry (1066).",
                    "Next perihelion: 28 July 2061.",
                    "Giotto probe imaged its nucleus up close in 1986.",
                    "Orbits retrograde — opposite direction to the planets."
                }
            });
        list.Add(halley);

        return list;
    }

    /// <summary>Resolve moon OrbitCenter links to their parent planets (by position each frame).</summary>
    public static void LinkMoons(List<SpaceObject> all)
    {
        var byId = all.OfType<PlanetObject>().ToDictionary(p => p.Id);
        foreach (var m in all.OfType<PlanetObject>())
        {
            if (m.Id == "moon") m.OrbitCenterRef = byId["earth"];
            else if (m.Id == "phobos" || m.Id == "deimos") m.OrbitCenterRef = byId["mars"];
            else if (m.Id is "io" or "europa" or "ganymede" or "callisto") m.OrbitCenterRef = byId["jupiter"];
            else if (m.Id is "titan" or "enceladus" or "rhea") m.OrbitCenterRef = byId["saturn"];
            else if (m.Id is "titania") m.OrbitCenterRef = byId["uranus"];
            else if (m.Id is "triton") m.OrbitCenterRef = byId["neptune"];
            else if (m.Id is "charon") m.OrbitCenterRef = byId["pluto"];
        }
    }

    private static PlanetObject Planet(string id, string name, string category, string tag,
        BodyTextureKind kind, SKColor atmos, double visR,
        KeplerOrbit? orbit = null, double spinDays = 1, double tiltDeg = 0, double specular = 0,
        CelestialInfo? info = null)
    {
        return new PlanetObject
        {
            Id = id, Name = name, Category = category, EmTag = tag,
            TextureKind = kind, Atmosphere = atmos, VisualRadius = visR,
            Orbit = orbit, SpinPeriodDays = spinDays, AxialTiltDeg = tiltDeg,
            Specular = specular, Info = info ?? new CelestialInfo()
        };
    }

    private static MoonObject MoonOf(string id, string name, PlanetObject parent, string tag,
        BodyTextureKind kind, double visR, double orbitR, double periodDays,
        CelestialInfo info, BodyTextureKind? texture = null)
    {
        return new MoonObject
        {
            Id = id, Name = name, Category = "Moon", EmTag = tag,
            TextureKind = texture ?? kind, VisualRadius = visR,
            Orbit = new KeplerOrbit(a: orbitR, periodDays: periodDays),
            OrbitCenterRef = parent,
            SpinPeriodDays = Math.Abs(periodDays),
            Info = info
        };
    }
}
