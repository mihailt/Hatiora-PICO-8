using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Hatiora.Pico8.Tools.Editor
{
    public static class CartridgeTestGenerator
    {
        public static void Generate(string rawName, string packageId)
        {
            try
            {
                string className = rawName + "Cartridge";
                string namespaceName = "Hatiora.Pico8." + rawName;
                
                var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));

                string sourcePath = Path.Combine(repoRoot, $"packages/{packageId}/Runtime/{className}.cs");
                if (!File.Exists(sourcePath))
                {
                    Debug.LogError($"[PICO-8 Tools] Source file not found: {sourcePath}");
                    return;
                }
                
                string sourceCode = File.ReadAllText(sourcePath);
                
                // Find overridden methods
                string testMethods = "";
                if (Regex.IsMatch(sourceCode, @"override\s+void\s+(?:_init|Init)\s*\("))
                {
                    testMethods += $"\n        [Test]\n        public void Init_SetsAppropriateInitialState()\n        {{\n            var engine = new Hatiora.Pico8.Unity.Pico8Builder().WithCartridge(_cartridge).Build();\n            // TODO: Arrange & assert initial state\n            Assert.DoesNotThrow(() => _cartridge.Init());\n        }}\n";
                }
                if (Regex.IsMatch(sourceCode, @"override\s+void\s+(?:_update|_update60|Update)\s*\("))
                {
                    testMethods += $"\n        [Test]\n        public void Update_SetsAppropriateState()\n        {{\n            var engine = new Hatiora.Pico8.Unity.Pico8Builder().WithCartridge(_cartridge).Build();\n            _cartridge.Init();\n            // TODO: Arrange interactions or time\n            _cartridge.Update();\n            // TODO: Assert appropriate effects and PICO-8 API side-effects\n        }}\n";
                }
                if (Regex.IsMatch(sourceCode, @"override\s+void\s+(?:_draw|Draw)\s*\("))
                {
                    testMethods += $"\n        [Test]\n        public void Draw_CallsExpectedApiFunctions()\n        {{\n            var engine = new Hatiora.Pico8.Unity.Pico8Builder().WithCartridge(_cartridge).Build();\n            _cartridge.Init();\n            // TODO: Mount test buffers if asserting drawing output\n            Assert.DoesNotThrow(() => _cartridge.Draw());\n        }}\n";
                }
                
                string template = CartridgeTemplateService.GetTestTemplate();
                if (string.IsNullOrEmpty(template)) return;

                template = template.Replace("%NAMESPACE%", namespaceName);
                template = template.Replace("%CLASSNAME%", className);
                template = template.Replace("%TEST_METHODS%", testMethods);
                
                string destDir = Path.Combine(repoRoot, $"packages/{packageId}/Tests/Editor");
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                
                string destPath = Path.Combine(destDir, $"{className}Tests.cs");
                File.WriteAllText(destPath, template);
                
                Debug.Log($"[PICO-8 Tools] Generated Test Template: {destPath}");
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PICO-8 Tools] Code generation failed: {ex.Message}");
            }
        }
    }
}
