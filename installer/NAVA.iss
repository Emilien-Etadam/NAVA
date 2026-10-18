#ifndef MyAppVersion
  #define MyAppVersion "0.1.0-fr.1"
#endif

#ifndef SourceDir
  #define SourceDir "..\artifacts\distribution\NAVA-0.1.0-fr.1-win-x64-portable"
#endif

#ifndef OutputDir
  #define OutputDir "..\artifacts\distribution"
#endif

#define MyAppName "NAVA"
#define MyAppPublisher "anko"
#define MyAppExeName "NAVA.exe"

[Setup]
AppId={{73FF0503-FF4A-4516-9A8C-09F514C28F9C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=NAVA-{#MyAppVersion}-win-x64-setup
SetupIconFile=..\src\RxV4A.Desktop\Assets\NAVA.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
CloseApplications=force
RestartApplications=no

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[CustomMessages]
french.CreateDesktopIcon=Créer un raccourci sur le bureau
french.AdditionalIcons=Raccourcis supplémentaires :
french.LaunchProgram=Lancer {#MyAppName}
english.CreateDesktopIcon=Create a desktop shortcut
english.AdditionalIcons=Additional shortcuts:
english.LaunchProgram=Launch {#MyAppName}

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram}"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  RegisteredCommand: String;
  InstalledCommand: String;
begin
  InstalledCommand := '"' + ExpandConstant('{app}\{#MyAppExeName}') + '"';
  if (CurUninstallStep = usUninstall) and
     RegQueryStringValue(HKEY_CURRENT_USER,
       'Software\Microsoft\Windows\CurrentVersion\Run', 'NAVA',
       RegisteredCommand) and
     (CompareText(RegisteredCommand, InstalledCommand) = 0) then
    RegDeleteValue(HKEY_CURRENT_USER,
      'Software\Microsoft\Windows\CurrentVersion\Run', 'NAVA');
end;
