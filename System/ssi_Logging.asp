<%

' ============================================================================
' Error Logging Functions
' ============================================================================
' Usage: Call LogError() from error handling pages
' Logs are written to /Logs/YYYY-MM-DD_ErrorLog.txt
' ============================================================================

Sub LogError(strSource, strDescription, strFile, intLine, strAdditionalInfo)
    On Error Resume Next
    
    Dim objFSO, objFile, strLogPath, strLogFile, strLogEntry
    Dim strDate, strTime, strUser, strIP, strURL, strQueryString
    Dim strSessionInfo
    
    ' Get current date/time
    strDate = Year(Now()) & "-" & Right("0" & Month(Now()), 2) & "-" & Right("0" & Day(Now()), 2)
    strTime = Right("0" & Hour(Now()), 2) & ":" & Right("0" & Minute(Now()), 2) & ":" & Right("0" & Second(Now()), 2)
    
    ' Get request info
    strURL = Request.ServerVariables("URL")
    strQueryString = Request.QueryString
    strIP = Request.ServerVariables("REMOTE_ADDR")
    strUser = ""
    
    ' Try to get user info from session or cookies
    If Session("Name") <> "" Then
        strUser = Session("Name")
    ElseIf Session("Code") <> "" Then
        strUser = Session("Code")
    ElseIf Request.Cookies("UserSettings")("Name") <> "" Then
        strUser = Request.Cookies("UserSettings")("Name")
    ElseIf Request.Cookies("UserSettings")("Code") <> "" Then
        strUser = Request.Cookies("UserSettings")("Code")
    Else
        strUser = "Anonymous"
    End If
    
    ' Build log file path - one file per day
    strLogPath = Server.MapPath("/Logs/")
    strLogFile = strLogPath & "\" & strDate & "_ErrorLog.txt"
    
    ' Build log entry
    strLogEntry = String(80, "=") & vbCrLf
    strLogEntry = strLogEntry & "TIMESTAMP: " & strDate & " " & strTime & vbCrLf
    strLogEntry = strLogEntry & "USER: " & strUser & vbCrLf
    strLogEntry = strLogEntry & "IP ADDRESS: " & strIP & vbCrLf
    strLogEntry = strLogEntry & "URL: " & strURL & vbCrLf
    If strQueryString <> "" Then
        strLogEntry = strLogEntry & "QUERY STRING: " & strQueryString & vbCrLf
    End If
    strLogEntry = strLogEntry & "SOURCE: " & strSource & vbCrLf
    strLogEntry = strLogEntry & "ERROR: " & strDescription & vbCrLf
    If strFile <> "" Then
        strLogEntry = strLogEntry & "FILE: " & strFile & vbCrLf
    End If
    If intLine > 0 Then
        strLogEntry = strLogEntry & "LINE: " & intLine & vbCrLf
    End If
    If strAdditionalInfo <> "" Then
        strLogEntry = strLogEntry & "ADDITIONAL INFO: " & strAdditionalInfo & vbCrLf
    End If
    strLogEntry = strLogEntry & String(80, "=") & vbCrLf & vbCrLf
    
    ' Write to log file
    Set objFSO = Server.CreateObject("Scripting.FileSystemObject")
    
    ' Check if logs directory exists, create if not
    If Not objFSO.FolderExists(strLogPath) Then
        objFSO.CreateFolder(strLogPath)
    End If
    
    ' Open file for appending (create if doesn't exist)
    If objFSO.FileExists(strLogFile) Then
        Set objFile = objFSO.OpenTextFile(strLogFile, 8, False) ' 8 = ForAppending
    Else
        Set objFile = objFSO.CreateTextFile(strLogFile, True)
    End If
    
    objFile.Write strLogEntry
    objFile.Close
    
    ' Cleanup
    Set objFile = Nothing
    Set objFSO = Nothing
    
    ' Don't raise errors from logging
    On Error GoTo 0
End Sub

' Convenience function to log ASP errors
Sub LogASPError(objError)
    On Error Resume Next
    
    Call LogError(_
        objError.Source, _
        objError.Description, _
        objError.File, _
        objError.Line, _
        "ASPCode: " & objError.ASPCode & " | Number: " & objError.Number & " (0x" & Hex(objError.Number) & ")"_
    )
    
    On Error GoTo 0
End Sub

' Function to log custom messages
Sub LogMessage(strMessage, strLevel)
    On Error Resume Next
    
    Dim objFSO, objFile, strLogPath, strLogFile, strLogEntry
    Dim strDate, strTime
    
    ' Get current date/time
    strDate = Year(Now()) & "-" & Right("0" & Month(Now()), 2) & "-" & Right("0" & Day(Now()), 2)
    strTime = Right("0" & Hour(Now()), 2) & ":" & Right("0" & Minute(Now()), 2) & ":" & Right("0" & Second(Now()), 2)
    
    ' Build log file path
    strLogPath = Server.MapPath("/Logs/")
    strLogFile = strLogPath & "\" & strDate & "_AppLog.txt"
    
    ' Build log entry
    strLogEntry = strDate & " " & strTime & " [" & strLevel & "] " & strMessage & vbCrLf
    
    ' Write to log file
    Set objFSO = Server.CreateObject("Scripting.FileSystemObject")
    
    If Not objFSO.FolderExists(strLogPath) Then
        objFSO.CreateFolder(strLogPath)
    End If
    
    If objFSO.FileExists(strLogFile) Then
        Set objFile = objFSO.OpenTextFile(strLogFile, 8, False)
    Else
        Set objFile = objFSO.CreateTextFile(strLogFile, True)
    End If
    
    objFile.Write strLogEntry
    objFile.Close
    
    Set objFile = Nothing
    Set objFSO = Nothing
    On Error GoTo 0
End Sub

%>
