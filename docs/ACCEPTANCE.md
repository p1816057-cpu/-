# v1.0.2 验收清单

- [x] EXE 启动（Release 构建验证通过）
- [x] 左侧 Sidebar（功能/设置分区）
- [x] 转换 / 历史 / 设置 / 关于模块
- [x] 上传区域（点击添加 + 拖拽）
- [x] 文件列表区域
- [x] 转换控制区域
- [x] 码率格式参数区域
- [x] 文件独立 Checkbox
- [x] 三态全选
- [x] 拖拽上传
- [x] 自动识别文件类型（音频 / 视频提取音轨）
- [x] 状态驱动 UI（等待/运行/完成/失败/取消/跳过）
- [x] 历史记录（最多 200 条）
- [x] 设置页面（六个分组）
- [x] Theme 统一（浅色、无渐变、低饱和）
- [x] Win7 兼容（net48 + manifest supportedOS + FFmpeg 7.0）
- [x] UI / Core 分层（UI 不直接操作 FFmpeg）

已通过端到端验证：WAV → MP3（320k CBR）真实转换成功，历史写入 `%APPDATA%\AudioConverter\history.json`。
