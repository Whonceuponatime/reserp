[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-123456789012}
AppName=SEACURE(CARE)
AppVersion=1.0.0
AppVerName=SEACURE(CARE) 1.0.0
AppPublisher=SEANET
AppPublisherURL=https://sea-net.co.kr
AppSupportURL=https://sea-net.co.kr
AppUpdatesURL=https://sea-net.co.kr
DefaultDirName={autopf}\SEACURE(CARE)
DisableProgramGroupPage=yes
OutputDir=Installer
OutputBaseFilename=SEACURE_CARE_Setup_v1.0.0_SingleFile
SetupIconFile=src\seacure_logo.ico
UninstallDisplayIcon={app}\seacure_logo.ico
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
; Single executable file (self-contained, includes appsettings.json embedded)
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\win-x64\publish\SEACURE_CARE.exe"; DestDir: "{app}"; Flags: ignoreversion
; Icon file for application and Programs and Features
Source: "src\seacure_logo.ico"; DestDir: "{app}"; Flags: ignoreversion
; Documentation
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "Documents\*"; DestDir: "{app}\Documentation"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\SEACURE(CARE)"; Filename: "{app}\SEACURE_CARE.exe"; IconFilename: "{app}\seacure_logo.ico"
Name: "{autodesktop}\SEACURE(CARE)"; Filename: "{app}\SEACURE_CARE.exe"; Tasks: desktopicon; IconFilename: "{app}\seacure_logo.ico"
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\SEACURE(CARE)"; Filename: "{app}\SEACURE_CARE.exe"; Tasks: quicklaunchicon; IconFilename: "{app}\seacure_logo.ico"

[Run]
Filename: "{app}\SEACURE_CARE.exe"; Description: "{cm:LaunchProgram,SEACURE(CARE)}"; Flags: nowait postinstall skipifsilent

[Dirs]
Name: "{userappdata}\SEACURE(CARE)\Database"; Permissions: users-full
Name: "{userappdata}\SEACURE(CARE)\Documents"; Permissions: users-full
Name: "{userappdata}\SEACURE(CARE)\Logs"; Permissions: users-full

[Code]
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
      '    "CompanyName": "SEANET",' + #13#10 +
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

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Create custom appsettings.json for user
    CreateUserAppSettings();
  end;
end; 