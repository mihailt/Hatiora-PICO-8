using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace Hatiora.Pico8
{
    /// <summary>
    /// Abstracts FontEngine calls for testability.
    /// </summary>
    public interface IFontEngine
    {
        FontEngineError LoadFontFace(Font font);
        FaceInfo GetFaceInfo();
    }

    /// <summary>
    /// Default implementation wrapping Unity's FontEngine static API.
    /// </summary>
    public sealed class UnityFontEngine : IFontEngine
    {
        public static readonly UnityFontEngine Instance = new();

        public FontEngineError LoadFontFace(Font font) => FontEngine.LoadFontFace(font);
        public FaceInfo GetFaceInfo() => FontEngine.GetFaceInfo();
    }
}
