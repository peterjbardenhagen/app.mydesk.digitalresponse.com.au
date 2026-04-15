<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim intItemId
Dim intTableId

intItemId = CLng(Request("ItemId"))
intTableId = CLng(Request("TableId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
	</head>
	<body>
		<span class="Header3">View Files</span><br><br>
		<li><a href="#" onclick="parent.document.location.href=parent.document.location.href;"><< Back</a><br>
		<li><a href="Add.asp?ItemId=<%= intItemId %>&TableId=<%= intTableId %>">Add File</a>
		<br/>
<%

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From TableFiles Where ItemId = " & intItemId & " And TableId = " & intTableId & " Order By [DateEntered] Desc"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then

%>
		<br/>
		<table width="100%" cellpadding=5 cellspacing=0 border=0>
			<tr>
				<td style="border-bottom:1px solid black;" width=100><b>Date</b></td>
				<td style="border-bottom:1px solid black;"><b>Description</b></td>
				<td style="border-bottom:1px solid black;" width=75><b>Uploaded By</b></td>
				<td style="border-bottom:1px solid black;text-align:right;" width=75><b>Action</b></td>
			</tr>
<%

	Do Until rs.EOF

		Initials = ""

		Set rs2 = Server.CreateObject("ADODB.RecordSet")
		sql = "Select Top 1 * From Users Where Code = '" & rs("Code") & "'"
		Set rs2 = dbConn.Execute(sql)
		
		If Not(rs2.BOF And rs2.EOF) Then
			Initials = rs2("Initials") & "&nbsp;"
		End If
		
		rs2.Close
		Set rs2 = Nothing

%>
			<tr>
				<td style="border-bottom:1px solid black;" width=100><%= FormatDateU(rs("DateEntered"), False) %>&nbsp;</td>
				<td style="border-bottom:1px solid black;"><%= rs("Description") %>&nbsp;</td>
				<td style="border-bottom:1px solid black;" width=75><%= Initials %>&nbsp;</td>
				<td style="border-bottom:1px solid black;text-align:right;" width=75><a href="Files/<%= rs("Filename") %>" target="_blank">Download</a></td>
			</tr>
<%

		rs.MoveNext
	Loop

%>
		</table>
<%

Else

%>
		<p>There are currently no files</p>
<%

End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
