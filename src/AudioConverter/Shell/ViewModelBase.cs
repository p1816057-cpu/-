using AudioConverter.Common;

namespace AudioConverter.Modules
{
    public abstract class ViewModelBase : ObservableObject
    {
        public virtual string Title { get; protected set; }
    }
}
