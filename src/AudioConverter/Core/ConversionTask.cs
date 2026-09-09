using AudioConverter.Models;

namespace AudioConverter.Core
{
    public sealed class ConversionTask
    {
        public ConversionTask(
            string filePath,
            OutputFormat outputFormat,
            string outputDirectory,
            ConflictPolicy conflictPolicy,
            bool autoRetry,
            int retryLimit)
        {
            FilePath = filePath;
            OutputFormat = outputFormat;
            OutputDirectory = outputDirectory;
            ConflictPolicy = conflictPolicy;
            AutoRetry = autoRetry;
            RetryLimit = retryLimit;
        }

        public string FilePath { get; }

        public OutputFormat OutputFormat { get; }

        public string OutputDirectory { get; }

        public ConflictPolicy ConflictPolicy { get; }

        public bool AutoRetry { get; }

        public int RetryLimit { get; }
    }

    public enum ConversionOutcome
    {
        Completed,
        Failed,
        Cancelled,
        Skipped
    }

    public sealed class ConversionTaskResult
    {
        public string FilePath { get; set; }

        public string OutputPath { get; set; }

        public ConversionOutcome Outcome { get; set; }

        public double ElapsedSeconds { get; set; }

        public long OutputSizeBytes { get; set; }

        public string ErrorCode { get; set; }

        public string ErrorMessage { get; set; }
    }

    public enum ConversionUpdateKind
    {
        TaskRunning,
        TaskProgress,
        TaskCompleted,
        TaskFailed,
        TaskCancelled,
        TaskSkipped
    }

    public sealed class ConversionTaskUpdate
    {
        public int Index { get; set; }

        public string FilePath { get; set; }

        public ConversionUpdateKind Kind { get; set; }

        public double Progress { get; set; }

        public string SpeedText { get; set; }

        public string Message { get; set; }

        public string OutputPath { get; set; }

        public ConversionOutcome Outcome { get; set; }
    }
}
