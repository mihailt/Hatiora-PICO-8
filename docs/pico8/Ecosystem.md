# Cartridge Ecosystem and Tooling

Due to the tedious nature of manually translating Lua routines to a Unity C# infrastructure, we developed a powerful workflow tooling ecosystem under `com.hatiora.pico8.tools`. These tools handle code generation, Unity Package Manager (UPM) integration, audio/visual extraction, and structural testing natively within the Editor. 

---

## 🖥 The Cartridge Manager UI

A unified Editor Window under `PICO-8 > Settings > Tools` ([CartridgeManagerWindow.cs](../../packages/com.hatiora.pico8.tools/Editor/CartridgeManagerWindow.cs)). It orchestrates all the underlying processes inside [CartridgeService](../../packages/com.hatiora.pico8.tools/Editor/CartridgeService.cs).

- **Cartridges Tab**: Displays active Cartridges in the ecosystem tracking Port/Test/Documentation status. States are stored in a centralized `CartridgeProgress.json` file.
- **Wizard Tab**: The Cartridge Package Creator. Users input a `.p8` ROM here to bootstrap a robust Unity Package that complies with our framework constraints.

---

## 🗃 Packages & [CartridgeService](../../packages/com.hatiora.pico8.tools/Editor/CartridgeService.cs)

Every Cartridge translates into its own isolated module.

When bootstrapping a Cartridge, [CartridgeService](../../packages/com.hatiora.pico8.tools/Editor/CartridgeService.cs) automatically creates:
1. `packages/com.hatiora.pico8.[name]/package.json` (Registration as a Unity module)
2. `Runtime/[Name]Cartridge.cs` (C# logic container boilerplate)
3. `.asmdef` Definitions (Managing dependency references to core PICO-8 engine)
4. `Tests/` scaffolding.

All packages follow this UPM standard ensuring games are portable un-coupled assets.

---

## 💾 Resource Extraction ([P8Extractor.cs](../../packages/com.hatiora.pico8.tools/Editor/P8Extractor.cs))

Original `.p8` files are text-based blobs encoding hexadecimal pixel and audio tracks.

During the [CartridgeService](../../packages/com.hatiora.pico8.tools/Editor/CartridgeService.cs) bootstrapping, [P8Extractor.cs](../../packages/com.hatiora.pico8.tools/Editor/P8Extractor.cs) reads the `__gfx__`, `__sfx__`, and `__music__` sections of the `.p8` file.
- **GFX**: Hex codes are evaluated against standard PICO-8 palette mappings (Colors 0–15). [P8Extractor](../../packages/com.hatiora.pico8.tools/Editor/P8Extractor.cs) converts this into a standard transparent `Texture2D` and exports a `.png` file into the Cartridge's `Runtime/Resources/` folder.
- **Audio**: Sound configurations and frequencies are exported into raw `.wav` or text configurations that interact with the Unity `AudioSource`.

The exported `.png` sprite-sheet is then exposed sequentially via `IUnityCartridge.GfxTexture`.

---

## 📄 Automated Documentation ([CartridgeDocGenerator.cs](../../packages/com.hatiora.pico8.tools/Editor/CartridgeDocGenerator.cs))

Documentation of translations is vital for verifying logic constraints.

Upon request via the Cartridge UI, [CartridgeDocGenerator.cs](../../packages/com.hatiora.pico8.tools/Editor/CartridgeDocGenerator.cs) extracts the Lua code from `__lua__` inside the original `.p8` file. It parses PICO-8 API usage (e.g. `rectfill`, `mget`, `spr`) and maps them against their native [Pico8Api](../../packages/com.hatiora.pico8/Runtime/Api/Pico8Api.cs) usage equivalents. 

It outputs a robust `<CartridgeName>/README.md` (such as `docs/pico8/cartridges/hello/README.md`) detailing:
1. PICO-8 implementation API mapping
2. Non-code Assets Detected. 
3. The raw, complete internal C# code from the translated Cartridge itself.
4. The raw, direct `.p8` syntax for verification.

---

## 🧪 Structural Test Generation ([CartridgeTestGenerator.cs](../../packages/com.hatiora.pico8.tools/Editor/CartridgeTestGenerator.cs))

We utilize Unity Test Runner natively.

When generated, [CartridgeTestGenerator.cs](../../packages/com.hatiora.pico8.tools/Editor/CartridgeTestGenerator.cs) builds a `[Name]CartridgeTests.cs` scaffold in the package's `Tests/Editor` directory, ensuring:
- Game logic compiles and binds interfaces correctly (`IUnityCartridge`).
- Extracted graphics exist and dimensions are valid.
- Default initialization passes safely through [Pico8Builder](../../packages/com.hatiora.pico8.unity/Runtime/Core/Pico8Builder.cs).
