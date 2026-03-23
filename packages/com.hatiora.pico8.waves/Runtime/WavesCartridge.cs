using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Waves
{
    public class WavesCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData => null;
        public string MusicData => null;
        public string MapData => null;
        public string GffData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Waves/waves/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Waves/waves/Label/label");

        private float r = 64;


        public override void Init() { }

        public override void Update() 
        { 
            
        }
        
        public override void Draw()
        {
            Cls(0);
            
            float scale = ContentScale;

            for (float y = -r; y <= r; y += 3)
            {
                for (float x = -r; x <= r; x += 2)
                {
                    float dist = Sqrt(x * x + y * y);
                    float z = Cos(dist / 40f - Time()) * 6f;
                    
                    if (ContentScale > 1f)
                    {
                        // Stretchy projection over High-Res pixels (simulated Pset filling area footprint = scale squared)
                        int px = (int)((r + x) * scale);
                        int py = (int)((r + y - z) * scale);
                        int fillWidth = Mathf.Max(1, (int)scale);
                        int fillHeight = Mathf.Max(1, (int)scale);
                        
                        Rectfill(px, py, px + fillWidth - 1, py + fillHeight - 1, 6);
                    }
                    else 
                    {
                        // Native 1x1 128px bounding box mapping
                        Pset((int)(r + x), (int)(r + y - z), 6);
                    }
                }
            }
        }
    }
}

