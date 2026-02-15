using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelSpark;

public static class FileIO
{
    public static void SaveCanvasAsPng(Canvas canvas, GraphicsDevice graphicsDevice, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var texture = new Texture2D(graphicsDevice, canvas.Width, canvas.Height);
        var pixels = new Color[canvas.Width * canvas.Height];

        for (int y = 0; y < canvas.Height; y++)
        {
            for (int x = 0; x < canvas.Width; x++)
            {
                Color? c = canvas.GetPixel(x, y);
                pixels[y * canvas.Width + x] = c ?? Color.Transparent;
            }
        }

        texture.SetData(pixels);

        using var stream = File.Create(path);
        texture.SaveAsPng(stream, canvas.Width, canvas.Height);
        texture.Dispose();
    }

    public static Canvas LoadCanvasFromPng(GraphicsDevice graphicsDevice, string path)
    {
        using var stream = File.OpenRead(path);
        var texture = Texture2D.FromStream(graphicsDevice, stream);

        var pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);

        var canvas = new Canvas(texture.Width, texture.Height);
        for (int y = 0; y < texture.Height; y++)
        {
            for (int x = 0; x < texture.Width; x++)
            {
                Color c = pixels[y * texture.Width + x];
                canvas.SetPixel(x, y, c.A == 0 ? null : c);
            }
        }

        texture.Dispose();
        return canvas;
    }
}
