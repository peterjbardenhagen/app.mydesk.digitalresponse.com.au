function checkDecimals(fieldName, fieldValue) {
	decallowed = 2;  // how many decimals are allowed?

	if (isNotNumeric(fieldValue) || fieldValue == "") {
		alert("Oops!  That does not appear to be a valid number.  Please try again.");
		fieldName.select();
		fieldName.focus();
	}
	else {
		if (fieldValue.indexOf('.') == -1) fieldValue += ".";
		dectext = fieldValue.substring(fieldValue.indexOf('.')+1, fieldValue.length);
		if (dectext.length > decallowed)
		{
			alert ("Oops!  Please enter a number with up to " + decallowed + " decimal places.  Please try again.");
			fieldName.select();
			fieldName.focus();
		}
		else {
			alert ("That number validated successfully.");
      }
   }
}

function Switch_GST(bGST) {
/*
	if(bGST) {
		document.getElementById("GST0").style.display = 'block';
		document.getElementById("GST1").style.display = 'block';
		document.getElementById("GST2").style.display = 'block';
		document.getElementById("GST3").style.display = 'block';
	} else {
		document.getElementById("GST0").style.display = 'none';
		document.getElementById("GST1").style.display = 'none';
		document.getElementById("GST2").style.display = 'none';
		document.getElementById("GST3").style.display = 'none';
	}
*/
}





// ARRAYS

const Quantity = [ ];
const POProductTypeId = [ ];
const PartCodeId = [ ];
const Item = [ ];
const PriceEx = [ ];
const PriceExSubTotal = [ ];

function saveValues() {
		var itemLine = 2; // starts at 2
		var i = itemLine;
		while (i < (itemLines+1)){
			if (document.getElementById("PriceEx"+i) != null && document.getElementById("PriceEx"+i).value != "undefined" && document.getElementById("PriceEx"+i).value != "" && (typeof document.getElementById("PriceEx"+i).value !== "undefined")) { // not empty	
				Quantity[i-2] = document.getElementById("Quantity"+i).value;
				POProductTypeId[i-2] = document.getElementById("POProductTypeId"+i).value;
				PartCodeId[i-2] = document.getElementById("PartCodeId"+i).value;
				Item[i-2] = document.getElementById("Item"+i).value;
				PriceEx[i-2] = document.getElementById("PriceEx"+i).value;
				PriceExSubTotal[i-2] = document.getElementById("PriceExSubTotal"+i).value;

				console.log('Function = saveValues');
				console.log('Item input value = ' + document.getElementById("Item"+i).value);
			} else {
				Quantity[i-2] = "0";
				POProductTypeId[i-2] = "1"; // consumable
				PartCodeId[i-2] = "2"; //unassigned
				Item[i-2] = "";
				PriceEx[i-2] = "0.00";
				PriceExSubTotal[i-2] = "0.00";
			}
			i++;
		}
}

function reloadValues() {
		var itemLine = 2; // starts at 2
		var i = itemLine;
		while (i < (itemLines+1)){
			if (PriceEx[i-2] != null && PriceEx[i-2] != "undefined" && PriceEx[i-2] != "" && (typeof PriceEx[i-2] !== "undefined")) { // not empty	
				document.getElementById("Quantity"+i).value = Quantity[i-2];
				document.getElementById("POProductTypeId"+i).value = POProductTypeId[i-2];
				document.getElementById("PartCodeId"+i).value = PartCodeId[i-2];
				document.getElementById("Item"+i).value = Item[i-2];
				document.getElementById("PriceEx"+i).value = PriceEx[i-2];
				document.getElementById("PriceExSubTotal"+i).value = PriceExSubTotal[i-2];
				
				console.log('Function = reloadValues');
				console.log('Item input value = ' + document.getElementById("Item"+i).value);
			} else {				
				document.getElementById("Quantity"+i).value = "0";
				document.getElementById("POProductTypeId"+i).value = "1";
				document.getElementById("PartCodeId"+i).value = "2";
				document.getElementById("Item"+i).value = "";
				document.getElementById("PriceEx"+i).value = "0.00";
				document.getElementById("PriceExSubTotal"+i).value = "0.00";
				
				console.log('test');
			}
			i++;
		}
}

function MyReplace(s,x) {
	// do each twice because of name and ID
	s = s.replace("name='POProductTypeId'", 'name="POProductTypeId'+x+'"');
	s = s.replace("id='POProductTypeId'", 'id="POProductTypeId'+x+'"');

	s = s.replace("name='PartCodeId'", 'name="PartCodeId'+x+'"');
	s = s.replace("id='PartCodeId'", 'id="PartCodeId'+x+'"');
	console.log(s);
	return s;
}



// PRODUCTS

// Insert new Item Line in Quote page
function Items_InsertLine()
{
	itemLines++;
	document.Form1.ItemLinesVal.value = itemLines;
	var newitem;
	var SingleQuote = "'";
	newitem="<table width='700' cellpadding='3' cellspacing='0' id='ItemLineTable"+itemLines+"'>";
	newitem+="	<tr>";
	newitem+="		<td width=50 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=50 height=1 border=0 alt=''><br><input type='text' name='Quantity" + itemLines + "' id='Quantity" + itemLines + "' onChange='Items_CalcLineSubTotal(" + itemLines + ");Items_CalcTotal();' size=5 style='width:50px;' value=0></td>";
	newitem+="		<td width=100 class='Quote_Item_TD'>"+MyReplace(sProductTypeSel,itemLines)+"<br><br><b>Part Code:</b>"+MyReplace(sPartCodeSel,itemLines)+"</td>";
	newitem+="		<td width=250 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=250 height=1 border=0 alt=''><br><textarea rows=3 type='text' name='Item" + itemLines + "' id='Item" + itemLines + "' style='width:350px;height:190px;Overflow:hidden;' onkeyup=parent.TrackCount(this,'TextCountItems"+itemLines+"',500) onkeyup='parent.LimitText(this,500)'></textarea><br>Characters Remaining: <input type='text' name='TextCountItems"+itemLines+"' size=4 value=500 readonly></td>";
	newitem+="		<td width=80 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=80 height=1 border=0 alt=''><br><input type='text' name='PriceEx" + itemLines + "' id='PriceEx" + itemLines + "' size=1 style='width:80px;text-align:right;' value='0.00' onChange='Items_CalcLineSubTotal(" + itemLines + ");Items_CalcTotal();'></td>";
	newitem+="		<td width=80 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=80 height=1 border=0 alt=''><br><input type='text' name='PriceExSubTotal" + itemLines + "' id='PriceExSubTotal" + itemLines + "' size=1 style='width:80px;text-align:right;' readonly></td>";
	newitem+="		<td width=40 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=40 height=1 border=0 alt=''><br><input value='X' style='color:red;font-weight:bold;' type='button' onClick='Items_ClearLine(" + itemLines + ");'></td>";
	newitem+="	</tr>";
	newitem+="</table>"
	
	saveValues();
	
	var myText = document.createTextNode(newitem);
	document.getElementById("PurchaseOrdersItems").innerHTML += newitem;
	
	reloadValues();
}

function Items_InsertLine_WithData(itemQuantity, itemItem, itemPriceEx, itemPriceExSubTotal, itemPOProductTypeId, itemPartCodeId)
{
	itemLines++;
	document.Form1.ItemLinesVal.value = itemLines;
	var newitem;
	newitem="<table width='700' cellpadding='3' cellspacing='0' id='ItemLineTable"+itemLines+"'>";
	newitem+="	<tr>";
	newitem+="		<td width=50 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=50 height=1 border=0 alt=''><br><input type='text' name='Quantity" + itemLines + "' id='Quantity" + itemLines + "' onChange='Items_CalcLineSubTotal(" + itemLines + ");Items_CalcTotal();' size=5 style='width:50px;' value=" + itemQuantity + "></td>";
	newitem+="		<td width=100 class='Quote_Item_TD'>"+MyReplace(sProductTypeSel,itemLines)+"<br><br><b>Part Code:</b>"+MyReplace(sPartCodeSel,itemLines)+"</td>";
	newitem+="		<td width=250 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=250 height=1 border=0 alt=''><br><textarea rows=3 type='text' name='Item" + itemLines + "' id='Item" + itemLines + "'  style='width:350px;height:190px;Overflow:hidden;' onkeyup=parent.TrackCount(this,'TextCountItems"+itemLines+"',500) onkeyup='parent.LimitText(this,500)'>" + itemItem + "</textarea><br>Characters Remaining: <input type='text' name='TextCountItems"+itemLines+"' size=4 value="+(500-itemItem.length)+" readonly></td>";
	newitem+="		<td width=80 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=80 height=1 border=0 alt=''><br><input type='text' name='PriceEx" + itemLines + "' id='PriceEx" + itemLines + "' size=1 style='width:80px;text-align:right;' value='" + formatDecimal(itemPriceEx) + "' onChange='Items_CalcLineSubTotal(" + itemLines + ");Items_CalcTotal();'></td>";
	newitem+="		<td width=80 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=80 height=1 border=0 alt=''><br><input type='text' name='PriceExSubTotal" + itemLines + "' id='PriceExSubTotal" + itemLines + "' size=1 style='width:80px;text-align:right;' value='" + formatDecimal(itemPriceExSubTotal) + "' readonly></td>";
	newitem+="		<td width=40 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=40 height=1 border=0 alt=''><br><input value='X' style='color:red;font-weight:bold;' type='button' onClick='Items_ClearLine(" + itemLines + ");'></td>";
	newitem+="	</tr>";
	newitem+="</table>"
	var myText = document.createTextNode(newitem);
	document.getElementById("PurchaseOrdersItems").innerHTML += newitem;
	
	try {
		if(itemPOProductTypeId>0){document.getElementById("POProductTypeId"+itemLines).value = itemPOProductTypeId;}
	} catch {
		console.log('error?');
	}
	try {
		if(itemPartCodeId>0){document.getElementById("PartCodeId"+itemLines).value = itemPartCodeId;}
	} catch {
		console.log('error?');
	}
}

function Items_ClearLine(itemLine) {
	document.getElementById("Quantity" + itemLine).value = '0';
	document.getElementById("Item" + itemLine).value = '';
	document.getElementById("PriceEx" + itemLine).value = '0.00';
	document.getElementById("PriceExSubTotal" + itemLine).value = '0.00';
	document.getElementById("ItemLineTable" + itemLine).style.display = 'none';
	Items_CalcAllRows();
	Items_CalcPriceExTotal();
	Items_CalcPriceIncTotal();

	console.log('recalc');
	console.log(itemLines);
	var i = 2;
	while (i < (itemLines+1)){
		Items_CalcLineSubTotal(i)
		i++
	}



}

function OpenUpdateStatus() {
	var w = window.open ('/System/Admin/Loading.html', 'PurchaseOrdersUpdateStatus', "width=300,height=300,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
	w.focus();

	if(screen.height > 800) {
		w.resizeTo(300, 300);
	}

	if (parseInt(navigator.appVersion) >= 4 && screen.height > 800) {
		var screenHeight = screen.height;
		var screenWidth = screen.width;
		var topPos = (screenHeight)/2-370;
		var leftPos = (screenWidth)/2-400;
	}
	w.moveTo(leftPos,topPos);
}

function Items_CalcLineSubTotal(itemLine) {
	document.getElementById("PriceEx" + itemLine).value = formatDecimal(document.getElementById("PriceEx" + itemLine).value);

	var lineQty = parseInt(document.getElementById("Quantity" + itemLine).value);

	if(isNotNumeric(lineQty)) {
		lineQty = 0;
		document.getElementById("Quantity" + itemLine).value = 0;
	} else {
		lineQty = parseInt(lineQty);
		document.getElementById("Quantity" + itemLine).value = lineQty;
	}

//	var quoteMargin = parseFloat(document.getElementById("Margin").value)/100;
	var linePriceEx = parseFloat(document.getElementById("PriceEx" + itemLine).value);
	var linePriceExSubTotal = linePriceEx * lineQty;

	if(isNotNumeric(lineQty)) {
		lineQty = 0;
	}

	if(isNotNumeric(linePriceEx)) {
		linePriceEx = 0;
	}

	if(isNotNumeric(linePriceExSubTotal)) {
		linePriceExSubTotal = 0;
	}

	document.getElementById("PriceExSubTotal" + itemLine).value = formatDecimal(linePriceExSubTotal);
}

function Items_CalcAllRows() {
	var i = 2;
	while (i < (itemLines+1)){
		Items_CalcLineSubTotal(i)
		i++
	}
}

function Items_CalcPriceExTotal() {
	console.log('CalcPriceExTotal');
	var i = 2;
	var myTotal = 0;
	while (i < (itemLines+1)){
		myTotal = myTotal + parseFloat(document.getElementById("PriceExSubTotal" + i).value);
		i++
	}
	document.getElementById("PriceExTotal").value = formatDecimal(myTotal);
}

function Items_CalcPriceIncTotal() {
	console.log('CalcPriceIncTotal');
	var myTotal = 0;
	myTotal = parseFloat(document.getElementById("PriceExTotal").value);
	document.getElementById("PriceGSTTotal").value = formatDecimal(formatDecimal(myTotal*1.1) - myTotal);
	document.getElementById("PriceIncTotal").value = formatDecimal(myTotal*1.1);
}

function Items_CalcTotal() {
	Items_CalcAllRows();
	Items_CalcPriceExTotal();
	Items_CalcPriceIncTotal();	
}


function CalcAll() {
	Items_CalcTotal;
	Items_CalcPriceIncTotal();
	Items_CalcTotal();
}