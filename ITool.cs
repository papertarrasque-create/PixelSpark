using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelSpark;

public interface ITool
{
    string Name { get; }

    /// <summary>
    /// If true, the game interpolates drag positions via Bresenham.
    /// If false, only the current mouse position is passed to OnDrag.
    /// </summary>
    bool InterpolateDrag => true;

    /// <summary>
    /// Begin a stroke. Returns the in-progress action that accumulates changes.
    /// </summary>
    PixelAction OnPress(Canvas canvas, int x, int y, Color color);

    /// <summary>
    /// Continue a stroke. Adds changes to the existing action.
    /// </summary>
    void OnDrag(Canvas canvas, int x, int y, Color color, PixelAction action);

    /// <summary>
    /// End a stroke. Deferred tools (line, rectangle) apply their pixels here.
    /// The caller commits the action to history.
    /// </summary>
    void OnRelease(Canvas canvas, PixelAction action) { }

    /// <summary>
    /// Draw tool preview overlay (e.g. line preview, rectangle preview).
    /// Called every frame during Draw. Default is no-op.
    /// </summary>
    void DrawPreview(SpriteBatch spriteBatch, Renderer renderer, Canvas canvas,
                     int zoom, Vector2 panOffset) { }
}
