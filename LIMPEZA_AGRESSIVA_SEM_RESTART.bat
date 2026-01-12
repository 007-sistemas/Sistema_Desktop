@echo off
REM Script AGRESSIVO SEM RESTART - para quem prefere nao reiniciar imediatamente

setlocal enabledelayedexpansion

echo ========================================
echo LIMPEZA AGRESSIVA (SEM RESTART)
echo ========================================
echo.
echo AVISO: Este script vai:
echo 1. Parar todos os servicos do leitor
echo 2. Desabilitar todos os dispositivos U.are.U
echo 3. Limpar entradas do registro
echo 4. NAO vai reiniciar (mas RECOMENDADO)
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
echo AGORA:
echo 1. Desconecte o leitor USB
echo 2. Pressione qualquer tecla para continuar
pause

echo.
echo Aguardando 3 segundos...
timeout /t 3 /nobreak

REM ETAPA 1: Parar servicos
echo.
echo [1/4] Parando servicos...
net stop WbioSrvc >nul 2>&1
sc config WbioSrvc start= disabled >nul 2>&1
taskkill /F /IM DPAgent.exe >nul 2>&1
taskkill /F /IM DPFP.exe >nul 2>&1
echo [OK] Servicos parados.

REM ETAPA 2: Desabilitar dispositivos
echo.
echo [2/4] Desabilitando dispositivos U.are.U...
powershell -NoProfile -Command "Get-PnpDevice | Where-Object { $_.FriendlyName -like '*U.are.U*' -and $_.Status -ne 'Unknown' } | ForEach-Object { try { Disable-PnpDevice -InstanceId $_.InstanceId -Confirm:$false -ErrorAction SilentlyContinue; Write-Host 'Desabilitado: ' $_.FriendlyName } catch { Write-Host 'Erro ao desabilitar: ' $_.FriendlyName } }" 2>nul
echo [OK] Dispositivos desabilitados.

REM ETAPA 3: Remover do registro
echo.
echo [3/4] Removendo entradas de registro...
reg delete "HKLM\SYSTEM\CurrentControlSet\Enum\USB\VID_05BA&PID_000A" /f >nul 2>&1
if %errorlevel% equ 0 (
    echo [OK] Entrada removida.
) else (
    echo [AVISO] Entrada ja nao existe ou protegida.
)

REM ETAPA 4: Varrer hardware novo
echo.
echo [4/4] Varredura de novo hardware...
powershell -NoProfile -Command "pnputil /scan-devices" 2>nul
echo [OK] Varredura concluida.

echo.
echo ========================================
echo CONCLUIDO!
echo ========================================
echo.
echo Proximas acoes:
echo.
echo 1. ALTAMENTE RECOMENDADO: Reiniciar o computador
echo    Digite: shutdown /s /t 60
echo.
echo 2. Se nao quiser reiniciar, siga:
echo    a) Aguarde 10 segundos
echo    b) RECONECTE o leitor
echo    c) Aguarde Windows instalar driver (2-3 min)
echo    d) Execute DIAGNOSTICAR_LEITOR.bat
echo.
echo IMPORTANTE: Se nao funcionar, RESTART e repita!
echo.
pause
