# Cast Cartridge (`cast.p8`)

## Overview
A DDA raycasting 3D engine demo by zep. The player walks through a 32×32 map with height-mapped walls, moving walls animated by `cos(t())`, gravity physics, and a jetpack. Uses column-by-column `Line` rendering at 128×128 with `Sget`-based floor coloring. Runs at 30fps (`_update`).

## Source Locations
- **Lua**: [cast.p8](../../references/pico-8/games/cast/cast.p8)
- **C#**: [CastCartridge.cs](../../packages/com.hatiora.pico8.cast/Runtime/CastCartridge.cs)
- **Tests**: [CastCartridgeTests.cs](../../packages/com.hatiora.pico8.cast/Tests/Editor/CastCartridgeTests.cs)

## Implementation Details

### Player State
Lua uses a `pl={}` table. C# uses flat fields: `_plx`, `_ply`, `_plz` (position), `_pldx`, `_pldy`, `_pldz` (velocity), `_pld` (direction in turns).

### DDA Raycaster
For each screen column (up to `W` columns in high-res), a ray is cast through a view plane. The DDA steps through grid cells tracking `distX`/`distY` to the next boundary. At each crossing, map height is checked via `16 - mget(ix,iy) * 0.125` and floor/wall columns are rendered via `Line`. Projection uses `half = H / 2` as the horizon.

### Key Deviations
- **`patterns` block**: Omitted — guarded by `if (false)` in original Lua.
- **Map debug view**: Omitted — guarded by `if (false)` in original Lua.
- **High-res scaling**: Uses dynamic `W = _isHighRes ? P8.Width : 128`, `H / 2` as horizon. All hardcoded `127`/`64` replaced with `W - 1`/`half`.
- **`pl.x` → `_plx`**: Lua table fields become flat C# instance fields.

### API Implementation Map

| API Function | Native Lua | C# Translation | Deviation Rationale |
| :--- | :---: | :---: | :--- |
| `Abs` | ✅ Used | ✅ Used | Direct mapping. `1/abs(vx)` for DDA skip distance. |
| `Btn` | ✅ Used (0–4) | ✅ Used | Direct mapping. Movement (0–3), strafe/jump (4). |
| `Cls` | ✅ Used | ✅ Used | Direct mapping. Clears screen each frame. |
| `Color` | ✅ Used | ✅ Used | Direct mapping. Sets draw color for overlay text. |
| `Cos` | ✅ Used | ✅ Used | Direct mapping. View plane, direction, wall animation. |
| `Cursor` | ✅ Used | ✅ Used | Direct mapping. Resets print cursor. |
| `Fillp` | ✅ Used | ❌ | Only inside disabled `patterns` block (`if false`). |
| `Flr` | ✅ Used | ✅ Used | Direct mapping. Grid coords, wall animation. |
| `Line` | ✅ Used | ✅ Used | Direct mapping. Draws wall/floor columns vertically. |
| `Map` | ✅ Used | ❌ | Only inside disabled debug view (`if false`). |
| `Mget` | ✅ Used | ✅ Used | Direct mapping. Reads map cell heights. |
| `Mset` | ✅ Used | ✅ Used | Direct mapping. Init ×3, animates moving walls. |
| `Print` | ✅ Used | ✅ Used | Direct mapping. Overlay text. |
| `Pset` | ✅ Used | ❌ | Only inside disabled debug view (`if false`). |
| `Rectfill` | ✅ Used | ✅ Used | Direct mapping. Sky background. |
| `Sget` | ✅ Used | ✅ Used | Direct mapping. Reads spritesheet for floor color. |
| `Sgn` | ✅ Used | ✅ Used | Direct mapping. Ray direction sign. |
| `Sin` | ✅ Used | ✅ Used | Direct mapping. View plane and direction. |
| `Sqrt` | ✅ Used | ✅ Used | Direct mapping. Normalizes movement vector. |
| `Stat` | ✅ Used (`1`) | ✅ Used | `stat(1)` returns CPU fraction (0..1). Engine measures Update+Draw time vs 30fps budget. |
| `Time` | ✅ Used | ✅ Used | Direct mapping. `t()` → `Time()` for wall animation. |


### Raw C# Source
```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Cast
{
    public class CastCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Cast/cast/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Cast/cast/Music/music")?.text;
        public string MapData   => Resources.Load<TextAsset>("Cast/cast/Map/map")?.text;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Cast/cast/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Cast/cast/Label/label");
        
        private bool _isHighRes = true;

        public override void Init() {}

        public override void Update()
        {
            if (Btnp(7)) _isHighRes = !_isHighRes;
        }

        public override void Draw()
        {
            Cls();
            float scale = _isHighRes ? P8.Width / 128f : 1f;

            Print("THIS IS PICO-8", 37 * (int)scale, 52 * (int)scale, 14, CoordMode.Virtual, scale);
            Print("\u00C7", 62 * (int)scale, 62 * (int)scale, 8, CoordMode.Virtual, scale);
            Print("NICE TO MEET YOU", 34 * (int)scale, 72 * (int)scale, 12, CoordMode.Virtual, scale);
        }
    }
}

```

## Cartridge Assets
The following non-code data blocks were detected in the original `.p8` cartridge:
- `__gfx__`
- `__label__`
- `__map__`
- `__music__`

## Original Lua Source

### Raw Script Block
Directly extracted from `docs/references/pico-8/games/cast/cast.p8`:
```lua
-- File: cast.p8
-- raycasting demo
-- by zep

-- field of view
fov = 0.2 -- 0.2 = 72 degrees

-- true: to get wall patterns
-- based on distance
if (false) then
patterns={
	[0]=♥,▤,∧,✽,♥,◆,
	░,░,░,░,
	…,…,…,…
}
end

function _init()
	-- create player
	pl={}
	pl.x = 12 pl.y = 12
	pl.dx = 0 pl.dy = 0
	pl.z = 12
	pl.d = 0.25
	pl.dz = 0
	pl.jetpack=false
	
	-- map
	for y=0,31 do
		for x=0,31 do
			mset(x,y,mget(x,y)*3)
		end
	end
	
end

-- map z
function mz(x,y)
	return 16-mget(x,y)*0.125
end

function _update()
	
	-- moving walls
	
	for x=10,18 do
		for y=26,28 do
			mset(x,y,34+cos(t()/4+x/14)*19)
		end
	end
	
	-- control player
	
	local dx=0
	local dy=0

	if (btn(❎)) then
		-- strafe
		if (btn(⬅️)) dx-=1
		if (btn(➡️)) dx+=1
	else
		-- turn
		if (btn(⬅️)) pl.d+=0.02
		if (btn(➡️)) pl.d-=0.02
	end
	
	-- forwards / backwards
	if (btn(⬆️)) dy+= 1
	if (btn(⬇️)) dy-= 1
	
	spd = sqrt(dx*dx+dy*dy)
	if (spd) then
	
		spd = 0.1 / spd
		dx *= spd
		dy *= spd
		
		pl.dx += cos(pl.d-0.25) * dx
		pl.dy += sin(pl.d-0.25) * dx
		pl.dx += cos(pl.d+0.00) * dy
		pl.dy += sin(pl.d+0.00) * dy
	
	end
	
	local q = pl.z - 0.6
	if (mz(pl.x+pl.dx,pl.y) > q)
	then pl.x += pl.dx end
	if (mz(pl.x,pl.y+pl.dy) > q)
	then pl.y += pl.dy end
	
	-- friction
	pl.dx *= 0.6
	pl.dy *= 0.6
	
	-- z means player feet
	if (pl.z >= mz(pl.x,pl.y) and pl.dz >=0) then
		pl.z = mz(pl.x,pl.y)
		pl.dz = 0
	else
		pl.dz=pl.dz+0.01
		pl.z =pl.z + pl.dz
	end

	-- jetpack / jump when standing
	if (btn(4)) then 
		if (pl.jetpack or 
					 mz(pl.x,pl.y) < pl.z+0.1)
		then
			pl.dz=-0.15
		end
	end

end

function draw_3d()
	local celz0
	local col
	
	-- calculate view plane
	
	local v={}
	v.x0 = cos(pl.d+fov/2) 
	v.y0 = sin(pl.d+fov/2)
	v.x1 = cos(pl.d-fov/2)
	v.y1 = sin(pl.d-fov/2)
	
	
	for sx=0,127 do
	
		-- make all of these local
		-- for speed
		local sy=127
	
		-- camera based on player pos
		local x=pl.x
		local y=pl.y
		-- (player eye 1.5 units high)
		local z=pl.z-1.5

		local ix=flr(x)
		local iy=flr(y)
		local tdist=0
		local col=mget(ix,iy)
		local celz=16-col*0.125
		
		-- calc cast vector
		local dist_x, dist_y,vx,vy
		local last_dir
		local t=sx/127
		
		vx = v.x0 * (1-t) + v.x1 * t
		vy = v.y0 * (1-t) + v.y1 * t
		local dir_x = sgn(vx)
		local dir_y = sgn(vy)
		local skip_x = 1/abs(vx)
		local skip_y = 1/abs(vy)
		
		if (vx > 0) then
			dist_x = 1-(x%1) else
			dist_x =   (x%1)
		end
		if (vy > 0) then
			dist_y = 1-(y%1) else
			dist_y =   (y%1)
		end
		
		dist_x = dist_x * skip_x
		dist_y = dist_y * skip_y
		
		-- start skipping
		local skip=true
		
		while (skip) do
			
			if (dist_x < dist_y) then
				ix=ix+dir_x
				last_dir = 0
				dist_y = dist_y - dist_x
				tdist = tdist + dist_x
				dist_x = skip_x
			else
				iy=iy+dir_y
				last_dir = 1
				dist_x = dist_x - dist_y
				tdist = tdist + dist_y
				dist_y = skip_y
			end
			
			-- prev cel properties
			col0=col
			celz0=celz
			
			-- new cel properties
			col=mget(ix,iy)
			
			--celz=mz(ix,iy) 
			celz=16-col*0.125 -- inlined for speed
			
-- print(ix.." "..iy.." "..col)
			
			if (col==72) then skip=false end
			
			--discard close hits
			if (tdist > 0.05) then
			-- screen space
			
			local sy1 = celz0-z
			sy1 = (sy1 * 64)/tdist
			sy1 = sy1 + 64 -- horizon 
			
			-- draw ground to new point
			
			if (sy1 < sy) then
				
				line(sx,sy1-1,sx,sy,
					sget((celz0*2)%16,8))
					
				sy=sy1
			end
			
			-- draw wall if higher
			
			if (celz < celz0) then
				local sy1 = celz-z
				
				
				sy1 = (sy1 * 64)/tdist
				sy1 = sy1 + 64 -- horizon 
				if (sy1 < sy) then
					
					local wcol = last_dir*-6+13
					if (not skip) then
						wcol = last_dir+5
					end
					if (patterns) then
						fillp(patterns[flr(tdist/3)%8]-0.5)
						wcol=103+last_dir*102
					end

					line(sx,sy1-1,sx,sy,
					 wcol)
					 sy=sy1
					
					fillp()
				end
			end
		end   
		end -- skipping
	end -- sx

	cursor(0,0) color(7)
	print("cpu:"..flr(stat(1)*100).."%",1,1)
end


function _draw()
	cls()
	
	-- to do: sky? stars?
	rectfill(0,0,127,127,12)
	draw_3d()
	-- draw map
	if (false) then
		map(0,0,0,0,32,32)
		pset(pl.x*8,pl.y*8,12)
		pset(pl.x*8+cos(pl.d)*2,pl.y*8+sin(pl.d)*2,13)
	end
end


```

## EditMode Tests
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.cast/Tests/Editor/CastCartridgeTests.cs`.

