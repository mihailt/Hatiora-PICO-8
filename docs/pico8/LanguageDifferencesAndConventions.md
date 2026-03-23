# Language Differences and Conventions

When translating original PICO-8 Lua scripts (`.p8`) into the native Unity C# ecosystem (`Cartridge.cs` subclasses), there are fundamental language paradigm shifts. This document outlines the critical deviations, constraints, and standardized conventions required to maintain mathematical and structural parity.

---

## 1. Data Structures & Indexing

### 1-Indexed (Lua) vs 0-Indexed (C#)
PICO-8 Lua tables and arrays are natively `1-indexed`. C# arrays and `List<T>` structures are natively `0-indexed`.
- **Constraint**: When migrating loop logic or array lookups, C# iterations must be carefully shifted.
- **Convention**: Most translations adapt the iteration logic natively to `0` to keep C# syntax idiomatic, rather than padding arrays with a blank `0` index.

### Tables (`{}`) vs C# Classes/Structs
Lua uses generic `Tables` for everything (Objects, Arrays, HashMaps).
- **Convention**: In our ecosystem, dynamically allocated Lua tables representing complex entities (like a `Player` or `Particle`) are translated into formal C# `class` or `struct` definitions inside the Cartridge namespace.
- **`all()` vs `foreach`**: Lua's `for e in all(entities) do` translates natively to C#'s `foreach (var e in entities)`. `add(entities, obj)` maps to `entities.Add(obj)`.

---

## 2. Mathematical Parity (`Pico8Math`)

PICO-8 has highly specific mathematical behaviors that do not directly map to standard `System.MathF` in C#. Our [`Pico8Math`](../../packages/com.hatiora.pico8/Runtime/Core/Pico8Math.cs) wrapper enforces these exact behaviors.

### Floor and Negatives (`Flr`)
Standard C# integer truncation (`(int)-2.3f`) results in `-2`. 
PICO-8's `flr(-2.3)` natively results in `-3`. 
- **Convention**: Always use the provided engine API `Flr(x)` which safely wraps `MathF.Floor(x)` and casts to `int`.

### Modulus (`%` vs Lua Modulo)
The `a % b` operator in C# works differently for negative numbers compared to Lua.
- In Lua: `-1 % 4` equals `3`.
- In C#: `-1 % 4` equals `-1`.
- **Convention**: Mathematical operations relying on negative wrapping must explicitly calculate the remainder safely (e.g., using a custom `LuaMod` helper or adding the base before modulo) to prevent out-of-bounds array lookups.

### Trigonometry (`Sin`, `Cos`, `Atan2`)
PICO-8 trigonometry operates on **Turns (0..1)**, not Radians or Degrees.
- **Deviation**: PICO-8's `Sin()` is vertically inverted compared to standard math (i.e., `Pico8Math.Sin(0.25f)` returns `-1`, not `1`).
- **Convention**: Cartridges simply call `Sin(x)` and `Cos(x)`, which are routed through `Pico8Math` to forcefully multiply by `MathF.PI * 2f` and invert axes automatically.

### Deterministic Randomness (`Rnd`, `Srand`)
- **Convention**: Do not use `UnityEngine.Random`. Always use the API `Rnd(x)` and `Srand(seed)`. `Pico8Math` maintains a dedicated `System.Random` instance to ensure seeded procedural generation remains totally deterministic and decoupled from the Unity Host. 

---

## 3. Structural Conventions

### The Cartridge Lifecycle
Every translated game must inherit from the base [`Cartridge`](../../packages/com.hatiora.pico8/Runtime/Core/Cartridge.cs) and implement the three sacred methods:
```csharp
public override void Init() { ... }
public override void Update() { ... }
public override void Draw() { ... }
```
A Cartridge is strictly a logic container and holds **no engine state itself**. 

### Global API Access
In PICO-8 Lua, commands like `rectfill()` or `sfx()` are globally scoped.
In C#, the base `Cartridge` class exposes identical methods natively (e.g. `Rectfill()`, `Sfx()`). 
When a cartridge invokes `Btnp(7)`, the base class delegates the call directly to the internal [`Pico8Api`](../../packages/com.hatiora.pico8/Runtime/Api/Pico8Api.cs) facade. 

### Resolution & Scaling Conventions
While older standard translations lock rendering mathematically to the strict `128x128` Grid, our ecosystem supports dynamic scaling through `EngineSpec`.

A standard convention across translations (like `SortCartridge` or `BounceCartridge`) is exposing a High-Res toggle via `Btnp(7)`:
```csharp
float scale = _isHighRes ? P8.Width / 128f : 1f;

// Scaling draw calls natively 
Rectfill(0, (int)(110 * scale), (int)(128 * scale) - 1, (int)(128 * scale) - 1, 14);
```
- **Constraint**: If `scale` falls below `1.0f` (due to floating point math), rendering elements can collapse to size `0` and disappear. Math logic affecting visual width must ensure minimal bounds sizes.

