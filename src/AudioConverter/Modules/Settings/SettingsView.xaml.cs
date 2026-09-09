using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AudioConverter.Modules.Settings
{
    public partial class SettingsView : UserControl
    {
        private static readonly DependencyProperty AnimatedVerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "AnimatedVerticalOffset",
                typeof(double),
                typeof(SettingsView),
                new PropertyMetadata(0d, OnAnimatedVerticalOffsetChanged));

        public SettingsView()
        {
            InitializeComponent();
        }

        private void OnSectionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var list = sender as ListBox;
            if (!IsLoaded || list == null || !(list.SelectedItem is string section))
            {
                return;
            }

            FrameworkElement target = null;
            switch (section)
            {
                case "常规":
                    target = SectionGeneral;
                    break;
                case "转换":
                    target = SectionConversion;
                    break;
                case "输出":
                    target = SectionOutput;
                    break;
                case "文件冲突":
                    target = SectionConflict;
                    break;
                case "任务":
                    target = SectionTask;
                    break;
                case "存储":
                    target = SectionStorage;
                    break;
            }

            if (target == null)
            {
                return;
            }

            if (SettingsScroll.Content is FrameworkElement content)
            {
                Point point = target.TranslatePoint(new Point(0, 0), content);
                double from = SettingsScroll.VerticalOffset;
                double to = Math.Max(0, point.Y - 8);
                var animation = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(260));
                animation.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
                SettingsScroll.BeginAnimation(AnimatedVerticalOffsetProperty, animation);
            }
        }

        private static void OnAnimatedVerticalOffsetChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
            }
        }
    }
}
