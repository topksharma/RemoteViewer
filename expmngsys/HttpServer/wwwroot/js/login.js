var webSocket;

function onBodyLoad() {
	var xmlHttpRequest = new XMLHttpRequest();
	xmlHttpRequest.onreadystatechange = function () {
		if (xmlHttpRequest.readyState === 4 && xmlHttpRequest.status === 200) {
			// got URL, open web socket
			webSocket = new WebSocket(xmlHttpRequest.responseText);
			webSocket.onopen = function (evt) { onWebSocketOpen(evt); }
			webSocket.onclose = function (evt) { onWebSocketClose(evt); }
			webSocket.onerror = function (evt) { onWebSocketError(evt); }
			webSocket.onmessage = function (evt) { onWebSocketMessage(evt); }
		}
	}
	xmlHttpRequest.open("GET", "webSocket", true);
	xmlHttpRequest.send();
}

function onWebSocketOpen(evt) {
	console.log("web-socket opened");
	//alert("web-socket opened");
	webSocket.send("hello from web-socket");
}

function onWebSocketClose(evt) {
	console.log("web-socket closed");
}

function onWebSocketError(evt) {
	console.log("web-socket error");
}

function onWebSocketMessage(evt) {
	console.log("web-socket-message " + evt.data);
	var msg = evt.data;
	var json = JSON.parse(msg);
	if (json.MsgId === "Login-Error") {
		var divLoginError = document.getElementById("divLoginError");
		divLoginError.innerHTML = json.Value;
	}
}

function btnLoginClicked() {
	var txtEmail = document.getElementById("txtEmail").value;
	var txtPassword = document.getElementById("txtPassword").value;

	if (!txtEmail || !txtPassword) {
		var perror = document.getElementById("divLoginError");
		perror.innerHTML = "<p id=pError class=pLoginErr>" + "Either email or password is empty." + "</p>";
	} else if (txtEmail && txtPassword) {
		var element = document.getElementById("pError");
		if (element != null) {
			element.parentNode.removeChild(element);
		}
		var cmd = "LOGIN:";
		cmd += txtEmail;
		cmd += "," + txtPassword;
		webSocket.send(cmd);
	}
}

