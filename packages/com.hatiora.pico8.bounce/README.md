# Bounce Cartridge (`bounce.p8`)

## Overview
A bouncy ball physics demo by zep. A ball with gravity bounces off walls, ceiling, and a dithered floor with dampening (`-0.9`). Press ❎ (`Btnp(5)`) to randomly bump the ball. If a bounce becomes too small, the ball auto-resets with a strong upward kick. Uses `_update60` for 60fps physics.

## Source Locations
- **Lua**: [bounce.p8](../../references/pico-8/games/bounce/bounce.p8)
- **C#**: [BounceCartridge.cs](../../packages/com.hatiora.pico8.bounce/Runtime/BounceCartridge.cs)
- **Tests**: [BounceCartridgeTests.cs](../../packages/com.hatiora.pico8.bounce/Tests/Editor/BounceCartridgeTests.cs)

## Implementation Details

### Resolution Scaling
Standard `_isHighRes` toggle via `Btnp(7)`. Physics always run in 128×128 logical space; only `Draw()` scales by `s = P8.Width / 128`.

### Key Deviations
- **`Spr` → `Sspr`**: Lua `spr(1,...)` draws 8×8 natively. At higher resolutions the sprite would be proportionally tiny. C# uses `Sspr(8,0,8,8, x,y, 8*s,8*s)` to scale it with the ball.
- **`Max`**: `Mathf.Max(1, (int)scale)` prevents scale collapsing to 0 at sub-128 resolutions.
- **Globals → `Init()`**: Lua top-level `ballx`, `bally`, `velx`, `vely` move into `Init()` as instance fields.

### API Implementation Map

| API Function | Native Lua | C# Translation | Deviation Rationale |
| :--- | :---: | :---: | :--- |
| `Btnp` | ✅ Used (`❎`) | ✅ Used | Lua `btnp(5)` maps to `Btnp(5)`. Added `Btnp(7)` for high-res toggle. |
| `Circfill` | ✅ Used | ✅ Used | Direct mapping. Ball radius and position scaled by `s`. |
| `Cls` | ✅ Used (`1`) | ✅ Used | Direct mapping. Dark blue background. |
| `Fillp` | ✅ Used (`░`) | ✅ Used | `░` maps to the binary pattern `0b0101101001011010`. |
| `Max` | ❌ | ✅ Used | `Mathf.Max(1, (int)scale)` prevents `s` from collapsing to 0 at sub-128 resolutions (not in original Lua). |
| `Print` | ✅ Used | ✅ Used | Direct mapping. Coordinates scaled by `s`. |
| `Rectfill` | ✅ Used | ✅ Used | Direct mapping. Floor area scaled by `s`. |
| `Rnd` | ✅ Used | ✅ Used | Direct mapping. `rnd(6)` → `Rnd(6f)`. |
| `Sfx` | ✅ Used (0–3) | ✅ Used | Direct mapping. 4 SFX: floor bounce (0), wall bounce (1), manual bump (2), auto-reset kick (3). |
| `Spr` | ✅ Used (1) | ❌ | Replaced by `Sspr` to support resolution-proportional sprite scaling. |
| `Sspr` | ❌ | ✅ Used | Draws sprite 1 at `8*s × 8*s` so the face scales proportionally with the `Circfill` ball at any resolution. |


### Raw C# Source
```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Bounce
{
    /// <summary>
    /// Bouncy ball demo by zep.
    /// A ball bounces around with gravity, wall/floor bouncing, and dampening.
    /// Press ❎ (button 5) to randomly bump the ball. Uses _update60.
    /// </summary>
    public class BounceCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Bounce/bounce/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Bounce/bounce/Music/music")?.text;
        public string MapData   => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Bounce/bounce/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Bounce/bounce/Label/label");

        private bool _isHighRes = true;

        // Ball state
        private float _ballx, _bally;
        private float _velx, _vely;
        private const int Size = 10;
        private const int FloorY = 100;

        public override void Init()
        {
            _ballx = 64;
            _bally = Size;

            // Starting velocity
            _velx = Rnd(6f) - 3f;
            _vely = Rnd(6f) - 3f;
        }

        public override void Update()
        {
            if (Btnp(7)) _isHighRes = !_isHighRes;

            // Move ball left/right
            if (_ballx + _velx < 0 + Size ||
                _ballx + _velx > 128 - Size)
            {
                // Bounce on side!
                _velx *= -1;
                Sfx(1);
            }
            else
            {
                // Move by x velocity
                _ballx += _velx;
            }

            // Move ball up/down
            if (_bally + _vely < 0 + Size ||
                _bally + _vely > FloorY - Size)
            {
                // Bounce on floor/ceiling
                _vely = _vely * -0.9f;
                Sfx(0);

                // If bounce was too small, bump into air
                if (_vely < 0 && _vely > -0.5f)
                {
                    _velx = Rnd(6f) - 3f;
                    _vely = -Rnd(5f) - 4f;
                    Sfx(3);
                }
            }
            else
            {
                _bally += _vely;
            }

            // Gravity!
            _vely += 0.2f;

            // Press ❎ to randomly choose a new velocity
            if (Btnp(5))
            {
                _velx = Rnd(6f) - 3f;
                _vely = Rnd(6f) - 8f;
                Sfx(2);
            }
        }

        public override void Draw()
        {
            float scale = _isHighRes ? P8.Width / 128f : 1f;
            int s = Mathf.Max(1, (int)scale);

            Cls(1);

            // "press ❎ to bump"
            Print("press \u00D7 to bump", 32 * s, 10 * s, 6, CoordMode.Virtual, scale);

            // Dithered floor
            Fillp(0b0101101001011010); // ░ pattern
            Rectfill(0, FloorY * s, 128 * s - 1, 128 * s - 1, 12);
            Fillp(); // Reset

            // Ball (filled circle)
            Circfill((int)(_ballx * s), (int)(_bally * s), Size * s, 14);

            // Sprite trail (sprite 1, scaled to match resolution)
            int sprX = (int)((_ballx - 4 - _velx) * s);
            int sprY = (int)((_bally - 4 - _vely) * s);
            // Source: sprite 1 = sheet coords (8,0), size 8x8
            Sspr(8, 0, 8, 8, sprX, sprY, 8 * s, 8 * s);
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
Directly extracted from `docs/references/pico-8/games/bounce/bounce.p8`:
```lua
-- File: bounce.p8
-- bouncy ball demo
-- by zep

size  = 10
ballx = 64
bally = size
floor_y = 100

-- starting velocity
velx = rnd(6)-3
vely = rnd(6)-3

function _draw()
	cls(1)
	
	print("press ❎ to bump",
	      32,10, 6)
	
	fillp(░)
	rectfill(0,floor_y,127,127,12)
	fillp() -- reset
	
	circfill(ballx,bally,size,14)
	
	spr(1,ballx-4-velx, 
	      bally-4-vely)
end

function _update60()
	
	-- move ball left/right
	
	if ballx+velx < 0+size or
	   ballx+velx > 128-size
	then
		-- bounce on side!
		velx *= -1 
		sfx(1)
	else
		-- move by x velocity
		ballx += velx
	end
	
	-- move ball up/down
	
	if bally+vely < 0+size or
	   bally+vely > floor_y-size
	then
		-- bounce on floor/ceiling
		vely = vely * -0.9
		sfx(0)
		
		-- if bounce was too small,
		-- bump into air
		if vely < 0 and
		   vely > -0.5 then
			velx = rnd(6)-3
			vely = -rnd(5)-4
			sfx(3)
		end
		
	else
		bally += vely
	end
	
	-- gravity!
	vely += 0.2
	
	-- press ❎ to ranomly
	-- choose a new velocity
	if (btnp(5)) then
		velx = rnd(6)-3
		vely = rnd(6)-8
		sfx(2)
	end
	
end


```

## EditMode Tests
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.bounce/Tests/Editor/BounceCartridgeTests.cs`.

