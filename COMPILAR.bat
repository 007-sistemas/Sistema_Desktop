@echo off
REM Script para compilar o projeto BiometricSystem
REM Tentará localizar e usar MSBuild ou dotnet

echo ========================================
echo COMPILACAO - Sistema Biometrico
echo ========================================
echo.

REM Verificar Visual Studio
echo Procurando compilador...
echo.

REM Tentar dotnet
where dotnet >nul 2>&1
if %errorlevel% equ 0 (
    echo Usando dotnet CLI...
    echo.
    dotnet build "BiometricSystem.csproj" -c Release
    
    if %errorlevel% equ 0 (
        echo.
        echo ========================================
        echo COMPILACAO CONCLUIDA COM SUCESSO!
        echo ========================================
        echo.
        echo Executavel gerado em:
        echo bin\Release\net8.0-windows\win-x64\BiometricSystem.exe
        echo.
    )
    goto :end
)

REM Nenhum compilador encontrado
echo ========================================
echo ERRO: Compilador nao encontrado!
echo ========================================
echo.
echo Para compilar o projeto:
echo.
echo OPCAO 1: Abra o Visual Studio
echo   1. Abra o arquivo BiometricSystem.csproj
echo   2. Pressione Ctrl+Shift+B
echo   3. OU clique em Build ^> Build Solution
echo.
echo OPCAO 2: Instale o .NET SDK
echo   1. Baixe de: https://dotnet.microsoft.com/download
echo   2. Instale o .NET 8.0 SDK
echo   3. Execute este script novamente
echo.

:end
pause
