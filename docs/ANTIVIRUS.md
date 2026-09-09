# 防病毒误报处理

## 现状

Windows Defender 曾将 `SkyFusion.exe` 识别为：

```text
Trojan:Win32/Bearfoos.A!ml
```

`!ml` 结尾表示该结果来自 Microsoft Defender 的**机器学习启发式**，不是传统特征库命中。对新编译、**未签名**、会启动外部进程（FFmpeg）的桌面程序，这类误报比较常见。

本程序的行为仅为：启动本地 `ffmpeg.exe / ffprobe.exe`、读写 `%APPDATA%\AudioConverter` 下的设置/历史 JSON，无自更新、无注入、无网络行为。

## 根治方案（推荐）

1. 使用正规代码签名证书（OV/EV Code Signing）对每次发布签名；
2. 发布到 GitHub Releases，通过长期信誉降低误报；
3. 每次新版本被误报后，向 Microsoft 提交误报申诉：

   <https://www.microsoft.com/en-us/wdsi/filesubmission>

   提交时选择 “Microsoft Defender Antivirus 误报”，上传 `SkyFusion.exe` 并说明这是离线音频格式转换工具，调用本机 FFmpeg。

## 临时方案

### 开发机排除目录（仅本机开发用）

```powershell
Add-MpPreference -ExclusionPath "D:\文档\ChatGPT\天融格式转换软件"
```

不要给最终用户使用排除方式，用户端应通过正式签名解决。

### 自签名（仅本地测试）

```powershell
powershell -ExecutionPolicy Bypass -File scripts\sign-release.ps1 -CreateSelfSigned
```

自签名无法让最终用户完全消除 SmartScreen / Defender 提示，只适合本地验证签名链路。正式发布仍应使用受信任的代码签名证书。

## 签名自动化

有 `.pfx` 证书后：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\sign-release.ps1 `
  -CertPath "D:\certs\skyfusion.pfx" `
  -CertPassword "密码"
```

需要先安装 Windows SDK Signing Tools 提供 `signtool.exe`。
