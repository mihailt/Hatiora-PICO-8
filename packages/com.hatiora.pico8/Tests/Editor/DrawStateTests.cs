using NUnit.Framework;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class DrawStateTests
    {
        private EngineSpec _spec;
        private DrawState _state;

        [SetUp]
        public void SetUp()
        {
            _spec = EngineSpec.Pico8;
            _state = new DrawState(_spec);
        }

        [Test]
        public void Reset_SetsDefaultColor()
        {
            _state.CurrentColor = 12;
            _state.Reset();
            Assert.AreEqual(6, _state.CurrentColor); // PICO-8 default
        }

        [Test]
        public void Reset_ClearsCamera()
        {
            _state.CameraX = 50;
            _state.CameraY = 30;
            _state.Reset();
            Assert.AreEqual(0, _state.CameraX);
            Assert.AreEqual(0, _state.CameraY);
        }

        [Test]
        public void Reset_SetsClipToFullScreen()
        {
            _state.ClipW = 10;
            _state.ClipH = 10;
            _state.Reset();
            Assert.AreEqual(0, _state.ClipX);
            Assert.AreEqual(0, _state.ClipY);
            Assert.AreEqual(128, _state.ClipW);
            Assert.AreEqual(128, _state.ClipH);
        }

        [Test]
        public void DrawPalette_InitializedAsIdentity()
        {
            for (int i = 0; i < 16; i++)
                Assert.AreEqual(i, _state.DrawPalette[i]);
        }

        [Test]
        public void DisplayPalette_InitializedAsIdentity()
        {
            for (int i = 0; i < 16; i++)
                Assert.AreEqual(i, _state.DisplayPalette[i]);
        }

        [Test]
        public void Transparency_Color0_TransparentByDefault()
        {
            Assert.IsTrue(_state.Transparency[0]);
            for (int i = 1; i < 16; i++)
                Assert.IsFalse(_state.Transparency[i]);
        }

        [Test]
        public void PaletteArrays_SizedFromSpec()
        {
            var bigSpec = new EngineSpec { PaletteSize = 32 };
            var bigState = new DrawState(bigSpec);
            Assert.AreEqual(32, bigState.DrawPalette.Length);
            Assert.AreEqual(32, bigState.DisplayPalette.Length);
            Assert.AreEqual(32, bigState.Transparency.Length);
        }

        [Test]
        public void ClipReset_UsesSpecScreenSize()
        {
            var custom = new EngineSpec { ScreenWidth = 256, ScreenHeight = 256 };
            var state = new DrawState(custom);
            Assert.AreEqual(256, state.ClipW);
            Assert.AreEqual(256, state.ClipH);
        }

        [Test]
        public void FillPattern_DefaultsToZero()
        {
            Assert.AreEqual(0, _state.FillPattern);
        }
    }
}
