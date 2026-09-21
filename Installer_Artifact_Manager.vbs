Option Explicit
Dim fso, base, xlsx, xlsm, bas, excel, wb
Set fso = CreateObject("Scripting.FileSystemObject")
base = fso.GetParentFolderName(WScript.ScriptFullName)
xlsx = fso.BuildPath(base, "Artifact_Manager_Modern_V2_SWLens.xlsx")
xlsm = fso.BuildPath(base, "Artifact_Manager_Modern_V2_SWLens_Macros.xlsm")
bas = fso.BuildPath(base, "ArtifactManager_Modern.bas")
If Not fso.FileExists(xlsx) Or Not fso.FileExists(bas) Then
  MsgBox "Fichier Excel ou module VBA absent du dossier.", 16, "Artifact Manager"
  WScript.Quit 1
End If
On Error Resume Next
Set excel = CreateObject("Excel.Application")
If Err.Number <> 0 Then
  MsgBox "Impossible de lancer Excel : " & Err.Description, 16, "Artifact Manager"
  WScript.Quit 2
End If
On Error GoTo 0
excel.Visible = False
excel.DisplayAlerts = False
On Error Resume Next
Set wb = excel.Workbooks.Open(xlsx)
wb.SaveAs xlsm, 52
wb.VBProject.VBComponents.Import bas
If Err.Number <> 0 Then
  wb.Close False
  excel.Quit
  MsgBox "Excel bloque l'installation VBA." & vbCrLf & vbCrLf & _
    "Dans Excel : Fichier > Options > Centre de gestion de la confidentialite > Parametres du Centre > Parametres des macros > active Acces approuve au modele d'objet du projet VBA, puis relance cet installateur.", 48, "Artifact Manager"
  WScript.Quit 3
End If
excel.Run "'" & wb.Name & "'!InstallerCommandesArtifacts"
wb.Save
wb.Close True
excel.Quit
On Error GoTo 0
MsgBox "Installation terminee." & vbCrLf & vbCrLf & "Le fichier Artifact_Manager_Modern_V2_SWLens_Macros.xlsm est pret.", 64, "Artifact Manager"
