using NUnit.Framework;
using UnityEngine;

namespace Hatiora.Pico8.Unity.Tests
{
    [TestFixture]
    public class SpriteLoaderTests
    {
        private Palette _palette;
        private MockTextureReader _reader;

        [SetUp]
        public void SetUp()
        {
            _palette = new Palette(EngineSpec.Pico8);
            _reader = new MockTextureReader();
        }

        [Test]
        public void Load_BlackTexture_AllZeros()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            _reader.SetPixels(4, 4, new Color32(0, 0, 0, 255));

            var result = UnitySpriteLoader.Load(tex, _palette, _reader);

            Assert.AreEqual(16, result.Length);
            for (int i = 0; i < 16; i++)
                Assert.AreEqual(0, result[i]); // black = palette index 0
        }

        [Test]
        public void Load_RedPixel_MatchesColor8()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _reader.SetPixels(1, 1, new Color32(0xFF, 0x00, 0x4D, 255));

            var result = UnitySpriteLoader.Load(tex, _palette, _reader);
            Assert.AreEqual(8, result[0]); // PICO-8 red = index 8
        }

        [Test]
        public void Load_TransparentPixel_ReturnsZero()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _reader.SetPixels(1, 1, new Color32(0xFF, 0x00, 0x4D, 0)); // alpha=0

            var result = UnitySpriteLoader.Load(tex, _palette, _reader);
            Assert.AreEqual(0, result[0]); // transparent → 0
        }

        [Test]
        public void Load_NearColorMatch_FindsClosest()
        {
            // Slightly off-white — should match palette index 7 (white = 0xFF, 0xF1, 0xE8)
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _reader.SetPixels(1, 1, new Color32(0xFE, 0xF0, 0xE7, 255));

            var result = UnitySpriteLoader.Load(tex, _palette, _reader);
            Assert.AreEqual(7, result[0]); // closest match to white
        }

        [Test]
        public void LoadBank_RegistersInStore()
        {
            var spec = EngineSpec.Pico8;
            var store = new SpriteStore(spec);
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            _reader.SetPixels(8, 8, new Color32(0, 0, 0, 255));

            UnitySpriteLoader.LoadBank(store, 0, tex, _palette, _reader);

            Assert.AreEqual(1, store.BankCount);
            Assert.AreEqual(8, store.GetBankWidth(0));
            Assert.AreEqual(8, store.GetBankHeight(0));
        }

        [Test]
        public void LoadBank_MultipleBank_Independent()
        {
            var spec = EngineSpec.Pico8;
            var store = new SpriteStore(spec);

            var tex1 = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            _reader.SetPixels(8, 8, new Color32(0, 0, 0, 255));
            UnitySpriteLoader.LoadBank(store, 0, tex1, _palette, _reader);

            var tex2 = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            _reader.SetPixels(16, 16, new Color32(0, 0, 0, 255));
            UnitySpriteLoader.LoadBank(store, 1, tex2, _palette, _reader);

            Assert.AreEqual(2, store.BankCount);
            Assert.AreEqual(8, store.GetBankWidth(0));
            Assert.AreEqual(16, store.GetBankWidth(1));
        }

        [Test]
        public void Load_DefaultReader_Works()
        {
            // Exercise the 2-arg overload that uses UnityTextureReader.Instance
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color32[4];
            for (int i = 0; i < 4; i++)
                pixels[i] = new Color32(0, 0, 0, 255);
            tex.SetPixelData(pixels, 0);
            tex.Apply();

            var result = UnitySpriteLoader.Load(tex, _palette);
            Assert.AreEqual(4, result.Length);
        }

        // ─── UnityTextureReader direct tests ───

        [Test]
        public void UnityTextureReader_ReadableTexture_ReturnsSamePixels()
        {
            var reader = UnityTextureReader.Instance;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var pixels = new Color32[4];
            for (int i = 0; i < 4; i++)
                pixels[i] = new Color32(0xFF, 0, 0, 255);
            tex.SetPixelData(pixels, 0);
            tex.Apply();

            var result = reader.ReadPixels(tex, out int w, out int h);
            Assert.AreEqual(2, w);
            Assert.AreEqual(2, h);
            Assert.AreEqual(4, result.Length);
        }

        [Test]
        public void UnityTextureReader_NonReadableTexture_StillReturnsPixels()
        {
            var reader = UnityTextureReader.Instance;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            for (int i = 0; i < 16; i++)
                pixels[i] = new Color32(0xFF, 0, 0x4D, 255);
            tex.SetPixelData(pixels, 0);
            tex.Apply(false, true); // makeNoLongerReadable=true

            Assert.IsFalse(tex.isReadable);
            var result = reader.ReadPixels(tex, out int w, out int h);
            Assert.AreEqual(4, w);
            Assert.AreEqual(4, h);
            Assert.AreEqual(16, result.Length);
        }

        // ─── Mock ───

        private class MockTextureReader : ITextureReader
        {
            private Color32[] _pixels;
            private int _w, _h;

            public void SetPixels(int w, int h, Color32 fill)
            {
                _w = w;
                _h = h;
                _pixels = new Color32[w * h];
                for (int i = 0; i < _pixels.Length; i++)
                    _pixels[i] = fill;
            }

            public Color32[] ReadPixels(Texture2D texture, out int width, out int height)
            {
                width = _w;
                height = _h;
                return _pixels;
            }
        }
    }
}
