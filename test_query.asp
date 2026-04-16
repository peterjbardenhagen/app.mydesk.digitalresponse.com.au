<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%
On Error Resume Next
currentMonth = Month(Date())
currentYear = Year(Date())

sql = "SELECT COUNT(*) as cnt, SUM(IIf(QuoteStatusId = 4 OR QuoteStatusId = 8, 1, 0)) as won, SUM(NettPriceTotal) as val FROM Quotes WHERE Month(QuoteDate) = " & currentMonth & " AND Year(QuoteDate) = " & currentYear & " AND Deleted = 0"
Response.Write "Query: " & sql & "<br>"
Set rs = dbConn.Execute(sql)

If Err.Number <> 0 Then
    Response.Write "Error: " & Err.Description & " (" & Err.Number & ")<br>"
Else
    If Not rs.EOF Then
        Response.Write "Count: " & rs("cnt") & "<br>"
        Response.Write "Won: " & rs("won") & "<br>"
        Response.Write "Val: " & rs("val") & "<br>"
    Else
        Response.Write "No results<br>"
    End If
End If
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
