using Microsoft.Xna.Framework;

namespace PixelSpark;

public class Canvas
{
    public int Width { get; }
    public int Height { get; }
    private readonly Color?[,] _pixels;

    public Canvas(int width, int height)
    {
        Width = width;
        Height = height;
        _pixels = new Color?[width, height];
    }

    public Color? GetPixel(int x, int y)
    {
        return InBounds(x, y) ? _pixels[x, y] : null;
    }

    public void SetPixel(int x, int y, Color? color)
    {
        if (InBounds(x, y))
            _pixels[x, y] = color;
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
}
