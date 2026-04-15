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



// PRODUCTS

// Insert new Item Line in Quote page
function Items_InsertLine()
{
	itemLines++;
	document.Form1.ItemLinesVal.value = itemLines;
	var newitem;
	newitem="<table width='700' cellpadding='3' cellspacing='0' id='ItemLineTable"+itemLines+"'>";
	newitem+="	<tr>";
	newitem+="		<td width=50 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=50 height=1 border=0 alt=''><br><input type='text' name='Quantity" + itemLines + "' size=5 style='width:50px;' value=0></td>";
	newitem+="		<td width=610 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=180 height=1 border=0 alt=''><br><textarea rows=3 type='text' name='Item" + itemLines + "' style='width:610px;border:1px solid black;Overflow:hidden;' onkeyup=parent.TrackCount(this,'TextCountItems"+itemLines+"',500) onkeyup='parent.LimitText(this,500)'></textarea><br>Characters Remaining: <input type='text' name='TextCountItems"+itemLines+"' size=4 value=500 readonly></td>";
	newitem+="		<td width=40 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=60 height=1 border=0 alt=''><br><input value='X' style='color:red;font-weight:bold;' type='button' onClick='Items_ClearLine(" + itemLines + ");'></td>";
	newitem+="	</tr>";
	newitem+="</table>"
	var myText = document.createTextNode(newitem);
	document.getElementById("RFQItems").innerHTML += newitem;
}

function Items_InsertLine_WithData(itemQuantity, itemItem, itemPriceEx)
{
	itemItem.replace("'","`");
	itemLines++;
	document.Form1.ItemLinesVal.value = itemLines;
	var newitem;
	newitem="<table cellpadding='3' cellspacing='0' id='ItemLineTable"+itemLines+"'>";
	newitem+="	<tr>";
	newitem+="		<td width=50 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=50 height=1 border=0 alt=''><br><input type='text' name='Quantity" + itemLines + "' size=5 style='width:50px;' value=" + itemQuantity + "></td>";
	newitem+="		<td width=610 class='Quote_Item_TD'><img src='/Images/Spacer.gif' width=180 height=1 border=0 alt=''><br><textarea rows=3 type='text' name='Item" + itemLines + "' style='width:610px;border:1px solid black;Overflow:hidden;' onkeyup=parent.TrackCount(this,'TextCountItems"+itemLines+"',500) onkeyup='parent.LimitText(this,500)'>" + itemItem + "</textarea><br>Characters Remaining: <input type='text' name='TextCountItems"+itemLines+"' size=4 value="+(500-itemItem.length)+" readonly></td>";
	newitem+="		<td width=40 class='Quote_Item_TD' align='right'><img src='/Images/Spacer.gif' width=60 height=1 border=0 alt=''><br><input value='X' style='color:red;font-weight:bold;' type='button' onClick='Items_ClearLine(" + itemLines + ");'></td>";
	newitem+="	</tr>";
	newitem+="</table>"
	var myText = document.createTextNode(newitem);
	document.getElementById("RFQItems").innerHTML += newitem;
}

function Items_ClearLine(itemLine) {
	document.getElementById("Quantity" + itemLine).value = '0';
	document.getElementById("Item" + itemLine).value = '';
	document.getElementById("ItemLineTable" + itemLine).style.display = 'none';
}



function OpenUpdateStatus() {
	var w = window.open ('/System/Admin/Loading.html', 'RFQUpdateStatus', "width=300,height=300,location=0,menubar=0,resizable=1,scrollbars=1,status=0,titlebar=1,toolbar=0");
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