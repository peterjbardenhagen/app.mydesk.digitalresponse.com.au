<%
Option Explicit

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Session("WorkingDir") = "" Then Response.Redirect("/")

Dim page
page = Request("Page")

%>
<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Frameset//EN"
   "http://www.w3.org/TR/html4/frameset.dtd">
<html>
<head>
	<title>MyDesk</title>
	<script language="javascript" src="<%= Session("WorkingDir") %>/System/Global.js"></script>
</head>
<frameset rows="70,*" frameborder=0 framespacing=0>
      <frame src="Header.asp" id="HeaderFrame" frameborder=No noresize scrolling=no>
<%
If page <> "" Then
%>
      <frame src="<%= page %>" id="MainFrame" frameborder=No noresize scrolling=yes>
<%
Else
%>
      <frame src="Dashboard.asp<% If Request("Msg") <> "" Then %>?Msg=<%= Trim(Request("Msg")) %><% End If %>" id="MainFrame" frameborder=No noresize scrolling=yes>
<%
End If
%>
</frameset>
</html>