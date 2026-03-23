using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Api
{
    public class ApiCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData   => Resources.Load<TextAsset>("Api/api/Sfx/sfx")?.text;
        public string MusicData => Resources.Load<TextAsset>("Api/api/Music/music")?.text;
        public string MapData   => Resources.Load<TextAsset>("Api/api/Map/map")?.text;
        public string GffData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Api/api/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Api/api/Label/label");

        private string[] tbl = { "\u30D2\u00DF", "\u30B3", "\u25C6" }; // 히゜, コ, ◆

        public override void Init()
        {
            Music(0);
        }

        public override void Update() 
        { 
            // HiRes toggle is handled by the engine via Select button

            if (Btnp(4, -1))
            {
                Sfx(0);
            }
        }
        
        public override void Draw()
        {
            float scale = ContentScale;

            // clear screen to dark blue
            Cls(1);

            // ❎: mess with camera / clipping
            Camera(); // reset
            Clip(); // MUST reset clip mask! Disable any lingering Clip boxes!
            if (Btn(5, -1))
            {
                Camera((int)(Cos(Time() / 6f) * 20f * scale), 0);
                Clip((int)(4 * scale), (int)(16 * scale), (int)(120 * scale), (int)(96 * scale));
            }

            // draw whole map (stars, clouds, background tiles)
            Map(0, 0, 0, 0, 128, 64);

            // circles  x,y,radius,col
            int r63 = (int)(63 * scale);
            int r67 = (int)(67 * scale);
            Circfill((int)(64 * scale), (int)(160 * scale), r63, 6);
            Circ((int)(64 * scale), (int)(160 * scale), r67, 14);

            // with fill pattern
            Fillp(0b0101101001011010); 
            Circfill((int)(64 * scale), (int)(160 * scale), (int)(52 * scale), 7);
            Fillp(); // reset

            // rectangles x0,y0,x1,y1,col
            Rectfill((int)(4 * scale), (int)(4 * scale), (int)(124 * scale) + (int)scale - 1, (int)(10 * scale) + (int)scale - 1, 0);
            Rect((int)(2 * scale), (int)(2 * scale), (int)(126 * scale) + (int)scale - 1, (int)(12 * scale) + (int)scale - 1, 0);

            // lines: x0,y0,x1,y1,col
            for (int i = 1; i <= 15; i++)
            {
                Line((int)((i * 8 - 1) * scale), (int)(6 * scale), (int)((i * 8 + 1) * scale), (int)(8 * scale), i);
            }

            // strings
            int num = 8;
            string str = "HELLO FROM API.P" + num;
            int str_len = str.Length;

            // print: str,x,y,col
            Print(str, (int)((64 - str_len * 2) * scale), (int)(20 * scale), 7, CoordMode.Virtual, scale);

            // tables / arrays iteration
            string[] tbl1 = { "a", "b", "c" }; // simulated add/del logic

            int cy1 = 104;
            foreach (var s in tbl1) 
            {
                Print(s, (int)(2 * scale), (int)(cy1 * scale), 5, CoordMode.Virtual, scale);
                cy1 += 6;
            }

            int cy2 = 104;
            foreach (var s in tbl1) 
            {
                Print(s, (int)(123 * scale), (int)(cy2 * scale), 5, CoordMode.Virtual, scale);
                cy2 += 6;
            }

            for (int i = 0; i < tbl.Length; i++)
            {
                Print(tbl[i], (int)(2 * scale), (int)((10 + (i + 1) * 6) * scale), 13, CoordMode.Virtual, scale);
                Print(tbl[i], (int)(114 * scale), (int)((10 + (i + 1) * 6) * scale), 13, CoordMode.Virtual, scale);
            }

            // draw sprites
            Palt(2, true);
            Palt(0, false);
            // spr(2,48,32,4,4) -> sprite 2 covers 4x4 tiles (32x32px)
            Sspr(16, 0, 32, 32, (int)(48 * scale), (int)(32 * scale), (int)(32 * scale), (int)(32 * scale));

            // stretched sprites
            float w = Cos(Time() / 2f) * 32f;
            int absW = (int)Mathf.Abs(w);
            bool flip = w < 0;

            if (absW > 0)
            {
                // draw back sides indigo
                if (flip) Pal(7, 13);

                // horizontal spinning bunny
                int hdw = (int)(absW * scale);
                int hdh = (int)(32f * scale);
                Sspr(16, 0, 32, 32, (int)((24f - absW / 2f) * scale), (int)(32 * scale), hdw, hdh, flipX: flip);

                // vertical spinning bunny
                int vdw = (int)(32f * scale);
                int vdh = (int)(absW * scale);
                Sspr(16, 0, 32, 32, (int)(88 * scale), (int)((48f - absW / 2f) * scale), vdw, vdh, flipY: flip);
            }

            Pal(); // reset palette

            // rotating star sprites
            for (int i = 0; i <= 31; i++)
            {
                float a = (i + Time() * 2f) / 32f;
                float rad = a * Mathf.PI * 2f;
                
                float sx = 64f + Mathf.Cos(rad) * 57f - 4f;
                float sy = 160f + -Mathf.Sin(rad) * 57f - 4f;

                int ssx = 64 + i % 16;
                int col = Sget(ssx, 0);

                Pal(7, col);
                Sspr(0, 8, 8, 8, (int)(sx * scale), (int)(sy * scale), (int)(8 * scale), (int)(8 * scale));
            }
            Pal(); // reset

            // draw state of buttons
            for (int pl = 0; pl <= 7; pl++)
            {
                for (int b = 0; b <= 7; b++)
                {
                    int sx = (int)((57 + b * 2) * scale);
                    int sy = (int)((70 + pl * 2) * scale);
                    int col = 5;
                    
                    // The user requested the primary inputs (Player 0) highlight on the 2nd row
                    int pIdx = pl == 0 ? 7 : pl - 1; 
                    if (Btn(b, pIdx)) col = b + 7;
                    
                    int fillSize = Mathf.Max(1, (int)scale);
                    Rectfill(sx, sy, sx + fillSize - 1, sy + fillSize - 1, col);
                }
            }
        }
    }
}

