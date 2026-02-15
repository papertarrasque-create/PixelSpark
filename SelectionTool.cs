using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelSpark;

public class SelectionTool : ITool
{
    public string Name => "Select";
    public bool InterpolateDrag => false;

    // Selection rectangle in grid coordinates
    private Rectangle? _selectionRect;

    // Floating pixel buffer (lifted from canvas)
    private Color?[,] _floatingPixels;
    private int _floatX, _floatY;

    // Internal action tracking for lift+stamp
    private PixelAction _liftAction;

    // Drag state
    private enum Mode { None, Selecting, Moving }
    private Mode _mode;
    private int _dragStartX, _dragStartY;
    private int _moveOffsetX, _moveOffsetY;

    public bool HasSelection => _selectionRect.HasValue;
    public bool HasFloat => _floatingPixels != null;
    public Rectangle? SelectionRect => _selectionRect;

    public PixelAction OnPress(Canvas canvas, int x, int y, Color color)
    {
        var action = new PixelAction();

        // If we have a float and click inside selection, start moving
        if (_floatingPixels != null && _selectionRect.HasValue && _selectionRect.Value.Contains(x, y))
        {
            _mode = Mode.Moving;
            _dragStartX = x;
            _dragStartY = y;
            _moveOffsetX = x - _floatX;
            _moveOffsetY = y - _floatY;
            return action;
        }

        // If we have a float and click outside, commit it first
        if (_floatingPixels != null)
        {
            StampFloat(canvas, _liftAction ?? new PixelAction());
            // Push the accumulated lift+stamp action
            // We need to return this as the action so it gets pushed to history
            var commitAction = _liftAction;
            _liftAction = null;
            _floatingPixels = null;
            _selectionRect = null;

            if (commitAction != null && !commitAction.IsEmpty)
            {
                // Apply the commit action changes are already on the canvas,
                // but we need to return it so history records it
                action = commitAction;
            }
        }

        // If we have a selection (no float) and click inside, lift pixels
        if (_selectionRect.HasValue && _floatingPixels == null && _selectionRect.Value.Contains(x, y))
        {
            LiftSelection(canvas);
            _mode = Mode.Moving;
            _dragStartX = x;
            _dragStartY = y;
            _moveOffsetX = x - _floatX;
            _moveOffsetY = y - _floatY;
            return action;
        }

        // Otherwise start a new selection
        _selectionRect = null;
        _mode = Mode.Selecting;
        _dragStartX = x;
        _dragStartY = y;
        return action;
    }

    public void OnDrag(Canvas canvas, int x, int y, Color color, PixelAction action)
    {
        if (_mode == Mode.Selecting)
        {
            int minX = Math.Min(_dragStartX, x);
            int minY = Math.Min(_dragStartY, y);
            int maxX = Math.Max(_dragStartX, x);
            int maxY = Math.Max(_dragStartY, y);
            _selectionRect = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
        else if (_mode == Mode.Moving && _floatingPixels != null)
        {
            _floatX = x - _moveOffsetX;
            _floatY = y - _moveOffsetY;
            UpdateSelectionRectFromFloat();
        }
    }

    public void OnRelease(Canvas canvas, PixelAction action)
    {
        if (_mode == Mode.Selecting)
        {
            // Finalize selection rectangle — if it's degenerate, clear it
            if (_selectionRect.HasValue &&
                (_selectionRect.Value.Width < 1 || _selectionRect.Value.Height < 1))
            {
                _selectionRect = null;
            }
        }
        _mode = Mode.None;
    }

    /// <summary>
    /// Commit floating pixels back to the canvas. Returns the combined action
    /// (lift + stamp) for history. Clears all selection state.
    /// </summary>
    public PixelAction CommitFloat(Canvas canvas)
    {
        if (_floatingPixels != null)
        {
            var action = _liftAction ?? new PixelAction();
            StampFloat(canvas, action);
            _liftAction = null;
            _floatingPixels = null;
            _selectionRect = null;
            _mode = Mode.None;
            return action;
        }

        _selectionRect = null;
        _mode = Mode.None;
        return new PixelAction();
    }

    /// <summary>
    /// Clone the floating pixel buffer (for copy).
    /// </summary>
    public Color?[,] CloneFloatingPixels()
    {
        return (Color?[,])_floatingPixels?.Clone();
    }

    /// <summary>
    /// Set up a floating buffer from external data (for paste).
    /// </summary>
    public void SetFloat(Color?[,] pixels, int x, int y)
    {
        _floatingPixels = pixels;
        _floatX = x;
        _floatY = y;
        _liftAction = new PixelAction(); // paste has no lift — it's new pixels
        UpdateSelectionRectFromFloat();
        _mode = Mode.None;
    }

    /// <summary>
    /// Clear selection without committing (for cut after copy).
    /// </summary>
    public void ClearSelection()
    {
        _selectionRect = null;
        _floatingPixels = null;
        _liftAction = null;
        _mode = Mode.None;
    }

    /// <summary>
    /// Discard the float without stamping. Returns the lift action
    /// so the cleared pixels are recorded in history.
    /// </summary>
    public PixelAction DiscardFloat()
    {
        var action = _liftAction ?? new PixelAction();
        _liftAction = null;
        _floatingPixels = null;
        _selectionRect = null;
        _mode = Mode.None;
        return action;
    }

    public void DrawPreview(SpriteBatch spriteBatch, Renderer renderer, Canvas canvas,
                            int zoom, Vector2 panOffset)
    {
        // Draw floating pixels
        if (_floatingPixels != null)
        {
            int fw = _floatingPixels.GetLength(0);
            int fh = _floatingPixels.GetLength(1);
            for (int y = 0; y < fh; y++)
            {
                for (int x = 0; x < fw; x++)
                {
                    Color? pixel = _floatingPixels[x, y];
                    if (pixel == null) continue;
                    int sx = (int)((_floatX + x) * zoom + panOffset.X);
                    int sy = (int)((_floatY + y) * zoom + panOffset.Y);
                    renderer.DrawRect(spriteBatch, new Rectangle(sx, sy, zoom, zoom), pixel.Value);
                }
            }
        }

        // Draw selection rectangle outline
        if (_selectionRect.HasValue)
        {
            var r = _selectionRect.Value;
            int sx = (int)(r.X * zoom + panOffset.X);
            int sy = (int)(r.Y * zoom + panOffset.Y);
            int sw = r.Width * zoom;
            int sh = r.Height * zoom;
            renderer.DrawRectOutline(spriteBatch, new Rectangle(sx, sy, sw, sh),
                                     new Color(255, 255, 255, 180), 1);
        }
    }

    private void LiftSelection(Canvas canvas)
    {
        var r = _selectionRect.Value;
        _floatingPixels = new Color?[r.Width, r.Height];
        _floatX = r.X;
        _floatY = r.Y;
        _liftAction = new PixelAction();

        for (int y = 0; y < r.Height; y++)
        {
            for (int x = 0; x < r.Width; x++)
            {
                int cx = r.X + x;
                int cy = r.Y + y;
                if (!canvas.InBounds(cx, cy)) continue;

                Color? pixel = canvas.GetPixel(cx, cy);
                _floatingPixels[x, y] = pixel;

                if (pixel != null)
                {
                    _liftAction.Add(new PixelChange(cx, cy, pixel, null));
                    canvas.SetPixel(cx, cy, null);
                }
            }
        }
    }

    private void StampFloat(Canvas canvas, PixelAction action)
    {
        if (_floatingPixels == null) return;

        int fw = _floatingPixels.GetLength(0);
        int fh = _floatingPixels.GetLength(1);
        for (int y = 0; y < fh; y++)
        {
            for (int x = 0; x < fw; x++)
            {
                Color? pixel = _floatingPixels[x, y];
                if (pixel == null) continue;
                int cx = _floatX + x;
                int cy = _floatY + y;
                if (!canvas.InBounds(cx, cy)) continue;
                Color? old = canvas.GetPixel(cx, cy);
                if (old.HasValue && old.Value == pixel.Value) continue;
                action.Add(new PixelChange(cx, cy, old, pixel));
                canvas.SetPixel(cx, cy, pixel);
            }
        }
    }

    private void UpdateSelectionRectFromFloat()
    {
        if (_floatingPixels == null) return;
        _selectionRect = new Rectangle(
            _floatX, _floatY,
            _floatingPixels.GetLength(0),
            _floatingPixels.GetLength(1));
    }
}
