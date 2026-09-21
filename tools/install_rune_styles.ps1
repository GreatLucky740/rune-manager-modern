$ErrorActionPreference='Stop'
$directory='C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees'
$target=Join-Path $directory 'Rune_Manager_Modern_Core.exe'
$pending=Join-Path $directory 'Rune_Styles_White_Ancient.pending.exe'
$expected='8D066684D781504614F8A4F5E083FBAE7A23336533B6384A1FF7B5011BA4F943'
$backup=Join-Path $directory ('Rune_Manager_Modern_Core.backup-icons-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.exe')
$deadline=(Get-Date).AddHours(4)
while((Get-Date) -lt $deadline){
 if(!(Get-Process -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $target})){
  if((Get-FileHash -LiteralPath $pending).Hash -ne $expected){throw 'Fichier de mise a jour incorrect'}
  if(!(Test-Path -LiteralPath $backup)){Copy-Item -LiteralPath $target -Destination $backup}
  Copy-Item -LiteralPath $pending -Destination $target -Force
  if((Get-FileHash -LiteralPath $target).Hash -ne $expected){throw 'Verification echouee'}
  exit 0
 }
 Start-Sleep -Seconds 2
}
exit 1
