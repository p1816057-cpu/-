using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using AudioConverter.Shell;
using AudioConverter.Services;

namespace AudioConverter
{
    public partial class App : Application
    {
        /// <summary>单实例锁名字：同一台电脑同一登录会话只允许运行一个天融。</summary>
        private const string SingleInstanceMutexName = @"Local\SkyFusion.SingleInstance.Shell";

        /// <summary>第二次启动时用这条系统广播消息，把已运行的实例窗口提到最前。</summary>
        private static readonly uint ActivateWindowMessage =
            RegisterWindowMessage("SkyFusion.ActivateRunningWindow");

        private static readonly IntPtr HwndBroadcast = new IntPtr(0xFFFF);
        private const int AsfwAny = -1;
        private const int SwRestore = 9;

        /// <summary>主窗口标题（与 MainWindow.xaml 保持一致），用于直接定位已运行实例的窗口。</summary>
        private const string MainWindowTitle = "天融 - SkyFusion";

        private Mutex _singleInstanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!AcquireSingleInstance())
            {
                // 已有实例在运行：给出与其它提示同样式的弹窗，然后直接退出本进程
                ToastService.Notify("检测到已打开同类型软件，请勿重复打开。", "提示", ToastKind.Warning);

                // 用户看完提示后，把已经打开的那个窗口恢复到前台
                ActivateRunningInstance();
                Shutdown();
                return;
            }

            base.OnStartup(e);

            DispatcherUnhandledException += (s, args) =>
            {
                LogService.Error("未处理异常", args.Exception);
                MessageBox.Show(
                    "程序发生未处理的错误：\n" + args.Exception.Message,
                    "天融 - SkyFusion",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            System.Windows.Forms.Application.EnableVisualStyles();

            var services = new AppServices();
            LogService.Info("程序启动 " + AudioConverter.Common.AppInfo.DisplayVersion);
            var window = new MainWindow
            {
                DataContext = new ShellViewModel(services)
            };
            MainWindow = window;
            window.Show();
            HookActivateMessage(window);

            if (e.Args != null && e.Args.Length > 0)
            {
                (window.DataContext as ShellViewModel)?.OpenMediaFiles(e.Args);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _singleInstanceMutex?.Dispose();
            }
            catch
            {
                // 退出阶段忽略释放失败
            }

            base.OnExit(e);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint RegisterWindowMessage(string message);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool AllowSetForegroundWindow(int processId);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string className, string windowName);

        /// <summary>
        /// 通知已运行的实例把自己弹到前台。
        /// 跨进程抢前台会被系统拦下，所以先交出前台权限（AllowSetForegroundWindow），再广播消息。
        /// </summary>
        private static void ActivateRunningInstance()
        {
            try
            {
                AllowSetForegroundWindow(AsfwAny);

                // 双保险：直接找到已运行实例的主窗口，先恢复再置前；
                // 再由对方响应广播消息自己激活一次（对方也能处理最小化状态）。
                IntPtr existing = FindWindow(null, MainWindowTitle);
                if (existing != IntPtr.Zero)
                {
                    ShowWindow(existing, SwRestore);
                    SetForegroundWindow(existing);
                }

                PostMessage(HwndBroadcast, ActivateWindowMessage, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                LogService.Error("激活已运行实例失败", ex);
            }
        }

        /// <summary>监听第二次启动发来的广播消息。</summary>
        private void HookActivateMessage(Window window)
        {
            try
            {
                var source = HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);
                source?.AddHook(OnWindowMessage);
            }
            catch (Exception ex)
            {
                LogService.Error("注册单实例激活消息失败", ex);
            }
        }

        private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if ((uint)msg == ActivateWindowMessage)
            {
                ActivateMainWindow();
                handled = true;
            }

            return IntPtr.Zero;
        }

        /// <summary>把主窗口从最小化恢复并提到最前。</summary>
        private void ActivateMainWindow()
        {
            var window = MainWindow;
            if (window == null)
            {
                return;
            }

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Show();
            window.Activate();
            window.Topmost = true;
            window.Topmost = false;
            window.Focus();

            try
            {
                SetForegroundWindow(new WindowInteropHelper(window).Handle);
            }
            catch
            {
                // 少数系统策略下无法抢前台，前面的 Activate 已经尽力
            }
        }

        /// <summary>
        /// 尝试获取单实例锁。互斥体随进程结束自动销毁，因此 createdNew=false 就代表已有实例在运行。
        /// 极端情况下（受限环境创建失败）不阻塞启动，保证程序仍可用。
        /// </summary>
        private bool AcquireSingleInstance()
        {
            try
            {
                bool createdNew;
                _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out createdNew);
                return createdNew;
            }
            catch (Exception ex)
            {
                LogService.Error("单实例检测失败", ex);
                return true;
            }
        }
    }
}
