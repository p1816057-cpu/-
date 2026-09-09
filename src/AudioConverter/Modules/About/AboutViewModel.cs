using System.Windows.Input;
using AudioConverter.Common;
using AudioConverter.Services;

namespace AudioConverter.Modules.About
{
    public sealed class AboutViewModel : ViewModelBase
    {
        private readonly AppServices _services;

        public AboutViewModel(AppServices services)
        {
            _services = services;
            Title = "关于";
            FfmpegVersion = services.Ffmpeg.GetFfmpegVersion();
            AppVersion = "Version 1.1.0";
            SloganChinese = "天融万物，格式无界";
            SloganEnglish = "Fuse Anything, Convert Anything";
            CompatibilityText = AudioConverter.Common.WindowsCompatibility.CompatibilityText;
            CopyrightText = "Copyright © 2026 天融 SkyFusion";
            OpenGitHubCommand = new RelayCommand(_ => OpenGitHub());
            ExportLogCommand = new RelayCommand(_ => LogExporter.Export(_services, System.Windows.Application.Current?.MainWindow));
        }

        public string FfmpegVersion { get; }

        public string AppVersion { get; }

        public string SloganChinese { get; }

        public string SloganEnglish { get; }

        public string CompatibilityText { get; }

        public string CopyrightText { get; }

        public ICommand OpenGitHubCommand { get; }

        public ICommand ExportLogCommand { get; }

        private void OpenGitHub()
        {
            Services.ToastService.Instance.Show(
                "GitHub 仓库地址将在项目上架后填写。",
                Services.ToastKind.Info);
        }
    }
}
