using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PixelSpark;

/// <summary>
/// A single pixel change — enough data to undo and redo.
/// </summary>
public readonly record struct PixelChange(int X, int Y, Color? OldColor, Color? NewColor);

/// <summary>
/// A compound action: one logical edit (e.g. a full drag stroke)
/// made up of individual pixel changes.
/// </summary>
public class PixelAction
{
    private readonly List<PixelChange> _changes = new();

    public IReadOnlyList<PixelChange> Changes => _changes;
    public bool IsEmpty => _changes.Count == 0;

    public void Add(PixelChange change)
    {
        _changes.Add(change);
    }

    public void Apply(Canvas canvas)
    {
        foreach (var c in _changes)
            canvas.SetPixel(c.X, c.Y, c.NewColor);
    }

    public void Undo(Canvas canvas)
    {
        // Walk backwards so overlapping pixels restore correctly
        for (int i = _changes.Count - 1; i >= 0; i--)
        {
            var c = _changes[i];
            canvas.SetPixel(c.X, c.Y, c.OldColor);
        }
    }
}
