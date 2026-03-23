# Hello Cartridge (`hello.p8`)

## Overview
PICO-8's "Hello World" — a wavy, rainbow-colored text animation spelling out the engine's logo letters. Each letter sprite oscillates with sine-wave motion at a different phase and color layer (pink through white), creating a shimmering cascade effect. Below the animation, "THIS IS PICO-8" and "NICE TO MEET YOU" are printed with a small heart sprite. Music plays on loop from pattern 0.

## Source Locations
- **Native Lua Script**: `docs/references/pico-8/games/hello/hello.p8`
- **C# Translation**: `packages/com.hatiora.pico8.hello/Runtime/HelloCartridge.cs`
- **Unit Tests**: `packages/com.hatiora.pico8.hello/Tests/Editor/HelloCartridgeTests.cs`

## Implementation Details

### Resolution & Coordinate Scaling
When `_isHighRes = true`, `scale = P8.Width / 128f`. All sprite draw positions and text coordinates are multiplied by `(int)scale`. Sprite rendering uses `Sspr` with explicitly scaled destination widths/heights (`dw = 8 * (int)scale`), keeping letters pixel-aligned.

### Key Routines

**Music** (`Init`) — `Music(0)` starts the looping music pattern. The original Lua calls `music(0)` at the top-level scope; in C# this is placed in `Init()`.

**Letter Wave Animation** (`Draw`) — A double-nested loop iterates:
- Outer: `col` from 14 down to 7 — eight color layers (pink → white) drawn back-to-front so white is on top.
- Inner: `i` from 1 to 11 — the 11 letter sprites (sprites 17–27 on the spritesheet).

Each letter's position is calculated as:
- `x = 8 + i*8 + cos(t1/90) * 3` — horizontal wobble
- `y = 38 + (col-7) + cos(t1/50) * 5` — vertical bounce

Where `t1 = Time()*30 + i*4 - col*2` creates per-letter and per-layer phase offsets. `Pal(7, col)` remaps white to the current layer color before drawing.

**Static Text & Heart** — Two `Print` calls render the greeting text. Sprite 1 (heart ♥) is drawn centered below via `Sspr`.

### API Implementation Map

| API Function | Native Lua | C# Translation | Deviation Rationale |
| :--- | :---: | :---: | :--- |
| `Btnp` | ❌ | ✅ Used | Added `Btnp(7)` for high-res toggle (not in original Lua). |
| `Cls` | ✅ Used | ✅ Used | Direct mapping. |
| `Cos` | ✅ Used | ✅ Used | Direct mapping (PICO-8 turn-based trigonometry). |
| `Flr` | ❌ | ✅ Used | Explicit `Flr(x)` and `Flr(y)` to truncate float positions to integers before scaling. Lua auto-truncates in `spr()`. |
| `Music` | ✅ Used | ✅ Used | Lua calls `music(0)` at top-level scope; C# places it in `Init()`. |
| `Pal` | ✅ Used | ✅ Used | Direct mapping. Remaps white (7) to each color layer. |
| `Palt` | ❌ | ✅ Used | `Palt()` reset added after each sprite draw to clear transparency state. Not explicitly called in Lua. |
| `Print` | ✅ Used | ✅ Used | Coordinates scaled by `scale`. Uses `CoordMode.Virtual` with scale parameter. |
| `Spr` | ✅ Used | ❌ | Replaced with `Sspr` to support resolution scaling. `spr(16+i, x, y)` → `Sspr(sx, sy, 8, 8, dx, dy, dw, dh)` with computed source rect and scaled destination. |
| `Sspr` | ❌ | ✅ Used | Used as replacement for `Spr` to support scaled destination sizes at high-res. |
| `Time` | ✅ Used | ✅ Used | Lua uses `t()` shorthand; C# uses `Time()`. |

### Raw C# Source
```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Hello
{
    public class HelloCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Hello/hello/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Hello/hello/Music/music")?.text;
        public string MapData   => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Hello/hello/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Hello/hello/Label/label");
        
        private bool _isHighRes = true;

        public override void Init() => Music(0);

        public override void Update()
        {
            if (Btnp(7)) _isHighRes = !_isHighRes;
        }

        public override void Draw()
        {
            Cls();

            // The underlying VRAM dimensions are determined by the engine configuration (P8.Width/Height).
            // We scale up to simulate the native 128x128 PICO-8 grid within those physical bounds.
            float scale = _isHighRes ? P8.Width / 128f : 1f;

            for (int col = 14; col >= 7; col--)
            {
                for (int i = 1; i <= 11; i++)
                {
                    float t1 = Time() * 30f + i * 4f - col * 2f;
                    
                    float x = 8f + i * 8f + Cos(t1 / 90f) * 3f;
                    float y = 38f + (col - 7f) + Cos(t1 / 50f) * 5f;
                    
                    Pal(7, col);
                    
                    int spriteIndex = 16 + i;
                    int sx = (spriteIndex % 16) * 8;
                    int sy = (spriteIndex / 16) * 8;
                    
                    int px = Flr(x);
                    int py = Flr(y);
                    
                    int dx = px * (int)scale;
                    int dy = py * (int)scale;
                    int sw = 8;
                    int sh = 8;
                    int dw = sw * (int)scale;
                    int dh = sh * (int)scale;

                    Sspr(sx, sy, sw, sh, dx, dy, dw, dh);
                    
                    Palt();
                }
            }

            Print("THIS IS PICO-8", 37 * (int)scale, 70 * (int)scale, 14, CoordMode.Virtual, scale);
            Print("NICE TO MEET YOU", 34 * (int)scale, 80 * (int)scale, 12, CoordMode.Virtual, scale);
            
            Sspr((1 % 16) * 8, (1 / 16) * 8, 8, 8, 
                 (64 - 4) * (int)scale, 90 * (int)scale, 
                 8 * (int)scale, 8 * (int)scale);
        }
    }
}

```

## Cartridge Assets
The following non-code data blocks were detected in the original `.p8` cartridge:
- `__gfx__` — spritesheet (128×128 PNG, contains letter sprites and heart)
- `__label__` — cartridge thumbnail (128×128 PNG)
- `__sfx__` — sound effects
- `__music__` — music patterns (looping from pattern 0)

## Original Lua Source

### Raw Script Block
Directly extracted from `docs/references/pico-8/games/hello/hello.p8`:
```lua
-- File: hello.p8
-- hello world
-- by zep

music(0)

function _draw()
	cls()
	
	-- for each color
	-- (from pink -> white)
	
	for col = 14,7,-1 do
		
		-- for each letter
		for i=1,11 do
		
			-- t() is the same as time()
			t1 = t()*30 + i*4 - col*2
			
			-- position
			x = 8+i*8     +cos(t1/90)*3
			y = 38+(col-7)+cos(t1/50)*5
			pal(7,col)
			spr(16+i, x, y)
		end
 end
	
	print("this is pico-8",
	 37, 70, 14) 
	print("nice to meet you",
	 34, 80, 12) 
	spr(1, 64-4, 90) -- ♥
end


```

## EditMode Tests
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.hello/Tests/Editor/HelloCartridgeTests.cs`.

