using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Bounce
{
    /// <summary>
    /// Bouncy ball demo by zep.
    /// A ball bounces around with gravity, wall/floor bouncing, and dampening.
    /// Press ❎ (button 5) to randomly bump the ball. Uses _update60.
    /// </summary>
    public class BounceCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Bounce/bounce/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Bounce/bounce/Music/music")?.text;
        public string MapData   => null;
        public string GffData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Bounce/bounce/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Bounce/bounce/Label/label");


        // Ball state
        private float _ballx, _bally;
        private float _velx, _vely;
        private const int Size = 10;
        private const int FloorY = 100;
        private float _lastTime;
        private float _frameAcc;
        private bool _pendingBump;

        public override void Init()
        {
            _ballx = 64;
            _bally = Size;

            // Starting velocity
            _velx = Rnd(6f) - 3f;
            _vely = Rnd(6f) - 3f;
        }

        public override void Update()
        {
            // Latch input BEFORE accumulator — captures presses on non-tick frames
            if (Btnp(5, -1)) _pendingBump = true;

            // dt-based timing: 30fps
            float now = Time();
            float dt = now - _lastTime;
            _lastTime = now;
            if (dt <= 0 || dt > 0.5f) dt = 1f / 30f;
            _frameAcc += dt;
            if (_frameAcc < 1f / 30f) return;
            _frameAcc -= 1f / 30f;

            // Move ball left/right
            if (_ballx + _velx < 0 + Size ||
                _ballx + _velx > 128 - Size)
            {
                // Bounce on side!
                _velx *= -1;
                Sfx(1);
            }
            else
            {
                // Move by x velocity
                _ballx += _velx;
            }

            // Move ball up/down
            if (_bally + _vely < 0 + Size ||
                _bally + _vely > FloorY - Size)
            {
                // Bounce on floor/ceiling
                _vely = _vely * -0.9f;
                Sfx(0);

                // If bounce was too small, bump into air
                if (_vely < 0 && _vely > -0.5f)
                {
                    _velx = Rnd(6f) - 3f;
                    _vely = -Rnd(5f) - 4f;
                    Sfx(3);
                }
            }
            else
            {
                _bally += _vely;
            }

            // Gravity!
            _vely += 0.2f;

            // Press ❎ to randomly choose a new velocity
            if (_pendingBump)
            {
                _pendingBump = false;
                _velx = Rnd(6f) - 3f;
                _vely = Rnd(6f) - 8f;
                Sfx(2);
            }
        }

        public override void Draw()
        {
            float scale = ContentScale;
            int s = Mathf.Max(1, (int)scale);

            Cls(1);

            // "press ❎ to bump"
            Print("press \u00D7 to bump", 32 * s, 10 * s, 6, CoordMode.Virtual, scale);

            // Dithered floor
            Fillp(0b0101101001011010); // ░ pattern
            Rectfill(0, FloorY * s, 128 * s - 1, 128 * s - 1, 12);
            Fillp(); // Reset

            // Ball (filled circle)
            Circfill((int)(_ballx * s), (int)(_bally * s), Size * s, 14);

            // Sprite trail (sprite 1, scaled to match resolution)
            int sprX = (int)((_ballx - 4 - _velx) * s);
            int sprY = (int)((_bally - 4 - _vely) * s);
            // Source: sprite 1 = sheet coords (8,0), size 8x8
            Sspr(8, 0, 8, 8, sprX, sprY, 8 * s, 8 * s);
        }
    }
}

