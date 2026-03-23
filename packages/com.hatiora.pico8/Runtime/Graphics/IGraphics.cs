namespace Hatiora.Pico8
{
    /// <summary>
    /// Low-level drawing subsystem. Called by <see cref="Pico8Api"/>, not by game code directly.
    /// All draw methods accept <see cref="CoordMode"/> to toggle virtual/physical coordinates.
    /// </summary>
    public interface IGraphics
    {
        int Width { get; }
        int Height { get; }

        void Clear(byte colorIndex = 0);
        void SetPixel(int x, int y, byte colorIndex, CoordMode mode = CoordMode.Virtual);
        byte GetPixel(int x, int y, CoordMode mode = CoordMode.Virtual);
        void DrawLine(int x0, int y0, int x1, int y1, byte colorIndex, CoordMode mode = CoordMode.Virtual);
        void DrawRect(int x0, int y0, int x1, int y1, byte colorIndex, bool fill, CoordMode mode = CoordMode.Virtual);
        void DrawCircle(int cx, int cy, int r, byte colorIndex, bool fill, CoordMode mode = CoordMode.Virtual);
        void DrawOval(int x0, int y0, int x1, int y1, byte colorIndex, bool fill, CoordMode mode = CoordMode.Virtual);
        void DrawSprite(int bank, int spriteIndex, int x, int y, int w, int h,
            bool flipX, bool flipY, CoordMode mode = CoordMode.Virtual);
        void DrawSpriteStretch(int sx, int sy, int sw, int sh,
            int dx, int dy, int dw, int dh, bool flipX, bool flipY,
            CoordMode mode = CoordMode.Virtual, float angle = 0f);
        void DrawMap(int tileX, int tileY, int screenX, int screenY,
            int tileW, int tileH, int layers, CoordMode mode = CoordMode.Virtual, int scale = 1);
        int DrawText(string str, int x, int y, byte colorIndex, CoordMode mode = CoordMode.Virtual, float scale = 1f);
        void Flush();
    }
}
