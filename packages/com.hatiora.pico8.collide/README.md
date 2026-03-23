# Collide Cartridge (`collide.p8`)

## Overview
Wall and actor collision demo by zep. A player and several NPC actors move through a tile-mapped world with AABB collision against walls (via sprite flag 1) and other actors. Features bounce response, friction, treasure collection (ring of gems), and room-based camera scrolling. Runs at 30fps (`_update`).

## Source Locations
- **Lua**: [collide.p8](../../references/pico-8/games/collide/collide.p8)
- **C#**: [CollideCartridge.cs](../../packages/com.hatiora.pico8.collide/Runtime/CollideCartridge.cs)
- **Tests**: [CollideCartridgeTests.cs](../../packages/com.hatiora.pico8.collide/Tests/Editor/CollideCartridgeTests.cs)

## Implementation Details

### Actor System
Lua uses a global `actor={}` table and `make_actor()` factory. C# uses a private `Actor` class with fields (`k`, `x`, `y`, `dx`, `dy`, `frame`, `friction`, `bounce`, `w`, `h`) and a `List<Actor>`. The player is stored as `_pl`.

### Collision Detection
- **Wall collision**: `Solid(x,y)` checks `Fget(Mget(x,y), 1)` for the wall flag. `SolidArea()` checks 4 AABB corners.
- **Actor collision**: `SolidActor()` tests AABB overlap between all actor pairs. On collision, both actors share the faster velocity ("cheat" bounce). `CollideEvent()` handles treasure pickup (sprite 35 → remove + SFX 3) or generic bump (SFX 2).
- **Safe iteration**: Update iterates a snapshot copy of the actor list to handle removal during collision events.

### Key Deviations
- **`Spr` → `Sspr`**: Sprites scale proportionally at higher resolutions using `8*s` dimensions.
- **`foreach(actor, fn)` → snapshot loop**: C# iterates a copy to safely remove actors during collision.
- **Camera scaling**: `Camera(roomX * 128 * s, ...)` scales room offset for high-res.
- **Lua `add`/`del` → `List.Add`/`Remove`**: Standard C# list operations.

### API Implementation Map

| API Function | Native Lua | C# Translation | Deviation Rationale |
| :--- | :---: | :---: | :--- |
| `Abs` | ✅ Used | ✅ Used | Direct mapping. Collision overlap and animation frame advance. |
| `Btn` | ✅ Used (0–3) | ✅ Used | Direct mapping. Player movement acceleration. |
| `Btnp` | ❌ | ✅ Used | Added `Btnp(7)` for high-res toggle. |
| `Camera` | ✅ Used | ✅ Used | Direct mapping. Room offset scaled by `s` for high-res. |
| `Cls` | ✅ Used | ✅ Used | Direct mapping. |
| `Cos` | ✅ Used | ✅ Used | Direct mapping. Treasure ring placement. |
| `Fget` | ✅ Used (flag 1) | ✅ Used | Direct mapping. Wall detection via sprite flag. |
| `Flr` | ✅ Used | ✅ Used | Direct mapping. Room coords, grid cell lookup. |
| `Map` | ✅ Used | ✅ Used | Direct mapping. Draws the tile map. |
| `Max` | ❌ | ✅ Used | `Mathf.Max(1, s)` for scale collapse prevention. |
| `Mget` | ✅ Used | ✅ Used | Direct mapping. Reads tile for wall check. |
| `Sfx` | ✅ Used (2,3) | ✅ Used | Direct mapping. Bump (2) and treasure pickup (3). |
| `Sin` | ✅ Used | ✅ Used | Direct mapping. Treasure ring placement. |
| `Spr` | ✅ Used | ❌ | Replaced by `Sspr` for proportional sprite scaling. |
| `Sspr` | ❌ | ✅ Used | Draws actor sprites at `8*s` for high-res support. |

### Raw C# Source
```csharp

## Cartridge Assets
The following non-code data blocks were detected in the original `.p8` cartridge:
- `__gfx__`
- `__label__`
- `__gff__`
- `__map__`
- `__sfx__`

## Original Lua Source

### Raw Script Block
Directly extracted from `docs/references/pico-8/games/collide/collide.p8`:
```lua
-- File: collide.p8
-- wall and actor collisions
-- by zep

actor = {} -- all actors

-- make an actor
-- and add to global collection
-- x,y means center of the actor
-- in map tiles
function make_actor(k, x, y)
	a={
		k = k,
		x = x,
		y = y,
		dx = 0,
		dy = 0,		
		frame = 0,
		t = 0,
		friction = 0.15,
		bounce  = 0.3,
		frames = 2,
		
		-- half-width and half-height
		-- slightly less than 0.5 so
		-- that will fit through 1-wide
		-- holes.
		w = 0.4,
		h = 0.4
	}
	
	add(actor,a)
	
	return a
end

function _init()

	-- create some actors
	
	-- make player
	pl = make_actor(21,2,2)
	pl.frames=4
	
	-- bouncy ball
	local ball = make_actor(33,8.5,11)
	ball.dx=0.05
	ball.dy=-0.1
	ball.friction=0.02
	ball.bounce=1
	
	-- red ball: bounce forever
	-- (because no friction and
	-- max bounce)
	local ball = make_actor(49,7,8)
	ball.dx=-0.1
	ball.dy=0.15
	ball.friction=0
	ball.bounce=1
	
	-- treasure
	
	for i=0,16 do
		a = make_actor(35,8+cos(i/16)*3,
		    10+sin(i/16)*3)
		a.w=0.25 a.h=0.25
	end
	
	-- blue peopleoids
	
	a = make_actor(5,7,5)
	a.frames=4
	a.dx=1/8
	a.friction=0.1
	
	for i=1,6 do
	 a = make_actor(5,20+i,24)
	 a.frames=4
	 a.dx=1/8
	 a.friction=0.1
	end
	
end

-- for any given point on the
-- map, true if there is wall
-- there.

function solid(x, y)
	-- grab the cel value
	val=mget(x, y)
	
	-- check if flag 1 is set (the
	-- orange toggle button in the 
	-- sprite editor)
	return fget(val, 1)
	
end

-- solid_area
-- check if a rectangle overlaps
-- with any walls

--(this version only works for
--actors less than one tile big)

function solid_area(x,y,w,h)
	return 
		solid(x-w,y-h) or
		solid(x+w,y-h) or
		solid(x-w,y+h) or
		solid(x+w,y+h)
end


-- true if [a] will hit another
-- actor after moving dx,dy

-- also handle bounce response
-- (cheat version: both actors
-- end up with the velocity of
-- the fastest moving actor)

function solid_actor(a, dx, dy)
	for a2 in all(actor) do
		if a2 != a then
		
			local x=(a.x+dx) - a2.x
			local y=(a.y+dy) - a2.y
			
			if ((abs(x) < (a.w+a2.w)) and
					 (abs(y) < (a.h+a2.h)))
			then
				
				-- moving together?
				-- this allows actors to
				-- overlap initially 
				-- without sticking together    
				
				-- process each axis separately
				
				-- along x
				
				if (dx != 0 and abs(x) <
				    abs(a.x-a2.x))
				then
					
					v=abs(a.dx)>abs(a2.dx) and 
					  a.dx or a2.dx
					a.dx,a2.dx = v,v
					
					local ca=
					 collide_event(a,a2) or
					 collide_event(a2,a)
					return not ca
				end
				
				-- along y
				
				if (dy != 0 and abs(y) <
					   abs(a.y-a2.y)) then
					v=abs(a.dy)>abs(a2.dy) and 
					  a.dy or a2.dy
					a.dy,a2.dy = v,v
					
					local ca=
					 collide_event(a,a2) or
					 collide_event(a2,a)
					return not ca
				end
				
			end
		end
	end
	
	return false
end


-- checks both walls and actors
function solid_a(a, dx, dy)
	if solid_area(a.x+dx,a.y+dy,
				a.w,a.h) then
				return true end
	return solid_actor(a, dx, dy) 
end

-- return true when something
-- was collected / destroyed,
-- indicating that the two
-- actors shouldn't bounce off
-- each other

function collide_event(a1,a2)
	
	-- player collects treasure
	if (a1==pl and a2.k==35) then
		del(actor,a2)
		sfx(3)
		return true
	end
	
	sfx(2) -- generic bump sound
	
	return false
end

function move_actor(a)

	-- only move actor along x
	-- if the resulting position
	-- will not overlap with a wall

	if not solid_a(a, a.dx, 0) then
		a.x += a.dx
	else
		a.dx *= -a.bounce
	end

	-- ditto for y

	if not solid_a(a, 0, a.dy) then
		a.y += a.dy
	else
		a.dy *= -a.bounce
	end
	
	-- apply friction
	-- (comment for no inertia)
	
	a.dx *= (1-a.friction)
	a.dy *= (1-a.friction)
	
	-- advance one frame every
	-- time actor moves 1/4 of
	-- a tile
	
	a.frame += abs(a.dx) * 4
	a.frame += abs(a.dy) * 4
	a.frame %= a.frames

	a.t += 1
	
end

function control_player(pl)

	accel = 0.05
	if (btn(0)) pl.dx -= accel 
	if (btn(1)) pl.dx += accel 
	if (btn(2)) pl.dy -= accel 
	if (btn(3)) pl.dy += accel 
	
end

function _update()
	control_player(pl)
	foreach(actor, move_actor)
end

function draw_actor(a)
	local sx = (a.x * 8) - 4
	local sy = (a.y * 8) - 4
	spr(a.k + a.frame, sx, sy)
end

function _draw()
	cls()
	
	room_x=flr(pl.x/16)
	room_y=flr(pl.y/16)
	camera(room_x*128,room_y*128)
	
	map()
	foreach(actor,draw_actor)
	
end


```

## EditMode Tests
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.collide/Tests/Editor/CollideCartridgeTests.cs`.

