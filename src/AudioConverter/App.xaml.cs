using System.Windows;
using AudioConverter.Shell;
using AudioConverter.Services;

namespace AudioConverter
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show(
                    "程序发生未处理的错误：\n" + args.Exception.Message,
                    "天融 - SkyFusion",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            System.Windows.Forms.Application.EnableVisualStyles();

            var services = new AppServices();
            var window = new MainWindow
            {
                DataContext = new ShellViewModel(services)
            };
            MainWindow = window;
            window.Show();

            if (e.Args != null && e.Args.Length > 0)
            {
                (window.DataContext as ShellViewModel)?.OpenMediaFiles(e.Args);
            }
        }
    }
}
