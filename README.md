# 天融 SkyFusion

**天融万物，格式无界 · Fuse Anything, Convert Anything**

天融（SkyFusion）离线全能格式转换工具。当前版本以音频转换为核心：FlClash 式模块化架构 + FFmpeg 音频转换核心，UI 采用 Codex Desktop / FlClash 风格的现代桌面工作台。

## 产品命名

| 项目 | 名称 |
| --- | --- |
| 中文名 | 天融 |
| 英文名 | SkyFusion |
| 中文 Slogan | 天融万物，格式无界 |
| 英文 Slogan | Fuse Anything, Convert Anything |
| 主程序 | `SkyFusion.exe` |
| 主窗口标题 | `天融 - SkyFusion` |
| 产品文件夹 | `SkyFusion` |

## 版本

UI/UX 架构基准：v1.0.3

## 技术栈

- C# / WPF / MVVM
- .NET Framework 4.8（兼容 Windows 7 SP1 x64）
- FFmpeg 7.0（Win7 可用构建，essentials/full 均可）
- 无浏览器、无 WebView、无 HTTP 服务、无 WebSocket、无第三方 UI 依赖

## 功能

- Application Shell：左侧 Sidebar（转换 / 历史｜设置 / 关于）
- 转换页：拖拽上传区、文件列表区、转换控制区、码率格式参数区
- 三态全选 / 独立勾选 / 实时计数与“开始转换(Y)”
- 自动识别音频与视频（MP4/MKV 提取音轨）
- 固定编码参数：MP3 320k CBR、WAV 16-bit PCM、FLAC Level 5
- 顺序任务队列：进度 / 速度 / 取消 / 自动重试 / 冲突策略
- 历史记录（最近 200 条，本机 JSON 持久化）
- 设置页（常规 / 转换 / 输出 / 文件冲突 / 任务 / 存储）
- 浅色统一主题与公共组件（Button / Card / Checkbox / List / Dropdown / ProgressBar / Toast / EmptyState）

## 目录结构

```
src/AudioConverter/
├─ App/（ShellViewModel 等位于 Shell 命名空间）
├─ Common/        纯工具与命令基类
├─ Core/          FFmpeg/FFprobe/任务队列/路径解析
├─ Data/          JSON 持久化
├─ Models/        领域模型
├─ Services/      组合根、设置、历史
├─ Modules/
│  ├─ Conversion/ 转换模块
│  ├─ History/    历史模块
│  ├─ Settings/   设置模块
│  └─ About/      关于模块
├─ Components/    公共组件
└─ Theme/         主题与转换器
tools/ffmpeg/bin/  FFmpeg 运行文件（构建时自动复制到 EXE 输出目录）
```

## 构建

```powershell
dotnet build AudioConverter.sln -c Release
```

输出：

```
src/AudioConverter/bin/Release/net48/SkyFusion.exe
src/AudioConverter/bin/Release/net48/ffmpeg/bin/ffmpeg.exe
src/AudioConverter/bin/Release/net48/ffmpeg/bin/ffprobe.exe
```

发布目录见 `artifacts/SkyFusion/`。

## FFmpeg

- Win10/11 也可直接使用官方新版 ffmpeg。
- Win7 SP1 请使用 FFmpeg 7.0 构建（本仓库工具目录已放置该版本）。
- 自动查找顺序：设置中指定目录 → 程序目录 `ffmpeg\bin` → PATH。

## 系统要求

- Windows 7 SP1 x64 / Windows 10 x64 / Windows 11 x64
- .NET Framework 4.8（Win7 需安装；Win10/11 通常自带）

## 数据位置

- 设置：`%APPDATA%\AudioConverter\settings.json`
- 历史：`%APPDATA%\AudioConverter\history.json`
