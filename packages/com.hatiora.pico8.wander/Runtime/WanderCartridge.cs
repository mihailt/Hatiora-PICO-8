using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Wander
{
    public class WanderCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Wander/wander/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Wander/wander/Music/music")?.text;
        public string MapData   => Resources.Load<TextAsset>("Wander/wander/Map/map")?.text;
        public string GffData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Wander/wander/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Wander/wander/Label/label");
        

        // State (matching Lua globals)
        private float _x, _y;   // position (in tiles)
        private float _dx, _dy; // velocity
        private float _f;       // animation frame
        private int _d;         // direction (-1 or 1)
        private float _lastTime;
        private float _frameAcc;

        public override void Init()
        {
            _x = 24; _y = 24;
            _dx = 0; _dy = 0;
            _f = 0;
            _d = 1;
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

            float ac = 0.1f; // acceleration

            if (Btn(0)) { _dx -= ac; _d = -1; } // left
            if (Btn(1)) { _dx += ac; _d =  1; } // right
            if (Btn(2)) { _dy -= ac; }           // up
            if (Btn(3)) { _dy += ac; }           // down

            // Move (add velocity)
            _x += _dx; _y += _dy;

            // Friction (lower for more sliding)
            _dx *= 0.7f;
            _dy *= 0.7f;

            // Advance animation according to speed
            // (or reset when standing almost still)
            float spd = Sqrt(_dx * _dx + _dy * _dy);
            _f = (_f + spd * 2) % 4; // 4 frames
            if (spd < 0.05f) _f = 0;

            // Collect apple
            int tx = Flr(_x), ty = Flr(_y);
            if (Mget(tx, ty) == 10)
            {
                Mset(tx, ty, 14);
                Sfx(0);
            }
        }

        public override void Draw()
        {
            float scale = ContentScale;
            int s = Mathf.Max(1, (int)scale);

            Cls(1); // dark blue background

            // Move camera to current room
            int roomX = Flr(_x / 16);
            int roomY = Flr(_y / 16);
            Camera(roomX * 128 * s, roomY * 128 * s);

            // Draw the whole map (128x32 tiles)
            Map(0, 0, 0, 0, 128, 32, 0, CoordMode.Virtual, s);

            // Draw the player
            int fr = 1 + Flr(_f); // frame index (sprites 1-4)
            int px = Flr(_x * 8) * s - 4 * s;
            int py = Flr(_y * 8) * s - 4 * s;
            int sprSx = (fr % 16) * 8;
            int sprSy = (fr / 16) * 8;
            Sspr(sprSx, sprSy, 8, 8, px, py, 8 * s, 8 * s, _d == -1);

            // Reset camera
            Camera();
        }
    }
}

