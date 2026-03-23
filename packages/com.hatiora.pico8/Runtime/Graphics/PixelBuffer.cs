using System;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Software pixel buffer. Renders to a byte[] of palette indices.
    /// Handles virtual↔physical coordinate conversion via <see cref="CoordMode"/>.
    /// The Unity adapter converts this byte[] to a Texture2D for display.
    /// </summary>
    public sealed class PixelBuffer : IGraphics
    {
        private readonly EngineSpec _spec;
        private readonly DrawState _state;
        private readonly ISpriteStore _sprites;
        private readonly IMap _map;
        private readonly IFontProvider _font;

        /// <summary>Physical pixel buffer — palette indices, not colors.</summary>
        public byte[] Pixels { get; }

        // Render at VIRTUAL resolution — GPU handles upscaling
        public int Width  => _spec.ScreenWidth;
        public int Height => _spec.ScreenHeight;
        public int PhysicalWidth  => _spec.ScreenWidth;
        public int PhysicalHeight => _spec.ScreenHeight;

        public PixelBuffer(EngineSpec spec, DrawState state, ISpriteStore sprites, IMap map, IFontProvider font = null)
        {
            _spec = spec;
            _state = state;
            _sprites = sprites;
            _map = map;
            _font = font;
            Pixels = new byte[PhysicalWidth * PhysicalHeight];
        }

        // ─── Coordinate helpers ───
        // Scale is always 1 — GPU handles upscaling via FilterMode.Point
        private int Scale => 1;

        private void VirtToPhys(ref int x, ref int y, CoordMode mode)
        {
            if (mode == CoordMode.Virtual)
            {
                x = x * Scale;
                y = y * Scale;
            }
        }

        private void ApplyCamera(ref int x, ref int y, CoordMode mode)
        {
            if (mode == CoordMode.Virtual)
            {
                x -= _state.CameraX;
                y -= _state.CameraY;
            }
        }

        private bool ClipVirtual(int vx, int vy)
        {
            return vx >= _state.ClipX && vy >= _state.ClipY &&
                   vx < _state.ClipX + _state.ClipW &&
                   vy < _state.ClipY + _state.ClipH;
        }

        private void WriteScaledPixel(int vx, int vy, byte color, CoordMode mode)
        {
            ApplyCamera(ref vx, ref vy, mode);

            if (mode == CoordMode.Virtual && !ClipVirtual(vx, vy)) return;

            // Apply draw palette
            color = _state.DrawPalette[color % _spec.PaletteSize];

            // Check fill pattern
            if (_state.FillPattern != 0)
            {
                int bit = (vx & 3) + (vy & 3) * 4;
                if ((_state.FillPattern & (1 << bit)) != 0) return;
            }

            int px = vx, py = vy;
            VirtToPhys(ref px, ref py, mode);

            int scale = (mode == CoordMode.Virtual) ? Scale : 1;
            int pw = PhysicalWidth;
            int ph = PhysicalHeight;
            var pix = Pixels;

            // Early reject: entire block is off-screen
            if (px >= pw || py >= ph || px + scale <= 0 || py + scale <= 0) return;

            // Clamp to buffer bounds once — inner loop needs no checks
            int x0 = px < 0 ? 0 : px;
            int x1 = (px + scale) > pw ? pw : (px + scale);
            int y0 = py < 0 ? 0 : py;
            int y1 = (py + scale) > ph ? ph : (py + scale);

            for (int ry = y0; ry < y1; ry++)
            {
                int rowBase = ry * pw;
                for (int rx = x0; rx < x1; rx++)
                    pix[rowBase + rx] = color;
            }
        }

        // ─── IGraphics implementation ───

        public void Clear(byte colorIndex = 0)
        {
            Array.Fill(Pixels, colorIndex);
        }

        public void SetPixel(int x, int y, byte colorIndex, CoordMode mode = CoordMode.Virtual)
        {
            WriteScaledPixel(x, y, colorIndex, mode);
        }

        public byte GetPixel(int x, int y, CoordMode mode = CoordMode.Virtual)
        {
            int px = x, py = y;
            ApplyCamera(ref px, ref py, mode);
            VirtToPhys(ref px, ref py, mode);
            if (px < 0 || py < 0 || px >= PhysicalWidth || py >= PhysicalHeight) return 0;
            return Pixels[py * PhysicalWidth + px];
        }

        public void DrawLine(int x0, int y0, int x1, int y1, byte colorIndex, CoordMode mode = CoordMode.Virtual)
        {
            // Bresenham's line algorithm
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                WriteScaledPixel(x0, y0, colorIndex, mode);
                if (x0 == x1 && y0 == y1) break;
                int e2 = err * 2;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 <  dx) { err += dx; y0 += sy; }
            }
        }

        public void DrawRect(int x0, int y0, int x1, int y1, byte colorIndex, bool fill, CoordMode mode = CoordMode.Virtual)
        {
            if (x0 > x1) (x0, x1) = (x1, x0);
            if (y0 > y1) (y0, y1) = (y1, y0);

            if (fill)
            {
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        WriteScaledPixel(x, y, colorIndex, mode);
            }
            else
            {
                for (int x = x0; x <= x1; x++)
                {
                    WriteScaledPixel(x, y0, colorIndex, mode);
                    WriteScaledPixel(x, y1, colorIndex, mode);
                }
                for (int y = y0 + 1; y < y1; y++)
                {
                    WriteScaledPixel(x0, y, colorIndex, mode);
                    WriteScaledPixel(x1, y, colorIndex, mode);
                }
            }
        }

        public void DrawCircle(int cx, int cy, int r, byte colorIndex, bool fill, CoordMode mode = CoordMode.Virtual)
        {
            if (r < 0) return;
            if (r == 0) { WriteScaledPixel(cx, cy, colorIndex, mode); return; }

            // Midpoint circle algorithm
            int x = r, y = 0, d = 1 - r;

            while (x >= y)
            {
                if (fill)
                {
                    DrawHLine(cx - x, cx + x, cy + y, colorIndex, mode);
                    DrawHLine(cx - x, cx + x, cy - y, colorIndex, mode);
                    DrawHLine(cx - y, cx + y, cy + x, colorIndex, mode);
                    DrawHLine(cx - y, cx + y, cy - x, colorIndex, mode);
                }
                else
                {
                    WriteScaledPixel(cx + x, cy + y, colorIndex, mode);
                    WriteScaledPixel(cx - x, cy + y, colorIndex, mode);
                    WriteScaledPixel(cx + x, cy - y, colorIndex, mode);
                    WriteScaledPixel(cx - x, cy - y, colorIndex, mode);
                    WriteScaledPixel(cx + y, cy + x, colorIndex, mode);
                    WriteScaledPixel(cx - y, cy + x, colorIndex, mode);
                    WriteScaledPixel(cx + y, cy - x, colorIndex, mode);
                    WriteScaledPixel(cx - y, cy - x, colorIndex, mode);
                }

                y++;
                if (d < 0) { d += 2 * y + 1; }
                else { x--; d += 2 * (y - x) + 1; }
            }
        }

        public void DrawOval(int x0, int y0, int x1, int y1, byte colorIndex, bool fill, CoordMode mode = CoordMode.Virtual)
        {
            if (x0 > x1) (x0, x1) = (x1, x0);
            if (y0 > y1) (y0, y1) = (y1, y0);

            float cx = (x0 + x1) / 2f;
            float cy = (y0 + y1) / 2f;
            float rx = (x1 - x0) / 2f;
            float ry = (y1 - y0) / 2f;
            if (rx < 0.5f || ry < 0.5f) return;

            for (int y = y0; y <= y1; y++)
            {
                float dy = (y - cy) / ry;
                if (dy * dy > 1f) continue;
                float span = rx * MathF.Sqrt(1f - dy * dy);
                int left = (int)MathF.Ceiling(cx - span);
                int right = (int)MathF.Floor(cx + span);
                if (fill)
                {
                    DrawHLine(left, right, y, colorIndex, mode);
                }
                else
                {
                    WriteScaledPixel(left, y, colorIndex, mode);
                    if (right != left) WriteScaledPixel(right, y, colorIndex, mode);
                }
            }
        }

        public void DrawSprite(int bank, int spriteIndex, int x, int y, int w, int h,
            bool flipX, bool flipY, CoordMode mode = CoordMode.Virtual)
        {
            int ss = _spec.SpriteSize;
            int tilesPerRow = _spec.SpritesPerRow;
            int srcX = (spriteIndex % tilesPerRow) * ss;
            int srcY = (spriteIndex / tilesPerRow) * ss;
            int pw = w * ss;
            int ph = h * ss;

            for (int dy = 0; dy < ph; dy++)
            {
                int sy = flipY ? (ph - 1 - dy) : dy;
                for (int dx = 0; dx < pw; dx++)
                {
                    int sx = flipX ? (pw - 1 - dx) : dx;
                    byte col = _sprites.GetPixel(bank, srcX + sx, srcY + sy);
                    if (_state.Transparency[col % _spec.PaletteSize]) continue;
                    WriteScaledPixel(x + dx, y + dy, col, mode);
                }
            }
        }

        public void DrawSpriteStretch(int sx, int sy, int sw, int sh,
            int dx, int dy, int dw, int dh, bool flipX, bool flipY,
            CoordMode mode = CoordMode.Virtual, float angle = 0f)
        {
            if (dw <= 0) dw = sw;
            if (dh <= 0) dh = sh;

            // ── Rotated path ──
            if (angle != 0f)
            {
                float cx = dx + dw / 2f;
                float cy = dy + dh / 2f;
                float rad = angle * MathF.PI * 2f;
                float cosA = MathF.Cos(-rad);
                float sinA = MathF.Sin(-rad);
                float halfDiag = MathF.Sqrt(dw * dw + dh * dh) / 2f + 1;
                int minX = (int)(cx - halfDiag);
                int maxX = (int)(cx + halfDiag);
                int minY = (int)(cy - halfDiag);
                int maxY = (int)(cy + halfDiag);

                for (int py = minY; py <= maxY; py++)
                {
                    for (int px = minX; px <= maxX; px++)
                    {
                        float relX = px - cx;
                        float relY = py - cy;
                        float localX = relX * cosA - relY * sinA + dw / 2f;
                        float localY = relX * sinA + relY * cosA + dh / 2f;
                        if (localX < 0 || localX >= dw || localY < 0 || localY >= dh) continue;

                        int srcLocalX = (int)(localX * sw / dw);
                        int srcLocalY = (int)(localY * sh / dh);
                        if (flipX) srcLocalX = sw - 1 - srcLocalX;
                        if (flipY) srcLocalY = sh - 1 - srcLocalY;
                        if (srcLocalX < 0) srcLocalX = 0;
                        if (srcLocalX >= sw) srcLocalX = sw - 1;
                        if (srcLocalY < 0) srcLocalY = 0;
                        if (srcLocalY >= sh) srcLocalY = sh - 1;

                        byte col = _sprites.GetPixel(0, sx + srcLocalX, sy + srcLocalY);
                        if (_state.Transparency[col % _spec.PaletteSize]) continue;
                        WriteScaledPixel(px, py, col, mode);
                    }
                }
                return;
            }

            // ── Non-rotated fast path (integer math) ──
            for (int py = 0; py < dh; py++)
            {
                int localY = flipY ? (sh - 1 - ((py * sh) / dh)) : ((py * sh) / dh);
                if (localY < 0) localY = 0;
                if (localY >= sh) localY = sh - 1;
                int srcy = sy + localY;

                for (int px = 0; px < dw; px++)
                {
                    int localX = flipX ? (sw - 1 - ((px * sw) / dw)) : ((px * sw) / dw);
                    if (localX < 0) localX = 0;
                    if (localX >= sw) localX = sw - 1;
                    int srcx = sx + localX;

                    byte col = _sprites.GetPixel(0, srcx, srcy);
                    if (_state.Transparency[col % _spec.PaletteSize]) continue;
                    WriteScaledPixel(dx + px, dy + py, col, mode);
                }
            }
        }

        public void DrawMap(int tileX, int tileY, int screenX, int screenY,
            int tileW, int tileH, int layers, CoordMode mode = CoordMode.Virtual, int scale = 1)
        {
            int ss = _spec.SpriteSize;
            int dss = ss * scale;
            for (int ty = 0; ty < tileH; ty++)
            {
                for (int tx = 0; tx < tileW; tx++)
                {
                    byte tile = _map.Get(tileX + tx, tileY + ty);
                    if (tile == 0) continue;

                    // Layer filtering via sprite flags
                    if (layers != 0)
                    {
                        byte flags = _map.GetFlag(tile);
                        if ((flags & layers) == 0) continue;
                    }

                    if (scale <= 1)
                    {
                        DrawSprite(0, tile, screenX + tx * dss, screenY + ty * dss,
                            1, 1, false, false, mode);
                    }
                    else
                    {
                        // Scaled tile: use stretch drawing
                        int sprSx = (tile % 16) * ss;
                        int sprSy = (tile / 16) * ss;
                        DrawSpriteStretch(sprSx, sprSy, ss, ss,
                            screenX + tx * dss, screenY + ty * dss,
                            dss, dss, false, false, mode);
                    }
                }
            }
        }

        public int DrawText(string str, int x, int y, byte colorIndex, CoordMode mode = CoordMode.Virtual, float scale = 1f)
        {
            if (str == null || _font == null) return x;

            _font.Prepare(str);
            int cx = x;
            int sw = Math.Max(1, (int)Math.Round(scale));
            int sh = sw; // square scaling

            foreach (char c in str)
            {
                if (c == '\n') { cx = x; y += (int)(6 * scale); continue; }

                var glyph = _font.GetGlyph(c);

                if (glyph.Pixels != null)
                {
                    int ox = cx + (int)(glyph.BearingX * scale);
                    int oy = y + (int)(glyph.BearingY * scale);

                    for (int gy = 0; gy < glyph.Height; gy++)
                        for (int gx = 0; gx < glyph.Width; gx++)
                            if (glyph.Pixels[gy * glyph.Width + gx])
                            {
                                int px = ox + (int)(gx * scale);
                                int py = oy + (int)(gy * scale);

                                for (int dy = 0; dy < sh; dy++)
                                {
                                    for (int dx = 0; dx < sw; dx++)
                                    {
                                        WriteScaledPixel(px + dx, py + dy, colorIndex, mode);
                                    }
                                }
                            }
                }

                cx += (int)(glyph.Advance * scale);
            }

            return cx;
        }


        public void Flush()
        {
            // No-op for software buffer. Unity adapter reads Pixels[] after this.
        }

        // ─── RAM sync (memory-mapped screen) ───

        /// <summary>
        /// Packs virtual pixels from <see cref="Pixels"/> into nibble-packed RAM
        /// at <paramref name="screenStart"/>. Samples every Scale-th physical pixel
        /// to reconstruct the virtual grid. Works with any ScreenWidth/Height.
        /// </summary>
        public void FlushToRam(byte[] ram, int screenStart)
        {
            int vw = _spec.ScreenWidth;
            int vh = _spec.ScreenHeight;
            int scale = Scale;
            int pw = PhysicalWidth;
            int bytesPerRow = vw / 2;

            for (int vy = 0; vy < vh; vy++)
            {
                int physY = vy * scale;
                int ramRow = screenStart + vy * bytesPerRow;

                for (int vx = 0; vx < vw; vx += 2)
                {
                    byte lo = Pixels[physY * pw + vx * scale];
                    byte hi = Pixels[physY * pw + (vx + 1) * scale];
                    ram[ramRow + vx / 2] = (byte)((lo & 0x0F) | ((hi & 0x0F) << 4));
                }
            }
        }

        /// <summary>
        /// Unpacks nibble-packed RAM at <paramref name="screenStart"/> back into
        /// <see cref="Pixels"/>, expanding each virtual pixel to a Scale×Scale
        /// physical block. Works with any ScreenWidth/Height.
        /// </summary>
        public void LoadFromRam(byte[] ram, int screenStart)
        {
            int vw = _spec.ScreenWidth;
            int vh = _spec.ScreenHeight;
            int scale = Scale;
            int pw = PhysicalWidth;
            int bytesPerRow = vw / 2;

            for (int vy = 0; vy < vh; vy++)
            {
                int ramRow = screenStart + vy * bytesPerRow;

                for (int vx = 0; vx < vw; vx += 2)
                {
                    byte packed = ram[ramRow + vx / 2];
                    byte lo = (byte)(packed & 0x0F);
                    byte hi = (byte)((packed >> 4) & 0x0F);

                    FillPhysicalBlock(vx, vy, scale, pw, lo);
                    FillPhysicalBlock(vx + 1, vy, scale, pw, hi);
                }
            }
        }

        private void FillPhysicalBlock(int vx, int vy, int scale, int pw, byte color)
        {
            int px = vx * scale;
            int py = vy * scale;
            int ph = PhysicalHeight;

            for (int dy = 0; dy < scale; dy++)
            {
                int ry = py + dy;
                if (ry >= ph) break;
                for (int dx = 0; dx < scale; dx++)
                {
                    int rx = px + dx;
                    if (rx >= pw) break;
                    Pixels[ry * pw + rx] = color;
                }
            }
        }

        // ─── Helpers ───

        private void DrawHLine(int x0, int x1, int y, byte colorIndex, CoordMode mode)
        {
            for (int x = x0; x <= x1; x++)
                WriteScaledPixel(x, y, colorIndex, mode);
        }
    }
}
