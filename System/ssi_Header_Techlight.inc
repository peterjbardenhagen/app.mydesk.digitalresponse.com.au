<%
On Error Resume Next

' strWorkingDir is defined in the parent page (Dashboard.asp, etc.)
' This file assumes strWorkingDir is already declared in the parent

' Get working directory with null checks
On Error Resume Next
If Request.Cookies("LoggedIn")&"" <> "" Then
	If Not Request.Cookies("ClientSettings") Is Nothing Then
		If Not IsEmpty(Request.Cookies("ClientSettings")("WorkingDir")) Then
			strWorkingDir = Request.Cookies("ClientSettings")("WorkingDir")
		End If
	End If
Else
	If Not IsEmpty(Session("WorkingDir")) Then
		strWorkingDir = Session("WorkingDir")
	End If
End If

' Fallback to ensure WorkingDir is never empty
If strWorkingDir = "" Or IsNull(strWorkingDir) Then
	strWorkingDir = "/Clients/SalesEngineTL"
End If
On Error GoTo 0

' Get current page for active nav highlighting
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
