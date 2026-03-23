# Engine Implementation Architecture

The `com.hatiora.pico8` package forms the core engine runtime. It encapsulates the complete virtual hardware of PICO-8, exposing strict abstraction layers that maintain functional parity with the original PICO-8 Lua environment while running entirely on native C# within Unity.

---

## 🏗 High-Level Architecture

The framework is structurally divided into five main modules:
1. **The API Facade**: [Pico8Api](../../packages/com.hatiora.pico8/Runtime/Api/Pico8Api.cs) acts as the global command switchboard.
2. **Virtual Memory**: [Pico8Memory](../../packages/com.hatiora.pico8/Runtime/Core/Pico8Memory.cs) governs the RAM addressing space with a pinned [MemoryLayout](../../packages/com.hatiora.pico8/Runtime/Core/MemoryLayout.cs) matching the real PICO-8 memory map (`ScreenStart = 0x6000`).
3. **The Rendering Pipeline**: [PixelBuffer](../../packages/com.hatiora.pico8/Runtime/Graphics/PixelBuffer.cs), [TextureGraphics](../../packages/com.hatiora.pico8.unity/Runtime/Graphics/TextureGraphics.cs) and [DrawState](../../packages/com.hatiora.pico8/Runtime/Core/DrawState.cs).
4. **Data Repositories**: `SpriteStore` and `MapStore`.
5. **The Application Bindings**: [Pico8Engine](../../packages/com.hatiora.pico8.unity/Runtime/Core/Pico8Engine.cs), [Pico8Builder](../../packages/com.hatiora.pico8.unity/Runtime/Core/Pico8Builder.cs), and peripheral providers (Input, Audio).

Below is the execution flow:
- A user creates a `Cartridge` subclass encapsulating their game logic.
- Unity ([App.cs](../../apps/Hatiora/Assets/Scripts/App.cs)) instantiates a [Pico8Builder](../../packages/com.hatiora.pico8.unity/Runtime/Core/Pico8Builder.cs) and mounts the [Cartridge](../../packages/com.hatiora.pico8/Runtime/Core/Cartridge.cs).
- [Pico8Builder.Build()](../../packages/com.hatiora.pico8.unity/Runtime/Core/Pico8Builder.cs#L86) allocates the Engine sub-components ([Pico8Memory](../../packages/com.hatiora.pico8/Runtime/Core/Pico8Memory.cs), `Palette`, `Graphics`, `Audio`, `Input`) and wires them to [Pico8Api](../../packages/com.hatiora.pico8/Runtime/Api/Pico8Api.cs).
- The Builder returns a [Pico8Engine](../../packages/com.hatiora.pico8.unity/Runtime/Core/Pico8Engine.cs) that is driven by Unity's core event loops (e.g. `Update()` triggering `Tick()`).

---

## ⚙️ The Core Game Loop ([Pico8Engine.cs](../../packages/com.hatiora.pico8.unity/Runtime/Core/Pico8Engine.cs))

[Pico8Engine](../../packages/com.hatiora.pico8.unity/Runtime/Core/Pico8Engine.cs) orchestrates standard tick execution:

1. **Poll Input**: Ask the `IInputProvider` (e.g. `UnityInputProvider`) to populate the engine's `Pico8Input` states.
2. **Update Game Logic**: Invoke `Cartridge.Update()`.
3. **Draw Elements**: Invoke `Cartridge.Draw()`.
4. **Measure CPU**: Record Update+Draw elapsed time and report to `Pico8Api.SetCpuFraction()` as a fraction of the 30fps budget (`1/30s`). Accessible via `Stat(1)`.
5. **Push to GPU**: Invoke `Flush()` on Graphics and present the resulting screen Texture to Unity's UI/Sprite stack.
6. **Snapshot Buttons**: Save the input states to correctly evaluate `Btnp` (button pressed in current frame) on the next loop.

> [!NOTE] 
> Because [Pico8Engine.Tick(float dt)](../../packages/com.hatiora.pico8.unity/Runtime/Core/Pico8Engine.cs#L49) is invoked manually by the Unity container ([App.Update()](../../apps/Hatiora/Assets/Scripts/App.cs#L89)), the virtual FPS operates directly at Unity's specified `Application.targetFrameRate` (which should theoretically be locked to 30 or 60).

---

## 🎮 The API Facade ([Pico8Api.cs](../../packages/com.hatiora.pico8/Runtime/Api/Pico8Api.cs))

[Pico8Api.cs](../../packages/com.hatiora.pico8/Runtime/Api/Pico8Api.cs) natively implements all functions exposed in Lua. If a user calls `Spr(1, 10, 10)`, the [Cartridge](../../packages/com.hatiora.pico8/Runtime/Core/Cartridge.cs) passes this directly to [Pico8Api.Spr()](../../packages/com.hatiora.pico8/Runtime/Api/Pico8Api.cs#L89).

The API serves as a router:
- **Math/System**: Routed to `Pico8Math` or the native `Stopwatch` (`Time()`). `Stat(1)` returns the CPU fraction (Update+Draw time / 30fps budget), measured by `Pico8Engine.Tick`.
- **Memory**: `Peek`/`Poke` route byte-per-byte to [Pico8Memory](../../packages/com.hatiora.pico8/Runtime/Core/Pico8Memory.cs). `Memcpy` auto-detects when the source or destination overlaps the screen memory region (`0x6000+`) and transparently syncs [PixelBuffer](../../packages/com.hatiora.pico8/Runtime/Graphics/PixelBuffer.cs) ↔ RAM via `FlushToRam()` / `LoadFromRam()`.
- **Audio**: `Sfx()` and `Music()` call the abstract `IAudio` module.
- **Input**: `Btn()` evaluates states held inside `IInput`. 
- **Sprite Flags**: `Fget(n, f)` / `Fset(n, f, val)` delegate to [MapStore](../../packages/com.hatiora.pico8/Runtime/Map/MapStore.cs) which reads from the `__gff__` sprite flag region (`0x3000`). Flag data is loaded at build time via [GffDataLoader](../../packages/com.hatiora.pico8.unity/Runtime/Loaders/GffDataLoader.cs).
- **Reload**: `Reload()` restores map and sprite flag RAM to the original ROM data loaded at build time. Used by cartridges that mutate the map (e.g., `jelpi.p8` level switching via `memcpy`).
- **Graphics**: Draw calls are routed to `IGraphics` ([PixelBuffer](../../packages/com.hatiora.pico8/Runtime/Graphics/PixelBuffer.cs)), applying modifiers like Camera and Colors from [DrawState](../../packages/com.hatiora.pico8/Runtime/Core/DrawState.cs). `Map()` accepts an optional `scale` parameter (default `1`); when `scale > 1`, each tile is drawn at `8×scale` pixels using `DrawSpriteStretch` for hi-res rendering. Cartridges that need hi-res support replace `Spr` calls with `Sspr` using `dw/dh = 8*s`.

## 📺 VRAM & Display Resolution Configuration ([EngineSpec](../../packages/com.hatiora.pico8/Runtime/Core/EngineSpec.cs))

Unlike traditional PICO-8 which is strictly locked to an unchangeable 128x128 memory block, our engine configuration via [EngineSpec](../../packages/com.hatiora.pico8/Runtime/Core/EngineSpec.cs) enables distinct rendering topologies without breaking native logic compatibility.

Every internal subsystem reads its structural dimensions from [EngineSpec](../../packages/com.hatiora.pico8/Runtime/Core/EngineSpec.cs). Crucially, this defines a separation between:
1. **Virtual Resolution** (`ScreenWidth`, `ScreenHeight`): The internal game bounds (128x128 specifically for the PICO-8 case, but can be configured to emulate any platform). Game logic, map iteration, and standard bounding boxes rely on this.
2. **Physical Resolution** (`PhysicalWidth`, `PhysicalHeight`): The raw dimensional boundary of the output buffer Texture instantiated by Unity (e.g., 1024x1024).

By splitting these responsibilities, [EngineSpec](../../packages/com.hatiora.pico8/Runtime/Core/EngineSpec.cs) determines a dynamic internal integer `Scale` factor. 

### Dual Coordinate Drawing ([CoordMode](../../packages/com.hatiora.pico8/Runtime/Graphics/CoordMode.cs))
All API drawing methods (like `Pset`, `Rectfill`, `Print`, `Spr`) accept an advanced `CoordMode` parameter.

- **`CoordMode.Virtual`** *(Default)*: Coordinates are automatically multiplied by [EngineSpec.Scale](../../packages/com.hatiora.pico8/Runtime/Core/EngineSpec.cs#L45). If a user draws at `X=10` with a scale of `8x`, it fundamentally draws at physical pixel `80`. This maintains absolute visual parity with real PICO-8.
- **`CoordMode.Physical`**: Bypasses the scale multiplier. Drawing at `X=10` draws exactly at pixel 10 in the high-res texture VRAM. 

This dual-coordinate architecture natively powers hi-resolution "modding" capabilities. A cartridge logic can run perfectly scaled while simultaneously drawing crisp UI or expanded sprite details in raw Physical mode.

---

## 🧠 Memory-Mapped Screen (`MemoryMap`)

The engine's [MemoryLayout](../../packages/com.hatiora.pico8/Runtime/Core/MemoryLayout.cs) computes RAM addresses from a configurable [MemoryMap](../../packages/com.hatiora.pico8/Runtime/Core/MemoryMap.cs) preset injected via `EngineSpec.MemoryMap`. The default preset is `MemoryMap.Pico8`, which mirrors the real PICO-8 memory map:

| Region | Address | Size (PICO-8 default) |
| :--- | :--- | :--- |
| GFX (spritesheet) | `0x0000` | 8192 bytes |
| Map | `0x2000` | 4096 bytes |
| Flags | `0x3000` | 256 bytes |
| Music | `0x3100` | 256 bytes |
| SFX | `0x3200` | 4352 bytes |
| Draw State | `0x5F00` | 64 bytes |
| **Screen** | **`0x6000`** | 8192 bytes (128×128/2) |

The screen region (`0x6000`) stores pixel data in **nibble-packed** format: each byte holds 2 pixels (low nibble = even column, high nibble = odd column). This matches the original PICO-8 VRAM layout.

When `Pico8Api.Memcpy` detects that the source or destination overlaps `[ScreenStart, ScreenStart+ScreenSize)`, it transparently:
1. **Flushes** `PixelBuffer.Pixels[]` → `Pico8Memory.Ram` via `FlushToRam()` (sampling every Scale-th physical pixel)
2. **Executes** the raw byte copy in RAM
3. **Loads** `Pico8Memory.Ram` → `PixelBuffer.Pixels[]` via `LoadFromRam()` (expanding each virtual pixel to a Scale×Scale physical block)

This enables classic PICO-8 screen effects like `memcpy(0x6000, 0x6040, 0x1FC0)` (scroll up 1 pixel) to work identically in C#. All dimensions are driven by `EngineSpec` (`ScreenWidth`, `ScreenHeight`, `Scale`), so custom resolutions work automatically.

---

## 🎨 The Rendering Pipeline

Because Unity does not render scanlines natively, we simulate a strict virtual pixel domain using C# arrays in [PixelBuffer.cs](../../packages/com.hatiora.pico8/Runtime/Graphics/PixelBuffer.cs) before marshaling them to the GPU.

1. **DrawState**: Retains the `CameraX/Y`, `ClipW/H`, `DrawPalette`, `DisplayPalette`, and `Transparency` map.
2. **PixelBuffer (Virtual)**: Operations like `Circfill` do not talk to Unity. They modify an internal C# `byte[]` array mathematically, applying [DrawState](../../packages/com.hatiora.pico8/Runtime/Core/DrawState.cs) operations in real-time. This buffer is completely decoupled from Unity classes. Additionally, `FlushToRam()` and `LoadFromRam()` provide bidirectional sync with `Pico8Memory.Ram` for memory-mapped screen effects.
3. **TextureGraphics (Physical)**: Once `Tick()` reaches the Flush phase, [TextureGraphics](../../packages/com.hatiora.pico8.unity/Runtime/Graphics/TextureGraphics.cs) iterates over the [PixelBuffer](../../packages/com.hatiora.pico8/Runtime/Graphics/PixelBuffer.cs) `byte[]`, evaluates the `DisplayPalette`, converts indexes to Unity `Color32` structs, and updates a low-level `Texture2D` using `SetPixelData<Color32>`.

Because [TextureGraphics](../../packages/com.hatiora.pico8.unity/Runtime/Graphics/TextureGraphics.cs) writes to the Texture memory natively in a single continuous array mapping, rendering performance is extremely high and minimizes GPU-side draw calls.
