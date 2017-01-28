var webSocket;
var map;
var marker;

function onBodyLoad() {
	var xmlHttpRequest = new XMLHttpRequest();
	xmlHttpRequest.onreadystatechange = function () {
		if (xmlHttpRequest.readyState === 4 && xmlHttpRequest.status === 200) {
			// got url, open web socket
			webSocket = new WebSocket(xmlHttpRequest.responseText);
			webSocket.onopen = function (evt) { onWebSocketOpen(evt); }
			webSocket.onclose = function (evt) { onWebSocketClose(evt); }
			webSocket.onerror = function (evt) { onWebSocketError(evt); }
			webSocket.onmessage = function (evt) { onWebSocketMessage(evt); }
		}
	}
	xmlHttpRequest.open("GET", "webSocket", true);
	xmlHttpRequest.send();
	//initialize();
	//setInterval(timerCallback, 2000);
}

function onListItemClicked(id) {
	//alert("i am clicked " + id);
	//var xmlHttpRequest = new XMLHttpRequest();
	//xmlHttpRequest.onreadystatechange = function () {
	//    if (xmlHttpRequest.readyState === 4 && xmlHttpRequest.status === 200) {
	//        // got data
	//        alert(xmlHttpRequest.responseText);
	//    }
	//}
	//xmlHttpRequest.open("POST", "pid-" + id, true);
	//xmlHttpRequest.send();
	if ((webSocket !== null || webSocket !== undefined)) {
		//webSocket.send("hello how are you ?" + id);
		webSocket.send("PID:" + id);
	}
}

function initialize() {
	var myLatlng = new google.maps.LatLng(59.328648, 13.485672);
	var mapProp = {
		center: myLatlng,
		zoom: 5,
		mapTypeId: google.maps.MapTypeId.ROADMAP
	};
	map = new google.maps.Map(document.getElementById("googleMap"), mapProp);
	marker = new google.maps.Marker({
		position: myLatlng,
		draggable: true,
		map: map
	});

}
//google.maps.event.addDomListener(window, 'load', initialize);

function timerCallback() {
	console.log(new Date().toLocaleString());
	getLocation();
}

function getLocation() {
	if (navigator.geolocation) {
		navigator.geolocation.getCurrentPosition(showPosition, showError);
	}
}

function showError(error) {
	//switch (error.code) {
	//    case error.PERMISSION_DENIED:
	//        x.innerHTML = "User denied the request for Geolocation."
	//        break;
	//    case error.POSITION_UNAVAILABLE:
	//        x.innerHTML = "Location information is unavailable."
	//        break;
	//    case error.TIMEOUT:
	//        x.innerHTML = "The request to get user location timed out."
	//        break;
	//    case error.UNKNOWN_ERROR:
	//        x.innerHTML = "An unknown error occurred."
	//        break;
	//}
	console.log("error " + error.code);
}

function showPosition(position) {
	clearInterval(timerCallback);
	var data = "loc:" + position.coords.latitude + ":" + position.coords.longitude;

	console.log(data);
	var newLatLng = new google.maps.LatLng(position.coords.latitude, position.coords.longitude);

	marker.setPosition(newLatLng);
	map.setCenter(newLatLng);
	// send to server
	var reqGeoLocation = new XMLHttpRequest();
	reqGeoLocation.onreadystatechange = function () {
		if (reqGeoLocation.readyState === 4 && reqGeoLocation.status === 200) {
			// got url, open web socket
			console.log("req geoloc success");
		}
	}
	reqGeoLocation.open("GET", data, true);
	reqGeoLocation.send();
	setInterval(timerCallback, 2000);
}


function onWebSocketOpen(evt) {
	console.log("websocket opened");
	//alert("websocket opened");
	webSocket.send("hello from websocket");
}

function onWebSocketClose(evt) {
	console.log("websocket closed");
}

function onWebSocketError(evt) {
	console.log("websocket error");
}

function enableChkAndButton() {
	var progressBar = document.getElementById("fileProgressBar");
	progressBar.style.display = "none";
	progressBar.style.width = "0%";
	progressBar.innerHTML = "";

	var btn = document.getElementById("btnTransferFiles");
	btn.disable = false;
	btn.disabled = "";
	btn.className = "btnShowFiles";
	// enable all check-boxes also
	var chkBoxes = document.getElementsByClassName("chkFile");
	var i;
	for (i = 0; i < chkBoxes.length; i++) {
		chkBoxes[i].disable = false;
		chkBoxes[i].disabled = "";
		chkBoxes[i].checked = false;
	}
}

function onWebSocketMessage(evt) {
	console.log("websocket message " + evt.data);
	var msg = evt.data;
	var json = JSON.parse(msg);
	if (json.MsgId === "Process") {
		handleProcessData(json);
	} else if (json.MsgId === "DateTime") {
		var eleDateTime = document.getElementById("hDateTime");
		eleDateTime.innerHTML = "[" + json.Value + "]";
	}
	else if (json.MsgId === "FilePercentage-Error") {
		var eleDivError = document.getElementById("divError");
		eleDivError.innerHTML = json.Value;
		enableChkAndButton();
	}
	else if (json.MsgId === "FilePercentage") {
		var progressBar = document.getElementById("fileProgressBar");
		if (json.Value === "DONE") {
			enableChkAndButton();
		} else {
			progressBar.style.width = json.Value;
			progressBar.innerHTML = "( " + json.Value + " )" + " completed";
		}
	}
}

function handleProcessData(jsonData) {
	var ele = document.getElementById("hPName");
	ele.innerHTML = "[ " + jsonData.ProcessName + " ]";

	var cvs = document.getElementById("pCanvas");
	var ctx = cvs.getContext("2d");

	ctx.clearRect(0, 0, cvs.width, cvs.height);
	ctx.stroke();
	var xPos = 5;
	var yPos = 35;
	var rowHeight = 30;
	var columnWidth = 350;

	ctx.font = "20px Georgia";
	ctx.fillText("ProcessName:", xPos, yPos);
	ctx.fillText(jsonData.ProcessName, xPos + columnWidth, yPos);
	yPos += rowHeight;

	ctx.fillText("Threads:", xPos, yPos);
	ctx.fillText(jsonData.Threads, xPos + columnWidth, yPos);
	yPos += rowHeight;

	ctx.fillText("PrivateMemorySize64:", xPos, yPos);
	ctx.fillText(jsonData.PrivateMemorySize64, xPos + columnWidth, yPos);
	yPos += rowHeight;

	ctx.fillText("BasePriority:", xPos, yPos);
	ctx.fillText(jsonData.BasePriority, xPos + columnWidth, yPos);
	yPos += rowHeight;

	ctx.fillText("PagedMemorySize64:", xPos, yPos);
	ctx.fillText(jsonData.PagedMemorySize64, xPos + columnWidth, yPos);
	yPos += rowHeight;

	ctx.fillText("MachineName:", xPos, yPos);
	ctx.fillText(jsonData.MachineName, xPos + columnWidth, yPos);
}

function btnShowFilesClicked() {
	console.log("show files clicked");
	webSocket.close();
	var ph = document.getElementById("pHidden");
	var userid = ph.innerText;
	//if ((webSocket !== null || webSocket !== undefined)) {
	//	webSocket.send("SHOWFILES:" + userid);
	//} else {
	var url = "show_files.html";
	var params = "SHOWFILES=" + userid;
	var xmlHttpRequest = new XMLHttpRequest();
	xmlHttpRequest.onreadystatechange = function () {
		//if (xmlHttpRequest.readyState === 4 && xmlHttpRequest.status === 200) {
		//}
	}
	xmlHttpRequest.open("POST", url, true);
	xmlHttpRequest.send(params);
	//}
}

function btnTransferFilesClicked() {
	console.log("btn transfer files clicked");
	var chkBoxes = document.getElementsByClassName("chkFile");
	var fileName = [];
	var i;
	for (i = 0; i < chkBoxes.length; i++) {
		if (chkBoxes[i].checked) {
			fileName.push(chkBoxes[i].value);
		}
	}

	if (fileName.length > 0) {
		var progressBar = document.getElementById("fileProgressBar");
		progressBar.style.display = "block";

		var btn = document.getElementById("btnTransferFiles");
		btn.disabled = "disabled";
		btn.className = "btnShowFilesDisable";
		// disable all check boxes
		for (i = 0; i < chkBoxes.length; i++) {
			chkBoxes[i].disable = true;
			chkBoxes[i].disabled = "disabled";
		}

		var divErr = document.getElementById("divError");
		divErr.innerHTML = "";

		var cmd = "FILE_TRANSFER:";
		for (var j = 0; j < fileName.length; j++) {
			if (j === fileName.length - 1) {
				cmd += fileName[j];
			} else {
				cmd += fileName[j] + ",";
			}
		}
		// send cmd
		if ((webSocket !== null || webSocket !== undefined)) {
			webSocket.send(cmd);
		}
	}
}