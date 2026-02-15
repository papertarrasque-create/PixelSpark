using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelSpark;

public class LineTool : ITool
{
    public string Name => "Line";
    public bool InterpolateDrag => false;

    private int _startX, _startY;
    private int _endX, _endY;
    private Color _color;
    private bool _isActive;

    public PixelAction OnPress(Canvas canvas, int x, int y, Color color)
    {
        _startX = x;
        _startY = y;
        _endX = x;
        _endY = y;
        _color = color;
        _isActive = true;
        return new PixelAction();
    }

    public void OnDrag(Canvas canvas, int x, int y, Color color, PixelAction action)
    {
        _endX = x;
        _endY = y;
        _color = color;
    }

    public void OnRelease(Canvas canvas, PixelAction action)
    {
        _isActive = false;
        foreach (var p in PixelMath.BresenhamLine(_startX, _startY, _endX, _endY))
        {
            if (!canvas.InBounds(p.X, p.Y)) continue;
            Color? old = canvas.GetPixel(p.X, p.Y);
            if (old.HasValue && old.Value == _color) continue;
            action.Add(new PixelChange(p.X, p.Y, old, _color));
            canvas.SetPixel(p.X, p.Y, _color);
        }
    }

    public void DrawPreview(SpriteBatch spriteBatch, Renderer renderer, Canvas canvas,
                            int zoom, Vector2 panOffset)
    {
        if (!_isActive) return;

        foreach (var p in PixelMath.BresenhamLine(_startX, _startY, _endX, _endY))
        {
            if (!canvas.InBounds(p.X, p.Y)) continue;
            int screenX = (int)(p.X * zoom + panOffset.X);
            int screenY = (int)(p.Y * zoom + panOffset.Y);
            renderer.DrawRect(spriteBatch, new Rectangle(screenX, screenY, zoom, zoom), _color * 0.6f);
        }
    }
}
