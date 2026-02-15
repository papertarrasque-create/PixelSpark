# PixelSpark — Product Roadmap

## Vision

A minimal pixel art editor that feels immediate. Open it, draw, export. No menus to learn, no features to discover — the canvas is the experience. Built in MonoGame so it can eventually live inside a game engine as a native sprite editor.

## Phase 1 — Canvas and Drawing

**Goal:** Get pixels on screen. Make drawing feel responsive and direct.

### Deliverables
- [x] MonoGame project scaffold (DesktopGL, .NET 9)
- [x] Canvas data model — 2D `Color?` array, null = transparent
- [x] Renderer — draw canvas pixels as `zoom × zoom` rectangles
- [x] Screen-to-grid coordinate conversion (mouse position → canvas cell)
- [x] Viewport panning (arrow keys + middle mouse drag)
- [x] Zoom in/out (+/- keys, scroll wheel, centered on canvas)
- [x] Pencil tool — click/drag places the active color
- [x] Eraser tool — click/drag sets pixels to null
- [x] Grid overlay (toggleable with G key, auto-hides below 4x zoom)
- [x] Checkerboard pattern behind transparent pixels
- [x] Top bar — active tool name, zoom level, grid status (text only)
- [x] Status bar — canvas dimensions, cursor grid position
- [x] Bresenham line interpolation for gap-free fast strokes

### Done When
You can open PixelSpark, draw on a canvas with a single color, erase pixels, zoom and pan around, and see exactly where you are at all times. Drawing feels instant — no lag, no missed pixels on fast strokes.

**Status: COMPLETE**

---

## Phase 2 — Undo and Palette

**Goal:** Make editing safe and give the artist colors to work with.

### Deliverables
- [ ] Action data structure — list of `(x, y, oldColor, newColor)` changes
- [ ] ActionHistory — undo stack + redo stack
- [ ] Compound actions — one drag stroke = one undo step
- [ ] Undo (Ctrl+Z) / Redo (Ctrl+Y)
- [ ] PaletteManager with built-in presets (NES, CGA, PICO-8, Endesga-32)
- [ ] Palette UI strip on the left edge — colored rectangles, click to select
- [ ] Active color highlight in the palette
- [ ] Palette switching (keyboard shortcut or small selector)

### Done When
You can draw with any color from a palette, undo/redo freely without losing work, and switch between palette presets. A full drag stroke undoes in one step, not pixel by pixel.

---

## Phase 3 — File I/O

**Goal:** Connect the editor to the outside world.

### Deliverables
- [ ] Save canvas as PNG (Ctrl+S) via `Texture2D.SaveAsPng()`
- [ ] Load PNG into canvas via `Texture2D.FromStream()`
- [ ] New canvas dialog — text input for width and height (Ctrl+N)
- [ ] Overwrite confirmation when saving to an existing file

### Done When
You can create a sprite, save it as a PNG, close the app, reopen, load the PNG, and continue editing exactly where you left off. Round-trip is lossless.

---

## Phase 4 — Sprite Sheet

**Goal:** The game-dev payoff. Multiple sprites, one export.

### Deliverables
- [ ] Project model — collection of named sprites (each with its own canvas)
- [ ] Sprite list panel or tab bar
- [ ] Add / remove / rename sprites within a project
- [ ] Sprite sheet export — grid layout, configurable columns (Ctrl+Shift+S)
- [ ] Sprite sheet import — split an image into equal-size frames
- [ ] Save/load project as JSON (preserving all sprites + metadata)

### Done When
You can create a project with multiple sprites, edit them individually, export a sprite sheet PNG ready for a game engine, and save/load the full project without data loss.

---

## Phase 5 — Tool Expansion (Post-v1)

**Goal:** Richer editing tools. Each implements the ITool interface.

### Deliverables
- [ ] Flood fill (queue-based)
- [ ] Line tool (Bresenham's algorithm)
- [ ] Rectangle tool (outline + filled variants)
- [ ] Eyedropper — sample color from canvas
- [ ] Selection + move
- [ ] Copy/paste (within and between sprites)
- [ ] Mirror / flip
- [ ] Palette editor — modify and save custom palettes

### Done When
The tool set covers the core operations a pixel artist needs for sprite work. Every tool follows the same interface, and adding new ones is straightforward.

---

## Principles

- **Canvas dominates.** Every UI element earns its screen space. If it's not helping the artist right now, it's in the way.
- **Game tooling, not art program.** Every architectural choice should make sense inside an engine someday.
- **Actions, not mutations.** Every edit is reversible. The tool produces data; the system decides what to do with it.
- **No black boxes.** Every system stays small enough to read, explain, and modify by hand.
