using Microsoft.Xna.Framework;

namespace PixelSpark;

public class EraserTool : ITool
{
    public string Name => "Eraser";

    public void OnPress(Canvas canvas, int x, int y, Color color)
    {
        canvas.SetPixel(x, y, null);
    }

    public void OnDrag(Canvas canvas, int x, int y, Color color)
    {
        canvas.SetPixel(x, y, null);
    }

    public void OnRelease(Canvas canvas) { }
}
