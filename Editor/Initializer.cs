using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.PackageManager;
using UnityEngine;
using static LazyRedpaw.StaticHashes.Constants;

namespace LazyRedpaw.StaticHashes
{
    public class Initializer// : AssetPostprocessor
    {
        // private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        // {
        //     var inPackages = importedAssets.Any(path => path.StartsWith("Packages/")) ||
        //                      deletedAssets.Any(path => path.StartsWith("Packages/")) ||
        //                      movedAssets.Any(path => path.StartsWith("Packages/")) ||
        //                      movedFromAssetPaths.Any(path => path.StartsWith("Packages/"));
        //
        //     if (inPackages)
        //     {
        //         InitializeOnLoad();
        //     }
        // }
        //
        // [InitializeOnLoadMethod]
        // private static void InitializeOnLoad()
        // {
        //     var listRequest = Client.List(true);
        //     while (!listRequest.IsCompleted)
        //         Thread.Sleep(100);
        //
        //     if (listRequest.Error != null)
        //     {
        //         Debug.Log("Error: " + listRequest.Error.message);
        //         return;
        //     }
        //
        //     var packages = listRequest.Result;
        //     var text = new StringBuilder("Packages:\n");
        //     foreach (var package in packages)
        //     {
        //         if (package.source == PackageSource.Registry)
        //             text.AppendLine($"{package.name}: {package.version} [{package.resolvedPath}]");
        //     }
        //
        //     Debug.Log(text.ToString());
        // }
        
        // static Initializer()
        // {
        //     AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
        // }
        //
        // private static void OnImportPackageCompleted(string packagename)
        // {
        //     Debug.Log($"Imported package: {packagename}");
        // }
        //
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (!File.Exists(StaticHashesHelperFilePath))
            {
                string directory = Path.GetDirectoryName(StaticHashesHelperFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Debug.Log("Directory created");
                }
                File.WriteAllText(StaticHashesHelperFilePath, HelperFileDefaultText);
                AssetDatabase.Refresh();
                Debug.Log("Static hashes initialized");
            }
        }
    }
}