using System;
using Microsoft.Xna.Framework;

namespace PixelSpark;

public class EyedropperTool : ITool
{
    public string Name => "Eyedropper";

    private readonly Action<Color> _onColorPicked;

    public EyedropperTool(Action<Color> onColorPicked)
    {
        _onColorPicked = onColorPicked;
    }

    public PixelAction OnPress(Canvas canvas, int x, int y, Color color)
    {
        var action = new PixelAction();
        PickColor(canvas, x, y);
        return action;
    }

    public void OnDrag(Canvas canvas, int x, int y, Color color, PixelAction action)
    {
        PickColor(canvas, x, y);
    }

    private void PickColor(Canvas canvas, int x, int y)
    {
        Color? pixel = canvas.GetPixel(x, y);
        if (pixel.HasValue)
            _onColorPicked(pixel.Value);
    }
}
