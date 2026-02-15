using System.Collections.Generic;

namespace PixelSpark;

public class ActionHistory
{
    private readonly Stack<PixelAction> _undoStack = new();
    private readonly Stack<PixelAction> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Record a completed action. Clears the redo stack.
    /// </summary>
    public void Push(PixelAction action)
    {
        if (action.IsEmpty) return;
        _undoStack.Push(action);
        _redoStack.Clear();
    }

    public void Undo(Canvas canvas)
    {
        if (!CanUndo) return;
        var action = _undoStack.Pop();
        action.Undo(canvas);
        _redoStack.Push(action);
    }

    public void Redo(Canvas canvas)
    {
        if (!CanRedo) return;
        var action = _redoStack.Pop();
        action.Apply(canvas);
        _undoStack.Push(action);
    }
}
