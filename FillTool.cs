using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PixelSpark;

public class FillTool : ITool
{
    public string Name => "Fill";

    public PixelAction OnPress(Canvas canvas, int x, int y, Color color)
    {
        var action = new PixelAction();
        Color? target = canvas.GetPixel(x, y);

        // If target is already the fill color, nothing to do
        if (target.HasValue && target.Value == color) return action;

        var visited = new bool[canvas.Width, canvas.Height];
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((x, y));
        visited[x, y] = true;

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            Color? current = canvas.GetPixel(cx, cy);

            if (!ColorsMatch(current, target)) continue;

            action.Add(new PixelChange(cx, cy, current, color));
            canvas.SetPixel(cx, cy, color);

            TryEnqueue(canvas, cx - 1, cy, visited, queue);
            TryEnqueue(canvas, cx + 1, cy, visited, queue);
            TryEnqueue(canvas, cx, cy - 1, visited, queue);
            TryEnqueue(canvas, cx, cy + 1, visited, queue);
        }

        return action;
    }

    public void OnDrag(Canvas canvas, int x, int y, Color color, PixelAction action)
    {
        // Fill is single-click only
    }

    private static bool ColorsMatch(Color? a, Color? b)
    {
        if (!a.HasValue && !b.HasValue) return true;
        if (!a.HasValue || !b.HasValue) return false;
        return a.Value == b.Value;
    }

    private static void TryEnqueue(Canvas canvas, int x, int y, bool[,] visited, Queue<(int, int)> queue)
    {
        if (!canvas.InBounds(x, y)) return;
        if (visited[x, y]) return;
        visited[x, y] = true;
        queue.Enqueue((x, y));
    }
}
