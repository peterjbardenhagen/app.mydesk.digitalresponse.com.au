<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
strMsg = Trim(Request("Msg"))

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
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / Employment /></span></td>
<%
If Request.Cookies("UserSettings")("Manager") Then
%>
						<td align="right"><a href="Add.asp" class="Header2">Add Employment Opportunity</a></td>
<%
End If
%>
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
If Request.Cookies("UserSettings")("Manager") Then
	sql = "Select Employment.*, Users.*, Divisions.* From (Employment INNER JOIN Users ON Employment.Code = Users.Code) INNER JOIN Divisions ON Employment.DivisionId = Divisions.DivisionId Order By DateEntered Desc"
Else
	sql = "Select Employment.*, Users.*, Divisions.* From (Employment INNER JOIN Users ON Employment.Code = Users.Code) INNER JOIN Divisions ON Employment.DivisionId = Divisions.DivisionId Where Employment.DateExpires >= #" & ServerToEST(Now()) & "# Order By DateEntered Desc"
End If
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then

%>
							<br/>
							<table width="100%" cellpadding=10 cellspacing=0 border=0 ID="Table3">
								<tr>
									<td class="ListHeaderRow" width=190><b>Date</b></td>
									<td class="ListHeaderRow"><b>Title</b></td>
									<td class="ListHeaderRow" width=150><b>By</b></td>
<%

	If Request.Cookies("UserSettings")("Manager") Then

%>
									<td class="ListHeaderRow" align="right" width=75><b>Action</b></td>
<%

	End If

%>
								</tr>
<%

	Do Until rs.EOF

%>
								<tr bgcolor="#ffffff">
									<td style="color:black;border-bottom:1px solid black;" width=190 valign="top"><%= FormatDateTime(rs("DateEntered"),1) %></td>
									<td style="color:black;border-bottom:1px solid black;" valign="top"><b><%= rs("Division") %><br><%= rs("Title") %></b><br/><%= Replace(rs("Details"), Chr(10), "<br>") %><br><br>This notice will be removed on <%= FormatDateU(rs("DateExpires"), False) %></td>
									<td style="color:black;border-bottom:1px solid black;" valign="top" width=150><%= rs("Name") %></td>
<%

If Request.Cookies("UserSettings")("Manager") Then
	If SearchArray(Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), rs("DivisionId")) Then
%>
									<td style="color:black;border-bottom:1px solid black;text-align:right;" width=75 valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Employment/Edit.asp?EmploymentId=<%= rs("EmploymentId") %>">Edit</a> | <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Employment/Del_Proc.asp?EmploymentId=<%= rs("EmploymentId") %>">Delete</a></td>
<%

	Else
%>
									<td style="color:black;border-bottom:1px solid black;text-align:right;">&nbsp;</td>
<%
	End If
End If
		rs.MoveNext
	Loop
%>
							</table>
<%

Else

%>
							<br><p>There are no Employment Opportunities</p>
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
