using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AudioConverter.Core
{
    /// <summary>
    /// 顺序任务队列：一次只转换一个文件，负责取消、重试、冲突策略与进度上报。
    /// UI 只订阅更新事件，不直接操作 FFmpeg。
    /// </summary>
    public sealed class ConversionTaskManager
    {
        private readonly FfmpegLocator _locator;
        private readonly FfprobeRunner _probeRunner;
        private readonly FfmpegRunner _ffmpegRunner;

        public ConversionTaskManager(FfmpegLocator locator)
        {
            _locator = locator;
            _probeRunner = new FfprobeRunner();
            _ffmpegRunner = new FfmpegRunner();
        }

        public async Task<IReadOnlyList<ConversionTaskResult>> RunAsync(
            IReadOnlyList<ConversionTask> tasks,
            IProgress<ConversionTaskUpdate> progress,
            CancellationToken cancellationToken)
        {
            var results = new List<ConversionTaskResult>(tasks.Count);

            for (int i = 0; i < tasks.Count; i++)
            {
                var task = tasks[i];
                if (cancellationToken.IsCancellationRequested)
                {
                    var cancelledBeforeStart = CreateResult(task);
                    cancelledBeforeStart.Outcome = ConversionOutcome.Cancelled;
                    cancelledBeforeStart.ErrorCode = "CANCELLED";
                    results.Add(cancelledBeforeStart);
                    Report(progress, i, task, ConversionUpdateKind.TaskCancelled, 0, null, cancelledBeforeStart.OutputPath, "已取消");
                    continue;
                }

                var resolution = OutputPathResolver.Resolve(
                    task.FilePath,
                    task.OutputDirectory,
                    task.OutputFormat,
                    task.ConflictPolicy);

                if (resolution.Action == OutputResolveAction.Skip)
                {
                    var skipped = CreateResult(task);
                    skipped.Outcome = ConversionOutcome.Skipped;
                    skipped.OutputPath = resolution.OutputPath;
                    skipped.ErrorCode = "OUTPUT_EXISTS";
                    results.Add(skipped);
                    Report(progress, i, task, ConversionUpdateKind.TaskSkipped, 0, null, resolution.OutputPath, "输出文件已存在，已跳过");
                    continue;
                }

                Report(progress, i, task, ConversionUpdateKind.TaskRunning, 0, null, resolution.OutputPath, null);

                var stopwatch = Stopwatch.StartNew();
                ConversionTaskResult final = null;
                int maxAttempts = task.AutoRetry ? Math.Max(1, task.RetryLimit + 1) : 1;

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        if (attempt > 1)
                        {
                            Report(progress, i, task, ConversionUpdateKind.TaskRunning, 0, null, resolution.OutputPath, null);
                        }

                        var media = await _probeRunner.ProbeAsync(
                                task.FilePath,
                                _locator.FfprobePath,
                                cancellationToken)
                            .ConfigureAwait(false);

                        if (!media.HasAudio)
                        {
                            final = CreateResult(task);
                            final.Outcome = ConversionOutcome.Failed;
                            final.OutputPath = resolution.OutputPath;
                            final.ErrorCode = "NO_AUDIO_STREAM";
                            final.ErrorMessage = "文件中没有可转换的音频流。";
                            break;
                        }

                        var runnerProgress = new Progress<ConversionProgress>(p =>
                            Report(progress, i, task, ConversionUpdateKind.TaskProgress, p.Percent, p.SpeedText, resolution.OutputPath, null));

                        var runResult = await _ffmpegRunner.ConvertAsync(
                                _locator.FfmpegPath,
                                task.FilePath,
                                resolution.OutputPath,
                                task.OutputFormat,
                                media.HasVideo,
                                media.DurationSeconds,
                                runnerProgress,
                                cancellationToken)
                            .ConfigureAwait(false);

                        if (runResult.Success)
                        {
                            final = CreateResult(task);
                            final.Outcome = ConversionOutcome.Completed;
                            final.OutputPath = resolution.OutputPath;
                            if (File.Exists(resolution.OutputPath))
                            {
                                final.OutputSizeBytes = new FileInfo(resolution.OutputPath).Length;
                            }

                            break;
                        }

                        final = CreateResult(task);
                        final.Outcome = ConversionOutcome.Failed;
                        final.OutputPath = resolution.OutputPath;
                        final.ErrorCode = "FFMPEG_EXIT_" + attempt;
                        final.ErrorMessage = runResult.ErrorText;

                        if (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
                        {
                            final = null;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        final = CreateResult(task);
                        final.Outcome = ConversionOutcome.Cancelled;
                        final.OutputPath = resolution.OutputPath;
                        final.ErrorCode = "CANCELLED";
                        break;
                    }
                    catch (Exception ex)
                    {
                        final = CreateResult(task);
                        final.Outcome = ConversionOutcome.Failed;
                        final.OutputPath = resolution.OutputPath;
                        final.ErrorCode = "RUNNER_ERROR";
                        final.ErrorMessage = ex.Message;
                    }
                }

                stopwatch.Stop();
                if (final == null)
                {
                    final = CreateResult(task);
                    final.Outcome = ConversionOutcome.Failed;
                    final.OutputPath = resolution.OutputPath;
                    final.ErrorCode = "UNKNOWN";
                }

                final.ElapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                results.Add(final);

                ConversionUpdateKind kind;
                switch (final.Outcome)
                {
                    case ConversionOutcome.Completed:
                        kind = ConversionUpdateKind.TaskCompleted;
                        break;
                    case ConversionOutcome.Cancelled:
                        kind = ConversionUpdateKind.TaskCancelled;
                        break;
                    case ConversionOutcome.Skipped:
                        kind = ConversionUpdateKind.TaskSkipped;
                        break;
                    default:
                        kind = ConversionUpdateKind.TaskFailed;
                        break;
                }

                Report(progress, i, task, kind, 0, null, final.OutputPath, final.ErrorMessage);
            }

            return results;
        }

        private static ConversionTaskResult CreateResult(ConversionTask task)
        {
            return new ConversionTaskResult { FilePath = task.FilePath };
        }

        private static void Report(
            IProgress<ConversionTaskUpdate> progress,
            int index,
            ConversionTask task,
            ConversionUpdateKind kind,
            double percent,
            string speed,
            string outputPath,
            string message)
        {
            progress?.Report(new ConversionTaskUpdate
            {
                Index = index,
                FilePath = task.FilePath,
                Kind = kind,
                Progress = percent,
                SpeedText = speed,
                Message = message,
                OutputPath = outputPath
            });
        }
    }
}
