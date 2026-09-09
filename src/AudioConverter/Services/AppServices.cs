using System;
using System.IO;
using AudioConverter.Core;

namespace AudioConverter.Services
{
    /// <summary>
    /// 组合根：集中创建设置、历史、FFmpeg 定位与任务管理器。
    /// </summary>
    public sealed class AppServices
    {
        public AppServices()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            StorageRoot = Path.Combine(appData, "AudioConverter");
            Directory.CreateDirectory(StorageRoot);

            Settings = new SettingsService(StorageRoot);
            History = new HistoryService(StorageRoot, Settings.Current.HistoryLimit);
            Ffmpeg = new FfmpegLocator(Settings.Current);
            TaskManager = new ConversionTaskManager(Ffmpeg);
            ProbeRunner = new FfprobeRunner();

            Directory.CreateDirectory(Settings.Current.LogDirectory);
            Directory.CreateDirectory(Settings.Current.TempDirectory);
        }

        public string StorageRoot { get; }

        public SettingsService Settings { get; }

        public HistoryService History { get; }

        public FfmpegLocator Ffmpeg { get; }

        public ConversionTaskManager TaskManager { get; }

        public FfprobeRunner ProbeRunner { get; }
    }
}
