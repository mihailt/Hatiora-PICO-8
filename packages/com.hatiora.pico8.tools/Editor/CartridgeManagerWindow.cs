using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hatiora.Pico8.Tools.Editor
{
    public class CartridgeManagerWindow : EditorWindow
    {
        [MenuItem("PICO-8/Settings/Tools")]
        public static void ShowWindow()
        {
            var window = GetWindow<CartridgeManagerWindow>();
            window.titleContent = new GUIContent("PICO-8");
            window.minSize = new Vector2(500, 600);
        }

        private CartridgeManagerUI _ui;

        public void CreateGUI()
        {
            _ui = new CartridgeManagerUI(rootVisualElement, CreateCartridge, GenerateAllDocs, GenerateAllTests);
        }

        private void CreateCartridge()
        {
            string name = _ui.NewCartName.Trim();
            if (string.IsNullOrEmpty(name)) return;

            // Enforce clean naming (remove spaces and redundant prefixes)
            name = name.Replace(" ", "");
            if (name.StartsWith("com.hatiora.pico8.", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring("com.hatiora.pico8.".Length);
            }

            string rawName = char.ToUpper(name[0]) + name.Substring(1);
            string pkgName = name.ToLower();
            string fullPkgName = "com.hatiora.pico8." + pkgName;
            
            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            string pkgPath = Path.Combine(repoRoot, "packages", fullPkgName);

            if (Directory.Exists(pkgPath))
            {
                EditorUtility.DisplayDialog("Error", "Package already exists!", "OK");
                return;
            }

            try
            {
                Directory.CreateDirectory(pkgPath);
                
                // 1. Create package.json
                string packageJson = CartridgeTemplateService.GetPackageJsonTemplate()
                    .Replace("[FullPkgName]", fullPkgName)
                    .Replace("[RawName]", rawName);
                File.WriteAllText(Path.Combine(pkgPath, "package.json"), packageJson);

                // 2. Create Runtime assembly
                string runtimeDir = Path.Combine(pkgPath, "Runtime");
                Directory.CreateDirectory(runtimeDir);
                string asmdef = CartridgeTemplateService.GetRuntimeAsmdefTemplate()
                    .Replace("[RawName]", rawName);
                File.WriteAllText(Path.Combine(runtimeDir, $"Hatiora.Pico8.{rawName}.asmdef"), asmdef);

                // 3. Create Cartridge.cs from Hello template
                string primaryP8Name = pkgName;
                var firstP8 = _ui.SourceFiles.FirstOrDefault(f => f.EndsWith(".p8", StringComparison.OrdinalIgnoreCase));
                if (firstP8 != null)
                {
                    primaryP8Name = Path.GetFileNameWithoutExtension(firstP8);
                }

                if (_ui.SourceFiles.Count > 0)
                {
                    string cs = CartridgeTemplateService.GetCartridgeRuntimeTemplate()
                        .Replace("[RawName]", rawName)
                        .Replace("[PrimaryP8Name]", primaryP8Name);
                    File.WriteAllText(Path.Combine(runtimeDir, $"{rawName}Cartridge.cs"), cs);
                }
                else
                {
                    string templatePath = Path.Combine(repoRoot, "packages/com.hatiora.pico8.hello/Runtime/HelloCartridge.cs");
                    if (File.Exists(templatePath))
                    {
                        string cs = File.ReadAllText(templatePath);
                        cs = cs.Replace("namespace Hatiora.Pico8.Hello", $"namespace Hatiora.Pico8.{rawName}");
                        cs = cs.Replace("public class HelloCartridge : Cartridge", $"public class {rawName}Cartridge : Cartridge");
                        cs = cs.Replace("Hello/hello", $"{rawName}/{primaryP8Name}");
                        File.WriteAllText(Path.Combine(runtimeDir, $"{rawName}Cartridge.cs"), cs);
                    }
                }

                // 4. Create Tests assembly
                string testsDir = Path.Combine(pkgPath, "Tests/Editor");
                Directory.CreateDirectory(testsDir);
                string testAsmdef = CartridgeTemplateService.GetTestsAsmdefTemplate()
                    .Replace("[RawName]", rawName);
                File.WriteAllText(Path.Combine(testsDir, $"Hatiora.Pico8.{rawName}.Editor.Tests.asmdef"), testAsmdef);

                // 5. Handle source files
                string targetP8Dir = Path.Combine(pkgPath, "Pico8");
                Directory.CreateDirectory(targetP8Dir);

                if (_ui.SourceFiles.Count > 0)
                {
                    foreach (var src in _ui.SourceFiles)
                    {
                        if (File.Exists(src))
                        {
                            string fileName = Path.GetFileName(src);
                            File.Copy(src, Path.Combine(targetP8Dir, fileName), true);
                        }
                    }
                }
                else
                {
                    string helloP8 = Path.Combine(repoRoot, "packages/com.hatiora.pico8.hello/Pico8/hello.p8");
                    if (File.Exists(helloP8))
                    {
                        File.Copy(helloP8, Path.Combine(targetP8Dir, $"{pkgName}.p8"), true);
                    }
                }

                // 6. Run Extractors and Generators
                string refsRelDir = $"packages/{fullPkgName}/Pico8";
                string outputResourcesDir = Path.Combine(pkgPath, "Runtime", "Resources");
                
                P8Extractor.Extract(Path.Combine(repoRoot, refsRelDir), outputResourcesDir, rawName, "*.p8");
                CartridgeTestGenerator.Generate(rawName, fullPkgName);
                CartridgeDocGenerator.Generate(rawName, fullPkgName, refsRelDir, "*.p8");

                // 7. Register in manifest.json
                string manifestPath = Path.Combine(repoRoot, "apps/Hatiora/Packages/manifest.json");
                if (File.Exists(manifestPath))
                {
                    string mf = File.ReadAllText(manifestPath);
                    mf = mf.Replace("\"dependencies\": {", $"\"dependencies\": {{\n    \"{fullPkgName}\": \"file:../../../packages/{fullPkgName}\",");
                    mf = mf.Replace("\"testables\": [", $"\"testables\": [\n    \"{fullPkgName}\",");
                    File.WriteAllText(manifestPath, mf);
                }

                _ui.NewCartName = "";
                _ui.SourceFiles.Clear();
                _ui.RefreshSourceFilesList();
                _ui.RefreshCartridgesList();
                
                Debug.Log($"[PICO-8 Tools] Cartridge {rawName} created successfully!");
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to create cartridge: {ex.Message}", "OK");
            }
        }

        private void GenerateAllDocs()
        {
            var carts = CartridgeService.GetInstalledCartridges();
            foreach (var c in carts)
            {
                CartridgeDocGenerator.Generate(c.rawName, c.fullPkgName, c.refsRelDir, c.fileFilter);
            }
            Debug.Log($"[PICO-8 Tools] Generated docs for {carts.Count} cartridges.");
        }

        private void GenerateAllTests()
        {
            var carts = CartridgeService.GetInstalledCartridges();
            foreach (var c in carts)
            {
                CartridgeTestGenerator.Generate(c.rawName, c.fullPkgName);
            }
            Debug.Log($"[PICO-8 Tools] Generated tests for {carts.Count} cartridges.");
        }
    }
}
