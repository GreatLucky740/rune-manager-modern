param([Parameter(Mandatory=$true)][string]$ExpectedHash,[Parameter(Mandatory=$true)][string]$PreviousHash)
$ErrorActionPreference='Stop'
$directory='C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees'
$target=Join-Path $directory 'Rune_Manager_Modern_Core.exe'
$pending=Join-Path $directory 'Rune_Old_Icons_Restore.pending.exe'
$backup=Join-Path $directory ('Rune_Manager_Modern_Core.backup-before-icon-restore-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.exe')
$deadline=(Get-Date).AddHours(4)
while((Get-Date) -lt $deadline){
  $running=Get-Process -Name Rune_Manager_Modern_Core -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq $target}
  if(!$running){
    if((Get-FileHash -LiteralPath $pending).Hash -ne $ExpectedHash){throw 'Le fichier de mise a jour a change.'}
    $currentHash=(Get-FileHash -LiteralPath $target).Hash
    if($currentHash -eq $ExpectedHash){exit 0}
    if($currentHash -ne $PreviousHash){throw 'Une autre mise a jour a modifie l application. Installation annulee pour la preserver.'}
    Copy-Item -LiteralPath $target -Destination $backup
    try {
      Copy-Item -LiteralPath $pending -Destination $target -Force
      if((Get-FileHash -LiteralPath $target).Hash -ne $ExpectedHash){throw 'Verification des icones echouee.'}
      exit 0
    } catch {
      Copy-Item -LiteralPath $backup -Destination $target -Force
      throw
    }
  }
  Start-Sleep -Seconds 2
}
exit 1
