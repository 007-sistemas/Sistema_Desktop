[Setup]
AppName=BiometricSystem
AppVersion=1.0
DefaultDirName={pf}\BiometricSystem
DefaultGroupName=BiometricSystem
OutputDir=.
OutputBaseFilename=BiometricSystemSetup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

<<<<<<< HEAD
[Files]
Source: "..\bin\Release\net8.0-windows\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
=======

[Files]
; ATENÇÃO: Use a pasta win-x64 (NÃO a publish) para garantir que o instalador replique o ambiente funcional.
; NÃO inclua o arquivo biometric.db para que o sistema crie um banco limpo na primeira execução.
Source: "..\bin\Release\net8.0-windows\win-x64\*"; Excludes: "biometric.db,*.sqlite,*.sqlite3"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\bin\Release\net8.0-windows\win-x64\biometric - Limpo.db"; DestDir: "{app}"; DestName: "biometric.db"; Flags: ignoreversion
>>>>>>> baa86cf (Atualização do sistema: build limpo, banco limpo, config de publish e instalador ajustados)

[Icons]
Name: "{group}\BiometricSystem"; Filename: "{app}\BiometricSystem.exe"
Name: "{commondesktop}\BiometricSystem"; Filename: "{app}\BiometricSystem.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na Área de Trabalho"; GroupDescription: "Opções adicionais:"

[Run]
Filename: "{app}\BiometricSystem.exe"; Description: "Executar BiometricSystem"; Flags: nowait postinstall skipifsilent
<<<<<<< HEAD

[Code]
// Verifica se o .NET 8 Runtime está instalado
function IsDotNet8Installed(): Boolean;
var
  key: string;
begin
  key := 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft .NET Runtime - 8.0.0 (x64)';
  Result := RegKeyExists(HKLM, key);
end;

function DownloadAndInstallDotNet(): Boolean;
var
  DotNetUrl, DotNetInstaller: string;
  ResultCode: Integer;
begin
  DotNetUrl := 'https://download.visualstudio.microsoft.com/download/pr/7e7e1e2e-2e2e-4e2e-8e2e-2e2e2e2e2e2e/8.0.0-windowsdesktop-runtime.exe';
  DotNetInstaller := ExpandConstant('{tmp}\windowsdesktop-runtime.exe');
  if not FileExists(DotNetInstaller) then
  begin
    if not DownloadTemporaryFile(DotNetUrl, DotNetInstaller) then
    begin
      MsgBox('Falha ao baixar o instalador do .NET 8 Runtime. Baixe manualmente em https://dotnet.microsoft.com/download/dotnet/8.0', mbError, MB_OK);
      Result := False;
      exit;
    end;
  end;
  ShellExec('', DotNetInstaller, '/install', '', SW_SHOW, ResultCode);
  Result := False; // O usuário deve rodar o setup novamente após instalar o .NET
end;

function InitializeSetup(): Boolean;
begin
  if not IsDotNet8Installed() then
  begin
    MsgBox('O .NET 8 Runtime não foi encontrado. O instalador irá baixar e executar o instalador do .NET 8 Desktop Runtime.', mbInformation, MB_OK);
    DownloadAndInstallDotNet();
    Result := False;
  end
  else
    Result := True;
end;

// Garante permissões de escrita na pasta do app
var
  ResultCode: Integer;

procedure CurStepChanged(CurStep: TSetupStep);
    ; ATENÇÃO: Use a pasta win-x64 (NÃO a publish) para garantir que o instalador replique o ambiente funcional.
    ; NÃO inclua o arquivo biometric.db para que o sistema crie um banco limpo na primeira execução.
    Source: "..\bin\Release\net8.0-windows\win-x64\*"; Excludes: "biometric.db,*.sqlite,*.sqlite3"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
    Source: "..\bin\Release\net8.0-windows\win-x64\biometric - Limpo.db"; DestDir: "{app}"; DestName: "biometric.db"; Flags: ignoreversion
  if CurStep = ssPostInstall then
    ShellExec('', 'icacls', ExpandConstant('{app}') + ' /grant *S-1-1-0:(OI)(CI)F /T', '', SW_HIDE, ResultCode);
end;
=======
>>>>>>> baa86cf (Atualização do sistema: build limpo, banco limpo, config de publish e instalador ajustados)
