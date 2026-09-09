using AudioConverter.Models;

namespace AudioConverter.Modules.Conversion
{
    public sealed class OutputFormatOption
    {
        public OutputFormatOption(OutputFormat value, string name, string summary)
        {
            Value = value;
            Name = name;
            Summary = summary;
        }

        public OutputFormat Value { get; }

        public string Name { get; }

        public string Summary { get; }

        public string DisplayName
        {
            get { return Name + " · " + Summary; }
        }

        public string Extension
        {
            get { return "." + Value.ToString().ToLowerInvariant(); }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
