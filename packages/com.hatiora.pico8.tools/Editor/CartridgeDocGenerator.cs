using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Hatiora.Pico8.Tools.Editor
{
    public static class CartridgeDocGenerator
    {
        private static HashSet<string> GetPico8Apis()
        {
            var pico8Type = typeof(Hatiora.Pico8.IPico8);
            var methods = pico8Type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            return new HashSet<string>(methods.Select(m => m.Name), StringComparer.OrdinalIgnoreCase);
        }

        public static void Generate(string rawName, string packageId, string refsRelDir, string fileFilter)
        {
            try
            {
                string className = rawName + "Cartridge";
                
                var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));

                // Parse C# Source for API usage
                string sourcePath = Path.Combine(repoRoot, $"packages/{packageId}/Runtime/{className}.cs");
                if (!File.Exists(sourcePath))
                {
                    Debug.LogError($"[PICO-8 Tools] Source file not found: {sourcePath}");
                    return;
                }
                string sourceCode = File.ReadAllText(sourcePath);
                
                var csharpApis = new HashSet<string>();
                var pico8Apis = GetPico8Apis();
                var matches = Regex.Matches(sourceCode, @"\b([A-Z][A-Za-z0-9_]*)\s*\(");
                foreach (Match match in matches)
                {
                    string method = match.Groups[1].Value;
                    if (pico8Apis.Contains(method))
                    {
                        csharpApis.Add(method);
                    }
                }

                // Parse Lua Source
                // Parse Lua Source & Functions
                string luaSource = "";
                var luaApis = new HashSet<string>();
                string dataBlocksStr = "*(None detected)*";
                var dataBlocks = new List<string>();
                
                string searchDir = Path.Combine(repoRoot, refsRelDir);
                if (Directory.Exists(searchDir))
                {
                    // Find all target .p8 and .lua files in the directory
                    var files = new List<string>();
                    files.AddRange(Directory.GetFiles(searchDir, fileFilter));
                    files.AddRange(Directory.GetFiles(searchDir, "*.lua"));
                    
                    // Deduplicate in case fileFilter is "*.lua" or we have overlaps
                    files = files.Distinct().ToList();

                    foreach (var path in files)
                    {
                        string p8Content = File.ReadAllText(path);
                        
                        if (path.EndsWith(".lua"))
                        {
                            luaSource += $"-- File: {Path.GetFileName(path)}\n" + p8Content + "\n\n";
                        }
                        else
                        {
                            var luaMatch = Regex.Match(p8Content, @"__lua__\s*(.*?)(?:__[a-z]+__|$)", RegexOptions.Singleline);
                            if (luaMatch.Success)
                            {
                                luaSource += $"-- File: {Path.GetFileName(path)}\n" + luaMatch.Groups[1].Value.Trim() + "\n\n";
                            }
                            
                            var blockMatches = Regex.Matches(p8Content, @"__([a-z]+)__");
                            foreach (Match bm in blockMatches)
                            {
                                string blockName = bm.Groups[1].Value;
                                if (blockName != "lua" && !dataBlocks.Contains(blockName))
                                {
                                    dataBlocks.Add(blockName);
                                }
                            }
                        }
                    }
                    
                    if (dataBlocks.Count > 0)
                    {
                        dataBlocksStr = "- " + string.Join("\n- ", dataBlocks.Select(b => $"`__{b}__`"));
                    }

                    if (string.IsNullOrWhiteSpace(luaSource))
                    {
                        luaSource = "*(Lua block not found)*";
                    }
                    else
                    {
                        var funcMatches = Regex.Matches(luaSource, @"\b([a-zA-Z0-9_]+)\s*\(");
                        foreach (Match fm in funcMatches)
                        {
                            string rawFunc = fm.Groups[1].Value.ToLower();
                            if (pico8Apis.Contains(rawFunc))
                            {
                                luaApis.Add(char.ToUpper(rawFunc[0]) + rawFunc.Substring(1).ToLower());
                            }
                        }
                        
                        // Fallback matchers for syntax-heavy APIs
                        if (Regex.IsMatch(luaSource, @"\b(?:time|t)\b")) luaApis.Add("Time");
                        if (Regex.IsMatch(luaSource, @"\b(?:print|printh)\b")) luaApis.Add("Print");
                        if (Regex.IsMatch(luaSource, @"\b(?:cos|sin|atan2|sqrt|flr|ceil|sgn|abs|min|max|mid|rnd|srand|band|bor|bxor|bnot|shl|shr|lshr|rotl|rotr)\b")) 
                        {
                            foreach(var mathFunc in new[] { "cos", "sin", "atan2", "sqrt", "flr", "ceil", "sgn", "abs", "min", "max", "mid", "rnd", "srand", "band", "bor", "bxor", "bnot", "shl", "shr", "lshr", "rotl", "rotr" })
                            {
                                if (Regex.IsMatch(luaSource, $@"\b{mathFunc}\b"))
                                {
                                    luaApis.Add(char.ToUpper(mathFunc[0]) + mathFunc.Substring(1).ToLower());
                                }
                            }
                        }
                    }
                }

                // Construct Comparison Table
                var allApis = new HashSet<string>(csharpApis);
                allApis.UnionWith(luaApis);
                
                string apiTableStrStr = "| API Function | Native Lua Script | C# Translation |\n| :--- | :---: | :---: |\n";
                if (allApis.Count == 0)
                {
                    apiTableStrStr += "| *(None Detected)* | - | - |\n";
                }
                else
                {
                    foreach (var api in allApis.OrderBy(a => a))
                    {
                        string luaCheck = luaApis.Contains(api) ? "✅ Used" : "❌";
                        string csCheck = csharpApis.Contains(api) ? "✅ Used" : "❌";
                        apiTableStrStr += $"| `{api}` | {luaCheck} | {csCheck} |\n";
                    }
                }

                // Inject into Template
                string template = CartridgeTemplateService.GetDocTemplate();
                if (string.IsNullOrEmpty(template)) return;

                template = template.Replace("%CARTRIDGE_NAME%", rawName + " Cartridge");
                template = template.Replace("%ORIGINAL_P8%", string.Join(", ", Directory.GetFiles(searchDir, fileFilter).Select(Path.GetFileName)));
                template = template.Replace("%REF_REL_DIR%", refsRelDir);
                template = template.Replace("%CLASSNAME%", className);
                template = template.Replace("%PACKAGE_NAME%", packageId);
                template = template.Replace("%API_COMPARISON_TABLE%", apiTableStrStr);
                template = template.Replace("%DATA_BLOCKS%", dataBlocksStr);
                template = template.Replace("%CSHARP_SOURCE%", sourceCode);
                template = template.Replace("%LUA_SOURCE%", luaSource);
                
                // Output
                string destDir = Path.Combine(repoRoot, $"packages/{packageId}");
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
                
                string destPath = Path.Combine(destDir, "README.md");
                File.WriteAllText(destPath, template);
                
                Debug.Log($"[PICO-8 Tools] Generated Documentation: {destPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PICO-8 Tools] Documentation generation failed: {ex.Message}");
            }
        }
    }
}
