using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace PixelSpark;

public static class ProjectIO
{
    public static void SaveProject(Project project, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var data = new ProjectData
        {
            FrameWidth = project.FrameWidth,
            FrameHeight = project.FrameHeight,
            ActiveIndex = project.ActiveIndex,
            Sprites = new List<SpriteData>()
        };

        foreach (var sprite in project.Sprites)
        {
            var canvas = sprite.Canvas;
            var pixels = new byte[canvas.Width * canvas.Height * 4];

            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    int offset = (y * canvas.Width + x) * 4;
                    Color? c = canvas.GetPixel(x, y);
                    if (c.HasValue)
                    {
                        pixels[offset] = c.Value.R;
                        pixels[offset + 1] = c.Value.G;
                        pixels[offset + 2] = c.Value.B;
                        pixels[offset + 3] = c.Value.A;
                    }
                    // null pixels stay as 0,0,0,0
                }
            }

            data.Sprites.Add(new SpriteData
            {
                Name = sprite.Name,
                Width = canvas.Width,
                Height = canvas.Height,
                Pixels = Convert.ToBase64String(pixels)
            });
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(path, json);
    }

    public static Project LoadProject(string path)
    {
        string json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<ProjectData>(json);

        var sprites = new List<Sprite>();
        foreach (var sd in data.Sprites)
        {
            var canvas = new Canvas(sd.Width, sd.Height);
            byte[] pixels = Convert.FromBase64String(sd.Pixels);

            for (int y = 0; y < sd.Height; y++)
            {
                for (int x = 0; x < sd.Width; x++)
                {
                    int offset = (y * sd.Width + x) * 4;
                    byte r = pixels[offset];
                    byte g = pixels[offset + 1];
                    byte b = pixels[offset + 2];
                    byte a = pixels[offset + 3];

                    if (a > 0)
                        canvas.SetPixel(x, y, new Color(r, g, b, a));
                }
            }

            sprites.Add(new Sprite(sd.Name, canvas));
        }

        var project = new Project(data.FrameWidth, data.FrameHeight, sprites);
        project.SetActive(Math.Clamp(data.ActiveIndex, 0, sprites.Count - 1));
        return project;
    }

    private class ProjectData
    {
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public int ActiveIndex { get; set; }
        public List<SpriteData> Sprites { get; set; }
    }

    private class SpriteData
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Pixels { get; set; }
    }
}
