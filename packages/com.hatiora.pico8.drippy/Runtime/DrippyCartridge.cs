using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Drippy
{
    public class DrippyCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Drippy/drippy/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Drippy/drippy/Music/music")?.text;
        public string MapData => null;
        public string GffData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Drippy/drippy/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Drippy/drippy/Label/label");

        private float x = 64f;
        private float y = 64f;
        private float c = 8f;
        private float _lastTime;
        private float _frameAcc;

        
        public override void Init()
        {
            float scale = ContentScale;
            Rectfill(0, 0, P8.Width - 1, P8.Height - 1, 0); // Base wipe
            Rectfill(0, 0, (int)(128 * scale) - 1, (int)(128 * scale) - 1, 1);
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

            float scale = ContentScale;

            if (Btn(0, -1)) x -= 1f;
            if (Btn(1, -1)) x += 1f;
            if (Btn(2, -1)) y -= 1f;
            if (Btn(3, -1)) y += 1f;

            c += 1f / 8f;
            if (c >= 16f) c = 8f;

            // Drip algorithm operates on the exact 128x128 logical grid to perfectly map the density.
            int drips = 800;

            for (int i = 0; i < drips; i++)
            {
                // Select a virtual coordinate explicitly on the 128 space
                int vx = (int)Rnd(128f);
                int vy = (int)Rnd(128f);
                
                // Map the virtual coordinate directly to the chunky visual scale grid
                int px = (int)(vx * scale);
                int py = (int)(vy * scale);
                
                int col = Pget(px, py);

                if (col > 1)
                {
                    // Move the block DOWN by exactly one Virtual unit (which is exactly `scale` physical pixels)
                    int fillSize = Mathf.Max(1, (int)scale);
                    Rectfill(px, py + fillSize, px + fillSize - 1, py + fillSize * 2 - 1, col); 
                }
            }
        }

        public override void Draw()
        {
            float scale = ContentScale;
            
            // Draw the actual block size to match scale
            int px = (int)(x * scale);
            int py = (int)(y * scale);
            int fillSize = Mathf.Max(1, (int)scale);
            Rectfill(px, py, px + fillSize - 1, py + fillSize - 1, (int)c); 
        }
    }
}

