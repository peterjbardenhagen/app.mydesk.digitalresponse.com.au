<% 

Response.AddHeader "Pragma", "No-Store"
Response.ExpiresAbsolute = ServerToEST(Now()) - 1
Response.AddHeader "pragma","no-cache"
Response.AddHeader "cache-control","private"
Response.CacheControl = "no-cache"

If Not Request.Cookies("UserSettings")("Manager") Then Response.Redirect("../Portal/AccessDenied.asp")

Dim strSort
Dim strFilter_Code

intDivisionId = CInt(Request("DivisionId"))
strLetter = Trim(Request("Letter"))

If intDivisionId = 0 Then intDivisionId = CInt(Request.Cookies("DivisionId"))
If strLetter = "" Then strLetter = "A"

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<!DOCTYPE html>
<html lang="en">
	<head>
		<title>Companies - Techlight MyDesk</title>
		<meta charset="UTF-8">
		<meta name="viewport" content="width=device-width, initial-scale=1.0">
		<meta http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate">
		<meta http-equiv="Expires" content="0">
		<meta http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="preconnect" href="https://fonts.googleapis.com">
		<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
		<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Style_Techlight.css">
		<link rel="stylesheet" type="text/css" href="/System/Style_Modern.css">
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
	</head>
	<body class="tl-bg-light">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
	<div class="tl-page-container">
		<nav class="tl-breadcrumb">
			<a href="/Portal.asp">Home</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a>
			<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"></polyline></svg>
			<span>Companies</span>
		</nav>

		<div class="tl-action-bar">
			<h1 class="tl-page-title">Companies</h1>
			<a href="Add.asp" class="tl-btn tl-btn-primary">
				<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"></line><line x1="5" y1="12" x2="19" y2="12"></line></svg>
				Add Company
			</a>
		</div>

		<div class="tl-main">
<%
strMsg = Trim(Request("Msg"))
If strMsg <> "" Then
%>
						<div class="tl-alert tl-alert-info" style="margin-bottom: 20px;">
							<%= strMsg %>
						</div>
<%
End If
%>
						<div class="tl-card" style="margin-bottom: 24px;">
							<form name="FormReport" id="FormReport" method="post" action="IFrame.asp" target="MyIFrame" style="padding: 24px;">
								<div style="display: flex; gap: 24px; align-items: flex-end;">
									<div class="tl-form-group" style="flex: 1; margin-bottom: 0;">
										<label class="tl-label">Division</label>
										<select name="DivisionId" ID="Select1" class="tl-input">
											<option value="555">All Divisions</option>
<%
Set rsDiv = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Divisions WHERE DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") ORDER BY Division"
Set rsDiv = dbConn.Execute(sql)

If Not(rsDiv.BOF And rsDiv.EOF) Then
	Do Until rsDiv.EOF
		If CLng(intDivisionId) = CLng(rsDiv("DivisionId")) Then
			Response.Write ("								<option selected value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		Else
			Response.Write ("								<option value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		End If
		rsDiv.MoveNext
	Loop
End If
rsDiv.Close
Set rsDiv = Nothing
%>
										</select>
									</div>

									<div class="tl-form-group" style="width: 200px; margin-bottom: 0;">
										<label class="tl-label">Starts With</label>
										<select name="Letter" ID="Select2" class="tl-input">
											<option value="">Any</option>
											<%
											For i = 65 To 90
												char = Chr(i)
												Response.Write("<option value=""" & char & """" & IIf(strLetter = char, " selected", "") & ">" & char & "</option>" & vbNewLine)
											Next
											For i = 0 To 9
												Response.Write("<option value=""" & i & """" & IIf(strLetter = CStr(i), " selected", "") & ">" & i & "</option>" & vbNewLine)
											Next
											%>
										</select>
									</div>

									<button type="submit" class="tl-btn tl-btn-primary" style="height: 42px;">
										<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="8"></circle><line x1="21" y1="21" x2="16.65" y2="16.65"></line></svg>
										Filter
									</button>
								</div>
							</form>
						</div>

						<div class="tl-card" style="padding: 0; overflow: hidden; height: calc(100vh - 350px); min-height: 400px;">
							<iframe width="100%" height="100%" name="MyIFrame" id="MyIFrame" src="IFrame.asp?Cache=<%= rnd() %>&CurPage=<%= CurPage %>&Letter=<%= strLetter %>&DivisionId=<%= intDivisionId %>" style="border: none;"></iframe>
						</div>
		</div>
	</div>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->