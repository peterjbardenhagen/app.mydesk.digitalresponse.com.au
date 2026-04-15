
function SubmitForm() {
	parent.OpenResults();
}

function OpenResults() {
	try {
		var w = window.open ("/Loading.html", 'winResults', "width=760,height=400,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
		w.focus();

		w.moveTo(0,0);
		if (document.all) {
			w.resizeTo(screen.availWidth,screen.availHeight/2);
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
	return newnumber;
}

function ViewQuote(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Quotes/View.asp?Qid=', Id, 'WinQuote', 760, 500, true);
}

function UpdateQuoteStatus(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Quotes/UpdateStatus.asp?Qid=', Id, 'WinQuoteStatus', 500, 250, false);
}

function ViewInvoice(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Invoices/View.asp?InvoiceId=', Id, 'WinInvoice', 760, 500, true);
}

function ViewInvoiceDeliveryNote(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Invoices/ViewDeliveryNote.asp?InvoiceId=', Id, 'WinInvoiceDeliveryNote', 760, 500, true);
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

function ViewPurchaseOrder(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/PurchaseOrders/View.asp?POid=', Id, 'WinPurchaseOrder', 760, 500, true);
}

function ViewComments(WorkingDir, Id) {
	var Path = '';
	var w = window.open (WorkingDir + Path + Id, Window, "width='+Width+',height='+Height+',location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();
	w.moveTo(0,0);
	if (document.all) {
		w.resizeTo(screen.availWidth,screen.availHeight/2);
	}
	ViewRecordStandard(WorkingDir, '/TableComments/ViewComments.asp?CommentId=', Id, TableId, ItemId, 'WinCommentRecord', 700, 500, true);
}

function ViewTimesheet(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Timesheets/View.asp?TimesheetId=', Id, 'WinTimesheet', 700, 500, true);
}

function ViewCallReport(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/CallReports/View.asp?CallReportId=', Id, 'WinCallReport', 700, 500, true);
}

function ViewContact(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/Contacts/View.asp?ContactId=', Id, 'WinContact', 700, 500, true);
}

function ViewSalesProject(WorkingDir, Id) {
	ViewRecordStandard(WorkingDir, '/SalesProjects/View.asp?SalesProjectId=', Id, 'WinJob', 700, 500, true);
}

function ViewRecordStandard(WorkingDir, Path, Id, Window, Width, Height, FullScreen) {
	document.location.href = WorkingDir + Path + Id;
/*	var w = window.open (WorkingDir + Path + Id, Window, "width="+Width+",height="+Height+",location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();

	w.moveTo(0,0);
	if(FullScreen) {
		if (document.all) {
			w.resizeTo(screen.availWidth,screen.availHeight/2);
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
		w.resizeTo(screen.availWidth,screen.availHeight/2);
	}
}

function ReplyToTableComment(WorkingDir, ItemId, TableId, FromCode, CommentId) {
	var w = window.open (WorkingDir + "/TableComments/Reply.asp?ItemId=" + ItemId + "&TableId=" + TableId + '&FromCode=' + FromCode + '&CommentId=' + CommentId, 'ReplyTableComment', "width=640,height=400,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();
	w.moveTo(0,0);
	if (document.all) {
		w.resizeTo(screen.availWidth,screen.availHeight/2);
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


function CreateNewContact(WorkingDir, Field, ContactType) {
	var w = window.open (WorkingDir + "/Contacts/AddNewWin.asp?Field="+Field+"&ContactType="+ContactType, 'CreateNewContact'+Field, "width=640,height=400,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();
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

function copyRecord(id) {
window.open('Copy_Proc.asp?Id=' + id);
parent.document.location.href=parent.document.location.href;
RefreshIFrame_Global();
	// try {
		// var w = window.open("Copy_Proc.asp?Id=" + id, 'winCopy', "width=25,height=25,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
		// w.focus();
		// w.moveTo(0, 0);
	// } catch (e) {
		// alert('Your computer has a pop-up blocker or the results window is already open.');
		// return;
	// }
}

function RefreshIFrame_Global_Opener() {
	
	try {
		alert('test');
		parent.document.location.href=parent.document.location.href;
	} catch(e) {
	}

	
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

// Strip out unneccessary fields to avoid duplicates
function PageQuery(q) {
	var QueryString = '';
	if(q.length > 1) this.q = q.substring(0, q.length);
	else this.q = null;
	this.keyValuePairs = new Array();
	if(q) {
		for(var i=0; i < this.q.split("&").length; i++) {
			this.keyValuePairs[i] = this.q.split("&")[i];
			if(!(eval(this.keyValuePairs[i].search('Submit2')==0) || eval(this.keyValuePairs[i].search('Cache')==0) || eval(this.keyValuePairs[i].search('Submit2')==0) || eval(this.keyValuePairs[i].search('NoCache')==0) || eval(this.keyValuePairs[i].search('Page')==0) || eval(this.keyValuePairs[i].search('SortIndex')==0) || eval(this.keyValuePairs[i].search('SortDirection')==0))) {
				QueryString = QueryString + "&" + this.keyValuePairs[i]
			}
		}
	}
	return QueryString;
}

function getTrackingQS() {
	var qs;
	var qsNew;
	var returnTo;

	qs = document.location.href;
	qs = qs.replace(document.location.host, '').replace(document.location.port, '').replace(document.location.pathname, '').replace('http://', '').replace('?', '');
	qsNew = 'SortIndex=' + document.FormTracking.SortIndex.value + '&SortDirection=' + document.FormTracking.SortDirection.value + '&Page=' + document.FormTracking.Page.value + '&ReturnTo=' + parent.document.location.pathname;

	return(qsNew + PageQuery(qs));
}

function redirect(url) {
	try {
		url = url + '&' + getTrackingQS();
		url = url.replace('?&', '?');
	} catch(e) {
	}
	document.location.href = url;
}

function redirectParent(url) {
	try {
		url = url + '&' + getTrackingQS();
		url = url.replace('?&', '?');
	} catch(e) {
	}
	parent.document.location.href = url;
}