using System;
using System.IO;
using AudioConverter.Common;
using AudioConverter.Models;

namespace AudioConverter.Modules.Conversion
{
    public sealed class FileItemViewModel : ObservableObject
    {
        private bool _isChecked;
        private ConversionStatus _status;
        private double _progress;
        private string _speedText;
        private string _errorMessage;

        public FileItemViewModel(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            string ext = (Path.GetExtension(filePath) ?? string.Empty).ToLowerInvariant();
            Format = ext.TrimStart('.').ToUpperInvariant();
            Kind = ext == ".mp4" || ext == ".mkv" ? FileKind.Video : FileKind.Audio;
            IsVideo = Kind == FileKind.Video;

            try
            {
                var info = new FileInfo(filePath);
                SizeBytes = info.Length;
                SizeText = FormatSize(info.Length);
            }
            catch
            {
                SizeText = "—";
            }

            DurationText = "—";
            Status = ConversionStatus.Waiting;
        }

        public string FilePath { get; }

        public string FileName { get; }

        public string Format { get; }

        public bool IsVideo { get; }

        public FileKind Kind { get; }

        public long SizeBytes { get; }

        public string SizeText { get; }

        public string DurationText { get; private set; }

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
                        return "正在转换";
                    case ConversionStatus.Completed:
                        return "已完成";
                    case ConversionStatus.Failed:
                        return "转换失败";
                    case ConversionStatus.Cancelled:
                        return "已取消";
                    default:
                        return "等待转换";
                }
            }
        }

        public double Progress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
        }

        public string SpeedText
        {
            get { return _speedText; }
            set { SetProperty(ref _speedText, value); }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { SetProperty(ref _errorMessage, value); }
        }

        public void SetDuration(double seconds)
        {
            DurationText = FormatDuration(seconds);
        }

        public static string FormatSize(long bytes)
        {
            double value = bytes;
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int unit = 0;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }

            return (unit == 0 ? value.ToString("0") : value.ToString("0.0")) + units[unit];
        }

        private static string FormatDuration(double seconds)
        {
            if (seconds <= 0)
            {
                return "—";
            }

            var ts = TimeSpan.FromSeconds(seconds);
            return ts.TotalHours >= 1
                ? string.Format("{0}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds)
                : string.Format("{0}:{1:00}", ts.Minutes, ts.Seconds);
        }
    }
}
