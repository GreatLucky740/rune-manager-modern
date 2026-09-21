@echo off
cd /d "%~dp0"
echo Build test (Sigmarus / experimental v9)...
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\build_rune_update.ps1"
if %ERRORLEVEL% NEQ 0 (
  echo.
  echo *** BUILD ECHOUE ***
) else (
  echo.
  echo OK -^> outputs\Rune_Manager_Modern_Core_Test.exe
)
pause
