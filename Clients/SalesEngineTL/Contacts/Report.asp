<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strCode
Dim strName
Dim strContactName
Dim intCompanyId
Dim strCustomer
Dim strOrderBy

strCode = Trim(Request.Form("Code"))
intCompanyId = CLng(Request("CompanyId"))
strOrderBy = Trim(Request("OrderBy"))

If strOrderBy = "" Then strOrderBy = " C.Company"

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
<%

If strCode <> "All" Then
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sqlUsers = "SELECT name FROM Users WHERE Code = '" & strCode & "'"
	Set rsUsers = dbConn.Execute(sqlUsers)

	strName = rsUsers("Name")

	rsUsers.Close
	Set rsUsers = Nothing
Else
	strName = "All Users"
End If

If intCompanyId <> 0 Then
	Set rsCU = Server.CreateObject("ADODB.RecordSet")
	sql = "SELECT Company FROM Companies WHERE CompanyId = " & intCompanyId
	Set rsCU = dbConn.Execute(sql)

	strCustomer = rsCU("Company")

	rsCU.Close
	Set rsCU = Nothing
End If

%>
		<table bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table1">
			<form method="post" action="?" name="Form1">
			<input type="hidden" name="OrderBy" value="<%= strOrderBy %>" ID="Hidden1">
			<input type="hidden" name="CompanyId" value="<%= intCompanyId %>" ID="Hidden2">
			<input type="hidden" name="Code" value="<%= strCode %>" ID="Hidden3">
			<tr>
				<td><input type="button" value="Order by Company" ID="Button3" NAME="Button3" onclick="document.Form1.OrderBy.value='C.CompanyName';document.Form1.submit();"> <input type="button" value="Order by Name" ID="Button4" NAME="Button4" onclick="document.Form1.OrderBy.value='C.Surname,C.FirstName';document.Form1.submit();"> <% If (strCode = Request.Cookies("UserSettings")("Code")) Or Request.Cookies("UserSettings")("Manager") Then %><input type="button" value=" Print " onclick="print();" ID="Button2" NAME="Button1"> (Make sure that you set the orientation to landscape)<% End If %></td>
			</tr>
			</form>
		</table>
		<br>
		<table width="1000" cellpadding=3 cellspacing=0 border=0 ID="Table2">
			<tr>
				<td valign="top">
				<span class="TimesHeader">My Contacts Report for <%= strName %></span><br><br>
				<span class="TimesItalicBold"><% If intCompanyId <> 0 Then %><strong>Includes <%= strCustomer %><% Else %>Includes All companies<% End If %><br>
				As at <%= FormatDateTime(ServerToEST(Now()),1) %></span>
				</td>
			</tr>
		</table>	
<%

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT C.* FROM Contacts_WithCustomersAndSuppliers_V2 C "
If strCode <> "All" And intCompanyId <> 0 Then
	sql = sql & "WHERE Deleted = 0 AND C.Code = '" & strCode & "' AND C.CompanyId = " & intCompanyId
ElseIf strCode <> "All" And intCompanyId = 0 Then
	sql = sql & "WHERE Deleted = 0 AND C.Code = '" & strCode & "'"
ElseIf strCode = "All" And intCompanyId <> 0 Then
	sql = sql & "WHERE Deleted = 0 AND (C.Code = '" & Request.Cookies("UserSettings")("Code") & "' OR Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")) AND (C.CompanyId = " & intCompanyId & ")"
ElseIf strCode = "All" And intCompanyId = 0 Then
	sql = sql & "WHERE Deleted = 0 AND (C.Code = '" & Request.Cookies("UserSettings")("Code") & "' OR Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & "))"
End If
sql = sql & " ORDER BY " & strOrderBy
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then

%>

		<br>
		<table width=100 cellpadding=3 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td class="TimesItalicBold" width=300>Full Name</td>
				<td class="TimesItalicBold" width=300>Company & Address</td>
				<td class="TimesItalicBold" width=170>Contact</td>
				<td class="TimesItalicBold" width=230>Email</td>
			</tr>
			<tr height=1>
				<td colspan=12>
					<table width="100%" height=1 cellpadding=0 cellspacing=0 border=0 ID="Table4">
						<tr>
							<td bgcolor="#000000"><img src="/Images/Black.gif" width=994 height=1 border=0 alt=""></td>
						</tr>
					</table>
				</td>
			</tr>
<%

	Do Until rs.EOF

		If Trim(rs("FirstName")) <> "" And Trim(rs("Surname")) <> "." And Trim(rs("Surname")) <> "?" Then
			strContactName = rs("Surname") & ", " & rs("FirstName")
		Else
			strContactName = rs("FirstName")
		End If

		strAddress = ""
		If rs("Address1") <> "" Then
			strAddress = rs("Address1")
			If rs("Address2") <> "" Then
				strAddress = strAddress & ", " & rs("Address2")
			End If
			strAddress = strAddress & "<br>"
		End If
		strAddress = strAddress & " " & rs("Suburb") & " " & rs("PostCode")
		strAddress = Trim(strAddress)

%>
			<tr>
				<td style="font-size:10px;vertical-align:top;" width=300><b><% If Not(Trim(strContactName)&"" = "" Or IsNull(strContactName)) Then Response.Write(strContactName) Else Response.Write("Name entered") %></b><br><% If Not(Trim(rs("Position"))&"" = "" Or IsNull(rs("Position"))) Then Response.Write(rs("Position")) Else Response.Write("Position not entered") %></td>
				<td style="font-size:10px;vertical-align:top;" width=300><b><% If Not(Trim(rs("CompanyName"))&"" = "" Or IsNull(rs("CompanyName"))) Then Response.Write(rs("CompanyName")) Else Response.Write("Company not entered") %></b><br><% If Not(Trim(strAddress)&"" = "" Or IsNull(strAddress)) Then Response.Write(strAddress) Else Response.Write("Address not entered") %></td>
				<td style="font-size:10px;vertical-align:top;" width=170>
				P: <% If Not(Trim(rs("Phone"))&"" = "" Or IsNull(rs("Phone"))) Then Response.Write(rs("Phone")) Else Response.Write("Phone not entered") %><br>
				F: <% If Not(Trim(rs("Fax"))&"" = "" Or IsNull(rs("Fax"))) Then Response.Write(rs("Fax")) Else Response.Write("Fax not entered") %><br>
				M: <% If Not(Trim(rs("Mobile"))&"" = "" Or IsNull(rs("Mobile"))) Then Response.Write(rs("Mobile")) Else Response.Write("Mobile not entered") %><br>
				</td>
				<td style="font-size:10px;vertical-align:top;" width=230><% If Not(Trim(rs("Email"))&"" = "" Or IsNull(rs("Email"))) Then Response.Write(ConvertToEmail(rs("Email"))) Else Response.Write("Email not entered") %></td>
			</tr>
			<tr height=1>
				<td colspan=12>
					<table width="100%" height=1 cellpadding=0 cellspacing=0 border=0 ID="Table5">
						<tr>
							<td bgcolor="#000000"><img src="/Images/Black.gif" width=994 height=1 border=0 alt=""></td>
						</tr>
					</table>
				</td>
			</tr>
<%
		
		rs.MoveNext
	Loop
	
%>
		</table>
<%

Else
	Response.Write("<br><table cellpadding=3 cellspacing=0 border=0><tr><td>There are no contacts for this user</td></tr></table>")
End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
