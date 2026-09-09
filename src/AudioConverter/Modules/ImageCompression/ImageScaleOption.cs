namespace AudioConverter.Modules.ImageCompression
{
    public sealed class ImageScaleOption
    {
        public ImageScaleOption(string display, int? percent, int? maxEdge)
        {
            DisplayName = display;
            Percent = percent;
            MaxEdge = maxEdge;
        }

        public string DisplayName { get; }

        public int? Percent { get; }

        public int? MaxEdge { get; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
