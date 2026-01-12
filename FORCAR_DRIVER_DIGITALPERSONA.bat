@echo off
REM Força o uso do driver legado DigitalPersona para U.are.U 4500
REM Execute como ADMINISTRADOR

echo ========================================
echo FORCAR DRIVER DIGITALPERSONA (LEGADO)
echo ========================================
echo.

net session >nul 2>&1
if %errorlevel% neq 0 (
  echo ERRO: Execute este script como ADMINISTRADOR.
  pause
  exit /b 1
)

echo [1/4] Desinstalando drivers WBF (CROSSMATCH)...
pnputil /delete-driver oem7.inf /uninstall /force
pnputil /delete-driver oem31.inf /uninstall /force

echo.
echo [2/4] Instalando driver DigitalPersona (dpersona_x64.inf)...
pnputil /add-driver "C:\Windows\System32\DriverStore\FileRepository\dpersona_x64.inf_amd64_d9a56a0c507c5e8f\dpersona_x64.inf" /install

echo.
echo [3/4] Desconecte e reconecte o leitor USB agora!
pause

echo.
echo [4/4] Verificando alteracoes de hardware...
pnputil /scan-devices

echo.
echo ========================================
echo CONCLUIDO
echo ========================================
echo.
echo Agora rode: DIAGNOSTICAR_LEITOR.bat

echo.
pause
