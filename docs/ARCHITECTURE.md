# 架构说明

参照 FlClash 的模块化组织（仅架构与视觉气质，不采用其 Flutter 技术栈，因为 Flutter 桌面不支持 Win7）。

## 分层

```text
UI（Modules Views）
  ↓ 数据绑定
ViewModel（Modules ViewModels）
  ↓ 命令
Services / Managers（AppServices、Toast、HistoryService、SettingsService）
  ↓
Core（FfmpegLocator、FfprobeRunner、FfmpegRunner、ConversionTaskManager、OutputPathResolver、OutputLocation）
  ↓
FFmpeg.exe / FFprobe.exe（本地进程，同机调用）
```

约束：

- UI 不直接调用 FFmpeg。
- UI 不拼接 FFmpeg 参数（参数只存在于 Core 的 FfmpegRunner）。
- 转换进度与状态由 TaskManager 事件 → ViewModel → UI。
- 模块之间不互相引用页面控件；共享控件在 `Components` / `Theme`。

## 模块职责

| 位置 | 职责 |
| --- | --- |
| `App` / `Shell` | 启动装配、侧栏导航、页面缓存 |
| `Modules/Conversion` | 上传、列表、参数、状态显示 |
| `Modules/History` | 历史展示与清空 |
| `Modules/Settings` | 六组设置 |
| `Modules/About` | 版本与 FFmpeg 信息 |
| `Core` | 纯转换逻辑，无 UI 依赖 |
| `Services` | 组合根、设置/历史/Toast |
| `Data` | JSON 持久化 |
| `Theme` | 颜色、字体、间距、控件模板 |
| `Components` | 跨页面复用控件（如 EmptyState、TriStateCheckBox） |
