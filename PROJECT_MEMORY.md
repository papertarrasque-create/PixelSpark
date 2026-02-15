# PixelSpark

## What It Is
Minimal pixel art editor in MonoGame. Game tooling — designed to eventually live inside an engine as a built-in sprite editor.

## Tech Stack
- **Framework:** MonoGame 3.8.4 (DesktopGL)
- **Language:** C# / .NET 9
- **Font:** DejaVu Sans Mono (system-available on Debian)
- **Platform:** Linux (Debian)

## Key Files
```
workbench/PixelSpark/
├── CLAUDE.md           # Architecture spec and design decisions
├── PRD.md              # Phased roadmap with checklists
├── PROJECT_MEMORY.md   # This file — session context
```

## Key Decisions
- All UI hand-rendered with SpriteBatch — no UI framework dependency
- Canvas is a pure data model (Color? array), decoupled from rendering
- Tools produce Actions (reversible change records), not direct canvas mutations
- Palette is a UI/workflow concept — switching palettes doesn't alter canvas data
- Performance: rebuild Texture2D from array each frame (fine for sprite-sized canvases up to ~256×256)
- File paths are typed, not browsed — deliberate scope choice. A visual file browser would be throwaway work since PixelSpark is destined for engine integration (the engine provides its own asset browser). Typed paths are a complete, shippable feature.
- Auto-append `.png` if no extension given, tilde expansion for home directory paths

## Current Phase
**Phase 5 — Tool Expansion** (complete, palette editor deferred)

## Recent History
- **Session 5 (2026-02-15):** Created PRD.md and PROJECT_MEMORY.md. Initialized GitHub repo. Starting Phase 1.
- **Session 6 (2026-02-15):** Completed Phase 3 — File I/O. Built modal dialog system (IDialog interface), reusable TextInputField with scissor-clipped scrolling, InputDialog (save/load paths), NewCanvasDialog (width/height with validation), ConfirmDialog (Y/N overwrite). Wired Ctrl+S/Ctrl+Shift+S/Ctrl+O/Ctrl+N flows into PixelSparkGame. Window title updates on save/load, status bar shows file name and timed messages. Discussed file browser vs typed path — chose typed path because a standalone file browser is throwaway work for a tool headed into an engine. Added versioning policy to CLAUDE.md. New files: FileIO.cs, TextInputField.cs, IDialog.cs, InputDialog.cs, NewCanvasDialog.cs, ConfirmDialog.cs. Modified: PixelSparkGame.cs.
- **Session 7 (2026-02-15):** Completed Phase 4 — Sprite Sheet. The game-dev payoff. Built the full project/sprite model: `Sprite` (name + canvas + per-sprite undo history), `Project` (collection of sprites with frame dimensions), `ProjectIO` (JSON save/load with base64 pixel data). Refactored PixelSparkGame from single canvas to project-backed — convenience properties (`Canvas`, `History`) keep the diff clean. Added sprite tab bar below the top bar with clickable tabs and [+] button. Sprite management: Ctrl+T add, Ctrl+W remove (with confirm), F2 rename, Ctrl+Tab/Ctrl+Shift+Tab cycle. Sprite sheet export (Ctrl+Shift+E) tiles all sprites into a grid PNG. Sprite sheet import (Ctrl+I) chops an image into frames using the project's dimensions, skips empty frames. Sheet preview mode (V key) shows all sprites tiled in a grid — click a frame to jump to it. Project saves as `.pxs` (renamed from `.pxproj` which collided with MSBuild's `*proj` glob). Ctrl+O auto-detects `.pxs` vs `.png` by extension. New files: Sprite.cs, Project.cs, ProjectIO.cs. Modified: PixelSparkGame.cs (major rewrite), FileIO.cs (added export/import).
- **Session 8 (2026-02-15):** Completed Phase 5 — Tool Expansion. Added seven features in dependency order. Foundation: extended ITool interface with `DrawPreview` (optional overlay), `InterpolateDrag` (controls Bresenham drag interpolation), and `OnRelease(Canvas, PixelAction)` (deferred commit). Extracted `BresenhamLine` to `PixelMath` utility class. Added `SwitchTool()` method that commits selection floats on tool change. Wrapped tool switching in `!_isDrawing` guard. Tools: EyedropperTool (I key) — samples canvas color, searches all palettes via `PaletteManager.SelectColorFromAll`, switches palette if needed. FillTool (F key) — queue-based 4-connected flood fill, exact color match. LineTool (L key) — Bresenham with 60% opacity preview, deferred commit on release. RectangleTool (R / Shift+R) — outline and filled variants, same preview/commit pattern. Canvas operations: MirrorHorizontal (Ctrl+M) and FlipVertical (Ctrl+Shift+M) — swap-based, single PixelAction. SelectionTool (S key) — three modes (selecting, moving, idle), floating pixel buffer with lift/stamp/commit lifecycle, internal `_liftAction` tracks combined operations for single-step undo. Copy/cut/paste (Ctrl+C/X/V) — clipboard as `Color?[,]` at game level for cross-sprite support, paste creates float via SelectionTool.SetFloat. Selection auto-commits on sprite switch (tab click, Ctrl+Tab, sheet preview frame click). Palette editor deferred to future phase, noted in CLAUDE.md. New files: EyedropperTool.cs, FillTool.cs, LineTool.cs, RectangleTool.cs, SelectionTool.cs, PixelMath.cs. Modified: ITool.cs, PaletteManager.cs, PixelSparkGame.cs.
