param([switch]$Stable)
$ErrorActionPreference='Stop'
$workspace=Split-Path -Parent $PSScriptRoot
Push-Location $workspace
try {
  $refs=@('/r:System.dll','/r:System.Core.dll','/r:System.Drawing.dll','/r:System.Windows.Forms.dll','/r:System.Web.Extensions.dll','/r:System.Runtime.Serialization.dll','/r:System.Net.Http.dll','/r:C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System.Runtime.WindowsRuntime.dll','/r:outputs\rune_manager_release\Donnees\Tesseract.dll')
  $refs+=Get-ChildItem 'C:\Windows\System32\WinMetadata' -Filter '*.winmd'|ForEach-Object {'/r:'+$_.FullName}
  foreach($name in @('System.Runtime','System.Runtime.InteropServices.WindowsRuntime','System.ObjectModel')){$refs+=Get-ChildItem ('C:\Windows\Microsoft.NET\assembly\GAC_MSIL\'+$name) -Recurse -Filter '*.dll'|ForEach-Object {'/r:'+$_.FullName}}
  $src=@('Loc','RuneManagerApp','RuneEngine','RuneEnhancements','WorldBossOptimizer','RtaDraftAdvisor','RtaMetaRefresh','RtaPoolTracker','RtaBuildOptimizer','RtaTargetEditor','ScreenCaptureLens','SwGameCodes')|ForEach-Object {'rune_manager_app\'+$_+'.cs'}
  $src+='rune_manager_app\WorldBossStableBridge.cs'
  $src+='rune_manager_app\PresetMenus.cs'
  $src+='rune_manager_app\PremiumRuneArt.cs'
  $refs+=Get-ChildItem 'rune_manager_app\assets\runes\detailed-v1' -Filter 'glyph-*.png'|ForEach-Object {'/resource:'+$_.FullName+',RuneArt.'+$_.Name}
  $flavor=if($Stable){@('/define:WORLD_BOSS_STABLE','/out:outputs\Rune_Manager_Principal_Update.exe')}else{@('/out:outputs\Rune_Manager_Modern_Core_Test.exe')}
  # Icone embarquee dans l'exe (barre des taches / raccourci / explorateur avant meme
  # le lancement). Sans /win32icon, seule l'icone de fenetre a l'execution (Icon=LoadAppIcon())
  # est correcte ; le fichier .exe lui-meme reste generique tant qu'il n'est pas ouvert.
  $icon='/win32icon:rune_manager_app\assets\app_icon.ico'
  & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:winexe $icon $flavor $refs $src
  if($LASTEXITCODE -ne 0){throw 'Compilation application failed'}
  & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:exe /main:RuneUpdateRegressionTest /out:outputs\RuneUpdateRegressionTest.exe $refs $src rune_manager_app\RuneUpdateRegressionTest.cs
  if($LASTEXITCODE -ne 0){throw 'Compilation tests failed'}
  if($Stable){
    & 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe' /nologo /target:winexe $icon /r:System.dll /r:System.Windows.Forms.dll /out:outputs\Rune_Manager_Update.exe rune_manager_app\RuneManagerUpdater.cs
    if($LASTEXITCODE -ne 0){throw 'Compilation updater failed'}
    Copy-Item -LiteralPath 'outputs\Rune_Manager_Principal_Update.exe' -Destination 'outputs\Rune_Manager_Modern_Core.exe' -Force
  }
} finally {Pop-Location}
