using UnityEngine;
using Hatiora.Pico8.Unity;

namespace Hatiora.Pico8.Dots3D
{
    public class Dots3DCartridge : Cartridge, IUnityCartridge
    {
        public override EngineSpec Spec => null; 
        
        public string SfxData => null;
        public string MusicData => null;
        public string MapData => null;
        public string GffData => null;
        public Texture2D GfxTexture   => Resources.Load<Texture2D>("Dots3D/dots3d/Gfx/gfx");
        public Texture2D LabelTexture => Resources.Load<Texture2D>("Dots3D/dots3d/Label/label");

        private class Point
        {
            public float X, Y, Z;
            public float Cx, Cy, Cz;
            public int Col;
        }

        private System.Collections.Generic.List<Point> _pt;

        private int LuaMod(float a, int b)
        {
            int ia = Mathf.FloorToInt(a);
            int m = ia % b;
            return m < 0 ? m + b : m;
        }

        public override void Init()
        {
            _pt = new System.Collections.Generic.List<Point>();
            for (float y = -1f; y <= 1.01f; y += 1f / 3f)
            {
                for (float x = -1f; x <= 1.01f; x += 1f / 3f)
                {
                    for (float z = -1f; z <= 1.01f; z += 1f / 3f)
                    {
                        var p = new Point { X = x, Y = y, Z = z };
                        p.Col = 8 + LuaMod(x * 2f + y * 3f, 8);
                        _pt.Add(p);
                    }
                }
            }
        }

        public override void Update() 
        { 
        }
        
        private (float x, float y) Rot(float x, float y, float a)
        {
            float x0 = x;
            float rx = Cos(a) * x - Sin(a) * y;
            float ry = Cos(a) * y + Sin(a) * x0; // *x0 matches lua "*x is wrong but kinda nice too"
            return (rx, ry);
        }

        public override void Draw()
        {
            Cls(0);
            
            float t = Time();
            float scale = ContentScale;

            foreach (var p in _pt)
            {
                // transform world space -> camera space
                var rot1 = Rot(p.X, p.Z, t / 8f);
                p.Cx = rot1.x;
                p.Cz = rot1.y;

                var rot2 = Rot(p.Y, p.Cz, t / 7f);
                p.Cy = rot2.x;
                p.Cz = rot2.y;

                p.Cz += 2f + Cos(t / 6f);
            }

            // sort furthest -> closest
            for (int pass = 1; pass <= 4; pass++)
            {
                for (int i = 0; i < _pt.Count - 1; i++)
                {
                    if (_pt[i].Cz < _pt[i + 1].Cz)
                    {
                        var temp = _pt[i];
                        _pt[i] = _pt[i + 1];
                        _pt[i + 1] = temp;
                    }
                }
                for (int i = _pt.Count - 2; i >= 0; i--)
                {
                    if (_pt[i].Cz < _pt[i + 1].Cz)
                    {
                        var temp = _pt[i];
                        _pt[i] = _pt[i + 1];
                        _pt[i + 1] = temp;
                    }
                }
            }

            float rad1 = 5f + Cos(t / 4f) * 4f;

            foreach (var p in _pt)
            {
                // transform camera space -> screen space
                float sx = 64f + p.Cx * 64f / p.Cz;
                float sy = 64f + p.Cy * 64f / p.Cz;
                float rad = rad1 / p.Cz;

                if (p.Cz > 0.1f)
                {
                    if (ContentScale > 1f)
                    {
                        Circfill((int)(sx * scale), (int)(sy * scale), (int)(rad * scale), p.Col);
                        Circfill((int)((sx + rad / 3f) * scale), (int)((sy - rad / 3f) * scale), Mathf.Max(1, (int)((rad / 3f) * scale)), 7);
                    }
                    else
                    {
                        Circfill((int)sx, (int)sy, (int)rad, p.Col);
                        Circfill((int)(sx + rad / 3f), (int)(sy - rad / 3f), Mathf.Max(1, (int)(rad / 3f)), 7);
                    }
                }
            }
        }
    }
}

