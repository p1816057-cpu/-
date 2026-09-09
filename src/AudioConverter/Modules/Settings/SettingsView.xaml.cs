using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AudioConverter.Modules.Settings
{
    public partial class SettingsView : UserControl
    {
        private bool _updatingFromScroll;

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

        private void OnSectionTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_updatingFromScroll ||
                !(e.NewValue is SettingsNavItem item) ||
                !(DataContext is SettingsViewModel viewModel))
            {
                return;
            }

            viewModel.SelectedSection = item.SectionKey;
            SmoothScrollTo(SectionByKey(item.SectionKey));
        }

        private void OnSettingsScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_updatingFromScroll || !IsLoaded || !(SettingsScroll.Content is FrameworkElement content))
            {
                return;
            }

            double trigger = SettingsScroll.VerticalOffset + SettingsScroll.ViewportHeight * 0.35;
            string active = "音频默认";

            var sections = new[]
            {
                new { Name = "音频默认", Element = SectionGeneral },
                new { Name = "图片默认", Element = SectionImageDefault },
                new { Name = "音频输出", Element = SectionOutput },
                new { Name = "图片输出", Element = SectionImageOutput },
                new { Name = "转换", Element = SectionConversion },
                new { Name = "文件冲突", Element = SectionConflict },
                new { Name = "任务", Element = SectionTask },
                new { Name = "存储", Element = SectionStorage }
            };

            foreach (var section in sections)
            {
                double top = section.Element.TranslatePoint(new Point(0, 0), content).Y;
                if (top <= trigger + 6)
                {
                    active = section.Name;
                }
                else
                {
                    break;
                }
            }

            if (DataContext is SettingsViewModel viewModel && viewModel.SelectedSection != active)
            {
                _updatingFromScroll = true;
                try
                {
                    viewModel.SelectedSection = active;
                    SelectInTree(active);
                }
                finally
                {
                    _updatingFromScroll = false;
                }
            }
        }

        private FrameworkElement SectionByKey(string key)
        {
            switch (key)
            {
                case "音频默认":
                    return SectionGeneral;
                case "图片默认":
                    return SectionImageDefault;
                case "音频输出":
                    return SectionOutput;
                case "图片输出":
                    return SectionImageOutput;
                case "转换":
                    return SectionConversion;
                case "文件冲突":
                    return SectionConflict;
                case "任务":
                    return SectionTask;
                case "存储":
                    return SectionStorage;
                default:
                    return null;
            }
        }

        private void SmoothScrollTo(FrameworkElement target)
        {
            if (target == null)
            {
                return;
            }

            if (SettingsScroll.Content is FrameworkElement content)
            {
                double top = target.TranslatePoint(new Point(0, 0), content).Y;
                double from = SettingsScroll.VerticalOffset;
                double to = Math.Max(0, top - 8);
                var animation = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(260));
                animation.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
                SettingsScroll.BeginAnimation(AnimatedVerticalOffsetProperty, animation);
            }
        }

        private void SelectInTree(string sectionKey)
        {
            if (!(DataContext is SettingsViewModel viewModel))
            {
                return;
            }

            SectionTree.UpdateLayout();

            foreach (var group in viewModel.NavGroups)
            {
                var groupContainer = SectionTree.ItemContainerGenerator.ContainerFromItem(group) as TreeViewItem;
                if (groupContainer == null)
                {
                    SectionTree.UpdateLayout();
                    groupContainer = SectionTree.ItemContainerGenerator.ContainerFromItem(group) as TreeViewItem;
                }

                if (groupContainer == null)
                {
                    continue;
                }

                groupContainer.IsExpanded = true;
                groupContainer.UpdateLayout();
                var item = group.Items.FirstOrDefault(i => i.SectionKey == sectionKey);
                if (item == null)
                {
                    continue;
                }

                var itemContainer = groupContainer.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (itemContainer != null)
                {
                    itemContainer.IsSelected = true;
                    return;
                }
            }
        }

        private static void OnAnimatedVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
            }
        }
    }
}
