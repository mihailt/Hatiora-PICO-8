# Basic Cartridge (`basic.p8`)

## Overview
Auto-generated documentation for the `BasicCartridge` engine translation.
*(Developer: fill in behavioral description of this cartridge)*

## Implementation Details
How the C# `BasicCartridge` accurately replicates the Lua logic. 

### API Implementation Map
The following table outlines the native PICO-8 functions used in the original `.p8` script and their C# translation counterparts:

| API Function | Native Lua Script | C# Translation |
| :--- | :---: | :---: |
| `Cls` | ✅ Used | ✅ Used |
| `Cos` | ✅ Used | ✅ Used |
| `Flr` | ❌ | ✅ Used |
| `Music` | ✅ Used | ✅ Used |
| `Pal` | ✅ Used | ✅ Used |
| `Palt` | ❌ | ✅ Used |
| `Print` | ✅ Used | ✅ Used |
| `Spr` | ✅ Used | ❌ |
| `Sspr` | ❌ | ✅ Used |
| `Time` | ✅ Used | ✅ Used |


### Raw C# Source
```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Basic
{
    public class BasicCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Basic/basic/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Basic/basic/Music/music")?.text;
        public string MapData   => null;
        public string GffData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Basic/basic/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Basic/basic/Label/label");
        

        public override void Init() => Music(0);

        public override void Update()
        {
        }

        public override void Draw()
        {
            Cls();

            // The underlying VRAM dimensions are determined by the engine configuration (P8.Width/Height).
            // We scale up to simulate the native 128x128 PICO-8 grid within those physical bounds.
            float scale = ContentScale;

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
- `__gfx__`
- `__label__`
- `__sfx__`
- `__music__`

## Original Lua Source

### Raw Script Block
Directly extracted from `docs/references/pico-8/games/basic/basic.p8`:
```lua
-- File: basic.p8
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
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.basic/Tests/Editor/BasicCartridgeTests.cs`.
