# Api Cartridge (`api.p8`)

## Overview
The API cartridge is PICO-8's official demonstration of most built-in API functions. It renders a dark-blue scene with background stars and clouds (via the tile map), a planet-like circle with a fill-pattern overlay, spinning bunny sprites, a rotating ring of star sprites, palette-colored lines, and a button-state indicator grid. Music plays on loop, and pressing 🅾️ triggers an SFX. Holding ❎ activates camera panning with clipping.

## Source Locations
- **Native Lua Script**: `docs/references/pico-8/games/api/api.p8`
- **C# Translation**: `packages/com.hatiora.pico8.api/Runtime/ApiCartridge.cs`
- **Unit Tests**: `packages/com.hatiora.pico8.api/Tests/Editor/ApiCartridgeTests.cs`

## Implementation Details

### Resolution & Coordinate Scaling
The cartridge defaults to `_isHighRes = true` where `scale = 1f` (native 128×128 mode). When toggled via `Btnp(7)`, `scale = P8.Width / 128f` stretches all coordinates and sprite draws to fill the high-res surface. All drawing coordinates are multiplied by `scale` and cast to `int` to keep to pixel-grid alignment.

### Key Routines

**Map Drawing** — `Map(0, 0, 0, 0, 128, 64)` renders the entire tile map, which contains background stars (tiles `0x26`, `0x27`) and cloud formations (tiles `0x06`, `0x07`, `0x16`) embedded in the map data. The map data is loaded from `map.txt` by the engine's `MapDataLoader` during `Pico8Builder.Build()`.

**Planet Circle** — Two `Circfill`/`Circ` calls draw a filled beige planet and an outline. A `Fillp(0b0101101001011010)` dithered overlay adds visual depth.

**Spinning Bunnies** — The bunny sprite (4×4 tiles at sprite 2) is drawn via `Sspr` with `cos(t/2)*32` controlling the width, producing a horizontal spin effect. The same approach is used vertically. When `w < 0` (back-facing), `Pal(7, 13)` recolors the sprite indigo.

**Rotating Star Ring** — 32 star sprites orbit around the planet at `(64, 160)` with a radius of 57. Each star's color is sampled from the spritesheet via `Sget`, then remapped with `Pal(7, col)` before drawing.

**Button State Grid** — A nested loop over 8 players × 8 buttons draws a 2×2 pixel grid using `Rectfill`. Active buttons highlight with color `b+7`.

### API Implementation Map

| API Function | Native Lua | C# Translation | Deviation Rationale |
| :--- | :---: | :---: | :--- |
| `Abs` | ❌ | ✅ Used | Used for stretched sprite width calculation (`Mathf.Abs`). |
| `Btn` | ✅ Used | ✅ Used | Direct mapping. Player index shifted (P0 → row 7) to match input layout. |
| `Btnp` | ✅ Used | ✅ Used | Added `Btnp(7)` for high-res toggle (not in original Lua). |
| `Camera` | ✅ Used | ✅ Used | Coordinates scaled by `scale`. |
| `Circ` | ✅ Used | ✅ Used | Radius scaled by `scale`. |
| `Circfill` | ✅ Used | ✅ Used | Radius scaled by `scale`. |
| `Clip` | ✅ Used | ✅ Used | Dimensions scaled by `scale`. |
| `Cls` | ✅ Used | ✅ Used | Direct mapping. |
| `Cos` | ✅ Used | ✅ Used | Direct mapping. |
| `Cursor` | ✅ Used | ❌ | Replaced by explicit `Print` coordinates — `cursor()` implicit text positioning is not supported. |
| `Fillp` | ✅ Used | ✅ Used | Fill pattern value `░` mapped to binary `0b0101101001011010`. |
| `Line` | ✅ Used | ✅ Used | Coordinates scaled by `scale`. |
| `Map` | ✅ Used | ✅ Used | Lua calls `map()` with no args (defaults to full map). C# passes explicit `Map(0, 0, 0, 0, 128, 64)`. |
| `Max` | ❌ | ✅ Used | Used in `Mathf.Max(1, (int)scale)` to prevent zero-size fills. |
| `Music` | ✅ Used | ✅ Used | Direct mapping. |
| `Pal` | ✅ Used | ✅ Used | Direct mapping. Used for star colors and bunny back-facing. |
| `Palt` | ✅ Used | ✅ Used | Direct mapping. |
| `Print` | ✅ Used | ✅ Used | Coordinates scaled by `scale`. Uses `CoordMode.Virtual` with scale parameter. |
| `Pset` | ✅ Used | ❌ | Button grid uses `Rectfill` instead of `Pset` to produce visible pixels at higher resolutions. |
| `Rect` | ✅ Used | ✅ Used | Coordinates scaled by `scale`. |
| `Rectfill` | ✅ Used | ✅ Used | Coordinates scaled by `scale`. Also used as `Pset` replacement for button grid. |
| `Sfx` | ✅ Used | ✅ Used | Direct mapping. |
| `Sget` | ✅ Used | ✅ Used | Direct mapping. Reads color from spritesheet for star palette. |
| `Sin` | ✅ Used | ✅ Used | Star ring uses `Mathf.Sin`/`Mathf.Cos` (C# standard trig) instead of PICO-8's `sin`/`cos` (normalize-turn based). The negation on `sy` compensates for direction. |
| `Spr` | ✅ Used | ❌ | Replaced with `Sspr` to support resolution scaling. `spr(2, x, y, 4, 4)` → `Sspr(16, 0, 32, 32, ...)`. Star sprites similarly use `Sspr` instead of `Spr(16, sx, sy)`. |
| `Sspr` | ✅ Used | ✅ Used | Used for both original Lua `sspr` calls and as replacement for `spr` calls. |
| `Time` | ✅ Used | ✅ Used | Direct mapping via `Time()`. |

### Raw C# Source
```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Api
{
    public class ApiCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Api/api/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Api/api/Music/music")?.text;
        public string MapData   => Resources.Load<TextAsset>("Api/api/Map/map")?.text;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Api/api/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Api/api/Label/label");

        private bool _isHighRes = true;
        private string[] tbl = { "\u30D2\u00DF", "\u30B3", "\u25C6" }; // 히゜, コ, ◆

        public override void Init()
        {
            float scale = _isHighRes ? 1f : P8.Width / 128f;
            Music(0);
        }

        public override void Update() 
        { 
            if (Btnp(7)) 
            {
                _isHighRes = !_isHighRes;
                float s = _isHighRes ? 1f : P8.Width / 128f;
                // Wipe the entire surface clean so trails don't persist outside the active screen bounds
                Rectfill(0, 0, P8.Width - 1, P8.Height - 1, 0, CoordMode.Physical);
                // Draw the blue background for the respective scaled screen size
                Rectfill(0, 0, (int)(128 * s) - 1, (int)(128 * s) - 1, 1, CoordMode.Physical);
            }

            if (Btnp(4))
            {
                Sfx(0);
            }
        }
        
        public override void Draw()
        {
            float scale = _isHighRes ? 1f : P8.Width / 128f;

            // clear screen to dark blue
            Cls(1);

            // ❎: mess with camera / clipping
            Camera(); // reset
            Clip(); // MUST reset clip mask! Disable any lingering Clip boxes!
            if (Btn(5))
            {
                Camera((int)(Cos(Time() / 6f) * 20f * scale), 0);
                Clip((int)(4 * scale), (int)(16 * scale), (int)(120 * scale), (int)(96 * scale));
            }

            // draw whole map (stars, clouds, background tiles)
            Map(0, 0, 0, 0, 128, 64);

            // circles  x,y,radius,col
            int r63 = (int)(63 * scale);
            int r67 = (int)(67 * scale);
            Circfill((int)(64 * scale), (int)(160 * scale), r63, 6);
            Circ((int)(64 * scale), (int)(160 * scale), r67, 14);

            // with fill pattern
            Fillp(0b0101101001011010); 
            Circfill((int)(64 * scale), (int)(160 * scale), (int)(52 * scale), 7);
            Fillp(); // reset

            // rectangles x0,y0,x1,y1,col
            Rectfill((int)(4 * scale), (int)(4 * scale), (int)(124 * scale) + (int)scale - 1, (int)(10 * scale) + (int)scale - 1, 0);
            Rect((int)(2 * scale), (int)(2 * scale), (int)(126 * scale) + (int)scale - 1, (int)(12 * scale) + (int)scale - 1, 0);

            // lines: x0,y0,x1,y1,col
            for (int i = 1; i <= 15; i++)
            {
                Line((int)((i * 8 - 1) * scale), (int)(6 * scale), (int)((i * 8 + 1) * scale), (int)(8 * scale), i);
            }

            // strings
            int num = 8;
            string str = "HELLO FROM API.P" + num;
            int str_len = str.Length;

            // print: str,x,y,col
            Print(str, (int)((64 - str_len * 2) * scale), (int)(20 * scale), 7, CoordMode.Virtual, scale);

            // tables / arrays iteration
            string[] tbl1 = { "a", "b", "c" }; // simulated add/del logic

            int cy1 = 104;
            foreach (var s in tbl1) 
            {
                Print(s, (int)(2 * scale), (int)(cy1 * scale), 5, CoordMode.Virtual, scale);
                cy1 += 6;
            }

            int cy2 = 104;
            foreach (var s in tbl1) 
            {
                Print(s, (int)(123 * scale), (int)(cy2 * scale), 5, CoordMode.Virtual, scale);
                cy2 += 6;
            }

            for (int i = 0; i < tbl.Length; i++)
            {
                Print(tbl[i], (int)(2 * scale), (int)((10 + (i + 1) * 6) * scale), 13, CoordMode.Virtual, scale);
                Print(tbl[i], (int)(114 * scale), (int)((10 + (i + 1) * 6) * scale), 13, CoordMode.Virtual, scale);
            }

            // draw sprites
            Palt(2, true);
            Palt(0, false);
            // spr(2,48,32,4,4) -> sprite 2 covers 4x4 tiles (32x32px)
            Sspr(16, 0, 32, 32, (int)(48 * scale), (int)(32 * scale), (int)(32 * scale), (int)(32 * scale));

            // stretched sprites
            float w = Cos(Time() / 2f) * 32f;
            int absW = (int)Mathf.Abs(w);
            bool flip = w < 0;

            if (absW > 0)
            {
                // draw back sides indigo
                if (flip) Pal(7, 13);

                // horizontal spinning bunny
                int hdw = (int)(absW * scale);
                int hdh = (int)(32f * scale);
                Sspr(16, 0, 32, 32, (int)((24f - absW / 2f) * scale), (int)(32 * scale), hdw, hdh, flipX: flip);

                // vertical spinning bunny
                int vdw = (int)(32f * scale);
                int vdh = (int)(absW * scale);
                Sspr(16, 0, 32, 32, (int)(88 * scale), (int)((48f - absW / 2f) * scale), vdw, vdh, flipY: flip);
            }

            Pal(); // reset palette

            // rotating star sprites
            for (int i = 0; i <= 31; i++)
            {
                float a = (i + Time() * 2f) / 32f;
                float rad = a * Mathf.PI * 2f;
                
                float sx = 64f + Mathf.Cos(rad) * 57f - 4f;
                float sy = 160f + -Mathf.Sin(rad) * 57f - 4f;

                int ssx = 64 + i % 16;
                int col = Sget(ssx, 0);

                Pal(7, col);
                Sspr(0, 8, 8, 8, (int)(sx * scale), (int)(sy * scale), (int)(8 * scale), (int)(8 * scale));
            }
            Pal(); // reset

            // draw state of buttons
            for (int pl = 0; pl <= 7; pl++)
            {
                for (int b = 0; b <= 7; b++)
                {
                    int sx = (int)((57 + b * 2) * scale);
                    int sy = (int)((70 + pl * 2) * scale);
                    int col = 5;
                    
                    // The user requested the primary inputs (Player 0) highlight on the 2nd row
                    int pIdx = pl == 0 ? 7 : pl - 1; 
                    if (Btn(b, pIdx)) col = b + 7;
                    
                    int fillSize = Mathf.Max(1, (int)scale);
                    Rectfill(sx, sy, sx + fillSize - 1, sy + fillSize - 1, col);
                }
            }
        }
    }
}

```

## Cartridge Assets
The following non-code data blocks were detected in the original `.p8` cartridge:
- `__gfx__` — spritesheet (128×128 PNG)
- `__label__` — cartridge thumbnail (128×128 PNG)
- `__map__` — tile map (hex text, loaded via `MapDataLoader`)
- `__sfx__` — sound effects
- `__music__` — music patterns

## Original Lua Source

### Raw Script Block
Directly extracted from `docs/references/pico-8/games/api/api.p8`:
```lua
-- File: api.p8
-- api.p8 by zep
-- demos most api functions

-- _draw() called once per frame
function _draw()
	
	-- clear screen to dark blue
	cls(1)
	
	-- ❎: mess with camera / clipping
	camera() -- reset
	if (btn(❎)) then
	 camera(cos(t()/6)*20,0)
	 clip(4,16,120,96)--x,y,w,h
	end
	
	-- draw whole map
	map()
	
	-- circles  x,y,radius,col
	circfill(64,160,63,6)
	circ(64,160,67,14)
	
	-- with fill pattern
	fillp(░)
	circfill(64,160,52,7)
	fillp() -- reset
	
	-- rectangles x0,y0,x1,y1,col
	rectfill(4,4,124,10,0)
	rect(2,2,126,12,0)
	
	-- lines: x0,y0,x1,y1,col
	-- (palette at top)
	for i=1,15 do
	 line(i*8-1, 6, i*8+1, 8, i)
	end
	
	-- strings
 
	num=8
	str="hello "
	str..="from api.p"..num
	str_len=#str
	
	-- print: str,x,y,col
	print(str, 64-str_len*2, 20, 7)
	
	-- tables / arrays
	
	tbl={"a"} -- single element
	
	add(tbl,"b") -- add to end
	add(tbl,"d")
	add(tbl,"c")
	del(tbl,"d") --remove by value
	
	-- iterate over the table
	-- (draw letters bottom left)
	cursor(2,104,5) -- x,y,col
	foreach(tbl,print)
	
	-- another way to iterate
	cursor(123,104,5)
	for i in all(tbl) do
	 print(i)
	end
	
	-- iterate with a for loop
	-- starts at index 1! (not 0)
	tbl={"ヒ゜","コ","◆"}
	
	for i=1,#tbl do
	 print(tbl[i],2,  10+i*6,13)
	 print(tbl[i],114,10+i*6,13)
	end
	
	
	-- draw sprites
	palt(2,true) --draw transparent
	palt(0,false)--draw solid (eyes)
	spr(2,48,32,4,4)
	
	-- stretched sprites
	-- (spinning bunnys)
	
	-- w: width to draw
	-- (1 turn ever 2 seconds)
	w = cos(t()/2) * 32
	
	-- draw back sides indigo
	if (w < 0) pal(7,13)
	
	--[[
	sspr: stretch sprite
	■ first 4 parameters specify
	  the source rect (x,y,w,h)
	■ last 4 params specify the
	  rectangle to draw (x,y,w,h)
	--]]
	sspr(16,0,32,32,
	    24-w/2,32,w,32)
	-- re-use w to mean height
	-- for vertical spinning
	sspr(16,0,32,32,
	    88,48-w/2,32,w)
	
	pal() -- reset palette
	
	-- rotating star sprites
	for i=0,31 do
	
	 -- angle based on time
	 local a=(i+t()*2)/32
	 
	 -- screen position
	 sx=64 +cos(a)*57 - 4
	 sy=160+sin(a)*57 - 4
	 
	 -- grab pixels from spritesheet
	 -- to use as color
	 ssx = 64+i%16   -- x location
	 col=sget(ssx,0) -- grab it
	 
	 -- draw star in that color
	 pal(7,col) -- (remap white)
	 spr(16, sx,sy)
	 
	end
	pal() -- reset
	
	-- draw state of buttons
	for pl=0,7 do
		for b=0,7 do
		 sx=57+b*2
		 sy=70+pl*2
		 col=5
		 if (btn(b,pl)) col=b+7
		 pset(sx,sy,col)
		end
	end

end

-- _update(): called 30 fps
-- (use _update60 for 60fps)
function _update()
	
	-- button pressed: play a sfx
	if (btnp(🅾️)) then
		sfx(0)
	end
	
end

-- _init() called once at start
function _init()
	
	-- music loops every 4 patterns
	-- because the loop-back flag
	-- is set on pattern 3
	music(0)
	
	-- make a custom menu item
	menuitem(1, "play sfx", 
	 function()
	  sfx(3)
	 end
	)
	
end

```

## EditMode Tests
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.api/Tests/Editor/ApiCartridgeTests.cs`.

