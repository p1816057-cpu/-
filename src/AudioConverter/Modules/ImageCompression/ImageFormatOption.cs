using AudioConverter.Models;

namespace AudioConverter.Modules.ImageCompression
{
    public sealed class ImageFormatOption
    {
        public ImageFormatOption(ImageOutputFormat value, string name, string summary)
        {
            Value = value;
            Name = name;
            Summary = summary;
        }

        public ImageOutputFormat Value { get; }

        public string Name { get; }

        public string Summary { get; }

        public string DisplayName
        {
            get { return Name + " · " + Summary; }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
