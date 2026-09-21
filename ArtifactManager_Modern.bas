Attribute VB_Name = "ArtifactManager_Modern"
Option Explicit

Private Const SH_HOME As String = "Home"
Private Const SH_ART As String = "Artifacts"
Private Const SH_CFG As String = "Configuration"
Private Const SH_MONSTRE As String = "Monstre"

Public Sub ImporterJSONArtefacts()
    Dim fd As FileDialog, jsonPath As String, basePath As String
    Set fd = Application.FileDialog(msoFileDialogFilePicker)
    With fd
        .Title = "Choisir le fichier JSON SWEX"
        .Filters.Clear: .Filters.Add "Fichier JSON", "*.json"
        .AllowMultiSelect = False
        If .Show <> -1 Then Exit Sub
        jsonPath = .SelectedItems(1)
    End With
    basePath = ThisWorkbook.Path
    ImporterAvecMoteur jsonPath, basePath
End Sub

Private Sub ImporterAvecMoteur(ByVal jsonPath As String, ByVal basePath As String)
    Dim engine As String, dataFile As String, tempFile As String, cmd As String
    Dim shell As Object, rc As Long, oldCalc As XlCalculation
    engine = basePath & Application.PathSeparator & "ArtifactManagerEngine.exe"
    dataFile = basePath & Application.PathSeparator & "Artifact_Manager_Data.json"
    tempFile = Environ$("TEMP") & Application.PathSeparator & "artifact_manager_import.tsv"
    If Dir$(engine) = "" Or Dir$(dataFile) = "" Then
        MsgBox "ArtifactManagerEngine.exe ou Artifact_Manager_Data.json est absent du dossier du classeur.", vbCritical
        Exit Sub
    End If
    On Error GoTo EH
    Application.ScreenUpdating = False: Application.EnableEvents = False
    oldCalc = Application.Calculation: Application.Calculation = xlCalculationManual
    cmd = Q(engine) & " " & Q(jsonPath) & " " & Q(dataFile) & " " & Q(tempFile)
    Set shell = CreateObject("WScript.Shell")
    rc = shell.Run(cmd, 0, True)
    If rc <> 0 Or Dir$(tempFile) = "" Then Err.Raise vbObjectError + 101, , "Le moteur d'import n'a pas pu traiter le JSON."
    ChargerTSV tempFile
    ThisWorkbook.Worksheets(SH_CFG).Range("B12").Value2 = jsonPath
    TrierPotentialValue
    AjusterZoomArtifacts
    MsgBox "Import termine et scores SWLens recalcules.", vbInformation
FIN:
    Application.Calculation = oldCalc: Application.EnableEvents = True: Application.ScreenUpdating = True
    Exit Sub
EH:
    MsgBox "Import interrompu : " & Err.Description, vbCritical
    Resume FIN
End Sub

Private Sub ChargerTSV(ByVal path As String)
    Dim stm As Object, txt As String, lines As Variant, cells As Variant
    Dim arr() As Variant, i As Long, j As Long, n As Long, ws As Worksheet
    Set stm = CreateObject("ADODB.Stream")
    stm.Type = 2: stm.Charset = "utf-8": stm.Open: stm.LoadFromFile path
    txt = stm.ReadText: stm.Close
    txt = Replace(txt, vbCr, "")
    lines = Split(txt, vbLf)
    n = UBound(lines) + 1
    If n > 0 And Len(lines(n - 1)) = 0 Then n = n - 1
    ReDim arr(1 To n, 1 To 18)
    For i = 0 To n - 1
        cells = Split(lines(i), vbTab)
        For j = 0 To Application.Min(17, UBound(cells))
            arr(i + 1, j + 1) = cells(j)
        Next j
        If IsNumeric(arr(i + 1, 13)) Then arr(i + 1, 13) = CDbl(arr(i + 1, 13))
    Next i
    Set ws = ThisWorkbook.Worksheets(SH_ART)
    ws.Rows("2:" & ws.Rows.Count).ClearContents
    ws.Range("A2").Resize(n, 18).Value2 = arr
    ws.Range("A2:R" & n + 1).Interior.Color = RGB(9, 13, 20)
    ws.Range("A2:R" & n + 1).Font.Color = RGB(226, 232, 240)
    ws.Range("M2:M" & n + 1).NumberFormat = "0.000"
    ws.Range("A2:R" & n + 1).RowHeight = 22
    MettreCouleursActions ws, n + 1
    AjusterLargeurColonnesArtifacts ws
End Sub

Private Sub AjusterLargeurColonnesArtifacts(ByVal ws As Worksheet)
    Dim col As Variant, limites As Variant, i As Long
    col = Array("A", "C", "D", "G", "H", "I", "J", "K", "L", "M", "N", "O")
    limites = Array(30, 11, 13, 19, 0, 0, 0, 0, 9, 14, 0, 20)
    For i = LBound(col) To UBound(col)
        With ws.Columns(CStr(col(i)))
            .AutoFit
            If CDbl(limites(i)) > 0 Then
                If .ColumnWidth > CDbl(limites(i)) Then .ColumnWidth = CDbl(limites(i))
            End If
        End With
    Next i
End Sub

Private Sub MettreCouleursActions(ByVal ws As Worksheet, ByVal lastRow As Long)
    Dim i As Long
    For i = 2 To lastRow
        If ws.Cells(i, 12).Value2 = "Sell" Then
            ws.Range(ws.Cells(i, 1), ws.Cells(i, 18)).Interior.Color = RGB(63, 17, 17)
        ElseIf ws.Cells(i, 12).Value2 = "Keep" Then
            ws.Cells(i, 12).Font.Color = RGB(34, 197, 94)
        Else
            ws.Cells(i, 12).Font.Color = RGB(249, 115, 22)
        End If
    Next i
End Sub

Public Sub RecalculerArtefacts()
    Dim p As String
    p = CStr(ThisWorkbook.Worksheets(SH_CFG).Range("B12").Value2)
    If Len(p) = 0 Or Dir$(p) = "" Then ImporterJSONArtefacts Else ImporterAvecMoteur p, ThisWorkbook.Path
End Sub

Public Sub TrierPotentialValue()
    TrierColonne 13, xlDescending
End Sub

Public Sub TrierObtained()
    TrierColonne 2, xlDescending
End Sub

Public Sub AfficherArtifactsMonstreElementRTA()
    AfficherArtifactsMonstreCategorie "Element", "RTA"
End Sub

Public Sub AfficherArtifactsMonstreElementSiege()
    AfficherArtifactsMonstreCategorie "Element", "Siege"
End Sub

Public Sub AfficherArtifactsMonstreTypeRTA()
    AfficherArtifactsMonstreCategorie "Type", "RTA"
End Sub

Public Sub AfficherArtifactsMonstreTypeSiege()
    AfficherArtifactsMonstreCategorie "Type", "Siege"
End Sub

Private Sub AfficherArtifactsMonstreCategorie(ByVal categorie As String, ByVal modeJeu As String)
    Dim ws As Worksheet, monstre As String, jsonPath As String, basePath As String
    Dim engine As String, dataFile As String, tempFile As String, cmd As String
    Dim shell As Object, rc As Long, oldCalc As XlCalculation
    Set ws = ThisWorkbook.Worksheets(SH_MONSTRE)
    monstre = Trim$(CStr(ws.Range("A2").Value2))
    If Len(monstre) = 0 Then
        MsgBox "Selectionne d'abord un monstre dans la liste.", vbExclamation
        Exit Sub
    End If
    jsonPath = CStr(ThisWorkbook.Worksheets(SH_CFG).Range("B12").Value2)
    If Len(jsonPath) = 0 Or Dir$(jsonPath) = "" Then
        MsgBox "Importe d'abord ton fichier JSON depuis la feuille Home.", vbExclamation
        Exit Sub
    End If
    basePath = ThisWorkbook.Path
    engine = basePath & Application.PathSeparator & "ArtifactManagerEngine.exe"
    dataFile = basePath & Application.PathSeparator & "Artifact_Manager_Data.json"
    tempFile = Environ$("TEMP") & Application.PathSeparator & "artifact_manager_monstre_" & LCase$(categorie) & "_" & LCase$(modeJeu) & ".tsv"
    On Error GoTo EH
    Application.ScreenUpdating = False: Application.EnableEvents = False
    oldCalc = Application.Calculation: Application.Calculation = xlCalculationManual
    cmd = Q(engine) & " " & Q(jsonPath) & " " & Q(dataFile) & " " & Q(tempFile) & " " & Q(monstre) & " " & Q(categorie) & " " & Q(modeJeu)
    Set shell = CreateObject("WScript.Shell")
    rc = shell.Run(cmd, 0, True)
    If rc <> 0 Or Dir$(tempFile) = "" Then Err.Raise vbObjectError + 102, , "Le classement du monstre n'a pas pu etre calcule."
    ChargerTSVMonstre tempFile
FIN:
    Application.Calculation = oldCalc: Application.EnableEvents = True: Application.ScreenUpdating = True
    Exit Sub
EH:
    MsgBox "Classement interrompu : " & Err.Description, vbCritical
    Resume FIN
End Sub

Private Sub ChargerTSVMonstre(ByVal path As String)
    Dim stm As Object, txt As String, lines As Variant, cells As Variant
    Dim arr() As Variant, i As Long, j As Long, n As Long, ws As Worksheet
    Set stm = CreateObject("ADODB.Stream")
    stm.Type = 2: stm.Charset = "utf-8": stm.Open: stm.LoadFromFile path
    txt = stm.ReadText: stm.Close
    txt = Replace(txt, vbCr, "")
    lines = Split(txt, vbLf)
    n = UBound(lines) + 1
    If n > 0 And Len(lines(n - 1)) = 0 Then n = n - 1
    Set ws = ThisWorkbook.Worksheets(SH_MONSTRE)
    ws.Rows("5:" & ws.Rows.Count).ClearContents
    If n = 0 Then
        MsgBox "Aucun profil SWLens ou aucun artefact compatible pour cette selection.", vbInformation
        Exit Sub
    End If
    ReDim arr(1 To n, 1 To 18)
    For i = 0 To n - 1
        cells = Split(lines(i), vbTab)
        For j = 0 To Application.Min(17, UBound(cells))
            arr(i + 1, j + 1) = cells(j)
        Next j
        If IsNumeric(arr(i + 1, 13)) Then arr(i + 1, 13) = CDbl(arr(i + 1, 13))
    Next i
    ws.Range("A5").Resize(n, 18).Value2 = arr
    ws.Range("A5:R" & n + 4).Interior.Color = RGB(9, 13, 20)
    ws.Range("A5:R" & n + 4).Font.Color = RGB(226, 232, 240)
    ws.Range("M5:M" & n + 4).NumberFormat = "0.000"
    ws.Range("A5:R" & n + 4).RowHeight = 22
    MettreCouleursActions ws, n + 4
    AjusterLargeurColonnesArtifacts ws
    With ws.Sort
        .SortFields.Clear
        .SortFields.Add Key:=ws.Range("C5:C" & n + 4), SortOn:=xlSortOnValues, Order:=xlAscending, DataOption:=xlSortNormal
        .SortFields.Add Key:=ws.Range("M5:M" & n + 4), SortOn:=xlSortOnValues, Order:=xlDescending, DataOption:=xlSortNormal
        .SetRange ws.Range("A4:R" & n + 4): .Header = xlYes: .Apply
    End With
    ws.Activate: ws.Range("A4:O4").Select: ActiveWindow.Zoom = True: ws.Range("A5").Select
End Sub

Private Sub TrierColonne(ByVal col As Long, ByVal ordre As XlSortOrder)
    Dim ws As Worksheet, lr As Long
    Set ws = ThisWorkbook.Worksheets(SH_ART)
    lr = ws.Cells(ws.Rows.Count, 1).End(xlUp).Row
    If lr < 3 Then Exit Sub
    With ws.Sort
        .SortFields.Clear
        .SortFields.Add Key:=ws.Range(ws.Cells(2, col), ws.Cells(lr, col)), SortOn:=xlSortOnValues, Order:=ordre, DataOption:=xlSortNormal
        .SetRange ws.Range("A1:R" & lr): .Header = xlYes: .Apply
    End With
    ws.Activate: ws.Range("A2").Select
End Sub

Public Sub InstallerCommandesArtifacts()
    Dim ws As Worksheet, wsM As Worksheet, names, labels, macros, colors, i As Long, sh As Shape, rg As Range, lastMonster As Long
    Set ws = ThisWorkbook.Worksheets(SH_HOME)
    ThisWorkbook.Worksheets(SH_ART).Range("B:B,E:F,P:R").EntireColumn.Hidden = True
    names = Array("btnImportArtifact", "btnTriPotentialArtifact", "btnTriObtainedArtifact", "btnRecalculArtifact")
    labels = Array("IMPORTER JSON", "TRIER POTENTIAL VALUE", "TRIER OBTAINED", "RECALCULER")
    macros = Array("ImporterJSONArtefacts", "TrierPotentialValue", "TrierObtained", "RecalculerArtefacts")
    colors = Array(RGB(8, 145, 178), RGB(15, 118, 110), RGB(15, 118, 110), RGB(2, 132, 199))
    On Error Resume Next: ws.Shapes("btnTriActionArtifact").Delete: On Error GoTo 0
    For i = 0 To 3
        On Error Resume Next: ws.Shapes(names(i)).Delete: On Error GoTo 0
        Set rg = ws.Range("D" & 5 + i & ":H" & 5 + i)
        Set sh = ws.Shapes.AddShape(msoShapeRoundedRectangle, rg.Left, rg.Top, rg.Width, rg.Height)
        sh.Name = names(i): sh.OnAction = "'" & ThisWorkbook.Name & "'!" & macros(i)
        sh.Fill.ForeColor.RGB = colors(i): sh.Line.Visible = msoFalse
        sh.TextFrame2.TextRange.Text = labels(i): sh.TextFrame2.TextRange.Font.Fill.ForeColor.RGB = RGB(255, 255, 255)
        sh.TextFrame2.TextRange.Font.Bold = msoTrue: sh.TextFrame2.VerticalAnchor = msoAnchorMiddle
        sh.TextFrame2.TextRange.ParagraphFormat.Alignment = msoAlignCenter
    Next i
    Set wsM = ThisWorkbook.Worksheets(SH_MONSTRE)
    wsM.Range("B:B,E:F,P:R").EntireColumn.Hidden = True
    lastMonster = wsM.Cells(wsM.Rows.Count, "Z").End(xlUp).Row
    With wsM.Range("A2").Validation
        .Delete
        .Add Type:=xlValidateList, AlertStyle:=xlValidAlertStop, Operator:=xlBetween, Formula1:="=$Z$2:$Z$" & lastMonster
        .IgnoreBlank = True: .InCellDropdown = True
        .InputTitle = "Recherche de monstre"
        .InputMessage = "Tape les premieres lettres du nom ou ouvre la liste."
        .ShowInput = True
    End With
    On Error Resume Next
    wsM.Shapes("btnAfficherMonstre").Delete
    wsM.Shapes("btnAfficherMonstreElement").Delete
    wsM.Shapes("btnAfficherMonstreType").Delete
    wsM.Shapes("btnMonstreElementRTA").Delete
    wsM.Shapes("btnMonstreElementSiege").Delete
    wsM.Shapes("btnMonstreTypeRTA").Delete
    wsM.Shapes("btnMonstreTypeSiege").Delete
    On Error GoTo 0
    Set rg = wsM.Range("D2:G2")
    Set sh = wsM.Shapes.AddShape(msoShapeRoundedRectangle, rg.Left, rg.Top, rg.Width, rg.Height)
    sh.Name = "btnMonstreElementRTA": sh.OnAction = "'" & ThisWorkbook.Name & "'!AfficherArtifactsMonstreElementRTA"
    sh.Fill.ForeColor.RGB = RGB(8, 145, 178): sh.Line.Visible = msoFalse
    sh.TextFrame2.TextRange.Text = "ARTEFACTS ELEMENT RTA"
    sh.TextFrame2.TextRange.Font.Fill.ForeColor.RGB = RGB(255, 255, 255)
    sh.TextFrame2.TextRange.Font.Bold = msoTrue: sh.TextFrame2.VerticalAnchor = msoAnchorMiddle
    sh.TextFrame2.TextRange.ParagraphFormat.Alignment = msoAlignCenter
    Set rg = wsM.Range("H2:K2")
    Set sh = wsM.Shapes.AddShape(msoShapeRoundedRectangle, rg.Left, rg.Top, rg.Width, rg.Height)
    sh.Name = "btnMonstreElementSiege": sh.OnAction = "'" & ThisWorkbook.Name & "'!AfficherArtifactsMonstreElementSiege"
    sh.Fill.ForeColor.RGB = RGB(15, 118, 110): sh.Line.Visible = msoFalse
    sh.TextFrame2.TextRange.Text = "ARTEFACTS ELEMENT SIEGE"
    sh.TextFrame2.TextRange.Font.Fill.ForeColor.RGB = RGB(255, 255, 255)
    sh.TextFrame2.TextRange.Font.Bold = msoTrue: sh.TextFrame2.VerticalAnchor = msoAnchorMiddle
    sh.TextFrame2.TextRange.ParagraphFormat.Alignment = msoAlignCenter
    Set rg = wsM.Range("D3:G3")
    Set sh = wsM.Shapes.AddShape(msoShapeRoundedRectangle, rg.Left, rg.Top, rg.Width, rg.Height)
    sh.Name = "btnMonstreTypeRTA": sh.OnAction = "'" & ThisWorkbook.Name & "'!AfficherArtifactsMonstreTypeRTA"
    sh.Fill.ForeColor.RGB = RGB(2, 132, 199): sh.Line.Visible = msoFalse
    sh.TextFrame2.TextRange.Text = "ARTEFACTS TYPE RTA"
    sh.TextFrame2.TextRange.Font.Fill.ForeColor.RGB = RGB(255, 255, 255)
    sh.TextFrame2.TextRange.Font.Bold = msoTrue: sh.TextFrame2.VerticalAnchor = msoAnchorMiddle
    sh.TextFrame2.TextRange.ParagraphFormat.Alignment = msoAlignCenter
    Set rg = wsM.Range("H3:K3")
    Set sh = wsM.Shapes.AddShape(msoShapeRoundedRectangle, rg.Left, rg.Top, rg.Width, rg.Height)
    sh.Name = "btnMonstreTypeSiege": sh.OnAction = "'" & ThisWorkbook.Name & "'!AfficherArtifactsMonstreTypeSiege"
    sh.Fill.ForeColor.RGB = RGB(124, 58, 237): sh.Line.Visible = msoFalse
    sh.TextFrame2.TextRange.Text = "ARTEFACTS TYPE SIEGE"
    sh.TextFrame2.TextRange.Font.Fill.ForeColor.RGB = RGB(255, 255, 255)
    sh.TextFrame2.TextRange.Font.Bold = msoTrue: sh.TextFrame2.VerticalAnchor = msoAnchorMiddle
    sh.TextFrame2.TextRange.ParagraphFormat.Alignment = msoAlignCenter
    AjusterZoomArtifacts
End Sub

Private Sub AjusterZoomArtifacts()
    Dim ws As Worksheet
    Set ws = ThisWorkbook.Worksheets(SH_ART)
    ws.Activate
    ws.Range("A1:O1").Select
    ActiveWindow.Zoom = True
    ws.Range("A2").Select
End Sub

Private Function Q(ByVal s As String) As String
    Q = Chr$(34) & Replace(s, Chr$(34), Chr$(34) & Chr$(34)) & Chr$(34)
End Function
