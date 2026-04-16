<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

intDivisionId = CInt(Request("DivisionId"))


Response.Redirect("Add2.asp?DivisionId=1")
Response.End

%>
<!--#include virtual="/System/ssi_Security.inc"-->
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
		<script language="JavaScript">

		function emptyField(textObj) {
			if (textObj.value.length == 0) return true;
			for (var i=0; i < textObj.value.length; i++) {
				var ch = textObj.value.charAt(i);
				if (ch != ' ' && ch != '\t') return false;
			}
			return true
		}

		// Check form for validation errors
		function checkForm() {

			var validFlag = true;

			if (validFlag) {
			if (emptyField(document.Form1.DivisionId)) {
				alert("Please select a Division.");
				validFlag = false;
				document.Form1.DivisionId.focus();
			}}

			return validFlag;
		}
		</script>
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
	</head>
	<body class="tl-bg-light">
<!--#include virtual="/Clients/SalesEngineTL/Header.asp"-->
	<div class="tl-main">
		<div class="tl-page-header">
			<div class="tl-breadcrumb">
				<a href="/Portal.asp">Home</a>
				<span class="tl-breadcrumb-separator">/</span>
				<a href="Default.asp">Quotes</a>
				<span class="tl-breadcrumb-separator">/</span>
				<span class="tl-breadcrumb-current">New Quote</span>
			</div>
			<h1 class="tl-page-title">Create Quote</h1>
			<p class="tl-page-subtitle">Select a division to begin</p>
		</div>

		<div class="tl-card" style="max-width: 600px;">
			<form method="post" name="Form1" action="Add2.asp" onSubmit="return checkForm();">
				<div class="tl-card-body">
					<div class="tl-form-group">
						<label class="tl-label tl-label-required">Division</label>
						<select name="DivisionId" class="tl-select">
							<option value="">Select a division...</option>
<%
Set rsDiv = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Divisions WHERE Quotes = True AND DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Quotes") & ") ORDER BY Division"
Set rsDiv = dbConn.Execute(sql)

If Not(rsDiv.BOF And rsDiv.EOF) Then
	Do Until rsDiv.EOF
		If CInt(Request.Cookies("DivisionId")) = CInt(rsDiv("DivisionId")) Then
			Response.Write ("							<option selected value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		Else
			Response.Write ("							<option value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		End If
		rsDiv.MoveNext
	Loop
End If

If IsObject(rsDiv) Then
	rsDiv.Close
	Set rsDiv = Nothing
End If
%>
						</select>
					</div>
				</div>
				<div class="tl-card-footer">
					<button type="button" class="tl-btn tl-btn-secondary" onclick="if(confirm('Are you sure you want to cancel?')){document.location.href='default.asp';};">Cancel</button>
					<button type="submit" class="tl-btn tl-btn-primary" ID="Submit2" NAME="Submit1">Continue</button>
				</div>
			</form>
		</div>
	</div>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
