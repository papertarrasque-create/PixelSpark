using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelSpark;

public class RectangleTool : ITool
{
    public string Name => _filled ? "Rect Fill" : "Rect";
    public bool InterpolateDrag => false;

    private bool _filled;
    private int _startX, _startY;
    private int _endX, _endY;
    private Color _color;
    private bool _isActive;

    public bool Filled
    {
        get => _filled;
        set => _filled = value;
    }

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
        foreach (var p in GetPixels())
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

        foreach (var p in GetPixels())
        {
            if (!canvas.InBounds(p.X, p.Y)) continue;
            int screenX = (int)(p.X * zoom + panOffset.X);
            int screenY = (int)(p.Y * zoom + panOffset.Y);
            renderer.DrawRect(spriteBatch, new Rectangle(screenX, screenY, zoom, zoom), _color * 0.6f);
        }
    }

    private IEnumerable<Point> GetPixels()
    {
        int minX = Math.Min(_startX, _endX);
        int maxX = Math.Max(_startX, _endX);
        int minY = Math.Min(_startY, _endY);
        int maxY = Math.Max(_startY, _endY);

        if (_filled)
        {
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    yield return new Point(x, y);
        }
        else
        {
            // Top and bottom edges
            for (int x = minX; x <= maxX; x++)
            {
                yield return new Point(x, minY);
                if (maxY != minY)
                    yield return new Point(x, maxY);
            }
            // Left and right edges (excluding corners)
            for (int y = minY + 1; y < maxY; y++)
            {
                yield return new Point(minX, y);
                if (maxX != minX)
                    yield return new Point(maxX, y);
            }
        }
    }
}
