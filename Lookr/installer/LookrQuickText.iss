#ifndef MyAppVersion
#define MyAppVersion "1.0.0"
#endif

#ifndef MySourceDir
#define MySourceDir "..\\src\\LookrQuickText\\bin\\Release\\net8.0-windows10.0.19041.0\\win-x64\\publish"
#endif

#define MyAppName "Lookr QuickText"
#define MyAppPublisher "Lookr"
#define MyAppExeName "LookrQuickText.exe"

[Setup]
AppId={{A89AF27A-5155-4E1A-9D27-5CBEA08F40D4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={userappdata}\\Programs\\LookrQuickText
DefaultGroupName=Lookr QuickText
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=dist
OutputBaseFilename=LookrQuickText-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\\{#MyAppExeName}
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\\Lookr QuickText"; Filename: "{app}\\{#MyAppExeName}"
Name: "{autodesktop}\\Lookr QuickText"; Filename: "{app}\\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\\{#MyAppExeName}"; Description: "Launch Lookr QuickText"; Flags: nowait postinstall skipifsilent
