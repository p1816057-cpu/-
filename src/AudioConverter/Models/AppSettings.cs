using System.Runtime.Serialization;

namespace AudioConverter.Models
{
    [DataContract]
    public sealed class AppSettings
    {
        [DataMember]
        public OutputFormat DefaultOutputFormat { get; set; } = OutputFormat.Mp3;

        [DataMember]
        public string OutputDirectory { get; set; }

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
