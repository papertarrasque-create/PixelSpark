using System;
using System.Collections.Generic;
using System.IO;
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

    private ActionHistory _history = new();
    private PixelAction _currentAction;

    private readonly PaletteManager _palette = new();

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

    // Palette UI layout
    private const int PaletteLeft = 8;
    private const int PaletteTop = 30;
    private const int SwatchSize = 20;
    private const int SwatchPad = 2;
    private const int PaletteColumns = 2;

    // Dialog system
    private IDialog _activeDialog;
    private Action<IDialog> _onDialogComplete;

    // File I/O state
    private string _currentFilePath;
    private string _lastDirectory;
    private string _statusMessage;
    private double _statusTimer;

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
        _lastDirectory = Environment.CurrentDirectory;
        CenterCanvas();

        Window.TextInput += OnTextInput;

        base.Initialize();
    }

    private void OnTextInput(object sender, TextInputEventArgs e)
    {
        _activeDialog?.OnTextInput(e.Character);
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

        // Tick status message timer
        if (_statusMessage != null)
        {
            _statusTimer -= gameTime.ElapsedGameTime.TotalSeconds;
            if (_statusTimer <= 0)
                _statusMessage = null;
        }

        if (_activeDialog != null)
        {
            _activeDialog.Update(keyboard, _prevKeyboard, gameTime);

            if (_activeDialog.IsComplete)
            {
                var dialog = _activeDialog;
                var callback = _onDialogComplete;
                _activeDialog = null;
                _onDialogComplete = null;
                callback?.Invoke(dialog);
            }
        }
        else
        {
            HandleKeyboard(keyboard);
            HandleMouse(mouse);
        }

        _prevKeyboard = keyboard;
        _prevMouse = mouse;
        base.Update(gameTime);
    }

    private void ShowDialog(IDialog dialog, Action<IDialog> onComplete)
    {
        _activeDialog = dialog;
        _onDialogComplete = onComplete;
    }

    private void ShowStatus(string message, double seconds = 2.0)
    {
        _statusMessage = message;
        _statusTimer = seconds;
    }

    private void UpdateTitle()
    {
        if (_currentFilePath != null)
            Window.Title = $"PixelSpark — {Path.GetFileName(_currentFilePath)}";
        else
            Window.Title = "PixelSpark";
    }

    // --- File I/O flows ---

    private void StartSave()
    {
        if (_currentFilePath != null)
        {
            DoSave(_currentFilePath);
            return;
        }
        StartSaveAs();
    }

    private void StartSaveAs()
    {
        string defaultPath = _currentFilePath
            ?? Path.Combine(_lastDirectory, "untitled.png");

        ShowDialog(new InputDialog("Save As:", defaultPath), dialog =>
        {
            var input = (InputDialog)dialog;
            if (input.WasCancelled) return;

            string path = ExpandPath(input.ResultText);
            if (File.Exists(path) && path != _currentFilePath)
            {
                ShowDialog(new ConfirmDialog($"Overwrite {Path.GetFileName(path)}?"), confirm =>
                {
                    if (((ConfirmDialog)confirm).Confirmed)
                        DoSave(path);
                });
            }
            else
            {
                DoSave(path);
            }
        });
    }

    private void DoSave(string path)
    {
        try
        {
            FileIO.SaveCanvasAsPng(_canvas, GraphicsDevice, path);
            _currentFilePath = path;
            _lastDirectory = Path.GetDirectoryName(path);
            UpdateTitle();
            ShowStatus($"Saved: {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            ShowStatus($"Save failed: {ex.Message}", 4.0);
        }
    }

    private void StartLoad()
    {
        string defaultPath = _lastDirectory + Path.DirectorySeparatorChar;

        ShowDialog(new InputDialog("Open File:", defaultPath), dialog =>
        {
            var input = (InputDialog)dialog;
            if (input.WasCancelled) return;

            string path = ExpandPath(input.ResultText);
            try
            {
                _canvas = FileIO.LoadCanvasFromPng(GraphicsDevice, path);
                _history = new ActionHistory();
                _currentFilePath = path;
                _lastDirectory = Path.GetDirectoryName(path);
                CenterCanvas();
                UpdateTitle();
                ShowStatus($"Loaded: {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                ShowStatus($"Load failed: {ex.Message}", 4.0);
            }
        });
    }

    private void StartNewCanvas()
    {
        ShowDialog(new NewCanvasDialog(_canvas.Width, _canvas.Height), dialog =>
        {
            var newDialog = (NewCanvasDialog)dialog;
            if (newDialog.WasCancelled) return;

            _canvas = new Canvas(newDialog.ResultWidth, newDialog.ResultHeight);
            _history = new ActionHistory();
            _currentFilePath = null;
            CenterCanvas();
            UpdateTitle();
            ShowStatus($"New canvas: {newDialog.ResultWidth}x{newDialog.ResultHeight}");
        });
    }

    private static string ExpandPath(string path)
    {
        if (path.StartsWith('~'))
            path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..];
        if (string.IsNullOrEmpty(Path.GetExtension(path)))
            path += ".png";
        return Path.GetFullPath(path);
    }

    // --- Input handling ---

    private void HandleKeyboard(KeyboardState keyboard)
    {
        bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        // File I/O shortcuts
        if (ctrl && shift && KeyPressed(keyboard, Keys.S))
        {
            StartSaveAs();
            return;
        }
        if (ctrl && !shift && KeyPressed(keyboard, Keys.S))
        {
            StartSave();
            return;
        }
        if (ctrl && KeyPressed(keyboard, Keys.O))
        {
            StartLoad();
            return;
        }
        if (ctrl && KeyPressed(keyboard, Keys.N))
        {
            StartNewCanvas();
            return;
        }

        // Undo / Redo (redo check first — Ctrl+Shift+Z includes Ctrl+Z)
        if (ctrl && shift && KeyPressed(keyboard, Keys.Z))
        {
            _history.Redo(_canvas);
            return;
        }
        if (ctrl && !shift && KeyPressed(keyboard, Keys.Z))
        {
            _history.Undo(_canvas);
            return;
        }

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

        // Palette switching
        if (KeyPressed(keyboard, Keys.OemOpenBrackets)) _palette.PrevPalette();
        if (KeyPressed(keyboard, Keys.OemCloseBrackets)) _palette.NextPalette();
    }

    private void AdjustZoom(int direction)
    {
        int newIndex = Math.Clamp(_zoomIndex + direction, 0, ZoomLevels.Length - 1);
        if (newIndex == _zoomIndex) return;

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

        // Left click on palette swatches
        if (mouse.LeftButton == ButtonState.Pressed
            && _prevMouse.LeftButton == ButtonState.Released
            && !_isPanning)
        {
            int swatchIndex = HitTestPalette(mouse.X, mouse.Y);
            if (swatchIndex >= 0)
            {
                _palette.SelectColor(swatchIndex);
                return; // don't start drawing
            }
        }

        // Left mouse drawing
        Point gridPos = ScreenToGrid(mouse.X, mouse.Y);
        Color activeColor = _palette.ActiveColor;

        if (mouse.LeftButton == ButtonState.Pressed && !_isPanning)
        {
            if (!_isDrawing)
            {
                _isDrawing = true;
                if (_canvas.InBounds(gridPos.X, gridPos.Y))
                {
                    _currentAction = _activeTool.OnPress(_canvas, gridPos.X, gridPos.Y, activeColor);
                    _lastGridPos = gridPos;
                }
            }
            else if (gridPos != _lastGridPos && _lastGridPos.X >= 0 && _currentAction != null)
            {
                foreach (var p in BresenhamLine(_lastGridPos.X, _lastGridPos.Y, gridPos.X, gridPos.Y))
                {
                    if (_canvas.InBounds(p.X, p.Y))
                        _activeTool.OnDrag(_canvas, p.X, p.Y, activeColor, _currentAction);
                }
                _lastGridPos = gridPos;
            }
            else if (_lastGridPos.X < 0 && _canvas.InBounds(gridPos.X, gridPos.Y))
            {
                if (_currentAction == null)
                    _currentAction = _activeTool.OnPress(_canvas, gridPos.X, gridPos.Y, activeColor);
                else
                    _activeTool.OnDrag(_canvas, gridPos.X, gridPos.Y, activeColor, _currentAction);
                _lastGridPos = gridPos;
            }
        }
        else if (_isDrawing)
        {
            _activeTool.OnRelease();
            if (_currentAction != null)
            {
                _history.Push(_currentAction);
                _currentAction = null;
            }
            _isDrawing = false;
            _lastGridPos = new Point(-1, -1);
        }
    }

    // --- Drawing ---

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(30, 30, 30));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        _renderer.DrawCanvas(_spriteBatch, _canvas, _zoom, _panOffset, _showGrid);
        DrawPalette();

        // Top bar
        string topBar = $"[{_activeTool.Name}]  Zoom: {_zoom}x  Grid: {(_showGrid ? "ON" : "OFF")}  Palette: {_palette.CurrentPalette.Name}";
        _spriteBatch.DrawString(_font, topBar, new Vector2(8, 6), Color.White);

        // Status bar
        var mouseState = Mouse.GetState();
        var gridPos = ScreenToGrid(mouseState.X, mouseState.Y);
        string status = $"{_canvas.Width}x{_canvas.Height}";
        if (_canvas.InBounds(gridPos.X, gridPos.Y))
            status += $"  ({gridPos.X}, {gridPos.Y})";
        if (_statusMessage != null)
            status += $"  {_statusMessage}";
        else if (_currentFilePath != null)
            status += $"  [{Path.GetFileName(_currentFilePath)}]";
        if (_history.CanUndo) status += "  [Ctrl+Z: Undo]";
        if (_history.CanRedo) status += "  [Ctrl+Shift+Z: Redo]";
        float statusY = _graphics.PreferredBackBufferHeight - _font.LineSpacing - 6;
        _spriteBatch.DrawString(_font, status, new Vector2(8, statusY), Color.Gray);

        // Dialog overlay
        if (_activeDialog != null)
            _activeDialog.Draw(_spriteBatch, _font, _renderer,
                _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, gameTime);

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawPalette()
    {
        var palette = _palette.CurrentPalette;
        for (int i = 0; i < palette.Colors.Length; i++)
        {
            int col = i % PaletteColumns;
            int row = i / PaletteColumns;
            int x = PaletteLeft + col * (SwatchSize + SwatchPad);
            int y = PaletteTop + row * (SwatchSize + SwatchPad);
            var rect = new Rectangle(x, y, SwatchSize, SwatchSize);

            // Draw swatch
            _renderer.DrawRect(_spriteBatch, rect, palette.Colors[i]);

            // Highlight active color
            if (i == _palette.ColorIndex)
                _renderer.DrawRectOutline(_spriteBatch, new Rectangle(x - 2, y - 2, SwatchSize + 4, SwatchSize + 4), Color.White, 2);
        }
    }

    private int HitTestPalette(int screenX, int screenY)
    {
        var palette = _palette.CurrentPalette;
        for (int i = 0; i < palette.Colors.Length; i++)
        {
            int col = i % PaletteColumns;
            int row = i / PaletteColumns;
            int x = PaletteLeft + col * (SwatchSize + SwatchPad);
            int y = PaletteTop + row * (SwatchSize + SwatchPad);

            if (screenX >= x && screenX < x + SwatchSize
                && screenY >= y && screenY < y + SwatchSize)
                return i;
        }
        return -1;
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
