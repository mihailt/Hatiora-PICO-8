using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Font provider for pixel-art TTF fonts.
    /// Auto-detects pixel grid. Supports downscaling to fit target char width.
    /// </summary>
    public class FontProvider : IFontProvider
    {
        private const int RenderSize = 32;

        private readonly Font _font;
        private readonly int _step;
        private readonly int _ascender;
        private readonly float _scale; // 1.0 = native, 0.5 = half size

        private Texture2D _atlasCache;

        /// <summary>Auto-detect pixelsPerEm using Unity FontEngine, native scale. Logs fallback warnings.</summary>
        public FontProvider(Font font) : this(font, UnityFontEngine.Instance, true) { }

        /// <summary>Auto-detect pixelsPerEm using injectable font engine, native scale.</summary>
        public FontProvider(Font font, IFontEngine fontEngine, bool logWarning = false)
            : this(font, DetectPixelsPerEm(font, fontEngine, logWarning)) { }

        /// <summary>Explicit pixelsPerEm, native scale.</summary>
        public FontProvider(Font font, int pixelsPerEm) : this(font, pixelsPerEm, pixelsPerEm) { }

        /// <summary>Explicit pixelsPerEm with target char width for downscaling.</summary>
        /// <param name="font">Unity Font asset</param>
        /// <param name="pixelsPerEm">Font pixels per em (e.g. 4 for PICO-8, 8 for PressStart2P)</param>
        /// <param name="charWidth">Desired screen advance for standard chars. If less than pixelsPerEm, glyphs are downscaled.</param>
        public FontProvider(Font font, int pixelsPerEm, int charWidth)
        {
            _font = font;
            _step = RenderSize / pixelsPerEm;
            _scale = (float)charWidth / pixelsPerEm;

            // Derive ascender from tallest glyph (in native font pixels), then scale
            const string probeChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZbdfhkl|";
            _font.RequestCharactersInTexture(probeChars, RenderSize, FontStyle.Normal);

            int maxMaxY = 0;
            foreach (char c in probeChars)
            {
                if (_font.GetCharacterInfo(c, out var info, RenderSize, FontStyle.Normal))
                {
                    int mY = Mathf.Abs(info.maxY);
                    if (mY > maxMaxY) maxMaxY = mY;
                }
            }

            int nativeAscender = Mathf.RoundToInt((float)maxMaxY / _step);
            _ascender = Mathf.RoundToInt(nativeAscender * _scale);
        }

        public void Prepare(string str)
        {
            _font.RequestCharactersInTexture(str, RenderSize, FontStyle.Normal);
            RebuildAtlasCache();
        }

        public GlyphData GetGlyph(char c)
        {
            if (_atlasCache == null) return default;
            if (!_font.GetCharacterInfo(c, out var info, RenderSize, FontStyle.Normal))
                return default;

            int aw = _atlasCache.width;
            int ah = _atlasCache.height;

            // Native glyph dimensions (font pixels)
            Vector2 spanX = info.uvTopRight - info.uvTopLeft;
            Vector2 spanY = info.uvBottomLeft - info.uvTopLeft;
            int nativeW = Mathf.RoundToInt(new Vector2(spanX.x * aw, spanX.y * ah).magnitude / _step);
            int nativeH = Mathf.RoundToInt(new Vector2(spanY.x * aw, spanY.y * ah).magnitude / _step);

            int nativeAdvance = Mathf.RoundToInt(info.advance / _step);

            if (nativeW <= 0 || nativeH <= 0)
                return new GlyphData { Advance = Mathf.Max(1, Mathf.RoundToInt(nativeAdvance * _scale)) };

            int nativeBearingX = Mathf.RoundToInt((float)info.minX / _step);

            // Output dimensions (scaled)
            int outW = Mathf.Max(1, Mathf.RoundToInt(nativeW * _scale));
            int outH = Mathf.Max(1, Mathf.RoundToInt(nativeH * _scale));
            int outAdvance = Mathf.Max(1, Mathf.RoundToInt(nativeAdvance * _scale));
            if (_scale < 1f) outAdvance += 1; // add 1px spacing when downscaled
            int outBearingX = Mathf.RoundToInt(nativeBearingX * _scale);

            // Sample directly at output resolution from UV quad
            bool[] pixels = new bool[outW * outH];
            Vector2 origin = info.uvTopLeft;
            Vector2 uvStepX = (info.uvTopRight - info.uvTopLeft) / outW;
            Vector2 uvStepY = (info.uvBottomLeft - info.uvTopLeft) / outH;

            for (int sy = 0; sy < outH; sy++)
            {
                for (int sx = 0; sx < outW; sx++)
                {
                    Vector2 uv = origin + uvStepX * (sx + 0.5f) + uvStepY * (sy + 0.5f);
                    int sampleX = Mathf.Clamp(Mathf.RoundToInt(uv.x * aw), 0, aw - 1);
                    int sampleY = Mathf.Clamp(Mathf.RoundToInt(uv.y * ah), 0, ah - 1);
                    pixels[sy * outW + sx] = _atlasCache.GetPixel(sampleX, sampleY).a > 0.3f;
                }
            }

            return new GlyphData
            {
                Width = outW,
                Height = outH,
                BearingX = outBearingX,
                BearingY = _ascender - Mathf.RoundToInt(Mathf.Abs(info.maxY) / _step * _scale),
                Advance = outAdvance,
                Pixels = pixels
            };
        }

        #region Atlas Cache

        private void RebuildAtlasCache()
        {
            var src = _font.material.mainTexture as Texture2D;
            if (src == null) { _atlasCache = null; return; }

            if (_atlasCache != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_atlasCache);
                else
                    Object.DestroyImmediate(_atlasCache);
            }

            var rt = RenderTexture.GetTemporary(src.width, src.height);
            rt.filterMode = FilterMode.Point;
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            _atlasCache = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            _atlasCache.filterMode = FilterMode.Point; // Prevent subpixel blurring when reading text glyphs for PICO-8
            _atlasCache.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            _atlasCache.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
        }

        #endregion

        #region Detection

        private static int DetectPixelsPerEm(Font font, IFontEngine engine, bool logWarning = false)
        {
            if (engine.LoadFontFace(font) != FontEngineError.Success)
                return 4;

            var face = engine.GetFaceInfo();
            int upm = face.pointSize > 0 ? (int)face.pointSize : 1000;

            int g = upm;
            int ascent = Mathf.Abs(Mathf.RoundToInt(face.ascentLine));
            int descent = Mathf.Abs(Mathf.RoundToInt(face.descentLine));
            int lineH = Mathf.Abs(Mathf.RoundToInt(face.lineHeight));

            if (ascent > 0) g = GCD(g, ascent);
            if (descent > 0) g = GCD(g, descent);
            if (lineH > 0) g = GCD(g, lineH);

            int pixelsPerEm = upm / (g > 0 ? g : 1);

            if (pixelsPerEm <= 2)
            {
                if (logWarning)
                    Debug.LogWarning($"[FontProvider] auto-detect unreliable for '{font.name}' (pixelsPerEm={pixelsPerEm}). Use FontProvider(font, pixelsPerEm) to specify manually.");
                pixelsPerEm = 4;
            }

            return pixelsPerEm;
        }

        private static int GCD(int a, int b)
        {
            a = Mathf.Abs(a); b = Mathf.Abs(b);
            while (b > 0) { int t = b; b = a % b; a = t; }
            return a;
        }

        #endregion

        public override string ToString() => $"FontProvider (step={_step}, ascender={_ascender}, scale={_scale}, font={_font.name})";
    }
}
