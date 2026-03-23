using UnityEngine;
using UnityEngine.UIElements;

namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// UIToolkit VisualElement that displays the PICO-8 screen and drives the game loop.
    /// </summary>
    public sealed class Pico8View : VisualElement
    {
        private readonly Pico8Engine _engine;
        private readonly Image _image;

        public Pico8View(Pico8Engine engine, int displayWidth, int displayHeight)
        {
            _engine = engine;
            _image = new Image { image = engine.TextureOutput.Texture };
            _image.style.width = displayWidth;
            _image.style.height = displayHeight;
            Add(_image);
        }

        /// <summary>
        /// Call once per frame to tick the engine and refresh the display.
        /// </summary>
        public void Tick(float dt)
        {
            _engine.Tick(dt);
            // Force UIToolkit to redraw with updated texture data
            _image.image = _engine.TextureOutput.Texture;
            _image.MarkDirtyRepaint();
        }
    }
}
