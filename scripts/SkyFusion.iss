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
Source: "..\dist\SkyFusion-Win7-Audio\运行库（Win7需安装）\NDP48-x86-x64-AllOS-ENU (1).exe"; DestDir: "{app}\运行库"; DestName: "NDP48-x86-x64-AllOS-ENU.exe"; Flags: ignoreversion

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
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  resultCode: Integer;
  installerPath: string;
begin
  if CurStep <> ssPostInstall then
    Exit;

  if IsDotNet48Installed() then
    Exit;

  installerPath := ExpandConstant('{app}\运行库\NDP48-x86-x64-AllOS-ENU.exe');
  if not FileExists(installerPath) then
    Exit;

  if MsgBox('SkyFusion needs Microsoft .NET Framework 4.8. ' + #13#10 +
            'Do you want to install it automatically now?' + #13#10 +
            'Yes = one-click install; No = open installer manually.',
            mbConfirmation, MB_YESNO) = IDYES then
  begin
    if Exec(installerPath, '/q /norestart', '', SW_SHOW, ewWaitUntilTerminated, resultCode) then
    begin
      if IsDotNet48Installed() then
        MsgBox('.NET Framework 4.8 has been installed successfully.', mbInformation, MB_OK)
      else
        MsgBox('The installation may need a restart. Please restart the computer and then run SkyFusion.',
               mbInformation, MB_OK);
    end
    else
      MsgBox('Automatic installation failed. Please install .NET Framework 4.8 manually.',
             mbError, MB_OK);
  end
  else
  begin
    ShellExec('', installerPath, '', '', SW_SHOW, ewWaitUntilTerminated, resultCode);
    if IsDotNet48Installed() then
      MsgBox('.NET Framework 4.8 has been installed.', mbInformation, MB_OK)
    else
      MsgBox('If you did not finish installing, please install .NET Framework 4.8 and restart before running SkyFusion.',
             mbInformation, MB_OK);
  end;
end;
