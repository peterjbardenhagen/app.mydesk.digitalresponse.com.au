
<%

lngQid = CLng(Request("Qid"))
lngDivisionId = CLng(Request("DivisionId"))
boolParent = CBool(Request("Parent"))

%>
<script language="javascript">
<% If boolParent Then %>
	document.location.href='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/PurchaseOrders/Add2.asp?Qid=<%= lngQid %>&DivisionId=<%= lngDivisionId %>';
<% Else %>
	document.location.href='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/PurchaseOrders/Add2.asp?Qid=<%= lngQid %>&DivisionId=<%= lngDivisionId %>';
<% End If %>
</script>