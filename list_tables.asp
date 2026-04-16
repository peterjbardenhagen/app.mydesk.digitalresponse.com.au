<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<%
Set rs = dbConn.Execute("SELECT name FROM sysobjects WHERE xtype='U' ORDER BY name")
Do Until rs.EOF
    Response.Write rs("name") & "<br>"
    rs.MoveNext
Loop
rs.Close
Set rs = Nothing
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
