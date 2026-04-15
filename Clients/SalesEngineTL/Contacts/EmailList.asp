<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

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
		<link rel="Stylesheet" type="text/css" href="/System/Style_Print.css" media="print">
	</head>
	<body style="background-color:#ffffff !important;" Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" cellpadding=10 cellspacing=0 border=0>
			<tr>
				<td style="background-color:#cccccc;font-size:16px;" colspan=2>Your Email List</td>
			</tr>
			<tr>
				<td colspan=2>Simply highlight all email addresses seperated by the semi colon, right click and select Copy. Next, open Microsoft Outlook and paste the email addresses into the BCC field of a new email message to email to all customers & suppliers.</td>
			</tr>
			<tr>
				<td colspan=2>
<%

Dim sql
Dim rs
sql = "Select * From Contacts Where Code = '" & Request.Cookies("UserSettings")("Code") & "'"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
	Do Until rs.EOF
		If rs("Email") <> "" Then
			Response.Write rs("Email") & ";"
		End If
		rs.MoveNext
	Loop
End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
				</td>
			</tr>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
