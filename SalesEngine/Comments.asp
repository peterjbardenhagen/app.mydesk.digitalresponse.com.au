<%

Response.Expires = -1

Dim lngQid
lngQid = CLng(Request("Qid"))

%>
<!--#include virtual="/SalesEngine/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/SalesEngine/System/ssi_Dates.inc"-->
<html>
	<head>
		<link rel="stylesheet" type="text/css" href="System/Style.css">
	</head>
	<body>
		<span class="Header3">View Comments</span>
		<br/><br/><li><a href="Comments_Add.asp?Qid=<%= lngQid %>">Add Comment</a></li>
		
<%

Set rs = Server.CreateObject("ADODB.RecordSet")
strSql = "Select * From ProjectHistory Where Qid = " & lngQid & " Order By Date Desc"
Set rs = dbConn.Execute(strSql)

%>
		<br/><br/>
		<table width="100%" cellpadding=5 cellspacing=0 border=0>
			<tr>
				<td style="border-bottom:1px solid black;" width=190><b>Date</b></td>
				<td style="border-bottom:1px solid black;"><b>Comment</b></td>
				<td style="border-bottom:1px solid black;" width=50><b>Initials</b></td>
			</tr>
<%

If Not(rs.BOF And rs.EOF) Then

	Do Until rs.EOF

%>
			<tr>
				<td style="border-bottom:1px solid black;" width=190><%= FormatDateTime(rs("Date"),1) %></td>
				<td style="border-bottom:1px solid black;"><%= rs("Comment") %></td>
				<td style="border-bottom:1px solid black;" width=50><%= rs("Initials") %></td>
			</tr>
<%

		rs.MoveNext
	Loop

%>
		</table>
<%

End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>	
		<br><div align="right"><a href="JavaScript:history.go(-1);"><< Go back to quote list</a></div>
	</body>
</html>
<!--#include virtual="/SalesEngine/System/ssi_dbConn_close.inc"-->