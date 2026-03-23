namespace Hatiora.Pico8
{
    /// <summary>
    /// Base class for PICO-8 cartridges in the new architecture.
    /// Subclasses override Init/Update/Draw and use <c>P8.*</c> for all API calls.
    /// </summary>
    public abstract class Cartridge
    {
        /// <summary>The engine spec this cartridge was designed for. Null = use default.</summary>
        public virtual EngineSpec Spec => null;

        /// <summary>The PICO-8 API facade. Set by the builder before Init.</summary>
        protected IPico8 P8 { get; private set; }

        /// <summary>The resolved engine spec at runtime. Set by the builder before Init.</summary>
        protected EngineSpec RuntimeSpec { get; private set; }

        /// <summary>Event emitted by the cartridge when it requests the host to load a different cartridge.</summary>
        public System.Action<string> OnLoadCartridge { get; set; }

        /// <summary>Called by the builder to wire the API before init.</summary>
        public void Bind(IPico8 api, EngineSpec spec)
        {
            P8 = api;
            RuntimeSpec = spec ?? EngineSpec.Pico8;
        }

        /// <summary>Called once when the cartridge starts.</summary>
        public abstract void Init();

        /// <summary>Called once per frame for game logic.</summary>
        public abstract void Update();

        /// <summary>Called once per frame for rendering.</summary>
        public abstract void Draw();

        // ─── Convenience methods (delegate to P8) ───

        protected void Cls(int col = 0) => P8.Cls(col);
        protected void Pset(int x, int y, int col, CoordMode mode = CoordMode.Virtual) => P8.Pset(x, y, col, mode);
        protected int Pget(int x, int y, CoordMode mode = CoordMode.Virtual) => P8.Pget(x, y, mode);
        protected void Line(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual) => P8.Line(x0, y0, x1, y1, col, mode);
        protected void Rect(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual) => P8.Rect(x0, y0, x1, y1, col, mode);
        protected void Rectfill(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual) => P8.Rectfill(x0, y0, x1, y1, col, mode);
        protected void Circ(int x, int y, int r, int col, CoordMode mode = CoordMode.Virtual) => P8.Circ(x, y, r, col, mode);
        protected void Circfill(int x, int y, int r, int col, CoordMode mode = CoordMode.Virtual) => P8.Circfill(x, y, r, col, mode);
        protected void Oval(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual) => P8.Oval(x0, y0, x1, y1, col, mode);
        protected void Ovalfill(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual) => P8.Ovalfill(x0, y0, x1, y1, col, mode);
        protected void Spr(int n, int x, int y, int w = 1, int h = 1, bool flipX = false, bool flipY = false, int bank = 0, CoordMode mode = CoordMode.Virtual) => P8.Spr(n, x, y, w, h, flipX, flipY, bank, mode);
        protected void Sspr(int sx, int sy, int sw, int sh, int dx, int dy, int dw = -1, int dh = -1, bool flipX = false, bool flipY = false, CoordMode mode = CoordMode.Virtual, float angle = 0f) => P8.Sspr(sx, sy, sw, sh, dx, dy, dw, dh, flipX, flipY, mode, angle);
        protected int Print(string str, int x, int y, int col, CoordMode mode = CoordMode.Virtual, float scale = 1f) => P8.Print(str, x, y, col, mode, scale);
        protected void Camera(int x = 0, int y = 0) => P8.Camera(x, y);
        protected void Clip(int x = 0, int y = 0, int w = -1, int h = -1) => P8.Clip(x, y, w, h);
        protected void Pal(int c0, int c1, int p = 0) => P8.Pal(c0, c1, p);
        protected void Pal() => P8.Pal();
        protected void Palt(int c, bool t) => P8.Palt(c, t);
        protected void Palt() => P8.Palt();
        protected void Fillp(int p = 0) => P8.Fillp(p);
        protected void Color(int col) => P8.Color(col);
        protected void Cursor(int x, int y, int col = -1) => P8.Cursor(x, y, col);
        protected int Sget(int x, int y) => P8.Sget(x, y);
        protected void Sset(int x, int y, int col) => P8.Sset(x, y, col);

        protected int Mget(int x, int y) => P8.Mget(x, y);
        protected void Mset(int x, int y, int val) => P8.Mset(x, y, val);
        protected void Map(int tileX = 0, int tileY = 0, int sx = 0, int sy = 0, int tileW = -1, int tileH = -1, int layers = 0, CoordMode mode = CoordMode.Virtual, int scale = 1) => P8.Map(tileX, tileY, sx, sy, tileW, tileH, layers, mode, scale);
        protected int Fget(int n, int f = -1) => P8.Fget(n, f);
        protected void Fset(int n, int f, bool val) => P8.Fset(n, f, val);

        protected bool Btn(int i, int p = 0) => P8.Btn(i, p);
        protected bool Btnp(int i, int p = 0) => P8.Btnp(i, p);

        protected void Sfx(int n, int channel = -1, int offset = 0, int length = 32) => P8.Sfx(n, channel, offset, length);
        protected void Music(int n, int fadeLen = 0, int channelMask = 0) => P8.Music(n, fadeLen, channelMask);

        protected float Rnd(float x = 1) => P8.Rnd(x);
        protected int Flr(float x) => P8.Flr(x);
        protected int Ceil(float x) => P8.Ceil(x);
        protected float Sin(float x) => P8.Sin(x);
        protected float Cos(float x) => P8.Cos(x);
        protected float Atan2(float dx, float dy) => P8.Atan2(dx, dy);
        protected float Sqrt(float x) => P8.Sqrt(x);
        protected float Abs(float x) => P8.Abs(x);
        protected float Sgn(float x) => P8.Sgn(x);
        protected float Min(float x, float y) => P8.Min(x, y);
        protected float Max(float x, float y) => P8.Max(x, y);
        protected float Mid(float x, float y, float z) => P8.Mid(x, y, z);
        protected void Srand(int x) => P8.Srand(x);

        protected int Shl(int x, int n) => P8.Shl(x, n);
        protected int Shr(int x, int n) => P8.Shr(x, n);
        protected int Bor(int x, int y) => P8.Bor(x, y);
        protected int Band(int x, int y) => P8.Band(x, y);
        protected int Bxor(int x, int y) => P8.Bxor(x, y);
        protected int Bnot(int x) => P8.Bnot(x);

        protected int Peek(int addr) => P8.Peek(addr);
        protected void Poke(int addr, int val) => P8.Poke(addr, val);
        protected void Memcpy(int dest, int src, int len) => P8.Memcpy(dest, src, len);
        protected void Memset(int dest, int val, int len) => P8.Memset(dest, val, len);

        protected float Time() => P8.Time();
        protected float Stat(int id) => P8.Stat(id);
        protected void Reload() => P8.Reload();

        protected void MenuItem(int index, string label = null, System.Action callback = null) 
            => P8.MenuItem(index, label, callback);
        protected int PhysW => P8.PhysicalWidth;
        protected int PhysH => P8.PhysicalHeight;
        protected int PixelScale => P8.Scale;
        protected float ContentScale => P8.ContentScale;
    }
}
