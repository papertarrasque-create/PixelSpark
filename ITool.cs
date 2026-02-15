using Microsoft.Xna.Framework;

namespace PixelSpark;

public interface ITool
{
    string Name { get; }

    /// <summary>
    /// Begin a stroke. Returns the in-progress action that accumulates changes.
    /// </summary>
    PixelAction OnPress(Canvas canvas, int x, int y, Color color);

    /// <summary>
    /// Continue a stroke. Adds changes to the existing action.
    /// </summary>
    void OnDrag(Canvas canvas, int x, int y, Color color, PixelAction action);

    /// <summary>
    /// End a stroke. The caller commits the action to history.
    /// </summary>
    void OnRelease() { }
}
