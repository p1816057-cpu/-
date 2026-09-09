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

namespace AudioConverter.Modules.ImageCompression
{
    public sealed class ImageCompressionViewModel : ViewModelBase
    {
        private static readonly string[] ImageExtensions =
            { ".jpg", ".jpeg", ".png", ".webp", ".bmp" };

        private readonly AppServices _services;
        private CancellationTokenSource _previewCancellation;
        private CancellationTokenSource _conversionCancellation;
        private bool _isRunning;
        private string _outputDirectory;
        private bool? _selectAllState;
        private int _addedCount;
        private int _selectedCount;
        private ImageFormatOption _selectedFormatOption;
        private int _quality = 75;
        private int _scalePercent = 100;
        private ImageItemViewModel _activeTask;

        public ImageCompressionViewModel(AppServices services)
        {
            _services = services;
            Title = "图片压缩";
            Items = new ObservableCollection<ImageItemViewModel>();
            ImageFormatOptions = new List<ImageFormatOption>
            {
                new ImageFormatOption(ImageOutputFormat.Webp, "WebP", "有损压缩，照片首选"),
                new ImageFormatOption(ImageOutputFormat.Jpg, "JPG", "通用兼容，画质可调"),
                new ImageFormatOption(ImageOutputFormat.Png, "PNG", "无损，尺寸缩小有限")
            };
            _selectedFormatOption = ImageFormatOptions[0];
            _outputDirectory = _services.Settings.Current.OutputDirectory;
            ScaleOptions = new[] { 100, 80, 60, 50 };

            AddFilesCommand = new RelayCommand(_ => AddFiles());
            BrowseOutputCommand = new RelayCommand(_ => BrowseOutputDirectory());
            StartCommand = new AsyncRelayCommand(_ => StartCompressionAsync());
            CancelCommand = new RelayCommand(_ => CancelCompression(), _ => _isRunning);
            RemoveFileCommand = new RelayCommand(p => RemoveItem(p as ImageItemViewModel), p => CanRemove(p as ImageItemViewModel));
            ClearAllCommand = new RelayCommand(_ => ClearAll(), _ => CanClearAll());

            Items.CollectionChanged += (s, e) => UpdateCounters();
            UpdateCounters();
        }

        public ObservableCollection<ImageItemViewModel> Items { get; }

        public IReadOnlyList<ImageFormatOption> ImageFormatOptions { get; }

        public IReadOnlyList<int> ScaleOptions { get; }

        public ICommand AddFilesCommand { get; }

        public ICommand BrowseOutputCommand { get; }

        public ICommand StartCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand RemoveFileCommand { get; }

        public ICommand ClearAllCommand { get; }

        public ImageFormatOption SelectedFormatOption
        {
            get { return _selectedFormatOption; }
            set
            {
                if (SetProperty(ref _selectedFormatOption, value) && value != null)
                {
                    SchedulePreview();
                }
            }
        }

        public int Quality
        {
            get { return _quality; }
            set
            {
                int val = Math.Max(1, Math.Min(100, value));
                if (SetProperty(ref _quality, val))
                {
                    OnPropertyChanged(nameof(Quality));
                    SchedulePreview();
                }
            }
        }

        public int ScalePercent
        {
            get { return _scalePercent; }
            set
            {
                if (SetProperty(ref _scalePercent, value))
                {
                    SchedulePreview();
                }
            }
        }

        public string OutputDirectory
        {
            get { return _outputDirectory; }
            set { SetProperty(ref _outputDirectory, value); }
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
            get { return string.Format("开始压缩({0})", SelectedCount); }
        }

        public bool IsEmpty
        {
            get { return Items.Count == 0; }
        }

        public string SummaryText
        {
            get { return string.Format("已添加 {0} 张图片，已选择 {1} 张", AddedCount, SelectedCount); }
        }

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

        public string SelectedFormatName
        {
            get { return SelectedFormatOption?.Name ?? "WebP"; }
        }

        public string SelectedFormatSummary
        {
            get { return SelectedFormatOption?.Summary ?? ""; }
        }

        public void AddPaths(IEnumerable<string> paths)
        {
            if (paths == null)
            {
                return;
            }

            bool added = false;
            foreach (var raw in paths)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                string path = raw.Trim().Trim('"');
                string ext = Path.GetExtension(path) ?? "";
                if (!ImageExtensions.Contains(ext.ToLowerInvariant()) || !File.Exists(path))
                {
                    continue;
                }

                if (Items.Any(i => string.Equals(i.FilePath, path, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var item = new ImageItemViewModel(path);
                item.PropertyChanged += OnItemPropertyChanged;
                Items.Add(item);
                ProbeDimensionsAsync(item);
                added = true;
            }

            if (added)
            {
                SchedulePreview();
            }
        }

        private async void ProbeDimensionsAsync(ImageItemViewModel item)
        {
            try
            {
                if (_services.Ffmpeg.IsAvailable(out _))
                {
                    var media = await _services.ProbeRunner.ProbeAsync(
                        item.FilePath,
                        _services.Ffmpeg.FfprobePath,
                        CancellationToken.None);
                    item.SetDimensions(media.Width, media.Height);
                }
            }
            catch
            {
                item.SetDimensions(0, 0);
            }
        }

        private void SchedulePreview()
        {
            if (Items.Count == 0)
            {
                return;
            }

            var token = RestartPreviewCancellation();
            _ = GeneratePreviewsAsync(token);
        }

        private CancellationToken RestartPreviewCancellation()
        {
            _previewCancellation?.Cancel();
            _previewCancellation?.Dispose();
            _previewCancellation = new CancellationTokenSource();
            return _previewCancellation.Token;
        }

        private async Task GeneratePreviewsAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(500, token);
                foreach (var item in Items.ToList())
                {
                    token.ThrowIfCancellationRequested();
                    await GeneratePreviewAsync(item, token);
                }
            }
            catch (OperationCanceledException)
            {
                // 参数快速变化时取消上一轮预览
            }
        }

        private async Task GeneratePreviewAsync(ImageItemViewModel item, CancellationToken token)
        {
            item.SetPreviewState(true, "正在生成压缩预览…");
            string dir = Path.Combine(_services.Settings.Current.TempDirectory, "ImagePreview");
            Directory.CreateDirectory(dir);
            string preview = Path.Combine(dir, Guid.NewGuid().ToString("N") + ImageFfmpegRunner.Extension(SelectedFormatOption.Value));

            try
            {
                var result = await _services.ImageRunner.ConvertAsync(
                    _services.Ffmpeg.FfmpegPath,
                    item.FilePath,
                    preview,
                    SelectedFormatOption.Value,
                    Quality,
                    ScalePercent,
                    token);

                if (result.Success && File.Exists(preview))
                {
                    item.SetPreview(preview, new FileInfo(preview).Length);
                }
                else
                {
                    item.SetPreviewState(false, "预览失败");
                    TryDelete(preview);
                }
            }
            catch (Exception)
            {
                item.SetPreviewState(false, "预览失败");
                TryDelete(preview);
            }
        }

        private void AddFiles()
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择图片文件",
                Multiselect = true,
                Filter = "图片文件 (*.jpg;*.jpeg;*.png;*.webp;*.bmp)|*.jpg;*.jpeg;*.png;*.webp;*.bmp|所有文件 (*.*)|*.*"
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
                dialog.Description = "选择图片输出目录";
                dialog.ShowNewFolderButton = true;
                if (Directory.Exists(OutputDirectory))
                {
                    dialog.SelectedPath = OutputDirectory;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OutputDirectory = dialog.SelectedPath;
                }
            }
        }

        private async Task StartCompressionAsync()
        {
            if (SelectedCount == 0)
            {
                ToastService.Instance.Show("请至少选择一张图片", ToastKind.Warning);
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

            try
            {
                Directory.CreateDirectory(OutputDirectory);
            }
            catch (Exception ex)
            {
                ToastService.Instance.Show("输出目录不可用：" + ex.Message, ToastKind.Error);
                return;
            }

            var selected = Items.Where(i => i.IsChecked).ToList();
            _conversionCancellation = new CancellationTokenSource();
            _isRunning = true;
            RaiseRunningState();
            var history = new List<HistoryEntry>();
            int done = 0;
            int failed = 0;
            int skipped = 0;
            int cancelled = 0;

            foreach (var item in selected)
            {
                if (_conversionCancellation.IsCancellationRequested)
                {
                    item.Status = ConversionStatus.Cancelled;
                    cancelled++;
                    continue;
                }

                item.Status = ConversionStatus.Running;
                _activeTask = item;
                RaiseRunningState();

                string ext = ImageFfmpegRunner.Extension(SelectedFormatOption.Value);
                var resolution = ResolveOutput(item.FilePath, ext);
                if (resolution.Skip)
                {
                    item.Status = ConversionStatus.Skipped;
                    skipped++;
                    _activeTask = null;
                    RaiseRunningState();
                    continue;
                }

                try
                {
                    var result = await _services.ImageRunner.ConvertAsync(
                        _services.Ffmpeg.FfmpegPath,
                        item.FilePath,
                        resolution.Path,
                        SelectedFormatOption.Value,
                        Quality,
                        ScalePercent,
                        _conversionCancellation.Token);

                    if (result.Success && File.Exists(resolution.Path))
                    {
                        item.Status = ConversionStatus.Completed;
                        done++;
                        history.Add(new HistoryEntry
                        {
                            FileName = item.FileName,
                            InputFormat = item.Format,
                            OutputFormat = SelectedFormatOption.Value.ToString().ToUpperInvariant(),
                            OutputPath = resolution.Path,
                            SizeBytes = new FileInfo(resolution.Path).Length,
                            DurationSeconds = 0,
                            Result = HistoryResult.Success,
                            Timestamp = DateTime.Now
                        });
                    }
                    else
                    {
                        item.Status = ConversionStatus.Failed;
                        failed++;
                    }
                }
                catch (OperationCanceledException)
                {
                    item.Status = ConversionStatus.Cancelled;
                    cancelled++;
                }
                catch
                {
                    item.Status = ConversionStatus.Failed;
                    failed++;
                }
                finally
                {
                    _activeTask = null;
                    RaiseRunningState();
                }
            }

            if (history.Count > 0)
            {
                _services.History.AddRange(history);
            }

            _conversionCancellation?.Dispose();
            _conversionCancellation = null;
            _isRunning = false;
            _activeTask = null;
            RaiseRunningState();

            string message = string.Format("压缩完成：成功 {0}，失败 {1}，跳过 {2}，取消 {3}", done, failed, skipped, cancelled);
            var successOutput = history.FirstOrDefault(h => h.OutputPath != null && File.Exists(h.OutputPath))?.OutputPath;
            if (successOutput != null)
            {
                ToastService.Instance.ShowWithAction(
                    message,
                    ToastKind.Success,
                    done == 1 ? "打开文件所在位置" : "打开输出目录",
                    () => ExplorerHelper.OpenInExplorer(done == 1 ? successOutput : Path.GetDirectoryName(successOutput)));
            }
            else
            {
                ToastService.Instance.Show(message, failed > 0 ? ToastKind.Error : ToastKind.Info);
            }
        }

        private (string Path, bool Skip) ResolveOutput(string inputPath, string extension)
        {
            string directory = string.IsNullOrWhiteSpace(OutputDirectory)
                ? Path.GetDirectoryName(inputPath)
                : OutputDirectory;
            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string candidate = Path.Combine(directory, baseName + extension);

            bool sameInput = string.Equals(
                Path.GetFullPath(candidate),
                Path.GetFullPath(inputPath),
                StringComparison.OrdinalIgnoreCase);

            if (sameInput || File.Exists(candidate))
            {
                switch (_services.Settings.Current.ConflictPolicy)
                {
                    case ConflictPolicy.Overwrite when !sameInput:
                        return (candidate, false);
                    case ConflictPolicy.Skip:
                        return (candidate, true);
                    default:
                        for (int i = 1; i < 10000; i++)
                        {
                            string name = string.Format("{0} ({1}){2}", baseName, i, extension);
                            string next = Path.Combine(directory, name);
                            if (!File.Exists(next) && !string.Equals(Path.GetFullPath(next), Path.GetFullPath(inputPath), StringComparison.OrdinalIgnoreCase))
                            {
                                return (next, false);
                            }
                        }

                        return (Path.Combine(directory, baseName + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + extension), false);
                }
            }

            return (candidate, false);
        }

        private void CancelCompression()
        {
            try
            {
                _conversionCancellation?.Cancel();
            }
            catch
            {
                // ignore
            }
        }

        private void RemoveItem(ImageItemViewModel item)
        {
            if (item == null || _isRunning)
            {
                return;
            }

            item.PropertyChanged -= OnItemPropertyChanged;
            Items.Remove(item);
        }

        private bool CanRemove(ImageItemViewModel item)
        {
            return item != null && !_isRunning;
        }

        private void ClearAll()
        {
            if (_isRunning)
            {
                return;
            }

            var copy = Items.ToList();
            foreach (var item in copy)
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
            if (e.PropertyName == nameof(ImageItemViewModel.IsChecked))
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

            OnPropertyChanged(nameof(SelectedFormatName));
            OnPropertyChanged(nameof(SelectedFormatSummary));
            CommandManager.InvalidateRequerySuggested();
        }

        private void RaiseRunningState()
        {
            OnPropertyChanged(nameof(IsTaskRunning));
            OnPropertyChanged(nameof(HasActiveTask));
            OnPropertyChanged(nameof(ActiveTaskName));
            OnPropertyChanged(nameof(ActiveStatusText));
            CommandManager.InvalidateRequerySuggested();
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
