const connection = new signalR.HubConnectionBuilder().withUrl("/twitchchat").withAutomaticReconnect().build();

connection.onreconnecting(error => {
    console.log("Reconnecting...", error);
});

connection.onreconnected(connectionId => {
    console.log("Reconnected. Connection ID:", connectionId);
});

connection.on("ReceiveMessage", appendChatMessage);

connection.onclose(error => {
   console.log("Connection closed. Attempting to reconnect...", error);
   tryReconnect();
});

async function tryReconnect() {
    while (true) {
        try {
            let channelId = new URLSearchParams(window.location.search.toLowerCase()).get("id");
            if (channelId === undefined || channelId === null) {
                console.log("No channel id provided for reconnection");
                break;
            }
            
            await connection.start();

            //subscribe connection to channel
            connection.invoke("JoinChannel", channelId).catch(err => console.error(err.toString()));
            
            break;
        } catch (err) {
            //console.error("Reconnection attempt failed:", err);
            await new Promise(resolve => setTimeout(resolve, 5000));
        }
    } 
}

function appendChatMessage(message) {
    var chatCounter = 0;

    var id = `chatid-${chatCounter}`;

    var badgeSpan = "";

    if (message.chatterBadges != null && message.chatterBadges.length > 0) {
        message.chatterBadges.forEach((badge) => {
            badgeSpan += `<span><img src="${badge.imageUrl}" alt="${badge.id}"/></span> `;
        });
    }

    $("#chatapp").append(`<p id="${id}">${badgeSpan}<span style="color:${message.chatterColor}">${message.chatterName}</span>: <span class="twitch-message">${message.message}</span></p>`);

    chatCounter++;

    $(`#${id}`)[0].scrollIntoView();

    setTimeout(() => {
        $(`#${id}`).fadeOut(1000, function() {
            $(`#${id}`).remove();
        });
    }, 60000);
}

async function startConnection() {
    try {
        let channelId = new URLSearchParams(window.location.search.toLowerCase()).get("id");
        if (channelId === undefined || channelId === null) {
            console.log("No channel id provided");
            return;
        }
        
        await connection.start();
        console.log("SignalR Connected.");
        
        //subscribe connection to channel
        connection.invoke("JoinChannel", channelId).catch(err => console.error(err.toString()));
    } catch (err) {
        console.error(err);
        setTimeout(() => startConnection(), 5000);
    }
}

startConnection();