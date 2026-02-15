using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelSpark;

public class NewCanvasDialog : IDialog
{
    private static readonly Color OverlayColor = new(0, 0, 0, 160);
    private static readonly Color PanelColor = new(40, 40, 40);
    private static readonly Color PanelBorder = new(100, 100, 100);
    private static readonly Color LabelColor = new(180, 180, 180);
    private static readonly Color HintColor = new(140, 140, 140);
    private static readonly Color ErrorColor = new(255, 100, 100);

    private const int PanelWidth = 340;
    private const int PanelHeight = 160;
    private const int MaxCanvasSize = 512;

    private readonly TextInputField _widthInput;
    private readonly TextInputField _heightInput;
    private int _activeField; // 0 = width, 1 = height
    private string _error;

    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }
    public int ResultWidth { get; private set; }
    public int ResultHeight { get; private set; }

    public NewCanvasDialog(int currentWidth, int currentHeight)
    {
        Func<char, bool> digitsOnly = char.IsAsciiDigit;
        _widthInput = new TextInputField(currentWidth.ToString(), maxLength: 4, charFilter: digitsOnly);
        _heightInput = new TextInputField(currentHeight.ToString(), maxLength: 4, charFilter: digitsOnly);
        _activeField = 0;
        _widthInput.IsFocused = true;
    }

    public void Update(KeyboardState keyboard, KeyboardState prevKeyboard, GameTime gameTime)
    {
        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            IsComplete = true;
            WasCancelled = true;
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Tab))
        {
            _activeField = 1 - _activeField;
            _widthInput.IsFocused = _activeField == 0;
            _heightInput.IsFocused = _activeField == 1;
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Enter))
        {
            if (TryValidate())
            {
                IsComplete = true;
                WasCancelled = false;
            }
            return;
        }

        var active = _activeField == 0 ? _widthInput : _heightInput;
        if (KeyPressed(keyboard, prevKeyboard, Keys.Back))
            active.HandleKey(Keys.Back);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Delete))
            active.HandleKey(Keys.Delete);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Left))
            active.HandleKey(Keys.Left);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Right))
            active.HandleKey(Keys.Right);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Home))
            active.HandleKey(Keys.Home);
        if (KeyPressed(keyboard, prevKeyboard, Keys.End))
            active.HandleKey(Keys.End);
    }

    public void OnTextInput(char character)
    {
        var active = _activeField == 0 ? _widthInput : _heightInput;
        active.HandleCharacter(character);
        _error = null;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer, int screenWidth, int screenHeight, GameTime gameTime)
    {
        renderer.DrawRect(spriteBatch, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        int px = (screenWidth - PanelWidth) / 2;
        int py = (screenHeight - PanelHeight) / 2;
        var panel = new Rectangle(px, py, PanelWidth, PanelHeight);
        renderer.DrawRect(spriteBatch, panel, PanelColor);
        renderer.DrawRectOutline(spriteBatch, panel, PanelBorder, 1);

        // Title
        spriteBatch.DrawString(font, "New Canvas", new Vector2(px + 10, py + 8), Color.White);

        int fieldTop = py + 8 + font.LineSpacing + 12;
        int labelWidth = (int)font.MeasureString("Height: ").X;
        int inputWidth = PanelWidth - labelWidth - 30;

        // Width field
        spriteBatch.DrawString(font, "Width:", new Vector2(px + 10, fieldTop + 5), LabelColor);
        var widthBounds = new Rectangle(px + 10 + labelWidth, fieldTop, inputWidth, 30);
        _widthInput.Draw(spriteBatch, font, renderer, widthBounds, gameTime);

        // Height field
        int heightFieldTop = fieldTop + 38;
        spriteBatch.DrawString(font, "Height:", new Vector2(px + 10, heightFieldTop + 5), LabelColor);
        var heightBounds = new Rectangle(px + 10 + labelWidth, heightFieldTop, inputWidth, 30);
        _heightInput.Draw(spriteBatch, font, renderer, heightBounds, gameTime);

        // Error message
        if (_error != null)
        {
            spriteBatch.DrawString(font, _error, new Vector2(px + 10, py + PanelHeight - font.LineSpacing * 2 - 8), ErrorColor);
        }

        // Hint
        string hint = "[Tab] Switch    [Enter] Create    [Esc] Cancel";
        var hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint, new Vector2(px + PanelWidth - hintSize.X - 10, py + PanelHeight - font.LineSpacing - 6), HintColor);
    }

    private bool TryValidate()
    {
        if (!int.TryParse(_widthInput.Text, out int w) || w < 1 || w > MaxCanvasSize)
        {
            _error = $"Width must be 1-{MaxCanvasSize}";
            return false;
        }
        if (!int.TryParse(_heightInput.Text, out int h) || h < 1 || h > MaxCanvasSize)
        {
            _error = $"Height must be 1-{MaxCanvasSize}";
            return false;
        }
        ResultWidth = w;
        ResultHeight = h;
        return true;
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key)
    {
        return current.IsKeyDown(key) && prev.IsKeyUp(key);
    }
}
