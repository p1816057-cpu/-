using System.IO;
using AudioConverter.Data;
using AudioConverter.Models;

namespace AudioConverter.Services
{
    public sealed class SettingsService
    {
        public SettingsService(string storageRoot)
        {
            StorageRoot = storageRoot;
            SettingsPath = Path.Combine(storageRoot, "settings.json");
            Current = JsonFile.Load<AppSettings>(SettingsPath) ?? new AppSettings();

            // 输出位置默认是程序目录下的「转换输出」（见 Core/OutputLocation.cs），
            // 这里不再写入 %USERPROFILE%\Music 之类的固定绝对路径，避免换电脑后路径失效。
            // 旧版本残留的固定路径在非自定义模式下不再保留，避免看起来“默认路径还是旧的”。
            bool cleanedLegacyPaths = false;
            if (Current.AudioOutputMode != OutputLocationMode.Custom)
            {
                cleanedLegacyPaths = !string.IsNullOrWhiteSpace(Current.OutputDirectory);
                Current.OutputDirectory = string.Empty;
            }

            if (Current.ImageOutputMode != OutputLocationMode.Custom)
            {
                cleanedLegacyPaths |= !string.IsNullOrWhiteSpace(Current.ImageOutputDirectory);
                Current.ImageOutputDirectory = string.Empty;
            }
            else if (string.IsNullOrWhiteSpace(Current.ImageOutputDirectory))
            {
                Current.ImageOutputDirectory = Current.OutputDirectory;
            }

            if (string.IsNullOrWhiteSpace(Current.LogDirectory))
            {
                Current.LogDirectory = Path.Combine(storageRoot, "logs");
            }

            if (string.IsNullOrWhiteSpace(Current.TempDirectory))
            {
                Current.TempDirectory = Path.Combine(Path.GetTempPath(), "AudioConverter");
            }

            if (Current.HistoryLimit <= 0)
            {
                Current.HistoryLimit = 200;
            }

            // 清理过旧的固定路径就落盘一次，保证配置文件里也不会残留别的电脑的目录
            if (cleanedLegacyPaths)
            {
                Save();
            }
        }

        public string StorageRoot { get; }

        public string SettingsPath { get; }

        public AppSettings Current { get; }

        public void Save()
        {
            JsonFile.Save(SettingsPath, Current);
        }
    }
}
