using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AudioConverter.Common;
using AudioConverter.Core;
using AudioConverter.Models;
using AudioConverter.Services;
using Microsoft.Win32;

namespace AudioConverter.Modules.Conversion
{
    public sealed class ConversionViewModel : ViewModelBase
    {
        private static readonly string[] AllowedExtensions =
            { ".mp3", ".wav", ".flac", ".mp4", ".mkv" };

        private readonly AppServices _services;
        private OutputFormatOption _selectedFormatOption;
        private OutputLocationMode _outputMode;
        private string _customOutputDirectory;
        private bool? _selectAllState;
        private int _selectedCount;
        private int _addedCount;
        private CancellationTokenSource _cancellation;
        private bool _isRunning;
        private FileItemViewModel _activeTask;

        public ConversionViewModel(AppServices services)
        {
            _services = services;
            Title = "转换";
            Items = new ObservableCollection<FileItemViewModel>();

            OutputFormatOptions = new List<OutputFormatOption>
            {
                new OutputFormatOption(OutputFormat.Mp3, "MP3", "320 kbps · CBR"),
                new OutputFormatOption(OutputFormat.Wav, "WAV", "16-bit PCM"),
                new OutputFormatOption(OutputFormat.Flac, "FLAC", "Compression Level 5")
            };

            var preferred = _services.Settings.Current.DefaultOutputFormat;
            _selectedFormatOption = OutputFormatOptions.First(o => o.Value == preferred);
            _outputMode = _services.Settings.Current.AudioOutputMode;
            _customOutputDirectory = _services.Settings.Current.OutputDirectory ?? string.Empty;

            AddFilesCommand = new RelayCommand(_ => AddFiles());
            BrowseOutputCommand = new RelayCommand(_ => BrowseOutputDirectory());
            UseDefaultOutputCommand = new RelayCommand(_ => ApplyOutputLocation(OutputLocationMode.ProgramFolder, CustomOutputDirectory));
            StartCommand = new AsyncRelayCommand(_ => StartConversionAsync());
            CancelCommand = new RelayCommand(_ => CancelConversion(), _ => _isRunning);
            RemoveFileCommand = new RelayCommand(p => RemoveFile(p as FileItemViewModel), p => CanRemove(p as FileItemViewModel));
            ClearAllCommand = new RelayCommand(_ => ClearAll(), _ => CanClearAll());

            Items.CollectionChanged += (s, e) => UpdateCounters();

            UpdateCounters();
            UpdateSelectedFormatInfo();
        }

        public ObservableCollection<FileItemViewModel> Items { get; }

        public IReadOnlyList<OutputFormatOption> OutputFormatOptions { get; }

        public ICommand AddFilesCommand { get; }

        public ICommand BrowseOutputCommand { get; }

        /// <summary>自定义模式下改回默认输出位置（程序所在文件夹的「转换输出」）。</summary>
        public ICommand UseDefaultOutputCommand { get; }

        public ICommand StartCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand RemoveFileCommand { get; }

        public ICommand ClearAllCommand { get; }

        public OutputFormatOption SelectedFormatOption
        {
            get { return _selectedFormatOption; }
            set
            {
                if (SetProperty(ref _selectedFormatOption, value) && value != null)
                {
                    UpdateSelectedFormatInfo();
                }
            }
        }

        /// <summary>输出位置模式：默认跟随源文件，用户可在转换页或设置页改为固定文件夹。</summary>
        public OutputLocationMode OutputMode
        {
            get { return _outputMode; }
            private set
            {
                if (SetProperty(ref _outputMode, value))
                {
                    RaiseOutputLocationState();
                }
            }
        }

        /// <summary>用户手动选择过的固定输出文件夹（仅自定义模式生效）。</summary>
        public string CustomOutputDirectory
        {
            get { return _customOutputDirectory; }
            private set
            {
                if (SetProperty(ref _customOutputDirectory, value ?? string.Empty))
                {
                    RaiseOutputLocationState();
                }
            }
        }

        /// <summary>输出框显示文案：源文件模式显示「与源文件相同文件夹」，自定义模式显示实际路径。</summary>
        public string OutputDirectoryDisplay
        {
            get
            {
                switch (OutputMode)
                {
                    case OutputLocationMode.Custom:
                        return CustomOutputDirectory;
                    case OutputLocationMode.SourceFolder:
                        return OutputLocation.SourceFolderDisplay;
                    default:
                        return OutputLocation.ProgramOutputDirectory;
                }
            }
        }

        public bool IsCustomOutputDirectory
        {
            get { return OutputMode == OutputLocationMode.Custom; }
        }

        public bool? SelectAllState
        {
            get { return _selectAllState; }
            set
            {
                if (SetProperty(ref _selectAllState, value))
                {
                    if (value.HasValue)
                    {
                        foreach (var item in Items)
                        {
                            item.IsChecked = value.Value;
                        }
                    }

                    UpdateCounters();
                }
            }
        }

        public int AddedCount
        {
            get { return _addedCount; }
            private set { SetProperty(ref _addedCount, value); }
        }

        public int SelectedCount
        {
            get { return _selectedCount; }
            private set
            {
                if (SetProperty(ref _selectedCount, value))
                {
                    OnPropertyChanged(nameof(StartButtonText));
                }
            }
        }

        public string StartButtonText
        {
            get { return string.Format("开始转换({0})", SelectedCount); }
        }

        public bool IsEmpty
        {
            get { return Items.Count == 0; }
        }

        public string SummaryText
        {
            get { return string.Format("已添加 {0} 个文件，已选择 {1} 个文件", AddedCount, SelectedCount); }
        }

        public string SelectedFormatName { get; private set; }

        public string SelectedFormatSummary { get; private set; }

        public bool IsTaskRunning
        {
            get { return _isRunning; }
        }

        public bool HasActiveTask
        {
            get { return _activeTask != null; }
        }

        public string ActiveTaskName
        {
            get { return _activeTask?.FileName ?? "—"; }
        }

        public string ActiveStatusText
        {
            get { return _activeTask?.StatusText ?? "—"; }
        }

        public double ActiveProgress
        {
            get { return _activeTask?.Progress ?? 0; }
        }

        public string ActiveSpeedText
        {
            get { return _activeTask?.SpeedText ?? "—"; }
        }

        public void AddPaths(IEnumerable<string> paths)
        {
            if (paths == null)
            {
                return;
            }

            foreach (var raw in paths)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                string path = raw.Trim().Trim('"');
                string ext = Path.GetExtension(path) ?? string.Empty;
                if (!AllowedExtensions.Contains(ext.ToLowerInvariant()))
                {
                    continue;
                }

                if (Items.Any(i => string.Equals(i.FilePath, path, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (!File.Exists(path))
                {
                    continue;
                }

                var item = new FileItemViewModel(path);
                item.PropertyChanged += OnItemPropertyChanged;
                Items.Add(item);
                ProbeDurationAsync(item);
            }
        }

        private async void ProbeDurationAsync(FileItemViewModel item)
        {
            try
            {
                if (!_services.Ffmpeg.IsAvailable(out _))
                {
                    return;
                }

                var media = await _services.ProbeRunner.ProbeAsync(
                        item.FilePath,
                        _services.Ffmpeg.FfprobePath,
                        CancellationToken.None)
                    .ConfigureAwait(true);

                item.SetDuration(media.DurationSeconds);
            }
            catch
            {
                // 探测失败时保持“—”即可，不影响添加文件
            }
        }

        private void AddFiles()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择音频或视频文件",
                Multiselect = true,
                Filter = "支持的媒体文件 (*.mp3;*.wav;*.flac;*.mp4;*.mkv)|*.mp3;*.wav;*.flac;*.mp4;*.mkv|所有文件 (*.*)|*.*"
            };

            if (dialog.ShowDialog(Application.Current.MainWindow) == true)
            {
                AddPaths(dialog.FileNames);
            }
        }

        private void BrowseOutputDirectory()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "选择固定输出文件夹";
                dialog.ShowNewFolderButton = true;
                if (!string.IsNullOrWhiteSpace(CustomOutputDirectory) && Directory.Exists(CustomOutputDirectory))
                {
                    dialog.SelectedPath = CustomOutputDirectory;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ApplyOutputLocation(OutputLocationMode.Custom, dialog.SelectedPath);
                }
            }
        }

        /// <summary>输出位置改动立即写入本机设置（%APPDATA%\AudioConverter\settings.json）。</summary>
        private void ApplyOutputLocation(OutputLocationMode mode, string customDirectory)
        {
            _outputMode = mode;
            _customOutputDirectory = customDirectory ?? string.Empty;
            RaiseOutputLocationState();

            var settings = _services.Settings.Current;
            settings.AudioOutputMode = mode;
            settings.OutputDirectory = _customOutputDirectory;
            _services.Settings.Save();
        }

        /// <summary>设置页可能改过输出位置，回到本页时同步一次。</summary>
        public override void OnActivated()
        {
            var settings = _services.Settings.Current;
            _outputMode = settings.AudioOutputMode;
            _customOutputDirectory = settings.OutputDirectory ?? string.Empty;
            RaiseOutputLocationState();
        }

        private void RaiseOutputLocationState()
        {
            OnPropertyChanged(nameof(OutputMode));
            OnPropertyChanged(nameof(CustomOutputDirectory));
            OnPropertyChanged(nameof(OutputDirectoryDisplay));
            OnPropertyChanged(nameof(IsCustomOutputDirectory));
        }

        private async Task StartConversionAsync()
        {
            if (SelectedCount == 0)
            {
                ToastService.Instance.Show("请至少选择一个文件", ToastKind.Warning);
                return;
            }

            if (_isRunning)
            {
                return;
            }

            if (!_services.Ffmpeg.IsAvailable(out string missing))
            {
                ToastService.Instance.Show(missing, ToastKind.Error);
                return;
            }

            var selected = Items.Where(i => i.IsChecked).ToList();
            var tasks = selected
                .Select(item => new ConversionTask(
                    item.FilePath,
                    SelectedFormatOption.Value,
                    OutputLocation.ResolveDirectory(item.FilePath, OutputMode, CustomOutputDirectory),
                    _services.Settings.Current.ConflictPolicy,
                    _services.Settings.Current.AutoRetry,
                    _services.Settings.Current.RetryLimit))
                .ToList();

            foreach (var item in selected)
            {
                item.Status = ConversionStatus.Waiting;
                item.Progress = 0;
                item.SpeedText = null;
                item.ErrorMessage = null;
            }

            _cancellation = new CancellationTokenSource();
            _isRunning = true;
            RaiseRunningState();

            var progress = new Progress<ConversionTaskUpdate>(update => OnTaskUpdate(update, selected));
            IReadOnlyList<ConversionTaskResult> results = null;

            try
            {
                results = await _services.TaskManager.RunAsync(tasks, progress, _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // 已通过更新事件把运行项置为“已取消”
            }
            catch (Exception ex)
            {
                ToastService.Instance.Show("任务队列发生错误：" + ex.Message, ToastKind.Error);
            }
            finally
            {
                _cancellation?.Dispose();
                _cancellation = null;
                _isRunning = false;
                _activeTask = null;
                RaiseRunningState();
            }

            if (results != null)
            {
                WriteHistory(results, tasks);
                int done = results.Count(r => r.Outcome == ConversionOutcome.Completed);
                int failed = results.Count(r => r.Outcome == ConversionOutcome.Failed);
                int cancelled = results.Count(r =>
                    r.Outcome == ConversionOutcome.Cancelled || r.Outcome == ConversionOutcome.Skipped);

                if (cancelled > 0 && done == 0 && failed == 0)
                {
                    ToastService.Instance.Show("转换已取消/跳过：" + Summary(done, failed, cancelled), ToastKind.Warning);
                }
                else if (failed > 0)
                {
                    ToastService.Instance.Show(Summary(done, failed, cancelled), ToastKind.Error);
                }
                else
                {
                    var successful = results.FirstOrDefault(r =>
                        r.Outcome == ConversionOutcome.Completed && File.Exists(r.OutputPath));
                    if (successful == null)
                    {
                        ToastService.Instance.Show("转换完成：" + Summary(done, failed, cancelled), ToastKind.Success);
                    }
                    else
                    {
                        string target = done == 1
                            ? successful.OutputPath
                            : Path.GetDirectoryName(successful.OutputPath);
                        ToastService.Instance.ShowWithAction(
                            "转换完成：" + Summary(done, failed, cancelled),
                            ToastKind.Success,
                            done == 1 ? "打开文件所在位置" : "打开输出目录",
                            () => ExplorerHelper.OpenInExplorer(target));
                    }
                }
            }
        }

        private void CancelConversion()
        {
            try
            {
                _cancellation?.Cancel();
            }
            catch
            {
                // ignore
            }
        }

        private void OnTaskUpdate(ConversionTaskUpdate update, List<FileItemViewModel> selected)
        {
            if (update.Index < 0 || update.Index >= selected.Count)
            {
                return;
            }

            var item = selected[update.Index];
            switch (update.Kind)
            {
                case ConversionUpdateKind.TaskRunning:
                    item.Status = ConversionStatus.Running;
                    item.Progress = update.Progress;
                    item.SpeedText = null;
                    _activeTask = item;
                    RaiseActiveState();
                    break;
                case ConversionUpdateKind.TaskProgress:
                    item.Progress = update.Progress;
                    if (!string.IsNullOrEmpty(update.SpeedText))
                    {
                        item.SpeedText = update.SpeedText;
                    }

                    RaiseActiveState();
                    break;
                case ConversionUpdateKind.TaskCompleted:
                    item.Status = ConversionStatus.Completed;
                    item.Progress = 100;
                    ClearActiveIfSame(item);
                    break;
                case ConversionUpdateKind.TaskFailed:
                    item.Status = ConversionStatus.Failed;
                    item.Progress = 0;
                    item.ErrorMessage = update.Message;
                    ClearActiveIfSame(item);
                    break;
                case ConversionUpdateKind.TaskCancelled:
                    item.Status = ConversionStatus.Cancelled;
                    ClearActiveIfSame(item);
                    break;
                case ConversionUpdateKind.TaskSkipped:
                    item.Status = ConversionStatus.Skipped;
                    item.Progress = 0;
                    ClearActiveIfSame(item);
                    break;
            }
        }

        private void WriteHistory(IReadOnlyList<ConversionTaskResult> results, List<ConversionTask> tasks)
        {
            var entries = new List<HistoryEntry>();
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                var task = tasks[i];
                var entry = new HistoryEntry
                {
                    FileName = Path.GetFileName(result.FilePath),
                    InputFormat = Path.GetExtension(result.FilePath).TrimStart('.').ToUpperInvariant(),
                    OutputFormat = task.OutputFormat.ToString().ToUpperInvariant(),
                    OutputPath = result.OutputPath,
                    SizeBytes = result.OutputSizeBytes,
                    DurationSeconds = Math.Round(result.ElapsedSeconds, 1),
                    Timestamp = DateTime.Now,
                    ErrorCode = result.ErrorCode,
                    ErrorMessage = result.ErrorMessage
                };

                switch (result.Outcome)
                {
                    case ConversionOutcome.Completed:
                        entry.Result = HistoryResult.Success;
                        break;
                    case ConversionOutcome.Cancelled:
                    case ConversionOutcome.Skipped:
                        entry.Result = HistoryResult.Cancelled;
                        break;
                    default:
                        entry.Result = HistoryResult.Failed;
                        break;
                }

                entries.Add(entry);
            }

            if (entries.Count > 0)
            {
                _services.History.AddRange(entries);
            }
        }

        private static string Summary(int done, int failed, int cancelled)
        {
            return string.Format("成功 {0}，失败 {1}，取消/跳过 {2}", done, failed, cancelled);
        }

        private void ClearActiveIfSame(FileItemViewModel item)
        {
            if (ReferenceEquals(_activeTask, item))
            {
                _activeTask = null;
                RaiseActiveState();
            }
        }

        private void RaiseRunningState()
        {
            OnPropertyChanged(nameof(IsTaskRunning));
            RaiseActiveState();
            CommandManager.InvalidateRequerySuggested();
        }

        private void RaiseActiveState()
        {
            OnPropertyChanged(nameof(HasActiveTask));
            OnPropertyChanged(nameof(ActiveTaskName));
            OnPropertyChanged(nameof(ActiveStatusText));
            OnPropertyChanged(nameof(ActiveProgress));
            OnPropertyChanged(nameof(ActiveSpeedText));
        }

        private void RemoveFile(FileItemViewModel item)
        {
            if (item == null || _isRunning)
            {
                return;
            }

            item.PropertyChanged -= OnItemPropertyChanged;
            Items.Remove(item);
        }

        private bool CanRemove(FileItemViewModel item)
        {
            return item != null && !_isRunning;
        }

        private void ClearAll()
        {
            var removable = Items.ToList();
            foreach (var item in removable)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
                Items.Remove(item);
            }
        }

        private bool CanClearAll()
        {
            return !_isRunning;
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileItemViewModel.IsChecked))
            {
                UpdateCounters();
            }
        }

        private void UpdateCounters()
        {
            AddedCount = Items.Count;
            SelectedCount = Items.Count(i => i.IsChecked);
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(IsEmpty));

            if (SelectedCount == 0)
            {
                _selectAllState = false;
                OnPropertyChanged(nameof(SelectAllState));
            }
            else if (SelectedCount == AddedCount)
            {
                _selectAllState = true;
                OnPropertyChanged(nameof(SelectAllState));
            }
            else
            {
                _selectAllState = null;
                OnPropertyChanged(nameof(SelectAllState));
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateSelectedFormatInfo()
        {
            var option = SelectedFormatOption ?? OutputFormatOptions[0];
            SelectedFormatName = option.Name;
            SelectedFormatSummary = option.Summary;
            OnPropertyChanged(nameof(SelectedFormatName));
            OnPropertyChanged(nameof(SelectedFormatSummary));
        }
    }
}
