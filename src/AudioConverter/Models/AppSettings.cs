using System.Runtime.Serialization;

namespace AudioConverter.Models
{
    [DataContract]
    public sealed class AppSettings
    {
        [DataMember]
        public OutputFormat DefaultOutputFormat { get; set; } = OutputFormat.Mp3;

        [DataMember]
        public ImageOutputFormat DefaultImageFormat { get; set; } = ImageOutputFormat.Webp;

        [DataMember]
        public string OutputDirectory { get; set; }

        [DataMember]
        public string ImageOutputDirectory { get; set; }

        /// <summary>音频输出位置模式，默认跟随源文件。</summary>
        [DataMember]
        public OutputLocationMode AudioOutputMode { get; set; }

        /// <summary>图片输出位置模式，默认跟随源文件。</summary>
        [DataMember]
        public OutputLocationMode ImageOutputMode { get; set; }

        [DataMember]
        public ConflictPolicy ConflictPolicy { get; set; } = ConflictPolicy.Rename;

        [DataMember]
        public bool AutoRetry { get; set; }

        [DataMember]
        public int RetryLimit { get; set; } = 2;

        [DataMember]
        public string LogDirectory { get; set; }

        [DataMember]
        public string TempDirectory { get; set; }

        [DataMember]
        public string FfmpegDirectory { get; set; }

        [DataMember]
        public int HistoryLimit { get; set; } = 200;
    }
}
