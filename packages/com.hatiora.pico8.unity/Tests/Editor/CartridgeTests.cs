using NUnit.Framework;

namespace Hatiora.Pico8.Unity.Tests
{
    [TestFixture]
    public class CartridgeTests
    {
        [Test]
        public void Bind_SetsP8Api()
        {
            var cart = new InspectableCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            engine.Init();
            Assert.IsTrue(cart.HasApi);
        }

        [Test]
        public void ConvenienceMethods_DelegateToPico8()
        {
            var cart = new DrawCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            engine.Init();
            engine.Tick(1f / 60f);

            // DrawCartridge.Draw calls Cls + Rectfill + Spr through convenience methods
            Assert.IsTrue(cart.DrawCompleted);
        }

        [Test]
        public void Spec_NullByDefault_UsesBuilderSpec()
        {
            var cart = new InspectableCartridge();
            Assert.IsNull(cart.Spec);
        }

        [Test]
        public void Spec_Override_UsedByBuilder()
        {
            var cart = new SpecCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            // SpecCartridge declares 256x256 spec
            Assert.AreEqual(256, engine.Api.Width);
            Assert.AreEqual(256, engine.Api.Height);
        }

        [Test]
        public void Math_Rnd_Accessible()
        {
            var cart = new MathCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            engine.Init();
            engine.Tick(1f / 60f);

            Assert.IsTrue(cart.MathWorked);
        }

        [Test]
        public void Input_Btn_Accessible()
        {
            var cart = new InputCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            engine.Init();
            engine.Tick(1f / 60f);

            // No input pressed, Btn should return false without crashing
            Assert.IsTrue(cart.InputChecked);
        }

        [Test]
        public void Map_MgetMset_Accessible()
        {
            var cart = new MapCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            engine.Init();
            engine.Tick(1f / 60f);

            Assert.IsTrue(cart.MapWorked);
        }

        [Test]
        public void Audio_Sfx_DoesNotThrow()
        {
            var cart = new AudioCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            engine.Init();
            Assert.DoesNotThrow(() => engine.Tick(1f / 60f));
        }

        [Test]
        public void Memory_PeekPoke_ThroughCartridge()
        {
            var cart = new MemoryCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            engine.Init();
            engine.Tick(1f / 60f);

            Assert.IsTrue(cart.MemoryWorked);
        }

        [Test]
        public void Time_ReturnsSomething()
        {
            var cart = new TimeCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            engine.Init();
            engine.Tick(1f / 60f);

            Assert.IsTrue(cart.TimeWorked);
        }

        // ─── Test cartridges ───

        private class InspectableCartridge : Cartridge, IUnityCartridge
        {
            public bool HasApi => P8 != null;
            public override void Init() { }
            public override void Update() { }
            public override void Draw() { }
            public UnityEngine.Texture2D GfxTexture { get; } = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            public string SfxData => null;
            public string MusicData => null;
            public string MapData => null;
            public string GffData => null;
            public UnityEngine.Texture2D LabelTexture => null;
        }

        private class DrawCartridge : Cartridge, IUnityCartridge
        {
            public bool DrawCompleted;
            public override void Init() { }
            public override void Update() { }
            public override void Draw()
            {
                Cls(0);
                Rectfill(0, 0, 10, 10, 7);
                Rect(20, 20, 30, 30, 8);
                Line(0, 0, 50, 50, 12);
                Circ(64, 64, 10, 11);
                Circfill(64, 64, 5, 9);
                Oval(10, 10, 30, 20, 5);
                Ovalfill(10, 10, 30, 20, 6);
                Pset(0, 0, 15);
                var v = Pget(0, 0);
                Spr(0, 50, 50);
                Sspr(0, 0, 8, 8, 60, 60);
                Sget(0, 0);
                Sset(0, 0, 5);
                Print("hello", 10, 10, 7);
                Camera(0, 0);
                Clip(10, 10, 100, 100);
                Clip();
                Pal(5, 8, 0);
                Pal();
                Palt(0, true);
                Palt();
                Fillp(0);
                Color(6);
                Cursor(0, 0, 7);
                int pw = PhysW;
                int ph = PhysH;
                int ps = PixelScale;
                DrawCompleted = true;
            }
            public UnityEngine.Texture2D GfxTexture { get; } = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            public string SfxData => null;
            public string MusicData => null;
            public string MapData => null;
            public string GffData => null;
            public UnityEngine.Texture2D LabelTexture => null;
        }

        private class SpecCartridge : Cartridge, IUnityCartridge
        {
            public override EngineSpec Spec => new EngineSpec
            {
                ScreenWidth = 256,
                ScreenHeight = 256,
            };
            public override void Init() { }
            public override void Update() { }
            public override void Draw() { }
            public UnityEngine.Texture2D GfxTexture { get; } = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            public string SfxData => null;
            public string MusicData => null;
            public string MapData => null;
            public string GffData => null;
            public UnityEngine.Texture2D LabelTexture => null;
        }

        private class MathCartridge : Cartridge, IUnityCartridge
        {
            public bool MathWorked;
            public override void Init() { }
            public override void Update()
            {
                float r = Rnd(10);
                int f = Flr(3.7f);
                int c = Ceil(3.1f);
                float s = Sin(0.25f);
                float co = Cos(0);
                float a = Atan2(1, 0);
                float sq = Sqrt(16);
                float ab = Abs(-5);
                float sg = Sgn(0);
                float mn = Min(3, 7);
                float mx = Max(3, 7);
                float md = Mid(1, 5, 10);
                Srand(42);

                MathWorked = (f == 3 && sg == 1f && ab == 5f);
            }
            public override void Draw() { }
            public UnityEngine.Texture2D GfxTexture { get; } = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            public string SfxData => null;
            public string MusicData => null;
            public string MapData => null;
            public string GffData => null;
            public UnityEngine.Texture2D LabelTexture => null;
        }

        private class InputCartridge : Cartridge, IUnityCartridge
        {
            public bool InputChecked;
            public override void Init() { }
            public override void Update()
            {
                bool b0 = Btn(0);
                bool b1 = Btn(1, 1);
                bool bp = Btnp(4);
                InputChecked = true;
            }
            public override void Draw() { }
            public UnityEngine.Texture2D GfxTexture { get; } = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            public string SfxData => null;
            public string MusicData => null;
            public string MapData => null;
            public string GffData => null;
            public UnityEngine.Texture2D LabelTexture => null;
        }

        private class MapCartridge : Cartridge, IUnityCartridge
        {
            public bool MapWorked;
            public override void Init() { }
            public override void Update()
            {
                Mset(5, 5, 42);
                int v = Mget(5, 5);
                Fset(10, 0, true);
                int f = Fget(10);
                MapWorked = (v == 42 && f != 0);
            }
            public override void Draw()
            {
                Map(0, 0, 0, 0, 16, 16);
            }
            public UnityEngine.Texture2D GfxTexture { get; } = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            public string SfxData => null;
            public string MusicData => null;
            public string MapData => null;
            public string GffData => null;
            public UnityEngine.Texture2D LabelTexture => null;
        }

        private class AudioCartridge : Cartridge, IUnityCartridge
        {
            public override void Init() { }
            public override void Update()
            {
                Sfx(0);
                Music(0);
            }
            public override void Draw() { }
            public UnityEngine.Texture2D GfxTexture { get; } = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            public string SfxData => null;
            public string MusicData => null;
            public string MapData => null;
            public string GffData => null;
            public UnityEngine.Texture2D LabelTexture => null;
        }

        private class MemoryCartridge : Cartridge, IUnityCartridge
        {
            public bool MemoryWorked;
            public override void Init() { }
            public override void Update()
            {
                Poke(500, 99);
                int v = Peek(500);
                Memset(600, 0xAB, 10);
                Memcpy(700, 600, 10);
                MemoryWorked = (v == 99 && Peek(700) == 0xAB);
            }
            public override void Draw() { }
            public UnityEngine.Texture2D GfxTexture { get; } = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            public string SfxData => null;
            public string MusicData => null;
            public string MapData => null;
            public string GffData => null;
            public UnityEngine.Texture2D LabelTexture => null;
        }

        private class TimeCartridge : Cartridge, IUnityCartridge
        {
            public bool TimeWorked;
            public override void Init() { }
            public override void Update()
            {
                float t = Time();
                TimeWorked = (t >= 0f);
            }
            public override void Draw() { }
            public UnityEngine.Texture2D GfxTexture { get; } = new UnityEngine.Texture2D(8, 8, UnityEngine.TextureFormat.RGBA32, false);
            public string SfxData => null;
            public string MusicData => null;
            public string MapData => null;
            public string GffData => null;
            public UnityEngine.Texture2D LabelTexture => null;
        }
    }
}
