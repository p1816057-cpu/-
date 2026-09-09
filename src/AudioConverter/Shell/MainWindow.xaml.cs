using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Microsoft.Win32;
using AudioConverter.Services;
using System.Windows.Controls;

namespace AudioConverter.Shell
{
    public partial class MainWindow : Window
    {
        private ShellViewModel _shell;

        public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_shell != null)
            {
                _shell.PropertyChanged -= OnShellPropertyChanged;
            }

            _shell = DataContext as ShellViewModel;
            if (_shell != null)
            {
                _shell.PropertyChanged += OnShellPropertyChanged;
            }
        }

        private void OnShellPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShellViewModel.CurrentViewModel) && IsLoaded)
            {
                var animation = new DoubleAnimation(0.35, 1, System.TimeSpan.FromMilliseconds(180));
                animation.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
                PageHost.BeginAnimation(OpacityProperty, animation);
            }
        }

        private void OnCaptionMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.ClickCount == 2)
                {
                    ToggleMaximize();
                }
                else
                {
                    try
                    {
                        DragMove();
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeClick(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximize()
        {
            if (WindowState == WindowState.Normal)
            {
                MaxWidth = SystemParameters.WorkArea.Width;
                MaxHeight = SystemParameters.WorkArea.Height;
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowState = WindowState.Normal;
                MaxWidth = double.PositiveInfinity;
                MaxHeight = double.PositiveInfinity;
            }
        }

        private void OnOpenAudioFileClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "打开音频或视频文件",
                Multiselect = true,
                Filter = "支持的媒体文件 (*.mp3;*.wav;*.flac;*.mp4;*.mkv)|*.mp3;*.wav;*.flac;*.mp4;*.mkv|所有文件 (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) == true)
            {
                _shell?.OpenMediaFiles(dialog.FileNames);
            }
        }

        private void OnAboutClick(object sender, RoutedEventArgs e)
        {
            _shell?.NavigateTo(AppPage.About);
        }

        private void OnGitHubClick(object sender, RoutedEventArgs e)
        {
            ToastService.Instance.Show("GitHub 仓库地址将在项目上架后填写。");
        }
    }
}
