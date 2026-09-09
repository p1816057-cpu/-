using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using AudioConverter.Services;
using System.Windows.Controls;

namespace AudioConverter.Shell
{
    public partial class MainWindow : Window
    {
        private ShellViewModel _shell;
        private DispatcherTimer _subMenuTimer;

        public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            SizeChanged += OnWindowSizeChanged;
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

            UpdateSidebarLayout();
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSidebarLayout();
        }

        private void UpdateSidebarLayout()
        {
            if (_shell == null)
            {
                return;
            }

            bool expanded = ActualWidth >= 1000;
            _shell.IsSidebarExpanded = expanded;
            SidebarColumn.Width = new GridLength(expanded ? 226 : 70);
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
            CloseMenus();
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
            CloseMenus();
            _shell?.NavigateTo(AppPage.About);
        }

        private void OnGitHubClick(object sender, RoutedEventArgs e)
        {
            CloseMenus();
            ToastService.Instance.Show("GitHub 仓库地址将在项目上架后填写。");
        }

        private void OnExportLogsClick(object sender, RoutedEventArgs e)
        {
            CloseMenus();
            LogExporter.Export(_shell?.Services, this);
        }

        private void OnFileMenuButtonClick(object sender, RoutedEventArgs e)
        {
            ImportSubPopup.IsOpen = false;
            HelpMenuPopup.IsOpen = false;
            FileMenuPopup.IsOpen = !FileMenuPopup.IsOpen;
        }

        private void OnHelpMenuButtonClick(object sender, RoutedEventArgs e)
        {
            FileMenuPopup.IsOpen = false;
            ImportSubPopup.IsOpen = false;
            HelpMenuPopup.IsOpen = !HelpMenuPopup.IsOpen;
        }

        private void CloseMenus()
        {
            FileMenuPopup.IsOpen = false;
            HelpMenuPopup.IsOpen = false;
            ImportSubPopup.IsOpen = false;
        }

        private void OnImportMenuButtonClick(object sender, RoutedEventArgs e)
        {
            ImportSubPopup.IsOpen = !ImportSubPopup.IsOpen;
        }

        private void OnImportMenuMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            StopSubMenuTimer();
            ImportSubPopup.IsOpen = true;
        }

        private void OnImportMenuMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            StartSubMenuCloseTimer();
        }

        private void OnImportSubMenuMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            StopSubMenuTimer();
        }

        private void OnImportSubMenuMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            StartSubMenuCloseTimer();
        }

        private void StartSubMenuCloseTimer()
        {
            StopSubMenuTimer();
            _subMenuTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(320) };
            _subMenuTimer.Tick += (s, e) =>
            {
                StopSubMenuTimer();
                ImportSubPopup.IsOpen = false;
            };
            _subMenuTimer.Start();
        }

        private void StopSubMenuTimer()
        {
            if (_subMenuTimer != null)
            {
                _subMenuTimer.Stop();
                _subMenuTimer = null;
            }
        }

        private void OnOpenImageFileClick(object sender, RoutedEventArgs e)
        {
            CloseMenus();
            var dialog = new OpenFileDialog
            {
                Title = "打开图片文件",
                Multiselect = true,
                Filter = "图片文件 (*.jpg;*.jpeg;*.png;*.webp;*.bmp)|*.jpg;*.jpeg;*.png;*.webp;*.bmp|所有文件 (*.*)|*.*"
            };

            if (dialog.ShowDialog(this) == true)
            {
                _shell?.OpenMediaFiles(dialog.FileNames);
            }
        }

        private void OnDocsClick(object sender, RoutedEventArgs e)
        {
            CloseMenus();
            string[] candidates =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "使用说明.txt"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "README.md")
            };

            string doc = Array.Find(candidates, File.Exists);
            if (doc == null)
            {
                ToastService.Instance.Show("帮助文档尚未生成到程序目录，请稍后重新尝试。", ToastKind.Warning);
                return;
            }

            try
            {
                Process.Start(doc);
            }
            catch (Exception ex)
            {
                ToastService.Instance.Show("无法打开文档：" + ex.Message, ToastKind.Error);
            }
        }

        private void OnCurrentVersionClick(object sender, RoutedEventArgs e)
        {
            CloseMenus();
            ToastService.Instance.Show(
                "当前版本：v1.1.0\n\n用于测试日志导出与问题反馈。",
                ToastKind.Info);
        }
    }
}
