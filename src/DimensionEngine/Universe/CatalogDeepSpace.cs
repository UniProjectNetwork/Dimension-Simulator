using DimensionEngine.Maths;
using DimensionEngine.Render;
using DimensionEngine.Utils;
using DimensionEngine.Sim;
using SkiaSharp;

namespace DimensionEngine.Universe;

/// <summary>
/// Deep Space catalog: named stars (from Proxima Centauri to Stephenson 2-18),
/// black holes (TON 618, Sagittarius A*, M87*, Cygnus X-1...), neutron stars,
/// nebulae, galaxies and exoplanet systems — laid out on a logarithmic distance map
/// with real data attached.
/// </summary>
public static class CatalogDeepSpace
{
    /// <summary>Logarithmic map: real light-years → scene units.</summary>
    public static double MapRadius(double lightYears) => 36 * Math.Log10(1 + lightYears / 4.2465);

    private static Vec3 MapPos(double lightYears, double angleDeg)
    {
        double r = MapRadius(lightYears);
        double a = angleDeg * MathUtil.DegToRad;
        return new Vec3(Math.Cos(a) * r, 0, Math.Sin(a) * r);
    }

    public static List<SpaceObject> Build()
    {
        var list = new List<SpaceObject>();
        var rng = Noise.Rng(424242);

        // ================= STARS =================

        list.Add(Star("proxima", "Proxima Centauri", "Star", "Closest star to the Sun · red dwarf",
            spectral: "M5.5Ve", color: new SKColor(255, 150, 95), visR: 2.2, ly: 4.2465, angle: 15,
            info: new CelestialInfo
            {
                Type = "M-type red dwarf (flare star)",
                MassText = "0.1221 M☉ (1.073 × 10^29 kg)",
                RadiusText = "0.1542 R☉ (~107,500 km)",
                TempText = "3,042 K",
                DistanceText = "4.2465 light-years (closest)",
                LuminosityText = "0.0017 L☉",
                AgeText = "≈ 4.85 billion years",
                Facts = new[]
                {
                    "Closest star to our Sun — 4.2465 ly (268,000 AU).",
                    "Hosts Proxima b, a potentially habitable Earth-mass planet (11.2-day orbit).",
                    "A flare star: brightens dramatically every few months.",
                    "Too dim to see with the naked eye (mag 11.1)."
                }
            }));

        list.Add(Star("alphacenA", "Alpha Centauri A", "Star", "Sun's twin next door",
            spectral: "G2V", color: new SKColor(255, 240, 195), visR: 3.6, ly: 4.365, angle: 40,
            info: new CelestialInfo
            {
                Type = "G2V main-sequence star (binary with α Cen B)",
                MassText = "1.079 M☉",
                RadiusText = "1.223 R☉",
                TempText = "5,790 K",
                DistanceText = "4.365 light-years",
                LuminosityText = "1.519 L☉",
                Facts = new[]
                {
                    "Nearly a twin of our Sun in type and temperature.",
                    "Part of the closest star system, 4.37 ly away.",
                    "Orbits its partner every ~80 years."
                }
            }));

        list.Add(Star("alphacenB", "Alpha Centauri B", "Star", "Orange companion of α Cen A",
            spectral: "K1V", color: new SKColor(255, 205, 140), visR: 3.1, ly: 4.365, angle: 44,
            info: new CelestialInfo
            {
                Type = "K1V main-sequence star",
                MassText = "0.909 M☉",
                RadiusText = "0.863 R☉",
                TempText = "5,260 K",
                DistanceText = "4.365 light-years",
                LuminosityText = "0.5 L☉",
                Facts = new[] { "K-type orange dwarf — longer-lived and calmer than the Sun." }
            }));

        list.Add(Star("barnard", "Barnard's Star", "Star", "Fastest-moving star in our sky",
            spectral: "M4V", color: new SKColor(255, 155, 100), visR: 2.3, ly: 5.963, angle: 70,
            info: new CelestialInfo
            {
                Type = "M4V red dwarf",
                MassText = "0.144 M☉",
                RadiusText = "0.196 R☉",
                TempText = "3,134 K",
                DistanceText = "5.963 light-years",
                AgeText = "≈ 10 billion years (twice the Sun's age)",
                Facts = new[]
                {
                    "Has the highest proper motion of any star — crosses a Moon-width every 180 years.",
                    "Hosts at least one planet (Barnard b, 3.15-day orbit)."
                }
            }));

        list.Add(Star("sirius", "Sirius A", "Star", "Brightest star in Earth's night sky",
            spectral: "A1V", color: new SKColor(222, 233, 255), visR: 4.4, ly: 8.6, angle: 100,
            info: new CelestialInfo
            {
                Type = "A1V main-sequence star (binary)",
                MassText = "2.063 M☉",
                RadiusText = "1.711 R☉",
                TempText = "9,940 K",
                DistanceText = "8.60 light-years",
                LuminosityText = "25.4 L☉",
                Facts = new[]
                {
                    "The brightest star in Earth's night sky (mag −1.46).",
                    "Ancient Egyptians used its dawn rising to predict Nile floods.",
                    "Has a white dwarf companion, Sirius B, in a 50-year orbit."
                }
            }));

        list.Add(Star("siriusb", "Sirius B", "White Dwarf", "Earth-sized stellar corpse",
            spectral: "DA2", color: new SKColor(205, 220, 255), visR: 1.9, ly: 8.6, angle: 104,
            info: new CelestialInfo
            {
                Type = "DA2 white dwarf (Sirius A's companion)",
                MassText = "1.018 M☉ — compressed into an Earth-sized sphere",
                RadiusText = "0.0084 R☉ (~5,850 km — smaller than Earth)",
                TempText = "25,200 K",
                DistanceText = "8.60 light-years",
                GravityText = "≈ 4 million g surface gravity",
                SchwarzschildText = "3.0 km",
                Facts = new[]
                {
                    "A teaspoon of its matter weighs ~5 tonnes.",
                    "Once a 5-solar-mass blue star, now a dead ember cooling for eternity.",
                    "Density ~1 tonne per cubic centimeter."
                }
            }));

        list.Add(Star("vega", "Vega", "Star", "Former northern pole star",
            spectral: "A0V", color: new SKColor(228, 236, 255), visR: 4.2, ly: 25.04, angle: 130,
            info: new CelestialInfo
            {
                Type = "A0V main-sequence star",
                MassText = "2.14 M☉",
                RadiusText = "2.36 × 2.74 R☉ (spinning fast, oblate)",
                TempText = "9,602 K",
                DistanceText = "25.04 light-years",
                LuminosityText = "40.12 L☉",
                Facts = new[]
                {
                    "Will be the pole star again around the year 13,727.",
                    "Spins at 236 km/s — visibly flattened.",
                    "The original zero-point of the photometric magnitude scale."
                }
            }));

        list.Add(Star("arcturus", "Arcturus", "Star", "Orange giant beacon of Boötes",
            spectral: "K1.5III", color: new SKColor(255, 190, 120), visR: 5.2, ly: 36.7, angle: 160,
            info: new CelestialInfo
            {
                Type = "K1.5III red giant",
                MassText = "1.08 M☉",
                RadiusText = "25.4 R☉",
                TempText = "4,286 K",
                DistanceText = "36.7 light-years",
                LuminosityText = "170 L☉",
                Facts = new[]
                {
                    "Brightest star in the northern celestial hemisphere.",
                    "A visitor from the galactic thick disk — older population.",
                    "It has exhausted its core hydrogen and swelled into a giant."
                }
            }));

        list.Add(Star("aldebaran", "Aldebaran", "Star", "The Bull's fiery eye",
            spectral: "K5III", color: new SKColor(255, 170, 110), visR: 5.0, ly: 65.3, angle: 190,
            info: new CelestialInfo
            {
                Type = "K5III red giant",
                MassText = "1.16 M☉",
                RadiusText = "44.1 R☉",
                TempText = "3,910 K",
                DistanceText = "65.3 light-years",
                LuminosityText = "439 L☉",
                Facts = new[]
                {
                    "The 'eye of the Bull' in Taurus; appears in the Hyades cluster's foreground.",
                    "Name means 'the follower' in Arabic (follows the Pleiades)."
                }
            }));

        list.Add(Star("polaris", "Polaris", "Star", "The North Star · Cepheid variable",
            spectral: "F7Ib", color: new SKColor(255, 245, 220), visR: 4.8, ly: 433, angle: 220,
            info: new CelestialInfo
            {
                Type = "F7Ib yellow supergiant (Cepheid variable)",
                MassText = "5.4 M☉",
                RadiusText = "37.5 R☉",
                TempText = "6,015 K",
                DistanceText = "≈ 433 light-years",
                LuminosityText = "1,260 L☉",
                Facts = new[]
                {
                    "Almost exactly aligned with Earth's rotation axis — north by definition.",
                    "Pulses in brightness every 3.97 days as a Cepheid.",
                    "Part of a triple star system."
                }
            }));

        list.Add(Star("pollux", "Pollux", "Star", "One of the Twins, with a planet",
            spectral: "K0III", color: new SKColor(255, 200, 140), visR: 4.9, ly: 33.8, angle: 250,
            info: new CelestialInfo
            {
                Type = "K0III orange giant",
                MassText = "1.91 M☉",
                RadiusText = "8.8 R☉",
                TempText = "4,666 K",
                DistanceText = "33.8 light-years",
                Facts = new[] { "Hosts Thestias (Pollux b), a planet at least twice Jupiter's mass." }
            }));

        list.Add(Star("rigel", "Rigel", "Star", "Blue supergiant of Orion's heel",
            spectral: "B8Ia", color: new SKColor(180, 200, 255), visR: 7.4, ly: 863, angle: 280,
            info: new CelestialInfo
            {
                Type = "B8Ia blue supergiant (7-star system)",
                MassText = "21 M☉",
                RadiusText = "78.9 R☉",
                TempText = "12,100 K",
                DistanceText = "863 light-years",
                LuminosityText = "≈ 120,000 L☉",
                Facts = new[]
                {
                    "Outshines the Sun 120,000 times.",
                    "Will explode as a supernova within a few million years.",
                    "If placed at the Sun's distance, it would outshine Venus 3,000×."
                }
            }));

        list.Add(Star("betelgeuse", "Betelgeuse", "Star", "Red supergiant · future supernova",
            spectral: "M1-2Ia", color: new SKColor(255, 130, 70), visR: 12.5, ly: 548, angle: 300,
            info: new CelestialInfo
            {
                Type = "M1-2Ia red supergiant",
                MassText = "≈ 16.5–19 M☉",
                RadiusText = "≈ 764 R☉ — would engulf Jupiter's orbit",
                TempText = "3,600 K",
                DistanceText = "548 light-years",
                LuminosityText = "≈ 126,000 L☉",
                AgeText = "8–8.5 million years (fast-living massive star)",
                Facts = new[]
                {
                    "Expected to go supernova 'soon' — within ~100,000 years.",
                    "When it does, it will shine as bright as the half Moon for months.",
                    "The 2019-20 'Great Dimming' was caused by a giant dust cloud burp.",
                    "If placed at the Sun, its surface would reach past Mars."
                }
            }));

        list.Add(Star("antares", "Antares", "Star", "Rival of Mars",
            spectral: "M1.5Iab", color: new SKColor(255, 125, 65), visR: 11.8, ly: 550, angle: 330,
            info: new CelestialInfo
            {
                Type = "M1.5Iab red supergiant",
                MassText = "≈ 12 M☉",
                RadiusText = "≈ 680 R☉",
                TempText = "3,660 K",
                DistanceText = "550 light-years",
                LuminosityText = "≈ 75,900 L☉",
                Facts = new[]
                {
                    "Name means 'rival of Ares (Mars)' — both glow red in the sky.",
                    "Another future supernova candidate."
                }
            }));

        list.Add(Star("uyscuti", "UY Scuti", "Star", "One of the largest known stars",
            spectral: "M4Ia", color: new SKColor(255, 115, 60), visR: 13.6, ly: 5900, angle: 355,
            info: new CelestialInfo
            {
                Type = "M4Ia red supergiant / extreme hypergiant",
                MassText = "≈ 7–10 M☉ (has shed most of its mass)",
                RadiusText = "≈ 1,708 R☉ — volume ≈ 5 billion Suns",
                TempText = "3,365 K",
                DistanceText = "≈ 5,900 light-years",
                LuminosityText = "≈ 340,000 L☉",
                Facts = new[]
                {
                    "If it replaced the Sun, its surface would reach Saturn's orbit.",
                    "A plane flying at 900 km/h would need 1,300 years to circle it."
                }
            }));

        list.Add(Star("st2-18", "Stephenson 2-18", "Star", "Largest known star",
            spectral: "M6Ia", color: new SKColor(255, 105, 55), visR: 14.4, ly: 18900, angle: 25,
            info: new CelestialInfo
            {
                Type = "M6 red supergiant (Stephenson 2 cluster)",
                MassText = "≈ 20–25 M☉ (estimated)",
                RadiusText = "≈ 2,150 R☉ — the largest known stellar radius",
                TempText = "3,200 K",
                DistanceText = "≈ 18,900 light-years",
                LuminosityText = "≈ 440,000 L☉",
                Facts = new[]
                {
                    "Volume ≈ 10 billion Suns; light takes 9 hours to travel its circumference.",
                    "If it replaced the Sun, its surface would engulf Saturn's orbit."
                }
            }));

        list.Add(Star("vycma", "VY Canis Majoris", "Star", "Extreme red hypergiant",
            spectral: "M3-M4.5", color: new SKColor(255, 110, 55), visR: 13.2, ly: 3820, angle: 55,
            info: new CelestialInfo
            {
                Type = "M3–M4.5 red hypergiant",
                MassText = "≈ 17 M☉",
                RadiusText = "≈ 1,420 R☉",
                TempText = "3,490 K",
                DistanceText = "≈ 3,820 light-years",
                LuminosityText = "≈ 270,000 L☉",
                Facts = new[]
                {
                    "Loses 30 Earth-masses of material every year in violent eruptions.",
                    "Surrounded by vast arcs of ejected dust."
                }
            }));

        list.Add(Star("etacar", "Eta Carinae", "Star", "Doomed hypergiant binary",
            spectral: "LBV", color: new SKColor(255, 190, 160), visR: 8.0, ly: 7500, angle: 80,
            info: new CelestialInfo
            {
                Type = "Luminous blue variable + Wolf-Rayet binary",
                MassText = "≈ 90–100 + 30 M☉",
                RadiusText = "≈ 240 R☉ (primary)",
                TempText = "≈ 15,000–25,000 K (variable)",
                DistanceText = "≈ 7,500 light-years",
                LuminosityText = "≈ 5 million L☉",
                Facts = new[]
                {
                    "Survived the 'Great Eruption' of 1843 that briefly made it the sky's 2nd-brightest star.",
                    "Wrapped in the Homunculus Nebula it blew off itself.",
                    "Prime candidate for a hypernova / gamma-ray burst."
                }
            }));

        list.Add(Star("trappist1", "TRAPPIST-1", "Star", "Ultra-cool dwarf with 7 Earths",
            spectral: "M8V", color: new SKColor(255, 130, 80), visR: 2.4, ly: 40.66, angle: 205,
            info: new CelestialInfo
            {
                Type = "M8V ultra-cool red dwarf",
                MassText = "0.0898 M☉ (just above the hydrogen-burning limit)",
                RadiusText = "0.1192 R☉ (barely bigger than Jupiter)",
                TempText = "2,566 K",
                DistanceText = "40.66 light-years",
                LuminosityText = "0.000553 L☉",
                Facts = new[]
                {
                    "Hosts 7 Earth-sized planets — 3 in the habitable zone.",
                    "All 7 planets would fit comfortably inside Mercury's orbit.",
                    "JWST is measuring their atmospheres planet-by-planet."
                }
            }));

        // ================= NEUTRON STARS =================

        var pulsar = new NeutronStarObject
        {
            Id = "psr-j1748", Name = "PSR J1748-2446ad", Category = "Neutron Star",
            EmTag = "Fastest known pulsar · 716 Hz",
            VisualRadius = 1.6,
            StarColor = new SKColor(210, 230, 255),
            Position = MapPos(25000, 310),
            Info = new CelestialInfo
            {
                Type = "Millisecond pulsar (neutron star)",
                MassText = "≈ 1.4 M☉",
                RadiusText = "≈ 16 km",
                TempText = "surface ~10^6 K",
                DistanceText = "≈ 25,000 light-years (Terzan 5 cluster)",
                RotationText = "716 Hz — fastest known spin",
                SchwarzschildText = "4.1 km",
                Facts = new[]
                {
                    "Spins 716 times per second — its equator moves at ~24% of light speed.",
                    "A sugar-cube of neutron-star matter weighs ~1 billion tonnes.",
                    "Formed when a massive star's core collapsed in a supernova."
                }
            }
        };
        list.Add(pulsar);

        var magnetar = new NeutronStarObject
        {
            Id = "sgr1806", Name = "SGR 1806-20", Category = "Neutron Star",
            EmTag = "Magnetar · 10^15 gauss",
            VisualRadius = 1.7,
            StarColor = new SKColor(190, 210, 255),
            Position = MapPos(50000, 130),
            Info = new CelestialInfo
            {
                Type = "Magnetar (ultra-magnetic neutron star)",
                MassText = "≈ 1.4 M☉",
                RadiusText = "≈ 20 km",
                DistanceText = "≈ 50,000 light-years",
                MagneticFieldText = "≈ 10^15 gauss (10^11 Tesla)",
                Facts = new[]
                {
                    "Its 2004 flare, 50,000 ly away, briefly altered Earth's ionosphere.",
                    "Magnetic field a quadrillion times stronger than Earth's.",
                    "Starquakes on its crust release more energy in 0.1 s than the Sun in 150,000 years."
                }
            }
        };
        list.Add(magnetar);

        // ================= BLACK HOLES =================

        list.Add(BlackHole("sgr-a", "Sagittarius A*", "Black Hole", "Our galaxy's central monster",
            massSolar: 4.297e6, ly: 26673, angle: 0, visR: 16.5, tilt: 40, jets: false,
            info: new CelestialInfo
            {
                Type = "Supermassive black hole (Milky Way center)",
                MassText = "4.297 million M☉",
                SchwarzschildText = "12.67 million km (≈ 0.085 AU)",
                DistanceText = "26,673 light-years",
                DiscoveryText = "Imaged by the Event Horizon Telescope, May 2022",
                Facts = new[]
                {
                    "Every star in the Milky Way orbits it — including our Sun at 220 km/s.",
                    "Star S2 passed within 120 AU at 3% of light speed to test Einstein.",
                    "First imaged: 12 May 2022 (EHT).",
                    "Nobel Prize 2020: Genzel & Ghez for its discovery via stellar orbits."
                }
            }));

        list.Add(BlackHole("m87", "M87*", "Black Hole", "First black hole ever photographed",
            massSolar: 6.5e9, ly: 5.35e7, angle: 95, visR: 23.5, tilt: 17, jets: true,
            info: new CelestialInfo
            {
                Type = "Supermassive black hole (Messier 87)",
                MassText = "6.5 billion M☉",
                SchwarzschildText = "≈ 1.9 × 10^10 km (≈ 127 AU)",
                DistanceText = "53.5 million light-years",
                DiscoveryText = "First BH image — Event Horizon Telescope, 10 April 2019",
                Facts = new[]
                {
                    "The famous 'orange donut' image — light bent around the photon ring.",
                    "Launches a 5,000-light-year-long relativistic jet.",
                    "Shadow is ~42 microarcseconds across — like photographing a coin on the Moon."
                }
            }));

        list.Add(BlackHole("ton618", "TON 618", "Black Hole", "Ultramassive quasar engine · 66 billion suns",
            massSolar: 6.6e10, ly: 1.04e10, angle: 165, visR: 27, tilt: 8, jets: true,
            info: new CelestialInfo
            {
                Type = "Ultramassive black hole (hyper-luminous quasar)",
                MassText = "66 billion M☉ — one of the most massive known",
                SchwarzschildText = "≈ 1.95 × 10^11 km (≈ 1,300 AU — 40× Pluto's orbit)",
                DistanceText = "10.8 billion light-years (z = 2.219)",
                LuminosityText = "≈ 140 trillion L☉ (quasar)",
                Facts = new[]
                {
                    "Its event horizon would swallow the entire Solar System 40 times over.",
                    "Light bends around it for weeks — its shadow is months wide.",
                    "Powers a Lyman-alpha nebula 100,000+ light-years wide.",
                    "Fun fact: despite the nickname some give it, TON 618 is NOT a stellar-mass black hole — it is ultramassive, millions of times heavier than any star."
                }
            }));

        list.Add(BlackHole("phoenix-a", "Phoenix A", "Black Hole", "Giant of the Phoenix Cluster",
            massSolar: 1e11, ly: 5.7e9, angle: 250, visR: 28, tilt: 30, jets: true,
            info: new CelestialInfo
            {
                Type = "Ultramassive black hole (estimated)",
                MassText = "≈ 100 billion M☉ (uncertain estimate)",
                SchwarzschildText = "≈ 2.95 × 10^11 km (≈ 1,970 AU)",
                DistanceText = "5.7 billion light-years",
                Facts = new[]
                {
                    "If confirmed, among the largest black holes ever estimated.",
                    "Host cluster's gas would otherwise cool into stars — the jet prevents it."
                }
            }));

        list.Add(BlackHole("cygx1", "Cygnus X-1", "Black Hole", "The first black hole confirmed",
            massSolar: 21.2, ly: 7240, angle: 285, visR: 5.4, tilt: 48, jets: true,
            info: new CelestialInfo
            {
                Type = "Stellar-mass black hole (X-ray binary)",
                MassText = "21.2 M☉",
                SchwarzschildText = "62.6 km",
                DistanceText = "7,240 light-years",
                DiscoveryText = "Subject of the famous Hawking–Thorne bet (resolved 1990: Thorne won)",
                Facts = new[]
                {
                    "Rips gas from blue supergiant HDE 226868 every 5.6 days.",
                    "Discovered as a powerful X-ray source in 1964.",
                    "First object widely accepted to be a black hole.",
                    "Spins at over 95% of the maximum allowed rate."
                }
            }));

        list.Add(BlackHole("gaiabh1", "Gaia BH1", "Black Hole", "Closest known black hole",
            massSolar: 9.62, ly: 1560, angle: 45, visR: 5.0, tilt: 55, jets: false,
            info: new CelestialInfo
            {
                Type = "Stellar-mass black hole (dormant)",
                MassText = "9.62 M☉",
                SchwarzschildText = "28.4 km",
                DistanceText = "1,560 light-years (closest known)",
                DiscoveryText = "Gaia astrometry, 2022",
                Facts = new[]
                {
                    "Discovered by the wobble it induces in a Sun-like companion star.",
                    "Dormant — no X-rays betray it; found purely by gravity.",
                    "There are likely ~100 million similar black holes in the Milky Way."
                }
            }));

        list.Add(BlackHole("gw150914", "GW150914 Remnant", "Black Hole", "Born from the first gravitational wave",
            massSolar: 62, ly: 1.3e9, angle: 340, visR: 5.6, tilt: 25, jets: false,
            info: new CelestialInfo
            {
                Type = "Stellar-mass black hole (merger remnant)",
                MassText = "62 M☉ (from merging 36 + 29 M☉ holes)",
                SchwarzschildText = "183 km",
                DistanceText = "1.3 billion light-years",
                DiscoveryText = "LIGO detection: 14 September 2015",
                Facts = new[]
                {
                    "The merger radiated 3 M☉c² as gravitational waves — 50× all starlight in the observable universe at that instant.",
                    "Confirmed Einstein's 1915 prediction a century later.",
                    "Final spin: 0.67 of maximum."
                }
            }));

        // ================= NEBULAE =================

        list.Add(Nebula("orion-neb", "Orion Nebula", "Nebula", "Stellar nursery in Orion's sword",
            seed: 5001, colorA: new SKColor(210, 70, 150), colorB: new SKColor(60, 170, 190), ly: 1344, sizeLy: 24, angle: 295, info: new CelestialInfo
            {
                Type = "H II region / emission nebula (M42)",
                DistanceText = "1,344 light-years",
                SizeText = "≈ 24 light-years across",
                AgeText = "≈ 2 million years",
                Facts = new[]
                {
                    "Bright enough to see with the naked eye — the 'middle star' of Orion's sword.",
                    "Home to the Trapezium Cluster of newborn giants.",
                    "Hubble & JWST have protoplanetary disks here — solar systems forming now.",
                    "Contains 700+ stars in formation."
                }
            }));

        list.Add(Nebula("eagle-neb", "Eagle Nebula", "Nebula", "Home of the Pillars of Creation",
            seed: 5002, colorA: new SKColor(200, 140, 60), colorB: new SKColor(70, 110, 200), ly: 5700, sizeLy: 70, angle: 350, info: new CelestialInfo
            {
                Type = "Young open cluster + emission nebula (M16)",
                DistanceText = "≈ 5,700 light-years",
                SizeText = "≈ 70 × 55 light-years",
                Facts = new[]
                {
                    "The Pillars of Creation — 4-5 light-year-tall columns of gas and dust.",
                    "Immortalized by Hubble (1995) and re-shot by JWST (2022) in infrared.",
                    "Radiation from nearby giants is evaporating the pillars."
                }
            }));

        list.Add(Nebula("carina-neb", "Carina Nebula", "Nebula", "Cathedral of star birth",
            seed: 5003, colorA: new SKColor(235, 120, 50), colorB: new SKColor(140, 40, 60), ly: 8500, sizeLy: 300, angle: 115, info: new CelestialInfo
            {
                Type = "Emission nebula (NGC 3372)",
                DistanceText = "≈ 8,500 light-years",
                SizeText = "≈ 300 light-years across (4× Orion)",
                Facts = new[]
                {
                    "Home to Eta Carinae and 'Mystic Mountain'.",
                    "JWST's 'Cosmic Cliffs' image was taken here.",
                    "Dozens of O-type monsters sculpt its cavities."
                }
            }));

        list.Add(Nebula("crab-neb", "Crab Nebula", "Nebula", "Remains of SN 1054",
            seed: 5004, colorA: new SKColor(80, 200, 190), colorB: new SKColor(110, 90, 220), ly: 6500, sizeLy: 11, angle: 75, info: new CelestialInfo
            {
                Type = "Supernova remnant + pulsar wind nebula (M1)",
                DistanceText = "≈ 6,500 light-years",
                SizeText = "≈ 11 light-years across, expanding at 1,500 km/s",
                AgeText = "Exploded in AD 1054 — seen by Chinese astronomers",
                Facts = new[]
                {
                    "The Crab Pulsar at its heart spins 30 times per second.",
                    "Recorded as a 'guest star' bright enough for daylight viewing for 23 days.",
                    "Its magnetic field whirls electrons to near light speed."
                }
            }));

        list.Add(Nebula("horsehead-neb", "Horsehead Nebula", "Nebula", "Dark silhouette in Orion",
            seed: 5005, colorA: new SKColor(190, 60, 50), colorB: new SKColor(230, 140, 70), ly: 1375, sizeLy: 3.5, angle: 305, info: new CelestialInfo
            {
                Type = "Dark nebula (Barnard 33)",
                DistanceText = "≈ 1,375 light-years",
                SizeText = "≈ 3.5 light-years tall",
                Facts = new[]
                {
                    "A dense cloud silhouetted against the glow of IC 434.",
                    "Will erode away within ~5 million years."
                }
            }));

        list.Add(Nebula("ring-neb", "Ring Nebula", "Nebula", "A dying sun-like star's shroud",
            seed: 5006, colorA: new SKColor(70, 190, 170), colorB: new SKColor(150, 90, 220), ly: 2570, sizeLy: 1, angle: 145, info: new CelestialInfo
            {
                Type = "Planetary nebula (M57)",
                DistanceText = "≈ 2,570 light-years",
                SizeText = "≈ 1 light-year across",
                Facts = new[]
                {
                    "The exposed core of a dead star glows at its center — a future white dwarf.",
                    "Our Sun will create something like this in ~5 billion years.",
                    "'Planetary' is a misnomer from 18th-century telescopes."
                }
            }));

        list.Add(Nebula("helix-neb", "Helix Nebula", "Nebula", "The Eye of God",
            seed: 5007, colorA: new SKColor(60, 180, 200), colorB: new SKColor(190, 70, 150), ly: 655, sizeLy: 2.9, angle: 235, info: new CelestialInfo
            {
                Type = "Planetary nebula (NGC 7293)",
                DistanceText = "≈ 655 light-years (closest planetary nebula)",
                SizeText = "≈ 2.9 light-years across",
                Facts = new[]
                {
                    "Comets knots: gas globules pointing radially toward the central star.",
                    "A preview of the Sun's far future."
                }
            }));

        // ================= GALAXIES =================

        list.Add(Galaxy("milkyway", "Milky Way", "Galaxy", "Our home galaxy · barred spiral",
            seed: 6001, arms: 4, visR: 58, ly: 0, incl: 62, angle: 90, info: new CelestialInfo
            {
                Type = "SBbc barred spiral galaxy",
                MassText = "≈ 1.5 trillion M☉ (incl. dark matter)",
                SizeText = "105,700 light-years across",
                StarCountText = "100–400 billion stars",
                AgeText = "13.6 billion years",
                DistanceText = "You are here — 26,000 ly from the center",
                Facts = new[]
                {
                    "100–400 billion stars; the Sun is one of them.",
                    "The Sun orbits the center at 220 km/s — one lap every 230 million years.",
                    "Will merge with Andromeda in ≈ 4.5 billion years ('Milkomeda').",
                    "Every star you can see with naked eyes belongs to it."
                }
            }));

        list.Add(Galaxy("andromeda", "Andromeda Galaxy", "Galaxy", "M31 · inbound at 110 km/s",
            seed: 6002, arms: 2, visR: 64, ly: 2.537e6, incl: 77, angle: 15, info: new CelestialInfo
            {
                Type = "SA(s)b spiral galaxy",
                MassText = "≈ 1.2–1.5 trillion M☉",
                SizeText = "≈ 152,000 light-years across",
                DistanceText = "2.537 million light-years (farthest naked-eye object)",
                StarCountText = "≈ 1 trillion stars",
                Facts = new[]
                {
                    "Approaching us at 110 km/s — direct collision in ~4.5 billion years.",
                    "Contains ~1 trillion stars, twice the Milky Way.",
                    "One trillion solar luminosities."
                }
            }));

        list.Add(Galaxy("triangulum", "Triangulum Galaxy", "Galaxy", "M33 · Local Group's third giant",
            seed: 6003, arms: 3, visR: 52, ly: 2.73e6, incl: 56, angle: 35, info: new CelestialInfo
            {
                Type = "SA(s)cd spiral galaxy",
                SizeText = "≈ 61,000 light-years across",
                DistanceText = "2.73 million light-years",
                StarCountText = "≈ 40 billion stars",
                Facts = new[] { "May be a distant satellite of Andromeda; largest known H II region NGC 604." }
            }));

        list.Add(Galaxy("sombrero", "Sombrero Galaxy", "Galaxy", "M104 · brimmed with dust",
            seed: 6004, arms: 2, visR: 50, ly: 2.93e7, incl: 84, angle: 55, info: new CelestialInfo
            {
                Type = "SA(s)a spiral galaxy (elliptical-like bulge)",
                SizeText = "≈ 49,000 light-years across",
                DistanceText = "≈ 29.3 million light-years",
                Facts = new[] { "Iconic dust lane; central SMBH of ~1 billion M☉; ~2,000 globular clusters." }
            }));

        list.Add(Galaxy("whirlpool", "Whirlpool Galaxy", "Galaxy", "M51 · grand-design spiral",
            seed: 6005, arms: 2, visR: 54, ly: 2.3e7, incl: 62, angle: 200, info: new CelestialInfo
            {
                Type = "Sc spiral galaxy (interacting pair with NGC 5195)",
                SizeText = "≈ 76,000 light-years across",
                DistanceText = "≈ 23 million light-years",
                Facts = new[] { "First galaxy recognized as a spiral (Lord Rosse, 1845); its companion sparks its arms." }
            }));

        list.Add(Galaxy("lmc", "Large Magellanic Cloud", "Galaxy", "Satellite with the Tarantula",
            seed: 6006, arms: 1, visR: 44, ly: 1.63e5, incl: 66, angle: 270, info: new CelestialInfo
            {
                Type = "Barred irregular satellite galaxy",
                SizeText = "≈ 32,200 light-years across",
                DistanceText = "163,000 light-years",
                Facts = new[]
                {
                    "Hosts the Tarantula Nebula — the most violent starburst in the Local Group.",
                    "Supernova 1987A exploded here — closest in 400 years."
                }
            }));

        list.Add(Galaxy("smc", "Small Magellanic Cloud", "Galaxy", "Dwarf companion",
            seed: 6007, arms: 1, visR: 38, ly: 2.0e5, incl: 58, angle: 285, info: new CelestialInfo
            {
                Type = "Irregular dwarf galaxy",
                SizeText = "≈ 18,900 light-years across",
                DistanceText = "≈ 200,000 light-years",
                Facts = new[] { "Tidally stretched; linked to the LMC by the Magellanic Stream of gas." }
            }));

        list.Add(Galaxy("gnz11", "GN-z11", "Galaxy", "Farthest known galaxy · z = 10.96",
            seed: 6008, arms: 2, visR: 10, ly: 1.339e10, incl: 70, angle: 110, info: new CelestialInfo
            {
                Type = "High-redshift galaxy (JWST spectroscopy)",
                SizeText = "≈ 3,400 light-years across (tiny!)",
                DistanceText = "13.39 billion light-years lookback (≈ 32 billion ly comoving)",
                AgeText = "Seen just 430 million years after the Big Bang",
                Facts = new[]
                {
                    "Formed when the universe was 3% of its current age.",
                    "Hosts an unexpectedly bright AGN — supermassive black hole growing fast."
                }
            }));

        // ================= EXOPLANETS (orbit their stars) =================

        AttachExoplanet(list, "trappist1", "trappist1e", "TRAPPIST-1 e", "Rocky world in the habitable zone",
            visR: 0.62, orbitUnits: 5.2, periodDays: 6.099,
            new CelestialInfo
            {
                Type = "Terrestrial exoplanet (TRAPPIST-1 system)",
                MassText = "0.692 M⊕",
                RadiusText = "0.920 R⊕",
                TempText = "≈ 251 K equilibrium (−22 °C)",
                YearText = "6.1 days",
                DistanceText = "40.7 light-years",
                Facts = new[]
                {
                    "One of 3 habitable-zone Earth-sized worlds around TRAPPIST-1.",
                    "Possibly tidally locked — one eternal day, one eternal night.",
                    "JWST is searching its atmosphere for CO₂ and water."
                }
            });

        AttachExoplanet(list, "proxima", "proximab", "Proxima b", "Nearest exoplanet to Earth",
            visR: 0.75, orbitUnits: 4.6, periodDays: 11.186,
            new CelestialInfo
            {
                Type = "Super-Earth in habitable zone",
                MassText = "≥ 1.07 M⊕",
                TempText = "≈ 234 K equilibrium — habitable if air-cocooned",
                YearText = "11.19 days",
                DistanceText = "4.2465 light-years — closest exoplanet known",
                Facts = new[]
                {
                    "Receives ~65% of Earth's sunlight; possible liquid water with atmosphere.",
                    "Blasted by flares — atmosphere may have been stripped.",
                    "Target for Breakthrough Starshot laser-sail probes."
                }
            });

        AttachExoplanet(list, "proxima", "proximac", "Proxima c", "Cold super-Earth",
            visR: 0.9, orbitUnits: 7.4, periodDays: 1829,
            new CelestialInfo
            {
                Type = "Super-Earth / mini-Neptune candidate",
                MassText = "≈ 7 M⊕",
                YearText = "5.0 years",
                Facts = new[] { "Far colder; detected via astrometry." }
            });

        return list;
    }

    // ---------- factories ----------

    private static StarObject Star(string id, string name, string category, string tag,
        string spectral, SKColor color, double visR, double ly, double angle, CelestialInfo info)
        => new()
        {
            Id = id, Name = name, Category = category, EmTag = tag,
            StarColor = color, VisualRadius = visR,
            Position = MapPos(ly, angle),
            Info = CloneInfo(info, i => { i.SpectralText = spectral; })
        };

    private static NebulaObject Nebula(string id, string name, string category, string tag,
        int seed, SKColor colorA, SKColor colorB, double ly, double sizeLy, double angle, CelestialInfo info)
        => new()
        {
            Id = id, Name = name, Category = category, EmTag = tag,
            Seed = seed, ColorA = colorA, ColorB = colorB,
            VisualRadius = Math.Max(9, 10 * Math.Log10(1 + sizeLy) * 2.6),
            Position = MapPos(ly, angle),
            Info = info
        };

    private static GalaxyObject Galaxy(string id, string name, string category, string tag,
        int seed, int arms, double visR, double ly, double incl, double angle, CelestialInfo info)
    {
        var g = new GalaxyObject
        {
            Id = id, Name = name, Category = category, EmTag = tag,
            VisualRadius = visR,
            Position = MapPos(ly, angle),
            Particles = Effects.MakeSpiralGalaxy(seed, count: 2600, arms: arms),
            Info = info
        };
        g.Particles.InclinationDeg = incl;
        return g;
    }

    private static BlackHoleObject BlackHole(string id, string name, string category, string tag,
        double massSolar, double ly, double angle, double visR, double tilt, bool jets, CelestialInfo info)
        => new()
        {
            Id = id, Name = name, Category = "Black Hole", EmTag = tag,
            MassSolarMasses = massSolar,
            VisualRadius = visR,
            Position = MapPos(ly, angle),
            DiskTiltDeg = tilt, Jets = jets,
            Info = info
        };

    private static void AttachExoplanet(List<SpaceObject> list, string parentStarId, string id, string name,
        string tag, double visR, double orbitUnits, double periodDays, CelestialInfo info)
    {
        var parent = list.FirstOrDefault(o => o.Id == parentStarId) as StarObject;
        if (parent == null) return;
        if (info.TempText.Length == 0) info.TempText = "see star irradiation";
        var exo = new PlanetObject
        {
            Id = id, Name = name, Category = "Exoplanet", EmTag = tag,
            TextureKind = BodyTextureKind.Exo,
            VisualRadius = visR,
            Orbit = new KeplerOrbit(a: orbitUnits, periodDays: periodDays),
            OrbitCenterStar = parent,
            LightSourceOverride = parent.Position,
            SpinPeriodDays = Math.Min(periodDays, 10),
            Info = info
        };
        list.Add(exo);
    }

    private static CelestialInfo CloneInfo(CelestialInfo src, Action<CelestialInfo> mutate)
    {
        var c = new CelestialInfo
        {
            Type = src.Type, MassText = src.MassText, RadiusText = src.RadiusText,
            TempText = src.TempText, DistanceText = src.DistanceText, SpectralText = src.SpectralText,
            GravityText = src.GravityText, EscapeVelText = src.EscapeVelText, DayText = src.DayText,
            YearText = src.YearText, MoonsText = src.MoonsText, AtmosphereText = src.AtmosphereText,
            CompositionText = src.CompositionText, LuminosityText = src.LuminosityText,
            AgeText = src.AgeText, DiscoveryText = src.DiscoveryText,
            SchwarzschildText = src.SchwarzschildText, Facts = src.Facts
        };
        mutate(c);
        return c;
    }
}
