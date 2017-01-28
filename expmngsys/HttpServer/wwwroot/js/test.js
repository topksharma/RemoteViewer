
function onWindowLoaded() {
	var form = document.getElementById('file-form');
	form.onsubmit = function (event) {
		event.preventDefault();
		sendAjaxData(event);
	}
}

function sendAjaxData(event) {
	event.preventDefault();
	var fileSelect = document.getElementById('file-select');
	var uploadButton = document.getElementById('upload-button');
	// Update button text.
	uploadButton.innerHTML = 'Uploading...';
	// Get the selected files from the input.
	var files = fileSelect.files;
	// Create a new FormData object.
	var formData = new FormData();
	// Loop through each of the selected files.
	for (var i = 0; i < files.length; i++) {
		var file = files[i];

		// Check the file type.
		if (!file.type.match('image.*')) {
			continue;
		}

		// Add the file to the request.
		formData.append('photos', file, file.name + "-" + file.size);
	}
	var txtName = document.getElementById('txtName');
	formData.append('name', txtName.value);

	// Set up the request.
	var xhr = new XMLHttpRequest();
	// Open the connection.
	xhr.open('POST', 'pic_data', true);
	// Set up a handler for when the request finishes.
	xhr.onload = function () {
		if (xhr.status === 200) {
			// File(s) uploaded.
			uploadButton.innerHTML = 'Upload';
		} else {
			alert('An error occurred!');
		}
	};

	// Send the Data.
	xhr.send(formData);
}



