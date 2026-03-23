using UnityEngine;

namespace Hatiora.Pico8.Unity
{
    /// <summary>
    /// Abstracts pixel reading from a Texture2D, allowing tests to bypass
    /// the Unity graphics pipeline (RenderTexture blit for non-readable textures).
    /// </summary>
    public interface ITextureReader
    {
        Color32[] ReadPixels(Texture2D texture, out int width, out int height);
    }
}
