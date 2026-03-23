using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Automata
{
    /// <summary>
    /// 1-D cellular automata demo by zep.
    /// Scrolls the screen up one pixel each frame via Memcpy, then generates
    /// a new bottom row using a 3-neighbor rule that changes every 16 lines.
    /// </summary>
    public class AutomataCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Automata/automata/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Automata/automata/Music/music")?.text;
        public string MapData   => null;
        public string GffData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Automata/automata/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Automata/automata/Label/label");

        private int _l;             // line counter
        private int[] _r;           // rule set (8 entries, index 0–7)
        private float _lastTime;    // for dt-based timing
        private float _frameAcc;    // accumulator for 30fps ticks
        private bool _ticked;       // whether a game tick occurred this frame

        // Virtual grid width/height (128 in native, P8.Width/Height in high-res)
        private int W => (int)(128 * ContentScale);
        private int H => (int)(128 * ContentScale);
        private float Scale => ContentScale;

        public override void Init()
        {
            Cls();
            _l = 0;

            // starting rule set: {0,1,0,1,1,0,0,1}
            _r = new int[] { 0, 1, 0, 1, 1, 0, 0, 1 };
        }

        public override void Update()
        {
            float now = Time();
            float dt = now - _lastTime;
            _lastTime = now;
            if (dt <= 0 || dt > 0.5f) dt = 1f / 30f;
            _frameAcc += dt;
            _ticked = _frameAcc >= 1f / 30f;
            if (_ticked) _frameAcc -= 1f / 30f;
            if (!_ticked) return;

            _l++;

            // change rule every 16 lines or when ❎ is pressed
            if (_l % 16 == 0 || Btnp(4, -1))
            {
                for (int i = 1; i <= 7; i++)
                {
                    _r[i] = Flr(Rnd(2.3f));
                }
            }

            // if the bottom line is blank, seed it
            bool found = false;
            for (int x = 0; x < W; x++)
            {
                if (Pget(x, H - 1) > 0) { found = true; break; }
            }

            if (!found)
            {
                Pset(W / 2 - 1, H - 1, 7);
            }
        }

        public override void Draw()
        {
            if (!_ticked) return; // Only scroll+generate on game ticks
            int w = W;
            int h = H;

            if (ContentScale > 1f)
            {
                // High-res: scroll via Memcpy on screen memory
                int bytesPerRow = P8.Width / 2; // nibble-packed row width
                int screenAddr = 0x6000;
                Memcpy(screenAddr, screenAddr + bytesPerRow, bytesPerRow * (P8.Height - 1));
            }
            else
            {
                // Native 128×128: scroll via Pget/Pset loop
                float s = Scale;
                int fillW = Mathf.Max(1, (int)s);
                int fillH = Mathf.Max(1, (int)s);

                for (int y = 0; y < h - 1; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int col = Pget(x, y + 1);
                        if (ContentScale > 1f)
                        {
                            int px = x * (int)s;
                            int py = y * (int)s;
                            Rectfill(px, py, px + fillW - 1, py + fillH - 1, col);
                        }
                        else
                        {
                            Pset(x, y, col);
                        }
                    }
                }
            }

            // Compute new bottom row from 3-neighbor cellular automata rule
            int bottomRow = h - 1;
            int neighborRow = h - 2;
            float scale = Scale;

            for (int x = 0; x < w; x++)
            {
                int n = 0;
                for (int b = 0; b <= 2; b++)
                {
                    int nx = x - 1 + b;
                    if (nx >= 0 && nx < w && Pget(nx, neighborRow) > 0)
                    {
                        n += 1 << b; // 2^b
                    }
                }

                int col = _r[n] * 7;
                if (ContentScale > 1f)
                {
                    Pset(x, bottomRow, col);
                }
                else
                {
                    Pset(x, bottomRow, col);
                }
            }
        }
    }
}

