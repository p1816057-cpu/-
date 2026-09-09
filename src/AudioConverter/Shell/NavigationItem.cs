namespace AudioConverter.Shell
{
    public sealed class NavigationItem
    {
        public NavigationItem(AppPage page, string displayName, string iconKey)
        {
            Page = page;
            DisplayName = displayName;
            IconKey = iconKey;
        }

        public AppPage Page { get; }

        public string DisplayName { get; }

        public string IconKey { get; }
    }
}
