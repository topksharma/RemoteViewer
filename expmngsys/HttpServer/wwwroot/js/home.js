function onItemClicked(selectedYear) {
	console.log("you selected year " + selectedYear);
	if (selectedYear) {
		document.getElementById("sel_year").value = selectedYear;
		// set up the request.
		var xhr = new XMLHttpRequest();
		// open the connection.
		var data = 'sel_year: ' + selectedYear + ':' + document.getElementById("pUserEmail").innerText;
		xhr.open('POST', data, true);
		// set up a handler for when the request finishes.
		xhr.onreadystatechange = function () {
			if (xhr.status === 200) {
				onSelectedYearResponseReceived(xhr.responseText);
			} else {
				if (xhr.response) {
					$('#alertMessage').html(xhr.response);
				} else {
					$('#alertMessage').text("Error while fetching year results.");
				}
				$('#alertDialog').modal('show');
			}
		};
		// send the Data.
		xhr.send();
	}
}

function onSelectedYearResponseReceived(response) {
	if (response) {
		var divListYear = document.getElementById('divListYear');
		divListYear.innerHTML = response;
	}
}

function onMonthItemClicked(yearPlusMonth) {
	console.log(yearPlusMonth);
	var splitted = yearPlusMonth.split(":");
	if (splitted.length === 3) {
		document.getElementById("sel_month").value = splitted[1];
		// set up the request.
		var xhr = new XMLHttpRequest();
		// open the connection.
		xhr.open('POST', 'sel_month: ' + yearPlusMonth, true);
		// set up a handler for when the request finishes.
		xhr.onreadystatechange = function () {
			if (xhr.status === 200) {
				onSelectedMonthResponseReceived(xhr.responseText);
			} else {
				if (xhr.response) {
					$('#alertMessage').html(xhr.response);
				} else {
					$('#alertMessage').text("Error while fetching month results.");
				}
				$('#alertDialog').modal('show');
			}
		};
		// send the Data.
		xhr.send();
	}
}


function viewImageClicked(imgID) {
	var id = "#" + imgID;
	var imgPath = "IMG_FULL_VIEW:" + document.getElementById("pUserEmail").innerText + "\\"
	+ document.getElementById("sel_year").value + "\\"
	+ document.getElementById("sel_month").value + "\\" + imgID;

	//$('.imagepreview').attr('src', $(id).attr('src'));
	$('.imagepreview').attr('src', imgPath);
	$('#imagemodal').modal('show');
}

function onSelectedMonthResponseReceived(response) {
	if (response) {
		var data = response.split("$$$$");

		var divListYear = document.getElementById('divListYear');
		divListYear.innerHTML = data[0];

		var divExpenseTable = document.getElementById('divExpenseTable');
		divExpenseTable.innerHTML = data[1];
	}
}

function closeModal(id) {
	var btnUpload;
	var frm;
	if (id === "1") {
		frm = document.getElementById("entry-form");
		frm.reset();
		btnUpload = document.getElementById("upload-button");
		btnUpload.innerText = "Submit";

		$('#entry-form').find(':input').prop('disabled', false);
		console.log("modal closed");
	} else {
		frm = document.getElementById("profile-form");
		frm.reset();
		btnUpload = document.getElementById("btnProfileUpload");
		btnUpload.innerText = "Submit";

		$('#profile-form').find(':input').prop('disabled', false);
		console.log("modal closed");
	}
}

function onCreateBtnClicked() {
	console.log("on create new clicked");
	$("#entry-form :input").prop('readonly', false);
	$('.fileinput-remove-button').click();
	var ele = document.getElementById("divSubmitDataError");
	ele.innerHTML = "";
	var billDateEle = document.getElementById("billDate");

	var date = new Date();
	var day = date.getDate();
	var month = date.getMonth() + 1;
	var year = date.getFullYear();
	if (month < 10) month = "0" + month;
	if (day < 10) day = "0" + day;
	var today = year + "-" + month + "-" + day;
	billDateEle.value = today;
}

function sendAjaxData() {

	var description = document.getElementById("description").value;
	var amount = document.getElementById("amount").value;
	if (!description || !amount) {
		// show message
		$('#alertMessage').text("Description & Amout must be given.");
		$('#alertDialog').modal('show');
		return;
	}
	//event.preventDefault();
	var fileSelect = document.getElementById('file-5');
	var uploadButton = document.getElementById('upload-button');
	// update button text.
	uploadButton.innerHTML = 'Uploading...';
	// create a new FormData object.
	var formData = new FormData();

	// get the selected files from the input.
	var files = fileSelect.files;
	// loop through each of the selected files.
	for (var i = 0; i < files.length; i++) {
		var file = files[i];
		// check the file type.
		if (!file.type.match('image.*')) {
			continue;
		}
		// add the file to the request.
		formData.append('photos', file, file.name + "-" + file.size);
	}

	formData.append("email", document.getElementById("pUserEmail").innerText);
	formData.append("year", document.getElementById("sel_year").value);
	formData.append("month", document.getElementById("sel_month").value);

	formData.append("description", document.getElementById("description").value);
	formData.append("amount", document.getElementById("amount").value);
	formData.append("currency", document.getElementById("currency").value);
	formData.append("billdate", document.getElementById("billDate").value);

	// set up the request.
	var xhr = new XMLHttpRequest();
	// open the connection.
	xhr.open('POST', 'entry_data', true);
	// set up a handler for when the request finishes.
	xhr.onload = function () {
		if (xhr.status === 200) {
			$("form :input").attr("disabled", "disabled");
			// file(s) uploaded.
			uploadButton.innerHTML = 'Uploaded';
			var eleMessage = document.getElementById("divSubmitDataError");
			eleMessage.className = "alert alert-success";
			eleMessage.innerHTML = "<strong>" + xhr.response + "</strong>";
			console.log(xhr.responseText);
			// get new data {year}:{monthNum}:{userEmail}
			var yearMonthEmail = document.getElementById("sel_year").value + ":" +
				document.getElementById("sel_month").value + ":" +
				document.getElementById("pUserEmail").innerText;
			onMonthItemClicked(yearMonthEmail);
		} else {
			var ele = document.getElementById("divSubmitDataError");
			ele.className = "alert alert-danger";
			ele.innerHTML = "<strong>" + xhr.response + "</strong>";
			console.log('An error occurred!');
		}
	};
	xhr.setRequestHeader("Content-Type", "multipart/form-data");
	// send the Data.
	xhr.send(formData);
}

function sendProfileData() {
	var userName = document.getElementById("userName").value;
	var name = document.getElementById("name").value;
	var userPassword = document.getElementById("userPassword").value;

	if (!userName || !name || !userPassword) {
		// show message
		$('#alertMessage').text("Username, Name & Password must be given.");
		$('#alertDialog').modal('show');
		return;
	}
	//event.preventDefault();
	//alert("send profile");
	var fileSelect = document.getElementById('file-6');
	var uploadButton = document.getElementById('btnProfileUpload');
	// update button text.
	uploadButton.innerHTML = 'Uploading...';
	// create a new FormData object.
	var formData = new FormData();

	// get the selected files from the input.
	var files = fileSelect.files;
	// loop through each of the selected files.
	for (var i = 0; i < files.length; i++) {
		var file = files[i];
		// check the file type.
		if (!file.type.match('image.*')) {
			continue;
		}
		// add the file to the request.
		formData.append('photos', file, file.name + "-" + file.size);
	}

	formData.append("email", document.getElementById("pUserEmail").innerText);

	formData.append("userName", document.getElementById("userName").value);
	formData.append("name", document.getElementById("name").value);
	formData.append("userPassword", document.getElementById("userPassword").value);

	// set up the request.
	var xhr = new XMLHttpRequest();
	// open the connection.
	xhr.open('POST', 'profile_data', true);
	// set up a handler for when the request finishes.
	xhr.onload = function () {
		if (xhr.status === 200) {
			$("form :input").attr("disabled", "disabled");
			$("#btnProfileClose").removeAttr('disabled');
			// file(s) uploaded.
			uploadButton.innerHTML = 'Uploaded';
			var header = document.getElementById("divPageHeader");
			header.innerHTML = xhr.response;

			var eleMessage = document.getElementById("divProfileDataError");
			eleMessage.className = "alert alert-success";
			eleMessage.innerHTML = "<strong>" + "profile updated successfully" + "</strong>";
			console.log(xhr.responseText);
		} else {
			var ele = document.getElementById("divProfileDataError");
			ele.className = "alert alert-danger";
			ele.innerHTML = "<strong>" + xhr.response + "</strong>";
			console.log('An error occurred!');
		}
	};
	xhr.setRequestHeader("Content-Type", "multipart/form-data");
	// send the Data.
	xhr.send(formData);
}

function onEntryDeleteClicked(entryId) {
	document.getElementById("sel_entry").value = entryId;
	$('#confirmation').modal('show');
}

function deleteEntry() {
	//$('#confirmation').modal('hide');
	//alert("delete entry");
	var separator = "$";
	var entryId = document.getElementById("pUserEmail").innerText + separator + document.getElementById("sel_year").value
		+ separator + document.getElementById("sel_month").value
		+ separator + document.getElementById("sel_entry").value;
	if (entryId) {
		// set up the request.
		var xhr = new XMLHttpRequest();
		// open the connection.
		xhr.open('POST', 'DELETE_ENTRY$' + entryId, true);
		// set up a handler for when the request finishes.
		xhr.onreadystatechange = function () {
			if (xhr.status === 200) {
				onEntryDeleteResponseReceived(xhr.responseText);
			} else {
				if (xhr.response) {
					$('#alertMessage').html(xhr.response);
				} else {
					$('#alertMessage').text("Error while deleting the entry.");
				}
				$('#alertDialog').modal('show');
			}
		};
		// send the Data.
		xhr.send();
	}
}

function onEntryDeleteResponseReceived(response) {
	if (response) {
		var data = response.split("$$$$");

		var divListYear = document.getElementById('divListYear');
		divListYear.innerHTML = data[0];

		var divExpenseTable = document.getElementById('divExpenseTable');
		divExpenseTable.innerHTML = data[1];

		$('#alertMessage').text("Entry successfully deleted");
		$('#alertDialog').modal('show');

		//var divExpenseTable = document.getElementById('divExpenseTable');
		//divExpenseTable.innerHTML = response;
	}
}

function onProfileLinkClicked() {
	$("#profile-form:input").prop('readonly', false);
	document.getElementById("userName").value = document.getElementById("_username").innerText;
	document.getElementById("name").value = document.getElementById("_name").innerText;
	document.getElementById("userPassword").value = "";

	var ele = document.getElementById("divProfileDataError");
	ele.innerHTML = "";
	$('#modalProfile').modal('show');
}

$('#sidebar').affix({
	offset: {
		top: 240
	}
});

function onSearchButtonClicked() {
	var ele = document.getElementById("searchInput");
	var filter = ele.value;

	var filterFloat = parseFloat(filter);
	if (isNaN(filterFloat)) {
		$('#alertMessage').text("No filter provided.");
		$('#alertDialog').modal('show');
	} else {
		var searchCommand = "SEARCH_RESULTS:" + filterFloat + ":" + document.getElementById("pUserEmail").innerText + ":" +
		document.getElementById("sel_year").value + ":" +
		document.getElementById("sel_month").value;
		// set up the request.
		var xhr = new XMLHttpRequest();
		// open the connection.
		xhr.open('POST', searchCommand, true);
		// set up a handler for when the request finishes.
		xhr.onreadystatechange = function () {
			if (xhr.status === 200) {
				onSearchSortResultsReceived(xhr.responseText);
			} else {
				$('#alertMessage').text("Error while fetching search results.");
				$('#alertDialog').modal('show');
			}
		};
		// send the Data.
		xhr.send();
	}
}

function onSearchSortResultsReceived(response) {
	if (response) {
		var divExpenseTable = document.getElementById('divExpenseTable');
		divExpenseTable.innerHTML = response;
	}
}

function onSearchBoxKeyup(event) {
	//event.preventDefault();
	//alert('You pressed a  key in textbox');
	if (event.keyCode === 13) {
		//alert('You pressed a "enter" key in textbox');
		document.getElementById("searchAnchor").click();
	}
}

//$('#searchInput').keypress(function (event) {
//	var keycode = (event.keyCode ? event.keyCode : event.which);
//	if (keycode == '13') {
//		alert('You pressed a "enter" key in textbox');
//	}
//	//Stop the event from propogation to other handlers
//	//If this line will be removed, then keypress event handler attached 
//	//at document level will also be triggered
//	event.stopPropagation();
//});

function onSortByBillDateClicked(sortOrder) {
	//alert(sortOrder);
	var sortBy = "BillDate:" + sortOrder;
	var sortCommand = "SORT_RESULTS:" + sortBy + ":" + document.getElementById("pUserEmail").innerText + ":" +
		document.getElementById("sel_year").value + ":" +
		document.getElementById("sel_month").value;
	// set up the request.
	var xhr = new XMLHttpRequest();
	// open the connection.
	xhr.open('POST', sortCommand, true);
	// set up a handler for when the request finishes.
	xhr.onreadystatechange = function () {
		if (xhr.status === 200) {
			onSearchSortResultsReceived(xhr.responseText);
		} else {
			$('#alertMessage').text("Error while fetching sort results.");
			$('#alertDialog').modal('show');
		}
	};
	// send the Data.
	xhr.send();
}

function onSortByAmountClicked(sortOrder) {
	//alert(sortOrder);
	var sortBy = "Amount:" + sortOrder;
	var sortCommand = "SORT_RESULTS:" + sortBy + ":" + document.getElementById("pUserEmail").innerText + ":" +
		document.getElementById("sel_year").value + ":" +
		document.getElementById("sel_month").value;
	// set up the request.
	var xhr = new XMLHttpRequest();
	// open the connection.
	xhr.open('POST', sortCommand, true);
	// set up a handler for when the request finishes.
	xhr.onreadystatechange = function () {
		if (xhr.status === 200) {
			onSearchSortResultsReceived(xhr.responseText);
		} else {
			$('#alertMessage').text("Error while fetching sort results.");
			$('#alertDialog').modal('show');
		}
	};
	// send the Data.
	xhr.send();
}

function onSummaryButtonClicked() {
	//alert(sortOrder);
	var summaryCommand = "SUMMARY_RESULT:" + document.getElementById("pUserEmail").innerText + ":" +
		document.getElementById("sel_year").value + ":" +
		document.getElementById("sel_month").value;
	// set up the request.
	var xhr = new XMLHttpRequest();
	// open the connection.
	xhr.open('POST', summaryCommand, true);
	// set up a handler for when the request finishes.
	xhr.onreadystatechange = function () {
		if (xhr.status === 200) {
			var eld = document.getElementById("modalDivSummaryBody");
			eld.innerHTML = xhr.responseText;
			//onSearchSortResultsReceived(xhr.responseText);
			$('#modalSummary').modal('show');
		} else {
			$('#alertMessage').text("Error while fetching summary data.");
			$('#alertDialog').modal('show');
		}
	};
	// send the Data.
	xhr.send();
}