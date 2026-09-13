using System;
using System.Reflection;

namespace AudioConverter.Common
{
    /// <summary>
    /// 程序版本信息的唯一来源：统一从程序集读取（对应 csproj 的 InformationalVersion），
    /// 避免界面、日志、诊断导出里各写一份版本号导致不一致。
    /// </summary>
    public static class AppInfo
    {
        /// <summary>形如 1.2.1。</summary>
        public static readonly string Version = ReadVersion();

        /// <summary>形如 v1.2.1，用于界面显示。</summary>
        public static readonly string DisplayVersion = "v" + Version;

        /// <summary>
        /// GitHub 仓库地址：仓库建好后填在这里（例如 https://github.com/用户名/SkyFusion），
        /// 关于页与帮助菜单会统一使用；留空时界面提示"仓库地址待填写"。
        /// </summary>
        public static readonly string RepositoryUrl = "";

        private static string ReadVersion()
        {
            try
            {
                Assembly assembly = typeof(AppInfo).Assembly;
                var informational = Attribute.GetCustomAttribute(
                    assembly,
                    typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;

                if (informational != null && !string.IsNullOrWhiteSpace(informational.InformationalVersion))
                {
                    return informational.InformationalVersion.Trim();
                }

                Version version = assembly.GetName().Version;
                return version == null
                    ? "1.0.0"
                    : string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            }
            catch
            {
                return "1.0.0";
            }
        }
    }
}
