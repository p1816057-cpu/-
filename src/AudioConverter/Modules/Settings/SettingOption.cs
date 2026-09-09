using AudioConverter.Models;

namespace AudioConverter.Modules.Settings
{
    public sealed class SettingOption
    {
        public SettingOption(OutputFormat value, string name)
        {
            Value = value;
            Name = name;
        }

        public OutputFormat Value { get; }

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
