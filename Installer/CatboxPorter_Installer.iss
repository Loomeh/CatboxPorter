[Setup]
AppId={{E9F7D0C3-2F3B-4F1C-9A5C-6B1A23C9F3A7}}
AppName=CatboxPorter
AppVersion=1.0.0
AppVerName=CatboxPorter 1.0.0
AppPublisher=Loomeh
AppPublisherURL=https://github.com/Loomeh/CatboxPorter
AppCopyright=Loomeh
DefaultDirName={localappdata}\Programs\CatboxPorter
DefaultGroupName=CatboxPorter
UninstallDisplayIcon={app}\CatboxPorter.exe
OutputBaseFilename=Install_CatboxPorter
OutputDir=.
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0
LicenseFile="..\LICENSE"
SetupIconFile=..\CatboxPorter\res\catboxporter.ico
UsePreviousAppDir=yes
DisableProgramGroupPage=yes
AllowNoIcons=yes
CloseApplications=yes
RestartApplications=no

[Files]
Source: "..\CatboxPorter\bin\x64\Release\net9.0-windows10.0.20348.0\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Registry]
; Context menu: "Upload to Catbox" for all files (standard upload)
Root: HKCU; SubKey: "Software\Classes\*\shell\CatboxPorterUpload"; ValueType: string; ValueName: "MUIVerb"; ValueData: "Upload to Catbox"; Flags: uninsdeletekey
Root: HKCU; SubKey: "Software\Classes\*\shell\CatboxPorterUpload"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\CatboxPorter.exe"
Root: HKCU; SubKey: "Software\Classes\*\shell\CatboxPorterUpload\command"; ValueType: string; ValueName: ""; ValueData: """{app}\CatboxPorter.exe"" ""%1"""; Flags: uninsdeletekey

; Context menu: "Upload to Catbox (Discord)" ONLY for video files
; .mp4
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.mp4\shell\CatboxPorterDiscord"; ValueType: string; ValueName: "MUIVerb"; ValueData: "Upload to Catbox (Discord)"; Flags: uninsdeletekey
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.mp4\shell\CatboxPorterDiscord"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\CatboxPorter.exe"
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.mp4\shell\CatboxPorterDiscord\command"; ValueType: string; ValueName: ""; ValueData: """{app}\CatboxPorter.exe"" ""%1"" discord"; Flags: uninsdeletekey

; .webm
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.webm\shell\CatboxPorterDiscord"; ValueType: string; ValueName: "MUIVerb"; ValueData: "Upload to Catbox (Discord)"; Flags: uninsdeletekey
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.webm\shell\CatboxPorterDiscord"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\CatboxPorter.exe"
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.webm\shell\CatboxPorterDiscord\command"; ValueType: string; ValueName: ""; ValueData: """{app}\CatboxPorter.exe"" ""%1"" discord"; Flags: uninsdeletekey

; .mkv
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.mkv\shell\CatboxPorterDiscord"; ValueType: string; ValueName: "MUIVerb"; ValueData: "Upload to Catbox (Discord)"; Flags: uninsdeletekey
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.mkv\shell\CatboxPorterDiscord"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\CatboxPorter.exe"
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.mkv\shell\CatboxPorterDiscord\command"; ValueType: string; ValueName: ""; ValueData: """{app}\CatboxPorter.exe"" ""%1"" discord"; Flags: uninsdeletekey

; .avi
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.avi\shell\CatboxPorterDiscord"; ValueType: string; ValueName: "MUIVerb"; ValueData: "Upload to Catbox (Discord)"; Flags: uninsdeletekey
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.avi\shell\CatboxPorterDiscord"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\CatboxPorter.exe"
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.avi\shell\CatboxPorterDiscord\command"; ValueType: string; ValueName: ""; ValueData: """{app}\CatboxPorter.exe"" ""%1"" discord"; Flags: uninsdeletekey

; .mov
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.mov\shell\CatboxPorterDiscord"; ValueType: string; ValueName: "MUIVerb"; ValueData: "Upload to Catbox (Discord)"; Flags: uninsdeletekey
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.mov\shell\CatboxPorterDiscord"; ValueType: string; ValueName: "Icon"; ValueData: "{app}\CatboxPorter.exe"
Root: HKCU; SubKey: "Software\Classes\SystemFileAssociations\.mov\shell\CatboxPorterDiscord\command"; ValueType: string; ValueName: ""; ValueData: """{app}\CatboxPorter.exe"" ""%1"" discord"; Flags: uninsdeletekey

[UninstallDelete]
Type: files; Name: "{app}\ffmpeg.exe"
Type: files; Name: "{app}\ffprobe.exe"

[Code]
var
  DownloadPage: TDownloadWizardPage;

const
  SevenZipUrl = 'https://www.7-zip.org/a/7zr.exe';
  FfmpegUrl   = 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.7z';

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log(Format('Successfully downloaded file to {tmp}: %s', [FileName]));
  Result := True;
end;

procedure InitializeWizard;
begin
  DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), @OnDownloadProgress);
end;

function ExecAndCheck(const FileName, Params: string): Boolean;
var
  ResultCode: Integer;
begin
  Log(Format('Executing: %s %s', [FileName, Params]));
  Result := Exec(FileName, Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  if not Result then
  begin
    Log(Format('Failed to execute: %s', [FileName]));
    exit;
  end;

  Log(Format('Process exit code: %d', [ResultCode]));
  Result := (ResultCode = 0);
end;

function EnsureFfmpegPresent: Boolean;
var
  TmpDir, AppDir, SevenZipExe, ArchivePath, ExtractDir: string;
  CopiedOK: Boolean;
begin
  AppDir := ExpandConstant('{app}');
  TmpDir := ExpandConstant('{tmp}');
  SevenZipExe := AddBackslash(TmpDir) + '7zr.exe';
  ArchivePath := AddBackslash(TmpDir) + 'ffmpeg-release-essentials.7z';
  ExtractDir := AddBackslash(TmpDir) + 'ffmpeg_extract';

  if FileExists(AddBackslash(AppDir) + 'ffmpeg.exe') and FileExists(AddBackslash(AppDir) + 'ffprobe.exe') then
  begin
    Log('ffmpeg binaries already present; skipping download.');
    Result := True;
    exit;
  end;

  DownloadPage.Clear;
  DownloadPage.Add(SevenZipUrl, '7zr.exe', '');
  DownloadPage.Add(FfmpegUrl, 'ffmpeg-release-essentials.7z', '');
  DownloadPage.Show;

  try
    try
      DownloadPage.Download;

      if DirExists(ExtractDir) then
      begin
        Log('Cleaning previous extract dir: ' + ExtractDir);
        DelTree(ExtractDir, True, True, True);
      end;
      CreateDir(ExtractDir);

      if not FileExists(SevenZipExe) then
      begin
        SuppressibleMsgBox('7zr.exe was not downloaded correctly.', mbCriticalError, MB_OK, IDOK);
        Result := False;
        exit;
      end;

      if not FileExists(ArchivePath) then
      begin
        SuppressibleMsgBox('ffmpeg 7z archive was not downloaded correctly.', mbCriticalError, MB_OK, IDOK);
        Result := False;
        exit;
      end;

      if not ExecAndCheck(SevenZipExe, 'e -y -o"' + ExtractDir + '" "' + ArchivePath + '" "*\bin\ffmpeg.exe" "*\bin\ffprobe.exe"') then
      begin
        SuppressibleMsgBox('Failed to extract ffmpeg binaries from the archive.', mbCriticalError, MB_OK, IDOK);
        Result := False;
        exit;
      end;

      if not DirExists(AppDir) then
        CreateDir(AppDir);

      CopiedOK := True;

      if FileExists(AddBackslash(ExtractDir) + 'ffmpeg.exe') then
      begin
        if not FileCopy(AddBackslash(ExtractDir) + 'ffmpeg.exe', AddBackslash(AppDir) + 'ffmpeg.exe', False) then
        begin
          Log('Failed to copy ffmpeg.exe');
          CopiedOK := False;
        end;
      end
      else
      begin
        Log('ffmpeg.exe not found in extract dir.');
        CopiedOK := False;
      end;

      if FileExists(AddBackslash(ExtractDir) + 'ffprobe.exe') then
      begin
        if not FileCopy(AddBackslash(ExtractDir) + 'ffprobe.exe', AddBackslash(AppDir) + 'ffprobe.exe', False) then
        begin
          Log('Failed to copy ffprobe.exe');
          CopiedOK := False;
        end;
      end
      else
      begin
        Log('ffprobe.exe not found in extract dir.');
        CopiedOK := False;
      end;

      if not CopiedOK then
      begin
        SuppressibleMsgBox('Failed to copy ffmpeg binaries to the application directory.', mbCriticalError, MB_OK, IDOK);
        Result := False;
        exit;
      end;

      Result := True;
    except
      SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK, IDOK);
      Result := False;
    end;
  finally
    if FileExists(ArchivePath) then
      DeleteFile(ArchivePath);
    if FileExists(SevenZipExe) then
      DeleteFile(SevenZipExe);
    if DirExists(ExtractDir) then
      DelTree(ExtractDir, True, True, True);

    DownloadPage.Hide;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  if CurPageID = wpReady then
  begin
    Result := EnsureFfmpegPresent;
  end
  else
    Result := True;
end;