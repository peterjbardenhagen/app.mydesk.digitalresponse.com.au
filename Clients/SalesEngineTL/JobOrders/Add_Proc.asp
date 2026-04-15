<!--METADATA TYPE="typelib" UUID="CD000000-8B95-11D1-82DB-00C04FB1625D" NAME="CDO for Windows Library" -->
<%

Response.AddHeader "Pragma", "No-Store"
Response.AddHeader "cache-control", "no-store, private, must-revalidate"
Response.Expires = -1
Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
Response.CacheControl = "no-store, private, must-revalidate"

If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then Response.Redirect("../Portal/AccessDenied.asp")

%>
<!--#include virtual="/System/ssi_Functions.asp"-->
<!--#include virtual="/Clients/SalesEngine/ssi_Security.inc"-->
<!--#include virtual="/System/ssi_dbConn_open.inc"-->
<!--#include virtual="/System/ssi_Dates.inc"-->
<%

strCode = Request.Cookies("UserSettings")("Code")
intDivisionId = Request("DivisionId")
lngQid = Request("Qid")
lngCompanyId = Request("CompanyId")
strCCompany = Trim(Replace(Request("CCompany"),"'","''"))
strCustomerPO = Trim(Replace(Request("CustomerPO"),"'","''"))
strDelCompany = Trim(Replace(Request("DelCompany"),"'","''"))
strDelAddress1 = Trim(Replace(Request("DelAddress1"),"'","''"))
strDelAddress2 = Trim(Replace(Request("DelAddress2"),"'","''"))
strDelSuburb = Trim(Replace(Request("DelSuburb"),"'","''"))
strDelState = Trim(Replace(Request("DelState"),"'","''"))
intDelStateId = Request("DelStateId")
strDelPostCode = Request("DelPostCode")
strDelCountry = Trim(Replace(Request("DelCountry"),"'","''"))
strInvCompany = Trim(Replace(Request("InvCompany"),"'","''"))
strInvAddress1 = Trim(Replace(Request("InvAddress1"),"'","''"))
strInvAddress2 = Trim(Replace(Request("InvAddress2"),"'","''"))
strInvSuburb = Trim(Replace(Request("InvSuburb"),"'","''"))
strInvState = Trim(Replace(Request("InvState"),"'","''"))
intInvStateId = Request("InvStateId")
strInvPostCode = Request("InvPostCode")
strInvCountry = Trim(Replace(Request("InvCountry"),"'","''"))
strProject = Trim(Replace(Request("Project"),"'","''"))
dteDateAccepted = ServerToEST(Now())

If intDelStateId = "" Then intDelStateId = 9
If intInvStateId = "" Then intInvStateId = 9

intItemLinesVal = CInt(Request.Form("ItemLinesVal"))
intThirdPartyLinesVal = CInt(Request.Form("ThirdPartyLinesVal"))

' Insert main quote details and generate a Quote Id
strSql = "Insert Into JobOrders (DivisionId, Qid, CompanyId, Company, CustomerPO, DelCompany, DelAddress1, DelAddress2, DelSuburb, DelState, DelStateId, DelPostCode, DelCountry, InvCompany, InvAddress1, InvAddress2, InvSuburb, InvState, InvStateId, InvPostCode, InvCountry, Project, Code, DateAccepted) " &_
			" Values (" & intDivisionId & ", " & lngQid & ", " & lngCompanyId & ", '" & strCCompany & "', '" & strCustomerPO & "', '" & strDelCompany & "', '" & strDelAddress1 & "', '" & strDelAddress2 & "', '" & strDelSuburb & "', '" & strDelState & "', " & intDelStateId & ", '" & strDelPostCode & "', '" & strDelCountry & "', '" & strInvCompany & "', '" & strInvAddress1 & "', '" & strInvAddress2 & "', '" & strInvSuburb & "', '" & strInvState & "', " & intInvStateId & ", '" & strInvPostCode & "', '" & strInvCountry & "', '" & strProject & "', '" & strCode & "', '" & dteDateAccepted & "')"
dbConn.Execute(strSql)

Set rsNew = Server.CreateObject("ADODB.RecordSet")
strSql = "Select @@IDENTITY As JobOrderId"
Set rsNew = dbConn.Execute(strSql)

lngJobOrderId = rsNew("JobOrderId")

rsNew.Close
Set rsNew = Nothing

' Items
For i = 2 To intItemLinesVal
    If IsNumeric(Request.Form("Quantity" & i)) Then
		lngProductId = Replace(Request.Form("ProductId" & i),"'","''")
		intQuantity = Replace(Request.Form("Quantity" & i),"'","''")
		strType = Replace(Request.Form("Type" & i),"'","''")
		intDays = Replace(Request.Form("Days" & i),"'","''")
		intUnits = Replace(Request.Form("Units" & i),"'","''")
		strProductCode = Replace(Request.Form("ProductCode" & i), "'","''")
		strDescription = Replace(Request.Form("Description" & i),"'","''")
		decUnitCost = Replace(Request.Form("UnitCost" & i),"'","''")
		decNettPrice = Replace(Request.Form("NettPrice" & i),"'","''")
		decUnitCostSubTotal = Replace(Request.Form("UnitCostSubTotal" & i),"'","''")
		decExtNettPrice = Replace(Request.Form("ExtNettPrice" & i),"'","''")
		strComment = Trim(Replace(Request.Form("Comment" & i),"'","''"))
		dteDateDeliveryRequested = Request.Form("DateDeliveryRequested" & i)
		dteDateDeliveryScheduled = Request.Form("DateDeliveryScheduled" & i)
		
		If Not IsDate(dteDateDeliveryRequested) Then
			dteDateDeliveryRequested = "01-Jan-1900"
		End If

		If Not IsDate(dteDateDeliveryScheduled) Then
			dteDateDeliveryScheduled = "01-Jan-1900"
		End If

		If intDays = "" Then intDays = 0
		If intUnits = "" Then intUnits = 0
        
		sql = "Insert Into JobOrderContents (JobOrderId, JobOrderStatusCode, ProductId, Quantity, Type, Days, Units, ProductCode, Description, UnitCost, NettPrice, UnitCostSubTotal, ExtNettPrice, DateDeliveryRequested, DateDeliveryScheduled, Comment) " &_
				"Values (" & lngJobOrderId & ", 10, " & lngProductId & ", " & intQuantity & ", '" & strType & "', " & intDays & ", " & intUnits & ", '" & strProductCode & "', '" & strDescription & "', " & decUnitCost & ", " & decNettPrice & ", " & decUnitCostSubTotal & ", " & decExtNettPrice & ", '" & dteDateDeliveryRequested & "', '" & dteDateDeliveryScheduled & "', '')"
		dbConn.Execute(sql)

		sql = "Select @@IDENTITY As NewJobOrderContentId"
		Set rsNew = dbConn.Execute(sql)
		lngJobOrderContentId = rsNew("NewJobOrderContentId")

		rsNew.Close
		Set rsNew = Nothing

		sql = "Insert Into JobOrderComments (JobOrderStatusCode, JobOrderContentId, Code, Comment, DateEntered) " &_
				"Values (10, " & lngJobOrderContentId & ", '" & Request.Cookies("UserSettings")("Code") & "', '" & strComment & "', '" & ServerToEST(Now()) & "')"
		dbConn.Execute(sql)
    End If
Next

' Third Party Supply Items
For i = 2 To intThirdPartyLinesVal
	If Request.Form("TP_Quantity" & i) <> "" Then
		If Request.Form("TP_Quantity" & i) > 0 Then
			strDescription = Replace(Request.Form("TP_Description" & i),"'","''")
			strJobOrderStatusCode = CLng(Request("TP_JobOrderStatusCode" & i))
			strSupplier = Replace(Request.Form("TP_Supplier" & i),"'","''")
			strQuoteNumber = Replace(Request.Form("TP_QuoteNumber" & i),"'","''")
			strQuoteDate = Replace(Request.Form("TP_QuoteDate" & i),"'","''")
			strExpiryDate = Replace(Request.Form("TP_ExpiryDate" & i),"'","''")
			strSupplierPartNumber = Replace(Request.Form("TP_SupplierPartNumber" & i),"'","''")
			strOurPartNumber = Replace(Request.Form("TP_OurPartNumber" & i),"'","''")
			strProductCode = strOurPartNumber
			intQuantity = Replace(Request.Form("TP_Quantity" & i),"'","''")
			strType = Replace(Request.Form("TP_Type" & i),"'","''")
			decUnitCost = Replace(Request.Form("TP_UnitCost" & i),"'","''")
			decNettPrice = Replace(Request.Form("TP_NettPrice" & i),"'","''")
			decExtNettPrice = Replace(Request.Form("TP_ExtNettPrice" & i),"'","''")
			strComment = Request.Form("TP_Comment" & i)
			dteDateDeliveryRequested = Request.Form("TP_DateDeliveryRequested" & i)
			dteDateDeliveryScheduled = Request.Form("TP_DateDeliveryScheduled" & i)
			
			If Not IsDate(dteDateDeliveryRequested) Then
				dteDateDeliveryRequested = "01-Jan-1900"
			End If

			If Not IsDate(dteDateDeliveryScheduled) Then
				dteDateDeliveryScheduled = "01-Jan-1900"
			End If

			strSql = "Insert Into JobOrderThirdPartyContents (JobOrderId, JobOrderStatusCode, ProductCode, Description, Supplier, QuoteNumber, QuoteDate, ExpiryDate, SupplierPartNumber, OurPartNumber, Quantity, Type, UnitCost, NettPrice, ExtNettPrice, DateDeliveryRequested, DateDeliveryScheduled, Comment) Values (" & lngJobOrderId & ", 10, '" & strProductCode & "', '" & strDescription & "', '" & strSupplier & "', '" & strQuoteNumber & "', '" & strQuoteDate & "', '" & strExpiryDate & "', '" & strSupplierPartNumber & "', '" & strOurPartNumber & "', " & intQuantity & ", '" & strType & "', " & decUnitCost & ", " & decNettPrice & ", " & decExtNettPrice & ", '" & dteDateDeliveryRequested & "', '" & dteDateDeliveryScheduled & "', '" & strComment & "')"
			dbConn.Execute(strSql)

			sql = "Select @@IDENTITY As NewJobOrderThirdPartyId"
			Set rsNew = dbConn.Execute(sql)
			lngJobOrderThirdPartyId = rsNew("NewJobOrderThirdPartyId")

			rsNew.Close
			Set rsNew = Nothing

			sql = "Insert Into JobOrderThirdPartyComments (JobOrderThirdPartyId, Code, Comment, JobOrderStatusCode, DateEntered) " &_
					"Values (" & lngJobOrderThirdPartyId & ", '" & Request.Cookies("UserSettings")("Code") & "', '" & strComment & "', 10, '" & ServerToEST(Now()) & "')"

			dbConn.Execute(sql)
		End If
	End If
Next

' Send Purchasing Manager a notification
AlertPurchasingManager intDivisionId, "New Job Order. Job # " & lngJobOrderId, "New Job Order. Job # " & lngJobOrderId & " awaiting your attention. Originator is " & Request.Cookies("UserSettings")("Name")

%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
<%

MyRedirect("Default.asp?DivisionId=" & intDivisionId & "&Msg=Job+Order+added")

%>