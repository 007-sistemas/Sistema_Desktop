@echo off
REM Script AGRESSIVO para limpar completamente o leitor U.are.U
REM Requer ADMINISTRADOR e vai REINICIAR o computador

setlocal enabledelayedexpansion

echo ========================================
echo LIMPEZA AGRESSIVA - U.are.U 4500
echo ========================================
echo.
echo AVISO: Este script vai:
echo 1. Parar servicos
echo 2. Remover do Gerenciador de Dispositivos
echo 3. Limpar entradas do registro
echo 4. REINICIAR o computador
echo.
echo Execute como ADMINISTRADOR!
echo.

REM Verificar se é administrador
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERRO: Execute como ADMINISTRADOR!
    pause
    exit /b 1
)

echo [OK] Permissoes de administrador detectadas.
echo.
echo Pressione qualquer tecla para DESCONECTAR o leitor agora...
pause

echo.
echo Aguardando 3 segundos...
timeout /t 3 /nobreak

REM ETAPA 1: Parar servicos
echo.
echo [1/5] Parando servicos...
net stop WbioSrvc >nul 2>&1
sc config WbioSrvc start= disabled >nul 2>&1
taskkill /F /IM DPAgent.exe >nul 2>&1
taskkill /F /IM DPFP.exe >nul 2>&1
echo [OK] Servicos parados.

REM ETAPA 2: Desabilitar dispositivos via PowerShell
echo.
echo [2/5] Desabilitando dispositivos U.are.U...
powershell -NoProfile -Command "Get-PnpDevice | Where-Object { $_.FriendlyName -like '*U.are.U*' } | ForEach-Object { try { Disable-PnpDevice -InstanceId $_.InstanceId -Confirm:$false -ErrorAction SilentlyContinue } catch {} }" 2>nul
echo [OK] Dispositivos desabilitados.

REM ETAPA 3: Remover do registro (CUIDADO!)
echo.
echo [3/5] Removendo entradas do registro...
reg delete "HKLM\SYSTEM\CurrentControlSet\Enum\USB\VID_05BA" /f >nul 2>&1
if %errorlevel% equ 0 (
    echo [OK] Entrada VID_05BA removida.
) else (
    echo [AVISO] Nao foi possivel remover VID_05BA.
)

REM ETAPA 4: Remover drivers OEM
echo.
echo [4/5] Removendo drivers OEM...
for /f "tokens=*" %%A in ('pnputil /enum-drivers 2^>nul ^| findstr /i "05BA"') do (
    for /f "tokens=2" %%B in ('echo %%A') do (
        pnputil /delete-driver %%B /uninstall /force >nul 2>&1
    )
)
echo [OK] Drivers OEM processados.

REM ETAPA 5: Agendar restart
echo.
echo [5/5] Agendando reinicio do computador em 30 segundos...
echo.
echo ========================================
echo PROXIMAS ACOES:
echo ========================================
echo.
echo 1. O computador vai REINICIAR em 30 segundos
echo 2. Se quiser CANCELAR, digite: shutdown /a
echo 3. Apos reiniciar, RECONECTE o leitor
echo 4. Windows vai instalar driver novo
echo 5. Aguarde 2-3 minutos
echo 6. Execute DIAGNOSTICAR_LEITOR.bat
echo.
echo Clique em qualquer tecla para prosseguir com o restart...
pause

REM Restart
shutdown /s /t 0 /c "Sistema reiniciando para corrigir drivers U.are.U"
