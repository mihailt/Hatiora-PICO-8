using NUnit.Framework;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class MemoryTests
    {
        private EngineSpec _spec;
        private Pico8Memory _mem;

        [SetUp]
        public void SetUp()
        {
            _spec = EngineSpec.Pico8;
            _mem = new Pico8Memory(_spec);
        }

        [Test]
        public void Ram_SizeMatchesLayout()
        {
            Assert.AreEqual(_mem.Layout.TotalSize, _mem.Ram.Length);
        }

        [Test]
        public void Layout_RegionsAreSequential()
        {
            var l = _mem.Layout;
            Assert.AreEqual(0, l.GfxStart);
            Assert.Greater(l.MapStart, l.GfxStart);
            Assert.Greater(l.FlagsStart, l.MapStart);
            Assert.Greater(l.MusicStart, l.FlagsStart);
            Assert.Greater(l.SfxStart, l.MusicStart);
            Assert.Greater(l.DrawStateStart, l.SfxStart);
            Assert.Greater(l.ScreenStart, l.DrawStateStart);
        }

        [Test]
        public void Peek_OutOfBounds_ReturnsZero()
        {
            Assert.AreEqual(0, _mem.Peek(-1));
            Assert.AreEqual(0, _mem.Peek(_mem.Ram.Length));
            Assert.AreEqual(0, _mem.Peek(int.MaxValue));
        }

        [Test]
        public void Poke_OutOfBounds_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _mem.Poke(-1, 42));
            Assert.DoesNotThrow(() => _mem.Poke(_mem.Ram.Length, 42));
        }

        [Test]
        public void PeekPoke_RoundTrip()
        {
            _mem.Poke(100, 0xAB);
            Assert.AreEqual(0xAB, _mem.Peek(100));
        }

        [Test]
        public void Peek2Poke2_RoundTrip()
        {
            _mem.Poke2(200, 0x1234);
            Assert.AreEqual(0x1234, _mem.Peek2(200));
        }

        [Test]
        public void Peek4Poke4_RoundTrip()
        {
            int val = 0x12345678;
            _mem.Poke4(300, val);
            Assert.AreEqual(val, _mem.Peek4(300));
        }

        [Test]
        public void Memset_FillsRange()
        {
            _mem.Memset(0, 0xFF, 10);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(0xFF, _mem.Ram[i]);
            Assert.AreEqual(0, _mem.Ram[10]);
        }

        [Test]
        public void Memcpy_CopiesData()
        {
            for (int i = 0; i < 5; i++)
                _mem.Ram[i] = (byte)(i + 1);
            _mem.Memcpy(100, 0, 5);
            for (int i = 0; i < 5; i++)
                Assert.AreEqual(i + 1, _mem.Ram[100 + i]);
        }

        [Test]
        public void Memcpy_NegativeLength_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _mem.Memcpy(0, 10, -1));
        }

        [Test]
        public void Memset_NegativeLength_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _mem.Memset(0, 0, -1));
        }

        [Test]
        public void Layout_GfxSize_CorrectForPico8()
        {
            // 128 * 128 / 2 = 8192
            Assert.AreEqual(8192, _mem.Layout.GfxSize);
        }

        [Test]
        public void Layout_MapSize_CorrectForPico8()
        {
            // 128 * 32 = 4096
            Assert.AreEqual(4096, _mem.Layout.MapSize);
        }

        [Test]
        public void Layout_FlagsSize_CorrectForPico8()
        {
            // 256 sprites
            Assert.AreEqual(256, _mem.Layout.FlagsSize);
        }

        [Test]
        public void Layout_ScreenStart_PinnedAt0x6000()
        {
            Assert.AreEqual(0x6000, _mem.Layout.ScreenStart);
        }

        [Test]
        public void Layout_DrawStateStart_PinnedAt0x5F00()
        {
            Assert.AreEqual(0x5F00, _mem.Layout.DrawStateStart);
        }

        [Test]
        public void Layout_TotalSize_Is0x8000ForPico8()
        {
            // 0x6000 + 128*128/2 = 0x6000 + 0x2000 = 0x8000 = 32768
            Assert.AreEqual(0x8000, _mem.Layout.TotalSize);
        }

        [Test]
        public void Layout_ScreenSize_CorrectForPico8()
        {
            // 128 * 128 / 2 = 8192
            Assert.AreEqual(8192, _mem.Layout.ScreenSize);
        }

        [Test]
        public void Layout_Sequential_PacksWithNoGaps()
        {
            var spec = new EngineSpec { MemoryMap = MemoryMap.Sequential };
            var mem = new Pico8Memory(spec);
            var l = mem.Layout;

            // DrawState should follow right after SFX
            Assert.AreEqual(l.SfxStart + l.SfxSize, l.DrawStateStart);
            // Screen should follow right after DrawState
            Assert.AreEqual(l.DrawStateStart + l.DrawStateSize, l.ScreenStart);
        }

        [Test]
        public void Layout_CustomMap_UsesProvidedAddresses()
        {
            var spec = new EngineSpec
            {
                MemoryMap = new MemoryMap
                {
                    DrawStateAddress = 0x7000,
                    ScreenAddress = 0x8000,
                }
            };
            var mem = new Pico8Memory(spec);

            Assert.AreEqual(0x7000, mem.Layout.DrawStateStart);
            Assert.AreEqual(0x8000, mem.Layout.ScreenStart);
        }
    }
}
