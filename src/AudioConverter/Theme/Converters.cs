using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AudioConverter.Models;

namespace AudioConverter.Theme
{
    public sealed class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value is ConversionStatus status ? StatusResourceKey(status) : "Brush.SurfaceAlt";
            return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static string StatusResourceKey(ConversionStatus status)
        {
            switch (status)
            {
                case ConversionStatus.Running:
                    return "Brush.AccentSoft";
                case ConversionStatus.Completed:
                    return "Brush.SuccessSoft";
                case ConversionStatus.Failed:
                    return "Brush.DangerSoft";
                case ConversionStatus.Cancelled:
                    return "Brush.WarningSoft";
                case ConversionStatus.Skipped:
                    return "Brush.WarningSoft";
                default:
                    return "Brush.SurfaceAlt";
            }
        }
    }

    public sealed class StatusToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value is ConversionStatus status ? StatusResourceKey(status) : "Brush.TextSecondary";
            return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static string StatusResourceKey(ConversionStatus status)
        {
            switch (status)
            {
                case ConversionStatus.Running:
                    return "Brush.Accent";
                case ConversionStatus.Completed:
                    return "Brush.Success";
                case ConversionStatus.Failed:
                    return "Brush.Danger";
                case ConversionStatus.Cancelled:
                    return "Brush.Warning";
                case ConversionStatus.Skipped:
                    return "Brush.Warning";
                default:
                    return "Brush.TextSecondary";
            }
        }
    }

    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : false;
        }
    }

    public sealed class HistoryResultToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Models.HistoryResult result)
            {
                switch (result)
                {
                    case Models.HistoryResult.Success:
                        return "成功";
                    case Models.HistoryResult.Cancelled:
                        return "取消";
                    default:
                        return "失败";
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class HistoryResultToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key;
            if (value is Models.HistoryResult result)
            {
                switch (result)
                {
                    case Models.HistoryResult.Success:
                        key = "Brush.SuccessSoft";
                        break;
                    case Models.HistoryResult.Cancelled:
                        key = "Brush.WarningSoft";
                        break;
                    default:
                        key = "Brush.DangerSoft";
                        break;
                }
            }
            else
            {
                key = "Brush.SurfaceAlt";
            }

            return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class HistoryResultToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key;
            if (value is Models.HistoryResult result)
            {
                switch (result)
                {
                    case Models.HistoryResult.Success:
                        key = "Brush.Success";
                        break;
                    case Models.HistoryResult.Cancelled:
                        key = "Brush.Warning";
                        break;
                    default:
                        key = "Brush.Danger";
                        break;
                }
            }
            else
            {
                key = "Brush.TextSecondary";
            }

            return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public sealed class BytesToSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is long bytes && bytes > 0 ? FormatSize(bytes) : "—";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static string FormatSize(long bytes)
        {
            double v = bytes;
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int unit = 0;
            while (v >= 1024 && unit < units.Length - 1)
            {
                v /= 1024;
                unit++;
            }

            return (unit == 0 ? v.ToString("0") : v.ToString("0.0")) + units[unit];
        }
    }

    public sealed class FileFormatToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string format = (value as string ?? "").ToUpperInvariant();
            return ResolveBrushKey(format, "Bg") ?? Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static Brush ResolveBrushKey(string format, string suffix)
        {
            string key;
            switch (format)
            {
                case "MP3":
                    key = "Brush.FileMp3" + suffix;
                    break;
                case "WAV":
                    key = "Brush.FileWav" + suffix;
                    break;
                case "FLAC":
                    key = "Brush.FileFlac" + suffix;
                    break;
                default:
                    key = "Brush.FileVideo" + suffix;
                    break;
            }

            return Application.Current?.TryFindResource(key) as Brush;
        }
    }

    public sealed class FileFormatToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key;
            string format = (value as string ?? "").ToUpperInvariant();
            switch (format)
            {
                case "MP3":
                    key = "Brush.FileMp3";
                    break;
                case "WAV":
                    key = "Brush.FileWav";
                    break;
                case "FLAC":
                    key = "Brush.FileFlac";
                    break;
                default:
                    key = "Brush.FileVideo";
                    break;
            }

            return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
