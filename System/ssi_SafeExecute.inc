<% 
If IsEmpty(ssi_SafeExecute_Included) Then
    ssi_SafeExecute_Included = True
%>
<% 
'===================================================================
' SafeExecute and utility helpers – centralised DB execution wrapper
'===================================================================
On Error Resume Next

'---------------------------------------------------------------
' SafeExecute – runs a SQL statement and logs any error
'---------------------------------------------------------------
Function SafeExecute(ByVal sql)
    Dim execResult
    execResult = False
    If IsObject(dbConn) Then
        If dbConn.State = 1 Then
            dbConn.Execute sql
            If Err.Number <> 0 Then
                ' Log the DB error to the same flat file used by the error handler
                Dim fso, ts, logPath
                logPath = Server.MapPath("/Logs/ASP_ErrorLog.txt")
                Set fso = Server.CreateObject("Scripting.FileSystemObject")
                Set ts = fso.OpenTextFile(logPath, 8, True)
                ts.WriteLine "=== DB EXECUTION ERROR ==="
                ts.WriteLine "SQL : " & sql
                ts.WriteLine "Error #: " & Err.Number
                ts.WriteLine "Source : " & Err.Source
                ts.WriteLine "Desc   : " & Err.Description
                ts.WriteLine "----------------------------------------"
                ts.Close
                Set ts = Nothing
                Set fso = Nothing
                Err.Clear
            Else
                execResult = True
            End If
        End If
    End If
    SafeExecute = execResult
End Function

'---------------------------------------------------------------
' CloseRS – safely close a Recordset if it exists
'---------------------------------------------------------------
Sub CloseRS(ByRef rsObj)
    If IsObject(rsObj) Then
        On Error Resume Next
        rsObj.Close
        Set rsObj = Nothing
        On Error GoTo 0
    End If
End Sub

%>
