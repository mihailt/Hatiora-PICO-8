using NUnit.Framework;

namespace Hatiora.Pico8.Unity.Tests
{
    /// <summary>
    /// Tests the full pipeline: Builder → Engine → API → PixelBuffer → TextureGraphics.
    /// Verifies that drawing through the API produces correct pixel output.
    /// </summary>
    [TestFixture]
    public class IntegrationTests
    {
        private Pico8Engine _engine;

        [SetUp]
        public void SetUp()
        {
            _engine = new Pico8Builder()
                .WithCartridge(new EmptyCartridge())
                .Build();
        }

        // ─── Drawing primitives end-to-end ───

        [Test]
        public void Cls_ThenPresent_ChangesTexture()
        {
            _engine.Api.Cls(8); // red
            _engine.TextureOutput.Present();

            var pixels = _engine.TextureOutput.Texture.GetPixels32();
            Assert.AreEqual(0xFF, pixels[0].r);
            Assert.AreEqual(0x00, pixels[0].g);
            Assert.AreEqual(0x4D, pixels[0].b);
        }

        [Test]
        public void Pset_SinglePixel_Visible()
        {
            _engine.Api.Cls(0);
            _engine.Api.Pset(0, 0, 7); // white at top-left
            _engine.TextureOutput.Present();

            var pixels = _engine.TextureOutput.Texture.GetPixels32();
            // top-left in game = bottom-left in Unity (Y-flip), last row
            var p = pixels[127 * 128]; // row 127, col 0
            Assert.AreEqual(0xFF, p.r);
            Assert.AreEqual(0xF1, p.g);
            Assert.AreEqual(0xE8, p.b);
        }

        [Test]
        public void Rectfill_AreaHasColor()
        {
            _engine.Api.Cls(0);
            _engine.Api.Rectfill(10, 10, 20, 20, 3); // dark-green
            _engine.TextureOutput.Present();

            var pixels = _engine.TextureOutput.Texture.GetPixels32();
            // Pixel at (15, 15) in game = (15, 128-1-15=112) in Unity
            var p = pixels[112 * 128 + 15];
            Assert.AreEqual(0x00, p.r);
            Assert.AreEqual(0x87, p.g);
            Assert.AreEqual(0x51, p.b);
        }

        [Test]
        public void Line_PixelsAreSet()
        {
            _engine.Api.Cls(0);
            _engine.Api.Line(0, 0, 10, 0, 11); // green horizontal line
            _engine.TextureOutput.Present();

            var pixels = _engine.TextureOutput.Texture.GetPixels32();
            // row 0 in game = row 127 in Unity
            for (int x = 0; x <= 10; x++)
            {
                var p = pixels[127 * 128 + x];
                Assert.AreEqual(0x00, p.r, $"x={x}");
                Assert.AreEqual(0xE4, p.g, $"x={x}");
                Assert.AreEqual(0x36, p.b, $"x={x}");
            }
        }

        // ─── Palette operations ───

        [Test]
        public void Pal_DrawPalette_RemapsColorBeforeBuffer()
        {
            _engine.Api.Cls(0);
            _engine.Api.Pal(7, 8); // remap white → red at draw time
            _engine.Api.Pset(5, 5, 7);
            _engine.Api.Pal(); // reset
            _engine.TextureOutput.Present();

            var pixels = _engine.TextureOutput.Texture.GetPixels32();
            var p = pixels[(127 - 5) * 128 + 5];
            // Should be red (8), not white (7)
            Assert.AreEqual(0xFF, p.r);
            Assert.AreEqual(0x00, p.g);
            Assert.AreEqual(0x4D, p.b);
        }

        [Test]
        public void Pal_DisplayPalette_RemapsAtOutput()
        {
            _engine.Api.Cls(0);
            _engine.Api.Pset(5, 5, 7); // white in buffer
            _engine.Api.Pal(7, 12, 1); // display remap: white → blue
            _engine.TextureOutput.Present();

            var pixels = _engine.TextureOutput.Texture.GetPixels32();
            var p = pixels[(127 - 5) * 128 + 5];
            Assert.AreEqual(0x29, p.r); // blue
            Assert.AreEqual(0xAD, p.g);
            Assert.AreEqual(0xFF, p.b);
        }

        // ─── Camera ───

        [Test]
        public void Camera_ShiftsDrawPosition()
        {
            _engine.Api.Cls(0);
            _engine.Api.Camera(5, 5);
            _engine.Api.Pset(10, 10, 7); // virtual (10,10) → physical (5,5)
            _engine.Api.Camera(0, 0);
            _engine.TextureOutput.Present();

            var pixels = _engine.TextureOutput.Texture.GetPixels32();
            // physical (5,5) in game = Unity row 122, col 5
            var p = pixels[122 * 128 + 5];
            Assert.AreEqual(0xFF, p.r); // white
        }

        // ─── Clip ───

        [Test]
        public void Clip_PreventsDrawOutside()
        {
            _engine.Api.Cls(0);
            _engine.Api.Clip(10, 10, 20, 20);
            _engine.Api.Pset(5, 5, 7); // outside clip
            _engine.Api.Pset(15, 15, 7); // inside clip
            _engine.Api.Clip(); // reset
            _engine.TextureOutput.Present();

            var pixels = _engine.TextureOutput.Texture.GetPixels32();
            var outside = pixels[(127 - 5) * 128 + 5];
            var inside = pixels[(127 - 15) * 128 + 15];

            Assert.AreEqual(0x00, outside.r); // clipped, still black
            Assert.AreEqual(0xFF, inside.r); // drawn, white
        }

        // ─── Memory through API ───

        [Test]
        public void MemcpyMemset_ThroughApi()
        {
            _engine.Api.Poke(100, 0xAA);
            Assert.AreEqual(0xAA, _engine.Api.Peek(100));

            _engine.Api.Memset(200, 0xBB, 5);
            Assert.AreEqual(0xBB, _engine.Api.Peek(200));
            Assert.AreEqual(0xBB, _engine.Api.Peek(204));
            Assert.AreEqual(0x00, _engine.Api.Peek(205));

            _engine.Api.Memcpy(300, 200, 5);
            Assert.AreEqual(0xBB, _engine.Api.Peek(300));
        }

        // ─── Map through API ───

        [Test]
        public void MgetMset_ThroughApi()
        {
            _engine.Api.Mset(10, 5, 42);
            Assert.AreEqual(42, _engine.Api.Mget(10, 5));
        }

        [Test]
        public void FgetFset_ThroughApi()
        {
            _engine.Api.Fset(10, 0, true);
            Assert.AreEqual(1, _engine.Api.Fget(10, 0));
            Assert.AreEqual(0, _engine.Api.Fget(10, 1));
        }

        private class EmptyCartridge : Cartridge, IUnityCartridge
        {
            public override void Init() { }
            public override void Update() { }
            public override void Draw() { }
            public UnityEngine.Texture2D GfxTexture { get; } = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            public UnityEngine.Texture2D LabelTexture => null;
            public string SfxData => null;
            public string MusicData => null;
            public string MapData => null;
            public string GffData => null;
        }
    }
}
