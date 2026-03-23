using NUnit.Framework;
using UnityEngine;

namespace Hatiora.Pico8.Unity.Tests
{
    [TestFixture]
    public class TextureGraphicsTests
    {
        private EngineSpec _spec;
        private DrawState _state;
        private Palette _palette;
        private PixelBuffer _buffer;
        private TextureGraphics _gfx;

        [SetUp]
        public void SetUp()
        {
            _spec = EngineSpec.Pico8;
            _state = new DrawState(_spec);
            _palette = new Palette(_spec);
            var sprites = new SpriteStore(_spec);
            var mem = new Pico8Memory(_spec);
            var map = new MapStore(_spec, mem);
            _buffer = new PixelBuffer(_spec, _state, sprites, map);
            _gfx = new TextureGraphics(_buffer, _palette, _state);
        }

        [Test]
        public void Texture_IsCreated()
        {
            Assert.IsNotNull(_gfx.Texture);
        }

        [Test]
        public void Texture_HasCorrectDimensions()
        {
            Assert.AreEqual(128, _gfx.Texture.width);
            Assert.AreEqual(128, _gfx.Texture.height);
        }

        [Test]
        public void Texture_IsPointFiltered()
        {
            Assert.AreEqual(FilterMode.Point, _gfx.Texture.filterMode);
        }

        [Test]
        public void Present_DoesNotThrow()
        {
            _buffer.Clear(0);
            Assert.DoesNotThrow(() => _gfx.Present());
        }

        [Test]
        public void Present_ClearedBlack_AllPixelsBlack()
        {
            _buffer.Clear(0); // color 0 = black
            _gfx.Present();

            var pixels = _gfx.Texture.GetPixels32();
            // Check a sample pixel
            Assert.AreEqual(0x00, pixels[0].r);
            Assert.AreEqual(0x00, pixels[0].g);
            Assert.AreEqual(0x00, pixels[0].b);
        }

        [Test]
        public void Present_ClearedColor8_AllPixelsRed()
        {
            _buffer.Clear(8); // color 8 = red (0xFF, 0x00, 0x4D)
            _gfx.Present();

            var pixels = _gfx.Texture.GetPixels32();
            // Bottom-left in Unity = top-left in buffer (Y-flip)
            var p = pixels[0];
            Assert.AreEqual(0xFF, p.r);
            Assert.AreEqual(0x00, p.g);
            Assert.AreEqual(0x4D, p.b);
        }

        [Test]
        public void Present_DisplayPaletteRemap_Applied()
        {
            // Remap display palette: color 0 → color 8 (red)
            _state.DisplayPalette[0] = 8;

            _buffer.Clear(0);
            _gfx.Present();

            var pixels = _gfx.Texture.GetPixels32();
            Assert.AreEqual(0xFF, pixels[0].r); // remapped to red
        }

        [Test]
        public void Present_ScaledSpec_CreatesLargerTexture()
        {
            var scaledSpec = new EngineSpec
            {
                ScreenWidth = 128,
                ScreenHeight = 128,
                PhysicalWidth = 256,
                PhysicalHeight = 256,
            };
            var state = new DrawState(scaledSpec);
            var palette = new Palette(scaledSpec);
            var sprites = new SpriteStore(scaledSpec);
            var mem = new Pico8Memory(scaledSpec);
            var map = new MapStore(scaledSpec, mem);
            var buf = new PixelBuffer(scaledSpec, state, sprites, map);
            var gfx = new TextureGraphics(buf, palette, state);

            Assert.AreEqual(256, gfx.Texture.width);
            Assert.AreEqual(256, gfx.Texture.height);
        }
    }
}
