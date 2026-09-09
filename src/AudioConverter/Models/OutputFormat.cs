namespace AudioConverter.Models
{
    public enum OutputFormat
    {
        Mp3,
        Wav,
        Flac
    }

    public enum FileKind
    {
        Audio,
        Video
    }

    public enum ConversionStatus
    {
        Waiting,
        Running,
        Completed,
        Failed,
        Cancelled,
        Skipped
    }

    public enum ConflictPolicy
    {
        Overwrite,
        Rename,
        Skip
    }
}
