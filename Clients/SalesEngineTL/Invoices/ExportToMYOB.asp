<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Invoices") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

' Default date range - current month
dteFrom = Request("DateFrom")
dteTo = Request("DateTo")

If dteFrom = "" Then
    dteFrom = FormatDateU(DateAdd("d", -30, ServerToEST(Now())), False)
    dteTo = FormatDateU(Now(), False)
End If

%>
<!--#include virtual="/System/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<html>
    <head>
        <title>MyDesk - Export Invoices to MYOB</title>
        <META http-equiv="Cache-Control" content="no-store, no-cache, must-revalidate, pre-check=0">
        <META http-equiv="Expires" content="0">
        <META http-equiv="Pragma" content="no-store, private, must-revalidate">
        <link rel="stylesheet" type="text/css" href="<%= Request.Cookies("ClientSettings")("WorkingDir") %>/System/<%= Request.Cookies("ClientSettings")("Stylesheet") %>">
        <script language="javascript" src="/System/cal2.js"></script>
        <script language="javascript" src="/System/cal_conf2.js"></script>
    </head>
    <body bgcolor="#dddddd">
<!--#include virtual="/System/ssi_Header.inc"-->
    <center>
    <table width=95% align="center" cellpadding=0 cellspacing=0 border=0 ID="Table3">
        <tr>
            <td>
                <br>
                <table width="100%" cellpadding=0 cellspacing=0 border=0 ID="Table4">
                    <tr>
                        <td><span class="Header2"><a href="/Portal.asp" class="Header2">Home</a> / <a href="Default.asp" class="Header2">Invoices</a> / Export to MYOB /></span></td>
                        <td align="right"><a href="Default.asp" class="Header2">Back to Invoices</a></td>
                    </tr>
                </table>
                <br>
                <table width=760 cellpadding=5 cellspacing=0 border=0 bgcolor="#ffffff">
                    <tr>
                        <td>
                            <fieldset style="width:760px;">
                                <legend style="font-weight:bold;">Export Invoices to MYOB</legend>
                                <table width="100%" cellpadding=5 cellspacing=0 border=0 ID="Table1">
                                    <form name="FormExport" id="FormExport" method="post" action="ExportToMYOB_Proc.asp">
                                    <tr>
                                        <td colspan=2>
                                            <p style="font-size:12px; margin-bottom:10px;">
                                                This tool exports invoices to a CSV file that can be imported into MYOB AccountRight.
                                                <br><br>
                                                <strong>Instructions:</strong>
                                                <ol style="font-size:12px; margin-left:20px;">
                                                    <li>Select the date range for invoices you want to export</li>
                                                    <li>Click "Generate CSV" to download the file</li>
                                                    <li>In MYOB, go to File &gt; Import/Export Assistant &gt; Sales &gt; Service Sales</li>
                                                    <li>Select the CSV file and import</li>
                                                </ol>
                                            </p>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="font-weight:bold; width:150px;">Date From:</td>
                                        <td>
                                            <input type="text" value="<%= dteFrom %>" name="DateFrom" readonly ID="InputFrom"> 
                                            <a href="javascript:showCal('CalendarFrom')"><img src="/Images/Calendar.gif" border=0></a>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="font-weight:bold;">Date To:</td>
                                        <td>
                                            <input type="text" value="<%= dteTo %>" name="DateTo" readonly ID="InputTo"> 
                                            <a href="javascript:showCal('CalendarTo')"><img src="/Images/Calendar.gif" border=0></a>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td style="font-weight:bold;">Export Only Unexported:</td>
                                        <td>
                                            <input type="checkbox" name="OnlyUnexported" value="1" checked> 
                                            <span style="font-size:11px;">(Check to only export invoices not previously exported to MYOB)</span>
                                        </td>
                                    </tr>
                                    <tr>
                                        <td colspan=2 align="center" style="padding-top:20px;">
                                            <input type="button" value="Cancel" onclick="document.location.href='Default.asp';" ID="Button1">
                                            <input type="submit" value="Generate CSV for MYOB" ID="Submit1" style="font-weight:bold;">
                                        </td>
                                    </tr>
                                    </form>
                                </table>
                            </fieldset>
                        </td>
                    </tr>
                </table>
                
                <% ' Show previously exported invoices %>
                <br>
                <table width=760 cellpadding=5 cellspacing=0 border=0 bgcolor="#ffffff">
                    <tr>
                        <td>
                            <fieldset style="width:760px;">
                                <legend style="font-weight:bold;">Recent Exports</legend>
                                <table width="100%" cellpadding=5 cellspacing=0 border=0>
                                    <tr style="background-color:#cccccc; font-weight:bold;">
                                        <td>Export Date</td>
                                        <td>Exported By</td>
                                        <td>Date Range</td>
                                        <td>Invoice Count</td>
                                        <td>Status</td>
                                    </tr>
<%
Set rsExports = dbConn.Execute("SELECT TOP 10 * FROM InvoiceExportLog ORDER BY ExportDate DESC")
If Not (rsExports.BOF And rsExports.EOF) Then
    Do While Not rsExports.EOF
        Response.Write "<tr>"
        Response.Write "<td>" & FormatDateU2(rsExports("ExportDate"), False) & "</td>"
        Response.Write "<td>" & rsExports("ExportedBy") & "</td>"
        Response.Write "<td>" & FormatDateU2(rsExports("DateFrom"), False) & " - " & FormatDateU2(rsExports("DateTo"), False) & "</td>"
        Response.Write "<td>" & rsExports("InvoiceCount") & "</td>"
        Response.Write "<td>" & rsExports("Status") & "</td>"
        Response.Write "</tr>"
        rsExports.MoveNext
    Loop
Else
    Response.Write "<tr><td colspan=5 align=center>No export history found</td></tr>"
End If
If IsObject(rsExports) Then
    rsExports.Close
    Set rsExports = Nothing
End If
%>
                                </table>
                            </fieldset>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
    </center>
    </body>
</html>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
