window.Overlay = {
    onMessageReceived: null,
    onEventReceived: null,
    onInit: null,
    customUsers: null
};

const broadcasterId = window.__broadcasterId;
const styleType = window.__styleType;

const connection = new signalR.HubConnectionBuilder().withUrl("/twitchchat").withAutomaticReconnect().build();

connection.on("ReceiveMessage", (data) => {
    if (typeof window.Overlay.onMessageReceived === "function") {
        window.Overlay.onMessageReceived(data);
    }
});

connection.on("ReceiveEvent", (data) => {
    if (typeof window.Overlay.onEventReceived === "function") {
        window.Overlay.onEventReceived(data);
    }
});

connection.onreconnecting(error => {
    console.log("Reconnecting...", error);
});

connection.onreconnected(connectionId => {
    console.log("Reconnected. Connection ID:", connectionId);
});

connection.onclose(error => {
   console.log("Connection closed. Attempting to reconnect...", error);
   tryReconnect();
});

function loadOverlayCss(styleType) {
    const link = document.getElementById("overlay-style");
    link.href = `/css/overlays/${styleType}.css`;
}

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

function getDefaultSettings() {
    return {
        chatStyle: "Boxes",
        ignoreBotMessages: false,
        ignoreCommandMessages: false,
        useTwitchBadges: true,
        chatDelayMilliseconds: 0,
        alwaysKeepMessages: false,
        chatMessageRemoveDelayMilliseconds: 60000,
        fontFamily: 'calibri',
        fontSize: 26
    }
}

// function appendChatMessage(message) {
//     let chatSettings = fetchFromLocalStorage(`chatOverlaySettings-${message.channelId}`);
//    
//     if (chatSettings == null) {
//         chatSettings = getDefaultSettings();
//     } else {
//         chatSettings = JSON.parse(chatSettings);
//     }
//    
//     var id = `chatid-${chatCounter}`;
//     var badgeSpan = "";
//
//     if (message.chatterBadges != null && message.chatterBadges.length > 0 && chatSettings.useTwitchBadges) {
//         message.chatterBadges.forEach((badge) => {
//             badgeSpan += `<span><img src="${badge.imageUrl}" alt="${badge.id}"/></span> `;
//         });
//     }
//
//     $("#chatapp").append(`<p id="${id}" style="font-family: ${chatSettings.fontFamily}; font-size: ${chatSettings.fontSize}px">${badgeSpan}<span style="color:${message.chatterColor}">${message.chatterName}:</span><span class="twitch-message">${message.message}</span></p>`);
//
//     chatCounter++;
//
//     $(`#${id}`)[0].scrollIntoView();
//    
//     setTimeout(() => {
//         $(`#${id}`).fadeOut(1000, function() {
//             $(`#${id}`).remove();
//         });
//     }, chatSettings.chatMessageRemoveDelayMilliseconds);
// }

async function startConnection() {
    try {
        let channelId = new URLSearchParams(window.location.search.toLowerCase()).get("broadcasterid");
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

function syncOverlaySettings() {
    try {
        let channelId = new URLSearchParams(window.location.search.toLowerCase()).get("broadcasterid");
        if (channelId === undefined || channelId === null) {
            console.log("No channel id provided");
            return;
        }
        //`/Index?handler=ChatOverlaySettings&broadcasterId=${encodeURIComponent(channelId)}`, { method: 'GET' })
        //     .
        fetch('/Index?handler=ChatOverlaySettings&broadcasterId=' + channelId)
            .then(response => response.json())
            .then(json => {
                if (json === null)
                    return;
                
                let cacheKey = `chatOverlaySettings-${channelId}`;
                addToLocalStorage(cacheKey, JSON.stringify(json));
            })
            .catch(err => console.error(err.toString()));
    }
    catch (err) {
        console.error(err);
    }
}

function addToLocalStorage(key, value) {
    if (localStorage.getItem(key)) {
        localStorage.removeItem(key);
    }
    
    localStorage.setItem(key, value);
}

function removeFromLocalStorage(key) {
    if (!localStorage.getItem(key)) {
        return;
    }
    
    localStorage.removeItem(key);
}

function fetchFromLocalStorage(key) {
    if (!localStorage.getItem(key)) {
        return null;
    }
    
    return localStorage.getItem(key);
}

//syncOverlaySettings();
loadOverlayCss(styleType);
startConnection();