<%@ Page Language="vb" AutoEventWireup="false" classname="TL_GenerateDeliveryNote" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Net" %>
<%@ Import Namespace="System.Web" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
    <head>
        <title>Generate</title>
    </head>
    <body>
        <script language="visualbasic" runat="server">

            ' Returns the appropriate protocol (http or https) based on the hostname
            ' Production (techlight.digitalresponse.com.au) requires HTTPS
            ' Local/UAT environments can use HTTP or HTTPS
            Private Function GetProtocol(ByVal hostname As String) As String
                Dim hostLower As String = hostname.ToLower()
                
                ' Production hostname - must use HTTPS
                If hostLower = "techlight.digitalresponse.com.au" Then
                    Return "https"
                Else
                    ' Local/UAT environments - use current protocol
                    Dim isHttps As String = Request.ServerVariables("HTTPS")
                    If isHttps = "on" OrElse isHttps = "true" Then
                        Return "https"
                    Else
                        Return "http"
                    End If
                End If
            End Function

            Private Sub Page_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
                Dim boolEmail As Boolean
                Dim boolFax As Boolean
                Dim lngInvoiceId As Long = CLng(Request("InvoiceId"))
                Dim strNotes As String = Request("Notes") & ""
                Dim strAttention As String = Request("Attention") & ""
                Dim strToEmail As String = Request("ToEmail") & ""
                Dim strFromFax As String = Request("FromFax") & ""
                Dim strToFax As String = Request("ToFax") & ""
                Dim strWorkingDir As String = Request("WorkingDir") & ""

                Dim intMode As Integer = CInt(Request("Mode"))
                Dim strStream As String = ""
                Dim strProtocol As String = GetProtocol(Request.ServerVariables("SERVER_NAME"))
        		Dim oRequest As WebRequest = WebRequest.Create(strProtocol & "://" & Request.ServerVariables("SERVER_NAME") & strWorkingDir & "/Invoices/ViewDeliveryNote.asp?Email=" & boolEmail & "&Fax=" & boolFax & "&InvoiceId=" & lngInvoiceId)
                Dim oResponse As WebResponse = oRequest.GetResponse()
                Dim oStream As Stream = oResponse.GetResponseStream()
                Dim oStreamReader As New StreamReader(oStream, True)
                Dim strPath As String = Server.MapPath(strWorkingDir & "/Invoices/Files/" & lngInvoiceId)

                If intMode = 1 Then boolEmail = True Else boolEmail = False
                If intMode = 2 Then boolFax = True Else boolFax = False

                If Not Directory.Exists(strPath) Then
                    Directory.CreateDirectory(strPath)
                End If

        		Dim myFile As String = strPath & "\DeliveryNote.html"
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
        			Response.Redirect("EmailDeliveryNote_Proc.asp?Notes=" & strNotes & "&Attention=" & strAttention & "&ToEmail=" & strToEmail & "&InvoiceId=" & lngInvoiceId)
                End If
            End Sub

        </script>
    </body>
</html>