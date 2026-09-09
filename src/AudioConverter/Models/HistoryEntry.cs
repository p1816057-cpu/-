using System;
using System.Runtime.Serialization;

namespace AudioConverter.Models
{
    public enum HistoryResult
    {
        Success,
        Failed,
        Cancelled
    }

    [DataContract]
    public sealed class HistoryEntry
    {
        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public string InputFormat { get; set; }

        [DataMember]
        public string OutputFormat { get; set; }

        [DataMember]
        public string OutputPath { get; set; }

        [DataMember]
        public long SizeBytes { get; set; }

        [DataMember]
        public double DurationSeconds { get; set; }

        [DataMember]
        public HistoryResult Result { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public string ErrorCode { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }
    }
}
