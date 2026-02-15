using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PixelSpark;

public class PaletteManager
{
    private readonly List<Palette> _palettes = new();
    private int _paletteIndex;
    private int _colorIndex;

    public Palette CurrentPalette => _palettes[_paletteIndex];
    public Color ActiveColor => CurrentPalette.Colors[_colorIndex];
    public int ColorIndex => _colorIndex;
    public int PaletteIndex => _paletteIndex;
    public IReadOnlyList<Palette> Palettes => _palettes;

    public PaletteManager()
    {
        _palettes.Add(Palette.PICO8);
        _palettes.Add(Palette.NES);
        _palettes.Add(Palette.CGA);
        _palettes.Add(Palette.Endesga32);
    }

    public void SelectColor(int index)
    {
        if (index >= 0 && index < CurrentPalette.Colors.Length)
            _colorIndex = index;
    }

    public void NextPalette()
    {
        _paletteIndex = (_paletteIndex + 1) % _palettes.Count;
        _colorIndex = 0;
    }

    public void PrevPalette()
    {
        _paletteIndex = (_paletteIndex - 1 + _palettes.Count) % _palettes.Count;
        _colorIndex = 0;
    }
}

public class Palette
{
    public string Name { get; }
    public Color[] Colors { get; }

    public Palette(string name, Color[] colors)
    {
        Name = name;
        Colors = colors;
    }

    public static readonly Palette PICO8 = new("PICO-8", new[]
    {
        new Color(0, 0, 0),        // black
        new Color(29, 43, 83),     // dark blue
        new Color(126, 37, 83),    // dark purple
        new Color(0, 135, 81),     // dark green
        new Color(171, 82, 54),    // brown
        new Color(95, 87, 79),     // dark grey
        new Color(194, 195, 199),  // light grey
        new Color(255, 241, 232),  // white
        new Color(255, 0, 77),     // red
        new Color(255, 163, 0),    // orange
        new Color(255, 236, 39),   // yellow
        new Color(0, 228, 54),     // green
        new Color(41, 173, 255),   // blue
        new Color(131, 118, 156),  // indigo
        new Color(255, 119, 168),  // pink
        new Color(255, 204, 170),  // peach
    });

    public static readonly Palette NES = new("NES", new[]
    {
        new Color(0, 0, 0),
        new Color(252, 252, 252),
        new Color(188, 188, 188),
        new Color(124, 124, 124),
        new Color(164, 0, 0),
        new Color(228, 0, 88),
        new Color(216, 40, 0),
        new Color(248, 56, 0),
        new Color(228, 92, 16),
        new Color(172, 124, 0),
        new Color(248, 184, 0),
        new Color(248, 216, 120),
        new Color(0, 120, 0),
        new Color(0, 168, 0),
        new Color(0, 168, 68),
        new Color(184, 248, 24),
        new Color(0, 88, 248),
        new Color(0, 120, 248),
        new Color(104, 136, 252),
        new Color(60, 188, 252),
        new Color(152, 120, 248),
        new Color(120, 124, 236),
        new Color(176, 98, 164),
        new Color(248, 120, 88),
    });

    public static readonly Palette CGA = new("CGA", new[]
    {
        new Color(0, 0, 0),        // black
        new Color(0, 0, 170),      // blue
        new Color(0, 170, 0),      // green
        new Color(0, 170, 170),    // cyan
        new Color(170, 0, 0),      // red
        new Color(170, 0, 170),    // magenta
        new Color(170, 85, 0),     // brown
        new Color(170, 170, 170),  // light gray
        new Color(85, 85, 85),     // dark gray
        new Color(85, 85, 255),    // light blue
        new Color(85, 255, 85),    // light green
        new Color(85, 255, 255),   // light cyan
        new Color(255, 85, 85),    // light red
        new Color(255, 85, 255),   // light magenta
        new Color(255, 255, 85),   // yellow
        new Color(255, 255, 255),  // white
    });

    public static readonly Palette Endesga32 = new("Endesga-32", new[]
    {
        new Color(190, 74, 47),
        new Color(215, 118, 67),
        new Color(234, 212, 170),
        new Color(228, 166, 114),
        new Color(184, 111, 80),
        new Color(115, 62, 57),
        new Color(62, 39, 49),
        new Color(162, 38, 51),
        new Color(228, 59, 68),
        new Color(247, 118, 34),
        new Color(254, 174, 52),
        new Color(254, 231, 97),
        new Color(99, 199, 77),
        new Color(62, 137, 72),
        new Color(38, 92, 66),
        new Color(25, 60, 62),
        new Color(18, 78, 137),
        new Color(0, 153, 219),
        new Color(44, 232, 245),
        new Color(192, 203, 220),
        new Color(139, 155, 180),
        new Color(90, 105, 136),
        new Color(58, 68, 102),
        new Color(38, 43, 68),
        new Color(24, 20, 37),
        new Color(255, 0, 68),
        new Color(104, 56, 108),
        new Color(181, 80, 136),
        new Color(246, 117, 122),
        new Color(232, 183, 150),
        new Color(194, 133, 105),
        new Color(143, 86, 59),
    });
}
