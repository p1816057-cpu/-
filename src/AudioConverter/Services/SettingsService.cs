using System;
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

            string music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (string.IsNullOrWhiteSpace(Current.OutputDirectory))
            {
                Current.OutputDirectory = Path.Combine(music, "AudioConverter");
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
