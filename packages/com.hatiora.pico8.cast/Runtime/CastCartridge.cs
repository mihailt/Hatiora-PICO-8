using System;
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Cast
{
    /// <summary>
    /// Raycasting 3D engine demo by zep.
    /// DDA-based renderer with player movement, collision, gravity, and jetpack.
    /// </summary>
    public class CastCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null;

        public string SfxData   => Resources.Load<TextAsset>("Cast/cast/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Cast/cast/Music/music")?.text;
        public string MapData   => Resources.Load<TextAsset>("Cast/cast/Map/map")?.text;
        public string GffData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Cast/cast/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Cast/cast/Label/label");

        // Field of view (0.2 = 72 degrees in PICO-8 turns)
        private const float Fov = 0.2f;


        // Player state
        private float _plx, _ply, _plz;
        private float _pldx, _pldy, _pldz;
        private float _pld; // direction (in turns, 0..1)
        private bool  _jetpack;
        private float _lastTime;
        private float _frameAcc;

        public override void Init()
        {
            _plx = 12f; _ply = 12f;
            _pldx = 0f; _pldy = 0f;
            _plz = 12f;
            _pld = 0.25f;
            _pldz = 0f;
            _jetpack = false;

            // Multiply map values by 3 (same as Lua _init)
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    Mset(x, y, Mget(x, y) * 3);
        }

        /// <summary>Map height function: 16 - mget(x,y) * 0.125</summary>
        private float Mz(float x, float y)
        {
            return 16f - Mget(Flr(x), Flr(y)) * 0.125f;
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

            // Moving walls (Time()-based, always correct)
            float time = Time();
            for (int x = 10; x <= 18; x++)
                for (int y = 26; y <= 28; y++)
                    Mset(x, y, 34 + Flr(Cos(time / 4f + x / 14f) * 19f));

            // Control player
            float dx = 0f, dy = 0f;

            if (Btn(4, -1)) // ❎ = strafe mode
            {
                if (Btn(0, -1)) dx -= 1; // ⬅️
                if (Btn(1, -1)) dx += 1; // ➡️
            }
            else
            {
                if (Btn(0, -1)) _pld += 0.02f;
                if (Btn(1, -1)) _pld -= 0.02f;
            }

            // Forwards / backwards
            if (Btn(2, -1)) dy += 1; // ⬆️
            if (Btn(3, -1)) dy -= 1; // ⬇️

            float spd = Sqrt(dx * dx + dy * dy);
            if (spd > 0)
            {
                spd = 0.1f / spd;
                dx *= spd;
                dy *= spd;

                _pldx += Cos(_pld - 0.25f) * dx;
                _pldy += Sin(_pld - 0.25f) * dx;
                _pldx += Cos(_pld + 0.00f) * dy;
                _pldy += Sin(_pld + 0.00f) * dy;
            }

            // Collision detection
            float q = _plz - 0.6f;
            if (Mz(_plx + _pldx, _ply) > q)
                _plx += _pldx;
            if (Mz(_plx, _ply + _pldy) > q)
                _ply += _pldy;

            // Friction
            _pldx *= 0.6f;
            _pldy *= 0.6f;

            // Z = player feet
            if (_plz >= Mz(_plx, _ply) && _pldz >= 0)
            {
                _plz = Mz(_plx, _ply);
                _pldz = 0;
            }
            else
            {
                _pldz += 0.01f;
                _plz  += _pldz;
            }

            // Jetpack / jump when standing
            if (Btn(4, -1))
            {
                if (_jetpack || Mz(_plx, _ply) < _plz + 0.1f)
                    _pldz = -0.15f;
            }
        }

        public override void Draw()
        {
            int W = (int)(128 * ContentScale);
            int H = (int)(128 * ContentScale);

            Cls();

            // Sky background
            Rectfill(0, 0, W - 1, H - 1, 12);

            Draw3D(W, H);
        }

        private void Draw3D(int W, int H)
        {
            int maxSx = W - 1;
            int half = H / 2;

            // Calculate view plane
            float vx0 = Cos(_pld + Fov / 2f);
            float vy0 = Sin(_pld + Fov / 2f);
            float vx1 = Cos(_pld - Fov / 2f);
            float vy1 = Sin(_pld - Fov / 2f);

            for (int sx = 0; sx <= maxSx; sx++)
            {
                int sy = H - 1;

                // Camera based on player position
                float x = _plx;
                float y = _ply;
                float z = _plz - 1.5f; // Player eye 1.5 units high

                int ix = Flr(x);
                int iy = Flr(y);
                float tdist = 0;
                int col = Mget(ix, iy);
                float celz = 16f - col * 0.125f;
                float celz0;
                int col0;

                // Calc cast vector
                float t = sx / (float)maxSx;
                float vx = vx0 * (1f - t) + vx1 * t;
                float vy = vy0 * (1f - t) + vy1 * t;
                float dirX = Sgn(vx);
                float dirY = Sgn(vy);
                float skipX = 1f / Abs(vx);
                float skipY = 1f / Abs(vy);

                float distX, distY;
                if (vx > 0)
                    distX = 1f - (x % 1f);
                else
                    distX = x % 1f;
                if (vy > 0)
                    distY = 1f - (y % 1f);
                else
                    distY = y % 1f;

                distX *= skipX;
                distY *= skipY;

                int lastDir = 0;

                // DDA ray marching
                bool skip = true;
                while (skip)
                {
                    if (distX < distY)
                    {
                        ix += (int)dirX;
                        lastDir = 0;
                        distY -= distX;
                        tdist += distX;
                        distX = skipX;
                    }
                    else
                    {
                        iy += (int)dirY;
                        lastDir = 1;
                        distX -= distY;
                        tdist += distY;
                        distY = skipY;
                    }

                    // Previous cell properties
                    col0 = col;
                    celz0 = celz;

                    // New cell properties
                    col = Mget(ix, iy);
                    celz = 16f - col * 0.125f; // Inlined mz for speed

                    if (col == 72) skip = false;

                    // Discard close hits
                    if (tdist > 0.05f)
                    {
                        // Screen space: project height to screen Y
                        float sy1f = celz0 - z;
                        sy1f = (sy1f * half) / tdist;
                        sy1f += half; // Horizon
                        int sy1 = (int)sy1f;

                        // Draw ground to new point
                        if (sy1 < sy)
                        {
                            int groundCol = Sget((int)(celz0 * 2f) % 16, 8);
                            Line(sx, sy1 - 1, sx, sy, groundCol);
                            sy = sy1;
                        }

                        // Draw wall if higher
                        if (celz < celz0)
                        {
                            float wy1f = celz - z;
                            wy1f = (wy1f * half) / tdist;
                            wy1f += half; // Horizon
                            int wy1 = (int)wy1f;

                            if (wy1 < sy)
                            {
                                int wcol = lastDir * -6 + 13;
                                if (!skip)
                                    wcol = lastDir + 5;

                                Line(sx, wy1 - 1, sx, sy, wcol);
                                sy = wy1;
                            }
                        }
                    }
                } // while skip
            } // for sx

            // Label
            float scale = ContentScale;
            int s = Mathf.Max(1, (int)scale);
            Print("cpu:" + Flr(Stat(1) * 100) + "%", 1 * s, 1 * s, 7, CoordMode.Virtual, scale);
        }
    }
}

