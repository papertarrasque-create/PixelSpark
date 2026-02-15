using Microsoft.Xna.Framework;

namespace PixelSpark;

public class PencilTool : ITool
{
    public string Name => "Pencil";

    public PixelAction OnPress(Canvas canvas, int x, int y, Color color)
    {
        var action = new PixelAction();
        ApplyPixel(canvas, x, y, color, action);
        return action;
    }

    public void OnDrag(Canvas canvas, int x, int y, Color color, PixelAction action)
    {
        ApplyPixel(canvas, x, y, color, action);
    }

    private static void ApplyPixel(Canvas canvas, int x, int y, Color color, PixelAction action)
    {
        Color? old = canvas.GetPixel(x, y);
        if (old.HasValue && old.Value == color) return; // no-op
        action.Add(new PixelChange(x, y, old, color));
        canvas.SetPixel(x, y, color);
    }
}
