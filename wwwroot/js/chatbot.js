async function sendMessage() {

    let msg =
        document.getElementById("message").value;

    let response =
        await fetch("/api/chat", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                message: msg
            })
        });

    let data = await response.json();

    document.getElementById("chat").innerHTML +=
        "<p><b>Bạn:</b> " + msg + "</p>" +
        "<p><b>Bot:</b> " + data.reply + "</p>";
}

function askQuick(text) {
    document.getElementById("message").value = text;
    sendMessage();
}