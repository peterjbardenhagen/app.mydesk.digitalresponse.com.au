<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
Dim strCode

strMsg = Trim(Request("Msg"))
strCode = Trim(Request("Code"))

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
		<script language="javascript">
			function checkForm() {
				if(document.Form1.Code[document.Form1.Code.selectedIndex].value.substring(0,8) == 'Division') {
					document.Form1.action = 'SalesReport_All.asp';
				} else {
					document.Form1.action = 'SalesReport.asp';
				}
			}
		</script>
	</head>
	<body bgcolor="#dddddd">
<!--#include virtual="/System/ssi_Header.inc"-->
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp">Reports</a> / Sales Reports /></span>
<%

If strMsg <> "" Then

%>
				<br><br>
				<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table2">
					<tr>
						<td><span style="color:red;"><%= strMsg %></span></td>
					</tr>
				</table>
<%

End If

%>
<!--			<br><br><li><a href="Upload.asp">Upload data from CSV file</a>-->
				<table width=100% cellpadding=0 cellspacing=0 border=0 ID="Table1">
					<tr>
						<td>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table3">
								<tr>
									<td>
									<br>
										<table width=350>
										<form name="Form1" method="post" action="SalesReport.asp" onsubmit="checkForm();">
											<tr>
												<td width=100 style="font-weight:bold;">Person</td>
												<td align="right">
												<select name="Code" style="width:250px;">
<%

	' Disable
	If 1 = 2 And Request.Cookies("UserSettings")("Manager") Then
		Set rsDI = Server.CreateObject("ADODB.RecordSet")
		sql = "Select * From Divisions Where DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") Order By Division"
		Set rsDI = dbConn.Execute(sql)
		
		If Not(rsDI.BOF And rsDI.EOF) Then
			Do Until rsDI.EOF
				If strCode = "Division_" & rsDI("DivisionId") Then
%>
													<option selected value="Division_<%= rsDI("DivisionId") %>">All sales people at <%= rsDI("Division") %></option>
<%
				Else
%>
													<option value="Division_<%= rsDI("DivisionId") %>">All sales people at <%= rsDI("Division") %></option>
<%
				End If
				rsDI.MoveNext
			Loop
			If IsObject(rsDI) Then
				rsDI.Close
				Set rsDI = Nothing
			End If
		End If
	End If

	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND (Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")) Order By Name"
	Set rsUsers = dbConn.Execute(sql)

	If Not(rsUsers.BOF And rsUsers.EOF) Then
		Do Until rsUsers.EOF
%>
													<option <% If strCode = rsUsers("Code") Then Response.Write("selected") %> value="<%= rsUsers("Code") %>"><%= rsUsers("Name") %></option>
<%
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
												<td width=100 style="font-weight:bold;">Year</td>
												<td align="right">
													<select name="Year" style="width:250px;">
<%

	Dim intYear
	For intYear = 2006 To Year(Now)
		Response.Write("														<option value=""" & intYear & """>" & intYear & "/" & Right(intYear+1,2) & "</option>")
	Next

%>
													</select>
												</td>
											</tr>
											<tr>
												<td colspan=2 align="right"><input type="submit" value="Generate"></td>
											</tr>
										</form>
										</table>
									</td>
								</tr>
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