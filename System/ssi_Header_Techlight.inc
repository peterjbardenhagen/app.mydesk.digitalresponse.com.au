<%
' ===============================================================================
' Techlight MyDesk - Header Include
' ===============================================================================
' REQUIREMENTS: Constants.asp must be included BEFORE this file
' DEPENDENCIES: TL_WORKING_DIR constant
' ===============================================================================

Option Explicit

'-------------------------------------------------------------------------------
' Working Directory - now uses constant instead of session/cookie
'-------------------------------------------------------------------------------
Dim strWorkingDir
strWorkingDir = TL_WORKING_DIR

'-------------------------------------------------------------------------------
' Navigation Helper Functions
'-------------------------------------------------------------------------------
Dim currentPage
currentPage = LCase(Request.ServerVariables("SCRIPT_NAME"))

Function IsActive(pageName)
    If InStr(currentPage, LCase(pageName)) > 0 Then
        IsActive = "active"
    Else
        IsActive = ""
    End If
End Function

Function IsDirector()
    Dim userTypeId
    userTypeId = Request.Cookies("UserSettings")("UserTypeID") & ""
    ' UserTypeID 1 = Director, 2 = Manager, 3 = Standard User
    IsDirector = (userTypeId = "1")
End Function
%>
