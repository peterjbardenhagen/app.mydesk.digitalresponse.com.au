<%@ Page Language="vb" AutoEventWireup="false" ClassName="TT_GenerateRFQ" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Net" %>
<%@ Import Namespace="System.Web" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
    <head>
        <title>Generate</title>
    </head>
    <body>
        <script language="VisualBasic" runat="Server">

            Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
                Dim boolEmail As Boolean
                Dim boolFax As Boolean
                Dim lngRFQid As Long = CLng(Request("RFQid"))
                Dim strAttention As String = Request("Attention") & ""
                Dim strToEmail As String = Request("ToEmail") & ""
                Dim strFromFax As String = Request("FromFax") & ""
                Dim strToFax As String = Request("ToFax") & ""
                Dim strWorkingDir As String = Request("WorkingDir") & ""

                Dim intMode As Integer = CInt(Request("Mode"))
                Dim strStream As String = ""
                Dim oRequest As WebRequest = WebRequest.Create("https://" & Request.ServerVariables("SERVER_NAME") & strWorkingDir & "/RFQ/View.asp?Email=" & boolEmail & "&Fax=" & boolFax & "&RFQid=" & lngRFQid)
                Dim oResponse As WebResponse = oRequest.GetResponse()
                Dim oStream As Stream = oResponse.GetResponseStream()
                Dim oStreamReader As New StreamReader(oStream, True)
                Dim strPath As String = Server.MapPath(strWorkingDir & "/RFQ/Files/" & lngRFQid)

                If intMode = 1 Then boolEmail = True Else boolEmail = False
                If intMode = 2 Then boolFax = True Else boolFax = False

                If Not Directory.Exists(strPath) Then
                    Directory.CreateDirectory(strPath)
                End If

                Dim myFile As String = strPath & "\RFQ.html"
                File.Delete(myFile)

                Dim oStreamWriter As StreamWriter
                Dim strStreamLine

                oStreamWriter = File.CreateText(myFile)

                While oStreamReader.Peek() <> -1
                    strStreamLine = oStreamReader.ReadLine()
                    strStream += strStreamLine
                    oStreamWriter.WriteLine(strStreamLine)
                End While

                oResponse.Close()
                oResponse = Nothing

                oStreamReader.Close()
                oStreamReader = Nothing

                oStreamWriter.Close()
                oStreamWriter = Nothing

                If intMode = 1 Then
                    Response.Redirect("Email_Proc.asp?Attention=" & strAttention & "&ToEmail=" & strToEmail & "&RFQid=" & lngRFQid)
                ElseIf intMode = 2 Then
                    Response.Redirect("Fax_Proc.asp?Attention=" & strAttention & "&ToFax=" & strToFax & "&FromFax=" & strFromFax & "&RFQid=" & lngRFQid)
                End If
            End Sub

        </script>
    </body>
</html>