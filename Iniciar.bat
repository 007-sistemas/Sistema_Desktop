@echo off
REM Script para iniciar o Sistema Biométrico
REM Navega até o diretório do executável Release
cd /d "%~dp0bin\Release\net8.0-windows\win-x64\"

REM Verifica se o arquivo de configuração existe
if not exist "appsettings.json" (
    echo Copiando appsettings.json...
    copy "..\..\..\appsettings.json" "appsettings.json"
)

REM Executa o sistema
echo Iniciando Sistema Biometrico...
start /B "" BiometricSystem.exe
exit /b %errorlevel%
