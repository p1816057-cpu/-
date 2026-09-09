using System.Collections.Generic;

namespace AudioConverter.Modules.Settings
{
    public sealed class SettingsNavGroup
    {
        public SettingsNavGroup(string name, IEnumerable<SettingsNavItem> items)
        {
            Name = name;
            Items = new List<SettingsNavItem>(items);
        }

        public string Name { get; }

        public IReadOnlyList<SettingsNavItem> Items { get; }
    }

    public sealed class SettingsNavItem
    {
        public SettingsNavItem(string name, string sectionKey)
        {
            Name = name;
            SectionKey = sectionKey;
        }

        public string Name { get; }

        public string SectionKey { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
