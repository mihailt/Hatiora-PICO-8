# Understanding Cartridges in the Output Ecosystem

In this ecosystem, a **Cartridge** is a native Unity C# implementation of an original PICO-8 `.p8` Lua file. We have designed our system so that translating from Lua to C# is nearly a 1:1 syntactic process, wrapped within a highly-performant Unity-native structure.

This document explains what a Cartridge is, its structural requirements, and how developers can build and run them.

---

## 🏗️ What is a Cartridge?

A Cartridge is essentially a standard C# class that inherits from `Hatiora.Pico8.Cartridge`. 
Under the hood, this inheritance provides direct access to the entire `IPico8` hardware abstraction layer—meaning functions like `Spr()`, `Circfill()`, `Mget()`, `Btn()`, and `Sfx()` exist as globally accessible methods exactly as they do in standard PICO-8 Lua.

### The `IUnityCartridge` Interface
For cartridges that require external Unity assets (like extracted sound effects, music, or physical spritesheet Textures), we append the `IUnityCartridge` interface. This allows the application container to intelligently bind Unity `Resources` before booting the cartridge.

```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Hello
{
    public class HelloCartridge : Cartridge, IUnityCartridge
    {
        // PICO-8 Boot specifications (null = default 128x128)
        public override EngineSpec Spec => null; 
        
        // IUnityCartridge Requirements (Path to extracted .wav / .png assets)
        public string SfxData   => Resources.Load<TextAsset>("Hello/hello/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Hello/hello/Music/music")?.text;
        public Texture2D GfxTexture => Resources.Load<Texture2D>("Hello/hello/Gfx/gfx");

        private bool _isHighRes = true;
    }
}
```

---

## ⚙️ The Game Loop (`_init`, `_update`, `_draw`)

In standard PICO-8 Lua, developers write `_init()`, `_update()`, and `_draw()` functions.
In our C# ecosystem, these map directly to three overridden methods:

1. **`Init()`**: Called once when the engine boots. Used for setting up initial variables and clearing the screen.
2. **`Update()`**: Called every frame by Unity's game loop. By default, its execution frequency depends on Unity's frame rate, unless explicitly locked (e.g., `Application.targetFrameRate = 30` or `60` in the App container) to replicate original PICO-8 timings.
3. **`Draw()`**: Called every frame after Update. Used exclusively for rendering API calls like `Rectfill()`, `Spr()`, and `Print()`.

### Example Implementation (`HelloCartridge`)

```csharp
public override void Init() => Music(0);

public override void Update()
{
    // Toggle High Res mode if START button is pressed
    if (Btnp(7)) _isHighRes = !_isHighRes;
}

public override void Draw()
{
    Cls();

    // Scale logic
    float scale = _isHighRes ? P8.Width / 128f : 1f;

    // Real API drawing example
    Print("THIS IS PICO-8", 37 * (int)scale, 70 * (int)scale, 14, CoordMode.Virtual, scale);
    Print("NICE TO MEET YOU", 34 * (int)scale, 80 * (int)scale, 12, CoordMode.Virtual, scale);
}
```

---

## 🎮 Running a Cartridge in Unity (`App.cs`)

Cartridges cannot run themselves; they are pure logic modules. To display a Cartridge on the screen, hear its audio, and pass keyboard/gamepad inputs to it, it must be injected into the **`Pico8Builder`**.

In our Unity environment, this happens inside `App.cs`. 

```csharp
Pico8View MakeGame(Cartridge cart, int texW, int texH, int dispW, int dispH)
{
    var spec = new EngineSpec
    {
        ScreenWidth = texW,
        ScreenHeight = texH,
        PhysicalWidth = dispW,
        PhysicalHeight = dispH,
        MemoryMap = MemoryMap.Pico8,
    };

    // Connect Unity Input, Audio, and Video abstraction layers
    // Note: Pico8Builder automatically parses IUnityCartridge for audio assets
    var engine = new Pico8Builder()
        .WithCartridge(cart)
        .WithSpec(spec)
        .WithAudio(_audioProvider)
        .WithInput(inputProvider)
        .Build();

    // Return the visual UI element that displays the game
    return new Pico8View(engine, dispW, dispH);
}

// In Awake():
_view = MakeGame(
    new Hatiora.Pico8.Hello.HelloCartridge(), 
    1024, 1024, 
    1024, 1024
);
```

---

## 🛠️ The Developer Tooling Workflow

Translating a cartridge manually from scratch requires creating folders, writing `.asmdef` files, and typing out dozens of boilerplate classes. **Do not do this manually.**

We have built a dedicated **Cartridge Manager Window** in Unity (`PICO-8 > Settings > Tools`) to automate this workflow.

1. **Wizard Tab**: Drag and drop your `.p8` and `.lua` files into the Cartridge Creator.
2. **Generation**: The tools will automatically construct the package inside `packages/com.hatiora.pico8.[name]`.
3. **Extraction**: The tools will automatically rip and translate the `.p8` Graphics and Sound data into Unity-compatible `.png` and `.wav` files inside the `Runtime/Resources/` folder.
4. **Scaffolding**: The tools will generate a `[Name]Cartridge.cs` boilerplate class.

### Translating the Code
Once the Cartridge structure is scaffolded by the Maker tool, your primary job is to open `[Name]Cartridge.cs` and translate the raw `__lua__` block from the original code into C#. 

Because our `Cartridge` environment mirrors the native PICO-8 API natively, translating loops, variables, and drawing commands is mostly a matter of converting Lua's `end` syntax into standard C# curly braces `{ }`, and adding explicit floating-point/integer types (`float x = 0f;`).

