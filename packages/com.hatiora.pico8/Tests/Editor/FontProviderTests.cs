using NUnit.Framework;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace Hatiora.Pico8.Tests
{
    [TestFixture]
    public class FontProviderTests
    {
        [Test]
        public void Constructor_AutoDetect_UsesUnityFontEngine()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            var provider = new FontProvider(font);
            Assert.IsNotNull(provider);
        }

        [Test]
        public void Constructor_MockEngine_Success()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            var engine = new MockFontEngine
            {
                Error = FontEngineError.Success,
                Face = new FaceInfo { pointSize = 8, ascentLine = 8, descentLine = -2, lineHeight = 10 }
            };
            var provider = new FontProvider(font, engine);
            Assert.IsNotNull(provider);
        }

        [Test]
        public void Constructor_MockEngine_Failure_FallsBackTo4()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            var engine = new MockFontEngine { Error = FontEngineError.Invalid_File };
            // DetectPixelsPerEm returns 4 on failure, so step = 32/4 = 8
            var provider = new FontProvider(font, engine);
            Assert.IsNotNull(provider);
            Assert.IsTrue(provider.ToString().Contains("step=8"));
        }

        [Test]
        public void Constructor_MockEngine_LowPixelsPerEm_FallsBackTo4()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            // Set face metrics so GCD produces pixelsPerEm <= 2
            // pointSize=10, all metrics are multiples of 5 → GCD=5, pixelsPerEm=10/5=2 → triggers fallback
            var engine = new MockFontEngine
            {
                Error = FontEngineError.Success,
                Face = new FaceInfo { pointSize = 10, ascentLine = 5, descentLine = -5, lineHeight = 10 }
            };
            var provider = new FontProvider(font, engine);
            Assert.IsNotNull(provider);
            Assert.IsTrue(provider.ToString().Contains("step=8")); // 32/4 = 8
        }

        [Test]
        public void Constructor_ExplicitPixelsPerEm_CreatesProvider()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            var provider = new FontProvider(font, 8);
            Assert.IsNotNull(provider);
        }

        [Test]
        public void Constructor_WithCharWidth_CreatesProvider()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            var provider = new FontProvider(font, 8, 4);
            Assert.IsNotNull(provider);
        }

        [Test]
        public void Prepare_DoesNotThrow()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            var provider = new FontProvider(font, 8);
            Assert.DoesNotThrow(() => provider.Prepare("Hello"));
        }

        [Test]
        public void GetGlyph_ReturnsData()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            var provider = new FontProvider(font, 8);
            provider.Prepare("A");
            var glyph = provider.GetGlyph('A');
            Assert.IsTrue(glyph.Advance > 0);
        }

        [Test]
        public void GetGlyph_WithoutPrepare_ReturnsDefault()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            var provider = new FontProvider(font, 8);
            var glyph = provider.GetGlyph('A'); // _atlasCache is null
            Assert.AreEqual(0, glyph.Width);
            Assert.AreEqual(0, glyph.Advance);
        }

        [Test]
        public void GetGlyph_MissingChar_ReturnsDefault()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            var provider = new FontProvider(font, 8);
            provider.Prepare("A"); // builds atlas
            var glyph = provider.GetGlyph('\uFFFF'); // non-existent char
            Assert.AreEqual(0, glyph.Width);
            Assert.AreEqual(0, glyph.Advance);
        }

        [Test]
        public void GetGlyph_SpaceChar_ReturnsAdvanceOnly()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            var provider = new FontProvider(font, 8);
            provider.Prepare(" "); // space has no visible pixels
            var glyph = provider.GetGlyph(' ');
            Assert.AreEqual(0, glyph.Width);
            Assert.AreEqual(0, glyph.Height);
            Assert.IsTrue(glyph.Advance > 0);
        }

        [Test]
        public void ToString_IncludesInfo()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            var provider = new FontProvider(font, 8);
            var str = provider.ToString();
            Assert.IsTrue(str.Contains("FontProvider"));
            Assert.IsTrue(str.Contains("step="));
        }

        [Test]
        public void Constructor_MockEngine_ZeroMetrics()
        {
            var font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            // All metrics zero — GCD stays at pointSize, pixelsPerEm = 1 → triggers fallback
            var engine = new MockFontEngine
            {
                Error = FontEngineError.Success,
                Face = new FaceInfo { pointSize = 8, ascentLine = 0, descentLine = 0, lineHeight = 0 }
            };
            var provider = new FontProvider(font, engine);
            Assert.IsNotNull(provider);
        }

        // ─── Mock ───

        private class MockFontEngine : IFontEngine
        {
            public FontEngineError Error;
            public FaceInfo Face;

            public FontEngineError LoadFontFace(Font font) => Error;
            public FaceInfo GetFaceInfo() => Face;
        }
    }
}
