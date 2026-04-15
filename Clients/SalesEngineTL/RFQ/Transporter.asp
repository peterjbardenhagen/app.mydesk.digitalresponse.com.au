
<%

lngRFQId = CLng(Request("RFQId"))
lngDivisionId = CLng(Request("DivisionId"))

%>
<script language="javascript">
	parent.document.location.href='<%= Request.Cookies("ClientSettings")("WorkingDir") %>/PurchaseOrders/Add2.asp?RFQId=<%= lngRFQId %>&DivisionId=<%= lngDivisionId %>';
</script>