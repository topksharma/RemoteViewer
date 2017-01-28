var client = new XMLHttpRequest();

function btnUploadFileClicked() {
	var file = document.getElementById("uploadfile");
	/* Create a FormData instance */
	var formData = new FormData();
	/* Add the file */
	formData.append("user", document.getElementById("pHidden").innerText);
	formData.append("upload", file.files[0]);

	client.open("post", "/upload", true);
	client.setRequestHeader("Content-Type", "multipart/form-data");
	/* Send to server */
	client.send(formData);
}

client.onreadystatechange = function () {
	if (client.readyState === 4 && client.status === 200) {
		//alert(client.statusText);
		console.log(client.statusText);
	}
}