using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AudioConverter.Models;

namespace AudioConverter.Core
{
    public sealed class ImageRunResult
    {
        public bool Success { get; set; }

        public string ErrorText { get; set; }
    }

    /// <summary>
    /// 图片压缩核心：支持 WebP / JPG / PNG，可等比缩放，全部本地运行。
    /// </summary>
    public sealed class ImageFfmpegRunner
    {
        public static string Extension(ImageOutputFormat format)
        {
            switch (format)
            {
                case ImageOutputFormat.Jpg:
                    return ".jpg";
                case ImageOutputFormat.Png:
                    return ".png";
                default:
                    return ".webp";
            }
        }

        public async Task<ImageRunResult> ConvertAsync(
            string ffmpegPath,
            string inputPath,
            string outputPath,
            ImageOutputFormat format,
            int quality,
            int scalePercent,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                throw new InvalidOperationException("未找到 ffmpeg.exe。");
            }

            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = BuildArguments(inputPath, outputPath, format, quality, scalePercent),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                var errorTask = process.StandardError.ReadToEndAsync();
                using (cancellationToken.Register(() => TryKill(process)))
                {
                    if (!process.WaitForExit(30000))
                    {
                        TryKill(process);
                        throw new OperationCanceledException(cancellationToken);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    string error = await errorTask.ConfigureAwait(false);
                    return new ImageRunResult
                    {
                        Success = process.ExitCode == 0,
                        ErrorText = process.ExitCode == 0 ? null : TrimError(error, process.ExitCode)
                    };
                }
            }
        }

        private static string BuildArguments(
            string inputPath,
            string outputPath,
            ImageOutputFormat format,
            int quality,
            int scalePercent)
        {
            var args = new System.Text.StringBuilder();
            args.Append("-hide_banner -nostdin -y ");
            args.Append("-i ").Append(CommandLine.Quote(inputPath)).Append(' ');
            args.Append("-frames:v 1 -an ");

            if (scalePercent > 0 && scalePercent != 100)
            {
                double factor = scalePercent / 100.0;
                string scale = factor.ToString("0.###", CultureInfo.InvariantCulture);
                args.Append("-vf scale=trunc(iw*").Append(scale).Append("/2)*2:trunc(ih*")
                    .Append(scale).Append("/2)*2 ");
            }

            switch (format)
            {
                case ImageOutputFormat.Jpg:
                    int q = Math.Max(2, Math.Min(31, 2 + (int)Math.Round((100 - Math.Max(1, Math.Min(100, quality))) * 0.29)));
                    args.Append("-c:v mjpeg -q:v ").Append(q).Append(' ');
                    break;
                case ImageOutputFormat.Png:
                    args.Append("-c:v png -compression_level 9 ");
                    break;
                default:
                    args.Append("-c:v libwebp -quality ")
                        .Append(Math.Max(1, Math.Min(100, quality)))
                        .Append(" -compression_level 6 ");
                    break;
            }

            args.Append(CommandLine.Quote(outputPath));
            return args.ToString();
        }

        private static string TrimError(string error, int exitCode)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return string.Format("FFmpeg 退出，错误码 {0}。", exitCode);
            }

            string[] lines = error.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int start = Math.Max(0, lines.Length - 5);
            return string.Join(" | ", lines, start, lines.Length - start).Trim();
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
