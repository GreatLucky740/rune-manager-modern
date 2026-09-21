$ErrorActionPreference='Stop'
$directory='C:\Users\Great-Lucky\Documents\Rune_Manager_Sigmarus_Test_Etendu\Donnees'
$target=Join-Path $directory 'Rune_Manager_Modern_Core.exe'
$pending=Join-Path $directory 'Rune_Manager_Modern_Core.pending.exe'
$backup=Join-Path $directory ('Rune_Manager_Modern_Core.backup-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.exe')
$expected='98050BB75A5C79BE810EA25F8512C39A08EF8256E30E2D441E6694B2675C37C9'
if((Get-FileHash -LiteralPath $pending -Algorithm SHA256).Hash -ne $expected){throw 'Le fichier de mise à jour ne correspond pas au fichier vérifié.'}
$deadline=(Get-Date).AddHours(4)
while((Get-Date) -lt $deadline){
  $running=Get-Process -Name Rune_Manager_Modern_Core -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $target}
  if(!$running){
    try {
      if(!(Test-Path -LiteralPath $backup)){Copy-Item -LiteralPath $target -Destination $backup}
      Copy-Item -LiteralPath $pending -Destination $target -Force
      if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $expected){throw 'Vérification après remplacement impossible.'}
      [IO.File]::WriteAllText((Join-Path $directory 'mise-a-jour-runes-terminee.txt'),'Mise à jour runes installée le '+(Get-Date)+'. Ancien exécutable : '+$backup)
      exit 0
    }catch {Start-Sleep -Seconds 2}
  }else {Start-Sleep -Seconds 2}
}
exit 1
