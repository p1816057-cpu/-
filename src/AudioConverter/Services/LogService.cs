using System;
using System.IO;
using System.Text;

namespace AudioConverter.Services
{
    public static class LogService
    {
        private static readonly object Sync = new object();

        public static string LogFilePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioConverter", "logs", "app.log"); }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warn(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message, Exception exception = null)
        {
            Write("ERROR", message + (exception == null ? "" : Environment.NewLine + exception));
        }

        public static void Write(string level, string message)
        {
            try
            {
                lock (Sync)
                {
                    string path = LogFilePath;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    string line = string.Format("{0:yyyy-MM-dd HH:mm:ss} [{1}] {2}{3}",
                        DateTime.Now, level, message, Environment.NewLine);
                    File.AppendAllText(path, line, Encoding.UTF8);
                }
            }
            catch
            {
                // 日志写入失败不影响程序运行
            }
        }
    }
}
