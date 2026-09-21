$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
Push-Location $workspace
try {
  $refs=@('/r:System.dll','/r:System.Core.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Web.Extensions.dll')
  # /win32icon manquant ici depuis le debut : c'est pour ca que l'icone du .exe
  # disparaissait dans l'explorateur a chaque rebuild (seule l'icone de fenetre a
  # l'execution existait, pas l'icone du fichier). Meme icone que celle deja
  # presente dans le dossier deploye (Artifact_Manager_Application\app_icon.ico).
  $icon='/win32icon:C:\Users\Great-Lucky\Documents\Artifact_Manager_Application\app_icon.ico'
  New-Item -ItemType Directory -Force -Path outputs\artifact_manager_app | Out-Null
  & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:winexe $icon /out:outputs\artifact_manager_app\Artifact_Manager_Modern.exe $refs artifact_manager_app\ArtifactManagerApp.cs
  if($LASTEXITCODE -ne 0){throw 'Compilation application echouee'}
  & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe /out:outputs\artifact_manager_app\ArtifactManagerEngine.exe /r:System.dll /r:System.Core.dll /r:System.Web.Extensions.dll ArtifactManagerEngine.cs
  if($LASTEXITCODE -ne 0){throw 'Compilation moteur echouee'}
} finally {Pop-Location}
