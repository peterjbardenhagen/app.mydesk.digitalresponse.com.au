function SubmitForm() {
	parent.OpenResults();
}
function OpenResults() {
	try {
		var w = window.open ("/Loading.html", 'winResults', "width=760,height=400,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
		w.focus();

		w.moveTo(0,0);
		if (document.all) {
			w.resizeTo(screen.availWidth,screen.availHeight);
		}
	} catch(e) {
		alert('Your computer has a pop-up blocker or the results window is already open.');
		return;
	}
}
// Format String to Decimal
function formatDecimal(myNum)
{
	var numberField = myNum; // Field where the number appears
	var rlength = 2; // The number of decimal places to round to
	var newnumber = Math.round(numberField*Math.pow(10,rlength))/Math.pow(10,rlength);
	newnumber = newnumber.toFixed(2);
	if(isNotNumeric(newnumber)){newnumber = '0.00'}
	return newnumber;
}
// Format String to Integer
function formatInteger(myNum)
{
	myNum = parseInt(myNum);
	if(isNotNumeric(myNum)){myNum = '0'}
	return myNum;
}
function ViewUserRoles(WorkingDir) {
	var Width = 600;
	var Height = 500;
	var Path = '/UserRoles/ViewUserRoles.asp';
	var Window = 'ViewUserRoles';
	var w = window.open (WorkingDir + Path, Window, "width="+Width+",height="+Height+",location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();
	w.moveTo(0,0);
}
function ViewQuote(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Quotes/View.asp?Qid=', Id, 'WinQuote', 760, 500, true);
}
function SelectEmailFromContacts(WorkingDir) {
	var w = window.open (WorkingDir + '/Contacts/SelectEmailFromContact.asp', 'EmailSelector', "width=450,height=200,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();
}
function SelectFaxFromContacts(WorkingDir) {
	var w = window.open (WorkingDir + '/Contacts/SelectFaxFromContact.asp', 'FaxSelector', "width=450,height=200,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();
}
function ViewInvoice(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Invoices/View.asp?InvoiceId=', Id, 'WinInvoice', 760, 500, true);
}
function ViewInvoiceDeliveryNote(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Invoices/ViewDeliveryNote.asp?InvoiceId=', Id, 'WinInvoiceDeliveryNote', 760, 500, true);
}
function ViewJobOrder(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/JobOrders/View.asp?JobOrderId=', Id, 'WinJobOrder', 760, 500, true);
}
function UpdateQuoteStatus(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Quotes/UpdateStatus.asp?Qid=', Id, 'WinQuoteStatus', 500, 250, false);
}
function UpdatePOStatus(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/PurchaseOrders/UpdateStatus.asp?POid=', Id, 'WinPOStatus', 500, 350, false);
}
function ViewRFQ(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/RFQ/View.asp?RFQid=', Id, 'WinRFQ', 760, 500, true);
}
function ViewExpense(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Expenses/View.asp?ExpenseId=', Id, 'WinExpense', 700, 500, true);
}
function ViewTMail(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/TMail/View.asp?TMailId=', Id, 'WinTMail', 700, 500, true);
}
function ViewCommentRecord(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/TableComments/ViewRecord.asp?CommentId=', Id, 'WinCommentRecord', 700, 500, true);
}
function ViewPurchaseOrder(WorkingDir, Id, Requests) {
	if(Requests) {
		ViewRecordStandard(WorkingDir, '/PurchaseOrders/ViewRequest.asp?POid=', Id, 'WinPurchaseOrder', 760, 500, true);
	} else {
		ViewRecordStandard(WorkingDir, '/PurchaseOrders/View.asp?POid=', Id, 'WinPurchaseOrder', 760, 500, true);
	}
}
function ViewComments(WorkingDir, Id) {
	var Path = '';
	var w = window.open (WorkingDir + Path + Id, Window, "width='+Width+',height='+Height+',location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();
	w.moveTo(0,0);
	if (document.all) {
		w.resizeTo(screen.availWidth,screen.availHeight);
	}
	ViewRecordStandard(WorkingDir, '/TableComments/ViewComments.asp?CommentId=', Id, TableId, ItemId, 'WinCommentRecord', 700, 500, true);
}
function ViewTimesheet(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Timesheets/View.asp?TimesheetId=', Id, 'WinTimesheet', 700, 500, true);
}
function ViewCallReport(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/CallReports/View.asp?CallReportId=', Id, 'WinCallReport', 700, 500, true);
}
function SelectSalesProject(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/SalesProjects/Select.asp?SalesProjectId=', Id, 'WinSelectSalesProject', 700, 500, true);
}
function ViewContact(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Contacts/View.asp?ContactId=', Id, 'WinContact', 700, 500, true);
}
function ViewSalesProject(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/SalesProjects/View.asp?SalesProjectId=', Id, 'WinJob', 700, 500, true);
}
function ViewRecordStandard(WorkingDir, Path, Id, Window, Width, Height, FullScreen) {
	document.location.href = WorkingDir + Path + Id;
/*
	FullScreen = true;
	var w = window.open (WorkingDir + Path + Id, Window, "width="+Width+",height="+Height+",location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();
	w.moveTo(0,0);
	if(FullScreen) {
		if (document.all) {
			w.resizeTo(screen.availWidth,screen.availHeight);
		}
	}
*/
}
function GetUser(form, PostPage) {
	var selCode = form.Code[form.Code.selectedIndex].value;
	if(selCode.length > 0 && selCode.length > 0) {
		var qs = "Code=" + escape(selCode);
		MainFrame.location.href=PostPage + "?" + qs;
	}
}
function ReplyToComment(WorkingDir, QuoteNumber, Qid, QCid, FromCode) {
	var w = window.open (WorkingDir + "/Comments/Reply.asp?QuoteNumber=" + QuoteNumber + "&Qid=" + Qid + "&QCid=" + QCid + '&FromCode=' + FromCode, 'ReplyComment', "width=640,height=400,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();
	w.moveTo(0,0);
	if (document.all) {
		w.resizeTo(screen.availWidth,screen.availHeight);
	}
}
function ReplyToTableComment(WorkingDir, ItemId, TableId, FromCode, CommentId) {
	var w = window.open (WorkingDir + "/TableComments/Reply.asp?ItemId=" + ItemId + "&TableId=" + TableId + '&FromCode=' + FromCode + '&CommentId=' + CommentId, 'ReplyTableComment', "width=640,height=400,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();
	w.moveTo(0,0);
	if (document.all) {
		w.resizeTo(screen.availWidth,screen.availHeight);
	}
}
function GeneratePurchaseOrder(WorkingDir, RFQid) {
	parent.document.location.href = WorkingDir + '/PurchaseOrders/GenerateFromRFQ.asp?RFQid=' + RFQid;
}
function MoveIt()
{
	if(screen.height > 800) {
		parent.window.resizeTo(545,680);
	}

	if (parseInt(navigator.appVersion) >= 4 && screen.height > 800) {
		var screenHeight = screen.height;
		var screenWidth = screen.width;
		var topPos = (screenHeight)/2-400;
		var leftPos = (screenWidth)/2-300;
	}
	parent.self.moveTo(leftPos,topPos);
}
function RefreshPage_Global_Opener() {
	if(window.opener && !window.opener.closed) {
		window.opener.document.parentWindow.RefreshPage_Global();
	}
}
function RefreshPage_Global() {
	var ref = document.location.href;
	if(confirm('Refresh page to load new data?')){
		if(ref.indexOf("/Portal.aspx") > 0) {
			document.location.href = '/Portal.aspx';
		} else {
			document.location.reload();
		}
	}
}
function RefreshWindowClose() {
	try {
		RefreshIFrame_Global_Opener();
	} catch(e) {
		setTimeout("window.close();",1000);
	} finally {
		setTimeout("window.close();",1000);
	}
}
function RedirectPage_Global(myRef) {
	document.location.href = myRef;
}
function CreateNewContact(WorkingDir, Field, ContactType) {
//	var url = WorkingDir + "/Contacts/AddNewWin.asp?CurrentPage="+document.location.href+"&Field="+Field+"&Qid="+Arg("Qid")+"POid="+Arg("POid")+"InvoiceId="+Arg("InvoiceId")+"&ContactType="+ContactType, 'CreateNewContact'+Field, "width=640,height=400,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0";
//	var w = window.open (url);
//	w.focus();
	var url = WorkingDir+'/Contacts/Add.asp'
	document.location.href=url;
}
function CreateNewContact_UpdateSelect(newValue, newText, Field) {
	if(window.opener && !window.opener.closed) {
		var myField;
		if(Field == 'ContactId') {
			myField = window.opener.document.parentWindow.MainFrame.Form1.ContactId;
		} else if (Field == 'ContactId1') {
			myField = window.opener.document.parentWindow.MainFrame.Form1.ContactId1;
		} else if (Field == 'ContactId2') {
			myField = window.opener.document.parentWindow.MainFrame.Form1.ContactId2;
		} else if (Field == 'ContactId3') {
			myField = window.opener.document.parentWindow.MainFrame.Form1.ContactId3;
		} else if (Field == 'ContactId4') {
			myField = window.opener.document.parentWindow.MainFrame.Form1.ContactId4;
		} else if (Field == 'ContactId5') {
			myField = window.opener.document.parentWindow.MainFrame.Form1.ContactId5;
		}
		myField.options[0].value = newValue;
		myField.options[0].text = newText;
		var option = new Option(newText, newValue);
		myField.options[0].selected = true;
//		myField.options.add(option, 2);
	}
}
// Textbox limitter
function TrackCount(fieldObj,countFieldName,maxChars) {
	var countField = eval("fieldObj.form."+countFieldName);
	var countFieldChars = fieldObj.value.length;

	if(countFieldChars <= maxChars) {
		var diff = maxChars - countFieldChars;
	} else {
		var diff = -1;
	}

	// Need to check & enforce limit here also in case user pastes data
	if(diff < 0) {
		fieldObj.value = fieldObj.value.substring(0,maxChars);
//		fieldObj.value = left(fieldObj.value, maxChars);
		diff = maxChars - fieldObj.value.length;
	}
	if(diff == -1) {
		//diff = 0;
	}
	countField.value = diff;
}
function LimitText(fieldObj,maxChars)
{
	var result = true;
	if (fieldObj.value.length >= maxChars)
		result = false;
	if (window.event)
		window.event.returnValue = result;
	return result;
}
// Disable Right Click
function right(e) {
/*	if (parseInt(document.location.host.indexOf('dev.mydesk.com.au')) == -1) {
		if (navigator.appName == 'Netscape' && (e.which == 3 || e.which == 2)) {
			return false;
		} else if (navigator.appName == 'Microsoft Internet Explorer' && (event.button == 2 || event.button == 3)) {                   
			return false;
		}
		return true;
	}  else {
		return true;
	}
}
if (parseInt(document.location.host.indexOf('dev.mydesk.com.au')) == -1) {
	document.onmousedown = right;

	if (document.layers) {
		window.captureEvents(Event.MOUSEDOWN);
	}

	window.onmousedown = right;

	document.oncontextmenu = function() {
		alert('Function disabled');
		return false;
	}*/
}
function isEmail(str) {
  // are regular expressions supported?
  var supported = 0;
  if (window.RegExp) {
    var tempStr = "a";
    var tempReg = new RegExp(tempStr);
    if (tempReg.test(tempStr)) supported = 1;
  }
  if (!supported) 
    return (str.indexOf(".") > 2) && (str.indexOf("@") > 0);
  var r1 = new RegExp("(@.*@)|(\\.\\.)|(@\\.)|(^\\.)");
  var r2 = new RegExp("^.+\\@(\\[?)[a-zA-Z0-9\\-\\.]+\\.([a-zA-Z]{2,3}|[0-9]{1,3})(\\]?)$");
  return (!r1.test(str) && r2.test(str));
}
function deleteRecord(id) {
window.open('Del_Proc.asp?Id=' + id);
parent.document.location.href=parent.document.location.href;
RefreshIFrame_Global();
/*	try {
		var w = window.open ("Del_Proc.asp?Id=" + id, 'winDelete', "width=25,height=25,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
		w.focus();
		w.moveTo(0,0);
	} catch(e) {
		alert('Your computer has a pop-up blocker or the results window is already open.');
		return;
	}
*/
}
function RefreshIFrame_Global_Opener() {
	try {
		if(window.opener && !window.opener.closed) {
			window.opener.document.parentWindow.RefreshIFrame_Global();
		}
	} catch(e) {
	}
}
function RefreshIFrame_Global() {
	try {
		document.parentWindow.location.reload();
	} catch(e) {
	}
}
function limitText(limitField, limitNum) {
	if (limitField.value.length > limitNum) {
		limitField.value = limitField.value.substring(0, limitNum);
	}
}
function isNotNumeric(sText)
{
	var ValidChars = "-0123456789.";
	var IsNotNumber = false;
	var Char;
	
	if(isNaN(sText)) {
		IsNotNumber = true;
	} else {
		try {
			for (i = 0; i < sText.length && IsNotNumber == false; i++) { 
				Char = sText.charAt(i); 
				if (ValidChars.indexOf(Char) == -1) {
					IsNotNumber = true;
				}
			}
		} catch(e) {
			IsNotNumber = true;
		}
	}
	return IsNotNumber;
}
function EnterDeliveryAddress(WorkingDir) {
	var w = window.open (WorkingDir + "/Contacts/DeliveryAddress.asp", 'DeliveryAddress', "width=500,height=430,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();
//	w.moveTo(0,0);
}
function DeliveryAddress_Select(FullAddress, Company, Address1, Address2, Suburb, StateId, State, PostCode, Country) {
	if(window.opener && !window.opener.closed) {
		window.opener.document.parentWindow.DeliveryAddress_Select_InJobOrder(FullAddress, Company, Address1, Address2, Suburb, StateId, State, PostCode, Country);
	}
}
function DeliveryAddress_Select_InJobOrder(FullAddress, Company, Address1, Address2, Suburb, StateId, State, PostCode, Country) {
	document.getElementById("DeliveryAddress").value = FullAddress;
	document.getElementById("DelCompany").value = Company;
	document.getElementById("DelAddress1").value = Address1;
	document.getElementById("DelAddress2").value = Address2;
	document.getElementById("DelSuburb").value = Suburb;
	document.getElementById("DelStateId").value = StateId;
	document.getElementById("DelState").value = State;
	document.getElementById("DelPostCode").value = PostCode;
	document.getElementById("DelCountry").value = Country;
}
function EnterInvoiceAddress(WorkingDir) {
	var w = window.open (WorkingDir + "/Contacts/InvoiceAddress.asp", 'InvoiceAddress', "width=500,height=430,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();
//	w.moveTo(0,0);
}
function InvoiceAddress_Select(FullAddress, Company, Address1, Address2, Suburb, StateId, State, PostCode, Country) {
	if(window.opener && !window.opener.closed) {
		window.opener.document.parentWindow.InvoiceAddress_Select_InJobOrder(FullAddress, Company, Address1, Address2, Suburb, StateId, State, PostCode, Country);
	}
}
function InvoiceAddress_Select_InJobOrder(FullAddress, Company, Address1, Address2, Suburb, StateId, State, PostCode, Country) {
	document.getElementById("InvoiceAddress").value = FullAddress;
	document.getElementById("InvCompany").value = Company;
	document.getElementById("InvAddress1").value = Address1;
	document.getElementById("InvAddress2").value = Address2;
	document.getElementById("InvSuburb").value = Suburb;
	document.getElementById("InvStateId").value = StateId;
	document.getElementById("InvState").value = State;
	document.getElementById("InvPostCode").value = PostCode;
	document.getElementById("InvCountry").value = Country;
}
function EditJobContent(WorkingDir, TP, JobOrderId, JobOrderContentId) {
	ViewRecordStandard(WorkingDir, '/JobOrders/Edit.asp?TP='+TP+'&JobOrderContentId=' + JobOrderContentId + '&JobOrderId=', JobOrderId, 'WinJobOrderEdit', 760, 500, true);
}
/*
function setLyr(obj,lyr)
{
	var newX = findPosX(obj);
	var newY = findPosY(obj);
	if (lyr == 'testP') newY -= 50;
	var x = new getObj(lyr);
	x.style.top = newY + 'px';
	x.style.left = newX + 'px';
}
function findPosX(obj)
{
	var curleft = 0;
	if (obj.offsetParent)
	{
		while (obj.offsetParent)
		{
			curleft += obj.offsetLeft
			obj = obj.offsetParent;
		}
	}
	else if (obj.x)
		curleft += obj.x;
	return curleft;
}
function findPosY(obj)
{
	var curtop = 0;
	if (obj.offsetParent)
	{
		while (obj.offsetParent)
		{
			curtop += obj.offsetTop
			obj = obj.offsetParent;
		}
	}
	else if (obj.y)
		curtop += obj.y;
	return curtop;
}
*/