<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
strMsg = Trim(Request("Msg"))

If Not Request.Cookies("UserSettings")("Manager") Then
	Response.Redirect("../Portal/AccessDenied.asp")
End If

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
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
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="../Setup" class="Header2">Setup</a> / Copy Contacts /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
<%

If strMsg <> "" Then

%>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table3">
								<tr>
									<td><span style="color:red;"><%= strMsg %></span></td>
								</tr>
							</table>
							<br>
<%

End If

%>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="Default_Proc.asp" method="post" name="Form1" ID="Form1">
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Copy From</td>
									<td valign="top">
									<select name="CopyFromCode" ID="Select2">
<%
Set rsUsers = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Order By Name"
Set rsUsers = dbConn.Execute(sql)

If Not(rsUsers.BOF And rsUsers.EOF) Then
	Do Until rsUsers.EOF
		If rsUsers("Code") = strFilter_Code Then
%>
										<option selected value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
		Else
%>
										<option value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
		End If	
		rsUsers.MoveNext
	Loop
End If

rsUsers.Close
Set rsUsers = Nothing
%>
									</select>
									</td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Copy To</td>
									<td valign="top">
									<select name="CopyToCode" ID="Select1">
<%
Set rsUsers = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Users Order By Name"
Set rsUsers = dbConn.Execute(sql)

If Not(rsUsers.BOF And rsUsers.EOF) Then
	Do Until rsUsers.EOF
		If rsUsers("Code") = strFilter_Code Then
%>
										<option selected value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
		Else
%>
										<option value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
		End If	
		rsUsers.MoveNext
	Loop
End If

rsUsers.Close
Set rsUsers = Nothing
%>
									</select>
									</td>
								</tr>
								<tr>
									<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};">&nbsp;<input type="submit" value="Submit" id="Submit" NAME="Submit"></td>
								</tr>
								</form>
							</table>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->