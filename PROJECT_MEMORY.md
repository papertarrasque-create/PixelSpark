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

## Current Phase
**Phase 1 — Canvas and Drawing** (not started)

## Recent History
- **Session 5 (2026-02-15):** Created PRD.md and PROJECT_MEMORY.md. Initialized GitHub repo. Starting Phase 1.
