using System;

namespace AudioConverter.Common
{
    /// <summary>
    /// Win7 兼容判断：仅在检测到 Win10+ 时启用可选增强，避免在旧系统调用不存在的 API。
    /// </summary>
    public static class WindowsCompatibility
    {
        public static bool IsWindows10OrLater
        {
            get { return Environment.OSVersion.Version.Major >= 10; }
        }

        public static bool IsWindows7
        {
            get
            {
                var v = Environment.OSVersion.Version;
                return v.Major == 6 && v.Minor == 1;
            }
        }

        public static string CompatibilityText
        {
            get { return "Windows 7 SP1 (x64) / Windows 10 / Windows 11"; }
        }
    }
}
