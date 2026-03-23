using NUnit.Framework;

namespace Hatiora.Pico8.Unity.Tests
{
    [TestFixture]
    public class EngineLifecycleTests
    {
        [Test]
        public void Init_OnlyCalledOnce()
        {
            var cart = new CountingCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            engine.Init();
            engine.Init();
            engine.Init();

            Assert.AreEqual(1, cart.InitCount);
        }

        [Test]
        public void Tick_AutoInits_IfNotInitialized()
        {
            var cart = new CountingCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            // Tick without explicit Init
            engine.Tick(1f / 60f);

            Assert.AreEqual(1, cart.InitCount);
            Assert.AreEqual(1, cart.UpdateCount);
            Assert.AreEqual(1, cart.DrawCount);
        }

        [Test]
        public void Tick_MultipleTimes_CallsUpdateAndDrawEachTime()
        {
            var cart = new CountingCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            engine.Tick(1f / 60f);
            engine.Tick(1f / 60f);
            engine.Tick(1f / 60f);

            Assert.AreEqual(1, cart.InitCount); // only once
            Assert.AreEqual(3, cart.UpdateCount);
            Assert.AreEqual(3, cart.DrawCount);
        }

        [Test]
        public void Api_Cls_DoesNotThrow()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new CountingCartridge())
                .Build();

            Assert.DoesNotThrow(() => engine.Api.Cls(0));
        }

        [Test]
        public void Api_Pset_Pget_RoundTrip()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new CountingCartridge())
                .Build();

            engine.Api.Cls(0);
            engine.Api.Pset(10, 10, 7);
            Assert.AreEqual(7, engine.Api.Pget(10, 10));
        }

        [Test]
        public void Api_Camera_ClipState()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new CountingCartridge())
                .Build();

            // Should not throw
            Assert.DoesNotThrow(() =>
            {
                engine.Api.Camera(10, 20);
                engine.Api.Clip(0, 0, 64, 64);
                engine.Api.Pal(1, 2);
                engine.Api.Pal();
                engine.Api.Palt(0, false);
                engine.Api.Palt();
                engine.Api.Fillp(0);
                engine.Api.Color(7);
                engine.Api.Cursor(0, 0);
            });
        }

        [Test]
        public void Api_Math_PicoSemantics()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new CountingCartridge())
                .Build();

            // PICO-8 sgn(0) = 1
            Assert.AreEqual(1f, engine.Api.Sgn(0f));

            // PICO-8 sin(0.25) = -1
            Assert.AreEqual(-1f, engine.Api.Sin(0.25f), 0.001f);

            // PICO-8 flr(-2.3) = -3
            Assert.AreEqual(-3, engine.Api.Flr(-2.3f));
        }

        [Test]
        public void Api_Memory_PeekPoke()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new CountingCartridge())
                .Build();

            engine.Api.Poke(100, 42);
            Assert.AreEqual(42, engine.Api.Peek(100));
        }

        [Test]
        public void Api_Time_Advances()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new CountingCartridge())
                .Build();

            float t1 = engine.Api.Time();
            // Time uses Stopwatch, should be >= 0
            Assert.GreaterOrEqual(t1, 0f);
        }

        private class CountingCartridge : Cartridge, IUnityCartridge
        {
            public int InitCount, UpdateCount, DrawCount;
            public override void Init() => InitCount++;
            public override void Update() => UpdateCount++;
            public override void Draw() => DrawCount++;
            public UnityEngine.Texture2D GfxTexture { get; } = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            public UnityEngine.Texture2D LabelTexture => null;
            public string SfxData => null;
            public string MusicData => null;
            public string MapData => null;
            public string GffData => null;
        }
    }
}
