let connectionStarted = false;

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.start().then(() => {
    connectionStarted = true;
    console.log("Conexión establecida");
}).catch(function (err) {
    return console.error("Error de conexión:", err.toString());
});

function joinRoom() {
    const room = document.getElementById("roomInput").value;
    if (!connectionStarted) {
        alert("Conexión aún no iniciada.");
        return;
    }
    if (room && username) {
        document.getElementById("chat").innerHTML = "";
        
        connection.invoke("JoinRoom", room, username)
            .then(() => {
                document.getElementById("sendBtn").disabled = false;
                document.getElementById("chatSection").classList.remove("hidden");
            })
            .catch(function (err) {
                console.error(err.toString());
            });
    }
}

document.getElementById("messageInput").addEventListener("keypress", function(event) {
    if (event.key === "Enter") {
        event.preventDefault();
        document.getElementById("sendBtn").click();
    }
});

document.getElementById("sendBtn").addEventListener("click", () => {
    const messageInput = document.getElementById("messageInput");
    const room = document.getElementById("roomInput").value;
    const msg = messageInput.value.trim();
    
    if (msg && room) {
        connection.invoke("SendMessageToRoom", room, msg)
            .then(() => {
                messageInput.value = "";
            })
            .catch(function (err) {
                console.error("Error al enviar mensaje:", err.toString());
            });
    }
});

connection.on("ReceiveMessage", (room, user, message) => {
    if (!room || !user || !message || user === "undefined" || message === "undefined") return;
    const msg = document.createElement("div");
    msg.textContent = `[${room}] ${user}: ${message}`;
    document.getElementById("chat").appendChild(msg);
});

connection.on("ReceiveHistory", (room, history) => {
    if (!room || !history) return;
    const chatContainer = document.getElementById("chat");
    chatContainer.innerHTML = "";
    history.forEach(item => {
        if (!item || !item.user || !item.message || item.user === "undefined" || item.message === "undefined") return;
        const msg = document.createElement("div");
        msg.textContent = `[${room}] ${item.user}: ${item.message}`;
        chatContainer.appendChild(msg);
    });
});