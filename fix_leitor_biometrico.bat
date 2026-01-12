@echo off
echo ========================================
echo FIX - Leitor Biometrico DigitalPersona
echo ========================================
echo.
echo Este script vai:
echo 1. Parar o servico Windows Biometric Framework (WBF)
echo 2. Desabilitar o servico WBF
echo 3. Desabilitar o driver WBF do leitor
echo.
echo IMPORTANTE: Execute como ADMINISTRADOR!
echo.
pause

echo.
echo [1/3] Parando servico WbioSrvc...
net stop WbioSrvc
if %errorlevel% neq 0 (
    echo ERRO: Nao foi possivel parar o servico. Execute como ADMINISTRADOR!
    pause
    exit /b 1
)

echo [2/3] Desabilitando servico WbioSrvc...
sc config WbioSrvc start= disabled
if %errorlevel% neq 0 (
    echo ERRO: Nao foi possivel desabilitar o servico.
    pause
    exit /b 1
)

echo [3/3] Desabilitando driver WBF do leitor...
powershell -Command "Get-PnpDevice | Where-Object { $_.FriendlyName -like '*U.are.U*WBF*' } | Disable-PnpDevice -Confirm:$false"

echo.
echo ========================================
echo CONCLUIDO COM SUCESSO!
echo ========================================
echo.
echo Agora:
echo 1. Desconecte o leitor USB
echo 2. Aguarde 5 segundos
echo 3. Conecte o leitor novamente
echo 4. Execute seu sistema biometrico
echo.
echo O Windows vai instalar o driver legado do DigitalPersona.
echo.
pause
