using NUnit.Framework;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class EngineSpecTests
    {
        [Test]
        public void Pico8Preset_HasCorrectDefaults()
        {
            var spec = EngineSpec.Pico8;

            Assert.AreEqual(128, spec.ScreenWidth);
            Assert.AreEqual(128, spec.ScreenHeight);
            Assert.AreEqual(8, spec.SpriteSize);
            Assert.AreEqual(128, spec.SheetWidth);
            Assert.AreEqual(128, spec.SheetHeight);
            Assert.AreEqual(128, spec.MapWidth);
            Assert.AreEqual(32, spec.MapHeight);
            Assert.AreEqual(16, spec.PaletteSize);
            Assert.AreEqual(64, spec.MaxSfx);
            Assert.AreEqual(64, spec.MaxMusic);
            Assert.AreEqual(4, spec.AudioChannels);
            Assert.AreEqual(6, spec.DefaultColor);
        }

        [Test]
        public void SpritesPerRow_ComputedCorrectly()
        {
            var spec = EngineSpec.Pico8;
            Assert.AreEqual(16, spec.SpritesPerRow); // 128 / 8
        }

        [Test]
        public void SpritesPerBank_ComputedCorrectly()
        {
            var spec = EngineSpec.Pico8;
            Assert.AreEqual(256, spec.SpritesPerBank); // 16 * 16
        }

        [Test]
        public void Scale_DefaultIsOne_WhenNoPhysicalSize()
        {
            var spec = EngineSpec.Pico8;
            Assert.AreEqual(1, spec.Scale);
        }

        [Test]
        public void Scale_ComputedFromPhysical()
        {
            var spec = EngineSpec.Pico8At4K;
            // min(3840/128, 2160/128) = min(30, 16) = 16
            Assert.AreEqual(16, spec.Scale);
        }

        [Test]
        public void EffectivePhysical_FallsBackToVirtual()
        {
            var spec = EngineSpec.Pico8;
            Assert.AreEqual(128, spec.EffectivePhysW);
            Assert.AreEqual(128, spec.EffectivePhysH);
        }

        [Test]
        public void EffectivePhysical_UsesExplicitValues()
        {
            var spec = EngineSpec.Pico8At4K;
            Assert.AreEqual(3840, spec.EffectivePhysW);
            Assert.AreEqual(2160, spec.EffectivePhysH);
        }

        [Test]
        public void CustomSpec_AllowsArbitraryValues()
        {
            var spec = new EngineSpec
            {
                ScreenWidth = 480,
                ScreenHeight = 270,
                SpriteSize = 16,
                SheetWidth = 256,
                SheetHeight = 256,
                PaletteSize = 32,
            };

            Assert.AreEqual(480, spec.ScreenWidth);
            Assert.AreEqual(16, spec.SpritesPerRow); // 256 / 16
            Assert.AreEqual(256, spec.SpritesPerBank); // 16 * 16
            Assert.AreEqual(32, spec.PaletteSize);
        }

        [Test]
        public void ExtendedPreset_HasLargerValues()
        {
            var spec = EngineSpec.Extended;
            Assert.AreEqual(256, spec.ScreenWidth);
            Assert.AreEqual(256, spec.ScreenHeight);
            Assert.AreEqual(32, spec.PaletteSize);
            Assert.AreEqual(128, spec.MaxSfx);
            Assert.AreEqual(8, spec.AudioChannels);
        }
    }
}
