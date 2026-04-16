<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("UserSettings")("Manager") Then MyRedirect(Request.Cookies("ClientSettings")("WorkingDir") & "/Reports/SalesReportGenAccessDenied.asp")

Dim intYear, strCode
Dim sql
Dim decSalesBudget
Dim dteBegin, dteEnd

intYear = CLng(Request("Year"))
strCode = Trim(Request("Code"))
dteBegin = CStr("01-Jul-20" & MakePadding(Right(intYear, 2), "", 2))
dteEnd = CStr("30-Jun-20" & MakePadding(Right(intYear+1, 2), "", 2))

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

If Not strCode = "All" Then
	Set rsUser = Server.CreateObject("ADODB.RecordSet")
	sql = "Select * From Users Where Deleted = 0 AND Code = '" & strCode & "'"
	rsUser.Open sql, dbConn

	If Not(rsUser.BOF And rsUser.EOF) Then
		decSalesBudget = CDbl(rsUser("SalesBudget"))
		intDivisionId = CLng(rsUser("DivisionId"))
	End If

	If IsObject(rsUser) Then
		rsUser.Close
		Set rsUser = Nothing
	End If
Else

%>
<script language="javascript">
	alert('No data available for All Sales People report. At least one person\'s figures for the year needs to be entered.');
	document.location.href='SalesReportGen.asp';
</script>
<%

End If

%>
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

<%

Dim y


%>
			if (validFlag) {
			if (emptyField(document.Form1.DateExpires)) {
				alert("Please select Date Expires.");
				validFlag = false;
				document.Form1.DateExpires.focus();
			}}
			
			if (validFlag) {
			if (emptyField(document.Form1.Heading)) {
				alert("Please complete the Heading field.");
				validFlag = false;
				document.Form1.Heading.focus();
			}}

		return validFlag 
		}

		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp">Reports</a> / <a href="SalesReportGen.asp">Sales Reports</a> / Enter Data /></span>
				<table width=100% cellpadding=0 cellspacing=0 border=0 ID="Table1">
					<tr>
						<td>
							<table width="100%" cellpadding=3 cellspacing=0 border=0 ID="Table3">
								<tr>
									<td>
									<br>
<%

Set rsSalesCheck = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From SalesResults_ByCustomer Where Code = '" & strCode & "' And [Date] >= #" & dteBegin & "# And [Date] <= #" & dteEnd & "#"
Set rsSalesCheck = dbConn.Execute(sql)

If (rsSalesCheck.BOF And rsSalesCheck.EOF) Then

%>
                                    You cannot continue as the sales data has not yet been entered.
<%

Else

%>
										<table cellpadding=3>
											<form method="post" action="SalesReportEnterData_Proc.asp">
											<input type="hidden" name="Code" value="<%= strCode %>">
											<input type="hidden" name="Year" value="<%= intYear %>">
											<tr>
												<td style="font-weight:bold;">Month</td>
												<td style="font-weight:bold;">Budget</td>
											</tr>
<%

    i = 1
    x = 7

    Do Until i = 13

%>
											<tr>
												<td style="font-weight:bold;"><%= MonthName(x, False) %></td>
												<td>$<input type="text" size=10 name="SalesBudget<%= x %>" value="<%= decSalesBudget %>" tabindex=<%= i + 12 %>></td>
											</tr>									
<%

	    i = i + 1
	    x = x + 1
	    If x = 13 Then x = 1
    Loop

%>
							
											<tr>
												<td colspan=3 align="right"><br><input type="submit" value="Enter data & generate"></td>
											</tr>
											</form>
										</table>
									<br><br>
									Click <a href="SalesReportGen.asp">here</a> to generate another report.
									</td>
								</tr>
							</table>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
<%

End If

rsSalesCheck.Close
Set rsSalesCheck = Nothing

%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->