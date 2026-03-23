using NUnit.Framework;
using UnityEngine.UIElements;

namespace Hatiora.Pico8.Unity.Tests
{
    [TestFixture]
    public class Pico8ViewTests
    {
        [Test]
        public void Constructor_CreatesView()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .Build();

            var view = new Pico8View(engine, 800, 600);
            Assert.IsNotNull(view);
        }

        [Test]
        public void View_ContainsImageChild()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .Build();

            var view = new Pico8View(engine, 512, 512);
            Assert.AreEqual(1, view.childCount);
            Assert.IsInstanceOf<Image>(view[0]);
        }

        [Test]
        public void View_ImageHasCorrectSize()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .Build();

            var view = new Pico8View(engine, 800, 600);
            var image = view[0] as Image;
            Assert.AreEqual(800, image.style.width.value.value);
            Assert.AreEqual(600, image.style.height.value.value);
        }

        [Test]
        public void Tick_DoesNotThrow()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .Build();

            var view = new Pico8View(engine, 128, 128);
            Assert.DoesNotThrow(() => view.Tick(1f / 60f));
        }

        [Test]
        public void Tick_CallsCartridgeUpdateDraw()
        {
            var cart = new CountCartridge();
            var engine = new Pico8Builder()
                .WithCartridge(cart)
                .Build();

            var view = new Pico8View(engine, 128, 128);
            view.Tick(1f / 60f);

            Assert.AreEqual(1, cart.UpdateCount);
            Assert.AreEqual(1, cart.DrawCount);
        }

        [Test]
        public void View_IsVisualElement()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .Build();

            var view = new Pico8View(engine, 128, 128);
            Assert.IsInstanceOf<VisualElement>(view);
        }

        [Test]
        public void View_DifferentSizes()
        {
            var engine = new Pico8Builder()
                .WithCartridge(new MinimalCartridge())
                .Build();

            var view1 = new Pico8View(engine, 128, 128);
            var view2 = new Pico8View(engine, 1920, 1080);

            var img1 = view1[0] as Image;
            var img2 = view2[0] as Image;

            Assert.AreEqual(128, img1.style.width.value.value);
            Assert.AreEqual(1920, img2.style.width.value.value);
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

        private class CountCartridge : Cartridge, IUnityCartridge
        {
            public int UpdateCount, DrawCount;
            public override void Init() { }
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
