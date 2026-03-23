# Sort Cartridge (`sort.p8`)

## Overview
Auto-generated documentation for the `SortCartridge` engine translation.
*(Developer: fill in behavioral description of this cartridge)*

## Implementation Details
How the C# `SortCartridge` accurately replicates the Lua logic. 

### API Implementation Map
The following table outlines the native PICO-8 functions used in the original `.p8` script and their C# translation counterparts:

| API Function | Native Lua Script | C# Translation |
| :--- | :---: | :---: |
| `Btnp` | ✅ Used | ✅ Used |
| `Cls` | ✅ Used | ✅ Used |
| `Flr` | ✅ Used | ✅ Used |
| `Print` | ✅ Used | ✅ Used |
| `Rectfill` | ✅ Used | ✅ Used |
| `Rnd` | ✅ Used | ✅ Used |
| `Sfx` | ✅ Used | ✅ Used |
| `Spr` | ✅ Used | ❌ |
| `Sspr` | ❌ | ✅ Used |


### Raw C# Source
```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Sort
{
    public class SortCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Sort/sort/Sfx/sfx")?.text;
        public string MusicData => null; // sort.p8 doesn't have music
        public Texture2D GfxTexture => Resources.Load<Texture2D>("Sort/sort/Gfx/gfx");
        
        private bool _isHighRes = false;
        private int[] g;

        public override void Init()
        {
            // start in high res mode
            _isHighRes = true;
            // starting giraffe heights
            g = new int[] { 3, 5, 7, 2, 9, 1, 2 };
        }

        public override void Update()
        {
            if (Btnp(7)) 
            {
                _isHighRes = !_isHighRes;
            }

            // ❎ to sort
            if (Btnp(5))
            {
                // look for a pair of giraffees out of order
                for (int i = 0; i < 6; i++)
                {
                    if (g[i] > g[i + 1])
                    {
                        // the left one is taller, so swap them!
                        int temp = g[i];
                        g[i] = g[i + 1];
                        g[i + 1] = temp;
                        
                        Sfx(0);
                        
                        // just one swap for now!
                        break;
                    }
                }
            }
            
            // 🅾️ to randomize
            if (Btnp(4))
            {
                for (int i = 0; i < 7; i++)
                {
                    g[i] = Flr(Rnd(9));
                }
                
                Sfx(1);
            }
        }

        public override void Draw()
        {
            Cls(12); // PICO-8 pale blue background
            
            float scale = _isHighRes ? P8.Width / 128f : 1f;

            // Optional: Support the 128x128 bounding box
            int sw = (int)(128 * scale);
            int sh = (int)(128 * scale);
            
            // Replicating rectfill(0,110,127,127,14) -> green ground
            Rectfill(0, (int)(110 * scale), (int)(128 * scale) - 1, (int)(128 * scale) - 1, 14);
            
            Print("PRESS \u00CE TO RANDOMIZE", (int)(22 * scale), (int)(2 * scale), 7, CoordMode.Virtual, scale);
            Print("PRESS \u00D7 TO SORT", (int)(32 * scale), (int)(10 * scale), 7, CoordMode.Virtual, scale);
            
            for (int i = 0; i < 7; i++)
            {
                DrawGiraffe((i + 1) * 16, 110, g[i], scale);
            }
        }

        // draw a giraffe at x,y with neck length of l
        private void DrawGiraffe(int x, int y, int l, float scale)
        {
            int sw = 8;
            int sh = 8;
            int dw = sw * (int)scale;
            int dh = sh * (int)scale;
            int dx, dy;

            // body is 2 tiles wide!
            sw = 16;
            dw = sw * (int)scale;
            dx = (x - 8) * (int)scale;
            dy = (y - 8) * (int)scale;
            Sspr((33 % 16) * 8, (33 / 16) * 8, sw, sh, dx, dy, dw, dh);
            
            // reset sw and dw for 1 tile wide neck and head
            sw = 8;
            dw = sw * (int)scale;
            
            // neck for l segments
            for (int i = 1; i <= l; i++)
            {
                dx = x * (int)scale;
                dy = (y - 8 - i * 8) * (int)scale;
                Sspr((18 % 16) * 8, (18 / 16) * 8, sw, sh, dx, dy, dw, dh);
            }
            
            // put head on top
            dx = x * (int)scale;
            dy = (y - 16 - l * 8) * (int)scale;
            Sspr((2 % 16) * 8, (2 / 16) * 8, sw, sh, dx, dy, dw, dh);
        }
    }
}

```

## Cartridge Assets
The following non-code data blocks were detected in the original `.p8` cartridge:
- `__gfx__`
- `__label__`
- `__sfx__`

## Original Lua Source

### Raw Script Block
Directly extracted from `docs/references/pico-8/games/sort/sort.p8`:
```lua
-- File: sort.p8
-- sorting demo
-- by zep

-- starting giraffee heights
g = {3,5,7,2,9,1,2}

-- draw a giraffe at x,y with
-- neck length of l
function draw_giraffe(x,y,l)

	-- body
	spr(33,x-8,y-8,2,1)
	
	-- neck for l segments
	for i=1,l do
		spr(18, x,y-8-i*8)
	end
	
	-- put head on top
	spr(2, x,y-16-l*8)

end

function _draw()
	cls(12)
	rectfill(0,110,127,127,14)
	
	print("press 🅾️ to randomize",
	      22, 2, 7)
	print("press ❎ to sort",
	      32, 10, 7)
	
	for i=1,7 do
		draw_giraffe(i*16,110,g[i])
	end

end

function _update()
	
	-- ❎ to sort
	
	if (btnp(5)) then
		
		-- look for a pair of
		-- giraffees out of order
		
		for i=1,6 do
			
			if (g[i] > g[i+1]) then
				
				-- the left one is taller,
				-- so swap them!
				temp   = g[i]
				g[i]   = g[i+1]
				g[i+1] = temp
				
				sfx(0)
				
				-- just one swap for now!
				break 
				
			end
		end
		
	end
	
	
	-- 🅾️ to randomize
	
	if (btnp(4)) then
		
		for i=1,7 do
			g[i]=flr(rnd(9))
		end
		
		sfx(1)
		
	end


end


```

## EditMode Tests
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.sort/Tests/Editor/SortCartridgeTests.cs`.

