param(
    [string]$ExePath = "",
    [string]$CertPath = "",
    [string]$CertPassword = "",
    [switch]$CreateSelfSigned,
    [string]$TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"

if (-not $ExePath) {
    $ExePath = Join-Path $PSScriptRoot "..\artifacts\SkyFusion\SkyFusion.exe"
}

if (-not (Test-Path -LiteralPath $ExePath)) {
    throw "未找到待签名文件：$ExePath"
}

$signtool = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin" `
        -Recurse -Filter "signtool.exe" -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending |
    Select-Object -First 1

if (-not $signtool) {
    throw "未找到 signtool.exe，请先安装 Windows SDK（Windows SDK Signing Tools 即可）。"
}

$signArgs = @("sign", "/fd", "SHA256")

if ($CreateSelfSigned) {
    $store = "Cert:\CurrentUser\My"
    $thumb = (Get-ChildItem $store -CodeSigningCert |
        Where-Object Subject -like "*SkyFusion*" |
        Sort-Object NotAfter -Descending |
        Select-Object -First 1).Thumbprint

    if (-not $thumb) {
        $cert = New-SelfSignedCertificate `
            -Type CodeSigningCert `
            -Subject "CN=SkyFusion Development" `
            -CertStoreLocation $store `
            -KeyExportPolicy Exportable `
            -KeyUsage DigitalSignature `
            -NotAfter (Get-Date).AddYears(3)
        $thumb = $cert.Thumbprint
    }

    $certPath = Join-Path $env:TEMP "SkyFusion-selfsigned.pfx"
    Export-PfxCertificate `
        -Cert (Get-Item "$store\$thumb") `
        -FilePath $certPath `
        -Password (ConvertTo-SecureString "SkyFusionLocalSign" -AsPlainText -Force) `
        | Out-Null
    $signArgs += @("/f", $certPath, "/p", "SkyFusionLocalSign")
}
else {
    if (-not $CertPath -or -not (Test-Path -LiteralPath $CertPath)) {
        throw "请提供 .pfx 证书路径（或使用 -CreateSelfSigned 生成本地自签名证书）。"
    }

    $signArgs += @("/f", $CertPath)
    if ($CertPassword) {
        $signArgs += @("/p", $CertPassword)
    }
}

$signArgs += @("/tr", $TimestampUrl, "/td", "SHA256", $ExePath)

Write-Host "开始签名：$ExePath"
& $signtool.FullName @signArgs

$result = Get-AuthenticodeSignature -LiteralPath $ExePath
if ($result.Status -eq [System.Management.Automation.SignatureStatus]::Valid) {
    Write-Host "签名成功：$($result.SignerCertificate.Subject)"
}
else {
    Write-Warning "签名状态：$($result.Status) $($result.StatusMessage)"
}
