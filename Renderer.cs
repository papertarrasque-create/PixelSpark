using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelSpark;

public class Renderer
{
    private static readonly Color CheckerLight = new(200, 200, 200);
    private static readonly Color CheckerDark = new(160, 160, 160);
    private static readonly Color GridColor = new(80, 80, 80, 128);

    private readonly Texture2D _pixel;

    public Renderer(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void DrawCanvas(SpriteBatch spriteBatch, Canvas canvas, int zoom, Vector2 panOffset, bool showGrid)
    {
        for (int gx = 0; gx < canvas.Width; gx++)
        {
            for (int gy = 0; gy < canvas.Height; gy++)
            {
                int screenX = (int)(gx * zoom + panOffset.X);
                int screenY = (int)(gy * zoom + panOffset.Y);
                var rect = new Rectangle(screenX, screenY, zoom, zoom);

                Color? pixel = canvas.GetPixel(gx, gy);
                if (pixel == null)
                {
                    Color checker = (gx + gy) % 2 == 0 ? CheckerLight : CheckerDark;
                    spriteBatch.Draw(_pixel, rect, checker);
                }
                else
                {
                    spriteBatch.Draw(_pixel, rect, pixel.Value);
                }
            }
        }

        if (showGrid && zoom >= 4)
            DrawGrid(spriteBatch, canvas, zoom, panOffset);
    }

    private void DrawGrid(SpriteBatch spriteBatch, Canvas canvas, int zoom, Vector2 panOffset)
    {
        int ox = (int)panOffset.X;
        int oy = (int)panOffset.Y;
        int totalW = canvas.Width * zoom;
        int totalH = canvas.Height * zoom;

        for (int x = 0; x <= canvas.Width; x++)
            spriteBatch.Draw(_pixel, new Rectangle(ox + x * zoom, oy, 1, totalH), GridColor);

        for (int y = 0; y <= canvas.Height; y++)
            spriteBatch.Draw(_pixel, new Rectangle(ox, oy + y * zoom, totalW, 1), GridColor);
    }
}
