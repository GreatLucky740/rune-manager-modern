$ErrorActionPreference='Stop'
$directory='C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees'
$target=Join-Path $directory 'Rune_Manager_Modern_Core.exe'
$pending=Join-Path $directory 'Rune_Manager_SetMenu_Update.pending.exe'
$backup=Join-Path $directory ('Rune_Manager_Modern_Core.backup-setmenu-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.exe')
$expected='CE8675908247E0F0846E1024870B23D19A990EF2FDB599077982FBEAAA6D336F'
if((Get-FileHash -LiteralPath $pending).Hash -ne $expected){throw 'Mise à jour incorrecte'}
$deadline=(Get-Date).AddHours(4)
while((Get-Date) -lt $deadline){
  $running=Get-Process -Name Rune_Manager_Modern_Core -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $target}
  if(!$running){try{if(!(Test-Path -LiteralPath $backup)){Copy-Item -LiteralPath $target -Destination $backup};Copy-Item -LiteralPath $pending -Destination $target -Force;if((Get-FileHash -LiteralPath $target).Hash -ne $expected){throw 'Copie non vérifiée'};exit 0}catch{Start-Sleep -Seconds 2}}
  else{Start-Sleep -Seconds 2}
}
exit 1
