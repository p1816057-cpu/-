using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AudioConverter.Common;
using AudioConverter.Modules;
using AudioConverter.Modules.About;
using AudioConverter.Modules.Conversion;
using AudioConverter.Modules.History;
using AudioConverter.Modules.Office;
using AudioConverter.Modules.Settings;
using AudioConverter.Services;

namespace AudioConverter.Shell
{
    public sealed class ShellViewModel : ObservableObject
    {
        private readonly Dictionary<AppPage, ViewModelBase> _pages = new Dictionary<AppPage, ViewModelBase>();
        private readonly AppServices _services;
        private ViewModelBase _currentViewModel;
        private NavigationItem _selectedTop;
        private NavigationItem _selectedBottom;

        public ShellViewModel(AppServices services)
        {
            _services = services;
            TopItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem(AppPage.Conversion, "音频转换", "Convert"),
                new NavigationItem(AppPage.Office, "办公转换", "Office"),
                new NavigationItem(AppPage.History, "历史", "History")
            };
            BottomItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem(AppPage.Settings, "设置", "Settings"),
                new NavigationItem(AppPage.About, "关于", "About")
            };

            NavigateCommand = new RelayCommand(p => Navigate(p as NavigationItem));
            Navigate(TopItems[0]);
        }

        public ObservableCollection<NavigationItem> TopItems { get; }

        public ObservableCollection<NavigationItem> BottomItems { get; }

        public ICommand NavigateCommand { get; }

        public NavigationItem SelectedTopItem
        {
            get { return _selectedTop; }
            set
            {
                if (SetProperty(ref _selectedTop, value) && value != null)
                {
                    Navigate(value);
                }
            }
        }

        public NavigationItem SelectedBottomItem
        {
            get { return _selectedBottom; }
            set
            {
                if (SetProperty(ref _selectedBottom, value) && value != null)
                {
                    Navigate(value);
                }
            }
        }

        public ViewModelBase CurrentViewModel
        {
            get { return _currentViewModel; }
            private set { SetProperty(ref _currentViewModel, value); }
        }

        private void Navigate(NavigationItem item)
        {
            if (item == null)
            {
                return;
            }

            if (!_pages.TryGetValue(item.Page, out var page))
            {
                switch (item.Page)
                {
                    case AppPage.Conversion:
                        page = new ConversionViewModel(_services);
                        break;
                    case AppPage.Office:
                        page = new OfficeViewModel();
                        break;
                    case AppPage.History:
                        page = new HistoryViewModel(_services);
                        break;
                    case AppPage.Settings:
                        page = new SettingsViewModel(_services);
                        break;
                    default:
                        page = new AboutViewModel(_services);
                        break;
                }

                _pages[item.Page] = page;
            }

            bool isTop = TopItems.Contains(item);
            _selectedTop = isTop ? item : null;
            _selectedBottom = isTop ? null : item;
            OnPropertyChanged(nameof(SelectedTopItem));
            OnPropertyChanged(nameof(SelectedBottomItem));

            CurrentViewModel = page;
        }

        /// <summary>
        /// 从命令行 / 资源管理器“打开方式”传入媒体文件时调用。
        /// </summary>
        public void OpenMediaFiles(IEnumerable<string> paths)
        {
            if (paths == null)
            {
                return;
            }

            var files = paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            if (files.Count == 0)
            {
                return;
            }

            Navigate(TopItems[0]);
            if (CurrentViewModel is ConversionViewModel conversion)
            {
                conversion.AddPaths(files);
            }
        }

        public void NavigateTo(AppPage page)
        {
            var item = TopItems.FirstOrDefault(i => i.Page == page) ??
                       BottomItems.FirstOrDefault(i => i.Page == page);
            if (item != null)
            {
                Navigate(item);
            }
        }
    }
}
