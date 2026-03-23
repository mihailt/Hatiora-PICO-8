namespace Hatiora.Pico8
{
    /// <summary>
    /// The complete PICO-8 API. This is what game code touches through <see cref="Cartridge"/>.
    /// Composed from subsystem interfaces internally by <see cref="Pico8Api"/>.
    /// </summary>
    public interface IPico8
    {
        // ─── Screen info ───
        int Width { get; }
        int Height { get; }
        int PhysicalWidth { get; }
        int PhysicalHeight { get; }
        int Scale { get; }
        float ContentScale { get; }

        // ─── Graphics ───
        void Cls(int col = 0);
        void Pset(int x, int y, int col, CoordMode mode = CoordMode.Virtual);
        int  Pget(int x, int y, CoordMode mode = CoordMode.Virtual);
        void Line(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual);
        void Rect(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual);
        void Rectfill(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual);
        void Circ(int x, int y, int r, int col, CoordMode mode = CoordMode.Virtual);
        void Circfill(int x, int y, int r, int col, CoordMode mode = CoordMode.Virtual);
        void Oval(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual);
        void Ovalfill(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual);
        void Spr(int n, int x, int y, int w = 1, int h = 1, bool flipX = false, bool flipY = false, int bank = 0, CoordMode mode = CoordMode.Virtual);
        void Sspr(int sx, int sy, int sw, int sh, int dx, int dy, int dw = -1, int dh = -1, bool flipX = false, bool flipY = false, CoordMode mode = CoordMode.Virtual, float angle = 0f);
        int  Print(string str, int x, int y, int col, CoordMode mode = CoordMode.Virtual, float scale = 1f);
        void Camera(int x = 0, int y = 0);
        void Clip(int x = 0, int y = 0, int w = -1, int h = -1);
        void Pal(int c0, int c1, int p = 0);
        void Pal();
        void Palt(int c, bool t);
        void Palt();
        void Fillp(int p = 0);
        void Color(int col);
        void Cursor(int x, int y, int col = -1);
        int  Sget(int x, int y);
        void Sset(int x, int y, int col);

        // ─── Map ───
        int  Mget(int x, int y);
        void Mset(int x, int y, int val);
        void Map(int tileX = 0, int tileY = 0, int sx = 0, int sy = 0, int tileW = -1, int tileH = -1, int layers = 0, CoordMode mode = CoordMode.Virtual, int scale = 1);
        int  Fget(int n, int f = -1);
        void Fset(int n, int f, bool val);

        // ─── Input ───
        bool Btn(int i, int p = 0);
        bool Btnp(int i, int p = 0);

        // ─── Audio ───
        void Sfx(int n, int channel = -1, int offset = 0, int length = 32);
        void Music(int n, int fadeLen = 0, int channelMask = 0);

        // ─── Math ───
        float Rnd(float x = 1);
        int   Flr(float x);
        int   Ceil(float x);
        float Sin(float x);
        float Cos(float x);
        float Atan2(float dx, float dy);
        float Sqrt(float x);
        float Abs(float x);
        float Sgn(float x);
        float Min(float x, float y);
        float Max(float x, float y);
        float Mid(float x, float y, float z);
        void  Srand(int x);

        // ─── Bitwise ───
        int Shl(int x, int n);
        int Shr(int x, int n);
        int Bor(int x, int y);
        int Band(int x, int y);
        int Bxor(int x, int y);
        int Bnot(int x);

        // ─── Memory ───
        int  Peek(int addr);
        void Poke(int addr, int val);
        void Memcpy(int dest, int src, int len);
        void Memset(int dest, int val, int len);

        // ─── System ───
        float Time();

        /// <summary>Returns system stat. id=1: CPU fraction (0..1) of frame budget used by Update+Draw.</summary>
        float Stat(int id);


        /// <summary>Reloads cartridge ROM data (map + flags) to their original loaded state.</summary>
        void Reload();

        // ─── System Menu ───
        
        /// <summary>Adds or removes a custom menu item from the system pause menu.</summary>
        void MenuItem(int index, string label = null, System.Action callback = null);
    }
}
