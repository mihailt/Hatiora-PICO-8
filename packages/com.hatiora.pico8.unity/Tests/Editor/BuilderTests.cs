using NUnit.Framework;

namespace Hatiora.Pico8.Unity.Tests
{
    [TestFixture]
    public class BuilderTests
    {
        [Test]
        public void Build_WithMinimalConfig_CreatesEngine()
        {
            var cart = new TestCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            Assert.IsNotNull(engine);
            Assert.IsNotNull(engine.Api);
            Assert.IsNotNull(engine.TextureOutput);
            Assert.IsNotNull(engine.TextureOutput.Texture);
        }

        [Test]
        public void Build_WithCustomSpec_UsesSpec()
        {
            var spec = new EngineSpec { ScreenWidth = 256, ScreenHeight = 256 };
            var engine = new Pico8Builder()
                .WithCartridge(new TestCartridge())
                .WithSpec(spec)
                .Build();

            Assert.AreEqual(256, engine.Api.Width);
            Assert.AreEqual(256, engine.Api.Height);
        }

        [Test]
        public void Build_DefaultSpec_IsPico8()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new TestCartridge())
                .Build();

            Assert.AreEqual(128, engine.Api.Width);
            Assert.AreEqual(128, engine.Api.Height);
        }

        [Test]
        public void Build_WithoutCartridge_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() =>
                new Pico8Builder().Build()
            );
        }

        [Test]
        public void Engine_Init_CallsCartridgeInit()
        {
            var cart = new TestCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            engine.Init();
            Assert.IsTrue(cart.InitCalled);
        }

        [Test]
        public void Engine_Tick_CallsUpdateAndDraw()
        {
            var cart = new TestCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            engine.Tick(1f / 60f);
            Assert.IsTrue(cart.UpdateCalled);
            Assert.IsTrue(cart.DrawCalled);
        }

        private class TestCartridge : Cartridge, IUnityCartridge
        {
            public bool InitCalled, UpdateCalled, DrawCalled;

            public override void Init() => InitCalled = true;
            public override void Update() => UpdateCalled = true;
            public override void Draw() => DrawCalled = true;
            public UnityEngine.Texture2D GfxTexture { get; } = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            public UnityEngine.Texture2D LabelTexture => null;
            public string SfxData => null;
            public string MusicData => null;
            public string MapData => null;
            public string GffData => null;
        }
    }
}
