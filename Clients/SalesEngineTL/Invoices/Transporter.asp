<%
Option Explicit

lngQid = CLng(Request("Qid"))

%>
<script language="javascript">
	parent.document.location.href='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/JobOrders/Add.asp?Qid=<%= lngQid %>';
</script>