<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("UserSettings")("Manager") Then Response.Redirect("../Portal/AccessDenied.asp")

Dim intProductId
intProductId = CLng(Request("ProductId"))

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
		<script language="javascript" src="/System/cal2.js"></script>
		<script language="javascript" src="/System/cal_conf2.js"></script>
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
			if (emptyField(document.Form1.DivisionId)) {
				alert("Please select a Division.");
				validFlag = false;
				document.Form1.DivisionId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.ProductCatId)) {
				alert("Please select a Product Category.");
				validFlag = false;
				document.Form1.ProductCatId.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.ProductCode)) {
				alert("Please complete the Product Code field.");
				validFlag = false;
				document.Form1.ProductCode.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.ProductName)) {
				alert("Please complete the Product Name field.");
				validFlag = false;
				document.Form1.ProductName.focus();
			}}

			if (validFlag) {
			if (emptyField(document.Form1.ProductDesc)) {
				alert("Please complete the Product Description field.");
				validFlag = false;
				document.Form1.ProductDesc.focus();
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.UnitCost)) {
				if (isNaN(document.Form1.UnitCost.value)) {
					alert("Please enter a valid number for the Unit Cost field.");
					validFlag = false;
					document.Form1.UnitCost.focus();
				}
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.NettPrice)) {
				if (isNaN(document.Form1.NettPrice.value)) {
					alert("Please enter a valid number for the Nett Price field.");
					validFlag = false;
					document.Form1.NettPrice.focus();
				}
			}}

			if (validFlag) {
			if (!emptyField(document.Form1.MinNettPrice)) {
				if (isNaN(document.Form1.MinNettPrice.value)) {
					alert("Please enter a valid number for the Minimum Nett Price field.");
					validFlag = false;
					document.Form1.MinNettPrice.focus();
				}
			}}
		
			return validFlag;
		}

		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

<%

Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select * From Products Where ProductId = " & intProductId
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then

%>

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp" class="Header2">Products</a> / Edit Product /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<form action="Edit_Proc.asp" method="post" name="Form1" ID="Form1" onSubmit="return checkForm();">
								<input type="hidden" name="ProductId" value="<%= rs("ProductId") %>">
								<tr>
									<td valign="top" class="Req">*</td>
									<td width=100 valign="top"><b>Division</b></td>
									<td valign="top">
										<select name="DivisionId" ID="Select1" style="width:280px;">
											<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsDiv = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT * FROM Divisions WHERE Quotes = True AND DivisionId In (" & Request.Cookies("DivisionIdsAccess")("Manager") & ") ORDER BY Division"
Set rsDiv = dbConn.Execute(sql)

If Not(rsDiv.BOF And rsDiv.EOF) Then
	Do Until rsDiv.EOF
		If Clng(rsDiv("DivisionId")) = CLng(rs("DivisionId")) Then
			Response.Write ("								<option selected value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
		Else
			Response.Write ("								<option value=""" & rsDiv("DivisionId") & """>" & rsDiv("Division") & "</option>" & vbNewLine)
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
									</td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td width=100 valign="top"><b>Product Category</b></td>
									<td valign="top">
										<select name="ProductCatId" ID="Select2" style="width:280px;">
											<option value="">- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - </option>
<%

Set rsPCat = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT ProductCat.*, Divisions.DivisionCode FROM ProductCat INNER JOIN Divisions ON ProductCat.DivisionId = Divisions.DivisionId ORDER BY Divisions.Division, ProductCat"
Set rsPCat = dbConn.Execute(sql)

If Not(rsPCat.BOF And rsPCat.EOF) Then
	Do Until rsPCat.EOF
		If rsPCat("ProductCatId") = rs("ProductCatId") Then
			Response.Write ("								<option selected value=""" & rsPCat("ProductCatId") & """>" & rsPCat("DivisionCode") & " - " & rsPCat("ProductCat") & "</option>" & vbNewLine)
		Else
			Response.Write ("								<option value=""" & rsPCat("ProductCatId") & """>" & rsPCat("DivisionCode") & " - " & rsPCat("ProductCat") & "</option>" & vbNewLine)
		End If
		rsPCat.MoveNext
	Loop
End If

If IsObject(rsPCat) Then
	rsPCat.Close
	Set rsPCat = Nothing
End If

%>
										</select>			
									</td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Product Code</td>
									<td valign="top"><input type="text" name="ProductCode" style="width:280px;" ID="Text1" maxlength=50 value="<%= rs("ProductCode") %>"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Product Name</td>
									<td valign="top"><input type="text" name="ProductName" style="width:280px;" ID="Text5" maxlength=50 value="<%= rs("ProductName") %>"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Product Description</td>
									<td valign="top">
									<textarea name="ProductDesc" id="ProductDesc" rows="5" cols="30" onkeyup="parent.TrackCount(this,'textcount1',500)" onkeypress="parent.LimitText(this,500)"><%= rs("ProductDesc") %></textarea><br/>Characters Remaining: <input type="text" name="textcount1" size="4" value="<%= 500-Len(rs("ProductDesc")) %>" readonly ID="Text2">
									</td>								
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Is this product per unit, per day?</td>
									<td valign="top">
										<input type="radio" name="PerUnitPerDay" id="PerUnitPerDay" value="-1" <% If rs("PerUnitPerDay") = -1 Then Response.Write("Checked") End If %>> Yes<br/>
										<input type="radio" name="PerUnitPerDay" id="PerUnitPerDay" value="0" <% If rs("PerUnitPerDay") = 0 Then Response.Write("Checked") End If %>> No
									</td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Unit Cost (Ex. GST)</td>
									<td valign="top">$<input type="text" size=20 name="UnitCost" maxlength=8 value="<%= rs("UnitCost") %>" ID="Text3"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Nett Price (Ex. GST)</td>
									<td valign="top">$<input type="text" size=20 name="NettPrice" maxlength=8 value="<%= rs("NettPrice") %>" ID="Text4"></td>
								</tr>
								<tr>
									<td valign="top" class="Req">*</td>
									<td valign="top" style="font-weight:bold;">Minimum Nett Price (Ex. GST)</td>
									<td valign="top">$<input type="text" size=20 name="MinNettPrice" maxlength=8 value="0.00" ID="Text6"></td>
								</tr>
								<tr>
									<td colspan=3 valign="top" align="right"><input type="button" value="Cancel" onclick="document.location.href='default.asp';" ID="Button1" NAME="Button1">&nbsp;<input type="submit" value="Submit" id="Submit1" NAME="Submit"></td>
								</tr>
								</form>
							</table>
						</td>
					</tr>
				</table>
			</td>
		</tr>
	</table>
<%

End If

%>
	</body>
</html>
<%

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
