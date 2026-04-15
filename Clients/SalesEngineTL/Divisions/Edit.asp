<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim intDivisionId
intDivisionId = CLng(Request("DivisionId"))

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
		<script language="JavaScript">

		function emptyField(textObj) {
			if (textObj.value.length == 0) return true;
			for (var i=0; i < textObj.value.length; i++) {
				var ch = textObj.value.charAt(i);
				if (ch != ' ' && ch != '\t') return false;
			}
			return true
		}

		function checkForm() {

			var validFlag = true
			
			if (validFlag) {
			if (emptyField(document.Form1.DivisionCode)) {
				alert("Please complete the Division Code field.");
				validFlag = false;
				document.Form1.DivisionCode.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.Division)) {
				alert("Please complete the Division field.");
				validFlag = false;
				document.Form1.Division.focus();
			}}

		return validFlag 
		}

		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

<%

Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Divisions Where DivisionId = " & intDivisionId
Set rs = dbConn.Execute(sql)

%>

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup">Setup</a> / <a href="Default.asp">Divisions</a> / Edit Division /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Divisions/Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<input type="hidden" value="<%= rs("DivisionId") %>" name="DivisionId">
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">ACN</td>
									<td valign="top"><input type="text" name="ACN" style="width:280px;" maxlength=50 ID="Text11" value="<%= rs("ACN") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">ABN</td>
									<td valign="top"><input type="text" name="ABN" style="width:280px;" maxlength=50 ID="Text10" value="<%= rs("ABN") %>"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Division Code</td>
									<td valign="top"><input type="text" size=50 maxlength=50 name="DivisionCode" id="DivisionCode" style="width:280px;" value="<%= rs("DivisionCode") %>"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Division</td>
									<td valign="top"><input type="text" size=50 maxlength=100 name="Division" id="Text1" style="width:280px;" value="<%= rs("Division") %>"></td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top"><b>Quotes</b></td>
									<td valign="top">
										<input type="radio" name="Quotes" id="Radio5" value="-1" <% If rs("Quotes") Then Response.Write "checked" %>> Yes<br/>
										<input type="radio" name="Quotes" id="Radio6" value="0" <% If Not rs("Quotes") Then Response.Write "checked" %>> No
									</td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top" style="font-weight:bold;">Minimum Quote Margin<br><small>Before having to be approved.</small></td>
									<td valign="top"><input type="text" name="MinimumMargin" style="width:50px;" maxlength=50 value="<% If rs("MinimumMargin") <> "" Then Response.Write(FormatNumber(rs("MinimumMargin"),2)) Else Response.Write("40.00") %>" ID="Text3">%</td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top"><b>RFQ</b></td>
									<td valign="top">
										<input type="radio" name="RFQ" id="RFQ1" value="-1" <% If rs("RFQ") Then Response.Write "checked" %>> Yes<br/>
										<input type="radio" name="RFQ" id="RFQ2" value="0" <% If Not rs("RFQ") Then Response.Write "checked" %>> No
									</td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top"><b>Prospects</b></td>
									<td valign="top">
										<input type="radio" name="Prospects" id="Prospects1" value="-1" <% If rs("Prospects") Then Response.Write "checked" %>> Yes<br/>
										<input type="radio" name="Prospects" id="Prospects2" value="0" <% If Not rs("Prospects") Then Response.Write "checked" %>> No
									</td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top"><b>Purchase Orders</b></td>
									<td valign="top">
										<input type="radio" name="PurchaseOrders" id="PurchaseOrders1" value="-1" <% If rs("PurchaseOrders") Then Response.Write "checked" %>> Yes<br/>
										<input type="radio" name="PurchaseOrders" id="PurchaseOrders2" value="0" <% If Not rs("PurchaseOrders") Then Response.Write "checked" %>> No
									</td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top"><b>Purchase Requests</b></td>
									<td valign="top">
										<input type="radio" name="PurchaseRequests" id="Radio1" value="-1" <% If rs("PurchaseRequests") Then Response.Write "checked" %>> Yes<br/>
										<input type="radio" name="PurchaseRequests" id="Radio2" value="0"  <% If Not rs("PurchaseRequests") Then Response.Write "checked" %>> No
									</td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top"><b>Users Access All</b></td>
									<td valign="top">
										<input type="radio" name="UsersAccessAll" id="Radio3" value="-1" <% If rs("UsersAccessAll") Then Response.Write "checked" %>> Yes<br/>
										<input type="radio" name="UsersAccessAll" id="Radio4" value="0"  <% If Not rs("UsersAccessAll") Then Response.Write "checked" %>> No
									</td>
								</tr>
								<tr>
									<td valign="top"></td>
									<td valign="top"><b>Logo</b></td>
									<td valign="top"><input type="text" name="Logo" style="width:280px;" maxlength=50 ID="Text2" value="<%= rs("Logo") %>"></td>
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
<%

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->