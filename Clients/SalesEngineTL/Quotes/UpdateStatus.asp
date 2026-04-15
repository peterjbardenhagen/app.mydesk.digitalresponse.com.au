<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

SetWorkingDir Request.ServerVariables("URL")

Dim lngQid
Dim sql
Dim rs
Dim lngQuoteStatusId

lngQid = CLng(Request("Qid"))

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/Var.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

Set rsQu = Server.CreateObject("ADODB.RecordSet")
sql = "Select Quotes.*, Quotes.CustomerNotes As CN, Quotes.DivisionId As QDivisionId, [Users].LocationId, [Users].Name, [Users].Email, [Users].Phone, [Users].Mobile, [Users].Fax, QuoteCOS.QuoteCOSFile, QuoteStatus.QuoteStatus From ((Quotes INNER JOIN Users ON Quotes.Code = Users.Code) INNER JOIN QuoteStatus ON Quotes.QuoteStatusId = QuoteStatus.QuoteStatusId) LEFT OUTER JOIN QuoteCOS ON Quotes.QuoteCOSId = QuoteCOS.QuoteCOSId Where Qid = " & lngQid
Set rsQu = dbConn.Execute(sql)

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Quotes Inner Join QuoteStatus QS On QS.QuoteStatusId = Quotes.QuoteStatusId Where Qid = " & lngQid
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
	lngQuoteStatusId = CLng(rs("QuoteStatusId"))
	strQuoteStatus = rs("QuoteStatus")
End If

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
<html>
	<head>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<style>
			body, p, td, th, select
			{
				font-family: Arial;
				font-size: 12px;
			}
			input
			{
				font-family: Arial;
				font-size: 10px;
			}
			.Header4
			{
				font-family: Arial;
				font-size: 14px;
				font-weight: bold;
				color: #000000;
			}
		</style>
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
			if (emptyField(document.Form1.QuoteStatusId)) {
				alert("Please select New Quote Status.");
				validFlag = false;
				document.Form1.QuoteStatusId.focus();
			}}

			return validFlag;
		}

		var itemLines=1;
		</script>
	</head>
	<body Marginheight=0 Marginwidth=2 topMargin=0 leftMargin=2>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td>
				<!--#include file="NavBar.asp"-->
				</td>
			</tr>
		</table>
		<table width="100%" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table3">
			<tr>
				<td bgcolor="#DDDDDD" class="Header4">Update Quote Status</td>
				<td bgcolor="#DDDDDD" align="right"><input type="button" value=" Close [x] " onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1"></td>
			</tr>
			<tr>
				<td colspan=2>
					<table cellpadding=5 cellspacing=0 border=0>
						<form name="Form1" id="Form1" method="post" action="UpdateStatus_Proc.asp" onsubmit="return checkForm();">
						<input type="hidden" name="Qid" value="<%= lngQid %>">
						<tr>
							<td></td>
							<td style="font-weight:bold;">Quote #</td>
							<td><%= lngQid %></td>
						</tr>
						<tr>
							<td></td>
							<td style="font-weight:bold;">Current Quote Status</td>
							<td><%= strQuoteStatus %></td>
						</tr>
						<tr>
							<td class="Req">*</td>
							<td style="font-weight:bold;">New Quote Status</td>
							<td>
							<select name="QuoteStatusId" style="width:280px;" ID="Select2">
								<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%
	Set rsStatus = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From QuoteStatus Order By QuoteStatus"
	Set rsStatus = dbConn.Execute(sql)

	If Not(rsStatus.BOF And rsStatus.EOF) Then
		Do Until rsStatus.EOF
			If (GetQuoteLastLineApprover(lngQid) = "Already Approved" Or Request.Cookies("UserSettings")("UserTypeId") = 6 Or lngQuoteStatusId = 2 Or lngQuoteStatusId = 4 Or lngQuoteStatusId = 7 Or lngQuoteStatusId = 10) Or (rsStatus("QuoteStatusId") <> 2 And rsStatus("QuoteStatusId") <> 3 And rsStatus("QuoteStatusId") <> 4 And rsStatus("QuoteStatusId") <> 5 And rsStatus("QuoteStatusId") <> 7 And rsStatus("QuoteStatusId") <> 10 And rsStatus("QuoteStatusId") <> 11) Then
				If (CLng(rsStatus("QuoteStatusId")) = lngQuoteStatusId) Then
%>
								<option selected value="<%= rsStatus("QuoteStatusId") %>"><%= rsStatus("QuoteStatus") %></option>
<%
				Else
%>
								<option value="<%= rsStatus("QuoteStatusId") %>"><%= rsStatus("QuoteStatus") %></option>
<%
				End If
			End If
			rsStatus.MoveNext
		Loop
	End If

	If IsObject(rsStatus) Then
		rsStatus.Close
		Set rsStatus = Nothing
	End If

%>
							</select>
							</td>
						</tr>
						<tr>
							<td colspan=3 align="right"><br><input type="submit" value="Submit"></td>
						</tr>
						</form>
					</table>
				</td>
			</tr>
		</table>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->