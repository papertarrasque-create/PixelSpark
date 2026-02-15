namespace PixelSpark;

public class Sprite
{
    public string Name { get; set; }
    public Canvas Canvas { get; }
    public ActionHistory History { get; } = new();

    public Sprite(string name, int width, int height)
    {
        Name = name;
        Canvas = new Canvas(width, height);
    }

    public Sprite(string name, Canvas canvas)
    {
        Name = name;
        Canvas = canvas;
    }
}
