# Wander Cartridge (`wander.p8`)

## Overview
A simple exploration demo by zep. The player controls a small cat-like character that wanders freely across a multi-room tile map using 4-directional movement with friction-based physics. The world is divided into 16×16-tile rooms; the camera snaps to the current room as the player crosses boundaries. Collectible apples (tile 10) can be picked up, replacing them with a different tile (14) and playing a sound effect. The player sprite has a 4-frame walk animation that cycles based on movement speed and resets to idle when nearly still.

## Source Locations
- **Native Lua Script**: `docs/references/pico-8/games/wander/wander.p8`
- **C# Translation**: `packages/com.hatiora.pico8.wander/Runtime/WanderCartridge.cs`

## Implementation Details

### Resolution & Coordinate Scaling
When `_isHighRes = true`, `scale = P8.Width / 128f`. The map is drawn using `Map(scale: s)` for hi-res tile rendering. The player sprite uses `Sspr` with `dw/dh = 8*s`. Camera offsets are multiplied by `s`. UI elements (if any) use `CoordMode.Virtual`.

### Key Routines

**Movement** — Continuous `Btn(0-3)` input adds acceleration (`ac = 0.1f`) to velocity. Velocity is applied to tile position (`_x += _dx`), then damped by friction (`*= 0.7f`) each frame. No wall collision — the player walks freely through all tiles, matching the original Lua behavior.

**Animation** — Speed is computed as `Sqrt(dx² + dy²)`. Frame advances as `_f = (_f + spd*2) % 4` (4 walk frames). When speed drops below `0.05`, frame resets to 0 (idle).

**Camera** — Room-based snapping: `roomX = Flr(x/16)`, `roomY = Flr(y/16)`, then `Camera(roomX * 128 * s, roomY * 128 * s)`. Each room is exactly 16 tiles (128 pixels at 1x).

**Apple Collection** — When the player's tile position overlaps tile 10 (apple), it's replaced with tile 14 via `Mset` and `Sfx(0)` plays.

### API Implementation Map
The following table outlines the native PICO-8 functions used in the original `.p8` script and their C# translation counterparts:

| API Function | Native Lua Script | C# Translation | Deviation Rationale |
| :--- | :---: | :---: | :--- |
| `Btn` | ✅ Used | ✅ Used | Direct mapping. 4-directional movement (buttons 0-3). |
| `Btnp` | ❌ | ✅ Used | Added `Btnp(7)` for hi-res toggle (not in original). |
| `Camera` | ✅ Used | ✅ Used | Room coords multiplied by `s` for hi-res. |
| `Cls` | ✅ Used | ✅ Used | Direct mapping. `Cls(1)` dark blue background. |
| `Flr` | ✅ Used | ✅ Used | Direct mapping. Room and tile index calculations. |
| `Map` | ✅ Used | ✅ Used | Uses `scale` parameter for hi-res tile rendering. |
| `Mget` | ✅ Used | ✅ Used | Direct mapping. Apple tile detection. |
| `Mset` | ✅ Used | ✅ Used | Direct mapping. Replace apple with collected tile. |
| `Print` | ❌ | ❌ | Removed placeholder prints; not in original Lua. |
| `Sfx` | ✅ Used | ✅ Used | Direct mapping. Apple collection sound. |
| `Spr` | ✅ Used | ❌ | Replaced with `Sspr` for hi-res scaling support. |
| `Sqrt` | ✅ Used | ✅ Used | Direct mapping. Movement speed calculation. |
| `Sspr` | ❌ | ✅ Used | Replaces `Spr` for player sprite with `dw/dh = 8*s`. |


### Raw C# Source
```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Wander
{
    public class WanderCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Wander/wander/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Wander/wander/Music/music")?.text;
        public string MapData   => Resources.Load<TextAsset>("Wander/wander/Map/map")?.text;
        public string GffData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Wander/wander/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Wander/wander/Label/label");
        
        private bool _isHighRes = true;

        // State (matching Lua globals)
        private float _x, _y;   // position (in tiles)
        private float _dx, _dy; // velocity
        private float _f;       // animation frame
        private int _d;         // direction (-1 or 1)

        public override void Init()
        {
            _x = 24; _y = 24;
            _dx = 0; _dy = 0;
            _f = 0;
            _d = 1;
        }

        public override void Update()
        {
            if (Btnp(7)) _isHighRes = !_isHighRes;

            float ac = 0.1f; // acceleration

            if (Btn(0)) { _dx -= ac; _d = -1; } // left
            if (Btn(1)) { _dx += ac; _d =  1; } // right
            if (Btn(2)) { _dy -= ac; }           // up
            if (Btn(3)) { _dy += ac; }           // down

            // Move (add velocity)
            _x += _dx; _y += _dy;

            // Friction (lower for more sliding)
            _dx *= 0.7f;
            _dy *= 0.7f;

            // Advance animation according to speed
            // (or reset when standing almost still)
            float spd = Sqrt(_dx * _dx + _dy * _dy);
            _f = (_f + spd * 2) % 4; // 4 frames
            if (spd < 0.05f) _f = 0;

            // Collect apple
            int tx = Flr(_x), ty = Flr(_y);
            if (Mget(tx, ty) == 10)
            {
                Mset(tx, ty, 14);
                Sfx(0);
            }
        }

        public override void Draw()
        {
            float scale = _isHighRes ? P8.Width / 128f : 1f;
            int s = Mathf.Max(1, (int)scale);

            Cls(1); // dark blue background

            // Move camera to current room
            int roomX = Flr(_x / 16);
            int roomY = Flr(_y / 16);
            Camera(roomX * 128 * s, roomY * 128 * s);

            // Draw the whole map (128x32 tiles)
            Map(0, 0, 0, 0, 128, 32, 0, CoordMode.Virtual, s);

            // Draw the player
            int fr = 1 + Flr(_f); // frame index (sprites 1-4)
            int px = Flr(_x * 8) * s - 4 * s;
            int py = Flr(_y * 8) * s - 4 * s;
            int sprSx = (fr % 16) * 8;
            int sprSy = (fr / 16) * 8;
            Sspr(sprSx, sprSy, 8, 8, px, py, 8 * s, 8 * s, _d == -1);

            // Reset camera
            Camera();
        }
    }
}
```

## Cartridge Assets
The following non-code data blocks were detected in the original `.p8` cartridge:
- `__gfx__` — spritesheet (player walk frames, tiles, apples, decorations)
- `__label__` — cartridge thumbnail (128x128 PNG)
- `__map__` — tile map data (multi-room world, 128x32 tiles)
- `__sfx__` — sound effects (apple collection)
- `__music__` — music patterns

## Original Lua Source

### Raw Script Block
Directly extracted from `docs/references/pico-8/games/wander/wander.p8`:
```lua
-- File: wander.p8
-- wander demo
-- by zep

x=24 y=24 -- position (in tiles)
dx=0 dy=0 -- velocity
f=0       -- frame number
d=1       -- direction (-1, 1)

function _draw()
	cls(1)
	
	-- move camera to current room
	room_x = flr(x/16)
	room_y = flr(y/16)
	camera(room_x*128,room_y*128)
	
	-- draw the whole map (128⁙32)
	map()
	
	-- draw the player
	spr(1+f,      -- frame index
	 x*8-4,y*8-4, -- x,y (pixels)
	 1,1,d==-1    -- w,h, flip
	)
end

function _update()
	
	ac=0.1 -- acceleration
	
	if (btn(⬅️)) dx-= ac d=-1
	if (btn(➡️)) dx+= ac d= 1
	if (btn(⬆️)) dy-= ac
	if (btn(⬇️)) dy+= ac
	
	-- move (add velocity)
	x+=dx y+=dy
	
	-- friction (lower for more)
	dx *=.7
	dy *=.7
	
	-- advance animation according
	-- to speed (or reset when
	-- standing almost still)
	spd=sqrt(dx*dx+dy*dy)
	f= (f+spd*2) % 4 -- 4 frames
	if (spd < 0.05) f=0
	
	-- collect apple
	if (mget(x,y)==10) then
		mset(x,y,14)
		sfx(0)
	end
	
end


```

## EditMode Tests
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.wander/Tests/Editor/WanderCartridgeTests.cs`.

