using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Hello
{
    public class HelloCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Hello/hello/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Hello/hello/Music/music")?.text;
        public string MapData   => null;
        public string GffData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Hello/hello/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Hello/hello/Label/label");
        

        public override void Init() => Music(0);

        public override void Update()
        {
        }

        public override void Draw()
        {
            Cls();

            // The underlying VRAM dimensions are determined by the engine configuration (P8.Width/Height).
            // We scale up to simulate the native 128x128 PICO-8 grid within those physical bounds.
            float scale = ContentScale;

            for (int col = 14; col >= 7; col--)
            {
                for (int i = 1; i <= 11; i++)
                {
                    float t1 = Time() * 30f + i * 4f - col * 2f;
                    
                    float x = 8f + i * 8f + Cos(t1 / 90f) * 3f;
                    float y = 38f + (col - 7f) + Cos(t1 / 50f) * 5f;
                    
                    Pal(7, col);
                    
                    int spriteIndex = 16 + i;
                    int sx = (spriteIndex % 16) * 8;
                    int sy = (spriteIndex / 16) * 8;
                    
                    int px = Flr(x);
                    int py = Flr(y);
                    
                    int dx = px * (int)scale;
                    int dy = py * (int)scale;
                    int sw = 8;
                    int sh = 8;
                    int dw = sw * (int)scale;
                    int dh = sh * (int)scale;

                    Sspr(sx, sy, sw, sh, dx, dy, dw, dh);
                    
                    Palt();
                }
            }

            Print("THIS IS PICO-8", 37 * (int)scale, 70 * (int)scale, 14, CoordMode.Virtual, scale);
            Print("NICE TO MEET YOU", 34 * (int)scale, 80 * (int)scale, 12, CoordMode.Virtual, scale);
            
            Sspr((1 % 16) * 8, (1 / 16) * 8, 8, 8, 
                 (64 - 4) * (int)scale, 90 * (int)scale, 
                 8 * (int)scale, 8 * (int)scale);
        }
    }
}

