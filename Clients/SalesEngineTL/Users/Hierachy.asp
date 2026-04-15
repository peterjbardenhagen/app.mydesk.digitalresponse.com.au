<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
strMsg = Trim(Request("Msg"))

If Not Request.Cookies("UserSettings")("UserTypeId") => 4 Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table1">
		<tr>
			<td>
				<br/>
				<table width="100%" cellpadding=0 cellspacing=0 border=0>
					<tr>
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp">Manage Users</a> / Hierachy /></span></td>
						<td align="right"><a href="Add.asp" class="Header2">Add User</a></td>
					</tr>
				</table>
				<table>
					<tr>
						<td style="font-size:14px;">
						<br>
<%

intSpacing = 20

' Get Directors
Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Where Deleted = 0 AND UserTypeId = 6"
Set rs = dbConn.Execute(sql)

Do Until rs.EOF

%>
						<img src="/Images/Spacer.gif" width=<%= intSpacing*1 %> height=1 border=0><b><%= rs("Name") %></b> (<%= rs("Position") %>)<br>
<%

	Set rs2 = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND LineManagerCode = '" & rs("Code") & "' And Code <> '" & rs("Code") & "'"
	Set rs2 = dbConn.Execute(sql)

	Do Until rs2.EOF

%>
						<img src="/Images/Spacer.gif" width=<%= intSpacing*2 %> height=1 border=0> > <b><%= rs2("Name") %></b> - <%= rs2("Position") %><br>
<%

		Set rs3 = Server.CreateObject("ADODB.RecordSet")
		sql = "Select * From Users Where Deleted = 0 AND LineManagerCode = '" & rs2("Code") & "' And Code <> '" & rs2("Code") & "'"
		Set rs3 = dbConn.Execute(sql)

		Do Until rs3.EOF

%>
						<img src="/Images/Spacer.gif" width=<%= intSpacing*3 %> height=1 border=0> > <b><%= rs3("Name") %></b> - <%= rs3("Position") %><br>
<%


			Set rs4 = Server.CreateObject("ADODB.RecordSet")
			sql = "Select * From Users Where Deleted = 0 AND LineManagerCode = '" & rs3("Code") & "' And Code <> '" & rs3("Code") & "'"
			Set rs4 = dbConn.Execute(sql)

			Do Until rs4.EOF

%>
						<img src="/Images/Spacer.gif" width=<%= intSpacing*4 %> height=1 border=0> > <b><%= rs4("Name") %></b> - <%= rs4("Position") %><br>
<%
				Set rs5 = Server.CreateObject("ADODB.RecordSet")
				sql = "Select * From Users Where Deleted = 0 AND LineManagerCode = '" & rs4("Code") & "' And Code <> '" & rs4("Code") & "'"
				Set rs5 = dbConn.Execute(sql)

				Do Until rs5.EOF

%>
						<img src="/Images/Spacer.gif" width=<%= intSpacing*5 %> height=1 border=0> > <b><%= rs5("Name") %></b> - <%= rs5("Position") %><br>
<%

					Set rs6 = Server.CreateObject("ADODB.RecordSet")
					sql = "Select * From Users Where Deleted = 0 AND LineManagerCode = '" & rs5("Code") & "' And Code <> '" & rs4("Code") & "'"
					Set rs6 = dbConn.Execute(sql)

					Do Until rs6.EOF

%>
						<img src="/Images/Spacer.gif" width=<%= intSpacing*6 %> height=1 border=0> > <b><%= rs6("Name") %></b> - <%= rs6("Position") %><br>
<%

						rs6.MoveNext
					Loop
					
					rs6.Close
					Set rs6 = Nothing

					rs5.MoveNext
				Loop

				rs5.Close
				Set rs5 = Nothing

				rs4.MoveNext
			Loop

			rs4.Close
			Set rs4 = Nothing


			rs3.MoveNext
		Loop

		rs3.Close
		Set rs3 = Nothing

		rs2.MoveNext
	Loop
	
	rs2.Close
	Set rs2 = Nothing
	
	rs.MoveNext
Loop

rs.Close
Set rs = Nothing

%>						
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
