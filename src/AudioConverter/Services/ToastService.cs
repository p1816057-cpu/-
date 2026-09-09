using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AudioConverter.Services
{
    public enum ToastKind
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// 居中模态弹窗：替代系统 MessageBox，UI 与 FlClash 风格保持一致。
    /// </summary>
    public sealed class ToastService
    {
        public static readonly ToastService Instance = new ToastService();

        public void Show(string message, ToastKind kind = ToastKind.Info)
        {
            Application app = Application.Current;
            if (app == null || app.MainWindow == null)
            {
                return;
            }

            app.Dispatcher.Invoke(new Action(() =>
                ShowDialog(app.MainWindow, DefaultTitle(kind), message, kind, false)));
        }

        public void ShowWithAction(
            string message,
            ToastKind kind,
            string actionText,
            Action action)
        {
            var app = Application.Current;
            if (app == null || app.MainWindow == null)
            {
                return;
            }

            app.Dispatcher.Invoke(new Action(() =>
                ShowDialog(app.MainWindow, DefaultTitle(kind), message, kind, false, actionText, action)));
        }

        public static bool Confirm(string message, string title = "请确认", ToastKind kind = ToastKind.Warning)
        {
            var owner = Application.Current?.MainWindow;
            if (owner == null)
            {
                return false;
            }

            bool? result = null;
            Application.Current.Dispatcher.Invoke(new Action(() =>
                result = ShowDialog(owner, title, message, kind, true)));
            return result == true;
        }

        private static bool? ShowDialog(
            Window owner,
            string title,
            string message,
            ToastKind kind,
            bool confirmMode,
            string actionText = null,
            Action action = null)
        {
            Brush surface = Resolve("Brush.Surface");
            Brush border = Resolve("Brush.Border");
            Brush kindBackground;
            Brush kindForeground;
            string glyph;

            switch (kind)
            {
                case ToastKind.Success:
                    kindBackground = Resolve("Brush.SuccessSoft");
                    kindForeground = Resolve("Brush.Success");
                    glyph = "\u2713";
                    break;
                case ToastKind.Warning:
                    kindBackground = Resolve("Brush.WarningSoft");
                    kindForeground = Resolve("Brush.Warning");
                    glyph = "!";
                    break;
                case ToastKind.Error:
                    kindBackground = Resolve("Brush.DangerSoft");
                    kindForeground = Resolve("Brush.Danger");
                    glyph = "\u2715";
                    break;
                default:
                    kindBackground = Resolve("Brush.AccentSoft");
                    kindForeground = Resolve("Brush.Accent");
                    glyph = "i";
                    break;
            }

            var root = new Border
            {
                Background = surface,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Width = 400
            };
            root.Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 24,
                ShadowDepth = 1,
                Opacity = 0.08
            };

            var panel = new StackPanel();
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            var iconBorder = new Border
            {
                Background = kindBackground,
                CornerRadius = new CornerRadius(14),
                Width = 28,
                Height = 28,
                VerticalAlignment = VerticalAlignment.Center
            };
            var icon = new TextBlock
            {
                Text = glyph,
                Foreground = kindForeground,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = icon;
            header.Children.Add(iconBorder);

            header.Children.Add(new TextBlock
            {
                Text = title,
                Foreground = Resolve("Brush.TextPrimary"),
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0)
            });
            panel.Children.Add(header);

            panel.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = Resolve("Brush.TextSecondary"),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(38, 10, 0, 0)
            });

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            Button cancelButton = null;
            Button actionButton = null;
            if (!string.IsNullOrEmpty(actionText) && action != null)
            {
                actionButton = new Button
                {
                    Content = actionText,
                    Style = (Style)Application.Current.FindResource("Button.Secondary"),
                    Width = 120,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                buttons.Children.Add(actionButton);
            }

            if (confirmMode)
            {
                cancelButton = new Button
                {
                    Content = "取消",
                    Style = (Style)Application.Current.FindResource("Button.Secondary"),
                    Width = 92,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                buttons.Children.Add(cancelButton);
            }

            var okButton = new Button
            {
                Content = confirmMode ? "确认" : "确定",
                Style = (Style)Application.Current.FindResource("Button.Primary"),
                Width = 92,
                IsDefault = true
            };
            buttons.Children.Add(okButton);
            panel.Children.Add(buttons);
            root.Child = panel;

            var dialog = new Window
            {
                Content = root,
                Title = title,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                SizeToContent = SizeToContent.WidthAndHeight
            };

            bool? result = false;
            okButton.Click += (s, e) =>
            {
                result = true;
                dialog.Close();
            };
            if (cancelButton != null)
            {
                cancelButton.Click += (s, e) =>
                {
                    result = false;
                    dialog.Close();
                };
            }

            if (actionButton != null)
            {
                actionButton.Click += (s, e) =>
                {
                    try
                    {
                        action?.Invoke();
                    }
                    finally
                    {
                        result = true;
                        dialog.Close();
                    }
                };
            }

            dialog.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    result = false;
                    dialog.Close();
                }
            };

            dialog.ShowDialog();
            return result;
        }

        private static string DefaultTitle(ToastKind kind)
        {
            switch (kind)
            {
                case ToastKind.Success:
                    return "操作成功";
                case ToastKind.Warning:
                    return "提示";
                case ToastKind.Error:
                    return "错误";
                default:
                    return "提示";
            }
        }

        private static Brush Resolve(string key)
        {
            return Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;
        }
    }
}
