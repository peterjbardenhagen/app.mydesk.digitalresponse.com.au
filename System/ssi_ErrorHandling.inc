<%
' ===============================================================================
' Techlight MyDesk - Proper Error Handling Patterns
' ===============================================================================
' RULES:
' 1. Use On Error Resume Next ONLY for existence checks (FileSystemObject, etc.)
' 2. ALWAYS check Err.Number immediately after the risky operation
' 3. ALWAYS use On Error GoTo 0 to turn off error suppression
' 4. NEVER leave On Error Resume Next on for extended code blocks
' 5. Use If/Then validation BEFORE operations when possible
' ===============================================================================

'-------------------------------------------------------------------------------
' Safe FileSystemObject Creation
'-------------------------------------------------------------------------------
Function CreateFSOSafe()
    Dim objFSO
    On Error Resume Next
    Set objFSO = Server.CreateObject("Scripting.FileSystemObject")
    If Err.Number <> 0 Then
        Set CreateFSOSafe = Nothing
        Err.Clear
    Else
        Set CreateFSOSafe = objFSO
    End If
    On Error GoTo 0
End Function

'-------------------------------------------------------------------------------
' Safe Database Connection
'-------------------------------------------------------------------------------
Function CreateDBConnectionSafe()
    Dim objConn
    On Error Resume Next
    Set objConn = Server.CreateObject("ADODB.Connection")
    If Err.Number <> 0 Then
        Set CreateDBConnectionSafe = Nothing
        Err.Clear
    Else
        Set CreateDBConnectionSafe = objConn
    End If
    On Error GoTo 0
End Function

'-------------------------------------------------------------------------------
' Safe Recordset Creation
'-------------------------------------------------------------------------------
Function CreateRecordsetSafe()
    Dim objRS
    On Error Resume Next
    Set objRS = Server.CreateObject("ADODB.Recordset")
    If Err.Number <> 0 Then
        Set CreateRecordsetSafe = Nothing
        Err.Clear
    Else
        Set CreateRecordsetSafe = objRS
    End If
    On Error GoTo 0
End Function

'-------------------------------------------------------------------------------
' Safe File Existence Check
'-------------------------------------------------------------------------------
Function FileExistsSafe(strPath)
    Dim objFSO, boolExists
    boolExists = False
    
    Set objFSO = CreateFSOSafe()
    If Not objFSO Is Nothing Then
        On Error Resume Next
        boolExists = objFSO.FileExists(strPath)
        If Err.Number <> 0 Then
            boolExists = False
            Err.Clear
        End If
        On Error GoTo 0
        Set objFSO = Nothing
    End If
    
    FileExistsSafe = boolExists
End Function

'-------------------------------------------------------------------------------
' Safe Folder Existence Check
'-------------------------------------------------------------------------------
Function FolderExistsSafe(strPath)
    Dim objFSO, boolExists
    boolExists = False
    
    Set objFSO = CreateFSOSafe()
    If Not objFSO Is Nothing Then
        On Error Resume Next
        boolExists = objFSO.FolderExists(strPath)
        If Err.Number <> 0 Then
            boolExists = False
            Err.Clear
        End If
        On Error GoTo 0
        Set objFSO = Nothing
    End If
    
    FolderExistsSafe = boolExists
End Function

%>
