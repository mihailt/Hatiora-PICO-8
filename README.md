# Hatiora PICO-8

Hatiora PICO-8 is a Unity-based runtime for building PICO-8-style games in C#. It reproduces key parts of the PICO-8 programming model and cartridge workflow inside Unity, while opening the door to the wider Unity ecosystem and deployment targets.

The repository includes the runtime, the Unity host application, editor tooling, and a growing library of cartridge ports and experiments.

![Hatiora PICO-8 demo](docs/assets/demo.webp)

Hatiora PICO-8 is designed for developers who like the clarity of the PICO-8 model, but want to work in a native Unity environment.

That means:

- game code is written in C#
- projects are not limited by PICO-8 token constraints
- games can use Unity systems when needed, including rendering, input, audio, UI, physics, tooling, editor workflows, integrations, and platform support
- the same game can target any platform Unity supports

## Repository Structure

The repository is organized into five main areas:

- `apps/Hatiora` - the Unity host application
- `packages/com.hatiora.pico8` - the core runtime
- `packages/com.hatiora.pico8.unity` - the Unity integration layer
- `packages/com.hatiora.pico8.tools` - editor tooling for cartridge extraction and maintenance
- `packages/com.hatiora.pico8.*` - cartridge packages

## How It Works

The host entry point is `apps/Hatiora/Assets/Scripts/App.cs`.

At startup, the host application:

1. creates the UI root and audio provider
2. builds the input provider for keyboard, mouse, and gamepad
3. defines the shared `EngineSpec`
4. registers cartridges through `CartridgeRegistry`
5. registers `LauncherCartridge` as the system shell
6. mounts the runtime through `Pico8Builder`

At runtime, the host advances the active cartridge through `_builder.Tick(Time.deltaTime)`.

## Programming Model

The programming model is centered on three types:

- `Cartridge` is the authoring surface. Game code inherits from it and implements `Init()`, `Update()`, and `Draw()`.
- `IPico8` is the game-facing API contract.
- `Pico8Api` implements `IPico8` and connects cartridge code to the runtime subsystems.

Through `Cartridge`, game code can use a PICO-8-style API for:

- graphics: `Cls`, `Spr`, `Sspr`, `Map`, `Print`, `Camera`, `Clip`, `Pal`, `Palt`, `Fillp`
- input: `Btn`, `Btnp`
- audio: `Sfx`, `Music`
- math: `Rnd`, `Flr`, `Sin`, `Cos`, `Atan2`, `Mid`
- memory: `Peek`, `Poke`, `Memcpy`, `Memset`
- system: `Time`, `Stat`, `Reload`, `MenuItem`

This keeps game code compact and familiar while still running as native Unity-managed C#.

## Unity Integration

`com.hatiora.pico8.unity` turns the core runtime into a usable Unity stack.

It provides:

- `Pico8Builder` for wiring cartridges, graphics, input, audio, fonts, launcher support, and resources
- `CartridgeRegistry` for named registration and deferred cartridge switching
- `Pico8View` for presenting the active cartridge in the host UI
- Unity resource loading through `IUnityCartridge`

The runtime stays intentionally small at the game layer, but it is built to operate inside the full Unity environment when a project needs more than the fantasy-console baseline.

## Cartridge Library

The current host application registers these cartridges:

- `api`
- `automata`
- `basic`
- `bounce`
- `cast`
- `collide`
- `coop`
- `dots3d`
- `drippy`
- `hello`
- `jelpi`
- `sort`
- `wander`
- `waves`

`launcher` is used as the system shell rather than as a normal game entry.

Most of these packages are ports of PICO-8 demo cartridges. `coop` is not a default PICO-8 demo cartridge; it is a custom sample used to explore couch co-op and sprite rotation in the runtime.

A typical cartridge package contains:

- `Pico8/*.p8` source data
- `Runtime/*Cartridge.cs` translated or custom logic
- `Runtime/Resources/...` extracted graphics, label, map, flags, SFX, and music
- `Tests/...` cartridge tests
- `README.md` cartridge-specific notes

## Tooling

`com.hatiora.pico8.tools` supports cartridge extraction and ongoing package maintenance.

It handles:

- cartridge discovery
- progress tracking in `CartridgeProgress.json`
- `.p8` section extraction
- conversion of `__gfx__` and `__label__` blocks into Unity PNG assets
- maintenance of package structure for cartridge work

## Documentation

- [System architecture](docs/pico8/Architecture.md)
- [Engine implementation](docs/pico8/PICO-8.md)
- [Cartridge model](docs/pico8/Cartridges.md)
- [Tooling and ecosystem](docs/pico8/Ecosystem.md)
- [Lua to C# porting guide](docs/pico8/PortingGuide.md)
- [Language differences and conventions](docs/pico8/LanguageDifferencesAndConventions.md)

## License

This project is released under the [MIT License](LICENSE).

## Recommended Reading Order

For code-first onboarding, read in this order:

1. [apps/Hatiora/Assets/Scripts/App.cs](apps/Hatiora/Assets/Scripts/App.cs)
2. [packages/com.hatiora.pico8.unity/Runtime/Core/Pico8Builder.cs](packages/com.hatiora.pico8.unity/Runtime/Core/Pico8Builder.cs)
3. [packages/com.hatiora.pico8.unity/Runtime/Core/CartridgeRegistry.cs](packages/com.hatiora.pico8.unity/Runtime/Core/CartridgeRegistry.cs)
4. [packages/com.hatiora.pico8/Runtime/Core/Cartridge.cs](packages/com.hatiora.pico8/Runtime/Core/Cartridge.cs)
5. [packages/com.hatiora.pico8/Runtime/Api/IPico8.cs](packages/com.hatiora.pico8/Runtime/Api/IPico8.cs)
6. [packages/com.hatiora.pico8/Runtime/Api/Pico8Api.cs](packages/com.hatiora.pico8/Runtime/Api/Pico8Api.cs)
7. a small cartridge such as [packages/com.hatiora.pico8.hello/Runtime/HelloCartridge.cs](packages/com.hatiora.pico8.hello/Runtime/HelloCartridge.cs) or [packages/com.hatiora.pico8.automata/Runtime/AutomataCartridge.cs](packages/com.hatiora.pico8.automata/Runtime/AutomataCartridge.cs)

After that, move to larger cartridges such as `cast`, `collide`, or `jelpi`.
