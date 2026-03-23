using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Basic
{
    public class BasicCartridge : Cartridge, IUnityCartridge
    {
        private float _x;
        private float _y;
        private float _lastTime;

        public string SfxData   => Resources.Load<TextAsset>("Basic/basic/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Basic/basic/Music/music")?.text;
        public string MapData   => null;
        public string GffData => null;

        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Basic/basic/Gfx/gfx");
        public Texture2D LabelTexture => null;
        
        public override void Init()
        {
            _x = P8.Width / 2 - 4;  // center sprite (8px wide)
            _y = P8.Height / 2 - 4; // center sprite (8px tall)
            _lastTime = Time();
            Music(0);
        }

        public override void Update()
        {
            float t = Time();
            float dt = t - _lastTime;
            _lastTime = t;

            float speed = 60f * dt; // 60 px/sec
            if (Btn(0, -1)) _x -= speed;
            if (Btn(1, -1)) _x += speed;
            if (Btn(2, -1)) _y -= speed;
            if (Btn(3, -1)) _y += speed;
        }

        public override void Draw()
        {
            Cls(1);
            // Heartbeat: ~72 BPM, snappy 8-bit pulse (snap between sizes)
            float pulse = Sin(Time() * 1.2f);
            int size = pulse > 0.2f ? 12 : 8; // snap, not smooth
            // Blink white on the beat
            if (pulse > 0.2f)
            {
                for (int c = 1; c < 16; c++) Pal(c, 7);
            }
            int cx = Flr(_x) + 4 - size / 2; // keep centered
            int cy = Flr(_y) + 4 - size / 2;
            Sspr(8, 0, 8, 8, cx, cy, size, size);
            Pal();
        }
    }
}
