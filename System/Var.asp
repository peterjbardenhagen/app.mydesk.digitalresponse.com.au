<%
' ===============================================================================
' Techlight MyDesk - Minimal Session/Cookie Setup
' ===============================================================================
' PURPOSE: Only set cookies that change per-user or are user-specific
' STATIC VALUES: All moved to Constants.asp (TL_* constants)
' ===============================================================================
'-------------------------------------------------------------------------------
' Approval Password (rarely changes, but kept for backward compatibility)
'-------------------------------------------------------------------------------
Response.Cookies("ApprovalPassword") = TL_APPROVAL_PASSWORD
Response.Cookies("ApprovalPassword").Expires = Date() + 365

'-------------------------------------------------------------------------------
' Legacy compatibility exports (for smooth transition)
' These provide backward compatibility during refactoring
'-------------------------------------------------------------------------------
Dim strWorkingDir, strGlobalPrefix, strGlobalState
strWorkingDir = TL_WORKING_DIR
strGlobalPrefix = TL_PREFIX
strGlobalState = TL_STATE
%>