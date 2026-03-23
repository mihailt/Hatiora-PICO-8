using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Hatiora.Pico8.Tools.Editor
{
    public enum CartStatus { Clean, InProgress, Completed }
    public enum CartPortStatus { Clean, InProgress, Completed, NotRelevant }
    public enum CartDocStatus { Clean, InProgress, Completed }

    public class CartConfig
    {
        public string rawName;
        public string fullPkgName;
        public string refsRelDir;
        public string fileFilter;
        public CartStatus status;
        public CartPortStatus portStatus;
        public CartDocStatus docStatus;
    }

    [System.Serializable]
    public class CartridgeProgressStore
    {
        public List<CartridgeProgressEntry> entries = new List<CartridgeProgressEntry>();
    }

    [System.Serializable]
    public class CartridgeProgressEntry
    {
        public string fullPkgName;
        public string status;
        public string portStatus;
        public string docStatus;
    }

    public static class CartridgeService
    {
        private static string GetProgressFilePath()
        {
            var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            return Path.Combine(repoRoot, "CartridgeProgress.json");
        }

        private static CartridgeProgressStore LoadProgressStore()
        {
            string path = GetProgressFilePath();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var store = JsonUtility.FromJson<CartridgeProgressStore>(json);
                if (store != null && store.entries != null) return store;
            }
            return new CartridgeProgressStore { entries = new List<CartridgeProgressEntry>() };
        }

        private static void SaveProgressStore(CartridgeProgressStore store)
        {
            string path = GetProgressFilePath();
            string json = JsonUtility.ToJson(store, true);
            File.WriteAllText(path, json);
        }

        public static List<CartConfig> GetInstalledCartridges()
        {
            var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
            var packagesDir = Path.Combine(repoRoot, "packages");
            var carts = new List<CartConfig>();

            if (!Directory.Exists(packagesDir)) return carts;

            var store = LoadProgressStore();
            var progressMap = store.entries.ToDictionary(e => e.fullPkgName);

            var dirs = Directory.GetDirectories(packagesDir, "com.hatiora.pico8.*");
            foreach (var dir in dirs)
            {
                string dirName = Path.GetFileName(dir);
                if (dirName.EndsWith(".tools") || dirName.EndsWith(".unity")) continue;
                if (dirName == "com.hatiora.pico8") continue;

                string suffix = dirName.Replace("com.hatiora.pico8.", "");
                string rawName = char.ToUpper(suffix[0]) + suffix.Substring(1);
                if (rawName == "Dots3d") rawName = "Dots3D";

                string refsRelDir = $"packages/{dirName}/Pico8";
                string fileFilter = "*.p8";

                // Check JSON store for work completion
                CartStatus status = CartStatus.Clean;
                CartPortStatus portStatus = CartPortStatus.Clean;
                CartDocStatus docStatus = CartDocStatus.Clean;

                if (progressMap.TryGetValue(dirName, out var entry))
                {
                    if (System.Enum.TryParse(entry.status, true, out CartStatus s)) status = s;
                    if (System.Enum.TryParse(entry.portStatus, true, out CartPortStatus ps)) portStatus = ps;
                    if (System.Enum.TryParse(entry.docStatus, true, out CartDocStatus ds)) docStatus = ds;
                }

                carts.Add(new CartConfig
                {
                    rawName = rawName,
                    fullPkgName = dirName,
                    refsRelDir = refsRelDir,
                    fileFilter = fileFilter,
                    status = status,
                    portStatus = portStatus,
                    docStatus = docStatus
                });
            }
            return carts.OrderBy(c => c.rawName).ToList();
        }

        public static void SetCartridgeFlag(string fullPkgName, string flagName, string value)
        {
            var store = LoadProgressStore();
            var entry = store.entries.FirstOrDefault(e => e.fullPkgName == fullPkgName);
            
            if (entry == null)
            {
                entry = new CartridgeProgressEntry 
                { 
                    fullPkgName = fullPkgName, 
                    status = CartStatus.Clean.ToString(), 
                    portStatus = CartPortStatus.Clean.ToString(), 
                    docStatus = CartDocStatus.Clean.ToString() 
                };
                store.entries.Add(entry);
            }

            if (flagName == "hatiora_status") entry.status = value;
            else if (flagName == "hatiora_portStatus") entry.portStatus = value;
            else if (flagName == "hatiora_docStatus") entry.docStatus = value;

            SaveProgressStore(store);
        }
    }
}
