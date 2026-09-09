using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AudioConverter.Models;

namespace AudioConverter.Core
{
    public sealed class ConversionProgress
    {
        public double Percent { get; set; }

        public string SpeedText { get; set; }
    }

    public sealed class FfmpegRunResult
    {
        public bool Success { get; set; }

        public string ErrorText { get; set; }
    }

    public sealed class FfmpegRunner
    {
        public async Task<FfmpegRunResult> ConvertAsync(
            string ffmpegPath,
            string inputPath,
            string outputPath,
            OutputFormat outputFormat,
            bool inputIsVideo,
            double durationSeconds,
            IProgress<ConversionProgress> progress,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
            {
                throw new InvalidOperationException("未找到 ffmpeg.exe。");
            }

            string arguments = BuildArguments(inputPath, outputPath, outputFormat, inputIsVideo);
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                using (cancellationToken.Register(() => TryKill(process)))
                {
                    var errorTask = process.StandardError.ReadToEndAsync();
                    double lastPercent = 0;

                    string line;
                    while ((line = process.StandardOutput.ReadLine()) != null)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (TryParseProgress(line, durationSeconds, ref lastPercent, out double percent, out string speed))
                        {
                            progress?.Report(new ConversionProgress { Percent = percent, SpeedText = speed });
                        }

                        if (string.Equals(line.Trim(), "progress=end", StringComparison.OrdinalIgnoreCase))
                        {
                            break;
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (!process.WaitForExit(8000))
                    {
                        TryKill(process);
                    }

                    string errorText = await errorTask.ConfigureAwait(false);
                    int exitCode = process.ExitCode;
                    bool success = exitCode == 0;

                    if (success)
                    {
                        progress?.Report(new ConversionProgress { Percent = 100, SpeedText = null });
                    }

                    return new FfmpegRunResult
                    {
                        Success = success,
                        ErrorText = success ? null : TrimError(errorText, exitCode)
                    };
                }
            }
        }

        private static string BuildArguments(
            string inputPath,
            string outputPath,
            OutputFormat outputFormat,
            bool inputIsVideo)
        {
            var args = new System.Text.StringBuilder();

            args.Append("-hide_banner -nostdin -y ");
            args.Append("-i ").Append(CommandLine.Quote(inputPath)).Append(' ');

            if (inputIsVideo)
            {
                args.Append("-map 0:a:0? -vn -sn -dn ");
            }
            else
            {
                args.Append("-map 0:a:0? ");
            }

            switch (outputFormat)
            {
                case OutputFormat.Mp3:
                    args.Append("-c:a libmp3lame -b:a 320k -write_xing 1 -id3v2_version 3 ");
                    break;
                case OutputFormat.Wav:
                    args.Append("-c:a pcm_s16le ");
                    break;
                case OutputFormat.Flac:
                    args.Append("-c:a flac -compression_level 5 ");
                    break;
            }

            args.Append("-map_metadata 0 -progress pipe:1 -nostats ");
            args.Append(CommandLine.Quote(outputPath));
            return args.ToString();
        }

        private static bool TryParseProgress(
            string line,
            double durationSeconds,
            ref double lastPercent,
            out double percent,
            out string speed)
        {
            percent = lastPercent;
            speed = null;

            int eq = line.IndexOf('=');
            if (eq < 0)
            {
                return false;
            }

            string key = line.Substring(0, eq).Trim();
            string value = line.Substring(eq + 1).Trim();
            bool changed = false;

            if (string.Equals(key, "out_time_us", StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double micro))
                {
                    double timeSeconds = micro / 1000000.0;
                    if (durationSeconds > 0)
                    {
                        percent = Math.Max(0, Math.Min(99, timeSeconds / durationSeconds * 100));
                        lastPercent = percent;
                        changed = true;
                    }
                }
            }
            else if (string.Equals(key, "speed", StringComparison.OrdinalIgnoreCase))
            {
                speed = value;
                changed = true;
            }

            return changed;
        }

        private static string TrimError(string error, int exitCode)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return string.Format("FFmpeg 退出，错误码 {0}。", exitCode);
            }

            string[] lines = error.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int start = Math.Max(0, lines.Length - 8);
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
