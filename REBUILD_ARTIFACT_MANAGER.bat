@echo off
set WORKSPACE=C:\Users\Great-Lucky\Documents\Codex\2026-08-09\referenced-chatgpt-conversation-this-is-an-2
set TARGET=C:\Users\Great-Lucky\Documents\Artifact_Manager_Application
cd /d "%WORKSPACE%"
echo Build Artifact Manager...
echo IMPORTANT : ferme l'appli Artifact Manager avant de continuer si elle tourne.
powershell -NoProfile -ExecutionPolicy Bypass -File "%WORKSPACE%\tools\build_artifact_manager.ps1"
if %ERRORLEVEL% NEQ 0 (
  echo.
  echo *** BUILD ECHOUE ***
  pause
  exit /b 1
)
echo.
echo Build OK. Deploiement dans Artifact_Manager_Application...
if exist "%TARGET%\Artifact_Manager_Modern.exe" copy /Y "%TARGET%\Artifact_Manager_Modern.exe" "%TARGET%\Artifact_Manager_Modern.backup-lastbuild.exe" >nul
if exist "%TARGET%\ArtifactManagerEngine.exe" copy /Y "%TARGET%\ArtifactManagerEngine.exe" "%TARGET%\ArtifactManagerEngine.backup-lastbuild.exe" >nul
copy /Y "%WORKSPACE%\outputs\artifact_manager_app\Artifact_Manager_Modern.exe" "%TARGET%\Artifact_Manager_Modern.exe" >nul
copy /Y "%WORKSPACE%\outputs\artifact_manager_app\ArtifactManagerEngine.exe" "%TARGET%\ArtifactManagerEngine.exe" >nul
if %ERRORLEVEL% NEQ 0 (
  echo *** COPIE ECHOUEE - ferme l'appli si elle tourne encore et relance ce .bat ***
) else (
  echo OK -^> deploye dans %TARGET%
)
pause
