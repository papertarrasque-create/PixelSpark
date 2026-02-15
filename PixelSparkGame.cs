using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PixelSpark;

public class PixelSparkGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private Renderer _renderer;

    private Canvas _canvas;
    private ITool _activeTool;
    private readonly PencilTool _pencilTool = new();
    private readonly EraserTool _eraserTool = new();

    private Color _activeColor = Color.White;
    private int _zoom = 16;
    private Vector2 _panOffset;
    private bool _showGrid = true;

    private MouseState _prevMouse;
    private KeyboardState _prevKeyboard;
    private bool _isDrawing;
    private Point _lastGridPos = new(-1, -1);
    private bool _isPanning;
    private Point _panStart;
    private Vector2 _panOffsetStart;

    private static readonly int[] ZoomLevels = { 1, 2, 4, 8, 16, 24, 32, 48, 64 };
    private int _zoomIndex = 4; // starts at 16x

    private const int PanSpeed = 10;

    public PixelSparkGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();
        Window.Title = "PixelSpark";

        _canvas = new Canvas(32, 32);
        _activeTool = _pencilTool;
        _zoom = ZoomLevels[_zoomIndex];
        CenterCanvas();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("Font");
        _renderer = new Renderer(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();

        HandleKeyboard(keyboard);
        HandleMouse(mouse);

        _prevKeyboard = keyboard;
        _prevMouse = mouse;
        base.Update(gameTime);
    }

    private void HandleKeyboard(KeyboardState keyboard)
    {
        // Tool switching
        if (KeyPressed(keyboard, Keys.B)) _activeTool = _pencilTool;
        if (KeyPressed(keyboard, Keys.E)) _activeTool = _eraserTool;

        // Grid toggle
        if (KeyPressed(keyboard, Keys.G)) _showGrid = !_showGrid;

        // Zoom
        if (KeyPressed(keyboard, Keys.OemPlus) || KeyPressed(keyboard, Keys.Add))
            AdjustZoom(1);
        if (KeyPressed(keyboard, Keys.OemMinus) || KeyPressed(keyboard, Keys.Subtract))
            AdjustZoom(-1);

        // Pan with arrow keys
        if (keyboard.IsKeyDown(Keys.Left)) _panOffset.X += PanSpeed;
        if (keyboard.IsKeyDown(Keys.Right)) _panOffset.X -= PanSpeed;
        if (keyboard.IsKeyDown(Keys.Up)) _panOffset.Y += PanSpeed;
        if (keyboard.IsKeyDown(Keys.Down)) _panOffset.Y -= PanSpeed;
    }

    private void AdjustZoom(int direction)
    {
        int newIndex = Math.Clamp(_zoomIndex + direction, 0, ZoomLevels.Length - 1);
        if (newIndex == _zoomIndex) return;

        // Keep canvas visually centered during zoom
        float canvasCenterX = _panOffset.X + _canvas.Width * _zoom / 2f;
        float canvasCenterY = _panOffset.Y + _canvas.Height * _zoom / 2f;

        _zoomIndex = newIndex;
        _zoom = ZoomLevels[_zoomIndex];

        _panOffset.X = canvasCenterX - _canvas.Width * _zoom / 2f;
        _panOffset.Y = canvasCenterY - _canvas.Height * _zoom / 2f;
    }

    private void HandleMouse(MouseState mouse)
    {
        // Scroll wheel zoom
        int scrollDelta = mouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
        if (scrollDelta != 0)
            AdjustZoom(scrollDelta > 0 ? 1 : -1);

        // Middle mouse pan
        if (mouse.MiddleButton == ButtonState.Pressed)
        {
            if (!_isPanning)
            {
                _isPanning = true;
                _panStart = new Point(mouse.X, mouse.Y);
                _panOffsetStart = _panOffset;
            }
            else
            {
                _panOffset = new Vector2(
                    _panOffsetStart.X + (mouse.X - _panStart.X),
                    _panOffsetStart.Y + (mouse.Y - _panStart.Y));
            }
        }
        else
        {
            _isPanning = false;
        }

        // Left mouse drawing
        Point gridPos = ScreenToGrid(mouse.X, mouse.Y);

        if (mouse.LeftButton == ButtonState.Pressed && !_isPanning)
        {
            if (!_isDrawing)
            {
                _isDrawing = true;
                if (_canvas.InBounds(gridPos.X, gridPos.Y))
                {
                    _activeTool.OnPress(_canvas, gridPos.X, gridPos.Y, _activeColor);
                    _lastGridPos = gridPos;
                }
            }
            else if (gridPos != _lastGridPos && _lastGridPos.X >= 0)
            {
                foreach (var p in BresenhamLine(_lastGridPos.X, _lastGridPos.Y, gridPos.X, gridPos.Y))
                {
                    if (_canvas.InBounds(p.X, p.Y))
                        _activeTool.OnDrag(_canvas, p.X, p.Y, _activeColor);
                }
                _lastGridPos = gridPos;
            }
            else if (_lastGridPos.X < 0 && _canvas.InBounds(gridPos.X, gridPos.Y))
            {
                _activeTool.OnDrag(_canvas, gridPos.X, gridPos.Y, _activeColor);
                _lastGridPos = gridPos;
            }
        }
        else if (_isDrawing)
        {
            _activeTool.OnRelease(_canvas);
            _isDrawing = false;
            _lastGridPos = new Point(-1, -1);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(30, 30, 30));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _renderer.DrawCanvas(_spriteBatch, _canvas, _zoom, _panOffset, _showGrid);

        // Top bar
        string topBar = $"[{_activeTool.Name}]  Zoom: {_zoom}x  Grid: {(_showGrid ? "ON" : "OFF")}";
        _spriteBatch.DrawString(_font, topBar, new Vector2(8, 6), Color.White);

        // Status bar
        var mouseState = Mouse.GetState();
        var gridPos = ScreenToGrid(mouseState.X, mouseState.Y);
        string status = $"{_canvas.Width}x{_canvas.Height}";
        if (_canvas.InBounds(gridPos.X, gridPos.Y))
            status += $"  ({gridPos.X}, {gridPos.Y})";
        float statusY = _graphics.PreferredBackBufferHeight - _font.LineSpacing - 6;
        _spriteBatch.DrawString(_font, status, new Vector2(8, statusY), Color.Gray);

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void CenterCanvas()
    {
        _panOffset = new Vector2(
            (_graphics.PreferredBackBufferWidth - _canvas.Width * _zoom) / 2f,
            (_graphics.PreferredBackBufferHeight - _canvas.Height * _zoom) / 2f);
    }

    private Point ScreenToGrid(int screenX, int screenY)
    {
        int gx = (int)Math.Floor((screenX - _panOffset.X) / _zoom);
        int gy = (int)Math.Floor((screenY - _panOffset.Y) / _zoom);
        return new Point(gx, gy);
    }

    private bool KeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key);
    }

    private static IEnumerable<Point> BresenhamLine(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            yield return new Point(x0, y0);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }
}
