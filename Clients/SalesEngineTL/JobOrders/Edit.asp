<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

lngJobOrderId = CLng(Request("JobOrderId"))
lngJobOrderContentId = CLng(Request("JobOrderContentId"))
boolTP = CBool(Request("TP"))

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

If boolTP Then
	sql = "Select *, JobOrderThirdPartyId As JobOrderContentId From JobOrderThirdPartyContents Where JobOrderThirdPartyId = " & lngJobOrderContentId
Else
	sql = "Select * From JobOrderContents Where JobOrderContentId = " & lngJobOrderContentId
End If
Set rs = dbConn.Execute(sql)

%>
<html>
	<head>
		<title>MyDesk</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Quotes.css">
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/JobOrders.js"></script>
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
		<script language="javascript">
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
				if (emptyField(document.Form1.JobOrderStatusCode)) {
					alert("Please select Status.");
					validFlag = false;
					document.Form1.JobOrderStatusCode.focus();
				}}

				if (validFlag) {
				if (emptyField(document.Form1.Comment)) {
					alert("Please enter Comment.");
					validFlag = false;
					document.Form1.Comment.focus();
				}}

				return validFlag;
			}
		</script>
	</head>
	<body bgcolor="#ffffff">
<!--#include virtual="/System/ssi_Header.inc"-->
	</body>
		<table width="100%" bgcolor="#DDDDDD" class="NoPrint" width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table4">
			<tr>
				<td><input type="button" onclick="window.close();" value="Close [x]"></td>
			</tr>
		</table>
		<table width="100%" cellpadding=5 cellspacing=0 border=0>
			<tr>
				<td width=500>
					<table width=500 cellpadding=5 cellspacing=0 border=0 ID="Table1">
						<form method="post" action="Edit_Proc.asp" name="Form1" ID="Form1" onSubmit="return checkForm();">
						<input type="hidden" name="JobOrderId" value="<%= Request("JobOrderId") %>" ID="Hidden1">
						<input type="hidden" name="JobOrderContentId" value="<%= Request("JobOrderContentId") %>" ID="Hidden2">
						<input type="hidden" name="TP" value="<%= Request("TP") %>" ID="Hidden3">
						<tr>
							<td style="font-size:16px;">Update Job <%= rs("JobOrderContentId") %></td>
						</tr>
						<tr>
							<td colspan=2>
								<table width="100%" cellpadding=2 cellspacing=0 border=0>
									<tr>
										<td colspan=2 style="background-color:#cccccc;font-weight:bold;">Job details</td>
									</tr>
									<tr>
										<td style="vertical-align:top;width:190px;font-size:12px;font-weight:bold;">Product Code</td>
										<td style="vertical-align:top;font-size:12px;"><%= rs("ProductCode") %></td>
									</tr>
									<tr>
										<td style="vertical-align:top;width:190px;font-size:12px;font-weight:bold;">Description</td>
										<td style="vertical-align:top;font-size:12px;"><%= rs("Description") %></td>
									</tr>
									<tr>
										<td style="vertical-align:top;width:190px;font-size:12px;font-weight:bold;">Unit Cost</td>
										<td style="vertical-align:top;font-size:12px;"><%= rs("UnitCost") %></td>
									</tr>
									<tr>
										<td style="vertical-align:top;width:190px;font-size:12px;font-weight:bold;">Nett Price</td>
										<td style="vertical-align:top;font-size:12px;"><%= rs("NettPrice") %></td>
									</tr>
									<tr>
										<td style="vertical-align:top;width:190px;font-size:12px;font-weight:bold;">Date Delivery Requested</td>
										<td style="vertical-align:top;font-size:12px;"><% If rs("DateDeliveryRequested") <> "1/1/1900" Then Response.Write rs("DateDeliveryRequested") Else Response.Write("Not set") %></td>
									</tr>
									<tr>
										<td style="vertical-align:top;width:190px;font-size:12px;font-weight:bold;">Date Delivery Scheduled</td>
										<td style="vertical-align:top;font-size:12px;"><% If rs("DateDeliveryScheduled") <> "1/1/1900" Then Response.Write(rs("DateDeliveryScheduled")) Else Response.Write("Not set") %></td>
									</tr>
								</table>
							</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;color:red;">Status</td>
							<td valign="top">
								<select name="JobOrderStatusCode" ID="Select1" style="width:100%;">
									<option value="">Select a status</option>
<%

sql = "Select * From JobOrderStatus Order By JobOrderStatus"
Set rsC = dbConn.Execute(sql)

If Not(rsC.BOF And rsC.EOF) Then
	Do Until rsC.EOF

%>
									<option value="<%= rsC("JobOrderStatusCode") %>"><%= UCase(rsC("JobOrderStatus")) %></option>
<%

		rsC.MoveNext
	Loop
End If

rsC.Close
Set rsC = Nothing

%>
								</select>
							</td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;color:red;">Comment</td>
							<td valign="top"><textarea name="Comment" rows="4" cols="30" onkeyup="parent.TrackCount(this,'textcount8',500)" onkeypress="parent.LimitText(this,500)" style="width:100%;" tabindex=7 ID="Textarea3"></textarea><br/>Characters Remaining: <input type="text" name="textcount8" size="4" value="500" readonly ID="Text4"></td>
						</tr>
						<tr>
							<td valign="top" style="font-weight:bold;color:red;">Date Scheduled</td>
							<td valign="top"><input type="input" value="<% If DateDiff("Y", rs("DateDeliveryScheduled"), Now()) < 100 Then Response.Write(rs("DateDeliveryScheduled")) End If %>" name="DateDeliveryScheduled" readonly ID="DateDeliveryScheduled" tabindex=8> <a href="javascript:showCal('Calendar20')"><img src="/Images/Calendar.gif" border=0></a></td>
						</tr>
<!--
						<tr>
							<td valign="top" style="font-weight:bold;color:red;">Date Requested</td>
							<td valign="top"><input type="input" value="" name="DateDeliveryRequested" readonly ID="Input1" tabindex=8> <a href="javascript:showCal('Calendar21')"><img src="/Images/Calendar.gif" border=0></a></td>
						</tr>
-->
						<tr>
							<td colspan=2 align="right"><input type="submit" value="Save"></td>
						</tr>
						</form>
					</table>
				</td>
				<td valign="top">
					<table>
						<tr>
							<td style="font-size:16px;">Related Jobs</td>
						</tr>
						<tr>
							<td colspan=2>
								<table ID="Table2">
<%

sql = "Select * From JobOrderThirdPartyContents Inner Join JobOrderStatus On JobOrderStatus.JobOrderStatusCode = JobOrderThirdPartyContents.JobOrderStatusCode Where JobOrderId = " & lngJobOrderId
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
	Do Until rs.EOF
%>
									<tr>
										<td><a href="Edit.asp?TP=false&JobOrderContentId=<%= rs("JobOrderThirdPartyId") %>&JobOrderId=<%= rs("JobOrderId") %>">Item # <%= rs("JobOrderThirdPartyId") %> - <%= rs("Quantity") %>&nbsp;x&nbsp;<%= rs("Description") %></a></td>
										<td style="font-weight:bold;color:red;"><%= UCase(rs("JobOrderStatus")) %></td>
									</tr>
<%
		rs.MoveNext
	Loop
End If

rs.Close
Set rs = Nothing

sql = "Select * From JobOrderThirdPartyContents Inner Join JobOrderStatus On JobOrderStatus.JobOrderStatusCode = JobOrderThirdPartyContents.JobOrderStatusCode Where JobOrderId = " & lngJobOrderId
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
	Do Until rs.EOF
%>
									<tr>
										<td><a href="Edit.asp?TP=true&JobOrderContentId=<%= rs("JobOrderThirdPartyId") %>&JobOrderId=<%= rs("JobOrderId") %>">3rd Party # <%= rs("JobOrderThirdPartyId") %> - <%= rs("Quantity") %>&nbsp;x&nbsp;<%= rs("Description") %></a></td>
										<td style="font-weight:bold;color:red;"><%= UCase(rs("JobOrderStatus")) %></td>
									</tr>
<%
		rs.MoveNext
	Loop
End If

rs.Close
Set rs = Nothing

%>
								</table>
								<br>
							</td>
						</tr>
					</table>				
				</td>
			</tr>
			<tr>
				<td style="font-size:16px;">Other Comments</td>
			</tr>
			<tr>
				<td>
					<table width=500 cellpadding=5 cellspacing=0 border=0 ID="Table3">
<%
If boolTP Then
	sql = "Select * From JobOrderThirdPartyComments Inner Join JobOrderStatus On JobOrderStatus.JobOrderStatusCode = JobOrderThirdPartyComments.JobOrderStatusCode Where JobOrderThirdPartyId In (Select JobOrderThirdPartyId From JobOrderThirdPartyContents Where JobOrderId = " & lngJobOrderId & " And JobOrderThirdPartyId = " & lngJobOrderContentId & ") Order By DateEntered Desc"
Else
	sql = "Select * From JobOrderComments Inner Join JobOrderStatus On JobOrderStatus.JobOrderStatusCode = JobOrderComments.JobOrderStatusCode Where JobOrderContentId In (Select JobOrderContentId From JobOrderContents Where JobOrderId = " & lngJobOrderId & " And JobOrderContentId = " & lngJobOrderContentId & ") Order By DateEntered Desc"
End If
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
	Do Until rs.EOF
%>
						<tr>
							<td width=220><img src="/Images/Spacer.gif" width=220 height=1 border=0 alt=""><br><% If rs("Comment") <> "" Then Response.Write(rs("Comment")) Else Response.Write("No comment") %></td>
							<td width=120><img src="/Images/Spacer.gif" width=120 height=1 border=0 alt=""><br><%= FormatDateU(rs("DateEntered"), True) %></td>
							<td width=100 style="font-weight:bold;color:red;"><img src="/Images/Spacer.gif" width=100 height=1 border=0 alt=""><br><%= UCase(rs("JobOrderStatus")) %></td>
						</tr>
<%
		rs.MoveNext
	Loop
End If

rs.Close
Set rs = Nothing
%>
					</table>
				</td>
			</tr>
		</table>
	</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
