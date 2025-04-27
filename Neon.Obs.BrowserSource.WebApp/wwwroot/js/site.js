// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
var connection = new signalR.HubConnectionBuilder().withUrl("/twitchchat").build();

var chatCounter = 0;

connection.start().then(function () {
    console.log("signalr connection started");
}).catch(function (err) {
    return console.error(err.toString());
});

connection.on("ReceiveMessage", function (message) {
    var id = `chatid-${chatCounter}`;

    var badgeSpan = "";

    message.chatterBadges.forEach((badge) => {
        badgeSpan += `<span><img src="${badge.imageUrl}" alt="${badge.id}"/></span> `;
    });

    $("#chatapp").append(`<p id="${id}">${badgeSpan}<span style="color:${message.chatterColor}">${message.chatterName}</span>: <span class="twitch-message">${message.message}</span></p>`);

    chatCounter++;

    $(`#${id}`)[0].scrollIntoView();

    setTimeout(() => {
        $(`#${id}`).fadeOut(1000, function() {
            $(`#${id}`).remove();
        });
    }, 20000);
});