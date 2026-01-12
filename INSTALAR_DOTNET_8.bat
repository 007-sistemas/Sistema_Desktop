@echo off
REM Script para instalar .NET 8.0 SDK e compilar o projeto

echo ========================================
echo INSTALACAO .NET 8.0 SDK
echo ========================================
echo.
echo O .NET 8.0 SDK nao foi encontrado no seu computador.
echo.
echo Para compilar e executar este projeto, voce precisa instalar:
echo .NET 8.0 SDK (Software Development Kit)
echo.
echo ========================================
echo OPCOES DE INSTALACAO:
echo ========================================
echo.
echo OPCAO 1: Instalador Web (Recomendado)
echo   Link: https://dotnet.microsoft.com/download/dotnet/8.0
echo.
echo   Passos:
echo   1. Abra o link acima no navegador
echo   2. Clique em ".NET 8.0 SDK" (Windows x64)
echo   3. Execute o instalador
echo   4. Reinicie o computador
echo   5. Execute este script novamente
echo.
echo OPCAO 2: Winget (Se instalado)
echo   Abra PowerShell ou Terminal e execute:
echo   winget install Microsoft.DotNet.SDK.8
echo.
echo OPCAO 3: Chocolate (Se instalado)
echo   choco install dotnet-8.0-sdk
echo.
echo ========================================
echo ALTERNATIVA: USAR VISUAL STUDIO
echo ========================================
echo.
echo Se voce tiver Visual Studio 2022 instalado:
echo   1. Abra Visual Studio
echo   2. Clique em "Open a project or solution"
echo   3. Abra: BiometricSystem.csproj
echo   4. Pressione Ctrl+Shift+B para compilar
echo.
echo ========================================
echo.
pause
