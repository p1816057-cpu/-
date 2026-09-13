using AudioConverter.Common;

namespace AudioConverter.Modules
{
    public abstract class ViewModelBase : ObservableObject
    {
        public virtual string Title { get; protected set; }

        /// <summary>
        /// 页面被切换到前台时调用：用于同步其它页面在设置里改过的数据。
        /// </summary>
        public virtual void OnActivated()
        {
        }
    }
}
