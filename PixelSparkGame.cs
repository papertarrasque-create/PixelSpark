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

    private Project _project;
    private ITool _activeTool;
    private readonly PencilTool _pencilTool = new();
    private readonly EraserTool _eraserTool = new();

    private PixelAction _currentAction;

    private readonly PaletteManager _palette = new();

    // Convenience accessors for the active sprite
    private Canvas Canvas => _project.ActiveSprite.Canvas;
    private ActionHistory History => _project.ActiveSprite.History;

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
    private const int PaletteTop = 52;
    private const int SwatchSize = 20;
    private const int SwatchPad = 2;
    private const int PaletteColumns = 2;

    // Sprite tab bar layout
    private const int TabBarY = 26;
    private const int TabHeight = 20;
    private const int TabPadX = 8;
    private const int TabGap = 2;

    // Dialog system
    private IDialog _activeDialog;
    private Action<IDialog> _onDialogComplete;

    // Sheet preview mode
    private bool _sheetPreview;
    private int _sheetColumns;

    // File I/O state
    private string _projectFilePath;
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

        _project = new Project(32, 32);
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
        if (_projectFilePath != null)
            Window.Title = $"PixelSpark — {Path.GetFileName(_projectFilePath)}";
        else
            Window.Title = "PixelSpark";
    }

    // --- File I/O flows ---

    private void StartSaveProject()
    {
        if (_projectFilePath != null)
        {
            DoSaveProject(_projectFilePath);
            return;
        }
        StartSaveProjectAs();
    }

    private void StartSaveProjectAs()
    {
        string defaultPath = _projectFilePath
            ?? Path.Combine(_lastDirectory, "untitled.pxs");

        ShowDialog(new InputDialog("Save Project:", defaultPath), dialog =>
        {
            var input = (InputDialog)dialog;
            if (input.WasCancelled) return;

            string path = ExpandPath(input.ResultText, ".pxs");
            if (File.Exists(path) && path != _projectFilePath)
            {
                ShowDialog(new ConfirmDialog($"Overwrite {Path.GetFileName(path)}?"), confirm =>
                {
                    if (((ConfirmDialog)confirm).Confirmed)
                        DoSaveProject(path);
                });
            }
            else
            {
                DoSaveProject(path);
            }
        });
    }

    private void DoSaveProject(string path)
    {
        try
        {
            ProjectIO.SaveProject(_project, path);
            _projectFilePath = path;
            _lastDirectory = Path.GetDirectoryName(path);
            UpdateTitle();
            ShowStatus($"Saved: {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            ShowStatus($"Save failed: {ex.Message}", 4.0);
        }
    }

    private void StartLoadProject()
    {
        string defaultPath = _lastDirectory + Path.DirectorySeparatorChar;

        ShowDialog(new InputDialog("Open File:", defaultPath), dialog =>
        {
            var input = (InputDialog)dialog;
            if (input.WasCancelled) return;

            string raw = input.ResultText;
            // Detect extension to decide format
            if (raw.StartsWith('~'))
                raw = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + raw[1..];
            string path = Path.GetFullPath(raw);

            string ext = Path.GetExtension(path).ToLowerInvariant();

            try
            {
                if (ext == ".pxs")
                {
                    _project = ProjectIO.LoadProject(path);
                    _projectFilePath = path;
                }
                else
                {
                    // Treat as PNG — load into a single-sprite project
                    if (string.IsNullOrEmpty(ext))
                        path += ".png";
                    var canvas = FileIO.LoadCanvasFromPng(GraphicsDevice, path);
                    _project = new Project(canvas.Width, canvas.Height);
                    // Replace the default sprite with the loaded canvas
                    _project.ReplaceSprite(0, new Sprite(Path.GetFileNameWithoutExtension(path), canvas));
                    _projectFilePath = null;
                }

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

    private void StartNewProject()
    {
        ShowDialog(new NewCanvasDialog(Canvas.Width, Canvas.Height), dialog =>
        {
            var newDialog = (NewCanvasDialog)dialog;
            if (newDialog.WasCancelled) return;

            _project = new Project(newDialog.ResultWidth, newDialog.ResultHeight);
            _projectFilePath = null;
            CenterCanvas();
            UpdateTitle();
            ShowStatus($"New project: {newDialog.ResultWidth}x{newDialog.ResultHeight}");
        });
    }

    private void StartExportSpriteSheet()
    {
        if (_project.Sprites.Count < 2)
        {
            ShowStatus("Need at least 2 sprites to export a sheet");
            return;
        }

        string defaultPath = Path.Combine(_lastDirectory, "spritesheet.png");
        ShowDialog(new InputDialog("Export Sprite Sheet:", defaultPath), dialog =>
        {
            var input = (InputDialog)dialog;
            if (input.WasCancelled) return;

            string path = ExpandPath(input.ResultText, ".png");
            if (File.Exists(path))
            {
                ShowDialog(new ConfirmDialog($"Overwrite {Path.GetFileName(path)}?"), confirm =>
                {
                    if (((ConfirmDialog)confirm).Confirmed)
                        DoExportSpriteSheet(path);
                });
            }
            else
            {
                DoExportSpriteSheet(path);
            }
        });
    }

    private void DoExportSpriteSheet(string path)
    {
        try
        {
            int columns = Math.Min(_project.Sprites.Count, 8);
            FileIO.ExportSpriteSheet(_project, GraphicsDevice, path, columns);
            _lastDirectory = Path.GetDirectoryName(path);
            ShowStatus($"Exported sheet: {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            ShowStatus($"Export failed: {ex.Message}", 4.0);
        }
    }

    private void StartImportSpriteSheet()
    {
        string defaultPath = _lastDirectory + Path.DirectorySeparatorChar;
        ShowDialog(new InputDialog("Import Sprite Sheet:", defaultPath), dialog =>
        {
            var input = (InputDialog)dialog;
            if (input.WasCancelled) return;

            string path = ExpandPath(input.ResultText, ".png");
            try
            {
                var sprites = FileIO.ImportSpriteSheet(
                    GraphicsDevice, path, _project.FrameWidth, _project.FrameHeight);

                foreach (var sprite in sprites)
                    _project.AddSprite(sprite);

                _lastDirectory = Path.GetDirectoryName(path);
                ShowStatus($"Imported {sprites.Count} frames from {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                ShowStatus($"Import failed: {ex.Message}", 4.0);
            }
        });
    }

    private void StartAddSprite()
    {
        string defaultName = _project.NextDefaultName();
        ShowDialog(new InputDialog("Sprite Name:", defaultName), dialog =>
        {
            var input = (InputDialog)dialog;
            if (input.WasCancelled) return;

            _project.AddSprite(input.ResultText.Trim());
            CenterCanvas();
            ShowStatus($"Added: {_project.ActiveSprite.Name}");
        });
    }

    private void StartRenameSprite()
    {
        ShowDialog(new InputDialog("Rename Sprite:", _project.ActiveSprite.Name), dialog =>
        {
            var input = (InputDialog)dialog;
            if (input.WasCancelled) return;

            string newName = input.ResultText.Trim();
            if (newName.Length > 0)
            {
                _project.RenameSprite(_project.ActiveIndex, newName);
                ShowStatus($"Renamed to: {newName}");
            }
        });
    }

    private void StartRemoveSprite()
    {
        if (_project.Sprites.Count <= 1)
        {
            ShowStatus("Can't remove the last sprite");
            return;
        }

        string name = _project.ActiveSprite.Name;
        ShowDialog(new ConfirmDialog($"Remove \"{name}\"?"), dialog =>
        {
            if (((ConfirmDialog)dialog).Confirmed)
            {
                _project.RemoveSprite(_project.ActiveIndex);
                CenterCanvas();
                ShowStatus($"Removed: {name}");
            }
        });
    }

    private static string ExpandPath(string path, string defaultExtension = ".png")
    {
        if (path.StartsWith('~'))
            path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path[1..];
        if (string.IsNullOrEmpty(Path.GetExtension(path)))
            path += defaultExtension;
        return Path.GetFullPath(path);
    }

    // --- Input handling ---

    private void HandleKeyboard(KeyboardState keyboard)
    {
        bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        // Sprite sheet export: Ctrl+Shift+E
        if (ctrl && shift && KeyPressed(keyboard, Keys.E))
        {
            StartExportSpriteSheet();
            return;
        }

        // File I/O shortcuts
        if (ctrl && shift && KeyPressed(keyboard, Keys.S))
        {
            StartSaveProjectAs();
            return;
        }
        if (ctrl && !shift && KeyPressed(keyboard, Keys.S))
        {
            StartSaveProject();
            return;
        }
        if (ctrl && KeyPressed(keyboard, Keys.O))
        {
            StartLoadProject();
            return;
        }
        if (ctrl && KeyPressed(keyboard, Keys.N))
        {
            StartNewProject();
            return;
        }

        // Sprite management
        if (ctrl && KeyPressed(keyboard, Keys.T))
        {
            StartAddSprite();
            return;
        }
        if (ctrl && KeyPressed(keyboard, Keys.W))
        {
            StartRemoveSprite();
            return;
        }
        if (KeyPressed(keyboard, Keys.F2))
        {
            StartRenameSprite();
            return;
        }

        // Sprite sheet import: Ctrl+I
        if (ctrl && KeyPressed(keyboard, Keys.I))
        {
            StartImportSpriteSheet();
            return;
        }

        // Sprite cycling: Ctrl+Tab / Ctrl+Shift+Tab
        if (ctrl && KeyPressed(keyboard, Keys.Tab))
        {
            FinishDrawing();
            if (shift)
                _project.SetActive((_project.ActiveIndex - 1 + _project.Sprites.Count) % _project.Sprites.Count);
            else
                _project.SetActive((_project.ActiveIndex + 1) % _project.Sprites.Count);
            CenterCanvas();
            return;
        }

        // Undo / Redo (redo check first — Ctrl+Shift+Z includes Ctrl+Z)
        if (ctrl && shift && KeyPressed(keyboard, Keys.Z))
        {
            History.Redo(Canvas);
            return;
        }
        if (ctrl && !shift && KeyPressed(keyboard, Keys.Z))
        {
            History.Undo(Canvas);
            return;
        }

        // Tool switching
        if (KeyPressed(keyboard, Keys.B)) _activeTool = _pencilTool;
        if (KeyPressed(keyboard, Keys.E)) _activeTool = _eraserTool;

        // Grid toggle
        if (KeyPressed(keyboard, Keys.G)) _showGrid = !_showGrid;

        // Sheet preview toggle
        if (KeyPressed(keyboard, Keys.V))
        {
            FinishDrawing();
            _sheetPreview = !_sheetPreview;
            if (_sheetPreview)
            {
                _sheetColumns = Math.Min(_project.Sprites.Count, 8);
                CenterSheet();
            }
            else
            {
                CenterCanvas();
            }
            return;
        }

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

    private void FinishDrawing()
    {
        if (!_isDrawing) return;
        _activeTool.OnRelease();
        if (_currentAction != null)
        {
            History.Push(_currentAction);
            _currentAction = null;
        }
        _isDrawing = false;
        _lastGridPos = new Point(-1, -1);
    }

    private void AdjustZoom(int direction)
    {
        int newIndex = Math.Clamp(_zoomIndex + direction, 0, ZoomLevels.Length - 1);
        if (newIndex == _zoomIndex) return;

        int viewW, viewH;
        if (_sheetPreview)
        {
            int rows = (int)Math.Ceiling((double)_project.Sprites.Count / _sheetColumns);
            viewW = _project.FrameWidth * _sheetColumns;
            viewH = _project.FrameHeight * rows;
        }
        else
        {
            viewW = Canvas.Width;
            viewH = Canvas.Height;
        }

        float centerX = _panOffset.X + viewW * _zoom / 2f;
        float centerY = _panOffset.Y + viewH * _zoom / 2f;

        _zoomIndex = newIndex;
        _zoom = ZoomLevels[_zoomIndex];

        _panOffset.X = centerX - viewW * _zoom / 2f;
        _panOffset.Y = centerY - viewH * _zoom / 2f;
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
                return;
            }

            // Left click on sprite tabs
            int tabIndex = HitTestTabs(mouse.X, mouse.Y);
            if (tabIndex >= 0)
            {
                if (tabIndex != _project.ActiveIndex)
                {
                    FinishDrawing();
                    _project.SetActive(tabIndex);
                    if (!_sheetPreview) CenterCanvas();
                }
                return;
            }
            if (tabIndex == -1) // [+] button
            {
                StartAddSprite();
                return;
            }

            // Click on a frame in sheet preview → switch to it and exit preview
            if (_sheetPreview)
            {
                int frameIndex = HitTestSheetFrame(mouse.X, mouse.Y);
                if (frameIndex >= 0)
                {
                    _project.SetActive(frameIndex);
                    _sheetPreview = false;
                    CenterCanvas();
                }
                return;
            }
        }

        // No drawing in sheet preview mode
        if (_sheetPreview) return;

        // Left mouse drawing
        Point gridPos = ScreenToGrid(mouse.X, mouse.Y);
        Color activeColor = _palette.ActiveColor;

        if (mouse.LeftButton == ButtonState.Pressed && !_isPanning)
        {
            if (!_isDrawing)
            {
                _isDrawing = true;
                if (Canvas.InBounds(gridPos.X, gridPos.Y))
                {
                    _currentAction = _activeTool.OnPress(Canvas, gridPos.X, gridPos.Y, activeColor);
                    _lastGridPos = gridPos;
                }
            }
            else if (gridPos != _lastGridPos && _lastGridPos.X >= 0 && _currentAction != null)
            {
                foreach (var p in BresenhamLine(_lastGridPos.X, _lastGridPos.Y, gridPos.X, gridPos.Y))
                {
                    if (Canvas.InBounds(p.X, p.Y))
                        _activeTool.OnDrag(Canvas, p.X, p.Y, activeColor, _currentAction);
                }
                _lastGridPos = gridPos;
            }
            else if (_lastGridPos.X < 0 && Canvas.InBounds(gridPos.X, gridPos.Y))
            {
                if (_currentAction == null)
                    _currentAction = _activeTool.OnPress(Canvas, gridPos.X, gridPos.Y, activeColor);
                else
                    _activeTool.OnDrag(Canvas, gridPos.X, gridPos.Y, activeColor, _currentAction);
                _lastGridPos = gridPos;
            }
        }
        else if (_isDrawing)
        {
            FinishDrawing();
        }
    }

    // --- Drawing ---

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(30, 30, 30));

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (_sheetPreview)
            DrawSheetPreview();
        else
            _renderer.DrawCanvas(_spriteBatch, Canvas, _zoom, _panOffset, _showGrid);

        DrawPalette();
        DrawSpriteTabs();

        // Top bar
        string topBar;
        if (_sheetPreview)
            topBar = $"[SHEET PREVIEW]  Zoom: {_zoom}x  {_project.Sprites.Count} sprites  [V] Exit  [Click] Select";
        else
            topBar = $"[{_activeTool.Name}]  Zoom: {_zoom}x  Grid: {(_showGrid ? "ON" : "OFF")}  Palette: {_palette.CurrentPalette.Name}";
        _spriteBatch.DrawString(_font, topBar, new Vector2(8, 6), _sheetPreview ? new Color(120, 200, 255) : Color.White);

        // Status bar
        string status;
        if (_sheetPreview)
        {
            int rows = (int)Math.Ceiling((double)_project.Sprites.Count / _sheetColumns);
            status = $"Sheet: {_sheetColumns}x{rows} ({_project.FrameWidth}x{_project.FrameHeight} per frame)";
            var mouseState = Mouse.GetState();
            int hoverFrame = HitTestSheetFrame(mouseState.X, mouseState.Y);
            if (hoverFrame >= 0)
                status += $"  [{_project.Sprites[hoverFrame].Name}]";
        }
        else
        {
            var mouseState = Mouse.GetState();
            var gridPos = ScreenToGrid(mouseState.X, mouseState.Y);
            status = $"{Canvas.Width}x{Canvas.Height}";
            if (Canvas.InBounds(gridPos.X, gridPos.Y))
                status += $"  ({gridPos.X}, {gridPos.Y})";
            if (_statusMessage != null)
                status += $"  {_statusMessage}";
            else
                status += $"  [{_project.ActiveSprite.Name}]  {_project.ActiveIndex + 1}/{_project.Sprites.Count}";
            if (History.CanUndo) status += "  [Ctrl+Z: Undo]";
            if (History.CanRedo) status += "  [Ctrl+Shift+Z: Redo]";
        }
        float statusY = _graphics.PreferredBackBufferHeight - _font.LineSpacing - 6;
        _spriteBatch.DrawString(_font, status, new Vector2(8, statusY), Color.Gray);

        // Dialog overlay
        if (_activeDialog != null)
            _activeDialog.Draw(_spriteBatch, _font, _renderer,
                _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, gameTime);

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void DrawSpriteTabs()
    {
        int x = 8;
        for (int i = 0; i < _project.Sprites.Count; i++)
        {
            string name = _project.Sprites[i].Name;
            var textSize = _font.MeasureString(name);
            int tabWidth = (int)textSize.X + TabPadX * 2;
            var tabRect = new Rectangle(x, TabBarY, tabWidth, TabHeight);

            if (i == _project.ActiveIndex)
            {
                _renderer.DrawRect(_spriteBatch, tabRect, new Color(60, 60, 60));
                _renderer.DrawRectOutline(_spriteBatch, tabRect, new Color(120, 120, 120), 1);
                _spriteBatch.DrawString(_font, name, new Vector2(x + TabPadX, TabBarY + 2), Color.White);
            }
            else
            {
                _renderer.DrawRect(_spriteBatch, tabRect, new Color(40, 40, 40));
                _spriteBatch.DrawString(_font, name, new Vector2(x + TabPadX, TabBarY + 2), Color.Gray);
            }

            x += tabWidth + TabGap;
        }

        // Draw [+] button
        string plus = "+";
        var plusSize = _font.MeasureString(plus);
        int plusWidth = (int)plusSize.X + TabPadX * 2;
        var plusRect = new Rectangle(x, TabBarY, plusWidth, TabHeight);
        _renderer.DrawRect(_spriteBatch, plusRect, new Color(40, 40, 40));
        _spriteBatch.DrawString(_font, plus, new Vector2(x + TabPadX, TabBarY + 2), new Color(100, 100, 100));
    }

    private int HitTestTabs(int screenX, int screenY)
    {
        if (screenY < TabBarY || screenY >= TabBarY + TabHeight)
            return -2; // not in tab bar

        int x = 8;
        for (int i = 0; i < _project.Sprites.Count; i++)
        {
            string name = _project.Sprites[i].Name;
            var textSize = _font.MeasureString(name);
            int tabWidth = (int)textSize.X + TabPadX * 2;

            if (screenX >= x && screenX < x + tabWidth)
                return i;

            x += tabWidth + TabGap;
        }

        // Check [+] button
        string plus = "+";
        var plusSize = _font.MeasureString(plus);
        int plusWidth = (int)plusSize.X + TabPadX * 2;
        if (screenX >= x && screenX < x + plusWidth)
            return -1; // signal: add new sprite

        return -2; // nothing hit
    }

    private void DrawSheetPreview()
    {
        int fw = _project.FrameWidth;
        int fh = _project.FrameHeight;

        for (int i = 0; i < _project.Sprites.Count; i++)
        {
            int col = i % _sheetColumns;
            int row = i / _sheetColumns;

            var frameOffset = new Vector2(
                _panOffset.X + col * fw * _zoom,
                _panOffset.Y + row * fh * _zoom);

            _renderer.DrawCanvas(_spriteBatch, _project.Sprites[i].Canvas, _zoom, frameOffset, _showGrid);

            // Highlight active sprite's frame
            if (i == _project.ActiveIndex)
            {
                var frameRect = new Rectangle(
                    (int)frameOffset.X, (int)frameOffset.Y,
                    fw * _zoom, fh * _zoom);
                _renderer.DrawRectOutline(_spriteBatch, frameRect, new Color(120, 200, 255, 180), 2);
            }
        }

        // Draw frame separator lines
        int totalCols = _sheetColumns;
        int totalRows = (int)Math.Ceiling((double)_project.Sprites.Count / _sheetColumns);
        var separatorColor = new Color(200, 200, 200, 60);

        for (int c = 1; c < totalCols; c++)
        {
            int sx = (int)(_panOffset.X + c * fw * _zoom);
            _renderer.DrawRect(_spriteBatch,
                new Rectangle(sx - 1, (int)_panOffset.Y, 2, totalRows * fh * _zoom),
                separatorColor);
        }
        for (int r = 1; r < totalRows; r++)
        {
            int sy = (int)(_panOffset.Y + r * fh * _zoom);
            _renderer.DrawRect(_spriteBatch,
                new Rectangle((int)_panOffset.X, sy - 1, totalCols * fw * _zoom, 2),
                separatorColor);
        }
    }

    private int HitTestSheetFrame(int screenX, int screenY)
    {
        int fw = _project.FrameWidth * _zoom;
        int fh = _project.FrameHeight * _zoom;

        int relX = screenX - (int)_panOffset.X;
        int relY = screenY - (int)_panOffset.Y;

        if (relX < 0 || relY < 0) return -1;

        int col = relX / fw;
        int row = relY / fh;

        if (col >= _sheetColumns) return -1;

        int index = row * _sheetColumns + col;
        if (index >= _project.Sprites.Count) return -1;

        return index;
    }

    private void CenterSheet()
    {
        int rows = (int)Math.Ceiling((double)_project.Sprites.Count / _sheetColumns);
        int totalW = _project.FrameWidth * _sheetColumns;
        int totalH = _project.FrameHeight * rows;
        _panOffset = new Vector2(
            (_graphics.PreferredBackBufferWidth - totalW * _zoom) / 2f,
            (_graphics.PreferredBackBufferHeight - totalH * _zoom) / 2f);
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

            _renderer.DrawRect(_spriteBatch, rect, palette.Colors[i]);

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
            (_graphics.PreferredBackBufferWidth - Canvas.Width * _zoom) / 2f,
            (_graphics.PreferredBackBufferHeight - Canvas.Height * _zoom) / 2f);
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
