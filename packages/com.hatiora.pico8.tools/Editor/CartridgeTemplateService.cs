using System.IO;
using UnityEngine;

namespace Hatiora.Pico8.Tools.Editor
{
    public static class CartridgeTemplateService
    {
        private const string TestTemplatePath = "Packages/com.hatiora.pico8.tools/Editor/Templates/CartridgeTest.template";
        private const string DocTemplatePath = "Packages/com.hatiora.pico8.tools/Editor/Templates/CartridgeDoc.template";

        private const string PackageJsonTemplatePath = "Packages/com.hatiora.pico8.tools/Editor/Templates/PackageJson.template";
        private const string RuntimeAsmdefTemplatePath = "Packages/com.hatiora.pico8.tools/Editor/Templates/RuntimeAsmdef.template";
        private const string CartridgeRuntimeTemplatePath = "Packages/com.hatiora.pico8.tools/Editor/Templates/CartridgeRuntime.template";
        private const string TestsAsmdefTemplatePath = "Packages/com.hatiora.pico8.tools/Editor/Templates/TestsAsmdef.template";

        public static string GetTestTemplate()
        {
            return LoadTemplate(TestTemplatePath);
        }

        public static string GetDocTemplate()
        {
            return LoadTemplate(DocTemplatePath);
        }

        public static string GetPackageJsonTemplate()
        {
            return LoadTemplate(PackageJsonTemplatePath);
        }

        public static string GetRuntimeAsmdefTemplate()
        {
            return LoadTemplate(RuntimeAsmdefTemplatePath);
        }

        public static string GetCartridgeRuntimeTemplate()
        {
            return LoadTemplate(CartridgeRuntimeTemplatePath);
        }

        public static string GetTestsAsmdefTemplate()
        {
            return LoadTemplate(TestsAsmdefTemplatePath);
        }

        private static string LoadTemplate(string relativePath)
        {
            string fullPath = Path.GetFullPath(relativePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[PICO-8 Tools] Template file not found at: {fullPath}");
                return string.Empty;
            }
            return File.ReadAllText(fullPath);
        }
    }
}
