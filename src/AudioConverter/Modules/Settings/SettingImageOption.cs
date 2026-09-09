using AudioConverter.Models;

namespace AudioConverter.Modules.Settings
{
    public sealed class SettingImageOption
    {
        public SettingImageOption(ImageOutputFormat value, string name)
        {
            Value = value;
            Name = name;
        }

        public ImageOutputFormat Value { get; }

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
