<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%
On Error Resume Next
Set rs = dbConn.Execute("SELECT TOP 1 * FROM Quotes")
For Each field In rs.Fields
    Response.Write field.Name & "<br>"
Next
rs.Close
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
