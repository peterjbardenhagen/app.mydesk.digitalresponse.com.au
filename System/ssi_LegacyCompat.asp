<%
' ===============================================================================
' Techlight MyDesk - Legacy Compatibility Layer
' ===============================================================================
' PURPOSE: Provides backward compatibility during transition to Constants.asp
' USAGE: Include AFTER Constants.asp to support old code still using cookies
' ===============================================================================

Option Explicit

'-------------------------------------------------------------------------------
' Legacy Cookie Exports (for smooth transition)
'-------------------------------------------------------------------------------
' These ensure old code still works while we migrate files to use constants

' WorkingDir - Now from constant instead of cookie
Dim strWorkingDir
strWorkingDir = TL_WORKING_DIR

' Export to ClientSettings cookie for legacy code (temporary during transition)
On Error Resume Next
Response.Cookies("ClientSettings")("WorkingDir") = TL_WORKING_DIR
Response.Cookies("ClientSettings")("Prefix") = TL_PREFIX
Response.Cookies("ClientSettings")("State") = TL_STATE
Response.Cookies("ClientSettings")("Stylesheet") = TL_STYLESHEET
Response.Cookies("ClientSettings").Expires = Date() + 1
On Error GoTo 0

'-------------------------------------------------------------------------------
' Legacy Variable Exports
'-------------------------------------------------------------------------------
Dim strGlobalPrefix, strGlobalState
strGlobalPrefix = TL_PREFIX
strGlobalState = TL_STATE

%>
