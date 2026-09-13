using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AudioConverter.Common;
using AudioConverter.Core;
using AudioConverter.Models;
using AudioConverter.Services;

namespace AudioConverter.Modules.Settings
{
    public sealed class SettingsViewModel : ViewModelBase
    {
        private readonly AppServices _services;
        private SettingOption _selectedDefaultFormat;
        private SettingImageOption _selectedImageDefaultFormat;
        private string _outputDirectory;
        private string _imageOutputDirectory;
        private string _logDirectory;
        private string _tempDirectory;
        private string _ffmpegDirectory;
        private bool _audioOutputProgramFolder;
        private bool _audioOutputSourceFolder;
        private bool _audioOutputCustom;
        private bool _imageOutputProgramFolder;
        private bool _imageOutputSourceFolder;
        private bool _imageOutputCustom;
        private bool _autoRetry;
        private int _retryLimit;
        private bool _overwrite;
        private bool _rename;
        private bool _skip;
        private string _selectedSection;
        private bool _isDirty;

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
            ImageFormatOptions = new ObservableCollection<SettingImageOption>
            {
                new SettingImageOption(ImageOutputFormat.Webp, "WebP"),
                new SettingImageOption(ImageOutputFormat.Jpg, "JPG"),
                new SettingImageOption(ImageOutputFormat.Png, "PNG")
            };

            _selectedDefaultFormat = FormatOptions.First(o => o.Value == settings.DefaultOutputFormat);
            _selectedImageDefaultFormat = ImageFormatOptions.First(o => o.Value == settings.DefaultImageFormat);
            _outputDirectory = settings.OutputDirectory;
            _imageOutputDirectory = settings.ImageOutputDirectory;
            _logDirectory = settings.LogDirectory;
            _tempDirectory = settings.TempDirectory;
            _ffmpegDirectory = settings.FfmpegDirectory;
            _audioOutputProgramFolder = settings.AudioOutputMode == OutputLocationMode.ProgramFolder;
            _audioOutputSourceFolder = settings.AudioOutputMode == OutputLocationMode.SourceFolder;
            _audioOutputCustom = settings.AudioOutputMode == OutputLocationMode.Custom;
            _imageOutputProgramFolder = settings.ImageOutputMode == OutputLocationMode.ProgramFolder;
            _imageOutputSourceFolder = settings.ImageOutputMode == OutputLocationMode.SourceFolder;
            _imageOutputCustom = settings.ImageOutputMode == OutputLocationMode.Custom;
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

            NavGroups = new List<SettingsNavGroup>
            {
                new SettingsNavGroup("功能区默认", new[]
                {
                    new SettingsNavItem("音频默认", "音频默认"),
                    new SettingsNavItem("图片默认", "图片默认")
                }),
                new SettingsNavGroup("输出位置", new[]
                {
                    new SettingsNavItem("音频输出", "音频输出"),
                    new SettingsNavItem("图片输出", "图片输出")
                }),
                new SettingsNavGroup("通用", new[]
                {
                    new SettingsNavItem("转换", "转换"),
                    new SettingsNavItem("文件冲突", "文件冲突"),
                    new SettingsNavItem("任务", "任务"),
                    new SettingsNavItem("存储", "存储")
                })
            };
            _selectedSection = "音频默认";
            RetryOptions = Enumerable.Range(1, 5).ToList();

            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave);
            ResetCommand = new RelayCommand(_ => ResetDefaults());
            BrowseOutputCommand = new RelayCommand(_ => BrowseOutput());
            BrowseImageOutputCommand = new RelayCommand(_ => BrowseImageOutput());
            BrowseLogCommand = new RelayCommand(_ => Browse(ref _logDirectory, nameof(LogDirectory)));
            BrowseTempCommand = new RelayCommand(_ => Browse(ref _tempDirectory, nameof(TempDirectory)));
            BrowseFfmpegCommand = new RelayCommand(_ => Browse(ref _ffmpegDirectory, nameof(FfmpegDirectory)));
            OpenDataFolderCommand = new RelayCommand(_ => OpenDataFolder());
            ExportLogCommand = new RelayCommand(_ => LogExporter.Export(_services, Application.Current?.MainWindow));

            // 任何一项设置被改动都要重新计算“保存”按钮是否可点
            PropertyChanged += OnSettingsPropertyChanged;
            UpdateDirtyState();
        }

        public ObservableCollection<SettingOption> FormatOptions { get; }

        public ObservableCollection<SettingImageOption> ImageFormatOptions { get; }

        public IReadOnlyList<SettingsNavGroup> NavGroups { get; }

        public IReadOnlyList<int> RetryOptions { get; }

        public ICommand SaveCommand { get; }

        public ICommand ResetCommand { get; }

        public ICommand BrowseOutputCommand { get; }

        public ICommand BrowseImageOutputCommand { get; }

        public ICommand BrowseLogCommand { get; }

        public ICommand BrowseTempCommand { get; }

        public ICommand BrowseFfmpegCommand { get; }

        public ICommand OpenDataFolderCommand { get; }

        public ICommand ExportLogCommand { get; }

        public string SelectedSection
        {
            get { return _selectedSection; }
            set { SetProperty(ref _selectedSection, value); }
        }

        /// <summary>当前界面上的设置与已保存的设置是否有差异（无差异时「保存设置」不可点）。</summary>
        public bool IsDirty
        {
            get { return _isDirty; }
        }

        public bool CanSave
        {
            get { return _isDirty; }
        }

        public SettingOption SelectedDefaultFormat
        {
            get { return _selectedDefaultFormat; }
            set { SetProperty(ref _selectedDefaultFormat, value); }
        }

        public SettingImageOption SelectedImageDefaultFormat
        {
            get { return _selectedImageDefaultFormat; }
            set { SetProperty(ref _selectedImageDefaultFormat, value); }
        }

        public string OutputDirectory
        {
            get { return _outputDirectory; }
            set { SetProperty(ref _outputDirectory, value); }
        }

        public string ImageOutputDirectory
        {
            get { return _imageOutputDirectory; }
            set { SetProperty(ref _imageOutputDirectory, value); }
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

        public bool AudioOutputProgramFolder
        {
            get { return _audioOutputProgramFolder; }
            set
            {
                if (SetProperty(ref _audioOutputProgramFolder, value) && value)
                {
                    SetProperty(ref _audioOutputSourceFolder, false, nameof(AudioOutputSourceFolder));
                    SetProperty(ref _audioOutputCustom, false, nameof(AudioOutputCustom));
                }

                OnPropertyChanged(nameof(AudioOutputDirectoryDisplay));
            }
        }

        public bool AudioOutputSourceFolder
        {
            get { return _audioOutputSourceFolder; }
            set
            {
                if (SetProperty(ref _audioOutputSourceFolder, value) && value)
                {
                    SetProperty(ref _audioOutputProgramFolder, false, nameof(AudioOutputProgramFolder));
                    SetProperty(ref _audioOutputCustom, false, nameof(AudioOutputCustom));
                }

                OnPropertyChanged(nameof(AudioOutputDirectoryDisplay));
            }
        }

        public bool AudioOutputCustom
        {
            get { return _audioOutputCustom; }
            set
            {
                if (SetProperty(ref _audioOutputCustom, value) && value)
                {
                    SetProperty(ref _audioOutputProgramFolder, false, nameof(AudioOutputProgramFolder));
                    SetProperty(ref _audioOutputSourceFolder, false, nameof(AudioOutputSourceFolder));
                }

                OnPropertyChanged(nameof(AudioOutputDirectoryDisplay));
            }
        }

        public bool ImageOutputProgramFolder
        {
            get { return _imageOutputProgramFolder; }
            set
            {
                if (SetProperty(ref _imageOutputProgramFolder, value) && value)
                {
                    SetProperty(ref _imageOutputSourceFolder, false, nameof(ImageOutputSourceFolder));
                    SetProperty(ref _imageOutputCustom, false, nameof(ImageOutputCustom));
                }

                OnPropertyChanged(nameof(ImageOutputDirectoryDisplay));
            }
        }

        public bool ImageOutputSourceFolder
        {
            get { return _imageOutputSourceFolder; }
            set
            {
                if (SetProperty(ref _imageOutputSourceFolder, value) && value)
                {
                    SetProperty(ref _imageOutputProgramFolder, false, nameof(ImageOutputProgramFolder));
                    SetProperty(ref _imageOutputCustom, false, nameof(ImageOutputCustom));
                }

                OnPropertyChanged(nameof(ImageOutputDirectoryDisplay));
            }
        }

        public bool ImageOutputCustom
        {
            get { return _imageOutputCustom; }
            set
            {
                if (SetProperty(ref _imageOutputCustom, value) && value)
                {
                    SetProperty(ref _imageOutputProgramFolder, false, nameof(ImageOutputProgramFolder));
                    SetProperty(ref _imageOutputSourceFolder, false, nameof(ImageOutputSourceFolder));
                }

                OnPropertyChanged(nameof(ImageOutputDirectoryDisplay));
            }
        }

        /// <summary>前两种模式下列出实际会用的目录（只读展示）；自定义模式由路径输入框接管。</summary>
        public string AudioOutputDirectoryDisplay
        {
            get { return DescribeOutputDirectory(_audioOutputSourceFolder); }
        }

        public string ImageOutputDirectoryDisplay
        {
            get { return DescribeOutputDirectory(_imageOutputSourceFolder); }
        }

        private static string DescribeOutputDirectory(bool sourceFolderMode)
        {
            if (sourceFolderMode)
            {
                return OutputLocation.SourceFolderDisplay + "（每个文件的「转换输出」子文件夹）";
            }

            string programOutput = OutputLocation.ProgramOutputDirectory;
            return OutputLocation.IsProgramFolderWritable()
                ? programOutput
                : programOutput + "（不可写，将改用源文件所在文件夹）";
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
            if (AudioOutputCustom && string.IsNullOrWhiteSpace(OutputDirectory))
            {
                ToastService.Instance.Show("请先为音频输出选择自定义文件夹", ToastKind.Warning);
                return;
            }

            if (ImageOutputCustom && string.IsNullOrWhiteSpace(ImageOutputDirectory))
            {
                ToastService.Instance.Show("请先为图片输出选择自定义文件夹", ToastKind.Warning);
                return;
            }

            var settings = _services.Settings.Current;
            settings.DefaultOutputFormat = SelectedDefaultFormat?.Value ?? OutputFormat.Mp3;
            settings.DefaultImageFormat = SelectedImageDefaultFormat?.Value ?? ImageOutputFormat.Webp;
            settings.OutputDirectory = OutputDirectory;
            settings.ImageOutputDirectory = ImageOutputDirectory;
            settings.AudioOutputMode = ResolveOutputMode(AudioOutputProgramFolder, AudioOutputSourceFolder, AudioOutputCustom);
            settings.ImageOutputMode = ResolveOutputMode(ImageOutputProgramFolder, ImageOutputSourceFolder, ImageOutputCustom);
            settings.LogDirectory = LogDirectory;
            settings.TempDirectory = TempDirectory;
            settings.FfmpegDirectory = FfmpegDirectory;
            settings.AutoRetry = AutoRetry;
            settings.RetryLimit = RetryLimit;
            settings.ConflictPolicy = Overwrite ? ConflictPolicy.Overwrite : (Skip ? ConflictPolicy.Skip : ConflictPolicy.Rename);

            try
            {
                EnsureOutputDirectory(settings.OutputDirectory, settings.AudioOutputMode);
                EnsureOutputDirectory(settings.ImageOutputDirectory, settings.ImageOutputMode);
                Directory.CreateDirectory(settings.LogDirectory);
                Directory.CreateDirectory(settings.TempDirectory);
            }
            catch (Exception ex)
            {
                ToastService.Instance.Show("目录不可用：" + ex.Message, ToastKind.Error);
                return;
            }

            _services.Settings.Save();
            UpdateDirtyState();
            ToastService.Instance.Show("设置已保存", ToastKind.Success);
        }

        /// <summary>只有自定义模式才需要固定文件夹；源文件模式下不创建任何目录。</summary>
        private static void EnsureOutputDirectory(string directory, OutputLocationMode mode)
        {
            if (mode == OutputLocationMode.Custom && !string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static OutputLocationMode ResolveOutputMode(bool programFolder, bool sourceFolder, bool custom)
        {
            if (custom)
            {
                return OutputLocationMode.Custom;
            }

            return sourceFolder ? OutputLocationMode.SourceFolder : OutputLocationMode.ProgramFolder;
        }

        private void ResetDefaults()
        {
            if (IsAtDefaults())
            {
                ToastService.Instance.Show("当前已是默认设置", ToastKind.Info);
                return;
            }

            if (!ToastService.Confirm("恢复默认设置将覆盖当前配置，是否继续？", "恢复默认"))
            {
                return;
            }

            string root = _services.Settings.StorageRoot;
            var settings = _services.Settings.Current;
            settings.DefaultOutputFormat = OutputFormat.Mp3;
            settings.DefaultImageFormat = ImageOutputFormat.Webp;
            // 默认输出到程序目录下的「转换输出」子文件夹，不再写入某台电脑的固定路径
            settings.AudioOutputMode = OutputLocationMode.ProgramFolder;
            settings.ImageOutputMode = OutputLocationMode.ProgramFolder;
            settings.OutputDirectory = string.Empty;
            settings.ImageOutputDirectory = string.Empty;
            settings.ConflictPolicy = ConflictPolicy.Rename;
            settings.AutoRetry = false;
            settings.RetryLimit = 2;
            settings.LogDirectory = Path.Combine(root, "logs");
            settings.TempDirectory = Path.Combine(Path.GetTempPath(), "AudioConverter");
            settings.FfmpegDirectory = "";

            _services.Settings.Save();

            SelectedDefaultFormat = FormatOptions.First(o => o.Value == settings.DefaultOutputFormat);
            SelectedImageDefaultFormat = ImageFormatOptions.First(o => o.Value == settings.DefaultImageFormat);
            OutputDirectory = settings.OutputDirectory;
            ImageOutputDirectory = settings.ImageOutputDirectory;
            AudioOutputProgramFolder = true;
            AudioOutputSourceFolder = false;
            AudioOutputCustom = false;
            ImageOutputProgramFolder = true;
            ImageOutputSourceFolder = false;
            ImageOutputCustom = false;
            LogDirectory = settings.LogDirectory;
            TempDirectory = settings.TempDirectory;
            FfmpegDirectory = "";
            AutoRetry = false;
            RetryLimit = 2;
            Overwrite = false;
            Skip = false;
            Rename = true;
            UpdateDirtyState();
            ToastService.Instance.Show("已恢复默认设置", ToastKind.Success);
        }

        /// <summary>转换页/压缩页手动改过输出位置后会立即保存，回到设置页时同步一次，避免用旧值覆盖。</summary>
        public override void OnActivated()
        {
            var settings = _services.Settings.Current;
            OutputDirectory = settings.OutputDirectory;
            ImageOutputDirectory = settings.ImageOutputDirectory;
            AudioOutputProgramFolder = settings.AudioOutputMode == OutputLocationMode.ProgramFolder;
            AudioOutputSourceFolder = settings.AudioOutputMode == OutputLocationMode.SourceFolder;
            AudioOutputCustom = settings.AudioOutputMode == OutputLocationMode.Custom;
            ImageOutputProgramFolder = settings.ImageOutputMode == OutputLocationMode.ProgramFolder;
            ImageOutputSourceFolder = settings.ImageOutputMode == OutputLocationMode.SourceFolder;
            ImageOutputCustom = settings.ImageOutputMode == OutputLocationMode.Custom;
            UpdateDirtyState();
        }

        private void OnSettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsDirty) || e.PropertyName == nameof(CanSave))
            {
                return;
            }

            UpdateDirtyState();
        }

        private void UpdateDirtyState()
        {
            bool dirty = ComputeDirty();
            if (dirty == _isDirty)
            {
                return;
            }

            _isDirty = dirty;
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(CanSave));
            CommandManager.InvalidateRequerySuggested();
        }

        private bool ComputeDirty()
        {
            var settings = _services.Settings.Current;
            return (SelectedDefaultFormat?.Value ?? OutputFormat.Mp3) != settings.DefaultOutputFormat
                || (SelectedImageDefaultFormat?.Value ?? ImageOutputFormat.Webp) != settings.DefaultImageFormat
                || ResolveOutputMode(AudioOutputProgramFolder, AudioOutputSourceFolder, AudioOutputCustom) != settings.AudioOutputMode
                || ResolveOutputMode(ImageOutputProgramFolder, ImageOutputSourceFolder, ImageOutputCustom) != settings.ImageOutputMode
                || !SamePath(OutputDirectory, settings.OutputDirectory)
                || !SamePath(ImageOutputDirectory, settings.ImageOutputDirectory)
                || !SamePath(LogDirectory, settings.LogDirectory)
                || !SamePath(TempDirectory, settings.TempDirectory)
                || !SamePath(FfmpegDirectory, settings.FfmpegDirectory)
                || AutoRetry != settings.AutoRetry
                || RetryLimit != settings.RetryLimit
                || Overwrite != (settings.ConflictPolicy == ConflictPolicy.Overwrite)
                || Skip != (settings.ConflictPolicy == ConflictPolicy.Skip)
                || Rename != (settings.ConflictPolicy == ConflictPolicy.Rename);
        }

        private static bool SamePath(string left, string right)
        {
            return string.Equals((left ?? string.Empty).Trim(), (right ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>界面上是否就是出厂默认值（用于「恢复默认」按钮的提示）。</summary>
        private bool IsAtDefaults()
        {
            return (SelectedDefaultFormat?.Value ?? OutputFormat.Mp3) == OutputFormat.Mp3
                && (SelectedImageDefaultFormat?.Value ?? ImageOutputFormat.Webp) == ImageOutputFormat.Webp
                && AudioOutputProgramFolder
                && ImageOutputProgramFolder
                && string.IsNullOrWhiteSpace(OutputDirectory)
                && string.IsNullOrWhiteSpace(ImageOutputDirectory)
                && Rename
                && !Overwrite
                && !Skip
                && !AutoRetry
                && RetryLimit == 2
                && string.IsNullOrWhiteSpace(FfmpegDirectory)
                && SamePath(LogDirectory, Path.Combine(_services.Settings.StorageRoot, "logs"))
                && SamePath(TempDirectory, Path.Combine(Path.GetTempPath(), "AudioConverter"));
        }

        private void Browse(ref string value, string propertyName)
        {
            string selected = PickFolder(value);
            if (selected != null)
            {
                value = selected;
                OnPropertyChanged(propertyName);
            }
        }

        /// <summary>音频输出选目录：选中固定文件夹后自动切到自定义模式。</summary>
        private void BrowseOutput()
        {
            string selected = PickFolder(OutputDirectory);
            if (selected != null)
            {
                OutputDirectory = selected;
                AudioOutputCustom = true;
            }
        }

        /// <summary>图片输出选目录：选中固定文件夹后自动切到自定义模式。</summary>
        private void BrowseImageOutput()
        {
            string selected = PickFolder(ImageOutputDirectory);
            if (selected != null)
            {
                ImageOutputDirectory = selected;
                ImageOutputCustom = true;
            }
        }

        private static string PickFolder(string current)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "请选择目录";
                dialog.ShowNewFolderButton = true;
                if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current))
                {
                    dialog.SelectedPath = current;
                }

                return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
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
