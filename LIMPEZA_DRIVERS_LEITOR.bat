@echo off
REM Script para limpar completamente os drivers do U.are.U e forçar reinstalação
REM Executar como ADMINISTRADOR

setlocal enabledelayedexpansion

echo ========================================
echo LIMPEZA COMPLETA - Leitor U.are.U
echo ========================================
echo.
echo Este script vai:
echo 1. Desabilitar o servico WBF (se ainda estiver ativo)
echo 2. Limpar drivers corrompidos
echo 3. Forcar deteccao de novo hardware
echo.
echo IMPORTANTE: Execute como ADMINISTRADOR!
echo.

REM Verificar se é administrador
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERRO: Este script requer permissoes de ADMINISTRADOR!
    echo.
    echo Como executar como administrador:
    echo 1. Clique com botao direito neste arquivo
    echo 2. Selecione "Executar como administrador"
    echo.
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

echo.
echo [1/4] Parando servico WbioSrvc...
sc stop WbioSrvc >nul 2>&1
sc config WbioSrvc start= disabled >nul 2>&1
echo [OK] Servico WBF parado e desabilitado.

echo.
echo [2/4] Procurando entrada de registro do leitor...
REM Remover do registro - Este comando remove os drivers
reg delete "HKLM\SYSTEM\CurrentControlSet\Enum\USB\VID_05BA&PID_000A" /f >nul 2>&1
if %errorlevel% equ 0 (
    echo [OK] Entrada do registro removida.
) else (
    echo [ATENCAO] Nao foi possivel remover do registro (pode estar em uso).
)

echo.
echo [3/4] Removendo driver OEM se encontrado...
REM Listar drivers OEM e remover se for do DigitalPersona
for /f "tokens=2" %%A in ('pnputil /enum-drivers 2^>nul ^| findstr /i "digital persona u.are"') do (
    echo Removendo driver: %%A
    pnputil /delete-driver %%A /uninstall /force >nul 2>&1
)

echo [OK] Drivers OEM processados.

echo.
echo ========================================
echo FASE 1 CONCLUIDA!
echo ========================================
echo.
echo Agora faca o seguinte:
echo.
echo 1. AGUARDE 10 segundos
echo 2. RECONECTE o leitor USB na porta
echo 3. O Windows vai detectar e instalar o driver automaticamente
echo.
echo (Se o Windows pedir drivers, aponte para:
echo  C:\Program Files\DigitalPersona\*)
echo.
echo Quando terminar, execute DIAGNOSTICAR_LEITOR.bat para validar.
echo.
pause
