<%
'If Not InStr(LCase(Request.ServerVariables("URL")),"del_proc.asp") > 0 Then
'	If Err.Number <> 0 Then
'		SendErrorEmail "ASP Error", Err.Source, Err.Number, Err.Description
'		Response.End
'	End If
'End If

'on error resume next
  if (isObject(dbConn)) then
    if (dbConn.State = adStateOpen) then
       dbConn.Close
    end if
  end if

  Set dbConn = nothing

%>
