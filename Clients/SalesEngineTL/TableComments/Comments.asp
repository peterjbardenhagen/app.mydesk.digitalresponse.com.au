<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

Dim intTableId
Dim intItemId

intTableId = CLng(Request("TableId"))
intItemId = CLng(Request("ItemId"))

%>
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
	<head>
		<link rel="stylesheet" type="text/css" href="/System/Style2.css">
		<META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
		<META http-equiv="Expires" content="0">
		<META http-equiv="Pragma" content="no-store, private, must-revalidate">
	</head>
	<body>
		<span class="Header3">Comments</span>
		<br/><br/>
		<li><a href="#" onclick="parent.document.location.href=parent.document.location.href;"><< Back</a><br>
		<li><a href="Add.asp?TableId=<%= intTableId %>&ItemId=<%= intItemId %>">Add Comment</a></li>
		<br/><br/>
<%

Dim sql
Dim rs
Set rs = Server.CreateObject("ADODB.RecordSet")
sql = "SELECT Comments.*, Users.Name FROM Comments INNER JOIN Users ON Users.Code = Comments.FromCode WHERE TableId = " & intTableId & " AND ItemId = " & intItemId & " ORDER BY DateEntered DESC"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
%>
		<table width=100% cellpadding=5 cellspacing=0 border=0 ID="Table2">
			<tr>
				<td width=100 class="ListHeaderRow">Date Entered</td>
				<td width=150 class="ListHeaderRow">User</td>
				<td class="ListHeaderRow">Comment</td>
				<td class="ListHeaderRow" width=140>Follow-Up Date</td>
				<td class="ListHeaderRow" width=140>Follow-Up Complete</td>
			</tr>
<%
	Do Until rs.EOF
%>
			<tr>
				<td width=100 style="background-color:white;color:black;border-bottom:1px solid black;font-size:12px;vertical-align:top;"><%= FormatDateU(rs("DateEntered"), False) %></td>
				<td width=150 style="background-color:white;color:black;border-bottom:1px solid black;font-size:12px;vertical-align:top;"><%= rs("Name") %></td>
				<td style="background-color:white;color:black;border-bottom:1px solid black;font-size:12px;vertical-align:top;"><%= Replace(rs("Comment"), Chr(39), "<br>") %></td>
<%
		If CBool(rs("FollowUpRequired")) Then
%>
				<td style="background-color:white;color:black;border-bottom:1px solid black;font-size:12px;vertical-align:top;" width=140><%= FormatDateU(rs("FollowUpDate"), False) %></td>
				<td style="background-color:white;color:black;border-bottom:1px solid black;font-size:12px;vertical-align:top;" width=140><% If rs("FollowUpComplete") Then Response.Write "Yes" Else Response.Write "<span style=""color:red;font-weight:bold;"">No</span>" %></td>
<%
		Else
%>
				<td style="background-color:white;color:black;border-bottom:1px solid black;font-size:12px;vertical-align:top;" width=140>&nbsp;</td>
				<td style="background-color:white;color:black;border-bottom:1px solid black;font-size:12px;vertical-align:top;" width=140>&nbsp;</td>
<%
		End If
%>
			</tr>
			<tr height=2>
				<td colspan=3></td>
			</tr>
<%
		rs.MoveNext
	Loop
End If

%>
		</table>
	</body>
</html>
<%

rs.Close
Set rs = Nothing

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
