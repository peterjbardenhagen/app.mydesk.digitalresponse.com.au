<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
strMsg = Trim(Request("Msg"))

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
		<script language="javascript" src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/Global.js"></script>
		<script language="javascript">	
		function MM_swapImgRestore() { //v3.0
		var i,x,a=document.MM_sr; for(i=0;a&&i<a.length&&(x=a[i])&&x.oSrc;i++) x.src=x.oSrc;
		}
		function MM_findObj(n, d) { //v4.01
		var p,i,x;  if(!d) d=document; if((p=n.indexOf("?"))>0&&parent.frames.length) {
			d=parent.frames[n.substring(p+1)].document; n=n.substring(0,p);}
		if(!(x=d[n])&&d.all) x=d.all[n]; for (i=0;!x&&i<d.forms.length;i++) x=d.forms[i][n];
		for(i=0;!x&&d.layers&&i<d.layers.length;i++) x=MM_findObj(n,d.layers[i].document);
		if(!x && d.getElementById) x=d.getElementById(n); return x;
		}
		function MM_swapImage() { //v3.0
		var i,j=0,x,a=MM_swapImage.arguments; document.MM_sr=new Array; for(i=0;i<(a.length-2);i+=3)
		if ((x=MM_findObj(a[i]))!=null){document.MM_sr[j++]=x; if(!x.oSrc) x.oSrc=x.src; x.src=a[i+2];}
		}
		
		// made redundant can't access in new browsers
		//parent.HeaderFrame.location.href = parent.HeaderFrame.location.href;
		
		function QuickNavigate() {
			if(document.QuickNav.Type.value == 'Quote') {
				parent.ViewQuote('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', document.QuickNav.Ident.value);
			} else {	
				parent.ViewPurchaseOrder('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', document.QuickNav.Ident.value);
			}
		}
		</script>
	</head>
	<body bgcolor="#dddddd">
<!--

User variables:

Prefix = <%= Request.Cookies("ClientSettings")("Prefix") %>
Division Id = <%= Request.Cookies("DivisionId") %>

Cookies:

<%

For Each Item In Request.Cookies
	Response.Write Item & " " & Request.Cookies(Item) & "<BR>"
Next

%>
<%= Request.Cookies("DivisionIdsAccess")("ArrDivisionIdsManager") %>
-->
<!--#include virtual="/System/ssi_Header.inc"-->
<style>
	body,p,td { font-size: 10px; }
</style>

<table width="100%" cellpadding=0 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table1">
	<tr>
		<td align="center">
			<table width="98%" height="100%" cellpadding=5 cellspacing=0 border=0 ID="Table2">
				<tr>
					<td valign="top">
						<br><span style="font-weight:bold;font-size:10px;">Welcome <%= Request.Cookies("UserSettings")("Name") %>.
						You have successfully logged into MyDesk. <% If Request.Cookies("UserSettings")("Manager") Then %>You are an Administrator.<% End If %></span><br><br>
<%
If Request.Cookies("UserSettings")("UserTypeId") = 6 Or (Request.Cookies("UserSettings")("Code") = "MD0290" Or Request.Cookies("UserSettings")("Code") = "MD0155" Or Request.Cookies("UserSettings")("Code") = "MD0140") Then
%>
						<table bgcolor="#eeeeee" width=500 align="center" cellpadding=0 cellspacing=0 border=0 ID="Table9">
							<form name="QuickNav" id="QuickNav">
							<tr>
								<td>
									<table cellpadding=5 cellspacing=0 border=0 ID="Table19">
										<tr>
											<td style="font-size:12px;font-weight:bold;">Quick navigation</td>
											<td style="font-size:12px;font-weight:bold;">ID#</td>
											<td><input type="text" name="Ident"></td>
											<td>
											<select name="Type">
												<option value="PurchaseOrder">Purchase Order
												<option value="Quote">Quote
											</select>
											</td>
											<td>
												<input type="button" value="Go >>" onclick="QuickNavigate();">
											</td>
										</tr>
									</table>
								</td>
							</tr>
							</form>
						</table>
<%
End If
%>
						<table width=100% height="500" align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
							<tr height=10>
								<td colspan=3>
									<table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table7">
										<tr>
											<td height=1 bgcolor="<%= Request.Cookies("ClientSettings")("HomeColor1") %>"><img src="/Images/Spacer.gif" width=1 height=1 border=0 alt=""></td>
										</tr>
										<tr height=30>
											<td background="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/Gradient.gif">
												<table cellpadding=5 cellspacing=0 border=0 ID="Table3">
													<tr>
														<td style="font-size:14px;color:white;"><b>Messages</b></td>
													</tr>
												</table>
											</td>
										</tr>
									</table>
								</td>
								<td width=5 style="background-color:#cccccc;"><img src="/Images/Spacer.gif" width=5 height=1 border=0 alt=""></td>
								<td width=2 style="background-color:#cccccc;"><img src="/Images/Spacer.gif" width=2 height=1 border=0 alt=""></td>
								<td width=5 style="background-color:#cccccc;"><img src="/Images/Spacer.gif" width=5 height=1 border=0 alt=""></td>
								<td colspan=3 valign="top">
									<table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table8">
										<tr>
											<td height=1 bgcolor="<%= Request.Cookies("ClientSettings")("HomeColor1") %>"><img src="/Images/Spacer.gif" width=1 height=1 border=0 alt=""></td>
										</tr>
										<tr height=30>
											<td background="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/Gradient.gif">
												<table cellpadding=5 cellspacing=0 border=0 ID="Table10">
													<tr>
														<td style="font-size:14px;color:white;"><b>Drawers</b></td>
													</tr>
												</table>
											</td>
										</tr>
									</table>
								</td>
							</tr>
							<tr>
								<td colspan=3 valign="top">
									<table bgcolor="#cccccc" width="100%" cellpadding=5 cellspacing=0 style="border:2px solid <%= Request.Cookies("ClientSettings")("HomeColor1") %>;" style="background-image:url('<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/MessagesBg.jpg');" ID="Table5">
										<tr>
											<td valign="top">
<%
If strMsg <> "" Then
%>
												<table bgcolor="#ffffff" width="100%" align="center" cellpadding=5 cellspacing=0 border=0 ID="Table6">
													<tr>
														<td><span style="color:red;"><%= strMsg %></span></td>
													</tr>
												</table>
<%
End If

' ### Purchase Order Approvals

Dim rsApprovals
Dim lngCountApprovals
sql = "SELECT DISTINCT POid, PODate, HasCapEx, iif(Contacts.CompanyId=142,Contacts.CCompany,Companies.Company) AS CompanyName, Name FROM ((Users AS U INNER JOIN PurchaseOrders AS PO ON U.Code = PO.Code) INNER JOIN Contacts ON PO.ContactId = Contacts.ContactId) INNER JOIN Companies ON Contacts.CompanyId = Companies.CompanyId WHERE (((PO.POStatusId)=2))"
Set rsApprovals = dbConn.Execute(sql)

lngCountApprovals = -1

If Not(rsApprovals.BOF And rsApprovals.EOF) Then
	lngCountApprovals = 0
%>
												<table width=100% cellpadding=3 cellspacing=0 border=0 ID="PurchaseOrderApprovals">
													<tr>
														<td colspan=5 style="font-size:12px;font-weight:bold;">Purchase Orders Pending Approval<hr style="color:<%= Request.Cookies("ClientSettings")("HomeColor1") %>;"></td>
													</tr>
												</table>
												<table width=100% cellpadding=5 cellspacing=0 border=0 ID="PurchaseOrderApprovals2">										
													<tr>
<%
	Do Until rsApprovals.EOF
		If GetPONextLineApprover(rsApprovals("POid"), rsApprovals("HasCapEx")) = Request.Cookies("UserSettings")("Name") Then
%>
													<tr>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;">
															<table width=120>
																<tr>
																	<td valign="top" nowrap><b>PO #:</b></td>
																	<td valign="top" nowrap style="font-size:14px;"><%= rsApprovals("POid") %></td>
																</tr>
																<tr>
																	<td valign="top"><b>PO Date:</b></td>
																	<td valign="top"><%= FormatDateU(rsApprovals("PODate"), False) %></td>
																</tr>
															</table>
														</td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;">
															<table width=200>
																<tr>
																	<td valign="top" width=50><b>Supplier:</b></td>
																	<td valign="top"><%= rsApprovals("CompanyName") %></td>
																</tr>
																<tr>
																	<td valign="top" width=50><b>Originator:</b></td>
																	<td valign="top"><%= rsApprovals("Name") %></td>
																</tr>
															</table>
														</td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;" width=140 align="right"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/PurchaseOrders/Edit.asp?POid=<%= rsApprovals("POid") %>">Edit</a> | <a href="#" onclick="parent.ViewPurchaseOrder('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', <%= rsApprovals("POid") %>);">View</a></td>
													</tr>
<%
			lngCountApprovals = lngCountApprovals + 1
		End If
		rsApprovals.MoveNext
	Loop
%>
													<tr>
														<td colspan=4><br></td>
													</tr>
												</table>
<%
End If
rsApprovals.Close
Set rsApprovals = Nothing

If lngCountApprovals = 0 Then
%>
<script language="javascript">document.getElementById("PurchaseOrderApprovals").style.display = 'none';document.getElementById("PurchaseOrderApprovals2").style.display = 'none';</script>
<%
End If

' ### Purchase Order Approvals - Days Lapsed

sql = "SELECT DISTINCT PurchaseOrders.POid, DateDiff('d',#" & ServerToEST(Now()) & "#,PA.DateEntered) AS DaysLapsed, Users.Name AS Name, PurchaseOrders.PODate, PurchaseOrders.HasCapEx, iif(Contacts.CompanyId=142,Contacts.CCompany,Companies.Company) AS CompanyName FROM (Contacts INNER JOIN (Users INNER JOIN (PurchaseOrders INNER JOIN PurchaseOrderAudit AS PA ON PurchaseOrders.POid = PA.POId) ON Users.Code = PurchaseOrders.Code) ON Contacts.ContactId = PurchaseOrders.ContactId) INNER JOIN Companies ON Contacts.CompanyId = Companies.CompanyId WHERE PurchaseOrders.POStatusId = 3 AND PA.Action='Approved' AND DateDiff('d',#" & ServerToEST(Now()) & "#,PA.DateEntered) < 0 AND DateDiff('m',#" & ServerToEST(Now()) & "#,PA.DateEntered) > -1 AND Users.LineManagerCode = '" & Request.Cookies("UserSettings")("Code") & "' AND PA.DateEntered = (SELECT MAX(DateEntered) FROM PurchaseOrderAudit WHERE Action = 'Approved' AND POid = PurchaseOrders.POid)"
Set rsApprovals = dbConn.Execute(sql)

lngCountApprovals = -1

If Not(rsApprovals.BOF And rsApprovals.EOF) Then
	lngCountApprovals = 0
%>
												<table width=100% cellpadding=3 cellspacing=0 border=0 ID="PurchaseOrderApprovalsDaysLapsed">
													<tr>
														<td colspan=5 style="font-size:12px;font-weight:bold;">Purchase Orders Approved (with no activity after one day since approval)<hr style="color:<%= Request.Cookies("ClientSettings")("HomeColor1") %>;"></td>
													</tr>
												</table>
												<table width=100% cellpadding=5 cellspacing=0 border=0 ID="PurchaseOrderApprovalsDaysLapsed2">										
<%
	Do Until rsApprovals.EOF
%>
													<tr>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;">
															<table width=120 ID="Table20">
																<tr>
																	<td valign="top" nowrap><b>PO #:</b></td>
																	<td valign="top" nowrap style="font-size:14px;"><%= rsApprovals("POid") %></td>
																</tr>
																<tr>
																	<td valign="top" nowrap><b>PO Date:</b></td>
																	<td valign="top" nowrap><%= FormatDateU(rsApprovals("PODate"), False) %></td>
																</tr>
															</table>
														</td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;">
															<table width=200>
																<tr>
																	<td valign="top" width=50><b>Supplier:</b></td>
																	<td valign="top"><%= rsApprovals("CompanyName") %></td>
																</tr>
																<tr>
																	<td valign="top" width=50><b>Originator:</b></td>
																	<td valign="top"><%= rsApprovals("Name") %></td>
																</tr>
															</table>
														</td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;" width=140 align="right"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/PurchaseOrders/Edit.asp?POid=<%= rsApprovals("POid") %>">Edit</a> | <a href="#" onclick="parent.ViewPurchaseOrder('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', <%= rsApprovals("POid") %>);">View</a></td>
													</tr>
<%
		lngCountApprovals = lngCountApprovals + 1
		rsApprovals.MoveNext
	Loop
%>
													<tr>
														<td colspan=4><br></td>
													</tr>
												</table>
<%
End If
rsApprovals.Close
Set rsApprovals = Nothing

If lngCountApprovals = 0 Then
%>
<script language="javascript">document.getElementById("PurchaseOrderApprovalsDaysLapsed").style.display = 'none';document.getElementById("PurchaseOrderApprovalsDaysLapsed2").style.display = 'none';</script>
<%
End If

' ### Quote Approvals

sql = "SELECT DISTINCT POid, PODate, HasCapEx, iif(Contacts.CompanyId=142,Contacts.CCompany,Companies.Company) AS CompanyName, Name FROM ((Users AS U INNER JOIN PurchaseOrders AS PO ON U.Code = PO.Code) INNER JOIN Contacts ON PO.ContactId = Contacts.ContactId) INNER JOIN Companies ON Contacts.CompanyId = Companies.CompanyId WHERE (((PO.POStatusId)=9))"
Set rsApprovals = dbConn.Execute(sql)

lngCountApprovals = -1

If Not(rsApprovals.BOF And rsApprovals.EOF) Then
	lngCountApprovals = 0
%>
												<table width=100% cellpadding=3 cellspacing=0 border=0 ID="QuoteApprovals">
													<tr>
														<td colspan=5 style="font-size:12px;font-weight:bold;">Quotes Pending Approval<hr style="color:<%= Request.Cookies("ClientSettings")("HomeColor1") %>;"></td>
													</tr>
												</table>
												<table width=100% cellpadding=5 cellspacing=0 border=0 ID="QuoteApprovals2">										
													<tr>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=130>Quote Date</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=130>Customer</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Quote #</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Originator</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Margin</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=140 align="right">Action</td>
													</tr>
<%
	Do Until rsApprovals.EOF
		If GetQuoteNextLineApprover(rsApprovals("Qid")) = Request.Cookies("UserSettings")("Name") Then
%>
													<tr>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;" width=130><%= FormatDateU(rsApprovals("QuoteDate"), False) %></td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;" width=130><%= rsApprovals("CompanyName") %></td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= rsApprovals("Qid") %></td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= rsApprovals("Name") %></td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= FormatNumber(rsApprovals("Margin"),2) %>%</td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;" width=140 align="right"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Quotes/Edit.asp?Qid=<%= rsApprovals("Qid") %>">Edit</a> | <a href="#" onclick="parent.ViewQuote('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', <%= rsApprovals("Qid") %>);">View</a></td>
													</tr>
<%
			lngCountApprovals = lngCountApprovals + 1
		End If
		rsApprovals.MoveNext
	Loop
%>
													<tr>
														<td colspan=4><br></td>
													</tr>
												</table>
<%
End If
rsApprovals.Close
Set rsApprovals = Nothing

If lngCountApprovals = 0 Then
%>
<script language="javascript">document.getElementById("QuoteApprovals").style.display = 'none';document.getElementById("QuoteApprovals2").style.display = 'none';</script>
<%
End If

' ### Quote Approvals - Days Lapsed

sql = "SELECT DISTINCT IIf(Contacts.CompanyId=142,Contacts.CCompany,Companies.Company) AS CompanyName, Quotes.Qid, DateDiff('d',#" & ServerToEST(Now()) & "#,PA.DateEntered) AS DaysLapsed, Quotes.Margin, Users.Name AS Name, Quotes.QuoteDate FROM (Contacts INNER JOIN (Users INNER JOIN (Quotes INNER JOIN QuoteAudit AS PA ON Quotes.Qid = PA.Qid) ON Users.Code = Quotes.Code) ON Contacts.ContactId = Quotes.ContactId) INNER JOIN Companies ON Contacts.CompanyId = Companies.CompanyId WHERE Quotes.QuoteStatusId = 9 AND PA.Action='Approved' AND DateDiff('d',#" & ServerToEST(Now()) & "#,PA.DateEntered) < 0 AND DateDiff('m',#" & ServerToEST(Now()) & "#,PA.DateEntered) > -1 AND Users.LineManagerCode = '" & Request.Cookies("UserSettings")("Code") & "' AND PA.DateEntered = (SELECT MAX(DateEntered) FROM QuoteAudit WHERE Action = 'Approved' AND Qid = Quotes.Qid)"
Set rsApprovals = dbConn.Execute(sql)

lngCountApprovals = -1

If Not(rsApprovals.BOF And rsApprovals.EOF) Then
	lngCountApprovals = 0
%>
												<table width=100% cellpadding=3 cellspacing=0 border=0 ID="QuoteApprovalsDaysLapsed">
													<tr>
														<td colspan=5 style="font-size:12px;font-weight:bold;">Quotes Approved (with no activity after one day since approval)<hr style="color:<%= Request.Cookies("ClientSettings")("HomeColor1") %>;"></td>
													</tr>
												</table>
												<table width=100% cellpadding=5 cellspacing=0 border=0 ID="QuoteApprovalsDaysLapsed2">										
													<tr>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=130>Quote Date</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=130>Customer</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Quote #</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Originator</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=140 align="right">Action</td>
													</tr>
<%
	Do Until rsApprovals.EOF
%>
													<tr>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;" width=130><%= FormatDateU(rsApprovals("QuoteDate"), False) %></td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;" width=130><%= rsApprovals("CompanyName") %></td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= rsApprovals("Qid") %></td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= rsApprovals("Name") %></td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;" width=140 align="right"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Quotes/Edit.asp?Qid=<%= rsApprovals("Qid") %>">Edit</a> | <a href="#" onclick="parent.ViewQuote('<%= Request.Cookies("ClientSettings")("WorkingDir") %>', <%= rsApprovals("Qid") %>);">View</a></td>
													</tr>
<%
		lngCountApprovals = lngCountApprovals + 1
		rsApprovals.MoveNext
	Loop
%>
													<tr>
														<td colspan=4><br></td>
													</tr>
												</table>
<%
End If
rsApprovals.Close
Set rsApprovals = Nothing

If lngCountApprovals = 0 Then
%>
<script language="javascript">document.getElementById("QuoteApprovalsDaysLapsed").style.display = 'none';document.getElementById("QuoteApprovalsDaysLapsed2").style.display = 'none';</script>
<%
End If

' ### T-Mail

Dim rsTMail
Dim sql
sql = "SELECT TMail.*, Users.Name FROM TMail INNER JOIN Users ON Users.Code = TMail.FromCode WHERE ToCode = '" & Request.Cookies("UserSettings")("Code") & "' AND Read = 0 ORDER BY [Date] DESC"
Set rsTMail = dbConn.Execute(sql)

If Not(rsTMail.BOF And rsTMail.EOF) Then
%>
												<table width=100% cellpadding=3 cellspacing=0 border=0 ID="Table14">
													<tr>
														<td colspan=5 style="font-size:12px;font-weight:bold;">T-Mail<hr style="color:<%= Request.Cookies("ClientSettings")("HomeColor1") %>;"></td>
													</tr>
												</table>
												<table width=100% cellpadding=5 cellspacing=0 border=0 ID="Table9">
													<tr>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Message</td>
													</tr>
<%
	Do Until rsTMail.EOF

%>
													<tr>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;">
														<b>Received Date:</b> <%= FormatDateU(rsTMail("Date"), False) %><br>
														<b>From:</b> <%= rsTMail("Name") %><br><br>
														<span style="font-size:14px;"><%= rsTMail("Subject") %></span><br>
														<%= rsTMail("Message") %><br>
														<br>
														<div align="right"><a href="TMail/Reply.asp?TMailId=<%= rsTMail("TMailId") %>">Reply</a> | <a href="TMail/MarkRead_Proc.asp?TMailId=<%= rsTMail("TMailId") %>">Mark Read</a> | <a href="TMail/Del_Proc.asp?TMailId=<%= rsTMail("TMailId") %>&Portal=True">Delete</a></div>
														</td>
													</tr>
<%

		rsTMail.MoveNext
	Loop
%>
												</table>
												<br>
<%
End If
rsTMail.Close
Set rsTMail = Nothing

Dim rsNotices
sql = "SELECT Noticeboard.*, Users.Name FROM Noticeboard INNER JOIN Users ON Users.Code = Noticeboard.Code WHERE Noticeboard.DateExpires >= #" & ServerToEST(Now()) & "# AND DateDiff('d', Noticeboard.DateEntered, #" & ServerToEST(Now()) & "#) <= 14 ORDER BY [DateEntered] DESC"
Set rsNotices = dbConn.Execute(sql)

If Not(rsNotices.BOF And rsNotices.EOF) Then
%>
												<table width=100% cellpadding=3 cellspacing=0 border=0 ID="Table12">
													<tr>
														<td colspan=5 style="font-size:12px;font-weight:bold;">Notices from the last two weeks<hr style="color:<%= Request.Cookies("ClientSettings")("HomeColor1") %>;"></td>
													</tr>
												</table>
												<table width=100% cellpadding=5 cellspacing=0 border=0 ID="Table11">
													<tr>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Date</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Message</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=120>From</td>
													</tr>
<%
	Do Until rsNotices.EOF
		If DateDiff("d",Now(),rsNotices("DateEntered")) >= -14 Then
%>
													<tr>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= FormatDateU(rsNotices("DateEntered"), False) %></td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;">
														<b><%= rsNotices("Heading") %></b><br>
														<%= rsNotices("Message") %>
														</td>
														<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= rsNotices("Name") %></td>
													</tr>
<%
		End If
		rsNotices.MoveNext
	Loop
%>
												</table>
												<br>
<%
End If
rsNotices.Close
Set rsNotices = Nothing

Dim rsFollowUps
sql = "SELECT Comments.*, Tables.* FROM Comments INNER JOIN Tables ON Tables.TableId = Comments.TableId WHERE FromCode = '" & Request.Cookies("UserSettings")("Code") & "' AND FollowUpComplete = 0 AND FollowUpRequired = -1 ORDER BY Comments.[FollowUpDate]"
Set rsFollowUps = dbConn.Execute(sql)

If Not(rsFollowUps.BOF And rsFollowUps.EOF) Then
%>
												<table width=100% cellpadding=3 cellspacing=0 border=0 ID="Table13">
													<tr>
														<td colspan=5 style="font-size:12px;font-weight:bold;">Follow Ups<hr style="color:<%= Request.Cookies("ClientSettings")("HomeColor1") %>;"></td>
													</tr>
												</table>
												<table width=100% cellpadding=5 cellspacing=0 border=0 ID="Table15">
													<tr>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Date Entered</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Follow Up Date</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Area</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Comment</td>
														<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=140 align="right">Action</td>
													</tr>
<%
	Do Until rsFollowUps.EOF
		If rsFollowUps("TableId") = 2 Then ' Call Reports
			strView = "parent.ViewCallReport(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & rsFollowUps("ItemId") & ");'"
		ElseIf rsFollowUps("TableId") = 5 Then ' Sales Projects
			strView = "parent.ViewSalesProject(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & rsFollowUps("ItemId") & ");'"
		ElseIf rsFollowUps("TableId") = 6 Then ' Quotes
			strView = "parent.ViewQuote(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & rsFollowUps("ItemId") & ");'"
		ElseIf rsFollowUps("TableId") = 7 Then ' RFQ
			strView = "parent.ViewRFQ(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & rsFollowUps("ItemId") & ");'"
		ElseIf rsFollowUps("TableId") = 8 Then ' Purchase Orders
			strView = "parent.ViewPurchaseOrder(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & rsFollowUps("ItemId") & ");'"
		ElseIf rsFollowUps("TableId") = 10 Then ' Invoices
			strView = "parent.ViewInvoice(""" & Request.Cookies("ClientSettings")("WorkingDir") & """, " & rsFollowUps("ItemId") & ");'"
		End If

		If DateDiff("d", CDate(rsFollowUps("FollowUpDate")), ServerToEST(Now())) >= 0  Then ' Overdue
			strColor = "#cd3434"
		ElseIf DateDiff("d", CDate(rsFollowUps("FollowUpDate")), ServerToEST(Now())) < 0 And DateDiff("d", CDate(rsFollowUps("FollowUpDate")), ServerToEST(Now())) > -14 Then
			strColor = "#000462"
		Else
			strColor = "#555555"
		End If


%>
													<tr style="background-color:<%= strColor %>">
														<td valign="top" style="color:white;border-bottom:1px solid #ffffff;"><%= FormatDateU(rsFollowUps("FollowUpDate"), False) %></td>
														<td valign="top" style="color:white;border-bottom:1px solid #ffffff;"><%= FormatDateU(rsFollowUps("FollowUpDate"), False) %></td>
														<td valign="top" style="color:white;border-bottom:1px solid #ffffff;"><%= rsFollowUps("TableDesc") %></td>
														<td valign="top" style="color:white;border-bottom:1px solid #ffffff;"><%= rsFollowUps("Comment") %></td>
														<td valign="top" style="color:white;border-bottom:1px solid #ffffff;" width=140 align="right"><a href="#" onclick='<%= strView %>' style="color:white;">View</a> | <a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/TableComments/Mark_FollowUpComplete_Proc.asp?CommentId=<%= rsFollowUps("CommentId") %>" style="color:white;">Mark Complete</a></td>
													</tr>
<%

		rsFollowUps.MoveNext
	Loop
%>
												</table>
												<table cellpadding=3 cellspacing=0 border=0 ID="Table16">
													<tr>
														<td><img src="/Images/Red.gif" width=10 height=5 border=0 alt="" style="border:2px solid white;"> indicates the follow up overdue.</td>
														<td><img src="/Images/Navy.gif" width=10 height=5 border=0 alt="" style="border:2px solid white;"> indicates the follow up is due in the next 14 days.</td>
														<td><img src="/Images/DarkGray.gif" width=10 height=5 border=0 alt="" style="border:2px solid white;"> indicates the follow up is due after the next 14 days.</td>
													</tr>
												</table>
<%
End If
%>
												<br>
<%
rsFollowUps.Close
Set rsFollowUps = Nothing
%>
											
											</td>
										</tr>
									</table>
								</td>
								<td width=5 style="background-color:#ebeadb;"><img src="/Images/Spacer.gif" width=5 height=1 border=0 alt=""></td>
								<td width=2 style="background-color:#ebeadb;"><img src="/Images/Spacer.gif" width=2 height=1 border=0 alt=""></td>
								<td width=5 style="background-color:#ebeadb;"><img src="/Images/Spacer.gif" width=5 height=1 border=0 alt=""></td>
								<td colspan=3 valign="top" align="right" width=450>
									<table bgcolor="#ffffff" width="450" cellpadding=0 cellspacing=0 border=0 ID="Table17">
										<tr>
											<td>
												<center>
												<table width="100%" bgcolor="#ffffff" cellpadding=0 cellspacing=0 border=0 ID="Table18">
													<tr>
														<td colspan=3><img src="/Images/Spacer.gif" width=450 height=10 border=0 alt=""></td>
													</tr>
													<tr>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Contacts" onMouseOver="MM_swapImage('btn_Contacts','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Contacts_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Contacts.gif" border=0 alt="Contacts" name="btn_Contacts"></a></td>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/CallReports" onMouseOver="MM_swapImage('btn_CallReports','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_CallReports_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_CallReports.gif" border=0 alt="Call Reports" name="btn_CallReports"></a></td>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/SalesProjects" onMouseOver="MM_swapImage('btn_SalesProjects','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_SalesProjects_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_SalesProjects.gif" border=0 alt="Sales Projects" name="btn_SalesProjects"></a></td>
													</tr>
													<tr>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Products" onMouseOver="MM_swapImage('btn_Products','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Products_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Products.gif" border=0 alt="Products" name="btn_Products"></a></td>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Quotes" onMouseOver="MM_swapImage('btn_Quotes','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Quotes_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Quotes.gif" border=0 alt="Quotes" name="btn_Quotes"></a></td>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/JobOrders" onMouseOver="MM_swapImage('btn_Jobs','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Jobs_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Jobs.gif" border=0 alt="Job Monitoring" name="btn_Jobs"></a></td>
													</tr>
													<tr>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Invoices" onMouseOver="MM_swapImage('btn_Invoices','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Invoices_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Invoices.gif" border=0 alt="Invoices" name="btn_Invoices"></td>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Purchasing" onMouseOver="MM_swapImage('btn_Purchasing','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Purchasing_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Purchasing.gif" border=0 alt="Purchases" name="btn_Purchasing"></a></td>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Expenses" onMouseOver="MM_swapImage('btn_Expenses','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Expenses_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Expenses.gif" border=0 alt="Expenses" name="btn_Expenses"></a></td>
													</tr>
													<tr>
														<!--<td valign="top"><a href="#" onclick="alert('Disabled');" onMouseOver="MM_swapImage('btn_Timesheets','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Timesheets_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Timesheets.gif" border=0 alt="Timesheets" name="btn_Timesheets"></a></td>-->
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/TMail" onMouseOver="MM_swapImage('btn_TMail','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_TMail_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_TMail.gif" border=0 alt="T-Mail" name="btn_TMail"></a></td>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Noticeboard" onMouseOver="MM_swapImage('btn_Noticeboard','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Noticeboard_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Noticeboard.gif" border=0 alt="Noticeboard" name="btn_Noticeboard"></a></td>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/FilesLibrary" onMouseOver="MM_swapImage('btn_FilesLibrary','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_FilesLibrary_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_FilesLibrary.gif" border=0 alt="Files Library" name="btn_FilesLibrary"></a></td>
													</tr>
													<tr>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Employment" onMouseOver="MM_swapImage('btn_Employment','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Employment_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Employment.gif" border=0 alt="Employment" name="btn_Employment"></a></td>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Setup" onMouseOver="MM_swapImage('btn_Setup','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Setup_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Setup.gif" border=0 alt="Setup" name="btn_Setup"></a></td>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Users" onMouseOver="MM_swapImage('btn_Users','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Users_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Users.gif" border=0 alt="Manage Users" name="btn_Users"></a></td>
													</tr>
													<tr>
														
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Reports" onMouseOver="MM_swapImage('btn_Reports','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Reports_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Reports.gif" border=0 alt="Reports" name="btn_Reports"></a></td>
														<td valign="top"><a href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Portal/LogOff.asp" onMouseOver="MM_swapImage('btn_LogOff','','<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_LogOff_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_LogOff.gif" border=0 alt="Log Off" name="btn_LogOff"></a></td>
														<td>&nbsp;</td>
													</tr>
													<!--
													<tr>
														<td valign="top"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Filler.gif" border=0 alt=""></td>
													</tr>
													
														<td valign="top"><img src="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/Images/btn_Filler.gif" border=0 alt=""></td>
													-->
												</table>
												</center>
											</td>
										</tr>
									</table>
								</td>
							</tr>
						</table>
					</td>
				</tr>
			</table><br>
			</td>
		</tr>
	</table>
<%

If Request.Cookies("UserSettings")("Code") = "MD0025" Then

%>
	<table cellpadding=10>
		<tr>
			<td>
			<a href="TableFiles/DefaultList.asp">Table Files</a>
			</td>
		</tr>
	</table>
<%

End If

%>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->