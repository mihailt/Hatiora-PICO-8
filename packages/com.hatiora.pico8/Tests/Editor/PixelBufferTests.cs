using NUnit.Framework;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class PixelBufferTests
    {
        private EngineSpec _spec;
        private DrawState _state;
        private SpriteStore _sprites;
        private MapStore _map;
        private Pico8Memory _mem;
        private PixelBuffer _buffer;

        [SetUp]
        public void SetUp()
        {
            _spec = EngineSpec.Pico8;
            _state = new DrawState(_spec);
            _sprites = new SpriteStore(_spec);
            _mem = new Pico8Memory(_spec);
            _map = new MapStore(_spec, _mem);
            // Initialize sprite bank 0 so sprite drawing tests work
            _sprites.LoadBank(0, new byte[128 * 128], 128, 128);
            _buffer = new PixelBuffer(_spec, _state, _sprites, _map);
        }

        [Test]
        public void Width_MatchesSpec()
        {
            Assert.AreEqual(128, _buffer.Width);
        }

        [Test]
        public void Height_MatchesSpec()
        {
            Assert.AreEqual(128, _buffer.Height);
        }

        [Test]
        public void Clear_FillsEntireBuffer()
        {
            _buffer.Clear(5);
            for (int i = 0; i < _buffer.Pixels.Length; i++)
                Assert.AreEqual(5, _buffer.Pixels[i], $"Pixel {i} should be 5");
        }

        [Test]
        public void SetPixel_Virtual_WritesToBuffer()
        {
            _buffer.SetPixel(0, 0, 7, CoordMode.Virtual);
            // Scale=1 for PICO-8 native, so virtual == physical
            Assert.AreEqual(7, _buffer.Pixels[0]);
        }

        [Test]
        public void GetPixel_ReturnsWrittenValue()
        {
            _buffer.SetPixel(10, 5, 12, CoordMode.Virtual);
            Assert.AreEqual(12, _buffer.GetPixel(10, 5, CoordMode.Virtual));
        }

        [Test]
        public void DrawRect_Fill_CoversBounds()
        {
            _buffer.Clear(0);
            _buffer.DrawRect(2, 3, 5, 6, 8, true, CoordMode.Virtual);

            // Pixel inside rect should be colored
            Assert.AreEqual(8, _buffer.GetPixel(3, 4, CoordMode.Virtual));

            // Pixel outside rect should be background
            Assert.AreEqual(0, _buffer.GetPixel(1, 3, CoordMode.Virtual));
            Assert.AreEqual(0, _buffer.GetPixel(6, 3, CoordMode.Virtual));
        }

        [Test]
        public void DrawRect_Outline_OnlyDrawsEdges()
        {
            _buffer.Clear(0);
            _buffer.DrawRect(0, 0, 4, 4, 7, false, CoordMode.Virtual);

            // Corners should be drawn
            Assert.AreEqual(7, _buffer.GetPixel(0, 0, CoordMode.Virtual));
            Assert.AreEqual(7, _buffer.GetPixel(4, 0, CoordMode.Virtual));
            Assert.AreEqual(7, _buffer.GetPixel(0, 4, CoordMode.Virtual));

            // Center should be empty
            Assert.AreEqual(0, _buffer.GetPixel(2, 2, CoordMode.Virtual));
        }

        [Test]
        public void CameraOffset_ShiftsDrawing()
        {
            _state.CameraX = 10;
            _state.CameraY = 5;

            _buffer.Clear(0);
            _buffer.SetPixel(15, 10, 9, CoordMode.Virtual);

            // The pixel should be drawn at physical (15-10, 10-5) = (5, 5)
            Assert.AreEqual(9, _buffer.Pixels[5 * 128 + 5]);
        }

        [Test]
        public void ClipRect_PreventsDrawingOutside()
        {
            _state.ClipX = 10;
            _state.ClipY = 10;
            _state.ClipW = 20;
            _state.ClipH = 20;

            _buffer.Clear(0);
            _buffer.SetPixel(5, 5, 7, CoordMode.Virtual); // outside clip
            _buffer.SetPixel(15, 15, 7, CoordMode.Virtual); // inside clip

            Assert.AreEqual(0, _buffer.Pixels[5 * 128 + 5]); // clipped
            Assert.AreEqual(7, _buffer.Pixels[15 * 128 + 15]); // visible
        }

        [Test]
        public void DrawPalette_RemapsColors()
        {
            // Remap color 5 → color 8
            _state.DrawPalette[5] = 8;

            _buffer.Clear(0);
            _buffer.SetPixel(0, 0, 5, CoordMode.Virtual);

            Assert.AreEqual(8, _buffer.Pixels[0]); // remapped
        }

        [Test]
        public void PhysicalMode_BypassesCamera()
        {
            _state.CameraX = 50;
            _state.CameraY = 50;

            _buffer.Clear(0);
            _buffer.SetPixel(3, 3, 11, CoordMode.Physical);

            // Should be at raw position, NOT offset by camera
            Assert.AreEqual(11, _buffer.Pixels[3 * 128 + 3]);
        }

        [Test]
        public void DrawLine_Horizontal()
        {
            _buffer.Clear(0);
            _buffer.DrawLine(2, 5, 7, 5, 4, CoordMode.Virtual);

            for (int x = 2; x <= 7; x++)
                Assert.AreEqual(4, _buffer.GetPixel(x, 5, CoordMode.Virtual));
        }

        [Test]
        public void DrawLine_Vertical()
        {
            _buffer.Clear(0);
            _buffer.DrawLine(3, 1, 3, 6, 9, CoordMode.Virtual);

            for (int y = 1; y <= 6; y++)
                Assert.AreEqual(9, _buffer.GetPixel(3, y, CoordMode.Virtual));
        }

        [Test]
        public void Scaled_SetPixel_WritesNxNBlock()
        {
            var scaledSpec = new EngineSpec
            {
                ScreenWidth = 128,
                ScreenHeight = 128,
                PhysicalWidth = 256,
                PhysicalHeight = 256,
            };
            var state = new DrawState(scaledSpec);
            var sprites = new SpriteStore(scaledSpec);
            var mem = new Pico8Memory(scaledSpec);
            var map = new MapStore(scaledSpec, mem);
            var buf = new PixelBuffer(scaledSpec, state, sprites, map);

            buf.Clear(0);
            buf.SetPixel(0, 0, 7, CoordMode.Virtual);

            // Scale=2, so 4 pixels should be written
            Assert.AreEqual(7, buf.Pixels[0]); // (0,0)
            Assert.AreEqual(7, buf.Pixels[1]); // (1,0)
            Assert.AreEqual(7, buf.Pixels[256]); // (0,1)
            Assert.AreEqual(7, buf.Pixels[257]); // (1,1)

            // Adjacent virtual pixel should be empty
            Assert.AreEqual(0, buf.Pixels[2]); // (2,0)
        }

        [Test]
        public void Flush_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _buffer.Flush());
        }

        [Test]
        public void DrawLine_Diagonal()
        {
            _buffer.Clear(0);
            _buffer.DrawLine(0, 0, 5, 5, 3, CoordMode.Virtual);
            // Bresenham diagonal should hit at least the endpoints
            Assert.AreEqual(3, _buffer.GetPixel(0, 0, CoordMode.Virtual));
            Assert.AreEqual(3, _buffer.GetPixel(5, 5, CoordMode.Virtual));
        }

        [Test]
        public void DrawLine_SteepDiagonal()
        {
            _buffer.Clear(0);
            _buffer.DrawLine(2, 0, 4, 10, 6, CoordMode.Virtual);
            Assert.AreEqual(6, _buffer.GetPixel(2, 0, CoordMode.Virtual));
            Assert.AreEqual(6, _buffer.GetPixel(4, 10, CoordMode.Virtual));
        }

        [Test]
        public void DrawCircle_Fill()
        {
            _buffer.Clear(0);
            _buffer.DrawCircle(64, 64, 5, 7, true, CoordMode.Virtual);
            // Center should be filled
            Assert.AreEqual(7, _buffer.GetPixel(64, 64, CoordMode.Virtual));
            // Point on edge should also be filled
            Assert.AreEqual(7, _buffer.GetPixel(64, 59, CoordMode.Virtual));
        }

        [Test]
        public void DrawCircle_Outline()
        {
            _buffer.Clear(0);
            _buffer.DrawCircle(64, 64, 5, 7, false, CoordMode.Virtual);
            // Center should NOT be filled for outline
            Assert.AreEqual(0, _buffer.GetPixel(64, 64, CoordMode.Virtual));
            // Point on edge should be drawn
            Assert.AreEqual(7, _buffer.GetPixel(64, 59, CoordMode.Virtual));
        }

        [Test]
        public void DrawOval_Fill()
        {
            _buffer.Clear(0);
            _buffer.DrawOval(30, 40, 50, 52, 9, true, CoordMode.Virtual);
            // Center should be filled
            int cx = (30 + 50) / 2;
            int cy = (40 + 52) / 2;
            Assert.AreEqual(9, _buffer.GetPixel(cx, cy, CoordMode.Virtual));
        }

        [Test]
        public void DrawOval_Outline()
        {
            _buffer.Clear(0);
            _buffer.DrawOval(30, 40, 50, 52, 9, false, CoordMode.Virtual);
            // Center should NOT be filled
            int cx = (30 + 50) / 2;
            int cy = (40 + 52) / 2;
            Assert.AreEqual(0, _buffer.GetPixel(cx, cy, CoordMode.Virtual));
        }

        [Test]
        public void DrawOval_TooSmall_NoOp()
        {
            _buffer.Clear(0);
            _buffer.DrawOval(50, 50, 50, 50, 9, true, CoordMode.Virtual);
            // Radius too small, nothing drawn
            Assert.AreEqual(0, _buffer.GetPixel(50, 50, CoordMode.Virtual));
        }

        [Test]
        public void DrawOval_SwappedCoords()
        {
            _buffer.Clear(0);
            // Pass coords in reverse order — should still work
            _buffer.DrawOval(50, 52, 30, 40, 9, true, CoordMode.Virtual);
            int cx = (30 + 50) / 2;
            int cy = (40 + 52) / 2;
            Assert.AreEqual(9, _buffer.GetPixel(cx, cy, CoordMode.Virtual));
        }

        [Test]
        public void DrawSprite_BasicDraw()
        {
            // Write a pixel into the sprite store at (0,0) of sprite 0
            _sprites.SetPixel(0, 0, 0, 5);
            _buffer.Clear(0);
            _state.Transparency[0] = true; // transparent color 0
            _state.Transparency[5] = false;
            _buffer.DrawSprite(0, 0, 10, 10, 1, 1, false, false, CoordMode.Virtual);
            Assert.AreEqual(5, _buffer.GetPixel(10, 10, CoordMode.Virtual));
        }

        [Test]
        public void DrawSprite_FlipX()
        {
            // Write a pixel at top-left of sprite
            _sprites.SetPixel(0, 0, 0, 5);
            _buffer.Clear(0);
            _state.Transparency[0] = true;
            _state.Transparency[5] = false;
            // FlipX: pixel at (0,0) in sprite → shown at (7,0) in output
            _buffer.DrawSprite(0, 0, 10, 10, 1, 1, true, false, CoordMode.Virtual);
            Assert.AreEqual(5, _buffer.GetPixel(17, 10, CoordMode.Virtual));
        }

        [Test]
        public void DrawSprite_FlipY()
        {
            _sprites.SetPixel(0, 0, 0, 5);
            _buffer.Clear(0);
            _state.Transparency[0] = true;
            _state.Transparency[5] = false;
            // FlipY: pixel at (0,0) in sprite → shown at (0,7) in output
            _buffer.DrawSprite(0, 0, 10, 10, 1, 1, false, true, CoordMode.Virtual);
            Assert.AreEqual(5, _buffer.GetPixel(10, 17, CoordMode.Virtual));
        }

        [Test]
        public void DrawSpriteStretch_Draws()
        {
            _sprites.SetPixel(0, 0, 0, 5);
            _buffer.Clear(0);
            _state.Transparency[0] = true;
            _state.Transparency[5] = false;
            // Stretch a 8x8 region to 16x16
            _buffer.DrawSpriteStretch(0, 0, 8, 8, 10, 10, 16, 16, false, false, CoordMode.Virtual);
            Assert.AreEqual(5, _buffer.GetPixel(10, 10, CoordMode.Virtual));
        }

        [Test]
        public void DrawSpriteStretch_DefaultSize()
        {
            _sprites.SetPixel(0, 0, 0, 5);
            _buffer.Clear(0);
            _state.Transparency[0] = true;
            _state.Transparency[5] = false;
            // dw=0, dh=0 → uses sw, sh
            _buffer.DrawSpriteStretch(0, 0, 8, 8, 10, 10, 0, 0, false, false, CoordMode.Virtual);
            Assert.AreEqual(5, _buffer.GetPixel(10, 10, CoordMode.Virtual));
        }

        [Test]
        public void DrawSpriteStretch_Flip()
        {
            _sprites.SetPixel(0, 0, 0, 5);
            _buffer.Clear(0);
            _state.Transparency[0] = true;
            _state.Transparency[5] = false;
            _buffer.DrawSpriteStretch(0, 0, 8, 8, 10, 10, 8, 8, true, true, CoordMode.Virtual);
            // Flipped: original (0,0) pixel appears at (17,17)
            Assert.AreEqual(5, _buffer.GetPixel(17, 17, CoordMode.Virtual));
        }

        [Test]
        public void DrawMap_Basic()
        {
            // Set tile 1 at map position (0,0)
            _map.Set(0, 0, 1);
            // Put a pixel in sprite 1
            _sprites.SetPixel(0, 0, 0, 5); // sprite 0 at (0,0) — tile 1 uses sprite 1
            _sprites.SetPixel(0, 8, 0, 5); // sprite 1 at (8,0) — first pixel

            _buffer.Clear(0);
            _state.Transparency[0] = true;
            _state.Transparency[5] = false;

            _buffer.DrawMap(0, 0, 0, 0, 1, 1, 0, CoordMode.Virtual);
            // Tile 1 draws sprite 1 at screen (0,0)
            Assert.AreEqual(5, _buffer.GetPixel(0, 0, CoordMode.Virtual));
        }

        [Test]
        public void DrawMap_LayerFilter()
        {
            _map.Set(0, 0, 1);
            _map.SetFlag(1, 1, true); // sprite 1 has flag bit 1 set (= 0x02)
            _sprites.SetPixel(0, 8, 0, 5);

            _buffer.Clear(0);
            _state.Transparency[0] = true;
            _state.Transparency[5] = false;

            // Filter for layer 0x01 — should NOT draw sprite 1 (has flag 0x02)
            _buffer.DrawMap(0, 0, 0, 0, 1, 1, 0x01, CoordMode.Virtual);
            Assert.AreEqual(0, _buffer.GetPixel(0, 0, CoordMode.Virtual));

            // Filter for layer 0x02 — should draw
            _buffer.DrawMap(0, 0, 0, 0, 1, 1, 0x02, CoordMode.Virtual);
            Assert.AreEqual(5, _buffer.GetPixel(0, 0, CoordMode.Virtual));
        }

        [Test]
        public void DrawText_NullString_ReturnsX()
        {
            int result = _buffer.DrawText(null, 10, 10, 7, CoordMode.Virtual);
            Assert.AreEqual(10, result);
        }

        [Test]
        public void DrawText_NullFont_ReturnsX()
        {
            // Default buffer has no font
            int result = _buffer.DrawText("hello", 10, 10, 7, CoordMode.Virtual);
            Assert.AreEqual(10, result);
        }

        [Test]
        public void DrawText_WithFont_DrawsPixels()
        {
            var font = new StubFontProvider();
            var buf = new PixelBuffer(_spec, _state, _sprites, _map, font);
            buf.Clear(0);
            int endX = buf.DrawText("A", 10, 10, 7, CoordMode.Virtual);
            // StubFontProvider returns a 4x5 glyph with all pixels set, advance=4
            Assert.AreEqual(14, endX);
            Assert.AreEqual(7, buf.GetPixel(10, 10, CoordMode.Virtual));
        }

        [Test]
        public void DrawText_Newline()
        {
            var font = new StubFontProvider();
            var buf = new PixelBuffer(_spec, _state, _sprites, _map, font);
            buf.Clear(0);
            buf.DrawText("A\nB", 5, 5, 7, CoordMode.Virtual);
            // First line at y=5, second at y=5+6=11
            Assert.AreEqual(7, buf.GetPixel(5, 5, CoordMode.Virtual));
            Assert.AreEqual(7, buf.GetPixel(5, 11, CoordMode.Virtual));
        }

        private class StubFontProvider : IFontProvider
        {
            public void Prepare(string text) { }

            public GlyphData GetGlyph(char c) => new GlyphData
            {
                Width = 4,
                Height = 5,
                BearingX = 0,
                BearingY = 0,
                Advance = 4,
                Pixels = CreateAllTrue(4, 5)
            };

            private static bool[] CreateAllTrue(int w, int h)
            {
                var arr = new bool[w * h];
                for (int i = 0; i < arr.Length; i++) arr[i] = true;
                return arr;
            }
        }

        [Test]
        public void FillPattern_MasksPixels()
        {
            // Set fill pattern to all bits set (0xFFFF = 16 bits, blocks all fills)
            _state.FillPattern = 0xFFFF;

            _buffer.Clear(0);
            _buffer.SetPixel(0, 0, 7, CoordMode.Virtual);

            // The pixel should NOT be written because fill pattern blocks it
            Assert.AreEqual(0, _buffer.GetPixel(0, 0, CoordMode.Virtual));
        }

        [Test]
        public void FillPattern_AllowsSomePixels()
        {
            // Set fill pattern with bit 0 clear (allows pixel at (0,0))
            _state.FillPattern = 0xFFFE; // bit 0 = 0, rest = 1

            _buffer.Clear(0);
            _buffer.SetPixel(0, 0, 7, CoordMode.Virtual);

            // Pixel at (0,0) should be written because bit 0 is clear
            Assert.AreEqual(7, _buffer.GetPixel(0, 0, CoordMode.Virtual));
        }

        [Test]
        public void FillPattern_Zero_NoMasking()
        {
            _state.FillPattern = 0;
            _buffer.Clear(0);
            _buffer.SetPixel(0, 0, 7, CoordMode.Virtual);
            Assert.AreEqual(7, _buffer.GetPixel(0, 0, CoordMode.Virtual));
        }

        [Test]
        public void DrawCircle_NegativeRadius_NoOp()
        {
            _buffer.Clear(0);
            _buffer.DrawCircle(64, 64, -1, 7, true, CoordMode.Virtual);
            // Nothing should be drawn
            Assert.AreEqual(0, _buffer.GetPixel(64, 64, CoordMode.Virtual));
        }

        [Test]
        public void DrawCircle_ZeroRadius_SinglePixel()
        {
            _buffer.Clear(0);
            _buffer.DrawCircle(64, 64, 0, 7, false, CoordMode.Virtual);
            Assert.AreEqual(7, _buffer.GetPixel(64, 64, CoordMode.Virtual));
        }

        [Test]
        public void GetPixel_OutOfBounds_ReturnsZero()
        {
            _buffer.Clear(5);
            Assert.AreEqual(0, _buffer.GetPixel(-1, -1, CoordMode.Virtual));
            Assert.AreEqual(0, _buffer.GetPixel(999, 999, CoordMode.Virtual));
        }

        [Test]
        public void PhysicalDimensions_MatchSpec()
        {
            Assert.AreEqual(128, _buffer.PhysicalWidth);
            Assert.AreEqual(128, _buffer.PhysicalHeight);
        }

        // ─── RAM sync tests ───

        [Test]
        public void FlushToRam_PacksNibbles()
        {
            _buffer.Clear(0);
            _buffer.SetPixel(0, 0, 7, CoordMode.Virtual);  // even pixel → lo nibble
            _buffer.SetPixel(1, 0, 3, CoordMode.Virtual);  // odd pixel → hi nibble

            var ram = _mem.Ram;
            int scrStart = _mem.Layout.ScreenStart;
            _buffer.FlushToRam(ram, scrStart);

            // byte = (lo & 0x0F) | ((hi & 0x0F) << 4) = 7 | (3 << 4) = 0x37
            Assert.AreEqual(0x37, ram[scrStart]);
        }

        [Test]
        public void LoadFromRam_UnpacksNibbles()
        {
            var ram = _mem.Ram;
            int scrStart = _mem.Layout.ScreenStart;

            // Pack: lo=5, hi=9 → byte = 5 | (9 << 4) = 0x95
            ram[scrStart] = 0x95;

            _buffer.Clear(0);
            _buffer.LoadFromRam(ram, scrStart);

            Assert.AreEqual(5, _buffer.GetPixel(0, 0, CoordMode.Virtual));
            Assert.AreEqual(9, _buffer.GetPixel(1, 0, CoordMode.Virtual));
        }

        [Test]
        public void FlushAndLoad_RoundTrip()
        {
            _buffer.Clear(0);
            _buffer.SetPixel(10, 5, 12, CoordMode.Virtual);
            _buffer.SetPixel(11, 5, 3, CoordMode.Virtual);

            var ram = _mem.Ram;
            int scrStart = _mem.Layout.ScreenStart;

            _buffer.FlushToRam(ram, scrStart);
            _buffer.Clear(0); // wipe pixels
            _buffer.LoadFromRam(ram, scrStart);

            Assert.AreEqual(12, _buffer.GetPixel(10, 5, CoordMode.Virtual));
            Assert.AreEqual(3, _buffer.GetPixel(11, 5, CoordMode.Virtual));
        }

        [Test]
        public void FlushAndLoad_Scaled_RoundTrip()
        {
            var scaledSpec = new EngineSpec
            {
                ScreenWidth = 128,
                ScreenHeight = 128,
                PhysicalWidth = 256,
                PhysicalHeight = 256,
            };
            var state = new DrawState(scaledSpec);
            var sprites = new SpriteStore(scaledSpec);
            var mem = new Pico8Memory(scaledSpec);
            var map = new MapStore(scaledSpec, mem);
            var buf = new PixelBuffer(scaledSpec, state, sprites, map);

            buf.Clear(0);
            buf.SetPixel(4, 6, 8, CoordMode.Virtual); // Scale=2, fills 2x2 block

            buf.FlushToRam(mem.Ram, mem.Layout.ScreenStart);
            buf.Clear(0);
            buf.LoadFromRam(mem.Ram, mem.Layout.ScreenStart);

            Assert.AreEqual(8, buf.GetPixel(4, 6, CoordMode.Virtual));
            // Verify physical block was filled (2x2)
            Assert.AreEqual(8, buf.Pixels[12 * 256 + 8]); // (4*2, 6*2) = (8,12)
            Assert.AreEqual(8, buf.Pixels[12 * 256 + 9]);
            Assert.AreEqual(8, buf.Pixels[13 * 256 + 8]);
            Assert.AreEqual(8, buf.Pixels[13 * 256 + 9]);
        }

        [Test]
        public void Memcpy_ScreenScroll_MovesPixels()
        {
            // Uses Pico8Api to test the full Memcpy → sync pipeline
            var api = new Pico8Api(
                _spec, _mem, _state, new Palette(_spec),
                _buffer, _sprites, _map,
                new StubAudio(), new StubInput());

            api.Cls(0);
            api.Pset(63, 127, 7); // white pixel on last row

            int scrStart = _mem.Layout.ScreenStart;
            int bytesPerRow = _spec.ScreenWidth / 2;

            // Scroll up 1 row: copy row 1→0, row 2→1, ..., row 127→126
            api.Memcpy(scrStart, scrStart + bytesPerRow, bytesPerRow * 127);

            // The pixel should now be on row 126 (scrolled up)
            Assert.AreEqual(7, api.Pget(63, 126));
        }

        private class StubAudio : IAudio
        {
            public void Sfx(int n) { }
            public void Sfx(int n, int channel, int offset, int length) { }
            public void SystemSfx(int n) { }
            public void Music(int n) { }
            public void Music(int n, int fadeLen, int channelMask) { }
            public void LoadSfx(string sfxData) { }
            public void LoadMusic(string musicData) { }
            public void LoadSystemSfx(string sfxData) { }
            public void ProcessAudio(float[] data, int channels) { }
            public int Volume { get; set; } = 8;
            public bool IsMuted { get; set; } = false;
        }

        private class StubInput : IInput
        {
            public bool Btn(int b, int p = 0) => false;
            public bool Btnp(int b, int p = 0) => false;
            public void SetButton(int button, int player, bool pressed) { }
            public void SetMouse(float x, float y) { }
            public void SetAxis(int player, int stick, float x, float y) { }
            public float GetAxisX(int player, int stick) => 0f;
            public float GetAxisY(int player, int stick) => 0f;
            public float MouseX => 0;
            public float MouseY => 0;
            public void Update() { }
        }
    }
}
