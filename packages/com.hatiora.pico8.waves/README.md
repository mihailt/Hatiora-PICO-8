# Waves Cartridge (`waves.p8`)

## Overview
A radial cosine wave visualizer. A grid of dots is plotted across a 128×128 region, with each dot's vertical position displaced by `cos(dist/40 - t()) * 6` — where `dist` is the Euclidean distance from the center. This creates a rippling pond/wave animation that radiates outward from the center of the screen. The grid uses a step of 3 vertically and 2 horizontally, producing a sparse dot pattern. No audio or sprites are used.

## Source Locations
- **Native Lua Script**: `docs/references/pico-8/games/waves/waves.p8`
- **C# Translation**: `packages/com.hatiora.pico8.waves/Runtime/WavesCartridge.cs`
- **Unit Tests**: `packages/com.hatiora.pico8.waves/Tests/Editor/WavesCartridgeTests.cs`

## Implementation Details

### Resolution & Coordinate Scaling
When `_isHighRes = true`, `scale = P8.Width / 128f`. Two separate rendering branches handle high-res and native modes. In high-res mode, each logical pixel becomes a `scale × scale` block via `Rectfill`; in native mode, `Pset` is used directly.

### Key Routines

**Wave Calculation** (`Draw`) — A double loop iterates `y` from `-r` to `r` (step 3) and `x` from `-r` to `r` (step 2), where `r = 64`. For each point:
1. `dist = sqrt(x² + y²)` — radial distance from center.
2. `z = cos(dist/40 - Time()) * 6` — vertical displacement. The `-Time()` term makes the wave propagate outward over time. The amplitude is ±6 pixels.
3. Screen position: `(r + x, r + y - z)` — centers the grid at `(64, 64)` with Z lifting dots upward.

All dots are drawn in color 6 (light gray) on a black background, creating a monochrome wireframe effect.

### API Implementation Map

| API Function | Native Lua | C# Translation | Deviation Rationale |
| :--- | :---: | :---: | :--- |
| `Btnp` | ❌ | ✅ Used | Added `Btnp(7)` for high-res toggle (not in original Lua). |
| `Cls` | ✅ Used | ✅ Used | Direct mapping. |
| `Cos` | ✅ Used | ✅ Used | Direct mapping (PICO-8 turn-based trigonometry). |
| `Max` | ❌ | ✅ Used | `Mathf.Max(1, (int)scale)` prevents zero-size `Rectfill` in high-res mode. |
| `Pset` | ✅ Used | ✅ Used | Used only in native 128×128 mode. In high-res, replaced by `Rectfill`. |
| `Rectfill` | ❌ | ✅ Used | Replaces `Pset` in high-res mode — draws a `scale × scale` block per logical pixel to maintain visible dot density. |
| `Sqrt` | ✅ Used | ✅ Used | Direct mapping. Computes radial distance for wave function. |
| `Time` | ✅ Used | ✅ Used | Lua uses `t()` shorthand; C# uses `Time()`. |

### Raw C# Source
```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Waves
{
    public class WavesCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData => null;
        public string MusicData => null;
        public string MapData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Waves/waves/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Waves/waves/Label/label");

        private float r = 64;

        private bool _isHighRes = true;

        public override void Init() { }

        public override void Update() 
        { 
            if (Btnp(7)) _isHighRes = !_isHighRes;
            
        }
        
        public override void Draw()
        {
            Cls(0);
            
            float scale = _isHighRes ? P8.Width / 128f : 1f;

            for (float y = -r; y <= r; y += 3)
            {
                for (float x = -r; x <= r; x += 2)
                {
                    float dist = Sqrt(x * x + y * y);
                    float z = Cos(dist / 40f - Time()) * 6f;
                    
                    if (_isHighRes)
                    {
                        // Stretchy projection over High-Res pixels (simulated Pset filling area footprint = scale squared)
                        int px = (int)((r + x) * scale);
                        int py = (int)((r + y - z) * scale);
                        int fillWidth = Mathf.Max(1, (int)scale);
                        int fillHeight = Mathf.Max(1, (int)scale);
                        
                        Rectfill(px, py, px + fillWidth - 1, py + fillHeight - 1, 6);
                    }
                    else 
                    {
                        // Native 1x1 128px bounding box mapping
                        Pset((int)(r + x), (int)(r + y - z), 6);
                    }
                }
            }
        }
    }
}

```

## Cartridge Assets
The following non-code data blocks were detected in the original `.p8` cartridge:
- `__gfx__` — spritesheet (128×128 PNG, unused by this cartridge)
- `__label__` — cartridge thumbnail (128×128 PNG)

## Original Lua Source

### Raw Script Block
Directly extracted from `docs/references/pico-8/games/waves/waves.p8`:
```lua
-- File: waves.p8
-- waves demo
-- by zep

r=64

function _draw()
	cls()
		for y=-r,r,3 do
			for x=-r,r,2 do
				local dist=sqrt(x*x+y*y)
				z=cos(dist/40-t())*6
				pset(r+x,r+y-z,6)
		end
	end
end


```

## EditMode Tests
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.waves/Tests/Editor/WavesCartridgeTests.cs`.

