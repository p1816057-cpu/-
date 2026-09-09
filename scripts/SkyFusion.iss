#define MyAppName "SkyFusion"
#define MyAppVersion "1.1.0"
#define MyAppExeName "SkyFusion.exe"

[Setup]
AppId={{A9F21C18-4B2A-4E5D-BB3A-C3A62A77F1C8}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=SkyFusion
DefaultDirName={autopf}\SkyFusion
DefaultGroupName=SkyFusion
DisableProgramGroupPage=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
OutputDir=..\dist
OutputBaseFilename=SkyFusion-Setup-1.1.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\SkyFusion.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "..\dist\SkyFusion-Win7-Audio\SkyFusion.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\SkyFusion-Win7-Audio\SkyFusion.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\SkyFusion-Win7-Audio\使用说明.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\SkyFusion-Win7-Audio\FFmpeg-LICENSE.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\SkyFusion-Win7-Audio\FFmpeg-README.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\dist\SkyFusion-Win7-Audio\ffmpeg\bin\ffmpeg.exe"; DestDir: "{app}\ffmpeg\bin"; Flags: ignoreversion
Source: "..\dist\SkyFusion-Win7-Audio\ffmpeg\bin\ffprobe.exe"; DestDir: "{app}\ffmpeg\bin"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\SkyFusion"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\SkyFusion"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Code]
function IsDotNet48Installed(): Boolean;
var
  release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', release);
  if Result then
    Result := release >= 528040;
end;

function InitializeSetup(): Boolean;
begin
  if not IsDotNet48Installed() then
  begin
    MsgBox('This software requires Microsoft .NET Framework 4.8. ' +
           'Please install .NET Framework 4.8 first (Windows 7 SP1 users can download the offline installer from Microsoft), then run this setup again.',
           mbError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;
