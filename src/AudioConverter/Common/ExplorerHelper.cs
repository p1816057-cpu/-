using System.Diagnostics;
using System.IO;

namespace AudioConverter.Common
{
    public static class ExplorerHelper
    {
        public static void OpenInExplorer(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (File.Exists(path))
            {
                Process.Start("explorer.exe", "/select,\"" + path + "\"");
            }
            else if (Directory.Exists(path))
            {
                Process.Start("explorer.exe", "\"" + path + "\"");
            }
        }
    }
}
