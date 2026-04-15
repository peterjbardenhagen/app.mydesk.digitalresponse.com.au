<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
strMsg = Trim(Request("Msg"))

If Not Request.Cookies("UserSettings")("UserTypeId") = 6 Then
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
				<table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table5">
					<tr>
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a> / Divisions /></span></td>
						<td align="right"><a href="Add.asp" class="Header2">Add Division</a></td>
					</tr>
				</table>
				<table width=100% cellpadding=0 cellspacing=0 border=0 ID="Table1">
					<tr>
						<td>
<%

If strMsg <> "" Then

%>
							<br>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table2">
								<tr>
									<td><span style="color:red;"><%= strMsg %></span></td>
								</tr>
							</table>
<%

End If

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Order By DivisionCode, Division"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then

%>
							<br/>
							<table width="100%" cellpadding=10 cellspacing=0 border=0 ID="Table3">
								<tr>
									<td class="ListHeaderRow" width=130><b>Division Code</b></td>
									<td class="ListHeaderRow"><b>Division</b></td>
									<td class="ListHeaderRow" align="right" width=75><b>Action</b></td>
								</tr>
<%

	Do Until rs.EOF

%>
								<tr bgcolor="#ffffff">
									<td style="color:black;border-bottom:1px solid black;" width=130 valign="top"><%= rs("DivisionCode") %></td>
									<td style="color:black;border-bottom:1px solid black;" valign="top"><%= rs("Division") %></td>
									<td style="color:black;border-bottom:1px solid black;text-align:right;" width=75 valign="top"><a href="Edit.asp?DivisionId=<%= rs("DivisionId") %>">Edit</a> | <a href="Del_Proc.asp?DivisionId=<%= rs("DivisionId") %>">Delete</a></td>
								</tr>
<%

		rs.MoveNext
	Loop

%>
							</table>
<%

Else

%>
							<br><p>There are no Divisions</p>
<%

End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>	
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>

	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
