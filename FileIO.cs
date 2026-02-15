using System;
using System.Collections.Generic;
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

    public static void ExportSpriteSheet(Project project, GraphicsDevice graphicsDevice, string path, int columns)
    {
        int count = project.Sprites.Count;
        int rows = (int)Math.Ceiling((double)count / columns);
        int sheetWidth = project.FrameWidth * columns;
        int sheetHeight = project.FrameHeight * rows;

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var pixels = new Color[sheetWidth * sheetHeight];
        // Fill with transparent
        Array.Fill(pixels, Color.Transparent);

        for (int i = 0; i < count; i++)
        {
            var canvas = project.Sprites[i].Canvas;
            int col = i % columns;
            int row = i / columns;
            int offsetX = col * project.FrameWidth;
            int offsetY = row * project.FrameHeight;

            int copyW = Math.Min(canvas.Width, project.FrameWidth);
            int copyH = Math.Min(canvas.Height, project.FrameHeight);

            for (int y = 0; y < copyH; y++)
            {
                for (int x = 0; x < copyW; x++)
                {
                    Color? c = canvas.GetPixel(x, y);
                    pixels[(offsetY + y) * sheetWidth + (offsetX + x)] = c ?? Color.Transparent;
                }
            }
        }

        var texture = new Texture2D(graphicsDevice, sheetWidth, sheetHeight);
        texture.SetData(pixels);

        using var stream = File.Create(path);
        texture.SaveAsPng(stream, sheetWidth, sheetHeight);
        texture.Dispose();
    }

    public static List<Sprite> ImportSpriteSheet(
        GraphicsDevice graphicsDevice, string path, int frameWidth, int frameHeight)
    {
        using var stream = File.OpenRead(path);
        var texture = Texture2D.FromStream(graphicsDevice, stream);

        var allPixels = new Color[texture.Width * texture.Height];
        texture.GetData(allPixels);

        int columns = texture.Width / frameWidth;
        int rows = texture.Height / frameHeight;
        var sprites = new List<Sprite>();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                var canvas = new Canvas(frameWidth, frameHeight);
                int offsetX = col * frameWidth;
                int offsetY = row * frameHeight;
                bool hasContent = false;

                for (int y = 0; y < frameHeight; y++)
                {
                    for (int x = 0; x < frameWidth; x++)
                    {
                        Color c = allPixels[(offsetY + y) * texture.Width + (offsetX + x)];
                        if (c.A > 0)
                        {
                            canvas.SetPixel(x, y, c);
                            hasContent = true;
                        }
                    }
                }

                if (hasContent)
                    sprites.Add(new Sprite($"Frame {sprites.Count + 1}", canvas));
            }
        }

        texture.Dispose();
        return sprites;
    }
}
