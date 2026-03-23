using UnityEngine;

namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// Interface for Pico-8 cartridges that provide their own Unity Texture2D assets
    /// (e.g. spritesheets or tilemasks) to be loaded automatically by the Pico8Builder.
    /// </summary>
    public interface IUnityCartridge
    {
        Texture2D GfxTexture { get; }
        Texture2D LabelTexture { get; }
        string SfxData { get; }
        string MusicData { get; }
        string MapData { get; }
        string GffData { get; }
    }
}
