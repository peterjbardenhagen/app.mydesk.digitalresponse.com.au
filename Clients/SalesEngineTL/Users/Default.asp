<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
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
<%

GetAccessCodesList Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeId")

%>
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
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / Manage Users /></span></td>
						<td align="right"><a href="Add.asp" class="Header2">Add User</a></td>
					</tr>
				</table>
<%

If strMsg <> "" Then

%>
							<br>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table3">
								<tr>
									<td><span style="color:red;"><%= strMsg %></span></td>
								</tr>
							</table>
<%

End If

%>
				<br/>
				<table width=100% cellpadding=10 cellspacing=0 border=0 ID="Table2">
<%

Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT U.*, D.Division, D.DivisionCode FROM Users U INNER JOIN Divisions D On D.DivisionId = U.DivisionId"
sql = sql & " WHERE U.Deleted = 0 AND U.Active = 1 AND U.Code IN (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")"
If Request.Cookies("UserSettings")("UserTypeId") <= 5 Then
	sql = sql & " AND U.UserTypeId <= " & Request.Cookies("UserSettings")("UserTypeId")
End If
sql = sql & " ORDER BY U.Name"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then

%>
					<tr>
						<td class="ListHeaderRow" width=150><b>Name</b></td>
						<td class="ListHeaderRow" width=50><b>Code</b></td>
						<td class="ListHeaderRow"><b>Division</b></td>
						<td class="ListHeaderRow" width=100><b>Position</b></td>
						<td class="ListHeaderRow" width=50><b>Active</b></td>
						<td class="ListHeaderRow" width=50><b>Manager</b></td>
						<td class="ListHeaderRow" width=90><b>User Type</b></td>
						<td class="ListHeaderRow" align="right" width=125><b>Action</b></td>
					</tr>
<%

	Do Until rs.EOF

%>
					<tr>
						<td style="background-color:#eeeeee;color:black;border-bottom:1px solid white;font-size:12px;vertical-align:top;" width=150><a href="Edit.asp?UserId=<%= rs("UserId") %>"><%= rs("Name") %></td>
						<td style="background-color:#eeeeee;color:black;border-bottom:1px solid white;font-size:12px;vertical-align:top;" width=50><%= rs("Code") %></td>
						<td style="background-color:#eeeeee;color:black;border-bottom:1px solid white;font-size:12px;vertical-align:top;"><%= rs("DivisionCode") %></td>
						<td style="background-color:#eeeeee;color:black;border-bottom:1px solid white;font-size:12px;vertical-align:top;"><%= rs("Position") %>&nbsp;</td>
						<td style="background-color:#eeeeee;color:black;border-bottom:1px solid white;font-size:12px;vertical-align:top;" width=50><% If rs("Active") Then Response.Write("Yes") Else Response.Write("No") %></td>
						<td style="background-color:#eeeeee;color:black;border-bottom:1px solid white;font-size:12px;vertical-align:top;" width=50><% If CheckIfAdmin(rs("UserId")) Then Response.Write("Yes") Else Response.Write("No") %></td>
						<td style="background-color:#eeeeee;color:black;border-bottom:1px solid white;font-size:12px;vertical-align:top;" width=90><%= rs("UserTypeId") %></td>
						<td style="background-color:#eeeeee;color:black;border-bottom:1px solid white;font-size:12px;vertical-align:top;" align="right" width=125><input type="button" onclick="document.location.href='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Users/Edit.asp?UserId=<%= rs("UserId") %>';" value="Edit"/>&nbsp;<input type="button" onclick="document.location.href = '<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Users/Del_Proc.asp?Code=<%= rs("Code") %>';" value="Delete"/></td>
					</tr>
<%

		rs.MoveNext
	Loop

End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
				</table>
			</td>
		</tr>
	</table>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
