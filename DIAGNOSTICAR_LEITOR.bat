@echo off
REM Script para diagnosticar e testar o leitor U.are.U

setlocal enabledelayedexpansion

echo ========================================
echo DIAGNOSTICO - Leitor U.are.U 4500
echo ========================================
echo.

REM Verificar se o PowerShell está disponível
where powershell >nul 2>&1
if %errorlevel% neq 0 (
    echo ERRO: PowerShell nao encontrado!
    pause
    exit /b 1
)

echo [VERIFICACOES INICIADAS...]
echo.

REM 1. Verificar servico WBF
echo [1] Status do Servico WBF (Windows Biometric):
sc query WbioSrvc | find /i "STATE" >nul 2>&1
if %errorlevel% equ 0 (
    for /f "tokens=3" %%A in ('sc query WbioSrvc ^| find /i "STATE"') do (
        echo     Status: %%A
        if "%%A"=="RUNNING" (
            echo     AVISO: WBF ainda esta RODANDO! Pode bloquear o leitor.
        ) else if "%%A"=="STOPPED" (
            echo     OK: WBF desabilitado.
        )
    )
)

echo.

REM 2. Verificar se o leitor está conectado
echo [2] Verificando conexao do leitor U.are.U:
powershell -Command "Get-PnpDevice | Where-Object { $_.FriendlyName -like '*U.are.U*' } | Select-Object Status, FriendlyName" 2>nul | findstr /i "OK Error Unknown" >nul 2>&1

if %errorlevel% equ 0 (
    echo     DISPOSITIVO ENCONTRADO! Status:
    powershell -Command "$devs = Get-PnpDevice | Where-Object { $_.FriendlyName -like '*U.are.U*' }; foreach ($dev in $devs) { Write-Host ('     ' + $dev.Status + ' - ' + $dev.FriendlyName) }"
) else (
    echo     ERRO: Nenhum U.are.U encontrado!
    echo     Verifique se o leitor esta conectado via USB.
)

echo.

REM 3. Verificar se DigitalPersona SDK está instalado
echo [3] Verificando DigitalPersona SDK:
if exist "C:\Program Files\DigitalPersona\Bin" (
    echo     OK: DigitalPersona instalado em: C:\Program Files\DigitalPersona\Bin
    dir "C:\Program Files\DigitalPersona\Bin\*.dll" 2>nul | find /c /v "" >nul 2>&1
    if %errorlevel% equ 0 (
        echo     DLLs encontradas: Sim
    )
) else (
    echo     ERRO: DigitalPersona nao encontrado!
    echo     Reinstale o DigitalPersona SDK.
)

echo.

REM 4. Verificar biblioteca SDK do .NET
echo [4] Verificando DLLs do SDK no projeto:
if exist "SDK\DPFPEngNET.dll" (
    echo     OK: DLLs do SDK presentes na pasta do projeto.
) else (
    echo     AVISO: DLLs do SDK nao encontradas em SDK\
)

echo.

REM 5. Verificar permissoes USB
echo [5] Verificando permissoes de acesso USB:
powershell -Command "try { $devs = Get-PnpDevice -ErrorAction Stop | Where-Object { $_.FriendlyName -like '*U.are.U*' }; Write-Host '     OK: Acesso ao USB funcionando' } catch { Write-Host '     ERRO: Problema ao acessar dispositivos USB' }" 2>nul

echo.

REM Resumo final
echo ========================================
echo RESUMO DO DIAGNOSTICO
echo ========================================
echo.
echo Se aparecer:
echo   [OK] - Tudo certo nessa verificacao
echo   [AVISO] - Pode causar problema
echo   [ERRO] - Precisa ser corrigido
echo.
echo Se todos os itens mostrarem [OK], o problema esta
echo no codigo da aplicacao, nao no hardware/driver.
echo.
pause
