# Jelpi Cartridge (`jelpi.p8`)

## Overview
A 2-player side-scrolling platformer demo by zep. Players control cat-like characters through multi-screen levels collecting gems, smashing loot crates, and defeating enemies via a directional dash attack. Features include parallax-scrolling themed backgrounds (forests, gardens, mountains), enemy AI (walking monsters, flying birds, tongue-lashing frogs, orbiting swirlies), bounce mushrooms, bridge-builder pickups, split-screen 2-player mode, a stage-clear gem tally with animated bounce-in, and a smooth palette fade-out on level transitions and death restarts. Music plays per-level (patterns 0, 4, 16 for levels 1-3).

## Source Locations
- **Native Lua Script**: `docs/references/pico-8/games/jelpi/jelpi.p8`
- **C# Translation**: `packages/com.hatiora.pico8.jelpi/Runtime/JelpiCartridge.cs`

## Implementation Details

### Resolution & Coordinate Scaling
When `_isHighRes = true`, `scale = P8.Width / 128f`. All clip regions, camera offsets, split-screen dividers, and UI text positions are multiplied by `s = Max(1, (int)scale)`. World rendering (Map, Spr) uses the engine's scaled `Map(scale: s)` and `Sspr` with `dw/dh = 8*s` to render tiles and sprites at hi-res. A `_drawScale` field makes scale accessible to all actor/sparkle draw functions.

### Key Routines

**Actor System** - Lua uses table-based polymorphism (`a.move`, `a.draw`); C# mirrors this with `Action<JelpiCartridge, Actor>` delegate fields (`Move`, `DrawFn`). An `Actor` class holds all Lua table fields (position, velocity, dash, life, standing, etc.). `MakeActor()` applies sprite-flag-driven defaults (flag 6 = pickup, flag 7 = monster, flag 4 = zero gravity) then per-type overrides via `ApplyActorData()`.

**Physics** (`MoveActor`) - Axis-separated collision using `Solid()`/`Smash()` lookups on the tile map. Horizontal movement tests `x + dx +/- 1/4` against tiles; vertical uses separate up/down branches with bounce and floor-snap. A pop-up loop (`while solid, y -= 0.125`) prevents embedding. Dashing players use `Smash()` which destroys flag-4 tiles and spawns loot. Jump-up-through platforms (flag 6) allow upward passage when `y%1 > 0.5`.

**Monster AI** - Four specialist routines:
- `MoveMonster`: basic patrol (walk + turn on wall hit), death via `BangPuff` sparkle explosions.
- `MoveFrog`: idle-stand jump AI with a tongue-grab attack using sine-wave extension (`Sin(tongueT/48) * 5`). Holds grabbed players until retract.
- `MoveBird`: flying AI that patrols a sine-wave orbit around home, picks up items/players, flaps on a 12-tick cycle with directional thrust.
- `MoveSwirly`: floating enemy with simple steering toward closest player, alternating between circling above and swooping, radius-based collision.

**Parallax Backgrounds** (`DrawWorld`) - A two-pass element system driven by `ThemeData` per-level. Pass 0 draws far elements (depth > 1) then the map/actors layer (`DrawZ1`); pass 1 draws foreground elements (depth <= 1). Each element has: map source rect, world position with depth-based parallax, optional auto-scroll via `scrollDx * Time()`, palette swaps, `fill_up`/`fill_down` colored bands, and tiling repeat.

**Fade-Out** (`StartFade` / `ApplyFadePalette`) - Frame-based palette darkening matching Lua's `fade_out()`. The `dpal` table maps each color to a darker neighbor; on each frame `i` (0-40), color `j` is darkened by applying `dpal` `floor((i + j%5) / 4)` times. Uses the display palette (`Pal(j, col, 1)`) which is applied as a post-process at present time. A one-frame transition state (`_fadeT = -2`) renders full black to prevent a flash of old scene data before `InitLevel`.

> [!IMPORTANT]
> The `dpal` table is 1-indexed in Lua but 0-indexed in C#. The C# array is prepended with `0` to shift all values: `{0, 0, 1, 1, 2, 1, 13, 6, 4, 4, 9, 3, 13, 1, 13, 14}`.

**Stage Clear** (`DrawFinished`) - Two-pass gem counter: pass 0 draws all `total_gems` as gray silhouettes (`Pal(i, 13)` for all colors), pass 1 draws only collected gems in full color with a sine-bounce animation (`Sin(t2/8) * 4 / (t2/2)`) and per-gem `Sfx(25)` on first reveal. Header text with shadow, and continue prompt after 45 frames.

**Camera** (`UpdateCamera`) - Tracks player position with smoothing via `Mid(0, px*8 - half, mapWidth*8 - viewWidth)` clamping. Split-screen mode halves the viewport width and determines left/right assignment by comparing `pl[1].x` vs `pl[2].x`.

**Level Loading** (`InitLevel`) - Calls `Reload()` to restore map/flag ROM, then uses `Memcpy(0x2000, 0x1000 + ((lev+1)%4) * 0x800, 0x800)` to copy the correct level section from the lower map region. Scans map rows 0-15 for player spawn (tile 72), gem count (tile 67), and loot crates (tile 48). Loot is shuffled with ~25% booby prizes (frogs or mushrooms).


### API Implementation Map
The following table outlines the native PICO-8 functions used in the original `.p8` script and their C# translation counterparts:

| API Function | Native Lua Script | C# Translation | Deviation Rationale |
| :--- | :---: | :---: | :--- |
| `Abs` | ✅ Used | ✅ Used | Direct mapping. |
| `Atan2` | ✅ Used | ✅ Used | Direct mapping (PICO-8 turn-based). |
| `Btn` | ✅ Used | ✅ Used | Lua `btn()>0` (no-arg bitmask) mapped to `AnyBtn()` helper. |
| `Btnp` | ✅ Used | ✅ Used | Added `Btnp(7)` for hi-res toggle (not in original). |
| `Camera` | ✅ Used | ✅ Used | Direct mapping. |
| `Circ` | ✅ Used | ❌ | Unused in relevant code paths; `Circfill` used instead. |
| `Circfill` | ✅ Used | ✅ Used | Direct mapping. Used for sparkle effects. |
| `Clip` | ✅ Used | ✅ Used | Direct mapping. Split-screen viewport clipping. |
| `Cls` | ✅ Used | ✅ Used | Direct mapping. |
| `Color` | ✅ Used | ✅ Used | Direct mapping. |
| `Cos` | ✅ Used | ✅ Used | Direct mapping (PICO-8 turn-based trigonometry). |
| `Fget` | ✅ Used | ✅ Used | Direct mapping. Tile/sprite flag checks. |
| `Flr` | ✅ Used | ✅ Used | Explicit in C# where Lua auto-truncates. |
| `Line` | ✅ Used | ✅ Used | Sky gradient, split-screen divider, spinning sparkles. |
| `Map` | ✅ Used | ✅ Used | Uses new `scale` parameter for hi-res tile rendering. |
| `Max` | ✅ Used | ✅ Used | Direct mapping. |
| `Memcpy` | ✅ Used | ✅ Used | Copies level map sections from ROM. |
| `Mget` | ✅ Used | ✅ Used | Direct mapping. |
| `Mid` | ✅ Used | ✅ Used | Direct mapping. |
| `Min` | ✅ Used | ✅ Used | Direct mapping. |
| `Mset` | ✅ Used | ✅ Used | Direct mapping. |
| `Music` | ✅ Used | ✅ Used | Level-specific patterns. |
| `Pal` | ✅ Used | ✅ Used | Mode 0 (draw) and mode 1 (display) used for fade-out. |
| `Palt` | ✅ Used | ✅ Used | Direct mapping. |
| `Pget` | ✅ Used | ❌ | Not used in ported code paths. |
| `Poke` | ✅ Used | ✅ Used | Used for glitch/corrupt mode. |
| `Print` | ✅ Used | ✅ Used | Scaled coords via `CoordMode.Virtual` with scale parameter. |
| `Pset` | ✅ Used | ✅ Used | Direct mapping. |
| `Rect` | ✅ Used | ✅ Used | Sign text frame. |
| `Rectfill` | ✅ Used | ✅ Used | Background fill bands and sign text. |
| `Reload` | ✅ Used | ✅ Used | Restores map/flag data from ROM snapshot. |
| `Rnd` | ✅ Used | ✅ Used | Direct mapping. |
| `Sfx` | ✅ Used | ✅ Used | Direct mapping. |
| `Sget` | ✅ Used | ✅ Used | Used by `AtomizeSprite` for pixel decomposition. |
| `Sgn` | ✅ Used | ✅ Used | Direct mapping. |
| `Sin` | ✅ Used | ✅ Used | Direct mapping (PICO-8 turn-based). |
| `Spr` | ✅ Used | ❌ | All `Spr` calls replaced with `Sspr` for hi-res scaling support. |
| `Sqrt` | ✅ Used | ✅ Used | Direct mapping. |
| `Srand` | ✅ Used | ❌ | Not implemented; game logic doesn't depend on seeded RNG. |
| `Sspr` | ❌ | ✅ Used | Replaces all `Spr` usage; supports scaled `dw/dh = 8*s`. |
| `Time` | ✅ Used | ✅ Used | Lua shorthand `t()` mapped to `Time()`. |


### Raw C# Source
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Jelpi
{
    /// <summary>
    /// Jelpi platformer demo by zep.
    /// 2-player platformer with enemies, dash, pickups, parallax backgrounds, and level transitions.
    /// </summary>
    public class JelpiCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null;

        public string SfxData   => Resources.Load<TextAsset>("Jelpi/jelpi/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Jelpi/jelpi/Music/music")?.text;
        public string MapData   => Resources.Load<TextAsset>("Jelpi/jelpi/Map/map")?.text;
        public string GffData   => Resources.Load<TextAsset>("Jelpi/jelpi/Gff/gff")?.text;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Jelpi/jelpi/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Jelpi/jelpi/Label/label");

        private bool _isHighRes = true;

        // ─── Configuration ───
        private int _level = 1;
#pragma warning disable CS0162 // Unreachable code (NumPlayers is const)
        private const int NumPlayers = 1;
        private const bool CorruptMode = false;
        private const bool PlayMusic = true;
        private const int MaxActors = 64;

        // ─── State ───
        private readonly List<Actor> _actor = new List<Actor>();
        private readonly List<Sparkle> _sparkle = new List<Sparkle>();
        private Actor[] _pl;
        private List<int> _loot = new List<int>();
        private int _levelT, _deathT, _finishedT, _gems, _totalGems;
        private bool _glitchMushroom;
        private float _camX, _camY, _ccyT, _ccy;
        private bool _split;
        private Dictionary<int, bool> _gemSfx = new Dictionary<int, bool>();
        private int _drawScale = 1; // current hi-res scale for draw functions

        // Fade state
        private int _fadeT = -1;          // -1 = not fading, 0..40 = fade frame
        private int _fadePendingLevel;    // level to init when fade completes

        // ─── Actor class ───
        private class Actor
        {
            public int k;
            public float x, y, dx, dy;
            public float homex, homey;
            public float frame;
            public int frames = 4;
            public int life = 1;
            public int hitT;
            public float ddx = 0.02f, ddy = 0.06f;
            public float w = 3f / 8f, h = 0.5f;
            public int d = -1;
            public float bounce = 0.8f;
            public float friction = 0.9f;
            public bool canBump = true;
            public int dash, super_, delay;
            public int t;
            public bool standing;
            public bool isPlayer, isMonster, isPickup;
            public int score;
            public int id; // player id (0 or 1)
            public float deathT;
            public bool hitWall;
            public int activeT; // mushroom
            public float tongueT, tongueX; // frog
            public Actor ha; // frog hold
            public Actor holding; // bird/swirly hold
            public List<List<TailSeg>> tail; // swirly
            public float r; // swirly collision radius
            public Action<JelpiCartridge, Actor> Move;
            public Action<JelpiCartridge, Actor> DrawFn;
        }

        private class TailSeg
        {
            public float x, y, r, slen;
        }

        private class Sparkle
        {
            public float x, y, dx, dy, ddy;
            public float k;
            public int frames = 1;
            public int col;
            public int t;
            public int maxT;
            public float frame;
            public float spin;
        }

        // ─── Actor Data ───
        private static readonly Dictionary<int, Action<Actor>> ActorDatSetup = new Dictionary<int, Action<Actor>>(); // populated in InitActorData

        // ─── Factories ───
        private Actor MakeActor(int k, float x, float y, int d = -1)
        {
            var a = new Actor
            {
                k = k, x = x, y = y, dx = 0, dy = 0,
                homex = x, homey = y,
                frame = 0, frames = 4, life = 1, hitT = 0,
                ddx = 0.02f, ddy = 0.06f,
                w = 3f / 8f, h = 0.5f,
                d = d, bounce = 0.8f, friction = 0.9f,
                canBump = true, dash = 0, super_ = 0, t = 0,
                standing = false,
                Move = MoveActor,
                DrawFn = DrawActor
            };

            if (Fget(k, 6) != 0) a.isPickup = true;
            if (Fget(k, 7) != 0) { a.isMonster = true; a.Move = MoveMonster; }
            if (Fget(k, 4) != 0) a.ddy = 0;

            // Apply per-type overrides
            ApplyActorData(a, k);

            if (_actor.Count < MaxActors) _actor.Add(a);
            return a;
        }

        private void ApplyActorData(Actor a, int k)
        {
            switch (k)
            {
                case 53: // bridge builder
                    a.ddy = 0; a.friction = 1; a.Move = MoveBuilder; a.DrawFn = DummyDraw; break;
                case 64: // charge powerup
                    a.DrawFn = DrawChargePowerup; break;
                case 65: // exit
                    a.DrawFn = DrawExit; break;
                case 80: // swirly
                    a.life = 2; a.frames = 1; a.bounce = 0; a.ddy = 0;
                    a.Move = MoveSwirly; a.DrawFn = DrawSwirly;
                    a.canBump = false; a.d = 0; a.r = 5; break;
                case 82: // bouncy mushroom
                    a.ddx = 0; a.frames = 1; a.activeT = 0; a.Move = MoveMushroom; break;
                case 84: // glitch mushroom
                    a.DrawFn = DrawGlitchMushroom; break;
                case 93: // bird
                    a.Move = MoveBird; a.DrawFn = DrawBird;
                    a.bounce = 0; a.ddy = 0.03f; break;
                case 96: // frog
                    a.Move = MoveFrog; a.DrawFn = DrawFrog;
                    a.bounce = 0; a.friction = 1; a.tongueT = 0; break;
                case 116: // tail
                    a.DrawFn = DrawTail; break;
            }
        }

        private Sparkle MakeSparkle(float k, float x, float y, int col = 0)
        {
            var s = new Sparkle
            {
                x = x, y = y, k = k, frames = 1, col = col,
                t = 0, maxT = 8 + (int)Rnd(4), dx = 0, dy = 0, ddy = 0
            };
            if (_sparkle.Count < 512) _sparkle.Add(s);
            return s;
        }

        private Actor MakePlayer(int k, float x, float y, int d = 1)
        {
            var a = MakeActor(k, x, y, d);
            a.isPlayer = true;
            a.Move = MovePlayer;
            a.score = 0; a.bounce = 0; a.delay = 0; a.id = 0;
            return a;
        }

        // ─── Init ───
        public override void Init()
        {
            InitLevel(_level);
        }

        private void InitLevel(int lev)
        {
            Cls();
            _level = lev;
            _levelT = 0; _deathT = 0; _finishedT = 0;
            _gems = 0; _totalGems = 0;
            _glitchMushroom = false;
            _gemSfx.Clear();
            Music(-1);

            if (PlayMusic)
            {
                if (_level == 1) Music(0);
                if (_level == 2) Music(4);
                if (_level == 3) Music(16);
            }

            _actor.Clear();
            _sparkle.Clear();
            _loot.Clear();

            // Reload map and flag data from ROM
            Reload();

            if (_level <= 4)
            {
                // Copy section of map: memcpy(0x2000, 0x1000+((lev+1)%4)*0x800, 0x800)
                Memcpy(0x2000, 0x1000 + ((_level + 1) % 4) * 0x800, 0x800);
            }

            // Spawn player from map
            _pl = new Actor[3]; // 1-indexed: pl[1], pl[2]
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    int val = Mget(x, y);
                    if (val == 72)
                    {
                        ClearCel(x, y);
                        _pl[1] = MakePlayer(72, x + 0.5f, y + 0.5f, 1);
                        if (NumPlayers == 2)
                        {
                            _pl[2] = MakePlayer(88, x + 2, y + 1, 1);
                            _pl[2].id = 1;
                        }
                    }
                    if (val == 67) _totalGems++;
                    if (val == 48) _loot.Add(67);
                }
            }

            // Shuffle loot
            int numBooby = 0;
            if (_loot.Count > 1)
            {
                numBooby = Flr((_loot.Count + 2) / 4f);
                for (int i = 0; i < numBooby; i++)
                {
                    _loot[i] = 96;
                    if (Rnd(10) < 1) _loot[i] = 84;
                }
                for (int i = 0; i < _loot.Count; i++)
                {
                    int j = Flr(Rnd(_loot.Count));
                    int k = Flr(Rnd(_loot.Count));
                    if (j >= 0 && j < _loot.Count && k >= 0 && k < _loot.Count)
                        (_loot[j], _loot[k]) = (_loot[k], _loot[j]);
                }
            }
            _totalGems += _loot.Count - numBooby;

            if (_pl[1] == null)
                _pl[1] = MakePlayer(72, 4, 4, 1);
        }


        // ─── Map helpers ───
        private void ClearCel(int x, int y)
        {
            int val0 = x > 0 ? Mget(x - 1, y) : -1;
            int val1 = x < 127 ? Mget(x + 1, y) : -1;
            if ((x > 0 && val0 == 0) || (x < 127 && val1 == 0))
                Mset(x, y, 0);
            else if (val1 >= 0 && Fget(val1, 1) == 0)
                Mset(x, y, val1);
            else if (val0 >= 0 && Fget(val0, 1) == 0)
                Mset(x, y, val0);
            else
                Mset(x, y, 0);
        }

        private void MoveSpawns(float x0)
        {
            int ix0 = Flr(x0);
            for (int y = 0; y <= 16; y++)
            {
                for (int x = ix0 - 10; x <= Mathf.Max(16, ix0 + 14); x++)
                {
                    int val = Mget(x, y);
                    if (Fget(val, 5) != 0)
                    {
                        MakeActor(val, x + 0.5f, y + 1);
                        ClearCel(x, y);
                    }
                }
            }
        }

        // ─── Collision detection ───
        private bool Solid(float x, float y, bool ignore = false)
        {
            if (x < 0 || x >= 128) return true;
            int val = Mget(Flr(x), Flr(y));

            // Flag 6: jump-up-through platform
            if (Fget(val, 6) != 0)
            {
                if (ignore) return false;
                if (y % 1 > 0.5f) return Solid(x, y + 1);
            }
            return Fget(val, 1) != 0;
        }

        private bool Smash(float x, float y, bool ignore = false)
        {
            int ix = Flr(x), iy = Flr(y);
            int val = Mget(ix, iy);
            if (Fget(val, 4) == 0) return Solid(x, y, ignore);

            // Spawn from loot crate
            if (val == 48 && _loot.Count > 0)
            {
                int lootVal = _loot[_loot.Count - 1];
                var a = MakeActor(lootVal, ix + 0.5f, iy - 0.2f);
                a.dy = -0.8f;
                a.d = Flr(Rnd(2)) * 2 - 1;
                if (lootVal == 80) a.d = 0; // swirly: no horizontal
                _loot.RemoveAt(_loot.Count - 1);
            }

            ClearCel(ix, iy);
            Sfx(10);

            // Make debris
            for (int by = 0; by <= 1; by++)
            {
                for (int bx = 0; bx <= 1; bx++)
                {
                    var s = MakeSparkle(22, 0.25f + ix + bx * 0.5f, 0.25f + iy + by * 0.5f, 0);
                    s.dx = (bx - 0.5f) / 4f;
                    s.dy = (by - 0.5f) / 4f;
                    s.maxT = 30;
                    s.ddy = 0.02f;
                }
            }
            return false; // not solid (it was smashed)
        }

        // ─── Actor movement ───
        private void MoveActor(JelpiCartridge cart, Actor a)
        {
            if (a.life <= 0) { _actor.Remove(a); return; }
            a.standing = false;

            // Solid function: use smash when dashing
            Func<float, float, bool, bool> ssolid = a.dash > 0 ? Smash : (Func<float, float, bool, bool>)Solid;
            Func<float, float, bool, bool> ssolidd = a.dash > 0 && Btn(3, a.id) ? Smash : (Func<float, float, bool, bool>)Solid;

            bool ign = a.ddy > 0;

            // X movement
            float x1 = a.x + a.dx + Sgn(a.dx) / 4f;
            if (!ssolid(x1, a.y - 0.5f, ign))
            {
                a.x += a.dx;
            }
            else
            {
                if (a.dash > 0) Sfx(12);
                a.dx *= -1;
                a.hitWall = true;
                if (a.isMonster) { a.d *= -1; a.dx = 0; }
            }

            // Y movement
            float fw = 0.25f;
            if (a.dy < 0)
            {
                // Going up
                if (ssolid(a.x - fw, a.y + a.dy - 1, ign) || ssolid(a.x + fw, a.y + a.dy - 1, ign))
                {
                    a.dy = 0;
                    a.y = Flr(a.y + 0.5f);
                }
                else
                {
                    a.y += a.dy;
                }
            }
            else
            {
                // Going down
                float y1 = a.y + a.dy;
                if (ssolidd(a.x - fw, y1, false) || ssolidd(a.x + fw, y1, false))
                {
                    if (a.bounce > 0 && a.dy > 0.2f)
                        a.dy = a.dy * -a.bounce;
                    else
                    {
                        a.standing = true;
                        a.dy = 0;
                    }
                    a.y = Flr(a.y + 0.75f);
                }
                else
                {
                    a.y += a.dy;
                }

                // Pop up
                int popSafety = 0;
                while (Solid(a.x, a.y - 0.05f) && popSafety++ < 100)
                    a.y -= 0.125f;
            }

            // Gravity and friction
            a.dy += a.ddy;
            a.dy *= 0.95f;
            a.dx *= a.friction;
            if (a.standing) a.dx *= a.friction;
            a.t += 1;
        }

        // ─── Player control ───
        private void MovePlayer(JelpiCartridge cart, Actor pl)
        {
            MoveActor(cart, pl);

            // Fall off the map → die
            if (pl.life > 0 && pl.y > 18) pl.life = 0;

            // Death: create sparkles and set timer (once only)
            if (pl.life <= 0)
            {
                if (pl.deathT == 0)
                {
                    for (int i = 1; i <= 32; i++)
                    {
                        var s = MakeSparkle(69, pl.x, pl.y - 0.6f);
                        s.dx = Cos(i / 32f) / 2f;
                        s.dy = Sin(i / 32f) / 2f;
                        s.maxT = 30;
                        s.ddy = 0.01f;
                        s.frame = 69 + Rnd(3);
                        s.col = 7;
                    }
                    Sfx(17);
                    pl.deathT = Time();
                }
                return;
            }

            int b = pl.id;
            float accel = 0.05f;
            float q = 0.7f;

            if (pl.dash > 10) accel = 0.08f;
            if (pl.super_ > 0) { q *= 1.5f; accel *= 1.5f; }
            if (!pl.standing) accel /= 2f;

            if (Btn(0, b)) { pl.dx -= accel; pl.d = -1; }
            if (Btn(1, b)) { pl.dx += accel; pl.d = 1; }
            if (Btn(4, b) && pl.standing) { pl.dy = -0.7f; Sfx(8); }

            // Dash/charge
            if (Btn(5, b) && pl.delay == 0)
            {
                pl.dash = 15; pl.delay = 20;
                float cdx = 0, cdy = 0;
                if (Btn(0, b)) cdx -= q;
                if (Btn(1, b)) cdx += q;
                if (Btn(2, b)) cdy -= q;
                if (Btn(3, b)) cdy += q;

                if (cdx == 0 && cdy == 0)
                    pl.dx += pl.d * 0.4f;
                else
                {
                    float aa = Atan2(cdx, cdy);
                    pl.dx += Cos(aa) / 2f;
                    pl.dy += Sin(aa) / 3f;
                    pl.dy = Max(-0.5f, pl.dy);
                }
                if (!pl.standing) pl.dy -= 0.2f;
                Sfx(11);
            }

            if (pl.super_ > 0) pl.dash = 2;

            // Dash sparkles
            if (pl.dash > 0 && (Abs(pl.dx) > 0.4f || Abs(pl.dy) > 0.2f))
            {
                for (int i = 1; i <= 3; i++)
                {
                    var s = MakeSparkle(69 + Rnd(3), pl.x + pl.dx * i / 3f, pl.y + pl.dy * i / 3f - 0.3f, (pl.t * 3 + i) % 9 + 7);
                    if (Rnd(2) < 1) s.col = 7;
                    s.dx = -pl.dx * 0.1f;
                    s.dy = -0.05f * i / 4f;
                    s.x += Rnd(0.6f) - 0.3f;
                    s.y += Rnd(0.6f) - 0.3f;
                }
            }

            pl.dash = (int)Max(0, pl.dash - 1);
            pl.delay = (int)Max(0, pl.delay - 1);
            pl.super_ = (int)Max(0, pl.super_ - 1);

            // Animation
            if (pl.standing)
                pl.frame = (pl.frame + Abs(pl.dx) * 2) % pl.frames;
            else
                pl.frame = (pl.frame + Abs(pl.dx) / 2f) % pl.frames;
            if (Abs(pl.dx) < 0.1f) pl.frame = 0;
        }

        // ─── Monster ───
        private void MoveMonster(JelpiCartridge cart, Actor m)
        {
            MoveActor(cart, m);
            if (m.life <= 0)
            {
                BangPuff(m.x, m.y - 0.5f, 104);
                Sfx(14);
                return;
            }
            m.dx += m.d * m.ddx;
            m.frame = (m.frame + Abs(m.dx) * 3 + 4) % m.frames;
            if (m.hitT > 0) m.hitT -= 1;
        }

        // ─── Monster hit ───
        private void MonsterHit(Actor m)
        {
            if (m.hitT > 0) return;
            m.life -= 1;
            m.hitT = 15;
            m.dx /= 4f;
            m.dy /= 4f;
            if (m.life > 0) Sfx(21);
        }

        // ─── Mushroom ───
        private void MoveMushroom(JelpiCartridge cart, Actor a)
        {
            a.frame = 0;
            if (a.activeT > 0) { a.activeT--; a.frame = 1; }
        }

        // ─── Builder ───
        private void MoveBuilder(JelpiCartridge cart, Actor a)
        {
            int ix = Flr(a.x), iy = Flr(a.y - 0.5f);
            int val = Mget(ix, iy);
            if (val == 0)
            {
                Mset(ix, iy, 53);
                Sfx(19);
            }
            else if (val != 53)
            {
                _actor.Remove(a);
            }
            a.t += 1;
            if (a.x < 1 || a.x > 126 || a.t > 30) _actor.Remove(a);

            for (float i = 0; i <= 0.2f; i += 0.1f)
            {
                var s = MakeSparkle(104, a.x, a.y - 0.5f);
                s.dx = Cos(i + a.x / 4f) / 8f;
                s.dy = Sin(i + a.x / 4f) / 8f;
                s.col = 10;
                s.maxT = 10 + (int)Rnd(10);
            }
            a.x += a.dx;
        }

        // ─── Frog ───
        private void MoveFrog(JelpiCartridge cart, Actor a)
        {
            MoveActor(cart, a);
            if (a.life <= 0) { BangPuff(a.x, a.y - 0.5f, 104); Sfx(26); return; }
            a.frame = 0;
            var p = ClosestP(a, 16);

            if (a.standing)
            {
                a.dy = 0; a.dx = 0;
                if (Rnd(20) < 1 && a.tongueT == 0)
                {
                    if (Rnd(3) < 2 && p != null) a.d = (int)Sgn(p.x - a.x);
                    a.dy = -0.6f - Rnd(0.4f);
                    a.dx = a.d / 4f;
                    a.standing = false;
                    Sfx(23);
                }
            }
            else a.frame = 1;

            // Tongue
            if (a.tongueT == 0 && p != null && Abs(a.x - p.x) < 5 && Rnd(20) < 1 && a.standing)
                a.tongueT = 1;

            if (a.tongueT > 0)
            {
                a.frame = 2;
                a.tongueT = (a.tongueT + 1) % 24;
                float tlen = Sin(a.tongueT / 48f) * 5;
                a.tongueX = a.x - tlen * a.d;

                if (a.ha == null && p != null)
                {
                    float tdx = p.x - a.tongueX;
                    float tdy = p.y - a.y;
                    if (tdx * tdx + tdy * tdy < 0.49f) { a.ha = p; Sfx(22); }
                }

                if (Solid(a.tongueX, a.y - 0.5f) && a.tongueT < 11)
                    a.tongueT = 24 - a.tongueT;
            }

            if (a.ha != null)
            {
                if (a.tongueT > 0) { a.ha.x = a.tongueX; a.ha.y = a.y; }
                else a.ha = null;
            }
            a.t += 1;
        }

        // ─── Bird ───
        private void MoveBird(JelpiCartridge cart, Actor a)
        {
            MoveActor(cart, a);
            if (a.holding != null)
            {
                a.holding.x = a.x;
                a.holding.y = a.y + 0.0546875f; // 0x0.e
                a.holding.dy = 0;
                if (a.standing) a.holding.x -= a.d / 2f;
                if (a.holding.life == 0) { a.holding = null; Sfx(28); }
            }

            var p = ClosestP(a, 12);
            float tx = a.homex + Cos(a.t / 120f) * 6;
            float ty = a.homey + Sin(a.t / 160f) * 4;
            if (p != null) { tx = p.x; ty = p.y - 3; }

            Actor a2 = null;
            if (a.holding == null)
            {
                a2 = ClosestA(a, _actor, true);
                if (a2 != null && Abs(a2.x - a.x) < 4 && Abs(a2.y - a.y) < 4)
                {
                    p = null; tx = a2.x; ty = a2.y;
                    if (a.standing) a.dy = -0.1f;
                }
                else a2 = null;
            }

            float bdx = tx - a.x, bdy = ty - a.y;
            float dd = Sqrt(bdx * bdx + bdy * bdy);
            if (a2 != null && dd < 1) { a.holding = a2; Sfx(28); }
            if (a.t % 8 == 0) a.d = (int)Sgn(bdx);

            if (a.standing)
            {
                a.frame = 0;
                if (!Solid(a.x, a.y + 0.2f)) a.dy = -0.2f;
                if (p != null && dd < 5) a.dy = -0.3f;
                a.dx = 0;
            }
            else
            {
                int tt = a.t % 12;
                a.frame = 1 + tt / 6f;
                if (tt == 6)
                {
                    float mag = 0.3f;
                    if (dd < 4 && a.y > ty) mag = 0.4f;
                    if (a.hitWall) mag = 0.45f;
                    if (p != null && a.y > ty && a.holding == null) mag = 0.45f;
                    a.hitWall = false;
                    a.dy -= mag;
                }
                if (a.dy < 0.2f) a.dx += a.d / 64f;
            }
            a.frame = a.standing ? 0 : 1 + (a.t / 4f) % 2;
        }

        // ─── Swirly (simplified — no tail physics for initial port) ───
        private void MoveSwirly(JelpiCartridge cart, Actor a)
        {
            a.t += 1;
            if (a.hitT > 0) a.hitT -= 1;

            if (a.life == 0 && a.t % 4 == 0) { _actor.Remove(a); Sfx(27); return; }

            a.x += a.dx; a.y += a.dy;
            a.dx *= 0.95f; a.dy *= 0.95f;

            float tx = a.homex, ty = a.homey;
            var p = ClosestP(a, 200);
            if (p != null) { tx = p.x; ty = p.y; }

            float accel = 1f / 64f;

            if (((a.t % 360 < 180 && a.life > 1) || a.life == 0) && Abs(a.x - tx) < 12)
                ty -= 6;
            else
            {
                accel = 1f / 40f;
                if (Abs(a.x - tx) > 12) accel *= 1.5f;
            }

            a.d = (int)Sgn(tx - a.x); // simplified direction
            a.dx += (tx - a.x) > 0 ? accel : -accel;
            a.dy += (ty - a.y) > 0 ? accel : -accel;

            // Collision with players
            if (p != null && a.life > 0)
            {
                float pdx = p.x - a.x, pdy = (p.y - 0.5f) - a.y;
                float pdd = Sqrt(pdx * pdx + pdy * pdy);
                if (pdd < 1.2f)
                {
                    float aa = Atan2(pdx, pdy);
                    p.dx = Cos(aa) / 2f;
                    p.dy = Sin(aa) / 2f;
                    Sfx(19);
                    if (p.dash > 0) MonsterHit(a);
                    else p.life = 0;
                }
            }
        }

        // ─── Collisions ───
        private void CollideEvent(Actor a1, Actor a2)
        {
            // Monster vs monster: turn around
            if (a1.isMonster && a1.canBump && a2.isMonster)
            {
                int d2 = (int)Sgn(a1.x - a2.x);
                if (a1.d != d2) { a1.dx = 0; a1.d = d2; }
            }

            // Bouncy mushroom
            if (a2.k == 82 && a1.dy > 0 && !a1.standing)
            {
                a1.dy = -1.1f;
                a2.activeT = 6;
                Sfx(18);
            }

            if (!a1.isPlayer) return;

            // Pickup
            if (a2.isPickup)
            {
                if (a2.k == 64) { a1.super_ = 30 * 4; a1.dx *= 2; Sfx(13); }
                if (a2.k == 80) { a1.score += 5; Sfx(9); }
                if (a2.k == 65) { _finishedT = 1; BangPuff(a2.x, a2.y - 0.5f, 108); _actor.Remove(_pl[1]); if (_pl[2] != null) _actor.Remove(_pl[2]); Music(-1, 500); Sfx(24); }
                if (a2.k == 84) { _glitchMushroom = true; Sfx(29); }
                if (a2.k == 67) { a1.score++; _gems++; }
                if (a2.k == 99)
                {
                    int bx = Flr(a2.x); float by = Flr(a2.y + 0.5f);
                    for (int xx = -1; xx <= 1; xx++)
                    {
                        if (Mget(bx + xx, (int)by) == 0)
                        {
                            var ba = MakeActor(53, bx + xx + 0.5f, by + 1);
                            ba.dx = xx / 2f;
                        }
                    }
                }
                a2.life = 0;
                var sp = MakeSparkle(85, a2.x, a2.y - 0.5f);
                sp.frames = 3; sp.maxT = 15;
                Sfx(9);
            }

            // Player vs monster
            if (a2.isMonster)
            {
                if ((a1.dash > 0 || a1.y < a2.y - a2.h / 2f) && a2.canBump)
                {
                    a1.dx *= 0.7f;
                    a1.dy *= -0.7f;
                    if (Btn(4, a1.id)) a1.dy -= 0.5f;
                    MonsterHit(a2);
                }
                else
                {
                    a1.life = 0;
                }
            }
        }

        private void Collide(Actor a1, Actor a2)
        {
            if (a1 == null || a2 == null || a1 == a2) return;
            if (Abs(a1.x - a2.x) < a1.w + a2.w && Abs(a1.y - a2.y) < a1.h + a2.h)
            {
                CollideEvent(a1, a2);
                CollideEvent(a2, a1);
            }
        }

        private void Collisions()
        {
            for (int i = 0; i < _actor.Count; i++)
                for (int j = i + 1; j < _actor.Count; j++)
                    Collide(_actor[i], _actor[j]);
        }

        // ─── Helpers ───
        private bool Alive(Actor a)
        {
            if (a == null) return false;
            if (a.life <= 0 && a.deathT > 0 && Time() > a.deathT + 0.5f) return false;
            return true;
        }

        /// <summary>Returns true if any button (0-5) is pressed on player 0. Matches PICO-8 btn()>0.</summary>
        private bool AnyBtn()
        {
            return Btn(0) || Btn(1) || Btn(2) || Btn(3) || Btn(4) || Btn(5);
        }

        /// <summary>Decomposes a sprite into spinning pixel sparkles. Matches Lua atomize_sprite().</summary>
        private void AtomizeSprite(int sprIdx, float mx, float my, int col = -1)
        {
            int sprX = (sprIdx % 16) * 8;
            int sprY = (sprIdx / 16) * 8;
            float w = 0.04f;
            for (int y = 0; y <= 7; y++)
            {
                for (int x = 0; x <= 7; x++)
                {
                    int c = Sget(sprX + x, sprY + y);
                    if (c > 0)
                    {
                        var q = MakeSparkle(0, mx + x / 8f, my + y / 8f);
                        q.dx = (x - 3.5f) / 32f + Rnd(w) - Rnd(w);
                        q.dy = (y - 7) / 32f + Rnd(w) - Rnd(w);
                        q.maxT = 20 + (int)Rnd(20);
                        q.t = (int)Rnd(10);
                        q.spin = 0.05f + Rnd(0.1f);
                        if (Rnd(2) < 1) q.spin *= -1;
                        q.ddy = 0.01f;
                        q.col = col >= 0 ? col : c;
                    }
                }
            }
        }

        private Actor ClosestP(Actor a, float maxDx)
        {
            Actor best = null; float bestD = float.MaxValue;
            for (int i = 1; i <= 2; i++)
            {
                if (_pl[i] == null || _pl[i] == a || _pl[i].life <= 0) continue;
                if (maxDx > 0 && Abs(_pl[i].x - a.x) >= maxDx) continue;
                float dx = _pl[i].x - a.x, dy = _pl[i].y - a.y;
                float d = dx * dx + dy * dy;
                if (d < bestD) { bestD = d; best = _pl[i]; }
            }
            return best;
        }

        // dpal: palette darkening table (from Lua, adjusted for 0-indexed C#)
        // Lua: dpal={0,1,1,2,1,13,6,4,4,9,3,13,1,13,14} (1-indexed)
        // C#:  index 0=black stays black, then Lua values shifted
        private static readonly int[] _dpal = { 0, 0, 1, 1, 2, 1, 13, 6, 4, 4, 9, 3, 13, 1, 13, 14 };

        /// <summary>Starts a fade-out. The actual fade runs over 41 Update/Draw frames.</summary>
        private void StartFade(int pendingLevel)
        {
            _fadeT = 0;
            _fadePendingLevel = pendingLevel;
        }

        /// <summary>Applies dpal palette darkening for current fade frame. Called from Draw().</summary>
        private void ApplyFadePalette()
        {
            if (_fadeT < 0) return;
            int i = _fadeT;
            for (int j = 1; j <= 15; j++)
            {
                int col = j;
                int steps = (i + (j % 5)) / 4;
                for (int k = 0; k < steps; k++)
                    col = _dpal[col];
                Pal(j, col, 1);
            }
        }

        private Actor ClosestA(Actor a0, List<Actor> list, bool pickupOnly)
        {
            Actor best = null; float bestD = float.MaxValue;
            foreach (var a in list)
            {
                if (pickupOnly && !a.isPickup) continue;
                if (a == a0 || a.life <= 0) continue;
                float dx = a.x - a0.x, dy = a.y - a0.y;
                float d = dx * dx + dy * dy;
                if (d < bestD) { bestD = d; best = a; }
            }
            return best;
        }

        private void BangPuff(float mx, float my, int sp)
        {
            float aa = Rnd(1);
            for (int i = 0; i <= 5; i++)
            {
                float pdx = Cos(aa + i / 6f) / 4f;
                float pdy = Sin(aa + i / 6f) / 4f;
                var s = MakeSparkle(sp, mx + pdx, my + pdy);
                s.dx = pdx; s.dy = pdy; s.maxT = 10;
            }
        }

        // ─── Camera ───
        private float PlCamX(float x, float sw) => Mid(0, x * 8 - sw / 2f, 1024 - sw);

        private void UpdateCamera()
        {
            int num = 0;
            if (Alive(_pl[1])) num++;
            if (Alive(_pl[2])) num++;

            _split = num == 2 && Abs(PlCamX(_pl[1].x, 64) - PlCamX(_pl[2].x, 64)) > 64;

            if (num == 2)
            {
                _ccyT = 0;
                for (int i = 1; i <= 2; i++)
                    _ccyT += (Flr(_pl[i].y / 2f + 0.5f) * 2 - 12) * 3;
                _ccyT /= 2f;
            }
            else
            {
                for (int i = 1; i <= 2; i++)
                {
                    if (Alive(_pl[i]) && _pl[i].standing)
                        _ccyT = (Flr(_pl[i].y / 2f + 0.5f) * 2 - 12) * 3;
                }
            }

            _ccyT = Min(0, _ccyT);
            _ccy = _ccy * 7f / 8f + _ccyT * 1f / 8f;
            _camY = _ccy;

            float xx = 0, qq = 0;
            for (int i = 1; i <= 2; i++)
            {
                if (_pl[i] != null && Alive(_pl[i]))
                {
                    float q = 1;
                    if (_pl[i].life <= 0 && _pl[i].deathT > 0)
                    {
                        q = Time() - _pl[i].deathT;
                        q = Mid(0, 1 - q * 2, 1);
                        q *= q;
                    }
                    xx += _pl[i].x * q;
                    qq += q;
                }
            }

            if (_split)
                _camX = PlCamX(_pl[1].x, 64);
            else if (qq > 0)
                _camX = PlCamX(xx / qq, 128);
        }

        // ─── Out-game logic ───
        private void OutgameLogic()
        {
            if (_deathT == 0 && !Alive(_pl[1]) && !Alive(_pl[2]))
            {
                _deathT = 1; Music(-1); Sfx(5);
            }

            if (_finishedT > 0)
            {
                _finishedT++;
                if (_finishedT > 60 && Btnp(5))
                {
                    StartFade(_level + 1);
                }
            }

            if (_deathT > 0)
            {
                _deathT++;
                if (_deathT > 45 && AnyBtn())
                {
                    Music(-1); Sfx(-1); Sfx(0);
                    StartFade(_level);
                }
            }
        }

        // ─── Update ───
        public override void Update()
        {
            if (Btnp(7)) _isHighRes = !_isHighRes;

            // During fade: just advance the fade counter
            if (_fadeT >= 0)
            {
                _fadeT++;
                if (_fadeT > 40)
                    _fadeT = -2; // transition: one black frame before level load
                return;
            }

            // Transition frame completed: now load the new level
            if (_fadeT == -2)
            {
                _fadeT = -1;
                Pal(); // Reset both draw and display palettes
                InitLevel(_fadePendingLevel);
                return;
            }

            var snapshot = new List<Actor>(_actor);
            foreach (var a in snapshot)
                if (_actor.Contains(a)) a.Move(this, a);

            var sparkSnap = new List<Sparkle>(_sparkle);
            foreach (var sp in sparkSnap)
                MoveSparkle(sp);

            Collisions();

            for (int i = 1; i <= 2; i++)
                if (_pl[i] != null && Alive(_pl[i]))
                    MoveSpawns(_pl[i].x);

            OutgameLogic();
            UpdateCamera();

            if (_glitchMushroom || CorruptMode)
                for (int i = 0; i < 4; i++)
                    Poke(Flr(Rnd(0x8000)), Flr(Rnd(0x100)));

            _levelT++;
        }

        private void MoveSparkle(Sparkle sp)
        {
            if (sp.t > sp.maxT) { _sparkle.Remove(sp); return; }
            sp.x += sp.dx; sp.y += sp.dy;
            sp.dy += sp.ddy; sp.t += 1;
        }

        // ─── Draw ───
        public override void Draw()
        {
            int W = _isHighRes ? P8.Width : 128;
            int H = _isHighRes ? P8.Height : 128;
            float scale = W / 128f;
            int s = Mathf.Max(1, (int)scale);
            _drawScale = s;

            Cls(12);

            int vw = _split ? 64 * s : W;
            Cls();

            int view0X = 0;
            if (_split && _pl[1] != null && _pl[2] != null && _pl[1].x > _pl[2].x)
                view0X = 64 * s;

            // Player 1 view (or whole screen)
            DrawWorld(view0X, 0, vw, H, _camX * s, _camY * s, s);

            // Player 2 split
            if (_split)
            {
                float cam2x = PlCamX(_pl[2].x, 64) * s;
                DrawWorld(64 * s - view0X, 0, vw, H, cam2x, _camY * s, s);
            }

            Camera(); Pal(); Clip();
            if (_split) Line(64 * s, 0, 64 * s, H, 0);

            Camera(0, 0);
            Color(7);

            if (_deathT > 45)
            {
                Print("\u00D7 restart", 44 * s, 11 * s, 14, CoordMode.Virtual, scale);
                Print("\u00D7 restart", 44 * s, 10 * s, 7, CoordMode.Virtual, scale);
            }

            if (_finishedT > 0) DrawFinished(_finishedT, s, scale);

            DrawSign(s, scale);

            // Apply fade-out palette (darkens display over 41 frames)
            // Also handles -2 transition state: full black screen
            if (_fadeT >= 0 || _fadeT == -2)
            {
                for (int j = 1; j <= 15; j++)
                {
                    if (_fadeT == -2)
                    {
                        Pal(j, 0, 1);
                    }
                    else
                    {
                        int col = j;
                        int steps = (_fadeT + (j % 5)) / 4;
                        for (int k = 0; k < steps; k++)
                            col = _dpal[col];
                        Pal(j, col, 1);
                    }
                }
            }
        }

        private static readonly string[] SignStrings = {
            "", // level 0 (unused)
            "", // level 1 (no sign)
            "this is an empty level!\n" +
            "use the map editor to add\n" +
            "some blocks and monsters.\n" +
            "in the code editor you\n" +
            "can also set level=2",
            "", // level 3
            "this is not a level!\n" +
            "\n" +
            "the bottom row of the map\n" +
            "in this cartridge is used\n" +
            "for making backgrounds."
        };

        private void DrawSign(int s, float scale)
        {
            if (_pl[1] == null) return;
            if (Mget(Flr(_pl[1].x), Flr(_pl[1].y - 0.5f)) != 25) return;
            if (_level < 0 || _level >= SignStrings.Length) return;
            string str = SignStrings[_level];
            if (string.IsNullOrEmpty(str)) return;

            Rectfill(8 * s, 6 * s, 120 * s, 46 * s, 0);
            Rect(8 * s, 6 * s, 120 * s, 46 * s, 7);
            Print(str, 12 * s, 12 * s, 6, CoordMode.Virtual, scale);
        }

        private void DrawWorld(int sx, int sy, int vw, int vh, float camX, float camY, int s)
        {
            int icx = Flr(camX);
            int icy = Flr(camY);
            if (_level >= 4) icy = 0;

            Clip(sx, sy, vw, vh);
            int adjCx = icx - sx;

            var ldat = GetThemeData(_level);

            // Sky gradient
            Camera(adjCx / 4, icy / 4);
            if (ldat.sky != null && ldat.sky.Length > 0)
            {
                for (int y = icy; y <= 127 * s; y++)
                {
                    int oy = y / s; // original 128-space y for color lookup
                    int idx = Flr(Mid(0, ldat.sky.Length - 1, (oy + (oy % 4) * 6) / 16f));
                    Line(0, y, 511 * s, y, ldat.sky[idx]);
                }
            }

            // Background elements - two passes (pass 0: far/behind map, pass 1: foreground)
            for (int pass = 0; pass <= 1; pass++)
            {
                Camera();
                if (ldat.bgels != null)
                {
                    foreach (var el in ldat.bgels)
                    {
                        // pass 0: depth > 1 (far), pass 1: depth <= 1 (near)
                        if ((pass == 0 && el.depth <= 1) || (pass == 1 && el.depth > 1)) continue;

                        Pal();
                        if (el.cols != null)
                        {
                            for (int i = 0; i < el.cols.Length - 1; i += 2)
                            {
                                if (el.cols[i + 1] == -1)
                                    Palt(el.cols[i], true);
                                else
                                    Pal(el.cols[i], el.cols[i + 1]);
                            }
                        }

                        int pixw = el.srcW * 8 * s;
                        int pixh = el.srcH * 8 * s;
                        float elsx = el.x * s;
                        if (el.scrollDx != 0) elsx += el.scrollDx * s * Time();
                        float elsy = el.y * s;

                        elsx = (elsx - adjCx) / el.depth;
                        elsy = (elsy - icy) / el.depth;

                        do
                        {
                            Map(el.srcX, el.srcY, Flr(elsx), Flr(elsy), el.srcW, el.srcH, 0, CoordMode.Virtual, s);
                            if (el.fillUp >= 0)
                                Rectfill(Flr(elsx), -1, Flr(elsx) + pixw - 1, Flr(elsy) - 1, el.fillUp);
                            if (el.fillDown >= 0)
                                Rectfill(Flr(elsx), Flr(elsy) + pixh, Flr(elsx) + pixw - 1, 128 * s, el.fillDown);
                            elsx += pixw;
                        } while (elsx < 128 * s && el.repeat);
                    }
                }
                Pal();

                if (pass == 0)
                {
                    // Draw map + actors between background and foreground passes
                    DrawZ1(adjCx, icy, s);
                }
            }

            Clip();
        }

        /// <summary>Map + actors layer (z=1).</summary>
        private void DrawZ1(int camX, int camY, int s)
        {
            Camera(camX, camY);
            Pal(12, 0); // color 12 is transparent in map
            Map(0, 0, 0, 0, 128, 64, 0, CoordMode.Virtual, s);
            Pal();

            // Sparkles
            foreach (var sp in new List<Sparkle>(_sparkle))
                DrawSparkle(sp, s);

            // Actors
            foreach (var a in new List<Actor>(_actor))
            {
                Pal();
                if (a.hitT > 0 && a.t % 4 < 2)
                    for (int i = 1; i <= 15; i++)
                        Pal(i, 8 + (a.t / 4) % 4);
                a.DrawFn(this, a);
            }

            // Foreground map layer
            Map(0, 0, 0, 0, 128, 64, 1, CoordMode.Virtual, s);
        }

        // ─── Theme Data ───
        private class BgElement
        {
            public int srcX, srcY, srcW, srcH;  // map source region
            public float x, y, depth;            // position and parallax depth
            public bool repeat;                   // tile horizontally
            public float scrollDx;               // auto-scroll speed
            public int[] cols;                    // palette swaps [from,to,from,to,...] (-1 = transparent)
            public int fillUp = -1, fillDown = -1; // fill colors above/below
        }

        private class ThemeData
        {
            public int[] sky;           // sky gradient colors (sampled by y)
            public BgElement[] bgels;   // background elements
        }

        private ThemeData GetThemeData(int level)
        {
            switch (level)
            {
                case 1: return _theme1;
                case 2: return _theme2;
                case 3: return _theme3;
                default: return _themeEmpty;
            }
        }

        private static readonly ThemeData _themeEmpty = new ThemeData();

        // Level 1: Forest
        private static readonly ThemeData _theme1 = new ThemeData
        {
            sky = new[] { 12, 12, 12, 12, 12 },
            bgels = new[]
            {
                // clouds
                new BgElement { srcX=16,srcY=56,srcW=16,srcH=8, x=0,y=28*4,depth=4,repeat=true,
                    scrollDx=-8, cols=new[]{15,7,1,-1}, fillDown=12 },
                // mountains
                new BgElement { srcX=0,srcY=56,srcW=16,srcH=8, x=0,y=28*4,depth=4,repeat=true,
                    fillDown=13 },
                // leaves: light
                new BgElement { srcX=32,srcY=48,srcW=16,srcH=6, x=118*8,y=-8,depth=1.5f,
                    cols=new[]{1,3}, fillUp=1 },
                // leaves: dark (foreground)
                new BgElement { srcX=32,srcY=48,srcW=16,srcH=6, x=118*8,y=-12,depth=0.8f,
                    cols=new[]{3,1}, fillUp=1 }
            }
        };

        // Level 2: Gardens
        private static readonly ThemeData _theme2 = new ThemeData
        {
            sky = new[] { 12 },
            bgels = new[]
            {
                // gardens
                new BgElement { srcX=32,srcY=56,srcW=16,srcH=8, x=0,y=100,depth=4,repeat=true,
                    cols=new[]{3,13,7,13,10,13,1,13,11,13,9,13,14,13,15,13,2,13}, fillDown=13 },
                // foreground shrubbery
                new BgElement { srcX=16,srcY=56,srcW=16,srcH=8, x=0,y=(int)(64*0.8f),depth=0.6f,repeat=true,
                    cols=new[]{15,1,7,1}, fillDown=12 },
                // foliage feature 1
                new BgElement { srcX=32,srcY=56,srcW=8,srcH=8, x=60,y=(int)(60*0.9f),depth=0.8f,
                    cols=new[]{15,1,7,1,3,1,11,1,10,1,9,1} },
                // foliage feature 2
                new BgElement { srcX=32,srcY=56,srcW=8,srcH=8, x=260,y=(int)(60*0.9f),depth=0.8f,
                    cols=new[]{15,1,7,1,3,1,11,1,10,1,9,1} },
                // leaves: indigo
                new BgElement { srcX=32,srcY=48,srcW=16,srcH=6, x=40,y=64,depth=4,repeat=true,
                    cols=new[]{1,13,3,13}, fillUp=13 },
                // leaves: light
                new BgElement { srcX=32,srcY=48,srcW=16,srcH=6, x=0,y=-4,depth=1.5f,repeat=true,
                    cols=new[]{1,3}, fillUp=1 },
                // leaves: dark (foreground)
                new BgElement { srcX=32,srcY=48,srcW=16,srcH=6, x=-40,y=-6,depth=0.8f,repeat=true,
                    cols=new[]{3,1}, fillUp=1 }
            }
        };

        // Level 3: Mountains
        private static readonly ThemeData _theme3 = new ThemeData
        {
            sky = new[] { 12, 14, 14, 14, 14 },
            bgels = new[]
            {
                // far mountains (indigo)
                new BgElement { srcX=0,srcY=56,srcW=16,srcH=8, x=-64,y=30,depth=8,repeat=true,
                    fillDown=13, cols=new[]{6,15,13,6} },
                // clouds between mountain layers
                new BgElement { srcX=16,srcY=56,srcW=16,srcH=8, x=0,y=50,depth=8,repeat=true,
                    scrollDx=-30, cols=new[]{15,7,1,-1}, fillDown=7 },
                // close mountains
                new BgElement { srcX=0,srcY=56,srcW=16,srcH=8, x=0,y=140,depth=8,repeat=true,
                    fillDown=13, cols=new[]{6,5,13,1} }
            }
        };

        // ─── Draw functions ───
        private void DrawActor(JelpiCartridge cart, Actor a)
        {
            int s = _drawScale;
            int fr = a.k + Flr(a.frame);
            if (a.dash > 0)
                for (int i = 2; i <= 15; i++) Pal(i, 7 + ((a.t / 2) % 8));

            int sx = Flr(a.x * 8) * s - 4 * s;
            int sy = Flr(a.y * 8) * s - 8 * s;
            if (Fget(fr, 3) != 0) sy -= 1 * s;

            int sprSx = (fr % 16) * 8;
            int sprSy = (fr / 16) * 8;
            Sspr(sprSx, sprSy, 8, 8, sx, sy, 8 * s, 8 * s, a.d < 0);

            // Sprite flag 2: repeat top pixel row (mimo's ears)
            if (Fget(fr, 2) != 0)
            {
                Pal(14, 7);
                int earSx = (fr % 16) * 8;
                int earSy = (fr / 16) * 8;
                if (a.d < 0) earSx += 7;
                Sspr(earSx, earSy, 8, 1, sx, sy - 1 * s, 8 * s, 1 * s, a.d < 0);
            }
            Pal();
        }

        private void DrawSparkle(Sparkle sp, int s)
        {
            // Spinning line sparkle (k==0)
            if (sp.k == 0)
            {
                float spx = sp.x * 8 * s, spy = sp.y * 8 * s;
                float rad = 1.4f * s;
                Line(Flr(spx), Flr(spy),
                     Flr(spx + Cos(sp.t * sp.spin) * rad),
                     Flr(spy + Sin(sp.t * sp.spin) * rad),
                     sp.col);
                return;
            }

            if (sp.col > 0)
                for (int i = 1; i <= 15; i++) Pal(i, sp.col);

            float fr = sp.frames * sp.t / (float)sp.maxT;
            fr = sp.k + Mid(0, fr, sp.frames - 1);
            int sprSrcX = (Flr(fr) % 16) * 8;
            int sprSrcY = (Flr(fr) / 16) * 8;
            Sspr(sprSrcX, sprSrcY, 8, 8, Flr(sp.x * 8) * s - 4 * s, Flr(sp.y * 8) * s - 4 * s, 8 * s, 8 * s);
            Pal();
        }

        private void DummyDraw(JelpiCartridge cart, Actor a) { }

        private void DrawTail(JelpiCartridge cart, Actor a)
        {
            int s = _drawScale;
            DrawActor(cart, a);
            int sx = Flr(a.x * 8) * s, sy = Flr(a.y * 8) * s - 2 * s;
            int d = -a.d;
            sx += d * 3 * s;
            if (a.d > 0) sx -= 1 * s;
            for (int i = 0; i <= 4; i += 2)
                Pset(sx + i * d * s, sy + Flr(Cos(i / 16f - Time()) * (1 + i) * Abs(a.dx) * 4) * s, 7);
        }

        private void DrawChargePowerup(JelpiCartridge cart, Actor a)
        {
            int s = _drawScale;
            DrawActor(cart, a);
            int sx = Flr(a.x * 8) * s, sy = Flr(a.y * 8) * s - 4 * s;
            for (int i = 0; i <= 5; i++)
                Circfill(sx + Flr(Cos(i / 6f + Time() / 2f) * 5.5f) * s,
                         sy + Flr(Sin(i / 6f + Time() / 2f) * 5.5f) * s,
                         Flr((i + Time() * 3) % 1.5f) * s, 7);
        }

        private void DrawExit(JelpiCartridge cart, Actor a)
        {
            int s = _drawScale;
            int sx = Flr(a.x * 8) * s, sy = Flr(a.y * 8) * s - 4 * s;
            sy += Flr(Cos(Time() / 2f) * 1.5f) * s;
            Circfill(sx - 1 * s + Flr(Cos(Time() * 1.5f)) * s, sy, 4 * s, 8);
            Circfill(sx + 1 * s + Flr(Cos(Time() * 1.3f)) * s, sy, 4 * s, 12);
            Circfill(sx, sy, 3 * s, 7);
            for (int i = 0; i <= 3; i++)
            {
                Circfill(sx + Flr(Cos(i / 8f + Time() * 0.6f) * 6) * s, sy + Flr(Sin(i / 5f + Time() * 0.4f) * 6) * s, Flr(1.5f + Cos(i / 7f + Time())) * s, 8 + i % 5);
                Circfill(sx + Flr(Cos(0.5f + i / 7f + Time() * 0.9f) * 5) * s, sy + Flr(Sin(0.5f + i / 9f + Time() * 0.7f) * 5) * s, Flr(0.5f + Cos(0.5f + i / 7f + Time())) * s, 14 + i % 2);
            }
        }

        private void DrawGlitchMushroom(JelpiCartridge cart, Actor a)
        {
            DrawActor(cart, a);
            // Simplified: skip per-pixel glitch effect for now
        }

        private void DrawSwirly(JelpiCartridge cart, Actor a)
        {
            int s = _drawScale;
            // Simplified: draw head sprites only
            int sx = Flr(a.x * 8) * s, sy = Flr(a.y * 8) * s - 4 * s;
            int sprSx81 = (81 % 16) * 8, sprSy81 = (81 / 16) * 8;
            Sspr(sprSx81, sprSy81, 8, 8, sx - 4 * s, sy + 5 * s + Flr(Cos(a.t / 30f)) * s, 8 * s, 8 * s);
            int sprSx80 = (80 % 16) * 8, sprSy80 = (80 / 16) * 8;
            Sspr(sprSx80, sprSy80, 8, 8, sx - 8 * s, sy, 8 * s, 8 * s);
            Sspr(sprSx80, sprSy80, 8, 8, sx, sy, 8 * s, 8 * s, true);
        }

        private void DrawFrog(JelpiCartridge cart, Actor a)
        {
            int s = _drawScale;
            DrawActor(cart, a);
            if (a.tongueT <= 0) return;
            int sx = Flr(a.x * 8) * s + a.d * 4 * s;
            int sy = Flr(a.y * 8) * s - 3 * s;
            int sx2 = Flr(a.tongueX * 8) * s;
            int sy2 = Flr((a.y + 0.25f) * 8) * s;
            Line(sx, sy, sx2, sy, 8);
            Rectfill(sx2, sy, sx2 + a.d * s, sy - 1 * s, 14);
        }

        private void DrawBird(JelpiCartridge cart, Actor a)
        {
            int q = Flr(a.t / 8f);
            if ((q * q) % 11 < 1) Pal(1, 15);
            DrawActor(cart, a);
        }

        private void DrawFinished(int tt, int s, float scale)
        {
            if (tt < 15) return;
            tt -= 15;

            // "★ stage clear ★" header
            string str = "\u00D2 stage clear \u00D2  ";
            int strX = Flr((64 - str.Length * 2) * scale);
            Print(str, strX, Flr(31 * scale), 14, CoordMode.Virtual, scale);
            Print(str, strX, Flr(30 * scale), 7, CoordMode.Virtual, scale);

            // Gem counter: two passes (0 = gray silhouettes, 1 = colored + bounce)
            int n = _totalGems;
            for (int i = 1; i <= 15; i++) Pal(i, 13); // gray for pass 0
            for (int pass = 0; pass <= 1; pass++)
            {
                for (int i = 0; i < n; i++)
                {
                    int t2 = tt - (i * 4 + 15);
                    bool q = i < _gems && t2 >= 0;
                    if (pass == 0 || q)
                    {
                        int gy = Flr((50 - pass) * scale);
                        if (q)
                        {
                            float bounce = t2 > 0 ? Sin(t2 / 8f) * 4f / (t2 / 2f) : 0;
                            gy += Flr(bounce * scale);
                            if (!_gemSfx.ContainsKey(i) || !_gemSfx[i])
                            {
                                Sfx(25);
                                _gemSfx[i] = true;
                            }
                        }
                        int sprGx = (67 % 16) * 8;
                        int sprGy = (67 / 16) * 8;
                        Sspr(sprGx, sprGy, 8, 8, Flr((64 - n * 4 + i * 8) * scale), gy, (int)(8 * scale), (int)(8 * scale));
                    }
                }
                Pal(); // reset palette after pass 0
            }

            if (tt > 45)
            {
                Print("\u00D7 continue", Flr(42 * scale), Flr(91 * scale), 12, CoordMode.Virtual, scale);
                Print("\u00D7 continue", Flr(42 * scale), Flr(90 * scale), 7, CoordMode.Virtual, scale);
            } 
        }
    }
}

```

## Cartridge Assets
The following non-code data blocks were detected in the original `.p8` cartridge:
- `__gfx__` — spritesheet (128x128 PNG, contains player/enemy/tile/background sprites)
- `__label__` — cartridge thumbnail (128x128 PNG)
- `__gff__` — sprite flags (collision, pickup, monster, platform, smashable markers)
- `__map__` — tile map data (top 32 rows; bottom 32 rows shared with `__gfx__` lower half)
- `__sfx__` — sound effects (jumps, dashes, hits, pickups, death, gems, etc.)
- `__music__` — music patterns (3 level tracks + additional patterns)

## Original Lua Source

### Raw Script Block
Directly extracted from `docs/references/pico-8/games/jelpi/jelpi.p8`:
```lua
-- File: jelpi.p8
-- jelpi demo
-- by zep  
-- license: cc4-by-nc-sa // https://creativecommons.org/licenses/by-nc-sa/4.0/

level=1

num_players = 1
corrupt_mode = false
paint_mode = false
max_actors = 64
play_music = true

function make_actor(k,x,y,d)
	local a = {
		k=k,
		frame=0,
		frames=4,
		life = 1,
		hit_t=0,
		x=x,y=y,dx=0,dy=0,
		homex=x,homey=y,
		ddx = 0.02, -- acceleration
		ddy = 0.06, -- gravity
		w=3/8,h=0.5, -- half-width
		d=d or -1, -- direction
		bounce=0.8,
		friction=0.9,
		can_bump=true,
		dash=0,
		super=0,
		t=0,
		standing = false,
		draw=draw_actor,
		move=move_actor,
	}
	
	-- attributes by flag
	
	if (fget(k,6)) then
		a.is_pickup=true
	end
	
	if (fget(k,7)) then
		a.is_monster=true
		a.move=move_monster
	end
	
	if (fget(k,4)) then
		a.ddy = 0 -- zero gravity
	end
	
	-- attributes from actor_dat
	
	for k,v in pairs(actor_dat[k])
	do
		a[k]=v
	end
	
	if (#actor < max_actors) then
		add(actor, a)
	end
	
	return a
end

function make_sparkle(k,x,y,col)
	local s = {
		x=x,y=y,k=k,
		frames=1,
		col=col,
		t=0, max_t = 8+rnd(4),
		dx = 0, dy = 0,
		ddy = 0
	}
	if (#sparkle < 512) then
		add(sparkle,s)
	end
	return s
end

function make_player(k, x, y, d)

	local a = make_actor(k, x, y, d)
	
	a.is_player=true
	a.move=move_player

	a.score   = 0
	a.bounce  = 0
	a.delay   = 0
	a.id      = 0 -- player 1

	
	return a
end




-- called at start by pico-8
function _init()

	init_actor_data() 
	init_level(level)
	
	menuitem(1,
	"restart level",
	function()
		init_level(level)
	end)
		
end

-- clear_cel using neighbour val
-- prefer empty, then non-ground
-- then left neighbour

function clear_cel(x, y)
	local val0 = mget(x-1,y)
	local val1 = mget(x+1,y)
	if ((x>0 and val0 == 0) or 
					(x<127 and val1 == 0)) then
		mset(x,y,0)
	elseif (not fget(val1,1)) then
		mset(x,y,val1)
	elseif (not fget(val0,1)) then
		mset(x,y,val0)
	else
		mset(x,y,0)
	end
end


function move_spawns(x0, y0)

	x0=flr(x0)
	y0=flr(y0)
	
	-- spawn actors close to x0,y0

	for y=0,16 do
		for x=x0-10,max(16,x0+14) do
			local val = mget(x,y)
			
			-- actor
			if (fget(val, 5)) then    
				m = make_actor(val,x+0.5,y+1)
				clear_cel(x,y)
			end
			
		end
	end

end

-- test if a point is solid
function solid (x, y, ignore)

	if (x < 0 or x >= 128 ) then
		return true end
	
	local val = mget(x, y)
	
	-- flag 6: can jump up through
	-- (and only top half counts)	
	if (fget(val,6)) then
		if (ignore) return false
		-- bottom half: solid iff solid below
		if (y%1 > 0.5) return solid(x,y+1)
	end
	
	return fget(val, 1)
end

-- solidx: solid at 2 points
-- along x axis
local function solidx(x,y,w)
	return solid(x-w,y) or
		solid(x+w,y)
end


function move_player(pl)

	move_actor(pl)
	
	if (pl.y > 18) pl.life=0

	local b = pl.id

	if (pl.life <= 0) then
				
				for i=1,32 do
					s=make_sparkle(69,
						pl.x, pl.y-0.6)
					s.dx = cos(i/32)/2
					s.dy = sin(i/32)/2
					s.max_t = 30 
					s.ddy = 0.01
					s.frame=69+rnd(3)
					s.col = 7
				end
				
				sfx(17)
				pl.death_t=time()
				
				
		return
	end
	
	local accel = 0.05
	local q=0.7
	
	if (pl.dash > 10) then
		accel = 0.08
	end
	
	if (pl.super > 0) then 
		q*=1.5
		accel*=1.5
	end
	
	if (not pl.standing) then
		accel = accel / 2
	end
		
	-- player control
	if (btn(0,b)) then 
			pl.dx = pl.dx - accel; pl.d=-1 end
	if (btn(1,b)) then 
		pl.dx = pl.dx + accel; pl.d=1 end

	if ((btn(4,b)) and 
		pl.standing) then
		pl.dy = -0.7
		sfx(8)
	end

	-- charge

	if (btn(5,b) and pl.delay == 0)
	then
		pl.dash = 15
		pl.delay= 20
		-- charge in dir of buttons
		dx=0 dy=0
		if (btn(0,b)) dx-=1*q
		if (btn(1,b)) dx+=1*q
		
		-- keep controls to 4 btns
		if (btn(2,b)) dy-=1*q
		if (btn(3,b)) dy+=1*q
		
		if (dx==0 and dy==0) then
			pl.dx += pl.d * 0.4
		else
			local aa=atan2(dx,dy)
			pl.dx += cos(aa)/2
			pl.dy += sin(aa)/3
			
			pl.dy=max(-0.5,pl.dy)
		end
		
		-- tiny extra vertical boost
		if (not pl.standing) then
			pl.dy = pl.dy - 0.2
		end 
	
		sfx(11)
	
	end
	
	-- super: give more dash
	
	if (pl.super > 0) pl.dash=2
	
	-- dashing
	
	if pl.dash > 0 then
		
		if (abs(pl.dx) > 0.4 or
						abs(pl.dy) > 0.2
		) then
		
		for i=1,3 do
			local s = make_sparkle(
				69+rnd(3),
				pl.x+pl.dx*i/3, 
				pl.y+pl.dy*i/3 - 0.3,
				(pl.t*3+i)%9+7)
			if (rnd(2) < 1) then
				s.col = 7
			end
			s.dx = -pl.dx*0.1
			s.dy = -0.05*i/4
			s.x = s.x + rnd(0.6)-0.3
			s.y = s.y + rnd(0.6)-0.3
		end
		end
	end 
	
	pl.dash = max(0,pl.dash-1)
	pl.delay = max(0,pl.delay-1)
	pl.super = max(0, pl.super-1)
	
	-- frame	

	if (pl.standing) then
		pl.frame = (pl.frame+abs(pl.dx)*2) % pl.frames
	else
		pl.frame = (pl.frame+abs(pl.dx)/2) % pl.frames
	end
	
	if (abs(pl.dx) < 0.1) pl.frame = 0
	
end

function move_monster(m)
	
	move_actor(m)
	
	if (m.life<=0) then
		bang_puff(m.x,m.y-0.5,104)

		sfx(14)
		return
	end
	

	m.dx = m.dx + m.d * m.ddx

	m.frame = (m.frame+abs(m.dx)*3+4) % m.frames
	
	-- jump
	if (false and m.standing and rnd(10) < 1)
	then
		m.dy = -0.5
	end
	
	-- hit cooldown
	-- (can't get hit twice within
	--  half a second)
	if (m.hit_t>0) m.hit_t-=1

end


function smash(x,y,b)

		local val = mget(x, y, 0)
		if (not fget(val,4)) then
			-- not smashable
			-- -> pass on to solid()
			return solid(x,y,b)
		end    
		
		
		-- spawn
		if (val == 48) then
			local a=make_actor(
				loot[#loot],
				x+0.5,y-0.2)
			
			a.dy=-0.8
			a.d=flr(rnd(2))*2-1
			a.d=0.25 -- swirly
			loot[#loot]=nil
		end
		
				
		clear_cel(x,y)
		sfx(10)
			
		-- make debris
		
		for by=0,1 do
			for bx=0,1 do
				s=make_sparkle(22,
				0.25+flr(x) + bx*0.5, 
				0.25+flr(y) + by*0.5,
				0)
				s.dx = (bx-0.5)/4
				s.dy = (by-0.5)/4
				s.max_t = 30 
				s.ddy = 0.02
			end
		end

		return false -- not solid
end

function move_actor(a)

	if (a.life<=0) del(actor,a)
	
	a.standing=false
	
	-- when dashing, call smash()
	-- for any touched blocks
	-- (except for landing blocks)
	local ssolid=
		a.dash>0 and smash or solid 
	
	-- solid going down -- only
	-- smash when holding down
	local ssolidd=
		a.dash>0 and (btn(3,a.id))
		 and smash or solid 
		
	--ignore jump-up-through
	--blocks only when have gravity
	local ign=a.ddy > 0
	
	-- x movement 
	
	-- candidate position
	x1 = a.x + a.dx + sgn(a.dx)/4
	
	if not ssolid(x1,a.y-0.5,ign)
	then
		-- nothing in the way->move
		a.x += a.dx 
		
	else -- hit wall
	
		-- bounce
		if (a.dash > 0)sfx(12) 
		a.dx *= -1
		
		a.hit_wall=true
		
		-- monsters turn around
		if (a.is_monster) then
			a.d *= -1
			a.dx = 0
		end
		
	end
	
	-- y movement
	
	local fw=0.25

	if (a.dy < 0) then
		-- going up
		
		if (
		 ssolid(a.x-fw, a.y+a.dy-1,ign) or
		 ssolid(a.x+fw, a.y+a.dy-1,ign))
		then
			a.dy=0
			
			-- snap to roof
			a.y=flr(a.y+.5)
			
		else
			a.y += a.dy
		end

	else
		-- going down
	
		local y1=a.y+a.dy
		if ssolidd(a.x-fw,y1) or
		   ssolidd(a.x+fw,y1)
		then
		
			-- bounce
			if (a.bounce > 0 and 
			    a.dy > 0.2) 
			then
				a.dy = a.dy * -a.bounce
			else
			
			a.standing=true
			a.dy=0
			end
			
			-- snap to top of ground
			a.y=flr(a.y+0.75)	
			
		else
			a.y += a.dy  
		end
		-- pop up
		
		while solid(a.x,a.y-0.05) do
			a.y -= 0.125
		end

	end


	-- gravity and friction
	a.dy += a.ddy
	a.dy *= 0.95

	-- x friction

	a.dx *= a.friction
	if (a.standing) then
		a.dx *= a.friction
	end

--end
	
	-- counters
	a.t = a.t + 1
end


function monster_hit(m)
	if(m.hit_t>0) return
	
	m.life-=1
	m.hit_t=15
	m.dx/=4
	m.dy/=4
	-- survived: thunk sound
	if (m.life>0) sfx(21)
	
end

function player_hit(p)
		if (p.dash>0) return
		p.life-=1
end

function collide_event(a1, a2)

	if (a1.is_monster and
					a1.can_bump and
					a2.is_monster) then
					local d=sgn(a1.x-a2.x)
					if (a1.d!=d) then
						a1.dx=0
						a1.d=d
					end
	end
	
	-- bouncy mushroom
	if (a2.k==82) then
		if (a1.dy > 0 and 
		not a1.standing) then
			a1.dy=-1.1
			a2.active_t=6
			sfx(18)
		end
	end

	if(a1.is_player) then
		if(a2.is_pickup) then

			if (a2.k==64) then
				a1.super = 30*4
				--sfx(17)
				a1.dx = a1.dx * 2
				--a1.dy = a1.dy-0.1
				-- a1.standing = false
				sfx(13)
			end

			-- watermelon
			if (a2.k==80) then
				a1.score+=5
				sfx(9)
			end
			
			-- end level
			if (a2.k==65) then
				finished_t=1
				bang_puff(a2.x,a2.y-0.5,108)
				del(actor,pl[1])
				del(actor,pl[2])
				music(-1,500)
				sfx(24)
			end
			
			-- glitch mushroom
			if (a2.k==84) then
				glitch_mushroom = true
				sfx(29)
			end
			
			-- gem
			if (a2.k==67) then
				a1.score = a1.score + 1
				
				-- total gems between players
				gems+=1
				
			end
			
			-- bridge builder
			if (a2.k==99) then
				local x,y=flr(a2.x)+.5,flr(a2.y+0.5)
				for xx=-1,1 do
				if (mget(x+xx,y)==0) then
					local a=make_actor(53,x+xx,y+1)
					a.dx=xx/2
				end
				end
			end
			
			a2.life=0
			
			s=make_sparkle(85,a2.x,a2.y-.5)
			s.frames=3
			s.max_t=15
			sfx(9)
		end
		
		-- charge or dupe monster
		
		if(a2.is_monster) then -- monster
			
			if(
					(a1.dash > 0 or 
						a1.y < a2.y-a2.h/2)
					and a2.can_bump
				) then
				
				-- slow down player
				a1.dx *= 0.7
				a1.dy *= -0.7
				
				if (btn(🅾️,a1.id))a1.dy -= .5
				
				monster_hit(a2)
				
			else
				-- player death
				a1.life=0
				
			end
		end
			
	end
end

function move_sparkle(sp)
	if (sp.t > sp.max_t) then
		del(sparkle,sp)
	end
	
	sp.x = sp.x + sp.dx
	sp.y = sp.y + sp.dy
	sp.dy= sp.dy+ sp.ddy
	sp.t = sp.t + 1
end


function collide(a1, a2)
	if (not a1) return
	if (not a2) return
	
	if (a1==a2) then return end
	local dx = a1.x - a2.x
	local dy = a1.y - a2.y
	if (abs(dx) < a1.w+a2.w) then
		if (abs(dy) < a1.h+a2.h) then
			collide_event(a1, a2)
			collide_event(a2, a1)
		end
	end
end

function collisions()

	-- to do: optimize if too
	-- many actors

	for i=1,#actor do
		for j=i+1,#actor do
			collide(actor[i],actor[j])
		end
	end
	
end



function outgame_logic()

	if death_t==0 and
			not alive(pl[1]) and 
			not alive(pl[2]) then
			death_t=1
			music(-1)
			sfx(5)
			
	end

	if (finished_t > 0) then
	
		finished_t += 1
		
		if (finished_t > 60) then
			if (btnp(❎)) then
				fade_out()
				init_level(level+1)
			end
		end
	
	end

	if (death_t > 0) then
		death_t = death_t + 1
		if (death_t > 45 and 
			btn()>0)
		then 
				music(-1)
				sfx(-1)
				sfx(0)
				fade_out()
				
				
				-- restart cart end of slice
				init_level(level)
			end
	end
	
end

function _update() 
	
	for a in all(actor) do
		a:move()
	end
		
	foreach(sparkle, move_sparkle)
	collisions()
	
	for i=1,#pl do
		move_spawns(pl[i].x,0)
	end
	
	outgame_logic()
	update_camera()

	if (glitch_mushroom or corrupt_mode) then
		for i=1,4 do
			poke(rnd(0x8000),rnd(0x100))
		end
	end
	
	level_t += 1
end



function _draw()

	cls(12)
	
	-- view width
	local vw=split and 64 or 128

	cls()
	
	-- decide which side to draw
	-- player 1 view
	local view0_x = 0
	if (split and pl[1].x>pl[2].x)
	then view0_x = 64 end
	
	-- player 1 (or whole screen)
	draw_world(
		view0_x,0,vw,128,
		cam_x,cam_y)
	
	-- player 2 view if needed
	if (split) then
		cam_x = pl_camx(pl[2].x,64)
		draw_world(64-view0_x,0,vw,128,
			cam_x, cam_y)
	end
	
	camera()pal()clip()
	if (split) line(64,0,64,128,0)

	-- player score
	camera(0,0)
	color(7)
	
	if (death_t > 45) then
		print("❎ restart",
			44,10+1,14)
		print("❎ restart",
			44,10,7)
	end
	
	if (finished_t > 0) then
		draw_finished(finished_t)
	end
	
	if (paint_mode) apply_paint()

	draw_sign()
end


sign_str={
"",
[[
	this is an empty level!
	use the map editor to add
	some blocks and monsters.
	in the code editor you
	can also set level=2
	]],
"",
[[
	this is not a level!
	
	the bottom row of the map 
	in this cartridge is used
	for making backgrounds.
]]
}

function draw_sign()

if (mget(pl[1].x,pl[1].y-0.5)!=25) return

rectfill(8,6,120,46,0)
rect(8,6,120,46,7)

print(sign_str[level],12,12,6)


end


function fade_out()

	dpal={0,1,1, 2,1,13,6,
							4,4,9,3, 13,1,13,14}
	
	
					
	-- palette fade
	for i=0,40 do
		for j=1,15 do
			col = j
			for k=1,((i+(j%5))/4) do
				col=dpal[col]
			end
			pal(j,col,1)
		end
		flip()
	end
	
end
-->8
-- draw world

function draw_sparkle(s)

	--spinning
	if (s.k == 0) then
		local sx=s.x*8
		local sy=s.y*8
		
		line(sx,sy,
				sx+cos(s.t*s.spin)*1.4,
				sy+sin(s.t*s.spin)*1.4,
				s.col)
				
		return
	end
	
	if (s.col and s.col > 0) then
		for i=1,15 do
			pal(i,s.col)
		end
	end

	local fr=s.frames * s.t/s.max_t
	fr=s.k+mid(0,fr,s.frames-1)
	spr(fr, s.x*8-4, s.y*8-4)

	pal()
end

function draw_actor(a)

	local fr=a.k + a.frame

	-- rainbow colour when dashing
	if (a.dash>0) for i=2,15 do pal(i,7+((a.t/2) % 8)) end
	
	local sx=a.x*8-4
	local sy=a.y*8-8
	
	-- sprite flag 3 (green):
	-- draw one pixel up
	if (fget(fr,3)) sy-=1

	-- draw the sprite
	spr(fr, sx,sy,1,1,a.d<0)

	-- sprite flag 2 (yellow):
	-- repeat top line
	-- (for mimo's ears!)
	
	if (fget(fr,2)) then
		pal(14,7)
		spr(fr,sx,sy-1,1,1/8,
						a.d<0)
	end
	
	pal()
end

function draw_tail(a)

	draw_actor(a)
	
	local sx=a.x*8
	local sy=a.y*8-2
	local d=-a.d
	sx += d*3
	if (a.d>0) sx-=1
	
	for i=0,4,2 do
		pset(sx+i*d*1,
		  sy + cos(i/16-time())*
		  (1+i)*abs(a.dx)*4,7)
	end
	
end


function apply_paint()
	if (tt==nil) tt=0
	tt=tt+0.25
	srand(flr(tt))
	local nn=rnd(128)
	local xx=0
	local yy=nn&127
	for i=1,1000*13,13 do
		nn+=i
		nn*=33
		xx=nn&127
		local col=pget(xx,yy)
		rectfill(xx,yy,xx+1,yy+1,col)
		line(xx-1,yy-1,xx+2,yy+2,col)
		nn+=i
		nn*=57
		yy=nn&127
		rectfill(xx-1,yy-1,xx,yy,pget(xx,yy))
			
	end
end

-- draw the world at sx,sy
-- with a view size: vw,vh
function draw_world(
		sx,sy,vw,vh,cam_x,cam_y)

	-- reduce jitter
	cam_x=flr(cam_x) 
	cam_y=flr(cam_y)
	
	if (level>=4) cam_y = 0
	
	clip(sx,sy,vw,vh)
	cam_x -= sx
	
	local ldat=theme_dat[level]
	if (not ldat) ldat={}
	
	-- sky
	camera (cam_x/4, cam_y/4)
	
	-- sample palette colour
	local colx=120+level
	
	-- sky gradient
	if (ldat.sky) then
		for y=cam_y,127 do
			col=ldat.sky[
				flr(mid(1,#ldat.sky,
					(y+(y%4)*6) / 16))]
				
			line(0,y,511,y,col)
		end
	end
	
	-- elements
	
	
	for pass=0,1 do
	camera()
	
	for el in all(ldat.bgels) do
	
	if (pass==0 and el.xyz[3]>1) or
	   (pass==1 and el.xyz[3]<=1)
	then
	
		pal()
		if (el.cols) then
		for i=1,#el.cols, 2 do
			if (el.cols[i+1]==-1) then
				palt(el.cols[i],true)
			else
				pal(el.cols[i],el.cols[i+1])
			end
		end
		end
		
		local s=el.src
		local pixw=s[3] * 8
		local pixh=s[4] * 8
		local sx=el.xyz[1]
		if (el.dx) then
			sx += el.dx*t()
		end
		local sy=el.xyz[2]
		
		sx = (sx-cam_x)/el.xyz[3]
		sy = (sy-cam_y)/el.xyz[3]
		
		repeat
			map(s[1],s[2],sx,sy,s[3],s[4])
			if (el.fill_up) then
				rectfill(sx,-1,sx+pixw-1,sy-1,el.fill_up)
			end
			if (el.fill_down) then
				rectfill(sx,sy+pixh,sx+pixw-1,128,el.fill_down)
			end
			sx+=pixw
		
		until sx >= 128 or not el.xyz[4] 
	
	end
	end
	pal()
	
		if (pass==0) then
			draw_z1(cam_x,cam_y)
		end
	end
	

	
	clip()
	
end
	

-- map and actors
function draw_z1(cam_x,cam_y)
	
	camera (cam_x,cam_y)
	pal(12,0)	-- 12 is transp
	map (0,0,0,0,128,64,0)
	pal()
	foreach(sparkle, draw_sparkle)
	for a in all(actor) do
		pal()
		if (a.hit_t>0 and a.t%4 < 2) then
			for i=1,15 do
				pal(i,8+(a.t/4)%4)
			end
		end
		a:draw() -- same as a.draw(a)
	end
	-- forground map
	map (0,0,0,0,128,64,1)
end


-->8
-- explosions

function bang_puff(mx,my,sp)

	local aa=rnd(1)
	for i=0,5 do
	
		local dx=cos(aa+i/6)/4
		local dy=sin(aa+i/6)/4
		local s=make_sparkle(
			sp,mx + dx, my + dy) 
		s.dx = dx
		s.dy = dy
		s.max_t=10
	end
	
end

function atomize_sprite(s,mx,my,col)

	local sx=(s%16)*8
	local sy=flr(s/16)*8
	local w=0.04
	
	for y=0,7 do
		for x=0,7 do
			if (sget(sx+x,sy+y)>0) then
				local q=make_sparkle(0,
					mx+x/8,
					my+y/8)
				q.dx=(x-3.5)/32 +rnd(w)-rnd(w)
				q.dy=(y-7)/32   +rnd(w)-rnd(w)
				q.max_t=20+rnd(20)
				q.t=rnd(10)
				q.spin=0.05+rnd(0.1)
				if (rnd(2)<1) q.spin*=-1
				q.ddy=0.01
				q.col=col or sget(sx+x,sy+y)
			end
		end
	end

end
-->8
-- camera

-- (camera y is lazy)
ccy_t=0
ccy  =0

-- splitscreen (multiplayer)
split=false

-- camera x for player i
function pl_camx(x,sw)
	return mid(0,x*8-sw/2,1024-sw)
end


function update_camera()

	local num=0
	if (alive(pl[1])) num+=1
	if (alive(pl[2])) num+=1
	
	split = num==2 and
		abs(pl_camx(pl[1].x,64) -
		    pl_camx(pl[2].x,64)) > 64
	
	-- camera y target changes
	-- when standing. quantize y
	-- into 2 blocks high so don't
	-- get small adjustments
	-- (should be in _update)
	
	if (num==2) then
		-- 2 active players: average y
		ccy_t=0
		for i=1,2 do
			ccy_t += (flr(pl[i].y/2+.5)*2-12)*3
		end
		ccy_t/=2
	else
	
		-- single: set target only
		-- when standing
		for i=1,#pl do
			if (alive(pl[i]) and
			    pl[i].standing) then
			    ccy_t=(
			     flr(pl[i].y/2+.5)*2-12
			    )*3
			end
		end
	end
	
	-- target always <= 0
	ccy_t=min(0,ccy_t)
	
	ccy = ccy*7/8+ccy_t*1/8
	cam_y = ccy
	
	local xx=0
	local qq=0
	for i=1,#pl do
			if (alive(pl[i])) then
				local q=1
				
				-- pan across when first
				-- player dies and not in
				-- split screen
				if (pl[i].life<=0 and pl[i].death_t) then
					q=time()-pl[i].death_t
					q=mid(0,1-q*2,1)
					q*=q
				end
				
				xx+=pl[i].x * q
				qq += q
			end
	end
	
	if (split) then
		cam_x = pl_camx(pl[1].x,64)
	elseif qq>0 then
		cam_x = pl_camx(xx/qq,128)
	end
	
end
-->8
-- actors

function init_actor_data()

function dummy() end

actor_dat=
{
	-- bridge builder
	[53]={
		ddy=0,
		friction=1,
		move=move_builder,
		draw=dummy
	},
	
	[64]={
		draw=draw_charge_powerup
	},
	
	[65]={
		draw=draw_exit
	},
	
	-- swirly
	[80]={
		life=2,
		frames=1,
		bounce=0,
		ddy=0, -- gravity
		move=move_swirly,
		draw=draw_swirly,
		can_bump=false,
		d=0.25,
		r=5 -- collisions
	},
	
	-- bouncy mushroom
	[82]={
		ddx=0,
		frames=1,
		active_t=0,
		move=move_mushroom
	},
	
	-- glitch mushroom
	[84]={
		draw=draw_glitch_mushroom
	},
	
	-- bird
	[93]={
		move=move_bird,
		draw=draw_bird,
		
		bounce=0,
		ddy=0.03,-- default:0.06
	},
	
	-- frog
	[96]={
		move=move_frog,
		draw=draw_frog,
		bounce=0,
		friction=1,
		tongue=0,
		tongue_t=0
	},
	
	[116]={
		draw=draw_tail
	}

}

end



function move_builder(a)
	
	local x,y=a.x,a.y-0.5
	local val=mget(x,y)
	if val==0 then
		mset(x,y,53)
		sfx(19)
	elseif val!=53
	then
		del(actor,a)
	end
	a.t += 1
	
	if (x<1 or x>126 or a.t > 30)
	then del(actor,a) end 
	
	for i=0,0.2,0.1 do
	local s=make_sparkle(
			104,a.x,a.y-0.5)   
	s.dx=cos(i+a.x/4)/8
	s.dy=sin(i+a.x/4)/8
	s.col=10
	s.max_t=10+rnd(10)
	end
	
	a.x+=a.dx
end

function move_frog(a)

	move_actor(a)
	
	if (a.life<=0) then
		bang_puff(a.x,a.y-0.5,104)
		sfx(26)
	end

	a.frame=0
	
	local p=closest_p(a,16)
	

	if (a.standing) then
		a.dy=0 a.dx=0
		
		-- jump
		
		if (rnd(20)<1 and
						a.tongue_t==0) then -- jump freq
			-- face player 2/3 times
			if rnd(3)<2 and p then
				a.d=sgn(p.x-a.x)
			end
			a.dy=-0.6-rnd(0.4)
			a.dx=a.d/4
			a.standing=false
			sfx(23)
		end
	else
		a.frame=1
	end
		
	-- move tongue
	
	-- stick tongue out when standing
	if a.tongue_t==0 and
				p and abs(a.x-p.x)<5 and
				rnd(20)<1 and
				a.standing then
		a.tongue_t=1
	end
	
	-- move active tongue
	if (a.tongue_t>0) then
		a.frame=2
		a.tongue_t = (a.tongue_t+1)%24
		local tlen = sin(a.tongue_t/48)*5
		a.tongue_x=a.x-tlen*a.d

		-- catch player
		
		if not a.ha and p then
			local dx=p.x-a.tongue_x
			local dy=p.y-a.y
			if (dx*dx+dy*dy<0.7^2)
			then a.ha=p sfx(22) end
		end
		
		-- skip to retracting
		if (solid(a.tongue_x,
						a.y-.5) and 
				a.tongue_t < 11) then
				a.tongue_t = 24-a.tongue_t
		end
	end
	
	-- move caught actor
	if (a.ha) then
		if (a.tongue_t>0) then
			a.ha.x = a.tongue_x
			a.ha.y = a.y
		else
			a.ha=nil
		end
	end
	
	--a.tongue=1 -- tiles
	
	a.t += 1
end


function draw_frog(a)
	draw_actor(a)
	
	local sx=a.x*8+a.d*4
	local sy=a.y*8-3
	local d=a.d
	
	
	if (a.tongue_t==0 or not a.tongue_t) return
	
	local sx2=a.tongue_x*8
	local sy2=(a.y+0.25)*8
	line(sx,sy,sx2,sy,8)
	rectfill(sx2,sy,sx2+d,sy-1,14)
end

function draw_charge_powerup(a)
	--pal(6,13+(a.t/4)%3)
	draw_actor(a)
	local sx=a.x*8
	local sy=a.y*8-4
	for i=0,5 do
		circfill(
			sx+cos(i/6+time()/2)*5.5,
			sy+sin(i/6+time()/2)*5.5,
			(i+time()*3)%1.5,7)
		end
		
end

function move_mushroom(a)
	a.frame=0
	if (a.active_t>0) then
		a.active_t-=1
		a.frame=1
	end
end

function draw_glitch_mushroom(a)
	local sx=a.x*8
	local sy=a.y*8-4
	
	draw_actor(a)


	dx=cos(time()*5)*3
	dy=sin(time()*3)*3
	
	for y=sy-12,sy+12 do
	for x=sx-12,sx+12 do
		local d=sqrt((y-sy)^2+(x-sx)^2)
		if (d<12 and 
			cos(d/5-time()*2)>.4) then
		pset(x,y,pget(x+dx,y+dy)
		+rnd(1.5))
--  pset(x,y,rnd(16))
		end
	end
	end
	
	pset(sx,sy,rnd(16))
	
	draw_actor(a)
end

function draw_exit(a)
	local sx=a.x*8
	local sy=a.y*8-4
	
	sy += cos(time()/2)*1.5
	
	circfill(sx-1+cos(time()*1.5),sy,3.5+cos(time()),8)
	circfill(sx+1+cos(time()*1.3),sy,3.5+cos(time()),12)
	circfill(sx,sy,3,7)
	
	for i=0,3 do
		circfill(
			sx+cos(i/8+time()*.6)*6,
			sy+sin(i/5+time()*.4)*6,
			1.5+cos(i/7+time()),
			8+i%5)
		circfill(
			sx+cos(.5+i/7+time()*.9)*5,
			sy+sin(.5+i/9+time()*.7)*5,
			.5+cos(.5+i/7+time()),
			14+i%2)
	end
	
end


function turn_to(a,ta,spd)
	
	a %=1 
	ta%=1
	
	while (ta < a-.5) ta += 1
	while (ta > a+.5) ta -= 1
	
	if (ta > a) then
		a = min(ta, a + spd)
	else
		a = max(ta, a - spd)
	end
	
	return a
end

function move_swirly(a)

	-- dying
	if (a.life==0 and a.t%4==0) then
		
		local tail=a.tail[1] 
		local s=tail[#tail]
		
		local cols= {7,15,14,15}
		-- reuse
		atomize_sprite(64,s.x-.5,s.y-.5,cols[1+#tail%#cols])
		del(tail,s) sfx(26)
		if (s==a) del(actor,a) sfx(27)
		
	end
	
	local ah=a.holding
	
	if (ah and a.tail and a.tail[1][15]) then
		ah.x=a.tail[1][15].x
		ah.y=a.tail[1][15].y
		
		ah.dy=-0.1 -- don't land
		if (a.standing) ah.x-=a.d/2
		if (ah.life==0) a.holding=nil
	end
	
	a.t += 1
	if (a.hit_t>0) a.hit_t-=1
	
	if (a.t < 20) then
		a.dx *=.95
		a.dy *=.95
	end
	
	a.x+=a.dx
	a.y+=a.dy
	a.dx *=.95
	a.dy *=.95
	
	local tx=a.homex
	local ty=a.homey
	local p=closest_p(a,200)
	if (p) tx,ty=p.x,p.y
	
	-- local variation
-- tx += cos(a.t/60)*3
-- ty += sin(a.t/40)*3
	
	local turn_spd=1/60
	local accel = 1/64
		
	-- charge 3 seconds 
	-- swirl 3 seconds
	if ((a.t%360 < 180
					and a.life > 1) 
					or a.life==0) and
					abs(a.x-tx<12) then
		ty -= 6
	else
		-- heat-seeking
		-- instant turn, but inertia
		-- means still get swirls
		turn_spd=1/30
		accel=1/40
		if (abs(a.x-tx)>12)accel*=1.5
	end
	
	
	a.d=turn_to(a.d,
		atan2(tx-a.x,ty-a.y),
		turn_spd
	)
	

	a.dx += cos(a.d)*accel
	a.dy += sin(a.d)*accel
	
	-- spawn tail
	if (not a.tail) then
		a.tail={}
		for j=1,3 do
		
			a.tail[j]={}
			for i=1,15 do
				local r=5-i*4/15
				r=mid(1,r,4)
				local slen=r/9 + 0.3
				if (j>1) then
					r=r/3 slen=0.3
					--if (i==1) slen=0
				end
				
				local seg={
					x=a.x-cos(a.d)*i/8,
					y=a.y-sin(a.d)*i/8,
					r=r,slen=slen
				}
				
				add(a.tail[j],seg)
				
			end
			a.tail[j][0]=a
		end
		
	end
	
	-- move tail
	
	for j=1,3 do
	for i=1,#a.tail[j] do
		
		local s=a.tail[j][i]
		local h=a.tail[j][i-1]
		local slen=s.len
		local hx = h.x
		local hy = h.y
		
		if (i==1) then
			if (j==2) hx -=.5 --hy-=.7
			if (j==3) hx +=.5 --hy-=.7
		end
		
		local dx=hx-s.x
		local dy=hy-s.y
		
		local aa=atan2(dx,dy)
	
		if (j==2) aa=turn_to(aa,7/8,0.02)
		if (j==3) aa=turn_to(aa,3/8,0.02)
		s.x=hx-cos(aa)*s.slen
		s.y=hy-sin(aa)*s.slen
	end
	end
	
	-- players collide with tail
	
	for i=0,#a.tail[1] do
	for pi=1,#pl do
		local p=pl[pi]
		if (alive(p) and a.life>0 and 
			p.life>0) then
			s = a.tail[1][i]
			local r=s.r/8 -- from pixels
			local dx=p.x-s.x
			local dy=(p.y-0.5)-s.y
			local dd=sqrt(dx*dx+dy*dy)
			local rr=0.5+r
			if (dd<0.5+r) then
					// janky bounce away
					local aa=atan2(dx,dy)
					aa+=rnd(0.4)-rnd(0.4)
					p.dx=cos(aa)/2
					p.dy=sin(aa)/2
					if (p.is_standing) p.dy=min(p.dy,-0.2)
					sfx(19)
					
					if (p.dash>0) then
						if (i==0) monster_hit(a)
					else
						player_hit(p)
					end
					
			end
		end
		end
		end
		
	
end


function draw_swirly(a)

	if (not a.tail) return
	
	for j=1,3 do
	for i=#a.tail[j],1,-1 do
		seg=a.tail[j][i]
		local sx=seg.x*8
		local sy=seg.y*8
		
		cols =  {7,15,14,15,7,7}
		cols2 = {6,14,8,14,6,6}
		local q= a.life==1 and 4 or 6
		local c=1+flr(i-time()*16)%q
		
		if (j>1) then
			if (i%2==0) then
			circfill(sx,sy,1,8)
			else
			pset(sx,sy,10)
			end
		else
			local r=seg.r+cos(i/8-time())/2
			r=mid(1,r,5)
			r=seg.r
			circfill(sx,sy+r/2,r,cols2[c])
			circfill(sx,sy,r,cols[c])
		end
		
	end
	end
	
	local sx=a.x*8
	local sy=a.y*8-4
	--circ(sx,sy+4,5,rnd(16))
	
	-- mouth
	spr(81,sx-4,sy+5+
		flr(cos(a.t/30)))
	-- head
	spr(80,sx-8,sy)
	spr(80,sx+0,sy,1,1,true)
-- 


end

function alive(a)
	if (not a) return false
	if (a.life <=0 and 
		(a.death_t and
			time() > a.death_t+0.5)
		) then return false end
	return true
end

-- ignore everything more than
-- 8 blocks away horizontally
function closest_a(a0,l,attr,maxdx)
	local best
	local best_d
	for i=1,#l do
		if not attr or l[i][attr] then
			local dx=l[i].x-a0.x
			local dy=l[i].y-a0.y
			d=dx*dx+dy*dy
			if (not best or d<best_d)
							and l[i]!=a0
							and l[i].life > 0
							and (not  maxdx or 
											abs(dx)<maxdx)
			then best=l[i] best_d=d end
		end
	end

	
	return best
end

function closest_p(a,dd)
	return closest_a(a,pl,nil,dd)
end


--[[
	birb
	follow player while close
	
	collect 
	
]]
function move_bird(a)

--[[
	-- spawn with gem
	if (a.t==0) then
		gem=make_actor(67,a.x,a.y)
		a.holding=gem
	end
]]

	move_actor(a)
	
	local ah=a.holding
	
	if (ah) then
		ah.x=a.x
		ah.y=a.y+0x0.e
		ah.dy=0
		if (a.standing) ah.x-=a.d/2
		if (ah.life==0) then
			a.holding=nil 
			sfx(28) -- chirp
		end
	end
	
	local p=closest_p(a,12)
	
	dx=100 dy=100
	-- patrol home no target
	tx,ty=
		a.homex+cos(a.t/120)*6,
		a.homey+sin(a.t/160)*4
	
	if (p) tx,ty=p.x,p.y-3
	
	local a2
	
	if (not a.holding) then
		a2=closest_a(a,actor,"is_pickup")
		if a2 and abs(a2.x-a.x)<4 and
					abs(a2.y-a.y)<4 then
			p=nil -- ignore player
			tx,ty=a2.x,a2.y
			if (a.standing) a.dy=-0.1
		else
			a2=nil -- ignore if far
		end
	end

	-- debug
-- a.tx=tx
-- a.ty=ty

	local dx,dy=tx-a.x,ty-a.y 
	local dd=sqrt(dx*dx+dy*dy)
	
	-- pick up
	if (a2 and dd<1) then
		
		a.holding=a2
		sfx(28) -- chirp
	
	end
	
	-- uncomment: pick up player!
	--[[
	if (p) then
		if (dd<0.5) a.holding=p
		if (a.holding==p) then
			if (btn(4,p.id) or btn(5,p.id)) a.holding=nil
			a.d=p.d
		end
	end
	]]
	
	if (a.t%8==0) a.d=sgn(dx)
	
	if (a.standing) then
		a.frame=0
		
		-- jump to start flying
		if (not solid(a.x,a.y+.2))a.dy=-0.2
		if (p and dd<5) a.dy=-0.3
		
		a.dx=0
		
	else
		-- flying
		local tt=a.t%12
		a.frame=1+tt/6
		-- flap
		if (tt==6) then
			local mag=.3 -- slowly decend
			
			-- fly up
			if (dd<4 and a.y>ty) mag=.4
			
			-- wall: fly to top
			if (a.hit_wall)mag=.45
			
			-- player can shoo upwards
			if (p and a.y>ty and not ah) mag=.45
			
			a.hit_wall = false
			a.dy-=mag
		end
	
		
		if (a.dy<0.2) then
			a.dx+=a.d/64
		end
		
	end
	
	a.frame=a.standing and 0 or
			1+(a.t/4)%2

end


function draw_bird(a)
	local q=flr(a.t/8)
	if ((q^2)%11<1) pal(1,15)
	
	draw_actor(a)
	
	-- debug: show target
	--[[
	if (a.tx) then
		local sx=a.tx*8
		local sy=a.ty*8
		circfill(sx,sy,1,rnd(16))
	end
	]]
end
-->8
-- themes (backgrounds)


theme_dat={

[1]={
	sky={12,12,12,12,12},
	bgels={
	
	{
		-- clouds
		src={16,56,16,8},
		xyz = {0,28*4,4,true},
		dx=-8,
		cols={15,7,1,-1},
		fill_down = 12
	},
	-- mountains
	{src={0,56,16,8},
		xyz = {0,28*4,4,true},
		fill_down=13,
	},
	
	-- leaves: light
	{src={32,48,16,6},
		xyz = {(118*8),-8,1.5},
		cols={1,3},
		fill_up=1
	},
	
	-- leaves: dark (foreground)
	{src={32,48,16,6},
		xyz = {(118*8),-12,0.8},
		cols={3,1},
		fill_up=1
	},
	
		
	}
},

--------------------------
-- level 2

[2]={
	sky={12},
	bgels={
	
	{
		-- gardens
		src={32,56,16,8},
		xyz = {0,100,4,true},
		--cols={7,6,15,6},
		cols={3,13,7,13,10,13,1,13,11,13,9,13,14,13,15,13,2,13},
		
		fill_down=13
	},
	{
		-- foreground shrubbery
		src={16,56,16,8},
		xyz = {0,64*0.8,0.6,true},
		cols={15,1,7,1},
		fill_down = 12
	},
	-- foreground shrubbery feature
	{
		src={32,56,8,8},
		xyz = {60,60*0.9,0.8,false},
		cols={15,1,7,1,3,1,11,1,10,1,9,1},
	},
	-- foreground shrubbery feature
	{
		src={32,56,8,8},
		xyz = {260,60*0.9,0.8,false},
		cols={15,1,7,1,3,1,11,1,10,1,9,1},
	},
	
	
		-- leaves: indigo
	{src={32,48,16,6},
		xyz = {40,64,4,true},
		cols={1,13,3,13},
		fill_up=13
	},
	
		-- leaves: light
	{src={32,48,16,6},
		xyz = {0,-4,1.5,true},
		cols={1,3},
		fill_up=1
	},
	
	-- leaves: dark (foreground)
	{src={32,48,16,6},
		xyz = {-40,-6,0.8,true},
		cols={3,1},
		fill_up=1
	}
	
	
	
	},
},
	----------------

-- double mountains

[3]={
	sky={12,14,14,14,14},
	bgels={
	
	
	-- mountains indigo (far)
	{src={0,56,16,8},
		xyz = {-64,30,8,true},
		fill_down=13,
		cols={6,15,13,6}
	},
	
	{
		-- clouds inbetween
		src={16,56,16,8},
		xyz = {0,50,8,true},
		dx=-30,
		cols={15,7,1,-1},
		fill_down = 7
	},
	
	-- mountains close
	{src={0,56,16,8},
		xyz = {0,140,8,true},
		fill_down=13,
		cols={6,5,13,1}
	},
		
	}
},

}

function init_level(lev)

  cls()reset()

	level=lev
	level_t = 0
	death_t = 0
	finished_t = 0
	gems = 0
	gem_sfx = {}
	total_gems = 0
	glitch_mushroom = false
	
	music(-1)

	if play_music then
	if (level==1) music(0)
	if (level==2) music(4)
	if (level==3) music(16)
	
	end

	actor = {}
	sparkle = {}
	pl = {}
	loot = {}
	
	reload()
	
	if (level <= 4) then
	-- copy section of map
	memcpy(0x2000,
			0x1000+((lev+1)%4)*0x800,
			0x800)
	end
	
	-- spawn player
	for y=0,15 do for x=0,127 do
	
		local val=mget(x,y)
		
		if (val == 72) then
			clear_cel(x,y)
			pl[1] = make_player(72, x+0.5,y+0.5,1)

			if (num_players==2) then
				pl[2] = make_player(88, x+2,y+1,1)
				pl[2].id = 1
			end
			
		end
		
		-- count gems
		if (val==67) then
			total_gems+=1
		end
		
		-- lootboxes
		if (val==48) then
			add(loot,67)
		end
	end end
	
	local num_booby=0
	-- shuffle lootboxes
	if (#loot > 1) then
		-- ~25% are booby prizes
		num_booby=flr((#loot+2) / 4)
		for i=1,num_booby do
			loot[i]=96
			if (rnd(10)<1) then
				loot[i]=84 -- mushroom
			end
		end
		
		-- shuffle
		for i=1,#loot do
			-- swap 2 random items
			j=flr(rnd(#loot))+1
			k=flr(rnd(#loot))+1
			loot[j],loot[k]=loot[k],loot[j]
		end
	end
	
	total_gems+= #loot-num_booby
	
	
	if (not pl[1]) then
		pl[1] = make_player(72,4,4,1)
	end

end

-->8
-- draw died / finished

function draw_finished(tt)

	if (tt < 15) return
	tt -= 15

	local str="★ stage clear ★  "
	
	print(str,64-#str*2,31,14)
	print(str,64-#str*2,30,7)
	
	-- gems
	local n = total_gems
	
	for i=1,15 do pal(i,13) end
	for pass=0,1 do
			
				for i=0,n-1 do
					t2=tt-(i*4+15)
					q=i<gems and t2>=0
					if (pass == 0 or q) then
						local y=50-pass
						if (q) then
								y+=sin(t2/8)*4/(t2/2)
								if (not gem_sfx[i]) sfx(25)
								gem_sfx[i]=true
						end
						
						spr(67,64-n*4+i*8,y)
						
					end
				end
	
		pal()
	end
	
	if (tt > 45) then
		print("❎ continue",42,91,12)
		print("❎ continue",42,90,7)
	end
	
end


```

## EditMode Tests
This cartridge's structural logic is formally verified in `Packages/com.hatiora.pico8.jelpi/Tests/Editor/JelpiCartridgeTests.cs`.

