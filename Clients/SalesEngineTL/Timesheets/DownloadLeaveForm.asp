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
				<span class="Header2"><a href="/Portal.asp">Home</a> / <a href="Default.asp" class="Header2">Timesheets</a> / Download Leave Form /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="Add_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<input type="hidden" name="Leave" value="1">
								<tr>
									<td colspan=3>
									<p><b>Timesheet saved.</b></p>
<%

If Request.Cookies("DivisionId") = 3 Then

%>
									<li><a href="../FilesLibrary/Files/TSA_Leave.pdf" target="_blank">Download a leave application form</a>. <b>Your leave will not be approved until you have submitted a leave request form.</b><br><br>
<%

ElseIf Request.Cookies("DivisionId") = 4 Then

%>
									<li><a href="../FilesLibrary/Files/Deneefe_Leave.pdf" target="_blank">Download a leave application form</a>. <b>Your leave will not be approved until you have submitted a leave request form.</b><br><br>
<%

Else

%>
									<p>No leave form available here.</p>
<%

End If

%>
									<br>Click <a href="default.asp">here</a> to go back to Timesheets list.
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
