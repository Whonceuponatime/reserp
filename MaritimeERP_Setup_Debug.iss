[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-123456789012}
AppName=SEACURE(CARE) Debug
AppVersion=1.0.0
AppVerName=SEACURE(CARE) 1.0.0 Debug
AppPublisher=SEANET
AppPublisherURL=https://sea-net.co.kr
AppSupportURL=https://sea-net.co.kr
AppUpdatesURL=https://sea-net.co.kr
DefaultDirName={autopf}\SEACURE(CARE) Debug
DisableProgramGroupPage=yes
OutputDir=Installer
OutputBaseFilename=SEACURE_CARE_Setup_v1.0.0_DEBUG
SetupIconFile=src\seacure_logo.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkablealone
Name: "debugicon"; Description: "Create debug shortcut (shows console)"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkablealone

[Files]
; Single executable file (self-contained, includes appsettings.json embedded)
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\win-x64\publish\MaritimeERP.Desktop.exe"; DestDir: "{app}"; Flags: ignoreversion
; Icon file
Source: "src\seacure_logo.ico"; DestDir: "{app}"; Flags: ignoreversion
; Documentation
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "Documents\*"; DestDir: "{app}\Documentation"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\SEACURE(CARE) Debug"; Filename: "{app}\MaritimeERP.Desktop.exe"; Parameters: "--debug"; IconFilename: "{app}\seacure_logo.ico"
Name: "{autodesktop}\SEACURE(CARE)"; Filename: "{app}\MaritimeERP.Desktop.exe"; Tasks: desktopicon; IconFilename: "{app}\seacure_logo.ico"
Name: "{autodesktop}\SEACURE(CARE) Debug"; Filename: "{app}\MaritimeERP.Desktop.exe"; Parameters: "--debug"; Tasks: debugicon; IconFilename: "{app}\seacure_logo.ico"; Comment: "Run with debug console"

[Run]
Filename: "{app}\MaritimeERP.Desktop.exe"; Parameters: "--debug"; Description: "Launch SEACURE(CARE) with debug console"; Flags: nowait postinstall skipifsilent

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