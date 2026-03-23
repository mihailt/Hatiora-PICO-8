# Dots3D Cartridge (`dots3d.p8`)

## Overview
A rotating 3D cube made of colorful spheres with specular highlights. A 7×7×7 grid of points in world space is rotated around two axes over time, projected with perspective onto the screen, depth-sorted so nearer spheres occlude farther ones, and drawn as filled circles with a small white highlight. No audio or input is used beyond the high-res toggle.

> [!WARNING]
> This cartridge draws hundreds of overlapping `Circfill` calls per frame. High-Res mode significantly increases GPU load. For performance testing, use the 128×128 lock via `Btnp(7)`.

## Source Locations
- **Native Lua Script**: `docs/references/pico-8/games/dots3d/dots3d.p8`
- **C# Translation**: `packages/com.hatiora.pico8.dots3d/Runtime/Dots3DCartridge.cs`
- **Unit Tests**: `packages/com.hatiora.pico8.dots3d/Tests/Editor/Dots3DCartridgeTests.cs`

## Implementation Details

### Resolution & Coordinate Scaling
When `_isHighRes = true`, `scale = P8.Width / 128f` scales all screen-space positions and radii. Two separate `Circfill` branches handle high-res (scaled) and native (1:1) drawing paths to keep integer casting correct.

### Key Routines

**Point Grid Generation** (`Init`) — Creates a 7×7×7 grid (343 points) in the `[-1, 1]` range with step `1/3`. Each point's color is assigned using `8 + LuaMod(x*2 + y*3, 8)`, cycling through palette colors 8–15. The upper bound uses `1.01f` to handle floating-point edge inclusion matching Lua's `for y=-1,1,1/3`.

**Rotation** (`Rot`) — An intentional "buggy" 2D rotation function preserved from the original Lua. The second component uses `x0` (the original `x`) instead of the already-rotated `x`, producing a slightly skewed but visually appealing rotation. Two sequential rotations (XZ then YZ) create the tumbling cube effect.

**Depth Sorting** — A 4-pass bidirectional bubble sort orders points from furthest to closest (`Cz` descending). The bidirectional (cocktail shaker) approach converges faster than single-direction for nearly-sorted data that shifts incrementally each frame.

**Perspective Projection & Drawing** — Each point is projected: `sx = 64 + cx*64/cz`, `sy = 64 + cy*64/cz`. A base radius `rad1 = 5 + cos(t/4)*4` pulses over time, and each point's radius is `rad1/cz` (depth-scaled). A white highlight circle at `1/3` size is offset toward the upper-right.

### API Implementation Map

| API Function | Native Lua | C# Translation | Deviation Rationale |
| :--- | :---: | :---: | :--- |
| `%` (Modulus) | ✅ Used | ✅ Used | Lua `%` always returns positive; C# `%` can return negative for negative operands. Replaced with `LuaMod()` using `FloorToInt` + conditional add. |
| `Btnp` | ❌ | ✅ Used | Added `Btnp(7)` for high-res toggle (not in original Lua). |
| `Circfill` | ✅ Used | ✅ Used | Coordinates and radii scaled by `scale` in high-res mode. |
| `Cls` | ✅ Used | ✅ Used | Lua `cls()` defaults to 0; C# explicitly passes `Cls(0)`. |
| `Cos` | ✅ Used | ✅ Used | Direct mapping (PICO-8 turn-based trigonometry). |
| `Max` | ❌ | ✅ Used | `Mathf.Max(1, ...)` prevents the white highlight from collapsing to radius 0 when scaled. |
| `Print` | ✅ Used | ❌ | Original Lua has `print(stat(1),2,2,7)` (CPU usage debug) commented out. Omitted entirely in C#. |
| `Sin` | ✅ Used | ✅ Used | Direct mapping (PICO-8 turn-based trigonometry). |
| `Time` | ✅ Used | ✅ Used | Direct mapping via `Time()`. |

### Raw C# Source
```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Dots3D
{
    public class Dots3DCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData => null;
        public string MusicData => null;
        public string MapData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Dots3D/dots3d/Gfx/gfx");
        public Texture2D LabelTexture => null;

        private class Point
        {
            public float X, Y, Z;
            public float Cx, Cy, Cz;
            public int Col;
        }

        private System.Collections.Generic.List<Point> _pt;
        private bool _isHighRes = true;

        private int LuaMod(float a, int b)
        {
            int ia = Mathf.FloorToInt(a);
            int m = ia % b;
            return m < 0 ? m + b : m;
        }

        public override void Init()
        {
            _pt = new System.Collections.Generic.List<Point>();
            for (float y = -1f; y <= 1.01f; y += 1f / 3f)
            {
                for (float x = -1f; x <= 1.01f; x += 1f / 3f)
                {
                    for (float z = -1f; z <= 1.01f; z += 1f / 3f)
                    {
                        var p = new Point { X = x, Y = y, Z = z };
                        p.Col = 8 + LuaMod(x * 2f + y * 3f, 8);
                        _pt.Add(p);
                    }
                }
            }
        }

        public override void Update() 
        { 
            if (Btnp(7)) _isHighRes = !_isHighRes;
        }
        
        private (float x, float y) Rot(float x, float y, float a)
        {
            float x0 = x;
            float rx = Cos(a) * x - Sin(a) * y;
            float ry = Cos(a) * y + Sin(a) * x0; // *x0 matches lua "*x is wrong but kinda nice too"
            return (rx, ry);
        }

        public override void Draw()
        {
            Cls(0);
            
            float t = Time();
            float scale = _isHighRes ? P8.Width / 128f : 1f;

            foreach (var p in _pt)
            {
                // transform world space -> camera space
                var rot1 = Rot(p.X, p.Z, t / 8f);
                p.Cx = rot1.x;
                p.Cz = rot1.y;

                var rot2 = Rot(p.Y, p.Cz, t / 7f);
                p.Cy = rot2.x;
                p.Cz = rot2.y;

                p.Cz += 2f + Cos(t / 6f);
            }

            // sort furthest -> closest
            for (int pass = 1; pass <= 4; pass++)
            {
                for (int i = 0; i < _pt.Count - 1; i++)
                {
                    if (_pt[i].Cz < _pt[i + 1].Cz)
                    {
                        var temp = _pt[i];
                        _pt[i] = _pt[i + 1];
                        _pt[i + 1] = temp;
                    }
                }
                for (int i = _pt.Count - 2; i >= 0; i--)
                {
                    if (_pt[i].Cz < _pt[i + 1].Cz)
                    {
                        var temp = _pt[i];
                        _pt[i] = _pt[i + 1];
                        _pt[i + 1] = temp;
                    }
                }
            }

            float rad1 = 5f + Cos(t / 4f) * 4f;

            foreach (var p in _pt)
            {
                // transform camera space -> screen space
                float sx = 64f + p.Cx * 64f / p.Cz;
                float sy = 64f + p.Cy * 64f / p.Cz;
                float rad = rad1 / p.Cz;

                if (p.Cz > 0.1f)
                {
                    if (_isHighRes)
                    {
                        Circfill((int)(sx * scale), (int)(sy * scale), (int)(rad * scale), p.Col);
                        Circfill((int)((sx + rad / 3f) * scale), (int)((sy - rad / 3f) * scale), Mathf.Max(1, (int)((rad / 3f) * scale)), 7);
                    }
                    else
                    {
                        Circfill((int)sx, (int)sy, (int)rad, p.Col);
                        Circfill((int)(sx + rad / 3f), (int)(sy - rad / 3f), Mathf.Max(1, (int)(rad / 3f)), 7);
                    }
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
Directly extracted from `docs/references/pico-8/games/dots3d/dots3d.p8`:
```lua
-- File: dots3d.p8
-- 3d dot party
-- by zep

function _init()
	-- make some points
	pt={}
	for y=-1,1,1/3 do
		for x=-1,1,1/3 do
			for z=-1,1,1/3 do
				p={}
				p.x=x p.y=y p.z=z
				p.col=8 + (x*2+y*3)%8
				add(pt,p)
			end
		end
	end
	
end

-- rotate point x,y by a
-- (rotates around 0,0)
function rot(x,y,a)
	local x0=x
	x = cos(a)*x - sin(a)*y
	y = cos(a)*y + sin(a)*x0 -- *x is wrong but kinda nice too
	return x,y
end

function _draw()
	cls()
	
	for p in all(pt) do
		--transform:
		--world space -> camera space
		
		p.cx,p.cz=rot(p.x,p.z,t()/8)
		p.cy,p.cz=rot(p.y,p.cz,t()/7)
		
		p.cz += 2 + cos(t()/6)
	end
	
	-- sort furthest -> closest
	-- (so that things in distance
	-- aren't drawn over things
	-- in the foreground)
	
	for pass=1,4 do
	for i=1,#pt-1 do
		if pt[i].cz < pt[i+1].cz then
			--swap
			pt[i],pt[i+1]=pt[i+1],pt[i]
		end
	end
	for i=#pt-1,1,-1 do
		if pt[i].cz < pt[i+1].cz then
			--swap
			pt[i],pt[i+1]=pt[i+1],pt[i]
		end
	end
	end
	
	rad1 = 5+cos(t()/4)*4
	for p in all(pt) do
		--transform:
		--camera space -> screen space
		sx = 64 + p.cx*64/p.cz
		sy = 64 + p.cy*64/p.cz
		rad= rad1/p.cz
		-- draw
		
		if (p.cz > .1) then
			circfill(sx,sy,rad,p.col)
			circfill(sx+rad/3,sy-rad/3,rad/3,7)
		end
	end

--print(stat(1),2,2,7)
end

```

## EditMode Tests
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.dots3d/Tests/Editor/Dots3DCartridgeTests.cs`.

