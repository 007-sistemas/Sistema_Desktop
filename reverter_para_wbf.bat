@echo off
echo ========================================
echo REVERTER - Habilitar WBF Novamente
echo ========================================
echo.
echo Este script vai REVERTER as alteracoes e habilitar
echo o Windows Biometric Framework (WBF) novamente.
echo.
echo IMPORTANTE: Execute como ADMINISTRADOR!
echo.
pause

echo.
echo [1/2] Habilitando servico WbioSrvc...
sc config WbioSrvc start= auto
net start WbioSrvc

echo [2/2] Habilitando driver WBF do leitor...
powershell -Command "Get-PnpDevice | Where-Object { $_.FriendlyName -like '*U.are.U*WBF*' } | Enable-PnpDevice -Confirm:$false"

echo.
echo ========================================
echo CONCLUIDO!
echo ========================================
echo.
echo O Windows Biometric Framework foi reabilitado.
echo Desconecte e reconecte o leitor se necessario.
echo.
pause
