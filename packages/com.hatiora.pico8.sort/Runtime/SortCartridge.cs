using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Sort
{
    public class SortCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Sort/sort/Sfx/sfx")?.text;
        public string MusicData => null; // sort.p8 doesn't have music
        public string MapData   => null;
        public string GffData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Sort/sort/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Sort/sort/Label/label");
        
        private int[] g;

        public override void Init()
        {
            // starting giraffe heights
            g = new int[] { 3, 5, 7, 2, 9, 1, 2 };
        }

        public override void Update()
        {

            // ❎ to sort
            if (Btnp(5, -1))
            {
                // look for a pair of giraffees out of order
                for (int i = 0; i < 6; i++)
                {
                    if (g[i] > g[i + 1])
                    {
                        // the left one is taller, so swap them!
                        int temp = g[i];
                        g[i] = g[i + 1];
                        g[i + 1] = temp;
                        
                        Sfx(0);
                        
                        // just one swap for now!
                        break;
                    }
                }
            }
            
            // 🅾️ to randomize
            if (Btnp(4, -1))
            {
                for (int i = 0; i < 7; i++)
                {
                    g[i] = Flr(Rnd(9));
                }
                
                Sfx(1);
            }
        }

        public override void Draw()
        {
            Cls(12); // PICO-8 pale blue background
            
            float scale = ContentScale;

            // Optional: Support the 128x128 bounding box
            int sw = (int)(128 * scale);
            int sh = (int)(128 * scale);
            
            // Replicating rectfill(0,110,127,127,14) -> green ground
            Rectfill(0, (int)(110 * scale), (int)(128 * scale) - 1, (int)(128 * scale) - 1, 14);
            
            Print("PRESS \u00CE TO RANDOMIZE", (int)(22 * scale), (int)(2 * scale), 7, CoordMode.Virtual, scale);
            Print("PRESS \u00D7 TO SORT", (int)(32 * scale), (int)(10 * scale), 7, CoordMode.Virtual, scale);

            for (int i = 0; i < 7; i++)
            {
                DrawGiraffe((i + 1) * 16, 110, g[i], scale);
            }
        }

        // draw a giraffe at x,y with neck length of l
        private void DrawGiraffe(int x, int y, int l, float scale)
        {
            int sw = 8;
            int sh = 8;
            int dw = sw * (int)scale;
            int dh = sh * (int)scale;
            int dx, dy;

            // body is 2 tiles wide!
            sw = 16;
            dw = sw * (int)scale;
            dx = (x - 8) * (int)scale;
            dy = (y - 8) * (int)scale;
            Sspr((33 % 16) * 8, (33 / 16) * 8, sw, sh, dx, dy, dw, dh);
            
            // reset sw and dw for 1 tile wide neck and head
            sw = 8;
            dw = sw * (int)scale;
            
            // neck for l segments
            for (int i = 1; i <= l; i++)
            {
                dx = x * (int)scale;
                dy = (y - 8 - i * 8) * (int)scale;
                Sspr((18 % 16) * 8, (18 / 16) * 8, sw, sh, dx, dy, dw, dh);
            }
            
            // put head on top
            dx = x * (int)scale;
            dy = (y - 16 - l * 8) * (int)scale;
            Sspr((2 % 16) * 8, (2 / 16) * 8, sw, sh, dx, dy, dw, dh);
        }
    }
}

