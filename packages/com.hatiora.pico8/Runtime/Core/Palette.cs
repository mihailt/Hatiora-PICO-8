using System;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Color palette stored as raw RGB bytes. No Unity Color dependency.
    /// </summary>
    public sealed class Palette
    {
        public byte[] R { get; }
        public byte[] G { get; }
        public byte[] B { get; }
        public int Size { get; }

        public Palette(EngineSpec spec)
        {
            Size = spec.PaletteSize;
            R = new byte[Size];
            G = new byte[Size];
            B = new byte[Size];
            LoadDefaults(spec.DefaultPalette);
        }

        private void LoadDefaults(byte[] rgb)
        {
            int count = Math.Min(Size, rgb.Length / 3);
            for (int i = 0; i < count; i++)
            {
                R[i] = rgb[i * 3];
                G[i] = rgb[i * 3 + 1];
                B[i] = rgb[i * 3 + 2];
            }
        }

        /// <summary>
        /// Official PICO-8 16-color palette as flat R,G,B bytes.
        /// </summary>
        public static readonly byte[] Pico8Rgb =
        {
            0x00, 0x00, 0x00,  // 0  black
            0x1D, 0x2B, 0x53,  // 1  dark-blue
            0x7E, 0x25, 0x53,  // 2  dark-purple
            0x00, 0x87, 0x51,  // 3  dark-green
            0xAB, 0x52, 0x36,  // 4  brown
            0x5F, 0x57, 0x4F,  // 5  dark-grey
            0xC2, 0xC3, 0xC7,  // 6  light-grey
            0xFF, 0xF1, 0xE8,  // 7  white
            0xFF, 0x00, 0x4D,  // 8  red
            0xFF, 0xA3, 0x00,  // 9  orange
            0xFF, 0xEC, 0x27,  // 10 yellow
            0x00, 0xE4, 0x36,  // 11 green
            0x29, 0xAD, 0xFF,  // 12 blue
            0x83, 0x76, 0x9C,  // 13 indigo
            0xFF, 0x77, 0xA8,  // 14 pink
            0xFF, 0xCC, 0xAA,  // 15 peach
        };
    }
}
