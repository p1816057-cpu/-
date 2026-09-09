using System.Windows.Controls;

namespace AudioConverter.Components
{
    /// <summary>
    /// 三态复选框：可显示 indeterminate；点击时只在全选/全不选之间切换。
    /// </summary>
    public class TriStateCheckBox : CheckBox
    {
        public TriStateCheckBox()
        {
            IsThreeState = true;
        }

        protected override void OnToggle()
        {
            IsChecked = IsChecked == true ? (bool?)false : (bool?)true;
        }
    }
}
