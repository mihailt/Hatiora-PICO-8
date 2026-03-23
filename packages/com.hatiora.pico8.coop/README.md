# Coop Cartridge (pork.p8, pork_tilemask.p8)

## Overview
This cartridge is a **custom 4-player local cooperative multiplayer sample** that demonstrates how to utilize multiple simultaneous inputs and basic grid-based movement within the Hatiora PICO-8 V2 engine architecture.

**Note:** Unlike other cartridges in this repository, CoopCartridge is **not** a direct code translation or port of the original pork.p8 game logic. Instead, it is an entirely original C# implementation that borrows the graphical and audio assets (__gfx__, __sfx__, __map__, etc.) from the Porklike game for demonstrative purposes.

## Implementation Details
The C# CoopCartridge implements a custom tick-based logic tailored for local cooperative gameplay, overriding Cartridge.Update and checking inputs for up to 4 players (Btn(., p)).

### Key Features
- **4-Player Local Support:** Maps keyboard and gamepad inputs to players 0 through 3.
- **Tick-based Grid Movement:** Players execute movement and bumping actions on an 8x8 grid space.
- **Custom Knockback Logic:** An original chain-bumping mechanic where players bumping into each other send each other sliding across the grid.
- **Time Delta Sourced:** Utilizes P8.Time() for calculating custom movement animations.

### Raw C# Source
```csharp
using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Coop
{
    /// <summary>
    /// Coop game using the new V2 architecture.
    /// Extends <see cref="Cartridge"/> — uses P8.* API, no Unity deps in game logic.
    /// </summary>
    public class CoopCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; // use whatever the builder provides

        // ─── Resources (Unity-dependent, loaded externally) ───
        public string SfxData   => Resources.Load<TextAsset>("Porklike/pork/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Porklike/pork/Music/music")?.text;
        public string MapData   => Resources.Load<TextAsset>("Porklike/pork/Map/map")?.text;
        public string GffData   => Resources.Load<TextAsset>("Porklike/pork/Gff/gff")?.text;
        public Texture2D GfxTexture      => Resources.Load<Texture2D>("Porklike/pork/Gfx/gfx");
        public Texture2D LabelTexture    => null;
        public Texture2D TilemaskTexture => Resources.Load<Texture2D>("Porklike/pork_tilemask/Gfx/gfx");

        private const int Players = 4;
        private const int TileSize = 8; // match SpriteSize — V2 draws 1:1, no fontScale
        private int _gridW => P8.Width / TileSize;
        private int _gridH => P8.Height / TileSize;

        private static readonly int[] AnimFrames = { 240, 241, 242, 243 };

        private int[] _cx, _cy;
        private float[] _ox, _oy;
        private float[] _sox, _soy;
        private bool[] _flipX;
        private int[] _color;
        private int[] _faceDx, _faceDy;
        private int[] _flash;

        private float[] _pt;
        private bool[] _isWalking;
        private bool[] _inTurn;

        private int[] _chainTarget;
        private int[] _chainDx, _chainDy, _chainDist;

        private float[] _moveTimer;
        private int[] _lastDir;
        private float[] _speed;
        private const float RepeatDelay = 0.3f;
        private const float RepeatRate = 0.2f;

        private float _dt;  // set before update via Tick override

        public override void Init()
        {
            _color = new[] { 11, 12, 8, 9 };
            _cx = new int[Players];
            _cy = new int[Players];
            _ox = new float[Players];
            _oy = new float[Players];
            _sox = new float[Players];
            _soy = new float[Players];
            _flipX = new bool[Players];
            _faceDx = new int[Players];
            _faceDy = new int[Players];
            _flash = new int[Players];
            _pt = new float[Players];
            _isWalking = new bool[Players];
            _inTurn = new bool[Players];
            _chainTarget = new int[Players];
            _chainDx = new int[Players];
            _chainDy = new int[Players];
            _chainDist = new int[Players];
            _moveTimer = new float[Players];
            _lastDir = new int[Players];
            _speed = new float[Players];

            for (int p = 0; p < Players; p++)
            {
                _faceDx[p] = 1;
                _lastDir[p] = -1;
                _chainTarget[p] = -1;
                _speed[p] = 1f;
            }

            int startCx = (_gridW - Players * 2 + 1) / 2;
            int centerCy = _gridH / 2;

            for (int p = 0; p < Players; p++)
            {
                _cx[p] = startCx + p * 2;
                _cy[p] = centerCy;
            }

            Music(0);
        }

        /// <summary>
        /// The new Cartridge Update() has no dt.
        /// We use P8.Time() delta instead.
        /// </summary>
        private float _lastTime;

        public override void Update()
        {
            float now = Time();
            float dt = now - _lastTime;
            _lastTime = now;
            if (dt <= 0 || dt > 0.5f) dt = 1f / 60f; // sanity

            for (int p = 0; p < Players; p++)
            {
                if (_inTurn[p])
                {
                    _pt[p] = Min(_pt[p] + 0.125f * dt * 60f * _speed[p], 1f);

                    if (_isWalking[p])
                    {
                        float tme = 1f - _pt[p];
                        _ox[p] = _sox[p] * tme;
                        _oy[p] = _soy[p] * tme;
                    }
                    else
                    {
                        float tme = _pt[p] > 0.5f ? 1f - _pt[p] : _pt[p];
                        _ox[p] = _sox[p] * tme;
                        _oy[p] = _soy[p] * tme;
                    }

                    if (_pt[p] >= 1f)
                    {
                        _inTurn[p] = false;
                        _ox[p] = 0;
                        _oy[p] = 0;

                        if (_chainTarget[p] >= 0)
                        {
                            PushMob(_chainTarget[p], _chainDx[p], _chainDy[p], _chainDist[p]);
                            _chainTarget[p] = -1;
                            Sfx(58);
                        }
                    }

                    continue;
                }

                int dir = -1;
                if (Btn(0, p)) dir = 0;
                else if (Btn(1, p)) dir = 1;
                else if (Btn(2, p)) dir = 2;
                else if (Btn(3, p)) dir = 3;

                bool shouldMove = false;
                if (dir == -1)
                {
                    _lastDir[p] = -1;
                    _moveTimer[p] = 0;
                }
                else if (dir != _lastDir[p])
                {
                    _lastDir[p] = dir;
                    _moveTimer[p] = 0;
                    shouldMove = true;
                }
                else
                {
                    _moveTimer[p] += dt;
                    if (_moveTimer[p] >= RepeatDelay)
                    {
                        _moveTimer[p] -= RepeatRate;
                        shouldMove = true;
                    }
                }

                if (dir != -1)
                {
                    int d_dx = dir == 0 ? -1 : dir == 1 ? 1 : 0;
                    int d_dy = dir == 2 ? -1 : dir == 3 ? 1 : 0;

                    if (d_dx != 0) _flipX[p] = d_dx < 0;
                    _faceDx[p] = d_dx;
                    _faceDy[p] = d_dy;
                }

                if (Btnp(5, p))
                {
                    int dx = _faceDx[p], dy = _faceDy[p];
                    PushMob(p, dx, dy, 3, 2f); // 3 tiles, 2x speed
                    Sfx(63);
                }
                else if (Btnp(4, p))
                {
                    int dx = _faceDx[p], dy = _faceDy[p];
                    int nx = (_cx[p] + dx + _gridW) % _gridW;
                    int ny = (_cy[p] + dy + _gridH) % _gridH;

                    MobBump(p, dx, dy);
                    _flash[p] = 10;

                    for (int o = 0; o < Players; o++)
                    {
                        if (o != p && _cx[o] == nx && _cy[o] == ny)
                        {
                            // 3 cell base push + (1 or 2 random additional cells)
                            int pushDist = 3 + (int)UnityEngine.Mathf.Floor(Rnd(2f)) + 1;
                            PushMob(o, dx, dy, pushDist);
                            break;
                        }
                    }
                    Sfx(58);
                }
                else if (shouldMove)
                {
                    int dx = _faceDx[p], dy = _faceDy[p];
                    int nx = (_cx[p] + dx + _gridW) % _gridW;
                    int ny = (_cy[p] + dy + _gridH) % _gridH;

                    bool blocked = false;
                    for (int o = 0; o < Players; o++)
                    {
                        if (o != p && _cx[o] == nx && _cy[o] == ny)
                        { blocked = true; break; }
                    }

                    if (blocked)
                    {
                        MobBump(p, dx, dy);
                        _flash[p] = 10;
                        for (int o = 0; o < Players; o++)
                        {
                            if (o != p && _cx[o] == nx && _cy[o] == ny)
                            {
                                int pushDist = 3 + (int)UnityEngine.Mathf.Floor(Rnd(2f)) + 1;
                                PushMob(o, dx, dy, pushDist);
                                break;
                            }
                        }
                        Sfx(58);
                    }
                    else
                    {
                        MobWalk(p, dx, dy, nx, ny);
                        Sfx(63);
                    }
                }
            }
        }

        private void PushMob(int target, int dx, int dy, int dist, float speedMultiplier = 1f)
        {
            _flash[target] = 10;
            int destX = _cx[target], destY = _cy[target];
            int hitPlayer = -1;

            for (int step = 0; step < dist; step++)
            {
                int testX = (destX + dx + _gridW) % _gridW;
                int testY = (destY + dy + _gridH) % _gridH;

                int blocker = -1;
                for (int q = 0; q < Players; q++)
                {
                    if (q != target && _cx[q] == testX && _cy[q] == testY)
                    { blocker = q; break; }
                }

                if (blocker >= 0) { hitPlayer = blocker; break; }
                destX = testX;
                destY = testY;
            }

            int totalDx = destX - _cx[target];
            int totalDy = destY - _cy[target];
            if (totalDx > _gridW / 2) totalDx -= _gridW;
            if (totalDx < -_gridW / 2) totalDx += _gridW;
            if (totalDy > _gridH / 2) totalDy -= _gridH;
            if (totalDy < -_gridH / 2) totalDy += _gridH;

            _cx[target] = destX;
            _cy[target] = destY;
            _sox[target] = -totalDx * TileSize;
            _soy[target] = -totalDy * TileSize;
            _ox[target] = _sox[target];
            _oy[target] = _soy[target];
            _isWalking[target] = true;
            _pt[target] = 0;
            _speed[target] = speedMultiplier;
            _inTurn[target] = true;

            if (hitPlayer >= 0)
            {
                _chainTarget[target] = hitPlayer;
                _chainDx[target] = dx;
                _chainDy[target] = dy;
                // Add random additional knockback distance
                _chainDist[target] = 3 + (int)UnityEngine.Mathf.Floor(Rnd(2f)) + 1;
            }
        }

        private void MobWalk(int p, int dx, int dy, int nx, int ny)
        {
            _cx[p] = nx;
            _cy[p] = ny;
            _sox[p] = -dx * TileSize;
            _soy[p] = -dy * TileSize;
            _ox[p] = _sox[p];
            _oy[p] = _soy[p];
            _isWalking[p] = true;
            _pt[p] = 0;
            _speed[p] = 1f;
            _inTurn[p] = true;
        }

        private void MobBump(int p, int dx, int dy)
        {
            _sox[p] = dx * TileSize;
            _soy[p] = dy * TileSize;
            _ox[p] = 0;
            _oy[p] = 0;
            _isWalking[p] = false;
            _pt[p] = 0;
            _speed[p] = 1f;
            _inTurn[p] = true;
        }

        public override void Draw()
        {
            Cls(0);

            float t = Time();
            int animTick = (int)(t * 60f);
            int frame = AnimFrames[(animTick / 15) % AnimFrames.Length];

            for (int p = 0; p < Players; p++)
            {
                int col = _color[p];
                if (_flash[p] > 0)
                {
                    _flash[p]--;
                    col = 7;
                }

                int px = _cx[p] * TileSize + (int)_ox[p];
                int py = _cy[p] * TileSize + (int)_oy[p];

                Pal(6, col);
                Spr(frame, px, py, 1, 1, _flipX[p]);
                Pal();
            }

            // Physical coords — same position as virtual, different color (red)
            int s = PixelScale;
            for (int p = 0; p < Players; p++)
            {
                int virtPx = _cx[p] * TileSize + (int)_ox[p];
                int virtPy = _cy[p] * TileSize + (int)_oy[p];
                int physX = virtPx * s;
                int physY = virtPy * s;

                Pal(6, 8); // red
                Spr(frame, physX, physY, 1, 1, _flipX[p], false, 0, CoordMode.Physical);
                Pal();
            }

            Print("virtual", 2, 2, 7);
            Print("physical", 2 * s, 2 * s, 8, CoordMode.Physical);
        }
    }
}

```


## EditMode Tests
This cartridge's structural logic is formally verified in Packages/com.hatiora.pico8.coop/Tests/Editor/CoopCartridgeTests.cs.
