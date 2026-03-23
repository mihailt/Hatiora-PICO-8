using System;
using System.Collections.Generic;
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Collide
{
    /// <summary>
    /// Wall and actor collision demo by zep.
    /// AABB-based collisions with bounce, friction, treasure collection, and room camera.
    /// </summary>
    public class CollideCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null;

        public string SfxData   => Resources.Load<TextAsset>("Collide/collide/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Collide/collide/Music/music")?.text;
        public string MapData   => Resources.Load<TextAsset>("Collide/collide/Map/map")?.text;
        public string GffData   => Resources.Load<TextAsset>("Collide/collide/Gff/gff")?.text;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Collide/collide/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Collide/collide/Label/label");


        private class Actor
        {
            public int k;        // sprite id
            public float x, y;   // center position (in map tiles)
            public float dx, dy; // velocity
            public float frame;
            public int t;
            public float friction;
            public float bounce;
            public int frames;   // animation frame count
            public float w, h;   // half-width, half-height
        }

        private readonly List<Actor> _actors = new List<Actor>();
        private Actor _pl;
        private float _lastTime;
        private float _frameAcc;

        private Actor MakeActor(int k, float x, float y)
        {
            var a = new Actor
            {
                k = k,
                x = x, y = y,
                dx = 0, dy = 0,
                frame = 0, t = 0,
                friction = 0.15f,
                bounce = 0.3f,
                frames = 2,
                w = 0.4f, h = 0.4f
            };
            _actors.Add(a);
            return a;
        }

        public override void Init()
        {
            _actors.Clear();

            // Player
            _pl = MakeActor(21, 2, 2);
            _pl.frames = 4;

            // Bouncy ball (green)
            var ball1 = MakeActor(33, 8.5f, 11);
            ball1.dx = 0.05f;
            ball1.dy = -0.1f;
            ball1.friction = 0.02f;
            ball1.bounce = 1f;

            // Red ball: bounce forever (no friction, max bounce)
            var ball2 = MakeActor(49, 7, 8);
            ball2.dx = -0.1f;
            ball2.dy = 0.15f;
            ball2.friction = 0f;
            ball2.bounce = 1f;

            // Treasure ring
            for (int i = 0; i <= 16; i++)
            {
                var a = MakeActor(35, 8 + Cos(i / 16f) * 3, 10 + Sin(i / 16f) * 3);
                a.w = 0.25f;
                a.h = 0.25f;
            }

            // Blue peopleoids
            var bp = MakeActor(5, 7, 5);
            bp.frames = 4;
            bp.dx = 1f / 8f;
            bp.friction = 0.1f;

            for (int i = 1; i <= 6; i++)
            {
                var a = MakeActor(5, 20 + i, 24);
                a.frames = 4;
                a.dx = 1f / 8f;
                a.friction = 0.1f;
            }
        }

        /// <summary>True if map cell at (x,y) has flag 1 set (wall).</summary>
        private bool Solid(float x, float y)
        {
            int val = Mget(Flr(x), Flr(y));
            return Fget(val, 1) != 0;
        }

        /// <summary>True if AABB overlaps any wall tile (works for actors < 1 tile).</summary>
        private bool SolidArea(float x, float y, float w, float h)
        {
            return Solid(x - w, y - h) ||
                   Solid(x + w, y - h) ||
                   Solid(x - w, y + h) ||
                   Solid(x + w, y + h);
        }

        /// <summary>
        /// True if actor [a] will hit another actor after moving (dx,dy).
        /// Also handles bounce response.
        /// </summary>
        private bool SolidActor(Actor a, float dx, float dy)
        {
            for (int i = _actors.Count - 1; i >= 0; i--)
            {
                var a2 = _actors[i];
                if (a2 == a) continue;

                float x = (a.x + dx) - a2.x;
                float y = (a.y + dy) - a2.y;

                if (Abs(x) < (a.w + a2.w) && Abs(y) < (a.h + a2.h))
                {
                    // Along x
                    if (dx != 0 && Abs(x) < Abs(a.x - a2.x))
                    {
                        float v = Abs(a.dx) > Abs(a2.dx) ? a.dx : a2.dx;
                        a.dx = v;
                        a2.dx = v;

                        bool ca = CollideEvent(a, a2) || CollideEvent(a2, a);
                        return !ca;
                    }

                    // Along y
                    if (dy != 0 && Abs(y) < Abs(a.y - a2.y))
                    {
                        float v = Abs(a.dy) > Abs(a2.dy) ? a.dy : a2.dy;
                        a.dy = v;
                        a2.dy = v;

                        bool ca = CollideEvent(a, a2) || CollideEvent(a2, a);
                        return !ca;
                    }
                }
            }
            return false;
        }

        /// <summary>Checks both walls and actors.</summary>
        private bool SolidA(Actor a, float dx, float dy)
        {
            if (SolidArea(a.x + dx, a.y + dy, a.w, a.h))
                return true;
            return SolidActor(a, dx, dy);
        }

        /// <summary>Returns true when something was collected/destroyed (no bounce).</summary>
        private bool CollideEvent(Actor a1, Actor a2)
        {
            // Player collects treasure
            if (a1 == _pl && a2.k == 35)
            {
                _actors.Remove(a2);
                Sfx(3);
                return true;
            }

            Sfx(2); // Generic bump sound
            return false;
        }

        private void MoveActor(Actor a)
        {
            // Move along x if no collision
            if (!SolidA(a, a.dx, 0))
                a.x += a.dx;
            else
                a.dx *= -a.bounce;

            // Move along y if no collision
            if (!SolidA(a, 0, a.dy))
                a.y += a.dy;
            else
                a.dy *= -a.bounce;

            // Friction
            a.dx *= (1f - a.friction);
            a.dy *= (1f - a.friction);

            // Animation: advance one frame per 1/4 tile moved
            a.frame += Abs(a.dx) * 4;
            a.frame += Abs(a.dy) * 4;
            a.frame %= a.frames;

            a.t += 1;
        }

        private void ControlPlayer()
        {
            float accel = 0.05f;
            if (Btn(0, -1)) _pl.dx -= accel;
            if (Btn(1, -1)) _pl.dx += accel;
            if (Btn(2, -1)) _pl.dy -= accel;
            if (Btn(3, -1)) _pl.dy += accel;
        }

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

            ControlPlayer();

            // Move all actors (iterate copy to allow removal)
            var snapshot = new List<Actor>(_actors);
            foreach (var a in snapshot)
            {
                if (_actors.Contains(a))
                    MoveActor(a);
            }
        }

        public override void Draw()
        {
            float s = ContentScale;
            int si = (int)s;
            Cls();

            int roomX = Flr(_pl.x / 16f);
            int roomY = Flr(_pl.y / 16f);

            // Camera in scaled pixel space
            Camera((int)(roomX * 128 * s), (int)(roomY * 128 * s));

            if (s <= 1f)
            {
                // Non-hires: standard Map/Spr at 128×128 (top-left quarter)
                Map();
                foreach (var a in _actors)
                    Spr(a.k + Flr(a.frame), (int)(a.x * 8) - 4, (int)(a.y * 8) - 4);
            }
            else
            {
                // Hires: draw map tiles manually at scale
                int mx = roomX * 16;
                int my = roomY * 16;
                for (int ty = 0; ty < 16; ty++)
                {
                    for (int tx = 0; tx < 16; tx++)
                    {
                        int tile = Mget(mx + tx, my + ty);
                        if (tile > 0)
                        {
                            Sspr((tile % 16) * 8, (tile / 16) * 8, 8, 8,
                                (mx + tx) * 8 * si, (my + ty) * 8 * si, 8 * si, 8 * si);
                        }
                    }
                }

                // Actors at scale
                foreach (var a in _actors)
                {
                    int sprId = a.k + Flr(a.frame);
                    Sspr((sprId % 16) * 8, (sprId / 16) * 8, 8, 8,
                        (int)(a.x * 8 * s) - 4 * si, (int)(a.y * 8 * s) - 4 * si, 8 * si, 8 * si);
                }
            }
        }
    }
}

