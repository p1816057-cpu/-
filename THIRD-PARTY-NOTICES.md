# Third-Party Notices

本项目在源码与发布版中涉及以下第三方组件，发布与再分发时请保留对应许可声明。

## FFmpeg

- 网站：<https://ffmpeg.org/>
- 本项目发布包使用 Windows 构建版（来源：<https://www.gyan.dev/ffmpeg/builds/>）
- 许可证：GNU General Public License v3.0 (GPLv3)
- 许可文本：发布包内 `FFmpeg-LICENSE.txt`
- FFmpeg 源码：<https://ffmpeg.org/download.html>
- 说明：SkyFusion 以独立进程方式调用 ffmpeg.exe / ffprobe.exe，
  不将 FFmpeg 库静态/动态链接进本项目源码。

## Microsoft .NET Framework

- 本项目目标框架：.NET Framework 4.8
- 发布 Setup 与 Win7 便携包内附带微软 .NET Framework 4.8 离线安装包，
  用于离线部署。
- 再分发须遵守微软软件许可条款，详见微软官网：
  <https://dotnet.microsoft.com/download/dotnet-framework/net48>

## Windows / WPF / 字体

- 应用基于 Microsoft WPF / .NET Framework 开发，遵循微软许可条款使用。
- 界面使用 Windows 系统字体（如 Segoe UI / Microsoft YaHei / Segoe UI Symbol），
  本项目不随包复制字体文件。

## UI 风格参考

- 本项目参考过 Codex Desktop、FlClash、PCL2 等产品的布局与交互风格，
  但未复制其源码、素材或品牌资源；图标与界面元素均为本项目原创。

## 其他

- 本项目不包含 QQ 音乐等商业加密格式的解密实现。
- 若后续新增依赖库，请同步更新本文件。
