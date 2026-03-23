# System Architecture (C4 Model)

This document provides a senior-level architectural overview of the PICO-8 Unity Ecosystem, adopting the **C4 Model** (Context, Containers, Components, and Code). It synthesizes the concepts defined in our [Cartridges](Cartridges.md), [PICO-8 Engine](PICO-8.md), and [Ecosystem Tooling](Ecosystem.md) specifications.

---

## Level 1: System Context 

The System Context illustrates how external users (Players and Developers) interact with the overarching PICO-8 Unity ecosystem.

```mermaid
C4Context
    title System Context Diagram for PICO-8 Unity Ecosystem
    
    Person(player, "Player", "A person playing the game")
    Person(developer, "Game Developer", "Developer building or porting games")
    
    System(unity, "Unity Host Application", "The Unity wrapper capturing input and rendering UI")
    System(pico8, "PICO-8 Native Runtime", "The core C# engine emulating PICO-8 behavior")
    System(tools, "Cartridge Dev Tools", "Editor-only toolkit translating .p8 data")
    
    System_Ext(p8file, "Original .p8 Lua Cartridge", "External raw PICO-8 file")

    Rel(player, unity, "Plays Game", "Input / Output")
    Rel(unity, pico8, "Drives tick cycle", "App.cs")
    
    Rel(developer, p8file, "Imports")
    Rel(developer, tools, "Scaffolds & Generates")
    Rel(tools, p8file, "Translates Data")
    Rel(tools, unity, "Generates C# Packages")
```

**Key Responsibilities:**
- **Unity Host Application**: The physical wrapper running on the target device. It captures hardware inputs (gamepads, keyboards) and renders Unity UI.
- **PICO-8 Native Runtime**: The strictly scoped C# engine responsible for emulating PICO-8 behavior without relying on Unity's ECS.
- **Cartridge Dev Tools**: The Editor-only ecosystem that translates raw `.p8` data into functional, compilable Unity packages.

---

## Level 2: Container Diagram

Zooming into the architecture, we separate the system into distinct deployable or logically grouped containers.

```mermaid
C4Container
    title Container Diagram for PICO-8 Framework
    
    System_Boundary(b1, "Unity Host") {
        Container(app, "App.cs", "C#, Unity", "The entry point and composition root")
        Container(picoview, "Pico8View", "UI Toolkit", "Displays the rendered texture")
    }

    System_Boundary(b2, "PICO-8 Framework") {
        Container(builder, "Pico8Builder", "C#", "Constructs the immutable engine")
        Container(engine, "Pico8Engine", "C#", "Orchestrates game loop")
        Container(cart, "Translated Cartridge", "C#", "Scoped game logic")
    }

    System_Boundary(b3, "Editor Tooling") {
        Container(cartman, "CartridgeService & UI", "C#, Editor", "Manages the ecosystem packages")
        Container(extractors, "Extractors / Generators", "C#", "Extracts GFX/SFX, gen code")
    }

    Rel(app, cart, "1. Instantiates")
    Rel(app, builder, "2. Configures")
    Rel(builder, cart, "3. Mounts")
    Rel(builder, engine, "4. Constructs")
    Rel(app, picoview, "5. Mounts Texture to")
    Rel(app, engine, "6. Executes Tick()")
    
    Rel(cartman, extractors, "Extracts Assets via")
    Rel(extractors, cart, "Generates Boilerplate for")
```

**Key Interactions:**
- **`App.cs`**: The ultimate composition root. It wires Unity's implementations of Audio/Input into the framework.
- **`Pico8Builder`**: A fluent factory. It consumes a `Cartridge` and an `EngineSpec`, wiring all internal sub-systems together before returning an immutable `Pico8Engine`.
- **`Pico8View`**: A Unity `VisualElement` (UI Toolkit) that strictly acts as a dumb display. It pushes the `TextureGraphics` output to the player's screen.

---

## Level 3: Component Diagram (The Engine Runtime)

This level breaks open the `Pico8Engine` to visualize the tight internal dependency graph and memory flows that dictate the virtual PICO-8 hardware.

```mermaid
C4Component
    title Component Diagram for Pico8Engine runtime
    
    Container_Boundary(b1, "Pico8Engine Structure") {
        Component(api, "Pico8Api", "Facade", "Routes Cartridge calls")
        Component(mem, "Pico8Memory", "32KB RAM", "Byte array with pinned memory map")
        Component(math, "Pico8Math", "Math Utils", "Fixed routines")
        
        Container_Boundary(b2, "Rendering Pipeline") {
            Component(drawstate, "DrawState", "State", "Active Camera, Clip Box, Palettes")
            Component(pixelbuf, "PixelBuffer", "Virtual Buffer", "Virtual pixel math with RAM sync")
            Component(texgfx, "TextureGraphics", "Physical Buffer", "Physical Color32 Texture map")
        }
        
        Container_Boundary(b3, "Data Repositories") {
            Component(sprites, "SpriteStore", "State", "Sprite sheets")
            Component(map, "MapStore", "State", "Map data")
            Component(pal, "Palette", "State", "Color indices")
        }
    }

    Component(cart, "User Cartridge", "C# logic", "User's translated game")
    System_Ext(unityGPU, "Unity GPU", "Device graphics hardware")

    Rel(cart, api, "Invokes P8.* commands")
    
    Rel(api, mem, "Peek/Poke/Memcpy")
    Rel(api, pixelbuf, "Memcpy screen sync")
    Rel(api, pixelbuf, "Draw API (Spr, Rect)")
    Rel(api, drawstate, "Camera/Clip/Color")
    Rel(api, math, "Calculate")
    
    Rel(pixelbuf, drawstate, "Reads State")
    Rel(pixelbuf, sprites, "Reads Tile/Pixels")
    Rel(pixelbuf, map, "Reads Map")
    Rel(pixelbuf, texgfx, "Writes pixels on Flush")
    
    Rel(texgfx, unityGPU, "Flushes to GPU")
```

**Component Breakdown:**
1. **The Cartridge (`Cartridge.cs`)**: Contains strictly standard `Init()`, `Update()`, and `Draw()` logic inherited from the `.p8` translation. It holds no engine state itself.
2. **The Facade (`Pico8Api`)**: The gatekeeper. It implements `IPico8` and routes all Cartridge calls (like `P8.Circfill()`, `P8.Btn()`) to the correct internal subsystem. When `Memcpy` touches the screen memory region (`0x6000+`), `Pico8Api` automatically syncs the `PixelBuffer` ↔ RAM (`FlushToRam` → raw copy → `LoadFromRam`).
3. **The Rendering Pipeline**:
    - **`DrawState`**: Pure data structure holding the active Camera, Clip Box, and Palette Mappings for the current frame.
    - **`PixelBuffer`**: Math-heavy module calculating pixel modifications in purely virtual C# `byte[]` arrays. Entirely decoupled from Unity logic. Resolves `EngineSpec.Scale` coordinates. Supports bidirectional sync with `Pico8Memory.Ram` via `FlushToRam()` / `LoadFromRam()` for memory-mapped screen effects (e.g. `Memcpy`-based scrolling).
    - **`TextureGraphics`**: The bridge. On `Flush()`, it maps the `byte[]` through the `Palette` into physical `Color32` arrays, writing directly to Unity's `Texture2D` memory block for extreme flush performance.

---

## Level 4: Code & Data Architecture Details

While UML class diagrams are often overkill, two specific data architectures dictate our ecosystem limitations and expansions:

### 1. The `EngineSpec` Boot Configuration
Instead of hard-coding `128x128`, the system relies entirely on `EngineSpec`. Component allocations (Memory, PixelBuffer, Textures) are entirely dynamic based on the configuration injected during `Pico8Builder.WithSpec()`. 
- **Virtual Resolution**: Game space coordinates.
- **Physical Resolution**: Output texture dimensions.
- **CoordMode**: Evaluates whether an API draw call sits on the virtual grid (upscaled) or the physical grid (1:1 with output).

### 2. The Centralized Tool Store (`CartridgeProgress.json`)
The ecosystem manages all Cartridge package generation data through a singular non-volatile store. 
Instead of polluting individual `package.json` registries, the Editor Tooling (`CartridgeService`) serializes all extraction statuses, migration ports, and doc generations into `CartridgeProgress.json` tracking the ecosystem health natively across branches.
