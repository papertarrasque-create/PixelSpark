using Microsoft.Xna.Framework;

namespace PixelSpark;

public class PencilTool : ITool
{
    public string Name => "Pencil";

    public void OnPress(Canvas canvas, int x, int y, Color color)
    {
        canvas.SetPixel(x, y, color);
    }

    public void OnDrag(Canvas canvas, int x, int y, Color color)
    {
        canvas.SetPixel(x, y, color);
    }

    public void OnRelease(Canvas canvas) { }
}
