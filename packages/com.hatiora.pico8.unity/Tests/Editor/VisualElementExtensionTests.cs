using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Hatiora.Pico8;

namespace Hatiora.Pico8.Unity.Tests
{
    /// <summary>
    /// Tests for VisualElement extension methods (Style, Children).
    /// These live in Hatiora.Pico8 namespace (core package) but are used
    /// heavily by the unity adapter, so we test them here too.
    /// </summary>
    [TestFixture]
    public class VisualElementExtensionTests
    {
        [Test]
        public void Style_ReturnsSameElement()
        {
            var el = new VisualElement();
            var result = el.Style(s => s.width = 100);
            Assert.AreSame(el, result);
        }

        [Test]
        public void Style_AppliesStyleValue()
        {
            var el = new VisualElement()
                .Style(s =>
                {
                    s.width = 200;
                    s.height = 150;
                });

            Assert.AreEqual(200, el.style.width.value.value);
            Assert.AreEqual(150, el.style.height.value.value);
        }

        [Test]
        public void Style_ChainMultiple()
        {
            var el = new VisualElement()
                .Style(s => s.flexGrow = 1)
                .Style(s => s.backgroundColor = Color.red);

            Assert.AreEqual(1, el.style.flexGrow.value);
        }

        [Test]
        public void Style_NullAction_DoesNotThrow()
        {
            var el = new VisualElement();
            Assert.DoesNotThrow(() => el.Style(null));
        }

        [Test]
        public void Children_AddsElements()
        {
            var parent = new VisualElement();
            var child1 = new VisualElement();
            var child2 = new VisualElement();

            parent.Children(child1, child2);

            Assert.AreEqual(2, parent.childCount);
            Assert.AreSame(child1, parent[0]);
            Assert.AreSame(child2, parent[1]);
        }

        [Test]
        public void Children_ReturnsSameElement()
        {
            var parent = new VisualElement();
            var result = parent.Children(new VisualElement());
            Assert.AreSame(parent, result);
        }

        [Test]
        public void Children_EmptyParams_DoesNotThrow()
        {
            var parent = new VisualElement();
            Assert.DoesNotThrow(() => parent.Children());
            Assert.AreEqual(0, parent.childCount);
        }

        [Test]
        public void Style_And_Children_Chained()
        {
            var container = new VisualElement()
                .Style(s => s.flexDirection = FlexDirection.Row)
                .Children(
                    new VisualElement().Style(s => s.width = 100),
                    new VisualElement().Style(s => s.width = 200)
                );

            Assert.AreEqual(FlexDirection.Row, container.style.flexDirection.value);
            Assert.AreEqual(2, container.childCount);
        }

        [Test]
        public void Style_FlexProperties()
        {
            var el = new VisualElement()
                .Style(s =>
                {
                    s.flexGrow = 1;
                    s.flexDirection = FlexDirection.Column;
                    s.alignItems = Align.Center;
                    s.justifyContent = Justify.Center;
                });

            Assert.AreEqual(Align.Center, el.style.alignItems.value);
            Assert.AreEqual(Justify.Center, el.style.justifyContent.value);
        }

        [Test]
        public void Style_OnImage_Works()
        {
            var img = new Image()
                .Style(s =>
                {
                    s.width = 512;
                    s.height = 512;
                });

            Assert.AreEqual(512, img.style.width.value.value);
            Assert.IsInstanceOf<Image>(img);
        }

        [Test]
        public void Style_OnLabel_Works()
        {
            var label = new Label("hello")
                .Style(s => s.color = Color.white);

            Assert.IsInstanceOf<Label>(label);
        }
    }
}
