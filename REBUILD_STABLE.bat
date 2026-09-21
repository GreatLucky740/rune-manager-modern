@echo off
cd /d "%~dp0"
echo Build stable (appli principale)...
powershell -NoProfile -ExecutionPolicy Bypass -File "tools\build_rune_update.ps1" -Stable
if %ERRORLEVEL% NEQ 0 (
  echo.
  echo *** BUILD ECHOUE ***
) else (
  echo.
  echo OK -^> outputs\Rune_Manager_Principal_Update.exe
)
pause
