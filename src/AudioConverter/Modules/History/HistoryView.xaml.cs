using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AudioConverter.Modules.History
{
    public partial class HistoryView : UserControl
    {
        private static readonly string[] SortKeys =
        {
            "文件", "原格式", "转换后格式", "结果", "输出大小", "耗时", "时间", "错误码/信息"
        };

        public HistoryView()
        {
            InitializeComponent();
            Loaded += (s, e) => UpdateHeaderArrows();
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible && DataContext is HistoryViewModel viewModel)
                {
                    viewModel.ReloadFromStore();
                    UpdateHeaderArrows();
                }
            };
        }

        private void OnSortClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button &&
                button.Tag is string key &&
                DataContext is HistoryViewModel viewModel)
            {
                viewModel.SortBy(key);
                UpdateHeaderArrows();
            }
        }

        private void UpdateHeaderArrows()
        {
            if (!(DataContext is HistoryViewModel viewModel))
            {
                return;
            }

            var headers = new Dictionary<string, Button>
            {
                ["文件"] = HeaderFile,
                ["原格式"] = HeaderInput,
                ["转换后格式"] = HeaderOutput,
                ["结果"] = HeaderResult,
                ["输出大小"] = HeaderSize,
                ["耗时"] = HeaderDuration,
                ["时间"] = HeaderTime,
                ["错误码/信息"] = HeaderError
            };

            string current = viewModel.SortColumn;
            string arrow = viewModel.IsSortAscending ? " ▲" : " ▼";
            foreach (var key in SortKeys)
            {
                if (headers.TryGetValue(key, out var button))
                {
                    button.Content = key + (string.Equals(current, key) ? arrow : "");
                }
            }
        }
    }
}
