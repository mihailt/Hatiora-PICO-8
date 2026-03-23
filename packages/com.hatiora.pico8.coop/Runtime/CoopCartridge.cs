using System.Collections.Generic;
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Coop
{
    /// <summary>
    /// Minimal arena — black screen, players join/rejoin, dash/weapon, screen wrap.
    /// </summary>
    public class CoopCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null;

        // ─── Resources ───
        public string SfxData   => Resources.Load<TextAsset>("Coop/porklike/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Coop/porklike/Music/music")?.text;
        public string MapData   => Resources.Load<TextAsset>("Coop/porklike/Map/map")?.text;
        public string GffData   => Resources.Load<TextAsset>("Coop/porklike/Gff/gff")?.text;
        public Texture2D GfxTexture      => Resources.Load<Texture2D>("Coop/porklike/Gfx/gfx");
        public Texture2D LabelTexture    => Resources.Load<Texture2D>("Coop/porklike/Label/label");
        public Texture2D TilemaskTexture => Resources.Load<Texture2D>("Coop/porklike/Gfx/gfx");

        // ─── Constants ───
        private const int Players = 4;
        private static readonly int[] AnimFrames = { 164, 165, 166, 167 };
        private static readonly int[] PlayerColors = { 11, 12, 8, 9 };

        private const float MoveSpeed = 50f;
        private const float DashSpeed = 200f;
        private const float DashDuration = 0.15f;
        private const float DashPushForce = 250f;
        private const float AttackPushForce = 150f;
        private const float AtkDuration = 0.12f;
        private const float WeaponReach = 4f;
        private const int WeaponSpr = 121;

        // ─── Classes ───
        private class Mob
        {
            public int typ, hp;
            public float px, py, vx, vy;
            public bool joined, dead, dashing, flipX;
            public float dashTimer, aimAngle, atkTimer;
            public float faceDx, faceDy;
            public int color, flash, kills;
            public int device; // hardware input device (-1 = unassigned)
            public int[] ani;
        }

        private class Effect
        {
            public float x, y, dx, dy, drag;
            public int[] ani;
            public int dur, t;
        }

        // ─── State ───
        private Mob[] _pl;
        private List<Mob> _mobs = new List<Mob>();
        private List<Effect> _effects = new List<Effect>();
        private static readonly int[] BoomSpr = { 89, 90, 91, 92 };
        private float _lastTime;
        private int _t;

        // ═══════════════════════════════════════════════════
        //  INIT
        // ═══════════════════════════════════════════════════
        public override void Init()
        {
            _pl = new Mob[Players];
            _mobs.Clear();
            _effects.Clear();
            for (int p = 0; p < Players; p++)
            {
                _pl[p] = new Mob
                {
                    typ = 1, hp = 5,
                    ani = AnimFrames,
                    color = PlayerColors[p],
                    faceDx = 1,
                    device = -1 // unassigned
                };
                SpawnPlayer(p);
                _mobs.Add(_pl[p]);
            }
            _lastTime = Time();
        }

        private void SpawnPlayer(int p)
        {
            _pl[p].px = P8.Width / 2 + (p % 2 == 0 ? -20 : 20);
            _pl[p].py = P8.Height / 2 + (p < 2 ? -20 : 20);
            _pl[p].vx = 0; _pl[p].vy = 0;
            _pl[p].dashing = false;
        }

        // ═══════════════════════════════════════════════════
        //  UPDATE
        // ═══════════════════════════════════════════════════
        public override void Update()
        {
            _t++;
            float now = Time();
            float dt = now - _lastTime;
            _lastTime = now;
            if (dt <= 0 || dt > 0.5f) dt = 1f / 60f;

            // ─── Join: scan all devices, assign to first free slot ───
            for (int dev = 0; dev < Players; dev++)
            {
                if (!Btnp(5, dev)) continue;
                // Is this device already claimed by a joined/dead player?
                bool claimed = false;
                for (int s = 0; s < Players; s++)
                    if (_pl[s].device == dev) { claimed = true; break; }
                if (claimed) continue;
                // Find first free slot
                for (int s = 0; s < Players; s++)
                {
                    if (!_pl[s].joined)
                    {
                        _pl[s].joined = true; _pl[s].dead = false; _pl[s].hp = 5;
                        _pl[s].flash = 10; _pl[s].device = dev;
                        SpawnPlayer(s);
                        Sfx(10);
                        break;
                    }
                }
            }

            // ─── Player input ───
            for (int p = 0; p < Players; p++)
            {
                if (!_pl[p].joined || _pl[p].device < 0) continue;
                int dev = _pl[p].device;

                // Rejoin on death
                if (_pl[p].dead)
                {
                    if (Btnp(5, dev))
                    {
                        _pl[p].dead = false; _pl[p].hp = 5;
                        _pl[p].flash = 10; _pl[p].kills = 0;
                        SpawnPlayer(p);
                        Sfx(10);
                    }
                    continue;
                }

                // Movement input (analog left stick > digital buttons)
                float dx = Stat(42 + dev * 2);     // left stick X
                float dy = -Stat(43 + dev * 2);    // left stick Y (invert: Unity Y-up → screen Y-down)
                float moveLen = Sqrt(dx * dx + dy * dy);
                if (moveLen < 0.15f) // deadzone — fall back to digital buttons
                {
                    dx = 0; dy = 0;
                    if (Btn(0, dev)) dx -= 1;
                    if (Btn(1, dev)) dx += 1;
                    if (Btn(2, dev)) dy -= 1;
                    if (Btn(3, dev)) dy += 1;
                    if (dx != 0 && dy != 0) { dx *= 0.707f; dy *= 0.707f; }
                }
                if (dx != 0 || dy != 0) { _pl[p].faceDx = dx; _pl[p].faceDy = dy; }

                // Aim (analog right stick > mouse > movement direction, persistent)
                float aimX = Stat(34 + dev * 2);   // right stick X
                float aimY = -Stat(35 + dev * 2);  // right stick Y (invert: Unity Y-up → screen Y-down)
                float aimLen = Sqrt(aimX * aimX + aimY * aimY);
                if (aimLen > 0.2f) // deadzone
                    _pl[p].aimAngle = Atan2(aimX, aimY);
                else if (dev == 0) // mouse aim for keyboard device
                {
                    float mx = Stat(32), my = Stat(33);
                    float mdx2 = mx - (_pl[p].px + 4);
                    float mdy2 = my - (_pl[p].py + 4);
                    if (mdx2 * mdx2 + mdy2 * mdy2 > 4)
                        _pl[p].aimAngle = Atan2(mdx2, mdy2);
                }
                // No fallback — aim persists from last input
                _pl[p].flipX = Cos(_pl[p].aimAngle) < 0;

                // Dash (in movement direction)
                if (Btnp(5, dev) && !_pl[p].dashing && _pl[p].flash <= 0)
                {
                    float fdx = _pl[p].faceDx, fdy = _pl[p].faceDy;
                    float flen = Sqrt(fdx * fdx + fdy * fdy);
                    if (flen > 0.01f) { fdx /= flen; fdy /= flen; }
                    _pl[p].dashing = true;
                    _pl[p].dashTimer = DashDuration;
                    _pl[p].vx = fdx * DashSpeed;
                    _pl[p].vy = fdy * DashSpeed;
                    _pl[p].flash = 8;
                    Sfx(11);
                }

                // Walk
                if (!_pl[p].dashing)
                {
                    _pl[p].px += dx * MoveSpeed * dt;
                    _pl[p].py += dy * MoveSpeed * dt;
                }

                // Attack
                _pl[p].atkTimer -= dt;
                if (Btnp(4, dev) && _pl[p].atkTimer <= 0)
                {
                    _pl[p].atkTimer = AtkDuration;
                    float hitX = _pl[p].px + 4 + Cos(_pl[p].aimAngle) * 12;
                    float hitY = _pl[p].py + 4 + Sin(_pl[p].aimAngle) * 12;
                    Sfx(14);
                    for (int i = _mobs.Count - 1; i >= 0; i--)
                    {
                        var m = _mobs[i];
                        if (m == _pl[p] || m.dead || !m.joined) continue;
                        float mdx = (m.px + 4) - hitX;
                        float mdy = (m.py + 4) - hitY;
                        float d = Sqrt(mdx * mdx + mdy * mdy);
                        if (d < 12)
                        {
                            m.hp--;
                            m.flash = 10;
                            if (m.hp <= 0)
                            {
                                m.dead = true;
                                _pl[p].kills++;
                                Boom(m.px, m.py);
                                Sfx(12);
                            }
                            else if (d > 0.01f)
                            {
                                m.vx = (mdx / d) * AttackPushForce;
                                m.vy = (mdy / d) * AttackPushForce;
                                m.dashing = true;
                                m.dashTimer = 0.15f;
                            }
                        }
                    }
                }
            }

            // ═══ LOOP 1: Movement — apply velocity ═══
            foreach (var m in _mobs)
            {
                if (!m.dashing || m.dead) continue;
                m.dashTimer -= dt;
                m.px += m.vx * dt;
                m.py += m.vy * dt;
                if (m.dashTimer <= 0)
                {
                    m.dashing = false;
                    m.vx = 0; m.vy = 0;
                }
            }

            // ═══ LOOP 2: Screen wrap ═══
            foreach (var m in _mobs)
            {
                if (m.dead) continue;
                if (m.px < -8) m.px = P8.Width;
                else if (m.px > P8.Width) m.px = -8;
                if (m.py < -8) m.py = P8.Height;
                else if (m.py > P8.Height) m.py = -8;
            }

            // ═══ LOOP 3: Mob collision ═══
            for (int i = 0; i < _mobs.Count; i++)
            {
                var a = _mobs[i];
                if (a.dead || !a.joined) continue;
                for (int j = i + 1; j < _mobs.Count; j++)
                {
                    var b = _mobs[j];
                    if (b.dead || !b.joined) continue;

                    float cx = b.px - a.px;
                    float cy = b.py - a.py;
                    float dist = Sqrt(cx * cx + cy * cy);
                    if (dist < 7 && dist > 0.01f)
                    {
                        float nx = cx / dist, ny = cy / dist;
                        float overlap = 7 - dist;

                        if (a.dashing && b.dashing)
                        {
                            float tv = a.vx; a.vx = b.vx * 0.5f; b.vx = tv * 0.5f;
                            tv = a.vy; a.vy = b.vy * 0.5f; b.vy = tv * 0.5f;
                            a.px -= nx * overlap * 0.5f; a.py -= ny * overlap * 0.5f;
                            b.px += nx * overlap * 0.5f; b.py += ny * overlap * 0.5f;
                        }
                        else if (a.dashing)
                        {
                            b.vx = nx * DashPushForce; b.vy = ny * DashPushForce;
                            b.dashing = true; b.dashTimer = 0.2f; b.flash = 10;
                            a.vx = -nx * DashPushForce * 0.5f; a.vy = -ny * DashPushForce * 0.5f;
                            a.dashTimer = 0.1f; Sfx(13);
                        }
                        else if (b.dashing)
                        {
                            a.vx = -nx * DashPushForce; a.vy = -ny * DashPushForce;
                            a.dashing = true; a.dashTimer = 0.2f; a.flash = 10;
                            b.vx = nx * DashPushForce * 0.5f; b.vy = ny * DashPushForce * 0.5f;
                            b.dashTimer = 0.1f; Sfx(13);
                        }
                        else
                        {
                            a.px -= nx * overlap * 0.5f; a.py -= ny * overlap * 0.5f;
                            b.px += nx * overlap * 0.5f; b.py += ny * overlap * 0.5f;
                        }
                    }
                }
            }

            // Effects update
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                var ef = _effects[i];
                ef.t++;
                ef.x += ef.dx; ef.y += ef.dy;
                ef.dx *= ef.drag; ef.dy *= ef.drag;
                if (ef.t >= ef.dur) _effects.RemoveAt(i);
            }
        }

        // ═══════════════════════════════════════════════════
        //  DRAW
        // ═══════════════════════════════════════════════════
        public override void Draw()
        {
            Cls(0);

            float t = Time();
            int animTick = (int)(t * 60f);

            // Effects
            foreach (var ef in _effects)
            {
                int fi = ef.ani[(ef.t / 6) % ef.ani.Length];
                Palt(11, true); Palt(0, false);
                Sspr((fi % 16) * 8, (fi / 16) * 8, 8, 8, (int)ef.x, (int)ef.y, 8, 8);
                Pal(); Palt();
            }

            // Players
            for (int p = 0; p < Players; p++)
            {
                if (!_pl[p].joined || _pl[p].dead) continue;

                int col = _pl[p].color;
                if (_pl[p].flash > 0) { _pl[p].flash--; col = 7; }

                int dv = _pl[p].device;
                bool walking = dv >= 0 && !_pl[p].dashing && (Btn(0, dv) || Btn(1, dv) || Btn(2, dv) || Btn(3, dv));
                int frame = walking || _pl[p].dashing
                    ? AnimFrames[(animTick / 8) % AnimFrames.Length]
                    : AnimFrames[0];

                Palt(11, true); Palt(0, false);
                Pal(10, col);
                Spr(frame, Flr(_pl[p].px), Flr(_pl[p].py), 1, 1, _pl[p].flipX);
                Pal(); Palt();

                // Weapon
                float reach = WeaponReach;
                if (_pl[p].atkTimer > 0)
                {
                    float thrustT = 1f - _pl[p].atkTimer / AtkDuration;
                    reach += 8f * (1f - thrustT);
                }
                float wpnDx = Cos(_pl[p].aimAngle);
                float wpnDy = Sin(_pl[p].aimAngle);
                int wx = Flr(_pl[p].px + 4 + wpnDx * reach - 4);
                int wy = Flr(_pl[p].py + 4 + wpnDy * reach - 4);
                int wsx = (WeaponSpr % 16) * 8;
                int wsy = (WeaponSpr / 16) * 8;
                Palt(0, true);
                Sspr(wsx, wsy, 8, 8, wx, wy, 8, 8, angle: 0.25f - _pl[p].aimAngle);
                Palt();
            }

            // ─── Corner boxes (Wind-style HP / join prompts) ───
            for (int p = 0; p < Players; p++)
            {
                string txt;
                int col;
                if (!_pl[p].joined || _pl[p].dead)
                {
                    txt = "PRESS \u00D7 TO JOIN";
                    col = _pl[p].color;
                }
                else
                {
                    txt = "P" + (p + 1) + " \u00C7" + Max(_pl[p].hp, 0) + " \u00C9" + _pl[p].kills;
                    col = _pl[p].color;
                }

                int bw = (txt.Length + 2) * 4 + 7;
                int bh = 13;
                bool isRight = p == 1 || p == 3;
                bool isBottom = p == 2 || p == 3;
                int bx = isRight ? P8.Width - bw - 2 : 2;
                int by = isBottom ? P8.Height - bh - 2 : 2;

                // Wind-style box: black fill + colored border
                Rectfill(bx, by, bx + bw - 1, by + bh - 1, 0);
                Rect(bx + 1, by + 1, bx + bw - 2, by + bh - 2, col);
                // Text centered in box
                Print(txt, bx + 5, by + 4, col);
            }
        }

        // ═══════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════
        private void Boom(float bx, float by)
        {
            for (int i = 0; i <= 20; i++)
            {
                float ang = Rnd();
                float dist = Rnd(5);
                float spd = 0.5f + Rnd(0.5f);
                _effects.Add(new Effect
                {
                    ani = BoomSpr, t = 0,
                    dur = (int)(10 + Rnd(35)),
                    x = bx + Sin(ang) * dist,
                    y = by + Cos(ang) * dist,
                    dx = Sin(ang) * spd,
                    dy = Cos(ang) * spd,
                    drag = 0.9f
                });
            }
        }
    }
}
