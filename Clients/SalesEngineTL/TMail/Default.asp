<% 

Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

Dim strCode
Dim strMsg

strCode = Trim(Request.Cookies("UserSettings")("Code"))
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
		<!--<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>-->
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->
	<center>
	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0>
		<tr>
			<td>
				<br>
				<table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table5">
					<tr>
						<td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / T-Mail /></span></td>
						<td align="right"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/TMail/Add.asp" class="Header2">Add Message</a></td>
					</tr>
				</table>
<%

If strMsg <> "" Then

%>
				<br>
				<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table4">
					<tr>
						<td><span style="color:red;"><%= strMsg %></span></td>
					</tr>
				</table>
<%

End If

%>
				<table width=760 ID="Table3">
					<tr>
						<td>
							<fieldset style="width:760px;">
								<legend style="font-weight:bold;">Filter Results</legend>
								<table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table1">
									<form name="FormReport" id="FormReport" method="post" action="IFrame.asp?Cache=<%= rnd() %>" target="MyIFrame">
									<tr>
										<td style="font-weight:bold;">Date From</td>
										<td valign="top"><input type="input" value="<%= FormatDateU(DateAdd("M", -1, ServerToEST(Now())), False) %>" name="DateFrom" readonly ID="Input1"> <a href="javascript:showCal('Calendar3')"><img src="/Images/Calendar.gif" border=0></a></td>
<%
If Request.Cookies("UserSettings")("Manager") Then
%>
										<td style="font-weight:bold;">User</td>
										<td>
											<select name="Code" ID="Select4">
											<option value="All">All users</option>
<%
	Set rsUsers = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND (Code In (" & GetAccessCodesList(Request.Cookies("UserSettings")("Code"), Request.Cookies("UserSettings")("UserTypeID")) & ")) Order By Name"
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
<%
End If
%>
									</tr>
									<tr>
										<td style="font-weight:bold;">Date To</td>
										<td valign="top"><input type="input" value="<%= FormatDateU(DateAdd("D", 1, ServerToEST(Now())), False) %>" name="DateTo" readonly ID="Input2"> <a href="javascript:showCal('Calendar4')"><img src="/Images/Calendar.gif" border=0></a></td>
										<td></td>
										<td align="right"><input type="submit" value="Filter" ID="Submit1" NAME="Submit1"></td>
									</tr>
								</form>
								</table>
							</fieldset>
						</td>
					</tr>
				</table>

				<table width=100% cellpadding=0 cellspacing=0 border=0 ID="Table2">
					<tr>
						<td>
						<iframe name="MyIFrame" id="MyIFrame" width=100% height=250 src="IFrame.asp?Cache=<%= rnd() %>&Sort=<%= strSort %>&CurPage=<%= CurPage %>&Code=<%= strCode %>&DateFrom=<%= DateAdd("M", -1, ServerToEST(Now())) %>&DateTo=<%= DateAdd("D", 1, ServerToEST(Now())) %>"></iframe>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
	</center>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->