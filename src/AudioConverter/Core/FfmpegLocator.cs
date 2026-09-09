using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using AudioConverter.Models;

namespace AudioConverter.Core
{
    /// <summary>
    /// 查找 ffmpeg.exe / ffprobe.exe。
    /// 查找顺序：设置目录 → EXE 同级 / ffmpeg/bin → PATH。
    /// </summary>
    public sealed class FfmpegLocator
    {
        private readonly AppSettings _settings;
        private string _ffmpegPath;
        private string _ffprobePath;
        private string _ffmpegVersion;

        public FfmpegLocator(AppSettings settings)
        {
            _settings = settings;
        }

        public string FfmpegPath
        {
            get
            {
                if (_ffmpegPath == null)
                {
                    _ffmpegPath = Locate("ffmpeg.exe");
                }

                return _ffmpegPath;
            }
        }

        public string FfprobePath
        {
            get
            {
                if (_ffprobePath == null)
                {
                    _ffprobePath = Locate("ffprobe.exe");
                }

                return _ffprobePath;
            }
        }

        public bool IsAvailable(out string message)
        {
            if (string.IsNullOrEmpty(FfmpegPath))
            {
                message = "未找到 ffmpeg.exe，请在设置中指定 FFmpeg 目录，或将 ffmpeg/ffprobe 放到程序目录。";
                return false;
            }

            if (string.IsNullOrEmpty(FfprobePath))
            {
                message = "未找到 ffprobe.exe，请与 ffmpeg.exe 一同提供。";
                return false;
            }

            message = null;
            return true;
        }

        public string GetFfmpegVersion()
        {
            if (_ffmpegVersion != null)
            {
                return _ffmpegVersion;
            }

            _ffmpegVersion = ReadVersion(FfmpegPath);
            return _ffmpegVersion;
        }

        private string Locate(string executable)
        {
            var candidates = new List<string>();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            candidates.Add(Path.Combine(baseDir, "ffmpeg", "bin", executable));
            candidates.Add(Path.Combine(baseDir, "ffmpeg", executable));
            candidates.Add(Path.Combine(baseDir, "tools", "ffmpeg", "bin", executable));
            candidates.Add(Path.Combine(baseDir, executable));

            if (!string.IsNullOrWhiteSpace(_settings.FfmpegDirectory))
            {
                string custom = _settings.FfmpegDirectory.Trim().Trim('"');
                if (custom.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(Path.GetFileName(custom), executable, StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Insert(0, custom);
                }
                else
                {
                    candidates.Insert(0, Path.Combine(custom, executable));
                }
            }

            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                {
                    return candidate;
                }
            }

            string pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var dir in pathValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = Path.Combine(dir.Trim('"'), executable);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string ReadVersion(string executable)
        {
            if (string.IsNullOrEmpty(executable))
            {
                return "未检测到";
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    var match = Regex.Match(output, @"ffmpeg version\s+(\S+)", RegexOptions.IgnoreCase);
                    if (!match.Success)
                    {
                        return "FFmpeg";
                    }

                    return match.Groups[1].Value;
                }
            }
            catch
            {
                return "读取失败";
            }
        }
    }
}
