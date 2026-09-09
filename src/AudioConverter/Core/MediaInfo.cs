namespace AudioConverter.Core
{
    public sealed class MediaInfo
    {
        public bool HasAudio { get; set; }

        public bool HasVideo { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public double DurationSeconds { get; set; }

        public long SizeBytes { get; set; }

        public string FormatName { get; set; }
    }
}
