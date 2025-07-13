[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-123456789012}
AppName=SEACURE(CARE)
AppVersion=1.0.0
AppVerName=SEACURE(CARE) 1.0.0
AppPublisher=Maritime Solutions
DefaultDirName={autopf}\SEACURE(CARE)
DisableProgramGroupPage=yes
OutputDir=Installer
OutputBaseFilename=SEACURE_CARE_Setup_v1.0.0
SetupIconFile=src\seacure_logo.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Main application executable
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\MaritimeERP.Desktop.exe"; DestDir: "{app}"; Flags: ignoreversion
; All DLL files
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
; Configuration files
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\*.json"; DestDir: "{app}"; Flags: ignoreversion
; Runtime files
Source: "src\MaritimeERP.Desktop\bin\Release\net8.0-windows\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs
; Database file from root directory
Source: "maritime_erp.db"; DestDir: "{app}"; Flags: ignoreversion
; Icon file
Source: "src\seacure_logo.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\SEACURE(CARE)"; Filename: "{app}\MaritimeERP.Desktop.exe"; IconFilename: "{app}\seacure_logo.ico"
Name: "{autodesktop}\SEACURE(CARE)"; Filename: "{app}\MaritimeERP.Desktop.exe"; IconFilename: "{app}\seacure_logo.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\MaritimeERP.Desktop.exe"; Description: "{cm:LaunchProgram,SEACURE(CARE)}"; Flags: nowait postinstall skipifsilent 