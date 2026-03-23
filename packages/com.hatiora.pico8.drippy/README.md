# Drippy Cartridge (`drippy.p8`)

## Overview
A drip-painting demo. The player moves a cursor with the D-pad, leaving a trail of color-cycling pixels (palette 8–15). Every frame, 800 randomly chosen pixels are checked — if they have color, a copy is painted one pixel below, creating a paint-dripping gravity effect. The screen starts with a dark-blue background and never clears, so drips accumulate over time.

## Source Locations
- **Native Lua Script**: `docs/references/pico-8/games/drippy/drippy.p8`
- **C# Translation**: `packages/com.hatiora.pico8.drippy/Runtime/DrippyCartridge.cs`
- **Unit Tests**: `packages/com.hatiora.pico8.drippy/Tests/Editor/DrippyCartridgeTests.cs`

## Implementation Details

### Resolution & Coordinate Scaling
When `_isHighRes = true`, `scale = P8.Width / 128f`. The drip algorithm always works in the 128×128 logical grid — random coordinates are generated in `[0, 128)` then scaled to physical pixels. Each "pixel" becomes a `fillSize × fillSize` block via `Rectfill`, preserving the chunky retro look at higher resolutions.

### Key Routines

**Initialization** (`Init`) — The original Lua runs `rectfill(0,0,127,127,1)` at the top-level scope (before `_init`). The C# translation places this in `Init()`, drawing the dark-blue background scaled to the active resolution. No `Cls()` is ever called — the persistent canvas is the core mechanic.

**Color Cycling Cursor** (`Draw`) — The cursor position `(x, y)` is drawn as a `fillSize × fillSize` block using the current color `c`, which cycles through palette indices 8–15 at `+1/8` per frame.

**Drip Algorithm** (`Update`) — Each frame:
1. 800 random virtual coordinates `(vx, vy)` are generated in `[0, 128)`.
2. Each is scaled to physical coordinates: `px = vx * scale`, `py = vy * scale`.
3. The pixel color at `(px, py)` is sampled via `Pget`.
4. If the color is > 1 (not black or dark-blue background), a same-colored block is drawn one virtual unit below (i.e., `fillSize` physical pixels down).

**Resolution Toggle** (`Update`) — `Btnp(7)` toggles between high-res and native 128×128 modes. On toggle, the entire surface is wiped and the blue background is redrawn at the new scale.

### API Implementation Map

| API Function | Native Lua | C# Translation | Deviation Rationale |
| :--- | :---: | :---: | :--- |
| `Btn` | ✅ Used | ✅ Used | Direct mapping for D-pad movement. |
| `Btnp` | ❌ | ✅ Used | Added `Btnp(7)` for high-res toggle (not in original Lua). |
| `Max` | ❌ | ✅ Used | `Mathf.Max(1, (int)scale)` prevents zero-size `Rectfill` when `scale < 1`. |
| `Pget` | ✅ Used | ✅ Used | Samples from scaled physical coordinates instead of virtual 128×128. |
| `Pset` | ✅ Used | ❌ | Replaced with `Rectfill` — a single `Pset` is invisible at high-res; a `fillSize × fillSize` block preserves the chunky pixel aesthetic. |
| `Rectfill` | ✅ Used | ✅ Used | Used for both background fill (original) and as `Pset` replacement (scaled). |
| `Rnd` | ✅ Used | ✅ Used | Direct mapping. Always generates in `[0, 128)` regardless of resolution. |

### Raw C# Source
```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Drippy
{
    public class DrippyCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData => null;
        public string MusicData => null;
        public string MapData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Drippy/drippy/Gfx/gfx");
        public Texture2D LabelTexture => null;

        private float x = 64f;
        private float y = 64f;
        private float c = 8f;

        private bool _isHighRes = true;
        
        public override void Init()
        {
            float scale = _isHighRes ? P8.Width / 128f : 1f;
            Rectfill(0, 0, P8.Width - 1, P8.Height - 1, 0); // Base wipe
            Rectfill(0, 0, (int)(128 * scale) - 1, (int)(128 * scale) - 1, 1);
        }

        public override void Update() 
        { 
            if (Btnp(7)) 
            {
                _isHighRes = !_isHighRes;
                float s = _isHighRes ? P8.Width / 128f : 1f;
                // Wipe the entire surface clean so trails don't persist outside the active screen bounds
                Rectfill(0, 0, P8.Width - 1, P8.Height - 1, 0);
                // Draw the blue background for the respective scaled screen size
                Rectfill(0, 0, (int)(128 * s) - 1, (int)(128 * s) - 1, 1);
            }
            
            float scale = _isHighRes ? P8.Width / 128f : 1f;

            if (Btn(0)) x -= 1f;
            if (Btn(1)) x += 1f;
            if (Btn(2)) y -= 1f;
            if (Btn(3)) y += 1f;

            c += 1f / 8f;
            if (c >= 16f) c = 8f;

            // Drip algorithm operates on the exact 128x128 logical grid to perfectly map the density.
            int drips = 800;

            for (int i = 0; i < drips; i++)
            {
                // Select a virtual coordinate explicitly on the 128 space
                int vx = (int)Rnd(128f);
                int vy = (int)Rnd(128f);
                
                // Map the virtual coordinate directly to the chunky visual scale grid
                int px = (int)(vx * scale);
                int py = (int)(vy * scale);
                
                int col = Pget(px, py);

                if (col > 1)
                {
                    // Move the block DOWN by exactly one Virtual unit (which is exactly `scale` physical pixels)
                    int fillSize = Mathf.Max(1, (int)scale);
                    Rectfill(px, py + fillSize, px + fillSize - 1, py + fillSize * 2 - 1, col); 
                }
            }
        }

        public override void Draw()
        {
            float scale = _isHighRes ? P8.Width / 128f : 1f;
            
            // Draw the actual block size to match scale
            int px = (int)(x * scale);
            int py = (int)(y * scale);
            int fillSize = Mathf.Max(1, (int)scale);
            Rectfill(px, py, px + fillSize - 1, py + fillSize - 1, (int)c); 
        }
    }
}

```

## Cartridge Assets
The following non-code data blocks were detected in the original `.p8` cartridge:
- `__gfx__` — spritesheet (128×128 PNG, unused by this cartridge)
- `__label__` — cartridge thumbnail (128×128 PNG)
- `__sfx__` — sound effects (unused by this cartridge)
- `__music__` — music patterns (unused by this cartridge)

## Original Lua Source

### Raw Script Block
Directly extracted from `docs/references/pico-8/games/drippy/drippy.p8`:
```lua
-- File: drippy.p8
-- drippy
-- by zep

rectfill(0,0,127,127,1)
x=64 y=64 c=8

function _draw()
	pset(x,y,c)
end

function _update()
	
	if (btn(0)) then x=x-1 end
	if (btn(1)) then x=x+1 end
	if (btn(2)) then y=y-1 end
	if (btn(3)) then y=y+1 end
	
	c=c+1/8
	if (c >= 16) then c = 8 end
	
	-- increase this number for
	-- extra drippyness
	for i=1,800 do 
	
	-- choose a random pixel
	local x2 = rnd(128)
	local y2 = rnd(128)
	local col = pget(x2,y2)
	
	--drip down if it is colourful
	if (col > 1) then
		pset(x2,y2+1,col) 
	end
	end
	
end


```

## EditMode Tests
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.drippy/Tests/Editor/DrippyCartridgeTests.cs`.

