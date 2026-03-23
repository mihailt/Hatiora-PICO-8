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
        private float _lastTime;
        private float _frameAcc;

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
        private new bool Btn(int i, int p = 0)
        {
            if (p == -1) return base.Btn(i, -1);
            if (NumPlayers == 1) return base.Btn(i, -1);
            return base.Btn(i, p);
        }

        private new bool Btnp(int i, int p = 0)
        {
            if (p == -1) return base.Btnp(i, -1);
            if (NumPlayers == 1) return base.Btnp(i, -1);
            return base.Btnp(i, p);
        }

        private bool Alive(Actor a)
        {
            if (a == null) return false;
            if (a.life <= 0 && a.deathT > 0 && Time() > a.deathT + 0.5f) return false;
            return true;
        }

        /// <summary>Returns true if any button (0-5) is pressed on player 0. Matches PICO-8 btn()>0.</summary>
        private bool AnyBtn()
        {
            return Btn(0, -1) || Btn(1, -1) || Btn(2, -1) || Btn(3, -1) || Btn(4, -1) || Btn(5, -1);
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
            // dt-based timing: 30fps
            float now = Time();
            float dt = now - _lastTime;
            _lastTime = now;
            if (dt <= 0 || dt > 0.5f) dt = 1f / 30f;
            _frameAcc += dt;
            if (_frameAcc < 1f / 30f) return;
            _frameAcc -= 1f / 30f;
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
            int W = (int)(128 * ContentScale);
            int H = (int)(128 * ContentScale);
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

