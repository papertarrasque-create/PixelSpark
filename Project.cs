using System;
using System.Collections.Generic;

namespace PixelSpark;

public class Project
{
    private readonly List<Sprite> _sprites = new();
    private int _activeIndex;

    public IReadOnlyList<Sprite> Sprites => _sprites;
    public int ActiveIndex => _activeIndex;
    public Sprite ActiveSprite => _sprites[_activeIndex];

    public int FrameWidth { get; }
    public int FrameHeight { get; }

    public Project(int frameWidth, int frameHeight)
    {
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        _sprites.Add(new Sprite("Sprite 1", frameWidth, frameHeight));
        _activeIndex = 0;
    }

    public Project(int frameWidth, int frameHeight, IEnumerable<Sprite> sprites)
    {
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        _sprites.AddRange(sprites);
        _activeIndex = 0;
    }

    public void SetActive(int index)
    {
        if (index >= 0 && index < _sprites.Count)
            _activeIndex = index;
    }

    public void AddSprite(string name)
    {
        _sprites.Add(new Sprite(name, FrameWidth, FrameHeight));
        _activeIndex = _sprites.Count - 1;
    }

    public void AddSprite(Sprite sprite)
    {
        _sprites.Add(sprite);
        _activeIndex = _sprites.Count - 1;
    }

    public void RemoveSprite(int index)
    {
        if (_sprites.Count <= 1) return;
        _sprites.RemoveAt(index);
        if (_activeIndex >= _sprites.Count)
            _activeIndex = _sprites.Count - 1;
    }

    public void RenameSprite(int index, string newName)
    {
        if (index >= 0 && index < _sprites.Count)
            _sprites[index].Name = newName;
    }

    public void ReplaceSprite(int index, Sprite sprite)
    {
        if (index >= 0 && index < _sprites.Count)
            _sprites[index] = sprite;
    }

    public string NextDefaultName()
    {
        return $"Sprite {_sprites.Count + 1}";
    }
}
