<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim lngContactId
lngContactId = CLng(Request("ContactId"))

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<title>SalesEngine</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

<%

Dim rs
Dim sql

Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "Select C.*, Users.DivisionId, Users.UserId, Users.Name From Contacts_WithCustomersAndSuppliers_V2 C Inner Join Users On Users.Code = C.Code Where C.ContactId = " & lngContactId
Set rs = dbConn.Execute(sql)

strEmail = ConvertToEmail(rs("Email"))
strWebsite = ConvertToWebAddress(rs("Website"))

%>

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
				<br/>
				<span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp" class="Header2">Contacts</a> / View Contact /></span>
				<br/><br/>
				<table width=100% align="center" ID="Table1">
					<tr>
						<td>
							<table cellpadding=3 cellspacing=0 border=0 ID="Table2">
								<input type="hidden" name="ContactId" value="<%= rs("ContactId") %>">
								<tr>
									<td valign="top">
										<table>
											<tr>
												<td valign="top" style="font-weight:bold;">Owner</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("Name") %></td>
											</tr>
											<tr>
												<td valign="top" style="font-weight:bold;">Full Name</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("FirstName") & " " & rs("Surname") %></td>
											</tr>
<%
If rs("Position") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Position</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("Position") %></td>
											</tr>
<%
End If
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Company</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("CompanyName") %></td>
											</tr>
											<tr>
												<td colspan=3><br><b>Invoice Address Details</b></td>
											</tr>
<%
If rs("Address1") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Address 1</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("Address1") %></td>
											</tr>
<%
End If
If rs("Address2") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Address 2</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("Address2") %></td>
											</tr>
<%
End If
If rs("Suburb") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Suburb</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("Suburb") %></td>
											</tr>
<%
End If
If rs("StateId") = 9 And rs("State") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">State</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("State") %></td>
											</tr>
<%
ElseIf rs("State") <> "" Then
	sql = "Select State From States Where StateId = " & rs("StateId")
	Set rsState = dbConn.Execute(sql)
%>
											<tr>
												<td valign="top" style="font-weight:bold;">State</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rsState("State") %></td>
											</tr>
<%
End If
If rs("PostCode") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Post Code</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("PostCode") %></td>
											</tr>
<%
End If
If rs("Country") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Country</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("Country") %></td>
											</tr>
<%
End If
%>
											<tr>
												<td colspan=3><br><b>Delivery (or other) Address Details</b></td>
											</tr>
<%
If rs("OAddress1") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Address 1</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("OAddress1") %></td>
											</tr>
<%
End If
If rs("OAddress2") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Address 2</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("OAddress2") %></td>
											</tr>
<%
End If
If rs("OSuburb") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Suburb</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("OSuburb") %></td>
											</tr>
<%
End If
If rs("OStateId") = 9 And rs("OState") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">State</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("OState") %></td>
											</tr>
<%
ElseIf rs("OState") <> "" Then
	sql = "Select State From States Where StateId = " & rs("OStateId")
	Set rsState = dbConn.Execute(sql)
%>
											<tr>
												<td valign="top" style="font-weight:bold;">State</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rsState("State") %></td>
											</tr>
<%
End If
If rs("OPostCode") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Post Code</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("OPostCode") %></td>
											</tr>
<%
End If
If rs("OCountry") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Country</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("OCountry") %></td>
											</tr>
<%
End If
%>
											<tr>
												<td colspan=3><br></td>
											</tr>
<%
If rs("Phone") <> "" Then
%>											<tr>
												<td valign="top" style="font-weight:bold;">Phone</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("Phone") %></td>
											</tr>
<%
End If
If rs("Mobile") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Mobile</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= rs("Mobile") %></td>
											</tr>
<%
End If
If rs("Email") <> "" Then
%>											<tr>
												<td valign="top" style="font-weight:bold;">Email</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= strEmail %></td>
											</tr>
<%
End If
If rs("Website") <> "" Then
%>
											<tr>
												<td valign="top" style="font-weight:bold;">Website</td>
												<td>&nbsp;&nbsp;</td>
												<td valign="top"><%= strWebsite %></td>
											</tr>
<%
End If
%>
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
<%

If IsObject(rs) Then
	rs.Close
	Set rs = Nothing
End If

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
