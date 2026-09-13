using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using AudioConverter.Common;
using Microsoft.Win32;

namespace AudioConverter.Services
{
    public static class LogExporter
    {
        public static void Export(AppServices services, Window owner)
        {
            try
            {
                LogService.Info("导出日志开始");

                var sb = new StringBuilder();
                sb.AppendLine("=== 天融 SkyFusion 诊断日志 ===");
                sb.AppendLine("版本: " + AppInfo.Version);
                sb.AppendLine("导出时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.AppendLine("系统: " + Environment.OSVersion.VersionString);
                sb.AppendLine("FFmpeg: " + services.Ffmpeg.GetFfmpegVersion());
                sb.AppendLine("历史记录数量: " + services.History.Entries.Count);
                sb.AppendLine();

                sb.AppendLine("--- 运行日志 ---");
                string logPath = LogService.LogFilePath;
                if (File.Exists(logPath))
                {
                    var lines = File.ReadAllLines(logPath, Encoding.UTF8);
                    foreach (var line in lines.Skip(Math.Max(0, lines.Length - 300)))
                    {
                        sb.AppendLine(line);
                    }
                }
                else
                {
                    sb.AppendLine("（暂无运行日志）");
                }

                sb.AppendLine();
                sb.AppendLine("--- 最近转换记录 ---");
                foreach (var entry in services.History.Entries.Take(20))
                {
                    sb.AppendFormat("{0:yyyy-MM-dd HH:mm}  {1} -> {2}  {3}  {4}",
                        entry.Timestamp, entry.InputFormat, entry.OutputFormat, entry.Result, entry.FileName);
                    sb.AppendLine();
                }

                var dialog = new SaveFileDialog
                {
                    Title = "导出当前软件日志",
                    Filter = "文本文件 (*.txt)|*.txt|日志文件 (*.log)|*.log",
                    FileName = "SkyFusion-logs-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt"
                };

                if (dialog.ShowDialog(owner) != true)
                {
                    return;
                }

                File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                LogService.Info("日志已导出: " + dialog.FileName);
                ToastService.Instance.ShowWithAction(
                    "日志已导出完成。",
                    ToastKind.Success,
                    "打开导出文件",
                    () => ExplorerHelper.OpenInExplorer(dialog.FileName));
            }
            catch (Exception ex)
            {
                LogService.Error("导出日志失败", ex);
                ToastService.Instance.Show("导出日志失败：" + ex.Message, ToastKind.Error);
            }
        }
    }
}
