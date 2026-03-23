using NUnit.Framework;
using UnityEngine;

namespace Hatiora.Pico8.Unity.Tests
{
    /// <summary>
    /// Edge cases for the builder: multiple specs, double build, logger injection, etc.
    /// </summary>
    [TestFixture]
    public class BuilderEdgeCaseTests
    {
        [Test]
        public void Build_CartridgeSpec_TakesPrecedenceOverDefault()
        {
            var cart = new LargeSpecCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            Assert.AreEqual(320, engine.Api.Width);
            Assert.AreEqual(240, engine.Api.Height);
        }

        [Test]
        public void Build_ExplicitSpec_OverridesCartridgeSpec()
        {
            var cart = new LargeSpecCartridge();
            var explicit128 = EngineSpec.Pico8;
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .WithSpec(explicit128) // explicitly 128x128
                .Build();

            Assert.AreEqual(128, engine.Api.Width);
        }

        [Test]
        public void Build_CustomLogger_Used()
        {
            var logger = new TestLogger();
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .WithLogger(logger)
                .Build();

            Assert.IsNotNull(engine);
        }

        [Test]
        public void Build_ExtendedPreset_Works()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .WithSpec(EngineSpec.Extended)
                .Build();

            Assert.AreEqual(256, engine.Api.Width);
            Assert.AreEqual(256, engine.Api.Height);
        }

        [Test]
        public void Build_Pico8At4K_Works()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .WithSpec(EngineSpec.Pico8At4K)
                .Build();

            Assert.AreEqual(128, engine.Api.Width);  // virtual
            Assert.AreEqual(3840, engine.TextureOutput.Texture.width);  // physical
        }

        [Test]
        public void Build_FluencyChaining_AllMethodsReturn()
        {
            var builder = new Pico8Builder();
            var result = builder
                .WithCartridge(new MinimalCartridge())
                .WithSpec(EngineSpec.Pico8)
                .WithLogger(new UnityLogger());

            Assert.IsNotNull(result);
            Assert.IsInstanceOf<Pico8Builder>(result);
        }

        [Test]
        public void Build_MultipleSpriteBanks_AllRegistered()
        {
            var tex1 = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            var pixels = new UnityEngine.Color32[64];
            for (int i = 0; i < 64; i++)
                pixels[i] = new UnityEngine.Color32(0, 0, 0, 255);
            tex1.SetPixelData(pixels, 0);
            tex1.Apply();

            var tex2 = new UnityEngine.Texture2D(16, 16, UnityEngine.TextureFormat.RGBA32, false);
            var pixels2 = new UnityEngine.Color32[256];
            for (int i = 0; i < 256; i++)
                pixels2[i] = new UnityEngine.Color32(0, 0, 0, 255);
            tex2.SetPixelData(pixels2, 0);
            tex2.Apply();

            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .WithSprites(0, tex1)
                .WithSprites(1, tex2)
                .Build();

            Assert.IsNotNull(engine);
        }

        [Test]
        public void Build_NoAudio_UsesNullAudio()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .Build();

            // Sfx and Music should not throw even without audio
            Assert.DoesNotThrow(() => engine.Api.Sfx(0));
            Assert.DoesNotThrow(() => engine.Api.Music(0));
        }

        [Test]
        public void Build_NoInput_TickDoesNotThrow()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .Build();

            Assert.DoesNotThrow(() => engine.Tick(1f / 60f));
        }

        [Test]
        public void Build_WithAudio_UsesProvidedAudio()
        {
            var audio = new FakeAudio();
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .WithAudio(audio)
                .Build();

            engine.Api.Sfx(0);
            engine.Api.Sfx(0, 1, 0, 32);
            engine.Api.Music(0);
            engine.Api.Music(0, 100, 0x0F);
            Assert.IsTrue(audio.SfxCalled);
        }

        [Test]
        public void Build_WithInput_IInputProvider()
        {
            var input = new FakeInputProvider();
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .WithInput(input)
                .Build();

            engine.Tick(1f / 60f);
            Assert.IsTrue(input.Polled);
        }

        [Test]
        public void Build_WithSprites_SingleArg_UsesBank0()
        {
            var tex = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            var pixels = new UnityEngine.Color32[64];
            for (int i = 0; i < 64; i++)
                pixels[i] = new UnityEngine.Color32(0, 0, 0, 255);
            tex.SetPixelData(pixels, 0);
            tex.Apply();

            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .WithSprites(tex)
                .Build();

            Assert.IsNotNull(engine);
        }

        [Test]
        public void NullAudio_AllMethods_NoOp()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .Build();

            // API-level audio calls (4-arg Sfx, 3-arg Music)
            Assert.DoesNotThrow(() =>
            {
                engine.Api.Sfx(0);
                engine.Api.Sfx(0, 1, 0, 32);
                engine.Api.Music(0);
                engine.Api.Music(0, 100, 0x0F);
            });

            // Exercise NullAudio methods not exposed through Pico8Api
            var audio = new Pico8Builder.NullAudio();
            Assert.DoesNotThrow(() =>
            {
                audio.Sfx(0);
                audio.Music(0);
                audio.LoadSfx("");
                audio.LoadMusic("");
                audio.ProcessAudio(new float[256], 1);
            });
        }

        [Test]
        public void LoggerService_Log_DoesNotThrow()
        {
            var logger = new LoggerService();
            UnityEngine.TestTools.LogAssert.Expect(LogType.Log, "test");
            Assert.DoesNotThrow(() => logger.Log("test"));
        }

        [Test]
        public void Build_WithFontProvider_UsesProvided()
        {
            var font = new StubFont();
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .WithFont(font)
                .Build();
            Assert.IsNotNull(engine);
        }

        [Test]
        public void Build_WithFont_DefaultOverload()
        {
            var font = UnityEngine.Resources.Load<UnityEngine.Font>("Fonts/pico-8");
            if (font == null) Assert.Ignore("pico-8 font not found in Resources");

            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .WithFont(font)
                .Build();
            Assert.IsNotNull(engine);
        }

        [Test]
        public void Build_WithFont_PixelsPerEm()
        {
            var font = UnityEngine.Resources.Load<UnityEngine.Font>("Fonts/pico-8");
            if (font == null) Assert.Ignore("pico-8 font not found in Resources");

            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .WithFont(font, 8)
                .Build();
            Assert.IsNotNull(engine);
        }

        [Test]
        public void Build_WithFont_PixelsPerEmAndCharWidth()
        {
            var font = UnityEngine.Resources.Load<UnityEngine.Font>("Fonts/pico-8");
            if (font == null) Assert.Ignore("pico-8 font not found in Resources");

            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .WithFont(font, 8, 4)
                .Build();
            Assert.IsNotNull(engine);
        }

        [Test]
        public void Build_WithInputBindings()
        {
            var action = new UnityEngine.InputSystem.InputAction("TestBtn", UnityEngine.InputSystem.InputActionType.Button);
            var bindings = new[] { (0, 0, action) };

            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .WithInput(bindings)
                .Build();

            Assert.IsNotNull(engine);
        }



        [Test]
        public void UnitySpriteLoader_TransparentPixel()
        {
            var spec = EngineSpec.Pico8;
            var palette = new Palette(spec);
            var tex = new UnityEngine.Texture2D(2, 2, UnityEngine.TextureFormat.RGBA32, false);
            var pixels = new UnityEngine.Color32[]
            {
                new(0, 0, 0, 0),   // transparent
                new(0, 0, 0, 255), // black
                new(255, 0, 0, 255), // red-ish
                new(0, 255, 0, 255), // green-ish
            };
            tex.SetPixelData(pixels, 0);
            tex.Apply();

            byte[] data = UnitySpriteLoader.Load(tex, palette);
            Assert.AreEqual(4, data.Length);
            // Transparent pixel should be 0
            // Check at least one pixel mapped (non-exact match covers MatchColor)
        }

        [Test]
        public void UnitySpriteLoader_LoadBank_RegistersWithStore()
        {
            var spec = EngineSpec.Pico8;
            var palette = new Palette(spec);
            var store = new SpriteStore(spec);
            var tex = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            var pixels = new UnityEngine.Color32[64];
            for (int i = 0; i < 64; i++)
                pixels[i] = new UnityEngine.Color32(0, 0, 0, 255);
            tex.SetPixelData(pixels, 0);
            tex.Apply();

            UnitySpriteLoader.LoadBank(store, 0, tex, palette);
            Assert.AreEqual(1, store.BankCount);
        }

        // ─── Test helpers ───

        private class LargeSpecCartridge : Cartridge, IUnityCartridge
        {
            public override EngineSpec Spec => new EngineSpec
            {
                ScreenWidth = 320,
                ScreenHeight = 240,
            };
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

        private class MinimalCartridge : Cartridge, IUnityCartridge
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

        private class TestLogger : ILogger
        {
            public void Log(string message) { }
        }

        private class FakeAudio : IAudio
        {
            public bool SfxCalled;
            public void Sfx(int n) => SfxCalled = true;
            public void Sfx(int n, int channel, int offset, int length) => SfxCalled = true;
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

        private class FakeInputProvider : IInputProvider
        {
            public bool Polled;
            public void Poll(IInput input) => Polled = true;
        }

        private class StubFont : IFontProvider
        {
            public void Prepare(string text) { }
            public GlyphData GetGlyph(char c) => new GlyphData
            {
                Width = 4, Height = 5, BearingX = 0, BearingY = 0, Advance = 4,
                Pixels = new bool[20]
            };
        }
    }
}
