using System;
using System.IO;
using AudioConverter.Models;

namespace AudioConverter.Core
{
    /// <summary>
    /// 统一的输出目录解析：默认在程序自身目录下新建「转换输出」子文件夹，转换结果就放在里面；
    /// 用户可改为输出到源文件所在文件夹，或显式选择固定文件夹。
    /// 程序目录不可写（例如装在 Program Files）时自动回退到源文件所在文件夹，保证转换不失败。
    /// </summary>
    public static class OutputLocation
    {
        /// <summary>自动创建的输出子文件夹名。</summary>
        public const string SourceSubfolderName = "转换输出";

        /// <summary>源文件模式下输出框显示的文案。</summary>
        public const string SourceFolderDisplay = "与源文件相同文件夹";

        private static bool? _programFolderWritable;

        /// <summary>程序自身所在目录，即 SkyFusion.exe 所在的文件夹。</summary>
        public static string ProgramDirectory
        {
            get { return AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/'); }
        }

        /// <summary>程序目录下的「转换输出」文件夹。</summary>
        public static string ProgramOutputDirectory
        {
            get { return Path.Combine(ProgramDirectory, SourceSubfolderName); }
        }

        public static string ResolveDirectory(string inputPath, OutputLocationMode mode, string customDirectory)
        {
            switch (mode)
            {
                case OutputLocationMode.Custom:
                    string custom = (customDirectory ?? string.Empty).Trim();
                    if (custom.Length > 0 && IsUsable(custom))
                    {
                        return custom;
                    }

                    break;
                case OutputLocationMode.SourceFolder:
                    return Path.Combine(GetSourceDirectory(inputPath), SourceSubfolderName);
            }

            return IsProgramFolderWritable()
                ? ProgramOutputDirectory
                : Path.Combine(GetSourceDirectory(inputPath), SourceSubfolderName);
        }

        /// <summary>
        /// 确保程序目录下的「转换输出」文件夹存在，返回是否可写。
        /// 首次调用时用真实写文件探测（Program Files 等只读目录会被判为不可写），结果在本进程内缓存。
        /// </summary>
        public static bool IsProgramFolderWritable()
        {
            if (_programFolderWritable.HasValue)
            {
                return _programFolderWritable.Value;
            }

            try
            {
                string directory = ProgramOutputDirectory;
                Directory.CreateDirectory(directory);
                string probe = Path.Combine(directory, ".write-test-" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllBytes(probe, new byte[0]);
                File.Delete(probe);
                _programFolderWritable = true;
            }
            catch
            {
                _programFolderWritable = false;
            }

            return _programFolderWritable.Value;
        }

        /// <summary>启动时先建好程序目录下的「转换输出」文件夹，不可写时返回 null。</summary>
        public static string EnsureProgramOutputDirectory()
        {
            return IsProgramFolderWritable() ? ProgramOutputDirectory : null;
        }

        public static string GetSourceDirectory(string inputPath)
        {
            string directory = string.IsNullOrWhiteSpace(inputPath) ? null : Path.GetDirectoryName(inputPath);
            return string.IsNullOrWhiteSpace(directory) ? Directory.GetCurrentDirectory() : directory;
        }

        /// <summary>
        /// 判断自定义文件夹在本机是否可用：盘符/根目录存在即视为可用。
        /// 文件夹本身允许尚不存在（转换前会自动创建），但盘符不存在的路径一定是它机遗留路径。
        /// </summary>
        public static bool IsUsable(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }

            try
            {
                string root = Path.GetPathRoot(Path.GetFullPath(directory.Trim()));
                return !string.IsNullOrWhiteSpace(root) && Directory.Exists(root);
            }
            catch
            {
                return false;
            }
        }
    }
}
