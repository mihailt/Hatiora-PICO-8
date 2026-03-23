using UnityEditor;
using UnityEngine;

namespace Hatiora.Pico8.Unity.Editor
{
    /// <summary>
    /// Automatically configures Texture2D imports for PICO-8 games.
    /// Assures textures (like Gfx and Label) use Point filtering and No Compression,
    /// avoiding pixel bleeding artifacts when mapped to the PICO-8 palette.
    /// </summary>
    public class Pico8TextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            // Only apply this to textures in a PICO-8 package or containing specific folders
            if (assetPath.Contains("Pico-8/packages") || assetPath.Contains("pico8") || assetPath.ToLower().Contains("gfx") || assetPath.ToLower().Contains("label"))
            {
                TextureImporter importer = (TextureImporter)assetImporter;

                // We only care about 2D sprite textures/images for Pico-8
                if (importer.textureType == TextureImporterType.Default || importer.textureType == TextureImporterType.Sprite)
                {
                    bool changed = false;

                    if (importer.filterMode != FilterMode.Point)
                    {
                        importer.filterMode = FilterMode.Point;
                        changed = true;
                    }

                    if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        importer.textureCompression = TextureImporterCompression.Uncompressed;
                        changed = true;
                    }

                    if (importer.mipmapEnabled != false)
                    {
                        importer.mipmapEnabled = false;
                        changed = true;
                    }

                    if (importer.isReadable != true)
                    {
                        importer.isReadable = true;
                        changed = true;
                    }

                    if (changed)
                    {
                        Debug.Log($"[Pico8TextureImporter] Auto-configured PICO-8 texture for pixel-perfect rendering: {assetPath}");
                    }
                }
            }
        }

        public static void ReimportAllPico8Textures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D");
            int reimportedCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Pico-8/packages") || path.Contains("pico8") || path.ToLower().Contains("gfx") || path.ToLower().Contains("label"))
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    reimportedCount++;
                }
            }
            Debug.Log($"[Pico8TextureImporter] Forced reimport of {reimportedCount} PICO-8 textures.");
        }
    }
}
