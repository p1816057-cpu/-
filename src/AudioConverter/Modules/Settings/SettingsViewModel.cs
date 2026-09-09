using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AudioConverter.Common;
using AudioConverter.Models;
using AudioConverter.Services;

namespace AudioConverter.Modules.Settings
{
    public sealed class SettingsViewModel : ViewModelBase
    {
        private readonly AppServices _services;
        private SettingOption _selectedDefaultFormat;
        private string _outputDirectory;
        private string _logDirectory;
        private string _tempDirectory;
        private string _ffmpegDirectory;
        private bool _autoRetry;
        private int _retryLimit;
        private bool _overwrite;
        private bool _rename;
        private bool _skip;
        private string _selectedSection;

        public SettingsViewModel(AppServices services)
        {
            _services = services;
            Title = "设置";

            var settings = _services.Settings.Current;
            FormatOptions = new ObservableCollection<SettingOption>
            {
                new SettingOption(OutputFormat.Mp3, "MP3"),
                new SettingOption(OutputFormat.Wav, "WAV"),
                new SettingOption(OutputFormat.Flac, "FLAC")
            };

            _selectedDefaultFormat = FormatOptions.First(o => o.Value == settings.DefaultOutputFormat);
            _outputDirectory = settings.OutputDirectory;
            _logDirectory = settings.LogDirectory;
            _tempDirectory = settings.TempDirectory;
            _ffmpegDirectory = settings.FfmpegDirectory;
            _autoRetry = settings.AutoRetry;
            _retryLimit = settings.RetryLimit;

            switch (settings.ConflictPolicy)
            {
                case ConflictPolicy.Overwrite:
                    _overwrite = true;
                    break;
                case ConflictPolicy.Skip:
                    _skip = true;
                    break;
                default:
                    _rename = true;
                    break;
            }

            Sections = new[] { "常规", "转换", "输出", "文件冲突", "任务", "存储" };
            _selectedSection = Sections[0];
            RetryOptions = Enumerable.Range(1, 5).ToList();

            SaveCommand = new RelayCommand(_ => Save());
            ResetCommand = new RelayCommand(_ => ResetDefaults());
            BrowseOutputCommand = new RelayCommand(_ => Browse(ref _outputDirectory, nameof(OutputDirectory)));
            BrowseLogCommand = new RelayCommand(_ => Browse(ref _logDirectory, nameof(LogDirectory)));
            BrowseTempCommand = new RelayCommand(_ => Browse(ref _tempDirectory, nameof(TempDirectory)));
            BrowseFfmpegCommand = new RelayCommand(_ => Browse(ref _ffmpegDirectory, nameof(FfmpegDirectory)));
            OpenDataFolderCommand = new RelayCommand(_ => OpenDataFolder());
        }

        public ObservableCollection<SettingOption> FormatOptions { get; }

        public IReadOnlyList<string> Sections { get; }

        public IReadOnlyList<int> RetryOptions { get; }

        public ICommand SaveCommand { get; }

        public ICommand ResetCommand { get; }

        public ICommand BrowseOutputCommand { get; }

        public ICommand BrowseLogCommand { get; }

        public ICommand BrowseTempCommand { get; }

        public ICommand BrowseFfmpegCommand { get; }

        public ICommand OpenDataFolderCommand { get; }

        public string SelectedSection
        {
            get { return _selectedSection; }
            set { SetProperty(ref _selectedSection, value); }
        }

        public SettingOption SelectedDefaultFormat
        {
            get { return _selectedDefaultFormat; }
            set { SetProperty(ref _selectedDefaultFormat, value); }
        }

        public string OutputDirectory
        {
            get { return _outputDirectory; }
            set { SetProperty(ref _outputDirectory, value); }
        }

        public string LogDirectory
        {
            get { return _logDirectory; }
            set { SetProperty(ref _logDirectory, value); }
        }

        public string TempDirectory
        {
            get { return _tempDirectory; }
            set { SetProperty(ref _tempDirectory, value); }
        }

        public string FfmpegDirectory
        {
            get { return _ffmpegDirectory; }
            set { SetProperty(ref _ffmpegDirectory, value); }
        }

        public bool AutoRetry
        {
            get { return _autoRetry; }
            set { SetProperty(ref _autoRetry, value); }
        }

        public int RetryLimit
        {
            get { return _retryLimit; }
            set { SetProperty(ref _retryLimit, value); }
        }

        public bool Overwrite
        {
            get { return _overwrite; }
            set
            {
                if (SetProperty(ref _overwrite, value) && value)
                {
                    SetProperty(ref _rename, false, nameof(Rename));
                    SetProperty(ref _skip, false, nameof(Skip));
                }
            }
        }

        public bool Rename
        {
            get { return _rename; }
            set
            {
                if (SetProperty(ref _rename, value) && value)
                {
                    SetProperty(ref _overwrite, false, nameof(Overwrite));
                    SetProperty(ref _skip, false, nameof(Skip));
                }
            }
        }

        public bool Skip
        {
            get { return _skip; }
            set
            {
                if (SetProperty(ref _skip, value) && value)
                {
                    SetProperty(ref _overwrite, false, nameof(Overwrite));
                    SetProperty(ref _rename, false, nameof(Rename));
                }
            }
        }

        private void Save()
        {
            var settings = _services.Settings.Current;
            settings.DefaultOutputFormat = SelectedDefaultFormat?.Value ?? OutputFormat.Mp3;
            settings.OutputDirectory = OutputDirectory;
            settings.LogDirectory = LogDirectory;
            settings.TempDirectory = TempDirectory;
            settings.FfmpegDirectory = FfmpegDirectory;
            settings.AutoRetry = AutoRetry;
            settings.RetryLimit = RetryLimit;
            settings.ConflictPolicy = Overwrite ? ConflictPolicy.Overwrite : (Skip ? ConflictPolicy.Skip : ConflictPolicy.Rename);

            try
            {
                Directory.CreateDirectory(settings.OutputDirectory);
                Directory.CreateDirectory(settings.LogDirectory);
                Directory.CreateDirectory(settings.TempDirectory);
            }
            catch (Exception ex)
            {
                ToastService.Instance.Show("目录不可用：" + ex.Message, ToastKind.Error);
                return;
            }

            _services.Settings.Save();
            ToastService.Instance.Show("设置已保存", ToastKind.Success);
        }

        private void ResetDefaults()
        {
            if (!ToastService.Confirm("恢复默认设置将覆盖当前配置，是否继续？", "恢复默认"))
            {
                return;
            }

            string music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            string root = _services.Settings.StorageRoot;
            var settings = _services.Settings.Current;
            settings.DefaultOutputFormat = OutputFormat.Mp3;
            settings.OutputDirectory = Path.Combine(music, "AudioConverter");
            settings.ConflictPolicy = ConflictPolicy.Rename;
            settings.AutoRetry = false;
            settings.RetryLimit = 2;
            settings.LogDirectory = Path.Combine(root, "logs");
            settings.TempDirectory = Path.Combine(Path.GetTempPath(), "AudioConverter");
            settings.FfmpegDirectory = "";

            _services.Settings.Save();

            SelectedDefaultFormat = FormatOptions.First(o => o.Value == settings.DefaultOutputFormat);
            OutputDirectory = settings.OutputDirectory;
            LogDirectory = settings.LogDirectory;
            TempDirectory = settings.TempDirectory;
            FfmpegDirectory = "";
            AutoRetry = false;
            RetryLimit = 2;
            Overwrite = false;
            Skip = false;
            Rename = true;
            ToastService.Instance.Show("已恢复默认设置", ToastKind.Success);
        }

        private void Browse(ref string value, string propertyName)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "请选择目录";
                dialog.ShowNewFolderButton = true;
                if (!string.IsNullOrWhiteSpace(value) && Directory.Exists(value))
                {
                    dialog.SelectedPath = value;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    value = dialog.SelectedPath;
                    OnPropertyChanged(propertyName);
                }
            }
        }

        private void OpenDataFolder()
        {
            try
            {
                Process.Start("explorer.exe", _services.Settings.StorageRoot);
            }
            catch (Exception ex)
            {
                ToastService.Instance.Show("无法打开数据目录：" + ex.Message, ToastKind.Error);
            }
        }
    }
}
