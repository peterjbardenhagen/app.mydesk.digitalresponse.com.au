<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim strMsg
strMsg = Trim(Request("Msg"))
%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<title>SalesEngine</title>
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
		<link rel="stylesheet" type="text/css" href="/System/<%= Session("Stylesheet") %>">
		<script language="JavaScript" src="/System/Global.js"></script>
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
		</script>
	</head>
	<body bgcolor="#dddddd">

<!--#include virtual="/System/ssi_Header.inc"-->

<table width="100%" cellpadding=0 cellspacing=0 border=0 bgcolor="#ffffff">
	<tr>
		<td>
			<table width="95%" align="center" cellpadding=0 cellspacing=0 border=0>
				<tr>
					<td>
						<br><span class="Header2">Welcome <strong><%= Session("Name") %>. You have successfully logged into SalesEngine. <% If Session("Admin") Then %>You are an Administrator.<% End If %></span><br><br>
					</td>
				</tr>
			</table>
<%
If strMsg <> "" Then
%>
			<br>
			<table width="100%" cellpadding=3 cellspacing=0 border=0 bgcolor="#ffffff" ID="Table6">
				<tr>
					<td><span style="color:red;"><%= strMsg %></span></td>
				</tr>
			</table>
<%
End If
%>
		</td>
	</tr>
</table>

<table background="/Clients/SalesEngineTT/Images/Filler.gif" bgcolor="#ffffff" width="100%" cellpadding=0 cellspacing=0 border=0>
	<tr>
		<td>
			<table bgcolor="#ffffff" cellpadding=0 cellspacing=0 border=0 ID="Table5">
				<tr>
					<td valign="top"><a href="/Clients/SalesEngineTT/Contacts" onMouseOver="MM_swapImage('btn_Contacts','','/Clients/SalesEngineTT/Images/btn_Contacts_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="/Clients/SalesEngineTT/Images/btn_Contacts.gif" border=0 alt="Contacts" name="btn_Contacts"></a></td>
					<td valign="top"><a href="/Clients/SalesEngineTT/CallReports" onMouseOver="MM_swapImage('btn_CallReports','','/Clients/SalesEngineTT/Images/btn_CallReports_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="/Clients/SalesEngineTT/Images/btn_CallReports.gif" border=0 alt="Call Reports" name="btn_CallReports"></a></td>
					<td valign="top"><a href="/Clients/SalesEngineTT/Users" onMouseOver="MM_swapImage('btn_Users','','/Clients/SalesEngineTT/Images/btn_Users_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="/Clients/SalesEngineTT/Images/btn_Users.gif" border=0 alt="Manage Users" name="btn_Users"></a></td>
				</tr>
				<tr>
					<td valign="top"><a href="/Clients/SalesEngineTT/Expenses" onMouseOver="MM_swapImage('btn_Expenses','','/Clients/SalesEngineTT/Images/btn_Expenses_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="/Clients/SalesEngineTT/Images/btn_Expenses.gif" border=0 alt="Expenses" name="btn_Expenses"></a></td>
					<td valign="top"><a href="/Clients/SalesEngineTT/Noticeboard" onMouseOver="MM_swapImage('btn_Noticeboard','','/Clients/SalesEngineTT/Images/btn_Noticeboard_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="/Clients/SalesEngineTT/Images/btn_Noticeboard.gif" border=0 alt="Noticeboard" name="btn_Noticeboard"></a></td>
					<td valign="top"><a href="/Clients/SalesEngineTT/Jobs" onMouseOver="MM_swapImage('btn_Jobs','','/Clients/SalesEngineTT/Images/btn_Jobs_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="/Clients/SalesEngineTT/Images/btn_Jobs.gif" border=0 alt="Jobs" name="btn_Jobs"></a></td>
				</tr>
				<tr>
					<td valign="top"><a href="/Clients/SalesEngineTT/Timesheets" onMouseOver="MM_swapImage('btn_Timesheets','','/Clients/SalesEngineTT/Images/btn_Timesheets_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="/Clients/SalesEngineTT/Images/btn_Timesheets.gif" border=0 alt="Timesheets" name="btn_Timesheets""></a></td>
					<td valign="top"><a href="/Clients/SalesEngineTT/TMail" onMouseOver="MM_swapImage('btn_TMail','','/Clients/SalesEngineTT/Images/btn_TMail_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="/Clients/SalesEngineTT/Images/btn_TMail.gif" border=0 alt="T-Mail" name="btn_TMail"></a></td>
					<td valign="top"><a href="/Clients/SalesEngineTT/Portal/LogOff.asp" onMouseOver="MM_swapImage('btn_LogOff','','/Clients/SalesEngineTT/Images/btn_LogOff_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="/Clients/SalesEngineTT/Images/btn_LogOff.gif" border=0 alt="Log Off" name="btn_LogOff"></a></td>
				</tr>
				<tr>
					<td valign="top"><a href="/Clients/SalesEngineTT/Types" onMouseOver="MM_swapImage('btn_Types','','/Clients/SalesEngineTT/Images/btn_Types_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="/Clients/SalesEngineTT/Images/btn_Types.gif" border=0 alt="Types" name="btn_Types"></a></td>
					<td valign="top"><a href="/Clients/SalesEngineTT/FilesLibrary" onMouseOver="MM_swapImage('btn_FilesLibrary','','/Clients/SalesEngineTT/Images/btn_FilesLibrary_On.gif',0);" onMouseOut="MM_swapImgRestore();"><img src="/Clients/SalesEngineTT/Images/btn_FilesLibrary.gif" border=0 alt="Files Library" name="btn_FilesLibrary"></a></td>
					<td background="/Clients/SalesEngineTT/Images/btn_Filler.gif" border=0></td>
				</tr>
			</table>
		</td>
	</tr>
</table>

	<table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table4">
		<tr>
			<td>
<%

Dim rsTMail
Dim strSql
strSql = "SELECT TMail.*, Users.Name FROM TMail INNER JOIN Users ON Users.Code = TMail.FromCode WHERE ToCode = '" & Session("Code") & "' AND Read = 0 ORDER BY [Date] DESC"
Set rsTMail = dbConn.Execute(strSql)

If Not(rsTMail.BOF And rsTMail.EOF) Then
%>
				<br>
				<b>You have unread TMail</b><br><br>
				<table width=100% cellpadding=5 cellspacing=0 border=0>
					<tr>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Date</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Message</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=150>From</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=150>Action</td>
					</tr>
<%
	Do Until rsTMail.EOF

%>
					<tr>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= FormatDateU(rsTMail("Date"), False) %></td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;">
						<b><%= rsTMail("Subject") %></b><br>
						<%= rsTMail("Message") %>
						</td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= rsTMail("Name") %></td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><a href="TMail/Reply.asp?TMailId=<%= rsTMail("TMailId") %>">Reply</a> | <a href="TMail/MarkRead_Proc.asp?TMailId=<%= rsTMail("TMailId") %>">Mark Read</a> | <a href="TMail/Del_Proc.asp?TMailId=<%= rsTMail("TMailId") %>&Portal=True">Delete</a></td>
					</tr>
<%

		rsTMail.MoveNext
	Loop
%>
				</table>
<%
End If
rsTMail.Close
Set rsTMail = Nothing

Dim rsNotices
strSql = "SELECT Noticeboard.*, Users.Name FROM Noticeboard INNER JOIN Users ON Users.Code = Noticeboard.Code WHERE Noticeboard.DateExpires >= Now() AND DateDiff('d', Noticeboard.DateEntered, Now()) <= 14 ORDER BY [DateEntered] DESC"
Set rsNotices = dbConn.Execute(strSql)

If Not(rsNotices.BOF And rsNotices.EOF) Then
%>
				<br>
				<b>You have unread Notices</b><br><br>
				<table width=100% cellpadding=5 cellspacing=0 border=0 ID="Table2">
					<tr>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Date</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Message</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=150>From</td>
					</tr>
<%
	Do Until rsNotices.EOF

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

		rsNotices.MoveNext
	Loop
%>
				</table>
<%
End If
rsNotices.Close
Set rsNotices = Nothing

Dim rsFollowUps
strSql = "SELECT Comments.* FROM Comments WHERE FromCode = '" & Session("Code") & "' AND FollowUpComplete = 0 ORDER BY Comments.[FollowUpDate]"
Set rsFollowUps = dbConn.Execute(strSql)

If Not(rsFollowUps.BOF And rsFollowUps.EOF) Then
%>
				<br>
				<b>These items need to be followed up</b><br><br>
				<table width=100% cellpadding=5 cellspacing=0 border=0 ID="Table3">
					<tr>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Date Entered</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Follow Up Date</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;">Comment</td>
						<td style="background-color:#ebeadb;border-bottom:1px solid #cbc7b8;font-weight:bold;" width=100>Action</td>
					</tr>
<%
	Do Until rsFollowUps.EOF

%>
					<tr>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= FormatDateU(rsFollowUps("FollowUpDate"), False) %></td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><span style="color:<%
		If DateDiff("d", CDate(rsFollowUps("FollowUpDate")), Now()) >= 14 Then
			Response.Write("Red;Font-Weight:Bold;")
		ElseIf DateDiff("d", CDate(rsFollowUps("FollowUpDate")), Now()) <= 14 Then
			Response.Write("Red")
		Else
			Response.Write("Black")
		End If		
%>;"><%= FormatDateU(rsFollowUps("FollowUpDate"), False) %></span></td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;"><%= rsFollowUps("Comment") %></td>
						<td valign="top" style="background-color:#ffffff;border-bottom:1px solid #666666;" width=100><a href="<%= Session("WorkingDir") %>/TableComments/Mark_FollowUpComplete_Proc.asp?CommentId=<%= rsFollowUps("CommentId") %>">Mark Complete</a></td>
					</tr>
<%

		rsFollowUps.MoveNext
	Loop
%>
				</table>
<%
End If
rsFollowUps.Close
Set rsFollowUps = Nothing
%>
			</td>
		</tr>
	</table>
	<br><br>
	</body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
