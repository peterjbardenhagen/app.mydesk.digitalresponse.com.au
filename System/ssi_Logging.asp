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
    
    ' Initialize variables to prevent null reference errors
    strUser = ""
    strIP = ""
    strURL = ""
    strQueryString = ""
    strSource = ""
    strDescription = ""
    strFile = ""
    strAdditionalInfo = ""
    
    ' Get current date/time with error handling
    strDate = Year(Now()) & "-" & Right("0" & Month(Now()), 2) & "-" & Right("0" & Day(Now()), 2)
    strTime = Right("0" & Hour(Now()), 2) & ":" & Right("0" & Minute(Now()), 2) & ":" & Right("0" & Second(Now()), 2)
    
    ' Get request info with error handling
    strURL = Request.ServerVariables("URL")
    If Err.Number <> 0 Then strURL = "Unknown"
    On Error Resume Next
    
    strQueryString = Request.QueryString
    If Err.Number <> 0 Then strQueryString = ""
    On Error Resume Next
    
    strIP = Request.ServerVariables("REMOTE_ADDR")
    If Err.Number <> 0 Then strIP = "Unknown"
    On Error Resume Next
    
    ' Try to get user info from session or cookies with null checks
    If Not IsEmpty(Session("Name")) And Session("Name") <> "" Then
        strUser = Session("Name")
    ElseIf Not IsEmpty(Session("Code")) And Session("Code") <> "" Then
        strUser = Session("Code")
    ElseIf Not Request.Cookies("UserSettings") Is Nothing Then
        If Not IsEmpty(Request.Cookies("UserSettings")("Name")) And Request.Cookies("UserSettings")("Name") <> "" Then
            strUser = Request.Cookies("UserSettings")("Name")
        ElseIf Not IsEmpty(Request.Cookies("UserSettings")("Code")) And Request.Cookies("UserSettings")("Code") <> "" Then
            strUser = Request.Cookies("UserSettings")("Code")
        End If
    Else
        strUser = "Anonymous"
    End If
    On Error Resume Next
    
    ' Sanitize input parameters
    If Not IsNull(strSource) Then strSource = CStr(strSource)
    If Not IsNull(strDescription) Then strDescription = CStr(strDescription)
    If Not IsNull(strFile) Then strFile = CStr(strFile)
    If Not IsNull(strAdditionalInfo) Then strAdditionalInfo = CStr(strAdditionalInfo)
    If Not IsNull(intLine) Then intLine = CLng(intLine) Else intLine = 0
    On Error Resume Next
    
    ' Build log file path - one file per day
    strLogPath = Server.MapPath("/Logs/")
    If Err.Number <> 0 Then
        ' If Logs directory doesn't exist or can't be mapped, use alternative path
        strLogPath = Server.MapPath(".")
    End If
    On Error Resume Next
    
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
    If Err.Number = 0 Then
        ' Check if logs directory exists, create if not
        If Not objFSO.FolderExists(strLogPath) Then
            On Error Resume Next
            objFSO.CreateFolder(strLogPath)
            If Err.Number <> 0 Then
                ' Failed to create directory, try using current directory
                strLogPath = Server.MapPath(".")
                strLogFile = strLogPath & "\" & strDate & "_ErrorLog.txt"
            End If
            On Error Resume Next
        End If
        
        ' Open file for appending (create if doesn't exist)
        If objFSO.FileExists(strLogFile) Then
            Set objFile = objFSO.OpenTextFile(strLogFile, 8, False)
        Else
            Set objFile = objFSO.CreateTextFile(strLogFile, True)
        End If
        
        If Err.Number = 0 Then
            objFile.Write strLogEntry
            objFile.Close
        End If
    End If
    
    ' Cleanup - always execute
    Set objFile = Nothing
    Set objFSO = Nothing
    
    ' Don't raise errors from logging
    On Error GoTo 0
End Sub

' Convenience function to log ASP errors
Sub LogASPError(objError)
    On Error Resume Next
    
    Dim strSource, strDescription, strFile, strAdditionalInfo
    Dim intLine
    
    ' Null check for error object
    If objError Is Nothing Then
        Call LogError("Unknown", "Error object is Nothing", "", 0, "LogASPError called with Nothing")
        On Error GoTo 0
        Exit Sub
    End If
    
    ' Extract error properties with error handling
    strSource = ""
    strDescription = ""
    strFile = ""
    intLine = 0
    strAdditionalInfo = ""
    
    On Error Resume Next
    If Not IsNull(objError.Source) Then strSource = CStr(objError.Source)
    If Err.Number <> 0 Then strSource = ""
    On Error Resume Next
    
    If Not IsNull(objError.Description) Then strDescription = CStr(objError.Description)
    If Err.Number <> 0 Then strDescription = ""
    On Error Resume Next
    
    If Not IsNull(objError.File) Then strFile = CStr(objError.File)
    If Err.Number <> 0 Then strFile = ""
    On Error Resume Next
    
    If Not IsNull(objError.Line) Then intLine = CLng(objError.Line)
    If Err.Number <> 0 Then intLine = 0
    On Error Resume Next
    
    strAdditionalInfo = "ASPCode: "
    If Not IsNull(objError.ASPCode) Then strAdditionalInfo = strAdditionalInfo & CStr(objError.ASPCode)
    strAdditionalInfo = strAdditionalInfo & " | Number: "
    If Not IsNull(objError.Number) Then strAdditionalInfo = strAdditionalInfo & CStr(objError.Number)
    strAdditionalInfo = strAdditionalInfo & " (0x"
    If Not IsNull(objError.Number) Then strAdditionalInfo = strAdditionalInfo & Hex(objError.Number)
    strAdditionalInfo = strAdditionalInfo & ")"
    On Error Resume Next
    
    Call LogError(strSource, strDescription, strFile, intLine, strAdditionalInfo)
    
    On Error GoTo 0
End Sub

' Function to log custom messages
Sub LogMessage(strMessage, strLevel)
    On Error Resume Next
    
    Dim objFSO, objFile, strLogPath, strLogFile, strLogEntry
    Dim strDate, strTime
    
    ' Initialize variables
    strMessage = ""
    strLevel = ""
    
    ' Sanitize inputs
    If Not IsNull(strMessage) Then strMessage = CStr(strMessage)
    If Not IsNull(strLevel) Then strLevel = CStr(strLevel)
    On Error Resume Next
    
    ' Get current date/time with error handling
    strDate = Year(Now()) & "-" & Right("0" & Month(Now()), 2) & "-" & Right("0" & Day(Now()), 2)
    strTime = Right("0" & Hour(Now()), 2) & ":" & Right("0" & Minute(Now()), 2) & ":" & Right("0" & Second(Now()), 2)
    
    ' Build log file path with fallback
    strLogPath = Server.MapPath("/Logs/")
    If Err.Number <> 0 Then
        strLogPath = Server.MapPath(".")
    End If
    On Error Resume Next
    
    strLogFile = strLogPath & "\" & strDate & "_AppLog.txt"
    
    ' Build log entry
    strLogEntry = strDate & " " & strTime & " [" & strLevel & "] " & strMessage & vbCrLf
    
    ' Write to log file
    Set objFSO = Server.CreateObject("Scripting.FileSystemObject")
    If Err.Number = 0 Then
        ' Check if logs directory exists, create if not
        If Not objFSO.FolderExists(strLogPath) Then
            On Error Resume Next
            objFSO.CreateFolder(strLogPath)
            If Err.Number <> 0 Then
                strLogPath = Server.MapPath(".")
                strLogFile = strLogPath & "\" & strDate & "_AppLog.txt"
            End If
            On Error Resume Next
        End If
        
        ' Open file for appending (create if doesn't exist)
        If objFSO.FileExists(strLogFile) Then
            Set objFile = objFSO.OpenTextFile(strLogFile, 8, False)
        Else
            Set objFile = objFSO.CreateTextFile(strLogFile, True)
        End If
        
        If Err.Number = 0 Then
            objFile.Write strLogEntry
            objFile.Close
        End If
    End If
    
    ' Cleanup - always execute
    Set objFile = Nothing
    Set objFSO = Nothing
    On Error GoTo 0
End Sub

%>
