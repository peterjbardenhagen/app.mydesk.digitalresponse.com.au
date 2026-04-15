<% 

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, ServerToEST(Now()))
Response.CacheControl = "no-store, private, must-revalidate"

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

sql = "Select * From PurchaseOrders_Totals_Mismatch"
Set rs = dbConn.Execute(sql)

If Not(rs.BOF And rs.EOF) Then
    Do Until rs.EOF
	    sql = "Update PurchaseOrders Set PriceExTotal = " & rs("Price2")
	    If CBool(rs("GST")) Then
	        sql = sql & ", PriceGSTTotal = " & rs("Price2")/10
	        sql = sql & ", PriceIncTotal = " & rs("Price2")*1.1
	    End If
	    sql = sql & " Where POid = " & rs("POid")
	    dbConn.Execute(sql)
	    rs.MoveNext
    Loop
End If

rs.Close
Set rs = Nothing

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?Msg=Maintenance+complete")

%>