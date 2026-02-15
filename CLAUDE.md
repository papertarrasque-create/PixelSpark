# PixelSpark

A minimal pixel art editor built in MonoGame. Designed for sprite creation, built with the intent to eventually live inside a game engine. The tool disappears. The art remains.

## Philosophy

Narrow scope, deep feel. PixelSpark does one thing — lets you draw and export pixel art — and it should feel immediate the moment you open it. Minimal chrome. Canvas dominates. Every control earns its screen space.

This is game tooling, not a general-purpose art program. Every architectural decision should ask: "Does this make sense inside an engine someday?"

## Architecture

### Data Model

**Canvas** — A 2D array of `Color?` (nullable). `null` = transparent. User sets width and height at creation. The canvas is the single source of truth for the image.

**Palette** — A named list of `Color` values. Multiple presets ship built-in (NES, CGA, PICO-8, Endesga-32). Only one palette is active at a time. The active color is an index into the current palette.

**Action** — A reversible change to the canvas. Every edit (pencil stroke, eraser stroke) is recorded as an Action with enough data to undo and redo. Actions store only the pixels that changed (old color, new color, position), not full canvas snapshots.

**Project** — A collection of named sprites (canvases) that can be exported individually or assembled into a sprite sheet. Each sprite has its own canvas and dimensions.

### Systems

**Renderer** — Draws the canvas to the screen at a configurable zoom level. Each canvas pixel renders as a `zoom × zoom` rectangle. Grid overlay drawn on top (toggleable). Handles viewport offset for panning.

**Input → Tool Pipeline** — Mouse position is converted from screen coordinates to grid coordinates via zoom and pan offset. The active tool receives grid coordinates and produces Actions. Tools are an interface:
```
ITool:
  OnPress(x, y, color) → Action
  OnDrag(x, y, color) → Action
  OnRelease() → Action?
```
Pencil sets a pixel. Eraser sets a pixel to null. Future tools (fill, line, shape) plug into the same interface.

**ActionHistory** — Two stacks: undo and redo. Applying a new action clears the redo stack. Each action is a list of pixel changes `(x, y, oldColor, newColor)`. Undo replays the old colors. Redo replays the new colors. A single drag stroke is one compound action (many pixel changes, one undo step).

**PaletteManager** — Holds all available palettes and the current selection. Switching palettes does not alter the canvas — existing colors stay, the palette panel just shows different swatches. Palette is a UI/workflow concept, not a data constraint.

**FileIO** — Save/load individual sprites as PNG. Export sprite sheet as PNG (grid layout: columns × rows, configurable). MonoGame's `Texture2D.SaveAsPng()` for output, `Texture2D.FromStream()` for input.

### UI Layout

```
┌──────────────────────────────────┐
│ [Tool] [Zoom] [Grid]             │
│  P  ┌──────────────────────────┐ │
│  A  │                          │ │
│  L  │                          │ │
│  E  │        CANVAS            │ │
│  T  │                          │ │
│  T  │                          │ │
│  E  │                          │ │
│     └──────────────────────────┘ │
│ [Status: sprite name, size, pos] │
└──────────────────────────────────┘
```

- **Top bar:** Active tool indicator, zoom level, grid toggle. Text only, no icons in v1.
- **Canvas area:** Center, takes most of the window. Pannable, zoomable.
- **Palette strip:** Left edge, vertical. Colored rectangles. Click to select. Current color highlighted.
- **Status bar:** Bottom. Sprite name, canvas dimensions, cursor grid position.
- All UI rendered directly with MonoGame (SpriteBatch + font). No UI framework.

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| B | Pencil tool |
| E | Eraser tool |
| G | Toggle grid |
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Ctrl+S | Save PNG |
| Ctrl+Shift+S | Export sprite sheet |
| +/- | Zoom in/out |
| Arrow keys / Middle mouse drag | Pan |
| Ctrl+N | New sprite |

## Build Phases

### Phase 1 — Canvas and Drawing
The foundation. Get pixels on screen and make editing feel responsive.
- MonoGame project scaffold
- Canvas data model (2D nullable Color array)
- Renderer: draw canvas at zoom level, viewport panning
- Screen-to-grid coordinate conversion
- Pencil tool (place active color)
- Eraser tool (place null/transparent)
- Grid overlay (toggleable)
- Zoom in/out
- Pan with arrow keys and/or middle mouse
- Checkerboard pattern behind transparent pixels

### Phase 2 — Undo and Palette
Make editing safe and give the artist colors to work with.
- Action data structure (pixel change list)
- ActionHistory (undo/redo stacks)
- Compound actions (one drag stroke = one undo step)
- PaletteManager with built-in presets
- Palette UI strip (right edge)
- Active color selection (click swatch)
- Palette switching (keyboard shortcut or small selector)

### Phase 3 — File I/O
Connect the editor to the outside world.
- Save canvas as PNG (Ctrl+S)
- Load PNG into canvas
- New canvas dialog (text input for width/height)
- Recent file tracking (optional, only if simple)

### Phase 4 — Sprite Sheet
The game-dev payoff.
- Project model (multiple named sprites)
- Sprite list panel or tab bar
- Add/remove/rename sprites within a project
- Sprite sheet export (grid layout, configurable columns)
- Sprite sheet import (split image into equal-size frames)
- Save/load project (custom JSON format preserving all sprites + metadata)

### Phase 5 — Tool Expansion (Post-v1)
Hooks for added complexity. Each tool implements ITool.
- Flood fill (recursive or queue-based)
- Line tool (Bresenham's algorithm)
- Rectangle tool (outline and filled)
- Color picker / eyedropper (sample color from canvas)
- Selection + move
- Copy/paste (within and between sprites)
- Mirror/flip
- Palette editor (modify and save custom palettes)

## Technical Notes

- **Framework:** MonoGame 3.8.4 (DesktopGL)
- **Font:** DejaVu Sans Mono (system-available on this Debian setup)
- **No UI library.** All UI is hand-rendered with SpriteBatch. This keeps dependencies minimal and teaches UI fundamentals.
- **Coordinate system:** Grid position (0,0) is top-left of the canvas. Screen position calculated as `gridPos * zoom + panOffset`.
- **Transparency:** Rendered as a checkerboard pattern (light/dark gray) behind the canvas, standard in image editors.
- **Performance:** For sprite-sized canvases (up to ~256×256), rebuilding a Texture2D from the color array each frame is fine. No need for GPU-side editing or dirty-rect optimization unless we hit a wall.

## Future: Engine Integration

PixelSpark is designed to eventually live inside a game engine as a built-in sprite editor. Architectural decisions that support this:
- Canvas is a pure data model (Color array), not coupled to rendering
- Tools produce Actions, not direct mutations — the engine could intercept or replay these
- Project/sprite sheet model maps directly to game asset pipelines
- No external dependencies beyond MonoGame itself

This is a note for the future, not a current requirement. Build the standalone editor first. Earn the complexity.

## Run

```
dotnet run
```
from the project root.
