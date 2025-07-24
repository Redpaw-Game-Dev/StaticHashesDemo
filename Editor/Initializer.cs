using System.IO;
using UnityEditor.Callbacks;
using static LazyRedpaw.StaticHashes.Constants;

namespace LazyRedpaw.StaticHashes
{
    public class Initializer
    {
        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (!File.Exists(StaticHashesHelperFilePath))
            {
                string directory = Path.GetDirectoryName(StaticHashesHelperFilePath);
                if(!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(StaticHashesHelperFilePath, HelperFileDefaultText);
            }
        }
    }
}