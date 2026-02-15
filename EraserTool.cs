using Microsoft.Xna.Framework;

namespace PixelSpark;

public class EraserTool : ITool
{
    public string Name => "Eraser";

    public PixelAction OnPress(Canvas canvas, int x, int y, Color color)
    {
        var action = new PixelAction();
        ApplyErase(canvas, x, y, action);
        return action;
    }

    public void OnDrag(Canvas canvas, int x, int y, Color color, PixelAction action)
    {
        ApplyErase(canvas, x, y, action);
    }

    private static void ApplyErase(Canvas canvas, int x, int y, PixelAction action)
    {
        Color? old = canvas.GetPixel(x, y);
        if (old == null) return; // already transparent
        action.Add(new PixelChange(x, y, old, null));
        canvas.SetPixel(x, y, null);
    }
}
