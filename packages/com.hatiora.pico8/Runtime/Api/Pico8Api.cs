using System;
using System.Diagnostics;

namespace Hatiora.Pico8
{
    /// <summary>
    /// The central PICO-8 API facade. Composes all subsystem interfaces.
    /// This is what <see cref="Cartridge"/> delegates to via <c>P8.*</c>.
    /// </summary>
    public sealed class Pico8Api : IPico8
    {
        public EngineSpec Spec { get; }

        private readonly Pico8Memory _mem;
        private readonly DrawState _state;
        private readonly Palette _palette;
        private readonly IGraphics _gfx;
        private readonly ISpriteStore _sprites;
        private readonly IMap _map;
        private readonly IAudio _audio;
        public IAudio Audio => _audio;
        private readonly IInput _input;
        private readonly Pico8Math _math;
        private readonly ILogger _log;
        private readonly Stopwatch _timer;

        public int Width  => Spec.ScreenWidth;
        public int Height => Spec.ScreenHeight;
        public int PhysicalWidth  => Spec.EffectivePhysW;
        public int PhysicalHeight => Spec.EffectivePhysH;
        public int Scale => Spec.Scale;
        public bool IsHighRes { get; set; }
        public float ContentScale => IsHighRes ? Spec.ContentScale : 1f;

        public class CustomMenuItem
        {
            public string Label;
            public System.Action Callback;
        }

        public CustomMenuItem[] CustomMenuItems { get; } = new CustomMenuItem[5];

        public Pico8Api(
            EngineSpec spec,
            Pico8Memory mem,
            DrawState state,
            Palette palette,
            IGraphics gfx,
            ISpriteStore sprites,
            IMap map,
            IAudio audio,
            IInput input,
            ILogger log = null)
        {
            Spec     = spec ?? throw new ArgumentNullException(nameof(spec));
            _mem     = mem ?? throw new ArgumentNullException(nameof(mem));
            _state   = state ?? throw new ArgumentNullException(nameof(state));
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _gfx     = gfx ?? throw new ArgumentNullException(nameof(gfx));
            _sprites = sprites ?? throw new ArgumentNullException(nameof(sprites));
            _map     = map ?? throw new ArgumentNullException(nameof(map));
            _audio   = audio ?? throw new ArgumentNullException(nameof(audio));
            _input   = input ?? throw new ArgumentNullException(nameof(input));
            _log     = log;
            _math    = new Pico8Math();
            _timer   = Stopwatch.StartNew();
        }

        // ─── Graphics ───

        public void Cls(int col = 0) => _gfx.Clear((byte)(col % Spec.PaletteSize));

        public void Pset(int x, int y, int col, CoordMode mode = CoordMode.Virtual)
            => _gfx.SetPixel(x, y, (byte)(col % Spec.PaletteSize), mode);

        public int Pget(int x, int y, CoordMode mode = CoordMode.Virtual)
            => _gfx.GetPixel(x, y, mode);

        public void Line(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual)
            => _gfx.DrawLine(x0, y0, x1, y1, (byte)(col % Spec.PaletteSize), mode);

        public void Rect(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual)
            => _gfx.DrawRect(x0, y0, x1, y1, (byte)(col % Spec.PaletteSize), false, mode);

        public void Rectfill(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual)
            => _gfx.DrawRect(x0, y0, x1, y1, (byte)(col % Spec.PaletteSize), true, mode);

        public void Circ(int x, int y, int r, int col, CoordMode mode = CoordMode.Virtual)
            => _gfx.DrawCircle(x, y, r, (byte)(col % Spec.PaletteSize), false, mode);

        public void Circfill(int x, int y, int r, int col, CoordMode mode = CoordMode.Virtual)
            => _gfx.DrawCircle(x, y, r, (byte)(col % Spec.PaletteSize), true, mode);

        public void Oval(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual)
            => _gfx.DrawOval(x0, y0, x1, y1, (byte)(col % Spec.PaletteSize), false, mode);

        public void Ovalfill(int x0, int y0, int x1, int y1, int col, CoordMode mode = CoordMode.Virtual)
            => _gfx.DrawOval(x0, y0, x1, y1, (byte)(col % Spec.PaletteSize), true, mode);

        public void Spr(int n, int x, int y, int w = 1, int h = 1,
            bool flipX = false, bool flipY = false, int bank = 0,
            CoordMode mode = CoordMode.Virtual)
            => _gfx.DrawSprite(bank, n, x, y, w, h, flipX, flipY, mode);

        public void Sspr(int sx, int sy, int sw, int sh,
            int dx, int dy, int dw = -1, int dh = -1,
            bool flipX = false, bool flipY = false,
            CoordMode mode = CoordMode.Virtual, float angle = 0f)
            => _gfx.DrawSpriteStretch(sx, sy, sw, sh, dx, dy,
                dw < 0 ? sw : dw, dh < 0 ? sh : dh, flipX, flipY, mode, angle);

        public int Print(string str, int x, int y, int col, CoordMode mode = CoordMode.Virtual, float scale = 1f)
            => _gfx.DrawText(str, x, y, (byte)(col % Spec.PaletteSize), mode, scale);

        // ─── Draw state ───

        public void Camera(int x = 0, int y = 0)
        {
            _state.CameraX = x;
            _state.CameraY = y;
        }

        public void Clip(int x = 0, int y = 0, int w = -1, int h = -1)
        {
            _state.ClipX = x;
            _state.ClipY = y;
            _state.ClipW = w < 0 ? Spec.ScreenWidth : w;
            _state.ClipH = h < 0 ? Spec.ScreenHeight : h;
        }

        public void Pal(int c0, int c1, int p = 0)
        {
            if (c0 < 0 || c0 >= Spec.PaletteSize) return;
            c1 = c1 % Spec.PaletteSize;
            if (p == 0)
                _state.DrawPalette[c0] = (byte)c1;
            else
                _state.DisplayPalette[c0] = (byte)c1;
        }

        public void Pal()
        {
            for (int i = 0; i < Spec.PaletteSize; i++)
            {
                _state.DrawPalette[i] = (byte)i;
                _state.DisplayPalette[i] = (byte)i;
                _state.Transparency[i] = (i == 0);
            }
        }

        public void Palt(int c, bool t)
        {
            if (c < 0 || c >= Spec.PaletteSize) return;
            _state.Transparency[c] = t;
        }

        public void Palt()
        {
            for (int i = 0; i < Spec.PaletteSize; i++)
                _state.Transparency[i] = (i == 0);
        }

        public void Fillp(int p = 0) => _state.FillPattern = p;

        public void Color(int col) => _state.CurrentColor = col % Spec.PaletteSize;

        public void Cursor(int x, int y, int col = -1)
        {
            _state.CursorX = x;
            _state.CursorY = y;
            if (col >= 0) _state.CurrentColor = col % Spec.PaletteSize;
        }

        public int Sget(int x, int y) => _sprites.GetPixel(0, x, y);

        public void Sset(int x, int y, int col) => _sprites.SetPixel(0, x, y, (byte)(col % Spec.PaletteSize));

        // ─── Map ───

        public int Mget(int x, int y) => _map.Get(x, y);
        public void Mset(int x, int y, int val) => _map.Set(x, y, (byte)val);

        public void Map(int tileX = 0, int tileY = 0, int sx = 0, int sy = 0,
            int tileW = -1, int tileH = -1, int layers = 0,
            CoordMode mode = CoordMode.Virtual, int scale = 1)
        {
            if (tileW < 0) tileW = Spec.MapWidth;
            if (tileH < 0) tileH = Spec.MapHeight;
            _gfx.DrawMap(tileX, tileY, sx, sy, tileW, tileH, layers, mode, scale);
        }

        public int Fget(int n, int f = -1)
        {
            if (f < 0) return _map.GetFlag(n);
            return _map.GetFlag(n, f) ? 1 : 0;
        }

        public void Fset(int n, int f, bool val) => _map.SetFlag(n, f, val);

        // ─── Input ───

        public bool Btn(int i, int p = 0) => _input.Btn(i, p);
        public bool Btnp(int i, int p = 0) => _input.Btnp(i, p);

        // ─── Audio ───

        public void Sfx(int n, int channel = -1, int offset = 0, int length = 32)
            => _audio.Sfx(n, channel, offset, length);

        public void Music(int n, int fadeLen = 0, int channelMask = 0)
            => _audio.Music(n, fadeLen, channelMask);

        // ─── Math ───

        public float Rnd(float x = 1)                 => _math.Rnd(x);
        public int   Flr(float x)                     => _math.Flr(x);
        public int   Ceil(float x)                    => _math.Ceil(x);
        public float Sin(float x)                     => _math.Sin(x);
        public float Cos(float x)                     => _math.Cos(x);
        public float Atan2(float dx, float dy)        => _math.Atan2(dx, dy);
        public float Sqrt(float x)                    => _math.Sqrt(x);
        public float Abs(float x)                     => _math.Abs(x);
        public float Sgn(float x)                     => _math.Sgn(x);
        public float Min(float x, float y)            => _math.Min(x, y);
        public float Max(float x, float y)            => _math.Max(x, y);
        public float Mid(float x, float y, float z)   => _math.Mid(x, y, z);
        public void  Srand(int x)                     => _math.Srand(x);

        // ─── Bitwise ───

        public int Shl(int x, int n) => x << n;
        public int Shr(int x, int n) => x >> n;
        public int Bor(int x, int y) => x | y;
        public int Band(int x, int y) => x & y;
        public int Bxor(int x, int y) => x ^ y;
        public int Bnot(int x) => ~x;

        // ─── Memory ───

        public int  Peek(int addr)                    => _mem.Peek(addr);
        public void Poke(int addr, int val)           => _mem.Poke(addr, (byte)val);
        public void Memcpy(int dest, int src, int len)
        {
            var layout = _mem.Layout;
            int scrStart = layout.ScreenStart;
            int scrEnd   = scrStart + layout.ScreenSize;

            bool srcTouchesScreen  = src < scrEnd && (src + len) > scrStart;
            bool destTouchesScreen = dest < scrEnd && (dest + len) > scrStart;

            // If reading from screen region, flush pixels → RAM first
            if ((srcTouchesScreen || destTouchesScreen) && _gfx is PixelBuffer pb)
            {
                pb.FlushToRam(_mem.Ram, scrStart);
                _mem.Memcpy(dest, src, len);
                if (destTouchesScreen)
                    pb.LoadFromRam(_mem.Ram, scrStart);
            }
            else
            {
                _mem.Memcpy(dest, src, len);
            }
        }

        public void Memset(int dest, int val, int len) => _mem.Memset(dest, (byte)val, len);

        // ─── System ───

        public float Time() => (float)_timer.Elapsed.TotalSeconds;

        /// <summary>CPU fraction set by the tick loop each frame.</summary>
        private float _cpuFraction;

        /// <summary>Called by the engine tick loop to report how much of the frame budget was used.</summary>
        public void SetCpuFraction(float fraction) => _cpuFraction = fraction;

        /// <summary>Returns system stat. id=1: CPU fraction (0..1).</summary>
        public float Stat(int id)
        {
            if (id == 1) return _cpuFraction;
            if (id == 32) return _input.MouseX;
            if (id == 33) return _input.MouseY;
            // Per-player analog axes: Right stick: Stat(34+p*2) = X, Stat(35+p*2) = Y
            if (id >= 34 && id <= 41)
            {
                int player = (id - 34) / 2;
                return (id % 2 == 0) ? _input.GetAxisX(player, 1) : _input.GetAxisY(player, 1);
            }
            // Left stick: Stat(42+p*2) = X, Stat(43+p*2) = Y
            if (id >= 42 && id <= 49)
            {
                int player = (id - 42) / 2;
                return (id % 2 == 0) ? _input.GetAxisX(player, 0) : _input.GetAxisY(player, 0);
            }
            return 0f;
        }

        /// <summary>Reloads cartridge ROM data (map + flags) to their original loaded state.</summary>
        public void Reload() => _map.Reload();

        // ─── System Menu ───
        
        public void MenuItem(int index, string label = null, System.Action callback = null)
        {
            // PICO-8 indexes menu items 1..5
            if (index < 1 || index > 5) return;
            
            if (string.IsNullOrEmpty(label) && callback == null)
            {
                CustomMenuItems[index - 1] = null;
            }
            else
            {
                CustomMenuItems[index - 1] = new CustomMenuItem { Label = label, Callback = callback };
            }
        }
    }
}
