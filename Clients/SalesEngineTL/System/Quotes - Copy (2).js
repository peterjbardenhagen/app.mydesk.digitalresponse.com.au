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

function Items_UnitCostChange(itemLine) {
	Items_FormatLine(itemLine);
	var quantity = document.getElementById("Quantity"+itemLine).value;
	var unitCost = document.getElementById("UnitCost"+itemLine).value;
	var nettPrice = document.getElementById("NettPrice"+itemLine).value;
	var margin = 1-(unitCost/nettPrice);
	document.getElementById("UnitCostSubTotal"+itemLine).value = formatDecimal(quantity * unitCost);
	if(margin>0){
		document.getElementById("LineMargin"+itemLine).value = formatDecimal(margin*100);
	}else{
		Items_MarginChange(itemLine);
	}
	console.log('Function = Items_UnitCostChange');
	console.log(margin*100);
	console.log('format decimal');
	console.log(formatDecimal(margin*100));
	console.log('LineMargin input value = ' + document.getElementById("LineMargin"+itemLine).value);
	Quotes_CalcAll();
}

// PRODUCTS

// Insert new Item Line in Quote page
function Items_InsertLine(bDivisionManager)
{
	itemLines++;
	document.Form1.ItemLinesVal.value = itemLines;
	var newitem;
	newitem="<table width='760' cellpadding='3' cellspacing='0' id='Items_Holder"+itemLines+"'>";
	newitem+="	<tr>";
	newitem+="		<td width=90 class='Quote_Item_TD' style='font-size:10px;' valign='top'><img src='/Images/Spacer.gif' width=90 height=1 border=0 alt=''><br>";
	newitem+="		<div id='ItemLineQuantity"+itemLines+"'>";
	newitem+="			<input type='text' name='Quantity" + itemLines + "' id='Quantity" + itemLines + "' onChange='Items_CalcLineSubTotal(" + itemLines + ")' size=5 style='width:50px;' value=0>";
	newitem+="		</div>";
	newitem+="		<div id='ItemLineQuantity"+itemLines+"_PerUnitPerDay' style='display:none;'>";
	newitem+="			<table cellpadding=2 cellspacing=0 border=0><tr><td style='font-size:10px;''>Days:</td><td><input type='text' name='Days" + itemLines + "' id='Days" + itemLines + "' onChange='Items_CalcLineSubTotal(" + itemLines + ")' size=5 style='width:50px;' value=0></td></tr><tr><td style='font-size:10px;'>Units:</td><td><input type='text' name='Units" + itemLines + "' id='Units" + itemLines + "' onChange='Items_CalcLineSubTotal(" + itemLines + ")' size=5 style='width:50px;' value=0></td></tr></table>";
	newitem+="		</div>";
	newitem+="		</td>";
	newitem+="		<td width=50 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=50 height=1 border=0 alt=''><br><input type='text' name='Type" + itemLines + "' id='Type" + itemLines + "' style='width:50px;' maxlength=5></td>";
	newitem+="		<td width=250 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=250 height=1 border=0 alt=''><br><b>Product Code</b><br><textarea rows=3 type='text' name='ProductCode" + itemLines + "' id='ProductCode" + itemLines + "' style='width:250px;Overflow:hidden;'></textarea><br><br><b>Description</b><br><textarea rows=5 type='text' name='Description" + itemLines + "' id='Description" + itemLines + "' style='width:250px;Overflow:hidden;'></textarea></td>";
	newitem+="		<td width=220 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=220 height=1 border=0 alt=''><br>";
	newitem+="			<table cellpadding=1 cellspacing=0 border=0>";
	newitem+="				<tr><td style='font-weight:bold;font-size:10px;'>Unit Cost</td><td><input type='text' name='UnitCost" + itemLines + "' id='UnitCost" + itemLines + "' size=1 style='width:110px;border:1px solid black;text-align:right;' value='0.00' onchange='Items_UnitCostChange("+itemLines+")'></td></tr>";
	newitem+="				<tr><td style='font-weight:bold;font-size:10px;'>Nett Price</td><td><input type='text' name='NettPrice" + itemLines + "' id='NettPrice" + itemLines + "' size=1 style='border:1px solid black;width:110px;text-align:right;' onChange='Items_NettPriceChange("+itemLines+");' value='0.00'></td></tr>";
	newitem+="				<tr><td style='font-weight:bold;font-size:10px;'>Ext. Nett Price</td><td><input type='text' name='ExtNettPrice" + itemLines + "' id='ExtNettPrice" + itemLines + "' size=1 style='border:0px 0px 0px 0px;width:110px;text-align:right;' value='0.00' readonly></td></tr>";
	newitem+="			</table>";
	newitem+="		</td>";
	newitem+="		<td width=40 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=40 height=1 border=0 alt=''><br><input type='text' name='LineMargin" + itemLines + "' id='LineMargin" + itemLines + "' size=1 style='width:40px;text-align:right;' onChange='Items_MarginChange("+itemLines+");' value='0.00'></td>";
	newitem+="		<td width=15 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''><br><input value='X' style='color:red;font-weight:bold;' type='button' onClick='Items_ClearLine(" + itemLines + ");'></td>";
	newitem+="	</tr>";
	newitem+="</table>"
	newitem+="<input type='hidden' name='ProductId" + itemLines + "' id='ProductId" + itemLines + "' value='0'>"
	newitem+="<input type='hidden' name='UnitCostSubTotal" + itemLines + "' id='UnitCostSubTotal" + itemLines + "' value='0'>"
	newitem+="<input type='hidden' name='MinNettPrice" + itemLines + "' id='MinNettPrice" + itemLines + "' value='0'>"	
	
	saveValues();
	
	var myText = document.createTextNode(newitem);
	document.getElementById("QuoteItems").innerHTML += newitem;
	Line_SwitchQuantityType(itemLines, false);
	
	reloadValues();
		
//	document.getElementById("Items_UnitCost_Holder" + itemLines).style.display = 'block';
//	document.getElementById("Items_Margin_Holder" + itemLines).style.display = 'block';
	
	document.getElementById("UnitCost" + itemLines).readOnly = false;
	document.getElementById("UnitCost" + itemLines).style.border = '1px solid';
}

// ARRAYS
const UnitCost = [ ];
const ProductCode = [ ];
const Type = [ ];
const Quantity = [ ];
const NettPrice = [ ];
const ExtNettPrice = [ ];
const Description = [ ];
const LineMargin = [ ];

function saveValues() {
		var itemLine = 2; // starts at 2
		var i = itemLine;
		while (i < (itemLines+1)){
			if (document.getElementById("UnitCost"+i) != null && document.getElementById("UnitCost"+i).value != "undefined" && document.getElementById("UnitCost"+i).value != "" && (typeof document.getElementById("UnitCost"+i).value !== "undefined")) { // not empty	
				UnitCost[i-2] = document.getElementById("UnitCost"+i).value;
				ProductCode[i-2] = document.getElementById("ProductCode"+i).value;
				Type[i-2] = document.getElementById("Type"+i).value;
				Quantity[i-2] = document.getElementById("Quantity"+i).value;
				NettPrice[i-2] = document.getElementById("NettPrice"+i).value;
				ExtNettPrice[i-2] = document.getElementById("ExtNettPrice"+i).value;
				Description[i-2] = document.getElementById("Description"+i).value;
				LineMargin[i-2] = document.getElementById("LineMargin"+i).value;

				console.log('Function = saveValues');
				console.log(LineMargin[1-2]);
				console.log('LineMargin input value = ' + document.getElementById("LineMargin"+i).value);

			} else {
				UnitCost[i-2] = "0.00";
				ProductCode[i-2] = "";
				Type[i-2] = "";
				Quantity[i-2] = "0";
				NettPrice[i-2] = "0.00";
				ExtNettPrice[i-2] = "0.00";
				Description[i-2] = "";
				LineMargin[i-2] = "0.00";
			}
			i++;
		}
}

function reloadValues() {
		var itemLine = 2; // starts at 2
		var i = itemLine;
		while (i < (itemLines+1)){
			if (UnitCost[i-2] != null && UnitCost[i-2] != "undefined" && UnitCost[i-2] != "" && (typeof UnitCost[i-2] !== "undefined")) { // not empty	
				document.getElementById("UnitCost"+i).value = UnitCost[i-2];
				document.getElementById("ProductCode"+i).value = ProductCode[i-2];
				document.getElementById("Type"+i).value = Type[i-2];
				document.getElementById("Quantity"+i).value = Quantity[i-2];
				document.getElementById("NettPrice"+i).value = NettPrice[i-2];
				document.getElementById("ExtNettPrice"+i).value = ExtNettPrice[i-2];
				document.getElementById("Description"+i).value = Description[i-2];
				if(LineMargin[i-2]>0){
					document.getElementById("LineMargin"+i).value = LineMargin[i-2];
				}
				
				console.log('Function = reloadValues');
				console.log(LineMargin[i-2]);
				console.log('LineMargin input value = ' + document.getElementById("LineMargin"+i).value);

				
			} else {
				document.getElementById("UnitCost"+i).value = "0.00";
				document.getElementById("ProductCode"+i).value = "";
				document.getElementById("Type"+i).value = "";
				document.getElementById("Quantity"+i).value = "0";
				document.getElementById("NettPrice"+i).value = "0.00";
				document.getElementById("ExtNettPrice"+i).value = "0.00";
				document.getElementById("Description"+i).value = "";
				console.log('test');
//				document.getElementById("LineMargin"+i).value = "0.00";

			}
			i++;
		}
}
	

function Items_InsertLine_WithData(itemLine, itemQuantity, itemDays, itemUnits, itemType, itemProductCode, itemDescription, itemProductId, itemUnitCost, itemMinNettPrice, itemNettPrice, itemUnitCostSubTotal, itemMinExtNettPrice, itemExtNettPrice, bDivisionManager, bPerUnitPerDay)
{
	margin = 0;
	var margin = 100*(1-(itemUnitCost/itemNettPrice))
	
	margin = formatDecimal(margin);
	console.log("New line margin = " + margin);
	
	itemLines++;
	document.Form1.ItemLinesVal.value = itemLines;
	var newitem;
	newitem="<table width='760' cellpadding='3' cellspacing='0' id='Items_Holder"+itemLines+"'>";
	newitem+="	<tr>";
	newitem+="		<td width=90 class='Quote_Item_TD' style='font-size:10px;' valign='top'><img src='/Images/Spacer.gif' width=90 height=1 border=0 alt=''><br>";
	newitem+="		<div id='ItemLineQuantity"+itemLines+"' style='display:none;'>";
	newitem+="			<input type='text' name='Quantity" + itemLines + "' id='Quantity" + itemLines + "' onChange='Items_CalcLineSubTotal(" + itemLines + ")' size=5 style='width:50px;' value="+itemQuantity+">";
	newitem+="		</div>";
	newitem+="		<div id='ItemLineQuantity"+itemLines+"_PerUnitPerDay' style='display:none;'>";
	newitem+="			<table cellpadding=2 cellspacing=0 border=0><tr><td style='font-size:10px;'>Days:</td><td><input type='text' name='Days" + itemLines + "' id='Days" + itemLines + "' onChange='Items_CalcLineSubTotal(" + itemLines + ")' size=5 style='width:50px;' value="+itemDays+"></td></tr><tr><td style='font-size:10px;'>Units:</td><td><input type='text' name='Units" + itemLines + "' id='Units" + itemLines + "' onChange='Items_CalcLineSubTotal(" + itemLines + ")' size=5 style='width:50px;' value="+itemUnits+"></td></tr></table>";
	newitem+="		</div>";
	newitem+="		</td>";
	newitem+="		<td width=50 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=50 height=1 border=0 alt=''><br><input type='text' name='Type" + itemLines + "' id='Type" + itemLines + "' style='width:50px;' maxlength=10 value='"+itemType+"'></td>";
	newitem+="		<td width=250 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=250 height=1 border=0 alt=''><br><b>Product Code</b><br><textarea rows=3 type='text' name='ProductCode" + itemLines + "' id='ProductCode" + itemLines + "' style='width:250px;Overflow:hidden;' onkeyup=parent.TrackCount(this,'TextCountPC"+itemLines+"',100) onkeypress='parent.LimitText(this,100)'>"+itemProductCode+"</textarea><br>Remaining: <input type='text' name='TextCountPC"+itemLines+"' size=4 value="+(100-itemProductCode.length)+" readonly><br><br><b>Description</b><br><textarea rows=5 type='text' name='Description" + itemLines + "' id='Description" + itemLines + "' style='width:250px;Overflow:hidden;' onkeyup=parent.TrackCount(this,'TextCountItems"+itemLines+"',500) onkeypress='parent.LimitText(this,500)'>"+itemDescription+"</textarea><br>Remaining: <input type='text' name='TextCountItems"+itemLines+"' size=4 value="+(500-itemDescription.length)+" readonly></td>";
	newitem+="		<td width=220 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=220 height=1 border=0 alt=''><br>";
	newitem+="			<table cellpadding=1 cellspacing=0 border=0>";
	newitem+="				<tr><td style='font-weight:bold;font-size:10px;'>Unit Cost</td><td><input type='text' name='UnitCost" + itemLines + "' id='UnitCost" + itemLines + "' size=1 style='width:110px;border:1px solid black;text-align:right;' value='"+formatDecimal(itemUnitCost)+"' onchange='Items_UnitCostChange("+itemLines+")'></td></tr>";
	newitem+="				<tr><td style='font-weight:bold;font-size:10px;'>Nett Price</td><td><input type='text' name='NettPrice" + itemLines + "' id='NettPrice" + itemLines + "' size=1 style='border:1px solid black;width:110px;text-align:right;' onChange='Items_NettPriceChange("+itemLines+");' value='"+formatDecimal(itemNettPrice)+"'></td></tr>";
	newitem+="				<tr><td style='font-weight:bold;font-size:10px;'>Ext. Nett Price</td><td><input type='text' name='ExtNettPrice" + itemLines + "' id='ExtNettPrice" + itemLines + "' size=1 style='border:0px 0px 0px 0px;width:110px;text-align:right;' value='"+formatDecimal(itemExtNettPrice)+"' readonly></td></tr>";
	newitem+="			</table>";
	newitem+="		</td>";
	newitem+="		<td width=40 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=40 height=1 border=0 alt=''><br><input type='text' name='LineMargin" + itemLines + "' id='LineMargin" + itemLines + "' size=1 style='width:40px;text-align:right;' onChange='Items_MarginChange("+itemLines+");' value='" + margin + "'></td>";
	newitem+="		<td width=15 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''><br><input value='X' style='color:red;font-weight:bold;' type='button' onClick='Items_ClearLine(" + itemLines + ");'></td>";
	newitem+="	</tr>";
	newitem+="</table>"
	newitem+="<input type='hidden' name='ProductId" + itemLines + "' id='ProductId" + itemLines + "' alue="+itemProductId+">"
	newitem+="<input type='hidden' name='UnitCostSubTotal" + itemLines + "' id='UnitCostSubTotal" + itemLines + "' value="+itemUnitCostSubTotal+">"
	newitem+="<input type='hidden' name='MinNettPrice" + itemLines + "' id='MinNettPrice" + itemLines + "' value='0'>"
	var myText = document.createTextNode(newitem);
	document.getElementById("QuoteItems").innerHTML += newitem;
	Line_SwitchQuantityType(itemLine, bPerUnitPerDay);
	Items_NettPriceChange(itemLines);

	if(itemProductId == 0) {
		document.getElementById("UnitCost" + itemLines).readOnly = false;
		document.getElementById("UnitCost" + itemLines).style.border = '0px';
//		document.getElementById("Items_UnitCost_Holder" + itemLine).style.display = 'block';
//		document.getElementById("Items_Margin_Holder" + itemLine).style.display = 'block';
	} else {
		if(itemUnitCost > 0) {
			if(bDivisionManager == 'True') {
//				document.getElementById("Items_UnitCost_Holder" + itemLine).style.display = 'block';
//				document.getElementById("Items_Margin_Holder" + itemLine).style.display = 'block';
			} else {
//				document.getElementById("Items_UnitCost_Holder" + itemLine).style.display = 'none';
//				document.getElementById("Items_Margin_Holder" + itemLine).style.display = 'none';
			}
		} else {
			document.getElementById("UnitCost" + itemLines).readOnly = false;
			document.getElementById("UnitCost" + itemLines).style.border = '0px';
//			document.getElementById("Items_UnitCost_Holder" + itemLine).style.display = 'block';
//			document.getElementById("Items_Margin_Holder" + itemLine).style.display = 'block';
		}
	}
	
	// fix stupid bug
	Items_NettPriceChange(2);
}

function Line_SwitchQuantityType(itemLine, bPerUnitPerDay) {
	if(bPerUnitPerDay == 'True') {
		document.getElementById("Quantity"+itemLine).value = 0;
		document.getElementById("ItemLineQuantity"+itemLine+"_PerUnitPerDay").style.display = 'block';
		document.getElementById("ItemLineQuantity" + itemLine).style.display = 'none';
	} else {
//		try { document.getElementById("Units"+itemLine).value = 0; } catch { console.log('error'); }
//		try { document.getElementById("Days"+itemLine).value = 0;  } catch { console.log('error'); }
		document.getElementById("ItemLineQuantity" + itemLine).style.display = 'block';
		document.getElementById("ItemLineQuantity"+itemLine+"_PerUnitPerDay").style.display = 'none';
	}
	Items_QuantityChange(itemLine);
}

function Items_CalcAllLines() {
	var i = 2;
	while (i < (itemLines+1)){
		// Items_CalcLine(i);
		i++;
	}

}

function Items_UnitCostChange(itemLine) {
	Items_FormatLine(itemLine);
	var quantity = document.getElementById("Quantity"+itemLine).value;
	var unitCost = document.getElementById("UnitCost"+itemLine).value;
	var nettPrice = document.getElementById("NettPrice"+itemLine).value;
	var margin = 1-(unitCost/nettPrice);
	document.getElementById("UnitCostSubTotal"+itemLine).value = formatDecimal(quantity * unitCost);
	if(margin>0){
		document.getElementById("LineMargin"+itemLine).value = formatDecimal(margin*100);
	} else {
		Items_MarginChange(itemLine);
	}

	console.log('Function = Items_UnitCostChange(itemLine)');
	console.log(formatDecimal(margin*100));
	console.log('LineMargin input value = ' + document.getElementById("LineMargin"+itemLine).value);

	Quotes_CalcAll();
}

function Items_CalcNettPriceTotal() {
	var i = 2;
	// For Items
	var unitCostTotal = parseFloat('0.00');
	var nettPriceTotal = parseFloat('0.00');
	while (i < (itemLines+1)){
		unitCostTotal = parseFloat(unitCostTotal) + parseFloat(document.getElementById("UnitCostSubTotal"+i).value);
		nettPriceTotal = parseFloat(nettPriceTotal) + parseFloat(document.getElementById("ExtNettPrice"+i).value);
		i++;
	}
	// For Third Party Supply
	i = 2;
	while (i < (thirdPartyLines+1)){
		unitCostTotal = parseFloat(unitCostTotal) + parseFloat(document.getElementById("TP_TotalCost"+i).value);
		nettPriceTotal = parseFloat(nettPriceTotal) + parseFloat(document.getElementById("TP_ExtNettPrice"+i).value);
		i++;
	}
	document.getElementById("UnitCostTotal").value = formatDecimal(unitCostTotal);
	document.getElementById("NettPriceTotal").value = formatDecimal(nettPriceTotal);
}

function Items_FormatLine(itemLine) {
	//document.getElementById("Quantity"+itemLine).value = formatInteger(document.getElementById("Quantity"+itemLine).value);
	//document.getElementById("UnitCost"+itemLine).value = formatDecimal(document.getElementById("UnitCost"+itemLine).value);
	//document.getElementById("NettPrice"+itemLine).value = formatDecimal(document.getElementById("NettPrice"+itemLine).value);
	//document.getElementById("UnitCostSubTotal"+itemLine).value = formatDecimal(document.getElementById("UnitCostSubTotal"+itemLine).value);
	//if(formatDecimal(document.getElementById("LineMargin"+itemLine).value)>0){
	//	document.getElementById("LineMargin"+itemLine).value = formatDecimal(document.getElementById("LineMargin"+itemLine).value);
	//	console.log('jackpot');
	//}
	//console.log('Function = Items_FormatLine(itemLine)');
	//console.log(formatDecimal(document.getElementById("LineMargin"+itemLine).value));

	// fix line margin bug
	var unitCost = document.getElementById("UnitCost"+itemLine).value;
	var nettPrice = document.getElementById("NettPrice"+itemLine).value;
	var margin = 1-(unitCost/nettPrice);
	if(margin > 0) {
		document.getElementById("LineMargin"+itemLine).value = margin;
	} else {
		console.log('problem');
	}
	console.log(margin);
	console.log('LineMargin input value = ' + document.getElementById("LineMargin"+itemLine).value);
}

function Items_QuantityChange(itemLine) {
	Items_FormatLine(itemLine);
	var quantity = document.getElementById("Quantity"+itemLine).value;
	var unitCost = document.getElementById("UnitCost"+itemLine).value;
	var nettPrice = document.getElementById("NettPrice"+itemLine).value;
	document.getElementById("UnitCostSubTotal"+itemLine).value = formatDecimal(quantity * unitCost);
	document.getElementById("ExtNettPrice"+itemLine).value = formatDecimal(quantity * nettPrice);
	Quotes_CalcAll();
}

function Items_NettPriceChange(itemLine) {
	Items_FormatLine(itemLine);
	var lineQty = parseInt(document.getElementById("Quantity" + itemLine).value);
	var lineDays = parseInt(document.getElementById("Days" + itemLine).value);
	var lineUnits = parseInt(document.getElementById("Units" + itemLine).value);
	
	if(lineDays > 0 && lineUnits > 0) {
		lineQty = lineDays * lineUnits;
	}

	if(isNotNumeric(lineQty)) {
		lineQty = 0;
		document.getElementById("Quantity" + itemLine).value = 0;
	}
	var unitCost = document.getElementById("UnitCost"+itemLine).value;
	var nettPrice = document.getElementById("NettPrice"+itemLine).value;
	var margin = 1-(unitCost/nettPrice);
	document.getElementById("UnitCostSubTotal"+itemLine).value = formatDecimal(lineQty * unitCost);
	if(margin>0){
		document.getElementById("LineMargin"+itemLine).value = formatDecimal(margin*100);
	} else {
		Items_MarginChange(itemLine);
	}
	document.getElementById("ExtNettPrice"+itemLine).value = formatDecimal(lineQty * nettPrice);
	
	
	console.log('Function = Items_NettPriceChange(itemLine)');
	console.log(formatDecimal(margin*100));
	console.log('LineMargin input value = ' + document.getElementById("LineMargin"+itemLine).value);
	
	Quotes_CalcAll();
}

function Items_MarginChange(itemLine) {
	Items_FormatLine(itemLine);
	var unitCost = document.getElementById("UnitCost"+itemLine).value;
	var margin = document.getElementById("LineMargin"+itemLine).value;
	
	console.log('Function = Items_MarginChange(itemLine)');
	console.log(document.getElementById("LineMargin"+itemLine).value);
	
	
	var nettPrice = formatDecimal(unitCost/(1-margin/100));
	document.getElementById("NettPrice"+itemLine).value = nettPrice;
	Items_NettPriceChange(itemLine)
	Quotes_CalcAll();
	console.log('Margin change');
}

// From Quotes page opens a window to select a Product
function Items_OpenSelectWindow(DivisionId,itemLine) {
//	try {
		var w = window.open ("../Products/Select.asp?DivisionId=" + DivisionId + "&itemLine=" + itemLine, 'winResults', "menubar=no,location=no,resizable=yes,scrollbars=yes,status=yes");
		w.focus();
		w.moveTo(0,0);
//	}
//	catch (error) {
//	}
}

// Select Product For Quote in Selector window - Links to Quotes Window
function Items_Select(itemLine, itemProductCode, itemDescription, itemProductId, itemUnitCost, itemMinNettPrice, itemNettPrice, bDivisionManager, bPerUnitPerDay) {
	if(window.opener && !window.opener.closed) {
		window.opener.document.parentWindow.Items_SelectInQuotes(itemLine, itemProductCode, itemDescription, itemProductId, itemUnitCost, itemMinNettPrice, itemNettPrice, bDivisionManager, bPerUnitPerDay);
		window.opener.document.parentWindow.Items_InsertLine();
	}
}

// Select Product for Quote in Quotes window
function Items_SelectInQuotes(itemLine, itemProductCode, itemDescription, itemProductId, itemUnitCost, itemMinNettPrice, itemNettPrice, bDivisionManager, bPerUnitPerDay) {
	document.getElementById("Quantity"+itemLine).value = 0;
	document.getElementById("Units"+itemLine).value = 0;
	document.getElementById("Days"+itemLine).value = 0;
	document.getElementById("ProductCode" + itemLine).value = itemProductCode;
	document.getElementById("Description" + itemLine).value = itemDescription;
	document.getElementById("ProductId" + itemLine).value = itemProductId;
	document.getElementById("UnitCost" + itemLine).value = itemUnitCost;
	document.getElementById("MinNettPrice" + itemLine).value = itemMinNettPrice;
	document.getElementById("NettPrice" + itemLine).value = itemNettPrice;
	Items_CalcLineSubTotal(itemLine);
	Line_SwitchQuantityType(itemLine, bPerUnitPerDay);

	// Reset character counters
	parent.TrackCount(document.getElementById("Description"+itemLine),"TextCountItems"+itemLine,500);
	parent.LimitText(document.getElementById("Description"+itemLine),500);

	parent.TrackCount(document.getElementById("ProductCode"+itemLine),"TextCountPC"+itemLine,100);
	parent.LimitText(document.getElementById("ProductCode"+itemLine),100);

	document.getElementById("UnitCost" + itemLines).readOnly = true;
	document.getElementById("UnitCost" + itemLines).style.border = '0px';
}

function Items_CalcLineSubTotal(itemLine) {
	Items_NettPriceChange(itemLine);
}

function Items_CalcRealMargin() {
	var myUnitCostTotal;
	var myNettPriceTotal;
	var myMargin = parseInt(0);
	var myUnitCostTotal = parseFloat(parseFloat(document.getElementById("UnitCostTotal").value)/100);
	var myNettPriceTotal = parseFloat(parseFloat(document.getElementById("NettPriceTotal").value)/100);

	if(myUnitCostTotal > 0 && myNettPriceTotal > 0) {
		myMargin = 100*(1-(myUnitCostTotal/myNettPriceTotal))
	}

	myMargin = formatDecimal(myMargin);
	document.getElementById("RealMargin").value = formatDecimal(myMargin);
	return formatDecimal(myMargin) + '%';
}

function Items_ClearLine(itemLine) {
	document.getElementById("Quantity" + itemLine).value = '0';
	document.getElementById("Units"+itemLine).value = 0;
	document.getElementById("Days"+itemLine).value = 0;
	document.getElementById("ProductCode" + itemLine).value = '';
	try { document.getElementById("Description" + itemLine).value = ''; } catch { } // upgrade - not sure why not working
	document.getElementById("MinNettPrice" + itemLine).value = '0.00';
	document.getElementById("NettPrice" + itemLine).value = '0.00';
	document.getElementById("UnitCost" + itemLine).value = '0.00';
	document.getElementById("UnitCostSubTotal" + itemLine).value = '0.00';
	document.getElementById("ProductId" + itemLine).value = '0';
	document.getElementById("ExtNettPrice" + itemLine).value = '0.00';
	document.getElementById("LineMargin" + itemLine).value = '0.00';
	document.getElementById("Items_Holder" + itemLine).style.display = 'none';
	Items_CalcAllLines();
	Items_CalcNettPriceTotal();
	Quotes_CalcTotal();
}

function Quotes_CalcTotal() {
	var myTotal = 0;
	myTotal = parseFloat(document.getElementById("NettPriceTotal").value);
	document.getElementById("NettPriceTotalInc").value = formatDecimal(myTotal*1.1);
	Items_CalcRealMargin();
}

function Quotes_CalcAll_Edit() {
	Quotes_CalcTotal;
//	Items_CalcAllLines();
	Items_CalcNettPriceTotal();
	Quotes_CalcTotal();
}

function Quotes_CalcAll() {
//	TP_Calc_Total();
	Quotes_CalcTotal;
//	Items_CalcAllLines();
	Items_CalcNettPriceTotal();
	Quotes_CalcTotal();
}


















function OpenUpdateStatus() {
	var w = window.open ('/System/Admin/Loading.html', 'QuotesUpdateStatus', "width=300,height=300,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
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

function calcLineMargin(UnitCost, NettPrice) {
	// eg. 95.00
	var LineMargin;
	LineMargin = formatDecimal(100*(1-(UnitCost/NettPrice)));
	if(isNotNumeric(LineMargin)) {
		console.log('Function = calcLineMargin(UnitCost, NettPrice)');
		console.log('margin = 0.01');
		return '0.01';
	} else {
		return LineMargin;
	}
}

function calcNettPrice(UnitCost, Margin) {
	var NettPrice;
	NettPrice = formatDecimal(UnitCost/(1-Margin));
	if(isNotNumeric(NettPrice)) {
		return '0.00';
	} else {
		return NettPrice;
	}
}


function ThirdParty_InsertLine()
{
	thirdPartyLines++;
	document.Form1.ThirdPartyLinesVal.value = thirdPartyLines;
	var newitem;
	newitem="<table width=760 cellpadding='0' cellspacing='0' id='TP_Holder"+thirdPartyLines+"'>";
	newitem+="	<tr>";
	newitem+="		<td valign='top'>";
	newitem+="			<table width='100%' cellpadding='3' cellspacing='0'>";
	newitem+="				<tr>";
	newitem+="					<td width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Description</td>";
	newitem+="					<td width=230 align='right'><img src='/Images/Spacer.gif' width=230 height=1 border=0 alt=''><br><input type='text' name='TP_Description" + thirdPartyLines + "' id='TP_Description" + thirdPartyLines + "' size=1 style='width:230px;' value='' maxlength=500></td>";
	newitem+="					<td width=20 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=20 height=1 border=0 alt=''></td>";
	newitem+="					<td width=80 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=80 height=1 border=0 alt=''><br>Supplier</td>";
	newitem+="					<td width=150 align='right'><img src='/Images/Spacer.gif' width=150 height=1 border=0 alt=''><br><input type='text' name='TP_Supplier" + thirdPartyLines + "' id='TP_Supplier" + thirdPartyLines + "' size=1 style='width:230px;' value='' maxlength=500></td>";
	newitem+="					<td align='right'><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''><br><input value='X' style='color:red;font-weight:bold;' type='button' onClick='ThirdParty_ClearLine(" + thirdPartyLines + ");'></td>";
	newitem+="				</tr>";
	newitem+="			</table>";
	newitem+="			<table width='100%' cellpadding='3' cellspacing='0'>";
	newitem+="				<tr>";
	newitem+="					<td width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Quote #</td>";
	newitem+="					<td width=100 align='right'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br><input type='text' name='TP_QuoteNumber" + thirdPartyLines + "' id='TP_QuoteNumber" + thirdPartyLines + "' size=1 style='width:100px;' value='' maxlength=50></td>";
	newitem+="					<td width=15><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''>"
	newitem+="					<td	width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Quote Date</td>";
	newitem+="					<td width=100 align='right'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br><input type='text' name='TP_QuoteDate" + thirdPartyLines + "' id='TP_QuoteDate" + thirdPartyLines + "' size=1 style='width:100px;' onclick='clearMe(this);;' value='dd/mm/yyyy' maxlength=10></td>";
	newitem+="					<td width=15><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''>"
	newitem+="					<td width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Expiry Date</td>";
	newitem+="					<td align='right'><img src='/Images/Spacer.gif' width=120 height=1 border=0 alt=''><br><input type='text' name='TP_ExpiryDate" + thirdPartyLines + "' id='TP_ExpiryDate" + thirdPartyLines + "' size=1 style='width:100px;' onclick='clearMe(this);;' value='dd/mm/yyyy' maxlength=10></td>";
	newitem+="				</tr>";
	newitem+="				<tr>";
	newitem+="					<td width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Supplier Part #</td>";
	newitem+="					<td width=100 align='right'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br><input type='text' name='TP_SupplierPartNumber" + thirdPartyLines + "' id='TP_SupplierPartNumber" + thirdPartyLines + "' size=1 style='width:100px;' value='' maxlength=50></td>";
	newitem+="					<td width=15><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''>"
	newitem+="					<td width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Our Part #</td>";
	newitem+="					<td width=100 align='right'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br><input type='text' name='TP_OurPartNumber" + thirdPartyLines + "' id='TP_OurPartNumber" + thirdPartyLines + "' size=1 style='width:100px;' value='' maxlength=50></td>";
	newitem+="					<td width=15><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''>"
	newitem+="					<td width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Type</td>";
	newitem+="					<td align='right'><img src='/Images/Spacer.gif' width=120 height=1 border=0 alt=''><br><input type='text' name='TP_Type" + thirdPartyLines + "' id='TP_Type" + thirdPartyLines + "' size=1 style='width:100px;' value='' maxlength=5></td>";
	newitem+="				</tr>";
	newitem+="				<tr>";
	newitem+="					<td width=100 class='Quote_Item_TD' style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Quantity</td>";
	newitem+="					<td width=100 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br><input type='text' name='TP_Quantity" + thirdPartyLines + "' id='TP_Quantity" + thirdPartyLines + "' size=1 style='width:100px;' value='0' maxlength=6 onChange='TP_Calc("+thirdPartyLines+");'></td>";
	newitem+="					<td width=15 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''>"
	newitem+="					<td width=100 class='Quote_Item_TD' style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Unit Cost ($)</td>";
	newitem+="					<td width=100 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br><input type='text' name='TP_UnitCost" + thirdPartyLines + "' id='TP_UnitCost" + thirdPartyLines + "' size=1 style='width:100px;text-align:right;' value='0.00' maxlength=20 onChange='TP_Calc("+thirdPartyLines+");'></td>";
	newitem+="					<td width=15 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''>"
	newitem+="					<td width=100 class='Quote_Item_TD' style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Nett Price ($)</td>";
	newitem+="					<td class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=120 height=1 border=0 alt=''><br><input type='text' name='TP_NettPrice" + thirdPartyLines + "' id='TP_NettPrice" + thirdPartyLines + "' size=1 style='width:100px;text-align:right;' value='0.00' maxlength=20 onChange='TP_Calc("+thirdPartyLines+");'></td>";
	newitem+="				</tr>";
	newitem+="				<tr>";
	newitem+="					<td width=100 style='background-color:#eeeeee;border-bottom:2px solid black;font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Margin (%)</td>";
	newitem+="					<td width=100 style='background-color:#eeeeee;border-bottom:2px solid black;' align='right'><img src='/Images/Spacer.gif' width=120 height=1 border=0 alt=''><br><input type='text' name='TP_Margin" + thirdPartyLines + "' id='TP_Margin" + thirdPartyLines + "' size=1 style='border:1px solid black;background-color:#ffffff;width:100px;text-align:right;' value='0.00' maxlength=20 onChange='TP_MarginChange("+thirdPartyLines+");'></td>";
	newitem+="					<td width=15 style='background-color:#eeeeee;border-bottom:2px solid black;'><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''></td>";
	newitem+="					<td width=100 style='background-color:#eeeeee;border-bottom:2px solid black;font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Total Cost Ex. GST ($)</td>";
	newitem+="					<td width=100 style='background-color:#eeeeee;border-bottom:2px solid black;' align='right'><img src='/Images/Spacer.gif' width=120 height=1 border=0 alt=''><br><input type='text' name='TP_TotalCost" + thirdPartyLines + "' id='TP_TotalCost" + thirdPartyLines + "' size=1 style='background-color:#eeeeee;border:0px 0px 0px 0px;width:100px;text-align:right;' value='0.00' maxlength=20 readonly></td>";
	newitem+="					<td width=15 style='background-color:#eeeeee;border-bottom:2px solid black;'><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''></td>";
	newitem+="					<td width=100 style='background-color:#eeeeee;border-bottom:2px solid black;font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Ext. Nett Price ($)</td>";
	newitem+="					<td style='background-color:#eeeeee;border-bottom:2px solid black;' align='right'><img src='/Images/Spacer.gif' width=120 height=1 border=0 alt=''><br><input type='text' name='TP_ExtNettPrice" + thirdPartyLines + "' id='TP_ExtNettPrice" + thirdPartyLines + "' size=1 style='background-color:#eeeeee;border:0px 0px 0px 0px;width:100px;text-align:right;' value='0.00' maxlength=20 readonly></td>";
	newitem+="				</tr>";
	newitem+="			</table><img src='/Images/Spacer.gif' width=700 height=1 border=0>";
	newitem+="		</td>";
	newitem+="	</tr>";
	newitem+="</table>"
	var myText = document.createTextNode(newitem);
	document.getElementById("thirdPartyLines").innerHTML += newitem;
}

function ThirdParty_InsertLineWithData(thirdPartyLine, TP_Description, TP_Supplier, TP_QuoteNumber, TP_QuoteDate, TP_ExpiryDate, TP_SupplierPartNumber, TP_OurPartNumber, TP_Quantity, TP_Type, TP_UnitCost, TP_NettPrice, TP_Margin, TP_TotalCost, TP_ExtNettPrice)
{
	thirdPartyLines++;
	document.Form1.ThirdPartyLinesVal.value = thirdPartyLines;
	var newitem;
	newitem="<table width=760 cellpadding='0' cellspacing='0' id='TP_Holder"+thirdPartyLines+"'>";
	newitem+="	<tr>";
	newitem+="		<td valign='top'>";
	newitem+="			<table width='100%' cellpadding='3' cellspacing='0'>";
	newitem+="				<tr>";
	newitem+="					<td width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Description</td>";
	newitem+="					<td width=230 align='right'><img src='/Images/Spacer.gif' width=230 height=1 border=0 alt=''><br><input type='text' name='TP_Description" + thirdPartyLines + "' id='TP_Description" + thirdPartyLines + "' size=1 style='width:230px;' value='"+TP_Description+"' maxlength=500></td>";
	newitem+="					<td width=20 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=20 height=1 border=0 alt=''></td>";
	newitem+="					<td width=80 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=80 height=1 border=0 alt=''><br>Supplier</td>";
	newitem+="					<td width=150 align='right'><img src='/Images/Spacer.gif' width=150 height=1 border=0 alt=''><br><input type='text' name='TP_Supplier" + thirdPartyLines + "' id='TP_Supplier" + thirdPartyLines + "' size=1 style='width:230px;' value='"+TP_Supplier+"' maxlength=500></td>";
	newitem+="					<td align='right'><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''><br><input value='X' style='color:red;font-weight:bold;' type='button' onClick='ThirdParty_ClearLine(" + thirdPartyLines + ");'></td>";
	newitem+="				</tr>";
	newitem+="			</table>";
	newitem+="			<table width='100%' cellpadding='3' cellspacing='0'>";
	newitem+="				<tr>";
	newitem+="					<td width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Quote #</td>";
	newitem+="					<td width=100 align='right'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br><input type='text' name='TP_QuoteNumber" + thirdPartyLines + "' id='TP_QuoteNumber" + thirdPartyLines + "' size=1 style='width:100px;' value='"+TP_QuoteNumber+"' maxlength=50></td>";
	newitem+="					<td width=15><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''>"
	newitem+="					<td	width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Quote Date</td>";
	newitem+="					<td width=100 align='right'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br><input type='text' name='TP_QuoteDate" + thirdPartyLines + "' id='TP_QuoteDate" + thirdPartyLines + "' size=1 style='width:100px;' onclick='clearMe(this);;' value='"+TP_QuoteDate+"' maxlength=10></td>";
	newitem+="					<td width=15><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''>"
	newitem+="					<td width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Expiry Date</td>";
	newitem+="					<td align='right'><img src='/Images/Spacer.gif' width=120 height=1 border=0 alt=''><br><input type='text' name='TP_ExpiryDate" + thirdPartyLines + "' id='TP_ExpiryDate" + thirdPartyLines + "' size=1 style='width:100px;' onclick='clearMe(this);;' value='"+TP_ExpiryDate+"' maxlength=10></td>";
	newitem+="				</tr>";
	newitem+="				<tr>";
	newitem+="					<td width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Supplier Part #</td>";
	newitem+="					<td width=100 align='right'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br><input type='text' name='TP_SupplierPartNumber" + thirdPartyLines + "' id='TP_SupplierPartNumber" + thirdPartyLines + "' size=1 style='width:100px;' value='"+TP_SupplierPartNumber+"' maxlength=50></td>";
	newitem+="					<td width=15><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''>"
	newitem+="					<td width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Our Part #</td>";
	newitem+="					<td width=100 align='right'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br><input type='text' name='TP_OurPartNumber" + thirdPartyLines + "' id='TP_OurPartNumber" + thirdPartyLines + "' size=1 style='width:100px;' value='"+TP_OurPartNumber+"' maxlength=50></td>";
	newitem+="					<td width=15><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''>"
	newitem+="					<td width=100 style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Type</td>";
	newitem+="					<td align='right'><img src='/Images/Spacer.gif' width=120 height=1 border=0 alt=''><br><input type='text' name='TP_Type" + thirdPartyLines + "' id='TP_Type" + thirdPartyLines + "' size=1 style='width:100px;' value='" + TP_Type + "' maxlength=5></td>";
	newitem+="				</tr>";
	newitem+="				<tr>";
	newitem+="					<td width=100 class='Quote_Item_TD' style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Quantity</td>";
	newitem+="					<td width=100 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br><input type='text' name='TP_Quantity" + thirdPartyLines + "' id='TP_Quantity" + thirdPartyLines + "' size=1 style='width:100px;' value='"+TP_Quantity+"' maxlength=6 onChange='TP_Calc("+thirdPartyLines+");'></td>";
	newitem+="					<td width=15 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''>"
	newitem+="					<td width=100 class='Quote_Item_TD' style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Unit Cost ($)</td>";
	newitem+="					<td width=100 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br><input type='text' name='TP_UnitCost" + thirdPartyLines + "' id='TP_UnitCost" + thirdPartyLines + "' size=1 style='width:100px;text-align:right;' value='"+TP_UnitCost+"' maxlength=20 onChange='TP_Calc("+thirdPartyLines+");'></td>";
	newitem+="					<td width=15 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''>"
	newitem+="					<td width=100 class='Quote_Item_TD' style='font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Nett Price ($)</td>";
	newitem+="					<td class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=120 height=1 border=0 alt=''><br><input type='text' name='TP_NettPrice" + thirdPartyLines + "' id='TP_NettPrice" + thirdPartyLines + "' size=1 style='width:100px;text-align:right;' value='"+TP_NettPrice+"' maxlength=20 onChange='TP_Calc("+thirdPartyLines+");'></td>";
	newitem+="				</tr>";
	newitem+="				<tr>";
	newitem+="					<td width=100 style='background-color:#eeeeee;border-bottom:2px solid black;font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Margin (%)</td>";
	newitem+="					<td width=100 style='background-color:#eeeeee;border-bottom:2px solid black;' align='right'><img src='/Images/Spacer.gif' width=120 height=1 border=0 alt=''><br><input type='text' name='TP_Margin" + thirdPartyLines + "' id='TP_Margin" + thirdPartyLines + "' size=1 style='border:1px solid black;background-color:#ffffff;width:100px;text-align:right;' value='"+TP_Margin+"' maxlength=20 onChange='TP_MarginChange("+thirdPartyLines+");'></td>";
	newitem+="					<td width=15 style='background-color:#eeeeee;border-bottom:2px solid black;'><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''></td>";
	newitem+="					<td width=100 style='background-color:#eeeeee;border-bottom:2px solid black;font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Total Cost Ex. GST ($)</td>";
	newitem+="					<td width=100 style='background-color:#eeeeee;border-bottom:2px solid black;' align='right'><img src='/Images/Spacer.gif' width=120 height=1 border=0 alt=''><br><input type='text' name='TP_TotalCost" + thirdPartyLines + "' id='TP_TotalCost" + thirdPartyLines + "' size=1 style='background-color:#eeeeee;border:0px 0px 0px 0px;width:100px;text-align:right;' value='"+TP_TotalCost+"' maxlength=20 readonly></td>";
	newitem+="					<td width=15 style='background-color:#eeeeee;border-bottom:2px solid black;'><img src='/Images/Spacer.gif' width=15 height=1 border=0 alt=''></td>";
	newitem+="					<td width=100 style='background-color:#eeeeee;border-bottom:2px solid black;font-weight:bold;'><img src='/Images/Spacer.gif' width=100 height=1 border=0 alt=''><br>Ext. Nett Price ($)</td>";
	newitem+="					<td style='background-color:#eeeeee;border-bottom:2px solid black;' align='right'><img src='/Images/Spacer.gif' width=120 height=1 border=0 alt=''><br><input type='text' name='TP_ExtNettPrice" + thirdPartyLines + "' id='TP_ExtNettPrice" + thirdPartyLines + "' size=1 style='background-color:#eeeeee;border:0px 0px 0px 0px;width:100px;text-align:right;' value='"+TP_ExtNettPrice+"' maxlength=20 readonly></td>";
	newitem+="				</tr>";
	newitem+="			</table><img src='/Images/Spacer.gif' width=700 height=1 border=0>";
	newitem+="		</td>";
	newitem+="	</tr>";
	newitem+="</table>"
	var myText = document.createTextNode(newitem);
	document.getElementById("thirdPartyLines").innerHTML += newitem;
	
	Quotes_CalcAll();
	
	TP_Calc(thirdPartyLines)
}

function ThirdParty_ClearLine(thirdPartyLine) {
	document.getElementById("TP_Quantity" + thirdPartyLine).value = 0;
	document.getElementById("TP_Supplier" + thirdPartyLine).value = '';
	document.getElementById("TP_QuoteNumber" + thirdPartyLine).value = '';
	document.getElementById("TP_QuoteDate" + thirdPartyLine).value = '';
	document.getElementById("TP_ExpiryDate" + thirdPartyLine).value = '';
	document.getElementById("TP_SupplierPartNumber" + thirdPartyLine).value = '';
	document.getElementById("TP_UnitCost" + thirdPartyLine).value = '';
	document.getElementById("TP_NettPrice" + thirdPartyLine).value = '';
	document.getElementById("TP_Description" + thirdPartyLine).value = '';
	document.getElementById("TP_Holder" + thirdPartyLine).style.display = 'none';
	Quotes_CalcAll();
}

function TP_Calc(thirdPartyLine) {
	TP_FormatLine(thirdPartyLine);
	var TP_Quantity = formatInteger(document.getElementById("TP_Quantity"+thirdPartyLine).value);
	var TP_UnitCost = formatDecimal(document.getElementById("TP_UnitCost"+thirdPartyLine).value);
	var TP_NettPrice = formatDecimal(document.getElementById("TP_NettPrice"+thirdPartyLine).value);
	var TP_ExtNettPrice = TP_Quantity * TP_NettPrice;
	var TP_TotalCost = TP_Quantity * TP_UnitCost;
	var TP_Margin = 100*(1-(TP_UnitCost/TP_NettPrice));
	document.getElementById("TP_ExtNettPrice"+thirdPartyLine).value = formatDecimal(TP_ExtNettPrice);
	document.getElementById("TP_TotalCost"+thirdPartyLine).value = formatDecimal(TP_TotalCost);
	document.getElementById("TP_Margin"+thirdPartyLine).value = formatDecimal(TP_Margin);
	Quotes_CalcAll();
}

function TP_MarginChange(thirdPartyLine) {
	var TP_UnitCost = document.getElementById("TP_UnitCost"+thirdPartyLine).value;
	var TP_Margin = document.getElementById("TP_Margin"+thirdPartyLine).value;
	var TP_NettPrice = formatDecimal(TP_UnitCost/(1-TP_Margin/100));
	document.getElementById("TP_Margin"+thirdPartyLine).value = formatDecimal(TP_Margin);
	document.getElementById("TP_NettPrice"+thirdPartyLine).value = formatDecimal(TP_NettPrice);
	Quotes_CalcAll();
}

function TP_Calc_Total() {
	var i = 2;
	var TP_Total = parseFloat('0.00');
	while (i < (thirdPartyLines+1)){
//		Items_CalcLine(i);
		TP_Total = parseFloat(TP_Total) + parseFloat(document.getElementById("TP_ExtNettPrice"+i).value);
		i++;
	}
	return TP_Total;
}

function TP_FormatLine(thirdPartyLine) {
	document.getElementById("TP_Quantity"+thirdPartyLine).value = formatInteger(document.getElementById("TP_Quantity"+thirdPartyLine).value);
	document.getElementById("TP_UnitCost"+thirdPartyLine).value = formatDecimal(document.getElementById("TP_UnitCost"+thirdPartyLine).value);
	document.getElementById("TP_NettPrice"+thirdPartyLine).value = formatDecimal(document.getElementById("TP_NettPrice"+thirdPartyLine).value);
	document.getElementById("TP_TotalCost"+thirdPartyLine).value = formatDecimal(document.getElementById("TP_TotalCost"+thirdPartyLine).value);
	document.getElementById("TP_Margin"+thirdPartyLine).value = formatDecimal(document.getElementById("TP_Margin"+thirdPartyLine).value);
}

function clearMe(obj) {
	//obj.value = '';
}