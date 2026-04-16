<%
' Techlight MyDesk - Single client configuration

' Techlight MyDesk - Single Client Configuration
' Values are hardcoded where needed to reduce session/cookie overhead
' Only set essential cookies that change per user

' Approval password cookie (rarely changes)
Response.Cookies("ApprovalPassword") = "approveme"
Response.Cookies("ApprovalPassword").Expires = Date() + 365
%>