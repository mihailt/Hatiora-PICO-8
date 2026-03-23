using System;
using UnityEngine;

namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// Converts a Unity Texture2D (RGBA) into palette-indexed byte[] data
    /// for loading into <see cref="ISpriteStore"/> banks.
    /// </summary>
    public static class UnitySpriteLoader
    {
        /// <summary>
        /// Loads a Texture2D as a sprite bank, converting RGBA pixels to
        /// palette indices by matching the closest color in the palette.
        /// Uses the default UnityTextureReader for pixel access.
        /// </summary>
        public static byte[] Load(Texture2D texture, Palette palette)
            => Load(texture, palette, UnityTextureReader.Instance);

        /// <summary>
        /// Loads a Texture2D using a custom texture reader (for testability).
        /// </summary>
        public static byte[] Load(Texture2D texture, Palette palette, ITextureReader reader)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            if (palette == null) throw new ArgumentNullException(nameof(palette));
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var pixels = reader.ReadPixels(texture, out int w, out int h);
            var result = new byte[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Unity: row 0 = bottom, sprite: row 0 = top
                    var c = pixels[(h - 1 - y) * w + x];

                    if (c.a < 128)
                    {
                        result[y * w + x] = 0; // transparent → color 0
                        continue;
                    }

                    result[y * w + x] = MatchColor(c, palette);
                }
            }

            return result;
        }

        /// <summary>
        /// Loads a Texture2D and registers it as a bank in the sprite store.
        /// </summary>
        public static void LoadBank(ISpriteStore store, int bankIndex, Texture2D texture, Palette palette)
            => LoadBank(store, bankIndex, texture, palette, UnityTextureReader.Instance);

        /// <summary>
        /// Loads a Texture2D and registers it as a bank using a custom reader.
        /// </summary>
        public static void LoadBank(ISpriteStore store, int bankIndex, Texture2D texture, Palette palette, ITextureReader reader)
        {
            var data = Load(texture, palette, reader);
            store.LoadBank(bankIndex, data, texture.width, texture.height);
        }

        private static byte MatchColor(Color32 c, Palette palette)
        {
            int bestIdx = 0;
            int bestDist = int.MaxValue;

            for (int i = 0; i < palette.Size; i++)
            {
                int dr = c.r - palette.R[i];
                int dg = c.g - palette.G[i];
                int db = c.b - palette.B[i];
                int dist = dr * dr + dg * dg + db * db;

                if (dist == 0) return (byte)i; // exact match
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            return (byte)bestIdx;
        }
    }
}
