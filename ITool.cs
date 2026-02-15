using Microsoft.Xna.Framework;

namespace PixelSpark;

public interface ITool
{
    string Name { get; }
    void OnPress(Canvas canvas, int x, int y, Color color);
    void OnDrag(Canvas canvas, int x, int y, Color color);
    void OnRelease(Canvas canvas);
}
