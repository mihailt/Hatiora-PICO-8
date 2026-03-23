# Automata Cartridge (`automata.p8`)

## Overview
A 1-D cellular automata visualizer by zep ([reference](https://en.wikipedia.org/wiki/Cellular_automaton)). Each frame, the screen scrolls up one pixel row and a new bottom row is generated from a 3-neighbor rule. The rule set randomizes every 16 lines (or when ❎ is pressed), producing continually evolving fractal-like patterns. If the bottom row ever goes blank, a single white seed pixel is placed at the center to restart the pattern.

## Source Locations
- **Native Lua Script**: `docs/references/pico-8/games/automata/automata.p8`
- **C# Translation**: `packages/com.hatiora.pico8.automata/Runtime/AutomataCartridge.cs`
- **Unit Tests**: `packages/com.hatiora.pico8.automata/Tests/Editor/AutomataCartridgeTests.cs`

## Implementation Details

### Resolution & Coordinate Scaling
When `_isHighRes = true` (default), the automata operates at the full `P8.Width × P8.Height` (e.g., 256×256) and uses `Memcpy`-based screen scrolling for performance. When toggled off via `Btnp(7)`, it falls back to native 128×128 using `Pget`/`Pset` scroll loops. `W`/`H`/`Scale` are computed dynamically based on the mode.

### Key Routines

**Initialization** (`Init`) — Clears screen, resets line counter, and sets the starting rule `{0,1,0,1,1,0,0,1}`. In the original Lua, `cls()` and `l=0` run at top-level scope; in C# these move into `Init()`.

**Rule Randomization** (`Update`) — Every 16 frames (`_l % 16 == 0`) or on ❎ press (`Btnp(4)`), entries `r[1]` through `r[7]` are randomized to `flr(rnd(2.3))` (producing 0 or 1). Entry `r[0]` is never changed. If the bottom row is blank (checked via `Pget` loop), a seed pixel is placed at the center.

**Screen Scroll** (`Draw`) — In high-res mode, `Memcpy(0x6000, 0x6000 + bytesPerRow, bytesPerRow * (H-1))` copies all rows up by one, operating on the PICO-8 nibble-packed screen memory. In native mode, a `Pget`/`Pset` loop performs the same shift. After scrolling, a new bottom row is computed: for each pixel `x`, the 3 neighbors at `(x-1, x, x+1)` on the row above are checked, forming a 3-bit index `n` into the rule array. The output color is `r[n] * 7` (0 = black, 7 = white).

### API Implementation Map

| API Function | Native Lua | C# Translation | Deviation Rationale |
| :--- | :---: | :---: | :--- |
| `Btnp` | ✅ Used (`❎`) | ✅ Used | Lua `btnp(❎)` maps to `Btnp(4)`. Added `Btnp(7)` for high-res toggle. |
| `Cls` | ✅ Used | ✅ Used | Direct mapping. Lua calls at top-level; C# calls in `Init()`. |
| `Flr` | ✅ Used | ✅ Used | Direct mapping. `flr(rnd(2.3))` produces 0 or 1. |
| `Memcpy` | ✅ Used | ✅ Used | High-res mode only. Scrolls nibble-packed screen memory at `0x6000`. Engine auto-syncs `PixelBuffer` ↔ RAM. |
| `Pget` | ✅ Used | ✅ Used | Direct mapping. Reads bottom row to detect blank lines and reads neighbor row for rule computation. |
| `Pset` | ✅ Used | ✅ Used | Direct mapping. Seeds blank rows and writes new bottom row. |
| `Rectfill` | ❌ | ✅ Used | Native mode scroll: fills `Scale×Scale` blocks for upscaled rendering (not in original Lua). |
| `Rnd` | ✅ Used | ✅ Used | Direct mapping. `rnd(2.3)` → `Rnd(2.3f)`. |

### Raw C# Source
```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Automata
{
    /// <summary>
    /// 1-D cellular automata demo by zep.
    /// Scrolls the screen up one pixel each frame via Memcpy, then generates
    /// a new bottom row using a 3-neighbor rule that changes every 16 lines.
    /// </summary>
    public class AutomataCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Automata/automata/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Automata/automata/Music/music")?.text;
        public string MapData   => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Automata/automata/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Automata/automata/Label/label");

        private int _l;             // line counter
        private int[] _r;           // rule set (8 entries, index 0–7)
        private bool _isHighRes = true;

        // Virtual grid width/height (128 in native, P8.Width/Height in high-res)
        private int W => _isHighRes ? P8.Width : 128;
        private int H => _isHighRes ? P8.Height : 128;
        private float Scale => _isHighRes ? P8.Width / 128f : 1f;

        public override void Init()
        {
            Cls();
            _l = 0;

            // starting rule set: {0,1,0,1,1,0,0,1}
            _r = new int[] { 0, 1, 0, 1, 1, 0, 0, 1 };
        }

        public override void Update()
        {
            if (Btnp(7)) _isHighRes = !_isHighRes;

            _l++;

            // change rule every 16 lines or when ❎ is pressed
            if (_l % 16 == 0 || Btnp(4))
            {
                for (int i = 1; i <= 7; i++)
                {
                    _r[i] = Flr(Rnd(2.3f));
                }
            }

            // if the bottom line is blank, seed it
            bool found = false;
            for (int x = 0; x < W; x++)
            {
                if (Pget(x, H - 1) > 0) { found = true; break; }
            }

            if (!found)
            {
                Pset(W / 2 - 1, H - 1, 7);
            }
        }

        public override void Draw()
        {
            int w = W;
            int h = H;

            if (_isHighRes)
            {
                // High-res: scroll via Memcpy on screen memory
                int bytesPerRow = P8.Width / 2; // nibble-packed row width
                int screenAddr = 0x6000;
                Memcpy(screenAddr, screenAddr + bytesPerRow, bytesPerRow * (P8.Height - 1));
            }
            else
            {
                // Native 128×128: scroll via Pget/Pset loop
                float s = Scale;
                int fillW = Mathf.Max(1, (int)s);
                int fillH = Mathf.Max(1, (int)s);

                for (int y = 0; y < h - 1; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int col = Pget(x, y + 1);
                        if (_isHighRes)
                        {
                            int px = x * (int)s;
                            int py = y * (int)s;
                            Rectfill(px, py, px + fillW - 1, py + fillH - 1, col);
                        }
                        else
                        {
                            Pset(x, y, col);
                        }
                    }
                }
            }

            // Compute new bottom row from 3-neighbor cellular automata rule
            int bottomRow = h - 1;
            int neighborRow = h - 2;
            float scale = Scale;

            for (int x = 0; x < w; x++)
            {
                int n = 0;
                for (int b = 0; b <= 2; b++)
                {
                    int nx = x - 1 + b;
                    if (nx >= 0 && nx < w && Pget(nx, neighborRow) > 0)
                    {
                        n += 1 << b; // 2^b
                    }
                }

                int col = _r[n] * 7;
                if (_isHighRes)
                {
                    Pset(x, bottomRow, col);
                }
                else
                {
                    Pset(x, bottomRow, col);
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
Directly extracted from `docs/references/pico-8/games/automata/automata.p8`:
```lua
-- File: automata.p8
-- 1-d cellular automata demo
-- by zep
-- ref: wikipedia.org/wiki/cellular_automaton

cls()
l=0 -- line count

--uncomment for kaleidoscope
--poke(0x5f2c,7)

-- starting rule set
r={[0]=0,1,0,1,1,0,0,1}


function _update()

	l+=1
	-- change rule every 16 lines
	-- (or when ❎ is pressed)
	if (l%16==0 or btnp(❎)) then
		for i=1,7 do
			r[i]=flr(rnd(2.3))
		end
	end
	
	
	-- if the line is blank, add
	-- something to get it started
	
	found = false
	for x=0,127 do
		if (pget(x,127)>0) found=true
	end
	
	if (not found) then
		pset(63,127,7)
	end

end

function _draw()
	-- scroll
	memcpy(0x6000,0x6040,0x1fc0)
	
	for x=0,127
	 do n=0 
	 for b=0,2 do
	  if (pget(x-1+b,126)>0)
	  then
	   n += 2 ^ b -- 1,2,4
	  end
	 end
	 pset(x,127,r[n]*7)
	end
	
end


```

## EditMode Tests
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.automata/Tests/Editor/AutomataCartridgeTests.cs`.

