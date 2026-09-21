$ErrorActionPreference = 'Stop'
$base = Split-Path -Parent $MyInvocation.MyCommand.Path
$xlsx = Join-Path $base 'Artifact_Manager_Modern_V2_SWLens.xlsx'
$xlsm = Join-Path $base 'Artifact_Manager_Modern_V2_SWLens_Macros.xlsm'
$bas = Join-Path $base 'ArtifactManager_Modern.bas'
if (!(Test-Path -LiteralPath $xlsx)) { throw "Fichier source absent: $xlsx" }
if (!(Test-Path -LiteralPath $bas)) { throw "Module VBA absent: $bas" }
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
try {
    $wb = $excel.Workbooks.Open($xlsx)
    $wb.SaveAs($xlsm, 52)
    try {
        [void]$wb.VBProject.VBComponents.Import($bas)
    } catch {
        throw "Excel bloque l'acces au projet VBA. Active 'Acces approuve au modele d'objet du projet VBA', puis relance ce programme."
    }
    $excel.Run("'" + $wb.Name + "'!InstallerCommandesArtifacts")
    $wb.Save()
    $wb.Close($true)
} finally {
    $excel.Quit()
    [Runtime.InteropServices.Marshal]::ReleaseComObject($excel) | Out-Null
}
Write-Output $xlsm
