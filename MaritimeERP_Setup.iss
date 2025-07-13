[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{A1B2C3D4-E5F6-7890-ABCD-123456789012}
AppName=SEACURE(CARE)
AppVersion=1.0.0
AppVerName=SEACURE(CARE) 1.0.0
AppPublisher=Maritime Solutions
AppPublisherURL=https://www.maritimesolutions.com/
AppSupportURL=https://www.maritimesolutions.com/support
AppUpdatesURL=https://www.maritimesolutions.com/updates
DefaultDirName={autopf}\SEACURE(CARE)
DisableProgramGroupPage=yes
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir=Installer
OutputBaseFilename=SEACURE_CARE_Setup_v1.0.0
SetupIconFile=src\logo.svg
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 0,6.1

[Files]
; Main application files
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\MaritimeERP.Desktop.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\cs\*"; DestDir: "{app}\cs"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\de\*"; DestDir: "{app}\de"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\es\*"; DestDir: "{app}\es"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\fr\*"; DestDir: "{app}\fr"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\it\*"; DestDir: "{app}\it"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\ja\*"; DestDir: "{app}\ja"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\ko\*"; DestDir: "{app}\ko"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\pl\*"; DestDir: "{app}\pl"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\pt-BR\*"; DestDir: "{app}\pt-BR"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\ru\*"; DestDir: "{app}\ru"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\tr\*"; DestDir: "{app}\tr"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\zh-Hans\*"; DestDir: "{app}\zh-Hans"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\zh-Hant\*"; DestDir: "{app}\zh-Hant"; Flags: ignoreversion recursesubdirs createallsubdirs; Languages: 

; Sample database (will be copied to user data folder)
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\maritime_erp.db"; DestDir: "{app}\Database"; Flags: ignoreversion; Check: not FileExists(ExpandConstant('{userappdata}\SEACURE(CARE)\Database\maritime_erp.db'))

; Documentation
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "Documents\*"; DestDir: "{app}\Documentation"; Flags: ignoreversion recursesubdirs createallsubdirs

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\SEACURE(CARE)"; Filename: "{app}\MaritimeERP.Desktop.exe"
Name: "{autodesktop}\SEACURE(CARE)"; Filename: "{app}\MaritimeERP.Desktop.exe"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\SEACURE(CARE)"; Filename: "{app}\MaritimeERP.Desktop.exe"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\MaritimeERP.Desktop.exe"; Description: "{cm:LaunchProgram,SEACURE(CARE)}"; Flags: nowait postinstall skipifsilent

[Dirs]
Name: "{userappdata}\SEACURE(CARE)\Database"; Permissions: users-full
Name: "{userappdata}\SEACURE(CARE)\Documents"; Permissions: users-full
Name: "{userappdata}\SEACURE(CARE)\Logs"; Permissions: users-full

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
var
  SourcePath, DestPath: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Copy database to user data folder if it doesn't exist
    SourcePath := ExpandConstant('{app}\Database\maritime_erp.db');
    DestPath := ExpandConstant('{userappdata}\SEACURE(CARE)\Database\maritime_erp.db');
    
    if not FileExists(DestPath) and FileExists(SourcePath) then
    begin
      if not ForceDirectories(ExtractFileDir(DestPath)) then
      begin
        MsgBox('Could not create database directory: ' + ExtractFileDir(DestPath), mbError, MB_OK);
        Exit;
      end;
      
      if not FileCopy(SourcePath, DestPath, False) then
      begin
        MsgBox('Could not copy database to user data folder.', mbError, MB_OK);
      end;
    end;
    
    // Create custom appsettings.json for user
    CreateUserAppSettings();
  end;
end;

procedure CreateUserAppSettings();
var
  UserConfigPath: String;
  ConfigContent: String;
begin
  UserConfigPath := ExpandConstant('{userappdata}\SEACURE(CARE)\appsettings.json');
  
  if not FileExists(UserConfigPath) then
  begin
    ConfigContent := '{' + #13#10 +
      '  "ConnectionStrings": {' + #13#10 +
      '    "DefaultConnection": "Data Source=' + ExpandConstant('{userappdata}\SEACURE(CARE)\Database\maritime_erp.db') + '"' + #13#10 +
      '  },' + #13#10 +
      '  "Logging": {' + #13#10 +
      '    "LogLevel": {' + #13#10 +
      '      "Default": "Information",' + #13#10 +
      '      "Microsoft": "Warning",' + #13#10 +
      '      "Microsoft.Hosting.Lifetime": "Information"' + #13#10 +
      '    }' + #13#10 +
      '  },' + #13#10 +
      '  "Application": {' + #13#10 +
      '    "Name": "SEACURE(CARE)",' + #13#10 +
      '    "Version": "1.0.0",' + #13#10 +
      '    "CompanyName": "Maritime Solutions",' + #13#10 +
      '    "DocumentStoragePath": "' + ExpandConstant('{userappdata}\SEACURE(CARE)\Documents') + '",' + #13#10 +
      '    "MaxDocumentSizeMB": 50,' + #13#10 +
      '    "SupportedDocumentTypes": [ ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".png" ]' + #13#10 +
      '  },' + #13#10 +
      '  "Security": {' + #13#10 +
      '    "PasswordMinLength": 8,' + #13#10 +
      '    "PasswordRequireSpecialChar": true,' + #13#10 +
      '    "SessionTimeoutMinutes": 480,' + #13#10 +
      '    "MaxLoginAttempts": 5,' + #13#10 +
      '    "LockoutTimeMinutes": 30' + #13#10 +
      '  }' + #13#10 +
      '}';
    
    if not SaveStringToFile(UserConfigPath, ConfigContent, False) then
    begin
      MsgBox('Could not create user configuration file.', mbError, MB_OK);
    end;
  end;
end; 