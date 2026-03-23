# Porting Guide: Lua to Native C#

When porting a legacy PICO-8 `.p8` cartridge to our native Unity C# ecosystem, there is a strict sequence of operations required to maintain parity with the engine's constraints while enabling our high-resolution capabilities.

This guide outlines exactly how a developer should migrate functionality.

---

## 1. Automated Scaffolding (Do Not Write from Scratch)

Before writing any C# code, you **must** use the ecosystem tooling to generate the package structure. Do not manually create folders, `.asmdef` files, or `README.md` scaffolding.

1. Open the [CartridgeManagerWindow](../../packages/com.hatiora.pico8.tools/Editor/CartridgeManagerWindow.cs) (`PICO-8 > Settings > Tools`).
2. Navigate to the **Wizard** tab and upload your source `.p8` ROM.
3. The [CartridgeService](../../packages/com.hatiora.pico8.tools/Editor/CartridgeService.cs) will invoke [P8Extractor](../../packages/com.hatiora.pico8.tools/Editor/P8Extractor.cs) to automatically:
   - Export physical `Gfx/gfx.png`, `Label/label.png`, `Sfx/sfx.txt`, `Music/music.txt`, `Map/map.txt`, and `Gff/gff.txt` assets.
   - Generate the UPM package containing a heavily templated `[Name]Cartridge.cs` script.

> [!IMPORTANT]
> After scaffolding, you must verify that **every** extracted resource file in `Runtime/Resources/` is wired in the cartridge's `IUnityCartridge` properties. The full list is:
> - `GfxTexture` → `Gfx/gfx` (spritesheet)
> - `LabelTexture` → `Label/label` (label image)
> - `SfxData` → `Sfx/sfx` (sound effects)
> - `MusicData` → `Music/music` (music patterns)
> - `MapData` → `Map/map` (tile map)
> - `GffData` → `Gff/gff` (sprite flags for `Fget`/`Fset`)
>
> Properties without a resource file return `null`. Failing to wire `GffData` causes `Fget()` to always return 0, breaking wall collision and any flag-based logic.

---

## 2. Converting the Game Loop

In PICO-8 Lua, `_init`, `_update` (or `_update60`), and `_draw` are global functions. Inside your generated [Cartridge](../../packages/com.hatiora.pico8/Runtime/Core/Cartridge.cs) subclass, these map to standard `override` methods:

```csharp
public override void Init()   { /* Translate _init() here */ }
public override void Update() { /* Translate _update() here */ }
public override void Draw()   { /* Translate _draw() here */ }
```

> [!IMPORTANT] 
> Because your subclass inherits from `Cartridge`, all global PICO-8 API functions (e.g., `Spr()`, `Sfx()`, `Cls()`) are exposed directly to you via the abstract [Pico8Api](../../packages/com.hatiora.pico8/Runtime/Api/Pico8Api.cs) facade. 

---

## 3. Typings and Data Structures

Lua is dynamically typed and relies entirely on generic `Tables`. C# is strictly typed.

### Variables & Declarations
- Convert Lua globals and `local` definitions to specific primitive fields (`int`, `float`, `bool`, `string`).
- **Hint:** In PICO-8, almost all numbers act strictly as floating points unless explicitly floored. Default to `float` for coordinates/velocity and `int` for indices/timers.

### Converting Lua Tables `{ }`
Do not attempt to use `Dictionary<string, object>` to replicate dynamic tables.
- **Convention**: If a table acts as an "Object" (e.g., `{ x=10, y=20, hp=5 }`), write a formal internal struct or class:
  ```csharp
  private class Player {
      public float x, y;
      public int hp;
  }
  ```
- **Convention**: If a table acts as a collection (e.g., adding enemies), use `List<T>`.
  - Lua: `add(enemies, e)` → C#: `_enemies.Add(e);`
  - Lua: `del(enemies, e)` → C#: `_enemies.Remove(e);`
  - Lua: `for e in all(enemies) do` → C#: `foreach (var e in _enemies) { }`

---

## 4. Array Indexing (The 1-to-0 Shift)

PICO-8 tables are `1-indexed`. C# arrays are natively `0-indexed`.
You must meticulously audit iteration loops, tile maps, and coordinate fetches:
- **Lua Loop**: `for i=1, 10 do`
- **C# Loop**: `for (int i = 0; i < 10; i++)`
You will almost always need to shift explicit math coordinates down by `1`.

---

## 5. Mathematical Parity (Enforcing Constraints)

You **must not** use standard `System.MathF` or standard C# operators for complex math constraints. Our engine replicates specific Lua edge-cases natively via `Pico8Math` (accessible automatically inside your `Cartridge`).

### Modulus and Negative Wrappers
If a translation uses standard C# `%` against negative numbers, it will fail because C# leaves negative remainders (Lua `-1 % 4 = 3`, C# `-1 % 4 = -1`).
- **Fix**: Write safe wrapping wrappers, or explicitly add the divisor before modulating.

### Floor and Ceiling
- Standard C# casting `(int)-2.3f` equals `-2`.
- PICO-8 `flr(-2.3)` equals `-3`.
- **Fix**: Always use the provided `Flr(x)` inherited method.

### Trigonometry (`Sin` / `Cos`)
- PICO-8 calculates angles in **Turns (0..1)**, not Radians.
- PICO-8's `Sin` is inverted (e.g., Y points down on the screen).
- **Fix**: Directly use the inherited `Sin(x)` and `Cos(x)` methods, which automatically funnel into `Pico8Math` to invert the axes and multiply against `MathF.PI * 2f`.

### Random Number Generation (`Rnd` / `Srand`)
- **Fix**: Never use `UnityEngine.Random`. Call `Rnd(max)` and `Srand(seed)`. These operate via the fixed engine deterministic sequence.

---

## 6. High-Res Rendering & Resolution Scaling (MANDATORY)

Every cartridge port **must** support high-resolution scaling. Our engine supports physical targets decoupled from the 128x128 virtual constraint, injected via [EngineSpec](../../packages/com.hatiora.pico8/Runtime/Core/EngineSpec.cs) inside `App.cs`. 

### Standard Pattern (Simple Cartridges)
For cartridges that use `Print`, `Rectfill`, `Circfill`, etc.:
1. Add `private bool _isHighRes = true;` and toggle via `Btnp(7)`.
2. Compute scale at the top of `Draw()`:
   ```csharp
   float scale = _isHighRes ? P8.Width / 128f : 1f;
   int s = Mathf.Max(1, (int)scale);
   ```
3. Multiply all coordinates by `s`. Use `Sspr` instead of `Spr` with `8*s` dimensions for proportional sprite scaling.

### Map-Heavy Cartridge Pattern (Platformers, etc.)
For cartridges that render using `Map()` + `Camera()` with many actor draw functions:
1. Store scale in a field accessible by all draw delegates:
   ```csharp
   private int _drawScale = 1;
   // In Draw():
   _drawScale = s;
   ```
2. Use `Map(scale: s)` for all tile map rendering — the engine scales each tile to `8×s` pixels via `DrawSpriteStretch`.
3. Replace all `Spr(n, x, y)` in actor/sparkle draw functions with:
   ```csharp
   int sprSx = (n % 16) * 8, sprSy = (n / 16) * 8;
   Sspr(sprSx, sprSy, 8, 8, x * s, y * s, 8 * s, 8 * s);
   ```
4. Scale all world pixel coordinates (`Line`, `Circfill`, `Rectfill`, `Pset`) by `s`.
5. Scale background element positions, parallax offsets, and fill bounds by `s`.
6. Camera offsets passed to `DrawWorld` should already be `_camX * s`.

### Complex Renderer Pattern (Raycasters, etc.)
For cartridges with full-screen renderers (e.g., raycasters, 3D engines):
1. Compute dynamic bounds:
   ```csharp
   int W = _isHighRes ? P8.Width : 128;
   int H = _isHighRes ? P8.Height : 128;
   int half = H / 2;
   ```
2. Replace ALL hardcoded `127` with `W - 1`, `64` with `half`, and `127f` with `(float)(W - 1)`.
3. Loop over `for (int sx = 0; sx <= W - 1; sx++)` and set `sy = H - 1`.

> [!IMPORTANT]
> **NEVER** hardcode `127` or `64` as screen bounds. Always derive from `P8.Width`/`P8.Height`. Use `Mathf.Max(1, ...)` to prevent scale collapse at sub-128 resolutions.

---

## 7. Memory-Mapped Screen Effects (`Memcpy` at `0x6000`)

Many PICO-8 cartridges use `memcpy` to directly manipulate screen memory for effects like scrolling, screen transitions, and pixel-level VRAM tricks. The original Lua idiom:
```lua
-- Scroll screen up 1 pixel row
memcpy(0x6000, 0x6040, 0x1fc0)
```

Our engine fully supports this. The [MemoryLayout](../../packages/com.hatiora.pico8/Runtime/Core/MemoryLayout.cs) pins the screen at `0x6000` (matching real PICO-8), and [Pico8Api.Memcpy](../../packages/com.hatiora.pico8/Runtime/Api/Pico8Api.cs) auto-detects when the copy range overlaps screen memory and transparently syncs `PixelBuffer` ↔ `RAM`.

### Translation Pattern
```csharp
// Screen address and row stride are derived from the engine spec
int screenAddr = 0x6000;
int bytesPerRow = P8.Width / 2; // nibble-packed: 2 pixels per byte

// Scroll up 1 row
Memcpy(screenAddr, screenAddr + bytesPerRow, bytesPerRow * (P8.Height - 1));
```

### How It Works
1. `Pico8Api.Memcpy` detects overlap with `[ScreenStart, ScreenStart+ScreenSize)`
2. **Flushes** `PixelBuffer.Pixels[]` → `Pico8Memory.Ram` via `FlushToRam()` (nibble-packs virtual pixels)
3. **Executes** the raw byte copy in RAM
4. **Loads** `Pico8Memory.Ram` → `PixelBuffer.Pixels[]` via `LoadFromRam()` (expands to Scale×Scale physical blocks)

> [!IMPORTANT]
> Always use `P8.Width / 2` for `bytesPerRow` (not hardcoded `64`), and `P8.Height` for row count. This ensures the effect works at any resolution configured via `EngineSpec`.


