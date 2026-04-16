<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%
On Error Resume Next
Response.Write "<h1>Database Migration: UserHistory</h1>"

' Check if table exists
Dim rsTable, tableExists
tableExists = False
Set rsTable = dbConn.OpenSchema(20, Array(Empty, Empty, "UserHistory", "TABLE"))
If Not rsTable.EOF Then
    tableExists = True
    Response.Write "<p>Table 'UserHistory' already exists.</p>"
End If
rsTable.Close
Set rsTable = Nothing

If Not tableExists Then
    Response.Write "<p>Creating table 'UserHistory'...</p>"
    ' Note: MS Access DDL
    sql = "CREATE TABLE UserHistory (" & _
          "HistoryId AUTOINCREMENT PRIMARY KEY, " & _
          "UserCode VARCHAR(50), " & _
          "PageUrl VARCHAR(255), " & _
          "PageTitle VARCHAR(255), " & _
          "VisitDate DATETIME DEFAULT NOW())"
    dbConn.Execute(sql)
    
    If Err.Number = 0 Then
        Response.Write "<p style='color:green;'>Table 'UserHistory' created successfully.</p>"
    Else
        Response.Write "<p style='color:red;'>Error creating table: " & Err.Description & "</p>"
    End If
End If

' Also check/create ActivityLogs if needed for more detailed tracking
' For now, UserHistory is enough for "where they left off"

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<p><a href="Default.asp">Back to Setup</a></p>
