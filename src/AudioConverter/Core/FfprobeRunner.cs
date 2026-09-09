using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AudioConverter.Core
{
    public sealed class FfprobeRunner
    {
        public async Task<MediaInfo> ProbeAsync(
            string filePath,
            string ffprobePath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ffprobePath) || !File.Exists(ffprobePath))
            {
                throw new InvalidOperationException("未找到 ffprobe.exe。");
            }

            var psi = new ProcessStartInfo
            {
                FileName = ffprobePath,
                Arguments =
                    "-v error " +
                    "-of default=noprint_wrappers=1 " +
                    "-show_entries format=duration:format=size:format=format_name " +
                    "-show_entries stream=codec_type " +
                    CommandLine.Quote(filePath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                using (cancellationToken.Register(() => TryKill(process)))
                {
                    await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
                    if (!process.WaitForExit(8000))
                    {
                        TryKill(process);
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();

                var info = Parse(outputTask.Result);
                string error = errorTask.Result;
                if (!string.IsNullOrWhiteSpace(error) && !info.HasAudio && !info.HasVideo)
                {
                    throw new InvalidOperationException(TrimError(error));
                }

                return info;
            }
        }

        private static MediaInfo Parse(string output)
        {
            var info = new MediaInfo();
            if (string.IsNullOrEmpty(output))
            {
                return info;
            }

            using (var reader = new StringReader(output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    int eq = line.IndexOf('=');
                    if (eq < 0)
                    {
                        continue;
                    }

                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    double parsed;

                    switch (key)
                    {
                        case "duration":
                            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                            {
                                info.DurationSeconds = parsed;
                            }

                            break;
                        case "size":
                            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long size))
                            {
                                info.SizeBytes = size;
                            }

                            break;
                        case "format_name":
                            info.FormatName = value;
                            break;
                        case "codec_type":
                            if (string.Equals(value, "audio", StringComparison.OrdinalIgnoreCase))
                            {
                                info.HasAudio = true;
                            }
                            else if (string.Equals(value, "video", StringComparison.OrdinalIgnoreCase))
                            {
                                info.HasVideo = true;
                            }

                            break;
                    }
                }
            }

            return info;
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

        public static string TrimError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return "读取媒体信息失败。";
            }

            string[] lines = error.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", lines).Trim();
        }
    }
}
