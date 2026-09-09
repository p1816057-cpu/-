using System;
using System.IO;
using AudioConverter.Common;
using AudioConverter.Models;

namespace AudioConverter.Modules.ImageCompression
{
    public sealed class ImageItemViewModel : ObservableObject
    {
        private bool _isChecked;
        private ConversionStatus _status;
        private string _previewPath;
        private long _previewSizeBytes;
        private string _previewMessage;
        private bool _isPreviewing;
        private int _originalWidth;
        private int _originalHeight;

        public ImageItemViewModel(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            Format = (Path.GetExtension(filePath) ?? "").TrimStart('.').ToUpperInvariant();
            FileInfo info = new FileInfo(filePath);
            SizeBytes = info.Length;
            SizeText = FormatSize(info.Length);
            DimensionsText = "读取中…";
            Status = ConversionStatus.Waiting;
            PreviewMessage = "等待参数预览";
        }

        public string FilePath { get; }

        public string FileName { get; }

        public string Format { get; }

        public long SizeBytes { get; }

        public string SizeText { get; }

        public string DimensionsText { get; private set; }

        public int OriginalWidth
        {
            get { return _originalWidth; }
        }

        public int OriginalHeight
        {
            get { return _originalHeight; }
        }

        public bool IsChecked
        {
            get { return _isChecked; }
            set { SetProperty(ref _isChecked, value); }
        }

        public ConversionStatus Status
        {
            get { return _status; }
            set
            {
                if (SetProperty(ref _status, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case ConversionStatus.Running:
                        return "压缩中";
                    case ConversionStatus.Completed:
                        return "已完成";
                    case ConversionStatus.Failed:
                        return "失败";
                    case ConversionStatus.Cancelled:
                        return "已取消";
                    case ConversionStatus.Skipped:
                        return "已跳过";
                    default:
                        return "待压缩";
                }
            }
        }

        public string PreviewPath
        {
            get { return _previewPath; }
            private set
            {
                if (SetProperty(ref _previewPath, value))
                {
                    OnPropertyChanged(nameof(HasPreview));
                    OnPropertyChanged(nameof(CompareResultText));
                }
            }
        }

        public bool HasPreview
        {
            get { return !string.IsNullOrEmpty(PreviewPath) && File.Exists(PreviewPath); }
        }

        public long PreviewSizeBytes
        {
            get { return _previewSizeBytes; }
            private set
            {
                if (SetProperty(ref _previewSizeBytes, value))
                {
                    OnPropertyChanged(nameof(CompareResultText));
                }
            }
        }

        public bool IsPreviewing
        {
            get { return _isPreviewing; }
            private set { SetProperty(ref _isPreviewing, value); }
        }

        public string PreviewMessage
        {
            get { return _previewMessage; }
            private set { SetProperty(ref _previewMessage, value); }
        }

        public string CompareResultText
        {
            get
            {
                if (!HasPreview || PreviewSizeBytes <= 0 || SizeBytes <= 0)
                {
                    return null;
                }

                double percent = (1.0 - PreviewSizeBytes / (double)SizeBytes) * 100;
                return string.Format("约 {0}（节省 {1:0.#}%）",
                    FormatSize(PreviewSizeBytes),
                    Math.Max(0, percent));
            }
        }

        public void SetDimensions(int width, int height)
        {
            _originalWidth = width;
            _originalHeight = height;
            if (width > 0 && height > 0)
            {
                DimensionsText = width + " × " + height;
            }
            else
            {
                DimensionsText = "—";
            }

            OnPropertyChanged(nameof(DimensionsText));
        }

        public void SetPreview(string path, long sizeBytes)
        {
            PreviewPath = path;
            PreviewSizeBytes = sizeBytes;
            IsPreviewing = false;
            PreviewMessage = "预览已生成";
        }

        public void SetPreviewState(bool previewing, string message)
        {
            IsPreviewing = previewing;
            PreviewMessage = message;
        }

        public void ClearPreview()
        {
            PreviewPath = null;
            PreviewSizeBytes = 0;
            PreviewMessage = "等待参数预览";
        }

        private static string FormatSize(long bytes)
        {
            double v = bytes;
            string[] units = { "B", "KB", "MB", "GB" };
            int unit = 0;
            while (v >= 1024 && unit < units.Length - 1)
            {
                v /= 1024;
                unit++;
            }

            return (unit == 0 ? v.ToString("0") : v.ToString("0.0")) + units[unit];
        }
    }
}
