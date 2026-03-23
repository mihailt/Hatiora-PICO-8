using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Hatiora.Pico8.Tools.Editor
{
    public static class P8Extractor
    {

        // PICO-8 default palette (same as Pico8Theme)
        private static readonly Color32[] Palette =
        {
            new Color32(  0,   0,   0, 255), // 0  black
            new Color32( 29,  43,  83, 255), // 1  dark-blue
            new Color32(126,  37,  83, 255), // 2  dark-purple
            new Color32(  0, 135,  81, 255), // 3  dark-green
            new Color32(171,  82,  54, 255), // 4  brown
            new Color32( 95,  87,  79, 255), // 5  dark-grey
            new Color32(194, 195, 199, 255), // 6  light-grey
            new Color32(255, 241, 232, 255), // 7  white
            new Color32(255,   0,  77, 255), // 8  red
            new Color32(255, 163,   0, 255), // 9  orange
            new Color32(255, 236,  39, 255), // 10 yellow
            new Color32(  0, 228,  54, 255), // 11 green
            new Color32( 41, 173, 255, 255), // 12 blue
            new Color32(131, 118, 156, 255), // 13 indigo
            new Color32(255, 119, 168, 255), // 14 pink
            new Color32(255, 204, 170, 255), // 15 peach
        };

        public static void ExtractAll()
        {
            var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            
            var carts = CartridgeService.GetInstalledCartridges();
            foreach (var cart in carts)
            {
                var refsDir = Path.Combine(repoRoot, cart.refsRelDir);
                if (!Directory.Exists(refsDir)) continue;
                
                string outDir = Path.GetFullPath(Path.Combine(repoRoot, $"packages/{cart.fullPkgName}/Runtime/Resources"));
                Extract(refsDir, outDir, cart.rawName, cart.fileFilter);
            }
        }

        public static void Extract(string refsDir, string outputRoot, string subfolder, string fileFilter = "*.p8")
        {
            if (!Directory.Exists(refsDir))
            {
                Debug.LogError($"[P8Extractor] References directory not found: {refsDir}");
                return;
            }

            var p8Files = Directory.GetFiles(refsDir, fileFilter);

            if (p8Files.Length == 0)
            {
                Debug.LogWarning($"[P8Extractor] No files matching {fileFilter} found in {refsDir}");
                return;
            }

            int totalSections = 0;

            var gfxPaths = new List<string>();

            foreach (var p8Path in p8Files)
            {
                var fileName = Path.GetFileNameWithoutExtension(p8Path);
                var sections = Parse(File.ReadAllLines(p8Path));
                var extracted = new List<string>();

                foreach (var (name, content) in sections)
                {
                    if (name == "lua" || content.Length == 0) continue;

                    var dir = Path.Combine(outputRoot, subfolder, fileName, Capitalize(name));
                    Directory.CreateDirectory(dir);

                    if (name == "gfx" || name == "label")
                    {
                        var pngPath = Path.Combine(dir, $"{name}.png");
                        SaveGfxAsPng(content.ToString(), pngPath);
                        gfxPaths.Add(pngPath);
                        extracted.Add($"{name}(128x128 PNG)");
                    }
                    else
                    {
                        File.WriteAllText(Path.Combine(dir, $"{name}.txt"), content.ToString());
                        extracted.Add($"{name}({CountLines(content)})");
                    }
                }

                totalSections += extracted.Count;
                Debug.Log($"[P8Extractor] {fileName}.p8 → {string.Join(", ", extracted)}");
            }

            AssetDatabase.Refresh();

            foreach (var path in gfxPaths)
            {
                string relativePath = path.Replace("\\", "/");
                int index = relativePath.IndexOf("packages/", System.StringComparison.OrdinalIgnoreCase);
                if (index >= 0) relativePath = "Packages/" + relativePath.Substring(index + 9);
                else if ((index = relativePath.IndexOf("assets/", System.StringComparison.OrdinalIgnoreCase)) >= 0)
                    relativePath = "Assets/" + relativePath.Substring(index + 7);

                var importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer != null)
                {
                    importer.isReadable = true;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }

            Debug.Log($"[P8Extractor] Done. {p8Files.Length} file(s), {totalSections} section(s) → {outputRoot}");
        }

        private static void SaveGfxAsPng(string hexData, string path)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };

            // Initialize all to transparent black
            var clearColor = Palette[0];
            clearColor.a = 0;
            var clearPixels = new Color32[size * size];
            for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = clearColor;
            tex.SetPixels32(clearPixels);

            var lines = hexData.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

            for (int row = 0; row < Mathf.Min(size, lines.Length); row++)
            {
                var line = lines[row];
                for (int col = 0; col < Mathf.Min(size, line.Length); col++)
                {
                    char c = line[col];
                    int idx = c >= '0' && c <= '9' ? c - '0' : c >= 'a' && c <= 'f' ? c - 'a' + 10 : 0;

                    // Index 0 = transparent
                    var color = Palette[idx];
                    if (idx == 0) color.a = 0;

                    // PICO-8 row 0 = top, Unity row 0 = bottom
                    tex.SetPixel(col, size - 1 - row, color);
                }
            }

            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static List<(string name, StringBuilder content)> Parse(string[] lines)
        {
            var sections = new List<(string, StringBuilder)>();
            StringBuilder current = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("__") && trimmed.EndsWith("__") && trimmed.Length > 4)
                {
                    current = new StringBuilder();
                    sections.Add((trimmed.Substring(2, trimmed.Length - 4), current));
                    continue;
                }

                if (current != null && trimmed.Length > 0)
                    current.AppendLine(trimmed);
            }

            return sections;
        }

        private static string Capitalize(string s) =>
            s.Length == 0 ? s : char.ToUpper(s[0]) + s.Substring(1);

        private static int CountLines(StringBuilder sb)
        {
            int count = 0;
            foreach (char c in sb.ToString())
                if (c == '\n') count++;
            return count;
        }
    }
}
