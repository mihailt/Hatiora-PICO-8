using UnityEngine;

namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// Default texture reader that handles non-readable textures by blitting
    /// through a RenderTexture before reading pixels.
    /// </summary>
    public sealed class UnityTextureReader : ITextureReader
    {
        public static readonly UnityTextureReader Instance = new();

        public Color32[] ReadPixels(Texture2D texture, out int width, out int height)
        {
            var readable = MakeReadable(texture);
            width = readable.width;
            height = readable.height;
            return readable.GetPixels32();
        }

        /// <summary>
        /// Returns a readable copy of the texture. If already readable, returns as-is.
        /// Non-readable textures (e.g. Resources.Load) are blitted through a RenderTexture.
        /// </summary>
        private static Texture2D MakeReadable(Texture2D source)
        {
            if (source.isReadable) return source;

            var prevFilter = source.filterMode;
            source.filterMode = FilterMode.Point;

            var prev = RenderTexture.active;

            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Point;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.filterMode = FilterMode.Point;
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            source.filterMode = prevFilter;

            return readable;
        }
    }
}
