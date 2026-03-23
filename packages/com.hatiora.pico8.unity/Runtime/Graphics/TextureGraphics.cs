using UnityEngine;

namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// Bridges the headless <see cref="PixelBuffer"/> to a Unity Texture2D.
    /// Reads palette indices from the buffer and converts to Color32 for display.
    /// </summary>
    public sealed class TextureGraphics
    {
        public Texture2D Texture { get; }

        private readonly PixelBuffer _buffer;
        private readonly Palette _palette;
        private readonly DrawState _state;
        private readonly Color32[] _outputPixels;
        private readonly int _physW;
        private readonly int _physH;

        public TextureGraphics(PixelBuffer buffer, Palette palette, DrawState state)
        {
            _buffer = buffer;
            _palette = palette;
            _state = state;
            _physW = buffer.PhysicalWidth;
            _physH = buffer.PhysicalHeight;
            _outputPixels = new Color32[_physW * _physH];
            Texture = new Texture2D(_physW, _physH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
        }

        /// <summary>
        /// Converts the palette-indexed pixel buffer to Color32 and uploads to GPU.
        /// Call after <see cref="PixelBuffer.Flush"/>.
        /// </summary>
        public void Present()
        {
            var pixels = _buffer.Pixels;
            var r = _palette.R;
            var g = _palette.G;
            var b = _palette.B;
            var dp = _state.DisplayPalette;
            int size = _palette.Size;
            int pw = _physW;
            int ph = _physH;

            // Row-based iteration avoids per-pixel division/modulo for Y-flip
            int srcIdx = 0;
            for (int row = 0; row < ph; row++)
            {
                int dstBase = (ph - 1 - row) * pw;
                for (int col = 0; col < pw; col++)
                {
                    int idx = dp[pixels[srcIdx] % size] % size;
                    _outputPixels[dstBase + col] = new Color32(r[idx], g[idx], b[idx], 255);
                    srcIdx++;
                }
            }

            Texture.SetPixelData(_outputPixels, 0);
            Texture.Apply();
        }
    }
}
