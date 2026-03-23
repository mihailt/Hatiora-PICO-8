using NUnit.Framework;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class PaletteTests
    {
        [Test]
        public void Pico8Rgb_Has48Bytes()
        {
            Assert.AreEqual(48, Palette.Pico8Rgb.Length); // 16 * 3
        }

        [Test]
        public void Pico8Rgb_Color0_IsBlack()
        {
            Assert.AreEqual(0x00, Palette.Pico8Rgb[0]);
            Assert.AreEqual(0x00, Palette.Pico8Rgb[1]);
            Assert.AreEqual(0x00, Palette.Pico8Rgb[2]);
        }

        [Test]
        public void Pico8Rgb_Color8_IsRed()
        {
            Assert.AreEqual(0xFF, Palette.Pico8Rgb[24]); // R
            Assert.AreEqual(0x00, Palette.Pico8Rgb[25]); // G
            Assert.AreEqual(0x4D, Palette.Pico8Rgb[26]); // B
        }

        [Test]
        public void Pico8Rgb_Color7_IsWhite()
        {
            Assert.AreEqual(0xFF, Palette.Pico8Rgb[21]); // R
            Assert.AreEqual(0xF1, Palette.Pico8Rgb[22]); // G
            Assert.AreEqual(0xE8, Palette.Pico8Rgb[23]); // B
        }

        [Test]
        public void Palette_LoadsFromSpec()
        {
            var spec = EngineSpec.Pico8;
            var palette = new Palette(spec);
            Assert.AreEqual(16, palette.Size);
            Assert.AreEqual(0x00, palette.R[0]); // black
            Assert.AreEqual(0xFF, palette.R[8]); // red
        }

        [Test]
        public void Palette_CustomSize()
        {
            var spec = new EngineSpec
            {
                PaletteSize = 4,
                DefaultPalette = new byte[]
                {
                    0xFF, 0x00, 0x00,
                    0x00, 0xFF, 0x00,
                    0x00, 0x00, 0xFF,
                    0xFF, 0xFF, 0xFF,
                }
            };
            var palette = new Palette(spec);
            Assert.AreEqual(4, palette.Size);
            Assert.AreEqual(0xFF, palette.R[0]);
            Assert.AreEqual(0x00, palette.G[0]);
            Assert.AreEqual(0xFF, palette.B[2]);
        }

        [Test]
        public void Palette_ExcessSize_ZeroFilled()
        {
            var spec = new EngineSpec
            {
                PaletteSize = 32,
                DefaultPalette = Palette.Pico8Rgb // only 16 colors
            };
            var palette = new Palette(spec);
            Assert.AreEqual(32, palette.Size);
            // Colors 16-31 should be zero
            Assert.AreEqual(0, palette.R[16]);
            Assert.AreEqual(0, palette.G[16]);
            Assert.AreEqual(0, palette.B[16]);
        }
    }
}
